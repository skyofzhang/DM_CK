/**
 * TribeWarSession — 单次攻防会话（§35 Tribe War）
 *
 * 生命周期：
 *   new → 累积能量 → 守方进夜释放远征怪 → 命中偷资源 → 某方结算/无能量/手动停 → end()
 *
 * 字段语义：
 *   energy             — 已累积未转化的攻击能量
 *   expeditionsSent    — 本 session 累计已派出的远征怪数（单次上限 5，整 session 无上限）
 *   stolenFood/Coal/Ore— 已偷资源汇总（attack_ended 推送用）
 *   lastEnergyAt       — §35.6 3 分钟无能量自动断开检测基准
 *   damageMultiplier   — §37 beacon 反击 ×1.5 预留（MVP 默认 1.0）
 *   _noEnergyTimer     — 每 10s 检查 lastEnergyAt 是否超 180s
 *
 * MVP 口径：
 *   - 远征怪"伤害/偷取/保底"不走独立战斗循环，而是在 releaseExpedition() 时
 *     立即对防守方 `_activeMonsters` 追加远征怪实例；后续由 defender 自身的 _spawnWave 兜底结算。
 *     偷取事件由 session 调用 `onExpeditionHitWorker()` 触发（当前从 releaseExpedition 发起，
 *     按远征怪只数依次 roll；严格遵守策划案 §35.5 30% 概率）。
 *   - 保底机制：防守方资源全 0 时，给予城门伤害 ×1.5 加成（通过广播 bobao 提示；实际城门 HP 扣除
 *     走 defender 的 `_spawnWave(gate damage)` 流水线——MVP 直接在 session 内扣）
 */

class TribeWarSession {
  /**
   * @param {string} id
   * @param {import('./SurvivalRoom')} attacker
   * @param {import('./SurvivalRoom')} defender
   * @param {import('./TribeWarManager')} manager
   * @param {number} [damageMultiplier=1.0]
   */
  constructor(id, attacker, defender, manager, damageMultiplier) {
    this.id = id;
    this.attacker = attacker;
    this.defender = defender;
    this._mgr = manager;

    this.energy = 0;
    this.expeditionsSent = 0;
    this.stolenFood = 0;
    this.stolenCoal = 0;
    this.stolenOre  = 0;

    this.startAt = Date.now();
    this.lastEnergyAt = Date.now();
    this.damageMultiplier = (typeof damageMultiplier === 'number') ? damageMultiplier : 1.0;

    this._ended = false;

    // P2：3 分钟无能量自动断开检测（每 10s 扫描一次）
    this._noEnergyTimer = setInterval(() => {
      if (this._ended) return;
      const idleMs = Date.now() - this.lastEnergyAt;
      if (idleMs > 180 * 1000) {
        console.log(`[TribeWarSession:${this.id}] Auto-end: no energy for ${(idleMs / 1000) | 0}s`);
        // 交由 manager._endSession 统一收尾
        this._mgr.stopAttack(this.id, 'no_energy');
      }
    }, 10 * 1000);
  }

  // ==================== 能量 ====================

  addEnergy(delta) {
    if (this._ended) return;
    if (!delta || delta <= 0) return;
    this.energy += delta;
    this.lastEnergyAt = Date.now();

    // 战报：能量增加（攻守双方同步看见）
    const payload = {
      sessionId: this.id,
      event: 'energy_added',
      detail: {
        delta,
        total: this.energy,
        expeditionsSent: this.expeditionsSent,
      },
    };
    this._broadcastCombatReport(payload);
  }

  // ==================== 远征怪 ====================

  /**
   * 防守方进入夜晚 → 本 session 释放一批远征怪。
   * 规则：每 20 能量 1 只，单次上限 5（策划案 §35.4）。
   */
  onDefenderEnterNight() {
    if (this._ended) return;
    // 守方必须正处夜晚（Engine._enterNight 调用前 state 已切为 night）
    const canSend = Math.floor(this.energy / 20);
    if (canSend <= 0) return;
    const count = Math.min(canSend, 5);
    this.releaseExpedition(count);
  }

