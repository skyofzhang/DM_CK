using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 修复设置面板所有 Button 的 ColorBlock：
/// - normalColor = 纯白，常态完整显示图标
/// - highlightedColor = 浅蓝高亮
/// - pressedColor = 深蓝按下反馈
/// - 同时修复 CloseBtn 使其始终可见为红色圆形 X
/// </summary>
public static class FixSettingsBtnColors
{
    [MenuItem("Tools/DrscfZ/Fix Settings Button Colors")]
    public static void Execute()
    {
        GameObject panel = null;
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go.name == "SurvivalSettingsPanel" && go.scene.name == "MainScene") { panel = go; break; }
        if (panel == null) { Debug.LogError("[FixSettingsBtnColors] SurvivalSettingsPanel 未找到"); return; }

        // 修复 CloseBtn
        FixBtn(panel, "CloseBtn",
            normal:      Color.white,
            highlighted: new Color(1f, 0.85f, 0.85f, 1f),   // 浅粉红
            pressed:     new Color(0.7f, 0.1f, 0.1f, 1f));  // 深红

        // 修复 BGMToggleBtn
        FixBtn(panel, "BGMRow/BGMToggleBtn",
            normal:      Color.white,
            highlighted: new Color(0.85f, 0.95f, 1f, 1f),
            pressed:     new Color(0.5f, 0.7f, 1f, 1f));

        // 修复 SFXToggleBtn
        FixBtn(panel, "SFXRow/SFXToggleBtn",
            normal:      Color.white,
            highlighted: new Color(0.85f, 0.95f, 1f, 1f),
            pressed:     new Color(0.5f, 0.7f, 1f, 1f));

        EditorSceneManager.MarkSceneDirty(panel.scene);
        EditorSceneManager.SaveScene(panel.scene);
        Debug.Log("[FixSettingsBtnColors] 完成，场景已保存。");
    }

    static void FixBtn(GameObject panel, string path,
                       Color normal, Color highlighted, Color pressed)
    {
        var t = panel.transform.Find(path);
        if (t == null) { Debug.LogWarning("[FixSettingsBtnColors] 未找到: " + path); return; }

        var btn = t.GetComponent<Button>();
        if (btn != null)
        {
            var cb = btn.colors;
            cb.normalColor      = normal;
            cb.highlightedColor = highlighted;
            cb.pressedColor     = pressed;
            cb.selectedColor    = normal;
            cb.disabledColor    = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            cb.colorMultiplier  = 1f;
            cb.fadeDuration     = 0.1f;
            btn.colors = cb;
            EditorUtility.SetDirty(btn);
        }

        // 同时确保 Image 本身也是 white（防止 Image.color 被染色）
        var img = t.GetComponent<Image>();
        if (img != null)
        {
            img.color = Color.white;
            EditorUtility.SetDirty(img);
        }

        Debug.Log("[FixSettingsBtnColors] " + path + " ColorBlock 已修复");
    }
}
