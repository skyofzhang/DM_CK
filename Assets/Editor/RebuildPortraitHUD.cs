#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 极地生存法则 — Phase 3: TopBar HUD 竖屏重建
/// 执行菜单: DrscfZ/Phase 3 - Rebuild TopBar HUD
///
/// 布局（1080×1920 竖屏, y: -960=底 ~ +960=顶）:
///   y=+960~+860  顶队伍区 (WinStreak, 100px)
///   y=+860~+690  力量条区 (TopBarBg, 170px)
///   y=+690~+620  计时/积分区 (Timer+Score, 70px)
///   y=+620~+280  玩家列表侧栏
///   y=+280~-870  游戏视口
/// </summary>
public class RebuildPortraitHUD
{
    [MenuItem("DrscfZ/Phase 3 - Rebuild TopBar HUD", false, 110)]
    public static void Execute()
    {
        Debug.Log("[Phase3] ===== 开始 Phase 3: TopBar HUD 竖屏重建 =====");

        SetupTopBarContainer();
        SetupForceBar();
        SetupTimerAndScore();
        SetupWinStreaks();
        SetupHintText();
        SetupPlayerLists();
        SetupButtons();
        SetupGiftNotification();

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

        Debug.Log("[Phase3] ===== 完成! TopBar HUD 已重建为竖屏布局. 场景已保存 =====");
    }

    // ════════════════════════════════════════════════════════
    // Phase 3A: TopBar 容器 (遮盖顶部 340px)
    // ════════════════════════════════════════════════════════
    static void SetupTopBarContainer()
    {
        var topBar = Find("Canvas/GameUIPanel/TopBar");
        if (!topBar) { Debug.LogError("[Phase3] TopBar not found!"); return; }

        // 全宽, 顶部对齐, 高 340px
        SetRT(topBar, 0f, 1f, 1f, 1f, 0.5f, 1f, 0f, 0f, 0f, 340f);

        // 深蓝背景
        var img = topBar.GetComponent<Image>();
        if (img == null) img = topBar.AddComponent<Image>();
        img.color = new Color(0.043f, 0.137f, 0.298f, 0.93f); // #0B234C 深蓝
        img.raycastTarget = false;
        EditorUtility.SetDirty(topBar);

        Debug.Log("[Phase3] TopBar 容器 ✓ (全宽 340px 顶部)");
    }

