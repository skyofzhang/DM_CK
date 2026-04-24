using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using DrscfZ.UI;

/// <summary>
/// 一键修复 SurvivalRankingPanel 结构 + 字段绑定
/// 菜单: DrscfZ / Setup Survival Ranking Panel
/// </summary>
public class SetupSurvivalRankingPanel
{
    [MenuItem("DrscfZ/Setup Survival Ranking Panel")]
    public static void Setup()
    {
        // ── 1. 找到 Canvas 上的 SurvivalRankingUI ──
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("[Setup] Canvas 未找到"); return; }

        var ui = canvas.GetComponent<SurvivalRankingUI>();
        if (ui == null) { Debug.LogError("[Setup] Canvas 上没有 SurvivalRankingUI 脚本"); return; }

        // ── 2. 找到 SurvivalRankingPanel ──
        var panelGo = canvas.transform.Find("SurvivalRankingPanel")?.gameObject;
        if (panelGo == null) { Debug.LogError("[Setup] SurvivalRankingPanel 未找到"); return; }

        // ── 3. 加载字体 ──
        var font = Resources.Load<TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF") ?? Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
        if (font == null) Debug.LogWarning("[Setup] ChineseFont SDF 未找到，字体将为默认值");

        // ── 4. 修复 TitleText ──
        var titleTextTr = panelGo.transform.Find("TitleText");
        TMP_Text titleTmp = null;
        if (titleTextTr != null)
        {
            titleTmp = titleTextTr.GetComponent<TMP_Text>();
            if (titleTmp != null && font != null)
            {
                titleTmp.font = font;
                titleTmp.fontSize = 32f;
                titleTmp.fontStyle = FontStyles.Bold;
                titleTmp.color = Color.white;
                titleTmp.alignment = TextAlignmentOptions.Center;
                titleTmp.enableWordWrapping = false;
                titleTmp.overflowMode = TextOverflowModes.Overflow;
                titleTmp.text = "排行榜";
                EditorUtility.SetDirty(titleTextTr.gameObject);
            }
        }

