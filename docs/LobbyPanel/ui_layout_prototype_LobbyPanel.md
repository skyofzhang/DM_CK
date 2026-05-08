# LobbyPanel 施工原型说明

> 对应 prefab：`Assets/Prefabs/UI/Panels/LobbyPanel.prefab`  
> 画布：1080 x 1920 竖屏  
> 原型文件：`ui_layout_prototype_LobbyPanel.svg`

## 1. 施工顺序

1. 先建立 1080x1920 根画布，确认 Safe Area 与全屏遮罩关系。
2. 按坐标表搭建主模块边界，保证每个节点的锚点和尺寸稳定。
3. 再填充标题、正文、列表、按钮、倒计时、进度条等动态内容。
4. 最后补临时动效层，短时反馈不应长期遮挡中央 gameplay 区。

## 2. 坐标表

| 模块 | Prefab/节点 | x | y | w | h | 说明 |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| 背景 | `LobbyBg` | 0 | 0 | 1080 | 1920 | 极地场景底 |
| 标题区 | `TitleImage/TitleText` | 120 | 300 | 840 | 180 | 极地生存法则 |
| 状态区 | `ServerStatus/StatusText` | 180 | 620 | 720 | 120 | 已连接 / 等待主播 |
| 开始按钮 | `StartBtn` | 240 | 860 | 600 | 110 | 开始游戏 |
| 次级按钮 | `RankingBtn/SettingsBtn` | 240 | 1005 | 600 | 96 | 排行榜 / 设置 |
| 底部留白 | `SafeFooter` | 80 | 1460 | 920 | 240 | 适配全面屏 |

## 3. 关键实现规则

- 原型中的虚线框、节点名、坐标和“施工标注”仅用于 Unity 搭建，不进入最终美术图。
- 所有模块按 1080x1920 竖屏设计；如运行时 CanvasScaler 等比适配，优先保持顶部/底部/边缘锚点。
- 文字容器需要设置溢出策略：短按钮不换行，长昵称/状态文案使用省略或滚动。
- 弹窗类界面必须暗化背景；HUD/横幅类界面必须保留游戏主体安全区。

## 4. 推荐可见文案

- `已连接 √`
- `排行榜`
- `开始游戏`
- `等待主播开始游戏...`
- `设置`
- `冬日生存法则`

## 5. Prefab 拆分与嵌套建议

LobbyPanel prefab 自身没有绑定项目脚本；运行时由 Canvas 上的 SurvivalIdleUI 通过 _panel、StartBtn、RankingBtn、SettingsBtn、StatusText、ServerStatus、TitleText 控制。Waiting 状态不再显示大厅，由 PreGameBannerUI 接管。

## 6. PNG 校验

本轮会生成 `ui_layout_prototype_LobbyPanel.png`，目标尺寸为 1080x1920。