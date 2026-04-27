using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace DrscfZ.Systems
{
    /// <summary>
    /// 音效管理器 - BGM(分阶段+淡入淡出)+SFX(按名称+并发限制)+循环SFX(火焰/警报)
    ///
    /// BGM原有 (GameManager用): battle_start / normal_battle / near_victory
    /// BGM新增 (昼夜系统用):    bgm_day / bgm_night / bgm_win / bgm_lose
    ///
    /// SFX原有: player_join / vip_join / game_start / victory / pushback /
    ///          ui_click / unit_spawn / pushing / push_force / upgrade / countdown
    /// SFX新增 (生存系统用): sfx_collect_food / sfx_collect_coal / sfx_collect_ore /
    ///          sfx_fire_start / sfx_fire_loop / sfx_monster_hit / sfx_monster_attack /
    ///          sfx_gate_alarm / sfx_cold_alarm / sfx_gift_t1_ding / sfx_gift_t2_bubble /
    ///          sfx_gift_t3_boom / sfx_gift_t4_electric / sfx_gift_t5_airdrop /
    ///          sfx_broadcaster_boost / sfx_day_start / sfx_night_start /
    ///          sfx_rank_up / sfx_rank_down / ui_toast / ui_settlement
    ///
    /// SFX并发限制: 同时播放不超过 maxConcurrentSFX (默认5)
    /// 音量通过 SetBGMVolume/SetSFXVolume 控制，数值保存到PlayerPrefs
    /// BGM切换使用 CrossfadeBGM(name) 或 SwitchBGM(clip) 淡入淡出
    /// 循环SFX: StartLoopSFX / StopLoopSFX（用于火焰/警报等持续音效）
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        public AudioSource bgmSource;
        public AudioSource sfxSource;

        // ---- 原有 BGM（GameManager 使用）----
        [Header("BGM Clips - Battle")]
        public AudioClip bgmBattleStart;
        public AudioClip bgmNormalBattle;
        public AudioClip bgmNearVictory;

        // ---- 新增 BGM（昼夜系统使用）----
        [Header("BGM Clips - Day/Night (assign in Inspector or Resources/Audio/BGM/)")]
        [SerializeField] public AudioClip bgmDay;    // bgm_day_winter
        [SerializeField] public AudioClip bgmNight;  // bgm_night_danger
        [SerializeField] public AudioClip bgmWin;    // bgm_win
        [SerializeField] public AudioClip bgmLose;   // bgm_lose

        // ---- 原有 SFX ----
        [Header("SFX Clips - Original")]
        public AudioClip sfxPlayerJoin;
        public AudioClip sfxVipJoin;
        public AudioClip sfxGameStart;
        public AudioClip sfxVictory;
        public AudioClip sfxPushback;
        public AudioClip sfxUIClick;
        public AudioClip sfxUnitSpawn;
        public AudioClip sfxPushing;
        public AudioClip sfxPushForce;
        public AudioClip sfxUpgrade;
        public AudioClip sfxCountdown;

        // ---- 新增 SFX（生存系统使用）----
        [Header("SFX Clips - Survival (assign in Inspector or Resources/Audio/SFX/)")]
        [SerializeField] private AudioClip sfxCollectFood;
        [SerializeField] private AudioClip sfxCollectCoal;
        [SerializeField] private AudioClip sfxCollectOre;
        [SerializeField] private AudioClip sfxFireStart;
        [SerializeField] private AudioClip sfxFireLoop;
        [SerializeField] private AudioClip sfxMonsterHit;
        [SerializeField] private AudioClip sfxMonsterAttack;
        [SerializeField] private AudioClip sfxGateAlarm;
        [SerializeField] private AudioClip sfxColdAlarm;
        [SerializeField] private AudioClip sfxGiftT1;
        [SerializeField] private AudioClip sfxGiftT2;
        [SerializeField] private AudioClip sfxGiftT3;
        [SerializeField] private AudioClip sfxGiftT4;
        [SerializeField] private AudioClip sfxGiftT5;
        [SerializeField] private AudioClip sfxBroadcasterBoost;
        [SerializeField] private AudioClip sfxDayStart;
        [SerializeField] private AudioClip sfxNightStart;
        [SerializeField] private AudioClip sfxRankUp;
        [SerializeField] private AudioClip sfxRankDown;
        [SerializeField] private AudioClip sfxUIToast;
        [SerializeField] private AudioClip sfxUISettlement;
        // ⚠️ audit-r24 GAP-C24-02 三方断点修复：常量定义 ✅ + 资源 ⏳ 美术延后 + 字典/LoadFromResources 注册 ✅
        // 调用方 SurvivalGameManager.cs:2131/2140 (LegendPromote/TierPromote) + WorkerController.cs:909 (WorkerShield) 在
        // r24 之前永远报 SFX not found warning；现 _sfxMap 字典 + LoadFromResources 都已注册，等待美术交付 mp3 即可生效。
        [SerializeField] private AudioClip sfxTierPromote;          // 矿工阶段晋升（含 SFX_LEGEND_PROMOTE 共用 clip）
        [SerializeField] private AudioClip sfxWorkerShieldActivate; // §30.3 阶8 护盾激活

        [Header("Settings")]
        public int maxConcurrentSFX = 5;

        // 当前活跃SFX数量追踪
        private int _activeSFXCount = 0;
        private Dictionary<string, AudioClip> _sfxMap;
        private Dictionary<string, AudioClip> _bgmMap;

        // 循环SFX（火焰、警报等）
        private Dictionary<string, AudioSource> _loopSources = new Dictionary<string, AudioSource>();

        // 音量 (0~1)
        private float _bgmVolume = 0.6f;
        private float _sfxVolume = 0.8f;
        private bool _bgmEnabled = true;
        private bool _sfxEnabled = true;

        // BGM状态
        private string _currentBGMName = "";
        private Coroutine _crossfadeCoroutine = null;

        // PlayerPrefs keys
        private const string KEY_BGM_VOL = "AudioBGMVolume";
        private const string KEY_SFX_VOL = "AudioSFXVolume";
        private const string KEY_BGM_ON  = "AudioBGMEnabled";
        private const string KEY_SFX_ON  = "AudioSFXEnabled";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 确保有AudioSource
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.playOnAwake = false;
                bgmSource.loop = true;
                bgmSource.spatialBlend = 0;
            }
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.loop = false;
                sfxSource.spatialBlend = 0;
            }

            // 加载保存的音量设置
            _bgmVolume = PlayerPrefs.GetFloat(KEY_BGM_VOL, 0.6f);
            _sfxVolume = PlayerPrefs.GetFloat(KEY_SFX_VOL, 0.8f);
            _bgmEnabled = PlayerPrefs.GetInt(KEY_BGM_ON, 1) == 1;
            _sfxEnabled = PlayerPrefs.GetInt(KEY_SFX_ON, 1) == 1;

            bgmSource.volume = _bgmEnabled ? _bgmVolume : 0;
            sfxSource.volume = _sfxEnabled ? _sfxVolume : 0;

            BuildMaps();
        }

        private void BuildMaps()
        {
            // 从 Resources/Audio/ 自动加载（场景引用为空时fallback）
            LoadFromResources();

            _sfxMap = new Dictionary<string, AudioClip>
            {
                // ---- 原有 SFX ----
                { "player_join",   sfxPlayerJoin },
                { "vip_join",      sfxVipJoin },
                { "game_start",    sfxGameStart },
                { "victory",       sfxVictory },
                { "pushback",      sfxPushback },
                { "ui_click",      sfxUIClick },
                { "unit_spawn",    sfxUnitSpawn },
                { "pushing",       sfxPushing },
                { "push_force",    sfxPushForce },
                { "upgrade",       sfxUpgrade },
                { "countdown",     sfxCountdown },

                // ---- 新增 SFX（生存系统）----
                { "sfx_collect_food",        sfxCollectFood },
                { "sfx_collect_coal",        sfxCollectCoal },
                { "sfx_collect_ore",         sfxCollectOre },
                { "sfx_fire_start",          sfxFireStart },
                { "sfx_fire_loop",           sfxFireLoop },
                { "sfx_monster_hit",         sfxMonsterHit },
                { "sfx_monster_attack",      sfxMonsterAttack },
                { "sfx_gate_alarm",          sfxGateAlarm },
                { "sfx_cold_alarm",          sfxColdAlarm },

                // T1-T5: 规范名（audit-r11 GAP-C11 清理：GiftNotificationUI 已删除，6 个别名已无消费方）
                { "sfx_gift_t1_ding",        sfxGiftT1 },
                { "sfx_gift_t2_bubble",      sfxGiftT2 },
                { "sfx_gift_t3_boom",        sfxGiftT3 },
                { "sfx_gift_t4_electric",    sfxGiftT4 },
                { "sfx_gift_t5_airdrop",     sfxGiftT5 },

                { "sfx_broadcaster_boost",   sfxBroadcasterBoost },
                { "sfx_day_start",           sfxDayStart },
                { "sfx_night_start",         sfxNightStart },
                { "sfx_rank_up",             sfxRankUp },
                { "sfx_rank_down",           sfxRankDown },
                { "ui_toast",                sfxUIToast },
                { "ui_settlement",           sfxUISettlement },
                // ⚠️ audit-r24 GAP-C24-02：补 _sfxMap 注册（之前 SFX_TIER_PROMOTE / SFX_LEGEND_PROMOTE / SFX_WORKER_SHIELD_ACTIVATE 永远报 SFX not found）
                { "sfx_tier_promote",            sfxTierPromote },
                { "sfx_worker_shield_activate",  sfxWorkerShieldActivate },
            };

            _bgmMap = new Dictionary<string, AudioClip>
            {
                // ---- 原有 BGM ----
                { "battle_start",  bgmBattleStart },
                { "normal_battle", bgmNormalBattle },
                { "near_victory",  bgmNearVictory },

                // ---- 新增 BGM（昼夜系统）----
                { "bgm_day",       bgmDay },
                { "bgm_night",     bgmNight },
                { "bgm_win",       bgmWin },
                { "bgm_lose",      bgmLose },
            };

            // 统计加载结果
            int bgmLoaded = 0, sfxLoaded = 0;
            foreach (var kv in _bgmMap) if (kv.Value != null) bgmLoaded++;
            foreach (var kv in _sfxMap) if (kv.Value != null) sfxLoaded++;
            Debug.Log($"[AudioManager] Loaded {bgmLoaded}/{_bgmMap.Count} BGM, {sfxLoaded}/{_sfxMap.Count} SFX");
        }

        /// <summary>从 Resources/Audio/BGM 和 Resources/Audio/SFX 自动加载音频文件</summary>
        private void LoadFromResources()
        {
            // ---- 原有 BGM ----
            if (bgmBattleStart  == null) bgmBattleStart  = Resources.Load<AudioClip>("Audio/BGM/battle_start");
            if (bgmNormalBattle == null) bgmNormalBattle = Resources.Load<AudioClip>("Audio/BGM/normal_battle");
            if (bgmNearVictory  == null) bgmNearVictory  = Resources.Load<AudioClip>("Audio/BGM/near_victory");

            // ---- 新增 BGM（昼夜系统）----
            if (bgmDay   == null) bgmDay   = Resources.Load<AudioClip>("Audio/BGM/bgm_day_winter");
            if (bgmNight == null) bgmNight = Resources.Load<AudioClip>("Audio/BGM/bgm_night_danger");
            if (bgmWin   == null) bgmWin   = Resources.Load<AudioClip>("Audio/BGM/bgm_win");
            if (bgmLose  == null) bgmLose  = Resources.Load<AudioClip>("Audio/BGM/bgm_lose");

            // ---- 原有 SFX ----
            if (sfxPlayerJoin == null) sfxPlayerJoin = Resources.Load<AudioClip>("Audio/SFX/player_join");
            if (sfxVipJoin    == null) sfxVipJoin    = Resources.Load<AudioClip>("Audio/SFX/vip_join");
            if (sfxGameStart  == null) sfxGameStart  = Resources.Load<AudioClip>("Audio/SFX/game_start");
            if (sfxVictory    == null) sfxVictory    = Resources.Load<AudioClip>("Audio/SFX/victory");
            if (sfxPushback   == null) sfxPushback   = Resources.Load<AudioClip>("Audio/SFX/pushback");
            if (sfxUIClick    == null) sfxUIClick    = Resources.Load<AudioClip>("Audio/SFX/ui_click");
            if (sfxUnitSpawn  == null) sfxUnitSpawn  = Resources.Load<AudioClip>("Audio/SFX/unit_spawn");
            if (sfxPushing    == null) sfxPushing    = Resources.Load<AudioClip>("Audio/SFX/pushing");
            if (sfxPushForce  == null) sfxPushForce  = Resources.Load<AudioClip>("Audio/SFX/push_force");
            if (sfxUpgrade    == null) sfxUpgrade    = Resources.Load<AudioClip>("Audio/SFX/upgrade");
            if (sfxCountdown  == null) sfxCountdown  = Resources.Load<AudioClip>("Audio/SFX/countdown");

            // ---- 新增 SFX（生存系统）----
            if (sfxCollectFood      == null) sfxCollectFood      = Resources.Load<AudioClip>("Audio/SFX/sfx_collect_food");
            if (sfxCollectCoal      == null) sfxCollectCoal      = Resources.Load<AudioClip>("Audio/SFX/sfx_collect_coal");
            if (sfxCollectOre       == null) sfxCollectOre       = Resources.Load<AudioClip>("Audio/SFX/sfx_collect_ore");
            if (sfxFireStart        == null) sfxFireStart        = Resources.Load<AudioClip>("Audio/SFX/sfx_fire_start");
            if (sfxFireLoop         == null) sfxFireLoop         = Resources.Load<AudioClip>("Audio/SFX/sfx_fire_loop");
            if (sfxMonsterHit       == null) sfxMonsterHit       = Resources.Load<AudioClip>("Audio/SFX/sfx_monster_hit");
            if (sfxMonsterAttack    == null) sfxMonsterAttack    = Resources.Load<AudioClip>("Audio/SFX/sfx_monster_attack");
            if (sfxGateAlarm        == null) sfxGateAlarm        = Resources.Load<AudioClip>("Audio/SFX/sfx_gate_alarm");
            if (sfxColdAlarm        == null) sfxColdAlarm        = Resources.Load<AudioClip>("Audio/SFX/sfx_cold_alarm");
            if (sfxGiftT1           == null) sfxGiftT1           = Resources.Load<AudioClip>("Audio/SFX/sfx_gift_t1_ding");
            if (sfxGiftT2           == null) sfxGiftT2           = Resources.Load<AudioClip>("Audio/SFX/sfx_gift_t2_bubble");
            if (sfxGiftT3           == null) sfxGiftT3           = Resources.Load<AudioClip>("Audio/SFX/sfx_gift_t3_boom");
            if (sfxGiftT4           == null) sfxGiftT4           = Resources.Load<AudioClip>("Audio/SFX/sfx_gift_t4_electric");
            if (sfxGiftT5           == null) sfxGiftT5           = Resources.Load<AudioClip>("Audio/SFX/sfx_gift_t5_airdrop");
            if (sfxBroadcasterBoost == null) sfxBroadcasterBoost = Resources.Load<AudioClip>("Audio/SFX/sfx_broadcaster_boost");
            if (sfxDayStart         == null) sfxDayStart         = Resources.Load<AudioClip>("Audio/SFX/sfx_day_start");
            if (sfxNightStart       == null) sfxNightStart       = Resources.Load<AudioClip>("Audio/SFX/sfx_night_start");
            if (sfxRankUp           == null) sfxRankUp           = Resources.Load<AudioClip>("Audio/SFX/sfx_rank_up");
            if (sfxRankDown         == null) sfxRankDown         = Resources.Load<AudioClip>("Audio/SFX/sfx_rank_down");
            if (sfxUIToast          == null) sfxUIToast          = Resources.Load<AudioClip>("Audio/UI/ui_toast");
            if (sfxUISettlement     == null) sfxUISettlement     = Resources.Load<AudioClip>("Audio/UI/ui_settlement");
            // ⚠️ audit-r24 GAP-C24-02：补 LoadFromResources（资源文件待美术交付，目前 Resources.Load 返 null fallback 走 Inspector 默认）
            if (sfxTierPromote          == null) sfxTierPromote          = Resources.Load<AudioClip>("Audio/SFX/sfx_tier_promote");
            if (sfxWorkerShieldActivate == null) sfxWorkerShieldActivate = Resources.Load<AudioClip>("Audio/SFX/sfx_worker_shield_activate");
        }

        // ==================== SFX ====================

        /// <summary>按名称播放SFX（受并发限制）</summary>
        public void PlaySFX(string sfxName)
        {
            if (!_sfxEnabled) return;
            if (_activeSFXCount >= maxConcurrentSFX) return;
            if (sfxSource == null) return;

            if (_sfxMap == null) BuildMaps();

            AudioClip clip = null;
            if (_sfxMap.TryGetValue(sfxName, out clip) && clip != null)
            {
                sfxSource.PlayOneShot(clip, _sfxVolume);
                _activeSFXCount++;
                StartCoroutine(DecrementSFXCount(clip.length));
            }
            else
            {
                Debug.LogWarning($"[AudioManager] SFX not found: '{sfxName}'");
            }
        }

        /// <summary>直接播放AudioClip（受并发限制）</summary>
        public void PlaySFX(AudioClip clip)
        {
            if (!_sfxEnabled || clip == null || sfxSource == null) return;
            if (_activeSFXCount >= maxConcurrentSFX) return;

            sfxSource.PlayOneShot(clip, _sfxVolume);
            _activeSFXCount++;
            StartCoroutine(DecrementSFXCount(clip.length));
        }

        /// <summary>按名称播放SFX，使用自定义音量（用于礼物音效等需要独立音量的场景）</summary>
        public void PlaySFX(string sfxName, float volume)
        {
            if (!_sfxEnabled) return;
            if (_activeSFXCount >= maxConcurrentSFX) return;
            if (sfxSource == null) return;

            if (_sfxMap == null) BuildMaps();

            AudioClip clip = null;
            if (_sfxMap.TryGetValue(sfxName, out clip) && clip != null)
            {
                sfxSource.PlayOneShot(clip, volume);
                _activeSFXCount++;
                StartCoroutine(DecrementSFXCount(clip.length));
            }
            else
            {
                Debug.LogWarning($"[AudioManager] SFX not found: '{sfxName}'");
            }
        }

        private IEnumerator DecrementSFXCount(float delay)
        {
            yield return new WaitForSeconds(delay);
            _activeSFXCount = Mathf.Max(0, _activeSFXCount - 1);
        }

        // ==================== 循环 SFX ====================

        /// <summary>
        /// 开始播放循环SFX（用于火焰噼啪声、城门警报、寒风等）
        /// 同一个 sfxId 只会创建一个循环源，重复调用无效
        /// </summary>
        public void StartLoopSFX(string sfxId, float volume = 0.5f)
        {
            if (_loopSources.ContainsKey(sfxId)) return;

            if (_sfxMap == null) BuildMaps();

            AudioClip clip;
            if (!_sfxMap.TryGetValue(sfxId, out clip) || clip == null)
            {
                Debug.LogWarning($"[AudioManager] StartLoopSFX: SFX not found: '{sfxId}'");
                return;
            }

            var loopObj = new GameObject($"LoopSFX_{sfxId}");
            loopObj.transform.SetParent(transform);
            var src = loopObj.AddComponent<AudioSource>();
            src.clip = clip;
            src.volume = _sfxEnabled ? volume : 0f;
            src.loop = true;
            src.spatialBlend = 0f;
            src.playOnAwake = false;
            src.Play();
            _loopSources[sfxId] = src;
        }

        /// <summary>停止并销毁指定循环SFX</summary>
        public void StopLoopSFX(string sfxId)
        {
            AudioSource src;
            if (_loopSources.TryGetValue(sfxId, out src))
            {
                if (src != null)
                {
                    src.Stop();
                    Destroy(src.gameObject);
                }
                _loopSources.Remove(sfxId);
            }
        }

        /// <summary>停止所有循环SFX</summary>
        public void StopAllLoopSFX()
        {
            foreach (var kv in _loopSources)
            {
                if (kv.Value != null)
                {
                    kv.Value.Stop();
                    Destroy(kv.Value.gameObject);
                }
            }
            _loopSources.Clear();
        }

        /// <summary>某个循环SFX是否正在播放</summary>
        public bool IsLoopSFXPlaying(string sfxId)
        {
            return _loopSources.ContainsKey(sfxId) && _loopSources[sfxId] != null;
        }

        // ==================== BGM ====================

        /// <summary>按名称播放BGM（battle_start / normal_battle / near_victory / bgm_day / bgm_night）</summary>
        public void PlayBGM(string bgmName)
        {
            if (bgmSource == null) return;
            if (_currentBGMName == bgmName && bgmSource.isPlaying) return;

            if (_bgmMap == null) BuildMaps();

            AudioClip clip = null;
            if (_bgmMap.TryGetValue(bgmName, out clip) && clip != null)
            {
                bgmSource.clip = clip;
                bgmSource.loop = true;
                bgmSource.volume = _bgmEnabled ? _bgmVolume : 0;
                bgmSource.Play();
                _currentBGMName = bgmName;
            }
        }

        /// <summary>直接播放AudioClip作为BGM</summary>
        public void PlayBGM(AudioClip clip)
        {
            if (bgmSource == null || clip == null) return;
            bgmSource.clip = clip;
            bgmSource.loop = true;
            bgmSource.volume = _bgmEnabled ? _bgmVolume : 0;
            bgmSource.Play();
            _currentBGMName = clip.name;
        }

        public void StopBGM()
        {
            if (bgmSource != null)
            {
                bgmSource.Stop();
                _currentBGMName = "";
            }
        }

        /// <summary>BGM淡入淡出切换（按名称），已有同名BGM播放时跳过</summary>
        public void CrossfadeBGM(string bgmName, float fadeDuration = 1f)
        {
            if (_currentBGMName == bgmName && bgmSource.isPlaying) return;

            if (_crossfadeCoroutine != null) StopCoroutine(_crossfadeCoroutine);
            _crossfadeCoroutine = StartCoroutine(CrossfadeCoroutine(bgmName, fadeDuration));
        }

        private IEnumerator CrossfadeCoroutine(string bgmName, float duration)
        {
            float half = duration * 0.5f;
            float startVol = bgmSource.volume;

            // Fade out
            float elapsed = 0;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(startVol, 0, elapsed / half);
                yield return null;
            }

            // Switch
            PlayBGM(bgmName);

            // Fade in
            float targetVol = _bgmEnabled ? _bgmVolume : 0;
            elapsed = 0;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(0, targetVol, elapsed / half);
                yield return null;
            }
            bgmSource.volume = targetVol;
            _crossfadeCoroutine = null;
        }

        /// <summary>
        /// BGM淡入淡出切换（按AudioClip），供 DayNightCycleManager 直接传入 clip 使用
        /// 如果传入的 clip 和当前一致则跳过
        /// </summary>
        public IEnumerator SwitchBGM(AudioClip newClip, float fadeDuration = 2f)
        {
            if (bgmSource == null || newClip == null) yield break;
            if (newClip == bgmSource.clip && bgmSource.isPlaying) yield break;

            // 中断正在进行的淡入淡出
            if (_crossfadeCoroutine != null)
            {
                StopCoroutine(_crossfadeCoroutine);
                _crossfadeCoroutine = null;
            }

            float originalVolume = _bgmEnabled ? _bgmVolume : 0f;
            float halfDuration   = fadeDuration * 0.5f;

            // Fade out
            float elapsed = 0f;
            float startVol = bgmSource.volume;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / halfDuration);
                yield return null;
            }
            bgmSource.volume = 0f;

            // Switch
            bgmSource.clip = newClip;
            bgmSource.loop = true;
            bgmSource.Play();
            _currentBGMName = newClip.name;

            // Fade in
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(0f, originalVolume, elapsed / halfDuration);
                yield return null;
            }
            bgmSource.volume = originalVolume;
        }

        // ==================== 音量控制 ====================

        public float BGMVolume
        {
            get => _bgmVolume;
            set
            {
                _bgmVolume = Mathf.Clamp01(value);
                if (bgmSource != null && _bgmEnabled)
                    bgmSource.volume = _bgmVolume;
                PlayerPrefs.SetFloat(KEY_BGM_VOL, _bgmVolume);
            }
        }

        public float SFXVolume
        {
            get => _sfxVolume;
            set
            {
                _sfxVolume = Mathf.Clamp01(value);
                if (sfxSource != null && _sfxEnabled)
                    sfxSource.volume = _sfxVolume;
                PlayerPrefs.SetFloat(KEY_SFX_VOL, _sfxVolume);
                // 同步更新所有循环SFX的音量
                foreach (var kv in _loopSources)
                    if (kv.Value != null) kv.Value.volume = _sfxVolume;
            }
        }

        public bool BGMEnabled
        {
            get => _bgmEnabled;
            set
            {
                _bgmEnabled = value;
                if (bgmSource != null)
                    bgmSource.volume = _bgmEnabled ? _bgmVolume : 0;
                PlayerPrefs.SetInt(KEY_BGM_ON, _bgmEnabled ? 1 : 0);
            }
        }

        public bool SFXEnabled
        {
            get => _sfxEnabled;
            set
            {
                _sfxEnabled = value;
                if (sfxSource != null)
                    sfxSource.volume = _sfxEnabled ? _sfxVolume : 0;
                // 同步静音/取消静音所有循环SFX
                foreach (var kv in _loopSources)
                    if (kv.Value != null) kv.Value.volume = _sfxEnabled ? _sfxVolume : 0;
                PlayerPrefs.SetInt(KEY_SFX_ON, _sfxEnabled ? 1 : 0);
            }
        }

        public void SetBGMVolume(float volume) { BGMVolume = volume; }
        public void SetSFXVolume(float volume) { SFXVolume = volume; }
    }
}
