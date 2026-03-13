using UnityEngine;
using UnityEditor;
using TMPro;

public class FixWorldSpaceScales
{
    [MenuItem("Tools/Fix WorldSpace Canvas Scales")]
    public static void Execute()
    {
        int count = 0;

        // 1. Fix WorldSpace Canvas objects (BubbleCanvas etc.)
        var allCanvases = GameObject.FindObjectsOfType<Canvas>(true);
        foreach (var canvas in allCanvases)
        {
            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                var t = canvas.transform;
                Debug.Log($"[FixWSScale] Canvas '{GetPath(t)}' {t.localScale} -> (0.1,0.1,0.1)");
                t.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                EditorUtility.SetDirty(canvas.gameObject);
                count++;
            }
        }

        // 2. Fix SceneUI3D Label_ parent objects (3D TMP parents)
        var sceneUI3D = GameObject.Find("SceneUI3D");
        if (sceneUI3D != null)
        {
            foreach (Transform child in sceneUI3D.transform)
            {
                if (child.name.StartsWith("Label_"))
                {
                    Debug.Log($"[FixWSScale] Label '{child.name}' {child.localScale} -> (0.1,0.1,0.1)");
                    child.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                    EditorUtility.SetDirty(child.gameObject);
                    count++;
                }
            }
        }
        else
        {
            Debug.LogWarning("[FixWSScale] SceneUI3D not found in scene!");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log($"[FixWSScale] Done! Fixed {count} objects to (0.1,0.1,0.1)");
    }

    static string GetPath(Transform t)
    {
        string path = t.name;
        var cur = t;
        while (cur.parent != null) { cur = cur.parent; path = cur.name + "/" + path; }
        return path;
    }
}
