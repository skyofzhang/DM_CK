# 极地生存法则 (drscfz) — DM_CK

## ⚡ 新会话启动指令（必须优先执行）
> 新对话框打开后，在做任何任务之前，必须按顺序执行：
> 1. 读取项目记忆: `C:\Users\Administrator\.claude\projects\E--AIProject-DM-CK\memory\MEMORY.md`
> 2. 确认 Coplay MCP 可用（调用 `get_unity_editor_state`，若多 Unity 实例先调 `set_unity_project_root` 设为 `E:\AIProject\DM_CK`）
> 3. 编译检查（`check_compile_errors`）
> 4. 向用户汇报当前状态 + 待完成列表，等待指示
> 5. 若用户请求"Multi-Agent 流程跑 §XX"或类似 → 读取 `docs/multi_agent_workflow.md` 按流程执行（PM 由主对话担任）

## 项目概述
抖音直播间互动游戏：观众刷礼物让矿工采集资源、抵御怪物入侵，坚持到天亮即胜利。
- Unity 2022.3.47f1 + URP
- Node.js WebSocket 服务器
- 目标平台：抖音直播互动插件

## 服务器
- **IP**: `101.34.30.65`（⚠️ `212.64.26.65` 是旧项目，不要用）
- **PM2 进程**: `drscfz-server`
- **服务器文件路径**: `/opt/drscfz/src/`（⚠️ 不是 `/opt/drscfz/Server/src/`）
- **WebSocket**: `ws://101.34.30.65:8081`
- **SSH 私钥**: `D:\共享\AI项目资料\id_rsa_transfer`
- **部署命令**:
  ```
  scp -i "D:/共享/AI项目资料/id_rsa_transfer" -o StrictHostKeyChecking=no "E:/AIProject/DM_CK/Server/src/SurvivalGameEngine.js" root@101.34.30.65:/opt/drscfz/src/
  ssh -i "D:/共享/AI项目资料/id_rsa_transfer" -o StrictHostKeyChecking=no root@101.34.30.65 "pm2 restart drscfz-server"
  ```

---

## 架构

### Unity 脚本结构
```
Assets/Scripts/
├── Survival/
│   ├── SurvivalGameManager.cs   — 状态机(Idle/Loading/Running/Settlement)
│   │                              处理 phase_changed / worker_died / worker_revived / live_ranking
│   ├── WorkerController.cs      — Worker状态机+协程移动(EaseOutCubic)
│   │                              State: Idle/Move/Work/Special/Frozen/Dead
│   │                              HP条：绿→黄→红渐变，Lerp平滑动画
│   ├── WorkerManager.cs         — StationSlot注册表+就近分配+EnterNightDefense/ExitNightDefense
│   ├── WorkerVisual.cs          — 颜色/发光/冻结/TriggerAssignmentFlash
│   └── SurvivalDataTypes.cs     — 消息数据结构（含LiveRankingEntry/LiveRankingData）
├── Monster/
│   ├── MonsterController.cs     — 状态机(Moving/Attacking/Dead)
│   │                              优先攻击最近存活Worker，无Worker时攻城门
│   │                              HP条：红色，Lerp平滑动画
│   └── MonsterWaveSpawner.cs    — 怪物波次生成，maxAliveMonsters=15上限
├── Core/
│   └── NetworkManager.cs        — WebSocket 通信
└── UI/
    ├── SurvivalIdleUI.cs / SurvivalLoadingUI.cs
    ├── GiftNotificationUI.cs    — 6种礼物横幅+效果描述
    ├── GiftAnimationUI.cs       — 礼物动画效果
    ├── SurvivalSettingsUI.cs    — BGM/SFX音量控制
    ├── SurvivalLiveRankingUI.cs — 实时贡献榜Top5（挂在GameUIPanel，服务器推送）
    └── FrozenStatusUI.cs        — 冻结状态横幅
```

### Node.js 服务器
```
Server/src/
├── SurvivalGameEngine.js   — 游戏引擎（状态机/资源/礼物/昼夜）
├── SurvivalRoom.js         — 游戏房间（含完整8人模拟+10轮工作+6礼物）
├── index.js                — 入口
└── ...（其余沿用多房间架构）
```

---

## 角色模型系统（kuanggong，2026-03-16更新）

