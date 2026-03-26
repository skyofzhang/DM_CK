using UnityEngine;
using UnityEditor;
using System.IO;

public class GenerateDifficultyAssets
{
    [MenuItem("Tools/DrscfZ/Generate Difficulty Card Assets")]
    public static void Execute()
    {
        string dir = "Assets/Art/UI/Difficulty";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        // 卡片背景 240x310
        GenerateCardBg(dir, "card_easy_bg",  new Color(0.15f, 0.45f, 0.55f), new Color(0.1f, 0.3f, 0.4f), new Color(0.3f, 0.7f, 0.8f, 0.6f));
        GenerateCardBg(dir, "card_hard_bg",  new Color(0.2f, 0.15f, 0.5f),  new Color(0.15f, 0.1f, 0.35f), new Color(0.4f, 0.3f, 0.9f, 0.6f));
        GenerateCardBg(dir, "card_hell_bg",  new Color(0.55f, 0.1f, 0.08f), new Color(0.35f, 0.05f, 0.05f), new Color(0.9f, 0.3f, 0.1f, 0.6f));

        // 面板背景 860x540
        GeneratePanelBg(dir, "difficulty_panel_bg", 860, 540);

        // 难度图标 80x80
        GenerateDifficultyIcon(dir, "icon_easy",  new Color(0.3f, 0.8f, 0.5f), "snowflake");
        GenerateDifficultyIcon(dir, "icon_hard",  new Color(0.4f, 0.4f, 1f),   "lightning");
        GenerateDifficultyIcon(dir, "icon_hell",  new Color(1f, 0.3f, 0.15f),  "fire");

        AssetDatabase.Refresh();

        // 设置所有为 Sprite
        string[] files = {
            "card_easy_bg", "card_hard_bg", "card_hell_bg",
            "difficulty_panel_bg",
            "icon_easy", "icon_hard", "icon_hell"
        };
        foreach (var f in files)
        {
            string path = $"{dir}/{f}.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.spriteBorder = new Vector4(16, 16, 16, 16);
                importer.SaveAndReimport();
            }
        }

