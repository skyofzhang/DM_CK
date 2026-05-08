/**
 * 礼物配置模块 — 极地生存法则
 *
 * 6种礼物按策划案 §7.1 定义（最终确定版）— r14 GAP-A14-A5 修：原 "7种" / "§5.1"
 * douyin_id 字段已按当前 6 档礼物回填；PLACEHOLDER_DOUYIN_ID 仅保留给校验逻辑。
 * price_fen 单位：分（1分 = 0.01元 = 0.1抖币）
 *
 * ⚠️ 上线前合规硬门槛（重要）：
 *   - 必须通过抖音平台 GetDouyinGiftIdList 核对当前 6 个 douyin_id
 *   - 当前 findGiftByPrice() 按 price_fen 匹配作为 fallback
 *     （仅在 gift_num=1 可靠；多件礼物合并推送可能匹配失败）
 *   - 同价礼物（若未来存在）无法仅凭价格区分，将返回第一个匹配项
 *   - 对接文档：
 *     https://developer.open-douyin.com/docs/resource/zh-CN/interaction/develop-guide/live/server-push
 *   - findGiftById 是主路径；findGiftByPrice 仅作为审计兜底。
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

const PLACEHOLDER_DOUYIN_ID = 'TBD';

const GIFTS = {
  // T1 — 仙女棒 (1抖币 = 10分)
  // M-NUMERIC：永久叠加设计，低价礼物靠积累产生价值；单次无持续时间概念
  fairy_wand: {
    id:        'fairy_wand',
    name_cn:   '仙女棒',
    douyin_id: 'n1/Dg1905sj1FyoBlQBvmbaDZFBNaKuKZH6zxHkv8Lg5x2cRfrKUTb8gzMs=',
    price_fen: 10,
    tier:      'T1',
    effect:    '发送者效率永久+5%（叠加，上限+100%；每次投喂立即生效，永久持续）',
    score:     1,
  },

  // T2 — 能力药丸 (10抖币 = 100分)
  ability_pill: {
    id:        'ability_pill',
    name_cn:   '能力药丸',
    douyin_id: '28rYzVFNyXEXFC8HI+f/WG+I7a6lfl3OyZZjUS+CVuwCgYZrPrUdytGHu0c=',
    price_fen: 100,
    tier:      'T2',
    effect:    '全员采矿效率+50%（持续30秒）',
    score:     100,
  },

  // T3 — 甜甜圈 (52抖币 = 520分)
  donut: {
    id:        'donut',
    name_cn:   '甜甜圈',
    douyin_id: 'PJ0FFeaDzXUreuUBZH6Hs+b56Jh0tQjrq0bIrrlZmv13GSAL9Q1hf59fjGk=',
    price_fen: 520,
    tier:      'T3',
    effect:    '城门修复+200HP，全局食物+100',
    score:     500,
  },

  // T4 — 能量电池 (99抖币 = 990分)
  energy_battery: {
    id:        'energy_battery',
    name_cn:   '能量电池',
    douyin_id: 'IkkadLfz7O/a5UR45p/OOCCG6ewAWVbsuzR/Z+v1v76CBU+mTG/wPjqdpfg=',
    price_fen: 990,
    tier:      'T4',
    effect:    '炉温+30℃，发送者效率+30%（持续180秒）',  // M-NUMERIC: 60s → 180s
    score:     1000,
  },

  // T5 — 爱的爆炸 (199抖币 = 1990分)
  love_explosion: {
    id:        'love_explosion',
    name_cn:   '爱的爆炸',
    douyin_id: 'gx7pmjQfhBaDOG2XkWI2peZ66YFWkCWRjZXpTqb23O/epru+sxWyTV/3Ufs=',
    price_fen: 1990,
    tier:      'T5',
    effect:    '全体怪物受200点AOE伤害、发送者矿工满血复活、城门+200HP',
    score:     2000,
  },

  // T6 — 神秘空投 (520抖币 = 5200分)
  mystery_airdrop: {
    id:        'mystery_airdrop',
    name_cn:   '神秘空投',
    douyin_id: 'pGLo7HKNk1i4djkicmJXf6iWEyd+pfPBjbsHmd3WcX0Ierm2UdnRR7UINvI=',
    price_fen: 5200,
    tier:      'T6',
    effect:    '超级补给：食物+500、煤炭+200、矿石+100、城门+300HP；触发 GIFT_PAUSE 3000ms',
    score:     5000,
  },
};

/**
 * 按抖音平台 gift_id 查找礼物配置
 * 注意：douyin_id 必须与抖音后台当前礼物列表保持一致。
 * @param {string} douyin_id
 * @returns {object|null}
 */
function findGiftById(douyin_id) {
  if (!douyin_id || douyin_id === PLACEHOLDER_DOUYIN_ID) return null;
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

function getUnresolvedDouyinGifts() {
  return Object.values(GIFTS)
    .filter(g => !g.douyin_id || g.douyin_id === PLACEHOLDER_DOUYIN_ID)
    .map(g => ({
      id: g.id,
      tier: g.tier,
      name_cn: g.name_cn,
      price_fen: g.price_fen,
      douyin_id: g.douyin_id || '',
    }));
}

function assertDouyinGiftIdsReady(options = {}) {
  const allowPlaceholders = options.allowPlaceholders === true;
  const unresolved = getUnresolvedDouyinGifts();
  if (unresolved.length > 0 && !allowPlaceholders) {
    const summary = unresolved.map(g => `${g.id}/${g.tier}/price_fen=${g.price_fen}`).join(', ');
    throw new Error(
      `[GiftConfig] Unresolved douyin_id placeholders: ${summary}. ` +
      'Fill real Douyin gift ids before production deploy, or set ALLOW_TBD_DOUYIN_GIFT_IDS=1 for local testing only.'
    );
  }

  const seen = new Map();
  const duplicates = [];
  for (const gift of Object.values(GIFTS)) {
    if (!gift.douyin_id || gift.douyin_id === PLACEHOLDER_DOUYIN_ID) continue;
    if (seen.has(gift.douyin_id)) {
      duplicates.push(`${gift.douyin_id}:${seen.get(gift.douyin_id)}+${gift.id}`);
    } else {
      seen.set(gift.douyin_id, gift.id);
    }
  }
  if (duplicates.length > 0) {
    throw new Error(`[GiftConfig] Duplicate douyin_id values: ${duplicates.join(', ')}`);
  }

  return unresolved;
}

/**
 * 将 'T1'~'T6' 转换为数字 1~6（客户端动画档位）— r14 GAP-A14-A6 修：原 T1~T5
 * @param {string} tier
 * @returns {number}
 */
function getTierNumber(tier) {
  return parseInt(tier.replace('T', ''), 10) || 1;
}

module.exports = {
  GIFTS,
  PLACEHOLDER_DOUYIN_ID,
  findGiftById,
  findGiftByPrice,
  getGift,
  getAllGifts,
  getUnresolvedDouyinGifts,
  assertDouyinGiftIdsReady,
  getTierNumber,
};
