#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 极地生存法则 — Phase 5+6: 结算面板 + 主菜单 竖屏重建
/// 执行菜单: DrscfZ/Phase 5 - Rebuild Settlement
///           DrscfZ/Phase 6 - Rebuild MainMenu
/// </summary>
public class RebuildPortraitPanels
{
    // ═══════════════════════════════════════════════════════
    // PHASE 5 — SettlementPanel
    // ═══════════════════════════════════════════════════════

    [MenuItem("DrscfZ/Phase 5 - Rebuild Settlement Panel", false, 130)]
    public static void ExecuteSettlement()
    {
        Debug.Log("[Phase5] ===== 开始 Phase 5: 结算面板竖屏重建 =====");

        var panel = FindGO("Canvas/SettlementPanel");
        if (!panel) { Debug.LogError("[Phase5] SettlementPanel not found"); return; }

        // ── 面板全屏背景 ──
        SetRT(panel, 0f,0f, 1f,1f, 0.5f,0.5f, 0f,0f, 0f,0f);
        var bg = EnsureImage(panel);
        bg.color = new Color(0.016f, 0.055f, 0.188f, 0.96f); // 深极地蓝
        bg.raycastTarget = true;
        EditorUtility.SetDirty(panel);

        // ── 胜利标题 (顶部, y=1780-1920, 高140px) ──
        var winner = FindGO("Canvas/SettlementPanel/WinnerText");
        if (winner)
        {
            SetRT(winner, 0.5f,1f, 0.5f,1f, 0.5f,0.5f, 0f,-90f, 1000f,160f);
            StyleTMP(winner, 64f, new Color(0.988f,0.827f,0.302f), TextAlignmentOptions.Center);
            var tmp = winner.GetComponent<TextMeshProUGUI>();
            if (tmp) tmp.fontStyle = FontStyles.Bold;
            EditorUtility.SetDirty(winner);
        }

        // ── 连胜信息 (y=1700-1740) ──
        var streak = FindGO("Canvas/SettlementPanel/StreakInfoText");
        if (streak)
        {
            SetRT(streak, 0.5f,1f, 0.5f,1f, 0.5f,0.5f, 0f,-195f, 820f,42f);
            StyleTMP(streak, 24f, new Color(0.72f,0.90f,1.0f), TextAlignmentOptions.Center);
            EditorUtility.SetDirty(streak);
        }

        // ── MVP 区域 (y=1560-1700, 140px) ──
        SetupMVPArea();

        // ── 双列排行榜 (y=1020-1540, 各540px高) ──
        SetupRankColumns();

        // ── 积分分配 (y=900-1020) ──
        SetupScoreDistArea();

        // ── 重新开始按钮 ──
        var btnRestart = FindGO("Canvas/SettlementPanel/BtnRestart");
        if (btnRestart)
        {
            SetRT(btnRestart, 0.5f,0f, 0.5f,0f, 0.5f,0.5f, 0f,56f, 420f,88f);
            var img = EnsureImage(btnRestart);
            img.color = new Color(0.145f,0.388f,0.922f,1f); // 蓝色按钮
            // 确保子文字正确
            var label = btnRestart.GetComponentInChildren<TextMeshProUGUI>();
            if (label) { label.text = "再来一局"; label.fontSize = 32f; label.color = Color.white; label.alignment = TextAlignmentOptions.Center; }
            EditorUtility.SetDirty(btnRestart);
        }

        SaveScene();
        Debug.Log("[Phase5] ===== 结算面板 重建完成 =====");
    }

