using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using DrscfZ.UI;
using DrscfZ.Survival;

namespace DrscfZ.Editor
{
    /// <summary>
    /// Tools → DrscfZ → Setup §34D Narrative UI
    ///
    /// 一键搭建 §34.4 情感引擎 Group D（叙事引擎）的 UI 骨架：
    ///
    ///   Canvas/ChapterAnnouncement                         (E2 全屏幕名公告 + ChapterAnnouncementUI)
    ///   Canvas/NightModifierBanner                         (E6 全屏修饰符名公告 + NightModifierUI)
    ///   Canvas/NightReportPanel                            (E5b 全屏多行夜战报告 + NightReportUI)
    ///   Canvas/GameUIPanel/EngagementReminder              (E8 短暂浮层 + EngagementReminderUI)
    ///   Canvas/BroadcasterPanel/StreamerPromptCard         (E5a 右上角话术卡 + StreamerPromptUI)
    ///   Canvas/BroadcasterPanel/DifficultyChangeButton     (E9 恢复期首日切难度按钮 + DifficultyChangeButtonUI)
    ///   SurvivalLightingController                         (E6 光照控制器 + SurvivalLightingController)
    ///
    /// 兜底策略（对齐 docs/multi_agent_workflow.md 决策 6 "MCP 优先 + Console 监控"）：
    ///   缺 GameObject → 占位建出（不跳过）
    ///   禁用 EditorUtility.DisplayDialog（CLAUDE.md 踩坑）
    ///   SerializedObject 写 m_fontColor + m_fontColor32（CLAUDE.md TMP 颜色踩坑）
    ///   AddComponent&lt;RectTransform&gt; 显式写，GameObject 构造后立即加（CLAUDE.md UI 布局踩坑）
    ///   不自动 SaveScene，由 PM 通过 UnityMCP manage_scene action='save' 或 SaveCurrentScene.Execute() 保存
    ///
    /// 视觉与 §24.5 BroadcasterDecisionHUD 兼容：
    ///   §24.5 DecisionHUD 挂 BroadcasterPanel 左上角 (anchor 0,1)；本脚本的 StreamerPromptCard
    ///   挂 BroadcasterPanel 右上角 (anchor 1,1) 避开，避免互相遮挡。
    /// </summary>
    public static class SetupSection34DNarrativeUI
    {
        private const string ChineseFontPath = "Assets/Resources/Fonts/ChineseFont SDF.asset";

