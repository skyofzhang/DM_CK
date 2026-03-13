using UnityEngine;
using System;
using System.Collections;
using DrscfZ.UI;

namespace DrscfZ.Survival
{
    /// <summary>
    /// 城门系统：管理城门HP + 被怪物攻击逻辑
    /// 数据权威来自服务器
    /// </summary>
    public class CityGateSystem : MonoBehaviour
    {
        public static CityGateSystem Instance { get; private set; }

        [Header("城门参数（数值来自策划案 §4.3）")]
        [SerializeField] private int _maxHp = 1000;
        [SerializeField] private int _initHp = 1000;

        [Header("WorldSpace UI（场景中预创建，Rule #2）")]
        [SerializeField] private WorldSpaceLabel _hpLabel;  // 拖入城门 WorldSpaceLabel（GateHP 类型）

        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }
        public float HpRatio => MaxHp > 0 ? (float)CurrentHp / MaxHp : 0f;

        // 事件
        public event Action<int, int> OnHpChanged;    // current, max
        public event Action OnGateBreached;            // HP=0 → 失败
        public event Action<int> OnGateRepaired;       // 修城门+X

        // ── 子任务5：受击变色 ─────────────────────────────────────────────────────
        private Renderer[] _gateRenderers;
        private Color[]    _originalColors;
        private bool       _isFlashing;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // 子任务5：缓存城门所有 Renderer 及原始颜色
            _gateRenderers  = GetComponentsInChildren<Renderer>(true);
            _originalColors = new Color[_gateRenderers.Length];
            for (int i = 0; i < _gateRenderers.Length; i++)
            {
                if (_gateRenderers[i].material.HasProperty("_Color"))
                    _originalColors[i] = _gateRenderers[i].material.color;
                else
                    _originalColors[i] = Color.white;
            }
        }

        public void Initialize(int maxHp = -1)
        {
            MaxHp     = maxHp > 0 ? maxHp : _maxHp;
            CurrentHp = _initHp;
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
            SyncHpLabel();
        }

        /// <summary>服务器同步城门HP（resource_update中附带）</summary>
        public void SyncFromServer(int hp, int maxHp)
        {
            // 子任务5：HP下降时触发受击变色 + 飘字
            if (hp < CurrentHp && CurrentHp > 0)
            {
                HitFlash();
                int delta = CurrentHp - hp;
                DamageNumber.Show(transform.position + Vector3.up * 3f, delta, Color.yellow);
            }

            bool wasAlive = CurrentHp > 0;
            MaxHp     = maxHp;
            CurrentHp = Mathf.Clamp(hp, 0, MaxHp);
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
            SyncHpLabel();

            if (wasAlive && CurrentHp <= 0)
                OnGateBreached?.Invoke();
        }

        /// <summary>怪物攻击（客户端预测，服务器会覆盖）</summary>
        public void TakeDamage(int damage)
        {
            int prev = CurrentHp;
            CurrentHp = Mathf.Max(0, CurrentHp - damage);
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
            SyncHpLabel();

            if (prev > 0 && CurrentHp <= 0)
                OnGateBreached?.Invoke();
        }

        /// <summary>修复城门（服务器确认后调用）</summary>
        public void Repair(int amount)
        {
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
            SyncHpLabel();
            OnGateRepaired?.Invoke(amount);
        }

        public void Reset()
        {
            Initialize();
        }

        /// <summary>HP颜色（绿→黄→红）</summary>
        public Color GetHpColor()
        {
            float r = HpRatio;
            if (r > 0.5f) return Color.Lerp(Color.yellow, Color.green, (r - 0.5f) * 2f);
            return Color.Lerp(Color.red, Color.yellow, r * 2f);
        }

        /// <summary>城门升级：更新最大HP</summary>
        public void HandleUpgrade(int level, int newMaxHp)
        {
            MaxHp = newMaxHp;
            // 升级时小幅恢复HP（不超过新的最大值）
            CurrentHp = Mathf.Min(CurrentHp + 20, MaxHp);
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
            SyncHpLabel();
            Debug.Log($"[CityGate] 城门升级到 Lv.{level}，最大HP: {MaxHp}，当前HP: {CurrentHp}");
        }

        // ── 子任务5：受击变色 ────────────────────────────────────────────────────

        /// <summary>触发城门受击红色闪烁（0.15s）</summary>
        public void HitFlash()
        {
            if (!_isFlashing) StartCoroutine(DoHitFlash());
        }

        private IEnumerator DoHitFlash()
        {
            _isFlashing = true;
            foreach (var r in _gateRenderers)
                if (r != null && r.material.HasProperty("_Color"))
                    r.material.color = new Color(1f, 0.3f, 0.3f);

            yield return new WaitForSeconds(0.15f);

            for (int i = 0; i < _gateRenderers.Length; i++)
                if (_gateRenderers[i] != null && _gateRenderers[i].material.HasProperty("_Color"))
                    _gateRenderers[i].material.color = _originalColors[i];

            _isFlashing = false;
        }

        // ── WorldSpace 标签同步 ────────────────────────────────────────────────────
        /// <summary>
        /// 将当前 HP 推送到城门上方的 WorldSpaceLabel（GateHP 类型）。
        /// WorldSpaceLabel 订阅了 OnHpChanged 事件会自动更新文本和颜色；
        /// 此方法作为直接引用的补充路径，两者并存不会重复写入。
        /// _hpLabel 在 Inspector 中拖入，Rule #2：预创建，不在运行时 Instantiate。
        /// </summary>
        private void SyncHpLabel()
        {
            if (_hpLabel == null) return;
            // WorldSpaceLabel.OnGateHpChanged 已通过事件订阅处理，
            // 此调用确保即使事件订阅在 Start 之前还未完成也能显示正确值。
            _hpLabel.ForceUpdateGateHp(CurrentHp, MaxHp);
        }
    }
}
