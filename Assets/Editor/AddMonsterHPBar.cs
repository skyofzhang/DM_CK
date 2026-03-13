using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class AddMonsterHPBar
{
    public static string Execute()
    {
        // 加载怪物 prefab
        var prefabPath = "Assets/Prefabs/Monsters/X_guai01.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return "Prefab not found: " + prefabPath;

        // 检查是否已有 HP bar
        if (prefab.transform.Find("HPBarCanvas") != null)
            return "HPBarCanvas already exists on X_guai01.prefab";

        // 进入 prefab 编辑模式
        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            var root = editingScope.prefabContentsRoot;

            // 创建 World Space Canvas
            var canvasGO = new GameObject("HPBarCanvas");
            canvasGO.transform.SetParent(root.transform);
            canvasGO.transform.localPosition = new Vector3(0, 2.2f, 0); // 头顶
            canvasGO.transform.localRotation = Quaternion.identity;
            canvasGO.transform.localScale    = new Vector3(0.01f, 0.01f, 0.01f);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = canvasGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100, 15);

            // 背景
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform);
            var bgRT = bgGO.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // HP Fill
            var fillGO = new GameObject("HPFill");
            fillGO.transform.SetParent(canvasGO.transform);
            var fillRT = fillGO.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(1, 1);
            fillRT.offsetMin = new Vector2(1, 1);
            fillRT.offsetMax = new Vector2(-1, -1);
            var fillImg = fillGO.AddComponent<Image>();
            fillImg.color      = new Color(0.9f, 0.2f, 0.2f, 1f); // 红色
            fillImg.type       = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 1f;
        }

        return "HP bar added to X_guai01.prefab";
    }
}
