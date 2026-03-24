using UnityEngine;
using System.Collections.Generic;
using DrscfZ.UI;

namespace DrscfZ.Survival
{
    // ==================== T006：工位槽位数据结构 ====================

    /// <summary>
    /// 单个工位槽（StationSlot）——可在Inspector编辑坐标（T003）。
    ///
    /// 设计：每种工作类型（cmdType）配置多个槽位，
    /// 使多名Worker同时工作时不会全部堆叠在同一点。
    /// 推荐每种类型配置3-4个槽，共约20个（对应MAX_WORKERS）。
    /// </summary>
    [System.Serializable]
    public class StationSlot
    {
        [Tooltip("工位类型（1=采食物/2=挖煤/3=挖矿/4=生火/6=打怪）")]
        public int cmdType;

        [Tooltip("工位世界坐标（可在Inspector直接填写，或使用Gizmo调整）")]
        public Vector3 position;

        [HideInInspector]
        public string occupyingPlayerId = "";  // 运行时：占用此槽的 playerId（空字符串=空闲）

        public bool IsOccupied => !string.IsNullOrEmpty(occupyingPlayerId);
    }

    // ==================== WorkerManager ====================

    /// <summary>
    /// 奶牛角色（工人）管理器
    ///
    /// M4 新增功能（T003~T008）：
    ///   T003：StationSlot[] 替代硬编码 WORK_POSITIONS，支持Inspector编辑坐标
    ///   T006：StationSlot 数据结构（槽位/状态追踪）
    ///   T007：Worker待机位（HomePosition），工作结束后平滑回到待机圈
    ///   T008：就近Worker分配算法（替代随机选取）
    ///
    /// 工位槽位配置说明：
    ///   在Inspector中为每种cmdType配置3~4个槽位。
    ///   或使用菜单 Tools → DrscfZ → Setup Default Station Slots 自动填充默认值。
    /// </summary>
    public class WorkerManager : MonoBehaviour
    {
        public static WorkerManager Instance { get; private set; }

        // ==================== T003：Inspector可编辑工位槽位 ====================

        [Header("工位槽位配置（T003，共约20个槽）")]
        [Tooltip("每种工作类型配置3-4个槽位坐标，不同槽位错开避免Worker重叠")]
        [SerializeField] private StationSlot[] _stationSlots;

        /// <summary>
        /// 向后兼容：返回每种cmdType的第一个槽位坐标（供外部旧代码查询用）。
        /// 新代码请直接使用 GetNearestAvailableSlot(cmdType, fromPos)。
        /// </summary>
        public static Dictionary<int, Vector3> WORK_POSITIONS => Instance?._GetWorkPositionsFallback();

        // ==================== 颜色/图标静态查询 ====================

        private static readonly Color[] _workColors =
        {
            Color.white,
            new Color(0.267f, 0.533f, 1.0f),   // [1] 采食物 #4488FF
            new Color(0.4f,   0.4f,   0.4f),   // [2] 挖煤   #666666
            new Color(0.533f, 0.8f,   1.0f),   // [3] 挖矿   #88CCFF
            new Color(1.0f,   0.408f, 0.125f), // [4] 生火   #FF6820
            Color.white,
            new Color(1.0f,   0.133f, 0.0f),   // [6] 打怪   #FF2200
        };

        private static readonly string[] _workIcons =
        {
            "", "食", "煤", "矿", "火", "", "战",
        };

        public static Color  GetColorForCmd(int cmd)
            => (cmd >= 0 && cmd < _workColors.Length) ? _workColors[cmd] : Color.white;
        public static string GetIconForCmd(int cmd)
            => (cmd >= 0 && cmd < _workIcons.Length)  ? _workIcons[cmd]  : "";

        // ==================== Inspector 配置 ====================

        [Header("Worker生成配置")]
        [SerializeField] private GameObject        _workerPrefab;    // 矿工外观A（KuanggongWorker_01）
        [SerializeField] private GameObject        _workerPrefab2;   // 矿工外观B（KuanggongWorker_02，随机选一）
        [SerializeField] private Transform         _fortressCenter;  // 中央堡垒（待机圈圆心）
        [SerializeField] private float             _idleRadius = 3f; // 待机圆圈半径

        [Header("城门防守配置")]
        [SerializeField] private Transform         _gateDefenseCenter; // 城门前防守集结点（拖入WorkTarget_Gate）

        [Header("预创建Worker池（Inspector拖入，数量=MAX_WORKERS）")]
        [SerializeField] private WorkerController[] _preCreatedWorkers;

        // ==================== 常量 ====================

