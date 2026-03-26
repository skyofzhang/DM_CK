# PVP竞技面板 — view_pvp

> Requires: ui_theme.md
> Canvas: L_PANEL (28)
> Priority: P2
> CachePolicy: Normal
> Entry: MainLobby[PVP]按钮
> Exit: @BackButton → MainLobby / 匹配成功→GameState:pvp_battle
> Animation: A_PANEL_IN / A_PANEL_OUT
> ShowCondition: 始终可用

---

## 元素表

### 容器层

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| BgFull | Image | root | (0,0)→(1,1) | 0,0,0,0 | full | C_BG_DEEP | 背景 |
| TitleBar | @TitleBar | root | — | — | — | — | "PVP竞技" |
| BackBtn | @BackButton | TitleBar | — | — | — | — | — |
| TowerArea | Panel | root | (0,0.45)→(1,1) | 0,-S_TITLEBAR_H,0,0 | 1080×968 | — | 通天塔区 |
| Divider | Image | root | (0,0.45)→(1,0.45) | 40,0,-40,0 | 1000×1 | C_BORDER_TITLE | 分隔线 |
| PvpArea | Panel | root | (0,0)→(1,0.45) | 0,0,0,0 | 1080×864 | — | 实时PVP区 |

### TowerArea 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| TowerIllust | Image | TowerArea | (0.5,0.6) | 0,0 | 480×600 | — | 通天塔插画 |
| TowerGlow | Image | TowerArea | (0.5,0.6) | 0,0 | 520×640 | C_TITLE Alpha=0.08 | 塔底光晕 |
| TowerTitle | TMP | TowerArea | (0.5,1) | 0,-16 | 300×36 | F_H1 C_TITLE | "通天塔" |
| FloorLabel | TMP | TowerArea | (0.5,0.25) | 0,40 | 400×48 | F_H1 C_TEXT_WHITE | "第 {cur} 层" |
| FloorBest | TMP | TowerArea | (0.5,0.25) | 0,0 | 300×28 | F_SMALL C_TEXT_DIM | "历史最高: {best} 层" |
| TowerTime | TMP | TowerArea | (0.5,0.15) | 0,0 | 400×24 | F_SMALL C_TEXT_DIM | "开放时间: 每日19:00-20:00" |
| TowerStatus | TMP | TowerArea | (0.5,0.1) | 0,0 | 300×28 | F_BODY logic=OpenColor | "已开放"/"未开放" |
| BtnTower | Button | TowerArea | (0.5,0) | 0,24 | 480×96 | F_BTN C_TITLE渐变底 | "进入通天塔" |

### PvpArea 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| RankBadge | Image | PvpArea | (0.5,0.75) | 0,0 | 120×120 | — | 段位徽章图标 |
| RankName | TMP | PvpArea | (0.5,0.6) | 0,0 | 300×36 | F_H2 logic=TierColor | 段位名称 |
| RankScore | TMP | PvpArea | (0.5,0.5) | 0,0 | 300×32 | F_NUM C_TEXT_WHITE | "积分: {score}" |
| ProgressBar | Panel | PvpArea | (0.5,0.4) | 0,0 | 600×32 | — | 段位进度条 |
| PB/Bg | Image | ProgressBar | (0,0)→(1,1) | 0,0,0,0 | 600×32 | C_BG_CARD 圆角16 | 进度背景 |
| PB/Fill | Image | ProgressBar | (0,0)→(0,1) | 2,2,-2,-2 | fill×28 | C_REALM渐变 Filled | 当前进度 |
| PB/Text | TMP | ProgressBar | (0.5,0.5) | 0,0 | 200×24 | F_SMALL C_TEXT_WHITE | "{cur}/{next}" |
| PvpTime | TMP | PvpArea | (0.5,0.25) | 0,0 | 400×24 | F_SMALL C_TEXT_DIM | "开放时间: 每日20:00-21:00" |
| PvpStatus | TMP | PvpArea | (0.5,0.2) | 0,0 | 300×28 | F_BODY logic=OpenColor | "已开放"/"未开放" |
| BtnMatch | Button | PvpArea | (0.5,0) | 0,24 | 480×96 | F_BTN C_REALM渐变底 | "匹配对战" |
| MatchingLabel | TMP | PvpArea | (0.5,0) | 0,24 | 480×96 | F_BTN C_TEXT_WHITE Alpha=0 | "匹配中... {sec}s" |