### 源模型路径
```
Assets/Res/DGMT_data/
├── Model_juese(use)/kuanggong/   — 5个预制好的源 Prefab（含骨骼+材质）
│   ├── kuanggong_01.prefab ~ 05.prefab
└── Model_yuanwenjian/部落守卫者/  — 原始 FBX 动画文件
    ├── kuanggong_01/ (Attack.fbx / Idle.fbx / Sitting Dazed.fbx)
    ├── kuanggong_02~05/ (同结构)
    └── 武器: pickaxe+3d+model.fbx / hammer.fbx 等（面数<500，跳过减面）
```

### 游戏用 Prefab 路径
```
Assets/Prefabs/
├── Characters/
│   ├── KuanggongWorker_01.prefab  — 矿工外观A（kuanggong_01，scale=0.015）
│   └── KuanggongWorker_02.prefab  — 矿工外观B（kuanggong_02，scale=0.015）
└── Monsters/
    ├── KuanggongMonster_03.prefab  — 普通怪物A（scale=0.01）
    ├── KuanggongMonster_04.prefab  — 普通怪物B（scale=0.01）
    └── KuanggongBoss_05.prefab     — Boss（scale=0.018，MonsterType=Boss）
```

### Animator Controller 参数（5套 controller 结构相同）
- 状态: `Idle / Run / Attack / Bankuang_run / Sit`
- Bool 参数: `IsIdle / IsRunning / IsMining / IsSitting / IsCarrying`
- 死亡播放状态: `Sit`（无 Dead 状态，用 Sitting Dazed 替代）
- WorkerController 使用 IsRunning/IsMining，完全兼容

### Worker Prefab 结构
```
KuanggongWorker_XX (root)
  ├── WorkerVisual (组件)
  ├── WorkerController (组件，运行时 Initialize 时自动查找子组件)
  ├── Mesh/ (kuanggong_XX 骨骼+SMR，scale=0.015，阴影关闭)
  ├── NameTag/ (World Space Canvas，头顶名字)
  └── HPBarCanvas/ (World Space Canvas，默认隐藏，夜晚显示，绿→黄→红渐变)
```

### WorkerPool（场景预创建）
- `WorkerPool/Worker_00 ~ Worker_11`（共12个，MAX_WORKERS=12）
- 全部基于 KuanggongWorker_01/02 交替
- 通过 `ReplaceWorkerPool.cs` 重建：`Tools → DrscfZ → Replace Worker Pool`

### Scale / Y 偏移参考
| 角色 | scale | srcMinY | yOffset | 游戏高度 |
|------|-------|---------|---------|---------|
| Worker 01 | 0.015 | -13.83 | 0.207 | ~1.8m |
| Worker 02 | 0.015 | -14.95 | 0.224 | ~1.8m |
| Monster 03 | 0.01 | -12.74 | 0.127 | ~1.2m |
| Monster 04 | 0.01 | -14.52 | 0.145 | ~1.2m |
| Boss 05 | 0.018 | -16.76 | 0.302 | ~2.5m |

---

## 夜间防御系统（2026-03-16实现）

### 服务器消息
- `phase_changed` → `{ phase: "night"|"day" }`
- `worker_died`   → `{ playerId, respawnAt }`（Unix毫秒）
- `worker_revived`→ `{ playerId }`
- `live_ranking`  → `{ rankings: [{rank, playerId, playerName, contribution}] }`（1.5s防抖推送）

### WorkerManager 方法
- `EnterNightDefense()` — 所有存活 Worker 放弃工位，全部移向 cmd=6 防守位
- `ExitNightDefense()`  — 清除槽位占用，全员 ResetWorker() 回待机圈
- `HandleWorkerDied(playerId, respawnAt)` — 触发 WorkerController.EnterDead()
- `HandleWorkerRevived(playerId)` — 触发 WorkerController.Revive()

### WorkerController 状态
- `Dead` 状态：灰色外观 + 头顶气泡显示倒计时秒数
- `Revive()` → 回 Idle，若夜晚有怪则自动发起攻击
- `SetHpBarVisible(bool)` → 控制 HPBarCanvas 显隐（白天隐藏，夜晚显示）

### MonsterController 目标优先级
1. `FindNearestAliveWorker()` — 优先攻击最近存活矿工（从 WorkerManager 缓存列表查）
2. 无存活矿工时 → 攻击城门 (`_gateTarget`)
3. 当前目标死亡/逃跑 → 重新进入 Moving 状态重新寻目标

