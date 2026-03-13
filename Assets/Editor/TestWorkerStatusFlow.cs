using UnityEditor;
using System.Collections;
using UnityEngine;

public class TestWorkerStatusFlow
{
    public static void Execute()
    {
        // Step 1: Execute Start Simulate
        EditorApplication.ExecuteMenuItem("Tools/DrscfZ/Test/Start Simulate (Play Mode)");
        
        // Step 2: Wait 8 seconds
        EditorApplication.delayCall += () => {
            System.Threading.Thread.Sleep(8000);
            
            // Step 3: Execute Check Worker Status
            EditorApplication.ExecuteMenuItem("Tools/DrscfZ/Test/Check Worker Status");
            
            // Step 4: Wait 3 seconds
            EditorApplication.delayCall += () => {
                System.Threading.Thread.Sleep(3000);
                Debug.Log("=== TEST FLOW COMPLETED ===");
            };
        };
    }
}
