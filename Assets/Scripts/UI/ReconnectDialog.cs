using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DrscfZ.UI
{
    /// <summary>
    /// 断线重连对话框（Scene 预创建，初始 inactive）
    /// 触发条件：连接后服务器返回 has_active_session=true（join_room_confirm 中）
    ///
    /// AI准则 #2：此对象在 Scene 中预创建，初始 SetActive(false)；不在运行时 Instantiate。
    /// AI准则 #7：脚本本身挂在始终活跃的父对象（如 Canvas）上，而非挂在此 Dialog 自身。
    ///           若确实需要挂在 Dialog 自身，请确保 Dialog 的父对象始终 active。
    /// </summary>
    public class ReconnectDialog : MonoBehaviour
    {
        public static ReconnectDialog Instance { get; private set; }

        [SerializeField] private Button     _reconnectButton;   // 继续上一局
        [SerializeField] private Button     _newGameButton;     // 开始新局
        [SerializeField] private TMP_Text   _titleText;
        [SerializeField] private TMP_Text   _descText;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            // 绑定中文字体
            var font = Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
            if (font != null)
            {
                if (_titleText != null) _titleText.font = font;
                if (_descText  != null) _descText.font  = font;
            }

            _reconnectButton?.onClick.AddListener(OnReconnect);
            _newGameButton?.onClick.AddListener(OnNewGame);

            // 初始隐藏（AI准则 #2：Scene 预创建，初始 inactive）
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>显示断线重连对话框（由 SurvivalGameManager 在检测到 has_active_session=true 时调用）</summary>
        public void Show()
        {
            gameObject.SetActive(true);
            if (_titleText != null) _titleText.text = "检测到上一局进行中";
            if (_descText  != null) _descText.text  = "发现未完成的游戏，是否继续上一局？";
        }

        private void OnReconnect()
        {
            // 通过 SurvivalGameManager 请求恢复：设 IsEnteringScene=true 并发 sync_state
            // 服务器收到后重新推送 survival_game_state{state:'day'/'night'}，客户端自动进入 Running
            DrscfZ.Survival.SurvivalGameManager.Instance?.RequestResumeSession();
            Debug.Log("[ReconnectDialog] 继续上一局 → RequestResumeSession()");
            gameObject.SetActive(false);
        }

        private void OnNewGame()
        {
            // 重置服务器（清空上一局），服务器响应后推送 survival_game_state{state:'idle'}
            // 客户端收到后回到 Idle 状态，LobbyPanel 显示"▶ 开始游戏"
            DrscfZ.Core.NetworkManager.Instance?.SendMessage("reset_game");
            Debug.Log("[ReconnectDialog] 放弃上一局 → 已发送 reset_game");
            gameObject.SetActive(false);
        }
    }
}
