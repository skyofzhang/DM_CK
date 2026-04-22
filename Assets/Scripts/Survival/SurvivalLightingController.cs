using UnityEngine;
using System.Collections;

namespace DrscfZ.Survival
{
    /// <summary>
    /// §34.4 E6 夜间修饰符光照控制器 —— 按 nightModifier.id 切换光照预设
    ///
    /// 订阅 SurvivalGameManager.OnPhaseChanged：
    ///   - phase="night" 且 nightModifier != null → 按 id 应用修饰符光照
    ///   - phase="day"                            → 恢复原始光照（白天正常）
    ///
    /// 7 种修饰符光照预设（策划案 5420-5425，零美术成本）：
    ///   normal         → 标准夜晚光照（不覆盖，保留既有 LightingController 管理）
    ///   blood_moon     → 主光 (0.5, 0.1, 0.1)，环境光偏红
    ///   polar_night    → 环境光降到 (0.08, 0.08, 0.1)（极端黑暗）
    ///   fortified      → 主光偏冷白 (0.9, 0.95, 1.0)
    ///   frenzy         → 主光略偏暖黄 (1.0, 0.9, 0.7)
    ///   hunters        → 主光偏紫 (0.8, 0.6, 1.0)
    ///   blizzard_night → RenderSettings.fogDensity = 0.03（浓雾感）
    ///
    /// 与既有 LightingController（昼夜基线光照）的分工：
    ///   - LightingController     负责 day/night 基线切换（冷白光 ↔ 深蓝月光）
    ///   - SurvivalLightingController 仅在 night+nightModifier 时叠加修饰符色调，
    ///     白天或 modifier=normal 时 Restore 到原始（不干扰基线逻辑）
    ///
    /// 挂载：场景中任意常驻 GO（推荐独立空 GO "SurvivalLightingController"）
    /// 运行时自动查找第一个 Directional Light 作为主光；缺失时降级为只改环境光/雾。
    /// </summary>
    public class SurvivalLightingController : MonoBehaviour
    {
        [Header("主光（可选；不指定则自动查找场景中首个 Directional Light）")]
        [SerializeField] private Light _mainLight;

        // ── 原始光照快照（Restore 用） ────────────────────────────────────
        private Color _origMainColor;
        private Color _origAmbientLight;
        private float _origFogDensity;
        private bool  _origFog;
        private bool  _snapshotTaken = false;

        // ── 当前修饰符 id（去抖：相同 id 不重复应用） ─────────────────────
        private string _currentModifierId = "";

        private bool _subscribed = false;

        // ── 生命周期 ──────────────────────────────────────────────────────

        private void Start()
        {
            EnsureMainLight();
            // Fix 4：不再在 Start 时预取快照（此时 LightingController 可能尚未应用基线色）。
            //   快照改为"首次接管前"取值，确保 Restore 回到 LightingController 刚设的值。
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

        // ── 主光查找 ──────────────────────────────────────────────────────

        private void EnsureMainLight()
        {
            if (_mainLight != null) return;

            var mainLightGO = GameObject.Find("Main Light");
            if (mainLightGO != null)
                _mainLight = mainLightGO.GetComponent<Light>();

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
                        if (l.type == LightType.Directional) { _mainLight = l; break; }
                    }
                }
            }

