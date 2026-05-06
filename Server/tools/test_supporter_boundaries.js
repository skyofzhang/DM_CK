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

console.log('PASS  supporter boundaries, first_barrage workerId, and rush counts');
