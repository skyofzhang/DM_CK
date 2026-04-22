using System.Collections;
using UnityEngine;
using TMPro;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §33.5 助威模式 —— AFK 替补跑马灯（金黄色）
    ///
    /// 订阅 SurvivalGameManager.OnSupporterPromoted。
    /// 文案：{newPlayerName} 替补上场！{oldPlayerName} 转为助威
    /// 颜色：金黄 (1, 0.84, 0.2)（与 §33.6.2 的浅紫助威跑马灯区分，强调"升级身份"仪式感）
    /// 显示节拍：5s 停留后 0.3s 淡出；连续触发 → 下一条替换当前（不排队）。
    ///
    /// 挂载规则（CLAUDE.md #7）：挂 GameUIPanel 子 GO（常驻激活），不在 Awake 中 SetActive(false)。
    /// Inspector 必填：
    ///   _label — 跑马灯 TMP（单行）
    /// </summary>
    public class SupporterPromotedMarqueeUI : MonoBehaviour
    {
        [Header("跑马灯 TMP（单行，金黄）")]
        [SerializeField] private TMP_Text _label;

        private const float HOLD     = 5.0f;
        private const float FADE_OUT = 0.3f;

        private static readonly Color PROMOTED_GOLD = new Color(1f, 0.84f, 0.2f, 1f);

        private bool      _subscribed;
        private Coroutine _runCoroutine;

        // ── 生命周期 ──────────────────────────────────────────────────────

        private void Start()
        {
            if (_label != null)
            {
                _label.text  = "";
                _label.color = PROMOTED_GOLD;
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
            sgm.OnSupporterPromoted += HandleSupporterPromoted;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null) sgm.OnSupporterPromoted -= HandleSupporterPromoted;
            _subscribed = false;
        }

        // ── 事件回调 ──────────────────────────────────────────────────────

        private void HandleSupporterPromoted(SupporterPromotedData data)
        {
            if (data == null) return;

            string newName = string.IsNullOrEmpty(data.newPlayerName) ? "匿名" : data.newPlayerName;
            string oldName = string.IsNullOrEmpty(data.oldPlayerName) ? "匿名" : data.oldPlayerName;
            string message = $"{newName} 替补上场！{oldName} 转为助威";

            if (_label != null)
            {
                _label.text  = message;
                _label.color = PROMOTED_GOLD;
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
