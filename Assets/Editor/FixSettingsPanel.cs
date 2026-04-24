using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

/// <summary>
/// 修复 SurvivalSettingsPanel：尺寸 + 全部 TMP 字体绑定
/// 菜单: DrscfZ / Fix Settings Panel
/// </summary>
public class FixSettingsPanel
{
    [MenuItem("DrscfZ/Fix Settings Panel")]
    public static void Fix()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("[Fix] Canvas 未找到"); return; }

        var panelTr = canvas.transform.Find("SurvivalSettingsPanel");
        if (panelTr == null) { Debug.LogError("[Fix] SurvivalSettingsPanel 未找到"); return; }

        var font = Resources.Load<TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF") ?? Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
        if (font == null) Debug.LogWarning("[Fix] ChineseFont SDF 未找到");

        // ── 1. 面板尺寸：放大到 780×620，居中 ──
        var panelRT = panelTr.GetComponent<RectTransform>();
        panelRT.anchorMin        = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax        = new Vector2(0.5f, 0.5f);
        panelRT.pivot            = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;
        panelRT.sizeDelta        = new Vector2(780f, 640f);
        EditorUtility.SetDirty(panelTr.gameObject);

        // ── 2. 背景铺满面板 ──
        var bgTr = panelTr.Find("Background");
        if (bgTr != null)
        {
            var bgRT = bgTr.GetComponent<RectTransform>();
            bgRT.anchorMin        = Vector2.zero;
            bgRT.anchorMax        = Vector2.one;
            bgRT.offsetMin        = Vector2.zero;
            bgRT.offsetMax        = Vector2.zero;
            var bgImg = bgTr.GetComponent<Image>();
            if (bgImg != null) bgImg.color = new Color(0.06f, 0.08f, 0.18f, 0.96f);
            EditorUtility.SetDirty(bgTr.gameObject);
        }

        // ── 3. TitleText 位置 + 字体 ──
        var titleTr = panelTr.Find("TitleText");
        if (titleTr != null)
        {
            var rt = titleTr.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -20f);
            rt.sizeDelta        = new Vector2(0f, 70f);

            var tmp = titleTr.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                if (font != null) tmp.font = font;
                tmp.fontSize  = 36f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color     = Color.white;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.text      = "设置";
            }
            EditorUtility.SetDirty(titleTr.gameObject);
        }

        // ── 4. CloseBtn 右上角 ──
        var closeTr = panelTr.Find("CloseBtn");
        if (closeTr != null)
        {
            var rt = closeTr.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-16f, -16f);
            rt.sizeDelta        = new Vector2(64f, 64f);

            var closeTmpTr = closeTr.Find("CloseText");
            if (closeTmpTr != null)
            {
                var tmp = closeTmpTr.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    if (font != null) tmp.font = font;
                    tmp.fontSize  = 34f;
                    tmp.color     = Color.white;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.text      = "✕";
                }
            }
            EditorUtility.SetDirty(closeTr.gameObject);
        }

        // ── 5. BGMRow 位置 + 字体 ──
        var bgmRowTr = panelTr.Find("BGMRow");
        if (bgmRowTr != null)
        {
            var rt = bgmRowTr.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -110f);
            rt.sizeDelta        = new Vector2(-60f, 70f);
            FixRowFonts(bgmRowTr, font);
            EditorUtility.SetDirty(bgmRowTr.gameObject);
        }

        // ── 6. SFXRow 位置 + 字体 ──
        var sfxRowTr = panelTr.Find("SFXRow");
        if (sfxRowTr != null)
        {
            var rt = sfxRowTr.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -200f);
            rt.sizeDelta        = new Vector2(-60f, 70f);
            FixRowFonts(sfxRowTr, font);
            EditorUtility.SetDirty(sfxRowTr.gameObject);
        }

        // ── 7. Divider ──
        var divTr = panelTr.Find("Divider");
        if (divTr != null)
        {
            var rt = divTr.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.05f, 1f);
            rt.anchorMax        = new Vector2(0.95f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -285f);
            rt.sizeDelta        = new Vector2(0f, 2f);
            EditorUtility.SetDirty(divTr.gameObject);
        }

        // ── 8. VersionText 底部 ──
        var verTr = panelTr.Find("VersionText");
        if (verTr != null)
        {
            var rt = verTr.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 0f);
            rt.anchorMax        = new Vector2(1f, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 20f);
            rt.sizeDelta        = new Vector2(0f, 50f);

            var tmp = verTr.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                if (font != null) tmp.font = font;
                tmp.fontSize  = 24f;
                tmp.color     = new Color(0.6f, 0.6f, 0.7f, 1f);
                tmp.alignment = TextAlignmentOptions.Center;
            }
            EditorUtility.SetDirty(verTr.gameObject);
        }

        // ── 9. 保存 ──
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[Fix] SurvivalSettingsPanel 修复完成：尺寸 780×640，全部 TMP 字体已绑定");
    }

    private static void FixRowFonts(Transform rowTr, TMP_FontAsset font)
    {
        if (font == null) return;
        // 修复行内所有 TMP（Label、ToggleText、ValueText）
        foreach (var tmp in rowTr.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            tmp.font      = font;
            tmp.fontSize  = Mathf.Max(tmp.fontSize, 28f); // 最小28px
            EditorUtility.SetDirty(tmp.gameObject);
        }
    }
}
