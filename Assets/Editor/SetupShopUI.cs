using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.IO;
using DrscfZ.UI;

namespace DrscfZ.Editor
{
    /// <summary>
    /// Tools → DrscfZ → Setup Section 39 Shop UI
    ///
    /// 一键搭建 §39 商店系统 UI 骨架：
    ///
    ///   Canvas/ShopPanel                         (居中 800×600，默认隐藏，挂 ShopUI)
    ///     ├─ Header
    ///     │   ├─ TitleText
    ///     │   └─ BtnClose
    ///     ├─ BalanceBar (余额 / 终身 / 可消费)
    ///     ├─ TabRow (HLG)
    ///     │   ├─ TabA
    ///     │   ├─ TabB
    ///     │   └─ TabInventory
    ///     ├─ ScrollRect
    ///     │   └─ Viewport (Mask)
    ///     │       └─ Content (VLG)
    ///     └─ StatusText
    ///   Canvas/ShopConfirmPanel                  (居中 500×300，默认隐藏，挂 ShopConfirmDialogUI)
    ///     ├─ TitleText
    ///     ├─ ItemNameText
    ///     ├─ PriceText
    ///     ├─ CountdownText
    ///     ├─ BtnConfirm
    ///     └─ BtnCancel
    ///
    /// ShopItemButton Prefab：Assets/Prefabs/UI/ShopItemButton.prefab
    ///   Button + Image bg + 3 个 TMP_Text (Name / Price / Effect)
    /// </summary>
    public static class SetupShopUI
    {
        private const string AlibabaFontPath = "Assets/Resources/Fonts/AlibabaPuHuiTi-3-85-Bold SDF.asset";
        private const string ShopItemButtonPrefabPath = "Assets/Prefabs/UI/ShopItemButton.prefab";

        [MenuItem("Tools/DrscfZ/Setup Section 39 Shop UI")]
        public static void Execute()
        {
            var canvas = FindInLoadedScene("Canvas");
            if (canvas == null)
            {
                Debug.LogError("[SetupShopUI] Canvas 未找到，终止。");
                return;
            }

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AlibabaFontPath);
            if (font == null)
                Debug.LogWarning($"[SetupShopUI] 未能加载字体: {AlibabaFontPath}");

            // 1. 确保 ShopItemButton Prefab 存在
            var itemButtonPrefab = EnsureShopItemButtonPrefab(font);

            // 2. 搭建 ShopPanel
            BuildShopPanel(canvas.transform, font, itemButtonPrefab);

            // 3. 搭建 ShopConfirmPanel
            BuildShopConfirmPanel(canvas.transform, font);

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[SetupShopUI] §39 Shop UI setup done (ShopPanel + ShopConfirmPanel + ShopItemButton prefab)");
        }

        // ==================== ShopItemButton Prefab ====================

        private static GameObject EnsureShopItemButtonPrefab(TMP_FontAsset font)
        {
            // 若已存在则直接返回
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(ShopItemButtonPrefabPath);
            if (existing != null)
            {
                Debug.Log($"[SetupShopUI] ShopItemButton prefab 已存在：{ShopItemButtonPrefabPath}");
                return existing;
            }

            // 确保目录存在
            var dir = Path.GetDirectoryName(ShopItemButtonPrefabPath).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(dir))
            {
                // 按 "Assets/Prefabs/UI" 切分创建
                var parts = dir.Split('/');
                string cur = parts[0]; // "Assets"
                for (int i = 1; i < parts.Length; i++)
                {
                    var next = $"{cur}/{parts[i]}";
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(cur, parts[i]);
                    cur = next;
                }
            }

