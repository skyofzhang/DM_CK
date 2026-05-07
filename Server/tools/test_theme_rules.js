const assert = require('assert');
const SurvivalGameEngine = require('../src/SurvivalGameEngine');

{
  const captures = [];
  const engine = new SurvivalGameEngine({}, (msg) => captures.push(msg));
  engine.seasonMgr = { seasonId: 2, seasonDay: 1, themeId: 'blood_moon' };
  engine.state = 'day';
  engine.food = 0;

  engine.handleGift('p_donut', 'Donut Player', '', 'donut', 520);

  assert.strictEqual(engine.food, 120, 'blood_moon should apply +20% to T3 food drop');
  const giftMsg = captures.find(m => m.type === 'survival_gift');
  assert.ok(giftMsg, 'T3 should broadcast survival_gift');
  assert.strictEqual(giftMsg.data.effects.addFood, 120, 'T3 broadcast effect should include themed food amount');
  engine._clearAllTimers();
}

{
  const captures = [];
  const engine = new SurvivalGameEngine({}, (msg) => captures.push(msg));
  engine.seasonMgr = { seasonId: 2, seasonDay: 1, themeId: 'blood_moon' };
  engine.state = 'day';
  engine.food = 0;
  engine.coal = 0;
  engine.ore = 0;

  engine.handleGift('p_airdrop', 'Airdrop Player', '', 'mystery_airdrop', 5200);

  assert.strictEqual(engine.food, 600, 'blood_moon should apply +20% to T6 food drop');
  assert.strictEqual(engine.coal, 240, 'blood_moon should apply +20% to T6 coal drop');
  assert.strictEqual(engine.ore, 120, 'blood_moon should apply +20% to T6 ore drop');
  const giftMsg = captures.find(m => m.type === 'survival_gift');
  assert.ok(giftMsg, 'T6 should broadcast survival_gift');
  assert.strictEqual(giftMsg.data.effects.addFood, 600, 'T6 broadcast effect should include themed food amount');
  assert.strictEqual(giftMsg.data.effects.addCoal, 240, 'T6 broadcast effect should include themed coal amount');
  assert.strictEqual(giftMsg.data.effects.addOre, 120, 'T6 broadcast effect should include themed ore amount');
  engine._clearAllTimers();
}

{
  const engine = new SurvivalGameEngine({}, () => {});
  engine.seasonMgr = { seasonId: 3, seasonDay: 1, themeId: 'snowstorm' };
  assert.strictEqual(engine._effectiveThemeDayMiningMult(), 0.9, 'snowstorm day mining penalty should resolve before first night init');
  engine.seasonMgr = { seasonId: 1, seasonDay: 1, themeId: 'classic_frozen' };
  assert.strictEqual(engine._effectiveThemeDayMiningMult(), 1.0, 'classic theme should keep day mining baseline');
  engine._clearAllTimers();
}

{
  const engine = new SurvivalGameEngine({}, () => {});
  engine.seasonMgr = { seasonId: 3, seasonDay: 2, themeId: 'snowstorm' };
  engine.state = 'night';
  engine.ore = 0;
  engine._applyWorkEffect(3, 'p_snow_miner');
  assert.strictEqual(engine.ore, 3, 'snowstorm night mining should guarantee +1 ore on top of base mining');
  engine._clearAllTimers();
}

{
  const engine = new SurvivalGameEngine({}, () => {});
  engine.seasonMgr = { seasonId: 1, seasonDay: 2, themeId: 'classic_frozen' };
  engine.state = 'night';
  engine.ore = 0;
  engine._applyWorkEffect(3, 'p_classic_miner');
  assert.strictEqual(engine.ore, 2, 'classic night mining should keep base ore only');
  engine._clearAllTimers();
}

console.log('PASS  season theme resource and mining multipliers');
