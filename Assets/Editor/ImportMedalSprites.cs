using UnityEngine;
using UnityEditor;

public class ImportMedalSprites
{
    [MenuItem("Tools/DrscfZ/Import Medal Sprites")]
    public static void Execute()
    {
        string[] paths = {
            "Assets/Art/UI/Rankings/medal_gold.png",
            "Assets/Art/UI/Rankings/medal_silver.png",
            "Assets/Art/UI/Rankings/medal_bronze.png",
        };
        foreach (var p in paths)
        {
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
            var imp = AssetImporter.GetAtPath(p) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.mipmapEnabled = false;
                imp.filterMode = FilterMode.Bilinear;
                imp.maxTextureSize = 128;
                imp.SaveAndReimport();
            }
            Debug.Log($"[ImportMedals] {p} → Sprite");
        }
    }
}
