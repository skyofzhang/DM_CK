using UnityEngine;
using UnityEditor;
using DrscfZ.Survival;
using System.Reflection;

public static class DiagUI
{
    static string GetPath(GameObject go)
    {
        if (go == null) return "(null)";
        string path = go.name;
        var t = go.transform.parent;
        while (t != null) { path = t.name + "/" + path; t = t.parent; }
        return path;
    }

    [MenuItem("Tools/DrscfZ/Diag UI Mount")]
    public static void Execute()
    {
        // ── 1. SurvivalGameManager → _settlementUI 是否绑定 ──
        var sgm = Object.FindObjectOfType<SurvivalGameManager>();
        if (sgm == null) { Debug.LogError("[DiagUI] 找不到 SurvivalGameManager"); return; }

        var fi = typeof(SurvivalGameManager).GetField("_settlementUI",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var settlementUI = fi?.GetValue(sgm);
        Debug.Log($"[DiagUI] SurvivalGameManager._settlementUI = {settlementUI ?? "(null)"}");

        // ── 2. IdleUI 挂在哪个 GameObject ──
        var allIdleUIs = Resources.FindObjectsOfTypeAll<DrscfZ.UI.SurvivalIdleUI>();
        if (allIdleUIs.Length == 0)
            Debug.LogError("[DiagUI] 找不到 SurvivalIdleUI");
        foreach (var u in allIdleUIs)
            Debug.Log($"[DiagUI] SurvivalIdleUI 挂在: {GetPath(u.gameObject)} (activeInHierarchy={u.gameObject.activeInHierarchy})");

        // ── 3. SettlementUI 挂在哪个 GameObject ──
        var allSettlementUIs = Resources.FindObjectsOfTypeAll<DrscfZ.UI.SurvivalSettlementUI>();
        if (allSettlementUIs.Length == 0)
            Debug.LogError("[DiagUI] 找不到 SurvivalSettlementUI");
        foreach (var s in allSettlementUIs)
            Debug.Log($"[DiagUI] SurvivalSettlementUI 挂在: {GetPath(s.gameObject)} (activeInHierarchy={s.gameObject.activeInHierarchy})");

        // ── 4. LiveRankingUI 挂在哪个 GameObject ──
        var allLiveUIs = Resources.FindObjectsOfTypeAll<DrscfZ.UI.SurvivalLiveRankingUI>();
        foreach (var l in allLiveUIs)
            Debug.Log($"[DiagUI] SurvivalLiveRankingUI 挂在: {GetPath(l.gameObject)} (activeInHierarchy={l.gameObject.activeInHierarchy})");

        // ── 5. IdleUI._panel ──
        foreach (var u in allIdleUIs)
        {
            var panelFi = typeof(DrscfZ.UI.SurvivalIdleUI).GetField("_panel",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var panel = panelFi?.GetValue(u) as GameObject;
            Debug.Log($"[DiagUI] SurvivalIdleUI._panel = {GetPath(panel)}");
        }
    }
}
