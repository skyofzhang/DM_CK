using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using DrscfZ.Config;

namespace DrscfZ.Core
{
    /// <summary>
    /// 卡皮巴拉对决 — WebSocket 网络管理器
    ///
    /// 【抖音小玩法接入架构说明】
    /// 我们采用"自建服务器 + HTTP推送"模式，而非官方 LiveOpenSDK 的指令直推模式。
    ///
    /// 数据流: 观众操作 → 抖音服务器 → HTTP POST推送到我们服务器 → 服务器处理游戏逻辑 → WebSocket推到客户端
    ///
    /// 官方SDK提供了三种架构:
    ///   1. 抖音云托管 + 长连接网关（适合需要深度定制的玩法）
    ///   2. 抖音云托管 + 指令直推（追求低延迟+有服务器结算）
    ///   3. 无服务器 + 指令直推（简单玩法）
    ///
    /// 我们选择自建服务器的原因:
    ///   - 完全控制游戏逻辑（推力计算、排行、持久化）
    ///   - 多房间架构需要服务器端路由
    ///   - 不依赖抖音云基础设施
    ///
    /// 【后续新游戏注意】
    /// 建议引入官方 LiveOpenSDK 并使用 MultiPushType.HTTPWithSDK 双推模式:
    ///   - HTTP推送 → 自建服务器做游戏逻辑
    ///   - SDK侧接收 → 自动做履约ACK上报（平台即将强制要求）
    /// 详见: docs/douyin_integration_guide.md 第十九章
    ///
    /// 【连接流程】（严格顺序）
    /// 1. Awake: ParseCommandLineToken() 解析直播伴侣传入的 -token=xxx
    /// 2. Connect: DouyinInitAsync(token) → POST /api/douyin/init → 获取roomId
    /// 3. ConnectAsync: WebSocket连接(带roomId参数) → 发送join_room
    /// 4. ReceiveLoop + HeartbeatLoop 启动
    ///
    /// 【线程安全】
    /// WebSocket接收在子线程，Unity API只能在主线程调用。
    /// 用 ConcurrentQueue 将消息/回调从子线程传递到主线程 Update() 处理。
    /// 官方SDK等价方案: Sdk.DefaultSynchronizationContext
    ///
    /// 使用 System.Net.WebSockets.ClientWebSocket (Unity 2022 内置)
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        [Header("Server Config")]
        public string serverUrl = "ws://101.34.30.65:8081";
        public float heartbeatInterval = 30f;
        public float reconnectDelay = 3f;
        public int maxReconnectAttempts = 3;

        [Header("Room Config")]
        [Tooltip("房间ID，对应抖音直播间ID。留空则使用 'default'")]
        public string roomId = "";

        [Header("Douyin Token")]
        [Tooltip("抖音直播伴侣启动时传入的token（自动从命令行解析，无需手动填写）")]
        public string douyinToken = "";

        [Header("HTTP Config")]
        [Tooltip("HTTP API地址，用于调用/api/douyin/init等接口")]
        public string httpUrl = "http://101.34.30.65:8081";

        [Header("Heartbeat Timeout")]
        [Tooltip("心跳超时时间（秒），超过此时间未收到服务器心跳回应则视为断线")]
        public float heartbeatTimeout = 45f;

        public bool IsConnected { get; private set; }
        public string CurrentRoomId => string.IsNullOrEmpty(roomId) ? "default" : roomId;

        /// <summary>是否已通过抖音token初始化获取到roomId</summary>
        public bool IsDouyinInitialized { get; private set; }

        /// <summary>显式 GM 模式标识：命令行带 -gm / -gm=1 / --gm 时为 true（强显式，覆盖 token 判定）</summary>
        private bool _explicitGMMode;

