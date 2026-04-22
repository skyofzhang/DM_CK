using UnityEngine;
using TMPro;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §34 Layer 2 组 A B9 游戏中个人贡献条
    ///
    /// 订阅 SurvivalGameManager.OnPlayerStatsUpdated（work_command_response.playerStats）：
    ///   收到首条 playerStats 后常驻显示，每次更新刷新文案。
    ///
    /// MVP 简化文案（§34.3 B9 指引）：
    ///   "你：贡献：{contribution} 排名：#{rank} | 仙女棒加成：+{fairyWandBonus}%"
    ///
    /// 完整版文案（食物/煤炭/矿石个人计数）由后端在 playerStats 里再扩展 foodCount/coalCount/oreCount 字段后实装；
    ///   当前标 P3，前端先按 MVP 呈现。
    ///
    /// 挂载规则（CLAUDE.md #7）：挂 Canvas/GameUIPanel/PersonalContribBar（常驻激活），Awake 不 SetActive(false)；
    ///   _contribText.gameObject 首次收到 playerStats 前保持 inactive（初始隐藏，首次收到后 SetActive(true)）。
    ///
    /// Inspector 必填：
    ///   _contribText — 显示文本的 TMP（白色 20px 左对齐）
    /// </summary>
    public class PersonalContribUI : MonoBehaviour
    {
        [Header("文本节点（初始 inactive，首条 playerStats 到达后 SetActive(true)）")]
        [SerializeField] private TextMeshProUGUI _contribText;

        private bool _subscribed;
        private bool _shown;   // 首次收到 playerStats 后置 true

        // ── 生命周期 ──────────────────────────────────────────────────────

        private void Start()
        {
            if (_contribText != null) _contribText.gameObject.SetActive(false);
            TrySubscribe();

            // 支持断线重连：若 SGM 已缓存 LastPlayerStats，立即渲染
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null && sgm.LastPlayerStats != null)
                HandlePlayerStats(sgm.LastPlayerStats);
        }

        private void OnEnable()  { TrySubscribe(); }
        private void OnDisable() { Unsubscribe(); }
        private void OnDestroy() { Unsubscribe(); }

        private void Update()
        {
            if (!_subscribed) TrySubscribe();
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;
            sgm.OnPlayerStatsUpdated += HandlePlayerStats;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null) sgm.OnPlayerStatsUpdated -= HandlePlayerStats;
            _subscribed = false;
        }

        // ── 事件回调 ──────────────────────────────────────────────────────

        private void HandlePlayerStats(PlayerStatsData stats)
        {
            if (stats == null) return;
            if (_contribText == null) return;

            // MVP 简化版文案
            if (stats.fairyWandBonus > 0)
            {
                _contribText.text = $"你：贡献：{stats.contribution} 排名：#{stats.rank} | 仙女棒加成：+{stats.fairyWandBonus}%";
            }
            else
            {
                _contribText.text = $"你：贡献：{stats.contribution} 排名：#{stats.rank}";
            }

            if (!_shown)
            {
                _contribText.gameObject.SetActive(true);
                _shown = true;
            }
        }
    }
}
