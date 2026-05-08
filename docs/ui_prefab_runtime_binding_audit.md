# UI Prefab 运行时绑定深度审计

> 日期：2026-05-08  
> 范围：30 个已有设计资料包，以及 24 个父面板覆盖子 prefab 的运行时追溯  
> 目的：把每个资料包背后的脚本字段、公开 API、事件订阅、显隐控制和数据刷新入口集中列出，便于后续 Unity prefab 落地时核对。

## 1. 汇总结论

| 资料包 | 控制脚本 | UI 字段 | 公开 API | 事件/入口 | 显隐控制 | 数据刷新 |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| `AnnouncementUI_AnnouncementPanel` | `AnnouncementUI` | 3 | 1 | 7 | 10 | 7 |
| `BossRushBanner` | `BossRushBanner` | 7 | 4 | 0 | 10 | 10 |
| `BroadcasterPanel_BroadcasterPanelController` | `FeatureLockOverlay`、`BroadcasterPanel` | 20 | 8 | 21 | 12 | 11 |
| `ChapterAnnouncementUI_ChapterAnnouncement` | `ChapterAnnouncementUI` | 4 | 0 | 4 | 9 | 8 |
| `ConnectPanel` | `SurvivalConnectUI` | 5 | 0 | 9 | 10 | 4 |
| `DayPreviewBanner` | `DayPreviewBanner` | 5 | 0 | 7 | 10 | 10 |
| `EfficiencyRaceUI_EfficiencyRaceBanner` | `EfficiencyRaceUI` | 3 | 0 | 7 | 10 | 8 |
| `FairyWandMaxedBanner` | `FairyWandMaxedBanner` | 4 | 0 | 4 | 8 | 2 |
| `FeatureUnlockBanner` | `FeatureUnlockBanner` | 4 | 5 | 7 | 10 | 2 |
| `FrozenStatusPanel` | `FrozenStatusUI` | 4 | 1 | 0 | 10 | 10 |
| `GameControlUI_BottomBar` | `GameControlUI` | 13 | 1 | 14 | 10 | 10 |
| `GateUpgradeConfirmUI` | `GateUpgradeConfirmUI` | 8 | 2 | 0 | 4 | 6 |
| `GiftImpactUI_GiftImpactBanner` | `GiftImpactUI` | 3 | 0 | 4 | 10 | 8 |
| `GloryMomentUI_GloryMomentBanner` | `GloryMomentUI` | 7 | 0 | 4 | 10 | 3 |
| `LobbyPanel` | `SurvivalIdleUI` | 7 | 0 | 8 | 10 | 10 |
| `NightModifierUI_NightModifierBanner` | `NightModifierUI` | 4 | 0 | 6 | 10 | 7 |
| `NightReportUI_NightReportPanel` | `NightReportUI` | 4 | 0 | 4 | 8 | 10 |
| `OreRepairFloatingText_OreRepairFloatRoot` | `OreRepairFloatingText` | 0 | 0 | 4 | 0 | 4 |
| `PauseOverlayUI_PauseOverlayPanel` | `PauseOverlayUI` | 0 | 0 | 7 | 3 | 7 |
| `PeaceNightOverlay` | `PeaceNightOverlay` | 4 | 0 | 6 | 9 | 10 |
| `PreGameBannerUI_PreGameBanner` | `PreGameBannerUI` | 5 | 0 | 12 | 10 | 10 |
| `ReconnectDialog` | `ReconnectDialog` | 4 | 1 | 2 | 2 | 10 |
| `ShopConfirmDialogUI_ShopConfirmPanel` | `ShopConfirmDialogUI` | 6 | 1 | 4 | 7 | 5 |
| `ShopUI_ShopPanel` | `ShopUI` | 9 | 5 | 10 | 10 | 10 |
| `StatusLineBannerUI_StatusLineBanner` | `StatusLineBannerUI` | 2 | 0 | 7 | 1 | 10 |
| `SurvivalLiveRankingUI_GameUIPanel` | `SurvivalLiveRankingUI`、`ResourceRankUI`、`SupporterTopBarUI`、`SupporterNightFlashUI` | 12 | 2 | 22 | 8 | 30 |
| `SurvivalRankingPanel` | `SurvivalRankingUI` | 14 | 3 | 6 | 10 | 10 |
| `SurvivalSettingsUI_SurvivalSettingsPanel` | `SurvivalSettingsUI` | 14 | 3 | 0 | 10 | 10 |
| `SurvivalSettlementUI_SurvivalSettlementPanel` | `SurvivalSettlementUI` | 14 | 2 | 9 | 10 | 10 |
| `TensionOverlayUI_TensionOverlay` | `TensionOverlayUI` | 1 | 0 | 4 | 6 | 6 |

## 2. 旧根目录文件归档检查

本轮检查了 `docs/` 根目录下的 Markdown 文件。未发现文件名直接匹配现有 30 个界面资料包的旧版散落文件；不需要移动归档。

根目录中仍保留的文件多为项目总文档、集成指南、历史计划或本轮批量审计索引。与界面资料包重名的旧文件数：0。

## 3. 逐界面脚本审计

## AnnouncementUI_AnnouncementPanel

### AnnouncementUI

脚本路径：`Assets/Scripts/UI/AnnouncementUI.cs`

**Inspector / 绑定字段摘录**

- `public CanvasGroup canvasGroup;`
- `public TextMeshProUGUI mainText;`
- `public TextMeshProUGUI subText;`

**公开 API 摘录**

- `public void ShowAnnouncement(string main, string sub, Color color, float duration)`

**事件 / 订阅 / 入口摘录**

- `var sgm = SurvivalGameManager.Instance;`
- `sgm.OnStateChanged += HandleSurvivalStateChanged;`
- `sgm.OnGameEnded    += HandleSurvivalGameEnded;`
- `sgm.OnStateChanged  -= HandleSurvivalStateChanged;`
- `sgm.OnGameEnded     -= HandleSurvivalGameEnded;`
- `private void HandleSurvivalStateChanged(SurvivalGameManager.SurvivalState state)`
- `private void HandleSurvivalGameEnded(SurvivalGameEndedData data)`

**显隐 / 动画控制摘录**

- `public CanvasGroup canvasGroup;`
- `canvasGroup = GetComponent<CanvasGroup>();`
- `canvasGroup = gameObject.AddComponent<CanvasGroup>();`
- `StartCoroutine(DelayedStartAnnouncement());`
- `ShowAnnouncement("守护开始", "坚持到黎明！", new Color(1f, 0.85f, 0.2f), 2.0f);`
- `ShowAnnouncement(main, sub, color, 4.0f);`
- `public void ShowAnnouncement(string main, string sub, Color color, float duration)`
- `_currentAnnouncement = StartCoroutine(AnnouncementRoutine(main, sub, color, duration));`
- `canvasGroup.blocksRaycasts = true;`
- `canvasGroup.interactable = true;`

**数据刷新 / 文案绑定摘录**

- `sgm.OnStateChanged += HandleSurvivalStateChanged;`
- `sgm.OnGameEnded    += HandleSurvivalGameEnded;`
- `sgm.OnStateChanged  -= HandleSurvivalStateChanged;`
- `sgm.OnGameEnded     -= HandleSurvivalGameEnded;`
- `ModalRegistry.RequestB(ModalId);`
- `mainText.text = main;`
- `subText.text = sub;`

## BossRushBanner

### BossRushBanner

脚本路径：`Assets/Scripts/UI/BossRushBanner.cs`

**Inspector / 绑定字段摘录**

- `private GameObject _panelRoot;`
- `private TMP_Text _titleText;`
- `private TMP_Text _bossHpText;`
- `private TMP_Text _nextThemeText;`
- `private TMP_Text _killedText;`
- `private Image _bossHpBar;`
- `private CanvasGroup _killedFlash;`

**公开 API 摘录**

- `public void Show(BossRushStartedData data)`
- `public void OnKilled(BossRushKilledData data)`
- `public void SetBossHpCurrent(int hp)`
- `public void Hide()`

**事件 / 订阅 / 入口摘录**

- 未发现显式事件订阅

**显隐 / 动画控制摘录**

- `[SerializeField] private CanvasGroup _killedFlash;`
- `if (_panelRoot != null) _panelRoot.SetActive(false);`
- `if (_killedText != null) _killedText.gameObject.SetActive(false);`
- `if (_killedFlash != null) _killedFlash.alpha = 0f;`
- `UI.AnnouncementUI.Instance?.ShowAnnouncement(`
- `_panelRoot.SetActive(true);`
- `_killedText.gameObject.SetActive(true);`
- `_flashCoroutine = StartCoroutine(PlayKilledFlash());`
- `_killedHideCoroutine = StartCoroutine(HideAfter(_killedHoldSec));`
- `_killedFlash.alpha = Mathf.Clamp01(t / 0.2f);`

**数据刷新 / 文案绑定摘录**

- `public class BossRushBanner : MonoBehaviour`
- `public static BossRushBanner Instance { get; private set; }`
- `[SerializeField] private TMP_Text _titleText;`
- `[SerializeField] private TMP_Text _killedText;`
- `[SerializeField] private Image _bossHpBar;`
- `[Tooltip("Boss 被击杀 → 横幅保留时长后自动隐藏（秒）")]`
- `public void Show(BossRushStartedData data)`
- `"赛季Boss来袭！",`
- `Debug.Log($"[BossRushBanner] (未绑定 _panelRoot) 降级到 AnnouncementUI: hpTotal={data.bossHpTotal} nextTheme={data.nextThemeId}");`
- `if (_titleText != null) _titleText.text = "赛季Boss来袭！";`

## BroadcasterPanel_BroadcasterPanelController

### FeatureLockOverlay

脚本路径：`Assets/Scripts/UI/FeatureLockOverlay.cs`

**Inspector / 绑定字段摘录**

- `private TMP_Text _btnLabel;`
- `private Image _lockIcon;`
- `private TMP_Text _unlockHint;`
- `private Image _btnBackground;`
- `private Color _colorLockedText   = new Color(0.70f, 0.70f, 0.70f, 1f);`
- `private Color _colorUnlockedText = Color.white;`

