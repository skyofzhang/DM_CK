using UnityEditor;
using UnityEngine;

public class ExecuteMenuItemScript
{
    public static void Execute()
    {
        bool success = EditorApplication.ExecuteMenuItem("Tools/DrscfZ/Fix Worker Mesh (Capsule -> CowWorker)");
        if (success)
        {
            Debug.Log("Successfully executed menu item: Tools/DrscfZ/Fix Worker Mesh (Capsule -> CowWorker)");
        }
        else
        {
            Debug.LogError("Failed to execute menu item: Tools/DrscfZ/Fix Worker Mesh (Capsule -> CowWorker)");
        }
    }
}
