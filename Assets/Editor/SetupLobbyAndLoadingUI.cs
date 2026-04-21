using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using DrscfZ.Survival;

namespace DrscfZ.Editor
{
    /// <summary>
    /// 编辑器工具：一键创建大厅(LobbyPanel) + 加载(LoadingPanel) + 游戏UI(GameUIPanel with ExitBtn)
    /// 并绑定 SurvivalIdleUI / SurvivalLoadingUI / SurvivalGameplayUI 的 SerializedField。
    /// 菜单：Tools / DrscfZ / Setup Lobby & Loading UI
    /// </summary>
    public static class SetupLobbyAndLoadingUI
    {
        [MenuItem("Tools/DrscfZ/Setup Lobby & Loading UI")]
        public static void Run()
        {
            // ---- 1. 找 Canvas ----
            var canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                Debug.LogError("[Setup] Canvas not found in scene!");
                return;
            }

            // 获取 Canvas 的 RectTransform 尺寸（用于全屏子 Panel）
            var canvasRT = canvas.GetComponent<RectTransform>();

            // ---- 2. 创建 LobbyPanel ----
            var lobbyPanel = CreateFullscreenPanel(canvas.transform, "LobbyPanel",
                new Color(0.05f, 0.08f, 0.15f, 0.92f));  // 深蓝半透明背景

            // LobbyPanel 子元素
            var titleGO     = CreateTMPText(lobbyPanel.transform, "TitleText",     "极地生存法则",
                                            new Vector2(0, 180),  new Vector2(600, 80),  40, Color.white);
            var serverStat  = CreateTMPText(lobbyPanel.transform, "ServerStatus",  "已连接 ✓",
                                            new Vector2(0, 120),  new Vector2(400, 40),  22, new Color(0.4f, 1f, 0.5f));
            var statusTxt   = CreateTMPText(lobbyPanel.transform, "StatusText",    "等待主播开始游戏...",
                                            new Vector2(0, 70),   new Vector2(500, 40),  20, new Color(0.8f, 0.8f, 0.8f));

            var startBtn    = CreateButton(lobbyPanel.transform, "StartBtn",    "开始玩法",
                                           new Vector2(0, 0),     new Vector2(260, 64),  28, Color.white,
                                           new Color(0.2f, 0.6f, 1f));
            var rankingBtn  = CreateButton(lobbyPanel.transform, "RankingBtn",  "排行榜",
                                           new Vector2(-140, -80), new Vector2(220, 50), 22, Color.white,
                                           new Color(0.15f, 0.4f, 0.7f));
            var settingsBtn = CreateButton(lobbyPanel.transform, "SettingsBtn", "设置",
                                           new Vector2(140, -80),  new Vector2(220, 50), 22, Color.white,
                                           new Color(0.25f, 0.25f, 0.35f));

            // 默认关闭 LobbyPanel（连接前不显示）
            lobbyPanel.SetActive(false);

            // ---- 3. 创建 LoadingPanel ----
            var loadingPanel = CreateFullscreenPanel(canvas.transform, "LoadingPanel",
                new Color(0f, 0f, 0f, 0.75f));  // 黑色半透明遮罩

            var loadingTxt = CreateTMPText(loadingPanel.transform, "LoadingText", "准备进入战场...",
                                           new Vector2(0, 30), new Vector2(600, 60), 32, Color.white);

            // Spinner（复用 ConnectPanel Spinner 样式）
            var spinnerGO = new GameObject("Spinner");
            spinnerGO.transform.SetParent(loadingPanel.transform, false);
            var spinnerImg = spinnerGO.AddComponent<Image>();
            spinnerImg.color = new Color(0.3f, 0.7f, 1f, 0.9f);
            var spinnerRT = spinnerGO.GetComponent<RectTransform>();
            spinnerRT.anchoredPosition = new Vector2(0, -40);
            spinnerRT.sizeDelta = new Vector2(60, 60);
            // 尝试找 ConnectPanel 的 Spinner 复用图片
            var connectSpinner = canvas.transform.Find("ConnectPanel/Spinner");
            if (connectSpinner != null)
            {
                var srcImg = connectSpinner.GetComponent<Image>();
                if (srcImg != null) spinnerImg.sprite = srcImg.sprite;
            }

            // 默认关闭 LoadingPanel
            loadingPanel.SetActive(false);

            // ---- 4. 创建 GameUIPanel（带 ExitBtn）----
            var gameUIPanel = GetOrCreate(canvas.transform, "GameUIPanel");
            var gameUIPanelRT = EnsureRectTransform(gameUIPanel);
            gameUIPanelRT.anchorMin = Vector2.zero;
            gameUIPanelRT.anchorMax = Vector2.one;
            gameUIPanelRT.offsetMin = Vector2.zero;
            gameUIPanelRT.offsetMax = Vector2.zero;

            // ExitBtn —— 右上角小按钮
            var exitBtn = CreateButton(gameUIPanel.transform, "ExitBtn", "← 退出大厅",
                                       new Vector2(-80, -40), new Vector2(160, 44), 18, Color.white,
                                       new Color(0.7f, 0.2f, 0.2f));
            var exitBtnRT = exitBtn.GetComponent<RectTransform>();
            exitBtnRT.anchorMin = new Vector2(1, 1);
            exitBtnRT.anchorMax = new Vector2(1, 1);
            exitBtnRT.pivot     = new Vector2(1, 1);
            exitBtnRT.anchoredPosition = new Vector2(-20, -20);
            exitBtnRT.sizeDelta = new Vector2(160, 44);

