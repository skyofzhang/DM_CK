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

        // ----- §30 矿工成长系统 -----
        public const string WorkerLevelUp         = "worker_level_up";
        public const string LegendReviveTriggered = "legend_revive_triggered";
        public const string WorkerSkinChanged     = "worker_skin_changed";
        public const string WorkerBlocked         = "worker_blocked";   // 阶6 15% 格挡视觉反馈
    }
}
