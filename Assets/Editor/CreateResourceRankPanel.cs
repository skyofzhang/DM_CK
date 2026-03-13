using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using DrscfZ.UI;

/// <summary>
/// 在 Canvas/GameUIPanel 下创建 ResourceRankPanel UI 层级，
/// 并绑定 ResourceRankUI 组件的所有 TMP 字段引用。
///
/// 运行方式：Tools → DrscfZ → Create Resource Rank Panel
/// </summary>
public static class CreateResourceRankPanel
{
    [MenuItem("Tools/DrscfZ/Create Resource Rank Panel")]
    public static void Execute()
    {
        // ── 找父容器 ──────────────────────────────────────────────────────────
        var gameUIPanel = GameObject.Find("Canvas/GameUIPanel");
        if (gameUIPanel == null)
        {
            Debug.LogError("[CreateResourceRankPanel] 找不到 Canvas/GameUIPanel，请确认场景已打开 MainScene");
            return;
        }

        // 若已存在则先删除重建
        var existing = gameUIPanel.transform.Find("ResourceRankPanel");
        if (existing != null)
        {
            GameObject.DestroyImmediate(existing.gameObject);
            Debug.Log("[CreateResourceRankPanel] 已删除旧 ResourceRankPanel，重新创建");
        }

        // ── 根面板 ────────────────────────────────────────────────────────────
        var panelGO = new GameObject("ResourceRankPanel");
        panelGO.transform.SetParent(gameUIPanel.transform, false);

        var panelRT = panelGO.AddComponent<RectTransform>();
        // 锚定顶部全宽，Y 偏移 -10（相对父容器顶部向下10px），高度 90px
        panelRT.anchorMin        = new Vector2(0f, 1f);
        panelRT.anchorMax        = new Vector2(1f, 1f);
        panelRT.pivot            = new Vector2(0.5f, 1f);
        panelRT.anchoredPosition = new Vector2(0f, -10f);
        panelRT.sizeDelta        = new Vector2(0f, 90f);

        // 半透明黑色背景
        var bgImg = panelGO.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.5f);
        bgImg.raycastTarget = false;

        // 挂载 ResourceRankUI（始终激活，符合 Rule#7）
        var rankUI = panelGO.AddComponent<ResourceRankUI>();

        // ── 水平布局（3列）────────────────────────────────────────────────────
        var hlg = panelGO.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth    = true;
        hlg.childControlHeight   = true;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.spacing              = 4f;
        hlg.padding              = new RectOffset(6, 6, 4, 4);

        // ── 字体加载 ──────────────────────────────────────────────────────────
        var chineseFont    = Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
        var chineseFontMat = Resources.Load<Material>("Fonts/ChineseFont SDF - Outline");
        if (chineseFont == null)
            Debug.LogWarning("[CreateResourceRankPanel] 未找到 Fonts/ChineseFont SDF，TMP 字体将使用默认值");

        // ── 创建3列 ───────────────────────────────────────────────────────────
        string[]           colIds    = { "Food", "Coal", "Ore" };
        string[]           colLabels = { "食物贡献", "煤炭贡献", "矿石贡献" };
        TextMeshProUGUI[]  titleTmps = new TextMeshProUGUI[3];
        TextMeshProUGUI[][] rowTmps  = new TextMeshProUGUI[3][];

        for (int c = 0; c < 3; c++)
        {
            // 列容器
            var colGO = new GameObject($"Col_{colIds[c]}");
            colGO.transform.SetParent(panelGO.transform, false);
            colGO.AddComponent<RectTransform>();

            var colVlg = colGO.AddComponent<VerticalLayoutGroup>();
            colVlg.childControlWidth    = true;
            colVlg.childControlHeight   = true;
            colVlg.childForceExpandWidth  = true;
            colVlg.childForceExpandHeight = true;
            colVlg.spacing              = 1f;

            // 标题行
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(colGO.transform, false);
            var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            titleTMP.text      = colLabels[c];
            titleTMP.fontSize  = 18f;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.color     = new Color(1f, 0.92f, 0.4f);
            BindFont(titleTMP, chineseFont, chineseFontMat);
            titleTmps[c] = titleTMP;

            // 3 行数据
            rowTmps[c] = new TextMeshProUGUI[3];
            for (int r = 0; r < 3; r++)
            {
                var rowGO = new GameObject($"Row{r + 1}");
                rowGO.transform.SetParent(colGO.transform, false);
                var rowTMP = rowGO.AddComponent<TextMeshProUGUI>();
                rowTMP.text      = $"{r + 1}. —";
                rowTMP.fontSize  = 15f;
                rowTMP.alignment = TextAlignmentOptions.Center;
                rowTMP.color     = Color.white;
                BindFont(rowTMP, chineseFont, chineseFontMat);
                rowTmps[c][r] = rowTMP;
            }
        }

        // ── 通过 SerializedObject 绑定字段 ────────────────────────────────────
        var so = new SerializedObject(rankUI);

        // _panel
        so.FindProperty("_panel").objectReferenceValue = panelGO;

        // _foodTitle / _coalTitle / _oreTitle
        so.FindProperty("_foodTitle").objectReferenceValue = titleTmps[0];
        so.FindProperty("_coalTitle").objectReferenceValue = titleTmps[1];
        so.FindProperty("_oreTitle" ).objectReferenceValue = titleTmps[2];

        // _foodRows / _coalRows / _oreRows
        SetTMPArray(so, "_foodRows", rowTmps[0]);
        SetTMPArray(so, "_coalRows", rowTmps[1]);
        SetTMPArray(so, "_oreRows",  rowTmps[2]);

        so.ApplyModifiedProperties();

        // ── 面板初始隐藏（由 ResourceRankUI.Awake 控制）──────────────────────
        // 注：panelGO 本身始终 active，SetActive(false) 在 Awake 中调用
        // 此处确保 panelGO 在 Hierarchy 中为 active（满足 Rule#7 脚本挂载要求）
        panelGO.SetActive(true);

        // ── 标记场景已修改 ────────────────────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[CreateResourceRankPanel] ResourceRankPanel 创建并绑定完成！");
        Debug.Log("  路径: Canvas/GameUIPanel/ResourceRankPanel");
        Debug.Log("  已绑定: _panel, _foodTitle, _coalTitle, _oreTitle, _foodRows[3], _coalRows[3], _oreRows[3]");
        Debug.Log("  请运行 Tools → DrscfZ → Save Current Scene 保存场景");
    }

    private static void BindFont(TextMeshProUGUI tmp, TMP_FontAsset font, Material mat)
    {
        if (tmp == null) return;
        if (font != null) tmp.font = font;
        if (mat  != null) tmp.fontSharedMaterial = mat;
    }

    private static void SetTMPArray(SerializedObject so, string propName, TextMeshProUGUI[] tmps)
    {
        var arr = so.FindProperty(propName);
        arr.arraySize = tmps.Length;
        for (int i = 0; i < tmps.Length; i++)
            arr.GetArrayElementAtIndex(i).objectReferenceValue = tmps[i];
    }
}
