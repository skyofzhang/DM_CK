using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 两项性能优化合并脚本：
///   A. 给所有角色 Prefab 添加 LODGroup（距离裁剪）
///   B. 对 kuanggong FBX 设置 High Mesh Compression
///
/// 用法：Tools → DrscfZ → Add LOD Groups + FBX Compression
/// </summary>
public class AddLODAndCompression
{
    static readonly string[] CHAR_PREFAB_FOLDERS = {
        "Assets/Prefabs/Characters",
        "Assets/Prefabs/Monsters",
        "Assets/Prefabs/Units",
    };

    // kuanggong 源模型目录
    static readonly string FBX_FOLDER = "Assets/Res/DGMT_data/Model_yuanwenjian";

    // LOD0 保留比例：屏幕高度占比低于此值时裁剪（5% ≈ 1080p下54px）
    const float LOD_CULL_RATIO = 0.05f;

    [MenuItem("Tools/DrscfZ/Add LOD Groups + FBX Compression")]
    public static void Execute()
    {
        int lodAdded    = 0;
        int lodSkipped  = 0;
        int fbxFixed    = 0;

        // ═══════════════════════════════════════════════════
        // A. LOD Group：给所有角色 Prefab 加激进距离裁剪
        // ═══════════════════════════════════════════════════
        foreach (var folder in CHAR_PREFAB_FOLDERS)
        {
            if (!Directory.Exists(folder)) continue;

            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            foreach (var guid in guids)
            {
                var path   = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                // 已有 LODGroup 则跳过
                if (prefab.GetComponent<LODGroup>() != null)
                {
                    lodSkipped++;
                    continue;
                }

                // 收集所有 Renderer（SkinnedMeshRenderer + MeshRenderer）
                var renderers = prefab.GetComponentsInChildren<Renderer>(true);
                if (renderers == null || renderers.Length == 0) continue;

                // 添加 LODGroup
                var lodGroup = prefab.AddComponent<LODGroup>();

                // LOD0：100% → 5%，显示全部 Renderer
                var lod0 = new LOD(LOD_CULL_RATIO, renderers);
                lodGroup.SetLODs(new LOD[] { lod0 });
                lodGroup.RecalculateBounds();

                EditorUtility.SetDirty(prefab);
                lodAdded++;
            }
        }

        AssetDatabase.SaveAssets();

        // ═══════════════════════════════════════════════════
        // B. FBX Mesh Compression：High（减少顶点精度/冗余）
        // ═══════════════════════════════════════════════════
        if (Directory.Exists(FBX_FOLDER))
        {
            var fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { FBX_FOLDER });
            foreach (var guid in fbxGuids)
            {
                var path     = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;

                bool changed = false;

                // Mesh Compression
                if (importer.meshCompression != ModelImporterMeshCompression.High)
                {
                    importer.meshCompression = ModelImporterMeshCompression.High;
                    changed = true;
                }

                // 关闭 Read/Write（减少内存占用，不需要 CPU 访问 mesh data）
                if (importer.isReadable)
                {
                    importer.isReadable = false;
                    changed = true;
                }

                // 关闭 BlendShapes（角色动画不用 BlendShape）
                if (importer.importBlendShapes)
                {
                    importer.importBlendShapes = false;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                    fbxFixed++;
                }
            }
        }
        else
        {
            Debug.LogWarning($"[AddLODAndCompression] FBX目录不存在：{FBX_FOLDER}");
        }

        Debug.Log($"[AddLODAndCompression] 完成：" +
                  $"LODGroup 新增 {lodAdded} 个（已有跳过 {lodSkipped} 个）| " +
                  $"FBX Compression 修复 {fbxFixed} 个");
    }
}
