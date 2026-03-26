using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEditor.Events;
using DrscfZ.UI;

/// <summary>
/// 将 BtnSettings.onClick 绑定到 SurvivalSettingsUI.TogglePanel()
/// Tools → DrscfZ → Bind BtnSettings
/// </summary>
public static class BindBtnSettings
{
    [MenuItem("Tools/DrscfZ/Bind BtnSettings")]
    public static void Execute()
    {
        // 找 BtnSettings
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("[BindBtnSettings] 找不到 Canvas"); return; }

        var btnGO = canvas.transform.Find("GameUIPanel/BtnSettings");
        if (btnGO == null) { Debug.LogError("[BindBtnSettings] 找不到 GameUIPanel/BtnSettings"); return; }

        var btn = btnGO.GetComponent<Button>();
        if (btn == null) { Debug.LogError("[BindBtnSettings] BtnSettings 没有 Button 组件"); return; }

        // 找 SurvivalSettingsUI
        var settingsUI = Object.FindObjectOfType<SurvivalSettingsUI>();
        if (settingsUI == null) { Debug.LogError("[BindBtnSettings] 场景中找不到 SurvivalSettingsUI"); return; }

        // 清除旧的持久化监听，避免重复绑定
        btn.onClick.RemoveAllListeners();

        // 绑定持久化事件（Inspector 可见）
        var method = typeof(SurvivalSettingsUI).GetMethod("TogglePanel");
        if (method == null) { Debug.LogError("[BindBtnSettings] SurvivalSettingsUI 没有 TogglePanel 方法"); return; }

        UnityEventTools.AddVoidPersistentListener(btn.onClick, settingsUI.TogglePanel);

        EditorUtility.SetDirty(btn);
        EditorSceneManager.MarkSceneDirty(btnGO.gameObject.scene);
        EditorSceneManager.SaveScene(btnGO.gameObject.scene);

        Debug.Log("[BindBtnSettings] 完成：BtnSettings.onClick → SurvivalSettingsUI.TogglePanel()，场景已保存");
    }
}
