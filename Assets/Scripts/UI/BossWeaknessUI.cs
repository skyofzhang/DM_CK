using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §34 F4 Boss 露出弱点大横幅（audit-r8 补齐）
    ///
    /// 订阅 SurvivalGameManager.OnBossWeaknessStarted / OnBossWeaknessEnded：
    ///   - started：屏幕居中显示 "Boss 露出弱点！全力攻击！" 红字黑描边 60sp 加粗
    ///             durationMs 保持 + 0.3s 协程淡出
    ///   - ended  ：立即隐藏（服务端已到期广播；提前抢停本地协程）
    ///
    /// 挂载规则（CLAUDE.md #7）：挂 Canvas/GameUIPanel 子 GO（常驻激活），不在 Awake 中 SetActive(false)。
    /// 若场景内未预创建，Start() 里 fallback 自建 Label + BG GameObject。
    ///
    /// 字体：Alibaba 优先 + ChineseFont SDF fallback（CLAUDE.md 规则字体统一）。
    ///
    /// ModalRegistry B 类排队（非阻塞，id="boss_weakness"）：新请求来时 release 旧 id 再入队，
    /// 协程结束自动 ReleaseB，避免列表残留。
    ///
    /// Inspector 可选字段（不填则 fallback 自建）：
    ///   _root       — 横幅容器 GO（初始 inactive）
    ///   _titleLabel — 主标题 TMP
    ///   _bgImage    — 背景半透明 Image
    /// </summary>
    public class BossWeaknessUI : MonoBehaviour
    {
        [Header("横幅根节点（默认 inactive，Started 时打开）")]
        [SerializeField] private GameObject _root;

        [Header("主标题 TMP（60sp 红字黑描边加粗）")]
        [SerializeField] private TMP_Text _titleLabel;

        [Header("背景 Image（半透明黑色 overlay）")]
        [SerializeField] private Image _bgImage;

        // 字体路径（CLAUDE.md 统一规范）
        private const string AlibabaFontPath = "Fonts/AlibabaPuHuiTi-3-85-Bold SDF";
        private const string ChineseFontPath = "Fonts/ChineseFont SDF";

        // 默认淡出时长
        private const float FADE_OUT_SEC = 0.3f;

        // ModalRegistry B 类 id
        private const string MODAL_B_ID = "boss_weakness";

        // 横幅文案
        private const string BANNER_TEXT = "Boss 露出弱点！全力攻击！";

        // 运行时
        private bool       _subscribed;
        private Coroutine  _run;
        private bool       _modalHeld;

        // ── 生命周期 ──────────────────────────────────────────────────────

        private void Start()
        {
            EnsureFallbackUI();
            EnsureFonts();
            if (_root != null) _root.SetActive(false);
            TrySubscribe();
        }

        private void OnEnable()  { TrySubscribe(); }
        private void OnDisable() { Unsubscribe(); }

        private void OnDestroy()
        {
            Unsubscribe();
            if (_run != null) { StopCoroutine(_run); _run = null; }
            if (_modalHeld)
            {
                ModalRegistry.ReleaseB(MODAL_B_ID);
                _modalHeld = false;
            }
        }

        private void Update()
        {
            if (!_subscribed) TrySubscribe();
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;
            sgm.OnBossWeaknessStarted += HandleStarted;
            sgm.OnBossWeaknessEnded   += HandleEnded;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnBossWeaknessStarted -= HandleStarted;
                sgm.OnBossWeaknessEnded   -= HandleEnded;
            }
            _subscribed = false;
        }

        /// <summary>若 Inspector 未绑定 _root/_titleLabel，在运行时自建 fallback UI 层级。
        /// 保证部署到未预建 GO 的场景也能显示（避免 NPE）。</summary>
        private void EnsureFallbackUI()
        {
            if (_root != null && _titleLabel != null) return;

            // 自建容器：铺满父节点（假定挂在 GameUIPanel/Canvas 下）
            var rootGo = new GameObject("BossWeakness_AutoRoot");
            var rootRt = rootGo.AddComponent<RectTransform>();
            rootRt.SetParent(transform, false);
            rootRt.anchorMin  = Vector2.zero;
            rootRt.anchorMax  = Vector2.one;
            rootRt.offsetMin  = Vector2.zero;
            rootRt.offsetMax  = Vector2.zero;
            _root = rootGo;

            // 背景半透明
            if (_bgImage == null)
            {
                var bgGo = new GameObject("BG");
                var bgRt = bgGo.AddComponent<RectTransform>();
                bgRt.SetParent(rootRt, false);
                bgRt.anchorMin = new Vector2(0f, 0.4f);
                bgRt.anchorMax = new Vector2(1f, 0.6f);
                bgRt.offsetMin = Vector2.zero;
                bgRt.offsetMax = Vector2.zero;
                _bgImage = bgGo.AddComponent<Image>();
                _bgImage.color = new Color(0f, 0f, 0f, 0.65f);
                _bgImage.raycastTarget = false;
            }

            // 主标题
            if (_titleLabel == null)
            {
                var labelGo = new GameObject("Title");
                var labelRt = labelGo.AddComponent<RectTransform>();
                labelRt.SetParent(rootRt, false);
                labelRt.anchorMin = new Vector2(0f, 0.42f);
                labelRt.anchorMax = new Vector2(1f, 0.58f);
                labelRt.offsetMin = Vector2.zero;
                labelRt.offsetMax = Vector2.zero;
                var tmp = labelGo.AddComponent<TextMeshProUGUI>();
                tmp.raycastTarget = false;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 60;
                tmp.fontStyle = FontStyles.Bold;
                tmp.text = BANNER_TEXT;
                _titleLabel = tmp;
            }
        }

        private void EnsureFonts()
        {
            var font = Resources.Load<TMP_FontAsset>(AlibabaFontPath);
            if (font == null) font = Resources.Load<TMP_FontAsset>(ChineseFontPath);
            if (font == null) return;
            if (_titleLabel != null && _titleLabel.font != font) _titleLabel.font = font;
        }

        // ── 事件回调 ──────────────────────────────────────────────────────

        private void HandleStarted(BossWeaknessStartedData data)
        {
            if (data == null) return;
            if (_root == null) return;

            if (_titleLabel != null)
            {
                _titleLabel.text = BANNER_TEXT;

                // 60sp 加粗红字黑描边
                _titleLabel.fontSize  = 60;
                _titleLabel.fontStyle = FontStyles.Bold;

                // 写 m_fontColor + m_fontColor32（项目规范：faceColor 默认白色）
                ApplyRedWithBlackOutline(_titleLabel);
            }

            _root.SetActive(true);
            SetAlpha(1f);

            // 非阻塞 B 类排队（id 去重，先 release 旧的避免堆叠）
            ModalRegistry.ReleaseB(MODAL_B_ID);
            ModalRegistry.RequestB(MODAL_B_ID, null);
            _modalHeld = true;

            // 重启协程
            if (_run != null) StopCoroutine(_run);
            int durMs = data.durationMs > 0 ? data.durationMs : 5000;
            _run = StartCoroutine(HoldAndFade(durMs / 1000f));
        }

        private void HandleEnded(BossWeaknessEndedData data)
        {
            // 服务端 duration 到期或弱点被打断：立即隐藏
            if (_run != null) { StopCoroutine(_run); _run = null; }
            if (_root != null) _root.SetActive(false);
            SetAlpha(1f); // 复位
            if (_modalHeld)
            {
                ModalRegistry.ReleaseB(MODAL_B_ID);
                _modalHeld = false;
            }
        }

        // ── 协程 ──────────────────────────────────────────────────────

        private IEnumerator HoldAndFade(float holdSec)
        {
            SetAlpha(1f);
            if (holdSec > 0f) yield return new WaitForSecondsRealtime(holdSec);

            float t = 0f;
            while (t < FADE_OUT_SEC)
            {
                t += Time.unscaledDeltaTime;
                SetAlpha(1f - Mathf.Clamp01(t / FADE_OUT_SEC));
                yield return null;
            }

            if (_root != null) _root.SetActive(false);
            SetAlpha(1f); // 复位，下一次触发直接用
            if (_modalHeld)
            {
                ModalRegistry.ReleaseB(MODAL_B_ID);
                _modalHeld = false;
            }
            _run = null;
        }

        private void SetAlpha(float a)
        {
            if (_titleLabel != null)
            {
                var c = _titleLabel.color; c.a = a; _titleLabel.color = c;
            }
            if (_bgImage != null)
            {
                var c = _bgImage.color; c.a = a * 0.65f; _bgImage.color = c;
            }
        }

        /// <summary>红色文字 + 黑色描边（CLAUDE.md 踩坑：TMP 运行时新建/faceColor 默认白色）。
        /// 运行时路径：写 color（Graphic 层）+ faceColor（material 层，确保真实渲染为红色）+ outline。
        /// Editor 创建时走 SerializedObject 路径（不适用于运行时纯代码创建场景）。</summary>
        private static void ApplyRedWithBlackOutline(TMP_Text label)
        {
            if (label == null) return;
            var red = new Color(1f, 0.15f, 0.2f, 1f);
            label.color = red;                          // Graphic 层
            // faceColor 写入要求 material 已就绪；在 AddComponent 后首帧可能尚未 ForceMeshUpdate
            // 这里 try/catch 兜底（CLAUDE.md 踩坑：faceColor setter NRE）
            try { label.faceColor = red; } catch { /* material 未初始化时静默跳过，color 已够用 */ }
            label.outlineColor = new Color32(0, 0, 0, 255);
            label.outlineWidth = 0.25f;
        }
    }
}
