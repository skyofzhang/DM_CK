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

const crypto = require('crypto');
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

// 城门等级配置（策划案 §10 v2：Lv1→Lv6，每级新增战术属性）
// 升级消耗：index=lv-1 即当前等级升级所需矿石（Lv1→Lv2 需 100 矿）
const GATE_UPGRADE_COSTS    = [100, 250, 500, 1000, 1500];
const GATE_MAX_HP_BY_LEVEL  = [1000, 1500, 2200, 3000, 4000, 5500];
// 升级后 HP 奖励（立即补血，不超 gateMaxHp）；Lv6 特判：直接回满（代码中用 Infinity 占位，实际按 gateMaxHp 计算）
const GATE_HP_BONUS_ON_UP   = [0, 100, 200, 300, 500, Infinity];
const GATE_TIER_NAMES       = ['木栅栏', '加固木门', '铁皮厚门', '钢骨堡门', '寒冰壁垒', '巨龙要塞'];
// Lv1=1.0 基准，Lv2/3/4=1.5，Lv5/6=2.0（矿石自动修门速度倍率）
const GATE_AUTO_REPAIR_MULT = [1.0, 1.5, 1.5, 1.5, 2.0, 2.0];
// 减伤比例：Lv3 起 10%
const GATE_DMG_REDUCTION    = [0, 0, 0.10, 0.10, 0.10, 0.10];
// 反伤比例：Lv4 起 20%
const GATE_THORNS_RATIO     = [0, 0, 0, 0.20, 0.20, 0.20];
// 冰霜光环半径：Lv5 起 6 格（服务器不追踪怪物位置，由客户端本地按距离减速表现）
const GATE_FROST_RADIUS     = [0, 0, 0, 0, 6, 6];
// 每级城门对应的战术属性列表（Lv1 无特性）
const GATE_FEATURES_BY_LV   = [
  [],
  ['auto_repair_1.5'],
  ['auto_repair_1.5', 'dmg_reduction_10'],
  ['auto_repair_1.5', 'dmg_reduction_10', 'thorns_20'],
  ['auto_repair_2.0', 'dmg_reduction_10', 'thorns_20', 'frost_aura_6'],
  ['auto_repair_2.0', 'dmg_reduction_10', 'thorns_20', 'frost_aura_6', 'frost_pulse'],
];
// 城门位置（仅用于 Lv5/Lv6 半径计算的参考；服务器不追踪怪物位置，冲击波按 _activeMonsters 全量 AOE 处理）
const GATE_POS = { x: 0, z: -4 };
// Lv6 寒冰冲击波参数
const FROST_PULSE_INTERVAL_MS = 15000;
const FROST_PULSE_RADIUS      = 8;
const FROST_PULSE_DAMAGE      = 100;
const FROST_PULSE_FREEZE_MS   = 2000;

// ==================== §17.15 新手引导气泡（🆕 v1.27 待实现）====================
// ONBOARDING_THROTTLE_MS：
//   观众进房触发 B1–B3 气泡后的节流窗口，避免主播屏被新观众流 spam。
//   策划案 §17.15 "触发规则"：`now - _lastOnboardingAt ≥ 300_000ms`（5 分钟）再触发下一轮。
// ONBOARDING_REPLAY_WINDOW_MS：
//   客户端 sync_state（断连重连/主动同步）时的补发窗口。落在 [_lastOnboardingAt, _lastOnboardingAt + 30_000]
//   内沿用同一 sessionId 重发 show_onboarding_sequence，客户端靠 sessionId 幂等；超窗口不重发。
//   默认 30 s（覆盖移动端弱网 4G/2G 重连延迟），可通过此常量调优。
const ONBOARDING_THROTTLE_MS      = 300_000;
const ONBOARDING_REPLAY_WINDOW_MS = 30_000;

// ==================== §31 怪物多样性系统（Monster Variants） ====================
// 第一/二/三阶段 variant：normal / rush / assassin / ice / summoner / guard / mini
// 首次出现天数（Normal 难度基准；Easy +2 / Hard -1；Hard rushCount +1）
const MONSTER_VARIANT_RUSH_DAY     = 3;
const MONSTER_VARIANT_ASSASSIN_DAY = 4;
const MONSTER_VARIANT_ICE_DAY      = 5;
const MONSTER_VARIANT_SUMMONER_DAY = 7;
const MONSTER_VARIANT_GUARD_DAY    = 3;
// 数值调整
const VARIANT_HP_MULT = {
  rush:     0.5,
  assassin: 1.0,
  ice:      1.2,
  summoner: 0.8,
  guard:    1.0,   // guard HP 单独按 bossHp × 0.12 计算，此处不用
  mini:     1.0,
  normal:   1.0,
};
// §31 变种速度倍率（相对 cfg.normal.spd）
const VARIANT_SPEED_MULT = {
  rush:     1.6,   // §31.2 冲锋
  assassin: 1.0,
  ice:      0.85,  // §31.3 略慢
  summoner: 0.9,   // §31.4 略慢
  guard:    1.0,
  mini:     1.0,
  normal:   1.0,
};
// 冰封冻结
const ICE_FREEZE_CHANCE = 0.30;
const ICE_FREEZE_MS     = 5000;
// 召唤怪死亡生成 mini 数量/HP/ATK
const SUMMONER_MINI_COUNT = 2;
const MINI_HP             = 30;
const MINI_ATK            = 1;
// 首领卫兵
const GUARD_HP_RATIO      = 0.12;   // Boss HP × 0.12
const GUARD_ATK_MULT      = 2;      // normal ATK × 2
const GUARD_COUNT         = 2;
const BOSS_ENRAGED_ATK_MULT = 1.3;
// 同屏上限（§8.7 mini 也计入；客户端有同样的 15 上限）
const MAX_ALIVE_MONSTERS = 15;

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

// 随机事件名称映射（策划案 §8 + §34.3 B3 扩充 5→15）
const EVENT_NAMES = {
  // 原有 5 种
  E01_snowstorm:    '暴风雪',
  E02_harvest:      '丰收季节',
  E03_monster_wave: '怪物来袭',
  E04_warm_spring:  '暖流涌现',
  E05_ore_vein:     '矿脉发现',
  // §34.3 B3 新增 10 种（事件 ID 与策划案 5080-5094 对齐）
  airdrop_supply:   '空投补给',
  ice_ground:       '地面冰封',
  aurora_flash:     '极光闪现',
  earthquake:       '地震',
  meteor_shower:    '流星雨',
  heavy_fog:        '浓雾',
  hot_spring:       '温泉涌出',
  food_spoil:       '食物变质',
  inspiration:      '灵感爆发',
  morale_boost:     '矿工士气',
};

// §34.3 B3 随机事件加权池 —— 原 5 种保持权重（为了与旧行为等价，原实现等权 → 各 20）
//   新增 10 种按策划案：B3 总体加权保留原 5 种，新增 10 种每种 5~10 权重
const RANDOM_EVENT_POOL = [
  // 原 5 种（维持原等权行为）
  { id: 'E01_snowstorm',     weight: 20 },
  { id: 'E02_harvest',       weight: 20 },
  { id: 'E03_monster_wave',  weight: 20 },
  { id: 'E04_warm_spring',   weight: 20 },
  { id: 'E05_ore_vein',      weight: 20 },
  // 新增 10 种（权重 5~10）
  { id: 'airdrop_supply',    weight: 10 },
  { id: 'ice_ground',        weight:  7 },
  { id: 'aurora_flash',      weight:  8 },
  { id: 'earthquake',        weight:  6 },
  { id: 'meteor_shower',     weight:  5 },
  { id: 'heavy_fog',         weight:  6 },
  { id: 'hot_spring',        weight:  8 },
  { id: 'food_spoil',        weight:  6 },
  { id: 'inspiration',       weight: 10 },
  { id: 'morale_boost',      weight: 10 },
];

// §34.3 B3：事件触发间隔 90-120s → 60-90s
const RANDOM_EVENT_INTERVAL_MIN_SEC = 60;
const RANDOM_EVENT_INTERVAL_MAX_SEC = 90;

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

// ==================== §34.4 Layer 3 组 D（E2 五幕 / E5a 提词 / E5b 夜报 / E6 修饰符 / E8 唤回 / E9 难度）====================
// 策划案 §34.4 第 5205–5482 行，与组 C（E1/E3/E4）并存：
//   - E2  seasonDay(1..7) → actTag('prologue'/'act1'/'act2'/'act3'/'finale') 映射，幕末事件（mini BossRush 等）
//   - E5a 每 10s 生成主播专用提示（TODO：_roomCreatorId 未注入 → 现版本广播；客户端按 isRoomCreator 过滤）
//   - E5b 夜→昼时基于 _nightStats 推送战报
//   - E6  _enterNight 加权随机 NIGHT_MODIFIERS 选一个（含 §31 交互规则：blood_moon 覆盖、polar_night × 1.5 频率）
//   - E8  每 300s 对 contributions > 0 玩家推送排名 + gapToTop3（TODO：单播能力未接入 → 广播，客户端自筛）
//   - E9  change_difficulty C→S（applyAt='next_night' 或 'next_season'），生效时机由 _enterNight / SeasonManager 轮询消费

// E2 赛季幕定义
//   seasonDay ∈ [1..7] → actTag + name + endNote；D7 终章复用 §36 BossRush（不重复实现）
const ACT_DEFINITIONS = [
  { actTag: 'prologue', name: '序章·踏入极地',   startDay: 1, endDay: 1, endNote: '教学+安全' },
  { actTag: 'act1',     name: '第一幕·资源争夺', startDay: 2, endDay: 3, endNote: '压力上升' },
  { actTag: 'act2',     name: '第二幕·暗夜降临', startDay: 4, endDay: 5, endNote: '持续高压' },
  { actTag: 'act3',     name: '第三幕·最后防线', startDay: 6, endDay: 6, endNote: '极限挑战' },
  { actTag: 'finale',   name: '终章·黎明之前',   startDay: 7, endDay: 7, endNote: 'BossRush 终章（§36）' },
];

// E5a 提词器：10s 调用一次
const STREAMER_PROMPT_INTERVAL_MS = 10 * 1000;

// E6 夜间修饰符池
//   normal 占 40% 权重作为保底，其余 6 种按 minDay 解锁
//   策划案 §34.4 第 5406-5414 行；权重 = 40+15+12+10+12+8+3 = 100
const NIGHT_MODIFIERS = [
  { id: 'normal',         name: '普通夜晚',   desc: '无特殊规则',                                   weight: 40, minDay: 1  },
  { id: 'blood_moon',     name: '血月',       desc: '单 Boss HP ×3，击杀贡献 ×1.5',                 weight: 15, minDay: 3  },
  { id: 'polar_night',    name: '极夜',       desc: '夜晚持续 180s，§31 变体频率 ×1.5 + 属性 +20%',  weight: 12, minDay: 5  },
  { id: 'fortified',      name: '坚守之夜',   desc: '全矿工 HP +30，无 Boss',                        weight: 10, minDay: 4  },
  { id: 'frenzy',         name: '狂潮之夜',   desc: '波次间隔 ×0.6，每波数量 ×0.5',                  weight: 12, minDay: 6  },
  { id: 'hunters',        name: '猎手之夜',   desc: '玩家对 Boss 伤害 ×2',                           weight:  8, minDay: 8  },
  { id: 'blizzard_night', name: '暴风雪夜',   desc: '全矿工 -2HP/10s 冻伤，T4 能量电池治愈',         weight:  3, minDay: 10 },
];
// 极夜夜长（秒），默认 nightDuration 为 120
const MODIFIER_POLAR_NIGHT_DURATION_SEC = 180;
// blizzard_night 每 10s 冻伤
const MODIFIER_BLIZZARD_TICK_SEC = 10;
const MODIFIER_BLIZZARD_DAMAGE   = 2;
// blizzard_night T4 能量电池治愈量（全矿工 +20HP）
const MODIFIER_BLIZZARD_T4_HEAL  = 20;
// frenzy 倍率
const MODIFIER_FRENZY_INTERVAL_MULT = 0.6;
const MODIFIER_FRENZY_COUNT_MULT    = 0.5;
// fortified 矿工 HP 加成
const MODIFIER_FORTIFIED_HP_BONUS   = 30;
// blood_moon Boss HP × 3，护卫（elite）数量 2
const MODIFIER_BLOOD_MOON_BOSS_HP_MULT = 3;
const MODIFIER_BLOOD_MOON_ELITE_COUNT   = 2;
const MODIFIER_BLOOD_MOON_CONTRIB_MULT  = 1.5;
// polar_night §31 变体频率 / 属性倍率
const MODIFIER_POLAR_VARIANT_FREQ_MULT = 1.5;
const MODIFIER_POLAR_VARIANT_STAT_MULT = 1.2;
// hunters Boss 伤害倍率
const MODIFIER_HUNTERS_BOSS_DMG_MULT   = 2;

