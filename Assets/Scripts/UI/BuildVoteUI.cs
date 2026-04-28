using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DrscfZ.Core;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §37 建造投票 UI(BuildVoteUI)
    /// 挂 Canvas/GameUIPanel/BuildVotePanel;Prefab 绑定由人工补。
    /// 订阅 SurvivalGameManager 的投票事件并在面板上展示 3 张候选 + 倒计时 + 实时票数。
    /// MVP 范围:
    ///   - 脚本齐全,Inspector 字段仍需人工拖入(TODO)
    ///   - 单 Singleton,与 §24.4 RouletteUI / §38 TraderCaravanUI 风格一致
    ///   - 并行数组协议(voteBuildIds/voteCounts)与 SurvivalDataTypes 对齐
    /// </summary>
    public class BuildVoteUI : MonoBehaviour
    {
        public static BuildVoteUI Instance { get; private set; }

        // ModalRegistry A 类 id（§17.16 audit-r5 切换新 API）
        private const string MODAL_A_ID = "build_vote_panel";

        [Header("面板引用(Inspector 拖入)")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text   _timerText;
        [SerializeField] private TMP_Text   _proposerText;

        [Header("3 个候选卡槽")]
        [SerializeField] private Button[]   _voteButtons  = new Button[3];
        [SerializeField] private TMP_Text[] _voteLabels   = new TMP_Text[3]; // 建筑中文名
        [SerializeField] private TMP_Text[] _voteCounts   = new TMP_Text[3]; // 票数

        private BuildVoteStartedData _current;
        private Coroutine _tickCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (_panel != null) _panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>服务端广播 build_vote_started 时调用:激活面板 + 启动倒计时 + 绑定按钮</summary>
        public void ShowVote(BuildVoteStartedData data)
        {
            if (data == null) return;
            _current = data;

            // §17.16 A 类 modal：新 API Request(id, priority=60, onReplaced)；被高优抢占时自动关闭
            if (_panel != null)
            {
                _panel.SetActive(true);
                ModalRegistry.Request(MODAL_A_ID, 60, () =>
                {
                    if (_panel != null) _panel.SetActive(false);
                });
            }
            if (_proposerText != null) _proposerText.text = $"{data.proposerName} 发起建造投票";

            int n = data.options != null ? data.options.Length : 0;
            for (int i = 0; i < _voteButtons.Length; i++)
            {
                bool active = i < n;
                if (_voteButtons[i] != null)
                {
                    _voteButtons[i].gameObject.SetActive(active);
                    _voteButtons[i].onClick.RemoveAllListeners();
                    if (active)
                    {
                        string buildId = data.options[i];
                        _voteButtons[i].onClick.AddListener(() => SendVote(buildId));
                    }
                }
                if (_voteLabels[i] != null && active)
                    _voteLabels[i].text = GetBuildingDisplayName(data.options[i]);
                if (_voteCounts[i] != null && active)
                    _voteCounts[i].text = "0 票";
            }

            if (_tickCoroutine != null) StopCoroutine(_tickCoroutine);
            _tickCoroutine = StartCoroutine(TickTimer());
        }

        /// <summary>服务端广播 build_vote_update 时调用:按并行数组更新票数</summary>
        public void UpdateVoteCounts(BuildVoteUpdateData data)
        {
            if (data == null || _current == null) return;
            if (data.proposalId != _current.proposalId) return;

            var map = new Dictionary<string, int>();
            if (data.voteBuildIds != null && data.voteCounts != null)
            {
                int len = Mathf.Min(data.voteBuildIds.Length, data.voteCounts.Length);
                for (int i = 0; i < len; i++) map[data.voteBuildIds[i]] = data.voteCounts[i];
            }

            for (int i = 0; i < _voteCounts.Length; i++)
            {
                if (_voteCounts[i] == null || i >= _current.options.Length) continue;
                int c = map.TryGetValue(_current.options[i], out int n) ? n : 0;
                _voteCounts[i].text = $"{c} 票";
            }
        }

        /// <summary>服务端广播 build_vote_ended 时调用:高亮 winner,2s 后自动隐藏</summary>
        public void CloseVote(BuildVoteEndedData data)
        {
            if (data == null || _current == null) return;
            if (data.proposalId != _current.proposalId) return;

            if (_tickCoroutine != null) { StopCoroutine(_tickCoroutine); _tickCoroutine = null; }
            if (_timerText != null) _timerText.text = string.IsNullOrEmpty(data.winnerId) ? "流产" : "通过";

            // 高亮 winner 按钮
            if (!string.IsNullOrEmpty(data.winnerId))
            {
                for (int i = 0; i < _voteButtons.Length; i++)
                {
                    if (_voteButtons[i] == null || i >= _current.options.Length) continue;
                    bool isWinner = _current.options[i] == data.winnerId;
                    var img = _voteButtons[i].image;
                    if (img != null)
                        img.color = isWinner ? new Color(0.4f, 1f, 0.6f) : new Color(0.7f, 0.7f, 0.7f);
                }
            }

            StartCoroutine(DelayHide(2f));
        }

        private IEnumerator DelayHide(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (_panel != null)
            {
                _panel.SetActive(false);
                ModalRegistry.Release(MODAL_A_ID);
            }
            _current = null;
        }

        private IEnumerator TickTimer()
        {
            while (_current != null)
            {
                long nowMs = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long remainMs = _current.votingEndsAt - nowMs;
                if (remainMs < 0) remainMs = 0;
                int sec = Mathf.FloorToInt(remainMs / 1000f);
                if (_timerText != null) _timerText.text = $"{sec} 秒";
                if (remainMs <= 0) yield break;
                yield return new WaitForSeconds(0.5f);
            }
        }

        /// <summary>🔴 audit-r37 GAP-C37-02：客户端发起 build_propose（首次启动建造投票）
        ///   旧版客户端 0 处发送 build_propose → §37 建造系统在客户端实际无法首次启动（仅响应弹幕投票）
        ///   r37 真闭环 — BroadcasterDecisionHUD.OpenBuildPropose 调本方法发起，§24.3 主播 HUD 路径可工作
        ///   服务端 SurvivalGameEngine.js handleBuildPropose 处理；失败时回 build_propose_failed</summary>
        public void SendPropose(string buildId)
        {
            if (string.IsNullOrEmpty(buildId)) { Debug.LogWarning("[BuildVoteUI] SendPropose: buildId 为空"); return; }
            string json = $"{{\"type\":\"build_propose\",\"data\":{{\"buildId\":\"{buildId}\"}}}}";
            NetworkManager.Instance?.SendJson(json);
            Debug.Log($"[BuildVoteUI] SendPropose: buildId={buildId}（r37 GAP-C37-02 闭环）");
        }

        private void SendVote(string buildId)
        {
            if (_current == null || string.IsNullOrEmpty(buildId)) return;
            string json = $"{{\"type\":\"build_vote\",\"data\":{{\"proposalId\":\"{_current.proposalId}\",\"buildId\":\"{buildId}\"}}}}";
            NetworkManager.Instance?.SendJson(json);
            Debug.Log($"[BuildVoteUI] vote {buildId} for proposal {_current.proposalId}");
        }

        /// <summary>建筑 ID → 中文(与 SurvivalGameManager 内部一致)</summary>
        private static string GetBuildingDisplayName(string buildId)
        {
            switch (buildId)
            {
                case "watchtower": return "瞭望塔";
                case "market":     return "市场";
                case "hospital":   return "医院";
                case "altar":      return "祭坛";
                case "beacon":     return "烽火台";
                default:           return buildId;
            }
        }
    }
}
