/**
 * 极地生存法则 — 生存游戏引擎
 *
 * 游戏循环：
 *   idle → [start_game] → Day1 → Night1 → Day2 → Night2 → ... → Day7通过 → 胜利
 *   三种失败：food=0 / furnaceTemp≤-100 / gateHp=0
 *
 * 每5秒推送一次 resource_update
 * 夜晚按 waveConfig 定时生成 monster_wave
 * 评论1-5 → 转发 work_command 给客户端
 * 评论6  → 攻击怪物（夜晚有效）
 * 礼物 → 按策划案 §5.1 七种礼物 ID 分发效果，推送 survival_gift
 */

const { findGiftById, findGiftByPrice, getGift, getTierNumber } = require('./GiftConfig');

// ==================== §36 全服同步 / §36.5.1 每日闯关上限 常量 ====================
// FeatureFlags：开发期可硬编码，线上可改为读 config
const FeatureFlags = {
  ENABLE_DAILY_CAP: true,   // §36.5.1 每日闯关上限（默认开）
};
const DAILY_FORTRESS_CAP_MAX = 150;                 // §36.5.1 每 dayKey 最多 +150 fortressDay
const DAILY_RESET_HOUR_UTC8  = 5;                    // §36.5.1 UTC+8 05:00 日切
const FORTRESS_NEWBIE_PROTECT_DAY = 10;              // §36.5 fortressDay ≤ 10 失败免罚

// ==================== 怪物波次配置 ====================
// 每天包含：
//   normal: { hp, atk, spd, count, waves }  — 普通怪
//   elite:  { hp, atk, count } | null        — 精英怪（Day1 为 null）
//   boss:   { hp, atk }                      — Boss（每天都有，Day1以后出现）
//   legacy: { monsterId, baseCount, maxCount, beginning, refreshTime } — 波次调度
// baseCount: 每批生成数量（千军万马感）；maxCount: 单夜上限；beginning: 首波延迟(s)；refreshTime: 批次间隔(s)
const WAVE_CONFIGS = [
  { day: 1,  normal: { hp: 150,  atk: 3,  spd: 2.0, count: 12, waves: 3 }, elite: null,                      boss: { hp: 1000,  atk: 10  }, monsterId: 'X_guai01', baseCount: 8,  maxCount: 25,  beginning: 3, refreshTime: 10 },
  { day: 2,  normal: { hp: 200,  atk: 4,  spd: 2.2, count: 20, waves: 4 }, elite: { hp: 500,   atk: 8,  count: 1  }, boss: { hp: 2000,  atk: 15  }, monsterId: 'X_guai01', baseCount: 10, maxCount: 35,  beginning: 3, refreshTime: 9  },
  { day: 3,  normal: { hp: 250,  atk: 5,  spd: 2.5, count: 30, waves: 5 }, elite: { hp: 600,   atk: 10, count: 2  }, boss: { hp: 3000,  atk: 18  }, monsterId: 'X_guai01', baseCount: 12, maxCount: 45,  beginning: 3, refreshTime: 8  },
  { day: 4,  normal: { hp: 325,  atk: 6,  spd: 2.7, count: 18, waves: 4 }, elite: { hp: 800,   atk: 13, count: 2  }, boss: { hp: 4500,  atk: 22  }, monsterId: 'X_guai01', baseCount: 14, maxCount: 55,  beginning: 2, refreshTime: 8  },
  { day: 5,  normal: { hp: 400,  atk: 7,  spd: 3.0, count: 22, waves: 4 }, elite: { hp: 1000,  atk: 16, count: 3  }, boss: { hp: 6500,  atk: 27  }, monsterId: 'X_guai01', baseCount: 16, maxCount: 65,  beginning: 2, refreshTime: 7  },
  { day: 6,  normal: { hp: 500,  atk: 9,  spd: 3.2, count: 28, waves: 5 }, elite: { hp: 1300,  atk: 20, count: 4  }, boss: { hp: 9000,  atk: 33  }, monsterId: 'X_guai01', baseCount: 18, maxCount: 75,  beginning: 2, refreshTime: 7  },
  { day: 7,  normal: { hp: 600,  atk: 12, spd: 3.5, count: 35, waves: 5 }, elite: { hp: 1750,  atk: 26, count: 5  }, boss: { hp: 14000, atk: 42  }, monsterId: 'X_guai01', baseCount: 20, maxCount: 80,  beginning: 2, refreshTime: 6  },
  { day: 10, normal: { hp: 750,  atk: 15, spd: 3.8, count: 40, waves: 6 }, elite: { hp: 2200,  atk: 32, count: 6  }, boss: { hp: 20000, atk: 55  }, monsterId: 'X_guai01', baseCount: 25, maxCount: 100, beginning: 2, refreshTime: 6  },
  { day: 15, normal: { hp: 950,  atk: 20, spd: 4.0, count: 50, waves: 7 }, elite: { hp: 3000,  atk: 40, count: 8  }, boss: { hp: 35000, atk: 70  }, monsterId: 'X_guai01', baseCount: 30, maxCount: 120, beginning: 2, refreshTime: 5  },
  { day: 20, normal: { hp: 1200, atk: 25, spd: 4.2, count: 60, waves: 8 }, elite: { hp: 4000,  atk: 50, count: 10 }, boss: { hp: 55000, atk: 90  }, monsterId: 'X_guai01', baseCount: 40, maxCount: 150, beginning: 2, refreshTime: 5  },
  { day: 30, normal: { hp: 1600, atk: 32, spd: 4.5, count: 70, waves: 9 }, elite: { hp: 6000,  atk: 65, count: 12 }, boss: { hp: 90000, atk: 120 }, monsterId: 'X_guai01', baseCount: 50, maxCount: 200, beginning: 2, refreshTime: 4  },
  { day: 40, normal: { hp: 2200, atk: 40, spd: 4.8, count: 80, waves:10 }, elite: { hp: 9000,  atk: 85, count: 15 }, boss: { hp: 150000,atk: 160 }, monsterId: 'X_guai01', baseCount: 60, maxCount: 250, beginning: 2, refreshTime: 4  },
  { day: 50, normal: { hp: 3000, atk: 50, spd: 5.0, count: 90, waves:10 }, elite: { hp: 12000, atk: 110,count: 18 }, boss: { hp: 250000,atk: 200 }, monsterId: 'X_guai01', baseCount: 80, maxCount: 300, beginning: 2, refreshTime: 3  },
];

function getWaveConfig(day) {
  let cfg = WAVE_CONFIGS[0];
  for (const c of WAVE_CONFIGS) {
    if (day >= c.day) cfg = c;
  }
  return cfg;
}

// 城门等级配置（策划案 §10.2：lv1→2=100矿, lv2→3=250矿, lv3→4=500矿）
// 2026-04-15 修复：数组首位原为 0（导致 Lv1→Lv2 免费升级的 Bug），已对齐策划案设计意图
const GATE_UPGRADE_COSTS   = [100, 250, 500]; // index: lv1→idx0=100, lv2→idx1=250, lv3→idx2=500
const GATE_MAX_HP_BY_LEVEL = [1000, 1500, 2200, 3000]; // 城门HP：1级/2级/3级/4级

// ==================== §38 探险系统（Expedition） ====================
// 总时长 90s = 40s 外出 + 15s 外域事件 + 35s 返回（白天 120s 内完成）
// 最多 3 名矿工同时在外
const EXPEDITION_MAX_CONCURRENT = 3;
const EXPEDITION_OUTBOUND_SEC   = 40;
const EXPEDITION_EVENT_SEC      = 15;
const EXPEDITION_RETURN_SEC     = 35;
const EXPEDITION_TOTAL_SEC      = EXPEDITION_OUTBOUND_SEC + EXPEDITION_EVENT_SEC + EXPEDITION_RETURN_SEC; // 90
const EXPEDITION_EVENT_WEIGHTS  = [
  { id: 'lost_cache',      w: 0.25  },
  { id: 'wild_beasts',     w: 0.25  },
  { id: 'trader_caravan',  w: 0.25  },
  { id: 'meteor_fragment', w: 0.083 },
  { id: 'bandit_raid',     w: 0.083 },
  { id: 'mystic_rune',     w: 0.084 }, // 0.083 + 0.001 凑 1.0
];
const RUNE_CHARGE_DAILY_CAP = 3;           // 24h 内最多 3 次响应（§38.3 mystic_rune 硬上限）
const RUNE_CHARGE_WINDOW_MS = 24 * 3600 * 1000;
const WILD_BEASTS_DEATH_RATE = 0.30;        // wild_beasts 30% 死亡概率
const WILD_BEASTS_CONTRIB    = 200;         // wild_beasts 击杀贡献
const LOST_CACHE_RES         = { food: 0, coal: 50, ore: 80 };
const TRADER_COST_FOOD       = 200;
const TRADER_COST_ORE        = 50;

// 矿工成长系统常量（策划案 §30.10）
// TIER_THRESHOLDS[tierIdx]：进入该阶段所需的最小累计终身贡献值
// TIER_COST_PER_LV[tierIdx]：该阶段内每级所需贡献值增量
// TIER_THREAT[tierIdx]：阶段威胁值倍率（用于怪物目标选择）
// TIER_SKINS[tierIdx]：阶段对应的皮肤 ID（自动换肤/手动换肤共用）
const TIER_THRESHOLDS  = [0, 100, 300, 700, 1700, 3700, 8700, 18700, 43700, 103700];
const TIER_COST_PER_LV = [10, 20, 40, 100, 200, 500, 1000, 2500, 6000, 15000];
const TIER_THREAT      = [1.0, 1.3, 1.7, 2.2, 2.8, 3.4, 4.0, 4.6, 5.2, 6.0];
const TIER_SKINS       = ['skin_rookie','skin_veteran','skin_backbone','skin_elite',
                          'skin_assault','skin_iron','skin_commander',
                          'skin_wargod','skin_myth','skin_legend'];

// 随机事件名称映射（策划案 §8）
const EVENT_NAMES = {
  E01_snowstorm:    '暴风雪',
  E02_harvest:      '丰收季节',
  E03_monster_wave: '怪物来袭',
  E04_warm_spring:  '暖流涌现',
  E05_ore_vein:     '矿脉发现',
};

// ==================== §37 建造系统（Building System，MVP）====================
// 5 种建筑：watchtower / market / hospital / altar / beacon
// 投票窗口 45s；每日 1 次（跨日重置）；建造时长 3–5 min
// PM 决策（MVP）：
//   - §36.12 feature_locked 跳过（暂不门槛）
//   - §35 tribeWar 远征怪攻击骨架逻辑跳过（beacon 仅存 state，TODO §35）
//   - Altar 对 efficiency666Bonus 采用未拆分 Math.max 版（未实现 P5 拆分 → 1.15→1.25 触发值）
//   - Hospital 通过 §30 _getWorkerRespawnSec 接入，公式 max(5, 30 - 10*hasLv31 - 15*hasHospital)
//   - _roomCreatorId 暂不引入（TODO）
//   - 每日限 1 次用 _buildVoteUsedToday bool + _enterDay 重置
const BUILDING_CATALOG = {
  watchtower: { cost: { ore: 80,  coal: 40  }, buildMs: 180000, position: { x: 1.5, y: 0, z: -2 } },
  market:     { cost: { ore: 100, food: 200 }, buildMs: 180000, position: { x: 0,   y: 0, z: 8  } },
  hospital:   { cost: { ore: 120, coal: 60  }, buildMs: 240000, position: { x: -4,  y: 0, z: 10 } },
  altar:      { cost: { ore: 150, food: 80  }, buildMs: 240000, position: { x: 6,   y: 0, z: 10 } },
  beacon:     { cost: { ore: 200, coal: 100 }, buildMs: 300000, position: { x: 1.5, y: 0, z: 2  } },
};
const BUILDING_IDS = Object.keys(BUILDING_CATALOG);
const BUILD_VOTE_WINDOW_MS = 45000;
const BUILDING_KEEP_ON_DEMOTE = ['watchtower', 'market'];

// ==================== §24.4 主播事件轮盘（Broadcaster Event Roulette） ====================
// 充能 300s；6 张事件卡随机抽 3 展示，从 3 张中随机定格 1；spin→apply 两步防客户端跳过动画
// PM 决策（MVP）：堡垒日 ≥30 黄金版 +20% 不实现（TODO §36.5）；
//   aurora 与 T5 love_explosion 的情形 A/B/C 时序细分 → 简化为"两者独立生效、各自 clamp 至上限"
//   elite_raid 客户端端不计入 maxAliveMonsters 由 monsterType='elite_raid' 路由，前端另行处理
// 参考策划案 §24.4（第 2673-2756 行）
const ROULETTE_CARD_IDS  = ['elite_raid', 'time_freeze', 'double_contrib', 'mystery_trader', 'meteor_shower', 'aurora'];
const ROULETTE_COOLDOWN_MS = 300 * 1000;    // 300s 充能
const ROULETTE_AUTO_APPLY_MS = 10 * 1000;   // 10s 未 apply 自动兜底

// ==================== §39 商店系统（Shop System，MVP） ====================
// 策划案 §39.2 商品清单 / §39.3 双轨货币体系 / §39.5 装备槽
// PM 决策（MVP）：
//   - §36.12 `shop.minDay=2` 跳过（全局允许，不做 feature_locked）
//   - §36 赛季末 5 min `season_locked` 跳过
//   - B9/B10 赛季限定 SKU 跳过（`currentSeasonShopPool` 始终空；`买B9/10` 返 `item_not_found`）
//   - 持久化跳过：所有新字段仅内存，重启清零（TODO RoomPersistence schemaVersion 2→3 迁移）
//   - `_roomCreatorId` 鉴权放开（与 §24.4 / §37 一致）
//   - `shop_effect_triggered` 广播照做（客户端响应由另一 Agent 负责）
//   - LiveRankingEntry 扩展 `equipped` 字段（_broadcastLiveRanking 填充）
//   - `entrance_spark` 触发：若 VIP 路径存在则触发，否则 log 占位
const SHOP_CATALOG = {
  // A 类——战术即时道具（货币：本局 contributions，不入库存）
  worker_pep_talk:  { category: 'A', price: 150, slot: null, effect: 'worker_pep_talk' },
  gate_quickpatch:  { category: 'A', price: 200, slot: null, effect: 'gate_quickpatch' },
  emergency_alert:  { category: 'A', price: 300, slot: null, effect: 'emergency_alert' },
  spotlight:        { category: 'A', price: 250, slot: null, effect: 'spotlight' },
  // B 类固定 SKU——永久身份装备（货币：_contribBalance，入库存）
  title_supporter:     { category: 'B', price: 500,   slot: 'title' },
  title_veteran:       { category: 'B', price: 5000,  slot: 'title' },
  title_legend_mover:  { category: 'B', price: 50000, slot: 'title' },
  frame_bronze:        { category: 'B', price: 1000,  slot: 'frame' },
  frame_silver:        { category: 'B', price: 10000, slot: 'frame' },
  entrance_spark:      { category: 'B', price: 3000,  slot: 'entrance' },
  barrage_glow:        { category: 'B', price: 2000,  slot: 'barrage' },
  barrage_crown:       { category: 'B', price: 8000,  slot: 'barrage' },
};
// 弹幕 `买A<n>` / `买B<n>` 索引映射（1-based → itemId）
const SHOP_A_INDEX = ['worker_pep_talk', 'gate_quickpatch', 'emergency_alert', 'spotlight'];  // 买A1~4
const SHOP_B_INDEX = ['title_supporter', 'title_veteran', 'title_legend_mover',
                       'frame_bronze',    'frame_silver',
                       'entrance_spark',
                       'barrage_glow',    'barrage_crown'];  // 买B1~8
// 弹幕 `装XY` 字母槽位映射
const SHOP_EQUIP_SLOT_MAP = { T: 'title', F: 'frame', E: 'entrance', B: 'barrage' };
// 双确认 pending 5s TTL（§39.7）
const SHOP_PENDING_TTL_MS = 5000;
// 装备切换 2s 冷却（§39.5）
const SHOP_EQUIP_COOLDOWN_MS = 2000;
// A1 worker_pep_talk 下一波前 30s 采矿 +15%；简化为固定 30s 窗口（§39.2 A1 效果）
const SHOP_PEP_TALK_DURATION_MS = 30 * 1000;
// A4 spotlight 激活时长 10s（§39.2 A4）
const SHOP_SPOTLIGHT_DURATION_MS = 10 * 1000;
// A2 gate_quickpatch 立即 +100 HP（§39.2 A2）
const SHOP_GATE_QUICKPATCH_HP = 100;
// A3 emergency_alert 预警提前秒数（§39.2 A3，无瞭望塔时 10s，有瞭望塔时 30s——MVP 简化为固定 10s）
const SHOP_EMERGENCY_ALERT_LEAD_SEC = 10;

class BroadcasterRoulette {
  /** @param {SurvivalGameEngine} engine */
  constructor(engine) {
    this._engine       = engine;
    this._readyAt      = 0;     // 0 = 已就绪且未抽；>0 = 下次就绪时刻（Unix ms）
    this._pending      = null;  // { cardId, displayedCards, spunAt, autoApplyAt }
    this._effectActive = null;  // { cardId, endsAt }
  }

  /** reset：清空 pending/effect；readyAt 归 -1 表示未进入 Running（需 onReadyAtRunningStart 点亮） */
  reset() {
    this._readyAt      = -1;
    this._pending      = null;
    this._effectActive = null;
  }

  /** Running 状态首次进入：立即就绪（策划案 §24.4.2 "避免新主播 5 分钟空窗"） */
  onReadyAtRunningStart() {
    this._readyAt      = 0;    // 0 = ready
    this._pending      = null;
    this._effectActive = null;
    // 广播首次就绪（新客户端可据此点亮按钮）
    this._engine._broadcast({
      type: 'broadcaster_roulette_ready',
      data: { readyAt: -1 },   // -1 = 已就绪
    });
  }

  /** 每秒 tick：检查 readyAt / pending autoApplyAt / effect endsAt */
  tick(nowMs) {
    // ① 充能完成 → 广播就绪
    if (this._readyAt > 0 && nowMs >= this._readyAt) {
      this._readyAt = 0;
      this._engine._broadcast({
        type: 'broadcaster_roulette_ready',
        data: { readyAt: -1 },
      });
    }
    // ② 主播未点 apply 兜底（10s 后自动执行，防止刷新页面导致效果永不触发）
    if (this._pending && nowMs >= this._pending.autoApplyAt) {
      console.log(`[Roulette] auto-apply fallback for ${this._pending.cardId}`);
      this.apply(nowMs);
    }
    // ③ 持续 buff 到期 → 广播 effect_ended 并清理
    if (this._effectActive && nowMs >= this._effectActive.endsAt) {
      const cardId = this._effectActive.cardId;
      this._effectActive = null;
      this._engine._broadcast({
        type: 'broadcaster_roulette_effect_ended',
        data: { cardId },
      });
      console.log(`[Roulette] effect ended: ${cardId}`);
    }
  }

  /** 当前是否可以 spin（按钮点亮 = readyAt==0 且无 pending） */
  canSpin() {
    return this._readyAt === 0 && !this._pending;
  }

  /** 抽卡（从 6 张随机抽 3 张展示，再从 3 张中定格 1 张） */
  spin(nowMs) {
    if (!this.canSpin()) return false;

    // Fisher-Yates 洗牌前 3 张
    const pool = ROULETTE_CARD_IDS.slice();
    for (let i = pool.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      [pool[i], pool[j]] = [pool[j], pool[i]];
    }
    const displayedCards = pool.slice(0, 3);
    const cardId = displayedCards[Math.floor(Math.random() * 3)];

    this._pending = {
      cardId,
      displayedCards,
      spunAt:       nowMs,
      autoApplyAt:  nowMs + ROULETTE_AUTO_APPLY_MS,
    };
    // 立即重置充能（避免 apply 前再次 spin 叠加）
    this._readyAt = nowMs + ROULETTE_COOLDOWN_MS;

    this._engine._broadcast({
      type: 'broadcaster_roulette_result',
      data: {
        cardId,
        displayedCards,
        spunAt: nowMs,
      },
    });
    console.log(`[Roulette] spin: displayed=[${displayedCards.join(',')}] → locked=${cardId}`);
    return true;
  }

  /** 执行效果（spin 后动画结束时客户端回调 broadcaster_roulette_apply 触发） */
  apply(nowMs) {
    if (!this._pending) return false;
    const cardId = this._pending.cardId;
    this._pending = null;

    // 调度到对应 _applyXxx（若返回 endsAt，则记录 effectActive）
    const endsAt = this._dispatch(cardId, nowMs);
    if (endsAt && endsAt > nowMs) {
      this._effectActive = { cardId, endsAt };
    } else {
      // 即时生效卡（elite_raid / mystery_trader 等），不设 endsAt
      this._effectActive = null;
    }
    console.log(`[Roulette] apply: ${cardId}${endsAt ? ` (endsAt +${Math.round((endsAt-nowMs)/1000)}s)` : ''}`);
    return true;
  }

  /** dispatch 到具体卡实现（返回 endsAt > 0 表示持续 buff，0/undefined 表示瞬时） */
  _dispatch(cardId, nowMs) {
    const e = this._engine;
    switch (cardId) {
      case 'elite_raid':      return e._applyEliteRaid(nowMs);
      case 'time_freeze':     return e._applyTimeFreeze(nowMs);
      case 'double_contrib':  return e._applyDoubleContrib(nowMs);
      case 'mystery_trader':  return e._applyMysteryTrader(nowMs);
      case 'meteor_shower':   return e._applyMeteorShower(nowMs);
      case 'aurora':          return e._applyAurora(nowMs);
      default:
        console.warn(`[Roulette] unknown cardId: ${cardId}`);
        return 0;
    }
  }
}

// ==================== SurvivalGameEngine ====================

class SurvivalGameEngine {
  // 参与玩家上限 = 客户端矿工模型池大小（WorkerManager.MAX_WORKERS=12）
  static MAX_PLAYERS = 12;

