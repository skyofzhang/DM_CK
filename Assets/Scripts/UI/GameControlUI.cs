using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrscfZ.Core;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// 底部GM工具面板 - 本地测试用
    /// 左上角快速点击6次唤出
    ///
    /// 功能:
    ///   BtnConnect  — GM连接服务器（无抖音token直连）
    ///   BtnStart    — 请求开始游戏（State=Idle 时可用）
    ///   BtnReset    — 重置游戏 → Idle（让服务器回到等待状态，可重现完整7阶段流程）
    ///   BtnSimulate — 模拟弹幕开关
    ///
    /// Rule #7：挂在 BottomBar 上（默认 inactive，Running 时由 SurvivalGameplayUI 激活）
    /// 生产环境默认隐藏，只有知道暗号（6连击左上角）才能呼出
    /// </summary>
    public class GameControlUI : MonoBehaviour
    {
        [Header("Buttons - Row1")]
        public Button gmLoginButton;    // 向后兼容（SceneUpdater 使用），也可直接用
        public Button connectButton;    // WireSurvivalGMButtons 绑定的新名，优先于 gmLoginButton
        public Button startButton;      // 开始游戏（BtnStart）
        public Button pauseButton;      // 暂停游戏（BtnPause）
        public Button endButton;        // 结束游戏（BtnEnd）
        public Button resetButton;      // 重置→Idle（BtnReset）
        public Button simulateButton;   // 模拟弹幕（BtnSimulate，已移到隐藏位置）

        [Header("Buttons - Row2 礼物/事件")]
        public Button giftT1Button;     // 模拟T1礼物
        public Button giftT3Button;     // 模拟T3礼物
        public Button giftT5Button;     // 模拟T5礼物
        public Button freezeButton;     // 触发冻结事件
        public Button monsterButton;    // 召唤怪物

        /// <summary>实际使用的连接按钮（connectButton 优先，fallback gmLoginButton）</summary>
        private Button ConnectBtn => connectButton != null ? connectButton : gmLoginButton;

        [Header("Status")]
        public TextMeshProUGUI statusText;

        private bool _simEnabled = false;
        private bool _connected  = false;

        // 隐藏/唤出控制
        private CanvasGroup _cg;
        private bool        _visible  = false;
        private int         _tapCount = 0;
        private float       _tapTimer = 0f;
        private const float TAP_INTERVAL   = 0.5f;
        private const int   TAP_SHOW_PANEL = 6;

        // ==================== 生命周期 ====================

        private void Awake()
        {
            _cg = GetComponent<CanvasGroup>();
            if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
            SetVisible(false);
        }

        private void Update()
        {
            // 左上角 1/6 区域快速点击 6 次唤出/隐藏面板
            if (_tapTimer > 0f)
            {
                _tapTimer -= Time.unscaledDeltaTime;
                if (_tapTimer <= 0f) _tapCount = 0;
            }

            if (Input.GetMouseButtonDown(0))
            {
                var pos = Input.mousePosition;
                if (pos.x < Screen.width / 6f && pos.y > Screen.height * 5f / 6f)
                {
                    _tapCount++;
                    _tapTimer = TAP_INTERVAL;

                    if (_tapCount >= TAP_SHOW_PANEL)
                    {
                        _tapCount = 0;
                        SetVisible(!_visible);
                    }
                }
            }

#if UNITY_EDITOR
            // 编辑器快捷键（方便 Coplay / AI 联调，无需手动操作面板）
            // F5 = 开始模拟 / F6 = 停止模拟 / F8 = 显示/隐藏 GM 面板
            if (Input.GetKeyDown(KeyCode.F8)) SetVisible(!_visible);
            if (Input.GetKeyDown(KeyCode.F5) && _connected && !_simEnabled) OnSimulateClicked();
            if (Input.GetKeyDown(KeyCode.F6) && _connected &&  _simEnabled) OnSimulateClicked();
#endif
        }

        public void SetVisible(bool visible)
        {
            _visible = visible;
            if (_cg != null)
            {
                _cg.alpha          = visible ? 1f : 0f;
                _cg.interactable   = visible;
                _cg.blocksRaycasts = visible;
            }
        }

        private void Start()
        {
            // 注册按钮回调
            ConnectBtn?.onClick.AddListener(OnConnectClicked);
            if (startButton)    startButton.onClick.AddListener(OnStartGameClicked);
            if (pauseButton)    pauseButton.onClick.AddListener(OnPauseClicked);
            if (endButton)      endButton.onClick.AddListener(OnEndGameClicked);
            if (resetButton)    resetButton.onClick.AddListener(OnResetClicked);
            if (simulateButton) simulateButton.onClick.AddListener(OnSimulateClicked);
            if (giftT1Button)   giftT1Button.onClick.AddListener(() => OnSimulateGift(1));
            if (giftT3Button)   giftT3Button.onClick.AddListener(() => OnSimulateGift(3));
            if (giftT5Button)   giftT5Button.onClick.AddListener(() => OnSimulateGift(5));
            if (freezeButton)   freezeButton.onClick.AddListener(OnFreezeClicked);
            if (monsterButton)  monsterButton.onClick.AddListener(OnMonsterClicked);

            // 订阅网络事件
            var net = NetworkManager.Instance;
            if (net != null)
            {
                net.OnConnected    += HandleConnected;
                net.OnDisconnected += HandleDisconnected;
            }

            // 订阅游戏状态事件
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
                sgm.OnStateChanged += HandleStateChanged;

            // BottomBar 在 Running 时才激活，所以 Start() 调用时可能已经连接了。
            // 查询当前实际连接状态，避免按钮状态与实际状态不符。
            bool alreadyConnected = net != null && net.IsConnected;
            var  currentState     = sgm?.State ?? SurvivalGameManager.SurvivalState.Idle;

            if (alreadyConnected)
            {
                _connected = true;
                SetButtonText(ConnectBtn, "已连接 OK");
                string mode = (net != null && net.IsGMMode) ? "GM模式" : "直播模式";
                SetStatusText($"GM工具就绪（{mode}，状态:{currentState}）");
            }
            else
            {
                SetStatusText("GM工具就绪");
            }

            UpdateButtonStates(_connected, currentState);
        }

        private void OnDestroy()
        {
            var net = NetworkManager.Instance;
            if (net != null)
            {
                net.OnConnected    -= HandleConnected;
                net.OnDisconnected -= HandleDisconnected;
            }

            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
                sgm.OnStateChanged -= HandleStateChanged;
        }

        // ==================== 按钮回调 ====================

        /// <summary>GM连接：无抖音token时直连服务器</summary>
        private void OnConnectClicked()
        {
            if (_connected)
            {
                SetStatusText("已连接，无需重复登录");
                return;
            }
            SetStatusText("GM连接中...");
            SetButtonText(ConnectBtn, "连接中...");
            // 通过 SurvivalGameManager（会同时重置状态到 Idle）
            SurvivalGameManager.Instance?.ConnectToServer();
        }

        /// <summary>开始游戏（State=Idle 或 Waiting 时有效）</summary>
        private void OnStartGameClicked()
        {
            if (!_connected) { SetStatusText("请先连接服务器"); return; }
            var sgm = SurvivalGameManager.Instance;
            bool canStart = sgm != null &&
                (sgm.State == SurvivalGameManager.SurvivalState.Idle ||
                 sgm.State == SurvivalGameManager.SurvivalState.Waiting);
            if (!canStart)
            {
                SetStatusText($"当前状态（{sgm?.State}）无法开始");
                return;
            }
            SetStatusText("发送 start_game...");
            sgm.RequestStartGame();
        }

        /// <summary>暂停游戏</summary>
        private void OnPauseClicked()
        {
            if (!_connected) { SetStatusText("请先连接服务器"); return; }
            long ts = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            NetworkManager.Instance?.SendJson($"{{\"type\":\"pause_game\",\"timestamp\":{ts}}}");
            SetStatusText("发送 pause_game...");
        }

        /// <summary>结束游戏</summary>
        private void OnEndGameClicked()
        {
            if (!_connected) { SetStatusText("请先连接服务器"); return; }
            long ts = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            NetworkManager.Instance?.SendJson($"{{\"type\":\"end_game\",\"timestamp\":{ts}}}");
            SetStatusText("发送 end_game...");
        }

        /// <summary>模拟指定tier礼物</summary>
        private void OnSimulateGift(int tier)
        {
            if (!_connected) { SetStatusText("请先连接服务器"); return; }
            long ts = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            NetworkManager.Instance?.SendJson(
                $"{{\"type\":\"simulate_gift\",\"data\":{{\"tier\":{tier}}},\"timestamp\":{ts}}}");
            SetStatusText($"发送 simulate_gift tier={tier}...");
        }

        /// <summary>触发冻结事件</summary>
        private void OnFreezeClicked()
        {
            if (!_connected) { SetStatusText("请先连接服务器"); return; }
            long ts = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            NetworkManager.Instance?.SendJson($"{{\"type\":\"simulate_freeze\",\"timestamp\":{ts}}}");
            SetStatusText("发送 simulate_freeze...");
        }

        /// <summary>召唤怪物</summary>
        private void OnMonsterClicked()
        {
            if (!_connected) { SetStatusText("请先连接服务器"); return; }
            long ts = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            NetworkManager.Instance?.SendJson($"{{\"type\":\"simulate_monster\",\"timestamp\":{ts}}}");
            SetStatusText("发送 simulate_monster...");
        }

        /// <summary>重置游戏 → Idle：让服务器回到等待状态，可重现完整7阶段流程</summary>
        private void OnResetClicked()
        {
            if (!_connected) { SetStatusText("请先连接服务器"); return; }
            SetStatusText("发送 reset_game → Idle...");
            Debug.Log("[GM] 重置游戏 → Idle，服务器收到后将回到等待状态");
            SurvivalGameManager.Instance?.RequestResetGame();
        }

        /// <summary>模拟弹幕开关</summary>
        private void OnSimulateClicked()
        {
            if (!_connected) { SetStatusText("请先连接服务器"); return; }

            _simEnabled = !_simEnabled;
            // 服务器期望 toggle_sim { enabled: bool }（SurvivalRoom.js）
            string enabledStr = _simEnabled ? "true" : "false";
            long ts = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            NetworkManager.Instance?.SendJson(
                $"{{\"type\":\"toggle_sim\",\"data\":{{\"enabled\":{enabledStr}}},\"timestamp\":{ts}}}");
            Debug.Log($"[GM] 发送 toggle_sim enabled={_simEnabled}");

            SetButtonText(simulateButton, _simEnabled ? "停止模拟" : "模拟");
            var img = simulateButton?.GetComponent<Image>();
            if (img != null)
                img.color = _simEnabled ? new Color(0.8f, 0.2f, 0.2f) : new Color(0.9f, 0.6f, 0.1f);

            SetStatusText(_simEnabled ? "模拟已开启 - 弹幕/礼物自动生成中" : "模拟已关闭");
        }

        // ==================== 事件回调 ====================

        private void HandleConnected()
        {
            _connected = true;
            SetButtonText(ConnectBtn, "已连接 OK");
            var net = NetworkManager.Instance;
            string mode = (net != null && net.IsGMMode) ? "GM模式" : "直播模式";
            SetStatusText($"已连接 ({mode})");
            UpdateButtonStates(true, SurvivalGameManager.Instance?.State ?? SurvivalGameManager.SurvivalState.Idle);
        }

        private void HandleDisconnected(string reason)
        {
            _connected  = false;
            _simEnabled = false;
            SetButtonText(ConnectBtn, "GM连接");
            if (simulateButton) SetButtonText(simulateButton, "模拟");
            UpdateButtonStates(false, SurvivalGameManager.SurvivalState.Idle);
            SetStatusText($"已断开: {reason}");
        }

        private void HandleStateChanged(SurvivalGameManager.SurvivalState state)
        {
            UpdateButtonStates(_connected, state);
            SetStatusText($"游戏状态: {state}");
        }

        // ==================== 工具 ====================

        private void UpdateButtonStates(bool connected, SurvivalGameManager.SurvivalState state)
        {
            bool isIdle    = state == SurvivalGameManager.SurvivalState.Idle;
            bool isWaiting = state == SurvivalGameManager.SurvivalState.Waiting;

            var cb = ConnectBtn;
            if (cb != null) cb.interactable = !connected;

            // 开始挑战按钮：Idle 或 Waiting 状态下可用
            if (startButton)    startButton.interactable    = connected && (isIdle || isWaiting);
            if (resetButton)    resetButton.interactable    = connected;
            if (simulateButton) simulateButton.interactable = connected;

            // 更新"开始游戏"按钮文本：Waiting 时改为"开始挑战"，Idle 时显示"开始游戏"
            if (startButton)
            {
                string btnLabel = isWaiting ? "开始挑战" : "开始游戏";
                SetButtonText(startButton, btnLabel);
            }
        }

        private void SetButtonText(Button btn, string text)
        {
            if (btn == null) return;
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = text;
        }

        private void SetStatusText(string text)
        {
            if (statusText != null) statusText.text = text;
            Debug.Log($"[GM] {text}");
        }
    }
}
