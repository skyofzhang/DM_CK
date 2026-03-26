# 断连提示 — view_disconnect

> Requires: ui_theme.md
> Canvas: L_OVERLAY (200)
> Priority: P0
> CachePolicy: Normal
> Entry: 网络断连超过3s自动弹出
> Exit: 重连成功 → 卡片缩小消失 + Toast"连接已恢复"
> Animation: Mask 0.3s淡入 + Card A_POPUP_IN; 退出 Card A_POPUP_OUT + Mask淡出
> ShowCondition: NetworkManager.isDisconnected && disconnectTime > 3s

---

## 元素表

### 容器层

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| Mask | Image | root | (0,0)→(1,1) | 0,0,0,0 | full | C_BG_OVERLAY_85 | 全屏遮罩(阻挡所有操作) |
| Card | Panel | root | (0.5,0.5) | -320,-220,320,220 | 640×440 | C_BG_PANEL 圆角16px Border=C_NEGATIVE 2px闪烁 | 断连卡片 |

### Card 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| DisconnectIcon | Image | Card | (0.5,0.8) | -44,-44,44,44 | 88×88 | — | 断连图标 |
| Title | TMP | Card | (0.5,0.6) | -200,-16,200,16 | 400×32 | F_H2 C_NEGATIVE align=center | "连接已断开" |
| Spinner | Image | Card | (0.5,0.4) | -32,-32,32,32 | 64×64 | RotateZ 360°/s | 旋转加载圈 |
| RetryInfo | TMP | Card | (0.5,0.25) | -200,-12,200,12 | 400×24 | F_SMALL C_TEXT_DIM align=center | "正在重连... (1/5)" |
| BtnRetry | Button | Card | (0.5,0.1) | -140,-32,140,32 | 280×64 | C_BG_CARD 圆角12px | [手动重试] F_BTN |

---

## 交互逻辑

```
重连状态机:
  STATE_AUTO_RETRY:
    retryCount = 0
    Spinner.active = true
    BtnRetry.active = false
    每5s尝试重连:
      retryCount++
      RetryInfo = "正在重连... ({retryCount}/5)"
      if 成功 → STATE_SUCCESS
      if retryCount >= 5 → STATE_MANUAL

  STATE_MANUAL:
    Spinner.active = false
    BtnRetry.active = true
    RetryInfo = "自动重连失败，请手动重试"

  STATE_SUCCESS:
    Card → A_POPUP_OUT
    Mask → Alpha 0, 0.3s
    ToastManager.Show("连接已恢复", SUCCESS)
    Close()

OnClick(BtnRetry):
  A_BTN_PRESS → 回到 STATE_AUTO_RETRY (retryCount重置)
```

---

## 数据源

| 字段 | 来源 | 刷新频率 |
|------|------|---------|
| 连接状态 | NetworkManager.connectionState | 实时监听 |
| 重试次数 | 本地计数 | 每次重试更新 |
