using UnityEngine;
using UnityEditor;
using System.Collections;

public class SimulateF5AndCheck : EditorWindow
{
    [MenuItem("Tools/Simulate F5 and Check")]
    public static void Execute()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogError("Must be in Play Mode to simulate F5!");
            return;
        }

        EditorApplication.delayCall += () =>
        {
            Debug.Log("=== Starting F5 Simulation Test ===");
            Debug.Log("Waiting 3 seconds for connection...");
            
            // Wait 3 seconds then simulate F5
            EditorApplication.delayCall += () =>
            {
                System.Threading.Thread.Sleep(3000);
                
                Debug.Log("=== 3 seconds elapsed, simulating F5 press ===");
                
                // Try to find the button and simulate click
                var simulateBtn = GameObject.Find("Canvas/BottomBar/BtnSimulate");
                if (simulateBtn != null)
                {
                    var button = simulateBtn.GetComponent<UnityEngine.UI.Button>();
                    if (button != null)
                    {
                        Debug.Log("Found BtnSimulate button, invoking onClick");
                        button.onClick.Invoke();
                    }
                    else
                    {
                        Debug.LogWarning("BtnSimulate found but has no Button component");
                    }
                }
                else
                {
                    Debug.LogWarning("BtnSimulate button not found at Canvas/BottomBar/BtnSimulate");
                }
                
                // Wait a bit then check the state
                EditorApplication.delayCall += () =>
                {
                    System.Threading.Thread.Sleep(1000);
                    CheckState();
                };
            };
        };
    }
    
    private static void CheckState()
    {
        Debug.Log("=== Checking State After F5 ===");
        
        // Check for WorkerPool/Worker_00
        var worker00 = GameObject.Find("WorkerPool/Worker_00");
        if (worker00 != null)
        {
            Debug.Log($"WorkerPool/Worker_00 found - Active: {worker00.activeSelf}");
        }
        else
        {
            Debug.LogWarning("WorkerPool/Worker_00 not found in hierarchy");
        }
        
        // Check WorkerPool children
        var workerPool = GameObject.Find("WorkerPool");
        if (workerPool != null)
        {
            Debug.Log($"WorkerPool has {workerPool.transform.childCount} children");
            for (int i = 0; i < workerPool.transform.childCount; i++)
            {
                var child = workerPool.transform.GetChild(i);
                Debug.Log($"  - {child.name}: Active={child.gameObject.activeSelf}");
            }
        }
        
        Debug.Log("=== Check Complete - Review logs above for: ===");
        Debug.Log("1. '[GM] 发送 toggle_sim enabled=True'");
        Debug.Log("2. 'survival_player_joined' related logs");
        Debug.Log("3. WorkerPool/Worker_00 active state");
    }
}
