using UnityEngine;
using UnityEditor;
using System.Reflection;

public class DiagnoseHPLive
{
    [MenuItem("Tools/DrscfZ/Diagnose HP Live (Play Mode)")]
    public static void Execute()
    {
        var mField  = typeof(DrscfZ.Monster.MonsterController).GetField("_hpFillImage",   BindingFlags.NonPublic | BindingFlags.Instance);
        var mHp     = typeof(DrscfZ.Monster.MonsterController).GetField("_currentHp",      BindingFlags.NonPublic | BindingFlags.Instance);
        var mMaxHp  = typeof(DrscfZ.Monster.MonsterController).GetField("_maxHp",          BindingFlags.NonPublic | BindingFlags.Instance);
        var mTarget = typeof(DrscfZ.Monster.MonsterController).GetField("_hpBarTargetFill",BindingFlags.NonPublic | BindingFlags.Instance);
        var mCanvas = typeof(DrscfZ.Monster.MonsterController).GetField("_hpBarCanvas",    BindingFlags.NonPublic | BindingFlags.Instance);

        var monsters = Object.FindObjectsOfType<DrscfZ.Monster.MonsterController>();
        Debug.Log($"[HPLive] 找到 {monsters.Length} 个怪物");

        foreach (var m in monsters)
        {
            var img    = mField?.GetValue(m)  as UnityEngine.UI.Image;
            var cvs    = mCanvas?.GetValue(m) as Transform;
            int curHp  = mHp    != null ? (int)mHp.GetValue(m)        : -1;
            int maxHp  = mMaxHp != null ? (int)mMaxHp.GetValue(m)     : -1;
            float tgt  = mTarget != null ? (float)mTarget.GetValue(m) : -1f;

            Debug.Log($"[HPLive] {m.name}:" +
                $"\n  _hpFillImage = {(img==null?"NULL":img.name)}" +
                $"\n  fillAmount   = {(img!=null?img.fillAmount.ToString("F3"):"N/A")}" +
                $"\n  _hpBarTarget = {tgt:F3}" +
                $"\n  hp={curHp}/{maxHp}" +
                $"\n  _hpBarCanvas = {(cvs==null?"NULL":cvs.name)} active={cvs?.gameObject.activeSelf}" +
                $"\n  img.type     = {(img!=null?img.type.ToString():"N/A")}");

            // 强制打一次伤害，看 fillAmount 是否变化
            if (img != null && maxHp > 0)
            {
                float before = img.fillAmount;
                m.TakeDamage(5);
                float after = img.fillAmount;
                float newTgt = mTarget != null ? (float)mTarget.GetValue(m) : -1f;
                Debug.Log($"[HPLive] {m.name} 强制TakeDamage(5):" +
                    $" fillAmount {before:F3}→{after:F3}  targetFill→{newTgt:F3}");
            }
        }

        // Worker 诊断
        var wField  = typeof(DrscfZ.Survival.WorkerController).GetField("_hpFillImage",   BindingFlags.NonPublic | BindingFlags.Instance);
        var wTarget = typeof(DrscfZ.Survival.WorkerController).GetField("_hpBarTargetFill",BindingFlags.NonPublic | BindingFlags.Instance);
        var workers = Object.FindObjectsOfType<DrscfZ.Survival.WorkerController>();
        Debug.Log($"[HPLive] 找到 {workers.Length} 个矿工（取前3个）");
        int n = 0;
        foreach (var w in workers)
        {
            if (n++ >= 3) break;
            var img   = wField?.GetValue(w)  as UnityEngine.UI.Image;
            float tgt = wTarget != null ? (float)wTarget.GetValue(w) : -1f;
            Debug.Log($"[HPLive] {w.name}: _hpFillImage={( img==null?"NULL":img.name)} fillAmount={( img!=null?img.fillAmount.ToString("F3"):"N/A")} targetFill={tgt:F3}");
        }
    }
}
