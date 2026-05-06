using System;

namespace DrscfZ.Survival
{
    // ==================== 服务器 → 客户端 数据结构 ====================
    // 与服务器 WebSocket 消息协议字段保持一致

    /// <summary>游戏状态同步（开始/暂停/结算）</summary>
    [Serializable]
    public class SurvivalGameStateData
    {
        public string state;         // "idle" | "day" | "night" | "settlement"（recovery 期由服务端归一为 "day"+variant="recovery"，audit-r12 GAP-A12-04）
        public int    day;           // 当前天数（1-N）
        public float  remainingTime; // 本阶段剩余秒数
        public int    food;
        public int    coal;
        public int    ore;
        public float  furnaceTemp;   // -100 ~ 100
        public int    gateHp;
        public int    gateMaxHp;
        public int    gateLevel;     // 城门当前等级（1-6，v1.22 扩展 Lv5/Lv6）
        public int    scorePool;     // 当前积分池总量
        // §16 v1.27 永续模式：recovery 期间服务端推送 'recovery'，常规为 'normal'
        // （旧消息缺失时反序列化回落默认 "normal"，向下兼容）
        public string variant = "normal";
        // 🆕 v1.22 §10 城门升级系统 v2
        public string   gateTierName;       // 城门层级名（"木栅栏"/"加固木门"/.../"巨龙要塞"）
        public string[] gateFeatures;       // 已激活的特性（如 ["thorns_20", "frost_aura_6"]）
        public bool     gateDailyUpgraded;  // 当前天是否已升级（时机限制）
        // 🆕 v1.27 §36.12 分时段解锁：截至当前 seasonDay 的已解锁功能 id 全集
        //   老用户豁免时服务端返全集；getFullState 每次推送
        //   feature id 常量见 SurvivalMessageProtocol.FeatureXxx
        public string[] unlockedFeatures;
        // 🆕 §36.5.1 每日闯关上限扩展 4 字段（服务端未启用 cap flag 时 dailyCapMax 可能为 0）
        public int  dailyFortressDayGained;
        public int  dailyCapMax;
        public long dailyResetAt;              // Unix ms，下次 UTC+8 05:00
        public bool dailyCapBlocked;
        // ⚠️ audit-r24 GAP-A24-02 补 7 个关键字段（断线重连静默丢失修复）：
        //   服务端 SurvivalGameEngine.js:1473-1479 getFullState() 已 emit 7 字段，但客户端 Data 类完全无定义，
        //   JsonUtility 反序列化时整块字段静默丢失，断线重连后客户端 SeasonTopBarUI / FortressDayBadgeUI 等关键 HUD
        //   会显示错误状态直到下一条 world_clock_tick / season_state / fortress_day_changed 抵达（最长 1s 不一致窗口）。
        //   r24 修复后客户端断线重连可立即拿到完整赛季/堡垒日/全服时钟状态。
        public int    fortressDay;             // §36 当前堡垒日（默认 1）
        public int    maxFortressDay;          // §36 最高堡垒日（主播榜算法依据，默认 1）
        public int    seasonId;                // §36 赛季 ID（递增 1, 2, 3...）
        public int    seasonDay;               // §36 赛季日（1-7）
        public string themeId;                 // §36 主题（'classic_frozen' / 'blood_moon' / 'snowstorm' / 'dawn' / 'frenzy' / 'serene'）
        public string phase;                   // §36 GlobalClock 全服时钟阶段（'day' / 'night'）
        public int    phaseRemainingSec;       // §36 GlobalClock 阶段剩余秒数
        // 🆕 §19.1 P0-B3：矿工等级/皮肤/HP 批量同步（服务端 getFullState 输出）
        //   每个元素对应一个矿工的当前运行态；缺失字段 JsonUtility 回落默认值。
        //   与 worker_hp_update 独立消息配合：game_state 推送全量；hp_update 推送增量。
        public WorkerStateData[] workers;
        // 注：服务端还发送 workerHp（dict），因 JsonUtility 不支持 Dictionary，
        //     由独立的 worker_hp_update 消息负责同步，此处忽略
        // 🔴 audit-r45 GAP-D45-06：§33 助威者断线重连恢复（spec line 5194「supporters 数组新增」）
        //   原 SurvivalGameStateData 无此两字段 → 重连后 SupporterTopBarUI 守护者:N 助威:M 计数无法初始化
        //   服务端 SurvivalGameEngine.js getFullState 末尾 emit；缺失时 supporters=null / supporterCount=0
        public SupporterStateEntry[] supporters;
        public int                   supporterCount;
    }

    /// <summary>🔴 audit-r45 GAP-D45-06：助威者断线重连快照（embedded in SurvivalGameStateData.supporters[]）。
    /// 服务端 _supporters Map 序列化输出；客户端 SupporterTopBarUI 据 supporterCount 刷新计数。</summary>
    [Serializable]
    public class SupporterStateEntry
    {
        public string playerId;
        public string playerName;
        public long   joinedAt;       // Unix ms（30s 冷却倒计时锚点）
        public int    totalContrib;   // 累计贡献值（含降级前快照，§33.9 weekly_ranking 合并源）
    }

    /// <summary>🆕 §19.1 P0-B3：矿工运行态（embedded in SurvivalGameStateData.workers[]）
    /// 服务端 getFullState 输出；每帧/每次 game_state 推送时覆盖客户端缓存。
    /// 与 WorkerHpUpdateData.WorkerHpEntry 字段语义一致但更完整（含 level/skinTier/state）。</summary>
    [Serializable]
    public class WorkerStateData
    {
        public string playerId;
        public string playerName;
        public int    level;        // §30 矿工等级 1-100
        public int    skinTier;     // §30 阶段 1-10（对应皮肤 T01-T10），传奇皮肤走 G01-G03
        public string skinId;       // 皮肤资源路径 id（如 "T05" / "G01"），空 = 默认
        public int    maxHp;
        public int    currentHp;
        public string state;        // "idle" | "working" | "combat" | "dead" | "frozen" | "expedition"
    }

    /// <summary>资源状态更新（服务器定时推送）</summary>
    [Serializable]
    public class ResourceUpdateData
    {
        public int   food;
        public int   coal;
        public int   ore;
        public float furnaceTemp;
        public int   gateHp;
        public int   gateMaxHp;
        public int   gateLevel;      // 城门当前等级（1-6，v1.22 扩展 Lv5/Lv6）
        public float remainingTime;
        public int   scorePool;      // 当前积分池总量
        // 🆕 v1.22 §10 城门升级系统 v2
        public string   gateTierName;
        public string[] gateFeatures;
        public bool     gateDailyUpgraded;
        // 🆕 §34 Layer 3 组 C 体验引擎扩展字段（服务端捎带推送；缺失字段 JsonUtility 回落 0/null）
        public int                    tension;              // §34 E1 危机感知 0-100
        public GiftRecommendationData giftRecommendation;   // §34 E4 精准付费触发（可为 null）
        public int                    totalContribution;    // §34 E3b 全服累计贡献（驱动里程碑进度条）
        // 🆕 §34B B3 heavy_fog 事件：30s 内隐藏所有怪物血条（服务端 resource_update 捎带推送，
        //   事件结束后置 false 恢复）。缺失字段 JsonUtility 反序列化为 false。
        //   Fix B (组 B Reviewer P0)：前端 ResourceUpdateData 原本没有对应字段，JsonUtility 会丢弃。
        public bool                   hideMonsterHp;
    }

    /// <summary>昼夜阶段切换</summary>
    [Serializable]
    public class PhaseChangedData
    {
        public string phase;         // "day" | "night"
        public int    day;           // 第几天
        public float  phaseDuration; // 本阶段秒数
        // §16 v1.27 永续模式：variant 仅在恢复期为 "recovery"，常规白天/黑夜为 "normal"
        // §36.5 v1.27 和平夜扩展：variant 可为 "peace_night"（D1 整夜柔光罩）
        //   / "peace_night_silent"（D2 无怪但不显示 UI）/ "peace_night_prelude"（D3 前 30s 和平）
        // 服务器内部 recovery 态对外仍以 phase="day" 推送，客户端靠 variant 识别
        // 旧消息缺失此字段时反序列化回落 "normal"（JsonUtility 对默认值生效）
        public string variant = "normal";
        // §36.5 v1.27 peace_night_prelude 专用：prelude 结束（怪物开始刷新）的 Unix ms 时间戳。
        // 其它 variant 服务端不下发（JsonUtility 反序列化默认为 0）
        public long   peacePreludeEndsAt;
        // 🆕 §34 Layer 3 组 D 叙事节奏（E2）：当前幕标签（prologue/act1/act2/act3/finale），
        //   仅用于 BGM 层切换；服务端下划线命名对齐，JsonUtility 反序列化缺失时保持空字符串。
        public string act_tag;
        // 🆕 §34 Layer 3 组 D 夜间修饰符（E6）：phase="night" 时服务端可能下发；其他变体/白天为 null。
        //   缺失字段 JsonUtility 反序列化为 null（整块），不触发 UI。
        public NightModifierData nightModifier;
    }

    /// <summary>🆕 §34 Layer 3 组 D（E2）叙事节奏 —— 幕切换公告数据（type=chapter_changed）
    /// 映射到赛季日范围：prologue(D1) / act1(D2-3) / act2(D4-5) / act3(D6) / finale(D7)。
    /// ⚠️ audit-r24 GAP-D24-02 补 endNote + seasonDay 2 字段：
    ///   服务端 SurvivalGameEngine.js:6921-6926 emit { name, actTag, startDay, endDay, endNote, seasonDay } 共 6 字段，
    ///   r24 之前客户端 ChapterChangedData 仅 4 字段（name/startDay/endDay/actTag），缺 endNote + seasonDay；
    ///   JsonUtility 反序列化时整块字段静默丢失，客户端 ChapterAnnouncementUI 永远无法显示 endNote 文案
    ///   （"首个精英怪出现"/"双 Boss 同时出现"等 §34.4 E2 幕终事件关键文案）。</summary>
    [Serializable]
    public class ChapterChangedData
    {
        public string name;      // "序章·踏入极地" / "第一幕·资源争夺" 等
        public int    startDay;  // 起始赛季日（含）
        public int    endDay;    // 结束赛季日（含）
        public string actTag;    // "prologue"/"act1"/"act2"/"act3"/"finale"（驼峰命名，与 phase_changed.act_tag 对齐 BGM 层）
        public string endNote;   // 🆕 audit-r24 GAP-D24-02 + 🔴 r26 GAP-A26-06 注释精确化：幕的整体特征描述（prologue→"教学+安全" / act1→"压力上升" / act2→"高峰冲突" / act3→"决战"等），与 §34.4 E2 "幕终事件关键文案"语义不同（后者由独立 chapter_end_event 协议承载，事件如"首个精英怪来袭"/"双 Boss 同时出现"通过 chapter_end_event.event/hint 字段获取）
        public int    seasonDay; // 🆕 audit-r24 GAP-D24-02：服务端推送时的当前赛季日（1-7）
    }

    /// <summary>🆕 §34 Layer 3 组 D（E6）夜间修饰符 —— 单次夜晚的"关卡条件"
    /// 7 种变体：normal / blood_moon / polar_night / fortified / frenzy / hunters / blizzard_night。
    /// 客户端根据 id 切换全屏光照预设 + 全屏公告文本。</summary>
    [Serializable]
    public class NightModifierData
    {
        public string id;          // "normal"/"blood_moon"/"polar_night"/"fortified"/"frenzy"/"hunters"/"blizzard_night"
        public string name;        // "血月"/"极夜"/"坚守之夜"/...
        public string description; // "单 Boss HP x3，击杀贡献 x1.5"
    }

    /// <summary>🆕 §34 Layer 3 组 D（E5a）智能提词器 —— 仅主播可见的话术提示（type=streamer_prompt）
    /// priority 三级视觉：urgent（红底加粗）/ social（蓝底）/ info（灰底半透）。
    /// 主播端仅 isRoomCreator=true 显示；其他观众端收到也应过滤。
    /// 🔴 audit-r37 GAP-E37-21：补 recipient 字段（服务端 SurvivalGameEngine.js:7170-7177 emit 含 recipient: 'broadcaster_only'）
    ///   旧版客户端缺字段 → 只能依赖客户端 isRoomCreator 自行过滤（脆弱），现在可双层守门</summary>
    [Serializable]
    public class StreamerPromptData
    {
        public string text;       // "食物快没了！提醒观众刷甜甜圈！"
        public string priority;   // "urgent" | "social" | "info"
        public string recipient;  // 🔴 audit-r37 GAP-E37-21：'broadcaster_only' = 仅主播；其它值时所有客户端均可见
        // 注：客户端 SurvivalGameManager.cs:950 case "streamer_prompt" 已加双层守门（recipient + _isRoomCreator）
    }

    /// <summary>🆕 §34 Layer 3 组 D（E5b）夜战报告 —— 夜→昼转换时 2.5s 多行回顾（type=night_report）
    /// 不与结算面板重叠（结算仅游戏结束时，夜战报告每夜转白天时）。
    /// ⚠️ audit-r24 GAP-D24-01 补 nightModifierId 字段：
    ///   服务端 SurvivalGameEngine.js:7246 emit `nightModifierId: this._currentNightModifier ? id : 'normal'`，
    ///   r24 之前客户端 NightReportData 完全无此字段，JsonUtility 静默丢失，
    ///   §34.4 E5b 夜战报告自实装至今从未触发过 modifier-aware 反馈
    ///   （"血月之夜：消灭 47 只怪"/"极夜：仅靠 X 名矿工守住"等 7 种修饰符差异化 UI 全部失效）。</summary>
    [Serializable]
    public class NightReportData
    {
        public int    day;               // 第几夜
        public int    monstersKilled;    // 总消灭数
        public bool   bossDefeated;      // 是否击杀 Boss
        public string mvpPlayerId;       // 夜间 MVP（可为空）
        public string mvpPlayerName;     // MVP 昵称（可为空）
        public int    mvpKills;          // MVP 击杀数
        public string topGifterName;     // 最佳援助昵称（可为空）
        public string topGiftName;       // 最佳援助礼物名（可为空）
        public float  closestCallHpPct;  // 城门最低血量比（0-1）
        public float  survivalRate;      // 矿工存活率（0-1）
        public string nightModifierId;   // 🆕 audit-r24 GAP-D24-01：'normal'/'blood_moon'/'polar_night'/'fortified'/'frenzy'/'hunters'/'blizzard_night'
    }

