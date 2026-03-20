using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// 为 LobbyPanel 的3个按钮应用统一的冬日风格底图，并设置文字。
/// 按钮规格：
///   StartBtn   → 宽300 高80，btn_winter_wide  ，文字"开始游戏"
///   RankingBtn → 宽180 高64，btn_winter_small ，文字"排行榜"
///   SettingsBtn→ 宽180 高64，btn_winter_small ，文字"设置"
/// </summary>
public static class SetupLobbyButtons
{
    [MenuItem("Tools/DrscfZ/Setup Lobby Buttons")]
    public static void Execute()
    {
        // ── 1. 确保资源以 Sprite 类型导入 ────────────────────────
        EnsureSprite("Assets/Art/UI/Buttons/btn_winter_wide.png");
        EnsureSprite("Assets/Art/UI/Buttons/btn_winter_small.png");
        AssetDatabase.Refresh();

        var wideSprite  = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Buttons/btn_winter_wide.png");
        var smallSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Buttons/btn_winter_small.png");

        if (wideSprite == null)  { Debug.LogError("[SetupLobbyButtons] btn_winter_wide 未找到，请先运行 Agent 生成图片"); return; }
        if (smallSprite == null) { Debug.LogError("[SetupLobbyButtons] btn_winter_small 未找到，请先运行 Agent 生成图片"); return; }

        // ── 2. 找 LobbyPanel ──────────────────────────────────────
        GameObject lobby = null;
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go.name == "LobbyPanel" && go.scene.name == "MainScene") { lobby = go; break; }
        if (lobby == null) { Debug.LogError("[SetupLobbyButtons] LobbyPanel 未找到"); return; }

        // ── 3. 应用按钮 ───────────────────────────────────────────
        Apply(lobby, "StartBtn",    wideSprite,  300, 80, "▶  开始游戏", 26, FontStyles.Bold);
        Apply(lobby, "RankingBtn",  smallSprite, 180, 64, "排行榜",      22, FontStyles.Bold);
        Apply(lobby, "SettingsBtn", smallSprite, 180, 64, "设置",        22, FontStyles.Bold);

        // ── 4. 保存 ───────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(lobby.scene);
        EditorSceneManager.SaveScene(lobby.scene);
        Debug.Log("[SetupLobbyButtons] ✅ 完成！场景已保存。");
    }

    static void Apply(GameObject parent, string btnName, Sprite sprite,
                      float w, float h, string label, int fontSize, FontStyles style)
    {
        var t = parent.transform.Find(btnName);
        if (t == null) { Debug.LogWarning($"[SetupLobbyButtons] {btnName} not found"); return; }

        // RectTransform 尺寸
        var rt = t.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,   h);
        }

        // Image sprite
        var img = t.GetComponent<Image>();
        if (img != null)
        {
            img.sprite           = sprite;
            img.color            = Color.white;
            img.type             = Image.Type.Sliced;   // 九宫格拉伸
            img.pixelsPerUnitMultiplier = 1f;
            img.preserveAspect   = false;
        }

        // TMP 文字
        var tmp = t.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
        {
            tmp.text      = label;
            tmp.fontSize  = fontSize;
            tmp.fontStyle = style;
            tmp.color     = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            // 增加描边，确保在任何背景下可读
            tmp.outlineWidth = 0.2f;
            tmp.outlineColor = new Color32(0, 30, 80, 200);
            // 关闭溢出截断
            tmp.overflowMode = TextOverflowModes.Overflow;
        }
        else
        {
            // fallback：Legacy Text
            var legText = t.GetComponentInChildren<Text>(true);
            if (legText != null)
            {
                legText.text      = label;
                legText.fontSize  = fontSize;
                legText.fontStyle = FontStyle.Bold;
                legText.color     = Color.white;
                legText.alignment = TextAnchor.MiddleCenter;
            }
        }

        Debug.Log($"[SetupLobbyButtons] ✅ {btnName} → size=({w}x{h}) text=\"{label}\"");
    }

    static void EnsureSprite(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType          = TextureImporterType.Sprite;
            importer.spriteImportMode     = SpriteImportMode.Single;
            importer.alphaIsTransparency  = true;
            // 九宫格边距（留16px做边框区域）
            importer.spriteBorder         = new Vector4(16, 16, 16, 16);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }
}
