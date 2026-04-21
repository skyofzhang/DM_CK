using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using DrscfZ.UI;

/// <summary>
/// Tools → DrscfZ → Create Gate Upgrade UI
///
/// 在场景主 Canvas 下创建 GateUpgradeConfirmUI（🆕 v1.22 §10）：
///   Canvas/
///     └─ GateUpgradeConfirmUI (script)  ← 常驻 active，Rule #6
///         └─ Panel (Image 遮罩，默认 SetActive(false))
///             ├─ Title (TMP)          "升级城门"
///             ├─ CurrentLevel (TMP)   "当前 Lv.X"
///             ├─ NextLevel (TMP)      "→ Lv.X+1「xxx」"
///             ├─ Cost (TMP)           "消耗矿石 × N"
///             ├─ Features (TMP)       "[特性描述]"
///             ├─ BtnConfirm (Button)  "确认升级"
///             └─ BtnCancel (Button)   "取消"
///
/// 规则：
///   - 禁止 EditorUtility.DisplayDialog（阻塞进程）
///   - 使用 SerializedObject 绑定私有字段
///   - 保存场景：EditorSceneManager.SaveScene()
/// </summary>
public static class CreateGateUpgradeUI
{
    private const string UI_GO_NAME = "GateUpgradeConfirmUI";
    private const string CHINESE_FONT_PATH = "Assets/Resources/Fonts/ChineseFont SDF.asset";