    static void SetupMVPArea()
    {
        var mvpArea = FindGO("Canvas/SettlementPanel/MVPArea");
        if (!mvpArea) return;

        SetRT(mvpArea, 0.5f,1f, 0.5f,1f, 0.5f,0.5f, 0f,-310f, 960f,120f);
        var bg = EnsureImage(mvpArea);
        bg.color = new Color(0.043f,0.137f,0.298f, 0.88f);
        EditorUtility.SetDirty(mvpArea);

        // MVP头像框
        var frame = FindGO("Canvas/SettlementPanel/MVPArea/MVPAvatarFrame");
        if (frame)
        {
            SetRT(frame, 0f,0.5f, 0f,0.5f, 0f,0.5f, 12f,0f, 80f,80f);
            var fi = EnsureImage(frame);
            fi.color = new Color(0.988f,0.827f,0.302f,1f); // 金色边框
            EditorUtility.SetDirty(frame);
        }

        // MVP名字
        var name_ = FindGO("Canvas/SettlementPanel/MVPArea/MVPNameBar/MVPName");
        if (name_)
        {
            SetRT(name_, 0f,1f, 0f,1f, 0f,1f, 100f,-8f, 520f,48f);
            StyleTMP(name_, 28f, Color.white, TextAlignmentOptions.Left);
            EditorUtility.SetDirty(name_);
        }

        // MVP标签
        var label_ = FindGO("Canvas/SettlementPanel/MVPArea/MVPLabel");
        if (label_)
        {
            SetRT(label_, 0f,1f, 0f,1f, 0f,1f, 100f,-58f, 200f,36f);
            StyleTMP(label_, 20f, new Color(0.988f,0.827f,0.302f), TextAlignmentOptions.Left);
            EditorUtility.SetDirty(label_);
        }

        // MVP贡献值
        var contrib = FindGO("Canvas/SettlementPanel/MVPArea/MVPContribution");
        if (contrib)
        {
            SetRT(contrib, 1f,0.5f, 1f,0.5f, 1f,0.5f, -12f,0f, 240f,60f);
            StyleTMP(contrib, 24f, new Color(0.988f,0.827f,0.302f), TextAlignmentOptions.Right);
            EditorUtility.SetDirty(contrib);
        }
    }

    static void SetupRankColumns()
    {
        // 标题行 y=1540, 列高 530px -> bottom at y=1010
        var leftCol = FindGO("Canvas/SettlementPanel/LeftRankColumn");
        if (leftCol)
        {
            SetRT(leftCol, 0f,1f, 0.5f,1f, 0f,1f, 4f,-430f, -8f,530f);
            var bg = EnsureImage(leftCol);
            bg.color = new Color(0.090f,0.216f,0.557f, 0.65f); // 蓝色半透明
            EditorUtility.SetDirty(leftCol);
        }

        var rightCol = FindGO("Canvas/SettlementPanel/RightRankColumn");
        if (rightCol)
        {
            SetRT(rightCol, 0.5f,1f, 1f,1f, 1f,1f, -4f,-430f, -8f,530f);
            var bg = EnsureImage(rightCol);
            bg.color = new Color(0.557f,0.090f,0.090f, 0.65f); // 红色半透明
            EditorUtility.SetDirty(rightCol);
        }

        // 标题行
        StyleRankTitle("Canvas/SettlementPanel/LeftRankColumn/LeftRankTitle",  "🔵 冰蓝队 Top 10", new Color(0.60f,0.78f,1.0f));
        StyleRankTitle("Canvas/SettlementPanel/RightRankColumn/RightRankTitle","🔴 冰红队 Top 10", new Color(1.0f,0.60f,0.60f));

        // 10行排行数据
        for (int i = 0; i < 10; i++)
        {
            float rowY = -42f - i * 48f; // 从标题行下方开始
            StyleRankRow($"Canvas/SettlementPanel/LeftRankColumn/LeftRankName_{i}",  rowY, true,  i);
            StyleRankRow($"Canvas/SettlementPanel/LeftRankColumn/LeftRankVal_{i}",   rowY, false, i);
            StyleRankRow($"Canvas/SettlementPanel/RightRankColumn/RightRankName_{i}", rowY, true,  i);
            StyleRankRow($"Canvas/SettlementPanel/RightRankColumn/RightRankVal_{i}",  rowY, false, i);
        }
    }

