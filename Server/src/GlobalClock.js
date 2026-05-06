/**
 * GlobalClock - §36 全服同步时钟（MVP 极简版 + §36.4 BossRush + §36.12 FEATURE_UNLOCK_DAY 广播）
 *
 * 职责：
 * 1. 所有房间共享同一 phase（day / night）
 * 2. 每秒广播 `world_clock_tick` 到所有注册房间（携带 newlyUnlockedFeatures 仅在 seasonDay 递增的那一秒）
 * 3. phase 到期时，向所有房间触发 `_enterNightFromClock` / `_enterDayFromClock`
 * 4. 驱动 SeasonManager：每次 night → day 切换时 advanceDay()
 * 5. 应用主题对昼夜时长的倍率（dawn ×0.8 / serene 夜长 ×1.3）
 * 6. §36.4 维护全服 Boss Rush HP 池（D7 夜晚启动，累加跨房间伤害）
 *
 * PM 决策：
 * - 赛季结算 season_settlement 由 SeasonManager.advanceDay 负责（携带 nextThemeId）
 * - D7 Boss 池归零 → 推 `season_boss_rush_killed` 后立即走赛季结算
 * - 不同步 fortressDay（fortressDay 仍是 per-room）
 */

const {
  getNewlyUnlockedFeatures,
  getUnlockedFeatures,
} = require('./config/FeatureUnlockConfig');

