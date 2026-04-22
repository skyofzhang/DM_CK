using System.Collections.Generic;
using UnityEngine;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §34 Layer 2 组 A B8c 仙女棒累计气泡
    ///
    /// 在 Worker 头顶显示其 fairy_wand 累计加成百分比，类似 "+{bonus}%"；
    /// 满级（>= 100）时文字带金色星形标记。
    ///
    /// 数据源说明：
    /// 服务端 work_command 广播附带 playerStats：{ contribution, rank, fairyWandBonus }。
    /// 由于 PlayerStatsData 无 playerId 字段，这里依靠**同批 work_command**的 playerId 建立映射：
    ///   订阅 OnWorkCommand(WorkCommandData) → 同时能拿到 playerId + playerStats（如果服务端下发了）
    ///   每位玩家的 fairyWandBonus 缓存到 _bonusByPlayer，当收到新的 work_command 且 bonus > 0 时
    ///   把气泡推送到对应 Worker。
    ///
    /// 不显示气泡的条件：
    /// - bonus == 0
    /// - 找不到 Worker（玩家可能是助威者/匿名）
    ///
    /// 挂载规则（CLAUDE.md #7）：挂任意常驻 GO；无 Inspector 字段；运行时驱动 WorkerBubble。
    /// </summary>
    public class FairyWandAccumUI : MonoBehaviour
    {
        public static FairyWandAccumUI Instance { get; private set; }

        private bool _subscribed;

        // 每位玩家最近一次的 fairyWandBonus 缓存（0-100）
        private readonly Dictionary<string, int> _bonusByPlayer = new Dictionary<string, int>();

        // 气泡显示时长：足够跨越 N 个 work_command（每次刷新）而不是秒级闪烁
        private const float BUBBLE_DURATION_SEC = 6f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()    { TrySubscribe(); }
        private void OnEnable() { TrySubscribe(); }
        private void OnDisable(){ Unsubscribe(); }
        private void OnDestroy()
        {
            Unsubscribe();
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!_subscribed) TrySubscribe();
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;
            sgm.OnWorkCommand += HandleWorkCommand;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null) sgm.OnWorkCommand -= HandleWorkCommand;
            _subscribed = false;
        }

        // ── 事件回调 ──────────────────────────────────────────────────────

        private void HandleWorkCommand(WorkCommandData wc)
        {
            if (wc == null || string.IsNullOrEmpty(wc.playerId)) return;
            if (wc.playerStats == null) return;   // 老服务端不下发 → 跳过

            int bonus = wc.playerStats.fairyWandBonus;
            _bonusByPlayer[wc.playerId] = bonus;

            if (bonus > 0)
                ShowAccum(wc.playerId, bonus);
        }

        /// <summary>主动推送：在指定 Worker 头顶显示累计加成气泡。可供其它脚本复用。</summary>
        /// <param name="playerId">目标 Worker 的 playerId</param>
        /// <param name="bonusPct">累计加成百分比（0-100 整数）；&lt;=0 时不显示</param>
        public void ShowAccum(string playerId, int bonusPct)
        {
            if (string.IsNullOrEmpty(playerId)) return;
            if (bonusPct <= 0) return;

            var wm = WorkerManager.Instance;
            if (wm == null) return;

            // 满级（>=100%）时气泡文字带金色星形标记（策划案 §34.3 B8：满叠加全屏金闪由 MaxedBanner 处理）
            string text = bonusPct >= 100 ? $"★ +{bonusPct}% ★" : $"+{bonusPct}%";
            wm.ShowBubbleOnWorker(playerId, text, BUBBLE_DURATION_SEC);
        }
    }
}
