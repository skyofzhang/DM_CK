using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

namespace DrscfZ.Editor
{
    /// <summary>
    /// TMP 字体批量替换工具
    ///
    /// 菜单：Tools → DrscfZ → Fix TMP Fonts (→ ChineseFont SDF)
    ///
    /// 功能：
    ///   扫描当前场景中所有 TMP_Text 组件，将使用 LiberationSans SDF 的组件
    ///   替换为 ChineseFont SDF，解决大量字体缺失 Warning 导致的 Coplay 卡顿。
    ///
    /// 注意：无 DisplayDialog，结果只写 Console。
    /// </summary>
    public static class FixTMPFonts
    {
        private const string ChineseFontPath = "Assets/Resources/Fonts/ChineseFont SDF.asset";
        private const string LiberationSansPath =
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        [MenuItem("Tools/DrscfZ/Fix TMP Fonts (→ ChineseFont SDF)")]
        public static void Execute()
        {
            // 加载目标字体
            var chineseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ChineseFontPath);
            if (chineseFont == null)
            {
                Debug.LogError($"[FixTMPFonts] 未找到 ChineseFont SDF，路径：{ChineseFontPath}");
                return;
            }

            // 加载 LiberationSans（用于对比，找到需要替换的组件）
            var liberationFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LiberationSansPath);
            // liberationFont 可能为 null（说明资产不在项目里），此时只替换 null 字体

            int replaced = 0;
            int nullReplaced = 0;

            // 扫描场景中所有 TMP_Text（含 TextMeshProUGUI）
            var allTMP = Resources.FindObjectsOfTypeAll<TMP_Text>();
            foreach (var tmp in allTMP)
            {
                // 跳过 Prefab 资产本身（只处理场景实例）
                if (!tmp.gameObject.scene.IsValid()) continue;

                bool needReplace = false;

                if (tmp.font == null)
                {
                    // 无字体 → 赋予中文字体
                    needReplace = true;
                    nullReplaced++;
                }
                else if (liberationFont != null && tmp.font == liberationFont)
                {
                    // 明确使用 LiberationSans → 替换
                    needReplace = true;
                }

                if (needReplace)
                {
                    Undo.RecordObject(tmp, "Fix TMP Font");
                    tmp.font = chineseFont;
                    EditorUtility.SetDirty(tmp);
                    replaced++;
                    Debug.Log($"[FixTMPFonts] 替换：{tmp.gameObject.name} / {tmp.GetType().Name}");
                }
            }

            // 保存场景（Rule #8）
            if (replaced > 0)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log($"[FixTMPFonts] ✅ 共替换 {replaced} 个 TMP 组件" +
                      $"（其中 null 字体 {nullReplaced} 个）。场景已保存。");
        }
    }
}
