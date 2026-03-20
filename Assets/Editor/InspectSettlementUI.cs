using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

public class InspectSettlementUI
{
    public static void Execute()
    {
        // 通过脚本组件定位，不依赖名字
        var comp = Object.FindObjectOfType<DrscfZ.UI.SurvivalSettlementUI>(true);
        if (comp == null) { Debug.LogError("[Inspect] 未找到 SurvivalSettlementUI 组件"); return; }

        GameObject go = comp.gameObject;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== SurvivalSettlementUI on: " + go.name + " ===");

        // 所有 TMP 文字（包含未激活对象）
        foreach (var t in go.GetComponentsInChildren<TextMeshProUGUI>(true))
            sb.AppendLine($"  TMP [{t.gameObject.name}] selfActive={t.gameObject.activeSelf}: \"{t.text}\"");

        // 所有按钮
        sb.AppendLine("--- Buttons ---");
        foreach (var b in go.GetComponentsInChildren<Button>(true))
        {
            var bTmp = b.GetComponentInChildren<TextMeshProUGUI>(true);
            sb.AppendLine($"  Button [{b.gameObject.name}] active={b.gameObject.activeSelf}: \"{(bTmp ? bTmp.text : "N/A")}\"");
        }

        // 检查关键字段绑定（通过序列化）
        var so = new SerializedObject(comp);
        string[] fields = { "_mvpNameText", "_mvpScoreText", "_mvpAnchorLineText", "_restartButton", "_top3Slots" };
        sb.AppendLine("--- Inspector Bindings ---");
        foreach (var f in fields)
        {
            var prop = so.FindProperty(f);
            bool bound = prop != null && prop.objectReferenceValue != null;
            sb.AppendLine($"  {f}: {(bound ? "已绑定 → " + prop.objectReferenceValue.name : "NULL (未绑定!)")}");
        }

        Debug.Log(sb.ToString());
    }
}
