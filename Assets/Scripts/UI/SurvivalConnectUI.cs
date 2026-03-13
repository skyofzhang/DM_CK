using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DrscfZ.Core;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// 生存游戏连接界面控制器。
    /// 挂载在 Canvas（始终激活，符合 Rule #7）。
    /// 管理 ConnectPanel 的显隐与连接流程，直接调用 SurvivalGameManager.ConnectToServer()。
    ///
    /// 连接成功后，先显示"已连接！"提示 1.5 秒再隐藏面板，
    /// 确保玩家能看到第①阶段（连接界面），再过渡到后续状态。
    /// </summary>
    public class SurvivalConnectUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text   _statusText;
        [SerializeField] private TMP_Text   _dotText;
        [SerializeField] private Image      _spinner;
        [SerializeField] private Button     _retryBtn;

        /// <summary>连接成功后延迟隐藏秒数（让玩家看到"已连接"提示）</summary>
        [SerializeField] private float _hideDelayAfterConnect = 1.5f;

        private Coroutine _dotCoroutine;
        private Coroutine _hideDelayCoroutine;
        private bool      _connected;

        // ==================== 生命周期 ====================

        private void Start()
        {
            // 隐藏重试按钮并监听点击
            if (_retryBtn != null)
            {
                _retryBtn.gameObject.SetActive(false);
                _retryBtn.onClick.AddListener(OnRetryClicked);
            }

            // 订阅网络事件
            var net = NetworkManager.Instance;
            if (net != null)
            {
                net.OnConnected      += OnConnected;
                net.OnDisconnected   += OnDisconnected;
                net.OnConnectFailed  += OnConnectFailed;
            }
            else
            {
                Debug.LogError("[SurvivalConnectUI] NetworkManager.Instance 未找到！");
            }

            // 显示连接界面并发起连接
            Debug.Log("[SurvivalConnectUI] 启动 → 显示连接界面，开始连接...");
            ShowPanel("正在连接服务器");
            DoConnect();
        }

        private void OnDestroy()
        {
            CancelHideDelay();
            if (_dotCoroutine != null) StopCoroutine(_dotCoroutine);

            var net = NetworkManager.Instance;
            if (net != null)
            {
                net.OnConnected     -= OnConnected;
                net.OnDisconnected  -= OnDisconnected;
                net.OnConnectFailed -= OnConnectFailed;
            }
        }

        private void Update()
        {
            // Spinner 旋转动画（连接中时持续转）
            if (_spinner != null && !_connected)
                _spinner.rectTransform.Rotate(0f, 0f, -200f * Time.deltaTime);
        }

        // ==================== 网络回调 ====================

        private void OnConnected()
        {
            _connected = true;
            Debug.Log("[SurvivalConnectUI] ✅ 已连接！→ 1.5s 后隐藏连接面板");
            // 显示"已连接"提示，延迟后隐藏（让玩家看到第①阶段）
            ShowPanel("已连接！正在加载游戏状态...");
            if (_retryBtn != null) _retryBtn.gameObject.SetActive(false);
            CancelHideDelay();
            _hideDelayCoroutine = StartCoroutine(HidePanelAfterDelay(_hideDelayAfterConnect));
        }

        private void OnDisconnected(string reason)
        {
            _connected = false;
            CancelHideDelay();
            string msg = string.IsNullOrEmpty(reason) ? "连接断开" : $"连接断开：{reason}";
            Debug.Log($"[SurvivalConnectUI] ❌ {msg}");
            ShowPanel(msg);
            if (_retryBtn != null) _retryBtn.gameObject.SetActive(true);
        }

        private void OnConnectFailed(string error)
        {
            _connected = false;
            CancelHideDelay();
            string msg = string.IsNullOrEmpty(error) ? "连接失败，请重试" : $"连接失败：{error}";
            Debug.Log($"[SurvivalConnectUI] ❌ {msg}");
            ShowPanel(msg);
            if (_retryBtn != null) _retryBtn.gameObject.SetActive(true);
        }

        // ==================== UI 操作 ====================

        private void OnRetryClicked()
        {
            if (_retryBtn != null) _retryBtn.gameObject.SetActive(false);
            ShowPanel("正在重新连接");
            DoConnect();
        }

        private void ShowPanel(string status)
        {
            if (_panel != null)
                _panel.SetActive(true);
            else
                Debug.LogWarning("[SurvivalConnectUI] _panel 未绑定，请运行 Tools/Phase2/Setup Connect & Start UI");

            if (_statusText != null) _statusText.text = status;

            if (_dotCoroutine != null) StopCoroutine(_dotCoroutine);
            _dotCoroutine = StartCoroutine(DotAnimation());
        }

        private void HidePanel()
        {
            Debug.Log("[SurvivalConnectUI] 隐藏连接面板 ✅");
            if (_dotCoroutine != null) { StopCoroutine(_dotCoroutine); _dotCoroutine = null; }
            if (_dotText != null) _dotText.text = "";
            if (_panel != null)
                _panel.SetActive(false);
        }

        private void CancelHideDelay()
        {
            if (_hideDelayCoroutine != null)
            {
                StopCoroutine(_hideDelayCoroutine);
                _hideDelayCoroutine = null;
            }
        }

        private IEnumerator HidePanelAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            _hideDelayCoroutine = null;
            HidePanel();
        }

        private IEnumerator DotAnimation()
        {
            string[] dots = { ".", "..", "...", "" };
            int i = 0;
            while (true)
            {
                if (_dotText != null) _dotText.text = dots[i % dots.Length];
                i++;
                yield return new WaitForSeconds(0.4f);
            }
        }

        // ==================== 连接逻辑 ====================

        private void DoConnect()
        {
            _connected = false;
            // 优先通过 SurvivalGameManager（同时重置状态机到 Idle）
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.ConnectToServer();
            }
            else
            {
                // Fallback：直接调用 NetworkManager
                var net = NetworkManager.Instance;
                if (net != null)
                    net.Connect();
                else
                    Debug.LogError("[SurvivalConnectUI] NetworkManager & SurvivalGameManager 均未找到！");
            }
        }
    }
}
