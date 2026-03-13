using UnityEngine;
using UnityEditor;

public class FixLabelBackgrounds
{
    [MenuItem("Tools/Fix Label Backgrounds")]
    public static void Execute()
    {
        var sceneUI3D = GameObject.Find("SceneUI3D");
        if (sceneUI3D == null)
        {
            Debug.LogError("[FixLabelBG] SceneUI3D not found!");
            return;
        }

        int count = 0;
        foreach (Transform child in sceneUI3D.transform)
        {
            if (!child.name.StartsWith("Label_")) continue;

            var bg = child.Find("Background");
            if (bg == null)
            {
                Debug.LogWarning($"[FixLabelBG] {child.name} has no Background child, skipping.");
                continue;
            }

            // 计算 Icon 和 Value 的 y 范围，让背景覆盖两者
            // Icon localPosition.y = 0.7, Value localPosition.y = 0.0
            // 背景居中 y = 0.35，高度需要覆盖从约 -0.5 到 +1.2 的范围（含边距）
            // 实际单位：父节点 scale=0.1，localScale 是在父节点局部空间
            // Icon sizeDelta=(6,4), Value sizeDelta=(8,3)，字号约2-3.5
            // 背景 localScale 调整为能覆盖整个标签区域
            bg.localPosition = new Vector3(0f, 0.35f, -0.02f); // z负值 = 退后到文字后面，y居中于Icon+Value
            bg.localScale    = new Vector3(2.2f, 1.6f, 1f);    // 宽2.2 高1.6，覆盖Icon(y=0.7)+Value(y=0)区域

            EditorUtility.SetDirty(child.gameObject);
            Debug.Log($"[FixLabelBG] Fixed Background on {child.name}: pos=(0, 0.35, -0.02), scale=(2.2, 1.6, 1)");
            count++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log($"[FixLabelBG] Done! Fixed {count} label backgrounds.");
    }
}
