using UnityEditor;
using UnityEngine;

public class CheckSurvivalRankingPanelUnderCanvas
{
    [MenuItem("Tools/Check SurvivalRankingPanel Under Canvas")]
    public static void CheckPanel()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.Log("Canvas not found in the scene.");
            return;
        }
        var panel = canvas.transform.Find("SurvivalRankingPanel");
        if (panel != null)
        {
            Debug.Log("SurvivalRankingPanel exists under Canvas.");
        }
        else
        {
            Debug.Log("SurvivalRankingPanel does NOT exist under Canvas.");
        }
    }
}