    static void StyleRankTitle(string path, string text, Color color)
    {
        var go = FindGO(path);
        if (!go) return;
        SetRT(go, 0f,1f, 1f,1f, 0.5f,1f, 0f,0f, 0f,40f);
        StyleTMP(go, 20f, color, TextAlignmentOptions.Center);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp) { tmp.text = text; tmp.fontStyle = FontStyles.Bold; }
        EditorUtility.SetDirty(go);
    }

    static void StyleRankRow(string path, float anchoredY, bool isName, int rank)
    {
        var go = FindGO(path);
        if (!go) return;

        if (isName)
        {
            SetRT(go, 0f,1f, 0f,1f, 0f,1f, 6f, anchoredY, 180f, 44f);
            StyleTMP(go, 18f, Color.white, TextAlignmentOptions.Left);
        }
        else
        {
            SetRT(go, 1f,1f, 1f,1f, 1f,1f, -6f, anchoredY, 120f, 44f);
            StyleTMP(go, 18f, new Color(0.988f,0.827f,0.302f), TextAlignmentOptions.Right);
        }

        // 前3名高亮
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp && rank < 3)
        {
            Color[] topColors = { new Color(1.0f,0.84f,0.0f), new Color(0.75f,0.75f,0.75f), new Color(0.80f,0.50f,0.20f) };
            tmp.color = topColors[rank];
        }
        EditorUtility.SetDirty(go);
    }

    static void SetupScoreDistArea()
    {
        var area = FindGO("Canvas/SettlementPanel/ScoreDistArea");
        if (!area) return;

        SetRT(area, 0.5f,0f, 0.5f,0f, 0.5f,0.5f, 0f,172f, 960f,100f);
        var bg = EnsureImage(area);
        bg.color = new Color(0.024f,0.071f,0.173f, 0.90f);
        EditorUtility.SetDirty(area);

        // 积分池标签
        var lbl = FindGO("Canvas/SettlementPanel/ScoreDistArea/ScorePoolLabel");
        if (lbl)
        {
            SetRT(lbl, 0f,1f, 1f,1f, 0.5f,1f, 0f,0f, 0f,36f);
            StyleTMP(lbl, 22f, new Color(0.988f,0.827f,0.302f), TextAlignmentOptions.Center);
            EditorUtility.SetDirty(lbl);
        }

        // 积分分配行 (6行, 横排2列)
        for (int i = 0; i < 6; i++)
        {
            float xName = (i % 2 == 0) ? -240f : 40f;
            float xVal  = (i % 2 == 0) ? -50f  : 440f;
            float yRow  = -36f - (i / 2) * 32f;

            var nameGO = FindGO($"Canvas/SettlementPanel/ScoreDistArea/ScoreDistName_{i}");
            var valGO  = FindGO($"Canvas/SettlementPanel/ScoreDistArea/ScoreDistVal_{i}");
            if (nameGO) { SetRT(nameGO, 0.5f,1f, 0.5f,1f, 0.5f,0.5f, xName,yRow, 180f,28f); StyleTMP(nameGO, 17f, new Color(0.7f,0.87f,1f), TextAlignmentOptions.Left);  EditorUtility.SetDirty(nameGO); }
            if (valGO)  { SetRT(valGO,  0.5f,1f, 0.5f,1f, 0.5f,0.5f, xVal, yRow, 160f,28f); StyleTMP(valGO,  17f, Color.white,              TextAlignmentOptions.Left); EditorUtility.SetDirty(valGO); }
        }
    }

    // ═══════════════════════════════════════════════════════
    // PHASE 6 — MainMenuPanel
    // ═══════════════════════════════════════════════════════

    [MenuItem("DrscfZ/Phase 6 - Rebuild MainMenu Panel", false, 140)]
    public static void ExecuteMainMenu()
    {
        Debug.Log("[Phase6] ===== 开始 Phase 6: 主菜单竖屏重建 =====");

        var panel = FindGO("Canvas/MainMenuPanel");
        if (!panel) { Debug.LogError("[Phase6] MainMenuPanel not found"); return; }

        // ── 面板背景 ──
        SetRT(panel, 0f,0f, 1f,1f, 0.5f,0.5f, 0f,0f, 0f,0f);
        var bg = EnsureImage(panel);
        bg.color = new Color(0.020f, 0.063f, 0.161f, 0.96f); // 深夜极地蓝
        EditorUtility.SetDirty(panel);

        // ── Logo 区域 (顶部, y=1400-1840) ──
        SetupLogo();

        // ── 按钮组 (中下部) ──
        SetupMainMenuButtons();

        SaveScene();
        Debug.Log("[Phase6] ===== 主菜单 重建完成 =====");
    }

    static void SetupLogo()
    {
        var logo = FindGO("Canvas/MainMenuPanel/Logo");
        if (!logo) return;

        SetRT(logo, 0.5f,1f, 0.5f,1f, 0.5f,0.5f, 0f,-320f, 960f,440f);
        EditorUtility.SetDirty(logo);

        // 确保Logo有一个主标题TMP
        var tmpAll = logo.GetComponentsInChildren<TextMeshProUGUI>();
        if (tmpAll.Length > 0)
        {
            // 最大的TMP当主标题
            TextMeshProUGUI main = tmpAll[0];
            foreach (var t in tmpAll) if (t.fontSize > main.fontSize) main = t;

            main.text      = "极地生存法则";
            main.fontSize  = 72f;
            main.color     = new Color(0.988f, 0.827f, 0.302f); // 金色
            main.fontStyle = FontStyles.Bold;
            main.alignment = TextAlignmentOptions.Center;
            EditorUtility.SetDirty(main.gameObject);
        }
        else
        {
            // Logo下没有TMP，新建一个
            var titleGO = new GameObject("TitleText");
            titleGO.transform.SetParent(logo.transform, false);
            var rt = titleGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f,0.5f); rt.anchorMax = new Vector2(1f,1f);
            rt.sizeDelta = Vector2.zero; rt.anchoredPosition = new Vector2(0f,-20f);
            var tmp = titleGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "极地生存法则"; tmp.fontSize = 72f;
            tmp.color = new Color(0.988f,0.827f,0.302f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;

            var subGO = new GameObject("SubText");
            subGO.transform.SetParent(logo.transform, false);
            var sRt = subGO.AddComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0f,0f); sRt.anchorMax = new Vector2(1f,0.5f);
            sRt.sizeDelta = Vector2.zero; sRt.anchoredPosition = new Vector2(0f,10f);
            var sTmp = subGO.AddComponent<TextMeshProUGUI>();
            sTmp.text = "极地冰原团队对战  刷礼物推动雪球赢得胜利";
            sTmp.fontSize = 26f; sTmp.color = new Color(0.7f,0.87f,1.0f);
            sTmp.alignment = TextAlignmentOptions.Center;
        }
    }

    static void SetupMainMenuButtons()
    {
        var group = FindGO("Canvas/MainMenuPanel/ButtonGroup");
        if (group)
        {
            SetRT(group, 0.5f,0f, 0.5f,0f, 0.5f,0.5f, 0f,480f, 700f,480f);
            EditorUtility.SetDirty(group);
        }

        // ── BtnStartGame — 主按钮, 全宽 ──
        var btnStart = FindGO("Canvas/MainMenuPanel/ButtonGroup/BtnStartGame");
        if (btnStart)
        {
            SetRT(btnStart, 0.5f,1f, 0.5f,1f, 0.5f,0.5f, 0f,-55f, 620f,100f);
            var img = EnsureImage(btnStart);
            img.color = new Color(0.145f,0.388f,0.922f,1f); // 冰蓝主按钮
            StyleBtnLabel(btnStart, "🎮  开始玩法", 36f, Color.white);
            EditorUtility.SetDirty(btnStart);
        }

        // ── BtnLeaderboard ──
        var btnLeader = FindGO("Canvas/MainMenuPanel/ButtonGroup/BtnLeaderboard");
        if (btnLeader)
        {
            SetRT(btnLeader, 0.5f,1f, 0.5f,1f, 0.5f,0.5f, -175f,-175f, 310f,76f);
            var img = EnsureImage(btnLeader);
            img.color = new Color(0.043f,0.137f,0.298f,0.90f);
            StyleBtnLabel(btnLeader, "排行榜", 26f, new Color(0.72f,0.90f,1.0f));
            EditorUtility.SetDirty(btnLeader);
        }

        // ── BtnGiftDesc ──
        var btnGift = FindGO("Canvas/MainMenuPanel/ButtonGroup/BtnGiftDesc");
        if (btnGift)
        {
            SetRT(btnGift, 0.5f,1f, 0.5f,1f, 0.5f,0.5f, 175f,-175f, 310f,76f);
            var img = EnsureImage(btnGift);
            img.color = new Color(0.043f,0.137f,0.298f,0.90f);
            StyleBtnLabel(btnGift, "礼物说明", 26f, new Color(0.72f,0.90f,1.0f));
            EditorUtility.SetDirty(btnGift);
        }

        // ── BtnRuleDesc ──
        var btnRule = FindGO("Canvas/MainMenuPanel/ButtonGroup/BtnRuleDesc");
        if (btnRule)
        {
            SetRT(btnRule, 0.5f,1f, 0.5f,1f, 0.5f,0.5f, -175f,-275f, 310f,76f);
            var img = EnsureImage(btnRule);
            img.color = new Color(0.043f,0.137f,0.298f,0.90f);
            StyleBtnLabel(btnRule, "游戏规则", 26f, new Color(0.72f,0.90f,1.0f));
            EditorUtility.SetDirty(btnRule);
        }

        // ── BtnStickerSettings ──
        var btnSticker = FindGO("Canvas/MainMenuPanel/ButtonGroup/BtnStickerSettings");
        if (btnSticker)
        {
            SetRT(btnSticker, 0.5f,1f, 0.5f,1f, 0.5f,0.5f, 175f,-275f, 310f,76f);
            var img = EnsureImage(btnSticker);
            img.color = new Color(0.043f,0.137f,0.298f,0.90f);
            StyleBtnLabel(btnSticker, "贴纸设置", 26f, new Color(0.72f,0.90f,1.0f));
            EditorUtility.SetDirty(btnSticker);
        }

        // ── 版本号文字 ──
        EnsureVersionText(FindGO("Canvas/MainMenuPanel"));
    }

    static void StyleBtnLabel(GameObject btn, string text, float size, Color color)
    {
        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp == null)
        {
            var lGO = new GameObject("Label");
            lGO.transform.SetParent(btn.transform, false);
            var rt = lGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero; rt.anchoredPosition = Vector2.zero;
            tmp = lGO.AddComponent<TextMeshProUGUI>();
        }
        tmp.text = text; tmp.fontSize = size; tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        EditorUtility.SetDirty(tmp.gameObject);
    }

    static void EnsureVersionText(GameObject panel)
    {
        if (!panel) return;
        var existing = panel.transform.Find("VersionText");
        GameObject go;
        if (existing) go = existing.gameObject;
        else
        {
            go = new GameObject("VersionText");
            go.transform.SetParent(panel.transform, false);
        }
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        SetRT(go, 1f,0f, 1f,0f, 1f,0f, -12f,12f, 220f,28f);
        var tmp = go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
        tmp.text = "v1.0.0  极地生存法则";
        tmp.fontSize = 18f;
        tmp.color = new Color(0.5f, 0.65f, 0.85f, 0.7f);
        tmp.alignment = TextAlignmentOptions.Right;
        EditorUtility.SetDirty(go);
    }

    // ═══════════════════════════════════════════════════════
    // PHASE 7+8 — Announcement + GM Debug Bar
    // ═══════════════════════════════════════════════════════

    [MenuItem("DrscfZ/Phase 7+8 - Announcement + GM Bar", false, 150)]
    public static void ExecuteAnnouncementGM()
    {
        Debug.Log("[Phase7] ===== 开始 Phase 7+8: 公告 + GM调试栏 =====");

        // ── AnnouncementPanel — 全屏居中 ──
        var announcement = FindGO("Canvas/AnnouncementPanel");
        if (announcement)
        {
            SetRT(announcement, 0f,0f, 1f,1f, 0.5f,0.5f, 0f,0f, 0f,0f);
            EditorUtility.SetDirty(announcement);

            var mainTxt = FindGO("Canvas/AnnouncementPanel/MainText");
            if (mainTxt)
            {
                SetRT(mainTxt, 0.5f,0.5f, 0.5f,0.5f, 0.5f,0.5f, 0f,80f, 900f,200f);
                StyleTMP(mainTxt, 80f, new Color(0.988f,0.827f,0.302f), TextAlignmentOptions.Center);
                var tmp = mainTxt.GetComponent<TextMeshProUGUI>();
                if (tmp) tmp.fontStyle = FontStyles.Bold;
                EditorUtility.SetDirty(mainTxt);
            }

            var subTxt = FindGO("Canvas/AnnouncementPanel/SubText");
            if (subTxt)
            {
                SetRT(subTxt, 0.5f,0.5f, 0.5f,0.5f, 0.5f,0.5f, 0f,-60f, 800f,80f);
                StyleTMP(subTxt, 36f, Color.white, TextAlignmentOptions.Center);
                EditorUtility.SetDirty(subTxt);
            }
        }

        // ── BottomBar (GM调试栏) — 竖屏右下角 ──
        var bottomBar = FindGO("Canvas/BottomBar");
        if (bottomBar)
        {
            // 右下角小条，不遮挡游戏区域
            SetRT(bottomBar, 1f,0f, 1f,0f, 1f,0f, 0f,742f, 460f,200f);
            var img = bottomBar.GetComponent<Image>();
            if (img) img.color = new Color(0.02f,0.06f,0.14f, 0.90f);
            EditorUtility.SetDirty(bottomBar);
        }

        SaveScene();
        Debug.Log("[Phase7] ===== 公告 + GM调试栏 调整完成 =====");
    }

    // ─────────────────────────────────────────
    // Helpers (shared)
    // ─────────────────────────────────────────

    static void SetRT(GameObject go,
        float ancMinX, float ancMinY, float ancMaxX, float ancMaxY,
        float pivX,    float pivY,
        float posX,    float posY,
        float sizeX,   float sizeY)
    {
        if (!go) return;
        var rt = go.GetComponent<RectTransform>();
        if (!rt) rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(ancMinX, ancMinY);
        rt.anchorMax        = new Vector2(ancMaxX, ancMaxY);
        rt.pivot            = new Vector2(pivX, pivY);
        rt.anchoredPosition = new Vector2(posX, posY);
        rt.sizeDelta        = new Vector2(sizeX, sizeY);
        EditorUtility.SetDirty(go);
    }

    static void StyleTMP(GameObject go, float size, Color color, TextAlignmentOptions align)
    {
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp) { tmp.fontSize = size; tmp.color = color; tmp.alignment = align; }
    }

    static Image EnsureImage(GameObject go)
    {
        var img = go.GetComponent<Image>();
        if (!img) img = go.AddComponent<Image>();
        return img;
    }

    static void SaveScene()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
    }

    static GameObject FindGO(string path)
    {
        var go = GameObject.Find(path);
        if (go) return go;
        var parts    = path.Split('/');
        var lastName = parts[parts.Length - 1];
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t == null || t.name != lastName) continue;
            if (BuildPath(t) == path) return t.gameObject;
        }
        Debug.LogWarning($"NOT FOUND: {path}");
        return null;
    }

    static string BuildPath(Transform t)
    {
        var parts = new List<string>();
        while (t != null) { parts.Insert(0, t.name); t = t.parent; }
        return string.Join("/", parts);
    }
}
#endif
