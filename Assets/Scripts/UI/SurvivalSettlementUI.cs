using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DrscfZ.Survival;
using DrscfZ.Systems;
using DrscfZ.Core;

namespace DrscfZ.UI
{
    /// <summary>
    /// 生存结算UI —— §34 B2 改造版（3 帧翻页 + 主播跳过 + 动态标语）
    ///
    /// 🆕 §34 B2 Layer 2 组 B（v1.27）：原 3 屏 A/B/C 序列（8s）升级为 3 帧 10+10+10=30s 翻页结构：
    ///   帧 A（10s）：高光时刻——读 SettlementHighlightsData（伤害最高 / 最佳救援 / 最戏剧性 / 最危险）
    ///   帧 B（10s）：守护者排行榜（复用现有 Top10 逻辑）
    ///   帧 C（10s）：个人数据卡——从 SurvivalGameManager 拼装（若无专属 API 则展示全局 Top/排名）
    ///
    /// 底部翻页圆点指示器（3 个），当前帧高亮。
    /// 主播（isRoomCreator=true）可见"立即重开"按钮，点击调 SurvivalGameManager.SendStreamerSkipSettlement()
    ///   触发服务端提前结束结算 → 立即进入 recovery。
    ///
    /// 🆕 v1.26 永续模式（§16）：
    ///   - 无胜利分支，`IsVictory` 恒为 false（接口保留防止下游空引用）
    ///   - 标题按 reason 区分："manual" → "本次主动终止"；其它失败 reason → "极地陷落 — xxx"
    ///   - 副标题新增堡垒日变化
    ///   - 自动关闭时长 ~30s（§16.6 / §23.1 升级至 30s 以配合 §34 B2）
    ///
    /// 数据来源：SurvivalGameManager.HandleGameEnded → ShowSettlement(SettlementData)
    /// 高光数据：SurvivalGameManager.LastSettlementHighlights（服务端在 survival_game_ended 前推送 settlement_highlights）。
    /// Rankings 由 RankingSystem.GetTopN(3) 在序列开始前自动注入。
    ///
    /// 动态标语（策划案 5066-5070）：
    ///   Top 3："你是部落的传奇守护者！"
    ///   食物最多："你养活了整个部落！"
    ///   击杀最多："怪物听到你的名字都发抖！"
    ///   贡献较低："每一份努力都有意义！下次继续加油！"
    ///
    /// C 屏 Top3 显示规则：
    ///   - _top3Slots 需在 Inspector 中拖拽赋值 3 个预创建的 GameObject（每个至少包含 2 个 TextMeshProUGUI：名字+积分）
    ///   - 若参与人数 &lt; 3，多余槽位自动隐藏（SetActive false）
    /// </summary>
    public class SurvivalSettlementUI : MonoBehaviour
    {
        [Header("Screen A - Highlights (10s)")]
        [SerializeField] private GameObject _screenA;
        [SerializeField] private TextMeshProUGUI _resultTitleText;
        [SerializeField] private TextMeshProUGUI _resultSubtitleText;
        // §34 B2 高光时刻 4 条统计（可选，未绑定时走降级逻辑）
        [SerializeField] private TextMeshProUGUI _topDamageText;      // "伤害最高矿工：XXX 3500"
        [SerializeField] private TextMeshProUGUI _bestRescueText;     // "最佳救援：XXX 送出 爱的爆炸"
        [SerializeField] private TextMeshProUGUI _dramaticEventText;  // "最戏剧性时刻：张力从 95 降到 30"
        [SerializeField] private TextMeshProUGUI _closestCallText;    // "最危险时刻：城门血量仅剩 7%"

        [Header("Screen B - Stats (10s)")]
        [SerializeField] private GameObject _screenB;
        [SerializeField] private TextMeshProUGUI _survivalDaysText;
        [SerializeField] private TextMeshProUGUI _totalKillsText;
        [SerializeField] private TextMeshProUGUI _totalGatherText;
        [SerializeField] private TextMeshProUGUI _totalRepairText;
        [SerializeField] private Transform _rankingListParent;
        [SerializeField] private GameObject _rankEntryPrefab; // pre-created in scene, unused at runtime

