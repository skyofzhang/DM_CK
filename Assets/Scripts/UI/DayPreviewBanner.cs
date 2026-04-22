using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §34 B10b 夜晚预告横幅 —— 白天最后 10s 底部出现
    ///
    /// 订阅 SurvivalGameManager.OnDayPreview（服务端白天最后 10s 推送一次）：
    ///   今夜预报：{nightModifier?.name ?? "普通夜晚"}
    ///   Boss HP：{bossHp}  预计怪物数：~{monsterCount}
    ///   特殊效果：{nightModifier?.description ?? "无"}
    ///   倒计时：{N}s
    ///
    /// 策划案 5184-5200：
    ///   - 白天最后 10 秒，底部横幅倒计时；
    ///   - nightModifier 可为 null（普通夜晚）；
    ///   - 制造期待感，观众抢囤资源；
    ///   - 收到 phase_changed.phase='night' 时立即隐藏（或倒计时自然结束）。
    ///
    /// 挂载规则（CLAUDE.md #7）：挂 Canvas/DayPreviewBanner（常驻激活），Awake 只对 _bannerRoot 子节点 SetActive(false)。
    ///
    /// Inspector 必填：
    ///   _bannerRoot        — 横幅根节点（子节点，初始 inactive）
    ///   _bannerCanvasGroup — CanvasGroup
    ///   _headlineText      — 第一行"今夜预报：..." TMP
    ///   _bodyText          — 第二三行"Boss HP / 预计怪物数 / 特殊效果"
    ///   _countdownText     — 倒计时"倒计时：8s" TMP
    /// </summary>
    public class DayPreviewBanner : MonoBehaviour
    {
        [Header("横幅根节点")]
        [SerializeField] private RectTransform   _bannerRoot;
        [SerializeField] private CanvasGroup     _bannerCanvasGroup;

        [Header("文字")]
        [SerializeField] private TextMeshProUGUI _headlineText;
        [SerializeField] private TextMeshProUGUI _bodyText;
        [SerializeField] private TextMeshProUGUI _countdownText;

        private const float FADE_IN       = 0.3f;
        private const int   COUNTDOWN_SEC = 10;   // 服务端在白天最后 10s 推送
        private const float FADE_OUT      = 0.3f;

        private Coroutine _runCoroutine;
        private bool      _subscribed = false;

        // ── 生命周期 ──────────────────────────────────────────────────────

        private void Start()
        {
            if (_bannerRoot != null) _bannerRoot.gameObject.SetActive(false);
            if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = 0f;
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
            sgm.OnDayPreview   += HandleDayPreview;
            sgm.OnPhaseChanged += HandlePhaseChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnDayPreview   -= HandleDayPreview;
                sgm.OnPhaseChanged -= HandlePhaseChanged;
            }
            _subscribed = false;
        }

        // ── 事件回调 ──────────────────────────────────────────────────────

        private void HandleDayPreview(DayPreviewData data)
        {
            if (data == null) return;
            if (_runCoroutine != null) StopCoroutine(_runCoroutine);
            _runCoroutine = StartCoroutine(RunBanner(data));
        }

        private void HandlePhaseChanged(PhaseChangedData data)
        {
            // 夜晚开始立即隐藏（即使倒计时未走完也收到 phase_changed 应该结束）
            if (data != null && data.phase == "night")
                HideImmediately();
        }

        private void HideImmediately()
        {
            if (_runCoroutine != null) { StopCoroutine(_runCoroutine); _runCoroutine = null; }
            if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = 0f;
            if (_bannerRoot != null) _bannerRoot.gameObject.SetActive(false);
        }

        private IEnumerator RunBanner(DayPreviewData data)
        {
            // 填充文案
            string modifierName = (data.nightModifier != null && !string.IsNullOrEmpty(data.nightModifier.name))
                ? data.nightModifier.name
                : "普通夜晚";
            string modifierDesc = (data.nightModifier != null && !string.IsNullOrEmpty(data.nightModifier.description))
                ? data.nightModifier.description
                : "无";

            if (_headlineText != null) _headlineText.text = $"今夜预报：{modifierName}";
            if (_bodyText != null)
                _bodyText.text = $"Boss HP：{data.bossHp}  预计怪物数：~{data.monsterCount}\n特殊效果：{modifierDesc}";

            if (_bannerRoot != null) _bannerRoot.gameObject.SetActive(true);

            // 淡入
            float t = 0f;
            while (t < FADE_IN)
            {
                t += Time.unscaledDeltaTime;
                if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = Mathf.Clamp01(t / FADE_IN);
                yield return null;
            }
            if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = 1f;

            // 倒计时（每秒刷新 countdownText）
            for (int remaining = COUNTDOWN_SEC; remaining > 0; remaining--)
            {
                if (_countdownText != null) _countdownText.text = $"倒计时：{remaining}s";
                yield return new WaitForSecondsRealtime(1f);
            }
            if (_countdownText != null) _countdownText.text = "倒计时：0s";

            // 淡出
            t = 0f;
            while (t < FADE_OUT)
            {
                t += Time.unscaledDeltaTime;
                if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = 1f - Mathf.Clamp01(t / FADE_OUT);
                yield return null;
            }
            if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = 0f;
            if (_bannerRoot != null) _bannerRoot.gameObject.SetActive(false);
            _runCoroutine = null;
        }
    }
}
