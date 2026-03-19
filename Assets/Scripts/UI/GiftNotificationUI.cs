using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrscfZ.Systems;

namespace DrscfZ.UI
{
    /// <summary>
    /// 礼物分级特效系统 — T1~T5 视觉主控制器
    ///
    /// 设计规则（遵循全局 AI开发准则）：
    ///   - Singleton：Instance 在 Awake 注册
    ///   - 所有 UI 对象在 Scene 中预创建，通过 SetActive 控制显隐
    ///   - 所有引用均为 [SerializeField]，禁止运行时 FindObjectOfType
    ///   - 代码默认值与 Scene 序列化值保持一致
    ///   - 挂载到始终激活的 Gift_Canvas 根对象上
    ///
    /// Scene 预创建结构：
    ///   Gift_Canvas (Sort Order=100)
    ///   ├── T1_StarParticle       ParticleSystem
    ///   ├── T2_BorderEffect       Panel (SetActive=false)
    ///   │   ├── TopLeft_PS / TopRight_PS / BotLeft_PS / BotRight_PS
    ///   │   └── CenterRing_Image
    ///   ├── T3_GiftBounce         Panel (SetActive=false)
    ///   │   ├── GiftIcon_Image
    ///   │   └── Explode_PS
    ///   ├── T4_FullscreenGlow     Panel (SetActive=false)
    ///   │   ├── OrangeOverlay
    ///   │   ├── BatteryIcon
    ///   │   └── ChargingSlider
    ///   ├── T5_EpicAirdrop        Panel (SetActive=false)
    ///   │   ├── BlackOverlay
    ///   │   ├── AirdropBox
    ///   │   ├── Fireworks_PS
    ///   │   ├── ResourceIcons     (包含4个子Image: food/coal/ore/shield)
    ///   │   └── PlayerNameText
    ///   └── GiftBannerQueue
    ///       ├── BannerSlot_0
    ///       ├── BannerSlot_1
    ///       └── BannerSlot_2
    ///
    /// 主入口：SurvivalGameManager 或 NetworkManager 调用
    ///   GiftNotificationUI.Instance.ShowGiftEffect(giftId, nickname, tier);
    /// </summary>
    public class GiftNotificationUI : MonoBehaviour
    {
        public static GiftNotificationUI Instance { get; private set; }

        // =========================================================================
        //  Scene 预创建引用 — Inspector 拖入
        // =========================================================================

        [Header("Gift_Canvas 根 RectTransform（用于屏幕震动）")]
        [SerializeField] private RectTransform _canvasRoot;

        // ---- T1 ----
        [Header("T1 — 仙女棒星星粒子")]
        [SerializeField] private ParticleSystem _t1Particle;

        // ---- T2 ----
        [Header("T2 — 四角粒子环面板")]
        [SerializeField] private GameObject _t2Panel;
        [SerializeField] private ParticleSystem _t2TopLeftPS;
        [SerializeField] private ParticleSystem _t2TopRightPS;
        [SerializeField] private ParticleSystem _t2BotLeftPS;
        [SerializeField] private ParticleSystem _t2BotRightPS;
        [SerializeField] private Image          _t2CenterRing;

        // ---- T3 ----
        [Header("T3 — 礼物飞入爆炸面板")]
        [SerializeField] private GameObject       _t3Panel;
        [SerializeField] private Image            _t3GiftIcon;
        [SerializeField] private ParticleSystem   _t3ExplodePS;

        // ---- T4 ----
        [Header("T4 — 全屏暖光面板")]
        [SerializeField] private GameObject _t4Panel;
        [SerializeField] private Image      _t4OrangeOverlay;
        [SerializeField] private Image      _t4BatteryIcon;
        [SerializeField] private Slider     _t4ChargingSlider;

