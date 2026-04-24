using System;
using System.Collections.Generic;
using UnityEngine;

namespace DrscfZ.UI
{
    /// <summary>
    /// §17.16 Modal 互斥注册表（全局静态工具类）
    ///
    /// 职责：
    ///   - A 类 modal：阻塞主流程（结算 / 升级确认 / 攻防战 / 投票 / 轮盘 / 新手引导 / 重连对话）
    ///     同一时刻只能打开一个；占用中若新请求优先级更高则替换旧 modal。
    ///     两套 API 并存：
    ///       * TryOpenModalA(GameObject) / CloseModalA(GameObject)  —— 向下兼容（现有调用点）
    ///       * Request(string id, int priority) / Release(string id) —— P0-B1 新增 id+优先级版本
    ///   - B 类 modal：非阻塞排队（各种 toast / marquee / event_triggered 公告）
    ///     两套 API 并存：
    ///       * QueueModalB(GameObject) / CloseModalB(GameObject)  —— 向下兼容
    ///       * RequestB(string id, Action onDismiss) / ReleaseB(string id) —— id 版本（策划案 §17.16）
    ///
    /// 策划案 §17.16 常量分类（仅示例，不用于强制校验）：
    ///   A 类：onboarding_bubble_sequence / gate_upgrade_confirm / build_vote / roulette_result / settlement / reconnect_dialog
    ///   B 类：fairy_wand_marquee / supporter_marquee / event_triggered_toast / tension_high_banner / glory_moment / chapter_announcement
    /// </summary>
    public static class ModalRegistry
    {
        // ── A 类：GameObject 模式（向下兼容）─────────────────────────────────────
        private static GameObject _activeModalA;

        // ── A 类：id 模式（P0-B1 新增）──────────────────────────────────────────
        // 当前仅保留一个 active id + priority；新请求优先级更高时会替换。
        private static string   _activeIdA;
        private static int      _activeIdAPriority;
        private static Action   _activeIdAOnReplaced; // 被替换时通知旧请求方（可选）

        // ── B 类：GameObject 队列（向下兼容）────────────────────────────────────
        private static Queue<GameObject> _modalBQueue;

        // ── B 类：id + onDismiss 回调（P0-B1 新增）───────────────────────────────
        private class ModalBItem { public string id; public Action onDismiss; }
        private static readonly List<ModalBItem> _modalBList = new List<ModalBItem>();

        /// <summary>是否有 A 类 modal 当前激活（UI 可用于"阻止其它 A 类打开"的条件）。
        /// 同时检查 GameObject 版和 id 版，任一激活即 true。</summary>
        public static bool HasActiveModalA
        {
            get
            {
                if (_activeModalA != null && _activeModalA.activeSelf) return true;
                if (!string.IsNullOrEmpty(_activeIdA)) return true;
                return false;
            }
        }

        // ==================== A 类 GameObject 版（向下兼容） ====================

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

        // ==================== A 类 id+优先级版（P0-B1 新增） ====================

        /// <summary>
        /// A 类 modal 请求（id+优先级版本，策划案 §17.16）。
        /// - 当前无 A 类激活 → 直接占用，返回 true。
        /// - 当前 id 相同（重复请求） → 视为已持有，返回 true。
        /// - 当前已被其他 id 占用 且 新优先级 ≤ 当前优先级 → 返回 false（调用方应跳过打开）。
        /// - 当前已被其他 id 占用 但 新优先级 > 当前优先级 → 替换：调用旧请求方的 onReplaced（若有），返回 true。
        /// </summary>
        public static bool Request(string id, int priority, Action onReplaced = null)
        {
            if (string.IsNullOrEmpty(id)) return false;

            // 已占用且 id 相同 → 视为已持有
            if (_activeIdA == id)
            {
                _activeIdAPriority = priority;
                _activeIdAOnReplaced = onReplaced;
                return true;
            }

            // 空闲 → 直接占用
            if (string.IsNullOrEmpty(_activeIdA))
            {
                _activeIdA = id;
                _activeIdAPriority = priority;
                _activeIdAOnReplaced = onReplaced;
                return true;
            }

            // 已被其他 id 占用 → 看优先级
            if (priority > _activeIdAPriority)
            {
                // 抢占：通知旧请求方
                var oldReplaced = _activeIdAOnReplaced;
                _activeIdA = id;
                _activeIdAPriority = priority;
                _activeIdAOnReplaced = onReplaced;
                try { oldReplaced?.Invoke(); } catch (Exception ex) { Debug.LogError($"[ModalRegistry] onReplaced 抛异常: {ex}"); }
                return true;
            }

            return false;
        }

        /// <summary>释放 A 类 modal（id 版本）。仅当当前占用 id 与传入一致时清空；否则无副作用。</summary>
        public static void Release(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (_activeIdA == id)
            {
                _activeIdA = null;
                _activeIdAPriority = 0;
                _activeIdAOnReplaced = null;
            }
        }

        /// <summary>查询 A 类 modal 当前持有者 id（可能为空）。</summary>
        public static string CurrentModalAId => _activeIdA;

        // ==================== B 类 GameObject 版（向下兼容） ====================

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

        // ==================== B 类 id 版（P0-B1 新增） ====================

        /// <summary>
        /// B 类 modal 请求（id 版本，策划案 §17.16）。
        /// 按 FIFO 排队，依次显示；首个入队立即 onDismiss 前触发回调由调用方自行管理（UI 组件层决定 SetActive）。
        /// 本函数仅维护序列，不操作 GameObject。
        /// </summary>
        public static void RequestB(string id, Action onDismiss = null)
        {
            if (string.IsNullOrEmpty(id)) return;
            // 去重：同 id 只入一次（避免弹多次）
            for (int i = 0; i < _modalBList.Count; i++)
                if (_modalBList[i].id == id) return;
            _modalBList.Add(new ModalBItem { id = id, onDismiss = onDismiss });
        }

        /// <summary>释放 B 类 modal（id 版本）。移除队列中首个匹配项；触发 onDismiss。</summary>
        public static void ReleaseB(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            for (int i = 0; i < _modalBList.Count; i++)
            {
                if (_modalBList[i].id == id)
                {
                    var cb = _modalBList[i].onDismiss;
                    _modalBList.RemoveAt(i);
                    try { cb?.Invoke(); } catch (Exception ex) { Debug.LogError($"[ModalRegistry] onDismiss 抛异常: {ex}"); }
                    return;
                }
            }
        }

        /// <summary>查询 B 类是否包含某 id（正在排队或显示中）。</summary>
        public static bool ContainsB(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            for (int i = 0; i < _modalBList.Count; i++)
                if (_modalBList[i].id == id) return true;
            return false;
        }

        /// <summary>清空所有队列（回 Idle/结算跳过用）。</summary>
        public static void Clear()
        {
            _activeModalA = null;
            _activeIdA = null;
            _activeIdAPriority = 0;
            _activeIdAOnReplaced = null;
            if (_modalBQueue != null) _modalBQueue.Clear();
            _modalBList.Clear();
        }
    }
}
