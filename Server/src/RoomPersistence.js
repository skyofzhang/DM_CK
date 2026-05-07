/**
 * RoomPersistence - §36 房间持久化（schemaVersion 6）
 *
 * 职责：
 * 1. 每房一 JSON 文件：./data/rooms/{roomId}.json
 * 2. 保存字段：fortressDay/maxFortressDay、§30 终身数据、§39 商店、§36.5.1 每日 cap、§30.4 衰减元数据
 * 3. 每 30s 自动保存；reset_game / end_game / destroy 前保存
 *
 * schemaVersion 迁移链：
 *   v1 → v2：若无 _contribBalance，从 _lifetimeContrib 拷贝
 *   v2 → v3：
 *     - 旧 _playerSkinId 字段改名 _playerSkinTier（保留旧字段以兼容，读取时 fallback）
 *     - 补齐 totalCycles=0 / lastThemeId=null / currentSeasonId=1 / seasonSnapshot=null /
 *           _supporters=[] / streamerKingTitle=null / _dailyBuildVoteUsed={}
 *   v3 → v4（audit-r7 §30.4）：
 *     - 补齐 _lastDailyDecayDayKey=null（UTC+8 "YYYY-MM-DD"）
 *     - 补齐 _playerLastActiveTs={}（playerId → ms，活跃/不活跃分档输入）
 *   v4 → v5：
 *     - 补齐 _buildingInProgress=[]（重启恢复暂停中的建造）
 *   v5 → v6：
 *     - 补齐 _seasonFailure={ seasonId, failed }（赛季结算 survivingRooms 统计口径）
 *
 * PM 决策（MVP 裁剪项）：
 * - schemaVersion=6；字段缺失自动回退默认值
 * - 失败只记 warn，不 throw（避免主循环阻塞）
 */

const fs = require('fs');
const path = require('path');

// schemaVersion 6：在 v5 基础上新增 _seasonFailure（本赛季失败标记）
const CURRENT_SCHEMA_VERSION = 6;

class RoomPersistence {
  /**
   * @param {string} [dataDir] 存储目录（默认 ./data/rooms/，相对 Server/src/）
   */
  constructor(dataDir) {
    this.dataDir = dataDir || path.join(__dirname, '..', 'data', 'rooms');
    try {
      fs.mkdirSync(this.dataDir, { recursive: true });
    } catch (e) {
      console.warn(`[RoomPersistence] mkdir fail: ${e.message}`);
    }
    console.log(`[RoomPersistence] dataDir=${this.dataDir} schemaVersion=${CURRENT_SCHEMA_VERSION}`);
  }

