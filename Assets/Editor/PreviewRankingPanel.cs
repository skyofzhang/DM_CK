using UnityEngine;
using UnityEditor;

public static class PreviewRankingPanel
{
    public static void ShowPanel()
    {
        // 确保 LobbyPanel 也激活（提供 Canvas 背景）
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.name == "LobbyPanel" && go.scene.name == "MainScene")
                go.SetActive(true);
        }

        // 激活遮罩和排行榜面板
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.name == "RankingOverlay" && go.scene.name == "MainScene")
                go.SetActive(true);
            if (go.name == "SurvivalRankingPanel" && go.scene.name == "MainScene")
                go.SetActive(true);
        }
    }

    public static void HidePanel()
    {
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.name == "RankingOverlay" && go.scene.name == "MainScene")
                go.SetActive(false);
            if (go.name == "SurvivalRankingPanel" && go.scene.name == "MainScene")
                go.SetActive(false);
            if (go.name == "LobbyPanel" && go.scene.name == "MainScene")
                go.SetActive(false);
        }
    }
}
