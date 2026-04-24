using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using DrscfZ.Core;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// 生存游戏排行榜 UI — 双页签：贡献排行榜 + 主播排行榜
    ///
    /// 设计：
    ///   - 大厅"排行榜"按钮 / 结算"查看英雄榜"按钮打开
    ///   - 两个页签：贡献榜（玩家本周累计贡献）、主播榜（主播完成难度×天数）
    ///   - 贡献榜：服务器 WeeklyRankingStore 持久化，每周一重置
    ///   - 主播榜：服务器 StreamerRankingStore 持久化，历史最佳记录
    ///   - 面板打开时主动请求两个榜的数据
    ///
    /// 挂载规则（Rule #7）：
    ///   脚本挂载在 Canvas（始终激活）；
    ///   面板根节点 _panel 初始 inactive，ShowPanel()/HidePanel() 控制。
    ///
    /// Inspector 必填：
    ///   _panel, _overlay, _closeBtn, _titleText, _subtitleText,
    ///   _tabContribution, _tabStreamer  — 两个页签按钮
    ///   _rowContainer, _emptyHint
    ///
    /// 预创建行 TMP 顺序（贡献榜）：
    ///   texts[0] = 名次  "#1"
    ///   texts[1] = 玩家名
    ///   texts[2] = 贡献值
    ///
    /// 主播榜行 TMP 顺序：
    ///   texts[0] = 名次  "#1"
    ///   texts[1] = 主播名
    ///   texts[2] = 难度+天数+胜率
    /// </summary>
    public class SurvivalRankingUI : MonoBehaviour
    {
        [Header("面板")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private GameObject _overlay;
        [SerializeField] private Button     _closeBtn;
        [SerializeField] private TMP_Text   _titleText;
        [SerializeField] private TMP_Text   _subtitleText;

        [Header("页签按钮")]
        [SerializeField] private Button _tabContribution;  // 贡献排行榜
        [SerializeField] private Button _tabStreamer;       // 主播排行榜

        [Header("页签素材")]
        [SerializeField] private Sprite _tabActiveSpriteRef;   // Inspector 拖入 tab_active
        [SerializeField] private Sprite _tabInactiveSpriteRef; // Inspector 拖入 tab_inactive

        [Header("表头")]
        [SerializeField] private GameObject _headerRow;  // HeaderRow 对象

        [Header("奖牌图标")]
        [SerializeField] private Sprite _medalGold;
        [SerializeField] private Sprite _medalSilver;
        [SerializeField] private Sprite _medalBronze;

        [Header("列表区域")]
        [SerializeField] private Transform  _rowContainer;
        [SerializeField] private TMP_Text   _emptyHint;

        private enum TabType { Contribution, Streamer }
        private TabType _currentTab = TabType.Contribution;

        // ——— 预创建行缓存 ———
        private readonly List<GameObject> _rows = new List<GameObject>();

        // ——— 数据缓存 ———
        private WeeklyRankingData   _cachedWeekly;
        private StreamerRankingData _cachedStreamer;

        // ——— 页签样式 ———
        private static readonly Color TAB_ACTIVE_TEXT    = new Color(1f, 0.85f, 0.4f, 1f);  // 金色文字
        private static readonly Color TAB_INACTIVE_TEXT  = new Color(0.6f, 0.6f, 0.7f, 1f); // 灰色文字
        private Sprite _tabActiveSprite;
        private Sprite _tabInactiveSprite;

        private bool _subscribed;

        // ==================== 生命周期 ====================

        private void Awake()
        {
            CollectRows();

            // 使用 Inspector 引用的页签素材
            _tabActiveSprite   = _tabActiveSpriteRef;
            _tabInactiveSprite = _tabInactiveSpriteRef;

            if (_closeBtn != null)
                _closeBtn.onClick.AddListener(HidePanel);

            if (_tabContribution != null)
                _tabContribution.onClick.AddListener(() => SwitchTab(TabType.Contribution));
            if (_tabStreamer != null)
                _tabStreamer.onClick.AddListener(() => SwitchTab(TabType.Streamer));

            if (_panel != null)
                _panel.SetActive(false);
        }

        private void Start()
        {
            if (_rows.Count == 0) CollectRows();

            if (_closeBtn != null)
            {
                _closeBtn.onClick.RemoveAllListeners();
                _closeBtn.onClick.AddListener(HidePanel);
            }

            if (_tabContribution != null)
            {
                _tabContribution.onClick.RemoveAllListeners();
                _tabContribution.onClick.AddListener(() => SwitchTab(TabType.Contribution));
            }
            if (_tabStreamer != null)
            {
                _tabStreamer.onClick.RemoveAllListeners();
                _tabStreamer.onClick.AddListener(() => SwitchTab(TabType.Streamer));
            }

            if (_panel != null && _panel.activeSelf)
                _panel.SetActive(false);

            TrySubscribe();
        }

        private void OnEnable()  { TrySubscribe(); }
        private void OnDisable() { Unsubscribe(); }
        private void OnDestroy() { Unsubscribe(); }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;
            sgm.OnWeeklyRankingReceived   += OnWeeklyRankingReceived;
            sgm.OnStreamerRankingReceived  += OnStreamerRankingReceived;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnWeeklyRankingReceived   -= OnWeeklyRankingReceived;
                sgm.OnStreamerRankingReceived  -= OnStreamerRankingReceived;
            }
            _subscribed = false;
        }

        // ==================== 事件回调 ====================

        private void OnWeeklyRankingReceived(WeeklyRankingData data)
        {
            _cachedWeekly = data;
            if (_panel != null && _panel.activeSelf && _currentTab == TabType.Contribution)
                RefreshContribution(data);
        }

        private void OnStreamerRankingReceived(StreamerRankingData data)
        {
            _cachedStreamer = data;
            if (_panel != null && _panel.activeSelf && _currentTab == TabType.Streamer)
                RefreshStreamer(data);
        }

        // ==================== 公开 API ====================

        public void ShowPanel()
        {
            if (_panel == null) return;
            if (_overlay != null) _overlay.SetActive(true);
            _panel.SetActive(true);

            SwitchTab(_currentTab);

            RequestWeeklyRanking();
            RequestStreamerRanking();
        }

        public void HidePanel()
        {
            if (_panel != null)
                _panel.SetActive(false);
            if (_overlay != null)
                _overlay.SetActive(false);
        }

        public void TogglePanel()
        {
            if (_panel == null) return;
            if (_panel.activeSelf) HidePanel();
            else ShowPanel();
        }

        // ==================== 页签切换 ====================

        private void SwitchTab(TabType tab)
        {
            _currentTab = tab;
            UpdateTabVisual();
            UpdateHeaderTexts(tab);

            if (tab == TabType.Contribution)
                RefreshContribution(_cachedWeekly);
            else
                RefreshStreamer(_cachedStreamer);
        }

        private void UpdateHeaderTexts(TabType tab)
        {
            if (_headerRow == null) return;
            var texts = _headerRow.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length >= 3)
            {
                texts[0].text = "名次";
                if (tab == TabType.Contribution)
                {
                    texts[1].text = "玩家";
                    texts[2].text = "贡献分";
                }
                else
                {
                    texts[1].text = "主播";
                    texts[2].text = "最佳记录";
                }
            }
        }

        private void UpdateTabVisual()
        {
            SetTabColor(_tabContribution, _currentTab == TabType.Contribution);
            SetTabColor(_tabStreamer,      _currentTab == TabType.Streamer);
        }

        private void SetTabColor(Button btn, bool active)
        {
            if (btn == null) return;
            // 切换背景 Sprite
            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                if (_tabActiveSprite != null && _tabInactiveSprite != null)
                {
                    img.sprite = active ? _tabActiveSprite : _tabInactiveSprite;
                    img.type   = Image.Type.Sliced;
                    img.color  = Color.white;
                }
                else
                {
                    // fallback：没有素材时用颜色区分
                    img.color = active ? new Color(0.1f, 0.15f, 0.3f, 0.9f) : new Color(0.06f, 0.1f, 0.18f, 0.8f);
                }
            }
            // 切换文字颜色
            var tmp = btn.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
                tmp.color = active ? TAB_ACTIVE_TEXT : TAB_INACTIVE_TEXT;
        }

        // ==================== 向服务器请求 ====================

        private void RequestWeeklyRanking()
        {
            var net = NetworkManager.Instance;
            if (net == null || !net.IsConnected) return;
            net.SendMessage("get_weekly_ranking");
        }

        private void RequestStreamerRanking()
        {
            var net = NetworkManager.Instance;
            if (net == null || !net.IsConnected) return;
            net.SendMessage("get_streamer_ranking");
        }

        // ==================== 贡献榜刷新 ====================

        private void RefreshContribution(WeeklyRankingData data)
        {
            bool hasData = data != null && data.rankings != null && data.rankings.Length > 0;

            if (_emptyHint != null)
            {
                _emptyHint.gameObject.SetActive(!hasData);
                if (!hasData) _emptyHint.text = "本周暂无数据";
            }

            if (_titleText != null)
                _titleText.text = "贡献排行榜";

            if (_subtitleText != null)
                _subtitleText.text = "每周一 00:00 重置";

            int rowCount = _rows.Count;
            for (int i = 0; i < rowCount; i++)
            {
                var row  = _rows[i];
                bool show = hasData && i < data.rankings.Length;
                row.SetActive(show);
                if (!show) continue;

                var entry = data.rankings[i];
                StyleRow(row, entry.rank);

                var texts = row.GetComponentsInChildren<TMP_Text>(true);
                // 跳过 MedalIcon 上的组件，只取 RankNum/PlayerName/Score
                var filteredTexts = new System.Collections.Generic.List<TMP_Text>();
                foreach (var t in texts)
                {
                    if (t.transform.parent == row.transform || t.transform.parent.name != "MedalIcon")
                        filteredTexts.Add(t);
                }

                var font = Resources.Load<TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF") ?? Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
                foreach (var t in filteredTexts)
                    if (t != null && font != null) t.font = font;

                if (filteredTexts.Count >= 3)
                {
                    filteredTexts[0].text = entry.rank <= 3 ? "" : $"{entry.rank}";
                    filteredTexts[1].text = TruncateName(entry.nickname, 7);
                    filteredTexts[2].text = entry.weeklyScore.ToString("N0");
                }
            }
        }

        // ==================== 主播榜刷新 ====================

        private void RefreshStreamer(StreamerRankingData data)
        {
            bool hasData = data != null && data.rankings != null && data.rankings.Length > 0;

            if (_emptyHint != null)
            {
                _emptyHint.gameObject.SetActive(!hasData);
                if (!hasData) _emptyHint.text = "暂无主播数据";
            }

            if (_titleText != null)
                _titleText.text = "主播排行榜";

            if (_subtitleText != null)
                _subtitleText.text = "历史最佳记录";

            int rowCount = _rows.Count;
            for (int i = 0; i < rowCount; i++)
            {
                var row  = _rows[i];
                bool show = hasData && i < data.rankings.Length;
                row.SetActive(show);
                if (!show) continue;

                var entry = data.rankings[i];
                StyleRow(row, entry.rank);

                var texts = row.GetComponentsInChildren<TMP_Text>(true);
                var filteredTexts = new System.Collections.Generic.List<TMP_Text>();
                foreach (var t in texts)
                {
                    if (t.transform.parent == row.transform || t.transform.parent.name != "MedalIcon")
                        filteredTexts.Add(t);
                }

                var font = Resources.Load<TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF") ?? Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
                foreach (var t in filteredTexts)
                    if (t != null && font != null) t.font = font;

                // audit-r5 §13.3 v1.26 字段：maxFortressDay / totalCycles / streamerKingTitle
                // （旧 maxDays / totalWins / totalGames / maxDifficulty 弃用，服务端 v1.26 已不写）
                string info = $"堡垒 D{entry.maxFortressDay} · 第{entry.totalCycles}局";

                // rank=1 且有堡垒之王称号 → 金色富文本前缀
                bool isKing = entry.rank == 1
                              && !string.IsNullOrEmpty(entry.streamerKingTitle)
                              && entry.streamerKingTitle == "堡垒之王";
                string nameDisplay = isKing
                    ? $"<color=#FFD700>【堡垒之王】</color>{TruncateName(entry.streamerName, 7)}"
                    : TruncateName(entry.streamerName, 7);

                if (filteredTexts.Count >= 3)
                {
                    filteredTexts[0].text = entry.rank <= 3 ? "" : $"{entry.rank}";
                    filteredTexts[1].text = nameDisplay;
                    filteredTexts[1].richText = true;
                    filteredTexts[2].text = info;
                }
            }
        }

        // ==================== 行样式 ====================

        private void CollectRows()
        {
            if (_rowContainer == null) return;
            _rows.Clear();
            foreach (Transform child in _rowContainer)
                _rows.Add(child.gameObject);
        }

        // ==================== 行样式 ====================

        /// <summary>设置行的背景颜色和奖牌图标（前三名特殊样式）</summary>
        private void StyleRow(GameObject row, int rank)
        {
            // 背景颜色
            var bg = row.GetComponent<Image>();
            if (bg != null)
            {
                switch (rank)
                {
                    case 1: bg.color = new Color(0.7f, 0.55f, 0.1f, 0.7f); break;   // 金色
                    case 2: bg.color = new Color(0.5f, 0.5f, 0.55f, 0.6f); break;    // 银色
                    case 3: bg.color = new Color(0.6f, 0.4f, 0.2f, 0.6f); break;     // 铜色
                    default: bg.color = new Color(0.12f, 0.16f, 0.28f, 0.45f); break; // 深蓝
                }
            }

            // 奖牌图标 — 查找或创建 MedalIcon 子对象
            var medalT = row.transform.Find("MedalIcon");
            var rankNumT = row.transform.Find("RankNum");

            if (rank <= 3 && GetMedalSprite(rank) != null)
            {
                // 显示奖牌图标，隐藏数字
                if (rankNumT != null)
                {
                    var rankTmp = rankNumT.GetComponent<TMP_Text>();
                    if (rankTmp != null) rankTmp.text = "";
                }

                if (medalT == null)
                {
                    // 动态创建奖牌图标
                    var medalGo = new GameObject("MedalIcon");
                    medalGo.transform.SetParent(row.transform, false);
                    medalGo.transform.SetAsFirstSibling();
                    var rt = medalGo.AddComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(36, 36);
                    // 放在行左侧
                    rt.anchorMin = new Vector2(0f, 0.5f);
                    rt.anchorMax = new Vector2(0f, 0.5f);
                    rt.pivot = new Vector2(0f, 0.5f);
                    rt.anchoredPosition = new Vector2(8f, 0f);
                    var img = medalGo.AddComponent<Image>();
                    img.sprite = GetMedalSprite(rank);
                    img.preserveAspect = true;
                    img.raycastTarget = false;
                    var le = medalGo.AddComponent<UnityEngine.UI.LayoutElement>();
                    le.preferredWidth = 36;
                    le.preferredHeight = 36;
                    le.flexibleWidth = 0;
                    medalT = medalGo.transform;
                }
                else
                {
                    medalT.gameObject.SetActive(true);
                    var img = medalT.GetComponent<Image>();
                    if (img != null) img.sprite = GetMedalSprite(rank);
                }
            }
            else
            {
                // 4名以后，隐藏奖牌
                if (medalT != null) medalT.gameObject.SetActive(false);
            }
        }

        private Sprite GetMedalSprite(int rank)
        {
            switch (rank)
            {
                case 1: return _medalGold;
                case 2: return _medalSilver;
                case 3: return _medalBronze;
                default: return null;
            }
        }

        // ==================== 工具 ====================

        private string TruncateName(string name, int maxLen)
        {
            if (string.IsNullOrEmpty(name)) return "匿名";
            return name.Length <= maxLen ? name : name.Substring(0, maxLen) + "..";
        }

        private string TranslateDifficulty(string diff)
        {
            switch (diff)
            {
                case "hard": return "困难";
                case "hell": return "地狱";
                default:     return "普通";
            }
        }

        private string FormatSubtitle(string week, long resetAtMs)
        {
            string weekNum = week ?? "";
            int wIdx = weekNum.IndexOf('-');
            if (wIdx >= 0) weekNum = weekNum.Substring(wIdx + 2);
            else weekNum = weekNum.Replace("W", "");

            var nowMs  = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var diffMs = resetAtMs - nowMs;
            string resetStr;
            if (diffMs <= 0)
                resetStr = "即将重置";
            else
            {
                long diffHours = diffMs / (1000L * 60 * 60);
                if (diffHours >= 24)
                    resetStr = $"{diffHours / 24}天后重置";
                else if (diffHours >= 1)
                    resetStr = $"{diffHours}小时后重置";
                else
                    resetStr = "即将重置";
            }

            return $"第{weekNum}周  ·  {resetStr}";
        }
    }
}
