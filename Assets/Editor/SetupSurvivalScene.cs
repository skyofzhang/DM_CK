#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using DrscfZ.Survival;
using DrscfZ.Monster;
using DrscfZ.UI;

/// <summary>
/// 极地生存法则 — 场景一键配置
/// 执行：DrscfZ/Setup Survival Scene
/// 做：
///   1. 清理场景中的残留CowWorker实例
///   2. 创建 [SurvivalManagers] 及子系统
///   3. 连接SurvivalGameManager各子系统引用
///   4. WorkerManager连接CowWorker预制体 + 堡垒中心
///   5. 隐藏旧角力UI元素
///   6. 隐藏旧角力Manager
///   7. 把SurvivalTopBarUI挂到TopBar，连接TMP字段
///   8. 标记场景Dirty
/// </summary>
public class SetupSurvivalScene
{
    [MenuItem("DrscfZ/Setup Survival Scene", false, 250)]
    public static void Execute()
    {
        // ==================== 1. 清理残留CowWorker ====================
        CleanupStrayWorkers();

        // ==================== 2. 创建 [SurvivalManagers] ====================
        var managersParent = CreateOrFindGO("[SurvivalManagers]", Vector3.zero);

        var survivalGMGO = CreateOrFindChild(managersParent, "SurvivalGameManager");
        var survivalGM   = EnsureComponent<SurvivalGameManager>(survivalGMGO);

        var resourceGO   = CreateOrFindChild(managersParent, "ResourceSystem");
        var resourceSys  = EnsureComponent<ResourceSystem>(resourceGO);

        var gateGO       = CreateOrFindChild(managersParent, "CityGateSystem");
        var gateSys      = EnsureComponent<CityGateSystem>(gateGO);

        var dayNightGO   = CreateOrFindChild(managersParent, "DayNightCycleManager");
        var dayNightMgr  = EnsureComponent<DayNightCycleManager>(dayNightGO);

        var monsterWaveGO = CreateOrFindChild(managersParent, "MonsterWaveSpawner");
        var waveSpawner   = EnsureComponent<MonsterWaveSpawner>(monsterWaveGO);

        var workerMgrGO  = CreateOrFindChild(managersParent, "WorkerManager");
        var workerMgr    = EnsureComponent<WorkerManager>(workerMgrGO);

        Debug.Log("[SetupScene] ✅ [SurvivalManagers] 子系统GameObject创建完毕");

        // ==================== 3. 连接 SurvivalGameManager 引用 ====================
        var soGM = new SerializedObject(survivalGM);
        soGM.FindProperty("dayNightManager"  ).objectReferenceValue = dayNightMgr;
        soGM.FindProperty("resourceSystem"   ).objectReferenceValue = resourceSys;
        soGM.FindProperty("cityGateSystem"   ).objectReferenceValue = gateSys;
        soGM.FindProperty("monsterWaveSpawner").objectReferenceValue = waveSpawner;
        soGM.FindProperty("workerManager"    ).objectReferenceValue = workerMgr;
        soGM.ApplyModifiedProperties();
        Debug.Log("[SetupScene] ✅ SurvivalGameManager 引用连接完毕");

        // ==================== 4. 连接 WorkerManager 引用 ====================
        var soWM = new SerializedObject(workerMgr);

        // 4a. CowWorker 预制体
        var cowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/CowWorker.prefab");
        if (cowPrefab != null)
        {
            soWM.FindProperty("_workerPrefab").objectReferenceValue = cowPrefab;
            Debug.Log("[SetupScene] ✅ WorkerManager: CowWorker.prefab 连接成功");
        }
        else
            Debug.LogWarning("[SetupScene] ⚠ 未找到 CowWorker.prefab，WorkerManager将使用胶囊体占位");

        // 4b. 堡垒中心
        var fortress = GameObject.Find("CentralFortress");
        if (fortress != null)
        {
            soWM.FindProperty("_fortressCenter").objectReferenceValue = fortress.transform;
            Debug.Log("[SetupScene] ✅ WorkerManager: _fortressCenter → CentralFortress");
        }

        // 4c. 城门修复点 → CityGate_Main
        var gateMain = GameObject.Find("CityGate_Main");
        if (gateMain != null)
            soWM.FindProperty("_gateTarget").objectReferenceValue = gateMain.transform;

        soWM.ApplyModifiedProperties();

        // ==================== 5. 隐藏旧角力 Manager ====================
        string[] oldManagers = {
            "[Managers]/GameManager",
            "[Managers]/CapybaraSpawner",
            "[Managers]/ForceSystem",
            "[Managers]/CampSystem",
            "[Managers]/GiftHandler",
            "[Managers]/UIManager",
            "[Managers]/FootDustManager",
            "[Managers]/RankingSystem",
        };
        foreach (var path in oldManagers)
            DisableGO(path);
        Debug.Log("[SetupScene] ✅ 旧角力Manager已禁用");

        // ==================== 6. 隐藏旧角力 UI 元素 ====================
        // TopBar 内部旧元素
        string[] oldTopBarChildren = {
            "Canvas/GameUIPanel/TopBar/TopBarBg",
            "Canvas/GameUIPanel/TopBar/ScorePoolBg",
            "Canvas/GameUIPanel/TopBar/ScorePoolText",
            "Canvas/GameUIPanel/TopBar/LeftForceText",
            "Canvas/GameUIPanel/TopBar/RightForceText",
            "Canvas/GameUIPanel/TopBar/TimerBg",
        };
        foreach (var path in oldTopBarChildren)
            DisableGO(path);

        // GameUIPanel 内旧元素
        string[] oldPanelElements = {
            "Canvas/GameUIPanel/LeftPlayerList",
            "Canvas/GameUIPanel/RightPlayerList",
            "Canvas/GameUIPanel/WinStreakLeftBg",
            "Canvas/GameUIPanel/WinStreakLeft",
            "Canvas/GameUIPanel/WinStreakRightBg",
            "Canvas/GameUIPanel/WinStreakRight",
            "Canvas/GameUIPanel/PlayerListUI",
            "Canvas/GameUIPanel/JoinNotification",
        };
        foreach (var path in oldPanelElements)
            DisableGO(path);
        Debug.Log("[SetupScene] ✅ 旧角力UI元素已隐藏");

        // ==================== 7. TopBar: 移除旧TopBarUI，挂载SurvivalTopBarUI ====================
        SetupTopBarUI();

        // ==================== 8. 保存场景 ====================
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[SetupScene] ===== 极地生存场景配置完毕！请 Ctrl+S 保存场景 =====");
        Debug.Log("[SetupScene] [SurvivalManagers] 已创建: SurvivalGameManager / ResourceSystem / CityGateSystem / DayNightCycleManager / MonsterWaveSpawner / WorkerManager");
    }

