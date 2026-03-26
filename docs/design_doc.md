# 极地生存法则 — 策划案 v3.0

> **版本**: v3.0 | **日期**: 2026-02-25 | **作者**: 策划Claude | **状态**: Phase 3 进行中，设计文档重大升级
>
> ⚠️ 本文档是唯一权威设计文档。开发Claude按此执行，**不得自行修改数值**。
>
> **v2.0 新增内容**: §0 产品链路图 | §3 精简弹幕指令（10→5+666）| §5.2 礼物视觉规格 | §新增 守护者排名竞争系统 | §新增 UI主题变量 | §新增 面板元素表 | §新增 Worker动画规格 | §新增 音效规格 | §新增 主播交互设计 | §新增 审核模式
>
> **v2.1 新增内容**: §1.3 游戏启动流程（大厅→Loading→游戏→退出）| §新增 Worker角色规格（CowWorker.prefab）
>
> **v3.0 新增内容（多Agent协作，2026-02-25）**: §20 交互闭环系统（11个完整状态机）| §21 技术美术规格（材质/光照/粒子/打击感/动画/后处理）| §22 完整开发计划（242个任务，10个模块，~938h）
>
> **设计评审（barrage-design-review）**: 综合评分 67/100（弹幕完整性72/落地完成度71/可玩性付费60）。v3.0核心改进：①礼物gift_id白名单T172补入服务器任务 ②守护者排名系统强化（见§9）③里程碑胜利机制（见§2.4）
>
> **详细规格模块**（像素级）: `docs/modules/` 目录

---

## §0. 产品链路图（必读）

```
【产品链路】
架构层 → 设计层 → 代码层 → 表现层 → 用户体验层

[✅ Phase 1 完成]
架构层: Node.js服务器 + WebSocket协议 + 抖音Push接入
设计层: 数值系统 + 弹幕映射 + 礼物效果 + 昼夜节奏
代码层: SurvivalGameEngine.js + 100人压测通过 + WebSocket联调通过

[🚧 Phase 2 目标]
表现层: 3D场景渲染 + Worker动画 + 礼物特效 + 音效 + 主播交互
体验层: 弹幕→工人移动→资源增加→视觉反馈→观众情绪曲线

【当前问题（Coplay自测暴露）】
❌ 3D场景黑屏（Camera/URP配置问题）
❌ Worker 0% 动画（仅Capsule占位符）
❌ 礼物通知 0%（GiftNotificationUI.cs 已标注"已弃用"）
❌ 主播无任何可点击交互
❌ 整体观感：黑屏+数字变化，不是一个游戏
```

---

## §1. 游戏概述

| 项目 | 值 |
|------|-----|
| 游戏名称 | 极地生存法则 |
| 平台 | 抖音直播（竖屏 1080×1920，9:16） |
| 引擎 | Unity 2022.3.47f1 + URP（**3D**，非Spine 2D） |
| 类型 | 全员协作PVE生存 + 个人贡献竞争排名 |
| 目标FPS | 30（最低20） |
| 技术栈 | Node.js + WebSocket + Unity |
| 服务器IP | 101.34.30.65:8081 |
| 本地路径 | D:/claude/drscfz |

### 核心玩法一句话
> 所有直播间观众共同扮演村民，**白天分工采集资源，夜晚消耗资源抵御怪物攻城**，三种失败条件并行，任一触发即结算。**守护者排名实时可见，刺激个人贡献竞争，驱动礼物送出。**

### 核心循环
```
开播 → 观众发弹幕加入（积分开始累计，守护者排名实时更新）
    ↓
【白天】发弹幕 1/2/3/4 分工采集资源 | 666弹幕全局加速
    ↓
资源积累 → 随机事件触发（暴风雪/丰收/怪物潮等）
    ↓
【夜晚】怪物攻城，发弹幕 6 打怪，送礼物召唤援军
    ↓
通过一夜 → +1天 → 难度递增 → 排名压力加剧 → 礼物驱动增强
    或
任一失败条件触发 → 结算（失败原因 + 存活天数 + MVP展示）
```

### 失败条件（三线并行检测）
| 条件 | 触发值 | 检测间隔 | bobao播报 |
|------|--------|---------|---------|
| 食物耗尽 | food_stock ≤ 0 | 1000ms | "村子断粮了，大家撑不住了..." |
| 炉火熄灭 | furnace_temp ≤ -100℃ | 1000ms | "炉火熄灭，极寒吞噬了村庄..." |
| 城门沦陷 | gate_hp ≤ 0 | 200ms | "城门被攻破，怪物涌入了！" |

---

### §1.3 游戏启动流程（大厅 → Loading → 游戏 → 退出）

> ✅ 已实现（2026-02-24，#110批次）

#### 完整状态机
```
[Play Mode 启动]
      ↓
[ConnectPanel = 自动连接中] ← 已实现，SurvivalConnectUI.cs
      ↓ OnConnected
[LobbyPanel = Idle 状态] ← 大厅界面
  - 标题："极地生存法则"
  - 连接状态："已连接" / "连接中..."
  - 状态文字："等待主播开始游戏..."
  - 按钮：▶ 开始玩法 | 排行榜 | 设置（占位）
      ↓ 主播点击 "▶ 开始玩法"
[LoadingPanel = Loading 状态, entering=true]
  - 文字："准备进入战场..."
  - Spinner 旋转动画
  - 客户端发送 start_game
  - 等待服务器 survival_game_state { state: "day" / "night" }
  - 超时保护：15秒强制回 Idle
      ↓ 服务器确认
[游戏UI = Running 状态] ← 正常游戏进行
      ↓ 主播点击 ExitBtn（右上角）
[LoadingPanel = Loading 状态, entering=false]
  - 文字："正在退出，返回大厅..."
  - 客户端发送 reset_game
  - 等待服务器 survival_game_state { state: "idle" }
  - 超时保护：15秒强制回 Idle
      ↓ 服务器确认
[LobbyPanel = Idle 状态] ← 回大厅（不走结算）

[游戏正常结束（胜负）]
  → SurvivalSettlementUI 三屏结算
  → 结算完毕 → 服务器推送 survival_game_state(idle) → 回大厅
```

#### UI Canvas 面板结构（预创建，Rule#2）
| 面板 | 路径 | 控制脚本 | 显示条件 |
|------|------|---------|---------|
| ConnectPanel | Canvas/ConnectPanel | SurvivalConnectUI | 未连接 |
| LobbyPanel | Canvas/LobbyPanel | SurvivalIdleUI | Idle + Connected |
| LoadingPanel | Canvas/LoadingPanel | SurvivalLoadingUI | State == Loading |
| GameUIPanel | Canvas/GameUIPanel | SurvivalGameplayUI | State == Running |
| SettlementPanel | Canvas/SettlementPanel | SurvivalSettlementUI | State == Settlement |

#### 关键实现规则
- **Rule#1**（服务器权威）：所有状态转换必须等待服务器 `survival_game_state` 消息，禁止本地直接跳转
- **Rule#2**（UI预创建）：所有面板在 Scene 预创建，通过 `SetActive(true/false)` 控制显隐
- **Rule#7**（脚本挂载）：`SurvivalLoadingUI` 挂在 Canvas（always-active），不挂在 LoadingPanel 上
- 退出到大厅不走结算屏，直接 `ResetAllSystems() + ChangeState(Idle)`

#### 服务器消息对应
| 客户端动作 | 发送消息 | 等待响应 | 状态转换 |
|-----------|---------|---------|---------|
| 点击开始玩法 | `start_game` | `survival_game_state` state=day/night | Idle→Loading→Running |
| 点击退出 | `reset_game` | `survival_game_state` state=idle | Running→Loading→Idle |

---

### §1.4 Worker 角色规格（CowWorker）

> ✅ 代码已实现，场景替换待执行（FixWorkerMesh.cs）

| 参数 | 值 |
|------|----|
| Prefab 路径 | `Assets/Prefabs/Characters/CowWorker.prefab` |
| 骨骼类型 | Mixamo（mixamorig:Hips 等） |
| 渲染组件 | SkinnedMeshRenderer（子对象 tripo_node_8a422929） |
| 约高 | 2m（AABB ~2.06×2.02×1.89） |
| 场景中 scale | (0.5, 0.5, 0.5) → 有效高度 ~1m |
| 场景挂载方式 | 作为每个 Worker_XX 的子对象 "Body"，通过 FixWorkerMesh.cs 批量写入 |
| Worker 数量 | 20个（Worker_00 ~ Worker_19，在 WorkerPool 下预创建） |

**WorkerVisual 颜色系统**（根据工位 commandId）：
| commandId | 工位 | 颜色 | HEX |
|-----------|------|------|-----|
| 1 | 采食物 | 蓝色 | #4488FF |
| 2 | 挖煤 | 深灰 | #666666 |
| 3 | 挖矿 | 冰蓝 | #88CCFF |
| 4 | 生火 | 橙红 | #FF6820 |
| 6 | 打怪 | 鲜红 | #FF2200 |

**特殊状态**：
- **Glow（金色光晕）**：666弹幕/主播加速触发，3秒后恢复
- **Frozen（冰蓝冻结）**：魔法镜礼物触发，30秒后恢复

---

## §2. 昼夜系统

| 参数 | 值 | 说明 |
|------|-----|------|
| 白天时长 | **240秒**（4分钟） | 资源采集阶段 |
| 夜晚时长 | **150秒**（2.5分钟） | 怪物防守阶段 |
| 白天采集效率 | ×1.0 | 基准 |
| 夜晚采集效率 | ×0.5 | 白天的一半 |
| 白天炉温衰减 | **-0.5℃/秒** | 慢速冷却 |
| 夜晚炉温衰减 | **-2.0℃/秒** | 极寒加速 |

---

## §3. 弹幕指令系统（v2.0 精简版）

> **v2.0 重大变更**：指令从10条精简到**5条核心 + 1条666**。
> 理由：观众看直播只有1-3秒决策时间，10条指令=没有指令。
> 指令5修墙→改为礼物自动触发；换/查看/兑换→移至主播控制台或后期版本。

### 核心弹幕指令（6条）

| 指令 | 别名 | 效果 | 有效阶段 | 夜晚修正 | 工人动作 |
|------|------|------|---------|---------|---------|
| **1** | 摸鱼/采鱼 | 采集食物 +5/200ms | 昼/夜 | ×0.5 | 走向鱼塘→挥网动画 |
| **2** | 挖煤 | 采集煤炭 +3/200ms | 昼/夜 | ×0.5 | 走向煤矿→挥镐动画 |
| **3** | 挖矿 | 采集矿石 +2/200ms | 昼/夜 | ×0.5 | 走向矿山→挥镐动画 |
| **4** | 生火/烧火 | 炉温 +3℃/200ms（消耗-1煤/200ms） | 昼/夜 | 无修正 | 走向炉灶→生火动画 |
| **6** | 攻击/打怪 | 随机命中1怪，造成20伤害 | **仅夜晚** | — | 走向城门→挥剑动画 |
| **666** | — | 全局效率+15%，持续30秒，不叠加（刷新计时） | 昼/夜 | — | 所有工人发光特效 |

> **已删除的指令**（v1.0→v2.0）：
> - ~~指令5（修墙）~~ → 改为礼物甜甜圈/能量电池自动触发城门修复
> - ~~换/查看/兑换~~ → 角色系统Phase 3实现，主播可通过控制台手动触发

### 冷却与防刷规则
| 规则 | 数值 |
|------|------|
| 单用户同指令冷却 | 2000ms |
| 弹幕聚合窗口 | 200ms（窗口内累加计算） |
| 防刷阈值 | 同用户1秒内>5条相同弹幕，忽略超出部分 |
| 无效指令 | 服务端静默丢弃，不播报 |

### 新用户引导
- 首次加入：欢迎播报 + 顶部5秒教程卡片（仅显示5个指令：1/2/3/4/6）
- 智能提示：根据当前最紧缺资源高亮建议指令（食物<100时提示"发1采集食物"）

---

## §4. 核心数值（策划确认值，服务器权威）

### 4.1 资源初始值与速率

