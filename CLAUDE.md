# 冬日生存法则 (drscfz) — DM_CK

## ⚡ 新会话启动指令（必须优先执行）
> 新对话框打开后，在做任何任务之前，必须按顺序执行：
> 1. 读取项目记忆: `C:\Users\Administrator\.claude\projects\E--AIProject-DM-CK\memory\MEMORY.md`
> 2. 确认 Coplay MCP 可用（调用 `get_unity_editor_state`，若多 Unity 实例先调 `set_unity_project_root` 设为 `E:\AIProject\DM_CK`）
> 3. 编译检查（`check_compile_errors`）
> 4. 向用户汇报当前状态 + 待完成列表，等待指示

## 项目概述
抖音直播间互动游戏：观众刷礼物让矿工采集资源、抵御怪物入侵，坚持到天亮即胜利。
- Unity 2022.3.47f1 + URP
- Node.js WebSocket 服务器
- 目标平台：抖音直播互动插件

## 服务器
- **IP**: `101.34.30.65`（⚠️ `212.64.26.65` 是旧项目，不要用）
- **PM2 进程**: `drscfz-server`
- **路径**: `/opt/drscfz/`
- **WebSocket**: `ws://101.34.30.65:8081`
- **SSH 私钥**: `D:\共享\AI项目资料\id_rsa_transfer`
- **部署**: `python E:\AIProject\DM_CK\deploy.py`

---

## 架构

### Unity 脚本结构
```
Assets/Scripts/
├── Survival/
│   ├── SurvivalGameManager.cs   — 状态机(Idle/Loading/Running/Settlement)
│   │                              处理 phase_changed / worker_died / worker_revived
│   ├── WorkerController.cs      — Worker状态机+协程移动(EaseOutCubic)
│   │                              State: Idle/Move/Work/Special/Frozen/Dead
│   │                              支持双Animator参数集(A:Speed/IsPushing; B:IsRunning/IsMining)
│   ├── WorkerManager.cs         — StationSlot注册表+就近分配+EnterNightDefense/ExitNightDefense
│   ├── WorkerVisual.cs          — 颜色/发光/冻结/TriggerAssignmentFlash
│   └── SurvivalDataTypes.cs     — 消息数据结构
├── Monster/
│   ├── MonsterController.cs     — 状态机(Moving/Attacking/Dead)
│   │                              优先攻击最近存活Worker，无Worker时攻城门
│   └── MonsterWaveSpawner.cs    — 怪物波次生成，maxAliveMonsters=15上限
├── Core/
│   └── NetworkManager.cs        — WebSocket 通信
└── UI/
    ├── SurvivalIdleUI.cs / SurvivalLoadingUI.cs
    ├── GiftNotificationUI.cs    — 7种礼物横幅+效果描述
    ├── SurvivalSettingsUI.cs    — BGM/SFX音量控制
    ├── SurvivalLiveRankingUI.cs — 实时贡献榜Top5(3s刷新)
    └── FrozenStatusUI.cs        — 冻结状态横幅
```

### Node.js 服务器
```
Server/src/
├── SurvivalGameEngine.js   — 游戏引擎（状态机/资源/礼物/昼夜）
├── SurvivalRoom.js         — 游戏房间（含完整8人模拟+10轮工作+7礼物）
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
  └── HPBarCanvas/ (World Space Canvas，默认隐藏，夜晚显示)
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

---

## 性能优化记录（2026-03-16，从 2.9 FPS → 目标 20+ FPS）

### 问题根源（优化前）
- Visible skinned meshes: 154，CPU: 340ms/帧
- Shadow casters: 547，Batches: 1079

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
   - 重导入: `Tools → DrscfZ → Reimport Kuanggong FBX`

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

---

## 礼物系统（7种）
| Tier | 礼物 | contribution | 效果 |
|------|------|-------------|------|
| T1 | 仙女棒 (fairy_wand) | 1 | 效率加成 |
| T2 | 能力药丸 (ability_pill) | 100 | +food=50，召唤守卫 |
| T2 | 魔法镜 (magic_mirror) | 0 | 捣乱礼物 |
| T3 | 甜甜圈 (donut) | 500 | +food=100, +gateHp=200 |
| T3 | 超能喷射 (super_jet) | 500 | 效率×2 |
| T4 | 能量电池 (energy_battery) | 1000 | +heat=30℃ |
| T5 | 神秘空投 (mystery_airdrop) | 5000 | +food=500,+coal=200,+ore=100,+gateHp=300 |

---

## 已知踩坑

### Unity / Coplay
- **Coplay 超时**：`create_coplay_task` 30s~2min，MCP 超时但 Unity 中照常执行
- **禁用 DisplayDialog**：Editor 脚本禁止 `EditorUtility.DisplayDialog`（阻塞进程）
- **场景保存用**: `Assets/Editor/SaveCurrentScene.cs` 的 `Execute()` 方法
- **多 Unity 实例**：Coplay MCP 报 "Multiple Unity Editor instances" 时先调 `set_unity_project_root`

### 模型 / Blender
- **Blender FBX 导出必须指定轴**：`axis_forward='-Z', axis_up='Y'`，否则模型在 Unity 中不可见
- **减面脚本**: `Tools/decimate_kuanggong_v2.py`（v1 轴设置错误，已废弃）
- **武器模型跳过减面**：pickaxe/hammer/chuizi 面数 < 500，无需处理
- **Boss Prefab 重建**：若出现多余 SMR 导致不可见，运行 `Tools → DrscfZ → Rebuild Boss Prefab`

### 怪物系统
- **刷新位置**：城门外侧 Z < -4（城门 Z=-4，堡垒 Z=0~8）
- **_monsterScale**：Inspector 设为 1.0（scale 已烘焙进 Prefab 的 Mesh 子节点，不再用该字段缩放）
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

---

## 待完成
1. **Play Mode 视觉确认**：kuanggong 模型在 Blender v2 减面 + 修正轴后是否正常显示
2. **Worker HPBarCanvas**：夜晚显示血条功能联调（`WorkerManager.EnterNightDefense` 调 `SetHpBarVisible`）
3. **守卫（ability_pill）AI**：当前召唤的守卫站在原地不动，需要加移动/攻击逻辑
4. **设置面板**：BtnSettings 点击后的面板功能实现
5. **AudioManager**：音效素材填充
6. **SettlementUI `_top3Slots`**：Inspector 手动绑定 3 个槽位

## Coplay MCP
- **配置文件**: `.mcp.json`（项目根目录）
- **多实例处理**: 先调 `set_unity_project_root("E:\\AIProject\\DM_CK")`
