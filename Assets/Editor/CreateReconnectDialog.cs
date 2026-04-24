using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using DrscfZ.UI;

/// <summary>
/// 编辑器工具脚本：在 Canvas 下创建 ReconnectDialog 面板
/// 运行后立即删除此文件
/// </summary>
public class CreateReconnectDialog
{
    [MenuItem("Tools/Create ReconnectDialog")]
    public static void Execute()
    {
        // 1. 找到主 Canvas
        var canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null)
        {
            Debug.LogError("[CreateReconnectDialog] Canvas not found!");
            return;
        }
        var canvas = canvasObj.GetComponent<Canvas>();

        // 2. 检查是否已存在
        var existing = canvasObj.transform.Find("ReconnectDialog");
        if (existing != null)
        {
            Debug.LogWarning("[CreateReconnectDialog] ReconnectDialog already exists, removing old one first.");
            Object.DestroyImmediate(existing.gameObject);
        }

        // 3. 加载中文字体
        var font = Resources.Load<TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF") ?? Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
        if (font == null)
            Debug.LogWarning("[CreateReconnectDialog] ChineseFont SDF not found, TMP may show box characters.");

        // ============================================================
        // 4. 创建根节点（全屏半透明遮罩）
        // ============================================================
        var rootGo = new GameObject("ReconnectDialog", typeof(RectTransform), typeof(CanvasGroup));
        rootGo.transform.SetParent(canvasObj.transform, false);

        // 全屏 stretch
        var rootRect = rootGo.GetComponent<RectTransform>();
        rootRect.anchorMin    = Vector2.zero;
        rootRect.anchorMax    = Vector2.one;
        rootRect.offsetMin    = Vector2.zero;
        rootRect.offsetMax    = Vector2.zero;

