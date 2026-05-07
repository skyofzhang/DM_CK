/**
 * §39 商店系统 MVP — 自测脚本
 *
 * 运行：node Server/tools/test_shop.js
 *
 * 覆盖策划案 §39 关键不变式 + MVP 任务清单 6 项测试：
 *   T1  A 类购买扣 contributions（不动 _contribBalance / _lifetimeContrib）
 *   T2  B 类 ≥1000 要求双确认（shop_purchase_prepare → 生成 pendingId + shop_purchase_confirm_prompt）
 *   T3  装备切换 2s 冷却（第二次 <2s 返 too_frequent）
 *   T4  弹幕命令解析（`买A1` / `装T2` 经 handleComment 分流）
 *   T5  购买窗口限制（idle/loading 拒 wrong_phase；A 类 settlement 拒）
 *   T6  _addContribution 原子同步三个字段（contributions / _lifetimeContrib / _contribBalance 同步增长）
 *
 * 覆盖面补充：
 *   - A2 gate_quickpatch 满血返 no_effect 不扣费
 *   - A4 spotlight per-game 限 1 次（激活期间 spotlight_active；激活后 limit_exceeded）
 *   - B 类 already_owned / insufficient 分支
 *   - handleShopInventory 返回 contribBalance + lifetimeContrib
 *   - loadSeason('season_1') 注入 _seasonShopPool
 *
 * 约定：不依赖 mocha/jest，直接 `node Server/tools/test_shop.js` 即可跑。
 */

const assert = require('assert');
const path = require('path');

const ROOT = path.join(__dirname, '..');
const SurvivalGameEngine = require(path.join(ROOT, 'src', 'SurvivalGameEngine'));

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

/** 构造一个最简引擎实例，捕获全部 broadcast */
function makeEngine() {
  const captures = [];
  const eng = new SurvivalGameEngine({}, (msg) => { captures.push(msg); });
  eng.room = { roomId: 'test_shop_' + Math.floor(Math.random() * 1e6), clients: new Set(), broadcast: () => {} };
  eng._capturedBroadcasts = captures;
  // 状态进入 day（购买窗口允许）
  eng.state = 'day';
  eng.gateMaxHp = 1000;
  eng.gateHp = 500;
  return eng;
}

function findBroadcast(eng, type, filter) {
  return eng._capturedBroadcasts.find(b => b.type === type && (!filter || filter(b.data)));
}

function clearBroadcasts(eng) {
  eng._capturedBroadcasts.length = 0;
}

// ============================================================
// T1: A 类购买扣 contributions（不动 _contribBalance / _lifetimeContrib）
// ============================================================
test('T1: A 类购买仅扣 contributions，不扣 _contribBalance / _lifetimeContrib', () => {
  const eng = makeEngine();
  const pid = 'p1_alice';
  // 给玩家足够 contributions + 记录基线
  eng.contributions[pid] = 300;
  eng._contribBalance[pid] = 5000;
  eng._lifetimeContrib[pid] = 5000;
  clearBroadcasts(eng);

  // 买 A1 worker_pep_talk (150)
  eng.handleShopPurchase(pid, 'Alice', 'worker_pep_talk', null);

  // 断言：contributions = 300 - 150 = 150
  assert.strictEqual(Math.round(eng.contributions[pid]), 150, 'contributions 应扣 150');
  // _contribBalance / _lifetimeContrib 不变
  assert.strictEqual(eng._contribBalance[pid], 5000, '_contribBalance 不应受 A 类影响');
  assert.strictEqual(eng._lifetimeContrib[pid], 5000, '_lifetimeContrib 不应受 A 类影响');

  // 应广播 shop_purchase_confirm（category='A'）+ shop_effect_triggered
  const purchaseOk = findBroadcast(eng, 'shop_purchase_confirm', d => d.category === 'A' && d.itemId === 'worker_pep_talk');
  assert.ok(purchaseOk, '应广播 shop_purchase_confirm { category:A }');
  const effectOk = findBroadcast(eng, 'shop_effect_triggered', d => d.itemId === 'worker_pep_talk');
  assert.ok(effectOk, '应广播 shop_effect_triggered');

  // _peptTalkBoostUntil 应设置为 now+30s（允许 ±1s 偏差）
  const now = Date.now();
  assert.ok(eng._peptTalkBoostUntil > now + 25 * 1000 && eng._peptTalkBoostUntil < now + 35 * 1000,
    `_peptTalkBoostUntil 应在 now+30s 附近，实际差 ${eng._peptTalkBoostUntil - now}ms`);
});

