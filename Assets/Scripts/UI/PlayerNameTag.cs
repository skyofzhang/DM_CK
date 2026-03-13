using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DrscfZ.UI
{
    /// <summary>
    /// 玩家头顶名字+头像标签（WorldSpace Canvas Billboard）
    /// 挂在 Worker GameObject 上；初始 inactive，由 WorkerManager 激活
    /// </summary>
    public class PlayerNameTag : MonoBehaviour
    {
        [Header("UI 引用（Inspector 拖入）")]
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private RawImage _avatarImage;
        [SerializeField] private Canvas _canvas;

        [Header("配置")]
        [SerializeField] private Vector3 _offset = new Vector3(0f, 2.5f, 0f); // 头顶偏移

        private Camera _cam;

        private void Awake()
        {
            _cam = Camera.main;
            // 确保字体绑定（R2规则）
            if (_nameText != null)
            {
                var font = Resources.Load<TMPro.TMP_FontAsset>("Fonts/ChineseFont SDF");
                if (font != null) _nameText.font = font;
                _nameText.fontSize = 36f;
                _nameText.fontStyle = FontStyles.Bold;
                _nameText.color = Color.white;

                // 绑定描边材质（任务3）
                var outlineMat = Resources.Load<Material>("Fonts/ChineseFont SDF - Outline");
                if (outlineMat != null)
                    _nameText.fontSharedMaterial = outlineMat;
                else
                {
                    // Fallback：直接在 fontMaterial 上设置描边
                    _nameText.fontMaterial.SetFloat(TMPro.ShaderUtilities.ID_OutlineWidth, 0.15f);
                    _nameText.fontMaterial.SetColor(TMPro.ShaderUtilities.ID_OutlineColor, Color.black);
                }
            }
        }

        private void LateUpdate()
        {
            // Billboard：始终面朝相机（与 WorldSpaceLabel 一致的 LookRotation 模式）
            if (_cam == null) _cam = Camera.main;
            if (_cam != null)
            {
                transform.rotation = Quaternion.LookRotation(
                    transform.position - _cam.transform.position
                );
            }
        }

        /// <summary>初始化名字标签（由 WorkerManager 调用）</summary>
        public void Initialize(string playerName, string avatarUrl = null)
        {
            if (_nameText != null)
                _nameText.text = playerName;

            // 头像加载或空URL占位（任务2）
            if (_avatarImage != null)
            {
                if (!string.IsNullOrEmpty(avatarUrl))
                {
                    StartCoroutine(LoadAvatar(avatarUrl));
                }
                else
                {
                    // avatarUrl 为空→显示蓝色占位，确保 alpha=1
                    _avatarImage.texture = null;
                    _avatarImage.color = new Color(0.3f, 0.6f, 1f, 1f);
                }
            }

            gameObject.SetActive(true);
        }

        private System.Collections.IEnumerator LoadAvatar(string url)
        {
            using (var req = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
            {
                yield return req.SendWebRequest();

                // 任务1：错误处理（网络失败→显示蓝色占位）
                if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    if (_avatarImage != null)
                    {
                        _avatarImage.texture = null;
                        _avatarImage.color = new Color(0.3f, 0.6f, 1f, 1f);  // 蓝色占位
                    }
                    yield break;
                }

                if (_avatarImage != null)
                {
                    var tex = UnityEngine.Networking.DownloadHandlerTexture.GetContent(req);
                    _avatarImage.texture = tex;
                    _avatarImage.color = Color.white;  // 任务1：确保 alpha=1，头像可见
                }
            }
        }
    }
}
