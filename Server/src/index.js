/**
 * 极地生存法则 - Node.js 服务端
 *
 * 架构: 多房间 + 抖音数据推送
 *
 * 职责:
 * 1. 管理多个游戏房间（每个主播直播间=一个Room）
 * 2. 接收抖音直播弹幕/礼物/点赞事件推送（HTTP POST）
 * 3. WebSocket同步到Unity客户端（按roomId路由）
 *
 * 房间生命周期:
 *   客户端连接(join_room) → 创建/加入房间
 *   客户端断开 → 暂停房间（保留状态）
 *   客户端重连 → 恢复房间
 *   30分钟无连接 → 销毁房间
 *
 * 抖音数据流:
 *   主播直播间 → 抖音服务器 → POST /api/douyin/push → RoomManager路由 → Room处理
 */

const WebSocket = require('ws');
const express = require('express');
require('dotenv').config();

const RoomManager = require('./RoomManager');
const DouyinAPI = require('./DouyinAPI');
const { getAllGifts } = require('./GiftConfig');

const http = require('http');
const config = require('../config/default.json');
const PORT = process.env.PORT || config.server.wsPort; // 统一使用wsPort(8081)

// ==================== Express HTTP ====================
const app = express();

// 抖音推送签名验证需要原始body字符串，所以用verify回调保存
app.use(express.json({
  limit: '1mb',
  verify: (req, res, buf) => {
    // 保存原始body用于签名验证
    req.rawBody = buf.toString('utf8');
  }
}));

const server = http.createServer(app);

// ==================== WebSocket ====================
const wss = new WebSocket.Server({ server });

// ==================== 核心模块 ====================
const roomManager = new RoomManager(config.game);

// §36 全服同步 + 赛季制（MVP）：SeasonManager → GlobalClock → RoomPersistence + §36.12 VeteranTracker
// 四者均为全局单例，注入 RoomManager 后由其构造 SurvivalRoom 时传递
const SeasonManager = require('./SeasonManager');
const GlobalClock = require('./GlobalClock');
const RoomPersistence = require('./RoomPersistence');
const VeteranTracker = require('./VeteranTracker');

const seasonManager   = new SeasonManager();
const globalClock     = new GlobalClock(seasonManager, {
  // 与 easy/normal/hard 的 dayDuration/nightDuration 保持一致（均为 120s）
  dayDurationMs:   (config.game.survivalDayDuration   || 120) * 1000,
  nightDurationMs: (config.game.survivalNightDuration || 120) * 1000,
});
const roomPersistence = new RoomPersistence();
const veteranTracker  = new VeteranTracker();
roomManager.globalClock     = globalClock;
roomManager.seasonMgr       = seasonManager;
roomManager.roomPersistence = roomPersistence;
roomManager.veteranTracker  = veteranTracker;
console.log('[index] §36 GlobalClock / SeasonManager / RoomPersistence / VeteranTracker attached');

// §35 Tribe War:全局 TribeWarManager 单例,反向注入给 RoomManager
// 房间创建时自动 setRoomContext() 到 SurvivalGameEngine
const TribeWarManager = require('./TribeWarManager');
const tribeWarManager = new TribeWarManager(roomManager);
roomManager.tribeWarMgr = tribeWarManager;
console.log('[index] TribeWarManager attached to RoomManager');

// §36 每 30s 自动保存所有房间快照
const _roomPersistenceSaveInterval = setInterval(() => {
  try {
    const n = roomPersistence.saveAll(roomManager.rooms);
    if (n > 0) {
      console.log(`[index] RoomPersistence autosave: ${n} rooms`);
    }
  } catch (e) {
    console.warn(`[index] RoomPersistence autosave error: ${e.message}`);
  }
}, 30 * 1000);

// 抖音API（评论/礼物/点赞回调路由到RoomManager）
const douyinAPI = new DouyinAPI({
  appId: process.env.DOUYIN_APP_ID,
  appSecret: process.env.DOUYIN_APP_SECRET,
  onComment: (roomId, secOpenId, nickname, avatarUrl, content) => {
    roomManager.routeDouyinComment(roomId, secOpenId, nickname, avatarUrl, content);
  },
  onGift: (roomId, secOpenId, nickname, avatarUrl, secGiftId, giftNum, giftValue) => {
    roomManager.routeDouyinGift(roomId, secOpenId, nickname, avatarUrl, secGiftId, giftNum, giftValue);
  },
  onLike: (roomId, secOpenId, nickname, avatarUrl, likeNum) => {
    roomManager.routeDouyinLike(roomId, secOpenId, nickname, avatarUrl, likeNum);
  }
});

