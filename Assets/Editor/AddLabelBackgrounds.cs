using UnityEngine;
using UnityEditor;

public class AddLabelBackgrounds
{
    [MenuItem("Tools/Add Label Backgrounds")]
    public static void Execute()
    {
        // 创建半透明黑色材质
        string matPath = "Assets/Materials/LabelBackground.mat";
        Material bgMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (bgMat == null)
        {
            // 尝试 URP Unlit shader
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");
            bgMat = new Material(shader);
            bgMat.color = new Color(0f, 0f, 0f, 0.7f);
            // 开启透明度
            bgMat.SetFloat("_Surface", 1f); // 0=Opaque, 1=Transparent (URP)
            bgMat.SetFloat("_Blend", 0f);
            bgMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            bgMat.renderQueue = 3000;
            System.IO.Directory.CreateDirectory("Assets/Materials");
            AssetDatabase.CreateAsset(bgMat, matPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[AddLabelBG] Created material: " + matPath);
        }

        // 找 SceneUI3D 或直接搜索所有 Label_ 对象
        var sceneUI3D = GameObject.Find("SceneUI3D");
        int count = 0;

        if (sceneUI3D != null)
        {
            foreach (Transform child in sceneUI3D.transform)
            {
                if (child.name.StartsWith("Label_"))
                {
                    AddBackgroundToLabel(child, bgMat);
                    count++;
                }
            }
        }
        else
        {
            // 备选：直接找所有 Label_ 对象
            var allObjects = GameObject.FindObjectsOfType<GameObject>(true);
            foreach (var go in allObjects)
            {
                if (go.name.StartsWith("Label_") && go.transform.parent != null)
                {
                    AddBackgroundToLabel(go.transform, bgMat);
                    count++;
                }
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log($"[AddLabelBG] Done! Added backgrounds to {count} labels.");
    }

    static void AddBackgroundToLabel(Transform labelTransform, Material bgMat)
    {
        // 检查是否已有 Background 子对象
        var existingBg = labelTransform.Find("Background");
        if (existingBg != null)
        {
            Debug.Log($"[AddLabelBG] {labelTransform.name} already has Background, skipping.");
            return;
        }

        // 创建 Quad
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Background";
        quad.transform.SetParent(labelTransform, false);
        quad.transform.localPosition = new Vector3(0f, 0f, 0.02f); // 稍在文字后方
        quad.transform.localRotation = Quaternion.identity;
        quad.transform.localScale = new Vector3(2.0f, 0.9f, 1f);   // 覆盖文字区域

        // 赋予材质
        var renderer = quad.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.sharedMaterial = bgMat;

        // 移除Collider（不需要物理）
        var collider = quad.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);

        EditorUtility.SetDirty(labelTransform.gameObject);
        Debug.Log($"[AddLabelBG] Added Background to {labelTransform.name}");
    }
}
