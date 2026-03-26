using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

namespace DrscfZ.UI
{
    /// <summary>
    /// 通用 UI 动画 Coroutine 工具集（static，无生命周期依赖）
    ///
    /// 当前使用方：
    ///   SurvivalTopBarUI — 数值弹跳动画（ShakeScale）
    ///
    /// 礼物/VIP 入场特效统一由 GiftAnimationUI 通过 WebM 视频实现，不再使用本工具集。
    /// </summary>
    public static class GiftEffectSystem
    {
        // =====================================================================
        //  淡入 / 淡出
        // =====================================================================

        /// <summary>
        /// 将 Graphic 的 Alpha 从 0 线性插值到 targetAlpha，duration 秒内完成。
        /// 调用前无需提前 SetActive；此方法会在动画开始前激活 GameObject。
        /// </summary>
        public static IEnumerator FadeIn(Graphic graphic, float duration, float targetAlpha = 1f)
        {
            if (graphic == null) yield break;

            float elapsed = 0f;
            Color color = graphic.color;
            color.a = 0f;
            graphic.color = color;
            graphic.gameObject.SetActive(true);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                color.a = Mathf.Lerp(0f, targetAlpha, t);
                graphic.color = color;
                yield return null;
            }

            color.a = targetAlpha;
            graphic.color = color;
        }

        /// <summary>
        /// 将 Graphic 的 Alpha 从当前值线性插值到 0，duration 秒内完成。
        /// 完成后自动 SetActive(false)。
        /// </summary>
        public static IEnumerator FadeOut(Graphic graphic, float duration)
        {
            if (graphic == null) yield break;

            float elapsed = 0f;
            Color color = graphic.color;
            float startAlpha = color.a;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                color.a = Mathf.Lerp(startAlpha, 0f, t);
                graphic.color = color;
                yield return null;
            }