        [Header("Screen C - Top3 (10s)")]
        [SerializeField] private GameObject _screenC;
        /// <summary>
        /// 3 个预创建的排名槽位 GameObject（索引 0=第1名，1=第2名，2=第3名）。
        /// 每个 GameObject 至少包含 2 个 TextMeshProUGUI 子组件：texts[0]=名字, texts[1]=积分。
        /// 若参与人数不足，多余槽位会被 SetActive(false)。
        /// ⚠ 请务必在 Inspector 中赋值，否则将退化为旧版 MVP 单行显示并输出 LogError。
        /// </summary>
        [SerializeField] private GameObject[] _top3Slots = new GameObject[3];
        /// <summary>MVP 横幅文字（"本局MVP是 XXX，感谢TA的付出！"）</summary>
        [SerializeField] private TextMeshProUGUI _mvpAnchorLineText;
        /// <summary>§34 B2 动态标语 TMP（按排名/贡献类型展示）</summary>
        [SerializeField] private TextMeshProUGUI _dynamicTaglineText;

        // ─── 旧版 MVP 单行字段（保留供降级显示，不再是主路径）──────────────
        [SerializeField] private TextMeshProUGUI _mvpNameText;
        [SerializeField] private TextMeshProUGUI _mvpScoreText;

        [Header("Ranking System (auto-inject Top3)")]
        [SerializeField] private RankingSystem _rankingSystem;

        [Header("Restart / Actions")]
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _btnViewRanking;  // "查看英雄榜"

        [Header("§34 B2 页码指示器 + 主播跳过")]
        /// <summary>3 个翻页圆点（索引 0=A 帧高亮，1=B 帧高亮，2=C 帧高亮）</summary>
        [SerializeField] private Image[] _pageDots = new Image[3];
        /// <summary>主播"立即重开"按钮（仅 isRoomCreator=true 时可见）</summary>
        [SerializeField] private Button _skipButton;

        // Pre-collected from the RankingList hierarchy (populated in Awake)
        private List<GameObject> _rankEntries = new List<GameObject>();

        // 当前序列 Coroutine 句柄（用于主播跳过时打断）
        private Coroutine _sequenceCoroutine;
        private Coroutine _recoveryWatchdogCoroutine;

        // 🆕 §34 B2 主播身份判定（通过 NetworkManager 的 join_room_confirm 消息确定）
        private bool _isRoomCreator = false;
        private bool _netSubscribed = false;

        // §34 B2 页码高亮色（保持与项目 UI 色调一致）
        private static readonly Color DOT_ACTIVE   = new Color(1f, 0.92f, 0.55f, 1f);   // 金黄
        private static readonly Color DOT_INACTIVE = new Color(1f, 1f, 1f, 0.35f);      // 半透明白

        private void Awake()
        {
            if (_restartButton != null)
                _restartButton.onClick.AddListener(OnRestartClicked);
            if (_btnViewRanking != null)
                _btnViewRanking.onClick.AddListener(OnViewRankingClicked);
            if (_skipButton != null)
                _skipButton.onClick.AddListener(OnSkipClicked);

            // Collect pre-created rank entry GameObjects (B screen)
            if (_rankingListParent != null)
            {
                foreach (Transform child in _rankingListParent)
                    _rankEntries.Add(child.gameObject);
            }

            // §34 B2 订阅 NetworkManager 识别主播身份（必须在 SetActive(false) 之前订阅，
            // 否则 join_room_confirm 在面板显示前先到，OnEnable 未触发将错过该消息）。
            // 保留 gameObject.SetActive(false) 的既有行为（SurvivalSettlementUI 原本就是 inactive → ShowSettlement 激活）。
            // 🟡 audit-r46 GAP-m-05：本路径违反 CLAUDE.md 规则 6（"禁 Awake 中 SetActive(false) 阻断 OnEnable"）但
            //   net.OnMessageReceived 是 static 委托不依赖 GameObject 激活，当前侥幸工作。
            //   理想改法：加 [SerializeField] _panelRoot 字段 + Inspector 拖入子节点，
            //   把 gameObject.SetActive(false) 替换为 _panelRoot.SetActive(false)。
            //   暂不改：需要 scene Editor 同步重建，超出 PM main thread 安全范围。
            //   line 241 PlaySettlementSequence 已 _skipButton.SetActive(_isRoomCreator) 兜底，无功能问题。
            TrySubscribeNet();

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            UnsubscribeNet();
            if (_sequenceCoroutine != null) StopCoroutine(_sequenceCoroutine);
            if (_recoveryWatchdogCoroutine != null) StopCoroutine(_recoveryWatchdogCoroutine);
        }

        private void TrySubscribeNet()
        {
            if (_netSubscribed) return;
            var net = NetworkManager.Instance;
            if (net == null) return;
            net.OnMessageReceived += HandleNetMessage;
            _netSubscribed = true;
        }

        private void UnsubscribeNet()
        {
            if (!_netSubscribed) return;
            var net = NetworkManager.Instance;
            if (net != null) net.OnMessageReceived -= HandleNetMessage;
            _netSubscribed = false;
        }