| 资源 | 初始值 | 上限 | 基础采集基数(/200ms/人) | 警告阈值 | 警告表现 |
|------|--------|------|----------------------|---------|---------|
| **食物** | **500** | 2000 | +5 | ≤100 | 红色闪烁+bobao |
| **煤炭** | **300** | 1500 | +3 | ≤60 | 红色闪烁 |
| **矿石** | **150** | 800 | +2 | ≤30 | 红色闪烁 |

### 4.2 炉温系统

| 参数 | 值 |
|------|-----|
| 初始炉温 | **20℃** |
| 最大炉温 | 100℃ |
| 失败阈值 | **-100℃** |
| 生火加热（有煤炭） | **+3℃/200ms**，同时消耗 **-1煤炭/200ms** |
| 生火加热（无煤炭） | **无效**，bobao播报："煤炭不足，无法生火！" |
| 警告 ≤ -50℃ | UI 蓝色闪烁 |
| 警告 ≤ -80℃ | UI 紫色闪烁 + 警报音效 |

### 4.3 城门系统

| 参数 | 值 |
|------|-----|
| 初始HP | **1000** |
| 失败阈值 | HP = 0 |
| 升级：1级→2级 | 消耗 100 矿石，HP上限+500（变为1500） |
| 警告阈值 | HP ≤ 30% 时红色闪烁 |

> **v2.0 变更**：指令5修墙已移除。城门修复改为礼物触发（T3甜甜圈+200HP，T4能量电池不修，T5神秘空投+300HP）

### 4.4 工作效率系统

**公式**：`actual_effect = base_value × phase_modifier × efficiency_multiplier`

| 修正来源 | 效果 | 持续时间 | 可叠加 |
|---------|------|---------|-------|
| 夜晚阶段（系统） | ×0.5 | 整个夜晚 | 不可（系统强制） |
| 666弹幕 / 点赞 | +15% | **30秒** | 不叠加，刷新计时 |
| 仙女棒礼物 | +5%/个 | 永久 | **叠加**，上限+100%（即最高×2.0） |
| 超能喷射分身 | ×2.0 | 60秒 | 同一玩家不叠加 |
| 主播⚡紧急加速 | ×2.0 | 30秒 | 与其他叠加，CD=120s |
| **全局效率上限** | **×3.0** | — | 所有加成综合上限 |

---

## §5. 礼物系统

### 5.1 礼物总表（7种）

| ID | 中文名 | 抖币价格 | 等级 | 类型 | 效果 | 本场得分 |
|----|--------|---------|------|------|------|---------|
| `fairy_wand` | **仙女棒** | 0.1 | T1 | 正面·个人 | 发送者效率永久+5%（叠加，上限+100%） | 1 |
| `ability_pill` | **能力药丸** | 10 | T2 | 正面·全局 | 全局食物+50，召唤守卫攻击当前波次怪物（持续30秒） | 100 |
| `magic_mirror` | **魔法镜** | 10 | T2 | 负面·趣味 | 随机冻结1名已加入玩家30秒（无法工作，趣味捣乱） | 0 |
| `donut` | **甜甜圈** | 52 | T3 | 正面·全局 | 城门修复+200HP，全局食物+100 | 500 |
| `energy_battery` | **能量电池** | 99 | T4 | 正面·全局 | 炉温+30℃，全局效率+30%（持续60秒） | 1000 |
| `mystery_airdrop` | **神秘空投** | 520 | T5 | 正面·全局 | 超级补给：食物+500、煤炭+200、矿石+100、城门+300HP；触发 GIFT_PAUSE 3000ms | 5000 |
| `super_jet` | **超能喷射** | 52 | T3 | 正面·个人 | 发送者召唤分身，效率×2（持续60秒） | 500 |

### 5.2 礼物视觉规格（v2.0 新增，详细表现层设计）

#### T1 — 仙女棒（0.1抖币）

| 属性 | 规格 |
|------|------|
| **特效描述** | 发送者头像周围出现5颗星形粒子旋转一圈后消散 |
| **位置** | 在弹幕滚动条中该玩家名字旁边，局限在名字区域内 |
| **屏幕占比** | 5%（小型，不抢镜） |
| **持续时长** | 0.5秒 |
| **音效** | 清脆铃声 × 1（sfx_gift_t1_ding） |
| **bobao播报** | 无（频率过高） |
| **技术实现** | ParticleSystem，5个星形粒子，0.2s旋转+0.3s淡出 |

#### T2 — 能力药丸 / 魔法镜（10抖币）

| 属性 | 规格 |
|------|------|
| **特效描述** | 屏幕四角同时发出粒子向中心汇聚，在中心爆发一圈彩色光环后消散 |
| **位置** | 全屏边缘 |
| **屏幕占比** | 30%（中等，可见但不遮挡游戏） |
| **持续时长** | 2秒（0.5s入场 + 1s留存 + 0.5s淡出） |
| **音效** | 魔法泡泡声（sfx_gift_t2_bubble） |
| **bobao播报** | 无 |
| **礼物名牌** | 左侧弹出小字条：「[昵称] 送出 能力药丸」，蓝色背景 |
| **魔法镜特殊** | 中心爆发后有冰晶效果，被冻结玩家名字变蓝+❄️图标2秒 |
| **技术实现** | 4个角落ParticleSystem，中心光环Image(Scale 0→1.5→0) |

#### T3 — 甜甜圈 / 超能喷射（52抖币）

| 属性 | 规格 |
|------|------|
| **特效描述** | 礼物大图标从屏幕外飞入中央，抖动3次后碎成金色粒子爆炸 |
| **位置** | 屏幕正中央 |
| **屏幕占比** | 40%（图标尺寸240×240px） |
| **持续时长** | 3秒（0.5s飞入 + 1.5s抖动 + 1s爆炸淡出） |
| **音效** | 礼炮声（sfx_gift_t3_boom） |
| **bobao播报** | "感谢 [昵称] 的 [礼物名]！" |
| **技术实现** | Tween飞入(from外侧x→center, EaseOutBounce, 0.5s)；抖动(scale 1→1.1→1, ×3)；爆炸ParticleSystem(金色粒子20个) |

#### T4 — 能量电池（99抖币）

| 属性 | 规格 |
|------|------|
| **特效描述** | 全屏闪现暖橙色光晕，中央出现能量电池大图+充能进度条动画，背景变暖色调2秒 |
| **位置** | 全屏 |
| **屏幕占比** | 80%（全屏光晕 + 中央图标）|
| **持续时长** | 5秒（0.3s闪入 + 4s展示 + 0.7s淡出） |
| **音效** | 电能充充充+爆鸣（sfx_gift_t4_electric） |
| **bobao播报** | "⚡ 感谢 [昵称] 的能量电池！村庄因你而温暖！" (黄色粗体) |
| **特殊效果** | 进度条从0%充到100%（动画3秒），充满后数字闪烁+炉温图标跳动 |
| **技术实现** | FullscreenOverlay(alpha 0→0.4→0, 橙色)；中央Image(scale 0→1.2→1)；Slider动画0→1，3s；背景PostProcess色温+30 |

#### T5 — 神秘空投（520抖币）★史诗★

| 属性 | 规格 |
|------|------|
| **特效描述** | 游戏暂停3秒：黑色遮罩淡入→空投箱从天而降→着地爆炸（金色烟花）→资源图标从箱子飞出散落→大字「[昵称] 拯救了村庄！」→遮罩淡出恢复游戏 |
| **位置** | 全屏独占（游戏PAUSE） |
| **屏幕占比** | 100%（全屏遮罩，游戏暂停） |
| **持续时长** | **8秒**（3秒PAUSE + 5秒展示）|
| **音效** | 飞机声→着地爆炸声→烟花声→胜利号角（sfx_gift_t5_airdrop序列） |
| **bobao播报** | "💥 超级感谢 [昵称] 的神秘空投！村庄得救了！" (金色，字号+50%，全屏顶部3秒) |
| **GAME PAUSE** | 触发服务器 GIFT_PAUSE=3000ms，所有tick暂停 |
| **玩家名高亮** | 该玩家在守护者排名中边框变金色闪烁10秒 |
| **技术实现** | Canvas遮罩Black(alpha 0→0.85, 0.3s)；空投箱Prefab从Y+2000落下(EaseInCubic, 1s)；着地ParticleSystem(金色烟花，2s)；资源图标飞散Tween；TMP大字Scale 0→1(EaseOutBack)；遮罩淡出0.5s |

### 5.3 礼物抖音ID映射（待后台填写）

```javascript
const GIFT_ID_MAP = {
  fairy_wand:       { name: "仙女棒",  douyin_id: "TBD", price_fen: 1    },
  ability_pill:     { name: "能力药丸",douyin_id: "TBD", price_fen: 100  },
  magic_mirror:     { name: "魔法镜",  douyin_id: "TBD", price_fen: 100  },
  donut:            { name: "甜甜圈",  douyin_id: "TBD", price_fen: 520  },
  energy_battery:   { name: "能量电池",douyin_id: "TBD", price_fen: 990  },
  mystery_airdrop:  { name: "神秘空投",douyin_id: "TBD", price_fen: 5200 },
  super_jet:        { name: "超能喷射",douyin_id: "TBD", price_fen: 520  },
};
```

---

## §6. 怪物波次设计

| 天数 | 普通怪HP | 普通怪攻击(/秒) | 精英怪HP | Boss HP | 夜晚波次数 |
|------|---------|--------------|---------|--------|---------|
| Day 1 | 30 | 3 | — | 200 | 1波 |
| Day 2 | 40 | 4 | 100 | 400 | 2波 |
| Day 3 | 55 | 6 | 150 | 700 | 2波 |
| Day 4 | 75 | 8 | 200 | 1000 | 3波 |
| Day 5 | 100 | 11 | 280 | 1500 | 3波 |
| Day 6 | 130 | 15 | 350 | 2200 | 4波 |
| Day 7 | 160 | 20 | 500 | 3500 | 4波 |

**Day 7以后**：普通怪HP × 1.15/天，攻击 × 1.1/天，波次数每2天+1，最多6波/夜

**玩家攻击规则（指令6）**：随机命中1只怪物，20点伤害，单用户冷却2秒

---

## §7. 角色系统（Phase 3 实现，当前Phase 2 只保留数据结构）

| 角色 | 解锁条件 | 专精加成 |
|------|---------|---------|
| **普通村民** | 默认解锁 | 无 |
| **渔夫** | 100积分 | 采集食物效率+20% |
| **矿工** | 100积分 | 采集矿石效率+20% |
| **守火人** | 500积分 | 生火效率+20%，炉温回升速度+30% |
| **战士** | 500积分 | 攻击伤害+50%（20→30） |
| **工程师** | 2000积分 | Phase 3 时修墙指令恢复后生效 |

---

## §8. 随机事件系统

| 事件ID | 名称 | 触发概率 | 效果 | 持续时间 |
|--------|------|---------|------|---------|
| E01 | **暴风雪加剧** | 25% | 炉温衰减速率×2 | 60秒 |
| E02 | **丰收季节** | 25% | 食物采集效率×1.5 | 90秒 |
| E03 | **怪物潮** | 15% | 当夜怪物数量×1.5 | 整个夜晚 |
| E04 | **发现煤矿** | 20% | 煤炭采集效率×2 | 90秒 |
| E05 | **矿石富矿** | 15% | 矿石采集效率×2 | 90秒 |

触发规则：白天每90-120秒随机触发一次，同时最多1个活跃事件，触发时全屏播报3秒

---

## §9. 积分与守护者排名系统（v2.0 增强付费驱动）

### 积分来源
| 行为 | 积分 | 备注 |
|------|------|------|
| 有效弹幕指令（1/2/3/4/6） | +1/次 | 冷却内有效次才计 |
| 发 666 / 点赞激励 | +2/次 | |
| 送礼物 | = 礼物得分（见 §5.1） | 仙女棒=1，神秘空投=5000 |
| 存活天数加成（结算时） | +10/天 | |

### 守护者排名（v2.0 新增 — 付费驱动核心）

> **设计意图**：在协作框架内引入个人竞争。谁是最强守护者？实时可见的排名比任何文字都有说服力。

