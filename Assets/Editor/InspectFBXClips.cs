#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 检查FBX文件中的原始动画片段名称
/// 执行：DrscfZ/Inspect FBX Clips
/// </summary>
public class InspectFBXClips
{
    [MenuItem("DrscfZ/Inspect FBX Clips", false, 200)]
    public static void Execute()
    {
        string[] fbxPaths = {
            "Assets/Models/juese/nn_01/nn_ainim_idle.fbx",
            "Assets/Models/juese/nn_01/nn_ainim_run.fbx",
            "Assets/Models/juese/nn_01/nn_ainim_attack.fbx",
            "Assets/Models/juese/nn_01/nn_ainim_banyun.fbx",
            "Assets/Models/juese/nn_01/nn_ainim_lose.fbx",
            "Assets/Models/juese/X_guai01/X_guai01.fbx",
        };

        foreach (var path in fbxPaths)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[InspectFBX] 未找到: {path}");
                continue;
            }

            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                // 使用默认动画片段（未自定义时）
                var defaultClips = importer.defaultClipAnimations;
                Debug.Log($"[InspectFBX] {System.IO.Path.GetFileName(path)} — 默认片段({defaultClips.Length}个):");
                foreach (var c in defaultClips)
                    Debug.Log($"  → 名称:'{c.name}'  帧:{c.firstFrame}-{c.lastFrame}  Loop:{c.loopTime}");
            }
            else
            {
                Debug.Log($"[InspectFBX] {System.IO.Path.GetFileName(path)} — 自定义片段({clips.Length}个):");
                foreach (var c in clips)
                    Debug.Log($"  → 名称:'{c.name}'  帧:{c.firstFrame}-{c.lastFrame}  Loop:{c.loopTime}");
            }
        }

        // 检查武器挂载参考Prefab结构
        var weaponPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Models/juese/nn_01/武器挂载预制体参考.prefab");
        if (weaponPrefab != null)
        {
            Debug.Log($"[InspectFBX] ===== 武器挂载预制体结构 =====");
            PrintHierarchy(weaponPrefab.transform, 0);
        }

        Debug.Log("[InspectFBX] ===== 检查完成 =====");
    }

    static void PrintHierarchy(Transform t, int depth)
    {
        string indent = new string(' ', depth * 2);
        Debug.Log($"{indent}[{t.name}] pos={t.localPosition} rot={t.localEulerAngles}");
        foreach (Transform child in t)
            PrintHierarchy(child, depth + 1);
    }
}
#endif
