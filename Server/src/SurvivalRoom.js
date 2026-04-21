/**
 * SurvivalRoom - 极地生存法则 游戏房间
 * 替代旧的 Room.js（角力玩法）
 *
 * 生命周期: active → paused(断线) → destroyed(超时)
 * 每个主播直播间对应一个独立的 SurvivalRoom + SurvivalGameEngine
 */

const SurvivalGameEngine    = require('./SurvivalGameEngine');
const WeeklyRankingStore    = require('./WeeklyRankingStore');
const StreamerRankingStore  = require('./StreamerRankingStore');

// 全局单例：主播排行榜（所有房间共享一个存储）
let _streamerRankingInstance = null;
function getStreamerRanking() {
  if (!_streamerRankingInstance) _streamerRankingInstance = new StreamerRankingStore();
  return _streamerRankingInstance;
}

class SurvivalRoom {
  /**
   * @param {string} roomId    - 房间ID（通常=抖音直播间ID）
   * @param {object} gameConfig - 来自 config/default.json
   */
  constructor(roomId, gameConfig) {
    this.roomId       = roomId;
    this.gameConfig   = gameConfig;
    this.createdAt    = Date.now();
    this.lastActiveAt = Date.now();
    this.status       = 'active'; // active | paused | destroyed

    // WebSocket 客户端集合
    this.clients = new Set();

    // 房间创建者：第一个连接的WebSocket客户端即为主播（房间创建者）
    // 用于 broadcaster_action 权限验证
    this.roomCreatorWs = null;

    // 主播专用：效率加速乘数（broadcaster_action触发）
    this.broadcasterEfficiencyMultiplier = 1.0;

    // 生存游戏引擎（替代旧 GameEngine + PlayerManager）
    this.survivalEngine = new SurvivalGameEngine(
      gameConfig,
      (msg) => this.broadcast(msg)
    );

    // 本周贡献榜持久化存储（每周日23:59→周一00:00 UTC+8 自动重置）
    this.weeklyRanking = new WeeklyRankingStore(roomId);

    // 主播排行榜（全局共享单例）
    this.streamerRanking = getStreamerRanking();

    // 暂停超时（30分钟后销毁）
    this._pauseTimer = null;
    this.pauseTimeout = gameConfig.pauseTimeout || 30 * 60 * 1000;

    console.log(`[SurvivalRoom:${roomId}] Created`);
  }

  // ==================== 客户端管理 ====================

  addClient(ws) {
    // 第一个连接的客户端即为房间创建者（主播）
    const isFirstClient = this.clients.size === 0 && this.roomCreatorWs === null;
    if (isFirstClient) {
      this.roomCreatorWs = ws;
      console.log(`[SurvivalRoom:${this.roomId}] Room creator assigned (first client)`);
    }

    this.clients.add(ws);
    this.lastActiveAt = Date.now();

    if (this.status === 'paused') {
      this._cancelPauseTimer();
      this.status = 'active';
      this.survivalEngine.resume();
      console.log(`[SurvivalRoom:${this.roomId}] Resumed from pause`);
    }

    // 告知客户端是否为房间创建者（主播端凭此决定是否显示BroadcasterPanel）
    // has_active_session=true 表示服务器有进行中的游戏，客户端弹出断线重连对话框
    const isCreator = ws === this.roomCreatorWs;
    const hasActiveSession = this.survivalEngine &&
      this.survivalEngine.state !== 'idle' &&
      this.survivalEngine.state !== undefined;
    try {
      ws.send(JSON.stringify({
        type: 'join_room_confirm',
        roomId: this.roomId,
        timestamp: Date.now(),
        data: {
          isRoomCreator: isCreator,
          has_active_session: hasActiveSession
        }
      }));
    } catch (e) {
      console.warn(`[SurvivalRoom:${this.roomId}] join_room_confirm send error: ${e.message}`);
    }

    // 新客户端连接：发送当前完整状态
    this._sendStateToClient(ws);
    console.log(`[SurvivalRoom:${this.roomId}] Client added (isCreator=${isCreator}, total: ${this.clients.size})`);
  }

