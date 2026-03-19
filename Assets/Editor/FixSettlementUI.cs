using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using System.Reflection;

/// <summary>
/// Tools → DrscfZ → Fix Settlement UI
/// 1. 将 BtnViewRanking 的 TMP 字体修复为与 RestartButton 相同
/// 2. 自动查找并绑定 _top3Slots（场景中已存在的 Slot 子对象）
/// </summary>
public class FixSettlementUI
{
    [MenuItem("Tools/DrscfZ/Fix Settlement UI")]
    public static void Execute()
    {
        var comp = Object.FindObjectOfType<DrscfZ.UI.SurvivalSettlementUI>(true);
        if (comp == null) { Debug.LogError("[FixSettlement] 未找到 SurvivalSettlementUI"); return; }

        var so = new SerializedObject(comp);

        // ── 1. 修复 BtnViewRanking 字体 ──────────────────────────────────
        // 找 RestartButton 上的 TMP 字体作为参考
        TMP_FontAsset referenceFont = null;
        var restartBtn = comp.transform.Find("RestartButton")
                      ?? FindDeep(comp.transform, "RestartButton");
        if (restartBtn != null)
        {
            var refTmp = restartBtn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (refTmp != null) referenceFont = refTmp.font;
        }

        var viewBtn = comp.transform.Find("BtnViewRanking")
                   ?? FindDeep(comp.transform, "BtnViewRanking");
        if (viewBtn != null)
        {
            var viewTmp = viewBtn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (viewTmp != null && referenceFont != null && viewTmp.font != referenceFont)
            {
                Undo.RecordObject(viewTmp, "Fix BtnViewRanking Font");
                viewTmp.font = referenceFont;
                EditorUtility.SetDirty(viewTmp);
                Debug.Log($"[FixSettlement] BtnViewRanking 字体已修复为: {referenceFont.name}");
            }
            else if (viewTmp == null)
                Debug.LogWarning("[FixSettlement] BtnViewRanking 没有 TMP 子组件");
            else if (referenceFont == null)
                Debug.LogWarning("[FixSettlement] 未找到 RestartButton 字体参考");
            else
                Debug.Log("[FixSettlement] BtnViewRanking 字体已经正确，无需修复");
        }
        else
            Debug.LogWarning("[FixSettlement] 未找到 BtnViewRanking");

        // ── 2. 自动绑定 _btnViewRanking ─────────────────────────────────
        if (viewBtn != null)
        {
            var btnComp = viewBtn.GetComponent<Button>();
            if (btnComp != null)
            {
                var btnProp = so.FindProperty("_btnViewRanking");
                if (btnProp != null)
                {
                    btnProp.objectReferenceValue = btnComp;
                    so.ApplyModifiedProperties();
                    Debug.Log("[FixSettlement] _btnViewRanking 已绑定");
                }
            }
        }

        // ── 3. 自动绑定 _top3Slots ───────────────────────────────────────
        // 找到场景中命名含 "Slot" 或包含多个 NameText/ScoreText 的子对象
        // 策略：查找所有直接/间接子对象，名字包含 "Slot" 或 "slot"
        var allChildren = comp.GetComponentsInChildren<Transform>(true);
        var slotObjects = new System.Collections.Generic.List<GameObject>();
        foreach (var t in allChildren)
        {
            // 找包含 NameText 和 ScoreText 子组件的容器
            bool hasName  = t.Find("NameText")  != null;
            bool hasScore = t.Find("ScoreText") != null;
            if (hasName && hasScore)
                slotObjects.Add(t.gameObject);
        }

        if (slotObjects.Count >= 3)
        {
            var top3Prop = so.FindProperty("_top3Slots");
            if (top3Prop != null)
            {
                top3Prop.arraySize = 3;
                for (int i = 0; i < 3; i++)
                    top3Prop.GetArrayElementAtIndex(i).objectReferenceValue = slotObjects[i];
                so.ApplyModifiedProperties();
                Debug.Log($"[FixSettlement] _top3Slots 已绑定: {slotObjects[0].name}, {slotObjects[1].name}, {slotObjects[2].name}");
            }
            else
                Debug.LogWarning("[FixSettlement] 找不到 _top3Slots 序列化字段");
        }
        else
        {
            Debug.LogWarning($"[FixSettlement] 找到 {slotObjects.Count} 个 Slot（需要3个），请手动绑定 _top3Slots");
            foreach (var s in slotObjects)
                Debug.Log($"  候选 Slot: {s.name}");
        }

        // ── 保存场景 ──────────────────────────────────────────────────────
        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("[FixSettlement] 场景已保存");
        }
        else
            Debug.Log("[FixSettlement] Play Mode 中运行，场景需退出后手动保存");
    }

    private static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }
}
