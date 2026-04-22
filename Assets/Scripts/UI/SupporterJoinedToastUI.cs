using System.Collections;
using UnityEngine;
using TMPro;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §33.6.4 助威模式 —— 新助威者加入 Toast（绿色，广播给全房间观众）
    ///
    /// 订阅 SurvivalGameManager.OnSupporterJoined。
    /// 文案：{playerName} 加入助威！发送弹幕为全队加油
    /// 颜色：绿色 (0.5, 1, 0.5)（与 BarrageMessageUI 的 COLOR_JOIN 风格一致，释放友好信号）
    /// 显示节拍：3s 停留后 0.3s 淡出；连续触发 → 下一条替换当前（不排队）。
    ///
    /// 挂载规则（CLAUDE.md #7）：挂 GameUIPanel 子 GO（常驻激活），不在 Awake 中 SetActive(false)。
    /// 布局建议：参考 GiftNotificationUI 的顶部偏右位置，不遮挡 TopBar/跑马灯。
    /// Inspector 必填：
    ///   _label — Toast TMP（可多行）
    /// </summary>
    public class SupporterJoinedToastUI : MonoBehaviour
    {
        [Header("Toast TMP（可多行，绿色）")]
        [SerializeField] private TMP_Text _label;

        private const float HOLD     = 3.0f;
        private const float FADE_OUT = 0.3f;

        private static readonly Color TOAST_GREEN = new Color(0.5f, 1f, 0.5f, 1f);

        private bool      _subscribed;
        private Coroutine _runCoroutine;

        // ── 生命周期 ──────────────────────────────────────────────────────

        private void Start()
        {
            if (_label != null)
            {
                _label.text  = "";
                _label.color = TOAST_GREEN;
            }
            TrySubscribe();
        }

        private void OnEnable()  { TrySubscribe(); }
        private void OnDisable() { Unsubscribe(); }
        private void OnDestroy()
        {
            Unsubscribe();
            if (_runCoroutine != null) { StopCoroutine(_runCoroutine); _runCoroutine = null; }
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
            sgm.OnSupporterJoined += HandleSupporterJoined;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null) sgm.OnSupporterJoined -= HandleSupporterJoined;
            _subscribed = false;
        }

        // ── 事件回调 ──────────────────────────────────────────────────────

        private void HandleSupporterJoined(SupporterJoinedData data)
        {
            if (data == null) return;

            string name = string.IsNullOrEmpty(data.playerName) ? "匿名" : data.playerName;
            string message = $"{name} 加入助威！发送弹幕为全队加油";

            if (_label != null)
            {
                _label.text  = message;
                _label.color = TOAST_GREEN;
            }

            if (_runCoroutine != null) StopCoroutine(_runCoroutine);
            _runCoroutine = StartCoroutine(HoldAndFade());
        }

        private IEnumerator HoldAndFade()
        {
            if (_label != null)
            {
                var c = _label.color; c.a = 1f; _label.color = c;
            }

            yield return new WaitForSecondsRealtime(HOLD);

            float t = 0f;
            while (t < FADE_OUT)
            {
                t += Time.unscaledDeltaTime;
                if (_label != null)
                {
                    var c = _label.color;
                    c.a = 1f - Mathf.Clamp01(t / FADE_OUT);
                    _label.color = c;
                }
                yield return null;
            }

            if (_label != null)
            {
                _label.text = "";
                var c = _label.color; c.a = 1f; _label.color = c;
            }
            _runCoroutine = null;
        }
    }
}
