using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// 修复 ResourceRankUI 挂载位置：从 ResourceRankPanel 移到 GameUIPanel。
/// 原因：脚本在 Awake 中 SetActive(false) 自己的 GO，导致永远无法重新激活。
/// 正确做法：脚本挂在 always-active 的 GameUIPanel 上，_panel 指向 ResourceRankPanel。
/// </summary>
public static class FixResourceRankUIMount
{
    [MenuItem("Tools/DrscfZ/Fix ResourceRankUI Mount")]
    public static void Execute()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("Canvas not found"); return; }

        var gameUIPanel = canvas.transform.Find("GameUIPanel");
        if (gameUIPanel == null) { Debug.LogError("GameUIPanel not found"); return; }

        var rankPanelT = gameUIPanel.Find("ResourceRankPanel");
        if (rankPanelT == null) { Debug.LogError("ResourceRankPanel not found"); return; }

        var rankPanelGO = rankPanelT.gameObject;

        // 读取旧组件的 SerializedObject 来获取所有引用
        var oldComp = rankPanelGO.GetComponent<DrscfZ.UI.ResourceRankUI>();
        if (oldComp == null)
        {
            Debug.Log("ResourceRankUI not on ResourceRankPanel (already moved?), checking GameUIPanel...");
            var existing = gameUIPanel.GetComponent<DrscfZ.UI.ResourceRankUI>();
            if (existing != null)
                Debug.Log("ResourceRankUI already on GameUIPanel - no action needed");
            else
                Debug.LogError("ResourceRankUI not found anywhere!");
            return;
        }

        // 用 SerializedObject 读取旧组件的所有字段引用
        var oldSO = new SerializedObject(oldComp);
        var panelRef      = oldSO.FindProperty("_panel").objectReferenceValue;
        var foodTitleRef   = oldSO.FindProperty("_foodTitle").objectReferenceValue;
        var coalTitleRef   = oldSO.FindProperty("_coalTitle").objectReferenceValue;
        var oreTitleRef    = oldSO.FindProperty("_oreTitle").objectReferenceValue;
        var foodRowsProp   = oldSO.FindProperty("_foodRows");
        var coalRowsProp   = oldSO.FindProperty("_coalRows");
        var oreRowsProp    = oldSO.FindProperty("_oreRows");
        var refreshInterval = oldSO.FindProperty("_refreshInterval").floatValue;

        // 保存数组引用
        var foodRefs = new Object[foodRowsProp.arraySize];
        var coalRefs = new Object[coalRowsProp.arraySize];
        var oreRefs  = new Object[oreRowsProp.arraySize];
        for (int i = 0; i < foodRowsProp.arraySize; i++)
            foodRefs[i] = foodRowsProp.GetArrayElementAtIndex(i).objectReferenceValue;
        for (int i = 0; i < coalRowsProp.arraySize; i++)
            coalRefs[i] = coalRowsProp.GetArrayElementAtIndex(i).objectReferenceValue;
        for (int i = 0; i < oreRowsProp.arraySize; i++)
            oreRefs[i] = oreRowsProp.GetArrayElementAtIndex(i).objectReferenceValue;

        // 删除旧组件
        Object.DestroyImmediate(oldComp);
        Debug.Log("Removed ResourceRankUI from ResourceRankPanel");

        // 在 GameUIPanel 上添加新组件
        var newComp = gameUIPanel.gameObject.AddComponent<DrscfZ.UI.ResourceRankUI>();
        var newSO = new SerializedObject(newComp);

        // 恢复所有引用
        newSO.FindProperty("_panel").objectReferenceValue = panelRef ?? rankPanelGO;
        newSO.FindProperty("_foodTitle").objectReferenceValue = foodTitleRef;
        newSO.FindProperty("_coalTitle").objectReferenceValue = coalTitleRef;
        newSO.FindProperty("_oreTitle").objectReferenceValue  = oreTitleRef;
        newSO.FindProperty("_refreshInterval").floatValue = refreshInterval;

        var newFoodRows = newSO.FindProperty("_foodRows");
        var newCoalRows = newSO.FindProperty("_coalRows");
        var newOreRows  = newSO.FindProperty("_oreRows");

        newFoodRows.arraySize = foodRefs.Length;
        newCoalRows.arraySize = coalRefs.Length;
        newOreRows.arraySize  = oreRefs.Length;

        for (int i = 0; i < foodRefs.Length; i++)
            newFoodRows.GetArrayElementAtIndex(i).objectReferenceValue = foodRefs[i];
        for (int i = 0; i < coalRefs.Length; i++)
            newCoalRows.GetArrayElementAtIndex(i).objectReferenceValue = coalRefs[i];
        for (int i = 0; i < oreRefs.Length; i++)
            newOreRows.GetArrayElementAtIndex(i).objectReferenceValue = oreRefs[i];

        newSO.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(gameUIPanel.gameObject);
        EditorUtility.SetDirty(rankPanelGO);

        // 保存场景
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene);

        Debug.Log("FixResourceRankUIMount: ResourceRankUI moved to GameUIPanel, _panel -> ResourceRankPanel, scene saved");
    }
}
