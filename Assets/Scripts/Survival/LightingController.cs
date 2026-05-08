using UnityEngine;
using System.Collections;

namespace DrscfZ.Survival
{
    /// <summary>
    /// Day/night lighting controller. It retries subscription because manager
    /// creation order can vary between scene and runtime fallback paths.
    /// </summary>
    public class LightingController : MonoBehaviour
    {
        [Header("Lights")]
        [SerializeField] private Light _mainLight;
        [SerializeField] private Light _fillLight;
        [SerializeField] private Light _rimLight;

        [Header("Day")]
        [SerializeField] private Color _dayColor = new Color(0.831f, 0.910f, 1f); // #D4E8FF
        [SerializeField] private float _dayIntensity = 1.3f;
        [SerializeField] private float _dayFillIntensity = 0.35f;
        [SerializeField] private float _dayRimIntensity = 0.25f;

        [Header("Night")]
        [SerializeField] private Color _nightColor = new Color(0.118f, 0.227f, 0.373f); // #1E3A5F
        [SerializeField] private float _nightIntensity = 0.4f;
        [SerializeField] private float _nightFillIntensity = 0.1f;
        [SerializeField] private float _nightRimIntensity = 0.08f;

        [Header("Transition")]
        [SerializeField] private float _transitionDuration = 2f;

        private Coroutine _transitionCoroutine;
        private DayNightCycleManager _subscribedManager;
        private bool _subscribed;

        private void Start()
        {
            ResolveLights();
            if (!TrySubscribeDayNight())
                Debug.LogWarning("[LightingController] DayNightCycleManager.Instance is null; retrying in Update.");
        }

        private void Update()
        {
            TrySubscribeDayNight();
        }

        private void OnDestroy()
        {
            if (_subscribedManager != null)
            {
                _subscribedManager.OnDayStarted -= HandleDayStarted;
                _subscribedManager.OnNightStarted -= HandleNightStarted;
            }
            _subscribedManager = null;
            _subscribed = false;
        }

        private void ResolveLights()
        {
            if (_mainLight == null)
            {
                var mainLightGO = GameObject.Find("Main Light");
                if (mainLightGO != null) _mainLight = mainLightGO.GetComponent<Light>();

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

                    if (_mainLight == null)
                    {
                        foreach (var l in lights)
                        {
                            if (l.type == LightType.Directional)
                            {
                                _mainLight = l;
                                break;
                            }
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
        }

        private bool TrySubscribeDayNight()
        {
            if (_subscribed) return true;
            var dnm = DayNightCycleManager.Instance;
            if (dnm == null) return false;

            dnm.OnDayStarted += HandleDayStarted;
            dnm.OnNightStarted += HandleNightStarted;
            _subscribedManager = dnm;
            _subscribed = true;
            ApplyLighting(_dayColor, _dayIntensity, _dayFillIntensity, _dayRimIntensity);
            return true;
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
            float targetMain = toNight ? _nightIntensity : _dayIntensity;
            float targetFill = toNight ? _nightFillIntensity : _dayFillIntensity;
            float targetRim = toNight ? _nightRimIntensity : _dayRimIntensity;

            Color startColor = _mainLight != null ? _mainLight.color : targetColor;
            float startMain = _mainLight != null ? _mainLight.intensity : targetMain;
            float startFill = _fillLight != null ? _fillLight.intensity : targetFill;
            float startRim = _rimLight != null ? _rimLight.intensity : targetRim;

            float t = 0;
            while (t < _transitionDuration)
            {
                t += Time.deltaTime;
                float ratio = Mathf.Clamp01(t / _transitionDuration);
                ApplyLighting(
                    Color.Lerp(startColor, targetColor, ratio),
                    Mathf.Lerp(startMain, targetMain, ratio),
                    Mathf.Lerp(startFill, targetFill, ratio),
                    Mathf.Lerp(startRim, targetRim, ratio)
                );
                yield return null;
            }
            ApplyLighting(targetColor, targetMain, targetFill, targetRim);
        }

        private void ApplyLighting(Color color, float mainIntensity, float fillIntensity, float rimIntensity)
        {
            if (_mainLight != null)
            {
                _mainLight.color = color;
                _mainLight.intensity = mainIntensity;
            }
            if (_fillLight != null) _fillLight.intensity = fillIntensity;
            if (_rimLight != null) _rimLight.intensity = rimIntensity;
        }
    }
}
