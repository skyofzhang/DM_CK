#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 奶牛角色设置：探查骨骼 + 挂载武器到手部
/// 执行：DrscfZ/Setup Cow Character
/// </summary>
public class SetupCowCharacter
{
    const string COW_PREFAB_PATH   = "Assets/Models/juese/nn_01/";
    const string WEAPON_MESH_PATH  = "Assets/Models/juese/nn_01/nn_mesh_weapon.fbx";
    const string ANIM_CONTROLLER   = "Assets/Models/juese/nn_01/kuanggong_05.controller";
    const string OUTPUT_PREFAB_DIR = "Assets/Prefabs/Characters/";
    const string OUTPUT_PREFAB     = "Assets/Prefabs/Characters/CowWorker.prefab";

    [MenuItem("DrscfZ/Setup Cow Character", false, 230)]
    public static void Execute()
    {
        // ---- 1. 找到奶牛角色FBX（用 idle 动画的FBX作为基础mesh）----
        // 通常角色 mesh 和骨骼在 idle.fbx 里
        string meshFbxPath = COW_PREFAB_PATH + "nn_ainim_idle.fbx";
        var meshRoot = AssetDatabase.LoadAssetAtPath<GameObject>(meshFbxPath);
        if (meshRoot == null)
        {
            Debug.LogError($"[SetupCow] 未找到角色FBX: {meshFbxPath}");
            return;
        }

        // ---- 2. 打印骨骼层级 ----
        Debug.Log($"[SetupCow] 角色FBX骨骼结构（{meshFbxPath}）:");
        PrintBones(meshRoot.transform, 0, out var boneNames);

        // ---- 3. 找手部骨骼名称 ----
        string handBoneName = FindHandBone(boneNames);
        Debug.Log($"[SetupCow] 识别到手部骨骼: '{handBoneName}'");

        // ---- 4. 创建预制体目录 ----
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Characters"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Characters");

        // ---- 5. 实例化角色 ----
        var cowInstance = Object.Instantiate(meshRoot);
        cowInstance.name = "CowWorker";

        // 挂载 Animator Controller
        // FBX实例化的Animator可能内部状态异常，先尝试删除重建
        var existingAnimator = cowInstance.GetComponent<Animator>();
        if (existingAnimator != null)
            Object.DestroyImmediate(existingAnimator);
        var animator = cowInstance.AddComponent<Animator>();
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ANIM_CONTROLLER);
        if (controller != null)
        {
            animator.runtimeAnimatorController = controller;
            Debug.Log($"[SetupCow] ✅ AnimatorController 挂载成功: {ANIM_CONTROLLER}");
        }
        else
            Debug.LogWarning($"[SetupCow] 未找到 AnimatorController: {ANIM_CONTROLLER}");

        // ---- 6. 找手部骨骼并挂载武器 ----
        var weaponFbx = AssetDatabase.LoadAssetAtPath<GameObject>(WEAPON_MESH_PATH);
        if (weaponFbx != null && !string.IsNullOrEmpty(handBoneName))
        {
            Transform handBone = FindBoneInHierarchy(cowInstance.transform, handBoneName);
            if (handBone != null)
            {
                var weaponInstance = Object.Instantiate(weaponFbx, handBone);
                weaponInstance.name = "Weapon_Hoe";
                weaponInstance.transform.localPosition = new Vector3(0, 0, 0.1f);
                weaponInstance.transform.localRotation = Quaternion.Euler(0, 0, 0);
                weaponInstance.transform.localScale    = Vector3.one;
                Debug.Log($"[SetupCow] ✅ 武器已挂载到骨骼: '{handBoneName}'");
            }
            else
            {
                Debug.LogWarning($"[SetupCow] 未找到手部骨骼 '{handBoneName}'，将武器挂载到根骨骼");
                // 找根骨骼或第一个子骨骼
                var firstChild = cowInstance.transform.childCount > 0 ? cowInstance.transform.GetChild(0) : cowInstance.transform;
                var weaponInstance = Object.Instantiate(weaponFbx, firstChild);
                weaponInstance.name = "Weapon_Hoe";
            }
        }
        else
        {
            if (weaponFbx == null)
                Debug.LogWarning($"[SetupCow] 未找到武器FBX: {WEAPON_MESH_PATH}");
        }

        // ---- 8. 保存预制体 ----
        // 注意：WorkerController 是 WorkerManager 的内部类，由运行时WorkerManager负责管理
        // 预制体本身只需要 Animator 组件，工作状态机由外部 WorkerManager 控制
        PrefabUtility.SaveAsPrefabAssetAndConnect(cowInstance, OUTPUT_PREFAB, InteractionMode.AutomatedAction);
        Object.DestroyImmediate(cowInstance);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[SetupCow] ===== 奶牛角色预制体保存完毕: {OUTPUT_PREFAB} =====");
    }

    static void PrintBones(Transform t, int depth, out List<string> names)
    {
        names = new List<string>();
        PrintBonesRecursive(t, depth, names);
    }

    static void PrintBonesRecursive(Transform t, int depth, List<string> names)
    {
        string indent = new string(' ', depth * 2);
        Debug.Log($"{indent}[{t.name}]");
        names.Add(t.name);
        foreach (Transform child in t)
            PrintBonesRecursive(child, depth + 1, names);
    }

    static string FindHandBone(List<string> boneNames)
    {
        // 常见手部骨骼名称关键词
        string[] handKeywords = {
            "RightHand", "R_Hand", "Hand_R", "hand_r", "righthand",
            "LeftHand",  "L_Hand", "Hand_L", "hand_l", "lefthand",
            "Bip001 R Hand", "Bip001 L Hand",
            "hand", "Hand", "wrist", "Wrist"
        };

        // 精确匹配
        foreach (var kw in handKeywords)
        {
            foreach (var n in boneNames)
            {
                if (n == kw) return n;
            }
        }

        // 模糊匹配（包含关键字）
        foreach (var n in boneNames)
        {
            string lower = n.ToLower();
            if (lower.Contains("hand") || lower.Contains("wrist"))
                return n;
        }

        // 找不到时返回第一个非根骨骼（猜测）
        if (boneNames.Count > 1) return boneNames[1];
        if (boneNames.Count > 0) return boneNames[0];
        return "";
    }

    static Transform FindBoneInHierarchy(Transform root, string boneName)
    {
        if (root.name == boneName) return root;
        foreach (Transform child in root)
        {
            var found = FindBoneInHierarchy(child, boneName);
            if (found != null) return found;
        }
        return null;
    }
}
#endif