        public const int MAX_WORKERS = 12;  // 性能优化：从20降至12，减少峰值 Skinned Mesh 数量

        /// <summary>当前场景内存活的 Worker 数量（供 PreGameBannerUI 等 UI 读取）</summary>
        public int WorkerCount => _activeWorkers.Count;

        /// <summary>当前活跃 Worker 只读列表（供 MonsterController 避免每帧 FindObjectsOfType）</summary>
        public IReadOnlyList<WorkerController> ActiveWorkers => _activeWorkers;

        // ==================== 运行时状态 ====================

        private readonly List<WorkerController> _activeWorkers = new List<WorkerController>();

        /// <summary>T008：记录 Worker → 槽位索引的映射（用于槽位释放）</summary>
        private readonly Dictionary<WorkerController, int> _workerToSlot
            = new Dictionary<WorkerController, int>();

        // ==================== 生命周期 ====================

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // 运行时：若 Inspector 未配置槽位则自动填充默认值，确保 Worker 能移动
            if (_stationSlots == null || _stationSlots.Length == 0)
            {
                PopulateDefaultSlotsRuntime();
                Debug.Log("[WorkerManager] 工位槽未配置，已自动填充默认值（如需精确位置请在Inspector手动调整）");
            }

            if (_preCreatedWorkers != null)
                foreach (var w in _preCreatedWorkers)
                    if (w != null) w.gameObject.SetActive(false);

            // 初始化围炉位置：按优先级查找场景中的火堆 GameObject
            // 优先级：M-CAMPFIRE > Campfire > campfire > Fireplace > furnace > StationSlot[cmd=4] > 默认原点
            InitCampfirePosition();

            // 初始化矿工站位：根据场景中的煤堆/矿堆实际位置重算工位坐标，使矿工包围资源堆
            InitMiningPositions();
        }

        /// <summary>
        /// 查找场景中的火堆 GameObject，将其坐标写入 WorkerController.CampfirePosition。
        /// 如果场景中没有火堆对象，则从 cmd=4 工位槽中心推算，或使用默认坐标。
        /// </summary>
        private void InitCampfirePosition()
        {
            // 候选名称（按优先级排列，M-CAMPFIRE 为最高优先级）
            string[] candidateNames = { "M-CAMPFIRE", "Campfire", "campfire", "CampFire",
                                        "Fireplace", "fireplace", "Furnace", "furnace",
                                        "FirePit", "fire_pit", "Bonfire", "bonfire" };
            foreach (string name in candidateNames)
            {
                GameObject obj = GameObject.Find(name);
                if (obj != null)
                {
                    WorkerController.CampfirePosition = obj.transform.position;
                    Debug.Log($"[WorkerManager] 围炉位置来自场景对象 '{name}'：{obj.transform.position}");
                    return;
                }
            }

            // 未找到场景对象时，从 cmd=4 工位槽中心推算
            if (_stationSlots != null)
            {
                Vector3 sum   = Vector3.zero;
                int     count = 0;
                foreach (var slot in _stationSlots)
                    if (slot.cmdType == 4) { sum += slot.position; count++; }
                if (count > 0)
                {
                    WorkerController.CampfirePosition = sum / count;
                    Debug.Log($"[WorkerManager] 围炉位置由 cmd=4 槽位中心推算：{WorkerController.CampfirePosition}");
                    return;
                }
            }

            // 最终兜底：使用场景默认炉灶坐标（与 PopulateDefaultSlotsRuntime 一致）
            WorkerController.CampfirePosition = new Vector3(3f, 0f, 3f);
            Debug.Log($"[WorkerManager] 未找到火堆对象，使用默认围炉位置：{WorkerController.CampfirePosition}");
        }

