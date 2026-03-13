using UnityEngine;
using UnityEditor;
using TMPro;
using DrscfZ.UI;

/// <summary>
/// 在 3D 场景中创建悬浮标签 UI（WorldSpace）。
///
/// 创建以下标签（悬浮在对应场景对象上方）：
///   - 🐟 食物采集区（LeftCamp 上方）
///   - ⛏ 煤矿区（BackMountain_L 上方 / 工位坐标）
///   - 🪨 矿山区（BackMountain_R 上方 / 工位坐标）
///   - 🔥 炉灶区（CentralFortress 旁）
///   - ⚔ 城门防线（CityGate_Main 上方）
///   - 🏰 中央堡垒（CentralFortress 正上方）
///
/// 菜单：Tools/Phase2/Setup 3D Scene UI
/// </summary>
public class Setup3DSceneUI
{
    private const float LABEL_HEIGHT = 4.0f;  // 悬浮高度（世界空间Y偏移）
    private const float ICON_SIZE    = 3.5f;  // 图标字号（world-space TMP，需较大才可见）
    private const float VALUE_SIZE   = 2.2f;  // 数值字号

    [MenuItem("Tools/Phase2/Setup 3D Scene UI")]
    public static void Execute()
    {
        // 父节点：SceneUI3D（始终激活，挂在根下）
        var root3D = EnsureRoot("SceneUI3D");

        // 加载字体（优先 ChineseFont SDF，其次 Unity 内置 LiberationSans）
        var font = LoadFont();

        // ── 创建各标签 ──────────────────────────────────────────────────

        // 用汉字单字代替 emoji（ChineseFont SDF 不含 emoji，汉字可正常显示）
        CreateLabel(root3D, font, "Label_FoodCamp",
            new Vector3(-6f, LABEL_HEIGHT, 12f),
            "鱼", "食物采集区", WorldSpaceLabel.LabelType.Food,
            new Color(0.267f, 0.533f, 1.0f));   // 蓝色

        CreateLabel(root3D, font, "Label_CoalMine",
            new Vector3(-8f, LABEL_HEIGHT, 16f),
            "煤", "煤矿区", WorldSpaceLabel.LabelType.Coal,
            new Color(0.55f, 0.55f, 0.55f));     // 深灰

        CreateLabel(root3D, font, "Label_OreMine",
            new Vector3(7f, LABEL_HEIGHT, 16f),
            "矿", "矿山区", WorldSpaceLabel.LabelType.Ore,
            new Color(0.533f, 0.8f, 1.0f));      // 冰蓝

        CreateLabel(root3D, font, "Label_Stove",
            new Vector3(3f, LABEL_HEIGHT, 3f),
            "火", "炉灶", WorldSpaceLabel.LabelType.Temperature,
            new Color(1.0f, 0.408f, 0.125f));    // 橙红

        // 城门标签（静态）
        CreateLabel(root3D, font, "Label_CityGate",
            new Vector3(0f, LABEL_HEIGHT, -4f),
            "城", "城门防线", WorldSpaceLabel.LabelType.Static,
            new Color(1.0f, 0.8f, 0.2f));        // 金黄

        // 中央堡垒标签（静态）
        CreateLabel(root3D, font, "Label_Fortress",
            new Vector3(0f, LABEL_HEIGHT + 1.5f, 2f),
            "堡", "中央堡垒", WorldSpaceLabel.LabelType.Static,
            Color.white);

        // ── 保存场景 ────────────────────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[Setup3DSceneUI] 6个3D场景标签已创建，场景已保存 ✅");
    }

    // ── 工具方法 ─────────────────────────────────────────────────────────

    private static GameObject EnsureRoot(string name)
    {
        var existing = GameObject.Find(name);
        if (existing != null) return existing;
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return go;
    }

    private static TMP_FontAsset LoadFont()
    {
        // 尝试加载中文字体
        var font = Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
        if (font == null)
        {
            // fallback：在 Assets 中找第一个 TMP FontAsset
            var guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                Debug.Log($"[Setup3DSceneUI] 使用字体: {path}");
            }
        }
        return font;
    }

    private static void CreateLabel(
        GameObject parent,
        TMP_FontAsset font,
        string objName,
        Vector3 worldPos,
        string iconStr,
        string labelStr,
        WorldSpaceLabel.LabelType labelType,
        Color color)
    {
        // 检查是否已存在，避免重复创建
        var existing = parent.transform.Find(objName);
        if (existing != null)
        {
            Debug.Log($"[Setup3DSceneUI] {objName} 已存在，跳过");
            return;
        }

        // 根 GameObject
        var root = new GameObject(objName);
        root.transform.SetParent(parent.transform, false);
        root.transform.position = worldPos;
        Undo.RegisterCreatedObjectUndo(root, $"Create {objName}");

        // 添加 WorldSpaceLabel 组件
        var wsl = root.AddComponent<WorldSpaceLabel>();
        wsl.icon      = iconStr;
        wsl.label     = labelStr;
        wsl.labelType = labelType;

        // 图标 TMP (3D TextMeshPro)
        var iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(root.transform, false);
        iconGO.transform.localPosition = new Vector3(0f, 0.7f, 0f);

        var iconTmp = iconGO.AddComponent<TextMeshPro>();
        iconTmp.text      = iconStr;
        iconTmp.fontSize  = ICON_SIZE;
        iconTmp.alignment = TextAlignmentOptions.Center;
        iconTmp.color     = color;
        if (font != null) iconTmp.font = font;

        var iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.sizeDelta = new Vector2(6f, 4f);

        wsl.iconText = iconTmp;
        EditorUtility.SetDirty(iconGO);

        // 数值/名称 TMP
        var valueGO = new GameObject("Value");
        valueGO.transform.SetParent(root.transform, false);
        valueGO.transform.localPosition = new Vector3(0f, 0f, 0f);

        var valueTmp = valueGO.AddComponent<TextMeshPro>();
        valueTmp.text      = labelType == WorldSpaceLabel.LabelType.Static ? labelStr : "--";
        valueTmp.fontSize  = VALUE_SIZE;
        valueTmp.alignment = TextAlignmentOptions.Center;
        valueTmp.color     = Color.white;
        if (font != null) valueTmp.font = font;

        var valueRT = valueGO.GetComponent<RectTransform>();
        valueRT.sizeDelta = new Vector2(8f, 3f);

        wsl.valueText = valueTmp;
        EditorUtility.SetDirty(valueGO);

        EditorUtility.SetDirty(root);
        Debug.Log($"[Setup3DSceneUI] {objName} ({iconStr} {labelStr}) 创建于 {worldPos} ✅");
    }
}
