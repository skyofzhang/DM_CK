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
 * MVP 口径（🔴 audit-r46 GAP-M-02 codex 方案 D 重构后）：
 *   - 远征怪 spawn 后立即追加到 defender._activeMonsters（占 maxAliveMonsters 名额）；
 *     服务端给每只远征怪 emit `earliestHitAt`（spawn + 按距离/速度估算的 travel ETA）字段。
 *   - 客户端 MonsterController 命中目标动画时上报 `tribe_war_expedition_hit { sessionId, monsterId, targetType }`，
 *     服务端 handleExpeditionHit() 校验 earliestHitAt + monsterId 后调真正结算（_damageBuildingSkeleton + onExpeditionHitWorker）。
 *   - Fallback 定时器：spawn 后 EXPEDITION_FALLBACK_MS（30s）仍未收到 hit 事件 → 强制 instant-settle 1 次保底。
 *   - 防重放：每只 monsterId 仅结算 1 次（_resolvedHitMonsters Set 跟踪）；客户端伪造的早于 earliestHitAt 命中事件被拒绝。
 *   - 历史背景：r1-r45 长期使用「spawn 后 3-4.6s 立即 instant-settle」MVP 简化（_scheduleExpeditionHit），
 *     违反策划案 §35.5「命中目标」/ §37.4「近身攻击骨架」明文。codex 评估后改为本节方案 D 混合模式。
 */

