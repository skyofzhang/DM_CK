/**
 * 极地生存法则 — 生存游戏引擎
 *
 * 游戏循环：
 *   idle → [start_game] → Day1 → Night1 → Day2 → Night2 → ... → Day7通过 → 胜利
 *   三种失败：food=0 / furnaceTemp≤-100 / gateHp=0
 *
 * 每5秒推送一次 resource_update
 * 夜晚按 waveConfig 定时生成 monster_wave
 * 评论1-5 → 转发 work_command 给客户端
 * 评论6  → 攻击怪物（夜晚有效）
 * 礼物 → 按策划案 §5.1 七种礼物 ID 分发效果，推送 survival_gift
 */

const { findGiftById, findGiftByPrice, getGift, getTierNumber } = require('./GiftConfig');

// ==================== 怪物波次配置 ====================
// 每天包含：
//   normal: { hp, atk, spd, count, waves }  — 普通怪
//   elite:  { hp, atk, count } | null        — 精英怪（Day1 为 null）
//   boss:   { hp, atk }                      — Boss（每天都有，Day1以后出现）
//   legacy: { monsterId, baseCount, maxCount, beginning, refreshTime } — 波次调度
// baseCount: 每批生成数量（千军万马感）；maxCount: 单夜上限；beginning: 首波延迟(s)；refreshTime: 批次间隔(s)
const WAVE_CONFIGS = [
  { day: 1,  normal: { hp: 150,  atk: 3,  spd: 2.0, count: 12, waves: 3 }, elite: null,                      boss: { hp: 1000,  atk: 10  }, monsterId: 'X_guai01', baseCount: 8,  maxCount: 25,  beginning: 3, refreshTime: 10 },
  { day: 2,  normal: { hp: 200,  atk: 4,  spd: 2.2, count: 20, waves: 4 }, elite: { hp: 500,   atk: 8,  count: 1  }, boss: { hp: 2000,  atk: 15  }, monsterId: 'X_guai01', baseCount: 10, maxCount: 35,  beginning: 3, refreshTime: 9  },
  { day: 3,  normal: { hp: 250,  atk: 5,  spd: 2.5, count: 30, waves: 5 }, elite: { hp: 600,   atk: 10, count: 2  }, boss: { hp: 3000,  atk: 18  }, monsterId: 'X_guai01', baseCount: 12, maxCount: 45,  beginning: 3, refreshTime: 8  },
  { day: 4,  normal: { hp: 325,  atk: 6,  spd: 2.7, count: 18, waves: 4 }, elite: { hp: 800,   atk: 13, count: 2  }, boss: { hp: 4500,  atk: 22  }, monsterId: 'X_guai01', baseCount: 14, maxCount: 55,  beginning: 2, refreshTime: 8  },
  { day: 5,  normal: { hp: 400,  atk: 7,  spd: 3.0, count: 22, waves: 4 }, elite: { hp: 1000,  atk: 16, count: 3  }, boss: { hp: 6500,  atk: 27  }, monsterId: 'X_guai01', baseCount: 16, maxCount: 65,  beginning: 2, refreshTime: 7  },
  { day: 6,  normal: { hp: 500,  atk: 9,  spd: 3.2, count: 28, waves: 5 }, elite: { hp: 1300,  atk: 20, count: 4  }, boss: { hp: 9000,  atk: 33  }, monsterId: 'X_guai01', baseCount: 18, maxCount: 75,  beginning: 2, refreshTime: 7  },
  { day: 7,  normal: { hp: 600,  atk: 12, spd: 3.5, count: 35, waves: 5 }, elite: { hp: 1750,  atk: 26, count: 5  }, boss: { hp: 14000, atk: 42  }, monsterId: 'X_guai01', baseCount: 20, maxCount: 80,  beginning: 2, refreshTime: 6  },
  { day: 10, normal: { hp: 750,  atk: 15, spd: 3.8, count: 40, waves: 6 }, elite: { hp: 2200,  atk: 32, count: 6  }, boss: { hp: 20000, atk: 55  }, monsterId: 'X_guai01', baseCount: 25, maxCount: 100, beginning: 2, refreshTime: 6  },
  { day: 15, normal: { hp: 950,  atk: 20, spd: 4.0, count: 50, waves: 7 }, elite: { hp: 3000,  atk: 40, count: 8  }, boss: { hp: 35000, atk: 70  }, monsterId: 'X_guai01', baseCount: 30, maxCount: 120, beginning: 2, refreshTime: 5  },
  { day: 20, normal: { hp: 1200, atk: 25, spd: 4.2, count: 60, waves: 8 }, elite: { hp: 4000,  atk: 50, count: 10 }, boss: { hp: 55000, atk: 90  }, monsterId: 'X_guai01', baseCount: 40, maxCount: 150, beginning: 2, refreshTime: 5  },
  { day: 30, normal: { hp: 1600, atk: 32, spd: 4.5, count: 70, waves: 9 }, elite: { hp: 6000,  atk: 65, count: 12 }, boss: { hp: 90000, atk: 120 }, monsterId: 'X_guai01', baseCount: 50, maxCount: 200, beginning: 2, refreshTime: 4  },
  { day: 40, normal: { hp: 2200, atk: 40, spd: 4.8, count: 80, waves:10 }, elite: { hp: 9000,  atk: 85, count: 15 }, boss: { hp: 150000,atk: 160 }, monsterId: 'X_guai01', baseCount: 60, maxCount: 250, beginning: 2, refreshTime: 4  },
  { day: 50, normal: { hp: 3000, atk: 50, spd: 5.0, count: 90, waves:10 }, elite: { hp: 12000, atk: 110,count: 18 }, boss: { hp: 250000,atk: 200 }, monsterId: 'X_guai01', baseCount: 80, maxCount: 300, beginning: 2, refreshTime: 3  },
];

function getWaveConfig(day) {
  let cfg = WAVE_CONFIGS[0];
  for (const c of WAVE_CONFIGS) {
    if (day >= c.day) cfg = c;
  }
  return cfg;
}

// 城门等级配置（策划案 §3.3：lv1→2=100矿, lv2→3=250矿, lv3→4=500矿）
const GATE_UPGRADE_COSTS   = [0, 100, 250, 500]; // index: lv1→idx0=100, lv2→idx1=250, lv3→idx2=500
const GATE_MAX_HP_BY_LEVEL = [1000, 1500, 2200, 3000]; // 城门HP：1级/2级/3级/4级