        /// <summary>GM 模式判定（分层）：
        ///   1) _explicitGMMode == true  → GM 模式（强显式）
        ///   2) 否则若 douyinToken 为空   → GM 模式（兜底，本地开发/无伴侣启动）
        ///   3) 否则                    → 真实抖音直播模式
        /// join_room / UI / SurvivalGameManager 都以此属性为准。</summary>
        public bool IsGMMode => _explicitGMMode || string.IsNullOrEmpty(douyinToken);

        /// <summary>供外部只读访问是否显式 GM（便于调试日志/服务端区分"显式 GM" vs "兜底 GM"）</summary>
        public bool IsExplicitGMMode => _explicitGMMode;

        /// <summary>服务器断线标志（供UI层检查）</summary>
        public bool IsServerTimeout { get; private set; }

        public event Action OnConnected;
        public event Action<string> OnDisconnected;
        public event Action<string, string> OnMessageReceived; // type, dataJson
        public event Action<string> OnConnectFailed;
        /// <summary>心跳超时事件（服务器断线）</summary>
        public event Action OnHeartbeatTimeout;
        /// <summary>连接错误（抖音 init 失败 / WebSocket 建立失败等），UI 层据此弹重连对话框</summary>
        public event Action<string> OnConnectError;
        /// <summary>需要重连（Token 初始化失败 / 多次断线），UI 层据此弹重连确认</summary>
        public event Action<string> OnReconnectRequired;

        private ClientWebSocket _ws;
        private CancellationTokenSource _cts;
        private readonly ConcurrentQueue<(string type, string data)> _messageQueue = new();
        private readonly ConcurrentQueue<Action> _actionQueue = new();
        private int _reconnectCount;
        private bool _intentionalClose;
        private float _lastHeartbeatResponse;
        private bool _heartbeatTimeoutFired;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 从 GameConfig 加载服务器配置（覆盖 Inspector 默认值）
            var config = Resources.Load<GameConfig>("Config/GameConfig");
            if (config != null)
            {
                serverUrl = config.serverUrl;
                httpUrl = config.httpUrl;
                heartbeatInterval = config.heartbeatInterval;
                reconnectDelay = config.reconnectDelay;
                maxReconnectAttempts = config.maxReconnectAttempts;
                if (!string.IsNullOrEmpty(config.roomId))
                    roomId = config.roomId;
            }

            // 解析命令行参数中的抖音token
            // 直播伴侣启动exe时传入: -token=xxx
            ParseCommandLineToken();