// ==================== HTTP 路由 ====================

// 健康检查
app.get('/health', (req, res) => {
  res.json({
    status: 'ok',
    timestamp: Date.now(),
    rooms: roomManager.getStats()
  });
});

// 全局统计
app.get('/stats', (req, res) => {
  res.json({
    rooms: roomManager.getStats(),
    douyin: douyinAPI.getStats()
  });
});

// 礼物配置
app.get('/gifts', (req, res) => {
  res.json(getAllGifts());
});

// 指定房间状态
app.get('/room/:roomId', (req, res) => {
  const room = roomManager.getRoom(req.params.roomId);
  if (!room) {
    return res.status(404).json({ error: 'Room not found' });
  }
  res.json(room.getInfo());
});

// 指定房间贡献榜（生存游戏）
app.get('/room/:roomId/rankings', (req, res) => {
  const room = roomManager.getRoom(req.params.roomId);
  if (!room) {
    return res.status(404).json({ error: 'Room not found' });
  }
  res.json(room.survivalEngine._buildRankings());
});

// 指定房间本周贡献榜（生存游戏）
app.get('/room/:roomId/rankings/weekly', (req, res) => {
  const room = roomManager.getRoom(req.params.roomId);
  if (!room) return res.status(404).json({ error: 'Room not found' });
  res.json(room.weeklyRanking.getPayload(10));
});

// 指定房间历史排行（生存游戏暂无此功能）
app.get('/room/:roomId/rankings/history', (req, res) => {
  res.json({ message: 'History rankings not available in survival mode', rankings: [] });
});

// ==================== 抖音数据推送回调 ====================

/**
 * 抖音推送数据接收端点
 * POST /api/douyin/push
 *
 * 抖音服务器推送格式:
 * {
 *   roomid: "直播间ID",
 *   msg_type: "live_comment" | "live_gift" | "live_like" | "live_fansclub",
 *   payload: "[{...}, {...}]"   // JSON字符串，需要JSON.parse反序列化
 * }
 *
 * Headers (用于签名验证):
 *   x-nonce-str: 随机字符串
 *   x-timestamp: 时间戳
 *   x-roomid: 直播间ID
 *   x-msg-type: 消息类型
 *   x-signature: 签名值
 */
app.post('/api/douyin/push', (req, res) => {
  /**
   * 履约ACK策略: 先返回HTTP 200，再异步处理数据
   * - 抖音要求2秒内返回2XX（礼物3秒），否则计为推送失败
   * - 连续10次失败触发熔断，平台直接丢弃数据
   * - 因此签名验证失败也返回200（只记日志），避免误触熔断
   */
  const msgType = req.headers['x-msg-type'] || req.body?.msg_type || 'unknown';
  const roomId = req.headers['x-roomid'] || req.body?.roomid || 'unknown';

  // 签名验证 — 失败时仍返回200避免熔断，只记警告日志
  const signature = req.headers['x-signature'];
  if (signature && !douyinAPI.verifySignature(req.headers, req.rawBody || '')) {
    console.warn(`[DRSCFZ] ⚠️ Push signature FAILED: msg_type=${msgType}, roomid=${roomId}`);
    res.json({ err_no: 0, err_msg: 'ok' });
    return;
  }

  // 立即返回200（ACK确认），后续异步处理数据
  res.json({ err_no: 0, err_msg: 'ok' });

  // 异步处理推送数据（不阻塞HTTP响应）
  try {
    console.log(`[DRSCFZ] 📨 Push: msg_type=${msgType}, roomid=${roomId}`);

    let pushBody = req.body;
    if (!pushBody.msg_type && req.headers['x-msg-type']) {
      pushBody = {
        msg_type: req.headers['x-msg-type'],
        roomid: req.headers['x-roomid'],
        payload: req.rawBody || JSON.stringify(req.body)
      };
    }

    douyinAPI.handlePushData(pushBody);
  } catch (e) {
    console.error(`[DRSCFZ] Push processing error: ${e.message}`);
  }
});

