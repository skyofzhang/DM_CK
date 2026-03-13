using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// 生存游戏排行榜 UI（全屏独立面板）
    ///
    /// 设计：
    ///   - 大厅"排行榜"按钮打开，左上角✕关闭
    ///   - 显示最近一局 Top 10 贡献者
    ///   - 若暂无数据则显示提示文字"暂无本场数据"
    ///
    /// 挂载规则（Rule #7）：
    ///   脚本挂载在 Canvas（始终激活）；
    ///   面板根节点 _panel 初始 inactive，通过 ShowPanel()/HidePanel() 控制。
    ///
    /// Inspector 必填字段：
    ///   _panel          — 排行榜面板根节点（全屏，初始 inactive）
    ///   _closeBtn       — 右上角关闭按钮
    ///   _rowContainer   — 排名行父节点（Vertical Layout Group）
    ///   _emptyHint      — "暂无本场数据"提示文字（无数据时显示）
    ///   _rowTemplate    — 预创建的行模板（包含 3 个 TMP: 名次/名称/贡献值）
    ///                     必须在 Scene 中预创建 10 个，均以 _rowTemplate 命名前缀无所谓，
    ///                     只要是 _rowContainer 的直接子对象即可。
    ///
    /// 行模板 TMP 顺序（GetComponentsInChildren 按 sibling index）：
    ///   texts[0] = 名次  "#1"
    ///   texts[1] = 玩家名
    ///   texts[2] = 贡献值
    /// </summary>
    public class SurvivalRankingUI : MonoBehaviour
    {
        [Header("面板")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button     _closeBtn;
        [SerializeField] private TMP_Text   _titleText;

        [Header("列表区域")]
        [SerializeField] private Transform  _rowContainer;  // 行父节点（预创建好 10 行子对象）
        [SerializeField] private TMP_Text   _emptyHint;     // "暂无本场数据"

        // ——— 缓存预创建行 ———
        private readonly List<GameObject> _rows = new List<GameObject>();

        // ——— 最近一局排行数据缓存（由结算消息写入）———
        private List<RankEntry> _lastRankings;

        // ==================== 生命周期 ====================

        private void Awake()
        {
            // 收集预创建行（若_rowContainer已绑定则立即收集，否则在Start延迟收集）
            CollectRows();

            if (_closeBtn != null)
                _closeBtn.onClick.AddListener(HidePanel);

            if (_panel != null)
                _panel.SetActive(false);
        }

        private void Start()
        {
            // Start时再次收集行（解决Editor脚本AddComponent后再绑定rowContainer的情况）
            if (_rows.Count == 0)
                CollectRows();

            // 确保关闭按钮已绑定（Awake时_closeBtn可能还未通过SerializedObject绑定）
            if (_closeBtn != null)
            {
                _closeBtn.onClick.RemoveAllListeners();
                _closeBtn.onClick.AddListener(HidePanel);
            }

            // 确保Panel默认inactive
            if (_panel != null && _panel.activeSelf)
                _panel.SetActive(false);

            // 订阅结算事件，缓存每局排行数据
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
                sgm.OnGameEnded += OnGameEnded;
        }

        /// <summary>从_rowContainer收集预创建行（可多次调用，重复不影响）</summary>
        private void CollectRows()
        {
            if (_rowContainer == null) return;
            _rows.Clear();
            foreach (Transform child in _rowContainer)
                _rows.Add(child.gameObject);
        }

        private void OnDestroy()
        {
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
                sgm.OnGameEnded -= OnGameEnded;
        }

        // ==================== 数据注入 ====================

        private void OnGameEnded(SurvivalGameEndedData data)
        {
            if (data.rankings == null || data.rankings.Length == 0) return;

            _lastRankings = new List<RankEntry>(data.rankings.Length);
            foreach (var r in data.rankings)
                _lastRankings.Add(new RankEntry
                {
                    Nickname = string.IsNullOrEmpty(r.playerName) ? r.playerId : r.playerName,
                    Score    = Mathf.RoundToInt(r.contribution)
                });
        }

        // ==================== 公开 API ====================

        /// <summary>打开排行榜（使用缓存数据刷新）</summary>
        public void ShowPanel()
        {
            if (_panel == null) return;
            _panel.SetActive(true);
            RefreshRows(_lastRankings);
        }

        /// <summary>外部注入数据后打开（如从结算UI跳转）</summary>
        public void ShowPanel(List<RankEntry> rankings)
        {
            _lastRankings = rankings;
            ShowPanel();
        }

        /// <summary>切换显示/隐藏</summary>
        public void TogglePanel()
        {
            if (_panel == null) return;
            if (_panel.activeSelf) HidePanel();
            else ShowPanel();
        }

        /// <summary>关闭排行榜</summary>
        public void HidePanel()
        {
            if (_panel != null)
                _panel.SetActive(false);
        }

        // ==================== 内部刷新 ====================

        private void RefreshRows(List<RankEntry> rankings)
        {
            bool hasData = rankings != null && rankings.Count > 0;

            // 空提示
            if (_emptyHint != null)
                _emptyHint.gameObject.SetActive(!hasData);

            // 标题
            if (_titleText != null)
                _titleText.text = hasData ? "本场贡献榜 TOP 10" : "排行榜";

            // 清空/填充行
            int rowCount = _rows.Count;
            for (int i = 0; i < rowCount; i++)
            {
                var row = _rows[i];
                bool show = hasData && i < rankings.Count;
                row.SetActive(show);

                if (!show) continue;

                var texts = row.GetComponentsInChildren<TMP_Text>(true);
                if (texts.Length >= 3)
                {
                    texts[0].text = $"#{i + 1}";
                    texts[1].text = rankings[i].Nickname;
                    texts[2].text = rankings[i].Score.ToString("N0");
                }
                else if (texts.Length == 2)
                {
                    texts[0].text = $"#{i + 1}  {rankings[i].Nickname}";
                    texts[1].text = rankings[i].Score.ToString("N0");
                }
            }
        }
    }
}
