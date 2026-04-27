using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using DrscfZ.Core;
using DrscfZ.Monster;
using DrscfZ.UI;
using DrscfZ.Systems;

namespace DrscfZ.Survival
{
    /// <summary>
    /// 极地生存游戏管理器（替代旧的角力 GameManager）
    /// - 订阅 NetworkManager.OnMessageReceived
    /// - 协调：DayNightCycleManager / ResourceSystem / CityGateSystem / MonsterWaveSpawner
    /// - 分发玩家工作指令到 WorkerManager
    /// 挂载到 Canvas（always active）
    /// </summary>
    public class SurvivalGameManager : MonoBehaviour
    {
        public static SurvivalGameManager Instance { get; private set; }

        [Header("子系统引用（在Inspector中拖入）")]
        public DayNightCycleManager dayNightManager;
        public ResourceSystem       resourceSystem;
        public CityGateSystem       cityGateSystem;
        public MonsterWaveSpawner   monsterWaveSpawner;
        public WorkerManager        workerManager;

        [Header("UI引用")]
        [SerializeField] private SurvivalSettlementUI _settlementUI;

        // 游戏状态
        public enum SurvivalState { Idle, Connecting, Waiting, Loading, Running, Settlement }
        [SerializeField] private SurvivalState _state = SurvivalState.Idle;
        public SurvivalState State => _state;

        // 难度系统
        public enum DifficultyLevel { None = 0, Easy = 1, Normal = 2, Hard = 3 }
        public DifficultyLevel SelectedDifficulty { get; private set; } = DifficultyLevel.None;

        // 加载方向（true=进入游戏，false=退出到大厅）
        public bool IsEnteringScene { get; private set; }

        // 结算后是否返回 Waiting（而非 Idle）
        private bool _returnToWaiting = false;

        // 加载超时协程
        private Coroutine _loadingTimeoutCoroutine;

        // 超时时长（秒）
        private const float LOADING_TIMEOUT_SECONDS = 15f;

        // gift_pause 暂停标志（T6 神秘空投 mystery_airdrop 特效期间，3000ms）
        // ⚠️ audit-r24 GAP-A24-07：r23 注释 "T5 神秘空投" 错位（T5=爱的爆炸 / T6=神秘空投，r23 GAP-C23-02 已修 AudioConstants 注释 + §29.2 doc，但漏修本字段注释）。
        private bool _isPaused = false;

        // 事件（UI订阅）
        public event Action<SurvivalState> OnStateChanged;
        public event Action<DifficultyLevel> OnDifficultySet;
        public event Action<WorkCommandData> OnWorkCommand;       // 工作指令 → WorkerManager
        public event Action<SurvivalGiftData> OnGiftReceived;    // 礼物效果
        public event Action<SurvivalPlayerJoinedData> OnPlayerJoined;
        public event Action<SurvivalGameEndedData> OnGameEnded;
        /// <summary>§16.4 end_game 被服务端拒绝（仅 state !∈ {day,night} 时推送）
        /// 订阅者：BroadcasterPanel / SettingsPanelUI 可据此在主播 HUD 显示"当前阶段不可结束"提示。</summary>
        public event Action<EndGameFailedData> OnEndGameFailed;
        public event Action<PhaseChangedData> OnPhaseChanged;    // 🆕 §4.2 昼夜/恢复期切换（携带 variant）
        public event Action<string> OnPlayerActivityMessage;     // 弹幕消息文本
        public event Action<int> OnScorePoolUpdated;             // 积分池变动（实时推送）
        public event Action<WeeklyRankingData>   OnWeeklyRankingReceived;   // 本周贡献榜（服务器推送）
        public event Action<LiveRankingData>     OnLiveRankingReceived;     // 实时贡献榜（游戏中防抖推送）
        public event Action<StreamerRankingData> OnStreamerRankingReceived; // 主播排行榜（服务器推送）

        // audit-r10 §29：记住上次 live_ranking Top1 的 playerId，用于检测"新王产生"播 RankUp SFX。
        private string _lastTop1PlayerId = null;

        // §16 / §4.2 最近一次 phase_changed 的 variant 缓存（"normal" | "recovery" | "peace_night*"）
        // HUD 推荐规则 R10-8 / PeaceNightOverlay / BroadcasterDecisionHUD 查询此字段过滤恢复期白天
        private string _lastPhaseVariant = "normal";
        public string LastPhaseVariant => _lastPhaseVariant;
        /// <summary>§4.2 当前 phase variant 别名（等同 LastPhaseVariant），UI 层推荐查询此属性。</summary>
        public string CurrentPhaseVariant => _lastPhaseVariant;
        /// <summary>§4.2 variant 变化事件：仅当 variant 字符串真正改变时触发一次；
        /// DayNightCycleManager / PeaceNightOverlay / BroadcasterDecisionHUD 等可直接订阅，
        /// 免去自己比较 OnPhaseChanged 去重。</summary>
        public event Action<string> OnPhaseVariantChanged;

        // 助威模式 §33
        public event Action<SupporterJoinedData>   OnSupporterJoined;
        public event Action<SupporterActionData>   OnSupporterAction;
        public event Action<SupporterPromotedData> OnSupporterPromoted;
        public event Action<GiftSilentFailData>    OnGiftSilentFail;

        // §30 矿工成长系统
        public event Action<WorkerLevelUpData>       OnWorkerLevelUp;
        public event Action<LegendReviveData>        OnLegendReviveTriggered;
        public event Action<WorkerSkinChangedData>   OnWorkerSkinChanged;

        // §24.4 主播事件轮盘
        public event Action<RouletteReadyData>       OnRouletteReady;
        public event Action<RouletteResultData>      OnRouletteResult;
        public event Action<RouletteEffectEndedData> OnRouletteEffectEnded;
        public event Action<TraderOfferData>         OnTraderOffer;

        // §38 探险系统
        public event Action<ExpeditionStartedData>  OnExpeditionStarted;
        public event Action<ExpeditionEventData>    OnExpeditionEvent;
        public event Action<ExpeditionReturnedData> OnExpeditionReturned;
        public event Action<ExpeditionFailedData>   OnExpeditionFailed;

        // §37 建造系统
        public event Action<BuildVoteStartedData>         OnBuildVoteStarted;
        public event Action<BuildVoteUpdateData>          OnBuildVoteUpdate;
        public event Action<BuildVoteEndedData>           OnBuildVoteEnded;
        public event Action<BuildStartedData>             OnBuildStarted;
        public event Action<BuildProgressData>            OnBuildProgress;  // 🆕 Batch I 补齐
        public event Action<BuildCompletedData>           OnBuildCompleted;
        public event Action<BuildDemolishedData>          OnBuildDemolished;
        public event Action<BuildProposeFailedData>       OnBuildProposeFailed;
        public event Action<BuildCancelledData>           OnBuildCancelled;
        public event Action<BuildingDemolishedBatchData>  OnBuildingDemolishedBatch;
        public event Action<MonsterWaveIncomingData>      OnMonsterWaveIncoming;

        // §34 Batch D Agent 协议补齐：单人贡献更新（Batch I 补齐事件声明）
        public event Action<ContributionUpdateData>       OnContributionUpdate;

        // §39 商店系统
        public event Action<ShopListData>                   OnShopListData;
        public event Action<ShopPurchaseConfirmPromptData>  OnShopPurchaseConfirmPrompt;
        public event Action<ShopPurchaseConfirmData>        OnShopPurchaseConfirm;
        public event Action<ShopPurchaseFailedData>         OnShopPurchaseFailed;
        public event Action<ShopEquipChangedData>           OnShopEquipChanged;
        public event Action<ShopEquipFailedData>            OnShopEquipFailed;
        public event Action<ShopInventoryData>              OnShopInventoryData;
        public event Action<ShopEffectTriggeredData>        OnShopEffectTriggered;

        // §35 跨直播间攻防战
        public event Action<TribeWarRoomListResultData>     OnTribeWarRoomListResult;
        public event Action<TribeWarAttackFailedData>       OnTribeWarAttackFailed;
        public event Action<TribeWarAttackStartedData>      OnTribeWarAttackStarted;
        public event Action<TribeWarUnderAttackData>        OnTribeWarUnderAttack;
        public event Action<TribeWarExpeditionSentData>     OnTribeWarExpeditionSent;
        public event Action<TribeWarExpeditionIncomingData> OnTribeWarExpeditionIncoming;
        public event Action<TribeWarCombatReportData>       OnTribeWarCombatReport;
        public event Action<TribeWarCombatReportData>       OnTribeWarCombatReportDefense;
        public event Action<TribeWarAttackEndedData>        OnTribeWarAttackEnded;

        // §36 全服同步 + 赛季制（🆕 v1.27 MVP）
        public event Action<WorldClockTickData>      OnWorldClockTick;
        public event Action<SeasonStateData>         OnSeasonState;
        public event Action<FortressDayChangedData>  OnFortressDayChanged;
        public event Action<RoomFailedData>          OnRoomFailed;
        public event Action<SeasonStartedData>       OnSeasonStarted;
        public event Action<SeasonSettlementData>    OnSeasonSettlement;

        // §36.4 赛季 Boss Rush（🆕 v1.27）
        public event Action<BossRushStartedData> OnBossRushStarted;
        public event Action<BossRushKilledData>  OnBossRushKilled;

        // §36.12 分时段解锁 & 老用户豁免（🆕 v1.27）
        public event Action<VeteranUnlockedData>         OnVeteranUnlocked;
        public event Action<BroadcasterActionFailedData> OnBroadcasterActionFailed;

        // §17.15 新手引导气泡（🆕 v1.27）
        public event Action<ShowOnboardingSequenceData> OnShowOnboardingSequence;

        // §34 B7 新手引导（🆕 v1.27+ audit-r3/P1）
        public event Action<NewbieWelcomeData> OnNewbieWelcome;   // 单播给本人（服务端已按 playerId 单播）
        public event Action<FirstBarrageData>  OnFirstBarrage;    // 广播给全房（浅绿 toast 3s）

        // §36.10 WaitingPhase（🆕 v1.27+ audit-r3/P1）
        public event Action<WaitingPhaseStartedData> OnWaitingPhaseStarted;
        public event Action<WaitingPhaseEndedData>   OnWaitingPhaseEnded;

        // §24.5 主播决策中心 HUD（🆕 v1.27）：resource_update 事件转发，供 HUD 触发推荐重算
        public event Action<ResourceUpdateData> OnResourceUpdate;

        // §10 城门等级特性触发事件（🆕 v1.22）：CityGateSystem / VFX 层订阅 → 播放光环 / 冲击波视觉。
        // 本事件仅做信号转发；具体视觉由订阅者（CityGateSystem / MonsterWaveSpawner）按 effect 字段分流。
        public event Action<GateEffectTriggeredData> OnGateEffectTriggered;

        // §34 Layer 3 组 C 体验引擎（🆕 v1.27）：GloryMoment / CoopMilestone / GiftImpact
        public event Action<GloryMomentData>   OnGloryMoment;
        public event Action<CoopMilestoneData> OnCoopMilestone;
        public event Action<GiftImpactData>    OnGiftImpact;

        // §34 Layer 3 组 D 叙事引擎（🆕 v1.27）：ChapterChanged / StreamerPrompt / NightReport / EngagementReminder
        // phase_changed 扩展字段（act_tag / nightModifier）通过既有 OnPhaseChanged 事件传递，无需新增事件。
        public event Action<ChapterChangedData>     OnChapterChanged;
        public event Action<StreamerPromptData>     OnStreamerPrompt;
        public event Action<NightReportData>        OnNightReport;
        public event Action<EngagementReminderData> OnEngagementReminder;

        // ----- §34 Layer 2 组 B 数据流可视化（🆕 v1.27）-----
        // settlement_highlights / efficiency_race / day_preview；random_event 沿用既有路由。
        public event Action<SettlementHighlightsData> OnSettlementHighlights;
        public event Action<EfficiencyRaceData>       OnEfficiencyRace;
        public event Action<DayPreviewData>           OnDayPreview;

        // ----- §34 Layer 2 组 A 新手友好（🆕 v1.27）-----
        // B1 StatusLineBanner 订阅 OnResourceUpdate / OnPhaseChanged（已有）；
        // B5 OreRepairFloatingText 订阅 OnResourceUpdate（已有）；
        // B8 fairy_wand 视觉系统：OnGiftImpact（已有，过滤 giftId=='fairy_wand'）+ OnFairyWandMaxed（新）；
        // B9 PersonalContribUI 订阅 OnPlayerStatsUpdated（新，随 work_command_response 分发）。
        public event Action<PlayerStatsData>    OnPlayerStatsUpdated;   // B9 每次 work_command_response 分发
        public event Action<FairyWandMaxedData> OnFairyWandMaxed;       // B8 仙女棒满级单播（+100% 满级）

        // ----- audit-r5 客户端补齐（🆕 v1.27+） -----
        public event Action<DifficultyChangedData>     OnDifficultyChanged;     // §19/§34.4 E9 难度生效广播
        public event Action<WorkerShieldActivatedData> OnWorkerShieldActivated; // §30.3 阶8 护盾触发视效
        public event Action<FairyWandAppliedData>      OnFairyWandApplied;      // §34 B8 仙女棒累计光点

        // audit-r6 客户端补齐
        public event Action<ChangeDifficultyFailedData>   OnChangeDifficultyFailed;   // §34.4 E9 切换难度失败
        public event Action<ChangeDifficultyAcceptedData> OnChangeDifficultyAccepted; // §34.4 E9 切换难度已排队
        public event Action<DailyTierDecayData>            OnDailyTierDecay;            // §30.4 每日不活跃等级衰减
        public event Action<GiftSkinAppliedData>           OnGiftSkinApplied;           // §30.7 T4/T5/T6 限时皮肤激活
        public event Action<GiftSkinExpiredData>           OnGiftSkinExpired;           // §30.7 限时皮肤到期

        // audit-r8 客户端补齐（§34 F 层）
        public event Action<BossWeaknessStartedData> OnBossWeaknessStarted; // §34 F4 Boss 露出弱点 5s
        public event Action<BossWeaknessEndedData>   OnBossWeaknessEnded;   // §34 F4 Boss 弱点结束
        public event Action<InvalidCommandHintData>  OnInvalidCommandHint;  // §34 F8 无效指令单播提示

        // ----- audit-r20 客户端补齐（🆕） -----
        public event Action<ChapterEndEventData>        OnChapterEndEvent;          // §34.3 D 段幕末事件全屏公告
        public event Action<FreeDeathPassTriggeredData> OnFreeDeathPassTriggered;   // §34.3 E3b 不朽证明（20000贡献）矿工免死豁免触发
        public event Action<RoomDestroyedData>          OnRoomDestroyed;            // §15 / §19.2 房间销毁通知（reason:'timeout' 等）

        /// <summary>§34 B9 最近一次收到的 playerStats（供 UI 查询，初始为 null）。
        /// 收到第一条 work_command_response.playerStats 后 PersonalContribUI 常驻显示。</summary>
        public PlayerStatsData LastPlayerStats { get; private set; }

        // 🆕 Fix C (组 B Reviewer P0) §34B B3：RandomEvent 事件总线
        //   订阅者：WorkerManager（morale_boost 矿工气泡）/ 其他 UI（aurora_flash / airdrop 等）。
        public event Action<RandomEventData> OnRandomEvent;

        // ----- 协议骨架补齐 Batch A（🆕 v1.27+） -----
        // 断线重连房间快照 / §36.12 功能解锁单事件 / §24.4 轮盘效果被阻止 / §35 P2 反击状态
        // 各订阅者：UI 层读取后按自身业务决定如何处理（本管理器不实现具体业务逻辑）。
        public event Action<RoomStateData>                 OnRoomState;
        public event Action<FeatureUnlockedData>           OnFeatureUnlocked;
        public event Action<RouletteEffectPreventedData>   OnRouletteEffectPrevented;
        public event Action<TribeWarRetaliateData>         OnTribeWarRetaliate;

        /// <summary>§36.12 seasonDay 从 N→N+1 递增的那一秒新解锁的功能 id 列表（由 world_clock_tick 触发）。
        /// 参数是该 tick 携带的 newlyUnlockedFeatures 字段内容，UI 层据此逐条播放解锁横幅。</summary>
        public event Action<string[]> OnNewlyUnlockedFeatures;

        /// <summary>§36.12 当前已解锁功能 id 集合（全集）变更通知。
        /// 由 season_state / survival_game_state 的 unlockedFeatures 字段触发；UI 层据此刷新按钮锁态。</summary>
        public event Action<string[]> OnUnlockedFeaturesSync;

        /// <summary>§36.12 当前已解锁功能 id 集合（只读缓存，UI 层查询按钮锁态）。
        /// 由 season_state / survival_game_state 同步；首次收到前为 null。</summary>
        public IReadOnlyList<string> CurrentUnlockedFeatures { get; private set; }

        /// <summary>§36 缓存的当前赛季/全服时钟状态，供 UI 层按需查询。
        /// phase/phaseRemainingSec 随 world_clock_tick 每秒刷新；seasonDay/themeId/seasonId
        /// 随 world_clock_tick + season_state 同步；首次收到任意一方消息前保持为 null。</summary>
        public SeasonRuntimeState CurrentSeasonState { get; private set; }

        // §39 本地装备缓存（自己最新的 equipped，供 UI 层做灰化/装备按钮回显）
        public ShopEquipped MyEquipped { get; private set; }

        // §34 B2 结算高光缓存：服务端在 survival_game_ended 前推送 settlement_highlights，
        // 客户端缓存最近一次，结算 UI 的帧 A 直接读取；非空时由 Reset 清理。
        public SettlementHighlightsData LastSettlementHighlights => _lastSettlementHighlights;
        private SettlementHighlightsData _lastSettlementHighlights;

        // 贡献追踪
        private System.Collections.Generic.Dictionary<string, float> _contributions
            = new System.Collections.Generic.Dictionary<string, float>();
        public int TotalPlayers => _contributions.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            var net = NetworkManager.Instance;
            if (net != null)
                net.OnMessageReceived += HandleMessage;

            // 初始化子系统
            resourceSystem?.Initialize();
            cityGateSystem?.Initialize();
            dayNightManager?.Reset();

            // 订阅失败条件
            if (resourceSystem != null)
            {
                resourceSystem.OnFoodDepleted += () => HandleDefeat("food_depleted");
                resourceSystem.OnTempFreeze   += () => HandleDefeat("temp_freeze");
            }
            if (cityGateSystem != null)
                cityGateSystem.OnGateBreached += () => HandleDefeat("gate_breached");

            // 订阅昼夜切换事件 → 顶部飘字
            if (dayNightManager != null)
            {
                dayNightManager.OnNightStarted += day => UI.TopFloatingTextUI.Instance?.ShowDanger("【夜袭】怪物入侵！全员防守！");
                dayNightManager.OnDayStarted   += day => UI.TopFloatingTextUI.Instance?.ShowGold("【黎明】守住了！太阳升起！");
            }

            Debug.Log("[SurvivalGM] 极地生存游戏管理器已启动");
        }

        private void OnDestroy()
        {
            var net = NetworkManager.Instance;
            if (net != null)
                net.OnMessageReceived -= HandleMessage;
        }

        // ==================== 网络消息分发 ====================

