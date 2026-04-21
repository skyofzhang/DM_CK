using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Reflection;
using DrscfZ.Survival;
using DrscfZ.UI;

/// <summary>
/// Phase 2 一键场景搭建工具
/// 执行后创建：WorkerPool（20个Worker）、Gift_Canvas、Broadcaster_Canvas
/// </summary>
public class SetupPhase2Scene
{
    [MenuItem("Tools/Phase2/Setup Scene (WorkerPool + Gift_Canvas + Broadcaster)")]
    public static void Execute()
    {
        SetupWorkerPool();
        SetupGiftCanvas();
        SetupBroadcasterCanvas();
        SaveScene();
        Debug.Log("[SetupPhase2Scene] ✅ All Phase2 scene objects created and wired.");
    }

    // ==================================================================
    // 1. Worker Pool
    // ==================================================================
    static void SetupWorkerPool()
    {
        // 检查是否已存在
        var existing = GameObject.Find("WorkerPool");
        if (existing != null)
        {
            Debug.Log("[SetupPhase2Scene] WorkerPool already exists, skipping.");
            return;
        }

        // 创建 WorkerPool 父对象（放在场景根）
        var poolRoot = new GameObject("WorkerPool");
        Undo.RegisterCreatedObjectUndo(poolRoot, "Create WorkerPool");
        poolRoot.transform.position = new Vector3(0, 0, 30); // 场景外等待区

        var workerControllers = new WorkerController[20];

        for (int i = 0; i < 20; i++)
        {
            // Capsule作为Worker本体
            var worker = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            worker.name = $"Worker_{i:00}";
            worker.transform.SetParent(poolRoot.transform);
            worker.transform.localPosition = new Vector3((i % 5) * 2f, 0f, (i / 5) * 2f);
            worker.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            // 设置白色材质
            var renderer = worker.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = Color.white;
                renderer.sharedMaterial = mat;
            }

            // 去掉CapsuleCollider（避免物理干扰）
            var col = worker.GetComponent<CapsuleCollider>();
            if (col != null) Object.DestroyImmediate(col);

            // 添加 WorkerVisual（先于 WorkerController，因为Controller会GetComponent）
            var visual = worker.AddComponent<WorkerVisual>();

            // 创建 BubbleCanvas 子对象
            var bubbleGO = new GameObject("BubbleCanvas");
            bubbleGO.transform.SetParent(worker.transform);
            bubbleGO.transform.localPosition = new Vector3(0f, 2.5f, 0f); // 头顶
            bubbleGO.transform.localRotation = Quaternion.identity;
            bubbleGO.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            // World Space Canvas
            var canvas = bubbleGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRT = bubbleGO.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(120f, 120f);

            // 气泡背景 Image
            var bgGO = new GameObject("BubbleBg");
            bgGO.transform.SetParent(bubbleGO.transform);
            var bgRT = bgGO.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            var bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.267f, 0.667f, 1.0f, 0.85f);

            // 气泡图标 TMP_Text
            var iconGO = new GameObject("BubbleIcon");
            iconGO.transform.SetParent(bubbleGO.transform);
            var iconRT = iconGO.AddComponent<RectTransform>();
            iconRT.anchorMin = Vector2.zero;
            iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = Vector2.zero;
            iconRT.offsetMax = Vector2.zero;
            var iconText = iconGO.AddComponent<TextMeshProUGUI>();
            iconText.text = "🐟";
            iconText.fontSize = 60f;
            iconText.alignment = TextAlignmentOptions.Center;

            // WorkerBubble 挂在 BubbleCanvas
            var bubble = bubbleGO.AddComponent<WorkerBubble>();
            // 通过SerializedObject注入私有字段
            var so = new SerializedObject(bubble);
            so.FindProperty("_iconText").objectReferenceValue = iconText;
            so.FindProperty("_bgImage").objectReferenceValue = bgImage;
            so.ApplyModifiedProperties();

            // WorkerController 挂在 Worker 本体
            var ctrl = worker.AddComponent<WorkerController>();
            // 注入 visual 和 bubble 引用
            var ctrlSO = new SerializedObject(ctrl);
            ctrlSO.FindProperty("_visual").objectReferenceValue = visual;
            ctrlSO.FindProperty("_bubble").objectReferenceValue = bubble;
            ctrlSO.ApplyModifiedProperties();

            worker.SetActive(false); // 初始隐藏
            workerControllers[i] = ctrl;