    /// <summary>🆕 §34 Layer 3 组 D（E8）参与感唤回 —— 单条记录（entries[] 数组元素）
    /// 服务端批量推送时每条代表一个贡献>0 的玩家。</summary>
    [Serializable]
    public class EngagementEntryData
    {
        public string playerId;         // 目标玩家 ID（广播模式下客户端过滤用）
        public int    rank;             // 当前排名（全服）
        public int    gapToTop3;        // 距 Top 3 的贡献差值
        public int    currentContrib;   // 当前贡献
    }

    /// <summary>🆕 §34 Layer 3 组 D（E8）参与感唤回 —— 每 5 分钟对贡献>0 的玩家推送（type=engagement_reminder）
    /// 服务端批量广播 entries[] 数组；客户端按 playerId === self 过滤或主播端一律跳过。
    /// ⚠️ 使用 EngagementEntryData[] 数组而非 List&lt;T&gt;，与项目其他协议结构（MonsterSpawnInfo[] 等）对齐，
    ///   规避 Unity JsonUtility 对嵌套 List&lt;T&gt; 的反序列化兼容性风险。</summary>
    [Serializable]
    public class EngagementReminderData
    {
        public EngagementEntryData[] entries;
    }

    // v1.27 §14 / §34.4 E9 废止：ChangeDifficultyData 已删除（difficulty 三档系统不再存在）

    /// <summary>单只怪物元数据（🆕 §31 多样性系统：monster_wave.monsters[] 数组元素）。
    /// 字段顺序严格对齐服务端广播（monsterId/type/variant/hp/speed）。</summary>
    [Serializable]
    public class MonsterSpawnInfo
    {
        public string monsterId;  // 唯一 ID（服务端生成）
        public string type;       // "normal" | "elite" | "boss"
        public string variant;    // "normal" | "rush" | "assassin" | "ice" | "summoner" | "guard" | "mini"
        public int    hp;         // 初始 HP（由服务端按 variant 乘系数算好）
        public float  speed;      // 🆕 §31 变种速度（服务端 cfg.normal.spd × VARIANT_SPEED_MULT；0 → 客户端 fallback）
    }

    /// <summary>怪物波次信息（🆕 §31 扩展 monsters[] / isSummonSpawn；旧字段保留向下兼容）</summary>
    [Serializable]
    public class MonsterWaveData
    {
        public int    waveIndex;  // 第几批（0-based；audit-r23 GAP-A23-05 注：白天 scout 路径 emit waveIndex=-1）
        public int    day;
        public string monsterId;  // e.g. "X_guai01"（旧协议字段，兼容保留）
        public int    count;      // 旧协议字段，兼容保留；新客户端优先走 monsters[]
        public string spawnSide;  // "left" | "right" | "top" | "all" | "spawn_at_death"

        // 🆕 §31 多样性系统扩展
        public MonsterSpawnInfo[] monsters;   // 新字段：每只怪物独立携带 variant；为 null/空 → 走旧 count 路径
        public bool               isSummonSpawn;  // 召唤怪死亡触发的迷你怪生成（spawn 位置从屏幕中心偏移）
        // audit-r23 GAP-A23-05：白天 scout 路径标识（服务端 L5442/L5458 emit；E03/§11 white-day scout 弱怪 atk=0 hp×0.3）
        // 客户端可据此过滤战斗判定（不计入 _activeMonsters 战斗逻辑），仅做视觉/玩法占位
        public bool               isDaytimeScout;
        // ⚠️ audit-r24 GAP-A24-03 补 elite_raid 4 字段（§24.4 主播轮盘"精英来袭"卡触发的单只精英怪）：
        //   服务端 SurvivalGameEngine.js:8175-8179 emit { spawnSide, monsterType: 'elite_raid', bypassCap: true, eliteHp, eliteAtk }
        //   r24 之前客户端 MonsterWaveData 完全无这 4 字段，JsonUtility 静默丢失，
        //   "精英来袭"卡触发后客户端按普通 normal 怪渲染，玩家无法识别精英怪威胁感（30s deadline）；
        //   bypassCap=true 也无法在 SpawnWave 路径跳过 maxAliveMonsters 检查导致排队延迟。
        public string monsterType;             // 'elite_raid'（轮盘卡触发）/ null（常规波次）
        public bool   bypassCap;               // true → SpawnWave 跳过 maxAliveMonsters 排队
        public int    eliteHp;                 // 精英怪 HP（服务端按 difficulty 计算）
        public int    eliteAtk;                // 精英怪 ATK
        // ⚠️ audit-r24 GAP-C24-03 补字段：服务端 SurvivalGameEngine.js:5239 _spawnBossGuards emit
        //   monster_wave { isBossGuardSpawn: true, ... }；客户端可据此触发暗金标识 + 震屏 + 音效
        public bool   isBossGuardSpawn;        // 召唤 Boss 卫兵生成
    }

    /// <summary>玩家工作指令（服务器转发评论）</summary>
    [Serializable]
    public class WorkCommandData
    {
        public string playerId;
        public string playerName;
        public string avatarUrl;
        // r14 GAP-A14-A4：cmd=5 修城门已 v2.0 移除（策划案 §6.1 L359）
        public int    commandId;   // 1=食物 2=煤炭 3=矿石 4=添柴 6=打怪（cmd=5 已移除）
        public string commandName; // "food"|"coal"|"ore"|"heat"|"attack"
        // 🆕 §34 Layer 2 组 A B9：服务端在 work_command 广播附带触发者的 playerStats 快照
        //   服务端 _calcPlayerStats(playerId) 返回 { contribution, rank, fairyWandBonus }；
        //   未注册守护者（匿名/助威者）时为 {0,0,0}。
        //   JsonUtility 反序列化缺失字段为 null（整块），客户端据此判断是否触发 PersonalContribUI。
        public PlayerStatsData playerStats;
    }

    /// <summary>礼物 effects 嵌套字段（audit-r6 P1-F1 补齐：T5 AOE 视觉反馈）
    /// 服务端 _handleLoveExplosion / _handleMysteryAirdrop / case 'energy_battery' / case 'mystery_airdrop' / 助威者 T1/T4/T5 等效果分支下发 effects object。
    /// ⚠️ audit-r22 GAP-A22-02 修复：
    ///   1. revivedWorkers 由 int 改为 string[]（服务端 L2169/L2488 emit `[playerId]` 数组，原 int 反序列化失败回 0）
    ///   2. 补 globalEfficiencyDuration（T2 服务端 L2041 emit 30s）+ addGateHp（T5/T6 服务端 L2178/L2493 emit 200）
    /// ⚠️ audit-r23 GAP-A23-01 补 10 字段（T4/T6/助威者 T1/T4/T5 路径之前 0 消费）：
    ///   tempBoost / boostDuration（T4 守护者+助威者 emit）/ unfrozenWorkers（T4 联动解冻）/ giftPause（T6）
    ///   redirectTargetId / redirectTargetName（助威者 T1/T4 重路由目标）
    ///   addFood / addCoal / addOre / addHeat（T6 神秘空投 + T4 炉温）
    /// </summary>
    [Serializable]
    public class GiftEffectsData
    {
        public int      aoeDamage;             // T5 全体怪物单次 AOE 伤害值
        public int      monstersKilled;        // T5 击杀怪物数（用于跑马灯/GloryMoment）
        public string[] revivedWorkers;        // T5/T6 复活矿工 playerId 数组（audit-r22 GAP-A22-02 类型修正：int → string[]）
        public int      healedWorkers;         // T5 治疗满血矿工数
        public float    efficiencyBonus;       // T1 仙女棒累计发送者个人加成（同步 §34 B8）
        public float    globalEfficiencyBoost; // T2 能力药丸全局 30s 提升倍率
        public int      globalEfficiencyDuration; // T2 全局加成持续时长秒（audit-r22 GAP-A22-02 补字段；服务端 L2041 emit 30）
        public int      addGateHp;             // T5/T6 城门 +HP（audit-r22 GAP-A22-02 补字段；服务端 L2178/L2493 emit 200）
        public bool     supporterRedirect;     // §33.4 T4/T5 由助威者发送时重路由到随机守护者
        // audit-r23 GAP-A23-01 补字段（T4 守护者 + T4/T6 + 助威者 T1/T4 路径 0 消费 16 轮 audit）：
        public float    tempBoost;             // T4 能量电池：发送者效率倍率（1.3）；助威者路径 = 重路由目标的倍率（服务端 L2069/L2551 emit）
        public int      boostDuration;         // T4 倍率持续秒数（180）；服务端 L2070/L2552 emit
        public int      unfrozenWorkers;       // T4 联动解冻：本次 T4 触发解冻的矿工数（服务端 L2079/L2565 emit）
        public int      giftPause;             // T6 神秘空投触发的全局暂停毫秒（3000）；服务端 L2100 emit
        public string   redirectTargetId;      // 助威者 T1/T4 重路由目标守护者 playerId；服务端 L2411/L2553 emit（空字符串=房间无守护者，未重路由）
        public string   redirectTargetName;    // 助威者 T1/T4 重路由目标守护者展示名；服务端 L2412/L2554 emit
        public int      addFood;               // T6 神秘空投资源加成；服务端 L2096 emit（500）；T3 甜甜圈 L2052 emit（100）
        public int      addCoal;               // T6 神秘空投煤炭加成；服务端 L2097 emit（200）
        public int      addOre;                // T6 神秘空投矿石加成；服务端 L2098 emit（100）
        public int      addHeat;               // T4 炉温加成；服务端 L2068/L2539 emit（30）
    }

    /// <summary>礼物效果</summary>
    [Serializable]
    public class SurvivalGiftData
    {
        public string playerId;
        public string playerName;
        public string avatarUrl;
        public string giftId;    // 礼物英文ID（如 "fairy_wand"），用于客户端效果/名称查询
        public string giftName;  // 礼物展示名（服务器设置，通常是中文）
        public int    giftTier;    // 1-6
        public string giftTierStr; // audit-r6 P1-F1: "T1"~"T6" 字符串（服务端下发）
        public int    score;       // audit-r6 P1-F1: 积分值（1/100/500/1000/2000/5000）
        public float  giftValue;   // 礼物价值（分）
        public int    addFood;
        public int    addCoal;
        public int    addOre;
        public float  addHeat;     // 炉温加成
        public int    addGateHp;   // 城门HP回复
        public float  contribution;// 贡献值（用于排行）
        public GiftEffectsData effects;  // audit-r6 P1-F1: nested effects（T5 AOE 等详细反馈）
    }

    /// <summary>玩家加入</summary>
    [Serializable]
    public class SurvivalPlayerJoinedData
    {
        public string playerId;
        public string playerName;
        public string avatarUrl;
        public int    totalPlayers;
    }

    /// <summary>游戏结算 - 排行榜单条记录</summary>
    [Serializable]
    public class SurvivalRankingEntry
    {
        public int    rank;
        public string playerId;
        public string playerName;
        public float  contribution;
        public int    payout;       // 本局积分池瓜分所得（D：积分池系统）
    }

    /// <summary>阶段性结算（🆕 v1.26 永续模式：无胜利分支，仅失败降级 + 恢复期）
    /// 字段顺序严格对齐服务端 Object.keys（§16.6）：
    ///   reason, dayssurvived, fortressDayBefore, fortressDayAfter, newbieProtected,
    ///   totalScore, rankings, scorePool, distributed, carryover, payoutRate</summary>
    [Serializable]
    public class SurvivalGameEndedData
    {
        // 🆕 v1.26：移除 result 字段（无胜利分支），reason 枚举新增 all_dead，保留 manual（§16.4）
        public string reason;             // "food_depleted" | "temp_freeze" | "gate_breached" | "all_dead" | "manual"
        public int    dayssurvived;       // 本次周期（上次恢复→本次失败）存活天数
        public int    fortressDayBefore;  // 🆕 §16.6 本次失败前的堡垒日
        public int    fortressDayAfter;   // 🆕 §16.6 降级后的堡垒日（新手保护期下 =before；manual 时等于 before）
        public bool   newbieProtected;    // 🆕 §16.6 是否触发新手保护（Day 1-10）
        public float  totalScore;         // 本次周期内全局总贡献值之和
        public SurvivalRankingEntry[] rankings; // Top10（含 payout）
        // D：积分池
        public int    scorePool;          // 本次周期积分池
        public int    distributed;        // 实际瓜分金额
        public int    carryover;          // 结余（进入恢复期后的下一周期初始积分池）
        public float  payoutRate;         // 固定 0.3（失败，永续模式无胜利分支）
        // audit-r18 §34 F6 双档分配：服务端 SurvivalGameEngine.js:3174-3177 已 emit，r18 客户端补字段供 SettlementUI 帧 B 渲染
        public int    top10Pool;          // 🆕 audit-r18 §34 F6：Top10 双档分配总额（70% 池）
        public int    tailPool;           // 🆕 audit-r18 §34 F6：剩余 30% 池总额（按贡献 ≥100 分配给非 Top10）
        public int    tailEligibleCount;  // 🆕 audit-r18 §34 F6：参与 tail 分配的玩家数（贡献 ≥100 的非 Top10）
        // 🔴 audit-r31 GAP-A26-08 实装：原 contributionRewards 用 Dictionary 结构 JsonUtility 不支持→静默丢失。
        //   服务端 r31 改 emit `tailRewards: [{playerId, playerName, share}]` 数组结构（兼容 JsonUtility）。
        //   旧字段 contributionRewards Dict 保留作老客户端兼容（新客户端不消费）。
        public TailRewardEntry[] tailRewards;  // 🆕 audit-r31：非 Top10 且 ≥100 贡献者的奖励数组
    }

