using UnityEngine;
using UnityEditor;

public class HideSettlementUI : Editor
{
    public static void Execute()
    {
        var all = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in all)
        {
            if (t.name == "SurvivalSettlementPanel" && t.GetComponent<RectTransform>() != null)
            {
                t.gameObject.SetActive(false);

                // 同时隐藏子屏幕
                var screenA = t.Find("ScreenA");
                var screenB = t.Find("ScreenB");
                var screenC = t.Find("ScreenC");
                if (screenA) screenA.gameObject.SetActive(false);
                if (screenB) screenB.gameObject.SetActive(false);
                if (screenC) screenC.gameObject.SetActive(false);

                EditorUtility.SetDirty(t.gameObject);
                Debug.Log("[HideSettlementUI] 结算面板已隐藏");
                return;
            }
        }
    }
}
