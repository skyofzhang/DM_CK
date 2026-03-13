using UnityEngine;
using UnityEditor;

public class AddWorkTargetVisuals
{
    public static string Execute()
    {
        var targets = new (string path, Color color, string label)[]
        {
            ("[SurvivalManagers]/WorkerManager/WorkTargets/WorkTarget_Fish", new Color(0.133f, 0.773f, 0.369f), "Fish"),
            ("[SurvivalManagers]/WorkerManager/WorkTargets/WorkTarget_Coal", new Color(0.471f, 0.443f, 0.424f), "Coal"),
            ("[SurvivalManagers]/WorkerManager/WorkTargets/WorkTarget_Ore",  new Color(0.580f, 0.639f, 0.722f), "Ore"),
            ("[SurvivalManagers]/WorkerManager/WorkTargets/WorkTarget_Heat", new Color(0.976f, 0.451f, 0.086f), "Heat"),
            ("[SurvivalManagers]/WorkerManager/WorkTargets/WorkTarget_Gate", new Color(0.937f, 0.267f, 0.267f), "Gate"),
        };

        int created = 0;
        var matFolder = "Assets/Materials/WorkTargets";
        if (!System.IO.Directory.Exists(matFolder))
            System.IO.Directory.CreateDirectory(matFolder);

        foreach (var (path, color, label) in targets)
        {
            // 在场景中找到 WorkTarget
            var parts = path.Split('/');
            var root = GameObject.Find(parts[0]);
            if (root == null) { Debug.LogWarning($"Root not found: {parts[0]}"); continue; }

            Transform t = root.transform;
            for (int i = 1; i < parts.Length; i++)
            {
                t = t.Find(parts[i]);
                if (t == null) break;
            }
            if (t == null) { Debug.LogWarning($"Target not found: {path}"); continue; }

            // 检查是否已有指示器
            if (t.Find("Indicator") != null) continue;

            // 创建小球指示器
            var indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "Indicator";
            indicator.transform.SetParent(t);
            indicator.transform.localPosition = Vector3.zero;
            indicator.transform.localScale    = new Vector3(0.5f, 0.5f, 0.5f);

            // 创建或复用材质
            var matPath = $"{matFolder}/WorkTarget_{label}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);
                mat.color = color;
                mat.enableInstancing = true;
                AssetDatabase.CreateAsset(mat, matPath);
            }

            var renderer = indicator.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = mat;

            // 禁用碰撞体（不影响游戏物理）
            var col = indicator.GetComponent<SphereCollider>();
            if (col != null) Object.DestroyImmediate(col);

            created++;
            Debug.Log($"[AddWorkTargetVisuals] Created indicator for {label}");
        }

        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        return $"Created {created} work target visuals";
    }
}
