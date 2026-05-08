# ShopConfirmDialogUI_ShopConfirmPanel UI 策划案

> Prefab：`Assets/Prefabs/UI/Panels/ShopConfirmDialogUI_ShopConfirmPanel.prefab`  
> 用途：商店购买二次确认  
> 主效果图规格：1080 x 1920 竖屏  
> 日期：2026-05-07

## 1. 界面定位与目的

购买消耗前展示商品名、价格、倒计时，并要求确认/取消。

该界面应被视为 商店购买二次确认，不是营销页。设计第一屏必须直接表达真实可用状态，并保留足够游戏主体安全区或明确的弹窗遮罩关系。

## 2. 设计目标

- 商品名和价格最清楚
- 倒计时不会挤占按钮
- 确认/取消按钮尺寸一致

## 3. 当前 prefab / 模块核对

| 项目 | 内容 |
| --- | --- |
| Prefab 路径 | `Assets/Prefabs/UI/Panels/ShopConfirmDialogUI_ShopConfirmPanel.prefab` |
| 绑定脚本 | `ShopConfirmDialogUI` |
| 节点数量摘录 | 8 个 `m_Name` 记录 |
| 文案数量摘录 | 6 个 TMP/Text 文案记录 |
| 嵌套子 prefab | 无直接嵌套 Panels prefab |

关键节点摘录：

- CountdownText
- Label
- BtnConfirm
- PriceText
- BtnCancel
- ItemNameText
- TitleText

可见/默认文案摘录：

- `5s`
- `取消`
- `价格：0`
- `商品名`
- `确认`
- `确认购买？`



## 4. 推荐布局与层级

TitleText、ItemNameText、PriceText、CountdownText、BtnConfirm、BtnCancel 组成紧凑弹窗。

| 模块 | Prefab/节点 | x | y | w | h | 说明 |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| 暗化背景 | `ModalBackdrop` | 0 | 0 | 1080 | 1920 | 购买确认 |
| 确认面板 | `ShopConfirmPanel` | 170 | 580 | 740 | 520 | 确认购买 |
| 标题 | `TitleText` | 220 | 635 | 640 | 60 | 确认购买？ |
| 商品名 | `ItemNameText` | 240 | 730 | 600 | 64 | 商品名 |
| 价格 | `PriceText` | 240 | 815 | 600 | 54 | 价格：0 |
| 倒计时 | `CountdownText` | 430 | 885 | 220 | 54 | 5s |
| 按钮 | `BtnCancel/BtnConfirm` | 230 | 975 | 620 | 82 | 取消 / 确认 |

## 5. 模块设计说明

### 暗化背景

- 对应节点：`ModalBackdrop`
- 建议位置：x=0 y=0 w=1080 h=1920。
- 设计要点：购买确认

### 确认面板

- 对应节点：`ShopConfirmPanel`
- 建议位置：x=170 y=580 w=740 h=520。
- 设计要点：确认购买

### 标题

- 对应节点：`TitleText`
- 建议位置：x=220 y=635 w=640 h=60。
- 设计要点：确认购买？

### 商品名

- 对应节点：`ItemNameText`
- 建议位置：x=240 y=730 w=600 h=64。
- 设计要点：商品名

### 价格

- 对应节点：`PriceText`
- 建议位置：x=240 y=815 w=600 h=54。
- 设计要点：价格：0

### 倒计时

- 对应节点：`CountdownText`
- 建议位置：x=430 y=885 w=220 h=54。
- 设计要点：5s

### 按钮

- 对应节点：`BtnCancel/BtnConfirm`
- 建议位置：x=230 y=975 w=620 h=82。
- 设计要点：取消 / 确认

## 6. 视觉风格

商店内弹窗，深色金属底，确认按钮绿色/金色，倒计时用琥珀色。

建议保持项目统一语言：冰蓝代表常规 HUD，金色代表贡献/VIP/奖励，红橙代表危险或不可逆操作，绿色代表修复/完成。所有文字必须在 1080x1920 竖屏下可读，按钮文字不得溢出。

## 7. AI 效果图场景

购买高价值商品时弹出确认框，倒计时显示 5s。

效果图应把施工原型里的坐标和灰盒理解为布局约束，而不是最终视觉元素。最终图可以加入材质、光效、图标、进度条和真实游戏背景，但不要出现 prefab 名、坐标、虚线框、施工标注等文字。

## 8. 实现注意事项

- 当前 prefab 没有额外特殊实现项；按以下通用规则落地。
- 所有临时层应通过 CanvasGroup 或子节点显隐控制，避免长期遮挡中央 gameplay 区。
- 动态文本需要预留 20%-30% 的宽度余量，中文、数字和昵称都要能截断或滚动。
- 若该界面作为其他主面板的子面板复用，应优先服从主面板坐标，不再另起一套视觉规范。

## 9. 验收标准

- 文件均位于 `docs/ShopConfirmDialogUI_ShopConfirmPanel/`。
- SVG 和 PNG 都是 1080x1920 竖屏参考。
- 效果图能清楚表达 商店购买二次确认 的常用状态。
- 默认文案、节点名称和脚本关系能在文档中追溯到 prefab。
- 若存在嵌套子 prefab，README 和批量索引中明确避免重复生成。