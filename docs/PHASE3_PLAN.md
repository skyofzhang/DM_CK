# Phase 3 开发计划 — 冬日生存法则

> **版本**: v1.0 | **制定时间**: 2026-02-24
> **目标**: 从"代码存在"变成"游戏真实可玩"
> **原则**: 每个功能模块必须包含自测脚本，不接受只写代码不验证的交付

---

## 🎯 Phase 3 总目标

当前状态：代码覆盖率约 92%，但从玩家视角来看游戏几乎不可玩：
- ❌ 没有角色（Capsule 占位）
- ❌ 666弹幕/随机事件/点赞功能完全不存在
- ❌ 音效全部无声
- ❌ 排行榜点开没数据且界面错误
- ❌ 设置/结算都是空壳
- ❌ 主播按钮效果不到达客户端

**Phase 3 完成标准**：
> 一个真实观众进入直播间，发弹幕采集资源，看到工人移动，看到夜晚来临怪物攻城，城门被攻破或熬过夜晚，看到结算界面，打开排行榜看到自己的名字和积分。全程无 Console Error。

---

## 📋 模块列表（按依赖顺序）

| 编号 | 模块 | 优先级 | 预计耗时 | 包含自测 | 状态 |
|------|------|--------|---------|---------|------|
| M3-00 | Worker 角色替换执行 | P0 | 10分钟 | ✅ | ⏳ |
| M3-01 | 服务器逻辑修复批次 | P0 | 3小时 | ✅ | ⬜ |
| M3-02 | 客户端消息处理补全 | P0 | 2小时 | ✅ | ⬜ |
| M3-03 | 排行榜界面完整重建 | P1 | 4小时 | ✅ | ⬜ |
| M3-04 | 结算界面修复与验证 | P1 | 2小时 | ✅ | ⬜ |
| M3-05 | 设置面板实现 | P1 | 3小时 | ✅ | ⬜ |
| M3-06 | 音效素材填充 | P1 | 2小时 | ✅ | ⬜ |
| M3-07 | 视觉反馈补全 | P2 | 3小时 | ✅ | ⬜ |
| M3-08 | 完整游戏循环自动化测试 | P0 | 2小时 | — | ⬜ |

---

## M3-00：Worker 角色替换执行

**时间预算**：10分钟

### 执行步骤
```
1. Unity 菜单 → Tools → DrscfZ → Fix Worker Mesh (Capsule → CowWorker)
2. Console 确认输出：[FixWorkerMesh] ✅ 共替换 20 个 Worker，场景已保存
```

### 自测（执行完立即做）
```csharp
// 创建编辑器脚本 TestWorkerReplacement.cs
// 验证：
// 1. WorkerPool 下所有 Worker_XX 有名为 "Body" 的子对象
// 2. Body 下有 SkinnedMeshRenderer（而非 MeshRenderer）
// 3. Worker_XX 本身没有 MeshFilter 和 MeshRenderer 组件
// 4. WorkerVisual._bodyRenderer != null（Awake 后能找到 SkinnedMeshRenderer）
// 5. capture_scene_object() 截图，确认场景中有人形角色
```

### 验收标准
- [ ] Console 无 Error
- [ ] 截图可见人形角色（非胶囊）
- [ ] WorkerVisual 颜色设置生效（测试：手动调用 Worker_00.GetComponent<WorkerVisual>().SetWorkColor(1)，截图确认蓝色）

---

## M3-01：服务器逻辑修复批次

**时间预算**：3小时

### 1a. 修复 666 弹幕处理（`Server/src/SurvivalGameEngine.js`）

**问题**：`handleComment` 只处理 1-6，666 被当作未知指令

**修复方案**：
```javascript
// SurvivalRoom.js handleDouyinComment 中，在指令判断前加：
if (comment.content === '666' || comment.content.includes('666')) {
    this.engine.handleComment({ userId, userName, content: '666', cmd: 666 });
    return;
}

// SurvivalGameEngine.js handleComment 中加 case 666：
case 666:
    this.efficiency666Bonus = 1.15;  // 全局效率+15%
    this.efficiency666Timer = 30;     // 持续30秒
    this.playerContributions[userId] = (this.playerContributions[userId] || 0) + 2;
    this.broadcast({ type: 'special_effect', effect: 'glow_all', duration: 3 });
    break;

// _tick 中处理倒计时：
if (this.efficiency666Timer > 0) {
    this.efficiency666Timer -= this.tickInterval / 1000;
    if (this.efficiency666Timer <= 0) this.efficiency666Bonus = 1.0;
}

// _applyWorkEffect 中将 totalMult 乘以 efficiency666Bonus
```

