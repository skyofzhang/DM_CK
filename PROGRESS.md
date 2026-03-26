# 极地生存法则 (drscfz) — 开发进度存档

> 最后更新：2026-02-26 #123
> Unity 2022.3.47f1 | URP | 项目路径：D:\claude\drscfz

---

## 🤖 新对话接力指南（AI 必读，#123 最新）

> 新对话启动后，**先读完本节**再动手。所有上下文都在这里，不需要问用户。

### 📍 当前所在位置（Phase 2 · #123 UI修复已完成，待 Unity 编译执行 AutoRunFixSession123 + Play Mode 验证）

**M4 Worker动画系统全部完成**（代码+场景已就绪）：

| 任务 | 内容 | 文件 |
|------|------|------|
| T003 ✅ | StationSlot 注册表（5种工位×4槽=20个，场景已保存） | WorkerManager.cs |
| T004 ✅ | EaseOutCubic 平滑移动协程（替代 MoveTowards） | WorkerController.cs |
| T006 ✅ | StationSlot 数据结构（含运行时占用追踪） | WorkerManager.cs |
| T007 ✅ | HomePosition 待机位（工作结束后平滑返回待机圈） | WorkerController.cs |
| T008 ✅ | 就近分配算法（最近空闲Worker→最近可用槽） | WorkerManager.cs |
| T010 ✅ | Animator双参数集（集A: Speed/IsPushing；集B: IsRunning/IsMining/IsIdle，自动检测） | WorkerController.cs |
| T211 ✅ | work_command → AssignWork() 链路已实现 | SurvivalGameManager.cs |
| T212 ✅ | 资源产出由服务器推送，无需客户端定时器 | 架构决策 |
| T213 ✅ | HUD数字弹跳动画（资源增加时ShakeScale） | SurvivalTopBarUI.cs |
| T214 ✅ | Worker指派白色闪烁（TriggerAssignmentFlash，0.15s双闪） | WorkerVisual.cs + WorkerController.cs |
| T215 ✅ | WorkerController.State 枚举已存在 | WorkerController.cs |
| Editor ✅ | Fix Worker Mesh + Setup Station Slots（Coplay已执行） | Assets/Editor/ |

**Animator 参数集说明（重要，#122 更正）**：
- `CowWorker.prefab` 默认使用 `kuanggong_05.controller`（GUID: `67e25648...`，参数: `IsRunning/IsMining/IsSitting/IsCarrying/IsIdle`，全 bool）
- `WorkerController.CacheAnimatorParams()` 自动检测两套参数，`SetAnimatorState()` 兼容两套：
  - 集A（Speed float + IsPushing bool）适合 AC_Kpbl.controller
  - 集B（IsRunning/IsMining/IsIdle bool）适合 kuanggong_05.controller（CowWorker 实际用的）
- **不要创建新的 AnimatorController**，WorkerController 代码已正确适配现有 controller

**关键架构决策（不能推翻）**：
- `WorkerController.AssignWork(int cmdType, Vector3 targetPos)` — 已解耦，targetPos 由 WorkerManager 传入
- `StationSlot` 独立类，`_stationSlots[]`（20个，已序列化到场景）存在 WorkerManager 上
- `OnWorkComplete` 事件通知 WorkerManager 释放槽位（`_workerToSlot` Dictionary 追踪）

---

### ✅ Play Mode 验证清单（下一步：全流程联调）

```
✅ 场景中 Worker_00~19 各有 Body 子对象（CowWorker，Fix Worker Mesh 已执行）
✅ WorkerManager._stationSlots 有 20 个元素（场景文件已验证）
✅ 无编译错误（Coplay 确认 hasCompilationErrors: false）
✅ 服务器模拟礼物 ID 已修复（#119，T1-T5特效现可正常触发）
✅ giftData.contribution 别名已添加（排行榜积分计算正确）
✅ 全部7种礼物自测通过（#120）：
     仙女棒 T1 c=1 food=0 ✅
     能力药丸 T2 c=100 food=50 ✅
     甜甜圈 T3 c=500 food=100 gate=200 ✅
     能量电池 T4 c=1000 heat=30 ✅
     神秘空投 T5 c=5000 food=500 coal=200 ore=100 gate=300 ✅
     超能喷射 T3 c=500 ✅
     魔法镜 T2 c=0（设计如此，捣乱礼物）✅
✅ SurvivalSettlementPanel 已创建（AutoRunSetupSettlement.cs，#120）

——— 2026-02-25 18:30 Play Mode 全流程跑通（服务器日志确认）———
✅ 服务器收到 work_command（挖掘煤炭/添柴升温）
✅ 礼物 T1-T5 全部触发（仙女棒/药丸/甜甜圈/电池/空投/喷射/魔法镜）
✅ 随机事件：E03怪物来袭 / E04暖流涌现 / E01暴风雪
✅ 夜晚阶段：Night1 7只怪 × 3波次，城门HP: 1000→970
✅ Day2正常推进
✅ 游戏结束：lose/temp_freeze，day=2（完整结算触发）

——— 待用户反馈（Play Mode 视觉确认）———
□ Worker 白色双闪 + EaseOut 移向工位（视觉）
□ 到达工位 → Z轴摆动 + IsPushing=true
□ 工作完成 → 平滑返回待机圈（HomePosition）
□ HUD 数字 ShakeScale 弹跳
□ WorkerVisual 颜色对应工位
□ CowWorker 尺寸正常（若入地/偏小调 scale）
□ 礼物特效 T1-T5 动画/Banner/粒子（视觉）
□ 结算3屏序列（A:结果→B:数据→C:Top3/MVP）
```

