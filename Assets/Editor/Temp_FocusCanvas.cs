using UnityEngine;
using UnityEditor;

public class Temp_FocusCanvas
{
    public static void Execute()
    {
        // Find and frame the Canvas
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            // Try finding inactive too
            var all = Resources.FindObjectsOfTypeAll<Canvas>();
            foreach (var c in all)
            {
                if (c.name == "Canvas")
                {
                    canvas = c.gameObject;
                    break;
                }
            }
        }

        if (canvas != null)
        {
            Selection.activeGameObject = canvas;

            // Frame the selection in scene view
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.in2DMode = true;
                sceneView.FrameSelected();
                sceneView.Repaint();
            }
            Debug.Log($"[FocusCanvas] Canvas framed. Position: {canvas.transform.position}");
        }
        else
        {
            Debug.LogWarning("[FocusCanvas] Canvas not found!");
        }
    }
}
