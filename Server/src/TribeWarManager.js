/**
 * TribeWarManager — 跨直播间攻防战 全局单例（§35 Tribe War）
 *
 * 职责：
 *   1. 维护所有活跃 TribeWarSession（攻方与守方一一对应，不可多对一）
 *   2. 提供大厅查询、发起/停止攻击、反击、结算清理等入口
 *   3. 接收 SurvivalGameEngine 的能量增量 + 夜晚入口事件，驱动 session 的能量累积与远征怪释放
 *
 * PM 决策（MVP，P1 9d 范围）：
 *   - 赛季切换强制断开：stopAllSessions('season_reset') 由 SeasonManager.advanceDay 触发
 *   - _roomCreatorId 鉴权放开（统一到 SurvivalRoom 层的 isRoomCreator 校验；MVP 阶段跳过）
 *   - P2 60s 手动停止冷却：_manualStopCooldowns Map 仅内存（重启即清，符合"冷却不跨重启"语义）
 *   - P2 战报持久化：engine._warReports 10 条滑动窗口 → RoomPersistence schemaVersion 3
 *   - P2 反击：damageMultiplier 由 SurvivalRoom 按 §37 beacon 填入（有 beacon 则 1.5，否则 1.0）
 *   - P2 3 分钟无能量自动断开：简单 timer 实现
 */

class TribeWarManager {
  /**
   * @param {RoomManager} roomManager — RoomManager 引用（查房间列表 + getRoom）
   */
  constructor(roomManager) {
    this._roomMgr = roomManager;
    /** @type {Map<string, import('./TribeWarSession')>} sessionId → session */
    this._sessions = new Map();
    /** @type {Map<string, string>} attackerRoomId → sessionId */
    this._attackerToSession = new Map();
    /** @type {Map<string, string>} defenderRoomId → sessionId */
    this._defenderToSession = new Map();
    /** @type {Map<string, number>} attackerRoomId → cooldownEndTs — §35 P2 手动停止 60s 冷却 */
    this._manualStopCooldowns = new Map();
    this._sessionIdCounter = 0;
    console.log('[TribeWarManager] Initialized');
  }

  // ==================== 大厅查询 ====================

  /**
   * 返回可攻击的房间列表（排除自己）。
   * @param {string} excludeRoomId
   * @returns {Array<{roomId,streamerName,state,day,underAttack,attackable}>}
   */
  getRoomList(excludeRoomId) {
    const list = [];
    if (!this._roomMgr || !this._roomMgr.rooms) return list;

    for (const [roomId, room] of this._roomMgr.rooms.entries()) {
      if (roomId === excludeRoomId) continue;
      if (!room || room.status === 'destroyed') continue;

      const engine = room.survivalEngine;
      const st = engine ? engine.state : 'idle';
      // 仅列出 running（day/night）房间；settlement/idle 房间不可攻击（防止对方战斗已结束）
      const isRunning = (st === 'day' || st === 'night');

      list.push({
        roomId,
        streamerName: room.streamerName || roomId,
        state: st,
        day: engine ? engine.currentDay : 0,
        underAttack: this._defenderToSession.has(roomId),
        attackable: isRunning && !this._defenderToSession.has(roomId),
      });
    }
    return list;
  }

  // ==================== 攻击生命周期 ====================