  /** 保存房间快照到 JSON 文件 */
  save(room) {
    if (!room || !room.roomId) return;
    const engine = room.survivalEngine;
    if (!engine) return;

    // §36.12 v1.27：VeteranTracker 是全局单例；每个房间快照镜像一份局部视图，
    //   即使跨进程重启，任一房间快照恢复时都能还原"当前玩家曾是老用户"的判定
    //   （即使 tracker 丢失，per-room 快照仍保留；VeteranTracker.loadSnapshot 合并）
    let veteranSnapshot = null;
    if (engine.veteranTracker && typeof engine.veteranTracker.snapshot === 'function') {
      try { veteranSnapshot = engine.veteranTracker.snapshot(); } catch (e) { /* ignore */ }
    }

    // P0-A2 §36 SeasonManager 关联字段（若 engine 已注入 seasonMgr）
    const seasonMgr = engine.seasonMgr || null;
    const currentSeasonId = seasonMgr && typeof seasonMgr.seasonId === 'number' ? seasonMgr.seasonId : 1;
    const lastThemeId     = seasonMgr ? (seasonMgr.themeId || null) : null;
    // seasonSnapshot：保存赛季级别的镜像（seasonId/seasonDay/themeId + _nextThemeId），
    //   用于跨进程重启时 SeasonManager 单例恢复。不参与排行。
    const seasonSnapshot = seasonMgr ? {
      seasonId:     seasonMgr.seasonId,
      seasonDay:    seasonMgr.seasonDay,
      themeId:      seasonMgr.themeId,
      nextThemeId:  seasonMgr._nextThemeId || null,
    } : null;

    // P0-A4 StreamerRanking v1.26：总周期数与堡垒之王称号（供恢复时填回 store）
    //   engine._streamerRankingEntry 不存在 → 读 room.streamerRanking 的当前条目
    let totalCycles = 0;
    let streamerKingTitle = null;
    if (room.streamerRanking && room.streamerRanking._data && room.streamerRanking._data.streamers) {
      const entry = room.streamerRanking._data.streamers[room.roomId];
      if (entry) {
        totalCycles = Number(entry.totalCycles) || 0;
        streamerKingTitle = entry.streamerKingTitle || null;
      }
    }

    // P0-A2 _supporters：Map → Array.from(entries()) 形式
    const supportersArr = (engine._supporters && typeof engine._supporters.entries === 'function')
      ? Array.from(engine._supporters.entries())
      : [];

    // P0-A2 _dailyBuildVoteUsed：对象 seasonDay → bool（§37 建造每日限额 per seasonDay）
    //   若 engine 尚未维护该字段（旧版本只用全局 bool _buildVoteUsedToday），快照回退空对象
    const dailyBuildVoteUsed = (engine._dailyBuildVoteUsed && typeof engine._dailyBuildVoteUsed === 'object')
      ? engine._dailyBuildVoteUsed
      : {};

    // P0-A2 _playerSkinTier：v3 新字段（取代旧 _playerSkinId）
    //   engine 已迁移 → 读 engine._playerSkinTier；未迁移 → 从 _playerSkinId 现有值兜底
    const playerSkinTier = (engine._playerSkinTier && typeof engine._playerSkinTier === 'object')
      ? engine._playerSkinTier
      : (engine._playerSkinId || {});

    const snapshot = {
      schemaVersion: CURRENT_SCHEMA_VERSION,
      roomId: room.roomId,
      savedAt: Date.now(),

      // §36 堡垒日 per-room
      fortressDay: engine.fortressDay || 1,
      maxFortressDay: engine.maxFortressDay || 1,
      _seasonFailure: {
        seasonId: currentSeasonId,
        failed: !!engine._seasonFailed,
      },

      // §30 矿工成长（跨局永续）
      _lifetimeContrib: engine._lifetimeContrib || {},
      _playerLevel: engine._playerLevel || {},
      // v3 新字段：_playerSkinTier 取代 _playerSkinId；旧字段仍保留写入（load 时向下兼容）
      _playerSkinTier: playerSkinTier,
      _playerSkinId: engine._playerSkinId || {},

      // §39 商店（跨局永续）
      _playerShopInventory: engine._playerShopInventory || {},
      _playerShopInventoryMeta: engine._playerShopInventoryMeta || {},
      _playerShopEquipped: engine._playerShopEquipped || {},
      _contribBalance: engine._contribBalance || {},

      // §36.5.1 每日闯关上限
      _dailyFortressDayGained: engine._dailyFortressDayGained || 0,
      _dailyResetKey: engine._dailyResetKey || 0,
      _dailyCapBlocked: engine._dailyCapBlocked || false,

      // §36.12 老用户豁免
      _veteranStreamers:     veteranSnapshot ? veteranSnapshot._veteranStreamers     : [],
      _maxSeasonDayAttended: veteranSnapshot ? veteranSnapshot._maxSeasonDayAttended : {},

      // §35 P2 攻防战战报（10 条滑动窗口，跨重启永续）
      _warReports: engine._warReports || [],
      // §38.3 探险符文 24h 滑动窗口（跨重启永续）
      _runeChargeLog: engine._runeChargeLog || [],

      // §37 建造系统：已建成建筑集合（跨重启永续；未完成骨架不持久化，重启视为取消）
      _buildings: (engine._buildings && typeof engine._buildings[Symbol.iterator] === 'function')
        ? [...engine._buildings]
        : [],
      _buildingInProgress: (engine._buildingInProgress && typeof engine._buildingInProgress.entries === 'function')
        ? Array.from(engine._buildingInProgress.entries()).map(([buildId, info]) => ({
            buildId,
            startedAt: Number(info && info.startedAt) || 0,
            totalMs: Number(info && info.totalMs) || 0,
            completesAt: Number(info && info.completesAt) || 0,
            remainingMs: info && info.paused
              ? Math.max(0, Number(info.remainingMs) || 0)
              : Math.max(0, (Number(info && info.completesAt) || 0) - Date.now()),
            paused: !!(info && info.paused),
            pausedSeasonId: info && info.pausedSeasonId ? info.pausedSeasonId : null,
          }))
        : [],

      // ---- P0-A2 v3 新增 8 字段 ----
      totalCycles,                      // v1.26 累计周期数（镜像自 streamerRanking entry）
      lastThemeId,                      // 当前（结束时即"上一"）赛季主题
      currentSeasonId,                  // 当前赛季 id（SeasonManager 镜像）
      seasonSnapshot,                   // SeasonManager 快照（seasonId/seasonDay/themeId/_nextThemeId）
      _supporters: supportersArr,       // Map → [[pid, data], ...] 序列化
      streamerKingTitle,                // v1.26 "堡垒之王" 称号
      _dailyBuildVoteUsed: dailyBuildVoteUsed,  // 对象 seasonDay → bool
      // _playerSkinTier 已在上面写入（与 §30 同组）

      // ---- audit-r7 v4 新增 2 字段（§30.4 每日衰减元数据）----
      // _lastDailyDecayDayKey：UTC+8 "YYYY-MM-DD"，跨日识别用；null = 首次启动
      _lastDailyDecayDayKey: (typeof engine._lastDailyDecayDayKey === 'string' && engine._lastDailyDecayDayKey)
        ? engine._lastDailyDecayDayKey : null,
      // _playerLastActiveTs：playerId → ms（最近一次正向贡献 ts），_applyDailyTierDecay 分档输入
      _playerLastActiveTs: (engine._playerLastActiveTs && typeof engine._playerLastActiveTs === 'object')
        ? engine._playerLastActiveTs : {},
    };

    try {
      const file = path.join(this.dataDir, `${room.roomId}.json`);
      this._writeJsonAtomic(file, snapshot);
    } catch (e) {
      console.warn(`[RoomPersistence] save fail ${room.roomId}: ${e.message}`);
    }
  }

