using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using DrscfZ.Core;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §38 探险系统——商队交易（trader_caravan）UI（MVP 简化版）
    ///
    /// 触发：服务端推 expedition_event (eventId="trader_caravan") → SurvivalGameManager
    ///       路由到本组件。
    /// 行为：
    ///   - 弹出 2 按钮"接受 / 拒绝"
    ///   - 顶部显示基于 eventEndsAt 的倒计时
    ///   - 接受 → 发送 expedition_event_vote {expeditionId, choice:"accept"}
    ///     服务端按 §38.3 执行 "200食物+50矿石 → 城门立即 Lv+1"
    ///   - 拒绝 → 发送 expedition_event_vote {expeditionId, choice:"cancel"}
    ///   - 超时自动关闭面板（服务端同时视作弃权，无需客户端发送消息）
    ///
    /// 挂载（Rule #7）：挂在 Canvas（always-active），_panel 初始 inactive。
    /// 若 _panel 未绑定（场景未预创建），所有显示逻辑降级为 Debug.Log，
    /// SurvivalGameManager 会退回到 AnnouncementUI 的文案提示路径。
    /// </summary>
    public class TraderCaravanUI : MonoBehaviour
    {
        public static TraderCaravanUI Instance { get; private set; }

        // ==================== Inspector 字段 ====================

        [Header("面板根（初始 inactive）")]
        [SerializeField] private GameObject _panel;

        [Header("接受 / 拒绝 按钮")]
        [SerializeField] private Button btnAccept;
        [SerializeField] private Button btnCancel;

        [Header("文案与倒计时")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _descText;
        [SerializeField] private TMP_Text _countdownText;

        // ==================== 运行时状态 ====================

        private string _expeditionId       = null;
        private long   _expiresAtUnixMs    = 0;
        private Coroutine _timeoutCoroutine;

        // ==================== 生命周期 ====================

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            if (_panel != null) _panel.SetActive(false);
        }

        private void Start()
        {
            if (btnAccept != null) btnAccept.onClick.AddListener(() => OnChoiceClicked("accept"));
            if (btnCancel != null) btnCancel.onClick.AddListener(() => OnChoiceClicked("cancel"));
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // 实时刷新倒计时（基于 eventEndsAt）
            if (_panel != null && _panel.activeSelf && _expiresAtUnixMs > 0 && _countdownText != null)
            {
                long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long remainMs = _expiresAtUnixMs - nowMs;
                int sec = remainMs <= 0 ? 0 : Mathf.CeilToInt(remainMs / 1000f);
                _countdownText.text = $"{sec}s";
            }
        }

        // ==================== 公共接口 ====================

        /// <summary>
        /// 显示商队交易面板。由 SurvivalGameManager.HandleExpeditionEvent 在 eventId=="trader_caravan"
        /// 且 options 非空时调用。
        /// </summary>
        public void Show(ExpeditionEventData data)
        {
            if (data == null) return;
            _expeditionId    = data.expeditionId;
            _expiresAtUnixMs = data.eventEndsAt;

            if (_panel == null)
            {
                // 降级：面板未预创建时打 Log（SurvivalGameManager 已处理 AnnouncementUI 兜底）
                Debug.Log($"[TraderCaravanUI] Show（面板未绑定，降级 Log）: expeditionId={data.expeditionId} eventEndsAt={data.eventEndsAt} options=[{string.Join(",", data.options ?? new string[0])}]");
                return;
            }

            if (_titleText != null)
                _titleText.text = "商队交易";
            if (_descText != null)
                _descText.text = "主播决定：\n<color=#FFC020>接受</color> 200食物 + 50矿石 → 城门立即 Lv+1\n<color=#AAAAAA>拒绝</color> 放弃本次交易";

            _panel.SetActive(true);

            // 启动超时自动关闭
            if (_timeoutCoroutine != null) StopCoroutine(_timeoutCoroutine);
            _timeoutCoroutine = StartCoroutine(AutoCloseOnExpiry());

            Debug.Log($"[TraderCaravanUI] 显示商队交易面板 expeditionId={data.expeditionId} eventEndsAt={data.eventEndsAt}");
        }

        // ==================== 按钮回调 ====================

        private void OnChoiceClicked(string choice)
        {
            if (string.IsNullOrEmpty(_expeditionId))
            {
                Debug.LogWarning("[TraderCaravanUI] OnChoiceClicked: _expeditionId 为空，忽略");
                ClosePanel();
                return;
            }

            var net = NetworkManager.Instance;
            if (net != null && net.IsConnected)
            {
                long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                string json = $"{{\"type\":\"expedition_event_vote\",\"data\":{{\"expeditionId\":\"{EscapeJson(_expeditionId)}\",\"choice\":\"{choice}\"}},\"timestamp\":{ts}}}";
                net.SendJson(json);
                Debug.Log($"[TraderCaravanUI] 发送 expedition_event_vote expeditionId={_expeditionId} choice={choice}");
            }
            else
            {
                Debug.LogWarning("[TraderCaravanUI] OnChoiceClicked: NetworkManager 未连接，忽略");
            }
            ClosePanel();
        }

        private IEnumerator AutoCloseOnExpiry()
        {
            while (true)
            {
                long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                if (nowMs >= _expiresAtUnixMs) break;
                yield return new WaitForSeconds(0.2f);
            }
            // 超时仅关面板；服务端自动视作弃权（cancel）
            Debug.Log($"[TraderCaravanUI] 倒计时结束，自动关闭面板 expeditionId={_expeditionId}（服务端视作弃权）");
            ClosePanel();
        }

        private void ClosePanel()
        {
            if (_timeoutCoroutine != null) { StopCoroutine(_timeoutCoroutine); _timeoutCoroutine = null; }
            if (_panel != null) _panel.SetActive(false);
            _expeditionId    = null;
            _expiresAtUnixMs = 0;
        }

        // ==================== 工具 ====================

        /// <summary>转义 JSON 字符串字段中的双引号与反斜杠（expeditionId 一般为 UUID，已够用）</summary>
        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
