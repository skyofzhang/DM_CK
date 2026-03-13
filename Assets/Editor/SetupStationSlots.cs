using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DrscfZ.Survival;

namespace DrscfZ.Editor
{
    /// <summary>
    /// T003：Worker 工位槽位初始化工具
    ///
    /// 菜单：Tools → DrscfZ → Setup Station Slots (Default)
    ///
    /// 功能：
    ///   找到场景中的 WorkerManager，调用 AutoPopulateDefaultSlots() 填入
    ///   5种工作类型 × 4个槽 = 20个默认工位坐标，保存场景。
    ///
    /// 设计师可在 Inspector 中对 WorkerManager._stationSlots 任意调整坐标，
    /// 无需重新运行本脚本。Scene Gizmo 会实时显示槽位球形标记。
    /// </summary>
    public static class SetupStationSlots
    {
        [MenuItem("Tools/DrscfZ/Setup Station Slots (Default 5×4=20)")]
        public static void Execute()
        {
            // 查找场景中的 WorkerManager（包含 inactive）
            WorkerManager wm = null;
            var allObjects = Resources.FindObjectsOfTypeAll<WorkerManager>();
            foreach (var obj in allObjects)
            {
                if (obj.gameObject.scene.IsValid())
                {
                    wm = obj;
                    break;
                }
            }

            if (wm == null)
            {
                Debug.LogError("[SetupStationSlots] 未找到 WorkerManager，请确保场景中存在该组件");
                return;
            }

            // 执行填充
            wm.AutoPopulateDefaultSlots();
            EditorUtility.SetDirty(wm);

            // 保存场景（Rule #8）
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            string msg = "已填充 20 个默认工位槽位：\n" +
                         "• 鱼塘 (cmd=1) × 4 槽 @ (-6,0,12) ±0.8\n" +
                         "• 煤矿 (cmd=2) × 4 槽 @ (-8,0,16) ±0.8\n" +
                         "• 矿山 (cmd=3) × 4 槽 @ (7,0,16)  ±0.8\n" +
                         "• 炉灶 (cmd=4) × 4 槽 @ (3,0,3)   ±0.8\n" +
                         "• 城门 (cmd=6) × 4 槽 @ (0,0,-4)  ±0.8\n\n" +
                         "场景已保存。可在 Inspector 中调整坐标，Scene 视图有 Gizmo 预览。";
            Debug.Log($"[SetupStationSlots] ✅ {msg}");
        }

        // =====================================================================
        // 验证菜单：列出所有槽位当前状态（调试用）
        // =====================================================================

        [MenuItem("Tools/DrscfZ/Debug/Print Station Slots")]
        public static void PrintSlots()
        {
            WorkerManager wm = Object.FindObjectOfType<WorkerManager>(true);
            if (wm == null) { Debug.LogWarning("[PrintSlots] WorkerManager not found"); return; }

            // 通过反射读取私有字段（仅用于调试）
            var field = typeof(WorkerManager).GetField(
                "_stationSlots",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field == null) { Debug.LogWarning("[PrintSlots] _stationSlots field not found"); return; }

            var slots = field.GetValue(wm) as StationSlot[];
            if (slots == null || slots.Length == 0)
            {
                Debug.Log("[PrintSlots] 槽位为空，请先运行 Setup Station Slots");
                return;
            }

            Debug.Log($"[PrintSlots] WorkerManager 共 {slots.Length} 个工位槽：");
            for (int i = 0; i < slots.Length; i++)
            {
                var s = slots[i];
                Debug.Log($"  [{i:D2}] cmdType={s.cmdType}  pos={s.position}  " +
                          $"occupied={s.IsOccupied} ({s.occupyingPlayerId})");
            }
        }
    }
}
