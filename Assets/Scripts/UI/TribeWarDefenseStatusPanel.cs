using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;
using DrscfZ.Core;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §35 跨直播间攻防战 - 防守方状态面板（MVP P1，Prefab 绑定由人工补）。
    ///
    /// 展示：
    ///   - 当前攻击者主播名
    ///   - 已承受远征怪数 / 被偷取资源汇总
    ///   - 文字战报滚动区（最多保留 N=30 条）
    ///   - [反击] 按钮 → 发 tribe_war_retaliate { targetRoomId=攻击者 roomId }
    ///
    /// 挂载：Canvas（always-active）；_panel 初始 inactive。
    /// 若 _panel / 文本字段未绑定，所有显示逻辑降级为 Debug.Log。
    /// </summary>
    public class TribeWarDefenseStatusPanel : MonoBehaviour
    {
        public static TribeWarDefenseStatusPanel Instance { get; private set; }

        // ==================== Inspector 字段（Prefab 绑定由人工） ====================

        [Header("面板根（初始 inactive）")]
        [SerializeField] private GameObject _panel;

        [Header("头部信息")]
        [SerializeField] private TMP_Text _titleText;              // 当前攻击者主播名
        [SerializeField] private TMP_Text _expeditionCountText;    // 已承受远征怪数
        [SerializeField] private TMP_Text _stolenText;             // 被偷取资源汇总

        [Header("战报滚动区 & 按钮")]
        [SerializeField] private TMP_Text _reportText;
        [SerializeField] private Button   _btnRetaliate;

        // ==================== 运行时 ====================

        private string _sessionId;
        private string _attackerRoomId;
        private string _attackerName;
        private int    _expeditionReceived;
        private int    _stolenFood;
        private int    _stolenCoal;
        private int    _stolenOre;

        private readonly Queue<string> _reportLines = new Queue<string>();
        private const int REPORT_MAX = 30;

        // ==================== 生命周期 ====================

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            EnsureFallbackUI();
            if (_panel != null) _panel.SetActive(false);
        }

        private void Start()
        {
            if (_btnRetaliate != null) _btnRetaliate.onClick.AddListener(OnRetaliateClicked);

            // 🔴 audit-r46 GAP-M-05：订阅 room_state 重连恢复事件
            //   原 SGM case "room_state" 完全跳过 tribeWar 字段 → 防御方面板重连后永久消失
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null) sgm.OnTribeWarRestore += HandleTribeWarRestore;
        }

        private void OnDestroy()
        {
            // 🔴 audit-r46 GAP-M-05：解除订阅
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null) sgm.OnTribeWarRestore -= HandleTribeWarRestore;

            if (Instance == this) Instance = null;
        }

        // 🔴 audit-r46 GAP-M-05：断线重连时从 room_state.tribeWar 恢复防守方面板
        //   仅当 role=='defender' 时显示；InProgressData 缺 attackerStreamerName，
        //   显示 targetRoomId（实际攻击方 roomId）占位
        private void HandleTribeWarRestore(TribeWarInProgressData data)
        {
            if (data == null || data.role != "defender") return;
            _sessionId          = data.sessionId;
            _attackerRoomId     = data.targetRoomId;
            _attackerName       = !string.IsNullOrEmpty(data.targetRoomId) ? data.targetRoomId : "—";
            _expeditionReceived = data.remoteMonstersAlive;
            _stolenFood         = data.stolenResources != null ? data.stolenResources.food : 0;
            _stolenCoal         = data.stolenResources != null ? data.stolenResources.coal : 0;
            _stolenOre          = data.stolenResources != null ? data.stolenResources.ore  : 0;
            _reportLines.Clear();
            _reportLines.Enqueue("[重连恢复] 正被攻击中…");

            if (_panel == null) return;
            _panel.SetActive(true);
            if (_titleText != null) _titleText.text = $"攻击者：{_attackerName}";
            UpdateExpeditionCountUI();
            UpdateStolenUI();
            UpdateReportUI();
            if (_btnRetaliate != null) _btnRetaliate.interactable = !string.IsNullOrEmpty(_attackerRoomId);
        }

        // ==================== 对外接口 ====================

        private void EnsureFallbackUI()
        {
            if (_panel != null) return;
            if (transform.parent == null)
                transform.SetParent(RuntimeUIFactory.GetCanvasTransform(), false);

            _panel = RuntimeUIFactory.CreatePanel(transform, "TribeWarDefensePanel",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-250f, -130f), new Vector2(460f, 300f), new Color(0.08f, 0.06f, 0.08f, 0.92f));
            RuntimeUIFactory.AddVerticalLayout(_panel, 8f, new RectOffset(18, 18, 16, 16), TextAnchor.UpperLeft);

            _titleText = RuntimeUIFactory.CreateText(_panel.transform, "Title", "攻击者：—", 26f,
                new Color(1f, 0.45f, 0.35f), TextAlignmentOptions.Left, new Vector2(420f, 36f));
            RuntimeUIFactory.AddLayoutElement(_titleText.gameObject, 36f);

            _expeditionCountText = RuntimeUIFactory.CreateText(_panel.transform, "Expeditions", "已承受：0", 20f,
                Color.white, TextAlignmentOptions.Left, new Vector2(420f, 28f));
            RuntimeUIFactory.AddLayoutElement(_expeditionCountText.gameObject, 28f);

            _stolenText = RuntimeUIFactory.CreateText(_panel.transform, "Stolen", "被偷取：食物-0 煤炭-0 矿石-0", 20f,
                Color.white, TextAlignmentOptions.Left, new Vector2(420f, 30f));
            RuntimeUIFactory.AddLayoutElement(_stolenText.gameObject, 30f);

            _reportText = RuntimeUIFactory.CreateText(_panel.transform, "Report", "", 18f,
                new Color(0.92f, 0.82f, 0.86f), TextAlignmentOptions.TopLeft, new Vector2(420f, 90f));
            RuntimeUIFactory.AddLayoutElement(_reportText.gameObject, 90f);

            _btnRetaliate = RuntimeUIFactory.CreateButton(_panel.transform, "Retaliate", "反击", out _,
                new Color(0.55f, 0.16f, 0.16f, 1f), new Vector2(150f, 44f));
            RuntimeUIFactory.AddLayoutElement(_btnRetaliate.gameObject, 44f, 150f);
        }

        /// <summary>tribe_war_under_attack 时调用。</summary>
        public void Show(TribeWarUnderAttackData data)
        {
            if (data == null) return;
            _sessionId          = data.sessionId;
            _attackerRoomId     = data.attackerRoomId;
            _attackerName       = data.attackerStreamerName;
            _expeditionReceived = 0;
            _stolenFood         = 0;
            _stolenCoal         = 0;
            _stolenOre          = 0;
            _reportLines.Clear();

            if (_panel == null)
            {
                Debug.Log($"[TribeWarDefenseStatusPanel] Show（面板未绑定，降级 Log）sessionId={data.sessionId} attackerRoomId={data.attackerRoomId} attacker={data.attackerStreamerName}");
                return;
            }

            _panel.SetActive(true);
            if (_titleText != null) _titleText.text = $"攻击者：{(_attackerName ?? "—")}";
            UpdateExpeditionCountUI();
            UpdateStolenUI();
            UpdateReportUI();
            if (_btnRetaliate != null) _btnRetaliate.interactable = !string.IsNullOrEmpty(_attackerRoomId);
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        /// <summary>tribe_war_expedition_incoming 调用，更新已承受远征怪数。</summary>
        public void UpdateExpeditionCount(int count)
        {
            _expeditionReceived += Mathf.Max(0, count);
            UpdateExpeditionCountUI();
        }

        /// <summary>tribe_war_attack_ended 调用，更新被偷取资源汇总。</summary>
        public void UpdateStolen(int food, int coal, int ore)
        {
            _stolenFood = Mathf.Max(0, food);
            _stolenCoal = Mathf.Max(0, coal);
            _stolenOre  = Mathf.Max(0, ore);
            UpdateStolenUI();
        }

        /// <summary>tribe_war_combat_report_defense 调用，追加战报一行。</summary>
        public void AppendReport(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            _reportLines.Enqueue(line);
            while (_reportLines.Count > REPORT_MAX) _reportLines.Dequeue();
            UpdateReportUI();
        }

        // ==================== UI 刷新 ====================

        private void UpdateExpeditionCountUI()
        {
            if (_expeditionCountText != null) _expeditionCountText.text = $"已承受：{_expeditionReceived}";
        }

        private void UpdateStolenUI()
        {
            if (_stolenText != null)
                _stolenText.text = $"被偷取：食物-{_stolenFood} 煤炭-{_stolenCoal} 矿石-{_stolenOre}";
        }

        private void UpdateReportUI()
        {
            if (_reportText == null) return;
            var sb = new StringBuilder();
            foreach (var line in _reportLines)
            {
                sb.AppendLine(line);
            }
            _reportText.text = sb.ToString();
        }

        // ==================== 按钮 ====================

        private void OnRetaliateClicked()
        {
            if (string.IsNullOrEmpty(_attackerRoomId))
            {
                Debug.LogWarning("[TribeWarDefenseStatusPanel] OnRetaliateClicked：_attackerRoomId 为空");
                return;
            }
            SendTribeWarRetaliate(_attackerRoomId);
            // MVP：不本地关面板，等服务端 tribe_war_attack_started（反击视角）/ tribe_war_attack_failed 回推
        }

        // ==================== 网络消息（C→S 字面量） ====================

        /// <summary>发起反击（C→S：tribe_war_retaliate { targetRoomId }）</summary>
        public static void SendTribeWarRetaliate(string targetRoomId)
        {
            var net = NetworkManager.Instance;
            if (net == null || !net.IsConnected)
            {
                Debug.LogWarning("[TribeWarDefenseStatusPanel] SendTribeWarRetaliate：NetworkManager 未连接");
                return;
            }
            long ts = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string json = $"{{\"type\":\"tribe_war_retaliate\",\"data\":{{\"targetRoomId\":\"{EscapeJson(targetRoomId)}\"}},\"timestamp\":{ts}}}";
            net.SendJson(json);
            Debug.Log($"[TribeWarDefenseStatusPanel] 发送 tribe_war_retaliate targetRoomId={targetRoomId}");
        }

        // ==================== 工具 ====================

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
