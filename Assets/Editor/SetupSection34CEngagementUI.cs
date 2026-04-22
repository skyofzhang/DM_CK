using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using DrscfZ.UI;

namespace DrscfZ.Editor
{
    /// <summary>
    /// Tools → DrscfZ → Setup §34C Engagement UI
    ///
    /// 一键搭建 §34.4 情感引擎 Group C（E1 危机感知 / E3a-c 社交竞争 / E4 精准付费触发）的 UI 骨架：
    ///
    ///   Canvas/TensionOverlay                            (E1 全屏 Image + TensionOverlayUI)
    ///   Canvas/GameUIPanel/CoopMilestoneBar              (E3b 进度条 + CoopMilestoneUI)
    ///   Canvas/GameUIPanel/CoopMilestoneBar/AnnounceRoot (E3b 达标全屏公告，属 CoopMilestoneUI 的子节点)
    ///   Canvas/GloryMomentBanner                         (E3a 顶部横幅 + GloryMomentUI + GoldBurst + RedFlash)
    ///   Canvas/GiftImpactBanner                          (E3c 顶部横幅 + GiftImpactUI)
    ///   Canvas/GameUIPanel/GiftIconBar/GiftRecommendBubble (E4 气泡 + GiftRecommendationUI)
    ///
    /// 兜底策略（对齐 docs/multi_agent_workflow.md 决策 6 "MCP 优先 + Console 监控"）：
    ///   缺 GameObject → 占位建出（不跳过）
    ///   禁用 EditorUtility.DisplayDialog（CLAUDE.md 踩坑）
    ///   SerializedObject 写 m_fontColor + m_fontColor32（CLAUDE.md TMP 颜色踩坑）
    ///   AddComponent<RectTransform> 显式写，GameObject 构造后立即加（CLAUDE.md UI 布局踩坑）
    ///   不自动 SaveScene，由 PM 通过 UnityMCP manage_scene action='save' 或 SaveCurrentScene.Execute() 保存
    /// </summary>
    public static class SetupSection34CEngagementUI
    {
        private const string ChineseFontPath = "Assets/Resources/Fonts/ChineseFont SDF.asset";

        [MenuItem("Tools/DrscfZ/Setup §34C Engagement UI")]
        public static void Run()
        {
            // ---- 1. 找 Canvas ----
            var canvas = FindInLoadedScene("Canvas");
            if (canvas == null)
            {
                Debug.LogError("[Setup34C] Canvas 未找到，终止。");
                return;
            }

            // ---- 2. 找/建 GameUIPanel ----
            var gameUIPanel = GetOrCreateFullscreenPanel(canvas.transform, "GameUIPanel");

            // ---- 3. 找中文字体（失败也继续） ----
            var chineseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ChineseFontPath);
            if (chineseFont == null)
                Debug.LogWarning($"[Setup34C] 未能加载中文字体: {ChineseFontPath}（TMP 可能显示方块）");

            // ---- 4. 分别搭建 5 个模块 ----
            BuildTensionOverlay(canvas.transform);
            BuildCoopMilestoneBar(gameUIPanel.transform, chineseFont);
            BuildGloryMomentBanner(canvas.transform, chineseFont);
            BuildGiftImpactBanner(canvas.transform, chineseFont);
            BuildGiftRecommendBubble(gameUIPanel.transform, chineseFont);

            // ---- 5. 标脏场景（由 PM 用 MCP 或 SaveCurrentScene 保存） ----
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[Setup34C] 完成！建议运行 MCP manage_scene action='save' 或 SaveCurrentScene.Execute() 保存场景。");
        }

        // ==================== E1 TensionOverlay ====================

