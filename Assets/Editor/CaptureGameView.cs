using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;

public class CaptureGameView
{
    public static string Capture()
    {
        // Method 1: Use Camera.Render to capture what Main Camera sees
        var cam = Camera.main;
        if (cam == null)
            return "ERROR: No main camera found";

        // Create a RenderTexture to render into
        int width = 540;
        int height = 960;
        RenderTexture rt = new RenderTexture(width, height, 24);
        RenderTexture prevTarget = cam.targetTexture;
        cam.targetTexture = rt;

        // Render
        cam.Render();

        // Read pixels
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        // Sample some pixels to understand what's rendering
        Color centerPixel = tex.GetPixel(width / 2, height / 2);
        Color topPixel = tex.GetPixel(width / 2, height - 10);
        Color bottomPixel = tex.GetPixel(width / 2, 10);
        Color cornerPixel = tex.GetPixel(10, height - 10);

        // Save to file
        byte[] pngBytes = tex.EncodeToPNG();
        string savePath = "D:/claude/drscfz/GameViewCapture.png";
        File.WriteAllBytes(savePath, pngBytes);

        // Restore
        cam.targetTexture = prevTarget;
        RenderTexture.active = null;
        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);

        return $"SAVED to {savePath}\n" +
               $"Center pixel: R={centerPixel.r:F3} G={centerPixel.g:F3} B={centerPixel.b:F3}\n" +
               $"Top pixel: R={topPixel.r:F3} G={topPixel.g:F3} B={topPixel.b:F3}\n" +
               $"Bottom pixel: R={bottomPixel.r:F3} G={bottomPixel.g:F3} B={bottomPixel.b:F3}\n" +
               $"Corner pixel: R={cornerPixel.r:F3} G={cornerPixel.g:F3} B={cornerPixel.b:F3}";
    }
}