  /**
   * 发起攻击。
   * 校验失败时返回 `{ ok: false, reason: ... }`；成功返回 `{ ok: true, sessionId }`。
   *
   * @param {string} attackerRoomId
   * @param {string} defenderRoomId
   * @param {object} [opts]
   * @param {number} [opts.damageMultiplier=1.0] — §37 beacon 反击加成（默认 1.0，TODO hook）
   * @returns {{ok: boolean, reason?: string, sessionId?: string}}
   */
  startAttack(attackerRoomId, defenderRoomId, opts) {
    opts = opts || {};
    const dm = (typeof opts.damageMultiplier === 'number') ? opts.damageMultiplier : 1.0;

    if (!attackerRoomId || !defenderRoomId) {
      return { ok: false, reason: 'room_not_found' };
    }
    if (attackerRoomId === defenderRoomId) {
      return { ok: false, reason: 'self_target' };
    }
    // §35 P2 手动停止冷却（60s）：若 attacker 上一次 manual 停攻未到期，拒绝新攻击
    const cooldownEnd = this._manualStopCooldowns.get(attackerRoomId);
    if (cooldownEnd && cooldownEnd > Date.now()) {
      return { ok: false, reason: 'in_cooldown', cooldownMs: cooldownEnd - Date.now() };
    }
    // 过期冷却自动清理（双保险；_endSession 时不反向扫描此表，避免重启后残留）
    if (cooldownEnd && cooldownEnd <= Date.now()) {
      this._manualStopCooldowns.delete(attackerRoomId);
    }
    if (this._attackerToSession.has(attackerRoomId)) {
      return { ok: false, reason: 'attacker_busy' };
    }
    if (this._defenderToSession.has(defenderRoomId)) {
      return { ok: false, reason: 'target_busy' };
    }

    const attacker = this._roomMgr.getRoom(attackerRoomId);
    const defender = this._roomMgr.getRoom(defenderRoomId);
    if (!attacker || !defender) {
      return { ok: false, reason: 'room_not_found' };
    }
    const atkSt = attacker.survivalEngine && attacker.survivalEngine.state;
    const defSt = defender.survivalEngine && defender.survivalEngine.state;
    if ((atkSt !== 'day' && atkSt !== 'night') ||
        (defSt !== 'day' && defSt !== 'night')) {
      return { ok: false, reason: 'wrong_phase' };
    }

    const TribeWarSession = require('./TribeWarSession');
    const sessionId = `tw_${Date.now()}_${++this._sessionIdCounter}`;
    const session = new TribeWarSession(sessionId, attacker, defender, this, dm);

    this._sessions.set(sessionId, session);
    this._attackerToSession.set(attackerRoomId, sessionId);
    this._defenderToSession.set(defenderRoomId, sessionId);

    // 双向通知（attacker → tribe_war_attack_started；defender → tribe_war_under_attack）
    const atkName = attacker.streamerName || attackerRoomId;
    const defName = defender.streamerName || defenderRoomId;
    try {
      attacker.broadcast({
        type: 'tribe_war_attack_started',
        timestamp: Date.now(),
        data: {
          sessionId,
          attackerRoomId,
          attackerStreamerName: atkName,
          defenderRoomId,
          defenderStreamerName: defName,
        },
      });
      defender.broadcast({
        type: 'tribe_war_under_attack',
        timestamp: Date.now(),
        data: {
          sessionId,
          attackerRoomId,
          attackerStreamerName: atkName,
        },
      });
    } catch (e) {
      console.warn(`[TribeWarManager] start broadcast error: ${e.message}`);
    }

    // P0-A5 room_state 广播：tribe_war_start 后双方各推一次
    try {
      if (attacker.survivalEngine && typeof attacker.survivalEngine._broadcastRoomState === 'function') {
        attacker.survivalEngine._broadcastRoomState('tribe_war_start');
      }
    } catch (_) { /* ignore */ }
    try {
      if (defender.survivalEngine && typeof defender.survivalEngine._broadcastRoomState === 'function') {
        defender.survivalEngine._broadcastRoomState('tribe_war_start');
      }
    } catch (_) { /* ignore */ }

    console.log(`[TribeWarManager] Attack started: ${attackerRoomId} → ${defenderRoomId} (session=${sessionId}, dm=${dm})`);
    return { ok: true, sessionId };
  }

  /**
   * 停止攻击（按 sessionId；reason 见 §35.6）。
   * @param {string} sessionId
   * @param {string} reason — 'manual' | 'no_energy' | 'settlement' | 'season_switch'
   */
  stopAttack(sessionId, reason) {
    const session = this._sessions.get(sessionId);
    if (!session) return false;
    this._endSession(session, reason || 'manual');
    return true;
  }

