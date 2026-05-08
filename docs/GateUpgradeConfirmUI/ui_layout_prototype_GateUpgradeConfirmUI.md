# GateUpgradeConfirmUI 施工原型说明

> 对应 prefab：`Assets/Prefabs/UI/Panels/GateUpgradeConfirmUI.prefab`  
> 画布：1080 x 1920 竖屏  
> 原型文件：`ui_layout_prototype_GateUpgradeConfirmUI.svg`

## 1. 施工顺序

1. 先建立 1080x1920 根画布，确认 Safe Area 与全屏遮罩关系。
2. 按坐标表搭建主模块边界，保证每个节点的锚点和尺寸稳定。
3. 再填充标题、正文、列表、按钮、倒计时、进度条等动态内容。
4. 最后补临时动效层，短时反馈不应长期遮挡中央 gameplay 区。

## 2. 坐标表

| 模块 | Prefab/节点 | x | y | w | h | 说明 |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| 暗化背景 | `ModalBackdrop` | 0 | 0 | 1080 | 1920 | A 类弹窗 |
| 弹窗盒 | `DialogBox` | 150 | 520 | 780 | 620 | 升级城门 |
| 标题 | `Title` | 210 | 570 | 660 | 70 | 升级城门 |
| 等级对比 | `CurrentLevel/NextLevel` | 220 | 680 | 640 | 84 | 当前 Lv.1 -> Lv.2 |
| 消耗 | `Cost` | 220 | 800 | 640 | 60 | 消耗矿石 x100 |
| 新特性 | `Features` | 220 | 890 | 640 | 110 | 新特性说明 |
| 按钮区 | `BtnCancel/BtnConfirm` | 220 | 1050 | 640 | 92 | 取消 / 确认升级 |

## 3. 关键实现规则

- 原型中的虚线框、节点名、坐标和“施工标注”仅用于 Unity 搭建，不进入最终美术图。
- 所有模块按 1080x1920 竖屏设计；如运行时 CanvasScaler 等比适配，优先保持顶部/底部/边缘锚点。
- 文字容器需要设置溢出策略：短按钮不换行，长昵称/状态文案使用省略或滚动。
- 弹窗类界面必须暗化背景；HUD/横幅类界面必须保留游戏主体安全区。


## 4. 推荐可见文案

- `升级城门`
- `→ Lv.2「铁栅」`
- `当前 Lv.1`
- `取消`
- `消耗矿石 × 100`
- `[新特性说明]`
- `确认升级`

## 5. Prefab 拆分与嵌套建议

该 prefab 暂无直接嵌套 Panels 子 prefab，可作为独立界面维护；若未来被更大的主面板嵌入，应以主面板坐标为准。

## 6. PNG 校验

本轮会生成 `ui_layout_prototype_GateUpgradeConfirmUI.png`，目标尺寸为 1080x1920。