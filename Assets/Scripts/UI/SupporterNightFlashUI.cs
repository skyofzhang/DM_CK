using System.Collections.Generic;
using UnityEngine;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §33.6.3 助威模式 —— 夜晚 cmd=6 随机矿工头顶闪光
    ///
    /// 订阅 SurvivalGameManager.OnSupporterAction。
    /// 仅当 data.cmd == 6 时处理：从 WorkerManager.Instance.ActiveWorkers 随机挑一名存活 Worker，
    /// 调用 WorkerVisual.TriggerAssignmentFlash() 做一次头顶闪光提示"助威传达到了"。
    ///
    /// 夜晚判定：DayNightCycleManager.Instance.IsNight；若 DNC 未就绪，cmd=6 本身是夜晚限定指令，
    ///   后端只在夜晚推送 → 直接触发（不做额外判定），对应策划案 §33.3 表。
    ///
    /// 纯逻辑组件，无自身 UI；挂到 GameUIPanel（常驻激活）即可。
    /// </summary>
    public class SupporterNightFlashUI : MonoBehaviour
    {
        private bool _subscribed;

        // ── 生命周期 ──────────────────────────────────────────────────────

        private void Start()    { TrySubscribe(); }
        private void OnEnable() { TrySubscribe(); }
        private void OnDisable(){ Unsubscribe(); }
        private void OnDestroy(){ Unsubscribe(); }

        private void Update()
        {
            if (!_subscribed) TrySubscribe();
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;
            sgm.OnSupporterAction += HandleSupporterAction;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null) sgm.OnSupporterAction -= HandleSupporterAction;
            _subscribed = false;
        }

        // ── 事件回调 ──────────────────────────────────────────────────────

        private void HandleSupporterAction(SupporterActionData data)
        {
            if (data == null) return;
            if (data.cmd != 6) return;  // 仅 cmd=6（攻击加成）触发闪光

            // 夜晚判定：若 DayNightCycleManager 可用，仅夜晚触发；否则信任后端推送时机（cmd=6 本就是夜晚限定）
            var dnc = DayNightCycleManager.Instance;
            if (dnc != null && !dnc.IsNight) return;

            var wm = WorkerManager.Instance;
            if (wm == null) return;

            // 从 ActiveWorkers 中筛选存活矿工
            var alive = new List<WorkerController>();
            foreach (var w in wm.ActiveWorkers)
                if (w != null && !w.IsDead) alive.Add(w);

            if (alive.Count == 0) return;

            var pick = alive[Random.Range(0, alive.Count)];
            var visual = pick.GetComponent<WorkerVisual>();
            if (visual != null) visual.TriggerAssignmentFlash();
        }
    }
}
