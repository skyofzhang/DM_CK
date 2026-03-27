using UnityEngine;
using UnityEditor;
using UnityEngine.Video;
using DrscfZ.UI;

/// <summary>
/// 将 GiftAnimationUI.tierVideoClips[0-5] 绑定到新转换的 tier1~tier6.webm，
/// 并将 VIPAnnouncementUI weekly/monthly Clip 字段绑定到 vip_rank*.webm
/// </summary>
public class BindGiftVIPVideos
{
    [MenuItem("Tools/DrscfZ/Bind Gift & VIP Videos")]
    public static void Execute()
    {
        // ============ 1. 加载礼物视频 ============
        var tier1  = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Art/GiftGifs/tier1.webm");
        var tier2  = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Art/GiftGifs/tier2.webm");
        var tier3  = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Art/GiftGifs/tier3.webm");
        var tier4  = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Art/GiftGifs/tier4.webm");
        var tier5  = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Art/GiftGifs/tier5.webm");
        var tier6  = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Art/GiftGifs/tier6.webm");

        VideoClip[] clips = new[] { tier1, tier2, tier3, tier4, tier5, tier6 };
        for (int i = 0; i < clips.Length; i++)
            if (clips[i] == null) Debug.LogWarning($"[BindGiftVIPVideos] tier{i+1}.webm NOT FOUND");

        // ============ 2. 加载 VIP 视频 ============
        var vipRank1     = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Art/VIPEntry/vip_rank1.webm");
        var vipRank2     = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Art/VIPEntry/vip_rank2.webm");
        var vipRank3     = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Art/VIPEntry/vip_rank3.webm");
        var vipRank4_10  = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Art/VIPEntry/vip_rank4_10.webm");
        var vipRank11_20 = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Art/VIPEntry/vip_rank11_20.webm");

        if (vipRank1    == null) Debug.LogWarning("[BindGiftVIPVideos] vip_rank1.webm NOT FOUND");
        if (vipRank2    == null) Debug.LogWarning("[BindGiftVIPVideos] vip_rank2.webm NOT FOUND");
        if (vipRank3    == null) Debug.LogWarning("[BindGiftVIPVideos] vip_rank3.webm NOT FOUND");
        if (vipRank4_10 == null) Debug.LogWarning("[BindGiftVIPVideos] vip_rank4_10.webm NOT FOUND");
        if (vipRank11_20== null) Debug.LogWarning("[BindGiftVIPVideos] vip_rank11_20.webm NOT FOUND");

        // ============ 3. 绑定 GiftAnimationUI ============
        // 使用 FindObjectsOfTypeAll 以包含 inactive 对象
        var giftAnims = Resources.FindObjectsOfTypeAll<GiftAnimationUI>();
        if (giftAnims.Length == 0)
        {
            Debug.LogWarning("[BindGiftVIPVideos] 场景中找不到 GiftAnimationUI！");
        }
        else
        {
            foreach (var ga in giftAnims)
            {
                var so = new SerializedObject(ga);
                var arr = so.FindProperty("tierVideoClips");
                if (arr != null)
                {
                    arr.arraySize = 6;
                    for (int i = 0; i < 6; i++)
                        arr.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(ga);
                    Debug.Log($"[BindGiftVIPVideos] GiftAnimationUI ({ga.gameObject.name}) tierVideoClips 绑定完成");
                }
                else
                {
                    Debug.LogWarning("[BindGiftVIPVideos] 找不到 tierVideoClips 属性");
                }
            }
        }

        // ============ 4. 绑定 VIPAnnouncementUI ============
        var vipUIs = Resources.FindObjectsOfTypeAll<VIPAnnouncementUI>();
        if (vipUIs.Length == 0)
        {
            Debug.LogWarning("[BindGiftVIPVideos] 场景中找不到 VIPAnnouncementUI！");
        }
        else
        {
            foreach (var vu in vipUIs)
            {
                var so = new SerializedObject(vu);

                // 周榜
                SetClip(so, "weeklyRank1Clip",    vipRank1);
                SetClip(so, "weeklyRank2Clip",    vipRank2);
                SetClip(so, "weeklyRank3Clip",    vipRank3);
                SetClip(so, "weeklyRank4_10Clip", vipRank4_10);
                SetClip(so, "weeklyRank11_20Clip",vipRank11_20);

                // 月榜（复用同一套视频）
                SetClip(so, "monthlyRank1Clip",    vipRank1);
                SetClip(so, "monthlyRank2Clip",    vipRank2);
                SetClip(so, "monthlyRank3Clip",    vipRank3);
                SetClip(so, "monthlyRank4_10Clip", vipRank4_10);
                SetClip(so, "monthlyRank11_20Clip",vipRank11_20);

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(vu);
                Debug.Log($"[BindGiftVIPVideos] VIPAnnouncementUI ({vu.gameObject.name}) 视频绑定完成");
            }
        }

        // ============ 5. 保存场景 ============
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[BindGiftVIPVideos] 场景已保存 ✓");
    }

    private static void SetClip(SerializedObject so, string fieldName, VideoClip clip)
    {
        var prop = so.FindProperty(fieldName);
        if (prop != null)
            prop.objectReferenceValue = clip;
        else
            Debug.LogWarning($"[BindGiftVIPVideos] 找不到字段 {fieldName}");
    }
}
