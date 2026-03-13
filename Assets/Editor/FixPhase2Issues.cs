using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// 修复 Phase 2 运行时两个已知问题：
///   1. Gift_Canvas 粒子系统 VelocityOverLifetime 曲线模式不一致 → 禁用该模块
///   2. WorkerBubble/BubbleIcon 使用 LiberationSans SDF → 改为 ChineseFont SDF（支持 emoji）
/// </summary>
public class FixPhase2Issues
{
    [MenuItem("Tools/Phase2/Fix Particle VelocityCurves + BubbleIcon Font")]
    public static void Execute()
    {
        int fixedParticles = 0;
        int fixedFonts = 0;

        // ── 1. 修复所有 Gift_Canvas 下粒子系统的 VelocityOverLifetime ────────
        var giftCanvas = GameObject.Find("Gift_Canvas");
        if (giftCanvas != null)
        {
            var particles = giftCanvas.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in particles)
            {
                // 禁用 VelocityOverLifetime 模块（避免曲线模式不一致错误）
                var vel = ps.velocityOverLifetime;
                if (vel.enabled)
                {
                    var so = new SerializedObject(ps);
                    // 找到 VelocityOverLifetime.enabled 序列化字段
                    var velModule = so.FindProperty("VelocityModule.enabled");
                    if (velModule != null)
                    {
                        velModule.boolValue = false;
                        so.ApplyModifiedProperties();
                        EditorUtility.SetDirty(ps);
                        fixedParticles++;
                    }
                }
            }
        }

        // ── 2. WorkerPool 所有 BubbleIcon → ChineseFont SDF ─────────────────
        var chineseFontPath = "Assets/Resources/Fonts/ChineseFont SDF.asset";
        var chineseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(chineseFontPath);

        if (chineseFont == null)
        {
            Debug.LogWarning($"[FixPhase2Issues] ChineseFont SDF not found at: {chineseFontPath}");
        }
        else
        {
            var workerPool = GameObject.Find("WorkerPool");
            if (workerPool != null)
            {
                var allTexts = workerPool.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var tmp in allTexts)
                {
                    if (tmp.gameObject.name == "BubbleIcon")
                    {
                        tmp.font = chineseFont;
                        EditorUtility.SetDirty(tmp);
                        fixedFonts++;
                    }
                }
            }
        }

        // ── 保存场景 ────────────────────────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[FixPhase2Issues] ✅ Fixed {fixedParticles} particle VelocityModules, {fixedFonts} BubbleIcon fonts → ChineseFont SDF. Scene saved.");
    }
}
