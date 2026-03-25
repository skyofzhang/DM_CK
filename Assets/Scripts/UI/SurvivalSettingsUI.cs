using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrscfZ.Systems;

namespace DrscfZ.UI
{
    /// <summary>
    /// 生存游戏设置面板
    ///
    /// 功能：
    ///   - 音量控制（BGM / SFX 滑条）
    ///   - BGM / SFX 开关按钮（🔊/🔇切换）
    ///   - 礼物视频动画开关
    ///   - VIP入场视频开关
    ///   - 版本号显示
    ///   - 关闭按钮
    ///
    /// 挂载规则（Rule #7）：
    ///   脚本挂载在 Canvas（始终激活）；_panel 初始 inactive（Rule #2）。
    ///
    /// 与 AudioManager 集成：
    ///   优先使用 AudioManager.Instance（保持全局音量状态一致，自动持久化）。
    ///   无 AudioManager 时回退到直接控制 _bgmSource / _sfxSource。
    ///
    /// 外部调用：
    ///   SurvivalSettingsUI.Instance.TogglePanel();
    ///   SurvivalSettingsUI.Instance.ShowPanel();
    /// </summary>
    public class SurvivalSettingsUI : MonoBehaviour
    {
        public static SurvivalSettingsUI Instance { get; private set; }

        // ==================== Inspector 字段 ====================

        [Header("面板（初始 inactive）")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button     _closeBtn;

        [Header("BGM 音量")]
        [SerializeField] private Slider   _bgmSlider;
        [SerializeField] private TMP_Text _bgmValueText;
        [SerializeField] private Button   _bgmToggle;     // 点击切换 BGM 静音/恢复
        [SerializeField] private TMP_Text _bgmToggleText; // 显示 🔊 / 🔇

        [Header("SFX 音量")]
        [SerializeField] private Slider   _sfxSlider;
        [SerializeField] private TMP_Text _sfxValueText;
        [SerializeField] private Button   _sfxToggle;     // 点击切换 SFX 静音/恢复
        [SerializeField] private TMP_Text _sfxToggleText; // 显示 🔊 / 🔇

        [Header("关于")]
        [SerializeField] private TMP_Text _versionText;

        [Header("音频源（无 AudioManager 时回退）")]
        [SerializeField] private AudioSource _bgmSource;
        [SerializeField] private AudioSource _sfxSource;

        [Header("Phase2 新增设置")]
        [SerializeField] private Toggle _giftVideoToggle;   // 礼物视频动画开关
        [SerializeField] private Toggle _vipVideoToggle;    // VIP入场视频开关

        // ==================== 生命周期 ====================

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            // Rule #2：初始隐藏
            if (_panel != null)
                _panel.SetActive(false);

            // 绑定按钮事件
            if (_closeBtn    != null) _closeBtn.onClick.AddListener(HidePanel);
            if (_bgmToggle   != null) _bgmToggle.onClick.AddListener(ToggleBGM);
            if (_sfxToggle   != null) _sfxToggle.onClick.AddListener(ToggleSFX);
        }

        private void Start()
        {
            // 首次同步（AudioManager 在此帧已完成 Awake）
            SyncFromAudioManager();

            // 礼物视频开关（默认ON）
            bool giftVideoOn = PlayerPrefs.GetInt("gift_video_enabled", 1) == 1;
            if (_giftVideoToggle != null)
            {
                _giftVideoToggle.isOn = giftVideoOn;
                _giftVideoToggle.onValueChanged.AddListener(OnGiftVideoToggleChanged);
            }

            // VIP入场视频开关（默认ON）
            bool vipVideoOn = PlayerPrefs.GetInt("vip_video_enabled", 1) == 1;
            if (_vipVideoToggle != null)
            {
                _vipVideoToggle.isOn = vipVideoOn;
                _vipVideoToggle.onValueChanged.AddListener(OnVIPVideoToggleChanged);
            }
        }

        private void OnEnable()
        {
            // 每次面板打开时刷新最新音量值
            SyncFromAudioManager();
        }

        // ==================== 公开 API ====================

        /// <summary>显示设置面板并同步当前音量</summary>
        public void ShowPanel()
        {
            if (_panel == null) return;
            _panel.SetActive(true);
            SyncFromAudioManager();
        }

        /// <summary>隐藏设置面板</summary>
        public void HidePanel()
        {
            if (_panel != null)
                _panel.SetActive(false);
        }

        /// <summary>切换显隐（供设置按钮绑定）</summary>
        public void TogglePanel()
        {
            if (_panel == null) return;
            if (_panel.activeSelf) HidePanel();
            else ShowPanel();
        }

        // ==================== 音量同步 ====================

