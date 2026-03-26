using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using DrscfZ.UI;

/// <summary>
/// 一键创建 ConnectPanel（连接界面）和 StartGamePanel（等待开始界面），
/// 并在 Canvas 上挂载 SurvivalConnectUI / SurvivalIdleUI，绑定所有 SerializedField。
/// 菜单：Tools/Phase2/Setup Connect & Start UI
/// </summary>
public class SetupConnectAndStartUI
{
    [MenuItem("Tools/Phase2/Setup Connect & Start UI")]
    public static void Execute()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[SetupConnectAndStartUI] Canvas not found in scene.");
            return;
        }

        // ── 清理旧组件（避免重复挂载）────────────────────────────
        var oldConnectUI = canvas.GetComponent<SurvivalConnectUI>();
        if (oldConnectUI != null) UnityEngine.Object.DestroyImmediate(oldConnectUI);

        var oldIdleUI = canvas.GetComponent<SurvivalIdleUI>();
        if (oldIdleUI != null) UnityEngine.Object.DestroyImmediate(oldIdleUI);

        // ── 清理旧面板 ────────────────────────────────────────────
        var oldConnect = canvas.transform.Find("ConnectPanel");
        if (oldConnect != null) UnityEngine.Object.DestroyImmediate(oldConnect.gameObject);

        var oldStart = canvas.transform.Find("StartGamePanel");
        if (oldStart != null) UnityEngine.Object.DestroyImmediate(oldStart.gameObject);

        // ── 创建面板 ──────────────────────────────────────────────
        var connectPanel   = CreateConnectPanel(canvas.transform);
        var startGamePanel = CreateStartGamePanel(canvas.transform);

        // ConnectPanel 放到最顶层渲染（最高 sibling index）
        connectPanel.transform.SetAsLastSibling();

        // ── 添加脚本组件到 Canvas ─────────────────────────────────
        var connectUI = canvas.AddComponent<SurvivalConnectUI>();
        var idleUI    = canvas.AddComponent<SurvivalIdleUI>();

        // ── 绑定 SurvivalConnectUI 字段 ───────────────────────────
        var connectSo = new SerializedObject(connectUI);
        connectSo.FindProperty("_panel")     .objectReferenceValue = connectPanel;
        connectSo.FindProperty("_statusText").objectReferenceValue =
            connectPanel.transform.Find("StatusText")?.GetComponent<TMP_Text>();
        connectSo.FindProperty("_dotText")   .objectReferenceValue =
            connectPanel.transform.Find("DotText")?.GetComponent<TMP_Text>();
        connectSo.FindProperty("_spinner")   .objectReferenceValue =
            connectPanel.transform.Find("Spinner")?.GetComponent<Image>();
        connectSo.FindProperty("_retryBtn")  .objectReferenceValue =
            connectPanel.transform.Find("RetryButton")?.GetComponent<Button>();
        connectSo.ApplyModifiedProperties();
        EditorUtility.SetDirty(connectUI);

        // ── 绑定 SurvivalIdleUI 字段 ──────────────────────────────
        var idleSo = new SerializedObject(idleUI);
        idleSo.FindProperty("_panel")    .objectReferenceValue = startGamePanel;
        idleSo.FindProperty("_startBtn") .objectReferenceValue =
            startGamePanel.transform.Find("StartButton")?.GetComponent<Button>();
        idleSo.FindProperty("_statusText").objectReferenceValue =
            startGamePanel.transform.Find("StatusText")?.GetComponent<TMP_Text>();
        idleSo.ApplyModifiedProperties();
        EditorUtility.SetDirty(idleUI);

        // ── 保存场景 ──────────────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[SetupConnectAndStartUI] ✅ ConnectPanel & StartGamePanel 创建完成，字段绑定成功，场景已保存。");
    }

    // ═══════════════════════════════════════════════════════════════════
    // ConnectPanel 构建
    // ═══════════════════════════════════════════════════════════════════

    private static GameObject CreateConnectPanel(Transform parent)
    {
        // ── 根节点（全屏遮罩）──────────────────────────────────────
        var go = new GameObject("ConnectPanel");
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        SetStretch(rt);

        // 深蓝色背景 Image  #0A1628 alpha=220/255
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.039f, 0.086f, 0.094f, 0.863f);
        bg.raycastTarget = true; // 阻止点击穿透

        // ── 标题 ────────────────────────────────────────────────────
        var title = CreateTMP(go.transform, "TitleText", "极地生存法则",
                              48, FontStyles.Bold, Color.white);
        SetPivotCenter(title.GetComponent<RectTransform>(),
                       new Vector2(0, 80), new Vector2(700, 90));

        // ── 状态文字 ────────────────────────────────────────────────
        var status = CreateTMP(go.transform, "StatusText", "正在连接服务器",
                               24, FontStyles.Normal, new Color(0.8f, 0.8f, 0.8f, 1f));
        SetPivotCenter(status.GetComponent<RectTransform>(),
                       new Vector2(0, 10), new Vector2(500, 50));

        // ── 动点文字 ────────────────────────────────────────────────
        var dot = CreateTMP(go.transform, "DotText", "...",
                            24, FontStyles.Normal, new Color(0.75f, 0.75f, 0.75f, 1f));
        SetPivotCenter(dot.GetComponent<RectTransform>(),
                       new Vector2(0, -30), new Vector2(120, 40));

        // ── Spinner（白色方块，运行时旋转）──────────────────────────
        var spinnerGO = new GameObject("Spinner");
        spinnerGO.transform.SetParent(go.transform, false);
        var spinnerRT  = spinnerGO.AddComponent<RectTransform>();
        SetPivotCenter(spinnerRT, new Vector2(0, -90), new Vector2(56, 56));
        var spinnerImg = spinnerGO.AddComponent<Image>();
        spinnerImg.color = new Color(1f, 1f, 1f, 0.7f);

        // ── 重试按钮（连接失败时显示）─────────────────────────────
        var retryGO = new GameObject("RetryButton");
        retryGO.transform.SetParent(go.transform, false);
        var retryRT  = retryGO.AddComponent<RectTransform>();
        SetPivotCenter(retryRT, new Vector2(0, -170), new Vector2(200, 60));

        var retryBg = retryGO.AddComponent<Image>();
        retryBg.color = new Color(0.76f, 0.1f, 0.1f, 0.85f); // 红色

        var retryBtn    = retryGO.AddComponent<Button>();
        var retryColors = retryBtn.colors;
        retryColors.highlightedColor = new Color(0.9f, 0.2f, 0.2f, 1f);
        retryColors.pressedColor     = new Color(0.55f, 0.05f, 0.05f, 1f);
        retryBtn.colors = retryColors;

        // 重试按钮文字
        var retryText = CreateTMP(retryGO.transform, "Text", "重 试",
                                  22, FontStyles.Bold, Color.white);
        var retryTextRT = retryText.GetComponent<RectTransform>();
        retryTextRT.anchorMin       = Vector2.zero;
        retryTextRT.anchorMax       = Vector2.one;
        retryTextRT.anchoredPosition = Vector2.zero;
        retryTextRT.sizeDelta        = Vector2.zero;

        // 重试按钮默认隐藏（SurvivalConnectUI.Start() 会在运行时控制显隐）
        retryGO.SetActive(false);

        // 默认激活（Play Mode 启动时立即可见）
        go.SetActive(true);

        return go;
    }

    // ═══════════════════════════════════════════════════════════════════
    // StartGamePanel 构建
    // ═══════════════════════════════════════════════════════════════════

    private static GameObject CreateStartGamePanel(Transform parent)
    {
        // ── 根节点（底部居中条）────────────────────────────────────
        var go = new GameObject("StartGamePanel");
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0, 20);
        rt.sizeDelta        = new Vector2(640, 200);

        // 半透明黑色背景
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.63f);

        // ── 状态文字 ────────────────────────────────────────────────
        var status = CreateTMP(go.transform, "StatusText", "等待主播开始游戏...",
                               20, FontStyles.Normal, new Color(0.85f, 0.85f, 0.85f, 1f));
        var statusRT = status.GetComponent<RectTransform>();
        statusRT.anchorMin        = new Vector2(0.5f, 1f);
        statusRT.anchorMax        = new Vector2(0.5f, 1f);
        statusRT.pivot            = new Vector2(0.5f, 1f);
        statusRT.anchoredPosition = new Vector2(0, -18);
        statusRT.sizeDelta        = new Vector2(560, 40);

        // ── 开始游戏按钮 ────────────────────────────────────────────
        var btnGO = new GameObject("StartButton");
        btnGO.transform.SetParent(go.transform, false);
        var btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin        = new Vector2(0.5f, 0f);
        btnRT.anchorMax        = new Vector2(0.5f, 0f);
        btnRT.pivot            = new Vector2(0.5f, 0f);
        btnRT.anchoredPosition = new Vector2(0, 35);
        btnRT.sizeDelta        = new Vector2(420, 80);

        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.18f, 0.80f, 0.44f, 0.92f); // #2ECC71 绿色

        var btn       = btnGO.AddComponent<Button>();
        var btnColors = btn.colors;
        btnColors.highlightedColor = new Color(0.25f, 0.92f, 0.55f, 1f);
        btnColors.pressedColor     = new Color(0.10f, 0.60f, 0.30f, 1f);
        btn.colors = btnColors;

        // 按钮文字（不用 ▶ 因 LiberationSans SDF 不含该字符）
        var btnText = CreateTMP(btnGO.transform, "Text", "开始游戏",
                                28, FontStyles.Bold, Color.white);
        var btnTextRT = btnText.GetComponent<RectTransform>();
        btnTextRT.anchorMin        = Vector2.zero;
        btnTextRT.anchorMax        = Vector2.one;
        btnTextRT.anchoredPosition = Vector2.zero;
        btnTextRT.sizeDelta        = Vector2.zero;

        // 默认隐藏（连接+Idle 后由 SurvivalIdleUI 控制显示）
        go.SetActive(false);

        return go;
    }

    // ═══════════════════════════════════════════════════════════════════
    // 辅助方法
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>创建 TMP_Text 子对象。</summary>
    private static TextMeshProUGUI CreateTMP(Transform parent, string name, string text,
                                              int fontSize, FontStyles style, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = style;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    /// <summary>全屏拉伸（anchorMin=0,0 / anchorMax=1,1 / pos=0 / size=0）。</summary>
    private static void SetStretch(RectTransform rt)
    {
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = Vector2.zero;
    }

    /// <summary>居中锚点，设置位置和尺寸。</summary>
    private static void SetPivotCenter(RectTransform rt, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = sizeDelta;
    }
}
