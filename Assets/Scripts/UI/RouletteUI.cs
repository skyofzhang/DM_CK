using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using DrscfZ.Core;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §24.4 主播事件轮盘 UI（MVP 版）
    ///
    /// 职责：
    ///   - 充能进度显示（300s），就绪时按钮从灰→金
    ///   - Spin → 3s 转轴动画（EaseOutQuint）→ 定格
    ///   - 定格 5s 倒计时自动 apply，或主播点击"执行"提前 apply
    ///   - aurora/time_freeze 等视觉反馈（部分复用现有 glow_all/frozen_all 路径）
    ///
    /// 挂载（Rule #7）：挂在 Canvas（always-active），_panel 初始 inactive。
    ///
    /// Inspector 字段（由人工在 Editor 中绑定）：
    ///   _panel           — RoulettePanel 根（初始 inactive）
    ///   btnSpin          — 🎰 启动轮盘按钮（常驻在 BroadcasterPanel，可为 null）
    ///   btnSpinBg        — btnSpin 的 Image 组件（灰/金切换）
    ///   btnSpinText      — btnSpin 的文字（充能中显示剩余秒数）
    ///   _card1Text/_card2Text/_card3Text — 3 张卡片中央文字
    ///   _descText        — 定格后效果描述
    ///   btnConfirm       — 定格后"执行"按钮
    ///   _countdownText   — 5s 自动 apply 倒计时文字
    ///
    /// 若 _panel 未绑定（场景未预创建面板），所有显示逻辑降级为 Debug.Log。
    /// </summary>
    public class RouletteUI : MonoBehaviour
    {
        public static RouletteUI Instance { get; private set; }

        // ==================== Inspector 字段 ====================

        [Header("面板根（初始 inactive）")]
        [SerializeField] private GameObject _panel;

        [Header("🎰 启动按钮（常驻 BroadcasterPanel）")]
        [SerializeField] private Button    btnSpin;
        [SerializeField] private Image     btnSpinBg;
        [SerializeField] private TMP_Text  btnSpinText;

        [Header("3 张卡片文字（中央 index=1 为定格）")]
        [SerializeField] private TMP_Text  _card1Text;
        [SerializeField] private TMP_Text  _card2Text;
        [SerializeField] private TMP_Text  _card3Text;

        [Header("效果描述 / 执行按钮 / 倒计时")]
        [SerializeField] private TMP_Text  _descText;
        [SerializeField] private Button    btnConfirm;
        [SerializeField] private TMP_Text  _countdownText;

        // ==================== 颜色常量 ====================

        private static readonly Color BtnSpinReady    = new Color(1.00f, 0.84f, 0.00f, 1f);  // 金色
        private static readonly Color BtnSpinCooldown = new Color(0.35f, 0.40f, 0.55f, 1f);  // 灰蓝

        // ==================== ModalRegistry A 类 id（§17.16 audit-r5）====================
        private const string MODAL_A_ID = "roulette_panel";

        // ==================== 状态机 ====================

        private enum State { Idle, Charging, Ready, Spinning, Locked }
        private State _state = State.Idle;

        // 充能剩余秒数（仅 Charging 状态使用，来自 RouletteReadyData.readyAt）
        private long  _readyAtUnixMs = -1;

        // 当前抽卡结果 / 定格倒计时
        private RouletteResultData _currentResult;
        private Coroutine          _rollCoroutine;
        private Coroutine          _autoApplyCoroutine;

        // 订阅状态
        private bool _subscribed = false;

        // ==================== 卡名中文表 ====================

        /// <summary>6 张卡的中文显示名（定格/展示通用）</summary>
        private static string CardDisplayName(string cardId)
        {
            return cardId switch
            {
                "elite_raid"     => "精英来袭",
                "time_freeze"    => "时间暂停",
                "double_contrib" => "双倍贡献",
                "mystery_trader" => "神秘商人",
                "meteor_shower"  => "流星雨",
                "aurora"         => "极光降临",
                _                => cardId ?? "??"
            };
        }

        /// <summary>定格后的效果描述文案</summary>
        private static string CardDescription(string cardId)
        {
            return cardId switch
            {
                "elite_raid"     => "直面精英，方显勇者\n刷 1 只精英怪，击杀奖励 +500 分",
                "time_freeze"    => "时空凝滞，喘息之机\n全场怪物冻结 8 秒",
                "double_contrib" => "英雄辈出的时刻\n全员贡献 ×2，持续 60 秒",
                "mystery_trader" => "商队路过堡垒\n30s 内二选一资源交换",
                "meteor_shower"  => "天降陨石\n15s 内随机怪物受伤，场上无怪则修城门",
                "aurora"         => "极光守护，众神庇佑\n全矿工满血+效率×1.5 60s+城门+200HP",
                _                => ""
            };
        }

        // ==================== 卡池（随机转轴动画用）====================

        private static readonly string[] AllCardIds = {
            "elite_raid", "time_freeze", "double_contrib",
            "mystery_trader", "meteor_shower", "aurora"
        };

        // ==================== 生命周期 ====================

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            if (_panel != null) _panel.SetActive(false);
        }

        private void Start()
        {
            if (btnSpin    != null) btnSpin.onClick.AddListener(OnSpinClicked);
            if (btnConfirm != null) btnConfirm.onClick.AddListener(OnConfirmClicked);

            SetSpinButtonState(State.Idle);
            TrySubscribe();
        }

        private void OnEnable()  { TrySubscribe(); }
        private void OnDisable() { Unsubscribe();  }
        private void OnDestroy() { Unsubscribe(); if (Instance == this) Instance = null; }

        private void Update()
        {
            // 补订阅（SGM 晚初始化的兜底）
            if (!_subscribed) TrySubscribe();

            // 充能倒计时显示
            if (_state == State.Charging && _readyAtUnixMs > 0 && btnSpinText != null)
            {
                long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long remainMs = _readyAtUnixMs - nowMs;
                if (remainMs <= 0)
                {
                    SetSpinButtonState(State.Ready);
                }
                else
                {
                    int sec = Mathf.CeilToInt(remainMs / 1000f);
                    btnSpinText.text = $"{sec}s";
                }
            }
        }

        // ==================== 订阅 ====================

        private void TrySubscribe()
        {
            if (_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;
            sgm.OnRouletteReady        += OnRouletteReady;
            sgm.OnRouletteResult       += OnRouletteResult;
            sgm.OnRouletteEffectEnded  += OnRouletteEffectEnded;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnRouletteReady       -= OnRouletteReady;
                sgm.OnRouletteResult      -= OnRouletteResult;
                sgm.OnRouletteEffectEnded -= OnRouletteEffectEnded;
            }
            _subscribed = false;
        }

        // ==================== 对外接口 ====================

        /// <summary>由 BroadcasterPanel 🎰 按钮点击时调用。面板仅在转轴/定格阶段显示，Idle/Charging/Ready 由 BroadcasterPanel 承担。</summary>
        public void OpenPanel()
        {
            // Ready 状态下点击 = 发起 spin（由 btnSpin 的 OnSpinClicked 处理更直接）
            if (_state == State.Ready)
            {
                OnSpinClicked();
            }
            else
            {
                Debug.Log($"[RouletteUI] OpenPanel 但 state={_state}，忽略（非 Ready）");
            }
        }

        // ==================== 事件回调 ====================

        /// <summary>服务端充能进度同步。readyAt=-1 → 已就绪；>0 → 剩余时间</summary>
        private void OnRouletteReady(RouletteReadyData data)
        {
            if (data == null) return;
            _readyAtUnixMs = data.readyAt;

            if (data.readyAt < 0)
            {
                // 立即就绪
                SetSpinButtonState(State.Ready);
            }
            else
            {
                long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                if (data.readyAt <= nowMs) SetSpinButtonState(State.Ready);
                else                        SetSpinButtonState(State.Charging);
            }
        }

        /// <summary>服务端返回 spin 结果 → 播转轴动画 3s → 定格</summary>
        private void OnRouletteResult(RouletteResultData data)
        {
            if (data == null) return;
            _currentResult = data;

            // 兜底：displayedCards 长度异常时仍能播
            if (data.displayedCards == null || data.displayedCards.Length < 3)
            {
                Debug.LogWarning($"[RouletteUI] displayedCards 长度异常：{(data.displayedCards == null ? -1 : data.displayedCards.Length)}");
            }

            // 切换到 Spinning，显示面板（§17.16 走 ModalRegistry A 类互斥，priority=70 主播轮盘高优）
            SetSpinButtonState(State.Spinning);
            if (_panel != null)
            {
                _panel.SetActive(true);
                ModalRegistry.Request(MODAL_A_ID, 70, () =>
                {
                    // 被更高优先级抢占时自动关闭面板
                    if (_panel != null) _panel.SetActive(false);
                });
            }

            if (_rollCoroutine != null) StopCoroutine(_rollCoroutine);
            _rollCoroutine = StartCoroutine(Roll(data));
        }

        /// <summary>服务端效果结束 → 清 UI</summary>
        private void OnRouletteEffectEnded(RouletteEffectEndedData data)
        {
            if (data == null) return;
            // 面板可能已关闭（如 time_freeze 8s 内玩家手动关面板），这里只是兜底（§17.16 走 ModalRegistry）
            if (_panel != null)
            {
                _panel.SetActive(false);
                ModalRegistry.Release(MODAL_A_ID);
            }

            // 回到 Charging（服务端清零重新计时，readyAt 随后会推 RouletteReady）
            _currentResult = null;
            if (_state == State.Locked || _state == State.Spinning)
                SetSpinButtonState(State.Charging);

            Debug.Log($"[RouletteUI] effect_ended: {data.cardId}");
        }

        // ==================== 转轴动画（EaseOutQuint）====================

        private IEnumerator Roll(RouletteResultData data)
        {
            const float dur = 3f;
            float  t         = 0f;
            float  lastSwitch = 0f;

            // EaseOutQuint 的相对速率：进度越靠后切换间隔越长
            // 切换间隔从 0.05s（快速闪烁）过渡到 0.35s（接近停止）
            while (t < dur)
            {
                float progress     = Mathf.Clamp01(t / dur);
                float easedOutQuint = 1f - Mathf.Pow(1f - progress, 5f);  // [0,1]
                float switchInterval = Mathf.Lerp(0.05f, 0.35f, easedOutQuint);

                if (t - lastSwitch >= switchInterval)
                {
                    lastSwitch = t;
                    // 3 张卡文字各自随机切换为一个卡名（视觉效果是滚动）
                    SetCardText(_card1Text, RandomCardId());
                    SetCardText(_card2Text, RandomCardId());
                    SetCardText(_card3Text, RandomCardId());
                }

                t += Time.deltaTime;
                yield return null;
            }

            // 定格：显示 displayedCards[0..2]，中间（index=1）为 cardId
            string[] display = (data.displayedCards != null && data.displayedCards.Length >= 3)
                ? data.displayedCards
                : new string[] { RandomCardId(), data.cardId, RandomCardId() };

            SetCardText(_card1Text, display[0]);
            SetCardText(_card2Text, display[1]);
            SetCardText(_card3Text, display[2]);

            // 中间卡片高亮（放大字号 + 金色）
            if (_card2Text != null)
            {
                _card2Text.fontStyle = FontStyles.Bold;
                _card2Text.color     = new Color(1f, 0.85f, 0.1f);
            }

            // 显示效果描述
            if (_descText != null)
            {
                _descText.text = CardDescription(data.cardId);
                _descText.gameObject.SetActive(true);
            }
            if (btnConfirm != null)
                btnConfirm.gameObject.SetActive(true);

            // 进入 Locked 状态，5s 倒计时自动 apply
            SetSpinButtonState(State.Locked);
            if (_autoApplyCoroutine != null) StopCoroutine(_autoApplyCoroutine);
            _autoApplyCoroutine = StartCoroutine(AutoApplyCountdown(5f));

            _rollCoroutine = null;
        }

        private IEnumerator AutoApplyCountdown(float seconds)
        {
            float remain = seconds;
            while (remain > 0f)
            {
                if (_countdownText != null)
                    _countdownText.text = $"{Mathf.CeilToInt(remain)}s 后自动执行";
                remain -= Time.deltaTime;
                yield return null;
            }
            // 超时：自动 apply
            DoApply();
        }

        private void SetCardText(TMP_Text target, string cardId)
        {
            if (target == null) return;
            target.text = CardDisplayName(cardId);
            // 滚动过程中清掉高亮
            target.fontStyle = FontStyles.Normal;
            target.color     = Color.white;
        }

        private static string RandomCardId()
        {
            return AllCardIds[UnityEngine.Random.Range(0, AllCardIds.Length)];
        }

        // ==================== 按钮回调 ====================

        private void OnSpinClicked()
        {
            if (_state != State.Ready)
            {
                Debug.Log($"[RouletteUI] 🎰 按钮点击被忽略（state={_state}）");
                return;
            }
            var net = NetworkManager.Instance;
            if (net == null || !net.IsConnected) return;

            long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            net.SendJson($"{{\"type\":\"broadcaster_roulette_spin\",\"data\":{{}},\"timestamp\":{ts}}}");
            Debug.Log("[RouletteUI] 发送 broadcaster_roulette_spin");

            // 进入 Spinning 前等服务端 result 推送，这里不提前切，防误点刷屏
            // 但视觉上禁用按钮，避免连点
            if (btnSpin != null) btnSpin.interactable = false;
        }

        private void OnConfirmClicked()
        {
            DoApply();
        }

        private void DoApply()
        {
            if (_state != State.Locked)
            {
                Debug.Log($"[RouletteUI] DoApply 被忽略（state={_state}）");
                return;
            }

            if (_autoApplyCoroutine != null) { StopCoroutine(_autoApplyCoroutine); _autoApplyCoroutine = null; }

            var net = NetworkManager.Instance;
            if (net != null && net.IsConnected)
            {
                long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                net.SendJson($"{{\"type\":\"broadcaster_roulette_apply\",\"data\":{{}},\"timestamp\":{ts}}}");
                Debug.Log($"[RouletteUI] 发送 broadcaster_roulette_apply (cardId={_currentResult?.cardId})");
            }

            // 关闭面板，回到充能阶段（服务端随后会推 RouletteReady 启动新一轮计时）；§17.16 走 ModalRegistry
            if (_panel != null)
            {
                _panel.SetActive(false);
                ModalRegistry.Release(MODAL_A_ID);
            }
            SetSpinButtonState(State.Charging);

            // aurora 的客户端视觉加强（60s 全员金光）——复用现有 API
            if (_currentResult != null && _currentResult.cardId == "aurora")
            {
                WorkerManager.Instance?.ActivateAllWorkersGlow(60f);
            }
            // time_freeze / 其他效果由服务端后续推送（frozen_all / survival_gift / monster_*）触发视觉

            _currentResult = null;
        }

        // ==================== UI 状态切换 ====================

        private void SetSpinButtonState(State newState)
        {
            _state = newState;

            if (btnSpin != null)
            {
                bool interactable = (newState == State.Ready);
                btnSpin.interactable = interactable;
            }
            if (btnSpinBg != null)
            {
                btnSpinBg.color = (newState == State.Ready) ? BtnSpinReady : BtnSpinCooldown;
            }
            if (btnSpinText != null)
            {
                // 按钮文字用中文，避免部分环境下 emoji 显示为方块（CLAUDE.md 已知踩坑）
                string txt = newState switch
                {
                    State.Ready    => "轮盘",
                    State.Charging => "充能中",
                    State.Spinning => "抽卡中",
                    State.Locked   => "定格",
                    _              => ""
                };
                btnSpinText.text = txt;
            }

            // 定格相关子对象显隐
            if (newState != State.Locked)
            {
                if (btnConfirm    != null) btnConfirm.gameObject.SetActive(false);
                if (_countdownText != null) _countdownText.text = "";
            }
        }
    }
}
