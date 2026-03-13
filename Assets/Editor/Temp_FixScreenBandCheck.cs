using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 1. 检查 LiveRankingPanel 在场景中的真实层级路径（Panel 01 诊断）
/// 2. 删除 ScreenB 中的 RankingTitle 和 RankingList（Panel 06 修复）
/// 3. 检查 JoinNotification 当前尺寸（Panel 11 诊断）
/// 4. 保存场景
/// </summary>
public class Temp_FixScreenBandCheck
{
    public static void Execute()
    {
        // ─── 工具函数 ───────────────────────────────────────────────
        RectTransform FindRT(string path)
        {
            var parts = path.Split('/');
            Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (var t in all)
            {
                if (t.name != parts[parts.Length - 1]) continue;
                bool match = true;
                Transform cur = t;
                for (int i = parts.Length - 1; i >= 0; i--)
                {
                    if (cur == null || cur.name != parts[i]) { match = false; break; }
                    cur = cur.parent;
                }
                if (match) return t as RectTransform;
            }
            return null;
        }

        string FullPath(Transform t)
        {
            string p = t.name;
            var parent = t.parent;
            while (parent != null) { p = parent.name + "/" + p; parent = parent.parent; }
            return p;
        }

        string RtInfo(RectTransform rt)
            => $"anchor({rt.anchorMin.x:F2},{rt.anchorMin.y:F2})-({rt.anchorMax.x:F2},{rt.anchorMax.y:F2}) " +
               $"pos({rt.anchoredPosition.x:F0},{rt.anchoredPosition.y:F0}) " +
               $"size({rt.sizeDelta.x:F0},{rt.sizeDelta.y:F0})";

        var sb = new System.Text.StringBuilder();

        // ══════════════════════════════════════════════════════════
        // 1. 探测 LiveRankingPanel 真实位置（Panel 01 诊断）
        // ══════════════════════════════════════════════════════════
        sb.AppendLine("═══ LiveRankingPanel 层级诊断 ═══");
        bool foundLRP = false;

        // 先尝试已知可能路径
        string[] knownPaths = {
            "Canvas/LiveRankingPanel",
            "Canvas/GameUIPanel/LiveRankingPanel",
            "Canvas/LobbyPanel/LiveRankingPanel",
        };
        foreach (var kp in knownPaths)
        {
            var r = FindRT(kp);
            if (r != null)
            {
                sb.AppendLine($"  ✅ FOUND at: {kp}");
                sb.AppendLine($"     {RtInfo(r)}");
                sb.AppendLine($"     activeSelf={r.gameObject.activeSelf}  activeInHier={r.gameObject.activeInHierarchy}");
                foundLRP = true;
            }
        }

        // 如果常规路径找不到，全场搜索
        if (!foundLRP)
        {
            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t.name != "LiveRankingPanel") continue;
                var rt = t as RectTransform;
                if (rt != null)
                {
                    sb.AppendLine($"  ✅ FOUND (全场搜索): {FullPath(t)}");
                    sb.AppendLine($"     {RtInfo(rt)}");
                    sb.AppendLine($"     activeSelf={rt.gameObject.activeSelf}  activeInHier={rt.gameObject.activeInHierarchy}");
                    foundLRP = true;
                }
            }
        }

        if (!foundLRP)
            sb.AppendLine("  ❌ LiveRankingPanel: 整场景未找到");

        // ══════════════════════════════════════════════════════════
        // 2. 删除 ScreenB 中的 RankingTitle / RankingList（Panel 06 修复）
        //    HTML06 原型无此两元素，它们是多余添加且会覆盖按钮区
        // ══════════════════════════════════════════════════════════
        sb.AppendLine("\n═══ ScreenB 修复（删除 HTML06 无对应的额外元素）═══");

        var screenB = FindRT("Canvas/SurvivalSettlementPanel/ScreenB");
        if (screenB != null)
        {
            sb.AppendLine($"修复前：ScreenB 共 {screenB.childCount} 个子元素");
            for (int i = 0; i < screenB.childCount; i++)
                sb.AppendLine($"  [{i}] {screenB.GetChild(i).name}");
        }
        else
        {
            sb.AppendLine("  ❌ ScreenB: 未找到");
        }

        // 删除 RankingTitle
        var rankingTitle = FindRT("Canvas/SurvivalSettlementPanel/ScreenB/RankingTitle");
        if (rankingTitle != null)
        {
            Object.DestroyImmediate(rankingTitle.gameObject);
            sb.AppendLine("  → ✅ DELETED: RankingTitle");
        }
        else
            sb.AppendLine("  → RankingTitle: 未找到（已删除或本来不存在）");

        // 删除 RankingList
        var rankingList = FindRT("Canvas/SurvivalSettlementPanel/ScreenB/RankingList");
        if (rankingList != null)
        {
            Object.DestroyImmediate(rankingList.gameObject);
            sb.AppendLine("  → ✅ DELETED: RankingList");
        }
        else
            sb.AppendLine("  → RankingList: 未找到（已删除或本来不存在）");

        // 报告修复后的 ScreenB
        screenB = FindRT("Canvas/SurvivalSettlementPanel/ScreenB");
        if (screenB != null)
        {
            sb.AppendLine($"\n修复后：ScreenB 共 {screenB.childCount} 个子元素");
            for (int i = 0; i < screenB.childCount; i++)
                sb.AppendLine($"  [{i}] {screenB.GetChild(i).name}");
        }

        // ══════════════════════════════════════════════════════════
        // 3. 检查 JoinNotification 当前状态（Panel 11 诊断）
        // ══════════════════════════════════════════════════════════
        sb.AppendLine("\n═══ JoinNotification 诊断 ═══");
        var jn = FindRT("Canvas/JoinNotification");
        if (jn == null) jn = FindRT("Canvas/GameUIPanel/JoinNotification");
        if (jn != null)
        {
            sb.AppendLine($"  FOUND: {FullPath(jn.transform)}");
            sb.AppendLine($"  {RtInfo(jn)}");
            // HTML11期望: full-width stretch (0,0)-(1,0) h=260 pos.y=0
            // 当前Unity: bc(0,20,700,280)
            bool fullWidthAnchor = (jn.anchorMin.x < 0.1f && jn.anchorMax.x > 0.9f);
            sb.AppendLine($"  横向拉伸: {(fullWidthAnchor ? "✅ 是" : "❌ 否（HTML11期望全宽度）")}");
            sb.AppendLine($"  HTML11期望: bottom-stretch h=260 pos.y=0（zone全宽底部渐变区）");
        }
        else
            sb.AppendLine("  ❌ JoinNotification: 未找到");

        // ══════════════════════════════════════════════════════════
        // 4. 检查 BottomBar 子元素（Panel 14 现状确认）
        // ══════════════════════════════════════════════════════════
        sb.AppendLine("\n═══ BottomBar 子元素确认 ═══");
        var bb = FindRT("Canvas/BottomBar");
        if (bb != null)
        {
            sb.AppendLine($"BottomBar: {RtInfo(bb)}");
            sb.AppendLine($"子元素共 {bb.childCount} 个：");
            for (int i = 0; i < bb.childCount; i++)
            {
                var child = bb.GetChild(i) as RectTransform;
                if (child != null)
                    sb.AppendLine($"  [{i}] {child.name}  pos({child.anchoredPosition.x:F0},{child.anchoredPosition.y:F0}) size({child.sizeDelta.x:F0},{child.sizeDelta.y:F0})");
            }
            sb.AppendLine("  HTML14期望: Row1=4个按钮(▶开始/⏸暂停/⏹结束/🔄重置) Row2=5个按钮(T1/T3/T5/冻结/怪物)");
        }
        else
            sb.AppendLine("  ❌ BottomBar: 未找到");

        // ══════════════════════════════════════════════════════════
        // 5. 保存场景
        // ══════════════════════════════════════════════════════════
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        sb.AppendLine("\n✅ Scene saved");

        Debug.Log("[FixScreenBandCheck]\n" + sb.ToString());
    }
}