        private void HandleNetMessage(string type, string dataJson)
        {
            if (type != "join_room_confirm") return;
            _isRoomCreator = ParseBoolField(dataJson, "isRoomCreator");
            // 若结算面板当前可见，立即按身份更新跳过按钮
            if (_skipButton != null)
                _skipButton.gameObject.SetActive(_isRoomCreator && gameObject.activeInHierarchy);
        }

        private static bool ParseBoolField(string json, string field)
        {
            if (string.IsNullOrEmpty(json)) return false;
            int idx = json.IndexOf("\"" + field + "\"");
            if (idx < 0) return false;
            int colon = json.IndexOf(':', idx);
            if (colon < 0) return false;
            int start = colon + 1;
            while (start < json.Length && (json[start] == ' ' || json[start] == '\t')) start++;
            if (start + 4 > json.Length) return false;
            return json.Substring(start, 4).ToLowerInvariant() == "true";
        }

        // ─── Public API: inject settlement data directly ──────────────────────

        private const string MODAL_A_ID_SETTLEMENT = "settlement";
        private bool _modalRegistered = false;

        public void ShowSettlement(SettlementData data)
        {
            // 保险：显示时再订阅一次（Awake 顺序若早于 NetworkManager 初始化时会跳过，此处兜底）

            // audit-r10 §29：结算面板出现 SFX（翻页声）

            // audit-r6 P1-F8：§17.16 登记 A 类 priority=80（最高）；其他 A 类（GateUpgradeConfirm 75 / Roulette 70 / BuildVote 60）自动被抢占
            if (!_modalRegistered)
            {
                if (!DrscfZ.UI.ModalRegistry.Request(MODAL_A_ID_SETTLEMENT, 80, () =>
                {
                    // 被更高优先级替换时（理论上不会发生，因为 80 已是最高），兜底关闭本面板
                    _modalRegistered = false;
                    gameObject.SetActive(false);
                }))
                {
                    return;
                }
                _modalRegistered = true;
            }

            gameObject.SetActive(true);
            TrySubscribeNet();
            DrscfZ.Systems.AudioManager.Instance?.PlaySFX(DrscfZ.Core.AudioConstants.SFX_UI_SETTLEMENT);

            if (_sequenceCoroutine != null) StopCoroutine(_sequenceCoroutine);
            _sequenceCoroutine = StartCoroutine(PlaySettlementSequence(data));
        }

        private void OnDisable()
        {
            if (_recoveryWatchdogCoroutine != null)
            {
                StopCoroutine(_recoveryWatchdogCoroutine);
                _recoveryWatchdogCoroutine = null;
            }

            if (_modalRegistered)
            {
                DrscfZ.UI.ModalRegistry.Release(MODAL_A_ID_SETTLEMENT);
                _modalRegistered = false;
            }
        }

        // ─── Sequence coroutine ───────────────────────────────────────────────
        // 🆕 §34 B2：~30s 自动关闭（帧 A 10s + 帧 B 10s + 帧 C 10s）；主播点"立即重开"提前结束。
        // 帧 A：SettlementHighlightsData（服务端 settlement_highlights 推送）
        // 帧 B：Rankings Top10
        // 帧 C：Top3 + MVP + 动态标语

        private IEnumerator PlaySettlementSequence(SettlementData data)
        {
            // 序列开始时隐藏重新开始按钮，防止误触
            if (_restartButton != null) _restartButton.gameObject.SetActive(false);
            // 主播跳过按钮按身份显示
            if (_skipButton != null) _skipButton.gameObject.SetActive(_isRoomCreator);

            // ── Rankings 注入：若外部未提供，从 RankingSystem 拉取 Top3 ──────
            if ((data.Rankings == null || data.Rankings.Count == 0) && _rankingSystem != null)
            {
                var top3 = _rankingSystem.GetTopN(3);
                if (top3 != null && top3.Count > 0)
                {
                    data.Rankings = new List<RankEntry>(top3.Count);
                    foreach (var c in top3)
                        data.Rankings.Add(new RankEntry { Nickname = c.Nickname, Score = c.Score });
                }
            }

            if (data.Rankings == null || data.Rankings.Count == 0)
                Debug.LogWarning("[SurvivalSettlementUI] Rankings 为空：RankingSystem 无本场数据（本局是否有玩家参与？），C 屏将跳过。");

            // 帧 A：10s
            ShowScreenA(data);
            UpdatePageDots(0);
            yield return new WaitForSecondsRealtime(10f);

            // 帧 B：10s
            ShowScreenB(data);
            UpdatePageDots(1);
            yield return new WaitForSecondsRealtime(10f);

            // 帧 C：10s（无榜时 C 屏仍走一次空态，保证 30s 总时长）
            ShowScreenC(data);
            UpdatePageDots(2);
            yield return new WaitForSecondsRealtime(10f);

            // 序列播完后显示"重新开始"按钮（GM 手动跳过用）；
            // 🔴 audit-r37 GAP-C37-08：服务端 30s 后自动推送 phase_changed{variant:recovery}，客户端进入恢复期白天（旧注释 8s 失真，r31 已改 30000ms）
            if (_restartButton != null) _restartButton.gameObject.SetActive(true);
            if (_skipButton != null) _skipButton.gameObject.SetActive(false);
            Debug.Log("[SurvivalSettlementUI] §34 B2 结算 30s 播完，等待服务端 recovery 推送或主播手动关闭");
            StartRecoveryWatchdog();
            _sequenceCoroutine = null;
        }

