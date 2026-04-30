using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

namespace DrscfZ.Editor
{
    /// <summary>
    /// audit-r45 后处理：扫描 / 修复所有 TMP_Text 组件持有的 stale font reference。
    ///
    /// 症状：Console 47+ MissingReferenceException「m_AtlasTextures of TMP_FontAsset doesn't exist anymore」
    /// 根因：场景里某些 TMP_Text 组件的 m_fontAsset 字段引用的 TMP_FontAsset 实例已被销毁（Unity fake-null），
    ///       但 .NET 端引用尚在，TMP 内部 ParseInputText → GetFallbackMaterial → fontAsset.atlasTextures 触发异常。
    ///
    /// 用法（按顺序 menu）：
    ///   1. Tools → DrscfZ → TMP → 1. 诊断 Missing TMP Fonts          —— 列出所有问题 GO 路径
    ///   2. Tools → DrscfZ → TMP → 2. 一键修复（重指派为 ChineseFont SDF）—— 自动改为 Resources/Fonts/ChineseFont SDF
    /// </summary>
    public static class FixMissingTmpFonts
    {
        private const string PRIMARY_FONT_PATH  = "Assets/Resources/Fonts/ChineseFont SDF.asset";
        private const string FALLBACK_FONT_PATH = "Assets/Resources/Fonts/AlibabaPuHuiTi-3-85-Bold SDF.asset";

        [MenuItem("Tools/DrscfZ/TMP/1. 诊断 Missing TMP Fonts")]
        public static void Diagnose()
        {
            var (missingUgui, missingMesh) = ScanMissing();
            int total = missingUgui.Count + missingMesh.Count;

            Debug.Log($"[TMPDiag] === 诊断结果 ===");
            Debug.Log($"[TMPDiag] TextMeshProUGUI 缺失字体: {missingUgui.Count}");
            Debug.Log($"[TMPDiag] TextMeshPro      缺失字体: {missingMesh.Count}");
            Debug.Log($"[TMPDiag] 合计: {total}");

            for (int i = 0; i < missingUgui.Count; i++)
            {
                Debug.LogError($"[TMPDiag] [UGUI #{i + 1}] {GetGameObjectPath(missingUgui[i].gameObject)}", missingUgui[i].gameObject);
            }
            for (int i = 0; i < missingMesh.Count; i++)
            {
                Debug.LogError($"[TMPDiag] [Mesh #{i + 1}] {GetGameObjectPath(missingMesh[i].gameObject)}", missingMesh[i].gameObject);
            }

            if (total == 0)
            {
                Debug.Log("[TMPDiag] ✅ 全场景无 missing TMP font，47 errors 可能来自 prefab/disabled GO，请保证场景全展开后再扫");
            }
            else
            {
                Debug.LogWarning($"[TMPDiag] ⚠️ 检测到 {total} 个 GO 字体丢失，跑 menu 「2. 一键修复」自动重指派");
            }
        }

        [MenuItem("Tools/DrscfZ/TMP/2. 一键修复（重指派为 ChineseFont SDF）")]
        public static void FixAll()
        {
            var primary  = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PRIMARY_FONT_PATH);
            if (primary == null)
            {
                Debug.LogError($"[TMPFix] 主字体加载失败: {PRIMARY_FONT_PATH}");
                return;
            }

            var (missingUgui, missingMesh) = ScanMissing();
            int fixedCount = 0;

            foreach (var t in missingUgui)
            {
                Undo.RecordObject(t, "Fix Missing TMP Font (UGUI)");
                t.font = primary;
                EditorUtility.SetDirty(t);
                fixedCount++;
                Debug.Log($"[TMPFix] [UGUI] {GetGameObjectPath(t.gameObject)} → {primary.name}", t.gameObject);
            }
            foreach (var t in missingMesh)
            {
                Undo.RecordObject(t, "Fix Missing TMP Font (Mesh)");
                t.font = primary;
                EditorUtility.SetDirty(t);
                fixedCount++;
                Debug.Log($"[TMPFix] [Mesh] {GetGameObjectPath(t.gameObject)} → {primary.name}", t.gameObject);
            }

            if (fixedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Debug.Log($"[TMPFix] ✅ 已修复 {fixedCount} 个 TMP 组件，记得 Ctrl+S 保存场景（或跑 Tools → DrscfZ → Save Current Scene）");
            }
            else
            {
                Debug.Log("[TMPFix] 无 missing 字体可修复（已全部健康）");
            }
        }

        [MenuItem("Tools/DrscfZ/TMP/3. 仅清场景内 47 errors（不改字体，仅 Refresh）")]
        public static void ForceTmpRefresh()
        {
            // 强制让所有 TMP_Text 重新 ParseInputText（触发 SetArraySizes 重新解析 fontAsset 引用）
            int touched = 0;
            foreach (var t in Object.FindObjectsOfType<TextMeshProUGUI>(true))
            {
                if (t == null) continue;
                t.SetAllDirty();
                touched++;
            }
            foreach (var t in Object.FindObjectsOfType<TextMeshPro>(true))
            {
                if (t == null) continue;
                t.SetAllDirty();
                touched++;
            }
            Debug.Log($"[TMPRefresh] 已 SetAllDirty {touched} 个 TMP 组件");
        }

        // ────────────── 内部 ──────────────

        private static (List<TextMeshProUGUI>, List<TextMeshPro>) ScanMissing()
        {
            var missingUgui = new List<TextMeshProUGUI>();
            var missingMesh = new List<TextMeshPro>();

            foreach (var t in Object.FindObjectsOfType<TextMeshProUGUI>(true))
            {
                if (t == null) continue;
                // Unity fake-null 检查：font == null 包含已销毁实例
                if (IsFontMissing(t.font)) missingUgui.Add(t);
            }
            foreach (var t in Object.FindObjectsOfType<TextMeshPro>(true))
            {
                if (t == null) continue;
                if (IsFontMissing(t.font)) missingMesh.Add(t);
            }
            return (missingUgui, missingMesh);
        }

        private static bool IsFontMissing(TMP_FontAsset font)
        {
            // null（真 null + Unity fake-null 都包含）
            if (font == null) return true;
            // atlas 引用为空（fileID 引用了 Texture2D 但 m_AtlasTextures 列表内为空）
            try
            {
                var atlases = font.atlasTextures;
                if (atlases == null || atlases.Length == 0) return true;
                for (int i = 0; i < atlases.Length; i++)
                {
                    if (atlases[i] == null) return true;
                }
            }
            catch (MissingReferenceException) { return true; }
            return false;
        }

        private static string GetGameObjectPath(GameObject go)
        {
            if (go == null) return "<null>";
            var path = go.name;
            var t = go.transform.parent;
            while (t != null)
            {
                path = t.name + "/" + path;
                t = t.parent;
            }
            return path;
        }
    }
}
