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
  // 参与玩家上限 = 客户端矿工模型池大小（WorkerManager.MAX_WORKERS=12）
  static MAX_PLAYERS = 12;

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
    // 目标：Normal难度下不采食约 3min 耗尽（200/1.0≈200s≈3.3min），持续紧迫
    this.foodDecayDay    = config.foodDecayDay    ?? 1.0;   // 白天食物消耗/s
    this.foodDecayNight  = config.foodDecayNight  ?? 1.5;   // 夜晚食物消耗/s（夜晚更快）
    this.tempDecayDay    = config.tempDecayDay    ?? 0.15;  // 白天炉温衰减/s
    this.tempDecayNight  = config.tempDecayNight  ?? 0.4;   // 夜晚炉温衰减/s
    this.coalBurnTicks   = 10;                              // 自动烧煤间隔（tick数，10=2秒）
    this.minTemp         = -100;
    this.maxTemp         = 100;

    // 怪物攻击城门伤害
    this.monsterGateDamage = config.monsterGateDamage ?? 5;

    // ── 矿工HP系统 ──────────────────────────────────────────────────────
    // { [playerId]: { hp, maxHp, isDead, respawnAt } }
    this._workerHp    = {};
    this._workerMaxHp = config.workerMaxHp ?? 100; // 每个矿工的最大HP
    // 矿工承伤倍率：每只怪对矿工的伤害 = monsterGateDamage × workerDamageMult
    this._workerDamageMult = config.workerDamageMult ?? 1.0;
    // 矿工死亡复活等待时间（毫秒）
    this._workerRespawnMs  = (config.workerRespawnSec ?? 30) * 1000;

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
    this._liveRankingTimer = null; // 贡献变化后防抖推送实时榜（1.5s）

    // 礼物效果状态（策划案 §5.1 / §4.4）
    this._playerEfficiencyBonus  = {};  // playerId → 累计加成值（仙女棒 +0.05/个，上限 1.0 = +100%）
    this._playerTempBoost        = {};  // playerId → 倍率（能量电池 1.3，持续180s，per-player）
    this._playerTempBoostTimers  = {};  // playerId → setTimeout handle

    // 666弹幕效果（策划案 §4.3）
    this.efficiency666Bonus = 1.0;   // 效率加成（1.15 = +15%）
    this.efficiency666Timer = 0;     // 剩余秒数

    // 难度倍率（由 _applyDifficulty 设置）
    this._difficulty       = 'normal';
    this._monsterHpMult    = 1.0;
    this._monsterCntMult   = 1.0;

    // ── §33 助威模式（Supporter Mode） ────────────────────────────────
    // _supporters: { playerId: { name, joinedAt, totalContrib } } — 助威者注册表
    // _guardianLastActive: { playerId: timestamp } — AFK 检测用（正式守护者活跃时间）
    // _supporterCmdCooldown: { cmd: timestamp } — 助威者弹幕全局冷却（各 cmd 独立，2s）
    // _supporterAtkBuff: 0 ~ 0.20 — 助威者 cmd=6 叠加的全局攻击加成（正式守护者攻击伤害乘以 1+buff）
    // _supporterAtkBuffTimer: 剩余秒数，归 0 时 buff 重置
    // _playerSlots: { playerId: slotIndex 0~MAX_PLAYERS-1 } — 稳定的矿工槽位分配，替补时槽位原位继承
    this._supporters             = new Map();
    this._guardianLastActive     = {};
    this._supporterCmdCooldown   = {};
    this._supporterAtkBuff       = 0;
    this._supporterAtkBuffTimer  = 0;
    this._afkCheckCounter        = 0;   // _tick 内每 10s 执行一次 AFK 检测的计数器
    this._playerSlots            = {};

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
    // decayMult: 资源衰减倍率（食物+炉温），越高越紧迫
    // coalBurnTicks: 自动烧煤间隔（tick数，1tick=200ms），越小烧得越快
    //   easy:   食物降~7/天, 煤3秒烧1个(宽裕), 初始资源1.5倍
    //   normal: 食物降~10/天, 煤2秒烧1个(标准), 压力适中
    //   hard:   食物降~14/天, 煤1.4秒烧1个(紧迫), 极限生存
    const presets = {
      easy:   { hpMult: 0.6, cntMult: 0.6, decayMult: 0.7, coalBurnTicks: 10, initFood: 160, initCoal: 96,  initOre: 40,  initGateHp: 800,  totalDays: 30, poolNightBase: 300, dayDuration: 120, nightDuration: 120 },
      normal: { hpMult: 1.0, cntMult: 1.0, decayMult: 1.0, coalBurnTicks: 7,  initFood: 200, initCoal: 120, initOre: 50,  initGateHp: 1000, totalDays: 50, poolNightBase: 500, dayDuration: 120, nightDuration: 120 },
      hard:   { hpMult: 1.5, cntMult: 1.5, decayMult: 1.5, coalBurnTicks: 5,  initFood: 300, initCoal: 180, initOre: 75,  initGateHp: 1500, totalDays: 70, poolNightBase: 800, dayDuration: 120, nightDuration: 120 },
    };
    const p = presets[difficulty] || presets.normal;

    // 存储怪物倍率（_enterNight 使用）
    this._monsterHpMult    = p.hpMult;
    this._monsterCntMult   = p.cntMult;
    // 积分池夜晚基础奖励
    this._poolNightBase    = p.poolNightBase;

    // 总关卡天数 + 阶段时长（各难度独立配置）
    this.totalDays     = p.totalDays;
    this.dayDuration   = p.dayDuration;
    this.nightDuration = p.nightDuration;

    // 按倍率调整资源衰减（在构造函数默认值基础上乘以倍率）
    // ⚠️ 默认值必须与构造函数保持一致：foodDecayDay=1.0, foodDecayNight=1.5
    this.foodDecayDay   = (this.config.foodDecayDay   ?? 1.0) * p.decayMult;
    this.foodDecayNight = (this.config.foodDecayNight ?? 1.5) * p.decayMult;
    this.tempDecayDay   = (this.config.tempDecayDay   ?? 0.15) * p.decayMult;
    this.tempDecayNight = (this.config.tempDecayNight ?? 0.40) * p.decayMult;
    this.coalBurnTicks  = p.coalBurnTicks || 10;  // 自动烧煤间隔

    // 各难度独立配置初始资源
    this.food      = p.initFood;
    this.coal      = p.initCoal;
    this.ore       = p.initOre;
    this.gateHp    = p.initGateHp;
    this.gateMaxHp = p.initGateHp;

    console.log(`[Engine] 难度: ${difficulty} | 怪物HP×${p.hpMult} 数量×${p.cntMult} 衰减×${p.decayMult} 煤烧速${p.coalBurnTicks}tick 食${p.initFood}/煤${p.initCoal}/矿${p.initOre}/门${p.initGateHp} 总天数:${p.totalDays}`);
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
    this._playerTempBoost       = {};
    // 清除所有临时 boost 定时器
    for (const t of Object.values(this._playerTempBoostTimers || {})) clearTimeout(t);
    this._playerTempBoostTimers = {};

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

    // §33 助威模式清理
    this._supporters.clear();
    this._guardianLastActive    = {};
    this._supporterCmdCooldown  = {};
    this._supporterAtkBuff      = 0;
    this._supporterAtkBuffTimer = 0;
    this._afkCheckCounter       = 0;
    this._playerSlots           = {};

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
      workerHp:     Object.fromEntries(
        Object.entries(this._workerHp).map(([pid, w]) => [pid, {
          hp: w.hp, maxHp: w.maxHp, isDead: w.isDead, respawnAt: w.respawnAt
        }])
      ),
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

    // §33 助威模式分流：
    // - 已是助威者 → 走助威者分支（优先判断，避免 _trackContribution 污染后被误判为守护者）
    // - 已是正式守护者（contributions[playerId] !== undefined）→ 刷新活跃时间，走现有逻辑
    // - 还有守护者名额（totalPlayers < MAX_PLAYERS） → 绑定为守护者，走现有逻辑
    // - 名额已满 → 进入助威者分支（_handleSupporterComment 内幂等注册 + 推 supporter_joined）
    if (playerId) {
      if (this._supporters.has(playerId)) {
        if (cmd >= 1 && cmd <= 6) {
          this._handleSupporterComment(playerId, playerName, cmd);
        }
        // 助威者 666 与守护者一致（效果本身就是全局的）→ 继续下方 666 分支
        if (content_trim !== '666') return;
      } else if (this.contributions[playerId] !== undefined) {
        // 已是正式守护者
        this._guardianLastActive[playerId] = Date.now();
      } else if (this.totalPlayers < SurvivalGameEngine.MAX_PLAYERS) {
        // 有名额 → 绑定为守护者（与 handlePlayerJoined 对齐）
        this.contributions[playerId] = 0;
        this.playerNames[playerId]   = playerName || playerId;
        this.totalPlayers++;
        this._guardianLastActive[playerId] = Date.now();
        this._allocatePlayerSlot(playerId); // §33: 稳定槽位分配
      } else {
        // 名额已满 → 进入助威者分支
        if (cmd >= 1 && cmd <= 6) {
          this._handleSupporterComment(playerId, playerName, cmd);
        }
        if (content_trim !== '666') return;
      }
    }

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
    // 注册/更新送礼玩家的名字（排行榜结算时使用）
    if (playerId && !this.playerNames[playerId])
      this.playerNames[playerId] = playerName || playerId;

    // §33 助威模式：名额已满且未注册 → 先晋升为助威者，再进入礼物处理
    //   PM 决策（MVP）：跳过 §36.12 seasonDay < supporter_mode.minDay 门槛，永远允许注册
    //   TODO §36.12: add seasonDay < supporter_mode.minDay gate when §36 implemented
    if (playerId
        && this.contributions[playerId] === undefined
        && !this._supporters.has(playerId)
        && this.totalPlayers >= SurvivalGameEngine.MAX_PLAYERS) {
      this._promoteToSupporter(playerId, playerName);
    }
    const isSupporter = playerId ? this._supporters.has(playerId) : false;

    // §33.5 守护者送礼属于活跃行为，刷新 AFK 计时
    if (playerId && !isSupporter && this.contributions[playerId] !== undefined) {
      this._guardianLastActive[playerId] = Date.now();
    }

    // 优先按 douyin_id 查找，TBD 期间回退到按 price_fen 查找
    let gift = findGiftById(giftId);
    if (!gift) gift = getGift(giftId);          // 模拟/测试时用内部ID直接匹配
    if (!gift) gift = findGiftByPrice(giftValue);

    // 完全未知的礼物：忽略，不产生任何游戏效果也不计入贡献/积分池
    if (!gift) {
      console.log(`[SurvivalEngine] unknown gift ignored: ${giftName || giftId} (id=${giftId}, val=${giftValue})`);
      return;
    }

    // ===== §33 助威者特殊礼物分支：T1 仙女棒 / T5 爱的爆炸 =====
    // 这两种礼物依赖"发送者自己有专属矿工"的假设，助威者无矿工 → 需要重路由到场上其他矿工
    if (isSupporter && gift.id === 'fairy_wand') {
      this._handleSupporterFairyWand(playerId, playerName, avatarUrl, gift, giftValue);
      return;
    }
    if (isSupporter && gift.id === 'love_explosion') {
      this._handleSupporterLoveExplosion(playerId, playerName, avatarUrl, gift, giftValue);
      return;
    }

    // 贡献值 = 礼物得分（策划案 §9）
    this._trackContribution(playerId, gift.score);
    // 积分池：礼物得分入池
    this.scorePool += gift.score;

    // ── 矿工复活：送礼时若该玩家的矿工已死亡，立即复活 ──────────────
    //   助威者无 _workerHp 条目，本段自然跳过（?.isDead 为 undefined）
    if (this.state === 'night' && this._workerHp[playerId]?.isDead) {
      this._reviveWorker(playerId);
      console.log(`[SurvivalEngine] Gift revival: ${playerName}'s worker revived`);
    }

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
        // 全员采矿效率+50%，持续30s（复用 efficiency666 系统）
        this.efficiency666Bonus = Math.max(this.efficiency666Bonus, 1.5); // +50%，不叠加只刷新
        this.efficiency666Timer = 30;
        effects.globalEfficiencyBoost = 1.5;
        effects.globalEfficiencyDuration = 30;
        needsResourceSync = true;
        console.log(`[SurvivalEngine] ability_pill: global efficiency +50% for 30s by ${playerName}`);
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
        // 炉温+30℃（共享资源），发送者效率+30%（持续180秒，仅影响自己矿工，刷新计时）
        this.furnaceTemp = Math.min(this.maxTemp, this.furnaceTemp + 30);
        this._playerTempBoost[playerId] = 1.3;
        if (this._playerTempBoostTimers[playerId]) clearTimeout(this._playerTempBoostTimers[playerId]);
        this._playerTempBoostTimers[playerId] = setTimeout(() => {
          this._playerTempBoost[playerId] = 1.0;
          delete this._playerTempBoostTimers[playerId];
        }, 180000);
        effects.addHeat           = 30;
        effects.tempBoost         = 1.3;
        effects.boostDuration     = 180;
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

      case 'love_explosion': {
        // 爱的爆炸：全体怪物AOE伤害200、所有矿工HP全满恢复、城门+200HP
        const aoeDmg = 200;
        const killed = [];
        for (const [mid, m] of this._activeMonsters) {
          m.currentHp -= aoeDmg;
          if (m.currentHp <= 0) killed.push(mid);
        }
        for (const mid of killed) {
          const m = this._activeMonsters.get(mid);
          this._activeMonsters.delete(mid);
          this._broadcast({ type: 'monster_died', data: { monsterId: mid, monsterType: m.type, killerId: playerId } });
        }
        effects.aoeDamage     = aoeDmg;
        effects.monstersKilled = killed.length;

        // 只治疗/复活发送者自己的矿工（不影响其他人）
        const myWorker = this._workerHp[playerId];
        if (myWorker) {
          if (myWorker.isDead) {
            this._reviveWorker(playerId);
            effects.revivedWorkers = [playerId];
          } else {
            myWorker.hp = myWorker.maxHp;
            effects.healedWorkers = 1;
          }
        }

        // 城门+200HP
        this.gateHp = Math.min(this.gateMaxHp, this.gateHp + 200);
        effects.addGateHp = 200;
        needsResourceSync = true;
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
      // §33 助威模式：满员时降级为助威者（替代原"静默丢弃"行为）
      if (this.totalPlayers >= SurvivalGameEngine.MAX_PLAYERS) {
        if (playerId && !this._supporters.has(playerId)) {
          this._promoteToSupporter(playerId, playerName);
        }
        return;
      }
      this.contributions[playerId] = 0;
      this.playerNames[playerId] = playerName || playerId;  // 记录玩家名，结算时填入排行榜
      this.totalPlayers++;
      // 守护者活跃时间初始化（供 §33.5 AFK 替补检测使用）
      this._guardianLastActive[playerId] = Date.now();
      this._allocatePlayerSlot(playerId); // §33: 稳定槽位分配（供 supporter_promoted 使用）
    }

    const data = {
      playerId,
      playerName,
      avatarUrl: avatarUrl || '',
      totalPlayers: this.totalPlayers,
    };
    this.broadcast({ type: 'survival_player_joined', timestamp: Date.now(), data });
  }

  // ==================== §33 助威模式 ====================

  /**
   * 晋升为助威者（幂等；可供 handleComment/handleGift/替补流程共用入口）。
   * 返回 true 表示已注册或已是助威者；false 表示因 §36.12 门槛不满足而保留旁观者身份。
   *
   * PM 决策（MVP v1.27）：跳过 §36.12 seasonDay < supporter_mode.minDay 门槛，永远允许注册。
   * 同样本次不推送 gift_silent_fail（门槛未生效，silent_fail 分支永远不触发）。
   */
  _promoteToSupporter(playerId, playerName) {
    if (!playerId) return false;
    // 幂等：已是助威者直接返回 true
    if (this._supporters.has(playerId)) return true;

    // TODO §36.12: add seasonDay < supporter_mode.minDay gate when §36 implemented
    // if (!this.room.isVeteran && this.room.seasonDay < FEATURE_UNLOCK_DAY.supporter_mode.minDay) return false;

    this._supporters.set(playerId, {
      name: playerName || playerId,
      joinedAt: Date.now(),
      totalContrib: 0,
    });
    // 保持 playerNames 缓存同步（live_ranking / 结算显示名共用）
    if (!this.playerNames[playerId]) this.playerNames[playerId] = playerName || playerId;

    this.broadcast({
      type: 'supporter_joined',
      timestamp: Date.now(),
      data: {
        playerId,
        playerName: playerName || playerId,
        supporterCount: this._supporters.size,
      },
    });
    console.log(`[SurvivalEngine] Supporter joined: ${playerName || playerId} (total=${this._supporters.size})`);
    return true;
  }

  /**
   * 处理助威者弹幕（§33.3 约定的 20-30% 效果 + 2s 全局冷却防刷）
   * @param {string} playerId
   * @param {string} playerName
   * @param {number} cmd  1~4 工作指令 / 6 夜晚攻击
   */
  _handleSupporterComment(playerId, playerName, cmd) {
    // 幂等保底：极端路径（如替补晋升）可能跳过 _promoteToSupporter
    if (!this._promoteToSupporter(playerId, playerName)) return;

    // 同一 cmd 全局冷却 2s（防 200 人直播间同时刷同一指令）
    const now = Date.now();
    if (now - (this._supporterCmdCooldown[cmd] || 0) < 2000) return;
    this._supporterCmdCooldown[cmd] = now;

    // 助威效果（§33.3 表）
    switch (cmd) {
      case 1: this.food = Math.min(2000, this.food + 1); break;
      case 2: this.coal = Math.min(1500, this.coal + 1); break;
      case 3: this.ore  = Math.min(800,  this.ore  + 1); break;
      case 4: this.furnaceTemp = Math.min(this.maxTemp, this.furnaceTemp + 0.5); break;
      case 6:
        if (this.state === 'night') {
          this._supporterAtkBuff = Math.min(0.20, this._supporterAtkBuff + 0.02);
          this._supporterAtkBuffTimer = 5;
        }
        break;
      default: return; // 其他 cmd 不生效，也不计贡献
    }

    // 助威者贡献值 +1（与正式守护者一致），计入 live_ranking
    this._trackContribution(playerId, 1);

    this.broadcast({
      type: 'supporter_action',
      timestamp: Date.now(),
      data: { playerId, playerName: playerName || playerId, cmd },
    });
  }

  /**
   * 助威者送 T1 仙女棒：随机挑一名在场守护者挂载 +0.05 效率（上限 1.0）。
   * 贡献值 +1，积分池 +1。
   */
  _handleSupporterFairyWand(playerId, playerName, avatarUrl, gift, giftValue) {
    const effects = { efficiencyBonus: 0, supporterRedirect: true };
    const guardians = Object.keys(this.contributions);
    if (guardians.length > 0) {
      const targetId = guardians[Math.floor(Math.random() * guardians.length)];
      const prev = this._playerEfficiencyBonus[targetId] || 0;
      this._playerEfficiencyBonus[targetId] = Math.min(1.0, prev + 0.05);
      effects.efficiencyBonus = this._playerEfficiencyBonus[targetId];
      effects.redirectTargetId   = targetId;
      effects.redirectTargetName = this.playerNames[targetId] || targetId;
    }

    // 贡献与积分池（不走 handleGift 的默认累加路径，避免重复计入）
    this._trackContribution(playerId, 1);
    this.scorePool += 1;

    const giftData = {
      playerId,
      playerName,
      avatarUrl: avatarUrl || '',
      giftId:     gift.id,
      giftName:   gift.name_cn,
      giftTier:   getTierNumber(gift.tier),
      giftTierStr: gift.tier,
      giftValue,
      score:      gift.score,
      contribution: gift.score,
      addFood: 0, addCoal: 0, addOre: 0, addHeat: 0, addGateHp: 0,
      effects,
    };
    this.broadcast({ type: 'survival_gift', timestamp: Date.now(), data: giftData });
    console.log(`[SurvivalEngine] supporter gift: ${playerName} → ${gift.name_cn} redirected to ${effects.redirectTargetName || '(no guardian)'}`);
  }

  /**
   * 助威者送 T5 爱的爆炸：AOE + 城门 HP 与守护者一致；复活逻辑改为"随机一名已死亡矿工"。
   * 贡献值 +2000，积分池 +2000。
   */
  _handleSupporterLoveExplosion(playerId, playerName, avatarUrl, gift, giftValue) {
    const effects = { supporterRedirect: true };
    const aoeDmg  = 200;

    // AOE 全体怪物 -200 HP
    const killed = [];
    for (const [mid, m] of this._activeMonsters) {
      m.currentHp -= aoeDmg;
      if (m.currentHp <= 0) killed.push(mid);
    }
    for (const mid of killed) {
      const m = this._activeMonsters.get(mid);
      this._activeMonsters.delete(mid);
      this._broadcast({
        type: 'monster_died',
        data: { monsterId: mid, monsterType: m.type, killerId: playerId },
      });
    }
    effects.aoeDamage      = aoeDmg;
    effects.monstersKilled = killed.length;

    // 随机复活一名已死亡矿工（替代"仅发送者矿工"）
    const deadWorkers = Object.entries(this._workerHp)
      .filter(([, w]) => w && w.isDead)
      .map(([pid]) => pid);
    if (deadWorkers.length > 0) {
      const targetId = deadWorkers[Math.floor(Math.random() * deadWorkers.length)];
      this._reviveWorker(targetId);
      effects.revivedWorkers = [targetId];
    }

    // 城门 +200HP
    this.gateHp = Math.min(this.gateMaxHp, this.gateHp + 200);
    effects.addGateHp = 200;

    // 贡献 + 积分池
    this._trackContribution(playerId, gift.score);
    this.scorePool += gift.score;

    const giftData = {
      playerId,
      playerName,
      avatarUrl: avatarUrl || '',
      giftId:     gift.id,
      giftName:   gift.name_cn,
      giftTier:   getTierNumber(gift.tier),
      giftTierStr: gift.tier,
      giftValue,
      score:      gift.score,
      contribution: gift.score,
      addFood: 0, addCoal: 0, addOre: 0, addHeat: 0,
      addGateHp: effects.addGateHp,
      effects,
    };
    this.broadcast({ type: 'survival_gift', timestamp: Date.now(), data: giftData });
    this._broadcastResourceUpdate();
    console.log(`[SurvivalEngine] supporter T5 love_explosion by ${playerName}: killed ${killed.length} monsters, revived ${effects.revivedWorkers ? effects.revivedWorkers.length : 0} worker(s)`);
  }

  /**
   * AFK 替补检测：正式守护者连续 60s 无任何操作时，最早加入的助威者替补上场。
   * 每 10s 调用一次（_tick 内），每次最多替换 1 人，避免批量震荡。
   */
  _checkAfkReplacement() {
    if (this._supporters.size === 0) return;

    const now = Date.now();
    const AFK_THRESHOLD = 60000; // 60s

    for (const [pid, lastActive] of Object.entries(this._guardianLastActive)) {
      // 豁免：主播永不替换（_roomCreatorId 未注入时视为无主播豁免）
      if (this._roomCreatorId && pid === this._roomCreatorId) continue;
      // 豁免：夜晚死亡等待复活中的矿工不替换
      if (this._workerHp[pid]?.isDead) continue;
      // 未到 AFK 阈值
      if (now - lastActive < AFK_THRESHOLD) continue;

      // 选最早加入且不在 30s 冷却期的助威者（joinedAt > now 表示冷却中）
      let earliestId = null;
      let earliestTime = Infinity;
      for (const [sid, sData] of this._supporters) {
        if (sData.joinedAt > now) continue; // 30s 冷却中，不可排队
        if (sData.joinedAt < earliestTime) {
          earliestId = sid;
          earliestTime = sData.joinedAt;
        }
      }
      if (!earliestId) return;

      this._promoteSupporter(earliestId, pid);
      return; // 每次只替换 1 人
    }
  }

  /**
   * 助威者替补上场（newId）/ 旧守护者降级为助威者（oldId，30s 冷却排队末尾）。
   */
  _promoteSupporter(newId, oldId) {
    const newData = this._supporters.get(newId);
    if (!newData) return;

    const oldName = this._getPlayerName(oldId);
    const newName = newData.name || this._getPlayerName(newId);
    const workerIndex = this._getWorkerIndex(oldId);

    // 旧守护者 → 助威者（保存当前贡献快照，30s 冷却）
    this._supporters.set(oldId, {
      name: oldName,
      joinedAt: Date.now() + 30000,
      totalContrib: this.contributions[oldId] || 0,
    });
    delete this.contributions[oldId];
    delete this._guardianLastActive[oldId];
    delete this._playerSlots[oldId]; // §33: 释放槽位供 newId 原位继承
    // 不减 totalPlayers：槽位由 newId 继承（见下）

    // 新助威者 → 守护者（继承 totalContrib 作为 contributions 起始值 + 继承 workerIndex 槽位）
    this.contributions[newId] = newData.totalContrib || 0;
    this.playerNames[newId]   = newName;
    this._guardianLastActive[newId] = Date.now();
    this._supporters.delete(newId);
    if (workerIndex >= 0) this._playerSlots[newId] = workerIndex; // 原位继承

    this.broadcast({
      type: 'supporter_promoted',
      timestamp: Date.now(),
      data: {
        newPlayerId:   newId,
        newPlayerName: newName,
        oldPlayerId:   oldId,
        oldPlayerName: oldName,
        workerIndex,
      },
    });
    console.log(`[SurvivalEngine] Supporter promoted: ${newName} replaces ${oldName} (workerIndex=${workerIndex})`);
  }

  /** 获取玩家名（优先 playerNames，fallback 到 supporters.name，再 fallback 到 playerId） */
  _getPlayerName(playerId) {
    if (!playerId) return '';
    if (this.playerNames[playerId]) return this.playerNames[playerId];
    const s = this._supporters.get(playerId);
    if (s && s.name) return s.name;
    return playerId;
  }

  /** 获取玩家的 workerIndex（0~MAX_PLAYERS-1），未找到返回 -1 */
  _getWorkerIndex(playerId) {
    if (!playerId) return -1;
    const slot = this._playerSlots[playerId];
    return (typeof slot === 'number') ? slot : -1;
  }

  /** 找一个空槽位（0~MAX_PLAYERS-1）分配给新守护者；满员返回 -1 */
  _allocatePlayerSlot(playerId) {
    if (!playerId) return -1;
    if (typeof this._playerSlots[playerId] === 'number') return this._playerSlots[playerId];
    const occupied = new Set(Object.values(this._playerSlots));
    for (let i = 0; i < SurvivalGameEngine.MAX_PLAYERS; i++) {
      if (!occupied.has(i)) {
        this._playerSlots[playerId] = i;
        return i;
      }
    }
    return -1;
  }

  // ==================== 内部：阶段切换 ====================

  _enterDay(day) {
    this._clearNightWaves();
    this._activeMonsters.clear();
    this.state         = 'day';

    // 天亮时立即复活所有死亡矿工
    this._reviveAllWorkers('day_started');
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

    // 初始化矿工HP（夜晚开始时全员满血上阵）
    this._initWorkerHp();

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

      // §33 助威者攻击加成衰减（每秒 -1；归 0 时 buff 重置）
      if (this._supporterAtkBuffTimer > 0) {
        this._supporterAtkBuffTimer = Math.max(0, this._supporterAtkBuffTimer - 1);
        if (this._supporterAtkBuffTimer <= 0) {
          this._supporterAtkBuff = 0;
        }
      }

      // §33 AFK 替补检测（每 10s 执行一次，每次最多换 1 人）
      this._afkCheckCounter = (this._afkCheckCounter || 0) + 1;
      if (this._afkCheckCounter >= 10) {
        this._afkCheckCounter = 0;
        this._checkAfkReplacement();
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

  // ==================== 矿工HP系统 ====================

  /** 夜晚开始时初始化所有已加入玩家的矿工HP（满血）*/
  _initWorkerHp() {
    this._workerHp = {};
    for (const pid of Object.keys(this.contributions)) {
      this._workerHp[pid] = {
        hp:        this._workerMaxHp,
        maxHp:     this._workerMaxHp,
        isDead:    false,
        respawnAt: 0,
      };
    }
    // 同步完整矿工HP状态到客户端
    this._broadcastWorkerHp();
    console.log(`[SurvivalEngine] Worker HP initialized for ${Object.keys(this._workerHp).length} players`);
  }

  /** 复活指定玩家的矿工 */
  _reviveWorker(playerId) {
    const w = this._workerHp[playerId];
    if (!w || !w.isDead) return;
    w.hp        = w.maxHp;
    w.isDead    = false;
    w.respawnAt = 0;
    this._broadcast({
      type: 'worker_revived',
      timestamp: Date.now(),
      data: { playerId }
    });
    this._broadcastWorkerHp();
    console.log(`[SurvivalEngine] Worker ${playerId} revived`);
  }

  /** 复活所有死亡矿工（天亮或结算时调用）*/
  _reviveAllWorkers(reason = 'day') {
    let count = 0;
    for (const pid of Object.keys(this._workerHp)) {
      const w = this._workerHp[pid];
      if (w.isDead) {
        w.hp        = w.maxHp;
        w.isDead    = false;
        w.respawnAt = 0;
        this._broadcast({ type: 'worker_revived', timestamp: Date.now(), data: { playerId: pid } });
        count++;
      }
    }
    if (count > 0) {
      this._broadcastWorkerHp();
      console.log(`[SurvivalEngine] Revived ${count} workers (reason: ${reason})`);
    }
  }

  /** 广播当前所有矿工HP快照（数组格式，方便 Unity JsonUtility 解析）*/
  _broadcastWorkerHp() {
    if (Object.keys(this._workerHp).length === 0) return;
    const workers = Object.entries(this._workerHp).map(([pid, w]) => ({
      playerId:  pid,
      hp:        w.hp,
      maxHp:     w.maxHp,
      isDead:    w.isDead,
      respawnAt: w.respawnAt,
    }));
    this._broadcast({ type: 'worker_hp_update', timestamp: Date.now(), data: { workers } });
  }

  _decayResources() {
    const isNight = this.state === 'night';

    // 食物：每秒消耗
    this.food = Math.max(0, this.food - (isNight ? this.foodDecayNight : this.foodDecayDay));

    // 炉温：有煤时自动烧煤维持温度，无煤时才开始失温
    const baseTempDecay = isNight ? this.tempDecayNight : this.tempDecayDay;
    const tempDecay = baseTempDecay * (this.tempDecayMultiplier || 1.0);
    if (this.coal > 0) {
      // 有煤：按难度自动消耗煤炭维持炉温（easy=2秒/个, normal=1.4秒/个, hard=1秒/个）
      if (this._tickCounter % this.coalBurnTicks === 0) {
        this.coal = Math.max(0, this.coal - 1);
      }
    } else {
      // 无煤：炉温持续下降（夜晚更快，暴风雪加速）
      this.furnaceTemp = Math.max(this.minTemp, this.furnaceTemp - tempDecay);
    }

    // 矿石 → 自动修复城门（每2秒消耗1矿石，补5HP；仅在城门受损时生效）
    // 设计意图：矿石让玩家挖矿有动力——矿越多，城门越能抵御怪物攻击
    if (this._tickCounter % 10 === 0 && this.ore > 0 && this.gateHp < this.gateMaxHp) {
      const repair = Math.min(5, this.gateMaxHp - this.gateHp);
      this.gateHp = Math.min(this.gateMaxHp, this.gateHp + repair);
      this.ore    = Math.max(0, this.ore - 1);
    }

    // 取整便于显示
    this.food        = Math.round(this.food * 10) / 10;
    this.furnaceTemp = Math.round(this.furnaceTemp * 10) / 10;
    this.gateHp      = Math.round(this.gateHp);
    this.ore         = Math.round(this.ore);
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

    // 综合效率倍率计算（策划案 §4.4）
    // playerBonus：仙女棒永久累计加成（最高+100%）
    // globalBoost：能量电池 per-player 临时加成（×1.3，持续180s）
    // 综合上限 ×3.0
    const playerBonus      = 1.0 + (this._playerEfficiencyBonus[playerId] || 0); // 仙女棒加成
    const globalBoost      = this._playerTempBoost[playerId] || 1.0;             // 能量电池 per-player 加成
    const broadcasterBoost = this.broadcasterEfficiencyMultiplier || 1.0;         // 主播紧急加速
    const eff666Boost      = this.efficiency666Bonus || 1.0;                      // 666弹幕加成
    const totalMult        = Math.min(3.0, playerBonus * globalBoost * broadcasterBoost * eff666Boost);

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

    // §33 助威者 cmd=6 叠加的全局攻击加成（1 + buff，buff 0~0.20）
    const baseDamage = 10;
    const damage = Math.max(1, Math.round(baseDamage * (1 + (this._supporterAtkBuff || 0))));
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

    // ── 怪物伤害路由：优先打矿工，矿工全死后才打城门 ──────────────────
    const totalDamage = Math.floor(count * this.monsterGateDamage * this._workerDamageMult);
    let remainingDamage = totalDamage;

    const aliveWorkers = Object.entries(this._workerHp).filter(([, w]) => !w.isDead);
    if (aliveWorkers.length > 0) {
      // 每个存活矿工平均承担伤害（至少1点，避免小伤害完全跳过矿工直打城门）
      const damagePerWorker = Math.max(1, Math.floor(totalDamage / aliveWorkers.length));
      for (const [pid, w] of aliveWorkers) {
        const actualDmg = Math.min(w.hp, damagePerWorker);
        w.hp -= actualDmg;
        remainingDamage -= actualDmg;

        if (w.hp <= 0) {
          w.hp = 0;
          w.isDead = true;
          w.respawnAt = Date.now() + this._workerRespawnMs;

          // 广播矿工死亡
          this._broadcast({
            type: 'worker_died',
            timestamp: Date.now(),
            data: { playerId: pid, respawnAt: w.respawnAt }
          });
          console.log(`[SurvivalEngine] Worker ${pid} died, respawn at +${this._workerRespawnMs/1000}s`);

          // 自动定时复活（若天亮先到则由 _reviveAllWorkers 处理）
          const respawnTimer = setTimeout(() => {
            if (this._workerHp[pid]?.isDead) this._reviveWorker(pid);
          }, this._workerRespawnMs);
          this._waveTimers.push(respawnTimer);
        }
      }
      // 如果所有矿工刚好被打死，剩余伤害继续打城门
    }

    // 剩余伤害（矿工全死后的溢出）打城门；clamp到0避免矿工分摊时负值"治疗"城门
    remainingDamage = Math.max(0, remainingDamage);
    if (remainingDamage > 0) {
      this.gateHp = Math.max(0, this.gateHp - remainingDamage);
    }

    // 广播矿工HP更新
    this._broadcastWorkerHp();

    console.log(`[SurvivalEngine] Wave ${waveIndex}: ${count} monsters (${spawnSide}), workers=${aliveWorkers.length}, gate -${remainingDamage}hp → ${this.gateHp}`);

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
        workerHp:     Object.fromEntries(
          Object.entries(this._workerHp).map(([pid, w]) => [pid, {
            hp: w.hp, maxHp: w.maxHp, isDead: w.isDead, respawnAt: w.respawnAt
          }])
        ),
      }
    });
  }

  // ==================== 内部：工具 ====================

  _trackContribution(playerId, amount) {
    if (!playerId) return;
    this.contributions[playerId] = (this.contributions[playerId] || 0) + amount;
    // 防抖：贡献变化后 1.5s 推送实时榜（避免每条弹幕都广播）
    this._scheduleLiveRankingBroadcast();
  }

  /** 防抖推送实时贡献榜（1.5s 内多次变化只推一次） */
  _scheduleLiveRankingBroadcast() {
    if (this._liveRankingTimer) return; // 已有待推送定时器，无需重复
    this._liveRankingTimer = setTimeout(() => {
      this._liveRankingTimer = null;
      this._broadcastLiveRanking();
    }, 1500);
  }

  /** 广播当前局 Top5 实时贡献榜 */
  _broadcastLiveRanking() {
    if (this.state !== 'day' && this.state !== 'night') return;
    const top5 = Object.entries(this.contributions)
      .sort(([, a], [, b]) => b - a)
      .slice(0, 5)
      .map(([id, score], i) => ({
        rank:        i + 1,
        playerId:    id,
        playerName:  this.playerNames[id] || id,
        contribution: Math.round(score),
      }));
    this.broadcast({ type: 'live_ranking', timestamp: Date.now(), data: { rankings: top5 } });
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
    // 清除 per-player 临时 boost 定时器（能量电池）
    for (const t of Object.values(this._playerTempBoostTimers || {})) clearTimeout(t);
    this._playerTempBoostTimers = {};
    // 清除随机事件超时
    for (const t of this._randomEventTimers) clearTimeout(t);
    this._randomEventTimers = [];
    // 清除实时榜防抖定时器
    if (this._liveRankingTimer) { clearTimeout(this._liveRankingTimer); this._liveRankingTimer = null; }
  }
}

module.exports = SurvivalGameEngine;