**自测**：
```javascript
// test_666.js
// 发送 666 弹幕后：
// ① 验证服务器广播 special_effect { effect: 'glow_all' }
// ② 验证 30 秒内效率倍率提升（采集量比平时多 15%）
// ③ 验证给发送者 +2 积分
```

### 1b. 修复炉温生火效果（`SurvivalGameEngine.js`）

**问题**：指令4每次只加 1℃，策划案要求 +3℃；且不消耗煤炭（策划案要求 -1煤/200ms）

**修复**：
```javascript
case 4:  // 生火
    this.furnaceTemp = Math.min(this.furnaceTemp + Math.round(3 * totalMult), 100);
    this.coal = Math.max(0, this.coal - 1);  // 消耗 1 煤炭
    this.playerContributions[userId] = (this.playerContributions[userId] || 0) + 1;
    break;
```

### 1c. 移除指令5修城门（与策划案 v2.0 矛盾）

**修复**：
```javascript
// handleComment: 改为 cmd >= 1 && cmd <= 4 || cmd === 6
// _applyWorkEffect: 删除 case 5 分支
// cmdNames: 将 [5] 改为 null 或 'unused'
```

### 1d. 修复城门升级数值

**问题**：升级消耗 [30, 60, 120] 矿石，策划案要求 [100, 250, 500]；HP [100, 200, 350, 550]，策划案要求 [1000, 1500, 2200, 3000]

**修复**：
```javascript
const GATE_UPGRADE_COSTS = [0, 100, 250, 500];  // 矿石
const GATE_MAX_HP_BY_LEVEL = [1000, 1500, 2200, 3000];
```

### 1e. 实现随机事件系统

**策划案 §8**：白天每 90-120s 随机触发一个事件

**修复**：
```javascript
// _tick 中加入随机事件触发器
_checkRandomEvents() {
    if (this.phase !== 'day') return;
    this.nextEventTimer -= this.tickInterval / 1000;
    if (this.nextEventTimer > 0) return;

    // 随机下一次触发时间 90-120s
    this.nextEventTimer = 90 + Math.random() * 30;

    const events = ['E01_snowstorm', 'E02_harvest', 'E03_monster_wave', 'E04_warm_spring', 'E05_ore_vein'];
    const event = events[Math.floor(Math.random() * events.length)];
    this._applyRandomEvent(event);
}

_applyRandomEvent(eventId) {
    switch(eventId) {
        case 'E01_snowstorm':   // 暴风雪：炉温衰减×2，持续60s
            this.tempDecayMultiplier = 2.0;
            setTimeout(() => this.tempDecayMultiplier = 1.0, 60000);
            break;
        case 'E02_harvest':     // 丰收：食物采集效率×1.5，持续30s
            this.foodBonus = 1.5;
            setTimeout(() => this.foodBonus = 1.0, 30000);
            break;
        case 'E03_monster_wave': // 怪物潮：额外生成2只怪物
            this.spawnExtraMonsters(2);
            break;
        case 'E04_warm_spring':  // 暖流：炉温+20℃
            this.furnaceTemp = Math.min(100, this.furnaceTemp + 20);
            break;
        case 'E05_ore_vein':    // 矿脉：矿石采集效率×2，持续45s
            this.oreBonus = 2.0;
            setTimeout(() => this.oreBonus = 1.0, 45000);
            break;
    }
    this.broadcast({ type: 'random_event', eventId, name: EVENT_NAMES[eventId] });
}
```

### 1f. 实现点赞处理

```javascript
// SurvivalRoom.js handleDouyinLike
handleDouyinLike(data) {
    const { userId, userName, likeCount = 1 } = data;
    // 每次点赞 +2 积分（策划案 §9）
    this.engine.addContribution(userId, 2 * likeCount);
    // 每 50 次点赞触发一次小资源加成
    this.engine.totalLikes = (this.engine.totalLikes || 0) + likeCount;
    if (this.engine.totalLikes % 50 === 0) {
        this.engine.food = Math.min(this.engine.food + 10, this.engine.maxFood);
    }
}
```

