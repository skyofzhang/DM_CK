# UI 架构设计 — nxxhnm v2.0

> Requires: ui_theme.md
> 本文定义 UIManager框架、面板生命周期、导航栈、GameState→UI映射。
> 开发前必读。

---

## 1. UIManager 单例

### 职责
- 管理所有面板的注册、加载、显示、隐藏、销毁
- 维护导航栈 (NavigationStack)
- 响应 GameState 变化自动切换 UI 层

### API

```
UIManager.Show<T>(data?) → 打开面板T, Push入栈
UIManager.Hide<T>() → 关闭面板T, Pop出栈
UIManager.HideAll() → 清空栈, 回到根面板
UIManager.GetPanel<T>() → 获取缓存的面板实例(可能null)
```

### 面板注册表

| PanelId | Prefab路径 | Canvas | 缓存策略 | 独占 |
|---------|-----------|--------|---------|------|
| MainLobby | R_PREFAB_UI/MainLobby | L_HUD | Always | true |
| BattleHUD | R_PREFAB_UI/BattleHUD | L_BATTLE_HUD | Always | true |
| BarrageBar | R_PREFAB_UI/BarrageBar | L_BARRAGE | Always | false |
| Character | R_PREFAB_UI/Character | L_PANEL | Normal | true |
| Equipment | R_PREFAB_UI/Equipment | L_PANEL | Normal | true |
| Gem | R_PREFAB_UI/Gem | L_PANEL | Normal | true |
| Pet | R_PREFAB_UI/Pet | L_PANEL | Normal | true |
| Costume | R_PREFAB_UI/Costume | L_PANEL | Normal | true |
| Shop | R_PREFAB_UI/Shop | L_PANEL | Normal | true |
| Ranking | R_PREFAB_UI/Ranking | L_PANEL | Normal | true |
| PVP | R_PREFAB_UI/PVP | L_PANEL | Normal | true |
| Lottery | R_PREFAB_UI/Lottery | L_PANEL | Normal | true |
| Inventory | R_PREFAB_UI/Inventory | L_PANEL | Normal | true |
| Collection | R_PREFAB_UI/Collection | L_PANEL | Disposable | true |
| Settings | R_PREFAB_UI/Settings | L_PANEL | Disposable | true |
| BattleResult | R_PREFAB_UI/BattleResult | L_BATTLE_HUD | Disposable | false |
| Loading | R_PREFAB_UI/Loading | L_OVERLAY | Always | true |
| ConfirmDialog | R_PREFAB_UI/ConfirmDialog | L_POPUP | Normal | false |
| Toast | R_PREFAB_UI/Toast | L_TOAST | Always | false |
| GiftEffect | R_PREFAB_UI/GiftEffect | L_GIFT_EFFECT | Always | false |
| Disconnect | R_PREFAB_UI/Disconnect | L_OVERLAY | Normal | true |

### 缓存策略说明

| 策略 | 行为 | 适用场景 |
|------|------|---------|
| Always | 实例化后永不销毁 | 高频/常驻面板(主界面/战斗HUD/弹幕条) |
| Normal | 关闭后保留实例,下次Open复用 | 中频面板(角色/装备/商城等) |
| Disposable | 关闭时销毁实例 | 低频面板(图鉴/设置/战斗结算) |

---

## 2. 面板生命周期

```
Instantiate → OnInit() → OnOpen(data) → [Active]
                                          ↓ (被新面板覆盖)
                                     OnPause()
                                          ↓ (子面板关闭返回)
                                     OnResume()
                                          ↓ (用户关闭)
                                     OnClose() → OnDestroy() (仅Disposable)
```

| 回调 | 时机 | 典型用途 |
|------|------|---------|
| OnInit() | 首次实例化(仅1次) | 缓存UI引用(TMP/Image/Button), 注册按钮事件 |
| OnOpen(data) | 每次打开 | 刷新数据, 播放A_PANEL_IN, 重置ScrollRect位置 |
| OnResume() | 子面板关闭返回时 | 刷新可能变化的数据(如强化后装备属性变了) |
| OnPause() | 新面板覆盖时 | 暂停不必要的Update/定时刷新 |
| OnClose() | 关闭面板 | 播放A_PANEL_OUT, 清理临时状态 |
| OnDestroy() | 实例销毁(Disposable) | 注销事件监听, 释放资源引用 |

---

## 3. 导航栈 (NavigationStack)

### 核心规则
- L_PANEL层内: 同时只有栈顶面板可见(exclusive=true的面板Push时会Pause前一个)
- 浮层不入栈: ConfirmDialog / Toast / GiftEffect / BarrageBar 独立管理,不影响栈
- 返回操作: 任何面板的@BackButton / @CloseButton → Pop()

### 栈操作

```
Push(panelId, data):
  currentTop.OnPause()
  newPanel = UIManager.Show(panelId, data)
  stack.Push(newPanel)

Pop():
  current = stack.Pop()
  current.OnClose()
  if stack.Count > 0 → stack.Peek().OnResume()

PopTo(panelId):
  while stack.Peek() != panelId → Pop()

Clear():
  while stack.Count > 0 → Pop()
```

### 特殊场景

