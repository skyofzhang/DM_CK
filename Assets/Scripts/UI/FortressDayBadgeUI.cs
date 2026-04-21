using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §36 / §36.5 / §36.5.1 堡垒日徽标 UI（MVP 极简版）。
    ///
    /// 展示：
    ///   - 当前房间堡垒日 Lv.X
    ///   - 今日闯关上限进度 X/150（cap 达标时红色高亮，临近时橙色）
    ///   - reason 高亮短暂 tint（promoted 绿 / demoted 红 / cap_blocked 橙 / cap_reset 蓝）
    ///
    /// 挂载：Canvas（常驻）；Prefab 绑定留给人工（_dayText/_capText/_icon）。
    /// 若 Inspector 字段未绑定，显示逻辑降级为 Debug.Log，不崩溃。
    /// </summary>
    public class FortressDayBadgeUI : MonoBehaviour
    {
        public static FortressDayBadgeUI Instance { get; private set; }

        [Header("文本字段（Prefab 绑定由人工）")]
        [SerializeField] private TMP_Text _dayText;       // "堡垒日 Lv.43"
        [SerializeField] private TMP_Text _capText;       // "今日 120/150"
        [SerializeField] private Image    _icon;          // 徽标图标（reason tint 用）

        [Header("颜色配置")]
        [SerializeField] private Color _colorNormal       = Color.white;
        [SerializeField] private Color _colorPromoted     = new Color(0.30f, 1.00f, 0.50f); // 绿
        [SerializeField] private Color _colorDemoted      = new Color(1.00f, 0.30f, 0.30f); // 红
        [SerializeField] private Color _colorNewbieGuard  = new Color(0.70f, 0.90f, 1.00f); // 浅蓝
        [SerializeField] private Color _colorCapBlocked   = new Color(1.00f, 0.55f, 0.10f); // 橙（警示）
        [SerializeField] private Color _colorCapReset     = new Color(0.40f, 0.85f, 1.00f); // 蓝（刷新）
        [SerializeField] private Color _colorCapWarn      = new Color(1.00f, 0.75f, 0.20f); // 金橙（接近上限）
        [SerializeField] private Color _colorCapFull      = new Color(1.00f, 0.30f, 0.30f); // 红（已达上限）
        [SerializeField] private Color _colorCapNormal    = Color.white;

        [Header("阈值")]
        [Tooltip("cap 接近上限的警示阈值百分比（默认 80%）")]
        [SerializeField] private float _capWarnRatio = 0.8f;

        [Header("Reason tint 动画")]
        [Tooltip("reason 触发后 tint 保持时长（秒）")]
        [SerializeField] private float _tintHoldSec = 1.5f;

        // 当前缓存（供外部 UI 读取）
        public int CurrentFortressDay { get; private set; } = 1;
        public int DailyGained        { get; private set; }
        public int DailyCapMax        { get; private set; }
        public bool DailyCapBlocked   { get; private set; }

        private Coroutine _tintResetCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this) { /* 保留先进入的实例 */ return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>SurvivalGameManager.HandleFortressDayChanged 调用。</summary>
        public void UpdateFortressDay(FortressDayChangedData data)
        {
            if (data == null) return;

            CurrentFortressDay = data.newFortressDay;
            DailyGained        = data.dailyFortressDayGained;
            DailyCapMax        = data.dailyCapMax;
            DailyCapBlocked    = data.dailyCapBlocked;

            RefreshDayText();
            RefreshCapText();
            ApplyReasonTint(data.reason);

            if (_dayText == null && _capText == null)
            {
                Debug.Log($"[FortressDayBadgeUI] (未绑定字段) Lv.{data.newFortressDay} {data.dailyFortressDayGained}/{data.dailyCapMax} reason={data.reason}");
            }
        }

        /// <summary>外部手动刷新（如 room_state 断线重连时）。</summary>
        public void SetSnapshot(int fortressDay, int dailyGained, int dailyCapMax, bool dailyCapBlocked)
        {
            CurrentFortressDay = fortressDay;
            DailyGained        = dailyGained;
            DailyCapMax        = dailyCapMax;
            DailyCapBlocked    = dailyCapBlocked;
            RefreshDayText();
            RefreshCapText();
        }

        private void RefreshDayText()
        {
            if (_dayText != null)
                _dayText.text = $"堡垒日 Lv.{CurrentFortressDay}";
        }

        private void RefreshCapText()
        {
            if (_capText == null) return;

            // cap 功能未启用 / 服务端未下发 → 隐藏
            if (DailyCapMax <= 0)
            {
                _capText.text = string.Empty;
                _capText.gameObject.SetActive(false);
                return;
            }
            _capText.gameObject.SetActive(true);
            _capText.text = $"今日 {DailyGained}/{DailyCapMax}";

            // 颜色分级：达上限红 / 接近上限金橙 / 其他白
            if (DailyCapBlocked || DailyGained >= DailyCapMax)
                _capText.color = _colorCapFull;
            else if (DailyCapMax > 0 && DailyGained >= Mathf.CeilToInt(DailyCapMax * _capWarnRatio))
                _capText.color = _colorCapWarn;
            else
                _capText.color = _colorCapNormal;
        }

        /// <summary>按 reason 短暂 tint 徽标图标。</summary>
        private void ApplyReasonTint(string reason)
        {
            if (_icon == null) return;

            Color tint;
            switch (reason)
            {
                case "promoted":         tint = _colorPromoted;    break;
                case "demoted":          tint = _colorDemoted;     break;
                case "newbie_protected": tint = _colorNewbieGuard; break;
                case "cap_blocked":      tint = _colorCapBlocked;  break;
                case "cap_reset":        tint = _colorCapReset;    break;
                default:                 tint = _colorNormal;      break;
            }
            _icon.color = tint;

            if (_tintResetCoroutine != null) StopCoroutine(_tintResetCoroutine);
            if (gameObject.activeInHierarchy)
                _tintResetCoroutine = StartCoroutine(ResetTintAfter(_tintHoldSec));
        }

        private System.Collections.IEnumerator ResetTintAfter(float sec)
        {
            yield return new WaitForSeconds(sec);
            if (_icon != null) _icon.color = _colorNormal;
            _tintResetCoroutine = null;
        }
    }
}
