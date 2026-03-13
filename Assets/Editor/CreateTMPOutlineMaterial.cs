using UnityEngine;
using UnityEditor;
using TMPro;
using System.IO;

/// <summary>
/// 为生存游戏创建带描边+阴影的 TMP 材质预设，并一键应用到场景所有 TMP 文字组件。
/// 菜单：Tools/Phase2/Create TMP Outline Material
/// </summary>
public class CreateTMPOutlineMaterial
{
    private const string FONT_PATH      = "Fonts/ChineseFont SDF";
    private const string MAT_SAVE_PATH  = "Assets/Resources/Fonts/ChineseFont SDF - Outline.mat";

    [MenuItem("Tools/Phase2/Create TMP Outline Material")]
    public static void Execute()
    {
        // 1. 加载字体 Asset
        var fontAsset = Resources.Load<TMP_FontAsset>(FONT_PATH);
        if (fontAsset == null)
        {
            Debug.LogError($"[TMP Outline] 找不到字体: Resources/{FONT_PATH}，请确认路径");
            return;
        }

        // 2. 基于字体原始 Material 创建新材质（保留相同 Shader + 纹理引用）
        var baseMat    = fontAsset.material;
        var outlineMat = new Material(baseMat);
        outlineMat.name = "ChineseFont SDF - Outline";

        // 3. 设置描边
        outlineMat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
        outlineMat.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);

        // 4. 启用 Underlay（阴影）
        outlineMat.EnableKeyword("UNDERLAY_ON");
        outlineMat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX,   0.5f);
        outlineMat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY,  -0.5f);
        outlineMat.SetFloat(ShaderUtilities.ID_UnderlaySoftness,  0.1f);
        outlineMat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.75f));

        // 5. 保存材质
        var dir = Path.GetDirectoryName(MAT_SAVE_PATH);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        // 已存在则覆盖，避免重复
        if (File.Exists(MAT_SAVE_PATH))
        {
            AssetDatabase.DeleteAsset(MAT_SAVE_PATH);
        }
        AssetDatabase.CreateAsset(outlineMat, MAT_SAVE_PATH);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[TMP Outline] 材质已保存: {MAT_SAVE_PATH}");

        // 6. 重新加载刚保存的材质（确保是持久化资产，不是临时对象）
        var savedMat = AssetDatabase.LoadAssetAtPath<Material>(MAT_SAVE_PATH);
        if (savedMat == null)
        {
            Debug.LogError("[TMP Outline] 材质保存后无法重新加载，中止");
            return;
        }

        // 7. 应用到场景所有 TextMeshProUGUI 组件
        var allUI = Object.FindObjectsOfType<TextMeshProUGUI>(true); // includeInactive=true
        int uiCount = 0;
        foreach (var tmp in allUI)
        {
            tmp.fontSharedMaterial = savedMat;
            EditorUtility.SetDirty(tmp);
            uiCount++;
        }

        // 8. 应用到场景所有 3D TextMeshPro 组件（WorldSpace Canvas 或 3D 文字）
        var all3D = Object.FindObjectsOfType<TextMeshPro>(true);
        int d3Count = 0;
        foreach (var tmp in all3D)
        {
            tmp.fontSharedMaterial = savedMat;
            EditorUtility.SetDirty(tmp);
            d3Count++;
        }

        // 9. 保存场景
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[TMP Outline] 完成！UI文字={uiCount}个，3D文字={d3Count}个，描边+阴影材质已应用，场景已保存。");
    }
}
