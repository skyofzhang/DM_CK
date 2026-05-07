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

test('baseline preset ignores stale config decay overrides', () => {
  const eng = new SurvivalGameEngine({
    foodDecayDay: 0.1,
    foodDecayNight: 0.1,
    tempDecayDay: 0.3,
    tempDecayNight: 0.8,
  }, () => {});
  try {
    eng._initBaselinePreset();
    assert.strictEqual(eng.foodDecayDay, 1.0);
    assert.strictEqual(eng.foodDecayNight, 1.0);
    assert.strictEqual(eng.tempDecayDay, 0.15);
    assert.strictEqual(eng.tempDecayNight, 0.40);
  } finally {
    eng._clearAllTimers();
  }
});

test('stop clears full runtime timer set for destroyed rooms', () => {
  const eng = makeEngine();
  try {
    eng._playerTempBoostTimers.p1 = setTimeout(() => {}, 10000);
    eng._randomEventTimers.push(setTimeout(() => {}, 10000));
    eng._shopSpotlightTimers.p1 = setTimeout(() => {}, 10000);
    eng._giftImpactTimers.push(setTimeout(() => {}, 10000));
    eng.stop();
    assert.deepStrictEqual(eng._playerTempBoostTimers, {});
    assert.deepStrictEqual(eng._randomEventTimers, []);
    assert.deepStrictEqual(eng._shopSpotlightTimers, {});
    assert.deepStrictEqual(eng._giftImpactTimers, []);
    assert.strictEqual(eng._roomPaused, true);
  } finally {
    eng._clearAllTimers();
  }
});

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
  eng._giftPauseUntil = Date.now() + 30000;

  eng._clearAllTimers();

  assert.deepStrictEqual(eng._playerTempBoost, {});
  assert.strictEqual(eng.tempDecayMultiplier, 1.0);
  assert.strictEqual(eng.foodBonus, 1.0);
  assert.strictEqual(eng.oreBonus, 1.0);
  assert.strictEqual(eng._iceGroundEndAt, 0);
  assert.strictEqual(eng._heavyFogEndAt, 0);
  assert.strictEqual(eng._hotSpringEndAt, 0);
  assert.strictEqual(eng._hotSpringLastTick, 0);
  assert.strictEqual(eng._giftPauseUntil, 0);
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

test('pause/resume restarts main tick, night waves, and in-progress builds', () => {
  const eng = makeEngine();
  try {
    const now = Date.now();
    eng.state = 'night';
    eng.currentDay = 4;
    eng.remainingTime = 60;
    eng.seasonMgr = { seasonDay: 4 };
    eng._peaceNightSkipSpawn = false;
    eng._buildingInProgress.set('watchtower', {
      startedAt: now,
      totalMs: 20000,
      completesAt: now + 20000,
      remainingMs: 0,
      paused: false,
      timer: setTimeout(() => {}, 20000),
      progressTimer: setInterval(() => {}, 2000),
    });

    eng.pause();
    const info = eng._buildingInProgress.get('watchtower');
    assert.strictEqual(eng._tickTimer, null, 'pause should stop tick timer');
    assert.strictEqual(eng._waveTimers.length, 0, 'pause should clear active wave timers');
    assert.strictEqual(info.pausedByRoom, true, 'pause should mark build as room-paused');
    assert.ok(info.remainingMs > 0, 'pause should retain build remaining time');

    eng.resume();

    assert.ok(eng._tickTimer, 'resume should restart tick timer');
    assert.ok(eng._waveTimers.length > 0, 'resume should restart night wave scheduling');
    assert.ok(info.timer, 'resume should restart build completion timer');
    assert.ok(info.progressTimer, 'resume should restart build progress timer');
    assert.strictEqual(info.paused, false, 'resume should unpause room-paused build');
  } finally {
    eng._clearAllTimers();
  }
});

