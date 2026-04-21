namespace DrscfZ.Survival
{
    /// <summary>
    /// 极地生存游戏消息类型常量
    /// 与服务器协议保持一致
    /// </summary>
    public static class SurvivalMessageProtocol
    {
        // ----- 游戏状态 -----
        public const string SurvivalGameState    = "survival_game_state";
        public const string PhaseChanged         = "phase_changed";
        public const string ResourceUpdate       = "resource_update";
        public const string MonsterWave          = "monster_wave";
        public const string WorkCommand          = "work_command";
        public const string SurvivalGift         = "survival_gift";
        public const string SurvivalPlayerJoined = "survival_player_joined";
        public const string SurvivalGameEnded    = "survival_game_ended";
        public const string PlayerJoined         = "player_joined"; // 旧协议兼容

        // ----- 战斗系统（新增）-----
        public const string CombatAttack = "combat_attack";
        public const string MonsterDied  = "monster_died";
        public const string NightCleared = "night_cleared";
        public const string GateUpgraded = "gate_upgraded";
        public const string BossAppeared = "boss_appeared";

        // ----- 城门系统 -----
        public const string GateUpgradeFailed = "gate_upgrade_failed";

        // ----- 矿工HP系统 -----
        public const string WorkerDied      = "worker_died";
        public const string WorkerRevived   = "worker_revived";
        public const string WorkerHpUpdate  = "worker_hp_update";

        // ----- 助威模式 §33 -----
        public const string SupporterJoined   = "supporter_joined";
        public const string SupporterAction   = "supporter_action";
        public const string SupporterPromoted = "supporter_promoted";
        public const string GiftSilentFail    = "gift_silent_fail";

        // ----- §30 矿工成长系统 -----
        public const string WorkerLevelUp         = "worker_level_up";
        public const string LegendReviveTriggered = "legend_revive_triggered";
        public const string WorkerSkinChanged     = "worker_skin_changed";
        public const string WorkerBlocked         = "worker_blocked";   // 阶6 15% 格挡视觉反馈

        // ----- §24.4 主播事件轮盘 -----
        public const string BroadcasterRouletteReady       = "broadcaster_roulette_ready";
        public const string BroadcasterRouletteResult      = "broadcaster_roulette_result";
        public const string BroadcasterRouletteEffectEnded = "broadcaster_roulette_effect_ended";
        public const string BroadcasterTraderOffer         = "broadcaster_trader_offer";
        // C→S 消息（客户端发送时直接用字面量，无需此常量解析）：
        //   broadcaster_roulette_spin / broadcaster_roulette_apply / broadcaster_trader_accept

        // ----- §38 探险系统 -----
        public const string ExpeditionCommand   = "expedition_command";   // C→S：主播/观众派出或召回探险
        public const string ExpeditionStarted   = "expedition_started";   // S→C：出发广播
        public const string ExpeditionEvent     = "expedition_event";     // S→C：外域事件推送
        public const string ExpeditionEventVote = "expedition_event_vote"; // C→S：主播对 trader_caravan 的接受/取消
        public const string ExpeditionReturned  = "expedition_returned";  // S→C：结算返回
        public const string ExpeditionFailed    = "expedition_failed";    // S→C：send/recall 被拒

        // ----- §37 建造系统 -----
        public const string BuildVoteStarted        = "build_vote_started";         // S→C：投票窗口打开
        public const string BuildVote               = "build_vote";                  // C→S：玩家投票
        public const string BuildVoteUpdate         = "build_vote_update";           // S→C：投票数实时更新（并行数组）
        public const string BuildVoteEnded          = "build_vote_ended";            // S→C：45s 窗口结束或全员投完
        public const string BuildStarted            = "build_started";               // S→C：选中建筑开始建造
        public const string BuildCompleted          = "build_completed";             // S→C：建造完成
        public const string BuildDemolished         = "build_demolished";            // S→C：单个建筑拆除
        public const string BuildProposeFailed      = "build_propose_failed";        // S→C：发起投票被拒
        public const string BuildCancelled          = "build_cancelled";             // S→C：投票通过但扣费时资源不足
        public const string BuildingDemolishedBatch = "building_demolished_batch";   // S→C：失败降级批量拆除
        public const string MonsterWaveIncoming     = "monster_wave_incoming";       // S→C：瞭望塔 10s 预告
        public const string BuildPropose            = "build_propose";               // C→S：主播或前 12 位守护者发起投票
    }
}
