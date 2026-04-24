using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace DrscfZ.UI
{
    /// <summary>
    /// Worker头顶气泡UI（World Space Canvas）
    /// 挂在每个Worker的BubbleCanvas子对象上
    /// Billboard效果：LateUpdate中始终朝向主相机
    /// </summary>
    public class WorkerBubble : MonoBehaviour
    {
        [SerializeField] private TMP_Text _iconText;  // 显示emoji图标
        [SerializeField] private Image    _bgImage;   // 气泡背景颜色

        // 工位气泡背景颜色（索引对应 commandId）
        private static readonly Color[] BUBBLE_COLORS = {
            Color.white,                                         // [0] unused
            new Color(0.267f, 0.667f, 1.0f,   0.9f),           // [1] 采食物 #44AAFF
            new Color(0.4f,   0.4f,   0.4f,   0.9f),           // [2] 挖煤   #666666
            new Color(0.533f, 0.8f,   1.0f,   0.9f),           // [3] 挖矿   #88CCFF
            new Color(1.0f,   0.408f, 0.125f, 0.9f),           // [4] 生火   #FF6820
            Color.white,                                         // [5] unused
            new Color(1.0f,   0.133f, 0.0f,   0.9f),           // [6] 打怪   #FF2200
        };

        // 工位气泡图标（索引对应 commandId，使用中文单字避免emoji缺字体警告）
        private static readonly string[] BUBBLE_ICONS = {
            "",    // [0] unused
            "食",  // [1] 采食物
            "煤",  // [2] 挖煤
            "矿",  // [3] 挖矿
            "火",  // [4] 生火
            "",    // [5] unused
            "战",  // [6] 打怪
        };

        private Camera _mainCamera;

        private void Start()
        {
            _mainCamera = Camera.main;
            // 绑定中文字体，防止气泡文字乱码
            if (_iconText != null)
            {
                var chineseFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF") ?? Resources.Load<TMPro.TMP_FontAsset>("Fonts/ChineseFont SDF");
                if (chineseFont != null) _iconText.font = chineseFont;
                if (_iconText.fontSize < 32f) _iconText.fontSize = 32f;
            }
            Hide();
        }

        private void LateUpdate()
        {
            // Billboard: 始终朝向相机，防止气泡字被背面渲染
            if (_mainCamera != null)
            {
                transform.LookAt(
                    transform.position + _mainCamera.transform.rotation * Vector3.forward,
                    _mainCamera.transform.rotation * Vector3.up);
            }
        }

        /// <summary>显示工作气泡（根据指令类型设置图标和颜色）</summary>
        public void ShowWork(int cmdType)
        {
            if (cmdType < 0 || cmdType >= BUBBLE_COLORS.Length) return;

            if (_iconText != null)
                _iconText.text = BUBBLE_ICONS[cmdType];
            if (_bgImage != null)
                _bgImage.color = BUBBLE_COLORS[cmdType];

            gameObject.SetActive(true);
        }

        /// <summary>显示特殊状态气泡（自定义图标和背景色）</summary>
        public void ShowSpecial(string icon, Color bgColor)
        {
            if (_iconText != null)
                _iconText.text = icon;
            if (_bgImage != null)
                _bgImage.color = bgColor;

            gameObject.SetActive(true);
        }

        /// <summary>隐藏气泡</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
