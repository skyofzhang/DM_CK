using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public class ApplyResourceIcons
{
    [MenuItem("Tools/DrscfZ/Apply Resource Icons")]
    public static void Execute()
    {
        var iconConfigs = new (string path, string spritePath)[]
        {
            ("GameUIPanel/TopBar/ResourceRow/FoodIcon/Icon", "Assets/Art/UI/Icons/Survival/icon_food.png"),
            ("GameUIPanel/TopBar/ResourceRow/CoalIcon/Icon", "Assets/Art/UI/Icons/Survival/icon_coal.png"),
            ("GameUIPanel/TopBar/ResourceRow/OreIcon/Icon", "Assets/Art/UI/Icons/Survival/icon_ore.png"),
            ("GameUIPanel/TopBar/ResourceRow/HeatIcon/Icon", "Assets/Art/UI/Icons/Survival/icon_heat.png"),
            ("GameUIPanel/TopBar/ResourceRow/GateIcon/Icon", "Assets/Art/UI/Icons/Survival/icon_gate.png"),
        };

        // Find Canvas (it should be active)
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("ApplyResourceIcons: Canvas not found!");
            return;
        }
        Debug.Log("ApplyResourceIcons: Canvas found");

        int count = 0;
        foreach (var (path, spritePath) in iconConfigs)
        {
            // Use Transform.Find to find inactive children
            var t = canvas.transform.Find(path);
            if (t == null)
            {
                Debug.LogWarning("ApplyResourceIcons: not found: Canvas/" + path);
                continue;
            }

            var go = t.gameObject;
            Debug.Log("ApplyResourceIcons: found " + go.name + " at " + path);

            // Remove TMP component (conflicts with Image as both are Graphic)
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                // Also remove CanvasRenderer if it exists (TMP adds it)
                Object.DestroyImmediate(tmp);
                Debug.Log("ApplyResourceIcons: removed TMP from " + go.name);
            }

            // Add or get Image component
            var img = go.GetComponent<Image>();
            if (img == null)
            {
                img = go.AddComponent<Image>();
            }

            // Load sprite
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                Debug.LogWarning("ApplyResourceIcons: sprite not found: " + spritePath);
                continue;
            }

            img.sprite = sprite;
            img.color = Color.white;
            img.raycastTarget = false;
            img.preserveAspect = true;
            img.enabled = true;

            EditorUtility.SetDirty(go);
            count++;
            Debug.Log("ApplyResourceIcons: applied " + spritePath + " to " + path);
        }

        // Save scene
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("ApplyResourceIcons: done, updated " + count + " icons, scene saved");
    }
}
