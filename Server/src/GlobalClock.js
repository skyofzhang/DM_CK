/**
 * GlobalClock - §36 全服同步时钟（MVP 极简版）
 *
 * 职责：
 * 1. 所有房间共享同一 phase（day / night）
 * 2. 每秒广播 `world_clock_tick` 到所有注册房间
 * 3. phase 到期时，向所有房间触发 `_enterNightFromClock` / `_enterDayFromClock`
 * 4. 驱动 SeasonManager：每次 night → day 切换时 advanceDay()
 * 5. 应用主题对昼夜时长的倍率（dawn ×0.8 / serene 夜长 ×1.3）
 *
 * PM 决策（MVP 裁剪项）：
 * - 跨房间 Boss Rush 不做
 * - 赛季结算 season_settlement 广播空对象占位
 * - 不同步 fortressDay（fortressDay 仍是 per-room）
 */

class GlobalClock {
  /**
   * @param {SeasonManager} seasonMgr - 全局赛季管理器单例
   * @param {object} [options]
   * @param {number} [options.tickMs=1000]           每秒 tick 一次
   * @param {number} [options.dayDurationMs=120000]  基础白天时长（毫秒）
   * @param {number} [options.nightDurationMs=120000] 基础夜晚时长（毫秒）
   */
  constructor(seasonMgr, options = {}) {
    this._seasonMgr = seasonMgr;
    this._rooms = new Set();                      // 注册的 SurvivalRoom
    this._tickMs = options.tickMs || 1000;
    this._baseDayDurationMs = options.dayDurationMs || 120 * 1000;
    this._baseNightDurationMs = options.nightDurationMs || 120 * 1000;

    this._phase = 'day';                          // 'day' | 'night'
    this._phaseStartedAt = Date.now();

    // 启动 tick
    this._timer = setInterval(() => this._tick(), this._tickMs);
    console.log(`[GlobalClock] Started: phase=${this._phase}, dayMs=${this._baseDayDurationMs}, nightMs=${this._baseNightDurationMs}`);
  }

  // ==================== 房间注册 ====================

  registerRoom(room) {
    this._rooms.add(room);
    // 新房间加入时立即推送一次 season_state（当前赛季信息）
    try {
      if (room.broadcast && this._seasonMgr) {
        room.broadcast({
          type: 'season_state',
          timestamp: Date.now(),
          data: {
            seasonId: this._seasonMgr.seasonId,
            seasonDay: this._seasonMgr.seasonDay,
            themeId: this._seasonMgr.themeId,
          },
        });
      }
    } catch (e) {
      // ignore
    }
  }

  unregisterRoom(room) {
    this._rooms.delete(room);
  }

  // ==================== Phase 查询 ====================

  /** 当前 phase 的剩余秒数（应用了主题倍率） */
  getPhaseRemainingSec() {
    const now = Date.now();
    const duration = this._getCurrentPhaseDurationMs();
    return Math.max(0, Math.round((duration - (now - this._phaseStartedAt)) / 1000));
  }

  /** 当前 phase（应用了主题倍率的）完整时长 ms */
  _getCurrentPhaseDurationMs() {
    const themeId = this._seasonMgr ? this._seasonMgr.themeId : 'classic_frozen';
    const isDay = this._phase === 'day';

    let dayMult = 1.0;
    let nightMult = 1.0;

    if (themeId === 'dawn') {
      // §36.x 黎明：昼夜长 ×0.8
      dayMult = 0.8;
      nightMult = 0.8;
    } else if (themeId === 'serene') {
      // §36.x 宁静：夜晚长 ×1.3
      nightMult = 1.3;
    }

    return isDay
      ? Math.round(this._baseDayDurationMs * dayMult)
      : Math.round(this._baseNightDurationMs * nightMult);
  }

  // ==================== 内部：tick 循环 ====================

  _tick() {
    const now = Date.now();
    const duration = this._getCurrentPhaseDurationMs();

    // ── Phase 到期 → 切换 ────────────────────────────────────────────
    if (now - this._phaseStartedAt >= duration) {
      const newPhase = this._phase === 'day' ? 'night' : 'day';
      const oldPhase = this._phase;
      this._phase = newPhase;
      this._phaseStartedAt = now;

      console.log(`[GlobalClock] Phase transition: ${oldPhase} → ${newPhase} (rooms=${this._rooms.size})`);

      // 触发所有房间的 phase 回调（容错：单房异常不影响其他房间）
      for (const room of this._rooms) {
        try {
          if (!room || !room.survivalEngine) continue;
          if (newPhase === 'night') {
            if (typeof room.survivalEngine._enterNightFromClock === 'function') {
              room.survivalEngine._enterNightFromClock();
            }
          } else {
            if (typeof room.survivalEngine._enterDayFromClock === 'function') {
              room.survivalEngine._enterDayFromClock();
            }
          }
        } catch (e) {
          console.warn(`[GlobalClock] phase callback error (room=${room && room.roomId}): ${e.message}`);
        }
      }

      // seasonDay 在 night → day 时推进（完成一个"日"循环）
      if (newPhase === 'day' && this._seasonMgr) {
        try {
          this._seasonMgr.advanceDay(this._rooms);
        } catch (e) {
          console.warn(`[GlobalClock] advanceDay error: ${e.message}`);
        }
      }
    }

    // ── 每秒广播 world_clock_tick ────────────────────────────────────
    const remainingSec = Math.max(0, Math.round((duration - (now - this._phaseStartedAt)) / 1000));
    const season = this._seasonMgr || {};
    const payload = {
      phase: this._phase,
      seasonDay: season.seasonDay || 1,
      seasonId: season.seasonId || 1,
      themeId: season.themeId || 'classic_frozen',
      phaseRemainingSec: remainingSec,
    };
    for (const room of this._rooms) {
      try {
        if (!room || typeof room.broadcast !== 'function') continue;
        // 未 active 的房间也广播（客户端可用于 UI 显示）
        room.broadcast({
          type: 'world_clock_tick',
          timestamp: now,
          data: payload,
        });
        // 同步到引擎的 remainingTime（让 getFullState 与 UI 显示一致）
        if (room.survivalEngine && (room.survivalEngine.state === 'day' || room.survivalEngine.state === 'night')) {
          room.survivalEngine.remainingTime = remainingSec;
        }
      } catch (e) {
        // 单房异常不影响其他房
      }
    }
  }

  stop() {
    if (this._timer) {
      clearInterval(this._timer);
      this._timer = null;
    }
    console.log('[GlobalClock] Stopped');
  }
}

module.exports = GlobalClock;
