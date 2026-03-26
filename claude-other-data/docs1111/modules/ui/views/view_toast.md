# Toast提示 — view_toast

> Requires: ui_theme.md
> Canvas: L_TOAST (40)
> Priority: P0
> CachePolicy: Always
> Entry: 任意系统触发 ToastManager.Show()
> Exit: 自动消失(停留1.5s→淡出0.3s)
> Animation: A_TOAST_IN / A_TOAST_STAY / A_TOAST_OUT
> ShowCondition: API调用触发

---

## 元素表

### 容器层

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| Root | Panel | canvas | (0,0)→(1,1) | 0,0,0,0 | full | Raycast=false | 穿透容器 |
| ToastAnchor | Panel | Root | (0.5,0.8) | -400,-80,400,80 | 800×160 | Raycast=false | Toast锚点(最多3条) |

### Toast单条模板

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| ToastItem | Panel | ToastAnchor | (0.5,1) | auto | auto | C_BG_OVERLAY_75 圆角32px CSF=PreferredSize Raycast=false | 单条Toast |
| TypeIcon | Image | ToastItem | (0,0.5) | 16,-12,40,12 | 24×24 | 按type着色 Raycast=false | 类型图标 |
| Message | TMP | ToastItem | (0,0.5) | 48,-12,-24,12 | auto×24 | F_TOAST C_TEXT_WHITE Raycast=false | 提示文字 |

---

## 5种类型

| Type | Icon | 图标色 |
|------|------|--------|
| SUCCESS | ✓ | C_POSITIVE |
| FAIL | ✗ | C_NEGATIVE |
| REWARD | + | C_TITLE |
| INFO | ℹ | C_Q_BLUE |
| WARN | ⚠ | C_Q_ORANGE |

---

## 交互逻辑

```
API: ToastManager.Show(text, type=INFO)

Show(text, type):
  if 当前显示数 >= 3 → 立即移除最旧一条
  item = Instantiate(ToastItem)
  item.TypeIcon = GetIcon(type)
  item.Message = text
  播放 A_TOAST_IN → 等待 A_TOAST_STAY(1.5s) → A_TOAST_OUT → Destroy(item)

新Toast出现时:
  已有Toast向下偏移(Y-=item.height+8), 0.15s EaseOut
```

---

## 数据源

| 字段 | 来源 | 刷新频率 |
|------|------|---------|
| text | Show()参数 | 每次调用 |
| type | Show()参数 | 每次调用 |
