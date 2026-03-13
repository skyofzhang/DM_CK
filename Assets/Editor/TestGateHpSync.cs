using UnityEngine;
using DrscfZ.Survival;

/// <summary>
/// 运行时测试：模拟服务器同步城门 HP 为 500/1000（50%），
/// 验证文字变为黄色、进度条收缩到一半。
/// 菜单：Tools/Phase2/Test Gate HP Sync (500/1000)
/// </summary>
public class TestGateHpSync
{
    [UnityEditor.MenuItem("Tools/Phase2/Test Gate HP Sync (500/1000)")]
    public static void Execute()
    {
        if (CityGateSystem.Instance == null)
        {
            Debug.LogWarning("[TestGateHpSync] CityGateSystem.Instance 为 null，请在运行模式下执行");
            return;
        }
        CityGateSystem.Instance.SyncFromServer(500, 1000);
        Debug.Log("[TestGateHpSync] 已同步 HP=500/1000，期望：文字黄色，进度条半满");
    }
}
