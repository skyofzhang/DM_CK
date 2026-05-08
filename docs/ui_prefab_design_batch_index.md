# UI Prefab Design Batch Index

> 批量生成日期：2026-05-07  
> 范围：`Assets/Prefabs/UI/Panels/*.prefab`  
> 规则：只为未被其他主面板直接嵌套覆盖的 prefab 生成独立资料；`SurvivalLiveRankingUI_GameUIPanel` 已跑过，不重跑。

## 本轮生成 29 套

| Prefab | 类型 | 目录 |
| --- | --- | --- |
| `AnnouncementUI_AnnouncementPanel` | 全屏公告层 | `docs/AnnouncementUI_AnnouncementPanel/` |
| `BossRushBanner` | 赛季 Boss 顶部横幅 | `docs/BossRushBanner/` |
| `BroadcasterPanel_BroadcasterPanelController` | 主播控制台主面板 | `docs/BroadcasterPanel_BroadcasterPanelController/` |
| `ChapterAnnouncementUI_ChapterAnnouncement` | 章节幕名公告 | `docs/ChapterAnnouncementUI_ChapterAnnouncement/` |
| `ConnectPanel` | 连接状态全屏页 | `docs/ConnectPanel/` |
| `DayPreviewBanner` | 夜晚预告横幅 | `docs/DayPreviewBanner/` |
| `EfficiencyRaceUI_EfficiencyRaceBanner` | 采集效率竞速横幅 | `docs/EfficiencyRaceUI_EfficiencyRaceBanner/` |
| `FairyWandMaxedBanner` | 仙女棒满额闪光跑马灯 | `docs/FairyWandMaxedBanner/` |
| `FeatureUnlockBanner` | 功能解锁提示横幅 | `docs/FeatureUnlockBanner/` |
| `FrozenStatusPanel` | 全体冻结状态提示 | `docs/FrozenStatusPanel/` |
| `GameControlUI_BottomBar` | GM/调试底部控制条 | `docs/GameControlUI_BottomBar/` |
| `GateUpgradeConfirmUI` | 城门升级确认弹窗 | `docs/GateUpgradeConfirmUI/` |
| `GiftImpactUI_GiftImpactBanner` | 礼物影响反馈横幅 | `docs/GiftImpactUI_GiftImpactBanner/` |
| `GloryMomentUI_GloryMomentBanner` | 高光时刻横幅 | `docs/GloryMomentUI_GloryMomentBanner/` |
| `LobbyPanel` | 大厅主面板 | `docs/LobbyPanel/` |
| `NightModifierUI_NightModifierBanner` | 夜间词条提示 | `docs/NightModifierUI_NightModifierBanner/` |
| `NightReportUI_NightReportPanel` | 夜晚战报面板 | `docs/NightReportUI_NightReportPanel/` |
| `OreRepairFloatingText_OreRepairFloatRoot` | 矿石修复飘字 | `docs/OreRepairFloatingText_OreRepairFloatRoot/` |
| `PauseOverlayUI_PauseOverlayPanel` | 暂停遮罩 | `docs/PauseOverlayUI_PauseOverlayPanel/` |
| `PeaceNightOverlay` | 和平夜全屏氛围层 | `docs/PeaceNightOverlay/` |
| `PreGameBannerUI_PreGameBanner` | 开局等待横幅/准备面板 | `docs/PreGameBannerUI_PreGameBanner/` |
| `ReconnectDialog` | 断线重连确认弹窗 | `docs/ReconnectDialog/` |
| `ShopConfirmDialogUI_ShopConfirmPanel` | 商店购买二次确认 | `docs/ShopConfirmDialogUI_ShopConfirmPanel/` |
| `ShopUI_ShopPanel` | 商店主面板 | `docs/ShopUI_ShopPanel/` |
| `StatusLineBannerUI_StatusLineBanner` | 局势状态细横幅 | `docs/StatusLineBannerUI_StatusLineBanner/` |
| `SurvivalRankingPanel` | 生存英雄榜/排行榜面板 | `docs/SurvivalRankingPanel/` |
| `SurvivalSettingsUI_SurvivalSettingsPanel` | 生存设置面板 | `docs/SurvivalSettingsUI_SurvivalSettingsPanel/` |
| `SurvivalSettlementUI_SurvivalSettlementPanel` | 生存结算全屏面板 | `docs/SurvivalSettlementUI_SurvivalSettlementPanel/` |
| `TensionOverlayUI_TensionOverlay` | 全屏张力危机覆盖层 | `docs/TensionOverlayUI_TensionOverlay/` |

