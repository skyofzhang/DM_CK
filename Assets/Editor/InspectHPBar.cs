using UnityEngine;
using UnityEditor;

public class InspectHPBar
{
    public static void Execute()
    {
        var sb = new System.Text.StringBuilder();

        // ── Worker ──────────────────────────────────────────────
        var worker = GameObject.Find("WorkerPool/Worker_00");
        if (worker == null) worker = GameObject.Find("Worker_00");
        if (worker != null)
        {
            sb.AppendLine("=== Worker_00 hierarchy ===");
            PrintHierarchy(worker.transform, 0, sb);
        }
        else sb.AppendLine("Worker_00 not found");

        // ── Monster ──────────────────────────────────────────────
        // 找场景里任意一个 MonsterController
        var mc = Object.FindObjectOfType<DrscfZ.Monster.MonsterController>(true);
        if (mc != null)
        {
            sb.AppendLine($"\n=== Monster: {mc.gameObject.name} hierarchy ===");
            PrintHierarchy(mc.transform, 0, sb);
        }
        else sb.AppendLine("No MonsterController found in scene");

        Debug.Log(sb.ToString());
    }

    static void PrintHierarchy(Transform t, int depth, System.Text.StringBuilder sb)
    {
        string indent = new string(' ', depth * 2);
        // 只显示有意义的节点，跳过骨骼（名字含 mixamorig 等）
        bool isBone = t.name.Contains("mixamorig") || t.name.Contains("Hips") || t.name.Contains("Spine");
        string tag = isBone ? " [BONE]" : "";
        sb.AppendLine($"{indent}{t.name}{tag}  pos={t.position:F2}  localPos={t.localPosition:F2}");

        // 递归，但跳过骨骼的子骨骼（只显示一层骨骼）
        foreach (Transform child in t)
        {
            if (depth < 3 || !isBone)
                PrintHierarchy(child, depth + 1, sb);
        }
    }
}
