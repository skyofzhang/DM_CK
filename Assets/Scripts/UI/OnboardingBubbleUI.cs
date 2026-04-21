using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrscfZ.Survival;
using DrscfZ.Core;

namespace DrscfZ.UI
{
    /// <summary>
    /// §17.15 新手引导气泡（🆕 v1.27 MVP）。
    ///
    /// 5 个事件：
    ///   B1 开场引导   - show_onboarding_sequence 触发后 0.3s，3s 文案"活过今夜就算胜利！白天采矿，夜晚打怪"
    ///   B2 矿工介绍   - B1 结束后 0.5s，3s 文案"矿工帮你打工，刷礼物让他变强"
    ///   B3 礼物介绍   - B2 结束后 0.5s，3s 文案"刷这些礼物来参与，最便宜 1 元"
    ///   B4 夜晚警示   - phase_changed→night 且 seasonDay≥3 且 fortressDay∈[3,5]，4s 文案"夜晚来临！做好准备"
    ///   B5 坚持鼓励   - fortress_day_changed{reason=promoted} 且 newFortressDay∈[3,5] 且 seasonDay≥3，3s 文案"活下来了！明天继续"
    ///
    /// B1-B3 幂等：<c>_lastSessionId</c> 内存变量，同 sessionId 重复推送不重播；断线重连自动清空。
    /// B4/B5 每个房间生命周期仅触发一次（<c>_b4ShownThisSession</c> / <c>_b5ShownThisSession</c>）。
    ///
    /// 挂载：<c>Canvas/GameUIPanel/OnboardingBubble</c>（always-active）。
    /// 预创建 Inspector 字段；若 <c>_bubbleRoot</c> 为空，运行时 <c>CreateBubbleRuntime()</c> 自建兜底。
    /// </summary>
    public class OnboardingBubbleUI : MonoBehaviour
    {
        public static OnboardingBubbleUI Instance { get; private set; }

        // ==================== Inspector 字段 ====================

        [Header("气泡节点（Editor 脚本预创建；若为 null 运行时自建）")]
        [SerializeField] private RectTransform _bubbleRoot;   // 气泡容器 RectTransform
        [SerializeField] private Image          _bubbleBg;    // 半透明黑底
        [SerializeField] private TMP_Text       _bubbleText;  // 文本（TMP）
        [SerializeField] private CanvasGroup    _canvasGroup; // 用于渐入/渐出（若为 null 运行时 AddComponent）

        [Header("时序参数（策划案 §17.15 默认，勿改）")]
        [SerializeField] private float _b1DisplaySec     = 3f;
        [SerializeField] private float _b2DisplaySec     = 3f;
        [SerializeField] private float _b3DisplaySec     = 3f;
        [SerializeField] private float _b4DisplaySec     = 4f;
        [SerializeField] private float _b5DisplaySec     = 3f;
        [SerializeField] private float _startDelaySec    = 0.3f;  // B1 触发前等待
        [SerializeField] private float _gapBetweenBubble = 0.5f;  // B1-B2, B2-B3 间隔
        [SerializeField] private float _fadeInSec        = 0.25f;
        [SerializeField] private float _fadeOutSec       = 0.25f;

        [Header("文案（中文，禁用 emoji；CLAUDE.md UI emoji 踩坑）")]
        [SerializeField] private string _b1Text = "活过今夜就算胜利！白天采矿，夜晚打怪";
        [SerializeField] private string _b2Text = "矿工帮你打工，刷礼物让他变强";
        [SerializeField] private string _b3Text = "刷这些礼物来参与，最便宜 1 元";
        [SerializeField] private string _b4Text = "夜晚来临！做好准备";
        [SerializeField] private string _b5Text = "活下来了！明天继续";

        // ==================== 内存状态（不持久化） ====================

        /// <summary>B1-B3 幂等键：同 sessionId 重复推送不重播。</summary>
        private string _lastSessionId = null;

        /// <summary>B4 已在本房间会话触发（MonoBehaviour 生命周期内有效）。</summary>
        private bool _b4ShownThisSession = false;

        /// <summary>B5 已在本房间会话触发（MonoBehaviour 生命周期内有效）。</summary>
        private bool _b5ShownThisSession = false;

        /// <summary>当前赛季日缓存（订阅 OnSeasonState 更新 / SeasonRuntimeState 兜底）。</summary>
        private int _currentSeasonDay = 0;

        /// <summary>当前房间堡垒日缓存（订阅 OnFortressDayChanged 更新）。</summary>
        private int _currentFortressDay = 1;

        /// <summary>正在播放的 B1-B3 协程（用于外部 DisableLocal 打断）。</summary>
        private Coroutine _sequenceCoroutine;

        /// <summary>当前显示的单气泡协程（B4/B5 或序列中间）。</summary>
        private Coroutine _singleBubbleCoroutine;