        private void HandleMessage(string type, string dataJson)
        {
            try { HandleMessageInternal(type, dataJson); }
            catch (Exception ex)
            {
                // #8 单条消息解析异常不应崩溃整个处理链
                Debug.LogError($"[SGM] HandleMessage 异常 type={type}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void HandleMessageInternal(string type, string dataJson)
        {
            // 结算/空闲状态下只处理连接确认和周榜，忽略所有游戏内消息
            // 避免结算期间礼物效果、昼夜切换、怪物等继续触发
            // 🆕 §16 永续模式：放行 phase_changed / survival_game_state，
            //   用于 Settlement 收到 variant=recovery 时切回 Running（服务端 8s 自动恢复路径）
            //   及断线重连到 recovery 期（state=day + variant=recovery）的兜底触发
            if (_state == SurvivalState.Settlement || _state == SurvivalState.Idle)
            {
                if (type != "join_room_confirm"
                    && type != "weekly_ranking"
                    && type != "streamer_ranking"
                    && type != "phase_changed"        // 🆕 §16 永续模式恢复期需要
                    && type != "survival_game_state") // 🆕 §16 断线重连 state=day+variant=recovery 需要
                    return;
            }

            switch (type)
            {
                // ----- 连接确认：Connecting → Waiting -----
                case "join_room_confirm":
                    var jrc = JsonUtility.FromJson<JoinRoomConfirmData>(dataJson);
                    HandleJoinRoomConfirm(jrc);
                    break;

                // ----- 游戏已启动（服务器 has_active_session=true 时推送）-----
                case "game_started":
                    if (_state == SurvivalState.Waiting || _state == SurvivalState.Loading)
                    {
                        StopLoadingTimeout();
                        ChangeState(SurvivalState.Running);
                        Debug.Log("[SGM] game_started → Running");
                    }
                    break;

                // ----- 游戏状态 -----
                case "survival_game_state":
                    var gs = JsonUtility.FromJson<SurvivalGameStateData>(dataJson);
                    HandleGameState(gs);
                    break;

                // ----- 昼夜切换 -----
                case "phase_changed":
                    var pc = JsonUtility.FromJson<PhaseChangedData>(dataJson);
                    // 🆕 §4.2 缓存 variant（旧消息字段缺失时 JsonUtility 会保留默认 "normal"；空字符串兜底）
                    string newVariant = string.IsNullOrEmpty(pc.variant) ? "normal" : pc.variant;
                    bool variantChanged = newVariant != _lastPhaseVariant;
                    _lastPhaseVariant = newVariant;
                    if (variantChanged)
                        OnPhaseVariantChanged?.Invoke(newVariant);

                    // 🆕 §16 永续模式：Settlement 态收到 variant=recovery → 回 Running
                    //   服务端 8s 结算 UI 自动关闭后推送 phase_changed{variant:recovery}（§23.3 T=11s）
                    //   这是客户端从 Settlement 回到 Running 的唯一合法路径
                    //   Idle 态收到 recovery 不做状态切换（玩家还未进入场景，应走 join_room_confirm/reconnect 流程）
                    if (_state == SurvivalState.Settlement && _lastPhaseVariant == "recovery")
                    {
                        Debug.Log("[SGM] Settlement 态收到 phase_changed{variant=recovery} → 切回 Running（§16 永续模式）");
                        ChangeState(SurvivalState.Running);
                        if (_settlementUI != null && _settlementUI.gameObject.activeSelf)
                            _settlementUI.gameObject.SetActive(false);  // 关闭结算面板（若 8s 自动关闭未触发）
                    }

                    // 结算/空闲状态下仍忽略昼夜计时器/防守切换等副作用（避免残留状态污染 UI）
                    //   注：放行 Settlement→Running 的切换已在上方完成，此 guard 仅拦截 dayNight/worker 副作用
                    if (_state == SurvivalState.Settlement || _state == SurvivalState.Idle)
                    {
                        OnPhaseChanged?.Invoke(pc);  // 事件仍然触发，让订阅者（HUD 等）识别 variant
                        break;
                    }

                    dayNightManager?.HandlePhaseChanged(pc);
                    // 黑夜开始 → 全员防守；白天开始 → 回工位
                    if (pc.phase == "night")
                        WorkerManager.Instance?.EnterNightDefense();
                    else if (pc.phase == "day")
                        WorkerManager.Instance?.ExitNightDefense();
                    OnPhaseChanged?.Invoke(pc);
                    break;

                // ----- 资源更新 -----
                case "resource_update":
                    var ru = JsonUtility.FromJson<ResourceUpdateData>(dataJson);
                    resourceSystem?.ApplyServerUpdate(ru);
                    cityGateSystem?.SyncFromServer(ru.gateHp, ru.gateMaxHp);
                    // 🆕 v1.22 §10 同步城门层级元数据（若服务端下发）
                    if (cityGateSystem != null)
                    {
                        cityGateSystem.ApplyDailyUpgraded(ru.gateDailyUpgraded);
                        cityGateSystem.ApplyTierMeta(ru.gateTierName, ru.gateFeatures);
                    }
                    dayNightManager?.SyncRemainingTime(ru.remainingTime);
                    if (ru.scorePool > 0) OnScorePoolUpdated?.Invoke(ru.scorePool);
                    // 🆕 §24.5 主播决策中心 HUD 依赖：每次资源更新触发推荐重算
                    OnResourceUpdate?.Invoke(ru);
                    break;

                // ----- §17.15 新手引导气泡（S→C：show_onboarding_sequence）-----
                case "show_onboarding_sequence":
                    var sos = JsonUtility.FromJson<ShowOnboardingSequenceData>(dataJson);
                    if (sos != null) OnShowOnboardingSequence?.Invoke(sos);
                    break;

                // ----- §34 B7 新手引导（audit-r3/P1）-----
                case "newbie_welcome":
                    var nw = JsonUtility.FromJson<NewbieWelcomeData>(dataJson);
                    if (nw != null) OnNewbieWelcome?.Invoke(nw);
                    break;

                case "first_barrage":
                    var fb = JsonUtility.FromJson<FirstBarrageData>(dataJson);
                    if (fb != null) OnFirstBarrage?.Invoke(fb);
                    break;

                // ----- §36.10 WaitingPhase（audit-r3/P1）-----
                case "waiting_phase_started":
                    var wps = JsonUtility.FromJson<WaitingPhaseStartedData>(dataJson);
                    if (wps != null) OnWaitingPhaseStarted?.Invoke(wps);
                    break;

                case "waiting_phase_ended":
                    var wpe = JsonUtility.FromJson<WaitingPhaseEndedData>(dataJson) ?? new WaitingPhaseEndedData();
                    OnWaitingPhaseEnded?.Invoke(wpe);
                    break;

                // ----- 怪物波次 -----
                case "monster_wave":
                    var mw = JsonUtility.FromJson<MonsterWaveData>(dataJson);
                    // ⚠️ audit-r24 GAP-A24-04：r23 GAP-A23-05 加 isDaytimeScout 字段后客户端 0 消费的半成品延续修复
                    // §11 E03 white-day scout 弱怪 atk=0 hp×0.3，仅做视觉/玩法占位，不计入战斗判定
                    if (mw != null && mw.isDaytimeScout)
                    {
                        Debug.Log($"[SGM] monster_wave isDaytimeScout=true wave={mw.waveIndex} count={mw.count} — 跳过 _activeMonsters 战斗逻辑");
                        UI.HorizontalMarqueeUI.Instance?.AddMessage(null, null, "白天侦察兵出现，注意警戒！");
                        // 不调 SpawnWave，仅 UI 提示（保持 _activeMonsters 干净）
                        break;
                    }
                    // ⚠️ audit-r24 GAP-A24-03：elite_raid 路径（§24.4 主播轮盘"精英来袭"卡触发的单只精英怪）
                    if (mw != null && mw.monsterType == "elite_raid")
                    {
                        Debug.Log($"[SGM] monster_wave elite_raid hp={mw.eliteHp} atk={mw.eliteAtk} bypassCap={mw.bypassCap}");
                        UI.HorizontalMarqueeUI.Instance?.AddMessage(null, null, $"精英来袭！HP {mw.eliteHp}，30s 内击杀方可脱险");
                    }
                    // ⚠️ audit-r24 GAP-C24-03：isBossGuardSpawn 路径（Boss 卫兵召唤）
                    if (mw != null && mw.isBossGuardSpawn)
                    {
                        Debug.Log($"[SGM] monster_wave isBossGuardSpawn=true count={mw.count}");
                        SurvivalCameraController.Shake(0.18f, 0.4f);
                        UI.HorizontalMarqueeUI.Instance?.AddMessage(null, null, "Boss 召唤卫兵护驾！");
                    }
                    monsterWaveSpawner?.SpawnWave(mw);
                    // 通知 WorkerManager 怪物出现，让闲置 Worker 自动攻击
                    WorkerManager.Instance?.OnMonstersAppear();
                    break;

                // ----- 矿工死亡/复活/HP全量同步（HP系统）-----
                case "worker_died":
                    var wd = JsonUtility.FromJson<WorkerDiedData>(dataJson);
                    WorkerManager.Instance?.HandleWorkerDied(wd.playerId, wd.respawnAt);
                    // audit-r21 GAP-A21-01：消费 wd.reason 推送差异化跑马灯（r19 加字段，r21 补 UI 链路）
                    // 服务端 emit reason: 'blizzard' / 'expedition_died' / 'expedition_night_kia' / ''（普通战斗）
                    if (!string.IsNullOrEmpty(wd.reason))
                    {
                        string deathDisplayName = ResolveDisplayName(wd.playerId);
                        string deathHint;
                        switch (wd.reason)
                        {
                            case "blizzard":             deathHint = "被暴风雪夺命"; break;
                            case "expedition_died":      deathHint = "外域遇险阵亡"; break;
                            case "expedition_night_kia": deathHint = "外域夜战阵亡"; break;
                            default:                     deathHint = null; break;
                        }
                        if (!string.IsNullOrEmpty(deathHint))
                        {
                            UI.HorizontalMarqueeUI.Instance?.AddMessage(deathDisplayName, null, deathHint);
                            OnPlayerActivityMessage?.Invoke($"{deathDisplayName} {deathHint}");
                        }
                    }
                    break;

                case "worker_revived":
                    var wrv = JsonUtility.FromJson<WorkerRevivedData>(dataJson);
                    WorkerManager.Instance?.HandleWorkerRevived(wrv.playerId);
                    break;

                case "worker_hp_update":
                    var hpu = JsonUtility.FromJson<WorkerHpUpdateData>(dataJson);
                    if (hpu?.workers != null)
                        WorkerManager.Instance?.HandleWorkerHpUpdate(hpu.workers);
                    break;

                // ----- §31 怪物多样性 个人冻结 / Boss 暴走 -----
                case "worker_frozen":
                    var wf = JsonUtility.FromJson<WorkerFrozenData>(dataJson);
                    if (wf != null)
                        WorkerManager.Instance?.HandleWorkerFrozen(wf.playerId, wf.duration);
                    break;

                case "worker_unfrozen":
                    var wuf = JsonUtility.FromJson<WorkerUnfrozenData>(dataJson);
                    if (wuf != null)
                        WorkerManager.Instance?.HandleWorkerUnfrozen(wuf.playerId);
                    break;

                case "boss_enraged":
                    var be = JsonUtility.FromJson<BossEnragedData>(dataJson);
                    if (be != null) HandleBossEnraged(be);
                    break;

                // ----- 玩家工作指令（评论触发）-----
                case "work_command":
                    var wc = JsonUtility.FromJson<WorkCommandData>(dataJson);
                    HandleWorkCommand(wc);
                    break;

                // ----- 礼物效果 -----
                case "survival_gift":
                    var sg = JsonUtility.FromJson<SurvivalGiftData>(dataJson);
                    HandleGift(sg);
                    break;

                // ----- 玩家加入 -----
                case "survival_player_joined":
                    var pj = JsonUtility.FromJson<SurvivalPlayerJoinedData>(dataJson);
                    HandlePlayerJoined(pj);
                    break;

                // ----- 结算 -----
                case "survival_game_ended":
                    var ge = JsonUtility.FromJson<SurvivalGameEndedData>(dataJson);
                    HandleGameEnded(ge);
                    break;

                // ----- §16.4 end_game 被拒（非 day/night 态）-----
                case "end_game_failed":
                    var egf = JsonUtility.FromJson<EndGameFailedData>(dataJson);
                    Debug.LogWarning($"[SGM] end_game rejected: reason={egf?.reason} currentState={egf?.currentState}");
                    // 主播反馈：跑马灯 + 事件分发。UI 侧订阅 OnEndGameFailed 可弹提示。
                    if (egf != null)
                    {
                        string reasonCn = egf.reason == "wrong_phase" ? "当前阶段不可结束本次守护" : $"结束被拒:{egf.reason}";
                        OnPlayerActivityMessage?.Invoke($"【提示】{reasonCn}（{egf.currentState}）");
                        OnEndGameFailed?.Invoke(egf);
                    }
                    break;

                // ----- 兼容旧协议（游戏未重构服务器时先用旧的player_joined）-----
                case "player_joined":
                    var oldPj = JsonUtility.FromJson<SurvivalPlayerJoinedData>(dataJson);
                    HandlePlayerJoined(oldPj);
                    break;

                // ----- 主播效果（broadcaster_action 的广播回调）-----
                case "broadcaster_effect":
                    HandleBroadcasterEffect(dataJson);
                    break;

                // ----- 特效消息（666弹幕全员光晕 等）-----
                case "special_effect":
                    HandleSpecialEffect(dataJson);
                    break;

                // ----- 随机事件公告 -----
                case "random_event":
                    HandleRandomEvent(dataJson);
                    break;

                // ----- T5礼物暂停（神秘空投落地特效）-----
                case "gift_pause":
                    HandleGiftPause(dataJson);
                    break;

                // ----- 服务器弹幕播报 -----
                case "bobao":
                    var bobaoData = JsonUtility.FromJson<BobaoData>(dataJson);
                    if (bobaoData != null && !string.IsNullOrEmpty(bobaoData.message))
                        OnPlayerActivityMessage?.Invoke(bobaoData.message);
                    break;

                // ----- 战斗 -----
                case "combat_attack":
                    HandleCombatAttack(type, dataJson);
                    break;
                case "monster_died":
                    HandleMonsterDied(type, dataJson);
                    break;
                case "night_cleared":
                    HandleNightCleared(type, dataJson);
                    break;
                case "gate_upgraded":
                    HandleGateUpgraded(type, dataJson);
                    break;
                case "gate_upgrade_failed":
                    HandleGateUpgradeFailed(dataJson);
                    break;
                case "gate_effect_triggered":
                    HandleGateEffectTriggered(dataJson);
                    break;
                case "game_paused":
                    Debug.Log("[SGM] game_paused received (GM command)");
                    break;
                case "boss_appeared":
                    HandleBossAppeared(type, dataJson);
                    break;

                // ----- 本周贡献榜（局结束后服务器推送 / 客户端主动请求）-----
                case "weekly_ranking":
                    var wr = JsonUtility.FromJson<WeeklyRankingData>(dataJson);
                    if (wr != null) OnWeeklyRankingReceived?.Invoke(wr);
                    break;

                // ----- 主播排行榜（服务器推送）-----
                case "streamer_ranking":
                    var sr = JsonUtility.FromJson<StreamerRankingData>(dataJson);
                    if (sr != null) OnStreamerRankingReceived?.Invoke(sr);
                    break;

                // ----- 实时贡献榜（游戏进行中，贡献变化后服务器防抖推送）-----
                case "live_ranking":
                    var lr = JsonUtility.FromJson<LiveRankingData>(dataJson);
                    if (lr != null)
                    {
                        // audit-r10 §29：本玩家自身排名变化 → rank_up/down SFX（仅 top10 内变化时）
                        TryPlayRankChangeSfx(lr);
                        OnLiveRankingReceived?.Invoke(lr);
                    }
                    break;

                // ----- 助威模式 §33 -----
                case "supporter_joined":
                    HandleSupporterJoined(JsonUtility.FromJson<SupporterJoinedData>(dataJson));
                    break;
                case "supporter_action":
                    HandleSupporterAction(JsonUtility.FromJson<SupporterActionData>(dataJson));
                    break;
                case "supporter_promoted":
                    HandleSupporterPromoted(JsonUtility.FromJson<SupporterPromotedData>(dataJson));
                    break;
                case "gift_silent_fail":
                    HandleGiftSilentFail(JsonUtility.FromJson<GiftSilentFailData>(dataJson));
                    break;

                // ----- §30 矿工成长系统 -----
                case "worker_level_up":
                    var wlu = JsonUtility.FromJson<WorkerLevelUpData>(dataJson);
                    if (wlu != null) HandleWorkerLevelUp(wlu);
                    break;
                case "legend_revive_triggered":
                    var lrv = JsonUtility.FromJson<LegendReviveData>(dataJson);
                    if (lrv != null) HandleLegendReviveTriggered(lrv);
                    break;
                case "worker_skin_changed":
                    var wsc = JsonUtility.FromJson<WorkerSkinChangedData>(dataJson);
                    if (wsc != null) HandleWorkerSkinChanged(wsc);
                    break;
                case "worker_blocked":
                    var wbk = JsonUtility.FromJson<WorkerBlockedData>(dataJson);
                    if (wbk != null) HandleWorkerBlocked(wbk);
                    break;

                // ----- §24.4 主播事件轮盘 -----
                case "broadcaster_roulette_ready":
                    var rrd = JsonUtility.FromJson<RouletteReadyData>(dataJson);
                    if (rrd != null) HandleRouletteReady(rrd);
                    break;
                case "broadcaster_roulette_result":
                    var rrs = JsonUtility.FromJson<RouletteResultData>(dataJson);
                    if (rrs != null) HandleRouletteResult(rrs);
                    break;
                case "broadcaster_roulette_effect_ended":
                    var ree = JsonUtility.FromJson<RouletteEffectEndedData>(dataJson);
                    if (ree != null) HandleRouletteEffectEnded(ree);
                    break;
                case "broadcaster_trader_offer":
                    var tof = JsonUtility.FromJson<TraderOfferData>(dataJson);
                    if (tof != null) HandleTraderOffer(tof);
                    break;
                case "broadcaster_trader_result":
                    var tre = JsonUtility.FromJson<BroadcasterTraderResultData>(dataJson);
                    if (tre != null) HandleBroadcasterTraderResult(tre);
                    break;

                // ----- §38 探险系统 -----
                case "expedition_started":
                    var expS = JsonUtility.FromJson<ExpeditionStartedData>(dataJson);
                    if (expS != null) HandleExpeditionStarted(expS);
                    break;
                case "expedition_event":
                    var expE = JsonUtility.FromJson<ExpeditionEventData>(dataJson);
                    if (expE != null) HandleExpeditionEvent(expE);
                    break;
                case "expedition_returned":
                    var expR = JsonUtility.FromJson<ExpeditionReturnedData>(dataJson);
                    if (expR != null) HandleExpeditionReturned(expR);
                    break;
                case "expedition_failed":
                    var expF = JsonUtility.FromJson<ExpeditionFailedData>(dataJson);
                    if (expF != null) HandleExpeditionFailed(expF);
                    break;

                // ----- §37 建造系统 -----
                case "build_vote_started":
                    var bvs = JsonUtility.FromJson<BuildVoteStartedData>(dataJson);
                    if (bvs != null) HandleBuildVoteStarted(bvs);
                    break;
                case "build_vote_update":
                    var bvu = JsonUtility.FromJson<BuildVoteUpdateData>(dataJson);
                    if (bvu != null) HandleBuildVoteUpdate(bvu);
                    break;
                case "build_vote_ended":
                    var bve = JsonUtility.FromJson<BuildVoteEndedData>(dataJson);
                    if (bve != null) HandleBuildVoteEnded(bve);
                    break;
                case "build_started":
                    var bst = JsonUtility.FromJson<BuildStartedData>(dataJson);
                    if (bst != null) HandleBuildStarted(bst);
                    break;
                case "build_progress":
                    var bpr = JsonUtility.FromJson<BuildProgressData>(dataJson);
                    if (bpr != null) HandleBuildProgress(bpr);
                    break;
                case "build_completed":
                    var bcp = JsonUtility.FromJson<BuildCompletedData>(dataJson);
                    if (bcp != null) HandleBuildCompleted(bcp);
                    break;
                case "build_demolished":
                    var bdm = JsonUtility.FromJson<BuildDemolishedData>(dataJson);
                    if (bdm != null) HandleBuildDemolished(bdm);
                    break;
                case "build_propose_failed":
                    var bpf = JsonUtility.FromJson<BuildProposeFailedData>(dataJson);
                    if (bpf != null) HandleBuildProposeFailed(bpf);
                    break;
                case "build_cancelled":
                    var bcl = JsonUtility.FromJson<BuildCancelledData>(dataJson);
                    if (bcl != null) HandleBuildCancelled(bcl);
                    break;
                case "building_demolished_batch":
                    var bdb = JsonUtility.FromJson<BuildingDemolishedBatchData>(dataJson);
                    if (bdb != null) HandleBuildingDemolishedBatch(bdb);
                    break;
                case "monster_wave_incoming":
                    var mwi = JsonUtility.FromJson<MonsterWaveIncomingData>(dataJson);
                    if (mwi != null) HandleMonsterWaveIncoming(mwi);
                    break;

                // ----- §39 商店系统 -----
                case "shop_list_data":
                    var sld = JsonUtility.FromJson<ShopListData>(dataJson);
                    if (sld != null) HandleShopListData(sld);
                    break;
                case "shop_purchase_confirm_prompt":
                    var spp = JsonUtility.FromJson<ShopPurchaseConfirmPromptData>(dataJson);
                    if (spp != null) HandleShopPurchaseConfirmPrompt(spp);
                    break;
                case "shop_purchase_confirm":
                    var spc = JsonUtility.FromJson<ShopPurchaseConfirmData>(dataJson);
                    if (spc != null) HandleShopPurchaseConfirm(spc);
                    break;
                case "shop_purchase_failed":
                    var spf = JsonUtility.FromJson<ShopPurchaseFailedData>(dataJson);
                    if (spf != null) HandleShopPurchaseFailed(spf);
                    break;
                case "shop_equip_changed":
                    var sec = JsonUtility.FromJson<ShopEquipChangedData>(dataJson);
                    if (sec != null) HandleShopEquipChanged(sec);
                    break;
                case "shop_equip_failed":
                    var sef = JsonUtility.FromJson<ShopEquipFailedData>(dataJson);
                    if (sef != null) HandleShopEquipFailed(sef);
                    break;
                case "shop_inventory_data":
                    var sid = JsonUtility.FromJson<ShopInventoryData>(dataJson);
                    if (sid != null) HandleShopInventoryData(sid);
                    break;
                case "shop_effect_triggered":
                    var set_ = JsonUtility.FromJson<ShopEffectTriggeredData>(dataJson);
                    if (set_ != null) HandleShopEffectTriggered(set_);
                    break;

                // ----- §35 跨直播间攻防战 -----
                case "tribe_war_room_list_result":
                    var twRL = JsonUtility.FromJson<TribeWarRoomListResultData>(dataJson);
                    if (twRL != null) HandleTribeWarRoomListResult(twRL);
                    break;
                case "tribe_war_attack_failed":
                    var twAF = JsonUtility.FromJson<TribeWarAttackFailedData>(dataJson);
                    if (twAF != null) HandleTribeWarAttackFailed(twAF);
                    break;
                case "tribe_war_attack_started":
                    var twAS = JsonUtility.FromJson<TribeWarAttackStartedData>(dataJson);
                    if (twAS != null) HandleTribeWarAttackStarted(twAS);
                    break;
                case "tribe_war_under_attack":
                    var twUA = JsonUtility.FromJson<TribeWarUnderAttackData>(dataJson);
                    if (twUA != null) HandleTribeWarUnderAttack(twUA);
                    break;
                case "tribe_war_expedition_sent":
                    var twES = JsonUtility.FromJson<TribeWarExpeditionSentData>(dataJson);
                    if (twES != null) HandleTribeWarExpeditionSent(twES);
                    break;
                case "tribe_war_expedition_incoming":
                    var twEI = JsonUtility.FromJson<TribeWarExpeditionIncomingData>(dataJson);
                    if (twEI != null) HandleTribeWarExpeditionIncoming(twEI);
                    break;
                case "tribe_war_combat_report":
                    var twCR = JsonUtility.FromJson<TribeWarCombatReportData>(dataJson);
                    if (twCR != null) HandleTribeWarCombatReport(twCR);
                    break;
                case "tribe_war_combat_report_defense":
                    var twCRD = JsonUtility.FromJson<TribeWarCombatReportData>(dataJson);
                    if (twCRD != null) HandleTribeWarCombatReportDefense(twCRD);
                    break;
                case "tribe_war_attack_ended":
                    var twAE = JsonUtility.FromJson<TribeWarAttackEndedData>(dataJson);
                    if (twAE != null) HandleTribeWarAttackEnded(twAE);
                    break;

                // ----- §36 全服同步 + 赛季制 -----
                case "world_clock_tick":
                    var wct = JsonUtility.FromJson<WorldClockTickData>(dataJson);
                    if (wct != null) HandleWorldClockTick(wct);
                    break;
                case "season_state":
                    var ss = JsonUtility.FromJson<SeasonStateData>(dataJson);
                    if (ss != null) HandleSeasonState(ss);
                    break;
                case "fortress_day_changed":
                    var fdc = JsonUtility.FromJson<FortressDayChangedData>(dataJson);
                    if (fdc != null) HandleFortressDayChanged(fdc);
                    break;
                case "room_failed":
                    var rf = JsonUtility.FromJson<RoomFailedData>(dataJson);
                    if (rf != null) HandleRoomFailed(rf);
                    break;
                case "season_started":
                    var sst = JsonUtility.FromJson<SeasonStartedData>(dataJson);
                    if (sst != null) HandleSeasonStarted(sst);
                    break;
                case "season_settlement":
                    var sstl = JsonUtility.FromJson<SeasonSettlementData>(dataJson);
                    if (sstl != null) HandleSeasonSettlement(sstl);
                    break;

                // ----- §36.4 赛季 Boss Rush -----
                case "season_boss_rush_start":
                    var brs = JsonUtility.FromJson<BossRushStartedData>(dataJson);
                    if (brs != null) HandleBossRushStarted(brs);
                    break;
                case "season_boss_rush_killed":
                    var brk = JsonUtility.FromJson<BossRushKilledData>(dataJson);
                    if (brk != null) HandleBossRushKilled(brk);
                    break;

                // ----- §36.12 老用户豁免 / 主播动作失败 -----
                case "veteran_unlocked":
                    var vu = JsonUtility.FromJson<VeteranUnlockedData>(dataJson);
                    if (vu != null) HandleVeteranUnlocked(vu);
                    break;
                case "broadcaster_action_failed":
                    var baf = JsonUtility.FromJson<BroadcasterActionFailedData>(dataJson);
                    if (baf != null) HandleBroadcasterActionFailed(baf);
                    break;
                case "roulette_spin_failed":
                    // §36.12 feature_locked 场景（roulette.minDay=1 实际不触发；此处为防御性兜底，避免静默吞消息）
                    HandleRouletteSpinFailed(dataJson);
                    break;

                // ----- §34 Layer 3 组 C 体验引擎（🆕 v1.27） -----
                case "glory_moment":
                    var glory = JsonUtility.FromJson<GloryMomentData>(dataJson);
                    if (glory != null) OnGloryMoment?.Invoke(glory);
                    break;
                case "coop_milestone":
                    var milestone = JsonUtility.FromJson<CoopMilestoneData>(dataJson);
                    if (milestone != null) OnCoopMilestone?.Invoke(milestone);
                    break;
                case "gift_impact":
                    var impact = JsonUtility.FromJson<GiftImpactData>(dataJson);
                    if (impact != null) OnGiftImpact?.Invoke(impact);
                    break;

                // ----- §34 Layer 3 组 D 叙事引擎（🆕 v1.27） -----
                // phase_changed 扩展 act_tag / nightModifier 字段走既有 case "phase_changed"；
                // 订阅方通过 OnPhaseChanged 拿到 PhaseChangedData 读取扩展字段，无需新 case。
                case "chapter_changed":
                    var chapter = JsonUtility.FromJson<ChapterChangedData>(dataJson);
                    if (chapter != null) OnChapterChanged?.Invoke(chapter);
                    break;
                case "streamer_prompt":
                    var prompt = JsonUtility.FromJson<StreamerPromptData>(dataJson);
                    if (prompt != null) OnStreamerPrompt?.Invoke(prompt);
                    break;
                case "night_report":
                    var report = JsonUtility.FromJson<NightReportData>(dataJson);
                    if (report != null) OnNightReport?.Invoke(report);
                    break;
                case "engagement_reminder":
                    var reminder = JsonUtility.FromJson<EngagementReminderData>(dataJson);
                    if (reminder != null) OnEngagementReminder?.Invoke(reminder);
                    break;

                // ----- §34 Layer 2 组 B 数据流可视化（🆕 v1.27） -----
                // random_event 沿用既有 case "random_event"；B3 仅扩展 eventId 枚举，前端 fallback 兜底。
                case "settlement_highlights":
                    var high = JsonUtility.FromJson<SettlementHighlightsData>(dataJson);
                    if (high != null)
                    {
                        _lastSettlementHighlights = high;  // 缓存供 HandleGameEnded 读取
                        OnSettlementHighlights?.Invoke(high);
                    }
                    break;
                case "efficiency_race":
                    var race = JsonUtility.FromJson<EfficiencyRaceData>(dataJson);
                    if (race != null) OnEfficiencyRace?.Invoke(race);
                    break;
                case "day_preview":
                    var dp = JsonUtility.FromJson<DayPreviewData>(dataJson);
                    if (dp != null) OnDayPreview?.Invoke(dp);
                    break;

                // ----- §34 Layer 2 组 A 新手友好（🆕 v1.27） -----
                // B9 work_command_response：协议预留 type（当前服务端实际把 playerStats 捎带在 work_command 广播上，
                //   由 HandleWorkCommand 统一分发 OnPlayerStatsUpdated；此 case 作为未来拆分独立 response type 的兜底）。
                //   若老服务端不下发本消息，前端保持静默（PersonalContribUI 不显示）。
                case "work_command_response":
                    var wcr = JsonUtility.FromJson<WorkCommandResponseData>(dataJson);
                    if (wcr != null && wcr.playerStats != null)
                    {
                        LastPlayerStats = wcr.playerStats;
                        OnPlayerStatsUpdated?.Invoke(wcr.playerStats);
                    }
                    break;

                // B8 fairy_wand_maxed：服务端 fairy_wand 累计跨过 +100% 时 unicast；
                //   前端全屏金闪 + 跑马灯 "满级矿工达成！{playerName}"。
                case "fairy_wand_maxed":
                    var fwm = JsonUtility.FromJson<FairyWandMaxedData>(dataJson);
                    if (fwm != null) OnFairyWandMaxed?.Invoke(fwm);
                    break;

                // §34 Batch D：单人贡献增量（Batch I 补齐透传）
                // 🔴 audit-r25 GAP-D25-03：r24 加 source 字段但 0 UI 订阅者 → 半成品延续第 5 轮
                //   仅在 T6 礼物路径 emit（'gift_t6_max_level_bonus' / 'gift_t6_upgrade_bonus' +100 贡献奖励）
                //   修复：在此处加跑马灯反馈，让玩家感知 T6 升级 / 满级奖励差异化路径
                case "contribution_update":
                    var cu = JsonUtility.FromJson<ContributionUpdateData>(dataJson);
                    if (cu != null)
                    {
                        OnContributionUpdate?.Invoke(cu);
                        // 🔴 audit-r25 GAP-D25-03：T6 路径专属跑马灯
                        if (!string.IsNullOrEmpty(cu.source) && cu.delta > 0)
                        {
                            string flavorMsg = cu.source switch
                            {
                                "gift_t6_max_level_bonus" => $"+{cu.delta} 贡献：T6 城门已满级奖励！",
                                "gift_t6_upgrade_bonus"   => $"+{cu.delta} 贡献：T6 触发城门升级奖励！",
                                _                          => null,
                            };
                            if (flavorMsg != null)
                            {
                                UI.HorizontalMarqueeUI.Instance?.AddMessage(
                                    string.IsNullOrEmpty(cu.playerName) ? "贡献" : cu.playerName,
                                    null, flavorMsg);
                            }
                        }
                    }
                    break;

                // ----- 协议骨架补齐 Batch A（🆕 v1.27+） -----
                // 以下 case 负责消息→事件透传 + 最小用户可见反馈（Debug.Log + 弹幕栏提示），
                // 具体业务（面板刷新 / 徽标等）由 UI 订阅者按自身逻辑实现。
                case "room_state":
                    var rst = JsonUtility.FromJson<RoomStateData>(dataJson);
                    if (rst != null)
                    {
                        Debug.Log($"[SGM] room_state 收到断线重连快照 (dailyGained={rst.dailyFortressDayGained}/{rst.dailyCapMax})");
                        OnRoomState?.Invoke(rst);
                    }
                    break;
                case "feature_unlocked":
                    var fu = JsonUtility.FromJson<FeatureUnlockedData>(dataJson);
                    if (fu != null)
                    {
                        Debug.Log($"[SGM] feature_unlocked: {fu.featureId} @ seasonDay={fu.unlockedAt}");
                        string msg = string.IsNullOrEmpty(fu.message) ? $"新功能解锁：{fu.featureId}" : fu.message;
                        OnPlayerActivityMessage?.Invoke(msg);
                        OnFeatureUnlocked?.Invoke(fu);
                    }
                    break;
                case "broadcaster_roulette_effect_prevented":
                    var rep = JsonUtility.FromJson<RouletteEffectPreventedData>(dataJson);
                    if (rep != null)
                    {
                        Debug.Log($"[SGM] roulette_effect_prevented: {rep.cardId} reason={rep.preventReason}");
                        string zh = rep.preventReason switch
                        {
                            "duplicate"               => "同类效果已生效",
                            "conflict_with_other_buff" => "与其他 Buff 冲突",
                            "game_not_running"        => "当前不在游戏中",
                            _                         => "效果被阻止"
                        };
                        OnPlayerActivityMessage?.Invoke($"【轮盘】{rep.cardId} · {zh}");
                        OnRouletteEffectPrevented?.Invoke(rep);
                    }
                    break;
                case "tribe_war_retaliate":
                    var twR = JsonUtility.FromJson<TribeWarRetaliateData>(dataJson);
                    if (twR != null)
                    {
                        Debug.Log($"[SGM] tribe_war_retaliate target={twR.targetRoomId} dmgMul={twR.damageMultiplier}");
                        OnTribeWarRetaliate?.Invoke(twR);
                    }
                    break;

                // ----- audit-r5 客户端补齐（🆕 v1.27+） -----
                // §19/§34.4 E9 难度生效：E9 UI / 跑马灯订阅
                case SurvivalMessageProtocol.DifficultyChanged:
                    var dc = JsonUtility.FromJson<DifficultyChangedData>(dataJson);
                    if (dc != null)
                    {
                        Debug.Log($"[SGM] difficulty_changed: {dc.difficulty}→{dc.appliedDifficulty} applyAt={dc.applyAt}");
                        OnDifficultyChanged?.Invoke(dc);
                    }
                    break;

                // §30.3 阶8 护盾触发：WorkerController 订阅播放 5s 蓝 tint + 无敌气泡
                case SurvivalMessageProtocol.WorkerShieldActivated:
                    var wsa = JsonUtility.FromJson<WorkerShieldActivatedData>(dataJson);
                    if (wsa != null)
                    {
                        Debug.Log($"[SGM] worker_shield_activated playerId={wsa.playerId} dur={wsa.durationMs}ms");
                        if (WorkerManager.Instance != null)
                            WorkerManager.Instance.HandleWorkerShieldActivated(wsa.playerId, wsa.durationMs);
                        OnWorkerShieldActivated?.Invoke(wsa);
                    }
                    break;

                // §34 B8 仙女棒累计光点：FairyWandAccumUI / Stardust 订阅，capped=true 时切金爆裂
                case SurvivalMessageProtocol.FairyWandApplied:
                    var fwa = JsonUtility.FromJson<FairyWandAppliedData>(dataJson);
                    if (fwa != null)
                    {
                        OnFairyWandApplied?.Invoke(fwa);
                    }
                    break;

                // ----- audit-r6 客户端补齐（🆕 v1.27+） -----
                // §34.4 E9 主播切换难度失败：r13 GAP-A2 — 对齐服务端实际 reason
                //   服务端 SurvivalGameEngine.handleChangeDifficulty 实发：not_broadcaster / invalid_difficulty / invalid_args
                //   旧 reason（wrong_phase/unknown_difficulty/season_frozen）保留作向后兼容
                case SurvivalMessageProtocol.ChangeDifficultyFailed:
                    var cdf = JsonUtility.FromJson<ChangeDifficultyFailedData>(dataJson);
                    if (cdf != null)
                    {
                        Debug.LogWarning($"[SGM] change_difficulty_failed reason={cdf.reason}");
                        OnChangeDifficultyFailed?.Invoke(cdf);
                        var reasonText = cdf.reason switch
                        {
                            "not_broadcaster"     => "仅主播可切换难度",
                            "invalid_difficulty"  => "难度无效（仅支持 easy / normal / hard）",
                            "invalid_args"        => "参数错误（applyAt 仅支持 next_night / next_season）",
                            // 向后兼容旧服务端版本
                            "wrong_phase"         => "当前不可切换（非 day/night）",
                            "unknown_difficulty"  => "未知难度",
                            "season_frozen"       => $"赛季末冻结（第 {cdf.unlockDay} 天解锁）",
                            _                      => "切换失败"
                        };
                        UI.HorizontalMarqueeUI.Instance?.AddMessage("系统", null, $"<color=#FF9999>切换难度失败：{reasonText}</color>");
                    }
                    break;

                case SurvivalMessageProtocol.ChangeDifficultyAccepted:
                    var cda = JsonUtility.FromJson<ChangeDifficultyAcceptedData>(dataJson);
                    if (cda != null)
                    {
                        Debug.Log($"[SGM] change_difficulty_accepted {cda.difficulty} applyAt={cda.applyAt}");
                        OnChangeDifficultyAccepted?.Invoke(cda);
                        UI.HorizontalMarqueeUI.Instance?.AddMessage("系统", null, $"<color=#9EC5FF>难度切换已排队：{cda.difficulty}</color>");
                    }
                    break;

                // §30.4 每日不活跃等级衰减：BarrageMessageUI / 名牌刷新
                case SurvivalMessageProtocol.DailyTierDecay:
                    var dtd = JsonUtility.FromJson<DailyTierDecayData>(dataJson);
                    if (dtd != null)
                    {
                        Debug.Log($"[SGM] daily_tier_decay playerId={dtd.playerId} {dtd.oldLevel}→{dtd.newLevel}");
                        OnDailyTierDecay?.Invoke(dtd);
                        if (WorkerManager.Instance != null)
                            WorkerManager.Instance.HandleDailyTierDecay(dtd.playerId, dtd.oldLevel, dtd.newLevel);
                    }
                    break;

                // §30.7 T4/T5/T6 限时皮肤激活
                case SurvivalMessageProtocol.GiftSkinApplied:
                    var gsa = JsonUtility.FromJson<GiftSkinAppliedData>(dataJson);
                    if (gsa != null)
                    {
                        Debug.Log($"[SGM] gift_skin_applied playerId={gsa.playerId} skinId={gsa.skinId} expireAt={gsa.expireAt}");
                        OnGiftSkinApplied?.Invoke(gsa);
                        if (WorkerManager.Instance != null)
                            WorkerManager.Instance.HandleGiftSkinApplied(gsa.playerId, gsa.skinId, gsa.expireAt);
                    }
                    break;

                // §30.7 限时皮肤到期
                case SurvivalMessageProtocol.GiftSkinExpired:
                    var gse = JsonUtility.FromJson<GiftSkinExpiredData>(dataJson);
                    if (gse != null)
                    {
                        Debug.Log($"[SGM] gift_skin_expired playerId={gse.playerId}");
                        OnGiftSkinExpired?.Invoke(gse);
                        if (WorkerManager.Instance != null)
                            WorkerManager.Instance.HandleGiftSkinExpired(gse.playerId);
                    }
                    break;

                // audit-r8 §34 F4 Boss 露出弱点
                case SurvivalMessageProtocol.BossWeaknessStarted:
                    var bws = JsonUtility.FromJson<BossWeaknessStartedData>(dataJson);
                    if (bws != null)
                    {
                        Debug.Log($"[SGM] boss_weakness_started bossId={bws.bossId} durMs={bws.durationMs} dmgMult={bws.damageMult} t5Mult={bws.t5Mult}");
                        OnBossWeaknessStarted?.Invoke(bws);
                    }
                    break;

                // audit-r8 §34 F4 Boss 弱点结束
                case SurvivalMessageProtocol.BossWeaknessEnded:
                    var bwe = JsonUtility.FromJson<BossWeaknessEndedData>(dataJson);
                    if (bwe != null)
                    {
                        Debug.Log($"[SGM] boss_weakness_ended bossId={bwe.bossId}");
                        OnBossWeaknessEnded?.Invoke(bwe);
                    }
                    break;

                // audit-r8 §34 F8 无效指令提示（单播）
                case SurvivalMessageProtocol.InvalidCommandHint:
                    var ich = JsonUtility.FromJson<InvalidCommandHintData>(dataJson);
                    if (ich != null)
                    {
                        Debug.Log($"[SGM] invalid_command_hint type={ich.type} msg={ich.msg} ttl={ich.ttl}");
                        OnInvalidCommandHint?.Invoke(ich);
                    }
                    break;

                // ----- audit-r20 客户端补齐（🆕） -----

                // §34.3 D 段幕末事件：5 种 actTag×event 组合的全屏公告，UI 层订阅显示
                case SurvivalMessageProtocol.ChapterEndEvent:
                    var cee = JsonUtility.FromJson<ChapterEndEventData>(dataJson);
                    if (cee != null)
                    {
                        Debug.Log($"[SGM] chapter_end_event seasonDay={cee.seasonDay} actTag={cee.actTag} event={cee.@event} hint={cee.hint}");
                        OnChapterEndEvent?.Invoke(cee);
                        if (!string.IsNullOrEmpty(cee.hint))
                            OnPlayerActivityMessage?.Invoke(cee.hint);
                    }
                    break;

                // §34.3 E3b 不朽证明：累计贡献 20000 触发免死豁免，矿工原地满血复活
                case SurvivalMessageProtocol.FreeDeathPassTriggered:
                    var fdp = JsonUtility.FromJson<FreeDeathPassTriggeredData>(dataJson);
                    if (fdp != null)
                    {
                        Debug.Log($"[SGM] free_death_pass_triggered playerId={fdp.playerId} playerName={fdp.playerName}");
                        OnFreeDeathPassTriggered?.Invoke(fdp);
                        var nm = string.IsNullOrEmpty(fdp.playerName) ? fdp.playerId : fdp.playerName;
                        OnPlayerActivityMessage?.Invoke($"<color=#FFD700>★ {nm} 触发不朽证明，矿工免死复活！</color>");
                    }
                    break;

                // §15 / §19.2 房间销毁：服务端 emit 后立即 ws.close(1000)；客户端 UI 弹窗 + 兜底回大厅
                case SurvivalMessageProtocol.RoomDestroyed:
                    var rd = JsonUtility.FromJson<RoomDestroyedData>(dataJson);
                    if (rd != null)
                    {
                        Debug.Log($"[SGM] room_destroyed roomId={rd.roomId} reason={rd.reason}");
                        OnRoomDestroyed?.Invoke(rd);
                        var rzh = rd.reason switch
                        {
                            "timeout" => "房间空闲超时",
                            _          => string.IsNullOrEmpty(rd.reason) ? "未知原因" : rd.reason,
                        };
                        OnPlayerActivityMessage?.Invoke($"<color=#FF9999>房间已销毁（{rzh}）</color>");
                    }
                    break;
            }
        }

        // ==================== 逻辑处理 ====================

        private void HandleJoinRoomConfirm(JoinRoomConfirmData data)
        {
            // 服务器确认连接成功：进入 Waiting，等待主播点击"开始挑战"
            if (_state == SurvivalState.Connecting || _state == SurvivalState.Idle)
            {
                ChangeState(SurvivalState.Waiting);
                Debug.Log($"[SGM] join_room_confirm → Waiting (isRoomCreator={data?.isRoomCreator}, has_active_session={data?.has_active_session})");

                // 若服务器检测到上局进行中，弹出断线重连对话框
                if (data != null && data.has_active_session)
                {
                    if (UI.ReconnectDialog.Instance != null)
                    {
                        UI.ReconnectDialog.Instance.Show();
                        Debug.Log("[SGM] has_active_session=true → 显示断线重连对话框");
                    }
                    else
                    {
                        // 对话框未在场景中创建：自动重置服务器，让主播重新开始
                        Debug.LogWarning("[SGM] has_active_session=true 但 ReconnectDialog 未找到，自动发送 reset_game 清除旧会话");
                        NetworkManager.Instance?.SendMessage("reset_game");
                    }
                }
            }
        }

        private void HandleGameState(SurvivalGameStateData data)
        {
            // 同步资源
            if (resourceSystem != null)
            {
                resourceSystem.ApplyServerUpdate(new ResourceUpdateData
                {
                    food = data.food, coal = data.coal, ore = data.ore,
                    furnaceTemp = data.furnaceTemp,
                    gateHp = data.gateHp, gateMaxHp = data.gateMaxHp,
                    remainingTime = data.remainingTime
                });
            }
            // 🆕 v1.22 §10 同步城门层级元数据（断线重连时确保状态一致）
            if (cityGateSystem != null)
            {
                cityGateSystem.ApplyDailyUpgraded(data.gateDailyUpgraded);
                cityGateSystem.ApplyTierMeta(data.gateTierName, data.gateFeatures);
            }
            // 同步积分池（连接/断线重连时立即刷新显示）
            if (data.scorePool > 0) OnScorePoolUpdated?.Invoke(data.scorePool);

            // 🆕 v1.27 §36.12 分时段解锁：survival_game_state 每次推送都携带已解锁功能全集
            //   断线重连兜底路径，确保客户端按钮锁态与服务端完全一致
            if (data.unlockedFeatures != null)
                SyncUnlockedFeatures(data.unlockedFeatures);

            switch (data.state)
            {
                case "idle":
                    if (_state == SurvivalState.Loading && !IsEnteringScene)
                    {
                        // 退出流程：服务器已重置 → 本地重置 → 回大厅
                        StopLoadingTimeout();
                        ResetAllSystems();
                        Debug.Log("[SGM] State→Idle，退出 Loading 完成（服务器已确认 reset）");
                    }
                    else if (_state != SurvivalState.Loading)
                    {
                        // 直接切 Idle（如断线重连后服务器推空闲状态）
                        ChangeState(SurvivalState.Idle);
                    }
                    break;

                case "day":
                case "night":
                    // 🆕 §16 永续模式：Settlement 态收到 state=day+variant=recovery（后端规范化后仍是 day）
                    //   → 断线重连兜底路径，允许从 Settlement 切回 Running
                    //   后端 getFullState 在 recovery 期对外推送 state='day' + variant='recovery'（非 state='recovery'）
                    string dataVariant = string.IsNullOrEmpty(data.variant) ? "normal" : data.variant;
                    if (_state == SurvivalState.Settlement && dataVariant == "recovery")
                    {
                        Debug.Log("[SGM] Settlement 态收到 survival_game_state{state=day, variant=recovery} → 切回 Running（§16 断线重连兜底）");
                        ChangeState(SurvivalState.Running);
                        if (_settlementUI != null && _settlementUI.gameObject.activeSelf)
                            _settlementUI.gameObject.SetActive(false);
                    }
                    else if ((_state == SurvivalState.Loading || _state == SurvivalState.Waiting) && IsEnteringScene)
                    {
                        // 进入流程：服务器已启动 → 停止超时 → 切 Running
                        StopLoadingTimeout();
                        ChangeState(SurvivalState.Running);
                        Debug.Log("[SGM] State→Running，进入 Loading/Waiting 完成（服务器已确认 start）");
                    }
                    else if (_state != SurvivalState.Running && IsEnteringScene)
                    {
                        // 仅在用户主动发起 start_game（IsEnteringScene=true）时才切换 Running
                        // 防止冷连接时服务器推送旧会话状态导致自动进入战斗场景
                        ChangeState(SurvivalState.Running);
                        Debug.Log("[SGM] State→Running（IsEnteringScene=true，服务器确认 day/night）");
                    }
                    else if (_state == SurvivalState.Waiting || _state == SurvivalState.Idle)
                    {
                        // 冷连接：服务器有活跃会话但用户未主动开始
                        // 等待断线重连对话框或主播手动选择（has_active_session 已由 join_room_confirm 处理）
                        Debug.Log($"[SGM] 收到 survival_game_state({data.state})，当前 IsEnteringScene=false，" +
                                  "跳过自动进入 Running（请在断线重连对话框选择[重连]）");
                    }
                    // 同步昼夜（仅在 Running 状态才同步，避免影响 Waiting 场景）
                    if (_state == SurvivalState.Running)
                    {
                        // 🆕 §16 / §4.2：从 survival_game_state 同步 variant，供 HUD 差异化 UI 识别恢复期
                        //   dataVariant 在上方已从 data.variant 规范化，此处复用
                        bool svVariantChanged = dataVariant != _lastPhaseVariant;
                        _lastPhaseVariant = dataVariant;
                        if (svVariantChanged)
                            OnPhaseVariantChanged?.Invoke(dataVariant);
                        dayNightManager?.HandlePhaseChanged(new PhaseChangedData
                        {
                            phase = data.state,
                            day = data.day,
                            phaseDuration = data.remainingTime,
                            variant = dataVariant
                        });
                    }
                    break;

                case "settlement":
                    // 断线重连时服务器处于结算状态 → 直接切换到 Settlement UI（原因未知，用 unknown）
                    // 不调用 HandleDefeatOrVictory（那会创建假的"胜利"结算数据）
                    // 10s 后服务器会自动重启，客户端会收到新的 survival_game_state(day)
                    if (_state == SurvivalState.Running || _state == SurvivalState.Loading)
                    {
                        Debug.Log("[SGM] survival_game_state=settlement，服务器正在结算，等待自动重开");
                        ChangeState(SurvivalState.Settlement);
                    }
                    break;
            }

            // 🔴 audit-r25 GAP-A25-01：r24 GAP-A24-02 加 7 字段（fortressDay/maxFortressDay/seasonId/seasonDay/themeId/phase/phaseRemainingSec）
            //   但 HandleGameState 完全没分发 → 断线重连后 UI 仍要等 world_clock_tick / season_state / fortress_day_changed 才能更新
            //   r25 修复：在末尾同步分发到 SeasonTopBarUI / FortressDayBadgeUI（重连兜底）
            //   NOTE: FortressDayChangedData / SeasonStateData 仅复用部分字段（其余字段为 0/null 占位是合理的）；
            //         订阅者据 reason='reconnect_sync' 可识别"重连兜底快照"分支。
            if (data.fortressDay > 0)
                OnFortressDayChanged?.Invoke(new FortressDayChangedData
                {
                    oldFortressDay = data.fortressDay,
                    newFortressDay = data.fortressDay,  // 同步快照（非变更事件）
                    reason         = "reconnect_sync",
                    seasonDay      = data.seasonDay,
                });
            if (!string.IsNullOrEmpty(data.themeId))
                OnSeasonState?.Invoke(new SeasonStateData
                {
                    seasonId  = data.seasonId,
                    seasonDay = data.seasonDay,
                    themeId   = data.themeId,
                    // unlockedFeatures: 同步快照不重传（已由 data.unlockedFeatures 走 SyncUnlockedFeatures 路径处理）
                });
        }

        private void HandleWorkCommand(WorkCommandData data)
        {
            OnWorkCommand?.Invoke(data);
            workerManager?.AssignWork(data);

            // 🆕 §34 Layer 2 组 A B9：服务端在 work_command 附带 playerStats 快照（可为 null）
            //   有快照 → 缓存 + 分发 OnPlayerStatsUpdated，PersonalContribUI / FairyWandAccumUI 消费。
            if (data?.playerStats != null)
            {
                LastPlayerStats = data.playerStats;
                OnPlayerStatsUpdated?.Invoke(data.playerStats);
            }

            // 上报资源分类贡献到排行系统（commandId 1=食物 2=煤炭 3=矿石）
            string resType = data.commandId switch
            {
                1 => "food",
                2 => "coal",
                3 => "ore",
                _ => null
            };
            if (resType != null && !string.IsNullOrEmpty(data.playerId))
                RankingSystem.Instance?.AddResourceContrib(data.playerId, data.playerName, resType, 1);

            string action = data.commandId switch
            {
                1 => "采集食物",
                2 => "挖掘煤炭",
                3 => "开采矿石",
                4 => "添柴升温",
                // case 5 修复城门已从策划案 v2.0 移除
                6 => "发动攻击",
                _ => "工作"
            };
            OnPlayerActivityMessage?.Invoke($"{data.playerName} → {action}");
            UI.HorizontalMarqueeUI.Instance?.AddMessage(data.playerName, data.avatarUrl, action);
        }

        // ==================== 新增：broadcaster_effect / special_effect / random_event / gift_pause ====================

        private void HandleBroadcasterEffect(string dataJson)
        {
            var data = JsonUtility.FromJson<BroadcasterEffectData>(dataJson);
            if (data == null) return;

            if (data.action == "efficiency_boost")
            {
                float dur = data.duration > 0 ? data.duration / 1000f : 30f;
                workerManager?.ActivateAllWorkersGlow(dur);
                ShowAnnouncementBanner("【加速】主播加速！全体效率翻倍！", dur);
                // audit-r10 §29：主播 ⚡加速 SFX
                Systems.AudioManager.Instance?.PlaySFX(Core.AudioConstants.SFX_BROADCASTER_BOOST);
                Debug.Log($"[SGM] broadcaster_effect: efficiency_boost ({dur}s)");
            }
            else if (data.action == "trigger_event")
            {
                HandleBroadcasterEvent(data.eventId);
                Debug.Log($"[SGM] broadcaster_effect: trigger_event ({data.eventId})");
            }
        }

        private void HandleSpecialEffect(string dataJson)
        {
            var data = JsonUtility.FromJson<SpecialEffectData>(dataJson);
            if (data == null) return;

            if (data.effect == "glow_all")
            {
                float dur = data.duration > 0 ? data.duration : 3f;
                workerManager?.ActivateAllWorkersGlow(dur);
                ShowAnnouncementBanner("【666】全员加速！", 2f);
                Debug.Log("[SGM] special_effect: glow_all");
            }
            else if (data.effect == "frozen_all")
            {
                float dur = data.duration > 0 ? data.duration : 30f;
                workerManager?.ActivateAllWorkersFrozen(dur);
                ShowAnnouncementBanner($"【冰冻】全体守护者冻结 {dur:F0}秒！", 3f);
                UI.FrozenStatusUI.ShowFrozen(dur);
                Debug.Log("[SGM] special_effect: frozen_all");
            }
        }

        private void HandleRandomEvent(string dataJson)
        {
            var data = JsonUtility.FromJson<RandomEventData>(dataJson);
            if (data == null) return;
            ShowFullScreenEvent(data.eventId, data.name);
            Debug.Log($"[SGM] random_event: {data.eventId} ({data.name})");

            // 🆕 Fix C (组 B Reviewer P0) §34B B3：morale_boost 矿工头顶气泡
            if (data.eventId == "morale_boost" && !string.IsNullOrEmpty(data.targetPlayerId) && !string.IsNullOrEmpty(data.bubbleText))
            {
                float sec = data.durationMs > 0 ? data.durationMs / 1000f : 3.0f;
                WorkerManager.Instance?.ShowBubbleOnWorker(data.targetPlayerId, data.bubbleText, sec);
            }

            // 🆕 Fix C §34B B3：广播 OnRandomEvent，订阅者（气泡 UI / 效果 UI 等）按 eventId 分发
            OnRandomEvent?.Invoke(data);
        }

        private void HandleGiftPause(string dataJson)
        {
            var data = JsonUtility.FromJson<GiftPauseData>(dataJson);
            float pauseSec = (data != null && data.duration > 0) ? data.duration / 1000f : 3f;
            StartCoroutine(PauseGameTick(pauseSec));
        }

        private IEnumerator PauseGameTick(float seconds)
        {
            _isPaused = true;
            workerManager?.PauseAllWorkers();
            Debug.Log($"[SGM] gift_pause: {seconds}s");
            yield return new WaitForSeconds(seconds);
            _isPaused = false;
            workerManager?.ResumeAllWorkers();
        }

        private void HandleBroadcasterEvent(string eventId)
        {
            string eventName = eventId switch
            {
                "snowstorm"    => "暴风雪来袭！",
                "harvest"      => "丰收季节！",
                "monster_wave" => "怪物来袭！",
                _              => "随机事件"
            };
            ShowFullScreenEvent(eventId, eventName);
        }

        /// <summary>显示短暂横幅公告（2行文字，持续 duration 秒后消失）</summary>
        private void ShowAnnouncementBanner(string text, float duration)
        {
            UI.AnnouncementUI.Instance?.ShowAnnouncement(text, "", new Color(1f, 0.85f, 0.1f), duration);
        }

        /// <summary>显示全屏随机事件公告（3秒后消失）。
        /// 🆕 §34 B3 扩展：10 个新 eventId（airdrop_supply / ice_ground / aurora_flash / earthquake /
        ///   meteor_shower / heavy_fog / hot_spring / food_spoil / inspiration / morale_boost）。
        /// 未知 eventId fallback 使用服务端下发的 eventName + "特殊事件发生！"，不崩溃。</summary>
        private void ShowFullScreenEvent(string eventId, string eventName)
        {
            string subText = eventId switch
            {
                "E01_snowstorm"    => "炉温衰减加速×2，持续60秒！",
                "E02_harvest"      => "食物采集效率×1.5，持续30秒！",
                "E03_monster_wave" => "额外2只怪物出现！",
                "E04_warm_spring"  => "炉温立即+20℃！",
                "E05_ore_vein"     => "矿石采集效率×2，持续45秒！",
                "snowstorm"        => "炉温衰减加速！",
                "harvest"          => "食物采集效率提升！",
                "monster_wave"     => "大量怪物涌入！",
                // 🆕 §34 B3 新增 10 事件
                "airdrop_supply"   => "随机资源大礼包降落！",
                "ice_ground"       => "移动速度 -20%，持续 30 秒！",
                "aurora_flash"     => "全员效率 +5%，持续 5 秒！",
                "earthquake"       => "炉温 -5，城门 -50 HP！",
                "meteor_shower"    => "天降陨石，怪物被消灭 2-3 只！",
                "heavy_fog"        => "浓雾弥漫，怪物血条隐藏 30 秒！",
                "hot_spring"       => "炉温 +2℃ / 5s，持续 30 秒！",
                "food_spoil"       => "食物变质，-15%！",
                "inspiration"      => "灵感爆发，下次工作产出 ×2！",
                "morale_boost"     => "矿工士气高涨！",
                _                  => "特殊事件发生！"
            };
            // 危险事件：红色；有利事件：绿色（按 eventId 集合归类，未知 eventId 默认绿色）
            bool isDanger = eventId is "E01_snowstorm" or "snowstorm"
                            or "E03_monster_wave" or "monster_wave"
                            or "ice_ground" or "earthquake"
                            or "heavy_fog" or "food_spoil";
            Color color = isDanger
                ? new Color(1f, 0.3f, 0.3f)    // 危险事件：红色
                : new Color(0.3f, 1f, 0.5f);    // 有利事件：绿色
            UI.AnnouncementUI.Instance?.ShowAnnouncement(eventName, subText, color, 3f);
        }

        private void HandleGift(SurvivalGiftData gift)
        {
            resourceSystem?.ApplyGiftEffect(gift);
            TrackContribution(gift.playerId, gift.contribution);
            OnGiftReceived?.Invoke(gift);
            OnPlayerActivityMessage?.Invoke($"{gift.playerName} 送出 {gift.giftName}！");
            UI.HorizontalMarqueeUI.Instance?.AddMessage(gift.playerName, gift.avatarUrl, $"打赏了 {gift.giftName}");

            // 礼物视觉特效由 GiftAnimationUI 统一通过 OnGiftReceived 事件处理（WebM视频，无需手动调用）

            // audit-r10 §29：按 tier 播对应 SFX（T6 与 T5 共用 clip，策划案设计债）
            string sfxId = gift.giftTier switch
            {
                1 => Core.AudioConstants.SFX_GIFT_T1,
                2 => Core.AudioConstants.SFX_GIFT_T2,
                3 => Core.AudioConstants.SFX_GIFT_T3,
                4 => Core.AudioConstants.SFX_GIFT_T4,
                5 => Core.AudioConstants.SFX_GIFT_T5,
                6 => Core.AudioConstants.SFX_GIFT_T6,
                _ => null
            };
            if (!string.IsNullOrEmpty(sfxId))
                Systems.AudioManager.Instance?.PlaySFX(sfxId);

            // ⚠️ audit-r24 GAP-C24-01：r23 GAP-C23-02 漏修 — T5/T6 文案错位修复：
            // T5 = love_explosion 爱的爆炸 / T6 = mystery_airdrop 神秘空投，按 tier 分发不同文案
            if (gift.giftTier == 5)
                UI.TopFloatingTextUI.Instance?.ShowGold($"{gift.playerName} 爱的爆炸！");
            else if (gift.giftTier >= 6)
                UI.TopFloatingTextUI.Instance?.ShowGold($"{gift.playerName} 神秘空投！");

            // 触发相机震屏（T3+ 礼物有重量感）
            if (gift.giftTier >= 3)
                SurvivalCameraController.Shake(gift.giftTier * 0.05f, 0.4f);

            // audit-r22 GAP-A22-02：effects 字段消费链路（修复服务端 emit 但客户端 0 消费的协议字段单向消费 gap）
            // audit-r23 GAP-A23-01：补 T4/T6/助威者 effects 消费（10 个补字段：tempBoost/boostDuration/unfrozenWorkers/giftPause/redirectTargetId/redirectTargetName/addFood/addCoal/addOre/addHeat）
            // 服务端 SurvivalGameEngine.js _handleLoveExplosion / _handleMysteryAirdrop / case 'ability_pill'/'energy_battery' / 助威者 _handleSupporter* 在 effects 嵌套对象写入这些字段
            if (gift.effects != null)
            {
                int reviveCount = gift.effects.revivedWorkers != null ? gift.effects.revivedWorkers.Length : 0;
                Debug.Log($"[SGM] gift_effects tier={gift.giftTier} aoe={gift.effects.aoeDamage} killed={gift.effects.monstersKilled} revived={reviveCount} healed={gift.effects.healedWorkers} addGateHp={gift.effects.addGateHp} globalBoost={gift.effects.globalEfficiencyBoost} dur={gift.effects.globalEfficiencyDuration}s tempBoost={gift.effects.tempBoost} boostDur={gift.effects.boostDuration} unfrozen={gift.effects.unfrozenWorkers} giftPause={gift.effects.giftPause} redirectTo={gift.effects.redirectTargetName} resources(F/C/O/H)={gift.effects.addFood}/{gift.effects.addCoal}/{gift.effects.addOre}/{gift.effects.addHeat} supporterRedirect={gift.effects.supporterRedirect}");

                // 助威者重路由提示（T1/T4/T5 路径，redirectTargetName 仅助威者路径写入）
                if (gift.effects.supporterRedirect && !string.IsNullOrEmpty(gift.effects.redirectTargetName))
                {
                    UI.HorizontalMarqueeUI.Instance?.AddMessage(gift.playerName, gift.avatarUrl,
                        $"的助威让 {gift.effects.redirectTargetName} 的矿工更强了！");
                }

                if (gift.giftTier == 5 && (gift.effects.aoeDamage > 0 || gift.effects.monstersKilled > 0))
                {
                    string t5Tail = reviveCount > 0 ? "，复活 1 名矿工" : (gift.effects.healedWorkers > 0 ? "，矿工满血" : string.Empty);
                    UI.HorizontalMarqueeUI.Instance?.AddMessage(gift.playerName, gift.avatarUrl,
                        $"爱的爆炸：全体怪物 -{gift.effects.aoeDamage}HP，击杀 {gift.effects.monstersKilled} 只" + t5Tail);
                }
                else if (gift.giftTier == 6)
                {
                    // T6 神秘空投：资源 + 复活 + 全局暂停
                    if (gift.effects.addFood > 0 || gift.effects.addCoal > 0 || gift.effects.addOre > 0)
                    {
                        UI.HorizontalMarqueeUI.Instance?.AddMessage(gift.playerName, gift.avatarUrl,
                            $"神秘空投：食物+{gift.effects.addFood}，煤炭+{gift.effects.addCoal}，矿石+{gift.effects.addOre}，城门+{gift.effects.addGateHp}HP");
                    }
                    if (reviveCount > 0)
                    {
                        UI.HorizontalMarqueeUI.Instance?.AddMessage(gift.playerName, gift.avatarUrl,
                            $"神秘空投：随机复活 {reviveCount} 名矿工");
                    }
                }
                else if (gift.giftTier == 2 && gift.effects.globalEfficiencyBoost > 1.0f)
                {
                    int boostPct = Mathf.RoundToInt((gift.effects.globalEfficiencyBoost - 1.0f) * 100f);
                    int dur = gift.effects.globalEfficiencyDuration > 0 ? gift.effects.globalEfficiencyDuration : 30;
                    UI.HorizontalMarqueeUI.Instance?.AddMessage(gift.playerName, gift.avatarUrl,
                        $"能力药丸：全员采矿效率 +{boostPct}%，持续 {dur}s");
                }
                else if (gift.giftTier == 4 && gift.effects.tempBoost > 1.0f)
                {
                    // T4 能量电池：守护者 = 自己 +30%；助威者路径 = 已通过 redirect 提示，本分支忽略避免重复跑马灯
                    if (!gift.effects.supporterRedirect)
                    {
                        int boostPct = Mathf.RoundToInt((gift.effects.tempBoost - 1.0f) * 100f);
                        int dur = gift.effects.boostDuration > 0 ? gift.effects.boostDuration : 180;
                        UI.HorizontalMarqueeUI.Instance?.AddMessage(gift.playerName, gift.avatarUrl,
                            $"能量电池：炉温+{gift.effects.addHeat}℃，自身效率 +{boostPct}%，持续 {dur}s");
                    }
                }

                // §31.3 T4 联动：解冻矿工跑马灯（独立于 T4 主消费，所有 T4 触发都可能解冻）
                if (gift.effects.unfrozenWorkers > 0)
                {
                    UI.HorizontalMarqueeUI.Instance?.AddMessage(gift.playerName, gift.avatarUrl,
                        $"联动效果：解冻 {gift.effects.unfrozenWorkers} 名被冰封矿工");
                }
            }
        }

        /// <summary>
        /// audit-r10 §29：排名变化 SFX 轻量触发（只做一件事：检测 Top1 是否易主）。
        /// 不做 Top1→Top2 或 per-player 比较，避免播放过密；目的是给直播间一个"新王上位"的声音反馈。
        /// </summary>
        private void TryPlayRankChangeSfx(LiveRankingData data)
        {
            if (data?.rankings == null || data.rankings.Length == 0) return;
            string currentTop1 = data.rankings[0]?.playerId;
            if (string.IsNullOrEmpty(currentTop1)) return;
            if (_lastTop1PlayerId != null && _lastTop1PlayerId != currentTop1)
            {
                Systems.AudioManager.Instance?.PlaySFX(Core.AudioConstants.SFX_RANK_UP);

                // audit-r11 GAP-C03：旧 Top1 完全跌出榜单（不在新 rankings 中）→ 播 SFX_RANK_DOWN
                //   只触发"跌出可见排名"，避免 Top1↔Top2 频繁切换播两个音效
                bool oldTop1StillVisible = false;
                for (int i = 0; i < data.rankings.Length; i++)
                {
                    if (data.rankings[i] != null && data.rankings[i].playerId == _lastTop1PlayerId)
                    {
                        oldTop1StillVisible = true;
                        break;
                    }
                }
                if (!oldTop1StillVisible)
                {
                    Systems.AudioManager.Instance?.PlaySFX(Core.AudioConstants.SFX_RANK_DOWN);
                }
            }
            _lastTop1PlayerId = currentTop1;
        }

        private void HandlePlayerJoined(SurvivalPlayerJoinedData data)
        {
            // #1 JsonUtility 返回默认对象时 playerId 为 null/空，拒绝处理
            if (data == null || string.IsNullOrEmpty(data.playerId)) return;
            var displayName = string.IsNullOrEmpty(data.playerName) ? "匿名" : data.playerName;

            if (!_contributions.ContainsKey(data.playerId))
                _contributions[data.playerId] = 0f;

            workerManager?.SpawnWorker(data);
            OnPlayerJoined?.Invoke(data);
            OnPlayerActivityMessage?.Invoke($"{displayName} 加入了极地部落");
            UI.HorizontalMarqueeUI.Instance?.AddMessage(displayName, data.avatarUrl, "加入了极地生存！");
        }

        private void HandleDefeat(string reason)
        {
            if (_state != SurvivalState.Running) return;
            Debug.Log($"[SurvivalGM] 失败！原因: {reason}");

            // 城门失守时触发顶部飘字
            if (reason == "gate_breached")
                UI.TopFloatingTextUI.Instance?.ShowDanger("【告急】城门失守！游戏结束");

            // 🆕 v1.26 永续模式：无胜利分支，客户端本地 fallback 结算（正常路径走服务器 survival_game_ended）
            var endData = new SurvivalGameEndedData
            {
                reason = reason,
                dayssurvived = dayNightManager != null ? dayNightManager.CurrentDay : 0
                // fortressDayBefore/After/newbieProtected 由服务器 survival_game_ended 权威提供；
                // 本地 fallback 时保持默认 0/false，UI 将显示 "堡垒日 0 → 0"（仅 fallback 极端场景）
            };
            HandleGameEnded(endData);
        }

        private void HandleDefeatOrVictory(string reason)
        {
            if (_state != SurvivalState.Running) return;
            // 🆕 v1.26 永续模式：方法名保留兼容旧调用，但永续模式无胜利分支——
            // "survived" reason 已从协议移除；若外部仍传入该值，按 manual 降级
            var effectiveReason = reason == "survived" ? "manual" : reason;
            var endData = new SurvivalGameEndedData
            {
                reason = effectiveReason,
                dayssurvived = dayNightManager != null ? dayNightManager.CurrentDay : 0,
                totalScore = GetTotalContribution()
            };
            HandleGameEnded(endData);
        }

        private void HandleGameEnded(SurvivalGameEndedData data)
        {
            monsterWaveSpawner?.StopAllWaves();
            dayNightManager?.HandleSettlement();
            OnGameEnded?.Invoke(data);

            // audit-r12 GAP-C03 / Agent-C：§29.1 BGM_LOSE 接入周期性失败结算（manual 主动终止不切）
            //   v1.26 永续模式 → 任何非 manual 的 game_ended 都视作失败降级
            if (data != null && data.reason != "manual")
            {
                DrscfZ.Systems.AudioManager.Instance?.CrossfadeBGM(DrscfZ.Core.AudioConstants.BGM_LOSE, 1.5f);
            }

            // 直接通知 SurvivalSettlementUI（它可能处于 inactive 状态，无法通过事件收到）
            if (_settlementUI != null)
            {
                // 将服务器排行映射到结算 UI 数据结构
                global::System.Collections.Generic.List<global::DrscfZ.UI.RankEntry> rankList = null;
                if (data.rankings != null && data.rankings.Length > 0)
                {
                    rankList = new global::System.Collections.Generic.List<global::DrscfZ.UI.RankEntry>(data.rankings.Length);
                    foreach (var r in data.rankings)
                        rankList.Add(new global::DrscfZ.UI.RankEntry
                        {
                            Nickname = string.IsNullOrEmpty(r.playerName) ? r.playerId : r.playerName,
                            Score    = Mathf.RoundToInt(r.contribution)
                        });
                }

                // 🆕 v1.26 永续模式：IsVictory 固定 false（无胜利分支）；
                // reason 映射表补 all_dead / manual（§16.5）
                var settlement = new global::DrscfZ.UI.SettlementData
                {
                    IsVictory    = false,
                    SurvivalDays = data.dayssurvived,
                    FailReason   = data.reason switch
                    {
                        "food_depleted" => "food",
                        "temp_freeze"   => "temperature",
                        "gate_breached" => "gate",
                        "all_dead"      => "all_dead",   // 🆕 §16.5 矿工夜晚全灭
                        "manual"        => "manual",     // 🆕 §16.4 GM 手动终止
                        _               => "unknown"
                    },
                    FortressDayBefore = data.fortressDayBefore,  // 🆕 §16.6
                    FortressDayAfter  = data.fortressDayAfter,   // 🆕 §16.6
                    NewbieProtected   = data.newbieProtected,    // 🆕 §16.6
                    IsManual          = data.reason == "manual", // 🆕 §16.4 区分主动终止
                    Rankings = rankList,
                };
                StartCoroutine(DelayShowSettlement(3f, settlement));
            }
            else
            {
                StartCoroutine(DelayedSettlement(3f));
            }
        }

        private IEnumerator DelayShowSettlement(float delay, global::DrscfZ.UI.SettlementData data)
        {
            yield return new WaitForSeconds(delay);
            ChangeState(SurvivalState.Settlement);
            if (_settlementUI != null)
                _settlementUI.gameObject.SetActive(true);
            _settlementUI?.ShowSettlement(data);
            // 结算界面停留，直到玩家点击"重新开始"按钮手动关闭
            // _restartButton → OnRestartClicked → RequestReturnFromSettlement()
        }

        private IEnumerator DelayedSettlement(float delay)
        {
            yield return new WaitForSeconds(delay);
            ChangeState(SurvivalState.Settlement);
            // 无UI时停留在结算状态，玩家通过外部方式（如服务器重置）返回大厅
        }

        /// <summary>
        /// 从结算状态返回大厅（返回 Waiting 状态，显示难度选择让主播重新选择）。
        /// </summary>
        public void RequestReturnFromSettlement()
        {
            if (_state != SurvivalState.Settlement) return;
            // 结算后回 Idle（大厅），让用户点"开始游戏"重新选难度开局
            // 不设 _returnToWaiting = true，避免进入 Waiting 后两个 UI 都不显示
            IsEnteringScene = false;
            ChangeState(SurvivalState.Loading);
            NetworkManager.Instance?.SendMessage("reset_game");
            _loadingTimeoutCoroutine = StartCoroutine(LoadingTimeout());
            Debug.Log("[SGM] 结算结束→Loading，已发送 reset_game，结束后回到 Idle 大厅...");
        }

        // ==================== 工具 ====================

        public void ConnectToServer()
        {
            ChangeState(SurvivalState.Connecting);
            NetworkManager.Instance?.Connect();
        }

        /// <summary>
        /// 断线重连"继续上一局"：设 IsEnteringScene=true → 发 sync_state → 等服务器推送当前状态。
        /// 服务器会重新发 survival_game_state{state:'day'/'night'}，触发客户端进入 Running。
        /// </summary>
        public void RequestResumeSession()
        {
            if (_state != SurvivalState.Waiting && _state != SurvivalState.Idle) return;

            IsEnteringScene = true;
            ChangeState(SurvivalState.Loading);
            NetworkManager.Instance?.SendMessage("sync_state");
            _loadingTimeoutCoroutine = StartCoroutine(LoadingTimeout());
            Debug.Log("[SGM] RequestResumeSession → Loading，已发送 sync_state，等待服务器重新推送当前状态...");
        }

        /// <summary>
        /// 从大厅进入等待阶段（本地切换，不发服务器消息）。
        /// Idle + 已连接 → Waiting，触发 DifficultySelectUI 显示。
        /// </summary>
        public void RequestEnterWaiting()
        {
            if (_state != SurvivalState.Idle) return;
            ChangeState(SurvivalState.Waiting);
            Debug.Log("[SGM] 进入等待阶段（Waiting），等待主播选择难度...");
        }

        /// <summary>
        /// 主播选择难度：记录所选难度并通知 UI。
        /// </summary>
        public void SetDifficulty(DifficultyLevel level)
        {
            SelectedDifficulty = level;
            OnDifficultySet?.Invoke(level);
            Debug.Log($"[SGM] 难度已选择: {level}");
        }

        /// <summary>
        /// 主播点击"▶ 开始玩法"：切 Loading → 发 start_game（含难度）→ 等服务器响应。
        /// </summary>
        public void RequestStartGame()
        {
            if (_state != SurvivalState.Idle && _state != SurvivalState.Waiting) return;

            IsEnteringScene = true;
            ChangeState(SurvivalState.Loading);

            // 将难度作为参数传给服务器
            string diffStr = SelectedDifficulty switch
            {
                DifficultyLevel.Easy   => "easy",
                DifficultyLevel.Hard   => "hard",
                _                      => "normal"
            };
            long ts = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            NetworkManager.Instance?.SendJson(
                $"{{\"type\":\"start_game\",\"data\":{{\"difficulty\":\"{diffStr}\"}},\"timestamp\":{ts}}}");

            _loadingTimeoutCoroutine = StartCoroutine(LoadingTimeout());
            Debug.Log($"[SGM] State→Loading (entering)，已发送 start_game(difficulty={diffStr})，等待服务器确认...");
        }

        /// <summary>
        /// 主播点击"退出返回大厅"：切 Loading → 发 reset_game → 等服务器响应。
        /// </summary>
        public void RequestExitToLobby()
        {
            if (_state != SurvivalState.Running) return;

            IsEnteringScene = false;
            ChangeState(SurvivalState.Loading);
            NetworkManager.Instance?.SendMessage("reset_game");
            _loadingTimeoutCoroutine = StartCoroutine(LoadingTimeout());
            Debug.Log("[SGM] State→Loading (exiting)，已发送 reset_game，等待服务器确认...");
        }

        /// <summary>
        /// 兼容旧接口（部分已有代码调用 RequestResetGame）。
        /// 支持 Running 和 Settlement 两种来源状态。
        /// </summary>
        public void RequestResetGame()
        {
            if (_state == SurvivalState.Running)
                RequestExitToLobby();
            else if (_state == SurvivalState.Settlement)
                RequestReturnFromSettlement();
            else if (_state == SurvivalState.Waiting)
            {
                // Waiting 状态重置：发 reset_game 让服务器清空，回到 Waiting
                NetworkManager.Instance?.SendMessage("reset_game");
                Debug.Log("[SGM] Waiting 状态发送 reset_game");
            }
        }

        /// <summary>§17.15 主播"关闭引导"：发送 disable_onboarding_for_session（服务端校验 isRoomCreator）。
        /// 服务端收到后置 `_onboardingDisabled=true`，本次房间会话内不再广播 B1-B3；
        /// 下一局（engine.reset()）自动清除。</summary>
        public void SendDisableOnboarding()
        {
            NetworkManager.Instance?.SendMessage("disable_onboarding_for_session");
            Debug.Log("[SGM] §17.15 disable_onboarding_for_session 已发送");
        }

        /// <summary>🆕 §34.4 E9 周期/赛季间难度切换（仅主播发送）。服务端校验 isRoomCreator + applyAt 合法性。
        /// <paramref name="difficulty"/>: "easy" | "normal" | "hard"
        /// <paramref name="applyAt"/>: "next_night"（恢复期第一个白天）| "next_season"（赛季切换 30s 窗口）</summary>
        public void SendChangeDifficulty(string difficulty, string applyAt)
        {
            if (string.IsNullOrEmpty(difficulty) || string.IsNullOrEmpty(applyAt))
            {
                Debug.LogWarning($"[SGM] SendChangeDifficulty 参数非法：difficulty={difficulty}, applyAt={applyAt}");
                return;
            }
            long ts = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string json = $"{{\"type\":\"change_difficulty\",\"data\":{{\"difficulty\":\"{difficulty}\",\"applyAt\":\"{applyAt}\"}},\"timestamp\":{ts}}}";
            NetworkManager.Instance?.SendJson(json);
            Debug.Log($"[SGM] §34.4 E9 change_difficulty 已发送 difficulty={difficulty} applyAt={applyAt}");
        }

        /// <summary>🆕 §34 B2 主播"立即重开"跳过结算（仅主播发送）。
        /// 服务端校验 isRoomCreator + 当前在结算期，校验通过后提前结束 30s 结算并进入 recovery。</summary>
        public void SendStreamerSkipSettlement()
        {
            long ts = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string json = $"{{\"type\":\"streamer_skip_settlement\",\"data\":{{}},\"timestamp\":{ts}}}";
            NetworkManager.Instance?.SendJson(json);
            Debug.Log("[SGM] §34 B2 streamer_skip_settlement 已发送");
        }

        private void ResetAllSystems()
        {
            _contributions.Clear();
            _lastSettlementHighlights = null;  // 🆕 §34 B2 清理缓存
            SelectedDifficulty = DifficultyLevel.None;  // 重置难度选择，让 DifficultySelectUI 再次显示
            resourceSystem?.Reset();
            cityGateSystem?.Reset();
            dayNightManager?.Reset();
            monsterWaveSpawner?.ResetAll();
            workerManager?.ClearAll();

            if (_returnToWaiting)
            {
                _returnToWaiting = false;
                ChangeState(SurvivalState.Waiting);     // 结算后回到等待阶段（可重新选难度）
            }
            else
            {
                ChangeState(SurvivalState.Idle);
            }
        }

        private void ChangeState(SurvivalState newState)
        {
            if (_state == newState) return;
            _state = newState;
            OnStateChanged?.Invoke(newState);

            // Running 状态开始时初始化墙壁屏障
            if (newState == SurvivalState.Running)
            {
                // 城墙 Z 轴防线参数（根据 chengqiang-chengmen 模型 bounds 推算）
                // chengqiang-chengmen bounds: Z from -8.33 to -6.38, center Z ≈ -7.35
                // 城门缺口 X: 约 -1.5 ~ 3.5（chengmen 中间开口区域）
                WallBarrier.Initialize(wallZ: -7.5f, gateMinX: -1.5f, gateMaxX: 3.5f, thickness: 2.0f);
            }
        }

        private void TrackContribution(string playerId, float amount)
        {
            if (string.IsNullOrEmpty(playerId)) return;
            if (!_contributions.ContainsKey(playerId)) _contributions[playerId] = 0f;
            _contributions[playerId] += amount;
        }

        private float GetTotalContribution()
        {
            float total = 0;
            foreach (var v in _contributions.Values) total += v;
            return total;
        }

        // ==================== Loading 超时保护 ====================

        private IEnumerator LoadingTimeout()
        {
            yield return new WaitForSeconds(LOADING_TIMEOUT_SECONDS);

            if (_state == SurvivalState.Loading)
            {
                Debug.LogWarning($"[SGM] Loading 超时（{LOADING_TIMEOUT_SECONDS}s），强制重置到 Idle");
                ResetAllSystems();
            }
        }

        private void StopLoadingTimeout()
        {
            if (_loadingTimeoutCoroutine != null)
            {
                StopCoroutine(_loadingTimeoutCoroutine);
                _loadingTimeoutCoroutine = null;
            }
        }

        // ==================== 新消息处理：战斗系统 ====================

        private void HandleCombatAttack(string type, string dataJson)
        {
            var data = JsonUtility.FromJson<CombatAttackData>(dataJson);
            if (data == null) return;

            // 找到目标怪物，播放受击特效（服务器已计算实际伤害）
            var monster = monsterWaveSpawner?.FindMonsterById(data.targetId);
            if (monster != null)
                monster.ShowHitEffect();

            // 怪物受击 → 轻微相机抖动
            SurvivalCameraController.OnMonsterAttack();

            // 追踪贡献（内部 float dict，用于结算总分统计；服务端不再下发 score，固定计 1）
            const int combatScore = 1;
            TrackContribution(data.attackerId, combatScore);

            // 同步到 RankingSystem（供 SurvivalLiveRankingUI 实时榜读取）
            RankingSystem.Instance?.TrackSurvivalScore(
                data.attackerId,
                string.IsNullOrEmpty(data.attackerName) ? "匿名" : data.attackerName,
                combatScore);

            OnPlayerActivityMessage?.Invoke($"{data.attackerName} 攻击怪物 +{combatScore}分");
        }

        private void HandleMonsterDied(string type, string dataJson)
        {
            var data = JsonUtility.FromJson<MonsterDiedData>(dataJson);
            if (data == null) return;

            // 找到目标怪物并强制触发死亡动画
            var monster = monsterWaveSpawner?.FindMonsterById(data.monsterId);
            if (monster != null && !monster.IsDead)
                monster.TakeDamage(999999);

            // 特殊处理：Boss死亡
            if (data.monsterType == "boss")
            {
                UI.AnnouncementUI.Instance?.ShowAnnouncement(
                    "BOSS已倒下!", "夜晚即将结束...",
                    new Color(1f, 0.85f, 0.1f), 3f);
            }

            // audit-r21 GAP-A21-02：消费 data.reason 推送差异化跑马灯（r19 加字段，r21 补 UI 链路）
            // 服务端 emit reason: 'elite_raid_timeout'（精英来袭超时撤退）/ 'meteor_shower'（流星雨群杀）/ ''（玩家击杀）
            if (!string.IsNullOrEmpty(data.reason))
            {
                switch (data.reason)
                {
                    case "elite_raid_timeout":
                        UI.HorizontalMarqueeUI.Instance?.AddMessage("精英来袭", null, "未能击杀，怪物撤退");
                        break;
                    case "meteor_shower":
                        UI.HorizontalMarqueeUI.Instance?.AddMessage("流星雨", null, "怪物被流星击杀");
                        break;
                }
            }

            // audit-r23 GAP-A23-02：消费 killerId PvE 击杀来源（服务端 L5139/L4521 emit；之前 0 客户端反馈）
            // 'gate_thorns' = §10 城门反伤被动；'gate_frost_pulse' = §10 寒冰冲击波被动
            switch (data.killerId)
            {
                case "gate_thorns":
                    UI.HorizontalMarqueeUI.Instance?.AddMessage("城门反伤", null, $"{data.monsterType} 被反伤击杀");
                    break;
                case "gate_frost_pulse":
                    UI.HorizontalMarqueeUI.Instance?.AddMessage("寒冰冲击波", null, $"{data.monsterType} 被冰脉击杀");
                    break;
            }

            Debug.Log($"[SurvivalGM] 怪物死亡: {data.monsterId} ({data.monsterType}) 击杀者:{data.killerId} 原因:{data.reason}");
        }

        private void HandleNightCleared(string type, string dataJson)
        {
            // 🆕 §2 永续无胜利：Boss被击败只是本夜阶段结束，非胜利；使用中性音效
            // 服务器会随后发phase_changed切换到白天
            UI.AnnouncementUI.Instance?.ShowAnnouncement(
                "夜晚已清扫!", "极地守护者们干得漂亮！",
                new Color(0.4f, 0.9f, 1f), 3f);
            SurvivalCameraController.OnNightCleared();
            // 改用 sfx_day_start（阶段结束切白天的中性提示音，已在 AudioConstants）
            // 若需单独音效可后续替换为 sfx_night_cleared
            Systems.AudioManager.Instance?.PlaySFX(AudioConstants.SFX_DAY_START);
            Debug.Log("[SurvivalGM] 夜晚提前结束！BOSS已被击败。");
        }

        private void HandleGateUpgraded(string type, string dataJson)
        {
            var data = JsonUtility.FromJson<GateUpgradedData>(dataJson);
            if (data == null) return;

            // 🆕 v1.22 §10 主路径：新签名含 tierName/features/hpBonus
            // audit-r8：Lv6 回满时服务端计算 gateMaxHp-gateHp，可能 =0（城门满血升级）；不用 20 兜底避免错误加血
            int hpBonus = data.hpBonus >= 0 ? data.hpBonus : 0;
            cityGateSystem?.HandleUpgrade(data.newLevel, data.newMaxHp, hpBonus, data.tierName, data.newFeatures);

            string tierLabel = string.IsNullOrEmpty(data.tierName) ? "" : $"「{data.tierName}」";
            // 🔴 audit-r25 GAP-D25-07：data.source 区分三种升级路径（r6 加字段但 UI 0 消费第 19 轮 audit）
            //   'broadcaster' 主播主动升级（消耗矿石）/ 'gift_t6' T6 礼物联动 / 'expedition_trader' 商队交易
            string sourceFlavor = data.source switch
            {
                "gift_t6"            => "（T6 神秘空投联动）",
                "expedition_trader"  => "（商队交易）",
                _                    => "",
            };
            UI.AnnouncementUI.Instance?.ShowAnnouncement(
                $"城门升级至 Lv.{data.newLevel}{tierLabel}!{sourceFlavor}",
                $"最大HP提升至 {data.newMaxHp}",
                new Color(0.2f, 0.8f, 1f), 2f);
            OnPlayerActivityMessage?.Invoke($"城门已升级至 Lv.{data.newLevel}{tierLabel}{sourceFlavor}（最大HP:{data.newMaxHp}）");
        }

        private void HandleGateUpgradeFailed(string dataJson)
        {
            var data = JsonUtility.FromJson<GateUpgradeFailedData>(dataJson);
            if (data == null) return;
            string msg;
            switch (data.reason)
            {
                case "max_level":
                    msg = "城门已达最高等级！";
                    break;
                case "insufficient_ore":
                    msg = $"矿石不足！需要 {data.required}，当前 {data.available}";
                    break;
                case "feature_locked":
                    msg = data.unlockDay > 0
                        ? $"Lv.{data.blockedLevel} 需第 {data.unlockDay} 天后解锁"
                        : "尚未解锁此等级";
                    break;
                case "daily_limit":
                    msg = "本日已升级过，请等待明天";
                    break;
                case "wrong_phase":
                    msg = "当前阶段不能升级城门（白天 / 夜晚濒危时段可升）";
                    break;
                case "boss_fight":
                    msg = "Boss 战斗中不能升级城门";
                    break;
                default:
                    msg = "城门升级失败";
                    break;
            }
            UI.AnnouncementUI.Instance?.ShowAnnouncement("城门升级失败", msg, new Color(1f, 0.4f, 0.2f), 2f);
            Debug.Log($"[SGM] gate_upgrade_failed: {data.reason}");
        }

        // ==================== §30 矿工成长系统 ====================

        private void HandleWorkerLevelUp(WorkerLevelUpData data)
        {
            // 路由到 WorkerManager 刷新对应 Worker 的显示
            WorkerManager.Instance?.HandleWorkerLevelUp(data);

            // 阶段10（传奇）→ 相机震撼 + 镜头推近（WorkerManager 消费 worker 位置）+ 金色跑马灯 + 专属音效
            //   audit-r9 §30.8 策划案 L3359：镜头短暂推近该矿工（0.8s）+ 金色粒子 + 跑马灯
            //   ZoomInBurst 由 WorkerManager.HandleWorkerLevelUp 调用（已有 worker ref）
            //   audit-r11 GAP-C09：接入 SFX_TIER_PROMOTE / SFX_LEGEND_PROMOTE（策划案 §30.8 "专属音效"未实装）
            if (data.newTier >= 10)
            {
                SurvivalCameraController.Shake(0.3f, 0.8f);
                Systems.AudioManager.Instance?.PlaySFX(Core.AudioConstants.SFX_LEGEND_PROMOTE);
                string legendName = string.IsNullOrEmpty(data.playerName) ? "传奇矿工" : data.playerName;
                UI.HorizontalMarqueeUI.Instance?.AddMessage(
                    legendName, null, "<color=#FFD700>晋升传奇矿工！</color>");
                Debug.Log($"[SGM][Legend] {legendName} 达到阶 10 传奇（Shake + SFX_LEGEND_PROMOTE + 金跑马灯；美术 VFX_LegendGold 粒子待交付）");
            }
            else
            {
                // 阶段 2~9：彩色公告横幅 + 专属音效
                Systems.AudioManager.Instance?.PlaySFX(Core.AudioConstants.SFX_TIER_PROMOTE);
                UI.AnnouncementUI.Instance?.ShowAnnouncement(
                    $"{data.playerName} 晋升{GetTierTitle(data.newTier)}！",
                    $"Lv.{data.newLevel}",
                    GetTierColor(data.newTier),
                    2f);
            }

            // 跑马灯消息（所有阶段都推送）
            UI.HorizontalMarqueeUI.Instance?.AddMessage(
                data.playerName, null, $"矿工升至阶{data.newTier}！");

            OnWorkerLevelUp?.Invoke(data);
            Debug.Log($"[SGM] worker_level_up: {data.playerName} Lv.{data.newLevel} tier={data.newTier} skin={data.skinId}");
        }

        private void HandleLegendReviveTriggered(LegendReviveData data)
        {
            WorkerManager.Instance?.HandleLegendRevive(data.playerId);
            // 🆕 P0-B10：跑马灯 + 弹幕双通道（Marquee 是主视觉，BarrageMessage 是日志层兜底）
            string displayName = string.IsNullOrEmpty(data.playerName) ? "传奇矿工" : data.playerName;
            UI.HorizontalMarqueeUI.Instance?.AddMessage(
                displayName, null, "传奇之力！免于死亡！");
            OnPlayerActivityMessage?.Invoke($"[传奇] {displayName} 传奇之力触发，免于死亡！");
            OnLegendReviveTriggered?.Invoke(data);
            Debug.Log($"[SGM] legend_revive_triggered: {displayName}");
        }

        private void HandleWorkerSkinChanged(WorkerSkinChangedData data)
        {
            WorkerManager.Instance?.HandleSkinChanged(data);
            OnWorkerSkinChanged?.Invoke(data);
            Debug.Log($"[SGM] worker_skin_changed: {data.playerName} tier={data.tier} skin={data.skinId}");
        }

        /// <summary>§30.3 阶6 15% 格挡触发（worker_blocked）→ 矿工一闪金光 + 跑马灯</summary>
        private void HandleWorkerBlocked(WorkerBlockedData data)
        {
            // 占位视觉：借用 WorkerVisual.TriggerAssignmentFlash 的金色闪烁表达"格挡生效"
            var worker = WorkerManager.Instance?.ActiveWorkers;
            if (worker != null)
            {
                foreach (var w in worker)
                {
                    if (w != null && w.PlayerId == data.playerId)
                    {
                        w.GetComponent<WorkerVisual>()?.TriggerAssignmentFlash();
                        break;
                    }
                }
            }
            OnPlayerActivityMessage?.Invoke($"[格挡] {data.playerName} 免于伤害");
        }

        /// <summary>阶段 1~10 对应的称号（§30.3 表）</summary>
        private static string GetTierTitle(int tier)
        {
            switch (Mathf.Clamp(tier, 1, 10))
            {
                case 1:  return "见习矿工";
                case 2:  return "老练矿工";
                case 3:  return "骨干矿工";
                case 4:  return "精英矿工";
                case 5:  return "强袭矿工";
                case 6:  return "铁卫矿工";
                case 7:  return "统帅矿工";
                case 8:  return "战神矿工";
                case 9:  return "神话矿工";
                case 10: return "传奇矿工";
                default: return "矿工";
            }
        }

        /// <summary>阶段 1~10 对应的主题色（§30.8 表，用于公告横幅）</summary>
        private static Color GetTierColor(int tier)
        {
            switch (Mathf.Clamp(tier, 1, 10))
            {
                case 1:  return new Color(0.70f, 0.70f, 0.70f); // 灰
                case 2:  return new Color(0.72f, 0.45f, 0.20f); // 铜
                case 3:  return new Color(0.75f, 0.75f, 0.78f); // 银
                case 4:  return new Color(1.00f, 0.84f, 0.00f); // 金
                case 5:  return new Color(1.00f, 0.55f, 0.10f); // 橙
                case 6:  return new Color(0.25f, 0.55f, 1.00f); // 蓝
                case 7:  return new Color(0.68f, 0.35f, 1.00f); // 紫
                case 8:  return new Color(0.65f, 0.10f, 0.10f); // 深红
                case 9:  return new Color(0.30f, 0.05f, 0.45f); // 深紫
                case 10: return new Color(1.00f, 0.30f, 0.10f); // 金红
                default: return Color.white;
            }
        }

        // ==================== §10 v1.22 城门升级系统 v2 ====================

        /// <summary>🆕 v1.22 §10 城门等级特性触发（Lv4反伤 / Lv5光环 / Lv6冲击波）
        /// audit-r7 §19 扩展：优先使用 hitMonsters（legacy），退回 targets（通用别名）；
        /// 位置优先 gatePos（nested），退回 flat x/y/z；durationMs 退回 freezeMs。</summary>
        private void HandleGateEffectTriggered(string dataJson)
        {
            var d = JsonUtility.FromJson<GateEffectTriggeredData>(dataJson);
            if (d == null || string.IsNullOrEmpty(d.effect)) return;

            // §10.7 对外广播事件：CityGateSystem / VFX 层订阅，按 effect 分流播视觉/音效
            OnGateEffectTriggered?.Invoke(d);

            // audit-r7 §19：hitMonsters 优先（legacy），为空时退回 targets 别名
            string[] effectiveTargets = (d.hitMonsters != null && d.hitMonsters.Length > 0)
                ? d.hitMonsters
                : d.targets;

            switch (d.effect)
            {
                case "thorns":
                {
                    // Lv4 反伤：服务端均分到全体活怪，遍历 effectiveTargets 逐个飘字 damagePerMonster
                    int perMonster = d.damagePerMonster;
                    if (perMonster > 0 && effectiveTargets != null && monsterWaveSpawner != null)
                    {
                        foreach (var mid in effectiveTargets)
                        {
                            var mc = monsterWaveSpawner.FindById(mid);
                            if (mc != null)
                                DamageNumber.Show(mc.transform.position + Vector3.up * 2f, perMonster, Color.yellow);
                        }
                    }
                    Debug.Log($"[GateFX] thorns 反伤 total={d.totalDamage} perMonster={perMonster} × {(effectiveTargets?.Length ?? 0)} 只");
                    break;
                }
                case "frost_aura":
                {
                    // 🆕 P0-B6 Lv5 冰霜光环：客户端减速 + 浅蓝 tint
                    //   服务端下发 { active, radius, slowMult, gatePos:{x,y,z} }。
                    //   写入 MonsterController 静态字段，所有活跃怪物每帧判定。
                    DrscfZ.Monster.MonsterController.FrostAuraActive    = d.active;
                    DrscfZ.Monster.MonsterController.FrostAuraRadius    = d.radius > 0 ? d.radius : 6f;
                    DrscfZ.Monster.MonsterController.FrostAuraSlowMult  = d.slowMult > 0f ? d.slowMult : 0.7f;
                    if (d.gatePos != null)
                    {
                        DrscfZ.Monster.MonsterController.FrostAuraCenter = new Vector3(
                            d.gatePos.x, d.gatePos.y, d.gatePos.z);
                    }
                    else if (d.x != 0f || d.y != 0f || d.z != 0f)
                    {
                        // audit-r7 §19：flat x/y/z 退路（服务端若下发扁平坐标）
                        DrscfZ.Monster.MonsterController.FrostAuraCenter = new Vector3(d.x, d.y, d.z);
                    }
                    else if (cityGateSystem != null)
                    {
                        // 兜底：服务端未下发任何位置时用本地 CityGateSystem 位置
                        DrscfZ.Monster.MonsterController.FrostAuraCenter = cityGateSystem.transform.position;
                    }
                    Debug.Log($"[GateFX] Frost aura {(d.active ? "ACTIVATED" : "DEACTIVATED")} " +
                              $"radius={DrscfZ.Monster.MonsterController.FrostAuraRadius}m " +
                              $"slow×{DrscfZ.Monster.MonsterController.FrostAuraSlowMult}");
                    break;
                }
                case "frost_pulse":
                {
                    // Lv6 寒冰冲击波：屏幕震动 + 对命中怪物播放冻结闪烁
                    SurvivalCameraController.Shake(0.2f, 0.3f);
                    if (effectiveTargets != null && monsterWaveSpawner != null)
                    {
                        // 🆕 P0-B7：写 FrozenUntil 让怪物停住；同时播放闪烁视觉
                        // audit-r7 §19：freezeMs 优先（legacy），0 时退回 durationMs 别名
                        int freezeMillis = d.freezeMs > 0 ? d.freezeMs : (int)d.durationMs;
                        float freezeDuration = freezeMillis > 0 ? freezeMillis / 1000f : 2f;
                        float until = Time.time + freezeDuration;
                        foreach (var id in effectiveTargets)
                        {
                            var target = monsterWaveSpawner.FindById(id);
                            if (target != null)
                            {
                                target.FrozenUntil = until;
                                target.PlayFreezeFlash();
                            }
                        }
                    }
                    // TODO §10.7：FX_FrostPulse 环形冲击波 VFX + sfx_frost_pulse（美术资源待落地）
                    Debug.Log($"[GateFX] Frost pulse ({(effectiveTargets?.Length ?? 0)} monsters hit, freezeMs={d.freezeMs} durationMs={d.durationMs})");
                    break;
                }
                default:
                    Debug.LogWarning($"[SGM] 未知 gate_effect_triggered.effect: {d.effect}");
                    break;
            }
        }

        private void HandleBossAppeared(string type, string dataJson)
        {
            var data = JsonUtility.FromJson<BossAppearedData>(dataJson);

            // 视觉：生成 Boss（X_guai01 放大 2.5 倍，红色标识）
            if (data != null)
                monsterWaveSpawner?.SpawnBoss(data.day, data.bossHp, data.bossAtk);

            UI.AnnouncementUI.Instance?.ShowAnnouncement(
                "BOSS降临!",
                "全力防守！集中火力击杀BOSS！",
                new Color(1f, 0.2f, 0.2f), 4f);
            SurvivalCameraController.Shake(0.3f, 0.8f);
            // r14 GAP-C14-02：原字面量 "sfx_gate_alarm" → AudioConstants.SFX_GATE_ALARM 常量
            Systems.AudioManager.Instance?.PlaySFX(AudioConstants.SFX_GATE_ALARM);
            Debug.Log("[SurvivalGM] BOSS出现！全员备战！");
        }

        // ==================== §31 怪物多样性 — Boss 暴走 ====================

        /// <summary>
        /// 🆕 §31 首领卫兵全部死亡 → Boss ATK × 1.3，服务端广播 boss_enraged。
        /// 客户端表现：跑马灯公告 + 摄像机震动 + 弹幕提示。
        /// </summary>
        private void HandleBossEnraged(BossEnragedData data)
        {
            string msg = $"首领卫兵阵亡！Boss 进入暴走状态！(ATK={data.newAtk})";

            // 跑马灯公告（服务器驱动的公共消息）
            UI.HorizontalMarqueeUI.Instance?.AddMessage("Boss 暴走", null, msg);
            // 弹幕面板提示（通过 OnPlayerActivityMessage 广播，BarrageMessageUI 自动渲染）
            OnPlayerActivityMessage?.Invoke(msg);

            // 摄像机震动（中等强度）
            SurvivalCameraController.Shake(0.3f, 0.5f);

            Debug.Log($"[SurvivalGM] Boss enraged! newAtk={data.newAtk}");
        }

        // ==================== 助威模式 §33（🆕 v1.27）====================

        private void HandleSupporterJoined(SupporterJoinedData data)
        {
            if (data == null || string.IsNullOrEmpty(data.playerId)) return;
            var displayName = string.IsNullOrEmpty(data.playerName) ? "匿名" : data.playerName;

            // §33 Tier C：TopBar 切换由 SupporterTopBarUI 订阅 OnSupporterJoined 接管（§33.6.1）
            // BarrageMessageUI 通过 OnPlayerActivityMessage 作日志层；SupporterJoinedToastUI 订阅 OnSupporterJoined 作主视觉层
            OnPlayerActivityMessage?.Invoke($"{displayName} 加入助威！发送弹幕为全队加油");
            OnSupporterJoined?.Invoke(data);
        }

        private void HandleSupporterAction(SupporterActionData data)
        {
            if (data == null || string.IsNullOrEmpty(data.playerId)) return;
            var displayName = string.IsNullOrEmpty(data.playerName) ? "匿名" : data.playerName;
            string cmdDesc = GetSupporterCmdDescription(data.cmd);

            // §33 Tier C：浅紫跑马灯 + 夜晚 cmd=6 闪光由 SupporterMarqueeUI / SupporterNightFlashUI 订阅 OnSupporterAction 接管（§33.6.2/.6.3）
            // BarrageMessageUI 通过 OnPlayerActivityMessage 作日志层
            OnPlayerActivityMessage?.Invoke($"[助威] {displayName} → {cmdDesc}");

            OnSupporterAction?.Invoke(data);
        }

        private void HandleSupporterPromoted(SupporterPromotedData data)
        {
            if (data == null) return;
            var newName = string.IsNullOrEmpty(data.newPlayerName) ? "匿名" : data.newPlayerName;
            var oldName = string.IsNullOrEmpty(data.oldPlayerName) ? "匿名" : data.oldPlayerName;

            // WorkerManager 层的矿工绑定迁移：保留（非 UI 副作用）
            WorkerManager.Instance?.HandleSupporterPromoted(data);

            // §33 Tier C：金黄跑马灯由 SupporterPromotedMarqueeUI 订阅 OnSupporterPromoted 接管（§33.5）
            // BarrageMessageUI 通过 OnPlayerActivityMessage 作日志层
            OnPlayerActivityMessage?.Invoke($"{newName} 替补上场！{oldName} 转为助威");
            OnSupporterPromoted?.Invoke(data);
        }

        private void HandleGiftSilentFail(GiftSilentFailData data)
        {
            if (data == null) return;
            // MVP 占位：服务端（§36.12 未实现时）暂不推送，若收到则仅 Debug.Log。
            // 未来接入 §36.12 后可改为向发送者弹 toast。
            Debug.Log($"[SGM] gift_silent_fail received: giftId={data.giftId} reason={data.reason} unlockDay={data.unlockDay} priceFen={data.priceFen}");
            OnGiftSilentFail?.Invoke(data);
        }

        /// <summary>助威者弹幕效果的中文描述（§33.3 表）</summary>
        private static string GetSupporterCmdDescription(int cmd)
        {
            return cmd switch
            {
                1   => "全局食物+1",
                2   => "全局煤炭+1",
                3   => "全局矿石+1",
                4   => "全局炉温+0.5℃",
                6   => "全员攻击+2%(5s)",
                666 => "全员效率+15%(30s)",
                _   => $"cmd={cmd}"
            };
        }

        // ==================== §24.4 主播事件轮盘（🆕 v1.27）====================

        /// <summary>轮盘充能完成/剩余秒数同步（RouletteUI 订阅后自行处理）
        /// 🔴 audit-r25 GAP-D25-05：data.source 区分"正常 300s 充能完成"vs"§38.3 神秘符文事件瞬间补满"。</summary>
        private void HandleRouletteReady(RouletteReadyData data)
        {
            OnRouletteReady?.Invoke(data);
            // 🔴 audit-r25 GAP-D25-05：mystic_rune 路径加专属跑马灯反馈（付费驱动力差异化）
            if (data.source == "mystic_rune")
            {
                UI.HorizontalMarqueeUI.Instance?.AddMessage(
                    "神秘符文", null, "轮盘充能瞬间补满！");
            }
            Debug.Log($"[SGM] broadcaster_roulette_ready: readyAt={data.readyAt} source={data.source}");
        }

        /// <summary>服务端已定格 cardId，客户端播转轴动画</summary>
        private void HandleRouletteResult(RouletteResultData data)
        {
            // 轮盘启动震屏（轻微 0.1 强度 0.3 秒）
            SurvivalCameraController.Shake(0.1f, 0.3f);
            OnRouletteResult?.Invoke(data);
            Debug.Log($"[SGM] broadcaster_roulette_result: cardId={data.cardId} displayed=[{string.Join(",", data.displayedCards ?? new string[0])}]");
        }

        /// <summary>轮盘效果结束，UI 清理倒计时条</summary>
        private void HandleRouletteEffectEnded(RouletteEffectEndedData data)
        {
            OnRouletteEffectEnded?.Invoke(data);
            Debug.Log($"[SGM] broadcaster_roulette_effect_ended: cardId={data.cardId}");
        }

        /// <summary>神秘商人 30s 限时二选一邀约</summary>
        private void HandleTraderOffer(TraderOfferData data)
        {
            OnTraderOffer?.Invoke(data);
            Debug.Log($"[SGM] broadcaster_trader_offer: expiresAt={data.expiresAt}");
        }

        /// <summary>神秘商人交易结果(整体复核 Critical #3 修复):显示成功/失败反馈并关闭交易面板</summary>
        private void HandleBroadcasterTraderResult(BroadcasterTraderResultData data)
        {
            if (data == null) return;
            string msg;
            if (data.success)
            {
                msg = data.choice == "A"
                    ? "商队交易成功:食物 → 矿石"
                    : "商队交易成功:煤炭 → 城门 HP";
                UI.AnnouncementUI.Instance?.ShowAnnouncement("交易完成", msg, new Color(0.4f, 1f, 0.6f), 2f);
            }
            else
            {
                msg = data.reason == "timeout"
                    ? "商队已离去"
                    : data.reason == "insufficient_resource" ? "资源不足" : data.reason;
                UI.AnnouncementUI.Instance?.ShowAnnouncement("交易取消", msg, new Color(0.8f, 0.8f, 0.8f), 2f);
            }
            OnPlayerActivityMessage?.Invoke(msg);
            Debug.Log($"[SGM] broadcaster_trader_result: success={data.success} choice={data.choice} reason={data.reason}");
        }

        // ==================== §38 探险系统（🆕 v1.27）====================

        /// <summary>
        /// 探险出发：路由到 WorkerManager 隐藏矿工模型、ExpeditionMarkerUI 显示地图边缘图标；
        /// 跑马灯提示 "[玩家名] 出发探险"。
        /// </summary>
        private void HandleExpeditionStarted(ExpeditionStartedData data)
        {
            WorkerManager.Instance?.HandleExpeditionStarted(data);
            OnExpeditionStarted?.Invoke(data);

            string displayName = ResolveDisplayName(data.playerId);
            UI.HorizontalMarqueeUI.Instance?.AddMessage(displayName, null, "出发探险！");
            OnPlayerActivityMessage?.Invoke($"{displayName} 出发探险！");
            Debug.Log($"[SGM] expedition_started: {data.playerId} workerIdx={data.workerIdx} expeditionId={data.expeditionId} returnsAt={data.returnsAt}");
        }

        /// <summary>
        /// 探险外域事件：UI 层显示事件图标/提示；trader_caravan 且有 options 时弹 TraderCaravanUI。
        /// 非 trader_caravan 的事件仅推到 ExpeditionMarkerUI.ShowEvent 做气泡提示（MVP：Log 降级）。
        /// </summary>
        private void HandleExpeditionEvent(ExpeditionEventData data)
        {
            UI.ExpeditionMarkerUI.Instance?.ShowEvent(data);
            OnExpeditionEvent?.Invoke(data);

            // trader_caravan 且 options 非空 → 显示接受/取消面板
            if (data.eventId == "trader_caravan" && data.options != null && data.options.Length > 0)
            {
                if (UI.TraderCaravanUI.Instance != null)
                {
                    UI.TraderCaravanUI.Instance.Show(data);
                }
                else
                {
                    // 降级：TraderCaravanUI 未绑定时用 AnnouncementUI 提示（主播仍可走弹幕路径）
                    UI.AnnouncementUI.Instance?.ShowAnnouncement(
                        "商队交易",
                        "主播：弹幕 \"accept 接受\" / \"cancel 拒绝\"",
                        new Color(1f, 0.85f, 0.1f),
                        4f);
                    Debug.LogWarning("[SGM] expedition_event trader_caravan：TraderCaravanUI.Instance 为 null，已降级到 AnnouncementUI");
                }
            }

            Debug.Log($"[SGM] expedition_event: expeditionId={data.expeditionId} eventId={data.eventId} eventEndsAt={data.eventEndsAt} options=[{(data.options == null ? "" : string.Join(",", data.options))}]");
        }

        /// <summary>
        /// 探险返回：路由到 WorkerManager 恢复矿工显示（或切 Dead）；跑马灯显示 outcome 文案。
        /// </summary>
        private void HandleExpeditionReturned(ExpeditionReturnedData data)
        {
            WorkerManager.Instance?.HandleExpeditionReturned(data);
            OnExpeditionReturned?.Invoke(data);

            string displayName = ResolveDisplayName(data.playerId);
            string outcomeText = FormatExpeditionOutcome(data.outcome);
            UI.HorizontalMarqueeUI.Instance?.AddMessage(displayName, null, outcomeText);
            OnPlayerActivityMessage?.Invoke($"{displayName} 探险归来：{outcomeText}");
            Debug.Log($"[SGM] expedition_returned: {data.playerId} expeditionId={data.expeditionId} outcome.type={data.outcome?.type} died={data.outcome?.died}");
        }

        /// <summary>
        /// 探险被拒：跑马灯 + 活动消息显示失败原因（本地化）。
        /// </summary>
        private void HandleExpeditionFailed(ExpeditionFailedData data)
        {
            OnExpeditionFailed?.Invoke(data);
            string displayName = ResolveDisplayName(data.playerId);
            string reasonText  = FormatExpeditionFailReason(data.reason, data.unlockDay);
            UI.HorizontalMarqueeUI.Instance?.AddMessage(displayName, null, $"探险失败：{reasonText}");
            OnPlayerActivityMessage?.Invoke($"{displayName} 探险失败：{reasonText}");
            Debug.Log($"[SGM] expedition_failed: {data.playerId} reason={data.reason} unlockDay={data.unlockDay}");
        }

        /// <summary>
        /// 格式化 outcome 文案：
        ///   success → "获得 矿石80 煤炭50"
        ///   died    → "外域阵亡"
        ///   empty   → "空手而归"
        ///   safe    → "平安归来"（audit-r23 GAP-E23-03 注释精确化：仅在 §34.4 E3b 不朽证明 _consumeFreeDeathPass
        ///            消费免死豁免救回时 emit；服务端 SurvivalGameEngine.js:8935 单点。
        ///            r21 注释误标"_wildBeasts/_meteor/_mysticRune 无事件分支" — 实际这些分支全 emit success/died/empty，
        ///            只有夜晚兜底死亡且玩家持有免死豁免（unique_proof_immortal）才 emit safe）
        /// </summary>
        private static string FormatExpeditionOutcome(ExpeditionOutcome outcome)
        {
            if (outcome == null) return "归来";
            if (outcome.died)    return "外域阵亡";

            if (outcome.type == "died")  return "外域阵亡";
            if (outcome.type == "empty") return "空手而归";
            if (outcome.type == "safe")  return "平安归来";

            // success: 组合 resources + contributions
            var sb = new System.Text.StringBuilder();
            if (outcome.resources != null)
            {
                if (outcome.resources.food > 0) sb.Append($"食物+{outcome.resources.food} ");
                if (outcome.resources.coal > 0) sb.Append($"煤炭+{outcome.resources.coal} ");
                if (outcome.resources.ore  > 0) sb.Append($"矿石+{outcome.resources.ore} ");
            }
            if (outcome.contributions > 0) sb.Append($"贡献+{outcome.contributions}");

            string result = sb.ToString().Trim();
            return string.IsNullOrEmpty(result) ? "归来" : $"获得 {result}";
        }

        /// <summary>探险失败原因枚举 → 中文描述（§38.5 表）</summary>
        private static string FormatExpeditionFailReason(string reason, int unlockDay)
        {
            switch (reason)
            {
                case "max_concurrent":        return "同时探险上限 3 人";
                case "wrong_phase":           return "只能在白天发起探险";
                case "worker_dead":           return "矿工当前已阵亡";
                case "duplicate":             return "该矿工已在探险中";
                case "supporter_not_allowed": return "助威者无法派出探险";
                case "season_ending":         return "赛季即将结束";
                case "feature_locked":        return unlockDay > 0 ? $"功能未解锁（需第{unlockDay}天后）" : "功能未解锁";
                default:                      return reason;
            }
        }

        /// <summary>根据 playerId 从 WorkerManager 找出昵称；找不到则回显 id 缩写。</summary>
        private static string ResolveDisplayName(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return "匿名";
            var wm = WorkerManager.Instance;
            if (wm != null)
            {
                var workers = wm.ActiveWorkers;
                if (workers != null)
                {
                    foreach (var w in workers)
                    {
                        if (w != null && w.PlayerId == playerId)
                            return string.IsNullOrEmpty(w.PlayerName) ? playerId : w.PlayerName;
                    }
                }
            }
            return playerId;
        }

        // ==================== §37 建造系统 ====================

        private void HandleBuildVoteStarted(BuildVoteStartedData data)
        {
            OnBuildVoteStarted?.Invoke(data);
            UI.BuildVoteUI.Instance?.ShowVote(data);
            UI.HorizontalMarqueeUI.Instance?.AddMessage(data.proposerName, null, "发起建造投票");
            OnPlayerActivityMessage?.Invoke($"{data.proposerName} 发起建造投票");
        }

        private void HandleBuildVoteUpdate(BuildVoteUpdateData data)
        {
            OnBuildVoteUpdate?.Invoke(data);
            UI.BuildVoteUI.Instance?.UpdateVoteCounts(data);
        }

        private void HandleBuildVoteEnded(BuildVoteEndedData data)
        {
            OnBuildVoteEnded?.Invoke(data);
            UI.BuildVoteUI.Instance?.CloseVote(data);
            string winText = string.IsNullOrEmpty(data.winnerId)
                ? $"投票流产（{data.totalVoters} 票）"
                : $"投票通过 → {GetBuildingChineseName(data.winnerId)}（{data.totalVoters} 票）";
            OnPlayerActivityMessage?.Invoke(winText);
        }

        private void HandleBuildStarted(BuildStartedData data)
        {
            OnBuildStarted?.Invoke(data);
            string name = GetBuildingChineseName(data.buildId);
            UI.HorizontalMarqueeUI.Instance?.AddMessage("建造", null, $"{name} 开工");
            OnPlayerActivityMessage?.Invoke($"{name} 开工");
            Debug.Log($"[SGM] build_started: {data.buildId} completesAt={data.completesAt}");
        }

        /// <summary>§37 建造进度心跳：仅透传事件，UI（BuildingStatusPanelUI）自己订阅刷新百分比。</summary>
        private void HandleBuildProgress(BuildProgressData data)
        {
            OnBuildProgress?.Invoke(data);
        }

        private void HandleBuildCompleted(BuildCompletedData data)
        {
            OnBuildCompleted?.Invoke(data);
            string name = GetBuildingChineseName(data.buildId);
            UI.AnnouncementUI.Instance?.ShowAnnouncement($"{name} 建成！", null,
                new Color(0.4f, 1f, 0.6f), 3f);
            UI.HorizontalMarqueeUI.Instance?.AddMessage("建造", null, $"{name} 建成");
            OnPlayerActivityMessage?.Invoke($"{name} 建成");
        }

        private void HandleBuildDemolished(BuildDemolishedData data)
        {
            OnBuildDemolished?.Invoke(data);
            string name = GetBuildingChineseName(data.buildId);
            Debug.Log($"[SGM] build_demolished: {data.buildId} reason={data.reason}");
            OnPlayerActivityMessage?.Invoke($"{name} 已拆除（{data.reason}）");
        }

        private void HandleBuildProposeFailed(BuildProposeFailedData data)
        {
            OnBuildProposeFailed?.Invoke(data);
            string reasonText = FormatBuildFailReason(data.reason, data.unlockDay);
            Debug.Log($"[SGM] build_propose_failed: {data.reason} unlockDay={data.unlockDay}");
            OnPlayerActivityMessage?.Invoke($"发起建造失败：{reasonText}");
        }

        private void HandleBuildCancelled(BuildCancelledData data)
        {
            OnBuildCancelled?.Invoke(data);
            string name = GetBuildingChineseName(data.buildId);
            OnPlayerActivityMessage?.Invoke($"{name} 建造取消（{data.reason}）");
        }

        private void HandleBuildingDemolishedBatch(BuildingDemolishedBatchData data)
        {
            OnBuildingDemolishedBatch?.Invoke(data);
            if (data.buildingIds == null || data.buildingIds.Length == 0) return;
            var names = new System.Text.StringBuilder();
            for (int i = 0; i < data.buildingIds.Length; i++)
            {
                if (i > 0) names.Append("、");
                names.Append(GetBuildingChineseName(data.buildingIds[i]));
            }
            OnPlayerActivityMessage?.Invoke($"降级失守，建筑拆除：{names}");
        }

        private void HandleMonsterWaveIncoming(MonsterWaveIncomingData data)
        {
            OnMonsterWaveIncoming?.Invoke(data);
            // 🔴 audit-r25 GAP-E25-01：r24 加 leadSec 字段但 UI 文案硬编码 "10 秒"
            //   修复：读 data.leadSec（watchtower=10 / emergency_alert=10/30）+ 30s 路径区分文案
            int leadSec = data.leadSec > 0 ? data.leadSec : 10;
            string title = leadSec >= 30 ? "紧急警报" : "即将来袭";
            string body  = leadSec >= 30
                ? $"紧急警报：{leadSec} 秒后怪物出现"
                : $"瞭望塔预警：{leadSec} 秒后怪物出现";
            UI.AnnouncementUI.Instance?.ShowAnnouncement(title, body,
                new Color(1f, 0.85f, 0.1f), 3f);
        }

        /// <summary>建筑 ID → 中文名（§37.2 表）</summary>
        private static string GetBuildingChineseName(string buildId)
        {
            switch (buildId)
            {
                case "watchtower": return "瞭望塔";
                case "market":     return "市场";
                case "hospital":   return "医院";
                case "altar":      return "祭坛";
                case "beacon":     return "烽火台";
                default:           return buildId;
            }
        }

        /// <summary>建造失败原因 → 中文（§37.3 + §19.2）</summary>
        private static string FormatBuildFailReason(string reason, int unlockDay)
        {
            switch (reason)
            {
                case "insufficient_resources": return "资源不足";
                case "daily_limit":            return "今日已投过票";
                case "wrong_phase":            return "仅白天可投票";
                case "already_built":          return "该建筑已建造";
                case "already_voting":         return "已有投票进行中";
                case "supporter_not_allowed":  return "助威者无法发起";
                case "season_ending":          return "赛季即将结束";
                case "feature_locked":         return unlockDay > 0 ? $"功能未解锁（需第{unlockDay}天后）" : "功能未解锁";
                default:                       return reason;
            }
        }

        // ==================== §39 商店系统（🆕 v1.27） ====================

        /// <summary>商品清单应答：路由到 ShopUI 渲染 A/B Tab 内容</summary>
        private void HandleShopListData(ShopListData data)
        {
            OnShopListData?.Invoke(data);
            UI.ShopUI.Instance?.PopulateList(data);
            Debug.Log($"[SGM] shop_list_data: category={data.category} items={(data.items == null ? 0 : data.items.Length)}");
        }

        /// <summary>B 类 ≥1000 主播 HUD 购买：弹双确认弹窗（5s TTL 倒计时）</summary>
        private void HandleShopPurchaseConfirmPrompt(ShopPurchaseConfirmPromptData data)
        {
            OnShopPurchaseConfirmPrompt?.Invoke(data);
            UI.ShopConfirmDialogUI.Instance?.Show(data);
            Debug.Log($"[SGM] shop_purchase_confirm_prompt: pendingId={data.pendingId} itemId={data.itemId} price={data.price} expiresAt={data.expiresAt}");
        }

        /// <summary>购买成功房间广播：跑马灯 + 活动消息；A 类由 shop_effect_triggered 另行处理视觉</summary>
        private void HandleShopPurchaseConfirm(ShopPurchaseConfirmData data)
        {
            OnShopPurchaseConfirm?.Invoke(data);

            string buyerName = string.IsNullOrEmpty(data.playerName) ? (string.IsNullOrEmpty(data.playerId) ? "匿名" : data.playerId) : data.playerName;
            string itemDisplay = GetShopItemDisplayName(data.itemId);
            UI.HorizontalMarqueeUI.Instance?.AddMessage(buyerName, null, $"购得 {itemDisplay}");
            OnPlayerActivityMessage?.Invoke($"{buyerName} 购得 {itemDisplay}");
            Debug.Log($"[SGM] shop_purchase_confirm: {data.playerId} itemId={data.itemId} category={data.category} remContrib={data.remainingContrib} remBal={data.remainingBalance}");
        }

        /// <summary>购买失败（unicast）：MVP 仅 Log + 活动消息提示发起方失败原因</summary>
        private void HandleShopPurchaseFailed(ShopPurchaseFailedData data)
        {
            OnShopPurchaseFailed?.Invoke(data);
            string reasonText = FormatShopFailReason(data.reason, data.unlockDay, data.minLifetimeContrib);
            string itemDisplay = GetShopItemDisplayName(data.itemId);
            OnPlayerActivityMessage?.Invoke($"购买 {itemDisplay} 失败：{reasonText}");
            Debug.Log($"[SGM] shop_purchase_failed: itemId={data.itemId} reason={data.reason} unlockDay={data.unlockDay} minLifetimeContrib={data.minLifetimeContrib}");
        }

        /// <summary>装备切换成功（unicast）：更新本地 MyEquipped 缓存 + 通知 UI 刷新</summary>
        private void HandleShopEquipChanged(ShopEquipChangedData data)
        {
            // 更新本地装备缓存（当服务器下发的是自己时）
            if (MyEquipped == null) MyEquipped = new ShopEquipped();
            switch (data.slot)
            {
                case "title":    MyEquipped.title    = data.itemId; break;
                case "frame":    MyEquipped.frame    = data.itemId; break;
                case "entrance": MyEquipped.entrance = data.itemId; break;
                case "barrage":  MyEquipped.barrage  = data.itemId; break;
            }
            OnShopEquipChanged?.Invoke(data);
            Debug.Log($"[SGM] shop_equip_changed: {data.playerId} slot={data.slot} itemId={data.itemId}");
        }

        /// <summary>装备切换失败（unicast）：r13 GAP-A1 — 加 toast 反馈（之前仅 Debug.Log，用户无感知）。
        /// 复用 FailureToastLocale 中央化映射（slot_mismatch / not_owned / too_frequent 已覆盖 §17.17）。</summary>
        private void HandleShopEquipFailed(ShopEquipFailedData data)
        {
            OnShopEquipFailed?.Invoke(data);
            string reasonText  = DrscfZ.UI.FailureToastLocale.Get(data?.reason);
            string itemDisplay = string.IsNullOrEmpty(data?.itemId) ? "" : GetShopItemDisplayName(data.itemId);
            string msg = string.IsNullOrEmpty(itemDisplay)
                ? $"装备失败：{reasonText}"
                : $"装备 {itemDisplay} 失败：{reasonText}";
            OnPlayerActivityMessage?.Invoke(msg);
            Debug.Log($"[SGM] shop_equip_failed: slot={data?.slot} itemId={data?.itemId} reason={data?.reason}");
        }

        /// <summary>进房/重连时的背包 + 装备快照：路由到 ShopUI 渲染；若是自己则更新本地缓存</summary>
        private void HandleShopInventoryData(ShopInventoryData data)
        {
            OnShopInventoryData?.Invoke(data);

            // 更新本地缓存（若是自己的数据）：MVP 暂以 data.playerId 为判据，自身 id 由上层上下文决定
            if (data.equipped != null)
                MyEquipped = data.equipped;

            UI.ShopUI.Instance?.UpdateInventory(data);
            Debug.Log($"[SGM] shop_inventory_data: {data.playerId} owned={(data.owned == null ? 0 : data.owned.Length)}");
        }

        /// <summary>A 类效果触发：按 itemId dispatch 到对应视觉/行为模块</summary>
        private void HandleShopEffectTriggered(ShopEffectTriggeredData data)
        {
            OnShopEffectTriggered?.Invoke(data);
            if (data == null || string.IsNullOrEmpty(data.itemId)) return;

            string sourceName = string.IsNullOrEmpty(data.sourcePlayerName)
                ? (string.IsNullOrEmpty(data.sourcePlayerId) ? "匿名" : data.sourcePlayerId)
                : data.sourcePlayerName;

            switch (data.itemId)
            {
                case "worker_pep_talk":
                {
                    float dur = data.durationSec > 0 ? data.durationSec : 30f;
                    WorkerManager.Instance?.ActivateAllWorkersGlow(dur);
                    UI.AnnouncementUI.Instance?.ShowAnnouncement(
                        "工地喊话！",
                        $"全员矿工效率+15%（{Mathf.RoundToInt(dur)}s）",
                        new Color(1f, 0.85f, 0.1f), 2f);
                    break;
                }
                case "gate_quickpatch":
                {
                    int before = data.metadata != null ? data.metadata.gateHpBefore : 0;
                    int after  = data.metadata != null ? data.metadata.gateHpAfter  : 0;
                    int delta  = after - before;
                    string subText = delta > 0 ? $"+{delta} HP" : "城门血量回复";
                    UI.AnnouncementUI.Instance?.ShowAnnouncement(
                        "城门已修补",
                        subText,
                        new Color(0.3f, 1f, 0.5f), 2f);
                    break;
                }
                case "emergency_alert":
                {
                    int leadSec = data.metadata != null ? data.metadata.leadSec : 10;
                    UI.AnnouncementUI.Instance?.ShowAnnouncement(
                        "预警来袭！",
                        $"{leadSec} 秒后出怪",
                        new Color(1f, 0.3f, 0.3f), 3f);
                    break;
                }
                case "spotlight":
                {
                    // targetPlayerId 通常 == sourcePlayerId；UI 高亮/弹幕星标等效果由后续 LiveRankingUI 或 BarrageMessageUI 消费时自行识别
                    float dur = data.durationSec > 0 ? data.durationSec : 10f;
                    // MVP 占位：Log + 跑马灯提示；具体 LiveRankingUI 金色高亮留待后续视觉任务实现
                    UI.HorizontalMarqueeUI.Instance?.AddMessage(sourceName, null, $"成为聚光灯焦点（{Mathf.RoundToInt(dur)}s）");
                    Debug.Log($"[SGM] shop_effect_triggered spotlight: source={data.sourcePlayerId} target={data.targetPlayerId} duration={dur}s (占位：UI 高亮效果待视觉任务实现)");
                    break;
                }
                default:
                    Debug.Log($"[SGM] shop_effect_triggered: 未处理 itemId={data.itemId}");
                    break;
            }
        }

        /// <summary>
        /// §39 itemId → 中文显示名（用于跑马灯/toast/活动消息）。
        /// 未知 id 直接回显原文，保证不崩溃。
        /// </summary>
        public static string GetShopItemDisplayName(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return "未知商品";
            switch (itemId)
            {
                // A 类
                case "worker_pep_talk":   return "工地喊话";
                case "gate_quickpatch":   return "快速修补";
                case "emergency_alert":   return "紧急警报";
                case "spotlight":         return "聚光灯";
                // B 类固定 8 款
                case "title_supporter":     return "守护者称号";
                case "title_veteran":       return "老兵称号";
                case "title_legend_mover":  return "大善人称号";
                case "frame_bronze":        return "青铜头像框";
                case "frame_silver":        return "白银头像框";
                case "entrance_spark":      return "火花入场";
                case "barrage_glow":        return "弹幕发光";
                case "barrage_crown":       return "皇冠弹幕";
                default:                    return itemId; // 限定 SKU / 未知 id 回显原文
            }
        }

        /// <summary>§39.11 失败原因枚举 → 中文描述（MVP 仅用于活动消息/Log）</summary>
        private static string FormatShopFailReason(string reason, int unlockDay, int minLifetimeContrib)
        {
            if (string.IsNullOrEmpty(reason)) return "未知原因";
            switch (reason)
            {
                case "insufficient":           return "贡献/余额不足";
                case "wrong_phase":            return "当前阶段不可购买";
                case "no_effect":              return "无需使用（已满）";
                case "feature_locked":         return unlockDay > 0 ? $"功能未解锁（需第{unlockDay}天后）" : "功能未解锁";
                case "item_not_found":         return "商品不存在";
                case "already_owned":          return "已拥有";
                case "pending_expired":        return "确认超时";
                case "pending_invalid":        return "确认凭证无效";
                case "limit_exceeded":         return "本局限购";
                case "supporter_not_allowed":  return "助威者不可购买战术道具";
                case "not_unlocked_yet":
                    if (minLifetimeContrib > 0) return $"需累计 {minLifetimeContrib} 贡献";
                    if (unlockDay > 0)          return $"赛季第 {unlockDay} 天解锁";
                    return "尚未解锁";
                case "season_locked":          return "赛季末 5 分钟不可购买 B 类";
                case "spotlight_active":       return "聚光灯已激活";
                case "per_game_limit":         return "本局该商品已达上限";
                default:                       return reason;
            }
        }

        // ==================== §35 跨直播间攻防战（🆕 v1.27） ====================

        /// <summary>大厅列表应答：路由到 TribeWarLobbyUI.PopulateList。</summary>
        private void HandleTribeWarRoomListResult(TribeWarRoomListResultData data)
        {
            OnTribeWarRoomListResult?.Invoke(data);
            UI.TribeWarLobbyUI.Instance?.PopulateList(data);
            int n = data.rooms == null ? 0 : data.rooms.Length;
            Debug.Log($"[SGM] tribe_war_room_list_result: rooms={n}");
        }

        /// <summary>攻击/反击失败（unicast 发起方）：跑马灯 + 活动消息。
        /// 🔴 audit-r25 GAP-D25-04：cooldownMs 字段定义但之前 FormatTribeWarFailReason 0 消费 → 显示静态"冷却中（60s）"。
        ///   修复：FormatTribeWarFailReason 加 cooldownMs 参数，in_cooldown 时拼接实时倒计时秒数。</summary>
        private void HandleTribeWarAttackFailed(TribeWarAttackFailedData data)
        {
            OnTribeWarAttackFailed?.Invoke(data);
            string reasonText = FormatTribeWarFailReason(data.reason, data.cooldownMs);
            OnPlayerActivityMessage?.Invoke($"攻防战操作失败：{reasonText}");
            UI.HorizontalMarqueeUI.Instance?.AddMessage("攻防战", null, reasonText);
            Debug.Log($"[SGM] tribe_war_attack_failed: reason={data.reason} cooldownMs={data.cooldownMs}");
        }

        /// <summary>攻击开始广播（双方房间均广播）：
        /// MVP 阶段不区分攻/守视角（缺少自身 roomId 上下文）——两边都打开对应状态面板。
        /// 人工 QA 可根据场景选用适合视角（攻击方/防守方面板之一）。</summary>
        private void HandleTribeWarAttackStarted(TribeWarAttackStartedData data)
        {
            OnTribeWarAttackStarted?.Invoke(data);
            UI.TribeWarAttackStatusPanel.Instance?.Show(data);
            OnPlayerActivityMessage?.Invoke($"【攻防战】{data.attackerStreamerName} → {data.defenderStreamerName}");
            UI.HorizontalMarqueeUI.Instance?.AddMessage(
                data.attackerStreamerName, null, $"发起攻防战 → {data.defenderStreamerName}");
            Debug.Log($"[SGM] tribe_war_attack_started: sessionId={data.sessionId} atk={data.attackerStreamerName} def={data.defenderStreamerName}");
        }

        /// <summary>被攻击通知（仅防守方房间广播）：展示防守方状态面板 + 顶部飘字告警。</summary>
        private void HandleTribeWarUnderAttack(TribeWarUnderAttackData data)
        {
            OnTribeWarUnderAttack?.Invoke(data);
            UI.TribeWarDefenseStatusPanel.Instance?.Show(data);
            UI.TopFloatingTextUI.Instance?.ShowDanger($"【被攻击】{data.attackerStreamerName} 发起攻势！");
            OnPlayerActivityMessage?.Invoke($"【被攻击】{data.attackerStreamerName} 发起攻势");
            Debug.Log($"[SGM] tribe_war_under_attack: sessionId={data.sessionId} atk={data.attackerStreamerName}");
        }

        /// <summary>远征怪已派出（仅攻击方房间广播）：更新攻击状态面板数字。</summary>
        private void HandleTribeWarExpeditionSent(TribeWarExpeditionSentData data)
        {
            OnTribeWarExpeditionSent?.Invoke(data);
            UI.TribeWarAttackStatusPanel.Instance?.UpdateExpeditionCount(data.count);
            UI.TribeWarAttackStatusPanel.Instance?.UpdateEnergy(data.remainingEnergy);
            Debug.Log($"[SGM] tribe_war_expedition_sent: sessionId={data.sessionId} count={data.count} remainingEnergy={data.remainingEnergy}");
        }

        /// <summary>远征怪来袭（仅防守方房间广播）：调 MonsterWaveSpawner 刷红色远征怪 + 更新防守面板 + 顶部飘字。</summary>
        private void HandleTribeWarExpeditionIncoming(TribeWarExpeditionIncomingData data)
        {
            OnTribeWarExpeditionIncoming?.Invoke(data);
            if (monsterWaveSpawner != null)
            {
                monsterWaveSpawner.SpawnTribeWarExpedition(data.count, data.attackerStreamerName);
            }
            else if (MonsterWaveSpawner.Instance != null)
            {
                MonsterWaveSpawner.Instance.SpawnTribeWarExpedition(data.count, data.attackerStreamerName);
            }
            else
            {
                Debug.LogWarning("[SGM] tribe_war_expedition_incoming: MonsterWaveSpawner 为 null，远征怪无法生成");
            }
            UI.TribeWarDefenseStatusPanel.Instance?.UpdateExpeditionCount(data.count);
            UI.TopFloatingTextUI.Instance?.ShowDanger($"【远征怪】{data.count} 只来袭！");
            Debug.Log($"[SGM] tribe_war_expedition_incoming: count={data.count} atk={data.attackerStreamerName}");
        }

        /// <summary>攻击方战报：追加到攻击状态面板的战报滚动区。</summary>
        private void HandleTribeWarCombatReport(TribeWarCombatReportData data)
        {
            OnTribeWarCombatReport?.Invoke(data);
            string line = FormatTribeWarCombatLine(data);
            UI.TribeWarAttackStatusPanel.Instance?.AppendReport(line);

            // 若战报含 resource_stolen 事件，把累计值同步给面板。
            // MVP 下累计值由后续 tribe_war_attack_ended 最终给出；中途仅靠 detail 文字。
            Debug.Log($"[SGM] tribe_war_combat_report: event={data.eventName} detail={data.detail}");
        }

        /// <summary>防守方战报：追加到防守状态面板的战报滚动区。</summary>
        private void HandleTribeWarCombatReportDefense(TribeWarCombatReportData data)
        {
            OnTribeWarCombatReportDefense?.Invoke(data);
            string line = FormatTribeWarCombatLine(data);
            UI.TribeWarDefenseStatusPanel.Instance?.AppendReport(line);
            Debug.Log($"[SGM] tribe_war_combat_report_defense: event={data.eventName} detail={data.detail}");
        }

        /// <summary>攻击结束（双方房间广播）：关闭攻/守状态面板、展示总结、同步偷取汇总。</summary>
        private void HandleTribeWarAttackEnded(TribeWarAttackEndedData data)
        {
            OnTribeWarAttackEnded?.Invoke(data);
            UI.TribeWarAttackStatusPanel.Instance?.UpdateStolen(data.stolenFood, data.stolenCoal, data.stolenOre);
            UI.TribeWarDefenseStatusPanel.Instance?.UpdateStolen(data.stolenFood, data.stolenCoal, data.stolenOre);

            string reasonText = FormatTribeWarEndReason(data.reason);
            string summary = $"【攻防战结束】{reasonText} 偷取 食物+{data.stolenFood} 煤炭+{data.stolenCoal} 矿石+{data.stolenOre}";
            UI.HorizontalMarqueeUI.Instance?.AddMessage("攻防战", null, summary);
            OnPlayerActivityMessage?.Invoke(summary);

            UI.TribeWarAttackStatusPanel.Instance?.Hide();
            UI.TribeWarDefenseStatusPanel.Instance?.Hide();
            Debug.Log($"[SGM] tribe_war_attack_ended: sessionId={data.sessionId} reason={data.reason} stolen=({data.stolenFood},{data.stolenCoal},{data.stolenOre})");
        }

        /// <summary>§35.10 attack_failed.reason → 中文（MVP 仅活动消息/跑马灯用）
        /// 🔴 audit-r25 GAP-D25-04：加 cooldownMs 参数，in_cooldown 时显示实时倒计时秒数（替代静态"60s"）。</summary>
        private static string FormatTribeWarFailReason(string reason, long cooldownMs = 0)
        {
            if (string.IsNullOrEmpty(reason)) return "未知原因";
            switch (reason)
            {
                // r15 GAP-D-MAJOR-04：服务端 TribeWarManager 实发 self_target/attacker_busy/target_busy（非 cannot_attack_self/already_attacking/target_already_under_attack）
                // 旧 case 保留作向后兼容（若有上游统一改名再清理）
                case "self_target":                 return "不能攻击自己";
                case "attacker_busy":               return "已在攻击目标";
                case "target_busy":                 return "目标已被其他房间攻击";
                case "cannot_attack_self":          return "不能攻击自己";
                case "in_cooldown":
                    // 🔴 audit-r25 GAP-D25-04：实时倒计时秒数（cooldownMs 0 时回退静态文案）
                    return cooldownMs > 0
                        ? $"冷却中（剩余 {Mathf.CeilToInt(cooldownMs / 1000f)}s）"
                        : "冷却中（60s）";
                case "already_attacking":           return "已在攻击目标";
                case "target_already_under_attack": return "目标已被其他房间攻击";
                case "target_unavailable":          return "目标房间不可攻击";
                case "target_not_playing":          return "目标房间未在游戏中";
                case "not_under_attack":            return "未被攻击，无法反击";
                case "wrong_phase":                 return "当前阶段不允许发起";
                case "feature_locked":              return "功能未解锁";
                case "room_not_found":              return "目标房间不存在";
                default:                            return reason;
            }
        }

        /// <summary>§35.10 attack_ended.reason → 中文（r15 GAP-D-MAJOR-05：与服务端 TribeWarManager.js:180 / TribeWarSession.js:58 实发 reason 对齐）
        /// 🔴 audit-r25 GAP-D25-08：补 'room_destroyed' case（一方主播退出 onRoomDestroyed 触发，之前显示原文 "结束: room_destroyed"）。</summary>
        private static string FormatTribeWarEndReason(string reason)
        {
            if (string.IsNullOrEmpty(reason)) return "结束";
            switch (reason)
            {
                // r15 GAP-D-MAJOR-05：服务端实发 manual / no_energy / settlement / season_reset
                case "manual":              return "手动停止";
                case "no_energy":           return "3 分钟无能量自动断开";
                case "settlement":          return "游戏结算";
                case "season_reset":        return "赛季切换";
                case "room_destroyed":      return "对方主播离开";  // 🔴 audit-r25 GAP-D25-08
                // 旧 case 保留作向后兼容
                case "manual_stop":         return "手动停止";
                case "zero_energy_timeout": return "3 分钟无能量自动断开";
                case "game_ended":          return "游戏结算";
                case "season_ended":        return "赛季切换";
                default:                    return reason;
            }
        }

        /// <summary>战报格式化：优先使用 detail；若 detail 为空，退化为 eventName 本地化。</summary>
        private static string FormatTribeWarCombatLine(TribeWarCombatReportData data)
        {
            if (data == null) return "";
            if (!string.IsNullOrEmpty(data.detail)) return data.detail;
            switch (data.eventName)
            {
                case "damage":             return "造成伤害";
                case "worker_killed":      return "击杀矿工";
                case "gate_hit":           return "击中城门";
                case "expedition_killed":  return "远征怪阵亡";
                case "resource_stolen":    return "偷取资源";
                case "gate_bonus":         return "保底城门加成";
                default:                   return data.eventName ?? "";
            }
        }

        // ==================== §36 全服同步 + 赛季制（🆕 v1.27 MVP） ====================

        /// <summary>world_clock_tick：更新顶部计时器+缓存 phase/seasonDay/themeId/phaseRemainingSec。
        /// 1Hz 广播，避免在此做昂贵操作（仅刷 UI 缓存 + 事件分发）。</summary>
        private void HandleWorldClockTick(WorldClockTickData data)
        {
            if (data == null) return;
            if (CurrentSeasonState == null) CurrentSeasonState = new SeasonRuntimeState();
            CurrentSeasonState.phase             = data.phase;
            CurrentSeasonState.phaseRemainingSec = data.phaseRemainingSec;
            CurrentSeasonState.seasonDay         = data.seasonDay;
            CurrentSeasonState.themeId           = data.themeId;
            CurrentSeasonState.seasonId          = data.seasonId;

            // 通知 UI：SurvivalTopBarUI 可拉取 CurrentSeasonState 显示"赛季 D%/主题"；
            // SeasonThemeUI 不由 tick 驱动（避免每秒闪），仅由 season_started 驱动。
            OnWorldClockTick?.Invoke(data);

            // 🆕 v1.27 §36.12 分时段解锁：seasonDay N→N+1 递增那一秒携带新解锁功能列表
            //   合并入本地缓存 + 触发 FeatureUnlockBanner 横幅；其他 tick 字段为 null/空。
            if (data.newlyUnlockedFeatures != null && data.newlyUnlockedFeatures.Length > 0)
            {
                MergeNewlyUnlockedFeatures(data.newlyUnlockedFeatures);
                OnNewlyUnlockedFeatures?.Invoke(data.newlyUnlockedFeatures);
                Debug.Log($"[SGM] world_clock_tick carries newlyUnlockedFeatures: {string.Join(",", data.newlyUnlockedFeatures)}");
            }
        }

        /// <summary>season_state：连接/主动请求时推送的赛季状态快照。更新缓存 + 触发主题切换。</summary>
        private void HandleSeasonState(SeasonStateData data)
        {
            if (data == null) return;
            if (CurrentSeasonState == null) CurrentSeasonState = new SeasonRuntimeState();

            int prevSeasonId = CurrentSeasonState.seasonId;
            string prevThemeId = CurrentSeasonState.themeId;

            CurrentSeasonState.seasonId  = data.seasonId;
            CurrentSeasonState.seasonDay = data.seasonDay;
            CurrentSeasonState.themeId   = data.themeId;

            OnSeasonState?.Invoke(data);

            // 🆕 v1.27 §36.12 分时段解锁：携带截至当前 seasonDay 的全集；用于中途进场客户端初始化按钮锁态
            if (data.unlockedFeatures != null)
                SyncUnlockedFeatures(data.unlockedFeatures);

            // 若主题变化 → 通知 SeasonThemeUI 应用横幅颜色（MVP 极简：仅颜色切换，无粒子/天空盒）
            if (!string.IsNullOrEmpty(data.themeId) && data.themeId != prevThemeId)
            {
                UI.SeasonThemeUI.Instance?.ApplyTheme(data.themeId);
            }

            Debug.Log($"[SGM] season_state: seasonId={data.seasonId} seasonDay={data.seasonDay} themeId={data.themeId} unlockedCount={(data.unlockedFeatures == null ? 0 : data.unlockedFeatures.Length)} (prev seasonId={prevSeasonId} themeId={prevThemeId})");
        }

        /// <summary>fortress_day_changed：堡垒日变更 → FortressDayBadgeUI 刷新 + 跑马灯提示。
        /// reason 枚举：'promoted'/'demoted'/'newbie_protected'/'cap_blocked'/'cap_reset'。</summary>
        private void HandleFortressDayChanged(FortressDayChangedData data)
        {
            if (data == null) return;

            UI.FortressDayBadgeUI.Instance?.UpdateFortressDay(data);

            string marqueeText = data.reason switch
            {
                "promoted"         => $"堡垒日晋级 Lv.{data.newFortressDay}！",
                "demoted"          => $"堡垒日降级 {data.oldFortressDay}→{data.newFortressDay}",
                "newbie_protected" => $"新手保护：堡垒日保持 Lv.{data.newFortressDay}",
                "cap_blocked"      => $"今日闯关已达上限 {data.dailyCapMax}！次日 05:00（北京时间）重置",
                "cap_reset"        => "今日闯关已重置，继续挑战！",
                _                  => $"堡垒日变更：{data.reason} → Lv.{data.newFortressDay}"
            };
            UI.HorizontalMarqueeUI.Instance?.AddMessage("堡垒日", null, marqueeText);
            OnPlayerActivityMessage?.Invoke(marqueeText);

            OnFortressDayChanged?.Invoke(data);
            Debug.Log($"[SGM] fortress_day_changed: {data.oldFortressDay}→{data.newFortressDay} reason={data.reason} seasonDay={data.seasonDay} daily={data.dailyFortressDayGained}/{data.dailyCapMax} capBlocked={data.dailyCapBlocked}");
        }

        /// <summary>room_failed：与 fortress_day_changed 同帧推送，展示失败降级信息。</summary>
        private void HandleRoomFailed(RoomFailedData data)
        {
            if (data == null) return;
            UI.RoomFailedBannerUI.Instance?.Show(data);
            OnRoomFailed?.Invoke(data);
            Debug.Log($"[SGM] room_failed: {data.oldFortressDay}→{data.newFortressDay} reason={data.demotionReason} newbieProtected={data.newbieProtected}");
        }

        /// <summary>season_started：新赛季开始 → AnnouncementUI 横幅 + 主题视觉</summary>
        private void HandleSeasonStarted(SeasonStartedData data)
        {
            if (data == null) return;

            // 更新缓存（若 world_clock_tick 尚未到达 season_started 的 tick 节拍）
            if (CurrentSeasonState == null) CurrentSeasonState = new SeasonRuntimeState();
            CurrentSeasonState.seasonId = data.seasonId;
            if (!string.IsNullOrEmpty(data.themeId))
                CurrentSeasonState.themeId = data.themeId;

            string themeName = GetSeasonThemeName(data.themeId);
            string subtitle  = GetSeasonThemeSubtitle(data.themeId);

            UI.AnnouncementUI.Instance?.ShowAnnouncement(
                "新赛季",
                string.IsNullOrEmpty(subtitle) ? themeName : $"{themeName} · {subtitle}",
                GetSeasonThemeColor(data.themeId),
                5f);

            // 主题视觉（MVP 极简：仅 SeasonThemeUI 横幅颜色）
            UI.SeasonThemeUI.Instance?.ApplyTheme(data.themeId);

            OnSeasonStarted?.Invoke(data);
            Debug.Log($"[SGM] season_started: seasonId={data.seasonId} themeId={data.themeId}");
        }

        /// <summary>season_settlement：赛季结算 → MVP 占位 Log；SeasonSettlementUI 属 P2 阶段。</summary>
        private void HandleSeasonSettlement(SeasonSettlementData data)
        {
            if (data == null) return;
            OnSeasonSettlement?.Invoke(data);

            string nextThemeName = GetSeasonThemeName(data.nextThemeId);
            UI.AnnouncementUI.Instance?.ShowAnnouncement(
                "赛季结算",
                string.IsNullOrEmpty(nextThemeName) ? "下一赛季即将开启" : $"下一赛季：{nextThemeName}",
                new Color(1f, 0.85f, 0.1f),
                5f);
            Debug.Log($"[SGM] season_settlement: seasonId={data.seasonId} nextThemeId={data.nextThemeId}");
        }

        /// <summary>§36.3 themeId → 中文名</summary>
        public static string GetSeasonThemeName(string themeId)
        {
            if (string.IsNullOrEmpty(themeId)) return "";
            switch (themeId)
            {
                case "classic_frozen": return "经典冰封";
                case "blood_moon":     return "血月";
                case "snowstorm":      return "暴风雪";
                case "dawn":           return "黎明";
                case "frenzy":         return "狂潮";
                case "serene":         return "宁静";
                default:               return themeId;
            }
        }

        /// <summary>§36.3 themeId → 简短说明（首条横幅副标题）</summary>
        public static string GetSeasonThemeSubtitle(string themeId)
        {
            if (string.IsNullOrEmpty(themeId)) return "";
            switch (themeId)
            {
                case "classic_frozen": return "基线赛季";
                case "blood_moon":     return "夜晚怪物强化";
                case "snowstorm":      return "白天减速·夜晚多矿";
                case "dawn":           return "节奏加快";
                case "frenzy":         return "高频怪潮";
                case "serene":         return "夜长 Boss 弱";
                default:               return "";
            }
        }

        /// <summary>§36.3 themeId → 主题色（AnnouncementUI 横幅 + SeasonThemeUI overlay）</summary>
        public static Color GetSeasonThemeColor(string themeId)
        {
            if (string.IsNullOrEmpty(themeId)) return new Color(1f, 0.85f, 0.1f);
            switch (themeId)
            {
                case "classic_frozen": return new Color(0.60f, 0.85f, 1.00f); // 蓝白冷色
                case "blood_moon":     return new Color(1.00f, 0.20f, 0.30f); // 红月
                case "snowstorm":      return new Color(0.85f, 0.90f, 0.95f); // 灰白
                case "dawn":           return new Color(1.00f, 0.65f, 0.30f); // 橙黄
                case "frenzy":         return new Color(1.00f, 0.30f, 0.60f); // 粉紫
                case "serene":         return new Color(0.40f, 1.00f, 0.75f); // 极光绿
                default:               return new Color(1f, 0.85f, 0.1f);
            }
        }

        // ==================== §36.4 赛季 Boss Rush（🆕 v1.27） ====================

        /// <summary>season_boss_rush_start：D7 夜晚开始 → BossRushBanner 显示全服血量池 + 下季主题预告。</summary>
        private void HandleBossRushStarted(BossRushStartedData data)
        {
            OnBossRushStarted?.Invoke(data);
            UI.BossRushBanner.Instance?.Show(data);
            int n = data.participatingRooms == null ? 0 : data.participatingRooms.Length;
            Debug.Log($"[SGM] season_boss_rush_start: seasonId={data.seasonId} bossHpTotal={data.bossHpTotal} participatingRooms={n} nextThemeId={data.nextThemeId}");
        }

        /// <summary>season_boss_rush_killed：全服 Boss 池归零 → BossRushBanner 击杀反馈 → 3s 后隐藏。</summary>
        private void HandleBossRushKilled(BossRushKilledData data)
        {
            OnBossRushKilled?.Invoke(data);
            UI.BossRushBanner.Instance?.OnKilled(data);
            Debug.Log($"[SGM] season_boss_rush_killed: seasonId={data.seasonId} killedAt={data.killedAt}");
        }

        // ==================== §36.12 分时段解锁 & 老用户豁免（🆕 v1.27） ====================

        /// <summary>veteran_unlocked：玩家首次达到老用户豁免条件 → FeatureUnlockBanner 专属横幅。</summary>
        private void HandleVeteranUnlocked(VeteranUnlockedData data)
        {
            OnVeteranUnlocked?.Invoke(data);
            UI.FeatureUnlockBanner.Instance?.ShowVeteranUnlocked(data);
            Debug.Log($"[SGM] veteran_unlocked: openId={data.openId} reason={data.reason}");
        }

        /// <summary>roulette_spin_failed：主播轮盘抽奖失败（含 feature_locked；roulette.minDay=1 实际不触发）→ 跑马灯兜底。</summary>
        private void HandleRouletteSpinFailed(string dataJson)
        {
            string reason = null;
            int unlockDay = 0;
            try
            {
                // 轻量反序列化（无专用 Data 类）
                var probe = JsonUtility.FromJson<BroadcasterActionFailedData>(dataJson);
                if (probe != null) { reason = probe.reason; unlockDay = probe.unlockDay; }
            }
            catch { /* ignore */ }
            string reasonText = reason == SurvivalMessageProtocol.ReasonFeatureLocked
                ? $"轮盘暂未解锁（D{unlockDay} 解锁）"
                : (string.IsNullOrEmpty(reason) ? "轮盘操作失败" : $"轮盘操作失败：{reason}");
            OnPlayerActivityMessage?.Invoke(reasonText);
            UI.HorizontalMarqueeUI.Instance?.AddMessage("轮盘", null, reasonText);
            Debug.Log($"[SGM] roulette_spin_failed: reason={reason} unlockDay={unlockDay}");
        }

        /// <summary>broadcaster_action_failed：主播 ⚡加速/🌊事件触发失败（含 feature_locked）→ 跑马灯 + 活动消息。</summary>
        private void HandleBroadcasterActionFailed(BroadcasterActionFailedData data)
        {
            OnBroadcasterActionFailed?.Invoke(data);
            string reasonText = FormatBroadcasterActionFailReason(data.reason, data.unlockDay);
            string actionText = GetBroadcasterActionName(data.action);
            string msg = $"{actionText}：{reasonText}";
            UI.AnnouncementUI.Instance?.ShowAnnouncement("主播操作失败", msg, new Color(1f, 0.4f, 0.2f), 2f);
            OnPlayerActivityMessage?.Invoke(msg);
            Debug.Log($"[SGM] broadcaster_action_failed: action={data.action} reason={data.reason} unlockDay={data.unlockDay}");
        }

        private static string FormatBroadcasterActionFailReason(string reason, int unlockDay)
        {
            if (string.IsNullOrEmpty(reason)) return "未知原因";
            switch (reason)
            {
                case "feature_locked": return unlockDay > 0 ? $"D{unlockDay} 解锁此功能" : "功能未解锁";
                case "in_cooldown":    return "冷却中";
                case "wrong_phase":    return "当前阶段不可用";
                case "not_broadcaster": return "仅主播可操作";  // 🔴 audit-r25 GAP-A25-MINOR-01：服务端 _requireBroadcaster 拒绝时 emit
                default:               return reason;
            }
        }

        private static string GetBroadcasterActionName(string action)
        {
            if (string.IsNullOrEmpty(action)) return "主播操作";
            switch (action)
            {
                case "efficiency_boost": return "紧急加速";
                case "trigger_event":    return "触发事件";
                default:                 return action;
            }
        }

        /// <summary>§36.12 合并新解锁功能到本地缓存（world_clock_tick.newlyUnlockedFeatures → CurrentUnlockedFeatures）。
        /// 不触发 OnUnlockedFeaturesSync（由 season_state/survival_game_state 的全集推送独占），
        /// 避免同一 tick 双事件导致订阅者重复刷新。</summary>
        private void MergeNewlyUnlockedFeatures(string[] incoming)
        {
            if (incoming == null || incoming.Length == 0) return;
            var merged = new List<string>();
            if (CurrentUnlockedFeatures != null)
            {
                foreach (var f in CurrentUnlockedFeatures) if (!string.IsNullOrEmpty(f)) merged.Add(f);
            }
            foreach (var f in incoming)
            {
                if (!string.IsNullOrEmpty(f) && !merged.Contains(f)) merged.Add(f);
            }
            CurrentUnlockedFeatures = merged;
        }

        /// <summary>§36.12 同步服务端推送的全集（season_state.unlockedFeatures / survival_game_state.unlockedFeatures）。
        /// 触发 OnUnlockedFeaturesSync，供 FeatureLockOverlay 等订阅者刷新按钮锁态。</summary>
        private void SyncUnlockedFeatures(string[] incoming)
        {
            if (incoming == null) return;
            var snapshot = new List<string>(incoming.Length);
            foreach (var f in incoming)
            {
                if (!string.IsNullOrEmpty(f) && !snapshot.Contains(f)) snapshot.Add(f);
            }
            CurrentUnlockedFeatures = snapshot;
            OnUnlockedFeaturesSync?.Invoke(incoming);
            Debug.Log($"[SGM] SyncUnlockedFeatures: {string.Join(",", incoming)}");
        }

        /// <summary>§36.12 查询某个功能是否已解锁（供 UI 按钮兜底判断）。</summary>
        public bool IsFeatureUnlocked(string featureId)
        {
            if (string.IsNullOrEmpty(featureId)) return false;
            if (CurrentUnlockedFeatures == null) return false; // 首次同步前保守按锁定
            foreach (var f in CurrentUnlockedFeatures)
            {
                if (f == featureId) return true;
            }
            return false;
        }
    }

    // ==================== §36 运行时缓存 ====================

    /// <summary>§36 SurvivalGameManager.CurrentSeasonState 缓存结构，UI 层只读。
    /// 由 world_clock_tick / season_state 消息各字段按需更新；首次更新前各字段为默认值（0 / null）。</summary>
    [Serializable]
    public class SeasonRuntimeState
    {
        public string phase;              // "day" | "night"
        public int    phaseRemainingSec;
        public int    seasonDay;
        public string themeId;
        public int    seasonId;
    }

    // ==================== M3-02 新增数据类型 ====================

    [Serializable] public class JoinRoomConfirmData
    {
        public bool   isRoomCreator;      // 是否为房间创建者（主播）
        public bool   has_active_session; // 服务器是否有进行中的会话
    }

    [Serializable] public class BroadcasterEffectData
    {
        public string action;        // "efficiency_boost" | "trigger_event"
        public float  duration;      // 毫秒
        public string eventId;       // trigger_event 时的事件 ID
        public string eventName;
        public string triggeredBy;
        // ⚠️ audit-r24 GAP-A24-05 补 multiplier：服务端 SurvivalRoom.js:1207 efficiency_boost 路径 emit
        // `multiplier: 2.0`；r24 之前客户端无字段 → JsonUtility 静默丢失 → UI 文案"效率翻倍"硬编码而非数据驱动。
        // 未来若服务端调整倍率（如 1.5/2.5）客户端 UI 需联动显示动态文案。
        public float  multiplier;    // efficiency_boost 时的倍率（2.0 默认；trigger_event 时为 0）
    }

    [Serializable] public class SpecialEffectData
    {
        public string effect;        // "glow_all" | "frozen_all"
        public float  duration;      // 秒
    }

    [Serializable] public class RandomEventData
    {
        public string eventId;       // "E01_snowstorm" / "ice_ground" / "heavy_fog" / "morale_boost" / ...
        public string name;          // 中文名称
        // 🆕 Fix C (组 B Reviewer P0) §34B B3 随机事件扩展字段（服务端每事件带独立字段，
        //   JsonUtility 反序列化时缺失字段回落 0/false/空，21 个字段全部在此声明）：
        public int      durationMs;        // 通用：持续时长 ms
        public float    slowMult;          // ice_ground：产出倍率（策划原文"移速 -20%"，等效产出 ×0.8）
        public float    effBonus;          // aurora_flash：效率加成
        public int      subFurnaceTemp;    // earthquake：炉温减少
        public int      subGateHp;         // earthquake：城门血量减少
        public int      killedCount;       // meteor_shower：击杀数
        public string[] killedIds;         // meteor_shower：被击杀怪物 id 列表
        public bool     hideMonsterHp;     // heavy_fog：30s 隐藏怪物血条（与 resource_update 语义一致）
        public int      tempPerTick;       // hot_spring：每 tick 炉温增量
        public int      tickSec;           // hot_spring：tick 间隔秒
        public int      foodBefore;        // food_spoil：腐败前食物量
        public int      foodAfter;         // food_spoil：腐败后食物量
        public float    lossPct;           // food_spoil：损耗百分比
        public int      nextWorkMult;      // inspiration：下一次工作倍率
        public string   targetPlayerId;    // morale_boost：被鼓舞矿工 playerId
        public string   targetPlayerName;  // morale_boost：矿工名（UI 显示）
        public string   bubbleText;        // morale_boost：气泡文字
        public int      scoutCount;        // E03 daytime scout：侦察兵数量
        public string[] scoutIds;          // E03 daytime scout：侦察兵 id 列表
        public bool     isDaytimeScout;    // E03 daytime scout：标志位
        public int      addFood;           // airdrop_supply：食物补给
        public int      addCoal;           // airdrop_supply：煤炭补给
        public int      addOre;            // airdrop_supply：矿石补给
    }

    [Serializable] public class GiftPauseData
    {
        public float duration;       // 毫秒
    }

    [Serializable] public class BobaoData
    {
        public string message;
    }
}
