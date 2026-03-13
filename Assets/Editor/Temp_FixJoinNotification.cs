using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 修复 JoinNotification：
/// 从 bc(0,20,700,280) 改为全宽底部拉伸，h=260，对齐 HTML11 的 .zone 设计
/// HTML11: .zone { bottom:0; width:1080px; height:260px; } —— 全宽底部渐变区
/// 保留 pos.y=0（与BottomBar不重叠由调用方控制）
/// </summary>
public class Temp_FixJoinNotification
{
    public static void Execute()
    {
        RectTransform FindRT(string path)
        {
            var parts = path.Split('/');
            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
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

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== JoinNotification 修复 ===");

        var jn = FindRT("Canvas/GameUIPanel/JoinNotification");
        if (jn == null) jn = FindRT("Canvas/JoinNotification");

        if (jn != null)
        {
            sb.AppendLine($"修复前: anchor({jn.anchorMin.x:F2},{jn.anchorMin.y:F2})-({jn.anchorMax.x:F2},{jn.anchorMax.y:F2}) " +
                          $"pos({jn.anchoredPosition.x:F0},{jn.anchoredPosition.y:F0}) size({jn.sizeDelta.x:F0},{jn.sizeDelta.y:F0})");

            // HTML11 .zone: bottom:0, width:1080px (full), height:260px
            // 改为 bottom-stretch（横向全宽），高度260，底部偏移0
            jn.anchorMin = new Vector2(0f, 0f);   // 横向全拉伸，底部锚定
            jn.anchorMax = new Vector2(1f, 0f);
            jn.pivot     = new Vector2(0.5f, 0f); // 底部中心pivot
            jn.anchoredPosition = new Vector2(0f, 0f); // 紧贴画布底部
            jn.sizeDelta = new Vector2(0f, 260f);  // h=260 匹配HTML11 zone

            EditorUtility.SetDirty(jn);

            sb.AppendLine($"修复后: anchor({jn.anchorMin.x:F2},{jn.anchorMin.y:F2})-({jn.anchorMax.x:F2},{jn.anchorMax.y:F2}) " +
                          $"pos({jn.anchoredPosition.x:F0},{jn.anchoredPosition.y:F0}) size({jn.sizeDelta.x:F0},{jn.sizeDelta.y:F0})");
            sb.AppendLine("  → ✅ 全宽底部拉伸 h=260，对齐 HTML11 .zone");
        }
        else
        {
            sb.AppendLine("  ❌ JoinNotification: 未找到");
        }

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        sb.AppendLine("\n✅ Scene saved");

        Debug.Log("[FixJoinNotification]\n" + sb.ToString());
    }
}
