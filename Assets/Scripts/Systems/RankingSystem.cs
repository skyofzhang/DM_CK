using System;
using System.Collections.Generic;
using UnityEngine;
using DrscfZ.Core;
using DrscfZ.Survival;

namespace DrscfZ.Systems
{
    /// <summary>
    /// 排行榜系统
    /// - 角力游戏：维护左右阵营 Top4 + 连胜信息（UpdateRankings / Reset）
    /// - 生存游戏：订阅 SurvivalGameManager 事件，维护本场贡献榜，提供 GetTopN(n)
    /// </summary>
    public class RankingSystem : MonoBehaviour
    {
        // ─── 单例（供 ResourceRankUI 等跨系统访问）───────────────────────────
        public static RankingSystem Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        // ─── 角力游戏排行（原有功能，保持不变）────────────────────────────────

        public RankingEntry[] LeftRankings  { get; private set; } = Array.Empty<RankingEntry>();
        public RankingEntry[] RightRankings { get; private set; } = Array.Empty<RankingEntry>();

        /// <summary>左右阵营连胜信息（每次 ranking_update 更新）</summary>
        public StreakInfo CurrentStreakInfo { get; private set; }

        public event Action OnRankingsUpdated;

        public void UpdateRankings(RankingEntry[] left, RankingEntry[] right, StreakInfo streakInfo = null)
        {
            LeftRankings  = left  ?? Array.Empty<RankingEntry>();
            RightRankings = right ?? Array.Empty<RankingEntry>();
            if (streakInfo != null)
                CurrentStreakInfo = streakInfo;
            OnRankingsUpdated?.Invoke();
        }

        public void Reset()
        {
            LeftRankings  = Array.Empty<RankingEntry>();
            RightRankings = Array.Empty<RankingEntry>();
            CurrentStreakInfo = null;
            OnRankingsUpdated?.Invoke();
        }

        // ─── 生存游戏贡献追踪（新增）─────────────────────────────────────────

        /// <summary>生存游戏单场贡献者快照（供结算 C 屏使用）</summary>
        public class SurvivalContributor
        {
            public string PlayerId;
            public string Nickname;
            public int    Score;
        }

        private readonly Dictionary<string, SurvivalContributor> _survivalScores
            = new Dictionary<string, SurvivalContributor>();

        // 防止重复订阅的标志
        private bool _survivalEventsSubscribed = false;

        private void Start()
        {
            TrySubscribeSurvivalEvents();
        }

        /// <summary>
        /// 尝试订阅 SurvivalGameManager 事件。
        /// 支持重复调用（已订阅则跳过），解决 Script Execution Order 导致
        /// Start() 时 Instance 尚未赋值而订阅失败的问题。
        /// </summary>
        private void TrySubscribeSurvivalEvents()
        {
            if (_survivalEventsSubscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null)
            {
                Debug.LogWarning("[RankingSystem] SurvivalGameManager.Instance 为 null，将在 Update 中重试订阅。");
                return;
            }
            sgm.OnWorkCommand  += HandleWorkCommand;
            sgm.OnGiftReceived += HandleGiftReceived;
            sgm.OnStateChanged += HandleSurvivalStateChanged;
            _survivalEventsSubscribed = true;
            Debug.Log("[RankingSystem] 已订阅 SurvivalGameManager 事件，生存贡献追踪已启动");
        }

        private void Update()
        {
            // 若 Start() 时订阅失败，每帧重试直到成功
            if (!_survivalEventsSubscribed)
                TrySubscribeSurvivalEvents();
        }