// E8 参与感唤回：每 300s 对 contributions > 0 的玩家推送排名
const ENGAGEMENT_REMINDER_INTERVAL_MS = 300 * 1000;

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
    // 城门每日升级标志（策划案 §10 v2：broadcaster/expedition_trader 每天仅能主动升级一次；T6 礼物联动不受限）
    this._gateUpgradedToday = false;
    // Lv6 寒冰冲击波定时器（setInterval 句柄，_startFrostPulseTimer 启动，_stopFrostPulseTimer 清理）
    this._frostPulseTimer = null;

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
    //   settlement (~30s) → recovery (120s，不生成新波次) → night(day+1)，无胜利终点（Fix A：8s→30s）
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

    // 夜晚活跃怪物 Map<monsterId, { id, type, variant, maxHp, currentHp, atk }>
    // type: 'normal' | 'elite' | 'boss' | 'elite_raid'
    // variant (§31): 'normal' | 'rush' | 'assassin' | 'ice' | 'summoner' | 'guard' | 'mini'
    this._activeMonsters = new Map();
    this._monsterIdCounter = 0;

    // ── §31 怪物多样性系统 ────────────────────────────────────────────────
    // _frozenWorkers: 被冰封怪冻结的矿工 Map<playerId, unfreezeAt(ms)>；与全局 simulate_freeze 独立
    // _guardsAlive: 当晚仍存活的首领卫兵数量（2→1→0 触发 Boss 暴走）
    // _lastBossSpawnSide: Boss 刷新侧（用于卫兵同侧生成；_enterNight 时随机选一侧）
    this._frozenWorkers      = new Map();
    this._guardsAlive        = 0;
    this._lastBossSpawnSide  = 'all';

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
    // Fix F (组 B Reviewer P1)：改用 RANDOM_EVENT_INTERVAL_MIN/MAX_SEC 常量（60-90s）
    this._nextEventTimer     = RANDOM_EVENT_INTERVAL_MIN_SEC + Math.random() * (RANDOM_EVENT_INTERVAL_MAX_SEC - RANDOM_EVENT_INTERVAL_MIN_SEC);
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
    // veteranTracker 由 setRoomContext 注入；§36.12 老用户豁免追踪
    this.globalClock     = null;
    this.seasonMgr       = null;
    this.roomPersistence = null;
    this.veteranTracker  = null;
    this._clockDriven    = false;  // true → 内部 _tick 跳过 phase 到期切换

    // §36.5 和平夜控制（由 _enterNight 设置，tick / _spawnWave 读取）
    this._peaceNightSkipSpawn = false;   // 整夜和平（Day1/Day2）→ 跳过所有刷怪
    this._peaceNightDelayUntil = 0;      // Day3 前 30s 和平（Unix ms）→ tick 检查该字段

    // §36.4 BossRush：仅 D7 夜晚累计 monster_died 伤害到全服池（由 _handleAttack / 其他 monster_died 路径调用）
    // 判定键：seasonMgr.seasonDay===7 && this.state==='night'

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

    // §17.15 新手引导气泡（内存变量，不持久化；服务端重启全房间重置为 0）
    this._lastOnboardingAt        = 0;     // Unix ms，最近一次触发时间
    this._lastOnboardingSessionId = null;  // 最近一次推送的 sessionId，供 sync_state 30s 补发窗口重发
    this._onboardingDisabled      = false; // 主播"关闭引导"标志（本次房间会话级，reset 时清除）

    // ── §34.4 Layer 3 组 C（沉浸体验：E1 张力 / E3 社交竞争 / E4 精准付费）──
    // 全部字段每局 reset，仅 _totalContribution 用于协作里程碑累计（每局独立）
    //
    // E1 危机感知（_calcTension）：无状态，每次 _broadcastResourceUpdate 现场计算
    // E4 精准付费触发（_calcGiftRecommendation）需 60s 同 giftId 节流防刷屏
    this._lastRecGiftId = null;    // E4 上一次非 gentle 推荐的 giftId
    this._lastRecAt     = 0;       // E4 上一次非 gentle 推荐的 Unix ms
    // E3b 协作里程碑
    //   _totalContribution 累计正向 finalAmount（_trackContribution 末尾累加）；_checkCoopMilestones 按阈值解锁
    //   _milestonesUnlocked 记录已触发阈值 id（Set 防重入）；reset() 清空（本局结束不续跨局）
    //   五档增益字段：
    //     _milestoneEffMult        全员效率 ×1.1（_applyWorkEffect 乘入 totalMult）
    //     _milestoneGateRepairMult 矿石修城门 ×2.0（_decayResources 矿石段）
    //     _milestoneWorkerHpBonus  矿工 HP 基线 +50（_getWorkerMaxHp 返回值 +bonus，同步重算现存 _workerHp）
    //     _milestoneGlobalMult     所有效果 ×1.2（_applyWorkEffect / _handleAttack / love_explosion AOE 乘入）
    //     _freeDeathPass           一次免死豁免（首次矿工将死时消费，广播 free_death_pass_triggered）
    this._totalContribution       = 0;
    this._milestonesUnlocked      = new Set();
    this._milestoneEffMult        = 1.0;
    this._milestoneGateRepairMult = 1.0;
    this._milestoneWorkerHpBonus  = 0;
    this._milestoneGlobalMult     = 1.0;
    this._freeDeathPass           = false;
    // E3c 礼物效果反馈：礼物结算 2s 后广播 gift_impact（setTimeout 句柄，reset 清理避免残留）
    this._giftImpactTimers        = [];

    // ── §34.4 Layer 3 组 D（叙事节奏 / 主播赋能 / 夜间修饰符 / 参与感唤回 / 难度切换）──
    // E2 五幕（赛季映射）：_currentActTag 随 seasonDay 变化时广播 chapter_changed；_lastActTagBroadcast 防重复
    //   同步到 phase_changed.act_tag（客户端 BGM 层切换）
    this._currentActTag       = 'prologue';  // 'prologue'/'act1'/'act2'/'act3'/'finale'
    this._lastActTagBroadcast = null;        // 最近广播 chapter_changed 的 actTag
    // E5a 提词器：setInterval 句柄，每 10s 调用 _generateStreamerPrompt 并推送 streamer_prompt
    this._streamerPromptTimer = null;
    // E5b 夜战报告：_enterNight 初始化，_exitNight 广播后清理（_endNight 路径统一处理）
    //   { monstersKilled, bossDefeated, killsPerPlayer{pid:count}, topGift{tier,playerId,playerName,giftName},
    //     minGateHpPct, totalWorkersAtStart, nightStartedAt }
    this._nightStats          = null;
    // E6 夜间修饰符：_enterNight 加权随机选一个，影响夜晚规则；_exitNight 撤销临时效果
    //   { id, name, desc } 或 null
    this._currentNightModifier = null;
    // Fix 2/5: frenzy 修饰符残留字段（首次初始化为默认值；_applyNightModifier(frenzy) 时设值，_clearNightModifier / reset 归位）
    this._modifierFrenzyIntervalMult    = 1.0;
    this._modifierSavedDynamicCountMult = null;
    // blizzard_night 每 10s 冻伤：_tickCounter 累积基准（以防夜晚内多次切换）
    this._blizzardLastTickAt   = 0;
    // E8 参与感唤回：300s setInterval 句柄
    this._engagementInterval  = null;
    // E9 难度切换 pending：{ difficulty, applyAt: 'next_night' | 'next_season' }；_enterNight / onSeasonStart 时消费
    this._pendingDifficulty   = null;
    // E9 next_season 触发判定：记录 _enterDay 最近一次看到的 seasonId，变化时触发一次 onSeasonStart
    this._lastSeasonIdForDiffCheck = null;

    // ── §34.3 Layer 2 组 B 字段 ─────────────────────────────────────────
    // B2 结算高光 + 跳过
    //   _damageLeaderboard: 本局累计每玩家伤害（_handleAttack / love_explosion AOE 累加）
    //   _bestRescue: { playerId, playerName, giftId, giftName, giftTier, tensionWhenSent, sentAt } | null
    //                仅在 tension>80 时刻发送的礼物被考虑；同 tensionWhenSent>80 前提下取最高 Tier
    //   _mostDramaticEvent: { type, desc, day } | null
    //                本局内最戏剧性事件（tension 暴跌 ≥65 / Boss 绝杀 <5HP / free_death_pass 触发）
    //   _overallClosestCall: { hpPct, day }  本局所有夜晚城门最低 HP 百分比（初值 1.0 不危险）
    //   _settleTimerHandle: _enterSettlement 30s 倒计时句柄（可被 streamer_skip_settlement 跳过；Fix A：8s→30s）
    this._damageLeaderboard = {};
    this._bestRescue        = null;
    this._mostDramaticEvent = null;
    this._overallClosestCall = { hpPct: 1.0, day: 0 };
    this._settleTimerHandle = null;
    this._tensionPrev       = 0;   // _broadcastResourceUpdate 用：算 tension 暴跌幅度

    // B3 新增事件状态字段
    //   _iceGroundEndAt:   ice_ground 事件 endAt（Date.now() ms），_applyWorkEffect 检查
    //                      (读作：玩家采矿速度 ×0.8 → 采矿倍率×0.8；位于 auroraBoost 后乘入)
    //   _heavyFogEndAt:    heavy_fog 隐藏怪物血条 endAt；客户端据 resource_update.hideMonsterHp 标志渲染
    //   _hotSpringEndAt:   hot_spring 温泉 endAt；_tick 每秒分 5 tick 处理 +2°C/5s
    //   _hotSpringLastTick: 上次触发温泉 tickCounter
    //   _oneShotWorkMult:  inspiration 下一次 work_command 产出 ×2（消费后归 1.0）
    this._iceGroundEndAt    = 0;
    this._heavyFogEndAt     = 0;
    this._hotSpringEndAt    = 0;
    this._hotSpringLastTick = 0;
    this._oneShotWorkMult   = 1.0;

    // B4 助威者冷却日志（记录最近 60s 统计，每 60s console.log）
    //   hitCount: 冷却通过成功执行的指令次数
    //   blockedByThrottleCount: 冷却内被拦截次数（throttle hit）
    //   totalAttempts: 总尝试（hit + blocked）
    //   _supporterStatsLogAt: 上次日志输出 tickCounter
    this._supporterStats = { hitCount: 0, blockedByThrottleCount: 0, totalAttempts: 0 };
    this._supporterStatsLogAt = 0;

    // B6 礼物 douyin_id 双路匹配统计
    //   exactHit: 精确 douyin_id 命中
    //   fallbackHit: 兜底 price_fen 命中
    //   missed: 双路都未命中，忽略
    //   _giftMatchStatsLogAt: 上次日志输出 tickCounter
    this._giftMatchStats = { exactHit: 0, fallbackHit: 0, missed: 0 };
    this._giftMatchStatsLogAt = 0;

    // B10 day_preview + efficiency_race
    //   _dayStats: 白天累计贡献 { contributions: {pid:n}, totalDay: n }；_enterDay 初始化，_enterNight 清零
    //   _lastEfficiencyRaceAt: 上次 efficiency_race 广播时刻（ms）—— 15s 节流
    //   _dayPreviewBroadcastedForDay: 已广播 day_preview 的 day 编号（防同白天重复推）
    //   _pendingNightModifier: day_preview 预算的 nightModifier 缓存（_enterNight 消费，避免二次随机）
    this._dayStats                  = null;
    this._lastEfficiencyRaceAt      = 0;
    this._dayPreviewBroadcastedForDay = -1;
    this._pendingNightModifier      = null;

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
   * @param {VeteranTracker}   [extras.veteranTracker]  §36.12 老用户豁免追踪（全局单例）
   */
  setRoomContext(room, tribeWarMgr, extras = {}) {
    this.room        = room || null;
    this.tribeWarMgr = tribeWarMgr || null;

    // §36 注入：GlobalClock / SeasonManager / RoomPersistence / VeteranTracker
    if (extras.globalClock)     this.globalClock     = extras.globalClock;
    if (extras.seasonMgr)       this.seasonMgr       = extras.seasonMgr;
    if (extras.roomPersistence) this.roomPersistence = extras.roomPersistence;
    if (extras.veteranTracker)  this.veteranTracker  = extras.veteranTracker;

    this._clockDriven = !!this.globalClock;

    // 持久化 load：恢复 fortressDay / _lifetimeContrib / _contribBalance / §36.5.1 daily cap / §36.12 veteran
    if (this.roomPersistence && room && room.roomId) {
      const snap = this.roomPersistence.load(room.roomId);
      if (snap) {
        this._applyPersistedSnapshot(snap);
      }
    }
  }

  /** §36.12：老用户豁免查询。未注入 tracker 时返 false（默认非老用户）*/
  _isRoomCreatorVeteran() {
    if (!this.veteranTracker) return false;
    // 用"房间创建者 openId"作为查询键；MVP 以 roomId 的第一位客户端 secOpenId 作为创建者
    //   PM 决策：roomCreatorOpenId 未全流程注入时兜底返 false，由上游 FEATURE_UNLOCK_DAY 拦截
    const creatorId = (this.room && this.room.roomCreatorOpenId) || null;
    if (!creatorId) return false;
    return this.veteranTracker.isVeteran(creatorId);
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

    // §36.12 老用户豁免：合并到全局 VeteranTracker（即使 tracker 是空单例，也可从 per-room 快照恢复）
    if (this.veteranTracker && typeof this.veteranTracker.loadSnapshot === 'function') {
      const payload = {
        _veteranStreamers:     Array.isArray(snap._veteranStreamers)      ? snap._veteranStreamers : [],
        _maxSeasonDayAttended: (snap._maxSeasonDayAttended && typeof snap._maxSeasonDayAttended === 'object') ? snap._maxSeasonDayAttended : {},
      };
      try { this.veteranTracker.loadSnapshot(payload); } catch (e) { /* ignore */ }
    }

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
    this._gateUpgradedToday = false;
    this._stopFrostPulseTimer();
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

    // §31 怪物多样性系统：清冻结矿工 Map / 卫兵计数 / Boss 刷新侧
    this._frozenWorkers.clear();
    this._guardsAlive        = 0;
    this._lastBossSpawnSide  = 'all';

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
    // Fix F (组 B Reviewer P1)：改用 RANDOM_EVENT_INTERVAL_MIN/MAX_SEC 常量（60-90s）
    this._nextEventTimer     = RANDOM_EVENT_INTERVAL_MIN_SEC + Math.random() * (RANDOM_EVENT_INTERVAL_MAX_SEC - RANDOM_EVENT_INTERVAL_MIN_SEC);
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

    // §17.15 新手引导：节流/disabled/sessionId 不跨局保留，新一局允许重新触发
    this._lastOnboardingAt        = 0;
    this._lastOnboardingSessionId = null;
    this._onboardingDisabled      = false;

    // §34.4 Layer 3 组 C 清理（E1/E3/E4 全部每局重置，不跨局）
    this._lastRecGiftId           = null;
    this._lastRecAt               = 0;
    this._totalContribution       = 0;
    this._milestonesUnlocked.clear();
    this._milestoneEffMult        = 1.0;
    this._milestoneGateRepairMult = 1.0;
    this._milestoneWorkerHpBonus  = 0;
    this._milestoneGlobalMult     = 1.0;
    this._freeDeathPass           = false;
    for (const t of (this._giftImpactTimers || [])) clearTimeout(t);
    this._giftImpactTimers        = [];

    // §34.4 Layer 3 组 D 清理：E2/E5a/E5b/E6/E8/E9 全部每局重置
    this._currentActTag        = 'prologue';
    this._lastActTagBroadcast  = null;
    this._currentNightModifier = null;
    this._nightStats           = null;
    this._blizzardLastTickAt   = 0;
    this._pendingDifficulty    = null;
    this._lastSeasonIdForDiffCheck = null;
    // Fix 2: 清 frenzy 修饰符残留（若上局夜晚中途失败 → _clearNightModifier 未调 → 跨局残留）
    this._modifierFrenzyIntervalMult    = 1.0;
    this._modifierSavedDynamicCountMult = null;
    // 清除 E5a 提词器定时器（reset 期间不应继续推送）
    if (this._streamerPromptTimer) { clearInterval(this._streamerPromptTimer); this._streamerPromptTimer = null; }
    // 清除 E8 参与感唤回定时器
    if (this._engagementInterval)  { clearInterval(this._engagementInterval);  this._engagementInterval  = null; }

    // §34.3 Layer 2 组 B 清理：B2/B3/B4/B6/B10 全部每局重置
    // B2 结算高光 + 跳过
    this._damageLeaderboard = {};
    this._bestRescue        = null;
    this._mostDramaticEvent = null;
    this._overallClosestCall = { hpPct: 1.0, day: 0 };
    if (this._settleTimerHandle) { clearTimeout(this._settleTimerHandle); this._settleTimerHandle = null; }
    this._tensionPrev = 0;
    // B3 新增事件状态
    this._iceGroundEndAt    = 0;
    this._heavyFogEndAt     = 0;
    this._hotSpringEndAt    = 0;
    this._hotSpringLastTick = 0;
    this._oneShotWorkMult   = 1.0;
    // B4 助威者冷却日志
    this._supporterStats      = { hitCount: 0, blockedByThrottleCount: 0, totalAttempts: 0 };
    this._supporterStatsLogAt = 0;
    // B6 礼物 douyin_id 双路匹配统计
    this._giftMatchStats      = { exactHit: 0, fallbackHit: 0, missed: 0 };
    this._giftMatchStatsLogAt = 0;
    // B10 day_preview + efficiency_race
    this._dayStats                    = null;
    this._lastEfficiencyRaceAt        = 0;
    this._dayPreviewBroadcastedForDay = -1;
    this._pendingNightModifier        = null;

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
      gateTierName:      GATE_TIER_NAMES[this.gateLevel - 1] || '',
      gateFeatures:      GATE_FEATURES_BY_LV[this.gateLevel - 1] || [],
      gateDailyUpgraded: !!this._gateUpgradedToday,
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
      // §36.12 分时段功能解锁：已解锁功能全集（断线重连/初始化用；与 season_state.unlockedFeatures 对齐）
      unlockedFeatures:       this._getUnlockedFeaturesForClient(),
    };
  }

  /** §36.12 查询当前已解锁功能列表（含老用户豁免 → 返回全集）*/
  _getUnlockedFeaturesForClient() {
    try {
      const { getUnlockedFeatures, FEATURE_IDS_IN_DAY_ORDER } = require('./config/FeatureUnlockConfig');
      // 老用户 → 返回全集（豁免）
      if (this._isRoomCreatorVeteran()) return [...FEATURE_IDS_IN_DAY_ORDER];
      const seasonDay = this.seasonMgr ? this.seasonMgr.seasonDay : 1;
      return getUnlockedFeatures(seasonDay);
    } catch (e) {
      return [];
    }
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
      // §36.12 shop 功能锁：未解锁时静默忽略弹幕（不推失败，避免骚扰观众 —— 同"未识别弹幕"处理）
      {
        const { isFeatureUnlocked: _shopLock } = require('./config/FeatureUnlockConfig');
        if (!_shopLock(this.room, 'shop')) return;
      }
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
      // §36.12 shop 功能锁：装备切换同样走 shop 解锁门（D<2 时观众无库存，静默忽略避免主动错误骚扰）
      {
        const { isFeatureUnlocked: _equipLock } = require('./config/FeatureUnlockConfig');
        if (!_equipLock(this.room, 'shop')) return;
      }
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

    // §31.3 冻结矿工：cmd 1-4（工作）/ cmd 6（攻击）期间被冻结则静默丢弃
    //   保留 666 / 探 / 召回 的弹幕响应（这些不依赖矿工动作）
    if (playerId && this._frozenWorkers.has(playerId) && (cmd >= 1 && cmd <= 4 || cmd === 6)) {
      console.log(`[SurvivalEngine] Command ${cmd} from ${playerName || playerId} dropped (frozen)`);
      return;
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

    // §34.3 B6 双路匹配：优先 douyin_id 精确命中 → price_fen 兜底 → 忽略
    //   命中方式分别计入 _giftMatchStats，供 60s 日志监控
    //   douyin_id 仍为 TBD（运营待定），兜底 price_fen 是当前主路径
    let gift = findGiftById(giftId);
    let _matchPath = 'exact';
    if (!gift) {
      // 内部 ID 匹配（模拟/测试用）归为 exact（等价精确命中）
      gift = getGift(giftId);
    }
    if (!gift) {
      gift = findGiftByPrice(giftValue);
      if (gift) {
        _matchPath = 'fallback';
        console.warn(`[Gift] Fallback match: douyin_id=${giftId || '(none)'} price_fen=${giftValue} matched=${gift.id}`);
      }
    }

    // 完全未知的礼物：忽略，不产生任何游戏效果也不计入贡献/积分池
    if (!gift) {
      this._giftMatchStats.missed++;
      console.warn(`[Gift] Unknown gift: douyin_id=${giftId || '(none)'} price_fen=${giftValue}`);
      console.log(`[SurvivalEngine] unknown gift ignored: ${giftName || giftId} (id=${giftId}, val=${giftValue})`);
      return;
    }
    if (_matchPath === 'exact')    this._giftMatchStats.exactHit++;
    else if (_matchPath === 'fallback') this._giftMatchStats.fallbackHit++;

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

    // §34.4 E3a 荣耀时刻（T3+）：先拍快照送礼前的排行，用于对比 isNewFirst / overtaken
    //   T1/T2 不触发；T3+ 在 survival_gift 广播前派发 glory_moment
    const giftTierNum = getTierNumber(gift.tier);
    const rankingBefore = (giftTierNum >= 3)
      ? Object.entries(this.contributions).sort(([, a], [, b]) => b - a).slice()
      : null;

    // §34.4 E5b：夜晚 topGift 追踪（取本夜 tier 最高的发送者）
    this._trackNightGift(playerId, playerName, gift.name_cn, giftTierNum);

    // §34.3 B2 bestRescue：tension > 80 时刻发送的礼物视为"救场礼物"
    //   规则：同危机态（tension>80）下，优先取更高 Tier；Tier 相同取更晚（last write wins）
    try {
      const _tensionNow = this._calcTension();
      if (_tensionNow > 80) {
        const prev = this._bestRescue;
        if (!prev || giftTierNum >= prev.giftTier) {
          this._bestRescue = {
            playerId,
            playerName: playerName || playerId || '(未知)',
            giftId:     gift.id,
            giftName:   gift.name_cn,
            giftTier:   giftTierNum,
            tensionWhenSent: _tensionNow,
            sentAt:     Date.now(),
          };
        }
      }
    } catch (e) { /* ignore */ }

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
        // §31.3 T4 联动：额外解冻当前所有被冰封怪冻结的矿工（不改变礼物文案）
        if (this._frozenWorkers.size > 0) {
          const unfrozen = [];
          for (const [pid] of this._frozenWorkers) {
            unfrozen.push(pid);
            this._broadcast({ type: 'worker_unfrozen', data: { playerId: pid } });
          }
          this._frozenWorkers.clear();
          effects.unfrozenWorkers = unfrozen.length;
          console.log(`[SurvivalEngine] energy_battery unfroze ${unfrozen.length} workers (ids=${unfrozen.join(',')})`);
        }
        // §34.4 E6 blizzard_night 治愈：夜晚若 modifier=blizzard_night → 全矿工额外 +20HP
        this._applyBlizzardT4Heal();
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
        // T6 礼物联动：城门自动升级（不扣矿石，不计每日额度），并补送者 +100 贡献
        if (this.gateLevel < GATE_MAX_HP_BY_LEVEL.length) {
          this._upgradeGate(playerId, 'gift_t6');
          this.contributions[playerId] = (this.contributions[playerId] || 0) + 100;
        }
        needsResourceSync = true;
        break;
      }

      case 'love_explosion': {
        // 爱的爆炸：全体怪物AOE伤害200、所有矿工HP全满恢复、城门+200HP
        // §34.4 E3b legend 里程碑（10000）：AOE 伤害 ×_milestoneGlobalMult（+20%）
        const aoeDmg = Math.round(200 * (this._milestoneGlobalMult || 1.0));
        const killed = [];
        for (const [mid, m] of this._activeMonsters) {
          m.currentHp -= aoeDmg;
          // §34.3 B2：AOE 伤害每击累加到发起者（按怪物剩余 HP 截断上限，避免"伤害"大于实际 HP）
          if (playerId) {
            const dealt = Math.min(aoeDmg, Math.max(0, aoeDmg + m.currentHp)); // pre-damage HP 与 aoeDmg 取 min
            this._damageLeaderboard[playerId] = (this._damageLeaderboard[playerId] || 0) + dealt;
          }
          if (m.currentHp <= 0) killed.push(mid);
        }
        for (const mid of killed) {
          const m = this._activeMonsters.get(mid);
          this._activeMonsters.delete(mid);
          // §34.4 E5b：累计夜晚击杀（T5 AOE 计入发起者）
          this._trackNightKill(playerId, m ? m.type : 'normal');
          this._broadcast({ type: 'monster_died', data: { monsterId: mid, monsterType: m.type, killerId: playerId } });
          // §31 guard/summoner 死亡后置钩子
          this._postMonsterDeathHooks(mid, m.type, m.variant);
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

    // §34.4 E3a 荣耀时刻（T3+ 送礼后）：对比送礼前快照 → 广播 glory_moment
    if (giftTierNum >= 3 && rankingBefore) {
      this._broadcastGloryMoment(playerId, playerName, gift.name_cn, giftTierNum, rankingBefore);
    }

    // §34.4 E3c 礼物效果反馈：礼物结算 2s 后广播 gift_impact
    //   fairy_wand privateOnly=true（仅 sender 显示），其他 6 种礼物 privateOnly=false
    const impactsText = this._formatGiftImpactText(gift.id, gift.name_cn, playerName, effects);
    if (impactsText) {
      this._scheduleGiftImpact(
        playerId,
        playerName,
        gift.id,
        gift.name_cn,
        impactsText,
        gift.id === 'fairy_wand'
      );
    }

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

      // §36.12 老用户豁免：玩家首次加入时评估一次（可能满足条件 1/3）
      //   注：此处 playerId 是加入者，不一定是创建者；只有与 roomCreatorOpenId 匹配时才会触发豁免
      //   evaluateVeteran 内部会过滤 non-creator（通过 _evaluateVeteranForCreator 统一入口）
      this._evaluateVeteranForCreator('player_joined');
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

    // §36.12 supporter_mode 门槛：seasonDay < 6 且非老用户 → 静默保留旁观者身份
    //   不推送任何 shop_purchase_failed / supporter_joined（策划案"不骚扰未主动请求的观众"）
    if (this.room && typeof this.room === 'object') {
      try {
        const { isFeatureUnlocked } = require('./config/FeatureUnlockConfig');
        if (!isFeatureUnlocked(this.room, 'supporter_mode')) {
          // 静默：既不晋升也不提示；D6 后若该观众再次发言，正常走路径
          return false;
        }
      } catch (e) { /* ignore require error on fallback */ }
    }

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

    // §34.3 B4：记录一次尝试（在冷却判定前，用于 throttle_rate 统计）
    this._supporterStats.totalAttempts++;

    // 同一 cmd 全局冷却 2s（防 200 人直播间同时刷同一指令）
    const now = Date.now();
    if (now - (this._supporterCmdCooldown[cmd] || 0) < 2000) {
      // §34.3 B4：冷却内拦截
      this._supporterStats.blockedByThrottleCount++;
      return;
    }
    this._supporterCmdCooldown[cmd] = now;
    // §34.3 B4：冷却通过（真实执行前 +1，以便 unsupported cmd 也被计入 hit；若要严格只计生效命令，可移动到 return 前）
    this._supporterStats.hitCount++;

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
    // §34.4 E3c 助威者 fairy_wand 也广播 gift_impact（privateOnly，仅 sender 看；文案复用守护者模板）
    const impactsText = this._formatGiftImpactText(gift.id, gift.name_cn, playerName, effects);
    if (impactsText) {
      this._scheduleGiftImpact(playerId, playerName, gift.id, gift.name_cn, impactsText, /* privateOnly */ true);
    }
    console.log(`[SurvivalEngine] supporter gift: ${playerName} → ${gift.name_cn} redirected to ${effects.redirectTargetName || '(no guardian)'}`);
  }

  /**
   * 助威者送 T5 爱的爆炸：AOE + 城门 HP 与守护者一致；复活逻辑改为"随机一名已死亡矿工"。
   * 贡献值 +2000，积分池 +2000。
   */
  _handleSupporterLoveExplosion(playerId, playerName, avatarUrl, gift, giftValue) {
    const effects = { supporterRedirect: true };
    // §34.4 E3b legend 里程碑（10000）：AOE 伤害 ×_milestoneGlobalMult（+20%），与守护者路径保持一致
    const aoeDmg  = Math.round(200 * (this._milestoneGlobalMult || 1.0));

    // AOE 全体怪物 HP
    const killed = [];
    for (const [mid, m] of this._activeMonsters) {
      m.currentHp -= aoeDmg;
      // §34.3 B2：AOE 伤害计入发起者（助威者）
      if (playerId) {
        const dealt = Math.min(aoeDmg, Math.max(0, aoeDmg + m.currentHp));
        this._damageLeaderboard[playerId] = (this._damageLeaderboard[playerId] || 0) + dealt;
      }
      if (m.currentHp <= 0) killed.push(mid);
    }
    for (const mid of killed) {
      const m = this._activeMonsters.get(mid);
      this._activeMonsters.delete(mid);
      // §34.4 E5b：累计夜晚击杀（助威者 T5 AOE 仍计入 playerId）
      this._trackNightKill(playerId, m ? m.type : 'normal');
      this._broadcast({
        type: 'monster_died',
        data: { monsterId: mid, monsterType: m.type, killerId: playerId },
      });
      // §31 guard/summoner 死亡后置钩子
      this._postMonsterDeathHooks(mid, m.type, m.variant);
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
    // §34.4 E3c 助威者 love_explosion 也广播 gift_impact（全场可见）
    //   助威者无 contributions 条目 → _broadcastGloryMoment 自动 no-op，不单独抑制
    const impactsText = this._formatGiftImpactText(gift.id, gift.name_cn, playerName, effects);
    if (impactsText) {
      this._scheduleGiftImpact(playerId, playerName, gift.id, gift.name_cn, impactsText, /* privateOnly */ false);
    }
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

    // §34.3 B10a：初始化本日贡献统计（仅白天）
    this._dayStats = { contributions: {}, totalDay: 0 };
    // 进入白天 → 重置上次广播的 day_preview 编号，允许新白天再次预告
    //   注意：this.currentDay 还未赋值（下面 this.currentDay = day）→ 用参数 day 对比
    if (this._dayPreviewBroadcastedForDay !== day) {
      this._dayPreviewBroadcastedForDay = -1;
    }
    // 每日节流时钟：reset efficiency_race 最近广播时刻，新白天从 0s 起算
    this._lastEfficiencyRaceAt = 0;

    // §31.3 进入白天：清除所有冻结状态并广播解冻（对齐"天亮复活"语义）
    if (this._frozenWorkers.size > 0) {
      for (const [pid] of this._frozenWorkers) {
        this._broadcast({ type: 'worker_unfrozen', data: { playerId: pid } });
      }
      this._frozenWorkers.clear();
    }

    // 天亮时立即复活所有死亡矿工
    this._reviveAllWorkers('day_started');
    this.currentDay    = day;
    this.remainingTime = this.dayDuration;

    // 每天开始时重置随机事件计时器
    // Fix F (组 B Reviewer P1)：改用 RANDOM_EVENT_INTERVAL_MIN/MAX_SEC 常量（60-90s）
    this._nextEventTimer = RANDOM_EVENT_INTERVAL_MIN_SEC + Math.random() * (RANDOM_EVENT_INTERVAL_MAX_SEC - RANDOM_EVENT_INTERVAL_MIN_SEC);
    // 城门每日升级标志：新一天重置（broadcaster/expedition_trader 路径可再次升级）
    this._gateUpgradedToday = false;

    // §37 建造系统：每日重置投票限额（每日 1 次跨日重置）
    this._buildVoteUsedToday = false;

    // §24.4 首次进入 Running（day=1）→ 轮盘立即就绪（策划案 §24.4.2）
    if (!this._rouletteRunningInited) {
      this._rouletteRunningInited = true;
      this._roulette.onReadyAtRunningStart();
      // §34.4 E5a / E8：首次进入 Running 时启动提词器 + 参与感唤回定时器（幂等）
      this._startStreamerPromptTimer();
      this._startEngagementReminderTimer();
    }

    console.log(`[SurvivalEngine] ===== Day ${day} Start =====`);

    // §34.4 E9 next_season：若 pending.applyAt='next_season' 且 seasonDay 刚归 1 → 调用 onSeasonStart
    //   GlobalClock 驱动下 advanceDay 在 night→day 前触发；此时 seasonDay 若为 1 且 _lastSeasonDayForDiffCheck 不是 1 → 新赛季 D1
    const currSeasonDay = this.seasonMgr ? this.seasonMgr.seasonDay : 1;
    const currSeasonId  = this.seasonMgr ? this.seasonMgr.seasonId  : 1;
    if (this._pendingDifficulty && this._pendingDifficulty.applyAt === 'next_season'
        && currSeasonDay === 1 && this._lastSeasonIdForDiffCheck !== currSeasonId) {
      this.onSeasonStart(currSeasonId);
    }
    this._lastSeasonIdForDiffCheck = currSeasonId;

    // §34.4 E2：检测当前 seasonDay 对应的幕（可能跨幕） → 必要时广播 chapter_changed
    this._checkActTagChange();

    this.broadcast({
      type: 'phase_changed',
      timestamp: Date.now(),
      data: {
        phase: 'day',
        day,
        phaseDuration: this.dayDuration,
        // §34.4 E2：白天也附带 act_tag（客户端 BGM 层切换）；白天无 nightModifier
        act_tag:       this._currentActTag || 'prologue',
        nightModifier: null,
      }
    });

    // 同步完整状态
    this.broadcast({ type: 'survival_game_state', timestamp: Date.now(), data: this.getFullState() });

    this._startTick();
  }

  /**
   * §36.5 和平夜变种查询（seasonDay 驱动，非 fortressDay）
   *   D1 → 'peace_night'（整夜和平 + UI 柔光罩）
   *   D2 → 'peace_night_silent'（整夜无怪但不显示 UI，保持紧张感）
   *   D3 → 'peace_night_prelude'（前 30s 和平 + UI）
   *   D4+ → null（正常夜晚）
   *
   * @param {number} seasonDay
   * @returns {'peace_night' | 'peace_night_silent' | 'peace_night_prelude' | null}
   */
  _getPeaceNightVariant(seasonDay) {
    if (!Number.isFinite(seasonDay)) return null;
    if (seasonDay === 1) return 'peace_night';
    if (seasonDay === 2) return 'peace_night_silent';
    if (seasonDay === 3) return 'peace_night_prelude';
    return null;
  }

  _enterNight(day) {
    this.state         = 'night';
    this.currentDay    = day;

    // §34.3 B10a：清零白天贡献统计（夜晚不再推 efficiency_race）
    this._dayStats = null;

    // §34.4 E9：消费 pending 难度（applyAt='next_night'）→ _applyDifficulty 可能覆写 nightDuration
    //   在 remainingTime 赋值前调用，以便 _applyDifficulty 覆写 nightDuration 时下一行读到新值
    this._consumePendingDifficultyOnNight();

    this.remainingTime = this.nightDuration;

    console.log(`[SurvivalEngine] ===== Night ${day} Start =====`);

    // §36.5 和平夜变种（基于 seasonDay，不是 currentDay）
    const seasonDay = this.seasonMgr ? this.seasonMgr.seasonDay : 1;
    const peaceVariant = this._getPeaceNightVariant(seasonDay);
    this._peaceNightSkipSpawn = (peaceVariant === 'peace_night' || peaceVariant === 'peace_night_silent');
    this._peaceNightDelayUntil = (peaceVariant === 'peace_night_prelude') ? (Date.now() + 30000) : 0;
    if (peaceVariant) {
      console.log(`[SurvivalEngine] Peace night variant: ${peaceVariant} (seasonDay=${seasonDay}, skipSpawn=${this._peaceNightSkipSpawn}, delayUntil=${this._peaceNightDelayUntil})`);
    }

    // §30.6 动态难度：每夜进入前刷新（避免每 tick 都算）
    // §30.3 阶10 传奇免死每晚 1 次 → 重置标记
    this._legendReviveUsed = {};
    this._updateDynamicDifficulty();

    // §31 每晚开始清零卫兵计数 + 清除上一晚的冻结状态（夜切换视为完全解冻，避免跨夜残留）
    this._guardsAlive = 0;
    if (this._frozenWorkers.size > 0) {
      for (const [pid] of this._frozenWorkers) {
        this._broadcast({ type: 'worker_unfrozen', data: { playerId: pid } });
      }
      this._frozenWorkers.clear();
    }

    // 初始化矿工HP（夜晚开始时全员满血上阵）
    this._initWorkerHp();

    // §38.6 夜晚兜底 KIA：必须在 _initWorkerHp 之后执行，否则 died=true 写入会被满血重建覆盖
    this._sweepExpeditionsOnNightStart(Date.now());

    // 初始化当天怪物追踪
    //   §36.5 整夜和平夜（peace_night / peace_night_silent）→ 跳过 _initActiveMonsters
    //   （避免波次调度器读空 map 异常；且任何 monster_wave / boss_appeared 都不广播）
    if (!this._peaceNightSkipSpawn) {
      this._initActiveMonsters(day);
    } else {
      // 保证 _activeMonsters 至少是空 Map（避免后续逻辑读 undefined）
      if (this._activeMonsters && typeof this._activeMonsters.clear === 'function') {
        this._activeMonsters.clear();
      }
    }

    // §34.4 E5b 夜战报告：初始化 _nightStats（跨整夜累积；_endNight 广播后清理）
    //   必须在 _initWorkerHp 之后，以便 totalWorkersAtStart 统计到正确的开夜人数
    this._initNightStats();

    // §34.4 E6 夜间修饰符：加权随机选一个 → 存 _currentNightModifier → 应用效果（服务端权威）
    //   §34.3 B10b：若 _pendingNightModifier 已由 day_preview 预算 → 直接消费，避免二次随机
    //   和平夜（peace_night / peace_night_silent）→ 强制 normal，不应用任何规则改写
    if (this._peaceNightSkipSpawn) {
      this._currentNightModifier = NIGHT_MODIFIERS[0]; // normal（仅用于 phase_changed 附带数据）
      this._pendingNightModifier = null; // 清缓存
    } else if (this._pendingNightModifier) {
      this._currentNightModifier = this._pendingNightModifier;
      this._pendingNightModifier = null; // 消费一次
      this._applyNightModifier(this._currentNightModifier, day);
    } else {
      this._currentNightModifier = this._pickNightModifier();
      this._applyNightModifier(this._currentNightModifier, day);
    }

    // §34.4 E2 幕末事件：应用当日幕末修正（spawn 额外 Boss / mini BossRush / HP × N 等）
    //   仅非和平夜执行；finale（seasonDay=7）复用 §36 BossRush，不重复触发
    if (!this._peaceNightSkipSpawn) {
      this._applyActEndEventIfNeeded(day);
    }

    // §36.5 phase_changed 扩展：variant 字段 + peacePreludeEndsAt（仅 prelude）
    // §34.4 扩展：act_tag（E2）+ nightModifier（E6）
    // polar_night 覆写 remainingTime=180s → phaseDuration 以 remainingTime 为准（客户端倒计时基准）
    const effectivePhaseDuration = (this._currentNightModifier && this._currentNightModifier.id === 'polar_night')
      ? MODIFIER_POLAR_NIGHT_DURATION_SEC
      : this.nightDuration;
    const phaseChangedData = {
      phase: 'night',
      day,
      phaseDuration: effectivePhaseDuration,
      // §34.4 E2 / E6
      act_tag: this._currentActTag || 'prologue',
      nightModifier: this._currentNightModifier ? {
        id:   this._currentNightModifier.id,
        name: this._currentNightModifier.name,
        description: this._currentNightModifier.desc,
      } : null,
    };
    if (peaceVariant) {
      phaseChangedData.variant = peaceVariant;
      if (peaceVariant === 'peace_night_prelude') {
        phaseChangedData.peacePreludeEndsAt = this._peaceNightDelayUntil;
      }
    }
    this.broadcast({
      type: 'phase_changed',
      timestamp: Date.now(),
      data: phaseChangedData,
    });

    // §34.4 E2：夜晚开始检测幕变化（相邻 seasonDay 可能跨幕）
    this._checkActTagChange();

    this.broadcast({ type: 'survival_game_state', timestamp: Date.now(), data: this.getFullState() });

    // §16 v1.27 tick 幂等重启：从 recovery 过来时 tick 仍是轻量分支（recovery），需切换到完整分支；
    //   从 _enterDay 过来时 tick 已在跑（_startTick 内部 _clearTick 保证幂等，无副作用）。
    //   这一行保障 _decayResources / _checkDefeat / remainingTime / 666buff / ore 修门 / 助威衰减
    //   全部在夜晚正常运转（修复 Reviewer Round 1 Critical：tick 断链）。
    this._startTick();

    // 播报 Boss 出现
    //   §36.5 整夜和平夜 → 跳过 boss_appeared 与 guard 生成（该夜无战斗）
    const cfg = getWaveConfig(day);
    if (cfg.boss && !this._peaceNightSkipSpawn) {
      // §31 Boss 刷新侧随机选（供首领卫兵同侧刷新参考；'all' 亦允许）
      const sides = ['left', 'right', 'top', 'all'];
      this._lastBossSpawnSide = sides[Math.floor(Math.random() * sides.length)];
      this.broadcast({
        type: 'boss_appeared',
        timestamp: Date.now(),
        data: {
          day,
          bossHp:  cfg.boss.hp,
          bossAtk: cfg.boss.atk,
        }
      });
      // §31.4 首领卫兵：Day 3+（Easy +2 / Hard -1），每晚 Boss 出现同时固定生成 2 只
      const diff = this._difficulty || 'normal';
      const guardDayOffset = diff === 'easy' ? 2 : diff === 'hard' ? -1 : 0;
      if (day >= MONSTER_VARIANT_GUARD_DAY + guardDayOffset) {
        this._spawnBossGuards(day);
      }
    }

    // §36.5 整夜和平夜 → 不启动波次调度器（_spawnWave 守卫也会兜底，但跳过 setTimeout 更干净）
    if (!this._peaceNightSkipSpawn) {
      // 3秒延迟后再启动夜晚怪物波次（给客户端过渡动画播放时间）
      // §36.5 prelude：首波延迟到 30s 后（30s 和平窗口），其他夜晚仍是 3s
      const baseDelayMs = (this._peaceNightDelayUntil > 0)
        ? Math.max(3000, this._peaceNightDelayUntil - Date.now() + 50)  // +50ms 安全边界
        : 3000;
      const nightStartDelay = setTimeout(() => {
        if (this.state === 'night') {
          this._scheduleNightWaves(day);
        }
      }, baseDelayMs);
      this._waveTimers.push(nightStartDelay);
    } else {
      console.log(`[SurvivalEngine] Night ${day} peace mode (seasonDay=${seasonDay}) — skipping wave scheduler`);
    }

    // §35 Tribe War：本房间若正在被其他房间攻击 → 通知 TribeWarManager 释放远征怪
    // （延迟 4s 跟随 nightStartDelay，保证夜晚 wave 调度先起；manager 内部 no-op 若未被攻击）
    //   §36.5 整夜和平夜 → 也跳过远征怪释放（设计即：D1/D2 对新手友好，无任何怪物）
    if (this.tribeWarMgr && !this._peaceNightSkipSpawn) {
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
   *   - ~30s 后自动进入恢复期（_enterRecovery），不再 reset + startGame（Fix A：原 8s → 30s 与前端 §34 B2 3 屏对齐）
   */
  _enterSettlement(reason) {
    if (this.state === 'settlement' || this.state === 'recovery') return;

    // §16.4 manual：GM 主动终止不触发降级；其他 reason 均为失败，走降级路径
    const isManual = (reason === 'manual');

    // Fix 3: §34D E5b/E6 —— 夜晚中途失败时补发 night_report + 清 nightModifier
    //   正常 _endNight 路径已调用这两个方法；_enterSettlement 跳过了 _endNight 直入结算,
    //   客户端 NightReportUI / NightModifierUI 若不补推永远拿不到清理/报告 → UI 残留旧数据
    //   放在 _clearAllTimers 之前：避免 broadcast 依赖的 _liveRankingTimer 被先清
    if (this.state === 'night') {
      if (typeof this._broadcastNightReportAndClear === 'function') {
        try { this._broadcastNightReportAndClear(); } catch (e) { /* ignore */ }
      }
      if (typeof this._clearNightModifier === 'function') {
        try { this._clearNightModifier(); } catch (e) { /* ignore */ }
      }
    }

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

    // Fix A (组 B Reviewer P0)：settlement_highlights 必须 **先** 于 survival_game_ended 广播。
    //   原因：客户端 SurvivalSettlementUI 30s 序列的 帧 A (ShowScreenA) 在收到 survival_game_ended 后立即触发，
    //         若 highlights 未到 → 读 LastSettlementHighlights 为 null → 帧 A 空白。
    //   修复：先构造并广播 settlement_highlights，再广播 survival_game_ended。
    try {
      const highlights = this._buildSettlementHighlights();
      this.broadcast({ type: 'settlement_highlights', timestamp: Date.now(), data: highlights });
      console.log(`[Settlement] highlights broadcast: topDamage=${highlights.topDamageValue}/${highlights.topDamagePlayerName || '-'}, bestRescue=${highlights.bestRescueGiftName || '-'}/${highlights.bestRescuePlayerName || '-'}, closestCall=${(highlights.closestCallHpPct*100).toFixed(1)}%`);
    } catch (e) {
      console.warn(`[Settlement] buildSettlementHighlights error: ${e.message}`);
    }

    this.broadcast({ type: 'survival_game_ended', timestamp: Date.now(), data });
    console.log(`[SurvivalEngine] Settlement: reason=${reason}, day=${this.currentDay}, fortressDay ${fortressDayBefore}→${fortressDayAfter} (newbie=${newbieProtected})`);

    // §36 持久化：结算 → 保存一次快照
    if (this.roomPersistence && this.room) {
      try { this.roomPersistence.save(this.room); } catch (e) { /* ignore */ }
    }

    // Fix A (组 B Reviewer P0)：结算 UI 定时器 8000ms → 30000ms。
    //   原因：前端 SurvivalSettlementUI 协程 10+10+10=30s，服务端 8s 后发 phase_changed{variant:recovery}，
    //         SurvivalGameManager.cs:308-314 在 Settlement 态收到 recovery 时 SetActive(false)，帧 B/C 永看不到。
    //   修复：后端倒计时延长到 30000ms 与前端对齐；handleStreamerSkipSettlement 在 30s 窗口内仍可提前跳过。
    // §34.3 B2：同时赋值 _settleTimerHandle，供 streamer_skip_settlement 跳过
    this._settleTimerHandle = setTimeout(() => {
      this._recoveryTimer = null;
      this._settleTimerHandle = null;
      this._enterRecovery();
    }, 30000);
    // 保留原 _recoveryTimer 句柄以兼容 _clearAllTimers 的清理路径（同一句柄）
    this._recoveryTimer = this._settleTimerHandle;
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
    // §34.4 E2 扩展：附带 act_tag；E6 扩展：白天/恢复期 nightModifier=null
    this.broadcast({
      type: 'phase_changed',
      timestamp: Date.now(),
      data: {
        phase:         'day',
        day:           this.currentDay,
        phaseDuration: 120,
        variant:       'recovery',
        act_tag:       this._currentActTag || 'prologue',
        nightModifier: null,
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

      // §31.3 冰封冻结到期扫描：5s 精度对玩家感知足够
      this._checkFrozenWorkers(Date.now());

      // §34.4 E6 blizzard_night：每 10s 全矿工 -2HP（仅夜晚）
      this._tickBlizzardNightIfActive();

      // 随机事件触发检查（§34.3 B3：白天/夜晚均可）
      this._checkRandomEvents();

      // §34.3 B3 hot_spring 温泉 tick：每 5s +2°C
      this._tickHotSpringIfActive();

      // §34.3 B4 supporter / B6 gift match 日志：每 60s 输出一次
      this._logSupporterStatsIfDue();
      this._logGiftMatchStatsIfDue();

      // §34.3 B10a efficiency_race：白天 tension<30 时每 15s 广播 Top3
      this._broadcastEfficiencyRaceIfDue();

      // §34.3 B10b day_preview：白天最后 10s 广播下一夜预告
      this._broadcastDayPreviewIfDue();

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

    // §36.12 老用户豁免评估：挺过一夜 → maxFortressDay 可能达到 50 → 触发豁免条件 2
    //   markSeasonAttendance 与 evaluateVeteran 都需要 creatorOpenId；若未注入则 skip
    this._evaluateVeteranForCreator('fortress_day');
  }

  /**
   * §36.12 对房间创建者（或合适范围内的玩家）进行老用户豁免评估
   * 条件：
   *   1. _lifetimeContrib[openId] ≥ 50000
   *   2. room.maxFortressDay ≥ 50
   *   3. 任一赛季 _maxSeasonDayAttended[openId] ≥ 5（由 markSeasonAttendance 填充）
   * 若首次达标 → VeteranTracker 内部自动广播 veteran_unlocked
   */
  _evaluateVeteranForCreator(hint) {
    if (!this.veteranTracker) return;
    const creatorId = (this.room && this.room.roomCreatorOpenId) || null;
    if (!creatorId) return;
    const lifetime = this._lifetimeContrib[creatorId] || 0;
    const maxFD = this.maxFortressDay || 0;
    // markSeasonAttendance：挺过 seasonDay >= N，标记创建者本赛季 attended N
    if (this.seasonMgr) {
      this.veteranTracker.markSeasonAttendance(creatorId, this.seasonMgr.seasonId, this.seasonMgr.seasonDay);
    }
    this.veteranTracker.evaluateVeteran(creatorId, lifetime, maxFD, this.broadcast);
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

    // §34.4 E5b：夜→昼广播战报（_nightStats 已在 _enterNight 初始化）
    //   必须在 _enterDay 之前，因为 _enterDay 会重置 _gateUpgradedToday 等字段并启动新阶段
    this._broadcastNightReportAndClear();

    // §34.4 E6：撤销夜间修饰符效果（fortified HP 回退 / frenzy 数量倍率还原 / blizzard 计时归零）
    this._clearNightModifier();

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

  /**
   * §31.3 冰封冻结到期扫描：遍历 _frozenWorkers，now >= unfreezeAt 则解冻并广播 worker_unfrozen。
   * 与 energy_battery / _enterNight / _damageWorker 多路径互斥（都会修改 _frozenWorkers）。
   */
  _checkFrozenWorkers(nowMs) {
    if (this._frozenWorkers.size === 0) return;
    const expired = [];
    for (const [pid, unfreezeAt] of this._frozenWorkers) {
      if (nowMs >= unfreezeAt) expired.push(pid);
    }
    for (const pid of expired) {
      this._frozenWorkers.delete(pid);
      this._broadcast({ type: 'worker_unfrozen', data: { playerId: pid } });
      console.log(`[SurvivalEngine] Worker ${pid} unfrozen (expired)`);
    }
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

    // 矿石 → 自动修复城门（每2秒消耗1矿石；基础 5HP，随城门等级缩放）
    // 设计意图：矿石让玩家挖矿有动力——矿越多，城门越能抵御怪物攻击
    // §10 v2：Lv2-4 ×1.5（修 7HP），Lv5-6 ×2.0（修 10HP）
    // §34.4 E3b steel_will 里程碑（2000）：_milestoneGateRepairMult ×2 → 单次修 10/14/20HP
    if (this._tickCounter % 10 === 0 && this.ore > 0 && this.gateHp < this.gateMaxHp) {
      const mult         = GATE_AUTO_REPAIR_MULT[Math.max(0, this.gateLevel - 1)] || 1.0;
      const milestoneMult = this._milestoneGateRepairMult || 1.0;
      const repairUnit   = Math.floor(5 * mult * milestoneMult);
      const need         = Math.max(0, this.gateMaxHp - this.gateHp);
      const actual       = Math.min(repairUnit, need);
      if (actual > 0) {
        this.gateHp = Math.min(this.gateMaxHp, this.gateHp + actual);
        this.ore    = Math.max(0, this.ore - 1);
      }
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
    // §34.3 B3 ice_ground：矿工移动速度 ×0.8，折算为工作产出 ×0.8（30s）
    // Fix G (组 B Reviewer P1)：策划案原文"移动速度 -20%"，项目无移动速度概念，采用采矿产出 ×0.8 等效。
    const iceGroundMult    = (this._iceGroundEndAt && Date.now() < this._iceGroundEndAt) ? 0.8 : 1.0;
    // §39 A1 worker_pep_talk：采矿效率 +15%（与 efficiency666Bonus / ability_pill 按 Math.max 取最大，不叠加）
    const pepTalkBoost     = (Date.now() < this._peptTalkBoostUntil) ? 1.15 : 1.0;
    const efficiencyAdditive = Math.max(pepTalkBoost, eff666Boost);
    // §34.4 E3b 协作里程碑（乘入 totalMult）：
    //   unity（500）_milestoneEffMult = 1.1（全员效率 +10%）
    //   legend（10000）_milestoneGlobalMult = 1.2（所有效果 +20%，×进 eff 与 atk 两路）
    const milestoneEff     = this._milestoneEffMult    || 1.0;
    const milestoneGlobal  = this._milestoneGlobalMult || 1.0;
    // §34.3 B3 inspiration：下一次 work_command ×2（oneShot，消费后归位）
    const oneShotMult      = this._oneShotWorkMult || 1.0;
    const totalMult        = Math.min(3.0, levelMult * playerBonus * globalBoost * broadcasterBoost * efficiencyAdditive * auroraBoost * iceGroundMult * milestoneEff * milestoneGlobal * oneShotMult);

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

    // §34.3 B3 inspiration：消费 oneShot 产出加成（即使 oneShotMult=1.0 也归位，防状态残留）
    if (this._oneShotWorkMult && this._oneShotWorkMult !== 1.0) {
      this._oneShotWorkMult = 1.0;
    }

    // §34.3 B10a：累计白天贡献（供 efficiency_race Top3 + dayTotal）
    if (this.state === 'day' && this._dayStats && playerId) {
      this._dayStats.contributions[playerId] = (this._dayStats.contributions[playerId] || 0) + 1;
      this._dayStats.totalDay++;
    }
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
          variant: 'normal',  // §31
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
          variant: 'normal',  // §31
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
        variant: 'normal',  // §31（Boss 自身非变种，但 guard 死亡后会改 atk）
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
    //   × §34.4 E3b legend 里程碑全局效果 ×1.2（所有效果 +20%）
    const atkMult         = this._getWorkerLevelAtkMult(secOpenId);
    const supporterBuff   = 1 + (this._supporterAtkBuff || 0);
    const milestoneGlobal = this._milestoneGlobalMult || 1.0;
    let damage = Math.max(1, Math.round(10 * atkMult * supporterBuff * milestoneGlobal));

    // §30.3 阶5+ 20% 概率重击（伤害 ×2）
    const attackerTier = this._getWorkerTier(secOpenId);
    if (attackerTier >= 5 && Math.random() < 0.20) {
      damage *= 2;
    }

    // §34.4 E6 hunters：玩家对 Boss 伤害 × 2（仅夜晚且 modifier=hunters）
    if (target.type === 'boss' && this._currentNightModifier && this._currentNightModifier.id === 'hunters') {
      damage *= MODIFIER_HUNTERS_BOSS_DMG_MULT;
    }

    target.currentHp -= damage;

    // §34.3 B2：累计伤害榜（本局；_handleAttack 路径）
    if (secOpenId) {
      this._damageLeaderboard[secOpenId] = (this._damageLeaderboard[secOpenId] || 0) + damage;
    }

    // §34.3 B2：Boss 绝杀（HP<=5 但未倒下那一刀）→ 最戏剧事件候选
    if (target.type === 'boss' && target.currentHp > 0 && target.currentHp < 5) {
      this._recordDramaticEvent('boss_low_hp', `Boss 仅剩 ${target.currentHp} HP！`);
    }

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

      // §34.4 E5b：累计夜晚击杀（仅夜晚 _nightStats 存在时；killerId=玩家 ID）
      this._trackNightKill(secOpenId, target.type);

      // §24.4 elite_raid 击杀奖励 +500（策划案明确值）；其余按常规
      const killScore = target.type === 'normal'     ? 20  :
                        target.type === 'elite'      ? 50  :
                        target.type === 'elite_raid' ? 500 : 200;
      // §34.4 E6 blood_moon：击杀贡献 × 1.5（仅夜晚且 modifier=blood_moon）
      const bloodMoonMult = (this._currentNightModifier && this._currentNightModifier.id === 'blood_moon')
        ? MODIFIER_BLOOD_MOON_CONTRIB_MULT : 1.0;
      this._addScore(secOpenId, nickname, killScore * bloodMoonMult);
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

      console.log(`[SurvivalEngine] Monster killed: ${target.id} (${target.type}/${target.variant}) by ${nickname}`);

      // §31 guard/summoner 死亡后置钩子（必须在 monster_died 广播之后，避免 mini wave 先于 summoner_died 抵达客户端）
      this._postMonsterDeathHooks(target.id, target.type, target.variant);

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

  // ==================== 内部：城门升级（策划案 §10 v2）====================

  /**
   * 升级失败统一出口
   * @param {string} reason - max_level | insufficient_ore | wrong_phase | boss_fight | daily_limit
   * @param {object} [extra] - 附加字段（如 currentLevel/required/available）
   */
  _failGateUpgrade(reason, extra = {}) {
    this._broadcast({
      type: 'gate_upgrade_failed',
      data: Object.assign({ reason }, extra),
    });
  }

  /**
   * 计算 _upgradeGate 成功后新增的特性列表（相对上一等级的差集）
   * 仅用于 gate_upgraded.newFeatures 广播
   */
  _computeNewFeatures(lv) {
    const curr = GATE_FEATURES_BY_LV[lv - 1] || [];
    const prev = GATE_FEATURES_BY_LV[lv - 2] || [];
    return curr.filter(f => !prev.includes(f));
  }

  /**
   * 升级城门（策划案 §10 v2：Lv1→Lv6，带战术属性 + 时机限制 + 每日上限 + T6 联动）
   * @param {string} secOpenId - 操作者（主播或 T6 送礼玩家）
   * @param {string} [source='broadcaster'] - broadcaster | expedition_trader | gift_t6
   *
   * 升级顺序：
   *   1. 阶段校验（必须 day/night）
   *   2. 夜晚特殊限制（Boss 出现时 / HP 非濒危 禁止主动升级，T6 不受限）
   *   3. 每日上限（非 T6 每天仅一次）
   *   4. 最高等级校验（Lv6 到顶）
   *   5. 矿石消耗校验（T6 跳过）
   *   6. 执行升级 + HP 奖励 + 标记每日 + 广播 + Lv6 启动冲击波
   */
  _upgradeGate(secOpenId, source = 'broadcaster') {
    // 1. 阶段校验
    if (this.state !== 'day' && this.state !== 'night') {
      this._failGateUpgrade('wrong_phase');
      return;
    }

    // 2. 夜晚特殊限制（T6 礼物可在任意合法阶段升级）
    if (this.state === 'night' && source !== 'gift_t6') {
      // Boss 已出现：主动升级被锁定（用 _activeMonsters 中是否存在 boss 类型判断）
      let bossActive = false;
      for (const m of this._activeMonsters.values()) {
        if (m.type === 'boss') { bossActive = true; break; }
      }
      if (bossActive) {
        this._failGateUpgrade('boss_fight');
        return;
      }
      // 非濒危不允许紧急加固（gateHp/gateMaxHp >= 30% 拒绝）
      if (this.gateMaxHp > 0 && (this.gateHp / this.gateMaxHp) >= 0.3) {
        this._failGateUpgrade('wrong_phase');
        return;
      }
    }

    // 3. 每日上限（T6 不受限；broadcaster 与 expedition_trader 共享额度）
    if (this._gateUpgradedToday && source !== 'gift_t6') {
      this._failGateUpgrade('daily_limit');
      return;
    }

    // 4. 最高等级
    const currentLevel = this.gateLevel;
    if (currentLevel >= GATE_MAX_HP_BY_LEVEL.length) {
      this._failGateUpgrade('max_level', { currentLevel });
      return;
    }

    // 5. 矿石消耗（T6 跳过）
    const cost = GATE_UPGRADE_COSTS[currentLevel - 1];
    if (source !== 'gift_t6' && this.ore < cost) {
      this._failGateUpgrade('insufficient_ore', {
        required:  cost,
        available: Math.round(this.ore),
      });
      return;
    }
    if (source !== 'gift_t6') this.ore -= cost;

    // 6. 执行升级
    this.gateLevel = currentLevel + 1;
    this.gateMaxHp = GATE_MAX_HP_BY_LEVEL[this.gateLevel - 1];

    // HP 奖励：Lv6 直接回满（hpBonus = 缺失值），其他按表加血
    let hpBonus;
    if (this.gateLevel === GATE_MAX_HP_BY_LEVEL.length) {
      hpBonus = Math.max(0, this.gateMaxHp - this.gateHp);
    } else {
      hpBonus = GATE_HP_BONUS_ON_UP[this.gateLevel - 1] || 0;
    }
    this.gateHp = Math.min(this.gateMaxHp, this.gateHp + hpBonus);

    // 每日上限（T6 不计）
    if (source !== 'gift_t6') this._gateUpgradedToday = true;

    // 广播
    this._broadcast({
      type: 'gate_upgraded',
      data: {
        newLevel:     this.gateLevel,
        newMaxHp:     this.gateMaxHp,
        oreRemaining: Math.round(this.ore),
        upgradedBy:   secOpenId || '',
        tierName:     GATE_TIER_NAMES[this.gateLevel - 1],
        newFeatures:  this._computeNewFeatures(this.gateLevel),
        hpBonus,
        source,
      }
    });

    console.log(`[SurvivalEngine] Gate upgraded Lv${currentLevel}→Lv${this.gateLevel} [${GATE_TIER_NAMES[this.gateLevel-1]}] (maxHp=${this.gateMaxHp}, +${hpBonus}HP, source=${source}, ore=${Math.round(this.ore)})`);

    // Lv6 启动寒冰冲击波定时器
    if (this.gateLevel === GATE_MAX_HP_BY_LEVEL.length && !this._frostPulseTimer) {
      this._startFrostPulseTimer();
    }

    this._broadcastResourceUpdate();
  }

  /**
   * Lv6 寒冰冲击波定时器（每 15s 对活跃怪物造成 100 伤害 + 2s 冻结）
   *
   * 注：服务器不追踪怪物位置，FROST_PULSE_RADIUS 语义由客户端呈现（视觉半径），
   *    服务器层对 _activeMonsters 全量应用伤害与 frozenUntil 标记；
   *    冻结标记由客户端读取并表现为怪物停滞动画。
   */
  _startFrostPulseTimer() {
    if (this._frostPulseTimer) return;
    this._frostPulseTimer = setInterval(() => {
      if (this.state !== 'day' && this.state !== 'night') return;
      const hits = [];
      const now = Date.now();
      // 冲击波：伤害+冻结（先收集，再遍历应用死亡广播，避免在迭代中修改 Map）
      const deaths = [];
      for (const [id, m] of this._activeMonsters) {
        m.currentHp -= FROST_PULSE_DAMAGE;
        m.frozenUntil = now + FROST_PULSE_FREEZE_MS;
        hits.push(id);
        if (m.currentHp <= 0) deaths.push({ id, type: m.type, variant: m.variant });
      }
      for (const d of deaths) {
        this._activeMonsters.delete(d.id);
        // §34.4 E5b：gate_frost_pulse 计入 monstersKilled 但不计玩家杀数
        this._trackNightKill('gate_frost_pulse', d.type);
        this._broadcast({ type: 'monster_died', data: { monsterId: d.id, monsterType: d.type, killerId: 'gate_frost_pulse' } });
        // §31 guard/summoner 死亡后置钩子
        this._postMonsterDeathHooks(d.id, d.type, d.variant);
      }
      this._broadcast({
        type: 'gate_effect_triggered',
        data: {
          effect: 'frost_pulse',
          hitMonsters: hits,
          radius: FROST_PULSE_RADIUS,
          damage: FROST_PULSE_DAMAGE,
          freezeMs: FROST_PULSE_FREEZE_MS,
        }
      });
    }, FROST_PULSE_INTERVAL_MS);
  }

  _stopFrostPulseTimer() {
    if (this._frostPulseTimer) {
      clearInterval(this._frostPulseTimer);
      this._frostPulseTimer = null;
    }
  }

  // ==================== 内部：怪物波次（旧调度，兼容保留）====================

  _scheduleNightWaves(day) {
    const cfg = getWaveConfig(day);
    // Fix 5: §34.4 E6 frenzy —— 波次间隔 × _modifierFrenzyIntervalMult（0.6 = -40%）
    //   仅在本夜 _applyNightModifier('frenzy') 后生效；正常夜晚该值 = 1.0
    const intervalMult   = this._modifierFrenzyIntervalMult || 1.0;
    const effectiveBeginning   = Math.max(0, Math.round(cfg.beginning   * intervalMult * 1000));
    const effectiveRefreshMs   = Math.max(100, Math.round(cfg.refreshTime * intervalMult * 1000));
    console.log(`[SurvivalEngine] Night ${day} waves: ${cfg.baseCount}-${cfg.maxCount} per wave, every ${cfg.refreshTime}s (mult=${intervalMult})`);

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
      }, effectiveRefreshMs);

      this._waveTimers.push(repeatTimer);
    }, effectiveBeginning);

    this._waveTimers.push(firstTimer);
  }

  /**
   * §31 怪物变种选择（行为变种 + 元素变种 + 特殊机制怪）
   * 难度偏移（§31.6 末）：Easy 首次出现天数 +2；Hard 首次出现 -1 且 rushCount +1
   * 优先级（策划案 §31.2 伪代码顺序）：
   *   1. assassin  index===0 && day>=4
   *   2. rush      index<rushCount && day>=3
   *   3. ice       index===1 && day>=5（若 rush 占据 index=1，此分支不触发 → 符合"每波最多 1 只"设计）
   *   4. summoner  index===2 && day>=7（若 rush/ice 占据，post-process 迁移到下一 normal 槽）
   */
  _selectVariant(day, index) {
    const diff = this._difficulty || 'normal';
    const dayOffset  = diff === 'easy' ? 2 : diff === 'hard' ? -1 : 0;
    const rushBonus  = diff === 'hard' ? 1 : 0;

    const assassinDay = MONSTER_VARIANT_ASSASSIN_DAY + dayOffset;
    const rushDay     = MONSTER_VARIANT_RUSH_DAY     + dayOffset;
    const iceDay      = MONSTER_VARIANT_ICE_DAY      + dayOffset;
    const summonerDay = MONSTER_VARIANT_SUMMONER_DAY + dayOffset;

    const baseRushCount = day >= 15 ? 3 : day >= 7 ? 2 : 1;
    const rushCount     = baseRushCount + rushBonus;

    if (day >= assassinDay && index === 0) return 'assassin';
    if (day >= rushDay && index < rushCount) return 'rush';
    if (day >= iceDay && index === 1) return 'ice';
    if (day >= summonerDay && index === 2) return 'summoner';
    return 'normal';
  }

  /**
   * §31.2 刺客怪目标选择：返回存活矿工中 HP 最低者的 playerId（无存活则返 null）
   * aliveWorkers: Array<[pid, workerHpObj]>
   */
  _getAssassinTarget(aliveWorkers) {
    let minHp = Infinity;
    let target = null;
    for (const [pid, w] of aliveWorkers) {
      if (!w || w.isDead) continue;
      if (w.hp < minHp) { minHp = w.hp; target = pid; }
    }
    return target;
  }

  /**
   * §31 内部：把一次"对单矿工的伤害份"施加到矿工身上，复用阶6 15% 格挡 / 阶10 免死 / 死亡广播。
   * 返回实际扣血（用于扣除 remainingDamage 计入城门溢出）
   * 若格挡成功返回 0（伤害归零，不顺延城门）；仅刺客伤害路径用 blockZeroes=true。
   */
  _damageWorker(pid, dmg, { blockZeroes = false, skipBlockForwarding = false } = {}) {
    const w = this._workerHp[pid];
    if (!w || w.isDead) return 0;

    // §30.3 阶6 15% 格挡
    if (this._calcWorkerBlock(pid)) {
      this._broadcast({
        type: 'worker_blocked',
        data: { playerId: pid, playerName: this._getPlayerName(pid) }
      });
      // blockZeroes=true（刺客伤害专用）：格挡后该份伤害直接丢弃，不顺延；
      // 否则（常规均摊）：伤害归零不扣 HP，但也不消耗 remainingDamage（调用方据返回 0 判断）
      return 0;
    }

    const actualDmg = Math.min(w.hp, dmg);
    w.hp -= actualDmg;

    if (w.hp <= 0) {
      // §30.3 阶10 每晚 1 次免死（优先消费；阶10 本身免死 → 不再消费 free_death_pass）
      if (this._getWorkerTier(pid) >= 10 && !this._legendReviveUsed[pid]) {
        this._legendReviveUsed[pid] = true;
        const maxHp = this._getWorkerMaxHp(pid);
        w.hp = Math.ceil(maxHp * 0.25);
        w.isDead = false;
        w.respawnAt = 0;
        this._broadcast({
          type: 'legend_revive_triggered',
          data: { playerId: pid, playerName: this._getPlayerName(pid) }
        });
        console.log(`[SurvivalEngine] Legend revive: ${this._getPlayerName(pid)} saved from death (${w.hp}/${maxHp} HP)`);
        return actualDmg;
      }

      // §34.4 E3b immortal 里程碑（20000）：一次全局免死豁免，消费后满血复活
      if (this._consumeFreeDeathPass(pid)) {
        return actualDmg;
      }

      w.hp = 0;
      w.isDead = true;
      const respawnSec = this._getWorkerRespawnSec(pid);
      const respawnMs  = respawnSec * 1000;
      w.respawnAt = Date.now() + respawnMs;

      this._broadcast({
        type: 'worker_died',
        timestamp: Date.now(),
        data: { playerId: pid, respawnAt: w.respawnAt }
      });
      console.log(`[SurvivalEngine] Worker ${pid} died, respawn at +${respawnSec}s (tier=${this._getWorkerTier(pid)})`);

      // §31 冻结清理：死亡矿工立即解冻（避免复活后仍被冻结标记）
      if (this._frozenWorkers.has(pid)) {
        this._frozenWorkers.delete(pid);
        this._broadcast({ type: 'worker_unfrozen', data: { playerId: pid } });
      }

      const respawnTimer = setTimeout(() => {
        if (this._workerHp[pid]?.isDead) this._reviveWorker(pid);
      }, respawnMs);
      this._waveTimers.push(respawnTimer);
    }
    return actualDmg;
  }

  _spawnWave(cfg, day, waveIndex) {
    if (this.state !== 'night') return;
    // §36.5 整夜和平夜（peace_night / peace_night_silent）→ 不刷怪
    if (this._peaceNightSkipSpawn) {
      console.log(`[SurvivalEngine] _spawnWave skipped (peace night full): wave=${waveIndex}`);
      return;
    }
    // §36.5 prelude（Day3 前 30s）→ 仍在和平窗口内 → 不刷怪
    if (this._peaceNightDelayUntil > 0 && Date.now() < this._peaceNightDelayUntil) {
      console.log(`[SurvivalEngine] _spawnWave skipped (peace prelude): wave=${waveIndex} delayUntil=${this._peaceNightDelayUntil}`);
      return;
    }

    // 生成数量随天数递增，应用基础难度倍率 + §30.6 动态难度数量加成
    const baseCnt = cfg.baseCount + Math.floor((day - 1) * 0.5);
    const count   = Math.max(1, Math.round(baseCnt * (this._monsterCntMult || 1.0) * (this._dynamicCountMult || 1.0)));

    const sides = ['left', 'right', 'top', 'all'];
    const spawnSide = sides[Math.floor(Math.random() * sides.length)];

    // ── §31 为本波每只怪选择 variant ─────────────────────────────
    const variants = new Array(count);
    for (let i = 0; i < count; i++) variants[i] = this._selectVariant(day, i);

    // §31.2/31.4 summoner post-process：若 day>=summonerDay 但 index=2 被 rush 占用，
    //   从后往前找第一个 'normal' 槽位迁移为 'summoner'（仍保证每波最多 1 只召唤怪）
    const diff = this._difficulty || 'normal';
    const summonerDayEff = MONSTER_VARIANT_SUMMONER_DAY + (diff === 'easy' ? 2 : diff === 'hard' ? -1 : 0);
    if (day >= summonerDayEff && !variants.includes('summoner')) {
      for (let i = variants.length - 1; i >= 2; i--) {
        if (variants[i] === 'normal') { variants[i] = 'summoner'; break; }
      }
    }

    // ── §31 构建 monsters[] 并写入 _activeMonsters（尊重 15 只上限，mini 同上限）──
    //   rush 不进 _activeMonsters（直奔城门，客户端不展示为可攻击目标？
    //   ↑ 策划案 §31.7：T5 AOE 对所有变种照常命中 → rush 也要进 _activeMonsters
    //     所以 rush 也进 _activeMonsters，只是伤害路由跳过矿工均摊）
    const baseHp       = Math.max(1, Math.round(cfg.normal ? cfg.normal.hp * (this._monsterHpMult || 1.0) * (this._dynamicHpMult || 1.0) : 50));
    const baseAtk      = cfg.normal ? cfg.normal.atk : 3;
    const baseSpd      = cfg.normal ? cfg.normal.spd : 2.0;
    const waveMonsters = [];
    let rushCountInWave     = 0;
    let assassinCountInWave = 0;
    let iceCountInWave      = 0;
    for (let i = 0; i < variants.length; i++) {
      const variant = variants[i];
      const hp = Math.max(1, Math.floor(baseHp * (VARIANT_HP_MULT[variant] || 1.0)));
      const speed = +(baseSpd * (VARIANT_SPEED_MULT[variant] || 1.0)).toFixed(2);
      const id = `w_${day}_${waveIndex}_${++this._monsterIdCounter}`;
      // §31.7 所有 variant 进 _activeMonsters（T5 AOE / 反伤 / 贡献记录都依赖 _activeMonsters 命中）
      //   maxAliveMonsters=15 为客户端渲染上限，服务端不阻塞 wave 生成；仅 mini spawn 遵守（§31.4）
      this._activeMonsters.set(id, {
        id,
        type: 'normal',     // Unity 客户端按 type 选择模型，variant 决定行为/色调
        variant,
        maxHp: hp,
        currentHp: hp,
        atk: baseAtk,
        spd: speed,
      });
      waveMonsters.push({ monsterId: id, type: 'normal', variant, hp, speed });
      if (variant === 'rush')     rushCountInWave++;
      else if (variant === 'assassin') assassinCountInWave++;
      else if (variant === 'ice') iceCountInWave++;
    }

    // §31.5 monster_wave 协议扩展：保留 count/monsterId 兼容旧客户端；新增 monsters[] 数组
    const waveData = {
      waveIndex,
      day,
      monsterId: cfg.monsterId,
      count,
      spawnSide,
      monsters: waveMonsters,
    };

    this.broadcast({ type: 'monster_wave', timestamp: Date.now(), data: waveData });

    // §24.4 time_freeze：冻结期内怪物对矿工/城门伤害暂停（仅本 wave 结算跳过）
    if (Date.now() < (this._freezeUntilMs || 0)) {
      console.log(`[SurvivalEngine] Wave ${waveIndex} damage skipped (time_freeze active)`);
      return;
    }

    // ── §31 伤害路由（分三路）────────────────────────────────────────
    // 总伤害 = count × monsterGateDamage × _workerDamageMult（保持原公式）
    // 1) rush 份额：rushCount × perMonsterDmg → 直接扣 gate（不均摊、不走 block、不进后续城门减伤/反伤流程）
    //    注：rush 绕过矿工直冲城门；为避免双倍减伤，这里直接扣 gateHp，再走后续 reducedDamage=0 路径。
    //    策划案 §31.2 明确"直接扣 gateHp（不均摊矿工）"。§10 减伤/反伤仍对普通伤害生效。
    //    为对齐玩家体感（rush 才是主要城门压力），rush 伤害不享受减伤。
    // 2) assassin 份额：assassin 每只独立攻击 HP 最低存活矿工（block 成功则该份归零，不顺延）
    // 3) 其余（normal/ice/summoner/guard/mini）：走原有均摊逻辑
    const perMonsterDmg = Math.max(1, Math.floor(this.monsterGateDamage * this._workerDamageMult));
    const aliveWorkersInitial = Object.entries(this._workerHp).filter(([, w]) => !w.isDead);

    // —— 子路 1：rush 直打城门（跳过减伤）——
    const rushGateDmg = rushCountInWave * perMonsterDmg;
    if (rushGateDmg > 0) {
      this.gateHp = Math.max(0, this.gateHp - rushGateDmg);
      console.log(`[SurvivalEngine] Wave ${waveIndex} rush×${rushCountInWave} → gate -${rushGateDmg} (no reduction)`);
    }

    // —— 子路 2：assassin 优先打最低 HP 矿工 ——
    //   blockZeroes=true：格挡后该份归零（不顺延，策划案"优先打低 HP"战术意图）
    //   所有矿工打光后，剩余刺客份顺延到 remainingDamage（最终计入城门）
    let assassinLeftover = 0;
    for (let a = 0; a < assassinCountInWave; a++) {
      const aliveNow = Object.entries(this._workerHp).filter(([, w]) => !w.isDead);
      if (aliveNow.length === 0) {
        assassinLeftover = assassinCountInWave - a;
        break;
      }
      const targetId = this._getAssassinTarget(aliveNow);
      if (!targetId) {
        assassinLeftover = assassinCountInWave - a;
        break;
      }
      this._damageWorker(targetId, perMonsterDmg, { blockZeroes: true });
    }

    // —— 子路 3：其余怪 → 常规均摊 ——
    const otherCount = Math.max(0, count - rushCountInWave - assassinCountInWave);
    const totalOtherDamage = otherCount * perMonsterDmg;
    let remainingDamage = totalOtherDamage + assassinLeftover * perMonsterDmg;

    const aliveWorkers = Object.entries(this._workerHp).filter(([, w]) => !w.isDead);
    if (aliveWorkers.length > 0 && remainingDamage > 0) {
      const damagePerWorker = Math.max(1, Math.floor(remainingDamage / aliveWorkers.length));
      for (const [pid] of aliveWorkers) {
        const w = this._workerHp[pid];
        if (!w || w.isDead) continue;
        // §30.3 阶6 15% 格挡（本次伤害归零）
        if (this._calcWorkerBlock(pid)) {
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
          // §30.3 阶10 每晚1次免死（优先消费；阶10 本身免死 → 不再消费 free_death_pass）
          if (this._getWorkerTier(pid) >= 10 && !this._legendReviveUsed[pid]) {
            this._legendReviveUsed[pid] = true;
            const maxHp = this._getWorkerMaxHp(pid);
            w.hp = Math.ceil(maxHp * 0.25);
            w.isDead = false;
            w.respawnAt = 0;
            this._broadcast({
              type: 'legend_revive_triggered',
              data: { playerId: pid, playerName: this._getPlayerName(pid) }
            });
            console.log(`[SurvivalEngine] Legend revive: ${this._getPlayerName(pid)} saved from death (${w.hp}/${maxHp} HP)`);
            continue;
          }

          // §34.4 E3b immortal 里程碑（20000）：一次全局免死豁免
          if (this._consumeFreeDeathPass(pid)) {
            continue;
          }

          w.hp = 0;
          w.isDead = true;
          const respawnSec = this._getWorkerRespawnSec(pid);
          const respawnMs  = respawnSec * 1000;
          w.respawnAt = Date.now() + respawnMs;

          this._broadcast({
            type: 'worker_died',
            timestamp: Date.now(),
            data: { playerId: pid, respawnAt: w.respawnAt }
          });
          console.log(`[SurvivalEngine] Worker ${pid} died, respawn at +${respawnSec}s (tier=${this._getWorkerTier(pid)})`);

          // §31 冻结清理：死亡矿工立即解冻
          if (this._frozenWorkers.has(pid)) {
            this._frozenWorkers.delete(pid);
            this._broadcast({ type: 'worker_unfrozen', data: { playerId: pid } });
          }

          const respawnTimer = setTimeout(() => {
            if (this._workerHp[pid]?.isDead) this._reviveWorker(pid);
          }, respawnMs);
          this._waveTimers.push(respawnTimer);
        }
      }
    }

    // 剩余伤害（矿工全死后的溢出）打城门；clamp到0避免矿工分摊时负值"治疗"城门
    remainingDamage = Math.max(0, remainingDamage);

    // ── 城门减伤（Lv3+）+ 反伤（Lv4+）──────────────────────
    const gateIdx = Math.max(0, this.gateLevel - 1);
    const dmgRed  = GATE_DMG_REDUCTION[gateIdx] || 0;
    const reducedDamage = remainingDamage > 0 ? Math.floor(remainingDamage * (1 - dmgRed)) : 0;
    if (reducedDamage > 0) {
      this.gateHp = Math.max(0, this.gateHp - reducedDamage);
    }

    // 反伤（Lv4+）：按 reducedDamage × thornsRatio 对整波怪物平均分摊（无怪位置，均分到 _activeMonsters）
    const thornsRatio = GATE_THORNS_RATIO[gateIdx] || 0;
    if (thornsRatio > 0 && reducedDamage > 0 && this._activeMonsters.size > 0) {
      const thornsTotal = Math.floor(reducedDamage * thornsRatio);
      if (thornsTotal > 0) {
        const perMonster = Math.max(1, Math.floor(thornsTotal / this._activeMonsters.size));
        const deaths = [];
        const hitIds = [];
        for (const [mid, m] of this._activeMonsters) {
          m.currentHp -= perMonster;
          hitIds.push(mid);
          if (m.currentHp <= 0) deaths.push({ id: mid, type: m.type, variant: m.variant });
        }
        // 反伤击杀不计玩家贡献（killerId 为城门标识，不触发 _trackContribution）
        for (const d of deaths) {
          this._activeMonsters.delete(d.id);
          // §34.4 E5b：gate_thorns 计入 monstersKilled 但不计玩家杀数
          this._trackNightKill('gate_thorns', d.type);
          this._broadcast({ type: 'monster_died', data: { monsterId: d.id, monsterType: d.type, killerId: 'gate_thorns' } });
          // §31 guard/summoner 死亡后置钩子
          this._postMonsterDeathHooks(d.id, d.type, d.variant);
        }
        this._broadcast({
          type: 'gate_effect_triggered',
          data: {
            effect: 'thorns',
            hitMonsters: hitIds,
            damagePerMonster: perMonster,
            totalDamage: thornsTotal,
          }
        });
      }
    }

    // ── §31.3 冰封怪 30% 概率冻结矿工（每只 ice 独立掷骰）──────────
    // 冻结依赖于 ice 怪对矿工成功命中；本实现简化为：每只 ice 独立选一名存活矿工掷骰。
    // 若目标已冻结则刷新冻结截止时间（不叠加持续时间）。
    if (iceCountInWave > 0) {
      const liveForFreeze = Object.entries(this._workerHp).filter(([, w]) => !w.isDead).map(([pid]) => pid);
      if (liveForFreeze.length > 0) {
        const now = Date.now();
        for (let k = 0; k < iceCountInWave; k++) {
          if (Math.random() >= ICE_FREEZE_CHANCE) continue;
          const pid = liveForFreeze[Math.floor(Math.random() * liveForFreeze.length)];
          if (!pid) continue;
          const existing = this._frozenWorkers.get(pid);
          const newUntil = now + ICE_FREEZE_MS;
          // 刷新（只延长不缩短）
          this._frozenWorkers.set(pid, Math.max(existing || 0, newUntil));
          this._broadcast({
            type: 'worker_frozen',
            data: { playerId: pid, duration: ICE_FREEZE_MS }
          });
          console.log(`[SurvivalEngine] Worker ${pid} frozen ${ICE_FREEZE_MS}ms by ice monster`);
        }
      }
    }

    // §16.1 all_dead 判定：本轮伤害结算后仍有矿工存活 → 刷新基准
    //   若全员死亡则 _lastAliveAt 保持上一次活着的时刻，_checkDefeat 据此判 5 分钟窗口
    if (Object.values(this._workerHp).some((w) => !w.isDead)) {
      this._lastAliveAt = Date.now();
    }

    // 广播矿工HP更新
    this._broadcastWorkerHp();

    const variantSummary = `rush=${rushCountInWave} assassin=${assassinCountInWave} ice=${iceCountInWave} other=${otherCount}`;
    console.log(`[SurvivalEngine] Wave ${waveIndex}: ${count} monsters (${spawnSide}) [${variantSummary}], workers=${aliveWorkersInitial.length}, rush→gate=${rushGateDmg}, remain raw=${remainingDamage} reduced=${reducedDamage} (Lv${this.gateLevel} red=${(dmgRed*100)|0}%) → HP ${this.gateHp}`);

    // 立即推送资源更新（城门HP变化）
    this._broadcastResourceUpdate();

    // 检查失败（城门被攻破）
    this._checkDefeat();
  }

  /**
   * §31.4 首领卫兵生成：Boss 出现时调用，固定生成 2 只 guard（_activeMonsters）。
   * spawnSide 取 _lastBossSpawnSide 保持与 Boss 同侧；guard 计入 maxAliveMonsters=15。
   */
  _spawnBossGuards(day) {
    const cfg = getWaveConfig(day);
    if (!cfg || !cfg.boss) return;
    const guardHpRaw = Math.floor(cfg.boss.hp * GUARD_HP_RATIO * (this._monsterHpMult || 1.0) * (this._dynamicHpMult || 1.0));
    const guardHp    = Math.max(1, guardHpRaw);
    const guardAtk   = (cfg.normal ? cfg.normal.atk : 3) * GUARD_ATK_MULT;
    const guardSpd   = +((cfg.normal ? cfg.normal.spd : 2.0) * (VARIANT_SPEED_MULT.guard || 1.0)).toFixed(2);
    const guardList  = [];
    this._guardsAlive = 0;
    // guards 固定生成 2 只（§31.4）；不受 maxAliveMonsters=15 限制（Boss 体验组合关键）
    for (let i = 0; i < GUARD_COUNT; i++) {
      const id = `guard_${day}_${++this._monsterIdCounter}`;
      this._activeMonsters.set(id, {
        id,
        type: 'normal',      // guard 沿用普通怪模型 type（客户端可按 variant 切暗金色调）
        variant: 'guard',
        maxHp: guardHp,
        currentHp: guardHp,
        atk: guardAtk,
        spd: guardSpd,
      });
      guardList.push({ monsterId: id, type: 'normal', variant: 'guard', hp: guardHp, speed: guardSpd });
      this._guardsAlive++;
    }
    if (guardList.length === 0) return;
    this.broadcast({
      type: 'monster_wave',
      timestamp: Date.now(),
      data: {
        waveIndex: -1,            // 负值标识非常规 wave（guard/summon spawn）
        day,
        monsterId: cfg.monsterId,
        count: guardList.length,
        spawnSide: this._lastBossSpawnSide || 'all',
        monsters: guardList,
        isBossGuardSpawn: true,
      }
    });
    console.log(`[SurvivalEngine] Boss guards spawned: ${guardList.length} (HP=${guardHp} ATK=${guardAtk}, side=${this._lastBossSpawnSide})`);
  }

  /**
   * §31.4 卫兵死亡检测：两只卫兵全亡 → Boss ATK ×1.3 并广播 boss_enraged
   * monsterId 以 'guard_' 前缀判定（或 variant === 'guard'）
   */
  _checkGuardDeath(monsterId, variant) {
    const isGuard = (variant === 'guard') || (typeof monsterId === 'string' && monsterId.startsWith('guard_'));
    if (!isGuard) return;
    if (this._guardsAlive <= 0) return;
    this._guardsAlive--;
    if (this._guardsAlive <= 0) {
      // 查找 boss（type='boss'）并 +1.3 倍攻击
      for (const [, m] of this._activeMonsters) {
        if (m.type === 'boss') {
          m.atk = Math.floor(m.atk * BOSS_ENRAGED_ATK_MULT);
          this._broadcast({
            type: 'boss_enraged',
            data: { newAtk: m.atk }
          });
          console.log(`[SurvivalEngine] Boss enraged: atk → ${m.atk} (guards all dead)`);
          break;
        }
      }
    }
  }

  /**
   * §31.4 召唤怪死亡：在原位置生成 SUMMONER_MINI_COUNT 只 mini（HP=30 ATK=1），计入 15 上限。
   * mini variant='mini' 走普通攻击路径；死亡不再触发任何特殊钩子。
   */
  _handleSummonerDeath(monsterId) {
    const miniList = [];
    const cfgDay   = getWaveConfig(this.currentDay);
    const miniSpd  = +((cfgDay && cfgDay.normal ? cfgDay.normal.spd : 2.0) * (VARIANT_SPEED_MULT.mini || 1.0)).toFixed(2);
    for (let i = 0; i < SUMMONER_MINI_COUNT; i++) {
      if (this._activeMonsters.size >= MAX_ALIVE_MONSTERS) break;
      const id = `mini_${++this._monsterIdCounter}`;
      this._activeMonsters.set(id, {
        id,
        type: 'normal',
        variant: 'mini',
        maxHp: MINI_HP,
        currentHp: MINI_HP,
        atk: MINI_ATK,
        spd: miniSpd,
      });
      miniList.push({ monsterId: id, type: 'normal', variant: 'mini', hp: MINI_HP, speed: miniSpd });
    }
    if (miniList.length === 0) {
      console.log(`[SurvivalEngine] Summoner ${monsterId} died but no mini spawn (maxAliveMonsters cap)`);
      return;
    }
    this.broadcast({
      type: 'monster_wave',
      timestamp: Date.now(),
      data: {
        waveIndex: -1,
        day: this.currentDay,
        monsterId: 'mini',
        count: miniList.length,
        spawnSide: 'spawn_at_death',
        monsters: miniList,
        isSummonSpawn: true,
      }
    });
    console.log(`[SurvivalEngine] Summoner ${monsterId} spawned ${miniList.length} mini`);
  }

  /**
   * §31 统一怪物死亡后置钩子：在 _activeMonsters.delete 与 monster_died broadcast 之后调用。
   * 处理：guard 计数 → Boss 暴走；summoner → 生成 mini；§36.4 D7 BossRush 池累加。
   * 所有 monster_died 触发点统一调用此方法（_handleAttack / T5 love_explosion / gate_frost_pulse /
   *   gate_thorns / meteor_shower / supporter love_explosion）。
   */
  _postMonsterDeathHooks(monsterId, monsterType, variant) {
    if (variant === 'guard') {
      this._checkGuardDeath(monsterId, variant);
    } else if (variant === 'summoner') {
      this._handleSummonerDeath(monsterId);
    }
    // §36.4 D7 夜晚：每只怪死亡 → 全服 BossRush 池累加伤害
    this._accumulateBossRushDamage(monsterType);
  }

  /**
   * §36.4 BossRush 累加：仅 seasonDay===7 && state==='night' 时向全服池扣伤害
   *   扣减值：普通怪 +100 / elite +500 / boss +2000（与击杀得分成比例）
   *   GlobalClock 内部判定池是否已归零（dedup），引擎只负责累加
   */
  _accumulateBossRushDamage(monsterType) {
    if (!this.globalClock || !this.seasonMgr) return;
    if (this.state !== 'night' || this.seasonMgr.seasonDay !== 7) return;
    let dmg = 100;
    if (monsterType === 'elite')      dmg = 500;
    else if (monsterType === 'boss')  dmg = 2000;
    else if (monsterType === 'elite_raid') dmg = 500;
    try { this.globalClock.damageBossRushPool(dmg); } catch (e) { /* ignore */ }
  }

  _clearNightWaves() {
    for (const t of this._waveTimers) {
      clearTimeout(t);
      clearInterval(t);
    }
    this._waveTimers = [];
  }

  // ==================== 随机事件系统（策划案 §8 + §34.3 B3 扩充）====================

  /**
   * 每秒检查是否触发随机事件
   * §34.3 B3 改造：
   *   - 频率 90-120s → 60-90s（RANDOM_EVENT_INTERVAL_MIN/MAX_SEC 常量）
   *   - 事件池 5 → 15（RANDOM_EVENT_POOL 加权抽取）
   *   - E03 白天也触发（生成 2-3 只弱侦察怪 atk=0，游走不攻击矿工）
   */
  _checkRandomEvents() {
    // B3：白天/夜晚均可触发（原仅白天；E03_monster_wave / meteor_shower 针对夜晚生效）
    if (this.state !== 'day' && this.state !== 'night') return;
    this._nextEventTimer -= 1; // 每秒递减
    if (this._nextEventTimer > 0) return;

    // 重置计时器（60-90s 后触发下次；B3 提频）
    this._nextEventTimer = RANDOM_EVENT_INTERVAL_MIN_SEC +
      Math.random() * (RANDOM_EVENT_INTERVAL_MAX_SEC - RANDOM_EVENT_INTERVAL_MIN_SEC);

    // 加权随机抽取事件
    const totalWeight = RANDOM_EVENT_POOL.reduce((s, e) => s + e.weight, 0);
    let r = Math.random() * totalWeight;
    let eventId = RANDOM_EVENT_POOL[0].id;
    for (const e of RANDOM_EVENT_POOL) {
      r -= e.weight;
      if (r <= 0) { eventId = e.id; break; }
    }
    this._applyRandomEvent(eventId);
  }

  /**
   * 应用随机事件效果并广播
   */
  _applyRandomEvent(eventId) {
    const name = EVENT_NAMES[eventId] || eventId;
    console.log(`[SurvivalEngine] Random event: ${eventId} (${name})`);

    // extraData 用于携带事件特有字段（如 addFood/hideMonsterHp/targetPlayerId 等）
    const extraData = {};

    switch (eventId) {
      // ========== 原 5 种 ==========
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
        // §34.3 B3 改造：白天 & 夜晚都触发
        //   夜晚：保持原行为（生成 2 只弱怪）
        //   白天：生成 2-3 只弱侦察怪（scout；atk=0 不攻击矿工；hp ×0.3 近似）
        if (this.state === 'night') {
          for (let i = 0; i < 2; i++) {
            const id = `wave_event_${++this._monsterIdCounter}`;
            this._activeMonsters.set(id, {
              id, type: 'normal', variant: 'normal',
              maxHp: 30, currentHp: 30, atk: 3,
            });
          }
        } else if (this.state === 'day') {
          // 白天弱侦察怪：atk=0（不攻击矿工），hp 为普通怪 0.3
          const cfg = getWaveConfig(this.currentDay || 1);
          const baseHp = (cfg && cfg.normal && cfg.normal.hp) ? cfg.normal.hp : 150;
          const scoutHp = Math.max(1, Math.round(baseHp * 0.3));
          const scoutCount = 2 + Math.floor(Math.random() * 2); // 2-3 只
          const scoutIds = [];
          const scoutList = [];
          for (let i = 0; i < scoutCount; i++) {
            const id = `scout_${++this._monsterIdCounter}`;
            // type='normal' + atk=0 近似 scout；客户端无 scout 类型时按普通渲染但不计伤害（atk=0 本身就不打矿工）
            const normalSpd = +((cfg && cfg.normal && cfg.normal.spd) ? cfg.normal.spd : 2.0).toFixed(2);
            this._activeMonsters.set(id, {
              id, type: 'normal', variant: 'normal',
              maxHp: scoutHp, currentHp: scoutHp, atk: 0, spd: normalSpd,
            });
            scoutIds.push(id);
            scoutList.push({ monsterId: id, type: 'normal', variant: 'normal', hp: scoutHp, speed: normalSpd });
          }
          extraData.scoutCount = scoutCount;
          extraData.scoutIds   = scoutIds;
          extraData.isDaytimeScout = true;

          // Fix D (组 B Reviewer P0) §34B B3 E03 daytime scout：
          //   原实现只在 _activeMonsters 新增 scout，未广播 monster_wave，客户端无法渲染侦察兵。
          //   修复：对齐现有 guard/summon spawn 的 monster_wave 广播格式（waveIndex=-1 标识非常规波次）。
          if (scoutList.length > 0) {
            this.broadcast({
              type: 'monster_wave',
              timestamp: Date.now(),
              data: {
                waveIndex: -1,
                day: this.currentDay || 1,
                monsterId: 'scout',
                count: scoutList.length,
                spawnSide: 'all',
                monsters: scoutList,
                isDaytimeScout: true,
              }
            });
          }
        }
        this._broadcastResourceUpdate();
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

      // ========== §34.3 B3 新增 10 种 ==========
      case 'airdrop_supply': {
        // 空投补给：随机 30-80 食物/煤炭/矿石（各自独立取值，任何时段触发）
        const addFood = 30 + Math.floor(Math.random() * 51);  // 30-80
        const addCoal = 30 + Math.floor(Math.random() * 51);
        const addOre  = 30 + Math.floor(Math.random() * 51);
        this.food = Math.min(2000, this.food + addFood);
        this.coal = Math.min(1500, this.coal + addCoal);
        this.ore  = Math.min(800,  this.ore  + addOre);
        extraData.addFood = addFood;
        extraData.addCoal = addCoal;
        extraData.addOre  = addOre;
        this._broadcastResourceUpdate();
        break;
      }
      case 'ice_ground': {
        // 地面冰封：矿工移动速度 ×0.8（在 _applyWorkEffect 时间算式处乘入），持续 30s
        // Fix G (组 B Reviewer P1)：策划案原文"移动速度 -20%"，项目无移动速度概念，
        //   采用采矿产出 ×0.8 作为等效实现（_applyWorkEffect 里乘入 iceGroundMult）。
        this._iceGroundEndAt = Date.now() + 30000;
        extraData.durationMs = 30000;
        extraData.slowMult   = 0.8;
        const t = setTimeout(() => {
          if (Date.now() >= this._iceGroundEndAt) this._iceGroundEndAt = 0;
        }, 30000);
        this._randomEventTimers.push(t);
        break;
      }
      case 'aurora_flash': {
        // 极光闪现：全员效率 +5%，5s（短暂 buff）
        //   复用 _auroraEffMult 字段？不——_auroraEffMult 已被 §24.4 轮盘使用为 ×1.5 （60s），避免冲突
        //   这里改为临时 +5% 独立加成：直接改 efficiency666Bonus 的 Math.max 叠加 → 不行（会被覆盖）
        //   采用 tempDecayMultiplier 风格：新增 _auroraFlashEndAt + efficiency 加权时读取
        //   简化：给 foodBonus/oreBonus 各 +5% 相当于全员效率 +5%（5s 短暂，影响小）
        const prevFoodBonus = this.foodBonus;
        const prevOreBonus  = this.oreBonus;
        this.foodBonus = Math.max(1.0, this.foodBonus) * 1.05;
        this.oreBonus  = Math.max(1.0, this.oreBonus)  * 1.05;
        extraData.durationMs  = 5000;
        extraData.effBonus    = 0.05;
        const t = setTimeout(() => {
          // 保守回滚（如果期间有其他事件设置 foodBonus/oreBonus，不强制复位；仅清除本事件加成）
          this.foodBonus = prevFoodBonus;
          this.oreBonus  = prevOreBonus;
        }, 5000);
        this._randomEventTimers.push(t);
        break;
      }
      case 'earthquake': {
        // 地震：炉温 -5，城门 -50HP
        this.furnaceTemp = Math.max(this.minTemp, this.furnaceTemp - 5);
        this.gateHp      = Math.max(0, this.gateHp - 50);
        extraData.subFurnaceTemp = 5;
        extraData.subGateHp      = 50;
        this._broadcastResourceUpdate();
        break;
      }
      case 'meteor_shower': {
        // 流星雨：仅夜晚，随机击杀 2-3 只怪物
        if (this.state === 'night' && this._activeMonsters.size > 0) {
          const killCount = Math.min(
            this._activeMonsters.size,
            2 + Math.floor(Math.random() * 2) // 2-3 只
          );
          const allIds = [...this._activeMonsters.keys()];
          // 优先击杀普通怪（避免 boss 被流星秒杀破坏节奏）
          const normalIds = allIds.filter(id => {
            const m = this._activeMonsters.get(id);
            return m && m.type === 'normal';
          });
          const candidates = normalIds.length >= killCount ? normalIds : allIds;
          const killed = [];
          for (let i = 0; i < killCount && candidates.length > 0; i++) {
            const idx = Math.floor(Math.random() * candidates.length);
            const mid = candidates.splice(idx, 1)[0];
            const m   = this._activeMonsters.get(mid);
            if (!m) continue;
            this._activeMonsters.delete(mid);
            this._broadcast({
              type: 'monster_died',
              data: { monsterId: mid, monsterType: m.type, killerId: 'meteor_shower' },
            });
            this._postMonsterDeathHooks(mid, m.type, m.variant);
            killed.push(mid);
          }
          extraData.killedCount = killed.length;
          extraData.killedIds   = killed;
          this._broadcastResourceUpdate();
        }
        break;
      }
      case 'heavy_fog': {
        // 浓雾：客户端隐藏怪物血条（服务端发 flag hideMonsterHp=true），30s
        this._heavyFogEndAt = Date.now() + 30000;
        extraData.durationMs     = 30000;
        extraData.hideMonsterHp  = true;
        const t = setTimeout(() => {
          if (Date.now() >= this._heavyFogEndAt) {
            this._heavyFogEndAt = 0;
            // 事件结束：补发 resource_update 让客户端恢复血条显示
            this._broadcastResourceUpdate();
          }
        }, 30000);
        this._randomEventTimers.push(t);
        this._broadcastResourceUpdate();
        break;
      }
      case 'hot_spring': {
        // 温泉涌出：炉温 +2°C/5s，持续 30s（6 次 tick，由 _tickHotSpring 每秒检查）
        this._hotSpringEndAt    = Date.now() + 30000;
        this._hotSpringLastTick = this._tickCounter || 0; // 基于 tick 计数
        extraData.durationMs  = 30000;
        extraData.tempPerTick = 2;
        extraData.tickSec     = 5;
        break;
      }
      case 'food_spoil': {
        // 食物变质：food ×0.85（一次性，无持续）
        const before = this.food;
        this.food = Math.floor(this.food * 0.85);
        extraData.foodBefore = before;
        extraData.foodAfter  = this.food;
        extraData.lossPct    = 0.15;
        this._broadcastResourceUpdate();
        break;
      }
      case 'inspiration': {
        // 灵感爆发：下一次 work_command 产出 ×2（_oneShotWorkMult=2，_applyWorkEffect 消费后归 1.0）
        this._oneShotWorkMult = 2.0;
        extraData.nextWorkMult = 2.0;
        break;
      }
      case 'morale_boost': {
        // 矿工士气：随机一名矿工头顶"加油"气泡 3s（服务端选 playerId + 客户端 UI 渲染）
        //   候选池：contributions 中有记录的玩家 id（守护者 + 已注册）；无则 skip
        const candidates = Object.keys(this.contributions || {}).filter(pid => pid);
        if (candidates.length > 0) {
          const pid = candidates[Math.floor(Math.random() * candidates.length)];
          extraData.targetPlayerId   = pid;
          extraData.targetPlayerName = this.playerNames[pid] || pid;
          extraData.bubbleText       = '加油';
          extraData.durationMs       = 3000;
        }
        break;
      }
    }

    // 统一广播 random_event（附带事件特有 data）
    this.broadcast({
      type: 'random_event',
      timestamp: Date.now(),
      data: Object.assign({ eventId, name }, extraData),
    });
  }

  /**
   * §34.3 B3 hot_spring 事件 tick 处理：每 5s 炉温 +2°C（持续 30s = 6 次）
   * 由 _tick 每秒调用，_tickCounter % 5 === 0 触发
   */
  _tickHotSpringIfActive() {
    if (!this._hotSpringEndAt || Date.now() >= this._hotSpringEndAt) {
      if (this._hotSpringEndAt && Date.now() >= this._hotSpringEndAt) {
        this._hotSpringEndAt = 0;
        this._hotSpringLastTick = 0;
      }
      return;
    }
    // 每 5 秒（25 tick）触发一次
    if (this._tickCounter - this._hotSpringLastTick < 25) return;
    this._hotSpringLastTick = this._tickCounter;
    this.furnaceTemp = Math.min(this.maxTemp, this.furnaceTemp + 2);
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

  // ==================== §34.3 Layer 2 组 B：B2 / B4 / B6 / B10 辅助方法 ====================

  /**
   * §34.3 B2：记录一个"戏剧性事件"候选，同一局内按优先级/新鲜度替换
   *   type: 'boss_low_hp' | 'tension_drop' | 'free_death_pass' | 'gate_critical' | 其他
   *   desc: 简短描述文本
   *   优先级（按 type 权重）：越往后覆盖越强
   */
  _recordDramaticEvent(type, desc) {
    const PRIORITY = {
      'tension_drop':     1,
      'gate_critical':    2,
      'boss_low_hp':      3,
      'free_death_pass':  4,
    };
    const newP = PRIORITY[type] || 0;
    const prev = this._mostDramaticEvent;
    if (!prev || (PRIORITY[prev.type] || 0) <= newP) {
      this._mostDramaticEvent = {
        type,
        desc: desc || '',
        day:  this.currentDay || 0,
      };
    }
  }

  /**
   * §34.3 B2：更新 closest call（最危险时刻）——每次 _broadcastResourceUpdate 调用
   *   跨夜晚跟踪 gateHp / gateMaxHp 最低百分比
   */
  _updateClosestCall() {
    if (!this.gateMaxHp || this.gateMaxHp <= 0) return;
    const pct = Math.max(0, Math.min(1, this.gateHp / this.gateMaxHp));
    if (this._overallClosestCall == null || pct < this._overallClosestCall.hpPct) {
      this._overallClosestCall = { hpPct: pct, day: this.currentDay || 0 };
    }
  }

  /**
   * §34.3 B2：构造 settlement_highlights 数据包
   *   在 _enterSettlement 中调用（失败/manual 均广播）
   */
  _buildSettlementHighlights() {
    // topDamage: 本局累计伤害最高
    let topDmgId = null, topDmgVal = 0;
    for (const [pid, dmg] of Object.entries(this._damageLeaderboard)) {
      if (dmg > topDmgVal) { topDmgVal = dmg; topDmgId = pid; }
    }
    const topDamagePlayerName = topDmgId ? (this.playerNames[topDmgId] || topDmgId) : null;

    const bestRescue = this._bestRescue;
    const mostDramatic = this._mostDramaticEvent;
    const closest = this._overallClosestCall || { hpPct: 1.0, day: 0 };

    // 赛季/日 ID（MVP 用 seasonDay）
    const dayOrSeasonId = (this.seasonMgr && this.seasonMgr.seasonDay)
      ? this.seasonMgr.seasonDay
      : (this.currentDay || 0);

    return {
      dayOrSeasonId,
      topDamagePlayerId:   topDmgId,
      topDamagePlayerName: topDamagePlayerName,
      topDamageValue:      Math.round(topDmgVal),
      bestRescueGiftId:    bestRescue ? bestRescue.giftId   : null,
      bestRescueGiftName:  bestRescue ? bestRescue.giftName : null,
      bestRescuePlayerName: bestRescue ? bestRescue.playerName : null,
      mostDramaticEvent:   mostDramatic ? {
        type: mostDramatic.type,
        desc: mostDramatic.desc,
        day:  mostDramatic.day,
      } : null,
      closestCallHpPct:    Math.round(closest.hpPct * 1000) / 1000,
      closestCallDay:      closest.day || 0,
    };
  }

  /**
   * §34.3 B2：主播跳过结算倒计时（C→S streamer_skip_settlement）
   *   校验：state==='settlement' && _settleTimerHandle 存在
   *   执行：clearTimeout + 立即执行倒计时到期逻辑（_enterRecovery）
   *   Fix A (组 B Reviewer P0)：结算倒计时已延长至 30000ms，
   *     跳过窗口自然扩大到 30s（只要 _settleTimerHandle 非 null 即视为 valid）。
   */
  handleStreamerSkipSettlement(playerId) {
    if (this.state !== 'settlement') {
      console.log(`[Settlement] skip rejected: state=${this.state}`);
      return false;
    }
    if (!this._settleTimerHandle) {
      console.log(`[Settlement] skip rejected: timer already fired or not set`);
      return false;
    }
    clearTimeout(this._settleTimerHandle);
    this._settleTimerHandle = null;
    console.log(`[Settlement] streamer skip: playerId=${playerId}, advance to recovery immediately`);
    // 立即执行 30s 倒计时到期等价逻辑（Fix A：原 8s → 30s）
    this._recoveryTimer = null;
    this._enterRecovery();
    return true;
  }

  /**
   * §34.3 B4：助威者冷却日志输出（每 60s 一次）
   *   从 _tick 每秒调用；内部判断 tickCounter 差值
   */
  _logSupporterStatsIfDue() {
    // _tickCounter 每秒 +5（5 tick/s）；60s = 300 tick
    if (this._tickCounter - this._supporterStatsLogAt < 300) return;
    this._supporterStatsLogAt = this._tickCounter;
    const s = this._supporterStats;
    const rate = s.totalAttempts > 0 ? ((s.blockedByThrottleCount / s.totalAttempts) * 100).toFixed(1) : '0.0';
    console.log(`[Supporter] hit=${s.hitCount} blocked=${s.blockedByThrottleCount} throttle_rate=${rate}%`);
  }

  /**
   * §34.3 B6：礼物 douyin_id 双路匹配统计日志（每 60s 一次）
   */
  _logGiftMatchStatsIfDue() {
    if (this._tickCounter - this._giftMatchStatsLogAt < 300) return;
    this._giftMatchStatsLogAt = this._tickCounter;
    const s = this._giftMatchStats;
    const total = s.exactHit + s.fallbackHit + s.missed;
    if (total === 0) return; // 无礼物活动，不必输出噪音日志
    const fallbackRate = total > 0 ? ((s.fallbackHit / total) * 100).toFixed(1) : '0.0';
    const missRate     = total > 0 ? ((s.missed / total) * 100).toFixed(1) : '0.0';
    console.log(`[Gift] match stats: exact=${s.exactHit} fallback=${s.fallbackHit} missed=${s.missed} fallback_rate=${fallbackRate}% miss_rate=${missRate}%`);
  }

  /**
   * §34.3 B10a：效率竞赛推送
   *   仅 phase='day' && tension<30 时每 15s 推 Top3 + dayTotal
   *   数据源：_dayStats（_enterDay 初始化，_enterNight 清零；_trackContribution 累加）
   */
  _broadcastEfficiencyRaceIfDue() {
    if (this.state !== 'day') return;
    if (!this._dayStats) return;

    const now = Date.now();
    if (now - this._lastEfficiencyRaceAt < 15000) return;

    // 张力实时计算，必须 < 30 才推送（安全期）
    const tension = this._calcTension();
    if (tension >= 30) return;

    this._lastEfficiencyRaceAt = now;

    // Top3：从 _dayStats.contributions 排序
    const entries = Object.entries(this._dayStats.contributions)
      .map(([pid, contrib]) => ({ pid, contrib }))
      .sort((a, b) => b.contrib - a.contrib)
      .slice(0, 3);

    const top3 = entries.map((e, idx) => ({
      rank:       idx + 1,
      playerId:   e.pid,
      playerName: this.playerNames[e.pid] || e.pid,
      contribution: Math.round(e.contrib),
    }));

    this.broadcast({
      type: 'efficiency_race',
      timestamp: now,
      data: {
        top3,
        dayTotal: Math.round(this._dayStats.totalDay || 0),
      },
    });
  }

  /**
   * §34.3 B10b：夜晚预告（白天最后 10s 触发一次）
   *   推送：monsterCount / bossHp / nightModifier（预先选定并缓存，_enterNight 消费）
   *   定时逻辑：remainingTime<=10 && state=='day' && 当前 day 尚未广播
   */
  _broadcastDayPreviewIfDue() {
    if (this.state !== 'day') return;
    if (this.remainingTime > 10 || this.remainingTime <= 0) return;
    // 按当前 day 去重（同一天仅推一次）
    if (this._dayPreviewBroadcastedForDay === this.currentDay) return;

    this._dayPreviewBroadcastedForDay = this.currentDay;

    // 下一夜配置（等价 _enterNight 的读法）
    const nextDay = this.currentDay; // day 进入夜晚 day 编号不变（_enterNight 传入当前 day）
    const cfg     = getWaveConfig(nextDay);
    // 预算 nightModifier（和平夜强制 normal，其他加权随机）
    const seasonDay = this.seasonMgr ? this.seasonMgr.seasonDay : 1;
    const peaceVariant = this._getPeaceNightVariant(seasonDay);
    const skipSpawn = (peaceVariant === 'peace_night' || peaceVariant === 'peace_night_silent');

    let modifier;
    if (skipSpawn) {
      modifier = NIGHT_MODIFIERS[0]; // normal
      this._pendingNightModifier = null;
    } else {
      modifier = this._pickNightModifier();
      this._pendingNightModifier = modifier; // _enterNight 将消费
    }

    // 预计算 monsterCount / bossHp（不考虑主题倍率，预告用粗估即可）
    const hpMult  = (this._monsterHpMult  || 1.0) * (this._dynamicHpMult    || 1.0);
    const cntMult = (this._monsterCntMult || 1.0) * (this._dynamicCountMult || 1.0);
    let monsterCount = 0;
    if (cfg && cfg.normal) monsterCount += Math.max(1, Math.round(cfg.normal.count * cntMult));
    if (cfg && cfg.elite)  monsterCount += Math.max(0, Math.round(cfg.elite.count  * cntMult));
    if (cfg && cfg.boss)   monsterCount += 1;
    const bossHp = cfg && cfg.boss ? Math.max(1, Math.round(cfg.boss.hp * hpMult)) : 0;

    // Fix E (组 B Reviewer P1) §34B B10b day_preview：
    //   原实现只发 {id, name}，前端 DayPreviewBanner 读 description 为空，固定显示"特殊效果：无"。
    //   修复：补带 description（服务端字段名为 desc；NightModifierData.description 对齐策划案）。
    this.broadcast({
      type: 'day_preview',
      timestamp: Date.now(),
      data: {
        monsterCount,
        bossHp,
        nightModifier: modifier ? {
          id:          modifier.id,
          name:        modifier.name,
          description: modifier.desc || '',
        } : null,
      },
    });
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

  // ==================== §17.15 新手引导气泡 ====================

  /**
   * 抖音 `viewer_joined` 事件钩子的服务端判定入口。
   * 满足 5 分钟节流 + 当前为 day/night + 未被主播关闭时，生成 sessionId 并广播 show_onboarding_sequence。
   *
   * 注意：此方法是**事件驱动**（由 viewer_joined 事件调用），严禁放到 setInterval 中轮询。
   * 当前项目尚未接入抖音 viewer_joined SDK 事件，SurvivalRoom 里留了 'viewer_joined' case 占位；
   * 抖音 SDK 接入时，将真实事件路由到该 case，本方法自动生效。
   */
  _maybeTriggerOnboarding() {
    if (this._onboardingDisabled) return;
    // idle / loading / settlement / recovery 等非游戏中的状态不触发
    if (this.state !== 'day' && this.state !== 'night') return;

    const now = Date.now();
    if (now - this._lastOnboardingAt < ONBOARDING_THROTTLE_MS) return;

    const sessionId = crypto.randomUUID();
    this._lastOnboardingAt        = now;
    this._lastOnboardingSessionId = sessionId;
    this._broadcast({
      type: 'show_onboarding_sequence',
      data: { sessionId, priority: 2 }
    });
    console.log(`[SurvivalEngine] Onboarding sequence triggered (sessionId=${sessionId})`);
  }

  /**
   * `sync_state` 联动：客户端断连重连 / 主动同步时，若节流窗口尚新（≤ 30s）且有 sessionId，
   * 沿用原 sessionId 向该发起者 unicast 重发一次 show_onboarding_sequence；
   * 客户端靠 sessionId 幂等，不会重播。
   * 超窗口不重发；此方法只 unicast，不广播整房。
   *
   * @param {function} send - 单播发送函数 (msg) => void（通常为 SurvivalRoom 封装的 ws.send 包装）
   */
  _replayOnboardingIfInWindow(send) {
    if (!this._lastOnboardingSessionId) return;
    const now = Date.now();
    if (now - this._lastOnboardingAt > ONBOARDING_REPLAY_WINDOW_MS) return;
    try {
      send({
        type: 'show_onboarding_sequence',
        timestamp: Date.now(),
        data: { sessionId: this._lastOnboardingSessionId, priority: 2 }
      });
    } catch (e) {
      console.warn(`[SurvivalEngine] Onboarding replay send error: ${e.message}`);
    }
  }

  // ==================== 内部：广播 ====================

  /**
   * 内部广播快捷方式（自动附加 timestamp）
   */
  _broadcast(msg) {
    this.broadcast(Object.assign({ timestamp: Date.now() }, msg));
  }

  _broadcastResourceUpdate() {
    // §34.4 Layer 3 组 C E1/E4/E3b 同步下发：前端 TensionOverlay / GiftRecommendation / CoopMilestone 驱动
    //   tension              危机张力（0~100），前端按阈值切 BGM 暗角脉冲（计算纯算术，无副作用）
    //   giftRecommendation   精准付费推荐 { giftId, reason, urgency }（内部含 60s 节流 + tension≤40 gentle 锁）
    //   totalContribution    全局协作总贡献（_trackContribution 累加），CoopMilestoneUI 进度条驱动
    const tension            = this._calcTension();
    const giftRecommendation = this._calcGiftRecommendation(tension);
    // §34.4 E5b：每次 resource_update 追踪一次夜晚最低城门 HP%（5s 精度对战报足够）
    this._trackNightGateHp();
    // §34.3 B2：同步追踪本局 closest call（跨白天/夜晚）+ 张力暴跌检测
    this._updateClosestCall();
    // tension 下跌幅度 ≥65 视为"最戏剧性事件"候选
    if (this._tensionPrev > 0 && (this._tensionPrev - tension) >= 65) {
      this._recordDramaticEvent('tension_drop', `危机从 ${this._tensionPrev} 降至 ${tension}`);
    }
    this._tensionPrev = tension;
    // gateHp 跌破 5% 记为 "gate_critical"
    if (this.gateMaxHp > 0 && (this.gateHp / this.gateMaxHp) < 0.05) {
      this._recordDramaticEvent('gate_critical', `城门仅剩 ${Math.round((this.gateHp / this.gateMaxHp) * 100)}% HP`);
    }

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
        gateTierName:      GATE_TIER_NAMES[this.gateLevel - 1] || '',
        gateFeatures:      GATE_FEATURES_BY_LV[this.gateLevel - 1] || [],
        gateDailyUpgraded: !!this._gateUpgradedToday,
        remainingTime: Math.round(this.remainingTime),
        scorePool:    Math.round(this.scorePool),
        workerHp:     Object.fromEntries(
          Object.entries(this._workerHp).map(([pid, w]) => [pid, {
            hp: w.hp, maxHp: w.maxHp, isDead: w.isDead, respawnAt: w.respawnAt
          }])
        ),
        // §34.4 Layer 3 组 C 扩展字段
        tension,
        giftRecommendation,
        totalContribution: Math.round(this._totalContribution || 0),
        // §34.3 B3 heavy_fog：客户端据此隐藏怪物血条（30s）
        hideMonsterHp: (this._heavyFogEndAt > 0 && Date.now() < this._heavyFogEndAt),
      }
    });
  }

  // ==================== §34.4 Layer 3 组 C（沉浸体验：E1 / E3 / E4）====================

  /**
   * §34.4 E1 危机感知：计算当前全局张力值（0-100 整数）
   * 权重分配（策划案 5216-5230）：食物 35 / 炉温 25 / 城门 25 / 煤炭 10-15
   * 时间乘数（永续模式）：fortressDay 100 日达到 +30% 封顶
   *
   * 每次 _broadcastResourceUpdate 调用一次，保持轻量（纯算术，无副作用）
   */
  _calcTension() {
    let t = 0;
    // 食物：消耗最快的致命资源，权重最高
    t += (1 - this.food / 2000) * 35;
    // 炉温：越接近 -100 越危急（向下偏离 20℃起计）
    t += Math.max(0, -this.furnaceTemp + 20) / 120 * 25;
    // 城门：HP 比例直接映射（夜晚核心压力）
    const gateMax = this.gateMaxHp > 0 ? this.gateMaxHp : 1;
    t += (1 - this.gateHp / gateMax) * 25;
    // 煤炭：为零 = 炉温即将下降（间接危险，权重跳升 10→15）
    t += (this.coal <= 0 ? 15 : (1 - this.coal / 1500) * 10);
    // 时间乘数：永续模式 fortressDay 驱动，100 日达到满权重（+30%）
    //   §34 永续模式改 fortressDay 为主；回退到 currentDay（|| 1 兜底避免 /0）
    const day = (this.fortressDay != null ? this.fortressDay : this.currentDay) || 1;
    const timeMult = 1 + Math.min(1, day / 100) * 0.3;
    return Math.round(Math.min(100, Math.max(0, t * timeMult)));
  }

  /**
   * §34.4 E4 精准付费触发：基于当前张力 + 资源比例 + 阶段推荐礼物
   *
   * 关键规则（策划案 5349）：
   *   - 仅 tension > 40 时允许返回非 gentle（否则强制兜底 fairy_wand:gentle，避免安全期推销）
   *   - 同种 giftId 推荐 60s 最小间隔（_lastRecGiftId + _lastRecAt）防刷屏；命中节流回落到 gentle
   *
   * 调用时机：仅 _broadcastResourceUpdate 内部（5s / 事件驱动），不单独定时
   */
  _calcGiftRecommendation(tensionVal) {
    const tension    = tensionVal != null ? tensionVal : this._calcTension();
    const gateRatio  = this.gateMaxHp > 0 ? this.gateHp / this.gateMaxHp : 1;
    const foodRatio  = this.food / 2000;
    const tempDanger = this.furnaceTemp < -30;

    // 原始候选（严格按策划案 5327-5339 顺序匹配）
    let rec;
    if (gateRatio < 0.2 && this.state === 'night') {
      rec = { giftId: 'love_explosion',  reason: '城门即将倒塌！',           urgency: 'critical' };
    } else if (foodRatio < 0.15) {
      rec = { giftId: 'mystery_airdrop', reason: '食物告急！全面补给！',       urgency: 'critical' };
    } else if (foodRatio < 0.3) {
      rec = { giftId: 'donut',           reason: '食物偏低，甜甜圈补食物+修城门', urgency: 'high'     };
    } else if (tempDanger) {
      rec = { giftId: 'energy_battery',  reason: '炉温下降！紧急加热',         urgency: 'high'     };
    } else if (this.state === 'night' && this._isBossAlive()) {
      rec = { giftId: 'love_explosion',  reason: 'Boss 在场！AOE 清场+修城门', urgency: 'medium'   };
    } else {
      rec = { giftId: 'fairy_wand',      reason: '提升你的矿工效率',           urgency: 'gentle'   };
    }

    // 关键设计：tension ≤ 40 时强制 gentle（不强推销）
    if (rec.urgency !== 'gentle' && tension <= 40) {
      rec = { giftId: 'fairy_wand', reason: '提升你的矿工效率', urgency: 'gentle' };
    }

    // 60s 最小间隔（同 giftId + 非 gentle）；命中节流时回落 gentle，保持客户端视觉"降级"
    const now = Date.now();
    if (rec.urgency !== 'gentle') {
      if (this._lastRecGiftId === rec.giftId && (now - (this._lastRecAt || 0)) < 60000) {
        rec = { giftId: 'fairy_wand', reason: '提升你的矿工效率', urgency: 'gentle' };
      } else {
        this._lastRecGiftId = rec.giftId;
        this._lastRecAt     = now;
      }
    }
    return rec;
  }

  /** §34.4 E4 Boss 活跃判定：_activeMonsters 中存在 type='boss' */
  _isBossAlive() {
    for (const m of this._activeMonsters.values()) {
      if (m && m.type === 'boss') return true;
    }
    return false;
  }

  /**
   * §34.4 E3b 协作里程碑：全服总贡献跨阈值 → 广播 coop_milestone + 应用全局增益
   *
   * 阈值设计（策划案 5300-5304）：500 / 2000 / 5000 / 10000 / 20000
   * 每个阈值的效果：
   *   500  「众志成城」全员采矿效率 +10%      → _milestoneEffMult = 1.1
   *   2000 「钢铁意志」矿石修城门速度 x2      → _milestoneGateRepairMult = 2.0
   *   5000 「极地奇迹」全矿工 HP 基线 +50      → _milestoneWorkerHpBonus = 50
   *   10000「传说降临」所有效果 +20%          → _milestoneGlobalMult = 1.2
   *   20000「不朽证明」一次免费死亡豁免       → _freeDeathPass = true（首次死亡时消费）
   *
   * 调用时机：_trackContribution 正向 finalAmount 累加后立即触发（每次跨阈值只触发一次）
   */
  _checkCoopMilestones() {
    const MILESTONES = [
      { total: 500,   id: 'unity',       name: '众志成城', desc: '全员采矿效率 +10%',       apply: () => { this._milestoneEffMult        = 1.1;  } },
      { total: 2000,  id: 'steel_will',  name: '钢铁意志', desc: '矿石修城门速度 ×2',        apply: () => { this._milestoneGateRepairMult = 2.0;  } },
      { total: 5000,  id: 'miracle',     name: '极地奇迹', desc: '全矿工 HP 基线 +50',       apply: () => { this._applyMilestoneWorkerHpBonus(50); } },
      { total: 10000, id: 'legend',      name: '传说降临', desc: '所有效果 +20%',            apply: () => { this._milestoneGlobalMult     = 1.2;  } },
      { total: 20000, id: 'immortal',    name: '不朽证明', desc: '一次免费死亡豁免',         apply: () => { this._freeDeathPass           = true; } },
    ];

    for (let i = 0; i < MILESTONES.length; i++) {
      const m = MILESTONES[i];
      if (this._totalContribution < m.total) break;
      if (this._milestonesUnlocked.has(m.id)) continue;
      this._milestonesUnlocked.add(m.id);
      try { m.apply(); } catch (e) { console.warn(`[SurvivalEngine] milestone apply error (${m.id}): ${e.message}`); }

      const next = MILESTONES[i + 1];
      this.broadcast({
        type: 'coop_milestone',
        timestamp: Date.now(),
        data: {
          id:           m.id,
          name:         m.name,
          desc:         m.desc,
          total:        m.total,
          currentTotal: Math.round(this._totalContribution),
          nextTarget:   next ? next.total : null,
        }
      });
      console.log(`[SurvivalEngine] coop_milestone unlocked: ${m.name} (total=${Math.round(this._totalContribution)})`);
    }
  }

  /**
   * §34.4 E3b 极地奇迹（5000）：全矿工已激活 _workerHp maxHp +bonus，同时 hp 按比例补齐。
   * 新加入玩家会在 _getWorkerMaxHp 返回值 +bonus（构造函数或首次伤害结算时生效）。
   */
  _applyMilestoneWorkerHpBonus(bonus) {
    if (bonus <= 0) return;
    this._milestoneWorkerHpBonus = bonus;
    for (const [, w] of Object.entries(this._workerHp || {})) {
      if (!w) continue;
      const ratio = w.maxHp > 0 ? (w.hp / w.maxHp) : 1;
      w.maxHp = w.maxHp + bonus;
      // 死亡矿工保持 isDead（避免误复活），hp 保持 0；存活则按原比例补齐
      if (!w.isDead) w.hp = Math.round(w.maxHp * ratio);
    }
    if (typeof this._broadcastWorkerHp === 'function') this._broadcastWorkerHp();
  }

  /**
   * §34.4 E3b 不朽证明（20000）：矿工将死时消费一次免死豁免 → 满血复活 + 广播 free_death_pass_triggered
   *   调用时机：所有 worker HP<=0 分支（_legendRevive 之后）—— §30.3 legend_revive 仍优先消费（阶10 免死）
   *   消费顺序：legend_revive → free_death_pass；两个互斥，不同时生效
   * @param {string} playerId
   * @returns {boolean} true=消费成功（调用方必须跳过后续死亡处理）
   */
  _consumeFreeDeathPass(playerId) {
    if (!this._freeDeathPass) return false;
    const w = this._workerHp[playerId];
    if (!w) return false;
    this._freeDeathPass = false;
    w.hp       = w.maxHp;
    w.isDead   = false;
    w.respawnAt = 0;
    this._broadcast({
      type: 'free_death_pass_triggered',
      data: {
        playerId,
        playerName: this._getPlayerName(playerId),
      }
    });
    if (typeof this._broadcastWorkerHp === 'function') this._broadcastWorkerHp();
    console.log(`[SurvivalEngine] Free death pass consumed: ${this._getPlayerName(playerId)} saved from death`);
    // §34.3 B2：记录"最戏剧性事件"候选
    this._recordDramaticEvent('free_death_pass', `${this._getPlayerName(playerId)} 触发免死豁免`);
    return true;
  }

  /**
   * §34.4 E3a 荣耀时刻：T3+ 礼物发送时广播
   *   计算：发送者当前排名 + gapToFirst + isNewFirst + overtaken
   * 注：送礼后已通过 _trackContribution 更新 contributions，本函数在 handleGift 内调用时必须
   *     使用"送礼前快照"的排名，才能准确判断 isNewFirst / overtaken。
   * @param {string} playerId
   * @param {string} playerName
   * @param {string} giftName
   * @param {number} giftTier  1~6
   * @param {Array<[string, number]>} rankingBefore  送礼前排序的 [playerId, contrib] 数组（entries）
   */
  _broadcastGloryMoment(playerId, playerName, giftName, giftTier, rankingBefore) {
    // 送礼后的当前排行（降序）
    const sortedAfter = Object.entries(this.contributions).sort(([, a], [, b]) => b - a);
    const afterIdx = sortedAfter.findIndex(([pid]) => pid === playerId);
    if (afterIdx < 0) return; // 助威者无 contributions 条目 → 不播荣耀时刻

    const rank        = afterIdx + 1;
    const firstEntry  = sortedAfter[0];
    const gapToFirst  = firstEntry ? Math.max(0, Math.round(firstEntry[1] - sortedAfter[afterIdx][1])) : 0;

    // 送礼前排行（入参）→ 对比判定 isNewFirst / overtaken
    const beforeIdx = (rankingBefore || []).findIndex(([pid]) => pid === playerId);
    const isNewFirst = (afterIdx === 0 && beforeIdx !== 0);

    // overtaken：送礼前排在当前玩家之前、送礼后排在当前玩家之后的第一个人
    let overtaken = null;
    if (beforeIdx > afterIdx && rankingBefore && rankingBefore.length > 0) {
      for (let i = afterIdx; i < beforeIdx; i++) {
        const beforeOther = rankingBefore[i];
        if (!beforeOther || beforeOther[0] === playerId) continue;
        overtaken = this.playerNames[beforeOther[0]] || beforeOther[0];
        break;
      }
    }

    this.broadcast({
      type: 'glory_moment',
      timestamp: Date.now(),
      data: {
        playerId,
        playerName:  playerName || this.playerNames[playerId] || playerId,
        giftName,
        giftTier,
        rank,
        gapToFirst,
        isNewFirst,
        overtaken,
      }
    });
  }

  /**
   * §34.4 E3c 礼物效果反馈：礼物结算 2s 后广播 gift_impact
   *   fairy_wand 设 privateOnly=true（仅 sender 显示）；其他 6 种 privateOnly=false 广播全场
   *   客户端按 privateOnly && playerId!==selfId 过滤
   * @param {string} playerId
   * @param {string} playerName
   * @param {string} giftId      GiftConfig 内部 id
   * @param {string} giftName    中文名
   * @param {string} impacts     格式化后的效果字串（含具体数字）
   * @param {boolean} [privateOnly=false]  true=fairy_wand（仅 sender 显示）
   */
  _scheduleGiftImpact(playerId, playerName, giftId, giftName, impacts, privateOnly = false) {
    if (!impacts) return;
    const t = setTimeout(() => {
      this.broadcast({
        type: 'gift_impact',
        timestamp: Date.now(),
        data: {
          playerId,
          playerName: playerName || this.playerNames[playerId] || playerId,
          giftId,
          giftName,
          impacts,
          privateOnly: !!privateOnly,
        }
      });
    }, 2000);
    this._giftImpactTimers.push(t);
  }

  /**
   * §34.4 E3c: 根据礼物 id + effects 构造 impacts 字串（格式严格按任务文档）。
   * 返回 null 表示不广播（未知礼物或无效 effects）。
   */
  _formatGiftImpactText(giftId, giftName, playerName, effects) {
    const pn = playerName || '玩家';
    const ef = effects || {};
    switch (giftId) {
      case 'love_explosion': {
        const n    = ef.monstersKilled || 0;
        const gate = ef.addGateHp || 0;
        return `消灭 ${n} 只怪物，城门 +${gate}HP`;
      }
      case 'mystery_airdrop': {
        const f = ef.addFood   || 0;
        const c = ef.addCoal   || 0;
        const o = ef.addOre    || 0;
        const g = ef.addGateHp || 0;
        return `食物+${f}，煤炭+${c}，矿石+${o}，城门+${g}HP`;
      }
      case 'donut': {
        const g = ef.addGateHp || 0;
        const f = ef.addFood   || 0;
        return `城门+${g}HP，食物+${f}`;
      }
      case 'energy_battery': {
        const h = ef.addHeat || 0;
        return `炉温+${h}℃，${pn}效率+30%`;
      }
      case 'ability_pill': {
        const dur = ef.globalEfficiencyDuration || 30;
        return `全员采矿效率+50%，持续${dur}s`;
      }
      case 'fairy_wand': {
        return `${pn}效率+5%`;
      }
      default:
        return null;
    }
  }

  // ==================== §34.4 Layer 3 组 D（E2/E5a/E5b/E6/E8/E9）====================

  /**
   * §34.4 E2 五幕：根据 seasonDay(1..7) 查询幕定义；越界归入 prologue（保险兜底）
   * @param {number} seasonDay
   * @returns {{actTag, name, startDay, endDay, endNote}}
   */
  _getActDefinitionForSeasonDay(seasonDay) {
    if (!Number.isFinite(seasonDay)) return ACT_DEFINITIONS[0];
    for (const act of ACT_DEFINITIONS) {
      if (seasonDay >= act.startDay && seasonDay <= act.endDay) return act;
    }
    return ACT_DEFINITIONS[0];
  }

  /**
   * §34.4 E2 五幕：检查 actTag 变化 → 广播 chapter_changed
   * 调用时机：_enterDay / _enterNight / _enterDayFromClock / _enterNightFromClock
   * 广播幂等（_lastActTagBroadcast 去重），同一 actTag 只广播一次
   */
  _checkActTagChange() {
    const seasonDay = this.seasonMgr ? this.seasonMgr.seasonDay : 1;
    const act = this._getActDefinitionForSeasonDay(seasonDay);
    if (this._currentActTag !== act.actTag) {
      this._currentActTag = act.actTag;
    }
    if (this._lastActTagBroadcast === act.actTag) return;
    this._lastActTagBroadcast = act.actTag;

    this.broadcast({
      type: 'chapter_changed',
      timestamp: Date.now(),
      data: {
        name:     act.name,
        actTag:   act.actTag,
        startDay: act.startDay,
        endDay:   act.endDay,
        endNote:  act.endNote,
        seasonDay,
      },
    });
    console.log(`[SurvivalEngine] chapter_changed → ${act.actTag} (${act.name}) seasonDay=${seasonDay}`);
  }

  /**
   * §34.4 E2 幕末事件：seasonDay 对应幕的最后一个夜晚 → 应用修正
   *   seasonDay 1（prologue 结束夜）：首个精英怪 → 作为"强化信号"广播 chapter_end_event
   *   seasonDay 3（act1 结束夜）   ：双 Boss（spawn 额外 1 只 boss）
   *   seasonDay 5（act2 结束夜）   ：Boss Rush mini（3 Boss 间隔 30s）
   *   seasonDay 6（act3 结束夜）   ：全怪 HP ×1.5 + 终极 Boss HP ×2（临时倍率叠加）
   *   seasonDay 7（finale）       ：沿用 §36 BossRush 结算（不重复实现）
   *
   * 调用时机：_enterNight 之后（_activeMonsters 已初始化）
   */
  _applyActEndEventIfNeeded(day) {
    if (!this.seasonMgr) return;

    // Fix 6: §34D E6（修饰符） × E2（幕末事件）冲突优先级 —— E6 优先
    //   - seasonDay=3 双 Boss × fortified（本夜无 Boss）矛盾
    //   - seasonDay=5 miniBossRush × fortified 冲突
    //   - seasonDay=6 HP×1.5 + Boss×2 × blood_moon（Boss×3）叠乘达 ×6 过高
    //   blood_moon 已重写 _activeMonsters 为 1Boss+2elite；再叠幕末改动会破坏服务端权威状态。
    //   fortified 无 Boss 条件下 spawn 额外 Boss 直接悖论。
    //   → 只要本夜修饰符是 blood_moon / fortified，幕末事件整体跳过（含广播）。
    const modId = this._currentNightModifier && this._currentNightModifier.id;
    if (modId === 'blood_moon' || modId === 'fortified') {
      console.log(`[SurvivalEngine] Act-end event skipped due to night modifier: ${modId}`);
      return;
    }

    const seasonDay = this.seasonMgr.seasonDay;
    let extra = null;

    // seasonDay 1 prologue 结束夜：首个精英怪广播（elite 已在 cfg 中按 wave 生成，这里只做全屏通告）
    if (seasonDay === 1) {
      extra = { actTag: 'prologue', event: 'first_elite', hint: '首个精英怪来袭！' };
    }
    // seasonDay 3 act1 结束夜：双 Boss（spawn 额外 1 只 boss）
    else if (seasonDay === 3) {
      this._spawnExtraBossForActEnd(day);
      extra = { actTag: 'act1', event: 'double_boss', hint: '双 Boss 同时出现！' };
    }
    // seasonDay 5 act2 结束夜：mini BossRush（3 Boss 间隔 30s；非 §36 D7，仅本幕独立效果）
    else if (seasonDay === 5) {
      this._scheduleActEndMiniBossRush(day);
      extra = { actTag: 'act2', event: 'mini_boss_rush', hint: 'Boss Rush 降临！' };
    }
    // seasonDay 6 act3 结束夜：全怪 HP ×1.5 + 终极 Boss HP ×2（即时应用到 _activeMonsters）
    else if (seasonDay === 6) {
      this._applyActEndHpMultiplier(1.5, 2.0);
      extra = { actTag: 'act3', event: 'hp_amplified', hint: '极限挑战：怪物 HP ×1.5，Boss HP ×2！' };
    }
    // seasonDay 7 终章：§36 BossRush 覆盖（不重复）
    else if (seasonDay === 7) {
      extra = { actTag: 'finale', event: 'boss_rush_finale', hint: '终章 Boss Rush（§36）' };
    }

    if (extra) {
      this.broadcast({
        type: 'chapter_end_event',
        timestamp: Date.now(),
        data: { day, seasonDay, ...extra },
      });
      console.log(`[SurvivalEngine] chapter_end_event seasonDay=${seasonDay} ${extra.actTag} ${extra.event}`);
    }
  }

  /**
   * E2 act1 结束夜（seasonDay=3）：额外 spawn 1 只 Boss（与本夜原 Boss 并存 → 双 Boss）
   */
  _spawnExtraBossForActEnd(day) {
    const cfg = getWaveConfig(day);
    if (!cfg.boss) return;
    const themeHpMult = this._themeHpMult || 1.0;
    const hpMult  = (this._monsterHpMult  || 1.0) * (this._dynamicHpMult   || 1.0) * themeHpMult;
    const hp = Math.max(1, Math.round(cfg.boss.hp * hpMult));
    const id = `b_${day}_actend_${++this._monsterIdCounter}`;
    this._activeMonsters.set(id, {
      id,
      type: 'boss',
      variant: 'normal',
      maxHp: hp,
      currentHp: hp,
      atk: cfg.boss.atk,
    });
    this.broadcast({
      type: 'boss_appeared',
      timestamp: Date.now(),
      data: { day, bossHp: hp, bossAtk: cfg.boss.atk, isActEndBoss: true },
    });
  }

  /**
   * E2 act2 结束夜（seasonDay=5）：mini BossRush（3 Boss 间隔 30s）
   * 非 §36 D7 BossRush（后者由 SeasonManager / GlobalClock 管理）
   */
  _scheduleActEndMiniBossRush(day) {
    const cfg = getWaveConfig(day);
    if (!cfg.boss) return;
    const themeHpMult = this._themeHpMult || 1.0;
    const hpMult  = (this._monsterHpMult  || 1.0) * (this._dynamicHpMult   || 1.0) * themeHpMult;
    for (let i = 0; i < 3; i++) {
      const t = setTimeout(() => {
        if (this.state !== 'night') return;
        const hp = Math.max(1, Math.round(cfg.boss.hp * hpMult));
        const id = `b_${day}_actend_${i}_${++this._monsterIdCounter}`;
        this._activeMonsters.set(id, {
          id,
          type: 'boss',
          variant: 'normal',
          maxHp: hp,
          currentHp: hp,
          atk: cfg.boss.atk,
        });
        this.broadcast({
          type: 'boss_appeared',
          timestamp: Date.now(),
          data: { day, bossHp: hp, bossAtk: cfg.boss.atk, isActEndBoss: true, rushIndex: i + 1 },
        });
      }, i * 30000);
      this._waveTimers.push(t);
    }
  }

  /**
   * E2 act3 结束夜（seasonDay=6）：即时应用 HP 乘数到 _activeMonsters
   * 对 normal/elite 应用 baseMult，对 boss 应用 bossMult（累乘到 maxHp/currentHp）
   */
  _applyActEndHpMultiplier(baseMult, bossMult) {
    for (const [, m] of this._activeMonsters) {
      if (!m) continue;
      const mult = m.type === 'boss' ? bossMult : baseMult;
      m.maxHp     = Math.max(1, Math.round(m.maxHp * mult));
      m.currentHp = Math.max(1, Math.round(m.currentHp * mult));
    }
  }

  // ==================== §34.4 E5a 智能提词器 ====================

  /**
   * §34.4 E5a：生成提词器文本
   * 返回 { text, priority } 或 null；priority: 'urgent' / 'social' / 'info'
   */
  _generateStreamerPrompt() {
    // 食物告急
    if (this.food / 2000 < 0.2) {
      return { text: '食物快没了！提醒观众刷甜甜圈！', priority: 'urgent' };
    }
    // 夜晚 Boss 血少
    if (this.state === 'night' && this._isBossAlive()) {
      let minBossRatio = 1;
      for (const m of this._activeMonsters.values()) {
        if (m && m.type === 'boss') {
          const r = m.maxHp > 0 ? (m.currentHp / m.maxHp) : 1;
          if (r < minBossRatio) minBossRatio = r;
        }
      }
      if (minBossRatio < 0.3) {
        return { text: 'Boss 快死了！号召冲锋！发 6！', priority: 'urgent' };
      }
    }
    // Top2 接近（差 < 300）
    const sorted = Object.entries(this.contributions).sort(([, a], [, b]) => b - a);
    if (sorted.length >= 2) {
      const [pid1, c1] = sorted[0];
      const [pid2, c2] = sorted[1];
      const gap = Math.max(0, Math.round(c1 - c2));
      if (gap < 300) {
        const nameA = this.playerNames[pid1] || pid1;
        const nameB = this.playerNames[pid2] || pid2;
        return { text: `${nameA} 和 ${nameB} 只差 ${gap}！引导他们！`, priority: 'social' };
      }
    }
    // 夜晚矿石修城门 info 提示（附加性信息，不是高优先级）
    if (this.state === 'night' && this.ore > 0 && this.gateHp < this.gateMaxHp) {
      const mult = GATE_AUTO_REPAIR_MULT[Math.max(0, this.gateLevel - 1)] || 1.0;
      const repair = Math.floor(5 * mult);
      return { text: `矿石自动修复城门 +${repair}HP/2s`, priority: 'info' };
    }
    return null;
  }

  /**
   * §34.4 E5a：启动 / 重启提词器定时器（每 10s 调用一次）
   * TODO: 接入 _roomCreatorId 单播能力后按主播身份过滤；当前版本广播，客户端按 isRoomCreator=true 过滤
   */
  _startStreamerPromptTimer() {
    if (this._streamerPromptTimer) {
      clearInterval(this._streamerPromptTimer);
      this._streamerPromptTimer = null;
    }
    this._streamerPromptTimer = setInterval(() => {
      if (this.state !== 'day' && this.state !== 'night' && this.state !== 'recovery') return;
      const prompt = this._generateStreamerPrompt();
      if (!prompt) return;
      this.broadcast({
        type: 'streamer_prompt',
        timestamp: Date.now(),
        data: {
          text:      prompt.text,
          priority:  prompt.priority,
          // TODO: 客户端按 isRoomCreator=true 过滤，仅主播 UI 展示
          recipient: 'broadcaster_only',
        },
      });
    }, STREAMER_PROMPT_INTERVAL_MS);
  }

  // ==================== §34.4 E5b 夜战报告 ====================

  /**
   * §34.4 E5b：夜晚开始时初始化统计结构
   */
  _initNightStats() {
    // 统计夜晚开始时的"存活矿工总数"——用 _workerHp（_initWorkerHp 已设置）
    let totalAtStart = 0;
    for (const pid of Object.keys(this._workerHp || {})) {
      const w = this._workerHp[pid];
      if (w && !w.isDead) totalAtStart++;
    }
    this._nightStats = {
      monstersKilled:     0,
      bossDefeated:       false,
      killsPerPlayer:     {},           // { pid: count }
      topGift:            null,         // { tier, playerId, playerName, giftName }
      minGateHpPct:       1.0,          // 夜晚内城门最低 HP 百分比
      totalWorkersAtStart: totalAtStart,
      nightStartedAt:     Date.now(),
    };
  }

  /**
   * §34.4 E5b：累计怪物击杀（由 monster_died 路径调用）
   * @param {string} killerId 可能为 'gate_frost_pulse'/'gate_thorns'/玩家 secOpenId
   */
  _trackNightKill(killerId, monsterType) {
    if (!this._nightStats) return;
    this._nightStats.monstersKilled += 1;
    if (monsterType === 'boss') {
      this._nightStats.bossDefeated = true;
    }
    // 仅玩家击杀计入 MVP（过滤城门反伤/冲击波）
    if (!killerId || killerId === 'gate_frost_pulse' || killerId === 'gate_thorns') return;
    this._nightStats.killsPerPlayer[killerId] = (this._nightStats.killsPerPlayer[killerId] || 0) + 1;
  }

  /**
   * §34.4 E5b：记录本夜最佳援助（tier 最高的礼物发送者）
   * @param {string} playerId
   * @param {string} playerName
   * @param {string} giftName
   * @param {number} tier 1~6
   */
  _trackNightGift(playerId, playerName, giftName, tier) {
    if (!this._nightStats || this.state !== 'night') return;
    const prev = this._nightStats.topGift;
    if (!prev || tier > prev.tier) {
      this._nightStats.topGift = { tier, playerId, playerName: playerName || playerId, giftName };
    }
  }

  /**
   * §34.4 E5b：每次城门 HP 变化时调用（_broadcastResourceUpdate / _decayResources / gate_quickpatch 均可挂）
   * 此处采取简化策略：在 _broadcastResourceUpdate 入口处调用一次，成本 O(1)
   */
  _trackNightGateHp() {
    if (!this._nightStats || this.state !== 'night') return;
    const pct = this.gateMaxHp > 0 ? this.gateHp / this.gateMaxHp : 1;
    if (pct < this._nightStats.minGateHpPct) this._nightStats.minGateHpPct = pct;
  }

  /**
   * §34.4 E5b：夜→昼转换时广播 night_report 并清理统计
   * 由 _endNight 调用（在 _enterDay 之前）
   */
  _broadcastNightReportAndClear() {
    const s = this._nightStats;
    if (!s) return;

    // MVP：从 killsPerPlayer 里找杀怪最多者
    let mvpPlayerId = null, mvpKills = 0;
    for (const pid of Object.keys(s.killsPerPlayer)) {
      if (s.killsPerPlayer[pid] > mvpKills) {
        mvpKills   = s.killsPerPlayer[pid];
        mvpPlayerId = pid;
      }
    }
    const mvpPlayerName = mvpPlayerId ? (this.playerNames[mvpPlayerId] || mvpPlayerId) : null;

    // 存活率：夜晚结束时（此时 _workerHp 仍保留，_endNight 调用顺序决定）
    let aliveNow = 0;
    for (const pid of Object.keys(this._workerHp || {})) {
      const w = this._workerHp[pid];
      if (w && !w.isDead) aliveNow++;
    }
    const survivalRate = s.totalWorkersAtStart > 0
      ? (aliveNow / s.totalWorkersAtStart)
      : 1.0;

    this.broadcast({
      type: 'night_report',
      timestamp: Date.now(),
      data: {
        day:              this.currentDay,
        monstersKilled:   s.monstersKilled,
        bossDefeated:     s.bossDefeated,
        mvpPlayerId,
        mvpPlayerName,
        mvpKills,
        topGifterName:    s.topGift ? s.topGift.playerName : null,
        topGiftName:      s.topGift ? s.topGift.giftName    : null,
        closestCallHpPct: +s.minGateHpPct.toFixed(3),
        survivalRate:     +survivalRate.toFixed(3),
        nightModifierId:  this._currentNightModifier ? this._currentNightModifier.id : 'normal',
      },
    });
    console.log(`[SurvivalEngine] night_report day=${this.currentDay} kills=${s.monstersKilled} boss=${s.bossDefeated} mvp=${mvpPlayerName || '-'} survival=${survivalRate.toFixed(2)}`);
    this._nightStats = null;
  }

  // ==================== §34.4 E6 夜间修饰符 ====================

  /**
   * §34.4 E6：按 seasonDay 过滤池 + 加权随机选一个 modifier
   * 若 seasonMgr 未注入，fallback 用 currentDay
   */
  _pickNightModifier() {
    const seasonDay = this.seasonMgr ? this.seasonMgr.seasonDay : this.currentDay;
    const day       = this.currentDay || 1;
    // 过滤：minDay <= max(seasonDay, fortressDay, currentDay)——保险起见取 currentDay（以便纯服务器测试时也能触发高阶修饰符）
    // 策划案字面为 day >= minDay，使用 currentDay 最接近旧版语义
    const pool = NIGHT_MODIFIERS.filter(m => day >= m.minDay && seasonDay >= 1);
    if (pool.length === 0) return NIGHT_MODIFIERS[0]; // 兜底 normal
    const totalWeight = pool.reduce((s, m) => s + m.weight, 0);
    let r = Math.random() * totalWeight;
    for (const m of pool) {
      r -= m.weight;
      if (r <= 0) return m;
    }
    return pool[pool.length - 1];
  }

  /**
   * §34.4 E6：应用夜间修饰符效果（服务端权威）
   * _enterNight 调用：在 _initActiveMonsters 之后（已有 _activeMonsters）
   */
  _applyNightModifier(mod, day) {
    if (!mod || mod.id === 'normal') return;

    switch (mod.id) {
      case 'blood_moon': {
        // 覆盖正常波次：清空 _activeMonsters，仅生成 1 Boss + 2 elite 护卫（§31 变体不出现 → 由 _spawnWave 守卫层处理）
        // PM 决策：直接改写 _activeMonsters（保留原 Boss 或新建 Boss × 3HP）
        const cfg = getWaveConfig(day);
        this._activeMonsters.clear();
        if (cfg.boss) {
          const bossHp = Math.max(1, Math.round(cfg.boss.hp * MODIFIER_BLOOD_MOON_BOSS_HP_MULT));
          const id = `b_${day}_bm_${++this._monsterIdCounter}`;
          this._activeMonsters.set(id, {
            id, type: 'boss', variant: 'normal',
            maxHp: bossHp, currentHp: bossHp, atk: cfg.boss.atk,
          });
          this.broadcast({
            type: 'boss_appeared',
            timestamp: Date.now(),
            data: { day, bossHp, bossAtk: cfg.boss.atk, isBloodMoon: true },
          });
        }
        if (cfg.elite) {
          const eliteHp = Math.max(1, Math.round(cfg.elite.hp * (this._monsterHpMult || 1.0) * (this._dynamicHpMult || 1.0)));
          for (let i = 0; i < MODIFIER_BLOOD_MOON_ELITE_COUNT; i++) {
            const id = `e_${day}_bm_${++this._monsterIdCounter}`;
            this._activeMonsters.set(id, {
              id, type: 'elite', variant: 'normal',
              maxHp: eliteHp, currentHp: eliteHp, atk: cfg.elite.atk,
            });
          }
        }
        break;
      }
      case 'polar_night': {
        // 夜晚持续 180s（覆盖原 nightDuration；remainingTime 已在 _enterNight 设置为 nightDuration，此处重写）
        this.remainingTime = MODIFIER_POLAR_NIGHT_DURATION_SEC;
        // §31 变体频率 × 1.5 + 属性 × 1.2：polar_* 倍率由 _initActiveMonsters 后已生效的 _dynamicHpMult 再叠加一层
        // PM 决策：MVP 以"属性 × 1.2"一次性应用到已有 _activeMonsters（频率由客户端感知；§31 本身为按 wave 决定的）
        for (const [, m] of this._activeMonsters) {
          if (!m) continue;
          m.maxHp     = Math.max(1, Math.round(m.maxHp * MODIFIER_POLAR_VARIANT_STAT_MULT));
          m.currentHp = Math.max(1, Math.round(m.currentHp * MODIFIER_POLAR_VARIANT_STAT_MULT));
          if (typeof m.atk === 'number') m.atk = Math.max(1, Math.round(m.atk * MODIFIER_POLAR_VARIANT_STAT_MULT));
        }
        break;
      }
      case 'fortified': {
        // 全矿工 HP +30（_initWorkerHp 已完成，此处遍历 _workerHp 调增 maxHp 并按比例补齐）
        for (const [, w] of Object.entries(this._workerHp || {})) {
          if (!w) continue;
          const ratio = w.maxHp > 0 ? (w.hp / w.maxHp) : 1;
          w.maxHp = w.maxHp + MODIFIER_FORTIFIED_HP_BONUS;
          if (!w.isDead) w.hp = Math.round(w.maxHp * ratio);
        }
        this._broadcastWorkerHp();
        // 无 Boss：清除当前 Boss（若已 spawn）→ 从 _activeMonsters 删除所有 boss
        for (const [id, m] of this._activeMonsters) {
          if (m && m.type === 'boss') this._activeMonsters.delete(id);
        }
        break;
      }
      case 'frenzy': {
        // 波次间隔 -40% + 每波数量减半：MVP 直接把 _dynamicCountMult × 0.5 暂存（_exitNight 恢复）
        //   间隔通过新增 "frenzy 修饰" 旗标由 _scheduleNightWaves 消费；MVP 简化为仅数量减半 + 直接提前重排
        // 保存原值由 _exitNight 恢复
        this._modifierSavedDynamicCountMult = this._dynamicCountMult;
        this._modifierFrenzyIntervalMult    = MODIFIER_FRENZY_INTERVAL_MULT;
        this._dynamicCountMult              = (this._dynamicCountMult || 1.0) * MODIFIER_FRENZY_COUNT_MULT;
        break;
      }
      case 'hunters': {
        // 玩家对 Boss 伤害 × 2：_handleAttack 路径读取 _currentNightModifier.id==='hunters' 即乘 2
        // 无状态存储，仅标记即可（modifier 存在 _currentNightModifier）
        break;
      }
      case 'blizzard_night': {
        // 全矿工 -2HP/10s 冻伤：_tick 每 10s 调用一次 _applyBlizzardTick
        this._blizzardLastTickAt = Date.now();
        break;
      }
    }
  }

  /**
   * §34.4 E6：撤销 modifier 临时效果（_exitNight 调用；仅还原需要恢复的字段）
   */
  _clearNightModifier() {
    if (!this._currentNightModifier) return;
    const mod = this._currentNightModifier;
    switch (mod.id) {
      case 'fortified':
        // HP 加成夜晚结束自动失效：遍历 _workerHp，把 maxHp 回退，hp 按比例缩回
        for (const [, w] of Object.entries(this._workerHp || {})) {
          if (!w) continue;
          const oldMax = w.maxHp;
          const newMax = Math.max(1, oldMax - MODIFIER_FORTIFIED_HP_BONUS);
          if (oldMax !== newMax) {
            const ratio = oldMax > 0 ? w.hp / oldMax : 1;
            w.maxHp = newMax;
            if (!w.isDead) w.hp = Math.min(newMax, Math.round(newMax * ratio));
          }
        }
        break;
      case 'frenzy':
        if (this._modifierSavedDynamicCountMult != null) {
          this._dynamicCountMult = this._modifierSavedDynamicCountMult;
          this._modifierSavedDynamicCountMult = null;
        }
        // Fix 2/5: 回归默认 1.0（非 null），便于 _scheduleNightWaves 乘数安全取用
        this._modifierFrenzyIntervalMult = 1.0;
        break;
      case 'blizzard_night':
        this._blizzardLastTickAt = 0;
        break;
      // blood_moon / polar_night / hunters 的倍率作用于已 spawn 的 _activeMonsters 或走读流分支，无需显式回退
    }
    this._currentNightModifier = null;
  }

  /**
   * §34.4 E6 blizzard_night：每 10s 全矿工 -2HP（由 _tick 1Hz 分支调用）
   */
  _tickBlizzardNightIfActive() {
    if (!this._currentNightModifier || this._currentNightModifier.id !== 'blizzard_night') return;
    if (this.state !== 'night') return;
    const now = Date.now();
    if (this._blizzardLastTickAt && (now - this._blizzardLastTickAt) < MODIFIER_BLIZZARD_TICK_SEC * 1000) return;
    this._blizzardLastTickAt = now;
    // 所有存活矿工 -2HP（不走 block，直接扣）
    for (const pid of Object.keys(this._workerHp || {})) {
      const w = this._workerHp[pid];
      if (!w || w.isDead) continue;
      w.hp = Math.max(0, w.hp - MODIFIER_BLIZZARD_DAMAGE);
      if (w.hp <= 0) {
        // 走正常死亡路径（阶10 免死 / free_death_pass 在 _damageWorker 里消费；此处已越过 _damageWorker 但仍需同一结果）
        // MVP 简化：复用 _damageWorker 的副作用——模拟扣到 0 并走死亡判定
        // 直接设置 dead + 广播（避免重复 HP 逻辑）
        // 阶10 免死 / free_death_pass
        if (this._getWorkerTier(pid) >= 10 && !this._legendReviveUsed[pid]) {
          this._legendReviveUsed[pid] = true;
          const maxHp = this._getWorkerMaxHp(pid);
          w.hp = Math.ceil(maxHp * 0.25);
          w.isDead = false;
          w.respawnAt = 0;
          this._broadcast({
            type: 'legend_revive_triggered',
            data: { playerId: pid, playerName: this._getPlayerName(pid) }
          });
          continue;
        }
        if (this._consumeFreeDeathPass(pid)) continue;
        w.isDead = true;
        const respawnSec = this._getWorkerRespawnSec(pid);
        w.respawnAt = Date.now() + respawnSec * 1000;
        this._broadcast({
          type: 'worker_died',
          data: { playerId: pid, respawnAt: w.respawnAt, cause: 'blizzard' },
        });
      }
    }
    this._broadcastWorkerHp();
  }

  /**
   * §34.4 E6 blizzard_night：T4 能量电池全矿工治愈 +20HP（由 handleGift energy_battery case 调用）
   */
  _applyBlizzardT4Heal() {
    if (!this._currentNightModifier || this._currentNightModifier.id !== 'blizzard_night') return;
    if (this.state !== 'night') return;
    let healed = 0;
    for (const pid of Object.keys(this._workerHp || {})) {
      const w = this._workerHp[pid];
      if (!w || w.isDead) continue;
      if (w.hp < w.maxHp) {
        w.hp = Math.min(w.maxHp, w.hp + MODIFIER_BLIZZARD_T4_HEAL);
        healed++;
      }
    }
    if (healed > 0) this._broadcastWorkerHp();
  }

  // ==================== §34.4 E8 参与感唤回 ====================

  /**
   * §34.4 E8：启动 / 重启定时器（每 300s 推送一次）
   * TODO: 单播能力未接入 → 广播，客户端按 playerId === self 过滤
   */
  _startEngagementReminderTimer() {
    if (this._engagementInterval) { clearInterval(this._engagementInterval); this._engagementInterval = null; }
    this._engagementInterval = setInterval(() => {
      if (this.state !== 'day' && this.state !== 'night' && this.state !== 'recovery') return;
      // 排序（降序）
      const sorted = Object.entries(this.contributions).sort(([, a], [, b]) => b - a);
      if (sorted.length === 0) return;
      const top3 = sorted.slice(0, 3);
      const top3Min = top3.length === 3 ? top3[2][1] : 0;

      // 打包成单个 engagement_reminder 批量消息（对所有 contrib > 0 玩家）
      const entries = [];
      for (let i = 0; i < sorted.length; i++) {
        const [pid, contrib] = sorted[i];
        if (contrib <= 0) continue;
        const rank = i + 1;
        const gapToTop3 = (rank <= 3) ? 0 : Math.max(0, Math.round(top3Min - contrib));
        entries.push({
          playerId:       pid,
          rank,
          gapToTop3,
          currentContrib: Math.round(contrib),
        });
      }
      if (entries.length === 0) return;
      this.broadcast({
        type: 'engagement_reminder',
        timestamp: Date.now(),
        data: {
          // TODO: 后端单播能力落地后改为单播；当前版本广播，客户端按 playerId === self 过滤
          entries,
        },
      });
    }, ENGAGEMENT_REMINDER_INTERVAL_MS);
  }

  // ==================== §34.4 E9 赛季/周期间难度切换 ====================

  /**
   * §34.4 E9：C→S handler，主播请求下一夜/下一赛季切换难度
   * @param {string} playerId
   * @param {object} data { difficulty: 'easy'|'normal'|'hard'|'nightmare', applyAt: 'next_night'|'next_season' }
   */
  handleChangeDifficulty(playerId, data) {
    // TODO: _roomCreatorId 鉴权未注入 → 放开（与 §24.4/§37/§39 一致）
    //   if (this.room && this.room.roomCreatorOpenId && playerId !== this.room.roomCreatorOpenId) {
    //     this._broadcast({ type: 'change_difficulty_failed', data: { reason: 'not_room_creator' } });
    //     return;
    //   }
    const diff    = (data && data.difficulty) || '';
    const applyAt = (data && data.applyAt)    || '';
    // Fix 7: §34D E9 —— `nightmare` preset 策划案未定具体参数,_softApplyDifficulty 会静默回退 hard,
    //   客户端与主播不会被告知差异 → 直接拒绝该值,强制 supported = ['easy','normal','hard'],
    //   与 ChangeDifficultyData.difficulty 文档字段声明口径对齐。
    const SUPPORTED_DIFFS = ['easy', 'normal', 'hard'];
    const validDiff    = SUPPORTED_DIFFS.includes(diff);
    const validApplyAt = (applyAt === 'next_night' || applyAt === 'next_season');
    if (!validDiff) {
      this._broadcast({
        type: 'change_difficulty_failed',
        data: { reason: 'invalid_difficulty', supported: SUPPORTED_DIFFS, difficulty: diff, applyAt },
      });
      return;
    }
    if (!validApplyAt) {
      this._broadcast({
        type: 'change_difficulty_failed',
        data: { reason: 'invalid_args', difficulty: diff, applyAt },
      });
      return;
    }
    this._pendingDifficulty = { difficulty: diff, applyAt };
    this._broadcast({
      type: 'change_difficulty_accepted',
      data: { difficulty: diff, applyAt },
    });
    console.log(`[SurvivalEngine] change_difficulty pending: ${diff} @ ${applyAt}`);
  }

  /**
   * §34.4 E9：软难度切换（中途切换，不重置资源 / 不改 currentDay / 不改 gateHp）
   * 仅更新：_difficulty / _monsterHpMult / _monsterCntMult / _poolNightBase / dayDuration / nightDuration
   *         / foodDecayDay / foodDecayNight / tempDecayDay / tempDecayNight / coalBurnTicks / totalDays
   * 不更新：food / coal / ore / gateHp / gateMaxHp（_applyDifficulty 里的 resource reset 仅开局 startGame 时合适）
   */
  _softApplyDifficulty(difficulty) {
    const presets = {
      easy:   { hpMult: 0.6, cntMult: 0.6, decayMult: 0.7, coalBurnTicks: 10, totalDays: 30, poolNightBase: 300, dayDuration: 120, nightDuration: 120 },
      normal: { hpMult: 1.0, cntMult: 1.0, decayMult: 1.0, coalBurnTicks: 7,  totalDays: 50, poolNightBase: 500, dayDuration: 120, nightDuration: 120 },
      hard:   { hpMult: 1.5, cntMult: 1.5, decayMult: 1.5, coalBurnTicks: 5,  totalDays: 40, poolNightBase: 800, dayDuration: 120, nightDuration: 120 },
    };
    const p = presets[difficulty] || presets.normal;
    this._difficulty       = difficulty;
    this._monsterHpMult    = p.hpMult;
    this._monsterCntMult   = p.cntMult;
    this._poolNightBase    = p.poolNightBase;
    this.totalDays         = p.totalDays;
    this.dayDuration       = p.dayDuration;
    this.nightDuration     = p.nightDuration;
    this.foodDecayDay      = (this.config.foodDecayDay   ?? 1.0)  * p.decayMult;
    this.foodDecayNight    = (this.config.foodDecayNight ?? 1.0)  * p.decayMult;
    this.tempDecayDay      = (this.config.tempDecayDay   ?? 0.15) * p.decayMult;
    this.tempDecayNight    = (this.config.tempDecayNight ?? 0.40) * p.decayMult;
    this.coalBurnTicks     = p.coalBurnTicks || 10;
    console.log(`[Engine] 软难度切换: ${difficulty} | HP×${p.hpMult} 数量×${p.cntMult} 衰减×${p.decayMult} （不重置资源）`);
  }

  /**
   * §34.4 E9：在 _enterNight 消费 applyAt='next_night' 的 pending
   * 使用 _softApplyDifficulty，不重置资源；nightmare MVP 回退 hard
   */
  _consumePendingDifficultyOnNight() {
    if (!this._pendingDifficulty) return;
    if (this._pendingDifficulty.applyAt !== 'next_night') return;
    const diff = this._pendingDifficulty.difficulty;
    const effectiveDiff = (diff === 'nightmare') ? 'hard' : diff;
    this._softApplyDifficulty(effectiveDiff);
    const applied = this._pendingDifficulty;
    this._pendingDifficulty = null;
    this.broadcast({
      type: 'difficulty_changed',
      timestamp: Date.now(),
      data: { difficulty: applied.difficulty, appliedDifficulty: effectiveDiff, applyAt: 'next_night' },
    });
    console.log(`[SurvivalEngine] difficulty applied (next_night): ${diff} → ${effectiveDiff}`);
  }

  /**
   * §34.4 E9：由 _enterDay 检测新赛季 D1 时调用；消费 applyAt='next_season' 的 pending
   * 同样使用 _softApplyDifficulty
   */
  onSeasonStart(newSeasonId) {
    if (!this._pendingDifficulty) return;
    if (this._pendingDifficulty.applyAt !== 'next_season') return;
    const diff = this._pendingDifficulty.difficulty;
    const effectiveDiff = (diff === 'nightmare') ? 'hard' : diff;
    this._softApplyDifficulty(effectiveDiff);
    const applied = this._pendingDifficulty;
    this._pendingDifficulty = null;
    this.broadcast({
      type: 'difficulty_changed',
      timestamp: Date.now(),
      data: {
        difficulty: applied.difficulty, appliedDifficulty: effectiveDiff,
        applyAt: 'next_season', seasonId: newSeasonId,
      },
    });
    console.log(`[SurvivalEngine] difficulty applied (next_season ${newSeasonId}): ${diff} → ${effectiveDiff}`);
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

  /** 等级决定的最大HP（基础100 + 每级+3 + §34.4 E3b miracle 里程碑 +bonus） */
  _getWorkerMaxHp(playerId) {
    const lv = this._playerLevel[playerId] || 1;
    return 100 + (lv - 1) * 3 + (this._milestoneWorkerHpBonus || 0);
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

    // §36.12 老用户豁免条件 1（_lifetimeContrib ≥ 50000）：只对房间创建者评估，避免遍历 200 人
    //   仅在 contrib 刚跨过 50000 阈值时评估一次；失败时 evaluateVeteran 内部会 dedup（集合去重）
    if (playerId && this.room && playerId === this.room.roomCreatorOpenId
        && currentLc < 50000 && this._lifetimeContrib[playerId] >= 50000) {
      this._evaluateVeteranForCreator('lifetime_contrib');
    }

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

    // §34.4 E3b 协作里程碑：仅正向 finalAmount 累加（§30 catchUpMult 乘进 _lifetimeContrib，
    //   但 _totalContribution 对齐 contributions 水位 → 进度条与实时榜口径一致，不重复×3）
    //   负值场景（reset/惩罚等）不计入；0 也跳过避免无谓调用 _checkCoopMilestones
    if (finalAmount > 0) {
      this._totalContribution += finalAmount;
      this._checkCoopMilestones();
    }

    // §34.3 B10a：白天贡献累计到 _dayStats（efficiency_race Top3 + dayTotal 数据源）
    //   仅正 finalAmount 且 state=='day'；_dayStats 在 _enterDay 初始化，_enterNight 清零
    //   _applyWorkEffect 里已单独计 +1，此处兜底其他路径（礼物/攻击/666 等）；
    //   为避免 _applyWorkEffect 重复累计，这里排除 source='barrage'（工作指令已在 _applyWorkEffect 里计了 +1）
    if (finalAmount > 0 && this.state === 'day' && this._dayStats && source !== 'barrage') {
      this._dayStats.contributions[playerId] = (this._dayStats.contributions[playerId] || 0) + finalAmount;
      this._dayStats.totalDay += finalAmount;
    }

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
    // §34.4 E5b：elite_raid 超时消失不算击杀，不计 monstersKilled（killerId 空）
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
      // §34.4 E5b：流星雨（§24.4 轮盘效果）计入 monstersKilled 但不计玩家杀数（killerId 空字符串 → 被过滤）
      this._trackNightKill('', m ? m.type : 'normal');
      this._broadcast({
        type: 'monster_died',
        data: { monsterId: mid, monsterType: m.type, killerId: '', reason: 'meteor_shower' },
      });
      // §31 guard/summoner 死亡后置钩子
      this._postMonsterDeathHooks(mid, m.type, m.variant);
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

      // §34.4 E3b immortal 里程碑（20000）：探险死亡也消耗一次免死豁免
      //   消耗成功 → 跳过死亡标记与广播（玩家视角：探险归来满血）
      if (this._consumeFreeDeathPass(playerId)) {
        this._broadcastWorkerHp();
      } else {

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
      } // end: !_consumeFreeDeathPass（§34.4 E3b immortal）
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

    // §34.4 E3b immortal 里程碑（20000）：夜晚兜底死亡也消耗一次免死豁免
    //   消耗成功 → 玩家满血归队，仍广播 expedition_returned 但不广播 worker_died
    if (this._consumeFreeDeathPass(playerId)) {
      this._broadcast({
        type: 'expedition_returned',
        data: {
          playerId,
          expeditionId: exp.expeditionId,
          outcome: { type: 'safe', resources: null, contributions: 0, died: false },
        },
      });
      this._broadcastWorkerHp();
      console.log(`[SurvivalEngine] expedition night KIA saved by free_death_pass: ${this._getPlayerName(playerId)} id=${exp.expeditionId}`);
      return;
    }

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
    // §16 恢复期定时器（_enterSettlement 的 30s UI 定时器 / _enterRecovery 的 120s 定时器共用同一句柄；Fix A：8s→30s）
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

    // §10 Lv6 寒冰冲击波定时器
    this._stopFrostPulseTimer();

    // §34.4 组 D：清 E5a 提词器 + E8 参与感唤回定时器
    if (this._streamerPromptTimer) { clearInterval(this._streamerPromptTimer); this._streamerPromptTimer = null; }
    if (this._engagementInterval)  { clearInterval(this._engagementInterval);  this._engagementInterval  = null; }

    // §34.3 B2：结算 30s 倒计时句柄（streamer_skip_settlement 亦会清；Fix A：原 8s → 30s）
    if (this._settleTimerHandle) { clearTimeout(this._settleTimerHandle); this._settleTimerHandle = null; }
  }
}

module.exports = SurvivalGameEngine;
