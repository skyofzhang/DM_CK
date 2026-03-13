using UnityEngine;
using UnityEditor;

/// <summary>
/// 将 SnowGround 的 sharedMaterial 设置为纯白雪地色，并移除纹理贴图
/// </summary>
public class FixSnowGround
{
    public static string Execute()
    {
        var log = new System.Text.StringBuilder();

        var snowGround = GameObject.Find("SnowGround");
        if (snowGround == null)
        {
            log.AppendLine("❌ SnowGround not found");
            return log.ToString();
        }

        var renderer = snowGround.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            log.AppendLine("❌ MeshRenderer not found on SnowGround");
            return log.ToString();
        }

        // 优先查找已有的 Mat_Snow 材质
        var matPath = AssetDatabase.FindAssets("Mat_Snow t:Material");
        Material mat = null;

        if (matPath.Length > 0)
        {
            var path = AssetDatabase.GUIDToAssetPath(matPath[0]);
            mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            log.AppendLine($"✅ Found Mat_Snow at: {path}");
        }

        if (mat == null)
        {
            // 创建新的雪地白色材质
            string savePath = "Assets/Materials/Mat_Snow.mat";
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.name = "Mat_Snow";
            AssetDatabase.CreateAsset(mat, savePath);
            log.AppendLine($"✅ Created Mat_Snow at: {savePath}");
        }

        // 清除纹理，设置纯净雪白色（淡蓝白）
        mat.mainTexture = null;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", new Color(0.94f, 0.96f, 1.0f, 1f));
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", new Color(0.94f, 0.96f, 1.0f, 1f));

        // 降低金属感和光滑度，模拟雪地漫反射
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", 0f);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0.1f);

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        // 赋给 sharedMaterial（在编辑器中永久生效）
        renderer.sharedMaterial = mat;
        EditorUtility.SetDirty(snowGround);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        log.AppendLine("✅ SnowGround sharedMaterial set to snow-white Mat_Snow (texture cleared)");
        return log.ToString();
    }
}
