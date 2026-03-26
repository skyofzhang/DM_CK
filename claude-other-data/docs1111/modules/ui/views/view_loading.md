# 加载画面 — view_loading

> Requires: ui_theme.md
> Canvas: L_OVERLAY (200)
> Priority: P0
> CachePolicy: Always
> Entry: 游戏启动 / 场景切换
> Exit: 加载完成 → 渐出0.5s → 切换场景
> Animation: A_FADEIN 进入; Alpha 1→0 0.5s 退出
> ShowCondition: 加载流程触发

---

## 元素表

### 容器层

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| BgFull | Image | root | (0,0)→(1,1) | 0,0,0,0 | full | — | 修仙风格全屏背景图 R_BG/loading_bg.png |
| Logo | Image | root | (0.5,0.7) | -320,-140,320,140 | 640×280 | — | 游戏Logo R_UI_COMMON/logo.png |
| ProgressRoot | Panel | root | (0.5,0) | -400,200,400,216 | 800×16 | C_BG_CARD 圆角8px | 进度条底框 |
| StatusText | TMP | root | (0.5,0) | -200,230,-200,260 | 400×30 | F_BODY C_TEXT_WHITE | 加载状态文字 |
| TipsText | TMP | root | (0.5,0) | -400,120,400,150 | 800×30 | F_SMALL C_TEXT_DIM | 随机Tips |

### ProgressRoot 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| ProgressFill | Image | ProgressRoot | (0,0)→(0,1) | 0,0,0,0 | 0~800×16 | Gradient C_REALM→C_TITLE 圆角8px | 进度填充(宽度=progress%) |

---

## 交互逻辑

```
OnOpen():
  ProgressFill.width = 0
  StatusText = "正在连接服务器..."

OnProgress(value):
  ProgressFill.width = Lerp(0, 800, value)
  if value < 0.3 → StatusText = "正在连接服务器..."
  if value < 0.8 → StatusText = "加载资源..."
  if value >= 1.0 → StatusText = "初始化完成！"

TipsLoop:
  每3s随机切换一条Tip, A_FADEIN

OnComplete:
  Alpha 1→0, 0.5s → Destroy
```

---

## 数据源

| 字段 | 来源 | 刷新频率 |
|------|------|---------|
| 进度值 | LoadingManager.progress | 每帧 |
| 状态文字 | 进度阈值判定 | 跟随进度 |
| Tips | Resources/Configs/tips.json | OnOpen随机取 |
