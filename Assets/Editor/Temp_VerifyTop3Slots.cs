using UnityEngine;
using UnityEditor;
using DrscfZ.UI;

public class Temp_VerifyTop3Slots
{
    public static void Execute()
    {
        var uis = Resources.FindObjectsOfTypeAll<SurvivalSettlementUI>();
        if (uis == null || uis.Length == 0) { Debug.LogError("未找到组件"); return; }

        var so = new SerializedObject(uis[0]);
        var prop = so.FindProperty("_top3Slots");

        if (prop == null) { Debug.LogError("_top3Slots 字段未找到"); return; }

        Debug.Log($"[VerifyTop3Slots] arraySize = {prop.arraySize}");
        for (int i = 0; i < prop.arraySize; i++)
        {
            var elem = prop.GetArrayElementAtIndex(i).objectReferenceValue;
            Debug.Log($"[VerifyTop3Slots] _top3Slots[{i}] = {(elem != null ? elem.name : "NULL ❌")}");
        }
    }
}