        // ---- T5 ----
        [Header("T5 — 史诗空投面板（最顶层）")]
        [SerializeField] private GameObject       _t5Panel;
        [SerializeField] private Image            _t5BlackOverlay;
        [SerializeField] private RectTransform    _t5AirdropBox;
        [SerializeField] private ParticleSystem   _t5FireworksPS;
        [SerializeField] private RectTransform    _t5ResourceIcons; // 父容器，含4个子 Image
        [SerializeField] private TextMeshProUGUI  _t5PlayerNameText;

        // ---- GiftBannerQueue ----
        [Header("礼物横幅队列（最多3条同时显示，预创建）")]
        [SerializeField] private GiftBannerSlot[] _bannerSlots = new GiftBannerSlot[3];

        // =========================================================================
        //  内部状态
        // =========================================================================

        /// <summary>当前正在播放的 Tier（防止低 Tier 打断高 Tier）</summary>
        private int _currentPlayingTier = 0;
        private Coroutine _currentEffect;

        /// <summary>横幅队列：已占用槽位数</summary>
        private int _activeBannerCount = 0;
        private Queue<BannerRequest> _bannerQueue = new Queue<BannerRequest>();

        // =========================================================================
        //  数据结构
        // =========================================================================

        [System.Serializable]
        public class GiftBannerSlot
        {
            public GameObject root;          // BannerSlot_N
            public Image      colorBar;      // 左侧等级色条
            public Image      giftIcon;      // 礼物图标（40×40）
            public TextMeshProUGUI infoText; // 昵称 + 礼物名
            public TextMeshProUGUI tierTag;  // "T1"~"T5" 标签
        }

        private struct BannerRequest
        {
            public string nickname;
            public string giftName;
            public int    tier;
        }

        // =========================================================================
        //  生命周期
        // =========================================================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 初始化：关闭所有面板（遵循 AI准则：Start 中 SetActive=false）
            HideAllPanels();
        }

        private void Start()
        {
            // 双重保险：确保所有面板处于隐藏状态
            HideAllPanels();
        }

        private void HideAllPanels()
        {
            SafeSetActive(_t2Panel, false);
            SafeSetActive(_t3Panel, false);
            SafeSetActive(_t4Panel, false);
            SafeSetActive(_t5Panel, false);

            // 横幅槽全部隐藏
            foreach (var slot in _bannerSlots)
                if (slot != null && slot.root != null)
                    slot.root.SetActive(false);
        }

        // =========================================================================
        //  主入口
        // =========================================================================

        /// <summary>
        /// 显示礼物特效。由服务器消息处理器（NetworkManager/SurvivalGameManager）调用。
        /// </summary>
        /// <param name="giftId">礼物ID（如 "fairy_wand"），用于T3匹配图标</param>
        /// <param name="nickname">送礼玩家昵称</param>
        /// <param name="tier">礼物等级 1~5</param>
        public void ShowGiftEffect(string giftId, string nickname, int tier)
        {
            tier = Mathf.Clamp(tier, 1, 5);

            // 高 Tier 可打断低 Tier；同 Tier 不打断（先到先得）
            if (tier < _currentPlayingTier)
            {
                // 低Tier 仍加入横幅队列
                EnqueueBanner(nickname, giftId, tier);
                return;
            }

            // 停止当前效果
            if (_currentEffect != null)
            {
                StopCoroutine(_currentEffect);
                HideAllPanels();
                _currentPlayingTier = 0;
            }

            // 同时将横幅加入队列（T1+以上）
            EnqueueBanner(nickname, giftId, tier);

            switch (tier)
            {
                case 1: _currentEffect = StartCoroutine(PlayT1Effect(nickname)); break;
                case 2: _currentEffect = StartCoroutine(PlayT2Effect(giftId, nickname)); break;
                case 3: _currentEffect = StartCoroutine(PlayT3Effect(giftId, nickname)); break;
                case 4: _currentEffect = StartCoroutine(PlayT4Effect(nickname)); break;
                case 5: _currentEffect = StartCoroutine(PlayT5Effect(nickname)); break;
            }
        }

