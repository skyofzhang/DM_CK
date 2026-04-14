/**
 * 礼物配置模块 — 极地生存法则
 *
 * 7种礼物按策划案 §5.1 定义（最终确定版）
 * douyin_id 字段全部保留 'TBD'，等待后台礼物ID对齐后填入
 * price_fen 单位：分（1分 = 0.01元 = 0.1抖币）
 */

/*
 * 礼物经济设计（2026）
 *
 * 目标：10-100人在线，消费RMB 0-10000元/天，游戏1个月不枯竭
 *
 * 档位划分：
 * - 免费/低消费 (<¥10/天)：挂机维持，3天内城市不崩
 * - 中消费 (¥10-100/天)：稳定增益，城市繁荣
 * - 高消费 (¥100-1000/天)：明显优势，特殊奖励
 * - 大R (¥1000+/天)：排行榜霸主，永久成就
 */

const GIFTS = {
  // T1 — 仙女棒 (0.1抖币 = 1分)
  // M-NUMERIC：永久叠加设计，低价礼物靠积累产生价值；单次无持续时间概念
  fairy_wand: {
    id:        'fairy_wand',
    name_cn:   '仙女棒',
    douyin_id: 'TBD',
    price_fen: 1,
    tier:      'T1',
    effect:    '发送者效率永久+5%（叠加，上限+100%；每次投喂立即生效，永久持续）',
    score:     1,
  },

  // T2 — 能力药丸 (10抖币 = 100分)
  ability_pill: {
    id:        'ability_pill',
    name_cn:   '能力药丸',
    douyin_id: 'TBD',
    price_fen: 100,
    tier:      'T2',
    effect:    '全员采矿效率+50%（持续30秒）',
    score:     100,
  },

  // T3 — 甜甜圈 (52抖币 = 520分)
  donut: {
    id:        'donut',
    name_cn:   '甜甜圈',
    douyin_id: 'TBD',
    price_fen: 520,
    tier:      'T3',
    effect:    '城门修复+200HP，全局食物+100',
    score:     500,
  },

  // T4 — 能量电池 (99抖币 = 990分)
  energy_battery: {
    id:        'energy_battery',
    name_cn:   '能量电池',
    douyin_id: 'TBD',
    price_fen: 990,
    tier:      'T4',
    effect:    '炉温+30℃，发送者效率+30%（持续180秒）',  // M-NUMERIC: 60s → 180s
    score:     1000,
  },

  // T5 — 爱的爆炸 (199抖币 = 1990分)
  love_explosion: {
    id:        'love_explosion',
    name_cn:   '爱的爆炸',
    douyin_id: 'TBD',
    price_fen: 1990,
    tier:      'T5',
    effect:    '全体怪物受200点AOE伤害、发送者矿工满血复活、城门+200HP',
    score:     2000,
  },

  // T6 — 神秘空投 (520抖币 = 5200分)
  mystery_airdrop: {
    id:        'mystery_airdrop',
    name_cn:   '神秘空投',
    douyin_id: 'TBD',
    price_fen: 5200,
    tier:      'T6',
    effect:    '超级补给：食物+500、煤炭+200、矿石+100、城门+300HP；触发 GIFT_PAUSE 3000ms',
    score:     5000,
  },
};

/**
 * 按抖音平台 gift_id 查找礼物配置
 * 注意：douyin_id 全部为 'TBD'，待后台对齐后填写
 * @param {string} douyin_id
 * @returns {object|null}
 */
function findGiftById(douyin_id) {
  if (!douyin_id || douyin_id === 'TBD') return null;
  return Object.values(GIFTS).find(g => g.douyin_id === douyin_id) || null;
}

/**
 * 按礼物价格（分）查找礼物配置
 * 注意：同价礼物无法仅凭价格区分，此函数返回第一个匹配项。
 * 后台 douyin_id 填写后请改用 findGiftById()。
 * @param {number} price_fen
 * @returns {object|null}
 */
function findGiftByPrice(price_fen) {
  return Object.values(GIFTS).find(g => g.price_fen === price_fen) || null;
}

/**
 * 按内部 gift id 查找礼物配置
 * @param {string} id  内部 ID（如 'fairy_wand'）
 * @returns {object|null}
 */
function getGift(id) {
  return GIFTS[id] || null;
}

function getAllGifts() {
  return GIFTS;
}

/**
 * 将 'T1'~'T5' 转换为数字 1~5（客户端动画档位）
 * @param {string} tier
 * @returns {number}
 */
function getTierNumber(tier) {
  return parseInt(tier.replace('T', ''), 10) || 1;
}

module.exports = { GIFTS, findGiftById, findGiftByPrice, getGift, getAllGifts, getTierNumber };
