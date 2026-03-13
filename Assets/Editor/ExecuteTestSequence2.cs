using UnityEditor;
using UnityEngine;
using System.Collections;

public class ExecuteTestSequence2
{
    public static void Execute()
    {
        Debug.Log("[ExecuteTestSequence2] Starting test sequence...");
        
        // Step 1: Enter Play Mode
        Debug.Log("[ExecuteTestSequence2] Step 1: Entering Play Mode...");
        EditorApplication.isPlaying = true;
        
        // Schedule next steps
        EditorApplication.update += WaitAndExecuteNextSteps;
    }
    
    private static float startTime = 0f;
    private static int step = 0;
    
    private static void WaitAndExecuteNextSteps()
    {
        if (step == 0)
        {
            // Wait for Play Mode to start
            if (EditorApplication.isPlaying)
            {
                startTime = Time.realtimeSinceStartup;
                step = 1;
                Debug.Log("[ExecuteTestSequence2] Play Mode entered. Waiting 6 seconds for scene loading...");
            }
        }
        else if (step == 1)
        {
            // Wait 6 seconds
            if (Time.realtimeSinceStartup - startTime >= 6f)
            {
                step = 2;
                startTime = Time.realtimeSinceStartup;
                Debug.Log("[ExecuteTestSequence2] Step 2: Executing 'Start Simulate'...");
                bool success = EditorApplication.ExecuteMenuItem("Tools/DrscfZ/Test/Start Simulate (Play Mode)");
                Debug.Log($"[ExecuteTestSequence2] Start Simulate executed: {success}");
            }
        }
        else if (step == 2)
        {
            // Wait 8 seconds for server to push events
            if (Time.realtimeSinceStartup - startTime >= 8f)
            {
                step = 3;
                startTime = Time.realtimeSinceStartup;
                Debug.Log("[ExecuteTestSequence2] Step 3: Executing 'Check Worker Status'...");
                bool success = EditorApplication.ExecuteMenuItem("Tools/DrscfZ/Test/Check Worker Status");
                Debug.Log($"[ExecuteTestSequence2] Check Worker Status executed: {success}");
            }
        }
        else if (step == 3)
        {
            // Wait 2 seconds
            if (Time.realtimeSinceStartup - startTime >= 2f)
            {
                step = 4;
                Debug.Log("[ExecuteTestSequence2] Step 4: Test sequence completed. Collecting logs...");
                EditorApplication.update -= WaitAndExecuteNextSteps;
            }
        }
    }
}