        /// <summary>
        /// 根据场景中煤堆/矿堆的实际 GameObject 坐标和 Renderer.bounds，
        /// 动态重算 cmd=2/3 的工位圆圈，使矿工恰好包围在资源堆外侧。
        /// 每次 Awake 时执行（无论 Inspector 是否已序列化 slots），确保工位与视觉模型对齐。
        /// </summary>
        private void InitMiningPositions()
        {
            const float MARGIN = 0.6f; // 工位站在 AABB 外侧的额外边距（矿工站在煤堆脚边）

            // --- cmd=2 煤矿：找 "煤" 对象，用 Renderer.bounds 计算精确包围半径 ---
            Vector3 coalCenter = new(-3.62f, 0f, 16.2f); // fallback
            float   coalRadius = 3.6f;                   // fallback（实测 max_half=3.01 + margin + 余量）
            GameObject coalObj = GameObject.Find("煤");
            if (coalObj != null)
            {
                // 用 AABB 中心 XZ 作为圆心（比 transform.pivot 更精确）
                Bounds b = GetRendererBounds(coalObj);
                coalCenter = new Vector3(b.center.x, 0f, b.center.z);
                // 半径 = XZ 方向最大半轴 + 边距，保证工位在 AABB 外侧
                coalRadius = Mathf.Max(b.extents.x, b.extents.z) + MARGIN;
                Debug.Log($"[WorkerManager] 煤矿: center={coalCenter} AABB半轴max={Mathf.Max(b.extents.x, b.extents.z):F2}m radius={coalRadius:F2}m");
            }
            else
            {
                Debug.LogWarning($"[WorkerManager] 未找到 '煤' 对象，使用 fallback: center={coalCenter} radius={coalRadius}");
            }
            OverwriteSlotCircle(2, coalCenter, coalRadius, 4);

            // --- cmd=3 矿山：优先找 "矿山1"，找不到则用 "矿山2" ---
            Vector3 oreCenter = new(5.81f, 0f, 19.3f); // fallback
            float   oreRadius = 3.6f;                  // fallback
            GameObject ore1 = GameObject.Find("矿山1");
            GameObject ore2 = GameObject.Find("矿山2");
            GameObject oreObj = ore1 ?? ore2;
            if (oreObj != null)
            {
                Bounds b = GetRendererBounds(oreObj);
                oreCenter = new Vector3(b.center.x, 0f, b.center.z);
                oreRadius = Mathf.Max(b.extents.x, b.extents.z) + MARGIN;
                Debug.Log($"[WorkerManager] 矿山 '{oreObj.name}': center={oreCenter} radius={oreRadius:F2}m");
            }
            else
            {
                Debug.LogWarning($"[WorkerManager] 未找到矿山对象，使用 fallback: center={oreCenter} radius={oreRadius}");
            }
            OverwriteSlotCircle(3, oreCenter, oreRadius, 4);

            // --- cmd=1 食物：找 "渔场" 对象 ---
            Vector3 fishCenter = new(-13.91f, 0f, 11.4f); // fallback（渔场实测中心）
            float   fishRadius = 3.4f;                    // fallback
            GameObject fishObj = GameObject.Find("渔场");
            if (fishObj != null)
            {
                Bounds fb = GetRendererBounds(fishObj);
                fishCenter = new Vector3(fb.center.x, 0f, fb.center.z);
                fishRadius = Mathf.Max(fb.extents.x, fb.extents.z) + MARGIN;
                Debug.Log($"[WorkerManager] 渔场: center={fishCenter} radius={fishRadius:F2}m");
            }
            else
            {
                Debug.LogWarning("[WorkerManager] 未找到 '渔场' 对象，使用 fallback");
            }
            OverwriteSlotCircle(1, fishCenter, fishRadius, 4);

            // --- cmd=4 炉灶：找 "火堆" 对象 ---
            Vector3 fireCenter = new(-0.016f, 0f, 1.856f); // fallback（火堆实测中心）
            float   fireRadius = 2.6f;                     // fallback
            GameObject fireObj = GameObject.Find("火堆");
            if (fireObj != null)
            {
                Bounds fb = GetRendererBounds(fireObj);
                fireCenter = new Vector3(fb.center.x, 0f, fb.center.z);
                fireRadius = Mathf.Max(fb.extents.x, fb.extents.z) + MARGIN;
                Debug.Log($"[WorkerManager] 火堆: center={fireCenter} radius={fireRadius:F2}m");
            }
            else
            {
                Debug.LogWarning("[WorkerManager] 未找到 '火堆' 对象，使用 fallback");
            }
            OverwriteSlotCircle(4, fireCenter, fireRadius, 4);
        }