        // ==================== 生命周期 ====================

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // 若 Editor 脚本未预创建 bubble 节点，运行时自建兜底（不至于哑掉）
            if (_bubbleRoot == null)
            {
                Debug.LogWarning("[OnboardingBubbleUI] _bubbleRoot 未绑定，运行时自建兜底；建议跑 Tools → DrscfZ → Setup Section 17.15 + 24.5 UI");
                CreateBubbleRuntime();
            }

            // 初始隐藏
            if (_bubbleRoot != null)
                _bubbleRoot.gameObject.SetActive(false);
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;

            // 订阅事件
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnShowOnboardingSequence += HandleShowOnboardingSequence;
                sgm.OnPhaseChanged           += HandlePhaseChanged;
                sgm.OnFortressDayChanged     += HandleFortressDayChanged;
                sgm.OnSeasonState            += HandleSeasonState;
            }
            else
            {
                Debug.LogWarning("[OnboardingBubbleUI] Start: SurvivalGameManager.Instance 为 null，无法订阅事件");
            }

            // 订阅 NetworkManager.OnConnected（NetworkManager.cs:88 已提供该事件），
            // 断线重连时清空 _lastSessionId，允许服务端重发的 UUID 重新播放（策划案 §17.15 要求）
            var net = NetworkManager.Instance;
            if (net != null)
                net.OnConnected += ResetOnboardingState;

            // 初始化 seasonDay 缓存（若 SurvivalGameManager 已收到 season_state）
            if (sgm != null && sgm.CurrentSeasonState != null)
                _currentSeasonDay = sgm.CurrentSeasonState.seasonDay;

