using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 为 SurvivalSettingsPanel 应用冬日主题 UI 资源：
/// 面板底图 / 关闭按钮 / 滑条轨道+填充+滑块 / Toggle开关
/// </summary>
public static class SetupSettingsPanel
{
    [MenuItem("Tools/DrscfZ/Setup Settings Panel")]
    public static void Execute()
    {
        const string root = "Assets/Art/UI/Settings/";

        // ── 1. 导入资源为 Sprite ────────────────────────────────────
        string[] paths = {
            root + "panel_settings_bg.png",
            root + "btn_close.png",
            root + "slider_track_bg.png",
            root + "slider_fill.png",
            root + "slider_handle.png",
            root + "toggle_on.png",
            root + "toggle_off.png",
        };
        foreach (var p in paths) EnsureSprite(p);
        AssetDatabase.Refresh();

        var panelBg      = Load(root + "panel_settings_bg.png");
        var closeBg      = Load(root + "btn_close.png");
        var trackBg      = Load(root + "slider_track_bg.png");
        var fillSpr      = Load(root + "slider_fill.png");
        var handleSpr    = Load(root + "slider_handle.png");
        var toggleOn     = Load(root + "toggle_on.png");
        var toggleOff    = Load(root + "toggle_off.png");

        if (panelBg == null || closeBg == null || trackBg == null ||
            fillSpr == null || handleSpr == null || toggleOn == null || toggleOff == null)
        { Debug.LogError("[SetupSettingsPanel] 部分资源加载失败，请检查 Assets/Art/UI/Settings/"); return; }

        // ── 2. 找 SurvivalSettingsPanel ──────────────────────────────
        GameObject panel = null;
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go.name == "SurvivalSettingsPanel" && go.scene.name == "MainScene") { panel = go; break; }
        if (panel == null) { Debug.LogError("[SetupSettingsPanel] SurvivalSettingsPanel 未找到"); return; }

        // ── 3. 面板底图（Background 子节点）────────────────────────
        var bg = panel.transform.Find("Background");
        if (bg != null) ApplyImage(bg, panelBg, Image.Type.Sliced, true);

        // ── 4. 关闭按钮 ──────────────────────────────────────────────
        var closeBtn = panel.transform.Find("CloseBtn");
        if (closeBtn != null)
        {
            ApplyImage(closeBtn, closeBg, Image.Type.Simple, true);
            // 调整大小 48×48
            var rt = closeBtn.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(48f, 48f);
        }

        // ── 5. 两个 Slider ───────────────────────────────────────────
        ApplySlider(panel, "BGMRow/BGMSlider", trackBg, fillSpr, handleSpr);
        ApplySlider(panel, "SFXRow/SFXSlider", trackBg, fillSpr, handleSpr);

        // ── 6. 两个 Toggle（礼物视频 / VIP视频）────────────────────
        ApplyToggle(panel, "GiftVideoRow/Toggle", toggleOn, toggleOff);
        ApplyToggle(panel, "VIPVideoRow/Toggle",  toggleOn, toggleOff);

        // ── 7. 保存 ───────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(panel.scene);
        EditorSceneManager.SaveScene(panel.scene);
        Debug.Log("[SetupSettingsPanel] 完成，场景已保存。");
    }

    // ──────────────────────── 辅助：滑条 ────────────────────────────
    static void ApplySlider(GameObject panel, string path,
                            Sprite track, Sprite fill, Sprite handle)
    {
        var sliderT = panel.transform.Find(path);
        if (sliderT == null) { Debug.LogWarning("[SetupSettingsPanel] 未找到: " + path); return; }

        // Background（轨道）
        var bgT = sliderT.Find("Background");
        if (bgT != null) ApplyImage(bgT, track, Image.Type.Sliced, false);

        // Fill Area / Fill（填充）
        var fillT = sliderT.Find("Fill Area/Fill");
        if (fillT != null) ApplyImage(fillT, fill, Image.Type.Sliced, false);

        // Handle Slide Area / Handle（滑块）
        var handleT = sliderT.Find("Handle Slide Area/Handle");
        if (handleT != null)
        {
            ApplyImage(handleT, handle, Image.Type.Simple, true);
            var rt = handleT.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(36f, 36f);
        }

        Debug.Log("[SetupSettingsPanel] " + path + " 已设置");
    }

    // ──────────────────────── 辅助：Toggle ──────────────────────────
    static void ApplyToggle(GameObject panel, string path,
                             Sprite toggleOn, Sprite toggleOff)
    {
        var togT = panel.transform.Find(path);
        if (togT == null) { Debug.LogWarning("[SetupSettingsPanel] 未找到: " + path); return; }

        // Toggle 自身 Image = 关闭态底图
        var img = togT.GetComponent<Image>();
        if (img != null)
        {
            img.sprite         = toggleOff;
            img.color          = Color.white;
            img.type           = Image.Type.Simple;
            img.preserveAspect = false;
            img.raycastTarget  = true;
            // 固定大小 80×40
            var rt = togT.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(80f, 40f);
        }

        // Checkmark = 开启态图片，覆盖整个 Toggle 区域
        var checkT = togT.Find("Checkmark");
        if (checkT != null)
        {
            var cImg = checkT.GetComponent<Image>();
            if (cImg == null) cImg = checkT.gameObject.AddComponent<Image>();
            cImg.sprite         = toggleOn;
            cImg.color          = Color.white;
            cImg.type           = Image.Type.Simple;
            cImg.preserveAspect = false;
            cImg.raycastTarget  = false;

            // Checkmark 铺满 Toggle
            var cRt = checkT.GetComponent<RectTransform>();
            if (cRt == null) cRt = checkT.gameObject.AddComponent<RectTransform>();
            cRt.anchorMin        = Vector2.zero;
            cRt.anchorMax        = Vector2.one;
            cRt.offsetMin        = Vector2.zero;
            cRt.offsetMax        = Vector2.zero;

            // 绑定到 Toggle.graphic
            var tog = togT.GetComponent<Toggle>();
            if (tog != null) tog.graphic = cImg;
        }

        Debug.Log("[SetupSettingsPanel] " + path + " Toggle 已设置");
    }

    // ──────────────────────── 辅助：通用 Image ──────────────────────
    static void ApplyImage(Transform t, Sprite sprite, Image.Type type, bool preserveAspect)
    {
        var img = t.GetComponent<Image>();
        if (img == null) img = t.gameObject.AddComponent<Image>();
        img.sprite         = sprite;
        img.color          = Color.white;
        img.type           = type;
        img.preserveAspect = preserveAspect;
        EditorUtility.SetDirty(img);
    }

    static void EnsureSprite(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;
        if (imp.textureType != TextureImporterType.Sprite)
        {
            imp.textureType         = TextureImporterType.Sprite;
            imp.spriteImportMode    = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled       = false;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }

    static Sprite Load(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
}
