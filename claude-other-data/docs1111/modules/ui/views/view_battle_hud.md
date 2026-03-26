# 战斗HUD — view_battle_hud

> Requires: ui_theme.md
> Canvas: L_BATTLE_HUD (50)
> Priority: P0+
> CachePolicy: Always
> Entry: GameState → battle_start / boss_battle / pvp_battle
> Exit: GameState → battle_end (叠加BattleResult)
> Animation: A_FADEIN 0.2s
> ShowCondition: GameState ∈ {battle_start, wave_1, wave_2, wave_3, next_level, boss_battle, pvp_battle}

---

## 元素表

### TopHUD 区

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| TopHUD | Panel | root | (0,1)→(1,1) | 0,-200,0,0 | 1080×200 | C_BG_OVERLAY_50 渐变 | 顶部信息区 |
| LevelLabel | TMP | TopHUD | (0.5,1) | -300,-50,300,-10 | 600×40 | F_H1 C_TEXT_WHITE 描边#000 | 关卡名居中 |
| WaveLabel | TMP | TopHUD | (0.5,1) | -160,-95,0,-55 | 160×40 | F_H2 C_TITLE | "第 {cur}/{max} 波" |
| TimerValue | TMP | TopHUD | (0.5,1) | 8,-95,180,-55 | 172×40 | F_H2 C_TEXT_WHITE | "MM:SS" |

### BossArea 区

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| BossArea | Panel | root | (0,1)→(1,1) | 0,-320,0,-200 | 1080×120 | C_BG_OVERLAY_50 | Boss血条区(Boss波次可见) |
| BossName | TMP | BossArea | (0.5,1) | -320,-40,320,-8 | 640×32 | F_H2 C_NEGATIVE | Boss名 |
| HpBarBg | Image | BossArea | (0,0)→(1,0) | 20,16,-20,56 | 1040×40 | C_BG_CARD | 血条背景 |
| HpBarFill | Image | BossArea | (0,0)→(1,0) | 22,18,-22,54 | fill×36 | Filled 见BossHpRule | 血量填充 |
| HpSegment | TMP | BossArea | (1,0.5) | -120,0,0,0 | 120×24 | F_SMALL C_TEXT_WHITE | "×{段数}" |
| HpText | TMP | BossArea | (0.5,0) | -200,20,200,52 | 400×32 | F_SMALL C_TEXT_WHITE | "{cur:N0}/{max:N0}" |

### PlayerList 区

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| PlayerList | ScrollRect | root | (0,0)→(1,0) | 0,220,0,340 | 1080×120 | C_BG_OVERLAY_50 水平 | 玩家头像列表 |
| PlayerItem | Template | PlayerList | — | — | 100×110 | gap=8 | 头像模板 |
| PItem/Avatar | Image | PlayerItem | (0.5,1) | 0,0 | 80×80 | Circular mask | 圆形头像 |
| PItem/Name | TMP | PlayerItem | (0.5,0) | 0,4 | 88×22 | F_SMALL C_TEXT | 玩家名(截断) |

### RankingArea 区

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| RankingArea | Panel | root | (1,0.5) | -240,-280,0,280 | 240×560 | C_BG_OVERLAY_50 | 右侧Top5排名 |
| RankItem | Template | RankingArea | — | — | 220×100 | — | 排名项模板 |
| RI/RankNo | TMP | RankItem | (0,0.5) | 8,0 | 36×36 | F_NUM C_TITLE | 排名数字 |
| RI/Name | TMP | RankItem | (0,0.5) | 52,10 | 100×36 | F_SMALL C_TEXT_WHITE | 玩家名 |
| RI/Score | TMP | RankItem | (1,0.5) | -76,0 | 68×30 | F_SMALL C_ATK | 伤害简写 |

