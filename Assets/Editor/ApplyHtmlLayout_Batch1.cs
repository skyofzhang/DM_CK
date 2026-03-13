using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Batch 1 — 按HTML布局调整以下面板：
///   - FrozenStatusPanel    (12-freeze-status-banner.html)
///   - SurvivalSettingsPanel(08-settings-modal.html)
///   - GameUIPanel/TopBar   (03-game-hud.html)
///   - GameUIPanel/LiveRankingPanel (03/04 html)
///   - LoadingPanel          (02-loading-ui.html)
/// </summary>
public class ApplyHtmlLayout_Batch1
{
    public static void Execute()
    {
        int ok = 0;

        // ════════════════════════════════════════
        // 1. FrozenStatusPanel
        //    HTML: top:48% = 921px, height:100px, full-width horizontal banner
        //    当前: 底部锚点, y=60, height=60 → 需改为垂直居中锚点
        //    转换: 画布中心=960, 元素中心y = 921+50=971, 从中心偏移 = 960-971 = -11
        // ════════════════════════════════════════
        {
            var rt = GetRT("Canvas/FrozenStatusPanel");
            if (rt)
            {
                rt.anchorMin = new Vector2(0f, 0.5f);   // 水平拉伸，垂直居中锚
                rt.anchorMax = new Vector2(1f, 0.5f);
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, -11f); // 略低于中心
                rt.sizeDelta = new Vector2(0f, 100f);         // 全宽, 高100px
                EditorUtility.SetDirty(rt.gameObject);
                Debug.Log("[Batch1] FrozenStatusPanel: top:48% → anchor center-h, y=-11, h=100");
                ok++;
            }
        }

        // ════════════════════════════════════════
        // 2. SurvivalSettingsPanel
        //    HTML: modal width:860px, height:620px, 居中弹窗 (top:50%,left:50%)
        //    当前: 780×640 → 改为 860×620，位置保持(0,0)居中
        // ════════════════════════════════════════
        {
            var rt = GetRT("Canvas/SurvivalSettingsPanel");
            if (rt)
            {
                rt.sizeDelta = new Vector2(860f, 620f);
                EditorUtility.SetDirty(rt.gameObject);
                Debug.Log("[Batch1] SurvivalSettingsPanel: 780×640 → 860×620");
                ok++;
            }
        }

        // ════════════════════════════════════════
        // 3. GameUIPanel/TopBar
        //    HTML: top:0, height:120px, full-width
        //    当前: top锚点, y=-60 (60px below top), height=120px
        //    → 改为 y=0 (flush with top)
        // ════════════════════════════════════════
        {
            var rt = GetRT("Canvas/GameUIPanel/TopBar");
            if (rt)
            {
                // 保持 anchorMin(0,1) anchorMax(1,1) pivot(0.5,1) 不变，只改 anchoredPosition.y
                rt.anchoredPosition = new Vector2(0f, 0f);  // flush top
                rt.sizeDelta = new Vector2(0f, 120f);        // 确保高度120px
                EditorUtility.SetDirty(rt.gameObject);
                Debug.Log("[Batch1] TopBar: y=-60 → y=0 (flush top), h=120");
                ok++;
            }
        }

        // ════════════════════════════════════════
        // 4. GameUIPanel/LiveRankingPanel (贡献榜浮窗)
        //    HTML: right:0, top:140px, width:230px, height:460px
        //    锚点保持右中 (1,0.5)，pivot(1,0.5)
        //    元素中心 y from top = 140+230=370, from canvas center = 960-370 = +590
        //    x: flush right = 0 (pivot在右边, anchoredPosition.x=0)
        // ════════════════════════════════════════
        {
            var rt = GetRT("Canvas/GameUIPanel/LiveRankingPanel");
            if (rt)
            {
                rt.anchorMin = new Vector2(1f, 0.5f);
                rt.anchorMax = new Vector2(1f, 0.5f);
                rt.pivot     = new Vector2(1f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, 590f);  // 590px above canvas center
                rt.sizeDelta = new Vector2(230f, 460f);
                EditorUtility.SetDirty(rt.gameObject);
                Debug.Log("[Batch1] LiveRankingPanel: right:0, top:140 → anchor R-center, pos(0,590), 230×460");
                ok++;
            }
        }

        // ════════════════════════════════════════
        // 5. LoadingPanel
        //    HTML: full screen panel (1080×1920)
        //    当前: 检查是否已是 stretch-all
        //    确保为全屏 stretch 锚点，anchoredPos(0,0), sizeDelta(0,0)
        // ════════════════════════════════════════
        {
            var rt = GetRT("Canvas/LoadingPanel");
            if (rt)
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;
                EditorUtility.SetDirty(rt.gameObject);
                Debug.Log("[Batch1] LoadingPanel: → fullscreen stretch");
                ok++;
            }

