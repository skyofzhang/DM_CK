/**
 * RoomPersistence - §36 房间持久化（MVP 极简版）
 *
 * 职责：
 * 1. 每房一 JSON 文件：./data/rooms/{roomId}.json
 * 2. 保存字段：fortressDay/maxFortressDay、§30 终身数据、§39 商店、§36.5.1 每日 cap
 * 3. 每 30s 自动保存；reset_game / end_game / destroy 前保存
 *
 * PM 决策（MVP 裁剪项）：
 * - schemaVersion=3；字段缺失自动回退默认值，不做向下兼容迁移脚本
 * - 失败只记 warn，不 throw（避免主循环阻塞）
 */

const fs = require('fs');
const path = require('path');

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
    console.log(`[RoomPersistence] dataDir=${this.dataDir}`);
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

    const snapshot = {
      schemaVersion: 3,
      roomId: room.roomId,
      savedAt: Date.now(),

      // §36 堡垒日 per-room
      fortressDay: engine.fortressDay || 1,
      maxFortressDay: engine.maxFortressDay || 1,

      // §30 矿工成长（跨局永续）
      _lifetimeContrib: engine._lifetimeContrib || {},
      _playerLevel: engine._playerLevel || {},
      _playerSkinId: engine._playerSkinId || {},

      // §39 商店（跨局永续）
      _playerShopInventory: engine._playerShopInventory || {},
      _playerShopEquipped: engine._playerShopEquipped || {},
      _contribBalance: engine._contribBalance || {},

      // §36.5.1 每日闯关上限
      _dailyFortressDayGained: engine._dailyFortressDayGained || 0,
      _dailyResetKey: engine._dailyResetKey || 0,
      _dailyCapBlocked: engine._dailyCapBlocked || false,

      // §36.12 老用户豁免（仅增字段；schemaVersion 保持 3）
      _veteranStreamers:     veteranSnapshot ? veteranSnapshot._veteranStreamers     : [],
      _maxSeasonDayAttended: veteranSnapshot ? veteranSnapshot._maxSeasonDayAttended : {},

      // §35 P2 攻防战战报（10 条滑动窗口，跨重启永续）
      _warReports: engine._warReports || [],
      // §38.3 探险符文 24h 滑动窗口（跨重启永续）
      _runeChargeLog: engine._runeChargeLog || [],

      // §37 建造系统：已建成建筑集合（跨重启永续；未完成骨架不持久化，重启视为取消，与 §37.4 规范对齐）
      _buildings: (engine._buildings && typeof engine._buildings[Symbol.iterator] === 'function')
        ? [...engine._buildings]
        : [],
    };

    try {
      const file = path.join(this.dataDir, `${room.roomId}.json`);
      fs.writeFileSync(file, JSON.stringify(snapshot), 'utf8');
    } catch (e) {
      console.warn(`[RoomPersistence] save fail ${room.roomId}: ${e.message}`);
    }
  }

  /**
   * 加载房间快照
   * @returns {object|null}  JSON 对象或 null（文件不存在/解析失败均返 null）
   */
  load(roomId) {
    if (!roomId) return null;
    try {
      const file = path.join(this.dataDir, `${roomId}.json`);
      if (!fs.existsSync(file)) return null;
      const buf = fs.readFileSync(file, 'utf8');
      const obj = JSON.parse(buf);
      return obj;
    } catch (e) {
      console.warn(`[RoomPersistence] load fail ${roomId}: ${e.message}`);
      return null;
    }
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
