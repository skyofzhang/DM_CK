# SurvivalSettlementUI_SurvivalSettlementPanel UI 策划案

> Prefab：`Assets/Prefabs/UI/Panels/SurvivalSettlementUI_SurvivalSettlementPanel.prefab`  
> 用途：生存结算全屏面板  
> 主效果图规格：1080 x 1920 竖屏  
> 日期：2026-05-07

## 1. 界面定位与目的

游戏结束后按高光、统计、MVP/Top3 展示结算流程，并给主播重开/跳过能力。

该界面应被视为 生存结算全屏面板，不是营销页。设计第一屏必须直接表达真实可用状态，并保留足够游戏主体安全区或明确的弹窗遮罩关系。

## 2. 设计目标

- 三屏结构清楚
- 按钮只在需要时出现
- Top3 和 MVP 视觉最强

## 3. 当前 prefab / 模块核对

| 项目 | 内容 |
| --- | --- |
| Prefab 路径 | `Assets/Prefabs/UI/Panels/SurvivalSettlementUI_SurvivalSettlementPanel.prefab` |
| 绑定脚本 | `SurvivalSettlementUI` |
| 节点数量摘录 | 96 个 `m_Name` 记录 |
| 文案数量摘录 | 57 个 TMP/Text 文案记录 |
| 嵌套子 prefab | 无直接嵌套 Panels prefab |

关键节点摘录：

- ScoreText
- RankEntry_5
- RankMark
- SurvivalDaysText
- RankText
- HighlightClosestCall
- StatsPanelBg
- NameText
- MvpNameText
- Dot2
- RankEntry_1
- RankEntry_8
- TitleDivider
- TotalGatherText
- HighlightDramatic
- SlotBg
- Top3Slot_2
- TotalRepairText
- Top3PanelBg
- DynamicTaglineText
- MvpAnchorLine
- RestartButton
- HighlightBestRescue
- HighlightTopDamage
- Dot3
- TotalKillsText
- Top3Slot_0
- MvpLabel

可见/默认文案摘录：

- `0`
- `3`
- `生存天数: 0`
- `'#2'`
- `1`
- `—`
- `2`
- `'#9'`
- `MVP`
- `总采集: 0`
- `总修墙: 0`
- `'#1'`



## 4. 推荐布局与层级

ScreenA 高光、ScreenB 统计排行榜、ScreenC MVP；PageDots/按钮管理流程。

| 模块 | Prefab/节点 | x | y | w | h | 说明 |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| 结算背景 | `SettlementBackdrop` | 0 | 0 | 1080 | 1920 | 全屏结算 |
| 结果标题 | `ResultTitle` | 90 | 130 | 900 | 120 | 极地陷落/胜利 |
| ScreenA 高光 | `ScreenA` | 100 | 300 | 880 | 360 | 伤害最高/最佳救援 |
| ScreenB 统计 | `ScreenB` | 100 | 700 | 880 | 450 | 天数/击杀/采集/修复/排行 |
| ScreenC MVP | `ScreenC` | 100 | 1190 | 880 | 300 | MVP/Top3 |
| 操作与页点 | `Buttons/PageDots` | 190 | 1550 | 700 | 120 | 重开/查看英雄榜/跳过 |

## 5. 模块设计说明

### 结算背景

- 对应节点：`SettlementBackdrop`
- 建议位置：x=0 y=0 w=1080 h=1920。
- 设计要点：全屏结算

### 结果标题

- 对应节点：`ResultTitle`
- 建议位置：x=90 y=130 w=900 h=120。
- 设计要点：极地陷落/胜利

### ScreenA 高光

- 对应节点：`ScreenA`
- 建议位置：x=100 y=300 w=880 h=360。
- 设计要点：伤害最高/最佳救援

### ScreenB 统计

- 对应节点：`ScreenB`
- 建议位置：x=100 y=700 w=880 h=450。
- 设计要点：天数/击杀/采集/修复/排行

### ScreenC MVP

- 对应节点：`ScreenC`
- 建议位置：x=100 y=1190 w=880 h=300。
- 设计要点：MVP/Top3

### 操作与页点

- 对应节点：`Buttons/PageDots`
- 建议位置：x=190 y=1550 w=700 h=120。
- 设计要点：重开/查看英雄榜/跳过

## 6. 视觉风格

胜负结算大屏，冰蓝背景配金色 MVP，高光卡片按内容分组。

建议保持项目统一语言：冰蓝代表常规 HUD，金色代表贡献/VIP/奖励，红橙代表危险或不可逆操作，绿色代表修复/完成。所有文字必须在 1080x1920 竖屏下可读，按钮文字不得溢出。

## 7. AI 效果图场景

结算按三屏轮播：高光时刻、总统计与排行榜、MVP 表彰。

效果图应把施工原型里的坐标和灰盒理解为布局约束，而不是最终视觉元素。最终图可以加入材质、光效、图标、进度条和真实游戏背景，但不要出现 prefab 名、坐标、虚线框、施工标注等文字。

## 8. 实现注意事项

- 当前 prefab 没有额外特殊实现项；按以下通用规则落地。
- 所有临时层应通过 CanvasGroup 或子节点显隐控制，避免长期遮挡中央 gameplay 区。
- 动态文本需要预留 20%-30% 的宽度余量，中文、数字和昵称都要能截断或滚动。
- 若该界面作为其他主面板的子面板复用，应优先服从主面板坐标，不再另起一套视觉规范。

## 9. 验收标准

- 文件均位于 `docs/SurvivalSettlementUI_SurvivalSettlementPanel/`。
- SVG 和 PNG 都是 1080x1920 竖屏参考。
- 效果图能清楚表达 生存结算全屏面板 的常用状态。
- 默认文案、节点名称和脚本关系能在文档中追溯到 prefab。
- 若存在嵌套子 prefab，README 和批量索引中明确避免重复生成。