        private void StartRecoveryWatchdog()
        {
            if (_recoveryWatchdogCoroutine != null)
                StopCoroutine(_recoveryWatchdogCoroutine);
            _recoveryWatchdogCoroutine = StartCoroutine(RecoverySyncWatchdog());
        }

        private IEnumerator RecoverySyncWatchdog()
        {
            yield return new WaitForSecondsRealtime(5f);
            _recoveryWatchdogCoroutine = null;
            if (!isActiveAndEnabled)
                yield break;

            NetworkManager.Instance?.SendMessage("sync_state");
            Debug.LogWarning("[SurvivalSettlementUI] recovery watchdog fired: sent sync_state to avoid stale settlement UI");
        }

        // ─── §34 B2 页码圆点高亮 ───────────────────────────────────────────────

        private void UpdatePageDots(int activeIndex)
        {
            if (_pageDots == null) return;
            for (int i = 0; i < _pageDots.Length; i++)
            {
                if (_pageDots[i] == null) continue;
                _pageDots[i].color = (i == activeIndex) ? DOT_ACTIVE : DOT_INACTIVE;
            }
        }

        // ─── Screen A: highlights（§34 B2 高光时刻） ─────────────────────────

        private void ShowScreenA(SettlementData data)
        {
            _screenA.SetActive(true);
            _screenB.SetActive(false);
            _screenC.SetActive(false);

            // 标题：manual=灰（中性）；失败=红；IsVictory 保留仅作兜底（v1.26 恒为 false）
            if (data.IsManual)
            {
                _resultTitleText.text  = "本次主动终止";
                _resultTitleText.color = new Color(0.8f, 0.8f, 0.8f); // neutral gray
            }
            else
            {
                _resultTitleText.text = data.FailReason switch
                {
                    "food"        => "极地陷落 — 食物耗尽",
                    "temperature" => "极地陷落 — 冻死冰原",
                    "gate"        => "极地陷落 — 城门攻破",
                    "all_dead"    => "极地陷落 — 矿工全灭",  // 🆕 §16.5
                    _             => "极地陷落"
                };
                _resultTitleText.color = new Color(0.9f, 0.2f, 0.2f); // red
            }

            // 副标题：优先展示堡垒日变化（§16.6），兼容旧数据（Before==0 且 After==0 时退回天数显示）
            if (_resultSubtitleText != null)
                _resultSubtitleText.text = BuildSubtitle(data);

            // §34 B2 高光 4 条：从 SurvivalGameManager.LastSettlementHighlights 读取
            var high = SurvivalGameManager.Instance != null
                ? SurvivalGameManager.Instance.LastSettlementHighlights
                : null;

            UpdateHighlightLine(_topDamageText, BuildTopDamageText(high));
            UpdateHighlightLine(_bestRescueText, BuildBestRescueText(high));
            UpdateHighlightLine(_dramaticEventText, BuildDramaticEventText(high));
            UpdateHighlightLine(_closestCallText, BuildClosestCallText(high));
        }

        private static void UpdateHighlightLine(TextMeshProUGUI tmp, string text)
        {
            if (tmp == null) return;
            if (string.IsNullOrEmpty(text))
            {
                tmp.gameObject.SetActive(false);
                return;
            }
            tmp.gameObject.SetActive(true);
            tmp.text = text;
        }

        private static string BuildTopDamageText(SettlementHighlightsData h)
        {
            if (h == null) return null;
            if (string.IsNullOrEmpty(h.topDamagePlayerName) || h.topDamageValue <= 0) return null;
            return $"伤害最高矿工：{h.topDamagePlayerName}（{h.topDamageValue}）";
        }

