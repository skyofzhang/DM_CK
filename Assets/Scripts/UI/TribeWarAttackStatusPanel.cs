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
    /// §35 跨直播间攻防战 - 攻击方状态面板（MVP P1，Prefab 绑定由人工补）。
    ///
    /// 展示：
    ///   - 当前攻击目标主播名
    ///   - 攻击能量进度 / 已派出远征怪数 / 偷取资源汇总（食/煤/矿）
    ///   - 文字战报滚动区（最多保留 N=30 条）
    ///   - [停止攻击] 按钮 → 发 tribe_war_stop
    ///
    /// 挂载：Canvas（always-active）；_panel 初始 inactive。
    /// 若 _panel / 文本字段未绑定，所有显示逻辑降级为 Debug.Log。
    /// </summary>
    public class TribeWarAttackStatusPanel : MonoBehaviour
    {
        public static TribeWarAttackStatusPanel Instance { get; private set; }

        // ==================== Inspector 字段（Prefab 绑定由人工） ====================

        [Header("面板根（初始 inactive）")]
        [SerializeField] private GameObject _panel;

        [Header("头部信息")]
        [SerializeField] private TMP_Text _titleText;       // 当前攻击目标主播名
        [SerializeField] private TMP_Text _energyText;      // 当前攻击能量
        [SerializeField] private Slider   _energyBar;       // 能量进度条（0~1，MVP 按 remainingEnergy / 能量基准 cap 100 归一化）
        [SerializeField] private TMP_Text _expeditionCountText; // 已派出远征怪数
        [SerializeField] private TMP_Text _stolenText;          // 偷取资源汇总

        [Header("战报滚动区 & 按钮")]
        [SerializeField] private TMP_Text _reportText;
        [SerializeField] private Button   _btnStop;

        // ==================== 运行时 ====================

        private string _sessionId;
        private string _defenderName;
        private int    _expeditionCount;
        private int    _energy;
        private int    _stolenFood;
        private int    _stolenCoal;
        private int    _stolenOre;

        private readonly Queue<string> _reportLines = new Queue<string>();
        private const int REPORT_MAX = 30;

        // 能量进度条归一化基准（MVP 临时值；服务端未给 cap 字段前使用）
        private const float ENERGY_BAR_CAP = 100f;

        // ==================== 生命周期 ====================

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (_panel != null) _panel.SetActive(false);
        }

        private void Start()
        {
            if (_btnStop != null) _btnStop.onClick.AddListener(OnStopClicked);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ==================== 对外接口 ====================

        /// <summary>tribe_war_attack_started 时调用（双方房间均广播；MVP 两边都 Show）。</summary>
        public void Show(TribeWarAttackStartedData data)
        {
            if (data == null) return;
            _sessionId       = data.sessionId;
            _defenderName    = data.defenderStreamerName;
            _expeditionCount = 0;
            _energy          = 0;
            _stolenFood      = 0;
            _stolenCoal      = 0;
            _stolenOre       = 0;
            _reportLines.Clear();

            if (_panel == null)
            {
                Debug.Log($"[TribeWarAttackStatusPanel] Show（面板未绑定，降级 Log）sessionId={data.sessionId} defender={data.defenderStreamerName}");
                return;
            }

            _panel.SetActive(true);
            if (_titleText != null) _titleText.text = $"攻击目标：{(_defenderName ?? "—")}";
            UpdateEnergyUI();
            UpdateExpeditionCountUI();
            UpdateStolenUI();
            UpdateReportUI();
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        /// <summary>tribe_war_expedition_sent 调用，更新已派出远征怪数。</summary>
        public void UpdateExpeditionCount(int count)
        {
            _expeditionCount += Mathf.Max(0, count);
            UpdateExpeditionCountUI();
        }

        /// <summary>tribe_war_expedition_sent 调用，更新剩余能量。</summary>
        public void UpdateEnergy(int remainingEnergy)
        {
            _energy = Mathf.Max(0, remainingEnergy);
            UpdateEnergyUI();
        }

        /// <summary>tribe_war_attack_ended 调用，更新累计偷取资源汇总。</summary>
        public void UpdateStolen(int food, int coal, int ore)
        {
            _stolenFood = Mathf.Max(0, food);
            _stolenCoal = Mathf.Max(0, coal);
            _stolenOre  = Mathf.Max(0, ore);
            UpdateStolenUI();
        }

        /// <summary>tribe_war_combat_report 调用，追加一行战报。</summary>
        public void AppendReport(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            _reportLines.Enqueue(line);
            while (_reportLines.Count > REPORT_MAX) _reportLines.Dequeue();
            UpdateReportUI();
        }

        // ==================== UI 刷新 ====================

        private void UpdateEnergyUI()
        {
            if (_energyText != null) _energyText.text = $"能量：{_energy}";
            if (_energyBar  != null) _energyBar.value = Mathf.Clamp01(_energy / ENERGY_BAR_CAP);
        }

        private void UpdateExpeditionCountUI()
        {
            if (_expeditionCountText != null) _expeditionCountText.text = $"已派出：{_expeditionCount}";
        }

        private void UpdateStolenUI()
        {
            if (_stolenText != null)
                _stolenText.text = $"偷取：食物+{_stolenFood} 煤炭+{_stolenCoal} 矿石+{_stolenOre}";
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

        private void OnStopClicked()
        {
            SendTribeWarStop();
            // MVP：不本地关面板，等服务端 tribe_war_attack_ended 广播后再关
        }

        // ==================== 网络消息（C→S 字面量） ====================

        /// <summary>停止攻击（C→S：tribe_war_stop）</summary>
        public static void SendTribeWarStop()
        {
            var net = NetworkManager.Instance;
            if (net == null || !net.IsConnected)
            {
                Debug.LogWarning("[TribeWarAttackStatusPanel] SendTribeWarStop：NetworkManager 未连接");
                return;
            }
            long ts = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string json = $"{{\"type\":\"tribe_war_stop\",\"data\":{{}},\"timestamp\":{ts}}}";
            net.SendJson(json);
            Debug.Log("[TribeWarAttackStatusPanel] 发送 tribe_war_stop");
        }
    }
}
