# UI Prefab Design 第七轮查漏补缺报告

> 日期：2026-05-08  
> 范围：Assets/Prefabs/UI/Panels 下 54 个 prefab、docs 下 30 个设计资料包、24 个父面板覆盖子 prefab  
> 目标：在第六轮交叉引用审计之后，再做一轮更深的 prefab 依赖、脚本绑定、父子覆盖、提示词硬约束和产物归档复核。

## 1. 检查统计

| 检查项 | 数量 |
| --- | ---: |
| Panels prefab 总数 | 54 |
| 实际资料包总数 | 30 |
| 应有资料包总数 | 30 |
| 跳过子 prefab 总数 | 24 |
| 有 Panels 子 prefab 的父面板数 | 2 |
| Panels prefab 直接嵌套关系数 | 24 |
| 脚本 GUID 映射数量 | 114 |
| PNG 尺寸校验数量 | 30 |
| 发现问题 | 0 |

## 2. 父子 prefab 覆盖复核

| 父 prefab | 覆盖子 prefab 数 | 子 prefab |
| --- | ---: | --- |
| `BroadcasterPanel_BroadcasterPanelController` | 2 | `BroadcasterDecisionHUD_DecisionHUD`、`StreamerPromptUI_StreamerPromptCard` |
| `SurvivalLiveRankingUI_GameUIPanel` | 22 | `BuildVoteUI_BuildVotePanel`、`BuildingStatusPanelUI_BuildingStatusPanel`、`CoopMilestoneUI_CoopMilestoneBar`、`EngagementReminderUI_EngagementReminder`、`EventTriggeredUI_EventTriggeredToast`、`ExpeditionMarkerUI_ExpeditionMarkerPanel`、`GiftAnimationUI_GiftAnimation`、`GiftRecommendationUI_GiftIconBar`、`HorizontalMarqueeUI_MarqueeZone`、`NewbieHintUI`、`OnboardingBubbleUI_OnboardingBubble`、`PersonalContribUI_PersonalContribBar`、`SeasonSettlementUI_SeasonSettlementPanel`、`SeasonTopBarUI_SeasonTopBar`、`SupporterActionLogUI_SupporterActionLog`、`SupporterJoinedToastUI_SupporterJoinedToast`、`SupporterMarqueeUI_SupporterMarquee`、`SupporterPromotedMarqueeUI_SupporterPromotedMarquee`、`SurvivalTopBarUI_TopBar`、`TraderCaravanUI_TraderCaravanPanel`、`VIPAnnouncementUI_VIPAnnouncement`、`WaitingPhaseUI` |

结论：当前只有上表两个父面板直接嵌套 Panels 子 prefab。所有 24 个子 prefab 均按“父资料包覆盖 + covered_child_prefabs_audit.md 追溯”的方式处理，没有重复独立建包。

## 3. 本轮新增检查点

- 解析 prefab meta GUID，确认资料包数量与应跑 prefab 数一致。
- 解析 m_SourcePrefab，确认跳过项全部能追到父面板。
- 解析 m_Script GUID，确认父子附录与运行时绑定审计能追到脚本依据。
- 复查每个资料包 6 个标准文件：plan、layout md、SVG、PNG、ai_prompt、README。
- 复查所有 SVG 的 1080x1920 声明与所有 PNG 文件头尺寸。
- 复查 AI prompt 标准章节、参考 PNG、1080x1920、不要出现/不要显示 prefab/坐标等硬约束。
- 复查 docs 根目录没有残留未归档的单界面 plan/layout 原型文件。
- 复查 SurvivalLiveRankingUI 的礼物贴纸区域，确认不是小图标按钮条，而是包含图标、名称、档位/消耗和游戏内效果说明。

## 4. 发现问题

未发现遗漏、重复建包、错链、尺寸错误、乱码或提示词结构问题。

## 5. 结论

第七轮复核通过。当前资料包覆盖关系是闭合的：30 个独立资料包完整存在，24 个子 prefab 均由父面板资料追溯覆盖，54 个 Panels prefab 均在矩阵和运行时绑定审计中有记录，30 张 PNG 均为 1080x1920，未发现重复资料包或遗漏子面板。