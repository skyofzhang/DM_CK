using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tools → DrscfZ → Rebuild Gift Icon Bar
/// 重建底部礼物说明栏：6个礼物卡片，每个卡片含礼物图片 + 名称 + 效果说明
/// </summary>
public class RebuildGiftIconBar
{
    // 6种礼物图标路径（相对 Assets）
    private static readonly string[] IconPaths = new[]
    {
        "Assets/Resources/GiftIcons/Gift_Icon_1.png",
        "Assets/Resources/GiftIcons/Gift_Icon_2.png",
        "Assets/Resources/GiftIcons/Gift_Icon_3.png",
        "Assets/Resources/GiftIcons/Gift_Icon_4.png",
        "Assets/Resources/GiftIcons/Gift_Icon_5.png",
        "Assets/Resources/GiftIcons/Gift_Icon_6.png",
    };

    private static readonly GiftData[] Gifts = new[]
    {
        new GiftData("仙女棒",  "T1", "效率永久+5%",       new Color(1.00f, 0.92f, 0.50f, 1f)),  // 淡金
        new GiftData("能力药丸","T2", "全员效率+50%",        new Color(0.55f, 0.78f, 1.00f, 1f)),  // 淡蓝
        new GiftData("甜甜圈",  "T3", "食物+100 城门+200",  new Color(0.82f, 0.58f, 1.00f, 1f)),  // 淡紫
        new GiftData("能量电池","T4", "炉温+30 效率+30%",   new Color(1.00f, 0.72f, 0.30f, 1f)),  // 淡橙
        new GiftData("爱的爆炸","T5", "AOE伤害 矿工全满",   new Color(1.00f, 0.50f, 0.55f, 1f)),  // 淡红
        new GiftData("神秘空投","T6", "超级补给 城门+300",  new Color(1.00f, 0.90f, 0.30f, 1f)),  // 明黄
    };

    // 中文字体路径
    private const string ChineseFontPath = "Assets/Resources/Fonts/AlibabaPuHuiTi-3-85-Bold SDF.asset";

    [MenuItem("Tools/DrscfZ/Rebuild Gift Icon Bar")]
    public static void Execute()
    {
        // 步骤1：先确保图片以 Sprite 类型导入
        ImportSprites();

        // 步骤2：刷新数据库使导入生效
        AssetDatabase.Refresh();

        // 步骤3：找到 GiftIconBar
        var barGo = FindInScene("GiftIconBar");
        if (barGo == null)
        {
            Debug.LogError("[RebuildGiftIconBar] 未找到 GiftIconBar，请检查场景层级。");
            return;
        }

        // 步骤4：删除所有旧子对象
        for (int i = barGo.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(barGo.transform.GetChild(i).gameObject);

        // 步骤5：确保父容器有 HorizontalLayoutGroup
        var hlg = barGo.GetComponent<HorizontalLayoutGroup>();
        if (hlg == null) hlg = barGo.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleCenter;
        hlg.spacing                = 6f;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.padding                = new RectOffset(8, 8, 4, 4);

        // 步骤6：加载中文字体
        var chineseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ChineseFontPath);
        if (chineseFont == null)
            Debug.LogWarning($"[RebuildGiftIconBar] 未能加载中文字体: {ChineseFontPath}");

        // 步骤7：创建 6 张礼物卡片
        for (int i = 0; i < Gifts.Length; i++)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(IconPaths[i]);
            if (sprite == null)
                Debug.LogWarning($"[RebuildGiftIconBar] 未能加载图标: {IconPaths[i]}");
            CreateCard(barGo.transform, Gifts[i], sprite, chineseFont);
        }

