using UnityEngine;
using System.Collections;
using DrscfZ.Systems;

namespace DrscfZ.Survival
{
    /// <summary>
    /// 极地生存游戏相机控制器（M-CAM 系统）
    ///
    /// 功能：
    ///   1. 相机震屏（Shake）— 静态调用，供任意系统触发
    ///   2. 滚轮缩放（FOV 20°~80°）
    ///   3. 左键拖拽平移（锁定 Y 轴，_initialOffset 模式）
    ///   4. 平移范围限制（基于初始位置的 ±50% 范围）
    ///   5. Z 键重置相机到初始位置 + 初始旋转
    ///   6. 自动订阅游戏事件：夜晚/城门危机/礼物 T3+
    ///
    /// 挂载规则（Rule #7）：
    ///   挂在场景中的 Main Camera 上（Camera 始终激活）。
    ///   若场景中无 Camera 被赋值，自动 fallback 到 Camera.main。
    ///
    /// 静态调用示例：
    ///   SurvivalCameraController.Shake(0.3f, 0.5f);   // intensity, duration
    /// </summary>
    public class SurvivalCameraController : MonoBehaviour
    {
        public static SurvivalCameraController Instance { get; private set; }

        [Header("相机引用（留空自动使用 Camera.main）")]
        [SerializeField] private Camera _camera;

        [Header("震屏参数默认值")]
        [SerializeField] private float _defaultIntensity = 0.15f;
        [SerializeField] private float _defaultDuration  = 0.3f;
        [SerializeField] private float _shakeFrequency   = 20f;    // 每秒震动次数

        [Header("缩放参数")]
        [SerializeField] private float _zoomSpeed = 3f;            // 每格滚轮缩放量（度）
        [SerializeField] private float _minFov    = 20f;           // FOV 最小值
        [SerializeField] private float _maxFov    = 80f;           // FOV 最大值

        [Header("平移参数")]
        [SerializeField] private float _panSpeed = 0.3f;           // 平移灵敏度

        // ---- 初始状态（Start() 时读取，之后只读）----
        private Vector3    _initPos;      // 用户在 Inspector 设置的初始世界坐标
        private Quaternion _initRot;      // 用户在 Inspector 设置的初始旋转

        // ---- 平移偏移量（_initialOffset 模式，Rule #5）----
        private Vector3 _offset = Vector3.zero;

        // ---- 震屏内部状态 ----
        private Vector3   _originLocalPos;     // 震屏基准位置（相机 localPosition）
        private Coroutine _shakeCoroutine;
        private bool      _shaking = false;
        private float     _currentIntensity = 0f;

        // ==================== 生命周期 ====================

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            if (_camera == null)
                _camera = Camera.main;
        }

        private void Start()
        {
            // 记录用户在 Inspector 中设置的初始位置和旋转（Rule #3/#4/#5）
            _initPos = transform.position;
            _initRot = transform.rotation;
            _offset  = Vector3.zero;

            // 震屏基准使用 localPosition（相对父节点）
            if (_camera != null)
                _originLocalPos = _camera.transform.localPosition;

            SubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void Update()
        {
            HandleZoom();
            HandlePan();
            HandleReset();
        }

        // ==================== 输入处理 ====================

        /// <summary>滚轮缩放：调整 Camera.fieldOfView</summary>
        private void HandleZoom()
        {
            if (_camera == null) return;

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Approximately(scroll, 0f)) return;

            // 向上滚（scroll > 0）→ FOV 减小（放大）；向下滚（scroll < 0）→ FOV 增大（缩小）
            // 使用 Mathf.Sign 保证每格滚轮精确缩放 _zoomSpeed 度，不受鼠标驱动 scroll 幅度影响
            float newFov = _camera.fieldOfView - Mathf.Sign(scroll) * _zoomSpeed;
            _camera.fieldOfView = Mathf.Clamp(newFov, _minFov, _maxFov);
        }

        /// <summary>左键拖拽平移（_initialOffset 模式，锁定 Y 轴）</summary>
        private void HandlePan()
        {
            if (!Input.GetMouseButton(0)) return;

            float dx = -Input.GetAxis("Mouse X") * _panSpeed;
            float dz = -Input.GetAxis("Mouse Y") * _panSpeed;

            _offset.x += dx;
            _offset.z += dz;

            // 平移范围限制：基于初始位置 ±50%（Rule #5）
            float halfX = Mathf.Max(Mathf.Abs(_initPos.x) * 0.5f, 20f);
            float halfZ = Mathf.Max(Mathf.Abs(_initPos.z) * 0.5f, 20f);

            _offset.x = Mathf.Clamp(_offset.x, -halfX, halfX);
            _offset.z = Mathf.Clamp(_offset.z, -halfZ, halfZ);

            // 应用偏移：不覆盖 _initPos，通过 _offset 叠加（Rule #5）
            // 注意：震屏期间 ShakeCoroutine 修改 localPosition，
            // 这里修改 world position，两者通过不同坐标系叠加，互不干扰。
            if (!_shaking)
                transform.position = _initPos + _offset;
            else
                // 震屏时只更新记录值，等震屏结束后位置自动恢复到正确值
                _originLocalPos = _camera.transform.localPosition;
        }

