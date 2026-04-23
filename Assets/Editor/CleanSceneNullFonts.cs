// Copyright 2026 极地生存法则
// 兜底扫描当前场景所有 TMP 组件,若 font 为 null,设置为 AlibabaPuHuiTi SDF。
// AlibabaPuHuiTi SDF 不存在时兜底用 ChineseFont SDF。
//
// 菜单：Tools → DrscfZ → Clean Scene Null Fonts
//
// 使用场景: UnifyFontsToAlibaba 批处理后,某些动态生成的 TMP 可能仍是 null,
// 或者手工新建组件忘记赋字体,一键打补丁。
//
// 关键约束:
//   - 禁止 EditorUtility.DisplayDialog
//   - 只修复 font == null,不覆盖已有字体(和 UnifyFontsToAlibaba 分工:那个负责统一,这个只兜底)
//   - 场景保存用 EditorSceneManager.SaveScene()

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

namespace DrscfZ.EditorTools
{
    public static class CleanSceneNullFonts
    {
        private const string PrimaryFontResourcePath = "Fonts/AlibabaPuHuiTi-3-85-Bold SDF";
        private const string FallbackFontResourcePath = "Fonts/ChineseFont SDF";

        [MenuItem("Tools/DrscfZ/Clean Scene Null Fonts")]
        public static void Execute()
        {
            var primary = Resources.Load<TMP_FontAsset>(PrimaryFontResourcePath);
            var fallback = Resources.Load<TMP_FontAsset>(FallbackFontResourcePath);

            TMP_FontAsset target = primary != null ? primary : fallback;
            if (target == null)
            {
                Debug.LogError(
                    $"[CleanSceneNullFonts] 找不到 AlibabaPuHuiTi SDF 也找不到 ChineseFont SDF。" +
                    "请先执行 Tools → DrscfZ → Generate Alibaba TMP Font Asset。");
                return;
            }

            string usedName = target.name;
            Debug.Log($"[CleanSceneNullFonts] 使用字体: {usedName}" +
                      (target == fallback ? " (fallback, Alibaba 未生成)" : ""));

            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogWarning("[CleanSceneNullFonts] 当前场景无效");
                return;
            }

            int fixedCount = 0;
            var fixedPaths = new List<string>();

            // 场景所有 TMP 组件(含隐藏)
            foreach (var tmp in Object.FindObjectsByType<TMP_Text>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (tmp == null) continue;
                if (tmp.font != null) continue;

                var so = new SerializedObject(tmp);
                var prop = so.FindProperty("m_fontAsset");
                if (prop != null)
                {
                    prop.objectReferenceValue = target;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                else
                {
                    tmp.font = target;
                }
                EditorUtility.SetDirty(tmp);
                fixedCount++;
                fixedPaths.Add(GetPath(tmp.transform));
            }

            if (fixedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[CleanSceneNullFonts] ✅ 修复 {fixedCount} 个 null 字体,场景已保存");
                foreach (var p in fixedPaths)
                    Debug.Log($"[CleanSceneNullFonts]   修复: {p}");
            }
            else
            {
                Debug.Log("[CleanSceneNullFonts] 场景中没有 null 字体,无需修复");
            }
        }

        private static string GetPath(Transform t)
        {
            var stack = new Stack<string>();
            while (t != null) { stack.Push(t.name); t = t.parent; }
            return string.Join("/", stack);
        }
    }
}
