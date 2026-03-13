#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

/// <summary>
/// 极地生存法则 — 竖屏场景初始化脚本
/// 执行：DrscfZ/Setup Portrait Scene
/// 功能：1) 摄像机俯视重设  2) 禁用热带场景  3) 极地占位背景
/// </summary>
public class SetupPortraitScene
{
    [MenuItem("DrscfZ/Setup Portrait Scene", false, 100)]
    public static void Execute()
    {
        Debug.Log("[SetupPortraitScene] ===== 开始执行竖屏场景初始化 =====");

        SetupCamera();
        SetupSceneBackground();
        DisableTropicalScene();

        // 保存场景
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

        Debug.Log("[SetupPortraitScene] ===== 完成！场景已保存 =====");
    }

    // ─────────────────────────────────────────
    // Phase 1: 摄像机俯视重设
    // ─────────────────────────────────────────
    static void SetupCamera()
    {
        // 找主摄像机
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            var camObj = GameObject.Find("Main Camera");
            if (camObj != null) mainCam = camObj.GetComponent<Camera>();
        }
        if (mainCam == null) { Debug.LogError("[SetupPortraitScene] 未找到 Main Camera"); return; }

        // 设置摄像机位置和角度（俯视竖屏）
        mainCam.transform.position = new Vector3(0f, 22f, -3f);
        mainCam.transform.rotation = Quaternion.Euler(78f, 0f, 0f);
        mainCam.fieldOfView = 55f;
        mainCam.backgroundColor = new Color(0.043f, 0.114f, 0.227f, 1f); // #0B1D3A
        mainCam.clearFlags = CameraClearFlags.SolidColor;

        // 设置 OrangeFollowCamera 参数
        var followCam = mainCam.GetComponent<DrscfZ.Systems.OrangeFollowCamera>();
        if (followCam == null) followCam = mainCam.GetComponentInChildren<DrscfZ.Systems.OrangeFollowCamera>();
        if (followCam != null)
        {
            // 用 SerializedObject 访问 private [SerializeField] 字段
            var so = new SerializedObject(followCam);
            so.FindProperty("cameraHeight").floatValue    = 22f;
            so.FindProperty("cameraDistance").floatValue  = 3f;
            so.FindProperty("pitchAngle").floatValue      = 78f;
            so.FindProperty("followSmooth").floatValue    = 5f;
            so.FindProperty("followX").boolValue          = true;
            so.FindProperty("enableDynamicHeight").boolValue = false;
            so.ApplyModifiedProperties();
            Debug.Log("[SetupPortraitScene] OrangeFollowCamera 参数已更新");
        }
        else
        {
            Debug.LogWarning("[SetupPortraitScene] 未找到 OrangeFollowCamera 组件，跳过");
        }

