using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// 修复 ResourceRankPanel：
/// 1. 重新绑定 GameUIPanel 上 ResourceRankUI 的 _foodRows/_coalRows/_oreRows 数组
/// 2. 给面板加半透明深色背景，确保白色文字可见
/// </summary>
public static class FixResourceRankPanel
{
    [MenuItem("Tools/DrscfZ/Fix Resource Rank Panel")]
    public static void Execute()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("Canvas not found"); return; }

        var gameUIPanel = canvas.transform.Find("GameUIPanel");
        if (gameUIPanel == null) { Debug.LogError("GameUIPanel not found"); return; }

        // 获取 ResourceRankUI 组件（现在在 GameUIPanel 上）
        var rankUI = gameUIPanel.GetComponent<DrscfZ.UI.ResourceRankUI>();
        if (rankUI == null) { Debug.LogError("ResourceRankUI not found on GameUIPanel"); return; }

        var rankPanel = gameUIPanel.Find("ResourceRankPanel");
        if (rankPanel == null) { Debug.LogError("ResourceRankPanel not found"); return; }

        // ── 1. 绑定 Row 数组 ──
        var so = new SerializedObject(rankUI);

        BindRows(so, "_foodRows", rankPanel, "Col_Food");
        BindRows(so, "_coalRows", rankPanel, "Col_Coal");
        BindRows(so, "_oreRows",  rankPanel, "Col_Ore");

        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("Row arrays bound successfully");

        // ── 2. 修复面板背景 ──
        var img = rankPanel.GetComponent<Image>();
        if (img != null)
        {
            img.color = new Color(0.05f, 0.08f, 0.15f, 0.75f); // 半透明深蓝
            img.sprite = null; // 纯色背景即可
            EditorUtility.SetDirty(img);
            Debug.Log("Panel background set to semi-transparent dark blue");
        }

        EditorUtility.SetDirty(rankUI);
        EditorUtility.SetDirty(rankPanel.gameObject);

        // 保存场景
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("FixResourceRankPanel: done, scene saved");
    }

    private static void BindRows(SerializedObject so, string propName, Transform panel, string colName)
    {
        var col = panel.Find(colName);
        if (col == null) { Debug.LogWarning("Column not found: " + colName); return; }

        var prop = so.FindProperty(propName);
        prop.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            var row = col.Find("Row" + (i + 1));
            if (row == null) { Debug.LogWarning(colName + "/Row" + (i+1) + " not found"); continue; }
            var tmp = row.GetComponent<TextMeshProUGUI>();
            prop.GetArrayElementAtIndex(i).objectReferenceValue = tmp;
            Debug.Log("Bound " + propName + "[" + i + "] -> " + colName + "/Row" + (i+1));
        }
    }
}
