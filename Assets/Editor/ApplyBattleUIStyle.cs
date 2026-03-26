using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;

public class ApplyBattleUIStyle
{
    [MenuItem("Tools/DrscfZ/Apply Battle UI Style")]
    public static void Execute()
    {
        AssetDatabase.Refresh();

        // ── 导入素材 ──
        SetSpriteImport("Assets/Art/UI/BattleUI/topbar_bg.png",          new Vector4(30, 30, 30, 80));
        SetSpriteImport("Assets/Art/UI/BattleUI/resource_panel_bg.png",   new Vector4(30, 30, 30, 80));
        SetSpriteImport("Assets/Art/UI/BattleUI/btn_small_blue.png",      new Vector4(15, 15, 15, 15));
        SetSpriteImport("Assets/Art/UI/BattleUI/btn_exit.png",            new Vector4(15, 15, 15, 15));
        SetSpriteImport("Assets/Art/UI/BattleUI/gift_bar_bg.png",         new Vector4(30, 30, 30, 80));

        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("Canvas not found"); return; }

        var gameUI = canvas.transform.Find("GameUIPanel");
        if (gameUI == null) { Debug.LogError("GameUIPanel not found"); return; }

        // ═══════════════════ TopBar ═══════════════════
        var topBar = gameUI.Find("TopBar");
        if (topBar != null)
        {
            // TopBarBg 使用新素材
            var topBarBg = topBar.Find("TopBarBg");
            if (topBarBg != null)
            {
                var img = topBarBg.GetComponent<Image>();
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/BattleUI/topbar_bg.png");
                if (img != null && sprite != null)
                {
                    img.sprite = sprite;
                    img.type = Image.Type.Sliced;
                    img.color = new Color(1f, 1f, 1f, 0.92f);
                }
            }

            // PhaseText 样式（第X天·白天）
            var phaseText = topBar.Find("PhaseText");
            if (phaseText != null)
            {
                var tmp = phaseText.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.fontSize = 28;
                    tmp.fontStyle = FontStyles.Bold;
                    SetTMPColor(tmp, new Color(0.95f, 0.95f, 1f, 1f));
                }
            }

            // TimerText 样式
            var timerText = topBar.Find("TimerText");
            if (timerText != null)
            {
                var tmp = timerText.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.fontSize = 40;
                    tmp.fontStyle = FontStyles.Bold;
                    SetTMPColor(tmp, new Color(1f, 1f, 1f, 1f));
                }
            }

            // TimerBg 样式
            var timerBg = topBar.Find("TimerBg");
            if (timerBg != null)
            {
                var img = timerBg.GetComponent<Image>();
                if (img != null)
                    img.color = new Color(0.05f, 0.1f, 0.2f, 0.7f);
            }

            // ScorePoolBg 样式
            var scorePoolBg = topBar.Find("ScorePoolBg");
            if (scorePoolBg != null)
            {
                var img = scorePoolBg.GetComponent<Image>();
                if (img != null)
                    img.color = new Color(0.05f, 0.1f, 0.2f, 0.7f);
            }

            // ResourceRow 各图标 Value 文字颜色加亮
            var resourceRow = topBar.Find("ResourceRow");
            if (resourceRow != null)
            {
                string[] icons = { "FoodIcon", "CoalIcon", "OreIcon", "HeatIcon", "GateIcon" };
                Color[] valueColors = {
                    new Color(0.4f, 1f, 0.4f, 1f),     // 食物绿
                    new Color(1f, 0.7f, 0.3f, 1f),      // 煤炭橙
                    new Color(0.7f, 0.8f, 1f, 1f),      // 矿石蓝白
                    new Color(1f, 0.5f, 0.3f, 1f),      // 温度橙红
                    new Color(0.6f, 0.9f, 1f, 1f),      // 城门青
                };

                for (int i = 0; i < icons.Length; i++)
                {
                    var icon = resourceRow.Find(icons[i]);
                    if (icon == null) continue;

                    var valueT = icon.Find("Value");
                    if (valueT != null)
                    {
                        var tmp = valueT.GetComponent<TMP_Text>();
                        if (tmp != null)
                        {
                            tmp.fontSize = 26;
                            tmp.fontStyle = FontStyles.Bold;
                            SetTMPColor(tmp, valueColors[i]);
                        }
                    }

                    var iconT = icon.Find("Icon");
                    if (iconT != null)
                    {
                        var tmp = iconT.GetComponent<TMP_Text>();
                        if (tmp != null)
                        {
                            tmp.fontSize = 22;
                            SetTMPColor(tmp, new Color(0.85f, 0.85f, 0.9f, 0.9f));
                        }
                    }
                }
            }
        }

        // ═══════════════════ ExitBtn ═══════════════════
        var exitBtn = gameUI.Find("ExitBtn");
        if (exitBtn != null)
        {
            var img = exitBtn.GetComponent<Image>();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/BattleUI/btn_exit.png");
            if (img != null && sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
                img.color = new Color(1f, 0.4f, 0.35f, 1f); // 红色退出按钮
            }
        }

        // ═══════════════════ BtnSettings ═══════════════════
        var btnSettings = gameUI.Find("BtnSettings");
        if (btnSettings != null)
        {
            var img = btnSettings.GetComponent<Image>();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/BattleUI/btn_small_blue.png");
            if (img != null && sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
        }

        // ═══════════════════ GiftIconBar 底部背景 ═══════════════════
        var giftIconBar = gameUI.Find("GiftIconBar");
        if (giftIconBar != null)
        {
            var img = giftIconBar.GetComponent<Image>();
            if (img != null)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/BattleUI/gift_bar_bg.png");
                if (sprite != null)
                {
                    img.sprite = sprite;
                    img.type = Image.Type.Sliced;
                    img.color = new Color(1f, 1f, 1f, 0.88f);
                }
            }
        }

        // ═══════════════════ BarragePanel 弹幕区背景 ═══════════════════
        var barragePanel = gameUI.Find("BarragePanel");
        if (barragePanel != null)
        {
            var img = barragePanel.GetComponent<Image>();
            if (img != null)
                img.color = new Color(0.05f, 0.08f, 0.15f, 0.6f);
        }

        // ── 保存 ──
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ApplyBattleUIStyle] 战斗界面UI样式已更新");
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
