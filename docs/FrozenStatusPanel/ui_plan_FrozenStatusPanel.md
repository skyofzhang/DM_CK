# FrozenStatusPanel UI 策划案

> Prefab：`Assets/Prefabs/UI/Panels/FrozenStatusPanel.prefab`  
> 用途：全体冻结底部状态横幅  
> 主效果图规格：1080 x 1920 竖屏  
> 日期：2026-05-08

## 1. 界面定位与目的

当全体守护者被冻结时，在屏幕底部展示冰晶横幅、冻结主文案和解冻倒计时，冻结结束后自动淡出。

该界面应被视为 全体冻结底部状态横幅，不是营销页。设计第一屏必须直接表达真实可用状态，并保留足够游戏主体安全区或明确的弹窗遮罩关系。

## 2. 设计目标

- 位置固定在底部安全区上方
- 倒计时每秒可读
- 淡入淡出但不拦截核心视野

## 3. 当前 prefab / 模块核对

| 项目 | 内容 |
| --- | --- |
| Prefab 路径 | `Assets/Prefabs/UI/Panels/FrozenStatusPanel.prefab` |
| 绑定脚本 | Prefab 自身未绑定项目脚本；运行时控制见 `Assets/Scripts/UI/FrozenStatusUI.cs` |
| 节点数量摘录 | 3 个 `m_Name` 记录 |
| 文案数量摘录 | 2 个 TMP/Text 文案记录 |

关键节点摘录：

- BackgroundImage
- CountdownText
- FrozenText

可见/默认文案摘录：

- `解冻倒计时：30s`
- `全体守护者已冻结`

## 3.1 运行时控制关系补充

- Assets/Scripts/UI/FrozenStatusUI.cs：ShowFrozen(duration) 静态入口
- ShowInternal 设置 _panel active、主文字和倒计时协程
- CountdownCoroutine 结束后显示“解冻完成！”并淡出隐藏

## 4. 推荐布局与层级

FrozenStatusPanel prefab 自身没有绑定项目脚本；运行时由 FrozenStatusUI 的 _panel、_frozenText、_countdownText、_backgroundImage 控制。脚本注释明确该面板是屏幕底部蓝色冰晶横幅，而不是中央弹窗。

| 模块 | Prefab/节点 | x | y | w | h | 说明 |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| 游戏画面安全区 | `SceneSafeArea` | 40 | 180 | 1000 | 1180 | 中部保持无遮挡 |
| 底部冻结横幅 | `FrozenStatusPanel/BackgroundImage` | 70 | 1460 | 940 | 180 | 蓝黑半透明全宽条 |
| 冻结主文案 | `FrozenText` | 130 | 1508 | 620 | 58 | 全体守护者已冻结 |
| 倒计时 | `CountdownText` | 130 | 1580 | 620 | 48 | 解冻倒计时：30s |
| 冰晶图标区 | `IceIcon` | 795 | 1505 | 110 | 110 | 雪花/冰晶 |

## 5. 模块设计说明

### 游戏画面安全区

- 对应节点：`SceneSafeArea`
- 建议位置：x=40 y=180 w=1000 h=1180。
- 设计要点：中部保持无遮挡

### 底部冻结横幅

- 对应节点：`FrozenStatusPanel/BackgroundImage`
- 建议位置：x=70 y=1460 w=940 h=180。
- 设计要点：蓝黑半透明全宽条

### 冻结主文案

- 对应节点：`FrozenText`
- 建议位置：x=130 y=1508 w=620 h=58。
- 设计要点：全体守护者已冻结

### 倒计时

- 对应节点：`CountdownText`
- 建议位置：x=130 y=1580 w=620 h=48。
- 设计要点：解冻倒计时：30s

### 冰晶图标区

- 对应节点：`IceIcon`
- 建议位置：x=795 y=1505 w=110 h=110。
- 设计要点：雪花/冰晶

## 6. 视觉风格

蓝黑半透明全宽条，冰晶/雪花语义明显，倒计时高对比；冻结完成时文字变为“解冻完成！”。

建议保持项目统一语言：冰蓝代表常规 HUD，金色代表贡献/VIP/奖励，红橙代表危险或不可逆操作，绿色代表修复/完成。所有文字必须在 1080x1920 竖屏下可读，按钮文字不得溢出。

## 7. AI 效果图场景

战斗画面底部浮出冻结状态条，显示“全体守护者已冻结”和“解冻倒计时：30s”，不遮挡中部战斗主体。

效果图应把施工原型里的坐标和灰盒理解为布局约束，而不是最终视觉元素。最终图可以加入材质、光效、图标、进度条和真实游戏背景，但不要出现 prefab 名、坐标、虚线框、施工标注等文字。

## 8. 实现注意事项

- Assets/Scripts/UI/FrozenStatusUI.cs：ShowFrozen(duration) 静态入口
- ShowInternal 设置 _panel active、主文字和倒计时协程
- CountdownCoroutine 结束后显示“解冻完成！”并淡出隐藏
- 所有临时层应通过 CanvasGroup 或子节点显隐控制，避免长期遮挡中央 gameplay 区。
- 动态文本需要预留 20%-30% 的宽度余量，中文、数字和昵称都要能截断或滚动。
- 若该界面作为其他主面板的子面板复用，应优先服从主面板坐标，不再另起一套视觉规范。

## 9. 验收标准

- 文件均位于 `docs/FrozenStatusPanel/`。
- SVG 和 PNG 都是 1080x1920 竖屏参考。
- 效果图能清楚表达 全体冻结底部状态横幅 的常用状态。
- 默认文案、节点名称和脚本关系能在文档中追溯到 prefab 或运行时控制脚本。