        /// <summary>合并 GameObject 及其所有子对象的 Renderer.bounds，返回世界空间 AABB。</summary>
        private static Bounds GetRendererBounds(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
                return new Bounds(obj.transform.position, Vector3.one * 2f); // 兜底
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);
            return b;
        }

        /// <summary>
        /// 将指定 cmdType 的前 count 个工位按圆形均匀排列（0°/90°/180°/270°）。
        /// </summary>
        private void OverwriteSlotCircle(int cmdType, Vector3 center, float radius, int count)
        {
            if (_stationSlots == null) return;
            int assigned = 0;
            foreach (var slot in _stationSlots)
            {
                if (slot.cmdType != cmdType) continue;
                float angle = assigned * (360f / count) * Mathf.Deg2Rad;
                slot.position = new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    0f,
                    center.z + Mathf.Sin(angle) * radius);
                assigned++;
                if (assigned >= count) break;
            }
            Debug.Log($"[WorkerManager] cmd={cmdType} 工位已重算 → 圆心={center} 半径={radius}m 共{assigned}个");
        }

        /// <summary>
        /// 运行时可调用的默认槽位填充（与Editor的AutoPopulateDefaultSlots逻辑相同）。
        /// 不依赖 #if UNITY_EDITOR，确保 Play Mode 下也能正常工作。
        /// cmd=2/3 坐标会在 InitMiningPositions() 中被场景对象覆盖。
        /// </summary>
        private void PopulateDefaultSlotsRuntime()
        {
            var defaults = new (int cmd, UnityEngine.Vector3 center)[]
            {
                (1, new UnityEngine.Vector3(-13.91f, 0f, 11.4f)),  // 鱼塘（渔场实测中心，InitMiningPositions会进一步校正）
                (2, new UnityEngine.Vector3(-3.62f, 0f, 16.2f)),  // 煤矿（实测坐标，InitMiningPositions会进一步校正）
                (3, new UnityEngine.Vector3( 5.81f, 0f, 19.3f)),  // 矿山（实测矿山1坐标）
                (4, new UnityEngine.Vector3(-0.016f, 0f, 1.856f)), // 炉灶（火堆实测中心，InitMiningPositions会进一步校正）
                (6, new UnityEngine.Vector3( 0f,    0f, -4f   )),  // 城门
            };
            // 圆形围绕偏移（半径2.5m，E/W/N/S），比旧的 ±0.8 更贴近资源堆边缘
            UnityEngine.Vector3[] offsets = {
                new( 2.5f, 0,  0f  ), // 东
                new(-2.5f, 0,  0f  ), // 西
                new( 0f,   0,  2.5f), // 北
                new( 0f,   0, -2.5f), // 南
            };
            var slots = new System.Collections.Generic.List<StationSlot>();
            foreach (var (cmd, center) in defaults)
                foreach (var offset in offsets)
                    slots.Add(new StationSlot { cmdType = cmd, position = center + offset });
            _stationSlots = slots.ToArray();
        }

        private void Update()
        {
            // T007：刷新槽位占用状态——Worker工作完成后自动释放槽位
            if (_workerToSlot.Count == 0) return;

            var toFree = new List<WorkerController>();
            foreach (var kvp in _workerToSlot)
            {
                if (kvp.Key == null || !kvp.Key.IsWorking)
                    toFree.Add(kvp.Key);
            }
            foreach (var w in toFree)
            {
                if (_workerToSlot.TryGetValue(w, out int slotIdx)
                    && _stationSlots != null
                    && slotIdx >= 0 && slotIdx < _stationSlots.Length)
                {
                    _stationSlots[slotIdx].occupyingPlayerId = "";
                }
                _workerToSlot.Remove(w);
            }
        }

        // ==================== 公共接口 ====================

        /// <summary>新玩家加入 → 激活一个空闲Worker，分配待机位（无人数上限）</summary>
        public void SpawnWorker(SurvivalPlayerJoinedData data)
        {

            WorkerController worker = GetPooledWorker();
            if (worker == null)
                worker = CreateWorkerAtPos(GetIdlePosition(_activeWorkers.Count));
            else
            {
                worker.gameObject.SetActive(true);
                worker.transform.position = GetIdlePosition(_activeWorkers.Count);
            }

            // T007：分配待机位（每名Worker的固定"家"）
            worker.HomePosition = GetIdlePosition(_activeWorkers.Count);
            worker.Initialize(data.playerId, data.playerName);

            // 激活头顶名字标签（M-NAMETAG）
            var nameTag = worker.GetComponentInChildren<PlayerNameTag>(true);
            if (nameTag != null)
                nameTag.Initialize(data.playerName, data.avatarUrl);

            // 订阅工作完成事件（用于槽位释放）
            worker.OnWorkComplete -= OnWorkerWorkComplete;
            worker.OnWorkComplete += OnWorkerWorkComplete;

            _activeWorkers.Add(worker);
            Debug.Log($"[WorkerManager] Worker#{_activeWorkers.Count} 已激活：{data.playerName}（{data.playerId}）");
        }

        /// <summary>
        /// 分配工作任务：只派遣发出指令的玩家自己的 Worker。
        /// 每个玩家只能控制自己的矿工，不影响其他人的矿工。
        /// </summary>
        public void AssignWork(WorkCommandData cmd)
        {
            if (_activeWorkers.Count == 0) return;

            // 找到发指令玩家对应的 Worker
            var worker = FindWorkerByPlayerId(cmd.playerId);
            if (worker == null)
            {
                Debug.Log($"[WorkerManager] AssignWork: playerId={cmd.playerId} 未找到对应Worker，忽略");
                return;
            }
            if (worker.IsDead)
            {
                Debug.Log($"[WorkerManager] AssignWork: {worker.PlayerName} 已死亡，忽略指令");
                return;
            }

            // 获取该 cmdType 下可用槽位（不含该 Worker 自己当前占用的槽）
            var availableSlots = GetAvailableSlotIndicesExcluding(cmd.commandId, worker);
            if (availableSlots.Count == 0)
            {
                Debug.Log($"[WorkerManager] AssignWork: cmdType={cmd.commandId} 所有槽位已满，忽略");
                return;
            }

            // 释放该 Worker 之前占用的槽位
            if (_workerToSlot.TryGetValue(worker, out int oldSlotIdx)
                && _stationSlots != null && oldSlotIdx >= 0 && oldSlotIdx < _stationSlots.Length)
                _stationSlots[oldSlotIdx].occupyingPlayerId = "";

            // 找距离该 Worker 最近的可用槽位
            int bestSlotIdx = FindNearestSlotToWorker(worker, availableSlots);
            StationSlot slot = _stationSlots[bestSlotIdx];

            slot.occupyingPlayerId = worker.PlayerId;
            _workerToSlot[worker]  = bestSlotIdx;
            worker.AssignWork(cmd.commandId, slot.position);

            Debug.Log($"[WorkerManager] {worker.PlayerName} 自主派遣 → 工位[{bestSlotIdx}]"
                    + $" cmdType={cmd.commandId} pos={slot.position}");
        }

        /// <summary>
        /// 怪物出现时，全员（非死亡）Worker 全部参与防守攻击，不受工位槽数量限制。
        /// 与 EnterNightDefense 逻辑一致，确保无论 phase_changed 是否先到，
        /// 怪物真正出现时所有矿工都投入战斗。
        /// </summary>
        public void OnMonstersAppear()
        {
            if (_activeWorkers.Count == 0) return;

            // 清除所有槽位占用，允许重新分配
            if (_stationSlots != null)
                foreach (var slot in _stationSlots)
                    slot.occupyingPlayerId = "";
            _workerToSlot.Clear();

            // 统计参战人数（用于均匀铺开站位）
            int total = 0;
            foreach (var w in _activeWorkers)
                if (w != null && !w.IsDead) total++;

            int assigned = 0;
            foreach (var w in _activeWorkers)
            {
                if (w == null || w.IsDead) continue;
                w.AssignWork(6, GetDefensePosition(assigned, total));
                w.SetHpBarVisible(true);
                assigned++;
            }
            Debug.Log($"[WorkerManager] OnMonstersAppear: {assigned}/{_activeWorkers.Count} 名Worker全员参战");
        }

        // ==================== 黑夜防守模式 ====================

        /// <summary>
        /// 黑夜开始 → 全员放弃当前工位，集结到城门前防守（cmd=6）。
        /// 不受工位槽位数量限制，所有存活Worker全部参战。
        /// </summary>
        public void EnterNightDefense()
        {
            if (_activeWorkers.Count == 0) return;

            // 清除所有槽位占用（防止旧工位阻塞重分配）
            if (_stationSlots != null)
                foreach (var slot in _stationSlots)
                    slot.occupyingPlayerId = "";
            _workerToSlot.Clear();

            // 统计参战人数（用于均匀铺开站位）
            int total = 0;
            foreach (var w in _activeWorkers)
                if (w != null && !w.IsDead) total++;

            int assigned = 0;
            foreach (var w in _activeWorkers)
            {
                if (w == null || w.IsDead) continue;
                w.AssignWork(6, GetDefensePosition(assigned, total));
                w.SetHpBarVisible(true);   // 夜晚显示矿工血条
                assigned++;
            }
            Debug.Log($"[WorkerManager] EnterNightDefense: {assigned}/{_activeWorkers.Count} 名Worker进入防守模式");
        }

        /// <summary>
        /// 天亮 → 全员退出防守，回到待机圈等待新工作指令。
        /// </summary>
        public void ExitNightDefense()
        {
            // 清除所有槽位占用
            if (_stationSlots != null)
                foreach (var slot in _stationSlots)
                    slot.occupyingPlayerId = "";
            _workerToSlot.Clear();

            foreach (var w in _activeWorkers)
            {
                if (w == null) continue;
                w.SetHpBarVisible(false);  // 白天隐藏矿工血条
                if (w.IsDead) continue;    // 死亡的矿工等服务器 worker_revived 复活后再处理
                w.ResetWorker();           // 回到 Idle，触发 ReturnHomeCoroutine
            }
            Debug.Log("[WorkerManager] ExitNightDefense: 全员恢复白天工作模式");
        }

        /// <summary>服务器通知矿工死亡（worker_died）→ 切换到 Dead 状态并显示倒计时</summary>
        public void HandleWorkerDied(string playerId, long respawnAt)
        {
            var worker = FindWorkerByPlayerId(playerId);
            if (worker == null)
            {
                Debug.LogWarning($"[WorkerManager] HandleWorkerDied: playerId={playerId} 未找到对应Worker");
                return;
            }
            worker.EnterDead(respawnAt);
            Debug.Log($"[WorkerManager] Worker '{playerId}' 已死亡，将在{respawnAt}ms后复活");
        }

        /// <summary>服务器通知矿工复活（worker_revived）→ 立即复活并重新加入战斗</summary>
        public void HandleWorkerRevived(string playerId)
        {
            var worker = FindWorkerByPlayerId(playerId);
            if (worker == null)
            {
                Debug.LogWarning($"[WorkerManager] HandleWorkerRevived: playerId={playerId} 未找到对应Worker");
                return;
            }
            worker.Revive();
            Debug.Log($"[WorkerManager] Worker '{playerId}' 已复活");
        }

        /// <summary>服务器推送矿工HP全量快照 → 更新各Worker血条显示</summary>
        public void HandleWorkerHpUpdate(WorkerHpEntry[] entries)
        {
            foreach (var entry in entries)
            {
                var worker = FindWorkerByPlayerId(entry.playerId);
                if (worker == null) continue;
                worker.SetHp(entry.hp, entry.maxHp);
            }
        }

        /// <summary>按playerId查找活跃Worker</summary>
        private WorkerController FindWorkerByPlayerId(string playerId)
        {
            foreach (var w in _activeWorkers)
                if (w != null && w.PlayerId == playerId) return w;
            return null;
        }

        /// <summary>触发全体Worker金色光晕（666弹幕/主播加速）</summary>
        public void ActivateAllWorkersGlow(float duration = 3f)
        {
            foreach (var w in _activeWorkers)
                if (w != null) w.TriggerSpecial();
        }

        /// <summary>触发全体Worker冻结（捣乱礼物预留接口）</summary>
        public void ActivateAllWorkersFrozen(float duration = 30f)
        {
            foreach (var w in _activeWorkers)
                if (w != null) w.TriggerFrozen();
        }

        /// <summary>暂停所有Worker动画（gift_pause特效期间）</summary>
        public void PauseAllWorkers()
        {
            foreach (var w in _activeWorkers)
                if (w != null) w.SetPaused(true);
        }

        /// <summary>恢复所有Worker动画</summary>
        public void ResumeAllWorkers()
        {
            foreach (var w in _activeWorkers)
                if (w != null) w.SetPaused(false);
        }

        /// <summary>清除所有活跃Worker（游戏结束/重置）</summary>
        public void ClearAll()
        {
            foreach (var w in _activeWorkers)
            {
                if (w == null) continue;
                w.OnWorkComplete -= OnWorkerWorkComplete;
                w.ResetWorker();
                w.gameObject.SetActive(false);
            }
            _activeWorkers.Clear();
            _workerToSlot.Clear();

            // 重置所有槽位占用状态
            if (_stationSlots != null)
                foreach (var slot in _stationSlots)
                    slot.occupyingPlayerId = "";

            // 重置围炉槽计数器，确保下局重新从槽0开始分配
            WorkerController._fireWorkerCount = 0;
        }

        // ==================== T007/T008 私有算法 ====================

        /// <summary>
        /// T007：计算第 index 名Worker的待机位（等间距分布在待机圆圈上）
        /// </summary>
        private Vector3 GetIdlePosition(int workerIndex)
        {
            Vector3 center = _fortressCenter != null ? _fortressCenter.position : Vector3.zero;
            // 圆圈槽数至少 MAX_WORKERS，超出时动态扩展，避免多人时位置重叠
            int totalSlots = Mathf.Max(MAX_WORKERS, workerIndex + 1);
            float angle = (workerIndex / (float)totalSlots) * 360f * Mathf.Deg2Rad;
            return center + new Vector3(Mathf.Cos(angle) * _idleRadius, 0f,
                                        Mathf.Sin(angle) * _idleRadius);
        }

        /// <summary>
        /// 根据参战人数为第 index 名 Worker 生成唯一防守站位。
        /// 沿城门前线横向均匀铺开，每人间距 0.8m，确保无论人数多少都不重叠。
        /// </summary>
        private Vector3 GetDefensePosition(int index, int total)
        {
            // 城门前防守基准点：优先使用 Inspector 绑定的 WorkTarget_Gate
            Vector3 gateCenter;
            if (_gateDefenseCenter != null)
            {
                gateCenter = _gateDefenseCenter.position;
            }
            else
            {
                // Fallback: 从 cmd=6 工位槽中心推算
                gateCenter = new Vector3(0f, 0f, -13.5f);
                if (_stationSlots != null)
                {
                    Vector3 sum = Vector3.zero;
                    int cnt = 0;
                    foreach (var s in _stationSlots)
                        if (s.cmdType == 6) { sum += s.position; cnt++; }
                    if (cnt > 0) gateCenter = sum / cnt;
                }
            }

            const float SPACING = 0.8f;
            float totalWidth = (total - 1) * SPACING;
            float offsetX = index * SPACING - totalWidth * 0.5f;
            return new Vector3(gateCenter.x + offsetX, gateCenter.y, gateCenter.z);
        }

        /// <summary>获取该cmdType下所有未占用的槽位索引列表</summary>
        private List<int> GetAvailableSlotIndices(int cmdType)
        {
            var result = new List<int>();
            if (_stationSlots == null) return result;
            for (int i = 0; i < _stationSlots.Length; i++)
            {
                if (_stationSlots[i].cmdType == cmdType && !_stationSlots[i].IsOccupied)
                    result.Add(i);
            }
            return result;
        }

        /// <summary>
        /// 获取该cmdType下所有未占用的槽位索引，允许该Worker自己当前占用的槽被重新选中。
        /// 用于玩家切换工作指令时不被自己的旧槽阻塞。
        /// </summary>
        private List<int> GetAvailableSlotIndicesExcluding(int cmdType, WorkerController excludeWorker)
        {
            var result = new List<int>();
            if (_stationSlots == null) return result;
            string excludeId = excludeWorker?.PlayerId ?? "";
            for (int i = 0; i < _stationSlots.Length; i++)
            {
                var slot = _stationSlots[i];
                if (slot.cmdType != cmdType) continue;
                // 空闲，或当前占用者就是该 Worker 本人（切换工作时允许重选自己的槽）
                if (!slot.IsOccupied || slot.occupyingPlayerId == excludeId)
                    result.Add(i);
            }
            return result;
        }

        /// <summary>获取当前所有空闲Worker列表（IsWorking==false）</summary>
        private List<WorkerController> GetIdleWorkers()
        {
            var idle = new List<WorkerController>();
            foreach (var w in _activeWorkers)
                if (w != null && !w.IsWorking) idle.Add(w);
            return idle;
        }

        /// <summary>
        /// T008：在候选Worker中找到距离 availableSlots 整体最近的那个（最小最近距离）
        /// </summary>
        private int FindNearestWorkerToSlots(List<WorkerController> workers, List<int> slotIndices)
        {
            int   bestWorkerIdx = 0;
            float bestDist      = float.MaxValue;

            for (int wi = 0; wi < workers.Count; wi++)
            {
                // 用该Worker到所有可用槽的最小距离来评分
                float minDistToAnySlot = float.MaxValue;
                foreach (int si in slotIndices)
                {
                    float d = Vector3.Distance(workers[wi].transform.position, _stationSlots[si].position);
                    if (d < minDistToAnySlot) minDistToAnySlot = d;
                }
                if (minDistToAnySlot < bestDist)
                {
                    bestDist      = minDistToAnySlot;
                    bestWorkerIdx = wi;
                }
            }
            return bestWorkerIdx;
        }

        /// <summary>
        /// T008：找距离指定Worker最近的可用槽位索引
        /// </summary>
        private int FindNearestSlotToWorker(WorkerController worker, List<int> slotIndices)
        {
            int   bestSlotIdx = slotIndices[0];
            float bestDist    = float.MaxValue;

            foreach (int si in slotIndices)
            {
                float d = Vector3.Distance(worker.transform.position, _stationSlots[si].position);
                if (d < bestDist) { bestDist = d; bestSlotIdx = si; }
            }
            return bestSlotIdx;
        }

        /// <summary>Worker工作完成事件回调（Update中也会刷新，这里仅作Log）</summary>
        private void OnWorkerWorkComplete(WorkerController worker)
        {
            Debug.Log($"[WorkerManager] {worker.PlayerName} 工作完成，槽位将在下帧释放");
        }

        // ==================== 私有工具方法 ====================

        private WorkerController GetPooledWorker()
        {
            if (_preCreatedWorkers == null) return null;
            foreach (var w in _preCreatedWorkers)
                if (w != null && !w.gameObject.activeSelf) return w;
            return null;
        }

        private WorkerController CreateWorkerAtPos(Vector3 pos)
        {
            GameObject go;
            // 随机选择两套外观，增加视觉多样性
            var prefabToUse = (_workerPrefab2 != null && UnityEngine.Random.value > 0.5f)
                              ? _workerPrefab2 : _workerPrefab;
            if (prefabToUse != null)
            {
                go = Instantiate(prefabToUse, pos, Quaternion.identity);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.transform.position   = pos;
                go.transform.localScale = new Vector3(0.6f, 0.8f, 0.6f);
                var rend = go.GetComponent<Renderer>();
                if (rend != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                                        ?? Shader.Find("Standard"));
                    mat.color      = new Color(0.95f, 0.95f, 0.95f);
                    rend.material  = mat;
                }
                go.AddComponent<WorkerVisual>();
            }
            go.name = "Worker_Pooled";
            return go.GetComponent<WorkerController>() ?? go.AddComponent<WorkerController>();
        }

        /// <summary>向后兼容：生成 cmdType → 第一槽位坐标 的字典</summary>
        private Dictionary<int, Vector3> _GetWorkPositionsFallback()
        {
            var dict = new Dictionary<int, Vector3>();
            if (_stationSlots == null) return dict;
            foreach (var slot in _stationSlots)
                if (!dict.ContainsKey(slot.cmdType))
                    dict[slot.cmdType] = slot.position;
            return dict;
        }

        // ==================== Editor 工具 ====================

