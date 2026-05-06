using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrscfZ.Core;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// 战场等待覆盖层（Waiting 状态 + 难度已选择 时可见）
    ///
    /// 显示条件：已连接 + SurvivalState.Waiting + DifficultyLevel != None
    /// 功能：
    ///   - 全屏半透明遮罩，屏幕正中央显示开始挑战按钮
    ///   - 显示已加入玩家数量
    ///   - [开始挑战] 按钮 → 3秒倒计时 → 启动游戏逻辑
    ///
    /// AI准则 #7：挂在 Canvas（始终激活对象）上
    /// AI准则 #2：面板在 Scene 中预创建，初始 inactive，通过 SetActive 控制显隐
    /// </summary>
    public class PreGameBannerUI : MonoBehaviour
    {
        public static PreGameBannerUI Instance { get; private set; }

        [Header("面板根节点（SetActive 控制显隐）")]
        [SerializeField] private GameObject _panel;

        [Header("文字")]
        [SerializeField] private TMP_Text _titleText;       // "极地生存法则"
        [SerializeField] private TMP_Text _statusText;      // "等待玩家加入... | 已有 N 位守护者"
        [SerializeField] private TMP_Text _playerCountText; // 倒计时大字 / 玩家人数大字

        [Header("按钮")]
        [SerializeField] private Button   _startBtn;        // 开始挑战

        // 本地计数
        private int  _playerCount    = 0;
        private bool _isCountingDown = false;

        // ==================== 生命周期 ====================

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            // 绑定字体
            var font = Resources.Load<TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF") ?? Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
            if (font != null)
            {
                if (_titleText       != null) _titleText.font       = font;
                if (_statusText      != null) _statusText.font      = font;
                if (_playerCountText != null) _playerCountText.font = font;
                if (_startBtn != null)
                    foreach (var tmp in _startBtn.GetComponentsInChildren<TMP_Text>())
                        tmp.font = font;
            }

            // 按钮绑定
            _startBtn?.onClick.AddListener(OnStartClicked);

            // 设置标题
            if (_titleText != null) _titleText.text = "极地生存法则";

            // 订阅网络事件
            var net = NetworkManager.Instance;
            if (net != null)
            {
                net.OnConnected    += RefreshVisibility;
                net.OnDisconnected += HandleDisconnected;
            }

            // 订阅游戏状态事件
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnStateChanged  += HandleStateChanged;
                sgm.OnPlayerJoined  += OnPlayerJoined;
                sgm.OnDifficultySet += HandleDifficultySet;
            }

            // 初始化
            RefreshVisibility();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            var net = NetworkManager.Instance;
            if (net != null)
            {
                net.OnConnected    -= RefreshVisibility;
                net.OnDisconnected -= HandleDisconnected;
            }

            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnStateChanged  -= HandleStateChanged;
                sgm.OnPlayerJoined  -= OnPlayerJoined;
                sgm.OnDifficultySet -= HandleDifficultySet;
            }
        }

        private void HandleDisconnected(string reason) => HidePanel();
        private void HandleStateChanged(SurvivalGameManager.SurvivalState state) => RefreshVisibility();
        private void HandleDifficultySet(SurvivalGameManager.DifficultyLevel level) => RefreshVisibility();

        // ==================== 显隐逻辑 ====================

        private void RefreshVisibility()
        {
            var net = NetworkManager.Instance;
            var sgm = SurvivalGameManager.Instance;

            bool isConnected       = net != null && net.IsConnected;
            var  state             = sgm?.State ?? SurvivalGameManager.SurvivalState.Idle;
            bool difficultyChosen  = sgm != null &&
                                     sgm.SelectedDifficulty != SurvivalGameManager.DifficultyLevel.None;

            // 只在 Waiting + 已选难度 时显示
            bool showPanel = isConnected &&
                             state == SurvivalGameManager.SurvivalState.Waiting &&
                             difficultyChosen;

            if (showPanel)
                ShowPanel();
            else
                HidePanel();
        }

        private void ShowPanel()
        {
            if (_panel != null) _panel.SetActive(true);

            // 重置倒计时状态
            _isCountingDown = false;
            if (_startBtn != null) _startBtn.interactable = true;

            // 同步玩家数量
            UpdatePlayerCount();
        }

        private void HidePanel()
        {
            if (_panel != null) _panel.SetActive(false);
            StopAllCoroutines();
            _isCountingDown = false;
        }

        // ==================== 事件回调 ====================

        private void OnPlayerJoined(SurvivalPlayerJoinedData data)
        {
            var sgm = SurvivalGameManager.Instance;
            if (sgm?.State != SurvivalGameManager.SurvivalState.Waiting) return;

            _playerCount = WorkerManager.Instance != null
                ? WorkerManager.Instance.WorkerCount
                : _playerCount + 1;

            UpdatePlayerCount();
        }

        private void UpdatePlayerCount()
        {
            if (WorkerManager.Instance != null)
                _playerCount = WorkerManager.Instance.WorkerCount;

            // 倒计时进行中，不更新文字
            if (_isCountingDown) return;

            if (_playerCountText != null)
                _playerCountText.text = _playerCount > 0 ? _playerCount.ToString() : "0";

            if (_statusText != null)
            {
                _statusText.text = _playerCount > 0
                    ? $"已有 {_playerCount} 位守护者加入，等待主播开始！"
                    : "等待玩家加入，随时可开始挑战...";
            }
        }

        // ==================== 按钮回调 ====================

        private void OnStartClicked()
        {
            if (_isCountingDown) return;

            var sgm = SurvivalGameManager.Instance;
            if (sgm == null)
            {
                Debug.LogWarning("[PreGameBannerUI] SurvivalGameManager not found!");
                return;
            }
            if (sgm.State != SurvivalGameManager.SurvivalState.Waiting)
            {
                Debug.LogWarning($"[PreGameBannerUI] 当前状态 {sgm.State} 无法开始");
                return;
            }

            // 防止重复点击
            if (_startBtn != null) _startBtn.interactable = false;
            StartCoroutine(CountdownThenStart(sgm));
        }

        /// <summary>3秒倒计时后发起 RequestStartGame</summary>
        private IEnumerator CountdownThenStart(SurvivalGameManager sgm)
        {
            _isCountingDown = true;

            for (int i = 3; i >= 1; i--)
            {
                if (_playerCountText != null)
                {
                    _playerCountText.text  = i.ToString();
                    _playerCountText.color = new Color(1f, 0.4f, 0.2f);  // 橙红色
                }
                if (_statusText != null) _statusText.text = "游戏即将开始...";
                yield return new WaitForSeconds(1f);
            }

            if (_playerCountText != null)
            {
                _playerCountText.text  = "出发！";
                _playerCountText.color = new Color(0.2f, 1f, 0.4f);  // 绿色
            }
            yield return new WaitForSeconds(0.5f);

            _isCountingDown = false;
            sgm.RequestStartGame();
            Debug.Log("[PreGameBannerUI] 倒计时结束 → RequestStartGame()");
        }
    }
}
