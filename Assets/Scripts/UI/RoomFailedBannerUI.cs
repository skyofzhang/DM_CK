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
            if (_panel != null) _panel.SetActive(false);
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
