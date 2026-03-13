using UnityEngine;
using UnityEditor;
using DrscfZ.UI;

/// <summary>
/// Label_CityGate 下添加 3D Quad HP 进度条，并将 HPBar_Fill 引用注入 WorldSpaceLabel._hpBarFillTransform。
///
/// HP 进度条原理（3D Quad 方案）：
///   - HPBar_BG：暗色背景 Quad（全宽）
///   - HPBar_Fill：红色填充 Quad（pivot 移到左端，通过 Transform.localScale.x 控制进度）
///
/// 进度更新时：fillTransform.localScale.x = initialScaleX * ratio
/// 为使缩放从左往右，Fill Quad 的锚点/pivot 在左侧：
///   - localPosition.x = -barHalfWidth（左对齐起点），pivot 在左
///   - 由于 Quad 默认 pivot 在中心，需偏移 localPosition.x += scaleX/2 * barWidth
///
/// 菜单：Tools/Phase2/Add CityGate HP Bar
/// </summary>
public class AddCityGateHPBar
{
    private const float BAR_WIDTH  = 2.0f;   // 进度条宽（世界单位，父 scale=0.1 → 实际 0.2m）
    private const float BAR_HEIGHT = 0.25f;  // 进度条高（世界单位）
    private const float BAR_Y      = -0.45f; // 相对 Label_CityGate 根的 Y 偏移（在文字下方）
    private const float BAR_Z      = -0.01f; // 比 Background 略靠前

