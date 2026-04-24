using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using DrscfZ.UI;

namespace DrscfZ.EditorTools
{
    /// <summary>
    /// audit-r5 Setup：为两个新 UI 脚本在 Canvas/GameUIPanel 下建占位 GO + 绑定 [SerializeField] 字段。
    ///   - EventTriggeredUI (§34 B3)：顶部中央 toast（默认 inactive，HandleEvent 触发）
    ///   - SupporterActionLogUI (§34 B4)：左侧 5 行 log（常驻激活，浅紫底色）
    ///
    /// 运行后 manage_scene 保存。字体 Alibaba + ChineseFont fallback。
    /// </summary>
    public static class SetupAuditR5UI
    {
        private const string CANVAS_PATH       = "Canvas/GameUIPanel";
        private const string EVENT_TOAST_NAME  = "EventTriggeredToast";
        private const string SUPPORTER_LOG_NAME = "SupporterActionLog";
        private const string FONT_PRIMARY_PATH = "Fonts/AlibabaPuHuiTi-3-85-Bold SDF";
        private const string FONT_FALLBACK_PATH = "Fonts/ChineseFont SDF";

        [MenuItem("Tools/DrscfZ/Setup Audit-r5 UI (EventTriggered + SupporterActionLog)")]
        public static void Execute()
        {
            var canvas = FindGameUIPanelInclusiveInactive();
            if (canvas == null)
            {
                Debug.LogError($"[SetupAuditR5UI] 未找到 {CANVAS_PATH}（含 inactive），请先打开 MainScene");
                return;
            }

            var font = Resources.Load<TMP_FontAsset>(FONT_PRIMARY_PATH) ?? Resources.Load<TMP_FontAsset>(FONT_FALLBACK_PATH);
            if (font == null)
                Debug.LogWarning($"[SetupAuditR5UI] 字体资产缺失：{FONT_PRIMARY_PATH} / {FONT_FALLBACK_PATH}，运行时将用默认字体");

            SetupEventTriggeredUI(canvas, font);
            SetupSupporterActionLogUI(canvas, font);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[SetupAuditR5UI] 完成：EventTriggeredToast + SupporterActionLog 已挂载并绑定字段。记得 manage_scene save。");
        }

        private static void SetupEventTriggeredUI(GameObject parent, TMP_FontAsset font)
        {
            var existing = parent.transform.Find(EVENT_TOAST_NAME);
            if (existing != null)
            {
                Debug.Log("[SetupAuditR5UI] EventTriggeredToast 已存在，跳过创建");
                return;
            }

            // Holder（挂 MonoBehaviour；常驻激活）
            var holder = new GameObject(EVENT_TOAST_NAME, typeof(RectTransform));
            holder.transform.SetParent(parent.transform, false);
            var holderRT = holder.GetComponent<RectTransform>();
            holderRT.anchorMin = new Vector2(0.5f, 1f);
            holderRT.anchorMax = new Vector2(0.5f, 1f);
            holderRT.pivot     = new Vector2(0.5f, 1f);
            holderRT.anchoredPosition = new Vector2(0, -140);
            holderRT.sizeDelta = new Vector2(640, 120);

            var ui = holder.AddComponent<EventTriggeredUI>();

            // Root（toast 容器，初始 inactive）
            var root = new GameObject("Root", typeof(RectTransform));
            root.transform.SetParent(holder.transform, false);
            var rootRT = root.GetComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.offsetMin = Vector2.zero;
            rootRT.offsetMax = Vector2.zero;

            // Bg Image（染色 tint）
            var bgGO = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(root.transform, false);
            var bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            var bgImg = bgGO.GetComponent<Image>();
            bgImg.color = new Color(0.15f, 0.2f, 0.35f, 0.85f);

            // NameLabel
            var nameGO = new GameObject("NameLabel", typeof(RectTransform));
            nameGO.transform.SetParent(root.transform, false);
            var nameRT = nameGO.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0.5f);
            nameRT.anchorMax = new Vector2(1, 1f);
            nameRT.offsetMin = new Vector2(16, 0);
            nameRT.offsetMax = new Vector2(-16, -4);
            var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
            nameTMP.text = "事件名";
            nameTMP.fontSize = 28;
            nameTMP.fontStyle = FontStyles.Bold;
            nameTMP.alignment = TextAlignmentOptions.Center;
            nameTMP.color = new Color(1f, 0.95f, 0.8f, 1f);
            if (font != null) nameTMP.font = font;