---

### 🔜 下一个开发任务

| 优先 | 任务 | 说明 |
|------|------|------|
| P0 | Unity 聚焦窗口触发编译 | AutoRunSetupSettlement_v4（任务A/B）+ AutoRunFixSession123（任务A-D）自动执行 |
| P1 | Play Mode 再验证 | 验证：①排行榜/设置按钮可点 ②GM按钮在底部 ③贴纸面板可拖动不遮挡 ④礼物横幅中文名+效果 ⑤Worker持续工作 ⑥怪物城门外侧刷新 |
| P2 | SettlementUI _top3Slots 绑定 | C屏当前退化为MVP单行；需在Inspector中手动绑3个槽位 |
| P3 | 完整UI资源 | 用户正在准备（字体/图标等） |
| P3 | 音效素材填充 | 向 Resources/Audio/BGM/ 和 SFX/ 目录填入实际 .mp3/.wav 文件 |

---

### 📁 核心文件速查

| 文件 | 最后修改 | 内容摘要 |
|------|---------|---------|
| `Assets/Scripts/Survival/WorkerController.cs` | ✅ #115 | 状态机+协程移动+HomePosition+Animator(T010)+T214调用 |
| `Assets/Scripts/Survival/WorkerManager.cs` | ✅ #113 | StationSlot注册表+就近分配算法 |
| `Assets/Scripts/Survival/WorkerVisual.cs` | ✅ #115 | 颜色/发光/冻结视觉 + T214 TriggerAssignmentFlash() |
| `Assets/Scripts/Survival/SurvivalGameManager.cs` | ✅ #110 | Idle/Loading/Running/Settlement状态机 |
| `Assets/Scripts/UI/SurvivalIdleUI.cs` | ✅ #110 | 大厅界面（标题/已连接/开始按钮） |
| `Assets/Scripts/UI/SurvivalLoadingUI.cs` | ✅ #110 | Loading界面（Spinner+文字） |
| `Assets/Editor/FixWorkerMesh.cs` | ✅ #110 | 菜单：Capsule→CowWorker替换（已执行 #116）|
| `Assets/Editor/SetupStationSlots.cs` | ✅ #116 | 菜单：填充20个默认工位槽（已执行，DisplayDialog已移除）|
| `Assets/Editor/FixTMPFonts.cs` | ✅ #116 | 菜单：LiberationSans→ChineseFont SDF批量替换（已执行，替换95个）|
| `Assets/Animations/AC_Kpbl.controller` | 📖 参照 | CowWorker当前用（Speed+IsPushing+IsDead） |
| `docs/design_doc.md` | ✅ v3.0 | 权威策划案（§21.6已更新T010实际实现说明） |

---

### 🖥️ 游戏后端服务器连接

| 项目 | 值 |
|------|-----|
| **IP地址** | `101.34.30.65`（⚠️ CLAUDE.md 里 `212.64.26.65` 是卡皮巴拉项目，本项目IP不同！） |
| **SSH连接** | `ssh root@101.34.30.65`（无需密码，本机 `C:\Users\Administrator\.ssh\id_rsa` 已授权） |
| **PM2进程名** | `drscfz-server` |
| **服务器路径** | `/opt/drscfz/` |
| **WebSocket** | `ws://101.34.30.65:8081` |
| **部署脚本** | `D:\claude\drscfz\deploy.py` |

```bash
# 检查服务状态（在 Claude 对话 Bash 工具里直接执行）
ssh root@101.34.30.65 "pm2 status"

# 查看日志
ssh root@101.34.30.65 "pm2 logs drscfz-server --lines 50 --nostream"

# 重启
ssh root@101.34.30.65 "pm2 restart drscfz-server"

# 健康检查（无需SSH）
curl http://101.34.30.65:8081/health

# 本地改代码后一键部署
python D:\claude\drscfz\deploy.py
```

---

### ⚠️ 已知约束（不要试图绕过）

