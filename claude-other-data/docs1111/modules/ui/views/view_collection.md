# 图鉴面板 — view_collection

> Requires: ui_theme.md
> Canvas: L_PANEL (20)
> Priority: P3
> CachePolicy: Disposable
> Entry: MainLobby[图鉴]
> Exit: @BackButton → MainLobby
> Animation: A_PANEL_IN 进入; A_PANEL_OUT 退出
> ShowCondition: NavigationStack.Push触发

---

## 元素表

### 容器层

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| BgFull | Image | root | (0,0)→(1,1) | 0,0,0,0 | full | C_BG_DEEP | 背景 |
| @TitleBar | — | root | — | — | — | Title="图鉴" | 标题栏+@BackButton |
| ProgressArea | Panel | root | (0,1)→(1,1) | 16,-240,16,-100 | 1048×140 | — | 总进度区 |
| TabBar | Panel | root | (0,1)→(1,1) | 0,-380,0,-240 | 1080×140 | C_BG_BAR | 分类Tab |
| ScrollArea | ScrollRect | root | (0,0)→(1,1) | 0,140,0,-380 | fill | Vertical | Boss网格滚动 |
| MilestoneBar | Panel | root | (0,0)→(1,0) | 0,S_SAFE_BOTTOM,0,140 | 1080×100 | C_BG_OVERLAY_50 | 里程碑奖励 |

### ProgressArea 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| ProgressBg | Image | ProgressArea | (0,0.3)→(1,0.7) | 0,0,0,0 | fill | C_BG_CARD 圆角8px | 进度条底 |
| ProgressFill | Image | ProgressArea | (0,0.3)→(0,0.7) | 0,0,0,0 | fill% | C_TITLE 圆角8px | 填充 |
| ProgressText | TMP | ProgressArea | (0.5,0.5) | 0,0,0,0 | auto | F_H2 C_TEXT_WHITE | "收集: 23/78 (29%)" |

### TabBar 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| Tab_Lingshou | Button | TabBar | (0,0)→(0.25,1) | 0,0,0,0 | 270×140 | 选中=C_BG_CARD | [灵兽秘境] F_BTN |
| Tab_Xianling | Button | TabBar | (0.25,0)→(0.5,1) | 0,0,0,0 | 270×140 | — | [仙灵幻域] F_BTN |
| Tab_Yuyi | Button | TabBar | (0.5,0)→(0.75,1) | 0,0,0,0 | 270×140 | — | [羽翼试炼] F_BTN |
| Tab_Shenbing | Button | TabBar | (0.75,0)→(1,1) | 0,0,0,0 | 270×140 | — | [神兵遗冢] F_BTN |

### BossGrid

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| BossGrid | GridLayout | ScrollArea | (0,1)→(1,1) | 16,-16,-16,0 | fill | cols=4 cell=244×280 gap=12 | Boss网格 |

### BossCell 模板

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| CellBg | Image | BossCell | (0,0)→(1,1) | 0,0,0,0 | 244×280 | C_BG_CARD 圆角8px | 格子底 |
| BossIcon | Image | BossCell | (0.5,0.6) | -50,-50,50,50 | 100×100 | 已收集=全彩/未收集=灰度 | Boss头像 |
| BossName | TMP | BossCell | (0.5,0) | -110,8,110,36 | 220×28 | F_SMALL C_TEXT | Boss名 |
| CheckMark | Image | BossCell | (1,1) | -36,-36,0,0 | 36×36 | C_POSITIVE | ✓已收集 |
| LockIcon | Image | BossCell | (0.5,0.6) | -24,-24,24,24 | 48×48 | C_TEXT_DIM | 🔒未收集 |

---

## 交互逻辑

```
OnClick(Tab_*): 切换分类, BossGrid刷新, ProgressText更新
OnClick(BossCell):
  if 已收集 → 弹出Boss详情浮层(名称+描述+首杀记录)
  if 未收集 → ToastManager.Show("尚未击败", INFO)
```

---

## 数据源

| 字段 | 来源 | 刷新频率 |
|------|------|---------|
| 收集列表 | S2C_CollectionList.bosses[] | OnOpen |
| Boss信息 | MonsterTable | 静态 |
| 总进度 | 本地计算(已收集/总数) | OnOpen/Tab切换 |
