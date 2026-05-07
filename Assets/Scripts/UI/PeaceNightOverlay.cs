using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DrscfZ.Core;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §36.5 和平夜柔光罩（MVP 极简版）。
    ///
    /// 触发时机（订阅 SurvivalGameManager.OnPhaseChanged，phase='night' 时）：
    ///   - variant='peace_night'         → 显示柔光 + 文本（D1 整夜，持续整夜）
    ///   - variant='peace_night_prelude' → 显示柔光 + 文本 + 小倒计时（D3 前 30s），
    ///     基于 peacePreludeEndsAt 驱动倒计时，到时自动隐藏
    ///   - variant='peace_night_silent'  → 不显示 UI（D2 无怪但保持紧张感，清除残留）
    ///   - 其它 variant 或 phase='day'    → 隐藏
    ///
    /// 挂载：Canvas/GameUIPanel（常驻）；Prefab 绑定留给人工（_overlayRoot/_hintText/_countdownText/_overlayImage）。
    /// 若 Inspector 字段未绑定，降级为 Debug.Log（不走 AnnouncementUI，避免横幅频繁打扰）。
    ///
    /// 设计原则（CLAUDE.md 规则 6）：Awake 不对自身 GO 执行 SetActive(false)；
    /// 通过 _overlayRoot 子节点 SetActive 控制显隐。
    /// </summary>
    public class PeaceNightOverlay : MonoBehaviour
    {
        public static PeaceNightOverlay Instance { get; private set; }

        [Header("柔光罩根节点（子节点，初始 inactive）")]
        [SerializeField] private GameObject _overlayRoot;

        [Header("柔光 Image（fade-in/out 控制 alpha；也可留空只用文本）")]
        [SerializeField] private Image _overlayImage;

        [Header("文本字段")]
        [SerializeField] private TMP_Text _hintText;         // "敌人尚未察觉你的存在"
        [SerializeField] private TMP_Text _countdownText;    // 仅 prelude 显示；非 prelude 隐藏

        [Header("柔光峰值 alpha（overlay image 最终 alpha；默认 0.25，避免过度遮挡）")]
        [SerializeField] private float _peakAlpha = 0.25f;

        [Header("柔光 fadeIn 秒")]
        [SerializeField] private float _fadeInSec = 1.2f;

        [Header("柔光 fadeOut 秒")]
        [SerializeField] private float _fadeOutSec = 1.2f;

        // prelude 倒计时截止时刻（Unix ms）；0 表示非 prelude 模式
        private long _preludeEndsAtMs = 0;

        // 动画协程引用（切换 variant 时安全取消）
        private Coroutine _fadeCoroutine;
        private Coroutine _countdownCoroutine;

        // 当前 variant（用于去抖：多次推送相同 variant 时不重复 fadeIn）
        private string _currentVariant = "normal";

        private void Awake()
        {
            if (Instance != null && Instance != this) { return; }
            Instance = this;
            // ✅ 合法：对子节点 _overlayRoot 执行 SetActive
            if (_overlayRoot != null) _overlayRoot.SetActive(false);
            if (_countdownText != null) _countdownText.gameObject.SetActive(false);
            ApplyOverlayAlpha(0f);
        }

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void Start()
        {
            // Start() 再订阅一次（Awake 顺序若 SGM.Instance 尚未创建，此处兜底）
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

        /// <summary>订阅 SurvivalGameManager.OnPhaseChanged 的入口。</summary>
        private void HandlePhaseChanged(PhaseChangedData data)
        {
            if (data == null) return;
            if (SurvivalGameManager.Instance == null ||
                SurvivalGameManager.Instance.State != SurvivalGameManager.SurvivalState.Running)
            {
                HideOverlay();
                return;
            }

            string variant = string.IsNullOrEmpty(data.variant)
                ? SurvivalMessageProtocol.PhaseVariantNormal
                : data.variant;

            // phase='day' 或 recovery 等非夜晚变体 → 必隐藏
            if (data.phase != "night")
            {
                HideOverlay();
                _currentVariant = variant;
                return;
            }

            // 夜晚内部 variant 分流
            switch (variant)
            {
                case SurvivalMessageProtocol.PhaseVariantPeaceNight:
                    ShowOverlay("敌人尚未察觉你的存在", showCountdown: false, preludeEndsAt: 0);
                    break;

                case SurvivalMessageProtocol.PhaseVariantPeaceNightPrelude:
                    ShowOverlay("片刻宁静，准备迎战", showCountdown: true, preludeEndsAt: data.peacePreludeEndsAt);
                    break;

                case SurvivalMessageProtocol.PhaseVariantPeaceNightSilent:
                    // 无怪但不显示 UI（保持紧张感）；清除上一次可能残留的 overlay
                    HideOverlay();
                    break;

                default:
                    // normal / recovery / 未知 variant → 隐藏
                    HideOverlay();
                    break;
            }

            _currentVariant = variant;
            Debug.Log($"[PeaceNightOverlay] phase_changed: phase={data.phase} variant={variant} preludeEndsAt={data.peacePreludeEndsAt}");
        }

        /// <summary>显示柔光罩 + hint 文本；prelude 模式额外开倒计时。</summary>
        private void ShowOverlay(string hintText, bool showCountdown, long preludeEndsAt)
        {
            if (_overlayRoot == null)
            {
                Debug.Log($"[PeaceNightOverlay] (未绑定 _overlayRoot) hint='{hintText}' countdown={showCountdown}");
                return;
            }
            _overlayRoot.SetActive(true);

            if (_hintText != null) _hintText.text = hintText;

            if (_countdownText != null)
            {
                _countdownText.gameObject.SetActive(showCountdown);
            }

            _preludeEndsAtMs = showCountdown ? preludeEndsAt : 0;

            // 切换倒计时协程
            if (_countdownCoroutine != null) { StopCoroutine(_countdownCoroutine); _countdownCoroutine = null; }
            if (showCountdown && preludeEndsAt > 0 && _countdownText != null && gameObject.activeInHierarchy)
            {
                _countdownCoroutine = StartCoroutine(UpdateCountdown());
            }

            // fade in 柔光（若从隐藏切来）
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            if (gameObject.activeInHierarchy)
                _fadeCoroutine = StartCoroutine(FadeOverlay(_peakAlpha, _fadeInSec));
        }

        /// <summary>隐藏柔光罩 + 停止倒计时。</summary>
        private void HideOverlay()
        {
            if (_countdownCoroutine != null) { StopCoroutine(_countdownCoroutine); _countdownCoroutine = null; }
            _preludeEndsAtMs = 0;

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            if (gameObject.activeInHierarchy && _overlayRoot != null && _overlayRoot.activeSelf)
            {
                _fadeCoroutine = StartCoroutine(FadeOverlayThenDeactivate(0f, _fadeOutSec));
            }
            else
            {
                if (_overlayRoot != null) _overlayRoot.SetActive(false);
                ApplyOverlayAlpha(0f);
            }
        }

        private IEnumerator UpdateCountdown()
        {
            while (_preludeEndsAtMs > 0)
            {
                long nowMs = NetworkManager.SyncedNowMs;
                long remainingMs = _preludeEndsAtMs - nowMs;
                if (remainingMs <= 0)
                {
                    // 倒计时结束 → 自然隐藏
                    HideOverlay();
                    yield break;
                }
                int remainingSec = Mathf.CeilToInt(remainingMs / 1000f);
                if (_countdownText != null) _countdownText.text = $"{remainingSec}s";
                yield return new WaitForSeconds(0.2f);
            }
        }

        private IEnumerator FadeOverlay(float targetAlpha, float durationSec)
        {
            if (_overlayImage == null)
            {
                ApplyOverlayAlpha(targetAlpha);
                yield break;
            }
            float startAlpha = _overlayImage.color.a;
            float t = 0f;
            while (t < durationSec)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(t / Mathf.Max(0.01f, durationSec)));
                ApplyOverlayAlpha(a);
                yield return null;
            }
            ApplyOverlayAlpha(targetAlpha);
            _fadeCoroutine = null;
        }

        private IEnumerator FadeOverlayThenDeactivate(float targetAlpha, float durationSec)
        {
            yield return FadeOverlay(targetAlpha, durationSec);
            if (_overlayRoot != null) _overlayRoot.SetActive(false);
        }

        private void ApplyOverlayAlpha(float a)
        {
            if (_overlayImage == null) return;
            var c = _overlayImage.color;
            c.a = a;
            _overlayImage.color = c;
        }
    }
}
