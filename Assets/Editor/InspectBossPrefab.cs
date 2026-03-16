using UnityEngine;
using UnityEditor;

public class InspectBossPrefab
{
    [MenuItem("Tools/DrscfZ/Inspect Boss Prefab")]
    public static void Execute()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monsters/KuanggongBoss_05.prefab");
        if (prefab == null) { Debug.LogError("Boss prefab not found!"); return; }

        Debug.Log($"=== KuanggongBoss_05 ===");
        Debug.Log($"  Root scale: {prefab.transform.localScale}");
        LogChildren(prefab.transform, 1);
    }

    static void LogChildren(Transform t, int depth)
    {
        string indent = new string(' ', depth * 2);
        foreach (Transform child in t)
        {
            var smr = child.GetComponent<SkinnedMeshRenderer>();
            var mr  = child.GetComponent<MeshRenderer>();
            var anim= child.GetComponent<Animator>();
            var mc  = child.GetComponent<DrscfZ.Monster.MonsterController>();

            string info = $"{indent}[{child.name}] pos={child.localPosition} scale={child.localScale} active={child.gameObject.activeSelf}";
            if (smr  != null) info += $" SMR(enabled={smr.enabled}, mesh={smr.sharedMesh?.name ?? "NULL"}, mats={smr.sharedMaterials.Length})";
            if (mr   != null) info += $" MR(enabled={mr.enabled})";
            if (anim != null) info += $" Anim(ctrl={anim.runtimeAnimatorController?.name ?? "NULL"})";
            if (mc   != null) info += $" MonsterController";
            Debug.Log(info);
            LogChildren(child, depth + 1);
        }
    }
}
