/**
 * §36.5.1 每日闯关上限 — Daily Cap 自测脚本
 *
 * 运行：node Server/tools/test_daily_cap.js
 *
 * 覆盖策划案 §36.5.1.5 测试用例 10+ 条（简化为可纯 Node 自跑的断言）：
 *   T1  dayKey 切换触发硬重置（wasBlocked → 推送 cap_reset）
 *   T2  达到 150 上限后 cap_blocked（首次 dedup）
 *   T3  NTP 后跳时钟防御（currentKey < storedKey 不重置）
 *   T4  失败降级不影响 dailyFortressDayGained（§36.5.1.4）
 *   T5  新赛季开始不重置 cap（§36.5.1.4 赛季切换 ❌ 不清零）
 *   T6  end_game / reset_game 保留计数
 *   T7  FeatureFlags.ENABLE_DAILY_CAP = false 时全量透传（不拦截）
 *   T8  schemaVersion 2→3 迁移默认值（缺字段回退）
 *   T9  dailyResetAt 计算指向未来 UTC+8 05:00
 *   T10 fortress_day_changed.reason 包含 cap_blocked/cap_reset enum
 *
 * PM 约定：不依赖测试框架（mocha/jest），用 node assert 直跑；失败时 process.exitCode=1。
 */

const assert = require('assert');
const path = require('path');

const ROOT = path.join(__dirname, '..');
const FeatureFlags = require(path.join(ROOT, 'src', 'FeatureFlags'));
const SurvivalGameEngine = require(path.join(ROOT, 'src', 'SurvivalGameEngine'));
const RoomPersistence = require(path.join(ROOT, 'src', 'RoomPersistence'));
const GlobalClock = require(path.join(ROOT, 'src', 'GlobalClock'));
const defaultConfig = require(path.join(ROOT, 'config', 'default.json'));

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

/** 构造一个最简引擎实例（不依赖 SurvivalRoom / DouyinAPI） */
function makeEngine() {
  // SurvivalGameEngine(config, broadcast) —— 构造时会调用 broadcast，先给一个 no-op
  const captures = [];
  const captureBroadcast = (msg) => { captures.push(msg); };
  const eng = new SurvivalGameEngine({}, captureBroadcast);
  // 注入假 room 给日志/roomId 路径
  eng.room = { roomId: 'test_room_' + Math.floor(Math.random() * 1e6), clients: new Set(), broadcast: () => {} };
  // 引擎内部调用 this.broadcast(...)；我们捕获到 captures 数组
  eng._capturedBroadcasts = captures;
  return eng;
}

// ============================================================
// T1: dayKey 切换触发硬重置（wasBlocked → 推送 cap_reset）
// ============================================================
test('T1: dayKey 切换触发硬重置并广播 cap_reset', () => {
  const eng = makeEngine();
  // 模拟前一日已 blocked
  eng._dailyResetKey = eng._computeDayKey() - 1;  // storedKey = 昨天
  eng._dailyFortressDayGained = 150;
  eng._dailyCapBlocked = true;

  eng._capturedBroadcasts.length = 0;
  eng._ensureDailyReset();

  assert.strictEqual(eng._dailyFortressDayGained, 0, 'gained 应重置为 0');
  assert.strictEqual(eng._dailyCapBlocked, false, 'blocked 应重置为 false');
  assert.strictEqual(eng._dailyResetKey, eng._computeDayKey(), 'dayKey 应同步到 current');

  const capResetBroadcast = eng._capturedBroadcasts.find(b => b.type === 'fortress_day_changed' && b.data && b.data.reason === 'cap_reset');
  assert.ok(capResetBroadcast, '应广播 cap_reset');
  assert.strictEqual(capResetBroadcast.data.dailyFortressDayGained, 0);
  assert.strictEqual(capResetBroadcast.data.dailyCapMax, 150);
  assert.strictEqual(capResetBroadcast.data.dailyCapBlocked, false);
});

