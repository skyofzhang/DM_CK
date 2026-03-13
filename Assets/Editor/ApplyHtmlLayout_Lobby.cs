using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 按 01-winter-survival-idle-lobby.html 布局修改 LobbyPanel
/// 坐标系：Canvas 1080x1920，全部元素中心锚点，y正方向=上
/// 转换公式：unity_y = 960 - html_top - height/2
///           unity_x = html_left + width/2 - 540 (居中元素 = 0)
/// </summary>
public class ApplyHtmlLayout_Lobby
{
    public static void Execute()
    {
        int changed = 0;

        // ── TitleText ─────────────────────────────────────────
        // HTML: top:80px, 居中, width:920px
        // 字号88px → 高度约110px
        SetRect("Canvas/LobbyPanel/TitleText",
            anchoredPos: new Vector2(0, 960 - 80 - 55),   // y = 825
            sizeDelta:   new Vector2(920, 110));
        changed++;

        // ── StatusText（主等待文字）────────────────────────────
        // HTML: wait-panel top:500, padding:48, stats行高110, margin-top:94
        // → 等效 top ≈ 752px, 字号52px → 高约65px
        SetRect("Canvas/LobbyPanel/StatusText",
            anchoredPos: new Vector2(0, 960 - 752 - 32),  // y = 176
            sizeDelta:   new Vector2(760, 65));
        changed++;

        // ── ServerStatus（副提示/在线人数）────────────────────
        // HTML: status-sub, top ≈ 752+65+34 = 851px, 字号30px → 高38px
        SetRect("Canvas/LobbyPanel/ServerStatus",
            anchoredPos: new Vector2(0, 960 - 851 - 19),  // y = 90
            sizeDelta:   new Vector2(760, 38));
        changed++;

        // ── StartBtn（开始游戏主按钮）─────────────────────────
        // HTML: bottom:120px, width:520px, height:110px
        // top = 1920 - 120 - 110 = 1690px
        SetRect("Canvas/LobbyPanel/StartBtn",
            anchoredPos: new Vector2(0, 960 - 1690 - 55),  // y = -785
            sizeDelta:   new Vector2(520, 110));
        changed++;

        // StartBtn/Text 字号
        SetTMPFontSize("Canvas/LobbyPanel/StartBtn/Text", 48);

        // ── RankingBtn（排行榜，次级按钮，放在StartBtn上方）──
        // top ≈ 1920-280-56 = 1584px → unity_y = -652
        SetRect("Canvas/LobbyPanel/RankingBtn",
            anchoredPos: new Vector2(-175, 960 - 1580 - 30), // y = -650, x左移
            sizeDelta:   new Vector2(230, 60));
        changed++;

        // ── SettingsBtn（设置，与RankingBtn同行右侧）─────────
        SetRect("Canvas/LobbyPanel/SettingsBtn",
            anchoredPos: new Vector2(175, 960 - 1580 - 30),  // y=-650, x右移
            sizeDelta:   new Vector2(230, 60));
        changed++;

        // 保存场景
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[ApplyHtmlLayout_Lobby] ✅ 完成，共修改 {changed} 个元素");
    }

    // ── helpers ────────────────────────────────────────────────

    static void SetRect(string path, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var obj = FindInactive(path);
        if (obj == null) { Debug.LogWarning($"[Layout] 未找到: {path}"); return; }

        var rt = obj.GetComponent<RectTransform>();
        if (rt == null) { Debug.LogWarning($"[Layout] 无RectTransform: {path}"); return; }

        // 确保锚点为中心（与现有设置一致）
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        EditorUtility.SetDirty(obj);
        Debug.Log($"[Layout] {path.Split('/')[^1]}: pos={anchoredPos}, size={sizeDelta}");
    }

    static void SetTMPFontSize(string path, float size)
    {
        var obj = FindInactive(path);
        if (obj == null) return;
        var tmp = obj.GetComponent<TMPro.TextMeshProUGUI>();
        if (tmp == null) return;
        tmp.fontSize = size;
        EditorUtility.SetDirty(obj);
    }

    static GameObject FindInactive(string path)
    {
        // 先尝试 Find（仅对 active 有效）
        var go = GameObject.Find(path);
        if (go != null) return go;

        // 遍历所有 Transform 找到 inactive 的
        var all = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in all)
        {
            if (BuildPath(t) == path)
                return t.gameObject;
        }
        return null;
    }

    static string BuildPath(Transform t)
    {
        if (t.parent == null) return t.name;
        return BuildPath(t.parent) + "/" + t.name;
    }
}