            if (_mainLight == null)
                Debug.LogWarning("[SurvivalLightingController] 未找到 Directional Light，修饰符光照将只改环境光/雾。");
        }

        private void TakeSnapshot()
        {
            // Fix 4：每次"即将接管"前都重新取值，确保 Restore 回到 LightingController 刚设的基线色,
            //   而非 Start 时的初始值（否则前一轮夜→白天切换期被我们撤销到夜色会闪烁）
            _origMainColor    = _mainLight != null ? _mainLight.color : Color.white;
            _origAmbientLight = RenderSettings.ambientLight;
            _origFog          = RenderSettings.fog;
            _origFogDensity   = RenderSettings.fogDensity;
            _snapshotTaken    = true;
        }

        // ── 事件回调 ──────────────────────────────────────────────────────

        private void HandlePhaseChanged(PhaseChangedData data)
        {
            if (data == null) return;

            // Fix 4: 仅在 modifier != null && id != "normal" 时接管 RenderSettings 与主光颜色,
            //   其余场景（白天 / 夜晚 modifier 为 null / id == "normal"）完全不动,
            //   让既有 LightingController（订阅 DayNightCycleManager）正常管理昼夜基线,
            //   避免两个控制器竞争同一 _mainLight.color / RenderSettings 产生顺序依赖 bug。
            //   "Snapshot 时机"改为首次接管前取值;"Restore 时机"仅在从接管态 → 非接管态时执行一次。

            bool shouldTakeOver =
                data.phase == "night"
                && data.nightModifier != null
                && !string.IsNullOrEmpty(data.nightModifier.id)
                && data.nightModifier.id != "normal";

            if (!shouldTakeOver)
            {
                // 释放接管：仅当上一次处于接管态时 Restore 一次，随后把 _mainLight/RenderSettings
                // 交还给 LightingController 自己决定昼夜基线
                if (!string.IsNullOrEmpty(_currentModifierId)
                    && _currentModifierId != "normal")
                {
                    RestoreLighting();
                }
                _currentModifierId = "";
                return;
            }

            string id = data.nightModifier.id;
            if (id == _currentModifierId) return;  // 去抖：已应用同一修饰符

            // 首次接管前取快照（Start 时 LightingController 尚未应用当日/夜色,
            // 必须等到真正接管前一刻再取值，才能保留"被接管前"的基线）
            if (string.IsNullOrEmpty(_currentModifierId)) TakeSnapshot();

            ApplyModifier(id);
            _currentModifierId = id;
        }

        // ── 光照预设 ──────────────────────────────────────────────────────

        private void ApplyModifier(string id)
        {
            switch (id)
            {
                case "blood_moon":
                    SetMainColor(new Color(0.50f, 0.10f, 0.10f));
                    RenderSettings.ambientLight = new Color(0.30f, 0.08f, 0.08f);
                    RenderSettings.fog = _origFog;
                    RenderSettings.fogDensity = _origFogDensity;
                    break;

                case "polar_night":
                    SetMainColor(_origMainColor);  // 主光色不变，只降环境光
                    RenderSettings.ambientLight = new Color(0.08f, 0.08f, 0.10f);
                    RenderSettings.fog = _origFog;
                    RenderSettings.fogDensity = _origFogDensity;
                    break;

                case "fortified":
                    SetMainColor(new Color(0.90f, 0.95f, 1.00f));
                    RenderSettings.ambientLight = _origAmbientLight;
                    RenderSettings.fog = _origFog;
                    RenderSettings.fogDensity = _origFogDensity;
                    break;

                case "frenzy":
                    SetMainColor(new Color(1.00f, 0.90f, 0.70f));
                    RenderSettings.ambientLight = _origAmbientLight;
                    RenderSettings.fog = _origFog;
                    RenderSettings.fogDensity = _origFogDensity;
                    break;

                case "hunters":
                    SetMainColor(new Color(0.80f, 0.60f, 1.00f));
                    RenderSettings.ambientLight = _origAmbientLight;
                    RenderSettings.fog = _origFog;
                    RenderSettings.fogDensity = _origFogDensity;
                    break;

                case "blizzard_night":
                    SetMainColor(_origMainColor);
                    RenderSettings.ambientLight = _origAmbientLight;
                    RenderSettings.fog = true;
                    RenderSettings.fogDensity = 0.03f;
                    break;

                default:
                    RestoreLighting();
                    break;
            }
        }

        private void SetMainColor(Color c)
        {
            if (_mainLight != null) _mainLight.color = c;
        }

        private void RestoreLighting()
        {
            if (!_snapshotTaken) return;
            if (_mainLight != null) _mainLight.color = _origMainColor;
            RenderSettings.ambientLight = _origAmbientLight;
            RenderSettings.fog          = _origFog;
            RenderSettings.fogDensity   = _origFogDensity;
        }
    }
}