    /// <summary>🆕 audit-r31 GAP-A26-08：tail 30% 池单条奖励记录（数组元素，替代 contributionRewards Dictionary 结构）</summary>
    [Serializable]
    public class TailRewardEntry
    {
        public string playerId;
        public string playerName;
        public int    share;
    }

    /// <summary>end_game 被服务端拒绝（仅 state !∈ {day,night} 时返回，§16.4）</summary>
    [Serializable]
    public class EndGameFailedData
    {
        public string reason;        // 当前仅 "wrong_phase"
        public string currentState;  // 'idle'|'loading'|'settlement'|'recovery' 等
    }

    /// <summary>Boss出现通知（服务器每夜推送）</summary>
    [Serializable]
    public class BossAppearedData
    {
        public int    day;
        public int    bossHp;
        public int    bossAtk;
        public bool   isActEndBoss; // audit-r18 §16 act1 终结之夜双 Boss 第 2 只标记（_spawnExtraBossForActEnd L7017 emit）
        public bool   isBloodMoon;  // audit-r18 §36 blood_moon 主题夜超级 Boss 标记（_applyNightModifier L7299 emit）
        public int    rushIndex;    // audit-r19 §16 act2 终结之夜 mini BossRush（3 Boss 间隔 30s，rushIndex 1-3，_scheduleActEndMiniBossRush L7047 emit）
    }

    /// <summary>战斗攻击数据（服务器广播，commandId=6触发）</summary>
    [Serializable]
    public class CombatAttackData
    {
        public string attackerId;          // 攻击者玩家ID（原 playerId）
        public string attackerName;        // 攻击者昵称（原 playerName）
        public string targetId;            // 目标怪物ID（原 monsterId）
        public string targetType;          // 怪物类型 "normal"|"elite"|"boss"
        public float  damage;              // 造成的伤害值
        public float  targetHpRemaining;   // 攻击后怪物剩余HP
    }

    /// <summary>怪物死亡数据（服务器广播）</summary>
    [Serializable]
    public class MonsterDiedData
    {
        public string monsterId;    // 死亡怪物ID
        public string monsterType;  // "normal" | "elite" | "boss" | "elite_raid"
        // audit-r23 GAP-A23-02：killerId 服务端 emit 5 种来源，全部需要客户端识别：
        //   - 玩家 secOpenId（普通击杀；常态）
        //   - 'gate_thorns'（§10 城门反伤；服务端 L5139 emit）
        //   - 'gate_frost_pulse'（§10 寒冰冲击波被动；服务端 L4521 emit）
        //   - 'meteor_shower'（§24.4 主播轮盘流星雨；服务端 L5560 emit；同时 reason='meteor_shower'）
        //   - ''（精英来袭超时撤退；服务端 L8211 emit；同时 reason='elite_raid_timeout'）
        public string killerId;
        public string reason;       // audit-r19 §31 被动死亡原因（'elite_raid_timeout' | 'meteor_shower' | ''；服务端 L8210/L8369 emit；玩家击杀时为空字符串）
    }

    /// <summary>城门升级数据（消耗矿石升级城门）</summary>
    [Serializable]
    public class GateUpgradedData
    {
        public int    newLevel;      // 升级后等级 (2-6，v1.22 扩展 Lv5/Lv6)
        public int    newMaxHp;      // 新的最大HP
        public int    oreRemaining;  // 升级后剩余矿石
        public string upgradedBy;    // 触发升级的玩家ID
        // 🆕 v1.22 §10 城门升级系统 v2
        public string   tierName;    // 层级名（"木栅栏"/"铁栅"/.../"巨龙要塞"）
        public string[] newFeatures; // 新解锁的特性 ["thorns", "frost_aura", "frost_pulse"]
        public int      hpBonus;     // 本次升级回血量（Lv6 = gateMaxHp - gateHp，即回满）
        public string   source;      // 'broadcaster' | 'gift_t6' | 'expedition_trader'
    }

    /// <summary>城门升级失败（矿石不足 / 已满级 / 未解锁 / 不在白天 / 每日限制 / Boss 战）</summary>
    [Serializable]
    public class GateUpgradeFailedData
    {
        // P0-B5 修正：按服务端 _handleGateUpgrade 所有 emit 路径对照实际下发枚举：
        //   "wrong_phase"      — 不在白天（非 day/night 态或 recovery）
        //   "boss_fight"       — Boss 战期间禁止升级
        //   "daily_limit"      — 当天已升过一次（gateDailyUpgraded=true）
        //   "feature_locked"   — §36.12 分时段解锁未到（Lv5-6 需 D4+）
        //   "max_level"        — 已达 Lv6
        //   "insufficient_ore" — 矿石不足
        // （历史注释里的 "already_upgraded_today" / "phase_disallowed" 服务端从不下发，以实际枚举为准）
        public string reason;
        public int    currentLevel;  // 当前等级（max_level时）
        public int    required;      // 需要矿石数（insufficient_ore时）
        public int    available;     // 当前矿石数（insufficient_ore时）
        // 🆕 v1.27 §10 仅 feature_locked 时有效
        public int    unlockDay;     // 解锁所需天数
        public int    blockedLevel;  // 阻止升级的等级边界（Lv1-4 vs Lv5-6 解锁分类）
    }

    /// <summary>城门等级特性触发（Lv4反伤 / Lv5光环激活 / Lv6冲击波）🆕 v1.22
    /// audit-r7 §19 扩展：补齐 x/y/z/durationMs/targets 通用字段（与 gatePos/hitMonsters 并存，互为 alias）。
    /// JsonUtility 对服务端未下发的字段保持默认值 0/null，Handler 按字段优先级兜底（flat > nested）。</summary>
    [Serializable]
    public class GateEffectTriggeredData
    {
        public string   effect;            // 'thorns' | 'frost_aura' | 'frost_pulse'
        // thorns：服务端 _spawnWave 批量结算反伤，均分到 _activeMonsters 全体怪物
        //   hitMonsters = 受反伤的所有怪物 ID；damagePerMonster = 每只怪物承担的反伤；totalDamage = 总反伤
        // frost_aura：Lv5 常驻光环 on/off（active=true 激活、false 关闭）+ radius/slowMult/gatePos
        // frost_pulse：Lv6 寒冰冲击波 15s 周期
        //   hitMonsters = 被冲击波命中的怪物 ID 列表；radius/damage/freezeMs = 视觉参数
        public string[] hitMonsters;       // thorns / frost_pulse 共用
        public int      damagePerMonster;  // thorns 时的每只怪物反伤
        public int      totalDamage;       // thorns 时的总反伤
        public int      radius;            // frost_aura / frost_pulse 的视觉+判定半径
        public int      damage;            // frost_pulse 时的冲击波单次伤害
        public int      freezeMs;          // frost_pulse 时的冻结时长
        // 🆕 P0-B6：frost_aura 专用字段
        public bool     active;            // true=激活（Lv5+ 升级时）/ false=关闭（降级时）
        public float    slowMult;          // 光环内速度倍率（默认 0.7）
        public GatePosData gatePos;        // 光环圆心（服务端 GATE_POS，{x,y,z}）

        // 🆕 audit-r7 §19 v1.27 通用字段（与 gatePos / hitMonsters / freezeMs 互为 alias，兼容未来服务端扁平化）
        public float    x;                 // 触发位置 X（相机/光环/冲击波原点，与 gatePos.x 等价；JsonUtility 默认 0）
        public float    y;                 // 触发位置 Y（默认 0）
        public float    z;                 // 触发位置 Z（默认 0）
        public float    durationMs;        // 持续时长（毫秒；瞬时特效可为 0 或 300；与 freezeMs 语义部分重叠）
        public string[] targets;           // 受影响 monsterId 数组（alias of hitMonsters；客户端 Handler 取 hitMonsters 优先，null 时退回 targets）
    }

    /// <summary>frost_aura 的 gatePos 嵌套结构（JsonUtility 需要显式 Serializable 子类）。🆕 P0-B6</summary>
    [Serializable]
    public class GatePosData
    {
        public float x;
        public float y;
        public float z;
    }

    /// <summary>🔴 audit-r43 GAP-A43-01 §10.6.2/§10.7.3 城门 Lv3+ 减伤双色飘字
    /// 服务端 SurvivalGameEngine.js _spawnWave 减伤路径在 reducedDamage &lt; rawDamage 时下发；
    /// 客户端 SurvivalGameManager case 'gate_damage_taken' handler 调 DamageNumber.Show 双色重载（青色，与 hp delta 黄色区分）。</summary>
    [Serializable]
    public class GateDamageTakenData
    {
        public int rawDamage;       // 原始伤害（未减免前；远征怪/普通怪累计）
        public int reducedDamage;   // 实际伤害（减免后扣除 gateHp）
        public int dmgRedPct;       // 减伤百分比（Lv3=10 / Lv4=15 / Lv5=20 / Lv6=25）
        public int gateLevel;       // 当前城门等级（1-6）
    }

    // ==================== §31 怪物多样性系统（🆕 v1.27）====================

    /// <summary>矿工被冰封怪冻结（type=worker_frozen）
    /// 与现有 TriggerFrozen()（固定 30s 全局）不同：个人动态时长，不触发 FrozenStatusUI 全局横幅。</summary>
    [Serializable]
    public class WorkerFrozenData
    {
        public string playerId;
        public int    duration;   // 冻结时长，毫秒（通常 5000）
    }

    /// <summary>矿工冰封解除（type=worker_unfrozen）
    /// 两种触发：冻结时间到期自动解冻 / T4 能量电池礼物强制解冻。</summary>
    [Serializable]
    public class WorkerUnfrozenData
    {
        public string playerId;
    }

    /// <summary>Boss 暴走通知（type=boss_enraged）
    /// 首领卫兵全部死亡 → Boss ATK × 1.3，服务端重算后广播新 atk 值。</summary>
    [Serializable]
    public class BossEnragedData
    {
        public int newAtk;
    }

    // ==================== 矿工HP系统 ====================

    /// <summary>矿工死亡通知（type=worker_died）</summary>
    [Serializable]
    public class WorkerDiedData
    {
        public string playerId;
        public long   respawnAt;  // Unix毫秒时间戳
        // 🔴 audit-r44 GAP-C44-01：r37 GAP-A37-03 服务端把"普通战斗"reason 从 ''→'wave' 但客户端 switch 漏 case "wave"，跨 r37→r43 共 7 轮 audit 漏检
        // 服务端实发 reason: 'wave'（普通波次战斗，r37 起，SurvivalGameEngine.js:5022）/ 'blizzard'（暴风雪夜，L7559）/ 'expedition_died'（外域阵亡，L8913/L8919/L9089）/ 'expedition_night_kia'（外域夜战阵亡）
        public string reason;
    }

    /// <summary>矿工复活通知（type=worker_revived）</summary>
    [Serializable]
    public class WorkerRevivedData
    {
        public string playerId;
    }

    /// <summary>单个矿工HP快照（worker_hp_update 数组元素）</summary>
    [Serializable]
    public class WorkerHpEntry
    {
        public string playerId;
        public int    hp;
        public int    maxHp;
        public bool   isDead;
        public long   respawnAt;
    }

    /// <summary>矿工HP全量快照（type=worker_hp_update），服务器在每次HP变化后广播</summary>
    [Serializable]
    public class WorkerHpUpdateData
    {
        public WorkerHpEntry[] workers;
    }

    // ==================== 实时贡献榜（type=live_ranking，游戏进行中推送）====================

    /// <summary>实时贡献榜单条记录</summary>
    [Serializable]
    public class LiveRankingEntry
    {
        public int    rank;
        public string playerId;
        public string playerName;
        public int    contribution;

        // §39.5 商店装备（服务端捎带推送；JsonUtility 对 null 引用会反序列化为 null，对端未下发时保持 null）
        public ShopEquipped equipped;
    }

    /// <summary>实时贡献榜（type=live_ranking）——贡献变化时服务器防抖推送</summary>
    [Serializable]
    public class LiveRankingData
    {
        public LiveRankingEntry[] rankings; // Top 5
    }

    /// <summary>单个玩家贡献更新（type=contribution_update，§34 Batch D Agent 新增）。
    /// ⚠️ audit-r24 GAP-A24-01/B24-12 补 total + source 字段（r22/r23 漏修）：
    ///   服务端 Server/src/SurvivalGameEngine.js:2110-2117 仅在 T6 礼物 +100 贡献奖励时单点 emit，
    ///   字段 `{ playerId, delta, total, source }` 4 项，**实际不是"每次 _trackContribution 后差额推送"**（r22 doc 描述失真）。
    ///   r24 之前客户端字段名 `contribution` ↔ 服务端 `total` 不匹配 → JsonUtility 反序列化时 contribution 恒为 0；
    ///   `source` 字段缺失 → T6 max_level_bonus / upgrade_bonus 差异化 UI 文案路径完全失效；
    ///   `playerName` 服务端 0 emit（保留兼容字段）。</summary>
    [Serializable]
    public class ContributionUpdateData
    {
        public string playerId;
        public string playerName;     // 客户端兼容字段（服务端 0 emit；JsonUtility 缺失字段为 null）
        public int    contribution;   // 客户端兼容字段（旧路径用；服务端实际发 total）
        public int    delta;          // 本次增量（可正可负）
        public int    total;          // 🆕 audit-r24 GAP-A24-01：服务端权威字段（当局累计贡献）；contribution 留作兼容
        public string source;         // 🆕 audit-r24 GAP-A24-01：'gift_t6_max_level_bonus' | 'gift_t6_upgrade_bonus'（仅 T6 路径 emit）
    }

