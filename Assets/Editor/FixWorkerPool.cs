using UnityEngine;
using UnityEditor;
using DrscfZ.Survival;

/// <summary>
/// 工人池绑定修复脚本：
///   1. 找到 [SurvivalManagers]/WorkerManager，绑定 _preCreatedWorkers（Worker_00 ~ Worker_19）
///   2. 绑定 _fortressCenter → PolarScene/CentralFortress
///   3. 在场景各工位处创建可见标记（Sphere + 标签文字）供开发调试
///
/// 菜单：Tools/Phase2/Fix Worker Pool
/// </summary>
public class FixWorkerPool
{
    [MenuItem("Tools/Phase2/Fix Worker Pool")]
    public static void Execute()
    {
        // ── 1. 找到 WorkerManager ────────────────────────────────────────
        var wmGO = GameObject.Find("[SurvivalManagers]/WorkerManager");
        if (wmGO == null)
        {
            // 尝试在根级别找
            wmGO = GameObject.Find("WorkerManager");
            if (wmGO == null)
            {
                Debug.LogError("[FixWorkerPool] 找不到 WorkerManager GameObject！");
                return;
            }
        }

        var wm = wmGO.GetComponent<WorkerManager>();
        if (wm == null)
        {
            Debug.LogError("[FixWorkerPool] WorkerManager 组件不存在！");
            return;
        }

        var so = new SerializedObject(wm);

        // ── 2. 绑定 _preCreatedWorkers（WorkerPool 下的 Worker_00 ~ 19）──
        var workerPool = GameObject.Find("WorkerPool");
        if (workerPool == null)
        {
            // 搜索整个场景
            workerPool = GameObject.Find("[SurvivalManagers]/WorkerPool");
        }

        if (workerPool != null)
        {
            var workersProp = so.FindProperty("_preCreatedWorkers");
            var workerList  = workerPool.GetComponentsInChildren<WorkerController>(true);
            workersProp.arraySize = workerList.Length;
            for (int i = 0; i < workerList.Length; i++)
            {
                workersProp.GetArrayElementAtIndex(i).objectReferenceValue = workerList[i];
            }
            Debug.Log($"[FixWorkerPool] _preCreatedWorkers 绑定 {workerList.Length} 个 Worker ✅");
        }
        else
        {
            Debug.LogWarning("[FixWorkerPool] WorkerPool 父对象未找到，跳过 _preCreatedWorkers 绑定");
        }

        // ── 3. 绑定 _fortressCenter ─────────────────────────────────────
        var fortressGO = GameObject.Find("PolarScene/CentralFortress");
        if (fortressGO != null)
        {
            so.FindProperty("_fortressCenter").objectReferenceValue = fortressGO.transform;
            Debug.Log("[FixWorkerPool] _fortressCenter -> PolarScene/CentralFortress ✅");
        }
        else
        {
            Debug.LogWarning("[FixWorkerPool] PolarScene/CentralFortress 未找到");
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(wm);

        // ── 4. 保存场景 ─────────────────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[FixWorkerPool] 完成！WorkerManager 绑定已修复，场景已保存。");
    }
}