        Debug.Log("[GenerateDifficultyAssets] 全部素材生成完成");
    }

    static void GenerateCardBg(string dir, string name, Color topColor, Color botColor, Color borderColor)
    {
        int w = 240, h = 310;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        int borderW = 3;
        int cornerR = 16;

        for (int y = 0; y < h; y++)
        {
            float t = (float)y / h;
            Color grad = Color.Lerp(botColor, topColor, t);

            for (int x = 0; x < w; x++)
            {
                // 圆角检测
                bool inCorner = false;
                float dist = 0;
                if (x < cornerR && y < cornerR)
                    { dist = Vector2.Distance(new Vector2(x, y), new Vector2(cornerR, cornerR)); inCorner = dist > cornerR; }
                else if (x >= w - cornerR && y < cornerR)
                    { dist = Vector2.Distance(new Vector2(x, y), new Vector2(w - cornerR - 1, cornerR)); inCorner = dist > cornerR; }
                else if (x < cornerR && y >= h - cornerR)
                    { dist = Vector2.Distance(new Vector2(x, y), new Vector2(cornerR, h - cornerR - 1)); inCorner = dist > cornerR; }
                else if (x >= w - cornerR && y >= h - cornerR)
                    { dist = Vector2.Distance(new Vector2(x, y), new Vector2(w - cornerR - 1, h - cornerR - 1)); inCorner = dist > cornerR; }

                if (inCorner)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                // 边框
                bool isBorder = x < borderW || x >= w - borderW || y < borderW || y >= h - borderW;
                // 圆角边框
                if (!isBorder && dist > 0 && dist > cornerR - borderW) isBorder = true;

                if (isBorder)
                    tex.SetPixel(x, y, borderColor);
                else
                {
                    // 添加微妙的噪点纹理
                    float noise = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.08f - 0.04f;
                    Color c = grad + new Color(noise, noise, noise * 1.5f, 0);
                    c.a = 0.92f;
                    tex.SetPixel(x, y, c);
                }
            }
        }

        // 顶部高光条
        for (int x = borderW + 4; x < w - borderW - 4; x++)
        {
            for (int dy = 0; dy < 2; dy++)
            {
                int y = h - borderW - 4 - dy;
                if (y >= 0 && y < h)
                {
                    Color c = tex.GetPixel(x, y);
                    c = Color.Lerp(c, Color.white, 0.2f - dy * 0.1f);
                    tex.SetPixel(x, y, c);
                }
            }
        }

        tex.Apply();
        File.WriteAllBytes($"{dir}/{name}.png", tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    static void GeneratePanelBg(string dir, string name, int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        int borderW = 2;
        int cornerR = 20;

        for (int y = 0; y < h; y++)
        {
            float t = (float)y / h;
            for (int x = 0; x < w; x++)
            {
                bool inCorner = false;
                float dist = 0;
                if (x < cornerR && y < cornerR)
                    { dist = Vector2.Distance(new Vector2(x, y), new Vector2(cornerR, cornerR)); inCorner = dist > cornerR; }
                else if (x >= w - cornerR && y < cornerR)
                    { dist = Vector2.Distance(new Vector2(x, y), new Vector2(w - cornerR - 1, cornerR)); inCorner = dist > cornerR; }
                else if (x < cornerR && y >= h - cornerR)
                    { dist = Vector2.Distance(new Vector2(x, y), new Vector2(cornerR, h - cornerR - 1)); inCorner = dist > cornerR; }
                else if (x >= w - cornerR && y >= h - cornerR)
                    { dist = Vector2.Distance(new Vector2(x, y), new Vector2(w - cornerR - 1, h - cornerR - 1)); inCorner = dist > cornerR; }

                if (inCorner) { tex.SetPixel(x, y, Color.clear); continue; }

                bool isBorder = x < borderW || x >= w - borderW || y < borderW || y >= h - borderW;
                if (!isBorder && dist > 0 && dist > cornerR - borderW) isBorder = true;

                if (isBorder)
                    tex.SetPixel(x, y, new Color(0.3f, 0.5f, 0.7f, 0.5f));
                else
                {
                    Color bg = new Color(0.05f, 0.08f, 0.15f, 0.88f);
                    float noise = Mathf.PerlinNoise(x * 0.02f, y * 0.02f) * 0.04f;
                    bg += new Color(noise, noise, noise * 2f, 0);
                    tex.SetPixel(x, y, bg);
                }
            }
        }
        tex.Apply();
        File.WriteAllBytes($"{dir}/{name}.png", tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    static void GenerateDifficultyIcon(string dir, string name, Color mainColor, string type)
    {
        int s = 80;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        // 清空
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
                tex.SetPixel(x, y, Color.clear);

        Vector2 center = new Vector2(s / 2f, s / 2f);

        if (type == "snowflake")
        {
            // 雪花：六角星形
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = x - center.x, dy = y - center.y;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > 35) continue;

                    float angle = Mathf.Atan2(dy, dx);
                    // 六角形
                    float r6 = Mathf.Cos(angle * 3) * 8 + 28;
                    // 中心到外围的辐射线
                    bool onLine = false;
                    for (int a = 0; a < 6; a++)
                    {
                        float la = a * Mathf.PI / 3f;
                        float perpDist = Mathf.Abs(dx * Mathf.Sin(la) - dy * Mathf.Cos(la));
                        if (perpDist < 2.5f && dist < 32) onLine = true;
                    }
                    if (dist < r6 || onLine || dist < 6)
                    {
                        float alpha = Mathf.Clamp01(1f - dist / 36f);
                        Color c = mainColor;
                        c.a = alpha * 0.9f;
                        if (dist < 6) c = Color.Lerp(c, Color.white, 0.5f);
                        tex.SetPixel(x, y, c);
                    }
                }
        }
        else if (type == "lightning")
        {
            // 闪电：简单的Z字形
            int[] boltX = { 42, 35, 45, 30, 40, 38 };
            int[] boltY = { 70, 55, 50, 35, 30, 10 };
            for (int i = 0; i < boltX.Length - 1; i++)
            {
                DrawThickLine(tex, boltX[i], boltY[i], boltX[i + 1], boltY[i + 1], mainColor, 4);
            }
            // 发光效果
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    Color c = tex.GetPixel(x, y);
                    if (c.a > 0.1f)
                    {
                        // 周围添加辉光
                        for (int dy = -3; dy <= 3; dy++)
                            for (int dx = -3; dx <= 3; dx++)
                            {
                                int nx = x + dx, ny = y + dy;
                                if (nx < 0 || nx >= s || ny < 0 || ny >= s) continue;
                                Color nc = tex.GetPixel(nx, ny);
                                if (nc.a < 0.05f)
                                {
                                    float glow = 0.3f / (1 + dx * dx + dy * dy);
                                    tex.SetPixel(nx, ny, new Color(mainColor.r, mainColor.g, mainColor.b, glow));
                                }
                            }
                    }
                }
        }
        else if (type == "fire")
        {
            // 火焰
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = x - center.x, dy = y - center.y;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float flameH = 32 + Mathf.Sin(x * 0.3f) * 8;
                    float flame = Mathf.Clamp01(1f - (dist / flameH));
                    flame *= Mathf.Clamp01((y - 10f) / 50f); // 底部收窄
                    if (flame > 0.05f)
                    {
                        float heat = Mathf.Clamp01(flame * 1.5f);
                        Color c = Color.Lerp(new Color(1f, 0.1f, 0f), new Color(1f, 0.8f, 0.2f), heat);
                        c.a = flame * 0.85f;
                        tex.SetPixel(x, y, c);
                    }
                }
        }

        tex.Apply();
        File.WriteAllBytes($"{dir}/{name}.png", tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    static void DrawThickLine(Texture2D tex, int x0, int y0, int x1, int y1, Color color, int thickness)
    {
        int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        int s = tex.width;

        while (true)
        {
            for (int ty = -thickness / 2; ty <= thickness / 2; ty++)
                for (int tx = -thickness / 2; tx <= thickness / 2; tx++)
                {
                    int px = x0 + tx, py = y0 + ty;
                    if (px >= 0 && px < s && py >= 0 && py < s)
                        tex.SetPixel(px, py, color);
                }

            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
    }
}
