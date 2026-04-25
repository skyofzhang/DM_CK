using System.Collections.Generic;

namespace DrscfZ.UI {
    /// <summary>
    /// §17.17 失败 toast 文案映射表（v1.27 + audit-r12 GAP-B04+E01+B08 扩展）
    /// 服务端下发 reason 字符串，客户端查表得中文提示
    ///
    /// audit-r11：r10 之前键名失配（'insufficient_resource' 单数 vs 服务端 'insufficient_resources' 复数；
    /// 'supporter' vs 'supporter_not_allowed' 等），永远查不到落 fallback "操作失败"。
    /// r11 按服务端 grep 出的 28 个真实 reason 字面量重写映射表 + 保留少量历史孤儿向后兼容。
    ///
    /// audit-r12：再扩 17 条
    ///   - 商店 §39 _shopFailPurchase 21 处全覆盖：not_unlocked_yet/already_owned/pending_invalid/pending_expired/
    ///                                          no_effect/spotlight_active/limit_exceeded/per_game_limit/
    ///                                          min_lifetime_contrib_required/insufficient（裸字单数）
    ///   - 跨房 §35：cannot_attack_self/already_attacking/target_already_under_attack/target_unavailable/
    ///             target_not_playing/season_ending
    ///   - 矿工/助威：worker_dead/duplicate/max_concurrent/already_voting/already_built
    ///   - 单数 alias：insufficient_resource → "资源不足"（broadcaster_trader_result 单数路径）
    /// </summary>
    public static class FailureToastLocale {
        private static readonly Dictionary<string, string> _map = new Dictionary<string, string> {
            // ────────────────────── 通用阶段 / 权限 ──────────────────────
            { "wrong_phase",            "当前阶段不可用" },
            { "in_cooldown",            "冷却中,请稍候" },
            { "daily_limit",            "今日次数已达上限" },
            { "feature_locked",         "功能尚未解锁" },
            { "not_broadcaster",        "仅主播可操作" },
            { "supporter_not_allowed",  "助威者不可执行此操作" },
            { "invalid_args",           "参数错误" },
            { "invalid_difficulty",     "难度参数无效" },
            { "too_frequent",           "操作过于频繁,请稍候" },
            { "timeout",                "操作超时" },
            { "duplicate",              "重复操作" },
            { "max_concurrent",         "并发上限已达" },

            // ────────────────────── 资源类（服务端复数 / 单数 双写,r12 加单数 alias 与裸字） ──────────────────────
            { "insufficient_resources", "资源不足" },     // 复数：广用
            { "insufficient_resource",  "资源不足" },     // r12 单数 alias（broadcaster_trader_result）
            { "insufficient",           "贡献不足" },     // r12 商店裸字（_shopFailPurchase）
            { "insufficient_ore",       "矿石不足" },
            { "insufficient_balance",   "贡献余额不足" },

            // ────────────────────── 城门 §10 ──────────────────────
            { "boss_fight",             "Boss 战期间不可升级" },
            { "max_level",              "已达最高等级" },

            // ────────────────────── 矿工 / 助威 §9 §33 ──────────────────────
            { "promoted",               "已晋升助威者" },           // fortress_day_changed.reason
            { "demoted_during_build",   "建造期间被降级,无法继续" },
            { "worker_dead",            "矿工已阵亡" },

            // ────────────────────── 探险 §38 ──────────────────────
            { "already_expedition",     "矿工已在探险" },
            { "over_limit",             "探险矿工已满（最多 3 名）" },
            { "expedition_died",        "探险矿工不幸遇难" },
            { "expedition_night_kia",   "夜晚战死,探险中止" },
            { "elite_raid_timeout",     "精英突袭超时" },
            { "meteor_shower",          "陨石坠落,探险中断" },
            { "not_owned",              "目标不属于你" },
            { "slot_mismatch",          "槽位不匹配" },

            // ────────────────────── 跨房 §35（r12 GAP-B04 全枚举） ──────────────────────
            { "self_target",                   "不可攻击自己的直播间" },
            { "self_room",                     "不可攻击自己的直播间" },
            { "cannot_attack_self",            "不可攻击自己的直播间" },
            { "room_not_found",                "目标房间不存在" },
            { "target_busy",                   "目标正在战斗中" },
            { "target_already_under_attack",   "目标已在战斗中" },
            { "attacker_busy",                 "你已在战斗中" },
            { "already_attacking",             "你已发起攻击,请等待结算" },
            { "not_under_attack",              "当前未被攻击" },
            { "boss_killed",                   "Boss 已被击杀" },
            { "target_unavailable",            "目标不可用" },
            { "target_not_playing",            "目标房间未在游戏中" },
            { "target_offline",                "目标房间离线" },
            { "season_ending",                 "赛季即将结束,无法发起" },

            // ────────────────────── 商店 §39（_shopFailPurchase 21 处全覆盖,r12 GAP-B04+E01） ──────────────────────
            { "item_not_found",                "商品不存在或已下架" },
            { "already_joined",                "已参与该活动" },
            { "not_unlocked_yet",              "尚未解锁,请稍候" },
            { "already_owned",                 "已拥有该物品" },
            { "pending_invalid",               "购买请求已失效" },
            { "pending_expired",               "确认窗口已过期,请重新购买" },
            { "no_effect",                     "城门血量已满,无需购买" },
            { "spotlight_active",              "聚光灯效果生效中,稍后再来" },
            { "limit_exceeded",                "本局购买上限已达" },
            { "per_game_limit",                "本局已使用过该效果" },
            { "min_lifetime_contrib_required", "终身贡献尚未达到购买门槛" },

            // ────────────────────── 投票 / 建造 §37 ──────────────────────
            { "already_voting",         "投票已在进行" },
            { "already_built",          "建筑已建成" },

            // ────────────────────── 每日 cap §36 ──────────────────────
            { "cap_blocked",            "今日闯关上限已达" },
            { "cap_reset",              "次数已重置" },

            // ────────────────────── 历史孤儿（向后兼容,部分服务端遗留路径仍发） ──────────────────────
            { "before_supporter_unlock","助威模式尚未开启（堡垒日 6 解锁）" },
            { "already_exists",         "已存在" },
            { "session_ended",          "会话已结束" },
            { "double_confirm_required","请在主播 HUD 确认购买" },
            { "sold_out",               "道具已售罄" },
            { "season_locked",          "赛季末期不可购买" },
        };

        public static string Get(string reason, string fallback = "操作失败") {
            if (string.IsNullOrEmpty(reason)) return fallback;
            return _map.TryGetValue(reason, out var text) ? text : fallback;
        }
    }
}
