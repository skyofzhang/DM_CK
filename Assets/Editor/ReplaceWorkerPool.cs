using UnityEngine;
using UnityEditor;
using DrscfZ.Survival;

/// <summary>
/// 将场景 WorkerPool 下所有旧 CowWorker 实例替换为新 KuanggongWorker 预制体。
/// 用法：Tools → DrscfZ → Replace Worker Pool
/// </summary>
public class ReplaceWorkerPool
{
    const string PREFAB_01 = "Assets/Prefabs/Characters/KuanggongWorker_01.prefab";
    const string PREFAB_02 = "Assets/Prefabs/Characters/KuanggongWorker_02.prefab";

    [MenuItem("Tools/DrscfZ/Replace Worker Pool")]
    public static void Execute()
    {
        var worker01 = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_01);
        var worker02 = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_02);
        if (worker01 == null || worker02 == null)
        {
            Debug.LogError("[ReplaceWorkerPool] 找不到 KuanggongWorker Prefab，请先运行 Setup Kuanggong Prefabs！");
            return;
        }

        // ── 找到 WorkerPool 父节点 ────────────────────────────────────
        var poolGo = GameObject.Find("WorkerPool");
        if (poolGo == null)
        {
            Debug.LogError("[ReplaceWorkerPool] 场景中未找到 WorkerPool GameObject！");
            return;
        }

        // ── 收集子节点快照（Transform列表），避免边删边遍历 ────────────
        int childCount = poolGo.transform.childCount;
        var newWorkers = new WorkerController[childCount];

        for (int i = 0; i < childCount; i++)
        {
            var oldChild = poolGo.transform.GetChild(0); // 每次删第0个，列表自动前移
            string workerName = $"Worker_{i:D2}";
            bool wasActive = oldChild.gameObject.activeSelf;

            // 删除旧实例
            Object.DestroyImmediate(oldChild.gameObject);

            // 交替使用两套外观
            var prefabToUse = (i % 2 == 0) ? worker01 : worker02;
            var newGo = (GameObject)PrefabUtility.InstantiatePrefab(prefabToUse, poolGo.transform);
            newGo.name = workerName;
            newGo.transform.localPosition = Vector3.zero;
            newGo.transform.localRotation = Quaternion.identity;
            newGo.transform.localScale    = Vector3.one;
            newGo.SetActive(wasActive);

            // 确保有 WorkerController 组件（Prefab 若无则动态添加）
            var ctrl = newGo.GetComponent<WorkerController>()
                    ?? newGo.AddComponent<WorkerController>();

            newWorkers[i] = ctrl;
        }

        // ── 更新 WorkerManager._preCreatedWorkers ─────────────────────
        var workerMgr = Object.FindObjectOfType<WorkerManager>(true);
        if (workerMgr != null)
        {
            var so   = new SerializedObject(workerMgr);
            var prop = so.FindProperty("_preCreatedWorkers");
            if (prop != null)
            {
                prop.arraySize = newWorkers.Length;
                for (int i = 0; i < newWorkers.Length; i++)
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = newWorkers[i];
                so.ApplyModifiedProperties();
            }
            EditorUtility.SetDirty(workerMgr);
            Debug.Log($"[ReplaceWorkerPool] WorkerManager._preCreatedWorkers 已更新（{newWorkers.Length} 个）");
        }
        else
        {
            Debug.LogWarning("[ReplaceWorkerPool] 场景中未找到 WorkerManager");
        }

        // ── 保存场景 ─────────────────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[ReplaceWorkerPool] 完成！WorkerPool 下 {childCount} 个 Worker 已替换为 KuanggongWorker。场景已保存。");
    }
}
