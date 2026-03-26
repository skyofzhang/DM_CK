using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;

public class FixResourceIcons3
{
    [MenuItem("Tools/DrscfZ/Fix Resource Icons v3")]
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
            var slot = resourceRowT.Find(slotNames[i]);
            if (slot == null) { Debug.LogWarning($"Slot {slotNames[i]} not found"); continue; }

            // 确保 Icon(TMP) 禁用
            var iconTmp = slot.Find("Icon");
            if (iconTmp != null)
            {
                var tmp = iconTmp.GetComponent<TMP_Text>();
                if (tmp != null) tmp.enabled = false;
            }

            // 删除旧的 IconImg（如果有）
            var oldImg = slot.Find("IconImg");
            if (oldImg != null) Object.DestroyImmediate(oldImg.gameObject);

            // 创建新的 IconImg 子对象
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPaths[i]);
            if (sprite == null) { Debug.LogWarning($"Sprite not found: {iconPaths[i]}"); continue; }

            var imgGo = new GameObject("IconImg");
            imgGo.transform.SetParent(slot, false);
            imgGo.transform.SetAsFirstSibling(); // 图标在最前

            var rt = imgGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(4, 0);
            rt.sizeDelta = new Vector2(28, 28);

            var img = imgGo.AddComponent<Image>();
            img.sprite = sprite;
            img.color = Color.white;
            img.preserveAspect = true;
            img.raycastTarget = false;

            // 添加 LayoutElement 防止被 HLG 拉伸
            var le = imgGo.AddComponent<LayoutElement>();
            le.preferredWidth = 28;
            le.preferredHeight = 28;
            le.flexibleWidth = 0;

            Debug.Log($"[FixIcons3] {slotNames[i]}/IconImg 创建完成");
        }

        // ── 保存 ──
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[FixResourceIcons3] 完成，5个资源图标已创建");
    }
}
