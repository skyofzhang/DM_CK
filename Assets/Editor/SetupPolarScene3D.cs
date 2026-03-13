#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

/// <summary>
/// 重建极地生存法则3D场景（色块BOX占位）
/// 执行：DrscfZ/Setup Polar Scene 3D
/// </summary>
public class SetupPolarScene3D
{
    // ---- 材质路径 ----
    const string MAT_DIR     = "Assets/Materials/Polar";
    const string MAT_SNOW    = "Assets/Materials/Polar/Mat_Snow.mat";
    const string MAT_SKY     = "Assets/Materials/Polar/Mat_Sky.mat";
    const string MAT_MOUTAIN = "Assets/Materials/Polar/Mat_Mountain.mat";
    const string MAT_FORTRESS= "Assets/Materials/Polar/Mat_Fortress.mat";
    const string MAT_CAMP_L  = "Assets/Materials/Polar/Mat_CampLeft.mat";
    const string MAT_CAMP_R  = "Assets/Materials/Polar/Mat_CampRight.mat";
    const string MAT_GATE    = "Assets/Materials/Polar/Mat_Gate.mat";

    [MenuItem("DrscfZ/Setup Polar Scene 3D", false, 210)]
    public static void Execute()
    {
        // ---- 0. 关闭旧的旋转平面 / SkyBg / SnowWall等遗留对象 ----
        DisableOldSceneObjects();

        // ---- 1. 创建材质目录 ----
        if (!AssetDatabase.IsValidFolder(MAT_DIR))
        {
            AssetDatabase.CreateFolder("Assets/Materials", "Polar");
        }

        // ---- 2. 创建所有材质 ----
        var matSnow     = EnsureMaterial(MAT_SNOW,    new Color(0.94f, 0.96f, 1.00f));   // 雪白 #F0F5FF
        var matSky      = EnsureMaterial(MAT_SKY,     new Color(0.04f, 0.11f, 0.23f));   // 深蓝 #0B1D3A
        var matMountain = EnsureMaterial(MAT_MOUTAIN, new Color(0.55f, 0.72f, 0.82f));   // 淡蓝 #8BB8D0
        var matFortress = EnsureMaterial(MAT_FORTRESS, new Color(0.12f, 0.23f, 0.37f));  // 深蓝 #1E3A5F
        var matCampL    = EnsureMaterial(MAT_CAMP_L,  new Color(0.15f, 0.39f, 0.92f));   // 冰蓝 #2563EB
        var matCampR    = EnsureMaterial(MAT_CAMP_R,  new Color(0.92f, 0.35f, 0.05f));   // 橙   #EA580C
        var matGate     = EnsureMaterial(MAT_GATE,    new Color(0.42f, 0.30f, 0.17f));   // 棕   #6B4C2A

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ---- 3. 创建父对象 PolarScene ----
        var existingParent = GameObject.Find("PolarScene");
        if (existingParent != null) Object.DestroyImmediate(existingParent);
        var parent = new GameObject("PolarScene");

        // ---- 4. 雪地地面（Quad，放平）----
        var ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
        ground.name = "SnowGround";
        ground.transform.SetParent(parent.transform);
        ground.transform.localPosition = new Vector3(0, -0.05f, 0);
        ground.transform.localRotation = Quaternion.Euler(90, 0, 0);
        ground.transform.localScale    = new Vector3(30, 20, 1);
        AssignMat(ground, matSnow);

        // ---- 5. 背景山（左）----
        var mtnL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mtnL.name = "BackMountain_L";
        mtnL.transform.SetParent(parent.transform);
        mtnL.transform.localPosition = new Vector3(-12, 2, 8);
        mtnL.transform.localScale    = new Vector3(6, 4, 2);
        AssignMat(mtnL, matMountain);

        // ---- 6. 背景山（右）----
        var mtnR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mtnR.name = "BackMountain_R";
        mtnR.transform.SetParent(parent.transform);
        mtnR.transform.localPosition = new Vector3(12, 2, 8);
        mtnR.transform.localScale    = new Vector3(6, 4, 2);
        AssignMat(mtnR, matMountain);

        // ---- 7. 中央堡垒（Cylinder）----
        var fortress = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        fortress.name = "CentralFortress";
        fortress.transform.SetParent(parent.transform);
        fortress.transform.localPosition = new Vector3(0, 1.5f, 2);
        fortress.transform.localScale    = new Vector3(3, 1.5f, 3);
        AssignMat(fortress, matFortress);

        // ---- 8. 左营地 ----
        var campL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        campL.name = "LeftCamp";
        campL.transform.SetParent(parent.transform);
        campL.transform.localPosition = new Vector3(-7, 1, 4);
        campL.transform.localScale    = new Vector3(4, 2, 3);
        AssignMat(campL, matCampL);

        // ---- 9. 右营地 ----
        var campR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        campR.name = "RightCamp";
        campR.transform.SetParent(parent.transform);
        campR.transform.localPosition = new Vector3(7, 1, 4);
        campR.transform.localScale    = new Vector3(4, 2, 3);
        AssignMat(campR, matCampR);

        // ---- 10. 城门主体 ----
        var gateMain = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gateMain.name = "CityGate_Main";
        gateMain.transform.SetParent(parent.transform);
        gateMain.transform.localPosition = new Vector3(0, 0.75f, -4);
        gateMain.transform.localScale    = new Vector3(8, 1.5f, 1);
        AssignMat(gateMain, matGate);

        // ---- 11. 城门左塔 ----
        var gateTL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gateTL.name = "CityGate_Tower_L";
        gateTL.transform.SetParent(parent.transform);
        gateTL.transform.localPosition = new Vector3(-4.5f, 1.5f, -4);
        gateTL.transform.localScale    = new Vector3(1, 3, 1);
        AssignMat(gateTL, matGate);

        // ---- 12. 城门右塔 ----
        var gateTR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gateTR.name = "CityGate_Tower_R";
        gateTR.transform.SetParent(parent.transform);
        gateTR.transform.localPosition = new Vector3(4.5f, 1.5f, -4);
        gateTR.transform.localScale    = new Vector3(1, 3, 1);
        AssignMat(gateTR, matGate);

        // ---- 13. 怪物刷新区标记（透明，仅用于编辑器定位）----
        var spawnZone = new GameObject("MonsterSpawnZone");
        spawnZone.transform.SetParent(parent.transform);
        spawnZone.transform.localPosition = new Vector3(0, 0, 9);  // 场景上方

        var spawnL = new GameObject("SpawnPoint_L");
        spawnL.transform.SetParent(spawnZone.transform);
        spawnL.transform.localPosition = new Vector3(-10, 0, 0);

        var spawnR = new GameObject("SpawnPoint_R");
        spawnR.transform.SetParent(spawnZone.transform);
        spawnR.transform.localPosition = new Vector3(10, 0, 0);

        var spawnC = new GameObject("SpawnPoint_C");
        spawnC.transform.SetParent(spawnZone.transform);
        spawnC.transform.localPosition = new Vector3(0, 0, 0);

        // ---- 14. 摄像机确认（只检查，不重置，之前已设好）----
        var cam = Camera.main;
        if (cam != null)
        {
            Debug.Log($"[SetupPolar] 摄像机当前: pos={cam.transform.position} rot={cam.transform.eulerAngles} FOV={cam.fieldOfView}");
            // 如果位置偏差大则重置
            if (Vector3.Distance(cam.transform.position, new Vector3(0,22,-3)) > 1f)
            {
                cam.transform.position = new Vector3(0, 22, -3);
                cam.transform.rotation = Quaternion.Euler(78, 0, 0);
                cam.fieldOfView = 55;
                cam.backgroundColor = HexColor("#0B1D3A");
                cam.clearFlags = CameraClearFlags.SolidColor;
                Debug.Log("[SetupPolar] 摄像机参数已重置");
            }
            else
            {
                Debug.Log("[SetupPolar] 摄像机参数正常 ✅");
            }
        }

        // ---- 15. 保存场景 ----
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[SetupPolar] ===== 极地3D场景搭建完毕，请手动保存场景（Ctrl+S）=====");
        Debug.Log("[SetupPolar] 场景结构: PolarScene/SnowGround + BackMountain_L/R + CentralFortress + LeftCamp/RightCamp + CityGate_Main/Tower_L/Tower_R + MonsterSpawnZone");
    }

    static void DisableOldSceneObjects()
    {
        string[] oldNames = {
            "scene-modle", "SnowGround", "SkyBg",
            "SnowWall_L", "SnowWall_R", "Orange",
            "BigSheep", "201_Sheep"
        };
        foreach (var name in oldNames)
        {
            var obj = GameObject.Find(name);
            if (obj != null)
            {
                obj.SetActive(false);
                Debug.Log($"[SetupPolar] 禁用旧对象: {name}");
            }
        }
    }

    static Material EnsureMaterial(string matPath, Color color)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            // 创建 URP Lit 或 Standard 材质
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader);
            mat.color = color;
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else
        {
            mat.color = color;
            EditorUtility.SetDirty(mat);
        }
        return mat;
    }

    static void AssignMat(GameObject go, Material mat)
    {
        if (mat == null) return;
        var rend = go.GetComponent<Renderer>();
        if (rend != null) rend.sharedMaterial = mat;
    }

    static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
#endif
