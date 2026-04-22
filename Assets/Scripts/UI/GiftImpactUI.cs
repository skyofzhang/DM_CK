using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §34.4 E3c 礼物效果反馈 —— 顶部横幅 3s
    ///
    /// 订阅 SurvivalGameManager.OnGiftImpact，在顶部展示：
    ///   "{playerName} 的 {giftName} → {impacts}"
    ///
    /// 闭合付费反馈环：付费 → 视觉奇观 → 效果确认 → 满足感 → 再次付费。
    ///
    /// 挂载规则：
    ///   挂 Canvas 顶层（GiftImpactBanner GO），常驻激活；CanvasGroup.alpha 做显隐。
    ///
    /// Inspector 必填：
    ///   _bannerRoot        — 横幅根节点
    ///   _bannerCanvasGroup — CanvasGroup 控制淡入淡出
    ///   _bannerText        — 主文字 TMP
    /// </summary>
    public class GiftImpactUI : MonoBehaviour
    {
        [Header("横幅")]
        [SerializeField] private RectTransform     _bannerRoot;
        [SerializeField] private CanvasGroup       _bannerCanvasGroup;
        [SerializeField] private TextMeshProUGUI   _bannerText;

        private const float DURATION = 3.0f;
        private const float FADE_IN  = 0.25f;
        private const float FADE_OUT = 0.40f;

        // ── 内部状态 ──────────────────────────────────────────────────────
        // 队列：同时来多条 gift_impact 时按序播放，避免互相打断
        private readonly Queue<GiftImpactData> _queue = new Queue<GiftImpactData>();
        private Coroutine _runner;
        private bool      _subscribed = false;

        // ── 生命周期 ──────────────────────────────────────────────────────

        private void Start()
        {
            if (_bannerRoot != null) _bannerRoot.gameObject.SetActive(false);
            if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = 0f;
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
            sgm.OnGiftImpact += HandleGiftImpact;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null) sgm.OnGiftImpact -= HandleGiftImpact;
            _subscribed = false;
        }

        // ── 事件回调 ──────────────────────────────────────────────────────

        private void HandleGiftImpact(GiftImpactData d)
        {
            if (d == null) return;

            // §34.4 E3c 单播过滤：T1 fairy_wand 等 privateOnly 消息仅发给发送者自己，
            // broadcaster/其他观众的广播横幅应忽略，避免 fairy_wand 刷屏全场。
            // 该客户端为主播侧（NetworkManager 无"本地 PlayerId"概念），privateOnly=true 一律跳过。
            if (d.privateOnly) return;

            _queue.Enqueue(d);
            if (_runner == null)
                _runner = StartCoroutine(RunQueue());
        }

        // ── 队列驱动 ──────────────────────────────────────────────────────

        private IEnumerator RunQueue()
        {
            while (_queue.Count > 0)
            {
                var d = _queue.Dequeue();
                yield return PlayBanner(d);
            }
            _runner = null;
        }

        private IEnumerator PlayBanner(GiftImpactData d)
        {
            if (_bannerRoot == null || _bannerCanvasGroup == null) yield break;

            _bannerRoot.gameObject.SetActive(true);

            if (_bannerText != null)
            {
                string player  = string.IsNullOrEmpty(d.playerName) ? "观众" : d.playerName;
                string giftN   = string.IsNullOrEmpty(d.giftName)   ? "礼物" : d.giftName;
                string impacts = string.IsNullOrEmpty(d.impacts)    ? ""     : d.impacts;
                _bannerText.text = $"{player} 的 {giftN} → {impacts}";
            }

            // 淡入
            float t = 0f;
            while (t < FADE_IN)
            {
                t += Time.unscaledDeltaTime;
                _bannerCanvasGroup.alpha = Mathf.Lerp(0f, 1f, Mathf.Clamp01(t / FADE_IN));
                yield return null;
            }
            _bannerCanvasGroup.alpha = 1f;

            yield return new WaitForSeconds(DURATION);

            // 淡出
            t = 0f;
            while (t < FADE_OUT)
            {
                t += Time.unscaledDeltaTime;
                _bannerCanvasGroup.alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(t / FADE_OUT));
                yield return null;
            }
            _bannerCanvasGroup.alpha = 0f;
            _bannerRoot.gameObject.SetActive(false);
        }
    }
}
