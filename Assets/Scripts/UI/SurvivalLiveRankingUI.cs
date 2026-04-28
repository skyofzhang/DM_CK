using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// 实时贡献榜 — 游戏进行时右侧显示 Top5 守护者
    ///
    /// 数据来源：服务器 live_ranking 消息（贡献变化后防抖 1.5s 推送）
    /// 纯显示组件，不做任何本地计分，确保数据与服务器完全一致。
    ///
    /// Inspector 必填：
    ///   _panel     — LiveRankingPanel 根节点（初始 inactive）
    ///   _rankRows  — 5 个预创建的行 GameObject
    ///               每行含 2-3 个 TMP（名次 / 名称 / 贡献值）
    ///   _titleText — 标题 TMP（可选）
    /// </summary>
    public class SurvivalLiveRankingUI : MonoBehaviour
    {
        // 🔴 audit-r30 GAP-D26-06 MVP：单例（仅 SurvivalGameManager.HandleShopEffectTriggered 调用 TriggerSpotlight）
        public static SurvivalLiveRankingUI Instance { get; private set; }

        [Header("面板")]
        [SerializeField] private GameObject   _panel;
        [SerializeField] private TMP_Text     _titleText;

        [Header("排名行（预创建 5 个）")]
        [SerializeField] private GameObject[] _rankRows = new GameObject[5];

        // ─── 内部状态 ────────────────────────────────────────────────────
        private bool   _visible    = false;
        private bool   _subscribed = false;

        // 上一帧 Top5 ID（用于检测排名变化触发高亮动画）
        private readonly string[] _prevRankIds = new string[5];

        // 🔴 audit-r30 GAP-D26-06 MVP 实装：§39.2 A4 spotlight 聚光灯效果
        //   购买 spotlight (250 贡献) 后，自己名字在 Top5 中高亮金色 + ★ 前缀，持续 N 秒
        private string _spotlightPlayerId = null;
        private float  _spotlightExpireTime = 0f;

        // ==================== 生命周期 ====================

        private void Awake()
        {
            // 🔴 audit-r30：Instance 单例赋值（CLAUDE.md 规则 6 — 不在 Awake 内 SetActive(false) 自身 GO）
            if (Instance == null) Instance = this;
            if (_panel != null) _panel.SetActive(false);
        }

        private void Start()
        {
            TrySubscribe();
            BindFont(_titleText);
        }

        private void OnEnable()   { TrySubscribe(); }
        private void OnDisable()  { Unsubscribe(); }
        private void OnDestroy()
        {
            Unsubscribe();
            if (Instance == this) Instance = null; // 🔴 audit-r30：清理 Instance 单例
        }

        // SGM 可能比本组件晚初始化，Update 里补订阅（成功后停止轮询）
        private void Update()
        {
            if (!_subscribed) TrySubscribe();
        }

        /// <summary>🔴 audit-r30 GAP-D26-06 MVP：§39.2 A4 spotlight 聚光灯触发
        /// 由 SurvivalGameManager.HandleShopEffectTriggered case "spotlight" 调用，将 targetPlayerId 高亮 durationSec 秒</summary>
        public void TriggerSpotlight(string targetPlayerId, float durationSec)
        {
            if (string.IsNullOrEmpty(targetPlayerId) || durationSec <= 0f) return;
            _spotlightPlayerId   = targetPlayerId;
            _spotlightExpireTime = Time.time + durationSec;
            // 立即重渲染当前 Top5（如果已可见）— 通过订阅最新数据触发或下次 live_ranking 自动更新
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;

            sgm.OnStateChanged        += OnStateChanged;
            sgm.OnLiveRankingReceived += OnLiveRankingReceived;
            _subscribed = true;

            // ⚠️ 订阅后立即检查当前状态：
            // GameUIPanel 被激活时游戏可能已在 Running 状态，
            // OnStateChanged 不会再次触发，必须主动补一次。
            OnStateChanged(sgm.State);
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnStateChanged        -= OnStateChanged;
                sgm.OnLiveRankingReceived -= OnLiveRankingReceived;
            }
            _subscribed = false;
        }

        // ==================== 事件回调 ====================

        private void OnStateChanged(SurvivalGameManager.SurvivalState state)
        {
            bool isRunning = state == SurvivalGameManager.SurvivalState.Running;
            SetVisible(isRunning);

            if (!isRunning)
            {
                // 非游戏状态：隐藏所有行
                HideAllRows();
            }
        }

        /// <summary>服务器推送实时榜时调用（贡献变化后 1.5s 内触发）</summary>
        private void OnLiveRankingReceived(LiveRankingData data)
        {
            if (!_visible) return;
            RefreshDisplay(data);
        }

        // ==================== 显示控制 ====================

        private void SetVisible(bool visible)
        {
            _visible = visible;
            if (_panel != null) _panel.SetActive(visible);
        }

        private void HideAllRows()
        {
            if (_rankRows == null) return;
            foreach (var row in _rankRows)
                if (row != null) row.SetActive(false);
        }

        // ==================== 刷新显示 ====================

        private void RefreshDisplay(LiveRankingData data)
        {
            if (_titleText != null) _titleText.text = "守护者榜";

            var rankings = data?.rankings;
            int count    = rankings != null ? rankings.Length : 0;
            int rowCount = _rankRows != null ? _rankRows.Length : 0;

            for (int i = 0; i < rowCount; i++)
            {
                var row  = _rankRows[i];
                if (row == null) continue;

                bool show = i < count;
                row.SetActive(show);
                if (!show) continue;

                var entry = rankings[i];

                // 排名变化时触发高亮
                bool changed = _prevRankIds[i] != entry.playerId;
                _prevRankIds[i] = entry.playerId;

                // 填充文字
                var texts = row.GetComponentsInChildren<TMP_Text>(true);
                BindFonts(texts);

                // §39.5 / audit-r5 §19：组合前缀渲染 title + frame + entrance + barrage + legend 金色
                string prefix = GetEquippedPrefix(entry.equipped);
                // 🔴 audit-r30 GAP-D26-06 spotlight MVP：高亮金色 ★ 前缀 + 名字染金（仅命中聚光灯目标 + 未过期）
                bool isSpotlight = !string.IsNullOrEmpty(_spotlightPlayerId)
                                && _spotlightPlayerId == entry.playerId
                                && Time.time < _spotlightExpireTime;
                string truncatedName = Truncate(entry.playerName, 6);
                string name = isSpotlight
                    ? $"<color=#FFD700>★ {prefix}{truncatedName}</color>"
                    : $"{prefix}{truncatedName}";
                string score = entry.contribution.ToString("N0");
                string rank  = $"#{entry.rank}";

                // audit-r5 §19：启用富文本（金色堡垒之王 / 冰霜王冠 / 传奇前缀）
                if (texts.Length >= 3)
                {
                    texts[0].text = rank;
                    texts[1].text = name; texts[1].richText = true;
                    texts[2].text = score;
                }
                else if (texts.Length == 2)
                {
                    texts[0].text = $"{rank} {name}"; texts[0].richText = true;
                    texts[1].text = score;
                }
                else if (texts.Length == 1)
                {
                    texts[0].text = $"{rank} {name}  {score}"; texts[0].richText = true;
                }

                if (changed) StartCoroutine(FlashRow(row));
            }
        }

        // ==================== 工具 ====================

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "玩家";
            return s.Length <= max ? s : s.Substring(0, max) + "..";
        }

        private static TMP_FontAsset _cachedFont;
        private static TMP_FontAsset GetFont()
        {
            if (_cachedFont == null)
                _cachedFont = Resources.Load<TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF") ?? Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
            return _cachedFont;
        }
        private static void BindFont(TMP_Text t)
        {
            if (t == null) return;
            var f = GetFont(); if (f != null) t.font = f;
        }
        private static void BindFonts(TMP_Text[] arr)
        {
            var f = GetFont(); if (f == null) return;
            foreach (var t in arr) if (t != null) t.font = f;
        }

        private IEnumerator FlashRow(GameObject row)
        {
            var images = row.GetComponentsInChildren<Image>(true);
            var orig   = new Color[images.Length];
            for (int i = 0; i < images.Length; i++) orig[i] = images[i].color;

            Color hi = new Color(1f, 0.9f, 0.3f, 0.8f);
            foreach (var img in images) if (img) img.color = hi;
            yield return new WaitForSeconds(0.3f);
            for (int i = 0; i < images.Length; i++) if (images[i]) images[i].color = orig[i];
        }

        /// <summary>
        /// §39.5 商店称号 itemId → 昵称前缀。
        /// 空 / 未知 → 空字符串；大善人的金色渲染另由 UI 层（富文本）做，不在前缀里加 color tag。
        /// </summary>
        private static string GetTitlePrefix(string titleItemId)
        {
            if (string.IsNullOrEmpty(titleItemId)) return "";
            switch (titleItemId)
            {
                case "title_supporter":    return "[守护者]";
                case "title_veteran":      return "[老兵]";
                case "title_legend_mover": return "[大善人]"; // 金色文字由 LiveRankingUI / BobaoData 富文本处理
                default:                    return "";
            }
        }

        /// <summary>
        /// audit-r5 §19/§13.1/§17.10/§39.5 组合 equipped 多槽位（title/frame/entrance/barrage）→ 富文本前缀。
        /// 优先级排序：legend_mover 金色 > barrage_crown 金色 > title → frame ★ → entrance 高亮。
        /// </summary>
        private static string GetEquippedPrefix(ShopEquipped equipped)
        {
            if (equipped == null) return "";
            var sb = new System.Text.StringBuilder();

            // title slot（legend_mover 金色，其他使用 GetTitlePrefix 标签）
            if (!string.IsNullOrEmpty(equipped.title))
            {
                if (equipped.title == "title_legend_mover")
                    sb.Append("<color=#FFD700>【大善人】</color>");
                else
                    sb.Append(GetTitlePrefix(equipped.title));
            }

            // frame slot（所有带星前缀）
            if (!string.IsNullOrEmpty(equipped.frame))
            {
                switch (equipped.frame)
                {
                    case "frame_gold":   sb.Append("<color=#FFD700>★</color>"); break;
                    case "frame_silver": sb.Append("<color=#C0C0C0>★</color>"); break;
                    case "frame_rose":   sb.Append("<color=#FF69B4>★</color>"); break;
                    case "frame_ice":    sb.Append("<color=#66CCFF>★</color>"); break;
                    case "frame_sunset": sb.Append("<color=#FF8C00>★</color>"); break;
                    default:             sb.Append("★"); break;
                }
            }

            // entrance slot（简化为淡色徽标前缀；美术交付前占位）
            if (!string.IsNullOrEmpty(equipped.entrance))
            {
                switch (equipped.entrance)
                {
                    case "entrance_fire":   sb.Append("<color=#FF6820>♨</color>"); break;
                    case "entrance_ice":    sb.Append("<color=#66CCFF>❄</color>"); break;
                    case "entrance_aurora": sb.Append("<color=#80FFD4>✦</color>"); break;
                    case "entrance_crown":  sb.Append("<color=#FFD700>♛</color>"); break;
                    default:                sb.Append("✦"); break;
                }
            }

            // barrage slot（barrage_crown = 冰霜王冠金色；其他无前缀但可由 BarrageMessageUI 着色）
            if (!string.IsNullOrEmpty(equipped.barrage))
            {
                if (equipped.barrage == "barrage_crown")
                    sb.Append("<color=#FFD700>[冰霜王冠]</color>");
                // 其他 barrage 样式由 BarrageMessageUI 渲染时单独读 equipped.barrage，这里不加前缀避免冗余
            }

            return sb.ToString();
        }
    }
}
