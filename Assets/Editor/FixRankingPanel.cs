using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 三步合一：
///   1. 移除 SurvivalRankingPanel 上重复挂载的 SurvivalRankingUI 组件（3次才开的 bug 根因）
///   2. 把 SurvivalIdleUI._rankingPanel 重绑到 Canvas 上的正确 SurvivalRankingUI
///   3. 打印 SurvivalRankingPanel 的子层级，供后续 UI 升级参考
/// </summary>
public static class FixRankingPanel
{
    public static void Execute()
    {
        // ── 1. 找 Canvas 上的 SurvivalRankingUI（正确实例）────────────────
        GameObject canvas = null;
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go.name == "Canvas" && go.scene.name == "MainScene") { canvas = go; break; }

        if (canvas == null) { Debug.LogError("[FixRanking] Canvas 未找到"); return; }

        var correctRankingUI = canvas.GetComponent<DrscfZ.UI.SurvivalRankingUI>();
        if (correctRankingUI == null)
        { Debug.LogError("[FixRanking] Canvas 上没有 SurvivalRankingUI"); return; }

        Debug.Log($"[FixRanking] ✓ 正确 SurvivalRankingUI 在 Canvas，_panel={GetPanelName(correctRankingUI)}");

        // ── 2. 找 SurvivalRankingPanel 上的重复组件并移除 ────────────────
        GameObject rankingPanel = null;
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go.name == "SurvivalRankingPanel" && go.scene.name == "MainScene") { rankingPanel = go; break; }

        if (rankingPanel == null) { Debug.LogError("[FixRanking] SurvivalRankingPanel 未找到"); return; }

        var wrongComp = rankingPanel.GetComponent<DrscfZ.UI.SurvivalRankingUI>();
        if (wrongComp != null)
        {
            Object.DestroyImmediate(wrongComp);
            Debug.Log("[FixRanking] ✓ 已移除 SurvivalRankingPanel 上的重复 SurvivalRankingUI");
        }
        else
        {
            Debug.Log("[FixRanking] SurvivalRankingPanel 上无重复组件（已干净）");
        }

        // ── 3. 重绑 SurvivalIdleUI._rankingPanel → Canvas 的 SurvivalRankingUI ──
        var idleUI = canvas.GetComponent<DrscfZ.UI.SurvivalIdleUI>();
        if (idleUI != null)
        {
            var so = new SerializedObject(idleUI);
            so.FindProperty("_rankingPanel").objectReferenceValue = correctRankingUI;
            so.ApplyModifiedProperties();
            Debug.Log("[FixRanking] ✓ SurvivalIdleUI._rankingPanel 已重绑到 Canvas/SurvivalRankingUI");
        }

        // ── 4. 打印 SurvivalRankingPanel 子层级 ──────────────────────────
        Debug.Log("=== SurvivalRankingPanel 子层级 ===");
        PrintChildren(rankingPanel.transform, "  ");

        // ── 5. 保存场景 ────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(rankingPanel.scene);
        EditorSceneManager.SaveScene(rankingPanel.scene);
        Debug.Log("[FixRanking] 完成，场景已保存。");
    }

    static string GetPanelName(DrscfZ.UI.SurvivalRankingUI ui)
    {
        var so = new SerializedObject(ui);
        var p = so.FindProperty("_panel");
        return p?.objectReferenceValue != null ? p.objectReferenceValue.name : "null";
    }

    static void PrintChildren(Transform t, string indent)
    {
        foreach (Transform child in t)
        {
            var img  = child.GetComponent<Image>();
            var btn  = child.GetComponent<Button>();
            var tmp  = child.GetComponent<TextMeshProUGUI>();
            var rt   = child.GetComponent<RectTransform>();

            string sizeStr = rt != null ? $" size=({rt.sizeDelta.x:0},{rt.sizeDelta.y:0})" : "";
            string sprStr  = img != null ? $" [Img:{(img.sprite != null ? img.sprite.name : "null")}]" : "";
            string btnStr  = btn != null ? " [Btn]" : "";
            string tmpStr  = tmp != null ? $" [TMP:\"{(tmp.text.Length > 15 ? tmp.text.Substring(0,15)+"…" : tmp.text)}\"]" : "";

            Debug.Log($"{indent}[{(child.gameObject.activeSelf ? "ON" : "off")}] {child.name}{sizeStr}{sprStr}{btnStr}{tmpStr}");
            PrintChildren(child, indent + "  ");
        }
    }
}
