using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using DrscfZ.Core;
using DrscfZ.Systems;
using DrscfZ.Survival;
using DrscfZ.Utils;

namespace DrscfZ.UI
{
    /// <summary>
    /// 礼物动画UI - 收到礼物时弹出WebM透明视频动画
    ///
    /// 素材: Assets/Art/GiftGifs/tier{N}-sp.webm (VP8 + Alpha)
    /// VideoClip通过tierVideoClips数组在Inspector中拖入
    ///
    /// 不做队列，多个礼物同时显示，超过上限移除最早的
    /// </summary>
    public class GiftAnimationUI : MonoBehaviour
    {
        public static GiftAnimationUI Instance { get; private set; }

        [Header("Config")]
        [Tooltip("同屏最大礼物动画数")]
        [SerializeField] private int maxSimultaneous = 3;
        [Tooltip("动画区域（锚定在屏幕下方）")]
        public RectTransform animationContainer;

        [Header("Animation Size (按tier价值递增，宽度按视频比例自适应)")]
        [Tooltip("tier1(1抖币) 最便宜最小")]
        [SerializeField] private float tier1Height = 420f;
        [Tooltip("tier2(10抖币) 能力药丸")]
        [SerializeField] private float tier2Height = 380f;
        [Tooltip("tier3(52抖币)")]
        [SerializeField] private float tier3Height = 480f;
        [Tooltip("tier4(99抖币)")]
        [SerializeField] private float tier4Height = 540f;
        [Tooltip("tier5(199抖币)")]
        [SerializeField] private float tier5Height = 580f;
        [Tooltip("tier6(520抖币) 最贵最大")]
        [SerializeField] private float tier6Height = 640f;

        [Header("Video Clips (tier1~tier6, Inspector中拖入WebM)")]
        [SerializeField] private VideoClip[] tierVideoClips = new VideoClip[6];

        [Header("VIP入场 - 全屏显示（场景预创建引用）")]
        [Tooltip("全屏 RawImage，用于播放 VIP 入场视频")]
        [SerializeField] private RawImage    _vipVideoDisplay;
        [Tooltip("控制 VIP 视频整体透明度（淡入淡出）")]
        [SerializeField] private CanvasGroup _vipCanvasGroup;
        [SerializeField] private float _vipFadeIn  = 0.5f;
        [SerializeField] private float _vipFadeOut = 1.0f;
        [Tooltip("最多排队多少个 VIP 入场（超出丢弃）")]
        [SerializeField] private int _maxVipQueue = 3;

        // ==================== 礼物信息字典 ====================

        /// <summary>礼物ID → (中文名, 效果描述)</summary>
        private static readonly Dictionary<string, (string name, string desc)> GiftInfo =
            new Dictionary<string, (string, string)>
        {
            { "fairy_wand",      ("仙女棒",   "效率+5%") },
            { "ability_pill",    ("能力药丸", "全员效率+50%") },
            { "donut",           ("甜甜圈",   "城门+200 食物+100") },
            { "energy_battery",  ("能量电池", "炉温+30℃ 效率+30%") },
            { "battery",         ("能量电池", "炉温+30℃ 效率+30%") }, // 兼容旧ID
            { "mystery_airdrop", ("神秘空投", "超级补给") },
            { "mystery_drop",    ("神秘空投", "超级补给") },            // 兼容旧ID
            { "airdrop",         ("神秘空投", "超级补给") },
            { "love_explosion",  ("爱的爆炸", "AOE伤害 矿工全满 城门+200") },
            { "magic_candy",     ("魔法糖",   "效率+10%") },
            { "super_blaster",   ("超能暴射", "全场伤害+50%") },
            { "love_blast",      ("爱心暴射", "全场伤害+50%") },        // 兼容旧ID
        };

        // 活跃动画实例（礼物弹窗）
        private List<GiftAnimInstance> _activeAnims = new List<GiftAnimInstance>();
        // r15 Legacy 阶段 A 解耦：删除 _giftHandler / _subscribed 字段（旧角力游戏 GiftHandler 路径）
        // 保留 _survivalSubscribed（SurvivalGameManager.OnGiftReceived 是当前唯一活跃路径）
        private bool _survivalSubscribed;

