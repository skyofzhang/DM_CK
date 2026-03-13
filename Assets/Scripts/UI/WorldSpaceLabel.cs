using System.Collections;
using UnityEngine;
using TMPro;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// 3D 场景中悬浮标签 — 始终面向摄像机（Billboard），显示地点图标+资源数量/状态
    /// 通过 SetLabelType(LabelType) 决定它跟踪哪个资源字段
    ///
    /// Rule #7：挂在始终激活的 WorldSpace Canvas 上
    /// Rule #5：不覆盖用户手动设置的位置，位置仅由 Scene 编辑器决定
    ///
    /// HP 进度条采用 3D Quad 方案（非 UI Image），通过 Transform.localScale.x 控制进度。
    /// HPBar_Fill 的 X 轴原点在中心，需配合 pivot 偏移使其从左往右缩放。
    /// </summary>
    public class WorldSpaceLabel : MonoBehaviour
    {
        public enum LabelType
        {
            Static,      // 纯静态文字，不跟踪资源
            Food,        // 跟踪 ResourceSystem.Food            格式："食物 {count}"
            Coal,        // 跟踪 ResourceSystem.Coal            格式："煤 {count}"
            Ore,         // 跟踪 ResourceSystem.Ore             格式："矿石 {count}"
            Temperature, // 跟踪 ResourceSystem.FurnaceTemp     格式："{temp}°C"
            Campfire,    // 跟踪 ResourceSystem.FurnaceTemp     格式："炉温 {temp}°C"，橙→蓝渐变
            GateHP,      // 跟踪 CityGateSystem HP              格式："城门 {hp}/{maxHp}"，绿→黄→红
        }

        [Header("显示配置")]
        public LabelType labelType = LabelType.Static;
        public string    icon      = "";        // 图标文字，如 "城"
        public string    label     = "";        // 静态显示文字（labelType=Static 时使用）

        [Header("组件引用")]
        public TextMeshPro iconText;    // 大图标 TMP (3D WorldSpace)
        public TextMeshPro valueText;   // 数值文字 TMP (3D WorldSpace)

        [Header("HP 进度条（GateHP 类型使用，3D Quad 方案）")]
        [Tooltip("HPBar_Fill Quad 的 Transform，通过缩放 X 轴模拟进度")]
        [SerializeField] private Transform _hpBarFillTransform;

        // HP 进度条填充 Quad 的初始 X 缩放（满血时的宽度），由场景编辑器设置
        private float _hpBarFillInitialScaleX = 1f;
        // HPBar_Fill Quad 的 MeshRenderer（用于动态改色绿→黄→红）
        private MeshRenderer _hpBarFillRenderer;

        private Transform _cam;

        // ── 注意：TextMeshPro (3D) 字体大小单位是世界单位，不是像素！
        // Label_CityGate 的 Transform.scale = (0.1, 0.1, 0.1)
        // fontSize=3.5 世界单位 × 0.1 scale = 0.35 实际大小 → 合理可读
        // 绝对不能用 < 40 判断然后设为 40（那会是 4 米高的字）
        // 最小安全值：2.5 世界单位（防止文字过小）
        private const float k_MinFontSizeWorldSpace = 2.5f;

        private void Awake()
        {
            // 记录 HPBar 填充条的初始 X 缩放（满血宽度基准），缓存 Renderer 用于变色
            if (_hpBarFillTransform != null)
            {
                _hpBarFillInitialScaleX = _hpBarFillTransform.localScale.x;
                // HPBar_Fill_Pivot 的子对象 HPBar_Fill 持有 MeshRenderer
                _hpBarFillRenderer = _hpBarFillTransform.GetComponentInChildren<MeshRenderer>();
            }
        }

        private void Start()
        {
            _cam = Camera.main?.transform;

            // 绑定中文字体，防止乱码（Rule R2）
            var chineseFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts/ChineseFont SDF");
            var outlineMat  = Resources.Load<Material>("Fonts/ChineseFont SDF - Outline");
            if (iconText != null)
            {
                if (chineseFont != null) iconText.font = chineseFont;
                if (outlineMat  != null) iconText.fontSharedMaterial = outlineMat;
                // 世界空间 TMP：字号单位是世界单位，最小 2.5，不能误设为像素值
                if (iconText.fontSize < k_MinFontSizeWorldSpace)
                    iconText.fontSize = k_MinFontSizeWorldSpace;
            }
            if (valueText != null)
            {
                if (chineseFont != null) valueText.font = chineseFont;
                if (outlineMat  != null) valueText.fontSharedMaterial = outlineMat;
                if (valueText.fontSize < k_MinFontSizeWorldSpace)
                    valueText.fontSize = k_MinFontSizeWorldSpace;
            }

            // 订阅资源变化事件（Food/Coal/Ore/Temperature/Campfire）
            bool needsResource = labelType == LabelType.Food
                              || labelType == LabelType.Coal
                              || labelType == LabelType.Ore
                              || labelType == LabelType.Temperature
                              || labelType == LabelType.Campfire;
            if (needsResource)
            {
                var res = ResourceSystem.Instance;
                if (res != null)
                    res.OnResourceChanged += OnResourceChanged;
            }

            // 订阅城门 HP 变化事件：用协程等待 Instance 可用，防止 Start 顺序问题
            if (labelType == LabelType.GateHP)
            {
                var gate = CityGateSystem.Instance;
                if (gate != null)
                {
                    // Instance 已就绪，直接订阅并同步当前值
                    gate.OnHpChanged += OnGateHpChanged;
                    OnGateHpChanged(gate.CurrentHp, gate.MaxHp);
                }
                else
                {
                    // Instance 还未初始化（Awake 顺序问题），协程等待
                    StartCoroutine(WaitAndSubscribeGateHp());
                }
            }

            // 静态模式直接显示 label
            if (labelType == LabelType.Static && valueText != null)
                valueText.text = label;

            if (iconText != null)
                iconText.text = icon;
        }

        /// <summary>
        /// 等待 CityGateSystem.Instance 可用后再订阅，防止 Start 执行时 Instance 为 null。
        /// 订阅成功后立即同步当前 HP 值，确保初始显示正确。
        /// </summary>
        private System.Collections.IEnumerator WaitAndSubscribeGateHp()
        {
            while (CityGateSystem.Instance == null)
                yield return null;

            var gate = CityGateSystem.Instance;
            gate.OnHpChanged += OnGateHpChanged;
            // 立即同步当前值
            OnGateHpChanged(gate.CurrentHp, gate.MaxHp);
        }

        private void OnDestroy()
        {
            var res = ResourceSystem.Instance;
            if (res != null)
                res.OnResourceChanged -= OnResourceChanged;

            var gate = CityGateSystem.Instance;
            if (gate != null)
                gate.OnHpChanged -= OnGateHpChanged;
        }

        private void LateUpdate()
        {
            // Billboard：始终面向摄像机
            if (_cam == null) _cam = Camera.main?.transform;
            if (_cam != null)
                transform.rotation = Quaternion.LookRotation(transform.position - _cam.position);
        }

        // ── 资源变化回调（Food / Coal / Ore / Temperature / Campfire）──────────────
        private void OnResourceChanged(int food, int coal, int ore, float temp)
        {
            if (valueText == null) return;
            switch (labelType)
            {
                case LabelType.Food:
                    valueText.text  = $"食物 {food}";
                    valueText.color = new Color(0.5f, 1f, 0.5f);   // 绿色
                    break;
                case LabelType.Coal:
                    valueText.text  = $"煤 {coal}";
                    valueText.color = new Color(0.7f, 0.7f, 0.7f); // 灰色
                    break;
                case LabelType.Ore:
                    valueText.text  = $"矿石 {ore}";
                    valueText.color = new Color(0.5f, 0.8f, 1f);   // 蓝白色
                    break;
                case LabelType.Temperature:
                    valueText.text  = $"{temp:+0;-0}°C";
                    valueText.color = GetTempColor(temp);
                    break;
                case LabelType.Campfire:
                    valueText.text      = $"炉温\n{temp:0}°C";
                    valueText.color     = GetTempColor(temp);
                    valueText.alignment = TMPro.TextAlignmentOptions.Center;
                    break;
            }
        }

        // ── 城门 HP 变化回调 ────────────────────────────────────────────────────────
        private void OnGateHpChanged(int current, int max)
        {
            if (valueText == null) return;
            valueText.text  = $"城门 {current}/{max}";
            valueText.color = GetGateHpColor(current, max);
            UpdateHpBar(current, max);
        }

        /// <summary>
        /// 由 CityGateSystem 直接调用，作为事件订阅的补充（防止 Start 顺序问题）。
        /// 仅在 labelType == GateHP 时生效，其他类型忽略。
        /// </summary>
        public void ForceUpdateGateHp(int current, int max)
        {
            if (labelType != LabelType.GateHP || valueText == null) return;
            valueText.text  = $"城门 {current}/{max}";
            valueText.color = GetGateHpColor(current, max);
            UpdateHpBar(current, max);
        }

        /// <summary>
        /// 通过 Transform.localScale.x 更新 3D Quad HP 进度条（Pivot 左端固定方案）。
        /// _hpBarFillTransform = HPBar_Fill_Pivot，位于背景条左端。
        /// 缩放 Pivot.X 使右端收缩，左端固定。
        /// 同时通过 MaterialPropertyBlock 将填充 Quad 颜色同步为 绿→黄→红。
        /// </summary>
        private void UpdateHpBar(int current, int max)
        {
            if (_hpBarFillTransform == null) return;
            float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;

            // 缩放 Pivot 的 X 轴（满血时 scale.x = _hpBarFillInitialScaleX = 1）
            var s = _hpBarFillTransform.localScale;
            s.x = _hpBarFillInitialScaleX * ratio;
            _hpBarFillTransform.localScale = s;

            // 同步填充颜色（绿→黄→红）
            if (_hpBarFillRenderer != null)
            {
                Color fillColor = GetGateHpColor(current, max);
                var mpb = new MaterialPropertyBlock();
                _hpBarFillRenderer.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", fillColor);  // URP Unlit
                mpb.SetColor("_Color", fillColor);       // Built-in fallback
                _hpBarFillRenderer.SetPropertyBlock(mpb);
            }
        }

        // ── 颜色计算 ─────────────────────────────────────────────────────────────────

        /// <summary>炉温颜色：冷=蓝，暖=橙，中性=白</summary>
        private static Color GetTempColor(float temp)
        {
            if (temp < 0f)
                return new Color(0.3f, 0.5f, 1f);   // 蓝色 = 冷
            if (temp > 50f)
                return new Color(1f, 0.5f, 0f);      // 橙色 = 热
            return Color.white;
        }

        /// <summary>城门HP颜色：绿（>60%）→ 黄（>30%）→ 橙红（≤30%）；使用高亮色提升暗背景可读性</summary>
        private static Color GetGateHpColor(int current, int max)
        {
            float ratio = max > 0 ? (float)current / max : 1f;
            if (ratio > 0.6f) return new Color(0.2f, 1f, 0.2f);   // 亮绿
            if (ratio > 0.3f) return new Color(1f, 0.9f, 0.1f);   // 亮黄
            return new Color(1f, 0.4f, 0.1f);                      // 橙红（比纯红在暗背景更醒目）
        }
    }
}
