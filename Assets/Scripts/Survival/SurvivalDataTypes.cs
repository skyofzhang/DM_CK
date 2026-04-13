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
        public int    gateLevel;     // 城门当前等级（1-4）
        public int    scorePool;     // 当前积分池总量
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
        public int   gateLevel;      // 城门当前等级（1-4）
        public float remainingTime;
        public int   scorePool;      // 当前积分池总量
    }

    /// <summary>昼夜阶段切换</summary>
    [Serializable]
    public class PhaseChangedData
    {
        public string phase;         // "day" | "night"
        public int    day;           // 第几天
        public float  phaseDuration; // 本阶段秒数
    }

    /// <summary>怪物波次信息</summary>
    [Serializable]
    public class MonsterWaveData
    {
        public int    waveIndex;  // 第几批（0-based）
        public int    day;
        public string monsterId;  // e.g. "X_guai01"
        public int    count;
        public string spawnSide;  // "left" | "right" | "top" | "all"
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

    /// <summary>游戏结算</summary>
    [Serializable]
    public class SurvivalGameEndedData
    {
        public string result;       // "win" | "lose"
        public string reason;       // "survived" | "food_depleted" | "temp_freeze" | "gate_breached"
        public int    dayssurvived; // 存活天数
        public float  totalScore;   // 总贡献值
        public SurvivalRankingEntry[] rankings; // 贡献排行（服务器 Top 10）
        // D：积分池
        public int    scorePool;    // 本局总积分池
        public int    distributed;  // 实际瓜分金额
        public int    carryover;    // 结余（流入主播下局）
        public float  payoutRate;   // 瓜分比率（胜利0.6，失败0.3）
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
        public int    newLevel;      // 升级后等级 (2-4)（原 level，已对齐服务端字段名）
        public int    newMaxHp;      // 新的最大HP
        public int    oreRemaining;  // 升级后剩余矿石（原 cost，已对齐服务端字段名）
        public string upgradedBy;    // 触发升级的玩家ID
    }

    /// <summary>城门升级失败（矿石不足或已满级）</summary>
    [Serializable]
    public class GateUpgradeFailedData
    {
        public string reason;        // "max_level" | "insufficient_ore"
        public int    currentLevel;  // 当前等级（max_level时）
        public int    required;      // 需要矿石数（insufficient_ore时）
        public int    available;     // 当前矿石数（insufficient_ore时）
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
    }

    /// <summary>实时贡献榜（type=live_ranking）——贡献变化时服务器防抖推送</summary>
    [Serializable]
    public class LiveRankingData
    {
        public LiveRankingEntry[] rankings; // Top 5
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
}