    [MenuItem("Tools/DrscfZ/Create Gate Upgrade UI")]
    public static void Execute()
    {
        // ── 1. 找 Canvas ─────────────────────────────────────────────────────
        var canvas = FindMainCanvas();
        if (canvas == null)
        {
            Debug.LogError("[CreateGateUpgradeUI] 未找到场景中的 Canvas（按名 'Canvas' / 首个 Canvas）。请先确保场景存在 Canvas。");
            return;
        }

        // ── 2. 删除旧实例（重建幂等）─────────────────────────────────────────
        var existing = canvas.transform.Find(UI_GO_NAME);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing.gameObject);
            Debug.Log("[CreateGateUpgradeUI] 已删除旧 GateUpgradeConfirmUI");
        }

        // ── 3. 创建根 UI（always-active，Rule #6）────────────────────────────
        var rootGo = new GameObject(UI_GO_NAME, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(rootGo, "Create GateUpgradeConfirmUI");
        rootGo.transform.SetParent(canvas.transform, false);

        var rootRt = rootGo.GetComponent<RectTransform>();
        rootRt.anchorMin        = Vector2.zero;
        rootRt.anchorMax        = Vector2.one;
        rootRt.offsetMin        = Vector2.zero;
        rootRt.offsetMax        = Vector2.zero;
        rootRt.anchoredPosition = Vector2.zero;
        rootRt.localScale       = Vector3.one;

        var confirmUI = rootGo.AddComponent<GateUpgradeConfirmUI>();

        // ── 4. 创建 Panel（子对象，默认 SetActive(false)）────────────────────
        var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(rootGo.transform, false);

        var panelRt = panelGo.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        var panelImg = panelGo.GetComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.75f); // 半透明黑遮罩
        panelImg.raycastTarget = true;

        // ── 4a. 创建中央内容盒（600x520 居中）─────────────────────────────────
        var boxGo = new GameObject("DialogBox", typeof(RectTransform), typeof(Image));
        boxGo.transform.SetParent(panelGo.transform, false);
        var boxRt = boxGo.GetComponent<RectTransform>();
        boxRt.anchorMin        = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax        = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta        = new Vector2(600f, 520f);
        boxRt.anchoredPosition = Vector2.zero;

        var boxImg = boxGo.GetComponent<Image>();
        boxImg.color = new Color(0.12f, 0.16f, 0.28f, 0.95f);

        // 加载中文字体
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CHINESE_FONT_PATH);
        if (font == null)
            Debug.LogWarning($"[CreateGateUpgradeUI] 未找到中文字体 {CHINESE_FONT_PATH}，文本可能乱码");

        // ── 5. 文本控件 ──────────────────────────────────────────────────────
        var titleTmp        = CreateTextChild(boxGo.transform, "Title",
            new Vector2(0f, 200f), new Vector2(560f, 60f), "升级城门",
            fontSize: 40f, color: new Color(0.95f, 0.82f, 0.3f, 1f), font: font, bold: true);

        var currentLevelTmp = CreateTextChild(boxGo.transform, "CurrentLevel",
            new Vector2(0f, 110f), new Vector2(560f, 48f), "当前 Lv.1",
            fontSize: 28f, color: new Color(0.8f, 0.8f, 0.8f, 1f), font: font, bold: false);

        var nextLevelTmp    = CreateTextChild(boxGo.transform, "NextLevel",
            new Vector2(0f, 50f), new Vector2(560f, 54f), "→ Lv.2「铁栅」",
            fontSize: 34f, color: new Color(0.2f, 0.9f, 1f, 1f), font: font, bold: true);

        var costTmp         = CreateTextChild(boxGo.transform, "Cost",
            new Vector2(0f, -10f), new Vector2(560f, 44f), "消耗矿石 × 100",
            fontSize: 26f, color: new Color(1f, 0.85f, 0.2f, 1f), font: font, bold: false);

        var featuresTmp     = CreateTextChild(boxGo.transform, "Features",
            new Vector2(0f, -80f), new Vector2(560f, 80f), "[新特性说明]",
            fontSize: 22f, color: new Color(0.7f, 0.95f, 0.7f, 1f), font: font, bold: false);

        // ── 6. 按钮 ──────────────────────────────────────────────────────────
        var btnConfirm = CreateButtonChild(boxGo.transform, "BtnConfirm",
            new Vector2(-120f, -200f), new Vector2(200f, 64f), "确认升级",
            bgColor: new Color(0.2f, 0.55f, 0.25f, 1f), textColor: Color.white, font: font);
        var btnCancel  = CreateButtonChild(boxGo.transform, "BtnCancel",
            new Vector2(120f, -200f), new Vector2(200f, 64f), "取消",
            bgColor: new Color(0.45f, 0.25f, 0.25f, 1f), textColor: Color.white, font: font);

        // ── 7. 默认 SetActive(false)（Panel 本身，不影响脚本自身）────────────
        panelGo.SetActive(false);

        // ── 8. 绑定私有字段到脚本 ────────────────────────────────────────────
        var so = new SerializedObject(confirmUI);
        SetObjProp(so, "_panel",            panelGo);
        SetObjProp(so, "_titleText",        titleTmp);
        SetObjProp(so, "_currentLevelText", currentLevelTmp);
        SetObjProp(so, "_nextLevelText",    nextLevelTmp);
        SetObjProp(so, "_costText",         costTmp);
        SetObjProp(so, "_featuresText",     featuresTmp);
        SetObjProp(so, "_btnConfirm",       btnConfirm);
        SetObjProp(so, "_btnCancel",        btnCancel);
        so.ApplyModifiedProperties();

        // ── 9. 标记场景脏 + 保存 ─────────────────────────────────────────────
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        bool saved = EditorSceneManager.SaveScene(activeScene);

        Debug.Log($"[CreateGateUpgradeUI] GateUpgradeConfirmUI 创建完成，场景保存={saved}");
    }

    // ==================== 辅助方法 ====================

    private static Canvas FindMainCanvas()
    {
        // 优先找名为 "Canvas" 的
        var go = GameObject.Find("Canvas");
        if (go != null)
        {
            var c = go.GetComponent<Canvas>();
            if (c != null) return c;
        }
        // 退化：找场景中第一个 Canvas
        var all = GameObject.FindObjectsOfType<Canvas>(true);
        foreach (var c in all)
        {
            if (c != null && c.isRootCanvas) return c;
        }
        return null;
    }

    private static TextMeshProUGUI CreateTextChild(Transform parent, string name,
        Vector2 anchoredPos, Vector2 size, string text, float fontSize, Color color,
        TMP_FontAsset font, bool bold)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = anchoredPos;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        if (font != null) tmp.font = font;

        // TMP 颜色 Bug 规避：通过 SerializedObject 写 m_fontColor / m_fontColor32
        var tso = new SerializedObject(tmp);
        var colProp = tso.FindProperty("m_fontColor");
        if (colProp != null) colProp.colorValue = color;
        var col32Prop = tso.FindProperty("m_fontColor32");
        if (col32Prop != null)
        {
            col32Prop.FindPropertyRelative("rgba").intValue =
                ((Color32)color).r | ((Color32)color).g << 8 | ((Color32)color).b << 16 | ((Color32)color).a << 24;
        }
        tso.ApplyModifiedProperties();
        tmp.color = color; // 兜底

        return tmp;
    }

    private static Button CreateButtonChild(Transform parent, string name,
        Vector2 anchoredPos, Vector2 size, string label, Color bgColor, Color textColor,
        TMP_FontAsset font)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = anchoredPos;

        var img = go.GetComponent<Image>();
        img.color = bgColor;

        var btn = go.GetComponent<Button>();
        // ColorBlock 调一下 highlight/press
        var cb = btn.colors;
        cb.highlightedColor = new Color(bgColor.r + 0.1f, bgColor.g + 0.1f, bgColor.b + 0.1f, bgColor.a);
        cb.pressedColor     = new Color(bgColor.r - 0.1f, bgColor.g - 0.1f, bgColor.b - 0.1f, bgColor.a);
        btn.colors = cb;

        // 按钮文字
        CreateTextChild(go.transform, "Text", Vector2.zero, size, label,
            fontSize: 24f, color: textColor, font: font, bold: true);

        return btn;
    }

    private static void SetObjProp(SerializedObject so, string propName, Object value)
    {
        var prop = so.FindProperty(propName);
        if (prop == null)
        {
            Debug.LogWarning($"[CreateGateUpgradeUI] 字段 {propName} 不存在，跳过绑定");
            return;
        }
        prop.objectReferenceValue = value;
    }
}
