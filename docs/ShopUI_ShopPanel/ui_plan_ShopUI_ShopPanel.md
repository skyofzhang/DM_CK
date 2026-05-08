# ShopUI_ShopPanel UI 策划案

> Prefab：`Assets/Prefabs/UI/Panels/ShopUI_ShopPanel.prefab`  
> 用途：商店主面板  
> 主效果图规格：1080 x 1920 竖屏  
> 日期：2026-05-07

## 1. 界面定位与目的

展示商品分类、背包、余额和可购买列表。

该界面应被视为 商店主面板，不是营销页。设计第一屏必须直接表达真实可用状态，并保留足够游戏主体安全区或明确的弹窗遮罩关系。

## 2. 设计目标

- 商品列表可扫描
- 余额随时可见
- 背包 Tab 和商品 Tab 区分清楚

## 3. 当前 prefab / 模块核对

| 项目 | 内容 |
| --- | --- |
| Prefab 路径 | `Assets/Prefabs/UI/Panels/ShopUI_ShopPanel.prefab` |
| 绑定脚本 | `ShopUI` |
| 节点数量摘录 | 18 个 `m_Name` 记录 |
| 文案数量摘录 | 6 个 TMP/Text 文案记录 |
| 嵌套子 prefab | 无直接嵌套 Panels prefab |

关键节点摘录：

- Label
- BtnClose
- StatusBar
- Header
- TabB
- TabInventory
- TabA
- TabRow
- BalanceBar
- Viewport
- BalanceText
- StatusText
- TitleText
- Content
- ScrollRoot

可见/默认文案摘录：

- `×`
- `我的背包`
- `贡献 0  可消费 0  终身 0`
- `商店`
- `A 类道具`
- `B 类装备`



## 4. 推荐布局与层级

Header/TitleText/BtnClose 顶部，TabRow 切换，BalanceBar 显示贡献，可滚动 Viewport 展示商品。

| 模块 | Prefab/节点 | x | y | w | h | 说明 |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| 暗化背景 | `ShopBackdrop` | 0 | 0 | 1080 | 1920 | 覆盖游戏 |
| 商店面板 | `ShopPanel` | 60 | 150 | 960 | 1520 | 商店 |
| 标题栏 | `Header/TitleText/BtnClose` | 90 | 190 | 900 | 90 | 商店 / 关闭 |
| 标签栏 | `TabRow` | 100 | 305 | 880 | 76 | 商店A / 商店B / 背包 |
| 余额条 | `BalanceBar/BalanceText` | 100 | 405 | 880 | 72 | 贡献/可消费/终身 |
| 商品列表 | `Viewport/Content` | 100 | 505 | 880 | 1040 | 商品按钮列表 |
| 状态提示 | `StatusText` | 120 | 1570 | 840 | 54 | 购买失败/状态 |

## 5. 模块设计说明

### 暗化背景

- 对应节点：`ShopBackdrop`
- 建议位置：x=0 y=0 w=1080 h=1920。
- 设计要点：覆盖游戏

### 商店面板

- 对应节点：`ShopPanel`
- 建议位置：x=60 y=150 w=960 h=1520。
- 设计要点：商店

### 标题栏

- 对应节点：`Header/TitleText/BtnClose`
- 建议位置：x=90 y=190 w=900 h=90。
- 设计要点：商店 / 关闭

### 标签栏

- 对应节点：`TabRow`
- 建议位置：x=100 y=305 w=880 h=76。
- 设计要点：商店A / 商店B / 背包

### 余额条

- 对应节点：`BalanceBar/BalanceText`
- 建议位置：x=100 y=405 w=880 h=72。
- 设计要点：贡献/可消费/终身

### 商品列表

- 对应节点：`Viewport/Content`
- 建议位置：x=100 y=505 w=880 h=1040。
- 设计要点：商品按钮列表

### 状态提示

- 对应节点：`StatusText`
- 建议位置：x=120 y=1570 w=840 h=54。
- 设计要点：购买失败/状态

## 6. 视觉风格

功能型商店界面，信息密度高但分区稳定，避免营销式大卡堆砌。

建议保持项目统一语言：冰蓝代表常规 HUD，金色代表贡献/VIP/奖励，红橙代表危险或不可逆操作，绿色代表修复/完成。所有文字必须在 1080x1920 竖屏下可读，按钮文字不得溢出。

## 7. AI 效果图场景

全屏商店面板，顶部标题，下面标签页与余额，主体为商品列表。

效果图应把施工原型里的坐标和灰盒理解为布局约束，而不是最终视觉元素。最终图可以加入材质、光效、图标、进度条和真实游戏背景，但不要出现 prefab 名、坐标、虚线框、施工标注等文字。

## 8. 实现注意事项

- 当前 prefab 没有额外特殊实现项；按以下通用规则落地。
- 所有临时层应通过 CanvasGroup 或子节点显隐控制，避免长期遮挡中央 gameplay 区。
- 动态文本需要预留 20%-30% 的宽度余量，中文、数字和昵称都要能截断或滚动。
- 若该界面作为其他主面板的子面板复用，应优先服从主面板坐标，不再另起一套视觉规范。

## 9. 验收标准

- 文件均位于 `docs/ShopUI_ShopPanel/`。
- SVG 和 PNG 都是 1080x1920 竖屏参考。
- 效果图能清楚表达 商店主面板 的常用状态。
- 默认文案、节点名称和脚本关系能在文档中追溯到 prefab。
- 若存在嵌套子 prefab，README 和批量索引中明确避免重复生成。