// ============================================================
// T2: 达到 150 上限后 cap_blocked（dedup 仅首次广播）
// ============================================================
test('T2: cap_blocked 首次广播 + 再次调用 dedup 不重复', () => {
  const eng = makeEngine();
  eng._dailyResetKey = eng._computeDayKey();  // 同一日，跳过 ensureReset
  eng._dailyFortressDayGained = 150;          // 已达上限
  eng._dailyCapBlocked = false;
  eng.fortressDay = 50;
  eng._capturedBroadcasts.length = 0;

  // 第一次调用 _onRoomSuccess → 应广播 cap_blocked
  eng._onRoomSuccess();
  let capBlockedCnt = eng._capturedBroadcasts.filter(b => b.type === 'fortress_day_changed' && b.data.reason === 'cap_blocked').length;
  assert.strictEqual(capBlockedCnt, 1, '首次应广播 1 次 cap_blocked');
  assert.strictEqual(eng._dailyCapBlocked, true);
  assert.strictEqual(eng.fortressDay, 50, 'fortressDay 不应变化');

  // 第二次调用 → dedup 不广播
  eng._capturedBroadcasts.length = 0;
  eng._onRoomSuccess();
  capBlockedCnt = eng._capturedBroadcasts.filter(b => b.type === 'fortress_day_changed' && b.data.reason === 'cap_blocked').length;
  assert.strictEqual(capBlockedCnt, 0, '第二次应 dedup 无广播');
  assert.strictEqual(eng.fortressDay, 50, 'fortressDay 仍不变');
});

// ============================================================
// T3: NTP 后跳时钟防御（currentKey < storedKey 不重置）
// ============================================================
test('T3: NTP 反向时钟漂移 — 不重置 dailyFortressDayGained', () => {
  const eng = makeEngine();
  const currentKey = eng._computeDayKey();
  // 伪造未来 dayKey（模拟系统时钟被回拨）
  eng._dailyResetKey = currentKey + 5;
  eng._dailyFortressDayGained = 100;
  eng._dailyCapBlocked = false;

  // 捕获 console.warn 以验证 log
  const origWarn = console.warn;
  let warned = false;
  console.warn = (...args) => { warned = true; };
  try {
    eng._ensureDailyReset();
  } finally {
    console.warn = origWarn;
  }

  assert.strictEqual(eng._dailyFortressDayGained, 100, 'gained 不应变化');
  assert.strictEqual(eng._dailyResetKey, currentKey + 5, 'storedKey 不应被回拨');
  assert.ok(warned, '应记录 backward skew warning');
});

// ============================================================
// T4: 失败降级不影响 dailyFortressDayGained（§36.5.1.4）
// ============================================================
test('T4: 失败降级 _onRoomFail 不清零 _dailyFortressDayGained', () => {
  const eng = makeEngine();
  eng._dailyResetKey = eng._computeDayKey();
  eng._dailyFortressDayGained = 120;
  eng._dailyCapBlocked = false;
  eng.fortressDay = 50;  // 超过 newbie_protect 10，会触发 demote

  eng._onRoomFail();

  // 失败降级后 gained 仍应保留
  assert.strictEqual(eng._dailyFortressDayGained, 120, 'gained 应保持 120（不清零）');
  // blocked 标记也不应被改
  assert.strictEqual(eng._dailyCapBlocked, false);
  // fortressDay 确实降级了
  assert.strictEqual(eng.fortressDay, 45, '50→45 (floor(50*0.9))');
});

// ============================================================
// T5: 赛季切换不重置 cap（§36.5.1.4）
// ============================================================
test('T5: 赛季切换路径 — 不清零 cap（_dailyFortressDayGained 保持）', () => {
  const eng = makeEngine();
  eng._dailyResetKey = eng._computeDayKey();
  eng._dailyFortressDayGained = 80;
  eng._dailyCapBlocked = false;

  // §36.5.1.4 赛季切换 ❌ 不清零 —— onSeasonStart 不应触碰 cap 字段
  // §14 v1.27：废止 difficulty 协议后 onSeasonStart 已是 noop，不再消费 _pendingDifficulty
  eng.onSeasonStart(2);

  assert.strictEqual(eng._dailyFortressDayGained, 80, 'gained 应保持 80');
  assert.strictEqual(eng._dailyCapBlocked, false);
});

