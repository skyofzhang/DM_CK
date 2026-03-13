using UnityEngine;
using UnityEditor;

public class FindGateTransform
{
    public static string Execute()
    {
        var sb = new System.Text.StringBuilder();
        // Find all objects at roughly Z=-4 (near the gate)
        var allObjects = GameObject.FindObjectsOfType<Transform>();
        foreach (var t in allObjects)
        {
            string name = t.name.ToLower();
            if (name.Contains("gate") || name.Contains("chengmen") || name.Contains("chengqiang"))
            {
                sb.AppendLine($"Found: {GetPath(t)} pos={t.position}");
            }
        }
        return sb.Length > 0 ? sb.ToString() : "No gate objects found in scene.";
    }

    static string GetPath(Transform t)
    {
        string path = t.name;
        Transform cur = t.parent;
        while (cur != null) { path = cur.name + "/" + path; cur = cur.parent; }
        return path;
    }
}