    // ════════════════════════════════════════════════════════
    // Phase 3B: 力量条 (y=-100 ~ y=-170, 中心 y=-135)
    // ════════════════════════════════════════════════════════
    static void SetupForceBar()
    {
        // ── TopBarBg: 力量条背景全宽 ──
        var topBarBg = Find("Canvas/GameUIPanel/TopBar/TopBarBg");
        if (topBarBg)
        {
            // anchor stretch 全宽, 高 70px, 顶部偏移 100px
            SetRT(topBarBg, 0f, 1f, 1f, 1f, 0.5f, 1f, 0f, -100f, 0f, 70f);
            var img = topBarBg.GetComponent<Image>();
            if (img) img.color = new Color(0.016f, 0.055f, 0.133f, 0.97f); // 极深蓝
            EditorUtility.SetDirty(topBarBg);
        }

        // ── ProgressBarContainer: 固定宽 840px, 铺满 TopBarBg 高度 ──
        // 固定宽度让 TopBarUI.cs 的 sizeDelta.x 计算正确
        var pbc = Find("Canvas/GameUIPanel/TopBar/TopBarBg/ProgressBarContainer");
        if (pbc)
        {
            SetRT(pbc, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0f, 0f, 840f, 70f);
            EditorUtility.SetDirty(pbc);
        }

        // ── BarLeft 冰蓝 (左队) ──
        ColorBar("Canvas/GameUIPanel/TopBar/TopBarBg/ProgressBarContainer/BarLeft",
                 new Color(0.231f, 0.392f, 0.918f, 1f)); // #3B64EA

        // ── BarRight 冰红 (右队) ──
        ColorBar("Canvas/GameUIPanel/TopBar/TopBarBg/ProgressBarContainer/BarRight",
                 new Color(0.937f, 0.267f, 0.267f, 1f)); // #EF4444

        // ── BarDivider 白色细线 ──
        ColorBar("Canvas/GameUIPanel/TopBar/TopBarBg/ProgressBarContainer/BarDivider",
                 Color.white);

        // ── PosIndicator 球图标, 悬浮在进度条上方 ──
        var posInd = Find("Canvas/GameUIPanel/TopBar/TopBarBg/PosIndicator");
        if (posInd)
        {
            SetRT(posInd, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0f, 8f, 44f, 44f);
            var img = posInd.GetComponent<Image>();
            if (img) img.color = Color.white;
            EditorUtility.SetDirty(posInd);
        }

        // ── EndMarkers 端点距离文字 ──
        var leftMark = Find("Canvas/GameUIPanel/TopBar/TopBarBg/LeftEndMarker");
        if (leftMark)
        {
            SetRT(leftMark, 0f, 0.5f, 0f, 0.5f, 0f, 0.5f, 4f, 0f, 90f, 22f);
            StyleTMP(leftMark, 16f, new Color(0.7f, 0.88f, 1f), TextAlignmentOptions.Left);
            EditorUtility.SetDirty(leftMark);
        }
        var rightMark = Find("Canvas/GameUIPanel/TopBar/TopBarBg/RightEndMarker");
        if (rightMark)
        {
            SetRT(rightMark, 1f, 0.5f, 1f, 0.5f, 1f, 0.5f, -4f, 0f, 90f, 22f);
            StyleTMP(rightMark, 16f, new Color(0.7f, 0.88f, 1f), TextAlignmentOptions.Right);
            EditorUtility.SetDirty(rightMark);
        }
        var centerMark = Find("Canvas/GameUIPanel/TopBar/TopBarBg/CenterMarker");
        if (centerMark)
        {
            // 中心标记隐藏 (小字, 不影响布局)
            centerMark.SetActive(false);
            EditorUtility.SetDirty(centerMark);
        }

        // ── LeftForceText: 力量条左侧数字 ──
        var leftForce = Find("Canvas/GameUIPanel/TopBar/LeftForceText");
        if (leftForce)
        {
            // 贴TopBar左边缘, 与力量条同行
            SetRT(leftForce, 0f, 1f, 0f, 1f, 0f, 0.5f, 8f, -135f, 112f, 70f);
            StyleTMP(leftForce, 26f, new Color(0.988f, 0.827f, 0.302f), TextAlignmentOptions.Left);
            EditorUtility.SetDirty(leftForce);
        }

        // ── RightForceText: 力量条右侧数字 ──
        var rightForce = Find("Canvas/GameUIPanel/TopBar/RightForceText");
        if (rightForce)
        {
            SetRT(rightForce, 1f, 1f, 1f, 1f, 1f, 0.5f, -8f, -135f, 112f, 70f);
            StyleTMP(rightForce, 26f, new Color(0.988f, 0.827f, 0.302f), TextAlignmentOptions.Right);
            EditorUtility.SetDirty(rightForce);
        }

        Debug.Log("[Phase3] Force bar ✓");
    }

    // ════════════════════════════════════════════════════════
    // Phase 3C: 计时器 + 积分池 (y=-220 ~ y=-280, 中心 y=-250)
    // ════════════════════════════════════════════════════════
    static void SetupTimerAndScore()
    {
        // TimerBg
        var timerBg = Find("Canvas/GameUIPanel/TopBar/TimerBg");
        if (timerBg)
        {
            SetRT(timerBg, 0.5f, 1f, 0.5f, 1f, 0.5f, 0.5f, -210f, -250f, 270f, 58f);
            var img = timerBg.GetComponent<Image>();
            if (img) img.color = new Color(0.02f, 0.08f, 0.20f, 0.9f);
            EditorUtility.SetDirty(timerBg);
        }

        // TimerText
        var timerTxt = Find("Canvas/GameUIPanel/TopBar/TimerText");
        if (timerTxt)
        {
            SetRT(timerTxt, 0.5f, 1f, 0.5f, 1f, 0.5f, 0.5f, -210f, -250f, 270f, 58f);
            StyleTMP(timerTxt, 36f, Color.white, TextAlignmentOptions.Center);
            EditorUtility.SetDirty(timerTxt);
        }

        // ScorePoolBg
        var scoreBg = Find("Canvas/GameUIPanel/TopBar/ScorePoolBg");
        if (scoreBg)
        {
            SetRT(scoreBg, 0.5f, 1f, 0.5f, 1f, 0.5f, 0.5f, 210f, -250f, 270f, 58f);
            var img = scoreBg.GetComponent<Image>();
            if (img) img.color = new Color(0.02f, 0.08f, 0.20f, 0.9f);
            EditorUtility.SetDirty(scoreBg);
        }

        // ScorePoolText
        var scoreTxt = Find("Canvas/GameUIPanel/TopBar/ScorePoolText");
        if (scoreTxt)
        {
            SetRT(scoreTxt, 0.5f, 1f, 0.5f, 1f, 0.5f, 0.5f, 210f, -250f, 270f, 58f);
            StyleTMP(scoreTxt, 22f, new Color(0.988f, 0.827f, 0.302f), TextAlignmentOptions.Center);
            EditorUtility.SetDirty(scoreTxt);
        }

        Debug.Log("[Phase3] Timer/Score ✓");
    }

