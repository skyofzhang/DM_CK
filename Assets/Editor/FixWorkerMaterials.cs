using UnityEngine;
using UnityEditor;
using DrscfZ.Survival;

/// <summary>
/// 修复 WorkerVisual._normalMaterial：
/// 将每个 Worker 的当前 MeshRenderer.sharedMaterial 注入到 _normalMaterial 字段
/// 同时创建 GlowMaterial（金色自发光）和 FrozenMaterial（冰蓝色）
/// 运行一次即可。
/// </summary>
public class FixWorkerMaterials
{
    [MenuItem("Tools/Phase2/Fix Worker Materials (_normalMaterial + Glow + Frozen)")]
    public static void Execute()
    {
        // 创建 Glow 材质（金色 HDR）
        var glowMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        glowMat.name = "Mat_Worker_Glow";
        glowMat.color = new Color(1f, 0.85f, 0f, 1f); // 金黄色
        // Enable emission
        glowMat.EnableKeyword("_EMISSION");
        glowMat.SetColor("_EmissionColor", new Color(1f, 0.75f, 0f, 1f) * 2f);
        AssetDatabase.CreateAsset(glowMat, "Assets/Materials/Mat_Worker_Glow.mat");

        // 创建 Frozen 材质（冰蓝色）
        var frozenMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        frozenMat.name = "Mat_Worker_Frozen";
        frozenMat.color = new Color(0.5f, 0.85f, 1f, 1f); // 冰蓝色
        frozenMat.EnableKeyword("_EMISSION");
        frozenMat.SetColor("_EmissionColor", new Color(0.3f, 0.7f, 1f, 1f) * 0.8f);
        AssetDatabase.CreateAsset(frozenMat, "Assets/Materials/Mat_Worker_Frozen.mat");

        AssetDatabase.SaveAssets();

        int fixedCount = 0;
        var poolRoot = GameObject.Find("WorkerPool");
        if (poolRoot == null)
        {
            Debug.LogError("[FixWorkerMaterials] WorkerPool not found in scene.");
            return;
        }

        foreach (Transform child in poolRoot.transform)
        {
            var visual = child.GetComponent<WorkerVisual>();
            var renderer = child.GetComponent<MeshRenderer>();
            if (visual == null || renderer == null) continue;

            var so = new SerializedObject(visual);

            // _normalMaterial = current sharedMaterial (白色 worker 材质)
            so.FindProperty("_normalMaterial").objectReferenceValue = renderer.sharedMaterial;

            // _glowMaterial
            so.FindProperty("_glowMaterial").objectReferenceValue = glowMat;

            // _frozenMaterial
            so.FindProperty("_frozenMaterial").objectReferenceValue = frozenMat;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(visual);
            fixedCount++;
        }

        // 保存场景
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[FixWorkerMaterials] ✅ Fixed {fixedCount} Workers: _normalMaterial + _glowMaterial + _frozenMaterial injected. Glow/Frozen materials saved to Assets/Materials/.");
    }
}
