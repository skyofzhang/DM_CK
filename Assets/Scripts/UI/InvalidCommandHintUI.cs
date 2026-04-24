using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §34 F8 无效指令提示（audit-r8 补齐）
    ///
    /// 订阅 SurvivalGameManager.OnInvalidCommandHint（服务端单播）：
    ///   - invalid_cmd_5  ：玩家发送未配置弹幕指令（数字超范围/非法）
    ///   - wrong_phase_6  ：玩家在错误阶段发指令（白天发 6 攻击、夜晚发 1 采集食物）
    ///
    /// 渲染：屏幕底部中央 Y=280 单行气泡（黄底 + 黑字）；ttl 毫秒 hold + 0.3s fade out。
    /// 允许多条消息堆叠（上下排列）——纯 Toast，不走 ModalRegistry。
    ///
    /// 挂载规则（CLAUDE.md #7）：挂 Canvas/GameUIPanel（常驻激活），不在 Awake 中 SetActive(false)。
    /// 字体：Alibaba 优先 + ChineseFont SDF fallback。
    ///
    /// Inspector 可选：
    ///   _templatePrefab — 单条 Toast 模板 Prefab（未填则运行时自建）
    ///   _stackContainer — 堆叠容器（未填则以 this.transform 为容器，新 toast 挂在本脚本 GO 下）
    /// </summary>
    public class InvalidCommandHintUI : MonoBehaviour
    {
        [Header("堆叠容器（未填则使用本脚本所在 GO，新 toast 依次加入）")]
        [SerializeField] private RectTransform _stackContainer;

        // 字体路径
        private const string AlibabaFontPath = "Fonts/AlibabaPuHuiTi-3-85-Bold SDF";
        private const string ChineseFontPath = "Fonts/ChineseFont SDF";

        // 默认 ttl（ms），服务端未给时的兜底
        private const int DEFAULT_TTL_MS = 4000;

        // 淡出时长
        private const float FADE_OUT_SEC = 0.3f;

        // 气泡布局
        private const float TOAST_BOTTOM_Y    = 280f;    // 贴底偏上
        private const float TOAST_LINE_HEIGHT = 54f;     // 堆叠步长
        private const float TOAST_WIDTH       = 640f;    // 单条宽度
        private const float TOAST_HEIGHT      = 48f;     // 单条高度

        // 字体缓存
        private TMP_FontAsset _font;

        // 活跃 toast 列表（按加入顺序排布）
        private readonly List<RectTransform> _activeToasts = new List<RectTransform>();

        // 订阅状态
        private bool _subscribed;

        // ── 生命周期 ──────────────────────────────────────────────────────

        private void Start()
        {
            LoadFont();
            EnsureStackContainer();
            TrySubscribe();
        }

        private void OnEnable()  { TrySubscribe(); }
        private void OnDisable() { Unsubscribe(); }
        private void OnDestroy() { Unsubscribe(); }

        private void Update()
        {
            if (!_subscribed) TrySubscribe();
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;
            sgm.OnInvalidCommandHint += HandleHint;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
                sgm.OnInvalidCommandHint -= HandleHint;
            _subscribed = false;
        }

        private void LoadFont()
        {
            _font = Resources.Load<TMP_FontAsset>(AlibabaFontPath);
            if (_font == null) _font = Resources.Load<TMP_FontAsset>(ChineseFontPath);
        }

        private void EnsureStackContainer()
        {
            if (_stackContainer != null) return;
            var rt = transform as RectTransform;
            if (rt != null)
            {
                _stackContainer = rt;
                return;
            }
            // 挂到非 RectTransform GO 上的兜底（不太可能）：自建一层
            var go = new GameObject("ToastStack");
            var newRt = go.AddComponent<RectTransform>();
            newRt.SetParent(transform, false);
            newRt.anchorMin = Vector2.zero;
            newRt.anchorMax = Vector2.one;
            newRt.offsetMin = Vector2.zero;
            newRt.offsetMax = Vector2.zero;
            _stackContainer = newRt;
        }

        // ── 事件回调 ──────────────────────────────────────────────────────

        private void HandleHint(InvalidCommandHintData data)
        {
            if (data == null) return;
            if (string.IsNullOrEmpty(data.msg)) return;
            if (_stackContainer == null) EnsureStackContainer();
            if (_stackContainer == null) return;

            int ttlMs = data.ttl > 0 ? data.ttl : DEFAULT_TTL_MS;
            StartCoroutine(ShowToast(data.msg, ttlMs / 1000f));
        }

        // ── 协程：单条 Toast 生命周期 ──────────────────────────────────────

        private IEnumerator ShowToast(string msg, float holdSec)
        {
            // 新建单条气泡 GO
            var go = new GameObject("InvalidCmdToast");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(_stackContainer, false);

            // 底部中央锚点
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(TOAST_WIDTH, TOAST_HEIGHT);

            // 背景（黄色半透明）
            var bg = go.AddComponent<Image>();
            bg.color = new Color(1f, 0.88f, 0.2f, 0.92f); // 黄色
            bg.raycastTarget = false;

            // 文本子节点
            var labelGo = new GameObject("Label");
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.SetParent(rt, false);
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(16f, 4f);
            labelRt.offsetMax = new Vector2(-16f, -4f);
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize  = 26;
            tmp.fontStyle = FontStyles.Bold;
            tmp.text      = msg;
            if (_font != null) tmp.font = _font;

            // CLAUDE.md 踩坑：TMP 新建时 faceColor 默认白色，Graphic .color=black 不生效，需双写
            var black = new Color(0.1f, 0.1f, 0.1f, 1f);
            tmp.color = black;
            try { tmp.faceColor = black; } catch { /* material 未初始化时兜底 */ }
            tmp.raycastTarget = false;

            // 入栈 → 重排 Y 位置
            _activeToasts.Add(rt);
            RelayoutStack();

            // hold
            if (holdSec > 0f) yield return new WaitForSecondsRealtime(holdSec);

            // fade out
            float t = 0f;
            while (t < FADE_OUT_SEC)
            {
                t += Time.unscaledDeltaTime;
                float a = 1f - Mathf.Clamp01(t / FADE_OUT_SEC);
                if (bg != null)  { var c = bg.color;  c.a = 0.92f * a; bg.color  = c; }
                if (tmp != null) { var c = tmp.color; c.a = a;         tmp.color = c; }
                yield return null;
            }

            // 清理
            _activeToasts.Remove(rt);
            if (rt != null) Destroy(rt.gameObject);
            RelayoutStack();
        }

        /// <summary>按当前 _activeToasts 索引重新排布 Y 轴，最新消息在下方（或堆叠在已有消息之上）。</summary>
        private void RelayoutStack()
        {
            for (int i = 0; i < _activeToasts.Count; i++)
            {
                var rt = _activeToasts[i];
                if (rt == null) continue;
                // 第 0 条最下，靠 TOAST_BOTTOM_Y；往上堆
                rt.anchoredPosition = new Vector2(0f, TOAST_BOTTOM_Y + i * TOAST_LINE_HEIGHT);
            }
        }
    }
}