            color.a = 0f;
            graphic.color = color;
            graphic.gameObject.SetActive(false);
        }

        // =====================================================================
        //  缩放弹入 (EaseOutBack)
        // =====================================================================

        /// <summary>
        /// 将 Transform.localScale 从 0 弹出到 Vector3.one，带轻微过冲（overshoot）。
        /// 近似 EaseOutBack 曲线，无需第三方动画库。
        /// </summary>
        public static IEnumerator PopIn(Transform target, float duration, float overshoot = 1.2f)
        {
            if (target == null) yield break;

            float elapsed = 0f;
            target.localScale = Vector3.zero;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scale = EaseOutBack(t, overshoot);
                target.localScale = Vector3.one * Mathf.Max(0f, scale);
                yield return null;
            }

            target.localScale = Vector3.one;
        }

        /// <summary>
        /// 将 Transform.localScale 从 Vector3.one 缩小到 0，带轻微缩进（EaseInBack）。
        /// </summary>
        public static IEnumerator PopOut(Transform target, float duration)
        {
            if (target == null) yield break;

            float elapsed = 0f;
            target.localScale = Vector3.one;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // EaseInBack: 反向的 EaseOutBack
                float scale = 1f - EaseOutBack(1f - t, 1.2f);
                target.localScale = Vector3.one * Mathf.Max(0f, scale);
                yield return null;
            }

            target.localScale = Vector3.zero;
        }

        // =====================================================================
        //  抖动 (Shake Scale)
        // =====================================================================

        /// <summary>
        /// 将 Transform 从 scale 1 快速抖动到 peakScale 再回到 1，重复 count 次。
        /// 每次抖动持续 singleDuration 秒。
        /// </summary>
        public static IEnumerator ShakeScale(Transform target, int count, float singleDuration, float peakScale = 1.15f)
        {
            if (target == null) yield break;

            for (int i = 0; i < count; i++)
            {
                // 放大到峰值
                float half = singleDuration * 0.5f;
                float elapsed = 0f;
                while (elapsed < half)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / half);
                    target.localScale = Vector3.one * Mathf.Lerp(1f, peakScale, t);
                    yield return null;
                }
                // 缩回1
                elapsed = 0f;
                while (elapsed < half)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / half);
                    target.localScale = Vector3.one * Mathf.Lerp(peakScale, 1f, t);
                    yield return null;
                }
            }

            target.localScale = Vector3.one;
        }

        // =====================================================================
        //  飞入 (EaseOutBounce X轴)
        // =====================================================================

        /// <summary>
        /// 将 RectTransform 从屏幕左侧 startX 水平飞入到 anchoredPosition.x = 0，
        /// Y 轴保持当前值不变，duration 秒内完成，使用 EaseOutBounce 缓动。
        /// </summary>
        public static IEnumerator FlyInFromLeft(RectTransform rt, float duration, float startX = -700f)
        {
            if (rt == null) yield break;

            float elapsed = 0f;
            float currentY = rt.anchoredPosition.y;
            Vector2 startPos = new Vector2(startX, currentY);
            Vector2 endPos = new Vector2(0f, currentY);
            rt.anchoredPosition = startPos;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = EaseOutBounce(t);
                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, easedT);
                yield return null;
            }

            rt.anchoredPosition = endPos;
        }

        // =====================================================================
        //  下落 (EaseInCubic Y轴)
        // =====================================================================

        /// <summary>
        /// 将 RectTransform 从 startY 沿 Y 轴落到 endY，使用 EaseInCubic（加速落下）。
        /// X 轴保持 anchoredPosition.x 不变。
        /// </summary>
        public static IEnumerator DropFromTop(RectTransform rt, float duration, float startY, float endY)
        {
            if (rt == null) yield break;

            float elapsed = 0f;
            float currentX = rt.anchoredPosition.x;
            rt.anchoredPosition = new Vector2(currentX, startY);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = EaseInCubic(t);
                float y = Mathf.Lerp(startY, endY, easedT);
                rt.anchoredPosition = new Vector2(currentX, y);
                yield return null;
            }

            rt.anchoredPosition = new Vector2(currentX, endY);
        }

        // =====================================================================
        //  礼物爆炸 (Scale 1→2 + Alpha 1→0)
        // =====================================================================

        /// <summary>
        /// 同时将 Graphic 的 Alpha 从 1 插值到 0，并将 Transform 的 Scale 从 1 插值到 scaleTarget，
        /// duration 秒内完成。完成后 SetActive(false)。
        /// </summary>
        public static IEnumerator ExplodeOut(Graphic graphic, float duration, float scaleTarget = 2f)
        {
            if (graphic == null) yield break;

            float elapsed = 0f;
            Color color = graphic.color;
            float startAlpha = color.a;
            Transform t = graphic.transform;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float p = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutQuart(p);

                t.localScale = Vector3.one * Mathf.Lerp(1f, scaleTarget, eased);
                color.a = Mathf.Lerp(startAlpha, 0f, eased);
                graphic.color = color;
                yield return null;
            }

            color.a = 0f;
            graphic.color = color;
            graphic.gameObject.SetActive(false);
        }

        // =====================================================================
        //  屏幕震动
        // =====================================================================

        /// <summary>
        /// 对 screenRoot 施加随机位置偏移（±magnitude px），持续 duration 秒后恢复原始位置。
        /// 传入 RectTransform 的根节点（Gift_Canvas 的根 RectTransform 即可）。
        /// </summary>
        public static IEnumerator ShakeScreen(RectTransform screenRoot, float duration, float magnitude = 5f)
        {
            if (screenRoot == null) yield break;

            Vector2 originalPos = screenRoot.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float x = Random.Range(-magnitude, magnitude);
                float y = Random.Range(-magnitude, magnitude);
                screenRoot.anchoredPosition = originalPos + new Vector2(x, y);
                yield return null;
            }

            screenRoot.anchoredPosition = originalPos;
        }

        // =====================================================================
        //  进度条动画
        // =====================================================================

        /// <summary>
        /// 将 Slider.value 从 from 线性插值到 to，duration 秒内完成。
        /// </summary>
        public static IEnumerator AnimateSlider(Slider slider, float from, float to, float duration)
        {
            if (slider == null) yield break;

            float elapsed = 0f;
            slider.value = from;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                slider.value = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            slider.value = to;
        }

        // =====================================================================
        //  资源图标飞散
        // =====================================================================

        /// <summary>
        /// 将 RectTransform 从 startPos 飞散到 endPos（EaseOutQuart），到达后淡出。
        /// alpha 从当前 Graphic.color.a 插值到 0。
        /// </summary>
        public static IEnumerator ScatterIcon(RectTransform rt, Graphic graphic,
            Vector2 startPos, Vector2 endPos, float moveDuration, float fadeDuration)
        {
            if (rt == null) yield break;

            // 移动
            float elapsed = 0f;
            rt.anchoredPosition = startPos;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveDuration);
                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, EaseOutQuart(t));
                yield return null;
            }
            rt.anchoredPosition = endPos;

            // 淡出
            if (graphic != null)
            {
                elapsed = 0f;
                Color color = graphic.color;
                float startAlpha = color.a;

                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / fadeDuration);
                    color.a = Mathf.Lerp(startAlpha, 0f, t);
                    graphic.color = color;
                    yield return null;
                }
                color.a = 0f;
                graphic.color = color;
            }
        }

        // =====================================================================
        //  横幅从左侧滑入 (EaseOutCubic)
        // =====================================================================

        /// <summary>
        /// 将 RectTransform 从 startX 水平滑入到 anchoredPosition.x = 0，
        /// 使用 EaseOutCubic，duration 秒内完成。
        /// </summary>
        public static IEnumerator SlideInFromLeft(RectTransform rt, float duration, float startX = -600f)
        {
            if (rt == null) yield break;

            float elapsed = 0f;
            float currentY = rt.anchoredPosition.y;
            Vector2 start = new Vector2(startX, currentY);
            Vector2 end = new Vector2(0f, currentY);
            rt.anchoredPosition = start;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutCubic(t);
                rt.anchoredPosition = Vector2.Lerp(start, end, eased);
                yield return null;
            }

            rt.anchoredPosition = end;
        }

        // =====================================================================
        //  Easing 函数（静态，内部使用）
        // =====================================================================

        public static float EaseOutBack(float t, float overshoot = 1.70158f)
        {
            t -= 1f;
            return t * t * ((overshoot + 1f) * t + overshoot) + 1f;
        }

        public static float EaseOutBounce(float t)
        {
            if (t < 1f / 2.75f)
                return 7.5625f * t * t;
            else if (t < 2f / 2.75f)
            {
                t -= 1.5f / 2.75f;
                return 7.5625f * t * t + 0.75f;
            }
            else if (t < 2.5f / 2.75f)
            {
                t -= 2.25f / 2.75f;
                return 7.5625f * t * t + 0.9375f;
            }
            else
            {
                t -= 2.625f / 2.75f;
                return 7.5625f * t * t + 0.984375f;
            }
        }

        public static float EaseInCubic(float t)
        {
            return t * t * t;
        }

        public static float EaseOutCubic(float t)
        {
            t -= 1f;
            return t * t * t + 1f;
        }

        public static float EaseOutQuart(float t)
        {
            t -= 1f;
            return -(t * t * t * t - 1f);
        }
    }
}