            // 启动模式汇总日志（审计要求）：显式 GM / 是否有 token / 最终生效模式
            bool hasToken = !string.IsNullOrEmpty(douyinToken);
            Debug.Log($"[DrscfZ-Net] Startup flags: explicitGM={_explicitGMMode}, hasToken={hasToken}, finalMode={(IsGMMode ? "GM" : "DOUYIN")}");
            Debug.Log($"[DrscfZ-Net] IsGMMode={IsGMMode}, ExplicitGM={_explicitGMMode}");
        }

        private void Update()
        {
            // 主线程分发消息
            while (_messageQueue.TryDequeue(out var msg))
            {
                // 心跳回应：更新最后收到时间
                // SurvivalRoom.js 回 heartbeat_ack，Room.js 回 heartbeat，两种都接受
                if (msg.type == "heartbeat" || msg.type == "heartbeat_ack")
                {
                    _lastHeartbeatResponse = Time.realtimeSinceStartup;
                    IsServerTimeout = false;
                    _heartbeatTimeoutFired = false;
                }

                try { OnMessageReceived?.Invoke(msg.type, msg.data); }
                catch (Exception e) { Debug.LogError($"[Net] Handler error: {e}"); }
            }
            while (_actionQueue.TryDequeue(out var action))
            {
                try { action(); }
                catch (Exception e) { Debug.LogError($"[Net] Action error: {e}"); }
            }

            // 心跳超时检测
            if (IsConnected && !_intentionalClose && !_heartbeatTimeoutFired
                && _lastHeartbeatResponse > 0f
                && Time.realtimeSinceStartup - _lastHeartbeatResponse > heartbeatTimeout)
            {
                _heartbeatTimeoutFired = true;
                IsServerTimeout = true;
                Debug.LogError($"[Net] 心跳超时！最后回应 {Time.realtimeSinceStartup - _lastHeartbeatResponse:F1}s 前");
                OnHeartbeatTimeout?.Invoke();
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        private void OnApplicationQuit()
        {
            // 应用退出时主动通知服务器断开
            _intentionalClose = true;
            if (_ws != null && _ws.State == WebSocketState.Open)
            {
                try
                {
                    // 发送 leave_room 通知（非阻塞尝试）
                    var leaveMsg = $"{{\"type\":\"leave_room\",\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}";
                    var bytes = Encoding.UTF8.GetBytes(leaveMsg);
                    _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None).Wait(500);
                }
                catch { }
            }
            Disconnect();
        }

        /// <summary>连接失败原因（供UI显示）</summary>
        public string ConnectError { get; private set; }

        public async void Connect()
        {
            if (IsConnected) return;
            _intentionalClose = false;
            _reconnectCount = 0;
            ConnectError = null;

            // IsGMMode 已改为派生属性，这里不再赋值；_explicitGMMode / douyinToken 决定模式。
            if (IsGMMode)
            {
                // GM 模式（显式 -gm 或兜底无 token）：跳过抖音 init，直接连接服务器
                if (string.IsNullOrEmpty(roomId)) roomId = "default";
                string gmReason = _explicitGMMode ? "显式 -gm" : "无抖音token（兜底）";
                Debug.Log($"[Net] GM模式：{gmReason}，直接连接服务器 roomId={roomId}");
                await ConnectAsync();
                return;
            }

            // 真实抖音模式：用token调init接口获取roomId
            if (!IsDouyinInitialized)
            {
                Debug.Log($"[Net] Douyin token detected, calling init API...");
                bool initOk = await DouyinInitAsync(douyinToken);
                if (!initOk)
                {
                    ConnectError = "直播间初始化失败，请重新从直播伴侣启动！";
                    Debug.LogError($"[Net] {ConnectError}");
                    // 三条通道同时触发：保留原 OnConnectFailed 向下兼容；
                    // OnConnectError 给通用错误 UI；OnReconnectRequired 让 UI 弹重连对话框
                    _actionQueue.Enqueue(() =>
                    {
                        OnConnectFailed?.Invoke(ConnectError);
                        OnConnectError?.Invoke(ConnectError);
                        OnReconnectRequired?.Invoke(ConnectError);
                    });
                    return;
                }
            }

            await ConnectAsync();
        }

        public void Disconnect()
        {
            _intentionalClose = true;
            _cts?.Cancel();
            if (_ws != null && _ws.State == WebSocketState.Open)
            {
                try { _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None).Wait(2000); }
                catch { }
            }
            _ws?.Dispose();
            _ws = null;
            IsConnected = false;
        }

        public new void SendMessage(string type)
        {
            SendRaw($"{{\"type\":\"{type}\",\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}");
        }

        public void SendJson(string json)
        {
            SendRaw(json);
        }

        private async void SendRaw(string json)
        {
            if (_ws == null || _ws.State != WebSocketState.Open) return;
            try
            {
                var bytes = Encoding.UTF8.GetBytes(json);
                await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Net] Send error: {e.Message}");
            }
        }

        private async Task ConnectAsync()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            _ws?.Dispose();
            _ws = new ClientWebSocket();

            try
            {
                await _ws.ConnectAsync(new Uri(serverUrl), _cts.Token);
                IsConnected = true;
                _reconnectCount = 0;
                _lastHeartbeatResponse = Time.realtimeSinceStartup;
                _heartbeatTimeoutFired = false;
                IsServerTimeout = false;

                // 连接后立即发送 join_room（告知服务器要加入哪个房间 + 当前模式）
                // 服务端据 isGMMode 字段区分 GM 模式（显式或兜底）与真实抖音模式，用于权限/路由分流
                // 🆕 GM 模式默认 playerId="gm_host" / playerName="GM主播"；真实抖音模式留空由服务端从 roomInfo 补全
                string playerId = IsGMMode ? "gm_host" : "";
                string playerName = IsGMMode ? "GM主播" : "";
                string joinMsg = $"{{\"type\":\"join_room\",\"data\":{{\"roomId\":\"{EscapeJsonString(CurrentRoomId)}\",\"isGMMode\":{(IsGMMode ? "true" : "false")},\"explicitGMMode\":{(_explicitGMMode ? "true" : "false")},\"playerId\":\"{EscapeJsonString(playerId)}\",\"playerName\":\"{EscapeJsonString(playerName)}\"}},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}";
                var joinBytes = Encoding.UTF8.GetBytes(joinMsg);
                await _ws.SendAsync(new ArraySegment<byte>(joinBytes), WebSocketMessageType.Text, true, _cts.Token);
                Debug.Log($"[Net] Joined room: {CurrentRoomId}, isGMMode={IsGMMode}, explicitGM={_explicitGMMode}");

                _actionQueue.Enqueue(() => OnConnected?.Invoke());
                _ = ReceiveLoop();
                _ = HeartbeatLoop();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Net] Connect failed: {e.Message}");
                IsConnected = false;
                string err = $"WebSocket 连接失败: {e.Message}";
                ConnectError = err;
                // 向 UI 层派发可弹窗的错误（重试仍交给 TryReconnect 处理）
                _actionQueue.Enqueue(() => OnConnectError?.Invoke(err));
                TryReconnect();
            }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[8192];
            var sb = new StringBuilder();

            try
            {
                while (_ws.State == WebSocketState.Open && !_cts.IsCancellationRequested)
                {
                    sb.Clear();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                        sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close) break;

                    ParseAndEnqueue(sb.ToString());
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                if (!_intentionalClose)
                    Debug.LogWarning($"[Net] Receive error: {e.Message}");
            }

            IsConnected = false;
            _actionQueue.Enqueue(() => OnDisconnected?.Invoke("Connection lost"));

            if (!_intentionalClose) TryReconnect();
        }

        private void ParseAndEnqueue(string json)
        {
            try
            {
                // 提取 type
                string type = ExtractField(json, "type");
                if (string.IsNullOrEmpty(type)) return;

                // 提取 data 子对象
                string dataJson = ExtractDataJson(json);
                _messageQueue.Enqueue((type, dataJson));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Net] Parse error: {e.Message}");
            }
        }

        private string ExtractField(string json, string field)
        {
            string key = $"\"{field}\":\"";
            int idx = json.IndexOf(key);
            if (idx < 0) return null;
            int start = idx + key.Length;
            int end = json.IndexOf('"', start);
            if (end < 0) return null;
            return json.Substring(start, end - start);
        }

        private string ExtractDataJson(string json)
        {
            int idx = json.IndexOf("\"data\":");
            if (idx < 0) return "{}";

            int start = idx + 7;
            // 跳过空白
            while (start < json.Length && json[start] == ' ') start++;

            if (start >= json.Length) return "{}";

            char first = json[start];
            if (first == '{')
            {
                int depth = 0;
                for (int i = start; i < json.Length; i++)
                {
                    if (json[i] == '{') depth++;
                    else if (json[i] == '}') { depth--; if (depth == 0) return json.Substring(start, i - start + 1); }
                }
            }
            else if (first == '[')
            {
                int depth = 0;
                for (int i = start; i < json.Length; i++)
                {
                    if (json[i] == '[') depth++;
                    else if (json[i] == ']') { depth--; if (depth == 0) return json.Substring(start, i - start + 1); }
                }
            }

            return "{}";
        }

        private async Task HeartbeatLoop()
        {
            try
            {
                while (IsConnected && !_cts.IsCancellationRequested)
                {
                    await Task.Delay((int)(heartbeatInterval * 1000), _cts.Token);
                    if (IsConnected) SendMessage("heartbeat");
                }
            }
            catch (OperationCanceledException) { }
        }

        private async void TryReconnect()
        {
            if (_intentionalClose) return;
            _reconnectCount++;
            if (_reconnectCount > maxReconnectAttempts)
            {
                string msg = $"达到最大重连次数 ({maxReconnectAttempts})，需手动重连";
                Debug.LogError($"[Net] {msg}");
                // UI 层据此弹"重新连接"对话框（具体面板交由其他 Agent 实现）
                _actionQueue.Enqueue(() => OnReconnectRequired?.Invoke(msg));
                return;
            }
            Debug.LogWarning($"[Net] Reconnecting in {reconnectDelay}s... ({_reconnectCount}/{maxReconnectAttempts})");
            await Task.Delay((int)(reconnectDelay * 1000));
            if (!_intentionalClose) await ConnectAsync();
        }

        // ==================== 抖音Token初始化 ====================
        //
        // 【抖音直播伴侣启动流程】
        // 1. 主播在抖音直播伴侣中挂载小玩法
        // 2. 直播伴侣检测到小玩法后，启动exe: game.exe -token=xxxxx
        // 3. token有效期30分钟，限流10次/秒/appId
        //
        // 【官方SDK等价操作】
        // 官方SDK用 Sdk.Initialize(appId) 初始化后，通过
        // Sdk.GetRoomInfoService().WaitForRoomInfoAsync 获取直播间信息（内部自动处理token）
        // 我们手动实现了等价功能: 解析token → POST /api/douyin/init → 服务器调GetRoomInfo
        //
        // 【踩坑记录】
        // - roomId是大整数(如7606023010126089006)，超过JS Number.MAX_SAFE_INTEGER
        //   服务器端必须从原始JSON用正则提取，否则精度丢失导致task/start失败
        // - 无token时（本地开发）降级到default房间，服务器会转发数据到default房间

        /// <summary>
        /// 解析命令行参数：
        ///   - `-token=xxx` / `-token xxx`：抖音直播伴侣启动时传入的 token
        ///   - `-gm` / `-gm=1` / `--gm`：显式 GM 模式（覆盖 token 判定）
        /// 直播伴侣启动 exe 时会以命令行参数传入 token；开发本地调试用 -gm 强制 GM。
        /// 不使用 `return` 提前退出，因为 token 和 gm 可能同时出现（token 测试 + GM 强覆盖）。
        /// </summary>
        private void ParseCommandLineToken()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                // --- Token 解析 ---
                if (arg.StartsWith("-token=", StringComparison.OrdinalIgnoreCase))
                {
                    douyinToken = arg.Substring(7);
                    Debug.Log($"[Net] Douyin token from command line: {SafeTokenPrefix(douyinToken)}...");
                    continue;
                }
                if (arg.Equals("-token", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    douyinToken = args[i + 1];
                    Debug.Log($"[Net] Douyin token from command line: {SafeTokenPrefix(douyinToken)}...");
                    i++; // 跳过已消费的值
                    continue;
                }

                // --- 显式 GM 模式解析 ---
                // 支持: -gm / -gm=1 / -gm=true / --gm / --gm=1
                // -gm=0 / -gm=false 时视为显式关闭 GM（便于开发调试）
                if (arg.Equals("-gm", StringComparison.OrdinalIgnoreCase)
                    || arg.Equals("--gm", StringComparison.OrdinalIgnoreCase))
                {
                    _explicitGMMode = true;
                    Debug.Log("[Net] Explicit GM mode enabled via -gm");
                    continue;
                }
                if (arg.StartsWith("-gm=", StringComparison.OrdinalIgnoreCase)
                    || arg.StartsWith("--gm=", StringComparison.OrdinalIgnoreCase))
                {
                    int eq = arg.IndexOf('=');
                    string val = eq >= 0 ? arg.Substring(eq + 1).Trim().ToLowerInvariant() : "";
                    bool on = val == "1" || val == "true" || val == "yes" || val == "on";
                    _explicitGMMode = on;
                    Debug.Log($"[Net] Explicit GM mode set via {arg}: {_explicitGMMode}");
                    continue;
                }
            }

            if (string.IsNullOrEmpty(douyinToken) && !_explicitGMMode)
            {
                Debug.Log("[Net] No Douyin token / no -gm flag, will fall back to GM mode with default room");
            }
        }

        /// <summary>日志用截断 token，避免把完整 token 写进日志</summary>
        private static string SafeTokenPrefix(string token)
        {
            if (string.IsNullOrEmpty(token)) return "";
            return token.Substring(0, Math.Min(8, token.Length));
        }

        /// <summary>最小化 JSON 字符串转义（只处理最常见的 \ 和 "），roomId/playerName 够用。
        /// 服务端 JSON.parse 严格遵循 RFC 8259，控制字符罕见于我们的字段，故不做完整表。</summary>
        private static string EscapeJsonString(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        /// <summary>
        /// 调用服务器 /api/douyin/init 接口
        /// 发送token → 服务器调GetRoomInfo获取roomId → 服务器自动启动推送任务 → 返回roomId
        ///
        /// 服务器端流程:
        /// 1. 用 appId+appSecret 从 minigame.zijieapi.com 换取 access_token
        /// 2. 用 access_token + 直播伴侣token 调 POST webcast.bytedance.com/api/webcastmate/info
        /// 3. 获取 roomId（从原始JSON正则提取，防精度丢失）
        /// 4. 调 task/start 启动3种推送任务（comment/gift/like）
        /// 5. 返回 {success, roomId, anchorName, startedTypes}
        ///
        /// 【官方SDK等价操作】
        /// 官方SDK中等价于: Sdk.GetRoomInfoService().WaitForRoomInfoAsync
        /// + Sdk.GetMessagePushService().StartPushTaskAsync("live_comment")
        /// 但我们在服务器端完成这些操作，客户端只需发token即可
        /// </summary>
        private async Task<bool> DouyinInitAsync(string token)
        {
            try
            {
                string url = $"{httpUrl}/api/douyin/init";
                string jsonBody = $"{{\"token\":\"{token}\"}}";

                Debug.Log($"[Net] POST {url}");

                // 使用Task包装UnityWebRequest（需要在主线程发起但可以await）
                var tcs = new TaskCompletionSource<string>();

                // 需要在主线程执行UnityWebRequest
                _actionQueue.Enqueue(() =>
                {
                    StartCoroutine(DouyinInitCoroutine(url, jsonBody, tcs));
                });

                string response = await tcs.Task;

                if (string.IsNullOrEmpty(response))
                {
                    Debug.LogError("[Net] Douyin init: empty response");
                    return false;
                }

                // 简单解析JSON响应（不用JsonUtility因为字段名不匹配）
                string extractedRoomId = ExtractField(response, "roomId");
                string success = ExtractField(response, "success");

                if (!string.IsNullOrEmpty(extractedRoomId) && extractedRoomId != "null")
                {
                    roomId = extractedRoomId;
                    IsDouyinInitialized = true;
                    Debug.Log($"[Net] Douyin init success! roomId={roomId}");
                    return true;
                }

                // 检查error字段
                string error = ExtractField(response, "error");
                Debug.LogError($"[Net] Douyin init failed: {error ?? response}");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Net] Douyin init exception: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Unity协程发送HTTP POST请求
        /// </summary>
        private System.Collections.IEnumerator DouyinInitCoroutine(string url, string jsonBody, TaskCompletionSource<string> tcs)
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            var request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 15;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                tcs.TrySetResult(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"[Net] HTTP error: {request.error}, response: {request.downloadHandler?.text}");
                tcs.TrySetResult(request.downloadHandler?.text ?? "");
            }

            request.Dispose();
        }
    }
}
