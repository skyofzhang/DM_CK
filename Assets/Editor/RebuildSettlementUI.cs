using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.IO;

/// <summary>
/// 重建结算界面 UI：极地冰雪主题
/// - 极光背景 + 暗色遮罩
/// - Kenney 蓝色按钮/面板
/// - Top3 槽位创建
/// Menu: Tools → DrscfZ → Rebuild Settlement UI
/// </summary>
public class RebuildSettlementUI : Editor
{
    static TMP_FontAsset _font;
    static readonly Color IceBlueDark  = new Color(0.08f, 0.12f, 0.22f, 0.95f);
    static readonly Color IceBlueLight = new Color(0.35f, 0.65f, 0.85f, 1f);
    static readonly Color IceCyan      = new Color(0.4f, 0.8f, 0.95f, 1f);
    static readonly Color GoldColor    = new Color(1f, 0.85f, 0.1f, 1f);
    static readonly Color WhiteColor   = Color.white;
    static readonly Color PanelBgColor = new Color(0.05f, 0.1f, 0.2f, 0.85f);

    [MenuItem("Tools/DrscfZ/Rebuild Settlement UI")]
    public static void Execute()
    {
        // 1) 加载字体
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Resources/Fonts/ChineseFont SDF.asset");
        if (_font == null) { Debug.LogError("找不到中文字体"); return; }

        // 2) 导入精灵资源（设置9切片等）
        ImportSprites();

        // 3) 找到 SurvivalSettlementPanel
        var panel = FindSettlementPanel();
        if (panel == null) { Debug.LogError("找不到 SurvivalSettlementPanel"); return; }

        // 4) 重建 BG
        RebuildBG(panel.transform);

        // 5) 重建 ScreenA
        RebuildScreenA(panel.transform);

        // 6) 重建 ScreenB
        RebuildScreenB(panel.transform);

        // 7) 重建 ScreenC + Top3 Slots
        RebuildScreenC(panel.transform);

        // 8) 重建按钮
        RebuildButtons(panel.transform);

        // 9) 绑定引用
        WireReferences(panel);

        // 10) 保存
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[RebuildSettlementUI] 结算界面重建完成！");
    }

    // ─── Import Sprites ─────────────────────────────────────────
    static void ImportSprites()
    {
        // 按钮精灵 - 设置为 Sprite + 9-slice
        SetupSprite("Assets/Art/UI/KenneyUI/PNG/Blue/Default/button_rectangle_depth_gloss.png",
            new Vector4(12, 12, 12, 16)); // left, bottom, right, top
        SetupSprite("Assets/Art/UI/KenneyUI/PNG/Blue/Default/button_rectangle_depth_border.png",
            new Vector4(12, 12, 12, 16));
        // 面板精灵
        SetupSprite("Assets/Art/UI/KenneyUI/RPG/PNG/panel_blue.png",
            new Vector4(10, 10, 10, 10));
        SetupSprite("Assets/Art/UI/KenneyUI/RPG/PNG/panelInset_blue.png",
            new Vector4(10, 10, 10, 10));
        // 极光背景
        SetupSprite("Assets/Art/UI/Settlement/settlement_bg_aurora.jpg", Vector4.zero, true);

        AssetDatabase.Refresh();
    }

    static void SetupSprite(string path, Vector4 border, bool isBackground = false)
    {
        if (!File.Exists(path)) { Debug.LogWarning($"找不到: {path}"); return; }
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        if (border != Vector4.zero)
            importer.spriteBorder = border;
        if (isBackground)
        {
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Compressed;
        }
        else
        {
            importer.maxTextureSize = 256;
        }
        importer.SaveAndReimport();
    }

