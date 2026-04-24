// Copyright 2026 极地生存法则
// §36 UI 一键搭建（BossRush + PeaceNight + FEATURE_UNLOCK_DAY）：
//  1. Canvas 下新增 3 个 GO（BossRushBanner / PeaceNightOverlay / FeatureUnlockBanner）
//  2. 给 BroadcasterPanel 的 6 个受锁按钮挂 FeatureLockOverlay + 配 featureId
// 菜单：Tools → DrscfZ → Setup Section36 UI
// 参考风格：RebuildGiftIconBar.cs / SetupLobbyAndLoadingUI.cs
// 执行后 MarkSceneDirty；需再调 manage_scene save 或 Ctrl+S 落盘

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using DrscfZ.UI;  // BossRushBanner / PeaceNightOverlay / FeatureUnlockBanner / FeatureLockOverlay / BroadcasterPanel

namespace DrscfZ.EditorTools
{
    public static class SetupSection36UI
    {
        [MenuItem("Tools/DrscfZ/Setup Section36 UI")]
        public static void Execute()
        {
            var canvasGO = GameObject.Find("Canvas");
            if (canvasGO == null)
            {
                Debug.LogError("[Setup36UI] 未找到 Canvas 根节点，中断。");
                return;
            }

            // 先清理旧 banner（Unity fake-null 与 ?? 运算符不兼容，显式销毁避免残留 fileID 引用问题）
            foreach (var name in new[] { "BossRushBanner", "PeaceNightOverlay", "FeatureUnlockBanner" })
            {
                var old = canvasGO.transform.Find(name);
                if (old != null)
                {
                    Undo.DestroyObjectImmediate(old.gameObject);
                }
            }

            int created = 0;
            created += SetupBossRushBanner(canvasGO.transform);
            created += SetupPeaceNightOverlay(canvasGO.transform);
            created += SetupFeatureUnlockBanner(canvasGO.transform);

            int attached = AttachFeatureLockOverlays();

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.IsValid()) EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log($"[Setup36UI] Done. Banners created/updated={created}, FeatureLockOverlays attached={attached}. 请保存场景。");
        }

