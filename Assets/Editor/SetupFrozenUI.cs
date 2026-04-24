using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrscfZ.UI;

namespace DrscfZ.Editor
{
    /// <summary>
    /// FrozenStatusPanel 场景结构自动化建立工具
    ///
    /// 菜单：Tools → DrscfZ → Setup Frozen UI (Status Panel)
    ///
    /// 功能：
    ///   在 Canvas 下创建 FrozenStatusPanel（底部冻结状态横幅）
    ///   并将 FrozenStatusUI 挂载到 Canvas，绑定所有字段。
    ///
    /// 注意：不使用 DisplayDialog；若对象已存在则跳过
    /// </summary>
    public static class SetupFrozenUI
    {
        [MenuItem("Tools/DrscfZ/Setup Frozen UI (Status Panel)")]
        public static void Execute()
        {
            int created = 0;

            // ── 1. 找到 Canvas ──────────────────────────────────────────────
            var canvasGo = GameObject.Find("Canvas");
            if (canvasGo == null)
            {
                Debug.LogError("[SetupFrozenUI] 未找到 Canvas，请确认场景中存在 Canvas 对象");
                return;
            }

            // ── 2. 创建 FrozenStatusPanel ───────────────────────────────────
            var existingPanel = canvasGo.transform.Find("FrozenStatusPanel");
            GameObject panelGo;

            if (existingPanel == null)
            {
                panelGo = CreateFrozenPanel(canvasGo.transform);
                created++;
                Debug.Log("[SetupFrozenUI] FrozenStatusPanel 创建完成 ✅");
            }
            else
            {
                panelGo = existingPanel.gameObject;
                Debug.Log("[SetupFrozenUI] FrozenStatusPanel 已存在，跳过");
            }

            // ── 3. 挂载 FrozenStatusUI 到 Canvas ───────────────────────────
            var frozenUI = canvasGo.GetComponent<FrozenStatusUI>();
            if (frozenUI == null)
            {
                frozenUI = canvasGo.AddComponent<FrozenStatusUI>();
                created++;
                Debug.Log("[SetupFrozenUI] FrozenStatusUI 挂载到 Canvas ✅");
            }
            else
            {
                Debug.Log("[SetupFrozenUI] FrozenStatusUI 已存在，跳过");
            }

            // ── 4. 自动绑定字段 ─────────────────────────────────────────────
            {
                var so = new SerializedObject(frozenUI);

                so.FindProperty("_panel").objectReferenceValue = panelGo;

                var frozenText = panelGo.transform.Find("FrozenText")?.GetComponent<TextMeshProUGUI>();
                so.FindProperty("_frozenText").objectReferenceValue = frozenText;

                var countdownText = panelGo.transform.Find("CountdownText")?.GetComponent<TextMeshProUGUI>();
                so.FindProperty("_countdownText").objectReferenceValue = countdownText;

                var bgImage = panelGo.transform.Find("BackgroundImage")?.GetComponent<Image>();
                so.FindProperty("_backgroundImage").objectReferenceValue = bgImage;

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(canvasGo);
                Debug.Log("[SetupFrozenUI] FrozenStatusUI 字段绑定完成 ✅");
            }

            // ── 5. 保存场景（Rule #8）─────────────────────────────────────
            if (created > 0)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[SetupFrozenUI] ✅ 完成！共创建/挂载 {created} 个对象，场景已保存");
            }
            else
            {
                Debug.Log("[SetupFrozenUI] ✅ 所有对象已存在，无需创建");
            }
        }

        // ==================== 创建面板结构 ====================

        private static GameObject CreateFrozenPanel(Transform parent)
        {
            // 根节点（初始 inactive，Rule #2）
            var panel = new GameObject("FrozenStatusPanel", typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            panel.SetActive(false);

            // 布局：底部全宽横幅，高 60px
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot     = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 60f); // 底部偏上一点（避开安全区）
            panelRect.sizeDelta = new Vector2(0f, 60f);

            // ── BackgroundImage（半透明蓝黑底）
            var bgGo = new GameObject("BackgroundImage", typeof(RectTransform));
            bgGo.transform.SetParent(panel.transform, false);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.1f, 0.35f, 0.88f); // 深蓝

            // HorizontalLayoutGroup 排版（图标 + 文字 + 倒计时）
            var hlayout = panel.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment    = TextAnchor.MiddleCenter;
            hlayout.spacing           = 16f;
            hlayout.padding           = new RectOffset(20, 20, 8, 8);
            hlayout.childControlWidth  = false;
            hlayout.childControlHeight = true;
            hlayout.childForceExpandWidth  = false;
            hlayout.childForceExpandHeight = true;

            // ── FrozenText（主文字）
            var frozenTextGo = new GameObject("FrozenText", typeof(RectTransform));
            frozenTextGo.transform.SetParent(panel.transform, false);
            var ftRect = frozenTextGo.GetComponent<RectTransform>();
            ftRect.sizeDelta = new Vector2(480f, 0f);
            var ftTmp = frozenTextGo.AddComponent<TextMeshProUGUI>();
            ftTmp.text      = "全体守护者已冻结";
            ftTmp.fontSize  = 22f;
            ftTmp.color     = new Color(0.7f, 0.9f, 1f, 1f); // 冰蓝
            ftTmp.alignment = TextAlignmentOptions.Center;
            ftTmp.fontStyle = FontStyles.Bold;
            ApplyChineseFont(ftTmp);

            // ── CountdownText（倒计时）
            var countdownGo = new GameObject("CountdownText", typeof(RectTransform));
            countdownGo.transform.SetParent(panel.transform, false);
            var ctRect = countdownGo.GetComponent<RectTransform>();
            ctRect.sizeDelta = new Vector2(200f, 0f);
            var ctTmp = countdownGo.AddComponent<TextMeshProUGUI>();
            ctTmp.text      = "解冻倒计时：30s";
            ctTmp.fontSize  = 20f;
            ctTmp.color     = new Color(0.9f, 1f, 1f, 1f);
            ctTmp.alignment = TextAlignmentOptions.Center;
            ApplyChineseFont(ctTmp);

            return panel;
        }

        private static void ApplyChineseFont(TextMeshProUGUI tmp)
        {
            var font = Resources.Load<TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF") ?? Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
            if (font != null) tmp.font = font;
        }
    }
}
