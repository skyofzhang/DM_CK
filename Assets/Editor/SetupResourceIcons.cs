using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;

public class SetupResourceIcons
{
    [MenuItem("Tools/DrscfZ/Setup Resource Icons")]
    public static void Execute()
    {
        AssetDatabase.Refresh();

        // 生成简单的彩色圆形图标（煤炭、矿石、温度、城门）
        GenerateCircleIcon("Assets/Art/UI/Icons/Survival/icon_coal.png",  new Color(0.25f, 0.22f, 0.2f), new Color(0.4f, 0.35f, 0.3f));
        GenerateCircleIcon("Assets/Art/UI/Icons/Survival/icon_ore.png",   new Color(0.3f, 0.5f, 0.8f), new Color(0.5f, 0.7f, 1f));
        GenerateCircleIcon("Assets/Art/UI/Icons/Survival/icon_heat.png",  new Color(0.9f, 0.35f, 0.1f), new Color(1f, 0.6f, 0.2f));
        GenerateCircleIcon("Assets/Art/UI/Icons/Survival/icon_gate.png",  new Color(0.4f, 0.55f, 0.7f), new Color(0.6f, 0.75f, 0.9f));

        // 设置所有图标为 Sprite
        string[] iconPaths = {
            "Assets/Art/UI/Icons/Survival/icon_food.png",
            "Assets/Art/UI/Icons/Survival/icon_coal.png",
            "Assets/Art/UI/Icons/Survival/icon_ore.png",
            "Assets/Art/UI/Icons/Survival/icon_heat.png",
            "Assets/Art/UI/Icons/Survival/icon_gate.png",
        };
        foreach (var p in iconPaths)
            SetSpriteImport(p);

        // ── 应用到 ResourceRow ──
        var resourceRow = GameObject.Find("Canvas/GameUIPanel/TopBar/ResourceRow");
        if (resourceRow == null) { Debug.LogError("ResourceRow not found"); return; }

        string[] slotNames = { "FoodIcon", "CoalIcon", "OreIcon", "HeatIcon", "GateIcon" };

        for (int i = 0; i < slotNames.Length; i++)
        {
            var slot = resourceRow.transform.Find(slotNames[i]);
            if (slot == null) continue;

            var iconT = slot.Find("Icon");
            if (iconT == null) continue;

            // 移除旧 TMP_Text 上的文字
            var tmp = iconT.GetComponent<TMP_Text>();
            if (tmp != null)
                tmp.text = ""; // 清空方块文字

            // 添加或更新 Image 组件显示图标
            var img = iconT.GetComponent<Image>();
            if (img == null)
                img = iconT.gameObject.AddComponent<Image>();

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPaths[i]);
            if (sprite != null)
            {
                img.sprite = sprite;
                img.color = Color.white;
                img.preserveAspect = true;
                img.raycastTarget = false;
            }

            // 确保 RectTransform 大小合适
            var rt = iconT.GetComponent<RectTransform>();
            if (rt != null)
                rt.sizeDelta = new Vector2(28, 28);
        }

        // ── 修复 TopBar Image Type 为 Sliced ──
        var topBar = GameObject.Find("Canvas/GameUIPanel/TopBar");
        if (topBar != null)
        {
            var topImg = topBar.GetComponent<Image>();
            if (topImg != null && topImg.sprite != null)
            {
                topImg.type = Image.Type.Sliced;
                topImg.color = new Color(1f, 1f, 1f, 0.92f);
            }
        }

        // ── 保存 ──
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SetupResourceIcons] 资源图标已设置完成");
    }

    static void GenerateCircleIcon(string path, Color inner, Color outer)
    {
        int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        float radius = size / 2f - 1;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= radius)
                {
                    float t = dist / radius;
                    // 渐变：内到外
                    Color c = Color.Lerp(inner, outer, t * 0.6f);
                    // 顶部高光
                    if (y > center + radius * 0.3f)
                        c = Color.Lerp(c, Color.white, 0.15f);
                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
                else if (dist <= radius + 1)
                {
                    // 边缘抗锯齿
                    float alpha = 1f - (dist - radius);
                    Color c = outer;
                    c.a = Mathf.Clamp01(alpha);
                    tex.SetPixel(x, y, c);
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }

        tex.Apply();
        var bytes = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);

        // 确保目录存在
        string dir = System.IO.Path.GetDirectoryName(path);
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);

        System.IO.File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);
        Debug.Log($"[SetupResourceIcons] 生成图标: {path}");
    }

    static void SetSpriteImport(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 64;
            importer.SaveAndReimport();
        }
    }
}
