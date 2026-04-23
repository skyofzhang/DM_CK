using System.Collections;
using UnityEngine;
using DrscfZ.UI;

namespace DrscfZ.Survival
{
    /// <summary>
    /// 单个Worker（奶牛/村民）控制器
    ///
    /// 状态机：
    ///   Idle(home) → Move(ToWork) → Work → Move(ToHome) → Idle
    ///   任意状态 → Special（3s金光）→ 恢复原状态
    ///   任意状态 → Frozen（30s冰冻）→ 恢复原状态
    ///
    /// M4改动（T003/T004/T007/T010）：
    ///   1. AssignWork 改为接受显式 targetPos 参数（解耦 WorkerManager.WORK_POSITIONS）
    ///   2. 移动改为协程 + EaseOutCubic，替代 Vector3.MoveTowards（T004）
    ///   3. 新增 HomePosition：工作结束后平滑返回待机位（T007）
    ///   4. 新增 OnWorkComplete 事件：通知 WorkerManager 释放槽位
    ///   5. Animator 状态同步：复用 AC_Kpbl.controller 参数（Speed/IsPushing），优雅降级（T010）
    ///
    /// 挂载规则：挂在Worker GameObject上（与WorkerVisual同级）。
    /// </summary>
    public class WorkerController : MonoBehaviour
    {
        // ==================== 状态枚举 ====================

        public enum State { Idle, Move, Work, Special, Frozen, Dead, Expedition }

        // ==================== Inspector 引用 ====================

        [Header("视觉组件（Inspector拖入）")]
        [SerializeField] private WorkerVisual _visual;
        [SerializeField] private WorkerBubble _bubble;

        [Header("HP 条（World Space Canvas，夜晚才显示）")]
        [SerializeField] private Transform _hpBarCanvas;
        [SerializeField] private UnityEngine.UI.Image _hpFillImage;
        [SerializeField] private float _hpBarHeightOffset = 1.8f; // 血条距脚底高度（世界单位）

        // 血条颜色：绿色（与怪物红色区分）
        private static readonly UnityEngine.Color _hpColorFull    = new UnityEngine.Color(0.1f, 0.85f, 0.2f); // 满血绿
        private static readonly UnityEngine.Color _hpColorMid     = new UnityEngine.Color(0.9f, 0.85f, 0.0f); // 半血黄
        private static readonly UnityEngine.Color _hpColorLow     = new UnityEngine.Color(1.0f, 0.2f, 0.1f); // 低血红

        // 血条平滑动画
        private float _hpBarTargetFill = 1f;

        /// <summary>当前 HP 相对比例（0~1）——由服务器 worker_hp_update 推送。
        /// §31 Assassin 怪物用其按 HP 升序选目标（比例最低即残血矿工）。</summary>
        public float CurrentHpRatio => _hpBarTargetFill;

        [Header("特效材质")]
        [SerializeField] private Material _hitExplosionMaterial; // 拖入 Mat_ParticleAdd

        // ==================== 公开属性 ====================

        public string PlayerId   { get; private set; }
        public string PlayerName { get; private set; }

        // ==================== §30 矿工成长系统 ====================

        /// <summary>当前矿工等级（1~100，由服务器 worker_level_up 驱动）</summary>
        public int CurrentLevel { get; private set; } = 1;

        /// <summary>当前阶段（1~10，由等级派生）</summary>
        public int CurrentTier => Mathf.Clamp((CurrentLevel - 1) / 10 + 1, 1, 10);

        /// <summary>
        /// 威胁倍率（§30.5：怪物目标选择的权重）
        /// 阶段 1~10 的威胁值：1.0 / 1.3 / 1.7 / 2.2 / 2.8 / 3.4 / 4.0 / 4.6 / 5.2 / 6.0
        /// </summary>
        public float ThreatMultiplier
        {
            get
            {
                float[] threats = { 1.0f, 1.3f, 1.7f, 2.2f, 2.8f, 3.4f, 4.0f, 4.6f, 5.2f, 6.0f };
                return threats[Mathf.Clamp(CurrentTier - 1, 0, 9)];
            }
        }

        /// <summary>Worker 是否正在移动或工作（影响空闲判定）</summary>
        public bool   IsWorking  => _state == State.Work || _state == State.Move;

        /// <summary>Worker 当前是否已死亡</summary>
        public bool   IsDead     => _state == State.Dead;

        /// <summary>当前执行的指令类型（0=无）</summary>
        public int    CurrentCmd { get; private set; }

        /// <summary>
        /// 待机基准位置（由 WorkerManager.SpawnWorker 分配）。
        /// Worker工作结束后会平滑返回此坐标。
        /// </summary>
        public Vector3 HomePosition { get; set; } = Vector3.zero;

        /// <summary>工作完成事件：通知 WorkerManager 释放对应槽位</summary>
        public event System.Action<WorkerController> OnWorkComplete;

        // ==================== 私有状态 ====================

        private State  _state              = State.Idle;
        private State  _stateBeforeSpecial = State.Idle;

        private Vector3 _basePos;    // Idle时的Bob动画基准（跟随HomePosition变化）
        private Vector3 _targetPos;  // 当前移动目标（工位或HomePosition）

        private Coroutine _moveCoroutine;        // 当前移动协程句柄
        private Coroutine _returnHomeCoroutine;  // 返回待机位协程句柄

        // ==================== Animator（T010）====================

        private Animator _animator;

        // --- 参数集 A：AC_Kpbl / AC_DrscfZ_Worker（Speed float + IsPushing bool）---
        private bool _hasSpeed;
        private bool _hasIsPushing;
        private static readonly int Hash_Speed     = Animator.StringToHash("Speed");
        private static readonly int Hash_IsPushing = Animator.StringToHash("IsPushing");

        // --- 参数集 B：kuanggong_05.controller（IsRunning/IsMining/IsIdle bool）------
        // CowWorker.prefab 默认使用 kuanggong_05，参数名与集A不同，需同时支持。
        private bool _hasIsRunning;
        private bool _hasIsMining;
        private bool _hasIsCarrying;
        private bool _hasIsIdle;
        private static readonly int Hash_IsRunning  = Animator.StringToHash("IsRunning");
        private static readonly int Hash_IsMining   = Animator.StringToHash("IsMining");
        private static readonly int Hash_IsCarrying = Animator.StringToHash("IsCarrying");
        private static readonly int Hash_IsIdle     = Animator.StringToHash("IsIdle");

