using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using System.Collections.Generic;

public class ForceFullRecompileAndReport : EditorWindow
{
    private static bool waitingForCompile = false;
    private static List<string> errorLogs = new List<string>();
    private static bool logHooked = false;

    [MenuItem("Tools/Force Full Recompile And Report")]
    public static void ForceRecompileAndReport()
    {
        if (waitingForCompile) return;
        errorLogs.Clear();
        waitingForCompile = true;
        if (!logHooked)
        {
            Application.logMessageReceived += OnLogMessageReceived;
            logHooked = true;
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
        EditorApplication.update += WaitForCompile;
    }

    private static void WaitForCompile()
    {
        if (EditorApplication.isCompiling)
            return;
        EditorApplication.update -= WaitForCompile;
        waitingForCompile = false;
        if (EditorUtility.scriptCompilationFailed || errorLogs.Count > 0)
        {
            foreach (var err in errorLogs)
            {
                Debug.LogError(err);
            }
        }
        else
        {
            Debug.Log("[COMPILE_OK] 编译成功无错误");
        }
        errorLogs.Clear();
    }

    private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            errorLogs.Add(condition + "\n" + stackTrace);
        }
    }
}
