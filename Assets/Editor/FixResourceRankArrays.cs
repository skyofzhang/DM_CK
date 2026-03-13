using UnityEngine;
using UnityEditor;
using TMPro;
using DrscfZ.UI;

/// <summary>
/// 验证并修复 ResourceRankUI 的 _foodRows/_coalRows/_oreRows 数组绑定。
/// 运行方式：Tools → DrscfZ → Fix Resource Rank Arrays
/// </summary>
public static class FixResourceRankArrays
{
    [MenuItem("Tools/DrscfZ/Fix Resource Rank Arrays")]
    public static void Execute()
    {
        var panelGO = GameObject.Find("Canvas/GameUIPanel/ResourceRankPanel");
        if (panelGO == null)
        {
            Debug.LogError("[FixResourceRankArrays] 找不到 ResourceRankPanel，请先运行 Create Resource Rank Panel");
            return;
        }

        var rankUI = panelGO.GetComponent<ResourceRankUI>();
        if (rankUI == null)
        {
            Debug.LogError("[FixResourceRankArrays] ResourceRankPanel 上没有 ResourceRankUI 组件");
            return;
        }

        var so = new SerializedObject(rankUI);

        // 按列名和行号查找 TMP
        string[] cols = { "Col_Food", "Col_Coal", "Col_Ore" };
        string[] propNames = { "_foodRows", "_coalRows", "_oreRows" };

        for (int c = 0; c < 3; c++)
        {
            var colTf = panelGO.transform.Find(cols[c]);
            if (colTf == null)
            {
                Debug.LogError($"[FixResourceRankArrays] 找不到子对象 {cols[c]}");
                continue;
            }

            var arr = so.FindProperty(propNames[c]);
            arr.arraySize = 3;

            for (int r = 0; r < 3; r++)
            {
                var rowTf = colTf.Find($"Row{r + 1}");
                if (rowTf == null)
                {
                    Debug.LogError($"[FixResourceRankArrays] 找不到 {cols[c]}/Row{r + 1}");
                    continue;
                }
                var tmp = rowTf.GetComponent<TextMeshProUGUI>();
                if (tmp == null)
                {
                    Debug.LogError($"[FixResourceRankArrays] {cols[c]}/Row{r + 1} 没有 TextMeshProUGUI 组件");
                    continue;
                }
                arr.GetArrayElementAtIndex(r).objectReferenceValue = tmp;
                Debug.Log($"[FixResourceRankArrays] 绑定 {propNames[c]}[{r}] → {cols[c]}/Row{r + 1}");
            }
        }

        so.ApplyModifiedProperties();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[FixResourceRankArrays] 所有行数组绑定完成！");
    }
}