| 属性 | 规格 |
|------|------|
| **显示位置** | 屏幕右侧中间，常驻（不随场景变化消失） |
| **显示内容** | Top 5 守护者：排名数字 + 头像 + 昵称 + 积分 + 排名变化箭头(↑↓) |
| **更新频率** | 实时（每次积分变化立即推送） |
| **排名变化动效** | 超越时：被超越者名字红色闪烁，超越者绿色闪烁，持续1秒 |
| **bobao超越播报** | "[玩家A] 超过了 [玩家B]！" （排名前3变化时触发） |
| **第1名高亮** | 排名第1的玩家昵称在所有bobao播报中黄色高亮显示 |
| **送T5礼物** | 全屏暂停期间守护者排名中该玩家金色边框闪烁10秒 |

### 榜单类型
| 榜单 | 内容 | 显示位置 | 重置规则 |
|------|------|---------|---------|
| 本场守护者榜 | 当局实时Top5（常驻右侧） | 屏幕右侧中间 | 每局重置 |
| 本场贡献榜（结算用） | 当局积分Top3 | 结算C屏 | 每局重置 |

---

## §10. 主播交互设计（v2.0 新增）

> **背景**：主播是直播间的灵魂，必须让主播有"权力感"。两个按钮让主播成为游戏的一部分，不只是旁观者。

### 主播控制面板布局
- **位置**：屏幕右侧底部，Y=1400-1600，2个圆形大按钮（直径120px）
- **背景**：半透明深色面板，宽度200px，仅在主播模式下显示（服务器判断room_creator）

### 按钮1：⚡ 紧急加速

| 属性 | 规格 |
|------|------|
| **图标** | 黄色闪电符号 |
| **效果** | 全局效率×2，持续30秒（与其他效率加成叠加，但受×3.0上限限制） |
| **冷却** | **120秒** CD（CD期间按钮灰色，显示倒计时）|
| **视觉反馈** | 所有工人同时发出金色光晕3秒；顶部大字"⚡主播激活紧急加速！" |
| **音效** | sfx_broadcaster_boost |
| **bobao播报** | "⚡ 主播激活紧急加速！全体效率翻倍30秒！" |
| **服务器消息** | `{type: "broadcaster_action", action: "efficiency_boost", duration: 30000}` |

### 按钮2：🌊 触发事件

| 属性 | 规格 |
|------|------|
| **图标** | 绿色波浪符号 |
| **效果** | 随机触发一个随机事件（§8的E01-E05，各20%概率） |
| **冷却** | **60秒** CD |
| **视觉反馈** | 全屏播报触发的事件（与正常随机事件相同效果，但多一行"主播触发了..."）|
| **音效** | sfx_broadcaster_event |
| **bobao播报** | "🌊 主播触发了 [事件名]！" |
| **服务器消息** | `{type: "broadcaster_action", action: "trigger_event"}` |

### 主播面板技术规范
- 文件：`Assets/Scripts/UI/BroadcasterPanel.cs`（新建）
- 服务器：`Server/src/SurvivalRoom.js` 添加 `broadcaster_action` 消息处理
- 仅当 `isRoomCreator=true` 时显示（服务器在 `join_room` 响应中返回）

---

## §11. 审核模式（v2.0 新增）

> **背景**：抖音小玩法上架前需要审核，审核期间不能接真实观众但游戏需要正常运转。之前完全缺失此设计是严重遗漏。

```javascript
// 服务器配置（SurvivalRoom.js）
const REVIEW_MODE = process.env.REVIEW_MODE === 'true'; // 默认 false

// 审核模式行为
if (REVIEW_MODE) {
  // 1. 自动模拟20个虚假玩家发弹幕
  // 机器人名字：村民A/B/C...T
  // 机器人每秒随机选择指令1/2/3/4（不发6，因为只在夜晚有效）
  // 机器人不发666，由主播手动触发

  // 2. 自动演示礼物（每30秒触发T3礼物）
  // 模拟礼物发送者：审核演示Bot

  // 3. 不连接真实抖音WebHook
  // DOUYIN_PUSH_SECRET 设为空字符串，拒绝所有来自外部的push

  // 4. 游戏正常运转，展示完整游戏流程
  // 目标存活天数：3天（展示完整昼夜循环×3）
}
```

### 审核模式UI提示
- 屏幕左上角常驻小字提示："演示模式"（灰色小字，不影响画面）
- 机器人玩家头像使用默认头像+名字前缀"[演]"

---

## §12. UI布局规格（精确像素，基准分辨率 1080×1920）

> **详细像素规格** 见 `docs/modules/panels/` 目录（每个面板独立文件）。
> 以下为汇总表。

| 元素 | 锚点/位置(x,y) | 尺寸(w,h) | 字号 | 颜色/备注 |
|------|--------------|----------|------|---------|
| **顶部状态栏** | (0, 0) | (1080, 120) | — | 半透明深色背景 `#0D1A2ACC` |
| 食物图标+数值 | (20, 36) | (48+90, 48) | 32px | 图标🍖左·数值右，暖橙`#FF8C3A` |
| 煤炭图标+数值 | (190, 36) | (48+90, 48) | 32px | 图标🪵左·数值右，灰白`#C8C8C8` |
| 矿石图标+数值 | (360, 36) | (48+90, 48) | 32px | 图标🪨左·数值右，青蓝`#88CCFF` |
| 炉温（温度计+数字） | (530, 16) | (160, 88) | 28px | -100℃=蓝`#4A90D9`，100℃=橙`#FF8C3A` |
| 倒计时（昼/夜图标+秒数） | (710, 20) | (180, 80) | 40px | 白天`#FFD700`，夜晚`#99AAFF` |
| 城门HP条 | (910, 34) | (150, 20) | — | 绿→红渐变，≤30%红闪 |
| **守护者排名（右侧中间）** | (890, 700) | (180, 400) | — | 常驻，见panel_ranking.md |
| **底部bobao滚动** | (20, 1340) | (540, 120) | 28px | 白色描边，向左滚动 |
| **底部弹幕滚动条** | (20, 1460) | (1040, 200) | — | 见panel_barrage.md |
| **礼物/事件通知弹窗** | (90, 120) | (900, 240) | 32px | 居中弹出，2-8秒后淡出（取决于礼物等级） |
| **主播控制面板** | (860, 1380) | (200, 240) | — | 仅主播可见，见panel_broadcaster.md |
| **场景区（3D游戏画面）** | (0, 0) | (1080, 1600) | — | UI层覆盖在3D场景上方 |

---

## §13. Canvas层级规范（v2.0 新增）

| Canvas名称 | Sort Order | 用途 |
|---|---|---|
| GameWorld_Canvas | 0 | Worker头顶气泡/血条（世界空间UI） |
| HUD_Canvas | 10 | 顶部状态栏+守护者排名+bobao（常驻） |
| Barrage_Canvas | 20 | 底部弹幕滚动条 |
| Alert_Canvas | 30 | 资源警报/倒计时警告 |
| Gift_Canvas | 100 | 礼物通知特效（T1-T5，穿透射线） |
| Settlement_Canvas | 50 | 结算三屏（A/B/C屏序列） |
| Broadcaster_Canvas | 60 | 主播控制面板 |
| Overlay_Canvas | 200 | T5遮罩/加载/断连画面（最高层） |

---

## §14. Worker动画规格（v2.0 新增）

> **详细动画规格** 见 `docs/modules/animation_spec.md`。以下为汇总。

### 工人模型规格
- 当前：Capsule占位符（红色/蓝色/绿色材质区分类型）
- Phase 2目标：保留Capsule形态，添加材质+头顶气泡（显示工作类型图标）
- Phase 3目标：替换为低多边形(Low-poly)人形模型

### 工人状态机（5状态）

| 状态 | 触发条件 | 动画描述 | 持续 |
|------|---------|---------|------|
| **Idle（待机）** | 无指令时 | 原地轻微晃动（Bob上下0.1单位，1秒循环） | 循环 |
| **Move（移动）** | 收到指令后 | 向目标工位移动，速度3单位/秒，直线移动 | 单次，到达后结束 |
| **Work（工作）** | 到达工位后 | 工具挥动动画（左右摆动30度，0.5秒一次） | 循环，持续至CD结束 |
| **Special（特殊）** | 666弹幕/主播加速 | 全身发金色光晕3秒 | 单次3秒 |
| **Frozen（冻结）** | 魔法镜礼物 | 原地结冰（蓝色粒子覆盖，不动） | 30秒 |

### 工位布局（3D坐标，Y=0地面）

| 工位 | 3D位置(X,Z) | 工作类型 | 最大工人数 |
|------|-----------|---------|---------|
| 鱼塘 | (-8, -5) | 采食物（指令1） | 10人 |
| 煤矿 | (-4, 8) | 挖煤（指令2） | 8人 |
| 矿山 | (6, 7) | 挖矿（指令3） | 6人 |
| 炉灶 | (0, -3) | 生火（指令4） | 5人 |
| 城门 | (8, -6) | 打怪（指令6，夜晚专用） | 15人 |
| 待机区 | (0, 0) | Idle状态工人聚集 | 无上限 |

### 工人头顶气泡规格
- 大小：64×64px（World Space UI，随摄像机距离缩放）
- 内容：工作类型emoji（🐟/⛏/🪨/🔥/⚔️）
- 背景：圆形白色，80%透明度
- 显示时机：工作状态时显示，待机时隐藏

---

## §15. 音效规格（v2.0 新增）

> **详细音效清单** 见 `docs/modules/audio_spec.md`。以下为汇总。

### 背景音乐
| 场景 | BGM | BPM | 时长 | 风格 |
|------|-----|-----|------|------|
| 白天 | bgm_day_winter | 100 BPM | 90秒循环 | 轻松冬日，木吉他+钟声 |
| 夜晚 | bgm_night_danger | 140 BPM | 60秒循环 | 紧张危机，鼓+弦乐 |
| 结算（胜） | bgm_win | — | 10秒，不循环 | 凯旋号角 |
| 结算（败） | bgm_lose | — | 8秒，不循环 | 哀婉钢琴 |

**切换规则**：白天→夜晚：3秒AudioMixer淡出淡入；夜晚→白天：2秒淡出淡入

### 核心SFX清单
| SFX ID | 触发时机 | 描述 |
|--------|---------|------|
| sfx_collect_food | 指令1有效时（每次） | 轻快"叮"声 |
| sfx_collect_coal | 指令2有效时 | 沉闷镐击声 |
| sfx_collect_ore | 指令3有效时 | 金属碰撞声 |
| sfx_fire_crackling | 指令4有效时（循环） | 噼啪火焰声 |
| sfx_monster_attack | 怪物攻击城门时 | 轰鸣撞击声 |
| sfx_gate_alarm | 城门HP≤30% | 警报声（3秒循环直到HP恢复） |
| sfx_cold_alarm | 炉温≤-80℃ | 寒风呼啸+警报 |
| sfx_gift_t1_ding | T1礼物 | 清脆铃声 |
| sfx_gift_t2_bubble | T2礼物 | 魔法泡泡声 |
| sfx_gift_t3_boom | T3礼物 | 礼炮声 |
| sfx_gift_t4_electric | T4礼物 | 电能冲充爆鸣 |
| sfx_gift_t5_airdrop | T5礼物（序列） | 飞机→爆炸→烟花→号角 |
| sfx_broadcaster_boost | 主播⚡加速 | 能量激活音 |
| sfx_day_start | 白天开始 | 鸟鸣+钟声 |
| sfx_night_start | 夜晚开始 | 狼嚎+警报 |

**音效来源**：Unity Asset Store 免费包 或 freesound.org CC0授权。文件格式：OGG，单声道，44100Hz

---

## §16. 结算界面（三屏序列）

| 屏序 | 内容 | 持续时间 |
|------|------|---------|
| **A屏** | 大字显示存活天数（字号200px）+ 失败原因（食物/炉温/城门，对应红/蓝/橙色图标） | 3秒 |
| **B屏** | 数据统计：参与人数 / 弹幕总数 / 礼物收入金额 / 击退怪物数 / 最高在线 | 5秒 |
| **C屏** | MVP展示：Top3贡献者头像+积分+专属边框动画；**第1名金色发光边框（与普通排名有明显区别）** | 3秒 |
| 返回 | 自动进入待机界面，等待主播下一局 | — |

