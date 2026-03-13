using UnityEngine;
using DrscfZ.Survival;

/// <summary>
/// 运行时通过 FindObjectOfType 调用 SyncFromServer，测试低血量（红色）状态。
/// </summary>
public class TestGateHpRuntime
{
    public static void Execute()
    {
        var gate = Object.FindObjectOfType<CityGateSystem>();
        if (gate == null)
        {
            Debug.LogWarning("[TestGateHpRuntime] 场景中找不到 CityGateSystem，请确认游戏已运行");
            return;
        }
        // 模拟 HP=200/1000（20%），期望文字红色、进度条约1/5
        gate.SyncFromServer(200, 1000);
        Debug.Log($"[TestGateHpRuntime] SyncFromServer(200,1000) 已调用，当前HP={gate.CurrentHp}/{gate.MaxHp}");
    }
}
