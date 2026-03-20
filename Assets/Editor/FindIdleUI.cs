using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public static class FindIdleUI
{
    [MenuItem("Tools/DrscfZ/Find Idle UI")]
    public static void Execute()
    {
        var allGO = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in allGO)
        {
            var comp = go.GetComponent("SurvivalIdleUI");
            if (comp != null)
            {
                Debug.Log("[IdleUI] Found on: " + GetPath(go));
                var btns = go.GetComponentsInChildren<Button>(true);
                foreach (var btn in btns)
                {
                    var img = btn.GetComponent<Image>();
                    string spriteName = (img != null && img.sprite != null) ? img.sprite.name : "null";
                    Debug.Log("  BTN: " + btn.gameObject.name + " | sprite=" + spriteName);
                }
                // 找背景 Image（直接子）
                var imgs = go.GetComponentsInChildren<Image>(true);
                foreach (var img in imgs)
                {
                    if (img.sprite == null) continue;
                    Debug.Log("  IMG: " + GetPath(img.gameObject) + " | " + img.sprite.name);
                }
            }
        }
        Debug.Log("[FindIdleUI] Done");
    }

    static string GetPath(GameObject go)
    {
        string p = go.name;
        var t = go.transform.parent;
        while (t != null) { p = t.name + "/" + p; t = t.parent; }
        return p;
    }
}
