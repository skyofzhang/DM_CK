using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DrscfZ.Systems;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// 白天物资贡献排行榜 — 游戏顶部显示，3列（食物/煤炭/矿石），每列 Top3
    ///
    /// 挂载规则（Rule #7）：
    ///   挂在 Canvas/GameUIPanel/ResourceRankPanel（始终激活）。
    ///   面板本身通过 SetActive(true/false) 控制显隐（仅在 Running 白天阶段显示）。
    ///
    /// Inspector 必填：
    ///   _panel        — ResourceRankPanel 根节点
    ///   _foodTitle / _coalTitle / _oreTitle — 3列标题 TMP
    ///   _foodRows[3] / _coalRows[3] / _oreRows[3] — 每列 3 行 TMP
    /// </summary>
    public class ResourceRankUI : MonoBehaviour
    {
        public static ResourceRankUI Instance { get; private set; }

        [Header("面板根节点（控制显隐）")]
        [SerializeField] private GameObject _panel;

        [Header("列标题 TMP")]
        [SerializeField] private TextMeshProUGUI _foodTitle;
        [SerializeField] private TextMeshProUGUI _coalTitle;
        [SerializeField] private TextMeshProUGUI _oreTitle;

        [Header("食物列 Top3 TMP")]
        [SerializeField] private TextMeshProUGUI[] _foodRows = new TextMeshProUGUI[3];

        [Header("煤炭列 Top3 TMP")]
        [SerializeField] private TextMeshProUGUI[] _coalRows = new TextMeshProUGUI[3];

        [Header("矿石列 Top3 TMP")]
        [SerializeField] private TextMeshProUGUI[] _oreRows = new TextMeshProUGUI[3];

        [Header("刷新间隔（秒）")]
        [SerializeField] private float _refreshInterval = 5f;

        private float _refreshTimer = 0f;
        private bool  _visible      = false;
        private bool  _subscribed   = false;

        // ==================== 生命周期 ====================

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // 初始隐藏面板
            if (_panel != null) _panel.SetActive(false);
        }

        private void Start()
        {
            BindFonts();
            SetTitles();
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
            sgm.OnStateChanged += OnStateChanged;
            _subscribed = true;
            // 订阅后立即检查当前状态（可能已经是 Running）
            OnStateChanged(sgm.State);
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null) sgm.OnStateChanged -= OnStateChanged;
            _subscribed = false;
        }

        private void Update()
        {
            // 订阅重试（Start() 时 Instance 可能尚未就绪）
            if (!_subscribed) TrySubscribe();

            if (!_visible) return;

            _refreshTimer += Time.deltaTime;
            if (_refreshTimer >= _refreshInterval)
            {
                _refreshTimer = 0f;
                Refresh();
            }
        }

        // ==================== 状态响应 ====================

        private void OnStateChanged(SurvivalGameManager.SurvivalState state)
        {
            // 仅在 Running 状态显示（夜晚也暂隐，可根据需求调整）
            bool show = state == SurvivalGameManager.SurvivalState.Running;
            SetVisible(show);
            if (show)
            {
                _refreshTimer = 0f;
                Refresh();
            }
        }

        private void SetVisible(bool visible)
        {
            _visible = visible;
            if (_panel != null) _panel.SetActive(visible);
        }

        // ==================== 刷新显示 ====================

        /// <summary>外部可主动调用强制刷新</summary>
        public void Refresh()
        {
            RefreshColumn(_foodRows, "food");
            RefreshColumn(_coalRows, "coal");
            RefreshColumn(_oreRows,  "ore");
        }

        private void RefreshColumn(TextMeshProUGUI[] rows, string resourceType)
        {
            List<(string playerId, string playerName, int amount)> top = null;
            var rankSys = RankingSystem.Instance;
            if (rankSys != null)
                top = rankSys.GetTopByResource(resourceType, 3);

            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == null) continue;
                if (top != null && i < top.Count)
                    rows[i].text = $"{i + 1}. {TruncateName(top[i].playerName, 6)} {top[i].amount}";
                else
                    rows[i].text = $"{i + 1}. —";
            }
        }

        // ==================== 初始化工具 ====================

        private void SetTitles()
        {
            SetTmp(_foodTitle, "食物贡献");
            SetTmp(_coalTitle, "煤炭贡献");
            SetTmp(_oreTitle,  "矿石贡献");
        }

        private void BindFonts()
        {
            var font    = Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
            var fontMat = Resources.Load<Material>("Fonts/ChineseFont SDF - Outline");

            BindTmp(_foodTitle, font, fontMat);
            BindTmp(_coalTitle, font, fontMat);
            BindTmp(_oreTitle,  font, fontMat);

            foreach (var t in _foodRows) BindTmp(t, font, fontMat);
            foreach (var t in _coalRows) BindTmp(t, font, fontMat);
            foreach (var t in _oreRows)  BindTmp(t, font, fontMat);
        }

        private static void SetTmp(TextMeshProUGUI tmp, string text)
        {
            if (tmp == null) return;
            tmp.text = text;
            var font = Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
            if (font != null) tmp.font = font;
            var mat = Resources.Load<Material>("Fonts/ChineseFont SDF - Outline");
            if (mat != null) tmp.fontSharedMaterial = mat;
        }

        private static void BindTmp(TextMeshProUGUI tmp, TMP_FontAsset font, Material fontMat)
        {
            if (tmp == null) return;
            if (font    != null) tmp.font               = font;
            if (fontMat != null) tmp.fontSharedMaterial = fontMat;
        }

        private static string TruncateName(string name, int maxLen)
        {
            if (string.IsNullOrEmpty(name)) return "匿名";
            return name.Length <= maxLen ? name : name.Substring(0, maxLen) + "..";
        }
    }
}