        // ==================== BossRushBanner（屏幕顶部横幅） ====================
        private static int SetupBossRushBanner(Transform canvas)
        {
            const string GO_NAME = "BossRushBanner";
            var root = FindOrCreateChild(canvas, GO_NAME, Vector2.zero, new Vector2(1200f, 160f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            root.anchoredPosition = new Vector2(0f, -100f);

            // panelRoot 作为可见容器（脚本 Show/Hide 切这个）
            var panelRoot = FindOrCreateChild(root, "Content", Vector2.zero, new Vector2(1200f, 160f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            EnsureImage(panelRoot.gameObject, new Color(0.08f, 0.04f, 0.20f, 0.85f));

            var titleText     = EnsureTMP(panelRoot, "TitleText", "赛季 Boss 来袭！", 40f, FontStyles.Bold, new Color(1f, 0.85f, 0.4f),
                                           new Vector2(0f, 45f), new Vector2(1180f, 60f), TextAlignmentOptions.Center);
            var bossHpText    = EnsureTMP(panelRoot, "BossHpText", "HP 5000 / 5000", 22f, FontStyles.Normal, new Color(1f, 0.85f, 0.85f),
                                           new Vector2(0f, -5f), new Vector2(800f, 30f), TextAlignmentOptions.Center);
            var nextThemeText = EnsureTMP(panelRoot, "NextThemeText", "下一赛季预告：经典冰封", 18f, FontStyles.Italic, new Color(0.7f, 0.85f, 1f),
                                           new Vector2(0f, -55f), new Vector2(800f, 26f), TextAlignmentOptions.Center);
            var killedText    = EnsureTMP(panelRoot, "KilledText", "赛季 Boss 已倒下！", 40f, FontStyles.Bold, new Color(0.6f, 1f, 0.6f),
                                           Vector2.zero, new Vector2(1180f, 160f), TextAlignmentOptions.Center);
            killedText.gameObject.SetActive(false);

            // HP 进度条：Image(Filled, Horizontal)
            var barGO = FindOrCreateChild(panelRoot, "BossHpBar", new Vector2(0f, -25f), new Vector2(1000f, 10f),
                                          new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)).gameObject;
            var barImg = EnsureImageComponent(barGO);
            barImg.color = new Color(0.95f, 0.3f, 0.3f, 0.95f);
            barImg.type = Image.Type.Filled;
            barImg.fillMethod = Image.FillMethod.Horizontal;
            barImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            barImg.fillAmount = 1f;

            // KilledFlash CanvasGroup（脚本控制闪光）
            var flashGO = FindOrCreateChild(panelRoot, "KilledFlash", Vector2.zero, new Vector2(1200f, 160f),
                                            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)).gameObject;
            EnsureImage(flashGO, new Color(1f, 1f, 0.6f, 0.5f));
            var flashCg = flashGO.GetComponent<CanvasGroup>();
            if (flashCg == null) flashCg = flashGO.AddComponent<CanvasGroup>();
            flashCg.alpha = 0f;
            flashCg.blocksRaycasts = false;
            flashCg.interactable = false;
            flashGO.SetActive(false);

            panelRoot.gameObject.SetActive(false);

            // 挂脚本 + 绑定字段
            var banner = root.gameObject.GetComponent<BossRushBanner>() ?? root.gameObject.AddComponent<BossRushBanner>();
            var so = new SerializedObject(banner);
            Bind(so, "_panelRoot",     panelRoot.gameObject);
            Bind(so, "_titleText",     titleText);
            Bind(so, "_bossHpText",    bossHpText);
            Bind(so, "_nextThemeText", nextThemeText);
            Bind(so, "_killedText",    killedText);
            Bind(so, "_bossHpBar",     barImg);
            Bind(so, "_killedFlash",   flashCg);
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[Setup36UI] BossRushBanner ready");
            return 1;
        }

        // ==================== PeaceNightOverlay（全屏柔光罩） ====================
        private static int SetupPeaceNightOverlay(Transform canvas)
        {
            const string GO_NAME = "PeaceNightOverlay";
            var root = FindOrCreateChild(canvas, GO_NAME, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
            // 全屏 stretch
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // overlayRoot（整体容器）
            var overlayRoot = FindOrCreateChild(root, "Content", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
            var ort = overlayRoot.GetComponent<RectTransform>();
            ort.anchorMin = Vector2.zero;
            ort.anchorMax = Vector2.one;
            ort.offsetMin = Vector2.zero;
            ort.offsetMax = Vector2.zero;

            // overlayImage（满屏冰蓝半透）
            var imgGO = FindOrCreateChild(overlayRoot, "OverlayImage", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one).gameObject;
            var ig = imgGO.GetComponent<RectTransform>();
            ig.anchorMin = Vector2.zero;
            ig.anchorMax = Vector2.one;
            ig.offsetMin = Vector2.zero;
            ig.offsetMax = Vector2.zero;
            var overlayImage = EnsureImageComponent(imgGO);
            overlayImage.color = new Color(0.4f, 0.7f, 1f, 0f); // alpha 起始为 0，由脚本 fade-in
            overlayImage.raycastTarget = false;

            // 提示文字
            var hintText     = EnsureTMP(overlayRoot, "HintText", "敌人尚未察觉你的存在",
                                         36f, FontStyles.Bold, new Color(0.95f, 0.98f, 1f, 1f),
                                         new Vector2(0f, 0f), new Vector2(1200f, 80f), TextAlignmentOptions.Center);
            hintText.enableAutoSizing = false;

            var countdownText = EnsureTMP(overlayRoot, "CountdownText", "",
                                          22f, FontStyles.Italic, new Color(0.85f, 0.95f, 1f, 0.9f),
                                          new Vector2(0f, -70f), new Vector2(800f, 40f), TextAlignmentOptions.Center);

            overlayRoot.gameObject.SetActive(false);

            var overlay = root.gameObject.GetComponent<PeaceNightOverlay>() ?? root.gameObject.AddComponent<PeaceNightOverlay>();
            var so = new SerializedObject(overlay);
            Bind(so, "_overlayRoot",   overlayRoot.gameObject);
            Bind(so, "_overlayImage",  overlayImage);
            Bind(so, "_hintText",      hintText);
            Bind(so, "_countdownText", countdownText);
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[Setup36UI] PeaceNightOverlay ready");
            return 1;
        }

        // ==================== FeatureUnlockBanner（中央解锁横幅） ====================
        private static int SetupFeatureUnlockBanner(Transform canvas)
        {
            const string GO_NAME = "FeatureUnlockBanner";
            var root = FindOrCreateChild(canvas, GO_NAME, Vector2.zero, new Vector2(900f, 180f),
                                         new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            root.anchoredPosition = new Vector2(0f, 180f);

            var panelRoot = FindOrCreateChild(root, "Content", Vector2.zero, new Vector2(900f, 180f),
                                              new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var panelImg = EnsureImage(panelRoot.gameObject, new Color(0.10f, 0.22f, 0.45f, 0.92f));
            panelImg.raycastTarget = false;

            var titleText = EnsureTMP(panelRoot, "TitleText", "今日解锁：",
                                      32f, FontStyles.Bold, new Color(1f, 0.85f, 0.3f),
                                      new Vector2(0f, 45f), new Vector2(880f, 50f), TextAlignmentOptions.Center);
            var descText  = EnsureTMP(panelRoot, "DescText",  "",
                                      22f, FontStyles.Normal, new Color(0.85f, 0.95f, 1f),
                                      new Vector2(0f, -25f), new Vector2(880f, 70f), TextAlignmentOptions.Center);

            var cg = panelRoot.GetComponent<CanvasGroup>();
            if (cg == null) cg = panelRoot.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            panelRoot.gameObject.SetActive(false);

            var banner = root.gameObject.GetComponent<FeatureUnlockBanner>() ?? root.gameObject.AddComponent<FeatureUnlockBanner>();
            var so = new SerializedObject(banner);
            Bind(so, "_panelRoot",  panelRoot.gameObject);
            Bind(so, "_titleText",  titleText);
            Bind(so, "_descText",   descText);
            Bind(so, "_canvasGroup", cg);
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[Setup36UI] FeatureUnlockBanner ready");
            return 1;
        }

        // ==================== FeatureLockOverlay 按钮挂载 ====================
        private static int AttachFeatureLockOverlays()
        {
            var bp = Object.FindFirstObjectByType<BroadcasterPanel>();
            if (bp == null)
            {
                Debug.LogWarning("[Setup36UI] 未找到 BroadcasterPanel 组件，跳过按钮挂载。");
                return 0;
            }

            var bpSO = new SerializedObject(bp);
            // feature id 对齐 §36.12（与 FeatureUnlockConfig.js / SurvivalMessageProtocol.FeatureXxx 一致）
            // (propName, featureId, placeholderLabel, placeholderPosY) — placeholder 仅在字段未绑定时使用
            var mapping = new (string propName, string featureId, string label, float posY)[]
            {
                ("_boostBtn",            "broadcaster_boost",  "加", 60f),
                ("_eventBtn",            "broadcaster_boost",  "浪", -60f),
                ("_rouletteButton",      "roulette",           "盘", 180f),
                ("_shopTabButton",       "shop",               "购", -180f),
                ("_tribeWarButton",      "tribe_war",          "战", -300f),
                ("_btnUpgradeGate",      "gate_upgrade_basic", "升", 300f),
                // audit-r6 P0-F3: §36.12 补齐 3 个缺失的 feature 入口
                ("_buildingButton",      "building",           "建", -420f),
                ("_expeditionButton",    "expedition",         "探", -540f),
                ("_supporterModeButton", "supporter_mode",     "援", -660f),
            };

            // 找到一个已存在的 Button GO 作为模板（用于为未绑定字段建占位）
            Button templateBtn = bp.GetType().GetField("_boostBtn",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.GetValue(bp) as Button;
            if (templateBtn == null)
            {
                // 兜底：从 mapping 第一个有绑定的字段拿
                foreach (var m in mapping)
                {
                    var p = bpSO.FindProperty(m.propName);
                    if (p != null && p.objectReferenceValue is Button b) { templateBtn = b; break; }
                }
            }
            Transform templateParent = templateBtn != null ? templateBtn.transform.parent : null;

            int count = 0;
            foreach (var (propName, featureId, label, posY) in mapping)
            {
                var prop = bpSO.FindProperty(propName);
                Button btn = prop != null ? (prop.objectReferenceValue as Button) : null;

                // 未绑定 → 创建占位按钮（不再跳过；决策 6 要求不留未完成给用户）
                if (btn == null)
                {
                    if (templateBtn == null || templateParent == null)
                    {
                        Debug.LogWarning($"[Setup36UI] 无模板按钮可用，{propName} 无法建占位，跳过。");
                        continue;
                    }

                    string goName = propName.TrimStart('_');
                    goName = char.ToUpperInvariant(goName[0]) + goName.Substring(1);   // _rouletteButton → RouletteButton

                    var existing = templateParent.Find(goName);
                    GameObject newGO;
                    if (existing != null)
                    {
                        newGO = existing.gameObject;
                    }
                    else
                    {
                        newGO = Object.Instantiate(templateBtn.gameObject, templateParent);
                        newGO.name = goName;
                        // 移除 template 遗留的 onClick Persistent 监听（占位按钮暂不响应）
                        var newBtn = newGO.GetComponent<Button>();
                        if (newBtn != null)
                        {
                            newBtn.onClick.RemoveAllListeners();
                            var soBtn = new SerializedObject(newBtn);
                            var calls = soBtn.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
                            if (calls != null) calls.arraySize = 0;
                            soBtn.ApplyModifiedPropertiesWithoutUndo();
                        }
                    }
                    // 调位置 + 改文本
                    var rt = newGO.GetComponent<RectTransform>();
                    if (rt != null) rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, posY);
                    var newTmp = newGO.GetComponentInChildren<TMP_Text>();
                    if (newTmp != null) newTmp.text = label;

                    // 回写 BroadcasterPanel SerializedField
                    btn = newGO.GetComponent<Button>();
                    if (btn != null && prop != null)
                    {
                        prop.objectReferenceValue = btn;
                    }
                    Debug.Log($"[Setup36UI] 占位按钮已创建: {GetPath(newGO.transform)}  label='{label}'");
                }

                if (btn == null) continue;

                var go = btn.gameObject;
                var overlay = go.GetComponent<FeatureLockOverlay>();
                if (overlay == null) overlay = go.AddComponent<FeatureLockOverlay>();

                var so = new SerializedObject(overlay);
                so.FindProperty("_featureId").stringValue = featureId;

                var bg = go.GetComponent<Image>();
                if (bg != null) Bind(so, "_btnBackground", bg);
                var tmp = go.GetComponentInChildren<TMP_Text>();
                if (tmp != null) Bind(so, "_btnLabel", tmp);
                so.ApplyModifiedPropertiesWithoutUndo();

                count++;
                Debug.Log($"[Setup36UI] FeatureLockOverlay attached: {GetPath(go.transform)}  featureId={featureId}");
            }

            // 回写 BroadcasterPanel（新创建的按钮字段）
            bpSO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bp);

            return count;
        }

        // ==================== 辅助 ====================
        private static RectTransform FindOrCreateChild(Transform parent, string name,
            Vector2 anchoredPos, Vector2 sizeDelta, Vector2 anchorMin, Vector2 anchorMax)
        {
            var existing = parent.Find(name);
            GameObject go;
            if (existing != null) go = existing.gameObject;
            else
            {
                go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
            }
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            return rt;
        }

        private static Image EnsureImage(GameObject go, Color color)
        {
            var img = go.GetComponent<Image>();
            if (img == null) img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static Image EnsureImageComponent(GameObject go)
        {
            var img = go.GetComponent<Image>();
            if (img == null) img = go.AddComponent<Image>();
            return img;
        }

        private static TMP_Text EnsureTMP(Transform parent, string name, string text, float fontSize,
            FontStyles style, Color color, Vector2 anchoredPos, Vector2 sizeDelta, TextAlignmentOptions align)
        {
            var rt = FindOrCreateChild(parent, name, anchoredPos, sizeDelta, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var tmp = rt.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = align;
            // TMP faceColor 默认白，必须用 SerializedObject 写 m_fontColor + m_fontColor32（见 CLAUDE.md 踩坑）
            var so = new SerializedObject(tmp);
            var p = so.FindProperty("m_fontColor");
            if (p != null) p.colorValue = color;
            var p2 = so.FindProperty("m_fontColor32");
            if (p2 != null) p2.colorValue = color;
            so.ApplyModifiedPropertiesWithoutUndo();
            tmp.color = color;
            return tmp;
        }

        private static void Bind(SerializedObject so, string propName, Object value)
        {
            var p = so.FindProperty(propName);
            if (p != null) p.objectReferenceValue = value;
            else Debug.LogWarning($"[Setup36UI] Property '{propName}' not found on {so.targetObject}");
        }

        private static string GetPath(Transform t)
        {
            var sb = new System.Text.StringBuilder(t.name);
            while (t.parent != null) { t = t.parent; sb.Insert(0, t.name + "/"); }
            return sb.ToString();
        }
    }
}
