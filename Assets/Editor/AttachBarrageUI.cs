#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using DrscfZ.UI;
using UnityEngine.UI;

/// <summary>
/// BarrageMessageUI を Canvas に追加し、BarrageContent/ScrollRect 参照を接続する
/// 実行: DrscfZ/Attach Barrage UI
/// </summary>
public class AttachBarrageUI
{
    [MenuItem("DrscfZ/Attach Barrage UI", false, 260)]
    public static void Execute()
    {
        // Canvas を取得
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[AttachBarrageUI] Canvas が見つかりません");
            return;
        }

        // BarrageMessageUI を追加 (既存があればそのまま)
        var ui = canvas.GetComponent<BarrageMessageUI>()
               ?? canvas.AddComponent<BarrageMessageUI>();

        var so = new SerializedObject(ui);

        // barrageContent → Canvas/GameUIPanel/BarragePanel/ScrollView/Viewport/BarrageContent
        Transform barrageContentT = FindPath(
            "Canvas/GameUIPanel/BarragePanel/ScrollView/Viewport/BarrageContent");
        if (barrageContentT != null)
        {
            so.FindProperty("barrageContent").objectReferenceValue = barrageContentT as RectTransform;
            Debug.Log("[AttachBarrageUI] ✅ barrageContent 连接: BarrageContent");
        }
        else
            Debug.LogWarning("[AttachBarrageUI] ⚠ 未找到 BarrageContent");

        // scrollRect → Canvas/GameUIPanel/BarragePanel/ScrollView
        Transform scrollT = FindPath("Canvas/GameUIPanel/BarragePanel/ScrollView");
        if (scrollT != null)
        {
            var sr = scrollT.GetComponent<ScrollRect>();
            so.FindProperty("scrollRect").objectReferenceValue = sr;
            Debug.Log("[AttachBarrageUI] ✅ scrollRect 连接: ScrollView");
        }
        else
            Debug.LogWarning("[AttachBarrageUI] ⚠ 未找到 ScrollView");

        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[AttachBarrageUI] ✅ BarrageMessageUI 挂载到 Canvas 完毕");
    }

    static Transform FindPath(string path)
    {
        var parts = path.Split('/');
        Transform t = null;
        foreach (var p in parts)
        {
            if (t == null)
            {
                var go = GameObject.Find(p);
                if (go == null) return null;
                t = go.transform;
            }
            else
            {
                t = t.Find(p);
                if (t == null) return null;
            }
        }
        return t;
    }
}
#endif
