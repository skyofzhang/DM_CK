using UnityEditor;
using UnityEngine;

public class QuickCheckSurvivalRankingUI
{
    [MenuItem("Tools/QuickCheckSurvivalRankingUI")]
    public static void Check()
    {
        bool compile = !EditorUtility.scriptCompilationFailed;
        var canvas = GameObject.Find("Canvas");
        bool panel = canvas != null && canvas.transform.Find("SurvivalRankingPanel") != null;
        bool rankingUI = canvas != null && canvas.GetComponent("SurvivalRankingUI") != null;
        Debug.Log($"[CHECK] compile={compile} panel={panel} rankingUI={rankingUI}");
    }
}