## 本轮跳过 25 个

| Prefab | 覆盖来源 | 原因 |
| --- | --- | --- |
| `BroadcasterDecisionHUD_DecisionHUD` | `BroadcasterPanel_BroadcasterPanelController` | 直接嵌套在主面板中，随主面板资料覆盖 |
| `BuildingStatusPanelUI_BuildingStatusPanel` | `SurvivalLiveRankingUI_GameUIPanel` | 直接嵌套在主面板中，随主面板资料覆盖 |
| `BuildVoteUI_BuildVotePanel` | `SurvivalLiveRankingUI_GameUIPanel` | 直接嵌套在主面板中，随主面板资料覆盖 |
| `CoopMilestoneUI_CoopMilestoneBar` | `SurvivalLiveRankingUI_GameUIPanel` | 直接嵌套在主面板中，随主面板资料覆盖 |
| `EngagementReminderUI_EngagementReminder` | `SurvivalLiveRankingUI_GameUIPanel` | 直接嵌套在主面板中，随主面板资料覆盖 |
| `EventTriggeredUI_EventTriggeredToast` | `SurvivalLiveRankingUI_GameUIPanel` | 直接嵌套在主面板中，随主面板资料覆盖 |
| `ExpeditionMarkerUI_ExpeditionMarkerPanel` | `SurvivalLiveRankingUI_GameUIPanel` | 直接嵌套在主面板中，随主面板资料覆盖 |
| `GiftAnimationUI_GiftAnimation` | `SurvivalLiveRankingUI_GameUIPanel` | 直接嵌套在主面板中，随主面板资料覆盖 |
| `GiftRecommendationUI_GiftIconBar` | `SurvivalLiveRankingUI_GameUIPanel` | 直接嵌套在主面板中，随主面板资料覆盖 |
| `HorizontalMarqueeUI_MarqueeZone` | `SurvivalLiveRankingUI_GameUIPanel` | 直接嵌套在主面板中，随主面板资料覆盖 |
| `NewbieHintUI` | `SurvivalLiveRankingUI_GameUIPanel` | 直接嵌套在主面板中，随主面板资料覆盖 |
| `OnboardingBubbleUI_OnboardingBubble` | `SurvivalLiveRankingUI_GameUIPanel` | 直接嵌套在主面板中，随主面板资料覆盖 |
| `PersonalContribUI_PersonalContribBar` | `SurvivalLiveRankingUI_GameUIPanel` | 直接嵌套在主面板中，随主面板资料覆盖 |
| `SeasonSettlementUI_SeasonSettlementPanel` | `SurvivalLiveRankingUI_GameUIPanel` | 直接嵌套在主面板中，随主面板资料覆盖 |
| `SeasonTopBarUI_SeasonTopBar` | `SurvivalLiveRankingUI_GameUIPanel` | 直接嵌套在主面板中，随主面板资料覆盖 |
| `StreamerPromptUI_StreamerPromptCard` | `BroadcasterPanel_BroadcasterPanelController` | 直接嵌套在主面板中，随主面板资料覆盖 |
| `SupporterActionLogUI_SupporterActionLog` | `SurvivalLiveRankingUI_GameUIPanel` | 直接嵌套在主面板中，随主面板资料覆盖 |
| `SupporterJoinedToastUI_SupporterJoinedToast` | `SurvivalLiveRankingUI_GameUIPanel` | 直接嵌套在主面板中，随主面板资料覆盖 |
| `SupporterMarqueeUI_SupporterMarquee` | `SurvivalLiveRankingUI_GameUIPanel` | 直接嵌套在主面板中，随主面板资料覆盖 |
| `SupporterPromotedMarqueeUI_SupporterPromotedMarquee` | `SurvivalLiveRankingUI_GameUIPanel` | 直接嵌套在主面板中，随主面板资料覆盖 |
| `SurvivalLiveRankingUI_GameUIPanel` | `(existing)` | 已存在 docs/SurvivalLiveRankingUI_GameUIPanel，本轮不重跑 |
| `SurvivalTopBarUI_TopBar` | `SurvivalLiveRankingUI_GameUIPanel` | 直接嵌套在主面板中，随主面板资料覆盖 |
| `TraderCaravanUI_TraderCaravanPanel` | `SurvivalLiveRankingUI_GameUIPanel` | 直接嵌套在主面板中，随主面板资料覆盖 |
| `VIPAnnouncementUI_VIPAnnouncement` | `SurvivalLiveRankingUI_GameUIPanel` | 直接嵌套在主面板中，随主面板资料覆盖 |
| `WaitingPhaseUI` | `SurvivalLiveRankingUI_GameUIPanel` | 直接嵌套在主面板中，随主面板资料覆盖 |

