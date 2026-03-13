using UnityEditor;
using UnityEngine;
using TMPro;

public class FixBottomBarFont
{
    public static string Execute()
    {
        var bottomBar = GameObject.Find("Canvas/BottomBar");
        if (bottomBar == null) return "BottomBar not found!";

        int fixedCount = 0;
        var tmps = bottomBar.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var tmp in tmps)
        {
            if (tmp.fontSize < 22f)
            {
                tmp.fontSize = 22f;
                EditorUtility.SetDirty(tmp.gameObject);
                fixedCount++;
                Debug.Log($"[FixBottomBarFont] Fixed {tmp.gameObject.name} fontSize to 22px");
            }
        }

        Debug.Log($"[FixBottomBarFont] Fixed {fixedCount} button texts.");
        return $"Fixed {fixedCount} button texts to 22px.";
    }
}