            // LoadingPanel/LoadingText
            //   HTML: 加载提示文字，居中，top约55%=1056px, font-size:36px, height~45px
            //   unity_y = 960 - 1056 - 22 = -118
            var rtTxt = GetRT("Canvas/LoadingPanel/LoadingText");
            if (rtTxt)
            {
                rtTxt.anchorMin = new Vector2(0.5f, 0.5f);
                rtTxt.anchorMax = new Vector2(0.5f, 0.5f);
                rtTxt.pivot = new Vector2(0.5f, 0.5f);
                rtTxt.anchoredPosition = new Vector2(0f, -118f);
                rtTxt.sizeDelta = new Vector2(700f, 45f);
                EditorUtility.SetDirty(rtTxt.gameObject);
                Debug.Log("[Batch1] LoadingPanel/LoadingText: → center, y=-118");
                ok++;
            }
        }

        // ════════════════════════════════════════
        // 6. AnnouncementPanel（全屏公告弹窗）
        //    HTML: full screen overlay
        //    确保全屏 stretch
        // ════════════════════════════════════════
        {
            var rt = GetRT("Canvas/AnnouncementPanel");
            if (rt)
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;
                EditorUtility.SetDirty(rt.gameObject);
                Debug.Log("[Batch1] AnnouncementPanel: → fullscreen stretch");
                ok++;
            }

            // AnnouncementPanel/MainText — 主公告文字，垂直居中偏上
            //   HTML: 主文字居中，约top:40% = 768px, font-size:100px, height~120px
            //   unity_y = 960 - 768 - 60 = 132
            var rtMain = GetRT("Canvas/AnnouncementPanel/MainText");
            if (rtMain)
            {
                rtMain.anchorMin = new Vector2(0.5f, 0.5f);
                rtMain.anchorMax = new Vector2(0.5f, 0.5f);
                rtMain.pivot = new Vector2(0.5f, 0.5f);
                rtMain.anchoredPosition = new Vector2(0f, 132f);
                rtMain.sizeDelta = new Vector2(900f, 140f);
                EditorUtility.SetDirty(rtMain.gameObject);
                Debug.Log("[Batch1] AnnouncementPanel/MainText: → center, y=132, w=900");
                ok++;
            }

            // AnnouncementPanel/SubText — 副文字
            //   HTML: 英文副标题在主文字下约50px, height~50px
            //   top = 768+120+50 = 938, unity_y = 960-938-25 = -3
            var rtSub = GetRT("Canvas/AnnouncementPanel/SubText");
            if (rtSub)
            {
                rtSub.anchorMin = new Vector2(0.5f, 0.5f);
                rtSub.anchorMax = new Vector2(0.5f, 0.5f);
                rtSub.pivot = new Vector2(0.5f, 0.5f);
                rtSub.anchoredPosition = new Vector2(0f, -30f);
                rtSub.sizeDelta = new Vector2(800f, 50f);
                EditorUtility.SetDirty(rtSub.gameObject);
                Debug.Log("[Batch1] AnnouncementPanel/SubText: → center, y=-30");
                ok++;
            }
        }

        // ════════════════════════════════════════
        // 7. SurvivalRankingPanel（完整排行榜弹窗）
        //    HTML: 大型居中面板，约 900×1400px，垂直靠近上方
        //    top≈100px, 高1400px, 居中
        //    unity_y = 960 - 100 - 700 = 160
        // ════════════════════════════════════════
        {
            var rt = GetRT("Canvas/SurvivalRankingPanel");
            if (rt)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, 160f);
                rt.sizeDelta = new Vector2(940f, 1400f);
                EditorUtility.SetDirty(rt.gameObject);
                Debug.Log("[Batch1] SurvivalRankingPanel: → center top-biased, 940×1400");
                ok++;
            }
        }

        // 保存
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log($"[ApplyHtmlLayout_Batch1] ✅ Batch1 完成，共修改 {ok} 个元素");
    }

    static RectTransform GetRT(string path)
    {
        var all = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in all)
        {
            if (BuildPath(t) == path)
                return t.GetComponent<RectTransform>();
        }
        Debug.LogWarning($"[Batch1] 未找到: {path}");
        return null;
    }

    static string BuildPath(Transform t)
    {
        if (t.parent == null) return t.name;
        return BuildPath(t.parent) + "/" + t.name;
    }
}