// 随机事件名称映射（策划案 §8）
const EVENT_NAMES = {
  E01_snowstorm:    '暴风雪',
  E02_harvest:      '丰收季节',
  E03_monster_wave: '怪物来袭',
  E04_warm_spring:  '暖流涌现',
  E05_ore_vein:     '矿脉发现',
};

// ==================== SurvivalGameEngine ====================

class SurvivalGameEngine {
  /**
   * @param {object} config        - 游戏配置（来自 config/default.json）
   * @param {function} broadcast   - 广播给所有客户端
   */
  constructor(config, broadcast) {
    this.config   = config;
    this.broadcast = broadcast;

    // 阶段时长（秒）
    // M-NUMERIC：每天120秒（2分钟），更贴合直播节奏
    this.dayDuration   = config.survivalDayDuration   || 120;  // 原180s，改为120s
    this.nightDuration = config.survivalNightDuration  || 120;  // 原90s，改为120s，防止夜晚太短
    this.totalDays     = config.survivalTotalDays      || 7;

    // 资源初始值
    this.food         = config.initFood        ?? 500;
    this.coal         = config.initCoal        ?? 300;
    this.ore          = config.initOre         ?? 150;
    this.furnaceTemp  = config.initFurnaceTemp  ?? 20;
    this.gateHp       = config.initGateHp       ?? 1000;
    this.gateMaxHp    = config.initGateHp       ?? 1000;

    // 城门等级
    this.gateLevel    = 1;

    // 资源衰减配置（每秒）
    // M-NUMERIC 数值深化：放缓消耗速率，支持长期在线玩法
    // 目标：10人在线不消费情况下，食物约可维持3天（7天周期 × 180+120s/天 ≈ 2100s，
    //       初始食物500，3天 ≈ 900s，foodDecay = 500/900 ≈ 0.055，取整为 0.05）
    this.foodDecayDay    = config.foodDecayDay    ?? 0.05;  // 白天食物消耗/s（原0.1，减半）
    this.foodDecayNight  = config.foodDecayNight  ?? 0.05;  // 夜晚食物消耗/s（原0.1，减半）
    this.tempDecayDay    = config.tempDecayDay    ?? 0.15;  // 白天炉温/s（原0.3，减半）
    this.tempDecayNight  = config.tempDecayNight  ?? 0.4;   // 夜晚炉温衰减更快/s（原0.8，减半）
    this.minTemp         = -100;
    this.maxTemp         = 100;

    // 怪物攻击城门伤害
    this.monsterGateDamage = config.monsterGateDamage ?? 5;

    // 游戏状态
    this.state          = 'idle';   // idle | day | night | settlement
    this.currentDay     = 0;
    this.remainingTime  = 0;

    // 贡献追踪 { playerId: float }
    this.contributions  = {};
    this.playerNames    = {};  // playerId → playerName（结算时填入排行榜显示名）
    this.totalPlayers   = 0;

    // 夜晚活跃怪物 Map<monsterId, { id, type, maxHp, currentHp, atk }>
    // type: 'normal' | 'elite' | 'boss'
    this._activeMonsters = new Map();
    this._monsterIdCounter = 0;

    // 定时器
    this._tickTimer    = null;   // 200ms tick（资源衰减+时间递减）
    this._resourceSyncTimer = null; // 5s 全量同步
    this._waveTimers   = [];     // 夜晚波次定时器列表
    this._tickCounter  = 0;

    // 礼物效果状态（策划案 §5.1 / §4.4）
    this._playerEfficiencyBonus = {};  // playerId → 累计加成值（仙女棒 +0.05/个，上限 1.0 = +100%）
    this._playerCloneBoost      = {};  // playerId → bool（超能喷射 ×2 持续60s）
    this._globalEfficiencyBoost = 1.0; // 全局效率倍率（能量电池 1.3 持续60s）
    this._globalEfficiencyBoostTimer = null;
    this._frozenPlayers         = new Set(); // magic_mirror 冻结中的 playerId

    // 666弹幕效果（策划案 §4.3）
    this.efficiency666Bonus = 1.0;   // 效率加成（1.15 = +15%）
    this.efficiency666Timer = 0;     // 剩余秒数

    // 难度倍率（由 _applyDifficulty 设置）
    this._difficulty       = 'normal';
    this._monsterHpMult    = 1.0;
    this._monsterCntMult   = 1.0;

    // 点赞统计
    this.totalLikes = 0;

    // 每天防守成功进入积分池的基础分（由难度决定，_applyDifficulty 覆盖）
    this._poolNightBase = 500;

    // 积分池（策划案 §D：防守/礼物/点赞/击杀 → 积分入池；结算后按比例分配给 Top10）
    this.scorePool       = 0;    // 本局累计积分池
    this._carryoverPool  = 0;    // 上局结余（流入本局）

    // 随机事件状态（策划案 §8）
    this._nextEventTimer     = 90 + Math.random() * 30; // 首次触发时间（90-120s）
    this.tempDecayMultiplier = 1.0;  // 炉温衰减倍率（暴风雪事件改为 2.0）
    this.foodBonus           = 1.0;  // 食物采集加成（丰收事件改为 1.5）
    this.oreBonus            = 1.0;  // 矿石采集加成（矿脉事件改为 2.0）
    this._randomEventTimers  = [];   // setTimeout 句柄列表（用于 reset 清理）
  }

  // ==================== 对外接口 ====================

  /** 客户端请求开始（含难度参数）*/
  startGame(difficulty = 'normal') {
    if (this.state !== 'idle') return;
    this._applyDifficulty(difficulty);
    this._enterDay(1);
  }

