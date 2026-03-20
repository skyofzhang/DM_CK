using UnityEngine;
using UnityEditor;

/// <summary>列出 SurvivalSettingsPanel（或含 SurvivalSettingsUI 的父面板）的子层级</summary>
public static class InspectSettingsPanel
{
    [MenuItem("Tools/DrscfZ/Inspect Settings Panel")]
    public static void Execute()
    {
        // 找挂有 SurvivalSettingsUI 的对象（挂在 Canvas 上）
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            var ui = go.GetComponent<DrscfZ.UI.SurvivalSettingsUI>();
            if (ui == null) continue;

            Debug.Log("=== SurvivalSettingsUI on: " + go.name + " ===");

            // 用 SerializedObject 读取 _panel 字段
            var so = new SerializedObject(ui);
            var panelProp = so.FindProperty("_panel");
            if (panelProp != null && panelProp.objectReferenceValue != null)
            {
                var panel = panelProp.objectReferenceValue as GameObject;
                Debug.Log("_panel = " + panel.name + " (active=" + panel.activeSelf + ")");
                PrintChildren(panel.transform, "  ");
            }
            else
            {
                Debug.LogWarning("_panel 未绑定或为 null");
            }
            return;
        }
        Debug.LogError("未找到 SurvivalSettingsUI");
    }

    static void PrintChildren(Transform t, string indent)
    {
        foreach (Transform child in t)
        {
            var img = child.GetComponent<UnityEngine.UI.Image>();
            var btn = child.GetComponent<UnityEngine.UI.Button>();
            var sldr = child.GetComponent<UnityEngine.UI.Slider>();
            var tog = child.GetComponent<UnityEngine.UI.Toggle>();
            string info = indent + "[" + (child.gameObject.activeSelf ? "ON" : "off") + "] " + child.name;
            if (img != null)  info += " [Image sprite=" + (img.sprite != null ? img.sprite.name : "null") + "]";
            if (btn != null)  info += " [Button]";
            if (sldr != null) info += " [Slider val=" + sldr.value.ToString("0.00") + "]";
            if (tog != null)  info += " [Toggle isOn=" + tog.isOn + "]";
            Debug.Log(info);
            PrintChildren(child, indent + "  ");
        }
    }
}
