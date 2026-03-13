using UnityEngine;

public class TestDamageNumber
{
    public static void Execute()
    {
        var cam = Camera.main;
        if (cam == null) { Debug.LogError("[TestDamageNumber] No main camera"); return; }
        var pos = cam.transform.position + cam.transform.forward * 5f;
        DamageNumber.Show(pos, 42, new Color(1f, 0.3f, 0.3f));
        DamageNumber.Show(pos + Vector3.up, 999, Color.yellow);
        Debug.Log("[TestDamageNumber] DamageNumber.Show called at " + pos);
    }
}
