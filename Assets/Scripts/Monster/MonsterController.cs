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

        // ==================== 初始化 ====================

        /// <summary>由 WaveSpawner 调用，注入参数</summary>
        public void Initialize(int hp, int attack, float speed, Transform gateTarget)
        {
            _maxHp     = hp;
            _currentHp = hp;
            _attack    = attack;
            _moveSpeed = speed;
            _gateTarget = gateTarget;
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
                    var font = Resources.Load<TMPro.TMP_FontAsset>("Fonts/ChineseFont SDF");
                    if (font != null) hpText.font = font;
                }
            }
            UpdateHpBar();
            // 初始化时立即设置 fillAmount=1（跳过 Lerp，避免从0渐变到满血的视觉错误）
            if (_hpFillImage != null)
                _hpFillImage.fillAmount = 1f;

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
            switch (_state)
            {
                case MonsterState.Moving:   UpdateMoving();   break;
                case MonsterState.Attacking: UpdateAttacking(); break;
            }
            // 血条平滑动画（每帧推进）
            UpdateHpBarSmooth();
        }

        private void UpdateMoving()
        {
            // §30.5 威胁值加权寻目标：阶段越高的矿工越容易被锁定
            // Boss 目标选择更均衡（threat 权重减半），防止一直盯传奇矿工
            var workerTarget = FindTargetWorker(Type == MonsterType.Boss);

            Vector3 targetPos;
            if (workerTarget != null)
            {
                _currentWorkerTarget = workerTarget;
                targetPos = workerTarget.transform.position;
            }
            else
            {
                _currentWorkerTarget = null;
                // 无存活矿工时攻击城门
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
                Vector3 newPos = transform.position + dir * _moveSpeed * Time.deltaTime;
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
            OnDead?.Invoke(this);
            Destroy(gameObject, 2f);
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
