#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 极地生存法则 — Phase 4: 弹幕区 + 礼物图标行
/// 执行菜单: DrscfZ/Phase 4 - Rebuild Barrage Panel
///
/// 竖屏布局（从下往上）:
///   y=   0 ~  740  BarragePanel  弹幕滚动日志 (740px)
///   y= 740 ~  742  BarrageDivider 分割线
///   y= 740 ~  860  GiftIconBar   6个礼物档位 (120px)
///   y= 860 ~ 1580  游戏视口区   (720px)
///   y=1580 ~ 1920  TopBar HUD    (340px)
/// </summary>
public class RebuildPortraitBarrage
{
    // 礼物档位颜色 (tier1-6)
    static readonly Color[] TierColors = {
        new Color(0.55f, 0.60f, 0.68f, 1f), // tier1 银灰  仙女棒
        new Color(0.20f, 0.72f, 0.38f, 1f), // tier2 翠绿  能力药丸
        new Color(0.22f, 0.52f, 0.92f, 1f), // tier3 冰蓝  甜甜圈
        new Color(0.58f, 0.22f, 0.92f, 1f), // tier4 紫    能量电池
        new Color(0.97f, 0.70f, 0.08f, 1f), // tier5 金    爱的爆炸
        new Color(0.92f, 0.22f, 0.22f, 1f), // tier6 红    神秘空投
    };

    static readonly string[] TierLabels = {
        "仙女棒\n0.1抖", "药丸\n10抖", "甜圈\n52抖",
        "电池\n99抖",    "爆炸\n199抖","空投\n520抖"
    };

    [MenuItem("DrscfZ/Phase 4 - Rebuild Barrage Panel", false, 120)]
    public static void Execute()
    {
        Debug.Log("[Phase4] ===== 开始 Phase 4: 弹幕区 + 礼物图标行 =====");

        var gameUIPanel = FindGO("Canvas/GameUIPanel");
        if (!gameUIPanel) { Debug.LogError("[Phase4] GameUIPanel not found!"); return; }

        // 清理旧的同名对象（先收集再销毁，避免迭代中修改）
        Cleanup(new[] { "GiftIconBar", "BarrageDivider", "BarragePanel" });

        CreateGiftIconBar(gameUIPanel.transform);
        CreateDivider(gameUIPanel.transform);
        CreateBarragePanel(gameUIPanel.transform);

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

        Debug.Log("[Phase4] ===== 完成! 场景已保存 =====");
    }

    // ═══════════════════════════════════════
    // GiftIconBar  y=740-860, center y=800
    // ═══════════════════════════════════════
    static void CreateGiftIconBar(Transform parent)
    {
        var bar = MakeGO("GiftIconBar", parent);
        var rt  = bar.AddComponent<RectTransform>();
        // anchor 底部中心点, center at y=800 from bottom
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 800f);
        rt.sizeDelta        = new Vector2(1080f, 120f);

        var bg = bar.AddComponent<Image>();
        bg.color = new Color(0.024f, 0.071f, 0.173f, 0.97f); // 深靛蓝
        bg.raycastTarget = false;

        // 顶部分隔高光线
        var topLine = MakeGO("TopHighlight", bar.transform);
        var tlRt = topLine.AddComponent<RectTransform>();
        tlRt.anchorMin = new Vector2(0f, 1f); tlRt.anchorMax = new Vector2(1f, 1f);
        tlRt.pivot = new Vector2(0.5f, 1f); tlRt.anchoredPosition = Vector2.zero;
        tlRt.sizeDelta = new Vector2(0f, 2f);
        topLine.AddComponent<Image>().color = new Color(0.4f, 0.7f, 1.0f, 0.4f);

        // HorizontalLayoutGroup 使6个图标均等分布
        var hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(16, 16, 10, 10);
        hlg.spacing = 10f;
        hlg.childAlignment        = TextAnchor.MiddleCenter;
        hlg.childControlWidth     = true;
        hlg.childControlHeight    = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        for (int i = 0; i < 6; i++)
            CreateTierIcon(bar.transform, i);

