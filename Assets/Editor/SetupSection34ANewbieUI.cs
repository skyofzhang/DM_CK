using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using DrscfZ.UI;

namespace DrscfZ.Editor
{
    /// <summary>
    /// Tools → DrscfZ → Setup §34A Newbie UI
    ///
    /// 一键搭建 §34 Layer 2 组 A 新手友好（B1/B5/B8/B9）UI 骨架：
    ///
    ///   Canvas/StatusLineBanner                            (B1 顶部居中一句话 + StatusLineBannerUI)
    ///   Canvas/OreRepairFloatRoot                          (B5 仅空容器，OreRepairFloatingText 挂载点)
    ///   Canvas/FairyWandMaxedBanner                        (B8d 全屏金闪 + 跑马灯 + FairyWandMaxedBanner)
    ///   Canvas/GameUIPanel/PersonalContribBar              (B9 左下角个人贡献 + PersonalContribUI)
    ///
    /// B8a FairyWandFloatingText / B8b FairyWandStardustTrail / B8c FairyWandAccumUI 无 UI 节点，
    ///   挂在常驻 GO 即可；本脚本把它们挂到 Canvas 上（不影响显示，仅事件驱动）。
    ///
    /// 兜底策略（对齐 docs/multi_agent_workflow.md 决策 6 "MCP 优先 + Console 监控"）：
    ///   缺 GameObject → 占位建出（不跳过）
    ///   禁用 EditorUtility.DisplayDialog（CLAUDE.md 踩坑）
    ///   SerializedObject 写 m_fontColor + m_fontColor32（CLAUDE.md TMP 颜色踩坑）
    ///   AddComponent&lt;RectTransform&gt; 显式写，GameObject 构造后立即加（CLAUDE.md UI 布局踩坑）
    ///   不自动 SaveScene，由 PM 通过 UnityMCP manage_scene action='save' 或 SaveCurrentScene.Execute() 保存
    ///
    /// 可视层级：
    ///   StatusLineBanner 顶部 Y=-80（避开 TopBarUI 100px 余量）
    ///   FairyWandMaxedBanner 全屏盖层（子节点 _flashRoot / _marqueeRoot 初始 inactive）
    ///   PersonalContribBar Y=170（GiftIconBar 高 160px，留 10px 间距）
    /// </summary>
    public static class SetupSection34ANewbieUI
    {
        private const string ChineseFontPath = "Assets/Resources/Fonts/ChineseFont SDF.asset";

        [MenuItem("Tools/DrscfZ/Setup §34A Newbie UI")]
        public static void Run()
        {
            // ---- 1. 找 Canvas ----
            var canvas = FindInLoadedScene("Canvas");
            if (canvas == null)
            {
                Debug.LogError("[Setup34A] Canvas 未找到，终止。");
                return;
            }

            // ---- 2. 找/建 GameUIPanel ----
            var gameUIPanel = GetOrCreateFullscreenPanel(canvas.transform, "GameUIPanel");

            // ---- 3. 找中文字体（失败也继续） ----
            var chineseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ChineseFontPath);
            if (chineseFont == null)
                Debug.LogWarning($"[Setup34A] 未能加载中文字体: {ChineseFontPath}（TMP 可能显示方块）");

            // ---- 4. 分别搭建 4 个模块 ----
            BuildStatusLineBanner    (canvas.transform, chineseFont);
            BuildOreRepairFloatRoot  (canvas.transform);
            BuildFairyWandMaxedBanner(canvas.transform, chineseFont);
            BuildPersonalContribBar  (gameUIPanel.transform, chineseFont);

            // B8a/b/c 无 UI，直接挂 Canvas 上（确保常驻）
            AttachFairyWandBehaviours(canvas.gameObject);

            // ---- 5. 标脏场景（由 PM 用 MCP 或 SaveCurrentScene 保存） ----
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[Setup34A] 完成！建议运行 MCP manage_scene action='save' 或 SaveCurrentScene.Execute() 保存场景。");
        }

        // ==================== B1 StatusLineBanner ====================

        private static void BuildStatusLineBanner(Transform canvasRoot, TMP_FontAsset font)
        {
            // 顶部居中一行（宽 1200，高 40；Y=-80 避开 TopBarUI）
            var banner = GetOrCreateChild(canvasRoot, "StatusLineBanner", () =>
            {
                var g  = new GameObject("StatusLineBanner");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(canvasRoot, false);
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot     = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -80f);
                rt.sizeDelta = new Vector2(1200f, 40f);
                return g;
            });

