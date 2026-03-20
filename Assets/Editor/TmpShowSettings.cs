using UnityEngine; using UnityEditor;
public static class TmpShowSettings {
    public static void Execute() {
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go.name == "SurvivalSettingsPanel" && go.scene.name == "MainScene")
                go.SetActive(false);
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go.name == "LobbyPanel" && go.scene.name == "MainScene")
                go.SetActive(false);
        Debug.Log("All hidden");
    }
}