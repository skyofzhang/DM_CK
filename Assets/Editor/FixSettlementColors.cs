using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class FixSettlementColors : Editor
{
    public static void Execute()
    {
        var all = Resources.FindObjectsOfTypeAll<Transform>();
        Transform panel = null;
        foreach (var t in all)
        {
            if (t.name == "SurvivalSettlementPanel" && t.GetComponent<RectTransform>() != null
                && (t.gameObject.scene.isLoaded || t.gameObject.scene.name != null))
            {
                panel = t;
                break;
            }
        }
        if (panel == null) { Debug.LogError("找不到面板"); return; }

        // 调亮 BG
        var bgImg = panel.Find("BG")?.GetComponent<Image>();
        if (bgImg) bgImg.color = new Color(0.06f, 0.10f, 0.20f, 0.94f);

        // 调亮极光
        var aurora = panel.Find("BG/AuroraDecor")?.GetComponent<Image>();
        if (aurora) aurora.color = new Color(0.6f, 0.6f, 0.7f, 0.55f);

        // 减弱底部遮罩
        var fade = panel.Find("BG/BottomFade")?.GetComponent<Image>();
        if (fade) fade.color = new Color(0.05f, 0.08f, 0.16f, 0.8f);

        // 调亮数据面板背景
        var statsBg = panel.Find("ScreenB/StatsPanelBg")?.GetComponent<Image>();
        if (statsBg) statsBg.color = new Color(0.12f, 0.22f, 0.38f, 0.85f);

        // 调亮排名列表背景
        var rankBg = panel.Find("ScreenB/RankListBg")?.GetComponent<Image>();
        if (rankBg) rankBg.color = new Color(0.1f, 0.18f, 0.32f, 0.8f);

        // 确保按钮有合适颜色
        var btnView = panel.Find("BtnViewRanking")?.GetComponent<Image>();
        if (btnView) btnView.color = new Color(0.25f, 0.55f, 0.85f, 1f);

        var btnRestart = panel.Find("RestartButton")?.GetComponent<Image>();
        if (btnRestart) btnRestart.color = new Color(0.2f, 0.7f, 0.45f, 1f);

        // 删除旧的按钮 Text 子节点（只保留新的 Label）
        DeleteOldText(panel.Find("RestartButton"));
        DeleteOldText(panel.Find("BtnViewRanking"));

        EditorUtility.SetDirty(panel.gameObject);
        Debug.Log("[FixSettlementColors] 颜色已调亮");
    }

    static void DeleteOldText(Transform btn)
    {
        if (btn == null) return;
        var oldText = btn.Find("Text");
        if (oldText != null)
            DestroyImmediate(oldText.gameObject);
    }
}
