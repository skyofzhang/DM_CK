using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace DrscfZ.Editor
{
    /// <summary>
    /// Tools → DrscfZ → Clean Legacy Orange Artifacts
    ///
    /// 扫描旧橙子/角力（Pushing）遗留 GameObject 和 Prefab，输出清单（不删除）。
    /// 目的：给 PM / 美术团队一份清理参考清单，避免冒险自动删除导致依赖破坏。
    ///
    /// 检查范围：
    ///   1. 场景中使用 Orange* / PushingController 等旧脚本的 GameObject
    ///   2. 已知旧 Prefab：
    ///        Assets/Prefabs/Scene/Orange.prefab
    ///        Assets/Prefabs/Units/OrangeUnit.prefab
    ///        Assets/Prefabs/Units/Pushing.prefab
    ///        Assets/Prefabs/Units/201_SheepUnit.prefab
    ///        Assets/Prefabs/Units/BigSheepUnit.prefab
    ///        Assets/Prefabs/Units/KpblUnit.prefab
    ///   3. 场景中命名包含 Orange / Pushing / Sheep 的 GameObject
    ///
    /// 输出：
    ///   - Debug.Log 打印摘要
    ///   - Temp/LegacyOrangeReport.txt 完整报告（含场景路径）
    ///
    /// 严格禁止：
    ///   - 不删除任何资源
    ///   - 不修改 tag（避免破坏现有 tag 体系；改为写入报告文件）
    /// </summary>
    public static class CleanLegacyOrangeArtifacts
    {
        private static readonly string[] LegacyScriptNames = new[]
        {
            "OrangeController",
            "OrangeFollowCamera",
            "OrangeDustTrail",
            "OrangeSpeedHUD",
        };

        private static readonly string[] LegacyPrefabPaths = new[]
        {
            "Assets/Prefabs/Scene/Orange.prefab",
            "Assets/Prefabs/Units/OrangeUnit.prefab",
            "Assets/Prefabs/Units/Pushing.prefab",
            "Assets/Prefabs/Units/201_SheepUnit.prefab",
            "Assets/Prefabs/Units/BigSheepUnit.prefab",
            "Assets/Prefabs/Units/KpblUnit.prefab",
        };

        private static readonly string[] LegacyNameKeywords = new[]
        {
            "Orange", "Pushing", "SheepUnit",
        };

        [MenuItem("Tools/DrscfZ/Clean Legacy Orange Artifacts")]
        public static void Execute()
        {
            var report = new StringBuilder();
            report.AppendLine("# 极地生存法则 - 旧橙子/角力遗留资产清单");
            report.AppendLine($"生成时间：{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine();

            // ---- 1. 扫描场景中使用旧脚本的 GameObject ----
            report.AppendLine("## 1. 场景中使用旧脚本的 GameObject");
            int scriptHitCount = 0;
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (!go.scene.isLoaded) continue;
                if (IsPrefabAsset(go)) continue;

                foreach (var comp in go.GetComponents<MonoBehaviour>())
                {
                    if (comp == null) continue;
                    string typeName = comp.GetType().Name;
                    foreach (var bad in LegacyScriptNames)
                    {
                        if (typeName == bad)
                        {
                            string path = GetScenePath(go.transform);
                            report.AppendLine($"  - [{typeName}] {path}");
                            scriptHitCount++;
                        }
                    }
                }
            }
            if (scriptHitCount == 0) report.AppendLine("  （无命中）");
            report.AppendLine();

            // ---- 2. 扫描命名命中 ----
            report.AppendLine("## 2. 场景中命名包含 Orange/Pushing/SheepUnit 的 GameObject");
            int nameHitCount = 0;
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (!go.scene.isLoaded) continue;
                if (IsPrefabAsset(go)) continue;

                foreach (var kw in LegacyNameKeywords)
                {
                    if (go.name.Contains(kw))
                    {
                        string path = GetScenePath(go.transform);
                        report.AppendLine($"  - [{go.name}] {path}");
                        nameHitCount++;
                        break;
                    }
                }
            }
            if (nameHitCount == 0) report.AppendLine("  （无命中）");
            report.AppendLine();

            // ---- 3. 旧 Prefab 存在清单 ----
            report.AppendLine("## 3. 旧 Prefab 存在清单（不自动删除，人工确认）");
            int prefabHitCount = 0;
            foreach (var p in LegacyPrefabPaths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (asset != null)
                {
                    report.AppendLine($"  - {p}  (存在，建议人工评估)");
                    prefabHitCount++;
                }
            }
            if (prefabHitCount == 0) report.AppendLine("  （无命中）");
            report.AppendLine();

            // ---- 4. 旧脚本文件清单 ----
            report.AppendLine("## 4. 旧脚本文件清单（Assets/Scripts 下）");
            int scriptFileHitCount = 0;
            var scriptGuids = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/Scripts" });
            foreach (var guid in scriptGuids)
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(scriptPath)) continue;
                string fileName = Path.GetFileNameWithoutExtension(scriptPath);
                foreach (var bad in LegacyScriptNames)
                {
                    if (fileName == bad)
                    {
                        report.AppendLine($"  - {scriptPath}");
                        scriptFileHitCount++;
                        break;
                    }
                }
            }
            if (scriptFileHitCount == 0) report.AppendLine("  （无命中）");
            report.AppendLine();

            // ---- 5. 清单尾 ----
            report.AppendLine("## 总计");
            report.AppendLine($"  脚本组件命中: {scriptHitCount}");
            report.AppendLine($"  命名命中:     {nameHitCount}");
            report.AppendLine($"  旧 Prefab:    {prefabHitCount}");
            report.AppendLine($"  旧脚本文件:   {scriptFileHitCount}");
            report.AppendLine();
            report.AppendLine("## 建议处理");
            report.AppendLine("  - 脚本组件命中 → 在场景中手动移除（Inspector Remove Component）");
            report.AppendLine("  - 命名命中      → 确认是否冗余 GameObject，可 SetActive(false) 观察或删除");
            report.AppendLine("  - 旧 Prefab     → 评估依赖后删除（建议先用 Unity Dependency Finder 查引用）");
            report.AppendLine("  - 旧脚本文件    → 若场景无引用可删除（否则先删场景引用再删脚本）");

            // ---- 6. 写入 Temp ----
            string outPath = Path.Combine(Application.dataPath, "..", "Temp", "LegacyOrangeReport.txt");
            outPath = Path.GetFullPath(outPath);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outPath));
                File.WriteAllText(outPath, report.ToString());
                Debug.Log($"[CleanLegacyOrangeArtifacts] 报告已写入: {outPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CleanLegacyOrangeArtifacts] 报告写入失败: {e.Message}");
            }

            Debug.Log($"[CleanLegacyOrangeArtifacts] 扫描完成 — 脚本组件 {scriptHitCount}  命名 {nameHitCount}  Prefab {prefabHitCount}  脚本文件 {scriptFileHitCount}");
        }

        // ==================== 辅助方法 ====================

        private static string GetScenePath(Transform t)
        {
            if (t == null) return "<null>";
            var stack = new List<string>();
            var cur = t;
            while (cur != null)
            {
                stack.Add(cur.name);
                cur = cur.parent;
            }
            stack.Reverse();
            return string.Join("/", stack);
        }

        private static bool IsPrefabAsset(GameObject go)
        {
            return go != null && (go.hideFlags & HideFlags.HideAndDontSave) != 0
                   && !go.scene.IsValid();
        }
    }
}
