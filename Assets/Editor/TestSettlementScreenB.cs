using UnityEditor;
using UnityEngine;
using TMPro;

public class TestSettlementScreenB
{
    public static string Execute()
    {
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
        if (screenA) screenA.gameObject.SetActive(false);
        if (screenB) screenB.gameObject.SetActive(true);
        if (screenC) screenC.gameObject.SetActive(false);

        // 填测试数据
        var days = panel.transform.Find("ScreenB/SurvivalDaysText")?.GetComponent<TextMeshProUGUI>();
        if (days) days.text = "生存天数: 7";

        var kills = panel.transform.Find("ScreenB/TotalKillsText")?.GetComponent<TextMeshProUGUI>();
        if (kills) kills.text = "击退敌人: 42次";

        var gather = panel.transform.Find("ScreenB/TotalGatherText")?.GetComponent<TextMeshProUGUI>();
        if (gather) gather.text = "资源采集: 380";

        var repair = panel.transform.Find("ScreenB/TotalRepairText")?.GetComponent<TextMeshProUGUI>();
        if (repair) repair.text = "修缮城门: 15次";

        // 填排行榜
        string[] names = { "极地勇士", "冰雪守护者", "暴风行者", "寒冰猎手", "雪原战士" };
        int[] scores = { 2800, 1950, 1430, 980, 720 };
        for (int i = 0; i < 5; i++)
        {
            var entry = panel.transform.Find($"ScreenB/RankingList/RankEntry_{i}");
            if (entry == null) continue;
            var rankTmp = entry.Find("RankText")?.GetComponent<TextMeshProUGUI>();
            var nameTmp = entry.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var scoreTmp = entry.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();
            if (rankTmp) rankTmp.text = $"#{i + 1}";
            if (nameTmp) nameTmp.text = names[i];
            if (scoreTmp) scoreTmp.text = $"{scores[i]}分";
        }

        EditorUtility.SetDirty(panel);
        return "ScreenB activated with test data";
    }
}