  removeClient(ws) {
    this.clients.delete(ws);

    // 如果房间创建者断线，将创建者身份转交给当前第一个在线客户端（若有）
    if (ws === this.roomCreatorWs) {
      if (this.clients.size > 0) {
        this.roomCreatorWs = this.clients.values().next().value;
        console.log(`[SurvivalRoom:${this.roomId}] Creator disconnected, transferring creator to next client`);
        // 通知新创建者
        try {
          this.roomCreatorWs.send(JSON.stringify({
            type: 'join_room_confirm',
            roomId: this.roomId,
            timestamp: Date.now(),
            data: { isRoomCreator: true }
          }));
        } catch (e) { }
      } else {
        this.roomCreatorWs = null;
      }
    }

    console.log(`[SurvivalRoom:${this.roomId}] Client removed (remaining: ${this.clients.size})`);

    if (this.clients.size === 0) {
      this._enterPaused();
    }
  }

  _enterPaused() {
    this.status = 'paused';
    this.survivalEngine.pause();
    console.log(`[SurvivalRoom:${this.roomId}] Paused - will destroy in ${this.pauseTimeout / 60000}min`);
    this._startPauseTimer();
  }

  _startPauseTimer() {
    this._cancelPauseTimer();
    this._pauseTimer = setTimeout(() => this.destroy(), this.pauseTimeout);
  }

  _cancelPauseTimer() {
    if (this._pauseTimer) { clearTimeout(this._pauseTimer); this._pauseTimer = null; }
  }

  refreshPauseTimer() {
    this.lastActiveAt = Date.now();
    if (this.status === 'paused' && this._pauseTimer) {
      this._startPauseTimer();
    }
  }

  /** 停止模拟器并清理所有定时器 */
  _stopSimulation() {
    this._simRunning = false;

    if (this._simPlayerTimers) {
      this._simPlayerTimers.forEach(t => clearTimeout(t));
      this._simPlayerTimers.clear();
      this._simPlayerTimers = null;
    }

    if (this._simNightTimer) {
      clearTimeout(this._simNightTimer);
      this._simNightTimer = null;
    }

    if (this._simWorkInterval)      clearInterval(this._simWorkInterval);
    if (this._simNightInterval)     clearInterval(this._simNightInterval);
    if (this._simDayGiftInterval)   clearInterval(this._simDayGiftInterval);
    if (this._simNightGiftInterval) clearInterval(this._simNightGiftInterval);
  }

  destroy() {
    this._cancelPauseTimer();
    this._stopSimulation();
    this.survivalEngine.pause();
    this.status = 'destroyed';

    for (const ws of this.clients) {
      try {
        ws.send(JSON.stringify({
          type: 'room_destroyed',
          timestamp: Date.now(),
          data: { roomId: this.roomId, reason: 'timeout' }
        }));
        ws.close(1000, 'room_destroyed');
      } catch (e) { }
    }
    this.clients.clear();
    console.log(`[SurvivalRoom:${this.roomId}] Destroyed`);
  }

  // ==================== 广播 ====================

  broadcast(message) {
    // ── 局结束时更新本周贡献榜，并广播最新周榜数据 ──────────────────
    if (message && message.type === 'survival_game_ended' && message.data) {
      const d = message.data;
      // 更新本周贡献榜
      if (d.rankings) {
        try {
          this.weeklyRanking.addGameResult(d.rankings);
          setTimeout(() => this._broadcastWeeklyRanking(), 500);
        } catch (e) {
          console.error(`[SurvivalRoom:${this.roomId}] WeeklyRanking update error: ${e.message}`);
        }
      }
      // 更新主播排行榜
      try {
        const difficulty = this.survivalEngine._difficulty || 'normal';
        const streamerName = this.roomId; // 房间ID即主播标识，后续可改为真实昵称
        this.streamerRanking.addGameResult(this.roomId, streamerName, difficulty, d.dayssurvived || 0, d.result || 'lose');
        setTimeout(() => this._broadcastStreamerRanking(), 600);
      } catch (e) {
        console.error(`[SurvivalRoom:${this.roomId}] StreamerRanking update error: ${e.message}`);
      }
    }

    if (this.clients.size === 0) return;
    message.roomId = this.roomId;
    const data = JSON.stringify(message);

    for (const client of this.clients) {
      try {
        if (client.readyState === 1) client.send(data);
      } catch (e) {
        console.warn(`[SurvivalRoom:${this.roomId}] Broadcast error: ${e.message}`);
      }
    }
  }