        private static string BuildBestRescueText(SettlementHighlightsData h)
        {
            if (h == null) return null;
            if (string.IsNullOrEmpty(h.bestRescuePlayerName) || string.IsNullOrEmpty(h.bestRescueGiftName)) return null;
            return $"最佳救援：{h.bestRescuePlayerName} 送出 {h.bestRescueGiftName}";
        }

        private static string BuildDramaticEventText(SettlementHighlightsData h)
        {
            if (h == null || h.mostDramaticEvent == null) return null;
            if (string.IsNullOrEmpty(h.mostDramaticEvent.desc)) return null;
            int day = h.mostDramaticEvent.day > 0 ? h.mostDramaticEvent.day : 0;
            return day > 0
                ? $"最戏剧性时刻（D{day}）：{h.mostDramaticEvent.desc}"
                : $"最戏剧性时刻：{h.mostDramaticEvent.desc}";
        }

        private static string BuildClosestCallText(SettlementHighlightsData h)
        {
            if (h == null) return null;
            if (h.closestCallHpPct <= 0f || h.closestCallHpPct > 1f) return null;
            int pct = Mathf.Clamp(Mathf.RoundToInt(h.closestCallHpPct * 100f), 0, 100);
            return h.closestCallDay > 0
                ? $"最危险时刻（D{h.closestCallDay}）：城门血量仅剩 {pct}%"
                : $"最危险时刻：城门血量仅剩 {pct}%";
        }

        /// <summary>副标题文案（§16.4 / §16.6）：
        ///  - manual → "本次为主动终止，堡垒日不变"
        ///  - newbieProtected → "新手保护期 · 堡垒日 {Before} 保持"
        ///  - 正常失败 → "堡垒日 {Before} → {After} · 坚守了 X 天"
        ///  - 服务端字段缺失（Before/After 均 0）→ fallback 到旧版天数文案</summary>
        private string BuildSubtitle(SettlementData data)
        {
            if (data.IsManual)
                return "本次为主动终止，堡垒日不变";

            // Before/After 均 0 视为服务端未填充字段（旧协议 / 本地 fallback 路径）
            bool hasFortressInfo = data.FortressDayBefore > 0 || data.FortressDayAfter > 0;
            if (!hasFortressInfo)
                return $"坚守了 {data.SurvivalDays} 天";

            if (data.NewbieProtected)
                return $"新手保护期 · 堡垒日 {data.FortressDayBefore} 保持";

            return $"堡垒日 {data.FortressDayBefore} → {data.FortressDayAfter} · 坚守了 {data.SurvivalDays} 天";
        }

        // ─── Screen B: stats + ranking ────────────────────────────────────────

        private void ShowScreenB(SettlementData data)
        {
            _screenA.SetActive(false);
            _screenB.SetActive(true);
            _screenC.SetActive(false);

            if (_survivalDaysText) _survivalDaysText.text = $"生存天数: {data.SurvivalDays}";
            // TotalKills/Gather/Repair 当前由服务器统计，暂无数据时隐藏避免显示"0"
            if (_totalKillsText)  _totalKillsText.gameObject.SetActive(data.TotalKills  > 0);
            if (_totalGatherText) _totalGatherText.gameObject.SetActive(data.TotalGather > 0);
            if (_totalRepairText) _totalRepairText.gameObject.SetActive(data.TotalRepair > 0);
            if (_totalKillsText  && data.TotalKills  > 0) _totalKillsText.text  = $"总击杀: {data.TotalKills}";
            if (_totalGatherText && data.TotalGather > 0) _totalGatherText.text = $"总采集: {data.TotalGather}";
            if (_totalRepairText && data.TotalRepair > 0) _totalRepairText.text = $"总修墙: {data.TotalRepair}";

            // Hide all pre-created entries, then reveal as needed
            foreach (var entry in _rankEntries) entry.SetActive(false);

            if (data.Rankings != null)
            {
                int count = Mathf.Min(data.Rankings.Count, _rankEntries.Count);
                for (int i = 0; i < count; i++)
                {
                    var entry = _rankEntries[i];
                    entry.SetActive(true);
                    var texts = entry.GetComponentsInChildren<TextMeshProUGUI>();
                    if (texts.Length >= 3)
                    {
                        texts[0].text = $"#{i + 1}";
                        texts[1].text = data.Rankings[i].Nickname;
                        texts[2].text = data.Rankings[i].Score.ToString();
                    }
                }
            }

            // 🔴 audit-r32 GAP-A26-08 r31 半成品闭环：§34 F6 双档分配 — tail 30% 池 ≥100 贡献分配
            //   r35 完整版（95% → 100%）：动态创建独立列表 UI 在 _screenB 下方显示一行行 "Nickname +Share"
            RenderTailRewardsList(data);
        }