### 自测脚本（`test_m3_server.js`）
```
验证项：
✅ 发 "666" 弹幕 → 收到 special_effect{glow_all}，30s后效率恢复
✅ 发 "4"×10 → 炉温+30℃，煤炭-10
✅ 发 "5" → 无任何效果（指令5已删除）
✅ 矿石达到 100 → 城门可升级到2级（消耗100矿石，HP变1500）
✅ 等待 90-120s → 收到 random_event 消息
✅ 点赞 × 50 → food+10
✅ 全部通过后，部署到生产服务器
```

---

## M3-02：客户端消息处理补全

**时间预算**：2小时

### 2a. 处理 `broadcaster_effect` 消息

**问题**：服务器广播此消息，客户端完全丢弃

**修复**：在 `SurvivalGameManager.cs` HandleMessage 添加：
```csharp
case "broadcaster_effect":
    var effect = msg["effect"]?.ToString();
    var duration = msg["duration"] != null ? (float)msg["duration"] : 3f;
    if (effect == "efficiency_boost") {
        WorkerManager.Instance?.ActivateAllWorkersGlow(duration);
        ShowAnnouncementBanner("⚡ 主播加速！效率提升！", duration);
    } else if (effect == "trigger_event") {
        var eventType = msg["eventType"]?.ToString();
        HandleBroadcasterEvent(eventType);
    }
    break;
```

### 2b. 处理 `special_effect` 消息（666弹幕全局光晕）

```csharp
case "special_effect":
    var fx = msg["effect"]?.ToString();
    var fxDuration = msg["duration"] != null ? (float)msg["duration"] : 3f;
    if (fx == "glow_all") {
        WorkerManager.Instance?.ActivateAllWorkersGlow(fxDuration);
        ShowAnnouncementBanner("🌟 全员666加速！", fxDuration);
    } else if (fx == "frozen_all") {
        WorkerManager.Instance?.ActivateAllWorkersFrozen(fxDuration);
    }
    break;
```

### 2c. 处理 `random_event` 消息（随机事件公告）

```csharp
case "random_event":
    var eventId = msg["eventId"]?.ToString();
    var eventName = msg["name"]?.ToString();
    ShowFullScreenEvent(eventId, eventName);  // 全屏事件公告，3秒后消失
    break;
```

### 2d. 处理 `gift_pause` 消息（T5 神秘空投暂停）

```csharp
case "gift_pause":
    var pauseMs = msg["duration"] != null ? (int)msg["duration"] : 3000;
    StartCoroutine(PauseGameTick(pauseMs / 1000f));
    break;

IEnumerator PauseGameTick(float seconds) {
    _isPaused = true;
    // 暂停所有 Worker 动画
    WorkerManager.Instance?.PauseAllWorkers();
    yield return new WaitForSeconds(seconds);
    _isPaused = false;
    WorkerManager.Instance?.ResumeAllWorkers();
}
```

### 2e. 资源警告阈值处理（顶栏红闪）

```csharp
// SurvivalTopBarUI.cs 添加警告阈值判断
void UpdateResourceDisplay(ResourceData res) {
    // 食物
    _foodText.text = res.food.ToString();
    _foodText.color = res.food <= 100 ? Color.red : Color.white;
    if (res.food <= 100 && !_foodWarningActive) {
        _foodWarningActive = true;
        AudioManager.Instance?.PlaySFX(AudioConstants.SFX_RESOURCE_LOW);
    }
    // 炉温
    if (res.furnaceTemp <= -50f) _furnaceTempText.color = Color.cyan;
    else if (res.furnaceTemp <= -80f) _furnaceTempText.color = new Color(0.5f, 0f, 1f); // 紫色
    else _furnaceTempText.color = Color.white;
}
```

### 自测脚本（Editor 脚本 `TestClientMessages.cs`）
```
通过 WebSocket 给运行中的 Unity 客户端发送各类消息，验证：
✅ broadcaster_effect{efficiency_boost} → Worker 发光 + 横幅公告出现
✅ special_effect{glow_all} → 全部 Worker 金色光晕
✅ random_event{E01_snowstorm} → 全屏"暴风雪来袭"公告显示 3s 后消失
✅ gift_pause{3000} → Worker 停止动作 3s 后恢复
✅ resource_update{food:50} → 食物数字变红色
✅ resource_update{furnaceTemp:-60} → 炉温变青色
```

