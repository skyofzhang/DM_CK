# 冬日生存法则 — UI 全局主题变量 v1.0

> **基准**: 1080×1920 竖屏 (9:16) | CanvasScaler: Scale With Screen Size, Match=0.5
> 命名约定: C_=颜色, F_=字体, A_=动画, S_=尺寸, L_=Canvas层级
> 所有 panel_*.md 文件引用此文件中的变量，不重复定义。
> 主题风格: **冬日极寒** — 深蓝黑背景 + 冰蓝白雪 + 火焰暖橙（对比） + 金色警报

---

## 1. 颜色 (C_)

### 主题色（冬日生存专用）

| 变量 | 值 | 用途 |
|------|-----|------|
| C_WINTER_BG | `#0A0F1A` | 主背景（深夜蓝黑） |
| C_WINTER_PANEL | `#0D1A2A` | 面板底色（深蓝） |
| C_WINTER_CARD | `#142030` | 卡片/按钮底色 |
| C_ICE_LIGHT | `#88CCFF` | 冰蓝浅色（矿石数值，装饰） |
| C_ICE_WHITE | `#E0F0FF` | 冰白（普通文字，雪花效果） |
| C_FIRE_HOT | `#FF6820` | 炉火橙红（炉温高时，生火特效） |
| C_FIRE_WARM | `#FF8C3A` | 暖橙（食物数值，温暖提示） |
| C_COAL_GRAY | `#C8C8C8` | 煤炭灰白（煤炭数值） |
| C_GOLD_ALERT | `#FFD700` | 金色警报（T5礼物，MVP，重要事件） |
| C_DANGER_RED | `#FF2200` | 危险红（失败条件临近，城门危急） |
| C_COLD_BLUE | `#4A90D9` | 冷蓝（炉温数值正常，夜晚标识） |
| C_COLD_PURPLE | `#7766DD` | 冷紫（炉温极低警告） |

### 功能颜色

| 变量 | 值 | 用途 |
|------|-----|------|
| C_TITLE | `#FFD700` | 大标题/重要金字/守护者第1名 |
| C_SUBTITLE | `#B8D4FF` | 小标题/区域名（冰蓝调） |
| C_TEXT | `#C0D0E0` | 正文描述（偏蓝的灰白） |
| C_TEXT_DIM | `#6080A0` | 小字注释（暗蓝灰） |
| C_TEXT_WHITE | `#FFFFFF` | 按钮/亮色文字 |
| C_POSITIVE | `#44FF88` | 加成/成功/资源增加 |
| C_NEGATIVE | `#FF2200` | 失败/减损/资源耗尽 |
| C_WARN | `#FF8800` | 警告（资源低于阈值） |
| C_MVP_GOLD | `#FFD700` | MVP第1名 |
| C_MVP_SILVER | `#C0C0C0` | MVP第2名 |
| C_MVP_BRONZE | `#CD7F32` | MVP第3名 |
| C_FROZEN | `#88CCFF` | 冻结状态（魔法镜效果） |
| C_REDDOT | `#FF3333` | 红点提示 |

### 背景/遮罩颜色

| 变量 | 值 | 用途 |
|------|-----|------|
| C_BG_DEEP | `#0A0F1A` | 主背景深蓝黑 |
| C_BG_PANEL | `#0D1A2A` | 面板底色 |
| C_BG_CARD | `#142030` | 卡片/按钮底色 |
| C_BG_HUD | `#0D1A2ACC` | HUD栏背景（80%透明深蓝） |
| C_BG_OVERLAY_40 | `#00000066` | 遮罩40%（礼物T4背景） |
| C_BG_OVERLAY_60 | `#00000099` | 遮罩60% |
| C_BG_OVERLAY_85 | `#000000D9` | T5空投遮罩85%（游戏暂停层） |
| C_BORDER_PANEL | `#2040608` | 面板边框线（冰蓝暗） |
| C_BORDER_ACTIVE | `#88CCFF` | 激活/选中边框 |
| C_BORDER_ALERT | `#FF2200` | 危险边框（闪烁警告） |