        // =========================================================================
        //  T1 — 仙女棒星星粒子（0.5 秒）
        // =========================================================================

        /// <summary>
        /// T1 效果：在弹幕区域附近播放金色星形粒子 0.5s，音效 sfx_gift_t1_ding。
        /// 无面板显示，效果极轻量。
        /// </summary>
        private IEnumerator PlayT1Effect(string nickname)
        {
            _currentPlayingTier = 1;

            if (_t1Particle != null)
                _t1Particle.Play();

            PlaySFX("sfx_gift_t1_ding");

            yield return new WaitForSeconds(0.5f);

            _currentPlayingTier = 0;
            _currentEffect = null;
        }

        // =========================================================================
        //  T2 — 四角粒子环（2 秒）
        // =========================================================================

        /// <summary>
        /// T2 效果时序：
        ///   0.0s  四角PS启动，中心光环 Scale 0→1（0.3s EaseOutBack）
        ///   0.8s  昵称字条滑入（横幅队列处理）
        ///   1.5s  光环淡出（0.5s）
        ///   2.0s  关闭面板
        /// </summary>
        private IEnumerator PlayT2Effect(string giftId, string nickname)
        {
            _currentPlayingTier = 2;

            _t2Panel.SetActive(true);

            // 启动四角粒子
            PlayPS(_t2TopLeftPS);
            PlayPS(_t2TopRightPS);
            PlayPS(_t2BotLeftPS);
            PlayPS(_t2BotRightPS);

            PlaySFX("sfx_gift_t2_appear");

            // 中心光环弹入（Size 0→300，用 Scale 模拟）
            if (_t2CenterRing != null)
            {
                _t2CenterRing.gameObject.SetActive(true);
                yield return StartCoroutine(GiftEffectSystem.PopIn(_t2CenterRing.transform, 0.3f, 1.2f));
            }

            // 等待到 1.5s
            yield return new WaitForSeconds(1.2f);

            // 光环淡出
            if (_t2CenterRing != null)
                yield return StartCoroutine(GiftEffectSystem.FadeOut(_t2CenterRing, 0.5f));

            _t2Panel.SetActive(false);
            _currentPlayingTier = 0;
            _currentEffect = null;
        }

        // =========================================================================
        //  T3 — 礼物飞入爆炸（3 秒）
        // =========================================================================

        /// <summary>
        /// T3 效果时序（参考 animation_spec.md §3）：
        ///   0.0s  礼物大图标从左侧外飞入中央（0.5s EaseOutBounce）
        ///   0.5s  抖动 3 次（Scale 1→1.15→1，0.3s/次）
        ///   1.4s  爆炸 PS 启动
        ///   1.4s  图标 Scale 1→2 + Alpha 1→0（0.5s EaseOutQuart）
        ///   2.5s  等待粒子消散
        ///   3.0s  关闭面板
        /// </summary>
        private IEnumerator PlayT3Effect(string giftId, string nickname)
        {
            _currentPlayingTier = 3;

            _t3Panel.SetActive(true);

            if (_t3GiftIcon != null)
            {
                // 重置状态
                Color c = _t3GiftIcon.color;
                c.a = 1f;
                _t3GiftIcon.color = c;
                _t3GiftIcon.transform.localScale = Vector3.one;
                _t3GiftIcon.gameObject.SetActive(true);

                // 飞入（从左侧 X=-600 到 X=0）
                RectTransform iconRT = _t3GiftIcon.rectTransform;
                yield return StartCoroutine(GiftEffectSystem.FlyInFromLeft(iconRT, 0.5f, -600f));

                PlaySFX("sfx_gift_t3_land");

                // 抖动 3 次
                yield return StartCoroutine(GiftEffectSystem.ShakeScale(_t3GiftIcon.transform, 3, 0.3f, 1.15f));

                // 爆炸粒子
                PlayPS(_t3ExplodePS);
                PlaySFX("sfx_gift_t3_explode");

                // 图标爆炸消散
                yield return StartCoroutine(GiftEffectSystem.ExplodeOut(_t3GiftIcon, 0.5f, 2f));
            }
            else
            {
                // 没有图标引用时，等待相同时长
                yield return new WaitForSeconds(1.9f);
            }

            // 粒子继续飞散 0.5s
            yield return new WaitForSeconds(0.5f);

            // 等待粒子视觉消散
            yield return new WaitForSeconds(0.6f);

            _t3Panel.SetActive(false);
            _currentPlayingTier = 0;
            _currentEffect = null;
        }

