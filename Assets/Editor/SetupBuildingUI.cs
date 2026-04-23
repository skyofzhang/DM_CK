using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using DrscfZ.UI;

namespace DrscfZ.Editor
{
    /// <summary>
    /// Tools → DrscfZ → Setup Section 37 Building UI
    ///
    /// 一键搭建 §37 建造系统 UI 骨架：
    ///
    ///   Canvas/GameUIPanel/BuildingStatusPanel   (右上角，挂 BuildingStatusPanelUI)
    ///     ├─ Row_Watchtower (Dot + Label + Percent)
    ///     ├─ Row_Market
    ///     ├─ Row_Hospital
    ///     ├─ Row_Altar
    ///     └─ Row_Beacon
    ///   Canvas/GameUIPanel/BuildVotePanel         (居中，挂 BuildVoteUI)
    ///     ├─ ProposerText
    ///     ├─ TimerText
    ///     ├─ Button_Vote1 (Label + Count)
    ///     ├─ Button_Vote2 (Label + Count)
    ///     └─ Button_Vote3 (Label + Count)
    ///
    /// 兜底策略（对齐 CLAUDE.md + docs/multi_agent_workflow.md）：
    ///   缺 GameObject → 占位建出（不跳过）
    ///   禁用 EditorUtility.DisplayDialog（CLAUDE.md 踩坑）
    ///   SerializedObject 写 m_fontColor + m_fontColor32（TMP 颜色踩坑）
    ///   AddComponent 显式 null 判空，幂等可重复跑
    /// </summary>
    public static class SetupBuildingUI
    {
        private const string AlibabaFontPath = "Assets/Resources/Fonts/AlibabaPuHuiTi-3-85-Bold SDF.asset";

        private static readonly string[] BuildingIds    = { "watchtower", "market", "hospital", "altar", "beacon" };
        private static readonly string[] BuildingLabels = { "瞭望塔",      "市场",   "医院",     "祭坛",  "烽火台" };

        [MenuItem("Tools/DrscfZ/Setup Section 37 Building UI")]
        public static void Execute()
        {
            var canvas = FindInLoadedScene("Canvas");
            if (canvas == null)
            {
                Debug.LogError("[SetupBuildingUI] Canvas 未找到，终止。");
                return;
            }

            var gameUIPanel = GetOrCreateFullscreenPanel(canvas.transform, "GameUIPanel");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AlibabaFontPath);
            if (font == null)
                Debug.LogWarning($"[SetupBuildingUI] 未能加载字体: {AlibabaFontPath}（TMP 可能显示方块）");

            BuildBuildingStatusPanel(gameUIPanel.transform, font);
            BuildBuildVotePanel(gameUIPanel.transform, font);

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[SetupBuildingUI] §37 Building UI setup done (BuildingStatusPanel + BuildVotePanel)");
        }

        // ==================== BuildingStatusPanel ====================

        private static void BuildBuildingStatusPanel(Transform parent, TMP_FontAsset font)
        {
            // 右上角面板（避让 TopBar 预留 140px）
            var panel = BuildPanel(parent, "BuildingStatusPanel",
                anchorMin: new Vector2(1f, 1f),
                anchorMax: new Vector2(1f, 1f),
                pivot: new Vector2(1f, 1f),
                anchoredPosition: new Vector2(-20f, -160f),
                sizeDelta: new Vector2(200f, 300f));

            // 半透明背景
            var bg = panel.GetComponent<Image>();
            if (bg == null) bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.35f);
            bg.raycastTarget = false;

            // 5 行：Row_Xxx
            var dots     = new Image[5];
            var labels   = new TMP_Text[5];
            var percents = new TMP_Text[5];
            const float rowHeight = 52f;
            const float topPadding = 8f;