        // 添加半透明遮罩 Image
        var overlayImg = rootGo.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.6f);
        overlayImg.raycastTarget = true;

        // ============================================================
        // 5. 对话框背景盒（居中，固定尺寸）
        // ============================================================
        var boxGo = new GameObject("DialogBox", typeof(RectTransform), typeof(Image));
        boxGo.transform.SetParent(rootGo.transform, false);

        var boxRect = boxGo.GetComponent<RectTransform>();
        boxRect.anchorMin        = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax        = new Vector2(0.5f, 0.5f);
        boxRect.pivot            = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta        = new Vector2(540f, 320f);
        boxRect.anchoredPosition = Vector2.zero;

        var boxImg = boxGo.GetComponent<Image>();
        boxImg.color = new Color(0.10f, 0.12f, 0.18f, 0.97f);  // 深蓝灰色背景

        // ============================================================
        // 6. 标题文字
        // ============================================================
        var titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(boxGo.transform, false);

        var titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin        = new Vector2(0f, 1f);
        titleRect.anchorMax        = new Vector2(1f, 1f);
        titleRect.pivot            = new Vector2(0.5f, 1f);
        titleRect.sizeDelta        = new Vector2(0f, 60f);
        titleRect.anchoredPosition = new Vector2(0f, -20f);

        var titleTMP = titleGo.GetComponent<TextMeshProUGUI>();
        titleTMP.text      = "检测到上一局进行中";
        titleTMP.fontSize  = 30f;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color     = new Color(1f, 0.85f, 0.3f); // 金色
        if (font != null) titleTMP.font = font;

        // ============================================================
        // 7. 描述文字
        // ============================================================
        var descGo = new GameObject("DescText", typeof(RectTransform), typeof(TextMeshProUGUI));
        descGo.transform.SetParent(boxGo.transform, false);

        var descRect = descGo.GetComponent<RectTransform>();
        descRect.anchorMin        = new Vector2(0f, 1f);
        descRect.anchorMax        = new Vector2(1f, 1f);
        descRect.pivot            = new Vector2(0.5f, 1f);
        descRect.sizeDelta        = new Vector2(-40f, 60f);
        descRect.anchoredPosition = new Vector2(0f, -100f);

        var descTMP = descGo.GetComponent<TextMeshProUGUI>();
        descTMP.text      = "发现未完成的游戏，是否继续上一局？";
        descTMP.fontSize  = 24f;
        descTMP.alignment = TextAlignmentOptions.Center;
        descTMP.color     = new Color(0.9f, 0.9f, 0.9f);
        if (font != null) descTMP.font = font;

        // ============================================================
        // 8. 按钮行容器
        // ============================================================
        var btnRowGo = new GameObject("ButtonRow", typeof(RectTransform));
        btnRowGo.transform.SetParent(boxGo.transform, false);

        var btnRowRect = btnRowGo.GetComponent<RectTransform>();
        btnRowRect.anchorMin        = new Vector2(0f, 0f);
        btnRowRect.anchorMax        = new Vector2(1f, 0f);
        btnRowRect.pivot            = new Vector2(0.5f, 0f);
        btnRowRect.sizeDelta        = new Vector2(-40f, 60f);
        btnRowRect.anchoredPosition = new Vector2(0f, 30f);

        var hLayout = btnRowGo.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing            = 20f;
        hLayout.childAlignment     = TextAnchor.MiddleCenter;
        hLayout.childForceExpandWidth  = true;
        hLayout.childForceExpandHeight = true;
        hLayout.padding = new RectOffset(0, 0, 0, 0);

        // ============================================================
        // 9. "继续上一局" 按钮（绿色）
        // ============================================================
        var reconnectBtnGo = new GameObject("ReconnectButton", typeof(RectTransform), typeof(Image), typeof(Button));
        reconnectBtnGo.transform.SetParent(btnRowGo.transform, false);

        var reconnBtnImg = reconnectBtnGo.GetComponent<Image>();
        reconnBtnImg.color = new Color(0.15f, 0.55f, 0.25f); // 深绿色

        // 按钮文字
        var reconnTextGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        reconnTextGo.transform.SetParent(reconnectBtnGo.transform, false);
        var reconnTextRect = reconnTextGo.GetComponent<RectTransform>();
        reconnTextRect.anchorMin = Vector2.zero;
        reconnTextRect.anchorMax = Vector2.one;
        reconnTextRect.offsetMin = Vector2.zero;
        reconnTextRect.offsetMax = Vector2.zero;

        var reconnTMP = reconnTextGo.GetComponent<TextMeshProUGUI>();
        reconnTMP.text      = "▶ 继续上一局";
        reconnTMP.fontSize  = 24f;
        reconnTMP.alignment = TextAlignmentOptions.Center;
        reconnTMP.color     = Color.white;
        if (font != null) reconnTMP.font = font;

        // ============================================================
        // 10. "放弃，重新开始" 按钮（红灰色）
        // ============================================================
        var newGameBtnGo = new GameObject("NewGameButton", typeof(RectTransform), typeof(Image), typeof(Button));
        newGameBtnGo.transform.SetParent(btnRowGo.transform, false);

        var newBtnImg = newGameBtnGo.GetComponent<Image>();
        newBtnImg.color = new Color(0.55f, 0.15f, 0.15f); // 深红色

        // 按钮文字
        var newTextGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        newTextGo.transform.SetParent(newGameBtnGo.transform, false);
        var newTextRect = newTextGo.GetComponent<RectTransform>();
        newTextRect.anchorMin = Vector2.zero;
        newTextRect.anchorMax = Vector2.one;
        newTextRect.offsetMin = Vector2.zero;
        newTextRect.offsetMax = Vector2.zero;

        var newTMP = newTextGo.GetComponent<TextMeshProUGUI>();
        newTMP.text      = "✖ 放弃，重新开始";
        newTMP.fontSize  = 24f;
        newTMP.alignment = TextAlignmentOptions.Center;
        newTMP.color     = Color.white;
        if (font != null) newTMP.font = font;

        // ============================================================
        // 11. 挂载 ReconnectDialog 脚本，序列化字段赋值
        // ============================================================
        var dialog = rootGo.AddComponent<ReconnectDialog>();

        // 通过反射给私有 SerializeField 赋值
        var t = typeof(ReconnectDialog);
        SetPrivateField(t, dialog, "_reconnectButton", reconnectBtnGo.GetComponent<Button>());
        SetPrivateField(t, dialog, "_newGameButton",   newGameBtnGo.GetComponent<Button>());
        SetPrivateField(t, dialog, "_titleText",       titleTMP);
        SetPrivateField(t, dialog, "_descText",        descTMP);

        // ============================================================
        // 12. 标记 dirty 并保存场景
        // ============================================================
        UnityEditor.EditorUtility.SetDirty(rootGo);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[CreateReconnectDialog] ReconnectDialog 创建完成！路径: Canvas/ReconnectDialog");
        Debug.Log("  ├─ ReconnectButton  = " + reconnectBtnGo.name);
        Debug.Log("  ├─ NewGameButton    = " + newGameBtnGo.name);
        Debug.Log("  ├─ TitleText        = " + titleGo.name);
        Debug.Log("  └─ DescText         = " + descGo.name);
    }

    private static void SetPrivateField(System.Type type, object obj, string fieldName, object value)
    {
        var field = type.GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            field.SetValue(obj, value);
        else
            Debug.LogWarning($"[CreateReconnectDialog] Field '{fieldName}' not found on {type.Name}");
    }
}
