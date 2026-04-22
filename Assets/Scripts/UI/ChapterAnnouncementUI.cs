using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §34.4 E2 叙事节奏 —— 幕切换全屏公告
    ///
    /// 订阅 SurvivalGameManager.OnChapterChanged，收到 chapter_changed 消息时全屏显示幕名 2s。
    /// 映射到赛季日范围（§36 v1.27 永续赛季制）：
    ///   prologue (D1)       → "序章·踏入极地"
    ///   act1     (D2-3)     → "第一幕·资源争夺"
    ///   act2     (D4-5)     → "第二幕·暗夜降临"
    ///   act3     (D6)       → "第三幕·最后防线"
    ///   finale   (D7)       → "终章·黎明之前"
    ///
    /// 视觉：屏幕正中，TMP 字号 80+，金色字（1, 0.9, 0.5）；淡入 0.4s → 停留 1.2s → 淡出 0.4s，总 2s。
    ///
    /// 挂载规则（CLAUDE.md #7）：
    ///   挂 Canvas/ChapterAnnouncement（常驻激活），Awake 内只对 _bannerRoot 子节点 SetActive(false)；
    ///   脚本本身 GO 始终 active 保证 OnEnable 订阅生效。
    ///
    /// Inspector 必填：
    ///   _bannerRoot        — 横幅根节点（子节点，初始 inactive）
    ///   _bannerCanvasGroup — CanvasGroup 控制淡入淡出
    ///   _nameText          — 幕名主文字 TMP（字号 80+）
    ///   _subText           — 幕范围副标题 TMP（字号 28，可选；若 null 则不显示副标题）
    /// </summary>
    public class ChapterAnnouncementUI : MonoBehaviour
    {
        [Header("横幅根节点")]
        [SerializeField] private RectTransform   _bannerRoot;
        [SerializeField] private CanvasGroup     _bannerCanvasGroup;

        [Header("文字")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _subText;

        private const float FADE_IN  = 0.4f;
        private const float HOLD     = 1.2f;
        private const float FADE_OUT = 0.4f;

        private Coroutine _runCoroutine;
        private bool      _subscribed = false;

        // ── 生命周期 ──────────────────────────────────────────────────────

        private void Start()
        {
            // 仅对子节点 _bannerRoot 执行 SetActive(false)（CLAUDE.md 规则 6：禁 Awake SetActive(false) 自身 GO）
            if (_bannerRoot != null) _bannerRoot.gameObject.SetActive(false);
            if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = 0f;
            TrySubscribe();
        }

        private void OnEnable()  { TrySubscribe(); }
        private void OnDisable() { Unsubscribe(); }
        private void OnDestroy() { Unsubscribe(); }

        private void Update()
        {
            if (!_subscribed) TrySubscribe();
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;
            sgm.OnChapterChanged += HandleChapterChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null) sgm.OnChapterChanged -= HandleChapterChanged;
            _subscribed = false;
        }

        // ── 事件回调 ──────────────────────────────────────────────────────

        private void HandleChapterChanged(ChapterChangedData data)
        {
            if (data == null || string.IsNullOrEmpty(data.name)) return;
            if (_runCoroutine != null) StopCoroutine(_runCoroutine);
            _runCoroutine = StartCoroutine(RunAnnouncement(data));
        }

        private IEnumerator RunAnnouncement(ChapterChangedData data)
        {
            if (_nameText != null) _nameText.text = data.name;
            if (_subText  != null)
            {
                _subText.text = (data.startDay > 0 && data.endDay > 0)
                    ? $"第 {data.startDay} 天 — 第 {data.endDay} 天"
                    : "";
            }

            if (_bannerRoot != null) _bannerRoot.gameObject.SetActive(true);

            // Fade in
            float t = 0f;
            while (t < FADE_IN)
            {
                t += Time.unscaledDeltaTime;
                if (_bannerCanvasGroup != null)
                    _bannerCanvasGroup.alpha = Mathf.Clamp01(t / FADE_IN);
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
                if (_bannerCanvasGroup != null)
                    _bannerCanvasGroup.alpha = 1f - Mathf.Clamp01(t / FADE_OUT);
                yield return null;
            }
            if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = 0f;

            if (_bannerRoot != null) _bannerRoot.gameObject.SetActive(false);
            _runCoroutine = null;
        }
    }
}