**公开 API 摘录**

- `public void Apply(bool unlocked)`

**事件 / 订阅 / 入口摘录**

- `var sgm = SurvivalGameManager.Instance;`
- `sgm.OnUnlockedFeaturesSync  += HandleSync;`
- `sgm.OnNewlyUnlockedFeatures += HandleNewlyUnlocked;`
- `sgm.OnUnlockedFeaturesSync  -= HandleSync;`
- `sgm.OnNewlyUnlockedFeatures -= HandleNewlyUnlocked;`
- `private void HandleSync(string[] allFeatures)`
- `private void HandleNewlyUnlocked(string[] newFeatures)`

**显隐 / 动画控制摘录**

- `_lockIcon.gameObject.SetActive(locked);`
- `_unlockHint.gameObject.SetActive(locked);`

**数据刷新 / 文案绑定摘录**

- `_unlockHint.text = unlockDay > 0 ? $"D{unlockDay} 解锁" : "未解锁";`

### BroadcasterPanel

脚本路径：`Assets/Scripts/UI/BroadcasterPanel.cs`

**Inspector / 绑定字段摘录**

- `private GameObject _panelRoot;`
- `private Button _boostBtn;`
- `private Image  _boostBtnBg;`
- `private TMP_Text _boostCdText;`
- `private Button   _eventBtn;`
- `private Image    _eventBtnBg;`
- `private TMP_Text _eventCdText;`
- `private Button   _rouletteButton;`
- `private Button   _shopTabButton;`
- `private Button   _tribeWarButton;`
- `private Button _btnUpgradeGate;`
- `private Button _buildingButton;`
- `private Button _expeditionButton;`
- `private Button _supporterModeButton;`

**公开 API 摘录**

- `public void OpenUpgradeGatePanel() => OnUpgradeGateClick();`
- `public void OpenRoulettePanel() => OnRouletteClicked();`
- `public void OpenShopPanel() => OnShopClicked();`
- `public void OpenTribeWarPanel() => OnTribeWarClicked();`
- `public void OpenBuildingPanel() => OnBuildingClicked();`
- `public void OpenExpeditionPanel() => OnExpeditionClicked();`
- `public void SendExpeditionCommand(string action = "send")`

**事件 / 订阅 / 入口摘录**

- `var net = NetworkManager.Instance;`
- `net.OnMessageReceived += HandleMessage;`
- `net.OnConnected       += HandleConnected;`
- `net.OnDisconnected    += HandleDisconnected;`
- `var sgm = SurvivalGameManager.Instance;`
- `sgm.OnPhaseChanged   += HandlePhaseChangedForTip;`
- `sgm.OnResourceUpdate += HandleResourceUpdateForTip;`
- `net.OnMessageReceived -= HandleMessage;`
- `net.OnConnected       -= HandleConnected;`
- `net.OnDisconnected    -= HandleDisconnected;`
- `sgm.OnPhaseChanged   -= HandlePhaseChangedForTip;`
- `sgm.OnResourceUpdate -= HandleResourceUpdateForTip;`
- `private void HandleMessage(string type, string dataJson)`
- `HandleBroadcasterEffect(dataJson);`

**显隐 / 动画控制摘录**

- `[Header("面板根对象（通过SetActive控制显隐）")]`
- `_panelRoot.SetActive(false);`
- `_panelRoot.SetActive(_isRoomCreator);`
- `roulette.OpenPanel();`
- `AnnouncementUI.Instance?.ShowAnnouncement("Roulette", "Panel is loading...", new Color(1f, 0.84f, 0.25f), 2f);`
- `ShopUI.Instance.OpenPanel("A");`
- `TribeWarLobbyUI.Instance.OpenPanel();`
- `AnnouncementUI.Instance?.ShowAnnouncement("建造系统", "请稍候，建造菜单加载中...", new Color(0.6f, 0.9f, 0.6f), 2f);`
- `AnnouncementUI.Instance?.ShowAnnouncement("助威模式", "观众满员后可助威支持！", new Color(0.8f, 0.55f, 0.9f), 2f);`
- `AnnouncementUI.Instance?.ShowAnnouncement(`

**数据刷新 / 文案绑定摘录**

- `[SerializeField] private string _tipNight   = "快刷礼物！怪物要攻城了！";`
- `_shopTabButton.onClick.AddListener(OnShopClicked);`
- `sgm.OnPhaseChanged   += HandlePhaseChangedForTip;`
- `sgm.OnResourceUpdate += HandleResourceUpdateForTip;`
- `RefreshBroadcasterTip(phase: "day", gateHp: int.MaxValue);`
- `sgm.OnPhaseChanged   -= HandlePhaseChangedForTip;`
- `sgm.OnResourceUpdate -= HandleResourceUpdateForTip;`
- `private void Update()`
- `_boostCdText.text = $"CD {Mathf.CeilToInt(_boostCd)}s";`
- `_eventCdText.text = $"CD {Mathf.CeilToInt(_eventCd)}s";`

## ChapterAnnouncementUI_ChapterAnnouncement

### ChapterAnnouncementUI

脚本路径：`Assets/Scripts/UI/ChapterAnnouncementUI.cs`

**Inspector / 绑定字段摘录**

- `private RectTransform   _bannerRoot;`
- `private CanvasGroup     _bannerCanvasGroup;`
- `private TextMeshProUGUI _nameText;`
- `private TextMeshProUGUI _subText;`

**公开 API 摘录**

- 无公开 UI API 或以 Unity 生命周期驱动

**事件 / 订阅 / 入口摘录**

- `var sgm = SurvivalGameManager.Instance;`
- `sgm.OnChapterChanged += HandleChapterChanged;`
- `if (sgm != null) sgm.OnChapterChanged -= HandleChapterChanged;`
- `private void HandleChapterChanged(ChapterChangedData data)`

**显隐 / 动画控制摘录**

- `[SerializeField] private CanvasGroup     _bannerCanvasGroup;`
- `if (_bannerRoot != null) _bannerRoot.gameObject.SetActive(false);`
- `if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = 0f;`
- `_runCoroutine = StartCoroutine(RunAnnouncement(data));`
- `if (_bannerRoot != null) _bannerRoot.gameObject.SetActive(true);`
- `if (_bannerCanvasGroup != null)`
- `_bannerCanvasGroup.alpha = Mathf.Clamp01(t / FADE_IN);`
- `if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = 1f;`
- `_bannerCanvasGroup.alpha = 1f - Mathf.Clamp01(t / FADE_OUT);`

**数据刷新 / 文案绑定摘录**

- `public class ChapterAnnouncementUI : MonoBehaviour`
- `private void Update()`
- `sgm.OnChapterChanged += HandleChapterChanged;`
- `if (sgm != null) sgm.OnChapterChanged -= HandleChapterChanged;`
- `private void HandleChapterChanged(ChapterChangedData data)`
- `private IEnumerator RunAnnouncement(ChapterChangedData data)`
- `if (_nameText != null) _nameText.text = data.name;`
- `_subText.text = !string.IsNullOrEmpty(data.endNote)`

## ConnectPanel

### SurvivalConnectUI

脚本路径：`Assets/Scripts/UI/SurvivalConnectUI.cs`

**Inspector / 绑定字段摘录**

- `private GameObject _panel;`
- `private TMP_Text   _statusText;`
- `private TMP_Text   _dotText;`
- `private Image      _spinner;`
- `private Button     _retryBtn;`

**公开 API 摘录**

- 无公开 UI API 或以 Unity 生命周期驱动

**事件 / 订阅 / 入口摘录**

- `var net = NetworkManager.Instance;`
- `net.OnConnected      += OnConnected;`
- `net.OnDisconnected   += OnDisconnected;`
- `net.OnConnectFailed  += OnConnectFailed;`
- `Debug.LogError("[SurvivalConnectUI] NetworkManager.Instance 未找到！");`
- `net.OnConnected     -= OnConnected;`
- `net.OnDisconnected  -= OnDisconnected;`
- `net.OnConnectFailed -= OnConnectFailed;`
- `var sgm = SurvivalGameManager.Instance;`

**显隐 / 动画控制摘录**

- `_retryBtn.gameObject.SetActive(false);`
- `ShowPanel("正在连接服务器");`
- `ShowPanel("已连接！正在加载游戏状态...");`
- `if (_retryBtn != null) _retryBtn.gameObject.SetActive(false);`
- `_hideDelayCoroutine = StartCoroutine(HidePanelAfterDelay(_hideDelayAfterConnect));`
- `ShowPanel(msg);`
- `if (_retryBtn != null) _retryBtn.gameObject.SetActive(true);`
- `ShowPanel("正在重新连接");`
- `private void ShowPanel(string status)`
- `_panel.SetActive(true);`

**数据刷新 / 文案绑定摘录**

- `private void Update()`
- `if (_statusText != null) _statusText.text = status;`
- `if (_dotText != null) _dotText.text = "";`
- `if (_dotText != null) _dotText.text = dots[i % dots.Length];`

## DayPreviewBanner

### DayPreviewBanner

脚本路径：`Assets/Scripts/UI/DayPreviewBanner.cs`

**Inspector / 绑定字段摘录**

- `private RectTransform   _bannerRoot;`
- `private CanvasGroup     _bannerCanvasGroup;`
- `private TextMeshProUGUI _headlineText;`
- `private TextMeshProUGUI _bodyText;`
- `private TextMeshProUGUI _countdownText;`

**公开 API 摘录**

- 无公开 UI API 或以 Unity 生命周期驱动

**事件 / 订阅 / 入口摘录**

- `var sgm = SurvivalGameManager.Instance;`
- `sgm.OnDayPreview   += HandleDayPreview;`
- `sgm.OnPhaseChanged += HandlePhaseChanged;`
- `sgm.OnDayPreview   -= HandleDayPreview;`
- `sgm.OnPhaseChanged -= HandlePhaseChanged;`
- `private void HandleDayPreview(DayPreviewData data)`
- `private void HandlePhaseChanged(PhaseChangedData data)`

