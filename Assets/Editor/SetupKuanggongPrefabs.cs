using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 一键生成5个新版矿工角色Prefab：
///   Worker: KuanggongWorker_01 / KuanggongWorker_02
///   Monster: KuanggongMonster_03 / KuanggongMonster_04
///   Boss:   KuanggongBoss_05
///
/// 用法：Unity菜单 → Tools → DrscfZ → Setup Kuanggong Prefabs
/// </summary>
public class SetupKuanggongPrefabs
{
    // ── 源 Prefab 路径 ──────────────────────────────────────────
    const string SRC_01 = "Assets/Res/DGMT_data/Model_juese(use)/kuanggong/kuanggong_01.prefab";
    const string SRC_02 = "Assets/Res/DGMT_data/Model_juese(use)/kuanggong/kuanggong_02.prefab";
    const string SRC_03 = "Assets/Res/DGMT_data/Model_juese(use)/kuanggong/kuanggong_03.prefab";
    const string SRC_04 = "Assets/Res/DGMT_data/Model_juese(use)/kuanggong/kuanggong_04.prefab";
    const string SRC_05 = "Assets/Res/DGMT_data/Model_juese(use)/kuanggong/kuanggong_05.prefab";

    // ── 参考 Prefab（复制组件结构） ───────────────────────────────
    const string REF_WORKER  = "Assets/Prefabs/Characters/CowWorker.prefab";
    const string REF_MONSTER = "Assets/Prefabs/Monsters/X_guai01.prefab";

    // ── 输出路径 ─────────────────────────────────────────────────
    const string OUT_WORKER  = "Assets/Prefabs/Characters";
    const string OUT_MONSTER = "Assets/Prefabs/Monsters";

    // ── 缩放配置 ─────────────────────────────────────────────────
    // Worker: 目标高度 ~1.8m，源模型高约123-137单位
    const float WORKER_SCALE  = 0.015f;
    // Monster: 目标高度 ~1.2m
    const float MONSTER_SCALE = 0.01f;
    // Boss: 目标高度 ~2.5m（显眼的大Boss）
    const float BOSS_SCALE    = 0.018f;

    // ── Y轴偏移（抬高到脚底贴地） ─────────────────────────────────
    // 各模型在 scale=1 时的 bounds.min.y（来自 InspectPrefabStructure 结果）
    const float Y_MIN_01 = -13.83f;
    const float Y_MIN_02 = -14.95f;
    const float Y_MIN_03 = -12.74f;
    const float Y_MIN_04 = -14.52f;
    const float Y_MIN_05 = -16.76f;

    [MenuItem("Tools/DrscfZ/Setup Kuanggong Prefabs")]
    public static void Execute()
    {
        // 确保输出目录存在
        if (!AssetDatabase.IsValidFolder(OUT_WORKER))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Characters");
        if (!AssetDatabase.IsValidFolder(OUT_MONSTER))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Monsters");

        // 加载参考Prefab（复用NameTag / HPBarCanvas）
        var refWorkerPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>(REF_WORKER);
        var refMonsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(REF_MONSTER);

        // ── 生成2个Worker Prefab ──────────────────────────────────
        CreateWorkerPrefab("KuanggongWorker_01", SRC_01, WORKER_SCALE,  Y_MIN_01, refWorkerPrefab, refMonsterPrefab);
        CreateWorkerPrefab("KuanggongWorker_02", SRC_02, WORKER_SCALE,  Y_MIN_02, refWorkerPrefab, refMonsterPrefab);

        // ── 生成2个Monster Prefab + 1个Boss Prefab ─────────────────
        CreateMonsterPrefab("KuanggongMonster_03", SRC_03, MONSTER_SCALE, Y_MIN_03, refMonsterPrefab, isBoss: false);
        CreateMonsterPrefab("KuanggongMonster_04", SRC_04, MONSTER_SCALE, Y_MIN_04, refMonsterPrefab, isBoss: false);
        CreateMonsterPrefab("KuanggongBoss_05",    SRC_05, BOSS_SCALE,    Y_MIN_05, refMonsterPrefab, isBoss: true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SetupKuanggongPrefabs] 全部5个Prefab创建完成！");
        Debug.Log($"  Workers  → {OUT_WORKER}/KuanggongWorker_01.prefab / KuanggongWorker_02.prefab");
        Debug.Log($"  Monsters → {OUT_MONSTER}/KuanggongMonster_03.prefab / KuanggongMonster_04.prefab");
        Debug.Log($"  Boss     → {OUT_MONSTER}/KuanggongBoss_05.prefab");
    }

