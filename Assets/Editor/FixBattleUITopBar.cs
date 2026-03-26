using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

public class FixBattleUITopBar
{
    [MenuItem("Tools/DrscfZ/Fix Battle TopBar Sprite")]
    public static void Execute()
    {
        var topBar = GameObject.Find("Canvas/GameUIPanel/TopBar");
        if (topBar == null) { Debug.LogError("TopBar not found"); return; }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/BattleUI/topbar_bg.png");
        if (sprite == null) { Debug.LogError("topbar_bg sprite not found"); return; }

        // TopBar 自身的 Image 改用新素材
        var img = topBar.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = new Color(1f, 1f, 1f, 0.92f);
            Debug.Log("[FixBattleUI] TopBar Image 已替换为 topbar_bg sprite");
        }

        // 保存
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[FixBattleUI] 场景已保存");
    }
}
