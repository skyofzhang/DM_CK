/**
 * WeeklyRankingStore — 本周贡献榜持久化存储
 *
 * - 文件存储: data/weekly_ranking_{roomId}.json
 * - 每 ISO 周（周一 00:00 UTC+8）自动重置
 * - Node.js 单线程，无并发安全问题
 */

'use strict';

const fs   = require('fs');
const path = require('path');

const DATA_DIR = path.join(__dirname, '../../data');
if (!fs.existsSync(DATA_DIR)) fs.mkdirSync(DATA_DIR, { recursive: true });

// ==================== 时间工具 ====================

/**
 * 获取 UTC+8 下的 ISO 周字符串，格式 "YYYY-WNN"
 * ISO 周定义：每周以周一开始，包含第一个周四的那周为第1周
 */
function getCurrentWeek() {
  const nowUtc8 = new Date(Date.now() + 8 * 3600 * 1000);
  const year = nowUtc8.getUTCFullYear();
  // 找到本年第一个 ISO 周的起始（包含 1 月 4 日的那个周一）
  const jan4 = new Date(Date.UTC(year, 0, 4));
  const startOfWeek1Ms = jan4.getTime() - ((jan4.getUTCDay() + 6) % 7) * 86400000;
  const weekNum = Math.floor((nowUtc8.getTime() - startOfWeek1Ms) / (7 * 86400000)) + 1;
  return `${year}-W${String(weekNum).padStart(2, '0')}`;
}

/**
 * 返回下周一 00:00:00 UTC+8 的 Unix 毫秒时间戳
 * 客户端用此字段显示"X天后重置"倒计时
 */
function getNextResetMs() {
  const nowUtc8   = new Date(Date.now() + 8 * 3600 * 1000);
  const dow       = nowUtc8.getUTCDay(); // 0=Sun, 1=Mon, ..., 6=Sat
  const daysUntil = dow === 0 ? 1 : 8 - dow;
  const nextMon   = new Date(nowUtc8);
  nextMon.setUTCDate(nextMon.getUTCDate() + daysUntil);
  nextMon.setUTCHours(0, 0, 0, 0);
  // 转回 UTC 时间戳（减去 UTC+8 偏移）
  return nextMon.getTime() - 8 * 3600 * 1000;
}

// ==================== Store 类 ====================

class WeeklyRankingStore {
  /**
   * @param {string} roomId  房间ID（对应主播直播间ID）
   */
  constructor(roomId) {
    this.roomId   = roomId;
    const safeName = roomId.replace(/[^a-zA-Z0-9_-]/g, '_');
    this.filePath = path.join(DATA_DIR, `weekly_ranking_${safeName}.json`);
    this._data    = this._load();
    console.log(`[WeeklyRanking:${roomId}] 初始化完成，week=${this._data.week}，玩家数=${Object.keys(this._data.players).length}`);
  }

  // ——— 内部：加载文件 ———

  _load() {
    try {
      if (fs.existsSync(this.filePath)) {
        const raw = JSON.parse(fs.readFileSync(this.filePath, 'utf8'));
        // 校验结构完整性
        if (raw && raw.week && raw.players) return raw;
      }
    } catch (e) {
      console.warn(`[WeeklyRanking:${this.roomId}] 文件加载失败: ${e.message}`);
    }
    return { week: getCurrentWeek(), players: {} };
  }

  // ——— 内部：每次操作前检查周次是否变更 ———

  _checkAndReset() {
    const current = getCurrentWeek();
    if (this._data.week !== current) {
      console.log(`[WeeklyRanking:${this.roomId}] 周次变更 ${this._data.week} → ${current}，自动重置`);
      this._data = { week: current, players: {} };
      this._save();
    }
  }

  // ——— 内部：保存文件 ———

  _save() {
    try {
      fs.writeFileSync(this.filePath, JSON.stringify(this._data, null, 2), 'utf8');
    } catch (e) {
      console.error(`[WeeklyRanking:${this.roomId}] 文件保存失败: ${e.message}`);
    }
  }

  // ==================== 公开 API ====================

  /**
   * 每局游戏结束后调用，将本局排名累加到本周统计
   * @param {Array<{playerId, playerName, contribution}>} rankings  服务器本局排名
   */
  addGameResult(rankings) {
    if (!Array.isArray(rankings) || rankings.length === 0) return;
    this._checkAndReset();

    let updated = false;
    for (const r of rankings) {
      if (!r.playerId || !r.contribution) continue;
      if (!this._data.players[r.playerId]) {
        this._data.players[r.playerId] = {
          nickname:    r.playerName || r.playerId,
          weeklyScore: 0,
        };
      } else {
        // 昵称以最新为准
        if (r.playerName) this._data.players[r.playerId].nickname = r.playerName;
      }
      this._data.players[r.playerId].weeklyScore += r.contribution;
      updated = true;
    }

    if (updated) this._save();
  }

  /**
   * 获取本周 Top N 排名列表
   * @param {number} n
   * @returns {Array<{rank, playerId, nickname, weeklyScore}>}
   */
  getTop(n = 10) {
    this._checkAndReset();
    return Object.entries(this._data.players)
      .sort(([, a], [, b]) => b.weeklyScore - a.weeklyScore)
      .slice(0, n)
      .map(([playerId, p], i) => ({
        rank:        i + 1,
        playerId,
        nickname:    p.nickname,
        weeklyScore: Math.round(p.weeklyScore),
      }));
  }

  /**
   * 返回完整 payload（供 WebSocket 响应 / HTTP 接口使用）
   * @param {number} n
   * @returns {{ week, resetAt, rankings }}
   */
  getPayload(n = 10) {
    return {
      week:     this._data.week,
      resetAt:  getNextResetMs(),
      rankings: this.getTop(n),
    };
  }
}

module.exports = WeeklyRankingStore;