        // VIP 入场队列
        private struct VIPRequest
        {
            public VideoClip clip;
            public string    playerName;
            public string    vipTitle;
            public float     maxDuration;
        }
        private Queue<VIPRequest> _vipQueue   = new Queue<VIPRequest>();
        private bool              _vipPlaying = false;
        private VideoPlayer       _vipPlayer;
        private RenderTexture     _vipRenderTex;

        private class GiftAnimInstance
        {
            public GameObject go;
            public CanvasGroup cg;
            public VideoPlayer videoPlayer;
            public RenderTexture renderTexture;
            public float totalDuration;
            public float elapsed;
        }

        // ==================== 生命周期 ====================

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            // 初始化：确保 VIP 显示层隐藏
            if (_vipVideoDisplay != null) _vipVideoDisplay.enabled = false;
            if (_vipCanvasGroup  != null) _vipCanvasGroup.alpha    = 0f;
        }

        private void OnEnable() { TrySubscribe(); }

        private void Start()
        {
            if (animationContainer == null)
                animationContainer = GetComponent<RectTransform>();
            TrySubscribe();
        }

        private void TrySubscribe()
        {
            // r15 Legacy 阶段 A 解耦：移除旧 GiftHandler（卡皮巴拉对决 gift_received）订阅块
            // 仅保留 SurvivalGameManager（survival_gift）— 当前唯一活跃路径
            if (!_survivalSubscribed)
            {
                var sgm = SurvivalGameManager.Instance;
                if (sgm != null)
                {
                    sgm.OnGiftReceived += OnSurvivalGiftReceived;
                    _survivalSubscribed = true;
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            // r15 Legacy 阶段 A 解耦：移除旧 GiftHandler 解订阅块；仅保留 SurvivalGameManager
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
                sgm.OnGiftReceived -= OnSurvivalGiftReceived;

            // 清理所有礼物 RenderTexture
            foreach (var a in _activeAnims)
                CleanupInstance(a);

            // 清理 VIP 视频
            CleanupVIPVideo();
        }

        private void Update()
        {
            for (int i = _activeAnims.Count - 1; i >= 0; i--)
            {
                var a = _activeAnims[i];
                if (a.go == null)
                {
                    CleanupInstance(a);
                    _activeAnims.RemoveAt(i);
                    continue;
                }

                a.elapsed += Time.deltaTime;

                // 结束
                if (a.elapsed >= a.totalDuration)
                {
                    CleanupInstance(a);
                    Destroy(a.go);
                    _activeAnims.RemoveAt(i);
                    continue;
                }

                // 淡出（最后0.8秒）
                float fadeTime = 0.8f;
                float fadeStart = a.totalDuration - fadeTime;
                if (a.elapsed > fadeStart && a.cg != null)
                {
                    a.cg.alpha = Mathf.Clamp01(1f - (a.elapsed - fadeStart) / fadeTime);
                }
            }
        }

        private void CleanupInstance(GiftAnimInstance a)
        {
            if (a.videoPlayer != null)
                a.videoPlayer.Stop();
            if (a.renderTexture != null)
                a.renderTexture.Release();
        }

        // ==================== 礼物触发 ====================

        // r15 Legacy 阶段 A 解耦：删除 OnGiftReceived(GiftReceivedData) 旧角力游戏入口
        // 该函数仅由 GiftHandler.OnGiftReceived 触发，删除 GiftHandler 订阅后即变孤儿；
        // 当前所有礼物动画统一走 OnSurvivalGiftReceived → ShowGiftPopup 路径

        /// <summary>
        /// 生存模式礼物事件适配器：将 SurvivalGiftData 转换为 GiftReceivedData 后触发动画
        /// </summary>
        private void OnSurvivalGiftReceived(SurvivalGiftData gift)
        {
            if (!gameObject.activeInHierarchy) return;
            if (!SettingsPanelUI.GiftVideoEnabled) return;

            // 转换为 GiftReceivedData（GiftAnimationUI 的统一输入格式）
            var adapted = new GiftReceivedData
            {
                playerId   = gift.playerId,
                playerName = gift.playerName,
                avatarUrl  = gift.avatarUrl,
                giftId     = gift.giftId,
                giftName   = gift.giftName,
                forceValue = gift.contribution,
                giftCount  = 1,
                tier       = gift.giftTier.ToString(),
                camp       = "left",  // 生存模式无阵营，统一左侧弹出
                isSummon   = false,
            };

            int tier = MapGiftToTier(adapted);

            while (_activeAnims.Count >= maxSimultaneous && _activeAnims.Count > 0)
            {
                CleanupInstance(_activeAnims[0]);
                if (_activeAnims[0].go != null) Destroy(_activeAnims[0].go);
                _activeAnims.RemoveAt(0);
            }

            ShowGiftPopup(adapted, tier, adapted.camp);
        }

        private void ShowGiftPopup(GiftReceivedData gift, int tier, string camp)
        {
            // === 根物体 ===
            var go = new GameObject($"Gift_t{tier}_video", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(animationContainer, false);
            var cg = go.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            var rt = go.GetComponent<RectTransform>();

            // 基准高度按tier递增
            float baseHeight = tier switch
            {
                1 => tier1Height,
                2 => tier2Height,
                3 => tier3Height,
                4 => tier4Height,
                5 => tier5Height,
                _ => tier6Height
            };

            // 根据视频实际宽高比计算宽度，避免拉伸
            int clipIdx = Mathf.Clamp(tier - 1, 0, 5);
            VideoClip clip = (tierVideoClips != null && clipIdx < tierVideoClips.Length)
                ? tierVideoClips[clipIdx] : null;

            float aspectRatio = 1f; // 默认1:1
            if (clip != null && clip.height > 0)
                aspectRatio = (float)clip.width / clip.height;

            // 限制最大尺寸：动画顶端不超出屏幕（考虑Canvas缩放）
            float screenH = animationContainer.rect.height > 0
                ? animationContainer.rect.height / 0.4f  // 容器占屏幕40%，反推全屏高度
                : 1920f;
            // 动画底部贴近容器底部，顶部不能超过屏幕顶部
            // yPos ≈ size.y * 0.15，顶部 ≈ yPos + size.y/2 = size.y * 0.65
            // 需要 size.y * 0.65 < screenH → size.y < screenH / 0.65
            float maxH = screenH / 0.65f;
            float finalHeight = Mathf.Min(baseHeight, maxH);

            Vector2 size = new Vector2(finalHeight * aspectRatio, finalHeight);
            rt.sizeDelta = size;

            // 位置：X轴居中（0），Y轴由 yPos 控制
            float xPos = 0f;

            // Y位置：屏幕中下部（屏幕中心往下12%）
            float yPos = -Screen.height * 0.12f;
            rt.anchoredPosition = new Vector2(xPos, yPos);

            // === VideoPlayer + RawImage ===
            // clip和clipIdx已在上方计算尺寸时获取

            RenderTexture renderTex = null;
            VideoPlayer vp = null;

            if (clip != null)
            {
                // 创建RenderTexture（匹配视频分辨率）
                int texW = (int)clip.width;
                int texH = (int)clip.height;
                if (texW <= 0) texW = 512;
                if (texH <= 0) texH = 512;
                renderTex = new RenderTexture(texW, texH, 0, RenderTextureFormat.ARGB32);
                renderTex.Create();

                // RawImage显示视频纹理
                var rawImgGo = new GameObject("VideoDisplay", typeof(RectTransform));
                rawImgGo.transform.SetParent(go.transform, false);
                var rawImgRT = rawImgGo.GetComponent<RectTransform>();
                rawImgRT.anchorMin = Vector2.zero;
                rawImgRT.anchorMax = Vector2.one;
                rawImgRT.offsetMin = Vector2.zero;
                rawImgRT.offsetMax = Vector2.zero;

                var rawImg = rawImgGo.AddComponent<RawImage>();
                rawImg.texture = renderTex;
                rawImg.raycastTarget = false;

                // 确保RawImage使用支持Alpha透明的材质
                // 默认UI/Default shader支持Alpha，但需要确认color.a=1
                rawImg.color = Color.white;

                // VideoPlayer
                vp = go.AddComponent<VideoPlayer>();
                vp.clip = clip;
                vp.renderMode = VideoRenderMode.RenderTexture;
                vp.targetTexture = renderTex;
                vp.isLooping = true;
                vp.playOnAwake = false;
                vp.audioOutputMode = VideoAudioOutputMode.None;
                vp.skipOnDrop = true;

                // VP9 Alpha: 确保RenderTexture在播放前清空为透明
                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = renderTex;
                GL.Clear(true, true, Color.clear);
                RenderTexture.active = prev;

                vp.Play();
            }
            else
            {
                // 没有视频时：不显示占位色块（视频素材未就绪），仅显示玩家信息
                var fallbackGo = new GameObject("Fallback", typeof(RectTransform));
                fallbackGo.transform.SetParent(go.transform, false);
                var fbRT = fallbackGo.GetComponent<RectTransform>();
                fbRT.anchorMin = Vector2.zero;
                fbRT.anchorMax = Vector2.one;
                fbRT.offsetMin = Vector2.zero;
                fbRT.offsetMax = Vector2.zero;

                // 透明背景，不显示色块
                var img = fallbackGo.AddComponent<Image>();
                img.color = Color.clear;
                img.raycastTarget = false;

                // 不显示 "Tier N" 文字（由 PlayerInfoOverlay 承载信息）
                var txtGo = new GameObject("FallbackText", typeof(RectTransform));
                txtGo.transform.SetParent(fallbackGo.transform, false);
                var txtRT = txtGo.GetComponent<RectTransform>();
                txtRT.anchorMin = Vector2.zero;
                txtRT.anchorMax = Vector2.one;
                txtRT.offsetMin = Vector2.zero;
                txtRT.offsetMax = Vector2.zero;
                var txt = txtGo.AddComponent<TextMeshProUGUI>();
                // AUTO-INJECT: 统一 Alibaba 字体
                if (txt.font == null) {
                    var __f = Resources.Load<TMPro.TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF");
                    if (__f == null) __f = Resources.Load<TMPro.TMP_FontAsset>("Fonts/ChineseFont SDF");
                    if (__f != null) txt.font = __f;
                }
                txt.text = "";  // 隐藏，等待真实视频素材
                txt.fontSize = 36;
                txt.alignment = TextAlignmentOptions.Center;
                txt.color = Color.white;
            }

            // === 玩家信息（头像+名字，视频下方居中） ===
            CreatePlayerInfoOverlay(go.transform, gift, tier, size);

            // === 注册实例 ===
            float duration = GetDuration(tier);
            var inst = new GiftAnimInstance
            {
                go = go,
                cg = cg,
                videoPlayer = vp,
                renderTexture = renderTex,
                totalDuration = duration,
                elapsed = 0
            };
            _activeAnims.Add(inst);

            // === 右侧阵营镜像翻转 ===
            // 对视频容器水平镜像（localScale.x = -1），但玩家信息不翻转
            if (camp == "right")
            {
                // 找到 VideoDisplay 或 Fallback 子对象，只翻转视频部分
                for (int ci = 0; ci < go.transform.childCount; ci++)
                {
                    var child = go.transform.GetChild(ci);
                    if (child.name == "VideoDisplay" || child.name == "Fallback")
                    {
                        child.localScale = new Vector3(-1f, 1f, 1f);
                        break;
                    }
                }
            }

            // === 弹入动效 ===
            StartCoroutine(AnimatePopup(go.transform, tier, duration, camp));
        }

        // ==================== 玩家信息 ====================

        /// <summary>
        /// 在礼物动画底部创建玩家信息条（头像 + 名字 + 礼物名 + 推力）
        /// 替代原GiftNotificationUI的底部文字通知，信息合并到动画弹窗中
        /// </summary>
        private void CreatePlayerInfoOverlay(Transform parent, GiftReceivedData gift, int tier, Vector2 parentSize)
        {
            string displayName = gift.playerName;
            if (string.IsNullOrEmpty(displayName)) return;

            // 名字最长5个字
            if (displayName.Length > 5)
                displayName = displayName.Substring(0, 5);

            // 中文字体（提前加载，多处复用）
            var chFont = Resources.Load<TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF") ?? Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");

            // === 外层垂直容器：上行=头像+名字+礼物名，下行=推力 ===
            var outerGo = new GameObject("PlayerInfoOuter", typeof(RectTransform));
            outerGo.transform.SetParent(parent, false);
            var outerRT = outerGo.GetComponent<RectTransform>();
            // 锚定在视频底部偏内（10%高度处），文字悬浮在视频效果下方
            outerRT.anchorMin = new Vector2(0.5f, 0.08f);
            outerRT.anchorMax = new Vector2(0.5f, 0.08f);
            outerRT.pivot = new Vector2(0.5f, 0.5f);
            outerRT.sizeDelta = new Vector2(0f, 0f);
            outerRT.anchoredPosition = new Vector2(0f, 0f);

            // 半透明背景
            var outerBg = outerGo.AddComponent<Image>();
            outerBg.color = new Color(0f, 0f, 0f, 0.6f);
            outerBg.raycastTarget = false;

            var vlg = outerGo.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = 2f;
            vlg.padding = new RectOffset(14, 14, 6, 6);
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = false;
            vlg.childControlHeight = true;

            var outerCSF = outerGo.AddComponent<ContentSizeFitter>();
            outerCSF.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            outerCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // === 第一行：头像 + 玩家名 + 礼物名 ===
            var row1Go = new GameObject("Row1", typeof(RectTransform));
            row1Go.transform.SetParent(outerGo.transform, false);

            var hlg = row1Go.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 6f;
            hlg.padding = new RectOffset(0, 0, 0, 0);
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            var row1CSF = row1Go.AddComponent<ContentSizeFitter>();
            row1CSF.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            row1CSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // --- 头像（圆形, 44x44） ---
            var avatarGo = new GameObject("Avatar", typeof(RectTransform));
            avatarGo.transform.SetParent(row1Go.transform, false);
            var avatarRT = avatarGo.GetComponent<RectTransform>();
            avatarRT.sizeDelta = new Vector2(44f, 44f);

            var avatarLE = avatarGo.AddComponent<LayoutElement>();
            avatarLE.preferredWidth = 44f;
            avatarLE.preferredHeight = 44f;
            avatarLE.minWidth = 44f;

            var avatarRawImg = avatarGo.AddComponent<RawImage>();
            avatarRawImg.raycastTarget = false;

            // 圆形遮罩材质
            var circleMat = Resources.Load<Material>("Materials/Mat_CircleMask");
            if (circleMat != null)
            {
                var matInst = new Material(circleMat);
                matInst.SetFloat("_BorderWidth", 0.05f);
                Color borderCol = GetTierInfoColor(tier);
                matInst.SetColor("_BorderColor", borderCol);
                avatarRawImg.material = matInst;
            }

            // 异步加载头像：加载完成前保持透明，加载成功后才显示
            avatarRawImg.texture = null;
            avatarRawImg.color = Color.clear; // 加载前透明，避免色块
            if (!string.IsNullOrEmpty(gift.avatarUrl))
            {
                var loader = AvatarLoader.Instance;
                if (loader != null)
                {
                    loader.Load(gift.avatarUrl, tex =>
                    {
                        if (avatarRawImg != null && tex != null)
                        {
                            avatarRawImg.texture = tex;
                            avatarRawImg.color = Color.white;
                        }
                    });
                }
            }
            // 无 URL 时头像保持透明（不显示色块）

            // --- 名字行：玩家名 + 礼物中文名 ---
            // 通过 GiftInfo 字典获取中文礼物名，降级到 gift.giftName，再降级到 giftId
            string giftId = gift.giftId ?? "";
            string giftDisplayName;
            string giftDesc = "";
            if (!string.IsNullOrEmpty(giftId) && GiftInfo.TryGetValue(giftId, out var info))
            {
                giftDisplayName = info.name;
                giftDesc        = info.desc;
            }
            else
            {
                giftDisplayName = string.IsNullOrEmpty(gift.giftName) ? giftId : gift.giftName;
            }
            string countStr = gift.giftCount > 1 ? $"×{gift.giftCount}" : "";
            string nameLine = $"{displayName}  {giftDisplayName}{countStr}";

            var nameGo = new GameObject("PlayerName", typeof(RectTransform));
            nameGo.transform.SetParent(row1Go.transform, false);

            var nameText = nameGo.AddComponent<TextMeshProUGUI>();
            nameText.text = nameLine;
            nameText.fontSize = 28;
            nameText.fontStyle = FontStyles.Bold;
            nameText.alignment = TextAlignmentOptions.Left;
            nameText.color = Color.white;
            nameText.enableWordWrapping = false;
            nameText.overflowMode = TextOverflowModes.Overflow;
            nameText.raycastTarget = false;

            var nameLE = nameGo.AddComponent<LayoutElement>();
            nameLE.preferredHeight = 44f;

            if (chFont != null) nameText.font = chFont;
            nameText.outlineWidth = 0.3f;
            nameText.outlineColor = new Color32(0, 0, 0, 220);

            // === 第二行：礼物效果描述（黄色, 22px） ===
            if (!string.IsNullOrEmpty(giftDesc))
            {
                var descGo = new GameObject("GiftDesc", typeof(RectTransform));
                descGo.transform.SetParent(outerGo.transform, false);

                var descText = descGo.AddComponent<TextMeshProUGUI>();
                descText.text = giftDesc;
                descText.fontSize = 22;
                descText.fontStyle = FontStyles.Bold;
                descText.alignment = TextAlignmentOptions.Center;
                descText.color = new Color(1f, 0.92f, 0.2f); // 黄色
                descText.enableWordWrapping = false;
                descText.overflowMode = TextOverflowModes.Overflow;
                descText.raycastTarget = false;

                if (chFont != null) descText.font = chFont;
                descText.outlineWidth = 0.25f;
                descText.outlineColor = new Color32(0, 0, 0, 200);

                var descLE = descGo.AddComponent<LayoutElement>();
                descLE.preferredHeight = 26f;
            }

            // 推力值行已移除（用户不需要显示推力数值）
        }

        /// <summary>获取tier对应的信息条颜色</summary>
        private static Color GetTierInfoColor(int tier)
        {
            switch (tier)
            {
                case 1: return new Color(0.9f, 0.9f, 0.85f);
                case 2: return new Color(0.4f, 0.7f, 1f);
                case 3: return new Color(0.7f, 0.4f, 1f);
                case 4: return new Color(1f, 0.84f, 0f);
                case 5: return new Color(1f, 0.35f, 0.2f);
                case 6: return new Color(1f, 0.92f, 0.5f);
                default: return Color.white;
            }
        }

        // ==================== 弹入动效 ====================

        private IEnumerator AnimatePopup(Transform t, int tier, float totalDuration, string camp = "left")
        {
            if (t == null) yield break;

            var rt = t as RectTransform;
            if (rt == null) rt = t.GetComponent<RectTransform>();

            Vector2 targetPos = rt.anchoredPosition;
            Vector2 startPos = targetPos + new Vector2(0f, -300f); // 从下方弹入，X轴居中不偏移
            rt.anchoredPosition = startPos;
            t.localScale = Vector3.one * 0.2f;

            // Phase 1: 滑入 + 放大 (0.3s)
            float phase1 = 0.3f;
            float elapsed = 0;
            while (elapsed < phase1 && t != null)
            {
                elapsed += Time.deltaTime;
                float p = Mathf.Clamp01(elapsed / phase1);
                float eased = 1f + 2.7f * Mathf.Pow(p - 1f, 3f) + 1.7f * Mathf.Pow(p - 1f, 2f);
                rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, eased);
                t.localScale = Vector3.one * Mathf.Lerp(0.2f, 1.1f, eased);
                yield return null;
            }
            if (t == null) yield break;

            // Phase 2: 回弹缩小 (0.15s)
            elapsed = 0;
            float phase2 = 0.15f;
            while (elapsed < phase2 && t != null)
            {
                elapsed += Time.deltaTime;
                float p = Mathf.Clamp01(elapsed / phase2);
                t.localScale = Vector3.one * Mathf.Lerp(1.1f, 0.95f, p);
                yield return null;
            }
            if (t == null) yield break;

            // Phase 3: 弹回1.0 (0.1s)
            elapsed = 0;
            float phase3 = 0.1f;
            while (elapsed < phase3 && t != null)
            {
                elapsed += Time.deltaTime;
                float p = Mathf.Clamp01(elapsed / phase3);
                t.localScale = Vector3.one * Mathf.Lerp(0.95f, 1f, p);
                yield return null;
            }
            if (t == null) yield break;
            t.localScale = Vector3.one;
            rt.anchoredPosition = targetPos;

            // Phase 4: 轻微晃动
            float wobbleDuration = tier <= 2 ? 0.5f : (tier <= 4 ? 0.8f : 1.2f);
            float wobbleAmplitude = tier <= 2 ? 3f : (tier <= 4 ? 5f : 8f);
            elapsed = 0;
            while (elapsed < wobbleDuration && t != null)
            {
                elapsed += Time.deltaTime;
                float decay = 1f - elapsed / wobbleDuration;
                float angle = Mathf.Sin(elapsed * 15f) * wobbleAmplitude * decay;
                t.localRotation = Quaternion.Euler(0, 0, angle);
                yield return null;
            }
            if (t != null)
                t.localRotation = Quaternion.identity;
        }

        // ==================== 映射 ====================

        private int MapGiftToTier(GiftReceivedData gift)
        {
            // 1. 优先使用服务端传来的tier（数字字符串 "1"~"6"）
            if (!string.IsNullOrEmpty(gift.tier) && int.TryParse(gift.tier, out int t))
                return Mathf.Clamp(t, 1, 6);

            // 2. 兼容旧格式字符串tier
            switch (gift.tier)
            {
                case "basic":     return 1;
                case "common":    return 2;
                case "rare":      return 3;
                case "epic":      return 4;
                case "legendary": return 5;
            }

            // 3. 按giftId精确匹配（不依赖forceValue）
            switch (gift.giftId)
            {
                case "fairy_wand":                      return 1;
                case "magic_candy":                     return 1; // tier1 别名
                case "ability_pill":                    return 2;
                case "donut":                           return 3;
                case "energy_battery":                  return 4;
                case "battery":                         return 4; // 兼容旧ID
                case "love_explosion":                  return 5;
                case "love_blast":                      return 5;
                case "super_blaster":                   return 5; // tier5 别名
                case "mystery_drop":                    return 6;
                case "mystery_airdrop":                 return 6; // 兼容新ID
                case "airdrop":                         return 6; // 兼容通用ID
            }

            // 4. 最终fallback用forceValue反推（保底）
            float value = gift.forceValue / Mathf.Max(1, gift.giftCount);
            if (value >= 6000) return 6;
            if (value >= 2000) return 5;
            if (value >= 1000) return 4;
            if (value >= 500)  return 3;
            if (value >= 100)  return 2;
            return 1;
        }

        private float GetDuration(int tier)
        {
            switch (tier)
            {
                case 1:  return 2.5f;
                case 2:  return 3f;
                case 3:  return 3.5f;
                case 4:  return 4f;
                case 5:  return 4.5f;
                case 6:  return 5f;
                default: return 3f;
            }
        }

        // ==================== VIP 入场 ====================

        /// <summary>
        /// 播放 VIP 入场 WebM 视频（与礼物特效使用同一套 VideoPlayer/RenderTexture 机制）。
        /// 由 VIPAnnouncementUI 事件路由调用。
        /// </summary>
        public void ShowVIPEffect(VideoClip clip, string playerName, string vipTitle, float maxDuration)
        {
            if (clip == null || !gameObject.activeInHierarchy) return;
            if (_vipQueue.Count >= _maxVipQueue) return; // 队列满则丢弃，防止积压

            _vipQueue.Enqueue(new VIPRequest
            {
                clip        = clip,
                playerName  = playerName,
                vipTitle    = vipTitle,
                maxDuration = maxDuration,
            });

            if (!_vipPlaying)
                StartCoroutine(ProcessVIPQueue());
        }

        private IEnumerator ProcessVIPQueue()
        {
            _vipPlaying = true;
            while (_vipQueue.Count > 0)
            {
                var req = _vipQueue.Dequeue();
                yield return PlayVIPVideoCoroutine(req);
                yield return new WaitForSeconds(0.3f);
            }
            _vipPlaying = false;
        }

        /// <summary>播放单条 VIP 入场视频：创建 RenderTexture → 播放 → 淡入等待淡出 → 清理</summary>
        private IEnumerator PlayVIPVideoCoroutine(VIPRequest req)
        {
            if (_vipVideoDisplay == null || _vipCanvasGroup == null)
            {
                Debug.LogWarning("[GiftAnimationUI] VIP显示引用未设置，请在Inspector拖入_vipVideoDisplay和_vipCanvasGroup");
                yield break;
            }

            // 创建 RenderTexture（与礼物 WebM 完全相同的流程）
            int texW = req.clip.width  > 0 ? (int)req.clip.width  : 1080;
            int texH = req.clip.height > 0 ? (int)req.clip.height : 1920;
            _vipRenderTex = new RenderTexture(texW, texH, 0, RenderTextureFormat.ARGB32);
            _vipRenderTex.Create();

            var prev = RenderTexture.active;
            RenderTexture.active = _vipRenderTex;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = prev;

            // 配置 VideoPlayer（复用 GiftAnimationUI 对象上的组件，避免多余 AddComponent）
            if (_vipPlayer == null)
                _vipPlayer = gameObject.AddComponent<VideoPlayer>();

            _vipPlayer.clip            = req.clip;
            _vipPlayer.renderMode      = VideoRenderMode.RenderTexture;
            _vipPlayer.targetTexture   = _vipRenderTex;
            _vipPlayer.isLooping       = false;
            _vipPlayer.playOnAwake     = false;
            _vipPlayer.audioOutputMode = VideoAudioOutputMode.None;
            _vipPlayer.skipOnDrop      = true;

            _vipVideoDisplay.texture = _vipRenderTex;
            _vipVideoDisplay.color   = Color.white;
            _vipVideoDisplay.enabled = true;

            _vipPlayer.Play();

            // 淡入
            yield return FadeVIPCanvasGroup(0f, 1f, _vipFadeIn);

            // 等待播放（不超过 maxDuration）
            float waited = 0f;
            while (_vipPlayer != null && _vipPlayer.isPlaying && waited < req.maxDuration)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            // 淡出
            yield return FadeVIPCanvasGroup(1f, 0f, _vipFadeOut);

            CleanupVIPVideo();
        }

        private void CleanupVIPVideo()
        {
            _vipPlaying = false;

            if (_vipPlayer != null)
            {
                _vipPlayer.Stop();
                _vipPlayer.clip          = null;
                _vipPlayer.targetTexture = null;
            }
            if (_vipRenderTex != null)
            {
                _vipRenderTex.Release();
                Destroy(_vipRenderTex);
                _vipRenderTex = null;
            }
            if (_vipVideoDisplay != null)
            {
                _vipVideoDisplay.texture = null;
                _vipVideoDisplay.enabled = false;
            }
            if (_vipCanvasGroup != null)
                _vipCanvasGroup.alpha = 0f;
        }

        private IEnumerator FadeVIPCanvasGroup(float from, float to, float duration)
        {
            if (_vipCanvasGroup == null) yield break;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _vipCanvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            _vipCanvasGroup.alpha = to;
        }
    }
}
