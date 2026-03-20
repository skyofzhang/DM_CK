using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public static class InspectLobbyPanel
{
    [MenuItem("Tools/DrscfZ/Inspect LobbyPanel")]
    public static void Execute()
    {
        var allGO = Resources.FindObjectsOfTypeAll<GameObject>();
        GameObject lobby = null;
        foreach (var go in allGO)
            if (go.name == "LobbyPanel" && go.scene.name == "MainScene") { lobby = go; break; }

        if (lobby == null) { Debug.LogError("LobbyPanel not found"); return; }

        Debug.Log($"=== LobbyPanel (active={lobby.activeSelf}) ===");
        foreach (Transform t in lobby.GetComponentsInChildren<Transform>(true))
        {
            var img = t.GetComponent<Image>();
            var btn = t.GetComponent<Button>();
            string line = $"[{(t.gameObject.activeSelf?"ON":"off")}] {t.name}";
            if (img != null) { string sn = (img.sprite != null) ? img.sprite.name : "null"; line += $" [Image sprite={sn} color=#{ColorUtility.ToHtmlStringRGBA(img.color)}]"; }
            if (btn != null) line += " [Button]";
            Debug.Log(line);
        }
    }
}
