using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §36.4 赛季 Boss Rush 横幅（MVP 极简版）。
    ///
    /// 触发：
    ///   - season_boss_rush_start → Show(data)：显示横幅 + 全服血量池初值 + 下赛季主题预告
    ///   - season_boss_rush_killed → OnKilled(data)：闪光 + "赛季 Boss 已倒下" → 3s 后 Hide
    ///
    /// 挂载：Canvas/GameUIPanel（常驻）；Prefab 绑定留给人工。
    /// 若 Inspector 字段未绑定，所有展示降级为 Debug.Log + AnnouncementUI 兜底，不崩溃。
    ///
    /// 设计原则（CLAUDE.md 规则 6）：
    ///   - 脚本挂在 always-active 父对象，本身不 SetActive(false)
    ///   - 通过 _panelRoot 子节点 SetActive 控制显隐
    ///   - Awake 不调用 SetActive(false) 父节点，避免阻断 OnEnable
    /// </summary>
    public class BossRushBanner : MonoBehaviour
    {
        public static BossRushBanner Instance { get; private set; }

        [Header("横幅根节点（子节点，初始 inactive）")]
        [SerializeField] private GameObject _panelRoot;

        [Header("文本字段")]
        [SerializeField] private TMP_Text _titleText;        // "赛季Boss来袭！"
        [SerializeField] private TMP_Text _bossHpText;       // "全服血量 15000"
        [SerializeField] private TMP_Text _nextThemeText;    // "下一赛季：血月"
        [SerializeField] private TMP_Text _killedText;       // "赛季 Boss 已倒下"（击杀反馈）

        [Header("全服血量进度条")]
        [SerializeField] private Image _bossHpBar;           // fillAmount 0-1

        [Header("击杀闪光图层（击杀反馈时 fadeIn → fadeOut）")]
        [SerializeField] private CanvasGroup _killedFlash;

        [Header("自动隐藏")]
        [Tooltip("Boss 被击杀 → 横幅保留时长后自动隐藏（秒）")]
        [SerializeField] private float _killedHoldSec = 3f;

        // 运行时状态缓存
        private int  _bossHpTotal;
        private int  _bossHpCurrent;
        private bool _killedPlayed;
        private Coroutine _killedHideCoroutine;
        private Coroutine _flashCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this) { return; }
            Instance = this;
            // ✅ 合法：对子节点 _panelRoot 执行 SetActive（不是脚本自己挂的 GameObject）
            if (_panelRoot != null) _panelRoot.SetActive(false);
            if (_killedText != null) _killedText.gameObject.SetActive(false);
            if (_killedFlash != null) _killedFlash.alpha = 0f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>SurvivalGameManager.HandleBossRushStarted 调用。</summary>
        public void Show(BossRushStartedData data)
        {
            if (data == null) return;

            _bossHpTotal   = data.bossHpTotal;
            _bossHpCurrent = data.bossHpTotal;
            _killedPlayed  = false;

            if (_panelRoot == null || _titleText == null)
            {
                // 未绑定 Prefab：降级到 AnnouncementUI
                string themeName = SurvivalGameManager.GetSeasonThemeName(data.nextThemeId);
                string subText = string.IsNullOrEmpty(themeName)
                    ? $"全服血量 {data.bossHpTotal}"
                    : $"全服血量 {data.bossHpTotal} · 下一赛季：{themeName}";
                UI.AnnouncementUI.Instance?.ShowAnnouncement(
                    "赛季Boss来袭！",
                    subText,
                    new Color(1f, 0.2f, 0.3f),
                    5f);
                Debug.Log($"[BossRushBanner] (未绑定 _panelRoot) 降级到 AnnouncementUI: hpTotal={data.bossHpTotal} nextTheme={data.nextThemeId}");
                return;
            }

            _panelRoot.SetActive(true);
            if (_killedText != null) _killedText.gameObject.SetActive(false);

            if (_titleText != null) _titleText.text = "赛季Boss来袭！";
            UpdateBossHpText();
            UpdateHpBar();

            if (_nextThemeText != null)
            {
                string themeName = SurvivalGameManager.GetSeasonThemeName(data.nextThemeId);
                _nextThemeText.text = string.IsNullOrEmpty(themeName)
                    ? ""
                    : $"下一赛季：{themeName}";
            }
        }

        /// <summary>SurvivalGameManager.HandleBossRushKilled 调用：播放击杀反馈 → _killedHoldSec 后隐藏。</summary>
        public void OnKilled(BossRushKilledData data)
        {
            if (_killedPlayed) return; // dedup（服务端保证不重复，但客户端再兜底一次）
            _killedPlayed = true;
            _bossHpCurrent = 0;
            UpdateBossHpText();
            UpdateHpBar();

            if (_killedText != null)
            {
                _killedText.gameObject.SetActive(true);
                _killedText.text = "赛季 Boss 已倒下";
            }
            if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
            if (gameObject.activeInHierarchy && _killedFlash != null)
                _flashCoroutine = StartCoroutine(PlayKilledFlash());

            // 降级兜底：未绑定横幅也播一次 AnnouncementUI
            if (_panelRoot == null)
            {
                UI.AnnouncementUI.Instance?.ShowAnnouncement(
                    "赛季Boss已倒下",
                    "全服合力击败！",
                    new Color(1f, 0.85f, 0.1f),
                    3f);
            }

            if (_killedHideCoroutine != null) StopCoroutine(_killedHideCoroutine);
            if (_killedHoldSec > 0f && gameObject.activeInHierarchy)
                _killedHideCoroutine = StartCoroutine(HideAfter(_killedHoldSec));
        }

        /// <summary>外部：直接更新当前 Boss 池剩余血量（可选；MVP 未在 SGM 中主动调用，保留给未来 combat_attack 聚合用）。</summary>
        public void SetBossHpCurrent(int hp)
        {
            _bossHpCurrent = Mathf.Max(0, hp);
            UpdateBossHpText();
            UpdateHpBar();
        }

        public void Hide()
        {
            if (_killedHideCoroutine != null) { StopCoroutine(_killedHideCoroutine); _killedHideCoroutine = null; }
            if (_flashCoroutine != null) { StopCoroutine(_flashCoroutine); _flashCoroutine = null; }
            if (_killedFlash != null) _killedFlash.alpha = 0f;
            if (_panelRoot != null) _panelRoot.SetActive(false);
        }

        private void UpdateBossHpText()
        {
            if (_bossHpText != null)
                _bossHpText.text = $"全服血量 {_bossHpCurrent}/{_bossHpTotal}";
        }

        private void UpdateHpBar()
        {
            if (_bossHpBar == null) return;
            float ratio = _bossHpTotal > 0 ? (float)_bossHpCurrent / _bossHpTotal : 0f;
            _bossHpBar.fillAmount = Mathf.Clamp01(ratio);
        }

        private IEnumerator PlayKilledFlash()
        {
            if (_killedFlash == null) yield break;
            // fade in 0.2s
            float t = 0f;
            while (t < 0.2f)
            {
                t += Time.deltaTime;
                _killedFlash.alpha = Mathf.Clamp01(t / 0.2f);
                yield return null;
            }
            _killedFlash.alpha = 1f;
            // hold 0.4s
            yield return new WaitForSeconds(0.4f);
            // fade out 0.6s
            t = 0f;
            while (t < 0.6f)
            {
                t += Time.deltaTime;
                _killedFlash.alpha = 1f - Mathf.Clamp01(t / 0.6f);
                yield return null;
            }
            _killedFlash.alpha = 0f;
            _flashCoroutine = null;
        }

        private IEnumerator HideAfter(float sec)
        {
            yield return new WaitForSeconds(sec);
            Hide();
        }
    }
}
