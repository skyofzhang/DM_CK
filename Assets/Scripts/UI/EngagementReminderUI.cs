using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DrscfZ.Survival;
using DrscfZ.Core;

namespace DrscfZ.UI
{
    /// <summary>
    /// §34.4 E8 参与感唤回 —— 每 5 分钟对贡献>0 玩家推送的排名提示
    ///
    /// 订阅 SurvivalGameManager.OnEngagementReminder。显示 3s 短暂浮层：
    ///   "你的排名是 #{rank}，距 Top 3 仅差 {gapToTop3} 贡献！"
    ///
    /// 播放模式：
    ///   方案 A (unicast)：后端仅发送给单个玩家，客户端收到即显示。
    ///   方案 B (广播)   ：后端按房间广播，客户端按 playerId === self 过滤。
    ///
    /// 主播侧策略（复用 §34C GiftImpact 的 privateOnly 习惯）：
    ///   主播端 NetworkManager 无"本地 PlayerId"概念，主播侧收到 engagement_reminder 一律跳过（不显示自己）。
    ///   判定依据：
    ///     - join_room_confirm.isRoomCreator=true → 主播 → 跳过
    ///     - false 或未知 → 非主播 → 显示
    ///
    /// 挂载：Canvas/GameUIPanel/EngagementReminder（常驻激活）。
    ///
    /// Inspector 必填：
    ///   _panelRoot        — 浮层根节点（子节点，初始 inactive）
    ///   _panelCanvasGroup — CanvasGroup 淡入淡出
    ///   _messageText      — 消息 TMP
    /// </summary>
    public class EngagementReminderUI : MonoBehaviour
    {
        [Header("浮层根节点")]
        [SerializeField] private RectTransform   _panelRoot;
        [SerializeField] private CanvasGroup     _panelCanvasGroup;

        [Header("文字")]
        [SerializeField] private TextMeshProUGUI _messageText;

        private const float FADE_IN  = 0.25f;
        private const float HOLD     = 2.5f;
        private const float FADE_OUT = 0.25f;

        private Coroutine _runCoroutine;
        private bool      _sgmSubscribed = false;
        private bool      _netSubscribed = false;

        // 主播身份判定（主播侧一律跳过）
        private bool _isRoomCreator = false;

        // ── 生命周期 ──────────────────────────────────────────────────────

        private void Start()
        {
            if (_panelRoot != null) _panelRoot.gameObject.SetActive(false);
            if (_panelCanvasGroup != null) _panelCanvasGroup.alpha = 0f;
            TrySubscribe();
        }

        private void OnEnable()  { TrySubscribe(); }
        private void OnDisable() { Unsubscribe(); }
        private void OnDestroy() { Unsubscribe(); if (_runCoroutine != null) { StopCoroutine(_runCoroutine); _runCoroutine = null; } }

        private void Update()
        {
            if (!_sgmSubscribed || !_netSubscribed) TrySubscribe();
        }

        private void TrySubscribe()
        {
            var sgm = SurvivalGameManager.Instance;
            if (!_sgmSubscribed && sgm != null)
            {
                sgm.OnEngagementReminder += HandleEngagementReminder;
                _sgmSubscribed = true;
            }
            var net = NetworkManager.Instance;
            if (!_netSubscribed && net != null)
            {
                net.OnMessageReceived += HandleNetMessage;
                _netSubscribed = true;
            }
        }

        private void Unsubscribe()
        {
            var sgm = SurvivalGameManager.Instance;
            if (_sgmSubscribed && sgm != null)
            {
                sgm.OnEngagementReminder -= HandleEngagementReminder;
            }
            _sgmSubscribed = false;
            var net = NetworkManager.Instance;
            if (_netSubscribed && net != null)
            {
                net.OnMessageReceived -= HandleNetMessage;
            }
            _netSubscribed = false;
        }

        private void HandleNetMessage(string type, string dataJson)
        {
            if (type != "join_room_confirm") return;
            _isRoomCreator = ParseBoolField(dataJson, "isRoomCreator");
        }

        private static bool ParseBoolField(string json, string field)
        {
            if (string.IsNullOrEmpty(json)) return false;
            int idx = json.IndexOf("\"" + field + "\"");
            if (idx < 0) return false;
            int colon = json.IndexOf(':', idx);
            if (colon < 0) return false;
            int start = colon + 1;
            while (start < json.Length && (json[start] == ' ' || json[start] == '\t')) start++;
            if (start + 4 > json.Length) return false;
            return json.Substring(start, 4).ToLowerInvariant() == "true";
        }

        // ── 事件回调 ──────────────────────────────────────────────────────

        private void HandleEngagementReminder(EngagementReminderData data)
        {
            if (data == null || data.entries == null || data.entries.Length == 0) return;

            // 主播侧一律跳过（NetworkManager 无 PlayerId，无法做 playerId === self 过滤，
            // 沿用组 C GiftImpactUI 策略：主播端不显示此类"自我激励"浮层）
            if (_isRoomCreator) return;

            // NetworkManager 无 SelfPlayerId 接口 → fallback 展示 entries[0]
            // （服务端已按贡献降序排序，非主播观众视角下第一条是当前榜首；
            //  后续接入 SelfPlayerId 时改为按 playerId === self 过滤）
            var entry = data.entries[0];
            if (entry == null) return;

            // 组合文案
            string msg;
            if (entry.rank <= 3)
            {
                msg = $"你已跻身 Top 3，当前第 #{entry.rank}！继续保持！";
            }
            else
            {
                msg = $"你的排名是 #{entry.rank}，距 Top 3 仅差 {entry.gapToTop3} 贡献！";
            }

            if (_runCoroutine != null) StopCoroutine(_runCoroutine);
            _runCoroutine = StartCoroutine(RunReminder(msg));
        }

        private IEnumerator RunReminder(string msg)
        {
            if (_messageText != null) _messageText.text = msg;
            if (_panelRoot != null) _panelRoot.gameObject.SetActive(true);

            float t = 0f;
            while (t < FADE_IN)
            {
                t += Time.unscaledDeltaTime;
                if (_panelCanvasGroup != null) _panelCanvasGroup.alpha = Mathf.Clamp01(t / FADE_IN);
                yield return null;
            }
            if (_panelCanvasGroup != null) _panelCanvasGroup.alpha = 1f;

            yield return new WaitForSecondsRealtime(HOLD);

            t = 0f;
            while (t < FADE_OUT)
            {
                t += Time.unscaledDeltaTime;
                if (_panelCanvasGroup != null) _panelCanvasGroup.alpha = 1f - Mathf.Clamp01(t / FADE_OUT);
                yield return null;
            }
            if (_panelCanvasGroup != null) _panelCanvasGroup.alpha = 0f;
            if (_panelRoot != null) _panelRoot.gameObject.SetActive(false);
            _runCoroutine = null;
        }
    }
}
