using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 修复设置面板按钮图标：
/// - CloseBtn：更大更明显的红色圆形 X（80×80）
/// - BGMToggleBtn / SFXToggleBtn：喇叭开/关图标（运行时动态由脚本切换）
///   Editor 预设为"开"状态图标
/// </summary>
public static class FixSettingsPanelButtons
{
    [MenuItem("Tools/DrscfZ/Fix Settings Panel Buttons")]
    public static void Execute()
    {
        const string root = "Assets/Art/UI/Settings/";

        EnsureSprite(root + "btn_close.png");
        EnsureSprite(root + "btn_speaker_on.png");
        EnsureSprite(root + "btn_speaker_off.png");
        AssetDatabase.Refresh();

        var closeSprite   = Load(root + "btn_close.png");
        var speakerOn     = Load(root + "btn_speaker_on.png");

        if (closeSprite == null || speakerOn == null)
        { Debug.LogError("[FixSettingsPanelButtons] 资源加载失败"); return; }

        // 找 SurvivalSettingsPanel
        GameObject panel = null;
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go.name == "SurvivalSettingsPanel" && go.scene.name == "MainScene") { panel = go; break; }
        if (panel == null) { Debug.LogError("[FixSettingsPanelButtons] SurvivalSettingsPanel 未找到"); return; }

        // ── 关闭按钮 ──────────────────────────────────────────────
        var closeT = panel.transform.Find("CloseBtn");
        if (closeT != null)
        {
            var img = closeT.GetComponent<Image>();
            if (img == null) img = closeT.gameObject.AddComponent<Image>();
            img.sprite         = closeSprite;
            img.color          = Color.white;
            img.type           = Image.Type.Simple;
            img.preserveAspect = true;
            img.raycastTarget  = true;

            var rt = closeT.GetComponent<RectTransform>();
            if (rt != null)
            {
                // 锚定到右上角，64×64
                rt.anchorMin        = new Vector2(1f, 1f);
                rt.anchorMax        = new Vector2(1f, 1f);
                rt.pivot            = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(-24f, -24f);
                rt.sizeDelta        = new Vector2(64f, 64f);
            }
            EditorUtility.SetDirty(closeT.gameObject);
            Debug.Log("[FixSettingsPanelButtons] CloseBtn 已更新");
        }

        // ── BGMToggleBtn ───────────────────────────────────────────
        ApplySpeakerBtn(panel, "BGMRow/BGMToggleBtn", speakerOn);

        // ── SFXToggleBtn ───────────────────────────────────────────
        ApplySpeakerBtn(panel, "SFXRow/SFXToggleBtn", speakerOn);

        EditorSceneManager.MarkSceneDirty(panel.scene);
        EditorSceneManager.SaveScene(panel.scene);
        Debug.Log("[FixSettingsPanelButtons] 完成，场景已保存。");
    }

    static void ApplySpeakerBtn(GameObject panel, string path, Sprite sprite)
    {
        var t = panel.transform.Find(path);
        if (t == null) { Debug.LogWarning("[FixSettingsPanelButtons] 未找到: " + path); return; }

        var img = t.GetComponent<Image>();
        if (img == null) img = t.gameObject.AddComponent<Image>();
        img.sprite         = sprite;
        img.color          = Color.white;
        img.type           = Image.Type.Simple;
        img.preserveAspect = true;
        img.raycastTarget  = true;

        var rt = t.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = new Vector2(56f, 42f);

        // 隐藏子 Text（避免文字emoji覆盖图标）
        var tmp = t.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (tmp != null) tmp.gameObject.SetActive(false);

        EditorUtility.SetDirty(t.gameObject);
        Debug.Log("[FixSettingsPanelButtons] " + path + " 已更新");
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