---

## M3-03：排行榜界面完整重建

**时间预算**：4小时

### UI 规格（全屏独立界面）

```
┌─────────────────────────────────────────────────────┐
│ [× 关闭]              守护者排行榜              [刷新] │
├──────────┬──────────┬──────────┬──────────────────────┤
│  本场贡献  │  本周榜   │  本月榜   │     连胜榜           │
├──────────┴──────────┴──────────┴──────────────────────┤
│  排名  │  头像  │  昵称            │  积分   │  今日  │
│────────┼────────┼──────────────────┼─────────┼────────│
│  🥇 1  │  [👤] │  玩家昵称         │  2450  │ +120   │
│  🥈 2  │  [👤] │  玩家昵称         │  1830  │  +85   │
│  🥉 3  │  [👤] │  玩家昵称         │  1240  │  +60   │
│    4   │  [👤] │  玩家昵称         │   890  │  +40   │
│   ...  │  ...  │  ...              │  ...   │  ...   │
│────────┴────────┴──────────────────┴─────────┴────────│
│ [我的排名：第 12 名 | 积分：320 | 今日 +45]            │
└─────────────────────────────────────────────────────┘
```

**规格细节**：
- 全屏覆盖（1080×1920 或适配 Canvas），背景半透明深色
- 关闭按钮右上角（或左上角 × 号），不依赖外部面板控制
- 4个 Tab：本场贡献 / 本周榜 / 本月榜 / 连胜榜
- 每行显示：排名 + 头像（TMP Sprite Asset 圆形裁切） + 昵称 + 积分 + 今日贡献
- 底部固定展示"我的排名"（当前玩家）
- 数据来源：`RankingSystem.GetTopN(20)` / `GetWeeklyTop(20)` / `GetMonthlyTop(20)` / `GetStreakTop(20)`
- 无数据时显示"暂无数据，快来发弹幕成为第一名！"

**实现方案**：
1. 新建 `SurvivalRankingUI.cs`（不使用旧 `RankingPanelUI`）
2. 新建 Canvas 子面板 `SurvivalRankingPanel`（`SetActive(false)` 预创建，Rule#2）
3. `SurvivalIdleUI.cs` 的 `OnRankingClicked` 改为调用 `SurvivalRankingPanel.Show()`
4. 游戏中（Running 状态）也可从 GameUIPanel 打开排行榜

**自测（`TestRankingUI.cs`）**：
```
1. 通过 RankingSystem.AddContribution 注入10个假玩家数据
2. 调用 SurvivalRankingUI.Show()
3. capture_ui_canvas("Canvas") → 截图验证
   ✅ 排行榜面板全屏显示
   ✅ 10个玩家按积分降序排列
   ✅ 头像/昵称/积分列宽均匀
   ✅ 点击 × 按钮 → 面板关闭
   ✅ 切换 Tab → 不同数据集显示
   ✅ 无数据 Tab → 显示提示文字
4. 验证 SurvivalIdleUI._rankingBtn 绑定到新面板
```

---

## M3-04：结算界面修复与验证

**时间预算**：2小时

### 当前问题
- B 屏排名列表区域空白（`_rankEntries` 未填充）
- 总击杀/总采集/总修复数据从未推送
- C 屏 Top3 已实现，但需端到端联调

### 修复方案

**服务器端**（`SurvivalGameEngine.js`，游戏结束时）：
```javascript
// 在 _endGame() 或 game_over 广播时加入统计：
this.broadcast({
    type: 'game_settlement',
    totalKills: this.totalKillsAllTime,
    totalGather: this.totalGatherAllTime,
    totalRepairs: this.totalRepairsAllTime,
    survivalDays: this.day,
    reason: this.failureReason,  // 'food'/'temp'/'gate'/'victory'
    topPlayers: this.engine.getTopN(3),  // 用于C屏
    allRankings: this.engine.getTopN(20) // 用于B屏
});
```

**客户端**（`SurvivalGameManager.cs`）：
```csharp
case "game_settlement":
    var settlementData = JsonConvert.DeserializeObject<SettlementData>(msg.ToString());
    SurvivalSettlementUI.Instance?.ShowSettlement(settlementData);
    ChangeState(SurvivalState.Settlement);
    break;
```