        // 🔴 audit-r35 GAP-A26-08 r32 半成品闭环（95% → 100%）：tailRewards 独立列表渲染
        //   动态创建子节点 — 不依赖场景预创建的 GameObject 槽位（与 PauseOverlayUI 同模式）
        private GameObject _tailRewardsContainer;

        private void RenderTailRewardsList(SettlementData data)
        {
            // 清理旧列表（每次 ShowScreenB 重建，避免叠加）
            if (_tailRewardsContainer != null)
            {
                Destroy(_tailRewardsContainer);
                _tailRewardsContainer = null;
            }

            if (data.TailRewards == null || data.TailRewards.Count == 0) return;

            int totalCount = Mathf.Max(data.TailEligibleCount, data.TailRewards.Count);
            int totalShare = 0;
            for (int i = 0; i < data.TailRewards.Count; i++) totalShare += data.TailRewards[i].Share;

            // 跑马灯保持（向下兼容 r32 行为）
            var sb = new System.Text.StringBuilder();
            sb.Append($"§34 F6 双档分配：{totalCount} 名 ≥100 贡献者瓜分 {totalShare} 积分");
            if (data.TailRewards.Count > 0)
            {
                int showN = Mathf.Min(3, data.TailRewards.Count);
                sb.Append("（");
                for (int i = 0; i < showN; i++)
                {
                    if (i > 0) sb.Append(" / ");
                    sb.Append($"{data.TailRewards[i].Nickname} +{data.TailRewards[i].Share}");
                }
                if (data.TailRewards.Count > showN) sb.Append($" 等 {totalCount - showN} 名");
                sb.Append("）");
            }
            HorizontalMarqueeUI.Instance?.AddMessage("结算分配", null, sb.ToString());

            // 🔴 r35 完整版：在 _screenB 下方创建独立列表（最多显示前 5 名 + "等 N 名"）
            if (_screenB == null) { Debug.LogWarning("[SettlementUI] _screenB null，跳过 tailRewards 独立列表渲染"); return; }

            _tailRewardsContainer = new GameObject("TailRewardsList");
            _tailRewardsContainer.transform.SetParent(_screenB.transform, false);
            var rt = _tailRewardsContainer.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 30f);    // 距 _screenB 底部 30px
            rt.sizeDelta = new Vector2(700f, 200f);

            var vlg = _tailRewardsContainer.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment   = TextAnchor.UpperCenter;
            vlg.spacing          = 4f;
            vlg.padding          = new RectOffset(8, 8, 8, 8);
            vlg.childControlHeight = false;
            vlg.childControlWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth  = true;

            // 标题
            var titleGO = new GameObject("TailTitle");
            titleGO.transform.SetParent(_tailRewardsContainer.transform, false);
            var titleRT = titleGO.AddComponent<RectTransform>();
            titleRT.sizeDelta = new Vector2(0f, 32f);
            var titleLE = titleGO.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 32f;
            var titleTmp = titleGO.AddComponent<TextMeshProUGUI>();
            BindFont(titleTmp);
            titleTmp.text      = $"§34 F6 双档分配 · {totalCount} 名 ≥100 贡献者 · 总池 {totalShare}";
            titleTmp.fontSize  = 22;
            titleTmp.color     = new Color(1f, 0.84f, 0.1f); // 金色
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.fontStyle = FontStyles.Bold;

            // 列表项（最多 5 条，超出加 "等 N 名"）
            int showN2 = Mathf.Min(5, data.TailRewards.Count);
            for (int i = 0; i < showN2; i++)
            {
                var entry = data.TailRewards[i];
                var rowGO = new GameObject($"TailRow_{i}");
                rowGO.transform.SetParent(_tailRewardsContainer.transform, false);
                var rowRT = rowGO.AddComponent<RectTransform>();
                rowRT.sizeDelta = new Vector2(0f, 22f);
                var rowLE = rowGO.AddComponent<LayoutElement>();
                rowLE.preferredHeight = 22f;
                var rowTmp = rowGO.AddComponent<TextMeshProUGUI>();
                BindFont(rowTmp);
                rowTmp.text      = $"  {entry.Nickname}  +{entry.Share}";
                rowTmp.fontSize  = 18;
                rowTmp.color     = new Color(0.95f, 0.95f, 0.95f);
                rowTmp.alignment = TextAlignmentOptions.Center;
            }
            if (data.TailRewards.Count > showN2)
            {
                var moreGO = new GameObject("TailMore");
                moreGO.transform.SetParent(_tailRewardsContainer.transform, false);
                var moreRT = moreGO.AddComponent<RectTransform>();
                moreRT.sizeDelta = new Vector2(0f, 22f);
                var moreLE = moreGO.AddComponent<LayoutElement>();
                moreLE.preferredHeight = 22f;
                var moreTmp = moreGO.AddComponent<TextMeshProUGUI>();
                BindFont(moreTmp);
                moreTmp.text      = $"...等 {totalCount - showN2} 名 ≥100 贡献者已分配";
                moreTmp.fontSize  = 16;
                moreTmp.color     = new Color(0.7f, 0.7f, 0.7f);
                moreTmp.alignment = TextAlignmentOptions.Center;
                moreTmp.fontStyle = FontStyles.Italic;
            }

