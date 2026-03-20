using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public static class InspectRankingBtn
{
    public static void Execute()
    {
        // ’“ RankingBtn
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.name != "RankingBtn") continue;
            var btn = go.GetComponent<Button>();
            if (btn == null) continue;

            var so = new SerializedObject(btn);
            var calls = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            Debug.Log("=== RankingBtn onClick listeners: " + calls.arraySize);
            for (int i = 0; i < calls.arraySize; i++)
            {
                var call = calls.GetArrayElementAtIndex(i);
                var target   = call.FindPropertyRelative("m_Target").objectReferenceValue;
                var method   = call.FindPropertyRelative("m_MethodName").stringValue;
                var mode     = call.FindPropertyRelative("m_Mode").intValue;
                Debug.Log("  [" + i + "] target=" + (target != null ? target.name : "null")
                    + " method=" + method + " mode=" + mode);
            }

            // Õ¨ ±ºÏ≤È SettingsBtn
            foreach (var go2 in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go2.name != "SettingsBtn") continue;
                var btn2 = go2.GetComponent<Button>();
                if (btn2 == null) continue;
                var so2 = new SerializedObject(btn2);
                var calls2 = so2.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
                Debug.Log("=== SettingsBtn onClick listeners: " + calls2.arraySize);
                for (int i = 0; i < calls2.arraySize; i++)
                {
                    var call = calls2.GetArrayElementAtIndex(i);
                    var target2  = call.FindPropertyRelative("m_Target").objectReferenceValue;
                    var method2  = call.FindPropertyRelative("m_MethodName").stringValue;
                    Debug.Log("  [" + i + "] target=" + (target2 != null ? target2.name : "null")
                        + " method=" + method2);
                }
                break;
            }
            return;
        }
        Debug.LogError("RankingBtn not found");
    }
}