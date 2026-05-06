using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrscfZ.UI
{
    /// <summary>
    /// Small runtime fallback UI builder for optional panels that may be missing
    /// from older scenes. Inspector-bound prefabs still take priority.
    /// </summary>
    public static class RuntimeUIFactory
    {
        public static Transform GetCanvasTransform()
        {
            var canvas = Object.FindObjectOfType<Canvas>(true);
            if (canvas == null)
            {
                var go = new GameObject("RuntimeUICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = go.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = go.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }
            return canvas.transform;
        }

        public static RectTransform EnsureRectTransform(Transform transform)
        {
            var rt = transform as RectTransform;
            if (rt != null) return rt;
            return transform.gameObject.GetComponent<RectTransform>() ?? transform.gameObject.AddComponent<RectTransform>();
        }

        public static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
            var img = go.GetComponent<Image>();
            img.color = color;
            return go;
        }

        public static TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize, Color color, TextAlignmentOptions alignment, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            BindFont(tmp);
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.enableWordWrapping = true;
            tmp.raycastTarget = false;
            return tmp;
        }

        public static Button CreateButton(Transform parent, string name, string label, out TextMeshProUGUI labelText, Color color, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = color;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;

            labelText = CreateText(go.transform, "Label", label, 24f, Color.white, TextAlignmentOptions.Center, size);
            var labelRt = labelText.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            return btn;
        }

        public static void AddVerticalLayout(GameObject go, float spacing, RectOffset padding, TextAnchor alignment = TextAnchor.UpperCenter)
        {
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding;
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        public static void AddLayoutElement(GameObject go, float preferredHeight, float preferredWidth = -1f)
        {
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = preferredHeight;
            if (preferredWidth > 0f) le.preferredWidth = preferredWidth;
        }

        public static void BindFont(TMP_Text tmp)
        {
            if (tmp == null) return;
            var font = Resources.Load<TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF")
                       ?? Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
            if (font != null) tmp.font = font;
        }
    }
}
