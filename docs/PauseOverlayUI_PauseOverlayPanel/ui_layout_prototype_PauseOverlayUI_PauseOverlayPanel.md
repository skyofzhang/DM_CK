# PauseOverlayUI_PauseOverlayPanel 施工原型说明

> 对应 prefab：`Assets/Prefabs/UI/Panels/PauseOverlayUI_PauseOverlayPanel.prefab`  
> 画布：1080 x 1920 竖屏  
> 原型文件：`ui_layout_prototype_PauseOverlayUI_PauseOverlayPanel.svg`

## 1. 施工顺序

1. 先建立 1080x1920 根画布，确认 Safe Area 与全屏遮罩关系。
2. 按坐标表搭建主模块边界，保证每个节点的锚点和尺寸稳定。
3. 再填充标题、正文、列表、按钮、倒计时、进度条等动态内容。
4. 最后补临时动效层，短时反馈不应长期遮挡中央 gameplay 区。

## 2. 坐标表

| 模块 | Prefab/节点 | x | y | w | h | 说明 |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| 全屏遮罩 | `PauseOverlayRoot/BgImage` | 0 | 0 | 1080 | 1920 | 黑色 alpha 0.7 |
| 暂停标题 | `Title` | 120 | 790 | 840 | 120 | 游戏已暂停 |
| 副标题 | `Subtitle` | 180 | 915 | 720 | 70 | GM 调试模式 — 等待主播恢复 |
| 点击阻挡层 | `RaycastBlocker` | 0 | 0 | 1080 | 1920 | 施工标注，不出现在效果图 |

## 3. 关键实现规则

- 原型中的虚线框、节点名、坐标和“施工标注”仅用于 Unity 搭建，不进入最终美术图。
- 所有模块按 1080x1920 竖屏设计；如运行时 CanvasScaler 等比适配，优先保持顶部/底部/边缘锚点。
- 文字容器需要设置溢出策略：短按钮不换行，长昵称/状态文案使用省略或滚动。
- 弹窗类界面必须暗化背景；HUD/横幅类界面必须保留游戏主体安全区。

## 4. 推荐可见文案

- 当前 prefab 文案多由运行时注入，设计稿应预留动态文本宽度。

## 5. Prefab 拆分与嵌套建议

PauseOverlayUI.cs 在 Awake 中动态创建 PauseOverlayRoot、背景 Image、Title、Subtitle；prefab 本身只有挂载点，不应设计继续/设置/退出按钮。

## 6. PNG 校验

本轮会生成 `ui_layout_prototype_PauseOverlayUI_PauseOverlayPanel.png`，目标尺寸为 1080x1920。