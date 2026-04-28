using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using DrscfZ.Survival;
using DrscfZ.Core;

namespace DrscfZ.UI
{
    /// <summary>
    /// §24.5 主播决策中心 HUD（🆕 v1.27 MVP）。
    ///
    /// 在 BroadcasterPanel 左上角展示一张"推荐决策卡组"，至多 3 张卡按紧急度排序（红→黄→绿）。
    /// 推荐规则详见策划案 §24.5 表格；客户端事件驱动，O(1) 重算，无节流。
    ///
    /// 推荐规则（<see cref="ComputeTopCards"/>）：
    ///   红 R1: gateHp &lt; 0.3*gateMaxHp         → "城门告急！考虑升级或建造"   → §10 升门面板
    ///   红 R2: phase=night 且 aliveWorkers &lt; 3  → "矿工快死光了！建议刷 T5 救场" → （无跳转）
    ///   黄 Y1: rouletteReady                    → "事件轮盘已就绪"             → §24.4 RouletteUI
    ///   黄 Y2: ore ≥ GateUpgradeCost(curLv) 且 phase=day 且 variant≠recovery
    ///                                           → "矿石已够升城门"             → §10 升门面板
    ///   黄 Y3: seasonDay≥3 且 buildings&lt;2 且 day 且 ≠recovery
    ///                                           → "建议发起建造投票"           → §37 BuildVoteUI
    ///   绿 G1: day 且 ≠recovery 且 exp&lt;2 且 seasonDay≥5
    ///                                           → "可派矿工探险赚外快"         → §38 ExpeditionUI
    ///   绿 G2: day 且 ≠recovery 且 tribeWar=idle 且 seasonDay≥7
    ///                                           → "可发起跨房攻防战"           → §35 TribeWarLobbyUI
    ///
    /// 红 R1 兜底：gateMaxHp ≤ 0（首个 resource_update 到达前的极短窗口）直接隐藏红 R1。
    ///
    /// 挂载：<c>BroadcasterPanel</c>；预创建 <c>DecisionHUD</c> 容器 + 3 张 CardPrefab 子节点。
    /// 若 <c>_cardsRoot</c> 为 null，运行时 <c>CreateRuntimeHudFallback</c> 自建兜底。
    /// </summary>
    public class BroadcasterDecisionHUD : MonoBehaviour
    {
        public static BroadcasterDecisionHUD Instance { get; private set; }

        // ==================== Inspector 字段 ====================

        [Header("卡片容器（水平或垂直排布；Editor 脚本预创建）")]
        [SerializeField] private RectTransform _cardsRoot;

        [Header("3 张卡片（Editor 脚本预创建；若为 null 运行时 CreateRuntimeHudFallback）")]
        [SerializeField] private CardView _card0;
        [SerializeField] private CardView _card1;
        [SerializeField] private CardView _card2;

        [Header("首次气泡提示（'这里会告诉你现在该做什么'）")]
        [SerializeField] private GameObject _firstTimeTipRoot;  // 可选：小气泡 GO
        [SerializeField] private TMP_Text   _firstTimeTipText;  // 可选：首次文案
        [SerializeField] private string     _firstTimeTip = "这里会告诉你现在该做什么";

        [Header("跳转回调（Inspector 绑定到现有面板打开方法）")]
        public UnityEvent _onUpgradeGateClicked;
        public UnityEvent _onRouletteClicked;
        public UnityEvent _onBuildClicked;
        public UnityEvent _onExpeditionClicked;
        public UnityEvent _onTribeWarClicked;

        // ==================== 内存状态缓存 ====================

        private int    _gateHp          = 0;
        private int    _gateMaxHp       = 0;
        private int    _gateLevel       = 1;
        private int    _ore             = 0;
        private string _phase           = "day";     // "day" | "night"
        private string _variant         = "normal";  // "normal" | "recovery" | "peace_night"...
        private int    _seasonDay       = 1;
        private bool   _rouletteReady   = false;
        private int    _activeBuildingsCount   = 0;
        private int    _activeExpeditionsCount = 0;
        private string _tribeWarState   = "idle";    // "idle" | "attacking" | "defending"

