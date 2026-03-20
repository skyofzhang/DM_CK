using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

public class ApplyTabSprites
{
    [MenuItem("Tools/DrscfZ/Apply Tab Sprites")]
    public static void Execute()
    {
        var activeSprite   = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Rankings/tab_active.png");
        var inactiveSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Rankings/tab_inactive.png");

        if (activeSprite == null || inactiveSprite == null)
        {
            Debug.LogError("[ApplyTabSprites] tab_active.png 或 tab_inactive.png 未找到");
            return;
        }

        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("Canvas not found"); return; }

        // 查找面板（可能 inactive）
        Transform panel = null;
        foreach (Transform child in canvas.transform)
        {
            if (child.name == "SurvivalRankingPanel")
            {
                panel = child;
                break;
            }
        }
        if (panel == null) { Debug.LogError("SurvivalRankingPanel not found"); return; }

        // 应用到 TabContribution
        var tab1 = panel.Find("TabContribution");
        if (tab1 != null)
        {
            var img1 = tab1.GetComponent<Image>();
            if (img1 != null)
            {
                img1.sprite = activeSprite;  // 默认选中贡献榜
                img1.type   = Image.Type.Sliced;
                img1.color  = Color.white;   // 用 sprite 原色，不叠加 tint
            }
        }

        // 应用到 TabStreamer
        var tab2 = panel.Find("TabStreamer");
        if (tab2 != null)
        {
            var img2 = tab2.GetComponent<Image>();
            if (img2 != null)
            {
                img2.sprite = inactiveSprite;  // 默认未选中
                img2.type   = Image.Type.Sliced;
                img2.color  = Color.white;
            }
        }

        // 保存
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ApplyTabSprites] 页签素材已应用并保存");
    }
}
