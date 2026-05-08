# UI Prefab 全量准确性矩阵

> 日期：2026-05-08  
> 范围：`Assets/Prefabs/UI/Panels/*.prefab`  
> 用途：逐个确认 54 个 prefab 的资料状态、父子覆盖、脚本绑定、节点/文案计数和文档去向。

## 1. 总览

| 指标 | 数量 |
| --- | ---: |
| Panels prefab 总数 | 54 |
| 独立资料包 | 29 |
| 已存在主 HUD 资料 | 1 |
| 父面板覆盖子 prefab | 24 |
| prefab 直接嵌套依赖 | 24 |

## 2. 全量矩阵

| Prefab | 状态 | 覆盖来源 | 绑定脚本 | 节点/文案 | 直接子 prefab | 文档去向 |
| --- | --- | --- | --- | ---: | --- | --- |
| `AnnouncementUI_AnnouncementPanel` | 独立资料包 | - | `AnnouncementUI` | 2 / 0 | - | `docs/AnnouncementUI_AnnouncementPanel/` |
| `BossRushBanner` | 独立资料包 | - | `BossRushBanner` | 7 / 4 | - | `docs/BossRushBanner/` |
| `BroadcasterDecisionHUD_DecisionHUD` | 父面板覆盖子 prefab | `BroadcasterPanel_BroadcasterPanelController` | `BroadcasterDecisionHUD` | 18 / 4 | - | `docs/BroadcasterPanel_BroadcasterPanelController/covered_child_prefabs_audit.md` |
| `BroadcasterPanel_BroadcasterPanelController` | 独立资料包 | - | `FeatureLockOverlay`、`BroadcasterPanel` | 32 / 11 | `StreamerPromptUI_StreamerPromptCard`、`BroadcasterDecisionHUD_DecisionHUD` | `docs/BroadcasterPanel_BroadcasterPanelController/` |
| `BuildVoteUI_BuildVotePanel` | 父面板覆盖子 prefab | `SurvivalLiveRankingUI_GameUIPanel` | `BuildVoteUI` | 11 / 8 | - | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `BuildingStatusPanelUI_BuildingStatusPanel` | 父面板覆盖子 prefab | `SurvivalLiveRankingUI_GameUIPanel` | `BuildingStatusPanelUI` | 20 / 5 | - | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `ChapterAnnouncementUI_ChapterAnnouncement` | 独立资料包 | - | `ChapterAnnouncementUI` | 3 / 0 | - | `docs/ChapterAnnouncementUI_ChapterAnnouncement/` |
| `ConnectPanel` | 独立资料包 | - | 无 | 6 / 4 | - | `docs/ConnectPanel/` |
| `CoopMilestoneUI_CoopMilestoneBar` | 父面板覆盖子 prefab | `SurvivalLiveRankingUI_GameUIPanel` | `CoopMilestoneUI` | 4 / 2 | - | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `DayPreviewBanner` | 独立资料包 | - | `DayPreviewBanner` | 4 / 0 | - | `docs/DayPreviewBanner/` |
| `EfficiencyRaceUI_EfficiencyRaceBanner` | 独立资料包 | - | `EfficiencyRaceUI` | 2 / 0 | - | `docs/EfficiencyRaceUI_EfficiencyRaceBanner/` |
| `EngagementReminderUI_EngagementReminder` | 父面板覆盖子 prefab | `SurvivalLiveRankingUI_GameUIPanel` | `EngagementReminderUI` | 2 / 0 | - | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `EventTriggeredUI_EventTriggeredToast` | 父面板覆盖子 prefab | `SurvivalLiveRankingUI_GameUIPanel` | `EventTriggeredUI` | 4 / 2 | - | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `ExpeditionMarkerUI_ExpeditionMarkerPanel` | 父面板覆盖子 prefab | `SurvivalLiveRankingUI_GameUIPanel` | `ExpeditionMarkerUI` | 4 / 1 | - | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `FairyWandMaxedBanner` | 独立资料包 | - | `FairyWandMaxedBanner` | 3 / 0 | - | `docs/FairyWandMaxedBanner/` |
| `FeatureUnlockBanner` | 独立资料包 | - | `FeatureUnlockBanner` | 3 / 1 | - | `docs/FeatureUnlockBanner/` |
| `FrozenStatusPanel` | 独立资料包 | - | 无 | 3 / 2 | - | `docs/FrozenStatusPanel/` |
| `GameControlUI_BottomBar` | 独立资料包 | - | `GameControlUI` | 23 / 12 | - | `docs/GameControlUI_BottomBar/` |
| `GateUpgradeConfirmUI` | 独立资料包 | - | `GateUpgradeConfirmUI` | 11 / 7 | - | `docs/GateUpgradeConfirmUI/` |
| `GiftAnimationUI_GiftAnimation` | 父面板覆盖子 prefab | `SurvivalLiveRankingUI_GameUIPanel` | `GiftAnimationUI` | 0 / 0 | - | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `GiftImpactUI_GiftImpactBanner` | 独立资料包 | - | `GiftImpactUI` | 1 / 0 | - | `docs/GiftImpactUI_GiftImpactBanner/` |
| `GiftRecommendationUI_GiftIconBar` | 父面板覆盖子 prefab | `SurvivalLiveRankingUI_GameUIPanel` | `GiftRecommendationUI` | 26 / 12 | - | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `GloryMomentUI_GloryMomentBanner` | 独立资料包 | - | `GloryMomentUI` | 5 / 0 | - | `docs/GloryMomentUI_GloryMomentBanner/` |
| `HorizontalMarqueeUI_MarqueeZone` | 父面板覆盖子 prefab | `SurvivalLiveRankingUI_GameUIPanel` | `HorizontalMarqueeUI` | 0 / 0 | - | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `LobbyPanel` | 独立资料包 | - | 无 | 10 / 6 | - | `docs/LobbyPanel/` |
| `NewbieHintUI` | 父面板覆盖子 prefab | `SurvivalLiveRankingUI_GameUIPanel` | `NewbieHintUI` | 2 / 0 | - | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `NightModifierUI_NightModifierBanner` | 独立资料包 | - | `NightModifierUI` | 3 / 0 | - | `docs/NightModifierUI_NightModifierBanner/` |
| `NightReportUI_NightReportPanel` | 独立资料包 | - | `NightReportUI` | 3 / 0 | - | `docs/NightReportUI_NightReportPanel/` |
| `OnboardingBubbleUI_OnboardingBubble` | 父面板覆盖子 prefab | `SurvivalLiveRankingUI_GameUIPanel` | `OnboardingBubbleUI` | 2 / 0 | - | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `OreRepairFloatingText_OreRepairFloatRoot` | 独立资料包 | - | `OreRepairFloatingText` | 0 / 0 | - | `docs/OreRepairFloatingText_OreRepairFloatRoot/` |
| `PauseOverlayUI_PauseOverlayPanel` | 独立资料包 | - | `PauseOverlayUI` | 0 / 0 | - | `docs/PauseOverlayUI_PauseOverlayPanel/` |
| `PeaceNightOverlay` | 独立资料包 | - | `PeaceNightOverlay` | 4 / 1 | - | `docs/PeaceNightOverlay/` |
| `PersonalContribUI_PersonalContribBar` | 父面板覆盖子 prefab | `SurvivalLiveRankingUI_GameUIPanel` | `PersonalContribUI` | 1 / 0 | - | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `PreGameBannerUI_PreGameBanner` | 独立资料包 | - | `PreGameBannerUI` | 7 / 4 | - | `docs/PreGameBannerUI_PreGameBanner/` |
| `ReconnectDialog` | 独立资料包 | - | `ReconnectDialog` | 8 / 4 | - | `docs/ReconnectDialog/` |
| `SeasonSettlementUI_SeasonSettlementPanel` | 父面板覆盖子 prefab | `SurvivalLiveRankingUI_GameUIPanel` | `SeasonSettlementUI` | 5 / 4 | - | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `SeasonTopBarUI_SeasonTopBar` | 父面板覆盖子 prefab | `SurvivalLiveRankingUI_GameUIPanel` | `SeasonTopBarUI` | 3 / 3 | - | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `ShopConfirmDialogUI_ShopConfirmPanel` | 独立资料包 | - | `ShopConfirmDialogUI` | 8 / 6 | - | `docs/ShopConfirmDialogUI_ShopConfirmPanel/` |
| `ShopUI_ShopPanel` | 独立资料包 | - | `ShopUI` | 18 / 6 | - | `docs/ShopUI_ShopPanel/` |
| `StatusLineBannerUI_StatusLineBanner` | 独立资料包 | - | `StatusLineBannerUI` | 1 / 0 | - | `docs/StatusLineBannerUI_StatusLineBanner/` |
| `StreamerPromptUI_StreamerPromptCard` | 父面板覆盖子 prefab | `BroadcasterPanel_BroadcasterPanelController` | `StreamerPromptUI` | 2 / 0 | - | `docs/BroadcasterPanel_BroadcasterPanelController/covered_child_prefabs_audit.md` |
| `SupporterActionLogUI_SupporterActionLog` | 父面板覆盖子 prefab | `SurvivalLiveRankingUI_GameUIPanel` | `SupporterActionLogUI` | 1 / 0 | - | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `SupporterJoinedToastUI_SupporterJoinedToast` | 父面板覆盖子 prefab | `SurvivalLiveRankingUI_GameUIPanel` | `SupporterJoinedToastUI` | 1 / 0 | - | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `SupporterMarqueeUI_SupporterMarquee` | 父面板覆盖子 prefab | `SurvivalLiveRankingUI_GameUIPanel` | `SupporterMarqueeUI` | 1 / 0 | - | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `SupporterPromotedMarqueeUI_SupporterPromotedMarquee` | 父面板覆盖子 prefab | `SurvivalLiveRankingUI_GameUIPanel` | `SupporterPromotedMarqueeUI` | 1 / 0 | - | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `SurvivalLiveRankingUI_GameUIPanel` | 已存在主 HUD 资料 | - | `SurvivalLiveRankingUI`、`ResourceRankUI`、`SupporterTopBarUI`、`SupporterNightFlashUI` | 58 / 36 | `EventTriggeredUI_EventTriggeredToast`、`GiftRecommendationUI_GiftIconBar`、`OnboardingBubbleUI_OnboardingBubble`、`BuildVoteUI_BuildVotePanel`、`SupporterMarqueeUI_SupporterMarquee`、`SeasonSettlementUI_SeasonSettlementPanel`、`TraderCaravanUI_TraderCaravanPanel`、`SupporterPromotedMarqueeUI_SupporterPromotedMarquee`、`SupporterActionLogUI_SupporterActionLog`、`CoopMilestoneUI_CoopMilestoneBar`、`NewbieHintUI`、`VIPAnnouncementUI_VIPAnnouncement`、`GiftAnimationUI_GiftAnimation`、`SurvivalTopBarUI_TopBar`、`EngagementReminderUI_EngagementReminder`、`HorizontalMarqueeUI_MarqueeZone`、`SeasonTopBarUI_SeasonTopBar`、`PersonalContribUI_PersonalContribBar`、`BuildingStatusPanelUI_BuildingStatusPanel`、`SupporterJoinedToastUI_SupporterJoinedToast`、`ExpeditionMarkerUI_ExpeditionMarkerPanel`、`WaitingPhaseUI` | `docs/SurvivalLiveRankingUI_GameUIPanel/` |
| `SurvivalRankingPanel` | 独立资料包 | - | 无 | 215 / 59 | - | `docs/SurvivalRankingPanel/` |
| `SurvivalSettingsUI_SurvivalSettingsPanel` | 独立资料包 | - | `SurvivalSettingsUI` | 36 / 9 | - | `docs/SurvivalSettingsUI_SurvivalSettingsPanel/` |
| `SurvivalSettlementUI_SurvivalSettlementPanel` | 独立资料包 | - | `SurvivalSettlementUI` | 96 / 57 | - | `docs/SurvivalSettlementUI_SurvivalSettlementPanel/` |
| `SurvivalTopBarUI_TopBar` | 父面板覆盖子 prefab | `SurvivalLiveRankingUI_GameUIPanel` | `SurvivalTopBarUI` | 39 / 14 | - | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `TensionOverlayUI_TensionOverlay` | 独立资料包 | - | `TensionOverlayUI` | 0 / 0 | - | `docs/TensionOverlayUI_TensionOverlay/` |
| `TraderCaravanUI_TraderCaravanPanel` | 父面板覆盖子 prefab | `SurvivalLiveRankingUI_GameUIPanel` | `TraderCaravanUI` | 7 / 5 | - | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `VIPAnnouncementUI_VIPAnnouncement` | 父面板覆盖子 prefab | `SurvivalLiveRankingUI_GameUIPanel` | `VIPAnnouncementUI` | 3 / 0 | - | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |
| `WaitingPhaseUI` | 父面板覆盖子 prefab | `SurvivalLiveRankingUI_GameUIPanel` | `WaitingPhaseUI` | 4 / 0 | - | `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md` |

