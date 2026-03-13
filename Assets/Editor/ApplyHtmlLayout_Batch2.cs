using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Batch 2 — 按HTML布局调整：
///   - SurvivalSettlementPanel ScreenA/B/C 内部元素
///   - RestartButton (底部主按钮)
///   - VIPAnnouncement (全屏overlay)
///   - JoinNotification (底部居中通知容器)
///   - GiftNotification (全宽横幅)
///   - BottomBar (高度 120→280px)
///
/// 坐标系：Canvas 1080×1920, 中心anchor, y正=上
/// 换算：unity_y = 960 - html_top - height/2
/// </summary>
public class ApplyHtmlLayout_Batch2
{
    public static void Execute()
    {
        int ok = 0;

        // ════════════════════════════════════════
        // 1. ScreenA 元素 — 结算A屏（胜利结果）
        //    HTML: hero区 top:280, 主标题 h1 font:120, 天数副标 font:88
        // ════════════════════════════════════════
        {
            // ResultTitle: "极地已守护！" / "度过凛冬！"
            // h1: top=280, font=120px → height≈140 → center_y=350
            // unity_y = 960-350 = 610
            var rt = GetRT("Canvas/SurvivalSettlementPanel/ScreenA/ResultTitle");
            if (rt)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, 610f);
                rt.sizeDelta        = new Vector2(860f, 140f);
                EditorUtility.SetDirty(rt.gameObject);
                Debug.Log("[Batch2] ScreenA/ResultTitle: → center, y=610, 860×140");
                ok++;
            }