            Debug.Log($"[SettlementUI] tailRewards rendered: {showN2}/{data.TailRewards.Count} entries shown, total {totalShare} (independent UI list)");
        }

        private static void BindFont(TextMeshProUGUI tmp)
        {
            if (tmp == null) return;
            var font = Resources.Load<TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF")
                    ?? Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
            if (font != null) tmp.font = font;
        }

        // ─── Screen C: Top3 贡献者 + 动态标语 ──────────────────────────────────

        private void ShowScreenC(SettlementData data)
        {
            _screenA.SetActive(false);
            _screenB.SetActive(false);
            _screenC.SetActive(true);

            // MVP 横幅（第1名姓名）—— 始终更新，不依赖 top3Slots 是否绑定
            var mvp = (data.Rankings != null && data.Rankings.Count > 0) ? data.Rankings[0] : null;
            if (mvp != null)
            {
                if (_mvpAnchorLineText)
                    _mvpAnchorLineText.text = $"本局MVP是 {mvp.Nickname}，感谢TA的付出！";
                if (_mvpNameText)  _mvpNameText.text  = mvp.Nickname;
                if (_mvpScoreText) _mvpScoreText.text = $"贡献值: {mvp.Score}";
            }

            // Top3 槽位完整性校验
            bool slotsValid = _top3Slots != null && _top3Slots.Length >= 3;
            if (slotsValid)
            {
                // 填充 Top3 槽位（超出参与人数的槽位隐藏）
                for (int i = 0; i < 3; i++)
                {
                    var slot = _top3Slots[i];
                    if (slot == null) continue;

                    bool hasData = data.Rankings != null && i < data.Rankings.Count;
                    slot.SetActive(hasData);
                    if (!hasData) continue;

                    // 按名字查找子组件，避免因 TMP 数量/顺序不同导致下标错位
                    var nameComp  = slot.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
                    var scoreComp = slot.transform.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();

                    if (nameComp  != null) nameComp.text  = data.Rankings[i].Nickname;
                    if (scoreComp != null) scoreComp.text = $"贡献值: {data.Rankings[i].Score}";
                }
            }
            else
            {
                Debug.LogWarning("[SurvivalSettlementUI] _top3Slots 未配置，跳过 Top3 显示");
            }

            // §34 B2 动态标语：按 Top3 / 贡献量化判定
            if (_dynamicTaglineText != null)
                _dynamicTaglineText.text = BuildDynamicTagline(data);
        }

        /// <summary>§34 B2 动态标语（策划案 5066-5070）：
        ///   Top 3（rank 1-3）  → "你是部落的传奇守护者！"
        ///   非 Top 3 且贡献 ≥ 100 → "每一份付出都点亮部落的希望！"
        ///   贡献较低（&lt; 100）→ "每一份努力都有意义！下次继续加油！"
        /// 注：MVP 阶段无"食物最多 / 击杀最多"分类数据，暂用贡献阈值近似；
        /// 后续接入 SelfPlayerId + playerStats（采集类型分布）时再按类型切换标语。</summary>
        private static string BuildDynamicTagline(SettlementData data)
        {
            int selfScore = 0;
            // MVP：取榜首作为当前观众视角（NetworkManager 无 SelfPlayerId 接口，fallback 到 rank 1）
            if (data.Rankings != null && data.Rankings.Count > 0)
                selfScore = data.Rankings[0].Score;

            bool inTop3 = data.Rankings != null && data.Rankings.Count >= 1 && selfScore > 0;
            if (inTop3 && selfScore >= 100)
                return "你是部落的传奇守护者！";
            if (selfScore >= 100)
                return "每一份付出都点亮部落的希望！";
            return "每一份努力都有意义！下次继续加油！";
        }

        // ─── Restart ─────────────────────────────────────────────────────────
        // 🆕 v1.26 永续模式（§23.1）：结算结束客户端**不再发送 reset_game**；
        //   🔴 audit-r37 GAP-C37-08：服务端 30s 后自动推送 phase_changed{variant:recovery} 进入恢复期（旧注释 8s 失真）。
        //   保留按钮仅作 GM 手动关闭 UI 兜底（例如序列播完后再次点击隐藏面板）。

