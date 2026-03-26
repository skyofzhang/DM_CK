# 角色面板 — view_character

> Requires: ui_theme.md
> Canvas: L_PANEL (20)
> Priority: P1
> CachePolicy: Normal
> Entry: MainLobby[角色] / 弹幕"查角色"
> Exit: @BackButton → MainLobby / Tab[时装] → Costume
> Animation: A_PANEL_IN 进入; A_PANEL_OUT 退出
> ShowCondition: NavigationStack.Push触发

---

## 元素表

### 容器层

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| BgOverlay | Image | root | (0,0)→(1,1) | 0,0,0,0 | full | C_BG_DEEP | 深色背景 |
| @TitleBar | — | root | — | — | — | Title="角色" Right=PowerValue(F_H2 C_POWER) | 标题栏+@BackButton+战力 |
| LeftArea | Panel | root | (0,0)→(0.45,1) | 0,140,0,-88 | 486×1692 | — | 角色模型区 |
| RightArea | Panel | root | (0.45,0)→(1,1) | 0,140,0,-88 | 594×1692 | — | 属性区 |
| BottomTab | Panel | root | (0,0)→(1,0) | 0,0,0,140 | 1080×140 | C_BG_BAR | 底部Tab栏 |

### LeftArea 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| CharSpine | SkeletonGraphic | LeftArea | (0.5,0.5) | 0,-60,0,0 | 440×800 | — | 角色idle, 可拖拽旋转 |
| RealmBadge | Panel | LeftArea | (0.5,0) | -200,180,200,244 | 400×64 | C_REALM Alpha=25% 圆角8px | 境界名容器 |
| RealmName | TMP | RealmBadge | (0.5,0.5) | 0,0,0,0 | 280×36 | F_H2 C_REALM align=center | "金丹期·中期" |
| ProgressBar | Image | LeftArea | (0.5,0) | -223,120,223,152 | 446×32 | C_REALM 圆角4px | 进度填充(Fill) |
| ProgressText | TMP | LeftArea | (0.5,0) | -120,88,120,114 | 240×26 | F_SMALL C_TEXT Alpha=80% | "45,230/100,000" |
| BreakBtn | Button | LeftArea | (0.5,0) | -160,8,160,80 | 320×72 | Gradient C_TITLE→C_Q_ORANGE 圆角12px | [突破] F_BTN |

### RightArea 子元素 — 8个属性条

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| AttrList | VertLayout | RightArea | (0,0.3)→(1,0.95) | 8,0,-8,0 | fill | spacing=8 | 8个属性条容器 |
| AttrItem | Panel | AttrList | — | — | 578×72 | — | 单属性条模板 ×8 |
| AttrItem/Icon | Image | AttrItem | (0,0.5) | 8,-22,52,22 | 44×44 | 属性色(C_HP/C_ATK等) | 属性图标 |
| AttrItem/Name | TMP | AttrItem | (0,0.5) | 60,-14,176,14 | 116×28 | F_BODY C_TEXT | "生命值" |
| AttrItem/Base | TMP | AttrItem | (0,0.5) | 180,-14,312,14 | 132×28 | F_BODY C_TEXT_WHITE | 基础值 |
| AttrItem/Add | TMP | AttrItem | (0,0.5) | 316,-14,432,14 | 116×28 | F_BODY C_POSITIVE | "+加成值" |
| AttrItem/Total | TMP | AttrItem | (1,0.5) | -140,-16,0,16 | 140×32 | F_NUM C_TITLE | "=总值" |

### 8属性: HP/ATK/DEF/暴击/闪避/吸血/命中/免伤

### BottomTab 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| Tab_Attr | Button | BottomTab | (0,0)→(0.333,1) | 0,0,0,0 | 360×140 | 选中态=C_BG_CARD | [属性] F_BTN |
| Tab_Costume | Button | BottomTab | (0.333,0)→(0.667,1) | 0,0,0,0 | 360×140 | — | [时装] F_BTN |
| Tab_Title | Button | BottomTab | (0.667,0)→(1,1) | 0,0,0,0 | 360×140 | — | [称号] F_BTN |

---

## 交互逻辑

```
OnClick(CharSpine):
  拖拽 → 角色Y轴旋转

OnClick(BreakBtn):
  if 材料足够 → ConfirmDialog.Show("突破确认", cost, OnBreak)
  else → ToastManager.Show("突破材料不足", FAIL)

OnClick(Tab_Costume): NavigationStack.Replace(Costume)
OnClick(Tab_Title): NavigationStack.Replace(Title)
OnClick(AttrItem): 弹出属性来源拆分浮层(L_POPUP)
```

---

## 状态变化

| 条件 | 表现 |
|------|------|
| 面板打开 | PowerValue A_NUM_ROLL 1.2s |
| 面板打开 | ProgressBar 从0填充至当前 0.8s |
| Add > 0 | C_POSITIVE; Add == 0 → C_TEXT_DIM |
| 材料足够 | BreakBtn Gradient金色; 不足 → C_TEXT_DIM 灰化 |

---

## 数据源

| 字段 | 来源 | 刷新频率 |
|------|------|---------|
| 战力 | S2C_PlayerInfo.power | OnOpen |
| 境界 | S2C_PlayerInfo.realm | OnOpen |
| 8属性 | S2C_PlayerInfo.attrs[] | OnOpen/OnResume |
| 进度 | S2C_PlayerInfo.realmExp | OnOpen |
