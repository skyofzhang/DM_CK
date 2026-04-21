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

        [Header("城门等级可视化（Editor 工具 SetupGatePrefabs 绑定，允许为空）")]
        [SerializeField] private GameObject[] _gateModels = new GameObject[6]; // Lv1..Lv6

        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }
        public float HpRatio => MaxHp > 0 ? (float)CurrentHp / MaxHp : 0f;

        // ── 🆕 v1.22 §10 城门升级系统 v2 ──────────────────────────────────────────
        /// <summary>城门当前等级（1-6）</summary>
        public int      GateLevel    { get; private set; } = 1;
        /// <summary>城门层级名（"木栅栏"/.../"巨龙要塞"）</summary>
        public string   TierName     { get; private set; } = "木栅栏";
        /// <summary>受到伤害的减伤率（0~1，来自服务端）</summary>
        public float    DmgReduction { get; private set; }
        /// <summary>反伤反弹比例（0~1，Lv4+）</summary>
        public float    ThornsRatio  { get; private set; }
        /// <summary>冰霜光环半径（米，Lv5+）</summary>
        public float    FrostRadius  { get; private set; }
        /// <summary>已激活的特性 ID 列表（如 ["thorns", "frost_aura"]）</summary>
        public string[] Features     { get; private set; } = new string[0];
        /// <summary>当前天是否已升级（时机限制，只在白天可升，每天一次）</summary>
        public bool     DailyUpgraded { get; private set; }

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

        /// <summary>
        /// 城门升级（旧签名，兼容过渡）：
        ///   - tierName 空、features 空、hpBonus=20（与 v1.22 之前的旧逻辑一致）
        ///   - 新代码应直接调用 HandleUpgrade(level, newMaxHp, hpBonus, tierName, features)
        /// </summary>
        public void HandleUpgrade(int level, int newMaxHp)
        {
            HandleUpgrade(level, newMaxHp, 20, "", null);
        }

        /// <summary>城门升级（🆕 v1.22 §10 主入口，由 SurvivalGameManager 解析 gate_upgraded 后调用）</summary>
        public void HandleUpgrade(int level, int newMaxHp, int hpBonus, string tierName, string[] features)
        {
            GateLevel = level;
            TierName  = string.IsNullOrEmpty(tierName) ? TierName : tierName;
            Features  = features ?? new string[0];
            MaxHp     = newMaxHp;

            // hpBonus 由服务端下发：
            //   - Lv2/3/4 常规：+20
            //   - Lv5：+50
            //   - Lv6：回满（hpBonus == gateMaxHp - gateHp）
            CurrentHp = Mathf.Clamp(CurrentHp + hpBonus, 0, MaxHp);
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
            SyncHpLabel();

            SetGateLevel(level);
            Debug.Log($"[CityGate] 升级 Lv.{level}「{TierName}」，MaxHp={MaxHp}，HP={CurrentHp}（+{hpBonus}）");
        }

        /// <summary>切换城门等级外观（启用 _gateModels[level-1]，其余隐藏）🆕 v1.22</summary>
        public void SetGateLevel(int level)
        {
            if (_gateModels == null) return;
            for (int i = 0; i < _gateModels.Length; i++)
            {
                if (_gateModels[i] != null)
                    _gateModels[i].SetActive(i == level - 1);
            }
            StartCoroutine(PlayUpgradeFX());
        }

        private IEnumerator PlayUpgradeFX()
        {
            // TODO §10.7：屏幕震动 / 光柱 / 环形冲击波 / sfx_gate_upgrade（美术资源到位后实现）
            // 保底：触发一次相机轻微震屏（若 SurvivalCameraController 存在）
            SurvivalCameraController.Shake(0.15f, 0.3f);
            yield break;
        }

        /// <summary>状态同步：服务端 survival_game_state / resource_update 把 gateDailyUpgraded 推给客户端</summary>
        public void ApplyDailyUpgraded(bool v) => DailyUpgraded = v;

        /// <summary>
        /// 状态同步（🆕 v1.22 §10）：断线重连 / 定时 resource_update 时补齐城门层级元数据。
        /// 仅在非空时覆盖，避免初始化后的空字段覆盖 HandleUpgrade 设置的值。
        /// </summary>
        public void ApplyTierMeta(string tierName, string[] features)
        {
            if (!string.IsNullOrEmpty(tierName))
                TierName = tierName;
            if (features != null)
                Features = features;
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