        // ==================== 围炉配置（cmd=4）====================

        /// <summary>火堆世界坐标，由 WorkerManager 在 Awake 中写入</summary>
        public static Vector3 CampfirePosition = Vector3.zero;

        /// <summary>当前围炉 Worker 总数（用于分配槽角度），重置时由 WorkerManager 清零</summary>
        public static int _fireWorkerCount = 0;

        /// <summary>本 Worker 分配到的围炉槽编号（-1=未分配）</summary>
        private int _myFireSlot = -1;

        private const float FIRE_RADIUS             = 1.8f;
        private const float FIRE_IDLE_INTERVAL_MIN  = 5f;
        private const float FIRE_IDLE_INTERVAL_MAX  = 15f;
        private float _nextFireMoveTime              = 0f;

        // ==================== 常量 ====================

        private const float MOVE_SPEED        = 1.5f;  // 移动速度（m/s）自然步行速度
        private const float WORK_DURATION     = 4f;  // 工作时长（秒）
        private const float SPECIAL_DURATION  = 3f;  // 金色光晕时长
        private const float FROZEN_DURATION   = 30f; // 冻结时长
        private const float BOB_AMPLITUDE     = 0.05f;
        private const float BOB_FREQ          = 2f * Mathf.PI; // 2π → 1 Hz
        private const float RETURN_HOME_SPEED = 1.5f;  // 返回待机位速度

        // ==================== 摄像机（Billboard 用）====================

        private Camera _mainCam;

        // ==================== 死亡/复活 ====================

        private long      _respawnAt   = 0;     // Unix毫秒时间戳
        private Coroutine _deadCoroutine = null;

        // ==================== 子任务1：攻击距离 ====================

        private const float ATTACK_RANGE = 2.5f;
        private const int   WORKER_ATTACK_DAMAGE = 5; // 矿工打怪每次命中伤害

        // ==================== 子任务3：攻击拖尾 ====================

        private TrailRenderer _attackTrail;

        // ==================== 初始化 ====================

        private void Start()
        {
            _mainCam = Camera.main;
        }

        private void LateUpdate()
        {
            // HPBarCanvas 是根对象子节点，动画的根节点旋转会带动其世界坐标偏移。
            // 在 LateUpdate 中同时覆写世界位置（锁定到脚底正上方）和旋转（Billboard），彻底解耦动画影响。
            if (_hpBarCanvas != null && _hpBarCanvas.gameObject.activeSelf)
            {
                if (_mainCam == null) _mainCam = Camera.main;
                // 位置：始终在根节点（脚底）正上方固定高度
                _hpBarCanvas.position = transform.position + Vector3.up * _hpBarHeightOffset;
                // 旋转：始终面向摄像机
                if (_mainCam != null)
                    _hpBarCanvas.rotation = Quaternion.LookRotation(
                        _hpBarCanvas.position - _mainCam.transform.position);
            }
        }

        private void Awake()
        {
            if (_visual == null) _visual = GetComponent<WorkerVisual>();
            if (_bubble == null) _bubble = GetComponentInChildren<WorkerBubble>(true);

            // 最早时机：强制隐藏 HPBarCanvas（白天 / 未初始化状态下不应显示任何血条方块）
            // 优先使用 Inspector 绑定的引用，否则运行时查找
            if (_hpBarCanvas == null)
                _hpBarCanvas = transform.Find("HPBarCanvas");
            if (_hpBarCanvas != null)
                _hpBarCanvas.gameObject.SetActive(false);

            // T010：绑定子对象 CowWorker.Body 上的 Animator（Body 由 FixWorkerMesh 挂载）
            _animator = GetComponentInChildren<Animator>(true);
            if (_animator != null)
            {
                _animator.applyRootMotion = false;                           // 禁止根运动
                _animator.cullingMode = AnimatorCullingMode.CullCompletely;  // 离开视野停止动画计算
            }
            CacheAnimatorParams();

            // ── 性能优化：关闭阴影投射 + 视野外停止蒙皮（Shadow casters 是主要瓶颈）
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.shadowCastingMode   = UnityEngine.Rendering.ShadowCastingMode.Off;
                smr.updateWhenOffscreen = false;
            }

            // 子任务3：初始化攻击拖尾
            SetupAttackTrail();
        }

        /// <summary>激活Worker并绑定玩家信息（WorkerManager.SpawnWorker 调用）</summary>
        public void Initialize(string playerId, string playerName)
        {
            PlayerId    = playerId;
            PlayerName  = playerName;
            CurrentCmd  = 0;
            _basePos    = HomePosition != Vector3.zero ? HomePosition : transform.position;

            // 自动绑定 HPBarCanvas（若 Inspector 未拖入则运行时查找）
            if (_hpBarCanvas == null)
                _hpBarCanvas = transform.Find("HPBarCanvas");
            if (_hpBarCanvas != null)
            {
                // 强制重置血条 Canvas 缩放（与 MonsterController 保持一致）
                _hpBarCanvas.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                // 每次 Initialize 都强制重新绑定，防止 Prefab 实例化时 Inspector 引用丢失
                var hpFillTr = _hpBarCanvas.Find("HPFill");
                if (hpFillTr != null)
                    _hpFillImage = hpFillTr.GetComponent<UnityEngine.UI.Image>();
            }
            // 白天默认隐藏血条
            SetHpBarVisible(false);
            // 初始化时立即置满（跳过 Lerp，避免从0渐变到满血）
            _hpBarTargetFill = 1f;
            if (_hpFillImage != null)
                _hpFillImage.fillAmount = 1f;

            TransitionTo(State.Idle);
        }

        /// <summary>控制矿工头顶血条显示（白天隐藏，夜晚显示）</summary>
        public void SetHpBarVisible(bool visible)
        {
            if (_hpBarCanvas != null)
                _hpBarCanvas.gameObject.SetActive(visible);
        }

