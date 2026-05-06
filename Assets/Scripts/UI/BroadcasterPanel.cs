using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrscfZ.Core;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// 主播控制面板 - 仅房间创建者（主播）可见
    ///
    /// 布局（Canvas基准 1080×1920）：
    ///   面板：右侧中下部，200×280px
    ///   BoostButton：⚡ 紧急加速，120×120px，CD=120s
    ///   EventButton：🌊 触发事件，120×120px，CD=60s
    ///
    /// 规则7：脚本挂在 always-active 的父对象上（如 Canvas 根节点或同级 always-active GO）
    ///         BroadcasterPanel GameObject 通过 SetActive(true/false) 控制显隐
    ///
    /// 规则2：UI对象在Scene中预创建；禁止运行时Instantiate
    /// 规则5：位置不直接覆盖，使用SetActive控制显隐而非移动
    /// </summary>
    public class BroadcasterPanel : MonoBehaviour
    {
        public static BroadcasterPanel Instance { get; private set; }

        // ==================== Inspector引用 ====================

        [Header("面板根对象（通过SetActive控制显隐）")]
        [SerializeField] private GameObject _panelRoot;

        [Header("⚡ 紧急加速按钮")]
        [SerializeField] private Button _boostBtn;
        [SerializeField] private Image  _boostBtnBg;
        [SerializeField] private TMP_Text _boostCdText;

        [Header("🌊 触发事件按钮")]
        [SerializeField] private Button   _eventBtn;
        [SerializeField] private Image    _eventBtnBg;
        [SerializeField] private TMP_Text _eventCdText;

        [Header("🎰 主播事件轮盘按钮（§24.4 🆕 v1.27，委托给 RouletteUI）")]
        [SerializeField] private Button   _rouletteButton;

        [Header("🛒 商店 Tab 按钮（§39 🆕 v1.27，委托给 ShopUI；Prefab 绑定由人工补）")]
        [SerializeField] private Button   _shopTabButton;

        [Header("⚔️ 跨直播间攻防战按钮（§35 🆕 v1.27，委托给 TribeWarLobbyUI；Prefab 绑定由人工补）")]
        [SerializeField] private Button   _tribeWarButton;

        [Header("🛡 升级城门按钮（🆕 v1.22 §10，可留空；场景中通过 Editor 工具绑定）")]
        [SerializeField] private Button _btnUpgradeGate;

        // audit-r6 P0-F3：补齐 §36.12 3 个缺失 feature 按钮入口（building/expedition/supporter_mode）
        [Header("🏗️ 建造（§37，D3 解锁）")]
        [SerializeField] private Button _buildingButton;

        [Header("🧭 探险（§38，D5 解锁）")]
        [SerializeField] private Button _expeditionButton;

        [Header("🤝 助威模式（§33，D6 解锁）")]
        [SerializeField] private Button _supporterModeButton;


        [Header("§17.15 关闭引导按钮（🆕 v1.27；点击后服务端房间会话内不再广播 B1-B3 气泡）")]
        [SerializeField] private Button _btnDisableOnboarding;

        [Header("§17.15 主播话术卡（🆕 v1.27；默认/入夜/低血三态 TMP 文本）")]
        [SerializeField] private TMP_Text _broadcasterTipText;
        [SerializeField] private string _tipDefault = "欢迎来到极地生存法则！白天刷仙女棒帮矿工，夜晚刷甜甜圈保护城门！";
        [SerializeField] private string _tipNight   = "快刷礼物！怪物要攻城了！";
        [SerializeField] private string _tipLowGate = "城门快破了！刷甜甜圈救场！";
        [Tooltip("§17.15 低血话术触发阈值：gateHp < 此值时显示 _tipLowGate（默认 300）")]
        // audit-r6 P1-F6：从硬编码 300 改为比例化（低血提示随 Gate.MaxHp 动态计算）
        // 保留 SerializeField 兼容旧场景绑定；若 Ratio>0 则优先使用 Ratio*MaxHp 作为阈值
        [SerializeField] private int _tipLowGateThreshold = 300;
        [SerializeField, Range(0f, 1f)] private float _tipLowGateRatio = 0.3f;

        // ==================== 🆕 v1.22 §10 升级常量表（与服务端 SurvivalGameEngine.js 顶部常量对齐）====================

        // 索引 i 对应 gateLevel=i+1 → gateLevel=i+2 的升级消耗
        // Lv1→Lv2=100, Lv2→Lv3=250, Lv3→Lv4=500, Lv4→Lv5=1000, Lv5→Lv6=1500
        private static readonly int[] _upgradeCostTable = new[] { 100, 250, 500, 1000, 1500 };

        // 索引 i 对应 gateLevel=i+1 的层级名（与策划案 §10.2 / GATE_TIER_NAMES 对齐）
        private static readonly string[] _tierNameTable = new[]
        {
            "木栅栏",    // Lv1
            "加固木门",  // Lv2（自动修复 ×1.5）
            "铁皮厚门",  // Lv3（减伤 10%）
            "钢骨堡门",  // Lv4（反伤 20%）
            "寒冰壁垒",  // Lv5（冰霜光环 6m）
            "巨龙要塞",  // Lv6（寒冰冲击波 15s/8m/100伤）
        };

        // 下一级新解锁的特性说明（展示在升级确认弹窗；索引 i 对应升级到 gateLevel=i+1）
        private static readonly string[] _nextFeatureDescTable = new[]
        {
            "",                                                           // 不会被使用（Lv1 初始）
            "加固木门：MaxHP 1500 + 自动修复 ×1.5",                       // → Lv2
            "铁皮厚门：MaxHP 2200 + 减伤 10%",                            // → Lv3
            "钢骨堡门：MaxHP 3000 + 反伤 20%",                            // → Lv4
            "寒冰壁垒：MaxHP 4000 + 冰霜光环（6m 减速 0.7×）",            // → Lv5
            "巨龙要塞：MaxHP 5500 + 寒冰冲击波（15s/8m/100伤+2s冻结）",   // → Lv6
        };

        // ==================== 颜色常量 ====================

        // ⚡ 按钮：可用/CD两种状态背景色
        private static readonly Color BoostBgReady    = new Color(0x2A / 255f, 0x3A / 255f, 0x10 / 255f, 1f); // #2A3A10
        private static readonly Color BoostBgCooldown = new Color(0x1A / 255f, 0x1A / 255f, 0x2A / 255f, 1f); // #1A1A2A

        // 🌊 按钮：可用/CD两种状态背景色
        private static readonly Color EventBgReady    = new Color(0x0A / 255f, 0x2A / 255f, 0x1A / 255f, 1f); // #0A2A1A
        private static readonly Color EventBgCooldown = new Color(0x1A / 255f, 0x1A / 255f, 0x2A / 255f, 1f); // #1A1A2A

        // ==================== CD参数 ====================

        private const float BOOST_CD = 120f;
        private const float EVENT_CD = 60f;

        private float _boostCd = 0f;
        private float _eventCd = 0f;
        private bool _boostPending = false;
        private bool _eventPending = false;

        // ==================== 状态 ====================

        private bool _isRoomCreator = false;

        // ==================== 生命周期 ====================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 规则2：默认隐藏面板，等待服务器确认后再显示
            if (_panelRoot != null)
                _panelRoot.SetActive(false);
        }

        private void Start()
        {
            // 挂载按钮监听
            if (_boostBtn != null)
                _boostBtn.onClick.AddListener(OnBoostClicked);
            if (_eventBtn != null)
                _eventBtn.onClick.AddListener(OnEventClicked);
            if (_rouletteButton != null)
                _rouletteButton.onClick.AddListener(OnRouletteClicked);
            if (_shopTabButton != null)
                _shopTabButton.onClick.AddListener(OnShopClicked);
            if (_tribeWarButton != null)
                _tribeWarButton.onClick.AddListener(OnTribeWarClicked);
            // 🆕 v1.22 §10 升级城门按钮
            if (_btnUpgradeGate != null)
                _btnUpgradeGate.onClick.AddListener(OnUpgradeGateClick);

            // audit-r6 P0-F3：3 新按钮 click 绑定
            if (_buildingButton != null)
                _buildingButton.onClick.AddListener(OnBuildingClicked);
            if (_expeditionButton != null)
                _expeditionButton.onClick.AddListener(OnExpeditionClicked);
            if (_supporterModeButton != null)
                _supporterModeButton.onClick.AddListener(OnSupporterModeClicked);

            // 🆕 §17.15 关闭引导按钮
            if (_btnDisableOnboarding != null)
                _btnDisableOnboarding.onClick.AddListener(OnDisableOnboardingClick);

            // 初始化UI状态
            ResetBoostBtn();
            ResetEventBtn();

            // 订阅网络消息
            var net = NetworkManager.Instance;
            if (net != null)
            {
                net.OnMessageReceived += HandleMessage;
                net.OnConnected       += HandleConnected;
                net.OnDisconnected    += HandleDisconnected;
            }

            // 🆕 §17.15 主播话术卡：订阅 phase_changed / resource_update
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnPhaseChanged   += HandlePhaseChangedForTip;
                sgm.OnResourceUpdate += HandleResourceUpdateForTip;
            }
            // 初始化话术（默认）
            RefreshBroadcasterTip(phase: "day", gateHp: int.MaxValue);
        }

        private void OnDestroy()
        {
            var net = NetworkManager.Instance;
            if (net != null)
            {
                net.OnMessageReceived -= HandleMessage;
                net.OnConnected       -= HandleConnected;
                net.OnDisconnected    -= HandleDisconnected;
            }
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnPhaseChanged   -= HandlePhaseChangedForTip;
                sgm.OnResourceUpdate -= HandleResourceUpdateForTip;
            }
        }

        private void Update()
        {
            // ⚡ 冷却倒计时
            if (_boostCd > 0f)
            {
                _boostCd -= Time.deltaTime;
                if (_boostCd <= 0f)
                {
                    _boostCd = 0f;
                    ResetBoostBtn();
                }
                else if (_boostCdText != null)
                {
                    _boostCdText.text = $"CD {Mathf.CeilToInt(_boostCd)}s";
                }
            }

            // 🌊 冷却倒计时
            if (_eventCd > 0f)
            {
                _eventCd -= Time.deltaTime;
                if (_eventCd <= 0f)
                {
                    _eventCd = 0f;
                    ResetEventBtn();
                }
                else if (_eventCdText != null)
                {
                    _eventCdText.text = $"CD {Mathf.CeilToInt(_eventCd)}s";
                }
            }
        }

        // ==================== 网络消息处理 ====================

        /// <summary>
        /// 处理服务器消息。
        /// OnMessageReceived 签名为 (string type, string dataJson)，与NetworkManager一致。
        /// </summary>
        private void HandleMessage(string type, string dataJson)
        {
            switch (type)
            {
                case "join_room_confirm":
                    // 服务器在客户端join_room后回复，告知是否为房间创建者
                    // dataJson 示例: {"isRoomCreator":true}
                    _isRoomCreator = ParseBoolField(dataJson, "isRoomCreator");
                    if (_panelRoot != null)
                        _panelRoot.SetActive(_isRoomCreator);
                    Debug.Log($"[BroadcasterPanel] join_room_confirm received, isRoomCreator={_isRoomCreator}");
                    break;

                case "broadcaster_effect":
                    // 服务器广播效果回来（服务器端处理后广播给所有客户端，包括主播自己）
                    HandleBroadcasterEffect(dataJson);
                    break;

                case "broadcaster_action_failed":
                    HandleBroadcasterActionFailed(dataJson);
                    break;
            }
        }

        private void HandleConnected()
        {
            // 重置状态（重连时可能变更主播身份）
            _isRoomCreator = false;
            if (_panelRoot != null)
                _panelRoot.SetActive(false);
        }

        private void HandleDisconnected(string reason)
        {
            _isRoomCreator = false;
            if (_panelRoot != null)
                _panelRoot.SetActive(false);
        }

        // ==================== 按钮点击 ====================

        private void OnBoostClicked()
        {
            if (_boostCd > 0f || _boostPending) return;
            if (IsKnownLocked(SurvivalMessageProtocol.FeatureBroadcasterBoost, "紧急加速")) return;

            // 发送消息到服务器
            // NetworkManager只有 SendMessage(string type) 和 SendJson(string json)
            // 使用 SendJson 携带 action 字段
            var net = NetworkManager.Instance;
            if (net == null || !net.IsConnected) return;

            // audit-r11 GAP-B04：删除 cooldown 字段（服务端不读，r9-r10 客户端冗余）；duration=30000 仍服务端读
            string json = $"{{\"type\":\"broadcaster_action\",\"data\":{{\"action\":\"efficiency_boost\",\"duration\":30000}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}";
            net.SendJson(json);

            _boostPending = true;
            SetBoostPendingState();
            Debug.Log("[BroadcasterPanel] ⚡ efficiency_boost 已发送");
        }

        private void OnEventClicked()
        {
            if (_eventCd > 0f || _eventPending) return;
            if (IsKnownLocked(SurvivalMessageProtocol.FeatureBroadcasterBoost, "主播事件")) return;

            var net = NetworkManager.Instance;
            if (net == null || !net.IsConnected) return;

            // audit-r11 GAP-B04：删除 cooldown 字段（服务端不读，r9-r10 客户端冗余）
            string json = $"{{\"type\":\"broadcaster_action\",\"data\":{{\"action\":\"trigger_event\"}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}";
            net.SendJson(json);

            _eventPending = true;
            SetEventPendingState();
            Debug.Log("[BroadcasterPanel] 🌊 trigger_event 已发送");
        }

        /// <summary>
        /// 🎰 主播事件轮盘（§24.4）— 委托给 RouletteUI。
        /// 充能状态/就绪/转轴动画全部在 RouletteUI 内部处理；本按钮仅是入口。
        /// 按钮的"就绪发光/充能灰蓝/剩余秒数"由 RouletteUI 直接操作其内部 btnSpin 引用，
        /// 若 _rouletteButton 与 RouletteUI.btnSpin 为同一 GameObject，逻辑完全重合；
        /// 否则（MVP 场景预创建可能仅有入口按钮）OnClick 等同 Spin 请求。
        /// </summary>
        private void OnRouletteClicked()
        {
            if (IsKnownLocked(SurvivalMessageProtocol.FeatureRoulette, "事件轮盘")) return;
            RouletteUI.Instance?.OpenPanel();
        }

        /// <summary>
        /// 🛒 商店入口（§39）— 默认切到 A 类战术道具 Tab。
        /// Prefab 绑定待补：_shopTabButton 需要人工在 Inspector 中拖入 Shop Tab Button 引用；
        /// ShopUI.Instance 须挂在 Canvas 下的 ShopPanel（always-active 父对象），
        /// 否则点击时会 Log 占位。
        /// </summary>
        private void OnShopClicked()
        {
            if (IsKnownLocked(SurvivalMessageProtocol.FeatureShop, "商店系统")) return;
            if (ShopUI.Instance != null)
            {
                ShopUI.Instance.OpenPanel("A");
            }
            else
            {
                Debug.LogWarning("[BroadcasterPanel] OnShopClicked：ShopUI.Instance 为 null，检查是否挂载 ShopUI 脚本");
            }
        }

        /// <summary>
        /// ⚔️ 跨直播间攻防战入口（§35）— 打开大厅面板并请求最新房间列表。
        /// Prefab 绑定待补：_tribeWarButton 需要人工在 Inspector 中拖入；
        /// TribeWarLobbyUI.Instance 须挂在 Canvas 下的 TribeWarLobbyPanel（always-active 父对象），
        /// 否则点击时会 Log 占位。
        /// </summary>
        private void OnTribeWarClicked()
        {
            if (IsKnownLocked(SurvivalMessageProtocol.FeatureTribeWar, "跨房攻防战")) return;
            if (TribeWarLobbyUI.Instance != null)
            {
                TribeWarLobbyUI.Instance.OpenPanel();
            }
            else
            {
                Debug.LogWarning("[BroadcasterPanel] OnTribeWarClicked：TribeWarLobbyUI.Instance 为 null，检查是否挂载 TribeWarLobbyUI 脚本");
            }
        }

        // ==================== audit-r6 P0-F3：§36.12 三个新按钮入口 ====================

        /// <summary>
        /// 🏗️ 建造系统入口（§37，D3 解锁）。
        /// 按钮实际受 FeatureLockOverlay(featureId=building) 视觉守门，
        /// 点击后由 BroadcasterDecisionHUD 接管发起 build_propose 投票流程；
        /// 若面板未挂则 Log 占位。
        /// </summary>
        private void OnBuildingClicked()
        {
            if (IsKnownLocked(SurvivalMessageProtocol.FeatureBuilding, "建造系统")) return;
            // 服务端 feature 锁定由 FeatureLockOverlay 展示；点击仍可发起（服务端拒后回 build_propose_failed）
            var hud = BroadcasterDecisionHUD.Instance;
            if (hud != null)
            {
                // BroadcasterDecisionHUD 有 OpenBuildPropose / ShowBuildMenu 类方法时调用（反射兼容）
                try
                {
                    var method = hud.GetType().GetMethod("OpenBuildPropose")
                               ?? hud.GetType().GetMethod("ShowBuildMenu")
                               ?? hud.GetType().GetMethod("OpenBuildMenu");
                    if (method != null)
                    {
                        method.Invoke(hud, null);
                        Debug.Log("[BroadcasterPanel] 🏗️ OnBuildingClicked → BroadcasterDecisionHUD");
                        return;
                    }
                }
                catch { /* fall through to placeholder */ }
            }
            Debug.LogWarning("[BroadcasterPanel] OnBuildingClicked：BroadcasterDecisionHUD 未就绪（或无 OpenBuildPropose），D3 解锁后请 Setup 脚本补齐");
            AnnouncementUI.Instance?.ShowAnnouncement("建造系统", "请稍候，建造菜单加载中...", new Color(0.6f, 0.9f, 0.6f), 2f);
        }

        /// <summary>
        /// 🧭 探险系统入口（§38，D5 解锁）。点击触发召回面板或下发 expedition_command (recall)。
        /// </summary>
        private void OnExpeditionClicked()
        {
            if (IsKnownLocked(SurvivalMessageProtocol.FeatureExpedition, "探险系统")) return;
            var hud = BroadcasterDecisionHUD.Instance;
            if (hud != null)
            {
                try
                {
                    var method = hud.GetType().GetMethod("OpenExpeditionPanel")
                               ?? hud.GetType().GetMethod("ShowExpeditionControls");
                    if (method != null)
                    {
                        method.Invoke(hud, null);
                        Debug.Log("[BroadcasterPanel] 🧭 OnExpeditionClicked → BroadcasterDecisionHUD");
                        return;
                    }
                }
                catch { /* fall through */ }
            }
            SendExpeditionCommand("send");
        }

        /// <summary>
        /// 🤝 助威模式入口（§33，D6 解锁）。打开设置面板的 supporter tab；MVP 期 toast 占位。
        /// </summary>
        private void OnSupporterModeClicked()
        {
            if (IsKnownLocked(SurvivalMessageProtocol.FeatureSupporterMode, "助威模式")) return;
            var settings = FindObjectOfType<SettingsPanelUI>(true);
            if (settings != null)
            {
                try
                {
                    var method = settings.GetType().GetMethod("OpenSupporterTab")
                               ?? settings.GetType().GetMethod("Show");
                    if (method != null)
                    {
                        method.Invoke(settings, null);
                        Debug.Log("[BroadcasterPanel] 🤝 OnSupporterModeClicked → SettingsPanelUI");
                        return;
                    }
                }
                catch { /* fall through */ }
            }
            AnnouncementUI.Instance?.ShowAnnouncement("助威模式", "观众满员后可助威支持！", new Color(0.8f, 0.55f, 0.9f), 2f);
            Debug.Log("[BroadcasterPanel] 🤝 OnSupporterModeClicked（MVP toast 占位）");
        }

        // ==================== 🆕 v1.22 §10 升级城门 ====================

        private void OnUpgradeGateClick()
        {
            if (IsKnownLocked(SurvivalMessageProtocol.FeatureGateUpgradeBasic, "城门升级")) return;
            var gate = CityGateSystem.Instance;
            if (gate == null)
            {
                Debug.LogWarning("[BroadcasterPanel] CityGateSystem.Instance 为空，无法升级");
                return;
            }
            if (gate.GateLevel >= 6)
            {
                AnnouncementUI.Instance?.ShowAnnouncement(
                    "城门已满级", "Lv.6 巨龙要塞已是最高等级！", new Color(1f, 0.7f, 0.2f), 2f);
                return;
            }

            // 🔴 audit-r39 GAP-PM39-03：daily_limit 客户端 pre-check（避免点击 → 弹确认 → 服务端拒错误体验链）
            //   策划案 §10.5 明确：每日仅可升级 1 次（_gateUpgradedToday，与 expedition_trader 共享）。
            //   r38 之前 OnUpgradeGateClick 0 处 DailyUpgraded pre-check：玩家点按钮 → GateUpgradeConfirmUI 弹窗 →
            //   "确认升级" → 服务端 daily_limit 拒 → 玩家收到 gate_upgrade_failed { reason: 'daily_limit' } toast。
            //   两步操作后才知道今日已升级，错误体验。r39 客户端 pre-check：直接 toast "今日已升级"，不弹确认窗。
            //   服务端 daily_limit 拒绝路径仍保留作权威回退。
            if (gate.DailyUpgraded)
            {
                AnnouncementUI.Instance?.ShowAnnouncement(
                    "今日已升级", "城门每日仅可升级 1 次（与探险商队共享额度），明日 UTC+8 05:00 重置", new Color(1f, 0.7f, 0.2f), 2.5f);
                return;
            }

            int currentLv = gate.GateLevel;
            int nextLv    = currentLv + 1;
            int cost      = GetUpgradeCost(currentLv);
            string nextTier = GetTierName(nextLv);
            string desc     = GetNextFeatureDesc(nextLv);

            var confirmUI = GateUpgradeConfirmUI.Instance;
            if (confirmUI == null)
            {
                Debug.LogWarning("[BroadcasterPanel] GateUpgradeConfirmUI.Instance 为空。" +
                                 "请运行 Tools → DrscfZ → Create Gate Upgrade UI 生成 UI。");
                // 回退：直接发送升级请求
                SendUpgradeGateRequest();
                return;
            }

            confirmUI.ShowConfirm(currentLv, nextLv, cost, nextTier, desc,
                onConfirm: SendUpgradeGateRequest);
        }

        private void SendUpgradeGateRequest()
        {
            var net = NetworkManager.Instance;
            if (net == null || !net.IsConnected)
            {
                Debug.LogWarning("[BroadcasterPanel] 网络未连接，无法发送 upgrade_gate");
                return;
            }

            // 协议：{ type: 'upgrade_gate', data: { secOpenId, source } }
            // secOpenId 主播身份由服务端自行识别（基于连接会话），此处留空
            long ts = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string json = $"{{\"type\":\"upgrade_gate\",\"data\":{{\"secOpenId\":\"\",\"source\":\"broadcaster\"}},\"timestamp\":{ts}}}";
            net.SendJson(json);
            Debug.Log("[BroadcasterPanel] 🛡 upgrade_gate 已发送");
        }

        public void OpenUpgradeGatePanel() => OnUpgradeGateClick();
        public void OpenRoulettePanel() => OnRouletteClicked();
        public void OpenShopPanel() => OnShopClicked();
        public void OpenTribeWarPanel() => OnTribeWarClicked();
        public void OpenBuildingPanel() => OnBuildingClicked();
        public void OpenExpeditionPanel() => OnExpeditionClicked();

        public void SendExpeditionCommand(string action = "send")
        {
            var net = NetworkManager.Instance;
            if (net == null || !net.IsConnected)
            {
                Debug.LogWarning("[BroadcasterPanel] 网络未连接，无法发送 expedition_command");
                return;
            }

            string safeAction = action == "recall" ? "recall" : "send";
            long ts = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string json = $"{{\"type\":\"expedition_command\",\"data\":{{\"action\":\"{safeAction}\"}},\"timestamp\":{ts}}}";
            net.SendJson(json);
            Debug.Log($"[BroadcasterPanel] 🧭 expedition_command 已发送 action={safeAction}");
        }

        /// <summary>获取当前等级升级至下一级的矿石消耗</summary>
        private static int GetUpgradeCost(int currentLevel)
        {
            int idx = currentLevel - 1;
            if (idx < 0 || idx >= _upgradeCostTable.Length) return 0;
            return _upgradeCostTable[idx];
        }

        private static string GetTierName(int level)
        {
            int idx = level - 1;
            if (idx < 0 || idx >= _tierNameTable.Length) return "";
            return _tierNameTable[idx];
        }

        private static string GetNextFeatureDesc(int nextLevel)
        {
            int idx = nextLevel - 1;
            if (idx < 0 || idx >= _nextFeatureDescTable.Length) return "";
            return _nextFeatureDescTable[idx];
        }

        // ==================== broadcaster_effect 全屏反馈 ====================

        private void HandleBroadcasterEffect(string dataJson)
        {
            string action = ParseStringField(dataJson, "action");

            if (action == "efficiency_boost")
            {
                _boostPending = false;
                _boostCd = BOOST_CD;
                SetBoostCdState(true);
                // 屏幕顶部大字公告
                AnnouncementUI.Instance?.ShowAnnouncement(
                    "【加速】主播激活紧急加速！",
                    "全体效率翻倍30秒！",
                    new Color(1f, 1f, 0f),  // #FFFF00
                    3f
                );
                Debug.Log("[BroadcasterPanel] broadcaster_effect: efficiency_boost 显示公告");
            }
            else if (action == "trigger_event")
            {
                _eventPending = false;
                _eventCd = EVENT_CD;
                SetEventCdState(true);
                // 随机事件公告由服务器的 bobao 广播处理，这里显示通用提示
                AnnouncementUI.Instance?.ShowAnnouncement(
                    "【事件】主播触发了随机事件！",
                    "好事or坏事？拭目以待！",
                    new Color(0.27f, 1f, 0.53f), // #44FF88
                    3f
                );
                Debug.Log("[BroadcasterPanel] broadcaster_effect: trigger_event 显示公告");
            }
        }

        private void HandleBroadcasterActionFailed(string dataJson)
        {
            string action = ParseStringField(dataJson, "action");
            string reason = ParseStringField(dataJson, "reason");
            if (action == "efficiency_boost")
            {
                _boostPending = false;
                ResetBoostBtn();
            }
            else if (action == "trigger_event")
            {
                _eventPending = false;
                ResetEventBtn();
            }

            AnnouncementUI.Instance?.ShowAnnouncement(
                "主播操作失败",
                FormatBroadcasterActionFailReason(reason),
                new Color(1f, 0.65f, 0.35f),
                2f);
        }

        // ==================== 按钮状态管理 ====================

        private void SetBoostCdState(bool inCooldown)
        {
            if (_boostBtn != null)
                _boostBtn.interactable = !inCooldown;
            if (_boostBtnBg != null)
                _boostBtnBg.color = inCooldown ? BoostBgCooldown : BoostBgReady;
            if (_boostCdText != null)
                _boostCdText.gameObject.SetActive(inCooldown);
        }

        private void SetBoostPendingState()
        {
            if (_boostBtn != null)
                _boostBtn.interactable = false;
            if (_boostBtnBg != null)
                _boostBtnBg.color = BoostBgCooldown;
            if (_boostCdText != null)
            {
                _boostCdText.gameObject.SetActive(true);
                _boostCdText.text = "Sending";
            }
        }

        private void SetEventCdState(bool inCooldown)
        {
            if (_eventBtn != null)
                _eventBtn.interactable = !inCooldown;
            if (_eventBtnBg != null)
                _eventBtnBg.color = inCooldown ? EventBgCooldown : EventBgReady;
            if (_eventCdText != null)
                _eventCdText.gameObject.SetActive(inCooldown);
        }

        private void SetEventPendingState()
        {
            if (_eventBtn != null)
                _eventBtn.interactable = false;
            if (_eventBtnBg != null)
                _eventBtnBg.color = EventBgCooldown;
            if (_eventCdText != null)
            {
                _eventCdText.gameObject.SetActive(true);
                _eventCdText.text = "Sending";
            }
        }

        private void ResetBoostBtn()
        {
            _boostPending = false;
            SetBoostCdState(false);
        }

        private void ResetEventBtn()
        {
            _eventPending = false;
            SetEventCdState(false);
        }

        private static bool IsKnownLocked(string featureId, string displayName)
        {
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null || sgm.CurrentUnlockedFeatures == null)
                return true;

            if (sgm.IsFeatureUnlocked(featureId))
                return false;

            AnnouncementUI.Instance?.ShowAnnouncement(
                "功能未解锁",
                $"{displayName} 尚未解锁",
                new Color(0.7f, 0.85f, 1f),
                2f);
            Debug.LogWarning($"[BroadcasterPanel] Feature locked: {featureId}");
            return true;
        }

        private static string FormatBroadcasterActionFailReason(string reason)
        {
            switch (reason)
            {
                case "feature_locked": return "功能尚未解锁";
                case "cooldown":
                case "in_cooldown": return "技能冷却中";
                case "wrong_phase": return "当前阶段不能使用";
                case "not_broadcaster": return "仅主播可操作";
                default: return string.IsNullOrEmpty(reason) ? "服务器拒绝了本次操作" : $"服务器拒绝：{reason}";
            }
        }

        // ==================== JSON 字段解析（轻量，不引入第三方库）====================

        /// <summary>
        /// 从 dataJson 中提取布尔字段值。
        /// 例如 {"isRoomCreator":true} → ParseBoolField(json, "isRoomCreator") → true
        /// </summary>
        private static bool ParseBoolField(string json, string fieldName)
        {
            if (string.IsNullOrEmpty(json)) return false;
            string key = $"\"{fieldName}\":";
            int idx = json.IndexOf(key);
            if (idx < 0) return false;
            int start = idx + key.Length;
            while (start < json.Length && json[start] == ' ') start++;
            // 读取值（直到逗号、}、空白）
            int end = start;
            while (end < json.Length && json[end] != ',' && json[end] != '}' && json[end] != ' ') end++;
            string value = json.Substring(start, end - start).Trim();
            return value == "true" || value == "1";
        }

        /// <summary>
        /// 从 dataJson 中提取字符串字段值（有引号的值）。
        /// 例如 {"action":"efficiency_boost"} → ParseStringField(json, "action") → "efficiency_boost"
        /// </summary>
        private static string ParseStringField(string json, string fieldName)
        {
            if (string.IsNullOrEmpty(json)) return "";
            string key = $"\"{fieldName}\":\"";
            int idx = json.IndexOf(key);
            if (idx < 0) return "";
            int start = idx + key.Length;
            int end = json.IndexOf('"', start);
            if (end < 0) return "";
            return json.Substring(start, end - start);
        }

        // ==================== §17.15 关闭引导 / 主播话术卡 ====================

        /// <summary>主播点击"关闭引导"按钮：委托 SurvivalGameManager 发送 disable_onboarding_for_session
        /// + 本地 OnboardingBubbleUI.DisableLocal() 立即打断协程。</summary>
        private void OnDisableOnboardingClick()
        {
            SurvivalGameManager.Instance?.SendDisableOnboarding();
            OnboardingBubbleUI.Instance?.DisableLocal();
            Debug.Log("[BroadcasterPanel] §17.15 关闭引导按钮点击：disable_onboarding_for_session 已发送 + 本地序列已停止");
        }

        private void HandlePhaseChangedForTip(PhaseChangedData pc)
        {
            if (pc == null) return;
            int gateHp = CityGateSystem.Instance != null ? CityGateSystem.Instance.CurrentHp : int.MaxValue;
            RefreshBroadcasterTip(pc.phase, gateHp);
        }

        private void HandleResourceUpdateForTip(ResourceUpdateData ru)
        {
            if (ru == null) return;
            string phase = SurvivalGameManager.Instance != null
                && SurvivalGameManager.Instance.CurrentSeasonState != null
                ? (SurvivalGameManager.Instance.CurrentSeasonState.phase ?? "day")
                : "day";
            RefreshBroadcasterTip(phase, ru.gateHp);
        }

        /// <summary>§17.15 主播话术卡三态切换。优先级：低血 > 入夜 > 默认。
        /// gateHp 兜底：<= 0 视作首帧未就绪，不切换低血态。
        /// audit-r6 P1-F6：阈值按 Gate.MaxHp × _tipLowGateRatio 动态计算；_tipLowGateThreshold 作为最终地板。</summary>
        private void RefreshBroadcasterTip(string phase, int gateHp)
        {
            if (_broadcasterTipText == null) return;
            string text;
            int threshold = _tipLowGateThreshold;
            var gate = CityGateSystem.Instance;
            if (gate != null && gate.MaxHp > 0 && _tipLowGateRatio > 0f)
                threshold = Mathf.Max(_tipLowGateThreshold, Mathf.RoundToInt(gate.MaxHp * _tipLowGateRatio));
            if (gateHp > 0 && gateHp < threshold)
                text = _tipLowGate;
            else if (phase == "night")
                text = _tipNight;
            else
                text = _tipDefault;
            _broadcasterTipText.text = text;
        }
    }
}