    // ==================== 助威模式 §33（🆕 v1.27，PM MVP：跳过 §36.12 / §30 依赖）====================

    /// <summary>观众注册为助威者（type=supporter_joined）</summary>
    [Serializable]
    public class SupporterJoinedData
    {
        public string playerId;
        public string playerName;
        public int    supporterCount;
    }

    /// <summary>助威者弹幕生效（type=supporter_action）
    /// 🆕 audit-r25 GAP-D25-06：补齐 atkBuffPct + atkBuffTimer，让客户端 UI 在夜晚 cmd=6 时显示
    /// "助威者 +X% 攻击力（剩 Ys）"实时反馈（多个助威者协作感）。</summary>
    [Serializable]
    public class SupporterActionData
    {
        public string playerId;
        public string playerName;
        public int    cmd;            // 1/2/3/4/6/666
        public float  atkBuffPct;     // 🆕 audit-r25 GAP-D25-06：累积百分比（0~0.20，仅夜晚 cmd=6 路径有意义）
        public int    atkBuffTimer;   // 🆕 audit-r25 GAP-D25-06：剩余生效秒数（0~5，每秒服务端 -1 至 0 后清 _supporterAtkBuff）
    }

    /// <summary>AFK 替补：助威者→守护者 + 旧守护者→助威者（type=supporter_promoted）</summary>
    [Serializable]
    public class SupporterPromotedData
    {
        public string newPlayerId;
        public string newPlayerName;
        public string oldPlayerId;
        public string oldPlayerName;
        public int    workerIndex;
    }

    /// <summary>D1–D5 超员观众礼物扣费但未生效反馈（type=gift_silent_fail，🆕 v1.27）。
    /// 🔴 audit-r26 GAP-D26-07 doc 精确化：服务端 SurvivalGameEngine.js:1924 已实装 emit（r24 修过 §36.12 但本注释未同步）。
    /// 触发时机：D1-D5 supporter_mode 未解锁，第 13+ 位观众发 T1 仙女棒时抖音照常扣费、服务端不累加效果，向发送者 unicast 该消息。</summary>
    [Serializable]
    public class GiftSilentFailData
    {
        public string giftId;
        public string reason;    // "before_supporter_unlock"
        public int    unlockDay; // 6
        public int    priceFen;
    }

    // ==================== 本周贡献榜（type=weekly_ranking）====================

    /// <summary>本周贡献榜单条记录。
    /// ⚠️ audit-r24 GAP-B24-02 / POSSIBLE-D24-19 注释精确化：客户端拉取 vs 服务端主动推送两路径 Top N 不一致：
    /// - 客户端主动请求（SurvivalRoom.js:592 case 'get_weekly_ranking' 默认 n=50，最大 50）→ 拿到 Top 50
    /// - 服务端结算后主动广播（SurvivalRoom.js:411 _broadcastWeeklyRanking → getPayload(10)）→ 仅 Top 10
    /// 客户端 SurvivalRankingUI 应理解两路径数据量差异，不假定单一上限。</summary>
    [Serializable]
    public class WeeklyRankingEntry
    {
        public int    rank;
        public string playerId;
        public string nickname;
        public int    weeklyScore;
    }

    /// <summary>
    /// 本周贡献榜响应（type=weekly_ranking）
    /// 服务器主动广播（每局结算后）及客户端主动请求（面板打开时）均使用此结构
    /// </summary>
    [Serializable]
    public class WeeklyRankingData
    {
        public string               week;      // 周标识，如 "2026-W12"
        public long                 resetAt;   // 下次重置时间（Unix ms，周一 00:00 UTC+8）
        public WeeklyRankingEntry[] rankings;  // Top N（audit-r23 GAP-B23-11：默认 50，最大 50；服务端 SurvivalRoom.js:592 默认 50；客户端可在 request_weekly_ranking 携带 top 字段缩短）
    }

    // ==================== §30 矿工成长系统 ====================

    /// <summary>矿工升级通知（type=worker_level_up）</summary>
    [Serializable]
    public class WorkerLevelUpData
    {
        public string playerId;
        public string playerName;
        public int    newLevel;    // 1~100
        public int    newTier;     // 1~10
        public string skinId;      // 皮肤 ID（MVP 仅改颜色，未来换模型）
    }

    /// <summary>传奇矿工触发免死（type=legend_revive_triggered）</summary>
    [Serializable]
    public class LegendReviveData
    {
        public string playerId;
        public string playerName;
    }

    /// <summary>矿工皮肤切换通知（type=worker_skin_changed）</summary>
    [Serializable]
    public class WorkerSkinChangedData
    {
        public string playerId;
        public string playerName;
        public int    tier;        // 1~10
        public string skinId;
    }

    /// <summary>阶6 矿工触发 15% 格挡（type=worker_blocked）</summary>
    [Serializable]
    public class WorkerBlockedData
    {
        public string playerId;
        public string playerName;
    }

    // ==================== §24.4 主播事件轮盘（Broadcaster Event Roulette，🆕 v1.27）====================

    /// <summary>轮盘充能就绪通知（type=broadcaster_roulette_ready）
    /// 🆕 audit-r25 GAP-D25-05：补 source 字段，区分"正常 300s 充能完成"vs"§38.3 神秘符文事件瞬间补满"。
    /// 服务端 SurvivalGameEngine.js:8696-8702 mystic_rune case emit `source='mystic_rune'`；正常充能完成无 source（默认 ""）。</summary>
    [Serializable]
    public class RouletteReadyData
    {
        public long   readyAt;    // Unix ms；-1 表示已就绪
        public string source;     // 🆕 audit-r25 GAP-D25-05：'mystic_rune' | ''（默认空字符串）
    }

    /// <summary>轮盘抽卡结果（type=broadcaster_roulette_result）
    /// 服务端已在 spin 时确定 cardId，客户端转轴动画仅为表演。
    /// displayedCards 固定长度 3，中间（index 1）为定格卡。</summary>
    [Serializable]
    public class RouletteResultData
    {
        public string   cardId;           // 定格卡 ID（elite_raid/time_freeze/double_contrib/mystery_trader/meteor_shower/aurora）
        public string[] displayedCards;   // 长度 3，展示给主播的 3 张卡
        public long     spunAt;           // spin 发生时刻（Unix ms，用于兜底 autoApplyAt 计算）
        public long     autoApplyAt;      // 🔴 audit-r37 GAP-E37-07：未主动 apply 时的 10s 兜底自动 apply 时刻（Unix ms，= spunAt + 10000）— 服务端 ROULETTE_AUTO_APPLY_MS = 10*1000
    }

    /// <summary>轮盘效果结束通知（type=broadcaster_roulette_effect_ended）</summary>
    [Serializable]
    public class RouletteEffectEndedData
    {
        public string cardId;
    }

    /// <summary>神秘商人交易卡（2 选 1 中单张）</summary>
    [Serializable]
    public class TraderCard
    {
        public int costFood;  public int costCoal;  public int costOre;
        public int gainFood;  public int gainCoal;  public int gainOre;
        public int gainGateHp;
    }

    /// <summary>神秘商人交易邀约（type=broadcaster_trader_offer）——30s 限时二选一，超时自动弃权。</summary>
    [Serializable]
    public class TraderOfferData
    {
        public TraderCard cardA;
        public TraderCard cardB;
        public long       expiresAt;   // Unix ms，到期服务端自动视作弃权
    }

    /// <summary>神秘商人交易结果(type=broadcaster_trader_result,整体复核 Critical #3 修复)</summary>
    [Serializable]
    public class BroadcasterTraderResultData
    {
        public bool   success;
        public string choice;    // 'A'|'B'|'' (timeout 时空)
        public string reason;    // 'ok'|'insufficient_resource'|'timeout'
    }

    // ==================== §38 探险系统（Expedition System，🆕 v1.27）====================

    /// <summary>探险开始通知（type=expedition_started）
    /// 服务端广播：某矿工已出发探险，客户端隐藏其模型并在地图边缘显示小图标。</summary>
    [Serializable]
    public class ExpeditionStartedData
    {
        public string playerId;
        public int    workerIdx;     // _activeWorkers 中该 Worker 的索引（兜底用，首选按 playerId 查）
        public string expeditionId;
        public long   returnsAt;     // Unix ms（未考虑 bandit_raid / 加速回程的最初 ETA）
    }

    /// <summary>探险外域事件（type=expedition_event）
    /// 探险到达 40s 时服务端随机触发，15s 时限内由主播决议（trader_caravan）或直接结算。</summary>
    [Serializable]
    public class ExpeditionEventData
    {
        public string   expeditionId;
        public string   eventId;     // 'lost_cache'/'wild_beasts'/'trader_caravan'/'meteor_fragment'/'bandit_raid'/'mystic_rune'
        public long     eventEndsAt; // Unix ms（= 发送时刻 + 15s）
        public string[] options;     // 仅 trader_caravan 非空（['accept','cancel']），其他 eventId 序列化为 null / 空数组
    }

    /// <summary>探险返回通知（type=expedition_returned）
    /// 35s 返程结束后广播，客户端恢复矿工模型（或切 Dead 状态）。</summary>
    [Serializable]
    public class ExpeditionReturnedData
    {
        public string            playerId;
        public string            expeditionId;
        public ExpeditionOutcome outcome;
    }

    /// <summary>探险结算详情（ExpeditionReturnedData.outcome 子对象）。
    /// ⚠️ audit-r24 GAP-E24-02 注释精确化（r23 GAP-E23-03 半成品延续 — r23 修了 SurvivalGameManager.cs:2562 注释，
    /// 但漏修本 Data 类注释）：'safe' 仅在 §34.4 E3b 不朽证明 _consumeFreeDeathPass 救回路径 emit（夜晚兜底专用），
    /// 不是"无事件触发的中性归来"（_wildBeasts/_meteor/_mysticRune 等无事件分支全 emit 'success'/'died'/'empty'）。</summary>
    [Serializable]
    public class ExpeditionOutcome
    {
        // 'success'：探险正常成功带回资源 / 'died'：矿工死亡 / 'empty'：空回（无收获）/
        // 'safe'：仅在 §34.4 E3b 不朽证明 _consumeFreeDeathPass 救回路径 emit（夜晚兜底专用，单点）
        public string         type;
        public ResourceBundle resources;     // 可为 null（empty/died/safe 时）
        public int            contributions; // 未产生贡献时为 0
        public bool           died;
    }

    /// <summary>协议层资源三元组 {food, coal, ore}（§38 首次定义）</summary>
    [Serializable]
    public class ResourceBundle
    {
        public int food;
        public int coal;
        public int ore;
    }

    /// <summary>探险拒绝通知（type=expedition_failed）
    /// 服务端拒绝 send/recall 时返回，客户端显示跑马灯提示原因。
    /// ⚠️ audit-r24 GAP-D24-06/E24-09 reason 注释精确化：r24 之前注释列 10 项 reason，但 'supporter' / 'already_expedition' /
    /// 'over_limit' 三项**服务端 0 emit**（grep `_startExpedition` 返回值），客户端 case 分支永远不触发，是 r19/r20 占位词残留。</summary>
    [Serializable]
    public class ExpeditionFailedData
    {
        public string playerId;
        public string workerId;    // 被拒的 Worker 索引/id（服务端 workerIdx 字符串化，空字符串为默认）
        // 服务端实际 emit 7 项 reason：
        //   'feature_locked'（D5 前未解锁）/ 'wrong_phase'（仅 day/recovery 可派遣）/ 'season_ending'（D7 终章禁止）/
        //   'supporter_not_allowed'（助威者不可发起）/ 'duplicate'（同 Worker 已在探险）/
        //   'max_concurrent'（同时 ≥ 2 探险）/ 'worker_dead'（Worker 已死亡）
        // 客户端兜底防御别名（FailureToastLocale 兼容旧实装名）：'supporter'/'already_expedition'/'over_limit' — 服务端 0 emit
        public string reason;
        public int    unlockDay;   // 仅 reason='feature_locked' 时有效；其他场景序列化为 0
    }

    // ==================== §37 建造系统（Building System，🆕 v1.27）====================
    // 协议详见 §37.6；所有消息走 SurvivalGameManager.HandleMessage 分发。
    // ⚠️ 协议口径：BuildVoteUpdateData 使用"并行数组"（voteBuildIds / voteCounts）而非 map，
    //   因 Unity JsonUtility 不支持 Dictionary<string,int> 反序列化。
    //   需后端对齐：广播 build_vote_update 时序列化为 { proposalId, voteBuildIds:["watchtower","market"],
    //   voteCounts:[3,1], totalVoters:4 }；若后端仍发 { votes:{buildId:count} } 映射则客户端解析为空——
    //   本次按"并行数组"实现，后端对齐前 UI 将显示为 0 票（Reviewer 会标 gap）。

    /// <summary>投票开始（type=build_vote_started）</summary>
    [Serializable]
    public class BuildVoteStartedData
    {
        public string   proposalId;
        public string   proposerName;
        public string[] options;        // 长度 1~3（单项退化场景）
        public long     votingEndsAt;   // Unix ms
    }

    /// <summary>投票实时更新（type=build_vote_update）
    /// ⚠️ 并行数组格式：voteBuildIds[i] 与 voteCounts[i] 按索引对应；
    ///   JsonUtility 不支持 Dictionary，后端须配合此格式。</summary>
    [Serializable]
    public class BuildVoteUpdateData
    {
        public string   proposalId;
        public string[] voteBuildIds;   // 并列键：['watchtower','market',...]
        public int[]    voteCounts;     // 并列值：[3,1,...]
        public int      totalVoters;
    }

