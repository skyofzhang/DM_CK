using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §34 Layer 2 组 A B8d 仙女棒满级全屏 —— 金色闪光 + 跑马灯
    ///
    /// 订阅 SurvivalGameManager.OnFairyWandMaxed：
    ///   1. 全屏金色 Image：alpha 0 → 0.8 → 0，总时长 0.5s（峰值 0.25s）
    ///   2. 跑马灯文字 "满级矿工达成！{playerName}" 从右 → 左滑动 3s
    ///
    /// 挂载规则（CLAUDE.md #7）：挂 Canvas/FairyWandMaxedBanner（常驻激活），Awake 不 SetActive(false)；
    ///   子节点 _flashRoot / _marqueeRoot 初始 inactive，由协程控制。
    ///
    /// Inspector 必填：
    ///   _flashRoot    — 全屏金闪根节点（Image 子节点，anchor 0-1 铺满）
    ///   _flashImage   — 全屏 Image（Image.color = (1, 0.85, 0.2, 0)；运行时驱动 alpha）
    ///   _marqueeRoot  — 跑马灯容器（容纳 _marqueeText 的 RectTransform）
    ///   _marqueeText  — TMP（初始 alpha=0，anchoredPosition.x 由协程驱动）
    /// </summary>
    public class FairyWandMaxedBanner : MonoBehaviour
    {
        [Header("全屏金色闪光")]
        [SerializeField] private RectTransform   _flashRoot;
        [SerializeField] private Image           _flashImage;

        [Header("跑马灯文字")]
        [SerializeField] private RectTransform   _marqueeRoot;
        [SerializeField] private TextMeshProUGUI _marqueeText;

        private const float FLASH_DURATION_SEC   = 0.5f;
        private const float FLASH_PEAK_ALPHA     = 0.8f;
        private const float MARQUEE_DURATION_SEC = 3f;
        private const float MARQUEE_START_X      = 960f;   // 从屏幕右侧 +960 进入（16:9 ~1920 宽）
        private const float MARQUEE_END_X        = -960f;  // 到左侧 -960 消失

        private bool _subscribed;
        private Coroutine _flashCo;
        private Coroutine _marqueeCo;

        // ── 生命周期 ──────────────────────────────────────────────────────

        private void Start()
        {
            if (_flashRoot   != null) _flashRoot.gameObject.SetActive(false);
            if (_marqueeRoot != null) _marqueeRoot.gameObject.SetActive(false);
            if (_flashImage  != null)
            {
                var c = _flashImage.color;
                c.a = 0f;
                _flashImage.color = c;
            }
            TrySubscribe();
        }

        private void OnEnable()  { TrySubscribe(); }
        private void OnDisable() { Unsubscribe(); }
        private void OnDestroy()
        {
            Unsubscribe();
            if (_flashCo   != null) { StopCoroutine(_flashCo);   _flashCo   = null; }
            if (_marqueeCo != null) { StopCoroutine(_marqueeCo); _marqueeCo = null; }
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
            sgm.OnFairyWandMaxed += HandleMaxed;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null) sgm.OnFairyWandMaxed -= HandleMaxed;
            _subscribed = false;
        }

        // ── 事件回调 ──────────────────────────────────────────────────────

        private void HandleMaxed(FairyWandMaxedData data)
        {
            if (data == null) return;
            string playerName = string.IsNullOrEmpty(data.playerName) ? "观众" : data.playerName;

            if (_flashCo   != null) StopCoroutine(_flashCo);
            if (_marqueeCo != null) StopCoroutine(_marqueeCo);

            _flashCo   = StartCoroutine(PlayFlash());
            _marqueeCo = StartCoroutine(PlayMarquee($"满级矿工达成！{playerName}"));
        }

        private IEnumerator PlayFlash()
        {
            if (_flashRoot == null || _flashImage == null) yield break;

            _flashRoot.gameObject.SetActive(true);

            float half = FLASH_DURATION_SEC * 0.5f;
            float t = 0f;
            // 上升：0 → peakAlpha
            while (t < half)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(0f, FLASH_PEAK_ALPHA, t / half);
                var c = _flashImage.color; c.a = a; _flashImage.color = c;
                yield return null;
            }
            // 下降：peakAlpha → 0
            t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(FLASH_PEAK_ALPHA, 0f, t / half);
                var c = _flashImage.color; c.a = a; _flashImage.color = c;
                yield return null;
            }
            var end = _flashImage.color; end.a = 0f; _flashImage.color = end;
            _flashRoot.gameObject.SetActive(false);
            _flashCo = null;
        }

        private IEnumerator PlayMarquee(string text)
        {
            if (_marqueeRoot == null || _marqueeText == null) yield break;

            _marqueeText.text = text;
            _marqueeRoot.gameObject.SetActive(true);

            // 初始位置：右侧 +MARQUEE_START_X；文字 alpha = 1
            _marqueeRoot.anchoredPosition = new Vector2(MARQUEE_START_X, _marqueeRoot.anchoredPosition.y);
            var c = _marqueeText.color; c.a = 1f; _marqueeText.color = c;

            float t = 0f;
            while (t < MARQUEE_DURATION_SEC)
            {
                t += Time.deltaTime;
                float lerp = t / MARQUEE_DURATION_SEC;
                float x = Mathf.Lerp(MARQUEE_START_X, MARQUEE_END_X, lerp);
                _marqueeRoot.anchoredPosition = new Vector2(x, _marqueeRoot.anchoredPosition.y);
                yield return null;
            }

            _marqueeRoot.gameObject.SetActive(false);
            _marqueeCo = null;
        }
    }
}
