using UnityEditor;
using UnityEngine;

public class CheckCanvasSiblings
{
    public static string Execute()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return "Canvas not found";

        var result = new System.Text.StringBuilder();
        int childCount = canvas.transform.childCount;
        result.AppendLine($"Canvas has {childCount} children:");

        for (int i = 0; i < childCount; i++)
        {
            var child = canvas.transform.GetChild(i);
            result.AppendLine($"  [{i}] {child.name} (active={child.gameObject.activeSelf})");
        }

        return result.ToString();
    }
}
