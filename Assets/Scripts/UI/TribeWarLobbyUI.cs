using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DrscfZ.Core;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §35 跨直播间攻防战 - 大厅 UI（MVP P1，客户端，Prefab 绑定由人工补）。
    ///
    /// 职责：
    ///   - OpenPanel()：显示面板 + 发送 tribe_war_room_list 拉取列表
    ///   - PopulateList(TribeWarRoomListResultData)：清空 _contentRoot 后按 rooms 生成按钮
    ///   - 每行按钮：主播名 / 天数 / 状态 / underAttack 标记 / [搞破坏] 按钮
    ///   - 点击 [搞破坏] → 发送 tribe_war_attack { targetRoomId }
    ///   - 关闭面板 / 刷新按钮
    ///
    /// 挂载（Rule #7）：挂在 Canvas（always-active）；_panel 初始 inactive。
    ///                  Prefab 绑定由人工补（_panel / _contentRoot / _rowPrefab / _btnRefresh / _btnClose / _statusText）。
    ///                  _rowPrefab 内部期望：1~2 个 TMP_Text + 1 个 Button（即攻击按钮）。
    ///
    /// 若 _panel / _contentRoot / _rowPrefab 未绑定，所有显示逻辑降级为 Debug.Log（不阻塞数据流）。
    /// </summary>
    public class TribeWarLobbyUI : MonoBehaviour
    {
        public static TribeWarLobbyUI Instance { get; private set; }

        // §17.16 audit-r11 GAP-B01：A 类阻塞 modal — 与 SurvivalSettlementUI / GateUpgradeConfirmUI 互斥
        private const string MODAL_A_ID = "tribe_war_lobby";

        // ==================== Inspector 字段（Prefab 绑定由人工） ====================

        [Header("面板根（初始 inactive）")]
        [SerializeField] private GameObject _panel;

        [Header("列表容器 / 行 Prefab（含 TMP_Text * 1~2 + Button）")]
        [SerializeField] private RectTransform _contentRoot;
        [SerializeField] private GameObject    _rowPrefab;

        [Header("顶部状态 / 按钮")]
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private Button   _btnRefresh;
        [SerializeField] private Button   _btnClose;

        // ==================== 运行时 ====================

        private readonly List<GameObject> _spawnedRows = new List<GameObject>();

        // ==================== 生命周期 ====================

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (_panel != null) _panel.SetActive(false);
        }

        private void Start()
        {
            if (_btnRefresh != null) _btnRefresh.onClick.AddListener(RequestRoomList);
            if (_btnClose   != null) _btnClose.onClick.AddListener(ClosePanel);
        }

        private void OnDestroy()
        {
            // 兜底：销毁时释放 modal 防止僵尸占位
            ModalRegistry.Release(MODAL_A_ID);
            if (Instance == this) Instance = null;
        }

        // ==================== 对外接口 ====================

        /// <summary>打开面板并请求最新房间列表。
        /// §17.16 audit-r11：申请 A 类 modal（priority=70），与 SurvivalSettlementUI(85) / GateUpgradeConfirmUI(75) 互斥。</summary>
        public void OpenPanel()
        {
            if (_panel == null)
            {
                Debug.LogWarning("[TribeWarLobbyUI] OpenPanel：_panel 未绑定（MVP 占位 Log）");
                RequestRoomList();
                return;
            }
            _panel.SetActive(true);
            if (_statusText != null) _statusText.text = "加载中...";
            ModalRegistry.Request(MODAL_A_ID, 70, () =>
            {
                if (_panel != null) _panel.SetActive(false);
            });
            RequestRoomList();
        }

        public void ClosePanel()
        {
            if (_panel != null) _panel.SetActive(false);
            ModalRegistry.Release(MODAL_A_ID);
        }

        /// <summary>SurvivalGameManager 收到 tribe_war_room_list_result 后路由到此。</summary>
        public void PopulateList(TribeWarRoomListResultData data)
        {
            ClearRows();

            if (data == null || data.rooms == null || data.rooms.Length == 0)
            {
                if (_statusText != null) _statusText.text = "暂无可攻击的直播间";
                return;
            }

            if (_statusText != null) _statusText.text = $"共 {data.rooms.Length} 个直播间";

            if (_contentRoot == null || _rowPrefab == null)
            {
                // 占位：Prefab 未绑定时仅 Log 列表
                foreach (var r in data.rooms)
                {
                    if (r == null) continue;
                    Debug.Log($"[TribeWarLobbyUI] Row 占位：roomId={r.roomId} streamer={r.streamerName} state={r.state} day={r.day} underAttack={r.underAttack} attackable={r.attackable}");
                }
                return;
            }

            foreach (var room in data.rooms)
            {
                if (room == null) continue;
                SpawnRow(room);
            }
        }

        // ==================== 渲染 ====================

        private void SpawnRow(TribeWarRoomInfo room)
        {
            var go = Instantiate(_rowPrefab, _contentRoot);
            go.SetActive(true);
            _spawnedRows.Add(go);

            // 填充文字：主播名 / 天数 / 状态 / underAttack 标记
            var texts = go.GetComponentsInChildren<TMP_Text>(true);
            string nameLine = string.IsNullOrEmpty(room.streamerName) ? "(匿名主播)" : room.streamerName;
            string infoLine = $"第{room.day}天 · {FormatState(room.state)}";
            if (room.underAttack) infoLine += " · 战斗中";

            if (texts != null && texts.Length >= 2)
            {
                texts[0].text = nameLine;
                texts[1].text = infoLine;
            }
            else if (texts != null && texts.Length == 1)
            {
                texts[0].text = $"{nameLine}   {infoLine}";
            }

            // 绑定按钮：row 上取第一个 Button 作为 [搞破坏] 按钮
            var btn = go.GetComponent<Button>();
            if (btn == null) btn = go.GetComponentInChildren<Button>(true);
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                bool canAttack = room.attackable && !room.underAttack;
                btn.interactable = canAttack;
                var snapshot = room; // closure 捕获
                btn.onClick.AddListener(() => OnAttackClicked(snapshot));
            }
        }

        private void ClearRows()
        {
            for (int i = _spawnedRows.Count - 1; i >= 0; i--)
            {
                var go = _spawnedRows[i];
                if (go != null) Destroy(go);
            }
            _spawnedRows.Clear();
        }

        private static string FormatState(string state)
        {
            if (string.IsNullOrEmpty(state)) return "—";
            switch (state)
            {
                case "day":        return "白天";
                case "night":      return "黑夜";
                case "recovery":   return "恢复期";
                case "settlement": return "结算中";
                case "idle":       return "空闲";
                case "loading":    return "加载中";
                default:           return state;
            }
        }

        // ==================== 点击 / 发送 ====================

        private void OnAttackClicked(TribeWarRoomInfo target)
        {
            if (target == null || string.IsNullOrEmpty(target.roomId))
            {
                Debug.LogWarning("[TribeWarLobbyUI] OnAttackClicked：target/roomId 为空");
                return;
            }
            SendTribeWarAttack(target.roomId);
            // MVP：不本地关面板，等服务端 tribe_war_attack_started / tribe_war_attack_failed 回推再切换状态
        }

        // ==================== 网络消息（C→S 字面量） ====================

        /// <summary>请求攻防战大厅列表（C→S：tribe_war_room_list）</summary>
        public static void RequestRoomList()
        {
            var net = NetworkManager.Instance;
            if (net == null || !net.IsConnected)
            {
                Debug.LogWarning("[TribeWarLobbyUI] RequestRoomList：NetworkManager 未连接");
                return;
            }
            net.SendJson("{\"type\":\"tribe_war_room_list\",\"data\":{}}");
            Debug.Log("[TribeWarLobbyUI] 发送 tribe_war_room_list");
        }

        /// <summary>发起攻击（C→S：tribe_war_attack { targetRoomId }）</summary>
        public static void SendTribeWarAttack(string targetRoomId)
        {
            var net = NetworkManager.Instance;
            if (net == null || !net.IsConnected)
            {
                Debug.LogWarning("[TribeWarLobbyUI] SendTribeWarAttack：NetworkManager 未连接");
                return;
            }
            long ts = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string json = $"{{\"type\":\"tribe_war_attack\",\"data\":{{\"targetRoomId\":\"{EscapeJson(targetRoomId)}\"}},\"timestamp\":{ts}}}";
            net.SendJson(json);
            Debug.Log($"[TribeWarLobbyUI] 发送 tribe_war_attack targetRoomId={targetRoomId}");
        }

        // ==================== 工具 ====================

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
