#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using DrscfZ.Survival;
using DrscfZ.UI;
using DrscfZ.Monster;

public class RunWireSurvivalManagers
{
    public static void Execute()
    {
        var sgm = Object.FindObjectOfType<SurvivalGameManager>();
        if (sgm == null) { Debug.LogError("[RunWireSurvivalManagers] SurvivalGameManager not found!"); return; }

        var so = new SerializedObject(sgm);

        var dayNight    = Object.FindObjectOfType<DayNightCycleManager>();
        var resource    = Object.FindObjectOfType<ResourceSystem>();
        var cityGate    = Object.FindObjectOfType<CityGateSystem>();
        var waveSpawner = Object.FindObjectOfType<MonsterWaveSpawner>();
        var workerMgr   = Object.FindObjectOfType<WorkerManager>();

        // SurvivalSettlementUI lives on Canvas/SurvivalSettlementPanel (inactive) — use FindObjectsOfTypeAll
        SurvivalSettlementUI settlementUI = null;
        foreach (var go in Resources.FindObjectsOfTypeAll<SurvivalSettlementUI>())
        {
            if (go.gameObject.scene.IsValid()) { settlementUI = go; break; }
        }

        if (dayNight)     { so.FindProperty("dayNightManager").objectReferenceValue    = dayNight;     Debug.Log("[RunWireSurvivalManagers] dayNightManager wired"); }
        else Debug.LogWarning("[RunWireSurvivalManagers] DayNightCycleManager not found");

        if (resource)     { so.FindProperty("resourceSystem").objectReferenceValue     = resource;     Debug.Log("[RunWireSurvivalManagers] resourceSystem wired"); }
        else Debug.LogWarning("[RunWireSurvivalManagers] ResourceSystem not found");

        if (cityGate)     { so.FindProperty("cityGateSystem").objectReferenceValue     = cityGate;     Debug.Log("[RunWireSurvivalManagers] cityGateSystem wired"); }
        else Debug.LogWarning("[RunWireSurvivalManagers] CityGateSystem not found");

        if (waveSpawner)  { so.FindProperty("monsterWaveSpawner").objectReferenceValue = waveSpawner;  Debug.Log("[RunWireSurvivalManagers] monsterWaveSpawner wired"); }
        else Debug.LogWarning("[RunWireSurvivalManagers] MonsterWaveSpawner not found");

        if (workerMgr)    { so.FindProperty("workerManager").objectReferenceValue      = workerMgr;    Debug.Log("[RunWireSurvivalManagers] workerManager wired"); }
        else Debug.LogWarning("[RunWireSurvivalManagers] WorkerManager not found");

        if (settlementUI) { so.FindProperty("_settlementUI").objectReferenceValue      = settlementUI; Debug.Log("[RunWireSurvivalManagers] _settlementUI wired"); }
        else Debug.LogWarning("[RunWireSurvivalManagers] SurvivalSettlementUI not found");

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(sgm.gameObject);
        Debug.Log("[RunWireSurvivalManagers] SurvivalGameManager fully wired.");
    }
}
#endif
