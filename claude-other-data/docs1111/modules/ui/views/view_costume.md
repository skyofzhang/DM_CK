# 时装面板 — view_costume

> Requires: ui_theme.md
> Canvas: L_PANEL (22)
> Priority: P2
> CachePolicy: Normal
> Entry: MainLobby[时装]按钮
> Exit: @BackButton → MainLobby
> Animation: A_PANEL_IN / A_PANEL_OUT
> ShowCondition: 始终可用

---

## 元素表

### 容器层

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| BgFull | Image | root | (0,0)→(1,1) | 0,0,0,0 | full | C_BG_DEEP | 背景 |
| TitleBar | @TitleBar | root | — | — | — | — | "时装" |
| BackBtn | @BackButton | TitleBar | — | — | — | — | — |
| PreviewArea | Panel | root | (0,0.6)→(1,1) | 0,-S_TITLEBAR_H,0,0 | 1080×680 | — | 角色预览区 |
| GridArea | Panel | root | (0,0.15)→(1,0.6) | 0,0,0,0 | 1080×864 | — | 时装网格区 |
| BottomArea | Panel | root | (0,0)→(1,0.15) | 0,0,0,0 | 1080×288 | C_BG_PANEL | 属性+按钮区 |

### PreviewArea 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| CharSpine | SkeletonGraphic | PreviewArea | (0.5,0.5) | 0,-40 | 600×600 | — | anim=idle, skin=选中时装 |
| CostumeName | TMP | PreviewArea | (0.5,0) | 0,16 | 400×36 | F_H2 C_SUBTITLE | 时装名称 |
| StarGroup | HLayout | PreviewArea | (0.5,0) | 0,56 | 280×32 | gap=4 | 星级显示(最多8星) |
| Star | Image | StarGroup | — | — | 28×28 | C_TITLE/C_TEXT_DIM | 已点亮/未点亮 |

### GridArea 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| GridScroll | ScrollRect | GridArea | (0,0)→(1,1) | 42,0,-42,0 | 996×864 | vertical | 纵向滚动 |
| GridLayout | GridLayout | GridScroll | — | — | — | cols=5, cell=196×196, gap=4 | 5列网格 |

### GridItem 模板 (×35)

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| ItemBg | Image | GridItem | (0,0)→(1,1) | 2,2,-2,-2 | 192×192 | C_BG_CARD 圆角6 | 卡片底色 |
| ItemIcon | Image | GridItem | (0.5,0.5) | 0,8 | 160×160 | — | 时装缩略图 |
| ItemBorder | @QualityBorder | GridItem | — | — | — | 品质色 | 品质边框 |
| ItemStars | TMP | GridItem | (0.5,0) | 0,6 | 120×20 | F_SMALL C_TITLE | "★×{n}" |
| LockMask | Image | GridItem | (0,0)→(1,1) | 0,0,0,0 | full | C_BG_OVERLAY_50 | 未拥有遮罩 |
| LockIcon | Image | LockMask | (0.5,0.5) | 0,0 | 48×48 | C_TEXT_DIM | 锁图标 |
| SelectedGlow | Image | GridItem | (0,0)→(1,1) | -3,-3,3,3 | — | C_Q_GOLD Alpha=0 | 选中发光 |

### BottomArea 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| AttrCompare | HLayout | BottomArea | (0,1)→(1,1) | 24,-8,-24,-80 | 1032×72 | gap=16 | 属性对比 |
| Attr_HP | Panel | AttrCompare | — | — | 320×60 | — | HP对比 |
| HP/Label | TMP | Attr_HP | (0,0.5) | 0,0 | 60×24 | F_SMALL C_HP | "HP" |
| HP/Cur | TMP | Attr_HP | (0.3,0.5) | 0,0 | 80×28 | F_NUM C_TEXT | 当前值 |
| HP/Arrow | TMP | Attr_HP | (0.6,0.5) | 0,0 | 24×24 | — | "→" |
| HP/New | TMP | Attr_HP | (0.7,0.5) | 0,0 | 80×28 | F_NUM logic=DiffColor | 新值 |
| Attr_ATK | Panel | AttrCompare | — | — | 320×60 | — | ATK对比(同HP结构) |
| Attr_DEF | Panel | AttrCompare | — | — | 320×60 | — | DEF对比(同HP结构) |
| BtnWear | Button | BottomArea | (0.25,0) | 0,24 | 460×88 | F_BTN C_TITLE渐变底 | "穿 戴" |
| BtnStarUp | Button | BottomArea | (0.75,0) | 0,24 | 460×88 | F_BTN C_REALM渐变底 | "升 星" |

---

## 交互逻辑

```
OnOpen(data):
  selectedIdx = player.equippedCostumeIdx
  RefreshGrid(data.costumes)
  SelectCostume(selectedIdx)

OnClick(GridItem[i]):
  if costumes[i].locked → return
  SelectCostume(i)

SelectCostume(idx):
  ClearAllGlow()
  GridItem[idx].SelectedGlow.Alpha = 1 (Loop呼吸)
  CharSpine.SetSkin(costumes[idx].skinName)
  CostumeName.text = costumes[idx].name
  RefreshStars(costumes[idx].star, costumes[idx].maxStar)
  RefreshAttrCompare(player.equipped, costumes[idx])

OnClick(BtnWear):
  if selected == equipped → Toast.Show("已穿戴")
  else → ConfirmDialog.Show("穿戴{name}?", onConfirm=S2C_WearCostume)

OnClick(BtnStarUp):
  if selected.star >= selected.maxStar → Toast.Show("已满星")
  elif costNotMet → Toast.Show("材料不足") + BtnStarUp.Shake(0.2s)
  else → ConfirmDialog.Show("升星消耗...", onConfirm=S2C_StarUpCostume)

OnWearResult(data):
  RefreshGrid() // 更新出战标记
  CharSpine.PlayAnimation("interact", false)

OnStarUpResult(data):
  RefreshStars(data.newStar)
  Star[data.newStar-1].Play(A_WAVE_BOUNCE)
  RefreshAttrCompare()
```

---

## 状态变化

| 条件 | 表现 |
|------|------|
| 选中时装 | SelectedGlow C_Q_GOLD呼吸 + Spine换肤 |
| 未拥有 | LockMask可见 + Alpha=0.4 + 锁图标 |
| 穿戴成功 | Spine播放interact动画 |
| 升星成功 | 对应星★ A_WAVE_BOUNCE |
| 属性提升 | 新值 C_POSITIVE; 降低 C_NEGATIVE |

---

## Logic Rules

```
DiffColor(newVal, curVal):
  if newVal > curVal → C_POSITIVE
  if newVal < curVal → C_NEGATIVE
  else → C_TEXT

CostumeAttrTable (7档):
  | 档位 | HP  | ATK | DEF |
  |  1   | 18  |  3  |  1  |
  |  2   | 35  |  5  |  2  |
  |  3   | 52  |  8  |  3  |
  |  4   | 103 | 15  |  5  |
  |  5   | 155 | 23  |  8  |
  |  6   | 206 | 31  | 10  |
  |  7   | 588 | 89  | 30  |
```

---

## 数据源

| 字段 | 来源 | 刷新频率 |
|------|------|---------|
| 时装列表(35套) | S2C_PlayerInfo.costumes[] | OnOpen |
| 当前穿戴 | S2C_PlayerInfo.equippedCostume | OnOpen/穿戴后 |
| 星级/属性 | CostumeTable[id] + starLevel | 选中切换时 |
| 升星材料 | S2C_PlayerInfo.items | OnOpen/消耗后 |
