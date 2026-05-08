# PauseOverlayUI_PauseOverlayPanel UI 策划案

> Prefab：`Assets/Prefabs/UI/Panels/PauseOverlayUI_PauseOverlayPanel.prefab`  
> 用途：暂停遮罩  
> 主效果图规格：1080 x 1920 竖屏  
> 日期：2026-05-08

## 1. 界面定位与目的

订阅暂停/恢复事件，暂停时动态创建并显示全屏半透明黑色遮罩与中央金色大字。

该界面应被视为 暂停遮罩，不是营销页。设计第一屏必须直接表达真实可用状态，并保留足够游戏主体安全区或明确的弹窗遮罩关系。

## 2. 设计目标

- 暂停态明确
- 不误导为可交互弹窗
- 遮罩阻挡点击穿透

## 3. 当前 prefab / 模块核对

| 项目 | 内容 |
| --- | --- |
| Prefab 路径 | `Assets/Prefabs/UI/Panels/PauseOverlayUI_PauseOverlayPanel.prefab` |
| 绑定脚本 | `PauseOverlayUI` |
| 节点数量摘录 | 0 个 `m_Name` 记录 |
| 文案数量摘录 | 0 个 TMP/Text 文案记录 |

关键节点摘录：

- 当前 prefab 没有额外特殊实现项；按以下通用规则落地。

可见/默认文案摘录：

- 当前 prefab 文案多由运行时注入，设计稿应预留动态文本宽度。

## 3.1 运行时控制关系补充

- Assets/Scripts/UI/PauseOverlayUI.cs：BuildOverlayHierarchy 动态创建 UI
- HandleGamePaused 显示 _overlayRoot，HandleGameResumed 隐藏
- 脚本自身 GO 保持 active，只有子节点 SetActive(false)

## 4. 推荐布局与层级

PauseOverlayUI.cs 在 Awake 中动态创建 PauseOverlayRoot、背景 Image、Title、Subtitle；prefab 本身只有挂载点，不应设计继续/设置/退出按钮。

| 模块 | Prefab/节点 | x | y | w | h | 说明 |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| 全屏遮罩 | `PauseOverlayRoot/BgImage` | 0 | 0 | 1080 | 1920 | 黑色 alpha 0.7 |
| 暂停标题 | `Title` | 120 | 790 | 840 | 120 | 游戏已暂停 |
| 副标题 | `Subtitle` | 180 | 915 | 720 | 70 | GM 调试模式 — 等待主播恢复 |
| 点击阻挡层 | `RaycastBlocker` | 0 | 0 | 1080 | 1920 | 施工标注，不出现在效果图 |

## 5. 模块设计说明

### 全屏遮罩

- 对应节点：`PauseOverlayRoot/BgImage`
- 建议位置：x=0 y=0 w=1080 h=1920。
- 设计要点：黑色 alpha 0.7

### 暂停标题

- 对应节点：`Title`
- 建议位置：x=120 y=790 w=840 h=120。
- 设计要点：游戏已暂停

### 副标题

- 对应节点：`Subtitle`
- 建议位置：x=180 y=915 w=720 h=70。
- 设计要点：GM 调试模式 — 等待主播恢复

### 点击阻挡层

- 对应节点：`RaycastBlocker`
- 建议位置：x=0 y=0 w=1080 h=1920。
- 设计要点：施工标注，不出现在效果图

## 6. 视觉风格

70% 黑色遮罩，中央金色“游戏已暂停”，副标题浅灰，持续可见直到恢复。

建议保持项目统一语言：冰蓝代表常规 HUD，金色代表贡献/VIP/奖励，红橙代表危险或不可逆操作，绿色代表修复/完成。所有文字必须在 1080x1920 竖屏下可读，按钮文字不得溢出。

## 7. AI 效果图场景

游戏暂停时全屏暗化，中央显示“游戏已暂停”和“GM 调试模式 — 等待主播恢复”；恢复时立即消失。

效果图应把施工原型里的坐标和灰盒理解为布局约束，而不是最终视觉元素。最终图可以加入材质、光效、图标、进度条和真实游戏背景，但不要出现 prefab 名、坐标、虚线框、施工标注等文字。

## 8. 实现注意事项

- Assets/Scripts/UI/PauseOverlayUI.cs：BuildOverlayHierarchy 动态创建 UI
- HandleGamePaused 显示 _overlayRoot，HandleGameResumed 隐藏
- 脚本自身 GO 保持 active，只有子节点 SetActive(false)
- 所有临时层应通过 CanvasGroup 或子节点显隐控制，避免长期遮挡中央 gameplay 区。
- 动态文本需要预留 20%-30% 的宽度余量，中文、数字和昵称都要能截断或滚动。
- 若该界面作为其他主面板的子面板复用，应优先服从主面板坐标，不再另起一套视觉规范。

## 9. 验收标准

- 文件均位于 `docs/PauseOverlayUI_PauseOverlayPanel/`。
- SVG 和 PNG 都是 1080x1920 竖屏参考。
- 效果图能清楚表达 暂停遮罩 的常用状态。
- 默认文案、节点名称和脚本关系能在文档中追溯到 prefab 或运行时控制脚本。