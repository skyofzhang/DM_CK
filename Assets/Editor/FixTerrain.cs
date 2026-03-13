using UnityEngine;
using UnityEditor;

/// <summary>
/// 禁用沙地 Terrain，扩大 SnowGround Quad 到 300×300 覆盖全场景
/// </summary>
public class FixTerrain
{
    public static string Execute()
    {
        var log = new System.Text.StringBuilder();

        // 1. 禁用沙地 Terrain
        var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in allObjects)
        {
            if (go.name == "Terrain" && go.GetComponent<UnityEngine.Terrain>() != null)
            {
                go.SetActive(false);
                EditorUtility.SetDirty(go);
                log.AppendLine($"✅ Disabled Terrain: {go.name} (was 300×300 sandy terrain)");
            }
        }

        // 2. 扩大 SnowGround Quad — 优先用 PolarScene/SnowGround
        GameObject snowGround = null;
        foreach (var go in allObjects)
        {
            if (go.name == "SnowGround" && go.activeInHierarchy)
            {
                // 优先 PolarScene 下的
                if (go.transform.parent != null && go.transform.parent.name == "PolarScene")
                {
                    snowGround = go;
                    break;
                }
                snowGround = go; // fallback
            }
        }

        if (snowGround == null)
        {
            // 尝试找 inactive 的也可
            foreach (var go in allObjects)
            {
                if (go.name == "SnowGround")
                {
                    snowGround = go;
                    break;
                }
            }
        }

        if (snowGround != null)
        {
            // 将 SnowGround Quad 扩大到 400×400（覆盖整个沙地地形范围）
            snowGround.transform.localScale = new Vector3(400f, 400f, 1f);
            snowGround.transform.position = new Vector3(0f, 0.01f, 0f);  // 略高于 y=0 避免Z-fighting

            // 确认 Mat_Snow 已分配
            var rend = snowGround.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                log.AppendLine($"✅ SnowGround material: {(rend.sharedMaterial != null ? rend.sharedMaterial.name : "None")}");
                log.AppendLine($"✅ SnowGround scale set to (400, 400, 1)");
            }

            EditorUtility.SetDirty(snowGround);
        }
        else
        {
            log.AppendLine("⚠️ SnowGround not found");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        return log.ToString();
    }
}
