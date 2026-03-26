# 装备面板 — view_equipment

> Requires: ui_theme.md
> Canvas: L_PANEL (20)
> Priority: P1
> CachePolicy: Normal
> Entry: MainLobby[装备]
> Exit: @BackButton → MainLobby / 空槽 → EquipSelectList(L_POPUP)
> Animation: A_PANEL_IN 进入; A_PANEL_OUT 退出
> ShowCondition: NavigationStack.Push触发

---

## 元素表

### 容器层

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| BgOverlay | Image | root | (0,0)→(1,1) | 0,0,0,0 | full | C_BG_DEEP | 背景 |
| @TitleBar | — | root | — | — | — | Title="装备" | 标题栏+@BackButton |
| UpperArea | Panel | root | (0,0.5)→(1,1) | 0,0,0,-88 | 1080×872 | — | 上半: 模型+槽位 |
| LowerArea | Panel | root | (0,0)→(1,0.5) | 0,136,0,0 | 1080×824 | C_BG_BAR | 下半: 详情+按钮 |

### UpperArea 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| ModelArea | Panel | UpperArea | (0,0)→(0.42,1) | 0,0,0,0 | 454×872 | — | 角色穿装模型 |
| CharSpine | SkeletonGraphic | ModelArea | (0.5,0.5) | 0,0,0,0 | 400×720 | — | 穿戴装备idle |
| SlotGrid | GridLayout | UpperArea | (0.42,0.15)→(1,0.85) | 20,0,-20,0 | 600×600 | cols=3 cell=184 gap=12 | 3×3装备槽位 |

### SlotItem 模板 (×9)

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| SlotBg | Image | SlotItem | (0,0)→(1,1) | 0,0,0,0 | 184×184 | C_BG_CARD | 槽位背景 |
| QualityBorder | @QualityBorder | SlotItem | — | — | 184×184 | 品质C_Q_* | 品质框 |
| EquipIcon | Image | SlotItem | (0.5,0.5) | 0,0,0,0 | 112×112 | — | 装备图标 R_ICON_EQUIP |
| StarText | TMP | SlotItem | (0.5,0) | -60,4,60,24 | 120×20 | F_SMALL C_TITLE | "★★★☆☆" |

### 9部位: 武器/头盔/项链/衣服/腰带/靴子/护手/护腕/戒指

### LowerArea 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| EquipName | TMP | LowerArea | (0,1) | 16,-52,476,-4 | 460×48 | F_H1 品质C_Q_* | 装备名 |
| AttrList | VertLayout | LowerArea | (0,0.3)→(1,0.85) | 16,0,-16,0 | fill | spacing=4 | 装备属性列表 |
| StarProgress | TMP | LowerArea | (1,1) | -140,-48,-16,-16 | 124×32 | F_BODY C_TITLE | "★ 23/40" |
| WordPanel | Panel | LowerArea | (0,0.15)→(1,0.3) | 16,0,-16,0 | 1048×112 | C_BG_CARD Alpha=50% | 词条区 |
| ActionBtns | Panel | LowerArea | (0,0)→(1,0) | 0,0,0,136 | 1080×136 | — | 4按钮容器 |
| Btn_Enhance | Button | ActionBtns | (0,0)→(0.25,1) | 4,4,-4,-4 | 258×128 | C_Q_ORANGE 圆角8px | [强化] F_BTN |
| Btn_Enchant | Button | ActionBtns | (0.25,0)→(0.5,1) | 4,4,-4,-4 | 258×128 | C_Q_BLUE 圆角8px | [附魔] F_BTN |
| Btn_Devour | Button | ActionBtns | (0.5,0)→(0.75,1) | 4,4,-4,-4 | 258×128 | C_Q_PURPLE 圆角8px | [吞噬] F_BTN |
| Btn_Remove | Button | ActionBtns | (0.75,0)→(1,1) | 4,4,-4,-4 | 258×128 | C_BG_CARD 圆角8px | [卸下] F_BTN C_TEXT_DIM |

---

## 交互逻辑

```
OnClick(SlotItem):
  if 已装备 → 选中高亮, LowerArea显示详情
  if 空槽 → NavigationStack.Push(EquipSelectList, slotType)

OnClick(Btn_Enhance): Push(EnhancePanel, equipId)   // 40星,每星+5%
OnClick(Btn_Enchant): Push(EnchantPanel, equipId)    // 20级附魔
OnClick(Btn_Devour):  Push(DevourSelectList, equipId) // 吞噬选择
OnClick(Btn_Remove):  ConfirmDialog.Show("确认卸下?", ..., OnRemove)
```

---

## 状态变化

| 条件 | 表现 |
|------|------|
| 选中槽位 | 外发光Glow Loop |
| 品质升级 | 闪光特效 0.8s |
| 强化进度条 | 0-10灰 / 11-20 C_Q_GREEN / 21-30 C_Q_BLUE / 31-40 C_TITLE |

---

## 数据源

| 字段 | 来源 | 刷新频率 |
|------|------|---------|
| 9槽装备 | S2C_EquipInfo.slots[] | OnOpen/OnResume |
| 装备属性 | S2C_EquipInfo.detail | 选中时 |