**`SurvivalSettlementUI.cs` 修复**：
- A 屏：显示胜利/失败原因 + 存活天数
- B 屏：遍历 `allRankings` 填充 `_rankEntries`（昵称+积分+贡献数据）
- C 屏：Top3 头像+名字+积分（已有基础）
- 结算完成后：监听服务器 `survival_game_state{idle}` → 自动回大厅

**自测**：
```
通过 WebSocket 发送模拟 game_settlement 消息：
✅ A屏显示失败原因（如"炉火熄灭"）和存活天数
✅ B屏显示 Top20 玩家列表（循环5个假玩家数据，重复4次模拟20人）
✅ C屏显示 Top3 带头像
✅ 结算计时结束（20s）或点击按钮 → 回到大厅（LobbyPanel 出现）
```

---

## M3-05：设置面板实现

**时间预算**：3小时

### UI 规格（二级全屏面板）

```
┌─────────────────────────────────────────────────────┐
│ ← 返回                  设置                          │
├─────────────────────────────────────────────────────┤
│ 🔊 音效音量                    ████████░░  80%       │
│ 🎵 背景音乐                    ███████░░░  70%       │
├─────────────────────────────────────────────────────┤
│ 游戏难度（仅主播可调）                                 │
│  ○ 简单    ● 普通    ○ 困难                           │
├─────────────────────────────────────────────────────┤
│ 昼夜时长（仅主播可调）                                 │
│  白天：[240] 秒    夜晚：[150] 秒                     │
├─────────────────────────────────────────────────────┤
│ 显示设置                                              │
│  [✓] 显示弹幕飘屏   [✓] 显示礼物特效   [✓] 显示工人气泡│
├─────────────────────────────────────────────────────┤
│ 关于                                                  │
│  版本 1.0.0 | 服务器 101.34.30.65:8081               │
│  [测试连接]   [清空排行榜数据]（危险操作，需确认）       │
└─────────────────────────────────────────────────────┘
```

**实现方案**：
1. 新建 `SurvivalSettingsUI.cs`
2. 新建 `Canvas/SurvivalSettingsPanel`（Rule#2 预创建）
3. 音量滑块 → 直接控制 `AudioManager.Instance.SetBGMVolume()` / `SetSFXVolume()`
4. 难度/时长 → 发送 `config_update` 消息到服务器（主播专用权限）
5. 显示开关 → 本地 PlayerPrefs 存储
6. `SurvivalIdleUI.cs` 的 `OnSettingsClicked` 改为调用 `SurvivalSettingsPanel.Show()`

**自测**：
```
✅ 点击设置按钮 → 设置面板全屏显示（截图）
✅ 拖动音效滑块 → AudioManager.sfxVolume 变化（Debug.Log 验证）
✅ 拖动BGM滑块 → 背景音乐音量变化
✅ 切换显示开关 → 对应 UI 元素隐藏/显示
✅ 点击返回按钮 → 回到大厅
✅ 测试连接按钮 → 显示服务器状态
```

---

## M3-06：音效素材填充

**时间预算**：2小时

### 缺失音效清单（参考 AudioConstants.cs）

**需要填充的文件（Resources/Audio/）**：
```
BGM（背景音乐）:
├── bgm_day.mp3        — 白天轻快旋律（无版权，推荐 pixabay.com 搜"adventure"）
├── bgm_night.mp3      — 夜晚紧张氛围（推荐搜"suspense"）
├── bgm_win.mp3        — 胜利音乐（推荐搜"victory fanfare"）
└── bgm_lose.mp3       — 失败音乐（推荐搜"game over"）

SFX（音效）:
├── sfx_gift_t1.wav    — T1礼物：清脆铃声
├── sfx_gift_t2.wav    — T2礼物：泡泡音效
├── sfx_gift_t3.wav    — T3礼物：礼炮声
├── sfx_gift_t4.wav    — T4礼物：电子充能声
├── sfx_gift_t5.wav    — T5礼物：空投炸弹落地声
├── sfx_worker_work.wav  — Worker 工作音（锤击/挖矿）
├── sfx_worker_glow.wav  — Worker 发光特效音
├── sfx_resource_low.wav — 资源警告音（低沉警报）
├── sfx_gate_hit.wav     — 城门被攻击（撞击声）
├── sfx_gate_destroy.wav — 城门被摧毁
├── sfx_day_start.wav    — 白天开始（鸡鸣/欢快）
├── sfx_night_start.wav  — 夜晚降临（狼嚎/不安）
├── sfx_monster_spawn.wav — 怪物出现（咆哮）
├── sfx_event_trigger.wav — 随机事件触发（神秘音效）
└── sfx_click.wav          — 按钮点击音
```

