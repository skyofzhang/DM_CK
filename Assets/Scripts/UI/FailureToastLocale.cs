using System.Collections.Generic;

namespace DrscfZ.UI {
    /// <summary>
    /// §17.17 失败 toast 文案映射表（v1.27）
    /// 服务端下发 reason 字符串，客户端查表得中文提示
    /// </summary>
    public static class FailureToastLocale {
        private static readonly Dictionary<string, string> _map = new Dictionary<string, string> {
            // 通用
            { "insufficient_resource", "资源不足" },
            { "wrong_phase", "当前阶段不可用" },
            { "in_cooldown", "冷却中，请稍候" },
            { "daily_limit", "今日次数已达上限" },
            { "feature_locked", "功能尚未解锁" },
            { "not_broadcaster", "仅主播可操作" },
            // 城门 §10
            { "boss_fight", "Boss 战期间不可升级" },
            { "insufficient_ore", "矿石不足" },
            // 助威 §33
            { "before_supporter_unlock", "助威模式尚未开启（堡垒日 6 解锁）" },
            // §37 Building
            { "already_exists", "建筑已存在" },
            // §38 Expedition
            { "supporter", "助威者不可探险" },
            { "already_expedition", "矿工已在探险" },
            { "over_limit", "探险矿工已满（最多 3 名）" },
            // §39 Shop
            { "insufficient_balance", "贡献余额不足" },
            { "sold_out", "道具已售罄" },
            { "season_locked", "赛季末期不可购买" },
            { "double_confirm_required", "请在主播 HUD 确认购买" },
            { "session_ended", "道具已失效（当局结束）" },
            // §35 Tribe War
            { "self_room", "不可攻击自己的直播间" },
            { "target_offline", "目标房间离线" },
            // §36 每日 cap
            { "cap_blocked", "今日闯关上限已达" },
        };

        public static string Get(string reason, string fallback = "操作失败") {
            if (string.IsNullOrEmpty(reason)) return fallback;
            return _map.TryGetValue(reason, out var text) ? text : fallback;
        }
    }
}
