using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace DrscfZ.Editor
{
    /// <summary>
    /// Tools → DrscfZ → Remove Difficulty UI From Scene
    ///
    /// v1.27 §14 / §17.5 / §34.4 E9 废止 difficulty 三档系统的场景清理脚本（一次性使用）。
    ///
    /// 功能：
    ///   1. 找到并销毁 Canvas/GameUIPanel/DifficultySelectPanel（DifficultySelectUI 挂载点）
    ///   2. 找到并销毁 Canvas/BroadcasterPanel/DifficultyChangeButton（DifficultyChangeButtonUI 挂载点）
    ///   3. 标脏并保存当前场景（EditorSceneManager.SaveScene）
    ///
    /// 兜底：节点不存在视为已清理（不报错），可重复运行幂等。
    ///
    /// 注意（CLAUDE.md 铁律）：
    ///   - 不直接编辑 .unity YAML（高风险）
    ///   - 不在 worktree 下 Unity 中跑（PM 在主 repo 合并后执行）
    ///   - 禁用 EditorUtility.DisplayDialog（阻塞进程）
    ///   - 用 EditorSceneManager.SaveScene() 保存（CLAUDE.md 关键设计决策 #3）
    /// </summary>
    public static class RemoveDifficultyUIFromScene
    {
        [MenuItem("Tools/DrscfZ/Remove Difficulty UI From Scene")]
        public static void Run()
        {
            int destroyed = 0;
            int notFound  = 0;

            // ---- 1. 找 Canvas（场景层级根） ----
            GameObject canvasGO = null;
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (!go.scene.isLoaded) continue;
                if (PrefabUtility.IsPartOfPrefabAsset(go)) continue;
                if (go.name == "Canvas" && go.transform.parent == null)
                {
                    canvasGO = go;
                    break;
                }
            }

            if (canvasGO == null)
            {
                Debug.LogWarning("[RemoveDifficultyUIFromScene] Canvas 未找到，终止。");
                return;
            }

            // ---- 2. 销毁 GameUIPanel/DifficultySelect（实际 Scene 节点名，无 Panel 后缀；同时兜底 DifficultySelectPanel）----
            //   PM 复核时 grep MainScene.unity 确认 m_Name=DifficultySelect (line 149064)
            string[] diffSelectNames = { "DifficultySelect", "DifficultySelectPanel" };
            bool diffSelectFound = false;
            foreach (var name in diffSelectNames)
            {
                var diffSelectPanel = FindDeepChild(canvasGO.transform, name);
                if (diffSelectPanel != null)
                {
                    Debug.Log($"[RemoveDifficultyUIFromScene] 销毁 {GetPath(diffSelectPanel)}（节点名={name}）");
                    Object.DestroyImmediate(diffSelectPanel.gameObject);
                    destroyed++;
                    diffSelectFound = true;
                    break;
                }
            }
            if (!diffSelectFound)
            {
                Debug.Log("[RemoveDifficultyUIFromScene] DifficultySelect / DifficultySelectPanel 均未找到（已清理）");
                notFound++;
            }

            // ---- 3. 销毁 BroadcasterPanel/DifficultyChangeButton ----
            var diffChangeBtn = FindDeepChild(canvasGO.transform, "DifficultyChangeButton");
            if (diffChangeBtn != null)
            {
                Debug.Log($"[RemoveDifficultyUIFromScene] 销毁 {GetPath(diffChangeBtn)}");
                Object.DestroyImmediate(diffChangeBtn.gameObject);
                destroyed++;
            }
            else
            {
                Debug.Log("[RemoveDifficultyUIFromScene] DifficultyChangeButton 未找到（已清理）");
                notFound++;
            }

            // ---- 4. 标脏并保存场景（CLAUDE.md 铁律 #3） ----
            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);

            Debug.Log($"[RemoveDifficultyUIFromScene] 完成：销毁 {destroyed} 个节点，{notFound} 个未找到（已清理）；场景保存={saved}");
        }

        /// <summary>BFS 深度搜索：找到指定名字的子节点（首个匹配）。</summary>
        private static Transform FindDeepChild(Transform root, string name)
        {
            if (root == null) return null;

            var queue = new System.Collections.Generic.Queue<Transform>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                for (int i = 0; i < cur.childCount; i++)
                {
                    var child = cur.GetChild(i);
                    if (child.name == name) return child;
                    queue.Enqueue(child);
                }
            }
            return null;
        }

        /// <summary>调试用：返回 GameObject 的层级路径（Canvas/GameUIPanel/DifficultySelectPanel 等）。</summary>
        private static string GetPath(Transform t)
        {
            string path = t.name;
            var p = t.parent;
            while (p != null)
            {
                path = p.name + "/" + path;
                p = p.parent;
            }
            return path;
        }
    }
}