  /**
   * @param {object} config        - 游戏配置（来自 config/default.json）
   * @param {function} broadcast   - 广播给所有客户端
   */
  constructor(config, broadcast) {
    this.config   = config;
    this.broadcast = broadcast;

    // 阶段时长（秒）
    // M-NUMERIC：每天120秒（2分钟），更贴合直播节奏
    this.dayDuration   = config.survivalDayDuration   || 120;  // 原180s，改为120s
    this.nightDuration = config.survivalNightDuration  || 120;  // 原90s，改为120s，防止夜晚太短
    this.totalDays     = config.survivalTotalDays      || 7;

    // 资源初始值
    this.food         = config.initFood        ?? 500;
    this.coal         = config.initCoal        ?? 300;
    this.ore          = config.initOre         ?? 150;
    this.furnaceTemp  = config.initFurnaceTemp  ?? 20;
    this.gateHp       = config.initGateHp       ?? 1000;
    this.gateMaxHp    = config.initGateHp       ?? 1000;

    // 城门等级
    this.gateLevel    = 1;

    // 资源衰减配置（每秒）
    // 目标：Normal难度下不采食约 3min 耗尽（200/1.0≈200s≈3.3min），持续紧迫
    this.foodDecayDay    = config.foodDecayDay    ?? 1.0;   // 白天食物消耗/s
    // F7: 夜晚食物消耗 = 白天 (策划案 §34 F7) —— 夜晚压力集中在单一维度（怪物伤害），去掉 ×1.5 决策焦虑
    this.foodDecayNight  = config.foodDecayNight  ?? 1.0;   // 夜晚食物消耗/s（与白天一致）
    this.tempDecayDay    = config.tempDecayDay    ?? 0.15;  // 白天炉温衰减/s
    this.tempDecayNight  = config.tempDecayNight  ?? 0.4;   // 夜晚炉温衰减/s
    this.coalBurnTicks   = 10;                              // 自动烧煤间隔（tick数，10=2秒）
    this.minTemp         = -100;
    this.maxTemp         = 100;

    // 怪物攻击城门伤害
    this.monsterGateDamage = config.monsterGateDamage ?? 5;

    // ── 矿工HP系统 ──────────────────────────────────────────────────────
    // { [playerId]: { hp, maxHp, isDead, respawnAt } }
    this._workerHp    = {};
    this._workerMaxHp = config.workerMaxHp ?? 100; // 每个矿工的最大HP
    // 矿工承伤倍率：每只怪对矿工的伤害 = monsterGateDamage × workerDamageMult
    this._workerDamageMult = config.workerDamageMult ?? 1.0;
    // 矿工死亡复活等待时间（毫秒）
    this._workerRespawnMs  = (config.workerRespawnSec ?? 30) * 1000;

    // 游戏状态
    // §16 v1.26 永续模式：状态机 idle | day | night | settlement | recovery
    //   settlement (~8s) → recovery (120s，不生成新波次) → night(day+1)，无胜利终点
    this.state          = 'idle';
    this.currentDay     = 0;
    this.remainingTime  = 0;

    // §16 恢复期 120s 定时器（_enterRecovery 启动，_clearAllTimers 清理）
    this._recoveryTimer = null;

    // §16.1 all_dead 失败路径：夜晚最近一次有矿工存活的时刻（Unix ms）
    // 更新时机：_initWorkerHp / _reviveWorker / _reviveAllWorkers / 夜晚伤害结算时任一矿工仍存活
    // 清零时机：constructor / reset() / _enterSettlement（避免跨局误触发）
    this._lastAliveAt   = 0;

    // 贡献追踪 { playerId: float }
    this.contributions  = {};
    this.playerNames    = {};  // playerId → playerName（结算时填入排行榜显示名）
    this.totalPlayers   = 0;

    // 夜晚活跃怪物 Map<monsterId, { id, type, maxHp, currentHp, atk }>
    // type: 'normal' | 'elite' | 'boss'
    this._activeMonsters = new Map();
    this._monsterIdCounter = 0;

    // 定时器
    this._tickTimer    = null;   // 200ms tick（资源衰减+时间递减）
    this._resourceSyncTimer = null; // 5s 全量同步
    this._waveTimers   = [];     // 夜晚波次定时器列表
    this._tickCounter  = 0;
    this._liveRankingTimer = null; // 贡献变化后防抖推送实时榜（1.5s）

    // 礼物效果状态（策划案 §5.1 / §4.4）
    this._playerEfficiencyBonus  = {};  // playerId → 累计加成值（仙女棒 +0.05/个，上限 1.0 = +100%）
    this._playerTempBoost        = {};  // playerId → 倍率（能量电池 1.3，持续180s，per-player）
    this._playerTempBoostTimers  = {};  // playerId → setTimeout handle

    // 666弹幕效果（策划案 §4.3）
    this.efficiency666Bonus = 1.0;   // 效率加成（1.15 = +15%）
    this.efficiency666Timer = 0;     // 剩余秒数

    // 难度倍率（由 _applyDifficulty 设置）
    this._difficulty       = 'normal';
    this._monsterHpMult    = 1.0;
    this._monsterCntMult   = 1.0;

    // ── §33 助威模式（Supporter Mode） ────────────────────────────────
    this._supporters             = new Map();
    this._guardianLastActive     = {};
    this._supporterCmdCooldown   = {};
    this._supporterAtkBuff       = 0;
    this._supporterAtkBuffTimer  = 0;
    this._afkCheckCounter        = 0;
    this._playerSlots            = {};

    // ── 矿工成长系统（策划案 §30，MVP）─────────────────────────────────
    // TODO §30 持久化：MVP 内存存储，服务重启清零；后续接入 WeeklyRankingStore（或新 WorkerLevelStore）
    this._lifetimeContrib    = {};   // 终身累计贡献（MVP 不持久化）
    this._playerLevel        = {};   // 1~100
    this._playerSkinId       = {};   // 当前激活皮肤（仅决定外观）
    this._legendReviveUsed   = {};   // 阶10 每晚1次免死标记（_enterNight 重置）
    this._playerSkinCooldown = {};   // 换肤弹幕冷却（60s）
    this._dynamicHpMult      = 1.0;  // 动态难度怪物HP倍率
    this._dynamicCountMult   = 1.0;  // 动态难度怪物数量倍率

    // ── §24.4 主播事件轮盘（Broadcaster Event Roulette）─────────────────
    this._roulette              = new BroadcasterRoulette(this);
    this._contribMult           = 1.0;   // double_contrib：全员贡献 ×2，60s
    this._contribMultTimer      = null;  // 60s 后归 1.0 的 setTimeout 句柄
    this._auroraEffMult         = 1.0;   // aurora：全员效率 ×1.5，60s
    this._auroraTimer           = null;  // 60s 后归 1.0 的 setTimeout 句柄
    this._freezeUntilMs         = 0;     // time_freeze：所有怪物冻结截止 Unix ms
    this._eliteRaidEndsAt       = 0;     // elite_raid：精英怪 30s 未击杀则攻击城门
    this._eliteRaidMonsterId    = null;  // elite_raid：精英怪 monsterId
    this._traderOffer           = null;  // mystery_trader：{ expiresAt, cardA, cardB }
    this._traderTimer           = null;  // 30s 超时弃权句柄
    this._meteorTimers          = [];    // meteor_shower：15 次 setTimeout 句柄
    this._rouletteRunningInited = false; // Running 首次进入 onReadyAtRunningStart 防止重复触发

    // ── §38 探险系统（Expedition，MVP）────────────────────────────────
    // expeditionId -> { playerId, workerIdx, startAt, returnsAt, eventId, eventEndsAt, options, outcome, outboundTimer, eventTimer, returnTimer, userChoice }
    this._expeditions           = new Map();
    this._expeditionIdCounter   = 0;
    this._meteorFragmentPending = false; // 下次 666 触发 efficiency666Bonus ×2.0（单次消费）
    // mystic_rune 24h 滑动窗口（最多 3 次）——MVP 内存 Map，TODO 接入 RoomPersistence 避免主播刷重启
    this._runeChargeLog         = [];

    // ── §37 建造系统（Building System，MVP）────────────────────────────
    // _buildings: 已建成建筑 ID 集合；跨夜/跨堡垒日保留，失败降级按 BUILDING_KEEP_ON_DEMOTE 部分保留
    // _buildingInProgress: buildId -> { completesAt, timer }
    // _buildVote: 当前活跃投票 { proposalId, options, startAt, votingEndsAt, votes, proposerName, timer }
    // _buildVoteUsedToday: 每日限 1 次投票；_enterDay 重置
    this._buildings          = new Set();
    this._buildingInProgress = new Map();
    this._buildVote          = null;
    this._buildVoteUsedToday = false;
    this._proposalIdCounter  = 0;

    // ── §39 商店系统（Shop System，MVP）─────────────────────────────────
    // 双轨货币：_lifetimeContrib（§30，仍为终身累计水位）+ _contribBalance（新增可消费余额）
    // MVP 持久化：全部内存存储，重启清零（TODO 接入 RoomPersistence schemaVersion 1→2）
    this._contribBalance              = {};  // {playerId: number} 可消费余额
    this._playerShopInventory         = {};  // {playerId: string[]} 已购 B 类 itemId
    this._playerShopEquipped          = {};  // {playerId: {title?, frame?, entrance?, barrage?}} 当前装备
    this._shopLastEquipAt             = {};  // {playerId: ts} 2s 冷却时间戳
    this._shopSpotlightActive         = {};  // {playerId: {endsAt}} spotlight 激活状态
    this._shopSpotlightUsedThisGame   = {};  // {playerId: true} spotlight 本局已用过（激活结束后）
    this._shopEmergencyAlertUsedWave  = {};  // {playerId: waveIdx} emergency_alert 同波次限 1 次
    this._shopPendingPurchases        = new Map();  // pendingId -> { playerId, itemId, price, expiresAt }
    this._peptTalkBoostUntil          = 0;   // §39.4 A1 worker_pep_talk 有效截止时间戳（Date.now() < 此值 → 采矿 +15%）
    this._shopSpotlightTimers         = {};  // {playerId: setTimeout handle} spotlight 过期句柄
    this._shopPendingIdCounter        = 0;

    // F8: 无效指令提示去重 (策划案 §34 F8) —— {`${playerId}:cmd5`|`${playerId}:cmd6day`: true}
    // 每位玩家每种提示类型每局最多显示一次；reset() 清空
    this._shopInvalidCmdHintSent      = {};

    // 点赞统计
    this.totalLikes = 0;

    // 每天防守成功进入积分池的基础分（由难度决定，_applyDifficulty 覆盖）
    this._poolNightBase = 500;

    // 积分池（策划案 §D：防守/礼物/点赞/击杀 → 积分入池；结算后按比例分配给 Top10）
    this.scorePool       = 0;    // 本局累计积分池
    this._carryoverPool  = 0;    // 上局结余（流入本局）

    // 随机事件状态（策划案 §8）
    this._nextEventTimer     = 90 + Math.random() * 30; // 首次触发时间（90-120s）
    this.tempDecayMultiplier = 1.0;  // 炉温衰减倍率（暴风雪事件改为 2.0）
    this.foodBonus           = 1.0;  // 食物采集加成（丰收事件改为 1.5）
    this.oreBonus            = 1.0;  // 矿石采集加成（矿脉事件改为 2.0）
    this._randomEventTimers  = [];   // setTimeout 句柄列表（用于 reset 清理）

    // §35 跨直播间攻防战：SurvivalRoom 构造后 setRoomContext() 注入 { room, tribeWarMgr }
    //   room       — 所属 SurvivalRoom 实例（用于查 roomId）
    //   tribeWarMgr— 全局 TribeWarManager 单例（用于能量/夜晚入口/结算断开）
    this.room         = null;
    this.tribeWarMgr  = null;

    // ── §36 全服同步 + 赛季制（MVP）─────────────────────────────────────
    // globalClock / seasonMgr 由 setRoomContext 注入；未注入时 phase 流转走原内部路径
    // roomPersistence 由 setRoomContext 注入；存 JSON 到 ./data/rooms/{roomId}.json
    this.globalClock     = null;
    this.seasonMgr       = null;
    this.roomPersistence = null;
    this._clockDriven    = false;  // true → 内部 _tick 跳过 phase 到期切换

    // ── §36 堡垒日（per-room）─────────────────────────────────────────
    // fortressDay 表示当前推进到的堡垒日；maxFortressDay 为历史最高
    // §36.5：首次 _onRoomSuccess +1；首次 _onRoomFail 按公式 demote
    // §36.5.1：每日闯关上限 150，以 UTC+8 05:00 为 dayKey
    this.fortressDay    = 1;
    this.maxFortressDay = 1;

    // §36.5.1 每日闯关上限（DAILY_CAP_MAX=150）
    this._dailyFortressDayGained = 0;      // 本 dayKey 已获得的 fortressDay 数
    this._dailyResetKey          = 0;      // 当前 dayKey（UTC+8 05:00 作为日切）
    this._dailyCapBlocked        = false;  // 达到 cap → 本 dayKey 后续 success 静默

    // FeatureFlags：§36.5.1 每日闯关上限默认启用
    if (!this.constructor._featureFlagsWarned) {
      this.constructor._featureFlagsWarned = true;
      console.log(`[Engine] FeatureFlags: ENABLE_DAILY_CAP=${FeatureFlags.ENABLE_DAILY_CAP}`);
    }
  }

  // ==================== §35 Tribe War 外部注入 ====================

  /**
   * SurvivalRoom 构造后调用，注入 room 引用和各全局服务单例。
   * 可选项；如果不调用，相关逻辑自动 no-op。
   *
   * @param {SurvivalRoom} room
   * @param {TribeWarManager} [tribeWarMgr]  §35 单例
   * @param {object} [extras]                §36 额外注入
   * @param {GlobalClock}      [extras.globalClock]     §36 全服时钟
   * @param {SeasonManager}    [extras.seasonMgr]       §36 赛季管理
   * @param {RoomPersistence}  [extras.roomPersistence] §36 持久化
   */
  setRoomContext(room, tribeWarMgr, extras = {}) {
    this.room        = room || null;
    this.tribeWarMgr = tribeWarMgr || null;

    // §36 注入：GlobalClock / SeasonManager / RoomPersistence
    if (extras.globalClock)     this.globalClock     = extras.globalClock;
    if (extras.seasonMgr)       this.seasonMgr       = extras.seasonMgr;
    if (extras.roomPersistence) this.roomPersistence = extras.roomPersistence;

    this._clockDriven = !!this.globalClock;

    // 持久化 load：恢复 fortressDay / _lifetimeContrib / _contribBalance / §36.5.1 daily cap
    if (this.roomPersistence && room && room.roomId) {
      const snap = this.roomPersistence.load(room.roomId);
      if (snap) {
        this._applyPersistedSnapshot(snap);
      }
    }
  }

  /**
   * 从持久化快照恢复字段。schemaVersion 缺失字段回退默认值（向下兼容）。
   */
  _applyPersistedSnapshot(snap) {
    if (!snap || typeof snap !== 'object') return;
    // §36 堡垒日
    if (typeof snap.fortressDay === 'number')    this.fortressDay    = Math.max(1, snap.fortressDay | 0);
    if (typeof snap.maxFortressDay === 'number') this.maxFortressDay = Math.max(this.fortressDay, snap.maxFortressDay | 0);

    // §30 矿工成长
    if (snap._lifetimeContrib && typeof snap._lifetimeContrib === 'object') this._lifetimeContrib = { ...snap._lifetimeContrib };
    if (snap._playerLevel     && typeof snap._playerLevel     === 'object') this._playerLevel     = { ...snap._playerLevel };
    if (snap._playerSkinId    && typeof snap._playerSkinId    === 'object') this._playerSkinId    = { ...snap._playerSkinId };

    // §39 商店
    if (snap._playerShopInventory && typeof snap._playerShopInventory === 'object') this._playerShopInventory = { ...snap._playerShopInventory };
    if (snap._playerShopEquipped  && typeof snap._playerShopEquipped  === 'object') this._playerShopEquipped  = { ...snap._playerShopEquipped };
    if (snap._contribBalance      && typeof snap._contribBalance      === 'object') this._contribBalance      = { ...snap._contribBalance };

    // §36.5.1 每日闯关上限
    if (typeof snap._dailyFortressDayGained === 'number') this._dailyFortressDayGained = snap._dailyFortressDayGained | 0;
    if (typeof snap._dailyResetKey          === 'number') this._dailyResetKey          = snap._dailyResetKey          | 0;
    if (typeof snap._dailyCapBlocked        === 'boolean') this._dailyCapBlocked       = snap._dailyCapBlocked;

    console.log(`[Engine:${(this.room && this.room.roomId) || '?'}] Loaded snapshot: fortressDay=${this.fortressDay} maxFortressDay=${this.maxFortressDay} dailyGained=${this._dailyFortressDayGained} dailyCapBlocked=${this._dailyCapBlocked}`);
  }

  /** §35 Tribe War：获取当前 roomId（未注入 room 时返 null）*/
  _getRoomId() {
    return (this.room && this.room.roomId) || null;
  }

  /**
   * §35 Tribe War：礼物/弹幕产生攻击能量 → 通知 TribeWarManager 累积到 session。
   * 遵守策划案 §35.3 能量表：
   *   弹幕 1/2/3/4/6 → +1；T1=1 / T2=5 / T3=10 / T4=20 / T5=50 / T6=100
   * 若本房间不是任何 session 的攻击方，manager 自动 no-op。
   */
  _tribeWarAddEnergy(delta) {
    if (!this.tribeWarMgr) return;
    const rid = this._getRoomId();
    if (!rid || !delta || delta <= 0) return;
    this.tribeWarMgr.onEnergyAdded(rid, delta);
  }

  /** §35 Tribe War：暴露 getWaveConfig 给 TribeWarSession（远征怪属性 = 守方当日普通怪） */
  _getWaveConfigForDay(day) {
    return getWaveConfig(day);
  }

  // ==================== 对外接口 ====================

  /** 客户端请求开始（含难度参数）*/
  startGame(difficulty = 'normal') {
    if (this.state !== 'idle') return;
    this._applyDifficulty(difficulty);
    this._enterDay(1);
  }

  /**
   * 根据难度调整怪物强度、资源消耗和总天数。
   * easy   → 适合 1-50  人观众：怪物弱, 资源多, 30天胜利（≈3小时）
   * normal → 适合 50-200人观众：标准值,           50天胜利（≈5小时）
   * hard   → 适合 200+ 人观众：怪物强, 资源紧, 70天胜利（≈7小时）
   * 资源衰减已×0.14（相对原7天难度），保证相同难度体验
   */
  _applyDifficulty(difficulty) {
    this._difficulty = difficulty;

    // decayMult 基于"7天难度 = 现在总天数的1/10"进行等比缩放：
    // decayMult: 资源衰减倍率（食物+炉温），越高越紧迫
    // coalBurnTicks: 自动烧煤间隔（tick数，1tick=200ms），越小烧得越快
    //   easy:   食物降~7/天, 煤3秒烧1个(宽裕), 初始资源1.5倍
    //   normal: 食物降~10/天, 煤2秒烧1个(标准), 压力适中
    //   hard:   食物降~14/天, 煤1.4秒烧1个(紧迫), 极限生存
    const presets = {
      // F5: 困难初始资源统一 200/120/50 (策划案 §34 F5) —— 困难通过消耗速率和怪物强度区分，不通过资源起始量
      // F3: 困难 70 → 40 天 (策划案 §34 F3) —— 抖音直播 4h40min 不现实；Day 40 WAVE_CONFIGS 已有配置
      easy:   { hpMult: 0.6, cntMult: 0.6, decayMult: 0.7, coalBurnTicks: 10, initFood: 160, initCoal: 96,  initOre: 40,  initGateHp: 800,  totalDays: 30, poolNightBase: 300, dayDuration: 120, nightDuration: 120 },
      normal: { hpMult: 1.0, cntMult: 1.0, decayMult: 1.0, coalBurnTicks: 7,  initFood: 200, initCoal: 120, initOre: 50,  initGateHp: 1000, totalDays: 50, poolNightBase: 500, dayDuration: 120, nightDuration: 120 },
      hard:   { hpMult: 1.5, cntMult: 1.5, decayMult: 1.5, coalBurnTicks: 5,  initFood: 300, initCoal: 180, initOre: 75,  initGateHp: 1500, totalDays: 40, poolNightBase: 800, dayDuration: 120, nightDuration: 120 },
    };
    const p = presets[difficulty] || presets.normal;

    // 存储怪物倍率（_enterNight 使用）
    this._monsterHpMult    = p.hpMult;
    this._monsterCntMult   = p.cntMult;
    // 积分池夜晚基础奖励
    this._poolNightBase    = p.poolNightBase;

    // 总关卡天数 + 阶段时长（各难度独立配置）
    this.totalDays     = p.totalDays;
    this.dayDuration   = p.dayDuration;
    this.nightDuration = p.nightDuration;

    // 按倍率调整资源衰减（在构造函数默认值基础上乘以倍率）
    // ⚠️ 默认值必须与构造函数保持一致：foodDecayDay=1.0, foodDecayNight=1.0 (F7)
    // F7: 夜晚食物消耗 = 白天 (策划案 §34 F7)
    this.foodDecayDay   = (this.config.foodDecayDay   ?? 1.0) * p.decayMult;
    this.foodDecayNight = (this.config.foodDecayNight ?? 1.0) * p.decayMult;
    this.tempDecayDay   = (this.config.tempDecayDay   ?? 0.15) * p.decayMult;
    this.tempDecayNight = (this.config.tempDecayNight ?? 0.40) * p.decayMult;
    this.coalBurnTicks  = p.coalBurnTicks || 10;  // 自动烧煤间隔

    // 各难度独立配置初始资源
    this.food      = p.initFood;
    this.coal      = p.initCoal;
    this.ore       = p.initOre;
    this.gateHp    = p.initGateHp;
    this.gateMaxHp = p.initGateHp;

    console.log(`[Engine] 难度: ${difficulty} | 怪物HP×${p.hpMult} 数量×${p.cntMult} 衰减×${p.decayMult} 煤烧速${p.coalBurnTicks}tick 食${p.initFood}/煤${p.initCoal}/矿${p.initOre}/门${p.initGateHp} 总天数:${p.totalDays}`);
  }

  /** 客户端请求重置 */
  reset() {
    this._clearAllTimers();
    this.state = 'idle';
    this.currentDay = 0;
    this.remainingTime = 0;
    // §16 all_dead 判定基准：reset 时归零，夜晚由 _initWorkerHp / 存活刷新
    this._lastAliveAt = 0;
    this.food        = this.config.initFood        ?? 500;
    this.coal        = this.config.initCoal        ?? 300;
    this.ore         = this.config.initOre         ?? 150;
    this.furnaceTemp = this.config.initFurnaceTemp  ?? 20;
    this.gateHp      = this.config.initGateHp       ?? 1000;
    this.gateMaxHp   = this.config.initGateHp       ?? 1000;
    this.gateLevel   = 1;
    this.contributions = {};
    this.playerNames   = {};   // 重置玩家名缓存
    this.totalPlayers  = 0;

    // 积分池：结余流入下局（carryover 机制）
    this.scorePool      = this._carryoverPool || 0;
    this._carryoverPool = 0;

    // 重置难度（等待下次 startGame 时重新设置）
    this._difficulty     = 'normal';
    this._monsterHpMult  = 1.0;
    this._monsterCntMult = 1.0;
    this._activeMonsters.clear();
    this._monsterIdCounter = 0;

    // §36 主题倍率（仅 night 生效）—— reset 时归默认；下次 _applyThemeRulesForNight 重新设置
    this._themeHpMult        = 1.0;
    this._themeCntMult       = 1.0;
    this._themeMaxAliveBonus = 0;

    // §36 fortressDay / maxFortressDay / §36.5.1 每日 cap 字段 —— reset 不清零（跨局永续）
    //   持久化字段全部来自 _applyPersistedSnapshot 或 _onRoomSuccess/_onRoomFail 路径

    // 重置礼物效果状态
    this._playerEfficiencyBonus = {};
    this._playerTempBoost       = {};
    // 清除所有临时 boost 定时器
    for (const t of Object.values(this._playerTempBoostTimers || {})) clearTimeout(t);
    this._playerTempBoostTimers = {};

    // 重置666 + 点赞 + 随机事件状态
    this.efficiency666Bonus  = 1.0;
    this.efficiency666Timer  = 0;
    this.totalLikes          = 0;
    this._nextEventTimer     = 90 + Math.random() * 30;
    this.tempDecayMultiplier = 1.0;
    this.foodBonus           = 1.0;
    this.oreBonus            = 1.0;
    for (const t of this._randomEventTimers) clearTimeout(t);
    this._randomEventTimers  = [];

    // §33 助威模式清理
    this._supporters.clear();
    this._guardianLastActive    = {};
    this._supporterCmdCooldown  = {};
    this._supporterAtkBuff      = 0;
    this._supporterAtkBuffTimer = 0;
    this._afkCheckCounter       = 0;
    this._playerSlots           = {};

    // §30 矿工成长：_lifetimeContrib / _playerLevel / _playerSkinId 跨局永续（不清）
    // _legendReviveUsed / _playerSkinCooldown 每局重置；_dynamicHp/Count 归 1.0
    this._legendReviveUsed    = {};
    this._playerSkinCooldown  = {};
    this._dynamicHpMult       = 1.0;
    this._dynamicCountMult    = 1.0;

    // §24.4 主播事件轮盘重置（buff 归默认 + 清定时器 + 轮盘状态清空）
    // _clearAllTimers 已清定时器句柄，这里再次清理确保字段归 1.0/0（reset 可能在定时器触发前调用）
    this._contribMult        = 1.0;
    if (this._contribMultTimer) { clearTimeout(this._contribMultTimer); this._contribMultTimer = null; }
    this._auroraEffMult      = 1.0;
    if (this._auroraTimer)      { clearTimeout(this._auroraTimer);      this._auroraTimer      = null; }
    this._freezeUntilMs      = 0;
    this._eliteRaidEndsAt    = 0;
    this._eliteRaidMonsterId = null;
    this._traderOffer        = null;
    if (this._traderTimer)      { clearTimeout(this._traderTimer);      this._traderTimer      = null; }
    for (const t of (this._meteorTimers || [])) clearTimeout(t);
    this._meteorTimers       = [];
    this._roulette.reset();
    this._rouletteRunningInited = false;

    // §38 探险系统重置：所有 expedition 视作 recall（无资源回馈），清定时器
    // _runeChargeLog 跨局永续（24h 滑动窗口，MVP 重启丢失；TODO 接入 RoomPersistence）
    this._cancelAllExpeditions('reset');
    this._expeditionIdCounter   = 0;
    this._meteorFragmentPending = false;

    // §37 建造系统重置：清建筑、清进行中、清投票
    this._buildings.clear();
    for (const [, info] of this._buildingInProgress) {
      if (info.timer) clearTimeout(info.timer);
    }
    this._buildingInProgress.clear();
    if (this._buildVote?.timer) clearTimeout(this._buildVote.timer);
    this._buildVote           = null;
    this._buildVoteUsedToday  = false;

    // §39 商店系统重置：
    //   跨局永续（MVP 仅内存，重启清零）：_contribBalance / _playerShopInventory / _playerShopEquipped
    //   每局重置：_shopLastEquipAt / _shopSpotlightActive / _shopSpotlightUsedThisGame /
    //              _shopEmergencyAlertUsedWave / _shopPendingPurchases / _peptTalkBoostUntil
    // TODO §39.10 持久化：接入 RoomPersistence schemaVersion 1→2，`_contribBalance[p]` 初值迁移为 `_lifetimeContrib[p]`
    this._shopLastEquipAt            = {};
    this._shopSpotlightActive        = {};
    this._shopSpotlightUsedThisGame  = {};
    this._shopEmergencyAlertUsedWave = {};
    this._shopPendingPurchases.clear();
    this._peptTalkBoostUntil         = 0;
    // 清理 spotlight 过期定时器
    for (const t of Object.values(this._shopSpotlightTimers || {})) clearTimeout(t);
    this._shopSpotlightTimers        = {};

    // F8: 无效指令提示去重每局重置 (策划案 §34 F8)
    this._shopInvalidCmdHintSent     = {};

    this.broadcast({ type: 'survival_game_state', timestamp: Date.now(), data: this.getFullState() });
  }

  /** WS客户端连接时发送当前状态 */
  getFullState() {
    return {
      // §4.2 v1.27 recovery 规范化：客户端只认 day/night 两态（HandleGameState 无 case "recovery"）；
      //   服务端内部保留 state='recovery'，对外转为 'day' + variant='recovery'（与 phase_changed 一致）
      state:        this.state === 'recovery' ? 'day' : this.state,
      day:          this.currentDay,
      remainingTime: Math.round(this.remainingTime),
      food:         Math.round(this.food),
      coal:         Math.round(this.coal),
      ore:          Math.round(this.ore),
      furnaceTemp:  Math.round(this.furnaceTemp * 10) / 10,
      gateHp:       Math.round(this.gateHp),
      gateMaxHp:    this.gateMaxHp,
      gateLevel:    this.gateLevel,
      scorePool:    Math.round(this.scorePool),
      workerHp:     Object.fromEntries(
        Object.entries(this._workerHp).map(([pid, w]) => [pid, {
          hp: w.hp, maxHp: w.maxHp, isDead: w.isDead, respawnAt: w.respawnAt
        }])
      ),

      // §36 全服同步 / 赛季 / 堡垒日 / 每日 cap（客户端 survival_game_state 同步）
      fortressDay:           this.fortressDay || 1,
      maxFortressDay:        this.maxFortressDay || 1,
      seasonId:              this.seasonMgr ? this.seasonMgr.seasonId : 1,
      seasonDay:             this.seasonMgr ? this.seasonMgr.seasonDay : 1,
      themeId:               this.seasonMgr ? this.seasonMgr.themeId : 'classic_frozen',
      phase:                 this.globalClock ? this.globalClock._phase : (this.state === 'night' ? 'night' : 'day'),
      phaseRemainingSec:     this.globalClock ? this.globalClock.getPhaseRemainingSec() : Math.round(this.remainingTime),
      // §4.2 v1.27 recovery variant：客户端重连时通过 variant 区分"普通白天 vs 恢复期白天"，默认 'normal'
      variant:               this.state === 'recovery' ? 'recovery' : 'normal',
      // §36.5.1 每日闯关上限 4 字段
      dailyFortressDayGained: this._dailyFortressDayGained || 0,
      dailyCapMax:            DAILY_FORTRESS_CAP_MAX,
      dailyResetAt:           this._getNextDailyResetMs(),
      dailyCapBlocked:        this._dailyCapBlocked || false,
    };
  }

  /** 停止游戏引擎（房间被销毁时调用） */
  pause() {
    this._clearAllTimers();
  }

  resume() {
    // 连接恢复后重新推送当前状态
    this.broadcast({ type: 'survival_game_state', timestamp: Date.now(), data: this.getFullState() });
  }

  // ==================== 抖音事件入口 ====================