**C屏MVP特效（v2.0新增）**：
- 第1名：金色粒子持续环绕，姓名金色大字，动态发光边框
- 第2名：银色边框+姓名银色
- 第3名：铜色边框+姓名铜色
- 如有人送T5礼物：该玩家在C屏专门展示1秒"本场最强守护者"称号

---

## §17. 技术约束（不可违反）

| # | 规则 |
|---|------|
| 1 | **服务器权威**：所有游戏数值由服务器推送，客户端只做显示缓存，禁止客户端自行计算胜负 |
| 2 | **UI预创建**：所有面板在Scene中预创建，用 `SetActive(true/false)` 控制显隐，禁止运行时 Instantiate |
| 3 | **代码默认值=场景值**：`[SerializeField]` 字段的代码默认值必须与Unity Scene序列化值一致 |
| 4 | **场景保存**：必须用 `DrscfZ/Save Current Scene` 菜单，禁止用 Coplay save_scene |
| 5 | **打包**：用 `DrscfZ/Quick Build`，输出 `Build/drscfz_1.0.0/drscfz.exe` |
| 6 | **进度更新**：每次完成任务后，必须更新 `docs/progress.md` 对应任务行 |
| 7 | **Worker UI预创建**：最多20个Worker预实例化在Scene中，通过SetActive控制显隐 |
| 8 | **礼物特效预创建**：所有礼物特效面板预创建在Gift_Canvas下，SetActive触发 |

---

## §18. 待确认事项（需后台对齐）

| 项目 | 状态 | 负责人 |
|------|------|-------|
| 7种礼物的抖音商城ID | ⏳ 待填写 | 用户（需登录抖音直播后台查询） |
| DOUYIN_APP_ID / SECRET | ⏳ SECRET待填入服务器 .env | 用户 |

---

## §19. Phase 2 进度追踪