  /**
   * 释放 count 只远征怪到防守方直播间。
   * 扣能量 → 追加到 defender._activeMonsters → 广播 monster_wave + 双方协议
   *        → 每只 roll 30% 资源偷取（策划案 §35.5）。
   *
   * @param {number} count
   */
  releaseExpedition(count) {
    if (this._ended) return;
    if (count <= 0) return;

    const cost = count * 20;
    if (this.energy < cost) return;
    this.energy -= cost;
    this.expeditionsSent += count;

    const defEngine = this.defender && this.defender.survivalEngine;
    if (!defEngine || defEngine.state !== 'night') {
      console.warn(`[TribeWarSession:${this.id}] releaseExpedition: defender not in night (state=${defEngine && defEngine.state})`);
      // 能量已扣（视为白扔），不恢复；后续等夜晚再派
      return;
    }

    // ── 取防守方当天普通怪参数作为远征怪属性（§35.4）──
    const cfg = this._getDefenderWaveConfig();
    const baseHp  = (cfg && cfg.normal && cfg.normal.hp)  || 30;
    const baseAtk = (cfg && cfg.normal && cfg.normal.atk) || 3;
    const hpMult  = (defEngine._monsterHpMult  || 1.0) * (defEngine._dynamicHpMult || 1.0);
    const hp  = Math.max(1, Math.round(baseHp * hpMult));
    const atk = Math.max(1, Math.round(baseAtk));

    // ── 追加到 defender._activeMonsters（占 maxAliveMonsters 名额）──
    const monsterIds = [];
    for (let i = 0; i < count; i++) {
      defEngine._monsterIdCounter = (defEngine._monsterIdCounter || 0) + 1;
      const mid = `tw_${this.id}_${defEngine._monsterIdCounter}`;
      defEngine._activeMonsters.set(mid, {
        id: mid,
        type: 'normal',           // 与普通怪兼容；tribeWarSessionId 区分来源
        maxHp: hp,
        currentHp: hp,
        atk,
        tribeWarSessionId: this.id,
      });
      monsterIds.push(mid);
    }

    const attackerName = this.attacker.streamerName || this.attacker.roomId;

    // ── 广播客户端 monster_wave（远征怪走 SpawnTop）──
    try {
      this.defender.broadcast({
        type: 'monster_wave',
        timestamp: Date.now(),
        data: {
          waveIndex: 9800,                  // 远征怪专用 waveIndex（与普通/elite_raid 区分）
          day:       defEngine.currentDay,
          monsterId: 'tribe_war',
          monsterType: 'tribe_war',
          count,
          spawnSide: 'top',
          bypassCap: false,                 // 远征怪占名额（策划案 §35.4）
          tribeWarSessionId: this.id,
          attackerRoomId: this.attacker.roomId,
          attackerStreamerName: attackerName,
        },
      });
    } catch (e) {
      console.warn(`[TribeWarSession:${this.id}] monster_wave broadcast error: ${e.message}`);
    }

    // ── 攻击方/防守方协议推送 ──
    try {
      this.attacker.broadcast({
        type: 'tribe_war_expedition_sent',
        timestamp: Date.now(),
        data: {
          sessionId: this.id,
          count,
          remainingEnergy: this.energy,
        },
      });
      this.defender.broadcast({
        type: 'tribe_war_expedition_incoming',
        timestamp: Date.now(),
        data: {
          sessionId: this.id,
          count,
          attackerStreamerName: attackerName,
        },
      });
    } catch (e) {
      console.warn(`[TribeWarSession:${this.id}] expedition protocol error: ${e.message}`);
    }

    // ── 每只远征怪独立 roll 偷取（策划案 §35.5）──
    for (const mid of monsterIds) {
      this.onExpeditionHitWorker(mid);
    }

    console.log(`[TribeWarSession:${this.id}] releaseExpedition: count=${count}, energy→${this.energy}, expeditions=${this.expeditionsSent}`);
  }

