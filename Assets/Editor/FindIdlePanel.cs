using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public static class FindIdlePanel
{
    [MenuItem("Tools/DrscfZ/Find Idle Panel")]
    public static void Execute()
    {
        // 列出 Canvas 的所有直接子节点
        var allGO = Resources.FindObjectsOfTypeAll<GameObject>();
        GameObject canvas = null;
        foreach (var go in allGO)
            if (go.name == "Canvas" && go.scene.name == "MainScene") { canvas = go; break; }

        if (canvas == null) { Debug.LogError("Canvas not found"); return; }

        Debug.Log("=== Canvas direct children ===");
        foreach (Transform child in canvas.transform)
            Debug.Log($"  [{(child.gameObject.activeSelf ? "ON" : "off")}] {child.name}");

        // 找所有挂了 SurvivalIdleUI 脚本的 GameObject
        Debug.Log("=== All GameObjects with SurvivalIdleUI component ===");
        foreach (var go in allGO)
        {
            var comp = go.GetComponent("SurvivalIdleUI");
            if (comp != null)
                Debug.Log($"  SurvivalIdleUI on: {GetPath(go)} active={go.activeInHierarchy}");
        }

        // 找名字包含 Idle / Lobby / Connect 的 Canvas 子孙
        Debug.Log("=== Canvas children matching Idle/Lobby/Connect ===");
        foreach (Transform t in canvas.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name.ToLower();
            if (n.Contains("idle") || n.Contains("lobby") || n.Contains("connect") || n.Contains("btnstart") || n.Contains("btn_start"))
                Debug.Log($"  [{(t.gameObject.activeSelf ? "ON" : "off")}] {GetPath(t.gameObject)}");
        }

        // 找 Button 上有 On Click → SurvivalSettingsUI 的
        Debug.Log("=== Buttons targeting SurvivalSettingsUI ===");
        foreach (var btn in Resources.FindObjectsOfTypeAll<Button>())
        {
            for (int i = 0; i < btn.onClick.GetPersistentEventCount(); i++)
            {
                string method = btn.onClick.GetPersistentMethodName(i);
                var target = btn.onClick.GetPersistentTarget(i);
                if (target != null && (target.GetType().Name.Contains("Settings") || target.GetType().Name.Contains("Ranking")))
                    Debug.Log($"  Btn [{btn.name}] path={GetPath(btn.gameObject)} → {target.GetType().Name}.{method} | sprite={(btn.GetComponent<Image>()?.sprite?.name ?? "null")}");
            }
        }
    }

    static string GetPath(GameObject go)
    {
        string p = go.name;
        var t = go.transform.parent;
        while (t != null) { p = t.name + "/" + p; t = t.parent; }
        return p;
    }
}