            // 临时 GameObject 作为 Prefab 源
            var go = new GameObject("ShopItemButton");
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(720f, 72f);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.18f, 0.18f, 0.24f, 0.9f);
            bg.raycastTarget = true;

            var btn = go.AddComponent<Button>();

            // Name (左侧)
            var nameTmp = CreateChildTMP(go.transform, "NameText",
                anchorMin: new Vector2(0f, 0f),
                anchorMax: new Vector2(0.5f, 1f),
                offsetMin: new Vector2(12f, 4f),
                offsetMax: new Vector2(0f, -4f),
                fontSize: 22f,
                align: TextAlignmentOptions.MidlineLeft,
                font: font,
                color: Color.white);
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.text = "商品名";

            // Price (中间)
            var priceTmp = CreateChildTMP(go.transform, "PriceText",
                anchorMin: new Vector2(0.5f, 0f),
                anchorMax: new Vector2(0.7f, 1f),
                offsetMin: new Vector2(0f, 4f),
                offsetMax: new Vector2(-4f, -4f),
                fontSize: 20f,
                align: TextAlignmentOptions.Center,
                font: font,
                color: new Color(1f, 0.84f, 0.2f, 1f));
            priceTmp.text = "0";

            // Effect (右侧)
            var effectTmp = CreateChildTMP(go.transform, "EffectText",
                anchorMin: new Vector2(0.7f, 0f),
                anchorMax: new Vector2(1f, 1f),
                offsetMin: new Vector2(0f, 4f),
                offsetMax: new Vector2(-12f, -4f),
                fontSize: 18f,
                align: TextAlignmentOptions.MidlineRight,
                font: font,
                color: new Color(0.8f, 0.95f, 1f, 1f));
            effectTmp.text = "效果描述";

            // 保存为 Prefab
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, ShopItemButtonPrefabPath);
            Object.DestroyImmediate(go);

            AssetDatabase.SaveAssets();
            Debug.Log($"[SetupShopUI] 已创建 Prefab: {ShopItemButtonPrefabPath}");
            return prefab;
        }

        // ==================== ShopPanel ====================

        private static void BuildShopPanel(Transform canvasRoot, TMP_FontAsset font, GameObject itemButtonPrefab)
        {
            var panel = BuildPanel(canvasRoot, "ShopPanel",
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                pivot: new Vector2(0.5f, 0.5f),
                anchoredPosition: Vector2.zero,
                sizeDelta: new Vector2(800f, 600f));

            var bg = panel.GetComponent<Image>();
            if (bg == null) bg = panel.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.09f, 0.14f, 0.95f);
            bg.raycastTarget = true;

            // ===== Header =====
            var header = BuildPanel(panel.transform, "Header",
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(1f, 1f),
                pivot: new Vector2(0.5f, 1f),
                anchoredPosition: Vector2.zero,
                sizeDelta: new Vector2(0f, 60f));

            var headerBg = header.GetComponent<Image>();
            if (headerBg == null) headerBg = header.AddComponent<Image>();
            headerBg.color = new Color(0.15f, 0.18f, 0.28f, 1f);
            headerBg.raycastTarget = false;

            var titleTmp = CreateChildTMP(header.transform, "TitleText",
                anchorMin: new Vector2(0f, 0f),
                anchorMax: new Vector2(0.85f, 1f),
                offsetMin: new Vector2(16f, 0f),
                offsetMax: new Vector2(0f, 0f),
                fontSize: 28f,
                align: TextAlignmentOptions.MidlineLeft,
                font: font,
                color: new Color(1f, 0.84f, 0.2f, 1f));
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.text = "商店";

            var btnClose = BuildButton(header.transform, "BtnClose",
                anchorMin: new Vector2(1f, 0f),
                anchorMax: new Vector2(1f, 1f),
                pivot: new Vector2(1f, 0.5f),
                anchoredPosition: new Vector2(-12f, 0f),
                sizeDelta: new Vector2(50f, 44f),
                labelText: "×",
                bgColor: new Color(0.6f, 0.3f, 0.3f, 1f),
                labelColor: Color.white,
                fontSize: 28f,
                font: font);

            // ===== BalanceBar =====
            var balanceBar = BuildPanel(panel.transform, "BalanceBar",
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(1f, 1f),
                pivot: new Vector2(0.5f, 1f),
                anchoredPosition: new Vector2(0f, -60f),
                sizeDelta: new Vector2(0f, 40f));

            var balanceBg = balanceBar.GetComponent<Image>();
            if (balanceBg == null) balanceBg = balanceBar.AddComponent<Image>();
            balanceBg.color = new Color(0.12f, 0.14f, 0.22f, 1f);
            balanceBg.raycastTarget = false;

            var balanceTmp = CreateChildTMP(balanceBar.transform, "BalanceText",
                anchorMin: Vector2.zero,
                anchorMax: Vector2.one,
                offsetMin: new Vector2(16f, 0f),
                offsetMax: new Vector2(-16f, 0f),
                fontSize: 18f,
                align: TextAlignmentOptions.Center,
                font: font,
                color: new Color(0.85f, 0.95f, 1f, 1f));
            balanceTmp.text = "贡献 0  可消费 0  终身 0";

            // ===== TabRow =====
            var tabRow = BuildPanel(panel.transform, "TabRow",
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(1f, 1f),
                pivot: new Vector2(0.5f, 1f),
                anchoredPosition: new Vector2(0f, -100f),
                sizeDelta: new Vector2(0f, 50f));

            var tabHlg = tabRow.GetComponent<HorizontalLayoutGroup>();
            if (tabHlg == null) tabHlg = tabRow.AddComponent<HorizontalLayoutGroup>();
            tabHlg.childAlignment         = TextAnchor.MiddleCenter;
            tabHlg.spacing                = 8f;
            tabHlg.childForceExpandWidth  = true;
            tabHlg.childForceExpandHeight = true;
            tabHlg.childControlWidth      = true;
            tabHlg.childControlHeight     = true;
            tabHlg.padding = new RectOffset(16, 16, 4, 4);

            var tabA = BuildButton(tabRow.transform, "TabA",
                anchorMin: Vector2.zero, anchorMax: Vector2.one,
                pivot: new Vector2(0.5f, 0.5f),
                anchoredPosition: Vector2.zero, sizeDelta: Vector2.zero,
                labelText: "A 类道具",
                bgColor: new Color(0.25f, 0.4f, 0.6f, 1f),
                labelColor: Color.white,
                fontSize: 20f,
                font: font);

            var tabB = BuildButton(tabRow.transform, "TabB",
                anchorMin: Vector2.zero, anchorMax: Vector2.one,
                pivot: new Vector2(0.5f, 0.5f),
                anchoredPosition: Vector2.zero, sizeDelta: Vector2.zero,
                labelText: "B 类装备",
                bgColor: new Color(0.5f, 0.3f, 0.5f, 1f),
                labelColor: Color.white,
                fontSize: 20f,
                font: font);

            var tabInventory = BuildButton(tabRow.transform, "TabInventory",
                anchorMin: Vector2.zero, anchorMax: Vector2.one,
                pivot: new Vector2(0.5f, 0.5f),
                anchoredPosition: Vector2.zero, sizeDelta: Vector2.zero,
                labelText: "我的背包",
                bgColor: new Color(0.3f, 0.5f, 0.3f, 1f),
                labelColor: Color.white,
                fontSize: 20f,
                font: font);

            // ===== ScrollRect + Viewport + Content =====
            var scrollRoot = BuildPanel(panel.transform, "ScrollRoot",
                anchorMin: new Vector2(0f, 0f),
                anchorMax: new Vector2(1f, 1f),
                pivot: new Vector2(0.5f, 0.5f),
                anchoredPosition: Vector2.zero,
                sizeDelta: Vector2.zero);
            // 覆盖 offset：上 150 (header60 + balance40 + tab50)，下 40 (status)
            var scrollRt = scrollRoot.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0f, 0f);
            scrollRt.anchorMax = new Vector2(1f, 1f);
            scrollRt.offsetMin = new Vector2(0f, 40f);
            scrollRt.offsetMax = new Vector2(0f, -150f);

            var scroll = scrollRoot.GetComponent<ScrollRect>();
            if (scroll == null) scroll = scrollRoot.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical   = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            // Viewport
            var viewport = GetOrCreateChild(scrollRoot.transform, "Viewport", () =>
            {
                var g  = new GameObject("Viewport");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(scrollRoot.transform, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return g;
            });
            var viewRt = viewport.GetComponent<RectTransform>();
            viewRt.anchorMin = Vector2.zero;
            viewRt.anchorMax = Vector2.one;
            viewRt.offsetMin = Vector2.zero;
            viewRt.offsetMax = Vector2.zero;
            var viewImg = viewport.GetComponent<Image>();
            if (viewImg == null) viewImg = viewport.AddComponent<Image>();
            viewImg.color = new Color(1f, 1f, 1f, 0.01f); // Mask 需要 Image（最小 alpha 避免遮挡）
            var mask = viewport.GetComponent<Mask>();
            if (mask == null) mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Content (VLG)
            var content = GetOrCreateChild(viewport.transform, "Content", () =>
            {
                var g  = new GameObject("Content");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(viewport.transform, false);
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot     = new Vector2(0.5f, 1f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(0f, 400f);
                return g;
            });
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot     = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 400f);

            var contentVlg = content.GetComponent<VerticalLayoutGroup>();
            if (contentVlg == null) contentVlg = content.AddComponent<VerticalLayoutGroup>();
            contentVlg.childAlignment         = TextAnchor.UpperCenter;
            contentVlg.spacing                = 8f;
            contentVlg.childForceExpandWidth  = true;
            contentVlg.childForceExpandHeight = false;
            contentVlg.childControlWidth      = true;
            contentVlg.childControlHeight     = false;
            contentVlg.padding = new RectOffset(16, 16, 12, 12);

            var csf = content.GetComponent<ContentSizeFitter>();
            if (csf == null) csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // 绑 ScrollRect
            scroll.viewport = viewRt;
            scroll.content  = contentRt;

            // ===== StatusText =====
            var statusBar = BuildPanel(panel.transform, "StatusBar",
                anchorMin: new Vector2(0f, 0f),
                anchorMax: new Vector2(1f, 0f),
                pivot: new Vector2(0.5f, 0f),
                anchoredPosition: Vector2.zero,
                sizeDelta: new Vector2(0f, 40f));
            var statusBarBg = statusBar.GetComponent<Image>();
            if (statusBarBg == null) statusBarBg = statusBar.AddComponent<Image>();
            statusBarBg.color = new Color(0.12f, 0.14f, 0.22f, 1f);
            statusBarBg.raycastTarget = false;

            var statusTmp = CreateChildTMP(statusBar.transform, "StatusText",
                anchorMin: Vector2.zero,
                anchorMax: Vector2.one,
                offsetMin: new Vector2(16f, 0f),
                offsetMax: new Vector2(-16f, 0f),
                fontSize: 18f,
                align: TextAlignmentOptions.MidlineLeft,
                font: font,
                color: new Color(0.9f, 0.9f, 0.9f, 1f));
            statusTmp.text = "";

            // ===== ShopUI 挂 + 绑字段 =====
            var ui = panel.GetComponent<ShopUI>();
            if (ui == null) ui = panel.AddComponent<ShopUI>();

            var so = new SerializedObject(ui);
            TryBind(so, "_panel", panel);
            TryBind(so, "_tabA", tabA);
            TryBind(so, "_tabB", tabB);
            TryBind(so, "_tabInventory", tabInventory);
            TryBind(so, "_btnClose", btnClose);
            TryBind(so, "_contentRoot", contentRt);
            TryBind(so, "_itemButtonPrefab", itemButtonPrefab);
            TryBind(so, "_titleText", titleTmp);
            TryBind(so, "_statusText", statusTmp);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);

            // 默认隐藏
            panel.SetActive(false);

            Debug.Log("[SetupShopUI] ShopPanel done (Header + Balance + Tabs + Scroll + StatusText + ShopUI bound)");
        }

        // ==================== ShopConfirmPanel ====================

        private static void BuildShopConfirmPanel(Transform canvasRoot, TMP_FontAsset font)
        {
            var panel = BuildPanel(canvasRoot, "ShopConfirmPanel",
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                pivot: new Vector2(0.5f, 0.5f),
                anchoredPosition: Vector2.zero,
                sizeDelta: new Vector2(500f, 300f));

            var bg = panel.GetComponent<Image>();
            if (bg == null) bg = panel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.16f, 0.95f);
            bg.raycastTarget = true;

            // TitleText
            var titleTmp = CreateChildTMP(panel.transform, "TitleText",
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(1f, 1f),
                offsetMin: new Vector2(16f, -70f),
                offsetMax: new Vector2(-16f, -20f),
                fontSize: 28f,
                align: TextAlignmentOptions.Center,
                font: font,
                color: new Color(1f, 0.84f, 0.2f, 1f));
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.text = "确认购买？";

            // ItemNameText（预留扩展；脚本未定义此字段时会 LogWarning）
            var itemNameTmp = CreateChildTMP(panel.transform, "ItemNameText",
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(1f, 1f),
                offsetMin: new Vector2(16f, -110f),
                offsetMax: new Vector2(-16f, -75f),
                fontSize: 22f,
                align: TextAlignmentOptions.Center,
                font: font,
                color: Color.white);
            itemNameTmp.text = "商品名";

            // PriceText
            var priceTmp = CreateChildTMP(panel.transform, "PriceText",
                anchorMin: new Vector2(0f, 0.5f),
                anchorMax: new Vector2(1f, 0.5f),
                offsetMin: new Vector2(16f, -10f),
                offsetMax: new Vector2(-16f, 30f),
                fontSize: 24f,
                align: TextAlignmentOptions.Center,
                font: font,
                color: new Color(1f, 0.84f, 0.2f, 1f));
            priceTmp.text = "价格：0";

            // CountdownText
            var cdTmp = CreateChildTMP(panel.transform, "CountdownText",
                anchorMin: new Vector2(0f, 0f),
                anchorMax: new Vector2(1f, 0f),
                offsetMin: new Vector2(16f, 80f),
                offsetMax: new Vector2(-16f, 110f),
                fontSize: 22f,
                align: TextAlignmentOptions.Center,
                font: font,
                color: new Color(1f, 0.5f, 0.5f, 1f));
            cdTmp.text = "5s";

            // BtnConfirm (左下)
            var btnConfirm = BuildButton(panel.transform, "BtnConfirm",
                anchorMin: new Vector2(0.1f, 0f),
                anchorMax: new Vector2(0.45f, 0f),
                pivot: new Vector2(0.5f, 0f),
                anchoredPosition: new Vector2(0f, 20f),
                sizeDelta: new Vector2(0f, 56f),
                labelText: "确认",
                bgColor: new Color(0.3f, 0.75f, 0.35f, 1f),
                labelColor: Color.white,
                fontSize: 24f,
                font: font);

            // BtnCancel (右下)
            var btnCancel = BuildButton(panel.transform, "BtnCancel",
                anchorMin: new Vector2(0.55f, 0f),
                anchorMax: new Vector2(0.9f, 0f),
                pivot: new Vector2(0.5f, 0f),
                anchoredPosition: new Vector2(0f, 20f),
                sizeDelta: new Vector2(0f, 56f),
                labelText: "取消",
                bgColor: new Color(0.6f, 0.3f, 0.3f, 1f),
                labelColor: Color.white,
                fontSize: 24f,
                font: font);

            var ui = panel.GetComponent<ShopConfirmDialogUI>();
            if (ui == null) ui = panel.AddComponent<ShopConfirmDialogUI>();

            var so = new SerializedObject(ui);
            TryBind(so, "_panel", panel);
            TryBind(so, "_titleText", titleTmp);
            TryBind(so, "_priceText", priceTmp);
            TryBind(so, "_timerText", cdTmp);
            TryBind(so, "_btnConfirm", btnConfirm);
            TryBind(so, "_btnCancel", btnCancel);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);

            panel.SetActive(false);

            Debug.Log("[SetupShopUI] ShopConfirmPanel done (Title/ItemName/Price/CD + Confirm/Cancel)");
        }

        // ==================== 辅助方法 ====================

        private static Button BuildButton(Transform parent, string name,
                                          Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
                                          Vector2 anchoredPosition, Vector2 sizeDelta,
                                          string labelText, Color bgColor, Color labelColor,
                                          float fontSize, TMP_FontAsset font)
        {
            var go = GetOrCreateChild(parent, name, () =>
            {
                var g  = new GameObject(name);
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(parent, false);
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.pivot     = pivot;
                rt.anchoredPosition = anchoredPosition;
                rt.sizeDelta = sizeDelta;
                return g;
            });

            var rt2 = go.GetComponent<RectTransform>();
            if (rt2 != null)
            {
                rt2.anchorMin = anchorMin;
                rt2.anchorMax = anchorMax;
                rt2.pivot     = pivot;
                rt2.anchoredPosition = anchoredPosition;
                rt2.sizeDelta = sizeDelta;
            }

            var img = go.GetComponent<Image>();
            if (img == null) img = go.AddComponent<Image>();
            img.color = bgColor;
            img.raycastTarget = true;

            var btn = go.GetComponent<Button>();
            if (btn == null) btn = go.AddComponent<Button>();

            var labelTmp = CreateChildTMP(go.transform, "Label",
                anchorMin: Vector2.zero,
                anchorMax: Vector2.one,
                offsetMin: new Vector2(4f, 4f),
                offsetMax: new Vector2(-4f, -4f),
                fontSize: fontSize,
                align: TextAlignmentOptions.Center,
                font: font,
                color: labelColor);
            labelTmp.fontStyle = FontStyles.Bold;
            labelTmp.text = labelText;

            return btn;
        }

        private static GameObject BuildPanel(Transform parent, string name,
                                             Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
                                             Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var go = GetOrCreateChild(parent, name, () =>
            {
                var g  = new GameObject(name);
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(parent, false);
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.pivot     = pivot;
                rt.anchoredPosition = anchoredPosition;
                rt.sizeDelta = sizeDelta;
                return g;
            });

            var rt2 = go.GetComponent<RectTransform>();
            if (rt2 != null)
            {
                rt2.anchorMin = anchorMin;
                rt2.anchorMax = anchorMax;
                rt2.pivot     = pivot;
                rt2.anchoredPosition = anchoredPosition;
                rt2.sizeDelta = sizeDelta;
            }
            return go;
        }

        private static TextMeshProUGUI CreateChildTMP(Transform parent, string name,
                                                      Vector2 anchorMin, Vector2 anchorMax,
                                                      Vector2 offsetMin, Vector2 offsetMax,
                                                      float fontSize, TextAlignmentOptions align,
                                                      TMP_FontAsset font, Color color)
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
            if (rt2 != null)
            {
                rt2.anchorMin = anchorMin;
                rt2.anchorMax = anchorMax;
                rt2.offsetMin = offsetMin;
                rt2.offsetMax = offsetMax;
            }

            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize         = fontSize;
            tmp.alignment        = align;
            tmp.enableAutoSizing = false;
            tmp.raycastTarget    = false;
            if (font != null)
            {
                var soTmp = new SerializedObject(tmp);
                var fontProp = soTmp.FindProperty("m_fontAsset");
                if (fontProp != null) fontProp.objectReferenceValue = font;
                soTmp.ApplyModifiedPropertiesWithoutUndo();
            }
            tmp.text = "";
            SetTMPColor(tmp, color);
            return tmp;
        }

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
                Debug.LogWarning($"[SetupShopUI] TryBind: 字段 {fieldName} 未找到");
                return;
            }
            p.objectReferenceValue = value;
        }

        private static GameObject GetOrCreateChild(Transform parent, string name, System.Func<GameObject> creator)
        {
            if (parent == null) return creator();
            var found = parent.Find(name);
            if (found != null) return found.gameObject;
            return creator();
        }

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
