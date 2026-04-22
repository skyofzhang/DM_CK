using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DrscfZ.Survival;
using DrscfZ.Core;

namespace DrscfZ.UI
{
    /// <summary>
    /// §34.4 E5a 智能提词器 —— 仅主播可见的话术提示
    ///
    /// 订阅 SurvivalGameManager.OnStreamerPrompt，在 BroadcasterPanel 右上角独立节点 StreamerPromptCard 显示：
    ///   - urgent 红底加粗     ("食物快没了！提醒观众刷甜甜圈！")
    ///   - social 蓝底普通     ("{top1}和{top2}只差{gap}！引导他们！")
    ///   - info   灰底半透      (矿石流向等提示)
    /// 5s 自动淡出，同时最多 1 条（新提示到来时立即切换）。
    ///
    /// 主播身份判定：沿用 BroadcasterPanel 的 join_room_confirm.isRoomCreator 判定；
    /// 本脚本通过 NetworkManager.OnMessageReceived 自行监听 join_room_confirm 解析 isRoomCreator 字段，
    /// 非主播收到（理论上服务端仅发给主播，保底过滤）直接忽略。
    ///
    /// 位置：BroadcasterPanel/StreamerPromptCard（右上角，避开 §24.5 左上角 BroadcasterDecisionHUD）。
    ///
    /// 挂载规则（CLAUDE.md #7）：脚本挂 StreamerPromptCard（常驻激活）；Awake 内仅对 _cardRoot 子节点 SetActive(false)。
    ///
    /// Inspector 必填：
    ///   _cardRoot        — 卡片根节点（子节点，初始 inactive）
    ///   _cardCanvasGroup — CanvasGroup 控制淡出
    ///   _cardBg          — 背景 Image（根据 priority 切换颜色）
    ///   _promptText      — 主文字 TMP
    /// </summary>
    public class StreamerPromptUI : MonoBehaviour
    {
        [Header("卡片根节点")]
        [SerializeField] private RectTransform   _cardRoot;
        [SerializeField] private CanvasGroup     _cardCanvasGroup;
        [SerializeField] private Image           _cardBg;

        [Header("文字")]
        [SerializeField] private TextMeshProUGUI _promptText;

        // ── 视觉参数 ───────────────────────────────────────────────────────

        private const float DISPLAY_DURATION = 5.0f;   // 5s 自动淡出
        private const float FADE_OUT         = 0.4f;

        // priority 三色：urgent 红 / social 蓝 / info 灰半透
        private static readonly Color URGENT_BG = new Color(0.85f, 0.15f, 0.15f, 0.92f);
        private static readonly Color SOCIAL_BG = new Color(0.20f, 0.45f, 0.85f, 0.92f);
        private static readonly Color INFO_BG   = new Color(0.30f, 0.30f, 0.30f, 0.75f);

        // 主播身份判定
        private bool _isRoomCreator = false;
        private bool _netSubscribed = false;
        private bool _sgmSubscribed = false;

        // 当前播放中协程（新提示到来时立即取消切换）
        private Coroutine _runCoroutine;

        // ── 生命周期 ──────────────────────────────────────────────────────

        private void Start()
        {
            // 仅对子节点 _cardRoot 执行 SetActive(false)
            if (_cardRoot != null) _cardRoot.gameObject.SetActive(false);
            if (_cardCanvasGroup != null) _cardCanvasGroup.alpha = 0f;
            TrySubscribe();
        }

        private void OnEnable()  { TrySubscribe(); }
        private void OnDisable() { Unsubscribe(); }
        private void OnDestroy() { Unsubscribe(); }

        private void Update()
        {
            if (!_sgmSubscribed || !_netSubscribed) TrySubscribe();
        }

        private void TrySubscribe()
        {
            var sgm = SurvivalGameManager.Instance;
            if (!_sgmSubscribed && sgm != null)
            {
                sgm.OnStreamerPrompt += HandleStreamerPrompt;
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
                sgm.OnStreamerPrompt -= HandleStreamerPrompt;
            }
            _sgmSubscribed = false;
            var net = NetworkManager.Instance;
            if (_netSubscribed && net != null)
            {
                net.OnMessageReceived -= HandleNetMessage;
            }
            _netSubscribed = false;
        }

        // ── join_room_confirm 解析主播身份 ────────────────────────────────

        private void HandleNetMessage(string type, string dataJson)
        {
            if (type != "join_room_confirm") return;
            _isRoomCreator = ParseBoolField(dataJson, "isRoomCreator");
            if (!_isRoomCreator)
            {
                // 非主播：强制隐藏卡片（可能是重连切换身份）
                if (_cardRoot != null) _cardRoot.gameObject.SetActive(false);
                if (_runCoroutine != null)
                {
                    StopCoroutine(_runCoroutine);
                    _runCoroutine = null;
                }
            }
        }

        /// <summary>极简 bool 字段解析（参考 BroadcasterPanel.ParseBoolField）。</summary>
        private static bool ParseBoolField(string json, string field)
        {
            if (string.IsNullOrEmpty(json)) return false;
            int idx = json.IndexOf("\"" + field + "\"");
            if (idx < 0) return false;
            int colon = json.IndexOf(':', idx);
            if (colon < 0) return false;
            int start = colon + 1;
            // 跳过空白
            while (start < json.Length && (json[start] == ' ' || json[start] == '\t')) start++;
            if (start + 4 > json.Length) return false;
            return json.Substring(start, 4).ToLowerInvariant() == "true";
        }

        // ── 事件回调 ──────────────────────────────────────────────────────

        private void HandleStreamerPrompt(StreamerPromptData data)
        {
            // 仅主播显示；后端原则上仅推送给主播，但保底过滤
            if (!_isRoomCreator) return;
            if (data == null || string.IsNullOrEmpty(data.text)) return;

            // 同时最多 1 条：新提示到来时立即取消当前协程
            if (_runCoroutine != null)
            {
                StopCoroutine(_runCoroutine);
                _runCoroutine = null;
            }

            _runCoroutine = StartCoroutine(RunPrompt(data));
        }

        private IEnumerator RunPrompt(StreamerPromptData data)
        {
            // 背景色 + 字样
            if (_cardBg != null)
            {
                switch (data.priority)
                {
                    case "urgent":  _cardBg.color = URGENT_BG; break;
                    case "social":  _cardBg.color = SOCIAL_BG; break;
                    default:        _cardBg.color = INFO_BG;   break;  // info 或未知
                }
            }
            if (_promptText != null)
            {
                _promptText.text = data.text;
                _promptText.fontStyle = (data.priority == "urgent") ? FontStyles.Bold : FontStyles.Normal;
            }

            if (_cardRoot != null) _cardRoot.gameObject.SetActive(true);
            if (_cardCanvasGroup != null) _cardCanvasGroup.alpha = 1f;

            // 停留 5s
            yield return new WaitForSecondsRealtime(DISPLAY_DURATION);

            // 淡出
            float t = 0f;
            while (t < FADE_OUT)
            {
                t += Time.unscaledDeltaTime;
                if (_cardCanvasGroup != null)
                    _cardCanvasGroup.alpha = 1f - Mathf.Clamp01(t / FADE_OUT);
                yield return null;
            }
            if (_cardCanvasGroup != null) _cardCanvasGroup.alpha = 0f;
            if (_cardRoot != null) _cardRoot.gameObject.SetActive(false);
            _runCoroutine = null;
        }
    }
}