**显隐 / 动画控制摘录**

- `[SerializeField] private CanvasGroup     _bannerCanvasGroup;`
- `if (_bannerRoot != null) _bannerRoot.gameObject.SetActive(false);`
- `if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = 0f;`
- `_runCoroutine = StartCoroutine(RunBanner(data));`
- `HideImmediately();`
- `private void HideImmediately()`
- `if (_bannerRoot != null) _bannerRoot.gameObject.SetActive(true);`
- `if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = Mathf.Clamp01(t / FADE_IN);`
- `if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = 1f;`
- `if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = 1f - Mathf.Clamp01(t / FADE_OUT);`

**数据刷新 / 文案绑定摘录**

- `public class DayPreviewBanner : MonoBehaviour`
- `private void Update()`
- `sgm.OnDayPreview   += HandleDayPreview;`
- `sgm.OnPhaseChanged += HandlePhaseChanged;`
- `sgm.OnDayPreview   -= HandleDayPreview;`
- `sgm.OnPhaseChanged -= HandlePhaseChanged;`
- `private void HandleDayPreview(DayPreviewData data)`
- `private IEnumerator RunBanner(DayPreviewData data)`
- `if (_headlineText != null) _headlineText.text = $"今夜预报：{modifierName}";`
- `_bodyText.text = $"Boss HP：{data.bossHp}  预计怪物数：~{data.monsterCount}\n特殊效果：{modifierDesc}";`

## EfficiencyRaceUI_EfficiencyRaceBanner

### EfficiencyRaceUI

脚本路径：`Assets/Scripts/UI/EfficiencyRaceUI.cs`

**Inspector / 绑定字段摘录**

- `private RectTransform   _bannerRoot;`
- `private CanvasGroup     _bannerCanvasGroup;`
- `private TextMeshProUGUI _messageText;`

**公开 API 摘录**

- 无公开 UI API 或以 Unity 生命周期驱动

**事件 / 订阅 / 入口摘录**

- `var sgm = SurvivalGameManager.Instance;`
- `sgm.OnEfficiencyRace += HandleEfficiencyRace;`
- `sgm.OnPhaseChanged   += HandlePhaseChanged;`
- `sgm.OnEfficiencyRace -= HandleEfficiencyRace;`
- `sgm.OnPhaseChanged   -= HandlePhaseChanged;`
- `private void HandleEfficiencyRace(EfficiencyRaceData data)`
- `private void HandlePhaseChanged(PhaseChangedData data)`

**显隐 / 动画控制摘录**

- `[SerializeField] private CanvasGroup     _bannerCanvasGroup;`
- `if (_bannerRoot != null) _bannerRoot.gameObject.SetActive(false);`
- `if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = 0f;`
- `_runCoroutine = StartCoroutine(RunBanner(msg));`
- `HideImmediately();`
- `private void HideImmediately()`
- `if (_bannerRoot != null) _bannerRoot.gameObject.SetActive(true);`
- `if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = Mathf.Clamp01(t / FADE_IN);`
- `if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = 1f;`
- `if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = 1f - Mathf.Clamp01(t / FADE_OUT);`

**数据刷新 / 文案绑定摘录**

- `public class EfficiencyRaceUI : MonoBehaviour`
- `private void Update()`
- `sgm.OnEfficiencyRace += HandleEfficiencyRace;`
- `sgm.OnPhaseChanged   += HandlePhaseChanged;`
- `sgm.OnEfficiencyRace -= HandleEfficiencyRace;`
- `sgm.OnPhaseChanged   -= HandlePhaseChanged;`
- `private void HandleEfficiencyRace(EfficiencyRaceData data)`
- `if (_messageText != null) _messageText.text = msg;`

## FairyWandMaxedBanner

### FairyWandMaxedBanner

脚本路径：`Assets/Scripts/UI/FairyWandMaxedBanner.cs`

**Inspector / 绑定字段摘录**

- `private RectTransform   _flashRoot;`
- `private Image           _flashImage;`
- `private RectTransform   _marqueeRoot;`
- `private TextMeshProUGUI _marqueeText;`

**公开 API 摘录**

- 无公开 UI API 或以 Unity 生命周期驱动

**事件 / 订阅 / 入口摘录**

- `var sgm = SurvivalGameManager.Instance;`
- `sgm.OnFairyWandMaxed += HandleMaxed;`
- `if (sgm != null) sgm.OnFairyWandMaxed -= HandleMaxed;`
- `private void HandleMaxed(FairyWandMaxedData data)`

**显隐 / 动画控制摘录**

- `if (_flashRoot   != null) _flashRoot.gameObject.SetActive(false);`
- `if (_marqueeRoot != null) _marqueeRoot.gameObject.SetActive(false);`
- `_flashCo   = StartCoroutine(PlayFlash());`
- `_marqueeCo = StartCoroutine(PlayMarquee($"满级矿工达成！{playerName}"));`
- `_flashRoot.gameObject.SetActive(true);`
- `_flashRoot.gameObject.SetActive(false);`
- `_marqueeRoot.gameObject.SetActive(true);`
- `_marqueeRoot.gameObject.SetActive(false);`

**数据刷新 / 文案绑定摘录**

- `private void Update()`
- `_marqueeText.text = text;`

## FeatureUnlockBanner

### FeatureUnlockBanner

脚本路径：`Assets/Scripts/UI/FeatureUnlockBanner.cs`

**Inspector / 绑定字段摘录**

- `private GameObject _panelRoot;`
- `private TMP_Text _titleText;`
- `private TMP_Text _descText;`
- `private CanvasGroup _canvasGroup;`

**公开 API 摘录**

- `public void ShowVeteranUnlocked(VeteranUnlockedData data)`
- `public void EnqueueUnlock(string featureId)`
- `public static string GetFeatureDisplayName(string featureId)`
- `public static string GetFeatureDesc(string featureId)`
- `public static string FormatVeteranReason(string reason)`

**事件 / 订阅 / 入口摘录**

- `var sgm = SurvivalGameManager.Instance;`
- `sgm.OnNewlyUnlockedFeatures += HandleNewlyUnlocked;`
- `sgm.OnVeteranUnlocked       += HandleVeteranUnlocked;`
- `sgm.OnNewlyUnlockedFeatures -= HandleNewlyUnlocked;`
- `sgm.OnVeteranUnlocked       -= HandleVeteranUnlocked;`
- `private void HandleNewlyUnlocked(string[] features)`
- `private void HandleVeteranUnlocked(VeteranUnlockedData data)`

**显隐 / 动画控制摘录**

- `[Header("CanvasGroup（滑入滑出 alpha 动画）")]`
- `[SerializeField] private CanvasGroup _canvasGroup;`
- `if (_panelRoot != null) _panelRoot.SetActive(false);`
- `if (_canvasGroup != null) _canvasGroup.alpha = 0f;`
- `_playCoroutine = StartCoroutine(PlayQueue());`
- `UI.AnnouncementUI.Instance?.ShowAnnouncement(`
- `_panelRoot.SetActive(true);`
- `_canvasGroup.alpha = 0f;`
- `_canvasGroup.alpha = Mathf.Clamp01(t / Mathf.Max(0.01f, _fadeInSec));`
- `_canvasGroup.alpha = 1f;`

**数据刷新 / 文案绑定摘录**

- `if (_titleText != null) _titleText.text = item.title;`
- `if (_descText != null)  _descText.text  = item.desc;`

## FrozenStatusPanel

### FrozenStatusUI

脚本路径：`Assets/Scripts/UI/FrozenStatusUI.cs`

**Inspector / 绑定字段摘录**

- `private GameObject _panel;`
- `private TextMeshProUGUI _frozenText;`
- `private TextMeshProUGUI _countdownText;`
- `private Image _backgroundImage;`

**公开 API 摘录**

- `public static void ShowFrozen(float duration)`

**事件 / 订阅 / 入口摘录**

- 未发现显式事件订阅

**显隐 / 动画控制摘录**

- `if (_panel != null) _panel.SetActive(false);`
- `Instance.ShowInternal(duration);`
- `private void ShowInternal(float duration)`
- `_panel.SetActive(true);`
- `_countdownCoroutine = StartCoroutine(CountdownCoroutine(duration));`
- `yield return StartCoroutine(FadePanel(0f, 1f, 0.3f));`
- `yield return StartCoroutine(FadePanel(1f, 0f, 0.4f));`
- `float alpha = Mathf.Lerp(startAlpha, endAlpha, t);`
- `SetAlpha(_backgroundImage, alpha);`
- `SetTextAlpha(_frozenText, alpha);`

**数据刷新 / 文案绑定摘录**

- `public class FrozenStatusUI : MonoBehaviour`
- `public static FrozenStatusUI Instance { get; private set; }`
- `public static void ShowFrozen(float duration)`
- `Debug.LogWarning("[FrozenStatusUI] _panel 未绑定，跳过显示");`
- `_frozenText.text = "全体守护者已冻结";`
- `_countdownText.text = $"解冻倒计时：{Mathf.CeilToInt(remaining)}s";`
- `_frozenText.text = "解冻完成！";`
- `_countdownText.text = "";`
- `SetTextAlpha(_frozenText, alpha);`
- `SetTextAlpha(_countdownText, alpha);`

## GameControlUI_BottomBar

### GameControlUI

脚本路径：`Assets/Scripts/UI/GameControlUI.cs`

**Inspector / 绑定字段摘录**

- `public Button gmLoginButton;`
- `public Button connectButton;`
- `public Button startButton;`
- `public Button pauseButton;`
- `public Button endButton;`
- `public Button resetButton;`
- `public Button simulateButton;`
- `public Button giftT1Button;`
- `public Button giftT3Button;`
- `public Button giftT5Button;`
- `public Button freezeButton;`
- `public Button monsterButton;`
- `public TextMeshProUGUI statusText;`

**公开 API 摘录**

- `public void SetVisible(bool visible)`

**事件 / 订阅 / 入口摘录**

