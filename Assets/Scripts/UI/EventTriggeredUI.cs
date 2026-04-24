using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §34 Layer 2 组 B B3 随机事件 Toast（audit-r5 补齐）
    ///
    /// 订阅 SurvivalGameManager.OnRandomEvent，按 eventId 映射中文名 + 染色 tint 渲染：
    ///   - 顶部中央 toast：事件名（28sp 加粗）+ 简介（18sp 轻色）
    ///   - 2.0s 显示 + 0.3s 淡出
    ///   - 通过 ModalRegistry.RequestB(id) 排队（非阻塞，允许多事件并发时顺序显示）
    ///
    /// 15 种事件映射表（对齐 Server/src/SurvivalGameEngine.js:181-199 EVENT_NAMES）：
    ///   E01_snowstorm    / E02_harvest  / E03_monster_wave / E04_warm_spring / E05_ore_vein
    ///   airdrop_supply   / ice_ground   / aurora_flash     / earthquake      / meteor_shower
    ///   heavy_fog        / hot_spring   / food_spoil       / inspiration     / morale_boost
    ///
    /// 挂载规则（CLAUDE.md #7）：挂 GameUIPanel 子 GO（常驻激活），不在 Awake 中 SetActive(false)。
    /// 字体：Alibaba 优先 + ChineseFont SDF fallback。
    ///
    /// Inspector 必填：
    ///   _root       — toast 容器 GO（初始 inactive，运行时 SetActive(true/false)）
    ///   _nameLabel  — 事件名 TMP（大字）
    ///   _descLabel  — 简介 TMP（小字）
    ///   _bgImage    — 背景 Image（染色 tint）
    /// </summary>
    public class EventTriggeredUI : MonoBehaviour
    {
        [Header("Toast Root（默认 inactive，HandleEvent 时打开）")]
        [SerializeField] private GameObject _root;

        [Header("事件名 TMP（大字加粗）")]
        [SerializeField] private TMP_Text _nameLabel;

        [Header("简介 TMP（小字轻色）")]
        [SerializeField] private TMP_Text _descLabel;

        [Header("背景 Image（染色 tint）")]
        [SerializeField] private Image _bgImage;

        // 字体路径
        private const string AlibabaFontPath = "Fonts/AlibabaPuHuiTi-3-85-Bold SDF";
        private const string ChineseFontPath = "Fonts/ChineseFont SDF";

        // Toast 显示时长
        private const float TOAST_HOLD     = 2.0f;
        private const float TOAST_FADE_OUT = 0.3f;

        // ModalRegistry B 类 id 前缀（拼 eventId 防冲突）
        private const string MODAL_B_ID_PREFIX = "event_triggered_";

        private bool      _subscribed;
        private Coroutine _toastRun;
        private string    _activeModalId;

        // 15 种事件映射表：eventId → (中文名, 简介, bg tint)
        private static readonly Dictionary<string, (string name, string desc, Color tint)> EVENT_META
            = new Dictionary<string, (string, string, Color)>
        {
            // 原 5 种
            { "E01_snowstorm",    ("暴风雪",     "工人采集效率降低",   new Color(0.7f, 0.85f, 1.0f, 0.9f)) }, // 冰蓝
            { "E02_harvest",      ("丰收时刻",   "食物产量大幅提升",   new Color(0.5f, 0.9f, 0.45f, 0.9f)) }, // 绿
            { "E03_monster_wave", ("侦察怪",     "小波怪物来袭",       new Color(1.0f, 0.85f, 0.3f, 0.9f)) }, // 黄
            { "E04_warm_spring",  ("暖流涌现",   "炉温暂时提升",       new Color(1.0f, 0.6f, 0.3f, 0.9f))  }, // 橙
            { "E05_ore_vein",     ("矿脉显现",   "矿石产量大幅提升",   new Color(0.75f, 0.55f, 0.95f, 0.9f))}, // 紫
            // §34.3 B3 新增 10 种
            { "airdrop_supply",   ("空投补给",   "天降全员资源加成",   new Color(1.0f, 0.95f, 0.4f, 0.9f)) }, // 金
            { "ice_ground",       ("地面冰封",   "工人移速减缓",       new Color(0.6f, 0.8f, 1.0f, 0.9f))  }, // 冰蓝
            { "aurora_flash",     ("极光闪现",   "全员效率短时提升",   new Color(0.5f, 1.0f, 0.85f, 0.9f)) }, // 青绿
            { "earthquake",       ("地震",       "炉温与城门受损",     new Color(0.85f, 0.45f, 0.3f, 0.9f))}, // 深红
            { "meteor_shower",    ("流星雨",     "流星击杀怪物",       new Color(0.9f, 0.4f, 0.9f, 0.9f))  }, // 粉紫
            { "heavy_fog",        ("浓雾",       "怪物血条暂时隐藏",   new Color(0.7f, 0.7f, 0.75f, 0.9f)) }, // 灰
            { "hot_spring",       ("温泉涌出",   "全员恢复生命",       new Color(1.0f, 0.75f, 0.75f, 0.9f))}, // 粉
            { "food_spoil",       ("食物变质",   "食物减少",           new Color(0.6f, 0.55f, 0.4f, 0.9f)) }, // 深米
            { "inspiration",      ("灵感爆发",   "城门加固奖励",       new Color(0.95f, 1.0f, 0.55f, 0.9f))}, // 黄绿
            { "morale_boost",     ("矿工士气",   "矿工鼓舞气泡",       new Color(1.0f, 0.78f, 0.25f, 0.9f))}, // 橙黄
        };

        // ── 生命周期 ──────────────────────────────────────────────────────

        private void Start()
        {
            EnsureFonts();
            if (_root != null) _root.SetActive(false);
            TrySubscribe();
        }

        private void OnEnable()  { TrySubscribe(); }
        private void OnDisable() { Unsubscribe(); }
        private void OnDestroy()
        {
            Unsubscribe();
            if (_toastRun != null) { StopCoroutine(_toastRun); _toastRun = null; }
            if (!string.IsNullOrEmpty(_activeModalId))
            {
                ModalRegistry.ReleaseB(_activeModalId);
                _activeModalId = null;
            }
        }

        private void Update()
        {
            if (!_subscribed) TrySubscribe();
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;
            sgm.OnRandomEvent += HandleRandomEvent;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null) sgm.OnRandomEvent -= HandleRandomEvent;
            _subscribed = false;
        }

        private void EnsureFonts()
        {
            var font = Resources.Load<TMP_FontAsset>(AlibabaFontPath);
            if (font == null) font = Resources.Load<TMP_FontAsset>(ChineseFontPath);
            if (font == null) return;
            if (_nameLabel != null && _nameLabel.font != font) _nameLabel.font = font;
            if (_descLabel != null && _descLabel.font != font) _descLabel.font = font;
        }

        // ── 事件回调 ──────────────────────────────────────────────────────

        private void HandleRandomEvent(RandomEventData data)
        {
            if (data == null || string.IsNullOrEmpty(data.eventId)) return;
            if (_root == null) return;

            // 查表获取中文名 + 简介 + tint；未命中时兜底使用服务端 name / 灰色
            string zhName;
            string zhDesc;
            Color  tint;
            if (EVENT_META.TryGetValue(data.eventId, out var meta))
            {
                zhName = meta.name;
                zhDesc = meta.desc;
                tint   = meta.tint;
            }
            else
            {
                zhName = string.IsNullOrEmpty(data.name) ? data.eventId : data.name;
                zhDesc = "";
                tint   = new Color(0.8f, 0.8f, 0.85f, 0.9f);
            }

            if (_nameLabel != null) _nameLabel.text = zhName;
            if (_descLabel != null) _descLabel.text = zhDesc;
            if (_bgImage   != null) _bgImage.color  = tint;

            _root.SetActive(true);

            // ModalRegistry B 类排队（非阻塞；不同 eventId 独立 id，不去重覆盖）
            string modalId = MODAL_B_ID_PREFIX + data.eventId;
            // 先 release 旧的同 id 避免残留
            ModalRegistry.ReleaseB(modalId);
            ModalRegistry.RequestB(modalId, () => { /* 被抢占时无需额外动作；协程结束自会 release */ });
            _activeModalId = modalId;

            if (_toastRun != null) StopCoroutine(_toastRun);
            _toastRun = StartCoroutine(ToastHoldAndFade(modalId));
        }

        // ── 协程 ──────────────────────────────────────────────────────

        private IEnumerator ToastHoldAndFade(string modalId)
        {
            SetAlpha(1f);
            yield return new WaitForSecondsRealtime(TOAST_HOLD);

            float t = 0f;
            while (t < TOAST_FADE_OUT)
            {
                t += Time.unscaledDeltaTime;
                SetAlpha(1f - Mathf.Clamp01(t / TOAST_FADE_OUT));
                yield return null;
            }

            if (_root != null) _root.SetActive(false);
            SetAlpha(1f); // 复位，下一次触发可直接用
            ModalRegistry.ReleaseB(modalId);
            if (_activeModalId == modalId) _activeModalId = null;
            _toastRun = null;
        }

        private void SetAlpha(float a)
        {
            if (_nameLabel != null) { var c = _nameLabel.color; c.a = a; _nameLabel.color = c; }
            if (_descLabel != null) { var c = _descLabel.color; c.a = a; _descLabel.color = c; }
            if (_bgImage   != null) { var c = _bgImage.color;   c.a = a * 0.9f; _bgImage.color = c; }
        }
    }
}
