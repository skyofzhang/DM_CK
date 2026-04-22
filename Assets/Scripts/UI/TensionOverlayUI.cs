using UnityEngine;
using UnityEngine.UI;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §34.4 E1 危机感知系统 —— 全屏 Tension Overlay
    ///
    /// 订阅 SurvivalGameManager.OnResourceUpdate，从 ResourceUpdateData.tension (0-100) 驱动屏幕边缘的
    /// 颜色/alpha/脉冲反馈。四级分段（策划案 5237-5242）：
    ///   0-30   安全: 隐藏
    ///   31-60  紧张: 淡黄暗角，缓慢呼吸（2.0s）
    ///   61-80  危急: 橙红脉冲，心跳（0.8s）
    ///   81-100 濒死: 深红剧烈脉冲（0.4s）
    ///
    /// 挂载规则（CLAUDE.md #7）：
    ///   挂 Canvas 下层（先于游戏画面，后于 UI 面板），GO 常驻激活。
    ///   Awake 内不 SetActive(false)；alpha=0 即可隐藏，保证 OnEnable 事件订阅不被阻断。
    ///
    /// Inspector 必填：
    ///   _overlayImage — 全屏 Image（铺满屏幕，颜色由脚本控制）
    /// </summary>
    public class TensionOverlayUI : MonoBehaviour
    {
        [Header("全屏覆盖 Image（铺满屏幕）")]
        [SerializeField] private Image _overlayImage;

        // ── 四级视觉参数（对应策划案 5237-5242）────────────────────────────

        // 0-30: 安全（隐藏）
        private const int LEVEL_SAFE_MAX = 30;

        // 31-60: 紧张（淡黄暗角，呼吸 2.0s）
        private const int   LEVEL_TENSE_MAX    = 60;
        private static readonly Color TENSE_COLOR   = new Color(1.00f, 0.85f, 0.40f, 0.12f);
        private const float TENSE_PERIOD   = 2.0f;
        private const float TENSE_AMPLITUDE = 0.30f;   // 呼吸 alpha 波动比例（base × (1 ± amp × sin)）

        // 61-80: 危急（橙红脉冲，心跳 0.8s）
        private const int   LEVEL_CRITICAL_MAX = 80;
        private static readonly Color CRITICAL_COLOR = new Color(1.00f, 0.35f, 0.10f, 0.25f);
        private const float CRITICAL_PERIOD   = 0.8f;
        private const float CRITICAL_AMPLITUDE = 0.50f;

        // 81-100: 濒死（深红剧烈脉冲 0.4s）
        private static readonly Color DYING_COLOR = new Color(0.80f, 0.00f, 0.00f, 0.40f);
        private const float DYING_PERIOD   = 0.4f;
        private const float DYING_AMPLITUDE = 0.70f;

        // ── 内部状态 ──────────────────────────────────────────────────────

        private int   _currentTension = 0;
        private bool  _subscribed     = false;

        // ── 生命周期 ──────────────────────────────────────────────────────

        private void Start()
        {
            EnsureOverlay();
            TrySubscribe();
        }

        private void OnEnable()  { TrySubscribe(); }
        private void OnDisable() { Unsubscribe(); }
        private void OnDestroy() { Unsubscribe(); }

        private void Update()
        {
            if (!_subscribed) TrySubscribe();
            UpdateVisual();
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;
            sgm.OnResourceUpdate += HandleResourceUpdate;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null) sgm.OnResourceUpdate -= HandleResourceUpdate;
            _subscribed = false;
        }

        // ── 事件回调 ──────────────────────────────────────────────────────

        private void HandleResourceUpdate(ResourceUpdateData data)
        {
            if (data == null) return;
            _currentTension = Mathf.Clamp(data.tension, 0, 100);
        }

        // ── 视觉刷新 ──────────────────────────────────────────────────────

        private void EnsureOverlay()
        {
            if (_overlayImage == null)
            {
                _overlayImage = GetComponent<Image>();
            }
            if (_overlayImage == null)
            {
                Debug.LogWarning("[TensionOverlayUI] _overlayImage 未绑定，无法渲染。");
                return;
            }
            _overlayImage.raycastTarget = false;
            // 初始隐藏（alpha=0，而不是 SetActive(false)，避免断订阅）
            var c = _overlayImage.color;
            c.a = 0f;
            _overlayImage.color = c;
        }

        private void UpdateVisual()
        {
            if (_overlayImage == null) return;

            // 0-30 安全：完全隐藏
            if (_currentTension <= LEVEL_SAFE_MAX)
            {
                ApplyColorAlpha(Color.black, 0f);
                return;
            }

            Color baseColor;
            float period;
            float amplitude;

            if (_currentTension <= LEVEL_TENSE_MAX)
            {
                // 31-60 紧张
                baseColor = TENSE_COLOR;
                period    = TENSE_PERIOD;
                amplitude = TENSE_AMPLITUDE;
            }
            else if (_currentTension <= LEVEL_CRITICAL_MAX)
            {
                // 61-80 危急
                baseColor = CRITICAL_COLOR;
                period    = CRITICAL_PERIOD;
                amplitude = CRITICAL_AMPLITUDE;
            }
            else
            {
                // 81-100 濒死
                baseColor = DYING_COLOR;
                period    = DYING_PERIOD;
                amplitude = DYING_AMPLITUDE;
            }

            // 手动脉冲动画：base.a × (1 + amp × sin(2πt/period))
            // 使用 Time.unscaledTime 避免游戏暂停影响（UI 反馈始终流畅）
            float phase  = (Time.unscaledTime * Mathf.PI * 2f) / period;
            float pulse  = Mathf.Sin(phase);
            float alpha  = baseColor.a * (1f + amplitude * pulse);
            alpha        = Mathf.Clamp01(alpha);

            ApplyColorAlpha(baseColor, alpha);
        }

        private void ApplyColorAlpha(Color baseColor, float alpha)
        {
            var c = _overlayImage.color;
            c.r = baseColor.r;
            c.g = baseColor.g;
            c.b = baseColor.b;
            c.a = alpha;
            _overlayImage.color = c;
        }
    }
}
