using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DrscfZ.Core;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// 全屏公告UI - 比赛开始/结束等重要提示
    /// 居中大字 + 渐入渐出动画
    ///
    /// 🆕 P0-B9：订阅切到 SurvivalGameManager（永续模式），移除旧的"香橙/柚子"阵营文案。
    /// - `SurvivalState.Running` 进入 → 显示当天 / 当夜公告
    /// - `OnGameEnded` → "堡垒失守！重建中..."（若 newbieProtected 则"新手保护：堡垒日不变"）
    /// </summary>
    public class AnnouncementUI : MonoBehaviour
    {
        public static AnnouncementUI Instance { get; private set; }

        [Header("References")]
        public CanvasGroup canvasGroup;
        public TextMeshProUGUI mainText;
        public TextMeshProUGUI subText;

        [Header("Animation")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.5f;

        // 🆕 P0-B9：字体改用阿里巴巴普惠 SDF；找不到时 fallback 到 ChineseFont SDF
        private const string AlibabaFontPath = "Fonts/AlibabaPuHuiTi-3-85-Bold SDF";
        private const string ChineseFontPath = "Fonts/ChineseFont SDF";

        private Coroutine _currentAnnouncement;
        private bool _subscribed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            EnsureComponents();
            HideVisual();
        }

        private void OnEnable()
        {
            // 🆕 P0-B9：切到 SurvivalGameManager（永续模式）；原 GameManager 是旧角力游戏的遗留单例
            // v1.27 §14 难度系统废止：不再订阅 OnDifficultySet
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnStateChanged += HandleSurvivalStateChanged;
                sgm.OnGameEnded    += HandleSurvivalGameEnded;
                _subscribed = true;
            }
        }

        private void Start()
        {
            TrySubscribe();
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;
            sgm.OnStateChanged += HandleSurvivalStateChanged;
            sgm.OnGameEnded    += HandleSurvivalGameEnded;
            _subscribed = true;
        }

        private TMP_FontAsset LoadBestFont()
        {
            var f = Resources.Load<TMP_FontAsset>(AlibabaFontPath);
            if (f != null) return f;
            return Resources.Load<TMP_FontAsset>(ChineseFontPath);
        }

        /// <summary>如果引用丢失，运行时自动创建必要组件</summary>
        private void EnsureComponents()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (mainText == null)
            {
                var existing = transform.Find("MainText");
                if (existing != null) mainText = existing.GetComponent<TextMeshProUGUI>();
            }
            if (mainText == null)
            {
                var go = new GameObject("MainText", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                mainText = go.AddComponent<TextMeshProUGUI>();
                mainText.fontSize = 72;
                mainText.color = Color.yellow;
                mainText.alignment = TextAlignmentOptions.Center;
                mainText.fontStyle = FontStyles.Bold;
                mainText.enableWordWrapping = false;
                var rt = go.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0, 30);
                rt.sizeDelta = new Vector2(900, 120);
                // 描边
                mainText.outlineWidth = 0.4f;
                mainText.outlineColor = new Color32(0, 0, 0, 255);
                var font = LoadBestFont();
                if (font != null) mainText.font = font;
            }

            if (subText == null)
            {
                var existing = transform.Find("SubText");
                if (existing != null) subText = existing.GetComponent<TextMeshProUGUI>();
            }
            if (subText == null)
            {
                var go = new GameObject("SubText", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                subText = go.AddComponent<TextMeshProUGUI>();
                subText.fontSize = 36;
                subText.color = Color.white;
                subText.alignment = TextAlignmentOptions.Center;
                var rt = go.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0, -50);
                rt.sizeDelta = new Vector2(600, 60);
                var font = LoadBestFont();
                if (font != null) subText.font = font;
            }
        }

        private void OnDisable()
        {
            // v1.27 §14 难度系统废止：不再退订 OnDifficultySet
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnStateChanged  -= HandleSurvivalStateChanged;
                sgm.OnGameEnded     -= HandleSurvivalGameEnded;
            }
            _subscribed = false;
        }

        /// <summary>P0-B9：SurvivalGameManager 状态切换 → 公告（替代旧的 GameManager 订阅）</summary>
        private void HandleSurvivalStateChanged(SurvivalGameManager.SurvivalState state)
        {
            if (state == SurvivalGameManager.SurvivalState.Running)
            {
                StartCoroutine(DelayedStartAnnouncement());
            }
        }

        private IEnumerator DelayedStartAnnouncement()
        {
            yield return new WaitForSeconds(0.1f);
            // 确认仍然在 Running 状态
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null || sgm.State != SurvivalGameManager.SurvivalState.Running) yield break;
            ShowAnnouncement("守护开始", "坚持到黎明！", new Color(1f, 0.85f, 0.2f), 2.0f);
        }

        /// <summary>P0-B9：永续模式失败公告 —— 没有胜负阵营，只有堡垒日升降。</summary>
        private void HandleSurvivalGameEnded(SurvivalGameEndedData data)
        {
            if (data == null) { HideVisual(); return; }

            // v1.26 永续模式：无胜利分支，只有失败降级 / 主动 end_game 临时休息
            Color color;
            string main;
            string sub;

            if (data.reason == "manual")
            {
                // §28.8 end_game：主播主动休息，不降级
                main = "临时休息";
                sub  = "不触发堡垒日降级";
                color = new Color(0.25f, 0.55f, 1.0f);  // 蓝色
            }
            else if (data.newbieProtected)
            {
                // 新手保护期：堡垒日不变
                main = "堡垒失守！重建中...";
                sub  = $"新手保护：堡垒日不变（{data.fortressDayBefore}）";
                color = new Color(0.30f, 1.00f, 0.55f);  // 浅绿
            }
            else if (data.fortressDayAfter < data.fortressDayBefore)
            {
                // 降级
                main = "堡垒失守！重建中...";
                sub  = $"当前堡垒日 {data.fortressDayBefore} → {data.fortressDayAfter}";
                color = new Color(1.00f, 0.35f, 0.25f);  // 橙红（警示）
            }
            else
            {
                // 失败但未降级（理论不会出现，兜底）
                main = "堡垒失守！重建中...";
                sub  = $"原因: {LocalizeReason(data.reason)}";
                color = new Color(0.85f, 0.55f, 0.25f);
            }

            ShowAnnouncement(main, sub, color, 4.0f);
        }

        // v1.27 §14 难度系统废止：HandleDifficultySet 已删除（DifficultyLevel enum 不再存在）

        private static string LocalizeReason(string reason)
        {
            switch (reason)
            {
                case "food_depleted": return "食物耗尽";
                case "temp_freeze":   return "炉温冻结";
                case "gate_breached": return "城门被攻破";
                case "all_dead":      return "全员阵亡";
                case "manual":        return "主播结束";
                default:              return reason ?? "未知";
            }
        }

        // §17.16 Modal 互斥：AnnouncementUI 视作 B 类 modal（非阻塞公告，与同级 B 类排队）。
        // id 固定 "announcement"：同时只一个公告在屏，新公告覆盖旧公告（StopCoroutine + 重新入队）。
        private const string ModalId = "announcement";

        /// <summary>
        /// 显示公告（自动隐藏）
        /// </summary>
        public void ShowAnnouncement(string main, string sub, Color color, float duration)
        {
            if (_currentAnnouncement != null)
            {
                StopCoroutine(_currentAnnouncement);
                // 覆盖旧公告：先释放旧 Modal slot，避免孤儿占位。
                ModalRegistry.ReleaseB(ModalId);
            }
            ModalRegistry.RequestB(ModalId);
            _currentAnnouncement = StartCoroutine(AnnouncementRoutine(main, sub, color, duration));
        }

        private IEnumerator AnnouncementRoutine(string main, string sub, Color color, float duration)
        {
            // audit-r10 §29：公告出现播 UI toast SFX（轻微"嗒"声）
            DrscfZ.Systems.AudioManager.Instance?.PlaySFX(DrscfZ.Core.AudioConstants.SFX_UI_TOAST);

            // 确保字体正确加载（解决乱码问题）
            var preferred = LoadBestFont();
            if (preferred != null)
            {
                if (mainText != null && mainText.font != preferred)
                    mainText.font = preferred;
                if (subText != null && subText.font != preferred)
                    subText.font = preferred;
            }

            // 设置内容
            if (mainText != null)
            {
                mainText.text = main;
                mainText.color = color;
            }
            if (subText != null)
            {
                subText.text = sub;
                subText.color = Color.white;
            }

            // 淡入（恢复可见状态）
            if (canvasGroup)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }
            float t = 0;
            while (t < fadeInDuration)
            {
                t += Time.deltaTime;
                if (canvasGroup) canvasGroup.alpha = Mathf.Lerp(0, 1, t / fadeInDuration);
                // 缩放弹出效果
                float scale = Mathf.Lerp(1.5f, 1f, t / fadeInDuration);
                if (mainText) mainText.transform.localScale = Vector3.one * scale;
                yield return null;
            }
            if (canvasGroup) canvasGroup.alpha = 1;
            if (mainText) mainText.transform.localScale = Vector3.one;

            // 保持显示
            yield return new WaitForSeconds(duration);

            // 淡出
            t = 0;
            while (t < fadeOutDuration)
            {
                t += Time.deltaTime;
                if (canvasGroup) canvasGroup.alpha = Mathf.Lerp(1, 0, t / fadeOutDuration);
                yield return null;
            }

            HideVisual();
            _currentAnnouncement = null;
            ModalRegistry.ReleaseB(ModalId);
        }

        /// <summary>隐藏视觉，但保持 GameObject 激活（不影响事件订阅）</summary>
        private void HideVisual()
        {
            if (canvasGroup)
            {
                canvasGroup.alpha = 0;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }

        private void OnDestroy()
        {
            // 场景切换 / 对象销毁时兜底释放 Modal slot，防止残留占位。
            ModalRegistry.ReleaseB(ModalId);
        }
    }
}
