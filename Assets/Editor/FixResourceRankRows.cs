using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using DrscfZ.UI;

/// <summary>
/// 为 ResourceRankPanel 的 Col_Food/Coal/Ore 各创建 Row_0~2 文字行，
/// 并将它们绑定到 ResourceRankUI 的 _foodRows/_coalRows/_oreRows。
/// Tools → DrscfZ → Fix ResourceRank Rows
/// </summary>
public static class FixResourceRankRows
{
    [MenuItem("Tools/DrscfZ/Fix ResourceRank Rows")]
    public static void Execute()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("[FixRankRows] 找不到 Canvas"); return; }

        // 找 ResourceRankUI 组件
        var rankUI = canvas.GetComponentInChildren<ResourceRankUI>(true);
        if (rankUI == null) { Debug.LogError("[FixRankRows] 找不到 ResourceRankUI 组件"); return; }

        // 找三列
        var panel = canvas.transform.Find("GameUIPanel/ResourceRankPanel");
        if (panel == null) { Debug.LogError("[FixRankRows] 找不到 ResourceRankPanel"); return; }

        var colFood = panel.Find("Col_Food");
        var colCoal = panel.Find("Col_Coal");
        var colOre  = panel.Find("Col_Ore");
        if (colFood == null || colCoal == null || colOre == null)
        {
            Debug.LogError("[FixRankRows] 找不到 Col_Food/Coal/Ore");
            return;
        }

        // 字体
        var font    = Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
        var fontMat = Resources.Load<Material>("Fonts/ChineseFont SDF - Outline");

        // 创建/复用行，收集 TMP 引用
        var foodRows = GetOrCreateRows(colFood, font, fontMat, new Color(0.4f, 1f, 0.5f));
        var coalRows = GetOrCreateRows(colCoal, font, fontMat, new Color(1f, 0.85f, 0.4f));
        var oreRows  = GetOrCreateRows(colOre,  font, fontMat, new Color(0.7f, 0.9f, 1f));

        // 通过 SerializedObject 写入私有数组字段
        var so = new SerializedObject(rankUI);

        WriteRowArray(so, "_foodRows", foodRows);
        WriteRowArray(so, "_coalRows", coalRows);
        WriteRowArray(so, "_oreRows",  oreRows);

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(rankUI);

        EditorSceneManager.MarkSceneDirty(rankUI.gameObject.scene);
        EditorSceneManager.SaveScene(rankUI.gameObject.scene);

        Debug.Log("[FixRankRows] 完成：_foodRows/_coalRows/_oreRows 已绑定，场景已保存");
    }

    static TextMeshProUGUI[] GetOrCreateRows(Transform col, TMP_FontAsset font, Material fontMat, Color color)
    {
        var result = new TextMeshProUGUI[3];
        for (int i = 0; i < 3; i++)
        {
            string rowName = $"Row_{i}";
            var existing = col.Find(rowName);
            Transform rowT;

            if (existing != null)
            {
                rowT = existing;
            }
            else
            {
                var go = new GameObject(rowName);
                go.AddComponent<RectTransform>();
                go.transform.SetParent(col, false);
                rowT = go.transform;
            }

            // 确保有 TMP 组件
            var tmp = rowT.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = rowT.gameObject.AddComponent<TextMeshProUGUI>();

            tmp.text      = $"{i + 1}. —";
            tmp.fontSize  = 16f;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.color     = color;
            if (font    != null) tmp.font               = font;
            if (fontMat != null) tmp.fontSharedMaterial = fontMat;

            result[i] = tmp;
        }
        return result;
    }

    static void WriteRowArray(SerializedObject so, string fieldName, TextMeshProUGUI[] rows)
    {
        var prop = so.FindProperty(fieldName);
        if (prop == null) { Debug.LogWarning($"[FixRankRows] 找不到字段 {fieldName}"); return; }
        prop.arraySize = rows.Length;
        for (int i = 0; i < rows.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = rows[i];
    }
}
