using UnityEngine;
using UnityEditor;
using System.Reflection;

public class DiagnoseHPRuntime
{
    [MenuItem("Tools/DrscfZ/Diagnose HP Runtime (Play Mode)")]
    public static void Execute()
    {
        // 运行时检查 MonsterController._hpFillImage 实际指向哪个对象
        var monsters = Object.FindObjectsOfType<DrscfZ.Monster.MonsterController>();
        Debug.Log($"=== 运行时怪物HP诊断 ({monsters.Length}个怪物) ===");

        var field = typeof(DrscfZ.Monster.MonsterController)
            .GetField("_hpFillImage", BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var m in monsters)
        {
            var img = field?.GetValue(m) as UnityEngine.UI.Image;
            if (img == null)
            {
                Debug.LogWarning($"  [{m.name}] _hpFillImage = NULL ← 这是问题所在!");
            }
            else
            {
                string imgName = img.name;
                string path = GetPath(img.transform);
                float fill = img.fillAmount;
                var imgType = img.type;
                Debug.Log($"  [{m.name}] _hpFillImage → '{imgName}' (路径:{path}) fillAmount={fill:F3} type={imgType}");
            }

            // 同时显示 HPBarCanvas 结构
            var hpCanvas = m.transform.Find("HPBarCanvas");
            if (hpCanvas != null)
            {
                Debug.Log($"    HPBarCanvas active={hpCanvas.gameObject.activeSelf} scale={hpCanvas.localScale}");
                foreach (Transform c in hpCanvas)
                {
                    var cImg = c.GetComponent<UnityEngine.UI.Image>();
                    Debug.Log($"      子节点 '{c.name}' Image={cImg!=null} {(cImg!=null?$"fillAmount={cImg.fillAmount:F3} type={cImg.type}":"")}");
                }
            }
        }

        // 同样检查 WorkerController
        var workers = Object.FindObjectsOfType<DrscfZ.Survival.WorkerController>();
        Debug.Log($"\n=== 运行时矿工HP诊断 ({workers.Length}个矿工) ===");

        var wField = typeof(DrscfZ.Survival.WorkerController)
            .GetField("_hpFillImage", BindingFlags.NonPublic | BindingFlags.Instance);

        int shown = 0;
        foreach (var w in workers)
        {
            if (shown++ >= 3) break; // 只显示前3个避免刷屏
            var img = wField?.GetValue(w) as UnityEngine.UI.Image;
            if (img == null)
                Debug.LogWarning($"  [{w.name}] _hpFillImage = NULL");
            else
                Debug.Log($"  [{w.name}] _hpFillImage → '{img.name}' fillAmount={img.fillAmount:F3} type={img.type}");
        }
    }

    private static string GetPath(Transform t)
    {
        string p = t.name;
        var parent = t.parent;
        while (parent != null && parent.name != null) { p = parent.name + "/" + p; parent = parent.parent; }
        return p;
    }
}
