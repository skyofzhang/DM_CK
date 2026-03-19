using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Reflection;
using DrscfZ.UI;

/// <summary>
/// Tools → DrscfZ → Fix Live Ranking UI
/// 1. 绑定 SurvivalLiveRankingUI 的 Inspector 字段
/// 2. 将行背景色改为半透明深色（消除彩色方块）
/// 3. 行颜色按名次区分：金/银/铜 + 普通深色
/// </summary>
public static class FixLiveRankingUI
{
    [MenuItem("Tools/DrscfZ/Fix Live Ranking UI")]
    public static void Execute()
    {
        // ── 找 LiveRankingPanel（可能非激活，用 FindObjectsOfTypeAll）──────
        GameObject panelGo = null;
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t.name == "LiveRankingPanel" && t.hideFlags == HideFlags.None)
            { panelGo = t.gameObject; break; }
        }
        if (panelGo == null) { Debug.LogError("[FixLiveRankingUI] 找不到 LiveRankingPanel"); return; }

        // ── 脚本必须挂在常驻激活的父节点（GameUIPanel），而不是面板本身 ──
        // 原因：若脚本挂在 LiveRankingPanel，Awake SetActive(false) 会把自己关掉，订阅断开
        var gameUIPanel = panelGo.transform.parent?.gameObject; // GameUIPanel
        if (gameUIPanel == null) { Debug.LogError("[FixLiveRankingUI] 找不到 LiveRankingPanel 的父节点"); return; }

        // 清理挂错位置的旧组件
        var wrongComp = panelGo.GetComponent<DrscfZ.UI.SurvivalLiveRankingUI>();
        if (wrongComp != null)
        {
            Object.DestroyImmediate(wrongComp);
            Debug.Log("[FixLiveRankingUI] 已移除 LiveRankingPanel 上的旧组件");
        }

        var ui = gameUIPanel.GetComponent<DrscfZ.UI.SurvivalLiveRankingUI>();
        if (ui == null)
        {
            ui = gameUIPanel.AddComponent<DrscfZ.UI.SurvivalLiveRankingUI>();
            Debug.Log($"[FixLiveRankingUI] SurvivalLiveRankingUI 已挂到常驻父节点: {gameUIPanel.name}");
        }

        var so = new SerializedObject(ui);

        // ── 1. _panel ────────────────────────────────────────────────────
        so.FindProperty("_panel").objectReferenceValue = panelGo;

        // ── 2. _titleText ──────────────────────────────────────────────
        var titleGo = panelGo.transform.Find("Title");
        if (titleGo != null)
            so.FindProperty("_titleText").objectReferenceValue = titleGo.GetComponent<TextMeshProUGUI>();

        // ── 3. _rankRows → RankRow_0 ~ RankRow_4 ─────────────────────
        var rowContainer = panelGo.transform.Find("RowContainer");
        if (rowContainer == null) { Debug.LogError("[FixLiveRankingUI] 找不到 RowContainer"); return; }

        var rowsProp = so.FindProperty("_rankRows");
        rowsProp.arraySize = 5;

        // 每行背景颜色：金/银/铜/普通/普通（Alpha=0.45）
        Color[] rowColors = {
            new Color(1.00f, 0.85f, 0.20f, 0.45f),   // #1 金色
            new Color(0.80f, 0.80f, 0.82f, 0.45f),   // #2 银色
            new Color(0.80f, 0.55f, 0.25f, 0.45f),   // #3 铜色
            new Color(0.15f, 0.20f, 0.30f, 0.55f),   // #4 深蓝灰
            new Color(0.15f, 0.20f, 0.30f, 0.55f),   // #5 深蓝灰
        };

        for (int i = 0; i < 5; i++)
        {
            var rowGo = rowContainer.Find($"RankRow_{i}")?.gameObject;
            if (rowGo == null) { Debug.LogWarning($"[FixLiveRankingUI] 找不到 RankRow_{i}"); continue; }

            rowsProp.GetArrayElementAtIndex(i).objectReferenceValue = rowGo;

            // 修改 Image 背景色
            var img = rowGo.GetComponent<Image>();
            if (img != null) img.color = rowColors[i];

            // 名次文字颜色
            var rankText = rowGo.transform.Find("RankText")?.GetComponent<TextMeshProUGUI>();
            if (rankText != null)
            {
                rankText.color = i == 0 ? new Color(1f, 0.88f, 0.1f) :
                                 i == 1 ? new Color(0.9f, 0.9f, 0.9f) :
                                 i == 2 ? new Color(0.9f, 0.62f, 0.2f) :
                                          new Color(0.8f, 0.8f, 0.8f);
            }

            EditorUtility.SetDirty(rowGo);
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(ui);

        // ── 4. 保存场景 ────────────────────────────────────────────────
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

        Debug.Log("[FixLiveRankingUI] 完成：_panel/_titleText/_rankRows 已绑定，行背景色已修正，场景已保存。");
    }
}
