const assert = require('assert');
const SurvivalGameEngine = require('../src/SurvivalGameEngine');

{
  const engine = new SurvivalGameEngine({}, () => {});
  engine.room = null;
  engine.state = 'day';

  for (let i = 0; i < SurvivalGameEngine.MAX_PLAYERS; i++) {
    const pid = `g${i}`;
    engine.contributions[pid] = 1;
    engine.playerNames[pid] = pid;
  }
  engine.totalPlayers = SurvivalGameEngine.MAX_PLAYERS;

  engine._handleSupporterComment('supporter_1', 'Supporter One', 1);

  assert.strictEqual(engine.contributions.supporter_1, undefined, 'supporter must not enter guardian contributions');
  assert.ok(engine._supporters.has('supporter_1'), 'supporter should be registered');
  assert.ok(engine._supporters.get('supporter_1').totalContrib > 0, 'supporter contribution should be tracked separately');

  engine.state = 'night';
  engine._initWorkerHp();
  assert.strictEqual(engine._workerHp.supporter_1, undefined, 'supporter must not get a dedicated worker');
  engine._clearAllTimers();
}

{
  const captures = [];
  const engine = new SurvivalGameEngine({}, (msg) => captures.push(msg));
  engine.state = 'day';
  engine.handleComment('p_first', 'First Player', '', '1');
  const first = captures.find(m => m.type === 'first_barrage');
  assert.ok(first, 'first_barrage should broadcast');
  assert.ok(first.data.workerId >= 0, 'guardian first_barrage should include allocated workerId');
  engine._clearAllTimers();
}

{
  const engine = new SurvivalGameEngine({}, () => {});
  const variants = Array.from({ length: 6 }, (_, i) => engine._selectVariant(4, i));
  assert.strictEqual(variants.filter(v => v === 'assassin').length, 1, 'day4 should include assassin');
  assert.strictEqual(variants.filter(v => v === 'rush').length, 1, 'day4 should still include one rush');
  const variants7 = Array.from({ length: 6 }, (_, i) => engine._selectVariant(7, i));
  assert.strictEqual(variants7.filter(v => v === 'rush').length, 2, 'day7 should include two rush monsters');
  const variants15 = Array.from({ length: 8 }, (_, i) => engine._selectVariant(15, i));
  assert.strictEqual(variants15.filter(v => v === 'rush').length, 3, 'day15 should include three rush monsters');
  engine._clearAllTimers();
}

{
  const captures = [];
  const engine = new SurvivalGameEngine({}, (msg) => captures.push(msg));
  engine.room = { seasonMgr: { seasonDay: 1 } };
  engine.state = 'day';
  for (let i = 0; i < SurvivalGameEngine.MAX_PLAYERS; i++) {
    const pid = `g${i}`;
    engine.contributions[pid] = 1;
    engine.playerNames[pid] = pid;
  }
  engine.totalPlayers = SurvivalGameEngine.MAX_PLAYERS;

  const originalRandom = Math.random;
  Math.random = () => 0;
  try {
    engine.handleGift('ghost_t4', 'Ghost T4', '', 'energy_battery', 990);
  } finally {
    Math.random = originalRandom;
  }

  assert.strictEqual(engine.contributions.ghost_t4, undefined, 'D1-D5 overflow ghost must not enter guardian contributions');
  assert.strictEqual(engine._playerTempBoost.g0, 1.3, 'overflow ghost T4 should redirect boost to a guardian');
  assert.strictEqual(engine._playerTempBoost.ghost_t4, undefined, 'overflow ghost T4 must not write a sender-only boost');
  const giftMsg = captures.find(m => m.type === 'survival_gift' && m.data && m.data.playerId === 'ghost_t4');
  assert.ok(giftMsg, 'overflow ghost T4 should still broadcast survival_gift');
  assert.strictEqual(giftMsg.data.effects.redirectTargetId, 'g0');
  engine._clearAllTimers();
}

{
  const engine = new SurvivalGameEngine({}, () => {});
  engine.room = { seasonMgr: { seasonDay: 1 } };
  engine.state = 'night';
  for (let i = 0; i < SurvivalGameEngine.MAX_PLAYERS; i++) {
    const pid = `g${i}`;
    engine.contributions[pid] = 1;
    engine.playerNames[pid] = pid;
    engine._workerHp[pid] = { hp: 100, maxHp: 100, isDead: false, respawnAt: 0 };
  }
  engine.totalPlayers = SurvivalGameEngine.MAX_PLAYERS;
  engine._workerHp.g0.hp = 0;
  engine._workerHp.g0.isDead = true;

  const originalRandom = Math.random;
  Math.random = () => 0;
  try {
    engine.handleGift('ghost_t5', 'Ghost T5', '', 'love_explosion', 1990);
  } finally {
    Math.random = originalRandom;
  }

  assert.strictEqual(engine.contributions.ghost_t5, undefined, 'D1-D5 overflow ghost T5 must not enter guardian contributions');
  assert.strictEqual(engine._workerHp.g0.isDead, false, 'overflow ghost T5 should revive a random dead worker');
  assert.strictEqual(engine._workerHp.ghost_t5, undefined, 'overflow ghost T5 must not create a sender worker');
  engine._clearAllTimers();
}

console.log('PASS  supporter boundaries, first_barrage workerId, rush counts, and overflow ghost T4/T5 redirects');