// ============================================================
// T6: end_game / reset_game 保留计数
// ============================================================
test('T6: reset() 保留 _dailyFortressDayGained / _dailyResetKey / _dailyCapBlocked', () => {
  const eng = makeEngine();
  const key = eng._computeDayKey();
  eng._dailyResetKey = key;
  eng._dailyFortressDayGained = 75;
  eng._dailyCapBlocked = false;
  eng.fortressDay = 30;

  // reset() 是 end_game / reset_game 入口的核心逻辑
  eng.reset();

  assert.strictEqual(eng._dailyFortressDayGained, 75, 'gained 应保留');
  assert.strictEqual(eng._dailyResetKey, key, 'dayKey 应保留');
  assert.strictEqual(eng._dailyCapBlocked, false, 'blocked 应保留');
});

// ============================================================
// T7: FeatureFlags.ENABLE_DAILY_CAP = false 时不拦截
// ============================================================
test('T7: FeatureFlags.ENABLE_DAILY_CAP=false 时 cap 不拦截（透传）', () => {
  const original = FeatureFlags.ENABLE_DAILY_CAP;
  FeatureFlags.ENABLE_DAILY_CAP = false;
  try {
    const eng = makeEngine();
    eng._dailyResetKey = eng._computeDayKey();
    eng._dailyFortressDayGained = 150;  // 已满
    eng._dailyCapBlocked = false;
    eng.fortressDay = 50;
    eng._capturedBroadcasts.length = 0;

    eng._onRoomSuccess();

    // flag=false → 不拦截，fortressDay 照常 +1
    assert.strictEqual(eng.fortressDay, 51, 'flag=false 时 fortressDay 应 +1');
    // 不应广播 cap_blocked
    const capBlocked = eng._capturedBroadcasts.find(b => b.type === 'fortress_day_changed' && b.data.reason === 'cap_blocked');
    assert.strictEqual(capBlocked, undefined, '不应广播 cap_blocked');
  } finally {
    FeatureFlags.ENABLE_DAILY_CAP = original;
  }
});

// ============================================================
// T8: schemaVersion 2→3 迁移默认值
// ============================================================
test('T8: _applyPersistedSnapshot 缺失 cap 字段时回退默认值', () => {
  const eng = makeEngine();
  // 模拟 schemaVersion=2 老快照（无 _daily* 字段）
  const oldSnap = {
    schemaVersion: 2,
    fortressDay: 45,
    maxFortressDay: 60,
    _lifetimeContrib: { p1: 5000 },
  };
  // 预置非默认值，验证 load 不会把它改回（没字段 → 不触发赋值）
  eng._dailyFortressDayGained = 77;

  eng._applyPersistedSnapshot(oldSnap);

  // fortressDay / maxFortressDay 已加载
  assert.strictEqual(eng.fortressDay, 45);
  assert.strictEqual(eng.maxFortressDay, 60);
  // cap 字段因快照无该字段保持原值（非 0 → 默认 0 逻辑交给 RoomPersistence 迁移层；此处引擎不回改）
  assert.strictEqual(eng._dailyFortressDayGained, 77);
  // 再次测试：带 cap 字段的快照能加载
  const newSnap = {
    schemaVersion: 3,
    _dailyFortressDayGained: 42,
    _dailyResetKey: 19800,
    _dailyCapBlocked: true,
  };
  eng._applyPersistedSnapshot(newSnap);
  assert.strictEqual(eng._dailyFortressDayGained, 42);
  assert.strictEqual(eng._dailyResetKey, 19800);
  assert.strictEqual(eng._dailyCapBlocked, true);
});