  /**
   * 根据难度调整怪物强度、资源消耗和总天数。
   * easy   → 适合 1-50  人观众：怪物弱, 资源多, 30天胜利（≈3小时）
   * normal → 适合 50-200人观众：标准值,           50天胜利（≈5小时）
   * hard   → 适合 200+ 人观众：怪物强, 资源紧, 70天胜利（≈7小时）
   * 资源衰减已×0.14（相对原7天难度），保证相同难度体验
   */
  _applyDifficulty(difficulty) {
    this._difficulty = difficulty;

    // decayMult 基于"7天难度 = 现在总天数的1/10"进行等比缩放：
    //   easy 原0.65 × (7/30) ≈ 0.15；normal 原1.00 × (7/50) ≈ 0.14；hard 原1.35 × (7/70) = 0.135
    // poolNightBase：每成功防守一夜进入积分池的基础分（× 当前天数）
    //   easy  → 300/天：怪少资源多，奖池积累慢，风险低
    //   normal → 500/天：标准
    //   hard  → 800/天：怪多资源紧，高风险高回报
    const presets = {
      easy:   { hpMult: 0.6, cntMult: 0.6, decayMult: 0.15, initMult: 1.5, totalDays: 30, poolNightBase: 300 },
      normal: { hpMult: 1.0, cntMult: 1.0, decayMult: 0.14, initMult: 1.0, totalDays: 50, poolNightBase: 500 },
      hard:   { hpMult: 1.5, cntMult: 1.5, decayMult: 0.14, initMult: 0.8, totalDays: 70, poolNightBase: 800 },
    };
    const p = presets[difficulty] || presets.normal;

    // 存储怪物倍率（_enterNight 使用）
    this._monsterHpMult    = p.hpMult;
    this._monsterCntMult   = p.cntMult;
    // 积分池夜晚基础奖励
    this._poolNightBase    = p.poolNightBase;

    // 总关卡天数
    this.totalDays = p.totalDays;

    // 按倍率调整资源衰减（在构造函数默认值基础上乘以倍率）
    this.foodDecayDay   = (this.config.foodDecayDay   ?? 0.05) * p.decayMult;
    this.foodDecayNight = (this.config.foodDecayNight ?? 0.05) * p.decayMult;
    this.tempDecayDay   = (this.config.tempDecayDay   ?? 0.15) * p.decayMult;
    this.tempDecayNight = (this.config.tempDecayNight ?? 0.40) * p.decayMult;

    // 按倍率调整初始资源
    const base = {
      food:      this.config.initFood       ?? 500,
      coal:      this.config.initCoal       ?? 300,
      ore:       this.config.initOre        ?? 150,
      gateHp:    this.config.initGateHp     ?? 1000,
    };
    this.food      = Math.round(base.food    * p.initMult);
    this.coal      = Math.round(base.coal    * p.initMult);
    this.ore       = Math.round(base.ore     * p.initMult);
    this.gateHp    = Math.round(base.gateHp  * p.initMult);
    this.gateMaxHp = this.gateHp;

    console.log(`[Engine] 难度: ${difficulty} | 怪物HP×${p.hpMult} 数量×${p.cntMult} 衰减×${p.decayMult} 资源×${p.initMult} 总天数:${p.totalDays} 夜晚积分基数:${p.poolNightBase}/天`);
  }

  /** 客户端请求重置 */
  reset() {
    this._clearAllTimers();
    this.state = 'idle';
    this.currentDay = 0;
    this.remainingTime = 0;
    this.food        = this.config.initFood        ?? 500;
    this.coal        = this.config.initCoal        ?? 300;
    this.ore         = this.config.initOre         ?? 150;
    this.furnaceTemp = this.config.initFurnaceTemp  ?? 20;
    this.gateHp      = this.config.initGateHp       ?? 1000;
    this.gateMaxHp   = this.config.initGateHp       ?? 1000;
    this.gateLevel   = 1;
    this.contributions = {};
    this.playerNames   = {};   // 重置玩家名缓存
    this.totalPlayers  = 0;

    // 积分池：结余流入下局（carryover 机制）
    this.scorePool      = this._carryoverPool || 0;
    this._carryoverPool = 0;

    // 重置难度（等待下次 startGame 时重新设置）
    this._difficulty     = 'normal';
    this._monsterHpMult  = 1.0;
    this._monsterCntMult = 1.0;
    this._activeMonsters.clear();
    this._monsterIdCounter = 0;

    // 重置礼物效果状态
    this._playerEfficiencyBonus = {};
    this._playerCloneBoost      = {};
    this._globalEfficiencyBoost = 1.0;
    if (this._globalEfficiencyBoostTimer) {
      clearTimeout(this._globalEfficiencyBoostTimer);
      this._globalEfficiencyBoostTimer = null;
    }
    this._frozenPlayers.clear();

    // 重置666 + 点赞 + 随机事件状态
    this.efficiency666Bonus  = 1.0;
    this.efficiency666Timer  = 0;
    this.totalLikes          = 0;
    this._nextEventTimer     = 90 + Math.random() * 30;
    this.tempDecayMultiplier = 1.0;
    this.foodBonus           = 1.0;
    this.oreBonus            = 1.0;
    for (const t of this._randomEventTimers) clearTimeout(t);
    this._randomEventTimers  = [];

    this.broadcast({ type: 'survival_game_state', timestamp: Date.now(), data: this.getFullState() });
  }

  /** WS客户端连接时发送当前状态 */
  getFullState() {
    return {
      state:        this.state,
      day:          this.currentDay,
      remainingTime: Math.round(this.remainingTime),
      food:         Math.round(this.food),
      coal:         Math.round(this.coal),
      ore:          Math.round(this.ore),
      furnaceTemp:  Math.round(this.furnaceTemp * 10) / 10,
      gateHp:       Math.round(this.gateHp),
      gateMaxHp:    this.gateMaxHp,
      gateLevel:    this.gateLevel,
      scorePool:    Math.round(this.scorePool),
    };
  }

  /** 停止游戏引擎（房间被销毁时调用） */
  pause() {
    this._clearAllTimers();
  }

  resume() {
    // 连接恢复后重新推送当前状态
    this.broadcast({ type: 'survival_game_state', timestamp: Date.now(), data: this.getFullState() });
  }

  // ==================== 抖音事件入口 ====================

