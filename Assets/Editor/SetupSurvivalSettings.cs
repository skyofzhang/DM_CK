using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrscfZ.UI;

namespace DrscfZ.Editor
{
    /// <summary>
    /// SurvivalSettingsPanel 场景结构自动化建立工具
    ///
    /// 菜单：Tools → DrscfZ → Setup Survival Settings Panel
    ///
    /// 功能：
    ///   在 Canvas 下创建 SurvivalSettingsPanel（居中半透明面板）
    ///   并将 SurvivalSettingsUI 挂载到 Canvas，绑定所有字段。
    ///
    /// 面板结构：
    ///   SurvivalSettingsPanel（初始 inactive, 480×420）
    ///   ├── Background（深色半透明底）
    ///   ├── TitleText（"⚙ 设置"）
    ///   ├── CloseBtn（右上角 "✕"）
    ///   ├── BGMRow（HorizontalLayoutGroup）
    ///   │   ├── BGMLabel
    ///   │   ├── BGMToggleBtn → BGMToggleText
    ///   │   ├── BGMSlider
    ///   │   └── BGMValueText
    ///   ├── SFXRow（HorizontalLayoutGroup）
    ///   │   ├── SFXLabel
    ///   │   ├── SFXToggleBtn → SFXToggleText
    ///   │   ├── SFXSlider
    ///   │   └── SFXValueText
    ///   └── VersionText
    ///
    /// 注意：不使用 DisplayDialog；若对象已存在则跳过
    /// </summary>
    public static class SetupSurvivalSettings
    {
        [MenuItem("Tools/DrscfZ/Setup Survival Settings Panel")]
        public static void Execute()
        {
            int created = 0;

            // ── 1. 找 Canvas ──────────────────────────────────────────────────
            var canvasGo = GameObject.Find("Canvas");
            if (canvasGo == null)
            {
                Debug.LogError("[SetupSurvivalSettings] 未找到 Canvas，请确认场景中存在 Canvas 对象");
                return;
            }

            // ── 2. 创建 / 复用 SurvivalSettingsPanel ─────────────────────────
            var existingPanel = canvasGo.transform.Find("SurvivalSettingsPanel");
            GameObject panelGo;

            if (existingPanel == null)
            {
                panelGo = CreateSettingsPanel(canvasGo.transform);
                created++;
                Debug.Log("[SetupSurvivalSettings] SurvivalSettingsPanel 创建完成 ✅");
            }
            else
            {
                panelGo = existingPanel.gameObject;
                Debug.Log("[SetupSurvivalSettings] SurvivalSettingsPanel 已存在，跳过创建");
            }

            // ── 3. 挂载 SurvivalSettingsUI 到 Canvas ─────────────────────────
            var settingsUI = canvasGo.GetComponent<SurvivalSettingsUI>();
            if (settingsUI == null)
            {
                settingsUI = canvasGo.AddComponent<SurvivalSettingsUI>();
                created++;
                Debug.Log("[SetupSurvivalSettings] SurvivalSettingsUI 挂载到 Canvas ✅");
            }
            else
            {
                Debug.Log("[SetupSurvivalSettings] SurvivalSettingsUI 已存在，跳过挂载");
            }

            // ── 4. 绑定所有字段 ───────────────────────────────────────────────
            {
                var so = new SerializedObject(settingsUI);

                so.FindProperty("_panel")    .objectReferenceValue = panelGo;

                var closeBtn = panelGo.transform.Find("CloseBtn")?.GetComponent<Button>();
                so.FindProperty("_closeBtn") .objectReferenceValue = closeBtn;

                // BGM 行
                var bgmRow = panelGo.transform.Find("BGMRow");
                if (bgmRow != null)
                {
                    so.FindProperty("_bgmSlider")     .objectReferenceValue =
                        bgmRow.Find("BGMSlider")?.GetComponent<Slider>();
                    so.FindProperty("_bgmValueText")  .objectReferenceValue =
                        bgmRow.Find("BGMValueText")?.GetComponent<TextMeshProUGUI>();
                    so.FindProperty("_bgmToggle")     .objectReferenceValue =
                        bgmRow.Find("BGMToggleBtn")?.GetComponent<Button>();
                    so.FindProperty("_bgmToggleText") .objectReferenceValue =
                        bgmRow.Find("BGMToggleBtn/BGMToggleText")?.GetComponent<TextMeshProUGUI>();
                }

                // SFX 行
                var sfxRow = panelGo.transform.Find("SFXRow");
                if (sfxRow != null)
                {
                    so.FindProperty("_sfxSlider")     .objectReferenceValue =
                        sfxRow.Find("SFXSlider")?.GetComponent<Slider>();
                    so.FindProperty("_sfxValueText")  .objectReferenceValue =
                        sfxRow.Find("SFXValueText")?.GetComponent<TextMeshProUGUI>();
                    so.FindProperty("_sfxToggle")     .objectReferenceValue =
                        sfxRow.Find("SFXToggleBtn")?.GetComponent<Button>();
                    so.FindProperty("_sfxToggleText") .objectReferenceValue =
                        sfxRow.Find("SFXToggleBtn/SFXToggleText")?.GetComponent<TextMeshProUGUI>();
                }

                // 版本文字
                so.FindProperty("_versionText").objectReferenceValue =
                    panelGo.transform.Find("VersionText")?.GetComponent<TextMeshProUGUI>();

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(canvasGo);
                Debug.Log("[SetupSurvivalSettings] SurvivalSettingsUI 字段绑定完成 ✅");
            }

            // ── 5. 将 settingsUI 直接绑定到 SurvivalIdleUI._settingsPanel ──────
            var idleUI = canvasGo.GetComponent<SurvivalIdleUI>();
            if (idleUI != null)
            {
                var soIdle = new SerializedObject(idleUI);
                var prop = soIdle.FindProperty("_settingsPanel");
                if (prop != null)
                {
                    prop.objectReferenceValue = settingsUI;
                    soIdle.ApplyModifiedProperties();
                    EditorUtility.SetDirty(canvasGo);
                    Debug.Log("[SetupSurvivalSettings] SurvivalIdleUI._settingsPanel 绑定完成 ✅");
                }
            }

            // ── 7. 保存场景 ───────────────────────────────────────────────────
            if (created > 0)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[SetupSurvivalSettings] ✅ 完成！共创建/挂载 {created} 个对象，场景已保存");
            }
            else
            {
                Debug.Log("[SetupSurvivalSettings] ✅ 所有对象已存在，仅重新绑定字段");
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        // ==================== 面板结构创建 ====================

        private static GameObject CreateSettingsPanel(Transform parent)
        {
            // 根节点（初始 inactive，Rule #2）
            var panel = new GameObject("SurvivalSettingsPanel", typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            panel.SetActive(false);

            // 居中，480×420
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = new Vector2(480f, 420f);

            // ── Background
            CreateBackground(panel.transform);

            // ── TitleText
            CreateTitle(panel.transform);

            // ── CloseBtn (右上角)
            CreateCloseButton(panel.transform);

            // ── BGM 行
            CreateVolumeRow(panel.transform, "BGMRow", "BGM 音乐",
                "BGMToggleBtn", "BGMToggleText",
                "BGMSlider", "BGMValueText",
                new Vector2(0f, 100f));

            // ── SFX 行
            CreateVolumeRow(panel.transform, "SFXRow", "SFX 音效",
                "SFXToggleBtn", "SFXToggleText",
                "SFXSlider", "SFXValueText",
                new Vector2(0f, 0f));

            // ── Divider line (可选装饰)
            CreateDivider(panel.transform, new Vector2(0f, -70f));

            // ── VersionText
            CreateVersionText(panel.transform);

            return panel;
        }

        // ── Background
        private static void CreateBackground(Transform parent)
        {
            var go = new GameObject("Background", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.SetAsFirstSibling(); // 背景在最底层

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.08f, 0.08f, 0.16f, 0.96f); // 深蓝黑，高不透明度
        }

        // ── 标题
        private static void CreateTitle(Transform parent)
        {
            var go = new GameObject("TitleText", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -16f);
            rt.sizeDelta        = new Vector2(0f, 50f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = "设置";
            tmp.fontSize  = 28f;
            tmp.color     = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            ApplyChineseFont(tmp);
        }

        // ── 关闭按钮（右上角）
        private static void CreateCloseButton(Transform parent)
        {
            var go = new GameObject("CloseBtn", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-12f, -12f);
            rt.sizeDelta        = new Vector2(44f, 44f);

            // 背景（半透明圆形）
            var bg = go.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.1f);

            var btn = go.AddComponent<Button>();
            var cb = btn.colors;
            cb.normalColor      = new Color(1f, 1f, 1f, 0.1f);
            cb.highlightedColor = new Color(1f, 0.3f, 0.3f, 0.8f);
            cb.pressedColor     = new Color(0.8f, 0.1f, 0.1f, 1f);
            btn.colors = cb;
            btn.targetGraphic = bg;

            // 文字
            var textGo = new GameObject("CloseText", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = "✕";
            tmp.fontSize  = 22f;
            tmp.color     = new Color(0.9f, 0.9f, 0.9f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
        }

        // ── 音量行（BGM 或 SFX）
        private static void CreateVolumeRow(
            Transform parent,
            string rowName, string labelText,
            string toggleBtnName, string toggleTextName,
            string sliderName, string valueTextName,
            Vector2 position)
        {
            var row = new GameObject(rowName, typeof(RectTransform));
            row.transform.SetParent(parent, false);

            var rowRt = row.GetComponent<RectTransform>();
            rowRt.anchorMin        = new Vector2(0f, 0.5f);
            rowRt.anchorMax        = new Vector2(1f, 0.5f);
            rowRt.pivot            = new Vector2(0.5f, 0.5f);
            rowRt.anchoredPosition = position;
            rowRt.sizeDelta        = new Vector2(-40f, 60f);

            // HorizontalLayoutGroup
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment       = TextAnchor.MiddleLeft;
            hlg.spacing              = 10f;
            hlg.padding              = new RectOffset(10, 10, 0, 0);
            hlg.childControlWidth    = false;
            hlg.childControlHeight   = true;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;

            // Label
            {
                var go = new GameObject("Label", typeof(RectTransform));
                go.transform.SetParent(row.transform, false);
                go.GetComponent<RectTransform>().sizeDelta = new Vector2(100f, 0f);
                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.text      = labelText;
                tmp.fontSize  = 20f;
                tmp.color     = new Color(0.85f, 0.95f, 1f, 1f);
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                ApplyChineseFont(tmp);
            }

            // Toggle Button (🔊/🔇)
            {
                var btnGo = new GameObject(toggleBtnName, typeof(RectTransform));
                btnGo.transform.SetParent(row.transform, false);
                btnGo.GetComponent<RectTransform>().sizeDelta = new Vector2(44f, 44f);

                var btnBg = btnGo.AddComponent<Image>();
                btnBg.color = new Color(0.2f, 0.2f, 0.35f, 0.9f);

                var btn = btnGo.AddComponent<Button>();
                var cb = btn.colors;
                cb.normalColor      = new Color(0.2f, 0.2f, 0.35f, 0.9f);
                cb.highlightedColor = new Color(0.3f, 0.3f, 0.5f, 1f);
                cb.pressedColor     = new Color(0.1f, 0.5f, 0.8f, 1f);
                btn.colors = cb;
                btn.targetGraphic = btnBg;

                var textGo = new GameObject(toggleTextName, typeof(RectTransform));
                textGo.transform.SetParent(btnGo.transform, false);
                var trt = textGo.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = Vector2.zero;
                trt.offsetMax = Vector2.zero;
                var tmp = textGo.AddComponent<TextMeshProUGUI>();
                tmp.text      = "🔊";
                tmp.fontSize  = 22f;
                tmp.color     = Color.white;
                tmp.alignment = TextAlignmentOptions.Center;
            }

            // Slider
            {
                var sliderGo = new GameObject(sliderName, typeof(RectTransform));
                sliderGo.transform.SetParent(row.transform, false);
                sliderGo.GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 0f);

                var slider = sliderGo.AddComponent<Slider>();
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.value    = sliderName.StartsWith("BGM") ? 0.6f : 0.8f;

                // Background
                var bg = new GameObject("Background", typeof(RectTransform));
                bg.transform.SetParent(sliderGo.transform, false);
                var bgRt = bg.GetComponent<RectTransform>();
                bgRt.anchorMin = new Vector2(0f, 0.25f);
                bgRt.anchorMax = new Vector2(1f, 0.75f);
                bgRt.offsetMin = Vector2.zero;
                bgRt.offsetMax = Vector2.zero;
                var bgImg = bg.AddComponent<Image>();
                bgImg.color = new Color(0.15f, 0.15f, 0.25f, 1f);

                // Fill Area
                var fillArea = new GameObject("Fill Area", typeof(RectTransform));
                fillArea.transform.SetParent(sliderGo.transform, false);
                var faRt = fillArea.GetComponent<RectTransform>();
                faRt.anchorMin = new Vector2(0f, 0.25f);
                faRt.anchorMax = new Vector2(1f, 0.75f);
                faRt.offsetMin = new Vector2(5f, 0f);
                faRt.offsetMax = new Vector2(-15f, 0f);

                var fill = new GameObject("Fill", typeof(RectTransform));
                fill.transform.SetParent(fillArea.transform, false);
                var fillRt = fill.GetComponent<RectTransform>();
                fillRt.anchorMin = Vector2.zero;
                fillRt.anchorMax = Vector2.one;
                fillRt.offsetMin = Vector2.zero;
                fillRt.offsetMax = Vector2.zero;
                var fillImg = fill.AddComponent<Image>();
                fillImg.color = new Color(0.2f, 0.7f, 1f, 1f); // 冰蓝

                // Handle Slide Area
                var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
                handleArea.transform.SetParent(sliderGo.transform, false);
                var haRt = handleArea.GetComponent<RectTransform>();
                haRt.anchorMin = Vector2.zero;
                haRt.anchorMax = Vector2.one;
                haRt.offsetMin = new Vector2(10f, 0f);
                haRt.offsetMax = new Vector2(-10f, 0f);

                var handle = new GameObject("Handle", typeof(RectTransform));
                handle.transform.SetParent(handleArea.transform, false);
                var handleRt = handle.GetComponent<RectTransform>();
                handleRt.anchorMin = new Vector2(0f, 0f);
                handleRt.anchorMax = new Vector2(0f, 1f);
                handleRt.pivot     = new Vector2(0.5f, 0.5f);
                handleRt.sizeDelta = new Vector2(20f, 20f);
                var handleImg = handle.AddComponent<Image>();
                handleImg.color = Color.white;

                // 设置 Slider 引用
                slider.fillRect   = fillRt;
                slider.handleRect = handleRt;
                slider.targetGraphic = handleImg;
                slider.direction = Slider.Direction.LeftToRight;
            }

            // Value Text (百分比)
            {
                var go = new GameObject(valueTextName, typeof(RectTransform));
                go.transform.SetParent(row.transform, false);
                go.GetComponent<RectTransform>().sizeDelta = new Vector2(56f, 0f);
                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.text      = sliderName.StartsWith("BGM") ? "60%" : "80%";
                tmp.fontSize  = 18f;
                tmp.color     = new Color(0.8f, 0.9f, 1f, 1f);
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                ApplyChineseFont(tmp);
            }
        }

        // ── 分割线
        private static void CreateDivider(Transform parent, Vector2 position)
        {
            var go = new GameObject("Divider", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.05f, 0.5f);
            rt.anchorMax        = new Vector2(0.95f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta        = new Vector2(0f, 1f);

            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.15f);
        }

        // ── 版本号文字
        private static void CreateVersionText(Transform parent)
        {
            var go = new GameObject("VersionText", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 0f);
            rt.anchorMax        = new Vector2(1f, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 16f);
            rt.sizeDelta        = new Vector2(0f, 40f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = "极地生存法则  v0.1";
            tmp.fontSize  = 15f;
            tmp.color     = new Color(0.55f, 0.65f, 0.75f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
            ApplyChineseFont(tmp);
        }

        // ── 字体
        private static void ApplyChineseFont(TextMeshProUGUI tmp)
        {
            var font = Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
            if (font != null) tmp.font = font;
        }
    }
}
