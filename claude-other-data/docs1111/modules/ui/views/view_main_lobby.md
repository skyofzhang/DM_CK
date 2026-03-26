# 主界面(大厅) — view_main_lobby

> Requires: ui_theme.md
> Canvas: L_HUD (10)
> Priority: P0
> CachePolicy: Always
> Entry: Loading完成 / BattleResult[返回大厅]
> Exit: 12个功能按钮→对应面板 / GameState→battle
> Animation: 无(常驻), 功能面板打开时OnPause, 关闭时OnResume
> ShowCondition: GameState == idle || waiting_players

---

## 元素表

### 容器层

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| BgFull | Image | root | (0,0)→(1,1) | 0,0,0,0 | full | C_BG_DEEP | 深蓝黑背景 |
| TopBar | Panel | root | (0,1)→(1,1) | 0,-140,0,0 | 1080×140 | C_BG_OVERLAY_50 | 顶部信息栏 |
| SpineRoot | Panel | root | (0.5,0.5)→(0.5,0.5) | -300,-480,300,480 | 600×960 | — | 角色Spine容器 |
| FuncGrid | Panel | root | (0,0)→(1,0) | 0,140,0,580 | 1080×440 | C_BG_OVERLAY_60 | 功能按钮区 |
| BarrageBar | Panel | root | (0,0)→(1,0) | 0,S_SAFE_BOTTOM,0,120 | 1080×80 | C_BG_OVERLAY_60 | 弹幕提示条(独立view) |

### TopBar 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| VipIcon | Image | TopBar | (0,0.5) | 16,-24,88,24 | 72×48 | — | VIP等级图标 |
| VipLabel | TMP | TopBar | (0,0.5) | 96,-14,196,14 | 100×28 | F_SMALL C_VIP | "VIP {level}" |
| PowerIcon | Image | TopBar | (0,0.5) | 208,-16,240,16 | 32×32 | C_POWER | 战力图标⚔ |
| PowerValue | TMP | TopBar | (0,0.5) | 248,-16,420,16 | 172×32 | F_H2 logic=PowerColorRule | "{power:N0}" |
| RealmLabel | TMP | TopBar | (0.5,0.5) | -120,-18,120,18 | 240×36 | F_NUM C_REALM | 境界名 |
| GoldIcon | Image | TopBar | (1,0.5) | -344,-16,-312,16 | 32×32 | C_TITLE | 金币图标 |
| GoldValue | TMP | TopBar | (1,0.5) | -308,-15,-168,15 | 140×30 | F_BODY C_TEXT_WHITE | 金币数量 |
| DiamondIcon | Image | TopBar | (1,0.5) | -160,-16,-128,16 | 32×32 | C_Q_BLUE | 钻石图标 |
| DiamondValue | TMP | TopBar | (1,0.5) | -124,-15,-16,15 | 108×30 | F_BODY C_TEXT_WHITE | 钻石数量 |

### SpineRoot 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| CharSpine | SkeletonGraphic | SpineRoot | (0.5,0.5) | 0,-60,0,0 | 600×960 | — | anim=idle_main, scale=1.2 |
| Shadow | Image | SpineRoot | (0.5,0) | -120,20,120,60 | 240×40 | #00000060 | 脚下椭圆阴影 |

### FuncGrid 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| GridLayout | GridLayout | FuncGrid | (0,0)→(1,1) | 20,10,-20,-10 | 1040×420 | cols=4, cell=240×130, gap=8 | 按钮网格容器 |

### 按钮模板 (GridLayout子项 ×12)

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| BtnBg | Image | BtnItem | (0,0)→(1,1) | 2,2,-2,-2 | 236×126 | C_BG_CARD 圆角8px | 按钮背景 |
| BtnIcon | Image | BtnItem | (0.5,1) | -36,-90,36,-14 | 72×76 | — | 功能图标 |
| BtnLabel | TMP | BtnItem | (0,0)→(1,0) | 4,4,-4,34 | fill×30 | F_SMALL C_TEXT_WHITE | 功能名 |
| BtnRedDot | @RedDot | BtnItem | — | — | — | — | 红点提示 |

### 12个功能按钮排列

```
行1: [角色] [装备] [宝石] [宠物]
行2: [时装] [商城] [排行] [PVP]
行3: [夺宝] [背包] [图鉴] [设置]
```

| 按钮 | Icon资源 | onClick | redDot条件 |
|------|---------|---------|-----------|
| Btn_Role | R_ICON_FUNC/role.png | Open(Character) | Player.hasNewAttr |
| Btn_Equip | R_ICON_FUNC/equip.png | Open(Equipment) | Player.hasNewEquip |
| Btn_Gem | R_ICON_FUNC/gem.png | Open(Gem) | Player.canUpgradeGem |
| Btn_Pet | R_ICON_FUNC/pet.png | Open(Pet) | Player.hasNewPet |
| Btn_Costume | R_ICON_FUNC/costume.png | Open(Costume) | Player.hasNewCostume |
| Btn_Shop | R_ICON_FUNC/shop.png | Open(Shop) | Shop.hasAffordable |
| Btn_Ranking | R_ICON_FUNC/ranking.png | Open(Ranking) | false |
| Btn_PVP | R_ICON_FUNC/pvp.png | Open(PVP) | PVP.isOpen |
| Btn_Lottery | R_ICON_FUNC/lottery.png | Open(Lottery) | Player.hasLotteryTicket |
| Btn_Inventory | R_ICON_FUNC/inventory.png | Open(Inventory) | Player.hasNewItem |
| Btn_Collection | R_ICON_FUNC/collection.png | Open(Collection) | Collection.hasNew |
| Btn_Settings | R_ICON_FUNC/settings.png | Open(Settings) | false |

---

## 交互逻辑

```
OnClick(Btn_XXX):
  NavigationStack.Push(targetPanel)

OnClick(CharSpine):
  CharSpine.PlayAnimation("interact", false)
  → 播完后回到 "idle_main"

OnResume():
  RefreshPower()
  RefreshCurrency()
  RefreshAllRedDots()
```

---

## 状态变化

| 条件 | 表现 |
|------|------|
| 战力变化 | PowerValue数字 A_NUM_ROLL 1.2s |
| 境界提升 | RealmLabel A_WAVE_BOUNCE + 粒子特效 |
| 金币/钻石变化 | 对应Value A_NUM_ROLL |
| 无操作60秒 | CharSpine随机播放 idle2 动画 |
| 战力着色 | PowerColorRule(见下方) |

---

## Logic Rules

```
PowerColorRule:
  if power < 100000 → C_POWER_1
  if power < 1000000 → C_POWER_2
  if power < 10000000 → C_POWER_3
  else → C_POWER_4

CharacterDisplay:
  OnOpen → skeleton.SetSkin(player.costume)
  OnOpen → skeleton.SetAttachment("wing_slot", player.wing)
```

---

## 数据源

| 字段 | 来源 | 刷新频率 |
|------|------|---------|
| VIP等级 | S2C_PlayerInfo.vipLevel | OnOpen/OnResume |
| 战力 | S2C_PlayerInfo.power | OnOpen/OnResume |
| 境界 | S2C_PlayerInfo.realm | OnOpen/OnResume |
| 金币 | S2C_PlayerInfo.gold | 30s轮询 + 消耗/获得时即刷 |
| 钻石 | S2C_PlayerInfo.diamond | 30s轮询 + 消耗/获得时即刷 |
| 角色外观 | S2C_PlayerInfo.costumeId + wingId | OnOpen |
| 红点状态 | 各子系统本地计算 | OnResume时批量刷新 |
