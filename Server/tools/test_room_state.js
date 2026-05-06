const assert = require('assert');
const SurvivalGameEngine = require('../src/SurvivalGameEngine');

const captures = [];
const engine = new SurvivalGameEngine({}, (msg) => captures.push(msg));
engine.state = 'day';
engine.currentDay = 1;
engine.fortressDay = 8;
engine.maxFortressDay = 9;
engine.seasonMgr = { seasonId: 2, seasonDay: 4, themeId: 'blood_moon' };
engine.room = {
  roomId: 'room_state_test',
  streamerName: 'State Tester',
  streamerRanking: { _data: { streamers: {} } },
};

const now = Date.now();
engine._roulette._pending = {
  cardId: 'aurora',
  displayedCards: ['aurora', 'meteor_shower', 'time_freeze'],
  spunAt: now,
  autoApplyAt: now + 10000,
};
engine._roulette._readyAt = now + 300000;

engine._buildVote = {
  proposalId: 'prop_1',
  proposerName: 'Builder',
  options: ['watchtower', 'market', 'hospital'],
  votingEndsAt: now + 45000,
  votes: new Map([['p1', 'watchtower'], ['p2', 'market'], ['p3', 'watchtower']]),
};

engine._expeditions.set('exp_1', {
  playerId: 'p1',
  workerIdx: 2,
  startAt: now - 5000,
  returnsAt: now + 85000,
  eventId: 'trader_caravan',
  eventEndsAt: now + 15000,
  options: ['accept', 'cancel'],
});

const session = {
  _ended: false,
  attacker: { roomId: 'room_state_test' },
  defender: { roomId: 'target_room', survivalEngine: { _activeMonsters: new Map([['tw_m1', { tribeWarSessionId: 'tw_1' }]]) } },
  energy: 40,
  stolenFood: 3,
  stolenCoal: 2,
  stolenOre: 1,
  expeditionsSent: 4,
  startedAt: now - 10000,
  damageMultiplier: 1.5,
};
engine.tribeWarMgr = {
  _attackerToSession: new Map([['room_state_test', 'tw_1']]),
  _defenderToSession: new Map(),
  _sessions: new Map([['tw_1', session]]),
};

engine._broadcastRoomState('test');
const msg = captures.find(x => x.type === 'room_state');
assert.ok(msg, 'room_state should be broadcast');
const data = msg.data;

assert.strictEqual(data.roulette.phase, 'pending');
assert.strictEqual(data.roulette.pending.cardId, 'aurora');
assert.deepStrictEqual(data.roulette.pending.displayedCards, ['aurora', 'meteor_shower', 'time_freeze']);

assert.strictEqual(data.build.voting.proposalId, 'prop_1');
assert.deepStrictEqual(data.build.voting.voteBuildIds.sort(), ['market', 'watchtower']);
assert.strictEqual(data.build.voting.totalVoters, 3);

assert.strictEqual(data.expeditions[0].playerId, 'p1');
assert.strictEqual(data.expeditions[0].eventPhase.eventId, 'trader_caravan');
assert.deepStrictEqual(data.expeditions[0].eventPhase.options, ['accept', 'cancel']);

assert.strictEqual(data.tribeWar.role, 'attacker');
assert.strictEqual(data.tribeWar.targetRoomId, 'target_room');
assert.strictEqual(data.tribeWar.energyAccumulated, 40);
assert.strictEqual(data.tribeWar.remoteMonstersAlive, 1);
assert.deepStrictEqual(data.tribeWar.stolenResources, { food: 3, coal: 2, ore: 1 });
assert.strictEqual(data.tribeWar.stats.damageMultiplier, 1.5);

engine._clearAllTimers();
console.log('PASS  room_state in-progress payload includes roulette/build/expedition/tribeWar details');