1. **Coplay 使用方式**：用 `create_coplay_task` 发任务（非对话模式）。任务较慢（30s~2min），MCP 会超时但 Unity 中照常执行。不要用 `execute_script`（Unknown tool）。Editor 工具**禁止使用 DisplayDialog**（会阻塞进程）。

2. **T011~T014 无法实现**：`AC_Kpbl.controller` 只有 Push 一个状态，无专属工作动画剪辑。需等美术提供 `.anim` 文件才能实现 5 种工作动画，当前 T010 已是最优解。

3. **Rule #8 场景保存**：必须用 `EditorSceneManager.SaveScene(scene)`，**禁止**用 Coplay `save_scene`（有路径 bug）。

4. **设计文档状态**：`docs/design_doc.md` v3.0（含 §20/§21/§22 完整规格），**不要自行修改数值**。

---

## ✅ 本次会话完成内容（#123）

### Play Mode 二轮测试反馈修复

**用户反馈（Play Mode 二轮）**：
1. ❌ 排行榜/设置点不开（`SurvivalRankingUI` 不在场景）
2. ❌ GM 4个测试按钮虽可用，但位置不在屏幕底部
3. ❌ 礼物贴纸介绍面板挡住战斗画面，需要可拖动 + 初始位置下移
4. ❌ 玩家加入/礼物弹窗：emoji 显示方块、礼物名/效果乱码、字体不清晰
5. ❌ 角色加入后站在原地不动；服务器模拟未包含玩家工作指令(1-4)

**修复内容**：

**① StickerPanelUI.cs — 修复 GameManager 引用**
- 原引用 `DrscfZ.Core.GameManager.Instance`（旧卡皮巴拉）→ 替换为 `SurvivalGameManager.Instance`
- `HandleStateChanged` 签名改为 `(SurvivalGameManager.SurvivalState newState)`

**② GiftNotificationUI.cs — 礼物横幅显示中文名+效果**
- 新增 `GetGiftDisplayName(giftId)` 映射（fairy_wand→仙女棒 等7种）
- 新增 `GetGiftEffect(giftId)` 映射（效果描述）
- `ShowBanner` 改为：`{nickname} 送出 {显示名}  [效果]`

**③ SurvivalRoom.js — 完整模拟脚本（覆盖玩家指令+礼物）**
- 8名玩家分批加入（冬日守卫甲~夜枭战士辛）
- 10轮持续工作指令（每轮5秒，4种cmd类型 1/2/3/4 全覆盖，不重复）
- 礼物7种全覆盖（T1-T5，不同发送玩家）
- t=28s 添加夜晚攻击指令 cmd=6（夜晚有效）
- 服务器已重启 ✅

**④ AutoRunFixSession123.cs（[InitializeOnLoad]，编译后自动执行）**
- 任务A：创建 `SurvivalRankingPanel`（全屏，10行预创建，TitleText/CloseBtn/EmptyHint）
         + 挂载 `SurvivalRankingUI` 到 Canvas（always-active）
         + 绑定 `SurvivalIdleUI._rankingPanel` + `_settingsPanel`
- 任务B：将 `GameControlUI` BottomBar 锚定到屏幕底部（anchorMin=(0,0) anchorMax=(1,0) h=120）
- 任务C：替换 `BroadcasterPanel` 中 emoji（⚡→"闪电"，🌊→"海浪"），避免方块字
- 任务D：将 `GiftInfoPanel` 移至右下角（anchoredPosition=(-20, 140)）

**修改文件**：
| 文件 | 修改 |
|------|------|
| `Assets/Scripts/UI/StickerPanelUI.cs` | GameManager → SurvivalGameManager |
| `Assets/Scripts/UI/GiftNotificationUI.cs` | 新增礼物名/效果映射，横幅中文化 |
| `Server/src/SurvivalRoom.js` | 完整8人模拟+10轮工作指令+7礼物 |
| `Assets/Editor/AutoRunFixSession123.cs` | 新建，4任务一次性自动修复 |

---

## ✅ 本次会话完成内容（#122）

### AutoRunSetupSettlement 清理 + WorkerController 参数集修正

**问题**：#121 创建的 `SetupWorkerAnimator.cs` 方向错误（被用户"你为什么不自测"发现）：
1. nn_01 动画为 Generic，CowWorker 用 Mixamo 骨骼 → 骨骼名不匹配，动画无法重定向
2. CowWorker.prefab **已有** `kuanggong_05.controller`（参数: `IsRunning/IsMining/IsIdle` bool）
3. WorkerController 只设 `Speed`/`IsPushing` → kuanggong_05 找不到 → 无任何动画状态切换

**正确修复（WorkerController.cs — #121完成）**：
- 新增参数集B：`_hasIsRunning`, `_hasIsMining`, `_hasIsCarrying`, `_hasIsIdle` + 对应 Hash
- `CacheAnimatorParams()` 自动检测两套参数（switch by nameHash）
- `SetAnimatorState(bool isMoving, bool isWorking)` 兼容两套：
  - 集A：Speed float + IsPushing bool（AC_Kpbl）
  - 集B：IsRunning/IsMining/IsIdle bool（kuanggong_05，CowWorker默认）