        private void OnRestartClicked()
        {
            // 仅关闭面板；不触发 reset_game（永续模式服务端自动进入 recovery）
            gameObject.SetActive(false);
            Debug.Log("[SurvivalSettlementUI] 主播手动关闭结算面板（不触发 reset_game）");
        }

        // ─── §34 B2 Skip Settlement (仅主播) ────────────────────────────────

        private void OnSkipClicked()
        {
            if (!_isRoomCreator)
            {
                Debug.LogWarning("[SurvivalSettlementUI] 非主播点击了跳过按钮（按钮本不应可见）");
                return;
            }
            // 立即停止序列，发送 C→S 消息
            if (_sequenceCoroutine != null)
            {
                StopCoroutine(_sequenceCoroutine);
                _sequenceCoroutine = null;
            }
            SurvivalGameManager.Instance?.SendStreamerSkipSettlement();
            StartRecoveryWatchdog();
            // 面板暂不隐藏，等待服务端 recovery/phase_changed 触发 SGM 隐藏逻辑
            Debug.Log("[SurvivalSettlementUI] 主播点击『立即重开』→ 发送 streamer_skip_settlement");
        }

        // ─── View Ranking ─────────────────────────────────────────────────────

        private void OnViewRankingClicked()
        {
            // 与大厅"排行榜"按钮打开同一个面板：SurvivalRankingUI.ShowPanel()
            // 🔴 audit-r46 GAP-m-06：优先用 Instance 单例（点击高频时避免全场 FindObjectOfType 抖动）
            var rankingUI = SurvivalRankingUI.Instance ?? FindObjectOfType<SurvivalRankingUI>(true);
            if (rankingUI != null)
            {
                rankingUI.ShowPanel();
                // 置顶，确保显示在结算面板之上
                rankingUI.transform.SetAsLastSibling();
                Debug.Log("[SurvivalSettlementUI] 打开本周贡献榜");
            }
            else
            {
                Debug.LogWarning("[SurvivalSettlementUI] 找不到 SurvivalRankingUI，请确认场景中存在该组件");
            }
        }
    }

    // ==================== Data Classes ====================

    /// <summary>
    /// 结算面板所需数据（从 SurvivalGameEndedData 映射而来）。
    /// Rankings 由 PlaySettlementSequence 从 RankingSystem.GetTopN(3) 自动注入，
    /// 也可由外部显式提供。
    ///
    /// 🆕 v1.26 永续模式（§16.6）：
    ///   - IsVictory 保留但恒为 false（永续模式无胜利分支，接口保留防止下游空引用）
    ///   - FailReason 枚举扩展："food" | "temperature" | "gate" | "all_dead" | "manual" | "unknown"
    ///   - 新增 FortressDayBefore / FortressDayAfter / NewbieProtected 用于副标题渲染
    ///   - 新增 IsManual 标记 GM 手动终止（§16.4，此时 FortressDayBefore==FortressDayAfter）
    /// </summary>
    [System.Serializable]
    public class SettlementData
    {
        public bool   IsVictory;             // v1.26 恒为 false；保留兼容（未来若增加胜利条件可复用）
        public string FailReason;            // "food" | "temperature" | "gate" | "all_dead" | "manual" | "unknown"
        public int    SurvivalDays;
        public int    TotalKills;
        public int    TotalGather;
        public int    TotalRepair;
        public int    FortressDayBefore;     // 🆕 §16.6
        public int    FortressDayAfter;      // 🆕 §16.6
        public bool   NewbieProtected;       // 🆕 §16.6 Day 1-10 新手保护
        public bool   IsManual;              // 🆕 §16.4 GM 手动终止（reason == "manual"）
        public List<RankEntry> Rankings;     // null = 由 PlaySettlementSequence 自动从 RankingSystem 获取
        // 🔴 audit-r32 GAP-A26-08 r31 半成品闭环：tail 30% 池 ≥100 贡献分配（非 Top10）— SurvivalGameEndedData.tailRewards 映射后渲染于帧 B
        public List<TailRewardSummary> TailRewards;  // null/empty 不显示
        public int    TailEligibleCount;     // §34 F6: tailRewards 总人数（>List.Count 时表示"还有 N 名"）
    }

    [System.Serializable]
    public class RankEntry
    {
        public string Nickname;
        public int    Score;
    }

    [System.Serializable]
    public class TailRewardSummary
    {
        public string Nickname;
        public int    Share;
    }
}
