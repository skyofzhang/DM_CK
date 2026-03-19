using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DrscfZ.Survival;
using DrscfZ.Systems;

namespace DrscfZ.UI
{
    /// <summary>
    /// 生存结算UI - 3屏序列: A(结果) → B(数据) → C(MVP/Top3)
    /// 挂载到 Canvas/SurvivalSettlementPanel（初始 inactive）
    ///
    /// 数据来源：SurvivalGameManager.HandleGameEnded → ShowSettlement(SettlementData)
    /// Rankings 由 RankingSystem.GetTopN(3) 在序列开始前自动注入。
    ///
    /// C 屏 Top3 显示规则：
    ///   - _top3Slots 需在 Inspector 中拖拽赋值 3 个预创建的 GameObject（每个至少包含 2 个 TextMeshProUGUI：名字+积分）
    ///   - 若参与人数 &lt; 3，多余槽位自动隐藏（SetActive false）
    ///   - 若槽位引用为 null，输出 LogError 并降级为旧版 MVP 单行显示
    /// </summary>
    public class SurvivalSettlementUI : MonoBehaviour
    {
        [Header("Screen A - Result (3s)")]
        [SerializeField] private GameObject _screenA;
        [SerializeField] private TextMeshProUGUI _resultTitleText;
        [SerializeField] private TextMeshProUGUI _resultSubtitleText;

        [Header("Screen B - Stats (5s)")]
        [SerializeField] private GameObject _screenB;
        [SerializeField] private TextMeshProUGUI _survivalDaysText;
        [SerializeField] private TextMeshProUGUI _totalKillsText;
        [SerializeField] private TextMeshProUGUI _totalGatherText;
        [SerializeField] private TextMeshProUGUI _totalRepairText;
        [SerializeField] private Transform _rankingListParent;
        [SerializeField] private GameObject _rankEntryPrefab; // pre-created in scene, unused at runtime

        [Header("Screen C - Top3 (3s)")]
        [SerializeField] private GameObject _screenC;
        /// <summary>
        /// 3 个预创建的排名槽位 GameObject（索引 0=第1名，1=第2名，2=第3名）。
        /// 每个 GameObject 至少包含 2 个 TextMeshProUGUI 子组件：texts[0]=名字, texts[1]=积分。
        /// 若参与人数不足，多余槽位会被 SetActive(false)。
        /// ⚠ 请务必在 Inspector 中赋值，否则将退化为旧版 MVP 单行显示并输出 LogError。
        /// </summary>
        [SerializeField] private GameObject[] _top3Slots = new GameObject[3];
        /// <summary>MVP 横幅文字（"本局MVP是 XXX，感谢TA的付出！"）</summary>
        [SerializeField] private TextMeshProUGUI _mvpAnchorLineText;

        // ─── 旧版 MVP 单行字段（保留供降级显示，不再是主路径）──────────────
        [SerializeField] private TextMeshProUGUI _mvpNameText;
        [SerializeField] private TextMeshProUGUI _mvpScoreText;

        [Header("Ranking System (auto-inject Top3)")]
        [SerializeField] private RankingSystem _rankingSystem;

        [Header("Restart / Actions")]
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _btnViewRanking;  // "查看英雄榜"

        // Pre-collected from the RankingList hierarchy (populated in Awake)
        private List<GameObject> _rankEntries = new List<GameObject>();

        private void Awake()
        {
            if (_restartButton != null)
                _restartButton.onClick.AddListener(OnRestartClicked);
            if (_btnViewRanking != null)
                _btnViewRanking.onClick.AddListener(OnViewRankingClicked);

            // Collect pre-created rank entry GameObjects (B screen)
            if (_rankingListParent != null)
            {
                foreach (Transform child in _rankingListParent)
                    _rankEntries.Add(child.gameObject);
            }

            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            // 订阅由SurvivalGameManager直接调用ShowSettlement代替
        }