  /**
   * 加载房间快照，并执行 schemaVersion 迁移（旧版本 → CURRENT_SCHEMA_VERSION）
   * @returns {object|null}  JSON 对象或 null（文件不存在/解析失败均返 null）
   */
  load(roomId) {
    if (!roomId) return null;
    try {
      const file = path.join(this.dataDir, `${roomId}.json`);
      if (!fs.existsSync(file)) return null;
      const obj = this._readJsonWithBackup(file);
      return this._migrate(obj);
    } catch (e) {
      console.warn(`[RoomPersistence] load fail ${roomId}: ${e.message}`);
      return null;
    }
  }

  _writeJsonAtomic(file, obj) {
    const tmp = `${file}.${process.pid}.${Date.now()}.tmp`;
    const bak = `${file}.bak`;
    fs.writeFileSync(tmp, JSON.stringify(obj), 'utf8');
    if (fs.existsSync(file)) {
      try { fs.copyFileSync(file, bak); } catch (e) { /* backup best-effort */ }
    }
    fs.renameSync(tmp, file);
  }

  _readJsonWithBackup(file) {
    try {
      return JSON.parse(fs.readFileSync(file, 'utf8'));
    } catch (primaryErr) {
      const bak = `${file}.bak`;
      if (fs.existsSync(bak)) {
        try {
          const obj = JSON.parse(fs.readFileSync(bak, 'utf8'));
          console.warn(`[RoomPersistence] primary snapshot corrupt, loaded backup: ${path.basename(file)} (${primaryErr.message})`);
          return obj;
        } catch (backupErr) {
          console.warn(`[RoomPersistence] backup snapshot also failed: ${path.basename(file)} (${backupErr.message})`);
        }
      }
      throw primaryErr;
    }
  }