| 场景 | 行为 |
|------|------|
| 角色面板→Tab切时装 | 不Push新面板, 同面板内Tab切换(内部状态) |
| 装备面板→空槽→选择列表 | Push子面板EquipSelect(L_PANEL+2, 非exclusive) |
| 宠物面板→装备按钮 | Push子面板PetEquip(L_PANEL+2) |
| 战斗开始(GameState切换) | NavigationStack.Clear(), 切换Canvas可见性 |

---

## 4. GameState → UI 映射

> GameState定义见 M08_tech_foundation.md

| GameState | 可见Canvas | 隐藏Canvas | 活跃面板 |
|-----------|-----------|------------|---------|
| idle | L_HUD, L_BARRAGE | L_BATTLE_HUD | MainLobby + BarrageBar |
| waiting_players | L_HUD, L_BARRAGE | L_BATTLE_HUD | MainLobby(等待动画) |
| battle_start | L_BATTLE_HUD, L_BARRAGE | L_HUD, L_PANEL | BattleHUD + BarrageBar |
| wave_1/2/3 | L_BATTLE_HUD, L_BARRAGE | L_HUD, L_PANEL | BattleHUD |
| battle_end | L_BATTLE_HUD | L_HUD | BattleResult叠加在BattleHUD上 |
| reward_settlement | L_BATTLE_HUD | L_HUD | BattleResult(奖励展示) |
| next_level | L_BATTLE_HUD, L_BARRAGE | L_HUD | BattleHUD(波次重置) |
| pvp_matching | L_PANEL, L_BARRAGE | — | PVP面板(匹配等待) |
| pvp_battle | L_BATTLE_HUD, L_BARRAGE | L_HUD, L_PANEL | BattleHUD(PVP模式) |
| pvp_settlement | L_BATTLE_HUD | — | BattleResult(PVP) |
| boss_waiting | L_BATTLE_HUD, L_BARRAGE | L_HUD | BattleHUD(Boss倒计时) |
| boss_battle | L_BATTLE_HUD, L_BARRAGE | L_HUD | BattleHUD(Boss模式) |
| boss_settlement | L_BATTLE_HUD | — | BattleResult(Boss) |

### 状态切换时的UI行为

```
idle → battle_start:
  NavigationStack.Clear()
  HideCanvas(L_HUD, L_PANEL, L_POPUP)
  ShowCanvas(L_BATTLE_HUD)
  BattleHUD.OnOpen({level, wave, enemies})

battle_end → idle:
  BattleResult.OnOpen({mvp, rewards, rankings})
  Wait(用户点击或5s自动) →
    HideCanvas(L_BATTLE_HUD)
    ShowCanvas(L_HUD)
    MainLobby.OnResume()

idle → 打开功能面板:
  MainLobby.OnPause() // 不隐藏,只暂停刷新
  ShowCanvas(L_PANEL)
  NavigationStack.Push(targetPanel)
```

### 全局浮层(不受GameState影响,任何时刻可触发)

| 浮层 | Canvas | 触发条件 | 特殊规则 |
|------|--------|---------|---------|
| ConfirmDialog | L_POPUP | 任何消耗操作 | 遮罩可点击关闭, 同时最多1个 |
| Toast | L_TOAST | 任何提示 | 同时最多3条, 新的在上旧的下移 |
| GiftEffect | L_GIFT_EFFECT | 收到礼物 | Raycast穿透, 520档时其他UI Alpha→0.3 |
| BarrageBar | L_BARRAGE | 常驻 | 全程不隐藏, Raycast穿透 |
| Disconnect | L_OVERLAY | 断连>3s | 阻挡所有操作, 自动重试 |
| Loading | L_OVERLAY | 启动/场景切换 | 阻挡所有操作 |

---

## 5. 通用交互约定

| 操作类型 | 统一行为 |
|---------|---------|
| 返回 | NavigationStack.Pop() + A_PANEL_OUT |
| 关闭弹窗 | 点击遮罩 或 @CloseButton → Popup.Close() |
| 消耗操作 | 必须经 ConfirmDialog 二次确认 |
| 材料不足 | Toast.Show(text, FAIL) + 按钮抖动0.2s |
| 材料足够 | 按钮高亮(金色渐变), 不足时灰化(#666666) |
| 红点刷新 | 返回MainLobby时(OnResume)批量刷新所有按钮红点 |
| 数值变化 | A_NUM_ROLL 数字滚动 |
| 品质提升 | 边框颜色切换 + 闪光特效0.8s |
| 按钮禁用 | Alpha=0.4 + 不响应点击 |

---

## 6. 场景渲染分层 (非UI,与M01对齐)

| SortingLayer | Order | 内容 |
|---|---|---|
| Background | -10 | 天空/远景 |
| BackDecor | -5 | 近景装饰/地面 |
| Characters | 0 | 角色主体 |
| CharFront | 5 | 武器挥击 |
| Effects | 10 | 技能特效/粒子 |
| UIWorld | 20 | 世界空间UI(血条/名字HUD) |

### 战斗场景世界坐标 (orthographicSize=5, 1080×1920)
- 世界可视: X[-2.8125, +2.8125], Y[-5.0, +5.0]
- 玩家队伍(右侧): X:+0.5~+2.5, Y:-1.5~+1.5
- 敌方队伍(左侧): X:-0.5~-2.5, Y:-1.5~+1.5
- 地面基准线: Y=-2.0
- Boss出场: X=-2.0, Y=-1.0
