using UnityEngine;
using UnityEditor;
using DrscfZ.Survival;
using DrscfZ.Monster;

/// <summary>
/// 将新生成的kuanggong Prefab自动赋值到场景中的
/// WorkerManager 和 MonsterWaveSpawner 的 Inspector 字段。
/// 用法：Tools → DrscfZ → Assign Kuanggong Prefabs
/// </summary>
public class AssignKuanggongPrefabs
{
    [MenuItem("Tools/DrscfZ/Assign Kuanggong Prefabs To Scene")]
    public static void Execute()
    {
        // ── 加载新Prefab ─────────────────────────────────────────────
        var worker01 = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Characters/KuanggongWorker_01.prefab");
        var worker02 = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Characters/KuanggongWorker_02.prefab");
        var monster03 = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Monsters/KuanggongMonster_03.prefab");
        var monster04 = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Monsters/KuanggongMonster_04.prefab");
        var boss05 = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Monsters/KuanggongBoss_05.prefab");

        bool anyMissing = worker01 == null || worker02 == null ||
                          monster03 == null || monster04 == null || boss05 == null;
        if (anyMissing)
        {
            Debug.LogError("[AssignKuanggongPrefabs] 部分Prefab未找到，请先运行 'Setup Kuanggong Prefabs'！");
            return;
        }

        // ── 赋值 WorkerManager ───────────────────────────────────────
        var workerMgr = Object.FindObjectOfType<WorkerManager>(true);
        if (workerMgr != null)
        {
            var so = new SerializedObject(workerMgr);
            var p1 = so.FindProperty("_workerPrefab");  if (p1 != null) p1.objectReferenceValue = worker01;
            var p2 = so.FindProperty("_workerPrefab2"); if (p2 != null) p2.objectReferenceValue = worker02;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(workerMgr);
            Debug.Log("[AssignKuanggongPrefabs] WorkerManager 已赋值: worker01 + worker02");
        }
        else
        {
            Debug.LogWarning("[AssignKuanggongPrefabs] 场景中未找到 WorkerManager");
        }

        // ── 赋值 MonsterWaveSpawner ──────────────────────────────────
        var spawner = Object.FindObjectOfType<MonsterWaveSpawner>(true);
        if (spawner != null)
        {
            var so = new SerializedObject(spawner);
            var q1 = so.FindProperty("_monsterPrefab");  if (q1 != null) q1.objectReferenceValue = monster03;
            var q2 = so.FindProperty("_monsterPrefab2"); if (q2 != null) q2.objectReferenceValue = monster04;
            var q3 = so.FindProperty("_bossPrefab");     if (q3 != null) q3.objectReferenceValue = boss05;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(spawner);
            Debug.Log("[AssignKuanggongPrefabs] MonsterWaveSpawner 已赋值: monster03 + monster04 + boss05");
        }
        else
        {
            Debug.LogWarning("[AssignKuanggongPrefabs] 场景中未找到 MonsterWaveSpawner");
        }

        // ── 保存场景 ──────────────────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[AssignKuanggongPrefabs] 场景已保存。全部赋值完成！");
    }
}
