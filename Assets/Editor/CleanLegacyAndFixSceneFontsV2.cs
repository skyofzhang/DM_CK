// DrscfZ 全场景清理 V2（audit-r3 Batch D）：
//   1. 失活遗留 Orange / 旧角力 UI 节点（MainMenuPanel / LoadingScreen / PlayerListUI / RankingPanel / RightPlayerList / LeftPlayerList / SettlementPanel 旧版 等）
//   2. 若场景仍挂 GameManager 旧单例组件 → SetActive(false)（保留 GO 但功能失效）
//   3. 扫描所有 TMP_Text / TextMeshProUGUI 组件，若 fontAsset == LiberationSans SDF → 替换为 AlibabaPuHuiTi-3-85-Bold SDF
//   4. 场景保存
// 非破坏式：不删 GameObject、不删组件；仅 SetActive(false) + 换字体引用

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public static class CleanLegacyAndFixSceneFontsV2
{
    const string AlibabaFontPath = "Assets/Resources/Fonts/AlibabaPuHuiTi-3-85-Bold SDF.asset";
    const string LiberationFontGuidPrefix = "8f586378b4e144a9851e7b34d9b748ee"; // LiberationSans SDF

    // 旧角力/遗留 UI 根节点名称（保守清单）
    static readonly string[] LegacyUINodeNames = {
        "MainMenuPanel", "LoadingScreen", "PlayerListUI",
        "RankingPanel", "RightPlayerList", "LeftPlayerList",
        "UpgradeNotificationPanel", "PlayerDataPanel",
        // 旧 SettlementPanel（非 SurvivalSettlementPanel）
        "SettlementPanel"
    };

    [MenuItem("Tools/DrscfZ/Clean Legacy + Fix Scene Fonts V2")]
    public static void Execute()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("[CleanV2] Scene 无效，中止");
            return;
        }

        int orangeDisabled = 0;
        int legacyUIDisabled = 0;
        int gameManagerDisabled = 0;
        int fontReplaced = 0;
        var notFoundLegacy = new List<string>();

        // ===== 第 1 步：失活 Orange 根节点 =====
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "Orange" || root.name == "[Scene]/Orange" || root.name.StartsWith("Orange "))
            {
                if (root.activeSelf)
                {
                    Undo.RegisterCompleteObjectUndo(root, "CleanV2 Disable Orange");
                    root.SetActive(false);
                    orangeDisabled++;
                    Debug.Log($"[CleanV2] ✅ Orange 根 SetActive(false): {Path(root.transform)}");
                }
            }
        }

        // ===== 第 2 步：失活旧 UI 根节点 =====
        foreach (var targetName in LegacyUINodeNames)
        {
            var candidates = FindAllInSceneByName(scene, targetName);
            if (candidates.Count == 0)
            {
                notFoundLegacy.Add(targetName);
                continue;
            }
            foreach (var go in candidates)
            {
                // 排除 Survival 前缀的新 UI（SurvivalSettlementPanel 等）
                if (go.name.StartsWith("Survival") || go.transform.parent != null && go.transform.parent.name.StartsWith("Survival"))
                    continue;
                if (go.activeSelf)
                {
                    Undo.RegisterCompleteObjectUndo(go, "CleanV2 Disable Legacy UI");
                    go.SetActive(false);
                    legacyUIDisabled++;
                    Debug.Log($"[CleanV2] ✅ 旧 UI SetActive(false): {Path(go.transform)}");
                }
            }
        }

        // ===== 第 3 步：失活旧 GameManager GO（保留 SurvivalGameManager） =====
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "GameManager" || root.name == "OldGameManager")
            {
                // 只有当该节点不是 SurvivalGameManager 时才失活
                var hasSurvivalGM = root.GetComponent("SurvivalGameManager") != null;
                if (!hasSurvivalGM && root.activeSelf)
                {
                    Undo.RegisterCompleteObjectUndo(root, "CleanV2 Disable Old GameManager");
                    root.SetActive(false);
                    gameManagerDisabled++;
                    Debug.Log($"[CleanV2] ✅ 旧 GameManager SetActive(false): {Path(root.transform)}");
                }
            }
        }

        // ===== 第 4 步：修复场景 LiberationSans → Alibaba =====
        var alibabaFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AlibabaFontPath);
        if (alibabaFont == null)
        {
            Debug.LogError($"[CleanV2] ❌ Alibaba SDF 不存在: {AlibabaFontPath}");
        }
        else
        {
            // 扫描所有 TMP_Text（TextMeshProUGUI + TextMeshPro）
            var allTMP = Resources.FindObjectsOfTypeAll<TMP_Text>();
            foreach (var tmp in allTMP)
            {
                if (tmp == null || tmp.gameObject == null) continue;
                // 只处理本场景的（不处理 prefab asset）
                if (!tmp.gameObject.scene.IsValid() || tmp.gameObject.scene != scene) continue;

                if (tmp.font == null) continue;
                var fontPath = AssetDatabase.GetAssetPath(tmp.font);
                var fontGuid = AssetDatabase.AssetPathToGUID(fontPath);
                // 如果当前字体是 LiberationSans 或任何非 Alibaba 非 ChineseFont 的 SDF，强制改 Alibaba
                var isLiberation = fontGuid.StartsWith(LiberationFontGuidPrefix);
                var isAlibaba = tmp.font == alibabaFont;
                if (isLiberation && !isAlibaba)
                {
                    Undo.RecordObject(tmp, "CleanV2 Font Swap");
                    tmp.font = alibabaFont;
                    EditorUtility.SetDirty(tmp);
                    fontReplaced++;
                    Debug.Log($"[CleanV2] ✅ 字体替换 LiberationSans → Alibaba: {Path(tmp.transform)} ({tmp.name})");
                }
            }
        }

        // ===== 第 5 步：保存场景 =====
        if (orangeDisabled > 0 || legacyUIDisabled > 0 || gameManagerDisabled > 0 || fontReplaced > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // ===== 汇总日志 =====
        Debug.Log($"[CleanV2] ============ 清理完成 ============");
        Debug.Log($"[CleanV2] Orange 根失活: {orangeDisabled}");
        Debug.Log($"[CleanV2] 旧 UI 节点失活: {legacyUIDisabled}");
        Debug.Log($"[CleanV2] 旧 GameManager 失活: {gameManagerDisabled}");
        Debug.Log($"[CleanV2] LiberationSans 字体替换: {fontReplaced}");
        if (notFoundLegacy.Count > 0)
        {
            Debug.Log($"[CleanV2] 场景中不存在（已清理或未挂）: {string.Join(", ", notFoundLegacy)}");
        }
        Debug.Log($"[CleanV2] 场景已保存: {scene.name}");
    }

    static List<GameObject> FindAllInSceneByName(Scene scene, string targetName)
    {
        var found = new List<GameObject>();
        foreach (var root in scene.GetRootGameObjects())
        {
            FindRecursive(root.transform, targetName, found);
        }
        return found;
    }

    static void FindRecursive(Transform t, string targetName, List<GameObject> found)
    {
        if (t.name == targetName)
            found.Add(t.gameObject);
        for (int i = 0; i < t.childCount; i++)
            FindRecursive(t.GetChild(i), targetName, found);
    }

    static string Path(Transform t)
    {
        if (t == null) return "";
        var p = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            p = t.name + "/" + p;
        }
        return p;
    }
}
