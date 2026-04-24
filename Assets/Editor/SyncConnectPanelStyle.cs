using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;

/// <summary>
/// 将 ConnectPanel 的样式同步为 LoadingScreen 的风格
/// </summary>
public class SyncConnectPanelStyle
{
    [MenuItem("Tools/DrscfZ/Sync ConnectPanel Style")]
    public static void Execute()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("Canvas not found"); return; }

        Transform cp = null;
        foreach (Transform child in canvas.transform)
        {
            if (child.name == "ConnectPanel") { cp = child; break; }
        }
        if (cp == null) { Debug.LogError("ConnectPanel not found"); return; }

        var font = Resources.Load<TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF") ?? Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
        var outlineMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Fonts/ChineseFont SDF - Outline.mat");

        // ── 1. 背景：使用与 LoadingScreen 相同的冬日背景图 ──
        var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Backgrounds/bg_lobby_winter.jpg");
        var bgImage = cp.GetComponent<Image>();
        if (bgImage != null && bgSprite != null)
        {
            bgImage.sprite = bgSprite;
            bgImage.color = new Color(1f, 1f, 1f, 0.92f);
            bgImage.type = Image.Type.Simple;
        }

        // ── 2. Spinner：使用 spinner_ring 图片，尺寸 100x100，位置偏上 ──
        var spinnerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/LoadingScreen/spinner_ring.png");
        var spinner = cp.Find("Spinner");
        if (spinner != null)
        {
            var srt = spinner.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.5f, 0.5f);
            srt.anchorMax = new Vector2(0.5f, 0.5f);
            srt.sizeDelta = new Vector2(100, 100);
            srt.anchoredPosition = new Vector2(0, 60);

            var simg = spinner.GetComponent<Image>();
            if (simg != null && spinnerSprite != null)
            {
                simg.sprite = spinnerSprite;
                simg.color = Color.white;
                simg.preserveAspect = true;
                simg.raycastTarget = false;
            }
        }

        // ── 3. StatusText：使用百分比锚点，字号34，加粗，浅蓝白色，Outline材质 ──
        var statusText = cp.Find("StatusText");
        if (statusText != null)
        {
            var strt = statusText.GetComponent<RectTransform>();
            strt.anchorMin = new Vector2(0.1f, 0.36f);
            strt.anchorMax = new Vector2(0.9f, 0.44f);
            strt.anchoredPosition = Vector2.zero;
            strt.sizeDelta = Vector2.zero;

            var sttmp = statusText.GetComponent<TMP_Text>();
            if (sttmp != null)
            {
                sttmp.fontSize = 34;
                sttmp.fontStyle = FontStyles.Bold;
                sttmp.alignment = TextAlignmentOptions.Center;
                if (font != null) sttmp.font = font;
                if (outlineMat != null) sttmp.fontSharedMaterial = outlineMat;

                // 颜色用 SerializedObject
                SetTMPColor(sttmp, new Color(0.85f, 0.95f, 1f, 1f));
            }
        }

        // ── 4. DotText：同步位置和样式 ──
        var dotText = cp.Find("DotText");
        if (dotText != null)
        {
            var dtrt = dotText.GetComponent<RectTransform>();
            // 放在 StatusText 右边
            dtrt.anchorMin = new Vector2(0.5f, 0.5f);
            dtrt.anchorMax = new Vector2(0.5f, 0.5f);
            dtrt.anchoredPosition = new Vector2(180, -50);
            dtrt.sizeDelta = new Vector2(80, 50);

            var dttmp = dotText.GetComponent<TMP_Text>();
            if (dttmp != null)
            {
                dttmp.fontSize = 34;
                dttmp.fontStyle = FontStyles.Bold;
                if (font != null) dttmp.font = font;
                if (outlineMat != null) dttmp.fontSharedMaterial = outlineMat;
                SetTMPColor(dttmp, new Color(0.85f, 0.95f, 1f, 1f));
            }
        }

        // ── 5. TitleText（ConnectPanel独有）：调整为标题样式 ──
        var titleText = cp.Find("TitleText");
        if (titleText != null)
        {
            var ttrt = titleText.GetComponent<RectTransform>();
            ttrt.anchorMin = new Vector2(0.1f, 0.55f);
            ttrt.anchorMax = new Vector2(0.9f, 0.65f);
            ttrt.anchoredPosition = Vector2.zero;
            ttrt.sizeDelta = Vector2.zero;

            var tttmp = titleText.GetComponent<TMP_Text>();
            if (tttmp != null)
            {
                tttmp.fontSize = 42;
                tttmp.fontStyle = FontStyles.Bold;
                tttmp.alignment = TextAlignmentOptions.Center;
                if (font != null) tttmp.font = font;
                if (outlineMat != null) tttmp.fontSharedMaterial = outlineMat;
                SetTMPColor(tttmp, Color.white);
            }
        }

        // ── 6. RetryButton：同步为橙色按钮，尺寸 280x70 ──
        var retryBtn = cp.Find("RetryButton");
        if (retryBtn != null)
        {
            var rbrt = retryBtn.GetComponent<RectTransform>();
            rbrt.anchorMin = new Vector2(0.5f, 0.5f);
            rbrt.anchorMax = new Vector2(0.5f, 0.5f);
            rbrt.sizeDelta = new Vector2(280, 70);
            rbrt.anchoredPosition = new Vector2(0, -120);

            var rbimg = retryBtn.GetComponent<Image>();
            if (rbimg != null)
            {
                rbimg.color = new Color(0.9f, 0.5f, 0.1f, 1f); // 橙色，与 LoadingScreen 一致
            }

            // 按钮文字
            var btnText = retryBtn.Find("Text");
            if (btnText != null)
            {
                var bttmp = btnText.GetComponent<TMP_Text>();
                if (bttmp != null)
                {
                    bttmp.fontSize = 30;
                    bttmp.fontStyle = FontStyles.Bold;
                    bttmp.alignment = TextAlignmentOptions.Center;
                    if (font != null) bttmp.font = font;
                    SetTMPColor(bttmp, Color.white);
                }
            }
        }

        // ── 保存 ──
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SyncConnectPanelStyle] ConnectPanel 样式已同步为 LoadingScreen 风格");
    }

    static void SetTMPColor(TMP_Text tmp, Color color)
    {
        var so = new SerializedObject(tmp);
        var p1 = so.FindProperty("m_fontColor");
        if (p1 != null) p1.colorValue = color;
        var p2 = so.FindProperty("m_fontColor32");
        if (p2 != null) p2.colorValue = color;
        so.ApplyModifiedProperties();
    }
}