| 任务 | 负责方 | 状态 | 验收标准 |
|------|-------|------|---------|
| 策划案v2.0 | 主Claude | ✅ 完成 | 本文档 |
| 3D场景黑屏修复 | 主Claude+Coplay | 🚧 进行中 | capture_scene_object看到雪地场景 |
| Worker视觉系统 | 子Claude | ⏳ 待开始 | 10条弹幕→工人移动到工位 |
| 礼物特效系统 | 子Claude | ⏳ 待开始 | T5礼物全屏特效正常播放 |
| 主播交互面板 | 子Claude | ⏳ 待开始 | 点击⚡按钮→效率×2激活 |
| 音效基础层 | 子Claude | ⏳ 待开始 | Play Mode无报错，白天BGM播放 |
| docs/modules/ui_theme.md | 主Claude | ⏳ 待开始 | 完整颜色/字体/动画变量表 |
| docs/modules/panels/*.md | 主Claude | ⏳ 待开始 | 6个面板像素级规格 |
| docs/modules/animation_spec.md | 主Claude | ⏳ 待开始 | Worker5种动画完整规格 |
| docs/modules/audio_spec.md | 主Claude | ⏳ 待开始 | 15条SFX + 4首BGM完整规格 |

---

*本文档由策划Claude维护 | 最后更新：2026-02-25 v3.0*
*Phase 1（数值/服务器）完成度100% | Phase 2（表现层）完成度 5% | Phase 3（美术&优化）待启动*
*如需修改任何数值，请先知会策划Claude，不得由开发Claude自行修改*
*v3.0新增章节：§20 交互闭环系统 | §21 技术美术规格 | §22 完整开发计划（242任务）*

---

## §20 交互闭环系统（Interaction Loop System）

> 本章定义所有主要交互回路的完整状态规格。每个循环遵循「触发 → 处理 → 结束 → 异常」四段式结构，作为前端表现层（Unity）与服务器逻辑层交互的唯一参考标准。
>
> **架构前提**：客户端仅作表现层，所有状态权威数据来源于服务器推送。客户端不得在本地写死状态流转逻辑，一切以服务器消息为准。

---

### §20.1 大厅 → 游戏进入循环

**状态定义**

| 状态标识 | 描述 |
|---|---|
| `LOBBY_IDLE` | 大厅面板显示，等待主播操作 |
| `ENTER_PENDING` | 进入请求已发出，等待服务器响应 |
| `LOADING` | Loading 面板激活，Spinner 运行中 |
| `GAME_RUNNING` | 游戏 HUD 显示，游戏进行中 |

**状态机图**

```
[LOBBY_IDLE]
    │
    │ 主播点击"▶开始玩法"按钮
    ▼
[ENTER_PENDING]
    │ 按钮变灰（interactable=false）
    │ 按钮文字 → "正在进入战场..."
    │ 发送 start_game 到服务器
    │ LoadingPanel.SetActive(true)，Spinner 开始旋转
    │
    ├──────────────────────────────────────────────────────┐
    │                                                      │
    │ 收到 survival_game_state                          超时（15s 无响应）
    ▼                                                      ▼
[LOADING → 淡出]                                   显示"连接超时，返回大厅"
    │                                               LoadingPanel.SetActive(false)
    │ LoadingPanel.SetActive(false)                 按钮恢复可点击（interactable=true）
    │ LobbyPanel.SetActive(false)                   → 回到 [LOBBY_IDLE]
    │ GameHUD.SetActive(true)
    ▼
[GAME_RUNNING]
```

**UI 状态变化表**

| 元素 | 变化前 | 变化后 |
|---|---|---|
| `StartGameBtn` | 绿色 `#4CAF50`，可点击 | 灰色 `#9E9E9E`，不可点击 |
| 按钮文字 | "▶ 开始玩法" | "正在进入战场..." |
| `LoadingPanel` | `SetActive(false)` | `SetActive(true)` |
| Loading Spinner 颜色 | — | `#FFFFFF`，透明度 0.9 |
| 背景遮罩颜色 | — | `#000000`，透明度 0.6 |

**异常处理**

| 异常类型 | 触发条件 | 处理方式 |
|---|---|---|
| 连接超时 | 15 秒内未收到 `survival_game_state` | Toast"连接超时，返回大厅"（3秒），恢复 `LOBBY_IDLE` |
| 服务器返回错误 | 收到 `error` 消息 | Toast 显示错误描述，恢复 `LOBBY_IDLE` |
| WebSocket 断线 | 请求过程中连接断开 | Loading 面板显示"网络连接中断"，3 秒后自动回 `LOBBY_IDLE` |

---

### §20.2 退出 → 大厅循环

**状态机图**

```
[GAME_RUNNING]
    │ 主播点击右上角红色 ExitBtn（#F44336）
    ▼
[EXIT_PENDING]
    │ LoadingPanel.SetActive(true)
    │ 发送 reset_game 到服务器
    │ 启动 10s 超时计时器
    │
    ├── 收到服务器 idle 确认 ──────────────────┐
    │                                         │
    ▼                                         ▼
LoadingPanel.SetActive(false)        超时强制 [LOBBY_IDLE]
GameHUD.SetActive(false)             Toast"退出超时，已强制返回大厅"
LobbyPanel.SetActive(true)
    ▼
[LOBBY_IDLE]
```

**ExitBtn 规格**

| 参数 | 值 |
|---|---|
| 按钮颜色 | `#F44336`（红色） |
| 位置 | 游戏 HUD 右上角，始终可点击 |
| 图标 | ✕ 白色图标 |
| 悬停效果 | 颜色变为 `#D32F2F`，缩放 1.05（0.1s ease） |

---

### §20.3 弹幕指令工人工作循环

**完整闭环流程**

```
[观众发弹幕]
    ▼
[服务器验证层]
    ├─ 校验：当前是否 RUNNING 状态？
    ├─ 校验：该指令是否适用当前时段？（夜晚不可采集/白天不可打怪）
    ├─ 校验：该玩家 CD 是否已过？
    └─ 通过 → 广播 work_command
            ▼
[Unity 接收 work_command]
    ├─ 查找最近空闲工人
    ├─ 无空闲工人？→ 工位气泡"满员！"（1s），循环结束
    └─ 找到空闲工人
            ▼
    [WORKER_MOVING] 工人 walk 动画，NavMesh 寻路，速度 3.5 units/s
            ▼
    [到达工位] 播放 work_start（一次性）
            ▼
    [WORKER_WORKING] 循环 work_loop 动画，工位 Slot 标记 occupied
            │  服务器推送 resource_update → HUD 数字弹跳（缩放1.0→1.2→1.0，0.3s）
            │  数字颜色短暂变为 #76FF03（绿色），0.5s 后恢复白色
            │
            │ 收到 phase_change → 触发 [WORKER_RETURNING]
            ▼
    [WORKER_RETURNING] walk 动画，速度 4.0 units/s，返回待机区
            ▼
    [WORKER_IDLE]
```

**指令-工位映射表**

| 指令ID | 关键词 | 目标工位 | 有效时段 |
|---|---|---|---|
| 1 | 食物/饭/hungry | `slot_food` | 白天 |
| 2 | 煤炭/coal | `slot_coal` | 白天 |
| 3 | 矿石/ore | `slot_ore` | 白天 |
| 4 | 添柴/fire | `slot_furnace` | 全天 |
| 6 | 打怪/fight/杀 | `slot_gate` | 夜晚 |

**满员气泡规格**

| 参数 | 值 |
|---|---|
| 文字 | "满员！" |
| 背景颜色 | `#FF5722`，透明度 0.85 |
| 位置 | 工位上方 +80px |
| 出现动画 | 向上浮起 +20px，透明度 0→1，时长 0.2s |
| 消失动画 | 继续上浮 +20px，透明度 1→0，时长 0.3s |

---

### §20.4 礼物效果循环

**完整闭环流程**

```
[观众送礼] → [服务器接收 Webhook] → 计算等级T1~T5 + 游戏效果
    ▼
[Unity 接收 gift_received]
    ├─ 当前有特效播放中？→ 加入队列（最大10条，T1优先丢弃）
    └─ 队列为空？→ 立即播放
            ▼
    GiftEffectPanel.SetActive(true)
    显示送礼信息 → 播放对应等级特效序列
            ▼
    特效播放期间：effects 数据同步更新 HUD（资源数值弹跳）
            ▼
    特效结束 → GiftEffectPanel.SetActive(false)
            ├─ 队列有待处理 → 取下一个
            └─ 队列空 → [GIFT_IDLE]
```

**礼物等级特效规格**

| 等级 | 特效时长 | 全屏效果 | 送礼信息文字大小 |
|---|---|---|---|
| T1 | 1 秒 | 无 | 24px |
| T2 | 2 秒 | 无 | 24px |
| T3 | 3 秒 | 全屏光晕叠加 | 36px 加粗 |
| T4 | 5 秒 | 全屏遮罩+爆炸 | 36px 加粗 |
| T5 | 8 秒 | 全程全屏（飞机→爆炸→烟花×4） | 36px 加粗 |

**T5 礼物 8 秒序列时间轴**

```
0.0s~1.5s  飞机从左侧飞入，播放引擎音效
1.5s~3.0s  载具停留，投放特效（包裹/炸弹下落动画）
3.0s~5.0s  落地爆炸，全屏白色闪光（0.3s），粒子四散
5.0s~7.0s  烟花序列（多发烟花依次爆炸），资源图标从天而降
7.0s~8.0s  特效淡出，礼物信息文字淡出
8.0s       GiftEffectPanel.SetActive(false)
```

---

### §20.5 倒计时状态切换循环

**状态机（含颜色规格）**

| 状态标识 | 剩余时间 | 颜色 | 动画 |
|---|---|---|---|
| `TIMER_NORMAL` | > 60 秒 | 绿色 `#4CAF50`（白天）/ 蓝色 `#1976D2`（夜晚） | 平滑减少 |
| `TIMER_WARNING` | 30~60 秒 | 黄色 `#FFC107` | 颜色过渡 0.5s，播放一次 sfx_timer_warning |
| `TIMER_DANGER` | < 30 秒 | 红色 `#F44336`，闪烁 | 透明度 1.0↔0.6，周期 0.5s |
| `PHASE_TRANSITION` | 0 秒 | 过渡特效（3 秒） | 全屏标语 + BGM 切换 |

**阶段切换过渡（3 秒）**

```
[白天→夜晚]
0.0s~1.0s  屏幕边缘 Vignette 增强（透明度 0→0.6）
0.5s       全屏文字"夜幕降临！"（#FFD700，64px，缩放 0.8→1.0，0.3s 出现）
1.0s       夜晚 BGM 淡入（2.0s），旧 BGM 同步淡出
2.0s       夜晚 UI 激活（月亮图标，倒计时色重置）
3.0s       进入夜晚阶段

[夜晚→白天]
0.0s~1.0s  Vignette 透明度 0.6→0
0.5s       全屏文字"天光来临！"（#87CEEB，64px）
1.0s       白天 BGM 淡入
3.0s       进入白天阶段，天数 +1
```

---

### §20.6 炉温警告循环

**状态机（含颜色+音效规格）**

| 状态 | 温度范围 | 颜色 | 动画 | 音效 |
|---|---|---|---|---|
| `TEMP_NORMAL` | > 0℃ | 蓝色 `#1976D2` | 静止 | — |
| `TEMP_COLD` | -40~0℃ | 黄色 `#FFC107` | 颜色过渡 0.5s | sfx_cold_warning（一次） |
| `TEMP_DANGER` | -80~-40℃ | 橙色 `#FF5722` | 温度数字缩放跳动，周期 0.5s | sfx_danger_warning（一次） |
| `TEMP_CRITICAL` | < -80℃ | 红色 `#F44336`，闪烁 | 图标透明度 1.0↔0.3，周期 0.4s | sfx_furnace_critical（循环） |

**TEMP_CRITICAL 全屏警告文字**

| 参数 | 值 |
|---|---|
| 文字内容 | "⚠ 炉火将熄！" |
| 颜色 | `#FF1744`，48px 加粗 |
| 位置 | 屏幕上方 1/3，水平居中 |
| 闪烁 | 透明度 1.0↔0，周期 0.6s |
| 出现动画 | 从上方 -30px 滑入，0.3s ease-out |
| 滞后区间 | 进入 < -80℃，解除 > -75℃（防抖动） |

---

### §20.7 城门 HP 循环

**状态机**

| 状态 | HP 比例 | HP 条颜色 | 额外 |
|---|---|---|---|
| `GATE_HEALTHY` | > 50% | 绿色 `#4CAF50` | 城门完整外观 |
| `GATE_DAMAGED` | 30~50% | 黄色 `#FFC107` | 裂缝贴图激活，sfx_gate_crack |
| `GATE_CRITICAL` | < 30% | 红色 `#F44336`，闪烁 | HP 数值闪烁，sfx_gate_alarm 循环，图标 X 轴位移晃动 |
| `GATE_DESTROYED` | HP = 0 | — | 城门破碎动画 → 3 秒 → 游戏失败结算 |

**受击/恢复动画**

- **怪物攻击**：HP 条数值平滑减少（0.3s），HP 条抖动（X -3px→+3px，0.15s×2），屏幕边缘红色闪光（Vignette 0→0.3→0，0.4s），sfx_gate_hit
- **礼物恢复**：HP 条平滑增加（0.5s，ease-out），HP 条短暂变白后恢复颜色（0.2s 闪光）

---

### §20.8 怪物波次循环（夜晚 Combat 循环）

**完整闭环流程**

```
[夜晚阶段开始]
    ▼
[MONSTER_PENDING] 等待 monster_wave 消息
    ▼ 收到 monster_wave
[MONSTER_SPAWNING] MonsterPool.GetFromPool()，定位 spawn_point，播放 spawn 动画（0.5s）
    ▼
[MONSTER_MOVING] walk 动画，沿预设路径移动，HUD 显示"⚠ 怪物来袭！"（2s 后滑出）
    ▼ 到达城门攻击位置
[MONSTER_ATTACKING] attack 动画循环
    │  服务器推送 hp_update → 城门 HP 条更新 + CameraShake + sfx_gate_hit
    │
    │  观众发"打怪/fight/杀"（指令6）→ 工人攻击 → 服务器广播 monster_hp_update
    │      怪物 HP ≤ 0 → [MONSTER_DYING]
    │          死亡动画（1.0s）→ MonsterPool.Return（SetActive(false)）
    │          击杀特效（粒子，0.5s）+ sfx_monster_die
    │
    │  收到 phase_change（夜晚→白天）
    ▼
[MONSTER_RETREAT] 存活怪物转身跑开（0.8s），至屏幕外后归还对象池
```

**波次提示 UI**

| 元素 | 规格 |
|---|---|
| "怪物来袭！"颜色 | `#FF1744`，48px 加粗 |
| 出现动画 | 从上方 -40px 滑入，0.3s ease-out |
| 消失动画 | 向上滑出 +40px，0.3s，延迟 2.0s |
| 波次编号 | "第 X 波！"，28px，`#FFD700` |

---

### §20.9 结算三屏循环

**完整流程与时间轴**

| 阶段 | 时长 | 内容 |
|---|---|---|
| SETTLE_DELAY | 3 秒 | 保持战场画面，播放 sfx_game_over 或 sfx_victory |
| SETTLE_SCREEN_A | 3 秒 | 存活天数（数字弹跳出现）+ 失败原因文字 |
| SETTLE_SCREEN_B | 5 秒 | 数据统计（弹幕人数/礼物价值/各资源采集次数/击杀数），逐行淡入（间隔 0.2s/行） |
| SETTLE_SCREEN_C | 3 秒 | MVP Top3（🥇 第一名弹性缩放 0.5→1.2→1.0） |
| SETTLE_LOADING | ~1~2 秒 | 发送 reset_game，Loading 面板，10s 超时保护 |

**结算 UI 颜色规格**

| 元素 | 胜利 | 失败 |
|---|---|---|
| 主标题颜色 | `#FFD700` | `#F44336` |
| 存活天数数字 | `#76FF03`，72px | `#FFFFFF`，72px |
| 面板背景 | `#0D1B2A`，透明度 0.92 | `#1A0000`，透明度 0.92 |

---

### §20.10 排行榜面板循环

**完整流程**

```
[LOBBY_IDLE] → 点击"排行榜"按钮
    ▼
[RANK_ENTERING] RankingPanel.SetActive(true)
    入场动画：X +800px → 0，时长 0.3s，ease-out cubic
    ▼
[RANK_VISIBLE]
    有数据 → 显示上局 Top10 | 无数据 → 显示"暂无本场数据"（#9E9E9E）
    ▼ 点击 ✕
[RANK_LEAVING] X 0 → +800px，时长 0.3s，ease-in cubic
    动画结束回调：RankingPanel.SetActive(false)
    ▼
[LOBBY_IDLE]
```

**排名颜色规格**

| 排名 | 名称颜色 | 行背景 |
|---|---|---|
| 第 1 名 | `#FFD700` 金色 | `#FFD700` 透明度 0.15 |
| 第 2 名 | `#C0C0C0` 银色 | `#C0C0C0` 透明度 0.10 |
| 第 3 名 | `#CD7F32` 铜色 | `#CD7F32` 透明度 0.10 |
| 第 4~10 名 | `#FFFFFF` | 透明 / 交替 `#FFFFFF` 透明度 0.03 |

> ⚠️ 遵守 AI开发准则第4条：排行榜坐标不可由代码覆盖，仅通过 `_initialOffset` 模式做动画偏移。

---

### §20.11 设置面板循环

**完整流程**

```
[LOBBY_IDLE] → 点击"设置"按钮
    ▼
[SETTINGS_ENTERING] SettingsPanel.SetActive(true)
    出现动画：缩放 0.8→1.0 + 透明度 0→1，时长 0.25s，ease-out
    背景遮罩：透明度 0→0.5（同步）
    ▼
[SETTINGS_VISIBLE]
    BGM 滑条 → AudioManager.SetBGMVolume(value)，立即执行 + PlayerPrefs 写入
    SFX 滑条 → AudioManager.SetSFXVolume(value)，立即执行 + PlayerPrefs 写入
    ▼ 点击 ✕ / 点击背景遮罩
[SETTINGS_LEAVING] 缩放 1.0→0.8 + 透明度 1→0，时长 0.2s，ease-in
    动画结束回调：SettingsPanel.SetActive(false)
    ▼
[LOBBY_IDLE]
```

**PlayerPrefs 键名规范**

| 设置项 | 键名 | 默认值 |
|---|---|---|
| BGM 音量 | `BGMVolume` | `0.75` |
| SFX 音量 | `SFXVolume` | `1.0` |

---

### §20.A 交互循环消息索引

| 消息类型 | 方向 | 触发循环 |
|---|---|---|
| `start_game` | Client → Server | §20.1 |
| `survival_game_state` | Server → Client | §20.1 |
| `reset_game` | Client → Server | §20.2 / §20.9 |
| `work_command` | Server → Client | §20.3 |
| `resource_update` | Server → Client | §20.3 / §20.6 |
| `gift_received` | Server → Client | §20.4 |
| `phase_change` | Server → Client | §20.5 / §20.3 / §20.8 |
| `hp_update` | Server → Client | §20.7 / §20.8 |
| `monster_wave` | Server → Client | §20.8 |
| `monster_died` | Server → Client | §20.8 |
| `game_ended` | Server → Client | §20.9 |

---

## §21 技术美术规格（Tech Art Specifications）

> 版本：v3.0 | 引擎：Unity 2022.3 + URP | 更新：2026-02-25

---

### §21.1 色彩与美术风格规范

**主色调体系（冬日冷调）**

| 角色 | 颜色名 | HEX | 用途 |
|------|--------|-----|------|
| 主色 | 冰川蓝 | `#4A90C4` | UI主按钮、选中高亮 |
| 主色深 | 深海蓝 | `#2B6CB0` | 标题背景、进度条底色 |
| 辅助色1 | 积雪白 | `#EEF4F9` | 地面、屋顶雪层、UI底板 |
| 辅助色2 | 云杉绿 | `#3D7A5C` | 常绿树、植被点缀 |
| 辅助色3 | 桦木棕 | `#8B6240` | 木制建筑、栅栏 |
| 强调色1 | 炉火橙 | `#FF6B2B` | 炉灶火光、高优先度提示 |
| 强调色2 | 暖琥珀 | `#FFB347` | 金币、T1礼物特效、正向反馈 |
| 强调色3 | 危机红 | `#FF2D55` | 城门血条危险、暴击数字 |
| 强调色4 | 怪物紫 | `#7B2FBE` | 夜晚怪物轮廓、T4礼物特效 |
| 中性色 | 石板灰 | `#4A5568` | 城墙石块、UI分隔线 |

**白天 vs 夜晚色彩对比**

| 参数 | 白天 | 夜晚 |
|------|------|------|
| 天空主色 | `#A8C8E8`（浅蓝冬日天空） | `#0D1B2A`（深靛蓝夜空） |
| 场景色温 | 5500K 暖白，积雪反射偏蓝 | 3200K 冷蓝，月光主导 |
| 雪地固有色 | `#E8F0F5`（微蓝白） | `#B8C8D8`（蓝灰） |
| 雾效颜色 | `#C8DCE8`（浅蓝雾） | `#0A1520`（近黑深蓝雾） |
| 整体亮度偏移 | 基准（Exposure +0） | 降低（Exposure -1.5 EV） |
| Fog 密度 | 0.008 | 0.015（夜雾更浓） |

**过渡规则**：白天→夜晚全程 3 秒 Lerp，使用 `Mathf.SmoothStep` 曲线，避免线性突变。

**UI 颜色变量表（CSS Variable 风格）**

```css
--color-primary:        #4A90C4;   /* 主色，按钮、链接 */
--color-primary-dark:   #2B6CB0;   /* 主色深，按钮按下态 */
--color-bg-panel:       #1A2A3A;   /* 面板底色 */
--color-bg-overlay:     #0D1B2A;   /* 全屏遮罩底色 */
--color-text-primary:   #F0F6FF;   /* 主要文字（亮白偏蓝） */
--color-text-secondary: #A0B8CC;   /* 次要文字（灰蓝） */
--color-success:        #48BB78;   /* 成功/正向（翠绿） */
--color-warning:        #FFB347;   /* 警告（琥珀橙） */
--color-danger:         #FF2D55;   /* 危险/错误（危机红） */
--color-fire-orange:    #FF6B2B;   /* 炉火/强调 */
--color-monster-purple: #7B2FBE;   /* 怪物主色 */
--color-gold-reward:    #FFB800;   /* 金币/奖励 */
--color-health-full:    #48BB78;   /* 血条满值 */
--color-health-low:     #FF2D55;   /* 血条危险值（<30%） */
```

---

### §21.2 材质规格（Unity URP Shader）

**优先级说明**：P0=必须首批实现，P1=重要，P2=增强效果

**雪地地面（P0）**

| Inspector 参数 | 值 |
|---|---|
| Shader | URP Lit |
| Base Map | `snow_ground_albedo.png`，冷白 `#E8F0F5` |
| Normal Map | `snow_ground_normal.png`，强度 0.6 |
| Smoothness | 0.1 |
| Specular Color | `#C8DCE8`（冷蓝白，模拟天空光反射偏色） |
| Tiling | X: 4, Y: 4 |