        /// <summary>更新矿工血量显示（由服务器 worker_hp 消息驱动）</summary>
        public void SetHp(int current, int max)
        {
            _hpBarTargetFill = max > 0 ? Mathf.Clamp01((float)current / max) : 1f;
            RebindHpFillIfNeeded();
            // 立即同步 fillAmount（不等 Lerp 下帧生效）
            if (_hpFillImage != null)
            {
                _hpFillImage.fillAmount = _hpBarTargetFill;
                // 颜色随血量变化
                float ratio = _hpBarTargetFill;
                UnityEngine.Color c = ratio > 0.5f
                    ? UnityEngine.Color.Lerp(_hpColorMid, _hpColorFull, (ratio - 0.5f) * 2f)
                    : UnityEngine.Color.Lerp(_hpColorLow, _hpColorMid, ratio * 2f);
                _hpFillImage.color = c;
            }
        }

        /// <summary>兜底重绑定：若 _hpFillImage 意外为 null，尝试从层次结构中重新查找</summary>
        private void RebindHpFillIfNeeded()
        {
            if (_hpFillImage != null) return;
            if (_hpBarCanvas == null)
                _hpBarCanvas = transform.Find("HPBarCanvas");
            if (_hpBarCanvas != null)
            {
                var hpFillTr = _hpBarCanvas.Find("HPFill");
                if (hpFillTr != null)
                    _hpFillImage = hpFillTr.GetComponent<UnityEngine.UI.Image>();
                if (_hpFillImage != null)
                    Debug.LogWarning($"[WorkerController] {name}: _hpFillImage was null, rebound to {_hpFillImage.name}");
            }
        }

        /// <summary>每帧平滑推进血条 fillAmount（在 Update 中调用）</summary>
        private void UpdateHpBarSmooth()
        {
            RebindHpFillIfNeeded();
            if (_hpFillImage == null) return;
            _hpFillImage.fillAmount = Mathf.Lerp(_hpFillImage.fillAmount, _hpBarTargetFill, Time.deltaTime * 10f);
        }

        // ==================== Update 主循环 ====================

        private void Update()
        {
            switch (_state)
            {
                case State.Idle:
                    UpdateIdle();
                    break;
                case State.Work:
                    // cmd=4 围炉：到达槽位后持续朝向火堆，并定时轻微换位
                    if (CurrentCmd == 4)
                        UpdateFireIdle();
                    break;
                // Move 和 Special/Frozen 完全由协程驱动，Update不处理
            }
            // 血条平滑动画（每帧推进，无论状态）
            UpdateHpBarSmooth();
        }

        private void UpdateIdle()
        {
            // Y轴Bob浮动：围绕 _basePos 上下振荡
            float bobY = Mathf.Sin(Time.time * BOB_FREQ) * BOB_AMPLITUDE;
            transform.position = _basePos + Vector3.up * bobY;
        }

        /// <summary>
        /// cmd=4 围炉专用更新：持续朝向火堆中心，定时轻微换位（5~15秒一次）。
        /// 在 State.Work 且 CurrentCmd==4 时每帧调用。
        /// </summary>
        private void UpdateFireIdle()
        {
            // 朝向火堆（仅Y轴旋转，避免角色倾斜）
            Vector3 toFire = CampfirePosition - transform.position;
            toFire.y = 0f;
            if (toFire.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(toFire),
                    Time.deltaTime * 5f);

            // 定时轻微换位：重新计算槽角度并移动
            if (Time.time >= _nextFireMoveTime)
            {
                _nextFireMoveTime = Time.time + Random.Range(FIRE_IDLE_INTERVAL_MIN, FIRE_IDLE_INTERVAL_MAX);

                // 在当前槽基础上加随机小偏移（±0.3m），增加自然感
                float baseAngle  = _myFireSlot * (360f / 8f) * Mathf.Deg2Rad;
                float jitter     = Random.Range(-0.25f, 0.25f);
                float angle      = baseAngle + jitter;
                Vector3 newSlot  = CampfirePosition + new Vector3(
                    Mathf.Cos(angle) * FIRE_RADIUS, 0f, Mathf.Sin(angle) * FIRE_RADIUS);

                // 用短移动协程平滑换位（不改变 State，Worker 仍处于 Work 状态）
                if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
                _moveCoroutine = StartCoroutine(FireDriftCoroutine(newSlot));
            }
        }

        /// <summary>围炉微移协程：短距离平滑漂移到新槽位（不切换状态）</summary>
        private IEnumerator FireDriftCoroutine(Vector3 targetPos)
        {
            Vector3 startPos = transform.position;
            float   distance = Vector3.Distance(startPos, targetPos);
            float   duration = Mathf.Clamp(distance / (MOVE_SPEED * 0.5f), 0.3f, 3f);
            float   elapsed  = 0f;

            while (elapsed < duration && _state == State.Work && CurrentCmd == 4)
            {
                elapsed += Time.deltaTime;
                float t = EaseInOutCubic(Mathf.Clamp01(elapsed / duration));
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }
            _moveCoroutine = null;
        }

        // ==================== 状态切换 ====================

        private void TransitionTo(State newState)
        {
            ExitState(_state);
            _state = newState;
            EnterState(newState);
        }

