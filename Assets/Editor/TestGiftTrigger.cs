using UnityEditor;
using UnityEngine;
using System.Reflection;

/// <summary>
/// 编辑器菜单：Play Mode 中触发测试礼物，验证 GiftBannerQueue 显示效果
/// 菜单：Tools / Test Gift Banner (T1~T5)
/// </summary>
public static class TestGiftTrigger
{
    [MenuItem("Tools/Test Gift/T1 仙女棒")]
    static void T1() => Fire("fairy_wand", "测试玩家A", 1);

    [MenuItem("Tools/Test Gift/T2 能力药丸")]
    static void T2() => Fire("power_pill", "测试玩家B", 2);

    [MenuItem("Tools/Test Gift/T3 魔法糖")]
    static void T3() => Fire("magic_candy", "甜甜圈大王", 3);

    [MenuItem("Tools/Test Gift/T4 超能暴射")]
    static void T4() => Fire("super_blaster", "药丸侠", 4);

    [MenuItem("Tools/Test Gift/T5 神秘空投")]
    static void T5() => Fire("airdrop", "史诗玩家", 5);

    [MenuItem("Tools/Test Gift/T1 仙女棒", true)]
    [MenuItem("Tools/Test Gift/T2 能力药丸", true)]
    [MenuItem("Tools/Test Gift/T3 魔法糖", true)]
    [MenuItem("Tools/Test Gift/T4 超能暴射", true)]
    [MenuItem("Tools/Test Gift/T5 神秘空投", true)]
    static bool ValidatePlay() => EditorApplication.isPlaying;

    static void Fire(string giftId, string nickname, int tier)
    {
        var uiType = System.Type.GetType("DrscfZ.UI.GiftNotificationUI, Assembly-CSharp");
        if (uiType == null) { Debug.LogError("[TestGift] GiftNotificationUI type not found"); return; }

        var instProp = uiType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
        var inst = instProp?.GetValue(null);
        if (inst == null) { Debug.LogError("[TestGift] GiftNotificationUI.Instance is null"); return; }

        var method = uiType.GetMethod("ShowGiftEffect", BindingFlags.Instance | BindingFlags.Public);
        if (method == null) { Debug.LogError("[TestGift] ShowGiftEffect method not found"); return; }

        method.Invoke(inst, new object[] { giftId, nickname, tier });
        Debug.Log($"[TestGift] Fired tier={tier} gift='{giftId}' from '{nickname}'");
    }
}
