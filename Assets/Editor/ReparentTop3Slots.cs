using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 把 Top3Slot_1/2/3 从 Top3Slot_0 容器中提出，直接挂到 ScreenC 下，
/// 然后删除空的 Top3Slot_0 容器。
/// Tools → DrscfZ → Reparent Top3 Slots
/// </summary>
public static class ReparentTop3Slots
{
    [MenuItem("Tools/DrscfZ/Rename Top3 Slots to 0-based")]
    public static void Rename012()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("[Rename] 找不到 Canvas"); return; }
        var screenC = canvas.transform.Find("SurvivalSettlementPanel/ScreenC");
        if (screenC == null) { Debug.LogError("[Rename] 找不到 ScreenC"); return; }

        // 1→0, 2→1, 3→2
        for (int i = 1; i <= 3; i++)
        {
            var slot = screenC.Find($"Top3Slot_{i}");
            if (slot == null) { Debug.LogWarning($"[Rename] Top3Slot_{i} 未找到"); continue; }
            slot.name = $"Top3Slot_{i - 1}";
            Debug.Log($"[Rename] Top3Slot_{i} → Top3Slot_{i - 1}");
        }

        EditorSceneManager.MarkSceneDirty(screenC.gameObject.scene);
        EditorSceneManager.SaveScene(screenC.gameObject.scene);
        Debug.Log("[Rename] 重命名完成，场景已保存");
    }

    [MenuItem("Tools/DrscfZ/Reparent Top3 Slots")]
    public static void Execute()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("[Reparent] 找不到 Canvas"); return; }

        var screenC = canvas.transform.Find("SurvivalSettlementPanel/ScreenC");
        if (screenC == null) { Debug.LogError("[Reparent] 找不到 ScreenC"); return; }

        var container = screenC.Find("Top3Slot_0");
        if (container == null) { Debug.Log("[Reparent] Top3Slot_0 不存在，无需处理"); return; }

        // 把 1/2/3 提到 ScreenC 直接子节点
        for (int i = 1; i <= 3; i++)
        {
            var slot = container.Find($"Top3Slot_{i}");
            if (slot == null) { Debug.LogWarning($"[Reparent] Top3Slot_{i} 未找到"); continue; }
            slot.SetParent(screenC, worldPositionStays: false);
            Debug.Log($"[Reparent] Top3Slot_{i} → ScreenC");
        }

        // 删除已空的 Top3Slot_0
        Object.DestroyImmediate(container.gameObject);
        Debug.Log("[Reparent] Top3Slot_0 已删除");

        EditorSceneManager.MarkSceneDirty(screenC.gameObject.scene);
        EditorSceneManager.SaveScene(screenC.gameObject.scene);
        Debug.Log("[Reparent] 完成，场景已保存");
    }
}
