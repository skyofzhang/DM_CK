const assert = require('assert');
const SurvivalRoom = require('../src/SurvivalRoom');

function makeRoom() {
  const room = new SurvivalRoom('gift_mapping_' + Math.floor(Math.random() * 1e6), {}, null, null);
  const calls = [];
  room.survivalEngine.handleGift = (...args) => calls.push(args);
  room.survivalEngine.handlePlayerJoined = () => {};
  return { room, calls };
}

{
  const { room, calls } = makeRoom();
  room.handleDouyinGift('p1', 'Tester', '', 'unknown_gift', 2, 100);
  assert.strictEqual(calls.length, 2, 'gift_num=2 unit-price payload should settle twice');
  assert.strictEqual(calls[0][4], 100);
  assert.strictEqual(calls[1][4], 100);
  room.destroy();
}

{
  const { room, calls } = makeRoom();
  room.handleDouyinGift('p1', 'Tester', '', 'unknown_gift', 2, 200);
  assert.strictEqual(calls.length, 2, 'gift_num=2 total-price payload should normalize to unit price and settle twice');
  assert.strictEqual(calls[0][4], 100);
  assert.strictEqual(calls[1][4], 100);
  room.destroy();
}

console.log('PASS  Douyin gift_num price normalization');
