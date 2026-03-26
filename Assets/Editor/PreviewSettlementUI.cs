using UnityEngine;
using UnityEditor;

public class PreviewSettlementUI : Editor
{
    [MenuItem("Tools/DrscfZ/Preview Settlement UI")]
    public static void Execute()
    {
        var all = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in all)
        {
            if (t.name == "SurvivalSettlementPanel" && t.GetComponent<RectTransform>() != null)
            {
                // 临时激活面板和各个屏幕以便查看
                t.gameObject.SetActive(true);

                var screenA = t.Find("ScreenA");
                var screenB = t.Find("ScreenB");
                var screenC = t.Find("ScreenC");

                // 显示 ScreenB（数据屏）作为预览
                if (screenA) screenA.gameObject.SetActive(false);
                if (screenB) screenB.gameObject.SetActive(true);
                if (screenC) screenC.gameObject.SetActive(false);

                // 激活按钮
                var btnView = t.Find("BtnViewRanking");
                var btnRestart = t.Find("RestartButton");
                if (btnView) btnView.gameObject.SetActive(true);
                if (btnRestart) btnRestart.gameObject.SetActive(true);

                Selection.activeGameObject = t.gameObject;
                Debug.Log("[PreviewSettlementUI] 结算面板已临时激活，显示 ScreenB");
                return;
            }
        }
        Debug.LogError("找不到 SurvivalSettlementPanel");
    }
}
