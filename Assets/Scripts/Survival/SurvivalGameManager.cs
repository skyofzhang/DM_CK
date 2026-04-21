using UnityEngine;
using System;
using System.Collections;
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

        // gift_pause 暂停标志（T5 神秘空投特效期间）
        private bool _isPaused = false;

        // 事件（UI订阅）
        public event Action<SurvivalState> OnStateChanged;
        public event Action<DifficultyLevel> OnDifficultySet;
        public event Action<WorkCommandData> OnWorkCommand;       // 工作指令 → WorkerManager
        public event Action<SurvivalGiftData> OnGiftReceived;    // 礼物效果
        public event Action<SurvivalPlayerJoinedData> OnPlayerJoined;
        public event Action<SurvivalGameEndedData> OnGameEnded;
        public event Action<PhaseChangedData> OnPhaseChanged;    // 🆕 §4.2 昼夜/恢复期切换（携带 variant）
        public event Action<string> OnPlayerActivityMessage;     // 弹幕消息文本
        public event Action<int> OnScorePoolUpdated;             // 积分池变动（实时推送）
        public event Action<WeeklyRankingData>   OnWeeklyRankingReceived;   // 本周贡献榜（服务器推送）
        public event Action<LiveRankingData>     OnLiveRankingReceived;     // 实时贡献榜（游戏中防抖推送）
        public event Action<StreamerRankingData> OnStreamerRankingReceived; // 主播排行榜（服务器推送）

        // §16 / §4.2 最近一次 phase_changed 的 variant 缓存（"normal" | "recovery"）
        // HUD 推荐规则 R10-8 / PeaceNightOverlay / BroadcasterDecisionHUD 查询此字段过滤恢复期白天
        private string _lastPhaseVariant = "normal";
        public string LastPhaseVariant => _lastPhaseVariant;

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
        public event Action<BuildCompletedData>           OnBuildCompleted;
        public event Action<BuildDemolishedData>          OnBuildDemolished;
        public event Action<BuildProposeFailedData>       OnBuildProposeFailed;
        public event Action<BuildCancelledData>           OnBuildCancelled;
        public event Action<BuildingDemolishedBatchData>  OnBuildingDemolishedBatch;
        public event Action<MonsterWaveIncomingData>      OnMonsterWaveIncoming;

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

        /// <summary>§36 缓存的当前赛季/全服时钟状态，供 UI 层按需查询。
        /// phase/phaseRemainingSec 随 world_clock_tick 每秒刷新；seasonDay/themeId/seasonId
        /// 随 world_clock_tick + season_state 同步；首次收到任意一方消息前保持为 null。</summary>
        public SeasonRuntimeState CurrentSeasonState { get; private set; }

        // §39 本地装备缓存（自己最新的 equipped，供 UI 层做灰化/装备按钮回显）
        public ShopEquipped MyEquipped { get; private set; }

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
                    _lastPhaseVariant = string.IsNullOrEmpty(pc.variant) ? "normal" : pc.variant;

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
                    dayNightManager?.SyncRemainingTime(ru.remainingTime);
                    if (ru.scorePool > 0) OnScorePoolUpdated?.Invoke(ru.scorePool);
                    break;

                // ----- 怪物波次 -----
                case "monster_wave":
                    var mw = JsonUtility.FromJson<MonsterWaveData>(dataJson);
                    monsterWaveSpawner?.SpawnWave(mw);
                    // 通知 WorkerManager 怪物出现，让闲置 Worker 自动攻击
                    WorkerManager.Instance?.OnMonstersAppear();
                    break;

                // ----- 矿工死亡/复活/HP全量同步（HP系统）-----
                case "worker_died":
                    var wd = JsonUtility.FromJson<WorkerDiedData>(dataJson);
                    WorkerManager.Instance?.HandleWorkerDied(wd.playerId, wd.respawnAt);
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
                    // 主播可选：跑马灯提示（当前仅日志，不弹 toast 避免阻塞）
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
                    if (lr != null) OnLiveRankingReceived?.Invoke(lr);
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
            // 同步积分池（连接/断线重连时立即刷新显示）
            if (data.scorePool > 0) OnScorePoolUpdated?.Invoke(data.scorePool);

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
                        _lastPhaseVariant = dataVariant;
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
        }

        private void HandleWorkCommand(WorkCommandData data)
        {
            OnWorkCommand?.Invoke(data);
            workerManager?.AssignWork(data);

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

        /// <summary>显示全屏随机事件公告（3秒后消失）</summary>
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
                _                  => "特殊事件发生！"
            };
            Color color = eventId is "E01_snowstorm" or "snowstorm" or "E03_monster_wave" or "monster_wave"
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

            // T5 神秘空投 → 顶部金色飘字
            if (gift.giftTier >= 5)
                UI.TopFloatingTextUI.Instance?.ShowGold($"{gift.playerName} 神秘空投！");

            // 触发相机震屏（T3+ 礼物有重量感）
            if (gift.giftTier >= 3)
                SurvivalCameraController.Shake(gift.giftTier * 0.05f, 0.4f);
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

            // 直接通知SettlementUI（它可能处于inactive状态，无法通过事件收到）
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

        private void ResetAllSystems()
        {
            _contributions.Clear();
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

            Debug.Log($"[SurvivalGM] 怪物死亡: {data.monsterId} ({data.monsterType}) 击杀者:{data.killerId}");
        }

        private void HandleNightCleared(string type, string dataJson)
        {
            // 夜晚提前结束（Boss被击败），服务器会随后发phase_changed切换到白天
            UI.AnnouncementUI.Instance?.ShowAnnouncement(
                "夜晚已清扫!", "极地守护者们干得漂亮！",
                new Color(0.4f, 0.9f, 1f), 3f);
            SurvivalCameraController.OnNightCleared();
            Systems.AudioManager.Instance?.PlaySFX("victory");
            Debug.Log("[SurvivalGM] 夜晚提前结束！BOSS已被击败。");
        }

        private void HandleGateUpgraded(string type, string dataJson)
        {
            var data = JsonUtility.FromJson<GateUpgradedData>(dataJson);
            if (data == null) return;

            // 更新城门最大HP
            cityGateSystem?.HandleUpgrade(data.newLevel, data.newMaxHp);
            UI.AnnouncementUI.Instance?.ShowAnnouncement(
                $"城门升级至 Lv.{data.newLevel}!",
                $"最大HP提升至 {data.newMaxHp}",
                new Color(0.2f, 0.8f, 1f), 2f);
            OnPlayerActivityMessage?.Invoke($"城门已升级至 Lv.{data.newLevel}（最大HP:{data.newMaxHp}）");
        }

        private void HandleGateUpgradeFailed(string dataJson)
        {
            var data = JsonUtility.FromJson<GateUpgradeFailedData>(dataJson);
            if (data == null) return;
            string msg = data.reason == "max_level"
                ? "城门已达最高等级！"
                : $"矿石不足！需要 {data.required}，当前 {data.available}";
            UI.AnnouncementUI.Instance?.ShowAnnouncement("城门升级失败", msg, new Color(1f, 0.4f, 0.2f), 2f);
            Debug.Log($"[SGM] gate_upgrade_failed: {data.reason}");
        }

        // ==================== §30 矿工成长系统 ====================

        private void HandleWorkerLevelUp(WorkerLevelUpData data)
        {
            // 路由到 WorkerManager 刷新对应 Worker 的显示
            WorkerManager.Instance?.HandleWorkerLevelUp(data);

            // 阶段10（传奇）→ 特殊相机震撼（TODO §30：未来替换为镜头推近 + 全场金红粒子爆发）
            if (data.newTier >= 10)
            {
                SurvivalCameraController.Shake(0.3f, 0.8f);
                // TODO §30：传奇镜头——镜头缓慢推近该矿工 1s，金红色粒子全场爆发
            }
            else
            {
                // 阶段 2~9：彩色公告横幅
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
            UI.HorizontalMarqueeUI.Instance?.AddMessage(
                data.playerName, null, "传奇之力！免于死亡！");
            OnLegendReviveTriggered?.Invoke(data);
            Debug.Log($"[SGM] legend_revive_triggered: {data.playerName}");
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
            // 占位视觉：借用 WorkerVisual.TriggerSpecial 的金色光晕表达"格挡生效"
            var worker = WorkerManager.Instance?.ActiveWorkers;
            if (worker != null)
            {
                foreach (var w in worker)
                {
                    if (w != null && w.PlayerId == data.playerId)
                    {
                        w.GetComponent<WorkerVisual>()?.TriggerSpecial();
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
            Systems.AudioManager.Instance?.PlaySFX("sfx_gate_alarm");
            Debug.Log("[SurvivalGM] BOSS出现！全员备战！");
        }

        // ==================== 助威模式 §33（🆕 v1.27）====================

        private void HandleSupporterJoined(SupporterJoinedData data)
        {
            if (data == null || string.IsNullOrEmpty(data.playerId)) return;
            var displayName = string.IsNullOrEmpty(data.playerName) ? "匿名" : data.playerName;

            UI.SurvivalTopBarUI.Instance?.UpdateSupporterCount(data.supporterCount);
            // BarrageMessageUI 通过订阅 OnPlayerActivityMessage 自动显示（COLOR_JOIN 绿色）
            OnPlayerActivityMessage?.Invoke($"{displayName} 加入助威！发送弹幕为全队加油");
            OnSupporterJoined?.Invoke(data);
        }

        private void HandleSupporterAction(SupporterActionData data)
        {
            if (data == null || string.IsNullOrEmpty(data.playerId)) return;
            var displayName = string.IsNullOrEmpty(data.playerName) ? "匿名" : data.playerName;
            string cmdDesc = GetSupporterCmdDescription(data.cmd);

            // 跑马灯显示"[助威] XXX：食物+1"
            UI.HorizontalMarqueeUI.Instance?.AddMessage($"[助威] {displayName}", null, cmdDesc);
            OnPlayerActivityMessage?.Invoke($"[助威] {displayName} → {cmdDesc}");

            // 夜晚 cmd=6 → 随机存活矿工头顶闪光
            if (data.cmd == 6)
                WorkerManager.Instance?.FlashRandomAliveWorker();

            OnSupporterAction?.Invoke(data);
        }

        private void HandleSupporterPromoted(SupporterPromotedData data)
        {
            if (data == null) return;
            var newName = string.IsNullOrEmpty(data.newPlayerName) ? "匿名" : data.newPlayerName;
            var oldName = string.IsNullOrEmpty(data.oldPlayerName) ? "匿名" : data.oldPlayerName;

            WorkerManager.Instance?.HandleSupporterPromoted(data);
            UI.HorizontalMarqueeUI.Instance?.AddMessage(newName, null, $"替补上场！{oldName} 转为助威");
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

        /// <summary>轮盘充能完成/剩余秒数同步（RouletteUI 订阅后自行处理）</summary>
        private void HandleRouletteReady(RouletteReadyData data)
        {
            OnRouletteReady?.Invoke(data);
            Debug.Log($"[SGM] broadcaster_roulette_ready: readyAt={data.readyAt}");
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
        ///   died    → "不幸阵亡"
        ///   empty   → "空手而归"
        /// </summary>
        private static string FormatExpeditionOutcome(ExpeditionOutcome outcome)
        {
            if (outcome == null) return "归来";
            if (outcome.died)    return "外域阵亡";

            if (outcome.type == "died")  return "外域阵亡";
            if (outcome.type == "empty") return "空手而归";

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
            UI.AnnouncementUI.Instance?.ShowAnnouncement("即将来袭",
                "瞭望塔预警：10 秒后怪物出现",
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

        /// <summary>装备切换失败（unicast）：MVP 仅 Log</summary>
        private void HandleShopEquipFailed(ShopEquipFailedData data)
        {
            OnShopEquipFailed?.Invoke(data);
            Debug.Log($"[SGM] shop_equip_failed: slot={data.slot} itemId={data.itemId} reason={data.reason}");
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

        /// <summary>攻击/反击失败（unicast 发起方）：跑马灯 + 活动消息。</summary>
        private void HandleTribeWarAttackFailed(TribeWarAttackFailedData data)
        {
            OnTribeWarAttackFailed?.Invoke(data);
            string reasonText = FormatTribeWarFailReason(data.reason);
            OnPlayerActivityMessage?.Invoke($"攻防战操作失败：{reasonText}");
            UI.HorizontalMarqueeUI.Instance?.AddMessage("攻防战", null, reasonText);
            Debug.Log($"[SGM] tribe_war_attack_failed: reason={data.reason}");
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

        /// <summary>§35.10 attack_failed.reason → 中文（MVP 仅活动消息/跑马灯用）</summary>
        private static string FormatTribeWarFailReason(string reason)
        {
            if (string.IsNullOrEmpty(reason)) return "未知原因";
            switch (reason)
            {
                case "cannot_attack_self":          return "不能攻击自己";
                case "in_cooldown":                 return "冷却中（60s）";
                case "already_attacking":           return "已在攻击目标";
                case "target_already_under_attack": return "目标已被其他房间攻击";
                case "target_unavailable":          return "目标房间不可攻击";
                case "target_not_playing":          return "目标房间未在游戏中";
                case "not_under_attack":            return "未被攻击，无法反击";
                case "wrong_phase":                 return "当前阶段不允许发起";
                case "feature_locked":              return "功能未解锁";
                default:                            return reason;
            }
        }

        /// <summary>§35.10 attack_ended.reason → 中文</summary>
        private static string FormatTribeWarEndReason(string reason)
        {
            if (string.IsNullOrEmpty(reason)) return "结束";
            switch (reason)
            {
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

            // 若主题变化 → 通知 SeasonThemeUI 应用横幅颜色（MVP 极简：仅颜色切换，无粒子/天空盒）
            if (!string.IsNullOrEmpty(data.themeId) && data.themeId != prevThemeId)
            {
                UI.SeasonThemeUI.Instance?.ApplyTheme(data.themeId);
            }

            Debug.Log($"[SGM] season_state: seasonId={data.seasonId} seasonDay={data.seasonDay} themeId={data.themeId} (prev seasonId={prevSeasonId} themeId={prevThemeId})");
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
    }

    [Serializable] public class SpecialEffectData
    {
        public string effect;        // "glow_all" | "frozen_all"
        public float  duration;      // 秒
    }

    [Serializable] public class RandomEventData
    {
        public string eventId;       // "E01_snowstorm" 等
        public string name;          // 中文名称
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
