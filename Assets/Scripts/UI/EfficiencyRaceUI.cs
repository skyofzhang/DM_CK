using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §34 B10a 安全期效率竞赛横幅 —— 顶部滚动文字 Top2 PK
    ///
    /// 订阅 SurvivalGameManager.OnEfficiencyRace（服务端仅在 tension &lt; 30 时推送）。
    /// 收到消息即显示：
    ///   "食物采集王：{name1}({c1}) vs {name2}({c2})"
    ///
    /// 策划案 5175-5181 要求：
    ///   - 白天 tension &lt; 30 的"安全期"每 15s 循环展示；
    ///   - 仅展示，不参与决策；
    ///   - 在平静期创造社交比较，避免"死区"。
    ///
    /// 可视层级：顶部位置但低于 §34C TensionOverlay（全屏覆盖），高于 §34D ChapterAnnouncement（中部）。
    ///   由 Sibling Index 决定渲染顺序，Editor 脚本建时 SetAsFirstSibling 确保位于底层。
    ///
    /// 挂载规则（CLAUDE.md #7）：挂 Canvas/EfficiencyRaceBanner（常驻激活），Awake 只对 _bannerRoot 子节点 SetActive(false)。
    ///
    /// 可见性规则：
    ///   - 收到 efficiency_race 且 top3.Length ≥ 2 → 展示 12s（淡入 0.4 → 停留 11.2 → 淡出 0.4）；
    ///   - 收到 phase_changed.phase='night' → 立即隐藏（安全期已结束）；
    ///   - 服务端已保证只在 tension &lt; 30 时推送，客户端不做 tension 二次判定。
    ///
    /// Inspector 必填：
    ///   _bannerRoot        — 横幅根节点（子节点，初始 inactive）
    ///   _bannerCanvasGroup — CanvasGroup 淡入淡出
    ///   _messageText       — 滚动文字 TMP
    /// </summary>
    public class EfficiencyRaceUI : MonoBehaviour
    {
        [Header("横幅根节点")]
        [SerializeField] private RectTransform   _bannerRoot;
        [SerializeField] private CanvasGroup     _bannerCanvasGroup;

        [Header("文字")]
        [SerializeField] private TextMeshProUGUI _messageText;

        private const float FADE_IN  = 0.4f;
        private const float HOLD     = 11.2f;  // 12s 窗口 - 0.4 淡入 - 0.4 淡出
        private const float FADE_OUT = 0.4f;

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
            sgm.OnEfficiencyRace += HandleEfficiencyRace;
            sgm.OnPhaseChanged   += HandlePhaseChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnEfficiencyRace -= HandleEfficiencyRace;
                sgm.OnPhaseChanged   -= HandlePhaseChanged;
            }
            _subscribed = false;
        }

        // ── 事件回调 ──────────────────────────────────────────────────────

        private void HandleEfficiencyRace(EfficiencyRaceData data)
        {
            if (data == null || data.top3 == null || data.top3.Length < 2)
            {
                // 数据不足：Top2 无法 PK，忽略
                return;
            }

            var e1 = data.top3[0];
            var e2 = data.top3[1];
            if (e1 == null || e2 == null) return;

            string n1 = string.IsNullOrEmpty(e1.playerName) ? "匿名" : e1.playerName;
            string n2 = string.IsNullOrEmpty(e2.playerName) ? "匿名" : e2.playerName;

            // 策划案 5173：滚动文字"食物采集王：XXX(32) vs YYY(28)"
            string msg = $"食物采集王：{n1}({e1.contribution}) vs {n2}({e2.contribution})";

            if (_runCoroutine != null) StopCoroutine(_runCoroutine);
            _runCoroutine = StartCoroutine(RunBanner(msg));
        }

        private void HandlePhaseChanged(PhaseChangedData data)
        {
            // 夜晚立即隐藏（效率竞赛仅在安全期展示）
            if (data != null && data.phase == "night")
                HideImmediately();
        }

        private void HideImmediately()
        {
            if (_runCoroutine != null) { StopCoroutine(_runCoroutine); _runCoroutine = null; }
            if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = 0f;
            if (_bannerRoot != null) _bannerRoot.gameObject.SetActive(false);
        }

        private IEnumerator RunBanner(string msg)
        {
            if (_messageText != null) _messageText.text = msg;
            if (_bannerRoot != null) _bannerRoot.gameObject.SetActive(true);

            // Fade in
            float t = 0f;
            while (t < FADE_IN)
            {
                t += Time.unscaledDeltaTime;
                if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = Mathf.Clamp01(t / FADE_IN);
                yield return null;
            }
            if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = 1f;

            // Hold
            yield return new WaitForSecondsRealtime(HOLD);

            // Fade out
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
