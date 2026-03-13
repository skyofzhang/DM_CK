using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using DrscfZ.Survival;

namespace DrscfZ.Editor
{
    /// <summary>
    /// 一键将 WorkerPool 中 Worker_00-19 的 Capsule 占位符
    /// 替换为 CowWorker.prefab 的角色网格。
    ///
    /// 操作流程：
    ///  1. 移除每个 Worker 上的 MeshFilter / MeshRenderer（Capsule 部分）
    ///  2. 以 CowWorker.prefab 的内容在子对象中实例化（命名 "Body"）
    ///  3. WorkerVisual 的 Awake 会自动通过 GetComponentInChildren 找到
    ///     CowWorker 内的 SkinnedMeshRenderer
    ///  4. 保存场景
    /// </summary>
    public static class FixWorkerMesh
    {
        private const string COWWORKER_PATH = "Assets/Prefabs/Characters/CowWorker.prefab";

        [MenuItem("Tools/DrscfZ/Fix Worker Mesh (Capsule → CowWorker)")]
        public static void Execute()
        {
            // ---- 加载 CowWorker.prefab ----
            var cowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(COWWORKER_PATH);
            if (cowPrefab == null)
            {
                Debug.LogError($"[FixWorkerMesh] CowWorker.prefab 未找到：{COWWORKER_PATH}");
                return;
            }
            Debug.Log($"[FixWorkerMesh] 已加载 CowWorker.prefab");

            // ---- 找 WorkerPool ----
            var workerPool = GameObject.Find("WorkerPool");
            if (workerPool == null)
            {
                // 尝试包含 inactive 对象
                var all = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var go in all)
                {
                    if (go.name == "WorkerPool" && go.scene.IsValid())
                    {
                        workerPool = go;
                        break;
                    }
                }
            }
            if (workerPool == null)
            {
                Debug.LogError("[FixWorkerMesh] WorkerPool not found in scene");
                return;
            }

            int count = 0;
            int childCount = workerPool.transform.childCount;
            Debug.Log($"[FixWorkerMesh] WorkerPool 子对象数量：{childCount}");

            for (int i = 0; i < childCount; i++)
            {
                var worker = workerPool.transform.GetChild(i).gameObject;

                // ---- 跳过 BubbleCanvas ----
                if (!worker.name.StartsWith("Worker_")) continue;

                // ---- 检查是否已有 Body 子对象 ----
                if (worker.transform.Find("Body") != null)
                {
                    Debug.Log($"[FixWorkerMesh] {worker.name} 已有 Body，跳过");
                    continue;
                }

                // ---- 移除 Capsule 的 MeshFilter / MeshRenderer ----
                var mf = worker.GetComponent<MeshFilter>();
                var mr = worker.GetComponent<MeshRenderer>();
                if (mf != null) Object.DestroyImmediate(mf);
                if (mr != null) Object.DestroyImmediate(mr);

                // ---- 实例化 CowWorker，挂到 Worker 下 ----
                // 注意：用 PrefabUtility.InstantiatePrefab 保持 prefab 关联（可在 Inspector 看到 prefab 来源）
                var body = (GameObject)PrefabUtility.InstantiatePrefab(cowPrefab, worker.transform);
                body.name = "Body";

                // 重置局部变换
                body.transform.localPosition = Vector3.zero;
                body.transform.localRotation = Quaternion.identity;
                body.transform.localScale    = Vector3.one;

                EditorUtility.SetDirty(worker);
                count++;
                Debug.Log($"[FixWorkerMesh] ✅ {worker.name}: Capsule → CowWorker.Body 完成");
            }

            // ---- 保存场景（Rule #8）----
            if (count > 0)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[FixWorkerMesh] ✅ 共替换 {count} 个 Worker，场景已保存");
            }
            else
            {
                Debug.Log("[FixWorkerMesh] 无需修改（所有 Worker 已有 Body）");
            }
        }
    }
}