        /// <summary>Z 键重置相机到初始位置和旋转</summary>
        private void HandleReset()
        {
            if (!Input.GetKeyDown(KeyCode.Z)) return;

            // 停止震屏
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                _shakeCoroutine = null;
                _shaking = false;
                _currentIntensity = 0f;
            }

            // 清空偏移，恢复到 Inspector 设置的原始状态
            _offset = Vector3.zero;
            transform.position = _initPos;
            transform.rotation = _initRot;

            // 重置震屏基准
            if (_camera != null)
                _originLocalPos = _camera.transform.localPosition;
        }

        // ==================== 事件订阅 ====================

        private void SubscribeEvents()
        {
            var dn = DayNightCycleManager.Instance;
            if (dn != null)
            {
                dn.OnNightStarted += OnNightStarted;
                dn.OnDayStarted   += OnDayStarted;
            }

            var gate = CityGateSystem.Instance;
            if (gate != null)
            {
                gate.OnHpChanged    += OnGateHpChanged;
                gate.OnGateBreached += OnGateBreached;
            }
        }

        private void UnsubscribeEvents()
        {
            var dn = DayNightCycleManager.Instance;
            if (dn != null)
            {
                dn.OnNightStarted -= OnNightStarted;
                dn.OnDayStarted   -= OnDayStarted;
            }

            var gate = CityGateSystem.Instance;
            if (gate != null)
            {
                gate.OnHpChanged    -= OnGateHpChanged;
                gate.OnGateBreached -= OnGateBreached;
            }
        }

        // ==================== 事件回调 ====================

        private void OnNightStarted(int day)
        {
            // 夜晚开始：中等震屏（压迫感）
            ShakeInternal(0.2f, 0.6f);
        }

        private void OnDayStarted(int day)
        {
            // 白天开始：轻微震屏（清爽感）
            ShakeInternal(0.05f, 0.2f);
        }

        private void OnGateHpChanged(int current, int max)
        {
            if (max <= 0) return;
            float ratio = (float)current / max;
            // 城门 HP 低于 30% 时每次受击轻微抖动
            if (ratio <= 0.3f && ratio > 0f)
                ShakeInternal(0.08f, 0.15f);
        }

        private void OnGateBreached()
        {
            // 城门沦陷：强烈震屏
            ShakeInternal(0.5f, 1.0f);
        }

        // ==================== 静态 API ====================

        /// <summary>
        /// 触发相机震屏（任意代码可调用）。
        /// 若当前震屏强度更高则不覆盖（高Tier礼物不被低Tier打断）。
        /// </summary>
        /// <param name="intensity">震屏强度（World Units），建议范围 0.05~0.5</param>
        /// <param name="duration">持续时间（秒）</param>
        public static void Shake(float intensity, float duration)
        {
            if (Instance != null)
                Instance.ShakeInternal(intensity, duration);
        }

        // ==================== 内部实现 ====================

        private void ShakeInternal(float intensity, float duration)
        {
            if (_camera == null) return;

            // 若当前震屏更强，不打断（等它结束）
            if (_shaking && _currentIntensity >= intensity) return;

            if (_shakeCoroutine != null)
                StopCoroutine(_shakeCoroutine);

            _shakeCoroutine = StartCoroutine(ShakeCoroutine(intensity, duration));
        }

        private IEnumerator ShakeCoroutine(float intensity, float duration)
        {
            _shaking = true;
            _currentIntensity = intensity;

            float elapsed = 0f;
            float halfDuration = duration * 0.5f;

            // 震屏基准：当前 localPosition（包含平移偏移后的结果）
            _originLocalPos = _camera.transform.localPosition;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                // 强度随时间衰减（前半程保持，后半程衰减）
                float decay = elapsed < halfDuration ? 1f
                    : 1f - (elapsed - halfDuration) / halfDuration;

                float currentIntensity = intensity * decay;

                // 使用 Perlin Noise 生成平滑震屏（比随机数更自然）
                float x = (Mathf.PerlinNoise(elapsed * _shakeFrequency, 0f) - 0.5f) * 2f * currentIntensity;
                float y = (Mathf.PerlinNoise(0f, elapsed * _shakeFrequency) - 0.5f) * 2f * currentIntensity;

                _camera.transform.localPosition = _originLocalPos + new Vector3(x, y, 0f);
                yield return null;
            }

            // 恢复震屏前的基准位置（平移偏移依然生效）
            _camera.transform.localPosition = _originLocalPos;
            _shaking = false;
            _currentIntensity = 0f;
            _shakeCoroutine = null;
        }

        // ==================== 工具：外部触发怪物攻击震屏 ====================

        /// <summary>怪物攻击城门时调用（由 MonsterController 或 SurvivalGameManager 触发）</summary>
        public static void OnMonsterAttack()
        {
            Shake(0.08f, 0.12f);
        }

        /// <summary>夜晚完全通过时调用（振奋感）</summary>
        public static void OnNightCleared()
        {
            Shake(0.12f, 0.4f);
        }
    }
}