  /**
   * 向所有客户端广播最新本周贡献榜
   */
  _broadcastWeeklyRanking() {
    const payload = this.weeklyRanking.getPayload(10);
    const msg = JSON.stringify({
      type:      'weekly_ranking',
      roomId:    this.roomId,
      timestamp: Date.now(),
      data:      payload,
    });
    for (const client of this.clients) {
      try {
        if (client.readyState === 1) client.send(msg);
      } catch (e) { }
    }
    console.log(`[SurvivalRoom:${this.roomId}] 本周榜已广播，week=${payload.week}，条数=${payload.rankings.length}`);
  }

  /**
   * 向所有客户端广播最新主播排行榜
   */
  _broadcastStreamerRanking() {
    const payload = this.streamerRanking.getPayload(10);
    const msg = JSON.stringify({
      type:      'streamer_ranking',
      roomId:    this.roomId,
      timestamp: Date.now(),
      data:      payload,
    });
    for (const client of this.clients) {
      try {
        if (client.readyState === 1) client.send(msg);
      } catch (e) { }
    }
    console.log(`[SurvivalRoom:${this.roomId}] 主播榜已广播，条数=${payload.rankings.length}`);
  }

  _sendStateToClient(ws) {
    try {
      ws.send(JSON.stringify({
        type: 'survival_game_state',
        roomId: this.roomId,
        timestamp: Date.now(),
        data: this.survivalEngine.getFullState()
      }));
    } catch (e) {
      console.warn(`[SurvivalRoom:${this.roomId}] Send state error: ${e.message}`);
    }
  }

  // ==================== WebSocket 指令处理 ====================

