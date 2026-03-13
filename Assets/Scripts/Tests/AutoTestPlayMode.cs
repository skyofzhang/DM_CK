using System.Collections;
using System.Reflection;
using UnityEngine;
using DrscfZ.Core;
using DrscfZ.Survival;

namespace DrscfZ.Tests
{
    /// <summary>
    /// 自动化 PlayMode 自测脚本
    /// 进入 PlayMode 后自动执行：连接 → 发 toggle_sim → 验证 Worker 生成
    /// 仅在 UNITY_EDITOR 下生效，不影响生产包。
    /// </summary>
    public class AutoTestPlayMode : MonoBehaviour
    {
        // ---- 总开关：false = 禁用自动连接（正常游玩时保持 false，避免干扰主流程）----
        // 需要自动化测试时，可在 Inspector 或此处改为 true
        [SerializeField] private bool _autoConnectEnabled = false;

        // ---- 配置：是否等待主播手动点击"开始挑战"再触发模拟 ----
        // true（默认）：只连接并等待，不自动发送 toggle_sim；等 Waiting→Running 后再模拟
        // false：保留旧行为，连接后立即发送 toggle_sim
        [SerializeField] private bool _waitForManualStart = true;

        // ---- 监听标志 ----
        private bool _playerJoined  = false;
        private bool _workCommand   = false;
        private int  _workerCount   = 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
#if UNITY_EDITOR
            // 只在编辑器 Play Mode 自动注入
            var go = new GameObject("[AutoTest]");
            DontDestroyOnLoad(go);
            go.AddComponent<AutoTestPlayMode>();
            Debug.Log("[AUTOTEST] 自测运行器已注入");
#endif
        }

        private void Start()
        {
            // ---- 总开关检查 ----
            if (!_autoConnectEnabled)
            {
                Debug.Log("[AUTOTEST] _autoConnectEnabled=false，自动测试已禁用，跳过连接。" +
                          "如需测试，请在 Inspector 将 _autoConnectEnabled 改为 true。");
                return;
            }

            // 订阅网络消息
            var net = NetworkManager.Instance;
            if (net != null)
                net.OnMessageReceived += OnMsg;

            StartCoroutine(TestRoutine());
        }

        private void OnDestroy()
        {
            var net = NetworkManager.Instance;
            if (net != null)
                net.OnMessageReceived -= OnMsg;
        }

        private void OnMsg(string type, string data)
        {
            if (type == "survival_player_joined") { _playerJoined = true; }
            if (type == "work_command")           { _workCommand  = true; }
        }

