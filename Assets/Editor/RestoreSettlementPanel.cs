using UnityEditor;
using UnityEngine;

public class RestoreSettlementPanel
{
    public static string Execute()
    {
        GameObject panel = null;
        var all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var p in all)
        {
            if (p.name == "SurvivalSettlementPanel" && p.scene.IsValid())
            {
                panel = p;
                break;
            }
        }
        if (panel == null) return "Panel not found";

        panel.SetActive(false);
        EditorUtility.SetDirty(panel);
        return "SurvivalSettlementPanel restored to inactive.";
    }
}