    // ─── Find Panel ─────────────────────────────────────────────
    static GameObject FindSettlementPanel()
    {
        // 在场景中查找
        var all = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in all)
        {
            if (t.name == "SurvivalSettlementPanel" && t.GetComponent<RectTransform>() != null)
            {
                // 确保是场景对象
                if (t.gameObject.scene.isLoaded || t.gameObject.scene.name != null)
                    return t.gameObject;
            }
        }
        return null;
    }

    // ─── Rebuild BG ─────────────────────────────────────────────
    static void RebuildBG(Transform panel)
    {
        // 找到或创建 BG
        var bgT = panel.Find("BG");
        if (bgT == null)
        {
            var bgGO = new GameObject("BG", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGO.transform.SetParent(panel, false);
            bgGO.transform.SetAsFirstSibling();
            bgT = bgGO.transform;
        }

        var bgRect = bgT.GetComponent<RectTransform>();
        StretchFull(bgRect);

        // 暗色底层
        var bgImg = bgT.GetComponent<Image>();
        bgImg.sprite = null;
        bgImg.color = new Color(0.03f, 0.06f, 0.12f, 0.96f);

        // 极光装饰层
        var auroraT = bgT.Find("AuroraDecor");
        if (auroraT == null)
        {
            var auroraGO = new GameObject("AuroraDecor", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            auroraGO.transform.SetParent(bgT, false);
            auroraT = auroraGO.transform;
        }

        var auroraRect = auroraT.GetComponent<RectTransform>();
        // 占顶部 40%
        auroraRect.anchorMin = new Vector2(0, 0.55f);
        auroraRect.anchorMax = new Vector2(1, 1f);
        auroraRect.offsetMin = Vector2.zero;
        auroraRect.offsetMax = Vector2.zero;

        var auroraSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Settlement/settlement_bg_aurora.jpg");
        var auroraImg = auroraT.GetComponent<Image>();
        auroraImg.sprite = auroraSpr;
        auroraImg.color = new Color(0.5f, 0.5f, 0.6f, 0.4f); // 半透明暗化
        auroraImg.preserveAspect = false;
        auroraImg.type = Image.Type.Simple;

        // 底部渐变遮罩（通过额外一层）
        var fadeT = bgT.Find("BottomFade");
        if (fadeT == null)
        {
            var fadeGO = new GameObject("BottomFade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fadeGO.transform.SetParent(bgT, false);
            fadeT = fadeGO.transform;
        }
        var fadeRect = fadeT.GetComponent<RectTransform>();
        fadeRect.anchorMin = new Vector2(0, 0);
        fadeRect.anchorMax = new Vector2(1, 0.6f);
        fadeRect.offsetMin = Vector2.zero;
        fadeRect.offsetMax = Vector2.zero;

        var fadeImg = fadeT.GetComponent<Image>();
        fadeImg.sprite = null;
        fadeImg.color = new Color(0.03f, 0.06f, 0.12f, 0.95f);
    }

    // ─── Rebuild ScreenA ────────────────────────────────────────
    static void RebuildScreenA(Transform panel)
    {
        var screenA = EnsureChild(panel, "ScreenA");
        StretchFull(screenA.GetComponent<RectTransform>());
        screenA.SetActive(false);

        // ResultTitle
        var titleTMP = EnsureTMP(screenA, "ResultTitle");
        var titleRect = titleTMP.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.05f, 0.55f);
        titleRect.anchorMax = new Vector2(0.95f, 0.7f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        SetTMPStyle(titleTMP, 72, FontStyles.Bold, GoldColor, TextAlignmentOptions.Center);
        titleTMP.text = "极地已守护!";
        titleTMP.enableWordWrapping = false;

        // 装饰线（标题下方）
        var divider = EnsureChild(screenA, "TitleDivider");
        var divRect = divider.GetComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0.2f, 0.53f);
        divRect.anchorMax = new Vector2(0.8f, 0.535f);
        divRect.offsetMin = Vector2.zero;
        divRect.offsetMax = Vector2.zero;
        var divImg = divider.GetComponent<Image>();
        if (divImg == null) divImg = divider.AddComponent<Image>();
        divImg.color = new Color(0.4f, 0.75f, 0.9f, 0.6f);

        // ResultSubtitle
        var subTMP = EnsureTMP(screenA, "ResultSubtitle");
        var subRect = subTMP.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.1f, 0.42f);
        subRect.anchorMax = new Vector2(0.9f, 0.52f);
        subRect.offsetMin = Vector2.zero;
        subRect.offsetMax = Vector2.zero;
        SetTMPStyle(subTMP, 48, FontStyles.Normal, WhiteColor, TextAlignmentOptions.Center);
        subTMP.text = "坚守了 3 天";
    }

    // ─── Rebuild ScreenB ────────────────────────────────────────
    static void RebuildScreenB(Transform panel)
    {
        var screenB = EnsureChild(panel, "ScreenB");
        StretchFull(screenB.GetComponent<RectTransform>());
        screenB.SetActive(false);

        // 标题
        var headerTMP = EnsureTMP(screenB, "StatsHeader");
        var headerRect = headerTMP.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0.05f, 0.82f);
        headerRect.anchorMax = new Vector2(0.95f, 0.92f);
        headerRect.offsetMin = Vector2.zero;
        headerRect.offsetMax = Vector2.zero;
        SetTMPStyle(headerTMP, 52, FontStyles.Bold, IceCyan, TextAlignmentOptions.Center);
        headerTMP.text = "本局数据";

        // 数据面板背景
        var statsBg = EnsureChild(screenB, "StatsPanelBg");
        var statsBgRect = statsBg.GetComponent<RectTransform>();
        statsBgRect.anchorMin = new Vector2(0.06f, 0.52f);
        statsBgRect.anchorMax = new Vector2(0.94f, 0.81f);
        statsBgRect.offsetMin = Vector2.zero;
        statsBgRect.offsetMax = Vector2.zero;

        var panelSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/KenneyUI/RPG/PNG/panelInset_blue.png");
        var statsBgImg = statsBg.GetComponent<Image>();
        if (statsBgImg == null) statsBgImg = statsBg.AddComponent<Image>();
        statsBgImg.sprite = panelSpr;
        statsBgImg.type = Image.Type.Sliced;
        statsBgImg.color = new Color(0.15f, 0.25f, 0.4f, 0.8f);

        // 4个数据文本
        string[] statNames = { "TotalGatherText", "TotalKillsText", "TotalRepairText", "SurvivalDaysText" };
        string[] defaults  = { "总采集: 0", "总击杀: 0", "总修墙: 0", "生存天数: 0" };
        for (int i = 0; i < 4; i++)
        {
            var tmp = EnsureTMP(screenB, statNames[i]);
            var rect = tmp.GetComponent<RectTransform>();
            float yMax = 0.78f - i * 0.065f;
            float yMin = yMax - 0.055f;
            rect.anchorMin = new Vector2(0.12f, yMin);
            rect.anchorMax = new Vector2(0.88f, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            SetTMPStyle(tmp, 40, FontStyles.Normal, WhiteColor, TextAlignmentOptions.Center);
            tmp.text = defaults[i];
        }

        // 贡献榜标题
        var rankTitleTMP = EnsureTMP(screenB, "RankingTitle");
        var rankTitleRect = rankTitleTMP.GetComponent<RectTransform>();
        rankTitleRect.anchorMin = new Vector2(0.1f, 0.42f);
        rankTitleRect.anchorMax = new Vector2(0.9f, 0.5f);
        rankTitleRect.offsetMin = Vector2.zero;
        rankTitleRect.offsetMax = Vector2.zero;
        SetTMPStyle(rankTitleTMP, 44, FontStyles.Bold, GoldColor, TextAlignmentOptions.Center);
        rankTitleTMP.text = "贡献榜";

        // 排名列表背景
        var rankBg = EnsureChild(screenB, "RankListBg");
        var rankBgRect = rankBg.GetComponent<RectTransform>();
        rankBgRect.anchorMin = new Vector2(0.06f, 0.12f);
        rankBgRect.anchorMax = new Vector2(0.94f, 0.42f);
        rankBgRect.offsetMin = Vector2.zero;
        rankBgRect.offsetMax = Vector2.zero;
        var rankBgImg = rankBg.GetComponent<Image>();
        if (rankBgImg == null) rankBgImg = rankBg.AddComponent<Image>();
        rankBgImg.sprite = panelSpr;
        rankBgImg.type = Image.Type.Sliced;
        rankBgImg.color = new Color(0.1f, 0.18f, 0.3f, 0.7f);

        // RankingList 容器
        var rankList = EnsureChild(screenB, "RankingList");
        var rankListRect = rankList.GetComponent<RectTransform>();
        rankListRect.anchorMin = new Vector2(0.08f, 0.13f);
        rankListRect.anchorMax = new Vector2(0.92f, 0.41f);
        rankListRect.offsetMin = Vector2.zero;
        rankListRect.offsetMax = Vector2.zero;

        var vlg = rankList.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = rankList.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = true;
        vlg.padding = new RectOffset(10, 10, 5, 5);

        // 创建/更新 10 个 RankEntry
        for (int i = 0; i < 10; i++)
        {
            string entryName = $"RankEntry_{i}";
            var entry = EnsureChild(rankList.transform, entryName);
            entry.SetActive(i < 3); // 默认只显示前3

            var hlg = entry.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = entry.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandHeight = true;

            // RankText
            var rankTmp = EnsureTMP(entry.transform, "RankText");
            SetTMPStyle(rankTmp, 34, FontStyles.Bold, IceCyan, TextAlignmentOptions.Center);
            rankTmp.text = $"#{i + 1}";
            var rankLE = rankTmp.GetComponent<LayoutElement>();
            if (rankLE == null) rankLE = rankTmp.gameObject.AddComponent<LayoutElement>();
            rankLE.preferredWidth = 80;
            rankLE.flexibleWidth = 0;

            // NameText
            var nameTmp = EnsureTMP(entry.transform, "NameText");
            SetTMPStyle(nameTmp, 34, FontStyles.Normal, WhiteColor, TextAlignmentOptions.Left);
            nameTmp.text = "—";
            var nameLE = nameTmp.GetComponent<LayoutElement>();
            if (nameLE == null) nameLE = nameTmp.gameObject.AddComponent<LayoutElement>();
            nameLE.flexibleWidth = 1;

            // ScoreText
            var scoreTmp = EnsureTMP(entry.transform, "ScoreText");
            SetTMPStyle(scoreTmp, 34, FontStyles.Normal, GoldColor, TextAlignmentOptions.Right);
            scoreTmp.text = "0";
            var scoreLE = scoreTmp.GetComponent<LayoutElement>();
            if (scoreLE == null) scoreLE = scoreTmp.gameObject.AddComponent<LayoutElement>();
            scoreLE.preferredWidth = 120;
            scoreLE.flexibleWidth = 0;
        }
    }

    // ─── Rebuild ScreenC ────────────────────────────────────────
    static void RebuildScreenC(Transform panel)
    {
        var screenC = EnsureChild(panel, "ScreenC");
        StretchFull(screenC.GetComponent<RectTransform>());
        screenC.SetActive(false);

        // MVP 标签
        var mvpLabel = EnsureTMP(screenC, "MvpLabel");
        var mvpLabelRect = mvpLabel.GetComponent<RectTransform>();
        mvpLabelRect.anchorMin = new Vector2(0.1f, 0.72f);
        mvpLabelRect.anchorMax = new Vector2(0.9f, 0.82f);
        mvpLabelRect.offsetMin = Vector2.zero;
        mvpLabelRect.offsetMax = Vector2.zero;
        SetTMPStyle(mvpLabel, 40, FontStyles.Bold, GoldColor, TextAlignmentOptions.Center);
        mvpLabel.text = "-- 本局最佳 --";

        // MVP 名字
        var mvpName = EnsureTMP(screenC, "MvpNameText");
        var mvpNameRect = mvpName.GetComponent<RectTransform>();
        mvpNameRect.anchorMin = new Vector2(0.1f, 0.62f);
        mvpNameRect.anchorMax = new Vector2(0.9f, 0.72f);
        mvpNameRect.offsetMin = Vector2.zero;
        mvpNameRect.offsetMax = Vector2.zero;
        SetTMPStyle(mvpName, 64, FontStyles.Bold, GoldColor, TextAlignmentOptions.Center);
        mvpName.text = "MVP";

        // MVP 分数
        var mvpScore = EnsureTMP(screenC, "MvpScoreText");
        var mvpScoreRect = mvpScore.GetComponent<RectTransform>();
        mvpScoreRect.anchorMin = new Vector2(0.15f, 0.55f);
        mvpScoreRect.anchorMax = new Vector2(0.85f, 0.62f);
        mvpScoreRect.offsetMin = Vector2.zero;
        mvpScoreRect.offsetMax = Vector2.zero;
        SetTMPStyle(mvpScore, 42, FontStyles.Normal, WhiteColor, TextAlignmentOptions.Center);
        mvpScore.text = "贡献值: 0";

        // MVP 主播评语
        var mvpAnchor = EnsureTMP(screenC, "MvpAnchorLine");
        var mvpAnchorRect = mvpAnchor.GetComponent<RectTransform>();
        mvpAnchorRect.anchorMin = new Vector2(0.08f, 0.47f);
        mvpAnchorRect.anchorMax = new Vector2(0.92f, 0.54f);
        mvpAnchorRect.offsetMin = Vector2.zero;
        mvpAnchorRect.offsetMax = Vector2.zero;
        SetTMPStyle(mvpAnchor, 34, FontStyles.Italic, IceBlueLight, TextAlignmentOptions.Center);
        mvpAnchor.text = "本局MVP是 XXX，感谢TA的付出！";

        // Top3 面板背景
        var top3Bg = EnsureChild(screenC, "Top3PanelBg");
        var top3BgRect = top3Bg.GetComponent<RectTransform>();
        top3BgRect.anchorMin = new Vector2(0.08f, 0.18f);
        top3BgRect.anchorMax = new Vector2(0.92f, 0.46f);
        top3BgRect.offsetMin = Vector2.zero;
        top3BgRect.offsetMax = Vector2.zero;

        var panelSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/KenneyUI/RPG/PNG/panel_blue.png");
        var top3BgImg = top3Bg.GetComponent<Image>();
        if (top3BgImg == null) top3BgImg = top3Bg.AddComponent<Image>();
        top3BgImg.sprite = panelSpr;
        top3BgImg.type = Image.Type.Sliced;
        top3BgImg.color = new Color(0.12f, 0.2f, 0.35f, 0.85f);

        // Top3 槽位
        for (int i = 0; i < 3; i++)
        {
            string slotName = $"Top3Slot_{i}";
            var slot = EnsureChild(screenC, slotName);
            var slotRect = slot.GetComponent<RectTransform>();
            float yMax = 0.43f - i * 0.085f;
            float yMin = yMax - 0.075f;
            slotRect.anchorMin = new Vector2(0.1f, yMin);
            slotRect.anchorMax = new Vector2(0.9f, yMax);
            slotRect.offsetMin = Vector2.zero;
            slotRect.offsetMax = Vector2.zero;

            // 槽位背景（内嵌面板）
            var slotBg = EnsureChild(slot.transform, "SlotBg");
            StretchFull(slotBg.GetComponent<RectTransform>());
            var insetSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/KenneyUI/RPG/PNG/panelInset_blue.png");
            var slotBgImg = slotBg.GetComponent<Image>();
            if (slotBgImg == null) slotBgImg = slotBg.AddComponent<Image>();
            slotBgImg.sprite = insetSpr;
            slotBgImg.type = Image.Type.Sliced;
            float alpha = i == 0 ? 0.7f : 0.5f;
            slotBgImg.color = new Color(0.15f, 0.25f, 0.4f, alpha);

            // 排名标记
            string[] rankLabels = { "1st", "2nd", "3rd" };
            Color[] rankColors = { GoldColor, new Color(0.75f, 0.75f, 0.8f), new Color(0.8f, 0.55f, 0.3f) };
            var rankTmp = EnsureTMP(slot.transform, "RankMark");
            var rankRect = rankTmp.GetComponent<RectTransform>();
            rankRect.anchorMin = new Vector2(0.02f, 0.1f);
            rankRect.anchorMax = new Vector2(0.15f, 0.9f);
            rankRect.offsetMin = Vector2.zero;
            rankRect.offsetMax = Vector2.zero;
            SetTMPStyle(rankTmp, 32, FontStyles.Bold, rankColors[i], TextAlignmentOptions.Center);
            rankTmp.text = rankLabels[i];

            // 名字
            var nameTmp = EnsureTMP(slot.transform, "NameText");
            var nameRect = nameTmp.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.16f, 0.1f);
            nameRect.anchorMax = new Vector2(0.65f, 0.9f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;
            int fontSize = i == 0 ? 40 : 36;
            SetTMPStyle(nameTmp, fontSize, FontStyles.Bold, WhiteColor, TextAlignmentOptions.Left);
            nameTmp.text = "—";

            // 分数
            var scoreTmp = EnsureTMP(slot.transform, "ScoreText");
            var scoreRect = scoreTmp.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0.65f, 0.1f);
            scoreRect.anchorMax = new Vector2(0.98f, 0.9f);
            scoreRect.offsetMin = Vector2.zero;
            scoreRect.offsetMax = Vector2.zero;
            SetTMPStyle(scoreTmp, fontSize, FontStyles.Normal, GoldColor, TextAlignmentOptions.Right);
            scoreTmp.text = "0";
        }
    }

    // ─── Rebuild Buttons ────────────────────────────────────────
    static void RebuildButtons(Transform panel)
    {
        var btnSpr = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Art/UI/KenneyUI/PNG/Blue/Default/button_rectangle_depth_gloss.png");
        var btnBorderSpr = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Art/UI/KenneyUI/PNG/Blue/Default/button_rectangle_depth_border.png");

        // 查看英雄榜
        var btnView = EnsureButton(panel, "BtnViewRanking");
        var btnViewRect = btnView.GetComponent<RectTransform>();
        btnViewRect.anchorMin = new Vector2(0.08f, 0.04f);
        btnViewRect.anchorMax = new Vector2(0.48f, 0.1f);
        btnViewRect.offsetMin = Vector2.zero;
        btnViewRect.offsetMax = Vector2.zero;

        var viewImg = btnView.GetComponent<Image>();
        viewImg.sprite = btnSpr;
        viewImg.type = Image.Type.Sliced;
        viewImg.color = new Color(0.3f, 0.6f, 0.85f, 1f);

        var viewTmp = EnsureTMP(btnView.transform, "Label");
        SetTMPStyle(viewTmp, 38, FontStyles.Bold, WhiteColor, TextAlignmentOptions.Center);
        viewTmp.text = "查看英雄榜";

        // 返回大厅 / 再战一局
        var btnRestart = EnsureButton(panel, "RestartButton");
        var btnRestartRect = btnRestart.GetComponent<RectTransform>();
        btnRestartRect.anchorMin = new Vector2(0.52f, 0.04f);
        btnRestartRect.anchorMax = new Vector2(0.92f, 0.1f);
        btnRestartRect.offsetMin = Vector2.zero;
        btnRestartRect.offsetMax = Vector2.zero;

        var restartImg = btnRestart.GetComponent<Image>();
        restartImg.sprite = btnSpr;
        restartImg.type = Image.Type.Sliced;
        restartImg.color = new Color(0.2f, 0.75f, 0.5f, 1f); // 绿色调

        var restartTmp = EnsureTMP(btnRestart.transform, "Label");
        SetTMPStyle(restartTmp, 38, FontStyles.Bold, WhiteColor, TextAlignmentOptions.Center);
        restartTmp.text = "返回大厅";
    }

    // ─── Wire References ────────────────────────────────────────
    static void WireReferences(GameObject panel)
    {
        var comp = panel.GetComponent<DrscfZ.UI.SurvivalSettlementUI>();
        if (comp == null)
        {
            Debug.LogWarning("[RebuildSettlementUI] SurvivalSettlementUI 组件不存在");
            return;
        }

        var so = new SerializedObject(comp);

        // Screen A
        SetRef(so, "_screenA", panel.transform.Find("ScreenA")?.gameObject);
        SetRef(so, "_resultTitleText", FindTMP(panel.transform, "ScreenA/ResultTitle"));
        SetRef(so, "_resultSubtitleText", FindTMP(panel.transform, "ScreenA/ResultSubtitle"));

        // Screen B
        SetRef(so, "_screenB", panel.transform.Find("ScreenB")?.gameObject);
        SetRef(so, "_survivalDaysText", FindTMP(panel.transform, "ScreenB/SurvivalDaysText"));
        SetRef(so, "_totalKillsText", FindTMP(panel.transform, "ScreenB/TotalKillsText"));
        SetRef(so, "_totalGatherText", FindTMP(panel.transform, "ScreenB/TotalGatherText"));
        SetRef(so, "_totalRepairText", FindTMP(panel.transform, "ScreenB/TotalRepairText"));
        SetRef(so, "_rankingListParent", panel.transform.Find("ScreenB/RankingList"));

        // Screen C
        SetRef(so, "_screenC", panel.transform.Find("ScreenC")?.gameObject);
        SetRef(so, "_mvpAnchorLineText", FindTMP(panel.transform, "ScreenC/MvpAnchorLine"));
        SetRef(so, "_mvpNameText", FindTMP(panel.transform, "ScreenC/MvpNameText"));
        SetRef(so, "_mvpScoreText", FindTMP(panel.transform, "ScreenC/MvpScoreText"));

        // Top3 Slots
        var top3Prop = so.FindProperty("_top3Slots");
        if (top3Prop != null)
        {
            top3Prop.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                var slot = panel.transform.Find($"ScreenC/Top3Slot_{i}");
                top3Prop.GetArrayElementAtIndex(i).objectReferenceValue = slot?.gameObject;
            }
        }

        // Buttons
        SetRef(so, "_restartButton", panel.transform.Find("RestartButton")?.GetComponent<Button>());
        SetRef(so, "_btnViewRanking", panel.transform.Find("BtnViewRanking")?.GetComponent<Button>());

        // RankingSystem
        var rankSys = Object.FindObjectOfType<DrscfZ.Systems.RankingSystem>();
        if (rankSys != null)
            SetRef(so, "_rankingSystem", rankSys);

        so.ApplyModifiedProperties();
        Debug.Log("[RebuildSettlementUI] 所有引用已绑定");
    }

    // ─── Helpers ─────────────────────────────────────────────────

    static GameObject EnsureChild(object parentObj, string name)
    {
        Transform parent;
        if (parentObj is Transform tf) parent = tf;
        else if (parentObj is GameObject go2) parent = go2.transform;
        else return null;

        var t = parent.Find(name);
        if (t != null) return t.gameObject;

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static GameObject EnsureButton(object parent, string name)
    {
        var go = EnsureChild(parent, name);
        if (go.GetComponent<CanvasRenderer>() == null) go.AddComponent<CanvasRenderer>();
        if (go.GetComponent<Image>() == null) go.AddComponent<Image>();
        if (go.GetComponent<Button>() == null) go.AddComponent<Button>();
        return go;
    }

    static TextMeshProUGUI EnsureTMP(object parentObj, string name)
    {
        Transform parent;
        if (parentObj is Transform tf) parent = tf;
        else if (parentObj is GameObject go2) parent = go2.transform;
        else return null;

        var t = parent.Find(name);
        TextMeshProUGUI tmp;
        if (t != null)
        {
            tmp = t.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = t.gameObject.AddComponent<TextMeshProUGUI>();
        }
        else
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            tmp = go.AddComponent<TextMeshProUGUI>();
        }
        return tmp;
    }

    static void SetTMPStyle(TextMeshProUGUI tmp, int size, FontStyles style, Color color, TextAlignmentOptions align)
    {
        if (_font != null) tmp.font = _font;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        // 用 SerializedObject 写颜色（避免 faceColor 白色覆盖）
        var so = new SerializedObject(tmp);
        var fc = so.FindProperty("m_fontColor");
        if (fc != null) fc.colorValue = color;
        var fc32 = so.FindProperty("m_fontColor32");
        if (fc32 != null) fc32.colorValue = color;
        so.ApplyModifiedProperties();
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void SetRef(SerializedObject so, string propName, Object value)
    {
        var prop = so.FindProperty(propName);
        if (prop != null && value != null)
            prop.objectReferenceValue = value;
    }

    static TextMeshProUGUI FindTMP(Transform root, string path)
    {
        var t = root.Find(path);
        return t?.GetComponent<TextMeshProUGUI>();
    }
}
