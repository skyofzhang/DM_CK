using UnityEngine;
using UnityEditor;

/// <summary>
/// 检查怪物/矿工 Prefab 内 HPBarCanvas 子节点结构
/// 及 Image 组件的 type、fillAmount 详细信息
/// </summary>
public class InspectHPBarPrefab
{
    [MenuItem("Tools/DrscfZ/Inspect HPBar Prefab Structure")]
    public static void Execute()
    {
        string[] paths = {
            "Assets/Prefabs/Monsters/KuanggongMonster_03.prefab",
            "Assets/Prefabs/Monsters/KuanggongMonster_04.prefab",
            "Assets/Prefabs/Monsters/KuanggongBoss_05.prefab",
            "Assets/Prefabs/Characters/KuanggongWorker_01.prefab",
        };

        foreach (var path in paths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { Debug.LogWarning($"[Inspect] 未找到: {path}"); continue; }

            Debug.Log($"=== {prefab.name} ===");

            // 1. 检查 MonsterController._hpFillImage Inspector 绑定
            var mc = prefab.GetComponent<DrscfZ.Monster.MonsterController>();
            var wc = prefab.GetComponent<DrscfZ.Survival.WorkerController>();

            var so = mc != null ? new SerializedObject(mc) : (wc != null ? new SerializedObject(wc) : null);
            if (so != null)
            {
                var imgProp = so.FindProperty("_hpFillImage");
                var canvasProp = so.FindProperty("_hpBarCanvas");
                Debug.Log($"  [Inspector] _hpFillImage = {(imgProp?.objectReferenceValue?.name ?? "NULL")}");
                Debug.Log($"  [Inspector] _hpBarCanvas = {(canvasProp?.objectReferenceValue?.name ?? "NULL")}");
            }

            // 2. 遍历 HPBarCanvas 子节点
            var hpCanvas = prefab.transform.Find("HPBarCanvas");
            if (hpCanvas == null)
            {
                Debug.LogWarning($"  [Hierarchy] 无 HPBarCanvas！");
                continue;
            }

            Debug.Log($"  [Hierarchy] HPBarCanvas 激活={hpCanvas.gameObject.activeSelf} scale={hpCanvas.localScale}");
            foreach (Transform child in hpCanvas)
            {
                var img = child.GetComponent<UnityEngine.UI.Image>();
                string imgInfo = img != null
                    ? $" Image: type={img.type} fillAmount={img.fillAmount:F3} fillMethod={img.fillMethod} fillOrigin={img.fillOrigin} color={img.color}"
                    : " (无 Image)";
                Debug.Log($"    子节点: '{child.name}' activeSelf={child.gameObject.activeSelf} scale={child.localScale}{imgInfo}");

                // 检查更深层的子节点
                foreach (Transform grandChild in child)
                {
                    var gcImg = grandChild.GetComponent<UnityEngine.UI.Image>();
                    string gcInfo = gcImg != null
                        ? $" Image: type={gcImg.type} fillAmount={gcImg.fillAmount:F3} fillMethod={gcImg.fillMethod}"
                        : " (无 Image)";
                    Debug.Log($"      孙节点: '{grandChild.name}'{gcInfo}");
                }
            }

            // 3. 用 Transform.Find("HPFill") 测试
            var hpFillTr = hpCanvas.Find("HPFill");
            Debug.Log($"  [Find Test] hpCanvas.Find(\"HPFill\") = {(hpFillTr != null ? hpFillTr.name : "NULL")}");

            // 4. GetComponentInChildren 顺序
            var firstImg = hpCanvas.GetComponentInChildren<UnityEngine.UI.Image>();
            Debug.Log($"  [GetComponentInChildren] 第一个 Image = '{firstImg?.name ?? "NULL"}'");

            // 5. 列出所有 Image
            var allImgs = hpCanvas.GetComponentsInChildren<UnityEngine.UI.Image>();
            for (int i = 0; i < allImgs.Length; i++)
                Debug.Log($"  [AllImages][{i}] '{allImgs[i].name}' type={allImgs[i].type} fillAmount={allImgs[i].fillAmount:F3}");
        }
    }
}