        EditorUtility.SetDirty(bar);
        Debug.Log("[Phase4] GiftIconBar ✓ (6档位)");
    }

    static void CreateTierIcon(Transform parent, int tier)
    {
        var icon = MakeGO($"Tier{tier + 1}Icon", parent);
        icon.AddComponent<RectTransform>();

        // 图标背景
        var bg = icon.AddComponent<Image>();
        bg.color = TierColors[tier];

        // 档位序号圆点 (左上角)
        var badge = MakeGO("Badge", icon.transform);
        var bRt = badge.AddComponent<RectTransform>();
        bRt.anchorMin = bRt.anchorMax = new Vector2(0f, 1f);
        bRt.pivot = new Vector2(0f, 1f);
        bRt.anchoredPosition = new Vector2(3f, -3f);
        bRt.sizeDelta = new Vector2(20f, 20f);
        var bImg = badge.AddComponent<Image>();
        bImg.color = new Color(0f, 0f, 0f, 0.45f);

        var bTxt = MakeGO("Num", badge.transform);
        var bTxtRt = bTxt.AddComponent<RectTransform>();
        bTxtRt.anchorMin = Vector2.zero; bTxtRt.anchorMax = Vector2.one;
        bTxtRt.sizeDelta = Vector2.zero; bTxtRt.anchoredPosition = Vector2.zero;
        var bTmp = bTxt.AddComponent<TextMeshProUGUI>();
        bTmp.text = $"T{tier + 1}";
        bTmp.fontSize = 10f;
        bTmp.color = Color.white;
        bTmp.alignment = TextAlignmentOptions.Center;
        bTmp.fontStyle = FontStyles.Bold;

        // 档位名称 + 价格
        var label = MakeGO("Label", icon.transform);
        var lRt = label.AddComponent<RectTransform>();
        lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
        lRt.sizeDelta = Vector2.zero; lRt.anchoredPosition = Vector2.zero;
        var lTmp = label.AddComponent<TextMeshProUGUI>();
        lTmp.text = TierLabels[tier];
        lTmp.fontSize = 16f;
        lTmp.color = Color.white;
        lTmp.alignment = TextAlignmentOptions.Center;
        lTmp.fontStyle = FontStyles.Bold;
    }

    // ═══════════════════════════════════════
    // BarrageDivider  y=740~742
    // ═══════════════════════════════════════
    static void CreateDivider(Transform parent)
    {
        var div  = MakeGO("BarrageDivider", parent);
        var rt   = div.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 0f);
        rt.anchorMax        = new Vector2(1f, 0f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 741f);
        rt.sizeDelta        = new Vector2(0f, 2f);

        div.AddComponent<Image>().color = new Color(0.35f, 0.60f, 0.90f, 0.45f);
        EditorUtility.SetDirty(div);
        Debug.Log("[Phase4] BarrageDivider ✓");
    }

    // ═══════════════════════════════════════
    // BarragePanel  y=0-740, ScrollView弹幕日志
    // ═══════════════════════════════════════
    static void CreateBarragePanel(Transform parent)
    {
        // ── 外层面板 ──
        var panel = MakeGO("BarragePanel", parent);
        var panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin        = new Vector2(0f, 0f);
        panelRt.anchorMax        = new Vector2(1f, 0f);
        panelRt.pivot            = new Vector2(0.5f, 0f);
        panelRt.anchoredPosition = new Vector2(0f, 0f);
        panelRt.sizeDelta        = new Vector2(0f, 740f);

        var panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.016f, 0.047f, 0.118f, 0.88f); // 深夜蓝
        panelBg.raycastTarget = false;

        // ── 顶部标题栏 ──
        var titleBar = MakeGO("TitleBar", panel.transform);
        var tbRt = titleBar.AddComponent<RectTransform>();
        tbRt.anchorMin = new Vector2(0f, 1f); tbRt.anchorMax = new Vector2(1f, 1f);
        tbRt.pivot = new Vector2(0.5f, 1f);
        tbRt.anchoredPosition = Vector2.zero; tbRt.sizeDelta = new Vector2(0f, 36f);
        titleBar.AddComponent<Image>().color = new Color(0.024f, 0.071f, 0.173f, 0.97f);

        var titleTxt = MakeGO("TitleText", titleBar.transform);
        var ttRt = titleTxt.AddComponent<RectTransform>();
        ttRt.anchorMin = Vector2.zero; ttRt.anchorMax = Vector2.one;
        ttRt.sizeDelta = Vector2.zero; ttRt.anchoredPosition = Vector2.zero;
        var ttTmp = titleTxt.AddComponent<TextMeshProUGUI>();
        ttTmp.text = "💬 弹幕动态";
        ttTmp.fontSize = 20f;
        ttTmp.color = new Color(0.7f, 0.87f, 1.0f);
        ttTmp.alignment = TextAlignmentOptions.Left;
        // 左侧内边距
        ttRt.anchoredPosition = new Vector2(12f, 0f);
        ttRt.sizeDelta = new Vector2(-12f, 0f);

        // ── ScrollView (占面板除标题外的全部高度) ──
        var scrollObj = MakeGO("ScrollView", panel.transform);
        var scrollRt  = scrollObj.AddComponent<RectTransform>();
        scrollRt.anchorMin        = new Vector2(0f, 0f);
        scrollRt.anchorMax        = new Vector2(1f, 1f);
        scrollRt.pivot            = new Vector2(0.5f, 0.5f);
        scrollRt.anchoredPosition = new Vector2(0f, -18f); // 让出标题栏一半
        scrollRt.sizeDelta        = new Vector2(0f, -36f); // 减去标题栏高度

        var scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.horizontal        = false;
        scroll.vertical          = true;
        scroll.movementType      = ScrollRect.MovementType.Clamped;
        scroll.inertia           = true;
        scroll.decelerationRate  = 0.12f;
        scroll.scrollSensitivity = 25f;

        // ── Viewport ──
        var viewport = MakeGO("Viewport", scrollObj.transform);
        var vpRt = viewport.AddComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
        vpRt.sizeDelta = Vector2.zero; vpRt.anchoredPosition = Vector2.zero;
        var vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(0f, 0f, 0f, 0.01f); // mask需要Image组件
        vpImg.raycastTarget = false;
        var mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // ── Content (垂直布局，自动伸缩) ──
        var content = MakeGO("BarrageContent", viewport.transform);
        var contentRt = content.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot     = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 0f);

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.padding             = new RectOffset(10, 10, 6, 6);
        vlg.spacing             = 3f;
        vlg.childAlignment      = TextAnchor.UpperLeft;
        vlg.childControlWidth   = true;
        vlg.childControlHeight  = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // 关联 ScrollRect 引用
        scroll.viewport = vpRt;
        scroll.content  = contentRt;
        scroll.verticalNormalizedPosition = 0f; // 初始显示底部

        // ── 预置欢迎/说明消息 ──
        AddWelcomeMessages(content.transform);

        // Mark dirty
        foreach (var go in new[] { panel, scrollObj, viewport, content })
            EditorUtility.SetDirty(go);

        Debug.Log("[Phase4] BarragePanel ✓ (ScrollView 740px + 弹幕内容区)");
    }

    static void AddWelcomeMessages(Transform contentParent)
    {
        var msgs = new (string text, Color color)[]
        {
            ("❄️ <color=#FCD34D>极地生存法则</color> 等待开始...",             new Color(0.99f, 0.84f, 0.30f)),
            ("📢 发送 <color=#60A5FA>1</color> 加入冰蓝队 | 发送 <color=#F87171>2</color> 加入冰红队",
                                                                               new Color(0.72f, 0.90f, 1.00f)),
            ("🎁 送礼物推动雪球，帮阵营赢得胜利！",                             new Color(0.72f, 0.90f, 1.00f)),
            ("⚔️ 雪球碰到边界即判定获胜，加油！",                               new Color(0.85f, 0.93f, 1.00f)),
            ("🏆 最高贡献者将显示在两侧排行榜中",                               new Color(0.85f, 0.93f, 1.00f)),
        };

        foreach (var (text, color) in msgs)
            CreateMsgRow(contentParent, text, color);
    }

    static void CreateMsgRow(Transform parent, string text, Color color)
    {
        var row = MakeGO("MsgRow", parent);
        row.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 34f);

        var tmp = row.AddComponent<TextMeshProUGUI>();
        tmp.text             = text;
        tmp.fontSize         = 21f;
        tmp.color            = color;
        tmp.alignment        = TextAlignmentOptions.Left;
        tmp.enableWordWrapping = false;
        tmp.overflowMode     = TextOverflowModes.Ellipsis;
        tmp.richText         = true;
    }

    // ─────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────

    static GameObject MakeGO(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    static void Cleanup(string[] names)
    {
        var toDestroy = new List<GameObject>();
        var nameSet = new HashSet<string>(names);
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t != null && nameSet.Contains(t.name))
                toDestroy.Add(t.gameObject);
        }
        foreach (var go in toDestroy)
            if (go != null) Object.DestroyImmediate(go);
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
        Debug.LogWarning($"[Phase4] NOT FOUND: {path}");
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