// ============================================================
// T2: B 类 ≥1000 要求双确认（shop_purchase_prepare）
// ============================================================
test('T2: B 类 ≥1000 prepare 生成 pendingId + 应答 shop_purchase_confirm_prompt', () => {
  const eng = makeEngine();
  const pid = 'p2_bob';
  eng._contribBalance[pid] = 10000;
  eng._lifetimeContrib[pid] = 10000;
  clearBroadcasts(eng);

  // 调 prepare：frame_silver 价格 10000（>=1000）
  eng.handleShopPurchasePrepare(pid, 'frame_silver');

  // 检查 pending 池
  const pendings = Array.from(eng._shopPendingPurchases.entries());
  assert.strictEqual(pendings.length, 1, '_shopPendingPurchases 应有 1 条');
  const [pendingId, pending] = pendings[0];
  assert.strictEqual(pending.playerId, pid);
  assert.strictEqual(pending.itemId, 'frame_silver');
  assert.strictEqual(pending.price, 10000);
  assert.ok(pending.expiresAt > Date.now() + 4000 && pending.expiresAt < Date.now() + 6000,
    'expiresAt 应在 5s TTL 内');

  // 应广播 shop_purchase_confirm_prompt
  const prompt = findBroadcast(eng, 'shop_purchase_confirm_prompt', d => d.itemId === 'frame_silver');
  assert.ok(prompt, '应广播 shop_purchase_confirm_prompt');
  assert.strictEqual(prompt.data.pendingId, pendingId);
  assert.strictEqual(prompt.data.price, 10000);

  // prepare 阶段未扣费
  assert.strictEqual(eng._contribBalance[pid], 10000, 'prepare 阶段不应扣费');
  assert.strictEqual((eng._playerShopInventory[pid] || []).length, 0, '未入库存');

  // 走第二步购买（带 pendingId）
  clearBroadcasts(eng);
  eng.handleShopPurchase(pid, 'Bob', 'frame_silver', pendingId);

  // 扣费 + 入库存
  assert.strictEqual(eng._contribBalance[pid], 0, '余额扣光');
  assert.ok(eng._playerShopInventory[pid].includes('frame_silver'), 'frame_silver 入库存');
  // pending 删除（一次性）
  assert.strictEqual(eng._shopPendingPurchases.size, 0, 'pending 应删除');
  const purchaseOk = findBroadcast(eng, 'shop_purchase_confirm', d => d.category === 'B' && d.itemId === 'frame_silver');
  assert.ok(purchaseOk, '应广播 shop_purchase_confirm { category:B }');

  // <1000 的 B1 title_supporter 不触发 prepare（直接扣费路径）
  const pid2 = 'p2_b_small';
  eng._contribBalance[pid2] = 600;
  clearBroadcasts(eng);
  eng.handleShopPurchasePrepare(pid2, 'title_supporter');  // price=500
  assert.strictEqual(eng._shopPendingPurchases.size, 0, '<1000 不进 pending');
  // 无任何广播（prepare 静默忽略 <1000）
  const silentPrompt = findBroadcast(eng, 'shop_purchase_confirm_prompt');

  clearBroadcasts(eng);
  const pid3 = 'p2_direct_hud';
  eng._contribBalance[pid3] = 10000;
  eng._lifetimeContrib[pid3] = 10000;
  eng.handleShopPurchase(pid3, 'Direct HUD', 'frame_silver', null, 'hud');
  assert.ok(findBroadcast(eng, 'shop_purchase_failed', d => d.reason === 'pending_required'), 'HUD B>=1000 must require pendingId');
  assert.strictEqual(eng._contribBalance[pid3], 10000, 'pending_required should not charge balance');

  clearBroadcasts(eng);
  const pid4 = 'p2_direct_barrage';
  eng._contribBalance[pid4] = 10000;
  eng._lifetimeContrib[pid4] = 10000;
  eng.handleShopPurchase(pid4, 'Direct Barrage', 'frame_silver', null, 'barrage');
  assert.ok(findBroadcast(eng, 'shop_purchase_confirm', d => d.category === 'B' && d.itemId === 'frame_silver'), 'barrage path may buy B>=1000 without HUD pending');
  assert.ok(!silentPrompt, '<1000 prepare 不应推 prompt');
});

