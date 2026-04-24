using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;

public class SetupRankingTabs
{
    [MenuItem("Tools/DrscfZ/Setup Ranking Tabs")]
    public static void Execute()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("Canvas not found"); return; }

        var panel = canvas.transform.Find("SurvivalRankingPanel");
        if (panel == null) { Debug.LogError("SurvivalRankingPanel not found"); return; }

        var font = Resources.Load<TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF") ?? Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");

        // ── 删除旧的 tab 按钮（如果存在）──
        var oldTab1 = panel.Find("TabContribution");
        if (oldTab1 != null) Object.DestroyImmediate(oldTab1.gameObject);
        var oldTab2 = panel.Find("TabStreamer");
        if (oldTab2 != null) Object.DestroyImmediate(oldTab2.gameObject);
        var oldSubtitle = panel.Find("SubtitleText");
        if (oldSubtitle != null) Object.DestroyImmediate(oldSubtitle.gameObject);

        // Panel 尺寸: 940 x 1400, TitleText at y=360
        // Tab 按钮放在 title 下方 y=290

        // ── 贡献排行榜 Tab ──
        var tab1Go = CreateTabButton(panel, "TabContribution", "贡献排行榜", new Vector2(-140, 290), font);

        // ── 主播排行榜 Tab ──
        var tab2Go = CreateTabButton(panel, "TabStreamer", "主播排行榜", new Vector2(140, 290), font);

        // ── 副标题文字 ──
        var subtitleGo = new GameObject("SubtitleText");
        subtitleGo.transform.SetParent(panel, false);
        var subRT = subtitleGo.AddComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.5f, 0.5f);
        subRT.anchorMax = new Vector2(0.5f, 0.5f);
        subRT.sizeDelta = new Vector2(800, 40);
        subRT.anchoredPosition = new Vector2(0, 250);
        var subTMP = subtitleGo.AddComponent<TextMeshProUGUI>();
        subTMP.text = "每周一 00:00 重置";
        subTMP.fontSize = 22;
        subTMP.alignment = TextAlignmentOptions.Center;
        if (font != null) subTMP.font = font;
        // 颜色用 SerializedObject 设置
        SetTMPColor(subTMP, new Color(0.8f, 0.8f, 0.8f, 1f));

        // ── 绑定到 SurvivalRankingUI ──
        var rankingUI = canvas.GetComponent<DrscfZ.UI.SurvivalRankingUI>();
        if (rankingUI != null)
        {
            var so = new SerializedObject(rankingUI);

            // _panel
            var panelProp = so.FindProperty("_panel");
            if (panelProp != null) panelProp.objectReferenceValue = panel.gameObject;

            // _closeBtn
            var closeBtn = panel.Find("CloseBtn");
            var closeProp = so.FindProperty("_closeBtn");
            if (closeProp != null && closeBtn != null)
                closeProp.objectReferenceValue = closeBtn.GetComponent<Button>();

            // _titleText
            var titleText = panel.Find("TitleText");
            var titleProp = so.FindProperty("_titleText");
            if (titleProp != null && titleText != null)
                titleProp.objectReferenceValue = titleText.GetComponent<TMP_Text>();

            // _subtitleText
            var subtitleProp = so.FindProperty("_subtitleText");
            if (subtitleProp != null)
                subtitleProp.objectReferenceValue = subTMP;

            // _tabContribution
            var tabContribProp = so.FindProperty("_tabContribution");
            if (tabContribProp != null)
                tabContribProp.objectReferenceValue = tab1Go.GetComponent<Button>();

            // _tabStreamer
            var tabStreamerProp = so.FindProperty("_tabStreamer");
            if (tabStreamerProp != null)
                tabStreamerProp.objectReferenceValue = tab2Go.GetComponent<Button>();

            // _rowContainer
            var rowContainer = panel.Find("RowContainer");
            var rowProp = so.FindProperty("_rowContainer");
            if (rowProp != null && rowContainer != null)
                rowProp.objectReferenceValue = rowContainer;

            // _emptyHint
            var emptyHint = panel.Find("EmptyHint");
            var emptyProp = so.FindProperty("_emptyHint");
            if (emptyProp != null && emptyHint != null)
                emptyProp.objectReferenceValue = emptyHint.GetComponent<TMP_Text>();

            // _overlay — 查找 RankingOverlay
            var overlay = canvas.transform.Find("RankingOverlay");
            var overlayProp = so.FindProperty("_overlay");
            if (overlayProp != null && overlay != null)
                overlayProp.objectReferenceValue = overlay.gameObject;

            so.ApplyModifiedProperties();
            Debug.Log("[SetupRankingTabs] SurvivalRankingUI 字段绑定完成");
        }
        else
        {
            Debug.LogWarning("[SetupRankingTabs] Canvas 上未找到 SurvivalRankingUI 组件");
        }

        // ── 保存场景 ──
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SetupRankingTabs] 排行榜页签创建完成，场景已保存");
    }

    private static GameObject CreateTabButton(Transform parent, string name, string label, Vector2 pos, TMP_FontAsset font)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(240, 60);
        rt.anchoredPosition = pos;

        // 背景 Image（半透明深色底）
        var img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.25f, 0.8f);

        // Button
        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor      = new Color(0.15f, 0.15f, 0.25f, 0.8f);
        colors.highlightedColor = new Color(0.25f, 0.25f, 0.4f, 0.9f);
        colors.pressedColor     = new Color(0.1f, 0.1f, 0.2f, 1f);
        btn.colors = colors;

        // Label
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        var labelRT = labelGo.AddComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        if (font != null) tmp.font = font;
        SetTMPColor(tmp, new Color(1f, 0.85f, 0.4f, 1f));

        return go;
    }

    private static void SetTMPColor(TMP_Text tmp, Color color)
    {
        var so = new SerializedObject(tmp);
        var fontColorProp = so.FindProperty("m_fontColor");
        if (fontColorProp != null) fontColorProp.colorValue = color;
        var fontColor32Prop = so.FindProperty("m_fontColor32");
        if (fontColor32Prop != null)
            fontColor32Prop.colorValue = color;
        so.ApplyModifiedProperties();
    }
}
