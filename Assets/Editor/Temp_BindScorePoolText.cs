#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using DrscfZ.UI;

/// <summary>
/// 将场景中已存在的 ScorePoolText 绑定到 SurvivalTopBarUI.scorePoolText
/// 并初始化文字样式（金色、粗体、居中）
/// 执行完毕后请删除本文件
/// </summary>
public static class Temp_BindScorePoolText
{
    [MenuItem("DrscfZ/Setup/Bind ScorePoolText")]
    public static void Run()
    {
        // FindObjectOfType works even when parent GameObjects are inactive
        var ui = Object.FindObjectOfType<SurvivalTopBarUI>(true);
        if (ui == null)
        {
            Debug.LogError("[BindScorePool] SurvivalTopBarUI not found in scene");
            return;
        }
        var topBarGo = ui.gameObject;

        // 查找已有 ScorePoolText 节点
        var textTr = topBarGo.transform.Find("ScorePoolText");
        if (textTr == null)
        {
            Debug.LogError("[BindScorePool] ScorePoolText child not found under TopBar");
            return;
        }

        var tmp = textTr.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
        {
            Debug.LogError("[BindScorePool] TextMeshProUGUI not found on ScorePoolText");
            return;
        }

        // ---- 样式设置 ----
        tmp.text      = "奖池:0";
        tmp.fontSize  = 28f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = false;

        EditorUtility.SetDirty(tmp.gameObject);

        // ---- 绑定字段 ----
        var so   = new SerializedObject(ui);
        var prop = so.FindProperty("scorePoolText");
        if (prop != null)
        {
            prop.objectReferenceValue = tmp;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(ui);
            Debug.Log("[BindScorePool] scorePoolText bound successfully");
        }
        else
        {
            Debug.LogError("[BindScorePool] scorePoolText property not found — compile first");
            return;
        }

        // ---- 保存场景 ----
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[BindScorePool] Scene saved.");
    }
}
#endif
