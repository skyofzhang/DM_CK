using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §36.10 WaitingPhase（🆕 v1.27+ audit-r3/P1）
    ///
    /// 订阅 SurvivalGameManager.OnWaitingPhaseStarted / OnWaitingPhaseEnded：
    ///   - OnWaitingPhaseStarted: 显示 30s 倒计时大横幅 "新赛季即将开始..." + 主题预览
    ///   - OnWaitingPhaseEnded:   立即隐藏（兜底）
    ///
    /// 使用 ModalRegistry A 类（id="waiting_phase"，priority=50）阻塞主画面。
    /// 其他 A 类 modal（如结算 priority=80）仍能抢占；若被抢占则静默关闭。
    ///
    /// 挂载规则（CLAUDE.md #7）：挂 GameUIPanel 子 GO（常驻激活），不在 Awake 中 SetActive(false)。
    /// 字体：Alibaba 优先 + ChineseFont SDF fallback。
    ///
    /// Inspector 必填：
    ///   _rootPanel     — 整个横幅根 GO（显隐切换用）
    ///   _titleLabel    — 大标题 "新赛季即将开始..."
    ///   _themeLabel    — 主题名称（中文，如"血月 Blood Moon"）
    ///   _countdownLabel— 倒计时数字
    /// </summary>
    public class WaitingPhaseUI : MonoBehaviour
    {
        [Header("Root GO（显隐切换）")]
        [SerializeField] private GameObject _rootPanel;

        [Header("大标题 TMP")]
        [SerializeField] private TMP_Text _titleLabel;

        [Header("主题名称 TMP")]
        [SerializeField] private TMP_Text _themeLabel;

        [Header("倒计时数字 TMP")]
        [SerializeField] private TMP_Text _countdownLabel;

        // 字体路径
        private const string AlibabaFontPath = "Fonts/AlibabaPuHuiTi-3-85-Bold SDF";
        private const string ChineseFontPath = "Fonts/ChineseFont SDF";

        // ModalRegistry A 类 id + 优先级
        private const string MODAL_A_ID        = "waiting_phase";
        private const int    MODAL_A_PRIORITY  = 50;  // 结算 80 > 50，仍可被抢占

        private bool      _subscribed;
        private Coroutine _countdownCo;
        private bool      _modalHeld;

        // ── 生命周期 ──────────────────────────────────────────────────────

        private void Start()
        {
            EnsureFonts();
            if (_rootPanel != null && _rootPanel.activeSelf) _rootPanel.SetActive(false);
            TrySubscribe();
        }

        private void OnEnable()  { TrySubscribe(); }
        private void OnDisable() { Unsubscribe(); }
        private void OnDestroy()
        {
            Unsubscribe();
            StopCountdown();
            ReleaseModalIfHeld();
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
            sgm.OnWaitingPhaseStarted += HandleWaitingPhaseStarted;
            sgm.OnWaitingPhaseEnded   += HandleWaitingPhaseEnded;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnWaitingPhaseStarted -= HandleWaitingPhaseStarted;
                sgm.OnWaitingPhaseEnded   -= HandleWaitingPhaseEnded;
            }
            _subscribed = false;
        }

        private void EnsureFonts()
        {
            var font = Resources.Load<TMP_FontAsset>(AlibabaFontPath);
            if (font == null) font = Resources.Load<TMP_FontAsset>(ChineseFontPath);
            if (font == null) return;
            if (_titleLabel     != null && _titleLabel.font     != font) _titleLabel.font     = font;
            if (_themeLabel     != null && _themeLabel.font     != font) _themeLabel.font     = font;
            if (_countdownLabel != null && _countdownLabel.font != font) _countdownLabel.font = font;
        }

        // ── 事件回调 ──────────────────────────────────────────────────────

        private void HandleWaitingPhaseStarted(WaitingPhaseStartedData data)
        {
            if (data == null) return;
            // 🔴 audit-r46 GAP-M-01：服务端两路径字段不同，必须双 fallback
            //   season_prepare_started 路径：发 durationSec=30（SeasonManager.js:201）
            //   waiting_phase_started 路径：发 countdownSec=waitSec（SurvivalGameEngine.js:4999，通常 15s）
            //   原 `data.durationSec > 0 ? data.durationSec : 30` 让 boss 波前永远 fallback 到 30s（与服务端实际不符）
            int durationSec = data.durationSec  > 0 ? data.durationSec
                            : data.countdownSec > 0 ? data.countdownSec
                            : 30;

            // r15 GAP-B-MAJOR-04 / GAP-D-MAJOR-02：消费 waveIdx + nightModifier 区分两种触发源
            //   waveIdx > 0 → audit-r6 §36.10 夜晚 Boss 波前窗口（显示倒计时 + nightModifier 氛围信息）
            //   waveIdx <= 0 → SeasonManager 赛季切换（显示新主题）
            if (data.waveIdx > 0)
            {
                if (_titleLabel != null) _titleLabel.text = "下一波倒计时";
                // 显示 nightModifier 氛围（r12 D03 服务端透传 / r15 GAP-D-MAJOR-03 字段对齐为 {id, name, description}）
                if (_themeLabel != null)
                {
                    if (data.nightModifier != null && !string.IsNullOrEmpty(data.nightModifier.id) && data.nightModifier.id != "normal")
                    {
                        string nmName = string.IsNullOrEmpty(data.nightModifier.name) ? data.nightModifier.id : data.nightModifier.name;
                        string nmDesc = data.nightModifier.description ?? "";
                        _themeLabel.text = string.IsNullOrEmpty(nmDesc) ? $"今晚：{nmName}" : $"今晚：{nmName}（{nmDesc}）";
                    }
                    else
                    {
                        _themeLabel.text = "请准备迎接下一波怪物";
                    }
                }
            }
            else
            {
                if (_titleLabel != null) _titleLabel.text = "新赛季即将开始...";
                if (_themeLabel != null) _themeLabel.text = $"赛季 {data.newSeasonId} 主题：{LocalizeTheme(data.newThemeId)}";
            }

            // 请求 A 类 modal（可能被更高优先级抢占；被抢占则 OnReplaced 清理）
            bool ok = ModalRegistry.Request(MODAL_A_ID, MODAL_A_PRIORITY, OnReplaced);
            if (!ok)
            {
                // 被更高优先级占用中，跳过本次显示
                Debug.Log("[WaitingPhaseUI] Modal A busy, skip");
                return;
            }
            _modalHeld = true;

            if (_rootPanel != null) _rootPanel.SetActive(true);

            StopCountdown();
            _countdownCo = StartCoroutine(CountdownCoroutine(durationSec));
        }

        private void HandleWaitingPhaseEnded(WaitingPhaseEndedData data)
        {
            // 兜底：关闭横幅（可能倒计时还没走完或被抢占后重新触发）
            Close();
        }

        private void OnReplaced()
        {
            // 被更高优先级 A 类 modal 抢占（如结算界面）→ 立即隐藏自己
            _modalHeld = false;
            if (_rootPanel != null) _rootPanel.SetActive(false);
            StopCountdown();
        }

        // ── 协程 / 工具 ──────────────────────────────────────────────────

        private IEnumerator CountdownCoroutine(int totalSec)
        {
            for (int remain = totalSec; remain >= 0; remain--)
            {
                if (_countdownLabel != null) _countdownLabel.text = $"{remain}s";
                yield return new WaitForSecondsRealtime(1f);
            }
            Close();
        }

        private void Close()
        {
            StopCountdown();
            if (_rootPanel != null) _rootPanel.SetActive(false);
            ReleaseModalIfHeld();
        }

        private void StopCountdown()
        {
            if (_countdownCo != null) { StopCoroutine(_countdownCo); _countdownCo = null; }
        }

        private void ReleaseModalIfHeld()
        {
            if (_modalHeld)
            {
                ModalRegistry.Release(MODAL_A_ID);
                _modalHeld = false;
            }
        }

        private static string LocalizeTheme(string themeId)
        {
            switch (themeId)
            {
                case "classic_frozen": return "经典冰原";
                case "blood_moon":     return "血月";
                case "snowstorm":      return "风雪";
                case "dawn":           return "黎明";
                case "frenzy":         return "狂潮";
                case "serene":         return "宁静";
                default:               return string.IsNullOrEmpty(themeId) ? "未知" : themeId;
            }
        }
    }
}
