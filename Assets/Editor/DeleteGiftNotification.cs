using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class DeleteGiftNotification
{
    [MenuItem("Tools/DrscfZ/Delete GiftNotification (废弃对象)")]
    public static void Execute()
    {
        // 查找场景中所有有 Missing Script 的 GiftNotification 对象
        var targets = new string[] {
            "GiftNotification",
        };

        int deleted = 0;
        // 必须用 Resources.FindObjectsOfTypeAll 才能找到非激活（inactive）对象
        var allGOs = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in allGOs)
        {
            if (go.scene.name == null || go.scene.name == "") continue; // 跳过 Prefab 资产
            foreach (var name in targets)
            {
                if (go.name == name)
                {
                    Debug.Log($"[DeleteGiftNotification] 删除废弃对象: {go.name} (路径: {GetPath(go)})");
                    Object.DestroyImmediate(go);
                    deleted++;
                    break;
                }
            }
        }

        if (deleted == 0)
            Debug.Log("[DeleteGiftNotification] 未找到 GiftNotification 对象（可能已删除）");
        else
        {
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log($"[DeleteGiftNotification] 已删除 {deleted} 个废弃对象，场景已保存");
        }
    }

    private static string GetPath(GameObject go)
    {
        string path = go.name;
        var t = go.transform.parent;
        while (t != null) { path = t.name + "/" + path; t = t.parent; }
        return path;
    }
}