    // ==================== TopBar UI 设置 ====================

    static void SetupTopBarUI()
    {
        var topBarGO = GameObject.Find("TopBar");
        if (topBarGO == null)
        {
            // 通过路径查找
            var canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                var panel = canvas.transform.Find("GameUIPanel");
                if (panel != null)
                    topBarGO = panel.Find("TopBar")?.gameObject;
            }
        }
        if (topBarGO == null)
        {
            Debug.LogError("[SetupScene] 未找到 TopBar GameObject");
            return;
        }

        // legacy-r22 清理：旧 DrscfZ.UI.TopBarUI 类已删除（chore commit 65e7259），不再尝试 GetComponent
        // 场景中残留旧 TopBarUI Component 会变 Missing Script，由 CleanupLegacyScene 编辑器工具一并清理

        // 添加 SurvivalTopBarUI
        var newUI = topBarGO.GetComponent<SurvivalTopBarUI>()
            ?? topBarGO.AddComponent<SurvivalTopBarUI>();

        // 连接字段（通过 SerializedObject）
        var so = new SerializedObject(newUI);

        // phaseText → TopBar/PhaseText
        so.FindProperty("phaseText").objectReferenceValue =
            FindTMPInPath("Canvas/GameUIPanel/TopBar/PhaseText");

