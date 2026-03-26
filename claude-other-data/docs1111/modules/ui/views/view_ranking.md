# 排行榜面板 — view_ranking

> Requires: ui_theme.md
> Canvas: L_PANEL (26)
> Priority: P2
> CachePolicy: Normal
> Entry: MainLobby[排行]按钮
> Exit: @BackButton → MainLobby
> Animation: A_PANEL_IN / A_PANEL_OUT
> ShowCondition: 始终可用

---

## 元素表

### 容器层

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| BgFull | Image | root | (0,0)→(1,1) | 0,0,0,0 | full | C_BG_DEEP | 背景 |
| TitleBar | @TitleBar | root | — | — | — | — | "排行榜" |
| BackBtn | @BackButton | TitleBar | — | — | — | — | — |
| TabBar | Panel | root | (0,1)→(1,1) | 0,-S_TITLEBAR_H-60,0,-S_TITLEBAR_H | 1080×60 | — | 4Tab栏 |
| Top3Area | Panel | root | (0,1)→(1,1) | 0,-S_TITLEBAR_H-460,0,-S_TITLEBAR_H-60 | 1080×400 | — | 前3名展示区 |
| ListArea | ScrollRect | root | (0,0)→(1,1) | 0,120,0,-S_TITLEBAR_H-460 | fill | vertical | 4-100名列表 |
| SelfBar | Panel | root | (0,0)→(1,0) | 0,0,0,120 | 1080×120 | C_BG_BAR | 自身排名栏 |

### TabBar 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| Tab_Power | Button | TabBar | (0.125,0.5) | 0,0 | 240×52 | F_BTN | "战力" |
| Tab_Contrib | Button | TabBar | (0.375,0.5) | 0,0 | 240×52 | F_BTN | "贡献" |
| Tab_Level | Button | TabBar | (0.625,0.5) | 0,0 | 240×52 | F_BTN | "过关" |
| Tab_PVP | Button | TabBar | (0.875,0.5) | 0,0 | 240×52 | F_BTN | "PVP" |
| TabIndicator | Image | TabBar | — | 动态 | 120×3 | C_TITLE | 选中下划线 |

### Top3Area 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| Rank1Card | Panel | Top3Area | (0.5,0.5) | 0,20 | 320×240 | — | 第1名(居中偏上) |
| R1/Glow | Image | Rank1Card | (0.5,0.5) | 0,0 | 280×280 | C_TITLE Alpha=0.2 | 金色光环粒子r=140 |
| R1/Crown | Image | Rank1Card | (0.5,1) | 0,-8 | 56×48 | — | 皇冠图标 |
| R1/Avatar | Image | Rank1Card | (0.5,0.6) | 0,0 | 96×96 | Circular mask | 头像 |
| R1/Name | TMP | Rank1Card | (0.5,0.3) | 0,0 | 240×28 | F_BODY C_TITLE | 昵称 |
| R1/Value | TMP | Rank1Card | (0.5,0.1) | 0,0 | 200×32 | F_NUM C_TEXT_WHITE | 数值 |
| Rank2Card | Panel | Top3Area | (0.2,0.4) | 0,0 | 280×200 | — | 第2名(左侧) |
| R2/Glow | Image | Rank2Card | (0.5,0.5) | 0,0 | 240×240 | C_TEXT Alpha=0.15 | 银色光环r=120 |
| R2/Avatar | Image | Rank2Card | (0.5,0.65) | 0,0 | 80×80 | Circular mask | 头像 |
| R2/Name | TMP | Rank2Card | (0.5,0.3) | 0,0 | 200×26 | F_BODY C_TEXT | 昵称 |
| R2/Value | TMP | Rank2Card | (0.5,0.1) | 0,0 | 160×28 | F_NUM C_TEXT_WHITE | 数值 |
| Rank3Card | Panel | Top3Area | (0.8,0.4) | 0,0 | 280×200 | — | 第3名(右侧) |
| R3/Glow | Image | Rank3Card | (0.5,0.5) | 0,0 | 240×240 | C_ATK Alpha=0.15 | 铜色光环r=120 |
| R3/Avatar | Image | Rank3Card | (0.5,0.65) | 0,0 | 80×80 | Circular mask | 头像 |
| R3/Name | TMP | Rank3Card | (0.5,0.3) | 0,0 | 200×26 | F_BODY C_TEXT | 昵称 |
| R3/Value | TMP | Rank3Card | (0.5,0.1) | 0,0 | 160×28 | F_NUM C_TEXT_WHITE | 数值 |

