#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class DisableOldManagers
{
    public static void Execute()
    {
        string[] toDisable = new string[]
        {
            "[Managers]/ForceSystem",
            "[Managers]/CampSystem",
            "[Managers]/CapybaraSpawner",
            "[Managers]/GiftHandler",
            "[Managers]/BarrageSimulator",
            "[Managers]/VFXSpawner",
            "[Managers]/FootDustManager",
        };

        int disabledCount = 0;
        int alreadyCount  = 0;
        int notFoundCount = 0;

        foreach (var path in toDisable)
        {
            var parts = path.Split('/');
            // Find root (may be inactive — scan all root objects)
            GameObject root = null;
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.transform.parent == null && go.name == parts[0] && go.scene.IsValid())
                {
                    root = go;
                    break;
                }
            }

            if (root == null)
            {
                Debug.LogWarning($"[DisableOldManagers] Root not found: {parts[0]}");
                notFoundCount++;
                continue;
            }

            // Traverse remaining path segments
            Transform current = root.transform;
            bool found = true;
            for (int i = 1; i < parts.Length; i++)
            {
                var child = current.Find(parts[i]);
                if (child == null)
                {
                    Debug.LogWarning($"[DisableOldManagers] Not found: {path}");
                    found = false;
                    notFoundCount++;
                    break;
                }
                current = child;
            }

            if (!found) continue;

            var target = current.gameObject;
            if (!target.activeSelf)
            {
                Debug.Log($"[DisableOldManagers] Already inactive: {path}");
                alreadyCount++;
            }
            else
            {
                target.SetActive(false);
                EditorUtility.SetDirty(target);
                Debug.Log($"[DisableOldManagers] Disabled: {path}");
                disabledCount++;
            }
        }

        // Mark scene dirty so save picks it up
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[DisableOldManagers] Done. Disabled={disabledCount}, AlreadyInactive={alreadyCount}, NotFound={notFoundCount}");
    }
}
#endif
