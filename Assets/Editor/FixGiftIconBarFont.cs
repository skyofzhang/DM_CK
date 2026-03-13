using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// 修复 GiftIconBar 中各等级 Icon TMP 的字体问题。
/// GiftIconBar 里的 Icon 对象用 LiberationSans SDF，不支持 emoji（🍖🪨💎🔥🏰等）。
/// 改为 ChineseFont SDF 后，emoji 可正常显示。
/// </summary>
public class FixGiftIconBarFont
{
    [MenuItem("Tools/Phase2/Fix GiftIconBar Icon Fonts")]
    public static void Execute()
    {
        var chineseFontPath = "Assets/Resources/Fonts/ChineseFont SDF.asset";
        var chineseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(chineseFontPath);

        if (chineseFont == null)
        {
            Debug.LogError($"[FixGiftIconBarFont] ChineseFont SDF not found at: {chineseFontPath}");
            return;
        }

        int fixedCount = 0;

        // 找所有名为 "Icon" 的 TMP Text（不区分大小写），限制在 Canvas 下
        var mainCanvas = GameObject.Find("Canvas");
        if (mainCanvas == null)
        {
            Debug.LogError("[FixGiftIconBarFont] Canvas not found");
            return;
        }

        var allTexts = mainCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var tmp in allTexts)
        {
            // 只修改名为 "Icon" 或在 GiftIconBar 下的 TMP
            bool isIconObject = tmp.gameObject.name == "Icon";
            bool isInGiftIconBar = IsInPath(tmp.transform, "GiftIconBar");
            bool isTitleText = tmp.gameObject.name == "TitleText" && IsInPath(tmp.transform, "BarragePanel");

            if ((isIconObject && isInGiftIconBar) || isTitleText)
            {
                if (tmp.font != chineseFont)
                {
                    tmp.font = chineseFont;
                    EditorUtility.SetDirty(tmp);
                    fixedCount++;
                }
            }
        }

        // 同样修复 BarragePanel 下 MsgRow 里含 emoji 的 TMP
        foreach (var tmp in allTexts)
        {
            if (IsInPath(tmp.transform, "BarrageContent") && tmp.font != chineseFont)
            {
                tmp.font = chineseFont;
                EditorUtility.SetDirty(tmp);
                fixedCount++;
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[FixGiftIconBarFont] ✅ Fixed {fixedCount} TMP fonts → ChineseFont SDF. Scene saved.");
    }

    private static bool IsInPath(Transform t, string partialPath)
    {
        while (t != null)
        {
            if (t.name == partialPath) return true;
            t = t.parent;
        }
        return false;
    }
}
