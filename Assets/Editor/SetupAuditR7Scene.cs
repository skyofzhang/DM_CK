using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

namespace DrscfZ.EditorTools
{
    /// <summary>
    /// audit-r7 场景终审批修：
    ///   1) 批量替换 MainScene 16 处 LiberationSans GUID → AlibabaPuHuiTi-3-85-Bold（运行时 UI 文本统一）
    ///   2) Legacy 节点 SetActive(false)：OrangeOverlay / OrangeIcon / CapybaraSpawner / VFXSpawner / FootDustManager / PlayerListUI
    ///      （r5 勘误：5 个节点部分在场景但未 inactive / 未删除）
    ///
    /// 执行后必须 manage_scene save 持久化。
    /// </summary>
    public static class SetupAuditR7Scene
    {
        [MenuItem("Tools/DrscfZ/Setup Audit-r7 Scene")]
        public static void Execute()
        {
            int fontChanged = ReplaceLiberationSansFonts();
            int legacyDeactivated = DeactivateLegacyNodes();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[SetupAuditR7Scene] 完成。字体替换 {fontChanged} 处 | Legacy 节点失活 {legacyDeactivated} 个。请 manage_scene save。");
        }

        private static int ReplaceLiberationSansFonts()
        {
            var alibaba = Resources.Load<TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF");
            var fallbackCjk = Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
            if (alibaba == null)
            {
                Debug.LogError("[SetupAuditR7Scene] AlibabaPuHuiTi-3-85-Bold SDF 加载失败（Assets/Resources/Fonts/）；跳过字体替换");
                return 0;
            }

            int count = 0;
            var allText = Resources.FindObjectsOfTypeAll<TMP_Text>();
            foreach (var t in allText)
            {
                if (t == null) continue;
                // 仅处理场景内 TMP，跳过 Project 里的 Prefab 模板
                if (string.IsNullOrEmpty(t.gameObject.scene.name)) continue;
                var curFont = t.font;
                if (curFont == null) continue;
                string curName = curFont.name ?? string.Empty;
                if (curName.Contains("LiberationSans") || curName.Contains("Inter-Regular") || curName.Contains("Inter_Regular"))
                {
                    t.font = alibaba;
                    // 确保 fallback 链：Alibaba 主 + ChineseFont fallback
                    if (fallbackCjk != null && alibaba.fallbackFontAssetTable != null
                        && !alibaba.fallbackFontAssetTable.Contains(fallbackCjk))
                    {
                        alibaba.fallbackFontAssetTable.Add(fallbackCjk);
                    }
                    EditorUtility.SetDirty(t);
                    Debug.Log($"[SetupAuditR7Scene] 字体替换：{GetFullPath(t.transform)} — '{curName}' → AlibabaPuHuiTi（原文：'{Truncate(t.text, 24)}'）");
                    count++;
                }
            }
            return count;
        }

        private static int DeactivateLegacyNodes()
        {
            string[] legacyNames = new[]
            {
                "OrangeOverlay", "OrangeIcon",
                "CapybaraSpawner", "VFXSpawner", "FootDustManager",
                "PlayerListUI"
            };
            int count = 0;
            var all = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in all)
            {
                if (go == null) continue;
                if (string.IsNullOrEmpty(go.scene.name)) continue; // 跳过 Prefab
                foreach (var nm in legacyNames)
                {
                    if (go.name != nm) continue;
                    if (go.activeSelf)
                    {
                        go.SetActive(false);
                        EditorUtility.SetDirty(go);
                        Debug.Log($"[SetupAuditR7Scene] Legacy 节点失活：{GetFullPath(go.transform)}");
                        count++;
                    }
                    break;
                }
            }
            return count;
        }

        private static string GetFullPath(Transform t)
        {
            if (t == null) return "<null>";
            var stack = new Stack<string>();
            while (t != null) { stack.Push(t.name); t = t.parent; }
            return string.Join("/", stack);
        }

        private static string Truncate(string s, int n)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= n ? s : s.Substring(0, n) + "…";
        }
    }
}
