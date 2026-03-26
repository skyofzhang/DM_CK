using UnityEngine;
using UnityEditor;
using System.IO;

public class GenerateLobbyAssets
{
    [MenuItem("Tools/DrscfZ/Generate Lobby Button Assets")]
    public static void Execute()
    {
        // 生成开始挑战按钮 (600x120) - 冰蓝渐变 + 圆角
        GenerateStartButton("Assets/Art/UI/Buttons/btn_lobby_start.png", 600, 120);

        // 生成小按钮 (240x80) - 排行榜/设置
        GenerateSmallButton("Assets/Art/UI/Buttons/btn_lobby_small.png", 240, 80);

        // 生成大厅面板背景 (800x600) - 半透明深蓝
        GenerateLobbyPanelBg("Assets/Art/UI/Panels/lobby_panel_bg.png", 800, 600);

        AssetDatabase.Refresh();
        Debug.Log("[GenerateLobbyAssets] 大厅按钮素材生成完成");
    }

    static void GenerateStartButton(string path, int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        float radius = h * 0.4f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = (float)x / w;
                float ny = (float)y / h;

                // 圆角
                float alpha = RoundedRectAlpha(x, y, w, h, radius);
                if (alpha <= 0) { tex.SetPixel(x, y, Color.clear); continue; }

                // 冰蓝渐变：顶部亮，底部深
                float r = Mathf.Lerp(0.15f, 0.35f, ny);
                float g = Mathf.Lerp(0.45f, 0.7f, ny);
                float b = Mathf.Lerp(0.7f, 0.95f, ny);

                // 边缘光晕
                float edgeDist = Mathf.Min(
                    Mathf.Min((float)x / w, 1f - (float)x / w),
                    Mathf.Min((float)y / h, 1f - (float)y / h)
                );
                float edgeGlow = Mathf.Pow(1f - Mathf.Clamp01(edgeDist * 8f), 2f) * 0.4f;
                r += edgeGlow * 0.5f;
                g += edgeGlow * 0.7f;
                b += edgeGlow;

                // 内部微光
                float centerGlow = 1f - Mathf.Abs(nx - 0.5f) * 1.5f;
                centerGlow *= 1f - Mathf.Abs(ny - 0.6f) * 2f;
                centerGlow = Mathf.Max(0, centerGlow) * 0.15f;
                r += centerGlow;
                g += centerGlow;
                b += centerGlow;

                tex.SetPixel(x, y, new Color(
                    Mathf.Clamp01(r),
                    Mathf.Clamp01(g),
                    Mathf.Clamp01(b),
                    alpha * 0.92f
                ));
            }
        }

        // 顶部高光线
        for (int x = (int)(w * 0.1f); x < (int)(w * 0.9f); x++)
        {
            for (int dy = 0; dy < 3; dy++)
            {
                int py = h - 8 - dy;
                if (py < 0 || py >= h) continue;
                Color c = tex.GetPixel(x, py);
                float glow = 0.3f * (1f - (float)dy / 3f);
                tex.SetPixel(x, py, new Color(
                    Mathf.Min(1, c.r + glow),
                    Mathf.Min(1, c.g + glow),
                    Mathf.Min(1, c.b + glow),
                    c.a
                ));
            }
        }

        tex.Apply();
        SaveTexture(tex, path);
        SetSpriteImportSettings(path, new Vector4(20, 20, 20, 20));
        Object.DestroyImmediate(tex);
    }

    static void GenerateSmallButton(string path, int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        float radius = h * 0.35f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float ny = (float)y / h;
                float alpha = RoundedRectAlpha(x, y, w, h, radius);
                if (alpha <= 0) { tex.SetPixel(x, y, Color.clear); continue; }

                // 深蓝底色
                float r = Mathf.Lerp(0.08f, 0.15f, ny);
                float g = Mathf.Lerp(0.12f, 0.22f, ny);
                float b = Mathf.Lerp(0.25f, 0.4f, ny);

                // 边框光
                float edgeDist = Mathf.Min(
                    Mathf.Min((float)x / w, 1f - (float)x / w),
                    Mathf.Min((float)y / h, 1f - (float)y / h)
                );
                float edge = Mathf.Pow(1f - Mathf.Clamp01(edgeDist * 10f), 3f) * 0.3f;
                r += edge * 0.3f;
                g += edge * 0.5f;
                b += edge * 0.8f;

                tex.SetPixel(x, y, new Color(
                    Mathf.Clamp01(r),
                    Mathf.Clamp01(g),
                    Mathf.Clamp01(b),
                    alpha * 0.85f
                ));
            }
        }

        tex.Apply();
        SaveTexture(tex, path);
        SetSpriteImportSettings(path, new Vector4(15, 15, 15, 15));
        Object.DestroyImmediate(tex);
    }

    static void GenerateLobbyPanelBg(string path, int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        float radius = 30f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float ny = (float)y / h;
                float alpha = RoundedRectAlpha(x, y, w, h, radius);
                if (alpha <= 0) { tex.SetPixel(x, y, Color.clear); continue; }

                float r = Mathf.Lerp(0.04f, 0.08f, ny);
                float g = Mathf.Lerp(0.06f, 0.12f, ny);
                float b = Mathf.Lerp(0.12f, 0.2f, ny);

                tex.SetPixel(x, y, new Color(r, g, b, alpha * 0.75f));
            }
        }

        tex.Apply();
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        SaveTexture(tex, path);
        SetSpriteImportSettings(path, new Vector4(25, 25, 25, 25));
        Object.DestroyImmediate(tex);
    }

    // ── 圆角矩形 alpha ──
    static float RoundedRectAlpha(int x, int y, int w, int h, float r)
    {
        float px = x, py = y;
        // 四个角的距离检查
        if (px < r && py < r)
            return Mathf.Clamp01((r - Vector2.Distance(new Vector2(px, py), new Vector2(r, r))) * 2f);
        if (px > w - r && py < r)
            return Mathf.Clamp01((r - Vector2.Distance(new Vector2(px, py), new Vector2(w - r, r))) * 2f);
        if (px < r && py > h - r)
            return Mathf.Clamp01((r - Vector2.Distance(new Vector2(px, py), new Vector2(r, h - r))) * 2f);
        if (px > w - r && py > h - r)
            return Mathf.Clamp01((r - Vector2.Distance(new Vector2(px, py), new Vector2(w - r, h - r))) * 2f);
        return 1f;
    }

    static void SaveTexture(Texture2D tex, string path)
    {
        string fullPath = Path.Combine(Application.dataPath, "..", path);
        string dir = Path.GetDirectoryName(fullPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllBytes(fullPath, tex.EncodeToPNG());
    }

    static void SetSpriteImportSettings(string path, Vector4 border)
    {
        AssetDatabase.Refresh();
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
        }
    }
}
