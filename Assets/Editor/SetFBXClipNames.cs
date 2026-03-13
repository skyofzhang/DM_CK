#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 修复FBX动画片段名称
/// 执行：DrscfZ/Set FBX Clip Names
/// </summary>
public class SetFBXClipNames
{
    [MenuItem("DrscfZ/Set FBX Clip Names", false, 201)]
    public static void Execute()
    {
        // nn_ainim_banyun: Bankuang_run → Carry
        RenameClipInFBX(
            "Assets/Models/juese/nn_01/nn_ainim_banyun.fbx",
            "Bankuang_run", "Carry",
            0, 43, true
        );

        // nn_ainim_lose: Sit → Lose
        RenameClipInFBX(
            "Assets/Models/juese/nn_01/nn_ainim_lose.fbx",
            "Sit", "Lose",
            0, 250, true
        );

        // 确保其他几个FBX的clip名称正确
        EnsureClipName("Assets/Models/juese/nn_01/nn_ainim_idle.fbx",   "Idle",   0, 59,  true);
        EnsureClipName("Assets/Models/juese/nn_01/nn_ainim_run.fbx",    "Run",    0, 25,  true);
        EnsureClipName("Assets/Models/juese/nn_01/nn_ainim_attack.fbx", "Attack", 0, 34,  false);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SetFBXClipNames] ===== 所有FBX动画Clip名称设置完毕 =====");
    }

    static void RenameClipInFBX(string path, string oldName, string newName, int firstFrame, int lastFrame, bool loop)
    {
        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[SetFBXClipNames] 未找到: {path}");
            return;
        }

        // 优先从已有自定义clip中找
        var clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
        {
            // 从默认clips创建自定义
            var defaultClips = importer.defaultClipAnimations;
            clips = new ModelImporterClipAnimation[defaultClips.Length];
            for (int i = 0; i < defaultClips.Length; i++)
                clips[i] = defaultClips[i];
        }

        bool found = false;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i].name == oldName || clips[i].name == newName)
            {
                clips[i].name       = newName;
                clips[i].firstFrame = firstFrame;
                clips[i].lastFrame  = lastFrame;
                clips[i].loopTime   = loop;
                found = true;
                Debug.Log($"[SetFBXClipNames] {System.IO.Path.GetFileName(path)}: '{oldName}' → '{newName}' 帧:{firstFrame}-{lastFrame} Loop:{loop}");
                break;
            }
        }

        if (!found)
        {
            // 如果找不到旧名，则用第一个clip重命名
            if (clips.Length > 0)
            {
                string prev = clips[0].name;
                clips[0].name       = newName;
                clips[0].firstFrame = firstFrame;
                clips[0].lastFrame  = lastFrame;
                clips[0].loopTime   = loop;
                Debug.Log($"[SetFBXClipNames] {System.IO.Path.GetFileName(path)}: 第0个clip'{prev}' → '{newName}' 帧:{firstFrame}-{lastFrame} Loop:{loop}");
            }
            else
            {
                Debug.LogWarning($"[SetFBXClipNames] {path} 没有可用的clip！");
                return;
            }
        }

        importer.clipAnimations = clips;
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        Debug.Log($"[SetFBXClipNames] {System.IO.Path.GetFileName(path)} 重新导入完成");
    }

    static void EnsureClipName(string path, string expectedName, int firstFrame, int lastFrame, bool loop)
    {
        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[SetFBXClipNames] 未找到: {path}");
            return;
        }

        var clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
        {
            // 用默认clips创建
            var defaultClips = importer.defaultClipAnimations;
            if (defaultClips == null || defaultClips.Length == 0) return;

            clips = new ModelImporterClipAnimation[defaultClips.Length];
            for (int i = 0; i < defaultClips.Length; i++)
                clips[i] = defaultClips[i];
        }

        bool needsSave = false;
        if (clips[0].name != expectedName)
        {
            clips[0].name       = expectedName;
            clips[0].firstFrame = firstFrame;
            clips[0].lastFrame  = lastFrame;
            clips[0].loopTime   = loop;
            needsSave = true;
        }

        if (needsSave)
        {
            importer.clipAnimations = clips;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            Debug.Log($"[SetFBXClipNames] {System.IO.Path.GetFileName(path)} 名称已修正为 '{expectedName}'");
        }
        else
        {
            Debug.Log($"[SetFBXClipNames] {System.IO.Path.GetFileName(path)} '{expectedName}' ✅ 无需修改");
        }
    }
}
#endif