        // ── 5. 确保有背景 ──
        if (panelGo.GetComponent<Image>() == null)
        {
            var bg = panelGo.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.15f, 0.92f);
        }
        else
        {
            var bg = panelGo.GetComponent<Image>();
            if (bg.color.a < 0.1f)
                bg.color = new Color(0.05f, 0.05f, 0.15f, 0.92f);
        }

        // ── 6. CloseBtn ──
        Button closeBtn = null;
        var closeBtnTr = panelGo.transform.Find("CloseBtn");
        if (closeBtnTr == null)
        {
            var go = new GameObject("CloseBtn", typeof(RectTransform));
            go.transform.SetParent(panelGo.transform, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);
            closeBtn = go.AddComponent<Button>();

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-20f, -20f);
            rt.sizeDelta = new Vector2(70f, 70f);

            // 关闭按钮文字
            var label = new GameObject("Label", typeof(RectTransform));
            label.transform.SetParent(go.transform, false);
            var lTmp = label.AddComponent<TextMeshProUGUI>();
            lTmp.text = "✕";
            lTmp.fontSize = 36f;
            lTmp.color = Color.white;
            lTmp.alignment = TextAlignmentOptions.Center;
            if (font != null) lTmp.font = font;
            var lRT = label.GetComponent<RectTransform>();
            lRT.anchorMin = Vector2.zero;
            lRT.anchorMax = Vector2.one;
            lRT.offsetMin = lRT.offsetMax = Vector2.zero;
            EditorUtility.SetDirty(go);
        }
        else
        {
            closeBtn = closeBtnTr.GetComponent<Button>();
        }

        // ── 7. EmptyHint ──
        TMP_Text emptyHint = null;
        var emptyTr = panelGo.transform.Find("EmptyHint");
        if (emptyTr == null)
        {
            var go = new GameObject("EmptyHint", typeof(RectTransform));
            go.transform.SetParent(panelGo.transform, false);
            emptyHint = go.AddComponent<TextMeshProUGUI>();
            if (font != null) emptyHint.font = font;
            emptyHint.text = "暂无本场数据";
            emptyHint.fontSize = 34f;
            emptyHint.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            emptyHint.alignment = TextAlignmentOptions.Center;
            emptyHint.enableWordWrapping = false;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(0f, 60f);
            go.SetActive(true); // 初始显示（没数据时显示）
            EditorUtility.SetDirty(go);
        }
        else
        {
            emptyHint = emptyTr.GetComponent<TMP_Text>();
            if (emptyHint != null && font != null) emptyHint.font = font;
        }

        // ── 8. RowContainer ──
        Transform rowContainer = panelGo.transform.Find("RowContainer");
        if (rowContainer == null)
        {
            var go = new GameObject("RowContainer", typeof(RectTransform));
            go.transform.SetParent(panelGo.transform, false);
            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(20, 20, 0, 0);

            var csf = go.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.1f);
            rt.anchorMax = new Vector2(1f, 0.85f);
            rt.offsetMin = new Vector2(20f, 0f);
            rt.offsetMax = new Vector2(-20f, 0f);

            rowContainer = go.transform;
            EditorUtility.SetDirty(go);
        }

        // ── 9. 预创建 10 行（每行: 名次 + 名字 + 贡献值）──
        int existingRows = rowContainer.childCount;
        for (int i = existingRows; i < 10; i++)
        {
            var row = new GameObject($"RankRow_{i}", typeof(RectTransform));
            row.transform.SetParent(rowContainer, false);

            var rowRT = row.GetComponent<RectTransform>();
            rowRT.sizeDelta = new Vector2(0f, 56f);

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // 行背景
            var bgImg = row.AddComponent<Image>();
            bgImg.color = new Color(0.12f, 0.15f, 0.28f, 0.8f);
            bgImg.raycastTarget = false;

            // 名次 TMP
            var numGo = new GameObject("RankNum", typeof(RectTransform));
            numGo.transform.SetParent(row.transform, false);
            var numRT = numGo.GetComponent<RectTransform>();
            numRT.sizeDelta = new Vector2(60f, 56f);
            var numTmp = numGo.AddComponent<TextMeshProUGUI>();
            numTmp.text = $"#{i + 1}";
            numTmp.fontSize = 28f;
            numTmp.fontStyle = FontStyles.Bold;
            numTmp.color = new Color(1f, 0.85f, 0.3f);
            numTmp.alignment = TextAlignmentOptions.Center;
            numTmp.enableWordWrapping = false;
            if (font != null) numTmp.font = font;

            // 名字 TMP
            var nameGo = new GameObject("PlayerName", typeof(RectTransform));
            nameGo.transform.SetParent(row.transform, false);
            var nameRT = nameGo.GetComponent<RectTransform>();
            nameRT.sizeDelta = new Vector2(280f, 56f);
            var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            nameTmp.text = "玩家名";
            nameTmp.fontSize = 26f;
            nameTmp.color = Color.white;
            nameTmp.alignment = TextAlignmentOptions.Left;
            nameTmp.overflowMode = TextOverflowModes.Ellipsis;
            nameTmp.enableWordWrapping = false;
            if (font != null) nameTmp.font = font;

            // 贡献值 TMP
            var scoreGo = new GameObject("Score", typeof(RectTransform));
            scoreGo.transform.SetParent(row.transform, false);
            var scoreRT = scoreGo.GetComponent<RectTransform>();
            scoreRT.sizeDelta = new Vector2(160f, 56f);
            var scoreTmp = scoreGo.AddComponent<TextMeshProUGUI>();
            scoreTmp.text = "0";
            scoreTmp.fontSize = 24f;
            scoreTmp.color = new Color(1f, 0.9f, 0.5f);
            scoreTmp.alignment = TextAlignmentOptions.Right;
            scoreTmp.enableWordWrapping = false;
            if (font != null) scoreTmp.font = font;

            row.SetActive(false); // 初始隐藏，由脚本按数据显示
            EditorUtility.SetDirty(row);
        }

        // ── 10. 确保 Panel 的 TitleText 在最顶层（sibling=0）──
        if (titleTextTr != null)
            titleTextTr.SetSiblingIndex(0);

        // ── 11. 确保 Panel 初始为 inactive（AI准则#2）──
        panelGo.SetActive(false);

        // ── 12. 通过 SerializedObject 绑定 SurvivalRankingUI 字段 ──
        var so = new SerializedObject(ui);

        var spPanel      = so.FindProperty("_panel");
        var spCloseBtn   = so.FindProperty("_closeBtn");
        var spTitleText  = so.FindProperty("_titleText");
        var spRowCont    = so.FindProperty("_rowContainer");
        var spEmptyHint  = so.FindProperty("_emptyHint");

        if (spPanel      != null) spPanel.objectReferenceValue      = panelGo;
        if (spCloseBtn   != null && closeBtn != null)
                                  spCloseBtn.objectReferenceValue   = closeBtn;
        if (spTitleText  != null && titleTmp != null)
                                  spTitleText.objectReferenceValue  = titleTmp;
        if (spRowCont    != null) spRowCont.objectReferenceValue    = rowContainer;
        if (spEmptyHint  != null && emptyHint != null)
                                  spEmptyHint.objectReferenceValue  = emptyHint;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(ui);

        // ── 13. 保存场景 ──
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[Setup] SurvivalRankingPanel 结构修复完成！已绑定: _panel / _closeBtn / _titleText / _rowContainer / _emptyHint，预创建10行");
    }
}
