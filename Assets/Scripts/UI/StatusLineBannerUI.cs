using UnityEngine;
using TMPro;
using DrscfZ.Survival;
using DrscfZ.Monster;

namespace DrscfZ.UI
{
    /// <summary>
    /// §34 Layer 2 组 A B1 状态一句话横幅 —— 顶部常驻单行
    ///
    /// 订阅 SurvivalGameManager.OnResourceUpdate + OnPhaseChanged，每 5s 或资源变化时刷新：
    ///   白天·安全（tension<30）         → "第 N 天白天 — X 名守护者正在采集资源，为夜晚做准备"
    ///   白天·资源紧张（food<20%）       → "第 N 天白天 — 食物告急！发 1 采集食物"
    ///   夜晚开始（刚进夜，无 Boss）     → "第 N 天夜晚 — 怪物来袭！发 6 全力攻击！"
    ///   夜晚·Boss（场上有 Boss）        → "Boss 降临！HP:{bossHp} — 发 6 全力攻击！"
    ///   夜晚·危急（gateHp<20%）         → "城门即将倒塌！刷礼物紧急修复！"
    ///
    /// 判定顺序：城门危急 > Boss > 夜晚基础 > 白天资源紧张 > 白天安全
    ///
    /// 实现口径（§34.3）：客户端本地生成文案，无需新协议；仅消费 resource_update / phase_changed。
    ///
    /// 挂载规则（CLAUDE.md #7）：挂 Canvas/StatusLineBanner（常驻激活），不在 Awake 中 SetActive(false)。
    /// Inspector 必填：
    ///   _bannerRoot   — 横幅根 RectTransform
    ///   _statusText   — 单行 TMP（白色描边字号 28）
    /// </summary>
    public class StatusLineBannerUI : MonoBehaviour
    {
        [Header("横幅根节点（常驻激活，通过 _statusText.gameObject.SetActive 控制实际显隐）")]
        [SerializeField] private RectTransform     _bannerRoot;
        [SerializeField] private TextMeshProUGUI   _statusText;

        [Header("参数")]
        [Tooltip("定时刷新间隔（秒）；OnResourceUpdate 触发时亦刷新")]
        [SerializeField] private float _refreshIntervalSec = 5f;

        // 阈值（与策划案 §34.3 B1 对齐）
        private const float FOOD_TIGHT_PCT = 0.2f;   // 食物 < 20% → 紧张
        private const float GATE_CRITICAL_PCT = 0.2f; // 城门 HP < 20% → 危急
        private const int   TENSION_SAFE_MAX = 29;    // tension < 30 → 安全

        private const int FOOD_BASELINE = 500;        // 食物满仓参考值（策划案 §4.1 _initFood=500）

        private float  _nextRefreshAt;
        private bool   _subscribed;

        // 当前阶段/天数（来自 phase_changed）
        private string _currentPhase = "day";
        private int    _currentDay   = 1;

        // 最近 resource_update 缓存（用于定时刷新场景）
        private int   _lastFood       = 0;
        private int   _lastGateHp     = 0;
        private int   _lastGateMaxHp  = 0;
        private int   _lastTension    = 0;

        // ── 生命周期 ──────────────────────────────────────────────────────

        private void Start()
        {
            // 初始文本：空字符串 + 激活，等首次 resource_update 再填
            if (_statusText != null) _statusText.text = "";
            _nextRefreshAt = Time.time + _refreshIntervalSec;
            TrySubscribe();
        }

        private void OnEnable()  { TrySubscribe(); }
        private void OnDisable() { Unsubscribe(); }
        private void OnDestroy() { Unsubscribe(); }

        private void Update()
        {
            if (!_subscribed) TrySubscribe();
            if (Time.time >= _nextRefreshAt)
            {
                _nextRefreshAt = Time.time + _refreshIntervalSec;
                RefreshStatus();
            }
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;
            sgm.OnResourceUpdate += HandleResourceUpdate;
            sgm.OnPhaseChanged   += HandlePhaseChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnResourceUpdate -= HandleResourceUpdate;
                sgm.OnPhaseChanged   -= HandlePhaseChanged;
            }
            _subscribed = false;
        }

        // ── 事件回调 ──────────────────────────────────────────────────────

        private void HandleResourceUpdate(ResourceUpdateData data)
        {
            if (data == null) return;
            _lastFood      = data.food;
            _lastGateHp    = data.gateHp;
            _lastGateMaxHp = data.gateMaxHp;
            _lastTension   = data.tension;
            RefreshStatus();
            _nextRefreshAt = Time.time + _refreshIntervalSec;
        }

        private void HandlePhaseChanged(PhaseChangedData data)
        {
            if (data == null) return;
            _currentPhase = data.phase ?? "day";
            _currentDay   = data.day;
            RefreshStatus();
            _nextRefreshAt = Time.time + _refreshIntervalSec;
        }

        // ── 文案生成 ──────────────────────────────────────────────────────

        private void RefreshStatus()
        {
            if (_statusText == null) return;

            // 1. 优先级 1：夜晚·危急（城门 HP < 20%）—— 只在夜晚 + 城门存在血量上限 + 低血
            bool isNight = _currentPhase == "night";
            float gateRatio = _lastGateMaxHp > 0 ? (float)_lastGateHp / _lastGateMaxHp : 1f;
            if (isNight && _lastGateMaxHp > 0 && gateRatio < GATE_CRITICAL_PCT && _lastGateHp > 0)
            {
                _statusText.text = "城门即将倒塌！刷礼物紧急修复！";
                return;
            }

            // 2. 优先级 2：夜晚·Boss（场上有 Boss 存活）
            if (isNight && IsBossAlive(out int bossHp))
            {
                _statusText.text = $"Boss 降临！HP:{bossHp} — 发 6 全力攻击！";
                return;
            }

            // 3. 优先级 3：夜晚基础（无 Boss / 未危急）
            if (isNight)
            {
                _statusText.text = $"第 {_currentDay} 天夜晚 — 怪物来袭！发 6 全力攻击！";
                return;
            }

            // 4. 优先级 4：白天·资源紧张（food < 20% of baseline）
            if (_lastFood > 0 && _lastFood < (int)(FOOD_BASELINE * FOOD_TIGHT_PCT))
            {
                _statusText.text = $"第 {_currentDay} 天白天 — 食物告急！发 1 采集食物";
                return;
            }

            // 5. 白天·安全（tension < 30 或无 tension 字段）
            int alive = GetAliveWorkerCount();
            _statusText.text = $"第 {_currentDay} 天白天 — {alive} 名守护者正在采集资源，为夜晚做准备";
        }

        private static int GetAliveWorkerCount()
        {
            var wm = WorkerManager.Instance;
            if (wm == null) return 0;
            int count = 0;
            foreach (var w in wm.ActiveWorkers)
                if (w != null && !w.IsDead) count++;
            return count;
        }

        /// <summary>是否有 Boss 存活；返回 true 时 out 最大 Boss HP。</summary>
        private static bool IsBossAlive(out int bossHp)
        {
            bossHp = 0;
            var spawner = MonsterWaveSpawner.Instance;
            if (spawner == null) return false;
            foreach (var m in spawner.ActiveMonsters)
            {
                if (m == null || m.IsDead) continue;
                if (m.Type == MonsterType.Boss)
                {
                    // 取首个存活 Boss 的 HP
                    bossHp = Mathf.Max(0, m.CurrentHp);
                    return true;
                }
            }
            return false;
        }
    }
}
