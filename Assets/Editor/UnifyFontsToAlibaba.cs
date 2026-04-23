// Copyright 2026 极地生存法则
// 批量扫描项目中所有场景 + Prefab 的 TMP 字体,统一换为 AlibabaPuHuiTi SDF。
// 对 Runtime C# 中 AddComponent<TextMeshProUGUI>() 但没有字体赋值的行,
// 不修改 .cs 文件,只把清单写到 Temp/UnifyFontsMissing.txt（第 3 步 FixRuntimeFontDefaults 据此注入）。
//
// 菜单：Tools → DrscfZ → Unify All Fonts → AlibabaPuHuiTi
//
// 前置：必须先跑 Tools → DrscfZ → Generate Alibaba TMP Font Asset
//
// 关键约束（CLAUDE.md）：
//   - 禁止 EditorUtility.DisplayDialog（阻塞进程）
//   - 场景保存用 EditorSceneManager.SaveScene(),不要 Coplay save_scene
//   - 不要改 TMP 颜色,颜色已在其他脚本处理(m_fontColor32)
//   - 过滤 Assets/Res/DGMT_data/ 和 Packages/,避免动源模型资源
//   - 只写 m_fontAsset 字段,通过 SerializedObject 确保 Prefab override 干净

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace DrscfZ.EditorTools
{
    public static class UnifyFontsToAlibaba
    {
        private const string TargetFontResourcePath = "Fonts/AlibabaPuHuiTi-3-85-Bold SDF";
        private const string TargetFontAssetPath = "Assets/Resources/Fonts/AlibabaPuHuiTi-3-85-Bold SDF.asset";
        private const string MissingReportPath = "Temp/UnifyFontsMissing.txt";

        // 跳过这些路径下的资源(源模型 / Packages / 工具自身)
        private static readonly string[] IgnorePathPrefixes =
        {
            "Assets/Res/DGMT_data/",
            "Packages/",
            "Assets/TextMesh Pro/",
        };

        [MenuItem("Tools/DrscfZ/Unify All Fonts → AlibabaPuHuiTi")]
        public static void Execute()
        {
            // 1. 加载目标字体
            var targetFont = Resources.Load<TMP_FontAsset>(TargetFontResourcePath);
            if (targetFont == null)
            {
                // Resources.Load 失败时兜底尝试 AssetDatabase
                targetFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TargetFontAssetPath);
            }
            if (targetFont == null)
            {
                Debug.LogError(
                    "[UnifyFontsToAlibaba] 目标字体未找到,请先执行 " +
                    "Tools → DrscfZ → Generate Alibaba TMP Font Asset 生成 " +
                    TargetFontAssetPath);
                return;
            }

            Debug.Log($"[UnifyFontsToAlibaba] 目标字体: {targetFont.name}");

            // ───── 扫描 1: 所有场景 ─────
            int sceneTmpCount = ProcessAllScenes(targetFont);

            // ───── 扫描 2: 所有 Prefab ─────
            int prefabTmpCount = ProcessAllPrefabs(targetFont);

            // ───── 扫描 3: Runtime C# 缺失字体 ─────
            int missingScriptLines = ReportRuntimeMissingFonts();

            Debug.Log(
                $"[UnifyFontsToAlibaba] ✅ 完成: 场景 TMP={sceneTmpCount} 个 / " +
                $"Prefab TMP={prefabTmpCount} 个 / Runtime 脚本待注入={missingScriptLines} 行 " +
                $"(详见 {MissingReportPath})");
            Debug.Log("[UnifyFontsToAlibaba] 下一步(可选): Tools → DrscfZ → Fix Runtime AddComponent TMP Default Font");
        }

        // =========================================================================
        //  场景处理
        // =========================================================================
        private static int ProcessAllScenes(TMP_FontAsset targetFont)
        {
            int total = 0;
            var originalScenePath = EditorSceneManager.GetActiveScene().path;
            bool originalDirty = EditorSceneManager.GetActiveScene().isDirty;

            // 收集所有 .unity 场景(去重: build settings + Assets/Scenes)
            var sceneSet = new HashSet<string>();
            foreach (var bs in EditorBuildSettings.scenes)
            {
                if (!string.IsNullOrEmpty(bs.path)) sceneSet.Add(bs.path);
            }
            var guidList = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
            foreach (var guid in guidList)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path)) sceneSet.Add(path);
            }

            foreach (var scenePath in sceneSet)
            {
                if (ShouldIgnorePath(scenePath)) continue;
                if (!File.Exists(scenePath)) continue;

                Scene scene;
                try
                {
                    scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[UnifyFontsToAlibaba] 打开场景失败 {scenePath}: {ex.Message}");
                    continue;
                }

                int count = ReplaceFontsInScene(scene, targetFont);
                total += count;

                if (count > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log($"[UnifyFontsToAlibaba] 场景 {scenePath}: 替换 {count} 个 TMP 字体,已保存");
                }
            }

            // 恢复原场景
            if (!string.IsNullOrEmpty(originalScenePath) && File.Exists(originalScenePath))
            {
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            }

            return total;
        }

        private static int ReplaceFontsInScene(Scene scene, TMP_FontAsset targetFont)
        {
            int count = 0;
            var rootGOs = scene.GetRootGameObjects();
            foreach (var root in rootGOs)
            {
                count += ReplaceFontsOnHierarchy(root.transform, targetFont);
            }
            return count;
        }

        // =========================================================================
        //  Prefab 处理
        // =========================================================================
        private static int ProcessAllPrefabs(TMP_FontAsset targetFont)
        {
            int total = 0;
            var guids = AssetDatabase.FindAssets("t:Prefab");

            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (ShouldIgnorePath(path)) continue;

                // 进度条便于观察（但不阻塞）
                if (i % 10 == 0)
                {
                    EditorUtility.DisplayProgressBar(
                        "Unify Fonts → AlibabaPuHuiTi",
                        $"[{i + 1}/{guids.Length}] {path}",
                        (float)i / guids.Length);
                }

                GameObject root = null;
                try
                {
                    root = PrefabUtility.LoadPrefabContents(path);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[UnifyFontsToAlibaba] Prefab 打开失败 {path}: {ex.Message}");
                    continue;
                }
                if (root == null) continue;

                int count = ReplaceFontsOnHierarchy(root.transform, targetFont);
                if (count > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    total += count;
                }
                PrefabUtility.UnloadPrefabContents(root);
            }

            EditorUtility.ClearProgressBar();
            return total;
        }

        // =========================================================================
        //  通用: 在一棵 hierarchy 上替换所有 TMP 字体
        // =========================================================================
        private static int ReplaceFontsOnHierarchy(Transform root, TMP_FontAsset targetFont)
        {
            int count = 0;

            // TMP_Text 覆盖 TextMeshProUGUI + TextMeshPro
            var tmps = root.GetComponentsInChildren<TMP_Text>(includeInactive: true);
            foreach (var tmp in tmps)
            {
                if (tmp == null) continue;
                if (tmp.font == targetFont) continue;

                // 用 SerializedObject 写 m_fontAsset,确保 override 标记干净 + 不影响颜色
                var so = new SerializedObject(tmp);
                var fontProp = so.FindProperty("m_fontAsset");
                if (fontProp != null)
                {
                    fontProp.objectReferenceValue = targetFont;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(tmp);
                    count++;
                }
                else
                {
                    // 回退: 属性名变了(TMP 版本差异),直接赋值
                    tmp.font = targetFont;
                    EditorUtility.SetDirty(tmp);
                    count++;
                }
            }

            return count;
        }

        private static bool ShouldIgnorePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return true;
            foreach (var prefix in IgnorePathPrefixes)
            {
                if (path.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        // =========================================================================
        //  Runtime C# 扫描: 找出 AddComponent<TextMeshProUGUI/TextMeshPro/TMP_Text>() 后未赋字体的行
        //  只生成报告,不改 .cs 文件(改动交给 FixRuntimeFontDefaults)
        // =========================================================================
        private static int ReportRuntimeMissingFonts()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Runtime AddComponent<TMP> 缺失字体赋值清单");
            sb.AppendLine("# 生成时间: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("# 格式: <绝对路径>|<行号>|<匹配代码>");
            sb.AppendLine();

            // 匹配: var x = something.AddComponent<TextMeshProUGUI>();
            //      var x = go.AddComponent<TMP_Text>();
            //      var x = go.AddComponent<TextMeshPro>();
            var addComponentRegex = new Regex(
                @"(?<var>\w+)\s*=\s*[\w\.\(\)\[\]]+?\.AddComponent<\s*(TextMeshProUGUI|TMP_Text|TextMeshPro)\s*>\s*\(\s*\)",
                RegexOptions.Compiled);

            int missingLines = 0;
            var csFiles = Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories);

            foreach (var file in csFiles)
            {
                string normalized = file.Replace('\\', '/');
                // 跳过编辑器脚本 + Editor 文件夹
                if (normalized.Contains("/Editor/")) continue;

                string[] lines;
                try { lines = File.ReadAllLines(file); }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[UnifyFontsToAlibaba] 读文件失败 {file}: {ex.Message}");
                    continue;
                }

                for (int i = 0; i < lines.Length; i++)
                {
                    var m = addComponentRegex.Match(lines[i]);
                    if (!m.Success) continue;

                    string varName = m.Groups["var"].Value;

                    // 检查接下来 25 行内是否有 <varName>.font = 或 <varName>.fontAsset =
                    // 或已经有 AUTO-INJECT 标记
                    bool hasFontAssign = false;
                    bool hasAutoInject = false;
                    int lookahead = System.Math.Min(i + 25, lines.Length);
                    for (int j = i; j < lookahead; j++)
                    {
                        if (lines[j].Contains("AUTO-INJECT: 统一 Alibaba 字体"))
                        {
                            hasAutoInject = true;
                            break;
                        }
                        if (Regex.IsMatch(
                                lines[j],
                                @"\b" + Regex.Escape(varName) + @"\.(font|fontAsset)\s*="))
                        {
                            hasFontAssign = true;
                            break;
                        }
                    }

                    if (!hasFontAssign && !hasAutoInject)
                    {
                        missingLines++;
                        string absPath = Path.GetFullPath(file).Replace('\\', '/');
                        sb.AppendLine($"{absPath}|{i + 1}|{lines[i].Trim()}");
                    }
                }
            }

            // 写入 Temp/
            try
            {
                Directory.CreateDirectory("Temp");
                File.WriteAllText(MissingReportPath, sb.ToString());
                Debug.Log($"[UnifyFontsToAlibaba] Runtime 扫描清单已写入 {MissingReportPath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[UnifyFontsToAlibaba] 写报告失败: {ex.Message}");
            }

            return missingLines;
        }
    }
}