        // =========================================================================
        //  T4 — 全屏暖光 + 充能进度条（5 秒）
        // =========================================================================

        /// <summary>
        /// T4 效果时序（参考 animation_spec.md §3）：
        ///   0.0s  橙色遮罩淡入（0→0.4，0.3s）
        ///   0.3s  电池图标弹入（Scale 0→1.2→1，0.3s EaseOutBack）
        ///   0.3s  充能进度条 0→1（3s 线性）
        ///   0.5s  bobao ⚡ 播报（横幅队列）
        ///   4.5s  图标+遮罩淡出（0.5s）
        ///   5.0s  关闭面板
        /// </summary>
        private IEnumerator PlayT4Effect(string nickname)
        {
            _currentPlayingTier = 4;

            _t4Panel.SetActive(true);

            PlaySFX("sfx_gift_t4_charge");

            // 橙色遮罩淡入
            if (_t4OrangeOverlay != null)
            {
                ResetGraphicAlpha(_t4OrangeOverlay, 0f);
                _t4OrangeOverlay.gameObject.SetActive(true);
                yield return StartCoroutine(GiftEffectSystem.FadeIn(_t4OrangeOverlay, 0.3f, 0.4f));
            }

            // 电池图标弹入
            if (_t4BatteryIcon != null)
            {
                _t4BatteryIcon.transform.localScale = Vector3.zero;
                _t4BatteryIcon.gameObject.SetActive(true);
                yield return StartCoroutine(GiftEffectSystem.PopIn(_t4BatteryIcon.transform, 0.3f, 1.2f));
            }

            // 进度条充能（3s，与等待并行：分开启动后等待总时间）
            if (_t4ChargingSlider != null)
            {
                _t4ChargingSlider.value = 0f;
                StartCoroutine(GiftEffectSystem.AnimateSlider(_t4ChargingSlider, 0f, 1f, 3f));
            }

            // 等待充能完成（0.3s 已过，还需等 3s，共 3.3s 后才到 4.5s 时刻差0.9s）
            // 时间轴：0.3s(图标弹入)+3s(充能)=3.3s；4.5s-3.3s=1.2s 停留
            yield return new WaitForSeconds(3f + 1.2f);

            // 淡出
            Coroutine c1 = null, c2 = null;
            if (_t4OrangeOverlay != null)
                c1 = StartCoroutine(GiftEffectSystem.FadeOut(_t4OrangeOverlay, 0.5f));
            if (_t4BatteryIcon != null)
                c2 = StartCoroutine(GiftEffectSystem.FadeOut(_t4BatteryIcon, 0.5f));

            yield return new WaitForSeconds(0.5f);

            _t4Panel.SetActive(false);
            _currentPlayingTier = 0;
            _currentEffect = null;
        }

        // =========================================================================
        //  T5 — 史诗空投（8 秒）
        // =========================================================================

