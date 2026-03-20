using UnityEngine;
using UnityEditor;
using TMPro;

public class Temp_FixFont
{
    public static void Execute()
    {
        var chFont = Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
        if (chFont == null)
        {
            Debug.LogError("[FixFont] ChineseFont SDF not found at Resources/Fonts/ChineseFont SDF");
            return;
        }
        Debug.Log($"[FixFont] Loaded font: {chFont.name}");

        var allTMPs = Object.FindObjectsOfType<TextMeshProUGUI>(true);
        int count = 0;
        foreach (var tmp in allTMPs)
        {
            if (!IsUnderGameControlUI(tmp.transform)) continue;
            Undo.RecordObject(tmp, "Fix Font");
            tmp.font = chFont;
            EditorUtility.SetDirty(tmp);
            count++;
            Debug.Log($"[FixFont] {tmp.transform.name} -> ChineseFont SDF");
        }
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[FixFont] Done. Fixed {count} TMP(s). Scene saved.");
    }

    static bool IsUnderGameControlUI(Transform t)
    {
        while (t != null)
        {
            if (t.name == "GameControlUI" || t.name == "GameControlPanel" || t.name == "BottomBar") return true;
            t = t.parent;
        }
        return false;
    }
}