  /**
   * 处理来自Unity客户端的WS消息
   */
  handleClientMessage(ws, msgType, data) {
    switch (msgType) {
      case 'start_game':
        // 新一局开始时，停止上一局残留的模拟器
        this._stopSimulation();
        // 支持难度参数：data.difficulty = 'easy' | 'normal' | 'hard'
        this.survivalEngine.startGame(data && data.difficulty ? data.difficulty : 'normal');
        break;
      case 'reset_game':
        this._stopSimulation();
        this.survivalEngine.reset();
        break;
      case 'sync_state':
        // 客户端请求重新同步当前游戏状态（用于"继续上一局"场景）
        this._sendStateToClient(ws);
        break;
      case 'heartbeat':
        try {
          ws.send(JSON.stringify({ type: 'heartbeat_ack', timestamp: Date.now(), roomId: this.roomId }));
        } catch (e) { }
        break;
      case 'upgrade_gate':
        // 城门升级（需要矿石资源）
        // data.secOpenId 可选，用于记录操作者；也接受连接时注册的操作者 ID
        this.survivalEngine._upgradeGate(data && data.secOpenId || '');
        break;
      case 'toggle_sim':
        // 生存游戏模拟器（直接触发测试事件）
        if (data && data.enabled) {
          this._runSimulation();
        } else {
          this._stopSimulation();
        }
        break;
      case 'get_weekly_ranking':
        // 客户端请求本周贡献榜（面板打开时触发）
        try {
          const n = (data && data.top && data.top <= 50) ? data.top : 50;
          const payload = this.weeklyRanking.getPayload(n);
          ws.send(JSON.stringify({
            type:      'weekly_ranking',
            roomId:    this.roomId,
            timestamp: Date.now(),
            data:      payload,
          }));
        } catch (e) {
          console.error(`[SurvivalRoom:${this.roomId}] get_weekly_ranking error: ${e.message}`);
        }
        break;

      case 'get_streamer_ranking':
        // 客户端请求主播排行榜
        try {
          const sN = (data && data.top && data.top <= 50) ? data.top : 50;
          const sPayload = this.streamerRanking.getPayload(sN);
          ws.send(JSON.stringify({
            type:      'streamer_ranking',
            roomId:    this.roomId,
            timestamp: Date.now(),
            data:      sPayload,
          }));
        } catch (e) {
          console.error(`[SurvivalRoom:${this.roomId}] get_streamer_ranking error: ${e.message}`);
        }
        break;

      case 'end_game':
        // GM 手动结束游戏 → 强制触发失败结算
        this._stopSimulation();
        if (this.survivalEngine.state === 'day' || this.survivalEngine.state === 'night') {
          this.survivalEngine._enterSettlement('lose', 'manual');
          console.log(`[SurvivalRoom:${this.roomId}] GM 手动结束游戏`);
        }
        break;
      case 'leave_room':
        // 客户端主动离开，忽略（连接断开由 WebSocket close 事件处理）
        break;
      case 'broadcaster_action':
        this._handleBroadcasterAction(ws, data);
        break;

      // ==================== §24.4 主播事件轮盘 ====================
      // PM 决策（MVP）：_roomCreatorId 未注入 → 放开给任何玩家；
      //   TODO: 等 _roomCreatorId 实现后，这里应先校验 `_isRoomCreator(ws)` 再路由
      case 'broadcaster_roulette_spin': {
        const pid = ws._playerId || '';
        this.survivalEngine.handleBroadcasterRouletteSpin(pid);
        break;
      }
      case 'broadcaster_roulette_apply': {
        const pid = ws._playerId || '';
        this.survivalEngine.handleBroadcasterRouletteApply(pid);
        break;
      }
      case 'broadcaster_trader_accept': {
        const pid = ws._playerId || '';
        const choice = (data && data.choice) || '';
        this.survivalEngine.handleBroadcasterTraderAccept(pid, choice);
        break;
      }

      // ==================== §38 探险系统 ====================
      case 'expedition_command': {
        // { playerId, action: 'send' | 'recall' }
        const pid    = (data && data.playerId) || ws._playerId || '';
        const action = (data && data.action)   || '';
        this.survivalEngine.handleExpeditionCommand(pid, action);
        break;
      }
      case 'expedition_event_vote': {
        // { expeditionId, choice: 'accept' | 'cancel' }
        const pid   = (data && data.playerId)     || ws._playerId || '';
        const expId = (data && data.expeditionId) || '';
        const choice= (data && data.choice)       || '';
        this.survivalEngine.handleExpeditionEventVote(pid, expId, choice);
        break;
      }

      // ==================== §37 建造系统 ====================
      case 'build_propose': {
        // { buildId, playerName? }
        const pid   = ws._playerId || (data && data.playerId) || '';
        // playerName 从 data.playerName 读；fallback 到引擎内 playerNames[pid] 或 pid
        const pname = (data && data.playerName)
                      || (this.survivalEngine.playerNames && this.survivalEngine.playerNames[pid])
                      || pid;
        const buildId = (data && data.buildId) || '';
        this.survivalEngine.handleBuildPropose(pid, pname, buildId);
        break;
      }
      case 'build_vote': {
        // { proposalId, buildId }
        const pid        = ws._playerId || (data && data.playerId) || '';
        const proposalId = (data && data.proposalId) || '';
        const buildId    = (data && data.buildId)    || '';
        this.survivalEngine.handleBuildVote(pid, proposalId, buildId);
        break;
      }

      // ==================== GM 测试指令 ====================
      case 'pause_game':
        // 暂停/恢复游戏（仅限调试）
        if (this.survivalEngine.state === 'day' || this.survivalEngine.state === 'night') {
          this.survivalEngine._clearAllTimers();
          this.broadcast({ type: 'game_paused', data: { paused: true } });
          console.log(`[SurvivalRoom:${this.roomId}] GM: game paused`);
        }
        break;

      case 'simulate_gift': {
        // 模拟礼物（tier 1-6，默认 tier=2）
        const tierMap = { 1: 'fairy_wand', 2: 'ability_pill', 3: 'donut', 4: 'energy_battery', 5: 'love_explosion', 6: 'mystery_airdrop' };
        const tier    = (data && data.tier && tierMap[data.tier]) ? data.tier : 2;
        const giftId  = tierMap[tier];
        this.survivalEngine.handleGift('gm_test', 'GM测试', '', giftId, 0, `GM礼物T${tier}`);
        console.log(`[SurvivalRoom:${this.roomId}] GM: simulate_gift tier=${tier} (${giftId})`);
        break;
      }

      case 'simulate_freeze':
        // 模拟冻结特效（广播 special_effect freeze_all，不触发游戏结束）
        this.broadcast({ type: 'special_effect', timestamp: Date.now(), data: { effect: 'frozen_all', duration: 5 } });
        console.log(`[SurvivalRoom:${this.roomId}] GM: simulate_freeze`);
        break;

      case 'simulate_monster':
        // 模拟刷怪（立即追加1只普通怪物）
        if (this.survivalEngine.state === 'night') {
          this.survivalEngine._spawnWave({ type: 'normal', count: 1 }, this.survivalEngine.day, 99);
          console.log(`[SurvivalRoom:${this.roomId}] GM: simulate_monster`);
        }
        break;

      default:
        console.log(`[SurvivalRoom:${this.roomId}] Unknown client message: ${msgType}`);
    }
    this.lastActiveAt = Date.now();
  }

