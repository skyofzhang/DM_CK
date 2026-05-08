# BroadcasterPanel_BroadcasterPanelController 施工原型说明

> 对应 prefab：`Assets/Prefabs/UI/Panels/BroadcasterPanel_BroadcasterPanelController.prefab`  
> 画布：1080 x 1920 竖屏  
> 原型文件：`ui_layout_prototype_BroadcasterPanel_BroadcasterPanelController.svg`

## 1. 施工顺序

1. 先建立 1080x1920 根画布，确认 Safe Area 与全屏遮罩关系。
2. 按坐标表搭建主模块边界，保证每个节点的锚点和尺寸稳定。
3. 再填充标题、正文、列表、按钮、倒计时、进度条等动态内容。
4. 最后补临时动效层，短时反馈不应长期遮挡中央 gameplay 区。

## 2. 坐标表

| 模块 | Prefab/节点 | x | y | w | h | 说明 |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| 游戏画面安全区 | `SceneSafeArea` | 32 | 130 | 760 | 1360 | 左侧主视野 |
| 主播控制台 | `PanelRoot` | 810 | 250 | 230 | 920 | 工具按钮栈 |
| 功能按钮网格 | `Boost/Event/Roulette/Shop` | 835 | 305 | 180 | 520 | 含 CD 文案 |
| 升级/建造/远征 | `Building/Expedition/Gate` | 835 | 850 | 180 | 250 | 锁定态灰化 |
| 决策 HUD 子面板 | `BroadcasterDecisionHUD` | 110 | 1280 | 760 | 220 | 本面板覆盖，不单独跑 |
| 主播话术卡 | `StreamerPromptCard` | 110 | 1515 | 760 | 140 | 本面板覆盖，不单独跑 |

## 3. 关键实现规则

- 原型中的虚线框、节点名、坐标和“施工标注”仅用于 Unity 搭建，不进入最终美术图。
- 所有模块按 1080x1920 竖屏设计；如运行时 CanvasScaler 等比适配，优先保持顶部/底部/边缘锚点。
- 文字容器需要设置溢出策略：短按钮不换行，长昵称/状态文案使用省略或滚动。
- 弹窗类界面必须暗化背景；HUD/横幅类界面必须保留游戏主体安全区。
- 直接嵌套子 prefab 已在本主面板内统一布局：`StreamerPromptUI_StreamerPromptCard`、`BroadcasterDecisionHUD_DecisionHUD`。


## 4. 推荐可见文案

- `建`
- `海浪`
- `购`
- `援`
- `闪电`
- `战`
- `盘`
- `欢迎来到极地生存法则！白天刷仙女棒帮矿工，夜晚刷甜甜圈保护城门！`
- `升级城门`
- `关闭引导`
- `探`

## 5. Prefab 拆分与嵌套建议

该界面是主面板，应继续把 `StreamerPromptUI_StreamerPromptCard`、`BroadcasterDecisionHUD_DecisionHUD` 当作内部模块维护，避免重复生成独立视觉规范。

## 6. PNG 校验

本轮会生成 `ui_layout_prototype_BroadcasterPanel_BroadcasterPanelController.png`，目标尺寸为 1080x1920。