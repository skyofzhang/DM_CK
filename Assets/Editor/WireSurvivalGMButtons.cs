using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using DrscfZ.UI;

/// <summary>
/// GM 工具面板（BottomBar）按钮绑定脚本。
///
/// 执行后：
///   1. 找到 Canvas/BottomBar 上的 GameControlUI
///   2. 绑定 connectButton  → Canvas/BottomBar/BtnConnect
///   3. 绑定 startButton    → Canvas/BottomBar/BtnStart
///   4. 绑定 resetButton    → Canvas/BottomBar/BtnReset
///   5. 绑定 simulateButton → Canvas/BottomBar/BtnSimulate（已绑定则跳过）
///   6. 更新各按钮的显示文字
///   7. 在 BottomBar 下创建/查找 StatusText TMP，并绑定 statusText 字段
///   8. MarkSceneDirty + SaveScene
///
/// 菜单：Tools/Phase2/Wire GM Buttons
/// </summary>
public class WireSurvivalGMButtons
{
    [MenuItem("Tools/Phase2/Wire GM Buttons")]
    public static void Execute()
    {
        // ── 1. 找到 Canvas ──────────────────────────────────────────────
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[WireGMButtons] Canvas not found in scene!");
            return;
        }

        // ── 2. 找到 BottomBar ───────────────────────────────────────────
        var bottomBarT = canvas.transform.Find("BottomBar");
        if (bottomBarT == null)
        {
            Debug.LogError("[WireGMButtons] Canvas/BottomBar not found!");
            return;
        }
        var bottomBar = bottomBarT.gameObject;

        // ── 3. 找到 GameControlUI ───────────────────────────────────────
        var gcUI = bottomBar.GetComponent<GameControlUI>();
        if (gcUI == null)
        {
            Debug.LogError("[WireGMButtons] GameControlUI component not found on BottomBar!");
            return;
        }

        var so = new SerializedObject(gcUI);

        // ── 4. 绑定 connectButton ───────────────────────────────────────
        var btnConnectT = bottomBarT.Find("BtnConnect");
        if (btnConnectT != null)
        {
            var btn = btnConnectT.GetComponent<Button>();
            if (btn != null)
            {
                so.FindProperty("connectButton").objectReferenceValue = btn;
                SetButtonLabel(btnConnectT, "GM连接");
                Debug.Log("[WireGMButtons] connectButton → BtnConnect ✅");
            }
        }
        else
            Debug.LogWarning("[WireGMButtons] BtnConnect not found in BottomBar");

        // ── 5. 绑定 startButton ─────────────────────────────────────────
        var btnStartT = bottomBarT.Find("BtnStart");
        if (btnStartT != null)
        {
            var btn = btnStartT.GetComponent<Button>();
            if (btn != null)
            {
                so.FindProperty("startButton").objectReferenceValue = btn;
                SetButtonLabel(btnStartT, "开始游戏");
                Debug.Log("[WireGMButtons] startButton → BtnStart ✅");
            }
        }
        else
            Debug.LogWarning("[WireGMButtons] BtnStart not found in BottomBar");

        // ── 6. 绑定 resetButton ─────────────────────────────────────────
        var btnResetT = bottomBarT.Find("BtnReset");
        if (btnResetT != null)
        {
            var btn = btnResetT.GetComponent<Button>();
            if (btn != null)
            {
                so.FindProperty("resetButton").objectReferenceValue = btn;
                SetButtonLabel(btnResetT, "重置Idle");
                Debug.Log("[WireGMButtons] resetButton → BtnReset ✅");
            }
        }
        else
            Debug.LogWarning("[WireGMButtons] BtnReset not found in BottomBar");

        // ── 7. 绑定 simulateButton ──────────────────────────────────────
        var btnSimT = bottomBarT.Find("BtnSimulate");
        if (btnSimT != null)
        {
            var btn = btnSimT.GetComponent<Button>();
            if (btn != null)
            {
                so.FindProperty("simulateButton").objectReferenceValue = btn;
                SetButtonLabel(btnSimT, "模拟");
                Debug.Log("[WireGMButtons] simulateButton → BtnSimulate ✅");
            }
        }
        else
            Debug.LogWarning("[WireGMButtons] BtnSimulate not found in BottomBar");

        // ── 8. 查找或创建 StatusText ────────────────────────────────────
        var statusTextGO = EnsureStatusText(bottomBarT);
        if (statusTextGO != null)
        {
            var tmp = statusTextGO.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                so.FindProperty("statusText").objectReferenceValue = tmp;
                Debug.Log("[WireGMButtons] statusText 已绑定 ✅");
            }
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(gcUI);

        // ── 9. 保存场景 ─────────────────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[WireGMButtons] GM面板按钮绑定完成！" +
                  "连接/开始/重置/模拟按钮已绑定，StatusText已创建，场景已保存。\n" +
                  "运行时左上角快速点击6次可唤出GM面板。\n" +
                  "点击[重置->Idle]让服务器回到等待状态，然后可体验完整7阶段流程。");
    }

    // ── 工具方法 ─────────────────────────────────────────────────────────

    /// <summary>设置按钮 Text/TMP 标签文字</summary>
    private static void SetButtonLabel(Transform btnT, string label)
    {
        var tmp = btnT.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = label;
            EditorUtility.SetDirty(tmp);
        }
    }

    /// <summary>在 parent 下查找或创建 StatusText TMP 文本</summary>
    private static GameObject EnsureStatusText(Transform parent)
    {
        // 先查找已存在的 StatusText
        var existing = parent.Find("StatusText");
        if (existing != null)
        {
            Debug.Log("[WireGMButtons] StatusText 已存在，复用");
            return existing.gameObject;
        }

        // 创建新的 StatusText GameObject
        var go = new GameObject("StatusText");
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        // 位于 BottomBar 底部，全宽居中
        rt.anchorMin        = new Vector2(0f,  0f);
        rt.anchorMax        = new Vector2(1f,  0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 5f);
        rt.sizeDelta        = new Vector2(0f, 28f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = "GM工具就绪";
        tmp.fontSize  = 18f;
        tmp.color     = new Color(0.8f, 0.8f, 0.8f, 1f);
        tmp.alignment = TMPro.TextAlignmentOptions.Center;

        EditorUtility.SetDirty(go);
        Debug.Log("[WireGMButtons] StatusText 已创建 ✅");
        return go;
    }
}
