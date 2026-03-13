using UnityEditor;
using UnityEngine;
using TMPro;

public class TestSettlementPanel
{
    public static string Execute()
    {
        // 找 inactive 的 SurvivalSettlementPanel
        GameObject panel = null;
        var all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var p in all)
        {
            if (p.name == "SurvivalSettlementPanel" && p.scene.IsValid())
            {
                panel = p;
                break;
            }
        }
        if (panel == null) return "Panel not found";

        panel.SetActive(true);

        var screenA = panel.transform.Find("ScreenA");
        var screenB = panel.transform.Find("ScreenB");
        var screenC = panel.transform.Find("ScreenC");
        if (screenA) screenA.gameObject.SetActive(true);
        if (screenB) screenB.gameObject.SetActive(false);
        if (screenC) screenC.gameObject.SetActive(false);

        // 填入测试文字
        var resultTitle = panel.transform.Find("ScreenA/ResultTitle")?.GetComponent<TextMeshProUGUI>();
        if (resultTitle) resultTitle.text = "极地已守护！";

        var resultSub = panel.transform.Find("ScreenA/ResultSubtitle")?.GetComponent<TextMeshProUGUI>();
        if (resultSub) resultSub.text = "全体英雄，辛苦了！";

        EditorUtility.SetDirty(panel);
        return "ScreenA activated for screenshot";
    }
}
