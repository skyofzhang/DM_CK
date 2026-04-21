#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// 重建极地生存游戏 UI（TopBar + BarragePanel + GiftIconBar）
/// 执行：DrscfZ/Rebuild Survival UI
/// </summary>
public class RebuildSurvivalUI
{
    [MenuItem("DrscfZ/Rebuild Survival UI", false, 220)]
    public static void Execute()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[RebuildUI] 未找到 Canvas！");
            return;
        }

        // ---- 1. 找到或创建 GameUIPanel ----
        var gameUIPanel = canvas.transform.Find("GameUIPanel")?.gameObject;
        if (gameUIPanel == null)
        {
            gameUIPanel = CreatePanel(canvas.transform, "GameUIPanel",
                new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
                Vector2.zero, new Vector2(1080, 1920));
        }

        // ---- 2. 重建 TopBar（状态栏 120px 顶部）----
        RebuildTopBar(gameUIPanel.transform);

        // ---- 3. 创建/重建 GiftIconBar（礼物档位图标行 120px）----
        RebuildGiftIconBar(gameUIPanel.transform);

        // ---- 4. 创建/重建 BarragePanel（弹幕流 640px 底部）----
        RebuildBarragePanel(gameUIPanel.transform);

        // ---- 5. 标记脏场景 ----
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[RebuildUI] ===== 极地生存UI重建完毕 =====");
        Debug.Log("[RebuildUI] TopBar(120px top) + GiftIconBar(120px above barrage) + BarragePanel(640px bottom)");
    }

    // ==================== TopBar ====================

    static void RebuildTopBar(Transform parent)
    {
        // 移除旧 TopBar，重新创建
        var old = parent.Find("TopBar");
        // 不删除旧的，只更新 RectTransform（保留脚本引用）

        GameObject topBar = old?.gameObject;
        if (topBar == null)
        {
            topBar = CreatePanel(parent, "TopBar",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0, -60), new Vector2(0, 120));
        }
        else
        {
            // 只更新 RectTransform
            var rt = topBar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(0, -60);
            rt.sizeDelta = new Vector2(0, 120);
        }

        // 背景色：深蓝半透明
        EnsureImage(topBar, new Color(0.04f, 0.18f, 0.35f, 0.92f));

        // ---- 阶段文字 ----
        var phaseGo = EnsureChild(topBar.transform, "PhaseText");
        SetRT(phaseGo, new Vector2(0.02f, 0f), new Vector2(0.35f, 1f), Vector2.zero, Vector2.zero);
        EnsureTMP(phaseGo, "第1天 · 白天", 28, TextAlignmentOptions.MidlineLeft,
            new Color(1f, 0.95f, 0.6f));

        // ---- 倒计时 ----
        var timerGo = EnsureChild(topBar.transform, "TimerText");
        SetRT(timerGo, new Vector2(0.38f, 0f), new Vector2(0.62f, 1f), Vector2.zero, Vector2.zero);
        EnsureTMP(timerGo, "03:00", 36, TextAlignmentOptions.Midline, Color.white);

        // ---- 资源行（下半部分用水平布局 5个图标）----
        var resRow = EnsureChild(topBar.transform, "ResourceRow");
        SetRT(resRow, new Vector2(0f, 0f), new Vector2(1f, 0.5f),
            new Vector2(0, 2), new Vector2(-10, 0));

        // 食物/煤炭/矿石/炉温/城门HP 5个图标
        CreateResourceIcon(resRow.transform, "FoodIcon",  "粮", "100", new Color(0.2f, 0.9f, 0.3f));
        CreateResourceIcon(resRow.transform, "CoalIcon",  "煤", "50",  new Color(0.7f, 0.7f, 0.7f));
        CreateResourceIcon(resRow.transform, "OreIcon",   "矿", "30",  new Color(0.6f, 0.85f, 1f));
        CreateResourceIcon(resRow.transform, "HeatIcon",  "热", "20°", new Color(1f, 0.5f, 0.1f));
        CreateResourceIcon(resRow.transform, "GateIcon",  "堡", "HP",  new Color(0.9f, 0.3f, 0.3f));

        // 水平布局
        var hlg = resRow.GetComponent<HorizontalLayoutGroup>() ?? resRow.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 8;
        hlg.padding = new RectOffset(8, 8, 2, 2);
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = false;

        Debug.Log("[RebuildUI] TopBar 重建完成");
    }

    static void CreateResourceIcon(Transform parent, string name, string iconText, string valueText, Color color)
    {
        var go = EnsureChild(parent, name);
        var hlg = go.GetComponent<HorizontalLayoutGroup>() ?? go.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 2;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.preferredWidth = 180;
        le.preferredHeight = 52;

        // 图标TMP
        var icon = EnsureChild(go.transform, "Icon");
        icon.GetComponent<RectTransform>().sizeDelta = new Vector2(36, 36);
        EnsureTMP(icon, iconText, 24, TextAlignmentOptions.Midline, color);

        // 数值TMP
        var val = EnsureChild(go.transform, "Value");
        val.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 36);
        EnsureTMP(val, valueText, 26, TextAlignmentOptions.MidlineLeft, Color.white);
    }

    // ==================== GiftIconBar ====================

    static void RebuildGiftIconBar(Transform parent)
    {
        var old = parent.Find("GiftIconBar");
        GameObject bar = old?.gameObject;
        // 布局：底部480px底部UI区 = BarragePanel(390px,y=0-390) + GiftIconBar(90px,y=390-480)
        // GiftIconBar: bottom=390, top=480 from canvas bottom → center=435, height=90
        if (bar == null)
        {
            bar = CreatePanel(parent, "GiftIconBar",
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0, 435), new Vector2(0, 90));
        }
        else
        {
            var rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(0, 435);
            rt.sizeDelta = new Vector2(0, 90);
        }

        EnsureImage(bar, new Color(0.06f, 0.14f, 0.28f, 0.9f));

        // 7个礼物档位图标
        Color[] tierColors = {
            new Color(0.8f, 0.8f, 0.8f),   // Tier1 灰白
            new Color(0.4f, 0.9f, 0.3f),   // Tier2 绿
            new Color(0.2f, 0.6f, 1f),     // Tier3 蓝
            new Color(0.7f, 0.2f, 1f),     // Tier4 紫
            new Color(1f, 0.6f, 0.1f),     // Tier5 橙
            new Color(1f, 0.2f, 0.2f),     // Tier6 红
            new Color(1f, 0.9f, 0.1f),     // Tier7 金
        };
        string[] tierNames = { "T1", "T2", "T3", "T4", "T5", "T6", "T7" };

        // 清理旧子对象（礼物图标）
        for (int i = bar.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(bar.transform.GetChild(i).gameObject);

        var hlg = bar.GetComponent<HorizontalLayoutGroup>() ?? bar.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 4;
        hlg.padding = new RectOffset(8, 8, 8, 8);
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        for (int i = 0; i < 7; i++)
        {
            var iconGo = new GameObject($"GiftTier{i+1}");
            iconGo.transform.SetParent(bar.transform, false);
            var bg = iconGo.AddComponent<Image>();
            bg.color = new Color(tierColors[i].r, tierColors[i].g, tierColors[i].b, 0.25f);

            var lbl = new GameObject("Label");
            lbl.transform.SetParent(iconGo.transform, false);
            var lrt = lbl.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var t = lbl.AddComponent<TextMeshProUGUI>();
            t.text = tierNames[i];
            t.fontSize = 20;
            t.alignment = TextAlignmentOptions.Midline;
            t.color = tierColors[i];
        }

        Debug.Log("[RebuildUI] GiftIconBar 重建完成（7档礼物占位）");
    }

    // ==================== BarragePanel ====================

    static void RebuildBarragePanel(Transform parent)
    {
        var old = parent.Find("BarragePanel");
        GameObject panel = old?.gameObject;
        // BarragePanel: bottom=0, top=390 from canvas bottom
        // pivot=(0.5,0.5): center=195, height=390
        if (panel == null)
        {
            panel = CreatePanel(parent, "BarragePanel",
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0, 195), new Vector2(0, 390));
        }
        else
        {
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 195);
            rt.sizeDelta = new Vector2(0, 390);
        }

        EnsureImage(panel, new Color(0.02f, 0.06f, 0.14f, 0.88f));

        // 创建 ScrollView
        var svGo = EnsureChild(panel.transform, "ScrollView");
        SetRT(svGo, Vector2.zero, Vector2.one, new Vector2(0,4), new Vector2(-4,-4));

        var sv = svGo.GetComponent<ScrollRect>() ?? svGo.AddComponent<ScrollRect>();
        sv.horizontal = false;
        sv.vertical = true;
        sv.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        // Viewport
        var vpGo = EnsureChild(svGo.transform, "Viewport");
        SetRT(vpGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var vm = vpGo.GetComponent<RectMask2D>() ?? vpGo.AddComponent<RectMask2D>();
        sv.viewport = vpGo.GetComponent<RectTransform>();

        // Content
        var cGo = EnsureChild(vpGo.transform, "Content");
        var cRT = cGo.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0f, 0f);
        cRT.anchorMax = new Vector2(1f, 0f);
        cRT.pivot     = new Vector2(0.5f, 0f);
        cRT.anchoredPosition = Vector2.zero;
        cRT.sizeDelta = new Vector2(0, 0);

        var vlg = cGo.GetComponent<VerticalLayoutGroup>() ?? cGo.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.LowerCenter;
        vlg.spacing = 4;
        vlg.padding = new RectOffset(8, 8, 4, 4);
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        var csf = cGo.GetComponent<ContentSizeFitter>() ?? cGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sv.content = cGo.GetComponent<RectTransform>();

        Debug.Log("[RebuildUI] BarragePanel 重建完成（ScrollView, 640px）");
    }

    // ==================== 工具方法 ====================

    static GameObject CreatePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing.gameObject;

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        return go;
    }

    static GameObject EnsureChild(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing.gameObject;
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static void SetRT(GameObject go, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
    }

    static Image EnsureImage(GameObject go, Color color)
    {
        var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static TextMeshProUGUI EnsureTMP(GameObject go, string text, float size,
        TextAlignmentOptions align, Color color)
    {
        var tmp = go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = color;
        tmp.raycastTarget = false;
        return tmp;
    }
}
#endif