  /**
   * 处理评论（弹幕指令）
   * content "1"-"5" → work_command
   * content "6"     → attack（夜晚有效）
   */
  handleComment(playerId, playerName, avatarUrl, content) {
    if (this.state !== 'day' && this.state !== 'night') return;
    const content_trim = (content || '').trim();

    const cmd = parseInt(content_trim);
    if (cmd >= 1 && cmd <= 4) {
      // 工作指令 1-4（策划案 v2.0 已移除指令5修城门）
      const cmdNames  = ['', 'food', 'coal', 'ore', 'heat'];
      const cmdLabels = ['', '采集食物', '挖掘煤炭', '开采矿石', '添柴升温'];

      const data = {
        playerId,
        playerName,
        avatarUrl: avatarUrl || '',
        commandId: cmd,
        commandName: cmdNames[cmd],
      };

      // 工作指令 → 转发给客户端，客户端驱动奶牛动画
      this.broadcast({
        type: 'work_command',
        timestamp: Date.now(),
        data
      });

      // 工作效果：小幅增加对应资源（服务器计算，不依赖客户端确认）
      this._applyWorkEffect(cmd, playerId);
      console.log(`[SurvivalEngine] work_command: ${playerName} → ${cmdLabels[cmd]}`);
    } else if (cmd === 6) {
      // 攻击指令（夜晚有效）
      if (this.state === 'night') {
        this._handleAttack(playerId, playerName);
      }
    } else if (content_trim === '666') {
      // 666弹幕：全员效率+15%，持续30秒（策划案 §4.3）
      this.efficiency666Bonus = 1.15;
      this.efficiency666Timer = 30;
      this._trackContribution(playerId, 2);
      this.broadcast({ type: 'special_effect', timestamp: Date.now(), data: { effect: 'glow_all', duration: 3 } });
      console.log(`[SurvivalEngine] 666 bonus activated by ${playerName}, efficiency +15% for 30s`);
    }
  }

  /**
   * 处理礼物（策划案 §5.1 七种礼物体系）
   * giftId   — 抖音平台 gift_id（douyin_id，当前均为 TBD，对齐后可精确匹配）
   * giftValue — 礼物价格，单位：分（1分=0.01元=0.1抖币）
   */
  handleGift(playerId, playerName, avatarUrl, giftId, giftValue, giftName) {
    // 优先按 douyin_id 查找，TBD 期间回退到按 price_fen 查找
    let gift = findGiftById(giftId);
    if (!gift) gift = getGift(giftId);          // 模拟/测试时用内部ID直接匹配
    if (!gift) gift = findGiftByPrice(giftValue);

    // 完全未知的礼物：轻量兜底（小额食物加成）
    if (!gift) {
      const fallbackScore = Math.max(1, Math.floor(giftValue / 50));
      this.food = Math.min(2000, this.food + fallbackScore);
      this._trackContribution(playerId, giftValue);
      this.scorePool += giftValue;  // 积分池：未知礼物按价值入池
      this._broadcastResourceUpdate();
      console.log(`[SurvivalEngine] unknown gift: ${giftName || giftId} (val=${giftValue}), fallback food bonus`);
      return;
    }

    // 贡献值 = 礼物得分（策划案 §9）
    this._trackContribution(playerId, gift.score);
    // 积分池：礼物得分入池
    this.scorePool += gift.score;

    // 每种礼物的具体效果（策划案 §5.1）
    const effects = {};
    let needsResourceSync = false;

    switch (gift.id) {

      case 'fairy_wand': {
        // 发送者效率永久+5%（叠加，上限+100%）
        const prev = this._playerEfficiencyBonus[playerId] || 0;
        this._playerEfficiencyBonus[playerId] = Math.min(1.0, prev + 0.05);
        effects.efficiencyBonus = this._playerEfficiencyBonus[playerId];
        break;
      }

      case 'ability_pill': {
        // 全局食物+50，召唤守卫（客户端动画；夜晚时对怪物造成额外伤害）
        this.food = Math.min(2000, this.food + 50);
        effects.addFood = 50;
        effects.guardSummoned = true;
        effects.guardDuration = 30;
        // 夜晚：守卫立即对当前所有怪物造成一次 AOE 伤害
        if (this.state === 'night' && this._activeMonsters.size > 0) {
          const guardDmg = 30;
          const killed = [];
          for (const [mid, m] of this._activeMonsters) {
            m.currentHp -= guardDmg;
            if (m.currentHp <= 0) killed.push(mid);
          }
          for (const mid of killed) {
            const m = this._activeMonsters.get(mid);
            this._activeMonsters.delete(mid);
            this._broadcast({ type: 'monster_died', data: { monsterId: mid, monsterType: m.type, killerId: 'guard' } });
          }
          effects.guardKilled = killed.length;
        }
        needsResourceSync = true;
        break;
      }

      case 'magic_mirror': {
        // 随机冻结1名已加入玩家30秒（无法工作）
        const playerIds = Object.keys(this.contributions).filter(id => id !== playerId);
        if (playerIds.length > 0) {
          const targetId = playerIds[Math.floor(Math.random() * playerIds.length)];
          this._frozenPlayers.add(targetId);
          effects.frozenPlayerId = targetId;
          effects.freezeDuration = 30;
          setTimeout(() => this._frozenPlayers.delete(targetId), 30000);
        }
        break;
      }

      case 'donut': {
        // 城门修复+200HP，全局食物+100
        this.gateHp = Math.min(this.gateMaxHp, this.gateHp + 200);
        this.food   = Math.min(2000, this.food + 100);
        effects.addGateHp = 200;
        effects.addFood   = 100;
        needsResourceSync = true;
        break;
      }

      case 'energy_battery': {
        // 炉温+30℃，全局效率+30%（持续180秒，不叠加，刷新计时）
        // M-NUMERIC：持续时间 60s → 180s，让中档礼物更有价值感
        this.furnaceTemp = Math.min(this.maxTemp, this.furnaceTemp + 30);
        this._globalEfficiencyBoost = 1.3;
        if (this._globalEfficiencyBoostTimer) clearTimeout(this._globalEfficiencyBoostTimer);
        this._globalEfficiencyBoostTimer = setTimeout(() => {
          this._globalEfficiencyBoost = 1.0;
          this._globalEfficiencyBoostTimer = null;
        }, 180000);
        effects.addHeat             = 30;
        effects.globalEfficiencyBoost = 1.3;
        effects.boostDuration       = 180;
        needsResourceSync = true;
        break;
      }

      case 'mystery_airdrop': {
        // 超级补给：食物+500、煤炭+200、矿石+100、城门+300HP；触发 GIFT_PAUSE 3000ms
        this.food    = Math.min(2000, this.food    + 500);
        this.coal    = Math.min(1500, this.coal    + 200);
        this.ore     = Math.min(800, this.ore     + 100);
        this.gateHp  = Math.min(this.gateMaxHp, this.gateHp + 300);
        effects.addFood   = 500;
        effects.addCoal   = 200;
        effects.addOre    = 100;
        effects.addGateHp = 300;
        effects.giftPause = 3000;
        this.broadcast({ type: 'gift_pause', timestamp: Date.now(), data: { duration: 3000 } });
        needsResourceSync = true;
        break;
      }

      case 'super_jet': {
        // 发送者召唤分身，效率×2（持续60秒，同一玩家不叠加：刷新计时）
        this._playerCloneBoost[playerId] = true;
        setTimeout(() => {
          this._playerCloneBoost[playerId] = false;
        }, 60000);
        effects.clonePlayerId = playerId;
        effects.cloneDuration = 60;
        break;
      }
    }

    const giftData = {
      playerId,
      playerName,
      avatarUrl:  avatarUrl || '',
      giftId:     gift.id,
      giftName:   gift.name_cn,
      giftTier:   getTierNumber(gift.tier),   // 数字 1-5，保持与客户端兼容
      giftTierStr: gift.tier,                 // 'T1'~'T5'，供扩展使用
      giftValue,
      score:      gift.score,
      contribution: gift.score,               // Unity SurvivalGiftData.contribution 字段别名
      // 礼物直接资源加成（扁平化，供 Unity 直接反序列化）
      addFood:    effects.addFood   || 0,
      addCoal:    effects.addCoal   || 0,
      addOre:     effects.addOre    || 0,
      addHeat:    effects.addHeat   || 0,
      addGateHp:  effects.addGateHp || 0,
      effects,
    };

    this.broadcast({ type: 'survival_gift', timestamp: Date.now(), data: giftData });

    if (needsResourceSync) this._broadcastResourceUpdate();
    console.log(`[SurvivalEngine] gift: ${playerName} → ${gift.name_cn} (${gift.tier}, score=${gift.score})`);
  }

