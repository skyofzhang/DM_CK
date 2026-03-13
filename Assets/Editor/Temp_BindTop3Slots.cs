using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using DrscfZ.UI;

/// <summary>
/// 临时脚本：将 ScreenC/Top3Slot_0~2 绑定到 SurvivalSettlementUI._top3Slots
/// 用完可删除
/// </summary>
public class Temp_BindTop3Slots
{
    public static void Execute()
    {
        // 用 FindObjectsOfTypeAll 找到 inactive 对象上的组件
        var uis = Resources.FindObjectsOfTypeAll<SurvivalSettlementUI>();
        if (uis == null || uis.Length == 0)
        {
            Debug.LogError("[BindTop3Slots] ❌ 未找到 SurvivalSettlementUI 组件");
            return;
        }

        var ui = uis[0];
        Debug.Log($"[BindTop3Slots] 找到组件，所在对象: {ui.gameObject.name}");

        // 通过 Transform.Find 定位 ScreenC 及其子槽位（支持 inactive）
        Transform screenC = ui.transform.Find("ScreenC");
        if (screenC == null)
        {
            Debug.LogError("[BindTop3Slots] ❌ 未找到 ScreenC，请确认层级路径正确");
            return;
        }

        Transform slot0 = screenC.Find("Top3Slot_0");
        Transform slot1 = screenC.Find("Top3Slot_1");
        Transform slot2 = screenC.Find("Top3Slot_2");

        if (slot0 == null || slot1 == null || slot2 == null)
        {
            Debug.LogError($"[BindTop3Slots] ❌ 槽位未找到：" +
                           $"Slot_0={slot0 != null}, Slot_1={slot1 != null}, Slot_2={slot2 != null}");
            return;
        }

        // 验证每个槽位至少有 2 个 TextMeshPro 子组件
        foreach (var slot in new[] { slot0, slot1, slot2 })
        {
            var tmps = slot.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            if (tmps.Length < 2)
            {
                Debug.LogWarning($"[BindTop3Slots] ⚠ {slot.name} 子 TMP 数量={tmps.Length}（需≥2），" +
                                 $"运行时会报错。请检查 NameText/ScoreText 是否存在。");
            }
            else
            {
                Debug.Log($"[BindTop3Slots] ✓ {slot.name}: texts[0]={tmps[0].name}, texts[1]={tmps[1].name}");
            }
        }

        // 用 SerializedObject 修改 private 序列化数组字段
        var so = new SerializedObject(ui);
        var prop = so.FindProperty("_top3Slots");

        if (prop == null)
        {
            Debug.LogError("[BindTop3Slots] ❌ 未找到序列化字段 _top3Slots，请确认字段名拼写");
            return;
        }

        prop.arraySize = 3;
        prop.GetArrayElementAtIndex(0).objectReferenceValue = slot0.gameObject;
        prop.GetArrayElementAtIndex(1).objectReferenceValue = slot1.gameObject;
        prop.GetArrayElementAtIndex(2).objectReferenceValue = slot2.gameObject;
        so.ApplyModifiedProperties();

        // 标记场景为已修改，确保可以保存
        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);

        Debug.Log($"[BindTop3Slots] ✅ 绑定完成！\n" +
                  $"  _top3Slots[0] = {slot0.name}\n" +
                  $"  _top3Slots[1] = {slot1.name}\n" +
                  $"  _top3Slots[2] = {slot2.name}\n" +
                  $"  场景已标脏，请用 CapybaraDuel/Save Current Scene 保存");
    }
}