        private void EnterState(State state)
        {
            switch (state)
            {
                case State.Idle:
                    transform.localRotation = Quaternion.identity;
                    if (_bubble != null) _bubble.Hide();

                    // 平滑返回待机位（T007：Worker工作结束后回到HomePosition）
                    Vector3 home = HomePosition != Vector3.zero ? HomePosition : transform.position;
                    if (Vector3.Distance(transform.position, home) > 0.1f)
                    {
                        // 先播行走动画，协程结束后切回 Idle
                        SetAnimatorState(true, false);
                        if (_returnHomeCoroutine != null) StopCoroutine(_returnHomeCoroutine);
                        _returnHomeCoroutine = StartCoroutine(ReturnHomeCoroutine(home));
                    }
                    else
                    {
                        _basePos = home;
                        SetAnimatorState(false, false);
                    }
                    break;

                case State.Move:
                    transform.localRotation = Quaternion.identity;
                    if (_bubble != null) _bubble.ShowWork(CurrentCmd);
                    SetAnimatorState(true, false);   // T010：Speed=3，IsPushing=false
                    // 协程由 AssignWork 启动
                    break;

                case State.Work:
                    if (_visual != null) _visual.SetWorkColor(CurrentCmd);
                    if (_bubble != null) _bubble.ShowWork(CurrentCmd);
                    // cmd=4（围炉）：站立朝向火堆，不播 Mining 动画
                    // cmd=1（食物）：靠近渔场站立，同样是 Idle 姿态
                    // cmd=2/3（煤/矿）：挖掘动画 IsMining=true
                    // cmd=6（打怪）：用 IsMining 替代战斗动画（现有参数集内最接近）
                    SetAnimatorStateForCmd(CurrentCmd);
                    StartCoroutine(WorkCoroutine());
                    break;

                case State.Special:
                    if (_visual != null) _visual.ActivateGlow(SPECIAL_DURATION);
                    if (_bubble != null) _bubble.ShowSpecial("★", new Color(1f, 0.843f, 0f, 0.9f));
                    Invoke(nameof(OnSpecialEnd), SPECIAL_DURATION);
                    break;

                case State.Frozen:
                    if (_visual != null) _visual.ActivateFrozen(FROZEN_DURATION);
                    if (_bubble != null) _bubble.ShowSpecial("冰", new Color(0.533f, 0.8f, 1.0f, 0.9f));
                    Invoke(nameof(OnFrozenEnd), FROZEN_DURATION);
                    break;

                case State.Dead:
                    StopAllCoroutines();
                    SetAnimatorState(false, false);
                    if (_visual != null) _visual.SetFrozen(true);  // 灰色冻结外观复用为死亡外观
                    if (_bubble != null) _bubble.ShowSpecial("倒计时", new Color(0.4f, 0.4f, 0.4f, 0.9f));
                    if (_deadCoroutine != null) StopCoroutine(_deadCoroutine);
                    _deadCoroutine = StartCoroutine(DeadCoroutine());
                    break;

                case State.Expedition:
                    // 停止 Work/Move 协程；头顶气泡提示；GO 可能已被 WorkerManager SetActive(false)
                    StopAllCoroutines();
                    SetAnimatorState(false, false);
                    if (_bubble != null) _bubble.ShowSpecial("探", new Color(0.6f, 0.85f, 1f, 0.9f));
                    break;
            }
        }

        private void ExitState(State state)
        {
            switch (state)
            {
                case State.Move:
                    StopMoveCoroutine();
                    break;
                case State.Work:
                    StopAllCoroutines(); // 停掉 WorkCoroutine（可能被打断）
                    transform.localRotation = Quaternion.identity;
                    break;
                case State.Special:
                    CancelInvoke(nameof(OnSpecialEnd));
                    break;
                case State.Frozen:
                    CancelInvoke(nameof(OnFrozenEnd));
                    // 🆕 §31 动态冻结协程也要清理（Freeze/Unfreeze 手动也会 Stop；这里兜底）
                    if (_freezeCoroutine != null)
                    {
                        StopCoroutine(_freezeCoroutine);
                        _freezeCoroutine = null;
                    }
                    break;

                case State.Dead:
                    if (_deadCoroutine != null) { StopCoroutine(_deadCoroutine); _deadCoroutine = null; }
                    if (_visual != null) _visual.SetFrozen(false);
                    if (_bubble != null) _bubble.Hide();
                    break;

                case State.Expedition:
                    if (_bubble != null) _bubble.Hide();
                    break;
            }
        }

        // ==================== 协程 ====================

        /// <summary>
        /// 平滑移动到目标点（T004：EaseOutCubic 插值，替代 MoveTowards）。
        /// 到达后自动切换到 State.Work。
        /// </summary>
        private IEnumerator MoveToWorkCoroutine(Vector3 targetPos)
        {
            // 墙壁绕行：如果目标在墙另一侧，先走到城门口
            Vector3? detour = WallBarrier.GetDetourPoint(transform.position, targetPos);
            if (detour.HasValue)
            {
                yield return MoveSegment(detour.Value);
            }

            yield return MoveSegment(targetPos);

            transform.position = targetPos;
            transform.localRotation = Quaternion.identity;

            if (_state == State.Move)
                TransitionTo(State.Work);
        }

        /// <summary>平滑移动到某个中间/最终点（EaseOutCubic）</summary>
        private IEnumerator MoveSegment(Vector3 targetPos)
        {
            Vector3 startPos = transform.position;
            float   distance = Vector3.Distance(startPos, targetPos);
            float   duration = Mathf.Clamp(distance / MOVE_SPEED, 0.3f, 30f);
            float   elapsed  = 0f;

            while (elapsed < duration && _state == State.Move)
            {
                elapsed += Time.deltaTime;
                float t  = EaseOutCubic(Mathf.Clamp01(elapsed / duration));
                transform.position = Vector3.Lerp(startPos, targetPos, t);

                Vector3 dir = new Vector3(targetPos.x - transform.position.x,
                                          0f,
                                          targetPos.z - transform.position.z);
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(dir);

                yield return null;
            }
        }