  // ==================== 抖音事件入口 ====================

  /**
   * 处理抖音评论
   * 数字 1-5 → 工作指令
   * 其他 → 视为新玩家加入（首次出现）
   */
  handleDouyinComment(secOpenId, nickname, avatarUrl, content) {
    this.lastActiveAt = Date.now();

    const trimmed = (content || '').trim();
    const cmd = parseInt(trimmed);

    // §30.7 换肤 / §38 探险 弹幕专用前缀：均需经 handleComment 路由
    const isSkinCmd      = /^换肤(\d{1,2})?$/.test(trimmed);
    const isExpeditionCmd = (trimmed === '探' || trimmed === '召回');

    if ((cmd >= 1 && cmd <= 6) || trimmed === '666' || isSkinCmd || isExpeditionCmd) {
      // 工作/攻击/666/换肤/探险指令 → 引擎解析
      this.survivalEngine.handleComment(secOpenId, nickname, avatarUrl, trimmed);
    } else {
      // 任意评论 = 玩家首次加入（生存游戏不需要阵营选择）
      this.survivalEngine.handlePlayerJoined(secOpenId, nickname, avatarUrl);
    }
  }

  /**
   * 处理抖音礼物
   */
  handleDouyinGift(secOpenId, nickname, avatarUrl, secGiftId, giftNum, giftValue) {
    this.lastActiveAt = Date.now();

    // 礼物总价值 = 单价 × 数量
    const totalValue = (giftValue || 1) * (giftNum || 1);

    // 礼物ID映射（可按需扩展，目前直接传 secGiftId）
    this.survivalEngine.handleGift(
      secOpenId,
      nickname,
      avatarUrl || '',
      secGiftId,
      totalValue,
      '' // giftName 由引擎内档位配置决定
    );

    // 送礼 = 玩家加入（如果还没加入）
    this.survivalEngine.handlePlayerJoined(secOpenId, nickname, avatarUrl);
  }

