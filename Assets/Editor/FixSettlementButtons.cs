using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class FixSettlementButtons : Editor
{
    public static void Execute()
    {
        var all = Resources.FindObjectsOfTypeAll<Transform>();
        Transform panel = null;
        foreach (var t in all)
        {
            if (t.name == "SurvivalSettlementPanel" && t.GetComponent<RectTransform>() != null
                && (t.gameObject.scene.isLoaded || t.gameObject.scene.name != null))
            {
                panel = t;
                break;
            }
        }
        if (panel == null) { Debug.LogError("找不到面板"); return; }

        // 修复按钮 Label 拉满
        FixButtonLabel(panel.Find("BtnViewRanking"), "查看英雄榜");
        FixButtonLabel(panel.Find("RestartButton"), "返回大厅");

        // 调大按钮高度（6%→8%）
        var btnViewRect = panel.Find("BtnViewRanking")?.GetComponent<RectTransform>();
        if (btnViewRect != null)
        {
            btnViewRect.anchorMin = new Vector2(0.08f, 0.03f);
            btnViewRect.anchorMax = new Vector2(0.48f, 0.09f);
            btnViewRect.offsetMin = Vector2.zero;
            btnViewRect.offsetMax = Vector2.zero;
        }

        var btnRestartRect = panel.Find("RestartButton")?.GetComponent<RectTransform>();
        if (btnRestartRect != null)
        {
            btnRestartRect.anchorMin = new Vector2(0.52f, 0.03f);
            btnRestartRect.anchorMax = new Vector2(0.92f, 0.09f);
            btnRestartRect.offsetMin = Vector2.zero;
            btnRestartRect.offsetMax = Vector2.zero;
        }

        EditorUtility.SetDirty(panel.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[FixSettlementButtons] 按钮已修复");
    }

    static void FixButtonLabel(Transform btn, string text)
    {
        if (btn == null) return;

        var label = btn.Find("Label");
        if (label == null) return;

        // Label 拉满按钮
        var rt = label.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(8, 4);
        rt.offsetMax = new Vector2(-8, -4);

        var tmp = label.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = text;
            tmp.fontSize = 36;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 20;
            tmp.fontSizeMax = 36;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Truncate;
        }
    }
}
