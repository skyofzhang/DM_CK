using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;

public class FixResourceIcons2
{
    [MenuItem("Tools/DrscfZ/Fix Resource Icons v2")]
    public static void Execute()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("Canvas not found"); return; }

        var resourceRowT = canvas.transform.Find("GameUIPanel/TopBar/ResourceRow");
        if (resourceRowT == null) { Debug.LogError("ResourceRow not found"); return; }

        string[] slotNames = { "FoodIcon", "CoalIcon", "OreIcon", "HeatIcon", "GateIcon" };
        string[] iconPaths = {
            "Assets/Art/UI/Icons/Survival/icon_food.png",
            "Assets/Art/UI/Icons/Survival/icon_coal.png",
            "Assets/Art/UI/Icons/Survival/icon_ore.png",
            "Assets/Art/UI/Icons/Survival/icon_heat.png",
            "Assets/Art/UI/Icons/Survival/icon_gate.png",
        };

        for (int i = 0; i < slotNames.Length; i++)
        {
            try
            {
                var slot = resourceRowT.Find(slotNames[i]);
                if (slot == null) { Debug.LogWarning($"Slot {slotNames[i]} not found"); continue; }

                var iconT = slot.Find("Icon");
                if (iconT == null) { Debug.LogWarning($"Icon in {slotNames[i]} not found"); continue; }

                Debug.Log($"[FixIcons] Processing {slotNames[i]}/Icon, components: {iconT.gameObject.GetComponents<Component>().Length}");

                // 禁用 TMP_Text
                var tmp = iconT.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.enabled = false;
                    Debug.Log($"[FixIcons] 禁用 TMP_Text on {slotNames[i]}/Icon");
                }

                // 加载 sprite
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPaths[i]);
                if (sprite == null)
                {
                    Debug.LogWarning($"[FixIcons] Sprite not found: {iconPaths[i]}");
                    continue;
                }

                // 确保有 Image
                var img = iconT.GetComponent<Image>();
                if (img == null)
                {
                    // TMP_Text 有 CanvasRenderer，Image 也需要一个，应该可以共存
                    img = Undo.AddComponent<Image>(iconT.gameObject);
                }

                if (img != null)
                {
                    img.sprite = sprite;
                    img.color = Color.white;
                    img.preserveAspect = true;
                    img.raycastTarget = false;
                    img.enabled = true;
                    Debug.Log($"[FixIcons] {slotNames[i]}/Icon sprite set OK");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FixIcons] Error on {slotNames[i]}: {e.Message}");
            }
        }

        // ── TopBar Image Type → Sliced ──
        try
        {
            var topBarT = canvas.transform.Find("GameUIPanel/TopBar");
            if (topBarT != null)
            {
                var topBarImg = topBarT.GetComponent<Image>();
                if (topBarImg != null)
                {
                    var so = new SerializedObject(topBarImg);
                    var typeProp = so.FindProperty("m_Type");
                    if (typeProp != null) typeProp.intValue = 1;
                    var colorProp = so.FindProperty("m_Color");
                    if (colorProp != null) colorProp.colorValue = new Color(1f, 1f, 1f, 0.92f);
                    so.ApplyModifiedProperties();
                    Debug.Log("[FixIcons] TopBar → Sliced + white");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FixIcons] TopBar error: {e.Message}");
        }

        // ── 保存 ──
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[FixResourceIcons2] 完成");
    }
}
