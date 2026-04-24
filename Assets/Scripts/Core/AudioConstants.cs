namespace DrscfZ.Core
{
    /// <summary>
    /// 音效ID常量 — 防止拼写错误，统一管理所有音频键名
    ///
    /// 用法：
    ///   AudioManager.Instance.PlaySFX(AudioConstants.SFX_GIFT_T5);
    ///   AudioManager.Instance.CrossfadeBGM(AudioConstants.BGM_DAY, 2f);
    ///   AudioManager.Instance.StartLoopSFX(AudioConstants.SFX_GATE_ALARM, 0.8f);
    /// </summary>
    public static class AudioConstants
    {
        // ============================================================
        // BGM
        // ============================================================

        /// <summary>白天BGM（轻松冬日氛围，100BPM循环）</summary>
        public const string BGM_DAY = "bgm_day";

        /// <summary>夜晚BGM（紧张危机感，140BPM循环）</summary>
        public const string BGM_NIGHT = "bgm_night";

        /// <summary>胜利BGM（凯旋号角，10s不循环）</summary>
        public const string BGM_WIN = "bgm_win";

        /// <summary>失败BGM（单钢琴，8s不循环）</summary>
        public const string BGM_LOSE = "bgm_lose";

        // 原有 Battle BGM（GameManager 使用）
        /// <summary>战斗开场BGM</summary>
        public const string BGM_BATTLE_START = "battle_start";

        /// <summary>普通战斗BGM</summary>
        public const string BGM_NORMAL_BATTLE = "normal_battle";

        /// <summary>临近胜利BGM</summary>
        public const string BGM_NEAR_VICTORY = "near_victory";

        // ============================================================
        // SFX — 采集
        // ============================================================

        /// <summary>采集食物音效（指令1）轻快"叮"声</summary>
        public const string SFX_COLLECT_FOOD = "sfx_collect_food";

        /// <summary>采集煤炭音效（指令2）沉闷镐击声</summary>
        public const string SFX_COLLECT_COAL = "sfx_collect_coal";

        /// <summary>采集矿石音效（指令3）金属碰撞声</summary>
        public const string SFX_COLLECT_ORE = "sfx_collect_ore";

        // ============================================================
        // SFX — 火焰
        // ============================================================

        /// <summary>点火音效（指令4首次触发）</summary>
        public const string SFX_FIRE_START = "sfx_fire_start";

        /// <summary>持续火焰音效（循环，用 StartLoopSFX/StopLoopSFX 控制）</summary>
        public const string SFX_FIRE_LOOP = "sfx_fire_loop";

        // ============================================================
        // SFX — 怪物
        // ============================================================

        /// <summary>怪物被攻击嚎叫声</summary>
        public const string SFX_MONSTER_HIT = "sfx_monster_hit";

        /// <summary>怪物攻击城门撞击声（高优先级）</summary>
        public const string SFX_MONSTER_ATTACK = "sfx_monster_attack";

        // ============================================================
        // SFX — 警报（循环）
        // ============================================================

        /// <summary>城门HP≤30%警报（循环，2s循环体，音量0.8）</summary>
        public const string SFX_GATE_ALARM = "sfx_gate_alarm";

        /// <summary>炉温≤-80℃寒风警报（循环，3s循环体，音量0.8）</summary>
        public const string SFX_COLD_ALARM = "sfx_cold_alarm";

        /// <summary>矿工护盾激活 5s 免伤（§30.3 阶8，audit-r6 P0-F4 常量化）</summary>
        public const string SFX_WORKER_SHIELD_ACTIVATE = "sfx_worker_shield_activate";

        // ============================================================
        // SFX — 礼物（按tier分级）
        // ============================================================

        /// <summary>T1礼物音效（仙女棒，0.5s清脆铃声，音量0.5）</summary>
        public const string SFX_GIFT_T1 = "sfx_gift_t1_ding";

        /// <summary>T2礼物音效（能力药丸，2s魔法泡泡声，音量0.7）</summary>
        public const string SFX_GIFT_T2 = "sfx_gift_t2_bubble";

        /// <summary>T3礼物音效（甜甜圈，3s礼炮声，音量0.9）</summary>
        public const string SFX_GIFT_T3 = "sfx_gift_t3_boom";

        /// <summary>T4礼物音效（能量电池，5s电能爆鸣，音量1.0）</summary>
        public const string SFX_GIFT_T4 = "sfx_gift_t4_electric";

        /// <summary>T5礼物音效（神秘空投，8s序列，音量1.0，最高优先级）</summary>
        public const string SFX_GIFT_T5 = "sfx_gift_t5_airdrop";

        // ---- GiftNotificationUI 内部使用的别名常量（映射到同一AudioClip）----
        // T2 – 出现
        public const string SFX_GIFT_T2_APPEAR   = "sfx_gift_t2_appear";
        // T3 – 飞入落地 / 爆炸（两个阶段共用同一clip，后续可替换为独立素材）
        public const string SFX_GIFT_T3_LAND     = "sfx_gift_t3_land";
        public const string SFX_GIFT_T3_EXPLODE  = "sfx_gift_t3_explode";
        // T4 – 充能
        public const string SFX_GIFT_T4_CHARGE   = "sfx_gift_t4_charge";
        // T5 – 落地 / 史诗序列
        public const string SFX_GIFT_T5_LAND     = "sfx_gift_t5_land";
        public const string SFX_GIFT_T5_EPIC     = "sfx_gift_t5_epic";

        // ============================================================
        // SFX — 昼夜切换
        // ============================================================

        /// <summary>白天开始（鸟鸣+清脆钟声，1.5s）</summary>
        public const string SFX_DAY_START = "sfx_day_start";

        /// <summary>夜晚开始（狼嚎+警报，2s）</summary>
        public const string SFX_NIGHT_START = "sfx_night_start";

        // ============================================================
        // SFX — 排名
        // ============================================================

        /// <summary>守护者排名上升（上升钢琴音调，0.5s）</summary>
        public const string SFX_RANK_UP = "sfx_rank_up";

        /// <summary>守护者排名下降（下降音调，0.5s）</summary>
        public const string SFX_RANK_DOWN = "sfx_rank_down";

        // ============================================================
        // SFX — 主播
        // ============================================================

        /// <summary>主播点击⚡加速按钮（能量激活音，1s）</summary>
        public const string SFX_BROADCASTER_BOOST = "sfx_broadcaster_boost";

        // ============================================================
        // SFX — 原有 Game SFX（GameManager / UI 使用）
        // ============================================================

        /// <summary>玩家加入（弹幕）</summary>
        public const string SFX_PLAYER_JOIN = "player_join";

        /// <summary>VIP加入</summary>
        public const string SFX_VIP_JOIN = "vip_join";

        /// <summary>游戏开始</summary>
        public const string SFX_GAME_START = "game_start";

        /// <summary>胜利</summary>
        public const string SFX_VICTORY = "victory";

        /// <summary>击退</summary>
        public const string SFX_PUSHBACK = "pushback";

        /// <summary>UI点击音效</summary>
        public const string SFX_UI_CLICK = "ui_click";

        /// <summary>单位出生</summary>
        public const string SFX_UNIT_SPAWN = "unit_spawn";

        /// <summary>推进中</summary>
        public const string SFX_PUSHING = "pushing";

        /// <summary>强力推进</summary>
        public const string SFX_PUSH_FORCE = "push_force";

        /// <summary>升级</summary>
        public const string SFX_UPGRADE = "upgrade";

        /// <summary>倒计时</summary>
        public const string SFX_COUNTDOWN = "countdown";

        // ============================================================
        // SFX — UI
        // ============================================================

        /// <summary>Toast提示出现（轻微"嗒"声）</summary>
        public const string SFX_UI_TOAST = "ui_toast";

        /// <summary>结算面板出现（翻页声）</summary>
        public const string SFX_UI_SETTLEMENT = "ui_settlement";
    }
}
