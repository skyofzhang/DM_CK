using UnityEngine;
using System;

namespace DrscfZ.Survival
{
    /// <summary>
    /// 资源系统：管理食物/煤炭/矿石数量 + 炉温
    /// 数据来自服务器推送，本地只做显示缓存
    /// </summary>
    public class ResourceSystem : MonoBehaviour
    {
        public static ResourceSystem Instance { get; private set; }

        [Header("资源初始值（与服务器配置一致，数值来自策划案 §4.1）")]
        [SerializeField] private int   _initFood        = 500;
        [SerializeField] private int   _initCoal        = 300;
        [SerializeField] private int   _initOre         = 150;
        [SerializeField] private float _initFurnaceTemp = 20f;  // 初始炉温
        [SerializeField] private float _minTemp         = -100f;
        [SerializeField] private float _maxTemp         = 100f;

        // ---- 当前值（由服务器更新） ----
        public int   Food         { get; private set; }
        public int   Coal         { get; private set; }
        public int   Ore          { get; private set; }
        public float FurnaceTemp  { get; private set; }

        // ---- 事件 ----
        public event Action<int, int, int, float> OnResourceChanged;  // food, coal, ore, temp
        public event Action OnFoodDepleted;    // 食物=0 → 失败
        public event Action OnTempFreeze;      // 炉温≤-100 → 失败

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Initialize()
        {
            Food         = _initFood;
            Coal         = _initCoal;
            Ore          = _initOre;
            FurnaceTemp  = _initFurnaceTemp;
            OnResourceChanged?.Invoke(Food, Coal, Ore, FurnaceTemp);
        }

        /// <summary>服务器 resource_update 消息 → 同步本地显示值</summary>
        public void ApplyServerUpdate(ResourceUpdateData data)
        {
            Food        = data.food;
            Coal        = data.coal;
            Ore         = data.ore;
            FurnaceTemp = data.furnaceTemp;
            OnResourceChanged?.Invoke(Food, Coal, Ore, FurnaceTemp);

            // 检测失败条件（辅助判断，服务器才是权威）
            if (Food <= 0)
                OnFoodDepleted?.Invoke();
            if (FurnaceTemp <= _minTemp)
                OnTempFreeze?.Invoke();
        }

        /// <summary>礼物增加资源（服务器推送 → 调用此方法）</summary>
        public void ApplyGiftEffect(SurvivalGiftData gift)
        {
            Food        = Mathf.Max(0, Food + gift.addFood);
            Coal        = Mathf.Max(0, Coal + gift.addCoal);
            Ore         = Mathf.Max(0, Ore  + gift.addOre);
            FurnaceTemp = Mathf.Clamp(FurnaceTemp + gift.addHeat, _minTemp, _maxTemp);
            OnResourceChanged?.Invoke(Food, Coal, Ore, FurnaceTemp);
        }

        public void Reset()
        {
            Initialize();
        }

        // ---- 调试/GM 直接设置 ----
        public void DebugSetFood(int v)        { Food = v;        OnResourceChanged?.Invoke(Food, Coal, Ore, FurnaceTemp); }
        public void DebugSetCoal(int v)        { Coal = v;        OnResourceChanged?.Invoke(Food, Coal, Ore, FurnaceTemp); }
        public void DebugSetOre(int v)         { Ore  = v;        OnResourceChanged?.Invoke(Food, Coal, Ore, FurnaceTemp); }
        public void DebugSetTemp(float v)      { FurnaceTemp = v; OnResourceChanged?.Invoke(Food, Coal, Ore, FurnaceTemp); }

        /// <summary>炉温颜色：越冷越蓝，越暖越橙</summary>
        public Color GetTempColor()
        {
            float t = Mathf.InverseLerp(_minTemp, _maxTemp, FurnaceTemp);
            return Color.Lerp(new Color(0.23f, 0.51f, 0.96f), new Color(0.97f, 0.45f, 0.07f), t);
        }
    }
}
