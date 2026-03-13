using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using System.Collections.Generic;

public class ForceRecompileAndReport : EditorWindow
{
    private static bool waitingForCompile = false;
    private static List<string> errorLogs = new List<string>();

    [MenuItem("Tools/Force Recompile And Report")]
    public static void Execute()
    {
        errorLogs.Clear();
        Application.logMessageReceived += OnLogMessageReceived;
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
        waitingForCompile = true;
        EditorApplication.update += WaitForCompile;
    }

    private static void WaitForCompile()
    {
        if (!EditorApplication.isCompiling)
        {
            EditorApplication.update -= WaitForCompile;
            Application.logMessageReceived -= OnLogMessageReceived;
            waitingForCompile = false;
            if (EditorUtility.scriptCompilationFailed || errorLogs.Count > 0)
            {
                foreach (var log in errorLogs)
                {
                    Debug.LogError(log);
                }
            }
            else
            {
                Debug.Log("[COMPILE_OK] 编译成功无错误");
            }
        }
    }

    private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            errorLogs.Add(condition);
        }
    }
}
