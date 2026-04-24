using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;

public class ApplyLobbyStyle
{
    [MenuItem("Tools/DrscfZ/Apply Lobby Style")]
    public static void Execute()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("Canvas not found"); return; }

        var lobby = canvas.transform.Find("LobbyPanel");
        if (lobby == null) { Debug.LogError("LobbyPanel not found"); return; }

        var font = Resources.Load<TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF") ?? Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");

        // ── 按钮素材加载 ──
        var startSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Buttons/btn_lobby_start.png");
        var smallSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Buttons/btn_lobby_small.png");
        var panelBg = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Panels/lobby_panel_bg.png");

        // ── StartBtn：应用新按钮素材 ──
        var startBtn = lobby.Find("StartBtn");
        if (startBtn != null)
        {
            var img = startBtn.GetComponent<Image>();
            if (img != null && startSprite != null)
            {
                img.sprite = startSprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }

            // 调整按钮大小和位置
            var rt = startBtn.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(380, 90);
            rt.anchoredPosition = new Vector2(0, -750);

            // 按钮文字改为TMP
            var oldText = startBtn.Find("Text");
            if (oldText != null)
            {
                var uiText = oldText.GetComponent<UnityEngine.UI.Text>();
                if (uiText != null)
                {
                    uiText.text = "开始挑战";
                    uiText.fontSize = 36;
                    uiText.alignment = TextAnchor.MiddleCenter;
                    uiText.color = Color.white;
                }
            }

            // Button colors
            var btn = startBtn.GetComponent<Button>();
            if (btn != null)
            {
                var colors = btn.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.85f, 0.92f, 1f, 1f);
                colors.pressedColor = new Color(0.6f, 0.75f, 0.9f, 1f);
                colors.disabledColor = new Color(0.4f, 0.4f, 0.5f, 0.6f);
                btn.colors = colors;
            }
        }

        // ── RankingBtn：小按钮样式 ──
        ApplySmallButtonStyle(lobby.Find("RankingBtn"), smallSprite, "排行榜", new Vector2(-160, -880));

        // ── SettingsBtn：小按钮样式 ──
        ApplySmallButtonStyle(lobby.Find("SettingsBtn"), smallSprite, "设置", new Vector2(160, -880));

        // ── TitleText 样式调整 ──
        var titleText = lobby.Find("TitleText");
        if (titleText != null)
        {
            var tmp = titleText.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.fontSize = 28;
                SetTMPColor(tmp, new Color(0.8f, 0.85f, 0.95f, 0.8f));
            }
            var trt = titleText.GetComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(0, 550);
        }

        // ── StatusText 样式 ──
        var statusText = lobby.Find("StatusText");
        if (statusText != null)
        {
            var tmp = statusText.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.fontSize = 24;
                SetTMPColor(tmp, new Color(0.7f, 0.8f, 0.9f, 0.7f));
            }
            var srt = statusText.GetComponent<RectTransform>();
            srt.anchoredPosition = new Vector2(0, -650);
        }

        // ── ServerStatus 样式 ──
        var serverStatus = lobby.Find("ServerStatus");
        if (serverStatus != null)
        {
            var tmp = serverStatus.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.fontSize = 72;
                SetTMPColor(tmp, new Color(0.9f, 0.95f, 1f, 1f));
            }
            var ssrt = serverStatus.GetComponent<RectTransform>();
            ssrt.anchoredPosition = new Vector2(0, 450);
        }

        // ── 保存场景 ──
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ApplyLobbyStyle] 大厅界面样式已更新");
    }

    static void ApplySmallButtonStyle(Transform btnT, Sprite sprite, string text, Vector2 pos)
    {
        if (btnT == null) return;

        var img = btnT.GetComponent<Image>();
        if (img != null && sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }

        var rt = btnT.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(240, 70);
        rt.anchoredPosition = pos;

        var oldText = btnT.Find("Text");
        if (oldText != null)
        {
            var uiText = oldText.GetComponent<UnityEngine.UI.Text>();
            if (uiText != null)
            {
                uiText.text = text;
                uiText.fontSize = 28;
                uiText.alignment = TextAnchor.MiddleCenter;
                uiText.color = Color.white;
            }
        }

        var btn = btnT.GetComponent<Button>();
        if (btn != null)
        {
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.8f, 0.85f, 1f, 1f);
            colors.pressedColor = new Color(0.5f, 0.6f, 0.8f, 1f);
            btn.colors = colors;
        }
    }

    static void SetTMPColor(TMP_Text tmp, Color color)
    {
        var so = new SerializedObject(tmp);
        var p1 = so.FindProperty("m_fontColor");
        if (p1 != null) p1.colorValue = color;
        var p2 = so.FindProperty("m_fontColor32");
        if (p2 != null) p2.colorValue = color;
        so.ApplyModifiedProperties();
    }
}
