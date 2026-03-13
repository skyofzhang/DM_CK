# 面板规格：主HUD（常驻顶部状态栏）

> Canvas: HUD_Canvas (L_HUD=10) | 基准: 1080×1920
> 引用: `docs/modules/ui_theme.md`

---

## 面板总体布局

| 属性 | 值 |
|------|-----|
| 位置 | Anchor全拉伸顶部, Y=0 |
| 尺寸 | 1080×120px |
| 背景 | C_BG_HUD (`#0D1A2ACC`，80%透明深蓝） |
| 底边分隔线 | 1px, C_BORDER_PANEL |

---

## 元素列表

### 元素1：食物 @ResourceDisplay

| 属性 | 值 |
|------|-----|
| 位置 | Anchor左上, X=20, Y=-36 (距顶部36px) |
| 图标 | 48×48px, `Resources/Icons/icon_food` |
| 数值TMP | 字号F_NUM(32px), 颜色C_FIRE_WARM(`#FF8C3A`) |
| 布局 | 图标左 + 8px间距 + 数值右，水平排列 |
| 总宽 | 48+8+80 = 136px |
| 警告阈值 | ≤100：A_FLASH_WARN(C_NEGATIVE, 0.5s周期) |
| 0值状态 | 颜色→C_DANGER_RED，Bold |

### 元素2：煤炭 @ResourceDisplay

| 属性 | 值 |
|------|-----|
| 位置 | X=190, Y=-36 |
| 图标 | 48×48px, `Resources/Icons/icon_coal` |
| 数值TMP | F_NUM(32px), C_COAL_GRAY(`#C8C8C8`) |
| 警告阈值 | ≤60：A_FLASH_WARN |

### 元素3：矿石 @ResourceDisplay

| 属性 | 值 |
|------|-----|
| 位置 | X=360, Y=-36 |
| 图标 | 48×48px, `Resources/Icons/icon_ore` |
| 数值TMP | F_NUM(32px), C_ICE_LIGHT(`#88CCFF`) |
| 警告阈值 | ≤30：A_FLASH_WARN |

### 元素4：炉温显示

| 属性 | 值 |
|------|-----|
| 位置 | X=530, Y=-16, 尺寸160×88px |
| 图标 | 温度计图标，32×64px，左侧 |
| 数值TMP | F_NUM(28px)，右侧 |
| 颜色逻辑 | >0℃：C_FIRE_WARM；-50~0℃：C_COLD_BLUE；-80~-50℃：C_COLD_BLUE+A_FLASH_WARN(1s)；<-80℃：C_COLD_PURPLE+A_FLASH_WARN(0.5s) |
| 格式 | "X℃"（带℃符号） |

### 元素5：倒计时

| 属性 | 值 |
|------|-----|
| 位置 | X=710, Y=-20, 尺寸180×80px |
| 图标 | 昼夜图标32×32（白天:☀️ / 夜晚:🌙），左侧 |
| 数值TMP | F_COUNTDOWN(40px)，居中，白天C_DAYTIME_SKY(`#FFD700`)，夜晚C_NIGHT_SKY(`#9999FF`) |
| 格式 | "X:XX"（分:秒）或直接显示秒数 |

### 元素6：城门HP条

| 属性 | 值 |
|------|-----|
| 位置 | X=910, Y=-34, 尺寸150×20px |
| 类型 | Slider（Fill Area方式） |
| 颜色 | 100%→30%：绿色`#44FF44`；30%→10%：橙色`#FF8800`；≤10%：红色`#FF2200`+A_FLASH_WARN |
| 标签 | "城门"小字，F_SMALL(20px), 上方8px |

---

## 状态依赖

- 数据来源：服务器推送 `resource_update` 消息
- 更新频率：服务器每5秒推送（`_resourceSyncTimer`），有变化立即更新
- C#脚本：`Assets/Scripts/UI/HUDController.cs`

---

*更新：2026-02-24*
