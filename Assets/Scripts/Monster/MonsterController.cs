using UnityEngine;
using System;
using System.Collections;

namespace DrscfZ.Monster
{
    /// <summary>
    /// 单个怪物控制器
    /// 状态机：Spawn → MoveToGate → Attack → Dead
    /// 参考《极寒之夜》怪物移动逻辑
    /// </summary>
    public enum MonsterType { Normal, Elite, Boss }

    /// <summary>🆕 §31 怪物多样性变种枚举（与服务端 variant 字符串一一对应）</summary>
    public enum MonsterVariant
    {
        Normal,    // 标准怪（原行为）
        Rush,      // 冲锋怪：红色，无视矿工直奔城门
        Assassin,  // 刺客怪：深紫色，优先最低 HP 矿工
        Ice,       // 冰封怪：蓝白色，命中有概率冻结（服务端判定，客户端仅染色）
        Summoner,  // 召唤怪：绿色，死亡时播粒子（迷你怪由服务端重推送 monster_wave）
        Guard,     // 首领卫兵：暗金色，行为同普通怪
        Mini       // 迷你怪：尺寸缩小（召唤怪死亡产物，isSummonSpawn=true）
    }

    public class MonsterController : MonoBehaviour
    {
        public enum MonsterState { Spawning, Moving, Attacking, Dead }

        [Header("怪物参数（由 WaveSpawner 注入）")]
        [SerializeField] private int   _maxHp      = 30;
        [SerializeField] private int   _attack     = 5;
        [SerializeField] private float _moveSpeed  = 1.5f;
        [SerializeField] private float _attackRange = 4.0f;
        [SerializeField] private float _attackInterval = 1.5f;

        [Header("Monster Type")]
        [SerializeField] private MonsterType _monsterType = MonsterType.Normal;
        public MonsterType Type => _monsterType;
        public string MonsterId { get; private set; }

        // 🆕 §31 多样性变种（初始化时写入，运行期只读）
        [SerializeField] private MonsterVariant _variant = MonsterVariant.Normal;
        public MonsterVariant Variant => _variant;

        [Header("HP 条（World Space Canvas）")]
        [SerializeField] private UnityEngine.UI.Image _hpFillImage;
        [SerializeField] private float _hpBarHeightOffset = 1.5f; // 血条距脚底高度（世界单位）

        // 血条平滑动画
        private float _hpBarTargetFill = 1f;
        private Transform _hpBarCanvas;  // 缓存引用，LateUpdate 中每帧更新位置
        private Camera _mainCam;

        // 运行时状态
        private int   _currentHp;
        private MonsterState _state = MonsterState.Spawning;
        private Transform _gateTarget;    // 城门目标Transform
        private Animator  _animator;
        private float _attackCooldown;

        // 当前正在攻击的矿工目标（为null则攻击城门）
        private Survival.WorkerController _currentWorkerTarget;

        // 事件
        public event Action<MonsterController> OnDead;  // 死亡通知 WaveSpawner

        public bool IsDead => _state == MonsterState.Dead;

        /// <summary>🆕 §34A B1 StatusLineBannerUI：当前 HP（只读）。供 UI 读取 Boss HP 展示。</summary>
        public int CurrentHp => _currentHp;

        // ==================== 🆕 P0-B6 Lv5 冰霜光环（MonsterController 静态共享） ====================
        // 服务端下发 gate_effect_triggered{effect:'frost_aura', active, radius, slowMult, gatePos}。
        // SurvivalGameManager.HandleGateEffectTriggered 统一写入以下静态字段，
        // 所有怪物每帧在 UpdateMoving 入口查一次距离判定，应用减速倍率。
        /// <summary>Lv5 冰霜光环是否激活（城门 Lv5+ 时 true）。</summary>
        public static bool    FrostAuraActive;
        /// <summary>冰霜光环圆心（通常=城门位置）。</summary>
        public static Vector3 FrostAuraCenter;
        /// <summary>冰霜光环作用半径（米，来自服务端 radius 字段，默认 6）。</summary>
        public static float   FrostAuraRadius = 6f;
        /// <summary>光环内速度倍率（0~1，服务端 slowMult 字段，默认 0.7）。</summary>
        public static float   FrostAuraSlowMult = 0.7f;

        /// <summary>🆕 P0-B7 Lv6 冲击波冻结：到期时间戳（Time.time）。0 或小于当前时间 = 未冻结。</summary>
        public float FrozenUntil;