**获取方案**：
1. 优先使用 Pixabay.com / Freesound.org 免版权音效
2. 用 python 脚本批量下载（或手动，约 30-60 分钟）
3. 放入 `Assets/Resources/Audio/`
4. 运行 AudioManager 加载测试，验证全部成功

**自测**：
```
Play Mode 启动后，AudioManager 日志应显示：
✅ BGM Loaded: 4/4
✅ SFX Loaded: 15/15（最低要求）
调用 AudioManager.PlaySFX(AudioConstants.SFX_GIFT_T1) → 能听到声音
```

---

## M3-07：视觉反馈补全

**时间预算**：3小时

### 7a. Worker 动画驱动（连接 Animator）

**方案**：
```csharp
// WorkerController.cs 添加：
private Animator _animator;
void Start() {
    _animator = GetComponentInChildren<Animator>();
}

// 各状态切换时：
EnterWork:  _animator?.SetBool("IsWorking", true);
EnterIdle:  _animator?.SetBool("IsWorking", false); _animator?.SetTrigger("Idle");
EnterMove:  _animator?.SetBool("IsRunning", true);
EnterSpecial: _animator?.SetTrigger("Dance");  // 666发光时跳舞
EnterFrozen:  _animator?.SetBool("IsFrozen", true); _animator?.speed = 0;
ExitFrozen:   _animator?.SetBool("IsFrozen", false); _animator?.speed = 1;
```

**需要在 CowWorker.prefab 的 Animator Controller 中添加**：
- 状态：Idle / Run / Work / Dance / Frozen
- 参数：IsWorking(bool) / IsRunning(bool) / IsFrozen(bool) / Dance(trigger)
- 需要的 Mixamo 动画：Idle / Run / Sword Attack（用于挖矿/打怪）/ Dance / T-Pose（冻结）

### 7b. 随机事件全屏公告 UI

```
新建 Canvas/EventAnnouncementPanel（全屏，z-order最高）
- 大字标题（如"❄️ 暴风雪来袭！"）
- 副标题（效果说明：炉温衰减加速60s）
- 自动 3s 后淡出消失
- 脚本：EventAnnouncementUI.cs（挂 Canvas）
```

### 7c. 昼夜切换视觉

**验证已有实现是否工作**（`DayNightCycleManager.cs`）：
```
Play Mode → 等待 240s → 验证：
✅ 天空颜色从蓝→黑
✅ 环境光变暗
✅ BGM 从 bgm_day 切换到 bgm_night（淡入淡出）
✅ Console 输出：[DayNight] Day→Night
如果天空不变化：检查 SkyboxBlend 属性名 / DayNightManager 是否挂在 active 对象上
```

### 7d. 怪物波次视觉确认

**验证已有实现**（`MonsterWaveSpawner.cs`）：
```
Play Mode → 触发 start_game → 等待夜晚 → 验证：
✅ 怪物从地图边缘生成
✅ 怪物向城门移动
✅ 到达城门时城门 HP 减少（顶栏可见）
✅ 发 "6" 弹幕 → 有 Worker 移向城门
如果怪物不生成：检查 MonsterWaveSpawner 是否在 Running 状态激活
```

---

## M3-08：完整游戏循环自动化测试

**时间预算**：2小时

> 这是项目负责人视角最重要的一环——不依赖人工操作，自动验证游戏完整循环。

### 测试脚本架构