        /// <summary>PlayerPrefs key：首次显示气泡标志。</summary>
        private const string PREFS_KEY_TIP_SHOWN = "bdh_tip_shown";

        // ==================== §10 城门升级消耗表（对齐 BroadcasterPanel._upgradeCostTable 与 Server GATE_UPGRADE_COSTS） ====================
        // 索引 i 对应 gateLevel=i+1 → i+2 的升级消耗（Lv1→Lv2=100, Lv2→Lv3=250, ..., Lv5→Lv6=1500）
        // §S0 修复（CLAUDE.md）：GATE_UPGRADE_COSTS[0]=100；策划案 §10.2.2/§10.10.1 定义 5 项
        private static readonly int[] GateUpgradeCosts = new[] { 100, 250, 500, 1000, 1500 };

        // ==================== 生命周期 ====================

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // 运行时兜底：若 Editor 脚本未预创建卡片容器，自建最小化容器
            if (_cardsRoot == null || _card0 == null)
            {
                Debug.LogWarning("[BroadcasterDecisionHUD] 卡片容器或卡片未绑定，运行时自建兜底；建议跑 Tools → DrscfZ → Setup Section 17.15 + 24.5 UI");
                CreateRuntimeHudFallback();
            }

            // 订阅事件
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnResourceUpdate           += HandleResourceUpdate;
                sgm.OnPhaseChanged             += HandlePhaseChanged;
                sgm.OnRouletteReady            += HandleRouletteReady;
                sgm.OnRouletteEffectEnded      += HandleRouletteEffectEnded;
                sgm.OnSeasonState              += HandleSeasonState;
                sgm.OnFortressDayChanged       += HandleFortressDayChanged;
                sgm.OnBuildCompleted           += HandleBuildCompleted;
                sgm.OnBuildDemolished          += HandleBuildDemolished;
                sgm.OnBuildingDemolishedBatch  += HandleBuildingDemolishedBatch;
                sgm.OnExpeditionStarted        += HandleExpeditionStarted;
                sgm.OnExpeditionReturned       += HandleExpeditionReturned;
                sgm.OnExpeditionFailed         += HandleExpeditionFailed;
                sgm.OnTribeWarAttackStarted    += HandleTribeWarAttackStarted;
                sgm.OnTribeWarUnderAttack      += HandleTribeWarUnderAttack;
                sgm.OnTribeWarAttackEnded      += HandleTribeWarAttackEnded;
                // ⚠️ audit-r24 GAP-B24-20：断线重连后 _activeExpeditionsCount 不同步修复
                // r24 之前仅订阅 OnExpeditionStarted/Returned 增减，断线重连时 room_state.expeditions[] 数组不同步
                // → HUD 推荐"可派矿工探险"但实际服务端有进行中探险，会被拒（被动 wrong_phase 提示，体验失真）
                sgm.OnRoomState                += HandleRoomStateForExpeditionCount;
            }

            // 从 SurvivalGameManager 缓存初始化 seasonDay / variant
            if (sgm != null)
            {
                _variant = sgm.LastPhaseVariant ?? "normal";
                if (sgm.CurrentSeasonState != null)
                {
                    _seasonDay = sgm.CurrentSeasonState.seasonDay;
                    _phase     = sgm.CurrentSeasonState.phase ?? "day";
                }
            }

            // 首次显示气泡（PlayerPrefs 标记避免每次开局都弹）
            if (PlayerPrefs.GetInt(PREFS_KEY_TIP_SHOWN, 0) == 0)
            {
                if (_firstTimeTipRoot != null)
                {
                    _firstTimeTipRoot.SetActive(true);
                    if (_firstTimeTipText != null) _firstTimeTipText.text = _firstTimeTip;
                }
                PlayerPrefs.SetInt(PREFS_KEY_TIP_SHOWN, 1);
                PlayerPrefs.Save();
            }
            else
            {
                if (_firstTimeTipRoot != null) _firstTimeTipRoot.SetActive(false);
            }

