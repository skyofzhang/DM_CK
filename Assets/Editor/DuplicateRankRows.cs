using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;

/// <summary>
/// 以场景中现有的 RankRow_0 为模板，复制50行到 ScrollArea/RowContainer
/// </summary>
public class DuplicateRankRows
{
    const int MAX_ROWS = 50;

    [MenuItem("Tools/DrscfZ/Duplicate Rank Rows From Template")]
    public static void Execute()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("Canvas not found"); return; }

        Transform panel = null;
        foreach (Transform child in canvas.transform)
        {
            if (child.name == "SurvivalRankingPanel") { panel = child; break; }
        }
        if (panel == null) { Debug.LogError("SurvivalRankingPanel not found"); return; }

        // 找到模板行（直接在 panel 下的 RankRow_0，或者名字含 RankRow_Comp）
        GameObject template = null;
        foreach (Transform child in panel)
        {
            if (child.name.StartsWith("RankRow"))
            {
                template = child.gameObject;
                break;
            }
        }

        // 也搜索 ScrollArea/RowContainer 下的第一行
        if (template == null)
        {
            var scrollArea = panel.Find("ScrollArea");
            if (scrollArea != null)
            {
                var rc = scrollArea.Find("RowContainer");
                if (rc != null && rc.childCount > 0)
                    template = rc.GetChild(0).gameObject;
            }
        }

        if (template == null)
        {
            Debug.LogError("[DuplicateRankRows] 未找到模板行 RankRow");
            return;
        }

        Debug.Log($"[DuplicateRankRows] 使用模板: {template.name}");

        // 确保 ScrollArea/RowContainer 存在
        var scrollAreaT = panel.Find("ScrollArea");
        if (scrollAreaT == null) { Debug.LogError("ScrollArea not found"); return; }
        var rowContainer = scrollAreaT.Find("RowContainer");
        if (rowContainer == null) { Debug.LogError("RowContainer not found"); return; }

        // 清除 RowContainer 内所有现有行
        while (rowContainer.childCount > 0)
            Object.DestroyImmediate(rowContainer.GetChild(0).gameObject);

        // 复制50行
        for (int i = 0; i < MAX_ROWS; i++)
        {
            var row = Object.Instantiate(template, rowContainer);
            row.name = $"RankRow_{i}";

            // 更新名次文字
            var texts = row.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length >= 1)
                texts[0].text = $"#{i + 1}";
            if (texts.Length >= 2)
                texts[1].text = "";
            if (texts.Length >= 3)
                texts[2].text = "";

            // Top 3 用金色背景，其余用暗色
            var img = row.GetComponent<Image>();
            if (img != null)
            {
                img.color = i < 3
                    ? new Color(0.6f, 0.45f, 0.15f, 0.6f)
                    : new Color(0.1f, 0.15f, 0.25f, 0.4f);
            }

            // 默认隐藏（运行时由代码控制显隐）
            row.SetActive(false);
        }

        // 删除面板下的原始模板行（如果它不在 ScrollArea 内）
        if (template.transform.parent == panel)
            Object.DestroyImmediate(template);

        // 重新绑定 _rowContainer
        var rankingUI = canvas.GetComponent<DrscfZ.UI.SurvivalRankingUI>();
        if (rankingUI != null)
        {
            var so = new SerializedObject(rankingUI);
            var rowProp = so.FindProperty("_rowContainer");
            if (rowProp != null) rowProp.objectReferenceValue = rowContainer;
            so.ApplyModifiedProperties();
        }

        // 保存
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[DuplicateRankRows] 已从模板复制 {MAX_ROWS} 行到 ScrollArea/RowContainer，场景已保存");
    }
}
