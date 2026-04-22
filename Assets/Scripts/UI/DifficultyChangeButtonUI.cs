using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrscfZ.Survival;
using DrscfZ.Core;

namespace DrscfZ.UI
{
    /// <summary>
    /// §34.4 E9 周期/赛季间难度切换 —— 主播专用"切换难度"按钮
    ///
    /// 显示条件（v1 第一版仅支持 recovery-first-day 入口；next_season 入口延后）：
    ///   1. 仅主播（isRoomCreator=true）
    ///   2. 恢复期第一个白天：收到 phase_changed{variant="recovery", phase="day"} 后 → 显示
    ///   3. 切到 night 或非 recovery 白天 → 隐藏
    ///
    /// 点击后弹出 3 个难度按钮（简易实现，无需复用 SurvivalLoadingUI 风格）：
    ///   轻松 / 困难 / 恐怖 → 调 SurvivalGameManager.SendChangeDifficulty(selected, "next_night")
    ///
    /// 说明（未完成项）：
    ///   next_season 入口（赛季切换 30s 窗口）依赖 SeasonSettlementUI 的存在周期信号，
    ///   当前项目的 SeasonSettlementData 只携带 seasonId/nextThemeId，无"窗口开始/结束"显式信号，
    ///   本 UI v1 暂不实现赛季切换按钮；后续可订阅 OnSeasonSettlement 显示 30s 后自动隐藏。
    ///
    /// 挂载：BroadcasterPanel/DifficultyChangeButton（常驻激活）。
    ///
    /// Inspector 必填：
    ///   _buttonRoot      — 主按钮根（恢复期首日可见）
    ///   _mainButton      — 主按钮 Button（点击展开 3 个难度）
    ///   _mainButtonLabel — 主按钮文字 TMP
    ///   _choicePanel     — 难度选择面板（默认隐藏，点击主按钮时显示）
    ///   _easyBtn / _normalBtn / _hardBtn — 3 个难度按钮
    /// </summary>
    public class DifficultyChangeButtonUI : MonoBehaviour
    {
        [Header("主按钮")]
        [SerializeField] private GameObject _buttonRoot;
        [SerializeField] private Button     _mainButton;
        [SerializeField] private TextMeshProUGUI _mainButtonLabel;

        [Header("难度选择面板（点击主按钮展开）")]
        [SerializeField] private GameObject _choicePanel;
        [SerializeField] private Button     _easyBtn;
        [SerializeField] private Button     _normalBtn;
        [SerializeField] private Button     _hardBtn;

        // 主播身份判定（复用 BroadcasterPanel 判定方式）
        private bool _isRoomCreator = false;

        // 恢复期首日可见窗口
        private bool _isFirstRecoveryDay = false;

        private bool _sgmSubscribed = false;
        private bool _netSubscribed = false;

        // ── 生命周期 ──────────────────────────────────────────────────────

        private void Start()
        {
            if (_buttonRoot  != null) _buttonRoot.SetActive(false);
            if (_choicePanel != null) _choicePanel.SetActive(false);
            if (_mainButtonLabel != null && string.IsNullOrEmpty(_mainButtonLabel.text))
                _mainButtonLabel.text = "切换难度";

            // 按钮绑定
            _mainButton?.onClick.AddListener(OnMainClicked);
            _easyBtn   ?.onClick.AddListener(() => OnDifficultyChosen("easy"));
            _normalBtn ?.onClick.AddListener(() => OnDifficultyChosen("normal"));
            _hardBtn   ?.onClick.AddListener(() => OnDifficultyChosen("hard"));

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
                sgm.OnPhaseChanged += HandlePhaseChanged;
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
                sgm.OnPhaseChanged -= HandlePhaseChanged;
            }
            _sgmSubscribed = false;
            var net = NetworkManager.Instance;
            if (_netSubscribed && net != null)
            {
                net.OnMessageReceived -= HandleNetMessage;
            }
            _netSubscribed = false;
        }

        // ── 主播身份判定 ──────────────────────────────────────────────────

        private void HandleNetMessage(string type, string dataJson)
        {
            if (type != "join_room_confirm") return;
            _isRoomCreator = ParseBoolField(dataJson, "isRoomCreator");
            RefreshVisibility();
        }

        private static bool ParseBoolField(string json, string field)
        {
            if (string.IsNullOrEmpty(json)) return false;
            int idx = json.IndexOf("\"" + field + "\"");
            if (idx < 0) return false;
            int colon = json.IndexOf(':', idx);
            if (colon < 0) return false;
            int start = colon + 1;
            while (start < json.Length && (json[start] == ' ' || json[start] == '\t')) start++;
            if (start + 4 > json.Length) return false;
            return json.Substring(start, 4).ToLowerInvariant() == "true";
        }

        // ── 阶段切换判定 ──────────────────────────────────────────────────

        private void HandlePhaseChanged(PhaseChangedData data)
        {
            if (data == null) return;

            string variant = string.IsNullOrEmpty(data.variant) ? "normal" : data.variant;

            // 恢复期白天 → 首次启用
            if (data.phase == "day" && variant == "recovery")
            {
                _isFirstRecoveryDay = true;
            }
            // 夜晚或非 recovery 白天 → 隐藏（下次 recovery 白天重新显示）
            else
            {
                _isFirstRecoveryDay = false;
                if (_choicePanel != null) _choicePanel.SetActive(false);
            }

            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            if (_buttonRoot == null) return;
            bool show = _isRoomCreator && _isFirstRecoveryDay;
            _buttonRoot.SetActive(show);
            if (!show && _choicePanel != null) _choicePanel.SetActive(false);
        }

        // ── 按钮回调 ──────────────────────────────────────────────────────

        private void OnMainClicked()
        {
            if (_choicePanel == null) return;
            _choicePanel.SetActive(!_choicePanel.activeSelf);
        }

        private void OnDifficultyChosen(string difficulty)
        {
            SurvivalGameManager.Instance?.SendChangeDifficulty(difficulty, "next_night");
            if (_choicePanel != null) _choicePanel.SetActive(false);
            // 发送后立即隐藏主按钮（一次性操作，避免反复修改）
            if (_buttonRoot != null) _buttonRoot.SetActive(false);
            _isFirstRecoveryDay = false;
            Debug.Log($"[DifficultyChangeButtonUI] §34.4 E9 发送 change_difficulty difficulty={difficulty} applyAt=next_night");
        }
    }
}