## 3. 运行时加载别名风险

`SurvivalGameManager.EnsureRuntimeUI<T>(objectName)` 和 `UIPrefabLoader` 支持通过 `UI/Panels/<id>` 加载。部分 objectName 与当前 prefab 文件名不完全一致；这不影响本轮文档覆盖，但如果未来要独立运行时加载这些子 prefab，需要在 `UIPrefabLoader` 中注册 id 别名，或保持父 prefab 已实例化。

| Runtime id | 复查备注 |
| --- | --- |
| `TopFloatingTextUI` | 无同名 Panels prefab；运行时可能走 RuntimeUIFactory 兜底 |
| `FortressDayBadgeUI` | 无同名 Panels prefab；运行时可能走 RuntimeUIFactory 兜底 |
| `RoomFailedBannerUI` | 无同名 Panels prefab；运行时可能走 RuntimeUIFactory 兜底 |
| `SurvivalLoadingUI` | 无同名 Panels prefab；运行时可能走 RuntimeUIFactory 兜底 |
| `SurvivalTopBarUI` | 对应嵌套 prefab 为 SurvivalTopBarUI_TopBar，若独立加载需 UIPrefabLoader 注册别名 |
| `SeasonTopBarUI` | 对应嵌套 prefab 为 SeasonTopBarUI_SeasonTopBar，若独立加载需 UIPrefabLoader 注册别名 |
| `BroadcasterDecisionHUD` | 对应嵌套 prefab 为 BroadcasterDecisionHUD_DecisionHUD，已由 BroadcasterPanel 覆盖 |
| `BuildVoteUI` | 对应嵌套 prefab 为 BuildVoteUI_BuildVotePanel，已由 SurvivalLiveRankingUI 覆盖 |
| `TraderCaravanUI` | 对应嵌套 prefab 为 TraderCaravanUI_TraderCaravanPanel，已由 SurvivalLiveRankingUI 覆盖 |
| `BuildingStatusPanelUI` | 对应嵌套 prefab 为 BuildingStatusPanelUI_BuildingStatusPanel，已由 SurvivalLiveRankingUI 覆盖 |
| `ExpeditionMarkerUI` | 对应嵌套 prefab 为 ExpeditionMarkerUI_ExpeditionMarkerPanel，已由 SurvivalLiveRankingUI 覆盖 |
| `SeasonSettlementUI` | 对应嵌套 prefab 为 SeasonSettlementUI_SeasonSettlementPanel，已由 SurvivalLiveRankingUI 覆盖 |
| `WaitingPhaseUI` | 存在同名 prefab，已由 SurvivalLiveRankingUI 覆盖 |
| `GiftAnimationUI` | 对应嵌套 prefab 为 GiftAnimationUI_GiftAnimation，已由 SurvivalLiveRankingUI 覆盖 |
| `GiftRecommendationUI` | 对应嵌套 prefab 为 GiftRecommendationUI_GiftIconBar，已由 SurvivalLiveRankingUI 覆盖 |
| `VIPAnnouncementUI` | 对应嵌套 prefab 为 VIPAnnouncementUI_VIPAnnouncement，已由 SurvivalLiveRankingUI 覆盖 |

## 4. 结论

没有发现漏跑 prefab。当前最重要的维护规则是：不要为父面板直接嵌套的子 prefab 重复建立独立资料包；如果需要更细的依据，查看父目录下的 `covered_child_prefabs_audit.md`。