using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// 为 LobbyPanel 应用冬日主题 UI 资源：
/// - 背景：雪山星空图 bg_lobby_winter.jpg
/// - StartBtn：金色"开始玩法"按钮
/// - RankingBtn：金色"排行榜"按钮
/// - SettingsBtn：金色空白框（保留"设置"文字）
/// </summary>
public static class SetupLobbyUI
{
    [MenuItem("Tools/DrscfZ/Setup Lobby UI")]
    public static void Execute()
    {
        // ── 1. 确保新图片已导入为 Sprite ──────────────────────────
        EnsureSprite("Assets/Art/UI/Backgrounds/bg_lobby_winter.jpg");
        EnsureSprite("Assets/Art/UI/MainMenu/btn_start_game.png");
        EnsureSprite("Assets/Art/UI/MainMenu/btn_leaderboard.png");
        EnsureSprite("Assets/Art/UI/Buttons/btn_golden_wide.png");
        AssetDatabase.Refresh();

        // ── 2. 加载 Sprite ────────────────────────────────────────
        var bgSprite      = LoadSprite("Assets/Art/UI/Backgrounds/bg_lobby_winter.jpg");
        var startSprite   = LoadSprite("Assets/Art/UI/MainMenu/btn_start_game.png");
        var rankingSprite = LoadSprite("Assets/Art/UI/MainMenu/btn_leaderboard.png");
        var settingsSprite= LoadSprite("Assets/Art/UI/Buttons/btn_golden_wide.png");

        if (bgSprite == null)      { Debug.LogError("[SetupLobbyUI] bg_lobby_winter not found"); return; }
        if (startSprite == null)   { Debug.LogError("[SetupLobbyUI] btn_start_game not found"); return; }
        if (rankingSprite == null) { Debug.LogError("[SetupLobbyUI] btn_leaderboard not found"); return; }
        if (settingsSprite == null){ Debug.LogError("[SetupLobbyUI] btn_golden_wide not found"); return; }

        // ── 3. 找 LobbyPanel ──────────────────────────────────────
        var allGO = Resources.FindObjectsOfTypeAll<GameObject>();
        GameObject lobby = null;
        foreach (var go in allGO)
            if (go.name == "LobbyPanel" && go.scene.name == "MainScene") { lobby = go; break; }

        if (lobby == null) { Debug.LogError("[SetupLobbyUI] LobbyPanel not found"); return; }

        // ── 4. 背景图 ─────────────────────────────────────────────
        var lobbyImg = lobby.GetComponent<Image>();
        if (lobbyImg != null)
        {
            lobbyImg.sprite = bgSprite;
            lobbyImg.color  = new Color(1f, 1f, 1f, 0.95f);   // 近全白，让图片自然显示
            lobbyImg.type   = Image.Type.Simple;
            lobbyImg.preserveAspect = false;
            Debug.Log("[SetupLobbyUI] ✅ 背景图已设置");
        }

        // ── 5. 子节点按钮 ─────────────────────────────────────────
        ApplyButton(lobby, "StartBtn",   startSprite,   clearText: true,  newText: "");
        ApplyButton(lobby, "RankingBtn", rankingSprite, clearText: true,  newText: "");
        ApplyButton(lobby, "SettingsBtn",settingsSprite,clearText: false, newText: "设置");

        // ── 6. 保存场景 ───────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(lobby.scene);
        EditorSceneManager.SaveScene(lobby.scene);
        Debug.Log("[SetupLobbyUI] ✅ 场景已保存。LobbyPanel UI 更新完成！");
    }

    static void ApplyButton(GameObject parent, string btnName, Sprite sprite, bool clearText, string newText)
    {
        var t = parent.transform.Find(btnName);
        if (t == null) { Debug.LogWarning($"[SetupLobbyUI] {btnName} not found in {parent.name}"); return; }

        var img = t.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = sprite;
            img.color  = Color.white;
            img.type   = Image.Type.Simple;
            img.preserveAspect = false;
        }

        // 处理子 Text（TMP 或 Legacy）
        var tmp = t.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            if (clearText) tmp.text = "";
            else           tmp.text = newText;
        }
        else
        {
            var legText = t.GetComponentInChildren<Text>();
            if (legText != null)
            {
                if (clearText) legText.text = "";
                else           legText.text = newText;
            }
        }

        Debug.Log($"[SetupLobbyUI] ✅ {btnName} sprite 已设置 → {sprite.name}, text={(clearText ? "(cleared)" : newText)}");
    }

    static void EnsureSprite(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            Debug.Log($"[SetupLobbyUI] 重导入为 Sprite: {path}");
        }
    }

    static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
