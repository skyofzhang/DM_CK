#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using DrscfZ.UI;
using TMPro;
using UnityEngine.UI;

public class WireSettlementUI : MonoBehaviour
{
    [MenuItem("极地生存/Wire Settlement UI")]
    public static void Execute()
    {
        var panel = GameObject.Find("SurvivalSettlementPanel");
        if (panel == null) { Debug.LogError("SurvivalSettlementPanel not found!"); return; }

        var ui = panel.GetComponent<SurvivalSettlementUI>();
        if (ui == null) { Debug.LogError("SurvivalSettlementUI not found!"); return; }

        var so = new SerializedObject(ui);

        // ScreenA
        var screenA = panel.transform.Find("ScreenA")?.gameObject;
        if (screenA != null)
        {
            so.FindProperty("_screenA").objectReferenceValue = screenA;
            var titleTMP = screenA.transform.Find("ResultTitle")?.GetComponent<TextMeshProUGUI>();
            var subtitleTMP = screenA.transform.Find("ResultSubtitle")?.GetComponent<TextMeshProUGUI>();
            if (titleTMP)    so.FindProperty("_resultTitleText").objectReferenceValue = titleTMP;
            if (subtitleTMP) so.FindProperty("_resultSubtitleText").objectReferenceValue = subtitleTMP;
        }

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
            if (daysTMP)   so.FindProperty("_survivalDaysText").objectReferenceValue = daysTMP;
            if (killsTMP)  so.FindProperty("_totalKillsText").objectReferenceValue = killsTMP;
            if (gatherTMP) so.FindProperty("_totalGatherText").objectReferenceValue = gatherTMP;
            if (repairTMP) so.FindProperty("_totalRepairText").objectReferenceValue = repairTMP;
            if (rankParent) so.FindProperty("_rankingListParent").objectReferenceValue = rankParent;
        }

        // ScreenC
        var screenC = panel.transform.Find("ScreenC")?.gameObject;
        if (screenC != null)
        {
            so.FindProperty("_screenC").objectReferenceValue = screenC;
            var mvpNameTMP   = screenC.transform.Find("MVPName")?.GetComponent<TextMeshProUGUI>();
            var mvpNameTMP2  = screenC.transform.Find("MvpNameText")?.GetComponent<TextMeshProUGUI>();
            var mvpScoreTMP  = screenC.transform.Find("MVPScore")?.GetComponent<TextMeshProUGUI>();
            var mvpScoreTMP2 = screenC.transform.Find("MvpScoreText")?.GetComponent<TextMeshProUGUI>();
            var mvpLineTMP   = screenC.transform.Find("MVPAnchorLine")?.GetComponent<TextMeshProUGUI>();
            var mvpLineTMP2  = screenC.transform.Find("MvpAnchorLine")?.GetComponent<TextMeshProUGUI>();

            var finalNameTMP  = mvpNameTMP  ?? mvpNameTMP2;
            var finalScoreTMP = mvpScoreTMP ?? mvpScoreTMP2;
            var finalLineTMP  = mvpLineTMP  ?? mvpLineTMP2;

            if (finalNameTMP)  so.FindProperty("_mvpNameText").objectReferenceValue = finalNameTMP;
            if (finalScoreTMP) so.FindProperty("_mvpScoreText").objectReferenceValue = finalScoreTMP;
            if (finalLineTMP)  so.FindProperty("_mvpAnchorLineText").objectReferenceValue = finalLineTMP;
        }

        // RestartButton
        var restartBtn = panel.transform.Find("RestartButton")?.GetComponent<Button>();
        if (restartBtn) so.FindProperty("_restartButton").objectReferenceValue = restartBtn;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(panel);
        Debug.Log("[WireSettlementUI] Done! All fields wired.");
    }
}
#endif