            // 绑定 ExitBtn onClick → SurvivalGameManager.RequestExitToLobby
            // （通过脚本组件绑定，运行时调用）
            var exitBtnComp = exitBtn.GetComponent<Button>();

            // 默认关闭 GameUIPanel（等 Running 状态才显示）
            gameUIPanel.SetActive(false);

            // ---- 5. 设置 sibling index（层级顺序）----
            // LobbyPanel 在 ConnectPanel 下方（较低）
            var connectPanel = canvas.transform.Find("ConnectPanel");
            if (connectPanel != null)
            {
                int connectIdx = connectPanel.GetSiblingIndex();
                lobbyPanel.transform.SetSiblingIndex(connectIdx);      // LobbyPanel 在 ConnectPanel 位置
                loadingPanel.transform.SetSiblingIndex(connectIdx + 1); // LoadingPanel 在 LobbyPanel 之上
                // ConnectPanel 已是顶层（最高 sibling），保持原位
            }

            // ---- 6. 添加 SurvivalLoadingUI 到 Canvas ----
            var loadingUI = canvas.GetComponent<DrscfZ.UI.SurvivalLoadingUI>();
            if (loadingUI == null)
                loadingUI = canvas.AddComponent<DrscfZ.UI.SurvivalLoadingUI>();

            // ---- 7. 绑定 SurvivalIdleUI 字段 ----
            var idleUI = canvas.GetComponent<DrscfZ.UI.SurvivalIdleUI>();
            if (idleUI != null)
            {
                var soIdle = new SerializedObject(idleUI);
                soIdle.FindProperty("_panel")       .objectReferenceValue = lobbyPanel;
                soIdle.FindProperty("_startBtn")    .objectReferenceValue = startBtn.GetComponent<Button>();
                soIdle.FindProperty("_rankingBtn")  .objectReferenceValue = rankingBtn.GetComponent<Button>();
                soIdle.FindProperty("_settingsBtn") .objectReferenceValue = settingsBtn.GetComponent<Button>();
                soIdle.FindProperty("_statusText")  .objectReferenceValue = statusTxt.GetComponent<TMP_Text>();
                soIdle.FindProperty("_serverStatus").objectReferenceValue = serverStat.GetComponent<TMP_Text>();
                soIdle.FindProperty("_titleText")   .objectReferenceValue = titleGO.GetComponent<TMP_Text>();
                soIdle.ApplyModifiedProperties();
                Debug.Log("[Setup] ✅ SurvivalIdleUI 字段绑定完成");
            }
            else
            {
                Debug.LogWarning("[Setup] SurvivalIdleUI not found on Canvas");
            }

            // ---- 8. 绑定 SurvivalLoadingUI 字段 ----
            if (loadingUI != null)
            {
                var soLoading = new SerializedObject(loadingUI);
                soLoading.FindProperty("_panel")      .objectReferenceValue = loadingPanel;
                soLoading.FindProperty("_loadingText").objectReferenceValue = loadingTxt.GetComponent<TMP_Text>();
                soLoading.FindProperty("_spinner")    .objectReferenceValue = spinnerImg;
                soLoading.ApplyModifiedProperties();
                Debug.Log("[Setup] ✅ SurvivalLoadingUI 字段绑定完成");
            }

            // ---- 9. 绑定 SurvivalGameplayUI 字段 ----
            var gameplayUI = canvas.GetComponent<DrscfZ.UI.SurvivalGameplayUI>();
            if (gameplayUI != null)
            {
                var soGameplay = new SerializedObject(gameplayUI);
                var gamePanelProp = soGameplay.FindProperty("_gameUIPanel");
                if (gamePanelProp != null)
                {
                    gamePanelProp.objectReferenceValue = gameUIPanel;
                    soGameplay.ApplyModifiedProperties();
                    Debug.Log("[Setup] ✅ SurvivalGameplayUI._gameUIPanel 绑定完成");
                }
            }

            // ---- 10. 保存场景（Rule #8）----
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            Debug.Log("[Setup] ✅ 大厅 + Loading UI 搭建完成！请检查 Inspector 确认字段绑定。");
        }

        // ==================== 辅助方法 ====================

        private static GameObject CreateFullscreenPanel(Transform parent, string name, Color bgColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return go;
        }

        private static GameObject GetOrCreate(Transform parent, string name)
        {
            var found = parent.Find(name);
            if (found != null) return found.gameObject;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static RectTransform EnsureRectTransform(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            return rt;
        }

        private static GameObject CreateTMPText(Transform parent, string name, string text,
            Vector2 anchoredPos, Vector2 size, float fontSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            return go;
        }

        private static GameObject CreateButton(Transform parent, string name, string label,
            Vector2 anchoredPos, Vector2 size, float fontSize, Color textColor, Color btnColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = btnColor;

            var btn = go.AddComponent<Button>();
            var cb = btn.colors;
            cb.normalColor   = btnColor;
            cb.highlightedColor = btnColor * 1.2f;
            cb.pressedColor  = btnColor * 0.8f;
            btn.colors = cb;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = fontSize;
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.Center;
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;

            return go;
        }
    }
}