        /// <summary>工作计时器（cmd=6 持续战斗直到怪物全灭；其他指令 4s 后回 Idle）</summary>
        private IEnumerator WorkCoroutine()
        {
            // ── cmd=6：持续战斗循环，直到场上无活跃怪物 ──────────────────────────
            if (CurrentCmd == 6)
            {
                if (_attackTrail != null) _attackTrail.enabled = true;

                while (_state == State.Work)
                {
                    // 1. 找最近活跃怪物
                    var target = FindNearestActiveMonster();
                    if (target == null)
                    {
                        // 暂时无怪（波次间隔 / 怪物正在生成）→ 在防守位待命，不退出
                        SetAnimatorState(false, false);  // 切回 Idle 姿势（原地待命）
                        yield return new WaitForSeconds(0.3f);
                        continue;
                    }

                    // 2. 追到攻击范围内
                    bool wasChasing = false;
                    while (target != null && target.gameObject.activeInHierarchy &&
                           Vector3.Distance(transform.position, target.position) > ATTACK_RANGE &&
                           _state == State.Work)
                    {
                        if (!wasChasing)
                        {
                            SetAnimatorState(true, false);   // 只在开始追击时切一次行走动画
                            wasChasing = true;
                        }
                        var dir = (target.position - transform.position).normalized;
                        dir.y = 0f;
                        Vector3 newPos = transform.position + dir * MOVE_SPEED * Time.deltaTime;
                        transform.position = WallBarrier.ClampMovement(transform.position, newPos);
                        if (dir.sqrMagnitude > 0.001f)
                            transform.rotation = Quaternion.LookRotation(dir);
                        yield return null;
                    }

                    if (target == null || !target.gameObject.activeInHierarchy) continue;

                    // 3. 进入攻击姿态
                    SetAnimatorState(false, false);
                    SetAnimatorStateForCmd(6);
                    Vector3 toM = target.position - transform.position;
                    toM.y = 0f;
                    if (toM.sqrMagnitude > 0.001f)
                        transform.rotation = Quaternion.LookRotation(toM);

                    // 4. 近身打击：持续攻击直到目标死亡或消失，每 ATTACK_INTERVAL 秒命中一次
                    const float ATTACK_INTERVAL = 1.2f;
                    float nextHitTime = Time.time + 0.3f; // 首次命中快一些
                    while (_state == State.Work)
                    {
                        if (target == null) break;
                        var mc = target.GetComponent<DrscfZ.Monster.MonsterController>();
                        if (mc == null || mc.IsDead) break;

                        // 如果目标走远了，重新追
                        if (Vector3.Distance(transform.position, target.position) > ATTACK_RANGE + 1.5f)
                            break;

                        // 面朝目标（3D 骨骼攻击动画已包含挥砍效果，不叠加 Z 轴旋转）
                        Vector3 toTarget = target.position - transform.position;
                        toTarget.y = 0f;
                        if (toTarget.sqrMagnitude > 0.001f)
                            transform.rotation = Quaternion.LookRotation(toTarget);

                        // 定时命中
                        if (Time.time >= nextHitTime)
                        {
                            if (Vector3.Distance(transform.position, target.position) <= ATTACK_RANGE + 1f)
                            {
                                SpawnHitExplosion(target.position + Vector3.up * 0.5f);
                                mc.TakeDamage(WORKER_ATTACK_DAMAGE);
                            }
                            nextHitTime = Time.time + ATTACK_INTERVAL;
                        }
                        yield return null;
                    }
                    // 目标死亡/消失 → 下一轮重新 FindNearestActiveMonster
                }

                // 所有怪物已死，关闭拖尾
                if (_attackTrail != null)
                {
                    yield return new WaitForSeconds(0.3f);
                    _attackTrail.enabled = false;
                }

                if (_state == State.Work)
                {
                    OnWorkComplete?.Invoke(this);
                    TransitionTo(State.Idle);
                }
                yield break;  // cmd=6 流程结束，跳过下方通用逻辑
            }

            // ── 其他 cmd（1/2/3/4）：固定 4 秒工作计时器 ────────────────────────
            float endTime = Time.time + WORK_DURATION;

            while (Time.time < endTime && _state == State.Work)
            {
                // 3D 骨骼动画已包含工具挥动效果（IsMining 状态），不需要额外 Z 轴旋转
                yield return null;
            }

            if (_state == State.Work)
            {
                OnWorkComplete?.Invoke(this);
                TransitionTo(State.Idle);
            }
        }

        /// <summary>平滑返回待机位（EaseInOutCubic，比工作移动稍快）</summary>
        private IEnumerator ReturnHomeCoroutine(Vector3 homePos)
        {
            Vector3 startPos = transform.position;
            float   distance = Vector3.Distance(startPos, homePos);
            float   duration = Mathf.Clamp(distance / RETURN_HOME_SPEED, 0.2f, 30f);
            float   elapsed  = 0f;

            while (elapsed < duration && _state == State.Idle)
            {
                elapsed += Time.deltaTime;
                float t = EaseInOutCubic(Mathf.Clamp01(elapsed / duration));
                _basePos = Vector3.Lerp(startPos, homePos, t);

                // 面朝移动方向
                Vector3 dir = new Vector3(homePos.x - transform.position.x, 0f, homePos.z - transform.position.z);
                if (dir.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 8f);

                yield return null;
            }

            _basePos = homePos;
            _returnHomeCoroutine = null;

            // 到达后切 Idle 动画 + 重置朝向
            if (_state == State.Idle)
            {
                transform.localRotation = Quaternion.identity;
                SetAnimatorState(false, false);
            }
        }

        /// <summary>死亡倒计时协程：显示泡泡倒计时直到 _respawnAt（由服务器 worker_revived 提前打断）</summary>
        private IEnumerator DeadCoroutine()
        {
            while (_state == State.Dead)
            {
                if (_respawnAt > 0)
                {
                    long remaining = _respawnAt - System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    if (remaining <= 0) { yield break; } // 等服务器 worker_revived 消息
                    int secs = Mathf.Max(0, Mathf.CeilToInt(remaining / 1000f));
                    if (_bubble != null) _bubble.ShowSpecial($"{secs}s", new Color(0.4f, 0.4f, 0.4f, 0.9f));
                }
                yield return new WaitForSeconds(0.5f);
            }
            _deadCoroutine = null;
        }

        // ==================== 特殊状态回调 ====================

        private void OnSpecialEnd()
        {
            if (_visual != null) _visual.Reset();
            _state = _stateBeforeSpecial;
            EnterState(_state);
        }

        private void OnFrozenEnd()
        {
            if (_visual != null) _visual.Reset();
            _state = _stateBeforeSpecial;
            EnterState(_state);
        }

        // ==================== 公共接口 ====================

