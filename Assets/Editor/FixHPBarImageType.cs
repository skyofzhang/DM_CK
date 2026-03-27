using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// 修复所有 Monster/Worker Prefab 中 HPFill Image 的 ImageType → Filled + Horizontal
/// fillAmount 只在 ImageType=Filled 时生效，Simple/Sliced 类型无效。
/// </summary>
public class FixHPBarImageType
{
    [MenuItem("Tools/DrscfZ/Fix HP Bar Image Type")]
    public static void Execute()
    {
        int fixedCount = 0;

        // Monster prefabs
        string[] monsterPaths = {
            "Assets/Prefabs/Monsters/KuanggongMonster_03.prefab",
            "Assets/Prefabs/Monsters/KuanggongMonster_04.prefab",
            "Assets/Prefabs/Monsters/KuanggongBoss_05.prefab",
        };

        // Worker prefabs
        string[] workerPaths = {
            "Assets/Prefabs/Characters/KuanggongWorker_01.prefab",
            "Assets/Prefabs/Characters/KuanggongWorker_02.prefab",
        };

        var allPaths = new System.Collections.Generic.List<string>();
        allPaths.AddRange(monsterPaths);
        allPaths.AddRange(workerPaths);

        foreach (var path in allPaths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { Debug.LogWarning($"[FixHPBar] 未找到: {path}"); continue; }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                var root = scope.prefabContentsRoot;
                var hpCanvas = root.transform.Find("HPBarCanvas");
                if (hpCanvas == null) { Debug.LogWarning($"[FixHPBar] {prefab.name}: 无 HPBarCanvas"); continue; }

                // 找 HPFill（直接子节点）
                var hpFillTr = hpCanvas.Find("HPFill");
                if (hpFillTr == null)
                {
                    // Fallback: 找任何名含 Fill 的子节点
                    foreach (Transform child in hpCanvas)
                    {
                        if (child.name.Contains("Fill") || child.name.Contains("fill"))
                        { hpFillTr = child; break; }
                    }
                }
                if (hpFillTr == null) { Debug.LogWarning($"[FixHPBar] {prefab.name}: 找不到 HPFill 子节点"); continue; }

                var img = hpFillTr.GetComponent<Image>();
                if (img == null) { Debug.LogWarning($"[FixHPBar] {prefab.name}: HPFill 无 Image 组件"); continue; }

                bool changed = false;
                if (img.type != Image.Type.Filled)
                {
                    img.type = Image.Type.Filled;
                    changed = true;
                }
                if (img.fillMethod != Image.FillMethod.Horizontal)
                {
                    img.fillMethod = Image.FillMethod.Horizontal;
                    changed = true;
                }
                if (img.fillOrigin != (int)Image.OriginHorizontal.Left)
                {
                    img.fillOrigin = (int)Image.OriginHorizontal.Left;
                    changed = true;
                }
                // 初始填充量设为1（满血）
                if (img.fillAmount < 0.99f)
                {
                    img.fillAmount = 1f;
                    changed = true;
                }

                if (changed)
                {
                    fixedCount++;
                    Debug.Log($"[FixHPBar] ✓ 已修复: {prefab.name}/HPBarCanvas/{hpFillTr.name} → Filled/Horizontal");
                }
                else
                {
                    Debug.Log($"[FixHPBar] OK (无需修改): {prefab.name}/{hpFillTr.name}");
                }
            }
        }

        // 同步修复场景中 WorkerPool 下所有 Worker 实例（场景里是实例，非 Prefab）
        var workers = Object.FindObjectsOfType<DrscfZ.Survival.WorkerController>(true);
        int sceneFixed = 0;
        foreach (var w in workers)
        {
            var hpCanvas = w.transform.Find("HPBarCanvas");
            if (hpCanvas == null) continue;
            var hpFillTr = hpCanvas.Find("HPFill");
            if (hpFillTr == null) continue;
            var img = hpFillTr.GetComponent<Image>();
            if (img == null) continue;

            bool changed = false;
            if (img.type != Image.Type.Filled)     { img.type = Image.Type.Filled; changed = true; }
            if (img.fillMethod != Image.FillMethod.Horizontal) { img.fillMethod = Image.FillMethod.Horizontal; changed = true; }
            if (img.fillOrigin != (int)Image.OriginHorizontal.Left) { img.fillOrigin = (int)Image.OriginHorizontal.Left; changed = true; }
            if (img.fillAmount < 0.99f)            { img.fillAmount = 1f; changed = true; }

            if (changed)
            {
                EditorUtility.SetDirty(w.gameObject);
                sceneFixed++;
            }
        }

        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[FixHPBar] 完成！修复 Prefab={fixedCount}，场景Worker={sceneFixed}");
    }
}