        private static void BuildTensionOverlay(Transform canvasRoot)
        {
            var overlay = GetOrCreateChild(canvasRoot, "TensionOverlay", () =>
            {
                var g  = new GameObject("TensionOverlay");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(canvasRoot, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return g;
            });

            // 层级：TensionOverlay 必须在 GameUIPanel 上层（遮住底层 UI 产生危机感），
            // 但必须在 GloryMomentBanner / GiftImpactBanner / CoopMilestoneAnnounce / AnnouncementUI 等
            // 顶层横幅/弹窗下层（否则会被 Overlay 盖住看不见）。
            // UGUI 渲染顺序：sibling 靠后的渲染在上。
            // 最佳策略：放在 GameUIPanel 之后一位（GameUIPanel 总在最前半段，banner 随后被本脚本创建时会追加到末尾）。
            var parentT = overlay.transform.parent;
            var gameUIPanelT = parentT != null ? parentT.Find("GameUIPanel") : null;
            if (gameUIPanelT != null)
            {
                int targetIndex = Mathf.Min(gameUIPanelT.GetSiblingIndex() + 1, parentT.childCount - 1);
                overlay.transform.SetSiblingIndex(targetIndex);
            }
            else
            {
                // 兜底：放到倒数第二位（假设最后一位是某 banner/弹窗；至少保证 TensionOverlay 不在最底层）
                int lastIdx = parentT != null ? parentT.childCount - 1 : 0;
                overlay.transform.SetSiblingIndex(Mathf.Max(0, lastIdx - 1));
                Debug.LogWarning("[Setup34C] 未找到 GameUIPanel，TensionOverlay sibling 降级为倒数第二位");
            }

            // 全屏 Image
            var img = overlay.GetComponent<Image>();
            if (img == null) img = overlay.AddComponent<Image>();
            img.color         = new Color(0f, 0f, 0f, 0f);  // 初始隐藏
            img.raycastTarget = false;

            // 挂 TensionOverlayUI
            var ui = overlay.GetComponent<TensionOverlayUI>();
            if (ui == null) ui = overlay.AddComponent<TensionOverlayUI>();

            // 绑定 _overlayImage
            var so = new SerializedObject(ui);
            var p  = so.FindProperty("_overlayImage");
            if (p != null) p.objectReferenceValue = img;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);

            Debug.Log("[Setup34C] E1 TensionOverlay 已挂 TensionOverlayUI + _overlayImage 绑定完成");
        }

        // ==================== E3b CoopMilestoneBar ====================

        private static void BuildCoopMilestoneBar(Transform parent, TMP_FontAsset font)
        {
            var bar = GetOrCreateChild(parent, "CoopMilestoneBar", () =>
            {
                var g  = new GameObject("CoopMilestoneBar");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(parent, false);
                // 顶部中央：宽 560，高 56（标题 + 进度条 + 文字三行）
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot     = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -50f);
                rt.sizeDelta = new Vector2(560f, 56f);
                return g;
            });