        /// <summary>
        /// 分配工作指令，Worker平滑移向指定工位坐标（T003：接受显式targetPos解耦WorkerManager）。
        /// cmd=4 时额外分配围炉槽位，Worker走向围炉圆圈上的固定位置。
        /// </summary>
        /// <param name="cmdType">工作类型（1=食物/2=煤/3=矿/4=火/6=打怪）</param>
        /// <param name="targetPos">目标世界坐标（由WorkerManager从StationSlot选出；cmd=4时被围炉计算覆盖）</param>
        public void AssignWork(int cmdType, Vector3 targetPos)
        {
            _visual?.TriggerAssignmentFlash(); // T214：指派时白色闪烁
            CurrentCmd  = cmdType;

            // cmd=4 围炉：按静态槽计数分配围炉圆圈上的固定位置，忽略 WorkerManager 传入的 targetPos
            if (cmdType == 4)
            {
                _myFireSlot = _fireWorkerCount++;
                float angle  = _myFireSlot * (360f / 8f) * Mathf.Deg2Rad;
                targetPos    = CampfirePosition + new Vector3(
                    Mathf.Cos(angle) * FIRE_RADIUS, 0f, Mathf.Sin(angle) * FIRE_RADIUS);
                _nextFireMoveTime = Time.time + Random.Range(FIRE_IDLE_INTERVAL_MIN, FIRE_IDLE_INTERVAL_MAX);
            }
            else
            {
                _myFireSlot = -1;
            }

            _targetPos  = targetPos;

            StopMoveCoroutine();
            if (_returnHomeCoroutine != null)
            {
                StopCoroutine(_returnHomeCoroutine);
                _returnHomeCoroutine = null;
            }

            TransitionTo(State.Move);
            _moveCoroutine = StartCoroutine(MoveToWorkCoroutine(targetPos));
        }

        /// <summary>触发Special状态（666/主播加速），3秒后恢复原状态</summary>
        public void TriggerSpecial()
        {
            if (_state == State.Special || _state == State.Frozen) return;
            _stateBeforeSpecial = _state;
            TransitionTo(State.Special);
        }

        /// <summary>触发Frozen状态，30秒后恢复原状态</summary>
        public void TriggerFrozen()
        {
            if (_state == State.Frozen) return;
            _stateBeforeSpecial = _state;
            TransitionTo(State.Frozen);
        }

        // ==================== §31 个人动态冻结（冰封怪命中）====================

        private Coroutine _freezeCoroutine;

        /// <summary>
        /// 🆕 §31 进入 Frozen 状态（动态时长）。
        /// 与 TriggerFrozen() 区别：
        ///   - 不触发 FrozenStatusUI 全局横幅（策划案 §31.3：个人冻结不与全局事件冲突）
        ///   - 气泡文字显示"冻结 Xs"倒计时
        ///   - duration 秒后自动 Unfreeze()（路径独立于 OnFrozenEnd 的 30s 固定 Invoke）
        /// 服务端 worker_unfrozen 抵达时会主动调 Unfreeze() 打断此协程（如 T4 提前解冻）。
        /// </summary>
        public void Freeze(float durationSec)
        {
            if (_state == State.Frozen)
            {
                // 已在 Frozen：仅重置倒计时（取最大时长）
                if (_freezeCoroutine != null) StopCoroutine(_freezeCoroutine);
                _freezeCoroutine = StartCoroutine(DynamicFreezeCoroutine(durationSec));
                return;
            }

            _stateBeforeSpecial = _state;

            // 切 Frozen 前先 Cancel 固定 30s Invoke（避免 EnterState(Frozen) 又挂 30s 定时）
            CancelInvoke(nameof(OnFrozenEnd));
            _state = State.Frozen;

            // 只应用视觉（蓝色材质），不调用 ShowSpecial("冰")——改用动态倒计时文字
            if (_visual != null) _visual.SetFrozen(true);
            if (_bubble  != null) _bubble.ShowSpecial($"冻结 {Mathf.CeilToInt(durationSec)}s",
                                                      new Color(0.533f, 0.8f, 1.0f, 0.9f));

            if (_freezeCoroutine != null) StopCoroutine(_freezeCoroutine);
            _freezeCoroutine = StartCoroutine(DynamicFreezeCoroutine(durationSec));
        }

        /// <summary>🆕 Fix C (组 B Reviewer P0) §34B B3 morale_boost：
        ///   在矿工头顶显示自定义气泡文字 durationSec 秒后自动 Hide。
        ///   不覆盖 Frozen / Dead 状态的气泡（由对应状态协程/Invoke 管理）。
        ///   复用 WorkerBubble.ShowSpecial（橙黄底色，表示鼓舞）。</summary>
        public void ShowBubbleText(string text, float durationSec)
        {
            if (_bubble == null) return;
            if (_state == State.Frozen || _state == State.Dead) return;
            _bubble.ShowSpecial(text, new Color(1.0f, 0.78f, 0.2f, 0.9f));
            // 用 CancelInvoke+Invoke 实现独占定时，避免多次叠加导致提前 Hide
            CancelInvoke(nameof(HideBubbleIfMorale));
            Invoke(nameof(HideBubbleIfMorale), Mathf.Max(0.1f, durationSec));
        }

        private void HideBubbleIfMorale()
        {
            // 仅当当前未处于 Frozen / Dead / 工作态时隐藏（工作态气泡由 EnterState 重新 ShowWork）
            if (_state == State.Frozen || _state == State.Dead) return;
            if (_bubble != null && _state != State.Work && _state != State.Move)
                _bubble.Hide();
        }

        /// <summary>
        /// 🆕 §31 解除 Frozen 状态（由 worker_unfrozen 协议或倒计时到期触发）。
        /// 从 Frozen 回到进入前状态（_stateBeforeSpecial），与 OnFrozenEnd 路径统一。
        /// </summary>
        public void Unfreeze()
        {
            if (_state != State.Frozen) return;

            if (_freezeCoroutine != null)
            {
                StopCoroutine(_freezeCoroutine);
                _freezeCoroutine = null;
            }
            CancelInvoke(nameof(OnFrozenEnd));

            // 恢复视觉 + 清气泡
            if (_visual != null) _visual.Reset();
            if (_bubble != null) _bubble.Hide();

            // 复用 OnFrozenEnd 路径：切回 _stateBeforeSpecial
            _state = _stateBeforeSpecial;
            EnterState(_state);
        }

