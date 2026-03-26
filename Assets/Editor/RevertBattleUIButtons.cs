using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class RevertBattleUIButtons
{
    [MenuItem("Tools/DrscfZ/Revert Battle UI Buttons")]
    public static void Execute()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("Canvas not found"); return; }
        var gameUI = canvas.transform.Find("GameUIPanel");
        if (gameUI == null) { Debug.LogError("GameUIPanel not found"); return; }

        // ExitBtn: 还原为 btn_exit.png + 红色调
        var exitBtn = gameUI.Find("ExitBtn");
        if (exitBtn != null)
        {
            var img = exitBtn.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/BattleUI/btn_exit.png");
                img.color = new Color(1f, 0.4f, 0.35f, 1f);
                img.type = Image.Type.Sliced;
                EditorUtility.SetDirty(img);
            }
            // 删除添加的图标子节点
            var icon = exitBtn.Find("ExitIcon");
            if (icon != null) Object.DestroyImmediate(icon.gameObject);
            Debug.Log("ExitBtn reverted");
        }

        // BtnSettings: 还原为 btn_small_blue.png + 白色
        var btnSettings = gameUI.Find("BtnSettings");
        if (btnSettings != null)
        {
            var img = btnSettings.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/BattleUI/btn_small_blue.png");
                img.color = Color.white;
                img.type = Image.Type.Sliced;
                img.preserveAspect = true;
                EditorUtility.SetDirty(img);
            }
            var icon = btnSettings.Find("SettingsIcon");
            if (icon != null) Object.DestroyImmediate(icon.gameObject);
            Debug.Log("BtnSettings reverted");
        }

        // ResourceRankPanel: 还原为半透明深色纯色
        var rankPanel = gameUI.Find("ResourceRankPanel");
        if (rankPanel != null)
        {
            var img = rankPanel.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = null;
                img.color = new Color(0.05f, 0.08f, 0.15f, 0.75f);
                EditorUtility.SetDirty(img);
            }
            Debug.Log("ResourceRankPanel reverted");
        }

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("RevertBattleUIButtons: done, scene saved");
    }
}
