using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrscfZ.UI;

namespace DrscfZ.Editor
{
    /// <summary>
    /// Gift_Canvas 场景结构自动化建立工具
    ///
    /// 菜单：Tools → DrscfZ → Setup Gift Canvas (T1-T5 Effects)
    ///
    /// 功能：
    ///   1. 在场景根创建 Gift_Canvas（Sort Order=100）
    ///   2. 创建 T1~T5 效果面板子结构
    ///   3. 创建 GiftBannerQueue（3个预创建横幅槽）
    ///   4. 将 GiftNotificationUI 挂载到 Gift_Canvas 并自动绑定所有字段
    ///   5. 保存场景
    ///
    /// 注意：
    ///   - 不使用 DisplayDialog（会阻塞 Coplay 进程）
    ///   - 若对象已存在则跳过，不重复创建
    ///   - T2~T5 面板初始 inactive（Rule #2）
    ///   - Gift_Canvas 始终 active（Rule #7，脚本挂在其上）
    /// </summary>
    public static class SetupGiftCanvas
    {
        [MenuItem("Tools/DrscfZ/Setup Gift Canvas (T1-T5 Effects)")]
        public static void Execute()
        {
            // ── 1. 查找或创建 Gift_Canvas ─────────────────────────────────────
            var existing = GameObject.Find("Gift_Canvas");
            if (existing != null)
            {
                Debug.Log("[SetupGiftCanvas] Gift_Canvas 已存在，跳过创建（直接检查子结构）");
                EnsureSubStructure(existing);
                return;
            }

            // 创建 Canvas
            var canvasGo = new GameObject("Gift_Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder  = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight  = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            Debug.Log("[SetupGiftCanvas] Gift_Canvas 创建完成 ✅");

            // ── 2. 挂载 GiftNotificationUI ──────────────────────────────────
            EnsureSubStructure(canvasGo);

            // ── 3. 保存场景（Rule #8）────────────────────────────────────────
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SetupGiftCanvas] ✅ 完成！Gift_Canvas 已创建并保存场景");
        }

        // ── 子结构确保方法（已有Canvas时也可调用）──────────────────────────────

        private static void EnsureSubStructure(GameObject canvasGo)
        {
            // GiftNotificationUI has been removed; skipping component setup.
            // var ui = canvasGo.GetComponent<GiftNotificationUI>();
            bool uiNew = false;
            // if (ui == null) { ui = canvasGo.AddComponent<GiftNotificationUI>(); uiNew = true; }

            var t = canvasGo.transform;

            // ── T1_StarParticle ──────────────────────────────────────────────
            var t1Go = EnsureChild(t, "T1_StarParticle");
            if (t1Go.GetComponent<ParticleSystem>() == null)
            {
                var ps = t1Go.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.loop = false;
                main.playOnAwake = false;
                main.maxParticles = 50;
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.3f));
                main.startSize  = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
                main.duration   = 0.5f;
                // 需要 ParticleSystemRenderer 为 Canvas 可见（World Space 粒子通常不在UI上，这里保持默认）
                Debug.Log("[SetupGiftCanvas] T1_StarParticle 粒子系统创建 ✅");
            }
            // T1 保持 active（PlayOnAwake=false，不会自动播放）
            SetRectFullscreen(t1Go);

            // ── T2_BorderEffect ──────────────────────────────────────────────
            var t2Go = EnsureChild(t, "T2_BorderEffect");
            t2Go.SetActive(false);
            SetRectFullscreen(t2Go);
            AddImageBackground(t2Go, new Color(0f, 0f, 0f, 0f)); // 透明背景

            var t2TL = EnsureChildWithPS(t2Go.transform, "TopLeft_PS");
            var t2TR = EnsureChildWithPS(t2Go.transform, "TopRight_PS");
            var t2BL = EnsureChildWithPS(t2Go.transform, "BotLeft_PS");
            var t2BR = EnsureChildWithPS(t2Go.transform, "BotRight_PS");

            // 四角定位
            SetCornerAnchor(t2TL, false, true);   // 左上
            SetCornerAnchor(t2TR, true, true);    // 右上
            SetCornerAnchor(t2BL, false, false);  // 左下
            SetCornerAnchor(t2BR, true, false);   // 右下