  /**
   * 处理新玩家加入（评论首次出现）
   */
  handlePlayerJoined(playerId, playerName, avatarUrl) {
    if (!this.contributions[playerId]) {
      this.contributions[playerId] = 0;
      this.playerNames[playerId] = playerName || playerId;  // 记录玩家名，结算时填入排行榜
      this.totalPlayers++;
    }

    const data = {
      playerId,
      playerName,
      avatarUrl: avatarUrl || '',
      totalPlayers: this.totalPlayers,
    };
    this.broadcast({ type: 'survival_player_joined', timestamp: Date.now(), data });
  }

  // ==================== 内部：阶段切换 ====================

  _enterDay(day) {
    this._clearNightWaves();
    this._activeMonsters.clear();
    this.state         = 'day';
    this.currentDay    = day;
    this.remainingTime = this.dayDuration;

    // 每天开始时重置随机事件计时器（90-120s后触发首次事件）
    this._nextEventTimer = 90 + Math.random() * 30;

    console.log(`[SurvivalEngine] ===== Day ${day} Start =====`);

    this.broadcast({
      type: 'phase_changed',
      timestamp: Date.now(),
      data: {
        phase: 'day',
        day,
        phaseDuration: this.dayDuration,
      }
    });

    // 同步完整状态
    this.broadcast({ type: 'survival_game_state', timestamp: Date.now(), data: this.getFullState() });

    this._startTick();
  }

  _enterNight(day) {
    this.state         = 'night';
    this.currentDay    = day;
    this.remainingTime = this.nightDuration;

    console.log(`[SurvivalEngine] ===== Night ${day} Start =====`);

    // 初始化当天怪物追踪
    this._initActiveMonsters(day);

    this.broadcast({
      type: 'phase_changed',
      timestamp: Date.now(),
      data: {
        phase: 'night',
        day,
        phaseDuration: this.nightDuration,
      }
    });

    this.broadcast({ type: 'survival_game_state', timestamp: Date.now(), data: this.getFullState() });

    // 播报 Boss 出现
    const cfg = getWaveConfig(day);
    if (cfg.boss) {
      this.broadcast({
        type: 'boss_appeared',
        timestamp: Date.now(),
        data: {
          day,
          bossHp:  cfg.boss.hp,
          bossAtk: cfg.boss.atk,
        }
      });
    }

    // 3秒延迟后再启动夜晚怪物波次（给客户端过渡动画播放时间）
    const nightStartDelay = setTimeout(() => {
      if (this.state === 'night') {
        this._scheduleNightWaves(day);
      }
    }, 3000);
    this._waveTimers.push(nightStartDelay);
  }

  _enterSettlement(result, reason) {
    if (this.state === 'settlement') return;
    this._clearAllTimers();
    this.state = 'settlement';

    const rankings = this._buildRankings();

    // ===== 积分池分配（策划案 §D）=====
    // 胜利瓜分60%，失败瓜分30%；剩余流入主播下局（上限=本局积分池50%）
    const payoutRate  = result === 'win' ? 0.6 : 0.3;
    const payoutTotal = Math.floor(this.scorePool * payoutRate);
    const rawCarryover = this.scorePool - payoutTotal;
    this._carryoverPool = Math.min(rawCarryover, Math.floor(this.scorePool * 0.5));

    // 排名权重（Top10：20/16/12/10/8/7/6/5/4/3，共91份）
    const weights     = [20, 16, 12, 10, 8, 7, 6, 5, 4, 3];
    const totalWeight = weights.slice(0, rankings.length).reduce((s, w) => s + w, 0) || 1;
    rankings.forEach((r, i) => {
      r.payout = Math.floor(payoutTotal * (weights[i] || 1) / totalWeight);
    });

    console.log(`[SurvivalEngine] ScorePool=${Math.round(this.scorePool)}, payout=${payoutTotal}(${(payoutRate*100)|0}%), carryover=${this._carryoverPool}`);

    const data = {
      result,          // 'win' | 'lose'
      reason,          // 'survived' | 'food_depleted' | 'temp_freeze' | 'gate_breached'
      dayssurvived: this.currentDay,
      totalScore: Object.values(this.contributions).reduce((a, b) => a + b, 0),
      rankings,
      // 积分池字段
      scorePool:   Math.round(this.scorePool),
      distributed: payoutTotal,
      carryover:   this._carryoverPool,
      payoutRate,
    };

    this.broadcast({ type: 'survival_game_ended', timestamp: Date.now(), data });
    console.log(`[SurvivalEngine] Game ended: ${result} (${reason}), day=${this.currentDay}`);

    // 10秒后自动重置并重启游戏（直播间持续运行）
    setTimeout(() => {
      console.log('[SurvivalEngine] Auto-restarting game after settlement...');
      this.reset();
      this.startGame(this._difficulty || 'normal');
    }, 10000);
  }

  // ==================== 内部：Tick ====================