    // ─────────────────────────────────────────────────────────────────
    //  Worker Prefab = 根节点(WorkerVisual+WorkerController) + 子节点(kuanggong mesh+anim)
    //                + 子节点(NameTag canvas，复制自CowWorker)
    //                + 子节点(HPBarCanvas，复制自X_guai01，白天隐藏/夜晚显示)
    // ─────────────────────────────────────────────────────────────────
    static void CreateWorkerPrefab(string prefabName, string srcPath, float scale, float srcMinY,
                                   GameObject refWorker, GameObject refMonster = null)
    {
        var meshSrc = AssetDatabase.LoadAssetAtPath<GameObject>(srcPath);
        if (meshSrc == null) { Debug.LogError($"[SetupKuanggongPrefabs] 找不到源Prefab: {srcPath}"); return; }

        // 1. 创建根节点
        var root = new GameObject(prefabName);
        root.transform.position   = Vector3.zero;
        root.transform.localScale = Vector3.one;

        // 2. 添加WorkerVisual（颜色/发光效果组件）
        root.AddComponent<DrscfZ.Survival.WorkerVisual>();

        // 3. 实例化kuanggong模型作为子节点，设置缩放+Y偏移
        var meshGo = Object.Instantiate(meshSrc, root.transform);
        meshGo.name = "Mesh";
        float yOffset = -srcMinY * scale;
        meshGo.transform.localPosition = new Vector3(0f, yOffset, 0f);
        meshGo.transform.localScale    = new Vector3(scale, scale, scale);
        // 禁止根运动（防止动画驱动位移）
        var anim = meshGo.GetComponentInChildren<Animator>(true);
        if (anim != null) anim.applyRootMotion = false;

        // 4. 复制NameTag（头顶玩家名字Canvas）
        if (refWorker != null)
        {
            var nameTagTr = refWorker.transform.Find("NameTag");
            if (nameTagTr != null)
            {
                var nameTagCopy = Object.Instantiate(nameTagTr.gameObject, root.transform);
                nameTagCopy.name = "NameTag";
                nameTagCopy.transform.localPosition = new Vector3(0f, 2.2f, 0f);
                nameTagCopy.transform.localScale    = new Vector3(0.01f, 0.01f, 0.01f);
            }
        }

        // 5. 复制HPBarCanvas（血条，默认隐藏，夜晚由 WorkerManager.EnterNightDefense 激活）
        if (refMonster != null)
        {
            var hpCanvasTr = refMonster.transform.Find("HPBarCanvas");
            if (hpCanvasTr != null)
            {
                var hpCopy = Object.Instantiate(hpCanvasTr.gameObject, root.transform);
                hpCopy.name = "HPBarCanvas";
                // 血条定位在头顶（矿工约1.8m高，+额外间距0.1m）
                float charTop = yOffset + 1.5f;
                hpCopy.transform.localPosition = new Vector3(0f, charTop + 0.1f, 0f);
                hpCopy.transform.localScale    = new Vector3(0.01f, 0.01f, 0.01f);
                hpCopy.SetActive(false); // 默认隐藏
            }
        }

        // 6. 保存为Prefab
        string savePath = $"{OUT_WORKER}/{prefabName}.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, savePath);
        Object.DestroyImmediate(root);

        if (prefab != null)
            Debug.Log($"[SetupKuanggongPrefabs] Worker Prefab已创建: {savePath}  scale={scale}  yOffset={yOffset:F3}");
        else
            Debug.LogError($"[SetupKuanggongPrefabs] 保存失败: {savePath}");
    }

    // ─────────────────────────────────────────────────────────────────
    //  Monster/Boss Prefab = 根节点(MonsterController) + 子节点(kuanggong mesh)
    //                      + 子节点(HPBarCanvas，复制自X_guai01)
    // ─────────────────────────────────────────────────────────────────
    static void CreateMonsterPrefab(string prefabName, string srcPath, float scale, float srcMinY,
                                    GameObject refMonster, bool isBoss)
    {
        var meshSrc = AssetDatabase.LoadAssetAtPath<GameObject>(srcPath);
        if (meshSrc == null) { Debug.LogError($"[SetupKuanggongPrefabs] 找不到源Prefab: {srcPath}"); return; }

        // 1. 创建根节点 + MonsterController
        var root = new GameObject(prefabName);
        root.transform.position   = Vector3.zero;
        root.transform.localScale = Vector3.one;
        var mc = root.AddComponent<DrscfZ.Monster.MonsterController>();

        // 2. 实例化kuanggong模型作为子节点
        var meshGo = Object.Instantiate(meshSrc, root.transform);
        meshGo.name = "Mesh";
        float yOffset = -srcMinY * scale;
        meshGo.transform.localPosition = new Vector3(0f, yOffset, 0f);
        meshGo.transform.localScale    = new Vector3(scale, scale, scale);
        var anim = meshGo.GetComponentInChildren<Animator>(true);
        if (anim != null) anim.applyRootMotion = false;

        // 3. 复制HPBarCanvas（血条UI Canvas）
        if (refMonster != null)
        {
            var hpCanvasTr = refMonster.transform.Find("HPBarCanvas");
            if (hpCanvasTr != null)
            {
                var hpCopy = Object.Instantiate(hpCanvasTr.gameObject, root.transform);
                hpCopy.name = "HPBarCanvas";
                // 将血条定位在角色头顶（近似：模型高度 * scale + Y偏移 + 额外间距）
                // 所有kuanggong模型高约120-135单位，顶部 ≈ srcMinY的绝对值 + 高度
                float charTopApprox = yOffset + (isBoss ? 2.5f : 1.5f);
                hpCopy.transform.localPosition = new Vector3(0f, charTopApprox + 0.1f, 0f);
                hpCopy.transform.localScale    = new Vector3(0.01f, 0.01f, 0.01f);
            }
        }

        // 4. Boss设置：MonsterType = Boss
        if (isBoss)
        {
            // 通过SerializedObject设置MonsterType枚举
            var so    = new SerializedObject(mc);
            var typeProp = so.FindProperty("_monsterType");
            if (typeProp != null)
            {
                typeProp.enumValueIndex = (int)DrscfZ.Monster.MonsterType.Boss;
                so.ApplyModifiedProperties();
            }
        }

        // 5. 保存为Prefab
        string savePath = $"{OUT_MONSTER}/{prefabName}.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, savePath);
        Object.DestroyImmediate(root);

        if (prefab != null)
            Debug.Log($"[SetupKuanggongPrefabs] {(isBoss?"Boss":"Monster")} Prefab已创建: {savePath}  scale={scale}  yOffset={yOffset:F3}");
        else
            Debug.LogError($"[SetupKuanggongPrefabs] 保存失败: {savePath}");
    }
}