            var t2Ring = EnsureChild(t2Go.transform, "CenterRing_Image");
            SetRectCenter(t2Ring, 300f, 300f);
            var ringImg = EnsureComponent<Image>(t2Ring);
            ringImg.color = new Color(0.5f, 0.9f, 1f, 0.8f);

            Debug.Log("[SetupGiftCanvas] T2_BorderEffect 创建 ✅");

            // ── T3_GiftBounce ────────────────────────────────────────────────
            var t3Go = EnsureChild(t, "T3_GiftBounce");
            t3Go.SetActive(false);
            SetRectFullscreen(t3Go);
            AddImageBackground(t3Go, new Color(0f, 0f, 0f, 0f));

            var t3Icon = EnsureChild(t3Go.transform, "GiftIcon_Image");
            SetRectCenter(t3Icon, 300f, 300f);
            var iconImg = EnsureComponent<Image>(t3Icon);
            iconImg.color = new Color(0.8f, 0.5f, 1f, 1f);

            var t3Explode = EnsureChildWithPS(t3Go.transform, "Explode_PS");
            SetRectCenter(t3Explode, 1f, 1f);

            Debug.Log("[SetupGiftCanvas] T3_GiftBounce 创建 ✅");

            // ── T4_FullscreenGlow ────────────────────────────────────────────
            var t4Go = EnsureChild(t, "T4_FullscreenGlow");
            t4Go.SetActive(false);
            SetRectFullscreen(t4Go);

            var t4Orange = EnsureChild(t4Go.transform, "OrangeOverlay");
            SetRectFullscreen(t4Orange);
            var orangeImg = EnsureComponent<Image>(t4Orange);
            orangeImg.color = new Color(1f, 0.55f, 0.1f, 0f); // 初始透明

            var t4Battery = EnsureChild(t4Go.transform, "BatteryIcon");
            SetRectCenter(t4Battery, 180f, 180f);
            var batteryImg = EnsureComponent<Image>(t4Battery);
            batteryImg.color = new Color(1f, 0.8f, 0.2f, 1f);