    /// <summary>投票结束（type=build_vote_ended）
    /// winnerId 可为 null（0 票流产时），服务端随后不广播 build_started。</summary>
    [Serializable]
    public class BuildVoteEndedData
    {
        public string proposalId;
        public string winnerId;         // 可为 null（0 票流产）
        public int    totalVoters;
    }

    /// <summary>JSON 友好的 Vector3 序列化结构（§37 首次引入，其他系统复用）</summary>
    [Serializable]
    public class Vector3Data
    {
        public float x;
        public float y;
        public float z;
    }

    /// <summary>建造开始（type=build_started）</summary>
    [Serializable]
    public class BuildStartedData
    {
        public string      buildId;
        public long        completesAt;   // Unix ms
        public Vector3Data position;
    }

    /// <summary>建造进度刷新（type=build_progress）
    /// 服务端在建造中周期下发；progress ∈ [0,1]。后端若未发本消息，
    /// 客户端可从 BuildStartedData.completesAt 自行线性插值做动画。</summary>
    [Serializable]
    public class BuildProgressData
    {
        public string buildId;
        public float  progress;       // 0.0 ~ 1.0
        public long   completesAt;    // Unix ms，冗余字段供校准倒计时
        public long   remainingMs;    // audit-r4 §19.2：服务端每 2s 下发剩余毫秒，UI 可直接显示倒计时
    }

    /// <summary>建造完成（type=build_completed）</summary>
    [Serializable]
    public class BuildCompletedData
    {
        public string buildId;
    }

    /// <summary>单个建筑拆除（type=build_demolished）</summary>
    [Serializable]
    public class BuildDemolishedData
    {
        public string buildId;
        public string reason;           // 'manual'/'attacked'/'demoted'/'demoted_during_build'
    }

    /// <summary>建造提议被拒（type=build_propose_failed）
    /// reason 枚举：insufficient_resource / already_exists / wrong_phase / feature_locked / daily_limit /
    ///   insufficient_resources / already_built / already_voting / supporter_not_allowed / season_ending（完整枚举见 §19.2 / §37.3）</summary>
    [Serializable]
    public class BuildProposeFailedData
    {
        public string buildingType;     // 被拒的建筑类型（watchtower / market / hospital / altar / beacon）
        public string reason;           // 完整枚举见 §19.2 / §37.3
        public int    unlockDay;        // 仅 reason='feature_locked' 时有效
    }

    /// <summary>投票通过但扣费失败导致建造取消（type=build_cancelled）</summary>
    [Serializable]
    public class BuildCancelledData
    {
        public string buildId;
    public string reason;           // 'insufficient_resources_at_finalize'
    }

    /// <summary>失败降级批量拆除（type=building_demolished_batch）</summary>
    [Serializable]
    public class BuildingDemolishedBatchData
    {
        public string[] buildingIds;
        public string   reason;         // 当前固定为 'demoted'
    }

    /// <summary>瞭望塔 10s 预告怪物波次（type=monster_wave_incoming）。
    /// ⚠️ audit-r24 GAP-E24-01 补 leadSec 字段：
    ///   两路径 emit：watchtower 路径（SurvivalGameEngine.js:4570-4577 / :4735-4744）emit 3 字段；
    ///   emergency_alert 路径（:9434-9439）emit 4 字段含 leadSec（10s/30s 区分有无瞭望塔）。
    ///   r24 之前客户端 MonsterWaveIncomingData 仅 3 字段缺 leadSec，emergency_alert 时 UI 走 fallback metadata.leadSec
    ///   兜底，体验失真。</summary>
    [Serializable]
    public class MonsterWaveIncomingData
    {
        public int  waveIndex;
        public long spawnsAt;           // Unix ms
        public long firstAttackAt;      // Unix ms ≈ spawnsAt + 3500
        public int  leadSec;            // 🆕 audit-r24 GAP-E24-01：emergency_alert 路径 30s + watchtower 路径 10s（PREVIEW_LEAD_MS）— r25 起两路径均携带 leadSec（GAP-E37-24 注释精确化）
    }

    // ==================== 主播排行榜（type=streamer_ranking）====================

    /// <summary>主播排行榜单条记录（v1.26 重构，audit-r4 补齐客户端字段）
    /// 服务端 StreamerRankingStore 已升至 v1.26（maxFortressDay / totalCycles / streamerKingTitle），
    /// 客户端需同步字段才能显示"堡垒之王"称号 + 总周期数统计。
    /// v1.25 字段（maxDays/totalWins/totalGames/score）保留向下兼容；v1.27 §14 难度系统废止，maxDifficulty 已删除。</summary>
    [Serializable]
    public class StreamerRankingEntry
    {
        public int    rank;
        public string streamerId;
        public string streamerName;      // 主播名

        // v1.26 新字段（audit-r4 补齐）
        public int    maxFortressDay;    // 堡垒日最大值（主排序键）
        public int    totalCycles;       // 总周期数（累计开播局数）
        public string streamerKingTitle; // "堡垒之王" / "" (Top1 持有者)

        // v1.25 兼容字段（服务端 addGameResult 向下写入）
        // v1.27 §14 废止：maxDifficulty 已删除（difficulty 三档系统不再存在）
        public int    maxDays;           // 最多坚持天数（最佳完成局）
        public int    totalWins;         // 总胜场数
        public int    totalGames;        // 总场次
        public int    score;             // 排名得分（v1.25 含 difficulty_weight × maxDays，v1.27 后纯 maxDays 系数）
    }

    /// <summary>主播排行榜响应（type=streamer_ranking）</summary>
    [Serializable]
    public class StreamerRankingData
    {
        // 🔴 audit-r25 GAP-C25-03/06：双路径上限不一致（与 r24 GAP-B24-02 weekly_ranking 同模式延续 — 半成品延续第 8 轮）
        //   - 服务端广播 _broadcastStreamerRanking(SurvivalRoom.js:430)：getPayload(10) 仅 Top 10
        //   - 客户端请求 get_streamer_ranking(SurvivalRoom.js:608)：默认 50/最大 50（data.top<=50?data.top:50）
        //   - StreamerRankingStore.js:208 getTop(n=10) 函数级默认 10 但入口默认 50
        //   - 客户端 SurvivalRankingUI.cs:285 RequestStreamerRanking() 不带 top 参数 → 实际拿 Top 50
        public StreamerRankingEntry[] rankings; // Top N（广播默认 10；客户端请求默认 50，最大 50）
    }

    // ==================== §39 商店系统（Shop System，🆕 v1.27） ====================
    // 协议见策划案 §39.9（C→S 4 + S→C 8 = 12 个新协议 + LiveRankingEntry.equipped 扩展字段）。
    // 客户端 MVP：仅脚本，Prefab 绑定留给人工。

    /// <summary>单个商品定义（§39.2 A/B 两类清单）</summary>
    [Serializable]
    public class ShopItem
    {
        public string itemId;             // 'worker_pep_talk' / 'title_supporter' / ...
        public string name;               // 中文名
        public int    price;              // 价格（A 类用 contributions，B 类用 _contribBalance）
        public string slot;               // B 类才有 'title' | 'frame' | 'entrance' | 'barrage'；A 类为 null
        public string category;           // 'A' | 'B'
        public string effect;             // 效果描述（前端直接显示）
        public int    minLifetimeContrib; // 赛季限定 SKU 才有，默认 0
        public string limitedSeasonId;    // 赛季限定 SKU 才有；非限定为 null
    }

    /// <summary>商品清单响应（type=shop_list_data，§39.9）</summary>
    [Serializable]
    public class ShopListData
    {
        public string     category;   // 'A' | 'B'
        public ShopItem[] items;
    }

    /// <summary>B 类 ≥1000 主播 HUD 购买触发双确认弹窗（type=shop_purchase_confirm_prompt，§39.7）
    /// 🔴 audit-r37 GAP-C37-15：补 playerId 字段（服务端 SurvivalGameEngine.js:9234-9237 emit 含 playerId）
    ///   旧版客户端 Data 类缺该字段 → 多人房间无法精确路由弹窗到发起人客户端</summary>
    [Serializable]
    public class ShopPurchaseConfirmPromptData
    {
        public string playerId;    // 🔴 audit-r37 GAP-C37-15：弹窗发起人 openId，主播触发时为 broadcaster openId
        public string pendingId;   // 一次性凭证 UUID
        public string itemId;
        public int    price;
        public long   expiresAt;   // Unix ms（5s TTL）
    }

    /// <summary>购买成功，房间广播（type=shop_purchase_confirm，§39.9）</summary>
    [Serializable]
    public class ShopPurchaseConfirmData
    {
        public string playerId;
        public string playerName;
        public string itemId;
        public string category;         // 'A' | 'B'
        public int    remainingContrib; // 本局剩余贡献（A 类扣费后）
        public int    remainingBalance; // 终身余额（B 类扣费后）
    }

    /// <summary>购买失败，仅回发起方（type=shop_purchase_failed，§39.11）</summary>
    [Serializable]
    public class ShopPurchaseFailedData
    {
        public string reason;             // insufficient / wrong_phase / no_effect / feature_locked / item_not_found / already_owned / pending_expired / pending_invalid / limit_exceeded / supporter_not_allowed / not_unlocked_yet / season_locked / spotlight_active / per_game_limit
        public string itemId;
        public int    unlockDay;          // 仅 feature_locked / not_unlocked_yet 时有效
        public int    minLifetimeContrib; // 仅 not_unlocked_yet 时有效
    }

    /// <summary>装备切换成功，unicast 发起方（type=shop_equip_changed，§39.5）</summary>
    [Serializable]
    public class ShopEquipChangedData
    {
        public string playerId;
        public string slot;      // 'title' | 'frame' | 'entrance' | 'barrage'
        public string itemId;    // null 表示卸下
    }

    /// <summary>装备切换失败，unicast 发起方（type=shop_equip_failed）</summary>
    [Serializable]
    public class ShopEquipFailedData
    {
        public string reason;    // not_owned / slot_mismatch / too_frequent
        public string slot;
        public string itemId;
    }

    /// <summary>玩家当前装备的 4 槽位（§39.5，LiveRankingEntry.equipped 字段与 ShopInventoryData.equipped 共用）</summary>
    [Serializable]
    public class ShopEquipped
    {
        public string title;
        public string frame;
        public string entrance;
        public string barrage;
    }

    /// <summary>背包/装备快照，进房或重连时推送（type=shop_inventory_data）
    /// 🔴 audit-r37 GAP-C37-01/E37-13/E37-16：补 contribBalance + lifetimeContrib 字段
    ///   服务端 SurvivalGameEngine.js:9657-9658（handleShopInventory）+ player_joined 路径（r37 补齐）已 emit
    ///   客户端旧版 Data 类仅 3 字段，JsonUtility 反序列化时静默丢弃 → ShopUI 重连后无法显示玩家"剩余可用积分"和"终身累计贡献"
    ///   r37 加字段让 §39.5 ShopUI 余额显示可用</summary>
    [Serializable]
    public class ShopInventoryData
    {
        public string       playerId;
        public string[]     owned;
        public ShopEquipped equipped;
        public int          contribBalance;    // 🔴 audit-r37 GAP-C37-01：当前可消费余额（A 类货币）
        public long         lifetimeContrib;   // 🔴 audit-r37 GAP-C37-01：终身累计贡献（B 类货币 / 装备购买基线）
    }

    /// <summary>A 类效果元数据（随 shop_effect_triggered 下发的附加信息）</summary>
    [Serializable]
    public class ShopEffectMetadata
    {
        public int gateHpBefore;
        public int gateHpAfter;
        public int waveIdx;
        public int leadSec;
    }

    /// <summary>A 类购买成功后的视觉/行为效果事件，房间广播（type=shop_effect_triggered）</summary>
    [Serializable]
    public class ShopEffectTriggeredData
    {
        public string             itemId;
        public string             sourcePlayerId;
        public string             sourcePlayerName;
        public string             targetPlayerId;  // spotlight 可能为空；其余通常 == sourcePlayerId
        public int                durationSec;     // 0 表示瞬时
        public ShopEffectMetadata metadata;
    }

    // ==================== §35 跨直播间攻防战(Tribe War,🆕 v1.27) ====================
    // 协议见策划案 §35.10 / §19；客户端 MVP P1：仅脚本，Prefab 绑定留给人工。
    //
    // ⚠️ 字段命名对齐备忘：协议层事件键 C# 端用 `eventName`（C# `event` 是关键字）。
    //    服务端 tribe_war_combat_report / tribe_war_combat_report_defense 须以 `eventName`
    //    作为字段名下发（策划案 §35.10 / §19 文案中 "type" 为描述，wire format 与前端统一）。
    //    若后端仍用 `event`，Unity JsonUtility 将无法填充此字段，需后端对齐。

    /// <summary>攻防战大厅列表中的单个房间条目（tribe_war_room_list_result.rooms[i]）。
    /// MVP 字段子集：只保留大厅选人最小必要集（roomId/streamerName/state/day/underAttack/attackable）。
    /// 其它后端字段（difficulty/playerCount/gateHpPct）在主版可补。
    /// 🔴 audit-r37 GAP-C37-03：补 fortressDay 字段（服务端 TribeWarManager.js:56-65 emit 含 fortressDay）
    ///   r36 GAP-C36-05 已 doc 同步但漏修客户端 Data 类，r37 真闭环 — TribeWarLobbyUI 可显示堡垒日</summary>
    [Serializable]
    public class TribeWarRoomInfo
    {
        public string roomId;
        public string streamerName;
        public string state;         // 'day' | 'night' | 'recovery' | 其它
        public int    day;
        public int    fortressDay;   // 🔴 audit-r37 GAP-C37-03：堡垒日（§36 同步），TribeWarLobbyUI 显示用
        public bool   underAttack;   // 当前是否正在被其他房间攻击
        public bool   attackable;    // 是否可作为目标（综合自我限制/互斥判定，由服务端给出）
    }

