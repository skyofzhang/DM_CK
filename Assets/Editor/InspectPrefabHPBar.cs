using UnityEngine;
using UnityEditor;

public class InspectPrefabHPBar
{
    public static string Execute()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monsters/X_guai01.prefab");
        if (prefab == null) return "ERROR: prefab not found";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Root localScale: " + prefab.transform.localScale);

        // Find HPBarCanvas
        var hpCanvas = prefab.transform.Find("HPBarCanvas");
        if (hpCanvas == null)
        {
            // Try recursive search
            foreach (Transform t in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "HPBarCanvas")
                {
                    hpCanvas = t;
                    break;
                }
            }
        }

        if (hpCanvas == null)
        {
            sb.AppendLine("HPBarCanvas NOT FOUND. All children:");
            foreach (Transform child in prefab.transform)
                sb.AppendLine("  Child: " + child.name + " scale=" + child.localScale);
        }
        else
        {
            sb.AppendLine("HPBarCanvas found at path: " + GetPath(hpCanvas));
            sb.AppendLine("HPBarCanvas localScale: " + hpCanvas.localScale);
            sb.AppendLine("HPBarCanvas children:");
            foreach (Transform child in hpCanvas)
                sb.AppendLine("  " + child.name + " localScale=" + child.localScale + " localPos=" + child.localPosition);
        }

        return sb.ToString();
    }

    static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }
}
