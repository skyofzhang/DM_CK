using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.Video;
using DrscfZ.UI;

public class VerifyVideoBindings
{
    [MenuItem("Tools/DrscfZ/Verify Video Bindings")]
    public static void Execute()
    {
        // --- GiftAnimationUI ---
        var giftAnims = Resources.FindObjectsOfTypeAll<GiftAnimationUI>();
        if (giftAnims.Length == 0) { Debug.LogError("[Verify] 找不到 GiftAnimationUI"); return; }

        var ga = giftAnims[0];
        var soGa = new SerializedObject(ga);

        var clips = soGa.FindProperty("tierVideoClips");
        Debug.Log($"[Verify] GiftAnimationUI.tierVideoClips size = {clips.arraySize}");
        for (int i = 0; i < clips.arraySize; i++)
        {
            var c = clips.GetArrayElementAtIndex(i).objectReferenceValue;
            Debug.Log($"  [tier{i+1}] = {(c != null ? c.name : "NULL")}");
        }

        var vpDisplay = soGa.FindProperty("_vipVideoDisplay").objectReferenceValue;
        var vpCG      = soGa.FindProperty("_vipCanvasGroup").objectReferenceValue;
        Debug.Log($"[Verify] _vipVideoDisplay = {(vpDisplay != null ? vpDisplay.name : "NULL !!!")}");
        Debug.Log($"[Verify] _vipCanvasGroup  = {(vpCG != null ? vpCG.name : "NULL !!!")}");

        // --- VIPAnnouncementUI ---
        var vipUIs = Resources.FindObjectsOfTypeAll<VIPAnnouncementUI>();
        if (vipUIs.Length == 0) { Debug.LogError("[Verify] 找不到 VIPAnnouncementUI"); return; }

        var vu = vipUIs[0];
        var soVu = new SerializedObject(vu);
        string[] fields = {
            "weeklyRank1Clip","weeklyRank2Clip","weeklyRank3Clip","weeklyRank4_10Clip","weeklyRank11_20Clip",
            "monthlyRank1Clip","monthlyRank2Clip","monthlyRank3Clip","monthlyRank4_10Clip","monthlyRank11_20Clip"
        };
        foreach (var f in fields)
        {
            var val = soVu.FindProperty(f)?.objectReferenceValue;
            Debug.Log($"[Verify] VIPAnnouncementUI.{f} = {(val != null ? val.name : "NULL !!!")}");
        }

        Debug.Log("[Verify] 验证完成");
    }
}
