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
    /// §24.4 神秘商人交易邀约 UI（MVP 简化版）
    ///
    /// 触发：服务端推 broadcaster_trader_offer → 订阅者为 RouletteUI/本组件
    /// 行为：
    ///   - 弹出 2 按钮（A / B），文案格式 "食物150 → 矿石100"
    ///   - 顶部显示倒计时（基于 expiresAt）
    ///   - 点击 A/B → 发送 broadcaster_trader_accept {choice:"A"} / {choice:"B"}
    ///   - 超时自动关闭面板（服务端同时视作弃权，无需客户端发送消息）
    ///
    /// 挂载（Rule #7）：挂在 Canvas（always-active），_panel 初始 inactive。
    /// 若 _panel 未绑定（场景未预创建），所有显示逻辑降级为 Debug.Log。
    /// </summary>
    public class TraderOfferUI : MonoBehaviour
    {
        public static TraderOfferUI Instance { get; private set; }

        // audit-r12 GAP-B02：§17.16 互斥组 A 阻塞型 modal id（priority=70 与 TribeWarLobbyUI 同档）
        private const string MODAL_A_ID  = "trader_offer";
        private const int    MODAL_PRIO  = 70;

        // ==================== Inspector 字段 ====================

        [Header("面板根（初始 inactive）")]
        [SerializeField] private GameObject _panel;

        [Header("A/B 按钮 + 文字描述")]
        [SerializeField] private Button   btnA;
        [SerializeField] private Button   btnB;
        [SerializeField] private TextMeshProUGUI _cardAText;
        [SerializeField] private TextMeshProUGUI _cardBText;

        [Header("倒计时文字")]
        [SerializeField] private TMP_Text _countdownText;

        // ==================== 状态 ====================

        private long _expiresAtUnixMs = 0;
        private bool _subscribed      = false;
        private Coroutine _timeoutCoroutine;

        // ==================== 生命周期 ====================

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            EnsureFallbackUI();
            if (_panel != null) _panel.SetActive(false);
        }

        private void Start()
        {
            if (btnA != null) btnA.onClick.AddListener(() => OnChoiceClicked("A"));
            if (btnB != null) btnB.onClick.AddListener(() => OnChoiceClicked("B"));
            TrySubscribe();
        }

        private void OnEnable()  { TrySubscribe(); }
        private void OnDisable() { Unsubscribe();  }
        private void OnDestroy()
        {
            Unsubscribe();
            ModalRegistry.Release(MODAL_A_ID);  // audit-r12 GAP-B02 兜底释放
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!_subscribed) TrySubscribe();

            // 实时刷新倒计时
            if (_panel != null && _panel.activeSelf && _expiresAtUnixMs > 0 && _countdownText != null)
            {
                long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long remainMs = _expiresAtUnixMs - nowMs;
                if (remainMs <= 0)
                {
                    _countdownText.text = "0s";
                }
                else
                {
                    int sec = Mathf.CeilToInt(remainMs / 1000f);
                    _countdownText.text = $"{sec}s";
                }
            }
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;
            sgm.OnTraderOffer += OnTraderOffer;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null) sgm.OnTraderOffer -= OnTraderOffer;
            _subscribed = false;
        }

        // ==================== 事件回调 ====================

        private void EnsureFallbackUI()
        {
            if (_panel != null) return;
            if (transform.parent == null)
                transform.SetParent(RuntimeUIFactory.GetCanvasTransform(), false);

            _panel = RuntimeUIFactory.CreatePanel(transform, "TraderOfferPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(560f, 300f), new Color(0.06f, 0.08f, 0.10f, 0.94f));
            RuntimeUIFactory.AddVerticalLayout(_panel, 12f, new RectOffset(24, 24, 20, 20));

            var title = RuntimeUIFactory.CreateText(_panel.transform, "Title", "神秘商人", 30f,
                new Color(1f, 0.86f, 0.25f), TextAlignmentOptions.Center, new Vector2(520f, 40f));
            RuntimeUIFactory.AddLayoutElement(title.gameObject, 42f);

            _countdownText = RuntimeUIFactory.CreateText(_panel.transform, "Countdown", "30s", 22f,
                Color.white, TextAlignmentOptions.Center, new Vector2(520f, 32f));
            RuntimeUIFactory.AddLayoutElement(_countdownText.gameObject, 34f);

            btnA = RuntimeUIFactory.CreateButton(_panel.transform, "ChoiceA", "A", out _cardAText,
                new Color(0.15f, 0.34f, 0.52f, 1f), new Vector2(500f, 64f));
            RuntimeUIFactory.AddLayoutElement(btnA.gameObject, 64f);

            btnB = RuntimeUIFactory.CreateButton(_panel.transform, "ChoiceB", "B", out _cardBText,
                new Color(0.22f, 0.40f, 0.24f, 1f), new Vector2(500f, 64f));
            RuntimeUIFactory.AddLayoutElement(btnB.gameObject, 64f);
        }

        private void OnTraderOffer(TraderOfferData data)
        {
            if (data == null) return;
            _expiresAtUnixMs = data.expiresAt;

            if (_panel == null)
            {
                // 降级：面板未预创建时，打 Log 让服务端侧也能记录流程
                Debug.Log($"[TraderOfferUI] 收到 trader_offer（面板未绑定，降级 Log）: " +
                          $"A={FormatCard(data.cardA)} / B={FormatCard(data.cardB)} expiresAt={data.expiresAt}");
                return;
            }

            // 填充 A/B 文案
            if (_cardAText != null) _cardAText.text = $"A：{FormatCard(data.cardA)}";
            if (_cardBText != null) _cardBText.text = $"B：{FormatCard(data.cardB)}";

            // audit-r12 GAP-B02：§17.16 互斥组 A 注册（被更高优先级抢占时回调关面板）
            if (!ModalRegistry.Request(MODAL_A_ID, MODAL_PRIO, OnModalReplaced))
            {
                Debug.LogWarning($"[TraderOfferUI] ModalRegistry.Request 被拒（更高优先级 modal 在前），降级为后台等待");
            }

            _panel.SetActive(true);
            Debug.Log($"[TraderOfferUI] 显示交易邀约：A={FormatCard(data.cardA)} B={FormatCard(data.cardB)}");

            // 启动超时自动关闭（到 expiresAt 仍未点击 → 关面板，不发消息，服务端自动视作弃权）
            if (_timeoutCoroutine != null) StopCoroutine(_timeoutCoroutine);
            _timeoutCoroutine = StartCoroutine(AutoCloseOnExpiry());
        }

        private IEnumerator AutoCloseOnExpiry()
        {
            while (true)
            {
                long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                if (nowMs >= _expiresAtUnixMs) break;
                yield return new WaitForSeconds(0.2f);
            }
            ClosePanel();
            Debug.Log("[TraderOfferUI] 交易超时，自动关闭（服务端视作弃权）");
        }

        // ==================== 按钮回调 ====================

        private void OnChoiceClicked(string choice)
        {
            var net = NetworkManager.Instance;
            if (net != null && net.IsConnected)
            {
                long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                net.SendJson($"{{\"type\":\"broadcaster_trader_accept\",\"data\":{{\"choice\":\"{choice}\"}},\"timestamp\":{ts}}}");
                Debug.Log($"[TraderOfferUI] 发送 broadcaster_trader_accept choice={choice}");
            }
            ClosePanel();
        }

        private void ClosePanel()
        {
            if (_timeoutCoroutine != null) { StopCoroutine(_timeoutCoroutine); _timeoutCoroutine = null; }
            if (_panel != null) _panel.SetActive(false);
            _expiresAtUnixMs = 0;
            ModalRegistry.Release(MODAL_A_ID);  // audit-r12 GAP-B02
        }

        // audit-r12 GAP-B02：被高优先级 modal（如结算）抢占时关闭自身，避免叠层
        private void OnModalReplaced()
        {
            if (_timeoutCoroutine != null) { StopCoroutine(_timeoutCoroutine); _timeoutCoroutine = null; }
            if (_panel != null) _panel.SetActive(false);
            _expiresAtUnixMs = 0;
        }

        // ==================== 格式化 ====================

        /// <summary>
        /// 将 TraderCard 的 cost/gain 字段格式化为 "食物150 → 矿石100" 形式。
        /// 仅取 cost 和 gain 各自非零的第 1 项做展示（MVP 简化）。
        /// </summary>
        private static string FormatCard(TraderCard card)
        {
            if (card == null) return "--";
            string cost = FormatSide(card.costFood, card.costCoal, card.costOre, 0);
            string gain = FormatSide(card.gainFood, card.gainCoal, card.gainOre, card.gainGateHp);
            if (string.IsNullOrEmpty(cost) && string.IsNullOrEmpty(gain)) return "--";
            if (string.IsNullOrEmpty(cost)) return gain;
            if (string.IsNullOrEmpty(gain)) return cost;
            return $"{cost} → {gain}";
        }

        /// <summary>格式化一组资源数值，返回首个非零项的中文描述（如 "食物150"）</summary>
        private static string FormatSide(int food, int coal, int ore, int gateHp)
        {
            if (food   > 0) return $"食物{food}";
            if (coal   > 0) return $"煤炭{coal}";
            if (ore    > 0) return $"矿石{ore}";
            if (gateHp > 0) return $"城门+{gateHp}HP";
            return "";
        }
    }
}
