# 商城面板 — view_shop

> Requires: ui_theme.md
> Canvas: L_PANEL (24)
> Priority: P1
> CachePolicy: Normal
> Entry: MainLobby[商城]按钮
> Exit: @BackButton → MainLobby
> Animation: A_PANEL_IN / A_PANEL_OUT
> ShowCondition: 始终可用

---

## 元素表

### 容器层

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| BgFull | Image | root | (0,0)→(1,1) | 0,0,0,0 | full | C_BG_DEEP | 背景 |
| TitleBar | @TitleBar | root | — | — | — | — | "商城" |
| BackBtn | @BackButton | TitleBar | — | — | — | — | — |
| CurrencyBar | Panel | root | (0,1)→(1,1) | 0,-S_TITLEBAR_H-64,0,-S_TITLEBAR_H | 1080×64 | C_BG_PANEL | 积分栏 |
| TabBar | Panel | root | (0,1)→(1,1) | 0,-S_TITLEBAR_H-128,0,-S_TITLEBAR_H-64 | 1080×64 | — | Tab栏 |
| ShopScroll | ScrollRect | root | (0,0)→(1,1) | 0,80,0,-S_TITLEBAR_H-128 | fill | vertical | 商品列表 |
| RefreshBar | Panel | root | (0,0)→(1,0) | 0,0,0,80 | 1080×80 | C_BG_BAR | 刷新倒计时栏 |

### CurrencyBar 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| PointIcon | Image | CurrencyBar | (0,0.5) | 24,0 | 40×40 | — | 积分图标(道具20013) |
| PointValue | TMP | CurrencyBar | (0,0.5) | 72,0 | 200×32 | F_NUM C_TITLE | 积分数量 |
| BtnGetPoint | Button | CurrencyBar | (1,0.5) | -180,0 | 160×48 | F_SMALL C_TEXT_WHITE C_BG_CARD | "获取积分" |

### TabBar 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| Tab_Material | Button | TabBar | (0.167,0.5) | 0,0 | 300×56 | F_BTN | "材料" |
| Tab_Fragment | Button | TabBar | (0.5,0.5) | 0,0 | 300×56 | F_BTN | "碎片" |
| Tab_Special | Button | TabBar | (0.833,0.5) | 0,0 | 300×56 | F_BTN | "特殊" |
| TabIndicator | Image | TabBar | (0,0)→(0,0) | 动态 | 120×3 | C_TITLE | 选中下划线(Lerp跟随) |

### ShopGrid 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| GridLayout | GridLayout | ShopScroll | (0,1)→(1,1) | 16,0,-16,0 | 1048 | cols=2, cell=516×160, gap=16 | 2列网格 |

### ShopItem 模板

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| ItemBg | Image | ShopItem | (0,0)→(1,1) | 0,0,0,0 | 516×160 | C_BG_CARD 圆角8 | 商品卡底 |
| ItemIcon | Image | ShopItem | (0,0.5) | 16,0 | 80×80 | @QualityBorder | 商品图标 |
| ItemName | TMP | ShopItem | (0,1) | 112,-16 | 260×32 | F_BODY C_TEXT_WHITE | 商品名 |
| ItemPrice | Panel | ShopItem | (0,0.5) | 112,-8 | 200×28 | — | 价格区 |
| Price/Icon | Image | ItemPrice | (0,0.5) | 0,0 | 24×24 | — | 积分小图标 |
| Price/Value | TMP | ItemPrice | (0,0.5) | 28,0 | 100×28 | F_NUM C_TITLE | 价格数值 |
| ItemRemain | TMP | ShopItem | (1,1) | -16,-16 | 120×22 | F_SMALL C_TEXT_DIM | "剩余 {n}/{max}" |
| BtnExchange | Button | ShopItem | (1,0) | -16,16 | 140×56 | F_SMALL C_TEXT_WHITE C_POSITIVE渐变 | "兑换" |
| SoldOutLabel | TMP | ShopItem | (1,0) | -16,16 | 140×56 | F_SMALL C_TEXT_DIM | "已售罄"(互斥BtnExchange) |

### RefreshBar 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| RefreshLabel | TMP | RefreshBar | (0.5,0.5) | 0,0 | 400×28 | F_SMALL C_TEXT_DIM | "每日0点刷新 {HH:MM:SS}" |

---

## 交互逻辑

```
OnOpen(data):
  PointValue.text = player.shopPoints
  currentTab = 0
  SwitchTab(0)

OnClick(Tab_X):
  SwitchTab(X.index)

SwitchTab(idx):
  currentTab = idx
  TabIndicator.LerpTo(Tab[idx].x, 0.2s)
  foreach tab: tab.color = C_TEXT_DIM
  Tab[idx].color = C_TITLE
  RefreshList(shopData.categories[idx])

RefreshList(items):
  GridLayout.Clear()
  foreach item in items:
    card = Instantiate(ShopItem)
    card.ItemName.text = item.name
    card.ItemIcon.sprite = Load(item.iconPath)
    card.Price/Value.text = item.price
    card.ItemRemain.text = "剩余 {item.remain}/{item.dailyLimit}"
    if item.remain <= 0:
      card.BtnExchange.SetActive(false)
      card.SoldOutLabel.SetActive(true)
    else:
      card.BtnExchange.SetActive(true)
      card.SoldOutLabel.SetActive(false)

OnClick(BtnExchange):
  if player.shopPoints < item.price:
    Toast.Show("积分不足") + BtnExchange.Shake(0.2s)
  else:
    ConfirmDialog.Show("花费{price}积分兑换{name}?", onConfirm=S2C_ShopBuy)

OnBuyResult(data):
  PointValue.text = data.newPoints
  PointValue.Play(A_NUM_ROLL)
  item.remain -= 1
  RefreshItem(item)
  Toast.Show("兑换成功!")

OnClick(BtnGetPoint):
  Toast.Show("通过礼物和战斗获取积分")

OnTimerTick():
  RefreshLabel.text = "每日0点刷新 " + FormatCountdown(nextMidnight)
```

---

## 状态变化

| 条件 | 表现 |
|------|------|
| 切换Tab | TabIndicator Lerp滑动 + 列表刷新 |
| 积分不足 | 按钮抖动 + Toast |
| 兑换成功 | 积分数字 A_NUM_ROLL + Toast |
| 售罄 | BtnExchange隐藏 → SoldOutLabel显示 |
| 积分变化 | PointValue A_NUM_ROLL |

---

## Logic Rules

```
ShopPriceTable:
  | 商品     | 价格  | 每日上限 | Tab   |
  | 突破石   | 10-20 | 10次    | 材料  |
  | 强化石   | 10-20 | 10次    | 材料  |
  | 附魔石   | 800   | 1次/周  | 材料  |
  | 宠物碎片 | 50    | 5次     | 碎片  |
  (完整配表见 ShopTable.json)
```

---

## 数据源

| 字段 | 来源 | 刷新频率 |
|------|------|---------|
| 积分余额 | S2C_PlayerInfo.shopPoints | OnOpen/兑换后 |
| 商品列表 | S2C_ShopList | OnOpen/切Tab |
| 剩余次数 | S2C_ShopList[i].remain | 兑换后 |
| 刷新倒计时 | 本地计算(距0点) | 每秒 |
