# EfficiencyRaceUI_EfficiencyRaceBanner UI 策划案

> Prefab：`Assets/Prefabs/UI/Panels/EfficiencyRaceUI_EfficiencyRaceBanner.prefab`  
> 用途：采集效率竞速横幅  
> 主效果图规格：1080 x 1920 竖屏  
> 日期：2026-05-07

## 1. 界面定位与目的

在安全期循环展示两位采集贡献者的竞速对比，提升直播互动感。

该界面应被视为 采集效率竞速横幅，不是营销页。设计第一屏必须直接表达真实可用状态，并保留足够游戏主体安全区或明确的弹窗遮罩关系。

## 2. 设计目标

- 一行文案不换行溢出
- 安全期低优先级
- 和危险状态颜色区分

## 3. 当前 prefab / 模块核对

| 项目 | 内容 |
| --- | --- |
| Prefab 路径 | `Assets/Prefabs/UI/Panels/EfficiencyRaceUI_EfficiencyRaceBanner.prefab` |
| 绑定脚本 | `EfficiencyRaceUI` |
| 节点数量摘录 | 2 个 `m_Name` 记录 |
| 文案数量摘录 | 0 个 TMP/Text 文案记录 |
| 嵌套子 prefab | 无直接嵌套 Panels prefab |

关键节点摘录：

- MessageText
- BannerRoot

可见/默认文案摘录：

- 当前 prefab 文案多由运行时注入，设计稿应预留动态文本宽度。



## 4. 推荐布局与层级

BannerRoot 小尺寸靠顶部，MessageText 一行展示对战信息。

| 模块 | Prefab/节点 | x | y | w | h | 说明 |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| 游戏安全区 | `SceneSafeArea` | 40 | 250 | 1000 | 1260 | 主画面 |
| 竞速条 | `BannerRoot` | 110 | 135 | 860 | 105 | 低张力展示 |
| 对比文案 | `MessageText` | 150 | 165 | 780 | 46 | 采集王 A vs B |

## 5. 模块设计说明

### 游戏安全区

- 对应节点：`SceneSafeArea`
- 建议位置：x=40 y=250 w=1000 h=1260。
- 设计要点：主画面

### 竞速条

- 对应节点：`BannerRoot`
- 建议位置：x=110 y=135 w=860 h=105。
- 设计要点：低张力展示

### 对比文案

- 对应节点：`MessageText`
- 建议位置：x=150 y=165 w=780 h=46。
- 设计要点：采集王 A vs B

## 6. 视觉风格

绿色/金色竞技感，低透明底，不压主 HUD。

建议保持项目统一语言：冰蓝代表常规 HUD，金色代表贡献/VIP/奖励，红橙代表危险或不可逆操作，绿色代表修复/完成。所有文字必须在 1080x1920 竖屏下可读，按钮文字不得溢出。

## 7. AI 效果图场景

顶部薄横幅显示“食物采集王：A vs B”，像赛事比分条。

效果图应把施工原型里的坐标和灰盒理解为布局约束，而不是最终视觉元素。最终图可以加入材质、光效、图标、进度条和真实游戏背景，但不要出现 prefab 名、坐标、虚线框、施工标注等文字。

## 8. 实现注意事项

- 当前 prefab 没有额外特殊实现项；按以下通用规则落地。
- 所有临时层应通过 CanvasGroup 或子节点显隐控制，避免长期遮挡中央 gameplay 区。
- 动态文本需要预留 20%-30% 的宽度余量，中文、数字和昵称都要能截断或滚动。
- 若该界面作为其他主面板的子面板复用，应优先服从主面板坐标，不再另起一套视觉规范。

## 9. 验收标准

- 文件均位于 `docs/EfficiencyRaceUI_EfficiencyRaceBanner/`。
- SVG 和 PNG 都是 1080x1920 竖屏参考。
- 效果图能清楚表达 采集效率竞速横幅 的常用状态。
- 默认文案、节点名称和脚本关系能在文档中追溯到 prefab。
- 若存在嵌套子 prefab，README 和批量索引中明确避免重复生成。