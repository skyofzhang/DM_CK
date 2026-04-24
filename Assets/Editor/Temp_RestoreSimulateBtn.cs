using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 把 BtnSimulate 恢复到 BottomBar Row2b 可见位置
/// 位置：怪物按钮右边，y=-106, x=-194, 200x60
/// </summary>
public class Temp_RestoreSimulateBtn
{
    public static void Execute()
    {
        // 找 BottomBar
        var allT = Object.FindObjectsOfType<Transform>(true);
        Transform bottomBar = null;
        foreach (var t in allT)
            if (t.name == "BottomBar") { bottomBar = t; break; }

        if (bottomBar == null) { Debug.LogError("[RestoreSim] BottomBar not found"); return; }

        var btnTr = bottomBar.Find("BtnSimulate");
        if (btnTr == null) { Debug.LogError("[RestoreSim] BtnSimulate not found"); return; }

        // 还原 RectTransform
        var rt = btnTr.GetComponent<RectTransform>();
        Undo.RecordObject(rt, "Restore BtnSimulate");
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(200f, 60f);
        rt.anchoredPosition = new Vector2(-194f, -106f);
        EditorUtility.SetDirty(rt);

        // 还原 Image 颜色（橙色，同其他按钮风格）
        var img = btnTr.GetComponent<Image>();
        if (img != null)
        {
            Undo.RecordObject(img, "Restore BtnSimulate Color");
            img.color = new Color(0.85f, 0.50f, 0.10f, 1f); // 橙色
            EditorUtility.SetDirty(img);
        }

        // 确保字体 + 文字
        var tmp = btnTr.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
        {
            var chFont = Resources.Load<TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF") ?? Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
            Undo.RecordObject(tmp, "Restore BtnSimulate Text");
            if (chFont != null) tmp.font = chFont;
            tmp.text      = "模拟弹幕";
            tmp.fontSize  = 22;
            tmp.color     = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            EditorUtility.SetDirty(tmp);
        }

        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[RestoreSim] Done. BtnSimulate restored and scene saved.");
    }
}
