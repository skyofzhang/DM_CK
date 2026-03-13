using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrscfZ.Core;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// 生存游戏大厅界面控制器（仅 Idle 状态）。
    /// 挂载在 Canvas（始终激活，符合 Rule #7）。
    /// 连接成功 + Idle 时显示大厅面板（首次连接入口）。
    /// - Idle：尚未建立会话，按钮文字"▶ 开始游戏"
    /// - Waiting 状态不再显示此面板，改由 PreGameBannerUI 接管
    ///   （让 3D 战场可见，玩家加入后主播点"开始挑战"才启动游戏逻辑）
    /// </summary>
    public class SurvivalIdleUI : MonoBehaviour
    {
        [Header("面板根节点")]
        [SerializeField] private GameObject _panel;

        [Header("核心按钮")]
        [SerializeField] private Button   _startBtn;     // ▶ 开始玩法
        [SerializeField] private Button   _rankingBtn;   // 排行榜入口
        [SerializeField] private Button   _settingsBtn;  // 设置（占位）

        [Header("关联面板（可选）")]
        [SerializeField] private SurvivalRankingUI  _rankingPanel;  // 排行榜面板引用
        [SerializeField] private SurvivalSettingsUI _settingsPanel; // 设置面板引用

        [Header("文字")]
        [SerializeField] private TMP_Text _statusText;   // 状态提示
        [SerializeField] private TMP_Text _serverStatus; // "已连接 ✓" / "重连中..."
        [SerializeField] private TMP_Text _titleText;    // 游戏标题

        // ==================== 生命周期 ====================

        private void Start()
        {
            // 按钮绑定
            if (_startBtn != null)
                _startBtn.onClick.AddListener(OnStartClicked);

            if (_rankingBtn != null)
                _rankingBtn.onClick.AddListener(OnRankingClicked);

            if (_settingsBtn != null)
                _settingsBtn.onClick.AddListener(OnSettingsClicked);

            // 订阅网络事件
            var net = NetworkManager.Instance;
            if (net != null)
            {
                net.OnConnected    += OnConnected;
                net.OnDisconnected += OnDisconnected;
            }

            // 订阅游戏状态事件
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
                sgm.OnStateChanged += OnStateChanged;

            // 初始标题
            if (_titleText != null)
                _titleText.text = "冬日生存法则";

            // 初始化（防止场景已连接+Idle 时漏显）
            RefreshVisibility();
        }

        private void OnDestroy()
        {
            var net = NetworkManager.Instance;
            if (net != null)
            {
                net.OnConnected    -= OnConnected;
                net.OnDisconnected -= OnDisconnected;
            }

            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
                sgm.OnStateChanged -= OnStateChanged;
        }

        // ==================== 事件回调 ====================

        private void OnConnected()                                       => RefreshVisibility();
        private void OnDisconnected(string _)                           => HidePanel();
        private void OnStateChanged(SurvivalGameManager.SurvivalState s) => RefreshVisibility();

        // ==================== 显隐逻辑 ====================

        private void RefreshVisibility()
        {
            var net = NetworkManager.Instance;
            var sgm = SurvivalGameManager.Instance;

            bool isConnected = net != null && net.IsConnected;
            var  state       = sgm?.State ?? SurvivalGameManager.SurvivalState.Idle;

            // 只在 Idle 状态显示 LobbyPanel（首次连接入口）
            // Waiting 状态由 PreGameBannerUI 负责，3D 战场直接可见
            bool isIdle = state == SurvivalGameManager.SurvivalState.Idle;

            if (isConnected && isIdle)
                ShowPanel();
            else
                HidePanel();

            // 更新连接状态文字
            if (_serverStatus != null)
                _serverStatus.text = isConnected ? "已连接 ✓" : "连接中...";
        }

        private void ShowPanel()
        {
            if (_panel != null) _panel.SetActive(true);

            if (_statusText != null)
                _statusText.text = "等待主播开始游戏...";

            if (_startBtn != null)
            {
                _startBtn.interactable = true;
                var btnLabel = _startBtn.GetComponentInChildren<TMP_Text>();
                if (btnLabel != null)
                    btnLabel.text = "▶ 开始游戏";
            }
        }

        private void HidePanel()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        // ==================== 按钮回调 ====================

        private void OnStartClicked()
        {
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null)
            {
                Debug.LogWarning("[SurvivalIdleUI] SurvivalGameManager not found!");
                return;
            }

            // 防止重复点击
            if (_startBtn != null) _startBtn.interactable = false;
            if (_statusText != null) _statusText.text = "进入战场，请选择难度...";

            // 进入 Waiting 阶段（不直接发 start_game！）
            // Waiting → DifficultySelectUI(选难度) → PreGameBannerUI(开始挑战) → RequestStartGame()
            sgm.RequestEnterWaiting();
            // 用户主动点击才允许难度选择面板弹出，防止自动连接后闪现
            DifficultySelectUI.Instance?.ShowByUserAction();
            Debug.Log("[SurvivalIdleUI] 切换 Waiting，等待主播选择难度后点击开始挑战");
        }

        private void OnRankingClicked()
        {
            if (_rankingPanel == null)
                _rankingPanel = FindObjectOfType<SurvivalRankingUI>(true);

            if (_rankingPanel != null)
                _rankingPanel.TogglePanel();
            else
                Debug.Log("[SurvivalIdleUI] SurvivalRankingUI 未找到，忽略排行榜按钮");
        }

        private void OnSettingsClicked()
        {
            if (_settingsPanel == null)
                _settingsPanel = FindObjectOfType<SurvivalSettingsUI>(true);

            if (_settingsPanel != null)
                _settingsPanel.TogglePanel();
            else
                Debug.Log("[SurvivalIdleUI] SurvivalSettingsUI 未找到，忽略设置按钮");
        }
    }
}
