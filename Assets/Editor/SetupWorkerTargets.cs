using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Creates 5 WorkTarget GameObjects in the scene and assigns them to WorkerManager's
/// individual Transform fields (_foodTarget, _coalTarget, _oreTarget, _heatTarget, _gateTarget).
/// </summary>
public class SetupWorkerTargets
{
    [MenuItem("极地生存/Setup Worker Targets")]
    public static void Execute()
    {
        // ── 1. Find WorkerManager in scene ──────────────────────────────────
        var workerManagerGO = GameObject.Find("[SurvivalManagers]/WorkerManager");
        if (workerManagerGO == null)
        {
            // Fallback: search all objects
            var all = Object.FindObjectsOfType<DrscfZ.Survival.WorkerManager>();
            if (all.Length > 0)
                workerManagerGO = all[0].gameObject;
        }

        if (workerManagerGO == null)
        {
            Debug.LogError("[SetupWorkerTargets] WorkerManager not found in scene!");
            return;
        }

        Debug.Log($"[SetupWorkerTargets] Found WorkerManager at: {GetPath(workerManagerGO)}");

        // ── 2. Create "WorkTargets" parent under WorkerManager ──────────────
        // Remove existing WorkTargets if already present
        Transform existingParent = workerManagerGO.transform.Find("WorkTargets");
        if (existingParent != null)
        {
            Undo.DestroyObjectImmediate(existingParent.gameObject);
            Debug.Log("[SetupWorkerTargets] Removed existing WorkTargets parent.");
        }

        GameObject workTargetsParent = new GameObject("WorkTargets");
        Undo.RegisterCreatedObjectUndo(workTargetsParent, "Create WorkTargets Parent");
        workTargetsParent.transform.SetParent(workerManagerGO.transform, false);
        workTargetsParent.transform.localPosition = Vector3.zero;

        // ── 3. Define the 5 target points ────────────────────────────────────
        // commandId 1 → _foodTarget  (钓鱼/摸鱼, fishing dock, right side)
        // commandId 2 → _coalTarget  (挖煤)
        // commandId 3 → _oreTarget   (挖矿)
        // commandId 4 → _heatTarget  (生火, near central fortress)
        // commandId 5 → _gateTarget  (修墙, in front of city gate)
        var targets = new (string name, string fieldName, Vector3 worldPos)[]
        {
            ("WorkTarget_Fish", "_foodTarget",  new Vector3(-7f, 0f, -5f)),
            ("WorkTarget_Coal", "_coalTarget",  new Vector3( 7f, 0f,  5f)),
            ("WorkTarget_Ore",  "_oreTarget",   new Vector3(-9f, 0f,  5f)),
            ("WorkTarget_Heat", "_heatTarget",  new Vector3( 0f, 0f,  2f)),
            ("WorkTarget_Gate", "_gateTarget",  new Vector3( 0f, 0f, -3f)),
        };

        // ── 4. Create the GameObjects ─────────────────────────────────────────
        var createdTransforms = new Transform[targets.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            var go = new GameObject(targets[i].name);
            Undo.RegisterCreatedObjectUndo(go, $"Create {targets[i].name}");
            go.transform.SetParent(workTargetsParent.transform, false);
            go.transform.position = targets[i].worldPos;
            createdTransforms[i] = go.transform;
            Debug.Log($"[SetupWorkerTargets] Created {targets[i].name} at world pos {targets[i].worldPos}");
        }

        // ── 5. Assign to WorkerManager via SerializedObject ──────────────────
        var so = new SerializedObject(workerManagerGO.GetComponent<DrscfZ.Survival.WorkerManager>());
        so.Update();

        for (int i = 0; i < targets.Length; i++)
        {
            string fieldName = targets[i].fieldName;
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = createdTransforms[i];
                Debug.Log($"[SetupWorkerTargets] Assigned {targets[i].name} → field '{fieldName}'");
            }
            else
            {
                Debug.LogWarning($"[SetupWorkerTargets] Field '{fieldName}' not found on WorkerManager!");
            }
        }

        so.ApplyModifiedProperties();

        // ── 6. Mark scene dirty ───────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(workerManagerGO.scene);

        Debug.Log("[SetupWorkerTargets] Done! 5 WorkTarget points created and assigned to WorkerManager.");
        Debug.Log("[SetupWorkerTargets]   WorkTarget_Fish  (_foodTarget)  → (-7, 0, -5)  [commandId=1, 钓鱼]");
        Debug.Log("[SetupWorkerTargets]   WorkTarget_Coal  (_coalTarget)  → ( 7, 0,  5)  [commandId=2, 挖煤]");
        Debug.Log("[SetupWorkerTargets]   WorkTarget_Ore   (_oreTarget)   → (-9, 0,  5)  [commandId=3, 挖矿]");
        Debug.Log("[SetupWorkerTargets]   WorkTarget_Heat  (_heatTarget)  → ( 0, 0,  2)  [commandId=4, 生火]");
        Debug.Log("[SetupWorkerTargets]   WorkTarget_Gate  (_gateTarget)  → ( 0, 0, -3)  [commandId=5, 修墙]");
    }

    private static string GetPath(GameObject go)
    {
        string path = go.name;
        Transform t = go.transform.parent;
        while (t != null) { path = t.name + "/" + path; t = t.parent; }
        return path;
    }
}
