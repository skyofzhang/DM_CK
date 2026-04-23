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
   * §36.4 v1.27：计算下一赛季主题（幂等，由 GlobalClock D7 夜晚启动 BossRush 时调用）
   * 规则：按 SEASON_THEMES 顺序轮换（与 advanceDay 里的推进一致）
   *
   * @returns {string} 下一赛季的 themeId
   */
  computeNextThemeId() {
    // 推进后的 seasonId 就是下一赛季；由 seasonId+1 决定主题
    return SEASON_THEMES[this.seasonId % SEASON_THEMES.length];
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

      // 新赛季
      this.seasonId += 1;
      this.seasonDay = 1;
      this.themeId = SEASON_THEMES[(this.seasonId - 1) % SEASON_THEMES.length];

      // 清理预告（新赛季开始）
      this._nextThemeId = null;

      console.log(`[SeasonManager] Season ${oldSeasonId} ended (${oldThemeId}) → Season ${this.seasonId} started (${this.themeId})`);

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
            // season_settlement（MVP 空占位）——携带 D7 夜晚就决定好的 nextThemeId
            room.broadcast({
              type: 'season_settlement',
              timestamp: Date.now(),
              data: {
                seasonId: oldSeasonId,
                nextThemeId: settlementNextTheme,
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
