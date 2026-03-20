using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 修复 SurvivalSettingsPanel 初始激活状态。
/// SurvivalSettingsUI 脚本挂在 SurvivalSettingsPanel 上，
/// 若面板初始为 inactive，则 Awake 不会被调用，Instance 永远为 null。
/// 修复：将面板设为初始 active，由 Awake() 自行 SetActive(false) 隐藏。
/// </summary>
public class FixSettingsPanelActive
{
    [MenuItem("Tools/DrscfZ/Fix Settings Panel Active")]
    public static void Execute()
    {
        // 查找非激活对象需用 Resources.FindObjectsOfTypeAll
        var allGOs = Resources.FindObjectsOfTypeAll<GameObject>();
        GameObject panel = null;

        foreach (var go in allGOs)
        {
            // 排除 Prefab 资产（只处理场景内对象）
            if (go.scene.name != "MainScene") continue;
            if (go.name == "SurvivalSettingsPanel")
            {
                panel = go;
                break;
            }
        }

        if (panel == null)
        {
            Debug.LogError("[FixSettingsPanelActive] 未找到 SurvivalSettingsPanel！");
            return;
        }

        if (panel.activeSelf)
        {
            Debug.Log("[FixSettingsPanelActive] SurvivalSettingsPanel 已经是 active，无需修复。");
            return;
        }

        Undo.RecordObject(panel, "Fix SurvivalSettingsPanel Active");
        panel.SetActive(true);
        EditorUtility.SetDirty(panel);

        var scene = panel.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[FixSettingsPanelActive] SurvivalSettingsPanel 已设为 active，场景已保存。");
        Debug.Log("[FixSettingsPanelActive] Awake() 会在游戏启动时自动调用 _panel.SetActive(false) 隐藏面板。");
    }
}
