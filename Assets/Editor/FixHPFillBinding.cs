using UnityEngine;
using UnityEditor;
using System.Reflection;

/// <summary>
/// 永久修复 Monster/Worker Prefab 中 _hpFillImage Inspector 绑定
/// 确保指向 HPBarCanvas/HPFill，而非 Background
/// </summary>
public class FixHPFillBinding
{
    [MenuItem("Tools/DrscfZ/Fix HPFill Inspector Binding")]
    public static void Execute()
    {
        int fixedCount = 0;

        // Monster Prefabs
        string[] paths = {
            "Assets/Prefabs/Monsters/KuanggongMonster_03.prefab",
            "Assets/Prefabs/Monsters/KuanggongMonster_04.prefab",
            "Assets/Prefabs/Monsters/KuanggongBoss_05.prefab",
            "Assets/Prefabs/Characters/KuanggongWorker_01.prefab",
            "Assets/Prefabs/Characters/KuanggongWorker_02.prefab",
        };

        foreach (var path in paths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { Debug.LogWarning($"[FixHPFill] 未找到: {path}"); continue; }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                var root = scope.prefabContentsRoot;
                var hpCanvas = root.transform.Find("HPBarCanvas");
                if (hpCanvas == null) { Debug.LogWarning($"[FixHPFill] {root.name}: 无HPBarCanvas"); continue; }

                // 找 HPFill（按名称，不用 GetComponentInChildren 避免拿到 Background）
                var hpFillTr = hpCanvas.Find("HPFill");
                if (hpFillTr == null)
                {
                    Debug.LogWarning($"[FixHPFill] {root.name}: 找不到 HPBarCanvas/HPFill");
                    // 打印子节点帮助排查
                    foreach (Transform c in hpCanvas)
                        Debug.Log($"  子节点: '{c.name}'");
                    continue;
                }

                var hpFillImg = hpFillTr.GetComponent<UnityEngine.UI.Image>();
                if (hpFillImg == null) { Debug.LogWarning($"[FixHPFill] {root.name}: HPFill 无 Image"); continue; }

                // 确保 HPFill 是 Filled 类型
                if (hpFillImg.type != UnityEngine.UI.Image.Type.Filled)
                {
                    hpFillImg.type = UnityEngine.UI.Image.Type.Filled;
                    hpFillImg.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
                    hpFillImg.fillOrigin = 0;
                    Debug.Log($"[FixHPFill] {root.name}: HPFill ImageType 已修正为 Filled");
                }
                hpFillImg.fillAmount = 1f;

                // 用 SerializedObject 写入 _hpFillImage 字段
                bool isMonster = root.GetComponent<DrscfZ.Monster.MonsterController>() != null;
                bool isWorker  = root.GetComponent<DrscfZ.Survival.WorkerController>() != null;

                Component target = isMonster
                    ? (Component)root.GetComponent<DrscfZ.Monster.MonsterController>()
                    : (Component)root.GetComponent<DrscfZ.Survival.WorkerController>();

                if (target == null)
                {
                    Debug.LogWarning($"[FixHPFill] {root.name}: 找不到 Monster/WorkerController");
                    continue;
                }

                var so = new SerializedObject(target);
                var prop = so.FindProperty("_hpFillImage");
                if (prop == null)
                {
                    Debug.LogWarning($"[FixHPFill] {root.name}: 找不到 _hpFillImage 序列化字段");
                    continue;
                }

                var oldRef = prop.objectReferenceValue;
                prop.objectReferenceValue = hpFillImg;
                so.ApplyModifiedProperties();

                string oldName = oldRef != null ? ((UnityEngine.Object)oldRef).name : "null";
                Debug.Log($"[FixHPFill] ✓ {root.name}: _hpFillImage {oldName} → HPFill");
                fixedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 同步修复场景中 Worker 实例（非运行时，仅 Edit 模式）
        if (!Application.isPlaying)
        {
            var workers = Object.FindObjectsOfType<DrscfZ.Survival.WorkerController>(true);
            foreach (var w in workers)
            {
                var hpCanvas = w.transform.Find("HPBarCanvas");
                if (hpCanvas == null) continue;
                var hpFillTr = hpCanvas.Find("HPFill");
                if (hpFillTr == null) continue;
                var img = hpFillTr.GetComponent<UnityEngine.UI.Image>();
                if (img == null) continue;

                var so = new SerializedObject(w);
                var prop = so.FindProperty("_hpFillImage");
                if (prop != null && prop.objectReferenceValue != img)
                {
                    prop.objectReferenceValue = img;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(w);
                    fixedCount++;
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        Debug.Log($"[FixHPFill] 完成! 共修复 {fixedCount} 个绑定");
    }
}
