using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrscfZ.Core;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// 生存游戏 Loading 界面控制器。
    /// 挂载在 Canvas（始终激活，符合 Rule #7）。
    /// SurvivalState.Loading 时显示：
    ///   - IsEnteringScene = true  → "准备进入战场..."
    ///   - IsEnteringScene = false → "正在退出，返回大厅..."
    /// </summary>
    public class SurvivalLoadingUI : MonoBehaviour
    {
        [Header("面板根节点")]
        [SerializeField] private GameObject _panel;

        [Header("文字 & 动画")]
        [SerializeField] private TMP_Text _loadingText;
        [SerializeField] private Image    _spinner;      // 旋转动画图标（可选）

        // Spinner 旋转速度（度/秒）
        private const float SPINNER_SPEED = 270f;

        // ==================== 生命周期 ====================

        private void Start()
        {
            // 订阅游戏状态事件
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
                sgm.OnStateChanged += OnStateChanged;

            // 订阅断线事件（断线时强制隐藏 Loading）
            var net = NetworkManager.Instance;
            if (net != null)
                net.OnDisconnected += OnDisconnected;

            // 初始化
            RefreshVisibility();
        }

        private void OnDestroy()
        {
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
                sgm.OnStateChanged -= OnStateChanged;

            var net = NetworkManager.Instance;
            if (net != null)
                net.OnDisconnected -= OnDisconnected;
        }

        private void Update()
        {
            // Spinner 旋转
            if (_spinner != null && _panel != null && _panel.activeSelf)
            {
                _spinner.transform.Rotate(0f, 0f, -SPINNER_SPEED * Time.deltaTime);
            }
        }

        // ==================== 事件回调 ====================

        private void OnStateChanged(SurvivalGameManager.SurvivalState state)
        {
            RefreshVisibility();
        }

        private void OnDisconnected(string _)
        {
            HidePanel();
        }

        // ==================== 显隐逻辑 ====================

        private void RefreshVisibility()
        {
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null && sgm.State == SurvivalGameManager.SurvivalState.Loading)
            {
                ShowPanel(sgm.IsEnteringScene);
            }
            else
            {
                HidePanel();
            }
        }

        private void ShowPanel(bool isEntering)
        {
            if (_panel != null) _panel.SetActive(true);

            if (_loadingText != null)
                _loadingText.text = isEntering ? "准备进入战场..." : "正在退出，返回大厅...";

            Debug.Log($"[LoadingUI] 显示 Loading：{(isEntering ? "进入" : "退出")}");
        }

        private void HidePanel()
        {
            if (_panel != null) _panel.SetActive(false);
        }
    }
}
