using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 删除 ResourceRankPanel 下各列中多余的旧 Row1/Row2/Row3，只保留 Row_0/Row_1/Row_2
/// Tools → DrscfZ → Clean Old Resource Rows
/// </summary>
public static class CleanOldResourceRows
{
    [MenuItem("Tools/DrscfZ/Clean Old Resource Rows")]
    public static void Execute()
    {
        // 用 Resources.FindObjectsOfTypeAll 支持非激活对象查找
        GameObject panel = null;
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.name == "ResourceRankPanel" &&
                go.scene.isLoaded) // 排除 Prefab Asset
            {
                panel = go;
                break;
            }
        }
        if (panel == null)
        {
            Debug.LogError("[CleanOldResourceRows] 未找到 ResourceRankPanel");
            return;
        }

        string[] colNames = { "Col_Food", "Col_Coal", "Col_Ore" };
        // 需要删除的旧名称（带大写、不带下划线编号）
        string[] oldNames = { "Row1", "Row2", "Row3" };

        int deleted = 0;
        foreach (var colName in colNames)
        {
            var colTrans = panel.transform.Find(colName);
            if (colTrans == null)
            {
                Debug.LogWarning($"[CleanOldResourceRows] 未找到列: {colName}");
                continue;
            }

            foreach (var rowName in oldNames)
            {
                var old = colTrans.Find(rowName);
                if (old != null)
                {
                    Undo.DestroyObjectImmediate(old.gameObject);
                    deleted++;
                    Debug.Log($"[CleanOldResourceRows] 已删除 {colName}/{rowName}");
                }
            }
        }

        if (deleted > 0)
        {
            EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            // 保存场景
            EditorSceneManager.SaveScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"[CleanOldResourceRows] 完成，共删除 {deleted} 个旧 Row，场景已保存");
        }
        else
        {
            Debug.Log("[CleanOldResourceRows] 没有找到需要删除的旧 Row");
        }
    }
}