    /// <summary>攻防战大厅列表应答(type=tribe_war_room_list_result)</summary>
    [Serializable]
    public class TribeWarRoomListResultData
    {
        public TribeWarRoomInfo[] rooms;
    }

    /// <summary>攻击/反击失败（type=tribe_war_attack_failed，unicast 发起方）。
    /// reason 枚举参见策划案 §35.10 / §19.2：cannot_attack_self / in_cooldown / already_attacking /
    /// target_already_under_attack / target_unavailable / target_not_playing / not_under_attack /
    /// wrong_phase / feature_locked / target_offline / self_room 等。</summary>
    [Serializable]
    public class TribeWarAttackFailedData
    {
        public string targetRoomId;   // 目标房间 id（上下文关联），服务端未下发时为空字符串
        public string reason;
        // §35 P2 冷却：reason='in_cooldown' 时附带剩余冷却毫秒数（SurvivalRoom 透传 TribeWarManager 值）
        public long   cooldownMs;
        // 🆕 v1.27 §36.12：reason='feature_locked' 时附带解锁所需赛季日（D7 解锁 tribe_war）
        public int    unlockDay;
    }

    /// <summary>攻击开始广播（type=tribe_war_attack_started，双方房间均广播，UI 根据自身 roomId 判断攻/守视角）</summary>
    [Serializable]
    public class TribeWarAttackStartedData
    {
        public string sessionId;
        public string attackerRoomId;
        public string attackerStreamerName;
        public string defenderRoomId;
        public string defenderStreamerName;
    }

    /// <summary>被攻击通知（type=tribe_war_under_attack，仅防守方房间广播，客户端展示被攻击横幅+状态面板）</summary>
    [Serializable]
    public class TribeWarUnderAttackData
    {
        public string sessionId;
        public string attackerRoomId;
        public string attackerStreamerName;
    }

    /// <summary>远征怪已派出（type=tribe_war_expedition_sent，仅攻击方房间广播，更新状态面板派出数/能量消耗）</summary>
    [Serializable]
    public class TribeWarExpeditionSentData
    {
        public string sessionId;
        public int    count;
        public int    remainingEnergy;
    }

    /// <summary>远征怪来袭（type=tribe_war_expedition_incoming，仅防守方房间广播，
    /// 客户端调 MonsterWaveSpawner.SpawnTribeWarExpedition 渲染红色远征怪）</summary>
    [Serializable]
    public class TribeWarExpeditionIncomingData
    {
        public string sessionId;
        public int    count;
        public string attackerStreamerName;
        public float  damageMultiplier;
        public string[] monsterIds;
        public TribeWarExpeditionMonsterData[] monsters;
    }

    [Serializable]
    public class TribeWarExpeditionMonsterData
    {
        public string monsterId;
        public int    hp;
        public int    atk;
    }

    /// <summary>战报条目（type=tribe_war_combat_report / tribe_war_combat_report_defense，双方视角各自广播）。
    /// <c>eventName</c> 对应策划案 §35.10 "type" 字段（damage/worker_killed/gate_hit/expedition_killed/resource_stolen/gate_bonus）；
    /// detail 是前端友好的一行文案，MVP 直接渲染到战报滚动区。
    /// ⚠️ 需后端对齐字段名 <c>eventName</c>（不用 C# 保留字 <c>event</c>）。</summary>
    [Serializable]
    public class TribeWarCombatReportData
    {
        public string sessionId;
        public string eventName;   // damage / worker_killed / gate_hit / expedition_killed / resource_stolen / gate_bonus
        public string detail;
    }

    /// <summary>攻击结束广播（type=tribe_war_attack_ended,双方房间均广播）。
    /// reason 枚举：manual_stop / zero_energy_timeout / game_ended / season_ended。
    /// stolen* 为本次会话累计被偷取的资源量（防守方视角 = 本房间被偷走的量）。</summary>
    [Serializable]
    public class TribeWarAttackEndedData
    {
        public string sessionId;
        public string reason;
        public int    stolenFood;
        public int    stolenCoal;
        public int    stolenOre;
    }

    // ==================== §17.15 新手引导气泡（🆕 v1.27） ====================

    /// <summary>新手引导气泡 B1–B3 连播触发（type=show_onboarding_sequence，S→C）。
    /// 服务端 5 分钟节流 + UUID 幂等：相同 sessionId 客户端只播放一次。</summary>
    [Serializable]
    public class ShowOnboardingSequenceData
    {
        public string   sessionId;   // 服务端 crypto.randomUUID()，用于客户端幂等
        public int      priority;    // 固定 2（normal），UI 引导非关键消息
        public int      seasonDay;   // 触发时的赛季日（1-7），UI 可据此筛选气泡
        public int      fortressDay; // 触发时的堡垒日，UI 可据此做进度提示
        public string[] bubbleIds;   // 连播的气泡 id 列表（如 ["B1","B2","B3"]），老服务端缺失时为 null
    }

    // ==================== §36 全服同步 + 赛季制（Global Sync + Season，🆕 v1.27） ====================
    // 协议详见策划案 §36.9 / §19.2；客户端 MVP 极简版：仅脚本,Prefab 绑定留给人工。
    //
    // 客户端 MVP 范围：world_clock_tick / season_state / fortress_day_changed / room_failed /
    //                  season_started(映射 season_state) / season_settlement
    // 协议字段对齐说明：
    //  - world_clock_tick 每秒 1 次由服务端广播（见 §36.9）,携带 phase/seasonDay/themeId/phaseRemainingSec。
    //  - fortress_day_changed 的 reason 枚举包含 'promoted'/'demoted'/'newbie_protected'/'cap_blocked'/'cap_reset'（见 §36.5.1）。
    //  - FortressDayChangedData.daily* 4 字段由 §36.5.1 扩展；服务端未开启 cap flag 时 dailyCapMax 可能为 0。
    //  - RoomFailedData.demotionReason 与 §16.1 失败规则 + survival_game_ended.reason 统一枚举。

    /// <summary>全服时钟每秒广播（type=world_clock_tick）</summary>
    [Serializable]
    public class WorldClockTickData
    {
        public string phase;              // "day" | "night"
        public int    seasonDay;          // 1-7
        public int    seasonId;           // 当前赛季 id（int 表示；服务端可能发字符串 id，此处保持 MVP 简化版）
        public string themeId;            // classic_frozen / blood_moon / snowstorm / dawn / frenzy / serene
        public int    phaseRemainingSec;  // 本阶段剩余秒数
        // 🆕 v1.27 §36.12 分时段解锁：仅在 seasonDay 从 N→N+1 递增的那一秒服务端携带；
        //   其他 tick 服务端不下发（JsonUtility 反序列化默认为 null）。
        //   feature id 常量见 SurvivalMessageProtocol.FeatureXxx。
        public string[] newlyUnlockedFeatures;
    }

    /// <summary>赛季状态快照（type=season_state，连接/主动请求时推送）</summary>
    [Serializable]
    public class SeasonStateData
    {
        public int    seasonId;
        public int    seasonDay;
        public string themeId;
        // 🆕 v1.27 §36.12 分时段解锁：截至当前 seasonDay 的已解锁功能 id 全集（与 FEATURE_UNLOCK_DAY 一致）。
        //   客户端据此初始化 BroadcasterPanel 按钮灰化/🔒 状态，避免错过 world_clock_tick.newlyUnlockedFeatures 的中途进场永久不知解锁状态。
        //   老用户豁免时服务端返全集。
        public string[] unlockedFeatures;
    }

    /// <summary>堡垒日变更（type=fortress_day_changed，挺过/降级/新手保护/cap_blocked/cap_reset 后推送）</summary>
    [Serializable]
    public class FortressDayChangedData
    {
        public int    oldFortressDay;
        public int    newFortressDay;
        public string reason;                    // 'promoted'/'demoted'/'newbie_protected'/'cap_blocked'/'cap_reset'
        public int    seasonDay;
        // §36.5.1 每日闯关上限扩展 4 字段（服务端未启用 cap flag 时 dailyCapMax 可能为 0）
        public int    dailyFortressDayGained;
        public int    dailyCapMax;
        public long   dailyResetAt;              // Unix ms，下次 UTC+8 05:00
        public bool   dailyCapBlocked;
    }

    /// <summary>房间失败降级补充数据（type=room_failed，与 fortress_day_changed 同帧推送）</summary>
    [Serializable]
    public class RoomFailedData
    {
        public int    oldFortressDay;
        public int    newFortressDay;
        public string demotionReason;            // 'gate_breached'/'food_depleted'/'temp_freeze'/'all_dead'
        public bool   newbieProtected;
    }

    /// <summary>赛季开始（type=season_started，MVP 占位；服务端若使用 season_state 替代时此消息可不下发）</summary>
    [Serializable]
    public class SeasonStartedData
    {
        public int    seasonId;
        public int    seasonDay;
        public string themeId;
    }

    /// <summary>赛季结算单条玩家贡献（跨房间 Top10）</summary>
    [Serializable]
    public class SeasonTopContributorEntry
    {
        public string playerId;
        public string playerName;
        public int    contribution;
    }

    /// <summary>赛季结算（type=season_settlement，D7 夜晚结束或 Boss 池归零后广播）
    /// audit-r4 补齐 survivingRooms / topContributors[] — 服务端 SeasonManager.advanceDay
    /// 在 season_settlement 广播中携带这两个字段（L174-176），客户端需接收并渲染</summary>
    [Serializable]
    public class SeasonSettlementData
    {
        public int    seasonId;
        public string themeId;
        public int    nextSeasonId;
        public string nextThemeId;
        public int    survivingRooms;                         // 当前赛季末 fortressDay>0 的房间数
        public SeasonTopContributorEntry[] topContributors;   // 跨房间 Top10 贡献榜
    }

    // ==================== §36.4 赛季 Boss Rush（🆕 v1.27） ====================

    /// <summary>赛季 Boss Rush 启动（type=season_boss_rush_start，D7 夜晚开始时广播）</summary>
    [Serializable]
    public class BossRushStartedData
    {
        public int      seasonId;
        public int      bossHpTotal;          // 全服 Boss 血量池（=5000 × activeRooms）
        public string[] participatingRooms;   // 注册房间 roomId 快照
        public string   nextThemeId;          // 下一赛季主题预告（D7 夜晚开始时就已决定）
    }

    /// <summary>赛季 Boss Rush 已击杀（type=season_boss_rush_killed，全服 HP 池归零时去重广播一次）</summary>
    [Serializable]
    public class BossRushKilledData
    {
        public int  seasonId;
        public long killedAt;   // Unix ms
    }

    // ==================== §36.12 分时段解锁 —— 老用户豁免（🆕 v1.27） ====================

    /// <summary>老用户豁免（type=veteran_unlocked，玩家首次达标时推送）</summary>
    [Serializable]
    public class VeteranUnlockedData
    {
        public string openId;
        public string reason;   // 'lifetime_contrib' / 'fortress_day' / 'seasons_completed'
    }

    // ==================== §36.12 分时段解锁 —— broadcaster_action_failed（🆕 v1.27） ====================

    /// <summary>主播 ⚡加速/🌊事件触发失败（type=broadcaster_action_failed，unicast 发起方）
    /// reason 枚举：feature_locked / in_cooldown / wrong_phase</summary>
    [Serializable]
    public class BroadcasterActionFailedData
    {
        public string action;     // 子动作名 'efficiency_boost' / 'trigger_event' / 'upgrade_gate' / 'spin' / 'event' / ...
        public string reason;     // 失败原因
        public long   cooldownMs; // 仅 reason='in_cooldown' 时有效（剩余冷却毫秒数）
        public int    unlockDay;  // 仅 reason='feature_locked' 时有效（§36.12 broadcaster_boost.minDay=2）
    }

    // ==================== §34 Layer 3 组 C 体验引擎 ====================
    // E1 TensionOverlay / E3 GloryMoment & CoopMilestone / E4 GiftImpact & Recommendation
    // 协议：resource_update 捎带 tension/totalContribution/giftRecommendation；
    //      glory_moment / coop_milestone / gift_impact 为独立消息。

    /// <summary>§34 E4 精准付费：资源缺口时服务端附加的礼物推荐（resource_update.giftRecommendation）。
    /// 缺失字段 JsonUtility 反序列化为 null（整块），不触发 UI。</summary>
    [Serializable]
    public class GiftRecommendationData
    {
        public string giftId;
        public string reason;
        public string urgency;  // "gentle" | "medium" | "high" | "critical"
    }

    /// <summary>§34 E3a 荣耀时刻（type=glory_moment）：礼物触发的高光瞬间横幅。
    /// overtaken 后端可发 null，JsonUtility 将 string 字段解为空字符串而非 null，客户端按空字符串判空。</summary>
    [Serializable]
    public class GloryMomentData
    {
        public string playerId;
        public string playerName;
        public string giftName;
        public int    giftTier;
        public int    rank;
        public int    gapToFirst;
        public bool   isNewFirst;
        public string overtaken;  // nullable; 后端传 null 时 JsonUtility 解为空字符串
    }

    /// <summary>§34 E3b 合作里程碑（type=coop_milestone）：全服累计贡献达到阈值时触发。
    /// 阈值档：unity(500) / steel_will(2000) / miracle(5000) / legend(10000) / immortal(20000)。
    /// nextTarget ≤ 0 表示已封顶（immortal 之后无进一步阈值）。</summary>
    [Serializable]
    public class CoopMilestoneData
    {
        public string id;          // "unity" | "steel_will" | "miracle" | "legend" | "immortal"
        public string name;
        public string desc;
        public int    total;          // 本次达成阈值（500/2000/5000/10000/20000）
        public int    currentTotal;   // 达成时全服累计贡献
        public int    nextTarget;     // 0 或负数代表封顶
    }