            EditorUtility.SetDirty(worker);
        }

        // 注入 WorkerManager._preCreatedWorkers
        var wm = Object.FindObjectOfType<WorkerManager>();
        if (wm != null)
        {
            var wmSO = new SerializedObject(wm);
            var arr = wmSO.FindProperty("_preCreatedWorkers");
            arr.arraySize = 20;
            for (int i = 0; i < 20; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = workerControllers[i];
            wmSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(wm);
            Debug.Log("[SetupPhase2Scene] WorkerManager._preCreatedWorkers wired (20 workers).");
        }
        else
        {
            Debug.LogWarning("[SetupPhase2Scene] WorkerManager not found in scene — wire _preCreatedWorkers manually.");
        }

        Debug.Log("[SetupPhase2Scene] ✅ WorkerPool created with 20 Workers.");
    }

    // ==================================================================
    // 2. Gift_Canvas
    // ==================================================================
    static void SetupGiftCanvas()
    {
        if (GameObject.Find("Gift_Canvas") != null)
        {
            Debug.Log("[SetupPhase2Scene] Gift_Canvas already exists, skipping.");
            return;
        }

        // 找到主Canvas作为父对象，或在根创建
        var mainCanvas = GameObject.Find("Canvas");
        Transform canvasParent = mainCanvas != null ? mainCanvas.transform.parent : null;

        var giftCanvasGO = new GameObject("Gift_Canvas");
        Undo.RegisterCreatedObjectUndo(giftCanvasGO, "Create Gift_Canvas");
        if (canvasParent != null)
            giftCanvasGO.transform.SetParent(canvasParent);

        var giftCanvas = giftCanvasGO.AddComponent<Canvas>();
        giftCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        giftCanvas.sortingOrder = 100;
        giftCanvasGO.AddComponent<CanvasScaler>();
        giftCanvasGO.AddComponent<GraphicRaycaster>();

        // ——— T1_StarParticle ———
        var t1 = new GameObject("T1_StarParticle");
        t1.transform.SetParent(giftCanvasGO.transform);
        t1.AddComponent<ParticleSystem>();
        CenterFull(t1);

        // ——— T2_BorderEffect ———
        var t2 = CreatePanel(giftCanvasGO.transform, "T2_BorderEffect");
        CreateParticleChild(t2.transform, "TopLeft_PS");
        CreateParticleChild(t2.transform, "TopRight_PS");
        CreateParticleChild(t2.transform, "BotLeft_PS");
        CreateParticleChild(t2.transform, "BotRight_PS");
        CreateImageChild(t2.transform, "CenterRing_Image", new Color(1f, 1f, 1f, 0f));
        t2.SetActive(false);

        // ——— T3_GiftBounce ———
        var t3 = CreatePanel(giftCanvasGO.transform, "T3_GiftBounce");
        CreateImageChild(t3.transform, "GiftIcon_Image", Color.white);
        CreateParticleChild(t3.transform, "Explode_PS");
        t3.SetActive(false);

        // ——— T4_FullscreenGlow ———
        var t4 = CreatePanel(giftCanvasGO.transform, "T4_FullscreenGlow");
        var orangeOverlay = CreateImageChild(t4.transform, "OrangeOverlay", new Color(1f, 0.408f, 0f, 0f));
        FullScreen(orangeOverlay);
        CreateImageChild(t4.transform, "BatteryIcon", Color.white);
        // ChargingSlider
        var sliderGO = new GameObject("ChargingSlider");
        sliderGO.transform.SetParent(t4.transform);
        sliderGO.AddComponent<RectTransform>();
        sliderGO.AddComponent<Slider>();
        t4.SetActive(false);

        // ——— T5_EpicAirdrop ———
        var t5 = CreatePanel(giftCanvasGO.transform, "T5_EpicAirdrop");
        var blackOverlay = CreateImageChild(t5.transform, "BlackOverlay", new Color(0f, 0f, 0f, 0f));
        FullScreen(blackOverlay);
        CreateImageChild(t5.transform, "AirdropBox", Color.white);
        CreateParticleChild(t5.transform, "Fireworks_PS");
        // ResourceIcons (4 images)
        var iconsParent = new GameObject("ResourceIcons");
        iconsParent.transform.SetParent(t5.transform);
        iconsParent.AddComponent<RectTransform>();
        for (int i = 0; i < 4; i++)
        {
            string[] names = { "FoodIcon", "CoalIcon", "OreIcon", "ShieldIcon" };
            CreateImageChild(iconsParent.transform, names[i], Color.white);
        }
        // PlayerNameText (TMP)
        var playerNameGO = new GameObject("PlayerNameText");
        playerNameGO.transform.SetParent(t5.transform);
        var playerNameRT = playerNameGO.AddComponent<RectTransform>();
        CenterRectTransform(playerNameRT, new Vector2(900, 120), new Vector2(0, 0));
        var playerNameTMP = playerNameGO.AddComponent<TextMeshProUGUI>();
        playerNameTMP.text = "玩家名 拯救了村庄！";
        playerNameTMP.fontSize = 60f;
        playerNameTMP.alignment = TextAlignmentOptions.Center;
        playerNameTMP.color = Color.white;
        t5.SetActive(false);

        // ——— GiftBannerQueue ———
        var bannerQueue = new GameObject("GiftBannerQueue");
        bannerQueue.transform.SetParent(giftCanvasGO.transform);
        var bqRT = bannerQueue.AddComponent<RectTransform>();
        // 屏幕左侧居中，从上往下3条横幅
        bqRT.anchorMin = new Vector2(0f, 0.5f);
        bqRT.anchorMax = new Vector2(0f, 0.5f);
        bqRT.anchoredPosition = new Vector2(10f, 0f);
        bqRT.sizeDelta = new Vector2(350f, 280f);

        for (int i = 0; i < 3; i++)
        {
            var slot = CreatePanel(bannerQueue.transform, $"BannerSlot_{i}");
            var slotRT = slot.GetComponent<RectTransform>();
            slotRT.anchorMin = new Vector2(0f, 1f);
            slotRT.anchorMax = new Vector2(1f, 1f);
            slotRT.pivot = new Vector2(0f, 1f);
            slotRT.anchoredPosition = new Vector2(0f, -i * 90f);
            slotRT.sizeDelta = new Vector2(0f, 80f);
            // BannerBg
            var bg = CreateImageChild(slot.transform, "BannerBg", new Color(0f, 0f, 0f, 0.75f));
            FullScreen(bg);
            // BannerText
            var bannerTextGO = new GameObject("BannerText");
            bannerTextGO.transform.SetParent(slot.transform);
            var bRT = bannerTextGO.AddComponent<RectTransform>();
            bRT.anchorMin = Vector2.zero; bRT.anchorMax = Vector2.one;
            bRT.offsetMin = new Vector2(10, 5); bRT.offsetMax = new Vector2(-10, -5);
            var bTMP = bannerTextGO.AddComponent<TextMeshProUGUI>();
            bTMP.text = "[昵称] 送出了 [礼物]";
            bTMP.fontSize = 24f;
            bTMP.alignment = TextAlignmentOptions.MidlineLeft;
            slot.SetActive(false);
        }

        // GiftNotificationUI has been removed; skipping component add.
        // var gnUI = giftCanvasGO.AddComponent<GiftNotificationUI>();
        EditorUtility.SetDirty(giftCanvasGO);

        Debug.Log("[SetupPhase2Scene] ✅ Gift_Canvas hierarchy created. Wire [SerializeField] refs in Inspector.");
    }

    // ==================================================================
    // 3. Broadcaster_Canvas
    // ==================================================================
    static void SetupBroadcasterCanvas()
    {
        if (GameObject.Find("Broadcaster_Canvas") != null)
        {
            Debug.Log("[SetupPhase2Scene] Broadcaster_Canvas already exists, skipping.");
            return;
        }

        var bcGO = new GameObject("Broadcaster_Canvas");
        Undo.RegisterCreatedObjectUndo(bcGO, "Create Broadcaster_Canvas");

        var bc = bcGO.AddComponent<Canvas>();
        bc.renderMode = RenderMode.ScreenSpaceOverlay;
        bc.sortingOrder = 90;
        bcGO.AddComponent<CanvasScaler>();
        bcGO.AddComponent<GraphicRaycaster>();

        // BroadcasterPanel_Root (always-active controller parent per Rule 7)
        var controllerGO = new GameObject("BroadcasterPanelController");
        controllerGO.transform.SetParent(bcGO.transform);
        controllerGO.AddComponent<RectTransform>();
        // Mount BroadcasterPanel script on always-active parent
        controllerGO.AddComponent<BroadcasterPanel>();

        // PanelRoot (the actual panel, hidden by default)
        var panelRoot = new GameObject("PanelRoot");
        panelRoot.transform.SetParent(controllerGO.transform);
        var prRT = panelRoot.AddComponent<RectTransform>();
        // 右侧居中，200×280px
        prRT.anchorMin = new Vector2(1f, 0.5f);
        prRT.anchorMax = new Vector2(1f, 0.5f);
        prRT.pivot = new Vector2(1f, 0.5f);
        prRT.anchoredPosition = new Vector2(-10f, 0f);
        prRT.sizeDelta = new Vector2(200f, 280f);
        var panelBg = panelRoot.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.6f);
        panelRoot.SetActive(false); // hidden by default

        // BoostButton (紧急加速)
        var boostBtn = CreateCircleButton(panelRoot.transform, "BoostButton", "加", new Vector2(0f, 60f));

        // EventButton (触发事件)
        var eventBtn = CreateCircleButton(panelRoot.transform, "EventButton", "浪", new Vector2(0f, -60f));

        // Wire BroadcasterPanel SerializedFields
        var bp = controllerGO.GetComponent<BroadcasterPanel>();
        var bpSO = new SerializedObject(bp);
        bpSO.FindProperty("_panelRoot").objectReferenceValue = panelRoot;

        // Try to find Button/Image/Text refs
        var boostBtnComp = boostBtn.GetComponent<Button>();
        var boostBtnBg = boostBtn.GetComponent<Image>();
        var boostCdText = boostBtn.transform.Find("CdText")?.GetComponent<TMP_Text>();
        var eventBtnComp = eventBtn.GetComponent<Button>();
        var eventBtnBg = eventBtn.GetComponent<Image>();
        var eventCdText = eventBtn.transform.Find("CdText")?.GetComponent<TMP_Text>();

        if (bpSO.FindProperty("_boostBtn") != null)
            bpSO.FindProperty("_boostBtn").objectReferenceValue = boostBtnComp;
        if (bpSO.FindProperty("_boostBtnBg") != null)
            bpSO.FindProperty("_boostBtnBg").objectReferenceValue = boostBtnBg;
        if (bpSO.FindProperty("_boostCdText") != null)
            bpSO.FindProperty("_boostCdText").objectReferenceValue = boostCdText;
        if (bpSO.FindProperty("_eventBtn") != null)
            bpSO.FindProperty("_eventBtn").objectReferenceValue = eventBtnComp;
        if (bpSO.FindProperty("_eventBtnBg") != null)
            bpSO.FindProperty("_eventBtnBg").objectReferenceValue = eventBtnBg;
        if (bpSO.FindProperty("_eventCdText") != null)
            bpSO.FindProperty("_eventCdText").objectReferenceValue = eventCdText;

        bpSO.ApplyModifiedProperties();
        EditorUtility.SetDirty(bcGO);

        Debug.Log("[SetupPhase2Scene] ✅ Broadcaster_Canvas created with BoostButton + EventButton.");
    }

    // ==================================================================
    // Helpers
    // ==================================================================

    static GameObject CreatePanel(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = new Color(0, 0, 0, 0); // transparent panel
        return go;
    }

    static GameObject CreateParticleChild(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = Vector3.zero;
        go.AddComponent<ParticleSystem>();
        return go;
    }

    static GameObject CreateImageChild(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(200f, 200f);
        go.AddComponent<Image>().color = color;
        return go;
    }

    static void FullScreen(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void CenterFull(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(100f, 100f);
    }

    static void CenterRectTransform(RectTransform rt, Vector2 size, Vector2 pos)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static GameObject CreateCircleButton(Transform parent, string name, string icon, Vector2 pos)
    {
        var btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent);
        var rt = btnGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(120f, 120f);
        var img = btnGO.AddComponent<Image>();
        img.color = new Color(0.2f, 0.6f, 1f, 0.9f);
        btnGO.AddComponent<Button>();

        // Icon label
        var iconGO = new GameObject("IconText");
        iconGO.transform.SetParent(btnGO.transform);
        var iRT = iconGO.AddComponent<RectTransform>();
        iRT.anchorMin = new Vector2(0.5f, 0.6f);
        iRT.anchorMax = new Vector2(0.5f, 1f);
        iRT.offsetMin = Vector2.zero; iRT.offsetMax = Vector2.zero;
        var iTMP = iconGO.AddComponent<TextMeshProUGUI>();
        iTMP.text = icon;
        iTMP.fontSize = 42f;
        iTMP.alignment = TextAlignmentOptions.Center;

        // CD countdown text
        var cdGO = new GameObject("CdText");
        cdGO.transform.SetParent(btnGO.transform);
        var cdRT = cdGO.AddComponent<RectTransform>();
        cdRT.anchorMin = new Vector2(0f, 0f);
        cdRT.anchorMax = new Vector2(1f, 0.45f);
        cdRT.offsetMin = Vector2.zero; cdRT.offsetMax = Vector2.zero;
        var cdTMP = cdGO.AddComponent<TextMeshProUGUI>();
        cdTMP.text = "";
        cdTMP.fontSize = 22f;
        cdTMP.alignment = TextAlignmentOptions.Center;
        cdTMP.color = Color.white;

        return btnGO;
    }

    static void SaveScene()
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        // Use SaveCurrentScene tool
        var saveType = System.Type.GetType("SaveCurrentScene");
        if (saveType != null)
        {
            var method = saveType.GetMethod("Save", BindingFlags.Static | BindingFlags.Public);
            method?.Invoke(null, null);
        }
        else
        {
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
        Debug.Log("[SetupPhase2Scene] Scene saved.");
    }
}