- `var net = NetworkManager.Instance;`
- `net.OnConnected    += HandleConnected;`
- `net.OnDisconnected += HandleDisconnected;`
- `var sgm = SurvivalGameManager.Instance;`
- `sgm.OnStateChanged += HandleStateChanged;`
- `net.OnConnected    -= HandleConnected;`
- `net.OnDisconnected -= HandleDisconnected;`
- `sgm.OnStateChanged -= HandleStateChanged;`
- `SurvivalGameManager.Instance?.ConnectToServer();`
- `NetworkManager.Instance?.SendJson($"{{\"type\":\"reset_game\",\"timestamp\":{ts}}}");`
- `NetworkManager.Instance?.SendJson($"{{\"type\":\"pause_game\",\"timestamp\":{ts}}}");`
- `NetworkManager.Instance?.SendJson($"{{\"type\":\"end_game\",\"timestamp\":{ts}}}");`
- `NetworkManager.Instance?.SendJson(`
- `NetworkManager.Instance?.SendJson($"{{\"type\":\"simulate_freeze\",\"timestamp\":{ts}}}");`

**显隐 / 动画控制摘录**

- `private CanvasGroup _cg;`
- `_cg = GetComponent<CanvasGroup>();`
- `if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();`
- `_cg.alpha          = visible ? 1f : 0f;`
- `_cg.interactable   = visible;`
- `_cg.blocksRaycasts = visible;`
- `StartCoroutine(DelayedStart(1.2f));`
- `if (cb != null) cb.interactable = !connected;`
- `if (startButton)    startButton.interactable    = connected;`
- `if (resetButton)    resetButton.interactable    = connected;`

**数据刷新 / 文案绑定摘录**

- `public Button gmLoginButton;`
- `private void Update()`
- `if (giftT1Button)   giftT1Button.onClick.AddListener(() => OnSimulateGift(1));`
- `if (giftT3Button)   giftT3Button.onClick.AddListener(() => OnSimulateGift(3));`
- `if (giftT5Button)   giftT5Button.onClick.AddListener(() => OnSimulateGift(5));`
- `sgm.OnStateChanged += HandleStateChanged;`
- `UpdateButtonStates(_connected, currentState);`
- `sgm.OnStateChanged -= HandleStateChanged;`
- `sgm.RequestStartGame();`
- `NetworkManager.Instance?.SendJson($"{{\"type\":\"reset_game\",\"timestamp\":{ts}}}");`

## GateUpgradeConfirmUI

### GateUpgradeConfirmUI

脚本路径：`Assets/Scripts/UI/GateUpgradeConfirmUI.cs`

**Inspector / 绑定字段摘录**

- `private GameObject      _panel;`
- `private TextMeshProUGUI _titleText;`
- `private TextMeshProUGUI _currentLevelText;`
- `private TextMeshProUGUI _nextLevelText;`
- `private TextMeshProUGUI _costText;`
- `private TextMeshProUGUI _featuresText;`
- `private Button _btnConfirm;`
- `private Button _btnCancel;`

**公开 API 摘录**

- `public void ShowConfirm(int currentLevel, int nextLevel, int cost,`
- `public void Hide()`

**事件 / 订阅 / 入口摘录**

- 未发现显式事件订阅

**显隐 / 动画控制摘录**

- `[Header("面板根（子 Panel，Awake 中 SetActive(false) 合法）")]`
- `_panel.SetActive(false);`
- `if (_panel != null) _panel.SetActive(false);`
- `_panel.SetActive(true);`

**数据刷新 / 文案绑定摘录**

- `_titleText.text = "升级城门";`
- `_currentLevelText.text = $"当前 Lv.{currentLevel}";`
- `_nextLevelText.text = $"→ Lv.{nextLevel}{tierLabel}";`
- `_costText.text = $"消耗矿石 × {cost}";`
- `_featuresText.text = newFeatureDesc ?? "";`
- `if (!ModalRegistry.Request(MODAL_A_ID, 75, () =>`

## GiftImpactUI_GiftImpactBanner

### GiftImpactUI

脚本路径：`Assets/Scripts/UI/GiftImpactUI.cs`

**Inspector / 绑定字段摘录**

- `private RectTransform     _bannerRoot;`
- `private CanvasGroup       _bannerCanvasGroup;`
- `private TextMeshProUGUI   _bannerText;`

**公开 API 摘录**

- 无公开 UI API 或以 Unity 生命周期驱动

**事件 / 订阅 / 入口摘录**

- `var sgm = SurvivalGameManager.Instance;`
- `sgm.OnGiftImpact += HandleGiftImpact;`
- `if (sgm != null) sgm.OnGiftImpact -= HandleGiftImpact;`
- `private void HandleGiftImpact(GiftImpactData d)`

**显隐 / 动画控制摘录**

- `[SerializeField] private CanvasGroup       _bannerCanvasGroup;`
- `if (_bannerRoot != null) _bannerRoot.gameObject.SetActive(false);`
- `if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = 0f;`
- `_runner = StartCoroutine(RunQueue());`
- `if (_bannerRoot == null \|\| _bannerCanvasGroup == null) yield break;`
- `_bannerRoot.gameObject.SetActive(true);`
- `_bannerCanvasGroup.alpha = Mathf.Lerp(0f, 1f, Mathf.Clamp01(t / FADE_IN));`
- `_bannerCanvasGroup.alpha = 1f;`
- `_bannerCanvasGroup.alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(t / FADE_OUT));`
- `_bannerCanvasGroup.alpha = 0f;`

**数据刷新 / 文案绑定摘录**

- `public class GiftImpactUI : MonoBehaviour`
- `private readonly Queue<GiftImpactData> _queue = new Queue<GiftImpactData>();`
- `private void Update()`
- `sgm.OnGiftImpact += HandleGiftImpact;`
- `if (sgm != null) sgm.OnGiftImpact -= HandleGiftImpact;`
- `private void HandleGiftImpact(GiftImpactData d)`
- `private IEnumerator PlayBanner(GiftImpactData d)`
- `_bannerText.text = $"{player} 的 {giftN} → {impacts}";`

## GloryMomentUI_GloryMomentBanner

### GloryMomentUI

脚本路径：`Assets/Scripts/UI/GloryMomentUI.cs`

**Inspector / 绑定字段摘录**

- `private RectTransform     _bannerRoot;`
- `private CanvasGroup       _bannerCanvasGroup;`
- `private Image             _bannerBg;`
- `private TextMeshProUGUI   _bannerText;`
- `private TextMeshProUGUI   _subText;`
- `private Image _goldBurst;`
- `private Image _redFlash;`

**公开 API 摘录**

- 无公开 UI API 或以 Unity 生命周期驱动

**事件 / 订阅 / 入口摘录**

- `var sgm = SurvivalGameManager.Instance;`
- `sgm.OnGloryMoment += HandleGloryMoment;`
- `if (sgm != null) sgm.OnGloryMoment -= HandleGloryMoment;`
- `private void HandleGloryMoment(GloryMomentData d)`

**显隐 / 动画控制摘录**

- `[SerializeField] private CanvasGroup       _bannerCanvasGroup;`
- `_currentBanner = StartCoroutine(PlayBanner(bannerColor, mainLine, subLine, duration,`
- `StartCoroutine(FlashImage(_redFlash, RED_FLASH_COLOR, RED_FLASH_SEC));`
- `StartCoroutine(FlashImage(_goldBurst, GOLD_BURST_COLOR, DURATION_NEW_FIRST * 0.5f));`
- `_subText.gameObject.SetActive(!string.IsNullOrEmpty(subLine));`
- `if (_bannerRoot != null) _bannerRoot.gameObject.SetActive(true);`
- `yield return FadeCanvasGroup(_bannerCanvasGroup, 0f, 1f, FADE_IN);`
- `yield return FadeCanvasGroup(_bannerCanvasGroup, 1f, 0f, FADE_OUT);`
- `if (_bannerRoot != null) _bannerRoot.gameObject.SetActive(false);`
- `private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float dur)`

**数据刷新 / 文案绑定摘录**

- `private void Update()`
- `if (_bannerText != null) _bannerText.text = mainLine;`
- `_subText.text = subLine;`

## LobbyPanel

### SurvivalIdleUI

脚本路径：`Assets/Scripts/UI/SurvivalIdleUI.cs`

**Inspector / 绑定字段摘录**

- `private GameObject _panel;`
- `private Button   _startBtn;`
- `private Button   _rankingBtn;`
- `private Button   _settingsBtn;`
- `private TMP_Text _statusText;`
- `private TMP_Text _serverStatus;`
- `private TMP_Text _titleText;`

**公开 API 摘录**

- 无公开 UI API 或以 Unity 生命周期驱动

**事件 / 订阅 / 入口摘录**

- `var net = NetworkManager.Instance;`
- `net.OnConnected    += OnConnected;`
- `net.OnDisconnected += OnDisconnected;`
- `var sgm = SurvivalGameManager.Instance;`
- `sgm.OnStateChanged += OnStateChanged;`
- `net.OnConnected    -= OnConnected;`
- `net.OnDisconnected -= OnDisconnected;`
- `sgm.OnStateChanged -= OnStateChanged;`

**显隐 / 动画控制摘录**

- `private void OnDisconnected(string _)                           => HidePanel();`
- `ShowPanel();`
- `HidePanel();`
- `private void ShowPanel()`
- `if (_panel != null) _panel.SetActive(true);`
- `_startBtn.interactable = true;`
- `private void HidePanel()`
- `if (_panel != null) _panel.SetActive(false);`
- `if (_startBtn != null) _startBtn.interactable = false;`
- `_rankingPanel.TogglePanel();`

**数据刷新 / 文案绑定摘录**

- `[SerializeField] private SurvivalRankingUI  _rankingPanel;`
- `_rankingBtn.onClick.AddListener(OnRankingClicked);`
- `sgm.OnStateChanged += OnStateChanged;`
- `_titleText.text = "极地生存法则";`
- `RefreshVisibility();`
- `sgm.OnStateChanged -= OnStateChanged;`
- `private void OnConnected()                                       => RefreshVisibility();`
- `private void OnStateChanged(SurvivalGameManager.SurvivalState s) => RefreshVisibility();`
- `private void RefreshVisibility()`
- `_serverStatus.text = isConnected ? "已连接 √" : "连接中...";`

