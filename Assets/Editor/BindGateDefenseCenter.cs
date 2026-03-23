using UnityEngine;
using UnityEditor;

public class BindGateDefenseCenter : Editor
{
    [MenuItem("Tools/DrscfZ/Bind Gate Defense Center")]
    public static void Execute()
    {
        // 找 WorkerManager
        var wm = Object.FindObjectOfType<DrscfZ.Survival.WorkerManager>();
        if (wm == null) { Debug.LogError("找不到 WorkerManager"); return; }

        // 找 WorkTarget_Gate
        var gate = GameObject.Find("[SurvivalManagers]/WorkerManager/WorkTargets/WorkTarget_Gate");
        if (gate == null)
        {
            // 尝试其他路径
            var all = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (var t in all)
            {
                if (t.name == "WorkTarget_Gate" && t.gameObject.scene.isLoaded)
                {
                    gate = t.gameObject;
                    break;
                }
            }
        }

        if (gate == null) { Debug.LogError("找不到 WorkTarget_Gate"); return; }

        var so = new SerializedObject(wm);
        var prop = so.FindProperty("_gateDefenseCenter");
        if (prop == null) { Debug.LogError("找不到 _gateDefenseCenter 字段"); return; }

        prop.objectReferenceValue = gate.transform;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(wm);

        Debug.Log($"[BindGateDefenseCenter] 已绑定 WorkTarget_Gate (pos={gate.transform.position}) 到 WorkerManager._gateDefenseCenter");
    }
}
