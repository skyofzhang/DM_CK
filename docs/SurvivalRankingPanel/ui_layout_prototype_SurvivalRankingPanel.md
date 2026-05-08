# SurvivalRankingPanel 施工原型说明

> 对应 prefab：`Assets/Prefabs/UI/Panels/SurvivalRankingPanel.prefab`  
> 画布：1080 x 1920 竖屏  
> 原型文件：`ui_layout_prototype_SurvivalRankingPanel.svg`

## 1. 施工顺序

1. 先建立 1080x1920 根画布，确认 Safe Area 与全屏遮罩关系。
2. 按坐标表搭建主模块边界，保证每个节点的锚点和尺寸稳定。
3. 再填充标题、正文、列表、按钮、倒计时、进度条等动态内容。
4. 最后补临时动效层，短时反馈不应长期遮挡中央 gameplay 区。

## 2. 坐标表

| 模块 | Prefab/节点 | x | y | w | h | 说明 |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| 暗化背景 | `_overlay` | 0 | 0 | 1080 | 1920 | 排行榜遮罩 |
| 排行榜面板 | `_panel` | 70 | 170 | 940 | 1510 | 双页签榜单 |
| 标题/关闭 | `_titleText/_subtitleText/_closeBtn` | 110 | 215 | 860 | 110 | 生存排行榜 / 关闭 |
| 页签栏 | `_tabContribution/_tabStreamer` | 120 | 355 | 840 | 82 | 贡献榜 / 主播榜 |
| 表头 | `_headerRow` | 120 | 465 | 840 | 62 | 名次 / 玩家或主播 / 数据 |
| 滚动列表 | `_rowContainer` | 120 | 545 | 840 | 880 | 统一行高排行 |
| 空榜提示 | `_emptyHint` | 210 | 1450 | 660 | 74 | 暂无数据时显示 |

## 3. 关键实现规则

- 原型中的虚线框、节点名、坐标和“施工标注”仅用于 Unity 搭建，不进入最终美术图。
- 所有模块按 1080x1920 竖屏设计；如运行时 CanvasScaler 等比适配，优先保持顶部/底部/边缘锚点。
- 文字容器需要设置溢出策略：短按钮不换行，长昵称/状态文案使用省略或滚动。
- 弹窗类界面必须暗化背景；HUD/横幅类界面必须保留游戏主体安全区。

## 4. 推荐可见文案

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

## 5. Prefab 拆分与嵌套建议

SurvivalRankingPanel prefab 自身没有绑定项目脚本；运行时由 SurvivalRankingUI 的 _panel、_overlay、_closeBtn、_titleText、_subtitleText、_tabContribution、_tabStreamer、_headerRow、_rowContainer、_emptyHint 控制。

## 6. PNG 校验

本轮会生成 `ui_layout_prototype_SurvivalRankingPanel.png`，目标尺寸为 1080x1920。