using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using DrscfZ.UI;

namespace DrscfZ.EditorTools
{
    /// <summary>
    /// audit-r6 场景 Setup 脚本 — 执行 P0/P1 级场景修复：
    ///   P0-A1 激活 RankingSystem GameObject（被误关导致 Instance=null，§9/§13.0/§17.6 全断）
    ///   P0-F3 跑 SetupSection36UI（已加 3 个新 feature 按钮 mapping）
    ///   P1-C1 SurvivalSettlementUI._top3Slots 自动绑定场景子节点
    ///
    /// 依赖：
    ///   - MainScene 已打开
    ///   - BroadcasterPanel / FeatureLockOverlay 已编译就绪
    ///
    /// 运行后 manage_scene save。
    /// </summary>
    public static class SetupAuditR6Scene
    {
        [MenuItem("Tools/DrscfZ/Setup Audit-r6 Scene")]
        public static void Execute()
        {
            int changed = 0;
            changed += ActivateRankingSystem();
            changed += BindSettlementTop3Slots();

            // 复用 SetupSection36UI：FeatureLockOverlay 挂载 + 占位按钮创建（3 新 mapping 已补）
            try
            {
                SetupSection36UI.Execute();
                Debug.Log("[SetupAuditR6Scene] SetupSection36UI 已执行（含 audit-r6 P0-F3 building/expedition/supporter_mode 3 按钮占位 + FeatureLockOverlay）");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SetupAuditR6Scene] SetupSection36UI 执行失败（不阻塞其他修复）：{e.Message}");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[SetupAuditR6Scene] 完成。本次 3 主项 + 场景累计变更点 {changed}+。请执行 manage_scene save 持久化。");
        }

        /// <summary>audit-r6 P0-A1: RankingSystem GO 在 MainScene 被误设 m_IsActive=0，
        /// 导致 RankingSystem.Instance 永为 null，影响 §9 矿工贡献、§13.0 本局排行、§17.6 ResourceRankUI、
        /// 以及 §23 SurvivalSettlementUI.GetTopN(3) Top3 渲染。</summary>
        private static int ActivateRankingSystem()
        {
            int count = 0;
            var all = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in all)
            {
                if (go == null) continue;
                if (go.name != "RankingSystem") continue;
                // 只在当前场景中找（忽略 Prefab/非场景对象）
                if (string.IsNullOrEmpty(go.scene.name)) continue;
                if (!go.activeSelf)
                {
                    go.SetActive(true);
                    EditorUtility.SetDirty(go);
                    Debug.Log($"[SetupAuditR6Scene] P0-A1 已激活 RankingSystem GO: {GetPath(go.transform)}（原 m_IsActive=0）");
                    count++;
                }
                else
                {
                    Debug.Log("[SetupAuditR6Scene] P0-A1 RankingSystem 已是 active，跳过");
                }
            }
            if (count == 0)
            {
                Debug.LogWarning("[SetupAuditR6Scene] P0-A1 未在场景中找到 RankingSystem GO（可能已被删除），影响 §9/§13.0/§17.6 Top3 功能链");
            }
            return count;
        }

        /// <summary>audit-r6 P1-C1: SurvivalSettlementUI._top3Slots Inspector 未绑定，
        /// 结算面板 Top2/3 不展示。此方法尝试从场景 Hierarchy 找 Top3 子节点自动绑定。</summary>
        private static int BindSettlementTop3Slots()
        {
            var ui = Object.FindFirstObjectByType<SurvivalSettlementUI>(FindObjectsInactive.Include);
            if (ui == null)
            {
                Debug.LogWarning("[SetupAuditR6Scene] P1-C1 未找到 SurvivalSettlementUI 组件，跳过 _top3Slots 绑定");
                return 0;
            }

            var so = new SerializedObject(ui);
            var prop = so.FindProperty("_top3Slots");
            if (prop == null)
            {
                Debug.LogWarning("[SetupAuditR6Scene] P1-C1 SurvivalSettlementUI._top3Slots 字段未找到（名称可能已变）");
                return 0;
            }

            // 尝试查找 3 个 Top{1|2|3} 或 Slot{0|1|2} 子节点
            var candidates = new System.Collections.Generic.List<Transform>();
            FindChildrenByPattern(ui.transform, new[] { "Top1", "Top2", "Top3", "TopSlot1", "TopSlot2", "TopSlot3", "Slot1", "Slot2", "Slot3", "Rank1", "Rank2", "Rank3", "Top3Slot0", "Top3Slot1", "Top3Slot2" }, candidates);

            // 去重、排序
            var seen = new System.Collections.Generic.HashSet<int>();
            var unique = new System.Collections.Generic.List<Transform>();
            foreach (var t in candidates)
            {
                if (t == null) continue;
                if (seen.Add(t.GetInstanceID())) unique.Add(t);
            }

            int bound = 0;
            prop.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                var elem = prop.GetArrayElementAtIndex(i);
                if (i < unique.Count)
                {
                    elem.objectReferenceValue = unique[i].gameObject;
                    Debug.Log($"[SetupAuditR6Scene] P1-C1 _top3Slots[{i}] = {GetPath(unique[i])}");
                    bound++;
                }
                else if (elem.objectReferenceValue == null)
                {
                    // 找不到 → 不强制占位（结算面板会跳过 SetActive，原有 warning 仍生效）
                    Debug.LogWarning($"[SetupAuditR6Scene] P1-C1 _top3Slots[{i}] 未找到匹配子节点；结算仅 MVP 显示（Top2/3 跳过）");
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);

            Debug.Log($"[SetupAuditR6Scene] P1-C1 _top3Slots 绑定 {bound}/3 完成（缺失槽位不阻塞，MVP 横幅仍会渲染）");
            return bound > 0 ? 1 : 0;
        }

        private static void FindChildrenByPattern(Transform root, string[] names, System.Collections.Generic.List<Transform> results)
        {
            foreach (Transform child in root)
            {
                foreach (var n in names)
                {
                    if (string.Equals(child.name, n, System.StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(child);
                        break;
                    }
                }
                FindChildrenByPattern(child, names, results);
            }
        }

        private static string GetPath(Transform t)
        {
            if (t == null) return "<null>";
            if (t.parent == null) return t.name;
            return GetPath(t.parent) + "/" + t.name;
        }
    }
}
