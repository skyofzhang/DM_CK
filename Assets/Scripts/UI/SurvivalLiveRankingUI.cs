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
    /// 实时贡献榜 — 游戏进行时右侧显示 Top5 守护者
    ///
    /// 功能：
    ///   - 每当有 work_command 或 gift 事件时，3秒内刷新一次排名显示
    ///   - 仅在 Running 状态下显示
    ///   - 显示 Top5 贡献者（昵称 + 贡献值）
    ///   - 名次变化时播放轻微动画
    ///
    /// 挂载规则（Rule #7）：
    ///   挂在 Canvas（始终激活）。
    ///   面板根节点 _panel（Canvas/GameUIPanel/LiveRankingPanel）初始 inactive。
    ///
    /// Inspector 必填：
    ///   _panel        — LiveRankingPanel 根节点（初始 inactive）
    ///   _rankRows     — 5 个预创建的行 GameObject（每个含 3 个 TMP：名次/名称/分数）
    ///   _titleText    — 标题 TMP（"守护者榜"）
    ///   _rankingSystem — RankingSystem 引用
    /// </summary>
    public class SurvivalLiveRankingUI : MonoBehaviour
    {
        [Header("面板")]
        [SerializeField] private GameObject    _panel;
        [SerializeField] private TMP_Text      _titleText;

        [Header("排名行（预创建5个，Row含3个TMP: 名次/名称/贡献值）")]
        [SerializeField] private GameObject[]  _rankRows = new GameObject[5];

        [Header("依赖")]
        [SerializeField] private RankingSystem _rankingSystem;

        [Header("刷新频率")]
        [SerializeField] private float _refreshInterval = 3f;  // 秒

        // 内部状态
        private bool  _dirty        = false;   // 有新事件待刷新
        private float _refreshTimer = 0f;
        private bool  _visible      = false;
        private bool  _subscribed   = false;

        // 上一帧 Top5（用于检测排名变化，触发动画）
        private string[] _prevRankIds = new string[5];

        // ==================== 生命周期 ====================

        private void Awake()
        {
            if (_panel != null)
                _panel.SetActive(false);
        }

        private void Start()
        {
            TrySubscribe();
            // 绑定标题字体，防止中文乱码
            if (_titleText != null)
            {
                var chineseFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts/ChineseFont SDF");
                if (chineseFont != null) _titleText.font = chineseFont;
            }
        }

        private void OnEnable()  { TrySubscribe(); }
        private void OnDisable() { Unsubscribe(); }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;

            sgm.OnStateChanged  += OnStateChanged;
            sgm.OnWorkCommand   += OnWorkCommand;
            sgm.OnGiftReceived  += OnGiftReceived;
            sgm.OnPlayerJoined  += OnPlayerJoined;

            // 找不到 RankingSystem 时尝试自动查找
            if (_rankingSystem == null)
                _rankingSystem = FindObjectOfType<RankingSystem>();

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnStateChanged -= OnStateChanged;
                sgm.OnWorkCommand  -= OnWorkCommand;
                sgm.OnGiftReceived -= OnGiftReceived;
                sgm.OnPlayerJoined -= OnPlayerJoined;
            }
            _subscribed = false;
        }

        private void Update()
        {
            if (!_dirty) return;

            _refreshTimer -= Time.deltaTime;
            if (_refreshTimer <= 0f)
            {
                _dirty = false;
                _refreshTimer = _refreshInterval;
                RefreshDisplay();
            }
        }

        // ==================== 事件回调 ====================

        private void OnStateChanged(SurvivalGameManager.SurvivalState state)
        {
            bool isRunning = state == SurvivalGameManager.SurvivalState.Running;
            SetVisible(isRunning);
            if (isRunning)
            {
                // 强制立即刷新一次
                RefreshDisplay();
            }
        }

        private void OnWorkCommand(WorkCommandData _)  => MarkDirty();
        private void OnGiftReceived(SurvivalGiftData _) => MarkDirty();
        private void OnPlayerJoined(SurvivalPlayerJoinedData _) => MarkDirty();

        private void MarkDirty()
        {
            _dirty = true;
            if (_refreshTimer <= 0f) _refreshTimer = _refreshInterval;
        }

        // ==================== 显示控制 ====================

        private void SetVisible(bool visible)
        {
            _visible = visible;
            if (_panel != null)
                _panel.SetActive(visible);
        }

        // ==================== 刷新显示 ====================

        private void RefreshDisplay()
        {
            if (!_visible || _rankingSystem == null) return;

            var top5 = _rankingSystem.GetTopN(5);

            // 更新标题
            if (_titleText != null)
                _titleText.text = top5.Count > 0 ? "守护者榜" : "守护者榜";

            // 隐藏所有行，再按需展示
            int rowCount = _rankRows != null ? _rankRows.Length : 0;
            for (int i = 0; i < rowCount; i++)
            {
                var row = _rankRows[i];
                bool show = i < top5.Count;
                if (row != null) row.SetActive(show);
                if (!show) continue;

                var contributor = top5[i];

                // 检测排名变化（动画触发）
                bool rankChanged = _prevRankIds[i] != contributor.PlayerId;
                _prevRankIds[i] = contributor.PlayerId;

                // 填充文字（期望每行有 ≥2 个 TMP）
                var texts = row.GetComponentsInChildren<TMP_Text>(true);
                // 绑定字体，防止中文乱码
                var chineseFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts/ChineseFont SDF");
                foreach (var t in texts)
                {
                    if (t != null && chineseFont != null) t.font = chineseFont;
                }
                if (texts.Length >= 3)
                {
                    // texts[0]=名次, texts[1]=昵称, texts[2]=贡献值
                    if (texts[0].fontSize < 24f) texts[0].fontSize = 24f;
                    if (texts[1].fontSize < 28f) texts[1].fontSize = 28f;
                    if (texts[2].fontSize < 24f) texts[2].fontSize = 24f;
                    texts[0].text = GetRankEmoji(i + 1);
                    texts[1].text = TruncateName(contributor.Nickname, 6);
                    texts[2].text = contributor.Score.ToString("N0");
                }
                else if (texts.Length == 2)
                {
                    if (texts[0].fontSize < 28f) texts[0].fontSize = 28f;
                    if (texts[1].fontSize < 24f) texts[1].fontSize = 24f;
                    texts[0].text = $"{GetRankEmoji(i + 1)} {TruncateName(contributor.Nickname, 5)}";
                    texts[1].text = contributor.Score.ToString("N0");
                }
                else if (texts.Length == 1)
                {
                    if (texts[0].fontSize < 28f) texts[0].fontSize = 28f;
                    texts[0].text = $"{i + 1}. {TruncateName(contributor.Nickname, 5)} {contributor.Score}";
                }

                // 排名变化时播放闪烁动画
                if (rankChanged && row != null)
                    StartCoroutine(FlashRow(row));
            }
        }

        // ==================== 工具 ====================

        private string GetRankEmoji(int rank)
        {
            return rank switch
            {
                1 => "#1",
                2 => "#2",
                3 => "#3",
                _ => $"#{rank}"
            };
        }

        private string TruncateName(string name, int maxLen)
        {
            if (string.IsNullOrEmpty(name)) return "匿名";
            return name.Length <= maxLen ? name : name.Substring(0, maxLen) + "..";
        }

        /// <summary>排名变化时行背景短暂高亮（0.3s）</summary>
        private IEnumerator FlashRow(GameObject row)
        {
            var images = row.GetComponentsInChildren<Image>(true);
            Color[] origColors = new Color[images.Length];
            for (int i = 0; i < images.Length; i++)
                origColors[i] = images[i].color;

            // 高亮到亮黄色
            Color highlight = new Color(1f, 0.9f, 0.3f, 0.8f);
            foreach (var img in images)
            {
                if (img != null) img.color = highlight;
            }

            yield return new WaitForSeconds(0.3f);

            // 还原
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null) images[i].color = origColors[i];
            }
        }
    }
}
