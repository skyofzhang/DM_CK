# 弹幕提示条 — view_barrage_bar

> Requires: ui_theme.md
> Canvas: L_BARRAGE (90)
> Priority: P0
> CachePolicy: Always
> Entry: 游戏启动后常驻，全程不隐藏
> Exit: 无（不可关闭）
> Animation: 无
> ShowCondition: always

---

## 元素表

### 容器层

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| Root | Panel | canvas | (0,0)→(1,0) | 0,S_SAFE_BOTTOM,0,100 | 1080×60 | C_BG_OVERLAY_75 Raycast=false | 底部弹幕条 |

### Root 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| Icon | Image | Root | (0,0.5) | 12,-16,44,16 | 32×32 | Raycast=false | 弹幕图标 |
| ScrollClip | Panel | Root | (0,0)→(1,1) | 52,0,0,0 | fill | Mask Raycast=false | 文字裁剪区 |
| ScrollText | TMP | ScrollClip | (0,0.5) | 0,-11,0,11 | auto×22 | F_SMALL C_TEXT_WHITE Raycast=false | 滚动弹幕提示 |

---

## 交互逻辑

```
ScrollText:
  持续向左滚动, speed=120px/s
  文字末尾离开左边缘后 → 重置到右边缘外重新滚动

滚动内容(循环):
  "发弹幕「加入」参与战斗 | 「查角色」查看修仙状态 | 「查背包」查看道具 | 送礼物触发超级特效！"
```

---

## 数据源

| 字段 | 来源 | 刷新频率 |
|------|------|---------|
| 滚动文字 | 硬编码(后续可配置) | 静态 |
