# SurvivalSettingsUI_SurvivalSettingsPanel UI 策划案

> Prefab：`Assets/Prefabs/UI/Panels/SurvivalSettingsUI_SurvivalSettingsPanel.prefab`  
> 用途：生存设置面板  
> 主效果图规格：1080 x 1920 竖屏  
> 日期：2026-05-07

## 1. 界面定位与目的

控制 BGM/SFX 音量、静音、礼物视频动画、VIP 入场视频等偏好。

该界面应被视为 生存设置面板，不是营销页。设计第一屏必须直接表达真实可用状态，并保留足够游戏主体安全区或明确的弹窗遮罩关系。

## 2. 设计目标

- 每行设置项对齐
- 滑条可触控
- 关闭按钮固定右上

## 3. 当前 prefab / 模块核对

| 项目 | 内容 |
| --- | --- |
| Prefab 路径 | `Assets/Prefabs/UI/Panels/SurvivalSettingsUI_SurvivalSettingsPanel.prefab` |
| 绑定脚本 | `SurvivalSettingsUI` |
| 节点数量摘录 | 36 个 `m_Name` 记录 |
| 文案数量摘录 | 9 个 TMP/Text 文案记录 |
| 嵌套子 prefab | 无直接嵌套 Panels prefab |

关键节点摘录：

- Fill
- Handle Slide Area
- Checkmark
- GiftVideoRow
- Label
- Fill Area
- SFXSlider
- Handle
- Toggle
- BGMToggleBtn
- Background
- Divider
- BGMValueText
- CloseBtn
- BGMToggleText
- VIPVideoRow
- TitleText
- BGMSlider
- SFXRow
- SFXToggleText
- VersionText
- CloseText
- BGMRow
- SFXValueText
- SFXToggleBtn

可见/默认文案摘录：

- `VIP入场视频`
- `背景音乐`
- `礼物视频动画`
- `音效`
- `80%`
- `设置`
- `冬日生存法则 v0.1`
- `X`



## 4. 推荐布局与层级

Panel 中按设置项分行：Slider、Toggle、Label、CloseBtn，VersionText 底部。

| 模块 | Prefab/节点 | x | y | w | h | 说明 |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| 暗化背景 | `SettingsBackdrop` | 0 | 0 | 1080 | 1920 | 设置弹窗 |
| 设置面板 | `SurvivalSettingsPanel` | 130 | 360 | 820 | 1040 | 设置 |
| 标题/关闭 | `Header/CloseBtn` | 180 | 420 | 720 | 82 | 设置 / 关闭 |
| BGM 行 | `BGMSlider/BGMToggle` | 190 | 555 | 700 | 110 | 背景音乐 |
| SFX 行 | `SFXSlider/SFXToggle` | 190 | 700 | 700 | 110 | 音效 |
| 视频开关 | `GiftVideo/VIPVideo` | 190 | 850 | 700 | 210 | 礼物视频 / VIP入场视频 |
| 版本号 | `VersionText` | 190 | 1240 | 700 | 54 | 版本信息 |

## 5. 模块设计说明

### 暗化背景

- 对应节点：`SettingsBackdrop`
- 建议位置：x=0 y=0 w=1080 h=1920。
- 设计要点：设置弹窗

### 设置面板

- 对应节点：`SurvivalSettingsPanel`
- 建议位置：x=130 y=360 w=820 h=1040。
- 设计要点：设置

### 标题/关闭

- 对应节点：`Header/CloseBtn`
- 建议位置：x=180 y=420 w=720 h=82。
- 设计要点：设置 / 关闭

### BGM 行

- 对应节点：`BGMSlider/BGMToggle`
- 建议位置：x=190 y=555 w=700 h=110。
- 设计要点：背景音乐

### SFX 行

- 对应节点：`SFXSlider/SFXToggle`
- 建议位置：x=190 y=700 w=700 h=110。
- 设计要点：音效

### 视频开关

- 对应节点：`GiftVideo/VIPVideo`
- 建议位置：x=190 y=850 w=700 h=210。
- 设计要点：礼物视频 / VIP入场视频

### 版本号

- 对应节点：`VersionText`
- 建议位置：x=190 y=1240 w=700 h=54。
- 设计要点：版本信息

## 6. 视觉风格

工具型设置弹窗，灰蓝底，滑条清楚，开关用明确的 on/off 状态。

建议保持项目统一语言：冰蓝代表常规 HUD，金色代表贡献/VIP/奖励，红橙代表危险或不可逆操作，绿色代表修复/完成。所有文字必须在 1080x1920 竖屏下可读，按钮文字不得溢出。

## 7. AI 效果图场景

中央设置面板覆盖大厅或游戏，音量滑条和礼物视频开关纵向排列。

效果图应把施工原型里的坐标和灰盒理解为布局约束，而不是最终视觉元素。最终图可以加入材质、光效、图标、进度条和真实游戏背景，但不要出现 prefab 名、坐标、虚线框、施工标注等文字。

## 8. 实现注意事项

- 当前 prefab 没有额外特殊实现项；按以下通用规则落地。
- 所有临时层应通过 CanvasGroup 或子节点显隐控制，避免长期遮挡中央 gameplay 区。
- 动态文本需要预留 20%-30% 的宽度余量，中文、数字和昵称都要能截断或滚动。
- 若该界面作为其他主面板的子面板复用，应优先服从主面板坐标，不再另起一套视觉规范。

## 9. 验收标准

- 文件均位于 `docs/SurvivalSettingsUI_SurvivalSettingsPanel/`。
- SVG 和 PNG 都是 1080x1920 竖屏参考。
- 效果图能清楚表达 生存设置面板 的常用状态。
- 默认文案、节点名称和脚本关系能在文档中追溯到 prefab。
- 若存在嵌套子 prefab，README 和批量索引中明确避免重复生成。