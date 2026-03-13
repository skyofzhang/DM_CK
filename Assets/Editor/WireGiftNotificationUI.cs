using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using DrscfZ.UI;

/// <summary>
/// 自动为 GiftNotificationUI 的所有 [SerializeField] 引用完成绑定：
///   - T1~T5 面板及子对象
///   - GiftBannerSlot[] 三槽位
/// 运行一次即可，之后字段保存在 Scene 序列化数据中。
/// </summary>
public class WireGiftNotificationUI
{
    [MenuItem("Tools/Phase2/Wire GiftNotificationUI SerializedFields")]
    public static void Execute()
    {
        var giftCanvasGO = GameObject.Find("Gift_Canvas");
        if (giftCanvasGO == null)
        {
            Debug.LogError("[WireGiftNotificationUI] Gift_Canvas not found in scene.");
            return;
        }

        var ui = giftCanvasGO.GetComponent<GiftNotificationUI>();
        if (ui == null)
        {
            Debug.LogError("[WireGiftNotificationUI] GiftNotificationUI component not found on Gift_Canvas.");
            return;
        }

        var so = new SerializedObject(ui);

        // ── _canvasRoot ───────────────────────────────────────────────────────
        so.FindProperty("_canvasRoot").objectReferenceValue =
            giftCanvasGO.GetComponent<RectTransform>();

        // ── T1 ───────────────────────────────────────────────────────────────
        var t1 = giftCanvasGO.transform.Find("T1_StarParticle");
        so.FindProperty("_t1Particle").objectReferenceValue =
            t1 != null ? t1.GetComponent<ParticleSystem>() : null;

        // ── T2 ───────────────────────────────────────────────────────────────
        var t2 = giftCanvasGO.transform.Find("T2_BorderEffect");
        so.FindProperty("_t2Panel").objectReferenceValue =
            t2 != null ? t2.gameObject : null;
        so.FindProperty("_t2TopLeftPS").objectReferenceValue =
            t2 != null ? t2.Find("TopLeft_PS")?.GetComponent<ParticleSystem>() : null;
        so.FindProperty("_t2TopRightPS").objectReferenceValue =
            t2 != null ? t2.Find("TopRight_PS")?.GetComponent<ParticleSystem>() : null;
        so.FindProperty("_t2BotLeftPS").objectReferenceValue =
            t2 != null ? t2.Find("BotLeft_PS")?.GetComponent<ParticleSystem>() : null;
        so.FindProperty("_t2BotRightPS").objectReferenceValue =
            t2 != null ? t2.Find("BotRight_PS")?.GetComponent<ParticleSystem>() : null;
        so.FindProperty("_t2CenterRing").objectReferenceValue =
            t2 != null ? t2.Find("CenterRing_Image")?.GetComponent<Image>() : null;

        // ── T3 ───────────────────────────────────────────────────────────────
        var t3 = giftCanvasGO.transform.Find("T3_GiftBounce");
        so.FindProperty("_t3Panel").objectReferenceValue =
            t3 != null ? t3.gameObject : null;
        so.FindProperty("_t3GiftIcon").objectReferenceValue =
            t3 != null ? t3.Find("GiftIcon_Image")?.GetComponent<Image>() : null;
        so.FindProperty("_t3ExplodePS").objectReferenceValue =
            t3 != null ? t3.Find("Explode_PS")?.GetComponent<ParticleSystem>() : null;

        // ── T4 ───────────────────────────────────────────────────────────────
        var t4 = giftCanvasGO.transform.Find("T4_FullscreenGlow");
        so.FindProperty("_t4Panel").objectReferenceValue =
            t4 != null ? t4.gameObject : null;
        so.FindProperty("_t4OrangeOverlay").objectReferenceValue =
            t4 != null ? t4.Find("OrangeOverlay")?.GetComponent<Image>() : null;
        so.FindProperty("_t4BatteryIcon").objectReferenceValue =
            t4 != null ? t4.Find("BatteryIcon")?.GetComponent<Image>() : null;
        so.FindProperty("_t4ChargingSlider").objectReferenceValue =
            t4 != null ? t4.Find("ChargingSlider")?.GetComponent<Slider>() : null;

        // ── T5 ───────────────────────────────────────────────────────────────
        var t5 = giftCanvasGO.transform.Find("T5_EpicAirdrop");
        so.FindProperty("_t5Panel").objectReferenceValue =
            t5 != null ? t5.gameObject : null;
        so.FindProperty("_t5BlackOverlay").objectReferenceValue =
            t5 != null ? t5.Find("BlackOverlay")?.GetComponent<Image>() : null;
        so.FindProperty("_t5AirdropBox").objectReferenceValue =
            t5 != null ? t5.Find("AirdropBox")?.GetComponent<RectTransform>() : null;
        so.FindProperty("_t5FireworksPS").objectReferenceValue =
            t5 != null ? t5.Find("Fireworks_PS")?.GetComponent<ParticleSystem>() : null;
        so.FindProperty("_t5ResourceIcons").objectReferenceValue =
            t5 != null ? t5.Find("ResourceIcons")?.GetComponent<RectTransform>() : null;
        so.FindProperty("_t5PlayerNameText").objectReferenceValue =
            t5 != null ? t5.Find("PlayerNameText")?.GetComponent<TextMeshProUGUI>() : null;

        // ── GiftBannerSlots ───────────────────────────────────────────────────
        var bannerQueue = giftCanvasGO.transform.Find("GiftBannerQueue");
        var slotsArr = so.FindProperty("_bannerSlots");
        slotsArr.arraySize = 3;

        for (int i = 0; i < 3; i++)
        {
            Transform slotT = bannerQueue != null ? bannerQueue.Find($"BannerSlot_{i}") : null;
            var slotProp = slotsArr.GetArrayElementAtIndex(i);

            // root → BannerSlot_N GameObject
            slotProp.FindPropertyRelative("root").objectReferenceValue =
                slotT != null ? slotT.gameObject : null;

            // colorBar → BannerSlot_N 上的 Image（作为背景色条复用）
            slotProp.FindPropertyRelative("colorBar").objectReferenceValue =
                slotT != null ? slotT.GetComponent<Image>() : null;

            // giftIcon → null（当前 BannerSlot 无专用礼物图标子对象，null-check 保护）
            slotProp.FindPropertyRelative("giftIcon").objectReferenceValue = null;

            // infoText → BannerText TMP
            var bannerTextT = slotT != null ? slotT.Find("BannerText") : null;
            slotProp.FindPropertyRelative("infoText").objectReferenceValue =
                bannerTextT != null ? bannerTextT.GetComponent<TextMeshProUGUI>() : null;

            // tierTag → null（当前无专用标签，null-check 保护）
            slotProp.FindPropertyRelative("tierTag").objectReferenceValue = null;

            // 确保 BannerSlot 初始不激活（FindFreeBannerSlot 依赖 !activeSelf）
            if (slotT != null)
                slotT.gameObject.SetActive(false);
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(ui);

        // 保存场景
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[WireGiftNotificationUI] ✅ GiftNotificationUI: _canvasRoot + T1~T5 panels + 3 BannerSlots 全部完成绑定，场景已保存。");
    }
}
