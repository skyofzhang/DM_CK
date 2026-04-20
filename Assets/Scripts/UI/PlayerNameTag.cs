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

        // §30 矿工成长系统：缓存玩家原始名字（不含 Lv 前缀）+ 当前等级/阶段
        private string _cachedPlayerName = "";
        private int    _currentLevel     = 1;
        private int    _currentTier      = 1;

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
            if (_cam == null) _cam = Camera.main;
            // 位置：始终锁定在父对象（矿工根节点）固定偏移处，避免动画根节点旋转带来的位移
            if (transform.parent != null)
                transform.position = transform.parent.position + _offset;
            // Billboard：始终面朝相机
            if (_cam != null)
                transform.rotation = Quaternion.LookRotation(
                    transform.position - _cam.transform.position
                );
        }

        /// <summary>初始化名字标签（由 WorkerManager 调用）</summary>
        public void Initialize(string playerName, string avatarUrl = null)
        {
            _cachedPlayerName = playerName ?? "";
            if (_nameText != null)
                _nameText.text = playerName;

            // 头像加载：有 URL 则异步加载，无 URL 则隐藏头像区域（避免显示实心色块）
            if (_avatarImage != null)
            {
                if (!string.IsNullOrEmpty(avatarUrl))
                {
                    _avatarImage.gameObject.SetActive(true);
                    StartCoroutine(LoadAvatar(avatarUrl));
                }
                else
                {
                    // 无头像：隐藏头像 RawImage，只显示名字文本
                    _avatarImage.gameObject.SetActive(false);
                }
            }

            gameObject.SetActive(true);
        }

        private System.Collections.IEnumerator LoadAvatar(string url)
        {
            using (var req = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
            {
                yield return req.SendWebRequest();

                // 网络失败 → 隐藏头像，只显示名字文本（避免显示实心色块）
                if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    if (_avatarImage != null) _avatarImage.gameObject.SetActive(false);
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

        // ==================== §30 矿工成长系统 ====================

        /// <summary>
        /// 设置矿工等级+阶段，刷新显示为 "[Lv.XX] 玩家名" 并按阶段染色。
        /// §30.8 颜色表：1=灰 / 2=铜 / 3=银 / 4=金 / 5=橙 / 6=蓝 / 7=紫 / 8=深红 / 9=深紫 / 10=金红
        /// TMP 颜色：PlayerNameTag 为运行时常驻脚本，Awake 已初始化 fontMaterial，
        /// .color + faceColor 双写即可生效（非 Editor 脚本场景不需要 SerializedObject）
        /// </summary>
        public void SetLevel(int level, int tier)
        {
            _currentLevel = Mathf.Clamp(level, 1, 100);
            _currentTier  = Mathf.Clamp(tier, 1, 10);
            RefreshTextAndColor();
        }

        /// <summary>仅刷新阶段颜色（手动换肤 / 皮肤切换调用）</summary>
        public void SetTier(int tier)
        {
            _currentTier = Mathf.Clamp(tier, 1, 10);
            RefreshTextAndColor();
        }

        private void RefreshTextAndColor()
        {
            if (_nameText == null) return;

            // 文本：当等级 > 1 显示 [Lv.XX] 前缀；等级 1 保持纯名字
            string displayName = string.IsNullOrEmpty(_cachedPlayerName)
                ? (_nameText.text ?? "")
                : _cachedPlayerName;
            _nameText.text = _currentLevel > 1
                ? $"[Lv.{_currentLevel}] {displayName}"
                : displayName;

            // 颜色：按阶段选取（§30.8）
            Color tierColor = GetTierColor(_currentTier);

            // 颜色写入：运行时用 .color + faceColor（CLAUDE.md 踩坑提示 SerializedObject 是 Editor-only，
            // 仅适用于 Editor 脚本批量构建 UI，运行时新建 TMP 时 faceColor 默认白色会覆盖 .color，
            // 但此处 PlayerNameTag 已由 Prefab 预创建并在 Awake 初始化 fontMaterial，
            // faceColor 已可安全调用。）
            _nameText.color = tierColor;
            try { _nameText.faceColor = tierColor; } catch { /* material 未就绪时忽略 */ }
        }

        /// <summary>§30.8 阶段颜色表</summary>
        private static Color GetTierColor(int tier)
        {
            switch (Mathf.Clamp(tier, 1, 10))
            {
                case 1:  return new Color(0.70f, 0.70f, 0.70f); // 灰
                case 2:  return new Color(0.72f, 0.45f, 0.20f); // 铜
                case 3:  return new Color(0.75f, 0.75f, 0.78f); // 银
                case 4:  return new Color(1.00f, 0.84f, 0.00f); // 金
                case 5:  return new Color(1.00f, 0.55f, 0.10f); // 橙
                case 6:  return new Color(0.25f, 0.55f, 1.00f); // 蓝
                case 7:  return new Color(0.68f, 0.35f, 1.00f); // 紫
                case 8:  return new Color(0.65f, 0.10f, 0.10f); // 深红
                case 9:  return new Color(0.30f, 0.05f, 0.45f); // 深紫
                case 10: return new Color(1.00f, 0.30f, 0.10f); // 金红
                default: return Color.white;
            }
        }
    }
}
