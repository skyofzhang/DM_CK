using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace DrscfZ.Editor
{
    /// <summary>
    /// Tools → DrscfZ → Run All Section Setups
    ///
    /// 顺序执行 §37 / §38 / §39 的前端 UI 一键搭建：
    ///   1. SetupBuildingUI.Execute()                  — §37 BuildingStatusPanel + BuildVotePanel
    ///   2. SetupExpeditionUI.Execute()                — §38 ExpeditionMarkerPanel + TraderCaravanPanel
    ///   3. SetupShopUI.Execute()                      — §39 ShopPanel + ShopConfirmPanel + ShopItemButton prefab
    ///   4. AttachShopTabToBroadcasterPanel.Execute()  — 把 BroadcasterPanel._shopTabButton.onClick → ShopUI.OpenPanel("A")
    ///
    /// 最后 MarkSceneDirty + SaveScene（用 EditorSceneManager.SaveScene，CLAUDE.md 指定）。
    ///
    /// 禁用 EditorUtility.DisplayDialog（避免阻塞 MCP）。
    /// </summary>
    public static class RunAllSectionSetups
    {
        [MenuItem("Tools/DrscfZ/Run All Section Setups")]
        public static void Execute()
        {
            Debug.Log("[RunAllSectionSetups] 开始 §37/§38/§39 前端 UI 一键搭建 ...");

            int succeeded = 0;
            int failed = 0;

            // --- 1. §37 Building UI ---
            try
            {
                SetupBuildingUI.Execute();
                succeeded++;
                Debug.Log("[RunAllSectionSetups] ✓ §37 Building UI 完成");
            }
            catch (System.Exception e)
            {
                failed++;
                Debug.LogError($"[RunAllSectionSetups] ✗ §37 Building UI 失败: {e.Message}\n{e.StackTrace}");
            }

            // --- 2. §38 Expedition UI ---
            try
            {
                SetupExpeditionUI.Execute();
                succeeded++;
                Debug.Log("[RunAllSectionSetups] ✓ §38 Expedition UI 完成");
            }
            catch (System.Exception e)
            {
                failed++;
                Debug.LogError($"[RunAllSectionSetups] ✗ §38 Expedition UI 失败: {e.Message}\n{e.StackTrace}");
            }

            // --- 3. §39 Shop UI ---
            try
            {
                SetupShopUI.Execute();
                succeeded++;
                Debug.Log("[RunAllSectionSetups] ✓ §39 Shop UI 完成");
            }
            catch (System.Exception e)
            {
                failed++;
                Debug.LogError($"[RunAllSectionSetups] ✗ §39 Shop UI 失败: {e.Message}\n{e.StackTrace}");
            }

            // --- 4. Attach Shop Tab to BroadcasterPanel ---
            try
            {
                AttachShopTabToBroadcasterPanel.Execute();
                succeeded++;
                Debug.Log("[RunAllSectionSetups] ✓ Attach Shop Tab 完成");
            }
            catch (System.Exception e)
            {
                failed++;
                Debug.LogError($"[RunAllSectionSetups] ✗ Attach Shop Tab 失败: {e.Message}\n{e.StackTrace}");
            }

            // --- 5. 保存场景 ---
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(scene);
                bool saved = EditorSceneManager.SaveScene(scene);
                Debug.Log($"[RunAllSectionSetups] 场景保存 {(saved ? "成功" : "失败")}: {scene.path}");
            }
            else
            {
                Debug.LogWarning("[RunAllSectionSetups] 当前场景不可用，跳过保存。");
            }

            Debug.Log($"[RunAllSectionSetups] 全部完成 — 成功 {succeeded}, 失败 {failed}. " +
                      $"请继续跑 Tools → DrscfZ → Clean Legacy Orange Artifacts 扫描旧资产（手动）。");
        }
    }
}