test('pause/resume freezes room runtime effect timers', () => {
  const eng = makeEngine();
  try {
    const now = Date.now();
    eng._liveRankingTimer = setTimeout(() => {}, 30000);
    eng.playerNames.p1 = 'Alice';
    eng._playerTempBoost.p1 = 1.3;
    eng._schedulePlayerTempBoostExpiry('p1', now + 30000);
    eng._shopSpotlightActive.p1 = { endsAt: now + 10000 };
    eng._scheduleShopSpotlightExpiry('p1', now + 10000);
    eng._freezeUntilMs = now + 30000;
    eng._eliteRaidEndsAt = now + 30000;
    eng.tempDecayMultiplier = 2.0;
    eng._scheduleRandomEventTimer('E01_snowstorm', 30000, () => { eng.tempDecayMultiplier = 1.0; });
    eng._scheduleMeteorTick(30000);
    eng._contribMult = 2.0;
    eng._scheduleContribMultExpiry(now + 30000);
    eng._roulette._effectActive = { cardId: 'aurora', endsAt: now + 30000 };
    eng._auroraEffMult = 1.5;
    eng._scheduleAuroraExpiry(now + 30000);
    eng._traderOffer = { expiresAt: now + 30000, cardA: { costFood: 1 }, cardB: { costCoal: 1 } };
    eng._scheduleTraderExpiry(now + 30000);

    eng.pause();

    assert.strictEqual(eng._liveRankingTimer, null, 'pause should clear live ranking debounce');
    assert.strictEqual(eng._playerTempBoostTimers.p1, undefined, 'pause should clear temp boost timer handle');
    assert.ok(eng._pausedPlayerTempBoostRemainingMs.p1 > 0, 'pause should retain temp boost remaining time');
    assert.strictEqual(eng._randomEventTimers.length, 1, 'pause should retain tracked random event entry');
    assert.strictEqual(eng._randomEventTimers[0].timer, null, 'pause should clear random event timer handle');
    assert.strictEqual(eng._meteorTimers.length, 1, 'pause should retain pending meteor tick entry');
    assert.strictEqual(eng._meteorTimers[0].timer, null, 'pause should clear meteor timer handle');
    assert.strictEqual(eng._contribMultTimer, null, 'pause should clear double contrib timer handle');
    assert.ok(eng._pausedContribMultRemainingMs > 0, 'pause should retain double contrib remaining time');
    assert.strictEqual(eng._auroraTimer, null, 'pause should clear aurora timer handle');
    assert.ok(eng._pausedAuroraRemainingMs > 0, 'pause should retain aurora remaining time');
    assert.strictEqual(eng._traderTimer, null, 'pause should clear trader timer handle');
    assert.ok(eng._pausedTraderRemainingMs > 0, 'pause should retain trader remaining time');
    assert.strictEqual(eng._freezeUntilMs, 0, 'pause should clear freeze wall-clock field');
    assert.ok(eng._pausedFreezeRemainingMs > 0, 'pause should retain freeze remaining time');
    assert.strictEqual(eng._eliteRaidEndsAt, 0, 'pause should clear elite raid wall-clock field');
    assert.ok(eng._pausedEliteRaidRemainingMs > 0, 'pause should retain elite raid remaining time');
    assert.strictEqual(eng._shopSpotlightTimers.p1, undefined, 'pause should clear spotlight timer handle');
    assert.ok(eng._shopSpotlightActive.p1.pausedRemainingMs > 0, 'pause should retain spotlight remaining time');

    eng._capturedBroadcasts.length = 0;
    eng.resume();

    assert.ok(eng._playerTempBoostTimers.p1, 'resume should restore temp boost timer');
    assert.ok(eng._randomEventTimers[0].timer, 'resume should restore random event timer');
    assert.ok(eng._meteorTimers[0].timer, 'resume should restore meteor tick timer');
    assert.ok(eng._contribMultTimer, 'resume should restore double contrib timer');
    assert.ok(eng._auroraTimer, 'resume should restore aurora timer');
    assert.ok(eng._traderTimer, 'resume should restore trader timer');
    assert.ok(eng._freezeUntilMs > Date.now(), 'resume should restore freeze wall-clock field');
    assert.ok(eng._eliteRaidEndsAt > Date.now(), 'resume should restore elite raid wall-clock field');
    assert.ok(eng._shopSpotlightTimers.p1, 'resume should restore spotlight timer');
    const spotlight = eng._capturedBroadcasts.find(m => m.type === 'shop_effect_triggered' && m.data && m.data.itemId === 'spotlight');
    assert.ok(spotlight, 'resume should rebroadcast spotlight effect for reconnecting clients');
    assert.strictEqual(spotlight.data.targetPlayerId, 'p1');
    assert.ok(spotlight.data.durationSec > 0, 'spotlight resume broadcast should carry remaining duration');
    assert.ok(eng._roulette._effectActive.endsAt > Date.now(), 'resume should shift roulette effect end time');
  } finally {
    eng._clearAllTimers();
  }
});

