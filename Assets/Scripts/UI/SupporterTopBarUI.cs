using UnityEngine;
using TMPro;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §33.6.1 助威模式 —— 顶部并存守护者/助威人数显示
    ///
    /// 订阅 SurvivalGameManager.OnSupporterJoined / OnSupporterPromoted。
    ///
    /// 设计目标：与既有 SurvivalTopBarUI.playerCountText（参与:N人）并存，不修改既有 SurvivalTopBarUI。
    /// supporterCount &gt; 0 → 自身 TMP 显示 "守护者:{guardianCount}  助威:{supporterCount}"
    /// supporterCount == 0 → 自身 TMP 清空（让 SurvivalTopBarUI 原始 "参与:N人" 显示生效）
    ///
    /// 挂载规则（CLAUDE.md #7）：挂 GameUIPanel（常驻激活），不在 Awake 中 SetActive(false)。
    /// Inspector 必填：
    ///   _label — 并存显示的 TMP（独立于 SurvivalTopBarUI.playerCountText）
    /// </summary>
    public class SupporterTopBarUI : MonoBehaviour
    {
        [Header("独立并存 TMP（与 SurvivalTopBarUI.playerCountText 并存，不共用）")]
        [SerializeField] private TMP_Text _label;

        private bool _subscribed;

        // 内部状态
        private int _supporterCount = 0;

        // Fallback 守护者人数（若 SurvivalGameManager.TotalPlayers 不可用时使用）
        // TODO：接入 §33.5 服务端 guardianCount 字段后替换此 fallback
        private const int GUARDIAN_FALLBACK = 12;

        // ── 生命周期 ──────────────────────────────────────────────────────

        private void Start()
        {
            if (_label != null) _label.text = "";
            TrySubscribe();
        }

        private void OnEnable()  { TrySubscribe(); }
        private void OnDisable() { Unsubscribe(); }
        private void OnDestroy() { Unsubscribe(); }

        private void Update()
        {
            if (!_subscribed) TrySubscribe();
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;
            sgm.OnSupporterJoined   += HandleSupporterJoined;
            sgm.OnSupporterPromoted += HandleSupporterPromoted;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnSupporterJoined   -= HandleSupporterJoined;
                sgm.OnSupporterPromoted -= HandleSupporterPromoted;
            }
            _subscribed = false;
        }

        // ── 事件回调 ──────────────────────────────────────────────────────

        private void HandleSupporterJoined(SupporterJoinedData data)
        {
            if (data == null) return;
            _supporterCount = data.supporterCount;
            Refresh();
        }

        private void HandleSupporterPromoted(SupporterPromotedData data)
        {
            // 替补不改变总助威人数，但可能改变具体身份 → 直接刷新当前显示
            Refresh();
        }

        // ── 显示 ──────────────────────────────────────────────────────

        private void Refresh()
        {
            if (_label == null) return;

            if (_supporterCount <= 0)
            {
                // 助威人数归零：清空自身，让 SurvivalTopBarUI 原始 "参与:N人" 生效
                _label.text = "";
                return;
            }

            int guardianCount = GetGuardianCount();
            _label.text = $"守护者:{guardianCount}  助威:{_supporterCount}";
        }

        private static int GetGuardianCount()
        {
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return GUARDIAN_FALLBACK;
            int n = sgm.TotalPlayers;
            return n > 0 ? n : GUARDIAN_FALLBACK;
        }
    }
}