**#122 清理**：
- `AutoRunSetupSettlement.cs` Task C（`SetupWorkerAnimator.Execute()`）已移除，SessionKey → v4
- `SetupWorkerAnimator.cs` 已删除（创建不兼容的 Generic AC，错误方向）

**修改文件**：
| 文件 | 修改 |
|------|------|
| `Assets/Editor/AutoRunSetupSettlement.cs` | 移除 Task C，SessionKey 改为 v4 |
| `Assets/Editor/SetupWorkerAnimator.cs` | **已删除** |

---

## ✅ 本次会话完成内容（#121）

### Play Mode 测试反馈修复

**用户反馈（Play Mode 验证）**：
1. ✅ 全流程跑通（Work/Gift/Event/Settlement均触发）
2. ❌ Worker 只有移动动画，缺少整体动作循环（idle/work无限循环）→ WorkerController 双参数集修复（#121/#122）
3. ✅ 用户调整了摄像机位置（已保留，代码不触碰）
4. ❌ 怪物模型比例过大（需缩小10倍），刷新位置在堡垒内部（应在城门外侧）→ 已修复
5. 📦 用户正在准备完整UI资源

**修复内容**：

**① 怪物缩放（MonsterWaveSpawner.cs）**
- 新增 `[SerializeField] private float _monsterScale = 0.1f;` 字段（Inspector 可调）
- `SpawnOneMonster()` 实例化后立即应用缩放：`go.transform.localScale = Vector3.one * s;`
- 同时对 HPBarCanvas 子对象反向补偿（`localScale = Vector3.one / s`），保持血条比例正常

**② 怪物刷新位置（MonsterWaveSpawner.cs + SetupMonsterSystem.cs）**
- `GetSpawnPos()` fallback 从 `(0,0,9)` → `(0,0,-10)` (城门外侧)
- `SetupMonsterSystem.cs` 刷新点坐标从堡垒内（Z=6/10）→ 城门外侧：
  - `SpawnLeft`:  (-8, 0, -8)
  - `SpawnRight`: (8, 0, -8)
  - `SpawnTop`:   (0, 0, -10)
- 场景坐标参考：城门 Z=-4，堡垒 Z=0~8，外侧 Z < -4

**③ WorkerController 双参数集支持（核心修复）**
- 发现根因：kuanggong_05.controller 用 bool 参数（IsRunning/IsMining/IsIdle），WorkerController 只设 Speed/IsPushing → 完全不匹配
- 修复：新增参数集B检测 + SetAnimatorState 兼容两套（详见 #122）

**④ AutoRunSetupSettlement.cs（任务A+B，任务C已移除）**
- 任务A：创建 SurvivalSettlementPanel（如不存在）
- 任务B：修复怪物刷新点（移到城门外侧）
- ~~任务C~~：已移除（SetupWorkerAnimator 错误方向）
- 触发方式：Unity 编译后自动运行，执行完自删除

**新增/修改文件**：
| 文件 | 修改 |
|------|------|
| `Assets/Scripts/Monster/MonsterWaveSpawner.cs` | `_monsterScale` 字段 + 缩放逻辑 + fallback坐标修正 |
| `Assets/Editor/SetupMonsterSystem.cs` | 刷新点坐标 Z=6/10 → Z=-8/-10 |
| `Assets/Scripts/Survival/WorkerController.cs` | 双参数集支持（集A+集B）|

---

## ✅ 本次会话完成内容（#120）

### 全系统完整性验证 & 结算面板补全

**服务器自测（全部7种模拟礼物）— 全部通过**

| 礼物 | Tier | contribution | addFood | addHeat | addGateHp | 备注 |
|------|------|-------------|---------|---------|-----------|------|
| 仙女棒 | T1 | 1 | 0 | 0 | 0 | 效率加成(服务器端) ✅ |
| 能力药丸 | T2 | 100 | 50 | 0 | 0 | 召唤守卫+补给 ✅ |
| 甜甜圈 | T3 | 500 | 100 | 0 | 200 | 城门修复+补给 ✅ |
| 能量电池 | T4 | 1000 | 0 | 30 | 0 | 炉温+30℃ ✅ |
| 神秘空投 | T5 | 5000 | 500 | 0 | 300 | 超级补给(+coal=200 +ore=100) ✅ |
| 超能喷射 | T3 | 500 | 0 | 0 | 0 | 效率×2(服务器端) ✅ |
| 魔法镜 | T2 | 0 | 0 | 0 | 0 | 捣乱礼物，贡献=0 ✅（设计如此）|

