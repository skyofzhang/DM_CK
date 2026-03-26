# 宝石面板 — view_gem

> Requires: ui_theme.md
> Canvas: L_PANEL (20)
> Priority: P1
> CachePolicy: Normal
> Entry: MainLobby[宝石]按钮
> Exit: @BackButton → MainLobby
> Animation: A_PANEL_IN / A_PANEL_OUT
> ShowCondition: 玩家已解锁宝石系统(realm >= 2)

---

## 元素表

### 容器层

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| BgFull | Image | root | (0,0)→(1,1) | 0,0,0,0 | full | C_BG_DEEP | 深蓝黑背景 |
| TitleBar | @TitleBar | root | — | — | — | — | "宝石" |
| BackBtn | @BackButton | TitleBar | — | — | — | — | — |
| GemOrbitArea | Panel | root | (0,0.3)→(1,1) | 0,-S_TITLEBAR_H,0,0 | 1080×1144 | — | 宝石轮盘区 |
| DetailArea | Panel | root | (0,0)→(1,0.3) | 0,0,0,0 | 1080×576 | C_BG_PANEL | 详情区 |

### GemOrbitArea 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| CharSilhouette | Image | GemOrbitArea | (0.5,0.5) | 0,0 | 480×640 | Alpha=0.12 C_TEXT_WHITE | 角色轮廓剪影 |
| OrbitRing | Image | GemOrbitArea | (0.5,0.5) | 0,0 | 640×640 | Alpha=0.06 C_TEXT_WHITE | 轨道环形虚线 |
| Gem_Chiyan | Panel | GemOrbitArea | (0.5,0.5) | 0,+280 | 120×140 | — | 赤炎石(0°/12点) |
| Gem_Pofeng | Panel | GemOrbitArea | (0.5,0.5) | +242,+140 | 120×140 | — | 破锋石(60°/2点) |
| Gem_Xuantie | Panel | GemOrbitArea | (0.5,0.5) | +242,-140 | 120×140 | — | 玄铁石(120°/4点) |
| Gem_Shixue | Panel | GemOrbitArea | (0.5,0.5) | 0,-280 | 120×140 | — | 噬血石(180°/6点) |
| Gem_Huanying | Panel | GemOrbitArea | (0.5,0.5) | -242,-140 | 120×140 | — | 幻影石(240°/8点) |
| Gem_Lietian | Panel | GemOrbitArea | (0.5,0.5) | -242,+140 | 120×140 | — | 裂天石(300°/10点) |

### GemSlot 模板 (×6)

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| GemIcon | Image | Gem_X | (0.5,1) | 0,-8 | 80×96 | C_GEM_* 六边形 | 宝石图标 |
| GemLevel | TMP | Gem_X | (0.5,0) | 0,4 | 60×28 | F_SMALL C_TITLE | "Lv.{n}" |
| GemExpBar | Image | Gem_X | (0,0)→(1,0) | 4,0,-4,20 | 112×20 | Filled C_GEM_* | 经验条 |
| GemGlow | Image | Gem_X | (0.5,0.5) | 0,0 | 140×160 | Alpha=0 | 选中呼吸发光 |

### DetailArea 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| GemName | TMP | DetailArea | (0,1) | 40,-60 | 580×48 | F_H1 C_GEM_*(动态) | 选中宝石名 |
| GemAttr | TMP | DetailArea | (0,1) | 40,-100 | 300×28 | F_BODY C_TEXT | "属性: {attrName}" |
| CurEffect | Panel | DetailArea | (0,1)→(0.5,1) | 24,-140,-8,-260 | 516×120 | C_BG_CARD | 当前效果 |
| Cur/Label | TMP | CurEffect | (0,1) | 12,-8 | 100×24 | F_SMALL C_TEXT_DIM | "当前" |
| Cur/Value | TMP | CurEffect | (0.5,0.5) | 0,0 | 200×36 | F_NUM C_TEXT_WHITE | "+{value}" |
| NextEffect | Panel | DetailArea | (0.5,1)→(1,1) | 8,-140,-24,-260 | 516×120 | C_BG_CARD | 下一级效果 |
| Next/Label | TMP | NextEffect | (0,1) | 12,-8 | 100×24 | F_SMALL C_POSITIVE | "下一级" |
| Next/Value | TMP | NextEffect | (0.5,0.5) | 0,0 | 200×36 | F_NUM C_POSITIVE | "+{value}" |
| CostArea | Panel | DetailArea | (0,0)→(1,0) | 24,120,-24,220 | 1032×100 | — | 材料消耗区 |
| Cost/Icon | Image | CostArea | (0,0.5) | 16,0 | 48×48 | — | 材料图标 |
| Cost/Have | TMP | CostArea | (0,0.5) | 72,0 | 200×28 | F_BODY logic=CostColor | "{have}/{need}" |
| BtnUpgrade | Button | DetailArea | (0.5,0) | 0,32 | 560×96 | F_BTN C_GEM_*(渐变底) | "升 级" |

---

## 交互逻辑

```
OnOpen(data):
  selectedGem = GemType.Chiyan  // 默认赤炎
  RefreshAllSlots(data.gems)
  SelectGem(selectedGem)

OnClick(Gem_X):
  selectedGem = X
  SelectGem(X)

SelectGem(type):
  foreach slot: slot.GemGlow.Stop(); slot.GemGlow.Alpha=0
  selected.GemGlow.Play(BreathLoop, 0→0.6→0, 1.5s)
  GemName.text = type.displayName
  GemName.color = C_GEM_{type}
  RefreshDetail(player.gems[type])

OnClick(BtnUpgrade):
  if costMet → S2C_GemUpgrade(selectedGem)
  else → Toast.Show("材料不足") + BtnUpgrade.Shake(0.2s)

OnLongPress(Gem_X, 0.5s):
  Popup.Show(GemGrowthTable, {type: X})

OnGemUpgradeResult(data):
  RefreshSlot(data.type, data.newLevel, data.newExp)
  GemLevel.Play(A_WAVE_BOUNCE)
  GemExpBar.AnimateFill(0.4s)
  if data.newLevel == maxLevel:
    GemLevel.text = "MAX"
    BtnUpgrade.interactable = false
    BtnUpgrade.Alpha = 0.4
```

---

## 状态变化

| 条件 | 表现 |
|------|------|
| 选中宝石 | GemGlow呼吸发光Loop |
| 升级成功 | GemLevel A_WAVE_BOUNCE + ExpBar填充0.4s |
| 满级 | "MAX"标签 + BtnUpgrade灰化 |
| 材料不足 | Toast + 按钮抖动0.2s |

---

## 数据源

| 字段 | 来源 | 刷新频率 |
|------|------|---------|
| 6颗宝石等级/经验 | S2C_PlayerInfo.gems[] | OnOpen/升级后 |
| 当前/下一级效果 | GemTable[type][level] | 选中切换时 |
| 材料持有量 | S2C_PlayerInfo.items | OnOpen/消耗后 |
| 升级消耗 | GemCostTable[type][level] | 选中切换时 |
