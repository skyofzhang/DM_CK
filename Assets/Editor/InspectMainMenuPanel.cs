using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class InspectMainMenuPanel
{
    [MenuItem("Tools/DrscfZ/Inspect MainMenuPanel")]
    public static void Execute()
    {
        // 找 MainMenuPanel
        var allGO = Resources.FindObjectsOfTypeAll<GameObject>();
        GameObject panel = null;
        foreach (var go in allGO)
        {
            if (go.name == "MainMenuPanel" || go.name.Contains("IdlePanel") || go.name.Contains("LobbyPanel"))
            {
                panel = go;
                Debug.Log("[Inspect] Found: " + GetPath(go) + " active=" + go.activeSelf);
            }
        }
        if (panel == null) { Debug.LogError("[Inspect] MainMenuPanel not found!"); return; }

        // 列出所有子孙
        var all = panel.GetComponentsInChildren<Transform>(true);
        foreach (var t in all)
        {
            var img = t.GetComponent<Image>();
            var btn = t.GetComponent<Button>();
            string info = GetPath(t.gameObject);
            if (img != null) info += " [Image sprite=" + (img.sprite != null ? img.sprite.name : "null") + "]";
            if (btn != null) info += " [Button]";
            Debug.Log(info);
        }
    }

    static string GetPath(GameObject go)
    {
        string p = go.name;
        var t = go.transform.parent;
        while (t != null) { p = t.name + "/" + p; t = t.parent; }
        return p;
    }
}