  _startTick() {
    this._clearTick();
    this._tickCounter = 0;
    this._tickTimer = setInterval(() => this._tick(), 200);

    // 5秒全量同步（独立定时器，避免被tick覆盖）
    this._resourceSyncTimer = setInterval(() => {
      if (this.state === 'day' || this.state === 'night')
        this._broadcastResourceUpdate();
    }, 5000);
  }

  _tick() {
    if (this.state !== 'day' && this.state !== 'night') return;
    this._tickCounter++;

    // 每5tick = 1秒
    if (this._tickCounter % 5 === 0) {
      this.remainingTime = Math.max(0, this.remainingTime - 1);

      // 666效率加成计时器倒计时
      if (this.efficiency666Timer > 0) {
        this.efficiency666Timer = Math.max(0, this.efficiency666Timer - 1);
        if (this.efficiency666Timer <= 0) {
          this.efficiency666Bonus = 1.0;
          console.log('[SurvivalEngine] 666 bonus expired');
        }
      }

      // 随机事件触发检查（仅白天）
      this._checkRandomEvents();

      // 资源衰减
      this._decayResources();

      // 检查失败条件
      if (this._checkDefeat()) return;

      // 阶段结束切换
      if (this.remainingTime <= 0) {
        if (this.state === 'day') {
          this._enterNight(this.currentDay);
        } else {
          this._endNight();
        }
      }
    }
  }

  /**
   * 夜晚结算（时间耗尽 或 Boss击杀 时调用）
   */
  _endNight() {
    // 成功防守一夜 → 积分入池（基础分由难度决定 × 当前天数）
    //   easy 300/天 | normal 500/天 | hard 800/天
    const nightBonus = (this._poolNightBase || 500) * this.currentDay;
    this.scorePool += nightBonus;
    console.log(`[SurvivalEngine] Night ${this.currentDay} cleared [${this._difficulty}], scorePool +${nightBonus} = ${Math.round(this.scorePool)}`);

    const nextDay = this.currentDay + 1;
    if (nextDay > this.totalDays) {
      this._enterSettlement('win', 'survived');
    } else {
      this._enterDay(nextDay);
    }
  }

  _decayResources() {
    const isNight = this.state === 'night';

    // 食物：每秒消耗
    this.food = Math.max(0, this.food - (isNight ? this.foodDecayNight : this.foodDecayDay));

    // 炉温：每秒下降（夜晚更快；暴风雪事件可加速衰减）
    const baseTempDecay = isNight ? this.tempDecayNight : this.tempDecayDay;
    const tempDecay = baseTempDecay * (this.tempDecayMultiplier || 1.0);
    this.furnaceTemp = Math.max(this.minTemp, this.furnaceTemp - tempDecay);

    // 取整便于显示
    this.food        = Math.round(this.food * 10) / 10;
    this.furnaceTemp = Math.round(this.furnaceTemp * 10) / 10;
  }

  _checkDefeat() {
    if (this.food <= 0) {
      this._enterSettlement('lose', 'food_depleted');
      return true;
    }
    if (this.furnaceTemp <= this.minTemp) {
      this._enterSettlement('lose', 'temp_freeze');
      return true;
    }
    if (this.gateHp <= 0) {
      this._enterSettlement('lose', 'gate_breached');
      return true;
    }
    return false;
  }

  // ==================== 内部：工作效果 ====================

  _applyWorkEffect(commandId, playerId) {
    if (this.state !== 'day' && this.state !== 'night') return;

    // 魔法镜冻结中：无法工作（策划案 §5.1 magic_mirror）
    if (this._frozenPlayers.has(playerId)) return;

    // 效率倍率计算（策划案 §4.4）
    // playerBonus   = 1 + 仙女棒累计加成（最高 +100%）
    // cloneBoost    = 2.0（超能喷射激活时）否则 1.0
    // globalBoost   = 1.3（能量电池激活时）否则 1.0
    // 综合上限 ×3.0
    // 综合效率倍率计算（策划案 §4.4）
    const playerBonus      = 1.0 + (this._playerEfficiencyBonus[playerId] || 0); // 仙女棒加成
    const cloneBoost       = this._playerCloneBoost[playerId] ? 2.0 : 1.0;       // 超能喷射分身
    const globalBoost      = this._globalEfficiencyBoost;                         // 能量电池全局加成
    const broadcasterBoost = this.broadcasterEfficiencyMultiplier || 1.0;         // 主播紧急加速
    const eff666Boost      = this.efficiency666Bonus || 1.0;                      // 666弹幕加成
    const totalMult        = Math.min(3.0, playerBonus * cloneBoost * globalBoost * broadcasterBoost * eff666Boost);

    // 工作效果：每次评论小幅增加资源（基础值 × 综合倍率）
    switch (commandId) {
      case 1: // 采食物（丰收事件额外加成）
        this.food = Math.min(2000, this.food + Math.round(5 * totalMult * (this.foodBonus || 1.0)));
        break;
      case 2: // 挖煤
        this.coal = Math.min(1500, this.coal + Math.round(3 * totalMult));
        break;
      case 3: // 采矿（矿脉事件额外加成）
        this.ore = Math.min(800, this.ore + Math.round(2 * totalMult * (this.oreBonus || 1.0)));
        break;
      case 4: // 添柴升温：每次+3℃（策划案要求），消耗1煤炭
        this.furnaceTemp = Math.min(this.maxTemp, this.furnaceTemp + Math.round(3 * totalMult));
        this.coal = Math.max(0, this.coal - 1);
        break;
      // case 5 (修城门) 已从策划案 v2.0 移除
    }

    this._trackContribution(playerId, 1); // 工作贡献值=1
  }

  // ==================== 内部：怪物追踪 ====================