### ListItem 模板 (4-100名)

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| ListItem | Panel | ListArea | (0,0)→(1,0) | 0,0,0,0 | 1080×80 | 偶数行C_BG_CARD | 排名行 |
| LI/RankNo | TMP | ListItem | (0,0.5) | 24,0 | 48×36 | F_NUM logic=RankColor | 排名 |
| LI/Avatar | Image | ListItem | (0,0.5) | 80,0 | 60×60 | Circular mask | 头像 |
| LI/Name | TMP | ListItem | (0,0.5) | 152,0 | 300×28 | F_BODY C_TEXT_WHITE | 昵称 |
| LI/Value | TMP | ListItem | (1,0.5) | -24,0 | 200×28 | F_NUM C_ATK align=right | 数值 |

### SelfBar 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| SelfBg | Image | SelfBar | (0,0)→(1,1) | 4,4,-4,-4 | fill | C_BG_CARD 描边2px C_TITLE | 金色描边背景 |
| Self/RankNo | TMP | SelfBar | (0,0.5) | 24,0 | 48×36 | F_NUM C_TITLE | 我的排名 |
| Self/Avatar | Image | SelfBar | (0,0.5) | 80,0 | 60×60 | Circular mask | 我的头像 |
| Self/Name | TMP | SelfBar | (0,0.5) | 152,0 | 300×28 | F_BODY C_TITLE | 我的昵称 |
| Self/Value | TMP | SelfBar | (1,0.5) | -24,0 | 200×28 | F_NUM C_TITLE align=right | 我的数值 |

---

## 交互逻辑

```
OnOpen(data):
  currentTab = 0
  SwitchTab(0)

OnClick(Tab_X):
  SwitchTab(X.index)

SwitchTab(idx):
  TabIndicator.LerpTo(Tab[idx].x, 0.2s)
  foreach tab: tab.color = C_TEXT_DIM
  Tab[idx].color = C_TITLE
  RequestRanking(idx)

OnRankingData(data):
  RefreshTop3(data.top3)
  RefreshList(data.list)  // 4-100
  RefreshSelf(data.selfRank)

RefreshTop3(top3):
  for i in 0..2:
    card = [Rank1Card, Rank2Card, Rank3Card][i]
    if i < top3.Count:
      card.SetActive(true)
      card.Avatar = LoadAvatar(top3[i].avatarUrl)
      card.Name.text = top3[i].name
      card.Value.text = FormatValue(top3[i].value)
    else:
      card.SetActive(false)
```

---

## 状态变化

| 条件 | 表现 |
|------|------|
| 切换Tab | TabIndicator Lerp + 数据刷新 |
| 自身在Top3 | SelfBar额外高亮闪烁 |
| 数据加载中 | 列表显示Loading动画 |

---

## Logic Rules

```
RankColor(rank):
  if rank <= 3 → C_TITLE
  if rank <= 10 → C_Q_PURPLE
  else → C_TEXT_WHITE

GlowRule:
  第1名: C_TITLE, 粒子半径140px
  前5名: C_REALM, 粒子半径120px
  前10名: C_DEF, 粒子半径100px
  前100名: C_POSITIVE, 粒子半径80px
```

---

## 数据源

| 字段 | 来源 | 刷新频率 |
|------|------|---------|
| 排行榜数据(Top100) | S2C_RankingList | OnOpen/切Tab |
| 自身排名 | S2C_RankingList.selfRank | 随榜单一起 |
| 玩家头像 | AvatarLoader缓存 | 按需加载 |
