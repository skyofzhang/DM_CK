using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using DrscfZ.UI;

/// <summary>
/// Session #123 残留任务：
/// Task A — 为 SurvivalRankingPanel 添加 SurvivalRankingUI 组件并绑定字段
/// Task C — 替换 BroadcasterPanel BoostButton/EventButton IconText emoji（⚡→闪电，🌊→海浪）
/// </summary>
public class Temp_FixSession123Remaining
{
    public static void Execute()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== Fix Session123 Remaining Tasks ===");

        // ── 辅助：通过路径查找 RectTransform ─────────────────────────────
        RectTransform FindRT(string path)
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
                if (match) return t as RectTransform;
            }
            return null;
        }

        // ── 辅助：通过路径查找任意 Transform（支持跨Canvas）─────────────
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

        // ════════════════════════════════════════════════════════════════
        // Task A: SurvivalRankingPanel — 添加组件 + 绑定字段
        // ════════════════════════════════════════════════════════════════
        sb.AppendLine("\n--- Task A: SurvivalRankingPanel 组件绑定 ---");

        var rankPanelRT = FindRT("Canvas/SurvivalRankingPanel");
        if (rankPanelRT == null)
        {
            sb.AppendLine("❌ Canvas/SurvivalRankingPanel 未找到");
        }
        else
        {
            var rankGo = rankPanelRT.gameObject;

            // 已有组件则复用，否则添加
            var rankUI = rankGo.GetComponent<SurvivalRankingUI>();
            if (rankUI == null)
            {
                rankUI = rankGo.AddComponent<SurvivalRankingUI>();
                sb.AppendLine("  ✅ SurvivalRankingUI 组件已添加");
            }
            else
            {
                sb.AppendLine("  ℹ SurvivalRankingUI 组件已存在，仅更新字段");
            }

            var so = new SerializedObject(rankUI);
            so.Update();

            // _panel → SurvivalRankingPanel 自身
            var pPanel = so.FindProperty("_panel");
            if (pPanel != null) { pPanel.objectReferenceValue = rankGo; sb.AppendLine("  _panel → SurvivalRankingPanel"); }
            else sb.AppendLine("  ⚠ _panel 字段未找到");

            // _closeBtn → Canvas/SurvivalRankingPanel/CloseBtn
            var closeBtnT = FindT("Canvas/SurvivalRankingPanel/CloseBtn");
            var pClose = so.FindProperty("_closeBtn");
            if (pClose != null && closeBtnT != null)
            {
                pClose.objectReferenceValue = closeBtnT.GetComponent<Button>();
                sb.AppendLine("  _closeBtn → CloseBtn");
            }
            else sb.AppendLine("  ⚠ _closeBtn 绑定失败（CloseBtn或字段未找到）");

            // _titleText → Canvas/SurvivalRankingPanel/TitleText
            var titleT = FindT("Canvas/SurvivalRankingPanel/TitleText");
            var pTitle = so.FindProperty("_titleText");
            if (pTitle != null && titleT != null)
            {
                pTitle.objectReferenceValue = titleT.GetComponent<TMP_Text>();
                sb.AppendLine("  _titleText → TitleText");
            }
            else sb.AppendLine("  ⚠ _titleText 绑定失败");

            // _rowContainer → Canvas/SurvivalRankingPanel/RowContainer
            var rowContT = FindT("Canvas/SurvivalRankingPanel/RowContainer");
            var pRow = so.FindProperty("_rowContainer");
            if (pRow != null && rowContT != null)
            {
                pRow.objectReferenceValue = rowContT;
                sb.AppendLine("  _rowContainer → RowContainer");
            }
            else sb.AppendLine("  ⚠ _rowContainer 绑定失败");

            // _emptyHint → Canvas/SurvivalRankingPanel/EmptyHint
            var emptyT = FindT("Canvas/SurvivalRankingPanel/EmptyHint");
            var pEmpty = so.FindProperty("_emptyHint");
            if (pEmpty != null && emptyT != null)
            {
                pEmpty.objectReferenceValue = emptyT.GetComponent<TMP_Text>();
                sb.AppendLine("  _emptyHint → EmptyHint");
            }
            else sb.AppendLine("  ⚠ _emptyHint 绑定失败");

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(rankGo);
            sb.AppendLine("  ✅ SurvivalRankingUI 字段绑定完成");

            // ── 绑定 SurvivalIdleUI._rankingPanel ────────────────────────
            var idleUI = Object.FindObjectOfType<SurvivalIdleUI>(true);
            if (idleUI != null)
            {
                var soIdle = new SerializedObject(idleUI);
                soIdle.Update();
                var pRankRef = soIdle.FindProperty("_rankingPanel");
                if (pRankRef != null)
                {
                    pRankRef.objectReferenceValue = rankUI;
                    soIdle.ApplyModifiedProperties();
                    EditorUtility.SetDirty(idleUI);
                    sb.AppendLine("  ✅ SurvivalIdleUI._rankingPanel 已绑定");
                }
                else sb.AppendLine("  ⚠ SurvivalIdleUI._rankingPanel 字段未找到");
            }
            else sb.AppendLine("  ⚠ SurvivalIdleUI 未找到（跳过引用绑定）");
        }

        // ════════════════════════════════════════════════════════════════
        // Task C: BroadcasterPanel IconText emoji 替换
        // ════════════════════════════════════════════════════════════════
        sb.AppendLine("\n--- Task C: BroadcasterPanel IconText emoji 替换 ---");

        // 尝试各种可能的路径
        string[] boostPaths = {
            "Broadcaster_Canvas/BroadcasterPanelController/PanelRoot/BoostButton/IconText",
            "Broadcaster_Canvas/PanelRoot/BoostButton/IconText",
            "Canvas/BroadcasterPanelController/PanelRoot/BoostButton/IconText",
            "Canvas/BoostButton/IconText",
        };
        string[] eventPaths = {
            "Broadcaster_Canvas/BroadcasterPanelController/PanelRoot/EventButton/IconText",
            "Broadcaster_Canvas/PanelRoot/EventButton/IconText",
            "Canvas/BroadcasterPanelController/PanelRoot/EventButton/IconText",
            "Canvas/EventButton/IconText",
        };

        Transform boostIconT = null;
        foreach (var path in boostPaths)
        {
            boostIconT = FindT(path);
            if (boostIconT != null) { sb.AppendLine($"  BoostButton/IconText 路径: {path}"); break; }
        }

        Transform eventIconT = null;
        foreach (var path in eventPaths)
        {
            eventIconT = FindT(path);
            if (eventIconT != null) { sb.AppendLine($"  EventButton/IconText 路径: {path}"); break; }
        }

        if (boostIconT != null)
        {
            var tmp = boostIconT.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                sb.AppendLine($"  BoostButton/IconText 修改前: [{tmp.text}]");
                tmp.text = "闪电";
                EditorUtility.SetDirty(boostIconT.gameObject);
                sb.AppendLine("  ✅ BoostButton/IconText → 闪电");
            }
            else sb.AppendLine("  ⚠ BoostButton/IconText 无 TextMeshProUGUI 组件");
        }
        else
        {
            // 回退：广撒网查找名为 IconText 且父为 BoostButton 的对象
            foreach (var t in Resources.FindObjectsOfTypeAll<TextMeshProUGUI>())
            {
                if (t.name == "IconText" && t.transform.parent != null && t.transform.parent.name == "BoostButton")
                {
                    sb.AppendLine($"  BoostButton/IconText 回退查找成功，修改前: [{t.text}]");
                    t.text = "闪电";
                    EditorUtility.SetDirty(t.gameObject);
                    sb.AppendLine("  ✅ BoostButton/IconText → 闪电（回退路径）");
                    break;
                }
            }
        }

        if (eventIconT != null)
        {
            var tmp = eventIconT.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                sb.AppendLine($"  EventButton/IconText 修改前: [{tmp.text}]");
                tmp.text = "海浪";
                EditorUtility.SetDirty(eventIconT.gameObject);
                sb.AppendLine("  ✅ EventButton/IconText → 海浪");
            }
            else sb.AppendLine("  ⚠ EventButton/IconText 无 TextMeshProUGUI 组件");
        }
        else
        {
            foreach (var t in Resources.FindObjectsOfTypeAll<TextMeshProUGUI>())
            {
                if (t.name == "IconText" && t.transform.parent != null && t.transform.parent.name == "EventButton")
                {
                    sb.AppendLine($"  EventButton/IconText 回退查找成功，修改前: [{t.text}]");
                    t.text = "海浪";
                    EditorUtility.SetDirty(t.gameObject);
                    sb.AppendLine("  ✅ EventButton/IconText → 海浪（回退路径）");
                    break;
                }
            }
        }

        // ════════════════════════════════════════════════════════════════
        // 保存场景
        // ════════════════════════════════════════════════════════════════
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        sb.AppendLine("\n✅ Scene saved");
        Debug.Log("[FixSession123Remaining]\n" + sb.ToString());
    }
}
