/**
 * VeteranTracker - §36.12 老用户豁免追踪
 *
 * 职责：
 * 1. 维护 _veteranStreamers: Set<openId>（首次达标后永久保留）
 * 2. 维护 _maxSeasonDayAttended: Map<openId, Map<seasonId, int>>（仅保留最近 10 赛季）
 * 3. evaluateVeteran() — 三条件任一满足即写入豁免集 + 推送 veteran_unlocked
 * 4. markSeasonAttendance() — 玩家进房/赛季推进时更新最大 seasonDay
 * 5. isVeteran(openId) — 查集
 *
 * 三条件（§36.12）：
 * 1. _lifetimeContrib[openId] >= 50000
 * 2. maxFortressDay >= 50（per-room 最高）
 * 3. 任一 seasonId 的 _maxSeasonDayAttended[openId][seasonId] >= 5
 *
 * 持久化：
 * - save() / loadSnapshot(data) 供 RoomPersistence 或 index.js 用
 * - MVP 单节点内存，跨进程重启不保留（TODO 接入全局 JSON 文件）
 */

const MAX_SEASONS_TO_KEEP = 10;

// 三条件阈值
const THRESHOLD_LIFETIME_CONTRIB = 50000;
const THRESHOLD_MAX_FORTRESS_DAY = 50;
const THRESHOLD_MAX_SEASON_DAY   = 5;

class VeteranTracker {
  constructor() {
    this._veteranStreamers = new Set();                    // openId 集合
    this._maxSeasonDayAttended = new Map();                // openId → Map<seasonId, int>
  }

  // ==================== 查询 ====================

  /** 是否为老用户（已达标豁免）*/
  isVeteran(openId) {
    if (!openId) return false;
    return this._veteranStreamers.has(openId);
  }

  // ==================== 赛季日追踪 ====================

  /**
   * 标记某玩家在指定赛季已达到的 seasonDay（取最大值）
   * @param {string} openId
   * @param {number} seasonId 当前赛季 id
   * @param {number} seasonDay 当前已达到的 seasonDay（1..7）
   */
  markSeasonAttendance(openId, seasonId, seasonDay) {
    if (!openId || !Number.isFinite(seasonId) || !Number.isFinite(seasonDay)) return;
    let perPlayer = this._maxSeasonDayAttended.get(openId);
    if (!perPlayer) {
      perPlayer = new Map();
      this._maxSeasonDayAttended.set(openId, perPlayer);
    }
    const cur = perPlayer.get(seasonId) || 0;
    if (seasonDay > cur) perPlayer.set(seasonId, seasonDay);

    // 仅保留最近 10 赛季：按 seasonId 降序排序，裁掉旧赛季
    if (perPlayer.size > MAX_SEASONS_TO_KEEP) {
      const ids = [...perPlayer.keys()].sort((a, b) => b - a);
      for (let i = MAX_SEASONS_TO_KEEP; i < ids.length; i++) {
        perPlayer.delete(ids[i]);
      }
    }
  }

  // ==================== 评估（三条件） ====================

  /**
   * 判断 openId 是否满足老用户豁免条件；满足则写入 _veteranStreamers + 推送 veteran_unlocked
   *
   * @param {string} openId
   * @param {number} lifetimeContribValue 当前 _lifetimeContrib[openId] 值
   * @param {number} maxFortressDay 当前房间的 maxFortressDay（条件 2 per-room）
   * @param {function} broadcast 接收 veteran_unlocked 的广播函数（room.broadcast）；MVP 房间级广播即可
   * @returns {{newlyUnlocked:boolean, reason:string|null}}
   */
  evaluateVeteran(openId, lifetimeContribValue, maxFortressDay, broadcast) {
    if (!openId) return { newlyUnlocked: false, reason: null };
    if (this._veteranStreamers.has(openId)) return { newlyUnlocked: false, reason: null };

    let reason = null;
    if (Number.isFinite(lifetimeContribValue) && lifetimeContribValue >= THRESHOLD_LIFETIME_CONTRIB) {
      reason = 'lifetime_contrib';
    } else if (Number.isFinite(maxFortressDay) && maxFortressDay >= THRESHOLD_MAX_FORTRESS_DAY) {
      reason = 'fortress_day';
    } else {
      // 条件 3：任一赛季 seasonDay >= 5
      const perPlayer = this._maxSeasonDayAttended.get(openId);
      if (perPlayer) {
        for (const d of perPlayer.values()) {
          if (d >= THRESHOLD_MAX_SEASON_DAY) { reason = 'seasons_completed'; break; }
        }
      }
    }

    if (!reason) return { newlyUnlocked: false, reason: null };

    // 首次达标 → 写入豁免集 + 推送
    this._veteranStreamers.add(openId);
    try {
      if (typeof broadcast === 'function') {
        broadcast({
          type: 'veteran_unlocked',
          timestamp: Date.now(),
          data: { openId, reason },
        });
      }
    } catch (e) {
      // 广播失败不影响豁免状态
    }
    console.log(`[VeteranTracker] Unlocked: ${openId} reason=${reason}`);
    return { newlyUnlocked: true, reason };
  }

  // ==================== 持久化 ====================

  /** 返回可 JSON 序列化的快照 */
  snapshot() {
    const seasons = {};
    for (const [openId, perPlayer] of this._maxSeasonDayAttended) {
      const obj = {};
      for (const [sid, day] of perPlayer) obj[String(sid)] = day;
      seasons[openId] = obj;
    }
    return {
      _veteranStreamers: [...this._veteranStreamers],
      _maxSeasonDayAttended: seasons,
    };
  }

  /** 从快照恢复（合并语义：全局单例在多房间构造时被逐次调用，必须 add-only，不能 clobber 其他房间数据）*/
  loadSnapshot(data) {
    if (!data || typeof data !== 'object') return;
    if (Array.isArray(data._veteranStreamers)) {
      for (const openId of data._veteranStreamers) this._veteranStreamers.add(openId);
    }
    if (data._maxSeasonDayAttended && typeof data._maxSeasonDayAttended === 'object') {
      for (const [openId, obj] of Object.entries(data._maxSeasonDayAttended)) {
        if (!obj || typeof obj !== 'object') continue;
        let perPlayer = this._maxSeasonDayAttended.get(openId);
        if (!perPlayer) {
          perPlayer = new Map();
          this._maxSeasonDayAttended.set(openId, perPlayer);
        }
        for (const [sid, day] of Object.entries(obj)) {
          const sidNum = parseInt(sid, 10);
          if (Number.isFinite(sidNum) && Number.isFinite(day)) {
            const existing = perPlayer.get(sidNum) || 0;
            if (day > existing) perPlayer.set(sidNum, day);
          }
        }
      }
    }
  }
}

module.exports = VeteranTracker;
module.exports.THRESHOLD_LIFETIME_CONTRIB = THRESHOLD_LIFETIME_CONTRIB;
module.exports.THRESHOLD_MAX_FORTRESS_DAY = THRESHOLD_MAX_FORTRESS_DAY;
module.exports.THRESHOLD_MAX_SEASON_DAY   = THRESHOLD_MAX_SEASON_DAY;