// ============================================================
// T3: 装备切换 2s 冷却
// ============================================================
test('T3: 装备切换 2s 冷却（第二次 <2s 返 too_frequent）', () => {
  const eng = makeEngine();
  const pid = 'p3_cindy';
  // 拥有两件 title SKU
  eng._playerShopInventory[pid] = ['title_supporter', 'title_veteran'];
  clearBroadcasts(eng);

  // 第一次装上 title_supporter
  eng.handleShopEquip(pid, 'title', 'title_supporter');
  assert.strictEqual(eng._playerShopEquipped[pid].title, 'title_supporter');
  const ok1 = findBroadcast(eng, 'shop_equip_changed', d => d.itemId === 'title_supporter');
  assert.ok(ok1, '首次装备应广播 shop_equip_changed');

  // 立刻切到 title_veteran → 应拒 too_frequent
  clearBroadcasts(eng);
  eng.handleShopEquip(pid, 'title', 'title_veteran');
  const fail = findBroadcast(eng, 'shop_equip_failed', d => d.reason === 'too_frequent');
  assert.ok(fail, '<2s 切换应返 too_frequent');
  // 未更新
  assert.strictEqual(eng._playerShopEquipped[pid].title, 'title_supporter', '未生效');

  // 模拟 2s 过去
  eng._shopLastEquipAt[pid] = Date.now() - 2100;
  clearBroadcasts(eng);
  eng.handleShopEquip(pid, 'title', 'title_veteran');
  const ok2 = findBroadcast(eng, 'shop_equip_changed', d => d.itemId === 'title_veteran');
  assert.ok(ok2, '2s 后切换应成功');
  assert.strictEqual(eng._playerShopEquipped[pid].title, 'title_veteran');

  // slot_mismatch：把 title 装到 frame 槽 → 失败
  eng._shopLastEquipAt[pid] = Date.now() - 2100;
  clearBroadcasts(eng);
  eng.handleShopEquip(pid, 'frame', 'title_supporter');
  const mismatch = findBroadcast(eng, 'shop_equip_failed', d => d.reason === 'slot_mismatch');
  assert.ok(mismatch, 'slot 不匹配应 slot_mismatch');

  // not_owned：装备一件未购买的 SKU
  eng._shopLastEquipAt[pid] = Date.now() - 2100;
  clearBroadcasts(eng);
  eng.handleShopEquip(pid, 'title', 'title_legend_mover');  // 未 owned
  const notOwned = findBroadcast(eng, 'shop_equip_failed', d => d.reason === 'not_owned');
  assert.ok(notOwned, '未拥有应 not_owned');
});

