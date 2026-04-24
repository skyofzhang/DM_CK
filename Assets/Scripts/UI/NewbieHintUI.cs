using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §34 B7 新手引导（🆕 v1.27+ audit-r3/P1）
    ///
    /// 订阅两路事件：
    ///   1. OnNewbieWelcome (单播，本人): 显示 30s 浅色横幅
    ///      "发送 1/2/3/4 弹幕指挥矿工采集吧！"（通过 ModalRegistry.RequestB 排队）
    ///   2. OnFirstBarrage  (广播，全房): 显示 3s "XXX 发送第一条弹幕！"（浅绿）
    ///
    /// 挂载规则（CLAUDE.md #7）：挂 GameUIPanel 子 GO（常驻激活），不在 Awake 中 SetActive(false)。
    /// 字体：Alibaba 优先 + ChineseFont SDF fallback。
    ///
    /// Inspector 必填：
    ///   _welcomeLabel — 浅色横幅 TMP（欢迎新玩家）
    ///   _barrageLabel — 浅绿 toast TMP（首次弹幕庆祝）
    /// </summary>
    public class NewbieHintUI : MonoBehaviour
    {
        [Header("Welcome 横幅 TMP（浅色）")]
        [SerializeField] private TMP_Text _welcomeLabel;

        [Header("First barrage Toast TMP（浅绿）")]
        [SerializeField] private TMP_Text _barrageLabel;

        private const float BARRAGE_HOLD     = 3.0f;
        private const float BARRAGE_FADE_OUT = 0.3f;
        private const float WELCOME_FADE_OUT = 0.5f;

        private static readonly Color WELCOME_COLOR      = new Color(1f, 0.95f, 0.7f, 1f);   // 浅米黄
        private static readonly Color BARRAGE_TOAST_COLOR = new Color(0.5f, 1f, 0.5f, 1f);   // 浅绿

        // 字体路径（与其他 UI 统一）
        private const string AlibabaFontPath = "Fonts/AlibabaPuHuiTi-3-85-Bold SDF";
        private const string ChineseFontPath = "Fonts/ChineseFont SDF";

        // ModalRegistry B 类 id
        private const string MODAL_B_ID_WELCOME = "newbie_welcome_banner";

        private bool      _subscribed;
        private Coroutine _welcomeRun;
        private Coroutine _barrageRun;

        // ── 生命周期 ──────────────────────────────────────────────────────

        private void Start()
        {
            EnsureFonts();
            if (_welcomeLabel != null)
            {
                _welcomeLabel.text  = "";
                _welcomeLabel.color = WELCOME_COLOR;
            }
            if (_barrageLabel != null)
            {
                _barrageLabel.text  = "";
                _barrageLabel.color = BARRAGE_TOAST_COLOR;
            }
            TrySubscribe();
        }

        private void OnEnable()  { TrySubscribe(); }
        private void OnDisable() { Unsubscribe(); }
        private void OnDestroy()
        {
            Unsubscribe();
            if (_welcomeRun != null) { StopCoroutine(_welcomeRun); _welcomeRun = null; }
            if (_barrageRun != null) { StopCoroutine(_barrageRun); _barrageRun = null; }
            // 兜底释放 Modal B
            ModalRegistry.ReleaseB(MODAL_B_ID_WELCOME);
        }

        private void Update()
        {
            if (!_subscribed) TrySubscribe();
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;
            sgm.OnNewbieWelcome += HandleNewbieWelcome;
            sgm.OnFirstBarrage  += HandleFirstBarrage;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnNewbieWelcome -= HandleNewbieWelcome;
                sgm.OnFirstBarrage  -= HandleFirstBarrage;
            }
            _subscribed = false;
        }

        private void EnsureFonts()
        {
            var font = Resources.Load<TMP_FontAsset>(AlibabaFontPath);
            if (font == null) font = Resources.Load<TMP_FontAsset>(ChineseFontPath);
            if (font == null) return;
            if (_welcomeLabel != null && _welcomeLabel.font != font) _welcomeLabel.font = font;
            if (_barrageLabel != null && _barrageLabel.font != font) _barrageLabel.font = font;
        }

        // ── 事件回调 ──────────────────────────────────────────────────────

        private void HandleNewbieWelcome(NewbieWelcomeData data)
        {
            if (data == null) return;
            if (_welcomeLabel == null) return;

            string hint = string.IsNullOrEmpty(data.hint)
                ? "发送 1/2/3/4 弹幕指挥矿工采集吧！"
                : data.hint;
            int ttl = data.ttlSec > 0 ? data.ttlSec : 30;

            _welcomeLabel.text  = "欢迎新朋友！" + hint;
            _welcomeLabel.color = WELCOME_COLOR;

            // ModalRegistry B 类排队（非阻塞）—— 使用 id 版本 + onDismiss 兜底
            ModalRegistry.RequestB(MODAL_B_ID_WELCOME, () => {
                // onDismiss 回调在被替换或显式 ReleaseB 时触发；此处无需额外动作
            });

            if (_welcomeRun != null) StopCoroutine(_welcomeRun);
            _welcomeRun = StartCoroutine(WelcomeHoldAndFade(ttl));
        }

        private void HandleFirstBarrage(FirstBarrageData data)
        {
            if (data == null) return;
            if (_barrageLabel == null) return;

            string name = string.IsNullOrEmpty(data.playerName) ? "观众" : data.playerName;
            _barrageLabel.text  = $"{name} 发送第一条弹幕！";
            _barrageLabel.color = BARRAGE_TOAST_COLOR;

            if (_barrageRun != null) StopCoroutine(_barrageRun);
            _barrageRun = StartCoroutine(BarrageHoldAndFade());
        }

        // ── 协程 ──────────────────────────────────────────────────────

        private IEnumerator WelcomeHoldAndFade(int ttlSec)
        {
            if (_welcomeLabel != null)
            {
                var c = _welcomeLabel.color; c.a = 1f; _welcomeLabel.color = c;
            }

            yield return new WaitForSecondsRealtime(ttlSec);

            float t = 0f;
            while (t < WELCOME_FADE_OUT)
            {
                t += Time.unscaledDeltaTime;
                if (_welcomeLabel != null)
                {
                    var c = _welcomeLabel.color;
                    c.a = 1f - Mathf.Clamp01(t / WELCOME_FADE_OUT);
                    _welcomeLabel.color = c;
                }
                yield return null;
            }

            if (_welcomeLabel != null)
            {
                _welcomeLabel.text = "";
                var c = _welcomeLabel.color; c.a = 1f; _welcomeLabel.color = c;
            }
            ModalRegistry.ReleaseB(MODAL_B_ID_WELCOME);
            _welcomeRun = null;
        }

        private IEnumerator BarrageHoldAndFade()
        {
            if (_barrageLabel != null)
            {
                var c = _barrageLabel.color; c.a = 1f; _barrageLabel.color = c;
            }

            yield return new WaitForSecondsRealtime(BARRAGE_HOLD);

            float t = 0f;
            while (t < BARRAGE_FADE_OUT)
            {
                t += Time.unscaledDeltaTime;
                if (_barrageLabel != null)
                {
                    var c = _barrageLabel.color;
                    c.a = 1f - Mathf.Clamp01(t / BARRAGE_FADE_OUT);
                    _barrageLabel.color = c;
                }
                yield return null;
            }

            if (_barrageLabel != null)
            {
                _barrageLabel.text = "";
                var c = _barrageLabel.color; c.a = 1f; _barrageLabel.color = c;
            }
            _barrageRun = null;
        }
    }
}
