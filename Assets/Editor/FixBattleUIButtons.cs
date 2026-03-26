using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// 优化战斗主界面按钮和面板：
/// 1. ExitBtn → btn_winter_small.png + 白色 + exit图标
/// 2. BtnSettings → btn_winter_small.png + 齿轮图标
/// 3. ResourceRankPanel → resource_panel_bg.png 背景
/// </summary>
public static class FixBattleUIButtons
{
    [MenuItem("Tools/DrscfZ/Fix Battle UI Buttons")]
    public static void Execute()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("Canvas not found"); return; }
        var gameUI = canvas.transform.Find("GameUIPanel");
        if (gameUI == null) { Debug.LogError("GameUIPanel not found"); return; }

        // ── 确保素材导入为 Sprite ──
        FixImport("Assets/Art/UI/Buttons/btn_winter_small.png");
        FixImport("Assets/Art/UI/Icons/Survival/icon_exit.png");
        FixImport("Assets/Art/UI/Icons/Survival/icon_settings.png");
        FixImport("Assets/Art/UI/BattleUI/resource_panel_bg.png");

        // ── 1. ExitBtn ──
        var exitBtn = gameUI.Find("ExitBtn");
        if (exitBtn != null)
        {
            var img = exitBtn.GetComponent<Image>();
            if (img != null)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Buttons/btn_winter_small.png");
                if (sprite != null)
                {
                    img.sprite = sprite;
                    img.color = new Color(0.85f, 0.35f, 0.3f, 0.95f); // 柔和暗红
                    img.type = Image.Type.Sliced;
                    EditorUtility.SetDirty(img);
                }
            }
            // 添加退出图标子节点
            AddIconChild(exitBtn, "ExitIcon", "Assets/Art/UI/Icons/Survival/icon_exit.png",
                new Vector2(8, 0), new Vector2(28, 28));
            Debug.Log("ExitBtn updated");
        }

        // ── 2. BtnSettings ──
        var btnSettings = gameUI.Find("BtnSettings");
        if (btnSettings != null)
        {
            var img = btnSettings.GetComponent<Image>();
            if (img != null)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Buttons/btn_winter_small.png");
                if (sprite != null)
                {
                    img.sprite = sprite;
                    img.color = new Color(0.4f, 0.5f, 0.65f, 0.9f); // 冷蓝灰色
                    img.type = Image.Type.Sliced;
                    EditorUtility.SetDirty(img);
                }
            }
            // 添加设置图标子节点
            AddIconChild(btnSettings, "SettingsIcon", "Assets/Art/UI/Icons/Survival/icon_settings.png",
                new Vector2(6, 0), new Vector2(24, 24));
            Debug.Log("BtnSettings updated");
        }

        // ── 3. ResourceRankPanel 背景 ──
        var rankPanel = gameUI.Find("ResourceRankPanel");
        if (rankPanel != null)
        {
            var img = rankPanel.GetComponent<Image>();
            if (img != null)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/BattleUI/resource_panel_bg.png");
                if (sprite != null)
                {
                    img.sprite = sprite;
                    img.color = new Color(1f, 1f, 1f, 0.85f);
                    img.type = Image.Type.Sliced;
                }
                else
                {
                    // 保持纯色背景
                    img.color = new Color(0.08f, 0.12f, 0.22f, 0.8f);
                }
                EditorUtility.SetDirty(img);
            }
            Debug.Log("ResourceRankPanel background updated");
        }

        // 保存场景
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("FixBattleUIButtons: done, scene saved");
    }

    private static void FixImport(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            Debug.Log("Fixed import: " + path);
        }
    }

    private static void AddIconChild(Transform parent, string name, string spritePath,
        Vector2 localPos, Vector2 size)
    {
        // 检查是否已存在
        var existing = parent.Find(name);
        if (existing != null) return; // 已有图标

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null) { Debug.LogWarning("Sprite not found: " + spritePath); return; }

        var iconGO = new GameObject(name);
        var rt = iconGO.AddComponent<RectTransform>();
        iconGO.transform.SetParent(parent, false);

        // 左侧居中
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = localPos;
        rt.sizeDelta = size;

        var img = iconGO.AddComponent<Image>();
        img.sprite = sprite;
        img.color = Color.white;
        img.raycastTarget = false;
        img.preserveAspect = true;

        // 让图标排在文字前面（sibling index 0）
        iconGO.transform.SetAsFirstSibling();

        EditorUtility.SetDirty(iconGO);
        Debug.Log("Added icon: " + name + " -> " + spritePath);
    }
}