  /**
   * 房间进入结算 → 该房间参与的所有 session 立即断开。
   * SurvivalRoom/Engine 在 _enterSettlement 时调用。
   */
  onRoomSettlement(roomId) {
    const asAttacker = this._attackerToSession.get(roomId);
    const asDefender = this._defenderToSession.get(roomId);

    if (asAttacker) {
      const s = this._sessions.get(asAttacker);
      if (s) this._endSession(s, 'settlement');
    }
    if (asDefender && asDefender !== asAttacker) {
      const s = this._sessions.get(asDefender);
      if (s) this._endSession(s, 'settlement');
    }
  }

  /**
   * 房间销毁 → 断开所有参与的 session（同 settlement，但原因不同）。
   */
  onRoomDestroyed(roomId) {
    const asAttacker = this._attackerToSession.get(roomId);
    const asDefender = this._defenderToSession.get(roomId);
    if (asAttacker) {
      const s = this._sessions.get(asAttacker);
      if (s) this._endSession(s, 'room_destroyed');
    }
    if (asDefender && asDefender !== asAttacker) {
      const s = this._sessions.get(asDefender);
      if (s) this._endSession(s, 'room_destroyed');
    }
  }

  // ==================== 能量 / 远征怪 ====================

  /**
   * 攻击方的弹幕/礼物能量增量 → 找对应 session 累积。
   * @param {string} attackerRoomId
   * @param {number} delta
   */
  onEnergyAdded(attackerRoomId, delta) {
    if (!delta || delta <= 0) return;
    const sid = this._attackerToSession.get(attackerRoomId);
    if (!sid) return;
    const s = this._sessions.get(sid);
    if (s) s.addEnergy(delta);
  }

  /**
   * 防守方进入夜晚 → 攻击方释放远征怪。
   * 由 SurvivalGameEngine._enterNight 调用。
   * @param {string} defenderRoomId
   */
  onDefenderEnterNight(defenderRoomId) {
    const sid = this._defenderToSession.get(defenderRoomId);
    if (!sid) return;
    const s = this._sessions.get(sid);
    if (!s) return;
    s.onDefenderEnterNight();
  }

  // ==================== 查询 ====================

  /** 攻击方房间是否正在攻击别人 */
  getSessionAsAttacker(roomId) {
    const sid = this._attackerToSession.get(roomId);
    return sid ? this._sessions.get(sid) : null;
  }

  /** 防守方房间是否正在被攻击 */
  getSessionAsDefender(roomId) {
    const sid = this._defenderToSession.get(roomId);
    return sid ? this._sessions.get(sid) : null;
  }

  // ==================== 内部 ====================

