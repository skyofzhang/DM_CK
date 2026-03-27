using UnityEngine;
using UnityEditor;
using UnityEngine.Video;
using DrscfZ.UI;

public class ReimportAndBindTier2
{
    [MenuItem("Tools/DrscfZ/Reimport And Bind Tier2")]
    public static void Execute()
    {
        // 强制刷新 AssetDatabase
        AssetDatabase.Refresh();

        // 重新导入 tier2.webm
        AssetDatabase.ImportAsset("Assets/Art/GiftGifs/tier2.webm", ImportAssetOptions.ForceUpdate);

        // 加载
        var tier2 = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Art/GiftGifs/tier2.webm");
        if (tier2 == null)
        {
            Debug.LogError("[Reimport] tier2.webm 加载失败，资源不存在或格式不支持！");
            // 列出目录下所有资源
            var guids = AssetDatabase.FindAssets("tier2", new[]{"Assets/Art/GiftGifs"});
            foreach (var g in guids)
                Debug.Log($"  found: {AssetDatabase.GUIDToAssetPath(g)}");
            return;
        }
        Debug.Log($"[Reimport] tier2.webm 加载成功: {tier2.name}");

        // 绑定到 GiftAnimationUI
        var giftAnims = Resources.FindObjectsOfTypeAll<GiftAnimationUI>();
        foreach (var ga in giftAnims)
        {
            var so = new SerializedObject(ga);
            var arr = so.FindProperty("tierVideoClips");
            if (arr != null && arr.arraySize > 1)
            {
                arr.GetArrayElementAtIndex(1).objectReferenceValue = tier2;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(ga);
                Debug.Log($"[Reimport] GiftAnimationUI tier2 重新绑定成功");
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[Reimport] 场景已保存");
    }
}
