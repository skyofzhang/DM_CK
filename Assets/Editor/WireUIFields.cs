using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using DrscfZ.UI;
using DrscfZ.Survival;

namespace DrscfZ.Editor
{
    public static class WireUIFields
    {
        [MenuItem("Tools/DrscfZ/Wire UI Fields")]
        public static void Execute()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) { Debug.LogError("[Wire] Canvas not found"); return; }

            // ---------- 隐藏 LobbyPanel（初始 inactive）----------
            var lobbyPanel = canvas.transform.Find("LobbyPanel")?.gameObject;
            if (lobbyPanel != null) lobbyPanel.SetActive(false);

            // ---------- 创建 LoadingPanel（如果不存在）----------
            GameObject loadingPanel = canvas.transform.Find("LoadingPanel")?.gameObject;
            if (loadingPanel == null)
            {
                loadingPanel = new GameObject("LoadingPanel");
                loadingPanel.transform.SetParent(canvas.transform, false);
                var img = loadingPanel.AddComponent<Image>();
                img.color = new Color(0f, 0f, 0f, 0.75f);
                var rt = loadingPanel.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            }

            // LoadingPanel 子元素：LoadingText
            GameObject loadingTextGO = loadingPanel.transform.Find("LoadingText")?.gameObject;
            if (loadingTextGO == null)
            {
                loadingTextGO = new GameObject("LoadingText");
                loadingTextGO.transform.SetParent(loadingPanel.transform, false);
                var tmp = loadingTextGO.AddComponent<TextMeshProUGUI>();
                tmp.text = "准备进入战场...";
                tmp.fontSize = 32;
                tmp.color = Color.white;
                tmp.alignment = TextAlignmentOptions.Center;
                var rt2 = loadingTextGO.GetComponent<RectTransform>();
                rt2.anchorMin = new Vector2(0.5f,0.5f); rt2.anchorMax = new Vector2(0.5f,0.5f);
                rt2.anchoredPosition = new Vector2(0, 30); rt2.sizeDelta = new Vector2(600, 60);
            }

            // LoadingPanel 子元素：Spinner（复用 ConnectPanel 的 Spinner 图片）
            GameObject spinnerGO = loadingPanel.transform.Find("Spinner")?.gameObject;
            if (spinnerGO == null)
            {
                spinnerGO = new GameObject("Spinner");
                spinnerGO.transform.SetParent(loadingPanel.transform, false);
                var spinnerImg = spinnerGO.AddComponent<Image>();
                spinnerImg.color = new Color(0.3f, 0.7f, 1f, 0.9f);
                // 尝试复用 ConnectPanel/Spinner 的图片
                var connectSpinner = canvas.transform.Find("ConnectPanel/Spinner");
                if (connectSpinner != null)
                {
                    var srcImg = connectSpinner.GetComponent<Image>();
                    if (srcImg != null) spinnerImg.sprite = srcImg.sprite;
                }
                var rt3 = spinnerGO.GetComponent<RectTransform>();
                rt3.anchorMin = new Vector2(0.5f,0.5f); rt3.anchorMax = new Vector2(0.5f,0.5f);
                rt3.anchoredPosition = new Vector2(0, -40); rt3.sizeDelta = new Vector2(60, 60);
            }

            loadingPanel.SetActive(false);

            // ---------- 创建 GameUIPanel + ExitBtn（如果不存在）----------
            GameObject gameUIPanel = canvas.transform.Find("GameUIPanel")?.gameObject;
            if (gameUIPanel == null)
            {
                gameUIPanel = new GameObject("GameUIPanel");
                gameUIPanel.transform.SetParent(canvas.transform, false);
                var rt4 = gameUIPanel.AddComponent<RectTransform>();
                rt4.anchorMin = Vector2.zero; rt4.anchorMax = Vector2.one;
                rt4.offsetMin = Vector2.zero; rt4.offsetMax = Vector2.zero;
            }

            GameObject exitBtnGO = gameUIPanel.transform.Find("ExitBtn")?.gameObject;
            if (exitBtnGO == null)
            {
                exitBtnGO = new GameObject("ExitBtn");
                exitBtnGO.transform.SetParent(gameUIPanel.transform, false);
                var img2 = exitBtnGO.AddComponent<Image>();
                img2.color = new Color(0.7f, 0.2f, 0.2f, 1f);
                exitBtnGO.AddComponent<Button>();
                var rt5 = exitBtnGO.GetComponent<RectTransform>();
                rt5.anchorMin = new Vector2(1,1); rt5.anchorMax = new Vector2(1,1);
                rt5.pivot = new Vector2(1,1);
                rt5.anchoredPosition = new Vector2(-20, -20);
                rt5.sizeDelta = new Vector2(160, 44);

                var labelGO = new GameObject("Label");
                labelGO.transform.SetParent(exitBtnGO.transform, false);
                var tmp2 = labelGO.AddComponent<TextMeshProUGUI>();
                tmp2.text = "← 退出大厅";
                tmp2.fontSize = 18;
                tmp2.color = Color.white;
                tmp2.alignment = TextAlignmentOptions.Center;
                var labelRT = labelGO.GetComponent<RectTransform>();
                labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
                labelRT.offsetMin = Vector2.zero; labelRT.offsetMax = Vector2.zero;
            }