    /// <summary>§34 E4 礼物影响详情（type=gift_impact）：点亮付费动机的即时反馈。
    /// privateOnly=true 时（fairy_wand）仅发送者本人看到；其余礼物房间广播。</summary>
    [Serializable]
    public class GiftImpactData
    {
        public string playerId;
        public string playerName;
        public string giftId;
        public string giftName;
        public string impacts;
        public bool   privateOnly;    // fairy_wand=true，其余 false
    }

    // ==================== §34 Layer 2 组 B 数据流可视化（🆕 v1.27）====================
    // 协议：settlement_highlights / streamer_skip_settlement(C→S) / efficiency_race / day_preview。
    // random_event 沿用现有 RandomEventData（B3 仅扩展 eventId 枚举，前端 fallback 兜底）。

    /// <summary>§34 B2 结算"最戏剧性事件"嵌套对象（settlement_highlights.mostDramaticEvent）。
    /// 缺失时 JsonUtility 反序列化为 null 整块。</summary>
    [Serializable]
    public class DramaticEventData
    {
        public string type;   // 'tension_drop' / 'boss_killed' / 'gate_almost_broken' / ...
        public string desc;   // 前端友好文案，MVP 阶段直接渲染
        public int    day;    // 事件发生在第几天
    }

    /// <summary>§34 B2 结算高光数据（type=settlement_highlights，S→C）。
    /// 服务端在 survival_game_ended 之后、结算序列开始前推送；客户端缓存最近一次，
    /// 在 PlaySettlementSequence 的帧 A（高光时刻）阶段渲染。
    /// closestCallHpPct 范围 0-1；mostDramaticEvent 可为 null（JsonUtility 整块 null）。</summary>
    [Serializable]
    public class SettlementHighlightsData
    {
        public int               dayOrSeasonId;
        public string            topDamagePlayerId;
        public string            topDamagePlayerName;
        public int               topDamageValue;
        public string            bestRescueGiftId;
        public string            bestRescueGiftName;
        public string            bestRescuePlayerName;
        public DramaticEventData mostDramaticEvent;
        public float             closestCallHpPct;
        public int               closestCallDay;
    }

    /// <summary>§34 B10a 安全期效率竞赛单条（efficiency_race.top3[i]）</summary>
    [Serializable]
    public class EfficiencyRaceEntry
    {
        public int    rank;           // 1-3
        public string playerId;
        public string playerName;
        public int    contribution;
    }

    /// <summary>§34 B10a 安全期效率竞赛（type=efficiency_race，S→C）。
    /// 服务端仅在 tension &lt; 30 的白天定期推送（~15s 间隔）；客户端滚动展示 Top2 PK 文案。
    /// top3 可能为 null 或长度 &lt; 3；top3 为空时前端直接忽略。</summary>
    [Serializable]
    public class EfficiencyRaceData
    {
        public EfficiencyRaceEntry[] top3;
        public int                   dayTotal;
    }

    /// <summary>§34 B10b 夜晚预告（type=day_preview，S→C）。
    /// 白天最后 10s 服务端推送一次，客户端渲染倒计时横幅；若 phase_changed.phase='night' 则立即隐藏。
    /// nightModifier 可为 null（普通夜晚），复用组 D 的 NightModifierData。</summary>
    [Serializable]
    public class DayPreviewData
    {
        public int                monsterCount;
        public int                bossHp;
        public NightModifierData  nightModifier;   // null → 普通夜晚
    }

    // ==================== §34 Layer 2 组 A 新手友好（B1/B5/B8/B9，🆕 v1.27） ====================
    // 🔴 audit-r28 GAP-A26-09 死代码清理：work_command_response 协议双轨问题已确认 — 服务端 0 emit
    //   'work_command_response'，playerStats 实际捎带在 work_command 广播上（HandleWorkCommand 路径分发 OnPlayerStatsUpdated）。
    //   选 B 决策：WorkCommandResponseData 类已死代码（case 路由删除 — SurvivalGameManager.cs 注释中保留历史参考）。
    //   PlayerStatsData 类保留 — 仍在 WorkCommandData.playerStats 字段中使用（HandleWorkCommand 路径）。

    /// <summary>§34 B9 个人贡献条 —— 每次 work_command 响应附带的玩家统计。
    /// 服务端将 playerStats 嵌入 work_command 广播（非独立 work_command_response），客户端在 HandleWorkCommand 中分发。
    /// 无 playerStats 时（兼容场景）前端保持隐藏不展示。
    /// fairyWandBonus 对应 §34 B8 累计加成（0-100 百分比整数；≥100 时满级）。</summary>
    [Serializable]
    public class PlayerStatsData
    {
        public int contribution;     // 本局累计贡献
        public int rank;             // 当前排名（从 1 起）；未上榜服务端按末位发送
        public int fairyWandBonus;   // 仙女棒累计效率加成（0-100 百分比整数）
    }

    // ⚠️ audit-r28 GAP-A26-09 死代码清理：原 WorkCommandResponseData 类已删除（无占位）。
    //   原由：服务端 0 emit 'work_command_response'，playerStats 实际捎带在 work_command 广播上；
    //   客户端 case "work_command_response" 路由也已删除（详见 SurvivalGameManager.cs 注释保留）；
    //   PlayerStatsData 类保留作 work_command.data.playerStats 字段使用（HandleWorkCommand 路径分发 OnPlayerStatsUpdated）。

    /// <summary>§34 B8 仙女棒满级（≥100% fairyWandBonus）的玩家。
    /// 服务端 fairy_wand 累计跨过 100% 阈值时 unicast 给该玩家，客户端全屏金闪 + 跑马灯。
    /// 由 SurvivalGameManager 路由到 OnFairyWandMaxed 事件。</summary>
    [Serializable]
    public class FairyWandMaxedData
    {
        public string playerId;
        public string playerName;
    }

    // ==================== 协议骨架补齐 Batch A（🆕 v1.27+） ====================
    // 本段用于断线重连状态快照 / 统一失败消息 / §36.12 解锁事件 / §24.4 轮盘防御 /
    // §35 P2 反击扩展等多个跨模块骨架类型。所有类型均为"协议骨架"——仅定义字段映射，
    // 具体业务处理由各自模块的 Handler 实现。

    // ---- A1. 断线重连 in-progress 状态快照 ----
    // 服务端重连时附带当前房间进行中的各种子系统状态；缺失字段 JsonUtility 回落 null/0。

    /// <summary>轮盘进行中状态（§24.4，断线重连快照）</summary>
    [Serializable]
    public class RoulettePendingData
    {
        public string cardId;
        public string[] displayedCards;
        public long spunAt;
        public long autoApplyAt;
    }

    [Serializable]
    public class RouletteEffectData
    {
        public string cardId;
        public long endsAt;
    }

    [Serializable]
    public class RouletteInProgressData
    {
        public string phase;         // "charging" / "ready" / "pending" / "effect"
        public RoulettePendingData pending;
        public RouletteEffectData effect;
        public string cardId;        // 当前生效卡片 id（若有）
        public long   effectEndsAt;  // 效果结束时间（Unix ms）
        public long   readyAt;       // 下次充能完成时间（Unix ms）
    }

    /// <summary>建造进行中状态（§37，断线重连快照）</summary>
    [Serializable]
    public class BuildVotingInProgressData
    {
        public string proposalId;
        public string proposerName;
        public string[] options;
        public long votingEndsAt;
        public string[] voteBuildIds;
        public int[] voteCounts;
        public int totalVoters;
    }

    [Serializable]
    public class BuildBuildingInProgressData
    {
        public string buildId;
        public long startedAt;
        public long completesAt;
        public Vector3Data position;
    }

    [Serializable]
    public class BuildInProgressData
    {
        public BuildVotingInProgressData voting;
        public BuildBuildingInProgressData building;
        public string buildingId;
        public string name;
        public float  progress;      // 0-1
        public long   completedAt;   // Unix ms
    }

    /// <summary>探险进行中状态（§38，断线重连快照，entry 元素）</summary>
    [Serializable]
    public class ExpeditionEventPhaseData
    {
        public string eventId;
        public long eventEndsAt;
        public string[] options;
    }

    [Serializable]
    public class ExpeditionInProgressData
    {
        public string expeditionId;
        public string workerId;
        public string phase;         // "outbound" / "event" / "return"
        public long   endsAt;        // Unix ms
        public string playerId;
        public int    workerIdx;
        public long   startedAt;
        public long   returnsAt;
        public ExpeditionEventPhaseData eventPhase;
    }

    /// <summary>攻防战进行中状态（§35，断线重连快照）</summary>
    [Serializable]
    public class TribeWarStolenResourcesData
    {
        public int food;
        public int coal;
        public int ore;
    }

    [Serializable]
    public class TribeWarStatsData
    {
        public int expeditionsSent;
        public long startedAt;
        public float damageMultiplier;
    }

    [Serializable]
    public class TribeWarInProgressData
    {
        public string sessionId;
        public string state;         // "attacking" / "defending" / "idle"
        public string role;          // "attacker" / "defender"
        public string targetRoomId;
        public long   endsAt;        // Unix ms
        public int    energyAccumulated;
        public int    remoteMonstersAlive;
        public TribeWarStolenResourcesData stolenResources;
        public TribeWarStatsData stats;
    }

    /// <summary>断线重连房间全量状态快照（type=room_state，S→C）。
    /// 服务端在客户端重连成功后推送一次，包含所有进行中子系统的最小必要状态。
    /// 任一子对象缺失时 JsonUtility 回落 null；客户端按 null 判空跳过。
    /// 4 个 daily cap 字段与 SurvivalGameStateData 同源扩展（§36.5.1 每日闯关上限）。</summary>
    [Serializable]
    public class RoomStateData
    {
        // 🆕 audit-r3 P1-Reviewer：服务端 _broadcastRoomState payload 的 10 主字段（断线重连快照）
        public int    fortressDay;                 // 当前堡垒日
        public int    maxFortressDay;              // 历史最大堡垒日
        public int    totalCycles;                 // 总循环次数（_enterSettlement 计数）
        public string streamerKingTitle;           // "堡垒之王" 或 null
        public int    currentSeasonId;             // 当前赛季 id
        public string lastThemeId;                 // 🔴 audit-r25 GAP-D25-09：服务端实发当前主题（与 themeId 同值，作为持久化保留入口；非"上个赛季"语义）
        public string themeId;                     // 本赛季主题 id
        public string phase;                       // "day"/"night"/"recovery"/"idle"/... 与 SurvivalGameStateData.state 同源
        public string variant;                     // "normal"/"recovery"/... 与 PhaseChangedData.variant 同源
        public long   lifetimeContribTotal;        // 全房间终身贡献总和（用 long 以防溢出）

        // 进行中状态（§24.4 轮盘 / §37 建造 / §38 探险 / §35 攻防战）
        public RouletteInProgressData       roulette;
        public BuildInProgressData          build;
        public ExpeditionInProgressData[]   expeditions;
        public TribeWarInProgressData       tribeWar;

        // §36.5.1 每日闯关上限扩展 4 字段（服务端未启用 cap flag 时 dailyCapMax 可能为 0）
        public int  dailyFortressDayGained;
        public int  dailyCapMax;
        public long dailyResetAt;              // Unix ms，下次 UTC+8 05:00
        public bool dailyCapBlocked;

        // 🆕 P0-B4：上一次 fortressDay 变更的原因（与 FortressDayChangedData.reason + SurvivalGameEndedData.reason 同源枚举）
        //   audit-r12 GAP-B09 注释精化：实际服务端 _lastFortressDayChangeReason 枚举为
        //   'none' | 'promoted' | 'cap_blocked' | 'cap_reset' | 'newbie_protected' | 'demoted' | 'gate_breached'
        //   | 'food_depleted' | 'temp_freeze' | 'all_dead' | 'manual'（注：'survived' 字符串实际未发，
        //   r10 之前注释保留作历史向后兼容）
        //   断线重连时 UI 可据此识别上次降级原因并回放 toast/动画；服务端不下发时 JsonUtility 回落 null/""。
        public string fortressDayChangeReason;

        public long   timestamp;                   // Unix ms，协议统一时间戳
    }

    /// <summary>room_state 兼容别名（部分模块在规范/审计中称为 RoomStateInProgressData，
    /// 为避免引用歧义并保持对齐，此处保留同结构别名类）。</summary>
    [Serializable]
    public class RoomStateInProgressData
    {
        public RouletteInProgressData       roulette;
        public BuildInProgressData          build;
        public ExpeditionInProgressData[]   expeditions;
        public TribeWarInProgressData       tribeWar;
    }

    // ---- A7. §36.12 feature_unlocked ----
    /// <summary>功能解锁通知（type=feature_unlocked，S→C）。
    /// 具体解锁哪个功能 → featureId（见 SurvivalMessageProtocol.FeatureXxx 常量）。
    /// 与 world_clock_tick.newlyUnlockedFeatures 的区别：后者是批量数组，本消息为单个事件，
    /// 适合 UI 逐条播放解锁横幅；服务端可能同时下发两者，客户端做幂等。</summary>
    [Serializable]
    public class FeatureUnlockedData
    {
        public string featureId;   // 对应 §36.12 FEATURE_UNLOCK_DAY 键（如 "expedition"/"shop"）
        public int    unlockedAt;  // 解锁时的 seasonDay（1-7）
        public string message;     // 展示文案（服务端提供，可为空串）
    }

