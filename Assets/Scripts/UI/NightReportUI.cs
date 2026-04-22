using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Text;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §34.4 E5b 夜战报告 —— 夜→昼转换 2.5s 多行回顾
    ///
    /// 订阅 SurvivalGameManager.OnNightReport。收到 night_report 消息时（服务端在 night→day 切换时推送）
    /// 全屏展示战斗回顾 2.5s（淡入 0.4s → 停留 1.7s → 淡出 0.4s），随后隐藏。
    ///
    /// 文案格式（策划案 5382-5390）：
    ///   第 {day} 夜战斗报告
    ///   消灭怪物：{monstersKilled} 只
    ///   Boss：{bossDefeated ? "已击杀！" : "未出现 / 未击杀"}
    ///   夜间 MVP：{mvpPlayerName}（{mvpKills} 杀）
    ///   最佳援助：{topGifterName}（{topGiftName}）
    ///   矿工存活率：{survivalRate*100}%
    ///   最危险时刻：城门血量仅剩 {closestCallHpPct*100}%
    ///
    /// 时机说明：
    ///   结算面板只在游戏结束时出现，夜战报告在每夜转白天时出现——两者不同时机不会冲突；
    ///   若遇到 end_game 的极端边界，NightReport 协程在 Unsubscribe/Destroy 时会被取消。
    ///
    /// 挂载规则（CLAUDE.md #7）：挂 Canvas/NightReportPanel（常驻激活），Awake 只对 _panelRoot 子节点 SetActive(false)。
    ///
    /// Inspector 必填：
    ///   _panelRoot        — 面板根节点（子节点，初始 inactive）
    ///   _panelCanvasGroup — CanvasGroup 淡入淡出
    ///   _titleText        — 标题 TMP "第 N 夜战斗报告"
    ///   _bodyText         — 多行正文 TMP（alignment=Left）
    /// </summary>
    public class NightReportUI : MonoBehaviour
    {
        [Header("面板根节点")]
        [SerializeField] private RectTransform   _panelRoot;
        [SerializeField] private CanvasGroup     _panelCanvasGroup;

        [Header("文字")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _bodyText;

        private const float FADE_IN  = 0.4f;
        private const float HOLD     = 1.7f;
        private const float FADE_OUT = 0.4f;

        private Coroutine _runCoroutine;
        private bool      _subscribed = false;

        // ── 生命周期 ──────────────────────────────────────────────────────

        private void Start()
        {
            if (_panelRoot != null) _panelRoot.gameObject.SetActive(false);
            if (_panelCanvasGroup != null) _panelCanvasGroup.alpha = 0f;
            TrySubscribe();
        }

        private void OnEnable()  { TrySubscribe(); }
        private void OnDisable() { Unsubscribe(); }
        private void OnDestroy()
        {
            Unsubscribe();
            if (_runCoroutine != null) { StopCoroutine(_runCoroutine); _runCoroutine = null; }
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
            sgm.OnNightReport += HandleNightReport;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null) sgm.OnNightReport -= HandleNightReport;
            _subscribed = false;
        }

        // ── 事件回调 ──────────────────────────────────────────────────────

        private void HandleNightReport(NightReportData data)
        {
            if (data == null) return;
            if (_runCoroutine != null) StopCoroutine(_runCoroutine);
            _runCoroutine = StartCoroutine(RunReport(data));
        }

        private IEnumerator RunReport(NightReportData data)
        {
            if (_titleText != null)
                _titleText.text = $"第 {data.day} 夜战斗报告";

            if (_bodyText != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"消灭怪物：{data.monstersKilled} 只");
                sb.AppendLine(data.bossDefeated
                    ? "Boss：已击杀！"
                    : "Boss：未出现 / 未击杀");

                if (!string.IsNullOrEmpty(data.mvpPlayerName))
                    sb.AppendLine($"夜间 MVP：{data.mvpPlayerName}（{data.mvpKills} 杀）");
                else
                    sb.AppendLine("夜间 MVP：无");

                if (!string.IsNullOrEmpty(data.topGifterName))
                {
                    string giftPart = string.IsNullOrEmpty(data.topGiftName) ? "" : $"（{data.topGiftName}）";
                    sb.AppendLine($"最佳援助：{data.topGifterName}{giftPart}");
                }
                else
                {
                    sb.AppendLine("最佳援助：无");
                }

                int survivalPct    = Mathf.Clamp(Mathf.RoundToInt(data.survivalRate    * 100f), 0, 100);
                int closestCallPct = Mathf.Clamp(Mathf.RoundToInt(data.closestCallHpPct * 100f), 0, 100);
                sb.AppendLine($"矿工存活率：{survivalPct}%");
                sb.Append   ($"最危险时刻：城门血量仅剩 {closestCallPct}%");

                _bodyText.text = sb.ToString();
            }

            if (_panelRoot != null) _panelRoot.gameObject.SetActive(true);

            // Fade in
            float t = 0f;
            while (t < FADE_IN)
            {
                t += Time.unscaledDeltaTime;
                if (_panelCanvasGroup != null) _panelCanvasGroup.alpha = Mathf.Clamp01(t / FADE_IN);
                yield return null;
            }
            if (_panelCanvasGroup != null) _panelCanvasGroup.alpha = 1f;

            // Hold
            yield return new WaitForSecondsRealtime(HOLD);

            // Fade out
            t = 0f;
            while (t < FADE_OUT)
            {
                t += Time.unscaledDeltaTime;
                if (_panelCanvasGroup != null) _panelCanvasGroup.alpha = 1f - Mathf.Clamp01(t / FADE_OUT);
                yield return null;
            }
            if (_panelCanvasGroup != null) _panelCanvasGroup.alpha = 0f;
            if (_panelRoot != null) _panelRoot.gameObject.SetActive(false);
            _runCoroutine = null;
        }
    }
}
