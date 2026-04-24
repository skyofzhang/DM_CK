using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DrscfZ.UI
{
    public class GameSpeedController : MonoBehaviour
    {
        [SerializeField] private Button _speedButton;
        [SerializeField] private TMP_Text _speedLabel;

        private int _speedIndex = 0;
        private static readonly int[] Speeds = { 1, 2, 3 };
        private static readonly string[] Labels = { "×1", "×2", "×3" };

        private void Start()
        {
            _speedButton?.onClick.AddListener(OnSpeedButtonClicked);
            UpdateDisplay();
        }

        private void OnSpeedButtonClicked()
        {
            _speedIndex = (_speedIndex + 1) % Speeds.Length;
            Time.timeScale = Speeds[_speedIndex];
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (_speedLabel != null)
            {
                _speedLabel.text = Labels[_speedIndex];
                // 绑定字体（防中文乱码）
                var font = Resources.Load<TMPro.TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF") ?? Resources.Load<TMPro.TMP_FontAsset>("Fonts/ChineseFont SDF");
                if (font != null) _speedLabel.font = font;
            }
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        // 在编辑器退出 PlayMode 时重置
        #if UNITY_EDITOR
        private void OnApplicationQuit()
        {
            Time.timeScale = 1f;
        }
        #endif
    }
}
