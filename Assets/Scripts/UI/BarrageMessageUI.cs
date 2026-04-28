using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// 弹幕消息面板控制器
    /// 订阅 SurvivalGameManager.OnPlayerActivityMessage，动态添加消息行
    /// 挂载到 Canvas（always active）
    /// 引用：barrageContent（Canvas/GameUIPanel/BarragePanel/ScrollView/Viewport/BarrageContent）
    ///        scrollRect（Canvas/GameUIPanel/BarragePanel/ScrollView）
    /// </summary>
    public class BarrageMessageUI : MonoBehaviour
    {
        // 🔴 audit-r33 GAP-D26-06 spotlight UI 完整版（80% → 100%）：BarrageMessageUI ★ 前缀（同 SurvivalLiveRankingUI 模式）
        public static BarrageMessageUI Instance { get; private set; }

        [Header("弹幕面板引用（Inspector拖入）")]
        public RectTransform barrageContent;   // BarrageContent（VerticalLayoutGroup）
        public ScrollRect    scrollRect;       // 用于自动滚到底部

        [Header("消息样式")]
        [SerializeField] private int   _maxMessages   = 30;   // 最多保留条数
        [SerializeField] private float _messageFontSz = 30f;  // 字体大小
        [SerializeField] private float _rowHeight     = 44f;  // 每行高度

        // 🔴 audit-r33 GAP-D26-06 spotlight UI 完整版（80% → 100%）：弹幕含 spotlight 玩家名时加 ★ 前缀
        //   §39.2 A4 设计 — 购买聚光灯效果后，自己名字下一条弹幕尾加 ★ 图标（同 SurvivalLiveRankingUI 高亮模式）
        private string _spotlightPlayerName = null;
        private float  _spotlightExpireTime = 0f;

        // 消息颜色配置
        private static readonly Color COLOR_JOIN     = new Color(0.40f, 0.85f, 0.40f);  // 加入 绿
        private static readonly Color COLOR_WORK     = new Color(0.70f, 0.90f, 1.00f);  // 工作 冰蓝
        private static readonly Color COLOR_GIFT     = new Color(1.00f, 0.80f, 0.20f);  // 礼物 金
        private static readonly Color COLOR_SYSTEM   = new Color(0.80f, 0.80f, 0.80f);  // 系统 灰

        private readonly Queue<GameObject> _msgRows = new Queue<GameObject>();
        private bool _subscribed = false;
        private Coroutine _scrollCoroutine;

        private void Awake()
        {
            // 🔴 audit-r33：Instance 单例（spotlight UI 完整版需 SurvivalGameManager 调用 TriggerSpotlight）
            if (Instance == null) Instance = this;
        }

        private void Start()
        {
            // 清理编辑器预留的占位MsgRow（保持干净起点）
            if (barrageContent != null)
            {
                for (int i = barrageContent.childCount - 1; i >= 0; i--)
                    Destroy(barrageContent.GetChild(i).gameObject);
            }

            TrySubscribe();
        }

        private void OnEnable()  { TrySubscribe(); }
        private void OnDisable() { Unsubscribe(); }
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>🔴 audit-r33 GAP-D26-06 spotlight UI 完整版：§39.2 A4 聚光灯触发
        /// 由 SurvivalGameManager.HandleShopEffectTriggered case "spotlight" 调用，spotlight 期间含 playerName 的弹幕加 ★ 前缀</summary>
        public void TriggerSpotlight(string targetPlayerName, float durationSec)
        {
            if (string.IsNullOrEmpty(targetPlayerName) || durationSec <= 0f) return;
            _spotlightPlayerName = targetPlayerName;
            _spotlightExpireTime = Time.time + durationSec;
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;

            sgm.OnPlayerActivityMessage += AddMessage;
            sgm.OnPlayerJoined          += data => AddMessage(GetJoinMessage(data.playerName), COLOR_JOIN);
            sgm.OnGiftReceived          += gift  => AddMessage(GetGiftMessage(gift.playerName, gift.giftName), COLOR_GIFT);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnPlayerActivityMessage -= AddMessage;
            }
            _subscribed = false;
        }

        // ==================== 公共接口 ====================

        /// <summary>添加一条弹幕消息（自动分配颜色）</summary>
        public void AddMessage(string text)
        {
            // 根据关键字自动判断颜色
            Color color = COLOR_SYSTEM;
            if (text.Contains("加入")) color = COLOR_JOIN;
            else if (text.Contains("→"))  color = COLOR_WORK;   // 工作指令 "玩家名 → 采集食物"
            else if (text.Contains("送出") || text.Contains("礼物")) color = COLOR_GIFT;

            AddMessage(text, color);
        }

        /// <summary>添加一条弹幕消息（指定颜色）</summary>
        public void AddMessage(string text, Color color)
        {
            if (barrageContent == null) return;

            // 🔴 audit-r33 GAP-D26-06 spotlight UI 完整版：弹幕含 spotlight 玩家名时加 ★ 前缀（金色，富文本）
            //   §39.2 A4 设计 — 购买聚光灯效果后，自己名字下一条弹幕尾加 ★ 图标
            string finalText = text;
            bool useRichText = false;
            if (!string.IsNullOrEmpty(_spotlightPlayerName)
                && Time.time < _spotlightExpireTime
                && !string.IsNullOrEmpty(text)
                && text.Contains(_spotlightPlayerName))
            {
                finalText = $"<color=#FFD700>★</color> {text}";
                useRichText = true;
            }

            // 创建消息行
            var rowGO = new GameObject("MsgRow");
            rowGO.transform.SetParent(barrageContent, false);

            var rt = rowGO.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, _rowHeight);

            var tmp = rowGO.AddComponent<TextMeshProUGUI>();
            var font = Resources.Load<TMPro.TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF") ?? Resources.Load<TMPro.TMP_FontAsset>("Fonts/ChineseFont SDF");
            if (font != null) tmp.font = font;
            tmp.text      = finalText;
            tmp.richText  = useRichText;
            tmp.fontSize  = _messageFontSz;
            tmp.color     = color;
            tmp.alignment = TextAlignmentOptions.Left;

            // 布局元素（VerticalLayoutGroup使用）
            var le = rowGO.AddComponent<LayoutElement>();
            le.preferredHeight = _rowHeight;
            le.flexibleWidth   = 1f;

            _msgRows.Enqueue(rowGO);

            // 超出上限，移除最旧的
            while (_msgRows.Count > _maxMessages)
            {
                var old = _msgRows.Dequeue();
                if (old != null) Destroy(old);
            }

            // 下一帧滚到底部（等布局刷新）
            if (_scrollCoroutine != null) StopCoroutine(_scrollCoroutine);
            _scrollCoroutine = StartCoroutine(ScrollToBottom());
        }

        // ==================== 私有方法 ====================

        private IEnumerator ScrollToBottom()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame(); // 等两帧确保布局刷新完
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 0f;
        }

        // ==================== 中文描述映射 ====================

        /// <summary>根据 commandId 返回中文弹幕描述</summary>
        private static string GetCommandDescription(int cmd, string playerName)
        {
            return cmd switch
            {
                1 => $"{playerName} 在渔场打鱼 （渔）",
                2 => $"{playerName} 去煤矿挖煤 （矿）",
                3 => $"{playerName} 去矿山采矿 （石）",
                4 => $"{playerName} 生火取暖 （火）",
                6 => $"{playerName} 拿起武器战斗！（战）",
                _ => $"{playerName} 执行了命令 {cmd}"
            };
        }

        /// <summary>玩家加入弹幕消息</summary>
        private static string GetJoinMessage(string playerName) => $"{playerName} 加入了部落 营地";

        /// <summary>礼物弹幕消息</summary>
        private static string GetGiftMessage(string playerName, string giftName) => $"{playerName} 送出了 {giftName}！礼物";
    }
}