## NightModifierUI_NightModifierBanner

### NightModifierUI

脚本路径：`Assets/Scripts/UI/NightModifierUI.cs`

**Inspector / 绑定字段摘录**

- `private RectTransform   _bannerRoot;`
- `private CanvasGroup     _bannerCanvasGroup;`
- `private TextMeshProUGUI _nameText;`
- `private TextMeshProUGUI _descText;`

**公开 API 摘录**

- 无公开 UI API 或以 Unity 生命周期驱动

**事件 / 订阅 / 入口摘录**

- `var sgm = SurvivalGameManager.Instance;`
- `sgm.OnPhaseChanged += HandlePhaseChanged;`
- `if (sgm != null) sgm.OnPhaseChanged -= HandlePhaseChanged;`
- `private void HandlePhaseChanged(PhaseChangedData data)`
- `if (SurvivalGameManager.Instance == null \|\|`
- `SurvivalGameManager.Instance.State != SurvivalGameManager.SurvivalState.Running)`

**显隐 / 动画控制摘录**

- `[SerializeField] private CanvasGroup     _bannerCanvasGroup;`
- `if (_bannerRoot != null) _bannerRoot.gameObject.SetActive(false);`
- `if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = 0f;`
- `HideImmediately();`
- `_runCoroutine = StartCoroutine(RunAnnouncement(data.nightModifier));`
- `private void HideImmediately()`
- `if (_bannerRoot != null) _bannerRoot.gameObject.SetActive(true);`
- `if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = Mathf.Clamp01(t / FADE_IN);`
- `if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = 1f;`
- `if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = 1f - Mathf.Clamp01(t / FADE_OUT);`

**数据刷新 / 文案绑定摘录**

- `public class NightModifierUI : MonoBehaviour`
- `private void Update()`
- `sgm.OnPhaseChanged += HandlePhaseChanged;`
- `if (sgm != null) sgm.OnPhaseChanged -= HandlePhaseChanged;`
- `private IEnumerator RunAnnouncement(NightModifierData modifier)`
- `if (_nameText != null) _nameText.text = modifier.name;`
- `if (_descText != null) _descText.text = modifier.description ?? "";`

## NightReportUI_NightReportPanel

### NightReportUI

脚本路径：`Assets/Scripts/UI/NightReportUI.cs`

**Inspector / 绑定字段摘录**

- `private RectTransform   _panelRoot;`
- `private CanvasGroup     _panelCanvasGroup;`
- `private TextMeshProUGUI _titleText;`
- `private TextMeshProUGUI _bodyText;`

**公开 API 摘录**

- 无公开 UI API 或以 Unity 生命周期驱动

**事件 / 订阅 / 入口摘录**

- `var sgm = SurvivalGameManager.Instance;`
- `sgm.OnNightReport += HandleNightReport;`
- `if (sgm != null) sgm.OnNightReport -= HandleNightReport;`
- `private void HandleNightReport(NightReportData data)`

**显隐 / 动画控制摘录**

- `[SerializeField] private CanvasGroup     _panelCanvasGroup;`
- `if (_panelRoot != null) _panelRoot.gameObject.SetActive(false);`
- `if (_panelCanvasGroup != null) _panelCanvasGroup.alpha = 0f;`
- `_runCoroutine = StartCoroutine(RunReport(data));`
- `if (_panelRoot != null) _panelRoot.gameObject.SetActive(true);`
- `if (_panelCanvasGroup != null) _panelCanvasGroup.alpha = Mathf.Clamp01(t / FADE_IN);`
- `if (_panelCanvasGroup != null) _panelCanvasGroup.alpha = 1f;`
- `if (_panelCanvasGroup != null) _panelCanvasGroup.alpha = 1f - Mathf.Clamp01(t / FADE_OUT);`

**数据刷新 / 文案绑定摘录**

- `public class NightReportUI : MonoBehaviour`
- `private void Update()`
- `sgm.OnNightReport += HandleNightReport;`
- `if (sgm != null) sgm.OnNightReport -= HandleNightReport;`
- `private void HandleNightReport(NightReportData data)`
- `private IEnumerator RunReport(NightReportData data)`
- `_titleText.text = $"第 {data.day} 夜战斗报告";`
- `? "Boss：已击杀！"`
- `: "Boss：未出现 / 未击杀");`
- `if (!string.IsNullOrEmpty(data.topGifterName))`

## OreRepairFloatingText_OreRepairFloatRoot

### OreRepairFloatingText

脚本路径：`Assets/Scripts/UI/OreRepairFloatingText.cs`

**Inspector / 绑定字段摘录**

- 无明显 UI 绑定字段

**公开 API 摘录**

- 无公开 UI API 或以 Unity 生命周期驱动

**事件 / 订阅 / 入口摘录**

- `var sgm = SurvivalGameManager.Instance;`
- `sgm.OnResourceUpdate += HandleResourceUpdate;`
- `if (sgm != null) sgm.OnResourceUpdate -= HandleResourceUpdate;`
- `private void HandleResourceUpdate(ResourceUpdateData data)`

**显隐 / 动画控制摘录**

- 未发现显式显隐控制

**数据刷新 / 文案绑定摘录**

- `private void Update()`
- `sgm.OnResourceUpdate += HandleResourceUpdate;`
- `if (sgm != null) sgm.OnResourceUpdate -= HandleResourceUpdate;`
- `private void HandleResourceUpdate(ResourceUpdateData data)`

## PauseOverlayUI_PauseOverlayPanel

### PauseOverlayUI

脚本路径：`Assets/Scripts/UI/PauseOverlayUI.cs`

**Inspector / 绑定字段摘录**

- 无明显 UI 绑定字段

**公开 API 摘录**

- 无公开 UI API 或以 Unity 生命周期驱动

**事件 / 订阅 / 入口摘录**

- `var sgm = SurvivalGameManager.Instance;`
- `sgm.OnGamePaused  += HandleGamePaused;`
- `sgm.OnGameResumed += HandleGameResumed;`
- `sgm.OnGamePaused  -= HandleGamePaused;`
- `sgm.OnGameResumed -= HandleGameResumed;`
- `private void HandleGamePaused()`
- `private void HandleGameResumed()`

**显隐 / 动画控制摘录**

- `_overlayRoot.SetActive(false);`
- `if (_overlayRoot != null) _overlayRoot.SetActive(true);`
- `if (_overlayRoot != null) _overlayRoot.SetActive(false);`

**数据刷新 / 文案绑定摘录**

- `private void Update()`
- `_mainText.text      = "⏸ 游戏已暂停";`
- `_subText.text      = "GM 调试模式 — 等待主播恢复";`
- `sgm.OnGamePaused  += HandleGamePaused;`
- `sgm.OnGameResumed += HandleGameResumed;`
- `sgm.OnGamePaused  -= HandleGamePaused;`
- `sgm.OnGameResumed -= HandleGameResumed;`

## PeaceNightOverlay

### PeaceNightOverlay

脚本路径：`Assets/Scripts/UI/PeaceNightOverlay.cs`

**Inspector / 绑定字段摘录**

- `private GameObject _overlayRoot;`
- `private Image _overlayImage;`
- `private TMP_Text _hintText;`
- `private TMP_Text _countdownText;`

**公开 API 摘录**

- 无公开 UI API 或以 Unity 生命周期驱动

**事件 / 订阅 / 入口摘录**

- `var sgm = SurvivalGameManager.Instance;`
- `sgm.OnPhaseChanged += HandlePhaseChanged;`
- `if (sgm != null) sgm.OnPhaseChanged -= HandlePhaseChanged;`
- `private void HandlePhaseChanged(PhaseChangedData data)`
- `if (SurvivalGameManager.Instance == null \|\|`
- `SurvivalGameManager.Instance.State != SurvivalGameManager.SurvivalState.Running)`

**显隐 / 动画控制摘录**

- `[Header("柔光 Image（fade-in/out 控制 alpha；也可留空只用文本）")]`
- `[Header("柔光峰值 alpha（overlay image 最终 alpha；默认 0.25，避免过度遮挡）")]`
- `if (_overlayRoot != null) _overlayRoot.SetActive(false);`
- `if (_countdownText != null) _countdownText.gameObject.SetActive(false);`
- `_overlayRoot.SetActive(true);`
- `_countdownText.gameObject.SetActive(showCountdown);`
- `_countdownCoroutine = StartCoroutine(UpdateCountdown());`
- `_fadeCoroutine = StartCoroutine(FadeOverlay(_peakAlpha, _fadeInSec));`
- `_fadeCoroutine = StartCoroutine(FadeOverlayThenDeactivate(0f, _fadeOutSec));`

**数据刷新 / 文案绑定摘录**

- `public class PeaceNightOverlay : MonoBehaviour`
- `public static PeaceNightOverlay Instance { get; private set; }`
- `sgm.OnPhaseChanged += HandlePhaseChanged;`
- `if (sgm != null) sgm.OnPhaseChanged -= HandlePhaseChanged;`
- `case SurvivalMessageProtocol.PhaseVariantPeaceNight:`
- `case SurvivalMessageProtocol.PhaseVariantPeaceNightPrelude:`
- `case SurvivalMessageProtocol.PhaseVariantPeaceNightSilent:`
- `Debug.Log($"[PeaceNightOverlay] phase_changed: phase={data.phase} variant={variant} preludeEndsAt={data.peacePreludeEndsAt}");`
- `Debug.Log($"[PeaceNightOverlay] (未绑定 _overlayRoot) hint='{hintText}' countdown={showCountdown}");`
- `if (_hintText != null) _hintText.text = hintText;`

## PreGameBannerUI_PreGameBanner

### PreGameBannerUI

脚本路径：`Assets/Scripts/UI/PreGameBannerUI.cs`

**Inspector / 绑定字段摘录**

