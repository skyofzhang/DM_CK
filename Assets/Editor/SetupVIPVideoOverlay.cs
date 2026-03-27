using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using DrscfZ.UI;

/// <summary>
/// 在 Canvas 根层级创建 VIPVideoOverlay（全屏 RawImage + CanvasGroup），
/// 并将引用绑定到 GiftAnimationUI._vipVideoDisplay / _vipCanvasGroup
/// </summary>
public class SetupVIPVideoOverlay
{
    [MenuItem("Tools/DrscfZ/Setup VIP Video Overlay")]
    public static void Execute()
    {
        // --- 1. 找到 Canvas ---
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[SetupVIPVideoOverlay] 找不到 Canvas！");
            return;
        }

        // --- 2. 删除旧的（如果已存在）---
        var old = canvas.transform.Find("VIPVideoOverlay");
        if (old != null)
        {
            Object.DestroyImmediate(old.gameObject);
            Debug.Log("[SetupVIPVideoOverlay] 删除旧 VIPVideoOverlay");
        }

        // --- 3. 创建 VIPVideoOverlay ---
        var overlayGO = new GameObject("VIPVideoOverlay");
        overlayGO.transform.SetParent(canvas.transform, false);

        var rt = overlayGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var cg = overlayGO.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        // 默认不激活（GiftAnimationUI 在播放时激活）
        overlayGO.SetActive(true); // 必须 active，否则协程无法运行

        // --- 4. 创建 RawImage 子节点 ---
        var rawGO = new GameObject("VideoDisplay");
        rawGO.transform.SetParent(overlayGO.transform, false);

        var rawRT = rawGO.AddComponent<RectTransform>();
        rawRT.anchorMin = Vector2.zero;
        rawRT.anchorMax = Vector2.one;
        rawRT.offsetMin = Vector2.zero;
        rawRT.offsetMax = Vector2.zero;

        var rawImage = rawGO.AddComponent<RawImage>();
        rawImage.color = Color.white;

        // --- 5. 排在 Canvas 最前（最高 sibling index = 覆盖所有其他元素）---
        overlayGO.transform.SetAsLastSibling();

        // --- 6. 绑定到 GiftAnimationUI ---
        var giftAnims = Resources.FindObjectsOfTypeAll<GiftAnimationUI>();
        if (giftAnims.Length == 0)
        {
            Debug.LogWarning("[SetupVIPVideoOverlay] 找不到 GiftAnimationUI！请手动绑定。");
        }
        else
        {
            foreach (var ga in giftAnims)
            {
                var so = new SerializedObject(ga);
                so.FindProperty("_vipVideoDisplay").objectReferenceValue  = rawImage;
                so.FindProperty("_vipCanvasGroup").objectReferenceValue   = cg;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(ga);
                Debug.Log($"[SetupVIPVideoOverlay] GiftAnimationUI ({ga.gameObject.name}) 已绑定 VIP 视频层");
            }
        }

        // --- 7. 保存场景 ---
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[SetupVIPVideoOverlay] 完成！VIPVideoOverlay 创建并绑定成功，场景已保存。");
    }
}