  /**
   * 处理评论（弹幕指令）
   * content "1"-"5" → work_command
   * content "6"     → attack（夜晚有效）
   */
  handleComment(playerId, playerName, avatarUrl, content) {
    if (this.state !== 'day' && this.state !== 'night') return;
    const content_trim = (content || '').trim();

    // ── §37.3 建造投票弹幕：`建X`（X=1..options.length，对应活跃投票的候选列表）──
    // 投票不活跃时静默忽略（避免与 cmd 1-6 冲突由 `^建\d+$` 限定）；活跃时调用 handleBuildVote
    const buildMatch = content_trim.match(/^建(\d{1,2})$/);
    if (buildMatch) {
      if (!this._buildVote) return;
      const idx = parseInt(buildMatch[1]) - 1;
      if (idx >= 0 && idx < this._buildVote.options.length) {
        this.handleBuildVote(playerId, this._buildVote.proposalId, this._buildVote.options[idx]);
      }
      return;
    }

    // ── §39.4 商店购买弹幕：`买A<n>` / `买B<n>`（严格正则）──
    // A1~4 / B1~8 → 查表 → handleShopPurchase（无 pendingId，弹幕意图已显式）
    // B9/B10 退化：当期赛季未配置 → 推送 shop_purchase_failed { reason: 'item_not_found' }
    // 任何越界静默忽略
    const shopMatch = content_trim.match(/^买([AB])(\d{1,2})$/);
    if (shopMatch) {
      if (!playerId) return;
      const cat = shopMatch[1];
      const idx = parseInt(shopMatch[2]);
      let itemId = null;
      if (cat === 'A' && idx >= 1 && idx <= 4) {
        itemId = SHOP_A_INDEX[idx - 1];
      } else if (cat === 'B' && idx >= 1 && idx <= 8) {
        itemId = SHOP_B_INDEX[idx - 1];
      } else if (cat === 'B' && (idx === 9 || idx === 10)) {
        // PM MVP 决策：B9/B10 赛季限定 SKU 跳过，currentSeasonShopPool 始终空 → item_not_found
        this._shopFailPurchase('item_not_found', `买${cat}${idx}`);
        return;
      }
      if (itemId) {
        this.handleShopPurchase(playerId, playerName, itemId, null);
      }
      // 越界（idx<1 或 A>=5）静默忽略
      return;
    }

    // ── §39.5 装备切换弹幕：`装XY`（X=T|F|E|B，Y=数字）──
    // T=title, F=frame, E=entrance, B=barrage；Y=0 卸下；Y>=1 装上对应 SKU
    // 同样 2s 冷却，越界静默忽略
    const equipMatch = content_trim.match(/^装([TFEB])(\d{1,2})$/);
    if (equipMatch) {
      if (!playerId) return;
      const slotLetter = equipMatch[1];
      const slot = SHOP_EQUIP_SLOT_MAP[slotLetter];
      if (!slot) return;
      const idx = parseInt(equipMatch[2]);
      let itemId = null;
      if (idx === 0) {
        itemId = '';  // 卸下
      } else if (slot === 'title' && idx >= 1 && idx <= 3) {
        itemId = ['title_supporter', 'title_veteran', 'title_legend_mover'][idx - 1];
      } else if (slot === 'frame' && idx >= 1 && idx <= 2) {
        itemId = ['frame_bronze', 'frame_silver'][idx - 1];
      } else if (slot === 'entrance' && idx === 1) {
        itemId = 'entrance_spark';
      } else if (slot === 'barrage' && idx >= 1 && idx <= 2) {
        itemId = ['barrage_glow', 'barrage_crown'][idx - 1];
      } else {
        return;  // 越界静默忽略
      }
      this.handleShopEquip(playerId, slot, itemId);
      return;
    }

    // ── §30.7 换肤弹幕：`换肤` / `换肤N`（1~10）──────────────────────────
    // 必须在数字指令解析前处理（regex 仅匹配 `换肤` 前缀，不会与 cmd 1-6 冲突）
    const skinMatch = content_trim.match(/^换肤(\d{1,2})?$/);
    if (skinMatch) {
      if (!playerId) return; // 匿名不响应
      const nStr = skinMatch[1];
      const nowTier = this._getWorkerTier(playerId);
      if (!nStr) {
        const unlocked = TIER_SKINS.slice(0, nowTier);
        this.broadcast({
          type: 'bobao',
          timestamp: Date.now(),
          data: { message: `${playerName} 已解锁皮肤：${unlocked.join('/')}（阶1~${nowTier}）` }
        });
        return;
      }
      const targetTier = parseInt(nStr);
      if (isNaN(targetTier) || targetTier < 1 || targetTier > 10) return;
      if (targetTier > nowTier) {
        this.broadcast({
          type: 'bobao',
          timestamp: Date.now(),
          data: { message: `${playerName} 未解锁阶${targetTier}皮肤（当前阶${nowTier}）` }
        });
        return;
      }
      const cd = this._playerSkinCooldown[playerId] || 0;
      if (Date.now() - cd < 60000) return;  // 60s 冷却静默丢弃
      this._playerSkinCooldown[playerId] = Date.now();
      this._playerSkinId[playerId] = TIER_SKINS[targetTier - 1];
      this._broadcast({
        type: 'worker_skin_changed',
        data: {
          playerId, playerName: playerName || playerId,
          tier: targetTier, skinId: TIER_SKINS[targetTier - 1],
        }
      });
      console.log(`[SurvivalEngine] Skin changed: ${playerName} → 阶${targetTier} ${TIER_SKINS[targetTier - 1]}`);
      return;
    }

    const cmd = parseInt(content_trim);

    // F8: 无效指令提示 (策划案 §34 F8) —— 防止观众刷"5"或白天"6"时沉默以为游戏坏了
    // 每位玩家每种提示类型每局最多一次；匿名（无 playerId）静默丢弃
    if (playerId) {
      if (content_trim === '5') {
        const key = `${playerId}:cmd5`;
        if (!this._shopInvalidCmdHintSent[key]) {
          this._shopInvalidCmdHintSent[key] = true;
          this.broadcast({
            type: 'bobao',
            timestamp: Date.now(),
            data: { message: `${playerName || playerId}：指令 5 不存在，发 1-4 采集资源或发 6 攻击怪物` },
          });
        }
        return;
      }
      if (cmd === 6 && this.state === 'day') {
        const key = `${playerId}:cmd6day`;
        if (!this._shopInvalidCmdHintSent[key]) {
          this._shopInvalidCmdHintSent[key] = true;
          this.broadcast({
            type: 'bobao',
            timestamp: Date.now(),
            data: { message: `${playerName || playerId}：白天是采集时间！发 1-4 分配矿工` },
          });
        }
        return;
      }
    }

    // §33 助威模式分流：
    // - 已是助威者 → 走助威者分支（优先判断，避免 _trackContribution 污染后被误判为守护者）
    // - 已是正式守护者（contributions[playerId] !== undefined）→ 刷新活跃时间，走现有逻辑
    // - 还有守护者名额（totalPlayers < MAX_PLAYERS） → 绑定为守护者，走现有逻辑
    // - 名额已满 → 进入助威者分支（_handleSupporterComment 内幂等注册 + 推 supporter_joined）
    // §38：助威者/非 666 情况下，`探`/`召回` 也需要走到下方分支（_handleExpeditionSend 内部会返 supporter_not_allowed）
    const isExpeditionCmd = (content_trim === '探' || content_trim === '召回');
    if (playerId) {
      if (this._supporters.has(playerId)) {
        if (cmd >= 1 && cmd <= 6) {
          this._handleSupporterComment(playerId, playerName, cmd);
        }
        // 助威者 666 与守护者一致（效果本身就是全局的）→ 继续下方 666 分支
        // 助威者 `探`/`召回` 也放行到下方，由 _handleExpeditionSend 返回 supporter_not_allowed
        if (content_trim !== '666' && !isExpeditionCmd) return;
      } else if (this.contributions[playerId] !== undefined) {
        // 已是正式守护者
        this._guardianLastActive[playerId] = Date.now();
      } else if (this.totalPlayers < SurvivalGameEngine.MAX_PLAYERS) {
        // 有名额 → 绑定为守护者（与 handlePlayerJoined 对齐）
        this.contributions[playerId] = 0;
        this.playerNames[playerId]   = playerName || playerId;
        this.totalPlayers++;
        this._guardianLastActive[playerId] = Date.now();
        this._allocatePlayerSlot(playerId); // §33: 稳定槽位分配
      } else {
        // 名额已满 → 进入助威者分支
        if (cmd >= 1 && cmd <= 6) {
          this._handleSupporterComment(playerId, playerName, cmd);
        }
        // 同上：`探`/`召回` 放行到下方发 supporter_not_allowed
        if (content_trim !== '666' && !isExpeditionCmd) return;
      }
    }

    if (cmd >= 1 && cmd <= 4) {
      // 工作指令 1-4（策划案 v2.0 已移除指令5修城门）
      const cmdNames  = ['', 'food', 'coal', 'ore', 'heat'];
      const cmdLabels = ['', '采集食物', '挖掘煤炭', '开采矿石', '添柴升温'];

      const data = {
        playerId,
        playerName,
        avatarUrl: avatarUrl || '',
        commandId: cmd,
        commandName: cmdNames[cmd],
      };

      // 工作指令 → 转发给客户端，客户端驱动奶牛动画
      this.broadcast({
        type: 'work_command',
        timestamp: Date.now(),
        data
      });

      // 工作效果：小幅增加对应资源（服务器计算，不依赖客户端确认）
      this._applyWorkEffect(cmd, playerId);
      console.log(`[SurvivalEngine] work_command: ${playerName} → ${cmdLabels[cmd]}`);
    } else if (cmd === 6) {
      // 攻击指令（夜晚有效）
      if (this.state === 'night') {
        this._handleAttack(playerId, playerName);
        this._tribeWarAddEnergy(1);  // §35 攻击能量：cmd=6 夜战 +1
      }
    } else if (content_trim === '探') {
      // §38 探险指令：派出自己的矿工外出探险
      if (!playerId) return;
      this._handleExpeditionSend(playerId, playerName);
    } else if (content_trim === '召回') {
      // §38 召回指令：仅主播可用，立即拉回在外探险矿工（空手而归）
      if (!playerId) return;
      this._handleExpeditionRecall(playerId, playerName);
    } else if (content_trim === '666') {
      // 666弹幕：全员效率+15%（已建 altar 时 1.25），持续30秒（策划案 §4.3 / §37.2 altar）
      // §37 altar 抬升 666 触发值：1.15 → 1.25（未拆分 P5 版，用 Math.max 合并 T2 ability_pill 的 1.5/1.6）
      this.efficiency666Bonus = this._buildings.has('altar') ? 1.25 : 1.15;
      // §38 meteor_fragment：若 pending，本次 666 ×2.0（单次消费）
      if (this._meteorFragmentPending) {
        this.efficiency666Bonus = this.efficiency666Bonus * 2.0;
        this._meteorFragmentPending = false;
        console.log(`[SurvivalEngine] meteor_fragment consumed: 666 bonus boosted to ${this.efficiency666Bonus}`);
      }
      this.efficiency666Timer = 30;
      this._trackContribution(playerId, 2, 'barrage');
      this._tribeWarAddEnergy(1);  // §35 攻击能量：666 弹幕 +1
      this.broadcast({ type: 'special_effect', timestamp: Date.now(), data: { effect: 'glow_all', duration: 3 } });
      console.log(`[SurvivalEngine] 666 bonus activated by ${playerName}, efficiency +${Math.round((this.efficiency666Bonus-1)*100)}% for 30s`);
    }
  }

  /**
   * 处理礼物（策划案 §5.1 七种礼物体系）
   * giftId   — 抖音平台 gift_id（douyin_id，当前均为 TBD，对齐后可精确匹配）
   * giftValue — 礼物价格，单位：分（1分=0.01元=0.1抖币）
   */
  handleGift(playerId, playerName, avatarUrl, giftId, giftValue, giftName) {
    // 注册/更新送礼玩家的名字（排行榜结算时使用）
    if (playerId && !this.playerNames[playerId])
      this.playerNames[playerId] = playerName || playerId;

    // §33 助威模式：名额已满且未注册 → 先晋升为助威者，再进入礼物处理
    //   PM 决策（MVP）：跳过 §36.12 seasonDay < supporter_mode.minDay 门槛，永远允许注册
    //   TODO §36.12: add seasonDay < supporter_mode.minDay gate when §36 implemented
    if (playerId
        && this.contributions[playerId] === undefined
        && !this._supporters.has(playerId)
        && this.totalPlayers >= SurvivalGameEngine.MAX_PLAYERS) {
      this._promoteToSupporter(playerId, playerName);
    }
    const isSupporter = playerId ? this._supporters.has(playerId) : false;

    // §33.5 守护者送礼属于活跃行为，刷新 AFK 计时
    if (playerId && !isSupporter && this.contributions[playerId] !== undefined) {
      this._guardianLastActive[playerId] = Date.now();
    }

    // 优先按 douyin_id 查找，TBD 期间回退到按 price_fen 查找
    let gift = findGiftById(giftId);
    if (!gift) gift = getGift(giftId);          // 模拟/测试时用内部ID直接匹配
    if (!gift) gift = findGiftByPrice(giftValue);

    // 完全未知的礼物：忽略，不产生任何游戏效果也不计入贡献/积分池
    if (!gift) {
      console.log(`[SurvivalEngine] unknown gift ignored: ${giftName || giftId} (id=${giftId}, val=${giftValue})`);
      return;
    }

    // ===== §33 助威者特殊礼物分支：T1 仙女棒 / T5 爱的爆炸 =====
    // 这两种礼物依赖"发送者自己有专属矿工"的假设，助威者无矿工 → 需要重路由到场上其他矿工
    if (isSupporter && gift.id === 'fairy_wand') {
      this._handleSupporterFairyWand(playerId, playerName, avatarUrl, gift, giftValue);
      return;
    }
    if (isSupporter && gift.id === 'love_explosion') {
      this._handleSupporterLoveExplosion(playerId, playerName, avatarUrl, gift, giftValue);
      return;
    }

    // 贡献值 = 礼物得分（策划案 §9）
    this._trackContribution(playerId, gift.score, 'gift');
    // §35 Tribe War 攻击能量（策划案 §35.3）：T1=1 / T2=5 / T3=10 / T4=20 / T5=50 / T6=100
    // 按 tier 映射（gift.tier 是 1~6）；兜底按 score 阈值
    const _tribeEnergyByTier = { 1: 1, 2: 5, 3: 10, 4: 20, 5: 50, 6: 100 };
    this._tribeWarAddEnergy(_tribeEnergyByTier[gift.tier] || 1);
    // 积分池：礼物得分入池（§37 market 抬升 ×1.1）
    const _marketMult = this._buildings.has('market') ? 1.1 : 1.0;
    this.scorePool += gift.score * _marketMult;

    // ── 矿工复活：送礼时若该玩家的矿工已死亡，立即复活 ──────────────
    //   助威者无 _workerHp 条目，本段自然跳过（?.isDead 为 undefined）
    if (this.state === 'night' && this._workerHp[playerId]?.isDead) {
      this._reviveWorker(playerId);
      console.log(`[SurvivalEngine] Gift revival: ${playerName}'s worker revived`);
    }

    // 每种礼物的具体效果（策划案 §5.1）
    const effects = {};
    let needsResourceSync = false;

    switch (gift.id) {

      case 'fairy_wand': {
        // 发送者效率永久+5%（叠加，上限+100%）
        const prev = this._playerEfficiencyBonus[playerId] || 0;
        this._playerEfficiencyBonus[playerId] = Math.min(1.0, prev + 0.05);
        effects.efficiencyBonus = this._playerEfficiencyBonus[playerId];
        break;
      }

      case 'ability_pill': {
        // 全员采矿效率+50%，持续30s（复用 efficiency666 系统）
        // §37 altar 抬升 T2 触发值：1.5 → 1.6（与 666 同一独立轨道，未拆分 P5 版）
        const pillVal = this._buildings.has('altar') ? 1.6 : 1.5;
        this.efficiency666Bonus = Math.max(this.efficiency666Bonus, pillVal); // +50%/60%，不叠加只刷新
        this.efficiency666Timer = 30;
        effects.globalEfficiencyBoost = pillVal;
        effects.globalEfficiencyDuration = 30;
        needsResourceSync = true;
        console.log(`[SurvivalEngine] ability_pill: global efficiency +${((pillVal-1)*100)|0}% for 30s by ${playerName}`);
        break;
      }

      case 'donut': {
        // 城门修复+200HP，全局食物+100
        this.gateHp = Math.min(this.gateMaxHp, this.gateHp + 200);
        this.food   = Math.min(2000, this.food + 100);
        effects.addGateHp = 200;
        effects.addFood   = 100;
        needsResourceSync = true;
        break;
      }

      case 'energy_battery': {
        // 炉温+30℃（共享资源），发送者效率+30%（持续180秒，仅影响自己矿工，刷新计时）
        this.furnaceTemp = Math.min(this.maxTemp, this.furnaceTemp + 30);
        this._playerTempBoost[playerId] = 1.3;
        if (this._playerTempBoostTimers[playerId]) clearTimeout(this._playerTempBoostTimers[playerId]);
        this._playerTempBoostTimers[playerId] = setTimeout(() => {
          this._playerTempBoost[playerId] = 1.0;
          delete this._playerTempBoostTimers[playerId];
        }, 180000);
        effects.addHeat           = 30;
        effects.tempBoost         = 1.3;
        effects.boostDuration     = 180;
        needsResourceSync = true;
        break;
      }

      case 'mystery_airdrop': {
        // 超级补给：食物+500、煤炭+200、矿石+100、城门+300HP；触发 GIFT_PAUSE 3000ms
        this.food    = Math.min(2000, this.food    + 500);
        this.coal    = Math.min(1500, this.coal    + 200);
        this.ore     = Math.min(800, this.ore     + 100);
        this.gateHp  = Math.min(this.gateMaxHp, this.gateHp + 300);
        effects.addFood   = 500;
        effects.addCoal   = 200;
        effects.addOre    = 100;
        effects.addGateHp = 300;
        effects.giftPause = 3000;
        this.broadcast({ type: 'gift_pause', timestamp: Date.now(), data: { duration: 3000 } });
        needsResourceSync = true;
        break;
      }

      case 'love_explosion': {
        // 爱的爆炸：全体怪物AOE伤害200、所有矿工HP全满恢复、城门+200HP
        const aoeDmg = 200;
        const killed = [];
        for (const [mid, m] of this._activeMonsters) {
          m.currentHp -= aoeDmg;
          if (m.currentHp <= 0) killed.push(mid);
        }
        for (const mid of killed) {
          const m = this._activeMonsters.get(mid);
          this._activeMonsters.delete(mid);
          this._broadcast({ type: 'monster_died', data: { monsterId: mid, monsterType: m.type, killerId: playerId } });
        }
        effects.aoeDamage     = aoeDmg;
        effects.monstersKilled = killed.length;

        // 只治疗/复活发送者自己的矿工（不影响其他人）
        const myWorker = this._workerHp[playerId];
        if (myWorker) {
          if (myWorker.isDead) {
            this._reviveWorker(playerId);
            effects.revivedWorkers = [playerId];
          } else {
            myWorker.hp = myWorker.maxHp;
            effects.healedWorkers = 1;
          }
        }

        // 城门+200HP
        this.gateHp = Math.min(this.gateMaxHp, this.gateHp + 200);
        effects.addGateHp = 200;
        needsResourceSync = true;
        break;
      }
    }

    const giftData = {
      playerId,
      playerName,
      avatarUrl:  avatarUrl || '',
      giftId:     gift.id,
      giftName:   gift.name_cn,
      giftTier:   getTierNumber(gift.tier),   // 数字 1-5，保持与客户端兼容
      giftTierStr: gift.tier,                 // 'T1'~'T5'，供扩展使用
      giftValue,
      score:      gift.score,
      contribution: gift.score,               // Unity SurvivalGiftData.contribution 字段别名
      // 礼物直接资源加成（扁平化，供 Unity 直接反序列化）
      addFood:    effects.addFood   || 0,
      addCoal:    effects.addCoal   || 0,
      addOre:     effects.addOre    || 0,
      addHeat:    effects.addHeat   || 0,
      addGateHp:  effects.addGateHp || 0,
      effects,
    };

    this.broadcast({ type: 'survival_gift', timestamp: Date.now(), data: giftData });

    if (needsResourceSync) this._broadcastResourceUpdate();
    console.log(`[SurvivalEngine] gift: ${playerName} → ${gift.name_cn} (${gift.tier}, score=${gift.score})`);
  }

  /**
   * 处理新玩家加入（评论首次出现）
   */
  handlePlayerJoined(playerId, playerName, avatarUrl) {
    if (!this.contributions[playerId]) {
      // §33 助威模式：满员时降级为助威者（替代原"静默丢弃"行为）
      if (this.totalPlayers >= SurvivalGameEngine.MAX_PLAYERS) {
        if (playerId && !this._supporters.has(playerId)) {
          this._promoteToSupporter(playerId, playerName);
        }
        return;
      }
      this.contributions[playerId] = 0;
      this.playerNames[playerId] = playerName || playerId;  // 记录玩家名，结算时填入排行榜
      this.totalPlayers++;
      // 守护者活跃时间初始化（供 §33.5 AFK 替补检测使用）
      this._guardianLastActive[playerId] = Date.now();
      this._allocatePlayerSlot(playerId); // §33: 稳定槽位分配（供 supporter_promoted 使用）
    }

    const data = {
      playerId,
      playerName,
      avatarUrl: avatarUrl || '',
      totalPlayers: this.totalPlayers,
    };
    this.broadcast({ type: 'survival_player_joined', timestamp: Date.now(), data });

    // §39 商店:玩家首次加入/重连时推送其 owned + equipped 快照(ShopUI 背包 Tab 依赖)
    // 整体复核 Critical #2:原实现缺此广播,导致重连后 B 类装备不可见
    const owned    = this._playerShopInventory[playerId] || [];
    const equipped = this._playerShopEquipped[playerId]  || {};
    this.broadcast({
      type: 'shop_inventory_data',
      timestamp: Date.now(),
      data: {
        playerId,
        owned: Array.isArray(owned) ? owned.slice() : [],
        equipped: {
          title:    equipped.title    || '',
          frame:    equipped.frame    || '',
          entrance: equipped.entrance || '',
          barrage:  equipped.barrage  || '',
        },
      },
    });
  }

  // ==================== §33 助威模式 ====================

  /**
   * 晋升为助威者（幂等；可供 handleComment/handleGift/替补流程共用入口）。
   * 返回 true 表示已注册或已是助威者；false 表示因 §36.12 门槛不满足而保留旁观者身份。
   *
   * PM 决策（MVP v1.27）：跳过 §36.12 seasonDay < supporter_mode.minDay 门槛，永远允许注册。
   * 同样本次不推送 gift_silent_fail（门槛未生效，silent_fail 分支永远不触发）。
   */
  _promoteToSupporter(playerId, playerName) {
    if (!playerId) return false;
    // 幂等：已是助威者直接返回 true
    if (this._supporters.has(playerId)) return true;

    // TODO §36.12: add seasonDay < supporter_mode.minDay gate when §36 implemented
    // if (!this.room.isVeteran && this.room.seasonDay < FEATURE_UNLOCK_DAY.supporter_mode.minDay) return false;

    this._supporters.set(playerId, {
      name: playerName || playerId,
      joinedAt: Date.now(),
      totalContrib: 0,
    });
    // 保持 playerNames 缓存同步（live_ranking / 结算显示名共用）
    if (!this.playerNames[playerId]) this.playerNames[playerId] = playerName || playerId;

    this.broadcast({
      type: 'supporter_joined',
      timestamp: Date.now(),
      data: {
        playerId,
        playerName: playerName || playerId,
        supporterCount: this._supporters.size,
      },
    });
    console.log(`[SurvivalEngine] Supporter joined: ${playerName || playerId} (total=${this._supporters.size})`);
    return true;
  }

  /**
   * 处理助威者弹幕（§33.3 约定的 20-30% 效果 + 2s 全局冷却防刷）
   * @param {string} playerId
   * @param {string} playerName
   * @param {number} cmd  1~4 工作指令 / 6 夜晚攻击
   */
  _handleSupporterComment(playerId, playerName, cmd) {
    // 幂等保底：极端路径（如替补晋升）可能跳过 _promoteToSupporter
    if (!this._promoteToSupporter(playerId, playerName)) return;

    // 同一 cmd 全局冷却 2s（防 200 人直播间同时刷同一指令）
    const now = Date.now();
    if (now - (this._supporterCmdCooldown[cmd] || 0) < 2000) return;
    this._supporterCmdCooldown[cmd] = now;

    // 助威效果（§33.3 表）
    switch (cmd) {
      case 1: this.food = Math.min(2000, this.food + 1); break;
      case 2: this.coal = Math.min(1500, this.coal + 1); break;
      case 3: this.ore  = Math.min(800,  this.ore  + 1); break;
      case 4: this.furnaceTemp = Math.min(this.maxTemp, this.furnaceTemp + 0.5); break;
      case 6:
        if (this.state === 'night') {
          this._supporterAtkBuff = Math.min(0.20, this._supporterAtkBuff + 0.02);
          this._supporterAtkBuffTimer = 5;
        }
        break;
      default: return; // 其他 cmd 不生效，也不计贡献
    }

    // 助威者贡献值 +1（与正式守护者一致），计入 live_ranking
    this._trackContribution(playerId, 1);

    this.broadcast({
      type: 'supporter_action',
      timestamp: Date.now(),
      data: { playerId, playerName: playerName || playerId, cmd },
    });
  }

  /**
   * 助威者送 T1 仙女棒：随机挑一名在场守护者挂载 +0.05 效率（上限 1.0）。
   * 贡献值 +1，积分池 +1。
   */
  _handleSupporterFairyWand(playerId, playerName, avatarUrl, gift, giftValue) {
    const effects = { efficiencyBonus: 0, supporterRedirect: true };
    const guardians = Object.keys(this.contributions);
    if (guardians.length > 0) {
      const targetId = guardians[Math.floor(Math.random() * guardians.length)];
      const prev = this._playerEfficiencyBonus[targetId] || 0;
      this._playerEfficiencyBonus[targetId] = Math.min(1.0, prev + 0.05);
      effects.efficiencyBonus = this._playerEfficiencyBonus[targetId];
      effects.redirectTargetId   = targetId;
      effects.redirectTargetName = this.playerNames[targetId] || targetId;
    }

    // 贡献与积分池（不走 handleGift 的默认累加路径，避免重复计入）
    // §37 market 抬升 ×1.1
    this._trackContribution(playerId, 1);
    this.scorePool += 1 * (this._buildings.has('market') ? 1.1 : 1.0);

    const giftData = {
      playerId,
      playerName,
      avatarUrl: avatarUrl || '',
      giftId:     gift.id,
      giftName:   gift.name_cn,
      giftTier:   getTierNumber(gift.tier),
      giftTierStr: gift.tier,
      giftValue,
      score:      gift.score,
      contribution: gift.score,
      addFood: 0, addCoal: 0, addOre: 0, addHeat: 0, addGateHp: 0,
      effects,
    };
    this.broadcast({ type: 'survival_gift', timestamp: Date.now(), data: giftData });
    console.log(`[SurvivalEngine] supporter gift: ${playerName} → ${gift.name_cn} redirected to ${effects.redirectTargetName || '(no guardian)'}`);
  }

  /**
   * 助威者送 T5 爱的爆炸：AOE + 城门 HP 与守护者一致；复活逻辑改为"随机一名已死亡矿工"。
   * 贡献值 +2000，积分池 +2000。
   */
  _handleSupporterLoveExplosion(playerId, playerName, avatarUrl, gift, giftValue) {
    const effects = { supporterRedirect: true };
    const aoeDmg  = 200;

    // AOE 全体怪物 -200 HP
    const killed = [];
    for (const [mid, m] of this._activeMonsters) {
      m.currentHp -= aoeDmg;
      if (m.currentHp <= 0) killed.push(mid);
    }
    for (const mid of killed) {
      const m = this._activeMonsters.get(mid);
      this._activeMonsters.delete(mid);
      this._broadcast({
        type: 'monster_died',
        data: { monsterId: mid, monsterType: m.type, killerId: playerId },
      });
    }
    effects.aoeDamage      = aoeDmg;
    effects.monstersKilled = killed.length;

    // 随机复活一名已死亡矿工（替代"仅发送者矿工"）
    const deadWorkers = Object.entries(this._workerHp)
      .filter(([, w]) => w && w.isDead)
      .map(([pid]) => pid);
    if (deadWorkers.length > 0) {
      const targetId = deadWorkers[Math.floor(Math.random() * deadWorkers.length)];
      this._reviveWorker(targetId);
      effects.revivedWorkers = [targetId];
    }

    // 城门 +200HP
    this.gateHp = Math.min(this.gateMaxHp, this.gateHp + 200);
    effects.addGateHp = 200;

    // 贡献 + 积分池（§37 market 抬升 ×1.1）
    this._trackContribution(playerId, gift.score);
    this.scorePool += gift.score * (this._buildings.has('market') ? 1.1 : 1.0);

    const giftData = {
      playerId,
      playerName,
      avatarUrl: avatarUrl || '',
      giftId:     gift.id,
      giftName:   gift.name_cn,
      giftTier:   getTierNumber(gift.tier),
      giftTierStr: gift.tier,
      giftValue,
      score:      gift.score,
      contribution: gift.score,
      addFood: 0, addCoal: 0, addOre: 0, addHeat: 0,
      addGateHp: effects.addGateHp,
      effects,
    };
    this.broadcast({ type: 'survival_gift', timestamp: Date.now(), data: giftData });
    this._broadcastResourceUpdate();
    console.log(`[SurvivalEngine] supporter T5 love_explosion by ${playerName}: killed ${killed.length} monsters, revived ${effects.revivedWorkers ? effects.revivedWorkers.length : 0} worker(s)`);
  }

