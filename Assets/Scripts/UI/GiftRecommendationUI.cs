using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §34.4 E4 精准付费触发 —— 礼物推荐气泡
    ///
    /// 订阅 SurvivalGameManager.OnResourceUpdate，从 ResourceUpdateData.giftRecommendation 驱动：
    ///   在 GiftIconBar 上方对应 giftId 卡片正上方弹出"推荐理由"气泡。
    ///
    /// 四级 urgency 视觉（策划案 5342-5347）：
    ///   gentle   : alpha=0.3，无脉冲
    ///   medium   : 灰色半透 (0.5,0.5,0.5,0.7)，淡淡边框
    ///   high     : 橙色 (1,0.6,0.1)，加粗文字，轻微脉冲 1.5s
    ///   critical : 红色 (1,0.1,0.1)，剧烈脉冲 0.5s + 闪烁
    ///
    /// 客户端防抖：同一 giftId 60s 内不重复展示（服务端已防，双保险）。
    ///
    /// 挂载规则：
    ///   挂 Canvas/GameUIPanel/GiftIconBar/GiftRecommendBubble（常驻激活，靠 alpha/Image 显隐）
    ///
    /// Inspector 必填：
    ///   _bubbleRoot      — 气泡根 RectTransform
    ///   _bubbleBg        — 气泡背景 Image
    ///   _reasonText      — 推荐理由 TMP
    ///   _giftIconBar     — 底部礼物栏 RectTransform（用于定位气泡到对应卡片上方）
    /// </summary>
    public class GiftRecommendationUI : MonoBehaviour
    {
        [Header("气泡 UI")]
        [SerializeField] private RectTransform     _bubbleRoot;
        [SerializeField] private Image             _bubbleBg;
        [SerializeField] private TextMeshProUGUI   _reasonText;

        [Header("定位锚点（GiftIconBar）")]
        [SerializeField] private RectTransform _giftIconBar;

        // ── 礼物顺序（与 RebuildGiftIconBar 一致，T1-T6）────────────────────
        //   giftId → 卡片索引（0 基）
        private static readonly Dictionary<string, int> GIFT_INDEX = new Dictionary<string, int>
        {
            { "fairy_wand",      0 },
            { "ability_pill",    1 },
            { "donut",           2 },
            { "energy_battery",  3 },
            { "love_explosion",  4 },
            { "mystery_airdrop", 5 },
        };

        // ── 四级 urgency 视觉参数 ────────────────────────────────────────

        private static readonly Color COLOR_GENTLE   = new Color(0.60f, 0.60f, 0.60f, 0.30f);
        private static readonly Color COLOR_MEDIUM   = new Color(0.50f, 0.50f, 0.50f, 0.70f);
        private static readonly Color COLOR_HIGH     = new Color(1.00f, 0.60f, 0.10f, 0.85f);
        private static readonly Color COLOR_CRITICAL = new Color(1.00f, 0.10f, 0.10f, 0.90f);

        // 脉冲参数（period=0 表示不脉冲）
        private const float PERIOD_HIGH     = 1.5f;
        private const float PERIOD_CRITICAL = 0.5f;
        private const float AMP_HIGH        = 0.25f;
        private const float AMP_CRITICAL    = 0.50f;

        // ── 客户端防抖（同 giftId 60s 内不重复显示）─────────────────────

        private const float CLIENT_DEBOUNCE_SEC = 60f;

        // ── 内部状态 ──────────────────────────────────────────────────────

        private bool   _subscribed = false;

        // 上次显示的 giftId + 显示时刻（Time.unscaledTime 秒），空 = 未显示过
        private string _lastShownGiftId  = null;
        private float  _lastShownTime    = -999f;

        // 当前显示参数（Update 脉冲用）
        private bool   _isShowing       = false;
        private Color  _currentBaseColor = Color.white;
        private float  _currentPeriod    = 0f;
        private float  _currentAmplitude = 0f;
        private float  _currentBaseAlpha = 0f;
        private bool   _currentBold      = false;

        // ── 生命周期 ──────────────────────────────────────────────────────

        private void Start()
        {
            if (_bubbleRoot != null) _bubbleRoot.gameObject.SetActive(false);
            TrySubscribe();
        }

        private void OnEnable()  { TrySubscribe(); }
        private void OnDisable() { Unsubscribe(); }
        private void OnDestroy() { Unsubscribe(); }

        private void Update()
        {
            if (!_subscribed) TrySubscribe();
            if (!_isShowing) return;
            UpdatePulse();
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;
            sgm.OnResourceUpdate += HandleResourceUpdate;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null) sgm.OnResourceUpdate -= HandleResourceUpdate;
            _subscribed = false;
        }

        // ── 事件回调 ──────────────────────────────────────────────────────

        private void HandleResourceUpdate(ResourceUpdateData data)
        {
            if (data == null) return;
            var rec = data.giftRecommendation;

            // 无推荐 → 隐藏气泡
            if (rec == null || string.IsNullOrEmpty(rec.giftId))
            {
                HideBubble();
                return;
            }

            // 客户端防抖：同 giftId 60s 内不重复展示
            float now = Time.unscaledTime;
            if (rec.giftId == _lastShownGiftId && now - _lastShownTime < CLIENT_DEBOUNCE_SEC)
            {
                // 已在展示同一礼物：只刷新文案/视觉，不重置位置 & 时间
                ApplyVisual(rec);
                return;
            }

            _lastShownGiftId = rec.giftId;
            _lastShownTime   = now;
            ShowBubble(rec);
        }

        // ── 显示/隐藏 ─────────────────────────────────────────────────────

        private void ShowBubble(GiftRecommendationData rec)
        {
            if (_bubbleRoot == null) return;
            _bubbleRoot.gameObject.SetActive(true);
            _isShowing = true;

            // 定位到对应 giftId 卡片上方
            PositionAboveGiftCard(rec.giftId);
            ApplyVisual(rec);
        }

        private void HideBubble()
        {
            if (_bubbleRoot != null) _bubbleRoot.gameObject.SetActive(false);
            _isShowing = false;
        }

        private void PositionAboveGiftCard(string giftId)
        {
            if (_bubbleRoot == null || _giftIconBar == null) return;
            if (!GIFT_INDEX.TryGetValue(giftId, out int idx)) return;

            // GiftIconBar 6 张卡片等宽平铺 → 第 idx 张卡片水平中心 x 比例 = (idx + 0.5) / 6
            float ratioX = (idx + 0.5f) / 6f;

            // 气泡锚定到 GiftIconBar 的上方（bar 的顶部 = y=1 方向），pivot 设在气泡底部中点
            _bubbleRoot.anchorMin = new Vector2(ratioX, 1f);
            _bubbleRoot.anchorMax = new Vector2(ratioX, 1f);
            _bubbleRoot.pivot     = new Vector2(0.5f, 0f);
            _bubbleRoot.anchoredPosition = new Vector2(0f, 10f);  // 距 bar 顶部 10px
        }

        private void ApplyVisual(GiftRecommendationData rec)
        {
            // 文案
            if (_reasonText != null)
            {
                _reasonText.text = rec.reason ?? "";
            }

            // urgency → 颜色/脉冲/加粗
            _currentBold      = false;
            _currentPeriod    = 0f;
            _currentAmplitude = 0f;

            switch (rec.urgency)
            {
                case "gentle":
                    _currentBaseColor = COLOR_GENTLE;
                    break;
                case "medium":
                    _currentBaseColor = COLOR_MEDIUM;
                    break;
                case "high":
                    _currentBaseColor = COLOR_HIGH;
                    _currentPeriod    = PERIOD_HIGH;
                    _currentAmplitude = AMP_HIGH;
                    _currentBold      = true;
                    break;
                case "critical":
                    _currentBaseColor = COLOR_CRITICAL;
                    _currentPeriod    = PERIOD_CRITICAL;
                    _currentAmplitude = AMP_CRITICAL;
                    _currentBold      = true;
                    break;
                default:
                    _currentBaseColor = COLOR_MEDIUM;
                    break;
            }

            _currentBaseAlpha = _currentBaseColor.a;

            if (_bubbleBg != null)
                _bubbleBg.color = _currentBaseColor;

            if (_reasonText != null)
            {
                _reasonText.fontStyle = _currentBold ? FontStyles.Bold : FontStyles.Normal;
                var tc = _reasonText.color;
                tc.a = 1f;
                _reasonText.color = tc;
            }
        }

        private void UpdatePulse()
        {
            if (_bubbleBg == null) return;
            if (_currentPeriod <= 0f) return;  // gentle/medium 不脉冲

            float phase = (Time.unscaledTime * Mathf.PI * 2f) / _currentPeriod;
            float pulse = Mathf.Sin(phase);
            float alpha = _currentBaseAlpha * (1f + _currentAmplitude * pulse);
            alpha = Mathf.Clamp01(alpha);

            var c = _bubbleBg.color;
            c.a = alpha;
            _bubbleBg.color = c;

            // critical：额外闪烁（阶跃可见/不可见）
            if (_currentAmplitude >= AMP_CRITICAL)
            {
                // 使用半周期翻转（sin > 0 显示，< 0 半透）让"闪烁"感更强
                float flick = pulse > 0f ? 1f : 0.4f;
                if (_reasonText != null)
                {
                    var tc = _reasonText.color;
                    tc.a = flick;
                    _reasonText.color = tc;
                }
            }
        }
    }
}
