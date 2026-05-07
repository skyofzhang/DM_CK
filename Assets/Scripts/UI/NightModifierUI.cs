using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §34.4 E6 夜间修饰符系统 —— 7 种变体名称公告
    ///
    /// 订阅 SurvivalGameManager.OnPhaseChanged，从 PhaseChangedData.nightModifier 驱动：
    ///   仅 phase="night" 且 nightModifier != null 时全屏显示 2s（淡入 0.4 → 停留 1.2 → 淡出 0.4）。
    ///
    /// 7 种修饰符（策划案 5406-5414）：
    ///   normal / blood_moon / polar_night / fortified / frenzy / hunters / blizzard_night
    /// 主标题：修饰符 name（血月/极夜/坚守之夜/…）
    /// 副标题：修饰符 description（"单 Boss HP x3，击杀贡献 x1.5" 等）
    ///
    /// 视觉设计（策划案 5420-5425）：光照变化走 LightingController；本 UI 只负责文本公告。
    ///
    /// 与 §36 peace_night 变体的兼容性：
    ///   phase_changed 携带 nightModifier 只在 variant != peace_night* 的普通夜晚；
    ///   和平夜 / prelude 由 PeaceNightOverlay 专门处理，不会触发 NightModifierUI（因为 nightModifier=null）。
    ///
    /// 挂载规则：挂 Canvas/NightModifierBanner（常驻激活），Awake 仅对 _bannerRoot 子节点 SetActive(false)。
    ///
    /// Inspector 必填：
    ///   _bannerRoot        — 横幅根节点（子节点，初始 inactive）
    ///   _bannerCanvasGroup — CanvasGroup
    ///   _nameText          — 修饰符名 TMP（大字）
    ///   _descText          — 修饰符描述 TMP（副标题）
    /// </summary>
    public class NightModifierUI : MonoBehaviour
    {
        [Header("横幅根节点")]
        [SerializeField] private RectTransform   _bannerRoot;
        [SerializeField] private CanvasGroup     _bannerCanvasGroup;

        [Header("文字")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _descText;

        private const float FADE_IN  = 0.4f;
        private const float HOLD     = 1.2f;
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
            sgm.OnPhaseChanged += HandlePhaseChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null) sgm.OnPhaseChanged -= HandlePhaseChanged;
            _subscribed = false;
        }

        // ── 事件回调 ──────────────────────────────────────────────────────

        private void HandlePhaseChanged(PhaseChangedData data)
        {
            if (data == null) return;
            if (SurvivalGameManager.Instance == null ||
                SurvivalGameManager.Instance.State != SurvivalGameManager.SurvivalState.Running)
            {
                HideImmediately();
                return;
            }

            // 仅 phase=night 且 nightModifier != null 时显示；切换到白天立即隐藏残留
            if (data.phase != "night")
            {
                HideImmediately();
                return;
            }
            if (data.nightModifier == null || string.IsNullOrEmpty(data.nightModifier.name))
            {
                // normal 变体可能省略 nightModifier 或 id=normal，不显示公告
                return;
            }
            // normal 修饰符不显示（避免每晚都弹横幅）
            if (data.nightModifier.id == "normal") return;

            if (_runCoroutine != null) StopCoroutine(_runCoroutine);
            _runCoroutine = StartCoroutine(RunAnnouncement(data.nightModifier));
        }

        private void HideImmediately()
        {
            if (_runCoroutine != null) { StopCoroutine(_runCoroutine); _runCoroutine = null; }
            if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = 0f;
            if (_bannerRoot != null) _bannerRoot.gameObject.SetActive(false);
        }

        private IEnumerator RunAnnouncement(NightModifierData modifier)
        {
            if (_nameText != null) _nameText.text = modifier.name;
            if (_descText != null) _descText.text = modifier.description ?? "";

            if (_bannerRoot != null) _bannerRoot.gameObject.SetActive(true);

            float t = 0f;
            while (t < FADE_IN)
            {
                t += Time.unscaledDeltaTime;
                if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = Mathf.Clamp01(t / FADE_IN);
                yield return null;
            }
            if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = 1f;

            yield return new WaitForSecondsRealtime(HOLD);

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