  /**
   * AFK 替补检测：正式守护者连续 60s 无任何操作时，最早加入的助威者替补上场。
   * 每 10s 调用一次（_tick 内），每次最多替换 1 人，避免批量震荡。
   */
  _checkAfkReplacement() {
    if (this._supporters.size === 0) return;

    const now = Date.now();
    const AFK_THRESHOLD = 60000; // 60s

    // §38.6 兼容：预计算在外探险中的玩家集合，这些玩家不走 AFK 替补
    // （探险中矿工 gameObject 已 SetActive(false)，AFK 替换会污染 _expeditions 与 contributions）
    const expeditionPlayers = new Set();
    for (const exp of this._expeditions.values()) {
      if (exp && exp.playerId) expeditionPlayers.add(exp.playerId);
    }

    for (const [pid, lastActive] of Object.entries(this._guardianLastActive)) {
      // 豁免：主播永不替换（_roomCreatorId 未注入时视为无主播豁免）
      if (this._roomCreatorId && pid === this._roomCreatorId) continue;
      // 豁免：夜晚死亡等待复活中的矿工不替换
      if (this._workerHp[pid]?.isDead) continue;
      // §38 豁免：在外探险的玩家不替换（返程前矿工不在场，替补会导致幽灵 contributions）
      if (expeditionPlayers.has(pid)) continue;
      // 未到 AFK 阈值
      if (now - lastActive < AFK_THRESHOLD) continue;

      // 选最早加入且不在 30s 冷却期的助威者（joinedAt > now 表示冷却中）
      let earliestId = null;
      let earliestTime = Infinity;
      for (const [sid, sData] of this._supporters) {
        if (sData.joinedAt > now) continue; // 30s 冷却中，不可排队
        if (sData.joinedAt < earliestTime) {
          earliestId = sid;
          earliestTime = sData.joinedAt;
        }
      }
      if (!earliestId) return;

      this._promoteSupporter(earliestId, pid);
      return; // 每次只替换 1 人
    }
  }

  /**
   * 助威者替补上场（newId）/ 旧守护者降级为助威者（oldId，30s 冷却排队末尾）。
   */
  _promoteSupporter(newId, oldId) {
    const newData = this._supporters.get(newId);
    if (!newData) return;

    const oldName = this._getPlayerName(oldId);
    const newName = newData.name || this._getPlayerName(newId);
    const workerIndex = this._getWorkerIndex(oldId);

    // 旧守护者 → 助威者（保存当前贡献快照，30s 冷却）
    this._supporters.set(oldId, {
      name: oldName,
      joinedAt: Date.now() + 30000,
      totalContrib: this.contributions[oldId] || 0,
    });
    delete this.contributions[oldId];
    delete this._guardianLastActive[oldId];
    delete this._playerSlots[oldId]; // §33: 释放槽位供 newId 原位继承
    // 不减 totalPlayers：槽位由 newId 继承（见下）

    // 新助威者 → 守护者（继承 totalContrib 作为 contributions 起始值 + 继承 workerIndex 槽位）
    this.contributions[newId] = newData.totalContrib || 0;
    this.playerNames[newId]   = newName;
    this._guardianLastActive[newId] = Date.now();
    this._supporters.delete(newId);
    if (workerIndex >= 0) this._playerSlots[newId] = workerIndex; // 原位继承

    this.broadcast({
      type: 'supporter_promoted',
      timestamp: Date.now(),
      data: {
        newPlayerId:   newId,
        newPlayerName: newName,
        oldPlayerId:   oldId,
        oldPlayerName: oldName,
        workerIndex,
      },
    });
    console.log(`[SurvivalEngine] Supporter promoted: ${newName} replaces ${oldName} (workerIndex=${workerIndex})`);
  }

  /** 获取玩家名（优先 playerNames，fallback 到 supporters.name，再 fallback 到 playerId） */
  _getPlayerName(playerId) {
    if (!playerId) return '';
    if (this.playerNames[playerId]) return this.playerNames[playerId];
    const s = this._supporters.get(playerId);
    if (s && s.name) return s.name;
    return playerId;
  }

  /** 获取玩家的 workerIndex（0~MAX_PLAYERS-1），未找到返回 -1 */
  _getWorkerIndex(playerId) {
    if (!playerId) return -1;
    const slot = this._playerSlots[playerId];
    return (typeof slot === 'number') ? slot : -1;
  }

  /** 找一个空槽位（0~MAX_PLAYERS-1）分配给新守护者；满员返回 -1 */
  _allocatePlayerSlot(playerId) {
    if (!playerId) return -1;
    if (typeof this._playerSlots[playerId] === 'number') return this._playerSlots[playerId];
    const occupied = new Set(Object.values(this._playerSlots));
    for (let i = 0; i < SurvivalGameEngine.MAX_PLAYERS; i++) {
      if (!occupied.has(i)) {
        this._playerSlots[playerId] = i;
        return i;
      }
    }
    return -1;
  }

  // ==================== 内部：阶段切换 ====================

  _enterDay(day) {
    this._clearNightWaves();
    this._activeMonsters.clear();
    this.state         = 'day';

    // 天亮时立即复活所有死亡矿工
    this._reviveAllWorkers('day_started');
    this.currentDay    = day;
    this.remainingTime = this.dayDuration;

    // 每天开始时重置随机事件计时器（90-120s后触发首次事件）
    this._nextEventTimer = 90 + Math.random() * 30;

    // §37 建造系统：每日重置投票限额（每日 1 次跨日重置）
    this._buildVoteUsedToday = false;

    // §24.4 首次进入 Running（day=1）→ 轮盘立即就绪（策划案 §24.4.2）
    if (!this._rouletteRunningInited) {
      this._rouletteRunningInited = true;
      this._roulette.onReadyAtRunningStart();
    }

    console.log(`[SurvivalEngine] ===== Day ${day} Start =====`);

    this.broadcast({
      type: 'phase_changed',
      timestamp: Date.now(),
      data: {
        phase: 'day',
        day,
        phaseDuration: this.dayDuration,
      }
    });

    // 同步完整状态
    this.broadcast({ type: 'survival_game_state', timestamp: Date.now(), data: this.getFullState() });

    this._startTick();
  }

  _enterNight(day) {
    this.state         = 'night';
    this.currentDay    = day;
    this.remainingTime = this.nightDuration;

    console.log(`[SurvivalEngine] ===== Night ${day} Start =====`);

    // §30.6 动态难度：每夜进入前刷新（避免每 tick 都算）
    // §30.3 阶10 传奇免死每晚 1 次 → 重置标记
    this._legendReviveUsed = {};
    this._updateDynamicDifficulty();

    // 初始化矿工HP（夜晚开始时全员满血上阵）
    this._initWorkerHp();

    // §38.6 夜晚兜底 KIA：必须在 _initWorkerHp 之后执行，否则 died=true 写入会被满血重建覆盖
    this._sweepExpeditionsOnNightStart(Date.now());

    // 初始化当天怪物追踪
    this._initActiveMonsters(day);

    this.broadcast({
      type: 'phase_changed',
      timestamp: Date.now(),
      data: {
        phase: 'night',
        day,
        phaseDuration: this.nightDuration,
      }
    });

    this.broadcast({ type: 'survival_game_state', timestamp: Date.now(), data: this.getFullState() });

    // §16 v1.27 tick 幂等重启：从 recovery 过来时 tick 仍是轻量分支（recovery），需切换到完整分支；
    //   从 _enterDay 过来时 tick 已在跑（_startTick 内部 _clearTick 保证幂等，无副作用）。
    //   这一行保障 _decayResources / _checkDefeat / remainingTime / 666buff / ore 修门 / 助威衰减
    //   全部在夜晚正常运转（修复 Reviewer Round 1 Critical：tick 断链）。
    this._startTick();

    // 播报 Boss 出现
    const cfg = getWaveConfig(day);
    if (cfg.boss) {
      this.broadcast({
        type: 'boss_appeared',
        timestamp: Date.now(),
        data: {
          day,
          bossHp:  cfg.boss.hp,
          bossAtk: cfg.boss.atk,
        }
      });
    }

    // 3秒延迟后再启动夜晚怪物波次（给客户端过渡动画播放时间）
    const nightStartDelay = setTimeout(() => {
      if (this.state === 'night') {
        this._scheduleNightWaves(day);
      }
    }, 3000);
    this._waveTimers.push(nightStartDelay);

    // §35 Tribe War：本房间若正在被其他房间攻击 → 通知 TribeWarManager 释放远征怪
    // （延迟 4s 跟随 nightStartDelay，保证夜晚 wave 调度先起；manager 内部 no-op 若未被攻击）
    if (this.tribeWarMgr) {
      const rid = this._getRoomId();
      if (rid) {
        const tribeWarRelease = setTimeout(() => {
          if (this.state === 'night' && this.tribeWarMgr) {
            try { this.tribeWarMgr.onDefenderEnterNight(rid); } catch (e) { /* ignore */ }
          }
        }, 4000);
        this._waveTimers.push(tribeWarRelease);
      }
    }
  }

  /**
   * 阶段性结算（§16.6）—— 🆕 v1.26 永续模式改造：
   *   - 单参数 `reason`（无 result）：'food_depleted' / 'temp_freeze' / 'gate_breached' / 'all_dead' / 'manual'
   *   - 固定 payoutRate = 0.3（无胜利分支）
   *   - manual：跳过 _demoteBuildings / _onRoomFail（fortressDay 不变，也不广播 room_failed）
   *   - 结算数据新增 fortressDayBefore / fortressDayAfter / newbieProtected（§16.6）
   *   - ~8s 后自动进入恢复期（_enterRecovery），不再 reset + startGame
   */
  _enterSettlement(reason) {
    if (this.state === 'settlement' || this.state === 'recovery') return;

    // §16.4 manual：GM 主动终止不触发降级；其他 reason 均为失败，走降级路径
    const isManual = (reason === 'manual');

    // §37.5 失败降级（manual 跳过）：按 BUILDING_KEEP_ON_DEMOTE 部分保留，其余清除
    if (!isManual) {
      this._demoteBuildings('demoted');
    }

    // §35 Tribe War：本房间进入结算 → 立即断开参与的所有 session（攻击方/防守方均断）
    if (this.tribeWarMgr) {
      const rid = this._getRoomId();
      if (rid) {
        try { this.tribeWarMgr.onRoomSettlement(rid); } catch (e) { /* ignore */ }
      }
    }

    this._clearAllTimers();
    this.state = 'settlement';

    // §16.6 fortressDayBefore：_onRoomFail 会修改 this.fortressDay，必须先缓存
    const fortressDayBefore = this.fortressDay || 1;

    const rankings = this._buildRankings();

    // ===== 积分池分配（§12.3 / §16.6）=====
    // 🆕 v1.26 固定 0.3（无胜利分支）；剩余流入下一周期（上限=本局积分池 50%）
    const payoutRate  = 0.3;
    const payoutTotal = Math.floor(this.scorePool * payoutRate);
    const rawCarryover = this.scorePool - payoutTotal;
    this._carryoverPool = Math.min(rawCarryover, Math.floor(this.scorePool * 0.5));

    // 排名权重（Top10：20/16/12/10/8/7/6/5/4/3，共91份）
    const weights     = [20, 16, 12, 10, 8, 7, 6, 5, 4, 3];
    const totalWeight = weights.slice(0, rankings.length).reduce((s, w) => s + w, 0) || 1;
    rankings.forEach((r, i) => {
      r.payout = Math.floor(payoutTotal * (weights[i] || 1) / totalWeight);
    });

    console.log(`[SurvivalEngine] ScorePool=${Math.round(this.scorePool)}, payout=${payoutTotal}(${(payoutRate*100)|0}%), carryover=${this._carryoverPool}`);

    // §36.5 堡垒日降级（manual 跳过）：新手保护 / 10% demote 公式 + room_failed 广播
    //   注意：先调 _onRoomFail（会修改 this.fortressDay）再读 fortressDayAfter
    if (!isManual) {
      try { this._onRoomFail(); } catch (e) {
        console.warn(`[SurvivalEngine] _onRoomFail error: ${e.message}`);
      }
    }
    const fortressDayAfter = this.fortressDay || fortressDayBefore;
    const newbieProtected  = fortressDayBefore <= FORTRESS_NEWBIE_PROTECT_DAY;

    // §16.6 data 字段（key 顺序严格按策划案表；移除 result，新增 fortressDay*/newbieProtected）
    const data = {
      reason,
      dayssurvived:      this.currentDay,
      fortressDayBefore,
      fortressDayAfter,
      newbieProtected,
      totalScore:        Object.values(this.contributions).reduce((a, b) => a + b, 0),
      rankings,
      // 积分池字段
      scorePool:         Math.round(this.scorePool),
      distributed:       payoutTotal,
      carryover:         this._carryoverPool,
      payoutRate,
    };

    this.broadcast({ type: 'survival_game_ended', timestamp: Date.now(), data });
    console.log(`[SurvivalEngine] Settlement: reason=${reason}, day=${this.currentDay}, fortressDay ${fortressDayBefore}→${fortressDayAfter} (newbie=${newbieProtected})`);

    // §36 持久化：结算 → 保存一次快照
    if (this.roomPersistence && this.room) {
      try { this.roomPersistence.save(this.room); } catch (e) { /* ignore */ }
    }

    // §16.2 step 1 + §16.4：~8s 结算 UI 展示后进入恢复期，不再 reset + startGame 自动重启
    this._recoveryTimer = setTimeout(() => {
      this._recoveryTimer = null;
      this._enterRecovery();
    }, 8000);
  }

  /**
   * 恢复期（§16.2 step 3 + §4.2 恢复期进入/离开）—— 🆕 v1.26：
   *   - 服务端独立 state='recovery'；客户端收 phase='day'，靠 variant='recovery' 区分
   *   - 120s 基础补给 + 城门满血 + 矿工复活；不启动新波次
   *   - 夜晚遗留 _activeMonsters 不清（已刷出的不蒸发；客户端继续可视化）
   *   - 120s 结束 → _enterNight(currentDay + 1)
   */
  _enterRecovery() {
    if (this.state === 'recovery') return;

    // ===== §16.2 note 周期边界清零（四件事同一同步帧内完成）=====
    // 1) carryover → 赋给新周期起始 scorePool（_carryoverPool 由 _enterSettlement 算好）
    this.scorePool      = this._carryoverPool || 0;
    this._carryoverPool = 0;

    // 2) weekly_ranking.addGameResult(contributions)：由 SurvivalRoom.broadcast 拦截
    //    survival_game_ended 消息时已同步执行（见 SurvivalRoom.js:240），此处无需重复调用

    // 3) contributions 清零：新周期从 0 累计
    this.contributions = {};

    // 4) §33 助威者 totalContrib 清零（Map 本身保留，身份位延续）
    if (this._supporters && typeof this._supporters.forEach === 'function') {
      for (const entry of this._supporters.values()) {
        if (entry) entry.totalContrib = 0;
      }
    }
    // ⚠️ _lifetimeContrib / _contribBalance 绝不清零（§30 / §39 跨周期永续）

    this.state = 'recovery';
    // §4.2 recovery 阶段时长 120s → remainingTime 供客户端倒计时（_tick 每秒递减）
    this.remainingTime = 120;
    // §3.1 流程：recovery 替代"失败周期那个白天"，currentDay 不推进（由 120s 结束后 _enterNight(currentDay+1) 推进）

    // ===== §16.2 资源基础补给（考虑 §5.1 上限）=====
    this.food = Math.min(2000, this.food + 100);
    this.coal = Math.min(1500, this.coal + 50);
    this.ore  = Math.min(800,  this.ore  + 20);

    // ===== 城门 HP 重置到当前等级上限 =====
    const gateMax = GATE_MAX_HP_BY_LEVEL[Math.max(0, (this.gateLevel || 1) - 1)]
                  || this.gateMaxHp
                  || 1000;
    this.gateMaxHp = gateMax;
    this.gateHp    = gateMax;

    // ===== 矿工全员复活（_reviveAllWorkers 内部刷新 _lastAliveAt）=====
    this._reviveAllWorkers('recovery');

    // ===== §4.2 广播 phase_changed（phase='day' + variant='recovery'，phaseDuration=120）=====
    this.broadcast({
      type: 'phase_changed',
      timestamp: Date.now(),
      data: {
        phase:         'day',
        day:           this.currentDay,
        phaseDuration: 120,
        variant:       'recovery',
      },
    });

    // 同步完整状态（客户端刷新资源条/城门/矿工 HP）
    this.broadcast({ type: 'survival_game_state', timestamp: Date.now(), data: this.getFullState() });

    console.log(`[SurvivalEngine] ===== Recovery Start (120s, day=${this.currentDay}) =====`);

    // ===== §16 v1.27 恢复期 tick 重启 =====
    //   _enterSettlement → _clearAllTimers 已清 tick；此处重启让 remainingTime 倒计时生效，
    //   客户端 5s resource_update 可读 120s → 0 倒计时。_tick 在 state='recovery' 下走轻量分支，
    //   不触发 decay / defeat / spawn / 666buff / 助威衰减。
    //   _enterNight → _initActiveMonsters 会清 _activeMonsters，此处不重复清理。
    this._startTick();

    // ===== 120s 后进入下一个夜晚（currentDay + 1）=====
    this._recoveryTimer = setTimeout(() => {
      this._recoveryTimer = null;
      this._enterNight((this.currentDay || 0) + 1);
    }, 120_000);
  }

  // ==================== 内部：Tick ====================

  _startTick() {
    this._clearTick();
    this._tickCounter = 0;
    this._tickTimer = setInterval(() => this._tick(), 200);

    // 5秒全量同步（独立定时器，避免被tick覆盖）
    // §16 v1.27：recovery 也纳入同步，客户端读 remainingTime 倒计时（120s → 0）
    this._resourceSyncTimer = setInterval(() => {
      if (this.state === 'day' || this.state === 'night' || this.state === 'recovery')
        this._broadcastResourceUpdate();
    }, 5000);
  }

  _tick() {
    // §16 v1.27 recovery 轻量 tick：仅倒计时 remainingTime，不跑 decay / defeat / spawn / 666buff / 助威衰减
    //   120s 进入 0 后由 _enterRecovery 中的 setTimeout(120_000) 触发 _enterNight 推进到下一夜
    if (this.state === 'recovery') {
      this._tickCounter++;
      if (this._tickCounter % 5 === 0) {
        this.remainingTime = Math.max(0, this.remainingTime - 1);
      }
      return;
    }
    if (this.state !== 'day' && this.state !== 'night') return;
    this._tickCounter++;

    // 每5tick = 1秒
    if (this._tickCounter % 5 === 0) {
      this.remainingTime = Math.max(0, this.remainingTime - 1);

      // 666效率加成计时器倒计时
      if (this.efficiency666Timer > 0) {
        this.efficiency666Timer = Math.max(0, this.efficiency666Timer - 1);
        if (this.efficiency666Timer <= 0) {
          this.efficiency666Bonus = 1.0;
          console.log('[SurvivalEngine] 666 bonus expired');
        }
      }

      // §33 助威者攻击加成衰减（每秒 -1；归 0 时 buff 重置）
      if (this._supporterAtkBuffTimer > 0) {
        this._supporterAtkBuffTimer = Math.max(0, this._supporterAtkBuffTimer - 1);
        if (this._supporterAtkBuffTimer <= 0) {
          this._supporterAtkBuff = 0;
        }
      }

      // §33 AFK 替补检测（每 10s 执行一次，每次最多换 1 人）
      this._afkCheckCounter = (this._afkCheckCounter || 0) + 1;
      if (this._afkCheckCounter >= 10) {
        this._afkCheckCounter = 0;
        this._checkAfkReplacement();
      }

      // §24.4 主播事件轮盘 tick（充能就绪 / 自动 apply / effect 到期）
      this._roulette.tick(Date.now());
      // §24.4 elite_raid 精英怪 30s 未击杀 → 穿门打城门
      this._tickEliteRaid(Date.now());

      // §39 商店：pending TTL 扫描（每秒清理过期双确认 pending）
      this._shopCleanupExpiredPending();

      // 随机事件触发检查（仅白天）
      this._checkRandomEvents();

      // 资源衰减
      this._decayResources();

      // 检查失败条件
      if (this._checkDefeat()) return;

      // 阶段结束切换（§36 GlobalClock 接管后，内部 _tick 不再自发切换 phase）
      if (this.remainingTime <= 0 && !this._clockDriven) {
        if (this.state === 'day') {
          this._enterNight(this.currentDay);
        } else {
          this._endNight();
        }
      }
    }
  }

  // ==================== §36 GlobalClock 驱动入口 ====================

  /**
   * GlobalClock 触发：全服进入夜晚 phase
   * - 如果本房间当前 state=day → 走原 _enterNight 逻辑 + 应用主题规则
   * - 其他 state（idle/settlement/night）→ no-op（房间自己未进入 Running 或已结算）
   */
  _enterNightFromClock() {
    if (this.state !== 'day') return;  // 本房间未处于 day → 不做 phase 切换
    // 应用主题规则（blood_moon：怪物 HP ×1.2 暂存到倍率字段；由 _initActiveMonsters 消费）
    this._applyThemeRulesForNight();
    this._enterNight(this.currentDay);
  }

  /**
   * GlobalClock 触发：全服进入白天 phase
   * - 如果本房间当前 state=night → 走原 _endNight 逻辑（nextDay 推进 / 胜利判定）
   *   然后判定堡垒日成功（只要本房 state 从 night 离开且未进入 settlement）
   * - 其他 state → no-op
   */
  _enterDayFromClock() {
    if (this.state !== 'night') return;  // 本房间未处于 night → 不做 phase 切换

    // 走原 _endNight 逻辑（§16 v1.26 后只会转入 _enterDay，不再有 _enterSettlement('win') 分支）
    const oldState = this.state;
    this._endNight();

    // 挺过一夜 → 堡垒日 +1
    //   _endNight 永远转入 _enterDay（§16 永续模式），state === 'day' 即代表成功挺夜
    //   保留 'settlement' 分支仅为防御性兼容（理论上不会发生，第三方补丁插入结算时不误判失败）
    if (oldState === 'night' && (this.state === 'day' || this.state === 'settlement')) {
      this._onRoomSuccess();
    }
  }

  /**
   * 根据当前赛季主题，设置 _themeHpMult / _themeCntMult 等倍率字段。
   * 这些倍率在 _initActiveMonsters 中被消费（与难度 / 动态难度相乘）。
   *
   * MVP 实现的主题效果：
   *   blood_moon: 怪物 HP ×1.2（资源掉落 ×1.2 暂未接入，TODO）
   *   frenzy:     maxAliveMonsters 上浮（由客户端消费；服务端仅标记）
   *   snowstorm:  白天采矿 ×0.9（TODO 接入 _applyWorkEffect）
   *   dawn/serene: 仅时长调整（由 GlobalClock 内部处理）
   */
  _applyThemeRulesForNight() {
    const themeId = this.seasonMgr ? this.seasonMgr.themeId : 'classic_frozen';
    this._themeHpMult = 1.0;
    this._themeCntMult = 1.0;
    this._themeMaxAliveBonus = 0;
    if (themeId === 'blood_moon') {
      this._themeHpMult = 1.2;
    } else if (themeId === 'frenzy') {
      this._themeMaxAliveBonus = 2;  // 15 → 17
    }
    // 其他主题 MVP 不改倍率
  }

  // ==================== §36.5 堡垒日（fortressDay）升降 ====================

  /**
   * 房间成功挺过一夜 → 堡垒日 +1（§36.5.1 受每日 cap 限制）
   * 由 _enterDayFromClock 在 night→day 成功切换后调用。
   *
   * 逻辑（§36.5 + §36.5.1）：
   *   1. 先 _ensureDailyReset 检查 dayKey 翻新
   *   2. 若 _dailyCapBlocked 为 true → 静默（reason='cap_blocked'）
   *   3. 否则 fortressDay +1, maxFortressDay=max(...), _dailyFortressDayGained +1
   *   4. 若 _dailyFortressDayGained 达到 cap → _dailyCapBlocked=true
   *   5. 广播 fortress_day_changed
   */
  _onRoomSuccess() {
    this._ensureDailyReset();

    const oldFortressDay = this.fortressDay;
    let reason = 'promoted';
    let actualChange = true;

    if (FeatureFlags.ENABLE_DAILY_CAP && this._dailyCapBlocked) {
      // 已触顶 → 不再推进
      reason = 'cap_blocked';
      actualChange = false;
    } else {
      this.fortressDay = oldFortressDay + 1;
      if (this.fortressDay > this.maxFortressDay) this.maxFortressDay = this.fortressDay;
      if (FeatureFlags.ENABLE_DAILY_CAP) {
        this._dailyFortressDayGained += 1;
        if (this._dailyFortressDayGained >= DAILY_FORTRESS_CAP_MAX) {
          this._dailyCapBlocked = true;
        }
      }
    }

    // 广播 fortress_day_changed
    this.broadcast({
      type: 'fortress_day_changed',
      timestamp: Date.now(),
      data: {
        oldFortressDay,
        newFortressDay: this.fortressDay,
        reason,
        seasonDay: this.seasonMgr ? this.seasonMgr.seasonDay : 1,
        dailyFortressDayGained: this._dailyFortressDayGained,
        dailyCapMax: DAILY_FORTRESS_CAP_MAX,
        dailyResetAt: this._getNextDailyResetMs(),
        dailyCapBlocked: this._dailyCapBlocked,
      },
    });

    console.log(`[Engine:${(this.room && this.room.roomId) || '?'}] _onRoomSuccess: ${oldFortressDay}→${this.fortressDay} reason=${reason} dailyGained=${this._dailyFortressDayGained}/${DAILY_FORTRESS_CAP_MAX}`);
  }

  /**
   * 房间失败 → 堡垒日降级
   *   §36.5 新手保护：fortressDay <= FORTRESS_NEWBIE_PROTECT_DAY (10) 免罚
   *   否则：fortressDay = max(1, floor(fortressDay * 0.9))
   * 广播 room_failed
   */
  _onRoomFail() {
    const oldFortressDay = this.fortressDay;
    let demotionReason = '';
    let newbieProtected = false;

    if (oldFortressDay <= FORTRESS_NEWBIE_PROTECT_DAY) {
      // 新手保护期
      newbieProtected = true;
      demotionReason = 'newbie_protected';
    } else {
      this.fortressDay = Math.max(1, Math.floor(oldFortressDay * 0.9));
      demotionReason = 'demoted';
      // 失败降级同时触发 dayKey 重置的补偿路径（§36.5.1 第三条路径）
      if (FeatureFlags.ENABLE_DAILY_CAP && this._dailyCapBlocked) {
        this._dailyCapBlocked = false;
        this._dailyFortressDayGained = 0;
        this.broadcast({
          type: 'fortress_day_changed',
          timestamp: Date.now(),
          data: {
            oldFortressDay: this.fortressDay,
            newFortressDay: this.fortressDay,
            reason: 'cap_reset',
            seasonDay: this.seasonMgr ? this.seasonMgr.seasonDay : 1,
            dailyFortressDayGained: 0,
            dailyCapMax: DAILY_FORTRESS_CAP_MAX,
            dailyResetAt: this._getNextDailyResetMs(),
            dailyCapBlocked: false,
          },
        });
      }
    }

    // 策划案 §36.5 要求:两种失败路径都必须同时推 fortress_day_changed(reason='demoted'|'newbie_protected')
    //   + room_failed。§36.5.1.6 P23 规定所有 fortress_day_changed 携带 4 cap 字段。
    this.broadcast({
      type: 'fortress_day_changed',
      timestamp: Date.now(),
      data: {
        oldFortressDay,
        newFortressDay: this.fortressDay,
        reason: newbieProtected ? 'newbie_protected' : 'demoted',
        seasonDay: this.seasonMgr ? this.seasonMgr.seasonDay : 1,
        dailyFortressDayGained: this._dailyFortressDayGained || 0,
        dailyCapMax: DAILY_FORTRESS_CAP_MAX,
        dailyResetAt: this._getNextDailyResetMs(),
        dailyCapBlocked: this._dailyCapBlocked || false,
      },
    });

    this.broadcast({
      type: 'room_failed',
      timestamp: Date.now(),
      data: {
        oldFortressDay,
        newFortressDay: this.fortressDay,
        demotionReason,
        newbieProtected,
      },
    });

    console.log(`[Engine:${(this.room && this.room.roomId) || '?'}] _onRoomFail: ${oldFortressDay}→${this.fortressDay} reason=${demotionReason}`);
  }