### 天气/时段颜色

| 变量 | 值 | 用途 |
|------|-----|------|
| C_DAYTIME_SKY | `#FFD700` | 白天倒计时颜色 |
| C_NIGHT_SKY | `#9999FF` | 夜晚倒计时颜色 |
| C_SNOWSTORM | `#AACCFF` | 暴风雪事件颜色 |
| C_HARVEST | `#FFCC44` | 丰收事件颜色 |
| C_MONSTER_WAVE | `#FF4422` | 怪物潮事件颜色 |

---

## 2. 字体 (F_)

> **字体文件**: TextMeshPro，中文字体（MS YaHei Bold / 思源黑体）

| 变量 | 字号 | 颜色 | 样式 | 用途 |
|------|------|------|------|------|
| F_H1 | 40px | C_TITLE | Bold | 大标题（结算天数、事件播报） |
| F_H2 | 32px | C_SUBTITLE | Bold | 面板标题、资源数值 |
| F_BODY | 28px | C_TEXT | Regular | bobao播报正文 |
| F_NUM | 32px | (按资源类型) | Bold Mono | 数值显示（食物/煤/矿/炉温） |
| F_SMALL | 20px | C_TEXT_DIM | Regular | 守护者排名积分、小字注释 |
| F_BTN | 28px | C_TEXT_WHITE | Bold | 主播按钮文字 |
| F_TOAST | 24px | C_TEXT_WHITE | Regular | Toast提示 |
| F_COUNTDOWN | 40px | (昼夜色) | Bold Mono | 倒计时数字 |

### 飘字/大字专用

| 变量 | 字号 | 颜色 | 说明 |
|------|------|------|------|
| F_SETTLEMENT_DAY | 200px | C_TITLE | 结算A屏天数大字 |
| F_GIFT_T5_NAME | 80px | C_GOLD_ALERT | T5礼物全屏玩家名 |
| F_RANKING_1ST | 36px | C_TITLE | 守护者第1名名字 |
| F_RANKING_2ND | 28px | C_MVP_SILVER | 守护者第2名名字 |
| F_RANKING_3RD | 28px | C_MVP_BRONZE | 守护者第3名名字 |
| F_BROADCASTER_ALERT | 44px | `#FFFF00` | 主播加速/触发事件全屏提示 |

---

## 3. 动画 (A_)

| 变量 | 参数 | Easing | 用途 |
|------|------|--------|------|
| A_PANEL_IN | X:+540→0, 0.3s | EaseOutCubic | 面板从右滑入 |
| A_PANEL_OUT | X:0→+540, 0.2s | EaseInCubic | 面板从右滑出 |
| A_POPUP_IN | Scale:0→1, 0.2s | EaseOutBack(1.2) | 弹窗出现 |
| A_POPUP_OUT | Scale:1→0, 0.15s | EaseInBack | 弹窗关闭 |
| A_TOAST_IN | Alpha:0→1, 0.15s | Linear | Toast淡入 |
| A_TOAST_STAY | 1.5s | — | Toast停留 |
| A_TOAST_OUT | Alpha:1→0, 0.3s | Linear | Toast淡出 |
| A_BTN_PRESS | Scale:1→0.95, 0.1s | Linear | 按钮按下（主播按钮用） |
| A_NUM_ROLL | 数值插值, 0.5s | EaseOutQuart | 数字跳动（资源数值变化） |
| A_WAVE_BOUNCE | Scale:1.0→1.3→1.0, 0.3s | EaseOutBounce | 波次开始/等级变化 |
| A_SLIDE_UP | Y:-100→0, 0.4s | EaseOutBack | 从下滑入（bobao新消息） |
| A_RANK_CHANGE | 位置插值, 0.5s | EaseOutQuart | 守护者排名位置变动 |
| A_FLASH_WARN | Alpha:1→0.3→1, 0.5s, 循环 | Linear | 资源低于阈值红闪 |
| A_GIFT_FLY_IN | 从外部→屏幕中央, 0.5s | EaseOutBounce | T3+礼物飞入动画 |
| A_GIFT_EXPLODE | Scale:1→2, Alpha:1→0, 0.5s | EaseOutQuart | 礼物爆炸消散 |
| A_FULLSCREEN_IN | Alpha:0→0.85, 0.3s | EaseInQuart | T5遮罩入场 |
| A_FULLSCREEN_OUT | Alpha:0.85→0, 0.5s | EaseOutQuart | T5遮罩退场 |
| A_WORKER_BOB | Y:+/-0.1单位, 1s | SineCurve | Worker待机晃动 |
| A_WORKER_WORK | 左右30度, 0.5s/次 | EaseInOutSine | Worker工作挥动 |
| A_FROZEN_ICE | ParticleSystem覆盖, 渐入0.5s | — | 冻结特效 |

