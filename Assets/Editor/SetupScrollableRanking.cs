using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;

/// <summary>
/// 将排行榜 RowContainer 改造为 ScrollRect 可滚动列表，预创建50行
/// </summary>
public class SetupScrollableRanking
{
    const int MAX_ROWS = 50;

    [MenuItem("Tools/DrscfZ/Setup Scrollable Ranking")]
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

        var font = Resources.Load<TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF") ?? Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
        var rowBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Rankings/ranking_row_top1_bg.png");

        // ── 1. 删除旧的 RowContainer 和 ScrollArea（如果存在）──
        var oldScroll = panel.Find("ScrollArea");
        if (oldScroll != null) Object.DestroyImmediate(oldScroll.gameObject);
        var oldRC = panel.Find("RowContainer");
        if (oldRC != null) Object.DestroyImmediate(oldRC.gameObject);

        // ── 2. 创建 ScrollArea（带 ScrollRect + RectMask2D）──
        var scrollGo = new GameObject("ScrollArea");
        scrollGo.transform.SetParent(panel, false);
        var scrollRT = scrollGo.AddComponent<RectTransform>();
        // 占面板 HeaderRow 下方到 SubtitleText 上方的区域
        scrollRT.anchorMin = new Vector2(0.02f, 0.08f);
        scrollRT.anchorMax = new Vector2(0.98f, 0.85f);
        scrollRT.offsetMin = Vector2.zero;
        scrollRT.offsetMax = Vector2.zero;

        // RectMask2D 裁剪溢出内容
        scrollGo.AddComponent<RectMask2D>();

        // ScrollRect 组件
        var scrollRect = scrollGo.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;

        // ── 3. 创建 Content（RowContainer）──
        var contentGo = new GameObject("RowContainer");
        contentGo.transform.SetParent(scrollGo.transform, false);
        var contentRT = contentGo.AddComponent<RectTransform>();
        // 锚定到顶部，宽度跟随父级
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        // 高度由 ContentSizeFitter 自动控制
        contentRT.sizeDelta = new Vector2(0f, 0f);

        // VLG
        var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 4;
        vlg.padding = new RectOffset(5, 5, 0, 5);

        // ContentSizeFitter
        var csf = contentGo.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 设置 ScrollRect.content
        scrollRect.content = contentRT;

        // ── 4. 预创建 50 行 ──
        for (int i = 0; i < MAX_ROWS; i++)
        {
            CreateRankRow(contentGo.transform, i, font, rowBgSprite);
        }

        // ── 5. 绑定 SurvivalRankingUI._rowContainer ──
        var rankingUI = canvas.GetComponent<DrscfZ.UI.SurvivalRankingUI>();
        if (rankingUI != null)
        {
            var so = new SerializedObject(rankingUI);
            var rowProp = so.FindProperty("_rowContainer");
            if (rowProp != null) rowProp.objectReferenceValue = contentGo.transform;
            so.ApplyModifiedProperties();
        }

        // ── 保存 ──
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[SetupScrollableRanking] ScrollRect + {MAX_ROWS} 行已创建，场景已保存");
    }

    static void CreateRankRow(Transform parent, int index, TMP_FontAsset font, Sprite bgSprite)
    {
        var rowGo = new GameObject($"RankRow_{index}");
        rowGo.transform.SetParent(parent, false);

        var rt = rowGo.AddComponent<RectTransform>();
        // 高度固定 48
        var le = rowGo.AddComponent<LayoutElement>();
        le.preferredHeight = 48;
        le.minHeight = 48;

        // 背景
        var img = rowGo.AddComponent<Image>();
        if (bgSprite != null)
        {
            img.sprite = bgSprite;
            img.type = Image.Type.Sliced;
            // Top 3 金色背景，其余深色半透明
            img.color = index < 3
                ? new Color(0.6f, 0.45f, 0.15f, 0.6f)
                : new Color(0.1f, 0.15f, 0.25f, 0.4f);
        }
        else
        {
            img.color = index < 3
                ? new Color(0.6f, 0.45f, 0.15f, 0.6f)
                : new Color(0.1f, 0.15f, 0.25f, 0.4f);
        }

        // HLG
        var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.spacing = 5;
        hlg.padding = new RectOffset(10, 10, 2, 2);

        // 三列 TMP
        CreateCell(rowGo.transform, "RankNum",    $"#{index + 1}", 0.12f, font, TextAlignmentOptions.Center, 22);
        CreateCell(rowGo.transform, "PlayerName", "",               0.53f, font, TextAlignmentOptions.Left, 22);
        CreateCell(rowGo.transform, "Score",      "",               0.35f, font, TextAlignmentOptions.Right, 22);

        // 默认隐藏
        rowGo.SetActive(false);
    }

    static void CreateCell(Transform parent, string name, string text, float flex, TMP_FontAsset font, TextAlignmentOptions align, int fontSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();

        var layoutElem = go.AddComponent<LayoutElement>();
        layoutElem.flexibleWidth = flex;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        if (font != null) tmp.font = font;

        // 白色文字
        var so = new SerializedObject(tmp);
        var col = Color.white;
        var p1 = so.FindProperty("m_fontColor");
        if (p1 != null) p1.colorValue = col;
        var p2 = so.FindProperty("m_fontColor32");
        if (p2 != null) p2.colorValue = col;
        so.ApplyModifiedProperties();
    }
}
