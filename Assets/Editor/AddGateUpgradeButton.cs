using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrscfZ.UI;

namespace DrscfZ.EditorTools
{
    /// <summary>
    /// 🆕 v1.22 §10：在场景 BroadcasterPanel 下复制 _eventBtn 为模板，
    /// 创建"升级城门" Button 并绑定到 BroadcasterPanel._btnUpgradeGate。
    /// 幂等：若字段已绑，直接跳过。
    /// </summary>
    public static class AddGateUpgradeButton
    {
        [MenuItem("Tools/DrscfZ/Add Gate Upgrade Button")]
        public static void Execute()
        {
            var bp = Object.FindObjectOfType<BroadcasterPanel>(true);
            if (bp == null)
            {
                Debug.LogError("[AddGateUpgradeButton] 未找到 BroadcasterPanel 组件");
                return;
            }

            var so = new SerializedObject(bp);
            var propUpgrade = so.FindProperty("_btnUpgradeGate");
            if (propUpgrade == null)
            {
                Debug.LogError("[AddGateUpgradeButton] BroadcasterPanel 无 _btnUpgradeGate 字段");
                return;
            }
            if (propUpgrade.objectReferenceValue != null)
            {
                Debug.Log($"[AddGateUpgradeButton] _btnUpgradeGate 已绑定（{propUpgrade.objectReferenceValue.name}），跳过");
                return;
            }

            var propEvent = so.FindProperty("_eventBtn");
            var propBoost = so.FindProperty("_boostBtn");
            Button refBtn =
                (propEvent?.objectReferenceValue as Button) ??
                (propBoost?.objectReferenceValue as Button);
            if (refBtn == null)
            {
                Debug.LogError("[AddGateUpgradeButton] 未找到参考按钮 _eventBtn/_boostBtn");
                return;
            }

            var refGo = refBtn.gameObject;
            var newGo = Object.Instantiate(refGo, refGo.transform.parent);
            newGo.name = "BtnUpgradeGate";

            var refRt = refBtn.GetComponent<RectTransform>();
            var newRt = newGo.GetComponent<RectTransform>();
            if (refRt != null && newRt != null)
            {
                float offsetY = refRt.sizeDelta.y + 8f;
                newRt.anchoredPosition = refRt.anchoredPosition + new Vector2(0f, -offsetY);
            }

            var newBtn = newGo.GetComponent<Button>();
            if (newBtn != null) newBtn.onClick.RemoveAllListeners();

            var img = newGo.GetComponent<Image>();
            if (img != null) img.color = new Color(0x1A / 255f, 0x3A / 255f, 0x5A / 255f, 1f);

            var tmp = newGo.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
            {
                var stmp = new SerializedObject(tmp);
                var pText = stmp.FindProperty("m_text");
                if (pText != null) pText.stringValue = "升级城门";
                var pFc = stmp.FindProperty("m_fontColor");
                if (pFc != null) pFc.colorValue = new Color(0.82f, 0.92f, 1f, 1f);
                var pFc32 = stmp.FindProperty("m_fontColor32");
                if (pFc32 != null) pFc32.colorValue = new Color(0.82f, 0.92f, 1f, 1f);
                stmp.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                var legacy = newGo.GetComponentInChildren<Text>(true);
                if (legacy != null) legacy.text = "升级城门";
            }

            propUpgrade.objectReferenceValue = newBtn;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(bp);
            EditorUtility.SetDirty(newGo);
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);

            Debug.Log($"[AddGateUpgradeButton] 完成：新按钮 {newGo.name} 已绑 BroadcasterPanel._btnUpgradeGate，场景保存={saved}");
        }
    }
}