        private IEnumerator TestRoutine()
        {
            Log("=== 自测开始 ===");

            // ── STEP 1: 等待 Manager 初始化 ──────────────────────────
            yield return new WaitForSeconds(1.5f);
            Log("STEP1 等待完成");

            // ── STEP 2: 连接服务器 ───────────────────────────────────
            var net = NetworkManager.Instance;
            if (net == null) { LogFail("STEP2 NetworkManager.Instance == null，中止"); yield break; }

            net.Connect();
            Log("STEP2 Connect() 已调用，等待连接...");

            float t = 0f;
            while (!net.IsConnected && t < 12f)
            {
                yield return new WaitForSeconds(0.5f);
                t += 0.5f;
            }

            if (!net.IsConnected)
            {
                LogFail("STEP2 连接超时（12s），请检查服务器是否启动 ws://101.34.30.65:8081");
                yield break;
            }
            Log("STEP2 已连接 ✓  IsGMMode=" + net.IsGMMode);

            // ── STEP 3: 发送 toggle_sim（若 _waitForManualStart=true 则等待主播手动开始）──
            if (_waitForManualStart)
            {
                Log("STEP3 _waitForManualStart=true，等待主播点击[开始挑战]按钮（Waiting->Running）后再触发模拟...");

                // 等待 SurvivalGameManager 进入 Running 状态（最多等 120 秒）
                var sgm = DrscfZ.Survival.SurvivalGameManager.Instance;
                float waitT = 0f;
                while (waitT < 120f)
                {
                    if (sgm != null && sgm.State == DrscfZ.Survival.SurvivalGameManager.SurvivalState.Running)
                        break;
                    yield return new WaitForSeconds(1f);
                    waitT += 1f;
                }

                if (sgm == null || sgm.State != DrscfZ.Survival.SurvivalGameManager.SurvivalState.Running)
                {
                    Log("STEP3 等待超时（120s），主播未点击开始；跳过 toggle_sim");
                    yield break;
                }
                Log("STEP3 检测到 Running 状态，游戏已由主播手动启动");
            }
            else
            {
                long ts = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                net.SendJson($"{{\"type\":\"toggle_sim\",\"data\":{{\"enabled\":true}},\"timestamp\":{ts}}}");
                Log("STEP3 toggle_sim enabled=true 已发送（_waitForManualStart=false）");
            }

            // ── STEP 4: 等待 survival_player_joined ─────────────────
            t = 0f;
            while (!_playerJoined && t < 15f)
            {
                yield return new WaitForSeconds(0.5f);
                t += 0.5f;
            }

            if (!_playerJoined)
            {
                LogFail("STEP4 15s 内未收到 survival_player_joined，服务器可能未处理 toggle_sim");
                // 继续检查 Worker 数量（可能还是 0）
            }
            else
            {
                Log("STEP4 收到 survival_player_joined ✓  (耗时 " + t.ToString("F1") + "s)");
            }

            // ── STEP 5: 检查 WorkerManager 激活数量 ─────────────────
            yield return new WaitForSeconds(1f);
            _workerCount = GetActiveWorkerCount();
            Log("STEP5 活跃 Worker 数量 = " + _workerCount);

            // ── STEP 6: 等待 work_command 并再次检查 Worker ──────────
            t = 0f;
            while (!_workCommand && t < 15f)
            {
                yield return new WaitForSeconds(0.5f);
                t += 0.5f;
            }

            if (_workCommand)
            {
                yield return new WaitForSeconds(0.5f);
                _workerCount = GetActiveWorkerCount();
                Log("STEP6 收到 work_command ✓  Worker 移动中，活跃数=" + _workerCount);
            }
            else
            {
                Log("STEP6 15s 内未收到 work_command（可能服务器没发）");
            }

            // ── STEP 7: 心跳检测（等 35s 确认不超时）───────────────
            Log("STEP7 心跳检测中（等35s，确认不断线）...");
            float heartbeatStart = Time.realtimeSinceStartup;
            yield return new WaitForSeconds(35f);
            bool stillConnected = net.IsConnected && !net.IsServerTimeout;
            if (stillConnected)
                Log("STEP7 心跳正常 ✓  35s 未断线，IsServerTimeout=" + net.IsServerTimeout);
            else
                LogFail("STEP7 心跳超时！IsConnected=" + net.IsConnected + " IsServerTimeout=" + net.IsServerTimeout);

            // ── 最终汇总 ─────────────────────────────────────────────
            Log("=== 自测结束 ===");
            Log("  连接:             " + (net.IsConnected ? "✓" : "✗"));
            Log("  player_joined:    " + (_playerJoined  ? "✓" : "✗"));
            Log("  work_command:     " + (_workCommand   ? "✓" : "✗"));
            Log("  活跃Worker数:     " + _workerCount);
            Log("  心跳不超时:       " + (stillConnected  ? "✓" : "✗"));

            bool allPass = net.IsConnected && _playerJoined && _workCommand && _workerCount > 0 && stillConnected;
            if (allPass)
                Log("======== ✅ 全部通过 ========");
            else
                LogFail("======== ❌ 存在失败项，请查看上方日志 ========");
        }

        // ── 反射读取 _activeWorkers 私有字段 ──────────────────────────
        private int GetActiveWorkerCount()
        {
            var wm = WorkerManager.Instance;
            if (wm == null) return -1;
            var field = typeof(WorkerManager).GetField("_activeWorkers",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) return -2;
            var list = field.GetValue(wm) as System.Collections.ICollection;
            return list?.Count ?? -3;
        }

        private static void Log(string msg)     => Debug.Log    ("[AUTOTEST] " + msg);
        private static void LogFail(string msg) => Debug.LogError("[AUTOTEST] " + msg);
    }
}
