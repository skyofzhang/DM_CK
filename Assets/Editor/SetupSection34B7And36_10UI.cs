// DrscfZ §34 B7 (NewbieHint) + §36.10 (WaitingPhase) UI 搭建 Editor 脚本（audit-r3 Batch E）
// 按 CLAUDE.md #7 铁律：挂 Canvas 常驻激活，不 Awake SetActive(false)
// 字体：Alibaba Primary + ChineseFont fallback
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public static class SetupSection34B7And36_10UI
{
    const string AlibabaFontPath = "Assets/Resources/Fonts/AlibabaPuHuiTi-3-85-Bold SDF.asset";
    const string ChineseFontPath = "Assets/Resources/Fonts/ChineseFont SDF.asset";

    [MenuItem("Tools/DrscfZ/Setup NewbieHint and WaitingPhase UI")]
    public static void Execute()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("[Setup34B7+36.10] Scene 无效，中止");
            return;
        }

        var canvasGO = GameObject.Find("Canvas");
        if (canvasGO == null)
        {
            Debug.LogError("[Setup34B7+36.10] 未找到 Canvas 根节点，中止");
            return;
        }

        var gameUIPanel = canvasGO.transform.Find("GameUIPanel");
        if (gameUIPanel == null)
        {
            Debug.LogError("[Setup34B7+36.10] 未找到 Canvas/GameUIPanel，中止");
            return;
        }

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AlibabaFontPath)
                   ?? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ChineseFontPath);
        if (font == null)
        {
            Debug.LogError("[Setup34B7+36.10] 字体加载失败");
            return;
        }

        SetupNewbieHint(gameUIPanel, font);
        SetupWaitingPhase(gameUIPanel, font);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Setup34B7+36.10] ============ 完成 ============");
        Debug.Log("[Setup34B7+36.10] 场景已保存: " + scene.name);
    }

    // ── §34 B7 NewbieHintUI 搭建 ─────────────────────────────────────────
    static void SetupNewbieHint(Transform gameUIPanel, TMP_FontAsset font)
    {
        // 若已存在则更新
        var existing = gameUIPanel.Find("NewbieHintUI");
        GameObject root;
        if (existing != null)
        {
            root = existing.gameObject;
            Debug.Log("[Setup34B7+36.10] NewbieHintUI 已存在，更新字段");
        }
        else
        {
            root = new GameObject("NewbieHintUI");
            root.transform.SetParent(gameUIPanel, false);
            Debug.Log("[Setup34B7+36.10] ✅ 新建 NewbieHintUI");
        }

        var rt = root.GetComponent<RectTransform>();
        if (rt == null) rt = root.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Welcome Label（顶部浅黄横幅）
        var welcomeLabel = GetOrCreateLabel(root.transform, "WelcomeLabel", new Vector2(0.1f, 0.85f), new Vector2(0.9f, 0.92f), font, 28f);
        welcomeLabel.color = new Color(1f, 0.95f, 0.7f, 1f);
        welcomeLabel.alignment = TextAlignmentOptions.Center;

        // Barrage Toast Label（中央浅绿 toast）
        var barrageLabel = GetOrCreateLabel(root.transform, "BarrageLabel", new Vector2(0.25f, 0.55f), new Vector2(0.75f, 0.62f), font, 32f);
        barrageLabel.color = new Color(0.5f, 1f, 0.5f, 1f);
        barrageLabel.alignment = TextAlignmentOptions.Center;

        // 挂脚本
        var script = root.GetComponent("NewbieHintUI") as MonoBehaviour;
        if (script == null)
        {
            var type = System.Type.GetType("DrscfZ.UI.NewbieHintUI, Assembly-CSharp");
            if (type != null)
            {
                script = root.AddComponent(type) as MonoBehaviour;
            }
            else
            {
                Debug.LogError("[Setup34B7+36.10] Type DrscfZ.UI.NewbieHintUI not found");
                return;
            }
        }

        // 绑字段
        var so = new SerializedObject(script);
        so.FindProperty("_welcomeLabel").objectReferenceValue = welcomeLabel;
        so.FindProperty("_barrageLabel").objectReferenceValue = barrageLabel;
        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("[Setup34B7+36.10] ✅ NewbieHintUI 字段绑定完成");
    }

    // ── §36.10 WaitingPhaseUI 搭建 ──────────────────────────────────────
    static void SetupWaitingPhase(Transform gameUIPanel, TMP_FontAsset font)
    {
        var existing = gameUIPanel.Find("WaitingPhaseUI");
        GameObject root;
        if (existing != null)
        {
            root = existing.gameObject;
            Debug.Log("[Setup34B7+36.10] WaitingPhaseUI 已存在，更新字段");
        }
        else
        {
            root = new GameObject("WaitingPhaseUI");
            root.transform.SetParent(gameUIPanel, false);
            Debug.Log("[Setup34B7+36.10] ✅ 新建 WaitingPhaseUI");
        }

        var rt = root.GetComponent<RectTransform>();
        if (rt == null) rt = root.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // 整个 panel：暗背景 + 标题 + 主题 + 倒计时
        GameObject panel;
        var panelT = root.transform.Find("Panel");
        if (panelT != null) { panel = panelT.gameObject; }
        else
        {
            panel = new GameObject("Panel");
            panel.transform.SetParent(root.transform, false);
            var prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;
            var img = panel.AddComponent<Image>();
            img.color = new Color(0.05f, 0.08f, 0.15f, 0.8f);
        }

        // 大标题
        var title = GetOrCreateLabel(panel.transform, "TitleLabel", new Vector2(0.15f, 0.55f), new Vector2(0.85f, 0.70f), font, 56f);
        title.color = Color.white;
        title.alignment = TextAlignmentOptions.Center;
        title.fontStyle = FontStyles.Bold;

        // 主题
        var theme = GetOrCreateLabel(panel.transform, "ThemeLabel", new Vector2(0.15f, 0.40f), new Vector2(0.85f, 0.50f), font, 36f);
        theme.color = new Color(0.9f, 0.85f, 0.55f, 1f);
        theme.alignment = TextAlignmentOptions.Center;

        // 倒计时
        var countdown = GetOrCreateLabel(panel.transform, "CountdownLabel", new Vector2(0.3f, 0.25f), new Vector2(0.7f, 0.38f), font, 72f);
        countdown.color = new Color(1f, 0.7f, 0.3f, 1f);
        countdown.alignment = TextAlignmentOptions.Center;

        // 挂脚本
        var script = root.GetComponent("WaitingPhaseUI") as MonoBehaviour;
        if (script == null)
        {
            var type = System.Type.GetType("DrscfZ.UI.WaitingPhaseUI, Assembly-CSharp");
            if (type != null)
            {
                script = root.AddComponent(type) as MonoBehaviour;
            }
            else
            {
                Debug.LogError("[Setup34B7+36.10] Type DrscfZ.UI.WaitingPhaseUI not found");
                return;
            }
        }

        // 绑字段
        var so = new SerializedObject(script);
        var rootPanelProp = so.FindProperty("_rootPanel");
        if (rootPanelProp != null) rootPanelProp.objectReferenceValue = panel;
        so.FindProperty("_titleLabel").objectReferenceValue = title;
        so.FindProperty("_themeLabel").objectReferenceValue = theme;
        so.FindProperty("_countdownLabel").objectReferenceValue = countdown;
        so.ApplyModifiedPropertiesWithoutUndo();

        // WaitingPhase 默认隐藏（由 script 控制）
        panel.SetActive(false);

        Debug.Log("[Setup34B7+36.10] ✅ WaitingPhaseUI 字段绑定完成");
    }

    // ── Helper ─────────────────────────────────────────────────────────
    static TMP_Text GetOrCreateLabel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, TMP_FontAsset font, float fontSize)
    {
        TMP_Text tmp;
        var existing = parent.Find(name);
        if (existing != null)
        {
            tmp = existing.GetComponent<TMP_Text>();
            if (tmp == null) tmp = existing.gameObject.AddComponent<TextMeshProUGUI>();
        }
        else
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            tmp = go.AddComponent<TextMeshProUGUI>();
        }
        // 通过 SerializedObject 写字体和颜色（避免 faceColor 白色 bug）
        var so = new SerializedObject(tmp);
        var fontProp = so.FindProperty("m_fontAsset");
        if (fontProp != null) fontProp.objectReferenceValue = font;
        so.ApplyModifiedPropertiesWithoutUndo();
        tmp.fontSize = fontSize;
        tmp.raycastTarget = false;
        tmp.text = "";
        return tmp;
    }
}