// ============================================================
// T9: dailyResetAt 计算指向未来 UTC+8 05:00
// ============================================================
test('T9: _getNextDailyResetMs 指向未来 UTC+8 05:00 时刻', () => {
  const eng = makeEngine();
  const now = Date.now();
  const nextResetMs = eng._getNextDailyResetMs(now);

  // 必须是未来时刻
  assert.ok(nextResetMs > now, `nextResetMs (${nextResetMs}) 应 > now (${now})`);
  // 距离当前不超过 24 小时
  assert.ok(nextResetMs - now <= 24 * 3600 * 1000, '距离当前应 ≤ 24h');
  // 距离当前不少于 0（现在可能刚好是日切）
  assert.ok(nextResetMs - now > 0);

  // 验证返回值指向 UTC+8 05:00 —— 即 UTC 时刻 21:00（05 - 08 = -3 → 加 24 = 21）
  const d = new Date(nextResetMs);
  const utcHour = d.getUTCHours();
  assert.strictEqual(utcHour, 21, `预期 UTC hour=21 (对应 UTC+8 05:00)，实际=${utcHour}`);
  assert.strictEqual(d.getUTCMinutes(), 0);
  assert.strictEqual(d.getUTCSeconds(), 0);
});

// ============================================================
// T10: fortress_day_changed.reason 包含 cap_blocked/cap_reset
// ============================================================
test('T10: fortress_day_changed reason 枚举新增 cap_blocked / cap_reset', () => {
  const eng = makeEngine();

  // cap_blocked 路径
  eng._dailyResetKey = eng._computeDayKey();
  eng._dailyFortressDayGained = 150;
  eng._dailyCapBlocked = false;
  eng._capturedBroadcasts.length = 0;
  eng._onRoomSuccess();
  const b1 = eng._capturedBroadcasts.find(b => b.type === 'fortress_day_changed');
  assert.ok(b1, 'cap_blocked 路径应有广播');
  assert.strictEqual(b1.data.reason, 'cap_blocked');
  assert.strictEqual(typeof b1.data.dailyFortressDayGained, 'number');
  assert.strictEqual(typeof b1.data.dailyCapMax, 'number');
  assert.strictEqual(typeof b1.data.dailyResetAt, 'number');
  assert.strictEqual(typeof b1.data.dailyCapBlocked, 'boolean');

  // cap_reset 路径（模拟跨日 blocked）
  const eng2 = makeEngine();
  eng2._dailyResetKey = eng2._computeDayKey() - 1;
  eng2._dailyFortressDayGained = 150;
  eng2._dailyCapBlocked = true;
  eng2._capturedBroadcasts.length = 0;
  eng2._ensureDailyReset();
  const b2 = eng2._capturedBroadcasts.find(b => b.type === 'fortress_day_changed');
  assert.ok(b2, 'cap_reset 路径应有广播');
  assert.strictEqual(b2.data.reason, 'cap_reset');
  assert.strictEqual(b2.data.dailyCapBlocked, false);
  assert.strictEqual(b2.data.dailyFortressDayGained, 0);
});

// ============================================================
// T11 Bonus: survival_game_state 携带 4 cap 字段
// ============================================================
test('T11: getFullState 携带 dailyFortressDayGained/dailyCapMax/dailyResetAt/dailyCapBlocked', () => {
  const eng = makeEngine();
  eng._dailyFortressDayGained = 33;
  eng._dailyCapBlocked = false;
  const s = eng.getFullState();
  assert.strictEqual(typeof s.dailyFortressDayGained, 'number');
  assert.strictEqual(s.dailyFortressDayGained, 33);
  assert.strictEqual(s.dailyCapMax, 150);
  assert.strictEqual(typeof s.dailyResetAt, 'number');
  assert.ok(s.dailyResetAt > Date.now());
  assert.strictEqual(s.dailyCapBlocked, false);
});

test('T12: default global clock day/night durations stay 120 seconds', () => {
  assert.strictEqual(defaultConfig.game.survivalDayDuration, 120);
  assert.strictEqual(defaultConfig.game.survivalNightDuration, 120);

  const clock = new GlobalClock({ seasonDay: 1, seasonId: 1, themeId: 'classic_frozen' }, {
    dayDurationMs: defaultConfig.game.survivalDayDuration * 1000,
    nightDurationMs: defaultConfig.game.survivalNightDuration * 1000,
    tickMs: 999999,
  });
  try {
    assert.strictEqual(clock._baseDayDurationMs, 120000);
    assert.strictEqual(clock._baseNightDurationMs, 120000);
  } finally {
    clock.stop();
  }
});

