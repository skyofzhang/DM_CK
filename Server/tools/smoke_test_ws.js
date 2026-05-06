// audit-r45 后烟雾测：连接生产 WS → init/join GM 模式 → 收 survival_game_state → 验证 dailyCap 字段 + schemaVersion
// 用法: node Server/tools/smoke_test_ws.js
const WebSocket = require('ws');

const URL = 'ws://101.34.30.65:8081';
const ROOM_ID = 'smoke_test_' + Date.now();
const TIMEOUT_MS = 12000;

const checks = {
  ws_open: false,
  game_state_received: false,
  has_dailyFortressDayGained: false,
  has_dailyCapMax: false,
  has_dailyResetAt: false,
  has_dailyCapBlocked: false,
  has_supporters: false,
  has_supporterCount: false,
  has_skinTier_in_workers: false,
  has_priority_tick: false,
};

const ws = new WebSocket(URL);
const timer = setTimeout(() => {
  console.error('TIMEOUT after', TIMEOUT_MS, 'ms');
  finish();
}, TIMEOUT_MS);

ws.on('open', () => {
  checks.ws_open = true;
  console.log('[smoke] WS open →', URL);
  ws.send(JSON.stringify({
    type: 'join_room',
    roomId: ROOM_ID,
    openId: 'smoke_' + Math.random().toString(36).slice(2, 8),
    isGMMode: true,
  }));
});

ws.on('message', (raw) => {
  let msg;
  try { msg = JSON.parse(raw.toString()); } catch { return; }
  console.log('[smoke] recv type=' + msg.type + (msg.priority !== undefined ? ' prio=' + msg.priority : '') + (msg.tick !== undefined ? ' tick=' + msg.tick : ''));

  if (msg.priority !== undefined && msg.tick !== undefined) checks.has_priority_tick = true;

  if (msg.type === 'survival_game_state' || msg.type === 'getFullState' || msg.data) {
    const state = msg.data || msg;
    checks.game_state_received = true;
    if (typeof state.dailyFortressDayGained !== 'undefined') checks.has_dailyFortressDayGained = true;
    if (typeof state.dailyCapMax !== 'undefined') checks.has_dailyCapMax = true;
    if (typeof state.dailyResetAt !== 'undefined') checks.has_dailyResetAt = true;
    if (typeof state.dailyCapBlocked !== 'undefined') checks.has_dailyCapBlocked = true;
    if (Array.isArray(state.supporters)) checks.has_supporters = true;
    if (typeof state.supporterCount !== 'undefined') checks.has_supporterCount = true;
    if (Array.isArray(state.workers) && state.workers.length > 0) {
      if (typeof state.workers[0].skinTier !== 'undefined') checks.has_skinTier_in_workers = true;
    }
  }

  if (msg.type === 'survival_game_state') {
    setTimeout(finish, 1500);
  }
});

ws.on('error', (e) => {
  console.error('[smoke] WS error:', e.message);
  finish();
});

ws.on('close', () => {
  console.log('[smoke] WS closed');
});

function finish() {
  clearTimeout(timer);
  try { ws.close(); } catch {}
  console.log('\n=== SMOKE TEST RESULT ===');
  let pass = 0, fail = 0;
  for (const [k, v] of Object.entries(checks)) {
    console.log((v ? 'PASS' : 'FAIL') + '  ' + k + ': ' + v);
    if (v) pass++; else fail++;
  }
  console.log('Total: ' + pass + ' / ' + (pass + fail));
  process.exit(fail > 0 ? 1 : 0);
}