// §36.4 BossRush HP 池基数：每个参与房间贡献 5000 HP
const BOSS_RUSH_HP_PER_ROOM = 5000;

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

    // §36.12 seasonDay 递增侦测：在 seasonDay 刚变化的那一个 tick 携带 newlyUnlockedFeatures
    this._lastSeasonDayTicked = seasonMgr ? seasonMgr.seasonDay : 1;
    this._pendingNewlyUnlocked = [];              // 仅首个 tick 非空，后续清空

    // §36.4 BossRush 全服共享 HP 池
    this._bossRushSeasonId = null;                 // 正在进行的 Boss Rush 赛季 id
    this._bossRushHpPool   = 0;                    // 剩余 HP
    this._bossRushHpTotal  = 0;                    // 总 HP（不随扣减变化，客户端进度条参考）
    this._bossRushParticipatingRooms = new Set();  // 参与房间 id（快照；每次启动重刷）
    this._bossRushKilled = false;                  // 本赛季是否已击杀过（防重复推送）
    this._bossRushSettlementScheduled = false;     // 击杀后结算只调度一次

    // 启动 tick
    this._timer = setInterval(() => this._tick(), this._tickMs);
    console.log(`[GlobalClock] Started: phase=${this._phase}, dayMs=${this._baseDayDurationMs}, nightMs=${this._baseNightDurationMs}`);
  }

  // ==================== 房间注册 ====================

  registerRoom(room) {
    this._rooms.add(room);
    // 新房间加入时立即推送一次 season_state（当前赛季信息 + 已解锁功能全集）
    try {
      if (room.broadcast && this._seasonMgr) {
        const seasonDay = this._seasonMgr.seasonDay;
        // 🔴 audit-r37 GAP-C37-05：老用户豁免覆盖 — 优先走 SurvivalEngine._getUnlockedFeaturesForClient()
        //   旧版直接调 getUnlockedFeatures(seasonDay) 仅按当前赛季日返回 → 老用户重连时多锁定
        //   新版优先走 engine 的 _getUnlockedFeaturesForClient（含老用户判断 → 返回全集）
        //   fallback 仍走 getUnlockedFeatures(seasonDay) 兼容无 engine 的特殊情形
        let unlockedFeatures = null;
        if (room.survivalEngine && typeof room.survivalEngine._getUnlockedFeaturesForClient === 'function') {
          try { unlockedFeatures = room.survivalEngine._getUnlockedFeaturesForClient(); } catch (e) { /* ignore */ }
        }
        if (!unlockedFeatures) unlockedFeatures = getUnlockedFeatures(seasonDay);
        room.broadcast({
          type: 'season_state',
          timestamp: Date.now(),
          data: {
            seasonId: this._seasonMgr.seasonId,
            seasonDay,
            themeId: this._seasonMgr.themeId,
            // §36.12：连接时同步一次已解锁列表，避免中途进场客户端错过 newlyUnlockedFeatures 横幅
            unlockedFeatures,
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

  // ==================== §36.4 BossRush ====================

  /**
   * D7 夜晚开始时由 _tick 调用：初始化 / 广播 Boss Rush 池
   */
  _initBossRushForD7Night() {
    if (!this._seasonMgr) return;
    const seasonId = this._seasonMgr.seasonId;
    // 每个赛季仅初始化一次（幂等；防跨 tick 或重放触发）
    if (this._bossRushSeasonId === seasonId) return;

    // 获取参与房间快照（当前注册的房间）+ 决定下一赛季主题（写入 SeasonManager）
    const participating = [];
    for (const room of this._rooms) {
      if (room && room.roomId) participating.push(room.roomId);
    }
    const activeRooms = participating.length || 1;

    const hpTotal = BOSS_RUSH_HP_PER_ROOM * activeRooms;
    this._bossRushSeasonId = seasonId;
    this._bossRushHpPool   = hpTotal;
    this._bossRushHpTotal  = hpTotal;
    this._bossRushParticipatingRooms = new Set(participating);
    this._bossRushKilled   = false;

    // 下一赛季主题预告（SeasonManager 提前决定，D7 夜晚开始时就写入 _nextThemeId）
    let nextThemeId = null;
    if (typeof this._seasonMgr.computeNextThemeId === 'function') {
      nextThemeId = this._seasonMgr.computeNextThemeId();
    } else {
      nextThemeId = this._seasonMgr.themeId || 'classic_frozen';
    }
    this._seasonMgr._nextThemeId = nextThemeId;

    console.log(`[GlobalClock] BossRush start: seasonId=${seasonId} rooms=${activeRooms} hpTotal=${hpTotal} nextTheme=${nextThemeId}`);

    const payload = {
      seasonId,
      bossHpTotal: hpTotal,
      participatingRooms: participating,
      nextThemeId,
    };
    for (const room of this._rooms) {
      try {
        if (!room || typeof room.broadcast !== 'function') continue;
        room.broadcast({
          type: 'season_boss_rush_start',
          timestamp: Date.now(),
          data: payload,
        });
      } catch (e) { /* ignore */ }
    }
  }

  /**
   * 扣减 Boss Rush 全服血量池（由引擎在 D7 夜晚的 monster_died 路径调用）
   * @param {number} damage 本次扣减
   * @returns {{ok:boolean, remaining:number}} 扣减后的剩余 HP
   */
  damageBossRushPool(damage) {
    if (!Number.isFinite(damage) || damage <= 0) {
      return { ok: false, remaining: this._bossRushHpPool };
    }
    if (this._bossRushSeasonId === null || this._bossRushHpPool <= 0) {
      return { ok: false, remaining: this._bossRushHpPool };
    }
    this._bossRushHpPool = Math.max(0, this._bossRushHpPool - Math.floor(damage));
    // 归零 → 广播一次击杀事件（仅一次 dedup）
    if (this._bossRushHpPool === 0 && !this._bossRushKilled) {
      this._bossRushKilled = true;
      const killedAt = Date.now();
      const payload = {
        seasonId: this._bossRushSeasonId,
        killedAt,
      };
      for (const room of this._rooms) {
        try {
          if (!room || typeof room.broadcast !== 'function') continue;
          room.broadcast({
            type: 'season_boss_rush_killed',
            timestamp: killedAt,
            data: payload,
          });
        } catch (e) { /* ignore */ }
      }
      console.log(`[GlobalClock] BossRush KILLED at season ${this._bossRushSeasonId}`);
      this._scheduleBossRushSettlement();
    }
    return { ok: true, remaining: this._bossRushHpPool };
  }

  _scheduleBossRushSettlement() {
    if (this._bossRushSettlementScheduled) return;
    this._bossRushSettlementScheduled = true;
    const timer = setTimeout(() => {
      try { this._settleSeasonAfterBossRushKilled(); }
      catch (e) { console.warn(`[GlobalClock] BossRush settlement error: ${e.message}`); }
    }, 0);
    if (timer && typeof timer.unref === 'function') timer.unref();
  }

  _settleSeasonAfterBossRushKilled() {
    if (!this._seasonMgr || this._seasonMgr.seasonDay !== 7) {
      this._resetBossRushPool();
      return;
    }

    const oldSeasonDay = this._seasonMgr.seasonDay;
    this._phase = 'day';
    this._phaseStartedAt = Date.now();

    // 与自然 night→day 一致：先让各房间完成 D7 夜晚成功收尾，再推进赛季结算。
    for (const room of this._rooms) {
      try {
        if (!room || !room.survivalEngine) continue;
        if (typeof room.survivalEngine._enterDayFromClock === 'function') {
          room.survivalEngine._enterDayFromClock();
        }
      } catch (e) {
        console.warn(`[GlobalClock] boss rush day callback error (room=${room && room.roomId}): ${e.message}`);
      }
    }

    try {
      this._seasonMgr.advanceDay(this._rooms);
    } catch (e) {
      console.warn(`[GlobalClock] boss rush advanceDay error: ${e.message}`);
    }

    const newSeasonDay = this._seasonMgr.seasonDay;
    if (newSeasonDay !== oldSeasonDay) {
      this._pendingNewlyUnlocked = getNewlyUnlockedFeatures(oldSeasonDay, newSeasonDay);
      this._lastSeasonDayTicked = newSeasonDay;
    }
    this._resetBossRushPool();
    console.log(`[GlobalClock] BossRush settlement completed: seasonDay ${oldSeasonDay} → ${newSeasonDay}`);
  }

  /** 查询 BossRush 池状态（给外部诊断/单测用）*/
  getBossRushState() {
    return {
      seasonId: this._bossRushSeasonId,
      hpPool: this._bossRushHpPool,
      hpTotal: this._bossRushHpTotal,
      killed: this._bossRushKilled,
      participating: [...this._bossRushParticipatingRooms],
    };
  }

  /** 赛季切换（或 Boss Rush 终结）时清理池 */
  _resetBossRushPool() {
    this._bossRushSeasonId = null;
    this._bossRushHpPool = 0;
    this._bossRushHpTotal = 0;
    this._bossRushParticipatingRooms = new Set();
    this._bossRushKilled = false;
    this._bossRushSettlementScheduled = false;
  }

  // ==================== 内部：tick 循环 ====================

  _tick() {
    const now = Date.now();
    let duration = this._getCurrentPhaseDurationMs();

    // ── Phase 到期 → 切换 ────────────────────────────────────────────
    if (now - this._phaseStartedAt >= duration) {
      const newPhase = this._phase === 'day' ? 'night' : 'day';
      const oldPhase = this._phase;
      this._phase = newPhase;
      this._phaseStartedAt = now;

      console.log(`[GlobalClock] Phase transition: ${oldPhase} → ${newPhase} (rooms=${this._rooms.size})`);

      // §36.4 D7 夜晚启动 Boss Rush 池（进入 night 那一刻且 seasonDay===7）
      if (newPhase === 'night' && this._seasonMgr && this._seasonMgr.seasonDay === 7) {
        try { this._initBossRushForD7Night(); } catch (e) { console.warn(`[GlobalClock] BossRush init error: ${e.message}`); }
      }

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
        const oldSeasonDay = this._seasonMgr.seasonDay;
        try {
          this._seasonMgr.advanceDay(this._rooms);
        } catch (e) {
          console.warn(`[GlobalClock] advanceDay error: ${e.message}`);
        }
        const newSeasonDay = this._seasonMgr.seasonDay;
        // §36.12 seasonDay 递增 → 计算 newlyUnlockedFeatures，塞到本 tick 的 world_clock_tick
        // 环绕时（7 → 1）getNewlyUnlockedFeatures 返回 []（设计即：不再重复推送初始解锁横幅）
        if (newSeasonDay !== oldSeasonDay) {
          this._pendingNewlyUnlocked = getNewlyUnlockedFeatures(oldSeasonDay, newSeasonDay);
          this._lastSeasonDayTicked = newSeasonDay;
          // 赛季切换（seasonDay 环绕到 1）→ 清理 Boss Rush 池
          if (newSeasonDay < oldSeasonDay) {
            this._resetBossRushPool();
          }
        }
      }

      duration = this._getCurrentPhaseDurationMs();
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
    // §36.12 仅在 seasonDay 刚刚递增的那一个 tick 携带（后续 tick 空数组，客户端按 length>0 判定）
    if (this._pendingNewlyUnlocked.length > 0) {
      payload.newlyUnlockedFeatures = this._pendingNewlyUnlocked;
      this._pendingNewlyUnlocked = [];   // 消费完即清空
    }
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
module.exports.BOSS_RUSH_HP_PER_ROOM = BOSS_RUSH_HP_PER_ROOM;
