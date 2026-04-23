using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §37 建筑状态面板（Batch I MVP）。
    ///
    /// 功能：
    ///   - 显示 5 种建筑（watchtower / market / hospital / altar / beacon）的当前状态
    ///   - 状态 4 态：未建（灰）/ 建造中+%（黄）/ 已建（绿）/ 拆除刚发生闪红 0.8s 回灰
    ///   - 订阅 SurvivalGameManager 的 OnBuildStarted / OnBuildProgress / OnBuildCompleted /
    ///     OnBuildDemolished / OnBuildingDemolishedBatch 事件
    ///
    /// 场景挂载（TODO Batch I）：
    ///   Canvas/GameUIPanel/BuildingStatusPanel/
    ///     ├── Row_Watchtower (Image dot + TMP_Text name/percent)
    ///     ├── Row_Market     ...
    ///     ├── Row_Hospital   ...
    ///     ├── Row_Altar      ...
    ///     └── Row_Beacon     ...
    ///   5 个 Row 一次性 Inspector 拖入 _buildingDots / _buildingLabels / _buildingPercents 并按固定顺序
    ///   （watchtower=0, market=1, hospital=2, altar=3, beacon=4）。
    ///
    /// MVP 降级：
    ///   - 未绑定任何 Inspector 字段时所有方法走 Debug.Log 路径不崩
    ///   - 若 progress 消息未到（服务端未实现 build_progress），本地基于 completesAt 线性插值
    ///
    /// Rule #7：挂 Canvas（always-active），_root 初始可直接激活（不是 modal，常驻）。
    /// </summary>
    public class BuildingStatusPanelUI : MonoBehaviour
    {
        public static BuildingStatusPanelUI Instance { get; private set; }

        // ==================== Inspector 引用 ====================

        [Header("面板根（可为空；为空时仅按文字/Log 输出）")]
        [SerializeField] private GameObject _root;

        [Header("5 个建筑状态 Dot Image（按 watchtower/market/hospital/altar/beacon 顺序）")]
        [SerializeField] private Image[] _buildingDots = new Image[5];

        [Header("5 个建筑名称 TMP（可选）")]
        [SerializeField] private TMP_Text[] _buildingLabels = new TMP_Text[5];

        [Header("5 个建筑进度 TMP（建造中显示百分比；其他态可隐藏）")]
        [SerializeField] private TMP_Text[] _buildingPercents = new TMP_Text[5];

        // ==================== 常量 ====================

        private static readonly string[] BUILDING_IDS = { "watchtower", "market", "hospital", "altar", "beacon" };
        private static readonly string[] BUILDING_NAMES = { "瞭望塔", "市场", "医院", "祭坛", "烽火台" };

        // 色值（MVP）
        private static readonly Color COLOR_UNBUILT  = new Color(0.35f, 0.35f, 0.35f, 1f); // 灰
        private static readonly Color COLOR_BUILDING = new Color(1f, 0.75f, 0.1f,  1f);     // 黄
        private static readonly Color COLOR_BUILT    = new Color(0.4f, 1f,   0.6f, 1f);     // 绿
        private static readonly Color COLOR_DEMO     = new Color(1f,   0.3f, 0.3f, 1f);     // 红（一闪即逝）

        // ==================== 运行时状态 ====================

        private enum BuildingState { Unbuilt, Building, Built }

        private readonly BuildingState[] _states = new BuildingState[5];
        private readonly float[] _progress = new float[5];         // 0..1（建造中）
        private readonly long[]  _completesAt = new long[5];       // Unix ms（用于服务端未发 progress 时本地插值）
        private bool _subscribed = false;

        // ==================== 生命周期 ====================

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            for (int i = 0; i < 5; i++) _states[i] = BuildingState.Unbuilt;
        }

        private void Start()
        {
            TrySubscribe();
            RenderAll();
        }

        private void OnEnable()   { TrySubscribe(); }
        private void OnDisable()  { Unsubscribe(); }
        private void OnDestroy()
        {
            Unsubscribe();
            if (Instance == this) Instance = null;
        }

        // SGM 可能比本组件晚初始化，Update 里补订阅（成功后停止轮询）+ 本地进度插值
        private void Update()
        {
            if (!_subscribed) TrySubscribe();

            // 若服务端未发 build_progress，本地用 completesAt 做兜底插值（每帧更新百分比文本）
            for (int i = 0; i < 5; i++)
            {
                if (_states[i] != BuildingState.Building) continue;
                if (_completesAt[i] <= 0) continue;

                long nowMs = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long remainMs = _completesAt[i] - nowMs;
                if (remainMs <= 0) continue;

                // 线性估算（不覆盖服务端真实 progress，仅在服务端未推送时用）
                // 这里只更新百分比文本，不动 state / dot 颜色
                RefreshPercentText(i);
            }
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;

            sgm.OnBuildStarted            += HandleBuildStarted;
            sgm.OnBuildProgress           += HandleBuildProgress;
            sgm.OnBuildCompleted          += HandleBuildCompleted;
            sgm.OnBuildDemolished         += HandleBuildDemolished;
            sgm.OnBuildingDemolishedBatch += HandleBuildingDemolishedBatch;
            sgm.OnBuildCancelled          += HandleBuildCancelled;
            _subscribed = true;
            Debug.Log("[BuildingStatusPanelUI] 已订阅 SurvivalGameManager 建造事件");
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnBuildStarted            -= HandleBuildStarted;
                sgm.OnBuildProgress           -= HandleBuildProgress;
                sgm.OnBuildCompleted          -= HandleBuildCompleted;
                sgm.OnBuildDemolished         -= HandleBuildDemolished;
                sgm.OnBuildingDemolishedBatch -= HandleBuildingDemolishedBatch;
                sgm.OnBuildCancelled          -= HandleBuildCancelled;
            }
            _subscribed = false;
        }

        // ==================== 事件回调 ====================

        private void HandleBuildStarted(BuildStartedData data)
        {
            int idx = IndexOfBuildId(data.buildId);
            if (idx < 0) return;
            _states[idx] = BuildingState.Building;
            _progress[idx] = 0f;
            _completesAt[idx] = data.completesAt;
            RenderRow(idx);
        }

        private void HandleBuildProgress(BuildProgressData data)
        {
            int idx = IndexOfBuildId(data.buildId);
            if (idx < 0) return;
            _progress[idx] = Mathf.Clamp01(data.progress);
            if (data.completesAt > 0) _completesAt[idx] = data.completesAt;
            // 状态保持 Building，仅刷新百分比
            RefreshPercentText(idx);
        }

        private void HandleBuildCompleted(BuildCompletedData data)
        {
            int idx = IndexOfBuildId(data.buildId);
            if (idx < 0) return;
            _states[idx] = BuildingState.Built;
            _progress[idx] = 1f;
            _completesAt[idx] = 0;
            RenderRow(idx);
        }

        private void HandleBuildDemolished(BuildDemolishedData data)
        {
            int idx = IndexOfBuildId(data.buildId);
            if (idx < 0) return;
            // 闪红 0.8s 再回灰
            StartCoroutine(FlashThenUnbuild(idx));
        }

        private void HandleBuildingDemolishedBatch(BuildingDemolishedBatchData data)
        {
            if (data == null || data.buildingIds == null) return;
            foreach (var buildId in data.buildingIds)
            {
                int idx = IndexOfBuildId(buildId);
                if (idx < 0) continue;
                StartCoroutine(FlashThenUnbuild(idx));
            }
        }

        private void HandleBuildCancelled(BuildCancelledData data)
        {
            // 投票通过但扣费失败 → 从 Building 回落到 Unbuilt
            int idx = IndexOfBuildId(data.buildId);
            if (idx < 0) return;
            _states[idx] = BuildingState.Unbuilt;
            _progress[idx] = 0f;
            _completesAt[idx] = 0;
            RenderRow(idx);
        }

        private IEnumerator FlashThenUnbuild(int idx)
        {
            // 先把 dot 染红 0.8s，再回到 Unbuilt
            if (idx >= 0 && idx < _buildingDots.Length && _buildingDots[idx] != null)
                _buildingDots[idx].color = COLOR_DEMO;
            yield return new WaitForSeconds(0.8f);
            _states[idx] = BuildingState.Unbuilt;
            _progress[idx] = 0f;
            _completesAt[idx] = 0;
            RenderRow(idx);
        }

        // ==================== 渲染 ====================

        private void RenderAll()
        {
            for (int i = 0; i < 5; i++) RenderRow(i);
        }

        private void RenderRow(int idx)
        {
            if (idx < 0 || idx >= 5) return;

            // Dot 颜色
            if (idx < _buildingDots.Length && _buildingDots[idx] != null)
            {
                _buildingDots[idx].color = _states[idx] switch
                {
                    BuildingState.Unbuilt  => COLOR_UNBUILT,
                    BuildingState.Building => COLOR_BUILDING,
                    BuildingState.Built    => COLOR_BUILT,
                    _                       => COLOR_UNBUILT,
                };
            }

            // 名称
            if (idx < _buildingLabels.Length && _buildingLabels[idx] != null)
                _buildingLabels[idx].text = BUILDING_NAMES[idx];

            // 百分比文本
            RefreshPercentText(idx);
        }

        private void RefreshPercentText(int idx)
        {
            if (idx < 0 || idx >= _buildingPercents.Length) return;
            var txt = _buildingPercents[idx];
            if (txt == null) return;

            switch (_states[idx])
            {
                case BuildingState.Building:
                {
                    float p = _progress[idx];
                    if (p > 0f)
                    {
                        txt.text = $"{Mathf.RoundToInt(p * 100f)}%";
                    }
                    else if (_completesAt[idx] > 0)
                    {
                        // 未收到 build_progress（服务端可能不发）：显示剩余秒数兜底
                        long nowMs = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        long remainMs = System.Math.Max(0, _completesAt[idx] - nowMs);
                        txt.text = $"{Mathf.CeilToInt(remainMs / 1000f)}s";
                    }
                    else
                    {
                        txt.text = "建造中";
                    }
                    break;
                }
                case BuildingState.Built:
                    txt.text = "已建";
                    break;
                case BuildingState.Unbuilt:
                default:
                    txt.text = "";
                    break;
            }
        }

        // ==================== 工具 ====================

        /// <summary>buildId → 数组索引；未知 id 返回 -1</summary>
        private static int IndexOfBuildId(string buildId)
        {
            if (string.IsNullOrEmpty(buildId)) return -1;
            for (int i = 0; i < BUILDING_IDS.Length; i++)
                if (BUILDING_IDS[i] == buildId) return i;
            return -1;
        }
    }
}
