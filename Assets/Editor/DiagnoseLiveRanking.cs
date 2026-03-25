using UnityEngine;
using UnityEditor;
using DrscfZ.UI;
using DrscfZ.Survival;

public static class DiagnoseLiveRanking
{
    [MenuItem("Tools/DrscfZ/Diagnose Live Ranking")]
    public static void Execute()
    {
        // 1. 找 SurvivalLiveRankingUI 挂在哪
        var comps = Resources.FindObjectsOfTypeAll<SurvivalLiveRankingUI>();
        Debug.Log($"[Diag] SurvivalLiveRankingUI 实例数: {comps.Length}");
        foreach (var c in comps)
        {
            Debug.Log($"[Diag]   挂载对象: {c.gameObject.name}  scene={c.gameObject.scene.name}  active={c.gameObject.activeInHierarchy}");
        }

        // 2. 找 LiveRankingPanel
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.name == "LiveRankingPanel" && go.scene.isLoaded)
            {
                Debug.Log($"[Diag] LiveRankingPanel found: active={go.activeSelf}  activeInHierarchy={go.activeInHierarchy}  parent={go.transform.parent?.name}");
            }
        }

        // 3. SGM 事件注册情况（运行时才有意义）
        var sgm = SurvivalGameManager.Instance;
        Debug.Log($"[Diag] SurvivalGameManager.Instance: {(sgm != null ? sgm.State.ToString() : "NULL")}");
        if (sgm != null)
        {
            Debug.Log($"[Diag] SGM state={sgm.State}");
        }
    }
}
