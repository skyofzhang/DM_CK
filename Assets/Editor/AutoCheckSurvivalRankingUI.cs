using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.Collections;

public class AutoCheckSurvivalRankingUI
{
    [MenuItem("Tools/AutoCheckSurvivalRankingUI/Run")] // 仅用于手动调试
    public static void Execute()
    {
        RunCheck();
    }

    [InitializeOnLoadMethod]
    static void RunCheck()
    {
        var c = GameObject.Find("Canvas");
        var panel = c?.transform.Find("SurvivalRankingPanel");
        var rankUI = c?.GetComponent("DrscfZ.UI.SurvivalRankingUI");
        Debug.Log("[AUTO] panel=" + (panel != null) + " rankUI=" + (rankUI != null));
    }
}
