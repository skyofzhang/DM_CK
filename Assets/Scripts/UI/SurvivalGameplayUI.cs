using System.Collections.Generic;
using UnityEngine;
using DrscfZ.Core;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §17.16 Modal 互斥注册表（全局静态工具类）
    ///
    /// 职责：
    ///   - A 类 modal：阻塞主流程（结算 / 升级确认 / 攻防战 / 投票 / 轮盘）
    ///     同一时刻只能打开一个；TryOpenModalA() 返回 false 表示已被占用
    ///   - B 类 modal：非阻塞排队（各种 toast）
    ///     QueueModalB() 依次显示，CloseModalB 出队
    ///
    /// 不改造已有 UI 内部逻辑，只在打开/关闭时加一层调度。
    /// </summary>
    public static class ModalRegistry
    {
        private static GameObject _activeModalA;            // A 类：阻塞主流程
        private static Queue<GameObject> _modalBQueue;      // B 类：非阻塞排队

        /// <summary>是否有 A 类 modal 当前激活（UI 可用于"阻止其它 A 类打开"的条件）。</summary>
        public static bool HasActiveModalA =>
            _activeModalA != null && _activeModalA.activeSelf;

        /// <summary>尝试打开 A 类 modal。占用中返回 false，调用方可据此弹提示或排队。</summary>
        public static bool TryOpenModalA(GameObject modal)
        {
            if (modal == null) return false;
            if (_activeModalA != null && _activeModalA != modal && _activeModalA.activeSelf) return false;
            _activeModalA = modal;
            modal.SetActive(true);
            return true;
        }

        /// <summary>关闭指定 A 类 modal；仅当正在占用的就是它时才清空 slot。</summary>
        public static void CloseModalA(GameObject modal)
        {
            if (modal == null) return;
            if (_activeModalA == modal) _activeModalA = null;
            modal.SetActive(false);
        }

        /// <summary>将 B 类 modal 加入队列；若队列空会立即触发显示。</summary>
        public static void QueueModalB(GameObject modal)
        {
            if (modal == null) return;
            if (_modalBQueue == null) _modalBQueue = new Queue<GameObject>();
            _modalBQueue.Enqueue(modal);
            TryShowNextModalB();
        }

        private static void TryShowNextModalB()
        {
            if (_modalBQueue == null || _modalBQueue.Count == 0) return;
            var next = _modalBQueue.Peek();
            if (next == null || !next) { _modalBQueue.Dequeue(); TryShowNextModalB(); return; }
            next.SetActive(true);
        }

        /// <summary>关闭指定 B 类 modal；若它当前是队头则出队并显示下一个。</summary>
        public static void CloseModalB(GameObject modal)
        {
            if (modal == null) return;
            if (_modalBQueue != null && _modalBQueue.Count > 0 && _modalBQueue.Peek() == modal)
                _modalBQueue.Dequeue();
            modal.SetActive(false);
            TryShowNextModalB();
        }

        /// <summary>清空所有队列（回 Idle/结算跳过用）。</summary>
        public static void Clear()
        {
            _activeModalA = null;
            if (_modalBQueue != null) _modalBQueue.Clear();
        }
    }

    /// <summary>
    /// 生存游戏战斗UI总控制器。
    /// 挂载在 Canvas（始终激活，Rule #7）。
    /// 根据游戏状态控制 GameUIPanel（TopBar/弹幕等）和 BottomBar 的显隐：
    ///   - State.Running   → GameUIPanel + BottomBar 显示
    ///   - 其他状态/断线  → GameUIPanel + BottomBar 隐藏
    /// </summary>
    public class SurvivalGameplayUI : MonoBehaviour
    {
        [SerializeField] private GameObject _gameUIPanel;   // Canvas/GameUIPanel
        [SerializeField] private GameObject _bottomBar;     // Canvas/BottomBar
        [SerializeField] private GameObject _announcementPanel; // Canvas/AnnouncementPanel

        private void Start()
        {
            // 订阅事件
            var net = NetworkManager.Instance;
            if (net != null)
                net.OnDisconnected += OnDisconnected;

            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
                sgm.OnStateChanged += OnStateChanged;

            // 初始化隐藏（防止场景以 Running 状态启动时漏显）
            RefreshVisibility();
        }

        private void OnDestroy()
        {
            var net = NetworkManager.Instance;
            if (net != null)
                net.OnDisconnected -= OnDisconnected;

            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
                sgm.OnStateChanged -= OnStateChanged;
        }

        // ==================== 事件回调 ====================

        private void OnDisconnected(string _) => HidePanels();

        private void OnStateChanged(SurvivalGameManager.SurvivalState state) => RefreshVisibility();

        // ==================== 显隐逻辑 ====================

        private void RefreshVisibility()
        {
            var sgm = SurvivalGameManager.Instance;
            bool isRunning = sgm != null && sgm.State == SurvivalGameManager.SurvivalState.Running;

            if (isRunning)
                ShowPanels();
            else
                HidePanels();
        }

        private void ShowPanels()
        {
            if (_gameUIPanel != null) _gameUIPanel.SetActive(true);
            if (_bottomBar != null)   _bottomBar.SetActive(true);
            // AnnouncementPanel 保持其自身逻辑（不强制显示）
        }

        private void HidePanels()
        {
            if (_gameUIPanel != null) _gameUIPanel.SetActive(false);
            if (_bottomBar != null)   _bottomBar.SetActive(false);
            if (_announcementPanel != null) _announcementPanel.SetActive(false);
        }
    }
}