// 🔴 audit-r46 GAP-M-02 codex 方案 D 常量
//   远征怪 ETA：按 Unity 刷怪点 SpawnTop z≈28 → 城门 z≈-4、MonsterWaveSpawner 速度公式估算。
//   EXPEDITION_FALLBACK_MS：spawn 后该时长仍未收到客户端 hit 事件 → 服务端强制 instant-settle 1 次保底（防丢包）
const EXPEDITION_SPAWN_TOP_Z        = 28;
const EXPEDITION_GATE_Z             = -4;
const EXPEDITION_BASE_SPEED         = 1.8;
const EXPEDITION_DAY_SPEED_BONUS    = 0.1;
const EXPEDITION_ATTACK_WINDUP_MS   = 1000;
const EXPEDITION_TIMING_BUFFER_MS   = 1000;
const EXPEDITION_MIN_TRAVEL_TIME_MS = 12000;
const EXPEDITION_FALLBACK_MS        = 30000;

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
    this.startedAt = Date.now();  // §35 P2 战报用（与 startAt 语义一致，显式命名便于读者）
    this.lastEnergyAt = Date.now();
    this.damageMultiplier = (typeof damageMultiplier === 'number') ? damageMultiplier : 1.0;

    this._ended = false;
    this._expeditionHitTimers = new Set();
    // 🔴 audit-r46 GAP-M-02 codex 方案 D：
    //   _expeditionMonsters: monsterId → { earliestHitAt, fallbackTimer, resolved }
    //   防重放 + earliestHitAt 校验 + fallback 定时器跟踪
    this._expeditionMonsters = new Map();

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

  _estimateExpeditionTravelMs(defEngine) {
    const day = Math.max(1, (defEngine && defEngine.currentDay) || 1);
    const speed = Math.max(0.1, EXPEDITION_BASE_SPEED + day * EXPEDITION_DAY_SPEED_BONUS);
    const distance = Math.abs(EXPEDITION_SPAWN_TOP_Z - EXPEDITION_GATE_Z);
    const travelMs = Math.ceil((distance / speed) * 1000)
      + EXPEDITION_ATTACK_WINDUP_MS
      + EXPEDITION_TIMING_BUFFER_MS;
    return Math.max(EXPEDITION_MIN_TRAVEL_TIME_MS, travelMs);
  }

  // ==================== 能量 ====================

  addEnergy(delta) {
    if (this._ended) return;
    if (!delta || delta <= 0) return;
    this.energy += delta;
    this.lastEnergyAt = Date.now();

    // 战报：能量增加(前端 detail 为 string,服务端预格式化为中文展示)
    const payload = {
      sessionId: this.id,
      eventName: 'energy_added',  // 前端 C# `event` 是关键字,用 `eventName` 对齐
      detail: `能量 +${delta} (总计 ${this.energy},已派 ${this.expeditionsSent} 只远征怪)`,
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

    const defEngine = this.defender && this.defender.survivalEngine;
    if (!defEngine || defEngine.state !== 'night') {
      console.warn(`[TribeWarSession:${this.id}] releaseExpedition: defender not in night (state=${defEngine && defEngine.state})`);
      return;
    }

    const maxAlive = (typeof defEngine._effectiveMaxAliveMonsters === 'function')
      ? defEngine._effectiveMaxAliveMonsters()
      : 15;
    const currentAlive = (defEngine._activeMonsters && typeof defEngine._activeMonsters.size === 'number')
      ? defEngine._activeMonsters.size
      : 0;
    const capacity = Math.max(0, maxAlive - currentAlive);
    const spawnCount = Math.min(count, capacity);
    if (spawnCount <= 0) {
      this._broadcastCombatReport({
        sessionId: this.id,
        eventName: 'expedition_blocked_by_cap',
        detail: `防守方怪物已达上限 ${currentAlive}/${maxAlive},远征怪暂缓释放`,
      });
      console.log(`[TribeWarSession:${this.id}] releaseExpedition blocked by cap: alive=${currentAlive}/${maxAlive}`);
      return;
    }

    const cost = spawnCount * 20;
    if (this.energy < cost) return;
    this.energy -= cost;
    this.expeditionsSent += spawnCount;

    // ── 取防守方当天普通怪参数作为远征怪属性（§35.4）──
    const cfg = this._getDefenderWaveConfig();
    const baseHp  = (cfg && cfg.normal && cfg.normal.hp)  || 30;
    const baseAtk = (cfg && cfg.normal && cfg.normal.atk) || 3;
    // §14 v1.27：废止 _monsterHpMult 后链 = _dynamicHpMult × _themeHpMult × _themeMonsterHpMult × _earlyDayMult(defender.fortressDay)
    //   远征怪强度跟随防守方 fortressDay 渐进保护曲线，避免 D1-2 被 strong attacker 团灭
    const _earlyMultExp = (typeof defEngine._getEarlyDayMult === 'function')
      ? defEngine._getEarlyDayMult(defEngine.fortressDay)
      : 1.0;
    const hpMult  = (defEngine._dynamicHpMult || 1.0)
      * (defEngine._themeHpMult || 1.0)
      * (defEngine._themeMonsterHpMult || 1.0)
      * _earlyMultExp;
    // 🔴 audit-r43 GAP-E43-05：远征怪 ATK 应用 _themeMonsterAtkMult（与防守方普通怪 ATK 一致 — SurvivalGameEngine.js:5046）
    //   原 baseAtk 直接 round 未应用主题倍率 → polar_night night modifier 下普通怪 ATK ×1.2 但远征怪 ATK 不变
    //   修复：补 atkMult 与 hpMult 同结构（_themeMonsterAtkMult 默认 1.0，仅特殊主题修改）
    const atkMult = (defEngine._themeMonsterAtkMult || 1.0);
    const hp  = Math.max(1, Math.round(baseHp * hpMult));
    const atk = Math.max(1, Math.round(baseAtk * atkMult * (this.damageMultiplier || 1.0)));

    // ── 追加到 defender._activeMonsters（占 maxAliveMonsters 名额）──
    const monsterIds = [];
    const monsterPayloads = [];
    const _spawnNow = Date.now();
    const travelMs = this._estimateExpeditionTravelMs(defEngine);
    for (let i = 0; i < spawnCount; i++) {
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
      // 🔴 audit-r46 GAP-M-02 codex 方案 D：
      //   每只远征怪轻微错峰（i × 200ms）模拟出怪间隔 + 服务端估算旅行时间 = earliestHitAt
      //   客户端 MonsterController.DoAttack 命中目标动画时若 now < earliestHitAt → 拒发 hit 事件（防伪造命中）
      const earliestHitAt = _spawnNow + travelMs + i * 200;
      monsterPayloads.push({ monsterId: mid, hp, atk, earliestHitAt });
    }

    const attackerName = this.attacker.streamerName || this.attacker.roomId;

    // 🔴 audit-r45 GAP-D45-01：原同函数内同时 broadcast `monster_wave` + `tribe_war_expedition_incoming`，
    //   导致客户端 spawn 2*count 只怪：count 只灰怪（monster_wave 走 SpawnBatch 旧路径）+ count 只红色远征怪
    //   （tribe_war_expedition_incoming → MonsterWaveSpawner.SpawnTribeWarExpedition）。
    //   灰怪在客户端 _activeMonsters 但服务端 _activeMonsters 中无对应实例 → 无 monster_died 同步 → 灰怪永不死，
    //   持续撞墙/攻矿工成为"幽灵无效怪"，污染游戏体验。**audit 史最大未发现 MAJOR**
    //   修复：删除冗余的 monster_wave broadcast，让客户端只走 tribe_war_expedition_incoming 单一路径
    //   （SpawnTribeWarExpedition 渲染红色远征怪，attackerName 作为头顶名字 MVP 占位由 §35.4 GAP-D45-04 后续补完）。

    // ── 攻击方/防守方协议推送 ──
    try {
      this.attacker.broadcast({
        type: 'tribe_war_expedition_sent',
        timestamp: Date.now(),
        data: {
          sessionId: this.id,
          count: spawnCount,
          remainingEnergy: this.energy,
          damageMultiplier: this.damageMultiplier || 1.0,
        },
      });
      this.defender.broadcast({
        type: 'tribe_war_expedition_incoming',
        timestamp: Date.now(),
        data: {
          sessionId: this.id,
          count: spawnCount,
          attackerStreamerName: attackerName,
          damageMultiplier: this.damageMultiplier || 1.0,
          monsterIds,
          monsters: monsterPayloads,
        },
      });
    } catch (e) {
      console.warn(`[TribeWarSession:${this.id}] expedition protocol error: ${e.message}`);
    }

    // 🔴 audit-r46 GAP-M-02 codex 方案 D：
    //   原 r1-r45 每只远征怪 spawn 后 3-4.6s 立即 instant-settle（_scheduleExpeditionHit），违反策划案
    //   §35.5「命中目标」/ §37.4「近身攻击骨架」明文。改为客户端命中事件驱动 + 服务端 fallback 兜底。
    //   每只怪注册 fallback 定时器：spawn 后 EXPEDITION_FALLBACK_MS（30s）仍未收到 hit → 强制 instant-settle 1 次。
    for (let i = 0; i < monsterIds.length; i++) {
      const mid = monsterIds[i];
      const earliestHitAt = monsterPayloads[i].earliestHitAt;
      const fallbackTimer = setTimeout(() => {
        this._expeditionHitTimers.delete(fallbackTimer);
        if (this._ended) return;
        const meta = this._expeditionMonsters.get(mid);
        if (!meta || meta.resolved) return;
        meta.resolved = true;
        console.log(`[TribeWarSession:${this.id}] expedition fallback fire: monsterId=${mid} (客户端 hit 事件丢失，强制结算)`);
        this._resolveExpeditionHit(mid, 'fallback');
      }, EXPEDITION_FALLBACK_MS);
      this._expeditionHitTimers.add(fallbackTimer);
      this._expeditionMonsters.set(mid, { earliestHitAt, fallbackTimer, resolved: false });
    }

    console.log(`[TribeWarSession:${this.id}] releaseExpedition: count=${spawnCount}/${count}, energy→${this.energy}, expeditions=${this.expeditionsSent}, atk=${atk}, dm=${this.damageMultiplier || 1.0}, earliestHitAt(first)=${monsterPayloads[0] && monsterPayloads[0].earliestHitAt}`);
  }

  /**
   * 🔴 audit-r46 GAP-M-02 codex 方案 D：处理 C→S 远征怪命中事件
   *   客户端 MonsterController.DoAttack 命中目标时 emit `tribe_war_expedition_hit { sessionId, monsterId, targetType }`
   *   - 防伪造：sessionId 必须匹配本 session
   *   - 防重放：每只 monsterId 仅结算 1 次（_expeditionMonsters[mid].resolved 标记）
   *   - 防早发：当前时间必须 ≥ earliestHitAt（防客户端篡改时序立刻命中）
   *   - 防鬼魂：monsterId 必须仍在 defender._activeMonsters 且 currentHp > 0
   *
   * @param {string} monsterId
   * @param {string} targetType  'worker' | 'gate' | 'building_skeleton'（MVP 不区分实际目标，统一调结算）
   * @returns {boolean} 是否成功结算
   */
  handleExpeditionHit(monsterId, targetType) {
    if (this._ended) return false;
    const meta = this._expeditionMonsters.get(monsterId);
    if (!meta) {
      console.log(`[TribeWarSession:${this.id}] handleExpeditionHit reject: monsterId=${monsterId} 不属于本 session`);
      return false;
    }
    if (meta.resolved) {
      console.log(`[TribeWarSession:${this.id}] handleExpeditionHit reject: monsterId=${monsterId} 已结算（防重放）`);
      return false;
    }
    const now = Date.now();
    if (now < meta.earliestHitAt) {
      console.log(`[TribeWarSession:${this.id}] handleExpeditionHit reject: monsterId=${monsterId} now(${now}) < earliestHitAt(${meta.earliestHitAt})`);
      return false;
    }
    const defEngine = this.defender && this.defender.survivalEngine;
    const monster = defEngine && defEngine._activeMonsters && defEngine._activeMonsters.get(monsterId);
    if (!monster || monster.currentHp <= 0) {
      console.log(`[TribeWarSession:${this.id}] handleExpeditionHit reject: monsterId=${monsterId} 已不存在或已死亡`);
      return false;
    }

    meta.resolved = true;
    if (meta.fallbackTimer) {
      clearTimeout(meta.fallbackTimer);
      this._expeditionHitTimers.delete(meta.fallbackTimer);
      meta.fallbackTimer = null;
    }
    return this._resolveExpeditionHit(monsterId, targetType || 'unknown');
  }

  /**
   * 🔴 audit-r46 GAP-M-02：原 _scheduleExpeditionHit setTimeout 回调内联结算逻辑抽出来复用：
   *   1) handleExpeditionHit 客户端命中事件路径
   *   2) fallback 定时器 30s 兜底路径
   * @private
   */
  _resolveExpeditionHit(monsterId, source) {
    const defEngine = this.defender && this.defender.survivalEngine;
    if (!defEngine) return false;
    const monster = defEngine._activeMonsters && defEngine._activeMonsters.get(monsterId);
    if (!monster || monster.currentHp <= 0) return false;
    const requestedTargetType = typeof source === 'string' ? source : 'unknown';
    const targetType = (requestedTargetType === 'building_skeleton' || requestedTargetType === 'fallback')
      && this._hasDamageableBuildingSkeleton(defEngine)
      ? 'building_skeleton'
      : 'worker';
    if (targetType === 'building_skeleton') {
      if (!this._damageBuildingSkeleton(monsterId)) this.onExpeditionHitWorker(monsterId);
    } else {
      this.onExpeditionHitWorker(monsterId);
    }
    console.log(`[TribeWarSession:${this.id}] _resolveExpeditionHit: monsterId=${monsterId} target=${targetType}`);
    return true;
  }

  _hasDamageableBuildingSkeleton(defEngine) {
    return !!(defEngine
      && defEngine._buildingInProgress
      && typeof defEngine._buildingInProgress.size === 'number'
      && defEngine._buildingInProgress.size > 0);
  }

  /**
   * @deprecated 🔴 audit-r46 GAP-M-02：原 r1-r45 「spawn 后 3-4.6s 立即 instant-settle」
   *   MVP 简化路径，违反策划案 §35.5「命中目标」/ §37.4「近身攻击骨架」明文。已被 codex 方案 D 替换为：
   *     1) 客户端 MonsterController 命中目标动画时上报 → handleExpeditionHit()
   *     2) 服务端 fallback 定时器 30s 兜底（在 releaseExpedition 内联注册）
   *   保留方法签名作历史向后兼容（确认无外部调用方后可删除）。
   */
  _scheduleExpeditionHit(monsterId, delayMs) {
    console.warn(`[TribeWarSession:${this.id}] _scheduleExpeditionHit deprecated: monsterId=${monsterId} delay=${delayMs} — 应改用 handleExpeditionHit (codex 方案 D)`);
  }

  onExpeditionHitWorker(monsterId) {
    if (this._ended) return;
    const defEngine = this.defender && this.defender.survivalEngine;
    const atkEngine = this.attacker && this.attacker.survivalEngine;
    if (!defEngine || !atkEngine) return;

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
        eventName: 'fallback_gate_damage',
        detail: `防守方资源耗尽,远征怪额外攻城 ${extraDmg} HP (剩余 ${defEngine.gateHp})`,
      };
      this._broadcastCombatReport(payload);
      console.log(`[TribeWarSession:${this.id}] Fallback: defender resources all 0, gate -${extraDmg}HP → ${defEngine.gateHp}`);
      return;
    }

    // 30% 概率偷取；资源全 0 的保底攻城不受该概率阻挡。
    if (Math.random() >= 0.30) return;

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

    const pickedCn = picked === 'food' ? '食物' : picked === 'coal' ? '煤炭' : '矿石';
    const payload = {
      sessionId: this.id,
      eventName: 'resource_stolen',
      detail: `偷得 ${pickedCn} +${stealAmount} (累计 食${this.stolenFood}/煤${this.stolenCoal}/矿${this.stolenOre})`,
    };
    this._broadcastCombatReport(payload);
    console.log(`[TribeWarSession:${this.id}] Stolen: ${picked}+${stealAmount} (cumul F/C/O=${this.stolenFood}/${this.stolenCoal}/${this.stolenOre})`);
  }

  /**
   * §37.4 / §35：远征怪会攻击防守方进行中的建造骨架。
   * 服务端没有逐帧路径命中，按每只远征怪释放时结算一次 -10% 进度；归零即拆毁。
   */
  _damageBuildingSkeleton(monsterId) {
    const defEngine = this.defender && this.defender.survivalEngine;
    if (!defEngine || !defEngine._buildingInProgress || defEngine._buildingInProgress.size <= 0) return false;

    const entry = defEngine._buildingInProgress.entries().next();
    if (!entry || entry.done) return false;
    const [buildId, info] = entry.value;
    const cfg = (defEngine.constructor && defEngine.constructor.BUILDING_CATALOG && defEngine.constructor.BUILDING_CATALOG[buildId]) || null;
    const totalMs = (info && info.totalMs) || (cfg && cfg.buildMs) || 1;
    const now = Date.now();
    const oldProgress = Math.min(1, Math.max(0, (totalMs - Math.max(0, (info.completesAt || now) - now)) / totalMs));
    const newProgress = Math.max(0, oldProgress - 0.10);

    if (newProgress <= 0.0001) {
      if (info.timer) clearTimeout(info.timer);
      if (info.progressTimer) clearInterval(info.progressTimer);
      defEngine._buildingInProgress.delete(buildId);
      try {
        defEngine._broadcast({
          type: 'build_demolished',
          data: { buildId, reason: 'attacked' },
        });
      } catch (e) { /* ignore */ }
      this._broadcastCombatReport({
        sessionId: this.id,
        eventName: 'building_demolished',
        detail: `远征怪击毁建造骨架 ${buildId}`,
      });
      console.log(`[TribeWarSession:${this.id}] Building skeleton demolished by ${monsterId}: ${buildId}`);
      return true;
    }

    const remainingMs = Math.max(1, Math.ceil((1 - newProgress) * totalMs));
    if (info.timer) clearTimeout(info.timer);
    info.completesAt = now + remainingMs;
    info.timer = setTimeout(() => {
      try { defEngine._completeBuild(buildId); } catch (e) { /* ignore */ }
    }, remainingMs);
    try {
      if (typeof defEngine._broadcastBuildProgress === 'function') {
        defEngine._broadcastBuildProgress(buildId);
      }
    } catch (e) { /* ignore */ }
    this._broadcastCombatReport({
      sessionId: this.id,
      eventName: 'building_hit',
      detail: `远征怪攻击建造骨架 ${buildId},进度 -10%`,
    });
    console.log(`[TribeWarSession:${this.id}] Building skeleton hit by ${monsterId}: ${buildId} progress ${oldProgress.toFixed(2)}→${newProgress.toFixed(2)}`);
    return true;
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
    for (const timer of this._expeditionHitTimers) clearTimeout(timer);
    this._expeditionHitTimers.clear();
    // 🔴 audit-r46 GAP-M-02：清理 expedition monster 状态（防重放追踪表）
    if (this._expeditionMonsters) this._expeditionMonsters.clear();
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