        [MenuItem("Tools/DrscfZ/Setup §34D Narrative UI")]
        public static void Run()
        {
            // ---- 1. 找 Canvas ----
            var canvas = FindInLoadedScene("Canvas");
            if (canvas == null)
            {
                Debug.LogError("[Setup34D] Canvas 未找到，终止。");
                return;
            }

            // ---- 2. 找/建 GameUIPanel（E8 父节点） ----
            var gameUIPanel = GetOrCreateFullscreenPanel(canvas.transform, "GameUIPanel");

            // ---- 3. 找 BroadcasterPanel（E5a + E9 父节点） ----
            GameObject broadcasterPanel = null;
            var existingBp = UnityEngine.Object.FindObjectOfType<DrscfZ.UI.BroadcasterPanel>(true);
            if (existingBp != null)
            {
                broadcasterPanel = existingBp.gameObject;
                Debug.Log($"[Setup34D] 已按脚本定位 BroadcasterPanel GO：{broadcasterPanel.name}");
            }
            if (broadcasterPanel == null)
                broadcasterPanel = FindInLoadedScene("BroadcasterPanel");
            if (broadcasterPanel == null)
            {
                Debug.LogWarning("[Setup34D] BroadcasterPanel 未找到，占位建出父节点。");
                broadcasterPanel = new GameObject("BroadcasterPanel");
                var rt = broadcasterPanel.AddComponent<RectTransform>();
                broadcasterPanel.transform.SetParent(canvas.transform, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            // ---- 4. 找中文字体（失败也继续） ----
            var chineseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ChineseFontPath);
            if (chineseFont == null)
                Debug.LogWarning($"[Setup34D] 未能加载中文字体: {ChineseFontPath}（TMP 可能显示方块）");

            // ---- 5. 构建 7 个模块 ----
            BuildChapterAnnouncement (canvas.transform,       chineseFont);
            BuildNightModifierBanner (canvas.transform,       chineseFont);
            BuildNightReportPanel    (canvas.transform,       chineseFont);
            BuildEngagementReminder  (gameUIPanel.transform,  chineseFont);
            BuildStreamerPromptCard  (broadcasterPanel.transform, chineseFont);
            BuildDifficultyChangeButton(broadcasterPanel.transform, chineseFont);
            BuildLightingController();

            // ---- 6. 标脏场景 ----
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[Setup34D] 完成！建议运行 MCP manage_scene action='save' 或 SaveCurrentScene.Execute() 保存场景。");
        }

        // ==================== E2 ChapterAnnouncement ====================

        private static void BuildChapterAnnouncement(Transform canvasRoot, TMP_FontAsset font)
        {
            // 宿主 GO：常驻激活；BannerRoot 为子节点（初始 inactive）
            var host = GetOrCreateChild(canvasRoot, "ChapterAnnouncement", () =>
            {
                var g  = new GameObject("ChapterAnnouncement");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(canvasRoot, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return g;
            });

            // BannerRoot：屏幕正中上半部
            var bannerRoot = GetOrCreateChild(host.transform, "BannerRoot", () =>
            {
                var g  = new GameObject("BannerRoot");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(host.transform, false);
                rt.anchorMin = new Vector2(0.5f, 0.55f);
                rt.anchorMax = new Vector2(0.5f, 0.75f);
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(1200f, 240f);
                rt.anchoredPosition = Vector2.zero;
                return g;
            });

            var cg = bannerRoot.GetComponent<CanvasGroup>();
            if (cg == null) cg = bannerRoot.AddComponent<CanvasGroup>();
            cg.alpha          = 0f;
            cg.blocksRaycasts = false;
            cg.interactable   = false;

            // 大字幕名
            var nameGO = CreateTMP(bannerRoot.transform, "NameText",
                new Vector2(0f, 0.38f), new Vector2(1f, 1f),
                new Vector2(8f, 0f),    new Vector2(-8f, 0f),
                96f, TextAlignmentOptions.Center, font);
            var nameTMP = nameGO.GetComponent<TextMeshProUGUI>();
            nameTMP.fontStyle = FontStyles.Bold;
            nameTMP.text      = "";
            SetTMPColor(nameTMP, new Color(1f, 0.90f, 0.50f));

            // 副标题（日范围）
            var subGO = CreateTMP(bannerRoot.transform, "SubText",
                new Vector2(0f, 0f), new Vector2(1f, 0.36f),
                new Vector2(8f, 0f), new Vector2(-8f, 0f),
                28f, TextAlignmentOptions.Center, font);
            var subTMP = subGO.GetComponent<TextMeshProUGUI>();
            subTMP.text = "";
            SetTMPColor(subTMP, Color.white);

            bannerRoot.SetActive(false);

            // 挂脚本 + 绑字段（挂在 host 上常驻）
            var ui = host.GetComponent<ChapterAnnouncementUI>();
            if (ui == null) ui = host.AddComponent<ChapterAnnouncementUI>();

            var so = new SerializedObject(ui);
            TryBind(so, "_bannerRoot",        bannerRoot.GetComponent<RectTransform>());
            TryBind(so, "_bannerCanvasGroup", cg);
            TryBind(so, "_nameText",          nameTMP);
            TryBind(so, "_subText",           subTMP);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);

            Debug.Log("[Setup34D] E2 ChapterAnnouncement 已挂 ChapterAnnouncementUI + 字段绑定完成");
        }

        // ==================== E6 NightModifierBanner ====================

        private static void BuildNightModifierBanner(Transform canvasRoot, TMP_FontAsset font)
        {
            var host = GetOrCreateChild(canvasRoot, "NightModifierBanner", () =>
            {
                var g  = new GameObject("NightModifierBanner");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(canvasRoot, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return g;
            });

            var bannerRoot = GetOrCreateChild(host.transform, "BannerRoot", () =>
            {
                var g  = new GameObject("BannerRoot");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(host.transform, false);
                // 屏幕正中（避开 ChapterAnnouncement 的上半部分）
                rt.anchorMin = new Vector2(0.5f, 0.40f);
                rt.anchorMax = new Vector2(0.5f, 0.60f);
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(1200f, 240f);
                rt.anchoredPosition = Vector2.zero;
                return g;
            });

            var cg = bannerRoot.GetComponent<CanvasGroup>();
            if (cg == null) cg = bannerRoot.AddComponent<CanvasGroup>();
            cg.alpha          = 0f;
            cg.blocksRaycasts = false;
            cg.interactable   = false;

            // 修饰符名
            var nameGO = CreateTMP(bannerRoot.transform, "NameText",
                new Vector2(0f, 0.45f), new Vector2(1f, 1f),
                new Vector2(8f, 0f),    new Vector2(-8f, 0f),
                88f, TextAlignmentOptions.Center, font);
            var nameTMP = nameGO.GetComponent<TextMeshProUGUI>();
            nameTMP.fontStyle = FontStyles.Bold;
            nameTMP.text      = "";
            SetTMPColor(nameTMP, new Color(1f, 0.35f, 0.35f));

            // 描述
            var descGO = CreateTMP(bannerRoot.transform, "DescText",
                new Vector2(0f, 0f), new Vector2(1f, 0.43f),
                new Vector2(8f, 0f), new Vector2(-8f, 0f),
                30f, TextAlignmentOptions.Center, font);
            var descTMP = descGO.GetComponent<TextMeshProUGUI>();
            descTMP.text = "";
            SetTMPColor(descTMP, Color.white);

            bannerRoot.SetActive(false);

            var ui = host.GetComponent<NightModifierUI>();
            if (ui == null) ui = host.AddComponent<NightModifierUI>();

            var so = new SerializedObject(ui);
            TryBind(so, "_bannerRoot",        bannerRoot.GetComponent<RectTransform>());
            TryBind(so, "_bannerCanvasGroup", cg);
            TryBind(so, "_nameText",          nameTMP);
            TryBind(so, "_descText",          descTMP);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);

            Debug.Log("[Setup34D] E6 NightModifierBanner 已挂 NightModifierUI + 字段绑定完成");
        }

        // ==================== E5b NightReportPanel ====================

        private static void BuildNightReportPanel(Transform canvasRoot, TMP_FontAsset font)
        {
            var host = GetOrCreateChild(canvasRoot, "NightReportPanel", () =>
            {
                var g  = new GameObject("NightReportPanel");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(canvasRoot, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return g;
            });

            // 报告面板：屏幕中央 800×620
            var panelRoot = GetOrCreateChild(host.transform, "PanelRoot", () =>
            {
                var g  = new GameObject("PanelRoot");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(host.transform, false);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(800f, 620f);
                rt.anchoredPosition = Vector2.zero;
                return g;
            });

            var bg = panelRoot.GetComponent<Image>();
            if (bg == null) bg = panelRoot.AddComponent<Image>();
            bg.color         = new Color(0f, 0f, 0f, 0.85f);
            bg.raycastTarget = false;

            var cg = panelRoot.GetComponent<CanvasGroup>();
            if (cg == null) cg = panelRoot.AddComponent<CanvasGroup>();
            cg.alpha          = 0f;
            cg.blocksRaycasts = false;
            cg.interactable   = false;

            var titleGO = CreateTMP(panelRoot.transform, "TitleText",
                new Vector2(0f, 0.78f), new Vector2(1f, 1f),
                new Vector2(12f, 0f),   new Vector2(-12f, 0f),
                44f, TextAlignmentOptions.Center, font);
            var titleTMP = titleGO.GetComponent<TextMeshProUGUI>();
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.text      = "";
            SetTMPColor(titleTMP, new Color(1f, 0.92f, 0.55f));

            var bodyGO = CreateTMP(panelRoot.transform, "BodyText",
                new Vector2(0f, 0.05f), new Vector2(1f, 0.76f),
                new Vector2(48f, 0f),   new Vector2(-48f, 0f),
                28f, TextAlignmentOptions.Left, font);
            var bodyTMP = bodyGO.GetComponent<TextMeshProUGUI>();
            bodyTMP.text = "";
            SetTMPColor(bodyTMP, Color.white);

            panelRoot.SetActive(false);

            var ui = host.GetComponent<NightReportUI>();
            if (ui == null) ui = host.AddComponent<NightReportUI>();

            var so = new SerializedObject(ui);
            TryBind(so, "_panelRoot",        panelRoot.GetComponent<RectTransform>());
            TryBind(so, "_panelCanvasGroup", cg);
            TryBind(so, "_titleText",        titleTMP);
            TryBind(so, "_bodyText",         bodyTMP);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);

            Debug.Log("[Setup34D] E5b NightReportPanel 已挂 NightReportUI + 字段绑定完成");
        }

        // ==================== E8 EngagementReminder ====================

        private static void BuildEngagementReminder(Transform gameUIPanel, TMP_FontAsset font)
        {
            var host = GetOrCreateChild(gameUIPanel, "EngagementReminder", () =>
            {
                var g  = new GameObject("EngagementReminder");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(gameUIPanel, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return g;
            });

            // 浮层面板：屏幕底部中央（避开 GiftIconBar 的底部 160px 区域）
            var panelRoot = GetOrCreateChild(host.transform, "PanelRoot", () =>
            {
                var g  = new GameObject("PanelRoot");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(host.transform, false);
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot     = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(720f, 80f);
                rt.anchoredPosition = new Vector2(0f, 200f);
                return g;
            });

            var bg = panelRoot.GetComponent<Image>();
            if (bg == null) bg = panelRoot.AddComponent<Image>();
            bg.color         = new Color(0.15f, 0.25f, 0.45f, 0.92f);
            bg.raycastTarget = false;

            var cg = panelRoot.GetComponent<CanvasGroup>();
            if (cg == null) cg = panelRoot.AddComponent<CanvasGroup>();
            cg.alpha          = 0f;
            cg.blocksRaycasts = false;
            cg.interactable   = false;

            var msgGO = CreateTMP(panelRoot.transform, "MessageText",
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(16f, 0f), new Vector2(-16f, 0f),
                26f, TextAlignmentOptions.Center, font);
            var msgTMP = msgGO.GetComponent<TextMeshProUGUI>();
            msgTMP.text = "";
            SetTMPColor(msgTMP, Color.white);

            panelRoot.SetActive(false);

            var ui = host.GetComponent<EngagementReminderUI>();
            if (ui == null) ui = host.AddComponent<EngagementReminderUI>();

            var so = new SerializedObject(ui);
            TryBind(so, "_panelRoot",        panelRoot.GetComponent<RectTransform>());
            TryBind(so, "_panelCanvasGroup", cg);
            TryBind(so, "_messageText",      msgTMP);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);

            Debug.Log("[Setup34D] E8 EngagementReminder 已挂 EngagementReminderUI + 字段绑定完成");
        }

        // ==================== E5a StreamerPromptCard ====================

        private static void BuildStreamerPromptCard(Transform broadcasterPanel, TMP_FontAsset font)
        {
            var host = GetOrCreateChild(broadcasterPanel, "StreamerPromptCard", () =>
            {
                var g  = new GameObject("StreamerPromptCard");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(broadcasterPanel, false);
                // 右上角：避开 §24.5 BroadcasterDecisionHUD 左上角
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot     = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-10f, -10f);
                rt.sizeDelta = new Vector2(420f, 100f);
                return g;
            });

            var cardRoot = GetOrCreateChild(host.transform, "CardRoot", () =>
            {
                var g  = new GameObject("CardRoot");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(host.transform, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return g;
            });

            var cardBg = cardRoot.GetComponent<Image>();
            if (cardBg == null) cardBg = cardRoot.AddComponent<Image>();
            cardBg.color         = new Color(0.30f, 0.30f, 0.30f, 0.75f);  // 默认 info 色
            cardBg.raycastTarget = false;

            var cg = cardRoot.GetComponent<CanvasGroup>();
            if (cg == null) cg = cardRoot.AddComponent<CanvasGroup>();
            cg.alpha          = 0f;
            cg.blocksRaycasts = false;
            cg.interactable   = false;

            var promptGO = CreateTMP(cardRoot.transform, "PromptText",
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(12f, 6f), new Vector2(-12f, -6f),
                22f, TextAlignmentOptions.MidlineLeft, font);
            var promptTMP = promptGO.GetComponent<TextMeshProUGUI>();
            promptTMP.text = "";
            SetTMPColor(promptTMP, Color.white);

            cardRoot.SetActive(false);

            var ui = host.GetComponent<StreamerPromptUI>();
            if (ui == null) ui = host.AddComponent<StreamerPromptUI>();

            var so = new SerializedObject(ui);
            TryBind(so, "_cardRoot",        cardRoot.GetComponent<RectTransform>());
            TryBind(so, "_cardCanvasGroup", cg);
            TryBind(so, "_cardBg",          cardBg);
            TryBind(so, "_promptText",      promptTMP);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);

            Debug.Log("[Setup34D] E5a StreamerPromptCard 已挂 StreamerPromptUI + 字段绑定完成");
        }

