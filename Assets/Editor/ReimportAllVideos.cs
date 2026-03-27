using UnityEngine;
using UnityEditor;
using UnityEngine.Video;
using DrscfZ.UI;

/// <summary>
/// 强制重导入所有礼物/VIP视频，并重新绑定到 Inspector
/// </summary>
public class ReimportAllVideos
{
    [MenuItem("Tools/DrscfZ/Reimport All Gift VIP Videos")]
    public static void Execute()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        // 礼物视频
        string[] giftPaths = {
            "Assets/Art/GiftGifs/tier1.webm",
            "Assets/Art/GiftGifs/tier2.webm",
            "Assets/Art/GiftGifs/tier3.webm",
            "Assets/Art/GiftGifs/tier4.webm",
            "Assets/Art/GiftGifs/tier5.webm",
            "Assets/Art/GiftGifs/tier6.webm",
        };

        // VIP视频
        string[] vipPaths = {
            "Assets/Art/VIPEntry/vip_rank1.webm",
            "Assets/Art/VIPEntry/vip_rank2.webm",
            "Assets/Art/VIPEntry/vip_rank3.webm",
            "Assets/Art/VIPEntry/vip_rank4_10.webm",
            "Assets/Art/VIPEntry/vip_rank11_20.webm",
        };

        foreach (var p in giftPaths)
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        foreach (var p in vipPaths)
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

        // 绑定礼物视频
        var clips = new VideoClip[6];
        bool allOk = true;
        for (int i = 0; i < 6; i++)
        {
            clips[i] = AssetDatabase.LoadAssetAtPath<VideoClip>(giftPaths[i]);
            if (clips[i] == null) { Debug.LogError($"[Reimport] {giftPaths[i]} 加载失败！"); allOk = false; }
            else Debug.Log($"[Reimport] tier{i+1} OK: {clips[i].name}");
        }

        var vipClips = new VideoClip[5];
        for (int i = 0; i < 5; i++)
        {
            vipClips[i] = AssetDatabase.LoadAssetAtPath<VideoClip>(vipPaths[i]);
            if (vipClips[i] == null) { Debug.LogError($"[Reimport] {vipPaths[i]} 加载失败！"); allOk = false; }
            else Debug.Log($"[Reimport] {System.IO.Path.GetFileNameWithoutExtension(vipPaths[i])} OK");
        }

        // GiftAnimationUI
        foreach (var ga in Resources.FindObjectsOfTypeAll<GiftAnimationUI>())
        {
            var so = new SerializedObject(ga);
            var arr = so.FindProperty("tierVideoClips");
            arr.arraySize = 6;
            for (int i = 0; i < 6; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(ga);
        }

        // VIPAnnouncementUI
        foreach (var vu in Resources.FindObjectsOfTypeAll<VIPAnnouncementUI>())
        {
            var so = new SerializedObject(vu);
            // rank1=vipClips[0], rank2=[1], rank3=[2], rank4_10=[3], rank11_20=[4]
            string[] fields = {"weeklyRank1Clip","weeklyRank2Clip","weeklyRank3Clip","weeklyRank4_10Clip","weeklyRank11_20Clip",
                               "monthlyRank1Clip","monthlyRank2Clip","monthlyRank3Clip","monthlyRank4_10Clip","monthlyRank11_20Clip"};
            int[] idx       = {0,1,2,3,4,  0,1,2,3,4};
            for (int i = 0; i < fields.Length; i++)
            {
                var prop = so.FindProperty(fields[i]);
                if (prop != null) prop.objectReferenceValue = vipClips[idx[i]];
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(vu);
        }

        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log(allOk ? "[Reimport] 全部完成，场景已保存 ✓" : "[Reimport] 完成，但有加载失败，请检查上方错误");
    }
}