test('pause/resume freezes all expedition phases', () => {
  const eng = makeEngine();
  try {
    const now = Date.now();
    eng._expeditions.set('exp_out', {
      expeditionId: 'exp_out',
      playerId: 'p1',
      playerName: 'P1',
      workerIdx: 0,
      startAt: now,
      outboundEndsAt: now + 40000,
      returnsAt: now + 90000,
      outboundSec: 40,
      eventSec: 15,
      returnSec: 35,
      eventId: null,
      eventEndsAt: 0,
      options: null,
      userChoice: null,
      outcome: null,
      outboundTimer: setTimeout(() => {}, 40000),
      eventTimer: null,
      returnTimer: null,
    });
    eng._expeditions.set('exp_evt', {
      expeditionId: 'exp_evt',
      playerId: 'p2',
      playerName: 'P2',
      workerIdx: 1,
      startAt: now - 40000,
      outboundEndsAt: now - 1,
      returnsAt: now + 50000,
      outboundSec: 40,
      eventSec: 15,
      returnSec: 35,
      eventId: 'lost_cache',
      eventEndsAt: now + 15000,
      options: null,
      userChoice: null,
      outcome: null,
      outboundTimer: null,
      eventTimer: setTimeout(() => {}, 15000),
      returnTimer: null,
    });
    eng._expeditions.set('exp_ret', {
      expeditionId: 'exp_ret',
      playerId: 'p3',
      playerName: 'P3',
      workerIdx: 2,
      startAt: now - 55000,
      outboundEndsAt: now - 20000,
      returnsAt: now + 35000,
      outboundSec: 40,
      eventSec: 15,
      returnSec: 35,
      eventId: 'lost_cache',
      eventEndsAt: now - 1,
      options: null,
      userChoice: null,
      outcome: { type: 'success', resources: null, contributions: 0, died: false },
      outboundTimer: null,
      eventTimer: null,
      returnTimer: setTimeout(() => {}, 35000),
    });

    eng.pause();

    for (const exp of eng._expeditions.values()) {
      assert.strictEqual(exp.outboundTimer, null, 'pause should clear outbound timer handles');
      assert.strictEqual(exp.eventTimer, null, 'pause should clear event timer handles');
      assert.strictEqual(exp.returnTimer, null, 'pause should clear return timer handles');
      assert.strictEqual(exp.pausedByRoom, true, 'pause should mark expedition room-paused');
      assert.ok(exp.pausedRemainingMs > 0, 'pause should retain expedition remaining time');
    }

    eng.resume();

    assert.ok(eng._expeditions.get('exp_out').outboundTimer, 'resume should restore outbound timer');
    assert.ok(eng._expeditions.get('exp_evt').eventTimer, 'resume should restore event timer');
    assert.ok(eng._expeditions.get('exp_ret').returnTimer, 'resume should restore return timer');
    assert.strictEqual(eng._expeditions.get('exp_out').pausedByRoom, false);
    assert.strictEqual(eng._expeditions.get('exp_evt').pausedByRoom, false);
    assert.strictEqual(eng._expeditions.get('exp_ret').pausedByRoom, false);
    assert.ok(eng._expeditions.get('exp_evt').eventEndsAt > Date.now(), 'resume should shift event end time');
    assert.ok(eng._expeditions.get('exp_ret').returnsAt > Date.now(), 'resume should shift return time');
  } finally {
    eng._clearAllTimers();
  }
});

test('room_state expedition phase follows outcome over eventEndsAt wall-clock', () => {
  const eng = makeEngine();
  try {
    const now = Date.now();
    eng._expeditions.set('exp_returning', {
      expeditionId: 'exp_returning',
      playerId: 'p1',
      playerName: 'P1',
      workerIdx: 0,
      startAt: now - 40000,
      outboundEndsAt: now - 1,
      returnsAt: now + 35000,
      outboundSec: 40,
      eventSec: 15,
      returnSec: 35,
      eventId: 'trader_caravan',
      eventEndsAt: now + 15000,
      options: ['accept', 'cancel'],
      userChoice: 'accept',
      outcome: { type: 'success', resources: null, contributions: 0, died: false },
      outboundTimer: null,
      eventTimer: null,
      returnTimer: setTimeout(() => {}, 35000),
    });

    const inProgress = eng._buildRoomStateInProgress();
    assert.ok(inProgress && Array.isArray(inProgress.expeditions), 'room_state should expose expeditions');
    assert.strictEqual(inProgress.expeditions[0].phase, 'return', 'outcome must take precedence over future eventEndsAt');
  } finally {
    eng._clearAllTimers();
  }
});

test('resume while not paused only rebroadcasts state', () => {
  const eng = makeEngine();
  try {
    eng.state = 'night';
    eng.currentDay = 4;
    eng.remainingTime = 60;
    eng._roomPaused = false;

    eng.resume();

    assert.strictEqual(eng._tickTimer, null, 'active resume should not restart tick');
    assert.strictEqual(eng._waveTimers.length, 0, 'active resume should not schedule waves');
    assert.ok(eng._capturedBroadcasts.find(m => m.type === 'survival_game_state'), 'active resume should still rebroadcast snapshot');
  } finally {
    eng._clearAllTimers();
  }
});