// ============================================================
// T4: 弹幕命令解析（`买A1` / `装T2`）
// ============================================================
test('T4: 弹幕 `买A1` / `装T2` / `买B9` 经 handleComment 分流', () => {
  const eng = makeEngine();
  const pid = 'p4_dave';
  eng.contributions[pid] = 500;
  eng._contribBalance[pid] = 10000;
  eng._playerShopInventory[pid] = ['title_veteran'];  // 已拥有 B2

  // 确保 shop 功能已解锁（MVP room 未接入 seasonMgr 时 isFeatureUnlocked 按 seasonDay=1 判定——shop.minDay=2 会被拒）
  // 注入假 seasonMgr 使 seasonDay=2
  eng.seasonMgr = { seasonDay: 2 };
  eng.room.seasonMgr = eng.seasonMgr;

  // 买 A1 worker_pep_talk
  clearBroadcasts(eng);
  eng.handleComment(pid, 'Dave', '', '买A1');
  assert.strictEqual(Math.round(eng.contributions[pid]), 350, '买A1 扣 150');
  const buyA1 = findBroadcast(eng, 'shop_purchase_confirm', d => d.itemId === 'worker_pep_talk');
  assert.ok(buyA1, '弹幕买A1 应触发 shop_purchase_confirm');

  // 装 T2（title_veteran）
  eng._shopLastEquipAt[pid] = 0;  // 清冷却
  clearBroadcasts(eng);
  eng.handleComment(pid, 'Dave', '', '装T2');
  assert.strictEqual(eng._playerShopEquipped[pid].title, 'title_veteran', '装T2 应装 title_veteran');
  const equipOk = findBroadcast(eng, 'shop_equip_changed', d => d.itemId === 'title_veteran');
  assert.ok(equipOk, '弹幕装T2 应触发 shop_equip_changed');

  // 买 B9（赛季限定未配置 → item_not_found）
  clearBroadcasts(eng);
  eng.handleComment(pid, 'Dave', '', '买B9');
  const b9Fail = findBroadcast(eng, 'shop_purchase_failed', d => d.reason === 'item_not_found');
  assert.ok(b9Fail, '买B9 无赛季配置应 item_not_found');

  // 越界 买A9 静默忽略（不应触发任何 shop_* 广播）
  clearBroadcasts(eng);
  eng.handleComment(pid, 'Dave', '', '买A9');
  const silentA9 = eng._capturedBroadcasts.find(b => b.type && b.type.startsWith('shop_'));
  assert.ok(!silentA9, '买A9 越界应静默');

  // 装T0（卸下）
  eng._shopLastEquipAt[pid] = 0;
  clearBroadcasts(eng);
  eng.handleComment(pid, 'Dave', '', '装T0');
  const unequip = findBroadcast(eng, 'shop_equip_changed', d => d.itemId === '');
  assert.ok(unequip, '装T0 应卸下');
  assert.strictEqual(eng._playerShopEquipped[pid].title || '', '', '卸下后 title 为空');
});

// ============================================================
// T5: 购买窗口限制
// ============================================================
test('T5: idle/loading 拒 wrong_phase；A 类 settlement 拒；B 类 settlement 通过', () => {
  const eng = makeEngine();
  const pid = 'p5_emma';
  eng.contributions[pid] = 500;
  eng._contribBalance[pid] = 5000;

  // idle 阶段拒 A 类
  eng.state = 'idle';
  clearBroadcasts(eng);
  eng.handleShopPurchase(pid, 'Emma', 'gate_quickpatch', null);
  const idleFail = findBroadcast(eng, 'shop_purchase_failed', d => d.reason === 'wrong_phase');
  assert.ok(idleFail, 'idle 阶段 A 类应 wrong_phase');
  assert.strictEqual(eng.contributions[pid], 500, 'idle 拒后未扣费');

  // settlement 阶段 A 类拒
  eng.state = 'settlement';
  clearBroadcasts(eng);
  eng.handleShopPurchase(pid, 'Emma', 'worker_pep_talk', null);
  const settleFail = findBroadcast(eng, 'shop_purchase_failed', d => d.reason === 'wrong_phase');
  assert.ok(settleFail, 'settlement 阶段 A 类应 wrong_phase');

  // settlement 阶段 B 类通过（允许结算后买身份装备）
  eng.state = 'settlement';
  clearBroadcasts(eng);
  eng.handleShopPurchase(pid, 'Emma', 'title_supporter', null);  // 500
  const settleOk = findBroadcast(eng, 'shop_purchase_confirm', d => d.itemId === 'title_supporter');
  assert.ok(settleOk, 'settlement 阶段 B 类应通过');
  assert.ok(eng._playerShopInventory[pid].includes('title_supporter'));

  // night 阶段 gate_quickpatch（最常用场景）
  eng.state = 'night';
  eng.gateHp = 200;
  eng.gateMaxHp = 1000;
  eng.contributions[pid] = 500;
  clearBroadcasts(eng);
  eng.handleShopPurchase(pid, 'Emma', 'gate_quickpatch', null);  // 200
  const gateOk = findBroadcast(eng, 'shop_purchase_confirm', d => d.itemId === 'gate_quickpatch');
  assert.ok(gateOk, 'night 阶段修城门应通过');
  assert.strictEqual(eng.gateHp, 300, '城门 +100 HP');

  // gate_quickpatch 满血拒 no_effect
  eng.gateHp = 1000;
  eng.contributions[pid] = 500;
  clearBroadcasts(eng);
  eng.handleShopPurchase(pid, 'Emma', 'gate_quickpatch', null);
  const noEffect = findBroadcast(eng, 'shop_purchase_failed', d => d.reason === 'no_effect');
  assert.ok(noEffect, '满血应 no_effect');
  assert.strictEqual(eng.contributions[pid], 500, 'no_effect 不扣费');

  // night 阶段 A3 emergency_alert 拒（仅 day/recovery）
  eng.state = 'night';
  clearBroadcasts(eng);
  eng.handleShopPurchase(pid, 'Emma', 'emergency_alert', null);
  const alertFail = findBroadcast(eng, 'shop_purchase_failed', d => d.reason === 'wrong_phase');
  assert.ok(alertFail, 'emergency_alert 夜晚应 wrong_phase');
});