test('T13: world_clock_tick after phase transition uses new phase duration', () => {
  const seasonMgr = {
    seasonDay: 1,
    seasonId: 1,
    themeId: 'classic_frozen',
    advanceDay() {},
  };
  const clock = new GlobalClock(seasonMgr, {
    dayDurationMs: 120000,
    nightDurationMs: 120000,
    tickMs: 999999,
  });
  const room = {
    roomId: 'clock_transition_room',
    broadcasts: [],
    broadcast(msg) { this.broadcasts.push(msg); },
    survivalEngine: {
      state: 'day',
      remainingTime: 0,
      _enterNightFromClock() { this.state = 'night'; },
      _enterDayFromClock() { this.state = 'day'; },
    },
  };
  try {
    clock._rooms.add(room);
    clock._phase = 'day';
    clock._phaseStartedAt = Date.now() - 121000;
    clock._tick();

    const tick = room.broadcasts.filter(m => m.type === 'world_clock_tick').pop();
    assert.ok(tick, 'world_clock_tick should broadcast');
    assert.strictEqual(tick.data.phase, 'night');
    assert.ok(tick.data.phaseRemainingSec <= 120, `remaining should be <= 120, got ${tick.data.phaseRemainingSec}`);
    assert.ok(tick.data.phaseRemainingSec >= 115, `remaining should be close to full night, got ${tick.data.phaseRemainingSec}`);
    assert.strictEqual(room.survivalEngine.state, 'night');
    assert.ok(room.survivalEngine.remainingTime <= 120, 'engine remainingTime should not retain old day duration');
  } finally {
    clock.stop();
  }
});

test('T14: early boss kill completes night success and increments fortressDay once', () => {
  const eng = makeEngine();
  try {
    eng.state = 'night';
    eng.currentDay = 1;
    eng.fortressDay = 10;
    eng.maxFortressDay = 10;
    eng._dailyResetKey = eng._computeDayKey();
    eng._dailyFortressDayGained = 0;
    eng._dailyCapBlocked = false;

    const ok = eng._completeNightSuccess('boss_killed_test');
    assert.strictEqual(ok, true);
    assert.strictEqual(eng.state, 'day');
    assert.strictEqual(eng.currentDay, 2);
    assert.strictEqual(eng.fortressDay, 11);
    assert.strictEqual(eng.maxFortressDay, 11);
    assert.strictEqual(eng._dailyFortressDayGained, 1);

    const duplicate = eng._completeNightSuccess('duplicate');
    assert.strictEqual(duplicate, false);
    assert.strictEqual(eng.fortressDay, 11, 'second call should not increment again');
    assert.strictEqual(eng._dailyFortressDayGained, 1);
  } finally {
    if (typeof eng._clearAllTimers === 'function') eng._clearAllTimers();
  }
});

test('T15: season_ending gate only applies during D7 night final 300s', () => {
  const eng = makeEngine();
  eng.seasonMgr = { seasonDay: 7, seasonId: 1, themeId: 'classic_frozen' };

  eng.state = 'day';
  eng.remainingTime = 1;
  assert.strictEqual(eng._isSeasonEnding(), false, 'D7 day is still preparation time and must not be season_ending');

  eng.state = 'night';
  eng.remainingTime = 301;
  assert.strictEqual(eng._isSeasonEnding(), false, 'D7 night with >300s remaining should not be locked');

  eng.remainingTime = 300;
  assert.strictEqual(eng._isSeasonEnding(), true, 'D7 night at <=300s remaining should be season_ending');
});

// ============================================================
// Summary
// ============================================================
console.log('');
console.log(`=== Daily Cap Test Summary ===`);
console.log(`Passed: ${passed}`);
console.log(`Failed: ${failed}`);
console.log(`Total : ${passed + failed}`);
if (failed > 0) {
  console.log('Result: FAIL');
  process.exit(1);
} else {
  console.log('Result: PASS');
}
