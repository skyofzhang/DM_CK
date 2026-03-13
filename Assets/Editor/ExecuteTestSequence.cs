using UnityEngine;
using UnityEditor;
using System.Collections;

public class ExecuteTestSequence
{
    public static void Execute()
    {
        EditorApplication.update += ExecuteSequence;
    }

    private static int step = 0;
    private static double startTime = 0;
    private static double waitUntil = 0;

    private static void ExecuteSequence()
    {
        double currentTime = EditorApplication.timeSinceStartup;

        switch (step)
        {
            case 0:
                Debug.Log("=== Starting Test Sequence ===");
                Debug.Log("Step 1: Entering Play Mode...");
                EditorApplication.isPlaying = true;
                step++;
                startTime = currentTime;
                waitUntil = currentTime + 5.0; // Wait 5 seconds
                break;

            case 1:
                // Wait for Play Mode to be active
                if (!EditorApplication.isPlaying)
                    return;
                
                if (currentTime < waitUntil)
                    return;

                Debug.Log("Step 2: 5 seconds elapsed, executing 'Tools/DrscfZ/Test/▶ Start Simulate (Play Mode)'...");
                EditorApplication.ExecuteMenuItem("Tools/DrscfZ/Test/▶ Start Simulate (Play Mode)");
                step++;
                waitUntil = currentTime + 4.0; // Wait 4 seconds
                break;

            case 2:
                if (currentTime < waitUntil)
                    return;

                Debug.Log("Step 3: 4 seconds elapsed, executing 'Tools/DrscfZ/Test/🔍 Check Worker Status'...");
                EditorApplication.ExecuteMenuItem("Tools/DrscfZ/Test/🔍 Check Worker Status");
                step++;
                waitUntil = currentTime + 1.0; // Wait 1 second for logs to appear
                break;

            case 3:
                if (currentTime < waitUntil)
                    return;

                Debug.Log("Step 4: Checking Console logs (see above for worker status results)");
                Debug.Log("Step 5: Exiting Play Mode...");
                EditorApplication.isPlaying = false;
                step++;
                waitUntil = currentTime + 1.0;
                break;

            case 4:
                if (currentTime < waitUntil)
                    return;

                Debug.Log("=== Test Sequence Complete ===");
                EditorApplication.update -= ExecuteSequence;
                step = 0;
                break;
        }
    }
}
