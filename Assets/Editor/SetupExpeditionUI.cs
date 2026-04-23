using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using DrscfZ.UI;

namespace DrscfZ.Editor
{
    /// <summary>
    /// Tools → DrscfZ → Setup Section 38 Expedition UI
    ///
    /// 一键搭建 §38 探险系统 UI 骨架：
    ///
    ///   Canvas/GameUIPanel/ExpeditionMarkerPanel  (右上角，挂 ExpeditionMarkerUI)
    ///     └─ Container (VLG 容器)
    ///         └─ MarkerTemplate (Image + TMP 倒计时，禁用作 Prefab 模板)
    ///   Canvas/GameUIPanel/TraderCaravanPanel     (居中，挂 TraderCaravanUI)
    ///     ├─ TitleText
    ///     ├─ DescText
    ///     ├─ CountdownText
    ///     ├─ BtnAccept (接受)
    ///     └─ BtnCancel (拒绝)
    /// </summary>
    public static class SetupExpeditionUI
    {
        private const string AlibabaFontPath = "Assets/Resources/Fonts/AlibabaPuHuiTi-3-85-Bold SDF.asset";

        [MenuItem("Tools/DrscfZ/Setup Section 38 Expedition UI")]
        public static void Execute()
        {
            var canvas = FindInLoadedScene("Canvas");
            if (canvas == null)
            {
                Debug.LogError("[SetupExpeditionUI] Canvas 未找到，终止。");
                return;
            }

            var gameUIPanel = GetOrCreateFullscreenPanel(canvas.transform, "GameUIPanel");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AlibabaFontPath);
            if (font == null)
                Debug.LogWarning($"[SetupExpeditionUI] 未能加载字体: {AlibabaFontPath}");

            BuildExpeditionMarkerPanel(gameUIPanel.transform, font);
            BuildTraderCaravanPanel(gameUIPanel.transform, font);

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[SetupExpeditionUI] §38 Expedition UI setup done (ExpeditionMarkerPanel + TraderCaravanPanel)");
        }

        // ==================== ExpeditionMarkerPanel ====================

        private static void BuildExpeditionMarkerPanel(Transform parent, TMP_FontAsset font)
        {
            // 右上角，避让 BuildingStatusPanel（Y=-160~-460），本面板放在更高处或右侧下移
            // 策略：右侧偏下 (-20, -480) 避开 BuildingStatusPanel
            var panel = BuildPanel(parent, "ExpeditionMarkerPanel",
                anchorMin: new Vector2(1f, 1f),
                anchorMax: new Vector2(1f, 1f),
                pivot: new Vector2(1f, 1f),
                anchoredPosition: new Vector2(-20f, -480f),
                sizeDelta: new Vector2(180f, 200f));

            var bg = panel.GetComponent<Image>();
            if (bg == null) bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.25f);
            bg.raycastTarget = false;

            // Container（VLG）
            var container = GetOrCreateChild(panel.transform, "Container", () =>
            {
                var g  = new GameObject("Container");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(panel.transform, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(8f, 8f);
                rt.offsetMax = new Vector2(-8f, -8f);
                return g;
            });

            var vlg = container.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) vlg = container.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment         = TextAnchor.UpperCenter;
            vlg.spacing                = 6f;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = false;
            vlg.padding = new RectOffset(0, 0, 0, 0);

            // MarkerTemplate (50×50 圆 Image + 倒计时 TMP，禁用，作模板)
            var markerTemplate = GetOrCreateChild(container.transform, "MarkerTemplate", () =>
            {
                var g  = new GameObject("MarkerTemplate");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(container.transform, false);
                rt.sizeDelta = new Vector2(160f, 50f);
                return g;
            });

            // 保证 LayoutElement 给 template 显式高度，避免 VLG 压成 0
            var le = markerTemplate.GetComponent<LayoutElement>();
            if (le == null) le = markerTemplate.AddComponent<LayoutElement>();
            le.minHeight       = 50f;
            le.preferredHeight = 50f;

