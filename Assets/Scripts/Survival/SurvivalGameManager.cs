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
        public event Action<string> OnPlayerActivityMessage;     // 弹幕消息文本
        public event Action<int> OnScorePoolUpdated;             // 积分池变动（实时推送）
        public event Action<WeeklyRankingData>   OnWeeklyRankingReceived;   // 本周贡献榜（服务器推送）
        public event Action<LiveRankingData>     OnLiveRankingReceived;     // 实时贡献榜（游戏中防抖推送）
        public event Action<StreamerRankingData> OnStreamerRankingReceived; // 主播排行榜（服务器推送）

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
            if (_state == SurvivalState.Settlement || _state == SurvivalState.Idle)
            {
                if (type != "join_room_confirm" && type != "weekly_ranking" && type != "streamer_ranking") return;
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
                    // 结算/空闲状态下忽略昼夜切换消息，避免计时器继续跑
                    if (_state == SurvivalState.Settlement || _state == SurvivalState.Idle) break;
                    var pc = JsonUtility.FromJson<PhaseChangedData>(dataJson);
                    dayNightManager?.HandlePhaseChanged(pc);
                    // 黑夜开始 → 全员防守；白天开始 → 回工位
                    if (pc.phase == "night")
                        WorkerManager.Instance?.EnterNightDefense();
                    else if (pc.phase == "day")
                        WorkerManager.Instance?.ExitNightDefense();
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
                    if ((_state == SurvivalState.Loading || _state == SurvivalState.Waiting) && IsEnteringScene)
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
                        dayNightManager?.HandlePhaseChanged(new PhaseChangedData
                        {
                            phase = data.state,
                            day = data.day,
                            phaseDuration = data.remainingTime
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

            var endData = new SurvivalGameEndedData
            {
                result = "lose",
                reason = reason,
                dayssurvived = dayNightManager != null ? dayNightManager.CurrentDay : 0
            };
            HandleGameEnded(endData);
        }

        private void HandleDefeatOrVictory(string reason)
        {
            if (_state != SurvivalState.Running) return;
            var endData = new SurvivalGameEndedData
            {
                result = reason == "survived" ? "win" : "lose",
                reason = reason,
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

                var settlement = new global::DrscfZ.UI.SettlementData
                {
                    IsVictory    = data.result == "win",
                    SurvivalDays = data.dayssurvived,
                    FailReason   = data.reason switch
                    {
                        "food_depleted" => "food",
                        "temp_freeze"   => "temperature",
                        "gate_breached" => "gate",
                        _               => "unknown"
                    },
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
            var monster = monsterWaveSpawner?.FindMonsterById(data.monsterId);
            if (monster != null)
                monster.ShowHitEffect();

            // 怪物受击 → 轻微相机抖动
            SurvivalCameraController.OnMonsterAttack();

            // 追踪贡献（内部 float dict，用于结算总分统计）
            int combatScore = data.score > 0 ? data.score : 1;
            TrackContribution(data.playerId, combatScore);

            // 同步到 RankingSystem（供 SurvivalLiveRankingUI 实时榜读取）
            RankingSystem.Instance?.TrackSurvivalScore(
                data.playerId,
                string.IsNullOrEmpty(data.playerName) ? "匿名" : data.playerName,
                combatScore);

            OnPlayerActivityMessage?.Invoke($"{data.playerName} 攻击怪物 +{combatScore}分");
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

            Debug.Log($"[SurvivalGM] 怪物死亡: {data.monsterId} ({data.monsterType}) 击杀者:{data.killedBy}");
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
            cityGateSystem?.HandleUpgrade(data.level, data.newMaxHp);
            UI.AnnouncementUI.Instance?.ShowAnnouncement(
                $"城门升级至 Lv.{data.level}!",
                $"最大HP提升至 {data.newMaxHp}",
                new Color(0.2f, 0.8f, 1f), 2f);
            OnPlayerActivityMessage?.Invoke($"城门已升级至 Lv.{data.level}（最大HP:{data.newMaxHp}）");
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