  // ==================== §36.5.1 每日闯关上限 ====================

  /**
   * 计算当前 dayKey（UTC+8 05:00 作为日切边界）。
   * 返回整数 dayKey（自 1970-01-01 UTC+8 05:00 起，按日递增）。
   */
  _computeDayKey(nowMs = Date.now()) {
    // UTC+8 = Asia/Shanghai；不处理 DST（中国无夏令时）
    const UTC8_OFFSET_MS = 8 * 3600 * 1000;
    const RESET_OFFSET_MS = DAILY_RESET_HOUR_UTC8 * 3600 * 1000;
    // 把 UTC+8 的 05:00 作为每日零点 → 再除以一天毫秒数
    const shifted = nowMs + UTC8_OFFSET_MS - RESET_OFFSET_MS;
    return Math.floor(shifted / (24 * 3600 * 1000));
  }

  /** 下一次 dayKey 切换的 Unix ms（用于客户端 UI 显示倒计时） */
  _getNextDailyResetMs(nowMs = Date.now()) {
    const currentKey = this._computeDayKey(nowMs);
    const UTC8_OFFSET_MS = 8 * 3600 * 1000;
    const RESET_OFFSET_MS = DAILY_RESET_HOUR_UTC8 * 3600 * 1000;
    return (currentKey + 1) * 24 * 3600 * 1000 + RESET_OFFSET_MS - UTC8_OFFSET_MS;
  }

  /**
   * 保证 dayKey 与当前 _dailyResetKey 一致：
   *   - 首次（_dailyResetKey=0） → 初始化
   *   - currentKey > storedKey → 重置 _dailyFortressDayGained / _dailyCapBlocked（NTP 后跳时钟防御）
   *   - currentKey <= storedKey → no-op（时钟回拨不触发重置）
   */
  _ensureDailyReset() {
    if (!FeatureFlags.ENABLE_DAILY_CAP) return;
    const currentKey = this._computeDayKey();
    const storedKey = this._dailyResetKey || 0;

    if (storedKey === 0) {
      // 首次
      this._dailyResetKey = currentKey;
      return;
    }
    if (currentKey > storedKey) {
      // dayKey 翻新 → 重置
      const wasBlocked = this._dailyCapBlocked;
      const wasGained = this._dailyFortressDayGained;
      this._dailyResetKey = currentKey;
      this._dailyFortressDayGained = 0;
      this._dailyCapBlocked = false;

      // 若前一日 cap_blocked，广播一次 cap_reset（让 UI 刷新徽标）
      if (wasBlocked || wasGained > 0) {
        this.broadcast({
          type: 'fortress_day_changed',
          timestamp: Date.now(),
          data: {
            oldFortressDay: this.fortressDay,
            newFortressDay: this.fortressDay,
            reason: 'cap_reset',
            seasonDay: this.seasonMgr ? this.seasonMgr.seasonDay : 1,
            dailyFortressDayGained: 0,
            dailyCapMax: DAILY_FORTRESS_CAP_MAX,
            dailyResetAt: this._getNextDailyResetMs(),
            dailyCapBlocked: false,
          },
        });
      }
      console.log(`[Engine:${(this.room && this.room.roomId) || '?'}] Daily cap reset: dayKey ${storedKey}→${currentKey}`);
    }
    // currentKey <= storedKey → 时钟回拨，不重置（§36.5.1 NTP 后跳时钟防御）
  }

  /**
   * 夜晚结算（时间耗尽 或 Boss击杀 时调用）
   */
  _endNight() {
    // 成功防守一夜 → 积分入池（基础分由难度决定 × 当前天数）
    //   easy 300/天 | normal 500/天 | hard 800/天
    const nightBonus = (this._poolNightBase || 500) * this.currentDay;
    this.scorePool += nightBonus;
    console.log(`[SurvivalEngine] Night ${this.currentDay} cleared [${this._difficulty}], scorePool +${nightBonus} = ${Math.round(this.scorePool)}`);

    // §16 v1.26 永续模式：无胜利终点。原 `if (nextDay > totalDays) _enterSettlement('win','survived')`
    //   已移除——永续循环由失败降级（§36.5）自平衡压力，挺过的夜直接进入下一天。
    //   `totalDays` 字段仍保留于 config/数值平衡参考，但不再触发"胜利结算"。
    const nextDay = this.currentDay + 1;
    this._enterDay(nextDay);
  }

  // ==================== 矿工HP系统 ====================

  /** 夜晚开始时初始化所有已加入玩家的矿工HP（满血，按等级动态值）*/
  _initWorkerHp() {
    this._workerHp = {};
    for (const pid of Object.keys(this.contributions)) {
      // §30 等级决定 maxHp（基础100 + 每级+3）
      const maxHp = this._getWorkerMaxHp(pid);
      this._workerHp[pid] = {
        hp:        maxHp,
        maxHp:     maxHp,
        isDead:    false,
        respawnAt: 0,
      };
    }
    // §16.1 all_dead 判定基准：夜晚开始时刷新（此刻全员满血）
    if (Object.keys(this._workerHp).length > 0) this._lastAliveAt = Date.now();
    // 同步完整矿工HP状态到客户端
    this._broadcastWorkerHp();
    console.log(`[SurvivalEngine] Worker HP initialized for ${Object.keys(this._workerHp).length} players`);
  }

  /** 复活指定玩家的矿工 */
  _reviveWorker(playerId) {
    const w = this._workerHp[playerId];
    if (!w || !w.isDead) return;
    w.hp        = w.maxHp;
    w.isDead    = false;
    w.respawnAt = 0;
    // §16.1 all_dead 判定：复活 → 刷新存活基准
    this._lastAliveAt = Date.now();
    this._broadcast({
      type: 'worker_revived',
      timestamp: Date.now(),
      data: { playerId }
    });
    this._broadcastWorkerHp();
    console.log(`[SurvivalEngine] Worker ${playerId} revived`);
  }

  /** 复活所有死亡矿工（天亮或结算时调用）*/
  _reviveAllWorkers(reason = 'day') {
    let count = 0;
    for (const pid of Object.keys(this._workerHp)) {
      const w = this._workerHp[pid];
      if (w.isDead) {
        w.hp        = w.maxHp;
        w.isDead    = false;
        w.respawnAt = 0;
        this._broadcast({ type: 'worker_revived', timestamp: Date.now(), data: { playerId: pid } });
        count++;
      }
    }
    if (count > 0) {
      // §16.1 all_dead 判定：批量复活 → 刷新存活基准
      this._lastAliveAt = Date.now();
      this._broadcastWorkerHp();
      console.log(`[SurvivalEngine] Revived ${count} workers (reason: ${reason})`);
    }
  }

  /** 广播当前所有矿工HP快照（数组格式，方便 Unity JsonUtility 解析）*/
  _broadcastWorkerHp() {
    if (Object.keys(this._workerHp).length === 0) return;
    const workers = Object.entries(this._workerHp).map(([pid, w]) => ({
      playerId:  pid,
      hp:        w.hp,
      maxHp:     w.maxHp,
      isDead:    w.isDead,
      respawnAt: w.respawnAt,
    }));
    this._broadcast({ type: 'worker_hp_update', timestamp: Date.now(), data: { workers } });
  }

  _decayResources() {
    const isNight = this.state === 'night';

    // 食物：每秒消耗
    this.food = Math.max(0, this.food - (isNight ? this.foodDecayNight : this.foodDecayDay));

    // 炉温：有煤时自动烧煤维持温度，无煤时才开始失温
    const baseTempDecay = isNight ? this.tempDecayNight : this.tempDecayDay;
    const tempDecay = baseTempDecay * (this.tempDecayMultiplier || 1.0);
    if (this.coal > 0) {
      // 有煤：按难度自动消耗煤炭维持炉温（easy=2秒/个, normal=1.4秒/个, hard=1秒/个）
      if (this._tickCounter % this.coalBurnTicks === 0) {
        this.coal = Math.max(0, this.coal - 1);
      }
    } else {
      // 无煤：炉温持续下降（夜晚更快，暴风雪加速）
      this.furnaceTemp = Math.max(this.minTemp, this.furnaceTemp - tempDecay);
    }

    // 矿石 → 自动修复城门（每2秒消耗1矿石，补5HP；仅在城门受损时生效）
    // 设计意图：矿石让玩家挖矿有动力——矿越多，城门越能抵御怪物攻击
    if (this._tickCounter % 10 === 0 && this.ore > 0 && this.gateHp < this.gateMaxHp) {
      const repair = Math.min(5, this.gateMaxHp - this.gateHp);
      this.gateHp = Math.min(this.gateMaxHp, this.gateHp + repair);
      this.ore    = Math.max(0, this.ore - 1);
    }

    // 取整便于显示
    this.food        = Math.round(this.food * 10) / 10;
    this.furnaceTemp = Math.round(this.furnaceTemp * 10) / 10;
    this.gateHp      = Math.round(this.gateHp);
    this.ore         = Math.round(this.ore);
  }

  _checkDefeat() {
    // §16.2 step 3 + §16.4：recovery / settlement 期间不再触发失败（缓冲期保护）
    if (this.state === 'recovery' || this.state === 'settlement') return false;

    if (this.food <= 0) {
      this._enterSettlement('food_depleted');
      return true;
    }
    if (this.furnaceTemp <= this.minTemp) {
      this._enterSettlement('temp_freeze');
      return true;
    }
    if (this.gateHp <= 0) {
      this._enterSettlement('gate_breached');
      return true;
    }
    // §16.1 §v1.27 新增 all_dead 分支：夜晚 + 全员已死 + 最近 5 分钟无复活
    //   罕见 safety net（30s 自动复活覆盖 99% 场景）；仅在复活定时器被清除 / _lastAliveAt 未写入时触发
    if (this.state === 'night' && this._lastAliveAt > 0) {
      const workers = Object.values(this._workerHp || {});
      if (workers.length > 0 && workers.every((w) => w && w.isDead === true)
          && Date.now() - this._lastAliveAt >= 300_000) {
        this._enterSettlement('all_dead');
        return true;
      }
    }
    return false;
  }

  // ==================== 内部：工作效果 ====================

  _applyWorkEffect(commandId, playerId) {
    if (this.state !== 'day' && this.state !== 'night') return;

    // 综合效率倍率计算（策划案 §4.4 + §30 等级加成）
    // levelMult：等级效率系数（每级 +0.8%，Lv.100≈×1.79）
    // playerBonus：仙女棒永久累计加成（最高+100%）
    // globalBoost：能量电池 per-player 临时加成（×1.3，持续180s）
    // 综合上限 ×3.0（策划案 §30.9 数值平衡约束）
    const levelMult        = this._getWorkerLevelEffMult(playerId);               // §30 等级效率
    const playerBonus      = 1.0 + (this._playerEfficiencyBonus[playerId] || 0); // 仙女棒加成
    const globalBoost      = this._playerTempBoost[playerId] || 1.0;             // 能量电池 per-player 加成
    const broadcasterBoost = this.broadcasterEfficiencyMultiplier || 1.0;         // 主播紧急加速
    const eff666Boost      = this.efficiency666Bonus || 1.0;                      // 666弹幕加成
    const auroraBoost      = this._auroraEffMult || 1.0;                          // §24.4 极光 ×1.5（60s）
    // §39 A1 worker_pep_talk：采矿效率 +15%（与 efficiency666Bonus / ability_pill 按 Math.max 取最大，不叠加）
    const pepTalkBoost     = (Date.now() < this._peptTalkBoostUntil) ? 1.15 : 1.0;
    const efficiencyAdditive = Math.max(pepTalkBoost, eff666Boost);
    const totalMult        = Math.min(3.0, levelMult * playerBonus * globalBoost * broadcasterBoost * efficiencyAdditive * auroraBoost);

    // 阶3+（Lv.21+）白天采矿 10% 概率双倍产出（策划案 §30.3 阶3 专属被动）
    // 适用 cmd 1/2/3（采食物/挖煤/采矿），不含 cmd=4 添柴
    const workerTier = this._getWorkerTier(playerId);
    const isMiningCmd = (commandId === 1 || commandId === 2 || commandId === 3);
    const doubleMult = (workerTier >= 3 && this.state === 'day' && isMiningCmd && Math.random() < 0.10) ? 2 : 1;

    // 工作效果：每次评论小幅增加资源（基础值 × 综合倍率）
    switch (commandId) {
      case 1: // 采食物（丰收事件额外加成）
        this.food = Math.min(2000, this.food + Math.round(5 * totalMult * (this.foodBonus || 1.0) * doubleMult));
        break;
      case 2: // 挖煤
        this.coal = Math.min(1500, this.coal + Math.round(3 * totalMult * doubleMult));
        break;
      case 3: // 采矿（矿脉事件额外加成）
        this.ore = Math.min(800, this.ore + Math.round(2 * totalMult * (this.oreBonus || 1.0) * doubleMult));
        break;
      case 4: // 添柴升温：每次+3℃（策划案要求），消耗1煤炭
        this.furnaceTemp = Math.min(this.maxTemp, this.furnaceTemp + Math.round(3 * totalMult));
        this.coal = Math.max(0, this.coal - 1);
        break;
      // case 5 (修城门) 已从策划案 v2.0 移除
    }

    this._trackContribution(playerId, 1, 'barrage'); // 工作贡献值=1（弹幕来源，阶2被动 ×1.5）
    this._tribeWarAddEnergy(1);  // §35 攻击能量：工作指令弹幕 cmd 1/2/3/4 +1
  }

  // ==================== 内部：怪物追踪 ====================

  /**
   * 夜晚开始时，根据当天波次配置初始化活跃怪物 Map
   */
  _initActiveMonsters(day) {
    this._activeMonsters.clear();
    const cfg = getWaveConfig(day);

    // 难度倍率（未设置时默认 1.0）
    // §30.6 动态难度叠加：HP 基础难度 × 动态难度（Hard 封顶+25%，Normal/Easy 封顶+50%）
    // §36 主题倍率叠加：blood_moon 主题 → HP ×1.2（_themeHpMult 由 _applyThemeRulesForNight 设置）
    // TODO §30.6：若未来加入随机 Hard Night 事件，HP 加成取 max(_dynamicHpMult, hardNightMult)
    const themeHpMult  = this._themeHpMult  || 1.0;
    const themeCntMult = this._themeCntMult || 1.0;
    const hpMult  = (this._monsterHpMult  || 1.0) * (this._dynamicHpMult   || 1.0) * themeHpMult;
    const cntMult = (this._monsterCntMult || 1.0) * (this._dynamicCountMult || 1.0) * themeCntMult;

    // 普通怪
    if (cfg.normal) {
      const count = Math.max(1, Math.round(cfg.normal.count * cntMult));
      const hp    = Math.max(1, Math.round(cfg.normal.hp    * hpMult));
      for (let i = 0; i < count; i++) {
        const id = `n_${day}_${++this._monsterIdCounter}`;
        this._activeMonsters.set(id, {
          id,
          type: 'normal',
          maxHp: hp,
          currentHp: hp,
          atk: cfg.normal.atk,
        });
      }
    }

    // 精英怪
    if (cfg.elite) {
      const count = Math.max(0, Math.round(cfg.elite.count * cntMult));
      const hp    = Math.max(1, Math.round(cfg.elite.hp    * hpMult));
      for (let i = 0; i < count; i++) {
        const id = `e_${day}_${++this._monsterIdCounter}`;
        this._activeMonsters.set(id, {
          id,
          type: 'elite',
          maxHp: hp,
          currentHp: hp,
          atk: cfg.elite.atk,
        });
      }
    }

    // Boss（每晚一只，HP受难度影响）
    if (cfg.boss) {
      const hp = Math.max(1, Math.round(cfg.boss.hp * hpMult));
      const id = `b_${day}_${++this._monsterIdCounter}`;
      this._activeMonsters.set(id, {
        id,
        type: 'boss',
        maxHp: hp,
        currentHp: hp,
        atk: cfg.boss.atk,
      });
    }

    console.log(`[SurvivalEngine] Night ${day} monsters initialized: ${this._activeMonsters.size} total (diff:${this._difficulty||'normal'} hpMult:${hpMult} cntMult:${cntMult})`);
  }

  // ==================== 内部：攻击处理（commandId=6）====================

  /**
   * 处理玩家攻击指令（夜晚限定）
   */
  _handleAttack(secOpenId, nickname) {
    const monsters = [...this._activeMonsters.values()];
    if (monsters.length === 0) return;

    // 目标优先级：普通怪 > 精英怪 > Boss
    const target = monsters.find(m => m.type === 'normal') ||
                   monsters.find(m => m.type === 'elite')  ||
                   monsters.find(m => m.type === 'boss');

    if (!target) return;

    // §30 等级攻击系数 (+0.8%/级) × §33 助威者全局攻击加成 (1 + buff, 0~0.20)
    const atkMult       = this._getWorkerLevelAtkMult(secOpenId);
    const supporterBuff = 1 + (this._supporterAtkBuff || 0);
    let damage = Math.max(1, Math.round(10 * atkMult * supporterBuff));

    // §30.3 阶5+ 20% 概率重击（伤害 ×2）
    const attackerTier = this._getWorkerTier(secOpenId);
    if (attackerTier >= 5 && Math.random() < 0.20) {
      damage *= 2;
    }

    target.currentHp -= damage;

    // 贡献奖励（攻击命中）
    const hitScore = 5;
    this._addScore(secOpenId, nickname, hitScore);

    // 广播攻击事件
    this._broadcast({
      type: 'combat_attack',
      data: {
        attackerId:       secOpenId,
        attackerName:     nickname,
        targetId:         target.id,
        targetType:       target.type,
        damage,
        targetHpRemaining: Math.max(0, target.currentHp),
      }
    });

    // 检查怪物是否死亡
    if (target.currentHp <= 0) {
      this._activeMonsters.delete(target.id);

      // §24.4 elite_raid 击杀奖励 +500（策划案明确值）；其余按常规
      const killScore = target.type === 'normal'     ? 20  :
                        target.type === 'elite'      ? 50  :
                        target.type === 'elite_raid' ? 500 : 200;
      this._addScore(secOpenId, nickname, killScore);
      // 阶7+ 击杀怪物贡献值 ×2（策划案 §30.3 阶7 专属被动）：再补一份贡献
      // 仅影响 contributions/_lifetimeContrib，不重复加 scorePool（scorePool 反映游戏产出不按身份倍增）
      if (attackerTier >= 7) {
        this._addScore(secOpenId, nickname, killScore);
      }
      // 积分池：击杀得分入池
      this.scorePool += killScore;

      // 阶8+ 吸血：击杀怪物回 10HP（策划案 §30.3）
      if (attackerTier >= 8 && this._workerHp[secOpenId] && !this._workerHp[secOpenId].isDead) {
        const w = this._workerHp[secOpenId];
        const heal = 10;
        w.hp = Math.min(w.maxHp, w.hp + heal);
        this._broadcastWorkerHp();
      }

      this._broadcast({
        type: 'monster_died',
        data: {
          monsterId:   target.id,
          monsterType: target.type,
          killerId:    secOpenId,
        }
      });

      console.log(`[SurvivalEngine] Monster killed: ${target.id} (${target.type}) by ${nickname}`);

      // Boss 击杀 → 提前结束当夜
      if (target.type === 'boss') {
        this._broadcast({
          type: 'night_cleared',
          data: { reason: 'boss_killed' }
        });
        this._endNight();
      }
    }
  }

  // ==================== 内部：城门升级 ====================

  /**
   * 升级城门
   * 等级 1→2→3→4，矿石消耗：100 / 250 / 500（策划案 §3.3）
   */
  _upgradeGate(secOpenId) {
    const currentLevel = this.gateLevel;
    if (currentLevel >= 4) {
      this._broadcast({
        type: 'gate_upgrade_failed',
        data: { reason: 'max_level', currentLevel }
      });
      return;
    }

    const cost = GATE_UPGRADE_COSTS[currentLevel - 1]; // index: lv1→idx0=100, lv2→idx1=250, lv3→idx2=500
    if (this.ore < cost) {
      this._broadcast({
        type: 'gate_upgrade_failed',
        data: {
          reason:    'insufficient_ore',
          required:  cost,
          available: Math.round(this.ore),
        }
      });
      return;
    }

    this.ore -= cost;
    this.gateLevel = currentLevel + 1;

    // 更新城门最大血量
    this.gateMaxHp = GATE_MAX_HP_BY_LEVEL[this.gateLevel - 1];
    // 当前血量不超过新的最大值（不主动缩减已有血量）

    this._broadcast({
      type: 'gate_upgraded',
      data: {
        newLevel:     this.gateLevel,
        newMaxHp:     this.gateMaxHp,
        oreRemaining: Math.round(this.ore),
        upgradedBy:   secOpenId || '',
      }
    });

    console.log(`[SurvivalEngine] Gate upgraded to level ${this.gateLevel} (maxHp=${this.gateMaxHp}), ore left=${Math.round(this.ore)}`);

    // 同步资源（矿石变动 + 城门信息）
    this._broadcastResourceUpdate();
  }

  // ==================== 内部：怪物波次（旧调度，兼容保留）====================

  _scheduleNightWaves(day) {
    const cfg = getWaveConfig(day);
    console.log(`[SurvivalEngine] Night ${day} waves: ${cfg.baseCount}-${cfg.maxCount} per wave, every ${cfg.refreshTime}s`);

    let waveIndex = 0;

    // 首波延迟
    const firstTimer = setTimeout(() => {
      this._spawnWave(cfg, day, waveIndex++);

      // 后续波次
      const repeatTimer = setInterval(() => {
        if (this.state !== 'night') {
          clearInterval(repeatTimer);
          return;
        }
        if (waveIndex >= cfg.maxCount) {
          clearInterval(repeatTimer);
          return;
        }
        this._spawnWave(cfg, day, waveIndex++);
      }, cfg.refreshTime * 1000);

      this._waveTimers.push(repeatTimer);
    }, cfg.beginning * 1000);

    this._waveTimers.push(firstTimer);
  }

  _spawnWave(cfg, day, waveIndex) {
    if (this.state !== 'night') return;

    // 生成数量随天数递增，应用基础难度倍率 + §30.6 动态难度数量加成
    const baseCnt = cfg.baseCount + Math.floor((day - 1) * 0.5);
    const count   = Math.max(1, Math.round(baseCnt * (this._monsterCntMult || 1.0) * (this._dynamicCountMult || 1.0)));

    const sides = ['left', 'right', 'top', 'all'];
    const spawnSide = sides[Math.floor(Math.random() * sides.length)];

    const waveData = {
      waveIndex,
      day,
      monsterId: cfg.monsterId,
      count,
      spawnSide,
    };

    this.broadcast({ type: 'monster_wave', timestamp: Date.now(), data: waveData });

    // §24.4 time_freeze：冻结期内怪物对矿工/城门伤害暂停（仅本 wave 结算跳过）
    if (Date.now() < (this._freezeUntilMs || 0)) {
      console.log(`[SurvivalEngine] Wave ${waveIndex} damage skipped (time_freeze active)`);
      return;
    }

    // ── 怪物伤害路由：优先打矿工，矿工全死后才打城门 ──────────────────
    const totalDamage = Math.floor(count * this.monsterGateDamage * this._workerDamageMult);
    let remainingDamage = totalDamage;

    const aliveWorkers = Object.entries(this._workerHp).filter(([, w]) => !w.isDead);
    if (aliveWorkers.length > 0) {
      // 每个存活矿工平均承担伤害（至少1点，避免小伤害完全跳过矿工直打城门）
      const damagePerWorker = Math.max(1, Math.floor(totalDamage / aliveWorkers.length));
      for (const [pid, w] of aliveWorkers) {
        // §30.3 阶6 15% 格挡（本次伤害归零）
        if (this._calcWorkerBlock(pid)) {
          // 格挡成功：不扣HP，不消耗 remainingDamage（视为防御完全抵挡）
          this._broadcast({
            type: 'worker_blocked',
            data: { playerId: pid, playerName: this._getPlayerName(pid) }
          });
          continue;
        }

        const actualDmg = Math.min(w.hp, damagePerWorker);
        w.hp -= actualDmg;
        remainingDamage -= actualDmg;

        if (w.hp <= 0) {
          // §30.3 阶10 每晚1次免死：回复至 25% maxHp（策划案 §30.3 "回复至25% HP + 10s 50%减伤护盾"）
          // MVP：不实现减伤护盾（TODO）
          if (this._getWorkerTier(pid) >= 10 && !this._legendReviveUsed[pid]) {
            this._legendReviveUsed[pid] = true;
            const maxHp = this._getWorkerMaxHp(pid);
            w.hp = Math.ceil(maxHp * 0.25);
            w.isDead = false;
            w.respawnAt = 0;
            this._broadcast({
              type: 'legend_revive_triggered',
              data: {
                playerId: pid,
                playerName: this._getPlayerName(pid),
              }
            });
            console.log(`[SurvivalEngine] Legend revive: ${this._getPlayerName(pid)} saved from death (${w.hp}/${maxHp} HP)`);
            // TODO §30.3: 10s 50% 减伤护盾，MVP 暂不实现
            continue; // 跳过正常死亡逻辑
          }

          w.hp = 0;
          w.isDead = true;
          // §30 矿工复活秒数按阶段（阶4+ 20s，否则 30s）
          const respawnSec = this._getWorkerRespawnSec(pid);
          const respawnMs  = respawnSec * 1000;
          w.respawnAt = Date.now() + respawnMs;

          // 广播矿工死亡
          this._broadcast({
            type: 'worker_died',
            timestamp: Date.now(),
            data: { playerId: pid, respawnAt: w.respawnAt }
          });
          console.log(`[SurvivalEngine] Worker ${pid} died, respawn at +${respawnSec}s (tier=${this._getWorkerTier(pid)})`);

          // 自动定时复活（若天亮先到则由 _reviveAllWorkers 处理）
          const respawnTimer = setTimeout(() => {
            if (this._workerHp[pid]?.isDead) this._reviveWorker(pid);
          }, respawnMs);
          this._waveTimers.push(respawnTimer);
        }
      }
      // 如果所有矿工刚好被打死，剩余伤害继续打城门
    }

    // 剩余伤害（矿工全死后的溢出）打城门；clamp到0避免矿工分摊时负值"治疗"城门
    remainingDamage = Math.max(0, remainingDamage);
    if (remainingDamage > 0) {
      this.gateHp = Math.max(0, this.gateHp - remainingDamage);
    }

    // §16.1 all_dead 判定：本轮伤害结算后仍有矿工存活 → 刷新基准
    //   若全员死亡则 _lastAliveAt 保持上一次活着的时刻，_checkDefeat 据此判 5 分钟窗口
    if (Object.values(this._workerHp).some((w) => !w.isDead)) {
      this._lastAliveAt = Date.now();
    }

    // 广播矿工HP更新
    this._broadcastWorkerHp();

    console.log(`[SurvivalEngine] Wave ${waveIndex}: ${count} monsters (${spawnSide}), workers=${aliveWorkers.length}, gate -${remainingDamage}hp → ${this.gateHp}`);

    // 立即推送资源更新（城门HP变化）
    this._broadcastResourceUpdate();

    // 检查失败（城门被攻破）
    this._checkDefeat();
  }