        private void OnDisable()
        {
            // 订阅由SurvivalGameManager直接调用ShowSettlement代替
        }

        // ─── Public API: inject settlement data directly ──────────────────────

        public void ShowSettlement(SettlementData data)
        {
            gameObject.SetActive(true);
            StartCoroutine(PlaySettlementSequence(data));
        }

        // ─── Sequence coroutine ───────────────────────────────────────────────

        private IEnumerator PlaySettlementSequence(SettlementData data)
        {
            // 序列开始时隐藏重新开始按钮，防止误触
            if (_restartButton != null) _restartButton.gameObject.SetActive(false);

            // ── Rankings 注入：若外部未提供，从 RankingSystem 拉取 Top3 ──────
            if ((data.Rankings == null || data.Rankings.Count == 0) && _rankingSystem != null)
            {
                var top3 = _rankingSystem.GetTopN(3);
                if (top3 != null && top3.Count > 0)
                {
                    data.Rankings = new List<RankEntry>(top3.Count);
                    foreach (var c in top3)
                        data.Rankings.Add(new RankEntry { Nickname = c.Nickname, Score = c.Score });
                }
            }

            if (data.Rankings == null || data.Rankings.Count == 0)
                Debug.LogWarning("[SurvivalSettlementUI] Rankings 为空：RankingSystem 无本场数据（本局是否有玩家参与？），C 屏将跳过。");

            ShowScreenA(data);
            yield return new WaitForSecondsRealtime(3f);

            ShowScreenB(data);
            yield return new WaitForSecondsRealtime(5f);

            if (data.Rankings != null && data.Rankings.Count > 0)
            {
                ShowScreenC(data);
                yield return new WaitForSecondsRealtime(3f);
            }

            // 序列播完后停留在最后一屏，显示"重新开始"按钮等待玩家操作
            if (_restartButton != null) _restartButton.gameObject.SetActive(true);
            Debug.Log("[SurvivalSettlementUI] 结算序列播完，等待玩家点击重新开始");
        }

        // ─── Screen A: result title ───────────────────────────────────────────

        private void ShowScreenA(SettlementData data)
        {
            _screenA.SetActive(true);
            _screenB.SetActive(false);
            _screenC.SetActive(false);

            if (data.IsVictory)
            {
                _resultTitleText.text    = "极地已守护!";
                _resultTitleText.color   = new Color(1f, 0.85f, 0.1f); // gold
                _resultSubtitleText.text = $"坚守了 {data.SurvivalDays} 天";
            }
            else
            {
                _resultTitleText.text = data.FailReason switch
                {
                    "food"        => "极地陷落 — 食物耗尽",
                    "temperature" => "极地陷落 — 冻死冰原",
                    "gate"        => "极地陷落 — 城门攻破",
                    _             => "极地陷落"
                };
                _resultTitleText.color   = new Color(0.9f, 0.2f, 0.2f); // red
                _resultSubtitleText.text = $"坚守了 {data.SurvivalDays} 天";
            }
        }

        // ─── Screen B: stats + ranking ────────────────────────────────────────

        private void ShowScreenB(SettlementData data)
        {
            _screenA.SetActive(false);
            _screenB.SetActive(true);
            _screenC.SetActive(false);

            if (_survivalDaysText) _survivalDaysText.text = $"生存天数: {data.SurvivalDays}";
            if (_totalKillsText)   _totalKillsText.text   = $"总击杀: {data.TotalKills}";
            if (_totalGatherText)  _totalGatherText.text  = $"总采集: {data.TotalGather}";
            if (_totalRepairText)  _totalRepairText.text  = $"总修墙: {data.TotalRepair}";

            // Hide all pre-created entries, then reveal as needed
            foreach (var entry in _rankEntries) entry.SetActive(false);

            if (data.Rankings != null)
            {
                int count = Mathf.Min(data.Rankings.Count, _rankEntries.Count);
                for (int i = 0; i < count; i++)
                {
                    var entry = _rankEntries[i];
                    entry.SetActive(true);
                    var texts = entry.GetComponentsInChildren<TextMeshProUGUI>();
                    if (texts.Length >= 3)
                    {
                        texts[0].text = $"#{i + 1}";
                        texts[1].text = data.Rankings[i].Nickname;
                        texts[2].text = data.Rankings[i].Score.ToString();
                    }
                }
            }
        }