## 说明

- `BroadcasterDecisionHUD_DecisionHUD` 与 `StreamerPromptUI_StreamerPromptCard` 已在 `BroadcasterPanel_BroadcasterPanelController` 的资料中作为子面板覆盖。
- `SurvivalLiveRankingUI_GameUIPanel` 及其直接嵌套的 HUD、礼物、跑马灯、事件、支持者子面板沿用既有资料，不重复生成。
- 每个生成目录均包含策划案、施工 SVG、PNG 参考、布局说明、AI 提示词和 README。

## 2026-05-08 二次查漏补缺

二次复查报告：`docs/ui_prefab_design_second_pass_audit.md`

本轮重新核对了 54 个 `Panels` prefab、24 条 prefab 直接嵌套依赖、30 个资料包文件完整性与 PNG 尺寸。结论是没有漏跑 prefab；全部 prefab 已按“独立资料包 / 主面板子模块 / 已存在主 HUD”归类。

本轮重点修正了 6 个资料包：

- `ConnectPanel`：补充 `SurvivalConnectUI.cs` 外部控制关系。
- `LobbyPanel`：补充 `SurvivalIdleUI.cs` 的 Idle 显示条件和按钮行为。
- `FrozenStatusPanel`：修正为底部蓝色冻结横幅。
- `PauseOverlayUI_PauseOverlayPanel`：修正为脚本动态创建的暂停遮罩，无继续/设置/退出按钮。
- `OreRepairFloatingText_OreRepairFloatRoot`：修正为 `DamageNumber.Show` 世界空间飘字。
- `SurvivalRankingPanel`：修正为贡献榜 / 主播榜双页签排行榜。

同时清理了部分策划案中空泛的 `暂无` 实现项，并重新渲染校验相关 PNG。30 张 PNG 均为 `1080x1920`。

## 2026-05-08 第三轮查漏补缺

第三轮复查报告：`docs/ui_prefab_design_third_pass_audit.md`

新增全量矩阵：`docs/ui_prefab_design_full_prefab_matrix.md`

新增父面板子 prefab 覆盖附录：

- `docs/SurvivalLiveRankingUI_GameUIPanel/covered_child_prefabs_audit.md`
- `docs/BroadcasterPanel_BroadcasterPanelController/covered_child_prefabs_audit.md`

本轮没有发现需要新增独立资料包的 prefab；重点补齐了 24 个跳过子 prefab 的证据链和运行时加载别名风险说明。

## 2026-05-08 第四轮查漏补缺

第四轮复查报告：`docs/ui_prefab_design_fourth_pass_audit.md`

新增运行时绑定深度审计：`docs/ui_prefab_runtime_binding_audit.md`

本轮用 Chrome headless 从 SVG 重新渲染了全部 30 张 PNG，并修正 `SurvivalLiveRankingUI_GameUIPanel/ai_prompt.md` 的标准结构。未发现新增漏跑 prefab，也未发现需要从 `docs/` 根目录迁移的同名旧界面资料。