        // ==================== E9 DifficultyChangeButton ====================

        private static void BuildDifficultyChangeButton(Transform broadcasterPanel, TMP_FontAsset font)
        {
            var host = GetOrCreateChild(broadcasterPanel, "DifficultyChangeButton", () =>
            {
                var g  = new GameObject("DifficultyChangeButton");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(broadcasterPanel, false);
                // 左下角（避开 §24.5 左上 DecisionHUD + §34D 右上 StreamerPromptCard）
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot     = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(10f, 160f);
                rt.sizeDelta = new Vector2(240f, 320f);  // 包括主按钮 + 3 选项
                return g;
            });

            // ButtonRoot 主按钮根（subnode，恢复期首日 SetActive(true)）
            var buttonRoot = GetOrCreateChild(host.transform, "ButtonRoot", () =>
            {
                var g  = new GameObject("ButtonRoot");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(host.transform, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return g;
            });

            // 主按钮（顶部）
            var mainBtnGO = GetOrCreateChild(buttonRoot.transform, "MainButton", () =>
            {
                var g  = new GameObject("MainButton");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(buttonRoot.transform, false);
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot     = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, 0f);
                rt.sizeDelta = new Vector2(0f, 60f);
                return g;
            });
            var mainBtnImg = mainBtnGO.GetComponent<Image>();
            if (mainBtnImg == null) mainBtnImg = mainBtnGO.AddComponent<Image>();
            mainBtnImg.color         = new Color(0.35f, 0.55f, 0.85f, 0.95f);
            mainBtnImg.raycastTarget = true;

            var mainBtn = mainBtnGO.GetComponent<Button>();
            if (mainBtn == null) mainBtn = mainBtnGO.AddComponent<Button>();

            var mainLabelGO = CreateTMP(mainBtnGO.transform, "Label",
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(8f, 4f), new Vector2(-8f, -4f),
                22f, TextAlignmentOptions.Center, font);
            var mainLabelTMP = mainLabelGO.GetComponent<TextMeshProUGUI>();
            mainLabelTMP.text = "切换难度";
            mainLabelTMP.fontStyle = FontStyles.Bold;
            SetTMPColor(mainLabelTMP, Color.white);

            // ChoicePanel（3 个难度选项，默认隐藏）
            var choicePanel = GetOrCreateChild(buttonRoot.transform, "ChoicePanel", () =>
            {
                var g  = new GameObject("ChoicePanel");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(buttonRoot.transform, false);
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot     = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -68f);   // 主按钮下方
                rt.sizeDelta = new Vector2(0f, 228f);          // 3 个按钮 × 68 + spacing
                return g;
            });
            var choiceBg = choicePanel.GetComponent<Image>();
            if (choiceBg == null) choiceBg = choicePanel.AddComponent<Image>();
            choiceBg.color         = new Color(0f, 0f, 0f, 0.85f);
            choiceBg.raycastTarget = true;

