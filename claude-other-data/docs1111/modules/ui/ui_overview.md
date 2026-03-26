# UI 面板总览 — nxxhnm v2.0

> Requires: ui_theme.md
> 所有面板详细规格见 views/ 目录下对应文件。

---

## 面板清单

| # | 面板 | 文件名 | Canvas | 优先级 | 入口 |
|---|------|--------|--------|--------|------|
| 1 | 主界面(大厅) | view_main_lobby.md | L_HUD | P0 | 默认画面,12功能按钮 |
| 2 | 战斗HUD | view_battle_hud.md | L_BATTLE_HUD | P0 | GameState→battle |
| 3 | 角色面板 | view_character.md | L_PANEL | P1 | 主界面[角色] |
| 4 | 装备面板 | view_equipment.md | L_PANEL | P1 | 主界面[装备] |
| 5 | 宝石面板 | view_gem.md | L_PANEL | P1 | 主界面[宝石] |
| 6 | 宠物面板 | view_pet.md | L_PANEL | P1 | 主界面[宠物] |
| 7 | 时装面板 | view_costume.md | L_PANEL | P2 | 主界面[时装] |
| 8 | 商城面板 | view_shop.md | L_PANEL | P1 | 主界面[商城] |
| 9 | 排行面板 | view_ranking.md | L_PANEL | P2 | 主界面[排行] |
| 10 | PVP面板 | view_pvp.md | L_PANEL | P2 | 主界面[PVP] |
| 11 | 夺宝面板 | view_lottery.md | L_POPUP | P1 | 主界面[夺宝]/99币礼物 |
| 12 | 背包面板 | view_inventory.md | L_PANEL | P2 | 主界面[背包] |
| 13 | 图鉴面板 | view_collection.md | L_PANEL | P3 | 主界面[图鉴] |
| 14 | 设置面板 | view_settings.md | L_PANEL | P3 | 主界面[设置] |
| 15 | 弹幕提示条 | view_barrage_bar.md | L_BARRAGE | P0 | 常驻 |
| 16 | 加载画面 | view_loading.md | L_OVERLAY | P0 | 启动/场景切换 |
| 17 | 战斗结算 | view_battle_result.md | L_BATTLE_HUD | P1 | battle_end |
| 18 | 确认弹窗 | view_confirm_dialog.md | L_POPUP | P0 | 任何消耗操作 |
| 19 | Toast提示 | view_toast.md | L_TOAST | P0 | 任何提示 |
| 20 | 礼物特效层 | view_gift_effect.md | L_GIFT_EFFECT | P1 | 收到礼物 |
| 21 | 网络断连 | view_disconnect.md | L_OVERLAY | P0 | 断连>3s |

---

## 优先级说明

| 优先级 | 含义 | 面板数 |
|--------|------|--------|
| P0 | 无这些游戏无法运行 | 7 (主界面/战斗HUD/弹幕条/加载/确认弹窗/Toast/断连) |
| P1 | 核心付费和养成功能 | 8 (角色/装备/宝石/宠物/商城/夺宝/礼物特效/战斗结算) |
| P2 | 重要但非核心 | 4 (时装/排行/PVP/背包) |
| P3 | 可延后实现 | 2 (图鉴/设置) |

---

## 验收清单

### P0 验收
- [ ] 主界面12个按钮全部可点击+打开对应面板
- [ ] 所有面板基于S_RESOLUTION竖屏正确布局
- [ ] 弹幕提示条常驻底部滚动+穿透点击
- [ ] 加载画面进度条+状态文字正确
- [ ] Toast/确认弹窗通用组件可复用
- [ ] 网络断连提示正确显示+自动重连

### P1 验收
- [ ] 战斗HUD Boss血条多段正确显示
- [ ] 伤害飘字3种区分(普攻/暴击/技能)
- [ ] 装备面板9槽位可装备/卸下/强化
- [ ] 宝石面板6槽位+升级正确
- [ ] 礼物特效4档正确触发+不阻挡操作
- [ ] 战斗结算MVP+奖励展示+排名

### P2 验收
- [ ] 排行榜前3名大卡+4-100名列表
- [ ] 夺宝转盘旋转+停在中奖格+保底进度
- [ ] 时装面板35套网格+穿戴预览

### 通用验收
- [ ] 所有面板可正常打开/关闭(动画流畅)
- [ ] NavigationStack返回行为正确(Pop回来源)
- [ ] 所有颜色引用C_变量,无硬编码
- [ ] 所有按钮有A_BTN_PRESS反馈