**PlayMode联调前置检查（代码审查确认）**：
- `GameControlUI.cs` — `toggle_sim` ✅ + F5/F6/F8快捷键 ✅
- `NetworkManager.cs` — `heartbeat_ack` 双类型接受 ✅
- `SurvivalGameManager.HandleGameEnded()` — `SettlementData` 映射正确 ✅
- `_buildRankings()` — `contribution: score` 字段正确 ✅
- `dayssurvived`（全小写）与服务器广播字段一致 ✅

**SurvivalSettlementPanel 创建**（#120）：
- 发现 `SetupSettlementUI.cs` 从未在场景执行（PROGRESS 未记录）
- 创建 `AutoRunSetupSettlement.cs`（`[InitializeOnLoad]`，自删除）
- Unity 检测到新脚本后将自动执行 `SetupSettlementUI.Execute()`，创建3屏结算面板
- ⚠️ C屏 `_top3Slots` 尚未在Inspector绑定（当前退化为MVP单行，待P2修复）

**服务器状态**：online，错误日志为空，运行稳定 ✅

---

## ✅ 本次会话完成内容（#119）

### 服务器 Bug 修复：模拟礼物 & 数据字段对齐

**问题 1：_runSimulation() 使用错误礼物价格 → T2-T5特效不触发**

- **根因**：`SurvivalRoom.js` 模拟用价格 `[1, 30, 150, 400, 1000, 3000, 6000]`（均不在礼物表中），服务器日志显示 `unknown gift: sim_gift, fallback food bonus`
- **修复**：改为使用实际礼物 ID `fairy_wand/ability_pill/donut/energy_battery/mystery_airdrop/super_jet/magic_mirror`

**问题 2：`findGiftById()` 无法匹配内部ID**

- **根因**：`findGiftById()` 按 `douyin_id` 查（均为 'TBD'），模拟用内部ID找不到礼物
- **修复**：在 `SurvivalGameEngine.js` `handleGift()` 中添加第二层查找 `getGift(giftId)`（按内部ID）

**问题 3：服务器 `giftData` 字段与客户端 `SurvivalGiftData` 不对齐**

- **根因**：服务器发 `score`，客户端期望 `contribution`；服务器资源加成放在嵌套 `effects` 对象中，客户端期望扁平的 `addFood`/`addCoal` 等字段
- **修复**：在 `giftData` 中添加 `contribution: gift.score` 别名，以及扁平化资源字段 `addFood/addCoal/addOre/addHeat/addGateHp`

**已修复文件**（服务器 + 本地 Server/src/）：
| 文件 | 修改 |
|------|------|
| `Server/src/SurvivalRoom.js` | `_runSimulation()` 改用实际礼物ID（T1→T5→T3→T2）|
| `Server/src/SurvivalGameEngine.js` | import 添加 `getGift`；`handleGift` 添加内部ID查找层；`giftData` 添加 `contribution` 别名和扁平资源字段 |

**服务器已重启**：`pm2 restart drscfz-server` ✅

### 全局代码审查（#119）

已验证以下系统全部正确且完整：
- `SurvivalConnectUI.DoConnect()` → `SurvivalGameManager.ConnectToServer()` ✅
- `GiftNotificationUI` T1-T5 所有特效和横幅队列 ✅
- `SetupGiftCanvas.cs` 创建完整 Gift_Canvas 结构并自动绑定所有字段 ✅
- `SurvivalLiveRankingUI` 订阅 SGM 事件，3秒刷新 Top5 ✅
- `RankingSystem.GetTopN(5)` 自动订阅 SGM，按贡献值降序排列 ✅
- `WorkerManager` 全部方法：SpawnWorker/AssignWork/ActivateAllWorkersGlow/Frozen/Pause/Resume/ClearAll ✅
- `AnnouncementUI.ShowAnnouncement()` ✅
- `SurvivalDataTypes.cs` 所有序列化类与服务器协议对齐 ✅
- 服务器 PM2 在线，80分钟无错误运行（17:25 Play Mode 测试成功：游戏启动→模拟开启→工作指令→礼物特效）

---

## ✅ 本次会话完成内容（#117~#118）

### M5 特效/相机/UI 系统全部就绪

| 系统 | 文件 | 状态 |
|------|------|------|
| 礼物特效 T1-T5 | GiftNotificationUI.cs + SetupGiftCanvas.cs | ✅ 场景已创建 |
| 相机震屏 | SurvivalCameraController.cs | ✅ 已挂载 Main Camera |
| 实时贡献榜 | SurvivalLiveRankingUI.cs | ✅ 已挂载 Canvas |
| 冻结状态横幅 | FrozenStatusUI.cs + SetupFrozenUI.cs | ✅ 场景已创建 |
| 比赛公告 | AnnouncementUI.cs | ✅ 已存在（复用） |
| 设置面板 | SurvivalSettingsUI.cs + SetupSurvivalSettings.cs | ✅ 功能完整 |

