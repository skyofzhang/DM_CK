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
    /// Tools → DrscfZ → Setup §34B Data Flow UI
    ///
    /// 一键搭建 §34 Layer 2 组 B 数据流可视化 UI 骨架：
    ///
    ///   Canvas/EfficiencyRaceBanner                        (B10a 顶部滚动 Top2 PK + EfficiencyRaceUI)
    ///   Canvas/DayPreviewBanner                            (B10b 底部夜晚预告 + DayPreviewBanner)
    ///   Canvas/SurvivalSettlementPanel 改造                 (B2 页码圆点 + 跳过按钮 + 动态标语 + 4 条高光)
    ///
    /// 兜底策略（对齐 docs/multi_agent_workflow.md 决策 6 "MCP 优先 + Console 监控"）：
    ///   缺 GameObject → 占位建出（不跳过）；Inspector 中现有 SurvivalSettlementUI 字段保持不变
    ///   禁用 EditorUtility.DisplayDialog（CLAUDE.md 踩坑）
    ///   SerializedObject 写 m_fontColor + m_fontColor32（CLAUDE.md TMP 颜色踩坑）
    ///   AddComponent&lt;RectTransform&gt; 显式写，GameObject 构造后立即加（CLAUDE.md UI 布局踩坑）
    ///   不自动 SaveScene（CLAUDE.md 禁止 Coplay save_scene），由 PM 通过 UnityMCP manage_scene action='save'
    ///
    /// 可视层级：
    ///   EfficiencyRaceBanner 在 Canvas 下作为第一个子节点（SetAsFirstSibling）避免遮挡 TensionOverlay/公告；
    ///   DayPreviewBanner 在 Canvas 下底部；SurvivalSettlementPanel 已存在，仅追加子节点。
    /// </summary>
    public static class SetupSection34BDataFlowUI
    {
        private const string ChineseFontPath = "Assets/Resources/Fonts/AlibabaPuHuiTi-3-85-Bold SDF.asset";

        [MenuItem("Tools/DrscfZ/Setup §34B Data Flow UI")]
        public static void Run()
        {
            // ---- 1. 找 Canvas ----
            var canvas = FindInLoadedScene("Canvas");
            if (canvas == null)
            {
                Debug.LogError("[Setup34B] Canvas 未找到，终止。");
                return;
            }

            // ---- 2. 找中文字体（失败也继续） ----
            var chineseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ChineseFontPath);
            if (chineseFont == null)
                Debug.LogWarning($"[Setup34B] 未能加载中文字体: {ChineseFontPath}（TMP 可能显示方块）");

            // ---- 3. 构建 3 个模块 ----
            BuildEfficiencyRaceBanner(canvas.transform, chineseFont);
            BuildDayPreviewBanner    (canvas.transform, chineseFont);
            AugmentSettlementPanel   (canvas.transform, chineseFont);

            // ---- 4. 标脏场景 ----
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[Setup34B] 完成！建议运行 MCP manage_scene action='save' 或 SaveCurrentScene.Execute() 保存场景。");
        }

        // ==================== B10a EfficiencyRaceBanner ====================

        private static void BuildEfficiencyRaceBanner(Transform canvasRoot, TMP_FontAsset font)
        {
            // Fix H (组 B Reviewer P1)：原 bool hostCreated = canvasRoot.Find("EfficiencyRaceBanner") == null;
            //   仅服务于废弃的 SetAsFirstSibling 判断（见下方注释），删除以消除 unused 警告。
            var host = GetOrCreateChild(canvasRoot, "EfficiencyRaceBanner", () =>
            {
                var g  = new GameObject("EfficiencyRaceBanner");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(canvasRoot, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return g;
            });

            // Fix H (组 B Reviewer P1)：原 SetAsFirstSibling 会把 EfficiencyRace 放在 Canvas 首位，
            //   位于 §34C TensionOverlay 等全屏覆盖层之下被遮挡。
            //   改为不调整 sibling：新建 GO 默认追加在末尾 = 最上层（自然在 TensionOverlay 之上）。
            //   （hostCreated 变量保留，供潜在其他初始化用；此处不再调用 SetAsFirstSibling）
            // if (hostCreated) host.transform.SetAsFirstSibling();  // 已废弃

            // BannerRoot：顶部横条（高 80px）
            var bannerRoot = GetOrCreateChild(host.transform, "BannerRoot", () =>
            {
                var g  = new GameObject("BannerRoot");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(host.transform, false);
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot     = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -140f);  // 避开 TopBarUI（预留 140px）
                rt.sizeDelta = new Vector2(-160f, 80f);        // 左右各留 80px
                return g;
            });

            var bg = bannerRoot.GetComponent<Image>();
            if (bg == null) bg = bannerRoot.AddComponent<Image>();
            bg.color         = new Color(0.18f, 0.45f, 0.25f, 0.80f);  // 暗绿色（安全期 + 社交比较）
            bg.raycastTarget = false;

            var cg = bannerRoot.GetComponent<CanvasGroup>();
            if (cg == null) cg = bannerRoot.AddComponent<CanvasGroup>();
            cg.alpha          = 0f;
            cg.blocksRaycasts = false;
            cg.interactable   = false;

            // MessageText：居中 PK 文案
            var msgGO = CreateTMP(bannerRoot.transform, "MessageText",
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(24f, 0f), new Vector2(-24f, 0f),
                28f, TextAlignmentOptions.Center, font);
            var msgTMP = msgGO.GetComponent<TextMeshProUGUI>();
            msgTMP.text = "";
            msgTMP.fontStyle = FontStyles.Bold;
            SetTMPColor(msgTMP, Color.white);

            bannerRoot.SetActive(false);

            var ui = host.GetComponent<EfficiencyRaceUI>();
            if (ui == null) ui = host.AddComponent<EfficiencyRaceUI>();

            var so = new SerializedObject(ui);
            TryBind(so, "_bannerRoot",        bannerRoot.GetComponent<RectTransform>());
            TryBind(so, "_bannerCanvasGroup", cg);
            TryBind(so, "_messageText",       msgTMP);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);

            Debug.Log("[Setup34B] B10a EfficiencyRaceBanner 已挂 EfficiencyRaceUI + 字段绑定完成");
        }

        // ==================== B10b DayPreviewBanner ====================

        private static void BuildDayPreviewBanner(Transform canvasRoot, TMP_FontAsset font)
        {
            var host = GetOrCreateChild(canvasRoot, "DayPreviewBanner", () =>
            {
                var g  = new GameObject("DayPreviewBanner");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(canvasRoot, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return g;
            });

            // BannerRoot：底部横条（高 180px，避开 GiftIconBar 160px 区）
            var bannerRoot = GetOrCreateChild(host.transform, "BannerRoot", () =>
            {
                var g  = new GameObject("BannerRoot");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(host.transform, false);
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot     = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 180f);  // GiftIconBar 160px 之上
                rt.sizeDelta = new Vector2(-100f, 180f);
                return g;
            });

            var bg = bannerRoot.GetComponent<Image>();
            if (bg == null) bg = bannerRoot.AddComponent<Image>();
            bg.color         = new Color(0.10f, 0.10f, 0.28f, 0.90f);  // 深夜蓝紫（营造紧迫感）
            bg.raycastTarget = false;

            var cg = bannerRoot.GetComponent<CanvasGroup>();
            if (cg == null) cg = bannerRoot.AddComponent<CanvasGroup>();
            cg.alpha          = 0f;
            cg.blocksRaycasts = false;
            cg.interactable   = false;

            // Headline：第一行"今夜预报：..."
            var headGO = CreateTMP(bannerRoot.transform, "HeadlineText",
                new Vector2(0f, 0.66f), new Vector2(1f, 1f),
                new Vector2(16f, 0f),   new Vector2(-16f, 0f),
                32f, TextAlignmentOptions.Center, font);
            var headTMP = headGO.GetComponent<TextMeshProUGUI>();
            headTMP.text = "";
            headTMP.fontStyle = FontStyles.Bold;
            SetTMPColor(headTMP, new Color(1f, 0.35f, 0.40f));  // 红色预警

            // Body：第二行"Boss HP / 预计怪物数 / 特殊效果"
            var bodyGO = CreateTMP(bannerRoot.transform, "BodyText",
                new Vector2(0f, 0.22f), new Vector2(1f, 0.66f),
                new Vector2(16f, 0f),   new Vector2(-16f, 0f),
                22f, TextAlignmentOptions.Center, font);
            var bodyTMP = bodyGO.GetComponent<TextMeshProUGUI>();
            bodyTMP.text = "";
            SetTMPColor(bodyTMP, Color.white);

            // Countdown：底部"倒计时：8s"
            var cdGO = CreateTMP(bannerRoot.transform, "CountdownText",
                new Vector2(0f, 0f), new Vector2(1f, 0.22f),
                new Vector2(16f, 4f), new Vector2(-16f, 0f),
                22f, TextAlignmentOptions.Center, font);
            var cdTMP = cdGO.GetComponent<TextMeshProUGUI>();
            cdTMP.text = "";
            SetTMPColor(cdTMP, new Color(1f, 0.92f, 0.55f));  // 金黄

            bannerRoot.SetActive(false);

            var ui = host.GetComponent<DayPreviewBanner>();
            if (ui == null) ui = host.AddComponent<DayPreviewBanner>();

            var so = new SerializedObject(ui);
            TryBind(so, "_bannerRoot",        bannerRoot.GetComponent<RectTransform>());
            TryBind(so, "_bannerCanvasGroup", cg);
            TryBind(so, "_headlineText",      headTMP);
            TryBind(so, "_bodyText",          bodyTMP);
            TryBind(so, "_countdownText",     cdTMP);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);

            Debug.Log("[Setup34B] B10b DayPreviewBanner 已挂 DayPreviewBanner + 字段绑定完成");
        }

        // ==================== B2 SurvivalSettlementPanel 改造 ====================

        private static void AugmentSettlementPanel(Transform canvasRoot, TMP_FontAsset font)
        {
            // 找现有 SurvivalSettlementUI 组件宿主
            var existing = UnityEngine.Object.FindObjectOfType<SurvivalSettlementUI>(true);
            GameObject settlementHost = existing != null ? existing.gameObject : null;
            if (settlementHost == null)
                settlementHost = FindInLoadedScene("SurvivalSettlementPanel");

            if (settlementHost == null)
            {
                Debug.LogWarning("[Setup34B] SurvivalSettlementPanel 未找到，跳过 B2 改造。请确认 SurvivalSettlementUI 已存在于场景中。");
                return;
            }

            var ui = settlementHost.GetComponent<SurvivalSettlementUI>();
            if (ui == null)
            {
                Debug.LogWarning("[Setup34B] SurvivalSettlementPanel 缺 SurvivalSettlementUI 组件，跳过字段绑定。");
                return;
            }

            // --- 1. 页码圆点容器 ---
            var pageDotsContainer = GetOrCreateChild(settlementHost.transform, "PageDotsContainer", () =>
            {
                var g  = new GameObject("PageDotsContainer");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(settlementHost.transform, false);
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot     = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 80f);  // 底部 80px
                rt.sizeDelta = new Vector2(180f, 30f);
                return g;
            });

            Image[] pageDots = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var dotGO = GetOrCreateChild(pageDotsContainer.transform, $"Dot{i + 1}", () =>
                {
                    var g  = new GameObject($"Dot{idx + 1}");
                    var rt = g.AddComponent<RectTransform>();
                    g.transform.SetParent(pageDotsContainer.transform, false);
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot     = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(18f, 18f);
                    rt.anchoredPosition = new Vector2((idx - 1) * 32f, 0f);  // -32 / 0 / +32
                    return g;
                });
                var img = dotGO.GetComponent<Image>();
                if (img == null) img = dotGO.AddComponent<Image>();
                img.color         = new Color(1f, 1f, 1f, 0.35f);
                img.raycastTarget = false;
                pageDots[i] = img;
            }

            // --- 2. Skip 按钮（主播"立即重开"）---
            var skipBtnGO = GetOrCreateChild(settlementHost.transform, "SkipButton", () =>
            {
                var g  = new GameObject("SkipButton");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(settlementHost.transform, false);
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot     = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(-40f, 60f);
                rt.sizeDelta = new Vector2(220f, 64f);
                return g;
            });

            var skipBg = skipBtnGO.GetComponent<Image>();
            if (skipBg == null) skipBg = skipBtnGO.AddComponent<Image>();
            skipBg.color         = new Color(0.85f, 0.35f, 0.20f, 0.95f);
            skipBg.raycastTarget = true;

            var skipBtn = skipBtnGO.GetComponent<Button>();
            if (skipBtn == null) skipBtn = skipBtnGO.AddComponent<Button>();

            var skipLabelGO = CreateTMP(skipBtnGO.transform, "Label",
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(8f, 4f), new Vector2(-8f, -4f),
                24f, TextAlignmentOptions.Center, font);
            var skipLabelTMP = skipLabelGO.GetComponent<TextMeshProUGUI>();
            skipLabelTMP.text = "立即重开";
            skipLabelTMP.fontStyle = FontStyles.Bold;
            SetTMPColor(skipLabelTMP, Color.white);

            skipBtnGO.SetActive(false);  // 默认隐藏，等主播身份确认后显示

            // --- 3. 动态标语 TMP（挂在 ScreenC 下，若找到）---
            TextMeshProUGUI dynamicTaglineTMP = null;
            Transform screenCTrans = settlementHost.transform.Find("ScreenC");
            if (screenCTrans == null)
                screenCTrans = FindChildRecursive(settlementHost.transform, "ScreenC");

            if (screenCTrans != null)
            {
                var taglineGO = GetOrCreateChild(screenCTrans, "DynamicTaglineText", () =>
                {
                    var g  = new GameObject("DynamicTaglineText");
                    var rt = g.AddComponent<RectTransform>();
                    g.transform.SetParent(screenCTrans, false);
                    rt.anchorMin = new Vector2(0.05f, 0.02f);
                    rt.anchorMax = new Vector2(0.95f, 0.12f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    return g;
                });
                var t = taglineGO.GetComponent<TextMeshProUGUI>();
                if (t == null) t = taglineGO.AddComponent<TextMeshProUGUI>();
                t.fontSize         = 26f;
                t.alignment        = TextAlignmentOptions.Center;
                t.enableAutoSizing = false;
                t.raycastTarget    = false;
                if (font != null) t.font = font;
                t.fontStyle = FontStyles.Italic;
                t.text = "";
                SetTMPColor(t, new Color(1f, 0.92f, 0.55f));
                dynamicTaglineTMP = t;
            }
            else
            {
                Debug.LogWarning("[Setup34B] 未找到 ScreenC 子节点，跳过 DynamicTaglineText 绑定");
            }

            // --- 4. 高光 4 条（挂在 ScreenA 下，若找到）---
            TextMeshProUGUI topDamageTMP = null, bestRescueTMP = null, dramaticTMP = null, closestCallTMP = null;
            Transform screenATrans = settlementHost.transform.Find("ScreenA");
            if (screenATrans == null)
                screenATrans = FindChildRecursive(settlementHost.transform, "ScreenA");

            if (screenATrans != null)
            {
                topDamageTMP   = EnsureHighlightTMP(screenATrans, "HighlightTopDamage",   0.35f, 0.43f, font);
                bestRescueTMP  = EnsureHighlightTMP(screenATrans, "HighlightBestRescue",  0.27f, 0.35f, font);
                dramaticTMP    = EnsureHighlightTMP(screenATrans, "HighlightDramatic",    0.19f, 0.27f, font);
                closestCallTMP = EnsureHighlightTMP(screenATrans, "HighlightClosestCall", 0.11f, 0.19f, font);
            }
            else
            {
                Debug.LogWarning("[Setup34B] 未找到 ScreenA 子节点，跳过高光 4 条绑定");
            }

            // --- 5. SerializedObject 字段绑定 ---
            var so = new SerializedObject(ui);

            // _pageDots 数组
            var dotsProp = so.FindProperty("_pageDots");
            if (dotsProp != null)
            {
                dotsProp.arraySize = 3;
                for (int i = 0; i < 3; i++)
                    dotsProp.GetArrayElementAtIndex(i).objectReferenceValue = pageDots[i];
            }
            else
            {
                Debug.LogWarning("[Setup34B] SurvivalSettlementUI 缺 _pageDots 字段");
            }

            TryBind(so, "_skipButton",          skipBtn);
            TryBind(so, "_dynamicTaglineText",  dynamicTaglineTMP);
            TryBind(so, "_topDamageText",       topDamageTMP);
            TryBind(so, "_bestRescueText",      bestRescueTMP);
            TryBind(so, "_dramaticEventText",   dramaticTMP);
            TryBind(so, "_closestCallText",     closestCallTMP);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);

            Debug.Log("[Setup34B] B2 SurvivalSettlementPanel 改造完成：PageDots ×3 + SkipButton + DynamicTagline + 4 高光条");
        }

        private static TextMeshProUGUI EnsureHighlightTMP(Transform screenA, string name,
                                                          float anchorYMin, float anchorYMax,
                                                          TMP_FontAsset font)
        {
            var go = GetOrCreateChild(screenA, name, () =>
            {
                var g  = new GameObject(name);
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(screenA, false);
                rt.anchorMin = new Vector2(0.08f, anchorYMin);
                rt.anchorMax = new Vector2(0.92f, anchorYMax);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return g;
            });

            var t = go.GetComponent<TextMeshProUGUI>();
            if (t == null) t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize         = 22f;
            t.alignment        = TextAlignmentOptions.Center;
            t.enableAutoSizing = false;
            t.raycastTarget    = false;
            if (font != null) t.font = font;
            t.text = "";
            SetTMPColor(t, new Color(0.95f, 0.95f, 0.85f));
            return t;
        }

        // ==================== 辅助方法 ====================

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
                Debug.LogWarning($"[Setup34B] TryBind: 字段 {fieldName} 未找到（可能脚本未定义该 SerializeField）");
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

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent == null) return null;
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var nested = FindChildRecursive(child, name);
                if (nested != null) return nested;
            }
            return null;
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