  /**
   * 处理抖音点赞（策划案 §9：每次点赞+2积分；每50次点赞食物+10）
   */
  handleDouyinLike(secOpenId, nickname, avatarUrl, likeNum) {
    this.lastActiveAt = Date.now();

    const count = likeNum || 1;

    // 点赞贡献值（每次点赞+2积分）
    if (secOpenId) {
      this.survivalEngine.addContribution(secOpenId, 2 * count);
    }

    // 积分池：点赞每次 +0.5 分（每2个点赞贡献1分；量大则效果可观）
    this.survivalEngine.scorePool = (this.survivalEngine.scorePool || 0) + Math.floor(count * 0.5);

    // 每50次点赞触发食物小加成
    this.survivalEngine.totalLikes = (this.survivalEngine.totalLikes || 0) + count;
    if (Math.floor(this.survivalEngine.totalLikes / 50) > Math.floor((this.survivalEngine.totalLikes - count) / 50)) {
      const bonusCount = Math.floor(this.survivalEngine.totalLikes / 50) - Math.floor((this.survivalEngine.totalLikes - count) / 50);
      this.survivalEngine.food = Math.min(2000, this.survivalEngine.food + 10 * bonusCount);
      this.broadcast({ type: 'bobao', data: { message: `点赞破${Math.floor(this.survivalEngine.totalLikes / 50) * 50}！食物+${10 * bonusCount}！` } });
      console.log(`[SurvivalRoom:${this.roomId}] Like milestone: total=${this.survivalEngine.totalLikes}, food+${10 * bonusCount}`);
    }
  }

  // ==================== 主播专用操作 ====================

  /**
   * 判断当前客户端是否为房间创建者（主播）
   * @param {WebSocket} ws
   * @returns {boolean}
   */
  _isRoomCreator(ws) {
    return ws === this.roomCreatorWs;
  }

  /**
   * 处理 broadcaster_action 消息
   * 仅允许房间创建者（主播）触发
   *
   * @param {WebSocket} ws   - 发送消息的WebSocket连接
   * @param {object} data    - 消息体 { action, duration?, cooldown? }
   */
  _handleBroadcasterAction(ws, data) {
    if (!this._isRoomCreator(ws)) {
      console.log(`[SurvivalRoom:${this.roomId}] broadcaster_action ignored: not room creator`);
      return;
    }

    const action = data && data.action;

    if (action === 'efficiency_boost') {
      const duration = (data && data.duration) || 30000; // 默认30秒
      this._applyEfficiencyBoost(2.0, duration);

      // 广播效果给所有客户端（含主播自身，让客户端显示全屏公告）
      // 注意：Unity NetworkManager 只解析 data 子对象，所有字段必须放在 data 里
      this.broadcast({
        type: 'broadcaster_effect',
        timestamp: Date.now(),
        data: {
          action: 'efficiency_boost',
          multiplier: 2.0,
          duration: duration,
          triggeredBy: '主播'
        }
      });

      // 弹幕播报
      this.broadcast({
        type: 'bobao',
        timestamp: Date.now(),
        data: { message: '⚡ 主播激活紧急加速！全体效率翻倍30秒！' }
      });

      console.log(`[SurvivalRoom:${this.roomId}] broadcaster efficiency_boost activated (${duration}ms)`);

    } else if (action === 'trigger_event') {
      const eventTypes = ['snowstorm', 'harvest', 'monster_wave'];
      const randomEvent = eventTypes[Math.floor(Math.random() * eventTypes.length)];
      const eventNames  = { snowstorm: '暴风雪', harvest: '丰收', monster_wave: '怪物潮' };

      // 广播效果给所有客户端
      this.broadcast({
        type: 'broadcaster_effect',
        timestamp: Date.now(),
        data: {
          action: 'trigger_event',
          eventId: randomEvent,
          eventName: eventNames[randomEvent] || '随机事件',
          triggeredBy: '主播'
        }
      });

      // 弹幕播报
      this.broadcast({
        type: 'bobao',
        timestamp: Date.now(),
        data: { message: `🌊 主播触发了${eventNames[randomEvent] || '随机事件'}！` }
      });

      // 通知游戏引擎触发事件（若引擎支持此接口）
      if (this.survivalEngine && typeof this.survivalEngine.triggerEvent === 'function') {
        this.survivalEngine.triggerEvent(randomEvent);
      }

      console.log(`[SurvivalRoom:${this.roomId}] broadcaster trigger_event: ${randomEvent}`);

    } else {
      console.log(`[SurvivalRoom:${this.roomId}] broadcaster_action unknown action: ${action}`);
    }
  }

