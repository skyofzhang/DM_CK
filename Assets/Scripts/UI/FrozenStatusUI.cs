using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace DrscfZ.UI
{
    /// <summary>
    /// 思能冻结状态视觉系统 — 全体守护者冻结时的屏幕指示器
    ///
    /// 功能：
    ///   - 全体冻结时，屏幕底部出现蓝色冰晶横幅，显示倒计时
    ///   - 冻结结束时自动隐藏
    ///   - 提供静态入口 ShowFrozen(duration)
    ///
    /// 挂载规则（Rule #7）：
    ///   挂在 Canvas（始终激活）。
    ///   _panel 初始 inactive（Rule #2）。
    ///
    /// 场景预创建结构：
    ///   Canvas
    ///   └── FrozenStatusPanel（初始 inactive）
    ///       ├── BackgroundImage（蓝黑半透明全宽条）
    ///       ├── IceIcon（白色❄图标 Image）
    ///       ├── FrozenText（主文字：「❄ 全体守护者已冻结」）
    ///       └── CountdownText（倒计时：「解冻倒计时：30s」）
    ///
    /// 静态调用示例：
    ///   FrozenStatusUI.ShowFrozen(30f);
    /// </summary>
    public class FrozenStatusUI : MonoBehaviour
    {
        public static FrozenStatusUI Instance { get; private set; }

        // ── Inspector 字段 ────────────────────────────────────────────────────

        [Header("面板根节点（初始 inactive）")]
        [SerializeField] private GameObject _panel;

        [Header("主冻结文字")]
        [SerializeField] private TextMeshProUGUI _frozenText;

        [Header("倒计时文字")]
        [SerializeField] private TextMeshProUGUI _countdownText;

        [Header("背景图（控制Alpha渐变）")]
        [SerializeField] private Image _backgroundImage;

        // ── 内部状态 ──────────────────────────────────────────────────────────

        private Coroutine _countdownCoroutine;

        // ── 生命周期 ──────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            if (_panel != null) _panel.SetActive(false);
        }

        // ── 静态入口 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 显示冻结状态横幅，持续 duration 秒后自动隐藏。
        /// 由 SurvivalGameManager.HandleSpecialEffect("frozen_all") 调用。
        /// </summary>
        public static void ShowFrozen(float duration)
        {
            if (Instance != null)
                Instance.ShowInternal(duration);
        }

        // ── 内部实现 ──────────────────────────────────────────────────────────

        private void ShowInternal(float duration)
        {
            // 停止之前的倒计时（若有）
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }

            if (_panel == null)
            {
                Debug.LogWarning("[FrozenStatusUI] _panel 未绑定，跳过显示");
                return;
            }

            _panel.SetActive(true);

            // 设置主文字
            if (_frozenText != null)
                _frozenText.text = "❄  全体守护者已冻结";

            _countdownCoroutine = StartCoroutine(CountdownCoroutine(duration));
        }

        private IEnumerator CountdownCoroutine(float totalDuration)
        {
            float remaining = totalDuration;

            // 淡入（0.3s）
            yield return StartCoroutine(FadePanel(0f, 1f, 0.3f));

            while (remaining > 0f)
            {
                remaining -= Time.deltaTime;
                if (remaining < 0f) remaining = 0f;

                if (_countdownText != null)
                    _countdownText.text = $"解冻倒计时：{Mathf.CeilToInt(remaining)}s";

                yield return null;
            }

            // 冻结结束：文字变为"❄ 解冻完成！"，0.5s 后淡出
            if (_frozenText != null)
                _frozenText.text = "❄  解冻完成！";
            if (_countdownText != null)
                _countdownText.text = "";

            yield return new WaitForSeconds(0.5f);

            // 淡出（0.4s）
            yield return StartCoroutine(FadePanel(1f, 0f, 0.4f));

            if (_panel != null) _panel.SetActive(false);
            _countdownCoroutine = null;
        }

        private IEnumerator FadePanel(float startAlpha, float endAlpha, float duration)
        {
            if (_backgroundImage == null && _frozenText == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float alpha = Mathf.Lerp(startAlpha, endAlpha, t);

                SetAlpha(_backgroundImage, alpha);
                SetTextAlpha(_frozenText, alpha);
                SetTextAlpha(_countdownText, alpha);
                yield return null;
            }

            SetAlpha(_backgroundImage, endAlpha);
            SetTextAlpha(_frozenText, endAlpha);
            SetTextAlpha(_countdownText, endAlpha);
        }

        private static void SetAlpha(Graphic g, float alpha)
        {
            if (g == null) return;
            Color c = g.color;
            c.a = alpha;
            g.color = c;
        }

        private static void SetTextAlpha(TextMeshProUGUI tmp, float alpha)
        {
            if (tmp == null) return;
            Color c = tmp.color;
            c.a = alpha;
            tmp.color = c;
        }
    }
}
