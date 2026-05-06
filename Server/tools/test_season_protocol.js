const assert = require('assert');
const SeasonManager = require('../src/SeasonManager');

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
    _lifetimeContrib: { p1: 1000 },
    playerNames: { p1: 'Player One' },
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
  season.advanceDay(new Set([room]));
} finally {
  global.setTimeout = originalSetTimeout;
}

const settlement = broadcasts.find(m => m.type === 'season_settlement');
assert.ok(settlement, 'season_settlement should broadcast');
assert.strictEqual(settlement.data.seasonId, 3);
assert.strictEqual(settlement.data.themeId, 'snowstorm');
assert.strictEqual(settlement.data.nextSeasonId, 4);
assert.strictEqual(settlement.data.nextThemeId, 'dawn');

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

console.log('PASS  season settlement/started protocol fields');
