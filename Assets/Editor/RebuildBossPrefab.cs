using UnityEngine;
using UnityEditor;
using DrscfZ.Monster;

/// <summary>
/// 重建 KuanggongBoss_05 Prefab（删掉旧的，从源 kuanggong_05 重新生成）
/// 用法：Tools → DrscfZ → Rebuild Boss Prefab
/// </summary>
public class RebuildBossPrefab
{
    const string SRC_BOSS    = "Assets/Res/DGMT_data/Model_juese(use)/kuanggong/kuanggong_05.prefab";
    const string REF_MONSTER = "Assets/Prefabs/Monsters/X_guai01.prefab";
    const string SAVE_PATH   = "Assets/Prefabs/Monsters/KuanggongBoss_05.prefab";

    // boss scale：让 kuanggong_05（~165单位高）在游戏中约 2.5m
    const float BOSS_SCALE  = 0.018f;
    const float Y_MIN_05    = -16.76f;   // 来自 InspectPrefabStructure 结果

    [MenuItem("Tools/DrscfZ/Rebuild Boss Prefab")]
    public static void Execute()
    {
        // ── 加载资产 ──────────────────────────────────────────────────
        var meshSrc    = AssetDatabase.LoadAssetAtPath<GameObject>(SRC_BOSS);
        var refMonster = AssetDatabase.LoadAssetAtPath<GameObject>(REF_MONSTER);
        if (meshSrc == null)
        {
            Debug.LogError($"[RebuildBoss] 找不到源 Prefab: {SRC_BOSS}");
            return;
        }

        // ── 创建根节点 ─────────────────────────────────────────────────
        var root = new GameObject("KuanggongBoss_05");
        root.transform.position   = Vector3.zero;
        root.transform.localScale = Vector3.one;

        // MonsterController
        var mc = root.AddComponent<MonsterController>();
        var so = new SerializedObject(mc);
        var typeProp = so.FindProperty("_monsterType");
        if (typeProp != null)
        {
            typeProp.enumValueIndex = (int)MonsterType.Boss;
            so.ApplyModifiedProperties();
        }

        // ── Mesh 子节点（只实例化源 prefab，不做任何组件修改）────────
        var meshGo = Object.Instantiate(meshSrc, root.transform);
        meshGo.name = "Mesh";

        float yOffset = -Y_MIN_05 * BOSS_SCALE;          // ≈ 0.302
        meshGo.transform.localPosition = new Vector3(0f, yOffset, 0f);
        meshGo.transform.localScale    = Vector3.one * BOSS_SCALE;

        // 禁用根运动（防止动画驱动位移）
        var anim = meshGo.GetComponentInChildren<Animator>(true);
        if (anim != null) anim.applyRootMotion = false;

        // 关闭阴影投射（性能优化，无需运行时再改）
        foreach (var smr in meshGo.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            smr.updateWhenOffscreen = false;
        }

        // ── HPBarCanvas（复制自 X_guai01）────────────────────────────
        if (refMonster != null)
        {
            var hpTr = refMonster.transform.Find("HPBarCanvas");
            if (hpTr != null)
            {
                var hpCopy = Object.Instantiate(hpTr.gameObject, root.transform);
                hpCopy.name = "HPBarCanvas";
                // Boss 约 2.5m 高，血条放在头顶上方 0.3m
                float charTop = yOffset + 2.5f;
                hpCopy.transform.localPosition = new Vector3(0f, charTop + 0.3f, 0f);
                hpCopy.transform.localScale    = Vector3.one * 0.012f; // 比普通怪略大
            }
        }

        // ── 保存（覆盖旧文件）─────────────────────────────────────────
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, SAVE_PATH);
        Object.DestroyImmediate(root);

        if (prefab != null)
            Debug.Log($"[RebuildBoss] 重建完成！  scale={BOSS_SCALE}  yOffset={yOffset:F3}  path={SAVE_PATH}");
        else
            Debug.LogError("[RebuildBoss] Prefab 保存失败！");

        AssetDatabase.Refresh();
    }
}