    // ---- A8. §24.4 轮盘效果被阻止 ----
    /// <summary>主播轮盘效果被阻止（type=broadcaster_roulette_effect_prevented，S→C，unicast 发起方）。
    /// preventReason 枚举：duplicate（同类效果已生效）/ conflict_with_other_buff（与其他 buff 冲突）/
    /// game_not_running（游戏不在运行态）。</summary>
    [Serializable]
    public class RouletteEffectPreventedData
    {
        public string cardId;
        public string preventReason;  // duplicate / conflict_with_other_buff / game_not_running
    }

    // ---- A12. §35 P2 反击/攻击扩展 ----
    /// <summary>攻防战反击请求（type=tribe_war_retaliate，C→S only）。
    /// 🔴 audit-r27 死代码清理：S→C 推送路径从未实装（服务端 0 emit 此 type，仅 SurvivalRoom.js:945 是 C→S inbound handler，
    ///   反击成功后服务端走 tribe_war_attack_started 路径广播）；客户端 case 路由 + OnTribeWarRetaliate Action 已删除。
    ///   原 damageMultiplier 字段（S→C only）一并删除 — 防止未来开发者按错误注释认为是双向协议。
    ///   保留：本类用作 C→S 请求时序列化 targetRoomId 字段。</summary>
    [Serializable]
    public class TribeWarRetaliateData
    {
        public string targetRoomId;
    }

    // ==================== §34 B7 新手引导（🆕 v1.27+ audit-r3/P1） ====================

    /// <summary>新玩家加入单播欢迎横幅（type=newbie_welcome，S→C，仅发给新玩家本人）。
    /// 触发条件：玩家 _lifetimeContrib 为 0 或当前赛季 id=1；UI 侧以 B 类 modal 显示 30s。</summary>
    [Serializable]
    public class NewbieWelcomeData
    {
        public string playerId;  // 目标玩家 id（客户端可据此过滤：只渲染自己为主角的弹幕）
        public string hint;      // 欢迎文案（服务端固定："发送弹幕 1/2/3/4 指挥矿工采集"）
        public int    ttlSec;    // 默认 30s，客户端倒计时自动隐藏
    }

    /// <summary>本局首次发送 1-6 有效弹幕的广播（type=first_barrage，S→C，房间广播）。
    /// 每位 playerId 本局仅广播一次；UI 侧以浅绿 B 类 toast 显示 3s。</summary>
    [Serializable]
    public class FirstBarrageData
    {
        public string playerId;
        public string playerName;
        public int    cmd;       // 1-6（cmd=5 已在服务端静默拦截，此处不会出现）
        // r14 GAP-B-MAJOR-03 / r12 GAP-D01：服务端 _getWorkerIndex(playerId) 派生
        // 客户端 NewbieHintUI / WorkerVisual 据此挂金色光柱到对应矿工（§32.2.1 L4005 / §34 B7 L5145）
        // -1 表示玩家未绑定矿工（助威者第 13+ 自动分流）；JsonUtility 不支持 nullable，用 -1 占位
        public int    workerId;
    }

    // ==================== §36.10 WaitingPhase（🆕 v1.27+ audit-r3/P1） ====================

    /// <summary>WaitingPhase 窗口开始（type=waiting_phase_started，S→C）。
    /// 两种触发源共用同一消息类型：
    ///   1) SeasonManager 赛季切换时广播（durationSec/newSeasonId/newThemeId 三字段）
    ///   2) audit-r6 §36.10：SurvivalGameEngine 夜晚 Boss 波前广播（waveIdx/countdownSec/allowVoteTypes）
    /// 客户端按 waveIdx > 0 判断是哪种源，驱动不同 UI 展示。</summary>
    [Serializable]
    public class WaitingPhaseStartedData
    {
        public int    durationSec;   // SeasonManager: 默认 30；客户端倒计时（兼容旧 schema）
        public int    newSeasonId;   // SeasonManager: 仅赛季切换时非 0
        public string newThemeId;    // SeasonManager: classic_frozen / blood_moon / snowstorm / dawn / frenzy / serene

        // audit-r6 §36.10 新增字段（夜晚 Boss 波前窗口）
        public int      waveIdx;         // 即将到来的波次 index（0 及以下视为 SeasonManager 源）
        public int      countdownSec;    // 波次窗口倒计时（默认 15）
        public string[] allowVoteTypes;  // ['support','experience','tribe_war'] 等；MVP 可空数组

        // r14 GAP-B-MAJOR-02 / r12 GAP-D03：服务端透传 _currentNightModifier
        // 客户端 WaitingPhaseUI 在 30s 投票窗口可显示"今晚血月 Boss HP×3"等氛围信息（§36.10 L6253 / §34 B10b L5191）
        // 复用 NightModifierData（与 PhaseChangedData.nightModifier 同源）；服务端无修饰符时为 null
        public NightModifierData nightModifier;
    }

    /// <summary>准备窗口结束（type=waiting_phase_ended，S→C）。
    /// 无字段（空 data 对象即可）；客户端用于兜底关闭 WaitingPhaseUI。</summary>
    [Serializable]
    public class WaitingPhaseEndedData
    {
        // 服务端下发空 data，客户端反序列化仍可生成空对象
    }

    // ==================== audit-r5 客户端补齐 Batch ====================

    // v1.27 §14 / §19 / §34.4 E9 废止：DifficultyChangedData 已删除（difficulty_changed 协议不再存在）

    /// <summary>§30.3 阶9 矿工护盾触发（type=worker_shield_activated，S→C）。
    /// 服务端在矿工 HP=0 但消耗 _invincibleShield 抵消致命伤时广播；客户端 5s 染蓝 tint + "无敌" 气泡。
    /// 字段对齐 SurvivalGameEngine.js:4388。</summary>
    [Serializable]
    public class WorkerShieldActivatedData
    {
        public string playerId;
        public string playerName;
        public long   durationMs;    // 默认 5000（5s）
    }

    /// <summary>§34 B8 仙女棒累计光点（type=fairy_wand_applied，S→C）。
    /// 每次 fairy_wand 送出都广播；客户端按 capped=true 切换到金爆裂，否则推一个蓝光点。
    /// 字段对齐 SurvivalGameEngine.js:1929-1939。</summary>
    [Serializable]
    public class FairyWandAppliedData
    {
        public string playerId;
        public string playerName;
        public float  prevBonus;    // 本次应用前的 _playerEfficiencyBonus（0.00~1.00）
        public float  nextBonus;    // 本次应用后（+0.05，上限 1.00）
        public int    stackCount;   // 累计送出次数
        public bool   capped;       // nextBonus >= 0.999 时 true
    }

    // ==================== audit-r6 客户端补齐 Batch ====================

    // v1.27 §14 / §34.4 E9 废止：ChangeDifficultyFailedData / ChangeDifficultyAcceptedData 已删除（change_difficulty_failed / change_difficulty_accepted 协议不再存在）

    /// <summary>🔴 audit-r26 GAP-E26-01 / GAP-A26-04：r25 GAP-E25-06 半成品闭环 — entrance_spark 装备时服务端 broadcast。
    /// 服务端 SurvivalGameEngine.js:9572-9582 实装 emit；客户端订阅 OnEntranceSparkTriggered 触发入场特效（VIPAnnouncementUI 路径或独立 EntranceFXUI）。</summary>
    [Serializable]
    public class EntranceSparkTriggeredData
    {
        public string playerId;
        public string playerName;
    }

    /// <summary>§30.4 每日等级衰减（type=daily_tier_decay，S→C，广播）。
    /// 🔴 audit-r26 GAP-D26-03：补 mode 字段（'active' / 'inactive'）+ 注释纠正语义。
    /// 服务端 SurvivalGameEngine.js:7831 _applyDailyTierDecay 对所有玩家在 UTC+8 00:00 触发：
    ///   - 'active' 路径：×0.975（每日活跃过的玩家小幅减免）
    ///   - 'inactive' 路径：×0.95（不活跃玩家衰减更大）
    /// 客户端可按 mode 显示差异化反馈（"昨夜未活跃，矿工等级 ×0.95" vs "活跃日衰减 ×0.975"）。</summary>
    [Serializable]
    public class DailyTierDecayData
    {
        public string playerId;
        public int    oldLevel;
        public int    newLevel;
        public string mode;     // 🔴 audit-r26 GAP-D26-03：'active' | 'inactive'
    }

    /// <summary>§30.7 限时皮肤激活（type=gift_skin_applied，S→C，广播）。
    /// T4 → G01 炽热矿工 / T5 → G02 战魂矿工 / T6 → G03 空投精英。
    /// 字段对齐 SurvivalGameEngine.js:_applyGiftSkin。</summary>
    [Serializable]
    public class GiftSkinAppliedData
    {
        public string playerId;
        public string skinId;       // "G01" | "G02" | "G03"
        public long   expireAt;     // Unix ms；到达昼夜切换结束
    }

    /// <summary>§30.7 限时皮肤到期（type=gift_skin_expired，S→C，广播）。
    /// 服务端 _scanGiftSkinExpiry 在昼夜切换时清理。
    /// audit-r6 Reviewer LOW：补 skinId 字段对齐服务端 `:7215` 广播 {playerId, skinId}，避免 JsonUtility 静默丢弃。</summary>
    [Serializable]
    public class GiftSkinExpiredData
    {
        public string playerId;
        public string skinId;    // "G01" | "G02" | "G03"（客户端可用于跑马灯 / 日志文案细节）
    }

    // ==================== audit-r8 §34 F 层补齐 ====================

    /// <summary>§34 F4 Boss 露出弱点（type=boss_weakness_started，S→C，广播）。
    /// 触发条件：玩家对数 HP 轰击命中关键节点，Boss 露出破绽 5s。
    /// 期间：弹幕 "6" 伤害 ×5，T5 AOE 伤害 ×3；服务端倒计时结束后自动 ended 广播。</summary>
    [Serializable]
    public class BossWeaknessStartedData
    {
        public string bossId;
        public int    durationMs;   // 默认 5000
        public float  damageMult;   // 弹幕 6 倍率，默认 5.0
        public float  t5Mult;       // T5 AOE 倍率，默认 3.0
    }

    /// <summary>§34 F4 Boss 弱点结束（type=boss_weakness_ended，S→C，广播）。
    /// 由服务端 durationMs 到期触发；客户端收到后立即隐藏弱点 UI。</summary>
    [Serializable]
    public class BossWeaknessEndedData
    {
        public string bossId;
    }

    /// <summary>§34 F8 指令无效提示（type=invalid_command_hint，S→C，单播）。
    /// - invalid_cmd_5：玩家发送未配置的弹幕指令（如 7/8/9 数字超范围）
    /// - wrong_phase_6：玩家在错误阶段发送指令（如白天发 6 攻击、夜晚发 1 采集食物）</summary>
    [Serializable]
    public class InvalidCommandHintData
    {
        public string type;  // "invalid_cmd_5" | "wrong_phase_6"
        public string msg;   // 中文提示文本，服务端本地化后单播
        public int    ttl;   // 毫秒，默认 4000
    }

    /// <summary>audit-r20：§34.3 D 段幕末事件全屏公告（type=chapter_end_event，S→C，全房广播）。
    /// 服务端 SurvivalGameEngine.js:6987 _applyActEndEventIfNeeded() emit；按 seasonDay 触发：
    /// - seasonDay=1（prologue 结束夜）：actTag='prologue', event='first_elite', hint='首个精英怪来袭！'
    /// - seasonDay=3（act1 结束夜）：actTag='act1',     event='double_boss', hint='双 Boss 同时出现！'
    /// - seasonDay=5（act2 结束夜）：actTag='act2',     event='mini_boss_rush', hint='Boss Rush 降临！'
    /// - seasonDay=6（act3 结束夜）：actTag='act3',     event='hp_amplified', hint='极限挑战：怪物 HP ×1.5，Boss HP ×2！'
    /// - seasonDay=7（终章）：       actTag='finale',   event='boss_rush_finale', hint='终章 Boss Rush（§36）'
    /// 客户端：UI 全屏 hint 公告（淡入淡出 2s）+ TopBar 角落 actTag。</summary>
    [Serializable]
    public class ChapterEndEventData
    {
        public int    day;        // 当前 day（含累积，与 fortressDay 同步）
        public int    seasonDay;  // 1~7 当前赛季日（D7 整周循环）
        public string actTag;     // "prologue" | "act1" | "act2" | "act3" | "finale"
        public string @event;     // "first_elite" | "double_boss" | "mini_boss_rush" | "hp_amplified" | "boss_rush_finale"
        public string hint;       // 全屏公告中文文本
    }

    /// <summary>audit-r20：§34.3 E3b 不朽证明免死豁免触发（type=free_death_pass_triggered，S→C，全房广播）。
    /// 服务端 SurvivalGameEngine.js:6752 _consumeFreeDeathPass() emit；
    /// 触发时机：玩家累计贡献达 20000（"不朽证明"门槛）后，矿工首次将死时消费一次免死豁免，
    /// 矿工原地满血复活；同时记录"最戏剧性事件"候选（_recordDramaticEvent('free_death_pass')）。
    /// 客户端：UI 跑马灯/全屏金光提示 "{playerName} 触发不朽证明，矿工免死复活！"</summary>
    [Serializable]
    public class FreeDeathPassTriggeredData
    {
        public string playerId;
        public string playerName;
    }

    /// <summary>audit-r20：§15 / §19.2 房间销毁通知（type=room_destroyed，S→C，单播给该房间所有 ws 客户端）。
    /// 服务端 SurvivalRoom.js:354 emit 后立即 ws.close(1000, 'room_destroyed')；
    /// reason 通常为 'timeout'（房间空闲超时被回收）。
    /// 客户端：UI 弹窗"房间已销毁（原因：超时）" + 自动回大厅；并兜底 NetworkManager.OnClose(1000) 触发同样路径。</summary>
    [Serializable]
    public class RoomDestroyedData
    {
        public string roomId;
        public string reason;  // "timeout" 等
    }
}
