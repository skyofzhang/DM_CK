# TensionOverlayUI_TensionOverlay UI 策划案

> Prefab：`Assets/Prefabs/UI/Panels/TensionOverlayUI_TensionOverlay.prefab`  
> 用途：全屏张力危机覆盖层  
> 主效果图规格：1080 x 1920 竖屏  
> 日期：2026-05-07

## 1. 界面定位与目的

根据 tension 值在屏幕边缘显示紧张、危急、濒死视觉反馈。

该界面应被视为 全屏张力危机覆盖层，不是营销页。设计第一屏必须直接表达真实可用状态，并保留足够游戏主体安全区或明确的弹窗遮罩关系。

## 2. 设计目标

- 中心 gameplay 仍清楚
- 颜色等级明确
- alpha 呼吸不影响读字

## 3. 当前 prefab / 模块核对

| 项目 | 内容 |
| --- | --- |
| Prefab 路径 | `Assets/Prefabs/UI/Panels/TensionOverlayUI_TensionOverlay.prefab` |
| 绑定脚本 | `TensionOverlayUI` |
| 节点数量摘录 | 0 个 `m_Name` 记录 |
| 文案数量摘录 | 0 个 TMP/Text 文案记录 |
| 嵌套子 prefab | 无直接嵌套 Panels prefab |

关键节点摘录：

- 当前 prefab 没有额外特殊实现项；按以下通用规则落地。
可见/默认文案摘录：

- 当前 prefab 文案多由运行时注入，设计稿应预留动态文本宽度。



## 4. 推荐布局与层级

OverlayImage 全屏，alpha 与颜色由张力等级驱动，不用 SetActive 隐藏。

| 模块 | Prefab/节点 | x | y | w | h | 说明 |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| 全屏覆盖 | `OverlayImage` | 0 | 0 | 1080 | 1920 | 颜色/alpha 由 tension 驱动 |
| 中心清晰区 | `GameplayClearZone` | 190 | 360 | 700 | 1050 | 中间尽量透明 |
| 边缘警戒 | `EdgeVignette` | 30 | 120 | 1020 | 1680 | 四边增强 |
| 等级标注 | `TensionLevels` | 270 | 1500 | 540 | 120 | 施工标注：safe/tense/critical/dying |

## 5. 模块设计说明

### 全屏覆盖

- 对应节点：`OverlayImage`
- 建议位置：x=0 y=0 w=1080 h=1920。
- 设计要点：颜色/alpha 由 tension 驱动

### 中心清晰区

- 对应节点：`GameplayClearZone`
- 建议位置：x=190 y=360 w=700 h=1050。
- 设计要点：中间尽量透明

### 边缘警戒

- 对应节点：`EdgeVignette`
- 建议位置：x=30 y=120 w=1020 h=1680。
- 设计要点：四边增强

### 等级标注

- 对应节点：`TensionLevels`
- 建议位置：x=270 y=1500 w=540 h=120。
- 设计要点：施工标注：safe/tense/critical/dying

## 6. 视觉风格

安全态透明，紧张态琥珀呼吸，危急态红橙脉冲，濒死态深红快速闪。

建议保持项目统一语言：冰蓝代表常规 HUD，金色代表贡献/VIP/奖励，红橙代表危险或不可逆操作，绿色代表修复/完成。所有文字必须在 1080x1920 竖屏下可读，按钮文字不得溢出。

## 7. AI 效果图场景

当城门/资源危机升高，屏幕边缘出现红色呼吸光，不遮挡中心操作。

效果图应把施工原型里的坐标和灰盒理解为布局约束，而不是最终视觉元素。最终图可以加入材质、光效、图标、进度条和真实游戏背景，但不要出现 prefab 名、坐标、虚线框、施工标注等文字。

## 8. 实现注意事项

- 当前 prefab 没有额外特殊实现项；按以下通用规则落地。
- 所有临时层应通过 CanvasGroup 或子节点显隐控制，避免长期遮挡中央 gameplay 区。
- 动态文本需要预留 20%-30% 的宽度余量，中文、数字和昵称都要能截断或滚动。
- 若该界面作为其他主面板的子面板复用，应优先服从主面板坐标，不再另起一套视觉规范。

## 9. 验收标准

- 文件均位于 `docs/TensionOverlayUI_TensionOverlay/`。
- SVG 和 PNG 都是 1080x1920 竖屏参考。
- 效果图能清楚表达 全屏张力危机覆盖层 的常用状态。
- 默认文案、节点名称和脚本关系能在文档中追溯到 prefab。
- 若存在嵌套子 prefab，README 和批量索引中明确避免重复生成。