using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DrscfZ.UI
{
    /// <summary>
    /// 顶部飘字系统（重要事件：右→左滑动）
    /// Scene 预创建3行，初始inactive（AI准则#2）
    /// 挂载到 Canvas（always active）
    /// 引用：_rows[0/1/2] 在 Scene 中预创建的 TMP Text 行对象
    /// </summary>
    public class TopFloatingTextUI : MonoBehaviour
    {
        public static TopFloatingTextUI Instance { get; private set; }

        [System.Serializable]
        public class TextRow
        {
            public TMP_Text text;
            public RectTransform rect;
        }

        [Header("行引用（Inspector拖入，Scene预创建，初始inactive）")]
        [SerializeField] private TextRow[] _rows = new TextRow[3]; // 最多3条同时显示

        [Header("动画参数")]
        [SerializeField] private float _scrollDuration = 3f;
        [SerializeField] private float _startX        = 1200f;  // 从右侧进入
        [SerializeField] private float _endX          = -1200f; // 到左侧消失
        [SerializeField] private float _rowSpacing    = 60f;    // 行间距（Y轴）

        private readonly Queue<(string message, Color color)> _pendingQueue
            = new Queue<(string, Color)>();
        private bool[] _rowBusy = new bool[3];

        // ==================== 颜色常量 ====================

        /// <summary>系统/昼夜消息（白色）</summary>
        public static readonly Color ColorSystem = Color.white;

        /// <summary>危险/城门消息（红色）</summary>
        public static readonly Color ColorDanger = new Color(1f, 0.2f, 0.2f);

        /// <summary>礼物/好消息（金色）</summary>
        public static readonly Color ColorGold = new Color(1f, 0.85f, 0.1f);

        // ==================== 生命周期 ====================

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            EnsureFallbackRows();

            // 绑定字体，确保中文显示（AI准则：字体统一用 ChineseFont SDF）
            var font = Resources.Load<TMPro.TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF") ?? Resources.Load<TMPro.TMP_FontAsset>("Fonts/ChineseFont SDF");
            foreach (var row in _rows)
            {
                if (row?.text != null)
                {
                    if (font != null) row.text.font = font;
                    row.text.fontSize  = 36f;
                    row.text.alignment = TextAlignmentOptions.Left;
                    row.text.gameObject.SetActive(false); // 初始隐藏（AI准则#2：UI预创建+SetActive控制）
                }
            }
        }

        private void EnsureFallbackRows()
        {
            bool missing = _rows == null || _rows.Length < 3;
            if (!missing)
            {
                for (int i = 0; i < _rows.Length; i++)
                {
                    if (_rows[i] == null || _rows[i].text == null || _rows[i].rect == null)
                    {
                        missing = true;
                        break;
                    }
                }
            }
            if (!missing) return;

            if (transform.parent == null)
                transform.SetParent(RuntimeUIFactory.GetCanvasTransform(), false);

            var root = RuntimeUIFactory.EnsureRectTransform(transform);
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.anchoredPosition = new Vector2(0f, -80f);
            root.sizeDelta = new Vector2(0f, 180f);

            _rows = new TextRow[3];
            for (int i = 0; i < _rows.Length; i++)
            {
                var tmp = RuntimeUIFactory.CreateText(transform, $"Row{i + 1}", "", 36f, Color.white, TextAlignmentOptions.Left, new Vector2(1200f, 48f));
                var rect = tmp.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0f, 0.5f);
                tmp.gameObject.SetActive(false);
                _rows[i] = new TextRow { text = tmp, rect = rect };
            }
        }

        // ==================== 公共接口 ====================

        /// <summary>显示系统/昼夜消息（白色）</summary>
        public void ShowSystem(string msg) => Show(msg, ColorSystem);

        /// <summary>显示危险/城门消息（红色）</summary>
        public void ShowDanger(string msg) => Show(msg, ColorDanger);

        /// <summary>显示礼物/好消息（金色）</summary>
        public void ShowGold(string msg) => Show(msg, ColorGold);

        /// <summary>显示飘字（指定颜色）</summary>
        public void Show(string msg, Color color)
        {
            // 找第一个空闲行
            int freeRow = -1;
            for (int i = 0; i < _rowBusy.Length; i++)
            {
                if (!_rowBusy[i]) { freeRow = i; break; }
            }

            if (freeRow < 0)
            {
                // 全部忙碌，加入队列（最多缓存5条，丢弃过旧消息）
                if (_pendingQueue.Count < 5)
                    _pendingQueue.Enqueue((msg, color));
                return;
            }

            StartCoroutine(PlayRow(freeRow, msg, color));
        }

        // ==================== 私有方法 ====================

        private IEnumerator PlayRow(int rowIndex, string msg, Color color)
        {
            if (rowIndex >= _rows.Length || _rows[rowIndex] == null) yield break;

            var row = _rows[rowIndex];
            _rowBusy[rowIndex] = true;

            // 设置内容与颜色
            row.text.text  = msg;
            row.text.color = color;
            row.text.gameObject.SetActive(true);

            // Y 位置按行索引垂直排布（行0在最上方，依次向下）
            float targetY = -rowIndex * _rowSpacing;
            row.rect.anchoredPosition = new Vector2(_startX, targetY);

            // 从右到左滚动
            float elapsed = 0f;
            while (elapsed < _scrollDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _scrollDuration;
                float x = Mathf.Lerp(_startX, _endX, t);
                row.rect.anchoredPosition = new Vector2(x, targetY);
                yield return null;
            }

            row.text.gameObject.SetActive(false);
            _rowBusy[rowIndex] = false;

            // 处理挂起队列
            if (_pendingQueue.Count > 0)
            {
                var (nextMsg, nextColor) = _pendingQueue.Dequeue();
                StartCoroutine(PlayRow(rowIndex, nextMsg, nextColor));
            }
        }
    }
}
