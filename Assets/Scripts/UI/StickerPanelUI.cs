using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// 礼物贴纸介绍面板控制器
    /// - BtnSticker 点击 → toggle GiftInfoPanel 显示/隐藏
    /// - GiftInfoPanel 上按住拖动 → 改变面板位置（左键拖动）
    /// - 进入 Running 状态时自动重置到默认位置
    ///
    /// 挂载规则：挂在始终激活的父对象上（Canvas或GameUIPanel）
    /// GiftInfoPanel 默认位置设在屏幕右下角，不遮挡战斗画面
    /// </summary>
    public class StickerPanelUI : MonoBehaviour, IDragHandler, IBeginDragHandler
    {
        [Header("References")]
        [SerializeField] private Button btnSticker;
        [SerializeField] private GameObject giftInfoPanel;

        [SerializeField] private Button btnResetPosition;

        private RectTransform _panelRect;
        private Canvas _canvas;
        private bool _isDragging = false;
        private Vector2 _defaultPosition;

        private void Awake()
        {
            if (giftInfoPanel != null)
                _panelRect = giftInfoPanel.GetComponent<RectTransform>();

            // 查找根Canvas用于坐标转换
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null)
            {
                // 获取最顶层的Canvas（ScreenSpace-Overlay模式）
                while (_canvas.transform.parent != null)
                {
                    var parentCanvas = _canvas.transform.parent.GetComponentInParent<Canvas>();
                    if (parentCanvas != null)
                        _canvas = parentCanvas;
                    else
                        break;
                }
            }
        }

        private void Start()
        {
            // 贴纸面板默认显示
            if (giftInfoPanel != null)
                giftInfoPanel.SetActive(true);

            // 记录初始位置用于重置
            if (_panelRect != null)
                _defaultPosition = _panelRect.anchoredPosition;

            if (btnSticker != null)
            {
                btnSticker.onClick.RemoveAllListeners();
                btnSticker.onClick.AddListener(TogglePanel);
            }

            if (btnResetPosition != null)
            {
                btnResetPosition.onClick.RemoveAllListeners();
                btnResetPosition.onClick.AddListener(ResetPosition);
            }

            // 订阅 SurvivalGameManager 状态变化，Running时自动重置位置（#123修复：原用旧GameManager）
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
                sgm.OnStateChanged += HandleStateChanged;
        }

        private void OnDestroy()
        {
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
                sgm.OnStateChanged -= HandleStateChanged;
        }

        /// <summary>进入 Running 状态时重置贴纸到默认位置</summary>
        private void HandleStateChanged(SurvivalGameManager.SurvivalState newState)
        {
            if (newState == SurvivalGameManager.SurvivalState.Running)
                ResetPosition();
        }

        /// <summary>重置贴纸到默认位置</summary>
        public void ResetPosition()
        {
            if (_panelRect != null)
            {
                _panelRect.anchoredPosition = _defaultPosition;
                Debug.Log("[StickerPanelUI] Position reset to default");
            }
        }

        private void TogglePanel()
        {
            if (giftInfoPanel != null)
            {
                bool isActive = giftInfoPanel.activeSelf;
                giftInfoPanel.SetActive(!isActive);
                Debug.Log($"[StickerPanelUI] GiftInfoPanel {(!isActive ? "显示" : "隐藏")}");
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_panelRect == null) return;

            // 根据Canvas的渲染模式计算正确的偏移
            float scaleFactor = 1f;
            if (_canvas != null)
                scaleFactor = _canvas.scaleFactor;

            _panelRect.anchoredPosition += eventData.delta / scaleFactor;
        }
    }
}