            // 不放背景（"不遮挡游戏画面"——策划案 §34.3 B1）

            // TMP 文本
            var textGO = CreateTMP(banner.transform, "StatusText",
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(12f, 0f), new Vector2(-12f, 0f),
                28f, TextAlignmentOptions.Center, font);
            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.fontStyle = FontStyles.Normal;
            tmp.text = "";
            SetTMPColor(tmp, Color.white);
            // 白色描边（策划案 §34.3 B1）——用 TMP Outline 近似
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = new Color32(0, 0, 0, 255);

            // 挂脚本 + 绑字段
            var ui = banner.GetComponent<StatusLineBannerUI>();
            if (ui == null) ui = banner.AddComponent<StatusLineBannerUI>();

            var so = new SerializedObject(ui);
            TryBind(so, "_bannerRoot", banner.GetComponent<RectTransform>());
            TryBind(so, "_statusText", tmp);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);

            Debug.Log("[Setup34A] B1 StatusLineBanner 已挂 StatusLineBannerUI + 字段绑定完成");
        }

        // ==================== B5 OreRepairFloatRoot ====================

        private static void BuildOreRepairFloatRoot(Transform canvasRoot)
        {
            // 只是空容器（占位），实际飘字 DamageNumber 创建世界空间 GO
            var root = GetOrCreateChild(canvasRoot, "OreRepairFloatRoot", () =>
            {
                var g  = new GameObject("OreRepairFloatRoot");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(canvasRoot, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return g;
            });

            // 挂脚本（无 Inspector 字段）
            var ui = root.GetComponent<OreRepairFloatingText>();
            if (ui == null) ui = root.AddComponent<OreRepairFloatingText>();
            EditorUtility.SetDirty(ui);

            Debug.Log("[Setup34A] B5 OreRepairFloatRoot 已挂 OreRepairFloatingText");
        }

        // ==================== B8d FairyWandMaxedBanner ====================

        private static void BuildFairyWandMaxedBanner(Transform canvasRoot, TMP_FontAsset font)
        {
            var banner = GetOrCreateChild(canvasRoot, "FairyWandMaxedBanner", () =>
            {
                var g  = new GameObject("FairyWandMaxedBanner");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(canvasRoot, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return g;
            });

            // 全屏金闪子节点
            var flashRoot = GetOrCreateChild(banner.transform, "FlashRoot", () =>
            {
                var g  = new GameObject("FlashRoot");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(banner.transform, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return g;
            });
            var flashImg = flashRoot.GetComponent<Image>();
            if (flashImg == null) flashImg = flashRoot.AddComponent<Image>();
            flashImg.color         = new Color(1f, 0.85f, 0.2f, 0f);   // 金色 初始 alpha 0
            flashImg.raycastTarget = false;
            flashRoot.SetActive(false);

            // 跑马灯容器（顶部 -160 位置，宽 1200 高 60）
            var marqueeRoot = GetOrCreateChild(banner.transform, "MarqueeRoot", () =>
            {
                var g  = new GameObject("MarqueeRoot");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(banner.transform, false);
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot     = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -160f);
                rt.sizeDelta = new Vector2(1200f, 60f);
                return g;
            });

            // 跑马灯文字
            var marqueeTextGO = CreateTMP(marqueeRoot.transform, "MarqueeText",
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(12f, 0f), new Vector2(-12f, 0f),
                36f, TextAlignmentOptions.Center, font);
            var marqueeTmp = marqueeTextGO.GetComponent<TextMeshProUGUI>();
            marqueeTmp.fontStyle = FontStyles.Bold;
            marqueeTmp.text = "";
            SetTMPColor(marqueeTmp, new Color(1f, 0.95f, 0.4f));
            marqueeTmp.outlineWidth = 0.3f;
            marqueeTmp.outlineColor = new Color32(80, 40, 0, 255);

            marqueeRoot.SetActive(false);

            // 挂脚本 + 绑字段
            var ui = banner.GetComponent<FairyWandMaxedBanner>();
            if (ui == null) ui = banner.AddComponent<FairyWandMaxedBanner>();

            var so = new SerializedObject(ui);
            TryBind(so, "_flashRoot",   flashRoot.GetComponent<RectTransform>());
            TryBind(so, "_flashImage",  flashImg);
            TryBind(so, "_marqueeRoot", marqueeRoot.GetComponent<RectTransform>());
            TryBind(so, "_marqueeText", marqueeTmp);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);

            Debug.Log("[Setup34A] B8d FairyWandMaxedBanner 已挂 FairyWandMaxedBanner + 字段绑定完成");
        }

        // ==================== B9 PersonalContribBar ====================

        private static void BuildPersonalContribBar(Transform gameUIPanel, TMP_FontAsset font)
        {
            // 左下角 Y=170（GiftIconBar 160px 之上，留 10px 间距）
            var bar = GetOrCreateChild(gameUIPanel, "PersonalContribBar", () =>
            {
                var g  = new GameObject("PersonalContribBar");
                var rt = g.AddComponent<RectTransform>();
                g.transform.SetParent(gameUIPanel, false);
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot     = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(12f, 170f);
                rt.sizeDelta = new Vector2(560f, 36f);
                return g;
            });

            // 半透明背景（便于阅读）
            var bg = bar.GetComponent<Image>();
            if (bg == null) bg = bar.AddComponent<Image>();
            bg.color         = new Color(0f, 0f, 0f, 0.45f);
            bg.raycastTarget = false;

            // TMP 文本（左对齐 20px）
            var textGO = CreateTMP(bar.transform, "ContribText",
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(10f, 0f), new Vector2(-10f, 0f),
                20f, TextAlignmentOptions.Left, font);
            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = "";
            SetTMPColor(tmp, Color.white);
            textGO.SetActive(false);   // 初始隐藏（首条 playerStats 到达后 PersonalContribUI 自动 SetActive(true)）

            // 挂脚本 + 绑字段
            var ui = bar.GetComponent<PersonalContribUI>();
            if (ui == null) ui = bar.AddComponent<PersonalContribUI>();

            var so = new SerializedObject(ui);
            TryBind(so, "_contribText", tmp);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);

            Debug.Log("[Setup34A] B9 PersonalContribBar 已挂 PersonalContribUI + 字段绑定完成");
        }

        // ==================== B8a/b/c 无 UI Behaviour 挂载 ====================

        private static void AttachFairyWandBehaviours(GameObject canvasGo)
        {
            // FloatingText / StardustTrail / AccumUI 无 Inspector 字段，挂 Canvas 即可
            if (canvasGo.GetComponent<FairyWandFloatingText>() == null)
                canvasGo.AddComponent<FairyWandFloatingText>();
            if (canvasGo.GetComponent<FairyWandStardustTrail>() == null)
                canvasGo.AddComponent<FairyWandStardustTrail>();
            if (canvasGo.GetComponent<FairyWandAccumUI>() == null)
                canvasGo.AddComponent<FairyWandAccumUI>();
            EditorUtility.SetDirty(canvasGo);

            Debug.Log("[Setup34A] B8a/b/c FairyWand Floating/Stardust/Accum 已挂到 Canvas");
        }

        // ==================== 辅助方法（与 §34B/C/D 同套路） ====================

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
                Debug.LogWarning($"[Setup34A] {name} 占位建出（原场景缺失）。");
                return g;
            });
        }

        private static GameObject CreateTMP(Transform parent, string name,
                                            Vector2 anchorMin, Vector2 anchorMax,
                                            Vector2 offsetMin, Vector2 offsetMax,
                                            float fontSize, TextAlignmentOptions align,
                                            TMP_FontAsset font)
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
            rt2.anchorMin = anchorMin;
            rt2.anchorMax = anchorMax;
            rt2.offsetMin = offsetMin;
            rt2.offsetMax = offsetMax;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize         = fontSize;
            tmp.alignment        = align;
            tmp.enableAutoSizing = false;
            tmp.raycastTarget    = false;
            if (font != null) tmp.font = font;
            SetTMPColor(tmp, Color.white);
            return go;
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
                Debug.LogWarning($"[Setup34A] TryBind: 字段 {fieldName} 未找到（可能脚本未定义该 SerializeField）");
                return;
            }
            p.objectReferenceValue = value;
        }

        private static GameObject GetOrCreateChild(Transform parent, string name, System.Func<GameObject> creator)
        {
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
