using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using DrscfZ.UI;

namespace DrscfZ.Editor
{
    /// <summary>
    /// Tools → DrscfZ → Setup Section 33 Supporter UI
    ///
    /// 一键搭建 §33 助威模式 UI 骨架：
    ///
    ///   Canvas/GameUIPanel (挂 SupporterTopBarUI + SupporterNightFlashUI)
    ///     ├─ SupporterTopBarLabel         (SupporterTopBarUI._label 的 TMP 子节点)
    ///     ├─ SupporterMarquee             (挂 SupporterMarqueeUI + 独立 TMP)
    ///     ├─ SupporterPromotedMarquee     (挂 SupporterPromotedMarqueeUI + 独立 TMP)
    ///     └─ SupporterJoinedToast         (挂 SupporterJoinedToastUI + 独立 TMP)
    ///
    /// 兜底策略（对齐 CLAUDE.md + docs/multi_agent_workflow.md）：
    ///   缺 GameObject → 占位建出（不跳过）
    ///   禁用 EditorUtility.DisplayDialog（CLAUDE.md 踩坑）
    ///   SerializedObject 写 m_fontColor + m_fontColor32（CLAUDE.md TMP 颜色踩坑）
    ///   AddOrGetComponent 显式 null 判空（禁用 fake-null 陷阱）
    ///   不自动 SaveScene（CLAUDE.md 禁止），由 PM 统一跑 MCP manage_scene 或 SaveCurrentScene.Execute()
    ///
    /// 可视层级（GameUIPanel 子节点，anchoredPosition 避开既有 UI）：
    ///   SupporterTopBarLabel     顶部 Y=-50（TopBar 之下 50px，不遮挡倒计时）
    ///   SupporterJoinedToast     顶部 Y=-110（TopBarLabel 之下 60px）
    ///   SupporterMarquee         底部 Y=340（GiftIconBar 160 + PersonalContribBar 36+10 + 留白 130 之上）
    ///   SupporterPromotedMarquee 底部 Y=400（SupporterMarquee 之上 60）
    /// </summary>
    public static class SetupSection33SupporterUI
    {
        private const string ChineseFontPath = "Assets/Resources/Fonts/AlibabaPuHuiTi-3-85-Bold SDF.asset";

        [MenuItem("Tools/DrscfZ/Setup Section 33 Supporter UI")]
        public static void Execute()
        {
            // ---- 1. 找 Canvas ----
            var canvas = FindInLoadedScene("Canvas");
            if (canvas == null)
            {
                Debug.LogError("[SetupSection33] Canvas 未找到，终止。");
                return;
            }

            // ---- 2. 找/建 GameUIPanel ----
            var gameUIPanel = GetOrCreateFullscreenPanel(canvas.transform, "GameUIPanel");

            // ---- 3. 找中文字体（失败也继续） ----
            var chineseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ChineseFontPath);
            if (chineseFont == null)
                Debug.LogWarning($"[SetupSection33] 未能加载中文字体: {ChineseFontPath}（TMP 可能显示方块）");

            // ---- 4. GameUIPanel 本体挂 SupporterTopBarUI + SupporterNightFlashUI ----
            var topBarUI      = AddOrGetComponent<SupporterTopBarUI>(gameUIPanel);
            var nightFlashUI  = AddOrGetComponent<SupporterNightFlashUI>(gameUIPanel);

            // SupporterTopBarUI 的 _label：建 SupporterTopBarLabel 子 TMP
            var topBarTmp = BuildTopBarLabel(gameUIPanel.transform, chineseFont);
            BindField(topBarUI, "_label", topBarTmp);

            // NightFlashUI 无 UI 字段；仅订阅。
            EditorUtility.SetDirty(nightFlashUI);

            // ---- 5. SupporterMarquee 子 GO（浅紫跑马灯）----
            var marqueeGo   = BuildChildPanel(gameUIPanel.transform, "SupporterMarquee",
                                              new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                                              new Vector2(0.5f, 0f), new Vector2(0f, 340f),
                                              new Vector2(900f, 40f));
            var marqueeTmp  = BuildChildTMP(marqueeGo.transform, "Label",
                                            new Color(0.7f, 0.5f, 1.0f, 1f),
                                            26f, TextAlignmentOptions.Center, chineseFont);
            var marqueeUI   = AddOrGetComponent<SupporterMarqueeUI>(marqueeGo);
            BindField(marqueeUI, "_label", marqueeTmp);

            // ---- 6. SupporterPromotedMarquee 子 GO（金黄跑马灯）----
            var promotedGo  = BuildChildPanel(gameUIPanel.transform, "SupporterPromotedMarquee",
                                              new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                                              new Vector2(0.5f, 0f), new Vector2(0f, 400f),
                                              new Vector2(900f, 40f));
            var promotedTmp = BuildChildTMP(promotedGo.transform, "Label",
                                            new Color(1f, 0.84f, 0.2f, 1f),
                                            26f, TextAlignmentOptions.Center, chineseFont);
            var promotedUI  = AddOrGetComponent<SupporterPromotedMarqueeUI>(promotedGo);
            BindField(promotedUI, "_label", promotedTmp);