            gameUIPanel.SetActive(false);

            // ---------- 调整 sibling index ----------
            var connectPanel2 = canvas.transform.Find("ConnectPanel");
            int topIdx = connectPanel2 != null ? connectPanel2.GetSiblingIndex() : canvas.transform.childCount - 1;
            if (lobbyPanel != null)    lobbyPanel.transform.SetSiblingIndex(topIdx > 0 ? topIdx - 1 : 0);
            if (loadingPanel != null)  loadingPanel.transform.SetSiblingIndex(topIdx);
            // ConnectPanel 是最顶层，放到最后
            if (connectPanel2 != null) connectPanel2.SetSiblingIndex(canvas.transform.childCount - 1);

            // ---------- 添加 SurvivalLoadingUI 到 Canvas ----------
            var loadingUI = canvas.GetComponent<SurvivalLoadingUI>();
            if (loadingUI == null) loadingUI = canvas.AddComponent<SurvivalLoadingUI>();

            // ---------- 绑定 SurvivalIdleUI ----------
            var idleUI = canvas.GetComponent<SurvivalIdleUI>();
            if (idleUI != null && lobbyPanel != null)
            {
                var so = new SerializedObject(idleUI);
                so.FindProperty("_panel")       .objectReferenceValue = lobbyPanel;
                so.FindProperty("_startBtn")    .objectReferenceValue = lobbyPanel.transform.Find("StartBtn")?.GetComponent<Button>();
                so.FindProperty("_rankingBtn")  .objectReferenceValue = lobbyPanel.transform.Find("RankingBtn")?.GetComponent<Button>();
                so.FindProperty("_settingsBtn") .objectReferenceValue = lobbyPanel.transform.Find("SettingsBtn")?.GetComponent<Button>();
                so.FindProperty("_statusText")  .objectReferenceValue = lobbyPanel.transform.Find("StatusText")?.GetComponent<TMP_Text>();
                so.FindProperty("_serverStatus").objectReferenceValue = lobbyPanel.transform.Find("ServerStatus")?.GetComponent<TMP_Text>();
                so.FindProperty("_titleText")   .objectReferenceValue = lobbyPanel.transform.Find("TitleText")?.GetComponent<TMP_Text>();
                so.ApplyModifiedProperties();
                Debug.Log("[Wire] ✅ SurvivalIdleUI 字段绑定完成");
            }
            else Debug.LogWarning($"[Wire] SurvivalIdleUI: idleUI={idleUI}, lobbyPanel={lobbyPanel}");

            // ---------- 绑定 SurvivalLoadingUI ----------
            if (loadingUI != null)
            {
                var so2 = new SerializedObject(loadingUI);
                so2.FindProperty("_panel")      .objectReferenceValue = loadingPanel;
                so2.FindProperty("_loadingText").objectReferenceValue = loadingTextGO?.GetComponent<TMP_Text>();
                so2.FindProperty("_spinner")    .objectReferenceValue = spinnerGO?.GetComponent<Image>();
                so2.ApplyModifiedProperties();
                Debug.Log("[Wire] ✅ SurvivalLoadingUI 字段绑定完成");
            }

            // ---------- 绑定 SurvivalGameplayUI._gameUIPanel ----------
            var gameplayUI = canvas.GetComponent<SurvivalGameplayUI>();
            if (gameplayUI != null)
            {
                var so3 = new SerializedObject(gameplayUI);
                var prop = so3.FindProperty("_gameUIPanel");
                if (prop != null)
                {
                    prop.objectReferenceValue = gameUIPanel;
                    so3.ApplyModifiedProperties();
                    Debug.Log("[Wire] ✅ SurvivalGameplayUI._gameUIPanel 绑定完成");
                }
                else Debug.LogWarning("[Wire] _gameUIPanel property not found on SurvivalGameplayUI");
            }

            // ---------- ExitBtn → RequestExitToLobby ----------
            var exitBtn2 = exitBtnGO?.GetComponent<Button>();
            if (exitBtn2 != null)
            {
                // 运行时通过脚本调用，不在Editor绑定（避免跨场景引用问题）
                Debug.Log("[Wire] ExitBtn 已创建，运行时通过 SurvivalGameManager.Instance.RequestExitToLobby() 调用");
            }

            // ---------- 保存场景（Rule #8）----------
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log("[Wire] ✅ 全部 UI 搭建 + 字段绑定完成！");
        }
    }
}
