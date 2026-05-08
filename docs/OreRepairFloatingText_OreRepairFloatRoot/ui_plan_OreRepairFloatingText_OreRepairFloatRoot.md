# OreRepairFloatingText_OreRepairFloatRoot UI 策划案

> Prefab：`Assets/Prefabs/UI/Panels/OreRepairFloatingText_OreRepairFloatRoot.prefab`  
> 用途：矿石修复世界飘字  
> 主效果图规格：1080 x 1920 竖屏  
> 日期：2026-05-08

## 1. 界面定位与目的

检测 resource_update 中 gateHp 增加且 ore 减少的自动修复行为，并在城门上方显示“矿石修复城门 +NHP”。

该界面应被视为 矿石修复世界飘字，不是营销页。设计第一屏必须直接表达真实可用状态，并保留足够游戏主体安全区或明确的弹窗遮罩关系。

## 2. 设计目标

- 只在自动矿石修复条件触发
- 跟随城门世界位置
- 不做常驻面板

## 3. 当前 prefab / 模块核对

| 项目 | 内容 |
| --- | --- |
| Prefab 路径 | `Assets/Prefabs/UI/Panels/OreRepairFloatingText_OreRepairFloatRoot.prefab` |
| 绑定脚本 | `OreRepairFloatingText` |
| 节点数量摘录 | 0 个 `m_Name` 记录 |
| 文案数量摘录 | 0 个 TMP/Text 文案记录 |

关键节点摘录：

- 当前 prefab 没有额外特殊实现项；按以下通用规则落地。

可见/默认文案摘录：

- 当前 prefab 文案多由运行时注入，设计稿应预留动态文本宽度。

## 3.1 运行时控制关系补充

- Assets/Scripts/UI/OreRepairFloatingText.cs：监听 SurvivalGameManager.OnResourceUpdate
- gateHpDelta > 0 且 oreDelta < 0 时显示飘字
- 复用 DamageNumber.Show，颜色为 new Color(1f, 0.85f, 0.3f)

## 4. 推荐布局与层级

OreRepairFloatingText 不依赖预建 UI 子节点；它订阅资源更新后调用 DamageNumber.Show(worldPos, text, color) 生成世界空间 TMP 飘字。prefab 根只是运行脚本的挂载点。

| 模块 | Prefab/节点 | x | y | w | h | 说明 |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| 城门世界投影区 | `CityGateWorldAnchor` | 360 | 730 | 360 | 120 | CityGate + Vector3.up * 2 |
| 飘字内容 | `DamageNumber.Show` | 300 | 650 | 480 | 76 | 矿石修复城门 +7HP |
| 上浮淡出轨迹 | `TweenPath` | 390 | 520 | 300 | 240 | 1.2 秒上浮 |
| 无常驻 UI 区 | `NoPersistentPanel` | 160 | 980 | 760 | 160 | prefab 只负责脚本挂载 |

## 5. 模块设计说明

### 城门世界投影区

- 对应节点：`CityGateWorldAnchor`
- 建议位置：x=360 y=730 w=360 h=120。
- 设计要点：CityGate + Vector3.up * 2

### 飘字内容

- 对应节点：`DamageNumber.Show`
- 建议位置：x=300 y=650 w=480 h=76。
- 设计要点：矿石修复城门 +7HP

### 上浮淡出轨迹

- 对应节点：`TweenPath`
- 建议位置：x=390 y=520 w=300 h=240。
- 设计要点：1.2 秒上浮

### 无常驻 UI 区

- 对应节点：`NoPersistentPanel`
- 建议位置：x=160 y=980 w=760 h=160。
- 设计要点：prefab 只负责脚本挂载

## 6. 视觉风格

暖黄色资源反馈飘字，1.2 秒上浮淡出，和红色伤害数字、绿色治疗数字区分。

建议保持项目统一语言：冰蓝代表常规 HUD，金色代表贡献/VIP/奖励，红橙代表危险或不可逆操作，绿色代表修复/完成。所有文字必须在 1080x1920 竖屏下可读，按钮文字不得溢出。

## 7. AI 效果图场景

城门上方出现“矿石修复城门 +7HP”之类暖黄飘字，随后上浮消失。

效果图应把施工原型里的坐标和灰盒理解为布局约束，而不是最终视觉元素。最终图可以加入材质、光效、图标、进度条和真实游戏背景，但不要出现 prefab 名、坐标、虚线框、施工标注等文字。

## 8. 实现注意事项

- Assets/Scripts/UI/OreRepairFloatingText.cs：监听 SurvivalGameManager.OnResourceUpdate
- gateHpDelta > 0 且 oreDelta < 0 时显示飘字
- 复用 DamageNumber.Show，颜色为 new Color(1f, 0.85f, 0.3f)
- 所有临时层应通过 CanvasGroup 或子节点显隐控制，避免长期遮挡中央 gameplay 区。
- 动态文本需要预留 20%-30% 的宽度余量，中文、数字和昵称都要能截断或滚动。
- 若该界面作为其他主面板的子面板复用，应优先服从主面板坐标，不再另起一套视觉规范。

## 9. 验收标准

- 文件均位于 `docs/OreRepairFloatingText_OreRepairFloatRoot/`。
- SVG 和 PNG 都是 1080x1920 竖屏参考。
- 效果图能清楚表达 矿石修复世界飘字 的常用状态。
- 默认文案、节点名称和脚本关系能在文档中追溯到 prefab 或运行时控制脚本。