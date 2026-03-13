using UnityEngine;
using UnityEditor;

namespace DrscfZ.Editor
{
    public static class PreviewLobby
    {
        [MenuItem("Tools/DrscfZ/Toggle LobbyPanel Preview")]
        public static void Execute()
        {
            var lobby = GameObject.Find("Canvas")?.transform.Find("LobbyPanel")?.gameObject;
            if (lobby == null) { Debug.LogError("LobbyPanel not found"); return; }
            lobby.SetActive(!lobby.activeSelf);
            Debug.Log($"[Preview] LobbyPanel → {lobby.activeSelf}");
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
    }
}
