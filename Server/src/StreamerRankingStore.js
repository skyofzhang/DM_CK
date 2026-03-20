/**
 * StreamerRankingStore — 主播排行榜持久化存储
 *
 * - 文件存储: data/streamer_ranking.json（全局共享，不按房间隔离）
 * - 记录每位主播的最佳成绩：难度 × 坚持天数
 * - 排名得分: difficulty_weight × maxDays
 *   - normal=1, hard=2, hell=3
 * - 不自动重置（历史最佳记录）
 */

'use strict';

const fs   = require('fs');
const path = require('path');

const DATA_DIR = path.join(__dirname, '../../data');
if (!fs.existsSync(DATA_DIR)) fs.mkdirSync(DATA_DIR, { recursive: true });

const DIFFICULTY_WEIGHT = { normal: 1, hard: 2, hell: 3 };

class StreamerRankingStore {
  constructor() {
    this.filePath = path.join(DATA_DIR, 'streamer_ranking.json');
    this._data    = this._load();
    console.log(`[StreamerRanking] 初始化完成，主播数=${Object.keys(this._data.streamers).length}`);
  }

  _load() {
    try {
      if (fs.existsSync(this.filePath)) {
        const raw = JSON.parse(fs.readFileSync(this.filePath, 'utf8'));
        if (raw && raw.streamers) return raw;
      }
    } catch (e) {
      console.warn(`[StreamerRanking] 文件加载失败: ${e.message}`);
    }
    return { streamers: {} };
  }

  _save() {
    try {
      fs.writeFileSync(this.filePath, JSON.stringify(this._data, null, 2), 'utf8');
    } catch (e) {
      console.error(`[StreamerRanking] 文件保存失败: ${e.message}`);
    }
  }

  /**
   * 每局结束后由房间调用，更新主播的历史最佳记录
   * @param {string} streamerId   房间ID（=主播直播间ID）
   * @param {string} streamerName 主播名（可选，fallback用streamerId）
   * @param {string} difficulty   "normal" | "hard" | "hell"
   * @param {int}    daysSurvived 本局坚持天数
   * @param {string} result       "win" | "lose"
   */
  addGameResult(streamerId, streamerName, difficulty, daysSurvived, result) {
    if (!streamerId) return;

    const weight = DIFFICULTY_WEIGHT[difficulty] || 1;
    const score  = weight * daysSurvived;

    let entry = this._data.streamers[streamerId];
    if (!entry) {
      entry = {
        streamerName: streamerName || streamerId,
        maxDifficulty: difficulty || 'normal',
        maxDays:    daysSurvived,
        totalWins:  0,
        totalGames: 0,
        score:      score,
      };
      this._data.streamers[streamerId] = entry;
    }

    // 更新昵称
    if (streamerName) entry.streamerName = streamerName;

    // 总场次
    entry.totalGames++;
    if (result === 'win') entry.totalWins++;

    // 更新最佳记录（取最高分）
    if (score > entry.score) {
      entry.maxDifficulty = difficulty;
      entry.maxDays       = daysSurvived;
      entry.score         = score;
    }

    this._save();
    console.log(`[StreamerRanking] ${streamerName||streamerId}: diff=${difficulty} days=${daysSurvived} result=${result} bestScore=${entry.score}`);
  }

  /**
   * 获取 Top N 主播排名
   * @param {number} n
   * @returns {Array<{rank, streamerId, streamerName, maxDifficulty, maxDays, totalWins, totalGames, score}>}
   */
  getTop(n = 10) {
    return Object.entries(this._data.streamers)
      .sort(([, a], [, b]) => b.score - a.score)
      .slice(0, n)
      .map(([streamerId, s], i) => ({
        rank:          i + 1,
        streamerId,
        streamerName:  s.streamerName,
        maxDifficulty: s.maxDifficulty,
        maxDays:       s.maxDays,
        totalWins:     s.totalWins,
        totalGames:    s.totalGames,
        score:         s.score,
      }));
  }

  /**
   * 返回完整 payload
   * @param {number} n
   * @returns {{ rankings }}
   */
  getPayload(n = 10) {
    return {
      rankings: this.getTop(n),
    };
  }
}

module.exports = StreamerRankingStore;