        private IEnumerator DynamicFreezeCoroutine(float durationSec)
        {
            float remaining = durationSec;
            while (remaining > 0f && _state == State.Frozen)
            {
                remaining -= Time.deltaTime;
                if (remaining < 0f) remaining = 0f;
                // 每 0.25s 刷一次气泡倒计时
                if (_bubble != null)
                    _bubble.ShowSpecial($"冻结 {Mathf.CeilToInt(remaining)}s",
                                        new Color(0.533f, 0.8f, 1.0f, 0.9f));
                yield return new WaitForSeconds(0.25f);
            }
            _freezeCoroutine = null;
            if (_state == State.Frozen) Unfreeze();
        }

        /// <summary>服务器通知矿工死亡（夜间HP归零）</summary>
        /// <param name="respawnAtMs">复活时间点（Unix毫秒），0=不计时</param>
        public void EnterDead(long respawnAtMs)
        {
            if (_state == State.Dead) return;
            _respawnAt = respawnAtMs;
            TransitionTo(State.Dead);
        }

        /// <summary>服务器通知矿工复活（礼物/天亮）</summary>
        public void Revive()
        {
            if (_state != State.Dead) return;
            _respawnAt = 0;
            TransitionTo(State.Idle);
            // 复活后自动参与战斗（若夜晚还有怪）
            var monster = FindNearestActiveMonster();
            if (monster != null)
                AssignWork(6, monster.position);
        }

        /// <summary>暂停/恢复Worker视觉（gift_pause特效期间）</summary>
        public void SetPaused(bool paused)
        {
            _visual?.SetFrozen(paused);
        }

        // ==================== §38 探险状态（Batch I 补齐） ====================

        /// <summary>
        /// 矿工出发探险（expedition_started）——策划案 §22.1。
        /// MVP 行为：切 State.Expedition + 头顶气泡 "探险中"。
        /// 注意：WorkerManager.HandleExpeditionStarted 同时会 SetActive(false) 让整个 GO 隐形，
        /// 本方法的视觉仅作为一个兜底，若场景改为不隐 GO，本状态仍能显示正确。
        /// </summary>
        public void EnterExpedition()
        {
            if (_state == State.Expedition) return;
            TransitionTo(State.Expedition);
        }

        /// <summary>
        /// 矿工探险归来（expedition_returned）——回到 Idle 流程。
        /// 若探险中阵亡，WorkerManager 会直接调 EnterDead，不走本方法。
        /// </summary>
        public void ExitExpedition()
        {
            if (_state != State.Expedition) return;
            TransitionTo(State.Idle);
        }

        // ==================== §30 矿工成长系统 ====================

        /// <summary>
        /// 服务器通知本矿工升级（worker_level_up）
        /// - 更新 CurrentLevel / CurrentTier
        /// - 刷新头顶名字标签（Lv 前缀 + 阶段颜色）
        /// - 跨阶段时触发白色闪光
        /// - TODO §30：newTier 达到阶段升级时切皮肤模型（目前仅改 NameTag 颜色）
        /// </summary>
        public void HandleWorkerLevelUp(WorkerLevelUpData data)
        {
            if (data == null || PlayerId != data.playerId) return;

            int prevTier = CurrentTier;
            CurrentLevel = Mathf.Clamp(data.newLevel, 1, 100);

            // 刷新头顶名字标签（Lv 前缀 + 阶段颜色）
            var tag = GetComponentInChildren<PlayerNameTag>(true);
            if (tag != null) tag.SetLevel(data.newLevel, data.newTier);

            // 跨阶段 → 白色闪光提示（复用现有 AssignmentFlash）
            if (data.newTier != prevTier && _visual != null)
                _visual.TriggerAssignmentFlash();

            // 切阶段皮肤（MVP 仅改颜色）
            SetTierSkin(data.newTier);
        }

        /// <summary>
        /// 服务器通知传奇矿工触发免死（legend_revive_triggered）
        /// MVP：复用现有金色光晕（TriggerSpecial）作为免死特效占位
        /// </summary>
        public void HandleLegendRevive()
        {
            // 金色光晕（3s）作为免死闪光
            if (_state != State.Special && _state != State.Frozen)
            {
                _stateBeforeSpecial = _state;
                TransitionTo(State.Special);
            }
        }

        /// <summary>
        /// 切换阶段皮肤（MVP：仅刷新 NameTag 颜色；实际模型切换待策划案 §30.11 SwapSkinModel）
        /// TODO §30：未来挂载 Assets/Prefabs/WorkerSkins/WorkerSkin_T{tier:D2}.prefab（美术待制作）
        /// </summary>
        public void SetTierSkin(int tier)
        {
            var tag = GetComponentInChildren<PlayerNameTag>(true);
            if (tag != null) tag.SetTier(tier);
        }

        /// <summary>重置Worker到Idle状态（归还对象池时调用）</summary>
        public void ResetWorker()
        {
            CancelInvoke();
            StopAllCoroutines();
            _moveCoroutine       = null;
            _returnHomeCoroutine = null;
            _deadCoroutine       = null;
            _freezeCoroutine     = null;   // 🆕 §31 动态冻结协程也要清理

            // 若该 Worker 持有围炉槽位，归还计数（静态计数器由 WorkerManager.ClearAll 统一清零）
            _myFireSlot = -1;

            // 子任务3：确保拖尾禁用
            if (_attackTrail != null) _attackTrail.enabled = false;

            _visual?.Reset();
            _state     = State.Idle;
            CurrentCmd = 0;
            transform.localRotation = Quaternion.identity;
            _bubble?.Hide();
        }

        // ==================== Animator 辅助（T010）====================

