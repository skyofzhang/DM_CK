# 战斗结算面板 — view_battle_result

> Requires: ui_theme.md
> Canvas: L_BATTLE_HUD (50)
> Priority: P1
> CachePolicy: Disposable
> Entry: GameState → battle_end / pvp_settlement / boss_settlement
> Exit: [进入下一关] → next_level / [返回大厅] → idle / 5s自动进入下一关
> Animation: A_FADEIN 0.3s + stagger子元素滑入(见StaggerRule)
> ShowCondition: GameState ∈ {battle_end, reward_settlement, pvp_settlement, boss_settlement}

---

## 元素表

### 容器层

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| Overlay | Image | root | (0,0)→(1,1) | 0,0,0,0 | full | C_BG_OVERLAY_75 | 半透明遮罩 |
| ResultCard | Panel | root | (0.5,0.5) | -480,-760,480,760 | 960×1520 | C_BG_PANEL 圆角20 | 结算卡片 |

### 标题区 (stagger 0.0s)

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| TitleLabel | TMP | ResultCard | (0.5,1) | 0,-48 | 600×56 | F_H1 logic=ResultColor | "战斗胜利!" / "战斗失败" |
| TitleGlow | Image | ResultCard | (0.5,1) | 0,-48 | 700×80 | Alpha=0.15 logic=ResultColor | 标题光晕 |

### MVP区 (stagger 0.3s)

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| MvpArea | Panel | ResultCard | (0,1)→(1,1) | 24,-120,-24,-280 | 912×160 | C_BG_CARD 圆角8 | MVP展示 |
| MvpBadge | TMP | MvpArea | (0,1) | 16,-8 | 80×24 | F_SMALL C_TITLE | "MVP" |
| MvpAvatar | Image | MvpArea | (0,0.5) | 24,0 | 96×96 | Circular mask | MVP头像 |
| MvpName | TMP | MvpArea | (0,0.5) | 136,16 | 300×32 | F_H2 C_TITLE | MVP昵称 |
| MvpDamage | TMP | MvpArea | (0,0.5) | 136,-16 | 300×28 | F_NUM C_ATK | 总伤害量 |
| MvpMulti | TMP | MvpArea | (1,0.5) | -24,0 | 120×36 | F_H2 C_TITLE | "×1.5" |

### 奖励区 (stagger 0.6s)

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| RewardArea | Panel | ResultCard | (0,1)→(1,1) | 24,-296,-24,-520 | 912×224 | — | 奖励展示 |
| RewardTitle | TMP | RewardArea | (0,1) | 0,-8 | 200×28 | F_H2 C_SUBTITLE | "获得奖励" |
| RewardGrid | HLayout | RewardArea | (0.5,0.5) | 0,-8 | 800×140 | gap=24 | 奖励物品行 |

### RewardItem 模板

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| RwdIcon | Image | RewardItem | (0.5,0.6) | 0,0 | 80×80 | @QualityBorder | 物品图标 |
| RwdName | TMP | RewardItem | (0.5,0) | 0,4 | 100×22 | F_SMALL C_TEXT | 物品名 |
| RwdCount | TMP | RewardItem | (1,1) | -4,-4 | 48×20 | F_SMALL C_TEXT_WHITE | "×{n}" |

### 排名区 (stagger 0.9s)

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| RankArea | Panel | ResultCard | (0,1)→(1,1) | 24,-536,-24,-940 | 912×404 | — | 前5名排名 |
| RankTitle | TMP | RankArea | (0,1) | 0,-8 | 200×28 | F_H2 C_SUBTITLE | "伤害排名" |
| RankList | VLayout | RankArea | (0,0)→(1,1) | 0,0,0,-40 | fill | gap=4 | 排名列表 |

### RankRow 模板 (×5)

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| RR/RankNo | TMP | RankRow | (0,0.5) | 8,0 | 36×32 | F_NUM logic=RankColor | 排名 |
| RR/Avatar | Image | RankRow | (0,0.5) | 52,0 | 52×52 | Circular mask | 头像 |
| RR/Name | TMP | RankRow | (0,0.5) | 116,8 | 280×26 | F_BODY C_TEXT_WHITE | 昵称 |
| RR/Damage | TMP | RankRow | (0,0.5) | 116,-10 | 200×22 | F_SMALL C_ATK | 伤害量 |
| RR/Multi | TMP | RankRow | (1,0.5) | -16,0 | 80×28 | F_NUM logic=MultiColor | "×{n}" |