  /**
   * 远征怪"命中"事件：30% 偷资源；全 0 时对城门造成 ×1.5 额外伤害。
   *
   * MVP 简化：由 releaseExpedition 立即 roll（而非在 _spawnWave 实际打到矿工时）；
   * 这样保证能量 → 资源偷取的事件链完整，即使守方矿工全死也能结算。
   *
   * @param {string} monsterId
   */
  onExpeditionHitWorker(monsterId) {
    if (this._ended) return;
    const defEngine = this.defender && this.defender.survivalEngine;
    const atkEngine = this.attacker && this.attacker.survivalEngine;
    if (!defEngine || !atkEngine) return;

    // 30% 概率偷取
    if (Math.random() >= 0.30) return;

    // ── 保底机制：防守方资源全 0 → 1.5× 城门伤害（策划案 §35.5）──
    const defFood = defEngine.food || 0;
    const defCoal = defEngine.coal || 0;
    const defOre  = defEngine.ore  || 0;

    if (defFood <= 0 && defCoal <= 0 && defOre <= 0) {
      // 取单只远征怪基础 gate 伤害 × 1.5
      const baseDmg = (defEngine.monsterGateDamage || 5) * (defEngine._workerDamageMult || 1.0);
      const extraDmg = Math.max(1, Math.round(baseDmg * 1.5 * this.damageMultiplier));
      defEngine.gateHp = Math.max(0, defEngine.gateHp - extraDmg);
      try {
        defEngine._broadcastResourceUpdate && defEngine._broadcastResourceUpdate();
      } catch (e) { /* ignore */ }

      const payload = {
        sessionId: this.id,
        event: 'fallback_gate_damage',
        detail: {
          monsterId,
          extraGateDamage: extraDmg,
          defenderGateHp: defEngine.gateHp,
        },
      };
      this._broadcastCombatReport(payload);
      console.log(`[TribeWarSession:${this.id}] Fallback: defender resources all 0, gate -${extraDmg}HP → ${defEngine.gateHp}`);
      return;
    }

    // ── 按剩余资源加权随机选一类偷 ──
    const weights = [];
    if (defFood > 0) weights.push(['food', defFood]);
    if (defCoal > 0) weights.push(['coal', defCoal]);
    if (defOre  > 0) weights.push(['ore',  defOre]);
    const totalW = weights.reduce((s, [, v]) => s + v, 0);
    let r = Math.random() * totalW;
    let picked = weights[0][0];
    for (const [t, v] of weights) {
      if (r < v) { picked = t; break; }
      r -= v;
    }

    // 偷取量（策划案 §35.5）
    let stealAmount = 0;
    if (picked === 'food') stealAmount = 3 + Math.floor(Math.random() * 6);  // 3~8
    else if (picked === 'coal') stealAmount = 2 + Math.floor(Math.random() * 4);  // 2~5
    else if (picked === 'ore')  stealAmount = 1 + Math.floor(Math.random() * 3);  // 1~3

    // 不超过当前库存
    const stock = picked === 'food' ? defFood : picked === 'coal' ? defCoal : defOre;
    stealAmount = Math.max(0, Math.min(stealAmount, stock));
    if (stealAmount <= 0) return;

    // ── 扣防守方 + 加给攻击方 ──
    if (picked === 'food') {
      defEngine.food = Math.max(0, defEngine.food - stealAmount);
      atkEngine.food = Math.min(2000, atkEngine.food + stealAmount);
      this.stolenFood += stealAmount;
    } else if (picked === 'coal') {
      defEngine.coal = Math.max(0, defEngine.coal - stealAmount);
      atkEngine.coal = Math.min(1500, atkEngine.coal + stealAmount);
      this.stolenCoal += stealAmount;
    } else {
      defEngine.ore = Math.max(0, defEngine.ore - stealAmount);
      atkEngine.ore = Math.min(800, atkEngine.ore + stealAmount);
      this.stolenOre += stealAmount;
    }

    // 双方资源 UI 同步
    try {
      defEngine._broadcastResourceUpdate && defEngine._broadcastResourceUpdate();
      atkEngine._broadcastResourceUpdate && atkEngine._broadcastResourceUpdate();
    } catch (e) { /* ignore */ }

    const payload = {
      sessionId: this.id,
      event: 'resource_stolen',
      detail: {
        monsterId,
        resource: picked,
        amount: stealAmount,
        totalStolenFood: this.stolenFood,
        totalStolenCoal: this.stolenCoal,
        totalStolenOre:  this.stolenOre,
      },
    };
    this._broadcastCombatReport(payload);
    console.log(`[TribeWarSession:${this.id}] Stolen: ${picked}+${stealAmount} (cumul F/C/O=${this.stolenFood}/${this.stolenCoal}/${this.stolenOre})`);
  }

  // ==================== 结束 ====================

  /**
   * 结束会话（清 timer；attack_ended 由 manager 层统一广播以保证登记表解除在广播前完成）。
   */
  end(reason) {
    if (this._ended) return;
    this._ended = true;
    if (this._noEnergyTimer) {
      clearInterval(this._noEnergyTimer);
      this._noEnergyTimer = null;
    }
    console.log(`[TribeWarSession:${this.id}] end(${reason})`);
  }

  // ==================== 内部 ====================

  _broadcastCombatReport(payload) {
    try {
      if (this.attacker && this.attacker.status !== 'destroyed') {
        this.attacker.broadcast({
          type: 'tribe_war_combat_report',
          timestamp: Date.now(),
          data: payload,
        });
      }
      if (this.defender && this.defender.status !== 'destroyed') {
        this.defender.broadcast({
          type: 'tribe_war_combat_report_defense',
          timestamp: Date.now(),
          data: payload,
        });
      }
    } catch (e) {
      console.warn(`[TribeWarSession:${this.id}] combat_report broadcast error: ${e.message}`);
    }
  }

  _getDefenderWaveConfig() {
    const defEngine = this.defender && this.defender.survivalEngine;
    if (!defEngine) return null;
    // 策划案 §35.4：远征怪属性 = 防守方当前天数普通怪
    // 通过 engine 暴露的 _getWaveConfigForDay 查询（SurvivalGameEngine 内部 require getWaveConfig）
    if (typeof defEngine._getWaveConfigForDay === 'function') {
      try {
        return defEngine._getWaveConfigForDay(defEngine.currentDay || 1);
      } catch (e) { /* fall through to default */ }
    }
    // 硬兜底：固定 30HP/3ATK
    return { normal: { hp: 30, atk: 3 } };
  }
}

module.exports = TribeWarSession;