        // ─── Screen C: Top3 贡献者 ────────────────────────────────────────────

        private void ShowScreenC(SettlementData data)
        {
            _screenA.SetActive(false);
            _screenB.SetActive(false);
            _screenC.SetActive(true);

            // MVP 横幅（第1名姓名）—— 始终更新，不依赖 top3Slots 是否绑定
            var mvp = data.Rankings[0];
            if (_mvpAnchorLineText)
                _mvpAnchorLineText.text = $"本局MVP是 {mvp.Nickname}，感谢TA的付出！";
            if (_mvpNameText)  _mvpNameText.text  = mvp.Nickname;
            if (_mvpScoreText) _mvpScoreText.text = $"贡献值: {mvp.Score}";

            // Top3 槽位完整性校验
            bool slotsValid = _top3Slots != null && _top3Slots.Length >= 3;
            if (!slotsValid)
            {
                Debug.LogWarning("[SurvivalSettlementUI] _top3Slots 未配置，跳过 Top3 显示");
                return;
            }

            // 填充 Top3 槽位（超出参与人数的槽位隐藏）
            for (int i = 0; i < 3; i++)
            {
                var slot = _top3Slots[i];
                if (slot == null) continue;

                bool hasData = i < data.Rankings.Count;
                slot.SetActive(hasData);
                if (!hasData) continue;

                // 按名字查找子组件，避免因 TMP 数量/顺序不同导致下标错位
                var nameComp  = slot.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
                var scoreComp = slot.transform.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();

                if (nameComp  != null) nameComp.text  = data.Rankings[i].Nickname;
                if (scoreComp != null) scoreComp.text = $"贡献值: {data.Rankings[i].Score}";
            }
        }

        // ─── Restart ─────────────────────────────────────────────────────────

        private void OnRestartClicked()
        {
            gameObject.SetActive(false);
            SurvivalGameManager.Instance?.RequestResetGame();
        }

        // ─── View Ranking ─────────────────────────────────────────────────────

        private void OnViewRankingClicked()
        {
            // 与大厅"排行榜"按钮打开同一个面板：SurvivalRankingUI.ShowPanel()
            var rankingUI = FindObjectOfType<SurvivalRankingUI>(true);
            if (rankingUI != null)
            {
                rankingUI.ShowPanel();
                // 置顶，确保显示在结算面板之上
                rankingUI.transform.SetAsLastSibling();
                Debug.Log("[SurvivalSettlementUI] 打开本周贡献榜");
            }
            else
            {
                Debug.LogWarning("[SurvivalSettlementUI] 找不到 SurvivalRankingUI，请确认场景中存在该组件");
            }
        }
    }

    // ==================== Data Classes ====================

    /// <summary>
    /// 结算面板所需数据（从 SurvivalGameEndedData 映射而来）。
    /// Rankings 由 PlaySettlementSequence 从 RankingSystem.GetTopN(3) 自动注入，
    /// 也可由外部显式提供。
    /// </summary>
    [System.Serializable]
    public class SettlementData
    {
        public bool   IsVictory;
        public string FailReason;   // "food" | "temperature" | "gate"
        public int    SurvivalDays;
        public int    TotalKills;
        public int    TotalGather;
        public int    TotalRepair;
        public List<RankEntry> Rankings; // null = 由 PlaySettlementSequence 自动从 RankingSystem 获取
    }

    [System.Serializable]
    public class RankEntry
    {
        public string Nickname;
        public int    Score;
    }
}
