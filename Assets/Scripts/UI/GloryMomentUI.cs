using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §34.4 E3a 荣耀时刻 —— T3+ 礼物顶部横幅
    ///
    /// 订阅 SurvivalGameManager.OnGloryMoment，根据 GloryMomentData 组合触发四种场景：
    ///
    ///   ┌───────────────┬──────────────────────────────────────────────────────────┐
    ///   │ 场景          │ 视觉 + 时长                                               │
    ///   ├───────────────┼──────────────────────────────────────────────────────────┤
    ///   │ 基础 T3+      │ 顶部横幅 3s："{name} 送出 {giftName}！当前排名 #{rank}"   │
    ///   │ 超越          │ 追加一行"超越了 {overtaken}！" 共 4s                       │
    ///   │ isNewFirst    │ 全屏金色爆发 + 横幅 5s + 镜头震动                         │
    ///   │ 第一被夺      │ 屏幕红闪 0.2s + 横幅 4s                                   │
    ///   │ 非 isNewFirst │ 追加"距第一名仅差 {gapToFirst} 贡献"                       │
    ///   └───────────────┴──────────────────────────────────────────────────────────┘
    ///
    /// 挂载规则（CLAUDE.md #7）：
    ///   挂 Canvas 顶层（GloryMomentBanner GO），常驻激活；用 CanvasGroup.alpha 做显隐。
    ///
    /// Inspector 必填：
    ///   _bannerRoot         — 顶部横幅容器（CanvasGroup 控制 alpha）
    ///   _bannerCanvasGroup  — 横幅 CanvasGroup
    ///   _bannerBg           — 横幅背景 Image（色调会随场景切换）
    ///   _bannerText         — 主文字 TMP
    ///   _subText            — 副文字（gap/超越提示）
    ///   _goldBurst          — 全屏金色爆发 Image（isNewFirst 专用）
    ///   _redFlash           — 全屏红闪 Image（第一被夺专用）
    /// </summary>
    public class GloryMomentUI : MonoBehaviour
    {
        [Header("顶部横幅")]
        [SerializeField] private RectTransform     _bannerRoot;
        [SerializeField] private CanvasGroup       _bannerCanvasGroup;
        [SerializeField] private Image             _bannerBg;
        [SerializeField] private TextMeshProUGUI   _bannerText;
        [SerializeField] private TextMeshProUGUI   _subText;

        [Header("全屏特效")]
        [SerializeField] private Image _goldBurst;   // isNewFirst 专用
        [SerializeField] private Image _redFlash;    // 第一被夺专用

        // ── 视觉常量 ──────────────────────────────────────────────────────
        private static readonly Color BANNER_COLOR_BASIC        = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        private static readonly Color BANNER_COLOR_OVERTAKE     = new Color(0.3f, 0.15f, 0.05f, 0.90f);
        private static readonly Color BANNER_COLOR_NEW_FIRST    = new Color(0.6f, 0.45f, 0.05f, 0.95f);
        private static readonly Color BANNER_COLOR_DETHRONE     = new Color(0.5f, 0.05f, 0.05f, 0.95f);
        private static readonly Color GOLD_BURST_COLOR          = new Color(1.00f, 0.85f, 0.20f, 0.60f);
        private static readonly Color RED_FLASH_COLOR           = new Color(1.00f, 0.10f, 0.10f, 0.50f);

        private const float FADE_IN  = 0.25f;
        private const float FADE_OUT = 0.40f;
        private const float DURATION_BASIC      = 3.0f;
        private const float DURATION_OVERTAKE   = 4.0f;
        private const float DURATION_NEW_FIRST  = 5.0f;
        private const float DURATION_DETHRONE   = 4.0f;
        private const float RED_FLASH_SEC       = 0.2f;
        private const float CAMERA_SHAKE_SEC    = 0.5f;
        private const float CAMERA_SHAKE_AMP    = 0.2f;

        // ── 内部状态 ──────────────────────────────────────────────────────
        private Coroutine _currentBanner;
        private bool      _subscribed = false;

        // ── 生命周期 ──────────────────────────────────────────────────────

        private void Start()
        {
            HideAll();
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
            sgm.OnGloryMoment += HandleGloryMoment;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null) sgm.OnGloryMoment -= HandleGloryMoment;
            _subscribed = false;
        }

        // ── 事件回调 ──────────────────────────────────────────────────────

        private void HandleGloryMoment(GloryMomentData d)
        {
            if (d == null) return;

            // 场景判定（互斥优先级：第一被夺 > 加冕 > 超越 > 基础）
            bool dethrone   = d.isNewFirst && !string.IsNullOrEmpty(d.overtaken) && d.rank == 1;
            bool newFirst   = d.isNewFirst && !dethrone;
            bool overtake   = !d.isNewFirst && !string.IsNullOrEmpty(d.overtaken);

            string mainLine = $"{d.playerName} 送出 {d.giftName}！当前排名 #{d.rank}";
            string subLine  = "";
            Color  bannerColor;
            float  duration;

            if (dethrone)
            {
                bannerColor = BANNER_COLOR_DETHRONE;
                duration    = DURATION_DETHRONE;
                mainLine    = $"守护之王易主！{d.playerName} 取代 {d.overtaken}！";
                subLine     = "";
            }
            else if (newFirst)
            {
                bannerColor = BANNER_COLOR_NEW_FIRST;
                duration    = DURATION_NEW_FIRST;
                mainLine    = $"{d.playerName} 加冕守护之王！";
                subLine     = $"{d.giftName} · 当前排名 #{d.rank}";
            }
            else if (overtake)
            {
                bannerColor = BANNER_COLOR_OVERTAKE;
                duration    = DURATION_OVERTAKE;
                subLine     = $"超越了 {d.overtaken}！";
                // 非 isNewFirst 且 rank != 1：额外追加"距第一名仅差 X"
                if (d.rank != 1 && d.gapToFirst > 0)
                    subLine += $"    距第一名仅差 {d.gapToFirst} 贡献";
            }
            else
            {
                bannerColor = BANNER_COLOR_BASIC;
                duration    = DURATION_BASIC;
                if (d.rank != 1 && d.gapToFirst > 0)
                    subLine = $"距第一名仅差 {d.gapToFirst} 贡献";
            }

            if (_currentBanner != null)
                StopCoroutine(_currentBanner);
            _currentBanner = StartCoroutine(PlayBanner(bannerColor, mainLine, subLine, duration,
                                                       newFirst, dethrone));
        }

        // ── 动画协程 ──────────────────────────────────────────────────────

        private IEnumerator PlayBanner(Color bannerColor, string mainLine, string subLine,
                                       float duration, bool newFirst, bool dethrone)
        {
            // 先处理全屏特效（不等待）
            if (dethrone)
            {
                StartCoroutine(FlashImage(_redFlash, RED_FLASH_COLOR, RED_FLASH_SEC));
            }
            if (newFirst)
            {
                StartCoroutine(FlashImage(_goldBurst, GOLD_BURST_COLOR, DURATION_NEW_FIRST * 0.5f));
                // 加冕震屏：复用 SurvivalCameraController.Shake（静态 API，内部 null-check）。
                // 避免与 ShakeCoroutine 同帧争写 Camera.main.localPosition 互相覆盖。
                // SurvivalCameraController.Instance 为 null 时 Shake 直接返回，GloryMoment 其它效果不受影响。
                SurvivalCameraController.Shake(CAMERA_SHAKE_AMP, CAMERA_SHAKE_SEC);
                if (SurvivalCameraController.Instance == null)
                    Debug.LogWarning("[GloryMomentUI] SurvivalCameraController.Instance 为空，震屏降级为空操作");
            }

            // 设置文字 + 背景色
            if (_bannerBg != null) _bannerBg.color = bannerColor;
            if (_bannerText != null) _bannerText.text = mainLine;
            if (_subText != null)
            {
                _subText.text = subLine;
                _subText.gameObject.SetActive(!string.IsNullOrEmpty(subLine));
            }
            if (_bannerRoot != null) _bannerRoot.gameObject.SetActive(true);

            // 淡入
            yield return FadeCanvasGroup(_bannerCanvasGroup, 0f, 1f, FADE_IN);

            // 保持
            yield return new WaitForSeconds(duration);

            // 淡出
            yield return FadeCanvasGroup(_bannerCanvasGroup, 1f, 0f, FADE_OUT);

            if (_bannerRoot != null) _bannerRoot.gameObject.SetActive(false);
            _currentBanner = null;
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float dur)
        {
            if (cg == null) yield break;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur));
                yield return null;
            }
            cg.alpha = to;
        }

        private IEnumerator FlashImage(Image img, Color color, float dur)
        {
            if (img == null) yield break;
            img.gameObject.SetActive(true);
            img.color = color;

            // 0.1s 淡入，余下时间淡出
            float fadeIn  = Mathf.Min(0.1f, dur * 0.3f);
            float fadeOut = dur - fadeIn;

            float t = 0f;
            Color c = img.color;
            while (t < fadeIn)
            {
                t += Time.unscaledDeltaTime;
                c.a = Mathf.Lerp(0f, color.a, Mathf.Clamp01(t / fadeIn));
                img.color = c;
                yield return null;
            }

            t = 0f;
            while (t < fadeOut)
            {
                t += Time.unscaledDeltaTime;
                c.a = Mathf.Lerp(color.a, 0f, Mathf.Clamp01(t / fadeOut));
                img.color = c;
                yield return null;
            }
            c.a = 0f;
            img.color = c;
            img.gameObject.SetActive(false);
        }

        // ── 辅助 ──────────────────────────────────────────────────────────

        private void HideAll()
        {
            if (_bannerRoot != null) _bannerRoot.gameObject.SetActive(false);
            if (_bannerCanvasGroup != null) _bannerCanvasGroup.alpha = 0f;
            if (_goldBurst != null)
            {
                var c = _goldBurst.color; c.a = 0f; _goldBurst.color = c;
                _goldBurst.gameObject.SetActive(false);
            }
            if (_redFlash != null)
            {
                var c = _redFlash.color; c.a = 0f; _redFlash.color = c;
                _redFlash.gameObject.SetActive(false);
            }
        }
    }
}