/**
 * 抖音初始化端点（Unity客户端启动时调用）
 * POST /api/douyin/init  { token: "直播伴侣传入的token" }
 *
 * 流程:
 * 1. 用token调用 GetRoomInfo 获取直播间ID
 * 2. 自动启动推送任务（评论+礼物+点赞）
 * 3. 返回roomId给客户端，客户端用此roomId加入WebSocket房间
 *
 * 响应: { success, roomId, anchorName, startedTypes }
 */
app.post('/api/douyin/init', async (req, res) => {
  try {
    const { token } = req.body;
    if (!token) return res.status(400).json({ error: 'token required' });

    console.log(`[DRSCFZ] Douyin init request: token=${token.substring(0, 20)}... (len=${token.length})`);

    const result = await douyinAPI.initFromToken(token);

    // 预绑定主播 open_id 到房间：此时 WS 尚未连接，预创建 room 并写入 roomCreatorOpenId
    // 这样真实抖音模式下 join_room 时 _isRoomCreator 可据 openId 匹配识别创建者
    if (result.roomId && result.anchorOpenId) {
      try {
        const room = roomManager.getOrCreateRoom(result.roomId);
        if (room && typeof room.bindDouyinAnchor === 'function') {
          room.bindDouyinAnchor(result.anchorOpenId);
          console.log(`[DrscfZ-Net] /api/douyin/init bound anchor openId to room: roomId=${result.roomId}, anchorOpenId=${result.anchorOpenId}, anchorName=${result.nickName}`);
        }
      } catch (e) {
        console.warn(`[DRSCFZ] Douyin init: bindDouyinAnchor failed: ${e.message}`);
      }
    } else if (result.roomId && !result.anchorOpenId) {
      console.warn(`[DRSCFZ] Douyin init: anchor_open_id missing from GetRoomInfo for roomId=${result.roomId} — douyin mode may fallback to first-client binding`);
    }

    res.json({
      success: true,
      roomId: result.roomId,
      anchorName: result.nickName,
      anchorOpenId: result.anchorOpenId || '',
      startedTypes: result.startedTypes,
      retrying: result.retrying || false
    });
  } catch (e) {
    console.error(`[DRSCFZ] Douyin init failed: ${e.message}`);
    res.status(500).json({ error: e.message });
  }
});

/**
 * 抖音任务管理端点（手动启动/停止数据推送任务）
 * POST /api/douyin/task/start  { roomId, msgTypes?: [...] }
 * POST /api/douyin/task/stop   { roomId }
 */
app.post('/api/douyin/task/start', async (req, res) => {
  try {
    const { roomId, msgTypes, maxRetries, retryDelay } = req.body;
    if (!roomId) return res.status(400).json({ error: 'roomId required' });
    const result = await douyinAPI.startTasks(roomId, msgTypes, { maxRetries, retryDelay });
    const taskInfo = douyinAPI.activeTasks.get(roomId);
    res.json({
      success: true,
      startedTypes: result,
      retrying: taskInfo?.retrying || false
    });
  } catch (e) {
    res.status(500).json({ error: e.message });
  }
});

// 查看活跃推送任务状态
app.get('/api/douyin/tasks', (req, res) => {
  const tasks = {};
  for (const [roomId, info] of douyinAPI.activeTasks.entries()) {
    tasks[roomId] = {
      msgTypes: info.msgTypes,
      startedAt: new Date(info.startedAt).toISOString(),
      retrying: info.retrying || false,
      durationSec: Math.round((Date.now() - info.startedAt) / 1000)
    };
  }
  res.json({ activeTasks: tasks, douyinStats: douyinAPI.getStats() });
});

app.post('/api/douyin/task/stop', async (req, res) => {
  try {
    const { roomId } = req.body;
    if (!roomId) return res.status(400).json({ error: 'roomId required' });
    await douyinAPI.stopTasks(roomId);
    res.json({ success: true });
  } catch (e) {
    res.status(500).json({ error: e.message });
  }
});

// ==================== 向后兼容: 旧版HTTP路由 ====================
// 这些路由操作 "default" 房间，确保旧客户端不报错

app.get('/state', (req, res) => {
  const room = roomManager.getOrCreateRoom('default');
  res.json({
    ...room.survivalEngine.getFullState(),
    totalPlayers: room.survivalEngine.totalPlayers,
  });
});

app.get('/rankings', (req, res) => {
  const room = roomManager.getOrCreateRoom('default');
  res.json(room.survivalEngine._buildRankings());
});