  /**
   * 应用效率加速乘数，duration后自动恢复
   * @param {number} multiplier - 乘数（如2.0代表翻倍）
   * @param {number} durationMs - 持续时间（毫秒）
   */
  _applyEfficiencyBoost(multiplier, durationMs) {
    this.broadcasterEfficiencyMultiplier = multiplier;

    // 同步给引擎（若引擎支持此属性）
    if (this.survivalEngine) {
      this.survivalEngine.broadcasterEfficiencyMultiplier = multiplier;
    }

    // 定时恢复
    setTimeout(() => {
      this.broadcasterEfficiencyMultiplier = 1.0;
      if (this.survivalEngine) {
        this.survivalEngine.broadcasterEfficiencyMultiplier = 1.0;
      }
      console.log(`[SurvivalRoom:${this.roomId}] broadcaster efficiency_boost expired`);
    }, durationMs);
  }

  // ==================== 模拟器（测试/审核演示用）====================

  /**
   * 完整审核演示：模拟玩家加入/工作指令(1-4全覆盖)/礼物(T1-T5全覆盖)
   * 用于抖音平台审核 & 本地联调测试
   *
   * 有效工作指令：1=采食物 2=挖煤 3=挖矿 4=添柴升温（5已移除，6=夜晚攻击）
   * 每轮4名玩家各发不同指令(1/2/3/4)，持续多轮 → Worker持续有任务
   */
  _runSimulation() {
    if (this.survivalEngine.state === 'idle') {
      this.survivalEngine.startGame();
    }

    // ── 8名玩家分批加入 ─────────────────────────────────────────────
    const players = [
      { id: 'sim_0', name: '守卫甲',     avatar: '' },
      { id: 'sim_1', name: '极地风',     avatar: '' },
      { id: 'sim_2', name: '小丙',       avatar: '' },
      { id: 'sim_3', name: '采矿队长',   avatar: '' },
      { id: 'sim_4', name: '冬日生存王', avatar: '' },
      { id: 'sim_5', name: '厨神',       avatar: '' },
      { id: 'sim_6', name: '守门将庚',   avatar: '' },
      { id: 'sim_7', name: '夜枭战士',   avatar: '' },
    ];
    players.forEach((p, i) => {
      setTimeout(() => {
        this.survivalEngine.handlePlayerJoined(p.id, p.name, '');
      }, i * 400);
    });

    // ── 礼物演示序列已移除（改为日夜动态触发，见下方两个interval）──

    // ── 白天：每玩家独立随机工作循环（6~14秒，错开启动）──────────────
    const cmdCycle = [1, 2, 3, 4];
    const simPlayerTimers = new Map();  // playerId → timerId

    const schedulePlayerWork = (player, idx, delay) => {
      const timer = setTimeout(() => {
        if (!this._simRunning) return;
        const engineState = this.survivalEngine && this.survivalEngine.state;
        if (engineState === 'settlement' || engineState === 'idle') return;
        if (engineState === 'day') {
          // 每位玩家循环使用 cmdCycle，按各自计数轮转
          const cmd = cmdCycle[idx % 4];
          this.survivalEngine.handleComment(player.id, player.name, '', String(cmd));
          idx++;
        }
        // 随机 6~14 秒后再次触发（不论当前是白天还是夜晚，保持计时，等到白天再执行）
        const nextDelay = 6000 + Math.floor(Math.random() * 8000);
        schedulePlayerWork(player, idx, nextDelay);
      }, delay);
      simPlayerTimers.set(player.id, timer);
    };

    // 每个玩家错开 0~4 秒启动，避免同时触发
    players.forEach((p, i) => {
      const stagger = i * 600 + Math.floor(Math.random() * 1000);
      schedulePlayerWork(p, i, stagger);
    });

    this._simRunning = true;
    this._simPlayerTimers = simPlayerTimers;

    // ── 夜晚：随机攻击循环（25~45秒随机间隔，防止Boss被秒杀）──────────────────────────
    const scheduleNightAttack = () => {
      const delay = 25000 + Math.floor(Math.random() * 20000);
      this._simNightTimer = setTimeout(() => {
        if (!this._simRunning) return;
        const engineState = this.survivalEngine && this.survivalEngine.state;
        if (engineState === 'settlement' || engineState === 'idle') return;
        if (engineState === 'night') {
          const attacker = players[Math.floor(Math.random() * players.length)];
          this.survivalEngine.handleComment(attacker.id, attacker.name, '', '6');

          // 30% 概率随机小礼物
          if (Math.random() < 0.3) {
            const giftPool = [
              { id: 'fairy_wand', val: 1 },
              { id: 'ability_pill', val: 100 },
            ];
            const g = giftPool[Math.floor(Math.random() * giftPool.length)];
            const giver = players[Math.floor(Math.random() * players.length)];
            this.survivalEngine.handleGift(
              `sim_n_${Date.now()}`, giver.name, '', g.id, g.val, ''
            );
          }
        }
        scheduleNightAttack();
      }, delay);
    };
    scheduleNightAttack();

    // ── 白天增益礼物（每15秒，仅白天触发）──────────────────────────
    const dayGiftPool = [
      { id: 'fairy_wand',   val: 1    },
      { id: 'ability_pill', val: 100  },
      { id: 'donut',        val: 520  },
      { id: 'energy_battery', val: 990 },
    ];
    this._simDayGiftInterval = setInterval(() => {
      if (!this.survivalEngine || this.survivalEngine.state !== 'day') return;
      const g = dayGiftPool[Math.floor(Math.random() * dayGiftPool.length)];
      const giver = players[Math.floor(Math.random() * players.length)];
      this.survivalEngine.handleGift(`sim_dg_${Date.now()}`, giver.name, '', g.id, g.val, '');
    }, 15000);

    // ── 夜晚战斗礼物（每12秒，仅夜晚触发）──────────────────────────
    const nightGiftPool = [
      { id: 'ability_pill',    val: 100  },
      { id: 'energy_battery',  val: 990  },
      { id: 'love_explosion',  val: 1990 },
      { id: 'mystery_airdrop', val: 5200 },
    ];
    this._simNightGiftInterval = setInterval(() => {
      if (!this.survivalEngine || this.survivalEngine.state !== 'night') return;
      const g = nightGiftPool[Math.floor(Math.random() * nightGiftPool.length)];
      const giver = players[Math.floor(Math.random() * players.length)];
      this.survivalEngine.handleGift(`sim_ng_${Date.now()}`, giver.name, '', g.id, g.val, '');
    }, 12000);

    console.log(`[SurvivalRoom:${this.roomId}] Simulation started (8 players, per-player async timers, randomized day/night, dynamic gifts)`);
  }

  // ==================== 状态接口（RoomManager 查询用）====================

  getInfo() {
    return {
      roomId:       this.roomId,
      status:       this.status,
      clientCount:  this.clients.size,
      totalPlayers: this.survivalEngine.totalPlayers,
      gameState:    this.survivalEngine.state,
      currentDay:   this.survivalEngine.currentDay,
      lastActiveAt: this.lastActiveAt,
    };
  }
}

module.exports = SurvivalRoom;
