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
        public const string GateUpgradeFailed   = "gate_upgrade_failed";
        public const string GateEffectTriggered = "gate_effect_triggered"; // 🆕 v1.22 §10 城门等级特性触发
        public const string GateDamageTaken     = "gate_damage_taken";     // 🔴 audit-r43 GAP-A43-01 §10.6.2/§10.7.3 Lv3+ 减伤双色飘字

        // ----- §16 永续模式失败降级 -----
        // EndGameFailed：end_game 在非 day/night 时被服务端拒（reason='wrong_phase'，§16.4）
        // RoomFailed 常量见下方 §36 section（与堡垒日降级同帧推送）
        public const string EndGameFailed = "end_game_failed";

        // ----- 矿工HP系统 -----
        public const string WorkerDied      = "worker_died";
        public const string WorkerRevived   = "worker_revived";
        public const string WorkerHpUpdate  = "worker_hp_update";

        // ----- §31 怪物多样性系统 -----
        public const string WorkerFrozen   = "worker_frozen";    // S→C：冰封怪冻结单名矿工（动态时长）
        public const string WorkerUnfrozen = "worker_unfrozen";  // S→C：冻结解除（到期 or T4 解冻）
        public const string BossEnraged    = "boss_enraged";     // S→C：首领卫兵全部死亡 → Boss 暴走

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
        public const string BroadcasterTraderResult        = "broadcaster_trader_result";  // Critical 修复: 交易结果反馈
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

        // ----- §39 商店系统 -----
        public const string ShopListData              = "shop_list_data";               // S→C：商品清单应答
        public const string ShopPurchaseConfirmPrompt = "shop_purchase_confirm_prompt"; // S→C：B 类 ≥1000 主播 HUD 双确认弹窗
        public const string ShopPurchaseConfirm       = "shop_purchase_confirm";        // S→C：购买成功房间广播
        public const string ShopPurchaseFailed        = "shop_purchase_failed";         // S→C：购买失败（unicast）
        public const string ShopEquipChanged          = "shop_equip_changed";           // S→C：装备切换成功（unicast）
        public const string ShopEquipFailed           = "shop_equip_failed";            // S→C：装备切换失败（unicast）
        public const string ShopInventoryData         = "shop_inventory_data";          // S→C：进房/重连推送 owned + equipped
        public const string ShopEffectTriggered       = "shop_effect_triggered";        // S→C：A 类效果触发房间广播
        // 🔴 audit-r45 GAP-E45-02：B6 entrance_spark 触发广播（spec §39.2 行 7139；服务端 handlePlayerJoined + handleShopEquip 双路径 emit）
        //   原仅 SurvivalGameManager.cs:840 字面量 case 路由，缺常量声明与同段 ShopXxx 命名规范不一致
        public const string EntranceSparkTriggered    = "entrance_spark_triggered";     // S→C：B6 入场特效触发（购买/装备/重连/玩家加入）
        // C→S 消息（客户端发送时直接用字面量，无需此常量解析）：
        //   shop_list / shop_purchase_prepare / shop_purchase / shop_equip

        // ----- §36 全服同步 + 赛季制（🆕 v1.27 MVP） -----
        public const string WorldClockTick     = "world_clock_tick";     // S→C：每秒广播（phase/seasonDay/themeId/phaseRemainingSec）
        public const string SeasonState        = "season_state";         // S→C：赛季状态快照（连接/主动请求）
        public const string FortressDayChanged = "fortress_day_changed"; // S→C：堡垒日变更（挺过/降级/新手保护/cap_blocked/cap_reset）
        public const string RoomFailed         = "room_failed";          // S→C：房间失败降级补充数据（与 fortress_day_changed 同帧推送）
        public const string SeasonStarted      = "season_started";       // S→C：赛季开始（MVP 占位；可由 season_state 替代）
        public const string SeasonSettlement   = "season_settlement";    // S→C：赛季结算广播（D7 夜晚结束或 Boss 池归零）

        // ----- §36.4 赛季 Boss Rush（🆕 v1.27） -----
        public const string SeasonBossRushStart  = "season_boss_rush_start";   // S→C：D7 夜晚开始赛季 Boss Rush
        public const string SeasonBossRushKilled = "season_boss_rush_killed";  // S→C：全服 Boss 血量池归零（dedup）

        // ----- §36.12 分时段解锁 & 老用户豁免（🆕 v1.27） -----
        public const string VeteranUnlocked          = "veteran_unlocked";            // S→C：玩家首次达到老用户豁免条件
        public const string BroadcasterActionFailed  = "broadcaster_action_failed";   // S→C：主播 ⚡加速/🌊事件触发失败（含 feature_locked）

        // ----- §36.12 reason 通用常量 -----
        public const string ReasonFeatureLocked = "feature_locked";

        // ----- §36.5 phase_changed.variant 常量 -----
        public const string PhaseVariantPeaceNight        = "peace_night";          // D1 整夜（柔光罩 + UI 提示）
        public const string PhaseVariantPeaceNightSilent  = "peace_night_silent";   // D2 整夜（无怪但不显示 UI）
        public const string PhaseVariantPeaceNightPrelude = "peace_night_prelude";  // D3 前 30s 和平期
        public const string PhaseVariantNormal            = "normal";               // 常规白天/夜晚
        public const string PhaseVariantRecovery          = "recovery";             // §16 永续模式恢复期

        // ----- §17.15 新手引导 -----
        // S→C：show_onboarding_sequence 广播 B1–B3 气泡连播（服务端 5 分钟节流）
        // C→S 消息（客户端发送时直接用字面量 "disable_onboarding_for_session"，无需此常量解析）
        public const string ShowOnboardingSequence = "show_onboarding_sequence";

        // ----- §36.12 feature id 常量（FEATURE_UNLOCK_DAY 键，供客户端查锁）-----
        public const string FeatureGateUpgradeBasic = "gate_upgrade_basic";   // Lv1–Lv4（D1 解锁）
        public const string FeatureGateUpgradeHigh  = "gate_upgrade_high";    // Lv5–Lv6（D4 解锁）
        public const string FeatureRoulette         = "roulette";             // §24.4 主播事件轮盘（D1 解锁）
        public const string FeatureBroadcasterBoost = "broadcaster_boost";    // §24.1 ⚡加速+🌊事件（D2 解锁）
        public const string FeatureShop             = "shop";                 // §39 商店系统（D2 解锁）
        public const string FeatureBuilding         = "building";             // §37 建造系统（D3 解锁）
        public const string FeatureExpedition       = "expedition";           // §38 探险系统（D5 解锁）
        public const string FeatureSupporterMode    = "supporter_mode";       // §33 助威模式（D6 解锁）
        public const string FeatureTribeWar         = "tribe_war";            // §35 跨直播间攻防战（D7 解锁）

        // ----- §35 跨直播间攻防战（Tribe War，🆕 v1.27） -----
        public const string TribeWarRoomListResult       = "tribe_war_room_list_result";       // S→C：大厅列表应答
        public const string TribeWarAttackFailed         = "tribe_war_attack_failed";          // S→C：攻击/反击失败（unicast 发起方）
        public const string TribeWarAttackStarted        = "tribe_war_attack_started";         // S→C：攻击开始广播（双方房间）
        public const string TribeWarUnderAttack          = "tribe_war_under_attack";           // S→C：被攻击通知（仅防守方房间）
        public const string TribeWarExpeditionSent       = "tribe_war_expedition_sent";        // S→C：远征怪已派出（仅攻击方房间）
        public const string TribeWarExpeditionIncoming   = "tribe_war_expedition_incoming";    // S→C：远征怪来袭（仅防守方房间）
        public const string TribeWarCombatReport         = "tribe_war_combat_report";          // S→C：攻击方战报
        public const string TribeWarCombatReportDefense  = "tribe_war_combat_report_defense";  // S→C：防守方战报
        public const string TribeWarAttackEnded          = "tribe_war_attack_ended";           // S→C：攻击结束广播（双方房间）
        // C→S 消息（客户端发送时直接用字面量，无需此常量解析）：
        //   tribe_war_room_list / tribe_war_attack / tribe_war_stop / tribe_war_retaliate

        // ----- §34 Layer 3 组 C 体验引擎（🆕 v1.27） -----
        // tension / giftRecommendation / totalContribution 附加在 resource_update 消息内，无需常量。
        public const string GloryMoment    = "glory_moment";     // S→C：E3a 荣耀时刻横幅
        public const string CoopMilestone  = "coop_milestone";   // S→C：E3b 全服合作里程碑
        public const string GiftImpact     = "gift_impact";      // S→C：E4 礼物影响详情

        // ----- §34 Layer 3 组 D 叙事引擎（🆕 v1.27） -----
        // act_tag / nightModifier 捎带在 phase_changed 消息内，无需常量。
        public const string ChapterChanged      = "chapter_changed";      // S→C：E2 幕切换（prologue/act1/act2/act3/finale）
        public const string StreamerPrompt      = "streamer_prompt";      // S→C：E5a 智能提词器（仅主播可见）
        public const string NightReport         = "night_report";         // S→C：E5b 夜战报告（夜→昼转换 2.5s）
        public const string EngagementReminder  = "engagement_reminder";  // S→C：E8 参与感唤回（每 5 分钟对贡献>0 玩家推送）
        // v1.27 §14 / §34.4 E9 废止：ChangeDifficulty 常量已删除（change_difficulty 协议不再存在）

        // ----- §34 Layer 2 组 B 数据流可视化（🆕 v1.27） -----
        // random_event 沿用 §2 "random_event" 常量；B3 仅扩展 eventId 枚举（前端 fallback 兜底）。
        public const string SettlementHighlights    = "settlement_highlights";      // S→C：B2 结算高光数据
        public const string StreamerSkipSettlement  = "streamer_skip_settlement";   // C→S：B2 主播"立即重开"跳过结算
        public const string EfficiencyRace          = "efficiency_race";            // S→C：B10a 安全期 Top2 PK 滚动
        public const string DayPreview              = "day_preview";                // S→C：B10b 白天最后 10s 夜晚预告

        // ----- §34 Layer 2 组 A 新手友好（🆕 v1.27） -----
        // B1 StatusLineBanner / B5 OreRepairFloatingText / B8 fairy_wand 视觉系统 / B9 PersonalContribUI
        public const string WorkCommandResponse = "work_command_response";  // S→C：B9 work_command 单播响应（playerStats）
        public const string FairyWandMaxed      = "fairy_wand_maxed";        // S→C：B8 仙女棒满级（+100% fairyWandBonus）

        // ----- 协议骨架补齐 Batch A（🆕 v1.27+） -----
        // 各模块统一错误/解锁/重连快照消息，具体业务处理由各自 Handler 实现。
        public const string RoomState                          = "room_state";                             // S→C：断线重连房间全量状态快照（§36/§37/§38/§35/§24.4）
        public const string FeatureUnlocked                    = "feature_unlocked";                       // S→C：§36.12 功能解锁单条事件（与 world_clock_tick.newlyUnlockedFeatures 互补）
        public const string TribeWarRetaliate                  = "tribe_war_retaliate";                    // C→S（反击请求）/ S→C（反击状态推送）：§35 P2 反击通道
        public const string BroadcasterRouletteEffectPrevented = "broadcaster_roulette_effect_prevented";  // S→C：§24.4 轮盘效果被阻止（unicast 发起方）

        // ----- §34 B7 新手引导（🆕 v1.27+ audit-r3/P1） -----
        public const string NewbieWelcome = "newbie_welcome"; // S→C：新玩家加入单播欢迎横幅（30s 浅色）
        public const string FirstBarrage  = "first_barrage";  // S→C：本局首次发送 1-6 弹幕 → 全房广播庆祝

        // ----- §36.10 WaitingPhase（🆕 v1.27+ audit-r3/P1） -----
        public const string WaitingPhaseStarted = "waiting_phase_started"; // S→C：波次等待窗口开始
        public const string WaitingPhaseEnded   = "waiting_phase_ended";   // S→C：波次等待窗口结束
        public const string SeasonPrepareStarted = "season_prepare_started"; // S→C：新赛季 30s 准备窗口开始
        public const string SeasonPrepareEnded   = "season_prepare_ended";   // S→C：新赛季准备窗口结束

        // ----- audit-r5 客户端补齐（🆕 v1.27+） -----
        // v1.27 §14 / §19 / §34.4 E9 废止：DifficultyChanged 常量已删除
        public const string WorkerShieldActivated   = "worker_shield_activated";   // S→C：§30.3 阶9 矿工 5s 护盾触发视觉反馈（r45 GAP-C45-03 注释失真修正）
        public const string FairyWandApplied        = "fairy_wand_applied";        // S→C：§34 B8 仙女棒累计光点（每次送都广播，含 capped）

        // ----- audit-r6 客户端补齐（🆕 v1.27+） -----
        // v1.27 §14 / §34.4 E9 废止：ChangeDifficultyFailed / ChangeDifficultyAccepted 常量已删除
        public const string DailyTierDecay           = "daily_tier_decay";           // S→C：§30.4 每日不活跃玩家等级衰减 ×0.95
        public const string GiftSkinApplied          = "gift_skin_applied";          // S→C：§30.7 T4/T5/T6 限时皮肤激活(G01/G02/G03)
        public const string GiftSkinExpired          = "gift_skin_expired";          // S→C：§30.7 限时皮肤到期（昼夜切换结束）

        // ----- audit-r8 客户端补齐（🆕 v1.27+） -----
        public const string BossWeaknessStarted = "boss_weakness_started"; // S→C：§34 F4 Boss 露出弹幕弱点 5s（发起 6 倍伤害 / T5 AOE 3 倍）
        public const string BossWeaknessEnded   = "boss_weakness_ended";   // S→C：§34 F4 Boss 弱点期结束
        public const string InvalidCommandHint  = "invalid_command_hint";  // S→C：§34 F8 指令无效单播提示（invalid_cmd_5 / wrong_phase_6）

        // ----- audit-r20 客户端补齐（🆕） -----
        public const string ChapterEndEvent          = "chapter_end_event";          // S→C：§34.3 D 段幕末事件全屏公告（first_elite/double_boss/mini_boss_rush/hp_amplified/boss_rush_finale）
        public const string FreeDeathPassTriggered   = "free_death_pass_triggered";  // S→C：§34.3 E3b 不朽证明（20000贡献）矿工免死豁免触发
        public const string RoomDestroyed            = "room_destroyed";             // S→C：§15 / §19.2 房间销毁通知（reason: 'timeout' 等）
    }
}
