using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// 修补结算界面：
///   1. 添加极地极光背景图（不破坏现有结构）
///   2. 在 ScreenC 创建 Top3Slot_1/2/3 并绑定 _top3Slots
///   3. 保存场景
/// Tools → DrscfZ → Fix Settlement Missing
/// </summary>
public static class FixSettlementMissing
{
    [MenuItem("Tools/DrscfZ/Fix Settlement Missing")]
    public static void Execute()
    {
        // GameObject.Find 找不到 inactive 对象，从 Canvas 子节点遍历
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("[FixSettlement] 找不到 Canvas"); return; }
        var panelTransform = canvas.transform.Find("SurvivalSettlementPanel");
        if (panelTransform == null) { Debug.LogError("[FixSettlement] Canvas 下找不到 SurvivalSettlementPanel"); return; }
        var panelGO = panelTransform.gameObject;

        var ui = panelGO.GetComponent<DrscfZ.UI.SurvivalSettlementUI>();
        if (ui == null) { Debug.LogError("[FixSettlement] 找不到 SurvivalSettlementUI 组件"); return; }

        // ── 1. 极光背景图 ────────────────────────────────────────────────────
        AddBackground(panelGO);

        // ── 2. Top3 槽位（在 ScreenC 内创建并绑定）──────────────────────────
        var screenCField = typeof(DrscfZ.UI.SurvivalSettlementUI).GetField(
            "_screenC", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var screenC = screenCField?.GetValue(ui) as GameObject;
        if (screenC == null) { Debug.LogError("[FixSettlement] _screenC 未绑定，请先运行 Setup Settlement UI"); return; }

        var slots = EnsureTop3Slots(screenC);

        // ── 3. 绑定 _top3Slots ───────────────────────────────────────────────
        var so = new SerializedObject(ui);
        var slotsProp = so.FindProperty("_top3Slots");
        slotsProp.arraySize = 3;
        for (int i = 0; i < 3; i++)
            slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
        so.ApplyModifiedProperties();
        Debug.Log("[FixSettlement] _top3Slots[0/1/2] 绑定完成");

        // ── 4. 保存场景 ──────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(panelGO.scene);
        EditorSceneManager.SaveScene(panelGO.scene);
        Debug.Log("[FixSettlement] 完成！场景已保存");
    }

    // ─── 添加极光背景（面板第一个子节点 BG + 遮罩 Overlay）─────────────────
    private static void AddBackground(GameObject panelGO)
    {
        // 已存在则直接更新贴图
        var existingBG = panelGO.transform.Find("BG");
        if (existingBG != null)
        {
            var img = existingBG.GetComponent<Image>();
            if (img != null)
            {
                var sprite = LoadSprite("Assets/Art/UI/Settlement/settlement_bg_aurora.jpg");
                if (sprite != null)
                {
                    img.sprite = sprite;
                    img.color = Color.white;
                    img.type = Image.Type.Simple;
                    img.preserveAspect = false;
                    Debug.Log("[FixSettlement] BG 已存在，更新为极光背景贴图");
                }
                else Debug.LogWarning("[FixSettlement] 未找到 settlement_bg_aurora.jpg");
            }
            return;
        }

        // 创建 BG
        var bg = new GameObject("BG");
        bg.transform.SetParent(panelGO.transform, false);
        bg.transform.SetSiblingIndex(0);
        var bgRT = bg.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.raycastTarget = false;

        var aurora = LoadSprite("Assets/Art/UI/Settlement/settlement_bg_aurora.jpg");
        if (aurora != null)
        {
            bgImg.sprite = aurora;
            bgImg.color = Color.white;
            bgImg.type = Image.Type.Simple;
            bgImg.preserveAspect = false;
            Debug.Log("[FixSettlement] 极光背景图已添加");
        }
        else
        {
            // fallback: 深蓝极地纯色
            bgImg.color = new Color(0.04f, 0.07f, 0.14f, 1f);
            Debug.LogWarning("[FixSettlement] 未找到极光贴图，使用深蓝纯色背景");
        }

        // Overlay 半透明遮罩（提升文字可读性）
        var overlay = new GameObject("Overlay");
        overlay.transform.SetParent(panelGO.transform, false);
        overlay.transform.SetSiblingIndex(1);
        var oRT = overlay.AddComponent<RectTransform>();
        oRT.anchorMin = Vector2.zero;
        oRT.anchorMax = Vector2.one;
        oRT.offsetMin = oRT.offsetMax = Vector2.zero;
        var oImg = overlay.AddComponent<Image>();
        oImg.color = new Color(0f, 0f, 0f, 0.5f);
        oImg.raycastTarget = false;
        Debug.Log("[FixSettlement] 半透明遮罩已添加");
    }

