using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §34 Layer 2 组 B B4 助威者行为日志（audit-r5 补齐）
    ///
    /// 5 行滚动 log：最新行从顶部压入，最老一行淡出；浅紫底色区分助威者路径。
    /// 订阅事件：
    ///   OnSupporterJoined   → "[助威者] {playerName} 加入助威"
    ///   OnSupporterAction   → "[助威者] {playerName} 发送指令 {cmdLabel}"
    ///   OnSupporterPromoted → "[守护者] {newPlayerName} 从助威者晋升"
    ///   OnGiftSilentFail    → "[助威者] 礼物未生效（未解锁 D{unlockDay}）"（MVP 暂无服务端推送）
    ///
    /// 挂载规则（CLAUDE.md #7）：挂 GameUIPanel 子 GO（常驻激活），左侧或右侧边栏。
    /// 字体：Alibaba 优先 + ChineseFont SDF fallback。
    ///
    /// Inspector 必填：
    ///   _container — 5 行 log 的父容器（VerticalLayoutGroup；TopToBottom 排列）
    ///   _rowPrefab — 单行 Prefab（Image 背景 + TMP 文字）；若为 null 则运行时自动生成
    ///   _bgImage   — 整体面板背景 Image（浅紫底色）
    /// </summary>
    public class SupporterActionLogUI : MonoBehaviour
    {
        [Header("5 行 log 父容器（VerticalLayoutGroup 建议 topToBottom）")]
        [SerializeField] private RectTransform _container;

        [Header("整体浅紫底色背景")]
        [SerializeField] private Image _bgImage;

        [Header("单行 Prefab（可为 null，运行时兜底生成）")]
        [SerializeField] private GameObject _rowPrefab;

        private const int MAX_ROWS         = 5;
        private const float ROW_HOLD       = 6.0f;   // 每行显示时长
        private const float ROW_FADE_OUT   = 0.5f;

        // 浅紫底色 + 浅紫文字
        private static readonly Color BG_TINT   = new Color(0.91f, 0.84f, 0.96f, 0.72f); // #E8D5F5
        private static readonly Color TEXT_TINT = new Color(0.45f, 0.25f, 0.65f, 1f);

        private const string AlibabaFontPath = "Fonts/AlibabaPuHuiTi-3-85-Bold SDF";
        private const string ChineseFontPath = "Fonts/ChineseFont SDF";

        // 行实例队列（最早在 [0]，最新在末尾）
        private readonly List<RowItem> _rows = new List<RowItem>(MAX_ROWS);
        private bool _subscribed;

        private class RowItem
        {
            public GameObject go;
            public TMP_Text   label;
            public Coroutine  fadeCo;
        }

        // ── 生命周期 ──────────────────────────────────────────────────────

        private void Start()
        {
            if (_bgImage != null) _bgImage.color = BG_TINT;
            TrySubscribe();
        }

        private void OnEnable()  { TrySubscribe(); }
        private void OnDisable() { Unsubscribe(); }
        private void OnDestroy()
        {
            Unsubscribe();
            foreach (var r in _rows)
            {
                if (r.fadeCo != null) StopCoroutine(r.fadeCo);
                if (r.go != null) Destroy(r.go);
            }
            _rows.Clear();
        }

        private void Update()
        {
            if (!_subscribed) TrySubscribe();
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;
            sgm.OnSupporterJoined   += HandleJoined;
            sgm.OnSupporterAction   += HandleAction;
            sgm.OnSupporterPromoted += HandlePromoted;
            sgm.OnGiftSilentFail    += HandleGiftSilentFail;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnSupporterJoined   -= HandleJoined;
                sgm.OnSupporterAction   -= HandleAction;
                sgm.OnSupporterPromoted -= HandlePromoted;
                sgm.OnGiftSilentFail    -= HandleGiftSilentFail;
            }
            _subscribed = false;
        }

        // ── 事件回调 ──────────────────────────────────────────────────────

        private void HandleJoined(SupporterJoinedData data)
        {
            if (data == null) return;
            string name = string.IsNullOrEmpty(data.playerName) ? "观众" : data.playerName;
            PushRow($"[助威者] {name} 加入助威");
        }

        private void HandleAction(SupporterActionData data)
        {
            if (data == null) return;
            string name = string.IsNullOrEmpty(data.playerName) ? "观众" : data.playerName;
            string cmdLabel = data.cmd switch
            {
                1   => "采食物",
                2   => "挖煤",
                3   => "挖矿",
                4   => "生火",
                6   => "打怪",
                666 => "666 鼓舞",
                _   => $"指令{data.cmd}"
            };
            PushRow($"[助威者] {name} 发送 {cmdLabel}");
        }

        private void HandlePromoted(SupporterPromotedData data)
        {
            if (data == null) return;
            string name = string.IsNullOrEmpty(data.newPlayerName) ? "观众" : data.newPlayerName;
            PushRow($"[守护者] {name} 从助威者晋升");
        }

        private void HandleGiftSilentFail(GiftSilentFailData data)
        {
            if (data == null) return;
            int unlockDay = data.unlockDay > 0 ? data.unlockDay : 6;
            PushRow($"[助威者] 礼物未生效（D{unlockDay} 解锁）");
        }

        // ── 行管理 ──────────────────────────────────────────────────────

        private void PushRow(string text)
        {
            if (_container == null) return;

            // 超员时淡出最老一行
            if (_rows.Count >= MAX_ROWS)
            {
                var oldest = _rows[0];
                _rows.RemoveAt(0);
                if (oldest.fadeCo != null) StopCoroutine(oldest.fadeCo);
                StartCoroutine(FadeOutAndDestroy(oldest));
            }

            var item = CreateRow(text);
            if (item == null) return;
            _rows.Add(item);
            item.fadeCo = StartCoroutine(RowHoldAndFade(item));

            // 最新一行插到顶部（sibling index 0）
            item.go.transform.SetSiblingIndex(0);
        }

        private RowItem CreateRow(string text)
        {
            GameObject go;
            if (_rowPrefab != null)
            {
                go = Instantiate(_rowPrefab, _container);
            }
            else
            {
                // 运行时兜底：生成一个带 TMP 的 GO
                go = new GameObject("LogRow", typeof(RectTransform));
                go.transform.SetParent(_container, false);
                var le = go.AddComponent<LayoutElement>();
                le.preferredHeight = 28f;
                le.minHeight       = 24f;
            }
            var label = go.GetComponentInChildren<TMP_Text>();
            if (label == null)
            {
                // 子节点无 TMP：新建一个填充整行
                var labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(go.transform, false);
                label = labelGo.AddComponent<TextMeshProUGUI>();
                label.fontSize = 16f;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                var rt = labelGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(6f, 2f);
                rt.offsetMax = new Vector2(-6f, -2f);
            }

            var font = Resources.Load<TMP_FontAsset>(AlibabaFontPath);
            if (font == null) font = Resources.Load<TMP_FontAsset>(ChineseFontPath);
            if (font != null) label.font = font;

            label.text  = text;
            label.color = TEXT_TINT;

            return new RowItem { go = go, label = label };
        }

        private IEnumerator RowHoldAndFade(RowItem item)
        {
            yield return new WaitForSecondsRealtime(ROW_HOLD);
            if (item == null || item.go == null) yield break;

            float t = 0f;
            while (t < ROW_FADE_OUT)
            {
                t += Time.unscaledDeltaTime;
                float a = 1f - Mathf.Clamp01(t / ROW_FADE_OUT);
                if (item.label != null)
                {
                    var c = item.label.color; c.a = a; item.label.color = c;
                }
                yield return null;
            }

            _rows.Remove(item);
            if (item.go != null) Destroy(item.go);
        }

        private IEnumerator FadeOutAndDestroy(RowItem item)
        {
            if (item == null || item.go == null) yield break;
            float t = 0f;
            while (t < ROW_FADE_OUT)
            {
                t += Time.unscaledDeltaTime;
                float a = 1f - Mathf.Clamp01(t / ROW_FADE_OUT);
                if (item.label != null)
                {
                    var c = item.label.color; c.a = a; item.label.color = c;
                }
                yield return null;
            }
            if (item.go != null) Destroy(item.go);
        }
    }
}