        /// <summary>
        /// 缓存 AC_Kpbl.controller 的参数，优雅降级（参数不存在时静默忽略）。
        /// 与 Capybara.cs 保持一致的模式。
        /// </summary>
        private void CacheAnimatorParams()
        {
            if (_animator == null || _animator.runtimeAnimatorController == null) return;
            foreach (var p in _animator.parameters)
            {
                switch (p.nameHash)
                {
                    // 集A
                    case var h when h == Hash_Speed:      _hasSpeed      = true; break;
                    case var h when h == Hash_IsPushing:  _hasIsPushing  = true; break;
                    // 集B (kuanggong_05)
                    case var h when h == Hash_IsRunning:  _hasIsRunning  = true; break;
                    case var h when h == Hash_IsMining:   _hasIsMining   = true; break;
                    case var h when h == Hash_IsCarrying: _hasIsCarrying = true; break;
                    case var h when h == Hash_IsIdle:     _hasIsIdle     = true; break;
                }
            }
        }

        /// <summary>
        /// 同步 Animator 参数到当前状态（兼容两套参数集）。
        /// 集A：Speed(float) + IsPushing(bool) → 用于 AC_Kpbl / AC_DrscfZ_Worker
        /// 集B：IsRunning/IsMining/IsIdle(bool) → 用于 kuanggong_05.controller（CowWorker 默认）
        /// isMoving=true  → 行走动画
        /// isWorking=true → 工作动画（IsMining）
        /// 两者均false     → 待机动画（Idle）
        /// </summary>
        private void SetAnimatorState(bool isMoving, bool isWorking)
        {
            if (_animator == null) return;

            // 集A
            if (_hasSpeed)     _animator.SetFloat(Hash_Speed,     isMoving  ? MOVE_SPEED : 0f);
            if (_hasIsPushing) _animator.SetBool (Hash_IsPushing, isWorking);

            // 集B
            if (_hasIsRunning)  _animator.SetBool(Hash_IsRunning,  isMoving);
            if (_hasIsMining)   _animator.SetBool(Hash_IsMining,   isWorking && !isMoving);
            if (_hasIsCarrying) _animator.SetBool(Hash_IsCarrying, false);  // 暂不使用，保留扩展
            if (_hasIsIdle)     _animator.SetBool(Hash_IsIdle,     !isMoving && !isWorking);
        }

        /// <summary>
        /// 根据工作类型精确映射动画参数（进入 Work 状态时调用）。
        ///
        /// 映射规则（仅使用现有 kuanggong_05.controller 参数，不添加新参数）：
        ///   cmd=1 食物/打鱼  → Idle 站立姿态（靠近渔场，不挥工具）
        ///   cmd=2 挖煤       → IsMining=true（挖掘动作）
        ///   cmd=3 挖矿       → IsMining=true（挖掘动作）
        ///   cmd=4 火堆围炉   → Idle 站立姿态（朝向火堆，由 UpdateFireIdle 控制朝向）
        ///   cmd=6 打怪       → IsMining=true（以挖掘动作替代战斗，现有参数集内最接近）
        /// </summary>
        private void SetAnimatorStateForCmd(int cmd)
        {
            if (_animator == null) return;

            bool useMining = (cmd == 2 || cmd == 3 || cmd == 6);

            // 集A
            if (_hasSpeed)     _animator.SetFloat(Hash_Speed,     0f);
            if (_hasIsPushing) _animator.SetBool (Hash_IsPushing, useMining);

            // 集B
            if (_hasIsRunning)  _animator.SetBool(Hash_IsRunning,  false);
            if (_hasIsMining)   _animator.SetBool(Hash_IsMining,   useMining);
            if (_hasIsCarrying) _animator.SetBool(Hash_IsCarrying, false);
            if (_hasIsIdle)     _animator.SetBool(Hash_IsIdle,     !useMining);
        }

        // ==================== 子任务1：辅助方法 ====================

        /// <summary>查找最近的活跃怪物（#7 用 WaveSpawner 缓存列表，避免每帧 FindObjectsOfType）</summary>
        private Transform FindNearestActiveMonster()
        {
            var spawner  = DrscfZ.Monster.MonsterWaveSpawner.Instance;
            var monsters = spawner != null
                ? (System.Collections.Generic.IReadOnlyList<DrscfZ.Monster.MonsterController>)spawner.ActiveMonsters
                : System.Array.Empty<DrscfZ.Monster.MonsterController>();
            Transform nearest = null;
            float minDist = float.MaxValue;
            foreach (var m in monsters)
            {
                if (m == null || !m.gameObject.activeInHierarchy || m.IsDead) continue;
                float d = Vector3.Distance(transform.position, m.transform.position);
                if (d < minDist) { minDist = d; nearest = m.transform; }
            }
            return nearest;
        }

        // ==================== 子任务3：攻击拖尾初始化 ====================

        private void SetupAttackTrail()
        {
            _attackTrail = gameObject.AddComponent<TrailRenderer>();
            _attackTrail.time       = 0.25f;
            _attackTrail.startWidth = 0.15f;
            _attackTrail.endWidth   = 0f;
            _attackTrail.material   = new Material(Shader.Find("Sprites/Default"));
            _attackTrail.startColor = new Color(1f, 0.55f, 0.1f, 0.85f);
            _attackTrail.endColor   = new Color(1f, 0.55f, 0.1f, 0f);
            _attackTrail.enabled    = false;
        }

        // ==================== 子任务4：爆炸粒子 ====================

        private void SpawnHitExplosion(Vector3 pos)
        {
            var go = new GameObject("HitExplosion");
            go.transform.position = pos;

            var ps   = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.5f;
            main.startSpeed    = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize     = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.startColor    = new Color(1f, 0.4f, 0.05f);
            main.maxParticles  = 25;
            main.loop          = false;

            var shape      = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.2f;

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, 20)
            });
            emission.enabled = true;

            // 设置材质，防止 ParticleSystemRenderer 因无材质显示紫色
            var psr = go.GetComponent<ParticleSystemRenderer>();
            if (_hitExplosionMaterial != null)
                psr.material = _hitExplosionMaterial;
            else
                psr.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));

            ps.Play();
            Object.Destroy(go, 1.5f);
        }

        // ==================== 私有工具 ====================

        private void StopMoveCoroutine()
        {
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }
        }

        /// <summary>缓出三次方（加速开始，减速到达）</summary>
        private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        /// <summary>缓进缓出三次方（适合返回待机）</summary>
        private static float EaseInOutCubic(float t)
            => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }
}
