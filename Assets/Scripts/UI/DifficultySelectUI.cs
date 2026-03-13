using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrscfZ.Core;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// 难度选择界面（全屏遮罩）
    ///
    /// 显示条件：已连接 + SurvivalState.Waiting + DifficultyLevel == None
    /// 功能：
    ///   - 全屏显示三种难度：轻松/困难/恐怖
    ///   - 点击后调用 SurvivalGameManager.SetDifficulty()
    ///   - 难度选定后本面板隐藏，由 PreGameBannerUI 接管
    ///
    /// AI准则 #7：挂在 Canvas（始终激活对象）上
    /// AI准则 #2：面板在 Scene 中预创建，初始 inactive，通过 SetActive 控制显隐
    /// </summary>
    public class DifficultySelectUI : MonoBehaviour
    {
        public static DifficultySelectUI Instance { get; private set; }

        // 防止自动弹出：只有用户主动点击"开始玩法"后才允许显示
        private bool _triggeredByUser = false;

        [Header("面板根节点（SetActive 控制显隐）")]
        [SerializeField] private GameObject _panel;

        [Header("难度按钮")]
        [SerializeField] private Button _easyBtn;    // 轻松模式
        [SerializeField] private Button _normalBtn;  // 困难模式
        [SerializeField] private Button _hardBtn;    // 恐怖模式

        [Header("说明文字（可选）")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _descText;

        // ==================== 生命周期 ====================

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            // 绑定字体
            var font = Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
            if (font != null)
            {
                if (_titleText != null) _titleText.font = font;
                if (_descText  != null) _descText.font  = font;

                ApplyFontToButton(_easyBtn,   font);
                ApplyFontToButton(_normalBtn, font);
                ApplyFontToButton(_hardBtn,   font);
            }

            // 按钮绑定
            _easyBtn  ?.onClick.AddListener(OnEasyClicked);
            _normalBtn?.onClick.AddListener(OnNormalClicked);
            _hardBtn  ?.onClick.AddListener(OnHardClicked);

            // 标题文字
            if (_titleText != null) _titleText.text = "选择游戏难度";
            if (_descText  != null) _descText.text  = "根据你的直播间观众规模选择，不同难度影响怪物强度和资源消耗";

            // 订阅网络 / 状态事件
            var net = NetworkManager.Instance;
            if (net != null)
            {
                net.OnConnected    += RefreshVisibility;
                net.OnDisconnected += _ => HidePanel();
            }

            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnStateChanged  += _ => RefreshVisibility();
                sgm.OnDifficultySet += _ => RefreshVisibility();
            }

            RefreshVisibility();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            var net = NetworkManager.Instance;
            if (net != null)
            {
                net.OnConnected    -= RefreshVisibility;
                net.OnDisconnected -= _ => HidePanel();
            }

            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnStateChanged  -= _ => RefreshVisibility();
                sgm.OnDifficultySet -= _ => RefreshVisibility();
            }
        }

        // ==================== 显隐逻辑 ====================

        /// <summary>仅在用户主动点击"开始玩法"时调用，防止自动弹出</summary>
        public void ShowByUserAction()
        {
            _triggeredByUser = true;
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            var net = NetworkManager.Instance;
            var sgm = SurvivalGameManager.Instance;

            bool isConnected      = net != null && net.IsConnected;
            var  state            = sgm?.State ?? SurvivalGameManager.SurvivalState.Idle;
            bool noDifficulty     = sgm == null ||
                                    sgm.SelectedDifficulty == SurvivalGameManager.DifficultyLevel.None;

            // 只在 Waiting + 尚未选难度 + 用户主动触发 时显示
            bool showPanel = isConnected &&
                             state == SurvivalGameManager.SurvivalState.Waiting &&
                             noDifficulty &&
                             _triggeredByUser;  // 必须由用户主动触发，防止自动弹出

            if (showPanel)
                ShowPanel();
            else
                HidePanel();
        }

        private void ShowPanel()
        {
            if (_panel != null) _panel.SetActive(true);
            // 重置按钮可用状态
            SetButtonsInteractable(true);
        }

        private void HidePanel()
        {
            if (_panel != null) _panel.SetActive(false);
            _triggeredByUser = false;  // 重置标志，防止下次进入 Waiting 时自动弹出
        }

        // ==================== 按钮回调 ====================

        private void OnEasyClicked()   => SelectDifficulty(SurvivalGameManager.DifficultyLevel.Easy);
        private void OnNormalClicked() => SelectDifficulty(SurvivalGameManager.DifficultyLevel.Normal);
        private void OnHardClicked()   => SelectDifficulty(SurvivalGameManager.DifficultyLevel.Hard);

        private void SelectDifficulty(SurvivalGameManager.DifficultyLevel level)
        {
            SetButtonsInteractable(false);
            SurvivalGameManager.Instance?.SetDifficulty(level);
            // RefreshVisibility 会通过 OnDifficultySet 事件自动被调用，面板会隐藏
            Debug.Log($"[DifficultySelectUI] 选择难度: {level}");
        }

        // ==================== 工具 ====================

        private void ApplyFontToButton(Button btn, TMP_FontAsset font)
        {
            if (btn == null || font == null) return;
            foreach (var tmp in btn.GetComponentsInChildren<TMP_Text>())
                tmp.font = font;
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (_easyBtn   != null) _easyBtn.interactable   = interactable;
            if (_normalBtn != null) _normalBtn.interactable = interactable;
            if (_hardBtn   != null) _hardBtn.interactable   = interactable;
        }
    }
}
