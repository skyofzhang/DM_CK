using UnityEngine;
using UnityEditor;

public static class PreviewLoadingScreen
{
    [MenuItem("Tools/DrscfZ/Preview Loading Screen ON")]
    public static void ShowPanel()
    {
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go.name == "LobbyPanel" && go.scene.name == "MainScene")
                go.SetActive(false);

        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go.name == "LoadingScreen" && go.scene.name == "MainScene")
            { go.SetActive(true); Debug.Log("[Preview] LoadingScreen → active"); return; }
    }

    [MenuItem("Tools/DrscfZ/Preview Loading Screen OFF")]
    public static void HidePanel()
    {
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go.name == "LoadingScreen" && go.scene.name == "MainScene")
            { go.SetActive(false); Debug.Log("[Preview] LoadingScreen → inactive"); return; }
    }
}