        // timerText → TopBar/TimerText（复用旧计时器）
        so.FindProperty("timerText").objectReferenceValue =
            FindTMPInPath("Canvas/GameUIPanel/TopBar/TimerText");

        // 资源显示Text（ResourceRow/XxxIcon/Value）
        so.FindProperty("foodText").objectReferenceValue =
            FindTMPInPath("Canvas/GameUIPanel/TopBar/ResourceRow/FoodIcon/Value");
        so.FindProperty("coalText").objectReferenceValue =
            FindTMPInPath("Canvas/GameUIPanel/TopBar/ResourceRow/CoalIcon/Value");
        so.FindProperty("oreText").objectReferenceValue =
            FindTMPInPath("Canvas/GameUIPanel/TopBar/ResourceRow/OreIcon/Value");
        so.FindProperty("furnaceTempText").objectReferenceValue =
            FindTMPInPath("Canvas/GameUIPanel/TopBar/ResourceRow/HeatIcon/Value");
        so.FindProperty("gateHpText").objectReferenceValue =
            FindTMPInPath("Canvas/GameUIPanel/TopBar/ResourceRow/GateIcon/Value");

        // playerCountText → 暂无独立节点，留null
        // Image字段（furnaceFillBar / gateHpBar等）留null（代码有null检查）

        so.ApplyModifiedProperties();

        Debug.Log("[SetupScene] ✅ SurvivalTopBarUI 挂载并连接完毕");
        Debug.Log("[SetupScene]   phaseText/timerText/foodText/coalText/oreText/furnaceTempText/gateHpText 均已连接");
    }

    // ==================== 工具方法 ====================

    static void CleanupStrayWorkers()
    {
        // 清理 Execute 失败留下的 CowWorker 实例（非Prefab资产）
        var stray = GameObject.Find("CowWorker");
        while (stray != null)
        {
            Debug.Log($"[SetupScene] 清理残留 CowWorker 实例：{stray.name}");
            Object.DestroyImmediate(stray);
            stray = GameObject.Find("CowWorker");
        }
    }

    static GameObject CreateOrFindGO(string name, Vector3 pos)
    {
        var existing = GameObject.Find(name);
        if (existing != null) return existing;
        var go = new GameObject(name);
        go.transform.position = pos;
        return go;
    }

    static GameObject CreateOrFindChild(GameObject parent, string childName)
    {
        var t = parent.transform.Find(childName);
        if (t != null) return t.gameObject;
        var child = new GameObject(childName);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    static T EnsureComponent<T>(GameObject go) where T : MonoBehaviour
    {
        return go.GetComponent<T>() ?? go.AddComponent<T>();
    }

    static void DisableGO(string hierarchyPath)
    {
        // 按层级路径查找（仅支持单层路径拆分查找）
        var parts = hierarchyPath.Split('/');
        Transform current = null;
        foreach (var part in parts)
        {
            if (current == null)
            {
                var root = GameObject.Find(part);
                if (root == null) return;
                current = root.transform;
            }
            else
            {
                current = current.Find(part);
                if (current == null) return;
            }
        }
        if (current != null && current.gameObject.activeSelf)
        {
            current.gameObject.SetActive(false);
            Debug.Log($"[SetupScene] 隐藏: {hierarchyPath}");
        }
    }

    static TMPro.TextMeshProUGUI FindTMPInPath(string path)
    {
        var parts = path.Split('/');
        Transform current = null;
        foreach (var part in parts)
        {
            if (current == null)
            {
                var root = GameObject.Find(part);
                if (root == null)
                {
                    Debug.LogWarning($"[SetupScene] 路径未找到: {path} (在 '{part}' 断开)");
                    return null;
                }
                current = root.transform;
            }
            else
            {
                current = current.Find(part);
                if (current == null)
                {
                    Debug.LogWarning($"[SetupScene] 路径未找到: {path} (在 '{part}' 断开)");
                    return null;
                }
            }
        }
        return current?.GetComponent<TMPro.TextMeshProUGUI>();
    }
}
#endif