            for (int i = 0; i < 5; i++)
            {
                float yOffset = -(topPadding + i * rowHeight + rowHeight / 2f);
                string rowName = $"Row_{char.ToUpperInvariant(BuildingIds[i][0])}{BuildingIds[i].Substring(1)}";
                var row = BuildPanel(panel.transform, rowName,
                    anchorMin: new Vector2(0f, 1f),
                    anchorMax: new Vector2(1f, 1f),
                    pivot: new Vector2(0.5f, 0.5f),
                    anchoredPosition: new Vector2(0f, yOffset),
                    sizeDelta: new Vector2(0f, rowHeight - 4f));

                // Dot (20×20 圆形)
                var dotGo = GetOrCreateChild(row.transform, "Dot", () =>
                {
                    var g  = new GameObject("Dot");
                    var rt = g.AddComponent<RectTransform>();
                    g.transform.SetParent(row.transform, false);
                    rt.anchorMin = new Vector2(0f, 0.5f);
                    rt.anchorMax = new Vector2(0f, 0.5f);
                    rt.pivot     = new Vector2(0f, 0.5f);
                    rt.anchoredPosition = new Vector2(8f, 0f);
                    rt.sizeDelta = new Vector2(20f, 20f);
                    return g;
                });
                var dotImg = dotGo.GetComponent<Image>();
                if (dotImg == null) dotImg = dotGo.AddComponent<Image>();
                dotImg.color = new Color(0.35f, 0.35f, 0.35f, 1f);
                dotImg.raycastTarget = false;
                dots[i] = dotImg;

                // Label（中文名）
                var labelTmp = BuildChildTMP(row.transform, "Label",
                    anchorMin: new Vector2(0f, 0f),
                    anchorMax: new Vector2(0.65f, 1f),
                    offsetMin: new Vector2(36f, 2f),
                    offsetMax: new Vector2(0f, -2f),
                    fontSize: 22f,
                    align: TextAlignmentOptions.MidlineLeft,
                    font: font,
                    color: Color.white);
                labelTmp.text = BuildingLabels[i];
                labels[i] = labelTmp;

                // Percent（右侧进度）
                var percentTmp = BuildChildTMP(row.transform, "Percent",
                    anchorMin: new Vector2(0.65f, 0f),
                    anchorMax: new Vector2(1f, 1f),
                    offsetMin: new Vector2(0f, 2f),
                    offsetMax: new Vector2(-8f, -2f),
                    fontSize: 20f,
                    align: TextAlignmentOptions.MidlineRight,
                    font: font,
                    color: new Color(1f, 0.9f, 0.4f, 1f));
                percentTmp.text = "";
                percents[i] = percentTmp;
            }

            var ui = panel.GetComponent<BuildingStatusPanelUI>();
            if (ui == null) ui = panel.AddComponent<BuildingStatusPanelUI>();