### SurvivalSettingsUI 完整实现（#118）

**文件**：`Assets/Scripts/UI/SurvivalSettingsUI.cs`

重写为完整功能版本：
- `public static SurvivalSettingsUI Instance` — 单例，供外部直接调用
- `SyncFromAudioManager()` — 每次面板打开时从 `AudioManager.Instance` 读取真实状态
- **AudioManager 集成**：设置音量时调用 `am.BGMVolume = value`（自动写入 PlayerPrefs）
- **BGM/SFX 开关按钮**：`ToggleBGM()` / `ToggleSFX()` — 切换静音状态，🔊/🔇 图标更新
- **滑条灰化**：静音时滑条 `interactable = false`
- **回退模式**：无 AudioManager 时回退到直接控制 `_bgmSource`/`_sfxSource`

**文件**：`Assets/Editor/SetupSurvivalSettings.cs`（新建）
- 菜单：`Tools → DrscfZ → Setup Survival Settings Panel`
- 创建 `SurvivalSettingsPanel`（480×420，居中，初始 inactive）
- 面板结构：Background + TitleText + CloseBtn + BGMRow + SFXRow + Divider + VersionText
- 每行含：Label + ToggleBtn(🔊) + Slider(冰蓝fill) + ValueText(%数字)
- 挂载 `SurvivalSettingsUI` 到 Canvas，自动绑定所有 [SerializeField] 字段
- **Coplay 已执行**：面板创建完毕，字段绑定完毕，场景已保存 ✅

**SurvivalIdleUI 已有完整的入口链路**（无需修改）：
```
SettingsBtn → OnSettingsClicked() → FindObjectOfType<SurvivalSettingsUI>(true)?.TogglePanel()
```

### 服务器修复（#117）

- `SurvivalRoom.js`：添加 `case 'leave_room': break;` — 消除 Unknown client message 告警
- 服务器已重启，状态 online ✅

---

## M4 完成情况汇总（#116 ✅）

---

## ✅ 本次会话完成内容（#115~#116）

### 0. M4 收尾自动化（#116）

**Editor 菜单 Coplay 执行**：
- `Tools/DrscfZ/Run Fix Worker Mesh NOW` → Worker_00~19 已有 Body 子对象（此前已执行）
- `Tools/DrscfZ/Setup Station Slots (Default 5×4=20)` → 20个工位槽已序列化到场景

**Editor 脚本清理**（99个→80个）：
- 删除废弃：BattleUIFix42~57（15个）、BattleUIFixRound2、BuildWindows、BuildWindowsAsync、CopyNetworkFiles（共19个+meta）
- 移除所有 DisplayDialog 阻塞调用（SetupStationSlots.cs）

**TMP 字体修复**（`Assets/Editor/FixTMPFonts.cs`）：
- 扫描场景全部 TMP_Text，将 LiberationSans SDF / null → ChineseFont SDF
- 替换 95 个组件，场景已保存，Console Warning 大幅减少

**编译状态**：`hasCompilationErrors: false` ✅

---

## ✅ 本次会话完成内容（#110~#114）

### 1. 大厅 + Loading 状态机

**文件**：`Assets/Scripts/Survival/SurvivalGameManager.cs`
- 新增 `Loading` 状态：`enum SurvivalState { Idle, Loading, Running, Settlement }`
- 新增 `public bool IsEnteringScene` 属性（true=进入游戏, false=退出回大厅）
- `RequestStartGame()`：`Idle → Loading(entering) → 发 start_game → 等服务器 survival_game_state(day/night) → Running`
- `RequestExitToLobby()`：`Running → Loading(exiting) → 发 reset_game → 等服务器 survival_game_state(idle) → ResetAll → Idle`
- `LoadingTimeout()` 协程：15 秒超时保护，强制回 Idle

**文件**：`Assets/Scripts/UI/SurvivalIdleUI.cs`
- 新增 `_rankingBtn`, `_settingsBtn`, `_serverStatus`, `_titleText` 字段
- 大厅仅在 `Idle + Connected` 时显示
- `_serverStatus.text = isConnected ? "已连接" : "连接中..."`

**文件**：`Assets/Scripts/UI/SurvivalLoadingUI.cs`（新建）
- 挂在 Canvas（always-active，Rule#7）
- `State == Loading` 时显示 LoadingPanel
- `IsEnteringScene=true` → "准备进入战场..."，`false` → "正在退出，返回大厅..."
- Spinner 旋转 270°/s

### 2. Canvas 场景结构（已创建并绑定）

