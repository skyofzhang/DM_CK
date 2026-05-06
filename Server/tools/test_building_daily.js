const assert = require('assert');
const SurvivalGameEngine = require('../src/SurvivalGameEngine');

function makeEngine(captures, seasonId = 1, seasonDay = 3) {
  const engine = new SurvivalGameEngine({}, (msg) => captures.push(msg));
  engine.state = 'day';
  engine.currentDay = 1;
  engine.seasonMgr = { seasonId, seasonDay };
  engine.room = {
    roomId: 'build_daily_test',
    roomCreatorOpenId: 'p1',
    creator: { isVeteran: true },
    seasonMgr: engine.seasonMgr,
    clients: new Set(),
  };
  engine.contributions.p1 = 1;
  engine.playerNames.p1 = 'Builder';
  engine.totalPlayers = 1;
  engine.ore = 999;
  engine.coal = 999;
  engine.food = 999;
  return engine;
}

function findMsg(list, type, reason) {
  return list.find(msg => msg.type === type && (!reason || (msg.data && msg.data.reason === reason)));
}

let captures = [];
const first = makeEngine(captures, 1, 3);
first.handleBuildPropose('p1', 'Builder', 'watchtower');
assert.ok(first._buildVote, 'first proposal should start a vote');
first._closeBuildVote();
assert.strictEqual(first._dailyBuildVoteUsed['1:3'], true, 'vote usage should persist under seasonId:seasonDay key');
first._clearAllTimers();

captures = [];
const restoredSameDay = makeEngine(captures, 1, 3);
restoredSameDay._dailyBuildVoteUsed = { ...first._dailyBuildVoteUsed };
restoredSameDay._syncBuildVoteUsedFlagForCurrentDay();
restoredSameDay.handleBuildPropose('p1', 'Builder', 'market');
assert.ok(findMsg(captures, 'build_propose_failed', 'daily_limit'), 'restored same day should reject second proposal');
restoredSameDay._clearAllTimers();

captures = [];
const nextSeasonSameDay = makeEngine(captures, 2, 3);
nextSeasonSameDay._dailyBuildVoteUsed = { ...first._dailyBuildVoteUsed };
nextSeasonSameDay._syncBuildVoteUsedFlagForCurrentDay();
nextSeasonSameDay.handleBuildPropose('p1', 'Builder', 'market');
assert.ok(nextSeasonSameDay._buildVote, 'same seasonDay in next season should not be blocked by previous season key');
nextSeasonSameDay._closeBuildVote();
nextSeasonSameDay._clearAllTimers();

captures = [];
const legacySnapshot = makeEngine(captures, 1, 3);
legacySnapshot._applyPersistedSnapshot({
  schemaVersion: 3,
  currentSeasonId: 1,
  seasonSnapshot: { seasonId: 1, seasonDay: 3, themeId: 'snowstorm' },
  _dailyBuildVoteUsed: { '3': true },
});
assert.strictEqual(legacySnapshot._dailyBuildVoteUsed['1:3'], true, 'legacy seasonDay key should migrate to scoped key');
legacySnapshot._syncBuildVoteUsedFlagForCurrentDay();
legacySnapshot.handleBuildPropose('p1', 'Builder', 'altar');
assert.ok(findMsg(captures, 'build_propose_failed', 'daily_limit'), 'legacy same-day snapshot should still reject second proposal');
legacySnapshot._clearAllTimers();

captures = [];
const legacyNextSeason = makeEngine(captures, 2, 3);
legacyNextSeason._dailyBuildVoteUsed = { ...legacySnapshot._dailyBuildVoteUsed };
legacyNextSeason._syncBuildVoteUsedFlagForCurrentDay();
legacyNextSeason.handleBuildPropose('p1', 'Builder', 'altar');
assert.ok(legacyNextSeason._buildVote, 'migrated legacy key should not block the same day number in a later season');
legacyNextSeason._closeBuildVote();
legacyNextSeason._clearAllTimers();

captures = [];
const finalizeInsufficient = makeEngine(captures, 1, 4);
finalizeInsufficient.handleBuildPropose('p1', 'Builder', 'watchtower');
assert.ok(finalizeInsufficient._buildVote, 'proposal should start before resources are depleted');
finalizeInsufficient.ore = 0;
finalizeInsufficient.handleBuildVote('p1', finalizeInsufficient._buildVote.proposalId, 'watchtower');
assert.ok(
  findMsg(captures, 'build_cancelled', 'insufficient_resources_at_finalize'),
  'finalize-time resource failure should use protocol reason insufficient_resources_at_finalize'
);
finalizeInsufficient._clearAllTimers();

console.log('PASS  §37 daily build vote persistence and season isolation');