        /// <summary>光环内怪物是否应用浅蓝 tint（持续态，由 UpdateMoving 判定，UpdateHpBar 兄弟可直接读）。</summary>
        private bool _frostAuraTintApplied;

        // ==================== 初始化 ====================

        /// <summary>由 WaveSpawner 调用，注入参数（旧签名委派到 variant 重载，默认 Normal）</summary>
        public void Initialize(int hp, int attack, float speed, Transform gateTarget)
        {
            Initialize(hp, attack, speed, gateTarget, MonsterVariant.Normal);
        }

        /// <summary>🆕 §31 变种重载：额外传入 variant，末尾应用颜色染色 + Rush 锁城门目标</summary>
        public void Initialize(int hp, int attack, float speed, Transform gateTarget, MonsterVariant variant)
        {
            _maxHp     = hp;
            _currentHp = hp;
            _attack    = attack;
            _moveSpeed = speed;
            _gateTarget = gateTarget;
            _variant    = variant;
            _animator   = GetComponentInChildren<Animator>();
            _state      = MonsterState.Moving;

            // ── 性能优化：关闭阴影投射 + 视野外停止蒙皮 ────────────────────────
            if (_animator != null)
                _animator.cullingMode = AnimatorCullingMode.CullCompletely;
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.shadowCastingMode   = UnityEngine.Rendering.ShadowCastingMode.Off;
                smr.updateWhenOffscreen = false;
            }

            // ── HPBarCanvas：缓存、激活、缩放、绑定 _hpFillImage ──────────────────
            _mainCam = Camera.main;
            _hpBarCanvas = transform.Find("HPBarCanvas");
            if (_hpBarCanvas != null)
            {
                _hpBarCanvas.gameObject.SetActive(true);          // 确保激活（默认可能是关闭的）
                _hpBarCanvas.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                foreach (Transform child in _hpBarCanvas)
                    child.localScale = Vector3.one;

                // 强制重新绑定 HPFill（按名称查找，不用 GetComponentInChildren 避免拿到 Background）
                var hpFillTr = _hpBarCanvas.Find("HPFill");
                if (hpFillTr != null)
                    _hpFillImage = hpFillTr.GetComponent<UnityEngine.UI.Image>();
            }
            var hpCanvas = _hpBarCanvas; // 兼容下方代码

            // ── 绑定中文字体到 HP 数字文本组件 ───────────────────────────────────
            if (hpCanvas != null)
            {
                var hpText = hpCanvas.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (hpText != null)
                {
                    if (hpText.fontSize < 36f) hpText.fontSize = 36f;
                    var font = Resources.Load<TMPro.TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF") ?? Resources.Load<TMPro.TMP_FontAsset>("Fonts/ChineseFont SDF");
                    if (font != null) hpText.font = font;
                }
            }
            UpdateHpBar();
            // 初始化时立即设置 fillAmount=1（跳过 Lerp，避免从0渐变到满血的视觉错误）
            if (_hpFillImage != null)
                _hpFillImage.fillAmount = 1f;

            // 🆕 §31 应用 variant 颜色染色 / 尺寸缩放（在 SMR 查好后再调用）
            ApplyVariantTint();

            // 🆕 §31 Rush：初始化时直接清掉任何可能的矿工目标，锁定城门
            if (_variant == MonsterVariant.Rush)
                _currentWorkerTarget = null;

            PlayAnim("Run");
        }

        /// <summary>由服务器驱动时调用，注入怪物类型和ID</summary>
        public void InitializeWithType(string monsterId, MonsterType type, float hp, float atk, float speed)
        {
            MonsterId    = monsterId;
            _monsterType = type;
            // #3 校验城门目标，未设置时打警告（调用方应先 SetGateTarget 再调此方法）
            if (_gateTarget == null)
                Debug.LogWarning($"[MonsterController] InitializeWithType: _gateTarget 为 null，怪物 {monsterId} 将使用兜底位置。请先调用 SetGateTarget()。");
            Initialize(Mathf.RoundToInt(hp), Mathf.RoundToInt(atk), speed, _gateTarget);
        }

        // ==================== 更新 ====================

