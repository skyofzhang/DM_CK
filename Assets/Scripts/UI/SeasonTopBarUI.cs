using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §36.10 / audit-r4 新建：赛季顶部条 UI（MVP 骨架）。
    /// 挂 Canvas/GameUIPanel/SeasonTopBar，常驻显示 seasonId/seasonDay/themeId/fortressDay。
    /// 订阅 SurvivalGameManager.OnSeasonState / OnGameStateReceived 更新文本。
    /// 字体：Alibaba 主 + ChineseFont fallback。
    /// </summary>
    public class SeasonTopBarUI : MonoBehaviour
    {
        private const string AlibabaFont = "Fonts/AlibabaPuHuiTi-3-85-Bold SDF";
        private const string FallbackFont = "Fonts/ChineseFont SDF";

        [SerializeField] private GameObject _panel;
        [SerializeField] private TextMeshProUGUI _lblSeason;      // "S3 · D4/7"
        [SerializeField] private TextMeshProUGUI _lblTheme;       // "主题：血月"
        [SerializeField] private TextMeshProUGUI _lblFortressDay; // "堡垒日 D17（历史最高 D25）"

        private void Awake()
        {
            if (_panel == null) _panel = gameObject;
            TryLoadFont(_lblSeason);
            TryLoadFont(_lblTheme);
            TryLoadFont(_lblFortressDay);
        }

        private void OnEnable()
        {
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;
            sgm.OnSeasonState += HandleSeasonState;
            sgm.OnRoomState   += HandleRoomState;
        }

        private void OnDisable()
        {
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;
            sgm.OnSeasonState -= HandleSeasonState;
            sgm.OnRoomState   -= HandleRoomState;
        }

        private void HandleSeasonState(SeasonStateData data)
        {
            if (data == null) return;
            if (_lblSeason != null) _lblSeason.text = $"S{data.seasonId} · D{data.seasonDay}/7";
            if (_lblTheme != null)  _lblTheme.text  = $"主题：{LocalizeTheme(data.themeId)}";
        }

        private void HandleRoomState(RoomStateData data)
        {
            if (data == null) return;
            if (_lblSeason != null) _lblSeason.text = $"S{data.currentSeasonId} · {data.themeId}";
            if (_lblFortressDay != null)
                _lblFortressDay.text = $"堡垒日 D{data.fortressDay}（最高 D{data.maxFortressDay}）";
        }

        private static string LocalizeTheme(string themeId)
        {
            switch (themeId)
            {
                case "classic_frozen": return "经典冰原";
                case "blood_moon":     return "血月";
                case "snowstorm":      return "风雪";
                case "dawn":           return "黎明";
                case "frenzy":         return "狂潮";
                case "serene":         return "宁静";
                default:               return themeId ?? "-";
            }
        }

        private static void TryLoadFont(TextMeshProUGUI label)
        {
            if (label == null) return;
            var f = Resources.Load<TMP_FontAsset>(AlibabaFont) ?? Resources.Load<TMP_FontAsset>(FallbackFont);
            if (f != null) label.font = f;
        }
    }
}
