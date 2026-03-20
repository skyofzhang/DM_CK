using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

namespace DrscfZ.UI
{
    /// <summary>
    /// 底部水平飘屏——事件从右向左滚动
    /// 格式：[头像] [玩家名] [事件描述]
    /// 使用对象池，最多同时 MAX_ROWS 条
    /// 挂载到 Canvas（always active）
    /// </summary>
    public class HorizontalMarqueeUI : MonoBehaviour
    {
        public static HorizontalMarqueeUI Instance { get; private set; }

        [Header("飘屏参数")]
        [SerializeField] private float _scrollSpeed  = 120f;   // px/s
        [SerializeField] private int   _maxRows       = 5;
        [SerializeField] private float _rowHeight     = 38f;   // 行间距
        [SerializeField] private float _avatarSize    = 40f;   // 头像尺寸

        [Header("引用")]
        [SerializeField] private RectTransform _zone;          // 飘屏区域容器

        // 对象池
        private List<MarqueeRow> _pool   = new List<MarqueeRow>();
        private List<MarqueeRow> _active = new List<MarqueeRow>();
        private int _nextRowSlot = 0;   // 新飘屏分配到第几行（0~MAX_ROWS-1 循环）

        private class MarqueeRow
        {
            public GameObject      root;
            public RectTransform   rect;
            public RawImage        avatar;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI eventText;
            public bool            inUse;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            BuildPool();
        }

        private void BuildPool()
        {
            var font       = Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
            var outlineMat = Resources.Load<Material>("Fonts/ChineseFont SDF - Outline");

            for (int i = 0; i < _maxRows * 2; i++)
            {
                var rowGO   = new GameObject($"MarqueeRow_{i}", typeof(RectTransform));
                rowGO.transform.SetParent(_zone, false);

                var rowRect       = rowGO.GetComponent<RectTransform>();
                rowRect.anchorMin = rowRect.anchorMax = new Vector2(0f, 0.5f);
                rowRect.pivot     = new Vector2(0f, 0.5f);
                rowRect.sizeDelta = new Vector2(700f, _rowHeight);

                // 水平布局
                var hlg                    = rowGO.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing                = 8f;
                hlg.childAlignment         = TextAnchor.MiddleLeft;
                hlg.childControlWidth      = false;
                hlg.childControlHeight     = false;
                hlg.childForceExpandWidth  = false;
                hlg.childForceExpandHeight = false;
                hlg.padding                = new RectOffset(10, 10, 4, 4);

                // 半透明背景
                var bg   = rowGO.AddComponent<Image>();
                bg.color = new Color(0f, 0f, 0f, 0.45f);

                // 头像
                var avatarGO   = new GameObject("Avatar", typeof(RectTransform), typeof(RawImage));
                avatarGO.transform.SetParent(rowGO.transform, false);
                var avatarRect = avatarGO.GetComponent<RectTransform>();
                avatarRect.sizeDelta = new Vector2(_avatarSize, _avatarSize);
                var avatarImg  = avatarGO.GetComponent<RawImage>();
                avatarImg.color = Color.clear; // 无头像时透明，不显示色块

                // 玩家名
                var nameGO   = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
                nameGO.transform.SetParent(rowGO.transform, false);
                var nameRect = nameGO.GetComponent<RectTransform>();
                nameRect.sizeDelta = new Vector2(80f, _rowHeight - 8f);
                var nameTmp  = nameGO.GetComponent<TextMeshProUGUI>();
                nameTmp.fontSize  = 22f;
                nameTmp.color     = new Color(1f, 0.95f, 0.6f);
                nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
                if (font != null)       nameTmp.font               = font;
                if (outlineMat != null) nameTmp.fontSharedMaterial  = outlineMat;

                // 事件描述
                var evGO   = new GameObject("Event", typeof(RectTransform), typeof(TextMeshProUGUI));
                evGO.transform.SetParent(rowGO.transform, false);
                var evRect = evGO.GetComponent<RectTransform>();
                evRect.sizeDelta = new Vector2(550f, _rowHeight - 8f);
                var evTmp  = evGO.GetComponent<TextMeshProUGUI>();
                evTmp.fontSize  = 20f;
                evTmp.color     = Color.white;
                evTmp.alignment = TextAlignmentOptions.MidlineLeft;
                if (font != null)       evTmp.font               = font;
                if (outlineMat != null) evTmp.fontSharedMaterial  = outlineMat;

                rowGO.SetActive(false);

                _pool.Add(new MarqueeRow
                {
                    root      = rowGO,
                    rect      = rowRect,
                    avatar    = avatarImg,
                    nameText  = nameTmp,
                    eventText = evTmp,
                    inUse     = false
                });
            }
        }

        private void Update()
        {
            float delta     = _scrollSpeed * Time.deltaTime;
            float zoneWidth = _zone != null ? _zone.rect.width : Screen.width;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var row = _active[i];
                var pos = row.rect.anchoredPosition;
                pos.x -= delta;
                row.rect.anchoredPosition = pos;

                // 超出左边缘则回收
                if (pos.x + row.rect.sizeDelta.x < 0f)
                {
                    row.root.SetActive(false);
                    row.inUse = false;
                    _active.RemoveAt(i);
                }
            }
        }

        /// <summary>添加一条飘屏消息</summary>
        public void AddMessage(string playerName, string avatarUrl, string eventText)
        {
            var row = GetFreeRow();
            if (row == null) return;

            row.nameText.text  = playerName;
            row.eventText.text = eventText;
            row.avatar.texture = null;
            row.avatar.color   = Color.clear; // 加载前透明，避免蓝色色块

            // 起始位置：从右侧屏幕外入场
            float zoneWidth = _zone != null ? _zone.rect.width : Screen.width;
            float yOffset   = (_nextRowSlot % _maxRows) * _rowHeight - (_maxRows * _rowHeight / 2f);
            _nextRowSlot++;

            row.rect.anchoredPosition = new Vector2(zoneWidth + 50f, yOffset);
            row.root.SetActive(true);
            row.inUse = true;
            _active.Add(row);

            // 异步加载头像
            if (!string.IsNullOrEmpty(avatarUrl))
                StartCoroutine(LoadAvatar(row, avatarUrl));
        }

        private MarqueeRow GetFreeRow()
        {
            foreach (var r in _pool)
                if (!r.inUse) return r;
            return null;
        }

        private IEnumerator LoadAvatar(MarqueeRow row, string url)
        {
            using var req = UnityWebRequestTexture.GetTexture(url);
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success && row.inUse)
            {
                row.avatar.texture = ((DownloadHandlerTexture)req.downloadHandler).texture;
                row.avatar.color   = Color.white;
            }
        }
    }
}
