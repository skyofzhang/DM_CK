using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §36.12 分时段解锁横幅 & 老用户豁免横幅（MVP 极简版）。
    ///
    /// 触发：
    ///   - SurvivalGameManager.OnNewlyUnlockedFeatures(string[]) 每个 featureId 依次滑入，2s 间隔队列
    ///   - SurvivalGameManager.OnVeteranUnlocked 专属横幅 "老玩家：全功能已开放"
    ///
    /// 挂载：Canvas/GameUIPanel（常驻）；Prefab 绑定留给人工（_panelRoot/_titleText/_descText）。
    /// 若未绑定 Prefab，降级到 AnnouncementUI（已存在的横幅系统）兜底。
    ///
    /// 设计原则（CLAUDE.md 规则 2/6）：
    ///   - 不在运行时 Instantiate 子节点；使用 _panelRoot 预创建的固定容器
    ///   - Awake 只对子节点 _panelRoot 调 SetActive(false)，不在自己 GameObject 上 SetActive
    /// </summary>
    public class FeatureUnlockBanner : MonoBehaviour
    {
        public static FeatureUnlockBanner Instance { get; private set; }

        [Header("横幅根节点（子节点，初始 inactive）")]
        [SerializeField] private GameObject _panelRoot;

        [Header("文本字段")]
        [SerializeField] private TMP_Text _titleText;   // "今日解锁"
        [SerializeField] private TMP_Text _descText;    // "城门升级 Lv5-Lv6 开放"

        [Header("CanvasGroup（滑入滑出 alpha 动画）")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("动画时序（秒）")]
        [SerializeField] private float _fadeInSec  = 0.35f;
        [SerializeField] private float _holdSec    = 2.0f;
        [SerializeField] private float _fadeOutSec = 0.35f;

        [Header("队列间隔（秒）— 连续多个 feature 解锁时")]
        [SerializeField] private float _queueGapSec = 0.4f;

        // 队列：pending 的横幅，依次播放
        private readonly Queue<BannerItem> _queue = new Queue<BannerItem>();
        private Coroutine _playCoroutine;
        private bool _isPlaying;

        private struct BannerItem
        {
            public string title;
            public string desc;
            public Color  color;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { return; }
            Instance = this;
            // ✅ 合法：对子节点 _panelRoot 执行 SetActive
            if (_panelRoot != null) _panelRoot.SetActive(false);
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
        }

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void Start()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (Instance == this) Instance = null;
        }

        private bool _subscribed = false;

        private void TrySubscribe()
        {
            if (_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;
            sgm.OnNewlyUnlockedFeatures += HandleNewlyUnlocked;
            sgm.OnVeteranUnlocked       += HandleVeteranUnlocked;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnNewlyUnlockedFeatures -= HandleNewlyUnlocked;
                sgm.OnVeteranUnlocked       -= HandleVeteranUnlocked;
            }
            _subscribed = false;
        }

        /// <summary>订阅：world_clock_tick 携带的新解锁 feature 列表 → 依次入队播放。</summary>
        private void HandleNewlyUnlocked(string[] features)
        {
            if (features == null || features.Length == 0) return;
            foreach (var f in features)
            {
                if (string.IsNullOrEmpty(f)) continue;
                EnqueueUnlock(f);
            }
        }

        /// <summary>订阅：veteran_unlocked → 专属横幅（只显示一次，不受 feature 队列影响）。</summary>
        private void HandleVeteranUnlocked(VeteranUnlockedData data)
        {
            ShowVeteranUnlocked(data);
        }

        /// <summary>SurvivalGameManager.HandleVeteranUnlocked 也会直接调用此方法（双路径冗余，无副作用）。</summary>
        public void ShowVeteranUnlocked(VeteranUnlockedData data)
        {
            string reasonText = data == null ? "" : FormatVeteranReason(data.reason);
            var item = new BannerItem
            {
                title = "老玩家：全功能已开放",
                desc  = string.IsNullOrEmpty(reasonText) ? "本赛季全功能始终可用" : reasonText,
                color = new Color(1f, 0.85f, 0.2f),
            };
            _queue.Enqueue(item);
            EnsurePlaying();
        }

        /// <summary>外部手动弹出一条 feature 解锁横幅（测试/调试用）。</summary>
        public void EnqueueUnlock(string featureId)
        {
            if (string.IsNullOrEmpty(featureId)) return;
            string displayName = GetFeatureDisplayName(featureId);
            string desc        = GetFeatureDesc(featureId);
            var item = new BannerItem
            {
                title = $"今日解锁：{displayName}！",
                desc  = desc,
                color = new Color(0.3f, 1f, 0.8f),
            };
            _queue.Enqueue(item);
            EnsurePlaying();
        }

        private void EnsurePlaying()
        {
            if (_isPlaying) return;
            if (!gameObject.activeInHierarchy) return;
            if (_playCoroutine != null) StopCoroutine(_playCoroutine);
            _playCoroutine = StartCoroutine(PlayQueue());
        }

        private IEnumerator PlayQueue()
        {
            _isPlaying = true;
            while (_queue.Count > 0)
            {
                var item = _queue.Dequeue();
                yield return PlayOne(item);
                if (_queue.Count > 0)
                    yield return new WaitForSeconds(_queueGapSec);
            }
            _isPlaying = false;
            _playCoroutine = null;
        }

        private IEnumerator PlayOne(BannerItem item)
        {
            if (_panelRoot == null || _titleText == null)
            {
                // 未绑定 Prefab：降级到 AnnouncementUI
                UI.AnnouncementUI.Instance?.ShowAnnouncement(
                    item.title, item.desc, item.color, _holdSec);
                Debug.Log($"[FeatureUnlockBanner] (未绑定 _panelRoot) 降级到 AnnouncementUI: title='{item.title}' desc='{item.desc}'");
                // 留出停留时间以串联队列
                yield return new WaitForSeconds(_holdSec + _fadeInSec + _fadeOutSec);
                yield break;
            }

            _panelRoot.SetActive(true);
            if (_titleText != null) _titleText.text = item.title;
            if (_descText != null)  _descText.text  = item.desc;

            // fade in
            if (_canvasGroup != null)
            {
                float t = 0f;
                _canvasGroup.alpha = 0f;
                while (t < _fadeInSec)
                {
                    t += Time.deltaTime;
                    _canvasGroup.alpha = Mathf.Clamp01(t / Mathf.Max(0.01f, _fadeInSec));
                    yield return null;
                }
                _canvasGroup.alpha = 1f;
            }

            // hold
            yield return new WaitForSeconds(_holdSec);

            // fade out
            if (_canvasGroup != null)
            {
                float t = 0f;
                float startA = _canvasGroup.alpha;
                while (t < _fadeOutSec)
                {
                    t += Time.deltaTime;
                    _canvasGroup.alpha = Mathf.Lerp(startA, 0f, Mathf.Clamp01(t / Mathf.Max(0.01f, _fadeOutSec)));
                    yield return null;
                }
                _canvasGroup.alpha = 0f;
            }

            _panelRoot.SetActive(false);
        }

        // ==================== feature id → 显示名 / 描述 ====================

        /// <summary>§36.12 feature id → 中文显示名。未知 id 回显原文，保证不崩溃。</summary>
        public static string GetFeatureDisplayName(string featureId)
        {
            if (string.IsNullOrEmpty(featureId)) return "未知功能";
            switch (featureId)
            {
                case "gate_upgrade_basic": return "城门升级 Lv1–Lv4";
                case "gate_upgrade_high":  return "城门升级 Lv5–Lv6";
                case "roulette":           return "主播事件轮盘";
                case "broadcaster_boost":  return "紧急加速 / 触发事件";
                case "shop":               return "商店系统";
                case "building":           return "建造系统";
                case "expedition":         return "探险系统";
                case "supporter_mode":     return "助威模式";
                case "tribe_war":          return "跨直播间攻防战";
                default:                   return featureId;
            }
        }

        /// <summary>§36.12 feature id → 简短描述（横幅 desc 行）。</summary>
        public static string GetFeatureDesc(string featureId)
        {
            if (string.IsNullOrEmpty(featureId)) return "";
            switch (featureId)
            {
                case "gate_upgrade_basic": return "主播可消耗矿石加固城门";
                case "gate_upgrade_high":  return "寒冰壁垒 / 巨龙要塞就绪";
                case "roulette":           return "5 分钟充能，抽 3 选 1";
                case "broadcaster_boost":  return "全员加速 · 随机事件";
                case "shop":               return "战术道具 + 身份装备";
                case "building":           return "瞭望塔 / 市场 / 医院 / 祭坛 / 烽火台";
                case "expedition":         return "90s 往返外域，带回资源";
                case "supporter_mode":     return "助威者可替补 AFK 守护者";
                case "tribe_war":          return "跨房间攻防 · 远征怪来袭";
                default:                   return "";
            }
        }

        /// <summary>veteran_unlocked.reason → 中文。</summary>
        public static string FormatVeteranReason(string reason)
        {
            if (string.IsNullOrEmpty(reason)) return "";
            switch (reason)
            {
                case "lifetime_contrib":   return "累计贡献达标，豁免分时段解锁";
                case "fortress_day":       return "历史堡垒日达标，豁免分时段解锁";
                case "seasons_completed":  return "赛季黏性达标，豁免分时段解锁";
                default:                   return reason;
            }
        }
    }
}