app.get('/rankings/weekly', (req, res) => {
  const room = roomManager.getOrCreateRoom('default');
  res.json(room.weeklyRanking.getPayload(10));
});

app.get('/rankings/history', (req, res) => {
  // 生存游戏暂无历史排行功能
  res.json({ message: 'History rankings not available in survival mode', rankings: [] });
});

// ==================== WebSocket 连接处理 ====================

wss.on('connection', (ws, req) => {
  // 从URL参数或首条消息获取roomId
  // 支持: ws://host:port?roomId=xxx 或连接后发送 join_room 消息
  const urlParams = new URL(req.url, `http://${req.headers.host}`).searchParams;
  const roomIdFromUrl = urlParams.get('roomId');

  if (roomIdFromUrl) {
    // URL中指定了roomId，立即加入房间
    const room = roomManager.handleClientConnect(ws, roomIdFromUrl);
    console.log(`[DRSCFZ] Client connected to room: ${roomIdFromUrl}`);
  } else {
    // 等待 join_room 消息
    ws._pendingJoin = true;
    console.log('[DRSCFZ] Client connected (waiting for join_room)');
  }

  ws.on('message', (message) => {
    try {
      const msg = JSON.parse(message.toString());

      // 处理加入房间（首次连接时 / URL 直连后补发 join_room 都经由此路径）
      if (msg.type === 'join_room') {
        const joinData = msg.data || {};
        const roomId   = joinData.roomId || 'default';
        if (ws._pendingJoin || !ws._roomId) {
          ws._pendingJoin = false;
          // 携带 joinData（含 isGMMode / playerId / playerName / explicitGMMode）
          // → RoomManager.handleClientConnect 会据此走 SurvivalRoom.handleJoinRoom 分流
          roomManager.handleClientConnect(ws, roomId, joinData);
          console.log(`[DRSCFZ] Client joined room: ${roomId}, isGMMode=${!!joinData.isGMMode}, playerId=${joinData.playerId || 'none'}`);
        } else if (ws._roomId === roomId) {
          // 已在房间内的 join_room（URL 直连后补发 / 重发场景）：让房间重新走 handleJoinRoom 识别模式 + creator
          const room = roomManager.getRoom(roomId);
          if (room && typeof room.handleJoinRoom === 'function') {
            room.handleJoinRoom(ws, joinData);
          }
        }
        return;
      }

      // 向后兼容: 如果客户端没有发join_room，自动加入default房间
      if (ws._pendingJoin || !ws._roomId) {
        ws._pendingJoin = false;
        roomManager.handleClientConnect(ws, 'default');
        console.log('[DRSCFZ] Client auto-joined default room');
        // SurvivalRoom.addClient() 已内置处理 paused→active 恢复
      }

      // 路由消息到对应房间
      roomManager.routeMessage(ws, msg);

    } catch (e) {
      console.error('[DRSCFZ] Message parse error:', e.message);
    }
  });

  ws.on('close', () => {
    const roomId = ws._roomId || 'unknown';
    roomManager.handleClientDisconnect(ws);
    console.log(`[DRSCFZ] Client disconnected from room: ${roomId}`);
  });

  ws.on('error', (err) => {
    console.error('[DRSCFZ] WebSocket error:', err.message);
  });
});

// ==================== 优雅关闭 ====================

function gracefulShutdown() {
  console.log('[DRSCFZ] Shutting down...');
  // §36 停 30s 自动保存，并做一次最终保存
  try {
    clearInterval(_roomPersistenceSaveInterval);
    roomPersistence.saveAll(roomManager.rooms);
  } catch (e) { /* ignore */ }
  douyinAPI.shutdown();
  roomManager.shutdown();
  wss.close();
  server.close(() => {
    console.log('[DRSCFZ] Server closed');
    process.exit(0);
  });
  // 强制退出保底（5秒）
  setTimeout(() => process.exit(0), 5000);
}

process.on('SIGTERM', gracefulShutdown);
process.on('SIGINT', gracefulShutdown);

// ==================== 启动服务 ====================
server.listen(PORT, () => {
  console.log(`[DRSCFZ] HTTP + WebSocket server on port ${PORT}`);
  console.log('[DRSCFZ] Multi-room architecture enabled');
  console.log(`[DRSCFZ] Douyin integration: ${douyinAPI.appId ? 'ENABLED' : 'DISABLED (no credentials)'}`);
  console.log('[DRSCFZ] Waiting for clients...');
});
