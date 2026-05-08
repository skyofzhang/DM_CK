# BroadcasterPanel_BroadcasterPanelController UI 策划案

> Prefab：`Assets/Prefabs/UI/Panels/BroadcasterPanel_BroadcasterPanelController.prefab`  
> 用途：主播控制台主面板  
> 主效果图规格：1080 x 1920 竖屏  
> 日期：2026-05-07

## 1. 界面定位与目的

为房主/主播提供加速、事件、轮盘、商店、建造、远征、支援模式、城门升级等快速操作入口。

该界面应被视为 主播控制台主面板，不是营销页。设计第一屏必须直接表达真实可用状态，并保留足够游戏主体安全区或明确的弹窗遮罩关系。

## 2. 设计目标

- 按钮尺寸稳定，适合直播中快速点按
- 冷却状态一眼可见
- 子面板决策卡与话术卡归入同一主面板资料

## 3. 当前 prefab / 模块核对

| 项目 | 内容 |
| --- | --- |
| Prefab 路径 | `Assets/Prefabs/UI/Panels/BroadcasterPanel_BroadcasterPanelController.prefab` |
| 绑定脚本 | `FeatureLockOverlay`、`BroadcasterPanel` |
| 节点数量摘录 | 32 个 `m_Name` 记录 |
| 文案数量摘录 | 11 个 TMP/Text 文案记录 |
| 嵌套子 prefab | `StreamerPromptUI_StreamerPromptCard.prefab`、`BroadcasterDecisionHUD_DecisionHUD.prefab` |

关键节点摘录：

- CdText
- IconText
- PanelRoot
- RouletteButton
- BuildingButton
- ShopTabButton
- SupporterModeButton
- DisableOnboardingButton
- BoostButton
- TipText
- BtnUpgradeGate
- EventButton
- Label
- TribeWarButton
- BroadcasterTipBar
- ExpeditionButton

可见/默认文案摘录：

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

> 本资料已覆盖这些直接嵌套子 prefab：`StreamerPromptUI_StreamerPromptCard`、`BroadcasterDecisionHUD_DecisionHUD`。为避免重复，本轮不为这些子面板单独生成资料。


## 4. 推荐布局与层级

PanelRoot 是控制台主体；BroadcasterDecisionHUD_DecisionHUD 与 StreamerPromptUI_StreamerPromptCard 已作为子 prefab 嵌入本主面板，不另起资料。

| 模块 | Prefab/节点 | x | y | w | h | 说明 |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| 游戏画面安全区 | `SceneSafeArea` | 32 | 130 | 760 | 1360 | 左侧主视野 |
| 主播控制台 | `PanelRoot` | 810 | 250 | 230 | 920 | 工具按钮栈 |
| 功能按钮网格 | `Boost/Event/Roulette/Shop` | 835 | 305 | 180 | 520 | 含 CD 文案 |
| 升级/建造/远征 | `Building/Expedition/Gate` | 835 | 850 | 180 | 250 | 锁定态灰化 |
| 决策 HUD 子面板 | `BroadcasterDecisionHUD` | 110 | 1280 | 760 | 220 | 本面板覆盖，不单独跑 |
| 主播话术卡 | `StreamerPromptCard` | 110 | 1515 | 760 | 140 | 本面板覆盖，不单独跑 |

## 5. 模块设计说明

### 游戏画面安全区

- 对应节点：`SceneSafeArea`
- 建议位置：x=32 y=130 w=760 h=1360。
- 设计要点：左侧主视野

### 主播控制台

- 对应节点：`PanelRoot`
- 建议位置：x=810 y=250 w=230 h=920。
- 设计要点：工具按钮栈

### 功能按钮网格

- 对应节点：`Boost/Event/Roulette/Shop`
- 建议位置：x=835 y=305 w=180 h=520。
- 设计要点：含 CD 文案

### 升级/建造/远征

- 对应节点：`Building/Expedition/Gate`
- 建议位置：x=835 y=850 w=180 h=250。
- 设计要点：锁定态灰化

### 决策 HUD 子面板

- 对应节点：`BroadcasterDecisionHUD`
- 建议位置：x=110 y=1280 w=760 h=220。
- 设计要点：本面板覆盖，不单独跑

### 主播话术卡

- 对应节点：`StreamerPromptCard`
- 建议位置：x=110 y=1515 w=760 h=140。
- 设计要点：本面板覆盖，不单独跑

## 6. 视觉风格

安静的直播运营工具面板，深蓝黑底、冰蓝描边，危险/冷却/锁定态用红橙或灰化。

建议保持项目统一语言：冰蓝代表常规 HUD，金色代表贡献/VIP/奖励，红橙代表危险或不可逆操作，绿色代表修复/完成。所有文字必须在 1080x1920 竖屏下可读，按钮文字不得溢出。

## 7. AI 效果图场景

竖屏右侧贴边出现主播操作栈，底部或中右弹出决策 HUD，左侧游戏主体保持可观察。

效果图应把施工原型里的坐标和灰盒理解为布局约束，而不是最终视觉元素。最终图可以加入材质、光效、图标、进度条和真实游戏背景，但不要出现 prefab 名、坐标、虚线框、施工标注等文字。

## 8. 实现注意事项

- FeatureLockOverlay 用于锁定/解锁按钮视觉，锁定态保留原按钮占位。
- 所有子卡片跟随 BroadcasterPanel 统一视觉规范。
- 所有临时层应通过 CanvasGroup 或子节点显隐控制，避免长期遮挡中央 gameplay 区。
- 动态文本需要预留 20%-30% 的宽度余量，中文、数字和昵称都要能截断或滚动。
- 若该界面作为其他主面板的子面板复用，应优先服从主面板坐标，不再另起一套视觉规范。

## 9. 验收标准

- 文件均位于 `docs/BroadcasterPanel_BroadcasterPanelController/`。
- SVG 和 PNG 都是 1080x1920 竖屏参考。
- 效果图能清楚表达 主播控制台主面板 的常用状态。
- 默认文案、节点名称和脚本关系能在文档中追溯到 prefab。
- 若存在嵌套子 prefab，README 和批量索引中明确避免重复生成。