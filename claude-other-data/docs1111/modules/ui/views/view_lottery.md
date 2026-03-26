# 夺宝面板 — view_lottery

> Requires: ui_theme.md
> Canvas: L_POPUP (30)
> Priority: P1
> CachePolicy: Normal
> Entry: MainLobby[夺宝] / 99币礼物触发
> Exit: @CloseButton → MainLobby
> Animation: A_SLIDE_UP 进入(从下0.4s EaseOutBack); A_PANEL_OUT 退出
> ShowCondition: NavigationStack.Push触发

---

## 元素表

### 容器层

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| BgOverlay | Image | root | (0,0)→(1,1) | 0,0,0,0 | full | C_BG_OVERLAY_60 | 半透明遮罩 |
| Card | Panel | root | (0,0.05)→(1,0.95) | 16,0,-16,0 | 1048×1728 | C_BG_PANEL 圆角16px | 主卡片 |
| @CloseButton | — | Card | — | — | — | — | 关闭按钮 |

### Card 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| Title | TMP | Card | (0.5,1) | -150,-60,150,-20 | 300×40 | F_H1 C_TITLE align=center | "夺宝" |
| WheelRoot | Panel | Card | (0.5,0.55) | -500,-500,500,500 | 1000×1000 | — | 转盘容器(36格奖品) |
| Pointer | Image | Card | (0.5,1) | -30,-30,30,0 | 60×30 | C_NEGATIVE | 顶部指针箭头 |
| GuaranteeBar | Panel | Card | (0,0.12)→(1,0.18) | 24,0,-24,0 | fill | — | 保底进度区 |
| LuckValue | TMP | Card | (0.5,0.08) | -120,-14,120,14 | 240×28 | F_BODY C_TITLE | "幸运值: 2,340" |
| BtnSpin | Button | Card | (0.5,0) | -200,16,200,88 | 400×72 | Gradient C_Q_PURPLE→C_REALM 圆角12px | [开始夺宝] F_BTN |

### GuaranteeBar 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| BarBg | Image | GuaranteeBar | (0,0)→(1,1) | 0,8,0,-8 | fill | C_BG_CARD 圆角4px | 进度底 |
| BarFill | Image | GuaranteeBar | (0,0)→(0,1) | 0,8,0,-8 | fill% | C_TITLE 圆角4px | 填充(width=current/16700) |
| BarText | TMP | GuaranteeBar | (0.5,0.5) | 0,0,0,0 | auto | F_SMALL C_TEXT_WHITE | "{current}/16,700 → 万法归宗" |

---

## 交互逻辑

```
OnClick(BtnSpin):
  if 无99币礼物资格 → ToastManager.Show("需要99币礼物", FAIL)
  else → StartSpin()

StartSpin():
  BtnSpin.interactable = false
  Phase1_Accelerate: 0→2400°/s, 0.5s
  Phase2_Constant:   2400°/s, 1.5s
  Phase3_Decelerate: →目标格, 1.5s EaseOutCubic
  Phase4_Stop:       指针抖动0.3s + 中奖格高亮
  Phase5_Result:     弹出奖品浮层(L_POPUP) → 2s后消失
  BtnSpin.interactable = true
```

---

## 奖品概率

| 类别 | 概率 |
|------|------|
| 碎片/突破石 | 60% |
| 10级宝石 | 20% |
| 仙灵/限定 | 5% |
| 其他 | 15% |

---

## 数据源

| 字段 | 来源 | 刷新频率 |
|------|------|---------|
| 保底进度 | S2C_LotteryInfo.guarantee | OnOpen/抽奖后 |
| 幸运值 | S2C_LotteryInfo.luckValue | OnOpen/抽奖后 |
| 奖品列表 | LuckyDrawTable | 静态 |
