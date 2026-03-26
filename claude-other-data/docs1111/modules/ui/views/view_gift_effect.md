# 礼物特效层 — view_gift_effect

> Requires: ui_theme.md
> Canvas: L_GIFT_EFFECT (100)
> Priority: P1
> CachePolicy: Always
> Entry: 收到礼物事件自动触发
> Exit: 特效播放完毕自动消失
> Animation: 按档位不同(见下方)
> ShowCondition: OnGiftReceived事件触发

---

## 元素表

### 容器层

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| Root | Panel | canvas | (0,0)→(1,1) | 0,0,0,0 | full | Raycast=false BlockingObjects=None | 穿透容器 |

### 特效模板(按档位实例化)

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| EffectIcon | Image | Root | — | 动态 | 按档位 | Raycast=false | 礼物图标 |
| Particles | ParticleSystem | Root | — | 动态 | — | Raycast=false | 粒子特效 |
| WhiteFlash | Image | Root | (0,0)→(1,1) | 0,0,0,0 | full | C_TEXT_WHITE Alpha=0 Raycast=false | 全屏闪白 |

---

## 4档特效

| 档位 | 币值 | 图标 | 特效 | 时长 |
|------|------|------|------|------|
| Tier1 | 1 | 60×60 | 图标从右下飘向左上 | 1.5s |
| Tier2 | 10-52 | 100×100 | 图标+4星光粒子散射 | 1.8s |
| Tier3 | 99-199 | 160×160 | 图标+全屏粒子爆散+闪白0.4s | 2.5s |
| Tier4 | 520 | 240×240 | 宝箱天降+3×闪白+震屏+粒子爆炸 | 3.5s |

---

## 交互逻辑

```
OnGiftReceived(giftId, coinValue):
  tier = GetTier(coinValue)
  PlayEffect(tier, giftId)

PlayEffect(tier, giftId):
  icon = LoadGiftIcon(giftId)
  if tier == 1:
    icon(60×60) 右下→左上漂移 1.5s → Destroy
  if tier == 2:
    icon(100×100) 居中弹入 + Particles(4星光) 1.8s → Destroy
  if tier == 3:
    icon(160×160) 居中弹入 + ParticlesBurst + WhiteFlash(Alpha 0→0.8→0 0.4s) → 2.5s Destroy
  if tier == 4:
    // 全屏级表演
    UIManager.SetOtherUIAlpha(0.3)        // 其他UI降透明
    TreasureBox(240×240) 从Y=+500落下 EaseOutBounce 0.6s
    WhiteFlash ×3 (间隔0.4s)
    Camera.Shake(0.3s, intensity=8)
    ParticlesExplosion
    Wait(3.5s) → Destroy
    UIManager.SetOtherUIAlpha(1.0)        // 恢复

GetTier(coinValue):
  if coinValue >= 520 → 4
  if coinValue >= 99  → 3
  if coinValue >= 10  → 2
  else → 1
```

---

## 数据源

| 字段 | 来源 | 刷新频率 |
|------|------|---------|
| 礼物事件 | S2C_GiftEvent | 实时WebSocket推送 |
| 礼物图标 | GiftEffectTable → iconPath | 静态 |
| 币值档位 | GiftEffectTable → coinValue | 静态 |