            // Icon (左侧 50×50)
            var iconGo = GetOrCreateChild(markerTemplate.transform, "Icon", () =>
            {
                var g  = new GameObject("Icon");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(markerTemplate.transform, false);
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot     = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, 0f);
                rt.sizeDelta = new Vector2(50f, 50f);
                return g;
            });
            var iconImg = iconGo.GetComponent<Image>();
            if (iconImg == null) iconImg = iconGo.AddComponent<Image>();
            iconImg.color = new Color(1f, 0.7f, 0.3f, 1f);
            iconImg.raycastTarget = false;

            // Countdown TMP
            var cdTmp = BuildChildTMP(markerTemplate.transform, "Countdown",
                anchorMin: new Vector2(0f, 0f),
                anchorMax: new Vector2(1f, 1f),
                offsetMin: new Vector2(58f, 0f),
                offsetMax: new Vector2(0f, 0f),
                fontSize: 20f,
                align: TextAlignmentOptions.MidlineLeft,
                font: font,
                color: Color.white);
            cdTmp.text = "90s";

            // 模板默认禁用（运行时 Instantiate 使用）
            markerTemplate.SetActive(false);

            var ui = panel.GetComponent<ExpeditionMarkerUI>();
            if (ui == null) ui = panel.AddComponent<ExpeditionMarkerUI>();

            var so = new SerializedObject(ui);
            TryBind(so, "_markerContainer", container.GetComponent<RectTransform>());
            TryBind(so, "_markerPrefab",    markerTemplate);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);

            Debug.Log("[SetupExpeditionUI] ExpeditionMarkerPanel done (Container VLG + MarkerTemplate)");
        }

        // ==================== TraderCaravanPanel ====================

        private static void BuildTraderCaravanPanel(Transform parent, TMP_FontAsset font)
        {
            var panel = BuildPanel(parent, "TraderCaravanPanel",
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                pivot: new Vector2(0.5f, 0.5f),
                anchoredPosition: Vector2.zero,
                sizeDelta: new Vector2(500f, 300f));

            var bg = panel.GetComponent<Image>();
            if (bg == null) bg = panel.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.08f, 0.2f, 0.92f);
            bg.raycastTarget = true;

            // TitleText
            var titleTmp = BuildChildTMP(panel.transform, "TitleText",
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(1f, 1f),
                offsetMin: new Vector2(16f, -70f),
                offsetMax: new Vector2(-16f, -20f),
                fontSize: 32f,
                align: TextAlignmentOptions.Center,
                font: font,
                color: new Color(1f, 0.84f, 0.2f, 1f));
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.text = "商队交易";

            // DescText
            var descTmp = BuildChildTMP(panel.transform, "DescText",
                anchorMin: new Vector2(0f, 0f),
                anchorMax: new Vector2(1f, 1f),
                offsetMin: new Vector2(16f, 100f),
                offsetMax: new Vector2(-16f, -80f),
                fontSize: 22f,
                align: TextAlignmentOptions.Center,
                font: font,
                color: Color.white);
            descTmp.text = "主播决定：\n接受 200食物 + 50矿石 → 城门立即 Lv+1\n拒绝 放弃本次交易";

            // CountdownText
            var cdTmp = BuildChildTMP(panel.transform, "CountdownText",
                anchorMin: new Vector2(1f, 1f),
                anchorMax: new Vector2(1f, 1f),
                offsetMin: new Vector2(-100f, -70f),
                offsetMax: new Vector2(-16f, -20f),
                fontSize: 26f,
                align: TextAlignmentOptions.MidlineRight,
                font: font,
                color: new Color(1f, 0.5f, 0.5f, 1f));
            cdTmp.text = "30s";

            // BtnAccept（左下）
            var btnAccept = BuildButton(panel.transform, "BtnAccept",
                anchorMin: new Vector2(0.1f, 0f),
                anchorMax: new Vector2(0.45f, 0f),
                pivot: new Vector2(0.5f, 0f),
                anchoredPosition: new Vector2(0f, 30f),
                sizeDelta: new Vector2(0f, 60f),
                labelText: "接受",
                bgColor: new Color(0.3f, 0.75f, 0.35f, 1f),
                labelColor: Color.white,
                font: font);

            // BtnCancel（右下）
            var btnCancel = BuildButton(panel.transform, "BtnCancel",
                anchorMin: new Vector2(0.55f, 0f),
                anchorMax: new Vector2(0.9f, 0f),
                pivot: new Vector2(0.5f, 0f),
                anchoredPosition: new Vector2(0f, 30f),
                sizeDelta: new Vector2(0f, 60f),
                labelText: "拒绝",
                bgColor: new Color(0.6f, 0.3f, 0.3f, 1f),
                labelColor: Color.white,
                font: font);

            var ui = panel.GetComponent<TraderCaravanUI>();
            if (ui == null) ui = panel.AddComponent<TraderCaravanUI>();

            var so = new SerializedObject(ui);
            TryBind(so, "_panel", panel);
            TryBind(so, "btnAccept", btnAccept);
            TryBind(so, "btnCancel", btnCancel);
            TryBind(so, "_titleText", titleTmp);
            TryBind(so, "_descText", descTmp);
            TryBind(so, "_countdownText", cdTmp);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);

            // 默认隐藏（由 TraderCaravanUI.Show 在收到事件时激活）
            panel.SetActive(false);

            Debug.Log("[SetupExpeditionUI] TraderCaravanPanel done (Title/Desc/CD + Accept/Cancel)");
        }

        // ==================== 辅助方法 ====================

        private static Button BuildButton(Transform parent, string name,
                                          Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
                                          Vector2 anchoredPosition, Vector2 sizeDelta,
                                          string labelText, Color bgColor, Color labelColor,
                                          TMP_FontAsset font)
        {
            var go = GetOrCreateChild(parent, name, () =>
            {
                var g  = new GameObject(name);
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(parent, false);
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.pivot     = pivot;
                rt.anchoredPosition = anchoredPosition;
                rt.sizeDelta = sizeDelta;
                return g;
            });

            var rt2 = go.GetComponent<RectTransform>();
            if (rt2 != null)
            {
                rt2.anchorMin = anchorMin;
                rt2.anchorMax = anchorMax;
                rt2.pivot     = pivot;
                rt2.anchoredPosition = anchoredPosition;
                rt2.sizeDelta = sizeDelta;
            }

            var img = go.GetComponent<Image>();
            if (img == null) img = go.AddComponent<Image>();
            img.color = bgColor;
            img.raycastTarget = true;

            var btn = go.GetComponent<Button>();
            if (btn == null) btn = go.AddComponent<Button>();

            var labelTmp = BuildChildTMP(go.transform, "Label",
                anchorMin: Vector2.zero,
                anchorMax: Vector2.one,
                offsetMin: new Vector2(4f, 4f),
                offsetMax: new Vector2(-4f, -4f),
                fontSize: 24f,
                align: TextAlignmentOptions.Center,
                font: font,
                color: labelColor);
            labelTmp.fontStyle = FontStyles.Bold;
            labelTmp.text = labelText;

            return btn;
        }

        private static GameObject BuildPanel(Transform parent, string name,
                                             Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
                                             Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var go = GetOrCreateChild(parent, name, () =>
            {
                var g  = new GameObject(name);
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(parent, false);
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.pivot     = pivot;
                rt.anchoredPosition = anchoredPosition;
                rt.sizeDelta = sizeDelta;
                return g;
            });

            var rt2 = go.GetComponent<RectTransform>();
            if (rt2 != null)
            {
                rt2.anchorMin = anchorMin;
                rt2.anchorMax = anchorMax;
                rt2.pivot     = pivot;
                rt2.anchoredPosition = anchoredPosition;
                rt2.sizeDelta = sizeDelta;
            }
            return go;
        }

        private static TextMeshProUGUI BuildChildTMP(Transform parent, string name,
                                                     Vector2 anchorMin, Vector2 anchorMax,
                                                     Vector2 offsetMin, Vector2 offsetMax,
                                                     float fontSize, TextAlignmentOptions align,
                                                     TMP_FontAsset font, Color color)
        {
            var go = GetOrCreateChild(parent, name, () =>
            {
                var g  = new GameObject(name);
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(parent, false);
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.offsetMin = offsetMin;
                rt.offsetMax = offsetMax;
                return g;
            });

            var rt2 = go.GetComponent<RectTransform>();
            if (rt2 != null)
            {
                rt2.anchorMin = anchorMin;
                rt2.anchorMax = anchorMax;
                rt2.offsetMin = offsetMin;
                rt2.offsetMax = offsetMax;
            }

            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize         = fontSize;
            tmp.alignment        = align;
            tmp.enableAutoSizing = false;
            tmp.raycastTarget    = false;
            if (font != null)
            {
                var soTmp = new SerializedObject(tmp);
                var fontProp = soTmp.FindProperty("m_fontAsset");
                if (fontProp != null) fontProp.objectReferenceValue = font;
                soTmp.ApplyModifiedPropertiesWithoutUndo();
            }
            tmp.text = "";
            SetTMPColor(tmp, color);
            return tmp;
        }

        private static void SetTMPColor(TextMeshProUGUI tmp, Color c)
        {
            if (tmp == null) return;
            var so = new SerializedObject(tmp);
            var p1 = so.FindProperty("m_fontColor");
            var p2 = so.FindProperty("m_fontColor32");
            if (p1 != null) p1.colorValue = c;
            if (p2 != null) p2.colorValue = c;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tmp);
        }

        private static void TryBind(SerializedObject so, string fieldName, UnityEngine.Object value)
        {
            if (so == null || string.IsNullOrEmpty(fieldName)) return;
            var p = so.FindProperty(fieldName);
            if (p == null)
            {
                Debug.LogWarning($"[SetupExpeditionUI] TryBind: 字段 {fieldName} 未找到");
                return;
            }
            p.objectReferenceValue = value;
        }

        private static GameObject GetOrCreateFullscreenPanel(Transform parent, string name)
        {
            return GetOrCreateChild(parent, name, () =>
            {
                var g  = new GameObject(name);
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(parent, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                Debug.LogWarning($"[SetupExpeditionUI] {name} 占位建出（原场景缺失）。");
                return g;
            });
        }

        private static GameObject GetOrCreateChild(Transform parent, string name, System.Func<GameObject> creator)
        {
            if (parent == null) return creator();
            var found = parent.Find(name);
            if (found != null) return found.gameObject;
            return creator();
        }

        private static GameObject FindInLoadedScene(string name)
        {
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.name == name && go.scene.isLoaded && !IsPrefabAsset(go))
                    return go;
            }
            return null;
        }

        private static bool IsPrefabAsset(GameObject go)
        {
            return go != null && (go.hideFlags & HideFlags.HideAndDontSave) != 0
                   && !go.scene.IsValid();
        }
    }
}
