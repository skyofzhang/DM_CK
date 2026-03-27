using UnityEngine;
using UnityEditor;

/// <summary>
/// Play Mode 下：自动实例化怪物 prefab、Initialize、
/// 连续 TakeDamage，并检查 fillAmount 变化
/// </summary>
public class TestHPBarRuntime
{
    [MenuItem("Tools/DrscfZ/Test HPBar Runtime (Play Mode Only)")]
    public static void Execute()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[HPBarTest] 请在 Play Mode 中运行！");
            return;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Monsters/KuanggongMonster_03.prefab");
        if (prefab == null) { Debug.LogError("[HPBarTest] 找不到 Monster_03 prefab"); return; }

        // 实例化并初始化
        var go = Object.Instantiate(prefab, new Vector3(0, 0, 0), Quaternion.identity);
        var mc = go.GetComponent<DrscfZ.Monster.MonsterController>();
        if (mc == null) { Debug.LogError("[HPBarTest] 找不到 MonsterController"); return; }

        // 用反射获取 _hpFillImage
        var fi = typeof(DrscfZ.Monster.MonsterController)
            .GetField("_hpFillImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var img0 = fi?.GetValue(mc) as UnityEngine.UI.Image;
        Debug.Log($"[HPBarTest] 实例化后（Initialize前）_hpFillImage={(img0==null?"NULL":img0.name)} fillAmount={(img0!=null?img0.fillAmount.ToString("F3"):"N/A")}");

        // 初始化
        mc.Initialize(100, 10, 1.5f, null);

        var img1 = fi?.GetValue(mc) as UnityEngine.UI.Image;
        Debug.Log($"[HPBarTest] Initialize(100hp) 后 _hpFillImage={(img1==null?"NULL":img1.name)} fillAmount={(img1!=null?img1.fillAmount.ToString("F3"):"N/A")}");

        // 模拟伤害
        mc.TakeDamage(30);
        var img2 = fi?.GetValue(mc) as UnityEngine.UI.Image;
        var targetFill2 = typeof(DrscfZ.Monster.MonsterController)
            .GetField("_hpBarTargetFill", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        float tgt2 = targetFill2 != null ? (float)targetFill2.GetValue(mc) : -1f;
        Debug.Log($"[HPBarTest] TakeDamage(30) 后 _hpFillImage={(img2==null?"NULL":img2.name)} fillAmount={(img2!=null?img2.fillAmount.ToString("F3"):"N/A")} targetFill={tgt2:F3}");
        Debug.Log($"  → fillAmount 应变化到 {tgt2:F3}（经 Lerp 多帧后）");

        // 直接强制设置 fillAmount 测试 Image 响应
        if (img2 != null)
        {
            img2.fillAmount = 0.3f;
            Debug.Log($"[HPBarTest] 强制 fillAmount=0.3 后实际值={(img2.fillAmount):F3}（应=0.300）");
            Debug.Log($"[HPBarTest] type={img2.type} fillMethod={img2.fillMethod} fillOrigin={img2.fillOrigin} enabled={img2.enabled}");
        }

        // 检查 HPBarCanvas 激活状态
        var canvasField = typeof(DrscfZ.Monster.MonsterController)
            .GetField("_hpBarCanvas", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var canvas = canvasField?.GetValue(mc) as Transform;
        Debug.Log($"[HPBarTest] _hpBarCanvas={(canvas==null?"NULL":canvas.name)} activeSelf={canvas?.gameObject.activeSelf}");

        // 直接找
        var directCanvas = go.transform.Find("HPBarCanvas");
        Debug.Log($"[HPBarTest] transform.Find(HPBarCanvas)={(directCanvas==null?"NULL":directCanvas.name)} activeSelf={directCanvas?.gameObject.activeSelf}");

        Object.Destroy(go, 5f);
        Debug.Log("[HPBarTest] 完成! 5秒后测试对象自动销毁。");
    }
}
