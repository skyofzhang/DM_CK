using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;

public class SetupDifficultyUI
{
    [MenuItem("Tools/DrscfZ/Setup Difficulty Select UI")]
    public static void Execute()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("Canvas not found"); return; }

        var diffSelect = canvas.transform.Find("DifficultySelect");
        if (diffSelect == null) { Debug.LogError("DifficultySelect not found"); return; }

        var bgOverlay = diffSelect.Find("BgOverlay");
        if (bgOverlay == null) { Debug.LogError("BgOverlay not found"); return; }

        var contentBox = bgOverlay.Find("ContentBox");
        if (contentBox == null) { Debug.LogError("ContentBox not found"); return; }

        var font = Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");

        // ── 加载素材 ──
        var panelBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Difficulty/difficulty_panel_bg.png");
        var cardEasyBg    = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Difficulty/card_easy_bg.png");
        var cardHardBg    = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Difficulty/card_hard_bg.png");
        var cardHellBg    = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Difficulty/card_hell_bg.png");
        var iconEasy      = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Difficulty/icon_easy.png");
        var iconHard      = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Difficulty/icon_hard.png");
        var iconHell      = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Difficulty/icon_hell.png");

        // ── 面板背景 ──
        var contentImg = contentBox.GetComponent<Image>();
        if (contentImg != null && panelBgSprite != null)
        {
            contentImg.sprite = panelBgSprite;
            contentImg.type = Image.Type.Sliced;
            contentImg.color = Color.white;
        }

        // ── 标题美化 ──
        var titleText = contentBox.Find("TitleText");
        if (titleText != null)
        {
            var tmp = titleText.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.text = "选择游戏难度";
                tmp.fontSize = 42;
                tmp.fontStyle = FontStyles.Bold;
                if (font != null) tmp.font = font;
                SetTMPColor(tmp, new Color(1f, 0.9f, 0.6f, 1f));
            }
            // 位置调高一点
            var rt = titleText.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = new Vector2(0, 220);
        }

        // ── 描述文字 ──
        var descText = contentBox.Find("DescText");
        if (descText != null)
        {
            var tmp = descText.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.text = "根据直播间观众规模选择，不同难度影响怪物强度和资源消耗";
                tmp.fontSize = 22;
                if (font != null) tmp.font = font;
                SetTMPColor(tmp, new Color(0.7f, 0.75f, 0.85f, 1f));
            }
            var rt = descText.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = new Vector2(0, 180);
        }

        // ── 卡片数据 ──
        var cards = new[] {
            new { name = "Card0", bg = cardEasyBg, icon = iconEasy, title = "轻松模式",
                  audience = "适合1~30位观众", detail = "怪物较弱\n资源丰富\n适合小型直播间",
                  titleColor = new Color(0.3f, 0.9f, 0.6f, 1f),
                  btnLabel = "选择" },
            new { name = "Card1", bg = cardHardBg, icon = iconHard, title = "困难模式",
                  audience = "适合30~200位观众", detail = "怪物凶猛\n资源不足\n适合中等直播间",
                  titleColor = new Color(0.5f, 0.5f, 1f, 1f),
                  btnLabel = "选择" },
            new { name = "Card2", bg = cardHellBg, icon = iconHell, title = "恐怖模式",
                  audience = "适合200+位观众", detail = "极强怪物\n极限生存\n适合大型直播间",
                  titleColor = new Color(1f, 0.4f, 0.2f, 1f),
                  btnLabel = "选择" },
        };

        Sprite[] cardBgs = { cardEasyBg, cardHardBg, cardHellBg };
        Sprite[] icons   = { iconEasy, iconHard, iconHell };

        for (int i = 0; i < cards.Length; i++)
        {
            var card = cards[i];
            var cardT = contentBox.Find(card.name);
            if (cardT == null) continue;

            // 卡片背景
            var cardImg = cardT.GetComponent<Image>();
            if (cardImg != null && cardBgs[i] != null)
            {
                cardImg.sprite = cardBgs[i];
                cardImg.type = Image.Type.Sliced;
                cardImg.color = Color.white;
            }

            // 卡片位置（三列均匀）
            var cardRT = cardT.GetComponent<RectTransform>();
            if (cardRT != null)
            {
                float xPos = (i - 1) * 270f; // -270, 0, 270
                cardRT.anchoredPosition = new Vector2(xPos, -30f);
                cardRT.sizeDelta = new Vector2(240, 320);
            }

            // 删除旧的图标（如果存在）
            var oldIcon = cardT.Find("DiffIcon");
            if (oldIcon != null) Object.DestroyImmediate(oldIcon.gameObject);

            // 添加难度图标
            if (icons[i] != null)
            {
                var iconGo = new GameObject("DiffIcon");
                iconGo.transform.SetParent(cardT, false);
                var iconRT = iconGo.AddComponent<RectTransform>();
                iconRT.anchorMin = new Vector2(0.5f, 1f);
                iconRT.anchorMax = new Vector2(0.5f, 1f);
                iconRT.pivot = new Vector2(0.5f, 1f);
                iconRT.sizeDelta = new Vector2(60, 60);
                iconRT.anchoredPosition = new Vector2(0, -15);
                var iconImg = iconGo.AddComponent<Image>();
                iconImg.sprite = icons[i];
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }

            // CardTitle
            var cardTitle = cardT.Find("CardTitle");
            if (cardTitle != null)
            {
                var tmp = cardTitle.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.text = card.title;
                    tmp.fontSize = 30;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.alignment = TextAlignmentOptions.Center;
                    if (font != null) tmp.font = font;
                    SetTMPColor(tmp, card.titleColor);
                }
                var rt = cardTitle.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = new Vector2(0, 75);
            }

            // AudienceText
            var audienceText = cardT.Find("AudienceText");
            if (audienceText != null)
            {
                var tmp = audienceText.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.text = card.audience;
                    tmp.fontSize = 18;
                    tmp.alignment = TextAlignmentOptions.Center;
                    if (font != null) tmp.font = font;
                    SetTMPColor(tmp, new Color(0.8f, 0.85f, 0.9f, 0.9f));
                }
                var rt = audienceText.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = new Vector2(0, 45);
            }

            // DetailText
            var detailText = cardT.Find("DetailText");
            if (detailText != null)
            {
                var tmp = detailText.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.text = card.detail;
                    tmp.fontSize = 18;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.lineSpacing = 5;
                    if (font != null) tmp.font = font;
                    SetTMPColor(tmp, new Color(0.75f, 0.78f, 0.85f, 0.85f));
                }
                var rt = detailText.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(0, -20);
                    rt.sizeDelta = new Vector2(200, 100);
                }
            }

            // SelectLabel（底部"选择"按钮文字）
            var selectLabel = cardT.Find("SelectLabel");
            if (selectLabel != null)
            {
                var tmp = selectLabel.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.text = card.btnLabel;
                    tmp.fontSize = 24;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.alignment = TextAlignmentOptions.Center;
                    if (font != null) tmp.font = font;
                    SetTMPColor(tmp, new Color(1f, 0.95f, 0.8f, 1f));
                }
                var rt = selectLabel.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = new Vector2(0, -130);
            }

            // Button 颜色调整
            var btn = cardT.GetComponent<Button>();
            if (btn != null)
            {
                var colors = btn.colors;
                colors.normalColor      = Color.white;
                colors.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
                colors.pressedColor     = new Color(0.8f, 0.8f, 0.8f, 1f);
                colors.selectedColor    = Color.white;
                btn.colors = colors;
            }
        }

        // ── 绑定 Inspector 字段 ──
        var diffUI = canvas.GetComponent<DrscfZ.UI.DifficultySelectUI>();
        if (diffUI != null)
        {
            var so = new SerializedObject(diffUI);

            var panelProp = so.FindProperty("_panel");
            if (panelProp != null) panelProp.objectReferenceValue = bgOverlay.gameObject;

            var easyProp = so.FindProperty("_easyBtn");
            if (easyProp != null && contentBox.Find("Card0") != null)
                easyProp.objectReferenceValue = contentBox.Find("Card0").GetComponent<Button>();

            var normalProp = so.FindProperty("_normalBtn");
            if (normalProp != null && contentBox.Find("Card1") != null)
                normalProp.objectReferenceValue = contentBox.Find("Card1").GetComponent<Button>();

            var hardProp = so.FindProperty("_hardBtn");
            if (hardProp != null && contentBox.Find("Card2") != null)
                hardProp.objectReferenceValue = contentBox.Find("Card2").GetComponent<Button>();

            var titleProp = so.FindProperty("_titleText");
            if (titleProp != null && titleText != null)
                titleProp.objectReferenceValue = titleText.GetComponent<TMP_Text>();

            var descProp = so.FindProperty("_descText");
            if (descProp != null && descText != null)
                descProp.objectReferenceValue = descText.GetComponent<TMP_Text>();

            so.ApplyModifiedProperties();
        }

        // ── 保存场景 ──
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SetupDifficultyUI] 难度选择界面优化完成，场景已保存");
    }

    private static void SetTMPColor(TMP_Text tmp, Color color)
    {
        var so = new SerializedObject(tmp);
        var fontColorProp = so.FindProperty("m_fontColor");
        if (fontColorProp != null) fontColorProp.colorValue = color;
        var fontColor32Prop = so.FindProperty("m_fontColor32");
        if (fontColor32Prop != null) fontColor32Prop.colorValue = color;
        so.ApplyModifiedProperties();
    }
}
