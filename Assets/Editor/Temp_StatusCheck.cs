using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using DrscfZ.UI;

/// <summary>
/// 快速状态检查：验证所有关键 Inspector 引用是否已绑定
/// </summary>
public class Temp_StatusCheck
{
    public static void Execute()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== Inspector 绑定状态检查 ===\n");

        Transform FindT(string path)
        {
            var parts = path.Split('/');
            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t.name != parts[parts.Length - 1]) continue;
                bool match = true;
                Transform cur = t;
                for (int i = parts.Length - 1; i >= 0; i--)
                {
                    if (cur == null || cur.name != parts[i]) { match = false; break; }
                    cur = cur.parent;
                }
                if (match) return t;
            }
            return null;
        }

        string OK  = "✅";
        string ERR = "❌";
        string NA  = "⚠";

        // ── 1. SurvivalRankingPanel ───────────────────────────────────
        sb.AppendLine("【SurvivalRankingPanel】");
        var rankT = FindT("Canvas/SurvivalRankingPanel");
        if (rankT == null) { sb.AppendLine($"  {ERR} GameObject 未找到"); }
        else
        {
            var rankUI = rankT.GetComponent<SurvivalRankingUI>();
            sb.AppendLine(rankUI != null ? $"  {OK} SurvivalRankingUI 组件已挂载" : $"  {ERR} SurvivalRankingUI 组件缺失");
            if (rankUI != null)
            {
                var so = new SerializedObject(rankUI);
                sb.AppendLine(so.FindProperty("_panel").objectReferenceValue  != null ? $"  {OK} _panel"        : $"  {ERR} _panel 未绑定");
                sb.AppendLine(so.FindProperty("_closeBtn").objectReferenceValue != null ? $"  {OK} _closeBtn"   : $"  {ERR} _closeBtn 未绑定");
                sb.AppendLine(so.FindProperty("_titleText").objectReferenceValue != null ? $"  {OK} _titleText"  : $"  {ERR} _titleText 未绑定");
                sb.AppendLine(so.FindProperty("_rowContainer").objectReferenceValue != null ? $"  {OK} _rowContainer" : $"  {ERR} _rowContainer 未绑定");
                sb.AppendLine(so.FindProperty("_emptyHint").objectReferenceValue != null ? $"  {OK} _emptyHint"  : $"  {ERR} _emptyHint 未绑定");
            }
        }

        // ── 2. SurvivalSettingsPanel ──────────────────────────────────
        sb.AppendLine("\n【SurvivalSettingsPanel】");
        var settT = FindT("Canvas/SurvivalSettingsPanel");
        if (settT == null) { sb.AppendLine($"  {ERR} GameObject 未找到"); }
        else
        {
            var settUI = settT.GetComponent<SurvivalSettingsUI>();
            sb.AppendLine(settUI != null ? $"  {OK} SurvivalSettingsUI 组件已挂载" : $"  {ERR} SurvivalSettingsUI 组件缺失");
            if (settUI != null)
            {
                var so = new SerializedObject(settUI);
                string[] fields = { "_panel","_closeBtn","_bgmSlider","_bgmValueText","_bgmToggle","_bgmToggleText",
                                    "_sfxSlider","_sfxValueText","_sfxToggle","_sfxToggleText","_versionText",
                                    "_giftVideoToggle","_vipVideoToggle" };
                foreach (var f in fields)
                {
                    var p = so.FindProperty(f);
                    if (p == null) sb.AppendLine($"  {NA} {f} 字段不存在");
                    else sb.AppendLine(p.objectReferenceValue != null ? $"  {OK} {f}" : $"  {ERR} {f} 未绑定");
                }
            }
        }

        // ── 3. SurvivalIdleUI refs ───────────────────────────────────
        sb.AppendLine("\n【SurvivalIdleUI】");
        var idleUI = Object.FindObjectOfType<SurvivalIdleUI>(true);
        if (idleUI == null) { sb.AppendLine($"  {ERR} SurvivalIdleUI 未找到"); }
        else
        {
            var so = new SerializedObject(idleUI);
            var pRank = so.FindProperty("_rankingPanel");
            var pSett = so.FindProperty("_settingsPanel");
            sb.AppendLine(pRank != null && pRank.objectReferenceValue != null ? $"  {OK} _rankingPanel" : $"  {ERR} _rankingPanel 未绑定");
            sb.AppendLine(pSett != null && pSett.objectReferenceValue != null ? $"  {OK} _settingsPanel" : $"  {ERR} _settingsPanel 未绑定");
        }

        // ── 4. SurvivalSettlementUI _top3Slots ───────────────────────
        sb.AppendLine("\n【SurvivalSettlementPanel — _top3Slots】");
        var settlT = FindT("Canvas/SurvivalSettlementPanel");
        if (settlT == null) { sb.AppendLine($"  {ERR} SurvivalSettlementPanel 未找到"); }
        else
        {
            var settlUI = settlT.GetComponent<SurvivalSettlementUI>();
            if (settlUI == null) { sb.AppendLine($"  {ERR} SurvivalSettlementUI 组件缺失"); }
            else
            {
                var so = new SerializedObject(settlUI);
                var slots = so.FindProperty("_top3Slots");
                if (slots == null) { sb.AppendLine($"  {ERR} _top3Slots 字段不存在"); }
                else
                {
                    sb.AppendLine($"  数组长度: {slots.arraySize}");
                    for (int i = 0; i < slots.arraySize; i++)
                    {
                        var elem = slots.GetArrayElementAtIndex(i);
                        sb.AppendLine(elem.objectReferenceValue != null
                            ? $"  {OK} _top3Slots[{i}] → {elem.objectReferenceValue.name}"
                            : $"  {ERR} _top3Slots[{i}] 未绑定");
                    }
                }
            }
        }

        // ── 5. GameControlUI — BottomBar 按钮 ────────────────────────
        sb.AppendLine("\n【GameControlUI — BottomBar】");
        var gcuiAll = Object.FindObjectsOfType<GameControlUI>(true);
        if (gcuiAll == null || gcuiAll.Length == 0) { sb.AppendLine($"  {ERR} GameControlUI 未找到"); }
        else
        {
            var gcui = gcuiAll[0];
            var so = new SerializedObject(gcui);
            string[] btns = { "startButton","pauseButton","endButton","resetButton",
                              "giftT1Button","giftT3Button","giftT5Button","freezeButton","monsterButton" };
            foreach (var b in btns)
            {
                var p = so.FindProperty(b);
                if (p == null) sb.AppendLine($"  {NA} {b} 字段不存在");
                else sb.AppendLine(p.objectReferenceValue != null ? $"  {OK} {b}" : $"  {ERR} {b} 未绑定");
            }
        }

        // ── 6. BottomBar 新按钮存在性 ────────────────────────────────
        sb.AppendLine("\n【BottomBar 子按钮存在性】");
        string[] btnNames = { "BtnStart","BtnPause","BtnEnd","BtnReset",
                              "BtnGiftT1","BtnGiftT3","BtnGiftT5","BtnFreeze","BtnMonster" };
        foreach (var name in btnNames)
        {
            var t = FindT($"Canvas/BottomBar/{name}");
            sb.AppendLine(t != null ? $"  {OK} {name}" : $"  {ERR} {name} 缺失");
        }

        // ── 7. SurvivalSettlementPanel 新按钮 ────────────────────────
        sb.AppendLine("\n【SurvivalSettlementPanel 按钮】");
        var vr = FindT("Canvas/SurvivalSettlementPanel/BtnViewRanking");
        sb.AppendLine(vr != null ? $"  {OK} BtnViewRanking" : $"  {ERR} BtnViewRanking 缺失");
        var rb = FindT("Canvas/SurvivalSettlementPanel/RestartButton");
        sb.AppendLine(rb != null ? $"  {OK} RestartButton (返回大厅)" : $"  {ERR} RestartButton 缺失");

        Debug.Log("[StatusCheck]\n" + sb.ToString());
    }
}
