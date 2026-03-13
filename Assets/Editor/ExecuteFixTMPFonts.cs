using UnityEditor;
using UnityEngine;

namespace DrscfZ.Editor
{
    public static class ExecuteFixTMPFonts
    {
        public static void Execute()
        {
            // Clear console before execution
            var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
            var clearMethod = logEntries?.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            clearMethod?.Invoke(null, null);

            // Execute the menu item
            EditorApplication.ExecuteMenuItem("Tools/DrscfZ/Fix TMP Fonts (→ ChineseFont SDF)");
        }
    }
}