  _clearNightWaves() {
    for (const t of this._waveTimers) {
      clearTimeout(t);
      clearInterval(t);
    }
    this._waveTimers = [];
  }

  // ==================== 随机事件系统（策划案 §8）====================

  /**
   * 每秒检查是否触发随机事件（仅白天）
   */
  _checkRandomEvents() {
    if (this.state !== 'day') return;
    this._nextEventTimer -= 1; // 每秒递减
    if (this._nextEventTimer > 0) return;

    // 重置计时器（90-120s后触发下次）
    this._nextEventTimer = 90 + Math.random() * 30;

    const events = ['E01_snowstorm', 'E02_harvest', 'E03_monster_wave', 'E04_warm_spring', 'E05_ore_vein'];
    const eventId = events[Math.floor(Math.random() * events.length)];
    this._applyRandomEvent(eventId);
  }

  /**
   * 应用随机事件效果并广播
   */
  _applyRandomEvent(eventId) {
    const name = EVENT_NAMES[eventId] || eventId;
    console.log(`[SurvivalEngine] Random event: ${eventId} (${name})`);

    switch (eventId) {
      case 'E01_snowstorm': {
        // 暴风雪：炉温衰减×2，持续60秒
        this.tempDecayMultiplier = 2.0;
        const t = setTimeout(() => { this.tempDecayMultiplier = 1.0; }, 60000);
        this._randomEventTimers.push(t);
        break;
      }
      case 'E02_harvest': {
        // 丰收：食物采集效率×1.5，持续30秒
        this.foodBonus = 1.5;
        const t = setTimeout(() => { this.foodBonus = 1.0; }, 30000);
        this._randomEventTimers.push(t);
        break;
      }
      case 'E03_monster_wave': {
        // 怪物潮：额外生成2只怪物（仅夜晚有意义；白天触发则在下次夜晚生效）
        if (this.state === 'night') {
          const day = this.currentDay;
          for (let i = 0; i < 2; i++) {
            const id = `wave_event_${++this._monsterIdCounter}`;
            this._activeMonsters.set(id, {
              id, type: 'normal',
              maxHp: 30, currentHp: 30, atk: 3,
            });
          }
          this._broadcastResourceUpdate();
        }
        break;
      }
      case 'E04_warm_spring': {
        // 暖流：炉温立即+20℃
        this.furnaceTemp = Math.min(this.maxTemp, this.furnaceTemp + 20);
        this._broadcastResourceUpdate();
        break;
      }
      case 'E05_ore_vein': {
        // 矿脉：矿石采集效率×2，持续45秒
        this.oreBonus = 2.0;
        const t = setTimeout(() => { this.oreBonus = 1.0; }, 45000);
        this._randomEventTimers.push(t);
        break;
      }
    }

    this.broadcast({ type: 'random_event', timestamp: Date.now(), data: { eventId, name } });
  }

  // ==================== 公开辅助接口 ====================

  /**
   * 给玩家增加贡献值（供外部调用，如点赞处理）
   */
  addContribution(playerId, amount) {
    this._trackContribution(playerId, amount);
  }

  /**
   * 主播触发随机事件（broadcaster_action: trigger_event）
   */
  triggerEvent(eventType) {
    // 将旧 eventType 格式映射到新格式
    const mapping = {
      snowstorm:    'E01_snowstorm',
      harvest:      'E02_harvest',
      monster_wave: 'E03_monster_wave',
    };
    const eventId = mapping[eventType] || eventType;
    this._applyRandomEvent(eventId);
  }

  // ==================== §37 建造系统（Building System，MVP）====================

  /**
   * 发起建造投票（C→S build_propose）
   * 前置：state==='day' + 无活跃投票 + 非助威者 + 未当日用完 + buildId 合法且未建成 + 资源充足
   * 候选生成：buildId 必入 options[0]；剩余 2 张从"未建造 + 资源够"随机抽；不足补"已建造可重建"；退化 [buildId]
   */
  handleBuildPropose(playerId, playerName, buildId) {
    // 1) 仅白天受理（策划案 §37.3 / §10.5 口径统一，不含 recovery）
    if (this.state !== 'day') {
      return this._broadcast({
        type: 'build_propose_failed',
        data: { reason: 'wrong_phase', unlockDay: 0 },
      });
    }
    // 2) 已有活跃投票
    if (this._buildVote !== null) {
      return this._broadcast({
        type: 'build_propose_failed',
        data: { reason: 'already_voting', unlockDay: 0 },
      });
    }
    // 3) 每日限 1 次
    if (this._buildVoteUsedToday) {
      return this._broadcast({
        type: 'build_propose_failed',
        data: { reason: 'daily_limit', unlockDay: 0 },
      });
    }
    // 4) 助威者不能发起（可投票但不能提案）
    if (playerId && this._supporters.has(playerId)) {
      return this._broadcast({
        type: 'build_propose_failed',
        data: { reason: 'supporter_not_allowed', unlockDay: 0 },
      });
    }
    // 5) 非法 buildId（当作 already_built 口径统一）
    if (!BUILDING_CATALOG[buildId]) {
      return this._broadcast({
        type: 'build_propose_failed',
        data: { reason: 'already_built', unlockDay: 0 },
      });
    }
    // 6) 已建成（同类建筑唯一）
    if (this._buildings.has(buildId)) {
      return this._broadcast({
        type: 'build_propose_failed',
        data: { reason: 'already_built', unlockDay: 0 },
      });
    }
    // 7) 资源不足
    if (!this._hasBuildResources(buildId)) {
      return this._broadcast({
        type: 'build_propose_failed',
        data: { reason: 'insufficient_resources', unlockDay: 0 },
      });
    }

    // ---- 生成候选 options（长度 1~3）----
    //  1. buildId 必入 options[0]
    //  2. 剩余从"未建造 + 资源够"随机抽 2
    //  3. 不足则补"已建造但可重建"（MVP：唯一性硬限 → 留空）
    //  4. 仍不足则退化 [buildId]
    const options  = [buildId];
    const candidates = BUILDING_IDS.filter(id =>
      id !== buildId && !this._buildings.has(id) && this._hasBuildResources(id)
    );
    // Fisher-Yates 洗牌
    for (let i = candidates.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      [candidates[i], candidates[j]] = [candidates[j], candidates[i]];
    }
    for (const c of candidates) {
      if (options.length >= 3) break;
      options.push(c);
    }
    // 不足则尝试补"已建造可重建"——MVP 唯一性硬限，跳过（TODO §37.2 拆除重建）
    // 最终退化：options 至少有 [buildId]

    const proposalId = `prop_${Date.now()}_${++this._proposalIdCounter}`;
    const startAt       = Date.now();
    const votingEndsAt  = startAt + BUILD_VOTE_WINDOW_MS;
    this._buildVote = {
      proposalId,
      options,
      startAt,
      votingEndsAt,
      votes:         new Map(),
      proposerName:  playerName || playerId || 'Unknown',
      timer:         setTimeout(() => this._closeBuildVote(), BUILD_VOTE_WINDOW_MS),
    };

    this._broadcast({
      type: 'build_vote_started',
      data: {
        proposalId,
        proposerName: this._buildVote.proposerName,
        options,
        votingEndsAt,
      },
    });
    console.log(`[Building] propose: ${playerName} → buildId=${buildId} options=[${options.join(',')}] votingEndsAt=+45s`);
  }

  /**
   * 投票（C→S build_vote 或 弹幕 `建X`）
   * 校验 proposalId 匹配 + buildId 在 options 内；每位玩家 1 票，重复投票覆盖
   * 广播并行数组格式 build_vote_update；全员投完（正式守护者，不含助威者）则立即 _closeBuildVote
   */
  handleBuildVote(playerId, proposalId, buildId) {
    if (!playerId || !this._buildVote) return;
    if (this._buildVote.proposalId !== proposalId) return;
    if (!this._buildVote.options.includes(buildId)) return;

    this._buildVote.votes.set(playerId, buildId);

    // 聚合 build_vote_update（并行数组格式：与客户端协议对齐）
    const tally = new Map();
    for (const [, bid] of this._buildVote.votes) {
      tally.set(bid, (tally.get(bid) || 0) + 1);
    }
    const voteBuildIds = [...tally.keys()];
    const voteCounts   = voteBuildIds.map(bid => tally.get(bid));
    const totalVoters  = this._buildVote.votes.size;

    this._broadcast({
      type: 'build_vote_update',
      data: {
        proposalId: this._buildVote.proposalId,
        voteBuildIds,
        voteCounts,
        totalVoters,
      },
    });

    // 全员投完（仅统计正式守护者，不含助威者）→ 立即结算
    // 正式守护者数 = Object.keys(contributions).length（§33 助威者不在 contributions 中）
    const guardianCount = Object.keys(this.contributions).length;
    const guardianVotes = [...this._buildVote.votes.keys()].filter(
      pid => this.contributions[pid] !== undefined
    ).length;
    if (guardianCount > 0 && guardianVotes >= guardianCount) {
      this._closeBuildVote();
    }
  }

  /** 结束投票并决定 winnerId；有票则 _startBuild，0 票则流产 */
  _closeBuildVote() {
    if (!this._buildVote) return;
    const vote = this._buildVote;
    // 先停定时器再清引用（防定时器兜底重入）
    if (vote.timer) { clearTimeout(vote.timer); vote.timer = null; }

    // 聚合 + 选 winner（0 票 → null；有票取 count 最大，平票按 options 顺序靠前）
    const tally = new Map();
    for (const [, bid] of vote.votes) {
      tally.set(bid, (tally.get(bid) || 0) + 1);
    }
    const totalVoters = vote.votes.size;
    let winnerId = null;
    if (tally.size > 0) {
      let maxCount = -1;
      for (const optId of vote.options) {
        const cnt = tally.get(optId) || 0;
        if (cnt > maxCount) {
          maxCount = cnt;
          winnerId = optId;
        }
      }
      if (maxCount <= 0) winnerId = null;   // 0 票流产
    }

    this._broadcast({
      type: 'build_vote_ended',
      data: {
        proposalId: vote.proposalId,
        winnerId,
        totalVoters,
      },
    });

    // 每日限额已用（即便流产也计入，避免反复刷流产——策划案 §37.3 明确）
    this._buildVoteUsedToday = true;
    this._buildVote = null;

    if (winnerId !== null) {
      // 二次确认资源（投票窗口期间可能被其他事件消耗掉）
      if (!this._hasBuildResources(winnerId)) {
        this._broadcast({
          type: 'build_cancelled',
          data: { buildId: winnerId, reason: 'insufficient_resources' },
        });
        console.log(`[Building] vote_ended winner=${winnerId} but insufficient_resources → cancelled`);
      } else {
        this._startBuild(winnerId);
      }
    } else {
      console.log(`[Building] vote_ended: no winner (totalVoters=${totalVoters})`);
    }
  }

  /** 扣资源并启动建造计时器，广播 build_started */
  _startBuild(buildId) {
    const cfg = BUILDING_CATALOG[buildId];
    if (!cfg) return;
    // 扣资源
    if (cfg.cost.ore)  this.ore  = Math.max(0, this.ore  - cfg.cost.ore);
    if (cfg.cost.coal) this.coal = Math.max(0, this.coal - cfg.cost.coal);
    if (cfg.cost.food) this.food = Math.max(0, this.food - cfg.cost.food);

    const completesAt = Date.now() + cfg.buildMs;
    const timer = setTimeout(() => this._completeBuild(buildId), cfg.buildMs);
    this._buildingInProgress.set(buildId, { completesAt, timer });

    this._broadcast({
      type: 'build_started',
      data: {
        buildId,
        completesAt,
        position: { x: cfg.position.x, y: cfg.position.y, z: cfg.position.z },
      },
    });
    this._broadcastResourceUpdate();
    console.log(`[Building] started: ${buildId} completesAt=+${cfg.buildMs/1000}s cost=${JSON.stringify(cfg.cost)}`);
  }

  /** 建造完成 → 加入 _buildings 集合，广播 build_completed */
  _completeBuild(buildId) {
    const info = this._buildingInProgress.get(buildId);
    if (info && info.timer) clearTimeout(info.timer);
    this._buildingInProgress.delete(buildId);
    // 幂等：若已被 demote 清理则不重复加入
    if (this._buildings.has(buildId)) return;
    this._buildings.add(buildId);

    this._broadcast({ type: 'build_completed', data: { buildId } });
    console.log(`[Building] completed: ${buildId} → _buildings=[${[...this._buildings].join(',')}]`);

    // TODO §37.2 watchtower：下次 wave spawn 前 10s 广播 monster_wave_incoming
    // （建议挂载点：_initActiveMonsters(day) 或 wave spawn 处检查 _buildings.has('watchtower') 并安排 10s 预告）
    if (buildId === 'watchtower') {
      console.log(`[Building] watchtower built — TODO: wave spawn 10s early warning hook not yet wired`);
    }
  }

  /** 失败降级清理（§37.5）：清除不在 BUILDING_KEEP_ON_DEMOTE 的建筑，进行中骨架全部取消（不返资源） */
  _demoteBuildings(reason) {
    // 1) 已建成：按清单部分保留
    const toRemove = [...this._buildings].filter(b => !BUILDING_KEEP_ON_DEMOTE.includes(b));
    for (const b of toRemove) this._buildings.delete(b);
    if (toRemove.length > 0) {
      this._broadcast({
        type: 'building_demolished_batch',
        data: { buildingIds: toRemove, reason: reason || 'demoted' },
      });
      console.log(`[Building] demote: removed ${toRemove.join(',')}, kept ${[...this._buildings].join(',') || '(none)'}`);
    }
    // 2) 进行中：全部取消（不返资源），广播 build_demolished
    for (const [bid, info] of this._buildingInProgress) {
      if (info.timer) clearTimeout(info.timer);
      this._broadcast({
        type: 'build_demolished',
        data: { buildId: bid, reason: 'demoted_during_build' },
      });
    }
    this._buildingInProgress.clear();
  }

  /** 资源检查（内部） */
  _hasBuildResources(buildId) {
    const cfg = BUILDING_CATALOG[buildId];
    if (!cfg) return false;
    if (cfg.cost.ore  && this.ore  < cfg.cost.ore)  return false;
    if (cfg.cost.coal && this.coal < cfg.cost.coal) return false;
    if (cfg.cost.food && this.food < cfg.cost.food) return false;
    return true;
  }

  // ==================== 内部：广播 ====================

  /**
   * 内部广播快捷方式（自动附加 timestamp）
   */
  _broadcast(msg) {
    this.broadcast(Object.assign({ timestamp: Date.now() }, msg));
  }

  _broadcastResourceUpdate() {
    this.broadcast({
      type: 'resource_update',
      timestamp: Date.now(),
      data: {
        food:         Math.round(this.food),
        coal:         Math.round(this.coal),
        ore:          Math.round(this.ore),
        furnaceTemp:  Math.round(this.furnaceTemp * 10) / 10,
        gateHp:       Math.round(this.gateHp),
        gateMaxHp:    this.gateMaxHp,
        gateLevel:    this.gateLevel,
        remainingTime: Math.round(this.remainingTime),
        scorePool:    Math.round(this.scorePool),
        workerHp:     Object.fromEntries(
          Object.entries(this._workerHp).map(([pid, w]) => [pid, {
            hp: w.hp, maxHp: w.maxHp, isDead: w.isDead, respawnAt: w.respawnAt
          }])
        ),
      }
    });
  }

  // ==================== 内部：矿工成长系统（策划案 §30）====================

  /** 阶段（1~10），派生自 `_playerLevel` */
  _getWorkerTier(playerId) {
    const lv = this._playerLevel[playerId] || 1;
    return Math.ceil(lv / 10);
  }

  /** 等级效率系数（每级 +0.8%，Lv.100≈+79%），乘入 _applyWorkEffect */
  _getWorkerLevelEffMult(playerId) {
    const lv = this._playerLevel[playerId] || 1;
    return 1 + (lv - 1) * 0.008;
  }

  /** 等级攻击系数（每级 +0.8%），乘入 _handleAttack 伤害 */
  _getWorkerLevelAtkMult(playerId) {
    const lv = this._playerLevel[playerId] || 1;
    return 1 + (lv - 1) * 0.008;
  }

  /** 等级决定的最大HP（基础100 + 每级+3） */
  _getWorkerMaxHp(playerId) {
    const lv = this._playerLevel[playerId] || 1;
    return 100 + (lv - 1) * 3;
  }

  /** 矿工复活秒数（§30 阶4+ -10s；§37 hospital -15s；最低 5s） */
  _getWorkerRespawnSec(playerId) {
    const tier = this._getWorkerTier(playerId);
    const hasLv31     = tier >= 4;   // §30.10 阶4 起 -10s
    const hasHospital = this._buildings.has('hospital');  // §37 医院 -15s
    return Math.max(5, 30 - (hasLv31 ? 10 : 0) - (hasHospital ? 15 : 0));
  }

  /** 阶6 矿工 15% 概率格挡（本次伤害归零） */
  _calcWorkerBlock(playerId) {
    const tier = this._getWorkerTier(playerId);
    return tier >= 6 && Math.random() < 0.15;
  }

  /** 玩家名（排行榜/贡献表/supporters 三层回退） */
  _getPlayerName(playerId) {
    if (!playerId) return 'Unknown';
    if (this.playerNames && this.playerNames[playerId]) return this.playerNames[playerId];
    // TODO §33 助威模式：若有 _supporters Map，亦可 fallback
    return playerId;
  }

  /**
   * 等级检查（每次 _trackContribution 末尾调用）：
   * 遍历 100→1，找到最高符合 `_lifetimeContrib[playerId]` 的等级，
   * 若跨阶段（tier 上升）则自动切换到新阶段皮肤并广播 `worker_level_up`。
   * 阶内升级（tier 未变）仅更新 `_playerLevel`，不播报公告（策划案 §30.8 防刷屏）。
   */
  _checkLevelUp(playerId) {
    if (!playerId) return;
    const lc  = this._lifetimeContrib[playerId] || 0;
    const cur = this._playerLevel[playerId]     || 1;
    let newLevel = 1;
    for (let lv = 100; lv >= 1; lv--) {
      const tier = Math.floor((lv - 1) / 10);
      const lvInTier = ((lv - 1) % 10) + 1;
      const needed = TIER_THRESHOLDS[tier] + (lvInTier - 1) * TIER_COST_PER_LV[tier];
      if (lc >= needed) { newLevel = lv; break; }
    }
    if (newLevel <= cur) return;
    this._playerLevel[playerId] = newLevel;
    const newTier = Math.ceil(newLevel / 10);
    const oldTier = Math.ceil(cur / 10);
    if (newTier > oldTier) {
      // 跨阶段：自动换肤（不受 60s 冷却限制）+ 广播
      this._playerSkinId[playerId] = TIER_SKINS[newTier - 1];
      this._broadcast({
        type: 'worker_level_up',
        data: {
          playerId,
          playerName: this._getPlayerName(playerId),
          newLevel,
          newTier,
          skinId: TIER_SKINS[newTier - 1],
        }
      });
      console.log(`[SurvivalEngine] Level up: ${this._getPlayerName(playerId)} → Lv.${newLevel} 阶${newTier} skin=${TIER_SKINS[newTier - 1]}`);
    }
  }

  /**
   * 当前场上/参与过贡献的所有玩家平均等级（§30.6 动态难度统计口径）。
   * 含 12 人 WorkerPool 之外的 supporters（§33 助威模式，若已实现）。
   */
  _getAverageLevel() {
    const ids = new Set(Object.keys(this.contributions));
    // TODO §33 助威模式：合并 this._supporters 的 key
    if (this._supporters && typeof this._supporters.keys === 'function') {
      for (const id of this._supporters.keys()) ids.add(id);
    }
    if (ids.size === 0) return 0;
    let total = 0;
    for (const id of ids) total += (this._playerLevel[id] || 1);
    return total / ids.size;
  }

  /**
   * 动态难度更新（策划案 §30.6），每次进入夜晚调一次。
   * 平均等级越高，怪物 HP/数量 越强；Hard 难度下动态加成封顶+25%。
   */
  _updateDynamicDifficulty() {
    const avg = this._getAverageLevel();
    const isHard = this._difficulty === 'hard';
    if (avg < 20) {
      this._dynamicHpMult = 1.0;
      this._dynamicCountMult = 1.0;
    } else if (avg < 40) {
      this._dynamicHpMult    = isHard ? 1.10 : 1.15;
      this._dynamicCountMult = 1.0;
    } else if (avg < 60) {
      this._dynamicHpMult    = isHard ? 1.20 : 1.30;
      this._dynamicCountMult = 1.10;
    } else {
      this._dynamicHpMult    = isHard ? 1.25 : 1.50;
      this._dynamicCountMult = 1.20;
    }
    console.log(`[SurvivalEngine] Dynamic difficulty: avgLv=${avg.toFixed(1)} hpMult=${this._dynamicHpMult} countMult=${this._dynamicCountMult} diff=${this._difficulty}`);
  }

  // ==================== 内部：工具 ====================

  /**
   * 追踪贡献值（本局排行榜用） + 同步累加 `_lifetimeContrib`（等级用） + 触发 `_checkLevelUp`。
   * @param playerId
   * @param amount
   * @param source 'barrage' | 'gift' | 'combat' | undefined — 弹幕工作指令来源区分（阶2皮肤 ×1.5 弹幕贡献加成）
   */
  _trackContribution(playerId, amount, source) {
    if (!playerId) return;

    // 阶2 弹幕贡献 ×1.5（策划案 §30.3 阶2 专属被动）：仅作用于 contributions 与 _lifetimeContrib
    let finalAmount = amount;
    if (source === 'barrage' && this._getWorkerTier(playerId) >= 2) {
      finalAmount = amount * 1.5;
    }

    // §24.4 double_contrib：全员贡献 ×2（60s），仅作用于正值加分；资源产出倍率不受影响
    if (finalAmount > 0 && this._contribMult > 1.0) {
      finalAmount = finalAmount * this._contribMult;
    }

    this.contributions[playerId] = (this.contributions[playerId] || 0) + finalAmount;

    // ── §30 新玩家追赶加速（×3 仅作用于 _lifetimeContrib，contributions 不受影响）─────
    // 策划案 §30.8：只要 `_lifetimeContrib < 100` 就持续 ×3（覆盖完整"追赶期"），
    // 直到累计满 100 后自动回归 ×1；不影响 contributions（排行榜公平）。
    // TODO §30.4：每日衰减（UTC+8 00:00，`newLevel = floor(currentLevel × 0.95)` / 活跃 ×0.975）
    //   —— MVP 未实现，等 §30 持久化（WeeklyRankingStore 扩展）落地后再加日重置定时器。
    const currentLc   = this._lifetimeContrib[playerId] || 0;
    const catchUpMult = (currentLc < 100 && finalAmount > 0) ? 3 : 1;
    this._lifetimeContrib[playerId] = currentLc + finalAmount * catchUpMult;

    // ── §39 商店：_contribBalance 与 _lifetimeContrib 同步增长（同 catchUpMult）────
    // 策划案 §39.3 不变式：_addContribution 前后 _lifetimeContrib 的差值 = _contribBalance 的差值
    // 该字段独立于 contributions（本局）与 _lifetimeContrib（终身水位）：
    //   - B 类购买仅扣 _contribBalance，_lifetimeContrib 不变（等级/皮肤/豁免不受影响）
    //   - 不可为负（购买前 handleShopPurchase 校验）
    // TODO §39.10 持久化：目前 MVP 内存存储，重启清零；等 RoomPersistence schemaVersion 1→2 落地
    this._contribBalance[playerId] = (this._contribBalance[playerId] || 0) + finalAmount * catchUpMult;

    // 首次玩家初始等级为 Lv.1
    if (this._playerLevel[playerId] == null) this._playerLevel[playerId] = 1;
    // 等级检查 + 跨阶广播
    this._checkLevelUp(playerId);

    // 防抖：贡献变化后 1.5s 推送实时榜（避免每条弹幕都广播）
    this._scheduleLiveRankingBroadcast();
  }

  /** 防抖推送实时贡献榜（1.5s 内多次变化只推一次） */
  _scheduleLiveRankingBroadcast() {
    if (this._liveRankingTimer) return; // 已有待推送定时器，无需重复
    this._liveRankingTimer = setTimeout(() => {
      this._liveRankingTimer = null;
      this._broadcastLiveRanking();
    }, 1500);
  }

  /** 广播当前局 Top5 实时贡献榜（§19.2: state ∈ { 'day', 'night', 'recovery' }，其他状态跳过） */
  _broadcastLiveRanking() {
    if (this.state !== 'day' && this.state !== 'night' && this.state !== 'recovery') return;
    const top5 = Object.entries(this.contributions)
      .sort(([, a], [, b]) => b - a)
      .slice(0, 5)
      .map(([id, score], i) => ({
        rank:        i + 1,
        playerId:    id,
        playerName:  this.playerNames[id] || id,
        contribution: Math.round(score),
        // §39.5 身份装备（搭现有 live_ranking 广播捎带；空对象兼容老客户端）
        equipped:    this._playerShopEquipped[id] || {},
      }));
    this.broadcast({ type: 'live_ranking', timestamp: Date.now(), data: { rankings: top5 } });
  }

  /**
   * 给玩家加分（贡献值），同时可扩展为分离的积分系统。
   * 攻击/击杀来源标记为 'combat'，不触发 §30 阶2 弹幕 ×1.5 加成。
   */
  _addScore(playerId, playerName, amount) {
    this._trackContribution(playerId, amount, 'combat');
  }

  _buildRankings() {
    return Object.entries(this.contributions)
      .sort(([, a], [, b]) => b - a)
      .slice(0, 10)
      .map(([id, score], i) => ({
        rank:         i + 1,
        playerId:     id,
        playerName:   this.playerNames[id] || id,  // 结算排行榜显示名
        contribution: score,
        payout:       0,  // 由 _enterSettlement 按权重填充
      }));
  }

  // ==================== §24.4 主播事件轮盘：C→S handlers ====================

  /**
   * 主播点击"抽卡"按钮：仅当充能就绪且无 pending 时执行
   * PM 决策（MVP）：_roomCreatorId 未注入 → 放开给任何玩家；TODO: 等 _roomCreatorId 落地后加主播鉴权
   */
  handleBroadcasterRouletteSpin(playerId) {
    // TODO: if (this._roomCreatorId && playerId !== this._roomCreatorId) { broadcast roulette_forbidden; return; }
    if (this.state !== 'day' && this.state !== 'night') return;
    const ok = this._roulette.spin(Date.now());
    if (!ok) {
      console.log(`[SurvivalEngine] Roulette spin rejected (ready=${this._roulette._readyAt}, pending=${!!this._roulette._pending})`);
    }
  }

  /**
   * 客户端动画结束回调：执行对应卡片效果
   * cardId 由服务端 pending.cardId 确定，不信任客户端参数
   */
  handleBroadcasterRouletteApply(playerId) {
    // TODO: 主播鉴权（同 Spin）
    if (this.state !== 'day' && this.state !== 'night') return;
    const ok = this._roulette.apply(Date.now());
    if (!ok) {
      console.log(`[SurvivalEngine] Roulette apply rejected (no pending)`);
    }
  }

