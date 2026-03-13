#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using DrscfZ.UI;
using TMPro;
using UnityEngine.UI;

public class RunWireSettlementUI
{
    public static void Execute()
    {
        // Find SurvivalSettlementPanel (may be inactive — use Resources trick)
        // Use includeInactive search via transform traversal
        SurvivalSettlementUI ui = null;
        foreach (var go in Resources.FindObjectsOfTypeAll<SurvivalSettlementUI>())
        {
            // Skip prefab assets; only scene objects
            if (go.gameObject.scene.IsValid())
            {
                ui = go;
                break;
            }
        }

        if (ui == null)
        {
            Debug.LogError("[RunWireSettlementUI] SurvivalSettlementUI not found in scene!");
            return;
        }

        var panel = ui.gameObject;
        var so = new SerializedObject(ui);

        // ScreenA
        var screenA = panel.transform.Find("ScreenA")?.gameObject;
        if (screenA != null)
        {
            so.FindProperty("_screenA").objectReferenceValue = screenA;
            var titleTMP    = screenA.transform.Find("ResultTitle")?.GetComponent<TextMeshProUGUI>();
            var subtitleTMP = screenA.transform.Find("ResultSubtitle")?.GetComponent<TextMeshProUGUI>();
            if (titleTMP)    so.FindProperty("_resultTitleText").objectReferenceValue = titleTMP;
            else             Debug.LogWarning("[RunWireSettlementUI] ScreenA/ResultTitle TMP not found");
            if (subtitleTMP) so.FindProperty("_resultSubtitleText").objectReferenceValue = subtitleTMP;
            else             Debug.LogWarning("[RunWireSettlementUI] ScreenA/ResultSubtitle TMP not found");
        }
        else Debug.LogWarning("[RunWireSettlementUI] ScreenA not found under panel");

        // ScreenB
        var screenB = panel.transform.Find("ScreenB")?.gameObject;
        if (screenB != null)
        {
            so.FindProperty("_screenB").objectReferenceValue = screenB;
            var daysTMP    = screenB.transform.Find("SurvivalDaysText")?.GetComponent<TextMeshProUGUI>();
            var killsTMP   = screenB.transform.Find("TotalKillsText")?.GetComponent<TextMeshProUGUI>();
            var gatherTMP  = screenB.transform.Find("TotalGatherText")?.GetComponent<TextMeshProUGUI>();
            var repairTMP  = screenB.transform.Find("TotalRepairText")?.GetComponent<TextMeshProUGUI>();
            var rankParent = screenB.transform.Find("RankingList");
            if (daysTMP)    so.FindProperty("_survivalDaysText").objectReferenceValue = daysTMP;
            if (killsTMP)   so.FindProperty("_totalKillsText").objectReferenceValue = killsTMP;
            if (gatherTMP)  so.FindProperty("_totalGatherText").objectReferenceValue = gatherTMP;
            if (repairTMP)  so.FindProperty("_totalRepairText").objectReferenceValue = repairTMP;
            if (rankParent) so.FindProperty("_rankingListParent").objectReferenceValue = rankParent;
        }
        else Debug.LogWarning("[RunWireSettlementUI] ScreenB not found under panel");

        // ScreenC — try both naming conventions (MVPName and MvpNameText etc.)
        var screenC = panel.transform.Find("ScreenC")?.gameObject;
        if (screenC != null)
        {
            so.FindProperty("_screenC").objectReferenceValue = screenC;

            var mvpNameTMP  = screenC.transform.Find("MVPName")?.GetComponent<TextMeshProUGUI>()
                           ?? screenC.transform.Find("MvpNameText")?.GetComponent<TextMeshProUGUI>();
            var mvpScoreTMP = screenC.transform.Find("MVPScore")?.GetComponent<TextMeshProUGUI>()
                           ?? screenC.transform.Find("MvpScoreText")?.GetComponent<TextMeshProUGUI>();
            var mvpLineTMP  = screenC.transform.Find("MVPAnchorLine")?.GetComponent<TextMeshProUGUI>()
                           ?? screenC.transform.Find("MvpAnchorLine")?.GetComponent<TextMeshProUGUI>();

            if (mvpNameTMP)  so.FindProperty("_mvpNameText").objectReferenceValue = mvpNameTMP;
            else             Debug.LogWarning("[RunWireSettlementUI] ScreenC MVP name TMP not found");
            if (mvpScoreTMP) so.FindProperty("_mvpScoreText").objectReferenceValue = mvpScoreTMP;
            else             Debug.LogWarning("[RunWireSettlementUI] ScreenC MVP score TMP not found");
            if (mvpLineTMP)  so.FindProperty("_mvpAnchorLineText").objectReferenceValue = mvpLineTMP;
            else             Debug.LogWarning("[RunWireSettlementUI] ScreenC MVP anchor line TMP not found");
        }
        else Debug.LogWarning("[RunWireSettlementUI] ScreenC not found under panel");

        // RestartButton
        var restartBtn = panel.transform.Find("RestartButton")?.GetComponent<Button>();
        if (restartBtn) so.FindProperty("_restartButton").objectReferenceValue = restartBtn;
        else            Debug.LogWarning("[RunWireSettlementUI] RestartButton not found");

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(panel);
        Debug.Log("[RunWireSettlementUI] Done! SurvivalSettlementUI fully wired.");
    }
}
#endif
