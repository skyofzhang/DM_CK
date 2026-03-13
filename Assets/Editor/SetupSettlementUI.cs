using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

public class SetupSettlementUI
{
    [MenuItem("极地生存/Setup Settlement UI")]
    public static void Execute()
    {
        // Find main canvas
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("[SetupSettlementUI] Canvas not found!"); return; }

        // Remove existing panel if already present
        var existing = canvas.transform.Find("SurvivalSettlementPanel");
        if (existing != null)
        {
            GameObject.DestroyImmediate(existing.gameObject);
            Debug.Log("[SetupSettlementUI] Removed existing SurvivalSettlementPanel.");
        }

        // ── ROOT PANEL ─────────────────────────────────────────────────────────
        var root = new GameObject("SurvivalSettlementPanel");
        root.transform.SetParent(canvas.transform, false);
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        root.AddComponent<CanvasGroup>();

        // Background (dark polar blue, semi-opaque)
        var bg = new GameObject("BG");
        bg.transform.SetParent(root.transform, false);
        var bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.04f, 0.07f, 0.14f, 0.93f);

        // ── SCREEN A: RESULT ──────────────────────────────────────────────────
        var screenA = new GameObject("ScreenA");
        screenA.transform.SetParent(root.transform, false);
        var saRect = screenA.AddComponent<RectTransform>();
        saRect.anchorMin = Vector2.zero;
        saRect.anchorMax = Vector2.one;
        saRect.offsetMin = Vector2.zero;
        saRect.offsetMax = Vector2.zero;

        // ResultTitle (centered, large)
        var title = CreateTMP("ResultTitle", screenA.transform, 72, FontStyles.Bold);
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.1f, 0.55f);
        titleRect.anchorMax = new Vector2(0.9f, 0.75f);
        titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;
        title.text = "极地已守护!";
        title.color = new Color(1f, 0.85f, 0.1f);
        title.alignment = TextAlignmentOptions.Center;

        // ResultSubtitle
        var subtitle = CreateTMP("ResultSubtitle", screenA.transform, 48, FontStyles.Normal);
        var subtitleRect = subtitle.GetComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0.1f, 0.42f);
        subtitleRect.anchorMax = new Vector2(0.9f, 0.55f);
        subtitleRect.offsetMin = subtitleRect.offsetMax = Vector2.zero;
        subtitle.text = "坚守了 7 天";
        subtitle.color = Color.white;
        subtitle.alignment = TextAlignmentOptions.Center;

        // ── SCREEN B: STATS ───────────────────────────────────────────────────
        var screenB = new GameObject("ScreenB");
        screenB.transform.SetParent(root.transform, false);
        var sbRect = screenB.AddComponent<RectTransform>();
        sbRect.anchorMin = Vector2.zero;
        sbRect.anchorMax = Vector2.one;
        sbRect.offsetMin = Vector2.zero;
        sbRect.offsetMax = Vector2.zero;
        screenB.SetActive(false);

        // Stats header
        var statsHeader = CreateTMP("StatsHeader", screenB.transform, 52, FontStyles.Bold);
        var shRect = statsHeader.GetComponent<RectTransform>();
        shRect.anchorMin = new Vector2(0.05f, 0.86f);
        shRect.anchorMax = new Vector2(0.95f, 0.97f);
        shRect.offsetMin = shRect.offsetMax = Vector2.zero;
        statsHeader.text = "本局数据";
        statsHeader.color = new Color(1f, 0.85f, 0.1f);
        statsHeader.alignment = TextAlignmentOptions.Center;

        // Stats text rows
        float[] statsY = { 0.78f, 0.69f, 0.60f, 0.51f };
        string[] statsDefaults = { "生存天数: 7", "总击杀: 0", "总采集: 0", "总修墙: 0" };
        string[] statsNames    = { "SurvivalDaysText", "TotalKillsText", "TotalGatherText", "TotalRepairText" };
        for (int i = 0; i < 4; i++)
        {
            var st = CreateTMP(statsNames[i], screenB.transform, 40, FontStyles.Normal);
            var r = st.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0.1f, statsY[i] - 0.08f);
            r.anchorMax = new Vector2(0.9f, statsY[i]);
            r.offsetMin = r.offsetMax = Vector2.zero;
            st.text = statsDefaults[i];
            st.color = Color.white;
            st.alignment = TextAlignmentOptions.Left;
        }

        // Ranking title
        var rankTitle = CreateTMP("RankingTitle", screenB.transform, 44, FontStyles.Bold);
        var rtRect = rankTitle.GetComponent<RectTransform>();
        rtRect.anchorMin = new Vector2(0.05f, 0.37f);
        rtRect.anchorMax = new Vector2(0.95f, 0.48f);
        rtRect.offsetMin = rtRect.offsetMax = Vector2.zero;
        rankTitle.text = "贡献榜";
        rankTitle.color = new Color(1f, 0.85f, 0.1f);
        rankTitle.alignment = TextAlignmentOptions.Center;

        // RankingList (VerticalLayoutGroup container)
        var rankList = new GameObject("RankingList");
        rankList.transform.SetParent(screenB.transform, false);
        var rlRect = rankList.AddComponent<RectTransform>();
        rlRect.anchorMin = new Vector2(0.05f, 0.02f);
        rlRect.anchorMax = new Vector2(0.95f, 0.37f);
        rlRect.offsetMin = rlRect.offsetMax = Vector2.zero;
        var vlg = rankList.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.padding = new RectOffset(4, 4, 4, 4);

        // Pre-create 10 rank entries (rule: pre-created in scene, not instantiated at runtime)
        for (int i = 0; i < 10; i++)
        {
            var entry = new GameObject($"RankEntry_{i}");
            entry.transform.SetParent(rankList.transform, false);
            var entryRect = entry.AddComponent<RectTransform>();
            entryRect.sizeDelta = new Vector2(0, 52);

            var hlg = entry.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childControlHeight = true;
            hlg.childControlWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.padding = new RectOffset(8, 8, 2, 2);

            // Rank number (#1, #2, ...)
            var rank = CreateTMP("RankText", entry.transform, 36, FontStyles.Bold);
            rank.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 0);
            rank.text = $"#{i + 1}";
            rank.color = (i == 0) ? new Color(1f, 0.85f, 0.1f) :
                         (i == 1) ? new Color(0.8f, 0.8f, 0.8f) :
                         (i == 2) ? new Color(0.8f, 0.55f, 0.3f) :
                                    new Color(0.7f, 0.7f, 0.7f);
            rank.alignment = TextAlignmentOptions.Center;

            // Player name (flexible width)
            var nameText = CreateTMP("NameText", entry.transform, 36, FontStyles.Normal);
            var nameLe = nameText.gameObject.AddComponent<LayoutElement>();
            nameLe.flexibleWidth = 1;
            nameText.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 0);
            nameText.text = "玩家名";
            nameText.color = Color.white;
            nameText.alignment = TextAlignmentOptions.Left;

            // Score
            var scoreText = CreateTMP("ScoreText", entry.transform, 36, FontStyles.Normal);
            scoreText.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 0);
            scoreText.text = "0";
            scoreText.color = new Color(0.8f, 1f, 0.8f);
            scoreText.alignment = TextAlignmentOptions.Right;

            // Only first 3 visible by default (others shown at runtime based on data)
            entry.SetActive(i < 3);
        }

        // ── SCREEN C: MVP ─────────────────────────────────────────────────────
        var screenC = new GameObject("ScreenC");
        screenC.transform.SetParent(root.transform, false);
        var scRect = screenC.AddComponent<RectTransform>();
        scRect.anchorMin = Vector2.zero;
        scRect.anchorMax = Vector2.one;
        scRect.offsetMin = Vector2.zero;
        scRect.offsetMax = Vector2.zero;
        screenC.SetActive(false);

        // MVP label
        var mvpLabel = CreateTMP("MvpLabel", screenC.transform, 40, FontStyles.Bold);
        var mlRect = mvpLabel.GetComponent<RectTransform>();
        mlRect.anchorMin = new Vector2(0.1f, 0.65f);
        mlRect.anchorMax = new Vector2(0.9f, 0.75f);
        mlRect.offsetMin = mlRect.offsetMax = Vector2.zero;
        mvpLabel.text = "— 本局最佳 —";
        mvpLabel.color = new Color(1f, 0.85f, 0.1f);
        mvpLabel.alignment = TextAlignmentOptions.Center;

        // MVP player name
        var mvpName = CreateTMP("MvpNameText", screenC.transform, 64, FontStyles.Bold);
        var mnRect = mvpName.GetComponent<RectTransform>();
        mnRect.anchorMin = new Vector2(0.1f, 0.52f);
        mnRect.anchorMax = new Vector2(0.9f, 0.68f);
        mnRect.offsetMin = mnRect.offsetMax = Vector2.zero;
        mvpName.text = "MVP: 玩家名";
        mvpName.color = new Color(1f, 0.85f, 0.1f);
        mvpName.alignment = TextAlignmentOptions.Center;

        // MVP score
        var mvpScore = CreateTMP("MvpScoreText", screenC.transform, 48, FontStyles.Normal);
        var msRect = mvpScore.GetComponent<RectTransform>();
        msRect.anchorMin = new Vector2(0.1f, 0.41f);
        msRect.anchorMax = new Vector2(0.9f, 0.53f);
        msRect.offsetMin = msRect.offsetMax = Vector2.zero;
        mvpScore.text = "贡献值: 0";
        mvpScore.color = Color.white;
        mvpScore.alignment = TextAlignmentOptions.Center;

        // MVP anchor line (主播词)
        var mvpAnchor = CreateTMP("MvpAnchorLine", screenC.transform, 38, FontStyles.Italic);
        var maRect = mvpAnchor.GetComponent<RectTransform>();
        maRect.anchorMin = new Vector2(0.05f, 0.28f);
        maRect.anchorMax = new Vector2(0.95f, 0.41f);
        maRect.offsetMin = maRect.offsetMax = Vector2.zero;
        mvpAnchor.text = "本局MVP是 XXX，感谢TA的付出！";
        mvpAnchor.color = new Color(0.8f, 0.95f, 1f);
        mvpAnchor.alignment = TextAlignmentOptions.Center;

        // ── RESTART BUTTON ────────────────────────────────────────────────────
        var btnObj = new GameObject("RestartButton");
        btnObj.transform.SetParent(root.transform, false);
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.3f, 0.04f);
        btnRect.anchorMax = new Vector2(0.7f, 0.13f);
        btnRect.offsetMin = btnRect.offsetMax = Vector2.zero;
        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.09f, 0.18f, 0.35f, 1f);
        var btn = btnObj.AddComponent<Button>();

        var btnText = CreateTMP("Text", btnObj.transform, 44, FontStyles.Bold);
        var btRect = btnText.GetComponent<RectTransform>();
        btRect.anchorMin = Vector2.zero;
        btRect.anchorMax = Vector2.one;
        btRect.offsetMin = btRect.offsetMax = Vector2.zero;
        btnText.text = "再战一局";
        btnText.color = Color.white;
        btnText.alignment = TextAlignmentOptions.Center;

        // ── SET INACTIVE (per AI rule #2: pre-create inactive) ────────────────
        root.SetActive(false);

        // ── ATTACH SCRIPT AND WIRE REFERENCES ────────────────────────────────
        var uiScript = root.AddComponent<DrscfZ.UI.SurvivalSettlementUI>();
        var so = new SerializedObject(uiScript);

        so.FindProperty("_screenA").objectReferenceValue = screenA;
        so.FindProperty("_screenB").objectReferenceValue = screenB;
        so.FindProperty("_screenC").objectReferenceValue = screenC;
        so.FindProperty("_resultTitleText").objectReferenceValue  = title;
        so.FindProperty("_resultSubtitleText").objectReferenceValue = subtitle;
        so.FindProperty("_survivalDaysText").objectReferenceValue =
            screenB.transform.Find("SurvivalDaysText")?.GetComponent<TextMeshProUGUI>();
        so.FindProperty("_totalKillsText").objectReferenceValue =
            screenB.transform.Find("TotalKillsText")?.GetComponent<TextMeshProUGUI>();
        so.FindProperty("_totalGatherText").objectReferenceValue =
            screenB.transform.Find("TotalGatherText")?.GetComponent<TextMeshProUGUI>();
        so.FindProperty("_totalRepairText").objectReferenceValue =
            screenB.transform.Find("TotalRepairText")?.GetComponent<TextMeshProUGUI>();
        so.FindProperty("_rankingListParent").objectReferenceValue = rankList.transform;
        so.FindProperty("_mvpNameText").objectReferenceValue  = mvpName;
        so.FindProperty("_mvpScoreText").objectReferenceValue = mvpScore;
        so.FindProperty("_mvpAnchorLineText").objectReferenceValue = mvpAnchor;
        so.FindProperty("_restartButton").objectReferenceValue = btn;

        so.ApplyModifiedProperties();

        // Mark scene dirty and save
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[SetupSettlementUI] SurvivalSettlementPanel created successfully on Canvas.");
    }

    private static TextMeshProUGUI CreateTMP(string name, Transform parent, float size, FontStyles style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.fontStyle = style;
        return tmp;
    }
}