        private void LateUpdate()
        {
            // 血条 Canvas 位置：始终锁定在脚底正上方 + 朝向摄像机（Billboard）
            if (_hpBarCanvas != null && _hpBarCanvas.gameObject.activeSelf)
            {
                if (_mainCam == null) _mainCam = Camera.main;
                _hpBarCanvas.position = transform.position + Vector3.up * _hpBarHeightOffset;
                if (_mainCam != null)
                    _hpBarCanvas.rotation = Quaternion.LookRotation(
                        _hpBarCanvas.position - _mainCam.transform.position);
            }
        }

        private void Update()
        {
            // 死亡状态跳过所有 AI 与 tint 更新（等待 Destroy）
            if (_state == MonsterState.Dead)
            {
                UpdateHpBarSmooth();
                return;
            }

            // 🆕 P0-B7 Lv6 冲击波冻结：期间跳过所有 AI，播放冻结 tint（浅蓝 hold，不闪）
            if (Time.time < FrozenUntil)
            {
                // 注：PlayFreezeFlash 协程末尾会 SetPropertyBlock(null)，可能擦除 hold tint。
                //   每帧强制重写以对抗该清除（仅冻结期间；代价 = N SetPropertyBlock，可接受）。
                _frozenTintApplied = false;
                ApplyFrozenTint(true);
                UpdateHpBarSmooth();
                return;
            }
            else if (_frozenTintApplied)
            {
                ApplyFrozenTint(false);
            }

            // 🆕 P0-B6 Lv5 冰霜光环：常驻浅蓝 tint（在光环范围内的怪物）——与冻结 tint 不同，较淡
            UpdateFrostAuraTint();

            switch (_state)
            {
                case MonsterState.Moving:   UpdateMoving();   break;
                case MonsterState.Attacking: UpdateAttacking(); break;
            }
            // 血条平滑动画（每帧推进）
            UpdateHpBarSmooth();
        }

        // 🆕 P0-B6/B7 冰霜相关 tint 状态：避免每帧重复 SetPropertyBlock
        private bool _frozenTintApplied;

        private void UpdateFrostAuraTint()
        {
            // 只有光环激活时检查；否则清除 tint
            bool shouldTint = false;
            if (FrostAuraActive && FrostAuraRadius > 0f)
            {
                float dist = Vector3.Distance(transform.position, FrostAuraCenter);
                shouldTint = dist <= FrostAuraRadius;
            }
            if (shouldTint == _frostAuraTintApplied) return;
            _frostAuraTintApplied = shouldTint;
            ApplyFrostAuraTintNow(shouldTint);
        }

        /// <summary>P0-B6 光环范围内怪物染浅蓝 tint（original × 0.7 + cyan × 0.3）。
        /// 与 variant tint 叠加：variant tint 在 Initialize 设置后由 ApplyVariantTint 写入，
        /// 这里覆写 _BaseColor/_Color → 退出光环时清 PropertyBlock 让 variant tint 重新生效（需 ApplyVariantTint 二次调用）。</summary>
        private void ApplyFrostAuraTintNow(bool on)
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return;

