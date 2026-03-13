using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// 战斗UI文字描边 + 字号放大
/// 目标：GameUIPanel 下所有过小的 TMP 文字统一放大并确认 Outline 材质
/// 以及 SceneUI3D 下世界空间标签使用 Outline 材质
/// </summary>
public class FixBattleUIText
{
    public static void Execute()
    {
        var outlineMat = Resources.Load<Material>("Fonts/ChineseFont SDF - Outline");
        if (outlineMat == null)
        {
            Debug.LogError("[FixBattleUIText] 找不到 Resources/Fonts/ChineseFont SDF - Outline 材质，请检查路径");
            return;
        }

        int fixedCount = 0;

        // ── 1. GameUIPanel 下所有 TextMeshProUGUI ──────────────────────
        var gameUIPanel = GameObject.Find("GameUIPanel");
        if (gameUIPanel != null)
        {
            var tmps = gameUIPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tmps)
            {
                bool changed = false;

                // 确保使用 Outline 材质（已有的不重复设置）
                if (tmp.fontSharedMaterial != outlineMat)
                {
                    tmp.fontSharedMaterial = outlineMat;
                    changed = true;
                }

                // 字号过小的场景：PlayerName(18→24)、PlayerForce(15→20)、
                // RankText / PlayerStreak 等小号文字 → 提升到可读范围
                if (tmp.fontSize < 20f)
                {
                    tmp.fontSize = 20f;
                    changed = true;
                }
                else if (tmp.fontSize < 24f && tmp.fontSize >= 18f)
                {
                    // PlayerName 一类 18px → 24px
                    tmp.fontSize = 24f;
                    changed = true;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(tmp);
                    fixedCount++;
                }
            }
            Debug.Log($"[FixBattleUIText] GameUIPanel: 处理了 {tmps.Length} 个 TMP，修改了 {fixedCount} 个");
        }
        else
        {
            Debug.LogWarning("[FixBattleUIText] 找不到 GameUIPanel");
        }

        // ── 2. SceneUI3D 世界空间标签（TextMeshPro，非 UGUI）──────────
        int worldFixed = 0;
        var sceneUI3D = GameObject.Find("SceneUI3D");
        if (sceneUI3D != null)
        {
            // 世界空间 TMP（TextMeshPro，不是 TextMeshProUGUI）
            var worldTmps = sceneUI3D.GetComponentsInChildren<TextMeshPro>(true);
            foreach (var tmp in worldTmps)
            {
                bool changed = false;

                // 世界空间标签使用 Outline 材质
                if (tmp.fontSharedMaterial != outlineMat)
                {
                    tmp.fontSharedMaterial = outlineMat;
                    changed = true;
                }

                // 世界空间字号：当前是 2.2，放大到 2.8（scale=0.1 的父级下）
                if (tmp.fontSize < 2.8f)
                {
                    tmp.fontSize = 2.8f;
                    changed = true;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(tmp);
                    worldFixed++;
                }
            }
            Debug.Log($"[FixBattleUIText] SceneUI3D: 处理了 {worldTmps.Length} 个 WorldSpace TMP，修改了 {worldFixed} 个");
        }
        else
        {
            Debug.LogWarning("[FixBattleUIText] 找不到 SceneUI3D");
        }

        // ── 3. 保存场景 ─────────────────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[FixBattleUIText] 完成。GameUIPanel修改 {fixedCount} 个，SceneUI3D修改 {worldFixed} 个，场景已保存");
    }
}
