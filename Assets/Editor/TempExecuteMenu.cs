using UnityEditor;
using UnityEngine;

public class TempExecuteMenu
{
    public static void Execute()
    {
        Debug.Log("Executing menu item: Tools/DrscfZ/Fix Worker Mesh (Capsule → CowWorker)");
        bool success = EditorApplication.ExecuteMenuItem("Tools/DrscfZ/Fix Worker Mesh (Capsule → CowWorker)");
        if (success)
        {
            Debug.Log("Menu item executed successfully.");
        }
        else
        {
            Debug.LogError("Failed to execute menu item. Make sure the path is correct.");
        }
    }
}
