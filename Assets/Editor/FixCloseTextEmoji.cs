using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// 修复场景中所有 TMP_Text 含 ✕（U+2715）等 ChineseFont 不支持字符的问题
/// Tools → DrscfZ → Fix Close Text Emoji
/// </summary>
public static class FixCloseTextEmoji
{
    [MenuItem("Tools/DrscfZ/Fix Close Text Emoji")]
    public static void Execute()
    {
        int fixed_count = 0;

        // 扫描所有 TMP_Text（含非激活对象）
        var allTmps = Resources.FindObjectsOfTypeAll<TMP_Text>();
        foreach (var tmp in allTmps)
        {
            // 只处理已加载的场景对象（排除 Prefab Asset）
            if (tmp.gameObject.scene == null || !tmp.gameObject.scene.isLoaded)
                continue;

            string original = tmp.text;
            if (string.IsNullOrEmpty(original)) continue;

            // U+2715 ✕ → X
            string replaced = original
                .Replace("\u2715", "X")   // ✕ → X
                .Replace("\u2716", "X")   // ✖ → X
                .Replace("\u274C", "X")   // ❌ → X
                .Replace("✓", "√")        // ✓ → √（某些字体无√，但 ChineseFont 有）
                ;

            if (replaced != original)
            {
                Undo.RecordObject(tmp, "Fix emoji in TMP");
                tmp.text = replaced;
                EditorUtility.SetDirty(tmp);
                fixed_count++;
                Debug.Log($"[FixCloseTextEmoji] 修复: {tmp.gameObject.name} \"{original}\" → \"{replaced}\"");
            }
        }

        if (fixed_count > 0)
        {
            EditorSceneManager.SaveScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"[FixCloseTextEmoji] 完成，修复 {fixed_count} 个 TMP，场景已保存");
        }
        else
        {
            Debug.Log("[FixCloseTextEmoji] 未发现需要修复的 TMP 字符");
        }
    }
}
