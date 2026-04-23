// Copyright 2026 极地生存法则
// 按 Temp/UnifyFontsMissing.txt 清单,向 Runtime C# 中 AddComponent<TextMeshProUGUI>() 等
// 之后没有字体赋值的位置,注入一段 Alibaba 字体加载样板代码。
//
// 菜单：Tools → DrscfZ → Fix Runtime AddComponent TMP Default Font
//
// 注入模板:
//   var label = go.AddComponent<TextMeshProUGUI>();
//   // AUTO-INJECT: 统一 Alibaba 字体
//   if (label.font == null) {
//       var f = Resources.Load<TMPro.TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF");
//       if (f == null) f = Resources.Load<TMPro.TMP_FontAsset>("Fonts/ChineseFont SDF");
//       if (f != null) label.font = f;
//   }
//
// 关键约束:
//   - 清单由 UnifyFontsToAlibaba 生成(Temp/UnifyFontsMissing.txt),本脚本只读不扫
//   - 如果行里已经有 "AUTO-INJECT: 统一 Alibaba 字体" 标记,跳过,避免重复注入
//   - 禁止 EditorUtility.DisplayDialog
//   - 先打印 dry-run 预览,再真正写盘(给用户看 Console 二次确认)
//   - 同一个文件里自下而上处理(行号从大到小),避免插入新行导致上方索引漂移

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace DrscfZ.EditorTools
{
    public static class FixRuntimeFontDefaults
    {
        private const string MissingReportPath = "Temp/UnifyFontsMissing.txt";
        private const string InjectMarker = "// AUTO-INJECT: 统一 Alibaba 字体";

        [MenuItem("Tools/DrscfZ/Fix Runtime AddComponent TMP Default Font")]
        public static void Execute()
        {
            if (!File.Exists(MissingReportPath))
            {
                Debug.LogError(
                    $"[FixRuntimeFontDefaults] 未找到清单 {MissingReportPath},请先执行 " +
                    "Tools → DrscfZ → Unify All Fonts → AlibabaPuHuiTi");
                return;
            }

            // 解析清单: <绝对路径>|<行号>|<匹配代码>
            var entries = new List<(string path, int line, string code)>();
            foreach (var raw in File.ReadAllLines(MissingReportPath))
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                if (raw.StartsWith("#")) continue;

                var parts = raw.Split(new[] { '|' }, 3);
                if (parts.Length < 3) continue;

                if (!int.TryParse(parts[1], out int line)) continue;
                entries.Add((parts[0].Trim(), line, parts[2].Trim()));
            }

            if (entries.Count == 0)
            {
                Debug.Log($"[FixRuntimeFontDefaults] 清单为空,无需注入: {MissingReportPath}");
                return;
            }

            // 按文件分组,文件内按行号倒序(自下而上插入)
            var groups = new Dictionary<string, List<(int line, string code)>>();
            foreach (var e in entries)
            {
                if (!File.Exists(e.path))
                {
                    Debug.LogWarning($"[FixRuntimeFontDefaults] 文件不存在,跳过: {e.path}");
                    continue;
                }
                if (!groups.TryGetValue(e.path, out var list))
                {
                    list = new List<(int, string)>();
                    groups[e.path] = list;
                }
                list.Add((e.line, e.code));
            }

            // ───── Dry-run 预览 ─────
            Debug.Log($"[FixRuntimeFontDefaults] Dry-run 预览: {groups.Count} 个文件, {entries.Count} 处注入点");
            foreach (var kv in groups)
            {
                Debug.Log($"[FixRuntimeFontDefaults]   {kv.Key}: {kv.Value.Count} 处");
            }

            // ───── 真正注入 ─────
            int filesChanged = 0;
            int linesInjected = 0;
            int skippedExisting = 0;

            // Regex: (?<var>\w+)\s*=\s*... .AddComponent<TMP_Text|TextMeshProUGUI|TextMeshPro>()
            var addComponentRegex = new Regex(
                @"(?<indent>^\s*)(?<stmt>.*?(?<var>\w+)\s*=\s*[\w\.\(\)\[\]]+?\.AddComponent<\s*(?:TextMeshProUGUI|TMP_Text|TextMeshPro)\s*>\s*\(\s*\)\s*;?)",
                RegexOptions.Compiled);

            foreach (var kv in groups)
            {
                string file = kv.Key;
                var list = kv.Value;
                list.Sort((a, b) => b.line.CompareTo(a.line)); // 倒序

                string[] lines;
                try { lines = File.ReadAllLines(file); }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[FixRuntimeFontDefaults] 读文件失败 {file}: {ex.Message}");
                    continue;
                }

                var working = new List<string>(lines);
                bool fileChanged = false;

                foreach (var (lineNo, codeSnippet) in list)
                {
                    int idx = lineNo - 1;
                    if (idx < 0 || idx >= working.Count) continue;

                    string originalLine = working[idx];

                    // 检查是否已经被注入过(兜底,避免重复写)
                    bool alreadyInjected = false;
                    for (int j = idx; j < Math.Min(idx + 8, working.Count); j++)
                    {
                        if (working[j].Contains(InjectMarker))
                        {
                            alreadyInjected = true;
                            break;
                        }
                    }
                    if (alreadyInjected)
                    {
                        skippedExisting++;
                        continue;
                    }

                    var m = addComponentRegex.Match(originalLine);
                    if (!m.Success)
                    {
                        Debug.LogWarning(
                            $"[FixRuntimeFontDefaults] 行无法匹配,跳过: {file}:{lineNo}\n  {originalLine}");
                        continue;
                    }

                    string indent = m.Groups["indent"].Value;
                    string varName = m.Groups["var"].Value;

                    // 构造注入片段(注意缩进继承 AddComponent 那行)
                    var snippet = new List<string>
                    {
                        indent + InjectMarker,
                        indent + $"if ({varName}.font == null) {{",
                        indent + "    var __f = Resources.Load<TMPro.TMP_FontAsset>(\"Fonts/AlibabaPuHuiTi-3-85-Bold SDF\");",
                        indent + "    if (__f == null) __f = Resources.Load<TMPro.TMP_FontAsset>(\"Fonts/ChineseFont SDF\");",
                        indent + $"    if (__f != null) {varName}.font = __f;",
                        indent + "}",
                    };

                    // 插入到 AddComponent 行之后
                    working.InsertRange(idx + 1, snippet);
                    linesInjected++;
                    fileChanged = true;
                }

                if (fileChanged)
                {
                    try
                    {
                        File.WriteAllLines(file, working);
                        filesChanged++;
                        Debug.Log($"[FixRuntimeFontDefaults] 已注入: {file}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[FixRuntimeFontDefaults] 写文件失败 {file}: {ex.Message}");
                    }
                }
            }

            AssetDatabase.Refresh();

            Debug.Log(
                $"[FixRuntimeFontDefaults] ✅ 完成: 修改 {filesChanged} 个文件, " +
                $"注入 {linesInjected} 处, 跳过已注入 {skippedExisting} 处。请关注 Console 是否有 CS 编译错误。");
        }
    }
}
