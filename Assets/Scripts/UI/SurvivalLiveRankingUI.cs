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

        // ==================== 生命周期 ====================

        private void Awake()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        private void Start()
        {
            TrySubscribe();
            BindFont(_titleText);
        }

        private void OnEnable()   { TrySubscribe(); }
        private void OnDisable()  { Unsubscribe(); }
        private void OnDestroy()  { Unsubscribe(); }

        // SGM 可能比本组件晚初始化，Update 里补订阅（成功后停止轮询）
        private void Update()
        {
            if (!_subscribed) TrySubscribe();
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

                string name  = Truncate(entry.playerName, 6);
                string score = entry.contribution.ToString("N0");
                string rank  = $"#{entry.rank}";

                if (texts.Length >= 3)
                {
                    texts[0].text = rank;
                    texts[1].text = name;
                    texts[2].text = score;
                }
                else if (texts.Length == 2)
                {
                    texts[0].text = $"{rank} {name}";
                    texts[1].text = score;
                }
                else if (texts.Length == 1)
                {
                    texts[0].text = $"{rank} {name}  {score}";
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
                _cachedFont = Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
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
    }
}
