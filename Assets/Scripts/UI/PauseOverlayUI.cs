using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// 🔴 audit-r35 GAP-A25-04 game_paused 完整版（90% → 100%）：持久 overlay UI
    ///
    /// 订阅 SurvivalGameManager.OnGamePaused / OnGameResumed 事件；
    /// pause 时显示全屏半透明黑色遮罩 + "游戏已暂停"金色大字（持续可见）；
    /// resume 时立即隐藏。
    ///
    /// 挂载规则（CLAUDE.md 规则 6 + 7）：
    ///   挂 Canvas/PauseOverlayPanel（常驻激活），Awake **动态创建**全屏半透明子节点（避免依赖场景预创建）。
    ///   _overlayRoot 子节点初始 inactive；脚本本身 GO 始终 active 保证 OnEnable 订阅生效。
    /// </summary>
    public class PauseOverlayUI : MonoBehaviour
    {
        public static PauseOverlayUI Instance { get; private set; }

        // ─── 内部状态 ────────────────────────────────────────────────────
        private GameObject     _overlayRoot;
        private Image          _bgImage;
        private TextMeshProUGUI _mainText;
        private TextMeshProUGUI _subText;
        private bool _subscribed = false;

        // 半透明黑色遮罩颜色
        private static readonly Color BG_COLOR    = new Color(0f, 0f, 0f, 0.7f);
        private static readonly Color TEXT_COLOR  = new Color(1f, 0.84f, 0.1f);  // 金色 #FFD700
        private static readonly Color SUB_COLOR   = new Color(0.9f, 0.9f, 0.9f); // 浅灰

        // ── 生命周期 ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance == null) Instance = this;
            BuildOverlayHierarchy();
        }

        private void Start()  { TrySubscribe(); }
        private void OnEnable()  { TrySubscribe(); }
        private void OnDisable() { Unsubscribe(); }
        private void OnDestroy()
        {
            Unsubscribe();
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!_subscribed) TrySubscribe();
        }

        // ── 动态构建 overlay 子节点 ────────────────────────────────────────

        private void BuildOverlayHierarchy()
        {
            // 🔴 audit-r35 hotfix：找 Canvas 作挂载父级（兼容父级是 Transform / RectTransform 两种情况）
            //   PauseOverlayPanel 挂载点本身可能是普通 Transform（MCP 创建时未升级 RectTransform），
            //   子节点必须挂在 Canvas 直接下才能正常 UI 渲染
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            Transform mountParent = parentCanvas != null ? parentCanvas.transform : transform;

            // 创建 overlay 根节点 — 全屏覆盖
            _overlayRoot = new GameObject("PauseOverlayRoot");
            _overlayRoot.transform.SetParent(mountParent, false);

            var rt = _overlayRoot.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // 半透明背景 Image
            _bgImage = _overlayRoot.AddComponent<Image>();
            _bgImage.color = BG_COLOR;
            _bgImage.raycastTarget = true;  // 阻挡点击穿透

            // 主标题（"游戏已暂停"）
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(_overlayRoot.transform, false);
            var titleRT = titleGO.AddComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0f, 0.5f);
            titleRT.anchorMax = new Vector2(1f, 0.5f);
            titleRT.pivot     = new Vector2(0.5f, 0.5f);
            titleRT.anchoredPosition = new Vector2(0f, 50f);
            titleRT.sizeDelta = new Vector2(0f, 120f);

            _mainText = titleGO.AddComponent<TextMeshProUGUI>();
            _mainText.text      = "⏸ 游戏已暂停";
            _mainText.fontSize  = 80;
            _mainText.color     = TEXT_COLOR;
            _mainText.alignment = TextAlignmentOptions.Center;
            _mainText.fontStyle = FontStyles.Bold;
            BindFont(_mainText);

            // 副标题（"GM 调试模式 — 等待主播恢复"）
            var subGO = new GameObject("Subtitle");
            subGO.transform.SetParent(_overlayRoot.transform, false);
            var subRT = subGO.AddComponent<RectTransform>();
            subRT.anchorMin = new Vector2(0f, 0.5f);
            subRT.anchorMax = new Vector2(1f, 0.5f);
            subRT.pivot     = new Vector2(0.5f, 0.5f);
            subRT.anchoredPosition = new Vector2(0f, -40f);
            subRT.sizeDelta = new Vector2(0f, 60f);

            _subText = subGO.AddComponent<TextMeshProUGUI>();
            _subText.text      = "GM 调试模式 — 等待主播恢复";
            _subText.fontSize  = 32;
            _subText.color     = SUB_COLOR;
            _subText.alignment = TextAlignmentOptions.Center;
            BindFont(_subText);

            // 初始隐藏（CLAUDE.md 规则 6：仅对 _overlayRoot 子节点 SetActive(false)）
            _overlayRoot.SetActive(false);
        }

        private static void BindFont(TextMeshProUGUI tmp)
        {
            if (tmp == null) return;
            var font = Resources.Load<TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF")
                    ?? Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
            if (font != null) tmp.font = font;
        }

        // ── 订阅 ──────────────────────────────────────────────────────────

        private void TrySubscribe()
        {
            if (_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;
            sgm.OnGamePaused  += HandleGamePaused;
            sgm.OnGameResumed += HandleGameResumed;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnGamePaused  -= HandleGamePaused;
                sgm.OnGameResumed -= HandleGameResumed;
            }
            _subscribed = false;
        }

        // ── 事件处理 ─────────────────────────────────────────────────────

        private void HandleGamePaused()
        {
            if (_overlayRoot != null) _overlayRoot.SetActive(true);
        }

        private void HandleGameResumed()
        {
            if (_overlayRoot != null) _overlayRoot.SetActive(false);
        }
    }
}
