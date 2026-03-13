using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// 修复遗留角力UI + 修正 GiftIconBar 数值标签
/// </summary>
public class FixUIRemnants
{
    public static string Execute()
    {
        var log = new System.Text.StringBuilder();

        // === 1. 隐藏 BtnEnd（旧"结束对局"按钮，有橙子图标，不适用于生存游戏）===
        var btnEnd = GameObject.Find("BtnEnd");
        if (btnEnd != null)
        {
            btnEnd.SetActive(false);
            log.AppendLine("✅ BtnEnd SetActive(false)");
        }
        else log.AppendLine("⚠️ BtnEnd not found");

        // === 2. 修正 GiftIconBar 7个档位的标签文字（策划案数值）===
        var tierData = new (string goName, string label)[]
        {
            ("GiftTier1", "小温暖\n≤5分"),
            ("GiftTier2", "暖炉礼\n≤50分"),
            ("GiftTier3", "丰收篮\n≤200分"),
            ("GiftTier4", "守护盾\n≤500分"),
            ("GiftTier5", "暴风眼\n≤1500分"),
            ("GiftTier6", "极地堡垒\n≤5000分"),
            ("GiftTier7", "神秘空投\n>5000分"),
        };

        foreach (var (goName, label) in tierData)
        {
            // 找到 GiftTier -> Label 子对象
            var tierGO = GameObject.Find(goName);
            if (tierGO == null)
            {
                log.AppendLine($"⚠️ {goName} not found via Find, trying hierarchy search");
                // 在 GiftIconBar 下搜索
                var bar = GameObject.Find("GiftIconBar");
                if (bar != null)
                {
                    var child = bar.transform.Find(goName);
                    if (child != null) tierGO = child.gameObject;
                }
            }

            if (tierGO == null)
            {
                log.AppendLine($"❌ {goName} NOT FOUND");
                continue;
            }

            // 找 Label（TMP）子对象
            var labelTrans = tierGO.transform.Find("Label");
            if (labelTrans == null)
            {
                // 尝试找第一个TMP子对象
                var tmp = tierGO.GetComponentInChildren<TMP_Text>(true);
                if (tmp != null)
                {
                    tmp.text = label;
                    log.AppendLine($"✅ {goName} label updated (via GetComponentInChildren)");
                }
                else log.AppendLine($"❌ {goName}: no Label or TMP found");
                continue;
            }

            var labelTMP = labelTrans.GetComponent<TMP_Text>();
            if (labelTMP == null)
            {
                log.AppendLine($"❌ {goName}/Label: no TMP_Text component");
                continue;
            }

            labelTMP.text = label;
            log.AppendLine($"✅ {goName} label = \"{label}\"");
        }

        // === 3. 修正 SnowGround 材质为更白的雪地色 ===
        var snowGround = GameObject.Find("SnowGround");
        if (snowGround != null)
        {
            var renderer = snowGround.GetComponent<MeshRenderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                // 直接修改材质颜色为纯白/淡蓝
                var mat = renderer.material; // 注意：这是实例化材质
                mat.color = new Color(0.94f, 0.96f, 1.0f, 1f); // 淡蓝白色
                log.AppendLine("✅ SnowGround color updated to snow white");
            }
            else log.AppendLine("⚠️ SnowGround: MeshRenderer or material not found");
        }
        else log.AppendLine("⚠️ SnowGround not found");

        // 标记场景已修改
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        return log.ToString();
    }
}
