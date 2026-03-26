# 宠物面板 — view_pet

> Requires: ui_theme.md
> Canvas: L_PANEL (20)
> Priority: P1
> CachePolicy: Normal
> Entry: MainLobby[宠物] / 弹幕"查宠物"
> Exit: @BackButton → MainLobby / [装备] → PetEquipPanel(L_PANEL sort=21)
> Animation: A_PANEL_IN 进入; A_PANEL_OUT 退出
> ShowCondition: NavigationStack.Push触发

---

## 元素表

### 容器层

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| BgFull | Image | root | (0,0)→(1,1) | 0,0,0,0 | full | C_BG_DEEP | 深色背景 |
| @TitleBar | — | root | — | — | — | Title="宠物" Right=VipLabel(F_SMALL C_VIP "VIP Lv.X") | 标题栏+@CloseButton |
| SpineDisplay | Panel | root | (0,0.45)→(1,1) | 0,0,0,-88 | 1080×968 | C_BG_PANEL | Spine展示区 |
| PetList | ScrollRect | root | (0,0.05)→(1,0.41) | 10,0,-10,0 | 1060×692 | Horizontal | 横向宠物卡片列表 |
| BtnGroup | Panel | root | (0,0)→(1,0) | 20,16,-20,96 | 1040×80 | — | 4按钮 |

### SpineDisplay 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| PetSpine | SkeletonGraphic | SpineDisplay | (0.15,0.1)→(0.55,1) | 0,0,0,0 | 432×690 | — | 宠物idle R_SPINE_PET |
| PetName | TMP | SpineDisplay | (0.55,0.78)→(1,0.85) | 8,0,-8,0 | fill | F_H1 品质C_Q_* | 宠物名 |
| PetHP | TMP | SpineDisplay | (0.55,0.7) | 8,-14,240,14 | 232×28 | F_H2 C_HP | HP值 |
| PetATK | TMP | SpineDisplay | (0.55,0.62) | 8,-14,240,14 | 232×28 | F_H2 C_ATK | ATK值 |
| EquipSlots | HLayout | SpineDisplay | (0.55,0.45)→(1,0.57) | 0,0,0,0 | 475×120 | spacing=4 | 6格宠物装备槽 |

### PetCard 模板

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| CardBg | Image | PetCard | (0,0)→(1,1) | 0,0,0,0 | 180×260 | C_BG_CARD 圆角8px | 卡片底色 |
| PetIcon | Image | PetCard | (0.5,0.6) | -40,-40,40,40 | 80×80 | — | 宠物头像 R_ICON_PET |
| CardName | TMP | PetCard | (0.5,0) | -80,8,80,36 | 160×28 | F_SMALL 品质C_Q_* | 宠物名 |
| @QualityBorder | — | PetCard | — | — | 180×260 | 品质色 | 品质边框 |
| ActiveMark | Image | PetCard | (0,1) | 4,-56,-62,0 | 116×56 | C_TITLE | "出战" |
| LockMask | Image | PetCard | (0,0)→(1,1) | 0,0,0,0 | full | C_BG_OVERLAY_50 | 未拥有遮罩 |

### 7宠物品质色

| 宠物 | 品质色 |
|------|--------|
| 青鸾 | C_Q_GREEN |
| 玄龟 | C_Q_BLUE |
| 赤蛟 | C_Q_PURPLE |
| 金翅 | C_Q_ORANGE |
| 墨麒麟 | C_Q_RED |
| 白泽 | C_Q_GOLD |
| 烛龙 | C_Q_DARK_GOLD |

### BtnGroup 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| Btn_Upgrade | Button | BtnGroup | (0,0)→(0.25,1) | 2,0,-2,0 | 256×80 | C_POSITIVE 圆角8px | [升级] F_BTN |
| Btn_Advance | Button | BtnGroup | (0.25,0)→(0.5,1) | 2,0,-2,0 | 256×80 | C_Q_PURPLE 圆角8px | [进阶] F_BTN |
| Btn_Equip | Button | BtnGroup | (0.5,0)→(0.75,1) | 2,0,-2,0 | 256×80 | C_Q_BLUE 圆角8px | [装备] F_BTN |
| Btn_Switch | Button | BtnGroup | (0.75,0)→(1,1) | 2,0,-2,0 | 256×80 | C_Q_ORANGE 圆角8px | [切换] F_BTN |

---

## 交互逻辑

```
OnClick(PetCard):
  选中宠物 → PetSpine切换 + PetName/HP/ATK刷新

OnClick(Btn_Upgrade): ConfirmDialog.Show("升级消耗", cost, OnUpgrade)
OnClick(Btn_Advance): ConfirmDialog.Show("进阶材料", materials, OnAdvance)
OnClick(Btn_Equip):   NavigationStack.Push(PetEquipPanel, petId)
OnClick(Btn_Switch):  SetActivePet(petId) → A_WAVE_BOUNCE 0.3s
```

---

## 状态变化

| 条件 | 表现 |
|------|------|
| 选中宠物 | CardBg高亮 + SpineDisplay切换 |
| 出战宠物 | ActiveMark可见, C_TITLE |
| 未拥有 | LockMask覆盖, 按钮灰化 |

---

## 数据源

| 字段 | 来源 | 刷新频率 |
|------|------|---------|
| 宠物列表 | S2C_PetList.pets[] | OnOpen |
| 宠物详情 | S2C_PetDetail | 选中时 |
| 出战ID | S2C_PlayerInfo.activePetId | OnOpen/切换后 |
