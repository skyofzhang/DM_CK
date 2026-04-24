using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using DrscfZ.UI;

/// <summary>
/// 完整重建 SurvivalSettingsPanel 布局并绑定所有 SurvivalSettingsUI 字段
/// 菜单: DrscfZ / Rebuild Settings Panel Layout
/// </summary>
public class RebuildSettingsPanelLayout
{
    [MenuItem("DrscfZ/Rebuild Settings Panel Layout")]
    public static void Rebuild()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("[Rebuild] Canvas 未找到"); return; }

        var ui = canvas.GetComponent<SurvivalSettingsUI>();
        if (ui == null) { Debug.LogError("[Rebuild] Canvas 上没有 SurvivalSettingsUI 脚本"); return; }

        var panelTr = canvas.transform.Find("SurvivalSettingsPanel");
        if (panelTr == null) { Debug.LogError("[Rebuild] SurvivalSettingsPanel 未找到"); return; }

        var font = Resources.Load<TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF") ?? Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
        if (font == null) Debug.LogWarning("[Rebuild] ChineseFont SDF 未找到，部分字体可能乱码");

        // ── 1. 修复 BGMRow ──
        var bgmRowTr = panelTr.Find("BGMRow");
        Slider    bgmSlider      = null;
        Button    bgmToggle      = null;
        TMP_Text  bgmValueText   = null;
        TMP_Text  bgmToggleText  = null;

        if (bgmRowTr != null)
            FixAudioRow(bgmRowTr, "背景音乐", font,
                "BGMToggleBtn", "BGMToggleText", "BGMSlider", "BGMValueText",
                out bgmSlider, out bgmToggle, out bgmValueText, out bgmToggleText);

        // ── 2. 修复 SFXRow ──
        var sfxRowTr = panelTr.Find("SFXRow");
        Slider    sfxSlider      = null;
        Button    sfxToggle      = null;
        TMP_Text  sfxValueText   = null;
        TMP_Text  sfxToggleText  = null;

        if (sfxRowTr != null)
            FixAudioRow(sfxRowTr, "音效", font,
                "SFXToggleBtn", "SFXToggleText", "SFXSlider", "SFXValueText",
                out sfxSlider, out sfxToggle, out sfxValueText, out sfxToggleText);

        // ── 3. 创建/修复 GiftVideoRow ──
        Toggle giftVideoToggle = null;
        var giftRowTr = panelTr.Find("GiftVideoRow");
        if (giftRowTr == null)
            giftRowTr = CreateVideoToggleRow(panelTr, "GiftVideoRow", "礼物视频动画", font,
                new Vector2(0f, -310f), out giftVideoToggle);
        else
            giftVideoToggle = giftRowTr.GetComponentInChildren<Toggle>(true);

        // ── 4. 创建/修复 VIPVideoRow ──
        Toggle vipVideoToggle = null;
        var vipRowTr = panelTr.Find("VIPVideoRow");
        if (vipRowTr == null)
            vipRowTr = CreateVideoToggleRow(panelTr, "VIPVideoRow", "VIP入场视频", font,
                new Vector2(0f, -390f), out vipVideoToggle);
        else
            vipVideoToggle = vipRowTr.GetComponentInChildren<Toggle>(true);

        // ── 5. 找 CloseBtn + VersionText ──
        var closeBtnTr = panelTr.Find("CloseBtn");
        Button   closeBtn    = closeBtnTr?.GetComponent<Button>();
        var verTr            = panelTr.Find("VersionText");
        TMP_Text versionText = verTr?.GetComponent<TMP_Text>();

        // ── 6. 通过 SerializedObject 绑定所有字段 ──
        var so = new SerializedObject(ui);
        BindRef(so, "_panel",          panelTr.gameObject);
        BindRef(so, "_closeBtn",       closeBtn);
        BindRef(so, "_bgmSlider",      bgmSlider);
        BindRef(so, "_bgmValueText",   bgmValueText);
        BindRef(so, "_bgmToggle",      bgmToggle);
        BindRef(so, "_bgmToggleText",  bgmToggleText);
        BindRef(so, "_sfxSlider",      sfxSlider);
        BindRef(so, "_sfxValueText",   sfxValueText);
        BindRef(so, "_sfxToggle",      sfxToggle);
        BindRef(so, "_sfxToggleText",  sfxToggleText);
        BindRef(so, "_versionText",    versionText);
        BindRef(so, "_giftVideoToggle", giftVideoToggle);
        BindRef(so, "_vipVideoToggle",  vipVideoToggle);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(ui);

        // ── 7. 保存场景 ──
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[Rebuild] SurvivalSettingsPanel 布局重建完成！\n" +
                  $"  BGM Slider={bgmSlider != null}, Toggle={bgmToggle != null}, " +
                  $"Value={bgmValueText != null}, Icon={bgmToggleText != null}\n" +
                  $"  SFX Slider={sfxSlider != null}, Toggle={sfxToggle != null}, " +
                  $"Value={sfxValueText != null}, Icon={sfxToggleText != null}\n" +
                  $"  GiftVideo={giftVideoToggle != null}, VIPVideo={vipVideoToggle != null}");
    }

    // ════════════════════════════════════════════
    //  修复一行音量控件的布局
    //  布局：[Label 150px] [ToggleBtn 70px] [Slider flex] [ValueText 80px]
    // ════════════════════════════════════════════
    static void FixAudioRow(Transform rowTr, string labelStr, TMP_FontAsset font,
        string toggleBtnName, string toggleTextName, string sliderName, string valueName,
        out Slider slider, out Button toggleBtn,
        out TMP_Text valueText, out TMP_Text toggleText)
    {
        slider      = null;
        toggleBtn   = null;
        valueText   = null;
        toggleText  = null;

        // 行高
        var rowRT = rowTr.GetComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(rowRT.sizeDelta.x, 76f);

        // HLG 参数
        var hlg = rowTr.GetComponent<HorizontalLayoutGroup>();
        if (hlg == null) hlg = rowTr.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth       = true;
        hlg.childControlHeight      = true;
        hlg.childForceExpandWidth   = false;
        hlg.childForceExpandHeight  = true;
        hlg.spacing    = 14f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.padding    = new RectOffset(20, 20, 10, 10);
        EditorUtility.SetDirty(rowTr.gameObject);

        // ── Label ──
        var labelTr = rowTr.Find("Label");
        if (labelTr != null)
        {
            SetLE(labelTr.gameObject, prefW: 150f, flexW: 0f);
            var tmp = labelTr.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                if (font != null) tmp.font = font;
                tmp.text      = labelStr;
                tmp.fontSize  = 30f;
                tmp.color     = Color.white;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.enableWordWrapping = false;
            }
            EditorUtility.SetDirty(labelTr.gameObject);
        }

        // ── ToggleBtn ──
        var toggleTr = rowTr.Find(toggleBtnName);
        if (toggleTr != null)
        {
            SetLE(toggleTr.gameObject, prefW: 72f, prefH: 56f, flexW: 0f);
            toggleBtn = toggleTr.GetComponent<Button>();

            // 按钮背景颜色
            var img = toggleTr.GetComponent<Image>();
            if (img != null) img.color = new Color(0.18f, 0.30f, 0.52f, 0.90f);

            // 子文字
            var ttTr = toggleTr.Find(toggleTextName);
            if (ttTr != null)
            {
                toggleText = ttTr.GetComponent<TMP_Text>();
                if (toggleText != null)
                {
                    if (font != null) toggleText.font = font;
                    toggleText.fontSize  = 30f;
                    toggleText.color     = Color.white;
                    toggleText.alignment = TextAlignmentOptions.Center;
                    toggleText.text      = "🔊";
                }
                // 让文字铺满按钮
                var ttRT = ttTr.GetComponent<RectTransform>();
                ttRT.anchorMin = Vector2.zero;
                ttRT.anchorMax = Vector2.one;
                ttRT.offsetMin = ttRT.offsetMax = Vector2.zero;
                EditorUtility.SetDirty(ttTr.gameObject);
            }
            EditorUtility.SetDirty(toggleTr.gameObject);
        }

        // ── Slider（保留原有结构，只调整 LayoutElement）──
        var sliderTr = rowTr.Find(sliderName);
        if (sliderTr != null)
        {
            SetLE(sliderTr.gameObject, prefH: 48f, flexW: 1f);
            slider = sliderTr.GetComponent<Slider>();
            EditorUtility.SetDirty(sliderTr.gameObject);
        }

        // ── ValueText ──
        var valueTr = rowTr.Find(valueName);
        if (valueTr != null)
        {
            SetLE(valueTr.gameObject, prefW: 80f, flexW: 0f);
            valueText = valueTr.GetComponent<TMP_Text>();
            if (valueText != null)
            {
                if (font != null) valueText.font = font;
                valueText.fontSize  = 28f;
                valueText.color     = new Color(0.85f, 0.95f, 1f, 1f);
                valueText.alignment = TextAlignmentOptions.MidlineRight;
                valueText.text      = "80%";
                valueText.enableWordWrapping = false;
            }
            EditorUtility.SetDirty(valueTr.gameObject);
        }
    }

    // ════════════════════════════════════════════
    //  创建 Phase2 视频开关行
    //  布局：[Label flex] [Toggle 60px]
    // ════════════════════════════════════════════
    static Transform CreateVideoToggleRow(Transform parent, string goName,
        string labelStr, TMP_FontAsset font, Vector2 anchoredPos, out Toggle toggle)
    {
        toggle = null;

        var go = new GameObject(goName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = new Vector2(-60f, 64f);

        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth       = true;
        hlg.childControlHeight      = true;
        hlg.childForceExpandWidth   = false;
        hlg.childForceExpandHeight  = true;
        hlg.spacing    = 14f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.padding    = new RectOffset(20, 20, 8, 8);

        // ── Label ──
        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(go.transform, false);
        var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
        if (font != null) labelTmp.font = font;
        labelTmp.text      = labelStr;
        labelTmp.fontSize  = 28f;
        labelTmp.color     = new Color(0.85f, 0.85f, 0.85f, 1f);
        labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
        labelTmp.enableWordWrapping = false;
        SetLE(labelGo, flexW: 1f);

        // ── Toggle 容器 ──
        var toggleGo = new GameObject("Toggle", typeof(RectTransform));
        toggleGo.transform.SetParent(go.transform, false);
        SetLE(toggleGo, prefW: 60f, prefH: 48f, flexW: 0f);

        var bgImg = toggleGo.AddComponent<Image>();
        bgImg.color = new Color(0.18f, 0.30f, 0.52f, 0.90f);

        toggle = toggleGo.AddComponent<Toggle>();
        toggle.isOn = true;
        toggle.targetGraphic = bgImg;

        // 打勾图标（Checkmark）
        var ckGo = new GameObject("Checkmark", typeof(RectTransform));
        ckGo.transform.SetParent(toggleGo.transform, false);
        var ckRT = ckGo.GetComponent<RectTransform>();
        ckRT.anchorMin = new Vector2(0.1f, 0.1f);
        ckRT.anchorMax = new Vector2(0.9f, 0.9f);
        ckRT.offsetMin = ckRT.offsetMax = Vector2.zero;
        var ckImg = ckGo.AddComponent<Image>();
        ckImg.color = new Color(0.25f, 0.85f, 0.35f, 1f);
        toggle.graphic = ckImg;

        EditorUtility.SetDirty(go);
        return go.transform;
    }

    // ════════════════════════════════════════════
    //  工具函数
    // ════════════════════════════════════════════

    static void SetLE(GameObject go, float prefW = -1f, float prefH = -1f,
                      float flexW = -1f, float flexH = -1f)
    {
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        if (prefW >= 0) le.preferredWidth  = prefW;
        if (prefH >= 0) le.preferredHeight = prefH;
        if (flexW >= 0) le.flexibleWidth   = flexW;
        if (flexH >= 0) le.flexibleHeight  = flexH;
    }

    static void BindRef(SerializedObject so, string propName, UnityEngine.Object obj)
    {
        if (obj == null) { Debug.LogWarning($"[Rebuild] 字段 {propName} 对应对象为 null，跳过绑定"); return; }
        var sp = so.FindProperty(propName);
        if (sp == null) { Debug.LogWarning($"[Rebuild] 找不到属性 {propName}"); return; }
        sp.objectReferenceValue = obj;
    }
}
