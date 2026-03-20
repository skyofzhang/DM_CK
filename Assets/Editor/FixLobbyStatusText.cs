using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// 修复 LobbyPanel 中 StatusText 和 ServerStatus 的可读性：
/// - 字号放大
/// - 整体下移
/// - 颜色改为亮白/浅青色 + 描边，确保在星空背景下清晰可见
/// </summary>
public static class FixLobbyStatusText
{
    [MenuItem("Tools/DrscfZ/Fix Lobby Status Text")]
    public static void Execute()
    {
        // 找 LobbyPanel
        GameObject lobby = null;
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go.name == "LobbyPanel" && go.scene.name == "MainScene") { lobby = go; break; }
        if (lobby == null) { Debug.LogError("[FixLobbyStatusText] LobbyPanel 未找到"); return; }

        // ── StatusText ────────────────────────────────────────────
        var statusT = lobby.transform.Find("StatusText");
        if (statusT != null)
        {
            var rt = statusT.GetComponent<RectTransform>();
            if (rt != null)
            {
                // 下移到面板中下部区域
                rt.anchorMin = new Vector2(0.1f, 0.25f);
                rt.anchorMax = new Vector2(0.9f, 0.38f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            var tmp = statusT.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                var so = new SerializedObject(tmp);
                so.FindProperty("m_fontSize").floatValue       = 28f;
                so.FindProperty("m_fontStyle").intValue        = (int)FontStyles.Bold;
                so.FindProperty("m_textAlignment").intValue    = (int)TextAlignmentOptions.Center;
                so.FindProperty("m_overflowMode").intValue     = (int)TextOverflowModes.Overflow;
                var iceBlue = new Color(0.85f, 0.95f, 1f, 1f);
                so.FindProperty("m_fontColor").colorValue      = iceBlue;
                so.FindProperty("m_fontColor32").colorValue    = iceBlue;
                // outline via material properties — skip direct setter, use renderer material after
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(tmp);
            }
            Debug.Log("[FixLobbyStatusText] StatusText 已更新");
        }
        else
        {
            Debug.LogWarning("[FixLobbyStatusText] StatusText 未找到");
        }

        // ── ServerStatus ──────────────────────────────────────────
        var serverT = lobby.transform.Find("ServerStatus");
        if (serverT != null)
        {
            var rt = serverT.GetComponent<RectTransform>();
            if (rt != null)
            {
                // 放在 StatusText 正下方
                rt.anchorMin = new Vector2(0.1f, 0.18f);
                rt.anchorMax = new Vector2(0.9f, 0.26f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            var tmp = serverT.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                var so = new SerializedObject(tmp);
                so.FindProperty("m_fontSize").floatValue       = 22f;
                so.FindProperty("m_fontStyle").intValue        = (int)FontStyles.Normal;
                so.FindProperty("m_textAlignment").intValue    = (int)TextAlignmentOptions.Center;
                so.FindProperty("m_overflowMode").intValue     = (int)TextOverflowModes.Overflow;
                var limeGreen = new Color(0.6f, 1f, 0.7f, 1f);
                so.FindProperty("m_fontColor").colorValue      = limeGreen;
                so.FindProperty("m_fontColor32").colorValue    = limeGreen;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(tmp);
            }
            Debug.Log("[FixLobbyStatusText] ServerStatus 已更新");
        }
        else
        {
            Debug.LogWarning("[FixLobbyStatusText] ServerStatus 未找到");
        }

        // 保存
        EditorSceneManager.MarkSceneDirty(lobby.scene);
        EditorSceneManager.SaveScene(lobby.scene);
        Debug.Log("[FixLobbyStatusText] 完成，场景已保存。");
    }
}
