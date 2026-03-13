using UnityEditor;
using UnityEditor.SceneManagement;

public class SaveCurrentScene
{
    public static void Execute()
    {
        EditorSceneManager.SaveOpenScenes();
        UnityEngine.Debug.Log("[SaveCurrentScene] 场景已保存");
    }
}