        EditorUtility.SetDirty(mainCam.gameObject);
        Debug.Log("[SetupPortraitScene] Phase 1 完成: 摄像机已设置为俯视竖屏视角");
    }

    // ─────────────────────────────────────────
    // Phase 2A: 禁用热带场景模型
    // ─────────────────────────────────────────
    static void DisableTropicalScene()
    {
        // 禁用热带场景模型
        var sceneModel = GameObject.Find("scene-modle");
        if (sceneModel != null)
        {
            sceneModel.SetActive(false);
            EditorUtility.SetDirty(sceneModel);
            Debug.Log("[SetupPortraitScene] scene-modle 已禁用");
        }
        else
        {
            Debug.LogWarning("[SetupPortraitScene] 未找到 scene-modle，尝试查找 [Scene] 下的子对象");
            var sceneRoot = GameObject.Find("[Scene]");
            if (sceneRoot != null)
            {
                var t = sceneRoot.transform.Find("scene-modle");
                if (t != null) { t.gameObject.SetActive(false); EditorUtility.SetDirty(t.gameObject); Debug.Log("[SetupPortraitScene] [Scene]/scene-modle 已禁用"); }
            }
        }

        // 禁用 Terrain（热带地形）
        var terrain = GameObject.Find("Terrain");
        if (terrain != null)
        {
            terrain.SetActive(false);
            EditorUtility.SetDirty(terrain);
            Debug.Log("[SetupPortraitScene] Terrain 已禁用");
        }
    }

    // ─────────────────────────────────────────
    // Phase 2B: 创建极地占位背景
    // ─────────────────────────────────────────
    static void SetupSceneBackground()
    {
        // 清理旧的占位背景（如果存在）
        DestroyExisting("SnowGround");
        DestroyExisting("SkyBg");
        DestroyExisting("SnowWall_L");
        DestroyExisting("SnowWall_R");

        // 场景容器
        var sceneRoot = GameObject.Find("[Scene]");
        Transform parent = sceneRoot != null ? sceneRoot.transform : null;

        // ── 雪地地面 ──
        var snowGround = GameObject.CreatePrimitive(PrimitiveType.Quad);
        snowGround.name = "SnowGround";
        snowGround.transform.SetParent(parent, false);
        snowGround.transform.position   = new Vector3(0f, -0.1f, 4f);
        snowGround.transform.rotation   = Quaternion.Euler(90f, 0f, 0f);
        snowGround.transform.localScale = new Vector3(120f, 40f, 1f);
        var groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        groundMat.color = new Color(0.92f, 0.96f, 1.0f); // 冰雪白
        groundMat.name  = "Mat_SnowGround";
        snowGround.GetComponent<Renderer>().material = groundMat;
        // 删除碰撞体（纯视觉）
        Object.DestroyImmediate(snowGround.GetComponent<MeshCollider>());
        EditorUtility.SetDirty(snowGround);

        // ── 天空背景（大Quad放在场景后方）──
        var skyBg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        skyBg.name = "SkyBg";
        skyBg.transform.SetParent(parent, false);
        skyBg.transform.position   = new Vector3(0f, 8f, 35f);
        skyBg.transform.rotation   = Quaternion.Euler(0f, 180f, 0f);
        skyBg.transform.localScale = new Vector3(120f, 50f, 1f);
        var skyMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        // 渐变冰蓝天空
        skyMat.color = new Color(0.5f, 0.78f, 0.95f); // 冰蓝天空
        skyMat.name  = "Mat_SkyBg";
        skyBg.GetComponent<Renderer>().material = skyMat;
        Object.DestroyImmediate(skyBg.GetComponent<MeshCollider>());
        EditorUtility.SetDirty(skyBg);

        // ── 左侧冰山边界（装饰）──
        var wallL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallL.name = "SnowWall_L";
        wallL.transform.SetParent(parent, false);
        wallL.transform.position   = new Vector3(-14f, 2f, 4f);
        wallL.transform.localScale = new Vector3(2f, 6f, 20f);
        var wallMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        wallMat.color = new Color(0.75f, 0.88f, 0.98f);
        wallMat.name  = "Mat_IceWall";
        wallL.GetComponent<Renderer>().material = wallMat;
        Object.DestroyImmediate(wallL.GetComponent<BoxCollider>());
        EditorUtility.SetDirty(wallL);

        // ── 右侧冰山边界（装饰）──
        var wallR = Object.Instantiate(wallL);
        wallR.name = "SnowWall_R";
        wallR.transform.SetParent(parent, false);
        wallR.transform.position = new Vector3(14f, 2f, 4f);
        EditorUtility.SetDirty(wallR);

        // ── Orange 换白色材质（雪球）──
        var orange = GameObject.Find("Orange");
        if (orange != null)
        {
            var renderer = orange.GetComponent<Renderer>();
            if (renderer != null)
            {
                var snowballMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                snowballMat.color = new Color(0.95f, 0.98f, 1.0f); // 雪白
                snowballMat.name  = "Mat_Snowball";
                // 增加金属感模拟冰晶
                snowballMat.SetFloat("_Smoothness", 0.6f);
                renderer.material = snowballMat;
                EditorUtility.SetDirty(orange);
                Debug.Log("[SetupPortraitScene] Orange 已换为雪球白色材质");
            }
        }

        Debug.Log("[SetupPortraitScene] Phase 2 完成: 极地占位背景已创建");
    }

    static void DestroyExisting(string name)
    {
        var obj = GameObject.Find(name);
        if (obj != null) { Object.DestroyImmediate(obj); }
    }
}
#endif
