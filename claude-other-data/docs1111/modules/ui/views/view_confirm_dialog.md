# 确认弹窗 — view_confirm_dialog

> Requires: ui_theme.md
> Canvas: L_POPUP (30)
> Priority: P0
> CachePolicy: Normal
> Entry: 任何消耗/危险操作触发 ConfirmDialog.Show()
> Exit: [确认] → onConfirm回调 / [取消]/遮罩点击 → onCancel回调
> Animation: A_POPUP_IN 进入; A_POPUP_OUT 退出
> ShowCondition: API调用触发

---

## 元素表

### 容器层

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| Mask | Image | root | (0,0)→(1,1) | 0,0,0,0 | full | C_BG_OVERLAY_60 | 半透明遮罩(可点击关闭) |
| Card | Panel | root | (0.5,0.5) | -360,-200,360,200 | 720×400 | C_BG_PANEL 圆角20px | 弹窗卡片 |

### Card 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| Title | TMP | Card | (0.5,1) | -300,-60,300,-20 | 600×40 | F_H2 C_TITLE align=center | 弹窗标题 |
| Content | TMP | Card | (0,0.3)→(1,0.8) | 40,0,-40,0 | fill | F_BODY C_TEXT align=center wrap | 弹窗内容 |
| BtnConfirm | Button | Card | (0.5,0) | -300,24,-20,96 | 280×72 | Gradient C_Q_PURPLE→C_REALM 圆角12px | [确认] F_BTN |
| BtnCancel | Button | Card | (0.5,0) | 20,24,300,96 | 280×72 | C_BG_CARD 圆角12px | [取消] F_BTN C_TEXT_DIM |

---

## 交互逻辑

```
API: ConfirmDialog.Show(title, content, onConfirm, onCancel)

OnClick(Mask):
  onCancel?.Invoke()
  Close()

OnClick(BtnConfirm):
  A_BTN_PRESS → onConfirm?.Invoke() → Close()

OnClick(BtnCancel):
  A_BTN_PRESS → onCancel?.Invoke() → Close()

Close():
  Card播放A_POPUP_OUT → Mask Alpha→0 0.15s → SetActive(false)
```

---

## 数据源

| 字段 | 来源 | 刷新频率 |
|------|------|---------|
| title | Show()参数 | 每次调用 |
| content | Show()参数 | 每次调用 |
