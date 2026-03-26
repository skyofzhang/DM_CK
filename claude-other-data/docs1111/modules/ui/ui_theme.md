# UI 全局主题变量 — nxxhnm v2.0

> 所有 view_*.md 文件引用此文件中的变量，不重复定义。
> 命名约定: C_=颜色, F_=字体, A_=动画, S_=尺寸, L_=Canvas层级, R_=资源路径

---

## 1. 颜色 (C_)

### 品质颜色

| 变量 | 值 | 背景底色(5%透明) |
|------|-----|------------------|
| C_Q_WHITE | #FFFFFF | #FFFFFF0D |
| C_Q_GREEN | #00FF00 | #00FF000D |
| C_Q_BLUE | #4488FF | #4488FF0D |
| C_Q_PURPLE | #CC44FF | #CC44FF0D |
| C_Q_ORANGE | #FF8800 | #FF88000D |
| C_Q_RED | #FF0000 | #FF00000D |
| C_Q_GOLD | #FFD700 | #FFD7000D |
| C_Q_DARK_GOLD | 渐变 #FFD700→#FF8800 | — |

### 功能颜色

| 变量 | 值 | 用途 |
|------|-----|------|
| C_TITLE | #FFD700 | 大标题/重要金字 |
| C_SUBTITLE | #E8D4FF | 小标题/区域名 |
| C_TEXT | #C8C8C8 | 正文描述 |
| C_TEXT_DIM | #888888 | 小字注释 |
| C_TEXT_WHITE | #FFFFFF | 按钮/亮色文字 |
| C_POSITIVE | #00FF88 | 加成/成功/HP回复 |
| C_NEGATIVE | #FF2200 | 失败/减损/倒计时 |
| C_POWER | #FF8800 | 战力数值 |
| C_REALM | #CC44FF | 境界相关 |
| C_VIP | #FFD700 | VIP相关 |
| C_REDDOT | #FF3333 | 红点背景 |

### 属性颜色

| 变量 | 值 | 属性 |
|------|-----|------|
| C_HP | #FF4422 | 生命值 |
| C_ATK | #FF8800 | 攻击力 |
| C_DEF | #4488FF | 防御力 |
| C_CRIT | #FFD700 | 暴击 |
| C_DODGE | #00CCFF | 闪避 |
| C_VAMP | #CC44FF | 吸血 |

### 宝石颜色

| 变量 | 值 | 宝石 |
|------|-----|------|
| C_GEM_CHIYAN | #FF4422 | 赤炎石(HP) |
| C_GEM_POFENG | #FF8800 | 破锋石(ATK) |
| C_GEM_XUANTIE | #4488FF | 玄铁石(DEF) |
| C_GEM_LIETIAN | #FFD700 | 裂天石(暴击) |
| C_GEM_HUANYING | #00CCFF | 幻影石(闪避) |
| C_GEM_SHIXUE | #CC44FF | 噬血石(吸血) |

### 背景颜色

| 变量 | 值 | 用途 |
|------|-----|------|
| C_BG_DEEP | #0A0A28 | 主背景深蓝黑 |
| C_BG_PANEL | #0D0D28 | 面板底色 |
| C_BG_CARD | #1A1A4A | 卡片/按钮底色 |
| C_BG_BAR | #0E0E2A | 底部Tab/工具栏 |
| C_BG_OVERLAY_50 | #00000080 | 半透明遮罩50% |
| C_BG_OVERLAY_60 | #00000099 | 遮罩60% |
| C_BG_OVERLAY_75 | #000000BF | 遮罩75% |
| C_BG_OVERLAY_85 | #000000D9 | 重遮罩85% |
| C_BG_TITLEBAR | 渐变 #1A0A2E→#0D1A3A→#1A0A2E, A=220 | 标题栏 |
| C_BORDER_TITLE | #6644AA | 标题栏底部分隔线 |

### 战力等级颜色

| 变量 | 值 | 条件 |
|------|-----|------|
| C_POWER_1 | #FFFFFF | power < 100k |
| C_POWER_2 | #87CEEB | power < 1M |
| C_POWER_3 | #DA70D6 | power < 10M |
| C_POWER_4 | #FFD700 | power >= 10M |

---

## 2. 字体 (F_)

| 变量 | 字号 | 颜色 | 样式 | 用途 |
|------|------|------|------|------|
| F_H1 | 32px | C_TITLE | Bold | 面板名大标题 |
| F_H2 | 26px | C_SUBTITLE | Bold | 区域名小标题 |
| F_BODY | 24px | C_TEXT | Regular | 正文描述 |
| F_NUM | 28px | (按需) | Bold Mono | 数值显示 |
| F_SMALL | 18px | C_TEXT_DIM | Regular | 小字注释 |
| F_BTN | 26px | C_TEXT_WHITE | Bold | 按钮文字(默认) |
| F_TOAST | 24px | C_TEXT_WHITE | Regular | Toast文字 |

### 飘字专用

| 变量 | 字号 | 颜色 | 说明 |
|------|------|------|------|
| F_DMG_NORMAL | 36px | C_TEXT_WHITE | 普攻飘字 |
| F_DMG_CRIT | 52px | C_TITLE | 暴击飘字 |
| F_DMG_SKILL | 44px | C_NEGATIVE | 技能飘字 |

---

## 3. 动画 (A_)

