using UnityEngine;
using UnityEditor;

public class DiagnoseHPBars
{
    [MenuItem("Tools/DrscfZ/Diagnose HP Bars")]
    public static void Execute()
    {
        // 查找场景中的 Worker_00
        var worker = GameObject.Find("Worker_00");
        if (worker != null)
        {
            Debug.Log($"=== Worker_00 HPBarCanvas 结构 ===");
            var hpCanvas = worker.transform.Find("HPBarCanvas");
            Debug.Log($"HPBarCanvas found: {hpCanvas != null}");
            if (hpCanvas != null)
            {
                PrintChildren(hpCanvas, "  ");
                // 尝试找 HPFill
                var hpFill = hpCanvas.Find("HPFill");
                Debug.Log($"HPFill (direct child): {hpFill != null}");
                var img = hpCanvas.GetComponentInChildren<UnityEngine.UI.Image>(true);
                Debug.Log($"Image GetComponentInChildren: {img?.name}");
            }
        }
        else Debug.LogWarning("Worker_00 not found in scene!");

        // 查找 Monster prefab 实例（运行时才有）
        var monsters = Object.FindObjectsOfType<DrscfZ.Monster.MonsterController>();
        Debug.Log($"=== Monster HP 诊断：找到 {monsters.Length} 个怪物 ===");
        foreach (var m in monsters)
        {
            var hpCanvas = m.transform.Find("HPBarCanvas");
            Debug.Log($"Monster '{m.name}': HPBarCanvas={hpCanvas!=null}");
            if (hpCanvas != null)
            {
                PrintChildren(hpCanvas, "  ");
                var hpFill = hpCanvas.Find("HPFill");
                Debug.Log($"  HPFill (direct child): {hpFill != null}");
                var img = hpCanvas.GetComponentInChildren<UnityEngine.UI.Image>(true);
                Debug.Log($"  Image GetComponentInChildren: {img?.name}");
            }
        }

        // 查找 KuanggongMonster 预制件
        string[] prefabPaths = {
            "Assets/Prefabs/Monsters/KuanggongMonster_03.prefab",
            "Assets/Prefabs/Monsters/KuanggongMonster_04.prefab",
            "Assets/Prefabs/Monsters/KuanggongBoss_05.prefab",
        };
        foreach (var path in prefabPaths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { Debug.LogWarning($"Prefab not found: {path}"); continue; }
            var hpCanvas = prefab.transform.Find("HPBarCanvas");
            Debug.Log($"\nPrefab '{prefab.name}': HPBarCanvas={hpCanvas!=null}");
            if (hpCanvas != null)
            {
                PrintChildren(hpCanvas, "  ");
                var hpFill = hpCanvas.Find("HPFill");
                Debug.Log($"  HPFill (direct child): {hpFill != null}");
                var img = hpCanvas.GetComponentInChildren<UnityEngine.UI.Image>(true);
                Debug.Log($"  Image (GetComponentInChildren): {img?.name}");
            }
        }
    }

    private static void PrintChildren(Transform t, string indent)
    {
        foreach (Transform child in t)
        {
            var img = child.GetComponent<UnityEngine.UI.Image>();
            var slider = child.GetComponent<UnityEngine.UI.Slider>();
            Debug.Log($"{indent}- '{child.name}' Image={img!=null} Slider={slider!=null}");
            PrintChildren(child, indent + "  ");
        }
    }
}
