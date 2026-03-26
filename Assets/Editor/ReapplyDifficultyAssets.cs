using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

public class ReapplyDifficultyAssets
{
    [MenuItem("Tools/DrscfZ/Reapply Difficulty Assets")]
    public static void Execute()
    {
        var contentBox = GameObject.Find("Canvas/DifficultySelect/BgOverlay/ContentBox");
        if (contentBox == null) { Debug.LogError("ContentBox not found"); return; }

        // 加载素材
        var panelBg  = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Difficulty/difficulty_panel_bg.png");
        var easyBg   = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Difficulty/card_easy_bg.png");
        var normalBg = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Difficulty/card_hard_bg.png");
        var hellBg   = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Difficulty/card_hell_bg.png");

        // ContentBox 背景
        var cbImg = contentBox.GetComponent<Image>();
        if (cbImg != null && panelBg != null)
        {
            cbImg.sprite = panelBg;
            cbImg.type = Image.Type.Sliced;
            cbImg.color = Color.white;
        }

        // Card0 = 轻松 (easy)
        ApplyCardSprite(contentBox.transform.Find("Card0"), easyBg);
        // Card1 = 困难 (hard/normal)
        ApplyCardSprite(contentBox.transform.Find("Card1"), normalBg);
        // Card2 = 恐怖 (hell)
        ApplyCardSprite(contentBox.transform.Find("Card2"), hellBg);

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ReapplyDifficultyAssets] 难度选择素材已重新绑定");
    }

    static void ApplyCardSprite(Transform card, Sprite sprite)
    {
        if (card == null || sprite == null) return;
        var img = card.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }

        // Button Normal Color 改白色让素材原色显示
        var btn = card.GetComponent<Button>();
        if (btn != null)
        {
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.95f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.8f, 1f);
            btn.colors = colors;
        }
    }
}
