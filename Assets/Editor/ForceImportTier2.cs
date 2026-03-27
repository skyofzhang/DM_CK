using UnityEngine;
using UnityEditor;
using UnityEngine.Video;
using DrscfZ.UI;

public class ForceImportTier2
{
    [MenuItem("Tools/DrscfZ/Force Import And Bind Tier2")]
    public static void Execute()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset("Assets/Art/GiftGifs/tier2.webm",
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

        var tier2 = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Art/GiftGifs/tier2.webm");
        if (tier2 == null)
        {
            var main = AssetDatabase.LoadMainAssetAtPath("Assets/Art/GiftGifs/tier2.webm");
            Debug.LogError($"[ForceImport] tier2.webm 仍然无法加载为 VideoClip，类型={main?.GetType().Name}");
            return;
        }
        Debug.Log($"[ForceImport] tier2.webm 加载成功！");

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
                Debug.Log($"[ForceImport] GiftAnimationUI tier2 绑定成功");
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[ForceImport] 完成");
    }
}