- `private GameObject _panel;`
- `private TMP_Text _titleText;`
- `private TMP_Text _statusText;`
- `private TMP_Text _playerCountText;`
- `private Button   _startBtn;`

**公开 API 摘录**

- 无公开 UI API 或以 Unity 生命周期驱动

**事件 / 订阅 / 入口摘录**

- `var net = NetworkManager.Instance;`
- `net.OnConnected    += RefreshVisibility;`
- `net.OnDisconnected += HandleDisconnected;`
- `var sgm = SurvivalGameManager.Instance;`
- `sgm.OnStateChanged  += HandleStateChanged;`
- `sgm.OnPlayerJoined  += OnPlayerJoined;`
- `net.OnConnected    -= RefreshVisibility;`
- `net.OnDisconnected -= HandleDisconnected;`
- `sgm.OnStateChanged  -= HandleStateChanged;`
- `sgm.OnPlayerJoined  -= OnPlayerJoined;`
- `private void HandleDisconnected(string reason) => HidePanel();`
- `private void HandleStateChanged(SurvivalGameManager.SurvivalState state) => RefreshVisibility();`

**显隐 / 动画控制摘录**

- `[Header("面板根节点（SetActive 控制显隐）")]`
- `private void HandleDisconnected(string reason) => HidePanel();`
- `ShowPanel();`
- `HidePanel();`
- `private void ShowPanel()`
- `if (_panel != null) _panel.SetActive(true);`
- `if (_startBtn != null) _startBtn.interactable = true;`
- `private void HidePanel()`
- `if (_panel != null) _panel.SetActive(false);`
- `if (_startBtn != null) _startBtn.interactable = false;`

**数据刷新 / 文案绑定摘录**

- `if (_titleText != null) _titleText.text = "极地生存法则";`
- `net.OnConnected    += RefreshVisibility;`
- `sgm.OnStateChanged  += HandleStateChanged;`
- `RefreshVisibility();`
- `net.OnConnected    -= RefreshVisibility;`
- `sgm.OnStateChanged  -= HandleStateChanged;`
- `private void HandleStateChanged(SurvivalGameManager.SurvivalState state) => RefreshVisibility();`
- `private void RefreshVisibility()`
- `UpdatePlayerCount();`
- `private void UpdatePlayerCount()`

## ReconnectDialog

### ReconnectDialog

脚本路径：`Assets/Scripts/UI/ReconnectDialog.cs`

**Inspector / 绑定字段摘录**

- `private Button     _reconnectButton;`
- `private Button     _newGameButton;`
- `private TMP_Text   _titleText;`
- `private TMP_Text   _descText;`

**公开 API 摘录**

- `public void Show()`

**事件 / 订阅 / 入口摘录**

- `DrscfZ.Survival.SurvivalGameManager.Instance?.RequestResumeSession();`
- `DrscfZ.Core.NetworkManager.Instance?.SendMessage("reset_game");`

**显隐 / 动画控制摘录**

- `gameObject.SetActive(false);`
- `gameObject.SetActive(true);`

**数据刷新 / 文案绑定摘录**

- `public class ReconnectDialog : MonoBehaviour`
- `public static ReconnectDialog Instance { get; private set; }`
- `_reconnectButton?.onClick.AddListener(OnReconnect);`
- `if (_titleText != null) _titleText.text = "检测到本次堡垒守护进行中";`
- `if (_descText  != null) _descText.text  = "发现未结束的守护，是否继续守护这座堡垒？";`
- `private void OnReconnect()`
- `DrscfZ.Survival.SurvivalGameManager.Instance?.RequestResumeSession();`
- `Debug.Log("[ReconnectDialog] 继续上一局 → RequestResumeSession()");`
- `DrscfZ.Core.NetworkManager.Instance?.SendMessage("reset_game");`
- `Debug.Log("[ReconnectDialog] 放弃上一局 → 已发送 reset_game");`

## ShopConfirmDialogUI_ShopConfirmPanel

### ShopConfirmDialogUI

脚本路径：`Assets/Scripts/UI/ShopConfirmDialogUI.cs`

**Inspector / 绑定字段摘录**

- `private GameObject _panel;`
- `private TMP_Text _titleText;`
- `private TMP_Text _priceText;`
- `private TMP_Text _timerText;`
- `private Button   _btnConfirm;`
- `private Button   _btnCancel;`

**公开 API 摘录**

- `public void Show(ShopPurchaseConfirmPromptData data)`

**事件 / 订阅 / 入口摘录**

- `var net = NetworkManager.Instance;`
- `net.OnDisconnected += HandleDisconnected;`
- `if (net != null) net.OnDisconnected -= HandleDisconnected;`
- `private void HandleDisconnected(string reason)`

**显隐 / 动画控制摘录**

- `if (_panel != null && _panel != gameObject) _panel.SetActive(false);`
- `if (_panel != null) _panel.SetActive(false);`
- `Debug.Log($"[ShopConfirmDialogUI] OnDisconnected → ClosePanel pendingId={_pendingId} reason={reason}");`
- `ClosePanel();`
- `_panel.SetActive(true);`
- `_timerCoroutine = StartCoroutine(TickAndExpire());`
- `private void ClosePanel()`

**数据刷新 / 文案绑定摘录**

- `private void Update()`
- `if (_titleText != null) _titleText.text = $"确认购买 {itemName}？";`
- `if (_priceText != null) _priceText.text = $"价格：{data.price}";`
- `if (_timerText != null) _timerText.text = ComputeRemainSecText();`
- `ModalRegistry.RequestB(MODAL_B_ID, null);`

## ShopUI_ShopPanel

### ShopUI

脚本路径：`Assets/Scripts/UI/ShopUI.cs`

**Inspector / 绑定字段摘录**

- `private GameObject _panel;`
- `private Button _tabA;`
- `private Button _tabB;`
- `private Button _tabInventory;`
- `private Button _btnClose;`
- `private RectTransform _contentRoot;`
- `private GameObject _itemButtonPrefab;`
- `private TMP_Text _titleText;`
- `private TMP_Text _statusText;`

**公开 API 摘录**

- `public void OpenPanel(string category = "A")`
- `public void ClosePanel()`
- `public void PopulateList(ShopListData data)`
- `public void UpdateInventory(ShopInventoryData data)`
- `public static void SendShopPurchase(string itemId, string pendingId)`

**事件 / 订阅 / 入口摘录**

- `var sgm = SurvivalGameManager.Instance;`
- `sgm.OnShopPurchaseFailed  += HandleShopPurchaseFailedToast;`
- `sgm.OnShopPurchaseConfirm += HandleShopPurchaseConfirmRefresh;`
- `sgm.OnShopPurchaseFailed  -= HandleShopPurchaseFailedToast;`
- `sgm.OnShopPurchaseConfirm -= HandleShopPurchaseConfirmRefresh;`
- `private void HandleShopPurchaseFailedToast(ShopPurchaseFailedData data)`
- `private void HandleShopPurchaseConfirmRefresh(ShopPurchaseConfirmData data)`
- `var net = NetworkManager.Instance;`
- `var eq = SurvivalGameManager.Instance?.MyEquipped;`
- `int seasonDay = SurvivalGameManager.Instance?.CurrentSeasonState?.seasonDay ?? 1;`

**显隐 / 动画控制摘录**

- `if (_panel != null && _panel != gameObject) _panel.SetActive(false);`
- `if (_btnClose != null)     _btnClose.onClick.AddListener(ClosePanel);`
- `if (_panel != null) _panel.SetActive(false);`
- `public void OpenPanel(string category = "A")`
- `Debug.LogWarning("[ShopUI] OpenPanel：_panel 未绑定（MVP 占位 Log）");`
- `_panel.SetActive(true);`
- `public void ClosePanel()`
- `go.SetActive(true);`
- `btn.interactable = false;`
- `btn.interactable = true;`

**数据刷新 / 文案绑定摘录**

- `private void Update()`
- `sgm.OnShopPurchaseFailed  += HandleShopPurchaseFailedToast;`
- `sgm.OnShopPurchaseConfirm += HandleShopPurchaseConfirmRefresh;`
- `sgm.OnShopPurchaseFailed  -= HandleShopPurchaseFailedToast;`
- `sgm.OnShopPurchaseConfirm -= HandleShopPurchaseConfirmRefresh;`
- `_statusText.text = $"购买失败：{reasonText}";`
- `private void HandleShopPurchaseConfirmRefresh(ShopPurchaseConfirmData data)`
- `if (_statusText != null) _statusText.text = "";`
- `if (!ModalRegistry.Request(MODAL_A_ID, MODAL_PRIO, OnModalReplaced))`
- `Debug.LogWarning($"[ShopUI] ModalRegistry.Request 被拒（结算/升级在前），不打开商店");`

## StatusLineBannerUI_StatusLineBanner

### StatusLineBannerUI

脚本路径：`Assets/Scripts/UI/StatusLineBannerUI.cs`

**Inspector / 绑定字段摘录**

- `private RectTransform     _bannerRoot;`
- `private TextMeshProUGUI   _statusText;`

**公开 API 摘录**

- 无公开 UI API 或以 Unity 生命周期驱动

**事件 / 订阅 / 入口摘录**

- `var sgm = SurvivalGameManager.Instance;`
- `sgm.OnResourceUpdate += HandleResourceUpdate;`
- `sgm.OnPhaseChanged   += HandlePhaseChanged;`
- `sgm.OnResourceUpdate -= HandleResourceUpdate;`
- `sgm.OnPhaseChanged   -= HandlePhaseChanged;`
- `private void HandleResourceUpdate(ResourceUpdateData data)`
- `private void HandlePhaseChanged(PhaseChangedData data)`

**显隐 / 动画控制摘录**

- `[Header("横幅根节点（常驻激活，通过 _statusText.gameObject.SetActive 控制实际显隐）")]`

**数据刷新 / 文案绑定摘录**

