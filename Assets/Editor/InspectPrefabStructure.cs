using UnityEngine;
using UnityEditor;
using System.Text;

public class InspectPrefabStructure
{
    public static void Execute()
    {
        string[] prefabs = {
            "Assets/Prefabs/Characters/CowWorker.prefab",
            "Assets/Prefabs/Monsters/X_guai01.prefab",
        };
        foreach (var path in prefabs)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) { Debug.LogError($"[InspectPrefab] NOT FOUND: {path}"); continue; }
            var sb = new StringBuilder();
            sb.AppendLine($"\n=== {go.name} ({path}) ===");
            PrintHierarchy(go.transform, sb, "  ");
            Debug.Log(sb.ToString());
        }
    }

    static void PrintHierarchy(Transform t, StringBuilder sb, string indent)
    {
        var comps = t.GetComponents<Component>();
        var compNames = new StringBuilder();
        foreach (var c in comps)
            if (c != null) compNames.Append($"{c.GetType().Name} ");
        sb.AppendLine($"{indent}[{t.name}] scale={t.localScale}  comps: {compNames}");
        foreach (Transform child in t)
            PrintHierarchy(child, sb, indent + "  ");
    }
}