            // ResultSubtitle: "第X天" — hero区内 .day 元素
            // .day top≈599px, font=88px → height≈100 → center_y=649
            // unity_y = 960-649 = 311
            var rt2 = GetRT("Canvas/SurvivalSettlementPanel/ScreenA/ResultSubtitle");
            if (rt2)
            {
                rt2.anchorMin = new Vector2(0.5f, 0.5f);
                rt2.anchorMax = new Vector2(0.5f, 0.5f);
                rt2.pivot     = new Vector2(0.5f, 0.5f);
                rt2.anchoredPosition = new Vector2(0f, 311f);
                rt2.sizeDelta        = new Vector2(700f, 100f);
                EditorUtility.SetDirty(rt2.gameObject);
                Debug.Log("[Batch2] ScreenA/ResultSubtitle: → center, y=311, 700×100");
                ok++;
            }
        }

        // ════════════════════════════════════════
        // 2. ScreenB 元素 — 本局数据
        //    HTML: 标题top:100, 数据卡片面板top:260 (left:80, right:80, pad:50 60)
        //    2列3行网格: 列宽380, colGap=40, rowGap=50, 行高180
        //    col1_cx=-210, col2_cx=210
        //    row1_cy=400→unity_y=560
        //    row2_cy=630→unity_y=330
        //    row3_cy=860→unity_y=100
        // ════════════════════════════════════════
        {
            var rtHeader = GetRT("Canvas/SurvivalSettlementPanel/ScreenB/StatsHeader");
            if (rtHeader)
            {
                rtHeader.anchorMin = new Vector2(0.5f, 0.5f);
                rtHeader.anchorMax = new Vector2(0.5f, 0.5f);
                rtHeader.pivot     = new Vector2(0.5f, 0.5f);
                rtHeader.anchoredPosition = new Vector2(0f, 818f);
                rtHeader.sizeDelta        = new Vector2(800f, 84f);
                EditorUtility.SetDirty(rtHeader.gameObject);
                Debug.Log("[Batch2] ScreenB/StatsHeader: → center top, y=818");
                ok++;
            }

            // TotalGatherText: 食物采集 (row1, col1)
            var rtGather = GetRT("Canvas/SurvivalSettlementPanel/ScreenB/TotalGatherText");
            if (rtGather)
            {
                rtGather.anchorMin = new Vector2(0.5f, 0.5f);
                rtGather.anchorMax = new Vector2(0.5f, 0.5f);
                rtGather.pivot     = new Vector2(0.5f, 0.5f);
                rtGather.anchoredPosition = new Vector2(-210f, 560f);
                rtGather.sizeDelta        = new Vector2(380f, 180f);
                EditorUtility.SetDirty(rtGather.gameObject);
                Debug.Log("[Batch2] ScreenB/TotalGatherText: → grid(1,1), pos(-210,560)");
                ok++;
            }

            // TotalKillsText: 怪物波次 (row1, col2)
            var rtKills = GetRT("Canvas/SurvivalSettlementPanel/ScreenB/TotalKillsText");
            if (rtKills)
            {
                rtKills.anchorMin = new Vector2(0.5f, 0.5f);
                rtKills.anchorMax = new Vector2(0.5f, 0.5f);
                rtKills.pivot     = new Vector2(0.5f, 0.5f);
                rtKills.anchoredPosition = new Vector2(210f, 560f);
                rtKills.sizeDelta        = new Vector2(380f, 180f);
                EditorUtility.SetDirty(rtKills.gameObject);
                Debug.Log("[Batch2] ScreenB/TotalKillsText: → grid(1,2), pos(210,560)");
                ok++;
            }

            // TotalRepairText: 城门HP (row2, col2)
            var rtRepair = GetRT("Canvas/SurvivalSettlementPanel/ScreenB/TotalRepairText");
            if (rtRepair)
            {
                rtRepair.anchorMin = new Vector2(0.5f, 0.5f);
                rtRepair.anchorMax = new Vector2(0.5f, 0.5f);
                rtRepair.pivot     = new Vector2(0.5f, 0.5f);
                rtRepair.anchoredPosition = new Vector2(210f, 330f);
                rtRepair.sizeDelta        = new Vector2(380f, 180f);
                EditorUtility.SetDirty(rtRepair.gameObject);
                Debug.Log("[Batch2] ScreenB/TotalRepairText: → grid(2,2), pos(210,330)");
                ok++;
            }

            // SurvivalDaysText: 存活天数 (row3, col2)
            var rtDays = GetRT("Canvas/SurvivalSettlementPanel/ScreenB/SurvivalDaysText");
            if (rtDays)
            {
                rtDays.anchorMin = new Vector2(0.5f, 0.5f);
                rtDays.anchorMax = new Vector2(0.5f, 0.5f);
                rtDays.pivot     = new Vector2(0.5f, 0.5f);
                rtDays.anchoredPosition = new Vector2(210f, 100f);
                rtDays.sizeDelta        = new Vector2(380f, 180f);
                EditorUtility.SetDirty(rtDays.gameObject);
                Debug.Log("[Batch2] ScreenB/SurvivalDaysText: → grid(3,2), pos(210,100)");
                ok++;
            }

            // RankingTitle: 排行榜标题 (数据网格下方)
            // 约 top:1040px, h:50px → center_y=1065 → unity_y=-105
            var rtRankTitle = GetRT("Canvas/SurvivalSettlementPanel/ScreenB/RankingTitle");
            if (rtRankTitle)
            {
                rtRankTitle.anchorMin = new Vector2(0.5f, 0.5f);
                rtRankTitle.anchorMax = new Vector2(0.5f, 0.5f);
                rtRankTitle.pivot     = new Vector2(0.5f, 0.5f);
                rtRankTitle.anchoredPosition = new Vector2(0f, -105f);
                rtRankTitle.sizeDelta        = new Vector2(800f, 50f);
                EditorUtility.SetDirty(rtRankTitle.gameObject);
                Debug.Log("[Batch2] ScreenB/RankingTitle: → below stats, y=-105");
                ok++;
            }

            // RankingList: 10行排行 (RankingTitle下方)
            // top:1110px, height:700px → center_y=1460 → unity_y=-500
            var rtRankList = GetRT("Canvas/SurvivalSettlementPanel/ScreenB/RankingList");
            if (rtRankList)
            {
                rtRankList.anchorMin = new Vector2(0.5f, 0.5f);
                rtRankList.anchorMax = new Vector2(0.5f, 0.5f);
                rtRankList.pivot     = new Vector2(0.5f, 0.5f);
                rtRankList.anchoredPosition = new Vector2(0f, -500f);
                rtRankList.sizeDelta        = new Vector2(920f, 700f);
                EditorUtility.SetDirty(rtRankList.gameObject);
                Debug.Log("[Batch2] ScreenB/RankingList: → below title, y=-500, 920×700");
                ok++;
            }
        }

        // ════════════════════════════════════════
        // 3. ScreenC 元素 — 英雄榜（MVP+颁奖台）
        //    HTML: MVP框 top:220 height:280, 颁奖台 top:580 bottom:960(top)
        //    颁奖台align-items:flex-end → 底部对齐于y=960(html)=unity_y=0
        // ════════════════════════════════════════
        {
            // MvpLabel: "本局MVP" 标签
            // mvp top:220, 内部 line+padding+label: center_y≈248 → unity_y=712
            var rtMvpLabel = GetRT("Canvas/SurvivalSettlementPanel/ScreenC/MvpLabel");
            if (rtMvpLabel)
            {
                rtMvpLabel.anchorMin = new Vector2(0.5f, 0.5f);
                rtMvpLabel.anchorMax = new Vector2(0.5f, 0.5f);
                rtMvpLabel.pivot     = new Vector2(0.5f, 0.5f);
                rtMvpLabel.anchoredPosition = new Vector2(0f, 712f);
                rtMvpLabel.sizeDelta        = new Vector2(700f, 40f);
                EditorUtility.SetDirty(rtMvpLabel.gameObject);
                Debug.Log("[Batch2] ScreenC/MvpLabel: → mvp header, y=712");
                ok++;
            }

            // MvpNameText: MVP玩家名
            // mvp center ≈ top:360, name font:48px → center_y=340 → unity_y=620
            var rtMvpName = GetRT("Canvas/SurvivalSettlementPanel/ScreenC/MvpNameText");
            if (rtMvpName)
            {
                rtMvpName.anchorMin = new Vector2(0.5f, 0.5f);
                rtMvpName.anchorMax = new Vector2(0.5f, 0.5f);
                rtMvpName.pivot     = new Vector2(0.5f, 0.5f);
                rtMvpName.anchoredPosition = new Vector2(80f, 620f);
                rtMvpName.sizeDelta        = new Vector2(500f, 70f);
                EditorUtility.SetDirty(rtMvpName.gameObject);
                Debug.Log("[Batch2] ScreenC/MvpNameText: → mvp name, y=620");
                ok++;
            }

            // MvpScoreText: MVP贡献值
            // below name ~44px → center_y=390 → unity_y=570
            var rtMvpScore = GetRT("Canvas/SurvivalSettlementPanel/ScreenC/MvpScoreText");
            if (rtMvpScore)
            {
                rtMvpScore.anchorMin = new Vector2(0.5f, 0.5f);
                rtMvpScore.anchorMax = new Vector2(0.5f, 0.5f);
                rtMvpScore.pivot     = new Vector2(0.5f, 0.5f);
                rtMvpScore.anchoredPosition = new Vector2(80f, 560f);
                rtMvpScore.sizeDelta        = new Vector2(400f, 44f);
                EditorUtility.SetDirty(rtMvpScore.gameObject);
                Debug.Log("[Batch2] ScreenC/MvpScoreText: → mvp score, y=560");
                ok++;
            }

            // MvpAnchorLine: 装饰分隔横线
            // mvp框底部约 top:500px → unity_y=460
            var rtLine = GetRT("Canvas/SurvivalSettlementPanel/ScreenC/MvpAnchorLine");
            if (rtLine)
            {
                rtLine.anchorMin = new Vector2(0.5f, 0.5f);
                rtLine.anchorMax = new Vector2(0.5f, 0.5f);
                rtLine.pivot     = new Vector2(0.5f, 0.5f);
                rtLine.anchoredPosition = new Vector2(0f, 460f);
                rtLine.sizeDelta        = new Vector2(600f, 3f);
                EditorUtility.SetDirty(rtLine.gameObject);
                Debug.Log("[Batch2] ScreenC/MvpAnchorLine: → separator, y=460, 600×3");
                ok++;
            }

            // ── 颁奖台 (Podium) ──
            // pod section: top:580, bottom:960(top)=unity_y=0
            // flex-end对齐: 各slot底部在 unity_y=0

            // Top3Slot_0 (rank1, 金牌, 中间最高)
            // s1 ava:110 + gap:12 + name:34 + score:34 + base:300 ≈ 490px
            // center = -490/2 = -245 from bottom → unity_y = +245, x=0
            var rtSlot0 = GetRT("Canvas/SurvivalSettlementPanel/ScreenC/Top3Slot_0");
            if (rtSlot0)
            {
                rtSlot0.anchorMin = new Vector2(0.5f, 0.5f);
                rtSlot0.anchorMax = new Vector2(0.5f, 0.5f);
                rtSlot0.pivot     = new Vector2(0.5f, 0.5f);
                rtSlot0.anchoredPosition = new Vector2(0f, 245f);
                rtSlot0.sizeDelta        = new Vector2(260f, 490f);
                EditorUtility.SetDirty(rtSlot0.gameObject);
                Debug.Log("[Batch2] ScreenC/Top3Slot_0 (rank1): → center, pos(0,245), 260×490");
                ok++;
            }

            // Top3Slot_1 (rank2, 银牌, 左侧)
            // s2 ava:90 + gap:12 + name:30 + score:32 + base:200 ≈ 364px
            // center = -182 from bottom → unity_y = +182, x=-250
            var rtSlot1 = GetRT("Canvas/SurvivalSettlementPanel/ScreenC/Top3Slot_1");
            if (rtSlot1)
            {
                rtSlot1.anchorMin = new Vector2(0.5f, 0.5f);
                rtSlot1.anchorMax = new Vector2(0.5f, 0.5f);
                rtSlot1.pivot     = new Vector2(0.5f, 0.5f);
                rtSlot1.anchoredPosition = new Vector2(-250f, 182f);
                rtSlot1.sizeDelta        = new Vector2(220f, 364f);
                EditorUtility.SetDirty(rtSlot1.gameObject);
                Debug.Log("[Batch2] ScreenC/Top3Slot_1 (rank2): → left, pos(-250,182), 220×364");
                ok++;
            }

            // Top3Slot_2 (rank3, 铜牌, 右侧)
            // s3 ava:80 + gap:12 + name:30 + score:32 + base:160 ≈ 314px
            // center = -157 from bottom → unity_y = +157, x=+260
            var rtSlot2 = GetRT("Canvas/SurvivalSettlementPanel/ScreenC/Top3Slot_2");
            if (rtSlot2)
            {
                rtSlot2.anchorMin = new Vector2(0.5f, 0.5f);
                rtSlot2.anchorMax = new Vector2(0.5f, 0.5f);
                rtSlot2.pivot     = new Vector2(0.5f, 0.5f);
                rtSlot2.anchoredPosition = new Vector2(260f, 157f);
                rtSlot2.sizeDelta        = new Vector2(200f, 314f);
                EditorUtility.SetDirty(rtSlot2.gameObject);
                Debug.Log("[Batch2] ScreenC/Top3Slot_2 (rank3): → right, pos(260,157), 200×314");
                ok++;
            }
        }

        // ════════════════════════════════════════
        // 4. RestartButton (SurvivalSettlementPanel级, 跨屏共享)
        //    HTML: bottom:160px, 560×110 (主按钮)
        //    top = 1920-160-110 = 1650 → center_y=1705 → unity_y=-745
        // ════════════════════════════════════════
        {
            var rt = GetRT("Canvas/SurvivalSettlementPanel/RestartButton");
            if (rt)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, -745f);
                rt.sizeDelta        = new Vector2(560f, 110f);
                EditorUtility.SetDirty(rt.gameObject);
                Debug.Log("[Batch2] RestartButton: → bottom, y=-745, 560×110");
                ok++;
            }
        }

        // ════════════════════════════════════════
        // 5. VIPAnnouncement — 全屏overlay
        //    HTML: 全屏遮罩+中心内容区920×1100
        //    当前: 900×120 center-anchor → 改为 stretch全屏
        // ════════════════════════════════════════
        {
            var rt = GetRT("Canvas/GameUIPanel/VIPAnnouncement");
            if (rt)
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta        = Vector2.zero;
                EditorUtility.SetDirty(rt.gameObject);
                Debug.Log("[Batch2] VIPAnnouncement: → fullscreen overlay stretch");
                ok++;
            }
        }

        // ════════════════════════════════════════
        // 6. JoinNotification — 底部居中通知条容器
        //    HTML: 3条通知 bottom:20/108/196px, 680×72
        //    容器: 底部居中, 宽700, 高280 (覆盖3条区域)
        //    当前: 右侧锚(1,0.5), 280×80 → 改为底部中心(0.5,0)
        // ════════════════════════════════════════
        {
            var rt = GetRT("Canvas/GameUIPanel/JoinNotification");
            if (rt)
            {
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot     = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 20f);
                rt.sizeDelta        = new Vector2(700f, 280f);
                EditorUtility.SetDirty(rt.gameObject);
                Debug.Log("[Batch2] JoinNotification: → bottom-center, 700×280, y=20 from bottom");
                ok++;
            }
        }

        // ════════════════════════════════════════
        // 7. GiftNotification — 全宽礼物通知横幅
        //    HTML: 全宽横幅 height:110~165px (各tier不同)
        //    位置: 游戏内容区偏下, 高度取中值140
        //    当前: 右侧锚(1,0.5), 320×120 → 全宽水平stretch
        // ════════════════════════════════════════
        {
            var rt = GetRT("Canvas/GameUIPanel/GiftNotification");
            if (rt)
            {
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(1f, 0.5f);
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, -300f);  // 300px below center (mid-lower screen)
                rt.sizeDelta        = new Vector2(0f, 140f);   // full-width, 140px tall
                EditorUtility.SetDirty(rt.gameObject);
                Debug.Log("[Batch2] GiftNotification: → horizontal stretch, y=-300, h=140");
                ok++;
            }
        }

        // ════════════════════════════════════════
        // 8. BottomBar (GM调试控制栏)
        //    HTML: bottom:0, height:280px
        //    当前: anchor(0,0)→(1,0), pivot(0.5,0), h=120 → 改为280
        // ════════════════════════════════════════
        {
            var rt = GetRT("Canvas/BottomBar");
            if (rt)
            {
                // anchorMin/Max和pivot已是底部拉伸(正确), 只改高度
                rt.sizeDelta = new Vector2(0f, 280f);
                EditorUtility.SetDirty(rt.gameObject);
                Debug.Log("[Batch2] BottomBar: height 120→280");
                ok++;
            }
        }

        // 保存
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log($"[ApplyHtmlLayout_Batch2] ✅ Batch2 完成，共修改 {ok} 个元素");
    }

    static RectTransform GetRT(string path)
    {
        var all = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in all)
        {
            if (BuildPath(t) == path)
                return t.GetComponent<RectTransform>();
        }
        Debug.LogWarning($"[Batch2] 未找到: {path}");
        return null;
    }

    static string BuildPath(Transform t)
    {
        if (t.parent == null) return t.name;
        return BuildPath(t.parent) + "/" + t.name;
    }
}