- `[Tooltip("定时刷新间隔（秒）；OnResourceUpdate 触发时亦刷新")]`
- `private float  _nextRefreshAt;`
- `if (_statusText != null) _statusText.text = "";`
- `_nextRefreshAt = Time.time + _refreshIntervalSec;`
- `private void Update()`
- `if (Time.time >= _nextRefreshAt)`
- `RefreshStatus();`
- `sgm.OnResourceUpdate += HandleResourceUpdate;`
- `sgm.OnPhaseChanged   += HandlePhaseChanged;`
- `sgm.OnResourceUpdate -= HandleResourceUpdate;`

## SurvivalLiveRankingUI_GameUIPanel

### SurvivalLiveRankingUI

脚本路径：`Assets/Scripts/UI/SurvivalLiveRankingUI.cs`

**Inspector / 绑定字段摘录**

- `private GameObject   _panel;`
- `private TMP_Text     _titleText;`
- `private GameObject[] _rankRows = new GameObject[5];`
- `private Button _settingsButton;`

**公开 API 摘录**

- `public void TriggerSpotlight(string targetPlayerId, float durationSec)`

**事件 / 订阅 / 入口摘录**

- `var sgm = SurvivalGameManager.Instance;`
- `sgm.OnStateChanged        += OnStateChanged;`
- `sgm.OnLiveRankingReceived += OnLiveRankingReceived;`
- `sgm.OnStateChanged        -= OnStateChanged;`
- `sgm.OnLiveRankingReceived -= OnLiveRankingReceived;`

**显隐 / 动画控制摘录**

- `if (_panel != null) _panel.SetActive(false);`
- `if (_panel != null) _panel.SetActive(visible);`
- `if (row != null) row.SetActive(false);`
- `if (settings != null) settings.TogglePanel();`
- `row.SetActive(show);`
- `if (changed) StartCoroutine(FlashRow(row));`

**数据刷新 / 文案绑定摘录**

- `public class SurvivalLiveRankingUI : MonoBehaviour`
- `public static SurvivalLiveRankingUI Instance { get; private set; }`
- `private void Update()`
- `sgm.OnStateChanged        += OnStateChanged;`
- `sgm.OnLiveRankingReceived += OnLiveRankingReceived;`
- `OnStateChanged(sgm.State);`
- `sgm.OnStateChanged        -= OnStateChanged;`
- `sgm.OnLiveRankingReceived -= OnLiveRankingReceived;`
- `private void OnStateChanged(SurvivalGameManager.SurvivalState state)`
- `private void OnLiveRankingReceived(LiveRankingData data)`

### ResourceRankUI

脚本路径：`Assets/Scripts/UI/ResourceRankUI.cs`

**Inspector / 绑定字段摘录**

- `private GameObject _panel;`
- `private TextMeshProUGUI _foodTitle;`
- `private TextMeshProUGUI _coalTitle;`
- `private TextMeshProUGUI _oreTitle;`
- `private TextMeshProUGUI[] _foodRows = new TextMeshProUGUI[3];`
- `private TextMeshProUGUI[] _coalRows = new TextMeshProUGUI[3];`
- `private TextMeshProUGUI[] _oreRows = new TextMeshProUGUI[3];`

**公开 API 摘录**

- `public void Refresh()`

**事件 / 订阅 / 入口摘录**

- `var sgm = SurvivalGameManager.Instance;`
- `sgm.OnStateChanged += OnStateChanged;`
- `if (sgm != null) sgm.OnStateChanged -= OnStateChanged;`

**显隐 / 动画控制摘录**

- `if (_panel != null) _panel.SetActive(false);`
- `if (_panel != null) _panel.SetActive(visible);`

**数据刷新 / 文案绑定摘录**

- `sgm.OnStateChanged += OnStateChanged;`
- `OnStateChanged(sgm.State);`
- `if (sgm != null) sgm.OnStateChanged -= OnStateChanged;`
- `private void Update()`
- `Refresh();`
- `private void OnStateChanged(SurvivalGameManager.SurvivalState state)`
- `public void Refresh()`
- `RefreshColumn(_foodRows, "food");`
- `RefreshColumn(_coalRows, "coal");`
- `RefreshColumn(_oreRows,  "ore");`

### SupporterTopBarUI

脚本路径：`Assets/Scripts/UI/SupporterTopBarUI.cs`

**Inspector / 绑定字段摘录**

- `private TMP_Text _label;`

**公开 API 摘录**

- 无公开 UI API 或以 Unity 生命周期驱动

**事件 / 订阅 / 入口摘录**

- `var sgm = SurvivalGameManager.Instance;`
- `sgm.OnSupporterJoined    += HandleSupporterJoined;`
- `sgm.OnSupporterPromoted  += HandleSupporterPromoted;`
- `sgm.OnSupporterCountSync += HandleSupporterCountSync;`
- `sgm.OnSupporterJoined    -= HandleSupporterJoined;`
- `sgm.OnSupporterPromoted  -= HandleSupporterPromoted;`
- `sgm.OnSupporterCountSync -= HandleSupporterCountSync;`
- `private void HandleSupporterCountSync(int count)`
- `private void HandleSupporterJoined(SupporterJoinedData data)`
- `private void HandleSupporterPromoted(SupporterPromotedData data)`

**显隐 / 动画控制摘录**

- 未发现显式显隐控制

**数据刷新 / 文案绑定摘录**

- `if (_label != null) _label.text = "";`
- `private void Update()`
- `Refresh();`
- `private void Refresh()`
- `_label.text = "";`
- `_label.text = $"守护者:{guardianCount}  助威:{_supporterCount}";`

### SupporterNightFlashUI

脚本路径：`Assets/Scripts/UI/SupporterNightFlashUI.cs`

**Inspector / 绑定字段摘录**

- 无明显 UI 绑定字段

**公开 API 摘录**

- 无公开 UI API 或以 Unity 生命周期驱动

**事件 / 订阅 / 入口摘录**

- `var sgm = SurvivalGameManager.Instance;`
- `sgm.OnSupporterAction += HandleSupporterAction;`
- `if (sgm != null) sgm.OnSupporterAction -= HandleSupporterAction;`
- `private void HandleSupporterAction(SupporterActionData data)`

**显隐 / 动画控制摘录**

- 未发现显式显隐控制

**数据刷新 / 文案绑定摘录**

- `public class SupporterNightFlashUI : MonoBehaviour`
- `private void Update()`
- `var dnc = DayNightCycleManager.Instance;`
- `if (dnc != null && !dnc.IsNight) return;`

## SurvivalRankingPanel

### SurvivalRankingUI

脚本路径：`Assets/Scripts/UI/SurvivalRankingUI.cs`

**Inspector / 绑定字段摘录**

- `private GameObject _panel;`
- `private GameObject _overlay;`
- `private Button     _closeBtn;`
- `private TMP_Text   _titleText;`
- `private TMP_Text   _subtitleText;`
- `private Button _tabContribution;`
- `private Button _tabStreamer;`
- `private Sprite _tabActiveSpriteRef;`
- `private Sprite _tabInactiveSpriteRef;`
- `private GameObject _headerRow;`
- `private Sprite _medalGold;`
- `private Sprite _medalSilver;`
- `private Sprite _medalBronze;`
- `private Transform  _rowContainer;`

**公开 API 摘录**

- `public void ShowPanel()`
- `public void HidePanel()`
- `public void TogglePanel()`

**事件 / 订阅 / 入口摘录**

- `var sgm = SurvivalGameManager.Instance;`
- `sgm.OnWeeklyRankingReceived   += OnWeeklyRankingReceived;`
- `sgm.OnStreamerRankingReceived  += OnStreamerRankingReceived;`
- `sgm.OnWeeklyRankingReceived   -= OnWeeklyRankingReceived;`
- `sgm.OnStreamerRankingReceived  -= OnStreamerRankingReceived;`
- `var net = NetworkManager.Instance;`

**显隐 / 动画控制摘录**

- `_closeBtn.onClick.AddListener(HidePanel);`
- `_panel.SetActive(false);`
- `public void ShowPanel()`
- `if (_overlay != null) _overlay.SetActive(true);`
- `_panel.SetActive(true);`
- `public void HidePanel()`
- `_overlay.SetActive(false);`
- `public void TogglePanel()`
- `if (_panel.activeSelf) HidePanel();`
- `else ShowPanel();`

**数据刷新 / 文案绑定摘录**

- `public class SurvivalRankingUI : MonoBehaviour`
- `public static SurvivalRankingUI Instance { get; private set; }`
- `private WeeklyRankingData   _cachedWeekly;`
- `private StreamerRankingData _cachedStreamer;`
- `sgm.OnWeeklyRankingReceived   += OnWeeklyRankingReceived;`
- `sgm.OnStreamerRankingReceived  += OnStreamerRankingReceived;`
- `sgm.OnWeeklyRankingReceived   -= OnWeeklyRankingReceived;`
- `sgm.OnStreamerRankingReceived  -= OnStreamerRankingReceived;`
- `private void OnWeeklyRankingReceived(WeeklyRankingData data)`
- `RefreshContribution(data);`

## SurvivalSettingsUI_SurvivalSettingsPanel

### SurvivalSettingsUI

脚本路径：`Assets/Scripts/UI/SurvivalSettingsUI.cs`

**Inspector / 绑定字段摘录**

- `private GameObject _panel;`
- `private Button     _closeBtn;`
- `private Slider   _bgmSlider;`
- `private TMP_Text _bgmValueText;`
- `private Button   _bgmToggle;`
- `private TMP_Text _bgmToggleText;`
- `private Slider   _sfxSlider;`
- `private TMP_Text _sfxValueText;`
- `private Button   _sfxToggle;`
- `private TMP_Text _sfxToggleText;`
- `private TMP_Text _versionText;`
- `private AudioSource _bgmSource;`
- `private AudioSource _sfxSource;`
- `private Toggle _giftVideoToggle;`

**公开 API 摘录**

- `public void ShowPanel()`
- `public void HidePanel()`
- `public void TogglePanel()`

**事件 / 订阅 / 入口摘录**

- 未发现显式事件订阅

**显隐 / 动画控制摘录**

