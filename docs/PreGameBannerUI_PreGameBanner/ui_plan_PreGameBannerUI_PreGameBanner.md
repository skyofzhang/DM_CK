# PreGameBannerUI_PreGameBanner UI 策划案

> Prefab：`Assets/Prefabs/UI/Panels/PreGameBannerUI_PreGameBanner.prefab`  
> 用途：开局等待横幅/准备面板  
> 主效果图规格：1080 x 1920 竖屏  
> 日期：2026-05-07

## 1. 界面定位与目的

等待玩家加入并让主播开始挑战。

该界面应被视为 开局等待横幅/准备面板，不是营销页。设计第一屏必须直接表达真实可用状态，并保留足够游戏主体安全区或明确的弹窗遮罩关系。

## 2. 设计目标

- 玩家数量醒目
- 主播开始按钮明显
- 状态文案不超过两行

## 3. 当前 prefab / 模块核对

| 项目 | 内容 |
| --- | --- |
| Prefab 路径 | `Assets/Prefabs/UI/Panels/PreGameBannerUI_PreGameBanner.prefab` |
| 绑定脚本 | `PreGameBannerUI` |
| 节点数量摘录 | 7 个 `m_Name` 记录 |
| 文案数量摘录 | 4 个 TMP/Text 文案记录 |
| 嵌套子 prefab | 无直接嵌套 Panels prefab |

关键节点摘录：

- TitleText
- PlayerCountText
- StatusText
- StartChallengeBtn
- Label
- BgPanel
- CenterBox

可见/默认文案摘录：

- `冬日生存法则`
- `0`
- `等待玩家加入，随时可开始挑战...`
- `开始挑战`



## 4. 推荐布局与层级

BgPanel/CenterBox 中包含 TitleText、PlayerCountText、StatusText、StartChallengeBtn。

| 模块 | Prefab/节点 | x | y | w | h | 说明 |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| 背景可见区 | `ScenePreview` | 0 | 0 | 1080 | 1920 | 大厅或场景底 |
| 中央准备盒 | `CenterBox` | 130 | 520 | 820 | 720 | 开局等待 |
| 标题 | `TitleText` | 190 | 590 | 700 | 84 | 冬日生存法则 |
| 人数 | `PlayerCountText` | 390 | 725 | 300 | 110 | 0 |
| 状态 | `StatusText` | 200 | 860 | 680 | 80 | 等待玩家加入... |
| 开始按钮 | `StartChallengeBtn` | 270 | 1020 | 540 | 104 | 开始挑战 |

## 5. 模块设计说明

### 背景可见区

- 对应节点：`ScenePreview`
- 建议位置：x=0 y=0 w=1080 h=1920。
- 设计要点：大厅或场景底

### 中央准备盒

- 对应节点：`CenterBox`
- 建议位置：x=130 y=520 w=820 h=720。
- 设计要点：开局等待

### 标题

- 对应节点：`TitleText`
- 建议位置：x=190 y=590 w=700 h=84。
- 设计要点：冬日生存法则

### 人数

- 对应节点：`PlayerCountText`
- 建议位置：x=390 y=725 w=300 h=110。
- 设计要点：0

### 状态

- 对应节点：`StatusText`
- 建议位置：x=200 y=860 w=680 h=80。
- 设计要点：等待玩家加入...

### 开始按钮

- 对应节点：`StartChallengeBtn`
- 建议位置：x=270 y=1020 w=540 h=104。
- 设计要点：开始挑战

## 6. 视觉风格

极地开局仪式感，中心卡片安静清晰，开始按钮暖金。

建议保持项目统一语言：冰蓝代表常规 HUD，金色代表贡献/VIP/奖励，红橙代表危险或不可逆操作，绿色代表修复/完成。所有文字必须在 1080x1920 竖屏下可读，按钮文字不得溢出。

## 7. AI 效果图场景

正式开始前，中央准备卡显示玩家数量与开始挑战按钮。

效果图应把施工原型里的坐标和灰盒理解为布局约束，而不是最终视觉元素。最终图可以加入材质、光效、图标、进度条和真实游戏背景，但不要出现 prefab 名、坐标、虚线框、施工标注等文字。

## 8. 实现注意事项

- 当前 prefab 没有额外特殊实现项；按以下通用规则落地。
- 所有临时层应通过 CanvasGroup 或子节点显隐控制，避免长期遮挡中央 gameplay 区。
- 动态文本需要预留 20%-30% 的宽度余量，中文、数字和昵称都要能截断或滚动。
- 若该界面作为其他主面板的子面板复用，应优先服从主面板坐标，不再另起一套视觉规范。

## 9. 验收标准

- 文件均位于 `docs/PreGameBannerUI_PreGameBanner/`。
- SVG 和 PNG 都是 1080x1920 竖屏参考。
- 效果图能清楚表达 开局等待横幅/准备面板 的常用状态。
- 默认文案、节点名称和脚本关系能在文档中追溯到 prefab。
- 若存在嵌套子 prefab，README 和批量索引中明确避免重复生成。