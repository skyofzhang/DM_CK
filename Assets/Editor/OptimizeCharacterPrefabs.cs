using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 一键优化所有角色 Prefab（Workers + Monsters）的渲染设置：
///   1. SkinnedMeshRenderer 关闭阴影投射（Shadow casters 547 → 接近 0）
///   2. SkinnedMeshRenderer.updateWhenOffscreen = false
///   3. Animator.cullingMode = CullCompletely
///   4. 同步优化 WorkerPool 下现有场景实例
///   5. 裁剪 WorkerPool 中超出 MAX_WORKERS(12) 的多余实例
/// 用法：Tools → DrscfZ → Optimize Character Prefabs
/// </summary>
public class OptimizeCharacterPrefabs
{
    static readonly string[] PREFAB_FOLDERS = {
        "Assets/Prefabs/Characters",
        "Assets/Prefabs/Monsters",
        "Assets/Prefabs/Units",
    };

    [MenuItem("Tools/DrscfZ/Optimize Character Prefabs")]
    public static void Execute()
    {
        int prefabCount  = 0;
        int smrFixed     = 0;
        int animFixed    = 0;

        // ── 1. 修复 Prefab 资产 ──────────────────────────────────────────
        foreach (var folder in PREFAB_FOLDERS)
        {
            if (!Directory.Exists(folder)) continue;

            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            foreach (var guid in guids)
            {
                var path   = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                bool dirty = false;

                // SkinnedMeshRenderer 优化
                foreach (var smr in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (smr.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off ||
                        smr.updateWhenOffscreen)
                    {
                        smr.shadowCastingMode   = UnityEngine.Rendering.ShadowCastingMode.Off;
                        smr.updateWhenOffscreen = false;
                        smrFixed++;
                        dirty = true;
                    }
                }

                // Animator 优化
                foreach (var anim in prefab.GetComponentsInChildren<Animator>(true))
                {
                    if (anim.cullingMode != AnimatorCullingMode.CullCompletely)
                    {
                        anim.cullingMode = AnimatorCullingMode.CullCompletely;
                        animFixed++;
                        dirty = true;
                    }
                }

                if (dirty)
                {
                    EditorUtility.SetDirty(prefab);
                    prefabCount++;
                }
            }
        }

        AssetDatabase.SaveAssets();

        // ── 2. 同步优化场景里已激活的实例 ───────────────────────────────
        int sceneFixed = 0;
        foreach (var smr in Object.FindObjectsOfType<SkinnedMeshRenderer>(true))
        {
            if (smr.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off ||
                smr.updateWhenOffscreen)
            {
                smr.shadowCastingMode   = UnityEngine.Rendering.ShadowCastingMode.Off;
                smr.updateWhenOffscreen = false;
                sceneFixed++;
            }
        }
        foreach (var anim in Object.FindObjectsOfType<Animator>(true))
        {
            if (anim.cullingMode != AnimatorCullingMode.CullCompletely)
                anim.cullingMode = AnimatorCullingMode.CullCompletely;
        }

        // ── 3. 裁剪 WorkerPool 超出 MAX_WORKERS 的多余实例 ──────────────
        const int MAX_WORKERS = 12;
        var poolGo = GameObject.Find("WorkerPool");
        int removed = 0;
        if (poolGo != null)
        {
            int childCount = poolGo.transform.childCount;
            for (int i = childCount - 1; i >= MAX_WORKERS; i--)
            {
                var child = poolGo.transform.GetChild(i);
                Object.DestroyImmediate(child.gameObject);
                removed++;
            }
            if (removed > 0) EditorUtility.SetDirty(poolGo);
        }

        // ── 4. 保存场景 ───────────────────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log($"[OptimizeCharacterPrefabs] 完成：" +
                  $"修复 Prefab {prefabCount} 个 | SMR {smrFixed} 个 | Animator {animFixed} 个 | " +
                  $"场景实例 SMR {sceneFixed} 个 | " +
                  $"删除多余 Worker {removed} 个。场景已保存。");
    }
}
