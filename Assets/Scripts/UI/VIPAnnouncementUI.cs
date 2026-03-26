using UnityEngine;
using UnityEngine.Video;
using DrscfZ.Core;
using DrscfZ.Systems;

namespace DrscfZ.UI
{
    /// <summary>
    /// VIP 入场事件路由
    ///
    /// 职责：订阅 CampSystem.OnVIPJoined 事件，根据排名选择对应 WebM VideoClip，
    /// 交由 GiftAnimationUI.Instance.ShowVIPEffect() 统一播放。
    ///
    /// 与礼物特效使用同一套 WebM 播放逻辑（VideoPlayer → RenderTexture → RawImage），
    /// 本脚本不持有 VideoPlayer，不做任何渲染，只做事件路由。
    ///
    /// 挂载规则（Rule #7）：挂在始终激活的 Canvas 子对象上。
    /// </summary>
    public class VIPAnnouncementUI : MonoBehaviour
    {
        [Header("VIP 入场视频 - 周榜")]
        [SerializeField] private VideoClip weeklyRank1Clip;
        [SerializeField] private VideoClip weeklyRank2Clip;
        [SerializeField] private VideoClip weeklyRank3Clip;
        [SerializeField] private VideoClip weeklyRank4_10Clip;
        [SerializeField] private VideoClip weeklyRank11_20Clip;

        [Header("VIP 入场视频 - 月榜")]
        [SerializeField] private VideoClip monthlyRank1Clip;
        [SerializeField] private VideoClip monthlyRank2Clip;
        [SerializeField] private VideoClip monthlyRank3Clip;
        [SerializeField] private VideoClip monthlyRank4_10Clip;
        [SerializeField] private VideoClip monthlyRank11_20Clip;

        private CampSystem _campSystem;
        private bool       _subscribed;

        // ==================== 生命周期 ====================

        private void Start()    => TrySubscribe();
        private void OnEnable() => TrySubscribe();

        private void TrySubscribe()
        {
            if (_subscribed) return;
            _campSystem = FindObjectOfType<CampSystem>();
            if (_campSystem == null) return;

            _campSystem.OnVIPJoined += HandleVIPJoined;
            _subscribed = true;
        }

        private void OnDestroy()
        {
            if (_campSystem != null)
                _campSystem.OnVIPJoined -= HandleVIPJoined;
        }

        // ==================== VIP 入场处理 ====================

        private void HandleVIPJoined(PlayerJoinedData data)
        {
            if (!gameObject.activeInHierarchy) return;
            if (!SettingsPanelUI.VIPVideoEnabled) return;

            var anim = GiftAnimationUI.Instance;
            if (anim == null)
            {
                Debug.LogWarning("[VIPAnnouncementUI] GiftAnimationUI.Instance 为 null，无法播放入场视频");
                return;
            }

            float playDuration;
            var clip = GetEntryClip(data.vipType, data.vipRank, out playDuration);
            if (clip == null) return; // 无对应视频时直接跳过

            string title = !string.IsNullOrEmpty(data.vipTitle)
                ? data.vipTitle
                : $"第{data.vipRank}名";

            anim.ShowVIPEffect(clip, data.playerName, title, playDuration);
        }

        /// <summary>根据 vipType / vipRank 选择对应视频及播放时长</summary>
        private VideoClip GetEntryClip(string vipType, int vipRank, out float playDuration)
        {
            playDuration = 8f;
            if (vipRank <= 0 || vipRank > 20 || string.IsNullOrEmpty(vipType))
                return null;

            bool isMonthly = vipType == "monthly";

            if (vipRank == 1)
            {
                playDuration = isMonthly ? 11f : 14f;
                return isMonthly ? monthlyRank1Clip : weeklyRank1Clip;
            }
            if (vipRank == 2)
            {
                playDuration = 13f;
                return isMonthly ? monthlyRank2Clip : weeklyRank2Clip;
            }
            if (vipRank == 3)
            {
                playDuration = isMonthly ? 9f : 11f;
                return isMonthly ? monthlyRank3Clip : weeklyRank3Clip;
            }
            if (vipRank <= 10)
            {
                playDuration = 6f;
                return isMonthly ? monthlyRank4_10Clip : weeklyRank4_10Clip;
            }
            if (vipRank <= 20)
            {
                playDuration = isMonthly ? 5f : 7f;
                return isMonthly ? monthlyRank11_20Clip : weeklyRank11_20Clip;
            }
            return null;
        }

        // ==================== 外部调用 ====================

        /// <summary>
        /// 强制停止（由 GameManager 在 game_ended/ResetGame 时调用）。
        /// VIP 视频由 GiftAnimationUI 管理，此处无本地状态需清理。
        /// </summary>
        public void ForceCleanup()
        {
            // VIP 视频生命周期归 GiftAnimationUI 管理，无需在此清理
        }
    }
}