        /// <summary>
        /// T5 效果时序（参考 animation_spec.md §3）：
        ///   0.0s  黑色遮罩淡入（0→0.85，0.3s）
        ///   0.3s  空投箱从 Y=+1200 下落到 Y=-100（1s EaseInCubic）
        ///   1.5s  着地震动（屏幕震动 X±5，0.2s）
        ///   1.5s  烟花 PS 启动
        ///   1.5s  资源图标飞散
        ///   2.0s  玩家名大字弹入（Scale 0→1，0.5s EaseOutBack）
        ///   2.0s  通知 RankingPanelUI 金色闪烁（10s）
        ///   2.5s  bobao 超级播报（横幅队列）
        ///   5.0s  大字淡出（1s）
        ///   6.0s  烟花消散等待
        ///   7.0s  遮罩淡出（0.5s）
        ///   8.0s  完全恢复
        /// </summary>
        private IEnumerator PlayT5Effect(string nickname)
        {
            _currentPlayingTier = 5;

            _t5Panel.SetActive(true);

            PlaySFX("sfx_gift_t5_airdrop");

            // 0.0s — 黑色遮罩淡入
            if (_t5BlackOverlay != null)
            {
                ResetGraphicAlpha(_t5BlackOverlay, 0f);
                _t5BlackOverlay.gameObject.SetActive(true);
                yield return StartCoroutine(GiftEffectSystem.FadeIn(_t5BlackOverlay, 0.3f, 0.85f));
            }
            else
            {
                yield return new WaitForSeconds(0.3f);
            }

            // 0.3s — 空投箱下落（从 Y=+1200 到 Y=-100，1s EaseInCubic）
            if (_t5AirdropBox != null)
            {
                _t5AirdropBox.gameObject.SetActive(true);
                yield return StartCoroutine(GiftEffectSystem.DropFromTop(_t5AirdropBox, 1f, 1200f, -100f));
            }
            else
            {
                yield return new WaitForSeconds(1f);
            }

            // 1.5s — 着地：屏幕震动
            PlaySFX("sfx_gift_t5_land");
            if (_canvasRoot != null)
                yield return StartCoroutine(GiftEffectSystem.ShakeScreen(_canvasRoot, 0.2f, 5f));
            else
                yield return new WaitForSeconds(0.2f);

            // 1.5s — 烟花 PS
            PlayPS(_t5FireworksPS);

            // 1.5s — 资源图标飞散（4 个子图标飞向四角）
            if (_t5ResourceIcons != null && _t5ResourceIcons.childCount >= 4)
                StartCoroutine(ScatterAllResourceIcons(_t5ResourceIcons));

            // 0.5s 等待（对应 2.0s 时刻）
            yield return new WaitForSeconds(0.5f);

            // 2.0s — 玩家名大字弹入
            if (_t5PlayerNameText != null)
            {
                _t5PlayerNameText.text = $"{nickname} 拯救了村庄！";
                _t5PlayerNameText.transform.localScale = Vector3.zero;
                _t5PlayerNameText.gameObject.SetActive(true);
                yield return StartCoroutine(GiftEffectSystem.PopIn(_t5PlayerNameText.transform, 0.5f, 1.2f));
            }

            // 2.0s — 通知排行榜金色闪烁
            // RankingPanelUI 在本 codebase 中名称为 RankingPanelUI，且无 Instance 单例。
            // StartGoldFlash 尚未实现；调用时检查防御，待 RankingPanelUI 增加该方法后会自动生效。
            // 若 RankingPanelUI 后续添加了 Instance + StartGoldFlash(nickname, duration)，
            // 取消下方注释即可：
            // RankingPanelUI.Instance?.StartGoldFlash(nickname, 10f);

            PlaySFX("sfx_gift_t5_epic");

            // 等待到 5.0s（2.0+0.5 已过 2.5s，还需等 2.5s）
            yield return new WaitForSeconds(2.5f);

            // 5.0s — 大字淡出（1s）
            if (_t5PlayerNameText != null)
                yield return StartCoroutine(GiftEffectSystem.FadeOut(_t5PlayerNameText, 1f));
            else
                yield return new WaitForSeconds(1f);

            // 6.0s — 等烟花视觉消散（约 1s）
            yield return new WaitForSeconds(1f);

            // 7.0s — 遮罩淡出（0.5s）
            if (_t5BlackOverlay != null)
                yield return StartCoroutine(GiftEffectSystem.FadeOut(_t5BlackOverlay, 0.5f));
            else
                yield return new WaitForSeconds(0.5f);

            // 7.5s — 收尾等待
            yield return new WaitForSeconds(0.5f);

            _t5Panel.SetActive(false);
            _currentPlayingTier = 0;
            _currentEffect = null;
        }

