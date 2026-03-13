using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel 06 修复：结算面板按钮重构
/// HTML06 .btns: .main(480×100 查看英雄榜 x=-165,y=-750) + .sub(300×80 返回大厅 x=+255,y=-740)
/// 原 RestartButton(560×110 center) → 改为 .sub 返回大厅(300×80 x=255,y=-740)
/// 新增 BtnViewRanking(480×100 x=-165,y=-750)
/// </summary>
public class Temp_FixSettlementButtons
{
    public static void Execute()
    {
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

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== Settlement Button Fix ===");

        var panel = FindRT("Canvas/SurvivalSettlementPanel");
        if (panel == null) { Debug.LogError("[FixSettlementButtons] SurvivalSettlementPanel not found"); return; }

        // ── 1. 修改 RestartButton → 次按钮"返回大厅" ────────────────────────
        var restartBtn = FindRT("Canvas/SurvivalSettlementPanel/RestartButton");
        if (restartBtn != null)
        {
            sb.AppendLine($"RestartButton 修改前: pos({restartBtn.anchoredPosition.x:F0},{restartBtn.anchoredPosition.y:F0}) size({restartBtn.sizeDelta.x:F0},{restartBtn.sizeDelta.y:F0})");

            restartBtn.anchorMin = restartBtn.anchorMax = new Vector2(0.5f, 0.5f);
            restartBtn.pivot = new Vector2(0.5f, 0.5f);
            restartBtn.anchoredPosition = new Vector2(255f, -740f);
            restartBtn.sizeDelta = new Vector2(300f, 80f);

            // 次按钮颜色：深蓝 (HTML06 .sub 样式)
            var img = restartBtn.GetComponent<Image>();
            if (img) img.color = new Color(0.12f, 0.23f, 0.37f, 1f);

            // 更新文字
            var tmp = restartBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) { tmp.text = "返回大厅"; tmp.fontSize = 26; }
            else
            {
                var legacyText = restartBtn.GetComponentInChildren<Text>();
                if (legacyText != null) legacyText.text = "返回大厅";
            }

            EditorUtility.SetDirty(restartBtn);
            sb.AppendLine("✅ RestartButton → 返回大厅: pos(255,-740) size(300,80)");
        }
        else sb.AppendLine("❌ RestartButton 未找到");

        // ── 2. 添加/更新 BtnViewRanking → 主按钮"查看英雄榜" ───────────────
        RectTransform vrRT = FindRT("Canvas/SurvivalSettlementPanel/BtnViewRanking");
        if (vrRT == null)
        {
            var go = new GameObject("BtnViewRanking");
            go.transform.SetParent(panel, false);

            vrRT = go.AddComponent<RectTransform>();
            vrRT.anchorMin = vrRT.anchorMax = new Vector2(0.5f, 0.5f);
            vrRT.pivot = new Vector2(0.5f, 0.5f);
            vrRT.anchoredPosition = new Vector2(-165f, -750f);
            vrRT.sizeDelta = new Vector2(480f, 100f);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.06f, 0.40f, 0.20f, 1f); // 深绿 (主按钮)

            go.AddComponent<Button>();

            // 文字子对象
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRT = textGo.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = textRT.offsetMax = Vector2.zero;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "查看英雄榜";
            tmp.fontSize = 30;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.3f, 1f, 0.5f); // 亮绿文字
            tmp.alignment = TextAlignmentOptions.Center;

            EditorUtility.SetDirty(panel);
            sb.AppendLine("✅ BtnViewRanking 新建: pos(-165,-750) size(480,100)");
        }
        else
        {
            // 已存在则仅调整位置
            vrRT.anchoredPosition = new Vector2(-165f, -750f);
            vrRT.sizeDelta = new Vector2(480f, 100f);
            EditorUtility.SetDirty(vrRT);
            sb.AppendLine("✅ BtnViewRanking 已存在，更新位置: pos(-165,-750) size(480,100)");
        }

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        sb.AppendLine("✅ Scene saved");
        Debug.Log("[FixSettlementButtons]\n" + sb.ToString());
    }
}