---

## 交互逻辑

```
OnOpen(data):
  RefreshTower(data.tower)
  RefreshPvp(data.pvp)

RefreshTower(tower):
  FloorLabel.text = "第 {tower.currentFloor} 层"
  FloorBest.text = "历史最高: {tower.bestFloor} 层"
  isOpen = CheckTimeWindow(19, 20)
  TowerStatus.text = isOpen ? "已开放" : "未开放"
  BtnTower.interactable = isOpen
  if !isOpen: BtnTower.Alpha = 0.4

RefreshPvp(pvp):
  tier = GetTier(pvp.score)
  RankBadge.sprite = Load(tier.badgePath)
  RankName.text = tier.name
  RankName.color = tier.color
  RankScore.text = "积分: {pvp.score}"
  PB/Fill.fillAmount = (pvp.score - tier.minScore) / (tier.maxScore - tier.minScore)
  PB/Text.text = "{pvp.score}/{tier.maxScore}"
  isOpen = CheckTimeWindow(20, 21)
  PvpStatus.text = isOpen ? "已开放" : "未开放"
  BtnMatch.interactable = isOpen
  if !isOpen: BtnMatch.Alpha = 0.4

OnClick(BtnTower):
  S2C_EnterTower()

OnClick(BtnMatch):
  BtnMatch.SetActive(false)
  MatchingLabel.SetActive(true)
  matchTimer = 0
  S2C_PvpMatch()

OnMatchTick():
  matchTimer += 1
  MatchingLabel.text = "匹配中... {matchTimer}s"

OnMatchResult(data):
  if data.success:
    → GameState = pvp_battle
  else:
    BtnMatch.SetActive(true)
    MatchingLabel.SetActive(false)
    Toast.Show("匹配超时，请重试")
```

---

## 状态变化

| 条件 | 表现 |
|------|------|
| 非开放时间 | 对应按钮Alpha=0.4 + 灰化 |
| 开放中 | 状态文字 C_POSITIVE |
| 匹配中 | BtnMatch隐藏 → MatchingLabel显示(计秒) |
| 匹配成功 | 切换GameState→pvp_battle |
| 段位提升 | RankBadge A_WAVE_BOUNCE + 粒子特效 |

---

## Logic Rules

```
TierTable (10级段位):
  | 等级 | 名称       | 分数范围    | 颜色      |
  |  1   | 青铜       | 0-999      | C_ATK     |
  |  2   | 白银       | 1000-1999  | C_TEXT    |
  |  3   | 黄金       | 2000-2999  | C_TITLE   |
  |  4   | 钻石       | 3000-3999  | C_DEF     |
  |  5   | 铂金       | 4000-4999  | C_DODGE   |
  |  6   | 翡翠       | 5000-5999  | C_POSITIVE|
  |  7   | 紫晶       | 6000-6999  | C_REALM   |
  |  8   | 星耀       | 7000-7999  | C_Q_ORANGE|
  |  9   | 王者       | 8000-8999  | C_NEGATIVE|
  | 10   | 传奇王者   | 9000+      | C_Q_DARK_GOLD |

OpenColor(isOpen):
  isOpen → C_POSITIVE
  !isOpen → C_TEXT_DIM

CheckTimeWindow(startHour, endHour):
  now = ServerTime.hour
  return now >= startHour && now < endHour
```

---

## 数据源

| 字段 | 来源 | 刷新频率 |
|------|------|---------|
| 通天塔当前/最高层 | S2C_PlayerInfo.tower | OnOpen |
| PVP积分/段位 | S2C_PlayerInfo.pvp | OnOpen |
| 开放时间判断 | ServerTime | OnOpen + 每分钟 |
| 匹配结果 | S2C_PvpMatchResult | 实时事件 |
