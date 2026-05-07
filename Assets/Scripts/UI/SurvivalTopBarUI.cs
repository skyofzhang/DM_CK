using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// 生存游戏顶部状态栏
    /// 显示：天数/阶段 | 食物 | 煤炭 | 矿石 | 炉温 | 城门HP | 倒计时
    /// 挂载到 Canvas/GameUIPanel/TopBar（或直接挂 Canvas）
    /// </summary>
    public class SurvivalTopBarUI : MonoBehaviour
    {
        public static SurvivalTopBarUI Instance { get; private set; }

        [Header("昼夜 & 倒计时")]
        public TextMeshProUGUI phaseText;     // "第1天 · 白天"
        public TextMeshProUGUI timerText;     // "02:30"

        [Header("食物")]
        public TextMeshProUGUI foodText;
        public Image           foodIcon;

        [Header("煤炭")]
        public TextMeshProUGUI coalText;
        public Image           coalIcon;

        [Header("矿石")]
        public TextMeshProUGUI oreText;
        public Image           oreIcon;

        [Header("炉温")]
        public TextMeshProUGUI furnaceTempText;
        public Image           furnaceFillBar;  // 温度条（fillAmount 0-1）
        public Image           furnaceIcon;

        [Header("城门HP")]
        public TextMeshProUGUI gateHpText;
        public Image           gateHpBar;       // HP条（fillAmount 0-1）
        public Image           gateIcon;

        [Header("玩家数")]
        public TextMeshProUGUI playerCountText; // "参与: 42人"

        [Header("积分池")]
        public TextMeshProUGUI scorePoolText;   // "积分池:1234" 或 "积分池:1.2万"

        private bool _subscribed = false;
        private bool _resSubscribed = false;
        private bool _gateSubscribed = false;
        private bool _dnSubscribed = false;
        private bool _sgmSubscribed = false;
        private ResourceSystem _subscribedRes;
        private CityGateSystem _subscribedGate;
        private DayNightCycleManager _subscribedDn;
        private SurvivalGameManager _subscribedSgm;
        private SurvivalGameManager _subscribedSgmS36;

        // T213：前值追踪，数值增加时触发弹跳动画
        private int   _prevFood, _prevCoal, _prevOre;
        private float _prevTemp;
        private int   _prevScorePool;

        // 助威模式 §33（🆕 v1.27）
        private int _supporterCount = 0;

        // §36 全服同步（🆕 v1.27 MVP）：最近一次 world_clock_tick/season_state 快照
        private int    _seasonDay;
        private string _themeId;
        private int    _fortressDay;
        // 订阅 §36 事件标志（独立于 _subscribed 的子系统聚合订阅，避免两条路径错位）
        private bool _sgmSubscribedS36 = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            if (Instance != null && Instance != this) { /* 保留先进入的实例 */ return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Start()
        {
            // Start()で再度試みる（Awake順序によりOnEnableでInstanceがnullだった場合の補完）
            TrySubscribe();
        }

        private void TrySubscribe()
        {
            if (Instance != this) return;
            var res  = ResourceSystem.Instance;
            var gate = CityGateSystem.Instance;
            var dn   = DayNightCycleManager.Instance;
            var sgm  = SurvivalGameManager.Instance;

            // 全部未初期化なら次の Start/OnEnable に持ち越す。
            if (res == null && gate == null && dn == null && sgm == null) return;

            if (!_resSubscribed && res != null)
            {
                res.OnResourceChanged += HandleResourceChanged;
                _subscribedRes = res;
                _resSubscribed = true;
                // 立即同步当前值
                HandleResourceChanged(res.Food, res.Coal, res.Ore, res.FurnaceTemp);
            }

            if (!_gateSubscribed && gate != null)
            {
                gate.OnHpChanged += HandleGateHpChanged;
                _subscribedGate = gate;
                _gateSubscribed = true;
                HandleGateHpChanged(gate.CurrentHp, gate.MaxHp);
            }

            if (!_dnSubscribed && dn != null)
            {
                dn.OnDayStarted    += HandleDayStarted;
                dn.OnNightStarted  += HandleNightStarted;
                dn.OnTimeTick      += HandleTimeTick;
                _subscribedDn = dn;
                _dnSubscribed = true;
                UpdatePhaseDisplay();
            }

            if (!_sgmSubscribed && sgm != null)
            {
                sgm.OnPlayerJoined     += HandlePlayerJoinedForCount;
                sgm.OnScorePoolUpdated += HandleScorePoolUpdated;
                _subscribedSgm = sgm;
                _sgmSubscribed = true;
                UpdatePlayerCount();
            }

            // §36 全服同步订阅
            if (!_sgmSubscribedS36 && sgm != null)
            {
                sgm.OnWorldClockTick     += HandleWorldClockTick;
                sgm.OnSeasonState        += HandleSeasonState;
                sgm.OnFortressDayChanged += HandleFortressDayChanged;
                _subscribedSgmS36 = sgm;
                _sgmSubscribedS36 = true;
                // 若已有缓存则立即同步一次
                var snap = sgm.CurrentSeasonState;
                if (snap != null)
                {
                    _seasonDay = snap.seasonDay;
                    _themeId   = snap.themeId;
                    UpdatePhaseDisplay();
                }
            }

            _subscribed = _resSubscribed || _gateSubscribed || _dnSubscribed || _sgmSubscribed || _sgmSubscribedS36;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            if (_resSubscribed && _subscribedRes != null)
                _subscribedRes.OnResourceChanged -= HandleResourceChanged;

            if (_gateSubscribed && _subscribedGate != null)
                _subscribedGate.OnHpChanged -= HandleGateHpChanged;

            if (_dnSubscribed && _subscribedDn != null)
            {
                _subscribedDn.OnDayStarted   -= HandleDayStarted;
                _subscribedDn.OnNightStarted -= HandleNightStarted;
                _subscribedDn.OnTimeTick     -= HandleTimeTick;
            }

            if (_sgmSubscribed && _subscribedSgm != null)
            {
                _subscribedSgm.OnPlayerJoined     -= HandlePlayerJoinedForCount;
                _subscribedSgm.OnScorePoolUpdated -= HandleScorePoolUpdated;
            }

            if (_sgmSubscribedS36 && _subscribedSgmS36 != null)
            {
                _subscribedSgmS36.OnWorldClockTick     -= HandleWorldClockTick;
                _subscribedSgmS36.OnSeasonState        -= HandleSeasonState;
                _subscribedSgmS36.OnFortressDayChanged -= HandleFortressDayChanged;
            }

            _resSubscribed = false;
            _gateSubscribed = false;
            _dnSubscribed = false;
            _sgmSubscribed = false;
            _sgmSubscribedS36 = false;
            _subscribedRes = null;
            _subscribedGate = null;
            _subscribedDn = null;
            _subscribedSgm = null;
            _subscribedSgmS36 = null;
            _subscribed = false;
        }

        // ==================== 事件处理 ====================

        // 警告阈值（策划案 §2）
        private const int   FOOD_DANGER_THRESHOLD  = 100;  // ≤100 红色
        private const int   FOOD_WARN_THRESHOLD    = 200;  // ≤200 橙色
        private const int   COAL_WARN_THRESHOLD    = 50;   // ≤50  橙色
        private const float TEMP_DANGER_THRESHOLD  = -80f; // ≤-80 紫色
        private const float TEMP_WARN_THRESHOLD    = -50f; // ≤-50 青色

        private void HandleResourceChanged(int food, int coal, int ore, float temp)
        {
            // 食物（策划案 §2：≤100 危险红，≤200 警告橙）
            if (foodText)
            {
                foodText.text = food.ToString();
                if (food <= FOOD_DANGER_THRESHOLD)
                    foodText.color = Color.red;
                else if (food <= FOOD_WARN_THRESHOLD)
                    foodText.color = new Color(1f, 0.5f, 0.1f); // 橙色
                else
                    foodText.color = Color.white;
            }

            // 煤炭（≤50 警告橙）
            if (coalText)
            {
                coalText.text = coal.ToString();
                coalText.color = coal <= 0 ? Color.red
                               : coal <= COAL_WARN_THRESHOLD ? new Color(1f, 0.5f, 0.1f)
                               : Color.white;
            }

            // 矿石（矿石耗尽显示灰色，不强制报红）
            if (oreText)
            {
                oreText.text = ore.ToString();
                oreText.color = ore <= 0 ? new Color(0.5f, 0.5f, 0.5f) : Color.white;
            }

            // 炉温
            UpdateFurnaceTemp(temp);

            // T213：数值增加时触发弹跳动画（复用 GiftEffectSystem.ShakeScale）
            if (food > _prevFood && foodText)
                StartCoroutine(GiftEffectSystem.ShakeScale(foodText.transform, 1, 0.2f, 1.15f));
            if (coal > _prevCoal && coalText)
                StartCoroutine(GiftEffectSystem.ShakeScale(coalText.transform, 1, 0.2f, 1.15f));
            if (ore > _prevOre && oreText)
                StartCoroutine(GiftEffectSystem.ShakeScale(oreText.transform, 1, 0.2f, 1.15f));
            if (temp > _prevTemp + 0.5f && furnaceTempText)
                StartCoroutine(GiftEffectSystem.ShakeScale(furnaceTempText.transform, 1, 0.2f, 1.15f));

            _prevFood = food; _prevCoal = coal;
            _prevOre  = ore;  _prevTemp = temp;
        }

        private void UpdateFurnaceTemp(float temp)
        {
            if (furnaceTempText)
            {
                furnaceTempText.text = $"{temp:F0}°C";
                var c = ResourceSystem.Instance?.GetTempColor() ?? Color.white;
                furnaceTempText.color = c;
                if (furnaceIcon) furnaceIcon.color = c;
            }

            if (furnaceFillBar)
            {
                // -100 ~ 100 映射到 0 ~ 1
                furnaceFillBar.fillAmount = Mathf.InverseLerp(-100f, 100f, temp);
                furnaceFillBar.color = ResourceSystem.Instance?.GetTempColor() ?? Color.white;
            }
        }

        private void HandleGateHpChanged(int hp, int maxHp)
        {
            float ratio = maxHp > 0 ? (float)hp / maxHp : 0f;

            if (gateHpText) gateHpText.text = $"{hp}/{maxHp}";
            if (gateHpBar)
            {
                gateHpBar.fillAmount = ratio;
                gateHpBar.color = CityGateSystem.Instance?.GetHpColor() ?? Color.green;
            }
            if (gateIcon && CityGateSystem.Instance != null)
                gateIcon.color = CityGateSystem.Instance.GetHpColor();
        }

        private void HandleDayStarted(int dayNumber)
        {
            UpdatePhaseDisplay();
        }

        private void HandleNightStarted(int dayNumber)
        {
            UpdatePhaseDisplay();
        }

        private void HandlePlayerJoinedForCount(SurvivalPlayerJoinedData data)
        {
            UpdatePlayerCount();
        }

        private void HandleTimeTick(float remaining)
        {
            if (timerText)
            {
                int min = Mathf.FloorToInt(remaining / 60f);
                int sec = Mathf.FloorToInt(remaining % 60f);
                timerText.text = $"{min:00}:{sec:00}";
                timerText.color = remaining <= 30f ? Color.red : Color.white;
            }
        }

        private void UpdatePhaseDisplay()
        {
            var dn = DayNightCycleManager.Instance;
            if (!phaseText) return;

            // §36 MVP：若已收到 world_clock_tick/season_state → 显示组合"堡垒 X / 赛季 Y (主题)"
            // 否则退化为旧版"第 N 天 · 白天/夜晚"
            bool hasSeasonData = _seasonDay > 0 && !string.IsNullOrEmpty(_themeId);
            bool hasFortressDay = _fortressDay > 0;

            if (hasSeasonData || hasFortressDay)
            {
                string themeName = SurvivalGameManager.GetSeasonThemeName(_themeId);
                string fortressStr = _fortressDay > 0 ? $"堡垒 {_fortressDay}" : "";
                string seasonStr   = _seasonDay > 0
                    ? (string.IsNullOrEmpty(themeName) ? $"赛季 D{_seasonDay}" : $"赛季 D{_seasonDay}({themeName})")
                    : "";
                phaseText.text = string.IsNullOrEmpty(fortressStr)
                    ? seasonStr
                    : (string.IsNullOrEmpty(seasonStr) ? fortressStr : $"{fortressStr} / {seasonStr}");
            }
            else if (dn != null)
            {
                phaseText.text = dn.GetPhaseDisplayName();
            }

            // 夜晚时文字变冰蓝（依旧沿用 DayNightCycleManager 的判定，若未初始化保持默认白）
            if (dn != null)
            {
                phaseText.color = dn.IsNight
                    ? new Color(0.6f, 0.85f, 1f)
                    : new Color(1f, 0.95f, 0.6f);
            }
        }

        // ==================== §36 全服同步事件处理 ====================

        /// <summary>world_clock_tick：1Hz 更新顶部计时器 + 缓存 phase/seasonDay/themeId。
        /// 注意：DayNightCycleManager.OnTimeTick 已驱动 timerText；本方法仅在 tick 携带
        /// phaseRemainingSec 且 DayNightCycleManager 未初始化时兜底写入。</summary>
        private void HandleWorldClockTick(WorldClockTickData data)
        {
            if (data == null) return;
            _seasonDay = data.seasonDay;
            _themeId   = data.themeId;

            // 兜底：若 DayNightCycleManager 未就绪（如纯 §36 服务端模式），用 world_clock_tick 写计时器
            if (timerText && DayNightCycleManager.Instance == null && data.phaseRemainingSec > 0)
            {
                int min = Mathf.FloorToInt(data.phaseRemainingSec / 60f);
                int sec = data.phaseRemainingSec % 60;
                timerText.text = $"{min:00}:{sec:00}";
                timerText.color = data.phaseRemainingSec <= 30 ? Color.red : Color.white;
            }
            UpdatePhaseDisplay();
        }

        private void HandleSeasonState(SeasonStateData data)
        {
            if (data == null) return;
            _seasonDay = data.seasonDay;
            _themeId   = data.themeId;
            UpdatePhaseDisplay();
        }

        private void HandleFortressDayChanged(FortressDayChangedData data)
        {
            if (data == null) return;
            _fortressDay = data.newFortressDay;
            if (data.seasonDay > 0) _seasonDay = data.seasonDay;
            UpdatePhaseDisplay();
        }

        private void UpdatePlayerCount()
        {
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null || playerCountText == null) return;

            // 助威模式 §33：有助威者时分开显示守护者/助威人数
            if (_supporterCount > 0)
                playerCountText.text = $"守护者:{sgm.TotalPlayers}  助威:{_supporterCount}";
            else
                playerCountText.text = $"参与:{sgm.TotalPlayers}人";
        }

        /// <summary>助威模式 §33：服务器推送 supporter_joined 时更新助威人数</summary>
        public void UpdateSupporterCount(int count)
        {
            _supporterCount = count;
            UpdatePlayerCount();
        }

        /// <summary>积分池实时刷新：每5秒由 resource_update 触发</summary>
        private void HandleScorePoolUpdated(int pool)
        {
            if (!scorePoolText) return;

            // 格式：<1万 显示整数；≥1万 显示 X.X万
            string poolStr = pool >= 10000
                ? $"{pool / 10000f:F1}万"
                : pool.ToString();
            scorePoolText.text = $"奖池:{poolStr}";

            // 数值上涨时触发弹跳动画（视觉反馈奖池增长）
            if (pool > _prevScorePool)
            {
                scorePoolText.color = new Color(1f, 0.85f, 0.1f); // 金色高亮
                StartCoroutine(GiftEffectSystem.ShakeScale(scorePoolText.transform, 1, 0.25f, 1.2f));
                StartCoroutine(FadeToWhite(scorePoolText, 1.5f));
            }
            _prevScorePool = pool;
        }

        private System.Collections.IEnumerator FadeToWhite(TextMeshProUGUI label, float duration)
        {
            float t = 0f;
            Color gold  = new Color(1f, 0.85f, 0.1f);
            Color white = Color.white;
            while (t < duration)
            {
                t += Time.deltaTime;
                if (label) label.color = Color.Lerp(gold, white, t / duration);
                yield return null;
            }
            if (label) label.color = white;
        }
    }
}
