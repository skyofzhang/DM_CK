using UnityEngine;
using UnityEditor;

public class PreviewSettingsPanel
{
    [MenuItem("DrscfZ/Preview Settings Panel (Toggle)")]
    public static void Toggle()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return;
        var panel = canvas.transform.Find("SurvivalSettingsPanel")?.gameObject;
        if (panel == null) return;
        panel.SetActive(!panel.activeSelf);
        EditorUtility.SetDirty(panel);
        Debug.Log($"[Preview] SurvivalSettingsPanel => {panel.activeSelf}");
    }
}
