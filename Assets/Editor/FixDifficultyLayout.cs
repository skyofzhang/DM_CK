using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEditor.SceneManagement;

public class FixDifficultyLayout
{
    [MenuItem("Tools/DrscfZ/Fix Difficulty Card Layout")]
    public static void Execute()
    {
        var contentBox = GameObject.Find("Canvas/DifficultySelect/BgOverlay/ContentBox");
        if (contentBox == null) { Debug.LogError("ContentBox not found"); return; }

        // ── 1. 加大 ContentBox 高度，给标题和卡片更多空间 ──
        var cbRT = contentBox.GetComponent<RectTransform>();
        cbRT.sizeDelta = new Vector2(860, 680);

        // ── 2. TitleText 上移 ──
        var titleText = contentBox.transform.Find("TitleText");
        if (titleText != null)
        {
            var rt = titleText.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, 300);
        }

        // ── 3. DescText 标题下方，间距拉开 ──
        var descText = contentBox.transform.Find("DescText");
        if (descText != null)
        {
            var rt = descText.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, 260);
            var tmp = descText.GetComponent<TMP_Text>();
            if (tmp != null) tmp.fontSize = 20;
        }

        // ── 4. 卡片下移，给标题区腾空间 ──
        // Card 高度 310, ContentBox 高度 680
        // 卡片区中心在 y=-40 (标题占上方)
        string[] cards = { "Card0", "Card1", "Card2" };
        float[] cardX = { -270, 0, 270 };
        for (int i = 0; i < cards.Length; i++)
        {
            var card = contentBox.transform.Find(cards[i]);
            if (card == null) continue;

            var crt = card.GetComponent<RectTransform>();
            crt.anchoredPosition = new Vector2(cardX[i], -40);
            crt.sizeDelta = new Vector2(240, 320);

            // CardTitle: 卡片顶部区域
            var title = card.Find("CardTitle");
            if (title != null)
            {
                var rt = title.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0, 120);
                var tmp = title.GetComponent<TMP_Text>();
                if (tmp != null) tmp.fontSize = 30;
            }

            // AudienceText: 标题下方
            var audience = card.Find("AudienceText");
            if (audience != null)
            {
                var rt = audience.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0, 80);
                var tmp = audience.GetComponent<TMP_Text>();
                if (tmp != null) tmp.fontSize = 20;
            }

            // DetailText: 卡片中部
            var detail = card.Find("DetailText");
            if (detail != null)
            {
                var rt = detail.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0, -20);
                rt.sizeDelta = new Vector2(210, 130);
                var tmp = detail.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.fontSize = 20;
                    tmp.lineSpacing = 8;
                }
            }

            // SelectLabel: 卡片底部内侧
            var selectLabel = card.Find("SelectLabel");
            if (selectLabel != null)
            {
                var rt = selectLabel.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0, -130);
                var tmp = selectLabel.GetComponent<TMP_Text>();
                if (tmp != null) tmp.fontSize = 24;
            }
        }

        // ── 保存场景 ──
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[FixDifficultyLayout] 难度选择界面布局已修复（含ContentBox扩大+标题间距+卡片内部）");
    }
}
