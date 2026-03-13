using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using DrscfZ.UI;

namespace DrscfZ.Editor
{
    /// <summary>
    /// 修复 LobbyPanel 的文字组件：
    /// 将 create_ui_element("text") 产生的 UnityEngine.UI.Text 替换为 TextMeshProUGUI，
    /// 然后重新绑定 SurvivalIdleUI 的 _statusText / _serverStatus / _titleText。
    /// </summary>
    public static class FixLobbyTextComponents
    {
        [MenuItem("Tools/DrscfZ/Fix Lobby Text Components")]
        public static void Execute()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) { Debug.LogError("[FixText] Canvas not found"); return; }

            var lobbyPanel = canvas.transform.Find("LobbyPanel")?.gameObject;
            if (lobbyPanel == null) { Debug.LogError("[FixText] LobbyPanel not found"); return; }

            // 修复 TitleText
            FixTextGO(lobbyPanel.transform, "TitleText",    "冬日生存法则",      40, Color.white);
            // 修复 ServerStatus
            FixTextGO(lobbyPanel.transform, "ServerStatus", "已连接 ✓",         22, new Color(0.4f,1f,0.5f,1f));
            // 修复 StatusText
            FixTextGO(lobbyPanel.transform, "StatusText",   "等待主播开始游戏...", 20, new Color(0.8f,0.8f,0.8f,1f));

            // ---- 重新绑定 SurvivalIdleUI ----
            var idleUI = canvas.GetComponent<SurvivalIdleUI>();
            if (idleUI != null)
            {
                var so = new SerializedObject(idleUI);
                so.FindProperty("_statusText")  .objectReferenceValue =
                    lobbyPanel.transform.Find("StatusText")?.GetComponent<TMP_Text>();
                so.FindProperty("_serverStatus").objectReferenceValue =
                    lobbyPanel.transform.Find("ServerStatus")?.GetComponent<TMP_Text>();
                so.FindProperty("_titleText")   .objectReferenceValue =
                    lobbyPanel.transform.Find("TitleText")?.GetComponent<TMP_Text>();
                so.ApplyModifiedProperties();
                Debug.Log("[FixText] ✅ SurvivalIdleUI 文字字段重新绑定完成");
            }
            else
            {
                Debug.LogWarning("[FixText] SurvivalIdleUI not found on Canvas");
            }

            // 保存
            EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("[FixText] ✅ LobbyPanel 文字组件修复完成");
        }

        private static void FixTextGO(Transform parent, string goName, string text,
                                       float fontSize, Color color)
        {
            var go = parent.Find(goName)?.gameObject;
            if (go == null)
            {
                Debug.LogWarning($"[FixText] {goName} not found under {parent.name}");
                return;
            }

            // 移除旧 UnityEngine.UI.Text
            var oldText = go.GetComponent<UnityEngine.UI.Text>();
            if (oldText != null)
            {
                Object.DestroyImmediate(oldText);
                var oldRenderer = go.GetComponent<CanvasRenderer>();
                // CanvasRenderer 由 TMP 自行添加，不额外删除
            }

            // 添加 TextMeshProUGUI（如已有则直接更新）
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();

            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.color     = color;
            tmp.alignment = TextAlignmentOptions.Center;

            Debug.Log($"[FixText] {goName}: UI.Text → TextMeshProUGUI ✅");
        }
    }
}
