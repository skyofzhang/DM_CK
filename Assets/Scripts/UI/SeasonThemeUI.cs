using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §36 赛季主题视觉 UI（MVP 极简版）。
    ///
    /// 主题视觉效果：MVP 仅切换一个 overlay（半透明横幅）的颜色；
    /// 不做粒子/天空盒/全屏滤镜（这些属 P2 内容打磨阶段，见策划案 §36.7）。
    ///
    /// 触发时机：
    ///   - season_started 推送 → ApplyTheme()
    ///   - season_state 推送且 themeId 变化 → ApplyTheme()
    ///
    /// 挂载：Canvas（常驻）；Prefab 绑定由人工（_themeOverlay + 6 色配置）。
    /// 若 _themeOverlay 未绑定，所有效果降级为 Debug.Log，不崩溃。
    /// </summary>
    public class SeasonThemeUI : MonoBehaviour
    {
        public static SeasonThemeUI Instance { get; private set; }

        [Header("主题 overlay（初始 inactive 或 alpha=0）")]
        [SerializeField] private Image _themeOverlay;

        [Header("6 套主题色（按 §36.3 顺序：classic_frozen/blood_moon/snowstorm/dawn/frenzy/serene）")]
        [SerializeField] private Color[] _themeColors = new Color[6]
        {
            new Color(0.60f, 0.85f, 1.00f, 0.25f), // classic_frozen
            new Color(1.00f, 0.20f, 0.30f, 0.25f), // blood_moon
            new Color(0.85f, 0.90f, 0.95f, 0.25f), // snowstorm
            new Color(1.00f, 0.65f, 0.30f, 0.25f), // dawn
            new Color(1.00f, 0.30f, 0.60f, 0.25f), // frenzy
            new Color(0.40f, 1.00f, 0.75f, 0.25f), // serene
        };

        [Header("动画时序（秒）")]
        [SerializeField] private float _fadeInSec  = 2f;
        [SerializeField] private float _holdSec    = 3f;
        [SerializeField] private float _fadeOutSec = 2f;

        /// <summary>当前生效的主题 id（无主题时为 null）。</summary>
        public string CurrentThemeId { get; private set; }

        private Coroutine _activeCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this) { return; }
            Instance = this;
            if (_themeOverlay != null)
            {
                var c = _themeOverlay.color;
                c.a = 0f;
                _themeOverlay.color = c;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>应用主题视觉：查 themeId 对应颜色 → fadeIn 2s → hold 3s → fadeOut 2s。</summary>
        public void ApplyTheme(string themeId)
        {
            CurrentThemeId = themeId;

            if (_themeOverlay == null)
            {
                Debug.Log($"[SeasonThemeUI] (未绑定 _themeOverlay) themeId={themeId}");
                return;
            }

            Color targetColor = ResolveThemeColor(themeId);

            if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
            if (gameObject.activeInHierarchy)
                _activeCoroutine = StartCoroutine(PlayOverlay(targetColor));
        }

        /// <summary>外部查询主题颜色（供 AnnouncementUI 横幅等复用）。</summary>
        public Color GetThemeColor(string themeId)
        {
            return ResolveThemeColor(themeId);
        }

        private Color ResolveThemeColor(string themeId)
        {
            if (string.IsNullOrEmpty(themeId)) return GetDefaultColor();
            int idx = ThemeIdToIndex(themeId);
            if (idx < 0 || _themeColors == null || idx >= _themeColors.Length) return GetDefaultColor();
            return _themeColors[idx];
        }

        private Color GetDefaultColor() => new Color(1f, 1f, 1f, 0.2f);

        /// <summary>§36.3 themeId → 数组索引（保持与 _themeColors 数组顺序一致）。</summary>
        public static int ThemeIdToIndex(string themeId)
        {
            switch (themeId)
            {
                case "classic_frozen": return 0;
                case "blood_moon":     return 1;
                case "snowstorm":      return 2;
                case "dawn":           return 3;
                case "frenzy":         return 4;
                case "serene":         return 5;
                default:               return -1;
            }
        }

        private IEnumerator PlayOverlay(Color targetColor)
        {
            // 设色（保留目标 alpha 作为峰值 alpha）
            float peakAlpha = targetColor.a;
            Color baseColor = targetColor;
            baseColor.a = 0f;
            _themeOverlay.color = baseColor;

            // fade in
            float t = 0f;
            while (t < _fadeInSec)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(0f, peakAlpha, Mathf.Clamp01(t / _fadeInSec));
                SetAlpha(a);
                yield return null;
            }
            SetAlpha(peakAlpha);

            // hold
            yield return new WaitForSeconds(_holdSec);

            // fade out
            t = 0f;
            while (t < _fadeOutSec)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(peakAlpha, 0f, Mathf.Clamp01(t / _fadeOutSec));
                SetAlpha(a);
                yield return null;
            }
            SetAlpha(0f);
            _activeCoroutine = null;
        }

        private void SetAlpha(float a)
        {
            if (_themeOverlay == null) return;
            var c = _themeOverlay.color;
            c.a = a;
            _themeOverlay.color = c;
        }
    }
}