        /// <summary>让 T5 ResourceIcons 容器下的4个子 Image 向四个角落飞散</summary>
        private IEnumerator ScatterAllResourceIcons(RectTransform parent)
        {
            // 四角飞散目标位置（相对于 Gift_Canvas 中心）
            Vector2[] targets = {
                new Vector2(-400f,  600f),  // 左上
                new Vector2( 400f,  600f),  // 右上
                new Vector2(-400f, -600f),  // 左下
                new Vector2( 400f, -600f),  // 右下
            };

            int count = Mathf.Min(parent.childCount, targets.Length);
            for (int i = 0; i < count; i++)
            {
                var childRT = parent.GetChild(i) as RectTransform;
                if (childRT == null) continue;

                var graphic = childRT.GetComponent<Graphic>();
                Vector2 startPos = childRT.anchoredPosition;
                Vector2 endPos = targets[i];

                StartCoroutine(GiftEffectSystem.ScatterIcon(childRT, graphic, startPos, endPos, 0.8f, 0.4f));
            }

            yield return new WaitForSeconds(1.2f);
        }

        // =========================================================================
        //  GiftBannerQueue — 礼物横幅队列（最多3条同时显示）
        // =========================================================================

        private void EnqueueBanner(string nickname, string giftName, int tier)
        {
            _bannerQueue.Enqueue(new BannerRequest
            {
                nickname = nickname,
                giftName = giftName,
                tier = tier
            });
            TryShowNextBanner();
        }

        private void TryShowNextBanner()
        {
            while (_activeBannerCount < _bannerSlots.Length && _bannerQueue.Count > 0)
            {
                var req = _bannerQueue.Dequeue();
                int slotIndex = FindFreeBannerSlot();
                if (slotIndex < 0) break;

                StartCoroutine(ShowBanner(slotIndex, req.nickname, req.giftName, req.tier));
            }
        }

