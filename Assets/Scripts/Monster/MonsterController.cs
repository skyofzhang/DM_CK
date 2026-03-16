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

            // ── 始终重置 HPBarCanvas 缩放（无论 _hpFillImage 是否已绑定）──────
            var hpCanvas = transform.Find("HPBarCanvas");
            if (hpCanvas != null)
            {
                hpCanvas.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                foreach (Transform child in hpCanvas)
                    child.localScale = Vector3.one;  // 强制子对象归 (1,1,1)
            }

            // ── 仅在 _hpFillImage 未绑定时查找组件引用 ─────────────────────────
            if (_hpFillImage == null && hpCanvas != null)
            {
                _hpFillImage = hpCanvas.Find("HPFill")?.GetComponent<UnityEngine.UI.Image>();

                // 绑定中文字体到 HP 数字文本组件
                var hpText = hpCanvas.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (hpText != null)
                {
                    if (hpText.fontSize < 36f) hpText.fontSize = 36f;
                    var font = Resources.Load<TMPro.TMP_FontAsset>("Fonts/ChineseFont SDF");
                    if (font != null) hpText.font = font;
                }
            }
            UpdateHpBar();

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

        private void Update()
        {
            switch (_state)
            {
                case MonsterState.Moving:   UpdateMoving();   break;
                case MonsterState.Attacking: UpdateAttacking(); break;
            }
        }

        private void UpdateMoving()
        {
            // 优先攻击最近的存活矿工（黑夜防守模式）
            var workerTarget = FindNearestAliveWorker();

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

        /// <summary>查找最近的存活矿工（#7 用 WorkerManager 缓存列表，避免每帧 FindObjectsOfType）</summary>
        private Survival.WorkerController FindNearestAliveWorker()
        {
            var mgr     = Survival.WorkerManager.Instance;
            var workers = mgr != null
                ? (System.Collections.Generic.IReadOnlyList<Survival.WorkerController>)mgr.ActiveWorkers
                : System.Array.Empty<Survival.WorkerController>();
            Survival.WorkerController nearest = null;
            float minD = float.MaxValue;
            foreach (var w in workers)
            {
                if (w == null || !w.gameObject.activeInHierarchy || w.IsDead) continue;
                float d = Vector3.Distance(transform.position, w.transform.position);
                if (d < minD) { minD = d; nearest = w; }
            }
            return nearest;
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
            Vector3 dir = (target - transform.position);
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f)
            {
                dir.Normalize();
                transform.position += dir * _moveSpeed * Time.deltaTime;
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
            if (_hpFillImage != null)
                _hpFillImage.fillAmount = _maxHp > 0 ? Mathf.Clamp01((float)_currentHp / _maxHp) : 0f;
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
