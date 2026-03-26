using UnityEngine;
using UnityEditor;

public class FixSurvivalIconImports
{
    [MenuItem("Tools/DrscfZ/Fix Survival Icon Imports")]
    public static void Execute()
    {
        string[] paths = new string[]
        {
            "Assets/Art/UI/Icons/Survival/icon_food.png",
            "Assets/Art/UI/Icons/Survival/icon_coal.png",
            "Assets/Art/UI/Icons/Survival/icon_ore.png",
            "Assets/Art/UI/Icons/Survival/icon_heat.png",
            "Assets/Art/UI/Icons/Survival/icon_gate.png",
        };

        foreach (var path in paths)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("FixSurvivalIconImports: not found: " + path);
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 128;
            importer.SaveAndReimport();
            Debug.Log("FixSurvivalIconImports: set Sprite type for " + path);
        }

        Debug.Log("FixSurvivalIconImports: done");
    }
}
