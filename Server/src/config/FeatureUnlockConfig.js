/**
 * FeatureUnlockConfig - §36.12 分时段功能解锁
 *
 * 职责：
 * 1. 维护 FEATURE_UNLOCK_DAY 配置（按 seasonDay 解锁策略功能）
 * 2. isFeatureUnlocked(room, featureId) — 查当前房间是否解锁（含老用户豁免）
 * 3. getNewlyUnlockedFeatures(oldSeasonDay, newSeasonDay) — seasonDay 递增时新解锁的 feature 集合（给 world_clock_tick 用）
 * 4. getUnlockedFeatures(seasonDay) — 截至当前 seasonDay 已解锁功能全集（给 season_state 用）
 *
 * 设计动机：抽到独立 config 文件避免 SurvivalGameEngine ↔ BuildingSystem ↔ ExpeditionSystem
 * 之间的循环 require（各模块都要查 isFeatureUnlocked 但不能跨引擎）。
 *
 * PM 决策（MVP v1.27）：
 * - 老用户豁免字段 `room.isVeteran` 未注入时按"非老用户"处理
 * - 老用户豁免的持久化实现由 VeteranTracker.js 提供（本文件仅消费 isVeteran 标记）
 */

// §36.12 功能解锁表（键 = featureId，值 = { minDay: 最小赛季日 }）
// 字段 maxLv 仅用于 gate_upgrade_basic/high 的分流（不进入统一解锁逻辑）
const FEATURE_UNLOCK_DAY = {
  gate_upgrade_basic: { minDay: 1, maxLv: 4 },
  gate_upgrade_high:  { minDay: 4, maxLv: 6 },
  roulette:           { minDay: 1 },
  broadcaster_boost:  { minDay: 2 },
  building:           { minDay: 3 },
  expedition:         { minDay: 5 },
  supporter_mode:     { minDay: 6 },
  tribe_war:          { minDay: 7 },
  shop:               { minDay: 2 },
};

// 固定遍历顺序（给 getUnlockedFeatures / getNewlyUnlockedFeatures 做稳定输出）
const FEATURE_IDS_IN_DAY_ORDER = [
  'gate_upgrade_basic',
  'roulette',
  'broadcaster_boost',
  'shop',
  'building',
  'gate_upgrade_high',
  'expedition',
  'supporter_mode',
  'tribe_war',
];

/**
 * 查询当前房间是否已解锁指定功能
 * @param {object} room 房间对象（SurvivalRoom）——本函数仅读取 creator.isVeteran / seasonMgr.seasonDay
 *                      其他字段不访问，方便调用方只传必要上下文
 * @param {string} featureId FEATURE_UNLOCK_DAY 的键
 * @returns {boolean}
 */
function isFeatureUnlocked(room, featureId) {
  const cfg = FEATURE_UNLOCK_DAY[featureId];
  if (!cfg) return true;                         // 未注册 featureId 默认放开（容错）

  // 老用户豁免：room.creator.isVeteran || room.isVeteran
  if (room) {
    if (room.creator && room.creator.isVeteran) return true;
    if (room.isVeteran) return true;
  }

  // seasonDay 检查（seasonMgr 未注入时按 seasonDay=1）
  const seasonDay = _getSeasonDay(room);
  return seasonDay >= cfg.minDay;
}

/**
 * 返回 seasonDay 从 oldSeasonDay 递增到 newSeasonDay 时"新解锁"的 featureId 列表
 * （即 minDay ∈ (oldSeasonDay, newSeasonDay]）
 *
 * 保证：
 * - oldSeasonDay >= newSeasonDay → 返回 []
 * - newSeasonDay 环绕到 1（赛季切换 D7→D1）→ 返回 []（重新开始时功能锁定也重置）
 *
 * @param {number} oldSeasonDay 上一赛季日（切换前）
 * @param {number} newSeasonDay 新赛季日（切换后）
 * @returns {string[]}
 */
function getNewlyUnlockedFeatures(oldSeasonDay, newSeasonDay) {
  if (!Number.isFinite(oldSeasonDay) || !Number.isFinite(newSeasonDay)) return [];
  // 环绕 / 降序 → 无新解锁
  if (newSeasonDay <= oldSeasonDay) return [];

  const out = [];
  for (const fid of FEATURE_IDS_IN_DAY_ORDER) {
    const cfg = FEATURE_UNLOCK_DAY[fid];
    if (!cfg) continue;
    if (cfg.minDay > oldSeasonDay && cfg.minDay <= newSeasonDay) {
      out.push(fid);
    }
  }
  return out;
}

/**
 * 返回截至当前 seasonDay 已解锁功能全集（给 season_state.unlockedFeatures 用）
 * @param {number} seasonDay
 * @returns {string[]}
 */
function getUnlockedFeatures(seasonDay) {
  if (!Number.isFinite(seasonDay)) seasonDay = 1;
  const out = [];
  for (const fid of FEATURE_IDS_IN_DAY_ORDER) {
    const cfg = FEATURE_UNLOCK_DAY[fid];
    if (!cfg) continue;
    if (seasonDay >= cfg.minDay) out.push(fid);
  }
  return out;
}

/** 内部：从 room 读 seasonDay（容错未注入场景） */
function _getSeasonDay(room) {
  if (!room) return 1;
  if (room.seasonMgr && typeof room.seasonMgr.seasonDay === 'number') return room.seasonMgr.seasonDay;
  if (room.survivalEngine && room.survivalEngine.seasonMgr && typeof room.survivalEngine.seasonMgr.seasonDay === 'number') {
    return room.survivalEngine.seasonMgr.seasonDay;
  }
  if (typeof room.seasonDay === 'number') return room.seasonDay;
  return 1;
}

module.exports = {
  FEATURE_UNLOCK_DAY,
  FEATURE_IDS_IN_DAY_ORDER,
  isFeatureUnlocked,
  getNewlyUnlockedFeatures,
  getUnlockedFeatures,
};