  /**
   * 夜晚开始时，根据当天波次配置初始化活跃怪物 Map
   */
  _initActiveMonsters(day) {
    this._activeMonsters.clear();
    const cfg = getWaveConfig(day);

    // 难度倍率（未设置时默认 1.0）
    const hpMult  = this._monsterHpMult  || 1.0;
    const cntMult = this._monsterCntMult || 1.0;

    // 普通怪
    if (cfg.normal) {
      const count = Math.max(1, Math.round(cfg.normal.count * cntMult));
      const hp    = Math.max(1, Math.round(cfg.normal.hp    * hpMult));
      for (let i = 0; i < count; i++) {
        const id = `n_${day}_${++this._monsterIdCounter}`;
        this._activeMonsters.set(id, {
          id,
          type: 'normal',
          maxHp: hp,
          currentHp: hp,
          atk: cfg.normal.atk,
        });
      }
    }

    // 精英怪
    if (cfg.elite) {
      const count = Math.max(0, Math.round(cfg.elite.count * cntMult));
      const hp    = Math.max(1, Math.round(cfg.elite.hp    * hpMult));
      for (let i = 0; i < count; i++) {
        const id = `e_${day}_${++this._monsterIdCounter}`;
        this._activeMonsters.set(id, {
          id,
          type: 'elite',
          maxHp: hp,
          currentHp: hp,
          atk: cfg.elite.atk,
        });
      }
    }

    // Boss（每晚一只，HP受难度影响）
    if (cfg.boss) {
      const hp = Math.max(1, Math.round(cfg.boss.hp * hpMult));
      const id = `b_${day}_${++this._monsterIdCounter}`;
      this._activeMonsters.set(id, {
        id,
        type: 'boss',
        maxHp: hp,
        currentHp: hp,
        atk: cfg.boss.atk,
      });
    }

    console.log(`[SurvivalEngine] Night ${day} monsters initialized: ${this._activeMonsters.size} total (diff:${this._difficulty||'normal'} hpMult:${hpMult} cntMult:${cntMult})`);
  }

  // ==================== 内部：攻击处理（commandId=6）====================

  /**
   * 处理玩家攻击指令（夜晚限定）
   */
  _handleAttack(secOpenId, nickname) {
    const monsters = [...this._activeMonsters.values()];
    if (monsters.length === 0) return;

    // 目标优先级：普通怪 > 精英怪 > Boss
    const target = monsters.find(m => m.type === 'normal') ||
                   monsters.find(m => m.type === 'elite')  ||
                   monsters.find(m => m.type === 'boss');

    if (!target) return;

    const damage = 10; // 基础攻击伤害
    target.currentHp -= damage;

    // 贡献奖励（攻击命中）
    const hitScore = 5;
    this._addScore(secOpenId, nickname, hitScore);

    // 广播攻击事件
    this._broadcast({
      type: 'combat_attack',
      data: {
        attackerId:       secOpenId,
        attackerName:     nickname,
        targetId:         target.id,
        targetType:       target.type,
        damage,
        targetHpRemaining: Math.max(0, target.currentHp),
      }
    });

    // 检查怪物是否死亡
    if (target.currentHp <= 0) {
      this._activeMonsters.delete(target.id);

      const killScore = target.type === 'normal' ? 20 :
                        target.type === 'elite'  ? 50 : 200;
      this._addScore(secOpenId, nickname, killScore);
      // 积分池：击杀得分入池
      this.scorePool += killScore;

      this._broadcast({
        type: 'monster_died',
        data: {
          monsterId:   target.id,
          monsterType: target.type,
          killerId:    secOpenId,
        }
      });

      console.log(`[SurvivalEngine] Monster killed: ${target.id} (${target.type}) by ${nickname}`);

      // Boss 击杀 → 提前结束当夜
      if (target.type === 'boss') {
        this._broadcast({
          type: 'night_cleared',
          data: { reason: 'boss_killed' }
        });
        this._endNight();
      }
    }
  }

  // ==================== 内部：城门升级 ====================

  /**
   * 升级城门
   * 等级 1→2→3→4，矿石消耗：100 / 250 / 500（策划案 §3.3）
   */
  _upgradeGate(secOpenId) {
    const currentLevel = this.gateLevel;
    if (currentLevel >= 4) {
      this._broadcast({
        type: 'gate_upgrade_failed',
        data: { reason: 'max_level', currentLevel }
      });
      return;
    }

    const cost = GATE_UPGRADE_COSTS[currentLevel - 1]; // index: lv1→idx0=100, lv2→idx1=250, lv3→idx2=500
    if (this.ore < cost) {
      this._broadcast({
        type: 'gate_upgrade_failed',
        data: {
          reason:    'insufficient_ore',
          required:  cost,
          available: Math.round(this.ore),
        }
      });
      return;
    }

    this.ore -= cost;
    this.gateLevel = currentLevel + 1;

    // 更新城门最大血量
    this.gateMaxHp = GATE_MAX_HP_BY_LEVEL[this.gateLevel - 1];
    // 当前血量不超过新的最大值（不主动缩减已有血量）

    this._broadcast({
      type: 'gate_upgraded',
      data: {
        newLevel:     this.gateLevel,
        newMaxHp:     this.gateMaxHp,
        oreRemaining: Math.round(this.ore),
        upgradedBy:   secOpenId || '',
      }
    });

    console.log(`[SurvivalEngine] Gate upgraded to level ${this.gateLevel} (maxHp=${this.gateMaxHp}), ore left=${Math.round(this.ore)}`);

    // 同步资源（矿石变动 + 城门信息）
    this._broadcastResourceUpdate();
  }

  // ==================== 内部：怪物波次（旧调度，兼容保留）====================

  _scheduleNightWaves(day) {
    const cfg = getWaveConfig(day);
    console.log(`[SurvivalEngine] Night ${day} waves: ${cfg.baseCount}-${cfg.maxCount} per wave, every ${cfg.refreshTime}s`);

    let waveIndex = 0;

    // 首波延迟
    const firstTimer = setTimeout(() => {
      this._spawnWave(cfg, day, waveIndex++);

      // 后续波次
      const repeatTimer = setInterval(() => {
        if (this.state !== 'night') {
          clearInterval(repeatTimer);
          return;
        }
        if (waveIndex >= cfg.maxCount) {
          clearInterval(repeatTimer);
          return;
        }
        this._spawnWave(cfg, day, waveIndex++);
      }, cfg.refreshTime * 1000);

      this._waveTimers.push(repeatTimer);
    }, cfg.beginning * 1000);

    this._waveTimers.push(firstTimer);
  }

  _spawnWave(cfg, day, waveIndex) {
    if (this.state !== 'night') return;

    // 生成数量随天数递增，并应用难度倍率
    const baseCnt = cfg.baseCount + Math.floor((day - 1) * 0.5);
    const count   = Math.max(1, Math.round(baseCnt * (this._monsterCntMult || 1.0)));

    const sides = ['left', 'right', 'top', 'all'];
    const spawnSide = sides[Math.floor(Math.random() * sides.length)];

    const waveData = {
      waveIndex,
      day,
      monsterId: cfg.monsterId,
      count,
      spawnSide,
    };

    this.broadcast({ type: 'monster_wave', timestamp: Date.now(), data: waveData });

    // 怪物攻击城门（模拟伤害）
    const damage = Math.floor(count * this.monsterGateDamage);
    this.gateHp = Math.max(0, this.gateHp - damage);

    console.log(`[SurvivalEngine] Wave ${waveIndex}: ${count} monsters (${spawnSide}), gate -${damage}hp → ${this.gateHp}`);

    // 立即推送资源更新（城门HP变化）
    this._broadcastResourceUpdate();

    // 检查失败（城门被攻破）
    this._checkDefeat();
  }