- `_panel.SetActive(false);`
- `if (_closeBtn    != null) _closeBtn.onClick.AddListener(HidePanel);`
- `public void ShowPanel()`
- `_panel.SetActive(true);`
- `public void HidePanel()`
- `public void TogglePanel()`
- `if (_panel.activeSelf) HidePanel();`
- `else ShowPanel();`
- `_bgmSlider.interactable = bgmOn;`
- `_sfxSlider.interactable = sfxOn;`

**数据刷新 / 文案绑定摘录**

- `_giftVideoToggle.onValueChanged.AddListener(OnGiftVideoToggleChanged);`
- `_bgmSlider.value    = bgmVol;`
- `_sfxSlider.value    = sfxVol;`
- `_versionText.text = $"极地生存法则  v{Application.version}";`
- `RefreshVolumeTexts(bgmVol, sfxVol);`
- `RefreshToggleIcons(bgmOn, sfxOn);`
- `_bgmValueText.text = $"{Mathf.RoundToInt(value * 100)}%";`
- `_sfxValueText.text = $"{Mathf.RoundToInt(value * 100)}%";`
- `RefreshToggleIcons(am.BGMEnabled, am.SFXEnabled);`
- `private void RefreshVolumeTexts(float bgm, float sfx)`

## SurvivalSettlementUI_SurvivalSettlementPanel

### SurvivalSettlementUI

脚本路径：`Assets/Scripts/UI/SurvivalSettlementUI.cs`

**Inspector / 绑定字段摘录**

- `private GameObject _screenA;`
- `private TextMeshProUGUI _resultTitleText;`
- `private TextMeshProUGUI _resultSubtitleText;`
- `private TextMeshProUGUI _topDamageText;`
- `private TextMeshProUGUI _bestRescueText;`
- `private TextMeshProUGUI _dramaticEventText;`
- `private TextMeshProUGUI _closestCallText;`
- `private GameObject _screenB;`
- `private TextMeshProUGUI _survivalDaysText;`
- `private TextMeshProUGUI _totalKillsText;`
- `private TextMeshProUGUI _totalGatherText;`
- `private TextMeshProUGUI _totalRepairText;`
- `private Transform _rankingListParent;`
- `private GameObject _rankEntryPrefab;`

**公开 API 摘录**

- `public void ShowSettlement(SettlementData data)`
- `public void HideForRecoveryWhenReady()`

**事件 / 订阅 / 入口摘录**

- `var net = NetworkManager.Instance;`
- `net.OnMessageReceived += HandleNetMessage;`
- `if (net != null) net.OnMessageReceived -= HandleNetMessage;`
- `var sgm = SurvivalGameManager.Instance;`
- `private void HandleNetMessage(string type, string dataJson)`
- `NetworkManager.Instance?.SendMessage("sync_state");`
- `var high = SurvivalGameManager.Instance != null`
- `? SurvivalGameManager.Instance.LastSettlementHighlights`
- `SurvivalGameManager.Instance?.SendStreamerSkipSettlement();`

**显隐 / 动画控制摘录**

- `gameObject.SetActive(false);`
- `_skipButton.gameObject.SetActive(_isRoomCreator && gameObject.activeInHierarchy);`
- `gameObject.SetActive(true);`
- `_sequenceCoroutine = StartCoroutine(PlaySettlementSequence(data));`
- `if (_restartButton != null) _restartButton.gameObject.SetActive(false);`
- `if (_skipButton != null) _skipButton.gameObject.SetActive(_isRoomCreator);`
- `if (_restartButton != null) _restartButton.gameObject.SetActive(true);`
- `if (_skipButton != null) _skipButton.gameObject.SetActive(false);`
- `_recoveryWatchdogCoroutine = StartCoroutine(RecoverySyncWatchdog());`
- `_screenA.SetActive(true);`

**数据刷新 / 文案绑定摘录**

- `public class SurvivalSettlementUI : MonoBehaviour`
- `[Header("Ranking System (auto-inject Top3)")]`
- `[SerializeField] private RankingSystem _rankingSystem;`
- `[SerializeField] private Button _btnViewRanking;`
- `private bool _hideAfterSequenceRequested = false;`
- `if (_btnViewRanking != null)`
- `_btnViewRanking.onClick.AddListener(OnViewRankingClicked);`
- `public void ShowSettlement(SettlementData data)`
- `_hideAfterSequenceRequested = false;`
- `if (!DrscfZ.UI.ModalRegistry.Request(MODAL_A_ID_SETTLEMENT, 80, () =>`

## TensionOverlayUI_TensionOverlay

### TensionOverlayUI

脚本路径：`Assets/Scripts/UI/TensionOverlayUI.cs`

**Inspector / 绑定字段摘录**

- `private Image _overlayImage;`

**公开 API 摘录**

- 无公开 UI API 或以 Unity 生命周期驱动

**事件 / 订阅 / 入口摘录**

- `var sgm = SurvivalGameManager.Instance;`
- `sgm.OnResourceUpdate += HandleResourceUpdate;`
- `if (sgm != null) sgm.OnResourceUpdate -= HandleResourceUpdate;`
- `private void HandleResourceUpdate(ResourceUpdateData data)`

**显隐 / 动画控制摘录**

- `private const float TENSE_AMPLITUDE = 0.30f;`
- `float alpha  = baseColor.a * (1f + amplitude * pulse);`
- `alpha        = Mathf.Clamp01(alpha);`
- `ApplyColorAlpha(baseColor, alpha);`
- `private void ApplyColorAlpha(Color baseColor, float alpha)`
- `c.a = alpha;`

**数据刷新 / 文案绑定摘录**

- `private void Update()`
- `UpdateVisual();`
- `sgm.OnResourceUpdate += HandleResourceUpdate;`
- `if (sgm != null) sgm.OnResourceUpdate -= HandleResourceUpdate;`
- `private void HandleResourceUpdate(ResourceUpdateData data)`
- `private void UpdateVisual()`

## 4. 父面板覆盖子 prefab 运行时绑定补充

> 第七轮补充：这些子 prefab 不单独建立设计资料包，但运行时绑定仍需要能从总审计追到。下表按真实 `m_SourcePrefab` 依赖生成，详细节点/文案依据见对应父面板的 `covered_child_prefabs_audit.md`。

| 子 prefab | 父面板 | 绑定脚本 | 追溯资料 |
| --- | --- | --- | --- |
| `BroadcasterDecisionHUD_DecisionHUD` | `BroadcasterPanel_BroadcasterPanelController` | `BroadcasterDecisionHUD` | `docs/BroadcasterPanel_BroadcasterPanelController/covered_child_prefabs_audit.md` |
| `StreamerPromptUI_StreamerPromptCard` | `BroadcasterPanel_BroadcasterPanelController` | `StreamerPromptUI` | `docs/BroadcasterPanel_BroadcasterPanelController/covered_child_prefabs_audit.md` |
| `BuildingStatusPanelUI_BuildingStatusPanel` | `SurvivalLiveRankingUI_GameUIPanel` | `BuildingStatusPanelUI` | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `BuildVoteUI_BuildVotePanel` | `SurvivalLiveRankingUI_GameUIPanel` | `BuildVoteUI` | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `CoopMilestoneUI_CoopMilestoneBar` | `SurvivalLiveRankingUI_GameUIPanel` | `CoopMilestoneUI` | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `EngagementReminderUI_EngagementReminder` | `SurvivalLiveRankingUI_GameUIPanel` | `EngagementReminderUI` | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `EventTriggeredUI_EventTriggeredToast` | `SurvivalLiveRankingUI_GameUIPanel` | `EventTriggeredUI` | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `ExpeditionMarkerUI_ExpeditionMarkerPanel` | `SurvivalLiveRankingUI_GameUIPanel` | `ExpeditionMarkerUI` | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `GiftAnimationUI_GiftAnimation` | `SurvivalLiveRankingUI_GameUIPanel` | `GiftAnimationUI` | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `GiftRecommendationUI_GiftIconBar` | `SurvivalLiveRankingUI_GameUIPanel` | `GiftRecommendationUI` | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `HorizontalMarqueeUI_MarqueeZone` | `SurvivalLiveRankingUI_GameUIPanel` | `HorizontalMarqueeUI` | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `NewbieHintUI` | `SurvivalLiveRankingUI_GameUIPanel` | `NewbieHintUI` | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `OnboardingBubbleUI_OnboardingBubble` | `SurvivalLiveRankingUI_GameUIPanel` | `OnboardingBubbleUI` | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `PersonalContribUI_PersonalContribBar` | `SurvivalLiveRankingUI_GameUIPanel` | `PersonalContribUI` | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `SeasonSettlementUI_SeasonSettlementPanel` | `SurvivalLiveRankingUI_GameUIPanel` | `SeasonSettlementUI` | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `SeasonTopBarUI_SeasonTopBar` | `SurvivalLiveRankingUI_GameUIPanel` | `SeasonTopBarUI` | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `SupporterActionLogUI_SupporterActionLog` | `SurvivalLiveRankingUI_GameUIPanel` | `SupporterActionLogUI` | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `SupporterJoinedToastUI_SupporterJoinedToast` | `SurvivalLiveRankingUI_GameUIPanel` | `SupporterJoinedToastUI` | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `SupporterMarqueeUI_SupporterMarquee` | `SurvivalLiveRankingUI_GameUIPanel` | `SupporterMarqueeUI` | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `SupporterPromotedMarqueeUI_SupporterPromotedMarquee` | `SurvivalLiveRankingUI_GameUIPanel` | `SupporterPromotedMarqueeUI` | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `SurvivalTopBarUI_TopBar` | `SurvivalLiveRankingUI_GameUIPanel` | `SurvivalTopBarUI` | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `TraderCaravanUI_TraderCaravanPanel` | `SurvivalLiveRankingUI_GameUIPanel` | `TraderCaravanUI` | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `VIPAnnouncementUI_VIPAnnouncement` | `SurvivalLiveRankingUI_GameUIPanel` | `VIPAnnouncementUI` | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `WaitingPhaseUI` | `SurvivalLiveRankingUI_GameUIPanel` | `WaitingPhaseUI` | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