            // DescLabel
            var descGO = new GameObject("DescLabel", typeof(RectTransform));
            descGO.transform.SetParent(root.transform, false);
            var descRT = descGO.GetComponent<RectTransform>();
            descRT.anchorMin = new Vector2(0, 0f);
            descRT.anchorMax = new Vector2(1, 0.5f);
            descRT.offsetMin = new Vector2(16, 4);
            descRT.offsetMax = new Vector2(-16, 0);
            var descTMP = descGO.AddComponent<TextMeshProUGUI>();
            descTMP.text = "简介";
            descTMP.fontSize = 18;
            descTMP.alignment = TextAlignmentOptions.Center;
            descTMP.color = new Color(0.9f, 0.9f, 0.95f, 0.85f);
            if (font != null) descTMP.font = font;

            // 绑定 [SerializeField] 字段
            var so = new SerializedObject(ui);
            TrySetRef(so, "_root", root);
            TrySetRef(so, "_nameLabel", nameTMP);
            TrySetRef(so, "_descLabel", descTMP);
            TrySetRef(so, "_bgImage", bgImg);
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            Debug.Log($"[SetupAuditR5UI] 已挂 {EVENT_TOAST_NAME}，字段已绑定");
        }

        private static void SetupSupporterActionLogUI(GameObject parent, TMP_FontAsset font)
        {
            var existing = parent.transform.Find(SUPPORTER_LOG_NAME);
            if (existing != null)
            {
                Debug.Log("[SetupAuditR5UI] SupporterActionLog 已存在，跳过创建");
                return;
            }

            var holder = new GameObject(SUPPORTER_LOG_NAME, typeof(RectTransform), typeof(Image));
            holder.transform.SetParent(parent.transform, false);
            var holderRT = holder.GetComponent<RectTransform>();
            holderRT.anchorMin = new Vector2(0f, 0.25f);
            holderRT.anchorMax = new Vector2(0f, 0.55f);
            holderRT.pivot     = new Vector2(0f, 0.5f);
            holderRT.anchoredPosition = new Vector2(16, 0);
            holderRT.sizeDelta = new Vector2(360, 0);
            var bgImg = holder.GetComponent<Image>();
            // 浅紫 #E8D5F5
            ColorUtility.TryParseHtmlString("#E8D5F5", out var tint);
            bgImg.color = new Color(tint.r, tint.g, tint.b, 0.92f);

            var ui = holder.AddComponent<SupporterActionLogUI>();

            // Container（VerticalLayoutGroup）
            var container = new GameObject("Container", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            container.transform.SetParent(holder.transform, false);
            var containerRT = container.GetComponent<RectTransform>();
            containerRT.anchorMin = new Vector2(0, 0);
            containerRT.anchorMax = new Vector2(1, 1);
            containerRT.offsetMin = new Vector2(8, 8);
            containerRT.offsetMax = new Vector2(-8, -8);
            var vlg = container.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.spacing = 4;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var csf = container.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 绑定 [SerializeField] 字段
            var so = new SerializedObject(ui);
            TrySetRef(so, "_container", containerRT);
            TrySetRef(so, "_bgImage", bgImg);
            // _rowPrefab 留空（代码有兜底运行时生成）
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"[SetupAuditR5UI] 已挂 {SUPPORTER_LOG_NAME}，字段已绑定（_rowPrefab 为 null，运行时兜底生成）");
        }

        private static GameObject FindGameUIPanelInclusiveInactive()
        {
            // 遍历所有场景根对象（含 inactive），找第一个名为 GameUIPanel 的子孙
            var scene = EditorSceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                var rt = FindDescendantByName(root.transform, "GameUIPanel");
                if (rt != null) return rt.gameObject;
            }
            return null;
        }

        private static Transform FindDescendantByName(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var hit = FindDescendantByName(child, name);
                if (hit != null) return hit;
            }
            return null;
        }

        private static void TrySetRef(SerializedObject so, string propName, Object value)
        {
            var prop = so.FindProperty(propName);
            if (prop == null)
            {
                Debug.LogWarning($"[SetupAuditR5UI] SerializedObject 字段未找到：{propName}");
                return;
            }
            prop.objectReferenceValue = value;
        }
    }
}
