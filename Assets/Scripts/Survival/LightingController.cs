using UnityEngine;
using System.Collections;

namespace DrscfZ.Survival
{
    /// <summary>
    /// 昼夜灯光控制器
    /// 挂载到 [SurvivalManagers]/SurvivalGameManager（always active）
    /// 订阅 DayNightCycleManager.OnDayStarted / OnNightStarted，平滑过渡灯光
    /// </summary>
    public class LightingController : MonoBehaviour
    {
        [Header("灯光引用")]
        [SerializeField] private Light _mainLight;
        [SerializeField] private Light _fillLight;
        [SerializeField] private Light _rimLight;

        [Header("白天参数（冷白光）")]
        [SerializeField] private Color _dayColor = new Color(0.831f, 0.910f, 1f); // #D4E8FF
        [SerializeField] private float _dayIntensity = 1.3f;
        [SerializeField] private float _dayFillIntensity = 0.35f;
        [SerializeField] private float _dayRimIntensity = 0.25f;

        [Header("夜晚参数（深蓝月光）")]
        [SerializeField] private Color _nightColor = new Color(0.118f, 0.227f, 0.373f); // #1E3A5F
        [SerializeField] private float _nightIntensity = 0.4f;
        [SerializeField] private float _nightFillIntensity = 0.1f;
        [SerializeField] private float _nightRimIntensity = 0.08f;

        [Header("过渡时长")]
        [SerializeField] private float _transitionDuration = 2f;

        private Coroutine _transitionCoroutine;

        private void Start()
        {
            // 自动查找灯光（如果未在Inspector中指定）
            if (_mainLight == null)
            {
                var mainLightGO = GameObject.Find("Main Light");
                if (mainLightGO != null) _mainLight = mainLightGO.GetComponent<Light>();
                // 备用名称
                if (_mainLight == null)
                {
                    var lights = FindObjectsOfType<Light>();
                    foreach (var l in lights)
                    {
                        if (l.type == LightType.Directional && l.name.ToLower().Contains("main"))
                        {
                            _mainLight = l;
                            break;
                        }
                    }
                    // 最后兜底：取第一个 Directional
                    if (_mainLight == null)
                    {
                        foreach (var l in lights)
                        {
                            if (l.type == LightType.Directional) { _mainLight = l; break; }
                        }
                    }
                }
            }
            if (_fillLight == null)
            {
                var fillGO = GameObject.Find("Fill Light");
                if (fillGO != null) _fillLight = fillGO.GetComponent<Light>();
            }
            if (_rimLight == null)
            {
                var rimGO = GameObject.Find("Rim Light");
                if (rimGO != null) _rimLight = rimGO.GetComponent<Light>();
            }

            // 订阅昼夜事件（DayNightCycleManager 使用 OnDayStarted/OnNightStarted，参数是 int day）
            var dnm = DayNightCycleManager.Instance;
            if (dnm != null)
            {
                dnm.OnDayStarted  += HandleDayStarted;
                dnm.OnNightStarted += HandleNightStarted;
                // 设置初始为白天
                ApplyLighting(_dayColor, _dayIntensity, _dayFillIntensity, _dayRimIntensity);
            }
            else
            {
                Debug.LogWarning("[LightingController] DayNightCycleManager.Instance 为空，将在 Update 中重试订阅");
            }
        }

        private void Update()
        {
            // 如果 Start 时 Instance 还未就绪，延迟订阅
            if (!_subscribed && DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.OnDayStarted   += HandleDayStarted;
                DayNightCycleManager.Instance.OnNightStarted += HandleNightStarted;
                ApplyLighting(_dayColor, _dayIntensity, _dayFillIntensity, _dayRimIntensity);
                _subscribed = true;
            }
        }

        private bool _subscribed = false;

        private void OnDestroy()
        {
            var dnm = DayNightCycleManager.Instance;
            if (dnm != null)
            {
                dnm.OnDayStarted   -= HandleDayStarted;
                dnm.OnNightStarted -= HandleNightStarted;
            }
        }

        private void HandleDayStarted(int day)
        {
            if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = StartCoroutine(TransitionLighting(false));
        }

        private void HandleNightStarted(int day)
        {
            if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = StartCoroutine(TransitionLighting(true));
        }

        private IEnumerator TransitionLighting(bool toNight)
        {
            Color targetColor = toNight ? _nightColor : _dayColor;
            float targetMain  = toNight ? _nightIntensity      : _dayIntensity;
            float targetFill  = toNight ? _nightFillIntensity  : _dayFillIntensity;
            float targetRim   = toNight ? _nightRimIntensity   : _dayRimIntensity;

            Color startColor = _mainLight != null ? _mainLight.color     : targetColor;
            float startMain  = _mainLight != null ? _mainLight.intensity : targetMain;
            float startFill  = _fillLight != null ? _fillLight.intensity : targetFill;
            float startRim   = _rimLight  != null ? _rimLight.intensity  : targetRim;

            float t = 0;
            while (t < _transitionDuration)
            {
                t += Time.deltaTime;
                float ratio = Mathf.Clamp01(t / _transitionDuration);
                ApplyLighting(
                    Color.Lerp(startColor, targetColor, ratio),
                    Mathf.Lerp(startMain,  targetMain,  ratio),
                    Mathf.Lerp(startFill,  targetFill,  ratio),
                    Mathf.Lerp(startRim,   targetRim,   ratio)
                );
                yield return null;
            }
            ApplyLighting(targetColor, targetMain, targetFill, targetRim);
        }

        private void ApplyLighting(Color color, float mainIntensity, float fillIntensity, float rimIntensity)
        {
            if (_mainLight != null) { _mainLight.color = color; _mainLight.intensity = mainIntensity; }
            if (_fillLight != null) _fillLight.intensity = fillIntensity;
            if (_rimLight  != null) _rimLight.intensity  = rimIntensity;
        }
    }
}
