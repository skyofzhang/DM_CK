using UnityEditor;
using UnityEngine;
using TMPro;

public class FixBarrageFont
{
    public static string Execute()
    {
        var contentGo = GameObject.Find("Canvas/GameUIPanel/BarragePanel/ScrollView/Viewport/BarrageContent");
        if (contentGo == null)
            return "BarrageContent not found!";

        int fixedCount = 0;
        var tmps = contentGo.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var tmp in tmps)
        {
            if (tmp.gameObject.name == "MsgRow" && tmp.fontSize < 26f)
            {
                tmp.fontSize = 26f;
                EditorUtility.SetDirty(tmp.gameObject);
                fixedCount++;
            }
        }

        Debug.Log($"[FixBarrageFont] Fixed {fixedCount} MsgRow font sizes to 26px.");
        return $"Fixed {fixedCount} MsgRow font sizes to 26px.";
    }
}