// ============================================================
// T6: _addContribution 原子同步三个字段
// ============================================================
test('T6: _addContribution 原子同步 contributions / _lifetimeContrib / _contribBalance', () => {
  const eng = makeEngine();
  const pid = 'p6_frank';

  // 基线：三字段均 0
  assert.strictEqual(eng.contributions[pid] || 0, 0);
  assert.strictEqual(eng._lifetimeContrib[pid] || 0, 0);
  assert.strictEqual(eng._contribBalance[pid] || 0, 0);

  // 加 50（<100 → §30.8 catchUpMult ×3 仅作用于 lifetime/balance）
  eng._addContribution(pid, 50, 'gift');
  assert.strictEqual(Math.round(eng.contributions[pid]), 50, 'contributions += 50');
  assert.strictEqual(eng._lifetimeContrib[pid], 150, '_lifetimeContrib += 50*3=150 (catchUp)');
  assert.strictEqual(eng._contribBalance[pid], 150, '_contribBalance += 50*3=150 (catchUp)');

  // 关键不变式：_lifetimeContrib 与 _contribBalance 同步增长
  const lifeDelta0 = eng._lifetimeContrib[pid];
  const balDelta0  = eng._contribBalance[pid];
  assert.strictEqual(lifeDelta0, balDelta0, '_lifetimeContrib diff === _contribBalance diff');

  // 再加 200（此时 _lifetimeContrib >= 100，catchUp 失效 ×1）
  eng._addContribution(pid, 200, 'gift');
  assert.strictEqual(Math.round(eng.contributions[pid]), 250, 'contributions += 200');
  assert.strictEqual(eng._lifetimeContrib[pid], 350, '_lifetimeContrib += 200 (no catchUp)');
  assert.strictEqual(eng._contribBalance[pid], 350, '_contribBalance += 200 (no catchUp)');

  // B 类购买：_contribBalance 扣；_lifetimeContrib 不变
  eng._contribBalance[pid] = 500;
  eng._lifetimeContrib[pid] = 500;
  clearBroadcasts(eng);
  eng.handleShopPurchase(pid, 'Frank', 'title_supporter', null);  // 500
  assert.strictEqual(eng._contribBalance[pid], 0, 'B 类购买扣 _contribBalance');
  assert.strictEqual(eng._lifetimeContrib[pid], 500, 'B 类购买不动 _lifetimeContrib');

  // 不变式：_contribBalance >= 0
  assert.ok(eng._contribBalance[pid] >= 0, '_contribBalance 不可为负');

  // 不变式：_contribBalance <= _lifetimeContrib（消费后前者低于后者）
  assert.ok(eng._contribBalance[pid] <= eng._lifetimeContrib[pid], '_contribBalance ≤ _lifetimeContrib');
});

