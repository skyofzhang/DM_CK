using UnityEngine;
using UnityEditor;
using UnityEngine.Video;

public class DiagnoseTier2
{
    [MenuItem("Tools/DrscfZ/Diagnose Tier2")]
    public static void Execute()
    {
        string path = "Assets/Art/GiftGifs/tier2.webm";

        // 方法1：GUID加载
        var guids = AssetDatabase.FindAssets("tier2 t:VideoClip", new[]{"Assets/Art/GiftGifs"});
        Debug.Log($"[Diag] FindAssets t:VideoClip 找到 {guids.Length} 个");
        foreach (var g in guids)
            Debug.Log($"  {AssetDatabase.GUIDToAssetPath(g)}");

        // 方法2：LoadAllAssetsAtPath
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
        Debug.Log($"[Diag] LoadAllAssetsAtPath 返回 {allAssets.Length} 个对象");
        foreach (var a in allAssets)
            Debug.Log($"  {a.GetType().Name}: {a.name}");

        // 方法3：直接加载Object类型
        var objAsset = AssetDatabase.LoadMainAssetAtPath(path);
        Debug.Log($"[Diag] LoadMainAssetAtPath type={objAsset?.GetType().Name ?? "null"}");

        // 检查 importer
        var importer = AssetImporter.GetAtPath(path);
        Debug.Log($"[Diag] AssetImporter type={importer?.GetType().Name ?? "null"}");
    }
}
