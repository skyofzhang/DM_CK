# ConnectPanel 施工原型说明

> 对应 prefab：`Assets/Prefabs/UI/Panels/ConnectPanel.prefab`  
> 画布：1080 x 1920 竖屏  
> 原型文件：`ui_layout_prototype_ConnectPanel.svg`

## 1. 施工顺序

1. 先建立 1080x1920 根画布，确认 Safe Area 与全屏遮罩关系。
2. 按坐标表搭建主模块边界，保证每个节点的锚点和尺寸稳定。
3. 再填充标题、正文、列表、按钮、倒计时、进度条等动态内容。
4. 最后补临时动效层，短时反馈不应长期遮挡中央 gameplay 区。

## 2. 坐标表

| 模块 | Prefab/节点 | x | y | w | h | 说明 |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| 背景 | `FullScreenBg` | 0 | 0 | 1080 | 1920 | 极地深色 |
| 标题 | `TitleText` | 120 | 420 | 840 | 100 | 冬日生存法则 |
| 连接动效 | `Spinner/DotText` | 440 | 650 | 200 | 200 | 旋转/点点 |
| 状态文案 | `StatusText` | 190 | 900 | 700 | 76 | 正在连接服务器 / 已连接 |
| 重试按钮 | `RetryButton` | 340 | 1045 | 400 | 96 | 失败时显示 |

## 3. 关键实现规则

- 原型中的虚线框、节点名、坐标和“施工标注”仅用于 Unity 搭建，不进入最终美术图。
- 所有模块按 1080x1920 竖屏设计；如运行时 CanvasScaler 等比适配，优先保持顶部/底部/边缘锚点。
- 文字容器需要设置溢出策略：短按钮不换行，长昵称/状态文案使用省略或滚动。
- 弹窗类界面必须暗化背景；HUD/横幅类界面必须保留游戏主体安全区。

## 4. 推荐可见文案

- `重 试`
- `...`
- `正在连接服务器`
- `冬日生存法则`

## 5. Prefab 拆分与嵌套建议

ConnectPanel prefab 自身没有绑定项目脚本；运行时由 Canvas 上的 SurvivalConnectUI 通过 _panel、_statusText、_dotText、_spinner、_retryBtn 控制。TitleText 是静态标题，StatusText/DotText/Spinner 是连接中状态，RetryButton 仅失败或断线时显示。

## 6. PNG 校验

本轮会生成 `ui_layout_prototype_ConnectPanel.png`，目标尺寸为 1080x1920。