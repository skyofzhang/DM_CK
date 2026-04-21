using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §36.12 通用功能锁定覆盖层（UI 按钮灰化 + 🔒 图标 + "DN 解锁" 提示）。
    ///
    /// 挂载方式（符合 CLAUDE.md 规则 2）：
    ///   - 在场景中的受锁按钮 GameObject 上预挂本组件
    ///   - Inspector 填 featureId + 可选的 btnLabel/lockIcon/unlockHint 引用
    ///   - 根据 SurvivalGameManager.CurrentUnlockedFeatures 在 Awake/Sync 时刷新视觉
    ///   - 不拦截点击（仅视觉反馈；服务端仍会返回 feature_locked 再走 *_failed 路径）
    ///
    /// 订阅：
    ///   - SurvivalGameManager.OnUnlockedFeaturesSync → 全集覆盖刷新
    ///   - SurvivalGameManager.OnNewlyUnlockedFeatures → 若本 featureId 在列表，切回正常
    ///
    /// 设计原则：不碰其他组件（颜色/icon/text 都是可选的），不绑定 Inspector 时降级为 Log。
    /// </summary>
    public class FeatureLockOverlay : MonoBehaviour
    {
        [Header("锁定 feature id（对齐 SurvivalMessageProtocol.FeatureXxx 常量）")]
        [SerializeField] private string _featureId;

        [Header("可选：按钮文字（解锁时恢复、锁定时变灰色）")]
        [SerializeField] private TMP_Text _btnLabel;

        [Header("可选：🔒 图标（锁定时显示，解锁时隐藏）")]
        [SerializeField] private Image _lockIcon;

        [Header("可选：\"DN 解锁\" 提示文本（锁定时显示）")]
        [SerializeField] private TMP_Text _unlockHint;

        [Header("可选：按钮 Image（锁定时变灰；未填则查同 GameObject 的 Button graphic）")]
        [SerializeField] private Image _btnBackground;

        [Header("颜色")]
        [SerializeField] private Color _colorLockedBg     = new Color(0.50f, 0.50f, 0.50f, 0.8f);
        [SerializeField] private Color _colorUnlockedBg   = Color.white;
        [SerializeField] private Color _colorLockedText   = new Color(0.70f, 0.70f, 0.70f, 1f);
        [SerializeField] private Color _colorUnlockedText = Color.white;

        // 缓存正常态颜色（首次锁定时从当前 _btnBackground 读）
        private Color? _origBgColor;
        private Color? _origTextColor;

        public string FeatureId => _featureId;

        private bool _subscribed = false;
        private bool _lastKnownLocked = false; // 首次刷新前保守为 false，避免误闪

        private void Awake()
        {
            // 兜底：若 _btnBackground 未填，尝试自动找 Button graphic
            if (_btnBackground == null)
            {
                var btn = GetComponent<Button>();
                if (btn != null && btn.targetGraphic is Image img) _btnBackground = img;
            }
        }

        private void OnEnable()
        {
            TrySubscribe();
            // OnEnable 时立即按当前缓存刷新一次（避免订阅前已同步过一次错过）
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null && sgm.CurrentUnlockedFeatures != null)
            {
                Apply(sgm.IsFeatureUnlocked(_featureId));
            }
        }

        private void Start()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;
            sgm.OnUnlockedFeaturesSync  += HandleSync;
            sgm.OnNewlyUnlockedFeatures += HandleNewlyUnlocked;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnUnlockedFeaturesSync  -= HandleSync;
                sgm.OnNewlyUnlockedFeatures -= HandleNewlyUnlocked;
            }
            _subscribed = false;
        }

        private void HandleSync(string[] allFeatures)
        {
            bool unlocked = ContainsFeature(allFeatures, _featureId);
            Apply(unlocked);
        }

        private void HandleNewlyUnlocked(string[] newFeatures)
        {
            if (ContainsFeature(newFeatures, _featureId))
            {
                Apply(true);
            }
        }

        private static bool ContainsFeature(string[] list, string featureId)
        {
            if (list == null || string.IsNullOrEmpty(featureId)) return false;
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i] == featureId) return true;
            }
            return false;
        }

        /// <summary>应用视觉：unlocked=true → 恢复正常色；unlocked=false → 灰化 + 显示 🔒 + 提示。</summary>
        public void Apply(bool unlocked)
        {
            bool locked = !unlocked;
            _lastKnownLocked = locked;

            // 缓存原色（首次进入且未填原值时）
            if (locked && _origBgColor == null && _btnBackground != null)
                _origBgColor = _btnBackground.color;
            if (locked && _origTextColor == null && _btnLabel != null)
                _origTextColor = _btnLabel.color;

            if (_btnBackground != null)
            {
                _btnBackground.color = locked
                    ? _colorLockedBg
                    : (_origBgColor ?? _colorUnlockedBg);
            }

            if (_btnLabel != null)
            {
                _btnLabel.color = locked
                    ? _colorLockedText
                    : (_origTextColor ?? _colorUnlockedText);
            }

            if (_lockIcon != null)
            {
                _lockIcon.gameObject.SetActive(locked);
            }

            if (_unlockHint != null)
            {
                _unlockHint.gameObject.SetActive(locked);
                if (locked)
                {
                    int unlockDay = GetUnlockDay(_featureId);
                    _unlockHint.text = unlockDay > 0 ? $"D{unlockDay} 解锁" : "未解锁";
                }
            }
        }

        /// <summary>本地回退表（§36.12 FEATURE_UNLOCK_DAY 的客户端镜像，仅用于文案提示）。
        /// 真正的锁态以 SurvivalGameManager.CurrentUnlockedFeatures 为准。</summary>
        private static int GetUnlockDay(string featureId)
        {
            if (string.IsNullOrEmpty(featureId)) return 0;
            switch (featureId)
            {
                case "gate_upgrade_basic": return 1;
                case "roulette":           return 1;
                case "broadcaster_boost":  return 2;
                case "shop":               return 2;
                case "building":           return 3;
                case "gate_upgrade_high":  return 4;
                case "expedition":         return 5;
                case "supporter_mode":     return 6;
                case "tribe_war":          return 7;
                default:                   return 0;
            }
        }
    }
}