### 刷怪位置（2026-03-19更新）
- `SpawnRight`: (22, 0, 18) — 地图右侧边缘
- `SpawnLeft`:  (-18, 0, 18) — 地图左侧边缘
- `SpawnTop`:   (5, 0, 28) — 地图后方边缘
- ⚠️ 城门在 Z=-4，刷怪不能太靠近城门

---

## 性能优化记录（2026-03-16，从 2.9 FPS → 目标 20+ FPS）

### 已实施优化
1. **MAX_WORKERS 20 → 12**：WorkerPool 减少 8 个实例
2. **maxAliveMonsters = 15**：怪物同屏上限，超出排队
3. **阴影投射关闭**：所有 kuanggong SMR `shadowCastingMode = Off`
4. **Animator CullingMode = CullCompletely**：离开视野停止计算
5. **SMR updateWhenOffscreen = false**
6. **LOD Group**：角色 Prefab 添加 LOD，小于 5% 屏占比时裁剪
7. **FBX Mesh Compression High**：导入设置压缩
8. **Blender 减面（50%）**：
   - 工具: `E:\AIProject\DM_CK\Tools\decimate_kuanggong_v2.py`
   - 结果: kuanggong_01 7414→1672面，02~05 约 9k→2k面
   - ⚠️ 必须用 `axis_forward='-Z', axis_up='Y'` 导出，否则模型不可见

### FindObjectsOfType 性能修复
- `MonsterController.FindNearestAliveWorker()` → 改用 `WorkerManager.Instance._activeWorkers` 缓存列表
- `WorkerController.FindNearestActiveMonster()` → 改用 `MonsterWaveSpawner.Instance._activeMonsters` 缓存列表

---

## 关键设计决策（不可推翻）

1. **服务器权威**：客户端等服务器 `survival_game_state` 确认才切换状态，不本地跳转
2. **UI 预创建**：所有面板 Editor 脚本预建，不在运行时 Instantiate
3. **场景保存**：必须用 `EditorSceneManager.SaveScene()`，禁止用 Coplay `save_scene`（路径 bug）
4. **Worker 动画**：使用 kuanggong 系列 controller（IsRunning/IsMining/IsIdle bool），不新建 Controller
5. **资源产出**：由服务器推送，无需客户端定时器
6. **脚本挂载**：UI 脚本挂 Canvas（always-active），禁止在 Awake() 中 SetActive(false) 阻断 OnEnable
7. **实时贡献榜**：`SurvivalLiveRankingUI` 必须挂在 `GameUIPanel`（常驻），不能挂在 `LiveRankingPanel`（会被 SetActive(false) 断开订阅）

---

## 礼物系统（6种，2026-03-19更新）
| Tier | 礼物 | contribution | 效果 |
|------|------|-------------|------|
| T1 | 仙女棒 (fairy_wand) | 1 | 发送者效率永久+5%（上限+100%） |
| T2 | 能力药丸 (ability_pill) | 100 | 全员采矿效率+50%，持续30s（复用 efficiency666 系统） |
| T3 | 甜甜圈 (donut) | 500 | 城门+200HP，全局食物+100 |
| T4 | 能量电池 (energy_battery) | 1000 | 炉温+30℃，发送者效率+30%（180s） |
| T5 | 爱的爆炸 (love_explosion) | 2000 | 全体怪物AOE-200，发送者矿工满血/复活，城门+200HP |
| T6 | 神秘空投 (mystery_airdrop) | 5000 | 食物+500，煤炭+200，矿石+100，城门+300HP |

### 矿石（Ore）使用机制（2026-03-19新增）
- 服务器 `_decayResources()` 每 2s 执行一次（tickCounter % 10 === 0）
- 若城门受损且矿石 > 0：消耗 1 矿石，修复城门 5 HP
- 最大修复上限为当前缺失血量，不超出

---

## UI 系统（2026-03-19更新）

### 礼物说明栏（GiftIconBar）
- 位置：Canvas/GameUIPanel/GiftIconBar，高160px，底部锚定
- Editor脚本：`Assets/Editor/RebuildGiftIconBar.cs`（`Tools → DrscfZ → Rebuild Gift Icon Bar`）
- 图标资源：`Assets/Resources/GiftIcons/Gift_Icon_1~6.png`
- 布局：**百分比锚点**（图标50%~100%、名称25%~50%、说明0%~25%）
- ⚠️ TMP颜色必须用 `SerializedObject` 写 `m_fontColor` + `m_fontColor32`，直接赋 `.color` 不生效（faceColor 默认白色）