  /**
   * schemaVersion 迁移：v1 → v2 → v3 → v4 → v5 → v6
   *   - v1→v2：补 _contribBalance（从 _lifetimeContrib 拷贝）
   *   - v2→v3：
   *       - 旧 _playerSkinId 字段 → _playerSkinTier（不存在则空）
   *       - 补齐 totalCycles=0 / lastThemeId=null / currentSeasonId=1 / seasonSnapshot=null /
   *             _supporters=[] / streamerKingTitle=null / _dailyBuildVoteUsed={}
   *   - v3→v4（audit-r7 §30.4）：
   *       - 补齐 _lastDailyDecayDayKey=null / _playerLastActiveTs={}
   *       - 旧存档加载后首次 _tickDailyDecayIfDue 触发时将当前 dayKey 回填，不立即衰减
   *         （_tickDailyDecayIfDue 内部: null → 仅记录不衰减，避免服务器启动即衰减）
   *   - v4→v5：补 _buildingInProgress=[]
   *   - v5→v6：补 _seasonFailure={ seasonId, failed:false }
   */
  _migrate(obj) {
    if (!obj || typeof obj !== 'object') return obj;
    const fromVersion = Number(obj.schemaVersion) || 1;

    // v1 → v2：_contribBalance 默认 = _lifetimeContrib 拷贝 + 补默认 _playerShopInventory / _playerShopEquipped
    if (fromVersion < 2) {
      if (!obj._contribBalance || typeof obj._contribBalance !== 'object') {
        obj._contribBalance = Object.assign({}, obj._lifetimeContrib || {});
      }
      // audit-r6 P1-E4: §36.8.1 v1→v2 补 shop inventory/equipped 默认
      if (!obj._playerShopInventory || typeof obj._playerShopInventory !== 'object') {
        obj._playerShopInventory = {};
      }
      if (!obj._playerShopInventoryMeta || typeof obj._playerShopInventoryMeta !== 'object') {
        obj._playerShopInventoryMeta = {};
      }
      if (!obj._playerShopEquipped || typeof obj._playerShopEquipped !== 'object') {
        obj._playerShopEquipped = {};
      }
    }

    // v2 → v3：_playerSkinId → _playerSkinTier + 补齐 8 新字段
    if (fromVersion < 3) {
      if (!obj._playerSkinTier || typeof obj._playerSkinTier !== 'object') {
        obj._playerSkinTier = Object.assign({}, obj._playerSkinId || {});
      }
      if (typeof obj.totalCycles !== 'number')          obj.totalCycles = 0;
      if (typeof obj.lastThemeId === 'undefined')       obj.lastThemeId = null;
      if (typeof obj.currentSeasonId !== 'number')      obj.currentSeasonId = 1;
      if (typeof obj.seasonSnapshot === 'undefined')    obj.seasonSnapshot = null;
      if (!Array.isArray(obj._supporters))              obj._supporters = [];
      if (typeof obj.streamerKingTitle === 'undefined') obj.streamerKingTitle = null;
      if (!obj._dailyBuildVoteUsed || typeof obj._dailyBuildVoteUsed !== 'object') {
        obj._dailyBuildVoteUsed = {};
      }
    }

    // §36.5.1 daily cap fields may be absent in early v3 snapshots created before the cap patch.
    if (typeof obj._dailyFortressDayGained !== 'number') obj._dailyFortressDayGained = 0;
    if (typeof obj._dailyResetKey !== 'number') obj._dailyResetKey = 0;
    if (typeof obj._dailyCapBlocked !== 'boolean') obj._dailyCapBlocked = false;
    if (!obj._playerShopInventoryMeta || typeof obj._playerShopInventoryMeta !== 'object') {
      obj._playerShopInventoryMeta = {};
    }

    // v3 → v4（audit-r7 §30.4）：补齐衰减元数据
    if (fromVersion < 4) {
      if (typeof obj._lastDailyDecayDayKey === 'undefined')   obj._lastDailyDecayDayKey = null;
      if (!obj._playerLastActiveTs || typeof obj._playerLastActiveTs !== 'object') {
        obj._playerLastActiveTs = {};
      }
    }

    // codex review v5：补齐 _buildingInProgress 数组（旧快照可能无该字段）
    if (fromVersion < 5 || !Array.isArray(obj._buildingInProgress)) {
      obj._buildingInProgress = [];
    }

    // codex review v6 + audit-r45：补齐 _seasonFailure 对象（§36 season failure tracking）
    if (fromVersion < 6 || !obj._seasonFailure || typeof obj._seasonFailure !== 'object') {
      const seasonId = Number(obj.currentSeasonId) || (obj.seasonSnapshot && Number(obj.seasonSnapshot.seasonId)) || 1;
      obj._seasonFailure = { seasonId, failed: false };
    }

    // §14 v1.27：废止 difficulty 三档系统。RoomPersistence 历史从未持久化 _difficulty 字段，
    //   但若旧版本测试快照 / 外部工具写入了 obj.difficulty，加载后保留（不再使用，由 _applyPersistedSnapshot 静默忽略）。
    //   类似 audit-r45 的 snap._seasonFailure 兼容模式：仅读取不消费，不报错。

    // 读后统一标记为最新
    obj.schemaVersion = CURRENT_SCHEMA_VERSION;
    return obj;
  }

  /**
   * 批量保存（30s 定时器调用）
   * @param {Map<string, SurvivalRoom>} rooms
   */
  saveAll(rooms) {
    if (!rooms || rooms.size === 0) return 0;
    let saved = 0;
    for (const [, room] of rooms) {
      try {
        if (!room || room.status === 'destroyed') continue;
        this.save(room);
        saved++;
      } catch (e) {
        // ignore
      }
    }
    return saved;
  }
}

module.exports = RoomPersistence;
module.exports.CURRENT_SCHEMA_VERSION = CURRENT_SCHEMA_VERSION;