**积雪建筑（P0）** — URP Lit + SnowAccumulation CustomPass（顶点色R通道控制积雪遮罩，1=完全覆雪，0=无雪；CustomPass Snow Threshold: 0.65）

**炉灶/火堆（P0）** — 炉体：URP Lit + Emission（`#FF6B2B`，HDR 1.2）+ Emission Map 仅炉口区域发光；火焰面片：Unlit + 4×4 序列帧贴图（24fps）

```csharp
// 炉温 → Emission 联动
float tempNormalized = Mathf.InverseLerp(-100f, 100f, currentTemp);
float emissionIntensity = Mathf.Lerp(0f, 2.5f, tempNormalized);
material.SetColor("_EmissionColor",
    Color.Lerp(Color.black, new Color(1f, 0.42f, 0.17f), tempNormalized) * emissionIntensity);
if (currentTemp < -80f) { material.SetColor("_EmissionColor", Color.black); fireParticleSystem.Stop(); }
```

**城门（P0）** — URP Lit（金属+木材混合）+ CrackedOverlay CustomPass，Alpha 随 HP 百分比 0→1（满血→濒死），HP<10% 时裂纹闪烁

**工人角色（P0）** — CartoonCharacter Shader（Flat Shading，Shade Threshold 0.4，Rim Light `#A8D8F0` 0.3）+ Inverted Hull Outline Pass（宽度 2px，颜色 `#1A1A2E`）

**冰晶/T4礼物（P1）** — 自定义 IceCrystal Shader（Transparent，SSS 近似：Back-Lit `dot(lightDir, -viewDir)` Pow × `#78C8F0` × Thickness 遮罩；Smoothness 0.95，Fresnel Scale 1.5）

---

### §21.3 光照规范

**白天主光（P0）**

| Inspector 参数 | 值 |
|---|---|
| Rotation | X: 42°，Y: -30°（冬日低太阳角） |
| Color | `#FFF5E0`（暖白 5500K） |
| Intensity | 1.1 |
| Shadow Type | Soft Shadows，Strength 0.7 |
| Cascade Count | 4，Distances 8m/25m/60m/150m |
| Ambient Sky | `#A8C8E8`，Ambient Intensity 0.4 |
| Fog | Exponential Squared，密度 0.008，颜色 `#C8DCE8` |

**夜晚月光（P0）**

| Inspector 参数 | 值 |
|---|---|
| Rotation | X: 55°，Y: 160° |
| Color | `#8AA8C8`（冷蓝 3200K） |
| Intensity | 0.35 |
| Ambient Sky | `#0D1B2A`，Ambient Intensity 0.2 |
| Fog 密度 | 0.015 |

**炉火 Point Light（夜晚）**

| 参数 | 值 |
|---|---|
| Color | `#FF6B2B` |
| Intensity | 3.0（与炉温 Lerp：0~3.0 对应 -100~+100℃） |
| Range | 8.0m（与炉温 Lerp：0~8m） |
| 闪烁 | ±0.3 幅度，8Hz 随机 Perlin Noise |

**日夜过渡动画（P1）**

```csharp
IEnumerator TransitionToNight(float duration = 3f) {
    float elapsed = 0f;
    // ... 快照初始值 ...
    while (elapsed < duration) {
        elapsed += Time.deltaTime;
        float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
        RenderSettings.ambientSkyColor = Color.Lerp(startSkyColor, new Color(0.051f,0.106f,0.165f), t);
        directionalLight.intensity = Mathf.Lerp(1.1f, 0.35f, t);
        RenderSettings.fogDensity = Mathf.Lerp(0.008f, 0.015f, t);
        if (elapsed > 0.5f) moonLight.intensity = Mathf.Lerp(0f, 0.35f, (elapsed-0.5f)/(duration-0.5f));
        yield return null;
    }
}
```

---

### §21.4 粒子系统规格

**雪花粒子（P0，常驻）**

| 参数 | 值 |
|---|---|
| Max Particles | 300（夜晚 Rate ×1.5） |
| Start Size | 0.02~0.08m（World Space） |
| Velocity Y | -0.8 m/s（下落） + X -0.3~+0.3 m/s（飘动） |
| Shape | Box 24m×0.2m×15m，Y+10m 顶部生成 |
| 夜晚颜色 | `#C8E0FF`（蓝色调） |

**炉火粒子（P0，3 子系统）**

- **火苗**：Max 40，Lifetime 0.6~1.0s，颜色 `#FFD166`→`#FF4500`→透明，Shape Cone 15°
- **烟尘**：Max 20，Lifetime 2.0~3.5s，Start Color `#4A4A4A` Alpha 0.4，Size 1.0→2.5（扩散）
- **火星子**：Max 8，每 2~5 秒 Burst 3~6 颗，Color `#FFB800`，拖尾 + Gravity 0.4（上升后回落）

**礼物特效粒子（分级）**

| 等级 | 粒子数 | 时长 | 特征 |
|---|---|---|---|
| T1 | 30 | 0.8s | 金色五角星散射，拖尾 Trail |
| T2 | 50 | 1.5s | 彩色泡泡上浮（折射材质） |
| T3 | 100 | 2.0s | 礼炮纸屑 Burst，速度 3~8 m/s，全屏旋转 |
| T4 | 60 | 3.0s | 蓝紫电弧 Trail + 闪白（Bloom 1.0→8.0，0.1s）+ CameraShake |
| T5 | 序列 | 8.0s | 飞机→爆炸云→烟花×4，CameraShake 振幅 0.3 |

**怪物死亡粒子（P1）**

- Mesh 预分割 12~20 碎片，死亡时解除父子关系 + Rigidbody 随机爆炸冲力（2~5N）
- 碎片 Scale 1→0 + Alpha 1→0，持续 0.5s
- 击杀文字"击倒！"：`#FFB800` 金色，Y+1.5m 上飘 1.0s，Scale 0.3→1.2→1.0，出现后 0.7s 淡出

---

### §21.5 打击感（Hit Feel）规范

**屏幕震动 CameraShake（P0）**

```csharp
// CameraRig 子节点操作（不修改 Camera 本身）
public IEnumerator Shake(float amplitude, float frequency, float duration) {
    float elapsed = 0f; float seed = Random.Range(0f, 100f);
    while (elapsed < duration) {
        elapsed += Time.unscaledDeltaTime; // 不受 Hit Stop timeScale=0 影响
        float dampedAmp = amplitude * (1f - elapsed / duration);
        float offsetX = (Mathf.PerlinNoise(seed + elapsed * frequency, 0f) - 0.5f) * 2f * dampedAmp;
        float offsetY = (Mathf.PerlinNoise(0f, seed + elapsed * frequency) - 0.5f) * 2f * dampedAmp;
        cameraRig.localPosition = new Vector3(offsetX, offsetY, 0f);
        yield return null;
    }
    cameraRig.localPosition = Vector3.zero;
}
```

| 触发事件 | 震幅 | 频率 | 时长 |
|---|---|---|---|
| 怪物攻击城门 | 0.15 | 20 Hz | 0.3s |
| T5礼物爆炸 | 0.3 | 15 Hz | 0.5s |
| T4礼物电弧 | 0.15 | 20 Hz | 0.3s |
| 城门破碎（HP=0） | 0.4 | 12 Hz | 0.8s |

**Hit Stop 暂停帧（P1）**

```csharp
public IEnumerator DoHitStop(int frames) {
    Time.timeScale = 0f;
    yield return new WaitForSecondsRealtime(frames / 60f);
    Time.timeScale = 1f;
}
// 粒子使用 useUnscaledTime = true，Hit Stop 期间不停止
```

| 触发事件 | 暂停帧数 |
|---|---|
| 怪物普通受击 | 2帧 |
| 怪物死亡 | 4帧 |
| T4/T5礼物触发 | 3帧 |

**伤害数字弹出（P1）**：Float Text 预创建 20 个对象池，Y+1.2m 上飘 1.0s；Scale 0.3→1.5→1.0（弹性，0.15s 出现）；颜色 `#FFFFFF`，暴击 `#FF2D55` 字号×1.5；0.7s 后 Alpha→0

**受击材质闪白（P1）**：使用 `MaterialPropertyBlock` 覆写 `_EmissionColor` 为 `#FFFFFF` HDR 4.0，持续 0.1s 后 Lerp 恢复，不修改原始 Material。

---

### §21.6 角色动画状态机

**Worker 动画状态机（P0，5 状态）**

> ⚠️ **T010 实际实现说明（2026-02-25）**：
> CowWorker.prefab 使用 `AC_Kpbl.controller`（与 Capybara 角色共用），**现有参数仅有 Speed/IsPushing/IsDead**，无专属 Worker 动画剪辑。
> WorkerController.cs（T010）已复用这三个参数实现基础绑定：
> - Move 状态 → Speed = 3（MOVE_SPEED），IsPushing = false
> - Work 状态 → Speed = 0，IsPushing = true
> - Idle 状态 → Speed = 0，IsPushing = false
>
> **T011~T014（5种工作动画）标记为 Pending-Art**，需美术提供 .anim 剪辑后创建 `AC_Worker.controller`。
> 届时补充 WorkType Int 参数（0=挖掘/1=拾取/2=添火/3=建造）区分工种。

**Worker 动画参数（目标规格，待美术资源就绪后实现）**

| 参数名 | 类型 | 说明 | 当前状态 |
|---|---|---|---|
| `Speed` | Float | 0~3 m/s，控制行走步频（Blend Tree） | ✅ 已绑定 |
| `IsPushing` | Bool | 工作状态（复用 AC_Kpbl 参数） | ✅ 已绑定 |
| `IsWalking` | Bool | 行走中（目标规格，待新 Controller） | ⏳ Pending-Art |
| `IsWorking` | Bool | 工作动画中（目标规格） | ⏳ Pending-Art |
| `IsFighting` | Bool | 战斗/攻击状态（目标规格） | ⏳ Pending-Art |
| `WorkType` | Int | 0=挖掘，1=拾取，2=添火，3=建造 | ⏳ Pending-Art |

```
[Idle] ─ IsWalking=true → [Walk]
  [Walk] ─ IsWorking=true → [Work 循环]
         ─ IsFighting=true → [Attack 循环]
  [Work] ─ IsWorking=false → [Walk 返回] → [Idle]
  [Attack] ─ IsFighting=false → [Idle]
```

**Transition 规则**：Has Exit Time = Off，Transition Duration = 0.2s（Walk↔Work 为 0.15s）

**怪物动画状态机（P0，3+1 状态）**

```
[Spawn（不循环）] → [March（Loop）]
  [March] ─ IsAttacking=true → [Attack（Loop）]
  [Attack/March] ─ IsDead 触发 → [Die（不循环）] → SetActive(false)
```