## 2026-05-08 第五轮查漏补缺

第五轮复查报告：`docs/ui_prefab_design_fifth_pass_audit.md`

本轮做结构、视觉、编码和去重校验：

- 30 个 SVG 均可作为 XML 解析，且 `width=1080`、`height=1920`、`viewBox=0 0 1080 1920`。
- 30 张 PNG 均为 `1080x1920`，并通过非空/非单色抽样检查。
- 30 个 `ai_prompt.md` 均具备标准章节、画布尺寸说明和施工标注反向约束。
- 未发现 UI 资料明显乱码。
- 24 个被父面板覆盖的子 prefab 没有误建独立资料目录。

第五轮没有发现需要新增独立资料包、重跑内容或移动旧文件的问题。

## 2026-05-08 第六轮查漏补缺

第六轮复查报告：`docs/ui_prefab_design_sixth_pass_audit.md`

本轮做交叉引用一致性审计：资料包、prefab 路径、README 文件清单、AI prompt 参考 PNG、SVG 标题/尺寸、总索引、全量矩阵、父子附录全部互相核对。未发现错链、漏链、旧名或重复目录。

## 2026-05-08 第七轮查漏补缺

第七轮复查报告：`docs/ui_prefab_design_seventh_pass_audit.md`

本轮做更深的 prefab 依赖与资料闭环审计：解析 prefab/meta GUID、m_SourcePrefab 父子关系、m_Script 绑定、README/plan/layout/prompt/SVG/PNG 标准产物、总索引、全量矩阵、运行时绑定审计和父子附录。结论为 0 个问题，未发现遗漏、重复建包、错链、尺寸错误或礼物贴纸要求缺失。

## 2026-05-08 第八轮查漏补缺

第八轮复查报告：`docs/ui_prefab_design_eighth_pass_audit.md`

本轮从可施工质量角度复核：真实节点/脚本/文案引用、layout 坐标边界、SVG/PNG 非空产物、AI prompt 中文可读性与施工标注排除、矩阵/运行时/索引闭环。结论为 0 个问题。

## 2026-05-08 第九轮查漏补缺

第九轮复查报告：`docs/ui_prefab_design_ninth_pass_audit.md`

第九轮回流校验：`docs/ui_prefab_design_ninth_pass_backflow_check.md`

新增切图回流总索引：`docs/ui_prefab_asset_slicing_backflow_index.md`

本轮补齐“设计图生成 UI 切图后反向回 prefab”的链路：30 个资料包均新增 `ui_asset_slicing_backflow_<PrefabName>.md`，把 layout 坐标、建议切图文件名、Unity Sprite 导入设置、九宫格、RectTransform 回填、TMP 动态文案保留策略全部写清。

## 2026-05-08 第十轮查漏补缺

第十轮复查报告：`docs/ui_prefab_design_tenth_pass_audit.md`

本轮把第九轮新增的切图回流规范反向对照真实 prefab：解析 `m_Name`、`m_SourcePrefab`、`m_Script`，逐行确认回流表能命中真实节点、子 prefab、脚本名、安全区参考，或已明确标记为“施工目标需创建/改名”；并检查切图文件名、RectTransform 回填、动态 TMP 不烘焙、脚本字段/按钮事件/显隐逻辑不断链。结论为 0 个问题。

## 2026-05-08 第十一轮查漏补缺

第十一轮复查报告：`docs/ui_prefab_design_eleventh_pass_audit.md`

新增机器可读切图 manifest：`docs/ui_prefab_asset_slice_manifest.json`、`docs/ui_prefab_asset_slice_manifest.csv`、`docs/ui_prefab_asset_slice_manifest.md`

本轮把 30 份切图回流规范整理成 162 条机器可读记录，并校验 prefab/spec/PNG/prompt 路径、切图文件名、Unity 目标路径、noSlice、安全区、九宫格、动态 TMP 保留和 allowCreateOrRename 状态。结论为 0 个问题。