#if UNITY_EDITOR
        /// <summary>
        /// OnValidate：若槽位为空则自动填入默认值；同时重算矿工工位使 Gizmo 在 Edit Mode 也正确包围煤堆
        /// </summary>
        private void OnValidate()
        {
            if (_stationSlots == null || _stationSlots.Length == 0)
                AutoPopulateDefaultSlots();

            // 在编辑器加载/刷新时也执行位置重算，使 Gizmo 在 Edit Mode 显示正确的围矿站位
            InitMiningPositions();
        }

        /// <summary>自动填充默认工位槽（每种类型4个，共20个）</summary>
        public void AutoPopulateDefaultSlots()
        {
            // 每种cmdType的中心坐标 + 4个槽偏移
            var defaults = new (int cmd, Vector3 center)[]
            {
                (1, new Vector3(-13.91f, 0f, 11.4f)), // 鱼塘（渔场实测中心）
                (2, new Vector3(-3.62f, 0f, 16.2f)), // 煤矿（与 InitMiningPositions fallback 一致）
                (3, new Vector3( 5.81f, 0f, 19.3f)), // 矿山（与 InitMiningPositions fallback 一致）
                (4, new Vector3(-0.016f, 0f, 1.856f)), // 炉灶（火堆实测中心）
                (6, new Vector3( 0f, 0f, -4f)),  // 城门
            };

            // 4个槽位偏移（形成小方阵）
            Vector3[] offsets = { new(-0.8f,0, 0.8f), new(0.8f,0,0.8f), new(-0.8f,0,-0.8f), new(0.8f,0,-0.8f) };

            var slots = new List<StationSlot>();
            foreach (var (cmd, center) in defaults)
                foreach (var offset in offsets)
                    slots.Add(new StationSlot { cmdType = cmd, position = center + offset });

            _stationSlots = slots.ToArray();
            Debug.Log($"[WorkerManager] 已自动填充 {_stationSlots.Length} 个默认工位槽位");
        }

        private void OnDrawGizmos()
        {
            if (_stationSlots == null) return;
            foreach (var slot in _stationSlots)
            {
                Color c = GetColorForCmd(slot.cmdType);
                Gizmos.color = slot.IsOccupied ? new Color(c.r, c.g, c.b, 0.4f) : new Color(c.r, c.g, c.b, 0.9f);
                Gizmos.DrawWireSphere(slot.position, 0.4f);
                Gizmos.DrawLine(slot.position, slot.position + Vector3.up * 0.8f);
            }

            // 绘制待机圆圈
            if (_fortressCenter == null) return;
            Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
            int seg = 32;
            Vector3 prev = _fortressCenter.position + new Vector3(_idleRadius, 0, 0);
            for (int i = 1; i <= seg; i++)
            {
                float a = i / (float)seg * 2f * Mathf.PI;
                Vector3 next = _fortressCenter.position
                    + new Vector3(Mathf.Cos(a) * _idleRadius, 0, Mathf.Sin(a) * _idleRadius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
#endif
    }
}
