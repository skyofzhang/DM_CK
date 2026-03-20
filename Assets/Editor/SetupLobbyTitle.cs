using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// 将 LobbyPanel 的文字标题替换为冰晶艺术字图片 title_logo.png
/// - 隐藏 TitleText（TMP）
/// - 创建/更新 TitleImage（Image 组件）显示 logo
/// - 位置：顶部居中，高约 160px
/// </summary>
public static class SetupLobbyTitle
{
    [MenuItem("Tools/DrscfZ/Setup Lobby Title Logo")]
    public static void Execute()
    {
        const string logoPath = "Assets/Art/UI/MainMenu/title_logo.png";

        // ── 1. 确保 logo 以 Sprite 导入 ──────────────────────────
        var importer = AssetImporter.GetAtPath(logoPath) as TextureImporter;
        if (importer == null) { Debug.LogError("[SetupLobbyTitle] title_logo.png 未找到，路径: " + logoPath); return; }

        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType         = TextureImporterType.Sprite;
            importer.spriteImportMode    = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled       = false;   // UI 不需要 mipmap
            AssetDatabase.ImportAsset(logoPath, ImportAssetOptions.ForceUpdate);
            Debug.Log("[SetupLobbyTitle] title_logo.png → 重导入为 Sprite");
        }

        var logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>(logoPath);
        if (logoSprite == null) { Debug.LogError("[SetupLobbyTitle] 加载 Sprite 失败"); return; }

        // ── 2. 找 LobbyPanel ──────────────────────────────────────
        GameObject lobby = null;
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go.name == "LobbyPanel" && go.scene.name == "MainScene") { lobby = go; break; }
        if (lobby == null) { Debug.LogError("[SetupLobbyTitle] LobbyPanel 未找到"); return; }

        // ── 3. 隐藏原有文字标题 TitleText ─────────────────────────
        var titleTextT = lobby.transform.Find("TitleText");
        if (titleTextT != null)
        {
            titleTextT.gameObject.SetActive(false);
            Debug.Log("[SetupLobbyTitle] TitleText → 已隐藏");
        }

        // ── 4. 创建或找到 TitleImage ──────────────────────────────
        var existingT = lobby.transform.Find("TitleImage");
        GameObject titleGO = existingT != null ? existingT.gameObject : new GameObject("TitleImage");

        if (existingT == null)
        {
            titleGO.transform.SetParent(lobby.transform, false);
            // 放在 TitleText 同位置（第一个子节点之前）
            titleGO.transform.SetSiblingIndex(0);
        }

        // ── 5. RectTransform：顶部居中，500×198px（保持 900:356 ≈ 2.53:1 比例）
        var rt = titleGO.GetComponent<RectTransform>();
        if (rt == null) rt = titleGO.AddComponent<RectTransform>();

        rt.anchorMin = new Vector2(0.5f, 1f);   // 顶部中央锚点
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, -60f);  // 距顶边 60px
        rt.sizeDelta = new Vector2(500f, 198f);       // 宽500 × 高198（2.53:1）

        // ── 6. Image 组件 ──────────────────────────────────────────
        var img = titleGO.GetComponent<Image>();
        if (img == null) img = titleGO.AddComponent<Image>();

        img.sprite          = logoSprite;
        img.color           = Color.white;
        img.type            = Image.Type.Simple;
        img.preserveAspect  = true;   // 保持原始宽高比
        img.raycastTarget   = false;  // 标题不需要响应点击

        // ── 7. 保存 ───────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(lobby.scene);
        EditorSceneManager.SaveScene(lobby.scene);
        Debug.Log("[SetupLobbyTitle] ✅ 完成！TitleImage 已设置，场景已保存。");
    }
}