  /**
   * 统一 session 收尾：通知双方 + 清登记 + 清 session 内部 timer。
   */
  _endSession(session, reason) {
    if (!session) return;
    if (!this._sessions.has(session.id)) return;  // 已清过，幂等

    try {
      session.end(reason);
    } catch (e) {
      console.warn(`[TribeWarManager] session.end error: ${e.message}`);
    }

    // 广播给双方（session.end 内部不广播 attack_ended，统一在 manager 层广播）
    const endPayload = {
      sessionId: session.id,
      reason,
      stolenFood: session.stolenFood,
      stolenCoal: session.stolenCoal,
      stolenOre: session.stolenOre,
    };
    try {
      if (session.attacker && session.attacker.status !== 'destroyed') {
        session.attacker.broadcast({
          type: 'tribe_war_attack_ended',
          timestamp: Date.now(),
          data: endPayload,
        });
      }
      if (session.defender && session.defender.status !== 'destroyed') {
        session.defender.broadcast({
          type: 'tribe_war_attack_ended',
          timestamp: Date.now(),
          data: endPayload,
        });
      }
    } catch (e) {
      console.warn(`[TribeWarManager] end broadcast error: ${e.message}`);
    }

    // §35 P2 手动停止 → 给 attacker 60s 冷却（仅 reason === 'manual' 生效）
    if (reason === 'manual' && session.attacker && session.attacker.roomId) {
      this._manualStopCooldowns.set(session.attacker.roomId, Date.now() + 60000);
    }

    // §35 P2 战报持久化：向双方 engine._warReports 追加一条（10 条滑动窗口由 engine 自行维持）
    const report = {
      sessionId: session.id,
      startedAt: session.startedAt || Date.now(),
      endedAt: Date.now(),
      reason,
      stolenFood: session.stolenFood,
      stolenCoal: session.stolenCoal,
      stolenOre: session.stolenOre,
      attackerRoomId: session.attacker ? session.attacker.roomId : null,
      defenderRoomId: session.defender ? session.defender.roomId : null,
      attackerName: session.attacker ? (session.attacker.streamerName || '') : '',
      defenderName: session.defender ? (session.defender.streamerName || '') : '',
      damageMultiplier: session.damageMultiplier || 1.0,
    };
    try {
      const atkEng = session.attacker && session.attacker.survivalEngine;
      const defEng = session.defender && session.defender.survivalEngine;
      if (atkEng && typeof atkEng._addWarReport === 'function') {
        atkEng._addWarReport({ ...report, role: 'attacker' });
      }
      if (defEng && typeof defEng._addWarReport === 'function') {
        defEng._addWarReport({ ...report, role: 'defender' });
      }
    } catch (e) { console.warn('[TribeWarManager] war report write fail: ' + e.message); }

    // 清登记
    this._sessions.delete(session.id);
    const atkId = session.attacker ? session.attacker.roomId : null;
    const defId = session.defender ? session.defender.roomId : null;
    if (atkId && this._attackerToSession.get(atkId) === session.id) {
      this._attackerToSession.delete(atkId);
    }
    if (defId && this._defenderToSession.get(defId) === session.id) {
      this._defenderToSession.delete(defId);
    }

    // P0-A5 room_state 广播：tribe_war_end 后双方各推一次（清登记后，room_state.tribeWar 将为 null）
    try {
      if (session.attacker && session.attacker.survivalEngine &&
          typeof session.attacker.survivalEngine._broadcastRoomState === 'function') {
        session.attacker.survivalEngine._broadcastRoomState('tribe_war_end');
      }
    } catch (_) { /* ignore */ }
    try {
      if (session.defender && session.defender.survivalEngine &&
          typeof session.defender.survivalEngine._broadcastRoomState === 'function') {
        session.defender.survivalEngine._broadcastRoomState('tribe_war_end');
      }
    } catch (_) { /* ignore */ }

    console.log(`[TribeWarManager] Session ended: ${session.id} (reason=${reason}, stolen F/C/O=${session.stolenFood}/${session.stolenCoal}/${session.stolenOre})`);
  }

  shutdown() {
    for (const s of [...this._sessions.values()]) {
      try { s.end('shutdown'); } catch (e) { /* ignore */ }
    }
    this._sessions.clear();
    this._attackerToSession.clear();
    this._defenderToSession.clear();
    console.log('[TribeWarManager] Shutdown');
  }

  /**
   * §36 赛季切换强制断开：清空所有 session 并广播 tribe_war_attack_ended（reason='season_reset' 或调用方传入）。
   * 幂等：已结束的 session 不重复广播；调用后 _manualStopCooldowns 也一并清空（赛季初不继承冷却）。
   * @param {string} [reason='season_reset'] 结束原因
   * @returns {number} 已中断的 session 数量
   */
  stopAllSessions(reason) {
    const r = reason || 'season_reset';
    const sessions = [...this._sessions.values()];
    let count = 0;
    for (const s of sessions) {
      try {
        this._endSession(s, r);
        count += 1;
      } catch (e) { console.warn(`[TribeWarManager] stopAllSessions error: ${e.message}`); }
    }
    // 赛季切换同时清冷却（避免跨赛季继承 60s 冷却）
    this._manualStopCooldowns.clear();
    if (count > 0) {
      console.log(`[TribeWarManager] stopAllSessions: ${count} session(s) terminated (reason=${r})`);
    }
    return count;
  }
}

module.exports = TribeWarManager;
