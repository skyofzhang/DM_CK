/**
 * StreamerRankingStore — 主播排行榜持久化存储（v1.26 schema，§36 永续模式）
 *
 * - 文件存储: data/streamer_ranking.json（全局共享，不按房间隔离）
 * - 字段（v1.26）：
 *     maxFortressDay       历史最高堡垒日（用于排名）
 *     totalCycles          累计经历的周期次数（每次 fortress_day_changed +1）
 *     streamerKingTitle    "堡垒之王" 称号（仅 Top1 持有；其余 null）
 * - 排名按 maxFortressDay 降序（同值按 totalCycles 降序作 tiebreaker）
 * - 不自动重置（历史最佳记录）
 *
 * v1.25 → v1.26 迁移：
 *   - 旧字段 maxDays / totalGames / totalWins 仍读取但不再写入
 *   - maxFortressDay = max(maxFortressDay || 0, maxDays || 0)
 *   - totalCycles    = max(totalCycles    || 0, totalGames || 0)
 *   - streamerKingTitle 首次 load 默认 null；save 时会重算
 */

'use strict';

const fs   = require('fs');
const path = require('path');

const DATA_DIR = path.join(__dirname, '../../data');
if (!fs.existsSync(DATA_DIR)) fs.mkdirSync(DATA_DIR, { recursive: true });

// v1.25 旧兼容：对仍调用 addGameResult(difficulty) 的路径保留权重映射（用于 legacy score，
// 不影响 v1.26 的 maxFortressDay 排名；仅保留旧兼容字段以便客户端老版本显示）
const DIFFICULTY_WEIGHT = { normal: 1, hard: 2, hell: 3 };

class StreamerRankingStore {
  constructor() {
    this.filePath = path.join(DATA_DIR, 'streamer_ranking.json');
    this._data    = this._load();
    console.log(`[StreamerRanking] 初始化完成 (v1.26)，主播数=${Object.keys(this._data.streamers).length}`);
  }

