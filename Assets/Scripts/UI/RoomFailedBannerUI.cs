using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §36.5 房间失败降级 Banner UI（MVP 极简版）。
    ///
    /// 触发：room_failed 推送（与 fortress_day_changed 同帧）→ Show(data)。
    ///
    /// 展示：
    ///   - 降级信息："城门失守 堡垒日 Lv.45 → Lv.40"
    ///   - 失败原因中文描述（gate_breached / food_depleted / temp_freeze / all_dead）
    ///   - 新手保护期（Day 1–10）特殊文案："新手保护期（不降级）"
    ///
    /// 挂载：Canvas（常驻）；Prefab 绑定由人工（_panel/_titleText/_reasonText/_closeButton）。
    /// 若字段未绑定，所有展示降级为 Debug.Log + AnnouncementUI 兜底。
    /// </summary>
    public class RoomFailedBannerUI : MonoBehaviour
    {
        public static RoomFailedBannerUI Instance { get; private set; }

        [Header("面板根（初始 inactive）")]
        [SerializeField] private GameObject _panel;

        [Header("文本字段")]
        [SerializeField] private TMP_Text _titleText;         // "房间失守"
        [SerializeField] private TMP_Text _fortressChangeText; // "堡垒日 Lv.45 → Lv.40"
        [SerializeField] private TMP_Text _reasonText;        // 失败原因中文
        [SerializeField] private Button   _closeButton;

        [Header("动画")]
        [Tooltip("Banner 自动关闭时长（秒）；0 或负数 = 不自动关")]
        [SerializeField] private float _autoCloseSec = 6f;

        private Coroutine _autoCloseCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this) { return; }
            Instance = this;
            EnsureFallbackUI();
            if (_panel != null) _panel.SetActive(false);
        }

        private void EnsureFallbackUI()
        {
            if (_panel != null && _titleText != null) return;
            if (transform.parent == null)
                transform.SetParent(RuntimeUIFactory.GetCanvasTransform(), false);

            _panel = RuntimeUIFactory.CreatePanel(transform, "RoomFailedBannerPanel",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -150f), new Vector2(680f, 190f),
                new Color(0.40f, 0.06f, 0.08f, 0.94f));
            RuntimeUIFactory.AddVerticalLayout(_panel, 8f, new RectOffset(24, 24, 18, 18));

            _titleText = RuntimeUIFactory.CreateText(_panel.transform, "Title", "房间失守", 30f,
                new Color(1f, 0.92f, 0.72f), TextAlignmentOptions.Center, new Vector2(620f, 40f));
            RuntimeUIFactory.AddLayoutElement(_titleText.gameObject, 42f);

            _fortressChangeText = RuntimeUIFactory.CreateText(_panel.transform, "FortressChange", "堡垒日 Lv.0 -> Lv.0", 22f,
                new Color(1f, 0.72f, 0.38f), TextAlignmentOptions.Center, new Vector2(620f, 32f));
            RuntimeUIFactory.AddLayoutElement(_fortressChangeText.gameObject, 34f);

            _reasonText = RuntimeUIFactory.CreateText(_panel.transform, "Reason", "未知原因", 22f,
                Color.white, TextAlignmentOptions.Center, new Vector2(620f, 32f));
            RuntimeUIFactory.AddLayoutElement(_reasonText.gameObject, 34f);

            _closeButton = RuntimeUIFactory.CreateButton(_panel.transform, "CloseButton", "关闭", out _,
                new Color(0.72f, 0.18f, 0.18f, 1f), new Vector2(180f, 44f));
            RuntimeUIFactory.AddLayoutElement(_closeButton.gameObject, 44f);
        }

        private void Start()
        {
            if (_closeButton != null) _closeButton.onClick.AddListener(Hide);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>SurvivalGameManager.HandleRoomFailed 调用。</summary>
        public void Show(RoomFailedData data)
        {
            if (data == null) return;

            string reasonZh = FormatDemotionReason(data.demotionReason);

            if (_panel == null || _titleText == null)
            {
                // 降级：未绑定 Prefab 字段时走 AnnouncementUI
                string subText = data.newbieProtected
                    ? $"{reasonZh} · 新手保护期（不降级）"
                    : $"{reasonZh} · 堡垒日 Lv.{data.oldFortressDay} → Lv.{data.newFortressDay}";
                UI.AnnouncementUI.Instance?.ShowAnnouncement(
                    "房间失守",
                    subText,
                    new Color(1f, 0.3f, 0.3f),
                    4f);
                Debug.Log($"[RoomFailedBannerUI] (未绑定 _panel) 降级到 AnnouncementUI: {subText}");
                return;
            }

            if (_titleText != null)
                _titleText.text = data.newbieProtected ? "房间失守（新手保护）" : "房间失守";

            if (_fortressChangeText != null)
            {
                if (data.newbieProtected && data.oldFortressDay == data.newFortressDay)
                    _fortressChangeText.text = $"堡垒日 Lv.{data.newFortressDay}（不降级）";
                else
                    _fortressChangeText.text = $"堡垒日 Lv.{data.oldFortressDay} → Lv.{data.newFortressDay}";
            }

            if (_reasonText != null)
                _reasonText.text = reasonZh;

            _panel.SetActive(true);

            if (_autoCloseCoroutine != null) StopCoroutine(_autoCloseCoroutine);
            if (_autoCloseSec > 0f && gameObject.activeInHierarchy)
                _autoCloseCoroutine = StartCoroutine(AutoClose(_autoCloseSec));
        }

        public void Hide()
        {
            if (_autoCloseCoroutine != null) { StopCoroutine(_autoCloseCoroutine); _autoCloseCoroutine = null; }
            if (_panel != null) _panel.SetActive(false);
        }

        private IEnumerator AutoClose(float sec)
        {
            yield return new WaitForSeconds(sec);
            Hide();
        }

        /// <summary>§16.1 + §36.5 demotionReason 枚举 → 中文（与 survival_game_ended.reason 对齐）</summary>
        public static string FormatDemotionReason(string reason)
        {
            if (string.IsNullOrEmpty(reason)) return "未知原因";
            switch (reason)
            {
                case "gate_breached": return "城门失守";
                case "food_depleted": return "食物耗尽";
                case "temp_freeze":   return "炉温冻结";
                case "all_dead":      return "矿工全灭";
                default:              return reason;
            }
        }
    }
}
