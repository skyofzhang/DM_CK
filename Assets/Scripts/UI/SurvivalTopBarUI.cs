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

        // T213：前值追踪，数值增加时触发弹跳动画
        private int   _prevFood, _prevCoal, _prevOre;
        private float _prevTemp;
        private int   _prevScorePool;

        // 助威模式 §33（🆕 v1.27）
        private int _supporterCount = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this) { /* 保留先进入的实例 */ return; }
            Instance = this;
        }

        private void OnDestroy()
        {
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
            if (_subscribed) return;

            // 全サブシステムが揃っている場合のみ購読完了とする
            var res  = ResourceSystem.Instance;
            var gate = CityGateSystem.Instance;
            var dn   = DayNightCycleManager.Instance;
            var sgm  = SurvivalGameManager.Instance;

            // いずれかが未初期化なら次のフレームに持ち越す（_subscribedはtrueにしない）
            if (res == null && gate == null && dn == null && sgm == null) return;

            if (res != null)
            {
                res.OnResourceChanged += HandleResourceChanged;
                // 立即同步当前值
                HandleResourceChanged(res.Food, res.Coal, res.Ore, res.FurnaceTemp);
            }

            if (gate != null)
            {
                gate.OnHpChanged += HandleGateHpChanged;
                HandleGateHpChanged(gate.CurrentHp, gate.MaxHp);
            }

            if (dn != null)
            {
                dn.OnDayStarted    += _ => UpdatePhaseDisplay();
                dn.OnNightStarted  += _ => UpdatePhaseDisplay();
                dn.OnTimeTick      += HandleTimeTick;
                UpdatePhaseDisplay();
            }

            if (sgm != null)
            {
                sgm.OnPlayerJoined     += _ => UpdatePlayerCount();
                sgm.OnScorePoolUpdated += HandleScorePoolUpdated;
                UpdatePlayerCount();
            }

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var res = ResourceSystem.Instance;
            if (res != null) res.OnResourceChanged -= HandleResourceChanged;

            var gate = CityGateSystem.Instance;
            if (gate != null) gate.OnHpChanged -= HandleGateHpChanged;

            var dn = DayNightCycleManager.Instance;
            if (dn != null) dn.OnTimeTick -= HandleTimeTick;

            var sgm = SurvivalGameManager.Instance;
            if (sgm != null) sgm.OnScorePoolUpdated -= HandleScorePoolUpdated;

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
            if (dn != null && phaseText)
            {
                phaseText.text = dn.GetPhaseDisplayName();
                // 夜晚时文字变冰蓝
                phaseText.color = dn.IsNight
                    ? new Color(0.6f, 0.85f, 1f)  // 冰蓝
                    : new Color(1f, 0.95f, 0.6f);  // 暖黄
            }
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
