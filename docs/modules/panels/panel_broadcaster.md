# 面板规格：主播控制面板

> Canvas: Broadcaster_Canvas (L_BROADCASTER=60) | 基准: 1080×1920
> 引用: `docs/modules/ui_theme.md`
> 仅在主播模式下显示（服务器 join_room 响应中 isRoomCreator=true）

---

## 面板总体布局

| 属性 | 值 |
|------|-----|
| 位置 | 右侧中下部：X=860, Y=1380（Anchor右下，偏移-200, -520） |
| 尺寸 | 200×280px |
| 背景 | 圆角矩形，C_BG_PANEL(`#0D1A2A`)，透明度85%，圆角16px |
| 边框 | 1px C_BORDER_ACTIVE(`#88CCFF`) |
| 标题 | "主播控制台" F_SMALL(20px, C_TEXT_DIM)，居中，顶部14px |

---

## 按钮1：⚡ 紧急加速

| 属性 | 值 |
|------|-----|
| 位置 | 面板内，X=50, Y=-60（面板中心，左侧） |
| 尺寸 | S_BROADCASTER_BTN (120×120px)，圆形 |
| 背景色 | 可用:`#2A3A10`（暗金绿）；CD中:`#1A1A2A`（暗灰） |
| 边框 | 可用:C_GOLD_ALERT 2px；CD中:C_TEXT_DIM 1px |
| 图标 | ⚡ 黄色闪电，80px |
| 标签 | "紧急加速" F_SMALL(18px)，按钮下方8px |
| CD标签 | "CD [X]s" F_SMALL(18px, C_WARN)，按钮中央（覆盖图标） |

**可用状态动画**：
- 脉冲：Scale 1→1.05→1, 1s, SineCurve, 循环
- 边框颜色脉冲：C_GOLD_ALERT 亮度 1.0→1.5→1.0, 1s

**点击动画**：A_BTN_PRESS (Scale 1→0.9, 0.1s)

**CD状态（120秒）**：
- 按钮灰色 (#1A1A2A)
- CD倒计时TMP：覆盖在按钮中央，从 "120" 倒数至 "0"
- CD结束：图标重新出现，脉冲恢复，sfx_broadcaster_boost 简短提示音

---

## 按钮2：🌊 触发事件

| 属性 | 值 |
|------|-----|
| 位置 | 面板内，X=50, Y=-200（按钮1下方120px） |
| 尺寸 | 120×120px，圆形 |
| 背景色 | 可用:`#0A2A1A`（暗绿）；CD中:`#1A1A2A` |
| 边框 | 可用:`#44FF88` 2px；CD中:C_TEXT_DIM 1px |
| 图标 | 🌊 绿色波浪，80px |
| 标签 | "触发事件" F_SMALL(18px)，按钮下方8px |

**可用状态动画**：
- 波纹效果：圆形ripple从按钮中心向外扩散，Alpha:0.5→0, 半径0→80px, 2s循环

**CD状态（60秒）**：
- 同按钮1，倒计时从60倒数

---

## 全屏反馈（点击后）

### ⚡ 紧急加速触发效果
1. 屏幕顶部大字（全宽）："⚡ 主播激活紧急加速！全体效率翻倍30秒！"
   - 字体：F_BROADCASTER_ALERT(44px, `#FFFF00`)
   - 位置：Y=-180（HUD下方）
   - 动画：Scale 0→1 EaseOutBack, 0.3s；停留2s；Alpha 1→0, 0.5s
2. 所有Worker同时触发Special动画（金色光晕3秒）
3. SFX：sfx_broadcaster_boost

### 🌊 触发事件效果
1. 与正常随机事件相同的全屏播报
2. 播报文字增加前缀："🌊 主播触发了 " + 事件名
3. SFX：sfx_broadcaster_event（若有）或使用sfx_gift_t2_bubble代替

---

## 服务器消息格式

```javascript
// 点击⚡ 紧急加速
{
  type: "broadcaster_action",
  action: "efficiency_boost",
  duration: 30000,      // 30秒
  cooldown: 120000      // 120秒CD
}

// 点击🌊 触发事件
{
  type: "broadcaster_action",
  action: "trigger_event",
  cooldown: 60000       // 60秒CD
}

// 服务器响应（广播给所有客户端）
{
  type: "broadcaster_effect",
  action: "efficiency_boost",
  multiplier: 2.0,
  duration: 30000,
  triggeredBy: "主播"
}
```

---

## 权限判断

- `BroadcasterPanel.gameObject.SetActive(isRoomCreator)`
- `isRoomCreator` 由服务器在 join_room 响应中设置
- 观众端：面板完全不可见，无法操作

---

*更新：2026-02-24*