            // 初始化 fortressDay 缓存（若 FortressDayBadgeUI 已有数据则沿用）
            if (FortressDayBadgeUI.Instance != null)
                _currentFortressDay = FortressDayBadgeUI.Instance.CurrentFortressDay;
        }

        private void OnDestroy()
        {
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnShowOnboardingSequence -= HandleShowOnboardingSequence;
                sgm.OnPhaseChanged           -= HandlePhaseChanged;
                sgm.OnFortressDayChanged     -= HandleFortressDayChanged;
                sgm.OnSeasonState            -= HandleSeasonState;
            }
            var net = NetworkManager.Instance;
            if (net != null)
                net.OnConnected -= ResetOnboardingState;

            if (Instance == this) Instance = null;
        }

        // ==================== 事件回调 ====================

        private void HandleShowOnboardingSequence(ShowOnboardingSequenceData data)
        {
            if (data == null || string.IsNullOrEmpty(data.sessionId)) return;

            // sessionId 幂等：同 id 重复推送不重播
            if (data.sessionId == _lastSessionId)
            {
                Debug.Log($"[OnboardingBubbleUI] sessionId={data.sessionId} 已播放过，幂等忽略");
                return;
            }
            _lastSessionId = data.sessionId;

            // 中断上一个序列（如果还在播）
            StopCurrentBubble();
            _sequenceCoroutine = StartCoroutine(PlayB1B2B3Sequence());
        }

        private void HandlePhaseChanged(PhaseChangedData pc)
        {
            if (pc == null) return;

            // B4 触发：phase=night + seasonDay>=3 + fortressDay∈[3,5] + 未触发过
            if (pc.phase == "night"
                && _currentSeasonDay >= 3
                && _currentFortressDay >= 3
                && _currentFortressDay <= 5
                && !_b4ShownThisSession)
            {
                _b4ShownThisSession = true;
                StopCurrentBubble();
                _singleBubbleCoroutine = StartCoroutine(ShowBubbleOnce(_b4Text, _b4DisplaySec));
                Debug.Log($"[OnboardingBubbleUI] B4 触发（seasonDay={_currentSeasonDay} fortressDay={_currentFortressDay}）");
            }
        }

        private void HandleFortressDayChanged(FortressDayChangedData data)
        {
            if (data == null) return;
            // 更新缓存
            _currentFortressDay = data.newFortressDay;

            // B5 触发：reason=promoted + newFortressDay∈[3,5] + seasonDay>=3 + 未触发过
            if (data.reason == "promoted"
                && data.newFortressDay >= 3
                && data.newFortressDay <= 5
                && data.seasonDay >= 3
                && !_b5ShownThisSession)
            {
                _b5ShownThisSession = true;
                StopCurrentBubble();
                _singleBubbleCoroutine = StartCoroutine(ShowBubbleOnce(_b5Text, _b5DisplaySec));
                Debug.Log($"[OnboardingBubbleUI] B5 触发（newFortressDay={data.newFortressDay} seasonDay={data.seasonDay}）");
            }
        }

        private void HandleSeasonState(SeasonStateData ss)
        {
            if (ss == null) return;
            _currentSeasonDay = ss.seasonDay;
        }

        // ==================== 公共 API ====================

        /// <summary>§17.15 主播点击"关闭引导"按钮时由 BroadcasterPanel 调用。
        /// 立即打断本地正在播放的 B1-B3 序列，清空 sessionId 允许后续服务端重发 UUID 再触发（若主播取消后反悔）。
        /// 注意：C→S 的 disable_onboarding_for_session 由 BroadcasterPanel 或调用方直接 SurvivalGameManager.SendDisableOnboarding()。</summary>
        public void DisableLocal()
        {
            StopCurrentBubble();
            _lastSessionId = null;
            Debug.Log("[OnboardingBubbleUI] DisableLocal 调用：已停止本地序列");
        }

        /// <summary>WS 断线重连时清空 sessionId 幂等键，允许服务端重发的相同 UUID 重新播放。
        /// 调用方：NetworkManager.OnConnected 订阅（策划案 §17.15 明确要求）；外部也可手动调用做 QA 测试。</summary>
        public void ResetOnboardingState()
        {
            _lastSessionId = null;
            Debug.Log("[OnboardingBubbleUI] ResetOnboardingState：sessionId 缓存已清空");
        }

        // ==================== 协程：B1-B3 时序链 ====================

        private IEnumerator PlayB1B2B3Sequence()
        {
            yield return new WaitForSeconds(_startDelaySec);

            // B1
            yield return PlayBubbleCoroutine(_b1Text, _b1DisplaySec);
            yield return new WaitForSeconds(_gapBetweenBubble);
            // B2
            yield return PlayBubbleCoroutine(_b2Text, _b2DisplaySec);
            yield return new WaitForSeconds(_gapBetweenBubble);
            // B3
            yield return PlayBubbleCoroutine(_b3Text, _b3DisplaySec);

            _sequenceCoroutine = null;
        }

        private IEnumerator ShowBubbleOnce(string text, float displaySec)
        {
            yield return PlayBubbleCoroutine(text, displaySec);
            _singleBubbleCoroutine = null;
        }

        private IEnumerator PlayBubbleCoroutine(string text, float displaySec)
        {
            if (_bubbleRoot == null) yield break;

            // 显示 + 写文本
            _bubbleRoot.gameObject.SetActive(true);
            if (_bubbleText != null) _bubbleText.text = text;

            // 渐入
            yield return FadeAlphaCoroutine(0f, 1f, _fadeInSec);
            // 停留
            yield return new WaitForSeconds(displaySec);
            // 渐出
            yield return FadeAlphaCoroutine(1f, 0f, _fadeOutSec);

            _bubbleRoot.gameObject.SetActive(false);
        }

        private IEnumerator FadeAlphaCoroutine(float from, float to, float duration)
        {
            if (_canvasGroup == null) yield break;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                yield return null;
            }
            _canvasGroup.alpha = to;
        }

        private void StopCurrentBubble()
        {
            if (_sequenceCoroutine != null)
            {
                StopCoroutine(_sequenceCoroutine);
                _sequenceCoroutine = null;
            }
            if (_singleBubbleCoroutine != null)
            {
                StopCoroutine(_singleBubbleCoroutine);
                _singleBubbleCoroutine = null;
            }
            if (_bubbleRoot != null)
                _bubbleRoot.gameObject.SetActive(false);
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;
        }

        // ==================== 运行时 Fallback：Editor 脚本未跑时自建最小化气泡 ====================

        private void CreateBubbleRuntime()
        {
            // 期望挂在 Canvas/GameUIPanel 下；parentRT 为 OnboardingBubbleUI GO 自身
            var parentRT = GetComponent<RectTransform>();
            if (parentRT == null) parentRT = gameObject.AddComponent<RectTransform>();

            // 气泡容器：屏幕中央上方，宽 800 高 100
            var rootGO = new GameObject("BubbleRoot");
            rootGO.transform.SetParent(parentRT, false);
            var rootRT = rootGO.AddComponent<RectTransform>();
            rootRT.anchorMin = new Vector2(0.5f, 0.7f);
            rootRT.anchorMax = new Vector2(0.5f, 0.7f);
            rootRT.pivot     = new Vector2(0.5f, 0.5f);
            rootRT.sizeDelta = new Vector2(800f, 100f);
            rootRT.anchoredPosition = Vector2.zero;

            // 黑底
            var bg = rootGO.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.7f);
            bg.raycastTarget = false;
            _bubbleBg = bg;

            // CanvasGroup for fade
            var cg = rootGO.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable   = false;
            cg.alpha          = 0f;
            _canvasGroup = cg;

            // 文本
            var textGO = new GameObject("BubbleText");
            textGO.transform.SetParent(rootGO.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(20f, 0f);
            textRT.offsetMax = new Vector2(-20f, 0f);

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text         = "";
            tmp.fontSize     = 36f;
            tmp.alignment    = TextAlignmentOptions.Center;
            tmp.color        = Color.white;
            tmp.raycastTarget = false;
            _bubbleText = tmp;

            _bubbleRoot = rootRT;
            _bubbleRoot.gameObject.SetActive(false);
        }
    }
}