    // ─── 确保 ScreenC 内有 Top3Slot_1/2/3（结构：NameText + ScoreText）────────
    private static GameObject[] EnsureTop3Slots(GameObject screenC)
    {
        // 名次配置
        float[] ancBot = { 0.62f, 0.42f, 0.22f };
        float[] ancTop = { 0.82f, 0.62f, 0.42f };
        Color[] rankColors =
        {
            new Color(1.0f, 0.85f, 0.10f),  // 金
            new Color(0.80f, 0.80f, 0.80f), // 银
            new Color(0.78f, 0.50f, 0.20f), // 铜
        };
        string[] rankTitles = { "冠军", "亚军", "季军" };

        var slots = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            string slotName = $"Top3Slot_{i + 1}";
            var existing = screenC.transform.Find(slotName)?.gameObject;
            if (existing != null)
            {
                slots[i] = existing;
                Debug.Log($"[FixSettlement] {slotName} 已存在，复用");
                continue;
            }

            // 容器
            var slot = new GameObject(slotName);
            slot.transform.SetParent(screenC.transform, false);
            var rt = slot.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.06f, ancBot[i]);
            rt.anchorMax = new Vector2(0.94f, ancTop[i]);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            // 半透明背景板
            var slotImg = slot.AddComponent<Image>();
            slotImg.color = new Color(0.06f, 0.10f, 0.22f, 0.85f);
            slotImg.raycastTarget = false;

            // 排名图标（左）
            AddTMP(slot.transform, "RankBadge", $"#{i+1} {rankTitles[i]}",
                ancMin: new Vector2(0f, 0.1f), ancMax: new Vector2(0.22f, 0.9f),
                size: 34f, color: rankColors[i], bold: true, align: TextAlignmentOptions.Center);

            // 分隔线（竖线）
            var sep = new GameObject("Sep");
            sep.transform.SetParent(slot.transform, false);
            var sepRT = sep.AddComponent<RectTransform>();
            sepRT.anchorMin = new Vector2(0.22f, 0.15f);
            sepRT.anchorMax = new Vector2(0.23f, 0.85f);
            sepRT.offsetMin = sepRT.offsetMax = Vector2.zero;
            var sepImg = sep.AddComponent<Image>();
            sepImg.color = new Color(1f, 1f, 1f, 0.2f);
            sepImg.raycastTarget = false;

            // 玩家名（中）— 运行时由 NameText 查找
            AddTMP(slot.transform, "NameText", "玩家名",
                ancMin: new Vector2(0.24f, 0.1f), ancMax: new Vector2(0.70f, 0.9f),
                size: 36f, color: Color.white, bold: false, align: TextAlignmentOptions.Left);

            // 积分（右）— 运行时由 ScoreText 查找
            AddTMP(slot.transform, "ScoreText", "贡献值: 0",
                ancMin: new Vector2(0.68f, 0.1f), ancMax: new Vector2(1f, 0.9f),
                size: 30f, color: new Color(0.65f, 0.90f, 1f), bold: false,
                align: TextAlignmentOptions.Right);

            slots[i] = slot;
            Debug.Log($"[FixSettlement] {slotName} 创建完成");
        }
        return slots;
    }

    // ─── 工具：创建 TMP ───────────────────────────────────────────────────────
    private static void AddTMP(Transform parent, string name, string text,
        Vector2 ancMin, Vector2 ancMax,
        float size, Color color, bool bold, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = ancMin;
        rt.anchorMax = ancMax;
        rt.offsetMin = new Vector2(6f, 0f);
        rt.offsetMax = new Vector2(-6f, 0f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;

        // 用 SerializedObject 写颜色，避免 faceColor 默认白色覆盖
        var so = new SerializedObject(tmp);
        so.FindProperty("m_fontColor").colorValue = color;
        so.FindProperty("m_fontColor32").colorValue = color;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ─── 工具：加载 Sprite 并确保导入类型为 Sprite ────────────────────────────
    private static Sprite LoadSprite(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            Debug.Log($"[FixSettlement] 已将 {assetPath} 导入类型改为 Sprite");
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }
}
