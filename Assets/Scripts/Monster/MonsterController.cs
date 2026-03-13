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
            if (_gateTarget == null)
            {
                // 没有明确目标，向 Z=-4 的城门方向移动
                Vector3 gatePos = new Vector3(transform.position.x * 0.5f, 0, -4f);
                MoveToward(gatePos);
                if (Vector3.Distance(transform.position, gatePos) < _attackRange)
                    StartAttacking();
                return;
            }

            float dist = Vector3.Distance(transform.position, _gateTarget.position);
            if (dist <= _attackRange)
            {
                StartAttacking();
            }
            else
            {
                MoveToward(_gateTarget.position);
            }

            // 有攻击中的矿工进入范围时，转向并播放攻击动画（视觉反击，不中断移动）
            TryFightBackWorker();
        }

        /// <summary>若有 cmd=6 矿工进入攻击范围，转向并播放 Attack 动画（视觉反击）</summary>
        private void TryFightBackWorker()
        {
            var workers = UnityEngine.Object.FindObjectsOfType<Survival.WorkerController>();
            Transform nearest = null;
            float minD = float.MaxValue;
            foreach (var w in workers)
            {
                if (w == null || !w.gameObject.activeInHierarchy) continue;
                if (w.CurrentCmd != 6) continue;
                float d = Vector3.Distance(transform.position, w.transform.position);
                if (d < _attackRange && d < minD) { minD = d; nearest = w.transform; }
            }
            if (nearest != null)
            {
                Vector3 dir = nearest.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(dir), Time.deltaTime * 8f);
                PlayAnim("Attack");
            }
            else
            {
                PlayAnim("Run");  // 无附近战斗矿工时恢复奔跑动画
            }
        }

        private void UpdateAttacking()
        {
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

        private IEnumerator HitFlash()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            // 获取原始材质颜色
            var origColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                origColors[i] = renderers[i].material.color;
                renderers[i].material.color = Color.white;
            }
            yield return new WaitForSeconds(0.1f);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].material.color = origColors[i];
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
            PlayAnim("Dead");
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
