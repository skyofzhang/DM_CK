# 背包面板 — view_inventory

> Requires: ui_theme.md
> Canvas: L_PANEL (20)
> Priority: P2
> CachePolicy: Normal
> Entry: MainLobby[背包] / 弹幕"查背包"
> Exit: @BackButton → MainLobby / 点击道具 → ItemDetailPopup(L_POPUP)
> Animation: A_PANEL_IN 进入; A_PANEL_OUT 退出
> ShowCondition: NavigationStack.Push触发

---

## 元素表

### 容器层

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| BgFull | Image | root | (0,0)→(1,1) | 0,0,0,0 | full | C_BG_DEEP | 背景 |
| @TitleBar | — | root | — | — | — | Title="背包" | 标题栏+@BackButton |
| TabBar | Panel | root | (0,1)→(1,1) | 0,-228,0,-88 | 1080×140 | C_BG_BAR | Tab栏 |
| ScrollArea | ScrollRect | root | (0,0)→(1,1) | 0,S_SAFE_BOTTOM,0,-228 | fill | Vertical | 道具滚动区 |

### TabBar 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| Tab_Material | Button | TabBar | (0,0)→(0.25,1) | 0,0,0,0 | 270×140 | 选中=C_BG_CARD | [材料] F_BTN |
| Tab_Fragment | Button | TabBar | (0.25,0)→(0.5,1) | 0,0,0,0 | 270×140 | — | [碎片] F_BTN |
| Tab_Consumable | Button | TabBar | (0.5,0)→(0.75,1) | 0,0,0,0 | 270×140 | — | [消耗品] F_BTN |
| Tab_Special | Button | TabBar | (0.75,0)→(1,1) | 0,0,0,0 | 270×140 | — | [特殊] F_BTN |

### ScrollArea → ItemGrid

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| ItemGrid | GridLayout | ScrollArea | (0,1)→(1,1) | 16,-16,-16,0 | fill | cols=5 cell=176×200 gap=12 | 道具网格 |

### ItemCell 模板

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| CellBg | Image | ItemCell | (0,0)→(1,1) | 0,0,0,0 | 176×200 | C_BG_CARD 圆角6px | 格子底 |
| ItemIcon | Image | ItemCell | (0.5,0.6) | -40,-40,40,40 | 80×80 | — | 道具图标 R_ICON_ITEM |
| @QualityBorder | — | ItemCell | — | — | 176×200 | 品质色 | 品质框 |
| Quantity | TMP | ItemCell | (1,0) | -56,4,0,24 | 56×20 | F_SMALL C_TEXT_WHITE align=right | "×99" |
| ItemName | TMP | ItemCell | (0.5,0) | -80,28,80,52 | 160×24 | F_SMALL C_TEXT | 道具名 |

---

## ItemDetailPopup (L_POPUP, 点击道具弹出)

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| Mask | Image | root | (0,0)→(1,1) | 0,0,0,0 | full | C_BG_OVERLAY_50 | 遮罩 |
| DetailCard | Panel | root | (0.5,0.5) | -260,-320,260,320 | 520×640 | C_BG_PANEL 圆角16px | 详情卡 |
| @CloseButton | — | DetailCard | — | — | — | — | 关闭 |
| D_Icon | Image | DetailCard | (0.5,0.85) | -48,-48,48,48 | 96×96 | — | 大图标 |
| D_Name | TMP | DetailCard | (0.5,0.72) | -200,-16,200,16 | 400×32 | F_H2 品质C_Q_* | 道具名 |
| D_Desc | TMP | DetailCard | (0,0.35)→(1,0.68) | 24,0,-24,0 | fill | F_BODY C_TEXT wrap | 描述 |
| D_Source | TMP | DetailCard | (0,0.22)→(1,0.35) | 24,0,-24,0 | fill | F_SMALL C_TEXT_DIM | "来源: xxx" |
| D_Usage | TMP | DetailCard | (0,0.14)→(1,0.22) | 24,0,-24,0 | fill | F_SMALL C_TEXT_DIM | "用途: xxx" |
| BtnUse | Button | DetailCard | (0.5,0) | -120,16,120,72 | 240×56 | C_Q_BLUE 圆角8px | [使用] F_BTN |

---

## 交互逻辑

```
OnClick(Tab_*): 切换分类, ItemGrid刷新
OnClick(ItemCell): ItemDetailPopup.Show(itemData)
OnClick(BtnUse):
  if 可使用 → UseItem(itemId) → Toast(SUCCESS) → 刷新数量
  else → ToastManager.Show("无法使用", FAIL)
OnClick(Mask): DetailPopup.Close()
```

---

## 数据源

| 字段 | 来源 | 刷新频率 |
|------|------|---------|
| 道具列表 | S2C_InventoryList.items[] | OnOpen/Tab切换 |
| 道具详情 | ItemTable + 运行时数量 | 点击时 |
