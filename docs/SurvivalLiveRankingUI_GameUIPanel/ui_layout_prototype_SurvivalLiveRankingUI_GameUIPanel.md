# SurvivalLiveRankingUI_GameUIPanel 施工布局原型说明

> 原型图：`ui_layout_prototype_SurvivalLiveRankingUI_GameUIPanel.svg`  
> 对应 prefab：`Assets/Prefabs/UI/Panels/SurvivalLiveRankingUI_GameUIPanel.prefab`  
> 基准画布：1080 x 1920 竖屏  
> 目标：做成可直接照着摆 prefab 的灰盒施工稿。

## 1. 施工顺序

1. 先搭四个常驻信息层：`TopBar`、`LiveRankingPanel`、`ResourceRankPanel`、`BarragePanel`。
2. 竖屏没有足够横向空间，不建议做左右大侧栏；`ResourceRankPanel` 和 `LiveRankingPanel` 分别改成游戏画面左上、右上悬浮窄榜，把原先榜单占用的纵向空间还给游戏主体。
3. 再搭底部辅助层：`BuildingStatusPanel`、`PersonalContribBar`、`GiftStickerPanel`。这里的礼物区不是单纯按钮入口，要能展示抖音要求的礼物贴纸说明。
4. 最后搭临时反馈层：`GiftAnimation`、`EventTriggeredToast`、`VIPAnnouncement`、`BuildVotePanel`、`TraderCaravanPanel`。

## 2. 建议坐标表

| 模块 | Prefab/节点 | x | y | w | h | 说明 |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| 返回按钮 | `ExitBtn` | 30 | 56 | 118 | 70 | 常驻或调试显示 |
| 设置按钮 | `BtnSettings` | 932 | 56 | 118 | 70 | 常驻 |
| 顶部主状态栏 | `SurvivalTopBarUI_TopBar` | 164 | 56 | 752 | 100 | 常驻，最高优先级 |
| 主跑马灯 | `HorizontalMarqueeUI_MarqueeZone` / `SupporterMarquee` | 40 | 176 | 1000 | 64 | 仅一条主播报 |
| 游戏主体安全区 | 场景画面预留 | 40 | 260 | 1000 | 1040 | 长期 UI 不进入；主体构图避开左右悬浮榜 |
| 事件提示 | `EventTriggeredUI_EventTriggeredToast` | 242 | 288 | 596 | 76 | 短时弹层，避开左右悬浮榜 |
| 资源贡献榜 | `ResourceRankPanel` | 56 | 288 | 174 | 350 | Running 状态显示，悬浮在用户左侧绿框位置 |
| 守护者榜 | `LiveRankingPanel` | 850 | 288 | 174 | 350 | Running 状态显示 Top5，悬浮在用户右侧绿框位置 |
| 礼物动画 | `GiftAnimationUI_GiftAnimation` | 260 | 1184 | 560 | 78 | 0.8-1.5 秒短时出现，避开左右悬浮榜 |
| 弹幕动态 | `BarragePanel` | 40 | 1324 | 1000 | 132 | 常驻或 Running 显示 |
| 建筑状态 | `BuildingStatusPanelUI_BuildingStatusPanel` | 40 | 1480 | 312 | 124 | 有建筑数据时显示 |
| 个人贡献 | `PersonalContribUI_PersonalContribBar` | 40 | 1624 | 312 | 92 | 有玩家/支持者数据时显示 |
| 礼物贴纸说明 | `StickerPanelUI` / `GiftRecommendationUI_GiftIconBar` / `GiftInfoPanel` | 384 | 1480 | 656 | 236 | 默认常驻，显示礼物图标、名称、触发效果 |
| VIP/加入提示 | `VIPAnnouncementUI_VIPAnnouncement` / `SupporterJoinedToastUI` | 40 | 1736 | 486 | 92 | 短时弹层 |
| 建造投票 | `BuildVoteUI_BuildVotePanel` | 554 | 1736 | 486 | 92 | 投票事件中显示 |

## 3. 关键施工规则

- `LiveRankingPanel` 按用户绿框位置悬浮在游戏主体右上角，使用窄榜样式，不再占用主画面下方整块空间。
- `LiveRankingPanel` 保持 5 个固定 `RankRow`，但视觉上压缩成紧凑两段式：`#1 ★云逸` + 缩写贡献值。
- `ResourceRankPanel` 按用户左侧绿框位置悬浮在游戏主体左上角，使用窄榜样式，视觉上改成食物、煤炭、矿石三组上下堆叠。
- `ResourceRankPanel` 仍保留三类数据绑定：食物、煤炭、矿石，每类 3 行 TMP；只是施工排布从横向三列改成竖向三组。
- 场景主体构图要避开左右约 200px 宽的悬浮榜覆盖区，怪物、核心建筑、主角和关键特效不要长期放在这些区域。
- 玩家名按脚本最多 6 字展示，施工时不要为超长名字留过宽空间。
- `BarragePanel` 做成底部信息流，不要做成聊天大面板。
- `礼物贴纸说明` 要按“贴纸说明”施工，不按小按钮施工：至少展示礼物图标、礼物名、消耗/档位、游戏内效果说明。推荐默认展开，满足抖音对直播礼物贴纸的展示要求。
- `GiftIconBar` 仍可保留为可点击入口，但视觉上并入 `StickerPanelUI` / `GiftInfoPanel` 大面板；礼物描述优先复用 `GiftAnimationUI` 里的礼物效果文案。
- `GiftAnimation`、`EventTriggeredToast`、`VIPAnnouncement`、`BuildVotePanel` 都是临时层，默认不应长期遮挡中央安全区。
- 顶部主状态栏、守护者榜、底部弹幕是这个 prefab 的三条主信息线，优先保证清晰。

## 4. 文案占位建议

| 区域 | 建议文案 |
| --- | --- |
| 守护者榜标题 | `守护者榜` |
| 守护者榜行 | `#1  ★ 云逸  18.6万` |
| 资源榜标题 | `食物贡献` / `煤炭贡献` / `矿石贡献` |
| 弹幕提示 | `发送 煤炭 提升炉火` / `发送 修门 守住城门` |
| 事件提示 | `暴风雪来袭` / `商队抵达` / `Boss 出现` |
| 礼物贴纸说明 | `仙女棒：效率+5%` / `能力药丸：全员效率+50%` / `甜甜圈：城门+200 食物+100` / `能量电池：炉温+30℃ 效率+30%` / `神秘空投：超级补给` |
| 投票按钮 | `修门` / `火炉` / `箭塔` / `哨站` |

## 5. 后续 prefab 拆分建议

这张图可以直接指导当前大 prefab 的重排。等布局定下来后，建议再把下面几个区域单独沉淀成可编辑子 prefab：

- `LiveRankingPanel.prefab`
- `ResourceRankPanel.prefab`
- `BarragePanel.prefab`
- `BattleTopStatusBar.prefab`
- `TemporaryEventToastLayer.prefab`

这样后面你调排行榜、资源榜、弹幕、顶部状态栏，就不用在一个大 `GameUIPanel` 里反复翻层级。
