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
const {
  FEATURE_UNLOCK_DAY,
  isFeatureUnlocked,
} = require('./config/FeatureUnlockConfig');

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
   * @param {TribeWarManager} [tribeWarMgr]      §35 单例
   * @param {object} [globalServices]            §36 全局服务注入
   * @param {GlobalClock}      [globalServices.globalClock]
   * @param {SeasonManager}    [globalServices.seasonMgr]
   * @param {RoomPersistence}  [globalServices.roomPersistence]
   */
  constructor(roomId, gameConfig, tribeWarMgr = null, globalServices = null) {
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

    // §35 / §36 注入
    this.streamerName = roomId;  // MVP:以 roomId 作展示名,真实主播名由抖音 API 另行同步
    this.tribeWarMgr = tribeWarMgr;
    const extras = globalServices || {};
    this.globalClock     = extras.globalClock     || null;
    this.seasonMgr       = extras.seasonMgr       || null;
    this.roomPersistence = extras.roomPersistence || null;
    this.veteranTracker  = extras.veteranTracker  || null;   // §36.12 老用户豁免追踪

    // §36.12 房间创建者的 openId（用于查豁免字段；第一个 addPlayer 或 handleComment 的 secOpenId 写入）
    //   MVP：未注入时 _isRoomCreatorVeteran 直接返 false
    this.roomCreatorOpenId = null;
    this.creator = null;   // { isVeteran: boolean }，由 _refreshVeteranStatus() 维护

    if (typeof this.survivalEngine.setRoomContext === 'function') {
      this.survivalEngine.setRoomContext(this, tribeWarMgr, {
        globalClock:     this.globalClock,
        seasonMgr:       this.seasonMgr,
        roomPersistence: this.roomPersistence,
        veteranTracker:  this.veteranTracker,
      });
    }

    // §36 GlobalClock：注册到全局时钟（tick 每秒 broadcast world_clock_tick；phase 切换触发 engine 入口）
    if (this.globalClock && typeof this.globalClock.registerRoom === 'function') {
      this.globalClock.registerRoom(this);
    }

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
      // §36.12 从 ws._playerId 缓存 roomCreatorOpenId；若 WS 尚未带 openId（抖音 SDK 连接晚绑定），
      //   则稍后由 handleDouyinComment 兜底（见 handleDouyinComment 起首）
      if (ws && ws._playerId) {
        this.roomCreatorOpenId = ws._playerId;
        this._refreshVeteranStatus();
      }
      console.log(`[SurvivalRoom:${this.roomId}] Room creator assigned (first client, openId=${this.roomCreatorOpenId || 'pending'})`);
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

    // §36 销毁前保存一次快照
    if (this.roomPersistence) {
      try { this.roomPersistence.save(this); } catch (e) { /* ignore */ }
    }

    // §36 从 GlobalClock 注销
    if (this.globalClock && typeof this.globalClock.unregisterRoom === 'function') {
      try { this.globalClock.unregisterRoom(this); } catch (e) { /* ignore */ }
    }

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
      // §16 v1.26：永续模式下无胜利分支，survival_game_ended 移除 result 字段，此处固定按 'lose' 入榜
      // （StreamerRankingStore 仅在 result === 'win' 时计胜场，现在永续循环不再累加胜场；
      //   totalGames / maxDays / score 按最佳记录正常更新）
      try {
        const difficulty = this.survivalEngine._difficulty || 'normal';
        const streamerName = this.roomId; // 房间ID即主播标识，后续可改为真实昵称
        this.streamerRanking.addGameResult(this.roomId, streamerName, difficulty, d.dayssurvived || 0, 'lose');
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
  /**
   * §36.12 刷新 this.creator.isVeteran 标记（供 isFeatureUnlocked 查询）
   * 由 addClient / handleComment 等入口在识别到创建者时调用
   */
  _refreshVeteranStatus() {
    if (!this.veteranTracker) { this.creator = null; return; }
    if (!this.roomCreatorOpenId) { this.creator = null; return; }
    const isVet = this.veteranTracker.isVeteran(this.roomCreatorOpenId);
    this.creator = { isVeteran: isVet, openId: this.roomCreatorOpenId };
  }

  /**
   * §36.12 功能解锁守卫：返回 true 表示已解锁；false 表示已返回拒绝（调用方直接 return）
   * @param {string} featureId
   * @param {string} failType broadcast 消息 type（如 'build_propose_failed'）
   * @param {object} [extraFields] 附加到 failed data 的字段（如 action / itemId）
   * @returns {boolean}
   */
  _checkFeatureOrFail(featureId, failType, extraFields) {
    this._refreshVeteranStatus();   // 保证 isVeteran 最新
    if (isFeatureUnlocked(this, featureId)) return true;
    const cfg = FEATURE_UNLOCK_DAY[featureId];
    const unlockDay = cfg ? cfg.minDay : 0;
    if (failType) {
      const data = Object.assign({ reason: 'feature_locked', unlockDay }, extraFields || {});
      try {
        this.broadcast({ type: failType, timestamp: Date.now(), data });
      } catch (e) { /* ignore */ }
    }
    console.log(`[SurvivalRoom:${this.roomId}] feature_locked: featureId=${featureId} unlockDay=${unlockDay} failType=${failType}`);
    return false;
  }

  handleClientMessage(ws, msgType, data) {
    switch (msgType) {
      case 'start_game':
        // 新一局开始时，停止上一局残留的模拟器
        this._stopSimulation();
        // 支持难度参数：data.difficulty = 'easy' | 'normal' | 'hard'
        this.survivalEngine.startGame(data && data.difficulty ? data.difficulty : 'normal');
        break;
      case 'reset_game':
        // §36 重置前保存一次（保留 fortressDay / 持久化字段）
        if (this.roomPersistence) {
          try { this.roomPersistence.save(this); } catch (e) { /* ignore */ }
        }
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
      case 'upgrade_gate': {
        // 城门升级（需要矿石资源）
        // data.secOpenId 可选，用于记录操作者；也接受连接时注册的操作者 ID
        // 防御：客户端不可通过 source='gift_t6' 免费升级——只允许 broadcaster / expedition_trader
        const rawSource  = data && typeof data.source === 'string' ? data.source : 'broadcaster';
        const safeSource = rawSource === 'expedition_trader' ? 'expedition_trader' : 'broadcaster';
        // §36.12 按目标等级分流：Lv1→4 用 gate_upgrade_basic（D1）；Lv5→6 用 gate_upgrade_high（D4）
        //   客户端升级按钮按当前 gateLevel 决定 targetLv（currentLevel+1）；若无则按 basic 保守走
        const currentLv = this.survivalEngine.gateLevel || 1;
        const targetLv  = (data && Number.isFinite(data.targetLv)) ? data.targetLv : (currentLv + 1);
        const featureId = targetLv >= 5 ? 'gate_upgrade_high' : 'gate_upgrade_basic';
        // 客户端 GateUpgradeFailedData 权威字段名为 blockedLevel（§10/§19.2）
        if (!this._checkFeatureOrFail(featureId, 'gate_upgrade_failed', { blockedLevel: targetLv })) break;
        this.survivalEngine._upgradeGate(data && data.secOpenId || '', safeSource);
        break;
      }
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

      case 'end_game': {
        // §16.4 GM 手动结束游戏 → 阶段性结算（走失败结算 UI，但不触发 fortressDay 降级 / 不重置每日 cap）
        // §36 结束前保存一次（_enterSettlement 内部会再保存；此处双保险）
        if (this.roomPersistence) {
          try { this.roomPersistence.save(this); } catch (e) { /* ignore */ }
        }
        this._stopSimulation();
        // §16.4 v1.27 修正语病：必须逐项比较（state === 'day' || 'night' 按 JS 语义恒真，原表达有 bug）
        const engineState = this.survivalEngine.state;
        if (engineState === 'day' || engineState === 'night') {
          this.survivalEngine._enterSettlement('manual');  // 🆕 v1.26 单参签名
          console.log(`[SurvivalRoom:${this.roomId}] GM 手动结束游戏`);
        } else {
          // §16.4：loading / settlement / recovery / idle 下 GM 按钮应灰化；服务端兜底返回 wrong_phase
          // 按 upgrade_gate 失败消息风格广播 end_game_failed（主播客户端会据此收敛 UI）
          this.broadcast({
            type: 'end_game_failed',
            timestamp: Date.now(),
            data: { reason: 'wrong_phase', currentState: engineState },
          });
          console.log(`[SurvivalRoom:${this.roomId}] GM end_game rejected: wrong_phase (state=${engineState})`);
        }
        break;
      }
      case 'leave_room':
        // 客户端主动离开，忽略（连接断开由 WebSocket close 事件处理）
        break;
      case 'broadcaster_action': {
        // §36.12 broadcaster_boost 门槛（seasonDay ≥ 2）；失败带 action 子动作名
        const action = (data && data.action) || '';
        if (!this._checkFeatureOrFail('broadcaster_boost', 'broadcaster_action_failed', { action })) break;
        this._handleBroadcasterAction(ws, data);
        break;
      }

      // ==================== §24.4 主播事件轮盘 ====================
      // PM 决策（MVP）：_roomCreatorId 未注入 → 放开给任何玩家；
      //   TODO: 等 _roomCreatorId 实现后，这里应先校验 `_isRoomCreator(ws)` 再路由
      case 'broadcaster_roulette_spin': {
        // §36.12 roulette 门槛（seasonDay ≥ 1 — 默认解锁；仅为保险起见放置）
        if (!this._checkFeatureOrFail('roulette', 'roulette_spin_failed', {})) break;
        const pid = ws._playerId || '';
        this.survivalEngine.handleBroadcasterRouletteSpin(pid);
        break;
      }
      case 'broadcaster_roulette_apply': {
        if (!this._checkFeatureOrFail('roulette', 'roulette_spin_failed', {})) break;
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
        // §36.12 expedition 门槛（seasonDay ≥ 5）
        if (!this._checkFeatureOrFail('expedition', 'expedition_failed', {})) break;
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
        // §36.12 building 门槛（seasonDay ≥ 3）
        if (!this._checkFeatureOrFail('building', 'build_propose_failed', {})) break;
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
        // §36.12 building 门槛（锁定期静默丢弃；不推失败，避免"观众投票看到红字"的骚扰）
        this._refreshVeteranStatus();
        if (!isFeatureUnlocked(this, 'building')) {
          console.log(`[SurvivalRoom:${this.roomId}] build_vote silently dropped (feature locked, seasonDay=${this.survivalEngine.seasonMgr && this.survivalEngine.seasonMgr.seasonDay})`);
          break;
        }
        // { proposalId, buildId }
        const pid        = ws._playerId || (data && data.playerId) || '';
        const proposalId = (data && data.proposalId) || '';
        const buildId    = (data && data.buildId)    || '';
        this.survivalEngine.handleBuildVote(pid, proposalId, buildId);
        break;
      }

      // ==================== §39 商店系统 ====================
      // PM 决策（MVP）：_roomCreatorId 鉴权放开，引擎内不校验 isRoomCreator；
      //   未解锁/赛季末等守门一律跳过（策划案 v1.27 MVP 范围）
      case 'shop_list': {
        // §36.12 shop 门槛（seasonDay ≥ 2）；锁定期返回空目录（不推失败，避免客户端商店 Tab 刷红）
        const pidL = ws._playerId || (data && data.playerId) || '';
        const catL = (data && data.category) || '';
        this._refreshVeteranStatus();
        if (!isFeatureUnlocked(this, 'shop')) {
          this.broadcast({
            type: 'shop_list_data',
            timestamp: Date.now(),
            data: { playerId: pidL, category: catL, items: [] },
          });
          break;
        }
        this.survivalEngine.handleShopList(pidL, catL);
        break;
      }
      case 'shop_purchase_prepare': {
        // §36.12 shop 门槛
        const itemIdP = (data && data.itemId) || '';
        if (!this._checkFeatureOrFail('shop', 'shop_purchase_failed', { itemId: itemIdP })) break;
        // { itemId } — 仅主播 HUD B 类 ≥1000 时客户端调用
        const pid    = ws._playerId || (data && data.playerId) || '';
        this.survivalEngine.handleShopPurchasePrepare(pid, itemIdP);
        break;
      }
      case 'shop_purchase': {
        // §36.12 shop 门槛
        const itemIdPr = (data && data.itemId) || '';
        if (!this._checkFeatureOrFail('shop', 'shop_purchase_failed', { itemId: itemIdPr })) break;
        // { itemId, pendingId? }
        const pid       = ws._playerId || (data && data.playerId) || '';
        const pname     = (data && data.playerName)
                          || (this.survivalEngine.playerNames && this.survivalEngine.playerNames[pid])
                          || pid;
        const pendingId = (data && data.pendingId) || null;
        this.survivalEngine.handleShopPurchase(pid, pname, itemIdPr, pendingId);
        break;
      }
      case 'shop_equip': {
        // §36.12 shop 门槛
        const itemIdE = (data && data.itemId) || '';
        if (!this._checkFeatureOrFail('shop', 'shop_purchase_failed', { itemId: itemIdE })) break;
        // { slot, itemId? } — itemId 缺省/空 = 卸下该槽位
        const pid    = ws._playerId || (data && data.playerId) || '';
        const slot   = (data && data.slot)   || '';
        this.survivalEngine.handleShopEquip(pid, slot, itemIdE);
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

      // ==================== §35 Tribe War C→S ====================
      case 'tribe_war_room_list': {
        if (!this.tribeWarMgr) {
          ws.send(JSON.stringify({ type: 'tribe_war_room_list_result', timestamp: Date.now(), data: { rooms: [] } }));
          break;
        }
        const rooms = this.tribeWarMgr.getRoomList(this.roomId);
        ws.send(JSON.stringify({ type: 'tribe_war_room_list_result', timestamp: Date.now(), data: { rooms } }));
        break;
      }
      case 'tribe_war_attack': {
        // §36.12 tribe_war 门槛（seasonDay ≥ 7）
        if (!this._checkFeatureOrFail('tribe_war', 'tribe_war_attack_failed', {})) break;
        if (!this.tribeWarMgr) break;
        const targetRoomId = data && data.targetRoomId;
        if (!targetRoomId) {
          ws.send(JSON.stringify({ type: 'tribe_war_attack_failed', timestamp: Date.now(), data: { reason: 'room_not_found' } }));
          break;
        }
        // TODO _roomCreatorId 鉴权放开(MVP,同 §24.4/§37/§39)
        const res = this.tribeWarMgr.startAttack(this.roomId, targetRoomId);
        if (!res.ok) {
          ws.send(JSON.stringify({ type: 'tribe_war_attack_failed', timestamp: Date.now(), data: { reason: res.reason } }));
        }
        break;
      }
      case 'tribe_war_stop': {
        if (!this.tribeWarMgr) break;
        const sid = this.tribeWarMgr._attackerToSession.get(this.roomId);
        if (sid) {
          this.tribeWarMgr.stopAttack(sid, 'manual');
        }
        break;
      }
      case 'tribe_war_retaliate': {
        // §36.12 tribe_war 门槛（反击也走同一锁）
        if (!this._checkFeatureOrFail('tribe_war', 'tribe_war_attack_failed', {})) break;
        // 仅防守方(被攻击中)可反击;damageMultiplier 由服务端生成(MVP 默认 1.0,TODO §37 beacon)
        if (!this.tribeWarMgr) break;
        const underAttackSid = this.tribeWarMgr._defenderToSession.get(this.roomId);
        if (!underAttackSid) {
          ws.send(JSON.stringify({ type: 'tribe_war_attack_failed', timestamp: Date.now(), data: { reason: 'not_under_attack' } }));
          break;
        }
        const atkSession = this.tribeWarMgr._sessions.get(underAttackSid);
        const targetRoomId = (data && data.targetRoomId) ||
                             (atkSession && atkSession.attacker && atkSession.attacker.roomId);
        if (!targetRoomId) {
          ws.send(JSON.stringify({ type: 'tribe_war_attack_failed', timestamp: Date.now(), data: { reason: 'room_not_found' } }));
          break;
        }
        const res = this.tribeWarMgr.startAttack(this.roomId, targetRoomId, { damageMultiplier: 1.0 });
        if (!res.ok) {
          ws.send(JSON.stringify({ type: 'tribe_war_attack_failed', timestamp: Date.now(), data: { reason: res.reason } }));
        }
        break;
      }

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

    // §36.12 roomCreatorOpenId 兜底绑定：若 addClient 时 ws 未带 _playerId，此处第一次见到 secOpenId 时绑定
    //   MVP 简化：任一评论者（含主播本人）只要 roomCreatorOpenId 还空就补写；不严格区分"主播/观众"
    //   当 WS 连接有 _playerId 时 addClient 已写入，此处仅兜底
    if (!this.roomCreatorOpenId && secOpenId) {
      this.roomCreatorOpenId = secOpenId;
      this._refreshVeteranStatus();
      console.log(`[SurvivalRoom:${this.roomId}] roomCreatorOpenId fallback-bound to ${secOpenId}`);
    }

    // §36.12 markSeasonAttendance：每次观众评论都记录当前赛季日（适用于老用户判定）
    //   简化：只对 roomCreatorOpenId 匹配者标记；范围控制到创建者，避免存 200 人的记录爆炸
    if (this.veteranTracker && this.seasonMgr && secOpenId && secOpenId === this.roomCreatorOpenId) {
      try {
        this.veteranTracker.markSeasonAttendance(secOpenId, this.seasonMgr.seasonId, this.seasonMgr.seasonDay);
      } catch (e) { /* ignore */ }
    }

    const trimmed = (content || '').trim();
    const cmd = parseInt(trimmed);

    // 弹幕命令前缀识别(需经 handleComment 路由):
    //   §30.7 换肤 / §38 探险 / §37 建造 / §39 商店购买/装备 / §34 F8 提示(发"5")
    const isSkinCmd       = /^换肤(\d{1,2})?$/.test(trimmed);
    const isExpeditionCmd = (trimmed === '探' || trimmed === '召回');
    const isBuildCmd      = /^建\d{1,2}$/.test(trimmed);         // §37
    const isShopBuyCmd    = /^买[AB]\d{1,2}$/.test(trimmed);     // §39
    const isShopEquipCmd  = /^装[TFEB]\d{1,2}$/.test(trimmed);  // §39

    if ((cmd >= 1 && cmd <= 6) || trimmed === '666'
        || isSkinCmd || isExpeditionCmd
        || isBuildCmd || isShopBuyCmd || isShopEquipCmd) {
      // 工作/攻击/666/换肤/探险/建造/购买/装备指令 → 引擎解析
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
