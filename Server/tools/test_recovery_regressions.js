const assert = require('assert');
const fs = require('fs');
const os = require('os');
const path = require('path');

const ROOT = path.join(__dirname, '..');
const SurvivalGameEngine = require(path.join(ROOT, 'src', 'SurvivalGameEngine'));
const StreamerRankingStore = require(path.join(ROOT, 'src', 'StreamerRankingStore'));

let passed = 0;
let failed = 0;

function test(name, fn) {
  try {
    fn();
    console.log(`  PASS  ${name}`);
    passed++;
  } catch (e) {
    console.error(`  FAIL  ${name}`);
    console.error(`        ${e.message}`);
    if (e.stack) console.error(`        ${e.stack.split('\n').slice(1, 3).join('\n        ')}`);
    failed++;
    process.exitCode = 1;
  }
}

function makeEngine() {
  const captures = [];
  const eng = new SurvivalGameEngine({}, (msg) => captures.push(msg));
  eng.room = { roomId: 'recovery_regression_room', clients: new Set(), broadcast: () => {} };
  eng._capturedBroadcasts = captures;
  return eng;
}

test('recovery preserves guardian identities and restores safe furnace temperature', () => {
  const eng = makeEngine();
  try {
    eng.state = 'settlement';
    eng.currentDay = 3;
    eng.contributions = { p1: 120, p2: 45 };
    eng.playerNames = { p1: 'Alice', p2: 'Bob' };
    eng._workerHp = {
      p1: { hp: 0, maxHp: 100, isDead: true, respawnAt: Date.now() + 10000 },
      p2: { hp: 30, maxHp: 100, isDead: false, respawnAt: 0 },
    };
    eng.food = 0;
    eng.coal = 0;
    eng.ore = 0;
    eng.furnaceTemp = eng.minTemp;

    eng._enterRecovery();

    assert.strictEqual(eng.state, 'recovery');
    assert.deepStrictEqual(Object.keys(eng.contributions).sort(), ['p1', 'p2']);
    assert.strictEqual(eng.contributions.p1, 0);
    assert.strictEqual(eng.contributions.p2, 0);
    assert.ok(eng.furnaceTemp >= 20, `recovery furnaceTemp should be at least 20, got ${eng.furnaceTemp}`);
    assert.strictEqual(eng._workerHp.p1.isDead, false, 'dead worker should revive during recovery');
  } finally {
    eng._clearAllTimers();
  }
});

test('recovery publishes demoted fortress day instead of stale failure day', () => {
  const eng = makeEngine();
  try {
    eng.state = 'settlement';
    eng.currentDay = 50;
    eng.fortressDay = 45;
    eng.maxFortressDay = 60;
    eng.contributions = { p1: 1 };
    eng._workerHp = { p1: { hp: 0, maxHp: 100, isDead: true, respawnAt: 0 } };

    eng._enterRecovery();

    const phase = eng._capturedBroadcasts.find(m => m.type === 'phase_changed' && m.data && m.data.variant === 'recovery');
    assert.ok(phase, 'recovery should broadcast phase_changed');
    assert.strictEqual(phase.data.day, 45);
    assert.strictEqual(eng.currentDay, 45);
  } finally {
    eng._clearAllTimers();
  }
});

test('Lv5 frost aura reduces authoritative wave damage', () => {
  const eng = makeEngine();
  try {
    eng.state = 'night';
    eng.currentDay = 1;
    eng.fortressDay = 5;
    eng.gateLevel = 5;
    eng.monsterGateDamage = 10;
    eng._workerDamageMult = 1;
    eng.contributions = { p1: 1 };
    eng._workerHp = { p1: { hp: 100, maxHp: 100, isDead: false, respawnAt: 0 } };

    eng._spawnWave({
      monsterId: 'test',
      baseCount: 1,
      maxCount: 1,
      normal: { hp: 10, atk: 1, spd: 1 },
    }, 1, 0);

    assert.strictEqual(eng._workerHp.p1.hp, 93, 'Lv5 frost aura should apply 0.7x server damage pressure');
  } finally {
    eng._clearAllTimers();
  }
});

test('settlement rankings expose full list for weekly store while keeping top10 UI list', () => {
  const eng = makeEngine();
  for (let i = 1; i <= 12; i++) {
    eng.contributions[`p${i}`] = i * 10;
  }

  const top10 = eng._buildRankings(10);
  const all = eng._buildRankings(0);

  assert.strictEqual(top10.length, 10);
  assert.strictEqual(all.length, 12);
  assert.strictEqual(all[0].rank, 1);
  assert.strictEqual(all[11].rank, 12);
});

test('clearAllTimers clears transient boosts and random event state', () => {
  const eng = makeEngine();
  eng._playerTempBoost = { p1: 1.3 };
  eng.tempDecayMultiplier = 2.0;
  eng.foodBonus = 1.5;
  eng.oreBonus = 2.0;
  eng._iceGroundEndAt = Date.now() + 30000;
  eng._heavyFogEndAt = Date.now() + 30000;
  eng._hotSpringEndAt = Date.now() + 30000;
  eng._hotSpringLastTick = 123;

  eng._clearAllTimers();

  assert.deepStrictEqual(eng._playerTempBoost, {});
  assert.strictEqual(eng.tempDecayMultiplier, 1.0);
  assert.strictEqual(eng.foodBonus, 1.0);
  assert.strictEqual(eng.oreBonus, 1.0);
  assert.strictEqual(eng._iceGroundEndAt, 0);
  assert.strictEqual(eng._heavyFogEndAt, 0);
  assert.strictEqual(eng._hotSpringEndAt, 0);
  assert.strictEqual(eng._hotSpringLastTick, 0);
});

test('max level gate restarts frost pulse after a phase transition', () => {
  const eng = makeEngine();
  try {
    eng.gateLevel = 6;
    eng._clearAllTimers();
    assert.strictEqual(eng._frostPulseTimer, null);

    eng._enterDay(1);

    assert.ok(eng._frostPulseTimer, 'Lv6 gate should restart frost pulse timer on day start');
  } finally {
    eng._clearAllTimers();
  }
});

test('updateIfBetter does not count fortress day promotion as a completed cycle', () => {
  const store = new StreamerRankingStore();
  const tmpFile = path.join(os.tmpdir(), `streamer_ranking_test_${Date.now()}_${Math.random().toString(16).slice(2)}.json`);
  try {
    store.filePath = tmpFile;
    store._data = { streamers: {} };

    store.updateIfBetter('room_a', 'Room A', 7, true);
    assert.strictEqual(store._data.streamers.room_a.maxFortressDay, 7);
    assert.strictEqual(store._data.streamers.room_a.totalCycles, 0);
    assert.strictEqual(store._data.streamers.room_a.totalGames, 0);

    store.addGameResult('room_a', 'Room A', 'normal', 3, 'lose');
    assert.strictEqual(store._data.streamers.room_a.totalCycles, 1);
    assert.strictEqual(store._data.streamers.room_a.totalGames, 1);
    assert.strictEqual(store._data.streamers.room_a.maxFortressDay, 7);
  } finally {
    try { if (fs.existsSync(tmpFile)) fs.unlinkSync(tmpFile); } catch (_) {}
  }
});

console.log('');
console.log(`=== Recovery Regression Test Summary ===`);
console.log(`Passed: ${passed}`);
console.log(`Failed: ${failed}`);
console.log(`Total : ${passed + failed}`);
if (failed > 0) {
  console.log('Result: FAIL');
  process.exit(1);
} else {
  console.log('Result: PASS');
}
