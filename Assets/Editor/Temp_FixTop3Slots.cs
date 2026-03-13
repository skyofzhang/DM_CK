using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using DrscfZ.UI;

/// <summary>
/// 为 SurvivalSettlementUI 绑定 _top3Slots[0~2]
/// → Canvas/SurvivalSettlementPanel/ScreenC/Top3Slot_0~2
/// </summary>
public class Temp_FixTop3Slots
{
    public static void Execute()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== Fix Top3Slots ===");

        Transform FindT(string path)
        {
            var parts = path.Split('/');
            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t.name != parts[parts.Length - 1]) continue;
                bool match = true;
                Transform cur = t;
                for (int i = parts.Length - 1; i >= 0; i--)
                {
                    if (cur == null || cur.name != parts[i]) { match = false; break; }
                    cur = cur.parent;
                }
                if (match) return t;
            }
            return null;
        }

        var panelT = FindT("Canvas/SurvivalSettlementPanel");
        if (panelT == null) { Debug.LogError("[FixTop3Slots] SurvivalSettlementPanel not found"); return; }

        var ui = panelT.GetComponent<SurvivalSettlementUI>();
        if (ui == null) { Debug.LogError("[FixTop3Slots] SurvivalSettlementUI component not found"); return; }

        var so = new SerializedObject(ui);
        so.Update();

        // _top3Slots 是 GameObject[] 长度3
        var prop = so.FindProperty("_top3Slots");
        if (prop == null) { Debug.LogError("[FixTop3Slots] _top3Slots property not found"); return; }

        prop.arraySize = 3;
        string[] slotPaths = {
            "Canvas/SurvivalSettlementPanel/ScreenC/Top3Slot_0",
            "Canvas/SurvivalSettlementPanel/ScreenC/Top3Slot_1",
            "Canvas/SurvivalSettlementPanel/ScreenC/Top3Slot_2",
        };

        for (int i = 0; i < 3; i++)
        {
            var slotT = FindT(slotPaths[i]);
            if (slotT == null)
            {
                sb.AppendLine($"  ⚠ Top3Slot_{i} 未找到");
                continue;
            }
            prop.GetArrayElementAtIndex(i).objectReferenceValue = slotT.gameObject;
            sb.AppendLine($"  ✅ _top3Slots[{i}] → {slotPaths[i]}");
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(panelT.gameObject);

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        sb.AppendLine("✅ Scene saved");
        Debug.Log("[FixTop3Slots]\n" + sb.ToString());
    }
}