**Spawn Scale 动画曲线**（EaseOutBack 弹性）：0.00s=0.0，0.35s=1.15（超射），0.45s=0.95，0.50s=1.00

---

### §21.7 后处理效果（Post-Processing）

> 全部通过 Unity URP Volume System 实现

**Bloom（P1）**

| 参数 | 白天值 | 夜晚值 |
|---|---|---|
| Threshold | 0.9 | 0.75 |
| Intensity | 0.5 | 1.2 |
| Tint | `#FFFFFF` | `#A8C8FF`（夜晚蓝调） |

> 发光物件单独放 `FX_Bloom` Layer，避免场景过曝

**Color Grading LUT（P1）**

| 参数 | 白天 | 夜晚 |
|---|---|---|
| LUT 文件 | `lut_daytime_winter.png` | `lut_night_winter.png` |
| Post Exposure | +0 EV | -1.5 EV |
| Saturation | -8% | -20% |
| Shadows | `#8AA8BE`（蓝灰影调） | `#0A1830`（深蓝影调） |

**Vignette（P1，程序驱动）**

```csharp
void UpdateVignetteFromGateHP(float hpPercent) {
    Vignette vignette; volume.profile.TryGet(out vignette);
    if (hpPercent > 0.3f) { vignette.color.value = Color.black; vignette.intensity.value = 0.2f; }
    else if (hpPercent > 0.1f) { vignette.color.value = Color.red; vignette.intensity.value = 0.5f; }
    else { float pulse = 0.45f + 0.05f * Mathf.Sin(Time.time * Mathf.PI * 2f);
           vignette.color.value = Color.red; vignette.intensity.value = pulse; }
}
```

**Depth of Field（P2）** — 仅大厅/结算激活，Bokeh Mode，Focus Distance 6m，Aperture f/2.8

**SSAO（禁用）** — Low Poly 风格不使用 SSAO，改用顶点色 AO 烘焙（建筑底部、树木接地处手动刷暗）

**Volume 优先级层级**

| Volume 名称 | Priority | 管控内容 |
|---|---|---|
| Global_Base | 0 | 基础 Color Grading，默认 Vignette |
| Override_Night | 10 | 夜晚 LUT，夜晚 Bloom |
| Override_Danger | 20 | 城门危险 Vignette |
| Override_UI_DOF | 30 | 大厅/结算景深 |
| Override_GiftFX | 40 | 礼物特效 Bloom 峰值（瞬间叠加后恢复） |

---

## §22 完整开发计划（Comprehensive Development Plan）

> 版本：v3.0 | 最后更新：2026-02-25 | **总任务数：242 个，总估时：~938h**

---

### §22.1 里程碑时间线

| 里程碑 | 名称 | 核心交付物 | 状态 | 预计工时 |
|--------|------|-----------|------|---------|
| M1 | 服务器+连接基础 | NetworkManager / SurvivalGameEngine.js / 抖音Webhook | ✅ 已完成 | 40h |
| M2 | 大厅+Loading+状态机 | SurvivalGameManager / IdleUI / LoadingUI | ✅ 已完成 | 32h |
| M3 | 排行榜+结算+设置 | RankingUI / SettlementUI / SettingsUI / RankingSystem | ✅ 已完成 | 28h |
| M4 | Worker完整动画系统 | 移动插值 / 工位分配 / 5种工作动画 / 气泡/进度圈 | ⏳ 待开始 | 56h |
| M5 | 礼物特效系统 | T1-T5特效 / 排队系统 / HUD同步 / 音效绑定 | ⏳ 待开始 | 48h |
| M6 | 怪物系统 | 对象池 / 行走攻击动画 / 多类型怪物 / 波次系统 | ⏳ 待开始 | 64h |
| M7 | 技术美术v1 | 雪地材质 / 炉火粒子 / 日夜LUT / 后处理 | ⏳ 待开始 | 72h |
| M8 | 音效系统 | AudioManager / BGM / SFX全绑定 / Volume控制 | ⏳ 待开始 | 32h |
| M9 | UI动画+打击感 | 面板动画 / 数字跳动 / CameraShake / HitStop | ⏳ 待开始 | 40h |
| M10 | 主播功能+测试发布 | 主播面板 / QA / 压测 / 打包上线 | ⏳ 待开始 | 48h |

**关键路径**：Worker(M4) → 礼物(M5) → 怪物(M6) → 技术美术(M7) → 音效(M8) → UI动画(M9) → 主播/QA/发布(M10)

---

### §22.2 任务总表（模块 A：Worker 系统）

| 任务ID | 任务名称 | 优先级 | 依赖 | 估时 | 状态 |
|--------|---------|--------|------|------|------|
| T001 | WorkerVisual脚本基础框架 | P0 | — | 4h | ✅ |
| T002 | 20个Worker预实例化场景布局 | P0 | T001 | 3h | ✅ |
| T003 | Worker移动目标坐标注册表 | P0 | T002 | 4h | ⏳ |
| T004 | Worker移动插值（DOTween Lerp） | P0 | T003 | 5h | ⏳ |
| T005 | Worker到达工位停止逻辑 | P0 | T004 | 3h | ⏳ |
| T006 | Worker工位数据结构定义 | P0 | — | 3h | ⏳ |
| T007 | 工位空闲状态管理（占用/释放） | P0 | T006 | 4h | ⏳ |
| T008 | 就近空闲Worker分配算法 | P0 | T006 T007 | 6h | ⏳ |
| T009 | 工位满员时拒绝分配逻辑 | P0 | T008 | 3h | ⏳ |
| T010 | 采食工作动画绑定 | P0 | T005 | 5h | ⏳ |
| T011 | 采煤工作动画绑定 | P0 | T005 | 5h | ⏳ |
| T012 | 采矿工作动画绑定 | P0 | T005 | 5h | ⏳ |
| T013 | 添柴工作动画绑定 | P0 | T005 | 5h | ⏳ |
| T014 | 攻击怪物动画绑定 | P0 | T005 | 5h | ⏳ |
| T015 | Idle待机动画（随机眨眼/抖耳） | P1 | T001 | 4h | ⏳ |
| T016 | Worker头顶气泡UI预制体 | P1 | T001 | 4h | ⏳ |
| T017 | 气泡Emoji绑定（5种工作类型） | P1 | T016 | 3h | ⏳ |
| T018 | 气泡淡入淡出动画 | P1 | T016 | 3h | ⏳ |
| T019 | 工作效率进度圈预制体 | P1 | T001 | 4h | ⏳ |
| T020 | 进度圈随产出周期转动 | P1 | T019 T010 | 4h | ⏳ |
| T021 | 进度圈完成时小爆炸粒子 | P2 | T020 | 3h | ⏳ |
| T022 | 工人碰撞路径避让算法 | P1 | T004 | 6h | ⏳ |
| T023 | 工人外观多样化（4种颜色Sprite） | P2 | T001 | 4h | ⏳ |
| T024 | 工人帽子/配件随机分配 | P2 | T023 | 3h | ⏳ |
| T025 | 工人数量上限随进度扩展逻辑 | P1 | T008 | 4h | ⏳ |
| T026 | 新增工人入场动画 | P2 | T025 | 3h | ⏳ |
| T027 | 工位满员气泡视觉提示（❌符号） | P1 | T009 T016 | 2h | ⏳ |
| T028 | Worker受怪物攻击受击动画 | P1 | T014 | 4h | ⏳ |
| T029 | Worker死亡/下场动画+复活计时 | P1 | T028 | 5h | ⏳ |
| T030 | Worker绑定玩家名显示 | P1 | T016 | 4h | ⏳ |
| T211 | Worker弹幕指令解析接入 | P0 | T008 | 4h | ⏳ |
| T212 | Worker工作产出定时器逻辑 | P0 | T010 | 4h | ⏳ |
| T213 | Worker产出资源HUD实时推送 | P0 | T212 | 3h | ⏳ |
| T214 | Worker被指派时高亮闪烁效果 | P2 | T004 | 3h | ⏳ |
| T215 | Worker状态枚举定义 | P0 | T001 | 2h | ⏳ |

---

### §22.3 任务总表（模块 B：礼物特效系统）

| 任务ID | 任务名称 | 优先级 | 依赖 | 估时 | 状态 |
|--------|---------|--------|------|------|------|
| T031 | GiftEffectManager单例脚本 | P0 | — | 4h | ⏳ |
| T032 | 礼物等级枚举与配置表（T1-T5） | P0 | T031 | 3h | ⏳ |
| T033 | 礼物特效排队队列系统 | P0 | T031 | 6h | ⏳ |
| T034 | T1礼物特效面板预制体 | P0 | T032 | 4h | ⏳ |
| T035 | T1礼物粒子效果（金色星星散射） | P0 | T034 | 4h | ⏳ |
| T036 | T2礼物特效面板+动画 | P0 | T032 | 5h | ⏳ |
| T037 | T2礼物粒子（彩色泡泡） | P0 | T036 | 4h | ⏳ |
| T038 | T3礼物全屏遮幕+粒子特效 | P0 | T032 | 6h | ⏳ |
| T039 | T3礼物序列帧动画播放器 | P0 | T038 | 5h | ⏳ |
| T040 | T4礼物电能特效（闪电粒子） | P0 | T032 | 6h | ⏳ |
| T041 | T4礼物屏幕边缘电光Shader | P1 | T040 | 5h | ⏳ |
| T042 | T5礼物全屏占用标志位 | P0 | T033 | 3h | ⏳ |
| T043 | T5礼物烟花发射粒子序列（8秒） | P0 | T042 | 8h | ⏳ |
| T044 | T5礼物播放时HUD隐藏/恢复逻辑 | P0 | T042 | 3h | ⏳ |
| T045 | 礼物送礼者名气泡弹出动画 | P1 | T034 | 4h | ⏳ |
| T046 | 多人连送时气泡堆叠显示 | P1 | T045 | 4h | ⏳ |
| T047 | 礼物资源量同步更新HUD | P0 | T031 | 3h | ⏳ |
| T048 | 礼物特效播放完毕回调事件 | P0 | T033 | 3h | ⏳ |
| T049 | 礼物特效对象池（GameObject复用） | P1 | T034 | 5h | ⏳ |
| T050 | 礼物特效与音效绑定接口 | P0 | T031 | 2h | ⏳ |
| T051 | 礼物连刷计数器（x5/x10显示） | P2 | T033 | 4h | ⏳ |
| T052 | 礼物触发资源+数值立即反馈 | P0 | T047 | 3h | ⏳ |
| T216 | T1-T5礼物对应资源量配置表 | P0 | T032 | 2h | ⏳ |
| T217 | 礼物特效播放超时强制结束保护 | P0 | T033 | 3h | ⏳ |
| T218 | 礼物特效屏幕适配（16:9/9:16） | P1 | T038 | 4h | ⏳ |

---

### §22.4 任务总表（模块 C：怪物系统）

