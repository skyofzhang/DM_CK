using UnityEngine;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §34 Layer 2 组 A B8a 仙女棒 "+5%" 头顶飘字
    ///
    /// 订阅 SurvivalGameManager.OnGiftImpact（§34C E4），过滤 giftId == "fairy_wand"：
    ///   在发送者 Worker 头顶飘字 "+5%"（1.5s 上浮 + 淡出）。
    ///
    /// 注意：GiftImpactData.privateOnly = true（仙女棒）时，现有 GiftImpactUI 在顶部横幅中过滤掉。
    ///   但本脚本要为"发送者的矿工"显示视觉反馈，这里不过滤 privateOnly——只有当 giftId=="fairy_wand"
    ///   且能找到对应 Worker 时显示飘字。主播端可能没有对应 Worker（发送者是外部观众），此时静默跳过。
    ///
    /// 定位：WorkerManager.GetWorkerByPlayerId(data.playerId) → WorkerController → transform.position。
    ///
    /// 飘字实现：复用 DamageNumber.Show(worldPos, "+5%", 金黄色)。
    /// 挂载规则：挂任意常驻 GO（仅事件驱动，无 UI 节点）。
    /// </summary>
    public class FairyWandFloatingText : MonoBehaviour
    {
        private bool _subscribed;

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
            sgm.OnGiftImpact += HandleGiftImpact;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null) sgm.OnGiftImpact -= HandleGiftImpact;
            _subscribed = false;
        }

        private void HandleGiftImpact(GiftImpactData data)
        {
            if (data == null) return;
            if (data.giftId != "fairy_wand") return;

            var wm = WorkerManager.Instance;
            if (wm == null) return;
            var worker = wm.GetWorkerByPlayerId(data.playerId);
            if (worker == null) return;   // 发送者的矿工未实例化（可能尚未分配 Worker），静默跳过

            // 头顶飘字 "+5%"，金黄色
            Vector3 pos = worker.transform.position + Vector3.up * 1.8f;
            DamageNumber.Show(pos, "+5%", new Color(1f, 0.85f, 0.2f));
        }
    }
}