            var easyBtn   = BuildChoiceButton(choicePanel.transform, "EasyButton",   "轻松",     0,   font);
            var normalBtn = BuildChoiceButton(choicePanel.transform, "NormalButton", "困难",     76,  font);
            var hardBtn   = BuildChoiceButton(choicePanel.transform, "HardButton",   "恐怖",     152, font);

            // 默认隐藏
            buttonRoot.SetActive(false);
            choicePanel.SetActive(false);

            var ui = host.GetComponent<DifficultyChangeButtonUI>();
            if (ui == null) ui = host.AddComponent<DifficultyChangeButtonUI>();

            var so = new SerializedObject(ui);
            TryBind(so, "_buttonRoot",      buttonRoot);
            TryBind(so, "_mainButton",      mainBtn);
            TryBind(so, "_mainButtonLabel", mainLabelTMP);
            TryBind(so, "_choicePanel",     choicePanel);
            TryBind(so, "_easyBtn",         easyBtn);
            TryBind(so, "_normalBtn",       normalBtn);
            TryBind(so, "_hardBtn",         hardBtn);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);

            Debug.Log("[Setup34D] E9 DifficultyChangeButton 已挂 DifficultyChangeButtonUI + 字段绑定完成");
        }

        private static Button BuildChoiceButton(Transform parent, string name, string label, float yOffsetFromTop, TMP_FontAsset font)
        {
            var go = GetOrCreateChild(parent, name, () =>
            {
                var g  = new GameObject(name);
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(parent, false);
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot     = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -8f - yOffsetFromTop);
                rt.sizeDelta = new Vector2(-16f, 60f);
                return g;
            });

            var img = go.GetComponent<Image>();
            if (img == null) img = go.AddComponent<Image>();
            img.color         = new Color(0.25f, 0.45f, 0.75f, 0.95f);
            img.raycastTarget = true;

            var btn = go.GetComponent<Button>();
            if (btn == null) btn = go.AddComponent<Button>();

            var labelGO = CreateTMP(go.transform, "Label",
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(8f, 4f), new Vector2(-8f, -4f),
                24f, TextAlignmentOptions.Center, font);
            var labelTMP = labelGO.GetComponent<TextMeshProUGUI>();
            labelTMP.text = label;
            labelTMP.fontStyle = FontStyles.Bold;
            SetTMPColor(labelTMP, Color.white);

            return btn;
        }

        // ==================== E6 SurvivalLightingController ====================

        private static void BuildLightingController()
        {
            // 独立空 GO（放 Canvas 同级，不是 Canvas 子节点，避免 RectTransform 噪声）
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            GameObject ctrlGO = null;

            // 场景根搜索（不嵌入 Canvas）
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.name == "SurvivalLightingController" && go.scene.isLoaded && !IsPrefabAsset(go))
                {
                    ctrlGO = go;
                    break;
                }
            }

            if (ctrlGO == null)
            {
                ctrlGO = new GameObject("SurvivalLightingController");
                UnityEditor.SceneManagement.EditorSceneManager.MoveGameObjectToScene(ctrlGO, scene);
                Debug.Log("[Setup34D] E6 SurvivalLightingController 新建场景根 GO");
            }

            var ui = ctrlGO.GetComponent<SurvivalLightingController>();
            if (ui == null) ui = ctrlGO.AddComponent<SurvivalLightingController>();

            EditorUtility.SetDirty(ui);
            Debug.Log("[Setup34D] E6 SurvivalLightingController 已挂脚本");
        }

        // ==================== 辅助方法 ====================

        /// <summary>创建一个覆盖全屏的面板（若存在则直接返回）。</summary>
        private static GameObject GetOrCreateFullscreenPanel(Transform parent, string name)
        {
            return GetOrCreateChild(parent, name, () =>
            {
                var g  = new GameObject(name);
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(parent, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                Debug.LogWarning($"[Setup34D] {name} 占位建出（原场景缺失）。");
                return g;
            });
        }

        /// <summary>创建一个带 TextMeshProUGUI 的子节点，返回 GameObject。</summary>
        private static GameObject CreateTMP(Transform parent, string name,
                                            Vector2 anchorMin, Vector2 anchorMax,
                                            Vector2 offsetMin, Vector2 offsetMax,
                                            float fontSize, TextAlignmentOptions align,
                                            TMP_FontAsset font)
        {
            var go = GetOrCreateChild(parent, name, () =>
            {
                var g  = new GameObject(name);
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(parent, false);
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.offsetMin = offsetMin;
                rt.offsetMax = offsetMax;
                return g;
            });

            var rt2 = go.GetComponent<RectTransform>();
            rt2.anchorMin = anchorMin;
            rt2.anchorMax = anchorMax;
            rt2.offsetMin = offsetMin;
            rt2.offsetMax = offsetMax;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize         = fontSize;
            tmp.alignment        = align;
            tmp.enableAutoSizing = false;
            tmp.raycastTarget    = false;
            if (font != null) tmp.font = font;
            SetTMPColor(tmp, Color.white);
            return go;
        }

        /// <summary>将 TMP 的颜色同时写入 m_fontColor + m_fontColor32（绕开 faceColor 默认白问题）。</summary>
        private static void SetTMPColor(TextMeshProUGUI tmp, Color c)
        {
            if (tmp == null) return;
            var so = new SerializedObject(tmp);
            var p1 = so.FindProperty("m_fontColor");
            var p2 = so.FindProperty("m_fontColor32");
            if (p1 != null) p1.colorValue = c;
            if (p2 != null) p2.colorValue = c;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tmp);
        }

        private static void TryBind(SerializedObject so, string fieldName, UnityEngine.Object value)
        {
            if (so == null || string.IsNullOrEmpty(fieldName)) return;
            var p = so.FindProperty(fieldName);
            if (p == null)
            {
                Debug.LogWarning($"[Setup34D] TryBind: 字段 {fieldName} 未找到（可能脚本未定义该 SerializeField）");
                return;
            }
            p.objectReferenceValue = value;
        }

        private static GameObject GetOrCreateChild(Transform parent, string name, System.Func<GameObject> creator)
        {
            var found = parent.Find(name);
            if (found != null) return found.gameObject;
            return creator();
        }

        /// <summary>Resources.FindObjectsOfTypeAll 遍历，返回 scene.isLoaded 的 GameObject（包含非激活）。</summary>
        private static GameObject FindInLoadedScene(string name)
        {
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.name == name && go.scene.isLoaded && !IsPrefabAsset(go))
                    return go;
            }
            return null;
        }

        private static bool IsPrefabAsset(GameObject go)
        {
            return go != null && (go.hideFlags & HideFlags.HideAndDontSave) != 0
                   && !go.scene.IsValid();
        }
    }
}