        private int FindFreeBannerSlot()
        {
            for (int i = 0; i < _bannerSlots.Length; i++)
            {
                var s = _bannerSlots[i];
                if (s != null && s.root != null && !s.root.activeSelf)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// 在指定槽位显示礼物横幅。
        /// 进入动画：从左侧滑入（X: -540→0，0.2s EaseOutCubic）。
        /// 4s 后淡出，释放槽位。
        /// </summary>
        private IEnumerator ShowBanner(int slotIndex, string nickname, string giftName, int tier)
        {
            var slot = _bannerSlots[slotIndex];
            if (slot == null || slot.root == null) yield break;

            _activeBannerCount++;

            // 设置内容（显示中文礼物名 + 效果描述）
            if (slot.infoText != null)
            {
                string displayName = GetGiftDisplayName(giftName);
                string effect      = GetGiftEffect(giftName);
                slot.infoText.text = string.IsNullOrEmpty(effect)
                    ? $"{nickname} 送出 {displayName}"
                    : $"{nickname} 送出 {displayName}  {effect}";
            }
            if (slot.tierTag != null)
                slot.tierTag.text = $"T{tier}";
            if (slot.colorBar != null)
                slot.colorBar.color = GetTierColor(tier);

            // 激活并播放滑入动画
            slot.root.SetActive(true);

            var slotRT = slot.root.GetComponent<RectTransform>();
            if (slotRT != null)
            {
                // 保证 Alpha 完整可见
                var cg = slot.root.GetComponent<CanvasGroup>();
                if (cg == null) cg = slot.root.AddComponent<CanvasGroup>();
                cg.alpha = 1f;

                yield return StartCoroutine(GiftEffectSystem.SlideInFromLeft(slotRT, 0.2f, -540f));
            }

            // 停留 4s
            yield return new WaitForSeconds(4f);

            // 淡出
            {
                var cg = slot.root.GetComponent<CanvasGroup>();
                if (cg == null) cg = slot.root.AddComponent<CanvasGroup>();
                float elapsed = 0f;
                float dur = 0.3f;
                while (elapsed < dur)
                {
                    elapsed += Time.deltaTime;
                    cg.alpha = Mathf.Lerp(1f, 0f, elapsed / dur);
                    yield return null;
                }
                cg.alpha = 0f;
            }

            slot.root.SetActive(false);
            _activeBannerCount = Mathf.Max(0, _activeBannerCount - 1);

            // 尝试显示队列中的下一条
            TryShowNextBanner();
        }

        // =========================================================================
        //  工具方法
        // =========================================================================

        /// <summary>礼物ID → 中文展示名（用于横幅显示）</summary>
        private static string GetGiftDisplayName(string giftId) => giftId switch
        {
            "fairy_wand"      => "仙女棒",
            "ability_pill"    => "能力药丸",
            "donut"           => "甜甜圈",
            "energy_battery"  => "能量电池",
            "love_explosion"  => "爱的爆炸",
            "mystery_airdrop" => "神秘空投",
            _                 => giftId
        };

        /// <summary>礼物ID → 简短效果描述（用于横幅右侧提示）</summary>
        private static string GetGiftEffect(string giftId) => giftId switch
        {
            "fairy_wand"      => "[效率+]",
            "ability_pill"    => "[全员效率+50% 30s]",
            "donut"           => "[食物+100 城门+200]",
            "energy_battery"  => "[炉温+30]",
            "love_explosion"  => "[AOE伤害 矿工全满 城门+200]",
            "mystery_airdrop" => "[超级补给!]",
            _                 => ""
        };

        private static Color GetTierColor(int tier)
        {
            switch (tier)
            {
                case 1: return new Color(1.0f, 0.95f, 0.7f); // 浅金
                case 2: return new Color(0.4f, 0.7f, 1.0f);  // 蓝
                case 3: return new Color(0.7f, 0.4f, 1.0f);  // 紫
                case 4: return new Color(1.0f, 0.6f, 0.1f);  // 橙
                case 5: return new Color(1.0f, 0.84f, 0f);   // 金
                default: return Color.white;
            }
        }

        /// <summary>安全播放 ParticleSystem（空引用防御）</summary>
        private static void PlayPS(ParticleSystem ps)
        {
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play();
            }
        }

        /// <summary>安全播放音效（检查 AudioManager.Instance 非空）</summary>
        private static void PlaySFX(string sfxName)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(sfxName);
        }

        /// <summary>安全 SetActive（空引用防御）</summary>
        private static void SafeSetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }

        /// <summary>重置 Graphic 的 Alpha 到指定值（不激活/隐藏对象）</summary>
        private static void ResetGraphicAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null) return;
            Color c = graphic.color;
            c.a = alpha;
            graphic.color = c;
        }

        // =========================================================================
        //  调试辅助（Editor Only）
        // =========================================================================

#if UNITY_EDITOR
        [ContextMenu("Test T1 Effect")]
        private void TestT1() => ShowGiftEffect("fairy_wand", "测试用户", 1);

        [ContextMenu("Test T2 Effect")]
        private void TestT2() => ShowGiftEffect("ability_pill", "测试用户", 2);

        [ContextMenu("Test T3 Effect")]
        private void TestT3() => ShowGiftEffect("donut", "测试用户", 3);

        [ContextMenu("Test T4 Effect")]
        private void TestT4() => ShowGiftEffect("battery", "测试用户", 4);

        [ContextMenu("Test T5 Effect")]
        private void TestT5() => ShowGiftEffect("mystery_drop", "测试用户A", 5);
#endif
    }
}
