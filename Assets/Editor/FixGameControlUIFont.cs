using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// 修复 GameControlUI 所有 TMP 文字方块问题：将字体替换为 ChineseFont SDF
/// 菜单: CapybaraDuel/Fix GameControlUI Font
/// </summary>
public class FixGameControlUIFont
{
    [MenuItem("CapybaraDuel/Fix GameControlUI Font")]
    static void Run()
    {
        // 加载中文字体
        var chFont = Resources.Load<TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF") ?? Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
        if (chFont == null)
        {
            Debug.LogError("[FixFont] ChineseFont SDF not found at Resources/Fonts/ChineseFont SDF");
            return;
        }
        Debug.Log($"[FixFont] Loaded font: {chFont.name}");

        // 找到 GameControlUI 根节点（含非激活）
        var allTMPs = Object.FindObjectsOfType<TextMeshProUGUI>(true);
        int count = 0;
        foreach (var tmp in allTMPs)
        {
            // 只处理 GameControlUI 下的文字
            if (!IsUnderGameControlUI(tmp.transform)) continue;

            if (tmp.font != chFont)
            {
                Undo.RecordObject(tmp, "Fix GameControlUI Font");
                tmp.font = chFont;
                EditorUtility.SetDirty(tmp);
                count++;
                Debug.Log($"[FixFont] Fixed: {GetPath(tmp.transform)}");
            }
        }

        if (count == 0)
        {
            Debug.Log("[FixFont] No TMP components needed fixing (already using ChineseFont SDF, or GameControlUI not found).");
        }
        else
        {
            // 保存场景
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[FixFont] Done. Fixed {count} TMP component(s). Scene saved.");
        }
    }

    static bool IsUnderGameControlUI(Transform t)
    {
        while (t != null)
        {
            if (t.name == "GameControlUI" || t.name == "GameControlPanel") return true;
            t = t.parent;
        }
        return false;
    }

    static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
