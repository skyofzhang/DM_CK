/**
 * SeasonManager - §36 赛季管理（MVP 极简版）
 *
 * 职责：
 * 1. 维护 seasonId / seasonDay(1..7) / themeId
 * 2. advanceDay() — 每次 night→day 切换时推进 seasonDay
 * 3. seasonDay > 7 → 赛季结束：seasonId +1, seasonDay 归 1, 主题按 6 主题顺序轮换
 * 4. 广播 season_settlement（空占位）与 season_started
 *
 * PM 决策（MVP 裁剪项）：
 * - 赛季结算内容（排名、奖励）为空占位对象
 * - D7 Boss Rush 不做（仅保留主题标记 frenzy 等）
 * - 跨房间 Top10 榜单不做
 */

const SEASON_THEMES = [
  'classic_frozen',  // 0: 经典冰原（无特殊）
  'blood_moon',       // 1: 血月（夜晚怪物 HP ×1.2 + 资源掉落 ×1.2）
  'snowstorm',        // 2: 风雪（白天采矿 ×0.9）
  'dawn',             // 3: 黎明（昼夜 ×0.8）
  'frenzy',           // 4: 狂潮（maxAliveMonsters 15→17）
  'serene',           // 5: 宁静（夜晚 ×1.3 + Boss HP ×0.9；MVP 仅夜长）
];

class SeasonManager {
  constructor() {
    this.seasonId = 1;
    this.seasonDay = 1;
    this.themeId = SEASON_THEMES[0];

    // §36.4 v1.27：D7 夜晚开始时就决定下一赛季主题（GlobalClock 写入），
    // 然后 season_settlement 携带同一 nextThemeId 给客户端做预告
    this._nextThemeId = null;
  }

  /**
   * §36.4 v1.27 / §36.6 首 2 赛季固定主题：计算下一赛季主题（幂等，由 GlobalClock D7 夜晚启动 BossRush 时调用）
   *
   * §36.6 决策 2 + 决策 3：
   *   - next seasonId === 1 → 强制 'classic_frozen'
   *   - next seasonId === 2 → 强制 'blood_moon'
   *   - next seasonId >= 3  → 从 SEASON_THEMES 中**排除当前 themeId（lastThemeId）** 后随机
   *                           （防止连续两赛季同主题）
   *
   * @returns {string} 下一赛季的 themeId
   */
  computeNextThemeId() {
    // 推进后的 seasonId 就是下一赛季；由 seasonId+1 决定主题
    const nextSeasonId = this.seasonId + 1;
    if (nextSeasonId === 1) return 'classic_frozen';
    if (nextSeasonId === 2) return 'blood_moon';
    // seasonId >= 3 → 排除 lastThemeId（即当前 this.themeId）后随机
    const lastThemeId = this.themeId;
    const pool = SEASON_THEMES.filter(t => t !== lastThemeId);
    const pickFrom = pool.length > 0 ? pool : SEASON_THEMES; // 极端兜底（不可能为空）
    const idx = Math.floor(Math.random() * pickFrom.length);
    return pickFrom[idx];
  }

  /**
   * §36.6 新赛季主题 resolver：
   *   seasonId === 1 → 'classic_frozen'
   *   seasonId === 2 → 'blood_moon'
   *   seasonId >= 3  → 排除 lastThemeId 后随机
   */
  _resolveThemeForSeason(seasonId, lastThemeId) {
    if (seasonId === 1) return 'classic_frozen';
    if (seasonId === 2) return 'blood_moon';
    const pool = SEASON_THEMES.filter(t => t !== lastThemeId);
    const pickFrom = pool.length > 0 ? pool : SEASON_THEMES;
    return pickFrom[Math.floor(Math.random() * pickFrom.length)];
  }

  /**
   * §36 P0-A3：聚合跨房间 topContributors Top10 + survivingRooms
   *   - survivingRooms：当前注册房间中 fortressDay > 0 的房间数
   *   - topContributors[]：跨房间按 _lifetimeContrib 汇总（同玩家跨房间取最大值），取 Top10
   *     每项 { playerId, playerName, contribution }
   */
  _buildSeasonSettlementExtras(rooms) {
    let survivingRooms = 0;
    const contribByPid = new Map();      // pid -> contribution（最大值）
    const nameByPid    = new Map();      // pid -> playerName（first-write wins）

    if (rooms && typeof rooms[Symbol.iterator] === 'function') {
      for (const room of rooms) {
        if (!room) continue;
        const engine = room.survivalEngine;
        if (!engine) continue;
        const fd = engine.fortressDay || 0;
        if (fd > 0) survivingRooms += 1;

        const lc = engine._lifetimeContrib || {};
        const names = engine.playerNames || {};
        for (const pid of Object.keys(lc)) {
          if (!pid) continue;
          const val = Number(lc[pid]) || 0;
          if (val <= 0) continue;
          const prev = contribByPid.get(pid) || 0;
          if (val > prev) contribByPid.set(pid, val);
          if (!nameByPid.has(pid)) {
            nameByPid.set(pid, names[pid] || pid);
          }
        }
      }
    }

    const topContributors = Array.from(contribByPid.entries())
      .sort((a, b) => b[1] - a[1])
      .slice(0, 10)
      .map(([pid, contribution]) => ({
        playerId:     pid,
        playerName:   nameByPid.get(pid) || pid,
        contribution,
      }));

    return { survivingRooms, topContributors };
  }