### 结算界面
- 不自动消失，停留直到玩家点击"重新开始"
- "查看英雄榜" 和大厅"排行榜"按钮 → 同一面板 `SurvivalRankingUI.ShowPanel()`

### 实时贡献榜（SurvivalLiveRankingUI）
- 纯服务器驱动，订阅 `SurvivalGameManager.OnLiveRankingReceived`
- 仅在 Running 状态显示，其余状态隐藏

---

## 已知踩坑

### Unity / Coplay
- **Coplay 超时**：`create_coplay_task` 30s~2min，MCP 超时但 Unity 中照常执行
- **禁用 DisplayDialog**：Editor 脚本禁止 `EditorUtility.DisplayDialog`（阻塞进程）
- **场景保存用**: `Assets/Editor/SaveCurrentScene.cs` 的 `Execute()` 方法
- **多 Unity 实例**：Coplay MCP 报 "Multiple Unity Editor instances" 时先调 `set_unity_project_root`
- **Play Mode 中不能执行 Editor 脚本**：execute_script 超时时先 Stop Game

### TMP（TextMeshProUGUI）
- **faceColor 默认白色**：新建 TMP 组件时 `faceColor` 为白，`color=black` 只写 Graphic 层，渲染仍白色
- **正确做法**：用 `SerializedObject` 写 `m_fontColor` 和 `m_fontColor32` 两个字段
- **faceColor setter 要求 material 存在**：在 AddComponent 后立即调用 `.faceColor=` 会报 `NullReferenceException`

### UI 布局
- **新建 GO 无 RectTransform**：`new GameObject()` 后必须先 `AddComponent<RectTransform>()` 再 `SetParent`
- **anchorMin/anchorMax 设置被覆盖**：HLG layout rebuild 会重置子孙的锚点，改用百分比锚点（两个 anchor 都设，不用 offsetMin/Max）
- **LayoutGroup childControlHeight 不生效**：Editor 脚本创建的 VLG 有时不实际控制高度，改用纯锚点布局

### 模型 / Blender
- **Blender FBX 导出必须指定轴**：`axis_forward='-Z', axis_up='Y'`，否则模型在 Unity 中不可见
- **减面脚本**: `Tools/decimate_kuanggong_v2.py`（v1 轴设置错误，已废弃）
- **武器模型跳过减面**：pickaxe/hammer/chuizi 面数 < 500，无需处理

### 怪物系统
- **刷新位置**：地图边缘（SpawnRight/Left/Top），不能太靠近城门（Z=-4）
- **_monsterScale**：Inspector 设为 1.0（scale 已烘焙进 Prefab 的 Mesh 子节点）
- **死亡动画**：MonsterController `PlayAnim("Sit")`，无 Dead 状态，用 Sitting Dazed 代替

### UI
- **emoji 显示方块**：改用中文文字替代（如 ⚡→"闪电"）
- **禁止在 Awake() 中 SetActive(false)**：会阻断 OnEnable 中的事件订阅

---

## 常用 Editor 脚本（Tools → DrscfZ 菜单）
| 脚本 | 用途 |
|------|------|
| `SetupKuanggongPrefabs.cs` | 从源 prefab 生成 5 个游戏用 Prefab |
| `ReplaceWorkerPool.cs` | 替换场景 WorkerPool 下所有旧实例 |
| `AssignKuanggongPrefabs.cs` | 将新 Prefab 赋值到 WorkerManager/MonsterWaveSpawner |
| `RebuildBossPrefab.cs` | 单独重建 KuanggongBoss_05.prefab |
| `ReimportKuanggongFBX.cs` | 强制重导入所有 kuanggong FBX（Blender 改动后用）|
| `SaveCurrentScene.cs` | 保存当前场景（替代 Coplay save_scene）|
| `RebuildGiftIconBar.cs` | 重建底部礼物说明栏（6卡片，图标+名称+效果）|

---

## 待完成
1. **Worker HPBarCanvas 联调**：夜晚显示血条（`WorkerManager.EnterNightDefense` 调 `SetHpBarVisible`）
2. **设置面板**：BtnSettings 点击后的面板功能实现
3. **AudioManager**：音效素材填充
4. **SettlementUI `_top3Slots`**：Inspector 手动绑定 3 个槽位

## Coplay MCP
- **配置文件**: `.mcp.json`（项目根目录）
- **多实例处理**: 先调 `set_unity_project_root("E:\\AIProject\\DM_CK")`
