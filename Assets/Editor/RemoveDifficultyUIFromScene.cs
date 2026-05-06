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

            // ---- 1. 全场景 GameObject 扫描（不限 Canvas 子树）----
            //   修复 r2：DifficultyChangeButton 父级是 BroadcasterPanelController（独立根节点，非 Canvas 子树），
            //   原 Canvas-only BFS 漏抓。改为遍历活动场景全部 GameObject 名匹配。
            var activeScene = SceneManager.GetActiveScene();
            var allRoots = activeScene.GetRootGameObjects();
            var allGOs = new System.Collections.Generic.List<GameObject>();
            foreach (var root in allRoots)
                CollectAllChildren(root.transform, allGOs);

            // ---- 2. 销毁 DifficultySelect / DifficultySelectPanel（兜底两个名字）----
            string[] diffSelectNames = { "DifficultySelect", "DifficultySelectPanel" };
            int selectDestroyed = DestroyByNames(allGOs, diffSelectNames);
            if (selectDestroyed > 0) { destroyed += selectDestroyed; }
            else { Debug.Log("[RemoveDifficultyUIFromScene] DifficultySelect / DifficultySelectPanel 均未找到（已清理）"); notFound++; }

            // ---- 3. 销毁 DifficultyChangeButton（含层级 BroadcasterPanelController/DifficultyChangeButton）----
            //   修复 r2：父级 BroadcasterPanelController（L205698）非 Canvas 子树
            string[] diffChangeNames = { "DifficultyChangeButton" };
            int changeDestroyed = DestroyByNames(allGOs, diffChangeNames);
            if (changeDestroyed > 0) { destroyed += changeDestroyed; }
            else { Debug.Log("[RemoveDifficultyUIFromScene] DifficultyChangeButton 未找到（已清理）"); notFound++; }

            // ---- 4. 标脏并保存场景（CLAUDE.md 铁律 #3） ----
            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);

            Debug.Log($"[RemoveDifficultyUIFromScene] 完成：销毁 {destroyed} 个节点，{notFound} 个未找到（已清理）；场景保存={saved}");
        }

        /// <summary>递归收集 root 及全部后代 GameObject 到列表。</summary>
        private static void CollectAllChildren(Transform root, System.Collections.Generic.List<GameObject> bag)
        {
            if (root == null) return;
            bag.Add(root.gameObject);
            for (int i = 0; i < root.childCount; i++)
                CollectAllChildren(root.GetChild(i), bag);
        }

        /// <summary>按名字数组从全场景列表中销毁所有匹配的 GameObject，返回销毁数量。</summary>
        private static int DestroyByNames(System.Collections.Generic.List<GameObject> bag, string[] names)
        {
            int n = 0;
            // 复制一份列表（销毁会改 children 顺序）
            var snapshot = new System.Collections.Generic.List<GameObject>(bag);
            foreach (var go in snapshot)
            {
                if (go == null) continue;
                foreach (var name in names)
                {
                    if (go.name == name)
                    {
                        Debug.Log($"[RemoveDifficultyUIFromScene] 销毁 {GetPath(go.transform)}（节点名={name}）");
                        Object.DestroyImmediate(go);
                        n++;
                        break;
                    }
                }
            }
            return n;
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