            var t4Slider = EnsureChild(t4Go.transform, "ChargingSlider");
            var sliderRect = t4Slider.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.15f, 0.15f);
            sliderRect.anchorMax = new Vector2(0.85f, 0.25f);
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;
            var slider = EnsureComponent<Slider>(t4Slider);
            slider.value = 0f;
            // Slider 需要 Background + Fill Area 子对象（创建简单版本）
            EnsureSliderComponents(t4Slider, slider);

            Debug.Log("[SetupGiftCanvas] T4_FullscreenGlow 创建 ✅");

            // ── T5_EpicAirdrop ───────────────────────────────────────────────
            var t5Go = EnsureChild(t, "T5_EpicAirdrop");
            t5Go.SetActive(false);
            SetRectFullscreen(t5Go);

            var t5Black = EnsureChild(t5Go.transform, "BlackOverlay");
            SetRectFullscreen(t5Black);
            var blackImg = EnsureComponent<Image>(t5Black);
            blackImg.color = new Color(0f, 0f, 0f, 0f);

            var t5Box = EnsureChild(t5Go.transform, "AirdropBox");
            var boxRect = t5Box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.pivot     = new Vector2(0.5f, 0.5f);
            boxRect.anchoredPosition = new Vector2(0f, 1200f); // 初始在屏幕外上方
            boxRect.sizeDelta = new Vector2(160f, 160f);
            var boxImg = EnsureComponent<Image>(t5Box);
            boxImg.color = new Color(0.9f, 0.7f, 0.2f, 1f);

            var t5Fireworks = EnsureChildWithPS(t5Go.transform, "Fireworks_PS");
            SetRectCenter(t5Fireworks, 1f, 1f);

            // ResourceIcons 容器（4个子Image）
            var t5Icons = EnsureChild(t5Go.transform, "ResourceIcons");
            SetRectCenter(t5Icons, 400f, 100f);
            string[] iconNames   = { "Icon_Food", "Icon_Coal", "Icon_Ore", "Icon_Shield" };
            Color[]  iconColors  = {
                new Color(0.3f, 0.9f, 0.3f),   // 食物：绿
                new Color(0.5f, 0.5f, 0.5f),   // 煤炭：灰
                new Color(0.6f, 0.8f, 1.0f),   // 矿石：冰蓝
                new Color(1.0f, 0.9f, 0.3f),   // 城门：金
            };
            for (int i = 0; i < 4; i++)
            {
                var iconGo = EnsureChild(t5Icons.transform, iconNames[i]);
                var iconRect = iconGo.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot     = new Vector2(0.5f, 0.5f);
                iconRect.anchoredPosition = new Vector2(-150f + i * 100f, 0f);
                iconRect.sizeDelta = new Vector2(80f, 80f);
                var iconImgC = EnsureComponent<Image>(iconGo);
                iconImgC.color = iconColors[i];
            }

            var t5Name = EnsureChild(t5Go.transform, "PlayerNameText");
            SetRectCenter(t5Name, 800f, 100f);
            var nameTmp = EnsureComponent<TextMeshProUGUI>(t5Name);
            nameTmp.text      = "守护者到达！";
            nameTmp.fontSize  = 48f;
            nameTmp.color     = new Color(1f, 0.9f, 0.3f);
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.fontStyle = FontStyles.Bold;
            ApplyChineseFont(nameTmp);
            t5Name.SetActive(false);

            Debug.Log("[SetupGiftCanvas] T5_EpicAirdrop 创建 ✅");

            // ── GiftBannerQueue ──────────────────────────────────────────────
            var queueGo = EnsureChild(t, "GiftBannerQueue");
            var queueRect = queueGo.GetComponent<RectTransform>();
            queueRect.anchorMin = new Vector2(0f, 0.5f);
            queueRect.anchorMax = new Vector2(0f, 0.5f);
            queueRect.pivot     = new Vector2(0f, 0.5f);
            queueRect.anchoredPosition = new Vector2(10f, 0f);
            queueRect.sizeDelta = new Vector2(440f, 300f);

            var layout = EnsureComponent<VerticalLayoutGroup>(queueGo);
            layout.spacing           = 6f;
            layout.childAlignment    = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth  = true;
            layout.childForceExpandHeight = false;

            for (int i = 0; i < 3; i++)
                EnsureBannerSlot(queueGo.transform, i);

            Debug.Log("[SetupGiftCanvas] GiftBannerQueue 创建 ✅");

            // ── 自动绑定 GiftNotificationUI 字段 ────────────────────────────
            // GiftNotificationUI has been removed; skipping auto-wiring.
            // if (uiNew || true) { ... }
            EditorUtility.SetDirty(canvasGo);
            Debug.Log("[SetupGiftCanvas] Gift_Canvas 子结构已创建 ✅ (GiftNotificationUI 已移除，跳过绑定)");
        }

        // ==================== 子结构辅助方法 ====================

        /// <summary>确保某子节点存在，不存在则创建（含 RectTransform）</summary>
        private static GameObject EnsureChild(Transform parent, string name)
        {
            var tf = parent.Find(name);
            if (tf != null) return tf.gameObject;

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        /// <summary>确保含 ParticleSystem 的子节点存在</summary>
        private static GameObject EnsureChildWithPS(Transform parent, string name)
        {
            var go = EnsureChild(parent, name);
            if (go.GetComponent<ParticleSystem>() == null)
            {
                var ps = go.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.loop = false;
                main.playOnAwake = false;
                main.maxParticles = 100;
            }
            return go;
        }

        /// <summary>确保组件存在（已有则直接返回）</summary>
        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }

        /// <summary>设置全屏 RectTransform（stretch，offset=0）</summary>
        private static void SetRectFullscreen(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>设置居中固定尺寸 RectTransform</summary>
        private static void SetRectCenter(GameObject go, float w, float h)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(w, h);
        }

        /// <summary>添加半透明背景 Image（已有则跳过）</summary>
        private static void AddImageBackground(GameObject go, Color color)
        {
            var img = go.GetComponent<Image>();
            if (img == null)
            {
                img = go.AddComponent<Image>();
                img.color = color;
                img.raycastTarget = false;
            }
        }

        /// <summary>设置四角锚点（小尺寸粒子系统位置）</summary>
        private static void SetCornerAnchor(GameObject go, bool right, bool top)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) return;
            float ax = right ? 1f : 0f;
            float ay = top   ? 1f : 0f;
            rt.anchorMin = new Vector2(ax, ay);
            rt.anchorMax = new Vector2(ax, ay);
            rt.pivot     = new Vector2(ax, ay);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(200f, 200f);
        }

        /// <summary>为 Slider 创建所需的 Background + Fill 子结构</summary>
        private static void EnsureSliderComponents(GameObject sliderGo, Slider slider)
        {
            // Background
            var bg = EnsureChild(sliderGo.transform, "Background");
            SetRectFullscreen(bg);
            var bgImg = EnsureComponent<Image>(bg);
            bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Fill Area
            var fillArea = EnsureChild(sliderGo.transform, "Fill Area");
            var fillAreaRT = fillArea.GetComponent<RectTransform>();
            fillAreaRT.anchorMin = Vector2.zero;
            fillAreaRT.anchorMax = Vector2.one;
            fillAreaRT.offsetMin = Vector2.zero;
            fillAreaRT.offsetMax = Vector2.zero;

            var fill = EnsureChild(fillArea.transform, "Fill");
            var fillRT = fill.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(1f, 1f);
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            var fillImg = EnsureComponent<Image>(fill);
            fillImg.color = new Color(1f, 0.75f, 0.1f, 1f);

            slider.fillRect = fillRT;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
        }

        /// <summary>创建 BannerSlot 子结构（ColorBar + GiftIcon + InfoText + TierTag）</summary>
        private static void EnsureBannerSlot(Transform parent, int index)
        {
            var slotGo = EnsureChild(parent, $"BannerSlot_{index}");
            slotGo.SetActive(false);
            var slotRect = slotGo.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(0f, 48f); // 高度由VerticalLayout管理，宽度自适应

            var bgImg = EnsureComponent<Image>(slotGo);
            bgImg.color = new Color(0f, 0f, 0f, 0.75f);

            var hlayout = EnsureComponent<HorizontalLayoutGroup>(slotGo);
            hlayout.childAlignment    = TextAnchor.MiddleLeft;
            hlayout.spacing           = 6f;
            hlayout.padding           = new RectOffset(4, 8, 4, 4);
            hlayout.childControlWidth  = false;
            hlayout.childControlHeight = true;
            hlayout.childForceExpandWidth  = false;
            hlayout.childForceExpandHeight = true;

            // LeftBar（Tier颜色条）
            var colorBar = EnsureChild(slotGo.transform, "ColorBar");
            var cbRect = colorBar.GetComponent<RectTransform>();
            cbRect.sizeDelta = new Vector2(6f, 0f);
            var cbImg = EnsureComponent<Image>(colorBar);
            cbImg.color = new Color(1f, 0.85f, 0.3f);

            // GiftIcon（40×40）
            var giftIcon = EnsureChild(slotGo.transform, "GiftIcon");
            var giRect = giftIcon.GetComponent<RectTransform>();
            giRect.sizeDelta = new Vector2(40f, 0f);
            var giImg = EnsureComponent<Image>(giftIcon);
            giImg.color = new Color(1f, 1f, 1f, 0.9f);

            // InfoText（主文字）
            var infoText = EnsureChild(slotGo.transform, "InfoText");
            var itRect = infoText.GetComponent<RectTransform>();
            itRect.sizeDelta = new Vector2(280f, 0f);
            var itTmp = EnsureComponent<TextMeshProUGUI>(infoText);
            itTmp.text      = "玩家X 送出 礼物";
            itTmp.fontSize  = 16f;
            itTmp.color     = Color.white;
            itTmp.alignment = TextAlignmentOptions.Left;
            itTmp.overflowMode = TextOverflowModes.Ellipsis;
            ApplyChineseFont(itTmp);

            // TierTag（右侧等级标签）
            var tierTag = EnsureChild(slotGo.transform, "TierTag");
            var ttRect = tierTag.GetComponent<RectTransform>();
            ttRect.sizeDelta = new Vector2(42f, 0f);
            var ttTmp = EnsureComponent<TextMeshProUGUI>(tierTag);
            ttTmp.text      = "T1";
            ttTmp.fontSize  = 14f;
            ttTmp.color     = new Color(1f, 0.85f, 0.3f);
            ttTmp.alignment = TextAlignmentOptions.Center;
            ttTmp.fontStyle = FontStyles.Bold;
            ApplyChineseFont(ttTmp);
        }

        private static void ApplyChineseFont(TextMeshProUGUI tmp)
        {
            var font = Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
            if (font != null) tmp.font = font;
        }
    }
}
