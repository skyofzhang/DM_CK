using UnityEngine;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §34 Layer 2 组 A B5 矿石消耗可视化 —— 城门自动修复飘字
    ///
    /// 服务端每 2s 消耗 1 矿石修复 5~10 HP（按城门等级），但观众看不到这个隐形消耗。
    /// 本脚本检测 resource_update 中：
    ///   gateHp 增加 且 ore 减少 → 判定为"矿石修复城门"，在城门位置上方飘字 "矿石修复城门 +{N}HP"。
    ///
    /// N 按 gateLevel 映射（策划案 §34.3 B5 / §10.3）：
    ///   gateLevel = 1        → 5
    ///   gateLevel = 2..4     → 7
    ///   gateLevel >= 5       → 10
    ///
    /// 飘字实现：复用 DamageNumber.Show(worldPos, text, color)（1.2s 上浮 + 淡出）。
    /// 城门位置：CityGateSystem.Instance.transform.position + Vector3.up * 2（+DamageNumber 自己再 +1.5 → 总 +3.5）。
    ///
    /// 挂载规则：挂 Canvas 或场景根（只要能跑 Update 即可，无需显式 UI 节点）。
    /// 无 Inspector 字段；不依赖任何预建 GO。
    /// </summary>
    public class OreRepairFloatingText : MonoBehaviour
    {
        // 最近一次 resource_update 的 gateHp / ore 快照
        private int  _lastGateHp = -1;
        private int  _lastOre    = -1;
        private bool _initialized;
        private bool _subscribed;

        // ── 生命周期 ──────────────────────────────────────────────────────

        private void Start()        { TrySubscribe(); }
        private void OnEnable()     { TrySubscribe(); }
        private void OnDisable()    { Unsubscribe(); }
        private void OnDestroy()    { Unsubscribe(); }

        private void Update()
        {
            if (!_subscribed) TrySubscribe();
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;
            sgm.OnResourceUpdate += HandleResourceUpdate;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null) sgm.OnResourceUpdate -= HandleResourceUpdate;
            _subscribed = false;
        }

        // ── 事件回调 ──────────────────────────────────────────────────────

        private void HandleResourceUpdate(ResourceUpdateData data)
        {
            if (data == null) return;

            int gateHp = data.gateHp;
            int ore    = data.ore;

            if (!_initialized)
            {
                _lastGateHp   = gateHp;
                _lastOre      = ore;
                _initialized  = true;
                return;
            }

            int gateHpDelta = gateHp - _lastGateHp;
            int oreDelta    = ore    - _lastOre;

            // 矿石修复条件：gateHp 增加 + ore 减少
            // 过滤：礼物（T3/T4/T5/T6）修复城门时 ore 不会减少；人工修城门 (commandId=5) 已从 v2.0 移除；
            //       所以 "gateHp 增加 + ore 减少" 只有可能是服务端 _decayResources 的矿石修复。
            if (gateHpDelta > 0 && oreDelta < 0)
            {
                int hpPerTick = MapGateLevelToHp(data.gateLevel);
                // 实际修复量可能 < hpPerTick（上限截断），取实际 delta 更准确；但优先贴近"每段常量"提示
                int shown = Mathf.Max(1, Mathf.Min(gateHpDelta, hpPerTick));
                ShowFloatingText($"矿石修复城门 +{shown}HP");
            }

            _lastGateHp = gateHp;
            _lastOre    = ore;
        }

        // ── 内部辅助 ──────────────────────────────────────────────────────

        private static int MapGateLevelToHp(int gateLevel)
        {
            if (gateLevel <= 1) return 5;
            if (gateLevel <= 4) return 7;
            return 10;
        }

        private static void ShowFloatingText(string text)
        {
            var gate = CityGateSystem.Instance;
            Vector3 pos;
            if (gate != null)
            {
                // CityGate 节点 + 上方偏移 2（DamageNumber 内部再 +1.5 = 总约 +3.5）
                pos = gate.transform.position + Vector3.up * 2f;
            }
            else
            {
                // 兜底：世界原点上方
                pos = new Vector3(0f, 3f, 0f);
            }

            // 复用 DamageNumber（1.2s 上浮 + 淡出 + 世界空间 TMP）
            // 颜色：暖黄（与资源图标同色系），区分普通受伤红飘字
            DamageNumber.Show(pos, text, new Color(1f, 0.85f, 0.3f));
        }
    }
}