### 飘字 区

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| DmgPopupRoot | Panel | root | (0,0)→(1,1) | 0,0,0,0 | full | transparent raycast=off | 飘字对象池容器 |
| Dmg/Normal | TMP | pooled | — | — | auto | F_DMG_NORMAL | 普攻飘字 |
| Dmg/Crit | TMP | pooled | — | — | auto | F_DMG_CRIT | 暴击飘字 |
| Dmg/Skill | TMP | pooled | — | — | auto | F_DMG_SKILL | 技能飘字 |

### BottomStatus 区

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| BottomStatus | Panel | root | (0,0)→(1,0) | 0,120,0,220 | 1080×100 | C_BG_OVERLAY_50 | 底部状态 |
| AutoLabel | TMP | BottomStatus | (0,0.5) | 24,0 | 236×36 | F_H2 C_POSITIVE | "自动战斗中" |

---

## 交互逻辑

```
OnOpen(data):
  LevelLabel.text = data.levelName
  WaveLabel.text = "第 {data.wave}/{data.maxWave} 波"
  TimerValue.text = FormatTime(data.timeLimit)
  BossArea.SetActive(data.hasBoss)
  PlayerList.Refresh(data.players)
  RankingArea.Clear()

OnWaveChange(wave):
  WaveLabel.text = "第 {wave}/{maxWave} 波"
  WaveLabel.Play(A_WAVE_BOUNCE)

OnBossHpChange(cur, max):
  → 见 BossHpRule

OnDamageEvent(target, amount, type):
  → 见 DmgPoolRule

OnTimerTick(seconds):
  TimerValue.text = FormatTime(seconds)
  if seconds <= 10:
    TimerValue.color = C_NEGATIVE
    TimerValue.Play(BlinkLoop 0.5s)

OnRankUpdate(list):
  for i in 0..min(4, list.Count):
    RankItem[i].Refresh(list[i])
```

---

## 状态变化

| 条件 | 表现 |
|------|------|
| 波次切换 | WaveLabel A_WAVE_BOUNCE |
| Boss出场 | BossArea A_SLIDE_UP |
| Boss段数变化 | HpBarFill闪白帧0.1s |
| 倒计时≤10s | TimerValue C_NEGATIVE + 闪烁 |
| 排名变化 | 对应RankItem高亮闪0.3s |

---

## Logic Rules

```
BossHpRule:
  segmentSize = 1_000_000
  segments = ceil(maxHp / segmentSize)
  currentSegment = ceil(curHp / segmentSize)
  fillPercent = (curHp % segmentSize) / segmentSize
  if curHp % segmentSize == 0 && curHp > 0: fillPercent = 1.0
  color = currentSegment % 2 == 1 ? C_NEGATIVE : C_ATK
  HpBarFill.fillAmount = Lerp(HpBarFill.fillAmount, fillPercent, 0.3s)
  HpSegment.text = "×{currentSegment}"
  onSegmentChange → HpBarFill.Flash(白, 0.1s)

DmgPoolRule:
  poolSize = 20 (预创建)
  item = pool.Get(type)  // Normal/Crit/Skill
  item.position = target.worldPos + Random(-30,30), Random(10,30)
  item.text = FormatDamage(amount)
  animate:
    0s: Scale=1.2, Alpha=1
    if Crit: 0~0.15s Scale 1.2→1.4→1.0 (抖动)
    duration: Normal=0.6s, Crit=0.8s, Skill=0.7s
    Scale → 0.6, Y += 80px, Alpha → 0
  onComplete → pool.Return(item)
```

---

## 数据源

| 字段 | 来源 | 刷新频率 |
|------|------|---------|
| 关卡名/波次/时限 | S2C_BattleStart | OnOpen |
| 当前波次 | S2C_WaveChange | 实时事件 |
| 倒计时 | 本地Timer(服务器校准) | 每秒 |
| Boss血量 | S2C_BossHpUpdate | 实时事件 |
| 玩家列表 | S2C_BattleStart.players | OnOpen |
| Top5排名 | S2C_RankUpdate | 每2秒 |
| 伤害事件 | S2C_DamageEvent | 实时事件 |
