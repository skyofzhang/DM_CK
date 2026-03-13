using UnityEditor;

namespace DrscfZ.Editor
{
    public static class ExecuteSetupGiftCanvas
    {
        public static void Execute()
        {
            EditorApplication.delayCall += () =>
            {
                EditorMenuItem.ExecuteMenuItem("Tools/DrscfZ/Setup Gift Canvas (T1-T5 Effects)");
            };
        }
    }

    public static class EditorMenuItem
    {
        public static void ExecuteMenuItem(string menuPath)
        {
            EditorApplication.ExecuteMenuItem(menuPath);
        }
    }
}
