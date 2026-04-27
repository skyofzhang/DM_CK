#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DrscfZ.Editor
{
    /// <summary>
    /// legacy-r22 场景清理工具：删除场景中残留的 Legacy 旧节点 + 所有 Missing Script Component
    /// 触发：Tools/DrscfZ/Cleanup Legacy Scene
    /// </summary>
    public static class CleanupLegacyScene
    {
        // 已知 Legacy GameObject 名（从 audit-r22 v22 文档清单 + Agent E grep 验证得出）
        // 这些是 inactive 残留，挂载已删的 Legacy MonoBehaviour（删 .cs 后变 Missing Script）
        private static readonly string[] LegacyGameObjectNames = new[]
        {
            "CapybaraSpawner",
            "ForceSystem",
            "GameManager",                  // Core/GameManager.cs 已删
            "GiftHandler",                  // Systems/GiftHandler.cs 已删
            "BarrageSimulator",             // Systems/BarrageSimulator.cs 已删
            "VFXSpawner",                   // VFX/VFXSpawner.cs 已删
            "FootDustManager",              // VFX/FootDustManager.cs 已删
            "UIManager",                    // UI/UIManager.cs 已删
            "PlayerListUI",
            "MainMenuUI",
            "LoadingScreenUI",
            "UpgradeNotificationUI",
            "ForceBoostNotificationUI",
            "RankingPanelUI",
            "PlayerDataPanelUI",
            "PlayerJoinNotificationUI",
            "OrangeSpeedHUD_Root",          // SceneGenerator.CreateOrUpdateSpeedHUD 创建的
        };

        [MenuItem("Tools/DrscfZ/Cleanup Legacy Scene")]
        public static void Execute()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("[CleanupLegacyScene] 没有活动场景");
                return;
            }

            int deletedGameObjects = 0;
            int removedMissingScripts = 0;
            var deletedNames = new List<string>();

            // ---------- Step 1：删 Legacy GameObject（按名匹配，含 inactive）----------
            var nameSet = new HashSet<string>(LegacyGameObjectNames);
            var allRoots = scene.GetRootGameObjects();
            // 收集所有 GO（含子级、含 inactive），递归
            var allGOs = new List<GameObject>();
            foreach (var root in allRoots) CollectAllGameObjects(root.transform, allGOs);

            foreach (var go in allGOs)
            {
                if (go == null) continue;
                if (nameSet.Contains(go.name))
                {
                    deletedNames.Add($"{GetGameObjectPath(go)} (active={go.activeSelf})");
                    Object.DestroyImmediate(go);
                    deletedGameObjects++;
                }
            }

            // ---------- Step 2：扫所有剩余 GO，删 Missing Script Component ----------
            allRoots = scene.GetRootGameObjects(); // refresh after deletion
            allGOs.Clear();
            foreach (var root in allRoots) CollectAllGameObjects(root.transform, allGOs);

            foreach (var go in allGOs)
            {
                if (go == null) continue;
                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                if (removed > 0)
                {
                    removedMissingScripts += removed;
                    Debug.Log($"[CleanupLegacyScene] {GetGameObjectPath(go)} 移除 {removed} 个 Missing Script Component");
                }
            }

            // ---------- Step 3：标脏 + 保存 ----------
            if (deletedGameObjects > 0 || removedMissingScripts > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            // ---------- 报告 ----------
            Debug.Log($"[CleanupLegacyScene] === 场景清理完成 ===");
            Debug.Log($"[CleanupLegacyScene] 删除 Legacy GameObject: {deletedGameObjects} 个");
            foreach (var n in deletedNames) Debug.Log($"[CleanupLegacyScene]   - {n}");
            Debug.Log($"[CleanupLegacyScene] 移除 Missing Script Component: {removedMissingScripts} 个");
            Debug.Log($"[CleanupLegacyScene] 场景已保存: {scene.path}");
        }

        private static void CollectAllGameObjects(Transform t, List<GameObject> output)
        {
            output.Add(t.gameObject);
            for (int i = 0; i < t.childCount; i++)
            {
                CollectAllGameObjects(t.GetChild(i), output);
            }
        }

        private static string GetGameObjectPath(GameObject go)
        {
            if (go == null) return "<null>";
            var path = go.name;
            var t = go.transform.parent;
            while (t != null)
            {
                path = t.name + "/" + path;
                t = t.parent;
            }
            return path;
        }
    }
}
#endif
