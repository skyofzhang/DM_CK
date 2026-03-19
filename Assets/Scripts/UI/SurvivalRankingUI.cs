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
    /// 生存游戏排行榜 UI — 本周贡献榜
    ///
    /// 设计：
    ///   - 大厅"排行榜"按钮打开，右上角✕关闭
    ///   - 显示本周累计 Top 10 贡献者（跨多局累积）
    ///   - 每周一 00:00 UTC+8 自动重置
    ///   - 数据来源：服务器 WeeklyRankingStore（真实持久化数据）
    ///   - 面板打开时主动向服务器请求最新数据
    ///   - 每局结束后服务器自动推送更新
    ///
    /// 挂载规则（Rule #7）：
    ///   脚本挂载在 Canvas（始终激活）；
    ///   面板根节点 _panel 初始 inactive，通过 ShowPanel()/HidePanel() 控制。
    ///
    /// Inspector 必填字段：
    ///   _panel          — 排行榜面板根节点（全屏，初始 inactive）
    ///   _closeBtn       — 右上角关闭按钮
    ///   _titleText      — 标题 TMP（显示"本周贡献榜"）
    ///   _subtitleText   — 副标题 TMP（显示周次+重置倒计时，可为 null）
    ///   _rowContainer   — 排名行父节点（Vertical Layout Group）
    ///   _emptyHint      — 暂无数据提示文字（无数据时显示）
    ///
    /// 预创建行 TMP 顺序（按 sibling index）：
    ///   texts[0] = 名次  "#1"
    ///   texts[1] = 玩家名
    ///   texts[2] = 本周贡献值
    /// </summary>
    public class SurvivalRankingUI : MonoBehaviour
    {
        [Header("面板")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button     _closeBtn;
        [SerializeField] private TMP_Text   _titleText;
        [SerializeField] private TMP_Text   _subtitleText;  // 可选：显示"第X周 · X天后重置"

        [Header("列表区域")]
        [SerializeField] private Transform  _rowContainer;
        [SerializeField] private TMP_Text   _emptyHint;

        // ——— 预创建行缓存 ———
        private readonly List<GameObject> _rows = new List<GameObject>();

        // ——— 最新周榜缓存（服务器推送后更新）———
        private WeeklyRankingData _cachedData;

        private bool _subscribed;

        // ==================== 生命周期 ====================

        private void Awake()
        {
            CollectRows();

            if (_closeBtn != null)
                _closeBtn.onClick.AddListener(HidePanel);

            if (_panel != null)
                _panel.SetActive(false);
        }

        private void Start()
        {
            if (_rows.Count == 0) CollectRows();

            // 确保关闭按钮绑定
            if (_closeBtn != null)
            {
                _closeBtn.onClick.RemoveAllListeners();
                _closeBtn.onClick.AddListener(HidePanel);
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
            sgm.OnWeeklyRankingReceived += OnWeeklyRankingReceived;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
                sgm.OnWeeklyRankingReceived -= OnWeeklyRankingReceived;
            _subscribed = false;
        }

        // ==================== 事件回调 ====================

        /// <summary>服务器推送周榜（局结束自动推送 / 主动请求回应）</summary>
        private void OnWeeklyRankingReceived(WeeklyRankingData data)
        {
            _cachedData = data;
            // 面板当前打开时实时刷新
            if (_panel != null && _panel.activeSelf)
                RefreshRows(data);
        }

        // ==================== 公开 API ====================

        /// <summary>打开排行榜面板，并主动向服务器请求最新周榜</summary>
        public void ShowPanel()
        {
            if (_panel == null) return;
            _panel.SetActive(true);

            // 先用缓存数据填充（避免空白闪烁）
            RefreshRows(_cachedData);

            // 向服务器请求最新数据（异步刷新）
            RequestWeeklyRanking();
        }

        /// <summary>关闭排行榜</summary>
        public void HidePanel()
        {
            if (_panel != null)
                _panel.SetActive(false);
        }

        /// <summary>切换显示/隐藏</summary>
        public void TogglePanel()
        {
            if (_panel == null) return;
            if (_panel.activeSelf) HidePanel();
            else ShowPanel();
        }

        // ==================== 内部：向服务器请求 ====================

        private void RequestWeeklyRanking()
        {
            var net = NetworkManager.Instance;
            if (net == null || !net.IsConnected)
            {
                Debug.LogWarning("[SurvivalRankingUI] NetworkManager 未连接，无法请求周榜");
                return;
            }
            net.SendMessage("get_weekly_ranking");
        }

        // ==================== 内部：刷新显示 ====================

        private void CollectRows()
        {
            if (_rowContainer == null) return;
            _rows.Clear();
            foreach (Transform child in _rowContainer)
                _rows.Add(child.gameObject);
        }

        private void RefreshRows(WeeklyRankingData data)
        {
            bool hasData = data != null && data.rankings != null && data.rankings.Length > 0;

            // ── 空提示 ──
            if (_emptyHint != null)
            {
                _emptyHint.gameObject.SetActive(!hasData);
                if (!hasData)
                    _emptyHint.text = "本周暂无数据";
            }

            // ── 标题 ──
            if (_titleText != null)
                _titleText.text = "本周贡献榜";

            // ── 副标题：第X周 · X天后重置 ──
            if (_subtitleText != null)
            {
                if (data != null)
                    _subtitleText.text = FormatSubtitle(data.week, data.resetAt);
                else
                    _subtitleText.text = "每周一 00:00 重置";
            }

            // ── 填充行 ──
            int rowCount = _rows.Count;
            for (int i = 0; i < rowCount; i++)
            {
                var row  = _rows[i];
                bool show = hasData && i < data.rankings.Length;
                row.SetActive(show);
                if (!show) continue;

                var entry = data.rankings[i];
                var texts = row.GetComponentsInChildren<TMP_Text>(true);

                // 绑定中文字体（防乱码）
                var font = Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
                foreach (var t in texts)
                    if (t != null && font != null) t.font = font;

                if (texts.Length >= 3)
                {
                    texts[0].text = $"#{entry.rank}";
                    texts[1].text = TruncateName(entry.nickname, 7);
                    texts[2].text = entry.weeklyScore.ToString("N0");
                }
                else if (texts.Length == 2)
                {
                    texts[0].text = $"#{entry.rank}  {TruncateName(entry.nickname, 6)}";
                    texts[1].text = entry.weeklyScore.ToString("N0");
                }
                else if (texts.Length == 1)
                {
                    texts[0].text = $"{entry.rank}. {TruncateName(entry.nickname, 5)}  {entry.weeklyScore}";
                }
            }
        }

        // ==================== 工具 ====================

        private string TruncateName(string name, int maxLen)
        {
            if (string.IsNullOrEmpty(name)) return "匿名";
            return name.Length <= maxLen ? name : name.Substring(0, maxLen) + "..";
        }

        /// <summary>生成副标题，例如："第12周 · 3天后重置"</summary>
        private string FormatSubtitle(string week, long resetAtMs)
        {
            // 解析周数字
            string weekNum = week ?? "";
            int wIdx = weekNum.IndexOf('-');
            if (wIdx >= 0) weekNum = weekNum.Substring(wIdx + 2); // "W12" → remove W
            else weekNum = weekNum.Replace("W", "");

            // 计算距重置天数
            var nowMs    = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var diffMs   = resetAtMs - nowMs;
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