```
Canvas
├── ConnectPanel          (已有)
├── LobbyPanel            ← 新增，inactive，全屏深蓝
│   ├── TitleText         "极地生存法则"（TMP，40px）
│   ├── ServerStatus      "已连接"（TMP，22px，绿色）
│   ├── StatusText        "等待主播开始游戏..."（TMP，20px）
│   ├── StartBtn          "▶ 开始玩法" → RequestStartGame()
│   ├── RankingBtn        "排行榜" → 切换 RankingPanelUI
│   └── SettingsBtn       "设置" → 占位
├── LoadingPanel          ← 新增，inactive，全屏黑遮罩
│   ├── LoadingText       (TMP)
│   └── Spinner           (Image，旋转)
├── GameUIPanel           ← 新增，inactive，全屏容器
│   └── ExitBtn           右上角红色，onClick → RequestExitToLobby()
├── GameplayUI            (已有游戏内 HUD)
└── ...其余面板
```

**SurvivalIdleUI 字段绑定**（全部已绑）：
```
_panel        → Canvas/LobbyPanel
_startBtn     → Canvas/LobbyPanel/StartBtn
_rankingBtn   → Canvas/LobbyPanel/RankingBtn
_settingsBtn  → Canvas/LobbyPanel/SettingsBtn
_statusText   → Canvas/LobbyPanel/StatusText
_serverStatus → Canvas/LobbyPanel/ServerStatus
_titleText    → Canvas/LobbyPanel/TitleText
```

**SurvivalLoadingUI 字段绑定**（全部已绑）：
```
_panel        → Canvas/LoadingPanel
_loadingText  → Canvas/LoadingPanel/LoadingText
_spinner      → Canvas/LoadingPanel/Spinner
```

✅ **Play Mode 截图验证通过**：标题/已连接/等待主播/开始玩法/排行榜/设置 全部正常显示

### 3. Worker 角色替换（代码就绪，待执行）

**根本原因**：`Worker_00~19` 使用 Capsule 占位符（MeshFilter fileID:10208），未使用 CowWorker.prefab

**CowWorker.prefab**：
- 路径：`Assets/Prefabs/Characters/CowWorker.prefab`
- 含 SkinnedMeshRenderer（子对象 `tripo_node_8a422929`）
- Mixamo 骨骼（`mixamorig:Hips` 等）+ Animator 组件
- 约 2m 高，AABB ~2.06×2.02×1.89

**已修复文件**：`Assets/Scripts/Survival/WorkerVisual.cs`
```csharp
// Awake() 改为：
_bodyRenderer = GetComponent<Renderer>()
             ?? GetComponentInChildren<Renderer>(true); // 支持子对象 SkinnedMeshRenderer
```

**待执行脚本**：`Assets/Editor/FixWorkerMesh.cs`
```
菜单：Tools → DrscfZ → Fix Worker Mesh (Capsule → CowWorker)
操作：移除每个 Worker_* 的 MeshFilter+MeshRenderer → 实例化 CowWorker.prefab 为子对象 "Body" → 保存场景
```

---

## 📁 本次新增/修改文件列表

| 文件 | 类型 | 状态 |
|------|------|------|
| `Assets/Scripts/Survival/SurvivalGameManager.cs` | 修改 | ✅ |
| `Assets/Scripts/UI/SurvivalIdleUI.cs` | 修改 | ✅ |
| `Assets/Scripts/UI/SurvivalLoadingUI.cs` | 新建 | ✅ |
| `Assets/Scripts/Survival/WorkerVisual.cs` | 修改 | ✅ |
| `Assets/Scripts/Survival/WorkerController.cs` | 修改 | ✅ T003/T004/T007/T010 |
| `Assets/Scripts/Survival/WorkerManager.cs` | 修改 | ✅ T006/T008 |
| `Assets/Editor/SetupStationSlots.cs` | 新建 | ✅ 待执行 |
| `Assets/Editor/FixWorkerMesh.cs` | 新建 | ⏳ 待执行 |
| `Assets/Editor/WireUIFields.cs` | 新建 | ✅ 已执行 |
| `Assets/Editor/FixLobbyTextComponents.cs` | 新建 | ✅ 已执行 |

---

## 🏗️ 状态机流程图

```
[Play Mode 启动]
      ↓
[ConnectPanel] ← 自动连接
      ↓ OnConnected
[LobbyPanel = Idle状态]
  "极地生存法则" + "▶ 开始玩法"
      ↓ 主播点击"▶ 开始玩法"
[LoadingPanel "准备进入战场..."]
  发 start_game → 等服务器 survival_game_state(day/night)
      ↓ 服务器确认
[游戏UI = Running状态]
      ↓ 主播点击 ExitBtn
[LoadingPanel "正在退出..."]
  发 reset_game → 等服务器 survival_game_state(idle)
      ↓ 服务器确认 / 15s超时
[LobbyPanel = Idle状态] ← 回大厅
```

---

## 📐 AI 开发准则遵守