    // ════════════════════════════════════════════════════════
    // Phase 3D: 连胜/队伍标志 (顶部 0-100px)
    // ════════════════════════════════════════════════════════
    static void SetupWinStreaks()
    {
        // WinStreakLeftBg — 顶左, 蓝色, 队伍名+连胜池
        var wslBg = Find("Canvas/GameUIPanel/WinStreakLeftBg");
        if (wslBg)
        {
            SetRT(wslBg, 0f, 1f, 0f, 1f, 0f, 1f, 0f, 0f, 390f, 100f);
            var img = wslBg.GetComponent<Image>();
            if (img) img.color = new Color(0.090f, 0.216f, 0.557f, 0.92f); // 冰蓝队伍色
            EditorUtility.SetDirty(wslBg);
        }

        var wsl = Find("Canvas/GameUIPanel/WinStreakLeft");
        if (wsl)
        {
            SetRT(wsl, 0f, 1f, 0f, 1f, 0f, 1f, 0f, 0f, 390f, 100f);
            StyleTMP(wsl, 26f, Color.white, TextAlignmentOptions.Center);
            EditorUtility.SetDirty(wsl);
        }

        // WinStreakRightBg — 顶右, 红色
        var wsrBg = Find("Canvas/GameUIPanel/WinStreakRightBg");
        if (wsrBg)
        {
            SetRT(wsrBg, 1f, 1f, 1f, 1f, 1f, 1f, 0f, 0f, 390f, 100f);
            var img = wsrBg.GetComponent<Image>();
            if (img) img.color = new Color(0.557f, 0.090f, 0.090f, 0.92f); // 冰红队伍色
            EditorUtility.SetDirty(wsrBg);
        }

        var wsr = Find("Canvas/GameUIPanel/WinStreakRight");
        if (wsr)
        {
            SetRT(wsr, 1f, 1f, 1f, 1f, 1f, 1f, 0f, 0f, 390f, 100f);
            StyleTMP(wsr, 26f, Color.white, TextAlignmentOptions.Center);
            EditorUtility.SetDirty(wsr);
        }

        Debug.Log("[Phase3] WinStreaks ✓");
    }

    // ════════════════════════════════════════════════════════
    // Phase 3E: 提示文字 (TopBar 下方, y=-315)
    // ════════════════════════════════════════════════════════
    static void SetupHintText()
    {
        var hint = Find("Canvas/GameUIPanel/HintText");
        if (!hint) return;
        SetRT(hint, 0.5f, 1f, 0.5f, 1f, 0.5f, 0.5f, 0f, -315f, 740f, 40f);
        StyleTMP(hint, 24f, Color.white, TextAlignmentOptions.Center);
        EditorUtility.SetDirty(hint);
        Debug.Log("[Phase3] HintText ✓");
    }

    // ════════════════════════════════════════════════════════
    // Phase 3F: 玩家列表侧栏 (两侧, y=-360 起, 高 380px)
    // ════════════════════════════════════════════════════════
    static void SetupPlayerLists()
    {
        // LeftPlayerList — 屏幕左侧
        var lpl = Find("Canvas/GameUIPanel/LeftPlayerList");
        if (lpl)
        {
            SetRT(lpl, 0f, 1f, 0f, 1f, 0f, 1f, 0f, -365f, 205f, 380f);
            EnsureImageBg(lpl, new Color(0.043f, 0.137f, 0.298f, 0.80f));
            EditorUtility.SetDirty(lpl);
        }

        // RightPlayerList — 屏幕右侧
        var rpl = Find("Canvas/GameUIPanel/RightPlayerList");
        if (rpl)
        {
            SetRT(rpl, 1f, 1f, 1f, 1f, 1f, 1f, 0f, -365f, 205f, 380f);
            EnsureImageBg(rpl, new Color(0.043f, 0.137f, 0.298f, 0.80f));
            EditorUtility.SetDirty(rpl);
        }

        Debug.Log("[Phase3] PlayerLists ✓");
    }

