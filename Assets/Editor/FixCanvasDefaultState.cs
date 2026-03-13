using UnityEngine;
using UnityEditor;
using DrscfZ.UI;

/// <summary>
/// 修复 Canvas 子面板默认激活状态：
///   - GameUIPanel    → SetActive(false)  (Running 时由 SurvivalGameplayUI 激活)
///   - BottomBar      → SetActive(false)  (Running 时由 SurvivalGameplayUI 激活)
///   - AnnouncementPanel → SetActive(false)
/// 同时在 Canvas 上挂载 SurvivalGameplayUI 并绑定字段。
/// 菜单：Tools/Phase2/Fix Canvas Default State
/// </summary>
public class FixCanvasDefaultState
{
    [MenuItem("Tools/Phase2/Fix Canvas Default State")]
    public static void Execute()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[FixCanvasDefaultState] Canvas not found!");
            return;
        }

        // ── 1. 设置默认 inactive ──────────────────────────────────────────
        SetChildActive(canvas, "GameUIPanel",       false);
        SetChildActive(canvas, "BottomBar",         false);
        SetChildActive(canvas, "AnnouncementPanel", false);

        // ── 2. 清理旧 SurvivalGameplayUI（避免重复挂载）─────────────────
        var old = canvas.GetComponent<SurvivalGameplayUI>();
        if (old != null) Object.DestroyImmediate(old);

        // ── 3. 添加 SurvivalGameplayUI ───────────────────────────────────
        var gameplayUI = canvas.AddComponent<SurvivalGameplayUI>();

        // ── 4. 绑定字段 ──────────────────────────────────────────────────
        var so = new SerializedObject(gameplayUI);

        var gameUIPanel = canvas.transform.Find("GameUIPanel");
        if (gameUIPanel != null)
            so.FindProperty("_gameUIPanel").objectReferenceValue = gameUIPanel.gameObject;
        else
            Debug.LogWarning("[FixCanvasDefaultState] GameUIPanel not found!");

        var bottomBar = canvas.transform.Find("BottomBar");
        if (bottomBar != null)
            so.FindProperty("_bottomBar").objectReferenceValue = bottomBar.gameObject;
        else
            Debug.LogWarning("[FixCanvasDefaultState] BottomBar not found!");

        var annoPanel = canvas.transform.Find("AnnouncementPanel");
        if (annoPanel != null)
            so.FindProperty("_announcementPanel").objectReferenceValue = annoPanel.gameObject;
        else
            Debug.LogWarning("[FixCanvasDefaultState] AnnouncementPanel not found!");

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(gameplayUI);

        // ── 5. 保存场景 ──────────────────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[FixCanvasDefaultState] ✅ GameUIPanel/BottomBar/AnnouncementPanel 已设为默认 inactive，" +
                  "SurvivalGameplayUI 已挂载并绑定，场景已保存。");
    }

    private static void SetChildActive(GameObject parent, string childName, bool active)
    {
        var t = parent.transform.Find(childName);
        if (t == null)
        {
            Debug.LogWarning($"[FixCanvasDefaultState] Child not found: {childName}");
            return;
        }
        t.gameObject.SetActive(active);
        EditorUtility.SetDirty(t.gameObject);
        Debug.Log($"[FixCanvasDefaultState] {childName}.SetActive({active}) ✅");
    }
}
