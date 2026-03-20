using UnityEditor;

public class RefreshAudioAssets
{
    [MenuItem("Tools/DrscfZ/Refresh Audio Assets")]
    public static void Execute()
    {
        AssetDatabase.Refresh();
        UnityEngine.Debug.Log("[RefreshAudioAssets] 音频资源已重新导入。");
    }
}
