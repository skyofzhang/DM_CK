using UnityEngine;
using UnityEditor;
using DrscfZ.UI;

public static class FixDuplicateLiveRankingUI
{
    [MenuItem("Tools/DrscfZ/Fix Duplicate LiveRankingUI")]
    public static void Execute()
    {
        var all = Resources.FindObjectsOfTypeAll<SurvivalLiveRankingUI>();
        if (all.Length == 0) { Debug.Log("[FixDuplicate] 没有找到 SurvivalLiveRankingUI"); return; }

        SurvivalLiveRankingUI keepComp  = null;
        foreach (var ui in all)
        {
            // 保留挂在 GameUIPanel 上的那个（正确位置）
            if (ui.gameObject.name == "GameUIPanel")
            {
                keepComp = ui;
                break;
            }
        }

        foreach (var ui in all)
        {
            if (ui == keepComp) continue;
            Debug.Log($"[FixDuplicate] 移除重复组件: {ui.gameObject.name}");
            Object.DestroyImmediate(ui);
        }

        if (keepComp != null)
            Debug.Log($"[FixDuplicate] 保留: {keepComp.gameObject.name}，完成");
        else
            Debug.LogWarning("[FixDuplicate] 未找到挂在 GameUIPanel 的实例，请手动检查");

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log("[FixDuplicate] 场景已保存");
    }
}