  /**
   * 推进一"日"。由 GlobalClock 在 night→day 切换时调用。
   * 满 7 日 → 赛季结算 + 新赛季开启。
   *
   * @param {Set<SurvivalRoom>} [rooms] 已注册的房间集合（用于广播 season_settlement / season_started）
   */
  advanceDay(rooms = null) {
    this.seasonDay += 1;
    if (this.seasonDay > 7) {
      // 旧赛季结算
      const oldSeasonId = this.seasonId;
      const oldThemeId = this.themeId;

      // §36.4 v1.27：若 GlobalClock 已在 D7 夜晚开始时写入 _nextThemeId，则 season_settlement 优先用它
      // （保证客户端在 D7 夜晚就看到下一赛季主题预告；否则 fallback 到计算值）
      const settlementNextTheme = this._nextThemeId || this.computeNextThemeId();

      // §36 P0-A3：聚合跨房间 topContributors + survivingRooms（在新赛季开始前采集，
      //   此时各房引擎的 _lifetimeContrib 仍为上一赛季的最终值）
      const extras = this._buildSeasonSettlementExtras(rooms);

      // 新赛季
      this.seasonId += 1;
      this.seasonDay = 1;
      // §36.6 首 2 赛季固定主题；S>=3 排除 lastThemeId 随机
      this.themeId = this._resolveThemeForSeason(this.seasonId, oldThemeId);

      // 清理预告（新赛季开始）
      this._nextThemeId = null;

      console.log(`[SeasonManager] Season ${oldSeasonId} ended (${oldThemeId}) → Season ${this.seasonId} started (${this.themeId}); survivingRooms=${extras.survivingRooms} topN=${extras.topContributors.length}`);

      // §36 赛季切换强制断开 Tribe War：复用 rooms 里任一 room 的 tribeWarMgr 引用（单例），幂等调用 stopAllSessions
      if (rooms && rooms.size > 0) {
        let _tribeWarMgr = null;
        for (const room of rooms) {
          if (room && room.tribeWarMgr) { _tribeWarMgr = room.tribeWarMgr; break; }
        }
        if (_tribeWarMgr && typeof _tribeWarMgr.stopAllSessions === 'function') {
          try { _tribeWarMgr.stopAllSessions('season_reset'); } catch (e) { console.warn(`[SeasonManager] stopAllSessions error: ${e.message}`); }
        }
      }

      // 广播
      if (rooms && rooms.size > 0) {
        for (const room of rooms) {
          try {
            if (!room || typeof room.broadcast !== 'function') continue;
            // §36 P0-A3 season_settlement：携带 nextThemeId + survivingRooms + topContributors[]
            room.broadcast({
              type: 'season_settlement',
              timestamp: Date.now(),
              data: {
                seasonId:         oldSeasonId,
                nextThemeId:      settlementNextTheme,
                survivingRooms:   extras.survivingRooms,
                topContributors:  extras.topContributors,
              },
            });
            // season_started
            room.broadcast({
              type: 'season_started',
              timestamp: Date.now(),
              data: {
                seasonId: this.seasonId,
                themeId: this.themeId,
              },
            });
            // P0-A5 room_state 广播：season_changed（season_started）后推一次
            try {
              if (room.survivalEngine && typeof room.survivalEngine._broadcastRoomState === 'function') {
                room.survivalEngine._broadcastRoomState('season_started');
              }
            } catch (_) { /* ignore */ }
          } catch (e) {
            // 单房异常不影响其他房
          }
        }
      }
    } else {
      console.log(`[SeasonManager] Day advanced: seasonId=${this.seasonId} seasonDay=${this.seasonDay} theme=${this.themeId}`);
    }
  }

  /**
   * 主题查询辅助：当前是否 blood_moon（影响怪物 HP ×1.2）
   */
  isBloodMoon() { return this.themeId === 'blood_moon'; }

  /**
   * 主题查询辅助：当前是否 frenzy（影响 maxAliveMonsters 上浮）
   */
  isFrenzy() { return this.themeId === 'frenzy'; }

  /**
   * 主题查询辅助：当前是否 snowstorm（影响白天采矿 ×0.9）
   */
  isSnowstorm() { return this.themeId === 'snowstorm'; }
}

module.exports = SeasonManager;
module.exports.SEASON_THEMES = SEASON_THEMES;
