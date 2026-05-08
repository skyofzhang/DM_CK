# OreRepairFloatingText_OreRepairFloatRoot 施工原型说明

> 对应 prefab：`Assets/Prefabs/UI/Panels/OreRepairFloatingText_OreRepairFloatRoot.prefab`  
> 画布：1080 x 1920 竖屏  
> 原型文件：`ui_layout_prototype_OreRepairFloatingText_OreRepairFloatRoot.svg`

## 1. 施工顺序

1. 先建立 1080x1920 根画布，确认 Safe Area 与全屏遮罩关系。
2. 按坐标表搭建主模块边界，保证每个节点的锚点和尺寸稳定。
3. 再填充标题、正文、列表、按钮、倒计时、进度条等动态内容。
4. 最后补临时动效层，短时反馈不应长期遮挡中央 gameplay 区。

## 2. 坐标表

| 模块 | Prefab/节点 | x | y | w | h | 说明 |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| 城门世界投影区 | `CityGateWorldAnchor` | 360 | 730 | 360 | 120 | CityGate + Vector3.up * 2 |
| 飘字内容 | `DamageNumber.Show` | 300 | 650 | 480 | 76 | 矿石修复城门 +7HP |
| 上浮淡出轨迹 | `TweenPath` | 390 | 520 | 300 | 240 | 1.2 秒上浮 |
| 无常驻 UI 区 | `NoPersistentPanel` | 160 | 980 | 760 | 160 | prefab 只负责脚本挂载 |

## 3. 关键实现规则

- 原型中的虚线框、节点名、坐标和“施工标注”仅用于 Unity 搭建，不进入最终美术图。
- 所有模块按 1080x1920 竖屏设计；如运行时 CanvasScaler 等比适配，优先保持顶部/底部/边缘锚点。
- 文字容器需要设置溢出策略：短按钮不换行，长昵称/状态文案使用省略或滚动。
- 弹窗类界面必须暗化背景；HUD/横幅类界面必须保留游戏主体安全区。

## 4. 推荐可见文案

- 当前 prefab 文案多由运行时注入，设计稿应预留动态文本宽度。

## 5. Prefab 拆分与嵌套建议

OreRepairFloatingText 不依赖预建 UI 子节点；它订阅资源更新后调用 DamageNumber.Show(worldPos, text, color) 生成世界空间 TMP 飘字。prefab 根只是运行脚本的挂载点。

## 6. PNG 校验

本轮会生成 `ui_layout_prototype_OreRepairFloatingText_OreRepairFloatRoot.png`，目标尺寸为 1080x1920。