**服务器端**（`test_m3_full_loop.js`）：
```javascript
async function testFullGameLoop() {
    // 1. 连接服务器
    const ws = new WebSocket('ws://localhost:8081');

    // 2. 模拟10个玩家加入
    for (let i = 0; i < 10; i++) {
        send({ type: 'player_joined', userId: `user_${i}`, userName: `测试玩家${i}` });
    }

    // 3. 主播开始游戏
    send({ type: 'start_game', isCreator: true });

    // 4. 白天：持续发弹幕
    await runFor(60, () => {
        sendComment('1');  // 采食物
        sendComment('2');  // 挖煤
        sendComment('3');  // 挖矿
        sendComment('4');  // 生火
        sendComment('666'); // 加速
    });

    // 5. 验证夜晚触发（等待 240s 后收到 phase:night）
    await waitForMessage('survival_game_state', s => s.phase === 'night');

    // 6. 夜晚：发打怪弹幕 + 发礼物
    await runFor(30, () => sendComment('6'));
    send({ type: 'gift', giftId: 'fairy_wand', userId: 'user_0' });

    // 7. 等待游戏结束（存活1天或失败）
    const result = await waitForMessage('game_settlement', null, 300000); // 5min timeout

    // 8. 验证结算数据完整
    assert(result.totalKills >= 0, '总击杀数存在');
    assert(result.topPlayers.length > 0, 'Top玩家列表非空');

    // 9. 主播退出到大厅
    send({ type: 'reset_game', isCreator: true });
    await waitForMessage('survival_game_state', s => s.state === 'idle');

    console.log('✅ 完整游戏循环测试通过！');
}
```

**Unity 端**（`AutoTest_GameLoop.cs` 编辑器脚本）：
```csharp
// 菜单：Tools/DrscfZ/Auto Test Game Loop
// 通过 EditorCoroutine 自动操作 UI
[MenuItem("Tools/DrscfZ/Auto Test Game Loop")]
static async void TestGameLoop() {
    // 1. 截图：初始状态
    var shot1 = await Capture("01_initial");
    Assert(ConnectPanel is visible, shot1);

    // 2. 等待连接（模拟，或直接触发）
    NetworkManager.Instance.SimulateConnect();
    await Wait(0.5f);
    var shot2 = await Capture("02_lobby");
    Assert(LobbyPanel is visible, shot2);

    // 3. 点击开始玩法
    SurvivalIdleUI.Instance.OnStartClicked();
    await Wait(0.3f);
    var shot3 = await Capture("03_loading");
    Assert(LoadingPanel is visible, shot3);

    // 4. 模拟服务器确认
    SurvivalGameManager.Instance.SimulateServerResponse("day");
    await Wait(0.5f);
    var shot4 = await Capture("04_ingame");
    Assert(GameUIPanel is visible, shot4);
    Assert(TopBar is visible, shot4);

    // 5. 触发结算
    SurvivalGameManager.Instance.SimulateSettlement();
    await Wait(0.5f);
    var shot5 = await Capture("05_settlement");
    Assert(SettlementPanel is visible, shot5);

    // 6. 退出到大厅
    SurvivalGameManager.Instance.SimulateServerResponse("idle");
    await Wait(0.5f);
    var shot6 = await Capture("06_back_to_lobby");
    Assert(LobbyPanel is visible, shot6);

    // 7. 打开排行榜
    SurvivalIdleUI.Instance.OnRankingClicked();
    await Wait(0.3f);
    var shot7 = await Capture("07_ranking");
    Assert(RankingPanel is visible, shot7);

    Debug.Log("✅ Unity UI 自动化测试全部通过！");
    // 输出截图报告到 Logs/AutoTest/
}
```

### 验收标准（全部通过才算 Phase 3 完成）
- [ ] `test_m3_full_loop.js` 全部断言通过
- [ ] `AutoTest_GameLoop.cs` 全部截图验证通过，截图保存到 `Logs/AutoTest/`
- [ ] 10人并发模拟测试：666弹幕/随机事件/礼物全部正常触发
- [ ] 音效全部有声（BGM切换/礼物音效/资源警告）
- [ ] 排行榜正确显示数据并可关闭
- [ ] 结算三屏正确显示数据
- [ ] 全程无 Console Error

---

## 执行顺序建议

```
Day 1（约6小时）：
  M3-00（10分钟）→ M3-01（3小时）→ M3-02（2小时）→ 服务器部署

Day 2（约6小时）：
  M3-03（4小时）→ M3-04（2小时）

Day 3（约5小时）：
  M3-05（3小时）→ M3-06（1小时）→ M3-07视觉验证（1小时）

Day 4（约2小时）：
  M3-08 全循环自动化测试 → 修复发现的问题
```

---

## 本计划制定说明

> 本计划基于 2026-02-24 全面代码审计结果制定（见 SESSION_LOG_20260224.md 附录）。
> 每个模块均包含：问题根因分析、具体修复代码、自测脚本、验收标准。
> AI 负责按此计划主动推进，不等待用户逐一发现问题后再补救。