test('pause/resume preserves settlement-to-recovery timer', () => {
  const eng = makeEngine();
  try {
    eng.state = 'settlement';
    eng._settleEndsAt = Date.now() + 10000;
    eng._settleTimerHandle = setTimeout(() => {}, 10000);
    eng._recoveryTimer = eng._settleTimerHandle;

    eng.pause();

    assert.strictEqual(eng._settleTimerHandle, null, 'pause should clear live settlement timer');
    assert.ok(eng._pausedSettlementRemainingMs > 0, 'pause should retain settlement remaining time');

    eng.resume();

    assert.ok(eng._settleTimerHandle, 'resume should restore settlement timer');
    assert.strictEqual(eng._recoveryTimer, eng._settleTimerHandle, 'compat recovery handle should point to restored settlement timer');
  } finally {
    eng._clearAllTimers();
  }
});

test('pause/resume freezes active build vote window', () => {
  const eng = makeEngine();
  try {
    eng.state = 'day';
    eng.currentDay = 1;
    eng.seasonMgr = { seasonId: 1, seasonDay: 3 };
    eng.room.seasonMgr = eng.seasonMgr;
    eng.contributions = { p1: 1, p2: 1 };
    eng.playerNames = { p1: 'Builder', p2: 'Voter' };
    eng.totalPlayers = 2;
    eng.food = 999;
    eng.coal = 999;
    eng.ore = 999;

    eng.handleBuildPropose('p1', 'Builder', 'watchtower');
    assert.ok(eng._buildVote && eng._buildVote.timer, 'proposal should create active vote timer');

    eng.pause();

    const vote = eng._buildVote;
    assert.ok(vote, 'pause should keep vote object');
    assert.strictEqual(vote.timer, null, 'pause should stop build vote timer');
    assert.strictEqual(vote.pausedByRoom, true, 'pause should mark build vote as room-paused');
    assert.ok(vote.pausedRemainingMs > 0, 'pause should retain vote remaining time');

    eng._capturedBroadcasts.length = 0;
    eng.resume();

    assert.ok(vote.timer, 'resume should restore build vote timer');
    assert.strictEqual(vote.pausedByRoom, false, 'resume should unpause build vote');
    assert.ok(vote.votingEndsAt > Date.now(), 'resume should refresh votingEndsAt');
    assert.ok(eng._capturedBroadcasts.find(m => m.type === 'build_vote_started'), 'resume should rebuild vote UI');
    assert.ok(eng._capturedBroadcasts.find(m => m.type === 'build_vote_update'), 'resume should sync vote tally');
  } finally {
    eng._clearAllTimers();
  }
});

test('recovery allows command 6 to clear lingering monsters', () => {
  const eng = makeEngine();
  try {
    eng.state = 'recovery';
    eng.contributions = { p1: 0 };
    eng.playerNames = { p1: 'Alice' };
    eng._workerHp = { p1: { hp: 100, maxHp: 100, isDead: false, respawnAt: 0 } };
    eng._activeMonsters.set('m_recovery', {
      id: 'm_recovery',
      type: 'normal',
      variant: 'normal',
      maxHp: 5,
      currentHp: 5,
      atk: 1,
      spd: 1,
    });

    eng.handleComment('p1', 'Alice', '', '6');

    assert.strictEqual(eng._activeMonsters.has('m_recovery'), false, 'recovery attack should kill lingering monster');
    assert.ok(eng._capturedBroadcasts.find(m => m.type === 'combat_attack'), 'recovery attack should emit combat_attack');
    assert.ok(eng._capturedBroadcasts.find(m => m.type === 'monster_died'), 'recovery attack should emit monster_died');
    assert.strictEqual(eng.state, 'recovery', 'clearing lingering monsters should not leave recovery state');
  } finally {
    eng._clearAllTimers();
  }
});

test('gift pause delays scheduled wave spawn instead of spawning immediately', () => {
  const eng = makeEngine();
  try {
    eng.state = 'night';
    eng.currentDay = 4;
    eng.remainingTime = 60;
    eng.fortressDay = 4;
    eng._giftPauseUntil = Date.now() + 200;

    eng._spawnWave({
      monsterId: 'paused_wave',
      baseCount: 1,
      maxCount: 1,
      normal: { hp: 10, atk: 1, spd: 1 },
    }, 4, 0);

    assert.ok(!eng._capturedBroadcasts.find(m => m.type === 'monster_wave'), 'gift pause should suppress immediate monster_wave');
    assert.ok(eng._waveTimers.length > 0, 'gift pause should enqueue delayed wave timer');
    assert.strictEqual(eng._activeMonsters.size, 0, 'gift pause should not spawn monsters immediately');
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
