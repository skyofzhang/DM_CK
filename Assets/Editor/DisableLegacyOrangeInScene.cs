// DrscfZ 场景清理：让旧橙子/角力游戏遗留节点在场景中失效
// 2026-04-23 - 保守策略：不删 GO/Prefab，仅 SetActive(false) + 移除旧组件
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DisableLegacyOrangeInScene
{
    [MenuItem("Tools/DrscfZ/Disable Legacy Orange In Scene")]
    public static void Execute()
    {
        var scene = SceneManager.GetActiveScene();
        int disabledCount = 0;
        int removedComponentCount = 0;

        // 1. 找根对象名为 "Orange" 的 GameObject 并失活
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "Orange")
            {
                Undo.RegisterCompleteObjectUndo(root, "Disable Legacy Orange");
                root.SetActive(false);
                disabledCount++;
                Debug.Log($"[DisableLegacyOrange] SetActive(false): {GetHierarchyPath(root.transform)}");
            }
        }

        // 2. 移除 Main Camera 上的 OrangeFollowCamera 组件
        var mainCam = Camera.main;
        if (mainCam != null)
        {
            var followCam = mainCam.GetComponent("OrangeFollowCamera") as Component;
            if (followCam != null)
            {
                Undo.DestroyObjectImmediate(followCam);
                removedComponentCount++;
                Debug.Log($"[DisableLegacyOrange] Removed component: Main Camera/OrangeFollowCamera");
            }
        }
        else
        {
            // Main Camera 可能没 MainCamera tag，按名字兜底
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "Main Camera")
                {
                    var followCam = root.GetComponent("OrangeFollowCamera") as Component;
                    if (followCam != null)
                    {
                        Undo.DestroyObjectImmediate(followCam);
                        removedComponentCount++;
                        Debug.Log($"[DisableLegacyOrange] Removed component: Main Camera/OrangeFollowCamera (by name fallback)");
                    }
                    break;
                }
            }
        }

        if (disabledCount > 0 || removedComponentCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[DisableLegacyOrange] ✅ 完成 — 失活 GO={disabledCount}, 移除组件={removedComponentCount}. 场景已保存.");
        }
        else
        {
            Debug.Log("[DisableLegacyOrange] 已清理过（或无遗留），无改动.");
        }

        // OrangeOverlay / OrangeIcon 保留不动：
        //   - Gift_Canvas/T4_FullscreenGlow/OrangeOverlay 可能是 T4 礼物视效
        //   - Canvas/GameUIPanel/TopBar/.../OrangeIcon 可能是用作矿石/资源图标
        // 若确认是遗留，请手动处理
    }

    static string GetHierarchyPath(Transform t)
    {
        if (t == null) return "";
        var path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
