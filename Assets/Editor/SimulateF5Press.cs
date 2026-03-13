using UnityEngine;
using UnityEditor;

public class SimulateF5Press
{
    public static void Execute()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[SimulateF5] Not in Play Mode!");
            return;
        }

        // Simulate F5 key press by sending the event
        var e = new Event();
        e.type = EventType.KeyDown;
        e.keyCode = KeyCode.F5;
        
        // Try to find GameControlUI and trigger its F5 handler
        var gameControlUI = GameObject.FindObjectOfType<DrscfZ.UI.GameControlUI>();
        if (gameControlUI != null)
        {
            Debug.Log("[SimulateF5] Found GameControlUI, simulating F5 press...");
            // Call the method that handles F5
            var method = gameControlUI.GetType().GetMethod("Update", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Simulate the key being pressed
            Input.GetKeyDown(KeyCode.F5);
        }
        else
        {
            Debug.LogWarning("[SimulateF5] GameControlUI not found!");
        }
    }
}