  /**
   * 主播在神秘商人面板选择 A/B：执行对应的资源兑换
   * choice: 'A' | 'B'；无效选项静默忽略
   */
  handleBroadcasterTraderAccept(playerId, choice) {
    // TODO: 主播鉴权（同 Spin）
    if (!this._traderOffer) {
      console.log(`[SurvivalEngine] Trader accept ignored: no active offer`);
      return;
    }
    if (Date.now() > this._traderOffer.expiresAt) {
      console.log(`[SurvivalEngine] Trader accept ignored: offer expired`);
      return;
    }
    if (choice !== 'A' && choice !== 'B') {
      console.log(`[SurvivalEngine] Trader accept ignored: invalid choice '${choice}'`);
      return;
    }

    const offer = this._traderOffer;
    let success = false;
    let result  = {};

    if (choice === 'A') {
      // A: 150 食物 → 100 矿石
      if (this.food >= offer.cardA.costFood) {
        this.food = Math.max(0, this.food - offer.cardA.costFood);
        this.ore  = Math.min(800, this.ore + offer.cardA.gainOre);
        success = true;
        result  = { choice: 'A', spent: { food: offer.cardA.costFood }, gained: { ore: offer.cardA.gainOre } };
      }
    } else {
      // B: 80 煤炭 → 城门 +50HP
      if (this.coal >= offer.cardB.costCoal) {
        this.coal   = Math.max(0, this.coal - offer.cardB.costCoal);
        this.gateHp = Math.min(this.gateMaxHp, this.gateHp + offer.cardB.gainGateHp);
        success = true;
        result  = { choice: 'B', spent: { coal: offer.cardB.costCoal }, gained: { gateHp: offer.cardB.gainGateHp } };
      }
    }

    // 清空 offer + 取消超时
    this._traderOffer = null;
    if (this._traderTimer) { clearTimeout(this._traderTimer); this._traderTimer = null; }

    this._broadcast({
      type: 'broadcaster_trader_result',
      data: { success, ...result, reason: success ? 'ok' : 'insufficient_resource' },
    });
    if (success) {
      this._broadcastResourceUpdate();
      console.log(`[SurvivalEngine] Trader: choice=${choice} ok`);
    } else {
      console.log(`[SurvivalEngine] Trader: choice=${choice} rejected (insufficient resource)`);
    }
  }

  // ==================== §24.4 主播事件轮盘：6 张卡效果实现 ====================

  /**
   * elite_raid：立即刷 1 只精英怪（HP/ATK 为当日精英的 2 倍；不计入客户端 maxAliveMonsters）
   * 30s 内未击杀 → 直接扣城门 HP（`_tickEliteRaid` 处理）；击杀后 scorePool +500
   * 即时效果（无持续 buff），返回 0
   */
  _applyEliteRaid(nowMs) {
    const cfg = getWaveConfig(this.currentDay || 1);
    // elite 基础（若当日无 elite 配置，用 normal 兜底）
    const baseHp  = (cfg.elite?.hp  || cfg.normal?.hp  || 300);
    const baseAtk = (cfg.elite?.atk || cfg.normal?.atk || 5);
    const hp  = Math.max(1, Math.round(baseHp  * 2));
    const atk = Math.max(1, Math.round(baseAtk * 2));

    const id = `eliteraid_${++this._monsterIdCounter}`;
    this._activeMonsters.set(id, {
      id,
      type: 'elite_raid', // 特殊 type，客户端据此跳过 maxAliveMonsters 上限
      maxHp: hp,
      currentHp: hp,
      atk,
    });
    this._eliteRaidMonsterId = id;
    this._eliteRaidEndsAt    = nowMs + 30 * 1000;

    // 广播 wave 让客户端生成 Prefab（count=1，spawnSide 随机）
    const sides = ['left', 'right', 'top'];
    const spawnSide = sides[Math.floor(Math.random() * sides.length)];
    this.broadcast({
      type: 'monster_wave',
      timestamp: nowMs,
      data: {
        waveIndex: 9900,           // elite_raid 专用标记，避免与普通 wave 冲突
        day:       this.currentDay,
        monsterId: 'elite_raid',   // 客户端按 monsterType 路由
        count:     1,
        spawnSide,
        monsterType: 'elite_raid', // 冗余字段方便客户端直接读
        bypassCap: true,           // 前端据此跳过 maxAliveMonsters
        eliteHp:   hp,
        eliteAtk:  atk,
      },
    });

    this.broadcast({
      type: 'bobao',
      timestamp: nowMs,
      data: { message: '直面精英，方显勇者！30秒内击杀方可脱险' },
    });
    console.log(`[SurvivalEngine] elite_raid spawned: id=${id} hp=${hp} atk=${atk} deadline=+30s`);
    return 0;
  }

  /** elite_raid tick 检测：30s 内未击杀 → 直接扣城门 30HP（精英"穿门"） */
  _tickEliteRaid(nowMs) {
    if (!this._eliteRaidEndsAt || nowMs < this._eliteRaidEndsAt) return;
    const mid = this._eliteRaidMonsterId;
    // 已击杀 → 清理状态
    if (!mid || !this._activeMonsters.has(mid)) {
      this._eliteRaidEndsAt    = 0;
      this._eliteRaidMonsterId = null;
      return;
    }
    // 未击杀 → 清理精英怪 + 城门受创 30HP
    this._activeMonsters.delete(mid);
    this._eliteRaidEndsAt    = 0;
    this._eliteRaidMonsterId = null;
    const dmg = 30;
    this.gateHp = Math.max(0, this.gateHp - dmg);
    this._broadcast({
      type: 'monster_died',
      data: { monsterId: mid, monsterType: 'elite_raid', killerId: '', reason: 'elite_raid_timeout' },
    });
    this.broadcast({
      type: 'bobao',
      timestamp: nowMs,
      data: { message: `精英来袭未能击杀！城门 -${dmg}HP` },
    });
    this._broadcastResourceUpdate();
    this._checkDefeat();
    console.log(`[SurvivalEngine] elite_raid timeout: gate -${dmg}HP → ${this.gateHp}`);
  }

  /**
   * time_freeze：全场存活怪物冻结 8s（对城门伤害暂停）
   * 复用 special_effect { frozen_all, duration: 8 }，客户端已有冻结动画
   * 返回 endsAt（effect 结束时广播 effect_ended）
   */
  _applyTimeFreeze(nowMs) {
    const durationMs = 8 * 1000;
    this._freezeUntilMs = nowMs + durationMs;
    this.broadcast({
      type: 'special_effect',
      timestamp: nowMs,
      data: { effect: 'frozen_all', duration: 8 },
    });
    this.broadcast({
      type: 'bobao',
      timestamp: nowMs,
      data: { message: '时空凝滞，喘息之机' },
    });
    return nowMs + durationMs;
  }

  /**
   * double_contrib：全员贡献值 ×2，60s（仅贡献值，资源产出倍率不变）
   * 返回 endsAt
   */
  _applyDoubleContrib(nowMs) {
    const durationMs = 60 * 1000;
    this._contribMult = 2.0;
    if (this._contribMultTimer) clearTimeout(this._contribMultTimer);
    this._contribMultTimer = setTimeout(() => {
      this._contribMult = 1.0;
      this._contribMultTimer = null;
      console.log('[SurvivalEngine] double_contrib expired');
    }, durationMs);
    this.broadcast({
      type: 'bobao',
      timestamp: nowMs,
      data: { message: '英雄辈出的时刻！全员贡献×2，持续60秒' },
    });
    return nowMs + durationMs;
  }

  /**
   * mystery_trader：30s 限时 2 选 1：
   *   A=150 食物→100 矿石 / B=80 煤炭→城门+50HP / 超时=弃权（不扣不给）
   * 不走 §5 ResourceSystem 衰减（即时兑换）
   * 返回 endsAt（offer 到期）
   */
  _applyMysteryTrader(nowMs) {
    const durationMs = 30 * 1000;
    const expiresAt  = nowMs + durationMs;
    // 扁平化结构:Unity JsonUtility 不支持嵌套 cost/gain 映射,统一用 costFood/gainOre 等扁平字段
    const offer = {
      expiresAt,
      cardA: { costFood: 150, costCoal: 0,  costOre: 0, gainFood: 0, gainCoal: 0, gainOre: 100, gainGateHp: 0  },
      cardB: { costFood: 0,   costCoal: 80, costOre: 0, gainFood: 0, gainCoal: 0, gainOre: 0,   gainGateHp: 50 },
    };
    this._traderOffer = offer;
    this._broadcast({
      type: 'broadcaster_trader_offer',
      data: { cardA: offer.cardA, cardB: offer.cardB, expiresAt },
    });
    this.broadcast({
      type: 'bobao',
      timestamp: nowMs,
      data: { message: '商队路过堡垒，30秒内选择交易' },
    });

    // 30s 超时兜底：自动弃权（不扣不给）
    if (this._traderTimer) clearTimeout(this._traderTimer);
    this._traderTimer = setTimeout(() => {
      if (!this._traderOffer) return;
      this._traderOffer = null;
      this._traderTimer = null;
      this._broadcast({
        type: 'broadcaster_trader_result',
        data: { success: false, reason: 'timeout' },
      });
      console.log('[SurvivalEngine] mystery_trader expired without choice');
    }, durationMs);

    return expiresAt;
  }

  /**
   * meteor_shower：15s 内每 1s 随机 30% 存活怪各 -100HP；无怪时城门 +10HP/tick
   * 使用 1s setTimeout 链（15 次），归一到 _meteorTimers 便于 reset 时清理
   * 返回 endsAt（最后一击 tick）
   */
  _applyMeteorShower(nowMs) {
    const ticks = 15;
    const durationMs = ticks * 1000;
    // 先清理旧计时器（极端情况下 reset 未清干净）
    for (const t of this._meteorTimers) clearTimeout(t);
    this._meteorTimers = [];

    for (let i = 1; i <= ticks; i++) {
      const timer = setTimeout(() => {
        this._meteorShowerTick();
      }, i * 1000);
      this._meteorTimers.push(timer);
    }
    this.broadcast({
      type: 'bobao',
      timestamp: nowMs,
      data: { message: '天降陨石！15秒流星雨降临' },
    });
    return nowMs + durationMs;
  }

  /** meteor_shower 的单次 tick：30% 概率伤害每只活怪；无怪时修城门 */
  _meteorShowerTick() {
    if (this.state !== 'day' && this.state !== 'night') return;
    const monsters = [...this._activeMonsters.entries()];
    if (monsters.length === 0) {
      // 无怪 → 城门 +10HP（每 tick）
      const before = this.gateHp;
      this.gateHp = Math.min(this.gateMaxHp, this.gateHp + 10);
      if (this.gateHp !== before) this._broadcastResourceUpdate();
      return;
    }

    const dmg = 100;
    const killed = [];
    for (const [mid, m] of monsters) {
      if (Math.random() >= 0.30) continue; // 30% 命中
      m.currentHp -= dmg;
      this._broadcast({
        type: 'combat_attack',
        data: {
          attackerId:   '',
          attackerName: '流星雨',
          targetId:     mid,
          targetType:   m.type,
          damage:       dmg,
          targetHpRemaining: Math.max(0, m.currentHp),
        },
      });
      if (m.currentHp <= 0) killed.push(mid);
    }
    for (const mid of killed) {
      const m = this._activeMonsters.get(mid);
      this._activeMonsters.delete(mid);
      this._broadcast({
        type: 'monster_died',
        data: { monsterId: mid, monsterType: m.type, killerId: '', reason: 'meteor_shower' },
      });
      // elite_raid 被流星雨击杀 → 清 pending 状态
      if (mid === this._eliteRaidMonsterId) {
        this._eliteRaidEndsAt    = 0;
        this._eliteRaidMonsterId = null;
      }
    }
  }

  /**
   * aurora：全矿工满血 + 效率 ×1.5（60s）+ 城门 +200HP
   * PM 决策：与 T5 love_explosion 的情形 A/B/C 细分不实现，两者独立生效、各自 clamp
   * 返回 endsAt
   */
  _applyAurora(nowMs) {
    const durationMs = 60 * 1000;

    // 全矿工满血（已死亡的复活到满血）
    for (const pid of Object.keys(this._workerHp)) {
      const w = this._workerHp[pid];
      if (w.isDead) {
        this._reviveWorker(pid);
      } else {
        w.hp = w.maxHp;
      }
    }
    this._broadcastWorkerHp();

    // 效率 ×1.5（60s）
    this._auroraEffMult = 1.5;
    if (this._auroraTimer) clearTimeout(this._auroraTimer);
    this._auroraTimer = setTimeout(() => {
      this._auroraEffMult = 1.0;
      this._auroraTimer = null;
      console.log('[SurvivalEngine] aurora expired');
    }, durationMs);

    // 城门 +200HP
    this.gateHp = Math.min(this.gateMaxHp, this.gateHp + 200);
    this._broadcastResourceUpdate();

    this.broadcast({
      type: 'bobao',
      timestamp: nowMs,
      data: { message: '极光守护，众神庇佑！效率+50% 持续60秒' },
    });

    return nowMs + durationMs;
  }

  // ==================== end §24.4 ====================

  // ==================== §38 探险系统（Expedition） ====================

  /**
   * 弹幕入口：`探` — 派出自己的矿工外出探险
   * 前置检查：day / 活跃 < 3 / 不在 expedition 中 / 矿工存活 / 非助威者
   */
  _handleExpeditionSend(playerId, playerName) {
    const failReason = this._startExpedition(playerId, Date.now());
    if (failReason) {
      this._broadcast({
        type: 'expedition_failed',
        data: {
          playerId,
          reason: failReason,
          unlockDay: 0, // MVP §36.12 不做解锁门槛，默认 0
        },
      });
      console.log(`[SurvivalEngine] expedition_failed: ${playerName} (${playerId}) reason=${failReason}`);
    }
  }

  /**
   * 弹幕入口：`召回` — 仅主播（MVP 放开给任意玩家，TODO _roomCreatorId 校验）
   * 扫描该玩家名下（PM 决策 MVP 简化：召回第一个匹配的）活跃 expedition，立即回城空手
   */
  _handleExpeditionRecall(playerId, playerName) {
    // TODO: _roomCreatorId 未注入 → 放开；等主播鉴权接入后限制非主播拒绝
    // if (this._roomCreatorId && playerId !== this._roomCreatorId) return;
    this._recallExpedition(playerId);
  }

  /**
   * 启动一次探险
   * @returns {string|null} 失败原因字符串（'wrong_phase'/'max_concurrent'/...），成功返回 null
   */
  _startExpedition(playerId, nowMs) {
    // §36.12 feature_locked 检查（MVP 不做，TODO）
    // if (!this.room?.isVeteran && this.room?.seasonDay < FEATURE_UNLOCK_DAY.expedition.minDay) return 'feature_locked';

    // 状态必须为白天（§38.5 wrong_phase；不含 recovery，MVP 无此状态）
    if (this.state !== 'day') return 'wrong_phase';

    // §33 助威者不能发 send（避免无限消耗守护者名额）
    if (this._supporters.has(playerId)) return 'supporter_not_allowed';

    // 必须有贡献条目（即正式守护者）
    if (this.contributions[playerId] === undefined) return 'supporter_not_allowed';

    // 已在外探险 → duplicate
    for (const exp of this._expeditions.values()) {
      if (exp.playerId === playerId) return 'duplicate';
    }

    // 活跃 expedition 上限 3
    if (this._expeditions.size >= EXPEDITION_MAX_CONCURRENT) return 'max_concurrent';

    // 矿工必须存活（白天默认存活，但夜晚死亡未复活的矿工 _workerHp[pid].isDead 仍为 true，白天会被 _reviveAllWorkers 清掉）
    const w = this._workerHp[playerId];
    if (w && w.isDead) return 'worker_dead';

    // ── 成功：创建 expedition ──
    const expeditionId = `exp_${++this._expeditionIdCounter}`;
    const workerIdx    = this._getWorkerIndex(playerId);

    // 白天剩余时间 (s) — remainingTime 是当前状态剩余秒
    const dayRemainingSec = Math.max(0, this.remainingTime);
    let totalSec = EXPEDITION_TOTAL_SEC;
    let outboundSec = EXPEDITION_OUTBOUND_SEC;
    let eventSec    = EXPEDITION_EVENT_SEC;
    let returnSec   = EXPEDITION_RETURN_SEC;
    let forceDied   = false;

    // §38.6 白天兜底加速：若 remainingTime < 90s → 线性压缩
    if (dayRemainingSec > 0 && dayRemainingSec < EXPEDITION_TOTAL_SEC) {
      if (dayRemainingSec < 10) {
        // 剩余 < 10s：抵达时即 died，无法享受事件收益
        forceDied = true;
        totalSec = dayRemainingSec;
        outboundSec = Math.max(1, Math.floor(dayRemainingSec * (40/90)));
        eventSec    = Math.max(1, Math.floor(dayRemainingSec * (15/90)));
        returnSec   = Math.max(0, dayRemainingSec - outboundSec - eventSec);
      } else {
        const ratio = dayRemainingSec / EXPEDITION_TOTAL_SEC;
        outboundSec = Math.max(1, Math.round(EXPEDITION_OUTBOUND_SEC * ratio));
        eventSec    = Math.max(1, Math.round(EXPEDITION_EVENT_SEC * ratio));
        returnSec   = Math.max(1, dayRemainingSec - outboundSec - eventSec);
        totalSec    = outboundSec + eventSec + returnSec;
      }
    }

    const returnsAt = nowMs + totalSec * 1000;

    const exp = {
      expeditionId,
      playerId,
      playerName: this._getPlayerName(playerId),
      workerIdx,
      startAt:       nowMs,
      returnsAt,
      outboundSec, eventSec, returnSec,
      eventId:       null,
      eventEndsAt:   0,
      options:       null,
      userChoice:    null,
      outcome:       null,
      forceDied,
      outboundTimer: null,
      eventTimer:    null,
      returnTimer:   null,
    };

    exp.outboundTimer = setTimeout(() => this._triggerExpeditionEvent(expeditionId, Date.now()), outboundSec * 1000);
    this._expeditions.set(expeditionId, exp);

    this._broadcast({
      type: 'expedition_started',
      data: { playerId, workerIdx, expeditionId, returnsAt },
    });
    console.log(`[SurvivalEngine] expedition_started: ${this._getPlayerName(playerId)} id=${expeditionId} returnsAt=+${totalSec}s forceDied=${forceDied}`);

    return null;
  }

  /**
   * 40s 外出完成 → 抽取事件卡并广播 expedition_event（15s 等待主播/观众决策）
   */
  _triggerExpeditionEvent(expeditionId, nowMs) {
    const exp = this._expeditions.get(expeditionId);
    if (!exp) return;

    // 若 forceDied（白天末段发起 → 抵达即判死），事件阶段仍跑一个短流程以保持协议连贯
    // 选 lost_cache（空奖励）作为中性事件，返回阶段会覆盖为 died=true
    let eventId;
    if (exp.forceDied) {
      eventId = 'lost_cache';
    } else {
      eventId = this._rollExpeditionEvent(nowMs);
    }

    exp.eventId     = eventId;
    exp.eventEndsAt = nowMs + exp.eventSec * 1000;
    exp.options     = (eventId === 'trader_caravan') ? ['accept', 'cancel'] : null;

    this._broadcast({
      type: 'expedition_event',
      data: {
        expeditionId,
        eventId,
        eventEndsAt: exp.eventEndsAt,
        options:     exp.options,
      },
    });
    console.log(`[SurvivalEngine] expedition_event: id=${expeditionId} event=${eventId}`);

    exp.eventTimer = setTimeout(() => this._resolveExpeditionEvent(expeditionId, exp.userChoice), exp.eventSec * 1000);
  }

  /**
   * 按权重抽选事件卡；mystic_rune 24h 内最多 3 次，超限退化为 lost_cache
   */
  _rollExpeditionEvent(nowMs) {
    // mystic_rune 上限判定（剔除 24h 外的记录再计数）
    this._runeChargeLog = this._runeChargeLog.filter(ts => nowMs - ts < RUNE_CHARGE_WINDOW_MS);

    let r = Math.random();
    let picked = null;
    for (const { id, w } of EXPEDITION_EVENT_WEIGHTS) {
      if (r < w) { picked = id; break; }
      r -= w;
    }
    if (!picked) picked = 'lost_cache'; // fallback（浮点误差兜底）

    if (picked === 'mystic_rune' && this._runeChargeLog.length >= RUNE_CHARGE_DAILY_CAP) {
      console.log(`[SurvivalEngine] mystic_rune cap reached (${this._runeChargeLog.length}/${RUNE_CHARGE_DAILY_CAP}), downgraded to lost_cache`);
      picked = 'lost_cache';
    } else if (picked === 'mystic_rune') {
      this._runeChargeLog.push(nowMs);
    }
    return picked;
  }

  /**
   * 15s 事件窗口结束 → 计算 outcome，设返回定时器
   * @param userChoice trader_caravan 时的玩家决策（'accept'/'cancel'/null 超时）
   */
  _resolveExpeditionEvent(expeditionId, userChoice) {
    const exp = this._expeditions.get(expeditionId);
    if (!exp) return;

    const outcome = { type: 'success', resources: null, contributions: 0, died: false };

    if (exp.forceDied) {
      outcome.type = 'died';
      outcome.died = true;
    } else {
      switch (exp.eventId) {
        case 'lost_cache':
          outcome.resources = { food: 0, coal: LOST_CACHE_RES.coal, ore: LOST_CACHE_RES.ore };
          outcome.type = 'success';
          break;

        case 'wild_beasts': {
          // 30% 概率死亡；击杀 +200 贡献（死亡也入账，视作"死前战功"）
          const died = Math.random() < WILD_BEASTS_DEATH_RATE;
          outcome.contributions = WILD_BEASTS_CONTRIB;
          outcome.died = died;
          outcome.type = died ? 'died' : 'success';
          break;
        }

        case 'trader_caravan': {
          // accept 且资源足够 → 扣 food+ore，城门直升 Lv+1（PM 决策：简化直接升级）
          if (userChoice === 'accept' && this.food >= TRADER_COST_FOOD && this.ore >= TRADER_COST_ORE) {
            if (this.gateLevel < GATE_MAX_HP_BY_LEVEL.length) {
              this.food = Math.max(0, this.food - TRADER_COST_FOOD);
              this.ore  = Math.max(0, this.ore  - TRADER_COST_ORE);
              this.gateLevel = this.gateLevel + 1;
              this.gateMaxHp = GATE_MAX_HP_BY_LEVEL[this.gateLevel - 1];
              this.gateHp    = this.gateMaxHp;
              this._broadcast({
                type: 'gate_upgraded',
                data: {
                  newLevel:     this.gateLevel,
                  newMaxHp:     this.gateMaxHp,
                  oreRemaining: Math.round(this.ore),
                  upgradedBy:   exp.playerId || '',
                  reason:       'trader_caravan',
                },
              });
              this._broadcastResourceUpdate();
              console.log(`[SurvivalEngine] trader_caravan accepted: gate Lv→${this.gateLevel}`);
              outcome.type = 'success';
            } else {
              outcome.type = 'empty'; // 已满级，空手
            }
          } else {
            // cancel / 超时 / 资源不足 → 空手
            outcome.type = 'empty';
          }
          break;
        }

        case 'meteor_fragment':
          // 下次 666 触发时 efficiency666Bonus ×2.0（单次）
          this._meteorFragmentPending = true;
          outcome.type = 'success';
          console.log(`[SurvivalEngine] meteor_fragment pending: next 666 will double`);
          break;

        case 'bandit_raid':
          outcome.type = 'empty';
          break;

        case 'mystic_rune':
          // §24.4 轮盘充能立即补满（不生成 pending）
          if (this._roulette) {
            const ro = this._roulette;
            const alreadyReady = (ro._readyAt === 0);
            const hasPending   = !!ro._pending;
            if (!alreadyReady && !hasPending) {
              ro._readyAt = 0;
              this._broadcast({
                type: 'broadcaster_roulette_ready',
                data: { readyAt: -1, source: 'mystic_rune' },
              });
              console.log(`[SurvivalEngine] mystic_rune: roulette charged to ready`);
            } else {
              console.log(`[SurvivalEngine] mystic_rune: no effect (ready=${alreadyReady} pending=${hasPending})`);
            }
          }
          outcome.type = 'success';
          break;

        default:
          outcome.type = 'empty';
      }
    }

    exp.outcome = outcome;

    // 设返回定时器
    exp.returnTimer = setTimeout(() => this._returnExpedition(expeditionId, Date.now()), exp.returnSec * 1000);
  }

  /**
   * 35s 返回 → 应用 outcome 到 engine 状态 + 广播 expedition_returned + 清理
   */
  _returnExpedition(expeditionId, nowMs) {
    const exp = this._expeditions.get(expeditionId);
    if (!exp) return;
    const { playerId, outcome } = exp;

    // 清理定时器（防 double-fire）
    if (exp.outboundTimer) clearTimeout(exp.outboundTimer);
    if (exp.eventTimer)    clearTimeout(exp.eventTimer);
    if (exp.returnTimer)   clearTimeout(exp.returnTimer);

    // 应用资源
    if (outcome.resources) {
      const r = outcome.resources;
      if (r.food) this.food = Math.min(2000, this.food + r.food);
      if (r.coal) this.coal = Math.min(1500, this.coal + r.coal);
      if (r.ore)  this.ore  = Math.min(800,  this.ore  + r.ore);
      this._broadcastResourceUpdate();
    }

    // 应用贡献（§30 双轨制：_trackContribution 同步累加 contributions + _lifetimeContrib）
    if (outcome.contributions > 0) {
      this._trackContribution(playerId, outcome.contributions, 'combat');
    }

    // 应用死亡
    if (outcome.died) {
      // 若矿工 HP 记录存在 → 标记为 dead + 设复活时刻；否则初始化一份
      if (!this._workerHp[playerId]) {
        const maxHp = this._getWorkerMaxHp(playerId);
        this._workerHp[playerId] = { hp: 0, maxHp, isDead: true, respawnAt: 0 };
      }
      const w = this._workerHp[playerId];
      w.hp = 0;
      w.isDead = true;
      // 白天抵达 → 下次 _enterNight 之前由 _reviveAllWorkers('day_started') 复活（§2.2 既有路径）
      // 已入夜抵达 → 进入 §30 30s 复活倒计时（按 tier 可能为 20s）
      if (this.state === 'night') {
        const respawnSec = this._getWorkerRespawnSec(playerId);
        w.respawnAt = nowMs + respawnSec * 1000;
        // 设复活定时器（与 _spawnWave 中的路径一致）
        const respawnTimer = setTimeout(() => {
          if (this._workerHp[playerId]?.isDead) this._reviveWorker(playerId);
        }, respawnSec * 1000);
        this._waveTimers.push(respawnTimer);
        this._broadcast({
          type: 'worker_died',
          data: { playerId, respawnAt: w.respawnAt, reason: 'expedition_died' },
        });
      } else {
        w.respawnAt = 0;
        this._broadcast({
          type: 'worker_died',
          data: { playerId, respawnAt: 0, reason: 'expedition_died' },
        });
      }
      this._broadcastWorkerHp();
    }

    // 广播 returned
    this._broadcast({
      type: 'expedition_returned',
      data: {
        playerId,
        expeditionId,
        outcome: {
          type:          outcome.type,
          resources:     outcome.resources || null,
          contributions: outcome.contributions || 0,
          died:          !!outcome.died,
        },
      },
    });
    console.log(`[SurvivalEngine] expedition_returned: ${exp.playerName} id=${expeditionId} type=${outcome.type} died=${outcome.died} contrib=${outcome.contributions}`);

    this._expeditions.delete(expeditionId);
  }