    // ════════════════════════════════════════════════════════
    // Phase 3G: 按钮 (TopBar 底部右侧)
    // ════════════════════════════════════════════════════════
    static void SetupButtons()
    {
        // BtnEnd (公告按钮) — TopBar 底左
        var btnEnd = Find("Canvas/GameUIPanel/BtnEnd");
        if (btnEnd)
        {
            SetRT(btnEnd, 0f, 1f, 0f, 1f, 0f, 1f, 8f, -302f, 100f, 34f);
            EditorUtility.SetDirty(btnEnd);
        }

        // BtnSettings — TopBar 底右
        var btnSet = Find("Canvas/GameUIPanel/BtnSettings");
        if (btnSet)
        {
            SetRT(btnSet, 1f, 1f, 1f, 1f, 1f, 1f, -8f, -302f, 100f, 34f);
            EditorUtility.SetDirty(btnSet);
        }

        // BtnSticker — 暂时隐藏
        var btnStk = Find("Canvas/GameUIPanel/BtnSticker");
        if (btnStk) { btnStk.SetActive(false); EditorUtility.SetDirty(btnStk); }

        Debug.Log("[Phase3] Buttons ✓");
    }

    // ════════════════════════════════════════════════════════
    // Phase 3H: 礼物通知 / 入场通知 (屏幕右侧中部)
    // ════════════════════════════════════════════════════════
    static void SetupGiftNotification()
    {
        // GiftNotification — 右侧浮动
        var giftNotif = Find("Canvas/GameUIPanel/GiftNotification");
        if (giftNotif)
        {
            SetRT(giftNotif, 1f, 0.5f, 1f, 0.5f, 1f, 0.5f, -8f, 100f, 320f, 120f);
            EditorUtility.SetDirty(giftNotif);
        }

        // JoinNotification — 右侧浮动, 稍低
        var joinNotif = Find("Canvas/GameUIPanel/JoinNotification");
        if (joinNotif)
        {
            SetRT(joinNotif, 1f, 0.5f, 1f, 0.5f, 1f, 0.5f, -8f, -40f, 280f, 80f);
            EditorUtility.SetDirty(joinNotif);
        }

        // VIPAnnouncement — 屏幕中部偏上
        var vip = Find("Canvas/GameUIPanel/VIPAnnouncement");
        if (vip)
        {
            SetRT(vip, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0f, 100f, 900f, 120f);
            EditorUtility.SetDirty(vip);
        }

        // GiftAnimation — 屏幕中部
        var giftAnim = Find("Canvas/GameUIPanel/GiftAnimation");
        if (giftAnim)
        {
            SetRT(giftAnim, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0f, 0f, 500f, 500f);
            EditorUtility.SetDirty(giftAnim);
        }

        // GiftInfoPanel — 左下角
        var giftInfo = Find("Canvas/GameUIPanel/GiftInfoPanel");
        if (giftInfo)
        {
            SetRT(giftInfo, 0f, 0f, 0f, 0f, 0f, 0f, 8f, 8f, 340f, 120f);
            EditorUtility.SetDirty(giftInfo);
        }

        Debug.Log("[Phase3] Notifications ✓");
    }

    // ════════════════════════════════════════════════════════
    // Helpers
    // ════════════════════════════════════════════════════════

    /// <summary>设置 RectTransform 的 anchor/pivot/position/size</summary>
    static void SetRT(GameObject go,
        float ancMinX, float ancMinY, float ancMaxX, float ancMaxY,
        float pivX,    float pivY,
        float posX,    float posY,
        float sizeX,   float sizeY)
    {
        if (!go) return;
        var rt = go.GetComponent<RectTransform>();
        if (!rt) return;
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

    static void ColorBar(string path, Color color)
    {
        var go = Find(path);
        if (!go) return;
        var img = go.GetComponent<Image>();
        if (img) img.color = color;
        EditorUtility.SetDirty(go);
    }

    static void EnsureImageBg(GameObject go, Color color)
    {
        var img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    /// <summary>按路径查找 GameObject（支持 inactive）</summary>
    static GameObject Find(string path)
    {
        var go = GameObject.Find(path);
        if (go) return go;

        // 搜索 inactive 对象
        var parts   = path.Split('/');
        var lastName = parts[parts.Length - 1];
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t.name != lastName) continue;
            if (BuildPath(t) == path) return t.gameObject;
        }
        Debug.LogWarning($"[Phase3] NOT FOUND: {path}");
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
