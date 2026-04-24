using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;

public class AddRankingHeader
{
    [MenuItem("Tools/DrscfZ/Add Ranking Header Row")]
    public static void Execute()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("Canvas not found"); return; }

        Transform panel = null;
        foreach (Transform child in canvas.transform)
        {
            if (child.name == "SurvivalRankingPanel") { panel = child; break; }
        }
        if (panel == null) { Debug.LogError("SurvivalRankingPanel not found"); return; }

        // 删除旧的 HeaderRow
        var oldHeader = panel.Find("HeaderRow");
        if (oldHeader != null) Object.DestroyImmediate(oldHeader.gameObject);

        var font = Resources.Load<TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF") ?? Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");

        // 创建 HeaderRow — 放在 RowContainer 上方
        // RowContainer: anchorMin=(0,0.1) anchorMax=(1,0.85)
        // HeaderRow 放在 anchorMin=(0,0.85) anchorMax=(1,0.89) 这个位置
        var headerGo = new GameObject("HeaderRow");
        headerGo.transform.SetParent(panel, false);
        var rt = headerGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.02f, 0.85f);
        rt.anchorMax = new Vector2(0.98f, 0.90f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // 不用 HLG，用固定锚点精确控制三列位置
        // 名次: 左侧 5%~15%
        CreateHeaderCell(headerGo.transform, "H_Rank",  "名次",
            new Vector2(0.03f, 0f), new Vector2(0.15f, 1f), font, TextAlignmentOptions.Center);
        // 玩家: 15%~55%
        CreateHeaderCell(headerGo.transform, "H_Name",  "玩家",
            new Vector2(0.15f, 0f), new Vector2(0.55f, 1f), font, TextAlignmentOptions.Left);
        // 贡献分: 55%~95%（对齐主播排行榜按钮下方）
        CreateHeaderCell(headerGo.transform, "H_Score", "贡献分",
            new Vector2(0.55f, 0f), new Vector2(0.97f, 1f), font, TextAlignmentOptions.Right);

        // 保存
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[AddRankingHeader] 表头行已创建并保存");
    }

    static void CreateHeaderCell(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, TMP_FontAsset font, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 22;
        tmp.alignment = align;
        tmp.fontStyle = FontStyles.Bold;
        if (font != null) tmp.font = font;

        // 用 SerializedObject 设置颜色
        var so = new SerializedObject(tmp);
        var col = new Color(0.7f, 0.8f, 0.9f, 0.8f); // 淡蓝灰色
        var fontColorProp = so.FindProperty("m_fontColor");
        if (fontColorProp != null) fontColorProp.colorValue = col;
        var fontColor32Prop = so.FindProperty("m_fontColor32");
        if (fontColor32Prop != null) fontColor32Prop.colorValue = col;
        so.ApplyModifiedProperties();
    }
}