| 任务ID | 任务名称 | 优先级 | 依赖 | 估时 | 状态 |
|--------|---------|--------|------|------|------|
| T053 | MonsterManager单例脚本 | P0 | — | 4h | ⏳ |
| T054 | 怪物GameObject对象池 | P0 | T053 | 6h | ⏳ |
| T055 | 怪物数据结构（HP/速度/伤害/类型） | P0 | — | 3h | ⏳ |
| T056 | 怪物生成点坐标配置（多路入侵） | P0 | T053 | 3h | ⏳ |
| T057 | 普通怪物行走动画 | P0 | T054 | 5h | ⏳ |
| T058 | 精英怪物行走动画（特殊外观） | P1 | T054 | 5h | ⏳ |
| T059 | Boss怪物行走动画+体型缩放 | P1 | T054 | 6h | ⏳ |
| T060 | 怪物攻击城门动画 | P0 | T057 | 5h | ⏳ |
| T061 | 怪物受击动画（击退效果） | P0 | T057 | 4h | ⏳ |
| T062 | 怪物受击材质闪白 | P0 | T061 | 4h | ⏳ |
| T063 | 怪物死亡动画+碎片粒子 | P0 | T061 | 6h | ⏳ |
| T064 | 怪物HP数字飘出（FloatingText） | P0 | T061 | 4h | ⏳ |
| T065 | 怪物头顶HP血条UI | P1 | T055 | 4h | ⏳ |
| T066 | 城门HP条同步显示 | P0 | — | 3h | ⏳ |
| T067 | 城门受击CameraShake触发 | P0 | T060 | 3h | ⏳ |
| T068 | 城门受损裂缝Overlay分级显示 | P1 | T066 | 5h | ⏳ |
| T069 | 城门破碎终态动画（游戏失败） | P0 | T068 | 5h | ⏳ |
| T070 | 怪物波次配置表（服务器下发） | P0 | T053 | 4h | ⏳ |
| T071 | 波次开始全屏提示UI（第X波） | P0 | T070 | 3h | ⏳ |
| T072 | 波次清空时奖励反馈动画 | P1 | T070 | 3h | ⏳ |
| T073 | 怪物路径寻路（NavMesh/预设路径） | P0 | T056 | 6h | ⏳ |
| T074 | 怪物被Worker攻击死亡联动 | P0 | T014 T063 | 5h | ⏳ |
| T075 | 精英怪物特殊技能（减速/AOE） | P2 | T058 | 6h | ⏳ |
| T076 | Boss怪物特殊入场演出 | P2 | T059 | 5h | ⏳ |
| T077 | 夜晚光照自动切换触发 | P0 | T070 | 3h | ⏳ |
| T078 | 怪物数量上限与性能保护 | P1 | T054 | 4h | ⏳ |
| T079 | 怪物死亡掉落资源粒子 | P2 | T063 | 4h | ⏳ |
| T219 | 怪物生成数量与波次指数增长配置 | P0 | T070 | 3h | ⏳ |
| T220 | 怪物移动速度随波次动态调整 | P1 | T073 | 3h | ⏳ |
| T221 | 城门HP同步到服务器（权威数值） | P0 | T066 | 4h | ⏳ |

---

### §22.5 任务总表（模块 D：音效系统）

| 任务ID | 任务名称 | 优先级 | 依赖 | 估时 | 状态 |
|--------|---------|--------|------|------|------|
| T080 | AudioManager单例脚本 | P0 | — | 5h | ⏳ |
| T081 | BGM播放器（循环/淡入淡出） | P0 | T080 | 4h | ⏳ |
| T082 | SFX播放器（单次/叠加） | P0 | T080 | 3h | ⏳ |
| T083 | Addressables音频包加载 | P0 | T080 | 4h | ⏳ |
| T084~T086 | 白天/夜晚BGM配置+切换 | P0 | T081 | 7h | ⏳ |
| T087~T091 | 5种指令SFX绑定（食物/煤/矿/柴/攻击） | P0 | T082 | 10h | ⏳ |
| T092~T096 | T1~T5礼物SFX绑定（含T5 8秒序列） | P0 | T082 T050 | 11h | ⏳ |
| T097~T101 | 炉温警报/城门受击/怪物死亡/攻击/日夜号角 | P0 | T082 | 10h | ⏳ |
| T102~T103 | 主播加速按钮SFX/UI通用按钮SFX | P1 | T082 | 4h | ⏳ |
| T104~T105 | 胜利/失败结算BGM配置 | P0 | T081 | 4h | ⏳ |
| T106~T108 | Volume控制接入SettingsUI+BGM/SFX分组+持久化 | P0/P1 | T080 | 8h | ⏳ |
| T222~T223 | 音效测试场景/Addressables打包归组 | P1/P2 | T080 | 6h | ⏳ |

---

### §22.6 任务总表（模块 E：技术美术）

| 任务ID | 任务名称 | 优先级 | 估时 | 状态 |
|--------|---------|--------|------|------|
| T109 | 雪地地面材质（URP Lit+法线贴图） | P0 | 6h | ⏳ |
| T110 | 积雪建筑材质（顶部积雪效果） | P0 | 5h | ⏳ |
| T111~T112 | 炉灶Emission材质+炉温数值绑定脚本 | P0 | 10h | ⏳ |
| T113 | 工人卡通轮廓线Shader | P1 | 6h | ⏳ |
| T114 | 怪物受击材质闪白Shader | P0 | 5h | ⏳ |
| T115 | 城门裂缝Overlay Shader（分级透明度） | P1 | 6h | ⏳ |
| T116 | 雪花粒子系统（全局飘雪） | P0 | 5h | ⏳ |
| T117~T118 | 炉火粒子（火苗+烟雾+火星）+炉温联动脚本 | P0 | 9h | ⏳ |
| T119 | 脚步雪地踩踏粒子 | P2 | 4h | ⏳ |
| T120~T122 | 白天/夜晚LUT+过渡动画脚本 | P1 | 12h | ⏳ |
| T123~T125 | 日夜Directional Light Lerp+月光+炉火Spot Light | P0/P1 | 10h | ⏳ |
| T126~T129 | Bloom配置+礼物联动+Vignette危险+颜色变化 | P1/P2 | 14h | ⏳ |
| T130~T133 | CameraShake/HitStop工具类+FloatingText系统+颜色区分 | P0/P1 | 15h | ⏳ |
| T134~T135 | T5礼物烟花序列特效资产+波次清场庆祝粒子 | P0/P1 | 12h | ⏳ |
| T136~T141 | LOD/遮挡剔除/光照贴图/阴影质量/天空盒+切换 | P1/P2 | 23h | ⏳ |
| T224~T226 | 反射探针/GPU Instancing/摄像机FOV配置 | P1/P2 | 7h | ⏳ |

---

### §22.7 任务总表（模块 F：UI/UX 优化）

| 任务ID | 任务名称 | 优先级 | 估时 | 状态 |
|--------|---------|--------|------|------|
| T142~T145 | 大厅/结算/排行榜/设置面板入场动画 | P1/P2 | 11h | ⏳ |
| T146~T147 | 倒计时颜色渐变+最后10秒脉冲缩放 | P0/P1 | 6h | ⏳ |
| T148~T150 | 炉温数字跳动/颜色渐变条/城门HP振动警告 | P0/P1 | 9h | ⏳ |
| T151~T152 | 阶段切换全屏标语动画（夜幕降临/天光来临） | P0 | 6h | ⏳ |
| T153~T155 | 弹幕指令Toast（无效/成功）+连击数字特效 | P1/P2 | 9h | ⏳ |
| T156~T158 | 资源量滚动动画+结算MVP图标+排行逐行飞入 | P1 | 11h | ⏳ |
| T159~T161 | 启动Logo过渡+游戏结束黑屏+T5礼物HUD渐隐 | P0/P1 | 10h | ⏳ |
| T162~T165 | 满员气泡抖动/低温HUD红框/波次数字弹跳/主播面板滑入 | P0/P1 | 10h | ⏳ |
| T166 | 在线人数实时显示组件 | P1 | 3h | ⏳ |
| T227~T230 | 弹幕名字滚动/安全区域适配/Loading进度绑定/结算分享 | P1/P2/P3 | 15h | ⏳ |

---

### §22.8 任务总表（模块 G：服务器/后端）

| 任务ID | 任务名称 | 优先级 | 估时 | 状态 |
|--------|---------|--------|------|------|
| T167~T170 | WebSocket连接/GameEngine/Webhook/PM2部署 | P0 | — | ✅ 已完成 |
| T171 | Winston日志系统接入 | P0 | 4h | ⏳ |
| T172~T173 | 礼物gift_id白名单配置表+防刷验证 | P0 | 7h | ⏳ |
| T174 | 高频弹幕去重+节流处理 | P0 | 4h | ⏳ |
| T175~T176 | 断线重连逻辑（指数退避）+重连状态同步 | P0 | 9h | ⏳ |
| T177~T178 | 抖音Token自动刷新+房间超时自动清理 | P0/P1 | 8h | ⏳ |
| T179~T182 | 健康接口/PM2监控/压测脚本/用户CD限流 | P1 | 14h | ⏳ |
| T183~T184 | 游戏结果持久化+运营数据统计接口 | P2 | 9h | ⏳ |
| T231~T233 | .env规范/CORS配置/WebSocket心跳 | P0 | 7h | ⏳ |

---

### §22.9 任务总表（模块 H-J：主播功能/QA/发布）

**模块 H：主播功能**

| 任务ID | 任务名称 | 优先级 | 估时 | 状态 |
|--------|---------|--------|------|------|
| T185~T186 | 主播面板UI预制体+主播UID白名单校验 | P0/P1 | 7h | ⏳ |
| T187~T188 | ⚡效率加速按钮（Worker产出×2）+倒计时显示 | P1 | 7h | ⏳ |
| T189~T190 | 紧急结束按钮+难度切换 | P1/P2 | 7h | ⏳ |
| T191~T195 | 测试模式/欢迎语/在线人数/一键重置/操作日志 | P1/P2 | 17h | ⏳ |
| T234~T235 | 热键绑定+误触确认弹窗 | P1/P2 | 5h | ⏳ |

**模块 I：QA**

| 任务ID | 任务名称 | 优先级 | 估时 | 状态 |
|--------|---------|--------|------|------|
| T196~T198 | PlayMode测试框架+Worker分配单元测试+礼物队列测试 | P1 | 13h | ⏳ |
| T199~T200 | Node.js Jest框架+GameEngine状态机测试 | P1 | 9h | ⏳ |
| T201~T207 | 内存泄漏/FPS监控/压力测试/边缘案例/重连端到端/稳定性/并发性能测试 | P0/P1 | 26h | ⏳ |
| T236~T237 | 礼物特效帧率压测+20人Worker并发性能测试 | P1 | 6h | ⏳ |

**模块 J：发布/运营**

| 任务ID | 任务名称 | 优先级 | 估时 | 状态 |
|--------|---------|--------|------|------|
| T208~T210 | QuickBuild验证+版本号自动递增+产物目录规范化 | P0/P1 | 8h | ⏳ |
| T238~T242 | 抖音审核资料/OBS配置文档/Bug反馈机器人/灰度发布流程/Changelog自动生成 | P0/P1/P2 | 16h | ⏳ |

---

### §22.10 任务统计总览

| 模块 | 任务数 | P0 | P1 | P2 | P3 | 总估时(h) | 已完成 |
|------|-------|----|----|----|----|----------|--------|
| A Worker系统 | 35 | 14 | 13 | 7 | 0 | 138 | 2 |
| B 礼物特效系统 | 25 | 12 | 7 | 3 | 0 | 110 | 0 |
| C 怪物系统 | 30 | 12 | 11 | 5 | 0 | 138 | 0 |
| D 音效系统 | 31 | 20 | 8 | 2 | 0 | 80 | 0 |
| E 技术美术 | 35 | 8 | 16 | 8 | 0 | 145 | 0 |
| F UI/UX优化 | 28 | 8 | 14 | 4 | 1 | 94 | 0 |
| G 服务器/后端 | 21 | 12 | 6 | 3 | 0 | 96 | 4 |
| H 主播功能 | 13 | 1 | 7 | 4 | 0 | 47 | 0 |
| I 质量保证 | 14 | 2 | 11 | 0 | 0 | 56 | 0 |
| J 发布/运营 | 10 | 1 | 5 | 3 | 0 | 34 | 0 |
| **合计** | **242** | **90** | **98** | **39** | **1** | **938h** | **6** |

---

### §22.11 下一步行动（Next Actions）

> 基于里程碑 M4，优先开始以下 P0 任务：

1. **T003** Worker移动目标坐标注册表 — 在编辑器中为20个工位配置坐标
2. **T004** Worker移动插值（DOTween） — `MoveTo(targetPos, 0.8s, Ease.InOutQuad)`
3. **T006~T008** 工位数据结构 + 空闲管理 + 就近分配算法
4. **T010~T014** 5种工作动画绑定（采食/采煤/采矿/添柴/攻击）
5. **T211~T215** 弹幕指令解析接入 + 产出定时器 + HUD推送 + 状态枚举

> M4预计工时 56h（含10h已完成基础框架），是整个项目关键路径首个待完成里程碑。