        // 步骤8：保存场景
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[RebuildGiftIconBar] 完成！6 个礼物卡片已创建（图标+名称+效果），场景已保存。");
    }

    // ──────────────────────────────────────────
    // 将图片导入设置改为 Sprite (2D and UI)
    // ──────────────────────────────────────────

    private static void ImportSprites()
    {
        bool changed = false;
        foreach (var path in IconPaths)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType    = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled  = false;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                changed = true;
            }
        }
        if (changed)
            Debug.Log("[RebuildGiftIconBar] 已将礼物图片导入类型设置为 Sprite。");
    }

    // ──────────────────────────────────────────
    // 创建单张礼物卡片
    // ──────────────────────────────────────────

    private static void CreateCard(Transform parent, GiftData d, Sprite icon, TMP_FontAsset font = null)
    {
        // ── 卡片根节点 ──
        var card   = new GameObject(d.giftName);
        card.transform.SetParent(parent, false);
        var bgImg  = card.AddComponent<Image>();
        bgImg.color         = d.bgColor;
        bgImg.raycastTarget = false;

        // 底部锚点 + 绝对像素高度，不受父级 LayoutGroup 影响
        // 卡片高度由 GiftIconBar HLG 强制为 160px
        // 布局：底部说明28px | 底部30px处名称30px | 顶部图标填满剩余

        // 改用百分比区域：底部25%=说明，25%~50%=名称，50%~100%=图标
        // 避免依赖绝对px高度，跟随卡片实际尺寸自适应

        // ── 效果说明：底部 0%~25% ──
        var effGo   = new GameObject("EffectLabel");
        var effRT   = effGo.AddComponent<RectTransform>();
        effGo.transform.SetParent(card.transform, false);
        effRT.anchorMin        = new Vector2(0f, 0f);
        effRT.anchorMax        = new Vector2(1f, 0.25f);
        effRT.offsetMin        = new Vector2(2f, 0f);
        effRT.offsetMax        = new Vector2(-2f, 0f);
        var effTMP  = effGo.AddComponent<TextMeshProUGUI>();
        effTMP.text             = d.effect;
        effTMP.fontSize         = 18f;
        effTMP.color            = Color.black;
        effTMP.alignment        = TextAlignmentOptions.Center;
        effTMP.overflowMode     = TextOverflowModes.Ellipsis;
        effTMP.enableAutoSizing = false;
        effTMP.raycastTarget    = false;
        if (font != null) effTMP.font = font;
        SetTMPColor(effTMP, Color.black);

        // ── 礼物名：25%~50% ──
        var nameGo  = new GameObject("NameLabel");
        var nameRT  = nameGo.AddComponent<RectTransform>();
        nameGo.transform.SetParent(card.transform, false);
        nameRT.anchorMin        = new Vector2(0f, 0.25f);
        nameRT.anchorMax        = new Vector2(1f, 0.50f);
        nameRT.offsetMin        = new Vector2(2f, 0f);
        nameRT.offsetMax        = new Vector2(-2f, 0f);
        var nameTMP = nameGo.AddComponent<TextMeshProUGUI>();
        nameTMP.text             = d.giftName;
        nameTMP.fontSize         = 22f;
        nameTMP.fontStyle        = FontStyles.Bold;
        nameTMP.color            = Color.black;
        nameTMP.alignment        = TextAlignmentOptions.Center;
        nameTMP.overflowMode     = TextOverflowModes.Ellipsis;
        nameTMP.enableAutoSizing = false;
        nameTMP.raycastTarget    = false;
        if (font != null) nameTMP.font = font;
        SetTMPColor(nameTMP, Color.black);

        // ── 图标：50%~100%（卡片上半部分） ──
        var iconGo  = new GameObject("Icon");
        var iconRT  = iconGo.AddComponent<RectTransform>();
        iconGo.transform.SetParent(card.transform, false);
        iconRT.anchorMin = new Vector2(0.05f, 0.50f);
        iconRT.anchorMax = new Vector2(0.95f, 0.98f);
        iconRT.offsetMin = Vector2.zero;
        iconRT.offsetMax = Vector2.zero;
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.sprite         = icon;
        iconImg.preserveAspect = true;
        iconImg.raycastTarget  = false;
        iconImg.color          = Color.white;
    }

    // ──────────────────────────────────────────
    // 直接写 TMP 序列化颜色字段（绕开 material 未初始化问题）
    // ──────────────────────────────────────────

    private static void SetTMPColor(TextMeshProUGUI tmp, Color c)
    {
        var so = new SerializedObject(tmp);
        // m_fontColor  → 影响 fontColor 属性
        // m_fontColor32 → TMP 渲染实际使用的顶点色
        var p1 = so.FindProperty("m_fontColor");
        var p2 = so.FindProperty("m_fontColor32");
        if (p1 != null) p1.colorValue = c;
        if (p2 != null) p2.colorValue = c;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(tmp);
    }

    // ──────────────────────────────────────────
    // 场景中按名字查找（包含非激活对象）
    // ──────────────────────────────────────────

    private static GameObject FindInScene(string name)
    {
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go.name == name && go.scene.isLoaded)
                return go;
        return null;
    }

    // ──────────────────────────────────────────
    // 数据类
    // ──────────────────────────────────────────

    private class GiftData
    {
        public string giftName;
        public string tierTag;
        public string effect;
        public Color  bgColor;

        public GiftData(string name, string tier, string eff, Color bg)
        {
            giftName = name;
            tierTag  = tier;
            effect   = eff;
            bgColor  = bg;
        }
    }
}
