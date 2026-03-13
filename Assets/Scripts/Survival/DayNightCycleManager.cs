using UnityEngine;
using System;
using DrscfZ.Systems;
using DrscfZ.Core;

namespace DrscfZ.Survival
{
    /// <summary>
    /// 昼夜循环管理器
    /// - 服务器权威：接收 phase_changed 消息后切换状态
    /// - 本地倒计时：仅用于UI显示平滑，服务器校正
    /// </summary>
    public class DayNightCycleManager : MonoBehaviour
    {
        public static DayNightCycleManager Instance { get; private set; }

        public enum Phase { Idle, Day, Night, Settlement }

        [Header("当前状态（只读）")]
        [SerializeField] private Phase _currentPhase = Phase.Idle;
        [SerializeField] private int   _currentDay   = 0;
        [SerializeField] private float _remainingTime = 0f;

        public Phase  CurrentPhase   => _currentPhase;
        public int    CurrentDay     => _currentDay;
        public float  RemainingTime  => _remainingTime;
        public bool   IsDay          => _currentPhase == Phase.Day;
        public bool   IsNight        => _currentPhase == Phase.Night;

        // 事件
        public event Action<int>   OnDayStarted;      // dayNumber
        public event Action<int>   OnNightStarted;    // dayNumber
        public event Action        OnSettlement;
        public event Action<float> OnTimeTick;         // remainingTime

        [Header("默认时长（服务器会覆盖）")]
        [SerializeField] private float _defaultDayDuration   = 240f;
        [SerializeField] private float _defaultNightDuration = 150f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (_currentPhase == Phase.Idle || _currentPhase == Phase.Settlement) return;

            // 本地倒计时（服务器会定期校正）
            _remainingTime = Mathf.Max(0f, _remainingTime - Time.deltaTime);
            OnTimeTick?.Invoke(_remainingTime);
        }

        // ==================== 服务器消息处理 ====================

        /// <summary>接收服务器 phase_changed 消息</summary>
        public void HandlePhaseChanged(PhaseChangedData data)
        {
            _currentDay    = data.day;
            _remainingTime = data.phaseDuration;

            switch (data.phase)
            {
                case "day":
                    _currentPhase = Phase.Day;
                    OnDayStarted?.Invoke(_currentDay);
                    Debug.Log($"[DayNight] 第{_currentDay}天 白天开始 ({data.phaseDuration:F0}s)");
                    OnDayAudio();
                    break;

                case "night":
                    _currentPhase = Phase.Night;
                    OnNightStarted?.Invoke(_currentDay);
                    Debug.Log($"[DayNight] 第{_currentDay}天 夜晚开始 ({data.phaseDuration:F0}s)");
                    OnNightAudio();
                    break;
            }
        }

        /// <summary>服务器校正倒计时</summary>
        public void SyncRemainingTime(float remaining)
        {
            _remainingTime = remaining;
        }

        /// <summary>进入结算</summary>
        public void HandleSettlement()
        {
            _currentPhase = Phase.Settlement;
            _remainingTime = 0f;
            OnSettlement?.Invoke();
        }

        // ==================== 音频集成 ====================

        /// <summary>白天开始时切换BGM并播放音效</summary>
        private void OnDayAudio()
        {
            var audio = AudioManager.Instance;
            if (audio == null) return;

            // 停止夜晚警报（如有）
            audio.StopLoopSFX(AudioConstants.SFX_GATE_ALARM);
            audio.StopLoopSFX(AudioConstants.SFX_COLD_ALARM);

            // 播放白天开场音效
            audio.PlaySFX(AudioConstants.SFX_DAY_START);

            // 切换到白天BGM（2s淡入淡出）
            if (audio.bgmDay != null)
                StartCoroutine(audio.SwitchBGM(audio.bgmDay, 2f));
            else
                audio.CrossfadeBGM(AudioConstants.BGM_DAY, 2f);
        }

        /// <summary>夜晚开始时切换BGM并播放音效</summary>
        private void OnNightAudio()
        {
            var audio = AudioManager.Instance;
            if (audio == null) return;

            // 播放夜晚开场音效
            audio.PlaySFX(AudioConstants.SFX_NIGHT_START);

            // 切换到夜晚BGM（3s淡入淡出，比白天切换慢一些，增加压迫感）
            if (audio.bgmNight != null)
                StartCoroutine(audio.SwitchBGM(audio.bgmNight, 3f));
            else
                audio.CrossfadeBGM(AudioConstants.BGM_NIGHT, 3f);
        }

        // ==================== 工具方法（原有）====================

        /// <summary>重置（新游戏）</summary>
        public void Reset()
        {
            _currentPhase  = Phase.Idle;
            _currentDay    = 0;
            _remainingTime = 0f;
        }

        // ==================== 工具方法 ====================

        /// <summary>阶段名称（中文，供UI显示）</summary>
        public string GetPhaseDisplayName()
        {
            return _currentPhase switch
            {
                Phase.Day        => $"第{_currentDay}天 • 白天",
                Phase.Night      => $"第{_currentDay}天 • 夜晚",
                Phase.Settlement => "结算中",
                _                => "等待开始"
            };
        }

        /// <summary>夜晚时天空颜色（深蓝），白天时浅蓝</summary>
        public Color GetSkyColor()
        {
            return _currentPhase == Phase.Night
                ? new Color(0.04f, 0.11f, 0.23f)   // #0B1D3A
                : new Color(0.35f, 0.60f, 0.85f);   // 浅蓝
        }

        /// <summary>强制提前结束夜晚（Boss击杀后调用）</summary>
        public void ForceEndNight()
        {
            if (_currentPhase != Phase.Night) return;
            _remainingTime = 0f;
            Debug.Log("[DayNight] 夜晚强制结束（BOSS已被击败）");
            // 服务器会发 phase_changed → day，客户端此处只做UI提示
        }
    }
}
