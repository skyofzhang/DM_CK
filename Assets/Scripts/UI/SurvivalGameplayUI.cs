using System.Collections.Generic;
using UnityEngine;
using DrscfZ.Core;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    // 注：§17.16 Modal 互斥注册表已迁移到独立文件 Assets/Scripts/Survival/ModalRegistry.cs（P0-B1）。
    //     TryOpenModalA / CloseModalA / QueueModalB / CloseModalB 签名向下兼容，所有调用点不受影响。
    //     新增 Request(id, priority, onReplaced) / Release(id) / RequestB(id, onDismiss) / ReleaseB(id) API。

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
            // 🔴 audit-r25 GAP-A25-03：之前强制 _announcementPanel.SetActive(false)，但 ShowPanels 不恢复
            //   → v1.26 永续模式 Settlement → Running 自动切换后 AnnouncementUI 全屏公告永久断链
            //   修复：让 AnnouncementPanel 自管显隐（doc §17.0:1660-1661 一致），不在 HidePanels 强制隐藏
            //   旧行: if (_announcementPanel != null) _announcementPanel.SetActive(false);
        }
    }
}
