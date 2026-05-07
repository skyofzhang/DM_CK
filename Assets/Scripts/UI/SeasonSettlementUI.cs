using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §36.9 / audit-r4 新建：赛季结算面板 UI（MVP 骨架）。
    /// 订阅 SurvivalGameManager.OnSeasonSettlement 显示：
    ///   - 旧赛季 ID + 下一赛季主题预告
    ///   - survivingRooms 跨房间幸存数
    ///   - topContributors[] 跨房间 Top10
    /// ModalRegistry A 类互斥 priority=80（高于 WaitingPhaseUI=50）。
    /// </summary>
    public class SeasonSettlementUI : MonoBehaviour
    {
        private const string MODAL_A_ID = "season_settlement";
        private const int    MODAL_PRIORITY = 80;
        private const string AlibabaFont = "Fonts/AlibabaPuHuiTi-3-85-Bold SDF";
        private const string FallbackFont = "Fonts/ChineseFont SDF";

        [SerializeField] private GameObject _panel;
        [SerializeField] private TextMeshProUGUI _lblTitle;          // "S3 结束 — 下赛季主题：血月"
        [SerializeField] private TextMeshProUGUI _lblSurvivingRooms; // "全服幸存房间：128 间"
        [SerializeField] private TextMeshProUGUI _lblTopList;        // 跨房 Top10 贡献（多行）
        [SerializeField] private Button _btnClose;

        private bool _seasonSubscribed;

        private void Awake()
        {
            if (_panel == null) _panel = gameObject;
            if (_panel != null && _panel != gameObject) _panel.SetActive(false);
            TryLoadFont(_lblTitle);
            TryLoadFont(_lblSurvivingRooms);
            TryLoadFont(_lblTopList);
            if (_btnClose != null) _btnClose.onClick.AddListener(HideAndRelease);

            TrySubscribe();
        }

        private void Start()
        {
            TrySubscribe();
            if (_panel != null) _panel.SetActive(false);
        }

        private void OnDestroy()
        {
            Unsubscribe();
            ModalRegistry.Release(MODAL_A_ID);
        }

        private void Update()
        {
            if (!_seasonSubscribed) TrySubscribe();
        }

        private void TrySubscribe()
        {
            if (_seasonSubscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;
            sgm.OnSeasonSettlement += HandleSeasonSettlement;
            _seasonSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_seasonSubscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null) sgm.OnSeasonSettlement -= HandleSeasonSettlement;
            _seasonSubscribed = false;
        }

        private void HandleSeasonSettlement(SeasonSettlementData data)
        {
            if (data == null) return;
            if (!ModalRegistry.Request(MODAL_A_ID, MODAL_PRIORITY, () =>
            {
                if (_panel != null) _panel.SetActive(false);
            }))
            {
                Debug.Log("[SeasonSettlementUI] Modal A blocked by higher-priority panel; skip render.");
                return;
            }
            if (_panel != null) _panel.SetActive(true);

            if (_lblTitle != null)
                _lblTitle.text = $"S{data.seasonId} 赛季结束\n下赛季主题预告：{LocalizeTheme(data.nextThemeId)}";

            if (_lblSurvivingRooms != null)
                _lblSurvivingRooms.text = $"全服幸存房间：{data.survivingRooms} 间";

            if (_lblTopList != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine("<b>跨房 Top10 贡献</b>");
                if (data.topContributors != null && data.topContributors.Length > 0)
                {
                    for (int i = 0; i < data.topContributors.Length && i < 10; i++)
                    {
                        var e = data.topContributors[i];
                        if (e == null) continue;
                        sb.AppendLine($"{i + 1}. {e.playerName}  —  {e.contribution}");
                    }
                }
                else
                {
                    sb.AppendLine("  (本赛季无有效数据)");
                }
                _lblTopList.text = sb.ToString();
            }
        }

        private void HideAndRelease()
        {
            if (_panel != null) _panel.SetActive(false);
            ModalRegistry.Release(MODAL_A_ID);
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
