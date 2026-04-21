using System;

namespace DrscfZ.Survival
{
    // ==================== 服务器 → 客户端 数据结构 ====================
    // 与服务器 WebSocket 消息协议字段保持一致

    /// <summary>游戏状态同步（开始/暂停/结算）</summary>
    [Serializable]
    public class SurvivalGameStateData
    {
        public string state;         // "idle" | "day" | "night" | "settlement"
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
        // 注：服务端还发送 workerHp（dict），因 JsonUtility 不支持 Dictionary，
        //     由独立的 worker_hp_update 消息负责同步，此处忽略
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
    }

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
        public int    waveIndex;  // 第几批（0-based）
        public int    day;
        public string monsterId;  // e.g. "X_guai01"（旧协议字段，兼容保留）
        public int    count;      // 旧协议字段，兼容保留；新客户端优先走 monsters[]
        public string spawnSide;  // "left" | "right" | "top" | "all" | "spawn_at_death"

        // 🆕 §31 多样性系统扩展
        public MonsterSpawnInfo[] monsters;   // 新字段：每只怪物独立携带 variant；为 null/空 → 走旧 count 路径
        public bool               isSummonSpawn;  // 召唤怪死亡触发的迷你怪生成（spawn 位置从屏幕中心偏移）
    }

    /// <summary>玩家工作指令（服务器转发评论）</summary>
    [Serializable]
    public class WorkCommandData
    {
        public string playerId;
        public string playerName;
        public string avatarUrl;
        public int    commandId;   // 1=食物 2=煤炭 3=矿石 4=添柴 5=修城门
        public string commandName; // "food"|"coal"|"ore"|"heat"|"repair"
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
        public float  giftValue;   // 礼物价值（分）
        public int    addFood;
        public int    addCoal;
        public int    addOre;
        public float  addHeat;     // 炉温加成
        public int    addGateHp;   // 城门HP回复
        public float  contribution;// 贡献值（用于排行）
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
        public string monsterType;  // "normal" | "elite" | "boss"
        public string killerId;     // 击杀玩家ID（可为空=被动死亡）（原 killedBy，已对齐服务端字段名）
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

    /// <summary>城门升级失败（矿石不足 / 已满级 / 未解锁 / 已升过）</summary>
    [Serializable]
    public class GateUpgradeFailedData
    {
        public string reason;        // "max_level" | "insufficient_ore" | "feature_locked" | "already_upgraded_today" | "phase_disallowed"
        public int    currentLevel;  // 当前等级（max_level时）
        public int    required;      // 需要矿石数（insufficient_ore时）
        public int    available;     // 当前矿石数（insufficient_ore时）
        // 🆕 v1.27 §10 仅 feature_locked 时有效
        public int    unlockDay;     // 解锁所需天数
        public int    blockedLevel;  // 阻止升级的等级边界（Lv1-4 vs Lv5-6 解锁分类）
    }

    /// <summary>城门等级特性触发（Lv4反伤 / Lv5光环激活 / Lv6冲击波）🆕 v1.22</summary>
    [Serializable]
    public class GateEffectTriggeredData
    {
        public string   effect;            // 'thorns' | 'frost_aura' | 'frost_pulse'
        // thorns：服务端 _spawnWave 批量结算反伤，均分到 _activeMonsters 全体怪物
        //   hitMonsters = 受反伤的所有怪物 ID；damagePerMonster = 每只怪物承担的反伤；totalDamage = 总反伤
        // frost_pulse：Lv6 寒冰冲击波 15s 周期
        //   hitMonsters = 被冲击波命中的怪物 ID 列表；radius/damage/freezeMs = 视觉参数
        public string[] hitMonsters;       // thorns / frost_pulse 共用
        public int      damagePerMonster;  // thorns 时的每只怪物反伤
        public int      totalDamage;       // thorns 时的总反伤
        public int      radius;            // frost_pulse 时的视觉半径
        public int      damage;            // frost_pulse 时的冲击波单次伤害
        public int      freezeMs;          // frost_pulse 时的冻结时长
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

    // ==================== 助威模式 §33（🆕 v1.27，PM MVP：跳过 §36.12 / §30 依赖）====================

    /// <summary>观众注册为助威者（type=supporter_joined）</summary>
    [Serializable]
    public class SupporterJoinedData
    {
        public string playerId;
        public string playerName;
        public int    supporterCount;
    }

    /// <summary>助威者弹幕生效（type=supporter_action）</summary>
    [Serializable]
    public class SupporterActionData
    {
        public string playerId;
        public string playerName;
        public int    cmd;       // 1/2/3/4/6/666
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
    /// MVP 阶段服务端暂不推送（§36.12 未实现），仅保留协议。</summary>
    [Serializable]
    public class GiftSilentFailData
    {
        public string giftId;
        public string reason;    // "before_supporter_unlock"
        public int    unlockDay; // 6
        public int    priceFen;
    }

    // ==================== 本周贡献榜（type=weekly_ranking）====================

    /// <summary>本周贡献榜单条记录</summary>
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
        public WeeklyRankingEntry[] rankings;  // Top 10
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

    /// <summary>轮盘充能就绪通知（type=broadcaster_roulette_ready）</summary>
    [Serializable]
    public class RouletteReadyData
    {
        public long readyAt;    // Unix ms；-1 表示已就绪
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

    /// <summary>探险结算详情（ExpeditionReturnedData.outcome 子对象）</summary>
    [Serializable]
    public class ExpeditionOutcome
    {
        public string         type;          // 'success' / 'died' / 'empty'
        public ResourceBundle resources;     // 可为 null（empty/died 时）
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
    /// 服务端拒绝 send/recall 时返回，客户端显示跑马灯提示原因。</summary>
    [Serializable]
    public class ExpeditionFailedData
    {
        public string playerId;
        public string reason;      // 'max_concurrent'/'wrong_phase'/'worker_dead'/'duplicate'/'supporter_not_allowed'/'season_ending'/'feature_locked'
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

    /// <summary>建造提议被拒（type=build_propose_failed）</summary>
    [Serializable]
    public class BuildProposeFailedData
    {
        public string reason;           // 完整枚举见 §19.2 / §37.3
        public int    unlockDay;        // 仅 reason='feature_locked' 时有效
    }

    /// <summary>投票通过但扣费失败导致建造取消（type=build_cancelled）</summary>
    [Serializable]
    public class BuildCancelledData
    {
        public string buildId;
        public string reason;           // 当前仅 'insufficient_resources'
    }

    /// <summary>失败降级批量拆除（type=building_demolished_batch）</summary>
    [Serializable]
    public class BuildingDemolishedBatchData
    {
        public string[] buildingIds;
        public string   reason;         // 当前固定为 'demoted'
    }

    /// <summary>瞭望塔 10s 预告怪物波次（type=monster_wave_incoming）</summary>
    [Serializable]
    public class MonsterWaveIncomingData
    {
        public int  waveIndex;
        public long spawnsAt;           // Unix ms
        public long firstAttackAt;      // Unix ms ≈ spawnsAt + 3500
    }

    // ==================== 主播排行榜（type=streamer_ranking）====================

    /// <summary>主播排行榜单条记录</summary>
    [Serializable]
    public class StreamerRankingEntry
    {
        public int    rank;
        public string streamerId;
        public string streamerName;    // 主播名
        public string maxDifficulty;   // "normal" | "hard" | "hell"
        public int    maxDays;         // 最多坚持天数（最佳完成局）
        public int    totalWins;       // 总胜场数
        public int    totalGames;      // 总场次
        public int    score;           // 排名得分（difficulty_weight × maxDays）
    }

    /// <summary>主播排行榜响应（type=streamer_ranking）</summary>
    [Serializable]
    public class StreamerRankingData
    {
        public StreamerRankingEntry[] rankings; // Top 10
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

    /// <summary>B 类 ≥1000 主播 HUD 购买触发双确认弹窗（type=shop_purchase_confirm_prompt，§39.7）</summary>
    [Serializable]
    public class ShopPurchaseConfirmPromptData
    {
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

    /// <summary>背包/装备快照，进房或重连时推送（type=shop_inventory_data）</summary>
    [Serializable]
    public class ShopInventoryData
    {
        public string       playerId;
        public string[]     owned;
        public ShopEquipped equipped;
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
    /// 其它后端字段（difficulty/playerCount/gateHpPct）在主版可补。</summary>
    [Serializable]
    public class TribeWarRoomInfo
    {
        public string roomId;
        public string streamerName;
        public string state;         // 'day' | 'night' | 'recovery' | 其它
        public int    day;
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
    /// wrong_phase / feature_locked 等。</summary>
    [Serializable]
    public class TribeWarAttackFailedData
    {
        public string reason;
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
        public string themeId;
    }

    /// <summary>赛季结算（type=season_settlement，D7 夜晚结束或 Boss 池归零后广播）</summary>
    [Serializable]
    public class SeasonSettlementData
    {
        public int    seasonId;
        public string nextThemeId;
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
        public string reason;     // 失败原因
        public int    unlockDay;  // 仅 reason='feature_locked' 时有效（§36.12 broadcaster_boost.minDay=2）
        public string action;     // 子动作名 'efficiency_boost' / 'trigger_event'
    }
}
