using UnityEditor;
using UnityEngine;
using DrscfZ.UI;
using DrscfZ.Core;

namespace DrscfZ.Editor
{
    /// <summary>
    /// Play Mode 模拟测试快捷菜单（仅 Editor 使用）
    /// 专门为 Coplay / AI 联调准备，无需手动操作 GM 面板
    ///
    /// 注意：必须在 Play Mode 下执行才有效（需已连接服务器）
    /// </summary>
    public static class TestSimulate
    {
        [MenuItem("Tools/DrscfZ/Test/Start Simulate (Play Mode)")]
        public static void StartSimulate()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[TestSimulate] 请先进入 Play Mode 再执行");
                return;
            }

            var net = NetworkManager.Instance;
            if (net == null || !net.IsConnected)
            {
                Debug.LogWarning("[TestSimulate] 未连接服务器，无法发送模拟指令");
                return;
            }

            long ts = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            net.SendJson($"{{\"type\":\"toggle_sim\",\"data\":{{\"enabled\":true}},\"timestamp\":{ts}}}");
            Debug.Log("[TestSimulate] OK 已发送 toggle_sim enabled=true，等待服务器推送 survival_player_joined...");
        }

        [MenuItem("Tools/DrscfZ/Test/Stop Simulate (Play Mode)")]
        public static void StopSimulate()
        {
            if (!EditorApplication.isPlaying) return;

            var net = NetworkManager.Instance;
            if (net == null || !net.IsConnected) return;

            long ts = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            net.SendJson($"{{\"type\":\"toggle_sim\",\"data\":{{\"enabled\":false}},\"timestamp\":{ts}}}");
            Debug.Log("[TestSimulate] ■ 已发送 toggle_sim enabled=false，模拟已停止");
        }

        [MenuItem("Tools/DrscfZ/Test/Check Worker Status")]
        public static void CheckWorkerStatus()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[TestSimulate] 请在 Play Mode 下执行");
                return;
            }

            var wm = DrscfZ.Survival.WorkerManager.Instance;
            if (wm == null) { Debug.LogWarning("[TestSimulate] WorkerManager 未找到"); return; }

            // 通过反射读取私有字段
            var field = typeof(DrscfZ.Survival.WorkerManager).GetField(
                "_activeWorkers",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var activeWorkers = field?.GetValue(wm) as System.Collections.Generic.List<DrscfZ.Survival.WorkerController>;
            int count = activeWorkers?.Count ?? 0;

            Debug.Log($"[TestSimulate] 当前激活 Worker 数量：{count} / 20");
            if (count > 0 && activeWorkers != null)
            {
                foreach (var w in activeWorkers)
                    Debug.Log($"  → {w.gameObject.name}  playerId={w.PlayerId}  isWorking={w.IsWorking}");
            }
        }
    }
}