---

## 4. 尺寸/间距 (S_)

| 变量 | 值 | 用途 |
|------|-----|------|
| S_RESOLUTION | 1080×1920 | 基准分辨率(9:16竖屏) |
| S_SAFE_TOP | 60px | 上安全区（含刘海/状态栏） |
| S_SAFE_BOTTOM | 40px | 下安全区（Home键区域） |
| S_HUD_HEIGHT | 120px | 顶部HUD高度 |
| S_BARRAGE_HEIGHT | 200px | 底部弹幕区高度 |
| S_RANKING_WIDTH | 180px | 守护者排名面板宽度 |
| S_RANKING_ROW | 72px | 排名每行高度 |
| S_BROADCASTER_BTN | 120×120 | 主播按钮尺寸（圆形） |
| S_WORKER_BUBBLE | 64×64 | 工人头顶气泡世界UI尺寸 |
| S_GIFT_ICON_T3 | 240×240 | T3礼物大图标 |
| S_GIFT_ICON_T4 | 320×320 | T4礼物大图标 |
| S_GIFT_ICON_T5 | 400×400 | T5礼物大图标 |
| S_RESOURCE_ICON | 48×48 | 顶部HUD资源图标 |
| S_BOBAO_WIDTH | 540px | bobao滚动条宽度 |

---

## 5. Canvas层级 (L_)

| 变量 | Sort Order | 说明 |
|------|-----------|------|
| L_GAME_WORLD | 0 | Worker头顶气泡/血条（世界空间） |
| L_HUD | 10 | 顶部状态栏+守护者排名+bobao（常驻） |
| L_BARRAGE | 20 | 底部弹幕滚动条 |
| L_ALERT | 30 | 资源警报/倒计时警告弹窗 |
| L_SETTLEMENT | 50 | 结算三屏 |
| L_BROADCASTER | 60 | 主播控制面板 |
| L_GIFT | 100 | 礼物通知特效（T1-T5，穿透射线） |
| L_OVERLAY | 200 | T5遮罩/加载/断连画面（最高） |

---

## 6. 通用组件（可复用）

### @ResourceDisplay（资源数值显示组件）
- 图标 Image：48×48px，锚点左对齐
- 数值 TMP：F_NUM，图标右侧8px间距
- 警告闪烁：数值低于阈值时应用 A_FLASH_WARN，颜色变为 C_WARN
- 0值状态：颜色变为 C_DANGER_RED，字体加粗

### @RankingRow（守护者排名行组件）
- 高度：S_RANKING_ROW (72px)，宽度：S_RANKING_WIDTH (180px)
- 结构：[排名数字 28px] [头像 40×40] [昵称 F_SMALL] [积分 F_SMALL]
- 变化箭头：↑绿色 / ↓红色，显示1秒后消失

### @GiftBanner（礼物播报横幅）
- 背景：渐变横条，左侧礼物等级色，右侧淡出
- 高度：80px，宽度：540px
- 出现：A_SLIDE_UP，消失：A_TOAST_OUT

---

*文档维护：策划Claude | 更新：2026-02-24*