| 变量 | 参数 | Easing | 用途 |
|------|------|--------|------|
| A_PANEL_IN | X:+540→0, 0.3s | EaseOutCubic | 面板从右滑入 |
| A_PANEL_OUT | X:0→+540, 0.2s | EaseInCubic | 面板从右滑出 |
| A_POPUP_IN | Scale:0→1, 0.2s | EaseOutBack(1.2) | 弹窗出现 |
| A_POPUP_OUT | Scale:1→0, 0.15s | EaseInBack | 弹窗关闭 |
| A_TOAST_IN | Alpha:0→1, 0.2s | Linear | Toast淡入 |
| A_TOAST_STAY | 1.5s | — | Toast停留 |
| A_TOAST_OUT | Alpha:1→0, 0.3s | Linear | Toast淡出 |
| A_BTN_PRESS | Scale:1→0.95, 0.1s | Linear | 按钮按下 |
| A_NUM_ROLL | 数值插值, 0.5s | EaseOutQuart | 数字跳动 |
| A_FADEIN | Alpha:0→1, 0.2s | Linear | 通用淡入 |
| A_WAVE_BOUNCE | Scale:1.0→1.3→1.0, 0.3s | EaseOutBounce | 波次/等级变化 |
| A_SLIDE_UP | Y:-100→0, 0.4s | EaseOutBack | 从下滑入 |

---

## 4. 尺寸/间距 (S_)

| 变量 | 值 | 用途 |
|------|-----|------|
| S_RESOLUTION | 1080×1920 | 基准分辨率(9:16竖屏) |
| S_SAFE_TOP | 40px | 上安全区(刘海/状态栏) |
| S_SAFE_BOTTOM | 40px | 下安全区(导航栏/Home) |
| S_USABLE | 1080×1840 (y:40~1880) | 可用区域 |
| S_CLOSE_BTN | 64×64 | 关闭按钮 |
| S_TITLEBAR_H | 88px | 标题栏高度 |
| S_REDDOT | 20×20(无数字) / 32×20(有数字) | 红点 |
| S_QUALITY_BORDER | 3px边框, 圆角6px | 品质边框 |

---

## 5. Canvas层级 (L_)

| 变量 | Sort Order | 用途 |
|------|-----------|------|
| L_GAME_WORLD | 0 | 战斗场景世界空间UI(血条/名字) |
| L_HUD | 10 | 主界面大厅(常驻) |
| L_PANEL | 20 | 主体面板层(背包/角色/装备等) |
| L_POPUP | 30 | 弹窗层(确认弹窗/详情浮层) |
| L_TOAST | 40 | Toast提示层 |
| L_BATTLE_HUD | 50 | 战斗HUD+战斗结算 |
| L_BARRAGE | 90 | 弹幕提示条(常驻) |
| L_GIFT_EFFECT | 100 | 礼物特效层(穿透射线) |
| L_OVERLAY | 200 | 网络断连/加载画面(最高) |

---

## 6. 资源路径 (R_)

| 变量 | 路径 | 用途 |
|------|------|------|
| R_UI_COMMON | Textures/UI/Common/ | 通用UI(关闭/红点/品质框) |
| R_ICON_EQUIP | Textures/UI/Equipment/ | 装备图标 |
| R_ICON_ITEM | Textures/UI/Items/ | 道具图标 |
| R_ICON_GEM | Textures/UI/Gems/ | 宝石图标 |
| R_ICON_PET | Textures/UI/Pets/ | 宠物图标 |
| R_ICON_COSTUME | Textures/UI/Costumes/ | 时装图标 |
| R_ICON_FUNC | Textures/UI/FuncIcons/ | 功能按钮图标 |
| R_SPINE_HERO | Spine/Heroes/ | 英雄Spine |
| R_SPINE_MONSTER | Spine/Monsters/ | 怪物Spine |
| R_SPINE_PET | Spine/Pets/ | 宠物Spine |
| R_BG | Textures/Backgrounds/ | 背景图 |
| R_PREFAB_UI | Prefabs/UI/ | UI预制体 |

---

## 7. 通用组件 (引用语法: @ComponentName)

### @CloseButton
- 锚点(1,1), pivot(1,1), 偏移(-16,-16)
- 尺寸: S_CLOSE_BTN, 图标"X"白, 背景圆形#000000 80%
- 按下: A_BTN_PRESS
- OnClick → NavigationStack.Pop() 或 Popup.Close()

### @TitleBar
- 锚点(0,1)→(1,1), 高度S_TITLEBAR_H
- 背景: C_BG_TITLEBAR, 底边1px C_BORDER_TITLE
- 标题: F_H1 居中

### @QualityBorder
- Image Sliced九宫, S_QUALITY_BORDER
- 颜色: C_Q_* 系列, 背景底色: 品质色+0D

### @RedDot
- 尺寸: S_REDDOT, 锚点(1,1), 偏移(+8,+8)
- 背景: C_REDDOT, 文字16px White Bold, 最大"99+"

### @BackButton
- 锚点(0,0.5), 尺寸88×80
- 图标"←"白色, 无背景
- 按下: A_BTN_PRESS
- OnClick → NavigationStack.Pop()

---

## 8. 默认值约定 (view文件省略时采用)

| 属性 | 默认值 |
|------|--------|
| 按钮字体 | F_BTN |
| 文字颜色 | C_TEXT |
| 标题颜色 | C_TITLE |
| 面板背景 | C_BG_DEEP |
| 面板进入动画 | A_PANEL_IN |
| 面板退出动画 | A_PANEL_OUT |
| 按钮按压 | A_BTN_PRESS |
| Canvas Match | 0.5 |
| CanvasScaler | Scale With Screen Size, 1080×1920 |