            RefreshCards();
        }

        private void OnDestroy()
        {
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnResourceUpdate           -= HandleResourceUpdate;
                sgm.OnPhaseChanged             -= HandlePhaseChanged;
                sgm.OnRouletteReady            -= HandleRouletteReady;
                sgm.OnRouletteEffectEnded      -= HandleRouletteEffectEnded;
                sgm.OnSeasonState              -= HandleSeasonState;
                sgm.OnFortressDayChanged       -= HandleFortressDayChanged;
                sgm.OnBuildCompleted           -= HandleBuildCompleted;
                sgm.OnBuildDemolished          -= HandleBuildDemolished;
                sgm.OnBuildingDemolishedBatch  -= HandleBuildingDemolishedBatch;
                sgm.OnExpeditionStarted        -= HandleExpeditionStarted;
                sgm.OnExpeditionReturned       -= HandleExpeditionReturned;
                sgm.OnExpeditionFailed         -= HandleExpeditionFailed;
                sgm.OnRoomState                -= HandleRoomStateForExpeditionCount;
                sgm.OnTribeWarAttackStarted    -= HandleTribeWarAttackStarted;
                sgm.OnTribeWarUnderAttack      -= HandleTribeWarUnderAttack;
                sgm.OnTribeWarAttackEnded      -= HandleTribeWarAttackEnded;
            }
            if (Instance == this) Instance = null;
        }

        // ==================== 事件回调 ====================

        private void HandleResourceUpdate(ResourceUpdateData ru)
        {
            if (ru == null) return;
            _gateHp    = ru.gateHp;
            _gateMaxHp = ru.gateMaxHp;
            _gateLevel = ru.gateLevel > 0 ? ru.gateLevel : _gateLevel;
            _ore       = ru.ore;
            RefreshCards();
        }

        private void HandlePhaseChanged(PhaseChangedData pc)
        {
            if (pc == null) return;
            _phase   = pc.phase;
            _variant = string.IsNullOrEmpty(pc.variant) ? "normal" : pc.variant;
            RefreshCards();
        }

        private void HandleRouletteReady(RouletteReadyData data)
        {
            _rouletteReady = true;
            RefreshCards();
        }

        private void HandleRouletteEffectEnded(RouletteEffectEndedData data)
        {
            // 轮盘效果结束即不再"就绪"（等下一次充能）
            _rouletteReady = false;
            RefreshCards();
        }

        private void HandleSeasonState(SeasonStateData ss)
        {
            if (ss == null) return;
            _seasonDay = ss.seasonDay;
            RefreshCards();
        }

        private void HandleFortressDayChanged(FortressDayChangedData data)
        {
            if (data == null) return;
            _seasonDay = data.seasonDay;
            RefreshCards();
        }

        private void HandleBuildCompleted(BuildCompletedData data)
        {
            _activeBuildingsCount++;
            RefreshCards();
        }

        private void HandleBuildDemolished(BuildDemolishedData data)
        {
            _activeBuildingsCount = Mathf.Max(0, _activeBuildingsCount - 1);
            RefreshCards();
        }

        private void HandleBuildingDemolishedBatch(BuildingDemolishedBatchData data)
        {
            if (data?.buildingIds == null) return;
            _activeBuildingsCount = Mathf.Max(0, _activeBuildingsCount - data.buildingIds.Length);
            RefreshCards();
        }

        private void HandleExpeditionStarted(ExpeditionStartedData data)
        {
            _activeExpeditionsCount++;
            RefreshCards();
        }

        private void HandleExpeditionReturned(ExpeditionReturnedData data)
        {
            _activeExpeditionsCount = Mathf.Max(0, _activeExpeditionsCount - 1);
            RefreshCards();
        }

        private void HandleExpeditionFailed(ExpeditionFailedData data)
        {
            // send 被拒的场景计数不变；已有探险此事件不触发
            RefreshCards();
        }

        /// <summary>
        /// ⚠️ audit-r24 GAP-B24-20：断线重连同步 _activeExpeditionsCount。
        /// 服务端 room_state 包含 expeditions[] 数组（进行中的探险列表），客户端订阅后从 Length 同步计数器。
        /// 仅在 expeditions 字段非 null 时同步，避免误清零。
        /// </summary>
        private void HandleRoomStateForExpeditionCount(RoomStateData data)
        {
            if (data == null || data.expeditions == null) return;
            int newCount = data.expeditions.Length;
            if (newCount != _activeExpeditionsCount)
            {
                Debug.Log($"[BroadcasterDecisionHUD] room_state sync expeditions: {_activeExpeditionsCount} → {newCount}");
                _activeExpeditionsCount = newCount;
                RefreshCards();
            }
        }

        private void HandleTribeWarAttackStarted(TribeWarAttackStartedData data)
        {
            // MVP：进入攻击/防御态均标记为非 idle（HUD G2 推荐规则"tribeWarState=idle"会被屏蔽）
            // 精确 attacker/defender 判断需要 local roomId，MVP 不区分
            _tribeWarState = "attacking";
            RefreshCards();
        }

        private void HandleTribeWarUnderAttack(TribeWarUnderAttackData data)
        {
            _tribeWarState = "defending";
            RefreshCards();
        }

        private void HandleTribeWarAttackEnded(TribeWarAttackEndedData data)
        {
            _tribeWarState = "idle";
            RefreshCards();
        }

        // ==================== 核心：推荐计算 ====================

        /// <summary>按策划案 §24.5 7 条规则计算卡组（最多 3 张，按紧急度排序）。
        /// 红 priority=0 / 黄 priority=1 / 绿 priority=2；同级内按规则表顺序。</summary>
        public List<CardData> ComputeTopCards()
        {
            var all = new List<CardData>();

            int aliveWorkers = GetAliveWorkerCount();

            // —— 红 R1：城门告急（红卡兜底：gateMaxHp ≤ 0 时不出）
            if (_gateMaxHp > 0 && _gateHp < 0.3f * _gateMaxHp)
            {
                all.Add(new CardData(
                    urgency: Urgency.Red,
                    icon: "盾",
                    message: "城门告急！考虑升级或建造",
                    action: CardAction.UpgradeGate
                ));
            }

            // —— 红 R2：矿工快死光了
            if (_phase == "night" && aliveWorkers < 3)
            {
                all.Add(new CardData(
                    urgency: Urgency.Red,
                    icon: "警",
                    message: "矿工快死光了！建议刷 T5 救场",
                    action: CardAction.None   // 无跳转，仅提示横幅
                ));
            }

            // —— 黄 Y1：轮盘就绪
            if (_rouletteReady)
            {
                all.Add(new CardData(
                    urgency: Urgency.Yellow,
                    icon: "盘",
                    message: "事件轮盘已就绪",
                    action: CardAction.Roulette
                ));
            }

            // —— 黄 Y2：矿石已够升城门
            if (_phase == "day" && _variant != "recovery")
            {
                int upgradeCost = GetUpgradeCost(_gateLevel);
                if (upgradeCost > 0 && _ore >= upgradeCost)
                {
                    all.Add(new CardData(
                        urgency: Urgency.Yellow,
                        icon: "盾",
                        message: "矿石已够升城门",
                        action: CardAction.UpgradeGate
                    ));
                }
            }

            // —— 黄 Y3：建造投票
            if (_seasonDay >= 3
                && _activeBuildingsCount < 2
                && _phase == "day"
                && _variant != "recovery")
            {
                all.Add(new CardData(
                    urgency: Urgency.Yellow,
                    icon: "建",
                    message: "建议发起建造投票",
                    action: CardAction.Build
                ));
            }

            // —— 绿 G1：派矿工探险
            if (_phase == "day"
                && _variant != "recovery"
                && _activeExpeditionsCount < 2
                && _seasonDay >= 5)
            {
                all.Add(new CardData(
                    urgency: Urgency.Green,
                    icon: "探",
                    message: "可派矿工探险赚外快",
                    action: CardAction.Expedition
                ));
            }

            // —— 绿 G2：跨房攻防战
            if (_phase == "day"
                && _variant != "recovery"
                && _tribeWarState == "idle"
                && _seasonDay >= 7)
            {
                all.Add(new CardData(
                    urgency: Urgency.Green,
                    icon: "战",
                    message: "可发起跨房攻防战",
                    action: CardAction.TribeWar
                ));
            }

            // 按紧急度排序（红 0 / 黄 1 / 绿 2），最多 3 张
            all.Sort((a, b) => ((int)a.urgency).CompareTo((int)b.urgency));
            if (all.Count > 3) all.RemoveRange(3, all.Count - 3);
            return all;
        }

        /// <summary>事件驱动重算卡片并更新 UI。</summary>
        public void RefreshCards()
        {
            var cards = ComputeTopCards();

            RenderCard(_card0, cards.Count > 0 ? cards[0] : null);
            RenderCard(_card1, cards.Count > 1 ? cards[1] : null);
            RenderCard(_card2, cards.Count > 2 ? cards[2] : null);
        }

        // ==================== audit-r11 GAP-E01：BroadcasterPanel 反射兼容入口 ====================
        // BroadcasterPanel.OnExpeditionClicked / OnBuildingClicked 用反射查找以下方法名作为面板入口：
        //   OpenExpeditionPanel / ShowExpeditionControls （§38 探险）
        //   OpenBuildPropose / ShowBuildMenu / OpenBuildMenu （§37 建造）
        // r11 之前这些方法不存在 → 反射 Invoke 走异常 fallback toast 占位（用户无入口操作）
        // 当前实装：触发 _onExpeditionClicked / _onBuildClicked UnityEvent（Inspector 绑定的现有面板回调）
        // 同时高亮当前 HUD 的对应卡片，强引导用户注意

        /// <summary>§38 探险面板入口（反射兼容；对外触发 UnityEvent _onExpeditionClicked）。</summary>
        public void OpenExpeditionPanel()
        {
            _onExpeditionClicked?.Invoke();
            Debug.Log("[BroadcasterDecisionHUD] OpenExpeditionPanel: _onExpeditionClicked.Invoke()");
        }

        /// <summary>§38 探险面板入口别名（兼容 BroadcasterPanel 反射的备用名）。</summary>
        public void ShowExpeditionControls() => OpenExpeditionPanel();

        /// <summary>§37 建造投票面板入口（反射兼容；对外触发 UnityEvent _onBuildClicked）。
        /// 🔴 audit-r38 GAP-PM38-01 真闭环：当 _onBuildClicked Inspector 0 listener 时，
        ///   fallback 调 BuildVoteUI.OpenProposeMenu() 启动建造投票（r37 SendPropose 公开方法的真正入口）。
        ///   双路径并存：Inspector 已绑定 → UnityEvent 路径；未绑定 → BuildVoteUI 直发 build_propose</summary>
        public void OpenBuildPropose()
        {
            int listenerCount = _onBuildClicked != null ? _onBuildClicked.GetPersistentEventCount() : 0;
            _onBuildClicked?.Invoke();
            // r38 GAP-PM38-01：Inspector 未绑定 listener 时（默认情况）走 BuildVoteUI 直发协议路径
            if (listenerCount == 0 && BuildVoteUI.Instance != null)
            {
                BuildVoteUI.Instance.OpenProposeMenu();
                Debug.Log("[BroadcasterDecisionHUD] OpenBuildPropose: fallback BuildVoteUI.OpenProposeMenu()（r38 GAP-PM38-01 闭环）");
            }
            else
            {
                Debug.Log($"[BroadcasterDecisionHUD] OpenBuildPropose: _onBuildClicked.Invoke() (listeners={listenerCount})");
            }
        }

        /// <summary>§37 建造投票面板入口别名 1。</summary>
        public void ShowBuildMenu() => OpenBuildPropose();

        /// <summary>§37 建造投票面板入口别名 2。</summary>
        public void OpenBuildMenu() => OpenBuildPropose();

        private void RenderCard(CardView view, CardData data)
        {
            if (view == null) return;
            if (data == null)
            {
                if (view.root != null) view.root.SetActive(false);
                return;
            }

            if (view.root != null) view.root.SetActive(true);
            if (view.iconText != null) view.iconText.text = data.icon;
            if (view.messageText != null) view.messageText.text = data.message;
            if (view.bg != null)
            {
                switch (data.urgency)
                {
                    case Urgency.Red:    view.bg.color = new Color(0.85f, 0.25f, 0.25f, 0.92f); break;
                    case Urgency.Yellow: view.bg.color = new Color(0.98f, 0.85f, 0.30f, 0.92f); break;
                    case Urgency.Green:  view.bg.color = new Color(0.35f, 0.80f, 0.45f, 0.92f); break;
                }
            }

            // 跳转按钮绑定：事件驱动 UnityEvent
            if (view.jumpButton != null)
            {
                view.jumpButton.onClick.RemoveAllListeners();
                switch (data.action)
                {
                    case CardAction.UpgradeGate: view.jumpButton.onClick.AddListener(() => _onUpgradeGateClicked?.Invoke()); break;
                    case CardAction.Roulette:    view.jumpButton.onClick.AddListener(() => _onRouletteClicked?.Invoke()); break;
                    case CardAction.Build:       view.jumpButton.onClick.AddListener(() => _onBuildClicked?.Invoke()); break;
                    case CardAction.Expedition:  view.jumpButton.onClick.AddListener(() => _onExpeditionClicked?.Invoke()); break;
                    case CardAction.TribeWar:    view.jumpButton.onClick.AddListener(() => _onTribeWarClicked?.Invoke()); break;
                    case CardAction.None:
                        // R2 矿工快死光仅提示横幅，点击也不跳转；按钮可隐藏或留
                        break;
                }
                // 若无跳转则灰化按钮
                view.jumpButton.interactable = (data.action != CardAction.None);
            }
        }

        // ==================== 工具 ====================

        /// <summary>查 WorkerManager 缓存列表（CLAUDE.md 性能优化：禁止 FindObjectsOfType 热路径）。
        /// MVP 允许兜底 FindObjectsOfType，因为 HUD 重算频率低（事件驱动）。</summary>
        private int GetAliveWorkerCount()
        {
            var mgr = WorkerManager.Instance;
            if (mgr != null && mgr.ActiveWorkers != null)
            {
                int alive = 0;
                for (int i = 0; i < mgr.ActiveWorkers.Count; i++)
                {
                    var w = mgr.ActiveWorkers[i];
                    if (w != null && !w.IsDead) alive++;
                }
                return alive;
            }
            // 兜底（场景未加载 WorkerManager 时）
            var all = Object.FindObjectsOfType<DrscfZ.Survival.WorkerController>();
            int count = 0;
            foreach (var w in all) if (w != null && !w.IsDead) count++;
            return count;
        }

        /// <summary>获取 gateLevel→gateLevel+1 的升级消耗（索引 idx=level-1）。</summary>
        private static int GetUpgradeCost(int currentLevel)
        {
            int idx = currentLevel - 1;
            if (idx < 0 || idx >= GateUpgradeCosts.Length) return 0;
            return GateUpgradeCosts[idx];
        }

        // ==================== 运行时 Fallback ====================

        private void CreateRuntimeHudFallback()
        {
            // 建最小化容器：垂直排列 3 张卡
            var parentRT = GetComponent<RectTransform>();
            if (parentRT == null) parentRT = gameObject.AddComponent<RectTransform>();

            var rootGO = new GameObject("DecisionHUD_Root");
            rootGO.transform.SetParent(parentRT, false);
            var rootRT = rootGO.AddComponent<RectTransform>();
            rootRT.anchorMin = new Vector2(0f, 1f);
            rootRT.anchorMax = new Vector2(0f, 1f);
            rootRT.pivot     = new Vector2(0f, 1f);
            rootRT.anchoredPosition = new Vector2(10f, -10f);
            rootRT.sizeDelta = new Vector2(340f, 240f);

            var vlg = rootGO.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment      = TextAnchor.UpperLeft;
            vlg.spacing             = 4f;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = false;
            vlg.padding = new RectOffset(4, 4, 4, 4);

            _cardsRoot = rootRT;
            _card0 = CreateCardView(rootRT, "Card0");
            _card1 = CreateCardView(rootRT, "Card1");
            _card2 = CreateCardView(rootRT, "Card2");
        }

        private CardView CreateCardView(RectTransform parent, string name)
        {
            var cv = new CardView();

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 70f);

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 70f;

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.85f, 0.25f, 0.25f, 0.92f);
            bg.raycastTarget = true;

            cv.root         = go;
            cv.bg           = bg;

            // icon
            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(go.transform, false);
            var iconRT = iconGO.AddComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0f, 0f);
            iconRT.anchorMax = new Vector2(0.15f, 1f);
            iconRT.offsetMin = Vector2.zero;
            iconRT.offsetMax = Vector2.zero;
            var iconTMP = iconGO.AddComponent<TextMeshProUGUI>();
            // AUTO-INJECT: 统一 Alibaba 字体
            if (iconTMP.font == null) {
                var __f = Resources.Load<TMPro.TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF");
                if (__f == null) __f = Resources.Load<TMPro.TMP_FontAsset>("Fonts/ChineseFont SDF");
                if (__f != null) iconTMP.font = __f;
            }
            iconTMP.fontSize = 28f;
            iconTMP.color = Color.white;
            iconTMP.alignment = TextAlignmentOptions.Center;
            iconTMP.raycastTarget = false;
            cv.iconText = iconTMP;

            // message
            var msgGO = new GameObject("Message");
            msgGO.transform.SetParent(go.transform, false);
            var msgRT = msgGO.AddComponent<RectTransform>();
            msgRT.anchorMin = new Vector2(0.15f, 0f);
            msgRT.anchorMax = new Vector2(0.78f, 1f);
            msgRT.offsetMin = new Vector2(4f, 0f);
            msgRT.offsetMax = new Vector2(-4f, 0f);
            var msgTMP = msgGO.AddComponent<TextMeshProUGUI>();
            // AUTO-INJECT: 统一 Alibaba 字体
            if (msgTMP.font == null) {
                var __f = Resources.Load<TMPro.TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF");
                if (__f == null) __f = Resources.Load<TMPro.TMP_FontAsset>("Fonts/ChineseFont SDF");
                if (__f != null) msgTMP.font = __f;
            }
            msgTMP.fontSize = 20f;
            msgTMP.color = Color.white;
            msgTMP.alignment = TextAlignmentOptions.MidlineLeft;
            msgTMP.enableAutoSizing = false;
            msgTMP.raycastTarget = false;
            cv.messageText = msgTMP;

            // jump button
            var btnGO = new GameObject("JumpBtn");
            btnGO.transform.SetParent(go.transform, false);
            var btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.78f, 0.1f);
            btnRT.anchorMax = new Vector2(0.98f, 0.9f);
            btnRT.offsetMin = Vector2.zero;
            btnRT.offsetMax = Vector2.zero;
            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = new Color(1f, 1f, 1f, 0.2f);
            var btn = btnGO.AddComponent<Button>();
            cv.jumpButton = btn;

            var btnLblGO = new GameObject("Label");
            btnLblGO.transform.SetParent(btnGO.transform, false);
            var lblRT = btnLblGO.AddComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero;
            lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = Vector2.zero;
            lblRT.offsetMax = Vector2.zero;
            var lblTMP = btnLblGO.AddComponent<TextMeshProUGUI>();
            // AUTO-INJECT: 统一 Alibaba 字体
            if (lblTMP.font == null) {
                var __f = Resources.Load<TMPro.TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF");
                if (__f == null) __f = Resources.Load<TMPro.TMP_FontAsset>("Fonts/ChineseFont SDF");
                if (__f != null) lblTMP.font = __f;
            }
            lblTMP.text = "前往";
            lblTMP.fontSize = 18f;
            lblTMP.color = Color.white;
            lblTMP.alignment = TextAlignmentOptions.Center;
            lblTMP.raycastTarget = false;

            go.SetActive(false);
            return cv;
        }

        // ==================== 嵌套类型 ====================

        [System.Serializable]
        public class CardView
        {
            public GameObject root;
            public Image      bg;
            public TMP_Text   iconText;
            public TMP_Text   messageText;
            public Button     jumpButton;
        }

        public enum Urgency { Red = 0, Yellow = 1, Green = 2 }

        public enum CardAction { None, UpgradeGate, Roulette, Build, Expedition, TribeWar }

        public class CardData
        {
            public Urgency    urgency;
            public string     icon;
            public string     message;
            public CardAction action;

            public CardData(Urgency urgency, string icon, string message, CardAction action)
            {
                this.urgency = urgency;
                this.icon    = icon;
                this.message = message;
                this.action  = action;
            }
        }
    }
}
