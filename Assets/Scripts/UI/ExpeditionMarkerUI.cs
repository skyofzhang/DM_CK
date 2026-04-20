using UnityEngine;
using System.Collections.Generic;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §38 探险系统——地图边缘探险图标 UI（MVP 简化版）
    ///
    /// 职责：
    ///   - 维护 playerId → marker GameObject 的映射
    ///   - AddMarker(playerId, returnsAt) 在地图边缘放置小图标，倒计时显示剩余秒数
    ///   - RemoveMarker(playerId) 清除图标
    ///   - ShowEvent(ExpeditionEventData) 临时显示外域事件气泡（占位：Log）
    ///
    /// 挂载（Rule #7）：挂在 Canvas（always-active）上的常驻组件；_markerContainer
    /// 为可选的 RectTransform 容器（场景预创建）。
    ///
    /// MVP 行为：
    ///   - 若 _markerContainer / _markerPrefab 任一为 null → 所有方法降级为 Debug.Log，不崩
    ///   - TODO 后续接入真实地图边缘 UI：
    ///       1) 场景预建 ExpeditionMarkerContainer（屏幕左/右侧垂直 HLG）
    ///       2) _markerPrefab 为带 TMP_Text 子物体的小卡片 Prefab
    ///       3) 读入矿工头像（WorkerController.PlayerName/_avatarUrl 可扩展）
    /// </summary>
    public class ExpeditionMarkerUI : MonoBehaviour
    {
        public static ExpeditionMarkerUI Instance { get; private set; }

        // ==================== Inspector 字段 ====================

        [Header("地图边缘图标容器（可选；MVP 未绑定时降级 Log）")]
        [SerializeField] private RectTransform _markerContainer;

        [Header("Marker Prefab（可选；MVP 未绑定时降级 Log）")]
        [SerializeField] private GameObject _markerPrefab;

        // ==================== 运行时状态 ====================

        /// <summary>playerId → marker GameObject</summary>
        private readonly Dictionary<string, GameObject> _markers = new Dictionary<string, GameObject>();

        /// <summary>playerId → returnsAt (Unix ms)，MVP：暂不接入 UI 倒计时</summary>
        private readonly Dictionary<string, long> _returnsAt = new Dictionary<string, long>();

        // ==================== 生命周期 ====================

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            _markers.Clear();
            _returnsAt.Clear();
        }

        // ==================== 公共接口 ====================

        /// <summary>
        /// 添加一个探险 marker。若 playerId 已存在则覆盖（刷新 returnsAt）。
        /// </summary>
        public void AddMarker(string playerId, long returnsAt)
        {
            if (string.IsNullOrEmpty(playerId)) return;

            _returnsAt[playerId] = returnsAt;

            // MVP 降级：未绑 UI 时只 Log 不崩
            if (_markerContainer == null || _markerPrefab == null)
            {
                Debug.Log($"[ExpeditionMarkerUI] AddMarker({playerId}, returnsAt={returnsAt}) — 容器/Prefab 未绑定，降级 Log（TODO 接入地图边缘 UI）");
                return;
            }

            // 若已有 marker，先清掉旧的
            if (_markers.TryGetValue(playerId, out var oldGo) && oldGo != null)
                Destroy(oldGo);

            var go = Instantiate(_markerPrefab, _markerContainer);
            go.name = $"Marker_{playerId}";
            _markers[playerId] = go;

            Debug.Log($"[ExpeditionMarkerUI] AddMarker: {playerId} returnsAt={returnsAt}");
        }

        /// <summary>清除指定 playerId 的 marker。若不存在则 noop。</summary>
        public void RemoveMarker(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return;
            _returnsAt.Remove(playerId);

            if (_markers.TryGetValue(playerId, out var go))
            {
                if (go != null) Destroy(go);
                _markers.Remove(playerId);
                Debug.Log($"[ExpeditionMarkerUI] RemoveMarker: {playerId}");
            }
            else
            {
                Debug.Log($"[ExpeditionMarkerUI] RemoveMarker: {playerId}（无 marker，可能未绑定 UI 或已清除）");
            }
        }

        /// <summary>
        /// 显示外域事件的临时气泡提示（MVP：Log）。
        /// 非 trader_caravan 的事件仅做视觉反馈；trader_caravan 由 TraderCaravanUI 接管。
        /// </summary>
        public void ShowEvent(ExpeditionEventData data)
        {
            if (data == null) return;
            Debug.Log($"[ExpeditionMarkerUI] ShowEvent: expeditionId={data.expeditionId} eventId={data.eventId} eventEndsAt={data.eventEndsAt}");
            // TODO 后续接入：在对应 marker 上方 3s 气泡显示事件中文名
            //   lost_cache="遗失宝藏" / wild_beasts="荒野猛兽" / ...
        }

        /// <summary>清除所有 marker（游戏结束/重置时调用）</summary>
        public void ClearAll()
        {
            foreach (var kvp in _markers)
                if (kvp.Value != null) Destroy(kvp.Value);
            _markers.Clear();
            _returnsAt.Clear();
            Debug.Log("[ExpeditionMarkerUI] ClearAll");
        }
    }
}
