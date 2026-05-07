using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DrscfZ.Core;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §39 商店系统 UI 主面板（客户端 MVP，Prefab 绑定由人工补齐）。
    ///
    /// 功能：
    ///   - A / B / 背包 三 Tab 切换（发 shop_list 拉目录；Inventory Tab 直接用本地 _lastInventory 渲染）
    ///   - PopulateList(ShopListData) 清空 content 后按 items 生成按钮
    ///   - UpdateInventory(ShopInventoryData) 更新本地缓存；当前在 Inventory Tab 会重绘
    ///   - 购买点击：
    ///       A 类 → shop_purchase { itemId }
    ///       B 类 < 1000 → shop_purchase { itemId }
    ///       B 类 ≥ 1000 → shop_purchase_prepare { itemId }（触发服务端双确认弹窗）
    ///   - 装备点击：发 shop_equip { slot, itemId }
    ///
    /// 挂载：Canvas（always-active） 子对象；初始 panel inactive；Prefab 绑定后由 BroadcasterPanel
    /// 的 Shop Tab 按钮触发 OpenPanel("A")。
    /// </summary>
    public class ShopUI : MonoBehaviour
    {
        public static ShopUI Instance { get; private set; }

        // audit-r12 GAP-B02：§17.16 互斥组 A 阻塞型 modal id（priority=50 商店主面板,可被结算/升级抢占）
        private const string MODAL_A_ID = "shop_panel";
        private const int    MODAL_PRIO = 50;

        // ==================== Inspector 字段（Prefab 绑定由人工） ====================

        [Header("面板根（初始 inactive）")]
        [SerializeField] private GameObject _panel;

        [Header("Tab 切换按钮")]
        [SerializeField] private Button _tabA;
        [SerializeField] private Button _tabB;
        [SerializeField] private Button _tabInventory;
        [SerializeField] private Button _btnClose;

        [Header("列表容器（按钮 Prefab 的父节点）")]
        [SerializeField] private RectTransform _contentRoot;

        [Header("商品按钮 Prefab（含子节点 TMP_Text * 2 + Button；不绑时 Log 占位）")]
        [SerializeField] private GameObject _itemButtonPrefab;

        [Header("标题/状态文本（可选）")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _statusText;

        // ==================== 运行时状态 ====================

        private string _currentTab = "A"; // 'A' | 'B' | 'I'（Inventory）
        private ShopInventoryData _lastInventory;
        private readonly List<GameObject> _spawnedItems = new List<GameObject>();
        private readonly Dictionary<string, ShopItem> _itemMetadata = new Dictionary<string, ShopItem>();
        private bool _sgmSubscribed;

        // ==================== 生命周期 ====================

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (_panel != null && _panel != gameObject) _panel.SetActive(false);
            TrySubscribeSurvivalGameManager();
        }

        private void Start()
        {
            if (_tabA != null)         _tabA.onClick.AddListener(() => SwitchTab("A"));
            if (_tabB != null)         _tabB.onClick.AddListener(() => SwitchTab("B"));
            if (_tabInventory != null) _tabInventory.onClick.AddListener(() => SwitchTab("I"));
            if (_btnClose != null)     _btnClose.onClick.AddListener(ClosePanel);
            if (_panel != null) _panel.SetActive(false);

            // Batch I 补齐：订阅失败/购买事件做最小 toast + 库存刷新
            TrySubscribeSurvivalGameManager();
        }

        private void OnDestroy()
        {
            UnsubscribeSurvivalGameManager();
            ModalRegistry.Release(MODAL_A_ID);  // audit-r12 GAP-B02 兜底释放
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!_sgmSubscribed) TrySubscribeSurvivalGameManager();
        }

        private void TrySubscribeSurvivalGameManager()
        {
            if (_sgmSubscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;
            sgm.OnShopPurchaseFailed  += HandleShopPurchaseFailedToast;
            sgm.OnShopPurchaseConfirm += HandleShopPurchaseConfirmRefresh;
            _sgmSubscribed = true;
        }

        private void UnsubscribeSurvivalGameManager()
        {
            if (!_sgmSubscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnShopPurchaseFailed  -= HandleShopPurchaseFailedToast;
                sgm.OnShopPurchaseConfirm -= HandleShopPurchaseConfirmRefresh;
            }
            _sgmSubscribed = false;
        }

        /// <summary>Batch I：购买失败 → toast（主面板可见时显示），复用 FailureToastLocale</summary>
        private void HandleShopPurchaseFailedToast(ShopPurchaseFailedData data)
        {
            if (_statusText == null || _panel == null || !_panel.activeSelf) return;
            string reasonText = DrscfZ.UI.FailureToastLocale.Get(data?.reason);
            _statusText.text = $"购买失败：{reasonText}";
        }

        /// <summary>Batch I：购买成功（本人或他人） → 若在 Inventory Tab 重绘；否则仅清理 status 文字</summary>
        private void HandleShopPurchaseConfirmRefresh(ShopPurchaseConfirmData data)
        {
            if (_panel == null || !_panel.activeSelf) return;
            if (_currentTab == "I")
                RenderInventory();
            if (_statusText != null) _statusText.text = "";
        }

        // ==================== 对外接口 ====================

        /// <summary>BroadcasterPanel Shop Tab 按钮调用：打开面板并切到指定 Tab（默认 A）</summary>
        public void OpenPanel(string category = "A")
        {
            if (_panel == null)
            {
                Debug.LogWarning("[ShopUI] OpenPanel：_panel 未绑定（MVP 占位 Log）");
                return;
            }
            // audit-r12 GAP-B02：§17.16 互斥组 A 注册
            if (!ModalRegistry.Request(MODAL_A_ID, MODAL_PRIO, OnModalReplaced))
            {
                Debug.LogWarning($"[ShopUI] ModalRegistry.Request 被拒（结算/升级在前），不打开商店");
                return;
            }
            _panel.SetActive(true);
            SwitchTab(string.IsNullOrEmpty(category) ? "A" : category);
        }

        public void ClosePanel()
        {
            if (_panel != null) _panel.SetActive(false);
            ModalRegistry.Release(MODAL_A_ID);  // audit-r12 GAP-B02
        }

        // audit-r12 GAP-B02：被结算/升级等高优先级 modal 抢占时关闭自身
        private void OnModalReplaced()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        /// <summary>SurvivalGameManager 收到 shop_list_data 后路由到此。清空列表并按 items 重建。</summary>
        public void PopulateList(ShopListData data)
        {
            if (data == null) return;
            CacheShopItems(data.items);

            // 更新标题 / 状态
            if (_titleText != null)
            {
                _titleText.text = data.category == "A" ? "A 类战术道具" :
                                  data.category == "B" ? "B 类身份装备" :
                                  $"商店 - {data.category}";
            }

            // 仅当 Tab 匹配时才重绘（避免 A/B 返回互覆盖）
            if (_currentTab != data.category) return;
            RenderItems(data.items);
        }

        /// <summary>SurvivalGameManager 收到 shop_inventory_data 后路由到此，缓存备用 + 若在 Inventory Tab 重绘。</summary>
        public void UpdateInventory(ShopInventoryData data)
        {
            _lastInventory = data;
            CacheShopItems(data?.ownedItems);
            if (_currentTab == "I")
                RenderInventory();
        }

        // ==================== Tab 切换 ====================

        private void SwitchTab(string tab)
        {
            _currentTab = tab;

            if (tab == "I")
            {
                // 背包：本地渲染即可（不发服务器请求）
                RenderInventory();
                return;
            }

            // A / B：清空列表后发请求，等 shop_list_data 返回再 PopulateList
            ClearItems();
            if (_statusText != null) _statusText.text = "加载中...";
            SendShopList(tab);
        }

        // ==================== 渲染 ====================

        private void RenderItems(ShopItem[] items)
        {
            ClearItems();

            if (_statusText != null)
            {
                _statusText.text = (items == null || items.Length == 0) ? "暂无商品" : "";
            }

            if (items == null || items.Length == 0) return;
            if (_contentRoot == null)
            {
                Debug.LogWarning($"[ShopUI] RenderItems：_contentRoot 未绑定，跳过渲染（items={items.Length}）");
                return;
            }

            HashSet<string> ownedSet = null;
            if (_lastInventory != null && _lastInventory.owned != null)
                ownedSet = new HashSet<string>(_lastInventory.owned);

            foreach (var item in items)
            {
                if (item == null) continue;
                SpawnItemButton(item, ownedSet);
            }
        }

        private void RenderInventory()
        {
            ClearItems();

            if (_titleText != null) _titleText.text = "我的背包";

            if (_lastInventory == null || _lastInventory.owned == null || _lastInventory.owned.Length == 0)
            {
                if (_statusText != null) _statusText.text = "背包为空（购买 B 类商品后显示）";
                return;
            }

            if (_statusText != null) _statusText.text = "";
            if (_contentRoot == null)
            {
                Debug.LogWarning($"[ShopUI] RenderInventory：_contentRoot 未绑定，跳过渲染（owned={_lastInventory.owned.Length}）");
                return;
            }

            if (_lastInventory.ownedItems != null && _lastInventory.ownedItems.Length > 0)
            {
                foreach (var item in _lastInventory.ownedItems)
                {
                    if (item == null || string.IsNullOrEmpty(item.itemId)) continue;
                    SpawnInventoryButton(item);
                }
                return;
            }

            foreach (var ownedId in _lastInventory.owned)
            {
                if (string.IsNullOrEmpty(ownedId)) continue;
                _itemMetadata.TryGetValue(ownedId, out var cached);
                var pseudo = cached ?? new ShopItem
                {
                    itemId   = ownedId,
                    name     = SurvivalGameManager.GetShopItemDisplayName(ownedId),
                    price    = 0,
                    slot     = InferSlotFromItemId(ownedId),
                    category = "B",
                    effect   = "点击装备 / 卸下",
                };
                SpawnInventoryButton(pseudo);
            }
        }

        private void CacheShopItems(ShopItem[] items)
        {
            if (items == null) return;
            foreach (var item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.itemId)) continue;
                _itemMetadata[item.itemId] = item;
            }
        }

        private void SpawnItemButton(ShopItem item, HashSet<string> ownedSet)
        {
            if (_itemButtonPrefab == null || _contentRoot == null)
            {
                Debug.Log($"[ShopUI] Spawn 占位：itemId={item.itemId} name={item.name} price={item.price} category={item.category} slot={item.slot} effect={item.effect}");
                return;
            }

            var go = Instantiate(_itemButtonPrefab, _contentRoot);
            go.SetActive(true);
            _spawnedItems.Add(go);

            // 尝试填充文字（Prefab 至少 1~3 个 TMP_Text）
            var texts = go.GetComponentsInChildren<TMP_Text>(true);
            bool owned = ownedSet != null && !string.IsNullOrEmpty(item.itemId) && ownedSet.Contains(item.itemId);
            string lockReason = GetItemLockReason(item);
            bool locked = !string.IsNullOrEmpty(lockReason);
            string nameLine  = owned ? $"{item.name}（已购）" : item.name;
            string priceLine = item.price > 0 ? $"{item.price}" : "免费";
            string effectLine = locked
                ? (string.IsNullOrEmpty(item.effect) ? lockReason : $"{item.effect} / {lockReason}")
                : (string.IsNullOrEmpty(item.effect) ? "" : item.effect);

            if (texts.Length >= 3)
            {
                texts[0].text = nameLine;
                texts[1].text = priceLine;
                texts[2].text = effectLine;
            }
            else if (texts.Length == 2)
            {
                texts[0].text = $"{nameLine}  {priceLine}";
                texts[1].text = effectLine;
            }
            else if (texts.Length == 1)
            {
                texts[0].text = $"{nameLine}  {priceLine}  {effectLine}";
            }

            // 绑定按钮点击（已购的 B 类禁点，避免重复发 shop_purchase）
            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                if ((owned && item.category == "B") || locked)
                {
                    btn.interactable = false;
                }
                else
                {
                    btn.interactable = true;
                    var snapshot = item; // closure 捕获
                    btn.onClick.AddListener(() => OnItemClicked(snapshot));
                }
            }
        }

        private void SpawnInventoryButton(ShopItem item)
        {
            if (_itemButtonPrefab == null || _contentRoot == null)
            {
                Debug.Log($"[ShopUI] InventorySpawn 占位：itemId={item.itemId} slot={item.slot}");
                return;
            }

            var go = Instantiate(_itemButtonPrefab, _contentRoot);
            go.SetActive(true);
            _spawnedItems.Add(go);

            var texts = go.GetComponentsInChildren<TMP_Text>(true);
            // 已装备标记
            string equippedMark = IsCurrentlyEquipped(item.itemId, item.slot) ? "[已装备] " : "";
            string nameLine = $"{equippedMark}{item.name}";
            string slotLine = $"槽位：{SlotDisplay(item.slot)}";

            if (texts.Length >= 3)
            {
                texts[0].text = nameLine;
                texts[1].text = slotLine;
                texts[2].text = item.effect ?? "";
            }
            else if (texts.Length == 2)
            {
                texts[0].text = nameLine;
                texts[1].text = slotLine;
            }
            else if (texts.Length == 1)
            {
                texts[0].text = $"{nameLine}  {slotLine}";
            }

            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                var snapshot = item;
                btn.onClick.AddListener(() =>
                {
                    // 已装备 → 发送 null 卸下；否则装备
                    bool equipped = IsCurrentlyEquipped(snapshot.itemId, snapshot.slot);
                    SendShopEquip(snapshot.slot, equipped ? null : snapshot.itemId);
                });
            }
        }

        private void ClearItems()
        {
            for (int i = _spawnedItems.Count - 1; i >= 0; i--)
            {
                var go = _spawnedItems[i];
                if (go != null) Destroy(go);
            }
            _spawnedItems.Clear();
        }

        // ==================== 点击处理 ====================

        private void OnItemClicked(ShopItem item)
        {
            if (item == null || string.IsNullOrEmpty(item.itemId)) return;

            // A 类 → 直接 shop_purchase
            // B 类 < 1000 → 直接 shop_purchase
            // B 类 ≥ 1000 → shop_purchase_prepare
            if (item.category == "B" && item.price >= 1000)
                SendShopPurchasePrepare(item.itemId);
            else
                SendShopPurchase(item.itemId, null);
        }

        // ==================== 网络消息发送 ====================

        private static void SendShopList(string category)
        {
            var net = NetworkManager.Instance;
            if (net == null || !net.IsConnected) return;
            long ts = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string json = $"{{\"type\":\"shop_list\",\"data\":{{\"category\":\"{EscapeJson(category)}\"}},\"timestamp\":{ts}}}";
            net.SendJson(json);
            Debug.Log($"[ShopUI] 发送 shop_list category={category}");
        }

        private static void SendShopPurchasePrepare(string itemId)
        {
            var net = NetworkManager.Instance;
            if (net == null || !net.IsConnected) return;
            long ts = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string json = $"{{\"type\":\"shop_purchase_prepare\",\"data\":{{\"itemId\":\"{EscapeJson(itemId)}\"}},\"timestamp\":{ts}}}";
            net.SendJson(json);
            Debug.Log($"[ShopUI] 发送 shop_purchase_prepare itemId={itemId}");
        }

        /// <summary>发送 shop_purchase；pendingId 为 null 时走无凭证路径（A 类 / 弹幕 / B 类 <1000）</summary>
        public static void SendShopPurchase(string itemId, string pendingId)
        {
            var net = NetworkManager.Instance;
            if (net == null || !net.IsConnected) return;
            long ts = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string dataBody = string.IsNullOrEmpty(pendingId)
                ? $"{{\"itemId\":\"{EscapeJson(itemId)}\"}}"
                : $"{{\"itemId\":\"{EscapeJson(itemId)}\",\"pendingId\":\"{EscapeJson(pendingId)}\"}}";
            string json = $"{{\"type\":\"shop_purchase\",\"data\":{dataBody},\"timestamp\":{ts}}}";
            net.SendJson(json);
            Debug.Log($"[ShopUI] 发送 shop_purchase itemId={itemId} pendingId={pendingId ?? "<null>"}");
        }

        private static void SendShopEquip(string slot, string itemId)
        {
            var net = NetworkManager.Instance;
            if (net == null || !net.IsConnected) return;
            long ts = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            // itemId=null 时走 JSON null，表示卸下
            string itemIdField = string.IsNullOrEmpty(itemId) ? "null" : $"\"{EscapeJson(itemId)}\"";
            string json = $"{{\"type\":\"shop_equip\",\"data\":{{\"slot\":\"{EscapeJson(slot)}\",\"itemId\":{itemIdField}}},\"timestamp\":{ts}}}";
            net.SendJson(json);
            Debug.Log($"[ShopUI] 发送 shop_equip slot={slot} itemId={itemId ?? "<null>"}");
        }

        // ==================== 工具 ====================

        private bool IsCurrentlyEquipped(string itemId, string slot)
        {
            var eq = SurvivalGameManager.Instance?.MyEquipped;
            if (eq == null || string.IsNullOrEmpty(slot) || string.IsNullOrEmpty(itemId)) return false;
            switch (slot)
            {
                case "title":    return eq.title    == itemId;
                case "frame":    return eq.frame    == itemId;
                case "entrance": return eq.entrance == itemId;
                case "barrage":  return eq.barrage  == itemId;
                default:         return false;
            }
        }

        /// <summary>根据 itemId 前缀推断 slot（背包数据只带 itemId，无 slot）</summary>
        private static string InferSlotFromItemId(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return "";
            if (itemId.StartsWith("title_"))    return "title";
            if (itemId.StartsWith("frame_"))    return "frame";
            if (itemId.StartsWith("entrance_")) return "entrance";
            if (itemId.StartsWith("barrage_"))  return "barrage";
            return "";
        }

        private static string SlotDisplay(string slot)
        {
            switch (slot)
            {
                case "title":    return "称号";
                case "frame":    return "头像框";
                case "entrance": return "入场特效";
                case "barrage":  return "弹幕装饰";
                default:         return "—";
            }
        }

        private string GetItemLockReason(ShopItem item)
        {
            if (item == null || item.category != "B") return "";

            int seasonDay = SurvivalGameManager.Instance?.CurrentSeasonState?.seasonDay ?? 1;
            if (item.minSeasonDay > 0 && seasonDay < item.minSeasonDay)
                return $"D{item.minSeasonDay} 解锁";

            long lifetime = _lastInventory != null ? _lastInventory.lifetimeContrib : 0L;
            if (item.minLifetimeContrib > 0 && lifetime < item.minLifetimeContrib)
                return $"需累计 {item.minLifetimeContrib} 贡献";

            return "";
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
