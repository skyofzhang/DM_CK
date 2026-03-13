using UnityEngine;
using UnityEditor;
using DrscfZ.Survival;
using DrscfZ.UI;

/// <summary>
/// Phase 2 诊断脚本：一键检查所有关键 SerializedField 绑定状态
/// </summary>
public class DiagPhase2
{
    [MenuItem("Tools/Phase2/Diagnose Phase2 Wiring")]
    public static void Execute()
    {
        int pass = 0, fail = 0;

        // ── WorkerManager._preCreatedWorkers ──────────────────────────────
        var wm = Object.FindObjectOfType<WorkerManager>();
        if (wm == null)
        {
            Debug.LogError("[Diag] ❌ WorkerManager NOT FOUND");
            fail++;
        }
        else
        {
            var wmSo = new SerializedObject(wm);
            var workers = wmSo.FindProperty("_preCreatedWorkers");
            int workerCount = workers != null ? workers.arraySize : -1;
            if (workerCount == 20)
            {
                // 检查第一个和最后一个是否非null
                bool first = workers.GetArrayElementAtIndex(0).objectReferenceValue != null;
                bool last  = workers.GetArrayElementAtIndex(19).objectReferenceValue != null;
                if (first && last)
                {
                    Debug.Log($"[Diag] ✅ WorkerManager._preCreatedWorkers: 20 workers wired (first={first}, last={last})");
                    pass++;
                }
                else
                {
                    Debug.LogWarning($"[Diag] ⚠️ WorkerManager._preCreatedWorkers: size=20 but some null (first={first}, last={last})");
                    fail++;
                }
            }
            else
            {
                Debug.LogError($"[Diag] ❌ WorkerManager._preCreatedWorkers: expected 20, got {workerCount}");
                fail++;
            }
        }

        // ── GiftNotificationUI ────────────────────────────────────────────
        var gn = Object.FindObjectOfType<GiftNotificationUI>();
        if (gn == null)
        {
            Debug.LogError("[Diag] ❌ GiftNotificationUI NOT FOUND");
            fail++;
        }
        else
        {
            var gnSo = new SerializedObject(gn);
            string[] fields = {
                "_canvasRoot", "_t1Particle",
                "_t2Panel", "_t2TopLeftPS", "_t2CenterRing",
                "_t3Panel", "_t3GiftIcon",
                "_t4Panel", "_t4OrangeOverlay", "_t4ChargingSlider",
                "_t5Panel", "_t5BlackOverlay", "_t5PlayerNameText"
            };
            int gnFail = 0;
            foreach (var f in fields)
            {
                var prop = gnSo.FindProperty(f);
                if (prop == null || prop.objectReferenceValue == null)
                {
                    Debug.LogWarning($"[Diag] ⚠️ GiftNotificationUI.{f} = null");
                    gnFail++;
                }
            }
            // BannerSlots
            var slots = gnSo.FindProperty("_bannerSlots");
            int slotFail = 0;
            if (slots != null && slots.arraySize == 3)
            {
                for (int i = 0; i < 3; i++)
                {
                    var root = slots.GetArrayElementAtIndex(i).FindPropertyRelative("root");
                    if (root == null || root.objectReferenceValue == null)
                        slotFail++;
                }
            }
            else slotFail = 3;

            if (gnFail == 0 && slotFail == 0)
            {
                Debug.Log("[Diag] ✅ GiftNotificationUI: all fields wired");
                pass++;
            }
            else
            {
                Debug.LogWarning($"[Diag] ⚠️ GiftNotificationUI: {gnFail} field(s) null, {slotFail} banner slot root(s) null");
                fail++;
            }
        }

        // ── BroadcasterPanel ─────────────────────────────────────────────
        var bp = Object.FindObjectOfType<DrscfZ.UI.BroadcasterPanel>();
        if (bp == null)
        {
            Debug.LogError("[Diag] ❌ BroadcasterPanel NOT FOUND");
            fail++;
        }
        else
        {
            var bpSo = new SerializedObject(bp);
            string[] bpFields = { "_panelRoot", "_boostBtn", "_boostCdText", "_eventBtn", "_eventCdText" };
            int bpFail = 0;
            foreach (var f in bpFields)
            {
                var prop = bpSo.FindProperty(f);
                if (prop == null || prop.objectReferenceValue == null)
                    bpFail++;
            }
            if (bpFail == 0)
            {
                Debug.Log("[Diag] ✅ BroadcasterPanel: all fields wired");
                pass++;
            }
            else
            {
                Debug.LogWarning($"[Diag] ⚠️ BroadcasterPanel: {bpFail} field(s) null");
                fail++;
            }
        }

        // ── Worker_00 visual check ────────────────────────────────────────
        var workerPool = GameObject.Find("WorkerPool");
        if (workerPool == null)
        {
            Debug.LogError("[Diag] ❌ WorkerPool NOT FOUND");
            fail++;
        }
        else
        {
            var w0 = workerPool.transform.Find("Worker_00");
            if (w0 == null)
            {
                Debug.LogError("[Diag] ❌ Worker_00 NOT FOUND");
                fail++;
            }
            else
            {
                var vis = w0.GetComponent<DrscfZ.Survival.WorkerVisual>();
                var ctrl = w0.GetComponent<WorkerController>();
                var bubble = w0.GetComponentInChildren<DrscfZ.UI.WorkerBubble>(true);

                var visSo = vis != null ? new SerializedObject(vis) : null;
                bool normalMatOk = visSo != null &&
                    visSo.FindProperty("_normalMaterial")?.objectReferenceValue != null;
                bool glowMatOk = visSo != null &&
                    visSo.FindProperty("_glowMaterial")?.objectReferenceValue != null;

                if (vis != null && ctrl != null && bubble != null && normalMatOk && glowMatOk)
                {
                    Debug.Log($"[Diag] ✅ Worker_00: WorkerVisual+Controller+Bubble OK, _normalMaterial+_glowMaterial set");
                    pass++;
                }
                else
                {
                    Debug.LogWarning($"[Diag] ⚠️ Worker_00: vis={vis!=null}, ctrl={ctrl!=null}, bubble={bubble!=null}, normalMat={normalMatOk}, glowMat={glowMatOk}");
                    fail++;
                }
            }
        }

        Debug.Log($"[Diag] ═══ Phase 2 Wiring Check: {pass} PASS / {fail} FAIL ═══");
    }
}
