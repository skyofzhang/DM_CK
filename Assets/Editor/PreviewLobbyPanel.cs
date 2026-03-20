using UnityEngine;
using UnityEditor;

public static class PreviewLobbyPanel
{
    [MenuItem("Tools/DrscfZ/Preview Lobby Panel ON")]
    public static void ShowPanel()
    {
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go.name == "LobbyPanel" && go.scene.name == "MainScene")
            { go.SetActive(true); Debug.Log("[Preview] LobbyPanel → active"); return; }
    }

    [MenuItem("Tools/DrscfZ/Preview Lobby Panel OFF")]
    public static void HidePanel()
    {
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go.name == "LobbyPanel" && go.scene.name == "MainScene")
            { go.SetActive(false); Debug.Log("[Preview] LobbyPanel → inactive"); return; }
    }
}
