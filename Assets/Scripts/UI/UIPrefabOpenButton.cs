using UnityEngine;
using UnityEngine.UI;

namespace DrscfZ.UI
{
    /// <summary>
    /// Button helper for opening UI panel prefabs through UIPrefabLoader.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIPrefabOpenButton : MonoBehaviour
    {
        [SerializeField] private string _panelId;
        [SerializeField] private bool _toggle;
        [SerializeField] private bool _close;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (_button == null)
                _button = GetComponent<Button>();

            _button.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClick);
        }

        private void HandleClick()
        {
            if (string.IsNullOrEmpty(_panelId))
                return;

            if (_close)
                UIPrefabLoader.ClosePanel(_panelId);
            else if (_toggle)
                UIPrefabLoader.TogglePanel(_panelId);
            else
                UIPrefabLoader.OpenPanel(_panelId);
        }
    }
}