        private void OnDestroy()
        {
            if (!_survivalEventsSubscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;
            sgm.OnWorkCommand  -= HandleWorkCommand;
            sgm.OnGiftReceived -= HandleGiftReceived;
            sgm.OnStateChanged -= HandleSurvivalStateChanged;
            _survivalEventsSubscribed = false;
        }

        // 每条有效弹幕指令 +1 分
        private void HandleWorkCommand(WorkCommandData cmd)
        {
            if (string.IsNullOrEmpty(cmd?.playerId)) return;
            TrackSurvivalScore(cmd.playerId, cmd.playerName ?? "匿名", 1);
        }

        // 礼物按 contribution 字段计分（已由服务器换算）
        private void HandleGiftReceived(SurvivalGiftData gift)
        {
            if (string.IsNullOrEmpty(gift?.playerId)) return;
            int score = Mathf.Max(1, Mathf.RoundToInt(gift.contribution));
            TrackSurvivalScore(gift.playerId, gift.playerName ?? "匿名", score);
        }

        // 进入 Idle 状态（新局）时清空本场积分和资源贡献
        private void HandleSurvivalStateChanged(SurvivalGameManager.SurvivalState state)
        {
            if (state == SurvivalGameManager.SurvivalState.Idle)
            {
                ResetSurvivalScores();
                ResetResourceContribs();
            }
        }

        /// <summary>手动追踪/累加一名玩家的生存积分（可供外部调用）</summary>
        public void TrackSurvivalScore(string playerId, string nickname, int score)
        {
            if (string.IsNullOrEmpty(playerId)) return;
            if (_survivalScores.TryGetValue(playerId, out var existing))
            {
                existing.Score += score;
            }
            else
            {
                _survivalScores[playerId] = new SurvivalContributor
                {
                    PlayerId = playerId,
                    Nickname = string.IsNullOrEmpty(nickname) ? "匿名" : nickname,
                    Score    = score
                };
            }
        }

        /// <summary>清空本场生存积分（每局开始时自动调用）</summary>
        public void ResetSurvivalScores()
        {
            _survivalScores.Clear();
            Debug.Log("[RankingSystem] 生存贡献榜已重置（新局开始）");
        }

        /// <summary>
        /// 获取生存贡献 Top N（按积分降序）。
        /// 若参与人数 &lt; n，返回实际人数；若为 0 则返回空列表。
        /// </summary>
        public List<SurvivalContributor> GetTopN(int n)
        {
            var list = new List<SurvivalContributor>(_survivalScores.Values);
            list.Sort((a, b) => b.Score.CompareTo(a.Score));
            if (list.Count > n) list.RemoveRange(n, list.Count - n);
            return list;
        }

        // ─── 资源分类贡献追踪（白天物资排行榜专用）──────────────────────────

        // 按资源类型分别记录贡献（food=食物, coal=煤炭, ore=矿石）
        private Dictionary<string, int>    _foodContrib = new Dictionary<string, int>();
        private Dictionary<string, int>    _coalContrib = new Dictionary<string, int>();
        private Dictionary<string, int>    _oreContrib  = new Dictionary<string, int>();
        // playerId → playerName 映射（显示用）
        private Dictionary<string, string> _playerNames = new Dictionary<string, string>();
        private bool _resourceDirty = false;

        /// <summary>记录玩家某类资源的贡献量（playerName 用于显示）</summary>
        public void AddResourceContrib(string playerId, string playerName, string resourceType, int amount)
        {
            if (string.IsNullOrEmpty(playerId) || amount <= 0) return;

            // 缓存最新 playerName
            if (!string.IsNullOrEmpty(playerName))
                _playerNames[playerId] = playerName;

            Dictionary<string, int> dict;
            switch (resourceType.ToLower())
            {
                case "food":  dict = _foodContrib; break;
                case "coal":  dict = _coalContrib; break;
                case "ore":   dict = _oreContrib;  break;
                default: return;
            }

            if (!dict.ContainsKey(playerId)) dict[playerId] = 0;
            dict[playerId] += amount;
            _resourceDirty = true;
        }

        /// <summary>获取某类资源的 Top N 玩家（含 playerName）</summary>
        public List<(string playerId, string playerName, int amount)> GetTopByResource(string resourceType, int n = 3)
        {
            Dictionary<string, int> dict;
            switch (resourceType.ToLower())
            {
                case "food":  dict = _foodContrib; break;
                case "coal":  dict = _coalContrib; break;
                case "ore":   dict = _oreContrib;  break;
                default: return new List<(string, string, int)>();
            }

            var result = new List<(string playerId, string playerName, int amount)>();
            foreach (var kv in dict)
            {
                _playerNames.TryGetValue(kv.Key, out var name);
                result.Add((kv.Key, name ?? kv.Key, kv.Value));
            }
            result.Sort((a, b) => b.amount.CompareTo(a.amount));
            if (result.Count > n) result.RemoveRange(n, result.Count - n);
            return result;
        }

        /// <summary>获取并清除资源脏标记</summary>
        public bool ConsumeResourceDirty()
        {
            bool v = _resourceDirty;
            _resourceDirty = false;
            return v;
        }

        /// <summary>重置所有资源分类贡献数据（新局开始时调用）</summary>
        public void ResetResourceContribs()
        {
            _foodContrib.Clear();
            _coalContrib.Clear();
            _oreContrib.Clear();
            _playerNames.Clear();
            _resourceDirty = false;
        }
    }
}