            var so = new SerializedObject(ui);
            TryBind(so, "_root", panel);
            TryBindArray(so, "_buildingDots", dots);
            TryBindArray(so, "_buildingLabels", labels);
            TryBindArray(so, "_buildingPercents", percents);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);

            Debug.Log("[SetupBuildingUI] BuildingStatusPanel done (5 rows + BuildingStatusPanelUI bound)");
        }

        // ==================== BuildVotePanel ====================

        private static void BuildBuildVotePanel(Transform parent, TMP_FontAsset font)
        {
            var panel = BuildPanel(parent, "BuildVotePanel",
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                pivot: new Vector2(0.5f, 0.5f),
                anchoredPosition: Vector2.zero,
                sizeDelta: new Vector2(600f, 400f));

            // 背景
            var bg = panel.GetComponent<Image>();
            if (bg == null) bg = panel.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.18f, 0.92f);
            bg.raycastTarget = true;

            // Proposer 文案
            var proposerTmp = BuildChildTMP(panel.transform, "ProposerText",
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(1f, 1f),
                offsetMin: new Vector2(16f, -70f),
                offsetMax: new Vector2(-16f, -20f),
                fontSize: 30f,
                align: TextAlignmentOptions.Center,
                font: font,
                color: new Color(1f, 0.84f, 0.2f, 1f));
            proposerTmp.fontStyle = FontStyles.Bold;
            proposerTmp.text = "发起建造投票";

            // Timer 文案
            var timerTmp = BuildChildTMP(panel.transform, "TimerText",
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(1f, 1f),
                offsetMin: new Vector2(16f, -110f),
                offsetMax: new Vector2(-16f, -75f),
                fontSize: 24f,
                align: TextAlignmentOptions.Center,
                font: font,
                color: Color.white);
            timerTmp.text = "0 秒";

            // 3 个投票按钮
            var voteButtons = new Button[3];
            var voteLabels  = new TMP_Text[3];
            var voteCounts  = new TMP_Text[3];
            const float btnWidth = 160f;
            const float btnHeight = 200f;
            const float spacing = 20f;
            float totalWidth = 3 * btnWidth + 2 * spacing;
            float startX = -totalWidth / 2f + btnWidth / 2f;

            for (int i = 0; i < 3; i++)
            {
                float posX = startX + i * (btnWidth + spacing);
                var btnGo = GetOrCreateChild(panel.transform, $"VoteButton_{i + 1}", () =>
                {
                    var g  = new GameObject($"VoteButton_{i + 1}");
                    var rt = g.AddComponent<RectTransform>();
                    g.transform.SetParent(panel.transform, false);
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot     = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(posX, -20f);
                    rt.sizeDelta = new Vector2(btnWidth, btnHeight);
                    return g;
                });

                var btnRt = btnGo.GetComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(0.5f, 0.5f);
                btnRt.anchorMax = new Vector2(0.5f, 0.5f);
                btnRt.pivot     = new Vector2(0.5f, 0.5f);
                btnRt.anchoredPosition = new Vector2(posX, -20f);
                btnRt.sizeDelta = new Vector2(btnWidth, btnHeight);

                var btnImg = btnGo.GetComponent<Image>();
                if (btnImg == null) btnImg = btnGo.AddComponent<Image>();
                btnImg.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                btnImg.raycastTarget = true;

                var btn = btnGo.GetComponent<Button>();
                if (btn == null) btn = btnGo.AddComponent<Button>();
                voteButtons[i] = btn;

                // VoteLabel（建筑名）
                var voteLabelTmp = BuildChildTMP(btnGo.transform, "VoteLabel",
                    anchorMin: new Vector2(0f, 0.5f),
                    anchorMax: new Vector2(1f, 1f),
                    offsetMin: new Vector2(4f, 4f),
                    offsetMax: new Vector2(-4f, -4f),
                    fontSize: 26f,
                    align: TextAlignmentOptions.Center,
                    font: font,
                    color: Color.black);
                voteLabelTmp.fontStyle = FontStyles.Bold;
                voteLabelTmp.text = "建筑";
                voteLabels[i] = voteLabelTmp;

                // VoteCount（票数）
                var voteCountTmp = BuildChildTMP(btnGo.transform, "VoteCount",
                    anchorMin: new Vector2(0f, 0f),
                    anchorMax: new Vector2(1f, 0.5f),
                    offsetMin: new Vector2(4f, 4f),
                    offsetMax: new Vector2(-4f, -4f),
                    fontSize: 22f,
                    align: TextAlignmentOptions.Center,
                    font: font,
                    color: new Color(0.2f, 0.2f, 0.2f, 1f));
                voteCountTmp.text = "0 票";
                voteCounts[i] = voteCountTmp;
            }

            var ui = panel.GetComponent<BuildVoteUI>();
            if (ui == null) ui = panel.AddComponent<BuildVoteUI>();

            var so = new SerializedObject(ui);
            TryBind(so, "_panel", panel);
            TryBind(so, "_timerText", timerTmp);
            TryBind(so, "_proposerText", proposerTmp);
            TryBindArray(so, "_voteButtons", voteButtons);
            TryBindArray(so, "_voteLabels", voteLabels);
            TryBindArray(so, "_voteCounts", voteCounts);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);

            // 默认隐藏（由 BuildVoteUI.ShowVote 在收到投票消息时激活）
            panel.SetActive(false);

            Debug.Log("[SetupBuildingUI] BuildVotePanel done (3 vote buttons + BuildVoteUI bound)");
        }

        // ==================== 辅助方法 ====================

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
                // 用 SerializedObject 写 fontAsset（避开 AddComponent 后 faceColor setter 的 null material 问题）
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
                Debug.LogWarning($"[SetupBuildingUI] TryBind: 字段 {fieldName} 未找到");
                return;
            }
            p.objectReferenceValue = value;
        }

        private static void TryBindArray<T>(SerializedObject so, string fieldName, T[] values) where T : UnityEngine.Object
        {
            if (so == null || string.IsNullOrEmpty(fieldName) || values == null) return;
            var p = so.FindProperty(fieldName);
            if (p == null || !p.isArray)
            {
                Debug.LogWarning($"[SetupBuildingUI] TryBindArray: 字段 {fieldName} 未找到或非数组");
                return;
            }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
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
                Debug.LogWarning($"[SetupBuildingUI] {name} 占位建出（原场景缺失）。");
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
