using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;

public class ApplyPreGameStyle
{
    [MenuItem("Tools/DrscfZ/Apply PreGame Banner Style")]
    public static void Execute()
    {
        // 先刷新导入新素材
        AssetDatabase.Refresh();

        // 设置导入参数
        SetSpriteImport("Assets/Art/UI/Panels/pregame_center_bg.png", new Vector4(30, 30, 30, 80));
        SetSpriteImport("Assets/Art/UI/Buttons/btn_pregame_start.png", new Vector4(20, 20, 20, 20));

        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("Canvas not found"); return; }

        var banner = canvas.transform.Find("PreGameBanner");
        if (banner == null) { Debug.LogError("PreGameBanner not found"); return; }

        var bgPanel = banner.Find("BgPanel");
        var centerBox = bgPanel != null ? bgPanel.Find("CenterBox") : null;

        // ── CenterBox: 应用面板素材 ──
        if (centerBox != null)
        {
            var panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Panels/pregame_center_bg.png");
            var img = centerBox.GetComponent<Image>();
            if (img != null && panelSprite != null)
            {
                img.sprite = panelSprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
        }

        // ── StartChallengeBtn: 应用按钮素材 ──
        var startBtn = centerBox != null ? centerBox.Find("StartChallengeBtn") : null;
        if (startBtn != null)
        {
            var btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Buttons/btn_pregame_start.png");
            var img = startBtn.GetComponent<Image>();
            if (img != null && btnSprite != null)
            {
                img.sprite = btnSprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }

            // Button Color Tint 改白色
            var btn = startBtn.GetComponent<Button>();
            if (btn != null)
            {
                var colors = btn.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.85f, 0.9f, 1f, 1f);
                colors.pressedColor = new Color(0.6f, 0.7f, 0.85f, 1f);
                colors.disabledColor = new Color(0.5f, 0.5f, 0.6f, 0.7f);
                btn.colors = colors;
            }

            // 按钮文字
            var label = startBtn.Find("Label");
            if (label != null)
            {
                var tmp = label.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.fontSize = 36;
                    SetTMPColor(tmp, Color.white);
                }
            }
        }

        // ── TitleText 标题文字样式 ──
        if (centerBox != null)
        {
            var titleText = centerBox.Find("TitleText");
            if (titleText != null)
            {
                var tmp = titleText.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.fontSize = 48;
                    tmp.fontStyle = FontStyles.Bold;
                    SetTMPColor(tmp, new Color(0.95f, 0.95f, 1f, 1f));
                }
            }

            // PlayerCountText
            var countText = centerBox.Find("PlayerCountText");
            if (countText != null)
            {
                var tmp = countText.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.fontSize = 72;
                    SetTMPColor(tmp, new Color(0.4f, 0.85f, 1f, 1f));
                }
            }

            // StatusText
            var statusText = centerBox.Find("StatusText");
            if (statusText != null)
            {
                var tmp = statusText.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.fontSize = 24;
                    SetTMPColor(tmp, new Color(0.7f, 0.8f, 0.9f, 0.8f));
                }
            }
        }

        // ── 保存场景 ──
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ApplyPreGameStyle] PreGameBanner 素材和样式已更新");
    }

    static void SetSpriteImport(string path, Vector4 border)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
        }
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
