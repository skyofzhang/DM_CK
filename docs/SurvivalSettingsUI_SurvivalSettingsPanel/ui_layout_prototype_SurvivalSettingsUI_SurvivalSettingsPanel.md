# SurvivalSettingsUI_SurvivalSettingsPanel 施工原型说明

> 对应 prefab：`Assets/Prefabs/UI/Panels/SurvivalSettingsUI_SurvivalSettingsPanel.prefab`  
> 画布：1080 x 1920 竖屏  
> 原型文件：`ui_layout_prototype_SurvivalSettingsUI_SurvivalSettingsPanel.svg`

## 1. 施工顺序

1. 先建立 1080x1920 根画布，确认 Safe Area 与全屏遮罩关系。
2. 按坐标表搭建主模块边界，保证每个节点的锚点和尺寸稳定。
3. 再填充标题、正文、列表、按钮、倒计时、进度条等动态内容。
4. 最后补临时动效层，短时反馈不应长期遮挡中央 gameplay 区。

## 2. 坐标表

| 模块 | Prefab/节点 | x | y | w | h | 说明 |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| 暗化背景 | `SettingsBackdrop` | 0 | 0 | 1080 | 1920 | 设置弹窗 |
| 设置面板 | `SurvivalSettingsPanel` | 130 | 360 | 820 | 1040 | 设置 |
| 标题/关闭 | `Header/CloseBtn` | 180 | 420 | 720 | 82 | 设置 / 关闭 |
| BGM 行 | `BGMSlider/BGMToggle` | 190 | 555 | 700 | 110 | 背景音乐 |
| SFX 行 | `SFXSlider/SFXToggle` | 190 | 700 | 700 | 110 | 音效 |
| 视频开关 | `GiftVideo/VIPVideo` | 190 | 850 | 700 | 210 | 礼物视频 / VIP入场视频 |
| 版本号 | `VersionText` | 190 | 1240 | 700 | 54 | 版本信息 |

## 3. 关键实现规则

- 原型中的虚线框、节点名、坐标和“施工标注”仅用于 Unity 搭建，不进入最终美术图。
- 所有模块按 1080x1920 竖屏设计；如运行时 CanvasScaler 等比适配，优先保持顶部/底部/边缘锚点。
- 文字容器需要设置溢出策略：短按钮不换行，长昵称/状态文案使用省略或滚动。
- 弹窗类界面必须暗化背景；HUD/横幅类界面必须保留游戏主体安全区。


## 4. 推荐可见文案

- `VIP入场视频`
- `背景音乐`
- `礼物视频动画`
- `音效`
- `80%`
- `设置`
- `冬日生存法则 v0.1`
- `X`

## 5. Prefab 拆分与嵌套建议

该 prefab 暂无直接嵌套 Panels 子 prefab，可作为独立界面维护；若未来被更大的主面板嵌入，应以主面板坐标为准。

## 6. PNG 校验

本轮会生成 `ui_layout_prototype_SurvivalSettingsUI_SurvivalSettingsPanel.png`，目标尺寸为 1080x1920。