            // ---- 7. SupporterJoinedToast 子 GO（绿色 Toast）----
            var toastGo     = BuildChildPanel(gameUIPanel.transform, "SupporterJoinedToast",
                                              new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                              new Vector2(0.5f, 1f), new Vector2(0f, -110f),
                                              new Vector2(700f, 50f));
            var toastTmp    = BuildChildTMP(toastGo.transform, "Label",
                                            new Color(0.5f, 1f, 0.5f, 1f),
                                            24f, TextAlignmentOptions.Center, chineseFont);
            var toastUI     = AddOrGetComponent<SupporterJoinedToastUI>(toastGo);
            BindField(toastUI, "_label", toastTmp);

            // ---- 8. 标脏场景 ----
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[SetupSection33] Supporter UI setup done (T:SupporterTopBar/M:Marquee/P:Promoted/J:Joined/N:NightFlash)");
        }

        // ==================== 子 TMP & 面板构建 ====================

        /// <summary>顶部"守护者:X  助威:Y"并存 TMP（无背景，避开既有 SurvivalTopBarUI.playerCountText）</summary>
        private static TextMeshProUGUI BuildTopBarLabel(Transform parent, TMP_FontAsset font)
        {
            var go = GetOrCreateChild(parent, "SupporterTopBarLabel", () =>
            {
                var g  = new GameObject("SupporterTopBarLabel");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(parent, false);
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot     = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -50f);
                rt.sizeDelta = new Vector2(600f, 34f);
                return g;
            });

            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize         = 24f;
            tmp.alignment        = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = false;
            tmp.raycastTarget    = false;
            if (font != null) tmp.font = font;
            tmp.text = "";
            SetTMPColor(tmp, Color.white);
            return tmp;
        }

        /// <summary>通用空面板：返回 GameObject（不挂 Image，纯容器）。
        /// 通过 anchorMin/anchorMax + pivot + anchoredPosition + sizeDelta 精确定位。</summary>
        private static GameObject BuildChildPanel(Transform parent, string name,
                                                  Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
                                                  Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var go = GetOrCreateChild(parent, name, () =>
            {
                var g  = new GameObject(name);
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(parent, false);
                rt.anchorMin        = anchorMin;
                rt.anchorMax        = anchorMax;
                rt.pivot            = pivot;
                rt.anchoredPosition = anchoredPosition;
                rt.sizeDelta        = sizeDelta;
                return g;
            });

            // 确保 RectTransform 参数正确（场景已存在时也同步）
            var rt2 = go.GetComponent<RectTransform>();
            if (rt2 != null)
            {
                rt2.anchorMin        = anchorMin;
                rt2.anchorMax        = anchorMax;
                rt2.pivot            = pivot;
                rt2.anchoredPosition = anchoredPosition;
                rt2.sizeDelta        = sizeDelta;
            }
            return go;
        }

        /// <summary>建一个子 TMP 填满父级（0~1 锚点），返回 TextMeshProUGUI。
        /// 颜色通过 SerializedObject 写 m_fontColor + m_fontColor32（CLAUDE.md 踩坑）。</summary>
        private static TextMeshProUGUI BuildChildTMP(Transform parent, string name, Color color,
                                                     float fontSize, TextAlignmentOptions align,
                                                     TMP_FontAsset font)
        {
            var go = GetOrCreateChild(parent, name, () =>
            {
                var g  = new GameObject(name);
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(parent, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(8f, 0f);
                rt.offsetMax = new Vector2(-8f, 0f);
                return g;
            });

            var rt2 = go.GetComponent<RectTransform>();
            if (rt2 != null)
            {
                rt2.anchorMin = Vector2.zero;
                rt2.anchorMax = Vector2.one;
                rt2.offsetMin = new Vector2(8f, 0f);
                rt2.offsetMax = new Vector2(-8f, 0f);
            }

            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize         = fontSize;
            tmp.alignment        = align;
            tmp.enableAutoSizing = false;
            tmp.raycastTarget    = false;
            tmp.fontStyle        = FontStyles.Bold;
            if (font != null) tmp.font = font;
            tmp.text = "";
            SetTMPColor(tmp, color);
            return tmp;
        }

        // ==================== 辅助方法 ====================

        /// <summary>将 TMP 的颜色同时写入 m_fontColor + m_fontColor32（绕开 faceColor 默认白问题）。</summary>
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

        /// <summary>显式 null 判空 + AddComponent（禁用 GetComponent&lt;T&gt;() ?? AddComponent 的 fake-null 陷阱）。</summary>
        private static T AddOrGetComponent<T>(GameObject go) where T : Component
        {
            if (go == null) return null;
            var comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }

        /// <summary>通过 SerializedObject 绑定字段到指定组件。</summary>
        private static void BindField(Object target, string fieldName, Object value)
        {
            if (target == null || string.IsNullOrEmpty(fieldName)) return;
            var so = new SerializedObject(target);
            var p  = so.FindProperty(fieldName);
            if (p == null)
            {
                Debug.LogWarning($"[SetupSection33] BindField: 字段 {fieldName} 未找到（组件：{target.GetType().Name}）");
                return;
            }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        /// <summary>创建一个覆盖全屏的面板（若存在则直接返回）。</summary>
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
                Debug.LogWarning($"[SetupSection33] {name} 占位建出（原场景缺失）。");
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

        /// <summary>Resources.FindObjectsOfTypeAll 遍历，返回 scene.isLoaded 的 GameObject（包含非激活）。</summary>
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
