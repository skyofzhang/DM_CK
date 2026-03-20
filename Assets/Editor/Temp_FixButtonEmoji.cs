using UnityEngine;
using UnityEditor;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 去掉 BottomBar 按钮文字中的 emoji，替换为纯中文
/// </summary>
public class Temp_FixButtonEmoji
{
    // 按钮名 → 期望文字（无 emoji）
    static readonly Dictionary<string, string> Labels = new Dictionary<string, string>
    {
        { "BtnConnect",  "GM连接" },
        { "BtnStart",    "开始游戏" },
        { "BtnPause",    "暂停游戏" },
        { "BtnEnd",      "结束游戏" },
        { "BtnReset",    "重置" },
        { "BtnSimulate", "模拟" },
        { "BtnGiftT1",   "T1 礼物" },
        { "BtnGiftT3",   "T3 礼物" },
        { "BtnGiftT5",   "T5 礼物" },
        { "BtnFreeze",   "冻结" },
        { "BtnMonster",  "召唤怪物" },
    };

    public static void Execute()
    {
        // 找 BottomBar
        var all = Object.FindObjectsOfType<Transform>(true);
        Transform bottomBar = null;
        foreach (var t in all)
        {
            if (t.name == "BottomBar") { bottomBar = t; break; }
        }
        if (bottomBar == null)
        {
            Debug.LogError("[FixEmoji] BottomBar not found");
            return;
        }

        int fixed_ = 0;
        foreach (var kv in Labels)
        {
            var btnTr = bottomBar.Find(kv.Key);
            if (btnTr == null) { Debug.LogWarning($"[FixEmoji] {kv.Key} not found"); continue; }

            // 找子 TMP
            var tmp = btnTr.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp == null) { Debug.LogWarning($"[FixEmoji] {kv.Key} has no TMP"); continue; }

            if (tmp.text != kv.Value)
            {
                Undo.RecordObject(tmp, "Fix Emoji");
                tmp.text = kv.Value;
                EditorUtility.SetDirty(tmp);
                Debug.Log($"[FixEmoji] {kv.Key}: \"{tmp.text}\" → \"{kv.Value}\"");
                fixed_++;
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[FixEmoji] Done. Fixed {fixed_} button(s). Scene saved.");
    }
}
