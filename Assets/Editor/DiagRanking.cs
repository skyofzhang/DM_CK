using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// 诊断排行榜按钮绑定情况：
///   - SurvivalIdleUI._rankingBtn 是否绑定
///   - SurvivalIdleUI._rankingPanel 是否绑定
///   - SurvivalRankingUI._panel 是否绑定
///   - 场景中有哪些排行榜相关面板
/// </summary>
public static class DiagRanking
{
    public static void Execute()
    {
        Debug.Log("========= DiagRanking =========");

        // 1. 找 SurvivalIdleUI
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            var idleUI = go.GetComponent<DrscfZ.UI.SurvivalIdleUI>();
            if (idleUI == null) continue;

            Debug.Log($"[SurvivalIdleUI] found on: {go.name}");
            var so = new SerializedObject(idleUI);

            var rankingBtnProp   = so.FindProperty("_rankingBtn");
            var rankingPanelProp = so.FindProperty("_rankingPanel");
            var settingsPanelProp = so.FindProperty("_settingsPanel");

            Debug.Log($"  _rankingBtn   = {(rankingBtnProp?.objectReferenceValue != null ? rankingBtnProp.objectReferenceValue.name : "NULL")}");
            Debug.Log($"  _rankingPanel = {(rankingPanelProp?.objectReferenceValue != null ? rankingPanelProp.objectReferenceValue.name : "NULL")}");
            Debug.Log($"  _settingsPanel= {(settingsPanelProp?.objectReferenceValue != null ? settingsPanelProp.objectReferenceValue.name : "NULL")}");

            // Check RankingBtn onClick count
            if (rankingBtnProp?.objectReferenceValue != null)
            {
                var btn = rankingBtnProp.objectReferenceValue as Button;
                if (btn != null)
                {
                    var btnSo = new SerializedObject(btn);
                    var calls = btnSo.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
                    Debug.Log($"  RankingBtn persistent listeners: {calls?.arraySize ?? -1}");
                }
            }
        }

        // 2. 找 SurvivalRankingUI
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            var rankUI = go.GetComponent<DrscfZ.UI.SurvivalRankingUI>();
            if (rankUI == null) continue;

            Debug.Log($"[SurvivalRankingUI] found on: {go.name}");
            var so = new SerializedObject(rankUI);
            var panelProp   = so.FindProperty("_panel");
            var closeBtnProp = so.FindProperty("_closeBtn");
            Debug.Log($"  _panel   = {(panelProp?.objectReferenceValue != null ? panelProp.objectReferenceValue.name : "NULL")}");
            Debug.Log($"  _closeBtn= {(closeBtnProp?.objectReferenceValue != null ? closeBtnProp.objectReferenceValue.name : "NULL")}");

            if (panelProp?.objectReferenceValue != null)
            {
                var panel = panelProp.objectReferenceValue as GameObject;
                Debug.Log($"  _panel.activeSelf = {panel?.activeSelf}");
            }
        }

        // 3. 列出场景所有名字含 "Ranking" 或 "ranking" 的 GO
        Debug.Log("--- All Ranking-related GameObjects ---");
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.name.ToLower().Contains("ranking") && go.scene.name == "MainScene")
            {
                string path = go.name;
                var t = go.transform.parent;
                while (t != null) { path = t.name + "/" + path; t = t.parent; }
                Debug.Log($"  [{(go.activeSelf ? "ON" : "off")}] {path}");
            }
        }
    }
}
