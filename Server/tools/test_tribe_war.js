const assert = require('assert');
const TribeWarSession = require('../src/TribeWarSession');

function makeRoom(roomId, engine) {
  const broadcasts = [];
  return {
    roomId,
    streamerName: roomId,
    status: 'active',
    survivalEngine: engine,
    broadcasts,
    broadcast(msg) {
      broadcasts.push(msg);
    },
  };
}

function makeEngine() {
  const broadcasts = [];
  return {
    state: 'night',
    currentDay: 1,
    food: 100,
    coal: 100,
    ore: 100,
    gateHp: 100,
    monsterGateDamage: 5,
    _workerDamageMult: 1,
    // §14 v1.27 废止 _monsterHpMult；TribeWarSession 现走 _getEarlyDayMult(fortressDay) 路径
    fortressDay: 10,  // 用 D5+ 让 _earlyDayMult=1.0（避免基线压力测试值漂移）
    _getEarlyDayMult(_d) { return 1.0; },
    _dynamicHpMult: 1,
    _themeHpMult: 1,
    _themeMonsterHpMult: 1,
    _themeMonsterAtkMult: 1,
    _monsterIdCounter: 0,
    _activeMonsters: new Map(),
    _buildingInProgress: new Map(),
    _effectiveMaxAliveMonsters() { return 15; },
    _getWaveConfigForDay() { return { normal: { hp: 30, atk: 3 } }; },
    _broadcast(msg) { broadcasts.push(msg); },
    _broadcastResourceUpdate() {},
    _broadcastBuildProgress(buildId) { broadcasts.push({ type: 'build_progress', data: { buildId } }); },
    _completeBuild(buildId) { broadcasts.push({ type: 'build_completed', data: { buildId } }); },
    broadcasts,
  };
}

function makeSession(damageMultiplier = 1.0) {
  const attackerEngine = makeEngine();
  const defenderEngine = makeEngine();
  const attacker = makeRoom('attacker', attackerEngine);
  const defender = makeRoom('defender', defenderEngine);
  const mgr = { stopAttack() {} };
  const session = new TribeWarSession('tw_test', attacker, defender, mgr, damageMultiplier);
  return { session, attackerEngine, defenderEngine, attacker, defender };
}

{
  const { session, defenderEngine, defender } = makeSession(1.5);
  for (let i = 0; i < 14; i++) {
    defenderEngine._activeMonsters.set(`m${i}`, { id: `m${i}` });
  }
  session.energy = 100;
  const beforeRelease = Date.now();
  session.releaseExpedition(5);

  assert.strictEqual(defenderEngine._activeMonsters.size, 15, 'remote monsters should respect defender cap');
  assert.strictEqual(session.energy, 80, 'only spawned remote monster should consume energy');
  const remote = [...defenderEngine._activeMonsters.values()].find(m => m.tribeWarSessionId === 'tw_test');
  assert.ok(remote, 'one remote monster should spawn');
  assert.strictEqual(remote.atk, 5, 'damageMultiplier should affect remote monster atk');
  const incoming = defender.broadcasts.find(m => m.type === 'tribe_war_expedition_incoming');
  assert.ok(incoming, 'defender should receive expedition incoming payload');
  assert.deepStrictEqual(incoming.data.monsterIds, [remote.id], 'incoming payload should carry the authoritative monster id');
  assert.strictEqual(incoming.data.monsters[0].monsterId, remote.id, 'incoming monster detail should match spawned id');
  assert.ok(incoming.data.monsters[0].earliestHitAt - beforeRelease >= 12000, 'earliestHitAt should use a travel-time ETA, not the old 5s shortcut');
  session.end('test');
}

{
  const { session, defenderEngine } = makeSession(1.0);
  defenderEngine.food = 0;
  defenderEngine.coal = 0;
  defenderEngine.ore = 0;
  defenderEngine.gateHp = 100;
  const originalRandom = Math.random;
  Math.random = () => 0.99;
  try {
    session.onExpeditionHitWorker('tw_gate_test');
  } finally {
    Math.random = originalRandom;
    session.end('test');
  }
  assert.ok(defenderEngine.gateHp < 100, 'zero-resource fallback gate damage should bypass steal probability');
}

{
  const { session, defenderEngine } = makeSession(1.0);
  const now = Date.now();
  defenderEngine._buildingInProgress.set('watchtower', {
    startedAt: now - 50000,
    completesAt: now + 50000,
    totalMs: 100000,
    timer: null,
    progressTimer: null,
  });
  session._damageBuildingSkeleton('tw_build_hit');
  const info = defenderEngine._buildingInProgress.get('watchtower');
  assert.ok(info, 'building should survive when progress remains above zero');
  assert.ok(info.completesAt > now + 50000, 'building completion should be delayed by remote hit');
  if (info.timer) clearTimeout(info.timer);
  session.end('test');
}

{
  const { session, defenderEngine } = makeSession(1.0);
  const now = Date.now();
  defenderEngine._buildingInProgress.set('market', {
    startedAt: now - 5000,
    completesAt: now + 95000,
    totalMs: 100000,
    timer: null,
    progressTimer: null,
  });
  session._damageBuildingSkeleton('tw_build_kill');
  assert.ok(!defenderEngine._buildingInProgress.has('market'), 'low-progress skeleton should be demolished');
  assert.ok(defenderEngine.broadcasts.find(m => m.type === 'build_demolished' && m.data.reason === 'attacked'), 'demolition should broadcast attacked reason');
  session.end('test');
}

{
  const { session, defenderEngine } = makeSession(1.0);
  const now = Date.now();
  defenderEngine._buildingInProgress.set('watchtower', {
    startedAt: now - 50000,
    completesAt: now + 50000,
    totalMs: 100000,
    timer: null,
    progressTimer: null,
  });
  session.energy = 20;
  const originalRandom = Math.random;
  Math.random = () => 0.0;
  try {
    session.releaseExpedition(1);
  } finally {
    Math.random = originalRandom;
  }
  assert.strictEqual(session.stolenFood + session.stolenCoal + session.stolenOre, 0, 'expedition should not steal at spawn time');
  const mid = defenderEngine._activeMonsters.keys().next().value;
  assert.strictEqual(session.handleExpeditionHit(mid, 'worker'), false, 'hit before earliestHitAt should be rejected');
  assert.strictEqual(defenderEngine._buildingInProgress.get('watchtower').completesAt, now + 50000, 'expedition should not damage building at spawn time');
  session.end('test');
}

console.log('PASS  tribe war expedition cap, damage, fallback, and building attack behavior');
