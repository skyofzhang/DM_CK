# SurvivalLiveRankingUI_GameUIPanel 已覆盖子 prefab 审计附录

> 日期：2026-05-08  
> 父 prefab：`Assets/Prefabs/UI/Panels/SurvivalLiveRankingUI_GameUIPanel.prefab`  
> 目的：给“已跳过、不单独跑资料包”的子 prefab 提供可追溯审计，避免重复设计的同时保留节点/脚本/文案依据。

## 1. 覆盖原则

- 以下子 prefab 均直接嵌套在 `SurvivalLiveRankingUI_GameUIPanel` 内，不另建 `docs/<ChildName>/`。
- 子 prefab 的坐标、显隐、视觉强弱必须服从父面板资料。
- 若后续某个子 prefab 从父面板拆出成为独立入口，再单独跑一套完整资料。

父面板绑定脚本：`SurvivalLiveRankingUI`、`ResourceRankUI`、`SupporterTopBarUI`、`SupporterNightFlashUI`  
父面板直接子 prefab 数：22

## 2. 子 prefab 明细

| 子 prefab | 绑定脚本 | 节点/文案计数 | 节点摘录 | 文案摘录 | 覆盖说明 |
| --- | --- | --- | --- | --- | --- |
| `EventTriggeredUI_EventTriggeredToast` | `EventTriggeredUI` | 4 / 2 | `Bg`、`Root`、`NameLabel`、`DescLabel` | `事件名`、`简介` | 事件触发 toast：危险/奖励事件短时反馈，和 GiftImpact/VIP/Marquee 同属临时反馈层。 |
| `GiftRecommendationUI_GiftIconBar` | `GiftRecommendationUI` | 26 / 12 | `NameLabel`、`爱的爆炸`、`EffectLabel`、`Icon`、`神秘空投`、`能量电池` | `仙女棒`、`超级补给 城门+300`、`食物+100 城门+200`、`炉温+30 效率+30%` | 礼物推荐/贴纸：必须包含礼物图标、礼物名、档位/成本和游戏内效果说明，不能只做小图标按钮条。 |
| `OnboardingBubbleUI_OnboardingBubble` | `OnboardingBubbleUI` | 2 / 0 | `BubbleText`、`BubbleRoot` | 运行时注入 | 新手气泡：引导指定操作或礼物规则，跟随主 HUD 节奏，不单独做整屏设计。 |
| `BuildVoteUI_BuildVotePanel` | `BuildVoteUI` | 11 / 8 | `VoteButton_3`、`VoteButton_2`、`VoteCount`、`VoteButton_1`、`VoteLabel`、`ProposerText` | `0 票`、`建筑`、`发起建造投票`、`0 秒` | 建造投票：属于主 HUD 的临时 A 类/准 A 类投票层，不能单独脱离主 HUD 规划。 |
| `SupporterMarqueeUI_SupporterMarquee` | `SupporterMarqueeUI` | 1 / 0 | `Label` | 运行时注入 | 支持者跑马灯：高价值行为播报，和 HorizontalMarquee 共用信息流原则。 |
| `SeasonSettlementUI_SeasonSettlementPanel` | `SeasonSettlementUI` | 5 / 4 | `LblTopList`、`Label`、`BtnClose`、`LblTitle`、`LblSurvivingRooms` | `(等待数据)`、`关闭`、`S1 赛季结束 / 下赛季主题预告：血月`、`全服幸存房间：- 间` | 赛季结算：主 HUD 非 Running 状态的结算层，已有主 HUD 文档约束其出现时机。 |
| `TraderCaravanUI_TraderCaravanPanel` | `TraderCaravanUI` | 7 / 5 | `TitleText`、`Label`、`BtnAccept`、`DescText`、`BtnCancel`、`CountdownText` | `商队交易`、`拒绝`、`接受`、`"主播决定： / 接受 200食物 + 50矿石` | 商队事件：主 HUD 事件浮层，需避开中心战斗目标并和 BuildVote/事件 toast 错位。 |
| `SupporterPromotedMarqueeUI_SupporterPromotedMarquee` | `SupporterPromotedMarqueeUI` | 1 / 0 | `Label` | 运行时注入 | 支持者晋升播报：金色/极光高亮，但仍是短时主 HUD 信息层。 |
| `SupporterActionLogUI_SupporterActionLog` | `SupporterActionLogUI` | 1 / 0 | `Container` | 运行时注入 | 支持者行为日志：右下/侧边窄列表，和弹幕、跑马灯互补。 |
| `CoopMilestoneUI_CoopMilestoneBar` | `CoopMilestoneUI` | 4 / 2 | `ProgressFill`、`ProgressBg`、`ProgressText`、`TitleText` | `0/500 — 再 500 解锁 全员效率 +10%`、`协作目标：众志成城` | 协作里程碑：主 HUD 进度反馈层，和资源、礼物贡献节奏联动。 |
| `NewbieHintUI` | `NewbieHintUI` | 2 / 0 | `WelcomeLabel`、`BarrageLabel` | 运行时注入 | 新手提示：主 HUD 新手引导层，出现频率低，应弱化并可关闭。 |
| `VIPAnnouncementUI_VIPAnnouncement` | `VIPAnnouncementUI` | 3 / 0 | `VideoContainer`、`VIPText`、`VideoDisplay` | 运行时注入 | VIP 公告：高价值加入/礼物反馈层，金色高亮但短时出现。 |
| `GiftAnimationUI_GiftAnimation` | `GiftAnimationUI` | 0 / 0 | 无 | 运行时注入 | 礼物动画：中下短时爆发效果，效果图可表现，但不得遮挡核心目标。 |
| `SurvivalTopBarUI_TopBar` | `SurvivalTopBarUI` | 39 / 14 | `IconImg`、`GateIcon`、`Icon`、`HeatIcon`、`Value`、`TimerText` | `100`、`03:00`、`第1天 · 白天`、`起点 0m` | 生存顶栏：主 HUD 顶部核心状态栏，天数、阶段、资源、城门等优先级最高。 |
| `EngagementReminderUI_EngagementReminder` | `EngagementReminderUI` | 2 / 0 | `MessageText`、`PanelRoot` | 运行时注入 | 互动提醒：直播互动弱提示，必须避免长期遮挡主战斗画面。 |
| `HorizontalMarqueeUI_MarqueeZone` | `HorizontalMarqueeUI` | 0 / 0 | 无 | 运行时注入 | 横向跑马灯：主 HUD 顶部或中上信息流，需和 StatusLine/VIP 公告错层。 |
| `SeasonTopBarUI_SeasonTopBar` | `SeasonTopBarUI` | 3 / 3 | `LblSeason`、`LblTheme`、`LblFortressDay` | `S1 · D1/7`、`主题：经典冰原`、`堡垒日 D1（最高 D1）` | 赛季顶栏：主 HUD 顶部次级信息，必须并入 SurvivalTopBar 的顶部状态体系。 |
| `PersonalContribUI_PersonalContribBar` | `PersonalContribUI` | 1 / 0 | `ContribText` | 运行时注入 | 个人贡献条：主 HUD 底部个人反馈，需和礼物贴纸、建筑状态共用底部信息区。 |
| `BuildingStatusPanelUI_BuildingStatusPanel` | `BuildingStatusPanelUI` | 20 / 5 | `Row_Hospital`、`Row_Beacon`、`Label`、`Dot`、`Percent`、`Row_Market` | `烽火台`、`祭坛`、`瞭望塔`、`医院` | 建筑状态：在主 HUD 底部/边缘展示建筑血量与状态，必须服从 SurvivalLiveRankingUI 的中部 gameplay 安全区。 |
| `SupporterJoinedToastUI_SupporterJoinedToast` | `SupporterJoinedToastUI` | 1 / 0 | `Label` | 运行时注入 | 支持者加入 toast：短时入场反馈，不能长期压在榜单或玩法主体上。 |
| `ExpeditionMarkerUI_ExpeditionMarkerPanel` | `ExpeditionMarkerUI` | 4 / 1 | `Countdown`、`Container`、`Icon`、`MarkerTemplate` | `90s` | 远征标记：贴近场景/单位边缘的悬浮标记，布局必须以主 HUD gameplay 区为基准。 |
| `WaitingPhaseUI` | `WaitingPhaseUI` | 4 / 0 | `CountdownLabel`、`Panel`、`TitleLabel`、`ThemeLabel` | 运行时注入 | 等待阶段提示：非 Running 状态提示，主 HUD 已约束 Running 时弱化/隐藏。 |

## 3. 复查结论

本附录确认这些子 prefab 没有漏掉；它们是父面板的组成模块，而不是独立界面。当前保持“父资料覆盖 + 本附录追溯”的方式最稳，既避免重复，又能在调整 Unity prefab 时查到真实节点和脚本。