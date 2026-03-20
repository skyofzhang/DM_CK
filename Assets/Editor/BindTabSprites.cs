using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class BindTabSprites
{
    [MenuItem("Tools/DrscfZ/Bind Tab Sprites")]
    public static void Execute()
    {
        var activeSprite   = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Rankings/tab_active.png");
        var inactiveSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Rankings/tab_inactive.png");

        if (activeSprite == null || inactiveSprite == null)
        {
            Debug.LogError("[BindTabSprites] 素材未找到");
            return;
        }

        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("Canvas not found"); return; }

        var rankingUI = canvas.GetComponent<DrscfZ.UI.SurvivalRankingUI>();
        if (rankingUI == null) { Debug.LogError("SurvivalRankingUI not found"); return; }

        var so = new SerializedObject(rankingUI);

        var activeProp = so.FindProperty("_tabActiveSpriteRef");
        if (activeProp != null) activeProp.objectReferenceValue = activeSprite;

        var inactiveProp = so.FindProperty("_tabInactiveSpriteRef");
        if (inactiveProp != null) inactiveProp.objectReferenceValue = inactiveSprite;

        // 绑定 HeaderRow
        Transform panel = null;
        foreach (Transform child in canvas.transform)
        {
            if (child.name == "SurvivalRankingPanel") { panel = child; break; }
        }
        if (panel != null)
        {
            var headerRow = panel.Find("HeaderRow");
            var headerProp = so.FindProperty("_headerRow");
            if (headerProp != null && headerRow != null)
                headerProp.objectReferenceValue = headerRow.gameObject;
        }

        // 绑定奖牌 Sprite
        var medalGold   = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Rankings/medal_gold.png");
        var medalSilver = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Rankings/medal_silver.png");
        var medalBronze = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Rankings/medal_bronze.png");

        var goldProp = so.FindProperty("_medalGold");
        if (goldProp != null) goldProp.objectReferenceValue = medalGold;
        var silverProp = so.FindProperty("_medalSilver");
        if (silverProp != null) silverProp.objectReferenceValue = medalSilver;
        var bronzeProp = so.FindProperty("_medalBronze");
        if (bronzeProp != null) bronzeProp.objectReferenceValue = medalBronze;

        so.ApplyModifiedProperties();

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[BindTabSprites] 页签素材已绑定到 Inspector 并保存");
    }
}
