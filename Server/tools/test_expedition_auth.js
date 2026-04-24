const assert = require('assert');
const path = require('path');

const ROOT = path.join(__dirname, '..');
const SurvivalGameEngine = require(path.join(ROOT, 'src', 'SurvivalGameEngine'));
const SurvivalRoom = require(path.join(ROOT, 'src', 'SurvivalRoom'));

const CMD_SEND = '\u63A2';
const CMD_RECALL = '\u53EC\u56DE';

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
    failed++;
    process.exitCode = 1;
  }
}

function makeEngine() {
  const captures = [];
  const eng = new SurvivalGameEngine({}, (msg) => captures.push(msg));
  eng._captured = captures;
  eng.state = 'day';
  eng.room = {
    roomId: 'exp_test_room',
    clients: new Set(),
    roomCreatorOpenId: 'creator',
    creator: { isVeteran: false },
    seasonMgr: { seasonDay: 1 },
  };
  return eng;
}

function findMsg(list, type, filter) {
  return list.find(x => x.type === type && (!filter || filter(x.data || {})));
}

function makeWs(playerId) {
  const out = [];
  return {
    _playerId: playerId,
    openId: playerId,
    readyState: 1,
    _sent: out,
    send(raw) {
      out.push(JSON.parse(raw));
    },
  };
}

function makeRoomWithWss() {
  const room = new SurvivalRoom('exp_room_' + Math.floor(Math.random() * 1e6), {}, null, null);
  room.isGMMode = false;
  room._modeInitialized = true;
  room.roomCreatorOpenId = 'creator';
  room.seasonMgr = { seasonDay: 5 };
  room.survivalEngine.seasonMgr = room.seasonMgr;

  const wsCreator = makeWs('creator');
  const wsViewer = makeWs('viewer');
  room.roomCreatorWs = wsCreator;
  room.clients.add(wsCreator);
  room.clients.add(wsViewer);

  return { room, wsCreator, wsViewer };
}

test('T1: barrage expedition send is feature-gated with unlockDay', () => {
  const eng = makeEngine();
  eng.room.seasonMgr.seasonDay = 1;

  eng.handleComment('u1', 'U1', '', CMD_SEND);

  const fail = findMsg(eng._captured, 'expedition_failed', d => d.playerId === 'u1');
  assert.ok(fail, 'expected expedition_failed');
  assert.strictEqual(fail.data.reason, 'feature_locked');
  assert.strictEqual(fail.data.unlockDay, 5);
});

test('T2: barrage recall is broadcaster-only', () => {
  const eng = makeEngine();
  eng.room.seasonMgr.seasonDay = 6;
  eng._expeditions.set('exp_1', {
    expeditionId: 'exp_1',
    playerId: 'miner_1',
    outboundTimer: null,
    eventTimer: null,
    returnTimer: null,
  });

  eng.handleComment('viewer', 'Viewer', '', CMD_RECALL);

  const fail = findMsg(eng._captured, 'expedition_failed', d => d.playerId === 'viewer');
  assert.ok(fail, 'expected expedition_failed for non-broadcaster recall');
  assert.strictEqual(fail.data.reason, 'supporter_not_allowed');
  assert.strictEqual(eng._expeditions.size, 1, 'expedition should not be recalled by non-broadcaster');
});

test('T3: broadcaster barrage recall recalls first in-flight expedition', () => {
  const eng = makeEngine();
  eng.room.seasonMgr.seasonDay = 6;
  eng._expeditions.set('exp_1', {
    expeditionId: 'exp_1',
    playerId: 'miner_1',
    outboundTimer: null,
    eventTimer: null,
    returnTimer: null,
  });

  eng.handleComment('creator', 'Creator', '', CMD_RECALL);

  assert.strictEqual(eng._expeditions.size, 0, 'expedition should be recalled by broadcaster');
  const returned = findMsg(eng._captured, 'expedition_returned', d => d.playerId === 'miner_1');
  assert.ok(returned, 'expected expedition_returned broadcast');
});

test('T4: ws recall command is broadcaster-only', () => {
  const { room, wsViewer } = makeRoomWithWss();
  room.survivalEngine._expeditions.set('exp_1', {
    expeditionId: 'exp_1',
    playerId: 'miner_1',
    outboundTimer: null,
    eventTimer: null,
    returnTimer: null,
  });

  room.handleClientMessage(wsViewer, 'expedition_command', { action: 'recall', playerId: 'miner_1' });

  assert.strictEqual(room.survivalEngine._expeditions.size, 1, 'viewer must not recall target expedition');
  const fail = wsViewer._sent.find(m => m.type === 'expedition_failed');
  assert.ok(fail, 'expected expedition_failed on unauthorized recall');
  assert.strictEqual(fail.data.reason, 'supporter_not_allowed');
});

test('T5: ws send command ignores spoofed playerId', () => {
  const { room, wsViewer } = makeRoomWithWss();
  let captured = null;
  room.survivalEngine.handleExpeditionCommand = (pid, action) => { captured = { pid, action }; };

  room.handleClientMessage(wsViewer, 'expedition_command', { action: 'send', playerId: 'spoofed' });

  assert.ok(captured, 'expected handleExpeditionCommand call');
  assert.strictEqual(captured.action, 'send');
  assert.strictEqual(captured.pid, 'viewer');
});

test('T6: expedition_event_vote is broadcaster-only', () => {
  const { room, wsCreator, wsViewer } = makeRoomWithWss();
  let voteCalls = 0;
  room.survivalEngine.handleExpeditionEventVote = () => { voteCalls++; };

  room.handleClientMessage(wsViewer, 'expedition_event_vote', { expeditionId: 'exp_1', choice: 'accept' });
  assert.strictEqual(voteCalls, 0, 'viewer should not pass vote');
  const reject = wsViewer._sent.find(m => m.type === 'broadcaster_action_failed');
  assert.ok(reject, 'viewer should receive broadcaster_action_failed');

  room.handleClientMessage(wsCreator, 'expedition_event_vote', { expeditionId: 'exp_1', choice: 'accept' });
  assert.strictEqual(voteCalls, 1, 'creator vote should pass');
});

console.log('\n=== Expedition auth/gating test summary ===');
console.log(`Passed: ${passed}`);
console.log(`Failed: ${failed}`);
console.log(`Total : ${passed + failed}`);
console.log(`Result: ${failed === 0 ? 'PASS' : 'FAIL'}`);