### 按钮区 (stagger 1.2s)

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| BtnArea | HLayout | ResultCard | (0.5,0) | 0,40 | 800×88 | gap=32 | 底部按钮组 |
| BtnNext | Button | BtnArea | — | — | 380×88 | F_BTN C_POSITIVE渐变底 | "进入下一关" |
| BtnLobby | Button | BtnArea | — | — | 380×88 | F_BTN C_BG_CARD | "返回大厅" |
| AutoTimer | TMP | ResultCard | (0.5,0) | 0,136 | 300×24 | F_SMALL C_TEXT_DIM | "自动继续 {n}s" |

---

## 交互逻辑

```
OnOpen(data):
  isVictory = data.result == "victory"
  TitleLabel.text = isVictory ? "战斗胜利!" : "战斗失败"
  TitleLabel.color = isVictory ? C_TITLE : C_TEXT_DIM
  TitleGlow.color = isVictory ? C_TITLE : C_TEXT_DIM

  RefreshMvp(data.mvp)
  RefreshRewards(data.rewards)
  RefreshRanking(data.rankings)

  PlayStagger()
  StartAutoTimer(5)

  if !isVictory:
    BtnNext.text = "重新挑战"

PlayStagger():
  TitleLabel.Play(A_SLIDE_UP, delay=0.0s)
  MvpArea.Play(A_FADEIN, delay=0.3s)
  RewardArea.Play(A_SLIDE_UP, delay=0.6s)
  RankArea.Play(A_FADEIN, delay=0.9s)
  BtnArea.Play(A_FADEIN, delay=1.2s)
  AutoTimer.Play(A_FADEIN, delay=1.2s)

StartAutoTimer(seconds):
  autoCountdown = seconds
  each 1s:
    autoCountdown -= 1
    AutoTimer.text = "自动继续 {autoCountdown}s"
    if autoCountdown <= 0 → OnClick(BtnNext)

OnClick(BtnNext):
  StopAutoTimer()
  if isVictory:
    GameState = next_level
  else:
    GameState = battle_start (重新挑战)

OnClick(BtnLobby):
  StopAutoTimer()
  GameState = idle

RefreshRanking(rankings):
  for i in 0..min(4, rankings.Count):
    row = RankRow[i]
    row.RankNo.text = i + 1
    row.Name.text = rankings[i].name
    row.Damage.text = FormatDamage(rankings[i].damage)
    row.Multi.text = "×" + GetMultiplier(i)
```

---

## 状态变化

| 条件 | 表现 |
|------|------|
| 胜利 | 标题C_TITLE金色 + 光晕 |
| 失败 | 标题C_TEXT_DIM灰色 |
| stagger入场 | 5段延迟动画序列(0→0.3→0.6→0.9→1.2s) |
| 自动倒计时归零 | 自动执行BtnNext逻辑 |
| 奖励品质不同 | @QualityBorder颜色匹配品质 |

---

## Logic Rules

```
ResultColor(isVictory):
  victory → C_TITLE
  defeat → C_TEXT_DIM

MultiplierTable:
  | 排名 | 倍率 |
  |  1   | 1.5x |
  |  2   | 1.3x |
  |  3   | 1.2x |
  | 4+   | 1.0x |

MultiColor(rank):
  rank == 1 → C_TITLE
  rank <= 3 → C_SUBTITLE
  else → C_TEXT

RankColor(rank):
  rank == 1 → C_TITLE
  rank == 2 → C_TEXT
  rank == 3 → C_ATK
  else → C_TEXT_WHITE
```

---

## 数据源

| 字段 | 来源 | 刷新频率 |
|------|------|---------|
| 胜负结果 | S2C_BattleEnd.result | OnOpen |
| MVP信息 | S2C_BattleEnd.mvp | OnOpen |
| 奖励列表 | S2C_BattleEnd.rewards[] | OnOpen |
| 排名列表 | S2C_BattleEnd.rankings[] | OnOpen |
| 自动倒计时 | 本地Timer | 每秒 |
