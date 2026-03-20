using UnityEngine;
using UnityEditor;
using System.IO;

public class GenerateTabSprites
{
    [MenuItem("Tools/DrscfZ/Generate Tab Sprites")]
    public static void Execute()
    {
        // 生成选中态和未选中态的页签按钮图片
        GenerateTab("tab_active",   new Color32(26, 39, 68, 255),  new Color32(77, 200, 255, 255), true);
        GenerateTab("tab_inactive", new Color32(15, 26, 46, 255),  new Color32(58, 80, 112, 200),  false);

        AssetDatabase.Refresh();
        Debug.Log("[GenerateTabSprites] 页签素材已生成");
    }

    static void GenerateTab(string name, Color32 bgColor, Color32 borderColor, bool glow)
    {
        int w = 240, h = 60;
        int borderW = 2;
        int cornerR = 12;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color32[w * h];

        // 填充透明
        var clear = new Color32(0, 0, 0, 0);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // 圆角矩形判定
                float dx = 0, dy = 0;
                if (x < cornerR && y < cornerR) { dx = cornerR - x - 0.5f; dy = cornerR - y - 0.5f; }
                else if (x >= w - cornerR && y < cornerR) { dx = x - (w - cornerR) + 0.5f; dy = cornerR - y - 0.5f; }
                else if (x < cornerR && y >= h - cornerR) { dx = cornerR - x - 0.5f; dy = y - (h - cornerR) + 0.5f; }
                else if (x >= w - cornerR && y >= h - cornerR) { dx = x - (w - cornerR) + 0.5f; dy = y - (h - cornerR) + 0.5f; }

                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > cornerR) continue; // 圆角外

                // 判断是否在边框区域
                bool isBorder = (x < borderW || x >= w - borderW || y < borderW || y >= h - borderW);
                // 圆角处的边框
                if (dist > cornerR - borderW) isBorder = true;

                if (isBorder)
                {
                    pixels[y * w + x] = borderColor;
                }
                else
                {
                    // 内部背景
                    if (glow)
                    {
                        // 选中态：内边缘微光效果
                        float edgeDist = Mathf.Min(x - borderW, w - borderW - x, y - borderW, h - borderW - y);
                        if (dist > 0) edgeDist = Mathf.Min(edgeDist, cornerR - dist);
                        float glowFactor = Mathf.Clamp01(1f - edgeDist / 15f) * 0.3f;
                        Color32 c = LerpColor32(bgColor, borderColor, glowFactor);
                        pixels[y * w + x] = c;
                    }
                    else
                    {
                        pixels[y * w + x] = bgColor;
                    }
                }
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        string dir = "Assets/Art/UI/Rankings";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        string path = $"{dir}/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);

        // 设置为 Sprite + 9-slice border
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(cornerR + 2, cornerR + 2, cornerR + 2, cornerR + 2); // 9-slice
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        Debug.Log($"[GenerateTabSprites] {name}.png → {path}");
    }

    static Color32 LerpColor32(Color32 a, Color32 b, float t)
    {
        return new Color32(
            (byte)(a.r + (b.r - a.r) * t),
            (byte)(a.g + (b.g - a.g) * t),
            (byte)(a.b + (b.b - a.b) * t),
            (byte)(a.a + (b.a - a.a) * t)
        );
    }
}
