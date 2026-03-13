using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// One-click setup: creates X_guai01 prefab and wires up MonsterWaveSpawner in scene.
/// Menu: 极地生存 / Setup Monster System
/// </summary>
public class SetupMonsterSystem
{
    [MenuItem("极地生存/Setup Monster System")]
    public static void Run()
    {
        // ----------------------------------------------------------------
        // 1. Ensure Prefabs/Monsters folder exists
        // ----------------------------------------------------------------
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Monsters"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Monsters");

        // ----------------------------------------------------------------
        // 2. Load X_guai01 FBX and create prefab
        // ----------------------------------------------------------------
        const string fbxPath    = "Assets/Models/juese/X_guai01/X_guai01.fbx";
        const string prefabPath = "Assets/Prefabs/Monsters/X_guai01.prefab";

        GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxAsset == null)
        {
            Debug.LogError("[SetupMonsterSystem] FBX not found: " + fbxPath);
            return;
        }

        // Instantiate into scene temporarily so we can save as prefab
        GameObject tempInst = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
        if (tempInst == null)
        {
            // Fallback: plain instantiate
            tempInst = Object.Instantiate(fbxAsset);
        }
        tempInst.name = "X_guai01";

        // Try to assign the animator controller if found alongside the FBX
        const string controllerPath = "Assets/Models/juese/X_guai01/X_guai01.controller";
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        if (controller != null)
        {
            var anim = tempInst.GetComponentInChildren<Animator>();
            if (anim != null) anim.runtimeAnimatorController = controller;
        }

        // Add MonsterController component so spawner can call Initialize
        if (tempInst.GetComponent<DrscfZ.Monster.MonsterController>() == null)
            tempInst.AddComponent<DrscfZ.Monster.MonsterController>();

        // Save as prefab asset
        bool prefabSuccess;
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(tempInst, prefabPath, out prefabSuccess);
        Object.DestroyImmediate(tempInst);

        if (!prefabSuccess || prefabAsset == null)
        {
            Debug.LogError("[SetupMonsterSystem] Failed to save prefab: " + prefabPath);
            return;
        }
        Debug.Log("[SetupMonsterSystem] Prefab saved: " + prefabPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ----------------------------------------------------------------
        // 3. Find MonsterWaveSpawner in scene
        // ----------------------------------------------------------------
        var spawnerGO = GameObject.Find("[SurvivalManagers]/MonsterWaveSpawner");
        if (spawnerGO == null)
        {
            // Fallback: search all
            var spawnerComp = Object.FindObjectOfType<DrscfZ.Monster.MonsterWaveSpawner>();
            if (spawnerComp != null) spawnerGO = spawnerComp.gameObject;
        }
        if (spawnerGO == null)
        {
            Debug.LogError("[SetupMonsterSystem] MonsterWaveSpawner not found in scene.");
            return;
        }
        Debug.Log("[SetupMonsterSystem] Found spawner: " + spawnerGO.name);

        // ----------------------------------------------------------------
        // 4. Create spawn point GameObjects (or reuse if they already exist)
        // ----------------------------------------------------------------
        // 城门在 Z=-4，怪物必须从城门外侧（Z << -4）刷新，向内移动穿过城门攻击基地。
        // 原来 Z=6/10 是堡垒内部（错误），已修正为 Z=-8/-10（城门外侧）。
        Transform spawnLeft  = GetOrCreateSpawnPoint(spawnerGO.transform, "SpawnLeft",  new Vector3(-8f, 0f, -8f));
        Transform spawnRight = GetOrCreateSpawnPoint(spawnerGO.transform, "SpawnRight", new Vector3( 8f, 0f, -8f));
        Transform spawnTop   = GetOrCreateSpawnPoint(spawnerGO.transform, "SpawnTop",   new Vector3( 0f, 0f, -10f));

        // ----------------------------------------------------------------
        // 5. Find CityGate_Main
        // ----------------------------------------------------------------
        GameObject cityGateGO = GameObject.Find("PolarScene/CityGate_Main");
        if (cityGateGO == null)
            cityGateGO = GameObject.Find("CityGate_Main");
        if (cityGateGO == null)
            Debug.LogWarning("[SetupMonsterSystem] CityGate_Main not found; _cityGateTarget will remain None.");

        // ----------------------------------------------------------------
        // 6. Assign fields via SerializedObject
        // ----------------------------------------------------------------
        var so = new SerializedObject(spawnerGO.GetComponent<DrscfZ.Monster.MonsterWaveSpawner>());
        so.Update();

        var propPrefab     = so.FindProperty("_monsterPrefab");
        var propLeft       = so.FindProperty("_spawnLeft");
        var propRight      = so.FindProperty("_spawnRight");
        var propTop        = so.FindProperty("_spawnTop");
        var propGateTarget = so.FindProperty("_cityGateTarget");

        if (propPrefab     != null) propPrefab.objectReferenceValue     = prefabAsset;
        if (propLeft       != null) propLeft.objectReferenceValue       = spawnLeft;
        if (propRight      != null) propRight.objectReferenceValue      = spawnRight;
        if (propTop        != null) propTop.objectReferenceValue        = spawnTop;
        if (propGateTarget != null && cityGateGO != null)
            propGateTarget.objectReferenceValue = cityGateGO.transform;

        so.ApplyModifiedProperties();

        // ----------------------------------------------------------------
        // 7. Mark scene dirty and save
        // ----------------------------------------------------------------
        EditorSceneManager.MarkSceneDirty(spawnerGO.scene);
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[SetupMonsterSystem] Done. MonsterWaveSpawner fields assigned and scene saved.");
    }

    // ----------------------------------------------------------------
    // Helper: find or create a child spawn point transform
    // ----------------------------------------------------------------
    private static Transform GetOrCreateSpawnPoint(Transform parent, string pointName, Vector3 localPos)
    {
        // Check if already exists as child
        Transform existing = parent.Find(pointName);
        if (existing != null)
        {
            existing.localPosition = localPos;
            Debug.Log($"[SetupMonsterSystem] Reused existing spawn point: {pointName}");
            return existing;
        }

        GameObject go = new GameObject(pointName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        Debug.Log($"[SetupMonsterSystem] Created spawn point: {pointName} at local {localPos}");
        return go.transform;
    }
}