  /**
   * 召回（主播专用 MVP 放开）：取消该玩家所有 expedition 定时器，立即发 expedition_returned 空手
   */
  _recallExpedition(playerId) {
    let recalled = 0;
    for (const [expId, exp] of [...this._expeditions.entries()]) {
      if (exp.playerId !== playerId) continue;
      if (exp.outboundTimer) clearTimeout(exp.outboundTimer);
      if (exp.eventTimer)    clearTimeout(exp.eventTimer);
      if (exp.returnTimer)   clearTimeout(exp.returnTimer);
      this._broadcast({
        type: 'expedition_returned',
        data: {
          playerId,
          expeditionId: expId,
          outcome: { type: 'empty', resources: null, contributions: 0, died: false },
        },
      });
      this._expeditions.delete(expId);
      recalled++;
    }
    if (recalled > 0) {
      console.log(`[SurvivalEngine] expedition recalled: ${this._getPlayerName(playerId)} count=${recalled}`);
    }
  }

  /**
   * C→S 入口：主播/观众对 expedition_event（trader_caravan）投票
   * MVP：仅 trader_caravan 有 accept/cancel 分支；其他 eventId 无需交互
   */
  handleExpeditionEventVote(playerId, expeditionId, choice) {
    const exp = this._expeditions.get(expeditionId);
    if (!exp) return;
    if (!exp.eventId || exp.eventId !== 'trader_caravan') return;
    if (choice !== 'accept' && choice !== 'cancel') return;
    // MVP：不限制只有主播，幂等记录最新选择；到 eventEndsAt 时决议
    exp.userChoice = choice;
  }

  /**
   * C→S 入口：expedition_command { action: 'send' | 'recall' }
   */
  handleExpeditionCommand(playerId, action) {
    if (!playerId) return;
    if (action === 'send') {
      this._handleExpeditionSend(playerId, this._getPlayerName(playerId));
    } else if (action === 'recall') {
      this._handleExpeditionRecall(playerId, this._getPlayerName(playerId));
    }
  }

  /**
   * 取消所有在外探险（reset / 失败降级 / 攻防战打断时调用），无资源回馈
   */
  _cancelAllExpeditions(reason) {
    const count = this._expeditions.size;
    for (const exp of this._expeditions.values()) {
      if (exp.outboundTimer) clearTimeout(exp.outboundTimer);
      if (exp.eventTimer)    clearTimeout(exp.eventTimer);
      if (exp.returnTimer)   clearTimeout(exp.returnTimer);
    }
    this._expeditions.clear();
    if (count > 0) {
      console.log(`[SurvivalEngine] All expeditions cancelled (${count}) reason=${reason}`);
    }
  }

  /**
   * §38.6 夜晚到来兜底：扫描在外探险，若 returnsAt 晚于夜晚到来时刻 → 强制立即归来且 died=true
   * _enterNight 开头调用
   */
  _sweepExpeditionsOnNightStart(nowMs) {
    for (const [expId, exp] of [...this._expeditions.entries()]) {
      // 夜晚到来时仍在外：强制死亡归来
      if (exp.returnsAt > nowMs) {
        if (exp.outboundTimer) clearTimeout(exp.outboundTimer);
        if (exp.eventTimer)    clearTimeout(exp.eventTimer);
        if (exp.returnTimer)   clearTimeout(exp.returnTimer);
        exp.outcome = exp.outcome || { type: 'died', resources: null, contributions: 0, died: true };
        exp.outcome.died = true;
        exp.outcome.type = 'died';
        // 立即 return（会标记矿工死亡 + 进入 §30 30s 倒计时，因已进入夜晚）
        // 注意：_returnExpedition 读 this.state；此处尚未切换到 night，先存起来延迟 1ms 执行
        // 但 _enterNight 调用在 state 切换之前 → 直接调用会走白天路径，不进入 respawn 倒计时
        // 故手动实现对等逻辑（死亡 + 广播 worker_died / expedition_returned），避开 state 依赖
        this._applyExpeditionDeathNow(exp, nowMs);
        this._expeditions.delete(expId);
      }
    }
  }

  /**
   * 夜晚兜底专用：立即应用 expedition 死亡（不依赖 state），广播 worker_died + expedition_returned
   * 调用时机：_enterNight 开头，此时 state 仍为 'day'，但夜晚即将到来 → 走夜晚死亡路径
   */
  _applyExpeditionDeathNow(exp, nowMs) {
    const playerId = exp.playerId;
    if (!this._workerHp[playerId]) {
      const maxHp = this._getWorkerMaxHp(playerId);
      this._workerHp[playerId] = { hp: 0, maxHp, isDead: true, respawnAt: 0 };
    }
    const w = this._workerHp[playerId];
    w.hp = 0;
    w.isDead = true;
    const respawnSec = this._getWorkerRespawnSec(playerId);
    w.respawnAt = nowMs + respawnSec * 1000;
    const respawnTimer = setTimeout(() => {
      if (this._workerHp[playerId]?.isDead) this._reviveWorker(playerId);
    }, respawnSec * 1000);
    this._waveTimers.push(respawnTimer);

    this._broadcast({
      type: 'worker_died',
      data: { playerId, respawnAt: w.respawnAt, reason: 'expedition_night_kia' },
    });
    this._broadcast({
      type: 'expedition_returned',
      data: {
        playerId,
        expeditionId: exp.expeditionId,
        outcome: { type: 'died', resources: null, contributions: 0, died: true },
      },
    });
    this._broadcastWorkerHp();
    console.log(`[SurvivalEngine] expedition KIA at night start: ${this._getPlayerName(playerId)} id=${exp.expeditionId}`);
  }

  // ==================== end §38 ====================

  // ==================== §39 商店系统（Shop System，MVP） ====================
  // 策划案 §39（第 6709–7115 行）。实现范围：
  //   - A 类 4 件战术即时道具（worker_pep_talk / gate_quickpatch / emergency_alert / spotlight）
  //   - B 类固定 8 件身份装备
  //   - 12 协议 handler（shop_list / shop_purchase_prepare / shop_purchase / shop_equip 输入；
  //                       shop_list_data / shop_purchase_confirm_prompt / shop_purchase_confirm /
  //                       shop_purchase_failed / shop_equip_changed / shop_equip_failed /
  //                       shop_inventory_data / shop_effect_triggered 输出）
  //   - 弹幕 `买A<n>` / `买B<n>` / `装XY`（handleComment 内集成）
  // PM 决策（MVP）见 SHOP_CATALOG 注释。

  /**
   * §39 工具：校验 itemId 是否允许在当前 phase 购买（§39.6 购买窗口）
   * state 合法值：idle | loading | day | night | settlement
   *   MVP 无独立 `recovery` variant（策划案中 recovery = running 的第三种，本版归入 day 口径）
   */
  _shopIsPhaseAllowed(itemId) {
    const cfg = SHOP_CATALOG[itemId];
    if (!cfg) return false;
    const st = this.state;
    if (cfg.category === 'B') {
      // B 类：day/night/settlement 均可（身份装备结算后也能买）
      return st === 'day' || st === 'night' || st === 'settlement';
    }
    // A 类：按 itemId 细分
    switch (itemId) {
      case 'worker_pep_talk':
      case 'emergency_alert':
        return st === 'day';
      case 'gate_quickpatch':
      case 'spotlight':
        return st === 'day' || st === 'night';
      default:
        return false;
    }
  }

  /** §39 工具：生成 UUID-like pendingId（MVP 轻量实现，避免引入 uuid 依赖） */
  _shopGenPendingId() {
    this._shopPendingIdCounter = (this._shopPendingIdCounter || 0) + 1;
    return `pend_${Date.now().toString(36)}_${this._shopPendingIdCounter}_${Math.floor(Math.random() * 0xfffff).toString(36)}`;
  }

  /** §39 工具：推送 shop_purchase_failed（带 reason + 可选附加字段） */
  _shopFailPurchase(reason, itemId, extra) {
    const data = { reason, itemId: itemId || null };
    if (extra) Object.assign(data, extra);
    this._broadcast({ type: 'shop_purchase_failed', data });
  }

  /**
   * handleShopList: 应答 shop_list 请求（C→S）
   * 返回 SHOP_CATALOG 过滤后的 items 数组。
   * A 类：按索引顺序列出 4 件；B 类：按索引顺序列出 8 件固定 SKU + owned 历史限定（MVP 暂无限定，列表为空）。
   * @param {string} playerId 请求者（用于 owned 合并，MVP 未使用）
   * @param {string} category 'A' | 'B'
   */
  handleShopList(playerId, category) {
    const items = [];
    if (category === 'A') {
      for (const itemId of SHOP_A_INDEX) {
        const cfg = SHOP_CATALOG[itemId];
        if (!cfg) continue;
        items.push({
          itemId,
          name: itemId,
          price: cfg.price,
          slot: cfg.slot || null,
          category: cfg.category,
          effect: cfg.effect || '',
          minLifetimeContrib: 0,
          limitedSeasonId: '',
        });
      }
    } else if (category === 'B') {
      for (const itemId of SHOP_B_INDEX) {
        const cfg = SHOP_CATALOG[itemId];
        if (!cfg) continue;
        items.push({
          itemId,
          name: itemId,
          price: cfg.price,
          slot: cfg.slot || null,
          category: cfg.category,
          effect: cfg.effect || '',
          minLifetimeContrib: 0,
          limitedSeasonId: '',
        });
      }
      // B9/B10 赛季限定 SKU 跳过（currentSeasonShopPool 始终空，PM MVP 决策）
    }
    // 应答：按策划案 §39.9，S→C shop_list_data { category, items }
    this._broadcast({
      type: 'shop_list_data',
      data: { playerId: playerId || '', category, items },
    });
  }

  /**
   * handleShopPurchasePrepare: 双重确认前置（C→S shop_purchase_prepare）
   * 触发条件（且）：isRoomCreator（MVP 放开） + B 类 + price ≥ 1000
   * 不满足 → 静默忽略（避免普通玩家占 pending 池）
   * 满足 → 生成 pendingId UUID，存 _shopPendingPurchases，推送 shop_purchase_confirm_prompt
   */
  handleShopPurchasePrepare(playerId, itemId) {
    if (!playerId) return;
    const cfg = SHOP_CATALOG[itemId];
    if (!cfg) return;                          // 静默忽略
    if (cfg.category !== 'B') return;          // 静默忽略（A 类无双确认）
    if (cfg.price < 1000) return;              // 静默忽略（<1000 直接购买）
    // MVP PM 决策：_roomCreatorId 鉴权放开，任何玩家都可发起 prepare

    // 校验余额（余额不足直接 shop_purchase_failed，不进 pending 池）
    const balance = this._contribBalance[playerId] || 0;
    if (balance < cfg.price) {
      return this._shopFailPurchase('insufficient', itemId);
    }

    // 已拥有 → already_owned（prepare 阶段就拦截，避免浪费 pending）
    const owned = this._playerShopInventory[playerId] || [];
    if (owned.includes(itemId)) {
      return this._shopFailPurchase('already_owned', itemId);
    }

    // 清掉该玩家已有的旧 pending（同主播同时最多 1 个 pending，§39.7）
    for (const [pid, p] of this._shopPendingPurchases) {
      if (p.playerId === playerId) this._shopPendingPurchases.delete(pid);
    }

    const pendingId = this._shopGenPendingId();
    const expiresAt = Date.now() + SHOP_PENDING_TTL_MS;
    this._shopPendingPurchases.set(pendingId, {
      playerId,
      itemId,
      price: cfg.price,
      expiresAt,
    });

    this._broadcast({
      type: 'shop_purchase_confirm_prompt',
      data: { playerId, pendingId, itemId, price: cfg.price, expiresAt },
    });
    console.log(`[Shop] prepare: player=${this._getPlayerName(playerId)} itemId=${itemId} price=${cfg.price} pending=${pendingId}`);
  }

  /**
   * handleShopPurchase: 核心购买流程（C→S shop_purchase / 弹幕 买A<n> 买B<n>）
   * 流程（§39.7）：
   *  1. 校验 itemId 存在 → 否则 item_not_found
   *  2. 校验 phase（§39.6 表）→ 否则 wrong_phase
   *  3. pendingId 校验（若传）：不存在 pending_invalid / 已过期 pending_expired / 匹配则删除一次性
   *  4. 扣费：A 类从 contributions；B 类从 _contribBalance + owned 检查
   *  5. A 类特殊校验：supporter_not_allowed / no_effect / spotlight_active / limit_exceeded / per_game_limit
   *  6. A 类效果执行
   *  7. B 类 → owned.push(itemId)
   *  8. 广播 shop_purchase_confirm（房间广播，带 remainingContrib / remainingBalance）
   */
  handleShopPurchase(playerId, playerName, itemId, pendingId) {
    if (!playerId) return;

    // 1) itemId 存在性
    const cfg = SHOP_CATALOG[itemId];
    if (!cfg) return this._shopFailPurchase('item_not_found', itemId);

    // 2) phase 校验
    if (!this._shopIsPhaseAllowed(itemId)) {
      return this._shopFailPurchase('wrong_phase', itemId);
    }

    // 3) pendingId 校验（若有）：一次性凭证
    if (pendingId) {
      const pending = this._shopPendingPurchases.get(pendingId);
      if (!pending) {
        return this._shopFailPurchase('pending_invalid', itemId);
      }
      if (Date.now() > pending.expiresAt) {
        this._shopPendingPurchases.delete(pendingId);
        return this._shopFailPurchase('pending_expired', itemId);
      }
      // playerId + itemId 一致性
      if (pending.playerId !== playerId || pending.itemId !== itemId) {
        return this._shopFailPurchase('pending_invalid', itemId);
      }
      // 匹配成功 → 一次性删除
      this._shopPendingPurchases.delete(pendingId);
    }

    // 4) 扣费 + 特殊校验
    const pname = playerName || this._getPlayerName(playerId);
    if (cfg.category === 'A') {
      // 助威者禁 A 类
      if (this._supporters.has(playerId)) {
        return this._shopFailPurchase('supporter_not_allowed', itemId);
      }

      // A 类特殊预检（在扣费前，避免"无效购买"也扣钱）
      if (itemId === 'gate_quickpatch') {
        if (this.gateHp >= this.gateMaxHp) {
          return this._shopFailPurchase('no_effect', itemId);  // 满血不扣费
        }
      }
      if (itemId === 'spotlight') {
        const active = this._shopSpotlightActive[playerId];
        if (active && Date.now() < active.endsAt) {
          return this._shopFailPurchase('spotlight_active', itemId);  // 激活中不扣费
        }
        if (this._shopSpotlightUsedThisGame[playerId]) {
          return this._shopFailPurchase('limit_exceeded', itemId);   // 本局已用过不扣费
        }
      }
      if (itemId === 'emergency_alert') {
        // 同波次同玩家限 1 次
        const lastWave = this._shopEmergencyAlertUsedWave[playerId];
        const curWave  = this.currentDay;  // 以 currentDay 作为 wave 代理（MVP 简化）
        if (lastWave !== undefined && lastWave === curWave) {
          return this._shopFailPurchase('per_game_limit', itemId);
        }
      }

      // 余额校验（A 类从 contributions 扣）
      const curContrib = this.contributions[playerId] || 0;
      if (curContrib < cfg.price) {
        return this._shopFailPurchase('insufficient', itemId);
      }
      // 扣费
      this.contributions[playerId] = curContrib - cfg.price;

      // 5) A 类效果执行
      this._shopApplyAEffect(itemId, playerId, pname);

      // 6) 广播购买成功（A 类带 remainingContrib，不带 remainingBalance）
      this._broadcast({
        type: 'shop_purchase_confirm',
        data: {
          playerId,
          playerName: pname,
          itemId,
          category: 'A',
          remainingContrib: Math.round(this.contributions[playerId] || 0),
        },
      });
      console.log(`[Shop] A purchase: ${pname} → ${itemId} (price ${cfg.price}, remaining ${this.contributions[playerId]})`);
    } else if (cfg.category === 'B') {
      // B 类：owned 检查
      if (!this._playerShopInventory[playerId]) this._playerShopInventory[playerId] = [];
      const owned = this._playerShopInventory[playerId];
      if (owned.includes(itemId)) {
        return this._shopFailPurchase('already_owned', itemId);
      }

      // 余额校验（B 类从 _contribBalance 扣）
      const balance = this._contribBalance[playerId] || 0;
      if (balance < cfg.price) {
        return this._shopFailPurchase('insufficient', itemId);
      }
      // 扣费 + 入库存
      this._contribBalance[playerId] = balance - cfg.price;
      owned.push(itemId);

      // B 类广播购买成功（带 remainingBalance，不带 remainingContrib）
      this._broadcast({
        type: 'shop_purchase_confirm',
        data: {
          playerId,
          playerName: pname,
          itemId,
          category: 'B',
          remainingBalance: Math.round(this._contribBalance[playerId] || 0),
        },
      });
      console.log(`[Shop] B purchase: ${pname} → ${itemId} (price ${cfg.price}, balance ${this._contribBalance[playerId]})`);
    }
  }

  /**
   * §39 A 类效果执行（由 handleShopPurchase 扣费成功后调用）
   * worker_pep_talk: 下一波前 30s 采矿 +15%
   * gate_quickpatch: 城门 +100 HP（不超 maxHp）
   * emergency_alert: 立即广播 monster_wave_incoming
   * spotlight:       10s 高亮 + BarrageMessageUI 下一条弹幕 ★
   */
  _shopApplyAEffect(itemId, sourcePlayerId, sourcePlayerName) {
    const now = Date.now();
    switch (itemId) {
      case 'worker_pep_talk': {
        // §39.2 A1：下一波出场前 30s 全员采矿 +15%（MVP 简化为"从现在起 30s 内"）
        //   _applyWorkEffect 用 Math.max(pepTalkBoost, eff666Boost) 与 666/ability_pill 并取
        this._peptTalkBoostUntil = now + SHOP_PEP_TALK_DURATION_MS;
        this._broadcast({
          type: 'shop_effect_triggered',
          data: {
            itemId,
            sourcePlayerId,
            sourcePlayerName,
            targetPlayerId: '',
            durationSec: Math.floor(SHOP_PEP_TALK_DURATION_MS / 1000),
          },
        });
        console.log(`[Shop] worker_pep_talk active for 30s by ${sourcePlayerName}`);
        break;
      }

      case 'gate_quickpatch': {
        // §39.2 A2：立即 +100 HP（不超 gateMaxHp）
        const hpBefore = Math.round(this.gateHp);
        this.gateHp = Math.min(this.gateMaxHp, this.gateHp + SHOP_GATE_QUICKPATCH_HP);
        const hpAfter = Math.round(this.gateHp);
        this._broadcast({
          type: 'shop_effect_triggered',
          data: {
            itemId,
            sourcePlayerId,
            sourcePlayerName,
            targetPlayerId: '',
            durationSec: 0,
            metadata: { gateHpBefore: hpBefore, gateHpAfter: hpAfter, waveIdx: 0, leadSec: 0 },
          },
        });
        // 同步资源（gateHp 改变）
        this._broadcastResourceUpdate();
        console.log(`[Shop] gate_quickpatch by ${sourcePlayerName}: ${hpBefore} → ${hpAfter}`);
        break;
      }

      case 'emergency_alert': {
        // §39.2 A3：立即推送一次 monster_wave_incoming；标记 _shopEmergencyAlertUsedWave
        const leadSec = SHOP_EMERGENCY_ALERT_LEAD_SEC;
        const waveIdx = this.currentDay; // MVP 简化：以 currentDay 代理 waveIdx
        const spawnsAt = now + leadSec * 1000;
        const firstAttackAt = spawnsAt + 3000;
        this._shopEmergencyAlertUsedWave[sourcePlayerId] = waveIdx;
        this._broadcast({
          type: 'monster_wave_incoming',
          data: {
            waveIndex: waveIdx,
            spawnsAt,
            firstAttackAt,
            leadSec,
          },
        });
        this._broadcast({
          type: 'shop_effect_triggered',
          data: {
            itemId,
            sourcePlayerId,
            sourcePlayerName,
            targetPlayerId: '',
            durationSec: 0,
            metadata: { gateHpBefore: 0, gateHpAfter: 0, waveIdx, leadSec },
          },
        });
        console.log(`[Shop] emergency_alert by ${sourcePlayerName}: waveIdx=${waveIdx} leadSec=${leadSec}`);
        break;
      }

      case 'spotlight': {
        // §39.2 A4：10s 高亮 + 本局限 1 次
        const endsAt = now + SHOP_SPOTLIGHT_DURATION_MS;
        this._shopSpotlightActive[sourcePlayerId] = { endsAt };
        // setTimeout 10s 清理激活并标记本局已用
        if (this._shopSpotlightTimers[sourcePlayerId]) {
          clearTimeout(this._shopSpotlightTimers[sourcePlayerId]);
        }
        this._shopSpotlightTimers[sourcePlayerId] = setTimeout(() => {
          delete this._shopSpotlightActive[sourcePlayerId];
          delete this._shopSpotlightTimers[sourcePlayerId];
          this._shopSpotlightUsedThisGame[sourcePlayerId] = true;
        }, SHOP_SPOTLIGHT_DURATION_MS);
        this._broadcast({
          type: 'shop_effect_triggered',
          data: {
            itemId,
            sourcePlayerId,
            sourcePlayerName,
            targetPlayerId: sourcePlayerId,  // spotlight 目标 = 源自己
            durationSec: Math.floor(SHOP_SPOTLIGHT_DURATION_MS / 1000),
          },
        });
        console.log(`[Shop] spotlight by ${sourcePlayerName} for 10s`);
        break;
      }

      default:
        console.warn(`[Shop] _shopApplyAEffect: unknown itemId=${itemId}`);
    }
  }

  /**
   * handleShopEquip: 装备槽切换（C→S shop_equip）
   * 校验：slot ∈ {title,frame,entrance,barrage} + itemId ∈ owned + SHOP_CATALOG[itemId].slot === slot + 2s 冷却
   * 成功 → 更新 _playerShopEquipped + _shopLastEquipAt，unicast shop_equip_changed
   * 失败 → unicast shop_equip_failed { reason: slot_mismatch | not_owned | too_frequent, slot, itemId }
   */
  handleShopEquip(playerId, slot, itemId) {
    if (!playerId) return;
    const validSlots = ['title', 'frame', 'entrance', 'barrage'];
    if (!validSlots.includes(slot)) {
      return this._broadcast({
        type: 'shop_equip_failed',
        data: { playerId, reason: 'slot_mismatch', slot: slot || '', itemId: itemId || '' },
      });
    }

    // 2s 冷却（卸下也走同冷却）
    const now = Date.now();
    const last = this._shopLastEquipAt[playerId] || 0;
    if (now - last < SHOP_EQUIP_COOLDOWN_MS) {
      return this._broadcast({
        type: 'shop_equip_failed',
        data: { playerId, reason: 'too_frequent', slot, itemId: itemId || '' },
      });
    }

    if (itemId === null || itemId === undefined || itemId === '') {
      // 卸下该槽位
      if (!this._playerShopEquipped[playerId]) this._playerShopEquipped[playerId] = {};
      delete this._playerShopEquipped[playerId][slot];
      this._shopLastEquipAt[playerId] = now;
      return this._broadcast({
        type: 'shop_equip_changed',
        data: { playerId, slot, itemId: '' },
      });
    }

    // 装上：owned 校验 + slot 匹配
    const cfg = SHOP_CATALOG[itemId];
    if (!cfg) {
      return this._broadcast({
        type: 'shop_equip_failed',
        data: { playerId, reason: 'not_owned', slot, itemId },
      });
    }
    if (cfg.slot !== slot) {
      return this._broadcast({
        type: 'shop_equip_failed',
        data: { playerId, reason: 'slot_mismatch', slot, itemId },
      });
    }
    const owned = this._playerShopInventory[playerId] || [];
    if (!owned.includes(itemId)) {
      return this._broadcast({
        type: 'shop_equip_failed',
        data: { playerId, reason: 'not_owned', slot, itemId },
      });
    }

    // 成功：更新装备 + 冷却
    if (!this._playerShopEquipped[playerId]) this._playerShopEquipped[playerId] = {};
    this._playerShopEquipped[playerId][slot] = itemId;
    this._shopLastEquipAt[playerId] = now;

    this._broadcast({
      type: 'shop_equip_changed',
      data: { playerId, slot, itemId },
    });

    // 若装上 entrance_spark → 触发入场特效（若 VIP 广播路径存在则复用；MVP log 占位）
    if (itemId === 'entrance_spark') {
      // TODO §39.13 / §17 VIPAnnouncementUI：若房间已有 VIPAnnouncement 路径则触发
      //   MVP：log 占位，避免依赖未落地的 VIP 协议
      console.log(`[Shop] entrance_spark equipped by ${this._getPlayerName(playerId)} — VIP path TODO`);
    }

    console.log(`[Shop] equip: ${this._getPlayerName(playerId)} slot=${slot} itemId=${itemId}`);
  }

  /**
   * §39.7 pending TTL 扫描（_tick 内调用，每秒一次）
   * 超时 pending 自动清理；客户端若发 shop_purchase 会收到 pending_invalid
   */
  _shopCleanupExpiredPending() {
    const now = Date.now();
    for (const [pid, p] of this._shopPendingPurchases) {
      if (now > p.expiresAt) {
        this._shopPendingPurchases.delete(pid);
      }
    }
  }

  // ==================== end §39 ====================

  _clearTick() {
    if (this._tickTimer) { clearInterval(this._tickTimer); this._tickTimer = null; }
    if (this._resourceSyncTimer) { clearInterval(this._resourceSyncTimer); this._resourceSyncTimer = null; }
    this._tickCounter = 0;
  }

  _clearAllTimers() {
    this._clearTick();
    this._clearNightWaves();
    // §16 恢复期定时器（_enterSettlement 的 8s UI 定时器 / _enterRecovery 的 120s 定时器共用同一句柄）
    if (this._recoveryTimer) { clearTimeout(this._recoveryTimer); this._recoveryTimer = null; }
    // 清除 per-player 临时 boost 定时器（能量电池）
    for (const t of Object.values(this._playerTempBoostTimers || {})) clearTimeout(t);
    this._playerTempBoostTimers = {};
    // 清除随机事件超时
    for (const t of this._randomEventTimers) clearTimeout(t);
    this._randomEventTimers = [];
    // 清除实时榜防抖定时器
    if (this._liveRankingTimer) { clearTimeout(this._liveRankingTimer); this._liveRankingTimer = null; }

    // §24.4 轮盘相关定时器
    if (this._contribMultTimer) { clearTimeout(this._contribMultTimer); this._contribMultTimer = null; }
    if (this._auroraTimer)      { clearTimeout(this._auroraTimer);      this._auroraTimer      = null; }
    if (this._traderTimer)      { clearTimeout(this._traderTimer);      this._traderTimer      = null; }
    for (const t of (this._meteorTimers || [])) clearTimeout(t);
    this._meteorTimers = [];

    // §38 探险系统定时器：由 _cancelAllExpeditions 清理；此处保险兜底
    for (const exp of this._expeditions.values()) {
      if (exp.outboundTimer) clearTimeout(exp.outboundTimer);
      if (exp.eventTimer)    clearTimeout(exp.eventTimer);
      if (exp.returnTimer)   clearTimeout(exp.returnTimer);
    }
    // 不清 _expeditions Map（由 reset() / _cancelAllExpeditions 决定清时机）

    // §37 建造系统：清投票定时器 + 进行中建造定时器（不清 _buildings 集合）
    if (this._buildVote?.timer) { clearTimeout(this._buildVote.timer); this._buildVote.timer = null; }
    for (const [, info] of this._buildingInProgress) {
      if (info.timer) { clearTimeout(info.timer); info.timer = null; }
    }

    // §39 商店系统：清 spotlight 过期定时器（不清 _shopSpotlightActive，交给 reset() 决定）
    for (const t of Object.values(this._shopSpotlightTimers || {})) clearTimeout(t);
    this._shopSpotlightTimers = {};
  }
}

module.exports = SurvivalGameEngine;
