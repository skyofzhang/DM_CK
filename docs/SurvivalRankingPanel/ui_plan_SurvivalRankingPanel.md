# SurvivalRankingPanel UI 策划案

> Prefab：`Assets/Prefabs/UI/Panels/SurvivalRankingPanel.prefab`  
> 用途：生存排行榜双页签面板  
> 主效果图规格：1080 x 1920 竖屏  
> 日期：2026-05-08

## 1. 界面定位与目的

展示贡献排行榜与主播排行榜两类数据，大厅排行榜按钮和结算“查看英雄榜”都可打开。

该界面应被视为 生存排行榜双页签面板，不是营销页。设计第一屏必须直接表达真实可用状态，并保留足够游戏主体安全区或明确的弹窗遮罩关系。

## 2. 设计目标

- 双页签结构必须明确
- 长名单行高一致
- 空榜提示和关闭按钮要有位置

## 3. 当前 prefab / 模块核对

| 项目 | 内容 |
| --- | --- |
| Prefab 路径 | `Assets/Prefabs/UI/Panels/SurvivalRankingPanel.prefab` |
| 绑定脚本 | Prefab 自身未绑定项目脚本；运行时控制见 `Assets/Scripts/UI/SurvivalRankingUI.cs` |
| 节点数量摘录 | 215 个 `m_Name` 记录 |
| 文案数量摘录 | 59 个 TMP/Text 文案记录 |

关键节点摘录：

- PlayerName
- RankNum
- Score
- RankRow_46
- RankRow_23
- RankRow_19
- RankRow_13
- RankRow_49
- Label
- RankRow_22
- RankRow_1
- RankRow_5
- RankRow_18
- RankRow_17
- RankRow_25
- RankRow_43
- RankRow_14
- RankRow_8
- HeaderRow
- RankRow_26
- RankRow_30
- RankRow_9
- TabContribution
- RankRow_24
- RankRow_40
- RankRow_0
- RankRow_2
- SubtitleText

可见/默认文案摘录：

- `'#47'`
- `'#8'`
- `'#3'`
- `'#13'`
- `'#26'`
- `'#38'`
- `'#18'`
- `'#6'`
- `贡献排行榜`
- `'#11'`
- `'#5'`
- `'#46'`

## 3.1 运行时控制关系补充

- Assets/Scripts/UI/SurvivalRankingUI.cs：ShowPanel 打开 overlay 和 panel 并请求两类榜单
- SwitchTab 在 Contribution / Streamer 间切换
- CollectRows 缓存预创建行，RefreshContribution / RefreshStreamer 更新列表

## 4. 推荐布局与层级

SurvivalRankingPanel prefab 自身没有绑定项目脚本；运行时由 SurvivalRankingUI 的 _panel、_overlay、_closeBtn、_titleText、_subtitleText、_tabContribution、_tabStreamer、_headerRow、_rowContainer、_emptyHint 控制。

| 模块 | Prefab/节点 | x | y | w | h | 说明 |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| 暗化背景 | `_overlay` | 0 | 0 | 1080 | 1920 | 排行榜遮罩 |
| 排行榜面板 | `_panel` | 70 | 170 | 940 | 1510 | 双页签榜单 |
| 标题/关闭 | `_titleText/_subtitleText/_closeBtn` | 110 | 215 | 860 | 110 | 生存排行榜 / 关闭 |
| 页签栏 | `_tabContribution/_tabStreamer` | 120 | 355 | 840 | 82 | 贡献榜 / 主播榜 |
| 表头 | `_headerRow` | 120 | 465 | 840 | 62 | 名次 / 玩家或主播 / 数据 |
| 滚动列表 | `_rowContainer` | 120 | 545 | 840 | 880 | 统一行高排行 |
| 空榜提示 | `_emptyHint` | 210 | 1450 | 660 | 74 | 暂无数据时显示 |

## 5. 模块设计说明

### 暗化背景

- 对应节点：`_overlay`
- 建议位置：x=0 y=0 w=1080 h=1920。
- 设计要点：排行榜遮罩

### 排行榜面板

- 对应节点：`_panel`
- 建议位置：x=70 y=170 w=940 h=1510。
- 设计要点：双页签榜单

### 标题/关闭

- 对应节点：`_titleText/_subtitleText/_closeBtn`
- 建议位置：x=110 y=215 w=860 h=110。
- 设计要点：生存排行榜 / 关闭

### 页签栏

- 对应节点：`_tabContribution/_tabStreamer`
- 建议位置：x=120 y=355 w=840 h=82。
- 设计要点：贡献榜 / 主播榜

### 表头

- 对应节点：`_headerRow`
- 建议位置：x=120 y=465 w=840 h=62。
- 设计要点：名次 / 玩家或主播 / 数据

### 滚动列表

- 对应节点：`_rowContainer`
- 建议位置：x=120 y=545 w=840 h=880。
- 设计要点：统一行高排行

### 空榜提示

- 对应节点：`_emptyHint`
- 建议位置：x=210 y=1450 w=660 h=74。
- 设计要点：暂无数据时显示

## 6. 视觉风格

冰蓝竞技榜，页签清楚区分贡献榜/主播榜；前三名可用金银铜奖牌，但仍作为列表行的一部分，而不是独立大卡。

建议保持项目统一语言：冰蓝代表常规 HUD，金色代表贡献/VIP/奖励，红橙代表危险或不可逆操作，绿色代表修复/完成。所有文字必须在 1080x1920 竖屏下可读，按钮文字不得溢出。

## 7. AI 效果图场景

全屏暗化后展示排行榜面板，顶部标题和副标题，下面是贡献榜/主播榜页签、表头和统一行高滚动列表。

效果图应把施工原型里的坐标和灰盒理解为布局约束，而不是最终视觉元素。最终图可以加入材质、光效、图标、进度条和真实游戏背景，但不要出现 prefab 名、坐标、虚线框、施工标注等文字。

## 8. 实现注意事项

- Assets/Scripts/UI/SurvivalRankingUI.cs：ShowPanel 打开 overlay 和 panel 并请求两类榜单
- SwitchTab 在 Contribution / Streamer 间切换
- CollectRows 缓存预创建行，RefreshContribution / RefreshStreamer 更新列表
- 所有临时层应通过 CanvasGroup 或子节点显隐控制，避免长期遮挡中央 gameplay 区。
- 动态文本需要预留 20%-30% 的宽度余量，中文、数字和昵称都要能截断或滚动。
- 若该界面作为其他主面板的子面板复用，应优先服从主面板坐标，不再另起一套视觉规范。

## 9. 验收标准

- 文件均位于 `docs/SurvivalRankingPanel/`。
- SVG 和 PNG 都是 1080x1920 竖屏参考。
- 效果图能清楚表达 生存排行榜双页签面板 的常用状态。
- 默认文案、节点名称和脚本关系能在文档中追溯到 prefab 或运行时控制脚本。