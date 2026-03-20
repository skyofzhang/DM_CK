using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// 为 LoadingScreen 应用冬日主题：
/// - 背景：复用 bg_lobby_winter.jpg 雪山星空图
/// - Spinner：渐变冰蓝圆弧 spinner_ring.png
/// - 文字：浅冰蓝色 + 加粗，居中可读
/// </summary>
public static class SetupLoadingScreen
{
    [MenuItem("Tools/DrscfZ/Setup Loading Screen")]
    public static void Execute()
    {
        const string bgPath      = "Assets/Art/UI/Backgrounds/bg_lobby_winter.jpg";
        const string spinnerPath = "Assets/Art/UI/LoadingScreen/spinner_ring.png";

        // ── 1. 确保资源为 Sprite ─────────────────────────────────
        EnsureSprite(bgPath,      false);
        EnsureSprite(spinnerPath, true);
        AssetDatabase.Refresh();

        var bgSprite      = AssetDatabase.LoadAssetAtPath<Sprite>(bgPath);
        var spinnerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spinnerPath);

        if (bgSprite == null)      { Debug.LogError("[SetupLoadingScreen] bg_lobby_winter 未找到"); return; }
        if (spinnerSprite == null) { Debug.LogError("[SetupLoadingScreen] spinner_ring 未找到"); return; }

        // ── 2. 找 LoadingScreen ──────────────────────────────────
        GameObject loadingGO = null;
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go.name == "LoadingScreen" && go.scene.name == "MainScene") { loadingGO = go; break; }
        if (loadingGO == null) { Debug.LogError("[SetupLoadingScreen] LoadingScreen 未找到"); return; }

        // ── 3. 背景图 ─────────────────────────────────────────────
        var bgImg = loadingGO.GetComponent<Image>();
        if (bgImg == null) bgImg = loadingGO.AddComponent<Image>();
        bgImg.sprite         = bgSprite;
        bgImg.color          = new Color(1f, 1f, 1f, 0.92f);
        bgImg.type           = Image.Type.Simple;
        bgImg.preserveAspect = false;
        // 撑满全屏
        var bgRt = loadingGO.GetComponent<RectTransform>();
        if (bgRt != null)
        {
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
        }
        Debug.Log("[SetupLoadingScreen] 背景图已设置");

        // ── 4. Spinner ────────────────────────────────────────────
        var spinnerT = loadingGO.transform.Find("Spinner");
        if (spinnerT != null)
        {
            var sImg = spinnerT.GetComponent<Image>();
            if (sImg == null) sImg = spinnerT.gameObject.AddComponent<Image>();
            sImg.sprite         = spinnerSprite;
            sImg.color          = Color.white;
            sImg.type           = Image.Type.Simple;
            sImg.preserveAspect = true;
            sImg.raycastTarget  = false;

            var sRt = spinnerT.GetComponent<RectTransform>();
            if (sRt != null)
            {
                sRt.anchorMin        = new Vector2(0.5f, 0.5f);
                sRt.anchorMax        = new Vector2(0.5f, 0.5f);
                sRt.pivot            = new Vector2(0.5f, 0.5f);
                sRt.anchoredPosition = new Vector2(0f, 60f);  // 文字上方
                sRt.sizeDelta        = new Vector2(100f, 100f);
            }
            Debug.Log("[SetupLoadingScreen] Spinner 已设置");
        }

        // ── 5. StatusText ─────────────────────────────────────────
        SetTmpStyle(loadingGO, "StatusText",
            fontSize: 34f, bold: true,
            color: new Color(0.85f, 0.95f, 1f, 1f),
            anchorMin: new Vector2(0.1f, 0.36f),
            anchorMax: new Vector2(0.9f, 0.44f));

        // ── 6. DotText（省略号，跟在 StatusText 右侧用 anchoredPos 调整即可）
        SetTmpStyle(loadingGO, "DotText",
            fontSize: 34f, bold: true,
            color: new Color(0.85f, 0.95f, 1f, 1f),
            anchorMin: new Vector2(0.55f, 0.36f),
            anchorMax: new Vector2(0.9f,  0.44f));

        // ── 7. TipText（提示文字，下方小字）────────────────────────
        SetTmpStyle(loadingGO, "TipText",
            fontSize: 26f, bold: false,
            color: new Color(0.7f, 0.85f, 1f, 1f),
            anchorMin: new Vector2(0.1f, 0.28f),
            anchorMax: new Vector2(0.9f, 0.36f));

        // ── 8. 保存 ───────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(loadingGO.scene);
        EditorSceneManager.SaveScene(loadingGO.scene);
        Debug.Log("[SetupLoadingScreen] 完成，场景已保存。");
    }

    static void SetTmpStyle(GameObject parent, string childName,
        float fontSize, bool bold, Color color,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var t = parent.transform.Find(childName);
        if (t == null) { Debug.LogWarning("[SetupLoadingScreen] 未找到: " + childName); return; }

        var rt = t.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        var tmp = t.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            var so = new SerializedObject(tmp);
            so.FindProperty("m_fontSize").floatValue    = fontSize;
            so.FindProperty("m_fontStyle").intValue     = bold ? (int)FontStyles.Bold : (int)FontStyles.Normal;
            so.FindProperty("m_textAlignment").intValue = (int)TextAlignmentOptions.Center;
            so.FindProperty("m_overflowMode").intValue  = (int)TextOverflowModes.Overflow;
            so.FindProperty("m_fontColor").colorValue   = color;
            so.FindProperty("m_fontColor32").colorValue = color;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(tmp);
        }
        Debug.Log("[SetupLoadingScreen] " + childName + " 样式已设置");
    }

    static void EnsureSprite(string path, bool noMipmap)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        bool changed = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType         = TextureImporterType.Sprite;
            importer.spriteImportMode    = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            changed = true;
        }
        if (noMipmap && importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            changed = true;
        }
        if (changed) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }
}