        /// <summary>从 AudioManager 读取当前状态并刷新 UI</summary>
        private void SyncFromAudioManager()
        {
            var am = AudioManager.Instance;
            float bgmVol = am != null ? am.BGMVolume : PlayerPrefs.GetFloat("BGMVolume", 0.8f);
            float sfxVol = am != null ? am.SFXVolume : PlayerPrefs.GetFloat("SFXVolume", 1.0f);
            bool  bgmOn  = am != null ? am.BGMEnabled : true;
            bool  sfxOn  = am != null ? am.SFXEnabled : true;

            // BGM 滑条（先移除监听再赋值，避免触发回调改写 AudioManager）
            if (_bgmSlider != null)
            {
                _bgmSlider.onValueChanged.RemoveAllListeners();
                _bgmSlider.minValue = 0f;
                _bgmSlider.maxValue = 1f;
                _bgmSlider.value    = bgmVol;
                _bgmSlider.interactable = bgmOn; // 静音时灰化滑条
                _bgmSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
            }

            // SFX 滑条
            if (_sfxSlider != null)
            {
                _sfxSlider.onValueChanged.RemoveAllListeners();
                _sfxSlider.minValue = 0f;
                _sfxSlider.maxValue = 1f;
                _sfxSlider.value    = sfxVol;
                _sfxSlider.interactable = sfxOn;
                _sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            }

            // 版本号
            if (_versionText != null)
                _versionText.text = $"极地生存法则  v{Application.version}";

            RefreshVolumeTexts(bgmVol, sfxVol);
            RefreshToggleIcons(bgmOn, sfxOn);
        }

        // ==================== 滑条回调 ====================

        private void OnBgmVolumeChanged(float value)
        {
            var am = AudioManager.Instance;
            if (am != null)
            {
                am.BGMVolume = value; // 属性 setter 自动写入 PlayerPrefs
            }
            else
            {
                PlayerPrefs.SetFloat("BGMVolume", value);
                if (_bgmSource != null) _bgmSource.volume = value;
            }

            if (_bgmValueText != null)
                _bgmValueText.text = $"{Mathf.RoundToInt(value * 100)}%";
        }

        private void OnSfxVolumeChanged(float value)
        {
            var am = AudioManager.Instance;
            if (am != null)
            {
                am.SFXVolume = value;
            }
            else
            {
                PlayerPrefs.SetFloat("SFXVolume", value);
                if (_sfxSource != null) _sfxSource.volume = value;
            }

            if (_sfxValueText != null)
                _sfxValueText.text = $"{Mathf.RoundToInt(value * 100)}%";
        }

        // ==================== 开关按钮 ====================

        private void ToggleBGM()
        {
            var am = AudioManager.Instance;
            if (am == null) return;

            am.BGMEnabled = !am.BGMEnabled;
            if (_bgmSlider != null) _bgmSlider.interactable = am.BGMEnabled;
            RefreshToggleIcons(am.BGMEnabled, am.SFXEnabled);
        }

        private void ToggleSFX()
        {
            var am = AudioManager.Instance;
            if (am == null) return;

            am.SFXEnabled = !am.SFXEnabled;
            if (_sfxSlider != null) _sfxSlider.interactable = am.SFXEnabled;
            RefreshToggleIcons(am.BGMEnabled, am.SFXEnabled);
        }

        // ==================== 内部工具 ====================

        private void RefreshVolumeTexts(float bgm, float sfx)
        {
            if (_bgmValueText != null) _bgmValueText.text = $"{Mathf.RoundToInt(bgm * 100)}%";
            if (_sfxValueText != null) _sfxValueText.text = $"{Mathf.RoundToInt(sfx * 100)}%";
        }

        private void RefreshToggleIcons(bool bgmOn, bool sfxOn)
        {
            if (_bgmToggleText != null) _bgmToggleText.text = bgmOn ? "开" : "关";
            if (_sfxToggleText != null) _sfxToggleText.text = sfxOn ? "开" : "关";
        }

        // ==================== Phase2 视频开关回调 ====================

        private void OnGiftVideoToggleChanged(bool value)
        {
            PlayerPrefs.SetInt("gift_video_enabled", value ? 1 : 0);
            PlayerPrefs.Save();
            // 同步到 SettingsPanelUI 的静态开关（GiftAnimationUI 读取此值）
            SettingsPanelUI.SetGiftVideoEnabled(value);
        }

        private void OnVIPVideoToggleChanged(bool value)
        {
            PlayerPrefs.SetInt("vip_video_enabled", value ? 1 : 0);
            PlayerPrefs.Save();
            // 同步到 SettingsPanelUI 的静态开关（VIPAnnouncementUI 读取此值）
            SettingsPanelUI.SetVIPVideoEnabled(value);
        }
    }
}
