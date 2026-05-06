const assert = require('assert');
const SeasonManager = require('../src/SeasonManager');
const GlobalClock = require('../src/GlobalClock');

const season = new SeasonManager();
season.seasonId = 3;
season.seasonDay = 7;
season.themeId = 'snowstorm';
season._nextThemeId = 'dawn';

const broadcasts = [];
const room = {
  broadcast(msg) { broadcasts.push(msg); },
  survivalEngine: {
    fortressDay: 12,
    _seasonFailed: false,
    _lifetimeContrib: { p1: 1000 },
    playerNames: { p1: 'Player One' },
    _broadcastRoomState() {},
    loadSeason() {},
  },
};
const failedRoom = {
  broadcast(msg) { broadcasts.push(msg); },
  survivalEngine: {
    fortressDay: 99,
    _seasonFailed: true,
    _lifetimeContrib: { p2: 5000 },
    playerNames: { p2: 'Failed Player' },
    _broadcastRoomState() {},
    loadSeason() {},
  },
};

const originalSetTimeout = global.setTimeout;
global.setTimeout = (fn) => {
  fn();
  return { unref() {} };
};
try {
  season.advanceDay(new Set([room, failedRoom]));
} finally {
  global.setTimeout = originalSetTimeout;
}

const settlement = broadcasts.find(m => m.type === 'season_settlement');
assert.ok(settlement, 'season_settlement should broadcast');
assert.strictEqual(settlement.data.seasonId, 3);
assert.strictEqual(settlement.data.themeId, 'snowstorm');
assert.strictEqual(settlement.data.nextSeasonId, 4);
assert.strictEqual(settlement.data.nextThemeId, 'dawn');
assert.strictEqual(settlement.data.survivingRooms, 1, 'survivingRooms should exclude rooms failed in the season');

const started = broadcasts.find(m => m.type === 'season_started');
assert.ok(started, 'season_started should broadcast');
assert.strictEqual(started.data.seasonId, 4);
assert.strictEqual(started.data.seasonDay, 1);
assert.ok(started.data.themeId, 'season_started should include themeId');
assert.strictEqual(started.data.themeId, 'dawn', 'first 6 seasons should use fixed theme order');

const seq = new SeasonManager();
assert.strictEqual(seq._resolveThemeForSeason(1), 'classic_frozen');
assert.strictEqual(seq._resolveThemeForSeason(2), 'blood_moon');
assert.strictEqual(seq._resolveThemeForSeason(3), 'snowstorm');
assert.strictEqual(seq._resolveThemeForSeason(4), 'dawn');
assert.strictEqual(seq._resolveThemeForSeason(5), 'frenzy');
assert.strictEqual(seq._resolveThemeForSeason(6), 'serene');

const randomSeason = new SeasonManager();
randomSeason.seasonId = 6;
randomSeason.seasonDay = 7;
randomSeason.themeId = 'serene';
randomSeason._nextThemeId = null;

const randomBroadcasts = [];
const randomRoom = {
  broadcast(msg) { randomBroadcasts.push(msg); },
  survivalEngine: {
    fortressDay: 22,
    _lifetimeContrib: {},
    playerNames: {},
    _broadcastRoomState() {},
    loadSeason() {},
  },
};

const originalRandom = Math.random;
const originalSetTimeout2 = global.setTimeout;
let randomIndex = 0;
Math.random = () => [0.0, 0.99][randomIndex++] ?? 0.0;
global.setTimeout = (fn) => {
  fn();
  return { unref() {} };
};
try {
  randomSeason.advanceDay(new Set([randomRoom]));
} finally {
  Math.random = originalRandom;
  global.setTimeout = originalSetTimeout2;
}

const randomSettlement = randomBroadcasts.find(m => m.type === 'season_settlement');
const randomStarted = randomBroadcasts.find(m => m.type === 'season_started');
assert.ok(randomSettlement, 'random branch should broadcast season_settlement');
assert.ok(randomStarted, 'random branch should broadcast season_started');
assert.strictEqual(
  randomStarted.data.themeId,
  randomSettlement.data.nextThemeId,
  'random S7+ nextThemeId and started themeId must come from the same draw',
);

const repeatSeason = new SeasonManager();
repeatSeason.themeId = 'serene';
const originalRandom3 = Math.random;
Math.random = () => 0.999;
try {
  assert.strictEqual(
    repeatSeason._resolveThemeForSeason(7, 'serene'),
    'serene',
    'S7+ theme random should allow repeating the previous theme',
  );
} finally {
  Math.random = originalRandom3;
}

{
  const bossSeason = new SeasonManager();
  bossSeason.seasonId = 8;
  bossSeason.seasonDay = 7;
  bossSeason.themeId = 'serene';
  bossSeason._nextThemeId = 'classic_frozen';

  const bossBroadcasts = [];
  const engine = {
    state: 'night',
    _seasonFailed: false,
    _lifetimeContrib: {},
    playerNames: {},
    _enterDayFromClock() {
      this.state = 'day';
      this.enteredDay = true;
    },
    _broadcastRoomState() {},
    loadSeason() {},
  };
  const bossRoom = {
    roomId: 'boss-room',
    broadcast(msg) { bossBroadcasts.push(msg); },
    survivalEngine: engine,
  };

  const clock = new GlobalClock(bossSeason, { tickMs: 999999 });
  const originalSetTimeout4 = global.setTimeout;
  global.setTimeout = (fn) => {
    fn();
    return { unref() {} };
  };
  try {
    clock.registerRoom(bossRoom);
    clock._phase = 'night';
    clock._initBossRushForD7Night();
    const state = clock.getBossRushState();
    clock.damageBossRushPool(state.hpPool);
  } finally {
    global.setTimeout = originalSetTimeout4;
    clock.stop();
  }

  assert.ok(bossBroadcasts.find(m => m.type === 'season_boss_rush_killed'), 'BossRush kill should broadcast killed');
  assert.ok(bossBroadcasts.find(m => m.type === 'season_settlement'), 'BossRush kill should trigger season settlement');
  assert.ok(bossBroadcasts.find(m => m.type === 'season_started'), 'BossRush kill should start next season');
  assert.strictEqual(bossSeason.seasonDay, 1, 'BossRush kill should wrap seasonDay to 1');
  assert.strictEqual(clock._phase, 'day', 'BossRush kill settlement should return world clock to day');
  assert.strictEqual(engine.enteredDay, true, 'BossRush kill should complete room night success callbacks');
  assert.strictEqual(clock.getBossRushState().seasonId, null, 'BossRush pool should reset after settlement');
}

console.log('PASS  season settlement/started protocol fields');
