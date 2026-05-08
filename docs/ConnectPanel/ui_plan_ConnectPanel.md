# ConnectPanel UI 策划案

> Prefab：`Assets/Prefabs/UI/Panels/ConnectPanel.prefab`  
> 用途：连接状态全屏页  
> 主效果图规格：1080 x 1920 竖屏  
> 日期：2026-05-08

## 1. 界面定位与目的

展示连接服务器、已连接延迟过渡、断线/失败重试状态，是进入直播玩法前的技术状态界面。

该界面应被视为 连接状态全屏页，不是营销页。设计第一屏必须直接表达真实可用状态，并保留足够游戏主体安全区或明确的弹窗遮罩关系。

## 2. 设计目标

- 连接中、成功、失败三种状态要可区分
- 重试按钮足够大且不误触
- 状态文案与点点动画不要挤压标题

## 3. 当前 prefab / 模块核对

| 项目 | 内容 |
| --- | --- |
| Prefab 路径 | `Assets/Prefabs/UI/Panels/ConnectPanel.prefab` |
| 绑定脚本 | Prefab 自身未绑定项目脚本；运行时控制见 `Assets/Scripts/UI/SurvivalConnectUI.cs` |
| 节点数量摘录 | 6 个 `m_Name` 记录 |
| 文案数量摘录 | 4 个 TMP/Text 文案记录 |

关键节点摘录：

- Spinner
- RetryButton
- Text
- DotText
- StatusText
- TitleText

可见/默认文案摘录：

- `重 试`
- `...`
- `正在连接服务器`
- `冬日生存法则`

## 3.1 运行时控制关系补充

- Assets/Scripts/UI/SurvivalConnectUI.cs：Start 显示连接面板并调用 DoConnect()
- OnConnected 显示“已连接！正在加载游戏状态...”并 1.5 秒后隐藏
- OnDisconnected / OnConnectFailed 显示失败原因并打开 RetryButton

## 4. 推荐布局与层级

ConnectPanel prefab 自身没有绑定项目脚本；运行时由 Canvas 上的 SurvivalConnectUI 通过 _panel、_statusText、_dotText、_spinner、_retryBtn 控制。TitleText 是静态标题，StatusText/DotText/Spinner 是连接中状态，RetryButton 仅失败或断线时显示。

| 模块 | Prefab/节点 | x | y | w | h | 说明 |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| 背景 | `FullScreenBg` | 0 | 0 | 1080 | 1920 | 极地深色 |
| 标题 | `TitleText` | 120 | 420 | 840 | 100 | 冬日生存法则 |
| 连接动效 | `Spinner/DotText` | 440 | 650 | 200 | 200 | 旋转/点点 |
| 状态文案 | `StatusText` | 190 | 900 | 700 | 76 | 正在连接服务器 / 已连接 |
| 重试按钮 | `RetryButton` | 340 | 1045 | 400 | 96 | 失败时显示 |

## 5. 模块设计说明

### 背景

- 对应节点：`FullScreenBg`
- 建议位置：x=0 y=0 w=1080 h=1920。
- 设计要点：极地深色

### 标题

- 对应节点：`TitleText`
- 建议位置：x=120 y=420 w=840 h=100。
- 设计要点：冬日生存法则

### 连接动效

- 对应节点：`Spinner/DotText`
- 建议位置：x=440 y=650 w=200 h=200。
- 设计要点：旋转/点点

### 状态文案

- 对应节点：`StatusText`
- 建议位置：x=190 y=900 w=700 h=76。
- 设计要点：正在连接服务器 / 已连接

### 重试按钮

- 对应节点：`RetryButton`
- 建议位置：x=340 y=1045 w=400 h=96。
- 设计要点：失败时显示

## 6. 视觉风格

冷静的极地等待屏，深色场景或磨砂底，连接动效用浅蓝环形/点状，失败态重试按钮可转为琥珀色。

建议保持项目统一语言：冰蓝代表常规 HUD，金色代表贡献/VIP/奖励，红橙代表危险或不可逆操作，绿色代表修复/完成。所有文字必须在 1080x1920 竖屏下可读，按钮文字不得溢出。

## 7. AI 效果图场景

玩家进入游戏后看到“冬日生存法则”，下方显示正在连接；连接成功时显示“已连接！正在加载游戏状态...”，1.5 秒后隐藏；失败时显示重试按钮。

效果图应把施工原型里的坐标和灰盒理解为布局约束，而不是最终视觉元素。最终图可以加入材质、光效、图标、进度条和真实游戏背景，但不要出现 prefab 名、坐标、虚线框、施工标注等文字。

## 8. 实现注意事项

- Assets/Scripts/UI/SurvivalConnectUI.cs：Start 显示连接面板并调用 DoConnect()
- OnConnected 显示“已连接！正在加载游戏状态...”并 1.5 秒后隐藏
- OnDisconnected / OnConnectFailed 显示失败原因并打开 RetryButton
- 所有临时层应通过 CanvasGroup 或子节点显隐控制，避免长期遮挡中央 gameplay 区。
- 动态文本需要预留 20%-30% 的宽度余量，中文、数字和昵称都要能截断或滚动。
- 若该界面作为其他主面板的子面板复用，应优先服从主面板坐标，不再另起一套视觉规范。

## 9. 验收标准

- 文件均位于 `docs/ConnectPanel/`。
- SVG 和 PNG 都是 1080x1920 竖屏参考。
- 效果图能清楚表达 连接状态全屏页 的常用状态。
- 默认文案、节点名称和脚本关系能在文档中追溯到 prefab 或运行时控制脚本。