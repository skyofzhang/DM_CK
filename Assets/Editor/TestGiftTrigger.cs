using UnityEditor;
using UnityEngine;
using System.Reflection;
using DrscfZ.Core;
using DrscfZ.UI;

/// <summary>
/// 编辑器菜单：Play Mode 中触发测试礼物，验证生存礼物动画显示效果。
/// 菜单：Tools / Test Gift (T1~T6)
/// </summary>
public static class TestGiftTrigger
{
    [MenuItem("Tools/Test Gift/T1 仙女棒")]
    static void T1() => Fire("fairy_wand", "仙女棒", "测试玩家A", 1);

    [MenuItem("Tools/Test Gift/T2 能力药丸")]
    static void T2() => Fire("ability_pill", "能力药丸", "测试玩家B", 2);

    [MenuItem("Tools/Test Gift/T3 甜甜圈")]
    static void T3() => Fire("donut", "甜甜圈", "甜甜圈大王", 3);

    [MenuItem("Tools/Test Gift/T4 能量电池")]
    static void T4() => Fire("energy_battery", "能量电池", "电池侠", 4);

    [MenuItem("Tools/Test Gift/T5 爱的爆炸")]
    static void T5() => Fire("love_explosion", "爱的爆炸", "爆炸玩家", 5);

    [MenuItem("Tools/Test Gift/T6 神秘空投")]
    static void T6() => Fire("mystery_airdrop", "神秘空投", "史诗玩家", 6);

    [MenuItem("Tools/Test Gift/T1 仙女棒", true)]
    [MenuItem("Tools/Test Gift/T2 能力药丸", true)]
    [MenuItem("Tools/Test Gift/T3 甜甜圈", true)]
    [MenuItem("Tools/Test Gift/T4 能量电池", true)]
    [MenuItem("Tools/Test Gift/T5 爱的爆炸", true)]
    [MenuItem("Tools/Test Gift/T6 神秘空投", true)]
    static bool ValidatePlay() => EditorApplication.isPlaying;

    static void Fire(string giftId, string giftName, string nickname, int tier)
    {
        var anim = GiftAnimationUI.Instance;
        if (anim == null) { Debug.LogError("[TestGift] GiftAnimationUI.Instance is null"); return; }

        var method = typeof(GiftAnimationUI).GetMethod("ShowGiftPopup", BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null) { Debug.LogError("[TestGift] GiftAnimationUI.ShowGiftPopup method not found"); return; }

        var data = new GiftReceivedData
        {
            playerId = $"editor_test_{giftId}",
            playerName = nickname,
            avatarUrl = string.Empty,
            camp = "left",
            giftId = giftId,
            giftName = giftName,
            forceValue = 0f,
            isSummon = false,
            unitId = string.Empty,
            giftCount = 1,
            tier = tier.ToString()
        };

        method.Invoke(anim, new object[] { data, tier, "left" });
        Debug.Log($"[TestGift] Fired tier={tier} gift='{giftId}' from '{nickname}'");
    }
}
