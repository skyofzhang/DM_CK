using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class Temp_VerifyAndFix
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

        void SetCenter(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            EditorUtility.SetDirty(rt);
        }

        void SetFullStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            EditorUtility.SetDirty(rt);
        }

        var sb = new System.Text.StringBuilder();

        // ===== CHECK GameUIPanel =====
        sb.AppendLine("=== GameUIPanel BEFORE ===");
        sb.AppendLine(Report("Canvas/GameUIPanel"));

        var gameUIPanel = FindRT("Canvas/GameUIPanel");
        if (gameUIPanel != null)
        {
            bool isFullstretch = gameUIPanel.anchorMin == Vector2.zero && gameUIPanel.anchorMax == Vector2.one
                && gameUIPanel.anchoredPosition == Vector2.zero && gameUIPanel.sizeDelta == Vector2.zero;
            if (!isFullstretch)
            {
                SetFullStretch(gameUIPanel);
                sb.AppendLine("  → FIXED: GameUIPanel set to fullscreen stretch");
            }
            else sb.AppendLine("  → OK: already fullscreen stretch");
        }

        // ===== FIX BottomBar buttons =====
        // HTML 14 has buttons in upper portion of 280px panel.
        // First button row in HTML: center at y ≈ +40 from panel center (280px panel).
        // Move all game-control buttons from y=0 to y=+60 (upper half).
        // StatusText has bottom-anchor, stays at y=5. No change needed.
        sb.AppendLine("\n=== BottomBar BEFORE FIX ===");
        var bbBefore = new string[] {
            "Canvas/BottomBar/BtnConnect", "Canvas/BottomBar/BtnStart",
            "Canvas/BottomBar/BtnSimulate", "Canvas/BottomBar/BtnReset",
            "Canvas/BottomBar/BtnPlayerData"
        };
        foreach (var p in bbBefore) sb.AppendLine(Report(p));

        int fixedCount = 0;
        foreach (var btnPath in bbBefore)
        {
            var rt = FindRT(btnPath);
            if (rt != null && rt.anchoredPosition.y == 0)
            {
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 60f);
                EditorUtility.SetDirty(rt);
                fixedCount++;
            }
        }
        sb.AppendLine($"\n  → FIXED: moved {fixedCount} buttons from y=0 → y=60 (upper portion of 280px panel)");

        sb.AppendLine("\n=== BottomBar AFTER FIX ===");
        foreach (var p in bbBefore) sb.AppendLine(Report(p));

        // ===== Check SurvivalSettlementPanel itself =====
        sb.AppendLine("\n=== SurvivalSettlementPanel ===");
        sb.AppendLine(Report("Canvas/SurvivalSettlementPanel"));

        // ===== Check ScreenA, B, C are fullscreen =====
        sb.AppendLine("\n=== ScreenA/B/C ===");
        sb.AppendLine(Report("Canvas/SurvivalSettlementPanel/ScreenA"));
        sb.AppendLine(Report("Canvas/SurvivalSettlementPanel/ScreenB"));
        sb.AppendLine(Report("Canvas/SurvivalSettlementPanel/ScreenC"));

        // ===== HTML 03 check - GameUIPanel children =====
        sb.AppendLine("\n=== GameUIPanel children ===");
        var gpRT = FindRT("Canvas/GameUIPanel");
        if (gpRT != null)
        {
            sb.AppendLine($"  [GameUIPanel has {gpRT.childCount} children:]");
            for (int i = 0; i < gpRT.childCount; i++)
            {
                var child = gpRT.GetChild(i) as RectTransform;
                if (child != null)
                    sb.AppendLine($"    child[{i}]: {child.name} anchor({child.anchorMin.x:F2},{child.anchorMin.y:F2})-({child.anchorMax.x:F2},{child.anchorMax.y:F2}) pos({child.anchoredPosition.x:F0},{child.anchoredPosition.y:F0}) size({child.sizeDelta.x:F0},{child.sizeDelta.y:F0})");
            }
        }

        // Save scene
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        sb.AppendLine("\n✅ Scene saved");

        Debug.Log("[VerifyAndFix]\n" + sb.ToString());
    }
}