            var bg = bar.GetComponent<Image>();
            if (bg == null) bg = bar.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.4f);
            bg.raycastTarget = false;

            // 里程碑名标题（上方 20%）
            var titleGO = CreateTMP(bar.transform, "TitleText",
                new Vector2(0f, 0.66f), new Vector2(1f, 1f),
                new Vector2(8f, 0f),    new Vector2(-8f, 0f),
                18f, TextAlignmentOptions.Left, font);
            var titleTMP = titleGO.GetComponent<TextMeshProUGUI>();
            titleTMP.text = "协作目标：众志成城";

            // 进度条（中间 40%）
            var fillBgGO = GetOrCreateChild(bar.transform, "ProgressBg", () =>
            {
                var g  = new GameObject("ProgressBg");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(bar.transform, false);
                rt.anchorMin = new Vector2(0f, 0.32f);
                rt.anchorMax = new Vector2(1f, 0.62f);
                rt.offsetMin = new Vector2(8f, 0f);
                rt.offsetMax = new Vector2(-8f, 0f);
                return g;
            });
            var fillBg = fillBgGO.GetComponent<Image>();
            if (fillBg == null) fillBg = fillBgGO.AddComponent<Image>();
            fillBg.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);
            fillBg.raycastTarget = false;

            var fillGO = GetOrCreateChild(fillBgGO.transform, "ProgressFill", () =>
            {
                var g  = new GameObject("ProgressFill");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(fillBgGO.transform, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return g;
            });
            var fill = fillGO.GetComponent<Image>();
            if (fill == null) fill = fillGO.AddComponent<Image>();
            fill.color         = new Color(0.2f, 0.85f, 0.4f, 1f);
            fill.type          = Image.Type.Filled;
            fill.fillMethod    = Image.FillMethod.Horizontal;
            fill.fillAmount    = 0f;
            fill.raycastTarget = false;

            // 进度文字（下方 30%）
            var textGO = CreateTMP(bar.transform, "ProgressText",
                new Vector2(0f, 0f), new Vector2(1f, 0.30f),
                new Vector2(8f, 0f), new Vector2(-8f, 0f),
                14f, TextAlignmentOptions.Center, font);
            var progTMP = textGO.GetComponent<TextMeshProUGUI>();
            progTMP.text = "0/500 — 再 500 解锁 全员效率 +10%";

            // ── 达标全屏公告（AnnounceRoot）────────────────────────────
            // 注意：公告定位需要全屏居中。作为 CoopMilestoneBar 子节点会受父矩形裁剪，
            // 故用 anchor (0,0)-(1,1) 并 sizeDelta=0 覆盖整个 bar；但 bar 只是顶部窄条，
            // 所以 AnnounceRoot 放在 Canvas 下更合适；挂到 Canvas 同级（通过查找 Canvas）。
            var canvasT = FindAncestorCanvas(parent);
            var announceRoot = GetOrCreateChild(canvasT, "CoopMilestoneAnnounce", () =>
            {
                var g  = new GameObject("CoopMilestoneAnnounce");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(canvasT, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return g;
            });
            var announceBg = announceRoot.GetComponent<Image>();
            if (announceBg == null) announceBg = announceRoot.AddComponent<Image>();
            announceBg.color         = new Color(0f, 0f, 0f, 0.0f);   // 透明蒙层
            announceBg.raycastTarget = false;

            var announceCg = announceRoot.GetComponent<CanvasGroup>();
            if (announceCg == null) announceCg = announceRoot.AddComponent<CanvasGroup>();
            announceCg.alpha         = 0f;
            announceCg.blocksRaycasts = false;
            announceCg.interactable   = false;

            var announceNameGO = CreateTMP(announceRoot.transform, "AnnounceName",
                new Vector2(0.1f, 0.52f), new Vector2(0.9f, 0.68f),
                Vector2.zero, Vector2.zero,
                64f, TextAlignmentOptions.Center, font);
            var announceNameTMP = announceNameGO.GetComponent<TextMeshProUGUI>();
            announceNameTMP.fontStyle = FontStyles.Bold;
            announceNameTMP.text      = "";
            SetTMPColor(announceNameTMP, new Color(1f, 0.85f, 0.2f));

            var announceDescGO = CreateTMP(announceRoot.transform, "AnnounceDesc",
                new Vector2(0.1f, 0.40f), new Vector2(0.9f, 0.52f),
                Vector2.zero, Vector2.zero,
                36f, TextAlignmentOptions.Center, font);
            var announceDescTMP = announceDescGO.GetComponent<TextMeshProUGUI>();
            announceDescTMP.text = "";
            SetTMPColor(announceDescTMP, Color.white);

            announceRoot.SetActive(false);

            // ── 挂脚本 + 绑字段 ──────────────────────────────────────
            var ui = bar.GetComponent<CoopMilestoneUI>();
            if (ui == null) ui = bar.AddComponent<CoopMilestoneUI>();

            var so = new SerializedObject(ui);
            TryBind(so, "_progressFill",        fill);
            TryBind(so, "_progressText",        progTMP);
            TryBind(so, "_milestoneNameText",   titleTMP);
            TryBind(so, "_announceRoot",        announceRoot.GetComponent<RectTransform>());
            TryBind(so, "_announceCanvasGroup", announceCg);
            TryBind(so, "_announceNameText",    announceNameTMP);
            TryBind(so, "_announceDescText",    announceDescTMP);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);

            SetTMPColor(titleTMP, Color.white);
            SetTMPColor(progTMP, Color.white);

            Debug.Log("[Setup34C] E3b CoopMilestoneBar 已挂 CoopMilestoneUI + 字段绑定完成");
        }

        // ==================== E3a GloryMomentBanner ====================

        private static void BuildGloryMomentBanner(Transform canvasRoot, TMP_FontAsset font)
        {
            var banner = GetOrCreateChild(canvasRoot, "GloryMomentBanner", () =>
            {
                var g  = new GameObject("GloryMomentBanner");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(canvasRoot, false);
                // 全屏容器（子节点按需定位）
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return g;
            });

            // BannerRoot 顶部居中
            var bannerRoot = GetOrCreateChild(banner.transform, "BannerRoot", () =>
            {
                var g  = new GameObject("BannerRoot");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(banner.transform, false);
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot     = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -120f);  // 距顶部 120（避开 CoopMilestoneBar）
                rt.sizeDelta = new Vector2(800f, 96f);
                return g;
            });

            var bannerBg = bannerRoot.GetComponent<Image>();
            if (bannerBg == null) bannerBg = bannerRoot.AddComponent<Image>();
            bannerBg.color         = new Color(0.1f, 0.1f, 0.1f, 0.85f);
            bannerBg.raycastTarget = false;

            var bannerCg = bannerRoot.GetComponent<CanvasGroup>();
            if (bannerCg == null) bannerCg = bannerRoot.AddComponent<CanvasGroup>();
            bannerCg.alpha          = 0f;
            bannerCg.blocksRaycasts = false;
            bannerCg.interactable   = false;

            var mainGO = CreateTMP(bannerRoot.transform, "MainText",
                new Vector2(0f, 0.48f), new Vector2(1f, 1f),
                new Vector2(12f, 0f),   new Vector2(-12f, 0f),
                28f, TextAlignmentOptions.Center, font);
            var mainTMP = mainGO.GetComponent<TextMeshProUGUI>();
            mainTMP.fontStyle = FontStyles.Bold;
            mainTMP.text = "";
            SetTMPColor(mainTMP, new Color(1f, 0.95f, 0.6f));

            var subGO = CreateTMP(bannerRoot.transform, "SubText",
                new Vector2(0f, 0f), new Vector2(1f, 0.48f),
                new Vector2(12f, 0f), new Vector2(-12f, 0f),
                18f, TextAlignmentOptions.Center, font);
            var subTMP = subGO.GetComponent<TextMeshProUGUI>();
            subTMP.text = "";
            SetTMPColor(subTMP, Color.white);

            bannerRoot.SetActive(false);

            // 全屏金色爆发（全 Canvas 覆盖，作为 banner 同级兄弟，但用独立子节点便于绑定）
            var goldBurst = GetOrCreateChild(banner.transform, "GoldBurst", () =>
            {
                var g  = new GameObject("GoldBurst");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(banner.transform, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return g;
            });
            var goldImg = goldBurst.GetComponent<Image>();
            if (goldImg == null) goldImg = goldBurst.AddComponent<Image>();
            goldImg.color         = new Color(1f, 0.85f, 0.2f, 0f);
            goldImg.raycastTarget = false;
            goldBurst.SetActive(false);

            // 全屏红闪
            var redFlash = GetOrCreateChild(banner.transform, "RedFlash", () =>
            {
                var g  = new GameObject("RedFlash");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(banner.transform, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return g;
            });
            var redImg = redFlash.GetComponent<Image>();
            if (redImg == null) redImg = redFlash.AddComponent<Image>();
            redImg.color         = new Color(1f, 0.1f, 0.1f, 0f);
            redImg.raycastTarget = false;
            redFlash.SetActive(false);

            // 挂脚本 + 绑字段
            var ui = banner.GetComponent<GloryMomentUI>();
            if (ui == null) ui = banner.AddComponent<GloryMomentUI>();

            var so = new SerializedObject(ui);
            TryBind(so, "_bannerRoot",        bannerRoot.GetComponent<RectTransform>());
            TryBind(so, "_bannerCanvasGroup", bannerCg);
            TryBind(so, "_bannerBg",          bannerBg);
            TryBind(so, "_bannerText",        mainTMP);
            TryBind(so, "_subText",           subTMP);
            TryBind(so, "_goldBurst",         goldImg);
            TryBind(so, "_redFlash",          redImg);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);

            Debug.Log("[Setup34C] E3a GloryMomentBanner 已挂 GloryMomentUI + 字段绑定完成");
        }

        // ==================== E3c GiftImpactBanner ====================

        private static void BuildGiftImpactBanner(Transform canvasRoot, TMP_FontAsset font)
        {
            var banner = GetOrCreateChild(canvasRoot, "GiftImpactBanner", () =>
            {
                var g  = new GameObject("GiftImpactBanner");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(canvasRoot, false);
                // 顶部居中 720×52，位置在 GloryMomentBanner 下方
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot     = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -230f);  // 距顶部 230
                rt.sizeDelta = new Vector2(720f, 52f);
                return g;
            });

            var bg = banner.GetComponent<Image>();
            if (bg == null) bg = banner.AddComponent<Image>();
            bg.color         = new Color(0.15f, 0.25f, 0.4f, 0.85f);
            bg.raycastTarget = false;

            var cg = banner.GetComponent<CanvasGroup>();
            if (cg == null) cg = banner.AddComponent<CanvasGroup>();
            cg.alpha          = 0f;
            cg.blocksRaycasts = false;
            cg.interactable   = false;

            var textGO = CreateTMP(banner.transform, "ImpactText",
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(12f, 0f), new Vector2(-12f, 0f),
                20f, TextAlignmentOptions.Center, font);
            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = "";
            SetTMPColor(tmp, Color.white);

            banner.SetActive(false);

            // 挂脚本 + 绑字段
            var ui = banner.GetComponent<GiftImpactUI>();
            if (ui == null) ui = banner.AddComponent<GiftImpactUI>();

            var so = new SerializedObject(ui);
            TryBind(so, "_bannerRoot",        banner.GetComponent<RectTransform>());
            TryBind(so, "_bannerCanvasGroup", cg);
            TryBind(so, "_bannerText",        tmp);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);

            Debug.Log("[Setup34C] E3c GiftImpactBanner 已挂 GiftImpactUI + 字段绑定完成");
        }

        // ==================== E4 GiftRecommendBubble ====================

        private static void BuildGiftRecommendBubble(Transform gameUIPanel, TMP_FontAsset font)
        {
            // 查 GiftIconBar
            var giftBar = FindInChildrenByName(gameUIPanel, "GiftIconBar");
            if (giftBar == null)
            {
                Debug.LogWarning("[Setup34C] GiftIconBar 未找到，将占位建一个空 GO（实际需 RebuildGiftIconBar 生成 6 张卡片）");
                var placeholder = new GameObject("GiftIconBar");
                var rt = placeholder.AddComponent<RectTransform>();
                placeholder.transform.SetParent(gameUIPanel, false);
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot     = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(0f, 160f);
                giftBar = placeholder.transform;
            }

            // 气泡挂 GiftIconBar 的子节点（pivot 由运行时脚本根据 giftId 动态调整）
            var bubble = GetOrCreateChild(giftBar, "GiftRecommendBubble", () =>
            {
                var g  = new GameObject("GiftRecommendBubble");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(giftBar, false);
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot     = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(260f, 44f);
                rt.anchoredPosition = new Vector2(0f, 10f);
                return g;
            });

            var bg = bubble.GetComponent<Image>();
            if (bg == null) bg = bubble.AddComponent<Image>();
            bg.color         = new Color(1f, 0.6f, 0.1f, 0.85f);
            bg.raycastTarget = false;

            var textGO = CreateTMP(bubble.transform, "ReasonText",
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(8f, 0f), new Vector2(-8f, 0f),
                18f, TextAlignmentOptions.Center, font);
            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = "";
            SetTMPColor(tmp, Color.white);

            bubble.SetActive(false);

            // 挂脚本放在 GiftIconBar 上（这样 bubble 本身可以 SetActive(false) 而脚本仍运行）
            // 但策划要求挂在气泡节点，并且脚本在 Start 时会 SetActive(false)，不会自阻断订阅（OnEnable 先订阅）
            // → 为避免 Awake 问题，挂在 GiftIconBar 上（常驻），脚本里再控制 bubble 子节点的显隐
            //   注：GiftRecommendationUI 的 Start 明确 SetActive(false) 气泡根节点，正确。
            //   挂载位置取 GiftIconBar（常驻）以确保事件订阅不被阻断。
            var host = giftBar.gameObject;
            var ui = host.GetComponent<GiftRecommendationUI>();
            if (ui == null) ui = host.AddComponent<GiftRecommendationUI>();

            var so = new SerializedObject(ui);
            TryBind(so, "_bubbleRoot",  bubble.GetComponent<RectTransform>());
            TryBind(so, "_bubbleBg",    bg);
            TryBind(so, "_reasonText",  tmp);
            TryBind(so, "_giftIconBar", giftBar.GetComponent<RectTransform>());
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);

            Debug.Log("[Setup34C] E4 GiftRecommendBubble 已挂 GiftRecommendationUI + 字段绑定完成（挂载宿主：GiftIconBar 常驻节点）");
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
                Debug.LogWarning($"[Setup34C] {name} 占位建出（原场景缺失）。");
                return g;
            });
        }

        /// <summary>创建一个带 TextMeshProUGUI 的子节点，返回 GameObject。
        /// 使用 anchor 百分比 + offset（保证 HLG/VLG rebuild 时锚点不被覆盖）。</summary>
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
                Debug.LogWarning($"[Setup34C] TryBind: 字段 {fieldName} 未找到（可能脚本未定义该 SerializeField）");
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

        /// <summary>向上回溯找 Canvas 节点（fallback：FindInLoadedScene("Canvas")）。</summary>
        private static Transform FindAncestorCanvas(Transform start)
        {
            var t = start;
            while (t != null)
            {
                if (t.GetComponent<Canvas>() != null) return t;
                t = t.parent;
            }
            var canvas = FindInLoadedScene("Canvas");
            return canvas != null ? canvas.transform : start;
        }

        /// <summary>在指定子孙树中按名字查找第一个 Transform（深度优先）。</summary>
        private static Transform FindInChildrenByName(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var hit = FindInChildrenByName(root.GetChild(i), name);
                if (hit != null) return hit;
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
