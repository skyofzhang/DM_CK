// Copyright 2026 极地生存法则
// 一键清理场景 TMP 组件中的 emoji 字面量：用中文单字替代独占按钮，其余前缀 emoji 去掉。
// 用途：消除 Unity Console 中 127+ "The character with Unicode value \\uXXXX was not found
//       in the [Inter-Regular SDF] font asset" 警告。
// 菜单：Tools → DrscfZ → Clean Scene TMP Emoji
// 规则见 CLAUDE.md「已知踩坑 UI · emoji 显示方块」：改用中文文字替代。

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

namespace DrscfZ.EditorTools
{
    public static class CleanSceneEmoji
    {
        // 独占按钮（整段文本只有一个 emoji）→ 用单个中文字替代，保持按钮视觉
        private static readonly Dictionary<string, string> SoloReplacements = new()
        {
            { "⚡",  "加" },   // BoostButton
            { "🌊",  "浪" },   // EventButton
            { "🔥",  "热" },   // HeatIcon
            { "🍖",  "粮" },   // FoodIcon
            { "🪨",  "煤" },   // CoalIcon
            { "💎",  "矿" },   // OreIcon
            { "🏰",  "堡" },   // GateIcon
            { "🎰",  "盘" },   // RouletteIcon
            { "🛒",  "购" },   // ShopIcon
            { "🛡",  "盾" },   // GateUpgradeIcon
            { "⚔",  "战" },   // TribeWarIcon
        };

        [MenuItem("Tools/DrscfZ/Clean Scene TMP Emoji")]
        public static void Execute()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogWarning("[CleanSceneEmoji] No active scene.");
                return;
            }

            int tmpCount = 0;
            int uiTextCount = 0;
            int tmp3dCount = 0;
            var changed = new List<string>(64);

            // ── TextMeshProUGUI（UI） ──
            foreach (var t in Object.FindObjectsByType<TextMeshProUGUI>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var cleaned = Clean(t.text);
                if (cleaned != t.text)
                {
                    string path = GetPath(t.transform);
                    changed.Add($"TMP[UGUI]  {path}\n    \"{t.text}\" → \"{cleaned}\"");
                    Undo.RecordObject(t, "Clean TMP Emoji");
                    t.text = cleaned;
                    EditorUtility.SetDirty(t);
                    tmpCount++;
                }
            }

            // ── TextMeshPro（3D） ──
            foreach (var t in Object.FindObjectsByType<TextMeshPro>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var cleaned = Clean(t.text);
                if (cleaned != t.text)
                {
                    string path = GetPath(t.transform);
                    changed.Add($"TMP[3D]    {path}\n    \"{t.text}\" → \"{cleaned}\"");
                    Undo.RecordObject(t, "Clean TMP Emoji");
                    t.text = cleaned;
                    EditorUtility.SetDirty(t);
                    tmp3dCount++;
                }
            }

            // ── UnityEngine.UI.Text（老式） ──
            foreach (var t in Object.FindObjectsByType<UnityEngine.UI.Text>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var cleaned = Clean(t.text);
                if (cleaned != t.text)
                {
                    string path = GetPath(t.transform);
                    changed.Add($"UI.Text    {path}\n    \"{t.text}\" → \"{cleaned}\"");
                    Undo.RecordObject(t, "Clean TMP Emoji");
                    t.text = cleaned;
                    EditorUtility.SetDirty(t);
                    uiTextCount++;
                }
            }

            int total = tmpCount + tmp3dCount + uiTextCount;
            Debug.Log($"[CleanSceneEmoji] Done: TMP_UGUI={tmpCount} TMP_3D={tmp3dCount} UI.Text={uiTextCount} (total={total})");

            if (total > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log("[CleanSceneEmoji] 场景已标记 dirty。请在 Unity 菜单 File → Save 保存场景（Ctrl+S）。");
                foreach (var line in changed)
                    Debug.Log("[CleanSceneEmoji] " + line);
            }
            else
            {
                Debug.Log("[CleanSceneEmoji] 场景中未检出需要清理的 emoji 文本。");
            }
        }

        /// <summary>清洗规则：独占 emoji 用映射表替换；混合文本剥离所有 emoji + VS16 + 连续空格归一。</summary>
        public static string Clean(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var trimmed = input.Trim();
            if (SoloReplacements.TryGetValue(trimmed, out var rep))
                return rep;

            var sb = new StringBuilder(input.Length);
            for (int i = 0; i < input.Length; i++)
            {
                int cp;
                bool isSurrogate = char.IsHighSurrogate(input[i]) && i + 1 < input.Length &&
                                   char.IsLowSurrogate(input[i + 1]);
                if (isSurrogate)
                    cp = char.ConvertToUtf32(input, i);
                else
                    cp = input[i];

                if (IsRemovableEmoji(cp))
                {
                    if (isSurrogate) i++;
                    continue;
                }

                sb.Append(input[i]);
                if (isSurrogate)
                {
                    sb.Append(input[i + 1]);
                    i++;
                }
            }

            // 合并多空格 + 去首尾
            var s = sb.ToString();
            while (s.Contains("  ")) s = s.Replace("  ", " ");
            return s.Trim();
        }

        private static bool IsRemovableEmoji(int cp)
        {
            // 覆盖 Unity console 警告的全部范围 + 常见视觉符号
            return (cp >= 0x1F000 && cp <= 0x1FFFF)   // BMP extended pictographs
                || (cp >= 0x2600  && cp <= 0x27BF)    // Misc symbols & dingbats（含 ⚡⚔⚙❄）
                || (cp == 0xFE0F)                      // Variation Selector-16
                || (cp >= 0x2300  && cp <= 0x23FF)    // Misc technical（含 ⌚）
                || (cp >= 0x25A0  && cp <= 0x25FF)    // Geometric（含 ▶ ▲ ■）
                || (cp >= 0x2B00  && cp <= 0x2BFF);   // Misc symbols arrows
        }

        private static string GetPath(Transform t)
        {
            var stack = new Stack<string>();
            while (t != null) { stack.Push(t.name); t = t.parent; }
            return string.Join("/", stack);
        }
    }
}