// ============================================================
// T7（补充）：handleShopInventory 返回 contribBalance + lifetimeContrib
// ============================================================
test('T7: handleShopInventory 返回完整快照（owned/equipped/contribBalance/lifetimeContrib）', () => {
  const eng = makeEngine();
  const pid = 'p7_gina';
  eng._playerShopInventory[pid] = ['title_veteran', 'frame_bronze'];
  eng._playerShopEquipped[pid] = { title: 'title_veteran', frame: 'frame_bronze' };
  eng._contribBalance[pid] = 1234;
  eng._lifetimeContrib[pid] = 5678;
  clearBroadcasts(eng);

  eng.handleShopInventory(pid);

  const inv = findBroadcast(eng, 'shop_inventory_data', d => d.playerId === pid);
  assert.ok(inv, '应广播 shop_inventory_data');
  assert.deepStrictEqual(inv.data.owned.sort(), ['frame_bronze', 'title_veteran']);
  assert.strictEqual(inv.data.equipped.title, 'title_veteran');
  assert.strictEqual(inv.data.equipped.frame, 'frame_bronze');
  assert.strictEqual(inv.data.equipped.entrance, '');
  assert.strictEqual(inv.data.equipped.barrage, '');
  assert.strictEqual(inv.data.contribBalance, 1234);
  assert.strictEqual(inv.data.lifetimeContrib, 5678);
});

// ============================================================
// T8（补充）：loadSeason('season_1') 注入赛季限定 SKU 池
// ============================================================
test('T8: loadSeason(season_1) 加载 B9/B10 赛季限定到 _seasonShopPool', () => {
  const eng = makeEngine();
  assert.ok(Array.isArray(eng._seasonShopPool) && eng._seasonShopPool.length === 0, '初始 _seasonShopPool 空');
  const n = eng.loadSeason('season_1');
  assert.ok(n >= 2, `应加载 >= 2 个 SKU，实际 ${n}`);
  assert.strictEqual(eng._seasonShopPool.length, n);
  // 第一个 SKU 字段检查
  const first = eng._seasonShopPool[0];
  assert.ok(first && first.id && first.slot, 'SKU 字段完整');
  assert.ok(first.lifetimeContribMin >= 1000, 'lifetimeContribMin >= 1000（策划案 §39.8 守门）');
});

// ============================================================
// T9（补充）：A4 spotlight per-game 限 1 次
// ============================================================
test('T9: A4 spotlight per-game 限 1 次（激活 spotlight_active / 结束后 limit_exceeded）', (done) => {
  const eng = makeEngine();
  const pid = 'p9_helen';
  eng.contributions[pid] = 2000;

  // 首次购买：成功 + 激活
  clearBroadcasts(eng);
  eng.handleShopPurchase(pid, 'Helen', 'spotlight', null);
  assert.ok(findBroadcast(eng, 'shop_purchase_confirm', d => d.itemId === 'spotlight'), '首次购买成功');
  assert.ok(eng._shopSpotlightActive[pid], 'spotlight 激活');
  assert.strictEqual(eng.contributions[pid], 1750, '扣 250');

  // 激活期间再买：spotlight_active
  clearBroadcasts(eng);
  eng.handleShopPurchase(pid, 'Helen', 'spotlight', null);
  const active = findBroadcast(eng, 'shop_purchase_failed', d => d.reason === 'spotlight_active');
  assert.ok(active, '激活期间应 spotlight_active');
  assert.strictEqual(eng.contributions[pid], 1750, 'spotlight_active 不扣费');

  // 模拟激活结束：手动清 active + 设 usedThisGame
  delete eng._shopSpotlightActive[pid];
  eng._shopSpotlightUsedThisGame[pid] = true;
  clearBroadcasts(eng);
  eng.handleShopPurchase(pid, 'Helen', 'spotlight', null);
  const limit = findBroadcast(eng, 'shop_purchase_failed', d => d.reason === 'limit_exceeded');
  assert.ok(limit, '激活结束后再买应 limit_exceeded');
  assert.strictEqual(eng.contributions[pid], 1750, 'limit_exceeded 不扣费');
});

// ============================================================
// 总结
// ============================================================
console.log(`\n${'='.repeat(60)}`);
console.log(`§39 Shop System MVP Tests: ${passed} PASS / ${failed} FAIL / ${passed + failed} TOTAL`);
console.log('='.repeat(60));
if (failed > 0) {
  console.error(`${failed} test(s) failed — see log above.`);
  process.exit(1);
} else {
  console.log('All tests passed.');
}
