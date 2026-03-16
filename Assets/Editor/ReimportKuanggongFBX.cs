using UnityEditor;
using UnityEngine;

/// <summary>
/// 强制重新导入 kuanggong 所有 FBX，使 Blender 减面结果生效。
/// 用法：Tools → DrscfZ → Reimport Kuanggong FBX
/// </summary>
public class ReimportKuanggongFBX
{
    [MenuItem("Tools/DrscfZ/Reimport Kuanggong FBX")]
    public static void Execute()
    {
        const string FOLDER = "Assets/Res/DGMT_data/Model_yuanwenjian";
        var guids = AssetDatabase.FindAssets("t:Model", new[] { FOLDER });
        int count = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            count++;
        }
        AssetDatabase.Refresh();
        Debug.Log($"[ReimportKuanggongFBX] 强制重导入 {count} 个模型资产完成。");
    }
}