    [MenuItem("Tools/Phase2/Add CityGate HP Bar")]
    public static void Execute()
    {
        var labelGO = GameObject.Find("Label_CityGate");
        if (labelGO == null)
        {
            Debug.LogError("[AddCityGateHPBar] 找不到 Label_CityGate，请先运行 Setup3DSceneUI");
            return;
        }

        // 删除已存在的旧进度条
        var oldBG = labelGO.transform.Find("HPBar_BG");
        if (oldBG != null)
        {
            Undo.DestroyObjectImmediate(oldBG.gameObject);
            Debug.Log("[AddCityGateHPBar] 已删除旧 HPBar_BG");
        }

        // ── 1. 背景条（HPBar_BG）──────────────────────────────────────────
        var bgGO = new GameObject("HPBar_BG");
        Undo.RegisterCreatedObjectUndo(bgGO, "Create HPBar_BG");
        bgGO.transform.SetParent(labelGO.transform, false);
        bgGO.transform.localPosition = new Vector3(0f, BAR_Y, BAR_Z);
        bgGO.transform.localScale    = new Vector3(BAR_WIDTH, BAR_HEIGHT, 1f);

        var bgMF = bgGO.AddComponent<MeshFilter>();
        bgMF.sharedMesh = GetQuadMesh();

        var bgMR = bgGO.AddComponent<MeshRenderer>();
        bgMR.sharedMaterial = CreateOrLoadMaterial("HPBar_BG", new Color(0.15f, 0.15f, 0.15f, 0.85f));

        // ── 2. 填充条（HPBar_Fill）── pivot 左对齐 ─────────────────────────
        // 由于 Unity Quad pivot 在中心，要让缩放从左端开始，
        // 将 Fill 放在 BG 的局部坐标 -0.5（BG 的左端），然后 Fill 自身 pivot 也靠左。
        // 实现：Fill 的 localPosition.x = -0.5（BG 宽度 1.0 的一半），
        //        fill 的 localScale.x = 1.0（满血），pivot=左 → 通过再嵌套一层 Pivot 实现。
        //
        // 更简单方案：用 localPosition.x 动态补偿（HPBar_Fill 以 BG 左端为参考点）
        //   Fill pivot 在中心，满血时 localPos.x=0, localScale.x=1
        //   当 ratio=0.5 时，localScale.x=0.5，localPos.x 应为 -(1-0.5)/2 = -0.25
        //   这样左端永远贴着 BG 左端。此计算在运行时脚本中处理，
        //   此处创建时先设置满血状态（ratio=1，pos.x=0）。
        //
        // 注意：为避免代码过复杂，WorldSpaceLabel 只控制 localScale.x，
        //       并通过 localPosition.x 补偿：posX = -(1 - ratio) * 0.5f * initScaleX
        //       但这要求 _hpBarFillTransform 脚本知道 BG 宽。
        //
        // 最终选择的简单方案：Fill 是 BG 的子对象，锚点=左端，
        //   localPosition = (-0.5, 0, 0.01) 在 BG 坐标系（BG scale = BAR_WIDTH）
        //   → 实际 local pos x = -0.5（BG 宽1.0的左半）
        //   Fill localScale.x = ratio（0~1 映射到 0~1 BG 宽）
        //   Fill localPosition.x = -0.5 + ratio * 0.5（使左端固定）
        //   → 运行时脚本需要同时更新 scale 和 pos。
        //
        // 为了让 WorldSpaceLabel 脚本保持简洁（只控制 scale），
        // 使用 Pivot 容器方案：
        //   HPBar_Fill_Pivot（空节点，位于 BG 左端 localPos.x=-0.5）
        //     └── HPBar_Fill（Quad，localPos.x=+0.5，scale=(1,1,1)）
        //   缩放 Pivot 的 localScale.x = ratio → 左端不动，右端收缩

        var pivotGO = new GameObject("HPBar_Fill_Pivot");
        Undo.RegisterCreatedObjectUndo(pivotGO, "Create HPBar_Fill_Pivot");
        pivotGO.transform.SetParent(bgGO.transform, false);
        // 在 BG 局部坐标中：BG 宽=1（因为 Quad 是1×1，BG scale=BAR_WIDTH）
        // pivot 位于 BG 左端（局部 x=-0.5），缩放后右端收缩
        pivotGO.transform.localPosition = new Vector3(-0.5f, 0f, 0.01f);
        pivotGO.transform.localScale    = Vector3.one;  // 满血=1，运行时改此值

        var fillGO = new GameObject("HPBar_Fill");
        Undo.RegisterCreatedObjectUndo(fillGO, "Create HPBar_Fill");
        fillGO.transform.SetParent(pivotGO.transform, false);
        // Fill Quad 中心在 pivot 右侧 0.5（Quad 宽1，右端=pivot+1）
        fillGO.transform.localPosition = new Vector3(0.5f, 0f, 0f);
        fillGO.transform.localScale    = Vector3.one;

        var fillMF = fillGO.AddComponent<MeshFilter>();
        fillMF.sharedMesh = GetQuadMesh();

        var fillMR = fillGO.AddComponent<MeshRenderer>();
        fillMR.sharedMaterial = CreateOrLoadMaterial("HPBar_Fill", new Color(0.2f, 0.85f, 0.2f, 1f));

        // ── 3. 将 HPBar_Fill_Pivot 的 Transform 注入 WorldSpaceLabel._hpBarFillTransform ──
        var wsl = labelGO.GetComponent<WorldSpaceLabel>();
        if (wsl != null)
        {
            var so = new SerializedObject(wsl);
            var prop = so.FindProperty("_hpBarFillTransform");
            if (prop != null)
            {
                prop.objectReferenceValue = pivotGO.transform;
                so.ApplyModifiedProperties();
                Debug.Log("[AddCityGateHPBar] _hpBarFillTransform 已设置为 HPBar_Fill_Pivot");
            }
            else
            {
                Debug.LogWarning("[AddCityGateHPBar] 找不到 _hpBarFillTransform 序列化属性，请检查字段名");
            }
            EditorUtility.SetDirty(wsl);
        }
        else
        {
            Debug.LogWarning("[AddCityGateHPBar] Label_CityGate 上没有 WorldSpaceLabel 组件");
        }

        EditorUtility.SetDirty(labelGO);

        // ── 4. 标记场景脏（由 SaveCurrentScene 负责保存）──────────────────
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[AddCityGateHPBar] HP 进度条创建完成 ✅");
        Debug.Log($"[AddCityGateHPBar] 层级：Label_CityGate/HPBar_BG/HPBar_Fill_Pivot/HPBar_Fill");
    }

    // ── 材质获取/创建 ──────────────────────────────────────────────────────────
    private static Material CreateOrLoadMaterial(string name, Color color)
    {
        string path = $"Assets/Materials/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        // 使用 URP Unlit 或 Standard（视管线而定）
        var shader = Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Unlit/Color")
                  ?? Shader.Find("Standard");
        var mat = new Material(shader);
        mat.name = name;

        // URP Unlit 的主颜色属性
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        else if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);

        // 半透明设置（背景条 alpha < 1）
        if (color.a < 1f)
        {
            mat.SetFloat("_Surface", 1f);       // Transparent
            mat.SetFloat("_Blend", 0f);          // Alpha
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
        }

        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();
        return mat;
    }

    // ── 获取内置 Quad Mesh ─────────────────────────────────────────────────────
    private static Mesh GetQuadMesh()
    {
        // 从内置资源找 Quad
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        var mesh = go.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(go);
        return mesh;
    }
}