  _load() {
    try {
      if (fs.existsSync(this.filePath)) {
        const raw = JSON.parse(fs.readFileSync(this.filePath, 'utf8'));
        if (raw && raw.streamers) {
          // v1.25 → v1.26 迁移：补齐 maxFortressDay / totalCycles / streamerKingTitle
          for (const sid of Object.keys(raw.streamers)) {
            const s = raw.streamers[sid];
            if (!s) continue;
            // 已有 v1.26 字段则保留；否则从旧字段兜底
            if (typeof s.maxFortressDay !== 'number') {
              s.maxFortressDay = Math.max(0, Number(s.maxDays) || 0);
            }
            if (typeof s.totalCycles !== 'number') {
              s.totalCycles = Math.max(0, Number(s.totalGames) || 0);
            }
            if (typeof s.streamerKingTitle !== 'string' && s.streamerKingTitle !== null) {
              s.streamerKingTitle = null;
            }
          }
          return raw;
        }
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
   * 重算所有主播的 streamerKingTitle 字段（仅 Top1 持有"堡垒之王"，其余 null）。
   * 在每次 updateFortressDay 之后调用；排名按 maxFortressDay 降序（同值按 totalCycles 降序）。
   */
  _recomputeKingTitle() {
    const entries = Object.entries(this._data.streamers);
    if (entries.length === 0) return;
    entries.sort(([, a], [, b]) => {
      const da = Number(b.maxFortressDay) || 0;
      const dc = Number(a.maxFortressDay) || 0;
      if (da !== dc) return da - dc;
      const ca = Number(b.totalCycles) || 0;
      const cc = Number(a.totalCycles) || 0;
      return ca - cc;
    });
    const [topId] = entries[0];
    for (const [sid, s] of entries) {
      s.streamerKingTitle = (sid === topId && (Number(s.maxFortressDay) || 0) > 0) ? '堡垒之王' : null;
    }
  }

  /**
   * §36 v1.26 主入口：fortress_day_changed 推送时机调用
   *   - 首次看到该 streamerId → 创建条目
   *   - maxFortressDay = max(old, fortressDay)
   *   - totalCycles += 1（每次 fortress_day_changed 后累加，与 wonCycle 无关）
   *   - 重算全局 streamerKingTitle（仅 Top1 持有）
   *
   * @param {string} streamerId    主播房间 id（= roomId）
   * @param {string} streamerName  主播展示名（可选，fallback=streamerId）
   * @param {number} fortressDay   当前堡垒日（success 或 fail 后的新值）
   * @param {boolean} wonCycle     是否本周期获胜（v1.25 老语义；v1.26 保留但不计入 totalCycles 增量）
   */
  updateFortressDay(streamerId, streamerName, fortressDay, wonCycle) {
    if (!streamerId) return;
    const fd = Math.max(0, Number(fortressDay) || 0);

    let entry = this._data.streamers[streamerId];
    if (!entry) {
      entry = {
        streamerName:      streamerName || streamerId,
        maxFortressDay:    fd,
        totalCycles:       1,
        streamerKingTitle: null,
        // 仅为兼容旧客户端显示字段（不再作排名依据）
        maxDifficulty:     'normal',
        maxDays:           fd,
        totalWins:         wonCycle ? 1 : 0,
        totalGames:        1,
        score:             fd,
      };
      this._data.streamers[streamerId] = entry;
    } else {
      if (streamerName) entry.streamerName = streamerName;
      entry.totalCycles = (Number(entry.totalCycles) || 0) + 1;
      if (fd > (Number(entry.maxFortressDay) || 0)) {
        entry.maxFortressDay = fd;
      }
      // 兼容字段同步
      entry.totalGames = (Number(entry.totalGames) || 0) + 1;
      if (wonCycle) entry.totalWins = (Number(entry.totalWins) || 0) + 1;
      if (fd > (Number(entry.maxDays) || 0)) {
        entry.maxDays = fd;
        entry.score   = fd;   // 兼容 score 字段（v1.25 排名用；v1.26 已不用）
      }
    }

    // 若仅改了自己 → 仍需重算 king title（别人 Top1 位置可能被刷下去）
    this._recomputeKingTitle();
    this._save();

    console.log(`[StreamerRanking v1.26] ${entry.streamerName}: fortressDay=${fd} maxFD=${entry.maxFortressDay} totalCycles=${entry.totalCycles} king=${entry.streamerKingTitle || '-'}`);
  }

  /**
   * v1.26 语义别名：仅在 maxFortressDay 变大时才更新（updateFortressDay 已内置此逻辑，
   * 但 totalCycles 仍会递增；updateIfBetter 供调用方按语义理解"fortress_day_changed 后最佳值写入"用）
   */
  updateIfBetter(streamerId, streamerName, fortressDay, wonCycle) {
    this.updateFortressDay(streamerId, streamerName, fortressDay, !!wonCycle);
  }

  /**
   * v1.25 旧入口（保留兼容，survival_game_ended 等旧路径仍在用）
   * - 按难度权重 × daysSurvived 计 score
   * - 同时通过 updateFortressDay 把 daysSurvived 写入 maxFortressDay 轨道（daysSurvived 即一局存活天数，
   *   虽然语义与 fortressDay 不完全等价，但作为兜底可确保旧路径也能更新新 schema）
   */
  addGameResult(streamerId, streamerName, difficulty, daysSurvived, result) {
    if (!streamerId) return;
    const weight = DIFFICULTY_WEIGHT[difficulty] || 1;
    const score  = weight * daysSurvived;

    let entry = this._data.streamers[streamerId];
    if (!entry) {
      entry = {
        streamerName:      streamerName || streamerId,
        maxFortressDay:    Number(daysSurvived) || 0,
        totalCycles:       1,
        streamerKingTitle: null,
        // 兼容
        maxDifficulty:     difficulty || 'normal',
        maxDays:           Number(daysSurvived) || 0,
        totalWins:         result === 'win' ? 1 : 0,
        totalGames:        1,
        score:             score,
      };
      this._data.streamers[streamerId] = entry;
    } else {
      if (streamerName) entry.streamerName = streamerName;
      entry.totalGames = (Number(entry.totalGames) || 0) + 1;
      entry.totalCycles = (Number(entry.totalCycles) || 0) + 1;
      if (result === 'win') entry.totalWins = (Number(entry.totalWins) || 0) + 1;
      if (score > (Number(entry.score) || 0)) {
        entry.maxDifficulty = difficulty;
        entry.maxDays       = Number(daysSurvived) || 0;
        entry.score         = score;
      }
      const fd = Number(daysSurvived) || 0;
      if (fd > (Number(entry.maxFortressDay) || 0)) {
        entry.maxFortressDay = fd;
      }
    }

    this._recomputeKingTitle();
    this._save();
    console.log(`[StreamerRanking v1.25-compat] ${streamerName||streamerId}: diff=${difficulty} days=${daysSurvived} result=${result} maxFD=${entry.maxFortressDay} king=${entry.streamerKingTitle || '-'}`);
  }

  /**
   * 获取 Top N 主播排名（v1.26：按 maxFortressDay 降序；同值 totalCycles 降序）
   * 返回字段同时包含新旧字段（客户端按版本消费）。
   */
  getTop(n = 10) {
    return Object.entries(this._data.streamers)
      .sort(([, a], [, b]) => {
        const diff = (Number(b.maxFortressDay) || 0) - (Number(a.maxFortressDay) || 0);
        if (diff !== 0) return diff;
        return (Number(b.totalCycles) || 0) - (Number(a.totalCycles) || 0);
      })
      .slice(0, n)
      .map(([streamerId, s], i) => ({
        rank:              i + 1,
        streamerId,
        streamerName:      s.streamerName,
        // v1.26 主字段
        maxFortressDay:    Number(s.maxFortressDay) || 0,
        totalCycles:       Number(s.totalCycles)    || 0,
        streamerKingTitle: s.streamerKingTitle || null,
        // v1.25 兼容字段
        maxDifficulty:     s.maxDifficulty || 'normal',
        maxDays:           Number(s.maxDays)    || 0,
        totalWins:         Number(s.totalWins)  || 0,
        totalGames:        Number(s.totalGames) || 0,
        score:             Number(s.score)      || 0,
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