  _clearNightWaves() {
    for (const t of this._waveTimers) {
      clearTimeout(t);
      clearInterval(t);
    }
    this._waveTimers = [];
  }

  // ==================== 随机事件系统（策划案 §8）====================

  /**
   * 每秒检查是否触发随机事件（仅白天）
   */
  _checkRandomEvents() {
    if (this.state !== 'day') return;
    this._nextEventTimer -= 1; // 每秒递减
    if (this._nextEventTimer > 0) return;

    // 重置计时器（90-120s后触发下次）
    this._nextEventTimer = 90 + Math.random() * 30;

    const events = ['E01_snowstorm', 'E02_harvest', 'E03_monster_wave', 'E04_warm_spring', 'E05_ore_vein'];
    const eventId = events[Math.floor(Math.random() * events.length)];
    this._applyRandomEvent(eventId);
  }

  /**
   * 应用随机事件效果并广播
   */
  _applyRandomEvent(eventId) {
    const name = EVENT_NAMES[eventId] || eventId;
    console.log(`[SurvivalEngine] Random event: ${eventId} (${name})`);

    switch (eventId) {
      case 'E01_snowstorm': {
        // 暴风雪：炉温衰减×2，持续60秒
        this.tempDecayMultiplier = 2.0;
        const t = setTimeout(() => { this.tempDecayMultiplier = 1.0; }, 60000);
        this._randomEventTimers.push(t);
        break;
      }
      case 'E02_harvest': {
        // 丰收：食物采集效率×1.5，持续30秒
        this.foodBonus = 1.5;
        const t = setTimeout(() => { this.foodBonus = 1.0; }, 30000);
        this._randomEventTimers.push(t);
        break;
      }
      case 'E03_monster_wave': {
        // 怪物潮：额外生成2只怪物（仅夜晚有意义；白天触发则在下次夜晚生效）
        if (this.state === 'night') {
          const day = this.currentDay;
          for (let i = 0; i < 2; i++) {
            const id = `wave_event_${++this._monsterIdCounter}`;
            this._activeMonsters.set(id, {
              id, type: 'normal',
              maxHp: 30, currentHp: 30, atk: 3,
            });
          }
          this._broadcastResourceUpdate();
        }
        break;
      }
      case 'E04_warm_spring': {
        // 暖流：炉温立即+20℃
        this.furnaceTemp = Math.min(this.maxTemp, this.furnaceTemp + 20);
        this._broadcastResourceUpdate();
        break;
      }
      case 'E05_ore_vein': {
        // 矿脉：矿石采集效率×2，持续45秒
        this.oreBonus = 2.0;
        const t = setTimeout(() => { this.oreBonus = 1.0; }, 45000);
        this._randomEventTimers.push(t);
        break;
      }
    }

    this.broadcast({ type: 'random_event', timestamp: Date.now(), data: { eventId, name } });
  }

  // ==================== 公开辅助接口 ====================

  /**
   * 给玩家增加贡献值（供外部调用，如点赞处理）
   */
  addContribution(playerId, amount) {
    this._trackContribution(playerId, amount);
  }

  /**
   * 主播触发随机事件（broadcaster_action: trigger_event）
   */
  triggerEvent(eventType) {
    // 将旧 eventType 格式映射到新格式
    const mapping = {
      snowstorm:    'E01_snowstorm',
      harvest:      'E02_harvest',
      monster_wave: 'E03_monster_wave',
    };
    const eventId = mapping[eventType] || eventType;
    this._applyRandomEvent(eventId);
  }

  // ==================== 内部：广播 ====================

  /**
   * 内部广播快捷方式（自动附加 timestamp）
   */
  _broadcast(msg) {
    this.broadcast(Object.assign({ timestamp: Date.now() }, msg));
  }

  _broadcastResourceUpdate() {
    this.broadcast({
      type: 'resource_update',
      timestamp: Date.now(),
      data: {
        food:         Math.round(this.food),
        coal:         Math.round(this.coal),
        ore:          Math.round(this.ore),
        furnaceTemp:  Math.round(this.furnaceTemp * 10) / 10,
        gateHp:       Math.round(this.gateHp),
        gateMaxHp:    this.gateMaxHp,
        gateLevel:    this.gateLevel,
        remainingTime: Math.round(this.remainingTime),
        scorePool:    Math.round(this.scorePool),
      }
    });
  }

  // ==================== 内部：工具 ====================

  _trackContribution(playerId, amount) {
    if (!playerId) return;
    this.contributions[playerId] = (this.contributions[playerId] || 0) + amount;
  }

  /**
   * 给玩家加分（贡献值），同时可扩展为分离的积分系统
   */
  _addScore(playerId, playerName, amount) {
    this._trackContribution(playerId, amount);
  }

  _buildRankings() {
    return Object.entries(this.contributions)
      .sort(([, a], [, b]) => b - a)
      .slice(0, 10)
      .map(([id, score], i) => ({
        rank:         i + 1,
        playerId:     id,
        playerName:   this.playerNames[id] || id,  // 结算排行榜显示名
        contribution: score,
        payout:       0,  // 由 _enterSettlement 按权重填充
      }));
  }

  _clearTick() {
    if (this._tickTimer) { clearInterval(this._tickTimer); this._tickTimer = null; }
    if (this._resourceSyncTimer) { clearInterval(this._resourceSyncTimer); this._resourceSyncTimer = null; }
    this._tickCounter = 0;
  }

  _clearAllTimers() {
    this._clearTick();
    this._clearNightWaves();
    if (this._globalEfficiencyBoostTimer) {
      clearTimeout(this._globalEfficiencyBoostTimer);
      this._globalEfficiencyBoostTimer = null;
    }
    // 清除随机事件超时
    for (const t of this._randomEventTimers) clearTimeout(t);
    this._randomEventTimers = [];
  }
}

module.exports = SurvivalGameEngine;
