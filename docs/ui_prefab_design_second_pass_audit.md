# UI Prefab Design 二次查漏补缺报告

> 日期：2026-05-08  
> 范围：`Assets/Prefabs/UI/Panels/*.prefab`  
> 目标：复查漏跑、误跳过、子面板重复、脚本关系缺失、原型位置偏差和 PNG 输出完整性。

## 1. 覆盖结论

本轮重新盘点 `Panels` 目录：

| 项目 | 数量 | 结论 |
| --- | ---: | --- |
| Panels prefab 总数 | 54 | 全部已归类 |
| prefab 直接嵌套依赖 | 24 | 全部已有覆盖来源 |
| 独立主面板 / 独立界面 | 29 | 已生成独立资料包 |
| 已存在主 HUD 资料 | 1 | `SurvivalLiveRankingUI_GameUIPanel` 不重跑 |
| 跳过项 | 25 | 24 个子 prefab + 1 个已存在主 HUD |
| 含 `ai_prompt.md` 的设计资料包 | 30 | 29 个本轮生成 + 1 个既有主 HUD |

结论：没有发现漏跑 prefab。每个 `Panels` 下的 prefab 要么有 `docs/<PrefabName>/` 资料包，要么在 `docs/ui_prefab_design_batch_index.md` 中有明确跳过来源。

## 2. 子面板重复检查

直接嵌套关系复查结果：

- `BroadcasterPanel_BroadcasterPanelController` 覆盖：
  - `BroadcasterDecisionHUD_DecisionHUD`
  - `StreamerPromptUI_StreamerPromptCard`
- `SurvivalLiveRankingUI_GameUIPanel` 覆盖：
  - `BuildingStatusPanelUI_BuildingStatusPanel`
  - `BuildVoteUI_BuildVotePanel`
  - `CoopMilestoneUI_CoopMilestoneBar`
  - `EngagementReminderUI_EngagementReminder`
  - `EventTriggeredUI_EventTriggeredToast`
  - `ExpeditionMarkerUI_ExpeditionMarkerPanel`
  - `GiftAnimationUI_GiftAnimation`
  - `GiftRecommendationUI_GiftIconBar`
  - `HorizontalMarqueeUI_MarqueeZone`
  - `NewbieHintUI`
  - `OnboardingBubbleUI_OnboardingBubble`
  - `PersonalContribUI_PersonalContribBar`
  - `SeasonSettlementUI_SeasonSettlementPanel`
  - `SeasonTopBarUI_SeasonTopBar`
  - `SupporterActionLogUI_SupporterActionLog`
  - `SupporterJoinedToastUI_SupporterJoinedToast`
  - `SupporterMarqueeUI_SupporterMarquee`
  - `SupporterPromotedMarqueeUI_SupporterPromotedMarquee`
  - `SurvivalTopBarUI_TopBar`
  - `TraderCaravanUI_TraderCaravanPanel`
  - `VIPAnnouncementUI_VIPAnnouncement`
  - `WaitingPhaseUI`

结论：这些子面板不另起独立资料包，避免同一 UI 同时有主面板规范和子面板规范互相打架。

## 3. 本轮修正

二次审计发现 6 个资料包需要提高准确性，已修正对应策划案、施工说明、SVG、PNG、AI 提示词和 README：

| Prefab | 修正点 |
| --- | --- |
| `ConnectPanel` | 补充外部控制脚本 `SurvivalConnectUI.cs`，明确连接成功 1.5 秒后隐藏、失败/断线显示重试按钮。 |
| `LobbyPanel` | 补充外部控制脚本 `SurvivalIdleUI.cs`，明确只在 connected + Idle 显示，Waiting 由 `PreGameBannerUI` 接管。 |
| `FrozenStatusPanel` | 修正为底部蓝色冰晶冻结横幅，不再按中央冻结面板处理；补充 `FrozenStatusUI.ShowFrozen(duration)` 控制关系。 |
| `PauseOverlayUI_PauseOverlayPanel` | 修正为脚本动态创建的全屏暂停遮罩，仅有标题与副标题，不再预留继续/设置/退出按钮。 |
| `OreRepairFloatingText_OreRepairFloatRoot` | 修正为不依赖预建 UI 节点的世界空间 `DamageNumber.Show` 飘字，颜色为暖黄资源反馈。 |
| `SurvivalRankingPanel` | 修正为 `SurvivalRankingUI` 控制的贡献榜/主播榜双页签，不再按独立 Top3 大卡排行榜处理。 |

同时清理了 20 个策划案中空泛的 `- 暂无` 实现项，替换为可执行的通用落地说明。

## 4. 文件与图片复验

复验规则：

- 每个资料包必须包含：
  - `ui_plan_<PrefabName>.md`
  - `ui_layout_prototype_<PrefabName>.md`
  - `ui_layout_prototype_<PrefabName>.svg`
  - `ui_layout_prototype_<PrefabName>.png`
  - `ai_prompt.md`
  - `README.md`
- 每个 PNG 必须是 `1080x1920`。
- 施工原型必须标注灰盒/坐标/节点名为施工信息，AI 提示词必须要求最终图不出现这些信息。

复验结果：30 个资料包文件完整；30 张 PNG 均为 `1080x1920`。

## 5. 后续注意

- 若后续新增 `Panels/*.prefab`，需要先重新跑依赖图，判断它是独立主面板还是被现有主面板嵌套。
- 对没有直接绑定脚本的 prefab，不应简单写“未发现脚本”；需要继续查 Canvas 控制器、`RuntimeUIFactory`、`UIPrefabLoader`、`Resources.Load("UI/Panels/...")` 等运行时入口。
- 对 HUD 类界面继续优先保留中部 gameplay 安全区；对弹窗类界面必须明确遮罩和按钮层级。
