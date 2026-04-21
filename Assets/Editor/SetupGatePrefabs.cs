using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using DrscfZ.Survival;

/// <summary>
/// Tools → DrscfZ → Setup Gate Prefabs
///
/// 将 Assets/Prefabs/Gate/Gate_Lv{1..6}.prefab 批量绑定到场景 CityGateSystem._gateModels。
///
/// 处理规则：
///   - 缺失的 Prefab 仅 LogWarning，不报错（美术资源由用户后续提供）
///   - 已有实例时保留（不重复 Instantiate）
///   - 使用 SerializedObject 写私有字段 _gateModels[]
///   - 场景保存：EditorSceneManager.SaveScene()
///   - 禁止 EditorUtility.DisplayDialog
/// </summary>
public static class SetupGatePrefabs
{
    private const string PREFAB_PATH_FMT = "Assets/Prefabs/Gate/Gate_Lv{0}.prefab";
    private const int    MAX_LEVEL       = 6;

    [MenuItem("Tools/DrscfZ/Setup Gate Prefabs")]
    public static void Execute()
    {
        // ── 1. 找场景中 CityGateSystem ───────────────────────────────────────
        var gate = GameObject.FindObjectOfType<CityGateSystem>(true);
        if (gate == null)
        {
            Debug.LogError("[SetupGatePrefabs] 未在场景中找到 CityGateSystem 实例，请先挂载。");
            return;
        }

        // ── 2. 加载 6 个 Prefab（缺失不报错）─────────────────────────────────
        var prefabs = new GameObject[MAX_LEVEL];
        int loaded = 0;
        for (int i = 0; i < MAX_LEVEL; i++)
        {
            string path = string.Format(PREFAB_PATH_FMT, i + 1);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[SetupGatePrefabs] 未找到 {path}（美术资源待落地，跳过）");
            }
            else
            {
                prefabs[i] = prefab;
                loaded++;
            }
        }

        // ── 3. 在场景中查找/创建 GateModels 容器 ─────────────────────────────
        //     位置：CityGateSystem 下的 "GateModels" 子节点
        var gateTr = gate.transform;
        var containerTr = gateTr.Find("GateModels");
        if (containerTr == null)
        {
            var containerGo = new GameObject("GateModels");
            Undo.RegisterCreatedObjectUndo(containerGo, "Create GateModels container");
            containerGo.transform.SetParent(gateTr, false);
            containerGo.transform.localPosition = Vector3.zero;
            containerGo.transform.localRotation = Quaternion.identity;
            containerGo.transform.localScale    = Vector3.one;
            containerTr = containerGo.transform;
        }

        // ── 4. 实例化每个等级的 Prefab 到容器下（已存在则保留，缺失只是跳过）─
        var modelInstances = new GameObject[MAX_LEVEL];
        for (int i = 0; i < MAX_LEVEL; i++)
        {
            string lvName = $"Gate_Lv{i + 1}";
            var existing = containerTr.Find(lvName);
            GameObject instance = existing != null ? existing.gameObject : null;

            if (instance == null && prefabs[i] != null)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[i], containerTr);
                if (instance != null)
                {
                    Undo.RegisterCreatedObjectUndo(instance, "Create Gate Lv instance");
                    instance.name = lvName;
                    instance.transform.localPosition = Vector3.zero;
                    instance.transform.localRotation = Quaternion.identity;
                    // 默认仅 Lv1 可见，其他隐藏
                    instance.SetActive(i == 0);
                }
            }
            else if (instance != null && prefabs[i] != null)
            {
                // 已存在：不覆盖其 transform，但保持默认显隐（仅 Lv1 active）
                instance.SetActive(i == 0);
            }

            modelInstances[i] = instance;
        }

        // ── 5. 使用 SerializedObject 写私有字段 _gateModels[] ────────────────
        var so = new SerializedObject(gate);
        var arrProp = so.FindProperty("_gateModels");
        if (arrProp == null)
        {
            Debug.LogError("[SetupGatePrefabs] CityGateSystem._gateModels 字段不存在。请确认 CityGateSystem.cs 已更新。");
            return;
        }
        arrProp.arraySize = MAX_LEVEL;
        for (int i = 0; i < MAX_LEVEL; i++)
        {
            var elem = arrProp.GetArrayElementAtIndex(i);
            elem.objectReferenceValue = modelInstances[i]; // 可为 null
        }
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(gate);

        // ── 6. 保存场景 ──────────────────────────────────────────────────────
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        bool saved = EditorSceneManager.SaveScene(activeScene);

        Debug.Log($"[SetupGatePrefabs] 绑定完成：{loaded}/{MAX_LEVEL} 个 Prefab 已加载，场景保存={saved}");
    }
}
