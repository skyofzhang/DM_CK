using UnityEngine;
using UnityEditor;

public class AssignMonsterAnimatorController
{
    public static void Execute()
    {
        string prefabPath = "Assets/Prefabs/Monsters/X_guai01.prefab";
        string controllerPath = "Assets/Models/juese/X_guai01/X_guai01.controller";

        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        if (controller == null)
        {
            Debug.LogError("[AssignMonster] Controller not found: " + controllerPath);
            return;
        }

        using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            var root = scope.prefabContentsRoot;

            // 查找已有 Animator（含 root 自身）
            var animator = root.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                // FBX 内嵌模型无 Animator，在 root 上添加
                animator = root.AddComponent<Animator>();
                Debug.Log("[AssignMonster] 添加了新 Animator 到 root");
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            Debug.Log($"[AssignMonster] OK — Animator on '{animator.gameObject.name}', controller = {controller.name}");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[AssignMonster] Prefab saved.");
    }
}
