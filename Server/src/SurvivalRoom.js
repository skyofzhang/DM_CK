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
const { findGiftById, findGiftByPrice } = require('./GiftConfig');
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

// 🔴 audit-r35 GAP-E25-08 B01 协议头 MVP：消息类型 → priority 数字映射（§19.0 设计）
//   0=critical（状态机/失败/赛季结束 — 必须立即处理）
//   1=high（游戏状态变更 — 优先处理）
//   2=normal（资源/排行/事件 — 默认）
//   3=low（debug / 装饰类 — 可降频）
function _resolveMessagePriority(messageType) {
  if (!messageType) return 2;
  // critical：状态机/失败/赛季关键事件
  if (messageType === 'phase_changed' ||
      messageType === 'room_failed' ||
      messageType === 'season_settlement' ||
      messageType === 'survival_game_ended' ||
      messageType === 'fortress_day_changed' ||
      messageType === 'free_death_pass_triggered' ||
      messageType === 'room_destroyed' ||
      messageType === 'season_state') return 0;
  // high：游戏状态/快照
  if (messageType === 'survival_game_state' ||
      messageType === 'room_state' ||
      messageType === 'world_clock_tick' ||
      messageType === 'game_paused' ||
      messageType === 'game_resumed' ||
      messageType === 'monster_wave_incoming' ||
      messageType === 'monster_wave' ||
      messageType === 'boss_appeared' ||
      messageType === 'gate_breach_warning') return 1;
  // low：debug / 装饰
  if (messageType === 'debug' ||
      messageType === 'live_comment') return 3;
  // 其他（资源/排行/礼物/事件等）= normal
  return 2;
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

    // 🔴 audit-r35 GAP-E25-08 B01 协议头 MVP：每条 S→C 广播自动注入 tick / serverTime / priority
    //   §19.0 设计：服务端递增计数 tick + Unix ms serverTime + priority 数字（0=critical / 1=high / 2=normal / 3=low）
    //   客户端可按 tick 序号识别"服务端重启 vs 客户端追赶"，按 priority 数字处理（MVP 仅 log）
    this._globalTick = 0;

    // WebSocket 客户端集合
    this.clients = new Set();

    // 房间创建者：
    //   - GM 模式（isGMMode=true）：第一个连接的 ws 即主播（first-client 兜底）
    //   - 真实抖音模式：roomCreatorOpenId 由 /api/douyin/init 预注入（owner open_id），
    //     ws 的 _playerId / openId 需与之匹配才认定为主播
    // 用于 broadcaster_action 权限验证
    this.roomCreatorWs = null;

    // 房间运行模式标记：
    //   false = 真实抖音（默认；roomCreatorOpenId 必须严格匹配）
    //   true  = GM 模式（first-client 绑定，roomCreatorOpenId 形如 gm_creator_xxx）
    // 第一条携带 isGMMode 的 join_room 会缓存到这里。
    this.isGMMode = false;
    // 仅首次 join_room 决定房间模式；后续 join（重连等）不再变更
    this._modeInitialized = false;

    // 主播专用：效率加速乘数（broadcaster_action触发）
    this.broadcasterEfficiencyMultiplier = 1.0;
    this._efficiencyBoostTimer = null;
    this._efficiencyBoostExpireAt = 0;

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

  /**
   * 由 /api/douyin/init 回调调用，预绑定抖音平台 owner open_id
   * 这样当主播 WS 连上来发 join_room 时，真实抖音模式能据此认定创建者
   * @param {string} openId - 抖音 anchor_open_id（从 GetRoomInfo 获取）
   */
  bindDouyinAnchor(openId) {
    if (!openId) return;
    // 仅在 GM 模式未占位 / 尚未绑定时写入，避免覆盖 GM 占位或已有的真实 openId
    if (this.isGMMode) {
      console.log(`[SurvivalRoom:${this.roomId}] bindDouyinAnchor skipped (room is in GM mode)`);
      return;
    }
    if (this.roomCreatorOpenId && this.roomCreatorOpenId === openId) return;
    this.roomCreatorOpenId = openId;
    this._refreshVeteranStatus();
    // 🔴 audit-r44 GAP-E44-01：r43 GAP-D43-05 仅在 setRoomContext (SurvivalGameEngine.js:992) 末尾按 room.roomCreatorOpenId 注入 _roomCreatorId，
    //   但 setRoomContext 在 SurvivalRoom 构造函数 line 121 调用，此时真实抖音模式 roomCreatorOpenId 可能由 /api/douyin/init 已预设、
    //   也可能 bindDouyinAnchor 在房间生命周期中后续才被调用 (e.g. RoomManager 路径) → 引擎 _roomCreatorId 永久为 null
    //   → 主播 AFK 60s 仍被替补（违背 §33.5「主播永不替换」），跨 r37→r43 共 7 轮 audit 漏检（r43 commit 标"已闭环"实为半成品延续）。
    //   修复：所有设置 roomCreatorOpenId 的路径（bindDouyinAnchor / addClient 真实抖音 / addClient GM first-client）都同步注入引擎字段。
    this._syncRoomCreatorIdToEngine();
    console.log(`[SurvivalRoom:${this.roomId}] bindDouyinAnchor: roomCreatorOpenId=${openId}`);
  }

  /**
   * 🔴 audit-r44 GAP-E44-01：把 roomCreatorOpenId 同步到 SurvivalGameEngine._roomCreatorId
   * 调用点：bindDouyinAnchor / addClient 真实抖音 openId 命中 / addClient GM first-client 兜底 / handleJoinRoom 升级 creator / handleDouyinComment fallback-bind
   * 容错：survivalEngine 未初始化时静默 skip（构造函数 line 121 后才进入此 helper）
   */
  _syncRoomCreatorIdToEngine() {
    if (this.survivalEngine && this.roomCreatorOpenId) {
      this.survivalEngine._roomCreatorId = this.roomCreatorOpenId;
    }
  }

  /**
   * 处理 join_room 消息（来自 RoomManager.routeMessage / handleClientConnect）
   * 读取 isGMMode / playerId / playerName 并决定主播绑定策略
   * @param {WebSocket} ws
   * @param {object} joinData - { roomId, isGMMode?, explicitGMMode?, playerId?, playerName? }
   */
  handleJoinRoom(ws, joinData) {
    const isGMMode = !!(joinData && joinData.isGMMode === true);
    const playerId = (joinData && typeof joinData.playerId === 'string') ? joinData.playerId : '';

    // 缓存 openId 到 ws（便于后续 _isRoomCreator 判定 / 审计）
    if (playerId) {
      ws._playerId = playerId;
      ws.openId = playerId;  // 保留别名，便于日志/审计引用
    }

    // 仅首次带 isGMMode 的 join_room 决定房间模式；后续 join（重连等）尊重已有 mode
    if (!this._modeInitialized) {
      this.isGMMode = isGMMode;
      this._modeInitialized = true;
      if (isGMMode) {
        // GM 模式：若尚未有 roomCreatorOpenId，用随机 token 占位（方便日志追踪）
        if (!this.roomCreatorOpenId) {
          this.roomCreatorOpenId = 'gm_creator_' + Math.random().toString(36).slice(2, 10);
        }
      }
      console.log(`[DrscfZ-Net] Room mode initialized: roomId=${this.roomId}, isGMMode=${this.isGMMode}, creatorOpenId=${this.roomCreatorOpenId || 'pending'}`);
    }

    // 已在 clients 集合内的 ws（URL 直连先 addClient，join_room 后补签模式）
    //   → 不重复 addClient；只需重新判定 creator 身份并回补 join_room_confirm
    if (this.clients.has(ws)) {
      // 真实抖音模式下 openId 匹配时升级为 creator（URL 直连时默认非 creator）
      if (!this.isGMMode && this.roomCreatorOpenId && !String(this.roomCreatorOpenId).startsWith('gm_creator_')
          && ws._playerId && ws._playerId === this.roomCreatorOpenId) {
        this.roomCreatorWs = ws;
        // 🔴 audit-r44 GAP-E44-01：handleJoinRoom 升级 creator 路径同步
        this._syncRoomCreatorIdToEngine();
        console.log(`[SurvivalRoom:${this.roomId}] Creator upgraded on late join_room (douyin mode): openId=${ws._playerId}`);
      }
      // 回补 join_room_confirm（客户端可重读 isRoomCreator）
      // 🔴 audit-r41 GAP-PM41-01: 改用 _sendToClient helper，自动注入 B01 协议头三字段
      this._sendToClient(ws, {
        type: 'join_room_confirm',
        data: {
          isRoomCreator: this._isRoomCreator(ws),
          has_active_session: this.survivalEngine &&
            this.survivalEngine.state !== 'idle' &&
            this.survivalEngine.state !== undefined
        }
      });
      return;
    }

    // 新 ws：走 addClient 正常流程（内部据 isGMMode/openId 决定是否为 creator）
    this.addClient(ws);
  }

  addClient(ws) {
    // 真实抖音模式：优先匹配 openId → 命中者即为主播
    // GM 模式 / 尚未绑定 openId：沿用"第一个连接即主播"兜底
    if (!this.isGMMode && this.roomCreatorOpenId && !String(this.roomCreatorOpenId).startsWith('gm_creator_')) {
      // 真实抖音模式：只认 openId 匹配
      if (ws._playerId && ws._playerId === this.roomCreatorOpenId) {
        this.roomCreatorWs = ws;
        // 🔴 audit-r44 GAP-E44-01：addClient 真实抖音 openId 命中路径同步引擎字段
        this._syncRoomCreatorIdToEngine();
        console.log(`[SurvivalRoom:${this.roomId}] Creator identified by openId match (douyin mode): openId=${ws._playerId}`);
      } else {
        console.log(`[SurvivalRoom:${this.roomId}] Client joined as viewer (douyin mode, openId=${ws._playerId || 'none'}, expected=${this.roomCreatorOpenId})`);
      }
    } else {
      // GM 模式 / 未知模式：first-client 兜底
      const isFirstClient = this.clients.size === 0 && this.roomCreatorWs === null;
      if (isFirstClient) {
        this.roomCreatorWs = ws;
        // §36.12 从 ws._playerId 缓存 roomCreatorOpenId；GM 模式下若构造时已写入 gm_creator_* 占位则保留
        if (ws && ws._playerId && !this.roomCreatorOpenId) {
          this.roomCreatorOpenId = ws._playerId;
        }
        this._refreshVeteranStatus();
        // 🔴 audit-r44 GAP-E44-01：addClient GM 模式 first-client 兜底路径同步引擎字段
        this._syncRoomCreatorIdToEngine();
        console.log(`[SurvivalRoom:${this.roomId}] Room creator assigned (first client fallback, openId=${this.roomCreatorOpenId || 'pending'}, isGMMode=${this.isGMMode})`);
      }
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
    const isCreator = this._isRoomCreator(ws);
    const hasActiveSession = this.survivalEngine &&
      this.survivalEngine.state !== 'idle' &&
      this.survivalEngine.state !== undefined;
    // 🔴 audit-r41 GAP-PM41-01: 改用 _sendToClient helper，自动注入 B01 协议头三字段
    this._sendToClient(ws, {
      type: 'join_room_confirm',
      data: {
        isRoomCreator: isCreator,
        has_active_session: hasActiveSession
      }
    });

    // 新客户端连接：发送当前完整状态
    this._sendStateToClient(ws);
    // P0-A5 room_state：首次连接 / start_game 等效场景 → 断线重连快照推一次
    try {
      if (this.survivalEngine && typeof this.survivalEngine._broadcastRoomState === 'function') {
        this.survivalEngine._broadcastRoomState('addClient');
      }
    } catch (e) { /* ignore */ }
    console.log(`[DrscfZ-Net] Mode=${this.isGMMode ? 'GM' : 'DOUYIN'}, roomId=${this.roomId}, openId=${ws.openId || ws._playerId || 'N/A'}, isCreator=${isCreator}, clients=${this.clients.size}`);
  }

  removeClient(ws) {
    this.clients.delete(ws);

    // 主播断线后的创建者转交策略：
    //   - 真实抖音模式：roomCreatorOpenId 由 /api/douyin/init 固定，不能转交给观众；
    //     仅清空 roomCreatorWs，等待主播 WS 重连时按 openId 重新绑定
    //   - GM 模式：沿用 first-client 兜底，转交给当前第一个在线客户端（便于本地调试）
    if (ws === this.roomCreatorWs) {
      if (!this.isGMMode && this.roomCreatorOpenId && !String(this.roomCreatorOpenId).startsWith('gm_creator_')) {
        // 真实抖音模式：仅清空 ws 引用；openId 保留，等重连
        this.roomCreatorWs = null;
        console.log(`[SurvivalRoom:${this.roomId}] Creator WS disconnected (douyin mode, openId preserved=${this.roomCreatorOpenId})`);
      } else if (this.clients.size > 0) {
        this.roomCreatorWs = this.clients.values().next().value;
        console.log(`[SurvivalRoom:${this.roomId}] Creator disconnected, transferring creator to next client (GM mode fallback)`);
        // 🔴 audit-r42 GAP-C42-01: GM 模式掉线主播转移路径必须注入 B01 协议头三字段。
        //   r41 标"协议头单播路径全覆盖"实仅扫了 ws.send，漏抓 this.roomCreatorWs.send 模式。
        //   改走 _sendToClient helper 自动注入 tick/serverTime/priority 三字段（与 join_room_confirm
        //   入房路径单播模式一致），避免客户端 priority 队列读不到 GM 转移的 isRoomCreator 单播。
        this._sendToClient(this.roomCreatorWs, {
          type: 'join_room_confirm',
          data: { isRoomCreator: true }
        });
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

    if (this._simWorkInterval)      { clearInterval(this._simWorkInterval); this._simWorkInterval = null; }
    if (this._simNightInterval)     { clearInterval(this._simNightInterval); this._simNightInterval = null; }
    if (this._simDayGiftInterval)   { clearInterval(this._simDayGiftInterval); this._simDayGiftInterval = null; }
    if (this._simNightGiftInterval) { clearInterval(this._simNightGiftInterval); this._simNightGiftInterval = null; }
  }

  destroy() {
    this._cancelPauseTimer();
    this._stopSimulation();
    if (this._efficiencyBoostTimer) {
      clearTimeout(this._efficiencyBoostTimer);
      this._efficiencyBoostTimer = null;
    }
    if (this.survivalEngine && typeof this.survivalEngine.stop === 'function') {
      this.survivalEngine.stop();
    } else if (this.survivalEngine) {
      this.survivalEngine.pause();
    }

    // §36 销毁前保存一次快照
    if (this.roomPersistence) {
      try { this.roomPersistence.save(this); } catch (e) { /* ignore */ }
    }

    // 🔴 audit-r44 GAP-E44-02：r? 起 TribeWarManager 已实现 onRoomDestroyed (TribeWarManager.js:205) 但全代码 0 调用方
    //   后果：30min 无人 → 房间 destroy → 旧 TribeWarSession 仍残留在 _sessions / _attackerToSession / _defenderToSession Map
    //   → 同 roomId 重新开播时 startAttack 查 _attackerToSession.has(roomId) 命中残留旧 session → 攻击拒绝 'attacker_busy'
    //   → tribe_war_attack_ended.reason='room_destroyed' (§35.10 5 种 reason 之一) 永不被广播。
    //   修复：destroy 时清理 TribeWar session（在 globalClock.unregisterRoom 之前，对齐资源清理顺序）。
    if (this.tribeWarMgr && typeof this.tribeWarMgr.onRoomDestroyed === 'function') {
      try { this.tribeWarMgr.onRoomDestroyed(this.roomId); } catch (e) { /* ignore */ }
    }

    // §36 从 GlobalClock 注销
    if (this.globalClock && typeof this.globalClock.unregisterRoom === 'function') {
      try { this.globalClock.unregisterRoom(this); } catch (e) { /* ignore */ }
    }

    this.status = 'destroyed';

    for (const ws of this.clients) {
      try {
        // 🔴 audit-r41 GAP-PM41-01: 改用 _sendToClient helper，自动注入 B01 协议头三字段
        this._sendToClient(ws, {
          type: 'room_destroyed',
          data: { roomId: this.roomId, reason: 'timeout' }
        });
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
      const weeklyRankings = d.weeklyRankings || d.rankings;
      if (weeklyRankings) {
        try {
          this.weeklyRanking.addGameResult(weeklyRankings);
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
        // §14 v1.27：废止 difficulty 三档系统；StreamerRankingStore.addGameResult 仍接受兼容字段，传 'normal' 占位（不参与 v1.26 maxFortressDay 排名权重）
        const streamerName = this.roomId; // 房间ID即主播标识，后续可改为真实昵称
        this.streamerRanking.addGameResult(this.roomId, streamerName, 'normal', d.dayssurvived || 0, 'lose');
        setTimeout(() => this._broadcastStreamerRanking(), 600);
      } catch (e) {
        console.error(`[SurvivalRoom:${this.roomId}] StreamerRanking update error: ${e.message}`);
      }
    }

    if (this.clients.size === 0) return;
    message.roomId = this.roomId;

    // 🔴 audit-r35 GAP-E25-08 B01 协议头 MVP：注入公共字段（不覆盖 emit 处已设的同名字段，向后兼容）
    // 🔴 audit-r40 GAP-PM40-02：抽成 _injectProtocolHeader helper，让 _sendToClient / _sendToPlayer 单播路径也可复用
    this._injectProtocolHeader(message);

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
   * 🔴 audit-r40 GAP-PM40-02: 协议头三字段注入 helper（B01 协议头 MVP 公共逻辑）
   *   - tick: 全局递增计数（客户端可据此检测服务端重启）
   *   - serverTime: 服务端时间戳（客户端时钟对齐）
   *   - priority: 4 档优先级（critical / high / normal / low），客户端 priority 队列消费
   *
   * 调用方：
   *   - this.broadcast() 路径（line 432+ 已用）
   *   - this._sendToClient(ws, msg) 单播路径（line ~1230+ 新加 helper）
   *   - this.survivalEngine._sendToPlayer(playerId, msg) 引擎单播路径（line ~995 已注入）
   *
   * @param {object} message - 完整消息体（{ type, data, ... }）
   */
  _injectProtocolHeader(message) {
    if (!message) return;
    if (message.tick === undefined) message.tick = ++this._globalTick;
    if (message.serverTime === undefined) message.serverTime = Date.now();
    if (message.priority === undefined) message.priority = _resolveMessagePriority(message.type);
  }

  /**
   * 🔴 audit-r40 GAP-PM40-02: 单播给指定 ws helper，注入 B01 协议头三字段
   *   替代直接 `ws.send(JSON.stringify({...}))` 模式（绕过 broadcast() 注入）。
   *
   * 调用方典型场景：
   *   - _requireBroadcaster() 单播错误响应
   *   - heartbeat_ack 心跳响应
   *   - tribe_war_attack_failed / tribe_war_room_list_result 单播失败响应
   *   - weekly_ranking / streamer_ranking 主动请求响应（C2S 同步请求）
   *
   * @param {WebSocket} ws
   * @param {object} msg - 完整消息体（{ type, data, ... }），自动补 roomId/timestamp
   * @returns {boolean}
   */
  _sendToClient(ws, msg) {
    if (!ws || !msg) return false;
    if (typeof ws.send !== 'function' || ws.readyState !== 1) return false;
    try {
      const copy = Object.assign({ roomId: this.roomId, timestamp: Date.now() }, msg);
      this._injectProtocolHeader(copy);
      ws.send(JSON.stringify(copy));
      return true;
    } catch (e) {
      console.warn(`[SurvivalRoom:${this.roomId}] _sendToClient error: ${e.message}`);
      return false;
    }
  }

  /**
   * 向所有客户端广播最新本周贡献榜
   * 🔴 audit-r40 GAP-PM40-03：r36 标"主播/周榜走 broadcast()"实际未真闭环（半成品延续模式第 9 轮再现）—
   *   r36 之前是 for-loop client.send 直发；r36 commit message 标修但代码仍用 for-loop（未实际改动）；
   *   r40 真闭环：改用 this.broadcast() 自动注入 B01 协议头三字段（tick/serverTime/priority）。
   */
  _broadcastWeeklyRanking() {
    const payload = this.weeklyRanking.getPayload(10);
    this.broadcast({
      type:      'weekly_ranking',
      timestamp: Date.now(),
      data:      payload,
    });
    console.log(`[SurvivalRoom:${this.roomId}] 本周榜已广播，week=${payload.week}，条数=${payload.rankings.length}`);
  }

  /**
   * 向所有客户端广播最新主播排行榜
   * 🔴 audit-r40 GAP-PM40-03：同上 r36 半成品延续模式第 9 轮再现，r40 真闭环走 this.broadcast()
   */
  _broadcastStreamerRanking() {
    const payload = this.streamerRanking.getPayload(10);
    this.broadcast({
      type:      'streamer_ranking',
      timestamp: Date.now(),
      data:      payload,
    });
    console.log(`[SurvivalRoom:${this.roomId}] 主播榜已广播，条数=${payload.rankings.length}`);
  }

  _sendStateToClient(ws) {
    // 🔴 audit-r40 GAP-PM40-02：改用 _sendToClient helper，自动注入 B01 协议头三字段
    this._sendToClient(ws, {
      type: 'survival_game_state',
      data: this.survivalEngine.getFullState()
    });
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
   * P0-A8：feature_locked 一律改为对发起方单播（ws.send），避免向全房间广播红字骚扰。
   * @param {string} featureId
   * @param {string} failType    消息 type（如 'build_propose_failed'）
   * @param {object} [extraFields] 附加到 failed data 的字段（如 action / itemId）
   * @param {WebSocket} [ws]     调用方 WebSocket（若提供则单播，否则兜底 broadcast 保持兼容）
   * @returns {boolean}
   */
  _checkFeatureOrFail(featureId, failType, extraFields, ws) {
    this._refreshVeteranStatus();   // 保证 isVeteran 最新
    if (isFeatureUnlocked(this, featureId)) return true;
    const cfg = FEATURE_UNLOCK_DAY[featureId];
    const unlockDay = cfg ? cfg.minDay : 0;
    if (failType) {
      const msg = {
        type:      failType,
        timestamp: Date.now(),
        data:      Object.assign({ reason: 'feature_locked', unlockDay }, extraFields || {}),
      };
      // P0-A8 单播：优先发给 ws；未提供 ws（纯弹幕入口等）时仍 broadcast 兜底
      // 🔴 audit-r41 GAP-PM41-01: 改用 _sendToClient helper，自动注入 B01 协议头三字段；失败时 fallback broadcast
      if (ws && this._sendToClient(ws, msg)) {
        // 单播成功
      } else {
        try {
          this.broadcast(msg);
        } catch (e) { /* ignore */ }
      }
    }
    console.log(`[SurvivalRoom:${this.roomId}] feature_locked: featureId=${featureId} unlockDay=${unlockDay} failType=${failType}`);
    return false;
  }

  handleClientMessage(ws, msgType, data) {
    switch (msgType) {
      case 'start_game':
        if (!this._requireBroadcaster(ws, 'start_game')) break;
        // 新一局开始时，停止上一局残留的模拟器
        this._stopSimulation();
        // §14 v1.27：废止 difficulty 三档系统；startGame 不再接受 difficulty 参数（统一基线 + fortressDay 渐进压力曲线）
        this.survivalEngine.startGame();
        // P0-A5 room_state：start_game 后推一次房间全量快照
        try {
          if (typeof this.survivalEngine._broadcastRoomState === 'function') {
            this.survivalEngine._broadcastRoomState('start_game');
          }
        } catch (_) { /* ignore */ }
        this._gmAudit(ws, 'start_game', {});
        break;
      case 'reset_game':
        if (!this._requireBroadcaster(ws, 'reset_game')) break;
        // §36 重置前保存一次（保留 fortressDay / 持久化字段）
        if (this.roomPersistence) {
          try { this.roomPersistence.save(this); } catch (e) { /* ignore */ }
        }
        this._stopSimulation();
        this.survivalEngine.reset();
        // reset() 本身只改内存状态；立刻广播 idle 快照，避免客户端一直停在 Loading 等待 15s 超时。
        this.broadcast({
          type: 'survival_game_state',
          timestamp: Date.now(),
          data: this.survivalEngine.getFullState(),
        });
        try {
          if (typeof this.survivalEngine._broadcastRoomState === 'function') {
            this.survivalEngine._broadcastRoomState('reset_game');
          }
        } catch (_) { /* ignore */ }
        this._gmAudit(ws, 'reset_game', {});
        break;
      case 'sync_state':
        // 客户端请求重新同步当前游戏状态（用于"继续上一局"场景）
        this._sendStateToClient(ws);
        // P0-A5 room_state：断线重连后推一次房间全量快照（含轮盘/建造/探险/攻防战 in-progress）
        try {
          if (this.survivalEngine && typeof this.survivalEngine._broadcastRoomState === 'function') {
            this.survivalEngine._broadcastRoomState('sync_state');
          }
        } catch (_) { /* ignore */ }
        // §17.15：若节流窗口尚新（≤ ONBOARDING_REPLAY_WINDOW_MS），
        // 沿用同一 sessionId 给该客户端补发一次 show_onboarding_sequence（客户端幂等）
        // 🔴 audit-r41 GAP-PM41-01: 改用 _sendToClient helper，自动注入 B01 协议头三字段
        this.survivalEngine._replayOnboardingIfInWindow((msg) => {
          this._sendToClient(ws, msg);
        });
        break;
      case 'heartbeat':
        // 🔴 audit-r41 GAP-PM41-01: 改用 _sendToClient helper，自动注入 B01 协议头三字段
        this._sendToClient(ws, { type: 'heartbeat_ack' });
        break;
      case 'upgrade_gate': {
        if (!this._requireBroadcaster(ws, 'upgrade_gate')) break;
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
        if (!this._checkFeatureOrFail(featureId, 'gate_upgrade_failed', { blockedLevel: targetLv }, ws)) break;
        this.survivalEngine._upgradeGate(data && data.secOpenId || '', safeSource);
        this._gmAudit(ws, 'upgrade_gate', { targetLv, source: safeSource });
        break;
      }
      case 'toggle_sim':
        if (!this._requireBroadcaster(ws, 'toggle_sim')) break;
        // 生存游戏模拟器（直接触发测试事件）
        if (data && data.enabled) {
          this._runSimulation();
        } else {
          this._stopSimulation();
        }
        this._gmAudit(ws, 'toggle_sim', { enabled: !!(data && data.enabled) });
        break;
      case 'get_weekly_ranking':
        // 客户端请求本周贡献榜（面板打开时触发）
        // 🔴 audit-r41 GAP-PM41-01: 改用 _sendToClient helper，自动注入 B01 协议头三字段
        try {
          const n = (data && data.top && data.top <= 50) ? data.top : 50;
          const payload = this.weeklyRanking.getPayload(n);
          this._sendToClient(ws, {
            type: 'weekly_ranking',
            data: payload,
          });
        } catch (e) {
          console.error(`[SurvivalRoom:${this.roomId}] get_weekly_ranking error: ${e.message}`);
        }
        break;

      case 'get_streamer_ranking':
        // 客户端请求主播排行榜
        // 🔴 audit-r41 GAP-PM41-01: 改用 _sendToClient helper，自动注入 B01 协议头三字段
        try {
          const sN = (data && data.top && data.top <= 50) ? data.top : 50;
          const sPayload = this.streamerRanking.getPayload(sN);
          this._sendToClient(ws, {
            type: 'streamer_ranking',
            data: sPayload,
          });
        } catch (e) {
          console.error(`[SurvivalRoom:${this.roomId}] get_streamer_ranking error: ${e.message}`);
        }
        break;

      case 'end_game': {
        if (!this._requireBroadcaster(ws, 'end_game')) break;
        // §16.4 GM 手动结束游戏 → 阶段性结算（走失败结算 UI，但不触发 fortressDay 降级 / 不重置每日 cap）
        // §36 结束前保存一次（_enterSettlement 内部会再保存；此处双保险）
        if (this.roomPersistence) {
          try { this.roomPersistence.save(this); } catch (e) { /* ignore */ }
        }
        this._stopSimulation();
        this._gmAudit(ws, 'end_game', { state: this.survivalEngine.state });
        // §16.4 v1.27 修正语病：必须逐项比较（state === 'day' || 'night' 按 JS 语义恒真，原表达有 bug）
        const engineState = this.survivalEngine.state;
        if (engineState === 'day' || engineState === 'night') {
          this.survivalEngine._enterSettlement('manual');  // 🆕 v1.26 单参签名
          console.log(`[SurvivalRoom:${this.roomId}] GM 手动结束游戏`);
        } else {
          // §16.4：loading / settlement / recovery / idle 下 GM 按钮应灰化；服务端兜底返回 wrong_phase
          // 🔴 audit-r43 GAP-B43-04：单播给主播本人（与 r41 GAP-PM41-01 单播大潮收敛模式一致）
          //   原 broadcast 路径会骚扰其他观众；客户端 HandleEndGameFailed 虽有 _isRoomCreator 过滤兜底，
          //   但服务端层应统一"主播失败反馈一律单播"，避免依赖客户端兜底
          this._sendToClient(ws, {
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
        const action = (data && data.action) || '';
        if (!this._requireBroadcaster(ws, action || 'broadcaster_action')) break;
        // §36.12 broadcaster_boost 门槛（seasonDay ≥ 2）；失败带 action 子动作名
        if (!this._checkFeatureOrFail('broadcaster_boost', 'broadcaster_action_failed', { action }, ws)) break;
        // r15 GAP-B-MAJOR-01：策划案 §24.3 L2675 明示 broadcaster_action 仅 day/night 受理，恢复期/其他状态拒绝
        // 🔴 audit-r41 GAP-PM41-01: 改用 _sendToClient helper，自动注入 B01 协议头三字段
        const _engineState = this.survivalEngine && this.survivalEngine.state;
        if (_engineState !== 'day' && _engineState !== 'night') {
          this._sendToClient(ws, {
            type: 'broadcaster_action_failed',
            data: { action, reason: 'wrong_phase' },
          });
          break;
        }
        this._handleBroadcasterAction(ws, data);
        break;
      }

      // ==================== §24.4 主播事件轮盘 ====================
      case 'broadcaster_roulette_spin': {
        if (!this._requireBroadcaster(ws, 'roulette_spin')) break;
        // §36.12 roulette 门槛（seasonDay ≥ 1 — 默认解锁；仅为保险起见放置）
        if (!this._checkFeatureOrFail('roulette', 'roulette_spin_failed', {}, ws)) break;
        const pid = ws._playerId || '';
        this.survivalEngine.handleBroadcasterRouletteSpin(pid);
        this._gmAudit(ws, 'roulette_spin', {});
        break;
      }
      case 'broadcaster_roulette_apply': {
        if (!this._requireBroadcaster(ws, 'roulette_apply')) break;
        if (!this._checkFeatureOrFail('roulette', 'roulette_spin_failed', {}, ws)) break;
        const pid = ws._playerId || '';
        // audit-r12 GAP-B03：透传 cardId 给 engine 做防伪造比对
        const clientCardId = (data && typeof data.cardId === 'string') ? data.cardId : null;
        this.survivalEngine.handleBroadcasterRouletteApply(pid, clientCardId);
        this._gmAudit(ws, 'roulette_apply', { cardId: clientCardId });
        break;
      }
      case 'broadcaster_trader_accept': {
        if (!this._requireBroadcaster(ws, 'trader_accept')) break;
        const pid = ws._playerId || '';
        const choice = (data && data.choice) || '';
        this.survivalEngine.handleBroadcasterTraderAccept(pid, choice);
        this._gmAudit(ws, 'trader_accept', { choice });
        break;
      }

      // ==================== §38 探险系统 ====================
      case 'expedition_command': {
        // §36.12 expedition 门槛（seasonDay ≥ 5）
        if (!this._checkFeatureOrFail('expedition', 'expedition_failed', {}, ws)) break;

        // { playerId, action: 'send' | 'recall' }
        const action = (data && data.action) || '';

        if (action === 'send') {
          // send 仅允许本人发起，忽略 data.playerId，防止伪造他人身份
          const pid = ws._playerId || '';
          if (!pid) break;
          this.survivalEngine.handleExpeditionCommand(pid, 'send');
          break;
        }

        if (action === 'recall') {
          // recall 仅主播可用（§38.5）
          // 🔴 audit-r41 GAP-PM41-01: 改用 _sendToClient helper，自动注入 B01 协议头三字段
          if (!this._isRoomCreator(ws)) {
            this._sendToClient(ws, {
              type: 'expedition_failed',
              data: { playerId: ws._playerId || '', reason: 'supporter_not_allowed', unlockDay: 0 },
            });
            break;
          }
          const targetPid = (data && data.playerId) || '';
          if (!targetPid) break;
          this.survivalEngine.handleExpeditionCommand(targetPid, 'recall', ws._playerId || targetPid);
          this._gmAudit(ws, 'expedition_recall', { targetPlayerId: targetPid });
          break;
        }

        break;
      }
      case 'expedition_event_vote': {
        // 仅主播可发（§38.5）
        if (!this._requireBroadcaster(ws, 'expedition_event_vote')) break;
        // { expeditionId, choice: 'accept' | 'cancel' }
        const pid    = ws._playerId || '';
        const expId  = (data && data.expeditionId) || '';
        const choice = (data && data.choice) || '';
        this.survivalEngine.handleExpeditionEventVote(pid, expId, choice);
        this._gmAudit(ws, 'expedition_event_vote', { expeditionId: expId, choice });
        break;
      }

      // ==================== §37 建造系统 ====================
      case 'build_propose': {
        // §36.12 building 门槛（seasonDay ≥ 3）
        if (!this._checkFeatureOrFail('building', 'build_propose_failed', {}, ws)) break;
        // { buildId, playerName? }
        const pid   = ws._playerId || '';
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
        const pid        = ws._playerId || '';
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
        const pidL = ws._playerId || '';
        const catL = (data && data.category) || '';
        this._refreshVeteranStatus();
        if (!isFeatureUnlocked(this, 'shop')) {
          this._sendToClient(ws, {
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
        if (!this._checkFeatureOrFail('shop', 'shop_purchase_failed', { itemId: itemIdP }, ws)) break;
        // ⚠️ audit-r24 GAP-E24-20 / 🔴 audit-r46 GAP-M-04（codex 路线 B）：
        //   shop_purchase_prepare 走 _requireBroadcaster，与 §24.3 主播专属动作统一；
        //   非主播单播 broadcaster_action_failed/not_broadcaster，且不会创建 pending（§17.16 modal 安全意图保留）。
        //   §39.7 旧版"否则静默忽略不回错"已废弃 — 现策略：服务端返失败用于审计/调试，
        //   客户端对 reason==='not_broadcaster' 静默不弹红 toast（B' 优化，详见 SurvivalGameManager.HandleBroadcasterActionFailed）。
        if (!this._requireBroadcaster(ws, 'shop_purchase_prepare')) break;
        // { itemId } — 仅主播 HUD B 类 ≥1000 时客户端调用
        const pid    = ws._playerId || '';
        this.survivalEngine.handleShopPurchasePrepare(pid, itemIdP);
        break;
      }
      case 'shop_purchase': {
        // §36.12 shop 门槛
        const itemIdPr = (data && data.itemId) || '';
        if (!this._checkFeatureOrFail('shop', 'shop_purchase_failed', { itemId: itemIdPr }, ws)) break;
        if (!this._requireBroadcaster(ws, 'shop_purchase')) break;
        // { itemId, pendingId? }
        const pid       = ws._playerId || '';
        const pname     = (data && data.playerName)
                          || (this.survivalEngine.playerNames && this.survivalEngine.playerNames[pid])
                          || pid;
        const pendingId = (data && data.pendingId) || null;
        this.survivalEngine.handleShopPurchase(pid, pname, itemIdPr, pendingId, 'hud');
        break;
      }
      case 'shop_equip': {
        // §36.12 shop 门槛
        const itemIdE = (data && data.itemId) || '';
        // 🔴 audit-r37 GAP-C37-06：失败类型 shop_purchase_failed → shop_equip_failed
        //   旧版 shop_equip 路径走 shop_purchase_failed → 客户端 ShopUI 误按购买失败处理（无法定位装备槽 toast）
        //   r37 改用 shop_equip_failed 与 §19.2 line 2540 设计对齐
        if (!this._checkFeatureOrFail('shop', 'shop_equip_failed', { itemId: itemIdE, slot: (data && data.slot) || '' }, ws)) break;
        // { slot, itemId? } — itemId 缺省/空 = 卸下该槽位
        const pid    = ws._playerId || '';
        const slot   = (data && data.slot)   || '';
        this.survivalEngine.handleShopEquip(pid, slot, itemIdE);
        break;
      }
      case 'shop_inventory': {
        // §39 库存查询（C→S）—— 主动触发 shop_inventory_data 推送
        // §36.12 shop 门槛：未解锁时推送空 inventory（避免客户端空指针，与 shop_list 锁定期返空一致）
        const pidI = ws._playerId || '';
        this._refreshVeteranStatus();
        if (!isFeatureUnlocked(this, 'shop')) {
          // r17 GAP-R17-PM-04：locked 路径 broadcast → unicast（与 r15 GAP-E15-4 / r16 GAP-R16-PM-01 同形态延伸）
          //   shop_inventory_data 含 owned/equipped/contribBalance 私有数据，必须 unicast 避免他客户端 MyEquipped 被该玩家空数据覆盖
          const _lockedMsg = {
            type: 'shop_inventory_data',
            timestamp: Date.now(),
            data: {
              playerId: pidI,
              owned: [],
              equipped: { title: '', frame: '', entrance: '', barrage: '' },
              contribBalance: 0,
              lifetimeContrib: 0,
            },
          };
          // 🔴 audit-r41 GAP-PM41-01: 改用 _sendToClient helper，自动注入 B01 协议头三字段；失败时 fallback broadcast
          if (!(ws && this._sendToClient(ws, _lockedMsg))) {
            this.broadcast(_lockedMsg);
          }
          break;
        }
        this.survivalEngine.handleShopInventory(pidI);
        break;
      }

      // ==================== GM 测试指令 ====================
      // 注：这些指令主要为调试用，GM 模式下也会执行（符合"保留但审计"的要求）；
      //     真实抖音模式下同样需要主播身份，防止观众客户端伪造消息触发
      case 'pause_game':
        if (!this._requireBroadcaster(ws, 'pause_game')) break;
        // 🔴 audit-r31 GAP-A25-04 协议对称化修复：原仅 emit game_paused，无 game_resumed 对称消息
        //   修复：根据当前 _gmPaused 状态切换；resume 时调 _startTick 真恢复 + emit game_resumed
        if (this.survivalEngine.state === 'day' || this.survivalEngine.state === 'night') {
          if (!this._gmPaused) {
            // pause: 清 timers + emit game_paused
            this.survivalEngine._clearAllTimers();
            this._gmPaused = true;
            this.broadcast({ type: 'game_paused', data: { paused: true } });
            console.log(`[SurvivalRoom:${this.roomId}] GM: game paused`);
          } else {
            // resume: 重启 _startTick + emit game_resumed
            if (typeof this.survivalEngine._startTick === 'function') {
              this.survivalEngine._startTick();
            }
            this._gmPaused = false;
            this.broadcast({ type: 'game_resumed', data: { paused: false } });
            console.log(`[SurvivalRoom:${this.roomId}] GM: game resumed`);
          }
        }
        this._gmAudit(ws, 'pause_game', { state: this.survivalEngine.state, paused: this._gmPaused });
        break;

      case 'simulate_gift': {
        if (!this._requireBroadcaster(ws, 'simulate_gift')) break;
        // 模拟礼物（tier 1-6，默认 tier=2）
        const tierMap = { 1: 'fairy_wand', 2: 'ability_pill', 3: 'donut', 4: 'energy_battery', 5: 'love_explosion', 6: 'mystery_airdrop' };
        const tier    = (data && data.tier && tierMap[data.tier]) ? data.tier : 2;
        const giftId  = tierMap[tier];
        this.survivalEngine.handleGift('gm_test', 'GM测试', '', giftId, 0, `GM礼物T${tier}`);
        console.log(`[SurvivalRoom:${this.roomId}] GM: simulate_gift tier=${tier} (${giftId})`);
        this._gmAudit(ws, 'simulate_gift', { tier, giftId });
        break;
      }

      case 'simulate_freeze':
        if (!this._requireBroadcaster(ws, 'simulate_freeze')) break;
        // 模拟冻结特效（广播 special_effect freeze_all，不触发游戏结束）
        this.broadcast({ type: 'special_effect', timestamp: Date.now(), data: { effect: 'frozen_all', duration: 5 } });
        console.log(`[SurvivalRoom:${this.roomId}] GM: simulate_freeze`);
        this._gmAudit(ws, 'simulate_freeze', {});
        break;

      case 'simulate_monster':
        if (!this._requireBroadcaster(ws, 'simulate_monster')) break;
        // 模拟刷怪（立即追加1只普通怪物）
        if (this.survivalEngine.state === 'night') {
          this.survivalEngine._spawnWave({ type: 'normal', count: 1 }, this.survivalEngine.day, 99);
          console.log(`[SurvivalRoom:${this.roomId}] GM: simulate_monster`);
        }
        this._gmAudit(ws, 'simulate_monster', { state: this.survivalEngine.state });
        break;

      // ==================== §35 Tribe War C→S ====================
      // 🔴 audit-r41 GAP-PM41-01: 全部改用 _sendToClient helper，自动注入 B01 协议头三字段
      case 'tribe_war_room_list': {
        // 🔴 audit-r43 GAP-E43-04：策划案 §35 line 6062 "**均仅主播**" — 加守门与同表 attack/stop/retaliate 一致
        //   原代码无主播守门 → 任意客户端可查跨房列表泄露元信息（roomId/streamerName/state/fortressDay/...）
        //   修复：与 _requireBroadcaster 行为一致，非主播单播 broadcaster_action_failed { reason: 'not_broadcaster' }
        if (!this._requireBroadcaster(ws, 'tribe_war_room_list')) break;
        if (!this.tribeWarMgr) {
          this._sendToClient(ws, { type: 'tribe_war_room_list_result', data: { rooms: [] } });
          break;
        }
        const rooms = this.tribeWarMgr.getRoomList(this.roomId);
        this._sendToClient(ws, { type: 'tribe_war_room_list_result', data: { rooms } });
        break;
      }
      case 'tribe_war_attack': {
        if (!this._requireBroadcaster(ws, 'tribe_war_attack')) break;
        // §36.12 tribe_war 门槛（seasonDay ≥ 7）
        if (!this._checkFeatureOrFail('tribe_war', 'tribe_war_attack_failed', {}, ws)) break;
        if (!this.tribeWarMgr) break;
        const targetRoomId = data && data.targetRoomId;
        if (!targetRoomId) {
          // 🔴 audit-r45 GAP-D45-09：tribe_war_attack_failed 补 targetRoomId 字段（客户端 SurvivalDataTypes.cs:1217 已声明）
          //   原 5 处 emit 全 0 emit → 客户端无法定位失败属于哪个目标房间（用户连点失败时按钮无法对应回原始行）
          this._sendToClient(ws, { type: 'tribe_war_attack_failed', data: { reason: 'room_not_found', targetRoomId: targetRoomId || '' } });
          break;
        }
        const res = this.tribeWarMgr.startAttack(this.roomId, targetRoomId);
        if (!res.ok) {
          const failData = { reason: res.reason, targetRoomId };  // r45 GAP-D45-09 补 targetRoomId
          if (res.cooldownMs !== undefined) failData.cooldownMs = res.cooldownMs;
          this._sendToClient(ws, { type: 'tribe_war_attack_failed', data: failData });
        }
        this._gmAudit(ws, 'tribe_war_attack', { targetRoomId });
        break;
      }
      case 'tribe_war_stop': {
        if (!this._requireBroadcaster(ws, 'tribe_war_stop')) break;
        if (!this.tribeWarMgr) break;
        const sid = this.tribeWarMgr._attackerToSession.get(this.roomId);
        if (sid) {
          this.tribeWarMgr.stopAttack(sid, 'manual');
        }
        break;
      }
      // 🔴 audit-r46 GAP-M-02 codex 方案 D：远征怪命中事件 C→S
      //   客户端 MonsterController 命中目标动画时上报，服务端 TribeWarSession.handleExpeditionHit 校验 + 结算
      //   不需要 _requireBroadcaster 守门：观众客户端的 MonsterController 也会上报命中（每只怪一次，幂等）
      //   防伪造由 sessionId + monsterId + earliestHitAt 三层校验保证
      case 'tribe_war_expedition_hit': {
        if (!this.tribeWarMgr) break;
        const sid       = (data && data.sessionId)  || '';
        const mid       = (data && data.monsterId)  || '';
        const tgtType   = (data && data.targetType) || '';
        if (!sid || !mid) break;
        const session = this.tribeWarMgr._sessions.get(sid);
        if (!session) {
          console.log(`[SurvivalRoom:${this.roomId}] tribe_war_expedition_hit reject: session ${sid} not found`);
          break;
        }
        // 仅防守方房间转发（防止攻击方观众端伪造命中）
        if (session.defender !== this) {
          console.log(`[SurvivalRoom:${this.roomId}] tribe_war_expedition_hit reject: not defender of session ${sid}`);
          break;
        }
        session.handleExpeditionHit(mid, tgtType);
        break;
      }

      case 'tribe_war_retaliate': {
        if (!this._requireBroadcaster(ws, 'tribe_war_retaliate')) break;
        // §36.12 tribe_war 门槛（反击也走同一锁）
        if (!this._checkFeatureOrFail('tribe_war', 'tribe_war_attack_failed', {}, ws)) break;
        // 仅防守方(被攻击中)可反击;damageMultiplier 查 engine._buildings.has('beacon') 填 1.5（§37.2 烽火台反击联动）
        if (!this.tribeWarMgr) break;
        // 🔴 audit-r41 GAP-PM41-01: 全部改用 _sendToClient helper，自动注入 B01 协议头三字段
        const underAttackSid = this.tribeWarMgr._defenderToSession.get(this.roomId);
        if (!underAttackSid) {
          this._sendToClient(ws, { type: 'tribe_war_attack_failed', data: { reason: 'not_under_attack' } });
          break;
        }
        const atkSession = this.tribeWarMgr._sessions.get(underAttackSid);
        const targetRoomId = (data && data.targetRoomId) ||
                             (atkSession && atkSession.attacker && atkSession.attacker.roomId);
        if (!targetRoomId) {
          // 🔴 audit-r45 GAP-D45-09：补 targetRoomId（即使 fallback 失败也传空字符串保字段一致）
          this._sendToClient(ws, { type: 'tribe_war_attack_failed', data: { reason: 'room_not_found', targetRoomId: '' } });
          break;
        }
        // §37.2 beacon 反击联动：建有 beacon 时 damageMultiplier=1.5，否则 1.0
        const _dm = (this.survivalEngine && this.survivalEngine._buildings && this.survivalEngine._buildings.has('beacon')) ? 1.5 : 1.0;
        const res = this.tribeWarMgr.startAttack(this.roomId, targetRoomId, { damageMultiplier: _dm });
        if (!res.ok) {
          const failData = { reason: res.reason, targetRoomId };  // r45 GAP-D45-09 补 targetRoomId
          if (res.cooldownMs !== undefined) failData.cooldownMs = res.cooldownMs;
          this._sendToClient(ws, { type: 'tribe_war_attack_failed', data: failData });
        }
        this._gmAudit(ws, 'tribe_war_retaliate', { targetRoomId, damageMultiplier: _dm });
        break;
      }

      // ==================== §14 v1.27：§34.4 E9 难度切换协议已废止 ====================
      // 旧 case 'change_difficulty' / handleChangeDifficulty / change_difficulty_accepted/failed/difficulty_changed 全部移除。
      // 压力曲线由 _initBaselinePreset 基线 + _getEarlyDayMult(fortressDay) 渐进 + §30.6 _dynamicHpMult 动态难度控制。

      // ==================== §17.15 新手引导气泡 ====================
      // TODO: 抖音 SDK 观众进房事件接入后，由 SDK 路由到本 case（或直接在 SDK 回调里调 engine._maybeTriggerOnboarding()）。
      // 目前仅占位：本地模拟 / GM 测试 / 客户端回放可主动发 viewer_joined 触发一轮 B1–B3 节流。
      case 'viewer_joined':
        this.survivalEngine._maybeTriggerOnboarding();
        break;

      case 'disable_onboarding_for_session':
        // 🔴 audit-r43 GAP-C43-09：与 §24.3 同表其他主播指令一致走 _requireBroadcaster
        //   原"非主播静默忽略"违反 §24.3 line 2909 "实际单播 broadcaster_action_failed { reason: 'not_broadcaster' }"统一规则
        //   修复：用 _requireBroadcaster 守门，非主播会收到单播 broadcaster_action_failed
        if (!this._requireBroadcaster(ws, 'disable_onboarding')) break;
        this.survivalEngine._onboardingDisabled = true;
        console.log(`[SurvivalRoom:${this.roomId}] Onboarding disabled for session by broadcaster`);
        break;

      // ==================== §34.3 B2 主播跳过结算倒计时 ====================
      // C→S streamer_skip_settlement { playerId? }：仅主播（roomCreatorWs）可发
      //   服务端收到后立即 clearTimeout 结算 8s 倒计时 → 进入恢复期
      //   非主播静默忽略；state 非 'settlement' 时返回 false（_enterSettlement 已清句柄则无效）
      case 'streamer_skip_settlement': {
        if (!this._requireBroadcaster(ws, 'streamer_skip_settlement')) break;
        const pid = ws._playerId || '';
        const ok = this.survivalEngine.handleStreamerSkipSettlement(pid);
        if (!ok) {
          console.log(`[SurvivalRoom:${this.roomId}] streamer_skip_settlement no-op (state=${this.survivalEngine.state})`);
        }
        this._gmAudit(ws, 'streamer_skip_settlement', { ok });
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

    // §36.12 roomCreatorOpenId 兜底绑定：仅在"GM 模式 + 尚未绑定"时生效
    //   真实抖音模式下应由 /api/douyin/init → bindDouyinAnchor() 预注入，不再让首个评论者抢占主播身份
    //   （若真实抖音模式下 init 未能获取到 anchor_open_id，也仅视为兼容性兜底）
    if (!this.roomCreatorOpenId && secOpenId && (this.isGMMode || !this._modeInitialized)) {
      this.roomCreatorOpenId = secOpenId;
      this._refreshVeteranStatus();
      // 🔴 audit-r44 GAP-E44-01：handleDouyinComment fallback-bind 路径同步引擎字段
      this._syncRoomCreatorIdToEngine();
      console.log(`[SurvivalRoom:${this.roomId}] roomCreatorOpenId fallback-bound to ${secOpenId} (isGMMode=${this.isGMMode})`);
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

    const count = Math.max(1, Number(giftNum || 1) | 0);
    let unitValue = Number(giftValue || 1) || 1;
    // 抖音推送字段在不同接入层可能是"单价"或"总价"；价格兜底时优先保持可直接匹配的单价，
    // 若 direct 不命中但 total/count 命中，则归一化为单价并逐件结算。
    if (!findGiftById(secGiftId) && count > 1 && !findGiftByPrice(unitValue)) {
      const divided = unitValue / count;
      if (Number.isFinite(divided) && Number.isInteger(divided) && findGiftByPrice(divided)) {
        unitValue = divided;
      }
    }

    // 多数量礼物按单件循环结算，避免 totalValue 误匹配高档位或漏匹配。
    for (let i = 0; i < count; i++) {
      this.survivalEngine.handleGift(
        secOpenId,
        nickname,
        avatarUrl || '',
        secGiftId,
        unitValue,
        '' // giftName 由引擎内档位配置决定
      );
    }

    // 送礼首行为的身份绑定由 SurvivalGameEngine.handleGift 内部完成，避免重复 join 广播。
  }

  /**
   * 处理抖音点赞（策划案 §15.1：每次点赞+2积分；每50次点赞食物+10）— r14 GAP-A14-A7 修：原 §9
   */
  handleDouyinLike(secOpenId, nickname, avatarUrl, likeNum) {
    this.lastActiveAt = Date.now();

    const count = likeNum || 1;

    // 点赞贡献值（每次点赞+2积分）
    if (secOpenId) {
      this.survivalEngine.addContribution(secOpenId, 2 * count, 'like', nickname, avatarUrl);
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
   * - GM 模式：沿用 ws === this.roomCreatorWs（first-client 兜底，兼容 Play Mode 无真实 openId 场景）
   * - 真实抖音模式：优先比对 ws.openId / ws._playerId 是否匹配 this.roomCreatorOpenId
   *   （防止重连后 roomCreatorWs 对象引用过期导致权限丢失）
   * @param {WebSocket} ws
   * @returns {boolean}
   */
  _isRoomCreator(ws) {
    if (!ws) return false;

    // 真实抖音模式：openId 严格匹配
    if (!this.isGMMode && this.roomCreatorOpenId && !String(this.roomCreatorOpenId).startsWith('gm_creator_')) {
      const wsOpenId = ws._playerId || ws.openId || '';
      if (wsOpenId && wsOpenId === this.roomCreatorOpenId) return true;
      // 兜底：引用相等（首次绑定未来得及重连场景）
      return ws === this.roomCreatorWs;
    }

    // GM 模式 / 尚未确立模式：first-client 兜底
    return ws === this.roomCreatorWs;
  }

  /**
   * broadcaster 动作鉴权统一入口：
   *   若不是主播 → 广播 broadcaster_action_failed { action, reason:'not_broadcaster' } 并返回 false
   *   调用方直接 `if (!this._requireBroadcaster(ws, 'upgrade_gate')) break;`
   *
   * @param {WebSocket} ws
   * @param {string} action - 子动作名（如 'upgrade_gate' / 'end_game' / 'roulette_spin'）
   * @returns {boolean}
   */
  _requireBroadcaster(ws, action) {
    if (this._isRoomCreator(ws)) return true;
    // 🔴 audit-r40 GAP-PM40-02：改用 _sendToClient helper，自动注入 B01 协议头三字段（避免单播路径绕过 broadcast() 注入）
    this._sendToClient(ws, {
      type: 'broadcaster_action_failed',
      data: { action: action || 'unknown', reason: 'not_broadcaster' }
    });
    const wsOpenId = ws && (ws._playerId || ws.openId) || 'N/A';
    console.log(`[GM-AUDIT] REJECTED action=${action} roomId=${this.roomId} openId=${wsOpenId} isGM=${this.isGMMode} reason=not_broadcaster`);
    return false;
  }

  /**
   * GM / broadcaster 动作统一审计日志
   *   GM 模式下这些指令通常允许执行（调试用），但仍记录便于回溯
   *   真实抖音模式下的合法执行也会在此落地（故无条件记）
   * @param {WebSocket} ws
   * @param {string} action
   * @param {object} extra - 附加字段（不含密钥）
   */
  _gmAudit(ws, action, extra) {
    const wsOpenId = ws && (ws._playerId || ws.openId) || 'N/A';
    const wsId     = ws && ws._wsId ? ws._wsId : 'unknown';
    const payload  = extra && typeof extra === 'object' ? JSON.stringify(extra) : '';
    console.log(`[GM-AUDIT] action=${action} roomId=${this.roomId} ws=${wsId} openId=${wsOpenId} isGM=${this.isGMMode}${payload ? ' extra=' + payload : ''}`);
  }

  /**
   * 处理 broadcaster_action 消息
   * 仅允许房间创建者（主播）触发
   *
   * @param {WebSocket} ws   - 发送消息的WebSocket连接
   * @param {object} data    - 消息体 { action, duration?, cooldown? }
   */
  _handleBroadcasterAction(ws, data) {
    // 外层 case 'broadcaster_action' 已调用 _requireBroadcaster；此处双保险
    if (!this._isRoomCreator(ws)) {
      console.log(`[SurvivalRoom:${this.roomId}] broadcaster_action ignored: not room creator (second-layer check)`);
      return;
    }

    const action = data && data.action;
    this._gmAudit(ws, action || 'broadcaster_action', { raw: data });

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
    if (this._efficiencyBoostTimer) {
      clearTimeout(this._efficiencyBoostTimer);
      this._efficiencyBoostTimer = null;
    }
    this._efficiencyBoostExpireAt = Date.now() + durationMs;
    this.broadcasterEfficiencyMultiplier = multiplier;

    // 同步给引擎（若引擎支持此属性）
    if (this.survivalEngine) {
      this.survivalEngine.broadcasterEfficiencyMultiplier = multiplier;
    }

    // 定时恢复
    this._efficiencyBoostTimer = setTimeout(() => {
      if (Date.now() < this._efficiencyBoostExpireAt) return;
      this.broadcasterEfficiencyMultiplier = 1.0;
      if (this.survivalEngine) {
        this.survivalEngine.broadcasterEfficiencyMultiplier = 1.0;
      }
      this._efficiencyBoostTimer = null;
      this._efficiencyBoostExpireAt = 0;
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
    this._stopSimulation();
    if (this.survivalEngine.state === 'idle') {
      this.survivalEngine.startGame();
    }
    this._simRunning = true;

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
    this._simPlayerTimers = new Map();
    players.forEach((p, i) => {
      const joinTimer = setTimeout(() => {
        if (!this._simRunning) return;
        this.survivalEngine.handlePlayerJoined(p.id, p.name, '');
      }, i * 400);
      this._simPlayerTimers.set(`join_${p.id}`, joinTimer);
    });

    // ── 礼物演示序列已移除（改为日夜动态触发，见下方两个interval）──

    // ── 白天：每玩家独立随机工作循环（6~14秒，错开启动）──────────────
    const cmdCycle = [1, 2, 3, 4];
    const simPlayerTimers = this._simPlayerTimers;  // playerId → timerId

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
