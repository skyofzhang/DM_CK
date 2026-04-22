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
    /// Tools → DrscfZ → Setup Section 17.15 + 24.5 UI
    ///
    /// 一键搭建：
    ///   - Canvas/GameUIPanel/OnboardingBubble（挂 OnboardingBubbleUI；B1-B3 气泡模板）
    ///   - BroadcasterPanel/DecisionHUD（挂 BroadcasterDecisionHUD；3 张卡片 + "前往"按钮）
    ///   - BroadcasterPanel/DisableOnboardingButton（关闭引导按钮）
    ///   - BroadcasterPanel/BroadcasterTipBar（顶部话术条）
    ///
    /// 兜底策略（对齐 docs/multi_agent_workflow.md 决策 6 "MCP 优先 + Console 监控"）：
    ///   缺 GameObject → 占位建出，不跳过
    ///   禁用 EditorUtility.DisplayDialog（CLAUDE.md 踩坑）
    ///   不 SaveScene（由 UnityMCP manage_scene action='save' 负责）
    /// </summary>
    public static class SetupSection17_15_24_5UI
    {
        [MenuItem("Tools/DrscfZ/Setup Section 17.15 + 24.5 UI")]
        public static void Run()
        {
            // ---- 1. 找 Canvas ----
            var canvas = FindInLoadedScene("Canvas");
            if (canvas == null)
            {
                Debug.LogError("[Setup17.15+24.5] Canvas 未找到，终止。");
                return;
            }

            // ---- 2. 找 GameUIPanel（§17.15 OnboardingBubble 父节点） ----
            var gameUIPanel = GetOrCreateChild(canvas.transform, "GameUIPanel", () =>
            {
                var go = new GameObject("GameUIPanel");
                go.transform.SetParent(canvas.transform, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                Debug.LogWarning("[Setup17.15+24.5] GameUIPanel 占位建出（原场景缺失）。");
                return go;
            });

            // ---- 3. 创建 OnboardingBubble（挂 OnboardingBubbleUI） ----
            BuildOnboardingBubble(gameUIPanel.transform);

            // ---- 4. 找 BroadcasterPanel（§24.5 DecisionHUD 父节点） ----
            //   优先按挂载的 BroadcasterPanel 脚本定位（主 repo GO 可能叫 BroadcasterPanelController / PanelRoot 等）；
            //   其次回退到按名字 Find；都没找到才占位建
            GameObject broadcasterPanel = null;
            var existingBp = UnityEngine.Object.FindObjectOfType<DrscfZ.UI.BroadcasterPanel>(true);
            if (existingBp != null)
            {
                broadcasterPanel = existingBp.gameObject;
                Debug.Log($"[Setup17.15+24.5] 已按脚本定位 BroadcasterPanel GO：{broadcasterPanel.name}");
            }
            if (broadcasterPanel == null)
                broadcasterPanel = FindInLoadedScene("BroadcasterPanel");
            if (broadcasterPanel == null)
            {
                // 占位建出（实际 BroadcasterPanel 应该已存在；走到这里说明主 repo 场景缺失该面板）
                Debug.LogWarning("[Setup17.15+24.5] BroadcasterPanel 未找到，占位建出父节点。");
                broadcasterPanel = new GameObject("BroadcasterPanel");
                broadcasterPanel.transform.SetParent(canvas.transform, false);
                var rt = broadcasterPanel.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            // ---- 5. 创建 DecisionHUD（挂 BroadcasterDecisionHUD） ----
            BuildDecisionHUD(broadcasterPanel.transform);

            // ---- 6. 创建 DisableOnboardingButton（BroadcasterPanel 次要区域） ----
            var disableBtn = BuildDisableOnboardingButton(broadcasterPanel.transform);

            // ---- 7. 创建 BroadcasterTipBar（顶部话术条） ----
            var tipBar = BuildBroadcasterTipBar(broadcasterPanel.transform);

            // ---- 8. 绑定 BroadcasterPanel 的 SerializedField（_btnDisableOnboarding / _broadcasterTipText） ----
            var bp = broadcasterPanel.GetComponent<BroadcasterPanel>();
            if (bp != null)
            {
                var so = new SerializedObject(bp);
                var pBtn = so.FindProperty("_btnDisableOnboarding");
                if (pBtn != null && disableBtn != null)
                    pBtn.objectReferenceValue = disableBtn.GetComponent<Button>();

                var pTip = so.FindProperty("_broadcasterTipText");
                if (pTip != null && tipBar != null)
                    pTip.objectReferenceValue = tipBar.GetComponentInChildren<TextMeshProUGUI>();

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(bp);
                Debug.Log("[Setup17.15+24.5] BroadcasterPanel _btnDisableOnboarding / _broadcasterTipText 绑定完成");
            }
            else
            {
                Debug.LogWarning("[Setup17.15+24.5] BroadcasterPanel 未挂 BroadcasterPanel 脚本，无法绑定字段；请人工补挂。");
            }

            // ---- 9. 标脏场景，不自动 SaveScene（由 UnityMCP save 处理） ----
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[Setup17.15+24.5] 完成！建议运行 MCP manage_scene action='save' 保存场景。");
        }

        // ==================== §17.15 OnboardingBubble ====================

        private static void BuildOnboardingBubble(Transform parent)
        {
            var bubble = GetOrCreateChild(parent, "OnboardingBubble", () =>
            {
                var go = new GameObject("OnboardingBubble");
                go.transform.SetParent(parent, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return go;
            });

            // 挂脚本
            var ui = bubble.GetComponent<OnboardingBubbleUI>();
            if (ui == null) ui = bubble.AddComponent<OnboardingBubbleUI>();

            // 气泡容器（屏幕中央上方）
            var bubbleRoot = GetOrCreateChild(bubble.transform, "BubbleRoot", () =>
            {
                var go = new GameObject("BubbleRoot");
                go.transform.SetParent(bubble.transform, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.7f);
                rt.anchorMax = new Vector2(0.5f, 0.7f);
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(800f, 100f);
                rt.anchoredPosition = Vector2.zero;
                return go;
            });

            // 黑底 Image
            var bg = bubbleRoot.GetComponent<Image>();
            if (bg == null) bg = bubbleRoot.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.7f);
            bg.raycastTarget = false;

            // CanvasGroup
            var cg = bubbleRoot.GetComponent<CanvasGroup>();
            if (cg == null) cg = bubbleRoot.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable   = false;
            cg.alpha          = 0f;

            // 文本
            var textGO = GetOrCreateChild(bubbleRoot.transform, "BubbleText", () =>
            {
                var go = new GameObject("BubbleText");
                go.transform.SetParent(bubbleRoot.transform, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(20f, 0f);
                rt.offsetMax = new Vector2(-20f, 0f);
                return go;
            });
            var textTMP = textGO.GetComponent<TextMeshProUGUI>();
            if (textTMP == null) textTMP = textGO.AddComponent<TextMeshProUGUI>();
            textTMP.text         = "";
            textTMP.fontSize     = 36f;
            textTMP.alignment    = TextAlignmentOptions.Center;
            textTMP.enableAutoSizing = false;
            textTMP.raycastTarget    = false;
            SetTMPColor(textTMP, Color.white);

            // 绑定 OnboardingBubbleUI 字段
            var so = new SerializedObject(ui);
            var pRoot = so.FindProperty("_bubbleRoot");
            var pBg   = so.FindProperty("_bubbleBg");
            var pText = so.FindProperty("_bubbleText");
            var pCg   = so.FindProperty("_canvasGroup");
            if (pRoot != null) pRoot.objectReferenceValue = bubbleRoot.GetComponent<RectTransform>();
            if (pBg   != null) pBg  .objectReferenceValue = bg;
            if (pText != null) pText.objectReferenceValue = textTMP;
            if (pCg   != null) pCg  .objectReferenceValue = cg;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);

            // 默认隐藏气泡（运行时脚本会控制显隐）
            bubbleRoot.SetActive(false);
            Debug.Log("[Setup17.15+24.5] OnboardingBubble 已挂 OnboardingBubbleUI + 字段绑定完成");
        }

        // ==================== §24.5 BroadcasterDecisionHUD ====================

        private static void BuildDecisionHUD(Transform parent)
        {
            var hud = GetOrCreateChild(parent, "DecisionHUD", () =>
            {
                var go = new GameObject("DecisionHUD");
                go.transform.SetParent(parent, false);
                var rt = go.AddComponent<RectTransform>();
                // 左上角 340×240
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot     = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(10f, -10f);
                rt.sizeDelta = new Vector2(340f, 240f);
                return go;
            });

            // 挂脚本
            var hudUI = hud.GetComponent<BroadcasterDecisionHUD>();
            if (hudUI == null) hudUI = hud.AddComponent<BroadcasterDecisionHUD>();

            // 卡片容器（垂直布局）
            var cardsRoot = GetOrCreateChild(hud.transform, "CardsRoot", () =>
            {
                var go = new GameObject("CardsRoot");
                go.transform.SetParent(hud.transform, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return go;
            });
            var vlg = cardsRoot.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) vlg = cardsRoot.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment         = TextAnchor.UpperLeft;
            vlg.spacing                = 4f;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = false;
            vlg.padding                = new RectOffset(4, 4, 4, 4);

            // 3 张卡片
            var card0Data = BuildCardView(cardsRoot.transform, "Card0");
            var card1Data = BuildCardView(cardsRoot.transform, "Card1");
            var card2Data = BuildCardView(cardsRoot.transform, "Card2");

            // 首次气泡提示
            var tipRoot = GetOrCreateChild(hud.transform, "FirstTimeTip", () =>
            {
                var go = new GameObject("FirstTimeTip");
                go.transform.SetParent(hud.transform, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot     = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -4f);
                rt.sizeDelta = new Vector2(0f, 40f);
                return go;
            });
            var tipBg = tipRoot.GetComponent<Image>();
            if (tipBg == null) tipBg = tipRoot.AddComponent<Image>();
            tipBg.color = new Color(0.2f, 0.6f, 1f, 0.85f);
            tipBg.raycastTarget = false;

            var tipTextGO = GetOrCreateChild(tipRoot.transform, "TipText", () =>
            {
                var go = new GameObject("TipText");
                go.transform.SetParent(tipRoot.transform, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(8f, 0f);
                rt.offsetMax = new Vector2(-8f, 0f);
                return go;
            });
            var tipTMP = tipTextGO.GetComponent<TextMeshProUGUI>();
            if (tipTMP == null) tipTMP = tipTextGO.AddComponent<TextMeshProUGUI>();
            tipTMP.text = "这里会告诉你现在该做什么";
            tipTMP.fontSize = 16f;
            tipTMP.alignment = TextAlignmentOptions.Center;
            tipTMP.enableAutoSizing = false;
            tipTMP.raycastTarget = false;
            SetTMPColor(tipTMP, Color.white);

            // 绑定 BroadcasterDecisionHUD 字段
            var so = new SerializedObject(hudUI);
            var pCardsRoot = so.FindProperty("_cardsRoot");
            if (pCardsRoot != null) pCardsRoot.objectReferenceValue = cardsRoot.GetComponent<RectTransform>();
            BindCardViewField(so, "_card0", card0Data);
            BindCardViewField(so, "_card1", card1Data);
            BindCardViewField(so, "_card2", card2Data);

            var pTipRoot = so.FindProperty("_firstTimeTipRoot");
            var pTipText = so.FindProperty("_firstTimeTipText");
            if (pTipRoot != null) pTipRoot.objectReferenceValue = tipRoot;
            if (pTipText != null) pTipText.objectReferenceValue = tipTMP;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hudUI);
            Debug.Log("[Setup17.15+24.5] BroadcasterDecisionHUD 已挂脚本 + 3 张卡片 + 首次气泡 + 字段绑定完成");
        }

        /// <summary>构建一张卡片（Root + Bg + Icon + Message + JumpButton）。</summary>
        private static CardViewData BuildCardView(Transform parent, string name)
        {
            var go = GetOrCreateChild(parent, name, () =>
            {
                var g = new GameObject(name);
                g.transform.SetParent(parent, false);
                var rt = g.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0f, 70f);
                return g;
            });

            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 70f;

            var bg = go.GetComponent<Image>();
            if (bg == null) bg = go.AddComponent<Image>();
            bg.color = new Color(0.85f, 0.25f, 0.25f, 0.92f);
            bg.raycastTarget = true;

            // icon
            var iconGO = GetOrCreateChild(go.transform, "Icon", () =>
            {
                var g = new GameObject("Icon");
                g.transform.SetParent(go.transform, false);
                var rt = g.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0.15f, 1f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return g;
            });
            var iconTMP = iconGO.GetComponent<TextMeshProUGUI>();
            if (iconTMP == null) iconTMP = iconGO.AddComponent<TextMeshProUGUI>();
            iconTMP.fontSize = 28f;
            iconTMP.alignment = TextAlignmentOptions.Center;
            iconTMP.enableAutoSizing = false;
            iconTMP.raycastTarget = false;
            SetTMPColor(iconTMP, Color.white);

            // message
            var msgGO = GetOrCreateChild(go.transform, "Message", () =>
            {
                var g = new GameObject("Message");
                g.transform.SetParent(go.transform, false);
                var rt = g.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.15f, 0f);
                rt.anchorMax = new Vector2(0.78f, 1f);
                rt.offsetMin = new Vector2(4f, 0f);
                rt.offsetMax = new Vector2(-4f, 0f);
                return g;
            });
            var msgTMP = msgGO.GetComponent<TextMeshProUGUI>();
            if (msgTMP == null) msgTMP = msgGO.AddComponent<TextMeshProUGUI>();
            msgTMP.fontSize = 20f;
            msgTMP.alignment = TextAlignmentOptions.MidlineLeft;
            msgTMP.enableAutoSizing = false;
            msgTMP.raycastTarget = false;
            SetTMPColor(msgTMP, Color.white);

            // jump button
            var btnGO = GetOrCreateChild(go.transform, "JumpBtn", () =>
            {
                var g = new GameObject("JumpBtn");
                g.transform.SetParent(go.transform, false);
                var rt = g.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.78f, 0.1f);
                rt.anchorMax = new Vector2(0.98f, 0.9f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return g;
            });
            var btnImg = btnGO.GetComponent<Image>();
            if (btnImg == null) btnImg = btnGO.AddComponent<Image>();
            btnImg.color = new Color(1f, 1f, 1f, 0.2f);

            var btn = btnGO.GetComponent<Button>();
            if (btn == null) btn = btnGO.AddComponent<Button>();

            var btnLblGO = GetOrCreateChild(btnGO.transform, "Label", () =>
            {
                var g = new GameObject("Label");
                g.transform.SetParent(btnGO.transform, false);
                var rt = g.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return g;
            });
            var lblTMP = btnLblGO.GetComponent<TextMeshProUGUI>();
            if (lblTMP == null) lblTMP = btnLblGO.AddComponent<TextMeshProUGUI>();
            lblTMP.text = "前往";
            lblTMP.fontSize = 18f;
            lblTMP.alignment = TextAlignmentOptions.Center;
            lblTMP.enableAutoSizing = false;
            lblTMP.raycastTarget = false;
            SetTMPColor(lblTMP, Color.white);

            go.SetActive(false);
            return new CardViewData
            {
                root = go,
                bg = bg,
                iconText = iconTMP,
                messageText = msgTMP,
                jumpButton = btn
            };
        }

        private static void BindCardViewField(SerializedObject so, string fieldName, CardViewData card)
        {
            var p = so.FindProperty(fieldName);
            if (p == null || card == null) return;
            var pRoot = p.FindPropertyRelative("root");
            var pBg   = p.FindPropertyRelative("bg");
            var pIcon = p.FindPropertyRelative("iconText");
            var pMsg  = p.FindPropertyRelative("messageText");
            var pBtn  = p.FindPropertyRelative("jumpButton");
            if (pRoot != null) pRoot.objectReferenceValue = card.root;
            if (pBg   != null) pBg  .objectReferenceValue = card.bg;
            if (pIcon != null) pIcon.objectReferenceValue = card.iconText;
            if (pMsg  != null) pMsg .objectReferenceValue = card.messageText;
            if (pBtn  != null) pBtn .objectReferenceValue = card.jumpButton;
        }

        // ==================== §17.15 DisableOnboardingButton ====================

        private static GameObject BuildDisableOnboardingButton(Transform parent)
        {
            var btnGO = GetOrCreateChild(parent, "DisableOnboardingButton", () =>
            {
                var g = new GameObject("DisableOnboardingButton");
                g.transform.SetParent(parent, false);
                var rt = g.AddComponent<RectTransform>();
                // 次要区域：左下角 160×36
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot     = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(10f, 10f);
                rt.sizeDelta = new Vector2(160f, 36f);
                return g;
            });

            var bg = btnGO.GetComponent<Image>();
            if (bg == null) bg = btnGO.AddComponent<Image>();
            bg.color = new Color(0.35f, 0.35f, 0.45f, 0.9f);

            var btn = btnGO.GetComponent<Button>();
            if (btn == null) btn = btnGO.AddComponent<Button>();

            var lblGO = GetOrCreateChild(btnGO.transform, "Label", () =>
            {
                var g = new GameObject("Label");
                g.transform.SetParent(btnGO.transform, false);
                var rt = g.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return g;
            });
            var lblTMP = lblGO.GetComponent<TextMeshProUGUI>();
            if (lblTMP == null) lblTMP = lblGO.AddComponent<TextMeshProUGUI>();
            lblTMP.text = "关闭引导";
            lblTMP.fontSize = 16f;
            lblTMP.alignment = TextAlignmentOptions.Center;
            lblTMP.enableAutoSizing = false;
            lblTMP.raycastTarget = false;
            SetTMPColor(lblTMP, Color.white);

            return btnGO;
        }

        // ==================== §17.15 BroadcasterTipBar ====================

        private static GameObject BuildBroadcasterTipBar(Transform parent)
        {
            var barGO = GetOrCreateChild(parent, "BroadcasterTipBar", () =>
            {
                var g = new GameObject("BroadcasterTipBar");
                g.transform.SetParent(parent, false);
                var rt = g.AddComponent<RectTransform>();
                // 顶部居中 720×40
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot     = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -4f);
                rt.sizeDelta = new Vector2(720f, 40f);
                return g;
            });

            var bg = barGO.GetComponent<Image>();
            if (bg == null) bg = barGO.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.55f);
            bg.raycastTarget = false;

            var textGO = GetOrCreateChild(barGO.transform, "TipText", () =>
            {
                var g = new GameObject("TipText");
                g.transform.SetParent(barGO.transform, false);
                var rt = g.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(12f, 0f);
                rt.offsetMax = new Vector2(-12f, 0f);
                return g;
            });
            var tipTMP = textGO.GetComponent<TextMeshProUGUI>();
            if (tipTMP == null) tipTMP = textGO.AddComponent<TextMeshProUGUI>();
            tipTMP.text = "欢迎来到极地生存法则！白天刷仙女棒帮矿工，夜晚刷甜甜圈保护城门！";
            tipTMP.fontSize = 18f;
            tipTMP.alignment = TextAlignmentOptions.MidlineLeft;
            tipTMP.enableAutoSizing = false;
            tipTMP.raycastTarget = false;
            SetTMPColor(tipTMP, Color.white);

            return barGO;
        }

        // ==================== 辅助方法 ====================

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
            // 排除未打开的 Prefab Asset 实例（hideFlags 区分）
            return go != null && (go.hideFlags & HideFlags.HideAndDontSave) != 0
                   && !go.scene.IsValid();
        }

        private static GameObject GetOrCreateChild(Transform parent, string name, System.Func<GameObject> creator)
        {
            var found = parent.Find(name);
            if (found != null) return found.gameObject;
            return creator();
        }

        /// <summary>写 TMP 颜色（必须同时写 m_fontColor + m_fontColor32，否则 faceColor 默认白）。</summary>
        private static void SetTMPColor(TextMeshProUGUI tmp, Color c)
        {
            var so = new SerializedObject(tmp);
            var p1 = so.FindProperty("m_fontColor");
            var p2 = so.FindProperty("m_fontColor32");
            if (p1 != null) p1.colorValue = c;
            if (p2 != null) p2.colorValue = c;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tmp);
        }

        // ==================== 内部数据结构 ====================

        private class CardViewData
        {
            public GameObject root;
            public Image bg;
            public TMP_Text iconText;
            public TMP_Text messageText;
            public Button jumpButton;
        }
    }
}
