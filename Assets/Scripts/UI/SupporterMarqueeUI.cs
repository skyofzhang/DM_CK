using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §33.6.2 助威模式 —— 助威行动跑马灯（浅紫色）
    ///
    /// 订阅 SurvivalGameManager.OnSupporterAction，根据 cmd 映射到效果描述并短暂显示：
    ///   cmd=1 → 食物+1     | cmd=2 → 煤炭+1     | cmd=3 → 矿石+1
    ///   cmd=4 → 炉温+0.5℃ | cmd=6 → 攻击加成   | cmd=666 或其他 → 不显示
    ///
    /// 格式：[助威] {playerName}：{effectText}
    /// 颜色：浅紫 (0.7, 0.5, 1.0)（与 HorizontalMarqueeUI 的默认白色区分，突出助威身份）
    ///
    /// 显示节拍：4s 停留后 0.3s 淡出；连续触发 → 下一条替换当前（不排队）。
    ///
    /// 挂载规则（CLAUDE.md #7）：挂 GameUIPanel 子 GO（常驻激活），不在 Awake 中 SetActive(false)。
    /// Inspector 必填：
    ///   _label — 跑马灯 TMP（单行）
    /// </summary>
    public class SupporterMarqueeUI : MonoBehaviour
    {
        [Header("跑马灯 TMP（单行，浅紫）")]
        [SerializeField] private TMP_Text _label;

        private const float HOLD     = 4.0f;
        private const float FADE_OUT = 0.3f;

        private static readonly Color SUPPORTER_PURPLE = new Color(0.7f, 0.5f, 1.0f, 1f);

        private static readonly Dictionary<int, string> CmdToText = new Dictionary<int, string>
        {
            { 1, "食物+1" },
            { 2, "煤炭+1" },
            { 3, "矿石+1" },
            { 4, "炉温+0.5℃" },
            { 6, "攻击加成" },
        };

        private bool      _subscribed;
        private Coroutine _runCoroutine;

        // ── 生命周期 ──────────────────────────────────────────────────────

        private void Start()
        {
            if (_label != null)
            {
                _label.text  = "";
                _label.color = SUPPORTER_PURPLE;  // 运行时着色（与 Editor TMP SerializedObject 并用双保险）
            }
            TrySubscribe();
        }

        private void OnEnable()  { TrySubscribe(); }
        private void OnDisable() { Unsubscribe(); }
        private void OnDestroy()
        {
            Unsubscribe();
            if (_runCoroutine != null) { StopCoroutine(_runCoroutine); _runCoroutine = null; }
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
            sgm.OnSupporterAction += HandleSupporterAction;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null) sgm.OnSupporterAction -= HandleSupporterAction;
            _subscribed = false;
        }

        // ── 事件回调 ──────────────────────────────────────────────────────

        private void HandleSupporterAction(SupporterActionData data)
        {
            if (data == null) return;
            if (!CmdToText.TryGetValue(data.cmd, out string effectText)) return;  // cmd=666 或未知：忽略

            string playerName = string.IsNullOrEmpty(data.playerName) ? "匿名" : data.playerName;
            string message    = $"[助威] {playerName}：{effectText}";

            if (_label != null)
            {
                _label.text  = message;
                // 运行时再写一次颜色，防止 Editor 绑定漏写
                _label.color = SUPPORTER_PURPLE;
            }

            // 替换当前（不排队）
            if (_runCoroutine != null) StopCoroutine(_runCoroutine);
            _runCoroutine = StartCoroutine(HoldAndFade());
        }

        // ── 显示协程 ──────────────────────────────────────────────────────

        private IEnumerator HoldAndFade()
        {
            // 显示区间：Alpha 1
            if (_label != null)
            {
                var c = _label.color; c.a = 1f; _label.color = c;
            }

            yield return new WaitForSecondsRealtime(HOLD);

            // 淡出
            float t = 0f;
            while (t < FADE_OUT)
            {
                t += Time.unscaledDeltaTime;
                if (_label != null)
                {
                    var c = _label.color;
                    c.a = 1f - Mathf.Clamp01(t / FADE_OUT);
                    _label.color = c;
                }
                yield return null;
            }

            if (_label != null)
            {
                _label.text = "";
                var c = _label.color; c.a = 1f; _label.color = c;  // 重置 alpha 供下次
            }
            _runCoroutine = null;
        }
    }
}
