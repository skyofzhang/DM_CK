using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §39.7 商店购买双确认弹窗（B 类 ≥1000 主播 HUD 购买触发）。
    ///
    /// 触发：服务端推 shop_purchase_confirm_prompt → SurvivalGameManager 路由到本组件 Show()。
    /// 行为：
    ///   - 显示价格 + 商品名 + 倒计时（基于 expiresAt，5s TTL）
    ///   - 确认 → ShopUI.SendShopPurchase(itemId, pendingId)；立即关闭
    ///   - 取消 / 倒计时结束 → 不发消息，服务端自动清理 pending
    ///
    /// 挂载：Canvas（always-active）；_panel 初始 inactive。Prefab 绑定由人工补。
    /// </summary>
    public class ShopConfirmDialogUI : MonoBehaviour
    {
        public static ShopConfirmDialogUI Instance { get; private set; }

        // audit-r12 GAP-B02：§17.16 互斥组 B 短时弹窗 modal id（5s TTL,排队不阻塞）
        private const string MODAL_B_ID = "shop_confirm_dialog";

        // ==================== Inspector 字段 ====================

        [Header("面板根（初始 inactive）")]
        [SerializeField] private GameObject _panel;

        [Header("标题 / 价格 / 倒计时 / 按钮")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _priceText;
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private Button   _btnConfirm;
        [SerializeField] private Button   _btnCancel;

        // ==================== 运行时状态 ====================

        private string    _pendingId;
        private string    _itemId;
        private long      _expiresAtUnixMs;
        private Coroutine _timerCoroutine;

        // ==================== 生命周期 ====================

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (_panel != null) _panel.SetActive(false);
        }

        private void Start()
        {
            if (_btnConfirm != null) _btnConfirm.onClick.AddListener(OnConfirmClicked);
            if (_btnCancel  != null) _btnCancel.onClick.AddListener(OnCancelClicked);
        }

        private void OnDestroy()
        {
            ModalRegistry.ReleaseB(MODAL_B_ID);  // audit-r12 GAP-B02 兜底释放
            if (Instance == this) Instance = null;
        }

        // ==================== 对外接口 ====================

        public void Show(ShopPurchaseConfirmPromptData data)
        {
            if (data == null) return;

            _pendingId       = data.pendingId;
            _itemId          = data.itemId;
            _expiresAtUnixMs = data.expiresAt;

            if (_panel == null)
            {
                // 降级：面板未绑定时 Log，仍允许上层通过事件发确认
                Debug.Log($"[ShopConfirmDialogUI] Show（面板未绑定，降级 Log）pendingId={data.pendingId} itemId={data.itemId} price={data.price} expiresAt={data.expiresAt}");
                return;
            }

            string itemName = SurvivalGameManager.GetShopItemDisplayName(data.itemId);
            if (_titleText != null) _titleText.text = $"确认购买 {itemName}？";
            if (_priceText != null) _priceText.text = $"价格：{data.price}";
            if (_timerText != null) _timerText.text = ComputeRemainSecText();

            // audit-r12 GAP-B02：§17.16 互斥组 B 短时弹窗注册（B 类 FIFO 不抢占）
            ModalRegistry.RequestB(MODAL_B_ID, null);

            _panel.SetActive(true);

            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
            _timerCoroutine = StartCoroutine(TickAndExpire());

            Debug.Log($"[ShopConfirmDialogUI] 显示双确认：itemId={data.itemId} price={data.price} pendingId={data.pendingId}");
        }

        // ==================== 按钮回调 ====================

        private void OnConfirmClicked()
        {
            // 发 shop_purchase 携带 pendingId
            if (!string.IsNullOrEmpty(_itemId))
                ShopUI.SendShopPurchase(_itemId, _pendingId);
            else
                Debug.LogWarning("[ShopConfirmDialogUI] OnConfirmClicked: _itemId 为空，忽略");
            ClosePanel();
        }

        private void OnCancelClicked()
        {
            // 不发消息；服务端 pending 5s 自动过期
            Debug.Log($"[ShopConfirmDialogUI] 用户点取消 pendingId={_pendingId}");
            ClosePanel();
        }

        // ==================== 计时器 ====================

        private IEnumerator TickAndExpire()
        {
            while (_panel != null && _panel.activeSelf)
            {
                long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                if (nowMs >= _expiresAtUnixMs)
                {
                    Debug.Log($"[ShopConfirmDialogUI] 倒计时到期，自动关闭 pendingId={_pendingId}（服务端自动清理 pending）");
                    ClosePanel();
                    yield break;
                }
                if (_timerText != null) _timerText.text = ComputeRemainSecText();
                yield return new WaitForSeconds(0.2f);
            }
        }

        private string ComputeRemainSecText()
        {
            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long remainMs = _expiresAtUnixMs - nowMs;
            if (remainMs < 0) remainMs = 0;
            int sec = Mathf.CeilToInt(remainMs / 1000f);
            return $"{sec}s";
        }

        private void ClosePanel()
        {
            if (_timerCoroutine != null) { StopCoroutine(_timerCoroutine); _timerCoroutine = null; }
            if (_panel != null) _panel.SetActive(false);
            _pendingId       = null;
            _itemId          = null;
            _expiresAtUnixMs = 0;
            ModalRegistry.ReleaseB(MODAL_B_ID);  // audit-r12 GAP-B02
        }
    }
}
