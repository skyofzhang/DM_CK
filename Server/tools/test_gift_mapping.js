const assert = require('assert');
const SurvivalRoom = require('../src/SurvivalRoom');
const { getUnresolvedDouyinGifts, assertDouyinGiftIdsReady } = require('../src/GiftConfig');

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

{
  const { room, calls } = makeRoom();
  room.handleDouyinGift('p1', 'Tester', '', 'unknown_gift', 2, 20);
  assert.strictEqual(calls.length, 2, 'gift_num=2 fairy_wand total-price payload should normalize to 1 Douyin coin');
  assert.strictEqual(calls[0][4], 10);
  assert.strictEqual(calls[1][4], 10);
  room.destroy();
}

{
  const unresolved = getUnresolvedDouyinGifts();
  if (unresolved.length > 0) {
    assert.throws(
      () => assertDouyinGiftIdsReady(),
      /Unresolved douyin_id placeholders/,
      'production validation should reject TBD douyin_id placeholders'
    );
    assert.doesNotThrow(
      () => assertDouyinGiftIdsReady({ allowPlaceholders: true }),
      'local/test validation may explicitly allow TBD placeholders'
    );
  } else {
    assert.doesNotThrow(() => assertDouyinGiftIdsReady(), 'filled Douyin ids should pass validation');
  }
}

console.log('PASS  Douyin gift_num price normalization');
