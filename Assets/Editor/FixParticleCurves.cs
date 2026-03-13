using UnityEngine;
using UnityEditor;

/// <summary>
/// 修复场景中所有粒子系统的 "Particle Velocity curves must all be in the same mode" 错误。
/// 原因：新建 ParticleSystem 时 VelocityOverLifetime 的 X/Y/Z 曲线可能使用了不同的 ParticleSystemCurveMode。
/// 解法：将所有轴强制改成 Constant 模式（值=0），然后禁用该模块。
/// </summary>
public class FixParticleCurves
{
    [MenuItem("Tools/Phase2/Fix All Particle Velocity Curves")]
    public static void Execute()
    {
        int fixed1 = 0;

        var allPS = Object.FindObjectsOfType<ParticleSystem>(true);
        foreach (var ps in allPS)
        {
            var so = new SerializedObject(ps);

            // VelocityModule 在序列化中的结构（Unity内部名称）
            // x/y/z 各有 scalar 和 curve 两套配置，还有 minMaxState
            // minMaxState: 0=Constant, 1=Curve, 2=RandomBetweenTwoConstants, 3=RandomBetweenTwoCurves

            bool dirty = false;

            // 强制 X/Y/Z 均改成 Constant(0) 并禁用模块
            var velEnabled = so.FindProperty("VelocityModule.enabled");
            var xState = so.FindProperty("VelocityModule.x.minMaxState");
            var yState = so.FindProperty("VelocityModule.y.minMaxState");
            var zState = so.FindProperty("VelocityModule.z.minMaxState");

            if (xState != null && yState != null && zState != null)
            {
                int xv = xState.intValue;
                int yv = yState.intValue;
                int zv = zState.intValue;

                if (xv != yv || yv != zv)
                {
                    // 全部强制改成 Constant (0)
                    xState.intValue = 0;
                    yState.intValue = 0;
                    zState.intValue = 0;
                    if (velEnabled != null) velEnabled.boolValue = false;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(ps);
                    dirty = true;
                    fixed1++;
                    Debug.Log($"[FixParticleCurves] Fixed '{ps.gameObject.name}' velocity curve modes: x={xv} y={yv} z={zv} → all Constant");
                }
            }
        }

        if (fixed1 > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        Debug.Log($"[FixParticleCurves] ✅ Scanned {allPS.Length} particle systems, fixed {fixed1}. Scene saved.");
    }
}
