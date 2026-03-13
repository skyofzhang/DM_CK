using UnityEditor;

public class ExecuteSetupSurvivalUI
{
    [MenuItem("Tools/DrscfZ/Execute Setup Survival UI")]
    public static void Execute()
    {
        // Call the Setup Survival UI menu item
        EditorApplication.ExecuteMenuItem("Tools/DrscfZ/Setup Survival UI (LiveRanking + Camera)");
    }
}