            if (on)
            {
                var block = new MaterialPropertyBlock();
                // Color.Lerp(original, cyan, 0.3f)：浅蓝偏冷
                // 使用固定 tint（与 variant tint 合并时略偏原变种色不是精确合成，但视觉够用）
                Color tint = new Color(0.7f, 0.9f, 1.1f, 1f);
                foreach (var r in renderers)
                {
                    if (r == null) continue;
                    block.SetColor(PropBaseColor, tint);
                    block.SetColor(PropColor, tint);
                    r.SetPropertyBlock(block);
                }
            }
            else
            {
                // 退出光环：清 PropertyBlock → 恢复 variant tint 或 shared material 原色
                foreach (var r in renderers)
                {
                    if (r == null) continue;
                    r.SetPropertyBlock(null);
                }
                // 重新应用 variant tint（因为清了 PropertyBlock）
                ApplyVariantTint();
            }
        }

        /// <summary>P0-B7 冻结 tint（更深的冰蓝白，持续显示直到 FrozenUntil 过期）。</summary>
        private void ApplyFrozenTint(bool on)
        {
            if (_frozenTintApplied == on) return;
            _frozenTintApplied = on;
            var renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers == null) return;

            if (on)
            {
                var block = new MaterialPropertyBlock();
                Color frost = new Color(0.6f, 0.85f, 1f, 1f);
                foreach (var r in renderers)
                {
                    if (r == null) continue;
                    block.SetColor(PropBaseColor, frost);
                    block.SetColor(PropColor, frost);
                    r.SetPropertyBlock(block);
                }
            }
            else
            {
                foreach (var r in renderers)
                {
                    if (r == null) continue;
                    r.SetPropertyBlock(null);
                }
                // 退出冻结：重新应用 variant tint + 若还在光环内重新应用 aura tint
                _frostAuraTintApplied = false;
                ApplyVariantTint();
            }
        }

        private void UpdateMoving()
        {
            // 🆕 §31 Rush：完全跳过矿工寻路，直接锁城门（符合"冲锋怪无视矿工直奔城门"）
            Survival.WorkerController workerTarget = null;
            if (_variant != MonsterVariant.Rush)
            {
                // §30.5 威胁值加权寻目标：阶段越高的矿工越容易被锁定
                // 🆕 §31 Assassin：在威胁值公式之外，优先选 HP 最低的存活矿工
                if (_variant == MonsterVariant.Assassin)
                    workerTarget = FindLowestHpWorker();
                else
                    workerTarget = FindTargetWorker(Type == MonsterType.Boss);
            }

            Vector3 targetPos;
            if (workerTarget != null)
            {
                _currentWorkerTarget = workerTarget;
                targetPos = workerTarget.transform.position;
            }
            else
            {
                _currentWorkerTarget = null;
                // 无存活矿工（或 Rush 强制走城门）时攻击城门
                if (_gateTarget != null)
                    targetPos = _gateTarget.position;
                else
                    targetPos = new Vector3(transform.position.x * 0.5f, 0f, -4f);
            }

            float dist = Vector3.Distance(transform.position, targetPos);
            if (dist <= _attackRange)
            {
                StartAttacking();
            }
            else
            {
                MoveToward(targetPos);
                PlayAnim("Run");
            }
        }

        /// <summary>
        /// §30.5 威胁值加权选目标（阶段越高的矿工越容易被锁定）。
        /// score = threat / sqrt(dist)，最大 score 胜出。
        /// Boss 使用 1 + threat*0.5 平衡权重（普通怪物则直接用 threat）。
        /// 使用 WorkerManager 缓存列表（#7 避免每帧 FindObjectsOfType）。
        /// </summary>
        private Survival.WorkerController FindTargetWorker(bool isBoss)
        {
            var mgr     = Survival.WorkerManager.Instance;
            var workers = mgr != null
                ? (System.Collections.Generic.IReadOnlyList<Survival.WorkerController>)mgr.ActiveWorkers
                : System.Array.Empty<Survival.WorkerController>();

            Survival.WorkerController best = null;
            float bestScore = -1f;
            foreach (var w in workers)
            {
                if (w == null || !w.gameObject.activeInHierarchy || w.IsDead) continue;
                float threat = isBoss
                    ? (1f + w.ThreatMultiplier * 0.5f)
                    : w.ThreatMultiplier;
                float dist  = Vector3.Distance(transform.position, w.transform.position);
                float score = threat / Mathf.Sqrt(Mathf.Max(dist, 0.1f));
                if (score > bestScore) { bestScore = score; best = w; }
            }
            return best;
        }

        /// <summary>
        /// 查找最近的存活矿工（保留兼容接口，内部转调 FindTargetWorker）。
        /// 旧代码（若有）继续可用；新代码请用 FindTargetWorker(Type == MonsterType.Boss)。
        /// </summary>
        [System.Obsolete("§30: 请改用 FindTargetWorker(bool isBoss) 以获得威胁值加权效果")]
        // ReSharper disable once UnusedMember.Local
        private Survival.WorkerController FindNearestAliveWorker()
        {
            return FindTargetWorker(Type == MonsterType.Boss);
        }

        /// <summary>
        /// 🆕 §31 Assassin 怪物目标选择：存活矿工按 HP 升序取第一个。
        /// 使用 WorkerController.CurrentHpRatio（0~1）作为 HP 排序键——服务端
        /// worker_hp_update 推送后由 WorkerController.SetHp() 写入。
        /// 若 _activeWorkers 为空 → 返回 null（UpdateMoving 会 fallback 到城门）。
        /// </summary>
        private Survival.WorkerController FindLowestHpWorker()
        {
            var mgr     = Survival.WorkerManager.Instance;
            var workers = mgr != null
                ? (System.Collections.Generic.IReadOnlyList<Survival.WorkerController>)mgr.ActiveWorkers
                : System.Array.Empty<Survival.WorkerController>();

            Survival.WorkerController best = null;
            float bestRatio = float.MaxValue;
            foreach (var w in workers)
            {
                if (w == null || !w.gameObject.activeInHierarchy || w.IsDead) continue;
                float r = w.CurrentHpRatio;
                if (r < bestRatio) { bestRatio = r; best = w; }
            }
            return best;
        }

        private void UpdateAttacking()
        {
            // 若当前攻击目标是矿工，检查其是否仍然存活
            if (_currentWorkerTarget != null)
            {
                bool targetGone = _currentWorkerTarget.IsDead
                               || !_currentWorkerTarget.gameObject.activeInHierarchy;
                bool targetFled = Vector3.Distance(transform.position,
                                    _currentWorkerTarget.transform.position) > _attackRange * 2f;
                if (targetGone || targetFled)
                {
                    // 矿工死亡/离开范围 → 重新进入寻路状态找新目标
                    _currentWorkerTarget = null;
                    _state = MonsterState.Moving;
                    PlayAnim("Run");
                    return;
                }
            }

            _attackCooldown -= Time.deltaTime;
            if (_attackCooldown <= 0f)
            {
                _attackCooldown = _attackInterval;
                DoAttack();
            }

            // 如果城门已经没了，停止攻击
            if (Survival.CityGateSystem.Instance != null &&
                Survival.CityGateSystem.Instance.CurrentHp <= 0)
            {
                _state = MonsterState.Dead;
            }
        }

        private void StartAttacking()
        {
            _state = MonsterState.Attacking;
            _attackCooldown = 0.5f; // 先等0.5秒再开始攻击
            PlayAnim("Attack");
        }

        private void DoAttack()
        {
            PlayAnim("Attack");
            // 城门HP由服务器resource_update权威管理，禁止客户端扣血
            // Survival.CityGateSystem.Instance?.TakeDamage(_attack);

            // audit-r12 GAP-C01：§29.2 怪物攻击撞击声（AudioManager 内部去重，多怪同时攻击不刷爆）
            DrscfZ.Systems.AudioManager.Instance?.PlaySFX(DrscfZ.Core.AudioConstants.SFX_MONSTER_ATTACK);
        }

        private void MoveToward(Vector3 target)
        {
            // 墙壁绕行：如果目标在墙另一侧且不在城门口，先走向城门
            Vector3 effectiveTarget = target;
            Vector3? detour = Survival.WallBarrier.GetDetourPoint(transform.position, target);
            if (detour.HasValue)
                effectiveTarget = detour.Value;

            Vector3 dir = (effectiveTarget - transform.position);
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f)
            {
                dir.Normalize();
                // 🆕 P0-B6 Lv5 冰霜光环：在光环范围内的怪物移速 × slowMult（默认 0.7）
                float slowFactor = 1f;
                if (FrostAuraActive && FrostAuraRadius > 0f)
                {
                    float distToCenter = Vector3.Distance(transform.position, FrostAuraCenter);
                    if (distToCenter <= FrostAuraRadius)
                        slowFactor = FrostAuraSlowMult;
                }
                float effectiveSpeed = _moveSpeed * slowFactor;
                Vector3 newPos = transform.position + dir * effectiveSpeed * Time.deltaTime;
                // 墙壁碰撞检测：阻止穿墙
                newPos = Survival.WallBarrier.ClampMovement(transform.position, newPos);
                transform.position = newPos;
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(dir), Time.deltaTime * 6f);
            }
        }

        // ==================== 受击 / 死亡 ====================

        public void TakeDamage(int dmg)
        {
            if (_state == MonsterState.Dead) return;
            _currentHp -= dmg;
            UpdateHpBar();

            // 子任务2/6：伤害飘字
            DamageNumber.Show(transform.position, dmg, new Color(1f, 0.3f, 0.3f));

            // audit-r12 GAP-C01：§29.2 怪物受击/嚎叫
            DrscfZ.Systems.AudioManager.Instance?.PlaySFX(DrscfZ.Core.AudioConstants.SFX_MONSTER_HIT);

            if (_currentHp <= 0)
                Die();
        }

        private void UpdateHpBar()
        {
            _hpBarTargetFill = _maxHp > 0 ? Mathf.Clamp01((float)_currentHp / _maxHp) : 0f;
            // 立即同步 fillAmount（不等 Lerp 下帧生效），确保受击瞬间可见
            RebindHpFillIfNeeded();
            if (_hpFillImage != null)
                _hpFillImage.fillAmount = _hpBarTargetFill;
        }

        /// <summary>🆕 Fix B (组 B Reviewer P0) §34B B3 heavy_fog：
        ///   外部（MonsterWaveSpawner 响应 resource_update.hideMonsterHp）控制怪物血条显隐。
        ///   true=隐藏（fog 事件期间），false=恢复显示（怪物默认血条常驻）。
        ///   怪物与 Worker 不同：怪物无"白天隐藏/夜晚显示"规则，默认一直可见，此开关直接 SetActive。</summary>
        public void SetHpBarVisible(bool visible)
        {
            if (_hpBarCanvas == null) _hpBarCanvas = transform.Find("HPBarCanvas");
            if (_hpBarCanvas != null && _hpBarCanvas.gameObject.activeSelf != visible)
                _hpBarCanvas.gameObject.SetActive(visible);
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
                    Debug.LogWarning($"[MonsterController] {name}: _hpFillImage was null, rebound to {_hpFillImage.name}");
            }
        }

        private void UpdateHpBarSmooth()
        {
            RebindHpFillIfNeeded();
            if (_hpFillImage == null) return;
            _hpFillImage.fillAmount = Mathf.Lerp(_hpFillImage.fillAmount, _hpBarTargetFill, Time.deltaTime * 10f);
        }

        public void TakeDamage(float damage)
        {
            TakeDamage(Mathf.RoundToInt(damage));
        }

        /// <summary>服务器攻击命中 → 播放受击特效（闪白）</summary>
        public void ShowHitEffect()
        {
            if (_state == MonsterState.Dead) return;
            StartCoroutine(HitFlash());
        }

        /// <summary>播放冻结闪烁（🆕 v1.22 §10 Lv6 寒冰冲击波命中时触发）</summary>
        public void PlayFreezeFlash()
        {
            if (_state == MonsterState.Dead) return;
            StartCoroutine(FreezeFlashCoroutine());
        }

        private IEnumerator FreezeFlashCoroutine()
        {
            // 冰蓝色闪烁 0.4s（共 2 次闪）+ 弱屏闪提示
            var renderers = GetComponentsInChildren<Renderer>();
            var block = new MaterialPropertyBlock();
            Color frost = new Color(0.6f, 0.85f, 1f, 1f);

            for (int flash = 0; flash < 2; flash++)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] == null) continue;
                    block.SetColor(PropBaseColor, frost);
                    block.SetColor(PropColor,     frost);
                    renderers[i].SetPropertyBlock(block);
                }
                yield return new WaitForSeconds(0.1f);

                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                        renderers[i].SetPropertyBlock(null);
                }
                yield return new WaitForSeconds(0.1f);
            }
        }

        // #2 静态 PropertyID 避免每次字符串哈希
        private static readonly int PropBaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int PropColor     = Shader.PropertyToID("_Color");

        // 🆕 §31 变种染色表（与 MonsterVariant 枚举同顺序；Normal 不染色）
        // Mini 不染色仅缩放 scale；因此颜色数组长度 = 枚举数 - 2（排除 Normal 和 Mini）
        // 表中存储 Rush/Assassin/Ice/Summoner/Guard 的 _BaseColor 值
        private static readonly System.Collections.Generic.Dictionary<MonsterVariant, Color> VariantTints
            = new System.Collections.Generic.Dictionary<MonsterVariant, Color>
        {
            { MonsterVariant.Rush,     new Color(1.30f, 0.50f, 0.50f, 1f) },  // 红色色调
            { MonsterVariant.Assassin, new Color(0.60f, 0.40f, 0.80f, 1f) },  // 深紫色调
            { MonsterVariant.Ice,      new Color(0.70f, 0.90f, 1.20f, 1f) },  // 蓝白色调
            { MonsterVariant.Summoner, new Color(0.50f, 1.20f, 0.70f, 1f) },  // 绿色发光
            { MonsterVariant.Guard,    new Color(1.10f, 0.95f, 0.55f, 1f) },  // 暗金色调
        };

        /// <summary>
        /// 🆕 §31 应用 variant 颜色/尺寸。在 Initialize 末尾调用，使用 MaterialPropertyBlock
        /// 覆盖所有 SkinnedMeshRenderer 的 _BaseColor/_Color（不新建材质实例，与 HitFlash 同机制）。
        /// Mini 变种：保持原色 + localScale × 0.6（服务端已标 isSummonSpawn=true）。
        /// Normal 变种：不做任何处理。
        /// </summary>
        private void ApplyVariantTint()
        {
            // Mini：仅缩放（不改颜色）——由 spawner 创建时或此处统一处理均可；此处兜底防 spawner 遗漏
            if (_variant == MonsterVariant.Mini)
            {
                transform.localScale *= 0.6f;
                return;
            }

            // Normal：无需染色
            if (!VariantTints.TryGetValue(_variant, out var tint)) return;

            // 遍历所有 SMR（含 LOD Group 下的多个 SMR，GetComponentsInChildren 会一并取到）
            var renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers == null || renderers.Length == 0) return;

            var mpb = new MaterialPropertyBlock();
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetColor(PropBaseColor, tint);  // URP Lit
                mpb.SetColor(PropColor,     tint);  // Built-in Standard 兜底
                r.SetPropertyBlock(mpb);
            }
        }

        private IEnumerator HitFlash()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            // #2 用 MaterialPropertyBlock 闪白，不创建材质实例
            var block = new MaterialPropertyBlock();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                block.SetColor(PropBaseColor, Color.white); // URP
                block.SetColor(PropColor,     Color.white); // Standard/备用
                renderers[i].SetPropertyBlock(block);
            }
            yield return new WaitForSeconds(0.1f);
            // 清除 PropertyBlock → 自动恢复共享材质原色，无实例残留
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].SetPropertyBlock(null);
            }
        }

        /// <summary>设置怪物ID和类型（不重新初始化）</summary>
        public void SetMonsterIdAndType(string monsterId, MonsterType type)
        {
            MonsterId    = monsterId;
            _monsterType = type;
        }

        /// <summary>设置城门目标（用于InitializeWithType后补充注入）</summary>
        public void SetGateTarget(Transform gateTarget)
        {
            _gateTarget = gateTarget;
        }

        public Transform GateTarget => _gateTarget;

        private void Die()
        {
            _state = MonsterState.Dead;
            PlayAnim("Sit");   // 所有kuanggong controller的死亡状态名是"Sit"（Sitting Dazed动画）

            // 🆕 §31 Summoner 死亡：绿色爆炸占位（粒子 Prefab 未就绪 → 用 Light 点亮 0.3s）
            if (_variant == MonsterVariant.Summoner)
                SpawnSummonerDeathFx(transform.position + Vector3.up * 0.5f);

            OnDead?.Invoke(this);
            Destroy(gameObject, 2f);
        }

        /// <summary>🆕 §31 Summoner 死亡视觉占位：在死亡位置生成 Light + 简单粒子，0.3s 后销毁。
        /// 迷你怪由服务端 monster_wave { isSummonSpawn: true } 重推送，客户端不在此处生成。</summary>
        private static void SpawnSummonerDeathFx(Vector3 worldPos)
        {
            var go = new GameObject("SummonerDeathFx");
            go.transform.position = worldPos;

            // 绿光 Light（点光源，0.3s 淡出）
            var light = go.AddComponent<Light>();
            light.type       = LightType.Point;
            light.color      = new Color(0.3f, 1.0f, 0.4f);
            light.intensity  = 3f;
            light.range      = 4f;

            // 简单粒子（绿色向上迸发）
            var ps   = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.6f;
            main.startSpeed    = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize     = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startColor    = new Color(0.3f, 1.0f, 0.4f);
            main.maxParticles  = 30;
            main.loop          = false;

            var shape      = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.3f;

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, 25)
            });
            emission.enabled = true;

            // 材质兜底：URP Particles/Unlit，防止紫色
            var psr = go.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
                psr.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")
                                         ?? Shader.Find("Sprites/Default"));

            ps.Play();
            UnityEngine.Object.Destroy(go, 1.0f);
        }

        // ==================== 动画 ====================

        private string _curAnim = "";
        private void PlayAnim(string name)
        {
            if (_animator == null || _curAnim == name) return;
            _curAnim = name;
            _animator.CrossFade(name, 0.1f);
        }

        // ==================== 调试显示 ====================

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _attackRange);
        }
    }
}
