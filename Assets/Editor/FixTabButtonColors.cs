using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

public class FixTabButtonColors
{
    [MenuItem("Tools/DrscfZ/Fix Tab Button Colors")]
    public static void Execute()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("Canvas not found"); return; }

        Transform panel = null;
        foreach (Transform child in canvas.transform)
        {
            if (child.name == "SurvivalRankingPanel") { panel = child; break; }
        }
        if (panel == null) { Debug.LogError("SurvivalRankingPanel not found"); return; }

        FixButton(panel, "TabContribution");
        FixButton(panel, "TabStreamer");

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[FixTabButtonColors] Button ColorBlock 已修复，场景已保存");
    }

    static void FixButton(Transform panel, string name)
    {
        var t = panel.Find(name);
        if (t == null) return;
        var btn = t.GetComponent<Button>();
        if (btn == null) return;

        var cb = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(0.9f, 0.9f, 1f, 1f);
        cb.pressedColor     = new Color(0.7f, 0.7f, 0.8f, 1f);
        cb.selectedColor    = Color.white;
        cb.disabledColor    = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        btn.colors = cb;

        Debug.Log($"[FixTabButtonColors] {name} Normal→White");
    }
}
