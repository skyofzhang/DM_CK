using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrscfZ.Core;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// Loading overlay shown while entering or leaving the survival battlefield.
    /// Creates a minimal fallback panel when scene bindings are missing.
    /// </summary>
    public class SurvivalLoadingUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject _panel;

        [Header("Text & Animation")]
        [SerializeField] private TMP_Text _loadingText;
        [SerializeField] private Image _spinner;

        private const float SPINNER_SPEED = 270f;
        private SurvivalGameManager _subscribedManager;
        private NetworkManager _subscribedNetwork;

        private void Start()
        {
            EnsureFallbackUI();
            TrySubscribe();
            RefreshVisibility();
        }

        private void Update()
        {
            TrySubscribe();
            if (_spinner != null && _panel != null && _panel.activeSelf)
                _spinner.transform.Rotate(0f, 0f, -SPINNER_SPEED * Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (_subscribedManager != null)
                _subscribedManager.OnStateChanged -= OnStateChanged;
            if (_subscribedNetwork != null)
                _subscribedNetwork.OnDisconnected -= OnDisconnected;
            _subscribedManager = null;
            _subscribedNetwork = null;
        }

        private void TrySubscribe()
        {
            if (_subscribedManager == null && SurvivalGameManager.Instance != null)
            {
                _subscribedManager = SurvivalGameManager.Instance;
                _subscribedManager.OnStateChanged += OnStateChanged;
            }

            if (_subscribedNetwork == null && NetworkManager.Instance != null)
            {
                _subscribedNetwork = NetworkManager.Instance;
                _subscribedNetwork.OnDisconnected += OnDisconnected;
            }
        }

        private void OnStateChanged(SurvivalGameManager.SurvivalState state)
        {
            RefreshVisibility();
        }

        private void OnDisconnected(string _)
        {
            HidePanel();
        }

        private void RefreshVisibility()
        {
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null && sgm.State == SurvivalGameManager.SurvivalState.Loading)
                ShowPanel(sgm.IsEnteringScene);
            else
                HidePanel();
        }

        private void ShowPanel(bool isEntering)
        {
            if (_panel != null) _panel.SetActive(true);
            if (_loadingText != null)
                _loadingText.text = isEntering ? "准备进入战场..." : "正在退出，返回大厅...";
        }

        private void HidePanel()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        private void EnsureFallbackUI()
        {
            if (_panel == null)
            {
                if (transform.parent == null)
                    transform.SetParent(RuntimeUIFactory.GetCanvasTransform(), false);

                _panel = RuntimeUIFactory.CreatePanel(
                    transform,
                    "LoadingPanel",
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero,
                    new Color(0.02f, 0.03f, 0.05f, 0.78f));

                var rt = _panel.GetComponent<RectTransform>();
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            if (_loadingText == null)
            {
                _loadingText = RuntimeUIFactory.CreateText(
                    _panel.transform,
                    "LoadingText",
                    "",
                    34f,
                    Color.white,
                    TextAlignmentOptions.Center,
                    new Vector2(720f, 80f));
                var textRt = _loadingText.GetComponent<RectTransform>();
                textRt.anchorMin = new Vector2(0.5f, 0.5f);
                textRt.anchorMax = new Vector2(0.5f, 0.5f);
                textRt.anchoredPosition = Vector2.zero;
            }

            _panel.SetActive(false);
        }
    }
}
