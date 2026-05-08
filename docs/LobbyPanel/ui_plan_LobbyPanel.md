# LobbyPanel UI 策划案

> Prefab：`Assets/Prefabs/UI/Panels/LobbyPanel.prefab`  
> 用途：大厅主面板  
> 主效果图规格：1080 x 1920 竖屏  
> 日期：2026-05-08

## 1. 界面定位与目的

连接成功且 SurvivalGameManager 处于 Idle 时展示首次入口；提供开始游戏、排行榜、设置入口。

该界面应被视为 大厅主面板，不是营销页。设计第一屏必须直接表达真实可用状态，并保留足够游戏主体安全区或明确的弹窗遮罩关系。

## 2. 设计目标

- 只在 Idle 显示，避免和 Waiting/Running HUD 重叠
- 开始按钮最醒目
- 连接状态和等待主播文案清楚

## 3. 当前 prefab / 模块核对

| 项目 | 内容 |
| --- | --- |
| Prefab 路径 | `Assets/Prefabs/UI/Panels/LobbyPanel.prefab` |
| 绑定脚本 | Prefab 自身未绑定项目脚本；运行时控制见 `Assets/Scripts/UI/SurvivalIdleUI.cs` |
| 节点数量摘录 | 10 个 `m_Name` 记录 |
| 文案数量摘录 | 6 个 TMP/Text 文案记录 |

关键节点摘录：

- ServerStatus
- Text
- StatusText
- RankingBtn
- SettingsBtn
- TitleText
- StartBtn
- TitleImage

可见/默认文案摘录：

- `已连接 √`
- `排行榜`
- `开始游戏`
- `等待主播开始游戏...`
- `设置`
- `冬日生存法则`

## 3.1 运行时控制关系补充

- Assets/Scripts/UI/SurvivalIdleUI.cs：RefreshVisibility 只在 connected + Idle 时 ShowPanel
- OnStartClicked 调 SurvivalGameManager.RequestStartGame()
- OnRankingClicked / OnSettingsClicked 分别 Toggle SurvivalRankingUI、SurvivalSettingsUI

## 4. 推荐布局与层级

LobbyPanel prefab 自身没有绑定项目脚本；运行时由 Canvas 上的 SurvivalIdleUI 通过 _panel、StartBtn、RankingBtn、SettingsBtn、StatusText、ServerStatus、TitleText 控制。Waiting 状态不再显示大厅，由 PreGameBannerUI 接管。

| 模块 | Prefab/节点 | x | y | w | h | 说明 |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| 背景 | `LobbyBg` | 0 | 0 | 1080 | 1920 | 极地场景底 |
| 标题区 | `TitleImage/TitleText` | 120 | 300 | 840 | 180 | 极地生存法则 |
| 状态区 | `ServerStatus/StatusText` | 180 | 620 | 720 | 120 | 已连接 / 等待主播 |
| 开始按钮 | `StartBtn` | 240 | 860 | 600 | 110 | 开始游戏 |
| 次级按钮 | `RankingBtn/SettingsBtn` | 240 | 1005 | 600 | 96 | 排行榜 / 设置 |
| 底部留白 | `SafeFooter` | 80 | 1460 | 920 | 240 | 适配全面屏 |

## 5. 模块设计说明

### 背景

- 对应节点：`LobbyBg`
- 建议位置：x=0 y=0 w=1080 h=1920。
- 设计要点：极地场景底

### 标题区

- 对应节点：`TitleImage/TitleText`
- 建议位置：x=120 y=300 w=840 h=180。
- 设计要点：极地生存法则

### 状态区

- 对应节点：`ServerStatus/StatusText`
- 建议位置：x=180 y=620 w=720 h=120。
- 设计要点：已连接 / 等待主播

### 开始按钮

- 对应节点：`StartBtn`
- 建议位置：x=240 y=860 w=600 h=110。
- 设计要点：开始游戏

### 次级按钮

- 对应节点：`RankingBtn/SettingsBtn`
- 建议位置：x=240 y=1005 w=600 h=96。
- 设计要点：排行榜 / 设置

### 底部留白

- 对应节点：`SafeFooter`
- 建议位置：x=80 y=1460 w=920 h=240。
- 设计要点：适配全面屏

## 6. 视觉风格

极地生存主菜单，标题清楚但不做营销页；开始游戏是第一操作，排行榜/设置保持次级。

建议保持项目统一语言：冰蓝代表常规 HUD，金色代表贡献/VIP/奖励，红橙代表危险或不可逆操作，绿色代表修复/完成。所有文字必须在 1080x1920 竖屏下可读，按钮文字不得溢出。

## 7. AI 效果图场景

连接成功后、Idle 状态下显示大厅；点击开始后状态文案变为“进入战场...”，随后请求 start_game；排行榜和设置打开各自面板。

效果图应把施工原型里的坐标和灰盒理解为布局约束，而不是最终视觉元素。最终图可以加入材质、光效、图标、进度条和真实游戏背景，但不要出现 prefab 名、坐标、虚线框、施工标注等文字。

## 8. 实现注意事项

- Assets/Scripts/UI/SurvivalIdleUI.cs：RefreshVisibility 只在 connected + Idle 时 ShowPanel
- OnStartClicked 调 SurvivalGameManager.RequestStartGame()
- OnRankingClicked / OnSettingsClicked 分别 Toggle SurvivalRankingUI、SurvivalSettingsUI
- 所有临时层应通过 CanvasGroup 或子节点显隐控制，避免长期遮挡中央 gameplay 区。
- 动态文本需要预留 20%-30% 的宽度余量，中文、数字和昵称都要能截断或滚动。
- 若该界面作为其他主面板的子面板复用，应优先服从主面板坐标，不再另起一套视觉规范。

## 9. 验收标准

- 文件均位于 `docs/LobbyPanel/`。
- SVG 和 PNG 都是 1080x1920 竖屏参考。
- 效果图能清楚表达 大厅主面板 的常用状态。
- 默认文案、节点名称和脚本关系能在文档中追溯到 prefab 或运行时控制脚本。