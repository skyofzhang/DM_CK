using UnityEngine;
using UnityEditor;

/// <summary>
/// 删除场景根节点中残留的 KuanggongMonster 测试实例
/// Tools → DrscfZ → Clean Stray Monsters
/// </summary>
public class CleanStrayMonsters
{
    [MenuItem("Tools/DrscfZ/Clean Stray Monsters")]
    public static void Execute()
    {
        string[] names = { "KuanggongMonster_03", "KuanggongMonster_04", "KuanggongBoss_05" };
        int removed = 0;
        foreach (var name in names)
        {
            var go = GameObject.Find(name);
            if (go != null)
            {
                Object.DestroyImmediate(go);
                Debug.Log($"[CleanStrayMonsters] 已删除场景中的残留实例: {name}");
                removed++;
            }
        }
        if (removed > 0)
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[CleanStrayMonsters] 完成，共删除 {removed} 个残留实例。");
    }
}