| Rule | 内容 | 状态 |
|------|------|------|
| #1 服务器权威 | Loading 等服务器确认，不本地跳转 | ✅ |
| #2 UI预创建 | 所有面板 Editor 脚本预建，不运行时 Instantiate | ✅ |
| #7 脚本挂载 | SurvivalLoadingUI 挂 Canvas（always-active） | ✅ |
| #8 场景保存 | EditorSceneManager.SaveScene()，不用 Coplay save_scene | ✅ |

---

## 🔧 工具坑记录（本次踩坑）

1. **execute_script 超时** → Coplay MCP 工具全部变 "Unknown tool"，只剩 set_unity_project_root 可用
   - 解法：重启 Claude 对话，或手动在 Unity Editor 运行菜单工具

2. **create_ui_element("text") 创建 UI.Text 而非 TMP** → SerializedField 类型不匹配，绑定为 null
   - 解法：FixLobbyTextComponents.cs 批量转换

3. **"✓" 字符不在 LiberationSans SDF 字体** → 显示为方块
   - 解法：改为 "已连接"（去掉 ✓）

---

## ✅ 本次会话完成内容（#117）

### M5 特效/相机/UI系统

| 任务 | 文件 | 说明 |
|------|------|------|
| ✅ Bug1修复 | GameControlUI.cs | 模拟消息改为 `toggle_sim {enabled}` |
| ✅ Bug2修复 | NetworkManager.cs | 心跳接受 `heartbeat_ack` |
| ✅ TestSimulate.cs | Assets/Editor/ | F5/F6快捷键+Editor菜单模拟 |
| ✅ SurvivalCameraController.cs | Assets/Scripts/Survival/ | Perlin震屏+事件自动订阅(昼夜/城门/礼物) |
| ✅ SurvivalLiveRankingUI.cs | Assets/Scripts/UI/ | 游戏内实时贡献榜(Top5,3s刷新) |
| ✅ SetupSurvivalUI.cs | Assets/Editor/ | Editor工具：挂载相机控制器+创建LiveRankingPanel(已执行✅) |
| ✅ GiftNotificationUI接入 | SurvivalGameManager.cs | HandleGift→ShowGiftEffect(T1-T5)+相机震屏 |
| ✅ SetupGiftCanvas.cs | Assets/Editor/ | Editor工具：创建Gift_Canvas完整T1-T5面板结构 |
| ✅ FrozenStatusUI.cs | Assets/Scripts/UI/ | 冻结状态横幅UI(倒计时+淡入淡出) |
| ✅ SetupFrozenUI.cs | Assets/Editor/ | Editor工具：创建FrozenStatusPanel场景结构 |

**Editor 菜单已执行（Coplay确认）**：
```
✅ Tools → DrscfZ → Setup Gift Canvas (T1-T5 Effects)  — T2/T3/T4/T5面板+BannerQueue已创建，字段已绑定
✅ Tools → DrscfZ → Setup Frozen UI (Status Panel)     — FrozenStatusPanel已创建，FrozenStatusUI已挂载
✅ Tools → DrscfZ → Setup Survival UI (LiveRanking+Camera) — LiveRankingPanel+SurvivalCameraController已就绪
```

---

## 📋 待办（按优先级）

- [x] **已完成**：Unity 执行 `Setup Gift Canvas` → Gift_Canvas T1-T5面板已创建
- [x] **已完成**：Unity 执行 `Setup Frozen UI` → FrozenStatusPanel已创建
- [ ] **P1**：Play Mode 全流程验证（大厅→进入→游戏→礼物特效→相机震屏→WorkerFrozen）
- [ ] **P1**：GiftNotificationUI 字段绑定验证（T1-T5 ParticleSystem/Image/Slider Inspector确认）
- [ ] **P2**：AnnouncementUI 验证（公告文字正常显示）
- [x] **已完成**：SurvivalCameraController 挂载 Main Camera（SetupSurvivalUI 已执行）
- [x] **已完成**：LiveRankingPanel 创建到 Canvas/GameUIPanel（SetupSurvivalUI 已执行）
- [x] **已完成 T213**：HUD数字弹跳动画（资源增加时ShakeScale）
- [x] **已完成 T214**：Worker指派白色闪烁（双闪0.35s）
- [ ] **P3（等美术）**：T011~T014 五种工作专属动画（采食/挖煤/挖矿/添柴/攻击）
- [ ] **P3**：设置面板功能实现（目前占位）
- [ ] **P3**：音效素材填充（AudioManager框架已就绪，Resources/Audio/BGM/ 目录需 .mp3/.wav 文件）

---

## 🌐 项目关联资源

- **Notion 知识库**：https://www.notion.so/2fdba9f99f5881dda656edc4104b3ae3
- **开发进度页**：Page ID `30dba9f9-9f58-810f-854f-eb13637c18aa`（本次更新 #110）
- **服务器**：212.64.26.65，PM2 进程：`dm-drscfz-server`（待确认进程名）
