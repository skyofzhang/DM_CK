using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class Temp_VerifyBatch3
{
    public static void Execute()
    {
        // Helper: find RT by path (works on inactive objects)
        RectTransform FindRT(string path)
        {
            var parts = path.Split('/');
            Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (var t in all)
            {
                if (t.name == parts[parts.Length - 1])
                {
                    // verify full path
                    bool match = true;
                    Transform cur = t;
                    for (int i = parts.Length - 1; i >= 0; i--)
                    {
                        if (cur == null || cur.name != parts[i]) { match = false; break; }
                        cur = cur.parent;
                    }
                    if (match) return t as RectTransform;
                }
            }
            return null;
        }

        string Report(string path)
        {
            var rt = FindRT(path);
            if (rt == null) return $"  {path}: NOT FOUND";
            var name = path.Substring(path.LastIndexOf('/') + 1);
            return $"  {name}: anchor({rt.anchorMin.x:F2},{rt.anchorMin.y:F2})-({rt.anchorMax.x:F2},{rt.anchorMax.y:F2}) " +
                   $"pivot({rt.pivot.x:F2},{rt.pivot.y:F2}) " +
                   $"pos({rt.anchoredPosition.x:F0},{rt.anchoredPosition.y:F0}) " +
                   $"size({rt.sizeDelta.x:F0},{rt.sizeDelta.y:F0})";
        }

        var sb = new System.Text.StringBuilder();

        sb.AppendLine("=== LOBBY PANEL ===");
        sb.AppendLine(Report("Canvas/LobbyPanel/TitleText"));
        sb.AppendLine(Report("Canvas/LobbyPanel/StatusText"));
        sb.AppendLine(Report("Canvas/LobbyPanel/ServerStatus"));
        sb.AppendLine(Report("Canvas/LobbyPanel/StartBtn"));
        sb.AppendLine(Report("Canvas/LobbyPanel/RankingBtn"));
        sb.AppendLine(Report("Canvas/LobbyPanel/SettingsBtn"));

        sb.AppendLine("=== LOADING PANEL CHILDREN ===");
        sb.AppendLine(Report("Canvas/LoadingPanel/LoadingText"));

        sb.AppendLine("=== ANNOUNCEMENT PANEL CHILDREN ===");
        sb.AppendLine(Report("Canvas/AnnouncementPanel/MainText"));
        sb.AppendLine(Report("Canvas/AnnouncementPanel/SubText"));

        sb.AppendLine("=== SETTLEMENT SCREEN A ===");
        sb.AppendLine(Report("Canvas/SurvivalSettlementPanel/ScreenA/ResultTitle"));
        sb.AppendLine(Report("Canvas/SurvivalSettlementPanel/ScreenA/ResultSubtitle"));

        sb.AppendLine("=== SETTLEMENT SCREEN B ===");
        sb.AppendLine(Report("Canvas/SurvivalSettlementPanel/ScreenB/StatsHeader"));
        sb.AppendLine(Report("Canvas/SurvivalSettlementPanel/ScreenB/TotalGatherText"));
        sb.AppendLine(Report("Canvas/SurvivalSettlementPanel/ScreenB/TotalKillsText"));
        sb.AppendLine(Report("Canvas/SurvivalSettlementPanel/ScreenB/TotalRepairText"));
        sb.AppendLine(Report("Canvas/SurvivalSettlementPanel/ScreenB/SurvivalDaysText"));
        sb.AppendLine(Report("Canvas/SurvivalSettlementPanel/ScreenB/RankingTitle"));
        sb.AppendLine(Report("Canvas/SurvivalSettlementPanel/ScreenB/RankingList"));

        sb.AppendLine("=== SETTLEMENT SCREEN C ===");
        sb.AppendLine(Report("Canvas/SurvivalSettlementPanel/ScreenC/MvpLabel"));
        sb.AppendLine(Report("Canvas/SurvivalSettlementPanel/ScreenC/MvpNameText"));
        sb.AppendLine(Report("Canvas/SurvivalSettlementPanel/ScreenC/MvpScoreText"));
        sb.AppendLine(Report("Canvas/SurvivalSettlementPanel/ScreenC/MvpAnchorLine"));
        sb.AppendLine(Report("Canvas/SurvivalSettlementPanel/ScreenC/Top3Slot_0"));
        sb.AppendLine(Report("Canvas/SurvivalSettlementPanel/ScreenC/Top3Slot_1"));
        sb.AppendLine(Report("Canvas/SurvivalSettlementPanel/ScreenC/Top3Slot_2"));

        sb.AppendLine("=== RESTART BUTTON ===");
        sb.AppendLine(Report("Canvas/SurvivalSettlementPanel/RestartButton"));

        sb.AppendLine("=== BOTTOMBAR CHILDREN ===");
        // Try common child names
        var bbPaths = new string[] {
            "Canvas/BottomBar/BtnConnect",
            "Canvas/BottomBar/BtnStart",
            "Canvas/BottomBar/BtnReset",
            "Canvas/BottomBar/BtnSimulate",
            "Canvas/BottomBar/StatusText",
        };
        foreach (var p in bbPaths) sb.AppendLine(Report(p));

        // Also list all children of BottomBar
        var bbRT = FindRT("Canvas/BottomBar");
        if (bbRT != null)
        {
            sb.AppendLine($"  [BottomBar has {bbRT.childCount} children:]");
            for (int i = 0; i < bbRT.childCount; i++)
            {
                var child = bbRT.GetChild(i) as RectTransform;
                if (child != null)
                    sb.AppendLine($"    child[{i}]: {child.name} pos({child.anchoredPosition.x:F0},{child.anchoredPosition.y:F0}) size({child.sizeDelta.x:F0},{child.sizeDelta.y:F0})");
            }
        }

        sb.AppendLine("=== VIP ANNOUNCEMENT CHILDREN ===");
        var vipRT = FindRT("Canvas/GameUIPanel/VIPAnnouncement");
        if (vipRT == null) vipRT = FindRT("Canvas/VIPAnnouncement");
        if (vipRT != null)
        {
            sb.AppendLine($"  [VIPAnnouncement has {vipRT.childCount} children:]");
            for (int i = 0; i < vipRT.childCount; i++)
            {
                var child = vipRT.GetChild(i) as RectTransform;
                if (child != null)
                    sb.AppendLine($"    child[{i}]: {child.name} anchor({child.anchorMin.x:F2},{child.anchorMin.y:F2})-({child.anchorMax.x:F2},{child.anchorMax.y:F2}) pos({child.anchoredPosition.x:F0},{child.anchoredPosition.y:F0}) size({child.sizeDelta.x:F0},{child.sizeDelta.y:F0})");
            }
        }
        else sb.AppendLine("  VIPAnnouncement: NOT FOUND");

        sb.AppendLine("=== JOIN NOTIFICATION CHILDREN ===");
        var jnRT = FindRT("Canvas/GameUIPanel/JoinNotification");
        if (jnRT == null) jnRT = FindRT("Canvas/JoinNotification");
        if (jnRT != null)
        {
            sb.AppendLine($"  [JoinNotification has {jnRT.childCount} children:]");
            for (int i = 0; i < jnRT.childCount; i++)
            {
                var child = jnRT.GetChild(i) as RectTransform;
                if (child != null)
                    sb.AppendLine($"    child[{i}]: {child.name} anchor({child.anchorMin.x:F2},{child.anchorMin.y:F2})-({child.anchorMax.x:F2},{child.anchorMax.y:F2}) pos({child.anchoredPosition.x:F0},{child.anchoredPosition.y:F0}) size({child.sizeDelta.x:F0},{child.sizeDelta.y:F0})");
            }
        }

        Debug.Log("[VerifyBatch3]\n" + sb.ToString());
    }
}
