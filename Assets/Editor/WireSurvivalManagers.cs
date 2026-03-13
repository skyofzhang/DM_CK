#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using DrscfZ.Survival;
using DrscfZ.UI;
using DrscfZ.Monster;

public class WireSurvivalManagers : MonoBehaviour
{
    [MenuItem("极地生存/Wire Survival Managers")]
    public static void Execute()
    {
        // SurvivalGameManager
        var sgm = Object.FindObjectOfType<SurvivalGameManager>();
        if (sgm == null) { Debug.LogError("SurvivalGameManager not found!"); return; }
        var so = new SerializedObject(sgm);

        var dayNight     = Object.FindObjectOfType<DayNightCycleManager>();
        var resource     = Object.FindObjectOfType<ResourceSystem>();
        var cityGate     = Object.FindObjectOfType<CityGateSystem>();
        var waveSpawner  = Object.FindObjectOfType<MonsterWaveSpawner>();
        var workerMgr    = Object.FindObjectOfType<WorkerManager>();
        var settlementUI = Object.FindObjectOfType<SurvivalSettlementUI>();

        if (dayNight)    so.FindProperty("dayNightManager").objectReferenceValue = dayNight;
        if (resource)    so.FindProperty("resourceSystem").objectReferenceValue = resource;
        if (cityGate)    so.FindProperty("cityGateSystem").objectReferenceValue = cityGate;
        if (waveSpawner) so.FindProperty("monsterWaveSpawner").objectReferenceValue = waveSpawner;
        if (workerMgr)   so.FindProperty("workerManager").objectReferenceValue = workerMgr;
        if (settlementUI) so.FindProperty("_settlementUI").objectReferenceValue = settlementUI;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(sgm.gameObject);
        Debug.Log("[WireSurvivalManagers] SurvivalGameManager fully wired.");
    }
}
#endif
