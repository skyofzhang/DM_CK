using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 为 SurvivalRankingPanel 添加 UI 底图、关闭按钮、全屏遮罩（阻止大厅按钮点击）。
/// Tools -> DrscfZ -> Setup Ranking Panel UI
/// </summary>
[InitializeOnLoad]
public static class SetupRankingPanelUI
{
    [MenuItem("Tools/DrscfZ/Setup Ranking Panel UI")]
    public static void Execute()
    {
        const string rootDir = "Assets/Art/UI/Rankings/";
        const string closePath = "Assets/Art/UI/Settings/btn_close.png";

        // ── 1. 导入为 Sprite ──────────────────────────────────────────────
        string[] spritePaths = {
            rootDir + "ranking_panel_bg.png",
            rootDir + "ranking_row_bg.png",
            rootDir + "ranking_row_top1_bg.png",
            rootDir + "ranking_row_top2_bg.png",
            rootDir + "ranking_row_top3_bg.png",
            rootDir + "overlay_mask.png",
            closePath,
        };
        foreach (var p in spritePaths) EnsureSprite(p);
        AssetDatabase.Refresh();

        var panelBgSpr = Load(rootDir + "ranking_panel_bg.png");
        var rowBgSpr   = Load(rootDir + "ranking_row_bg.png");
        var top1Spr    = Load(rootDir + "ranking_row_top1_bg.png");
        var top2Spr    = Load(rootDir + "ranking_row_top2_bg.png");
        var top3Spr    = Load(rootDir + "ranking_row_top3_bg.png");
        var overlayBg  = Load(rootDir + "overlay_mask.png");
        var closeSpr   = Load(closePath);

        if (panelBgSpr == null) { Debug.LogError("[SetupRankingPanelUI] ranking_panel_bg.png 未找到"); return; }

        // ── 2. 找 SurvivalRankingPanel ────────────────────────────────────
        GameObject panel = null;
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go.name == "SurvivalRankingPanel" && go.scene.name == "MainScene") { panel = go; break; }
        if (panel == null) { Debug.LogError("[SetupRankingPanelUI] SurvivalRankingPanel 未找到"); return; }

        // ── 3. 面板底图（SurvivalRankingPanel 自身 Image） ──────────────
        var panelImg = panel.GetComponent<Image>();
        if (panelImg == null) panelImg = panel.AddComponent<Image>();
        panelImg.sprite         = panelBgSpr;
        panelImg.color          = Color.white;
        panelImg.type           = Image.Type.Sliced;
        panelImg.raycastTarget  = true;  // 拦截点击，防穿透
        EditorUtility.SetDirty(panelImg);
        Debug.Log("[SetupRankingPanelUI] 面板底图已设置");

        // 确保面板 RectTransform 有固定尺寸（居中）
        var panelRt = panel.GetComponent<RectTransform>();
        if (panelRt != null)
        {
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot     = new Vector2(0.5f, 0.5f);
            // 保持现有尺寸，只在 sizeDelta 为 0 时设默认值
            if (panelRt.sizeDelta.x < 100f)
                panelRt.sizeDelta = new Vector2(640f, 760f);
        }

        // ── 4. 关闭按钮 ───────────────────────────────────────────────────
        var closeBtnT = panel.transform.Find("CloseBtn");
        if (closeBtnT != null && closeSpr != null)
        {
            var cImg = closeBtnT.GetComponent<Image>();
            if (cImg == null) cImg = closeBtnT.gameObject.AddComponent<Image>();
            cImg.sprite         = closeSpr;
            cImg.color          = Color.white;
            cImg.type           = Image.Type.Simple;
            cImg.preserveAspect = true;
            EditorUtility.SetDirty(cImg);

            var btn = closeBtnT.GetComponent<Button>();
            if (btn != null)
            {
                var cb = btn.colors;
                cb.normalColor      = Color.white;
                cb.highlightedColor = new Color(1f, 0.85f, 0.85f, 1f);
                cb.pressedColor     = new Color(0.7f, 0.1f, 0.1f, 1f);
                btn.colors = cb;
                EditorUtility.SetDirty(btn);
            }

            // 位置：右上角
            var cRt = closeBtnT.GetComponent<RectTransform>();
            if (cRt != null)
            {
                cRt.anchorMin = new Vector2(1f, 1f);
                cRt.anchorMax = new Vector2(1f, 1f);
                cRt.pivot     = new Vector2(1f, 1f);
                cRt.anchoredPosition = new Vector2(-12f, -12f);
                cRt.sizeDelta = new Vector2(56f, 56f);
            }
            Debug.Log("[SetupRankingPanelUI] CloseBtn 已设置");
        }

        // ── 5. 排名行底图 ─────────────────────────────────────────────────
        var rowContainer = panel.transform.Find("RowContainer");
        if (rowContainer != null && rowBgSpr != null)
        {
            Sprite[] topSprs = { top1Spr, top2Spr, top3Spr };
            int rowIdx = 0;
            foreach (Transform row in rowContainer)
            {
                Sprite spr = (rowIdx < 3 && topSprs[rowIdx] != null) ? topSprs[rowIdx] : rowBgSpr;
                var rImg = row.GetComponent<Image>();
                if (rImg == null) rImg = row.gameObject.AddComponent<Image>();
                rImg.sprite = spr;
                rImg.color  = Color.white;
                rImg.type   = Image.Type.Sliced;
                rImg.raycastTarget = false;
                EditorUtility.SetDirty(rImg);
                rowIdx++;
            }
            Debug.Log($"[SetupRankingPanelUI] {rowIdx} 个排名行底图已设置");
        }

        // ── 6. 全屏遮罩（阻挡主界面点击）──────────────────────────────────
        // 遮罩挂在 Canvas 下，位于 SurvivalRankingPanel 之前（同级别 sibling）
        GameObject canvasGO = panel.transform.parent.gameObject;

        GameObject overlay = null;
        var existingOverlay = canvasGO.transform.Find("RankingOverlay");
        if (existingOverlay != null)
        {
            overlay = existingOverlay.gameObject;
            Debug.Log("[SetupRankingPanelUI] RankingOverlay 已存在，复用");
        }
        else
        {
            overlay = new GameObject("RankingOverlay");
            overlay.AddComponent<RectTransform>();
            overlay.transform.SetParent(canvasGO.transform, false);
            Debug.Log("[SetupRankingPanelUI] RankingOverlay 已创建");
        }

        // 全屏铺满
        var oRt = overlay.GetComponent<RectTransform>();
        oRt.anchorMin = Vector2.zero;
        oRt.anchorMax = Vector2.one;
        oRt.offsetMin = Vector2.zero;
        oRt.offsetMax = Vector2.zero;

        var oImg = overlay.GetComponent<Image>();
        if (oImg == null) oImg = overlay.AddComponent<Image>();
        if (overlayBg != null)
        {
            oImg.sprite = overlayBg;
            oImg.type   = Image.Type.Sliced;
        }
        oImg.color         = new Color(0f, 0f, 0f, 0.55f);
        oImg.raycastTarget = true;   // 关键：阻断下方所有点击

        // 让遮罩成为 SurvivalRankingPanel 的直接前一个 sibling
        int panelIdx = panel.transform.GetSiblingIndex();
        overlay.transform.SetSiblingIndex(panelIdx);     // overlay 在 panel 前面
        panel.transform.SetSiblingIndex(panelIdx + 1);   // panel 在 overlay 后面（渲染在上）

        // 遮罩默认隐藏，由 SurvivalRankingOverlay 脚本控制
        overlay.SetActive(false);
        EditorUtility.SetDirty(overlay);
        Debug.Log("[SetupRankingPanelUI] RankingOverlay 全屏遮罩已设置");

        // ── 7. 绑定遮罩到 SurvivalRankingUI ──────────────────────────────
        // 注入 _overlay 字段（通过 SerializedObject）
        var canvas = canvasGO;
        var rankingUI = canvas.GetComponent<DrscfZ.UI.SurvivalRankingUI>();
        if (rankingUI != null)
        {
            var so = new SerializedObject(rankingUI);
            var overlayProp = so.FindProperty("_overlay");
            if (overlayProp != null)
            {
                overlayProp.objectReferenceValue = overlay;
                so.ApplyModifiedProperties();
                Debug.Log("[SetupRankingPanelUI] SurvivalRankingUI._overlay 已绑定");
            }
            else
            {
                Debug.LogWarning("[SetupRankingPanelUI] SurvivalRankingUI 没有 _overlay 字段，需手动在脚本中添加");
            }
        }

        // ── 8. 保存场景 ───────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(panel.scene);
        EditorSceneManager.SaveScene(panel.scene);
        Debug.Log("[SetupRankingPanelUI] 完成，场景已保存。");
    }

    static void EnsureSprite(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;
        bool changed = false;
        if (imp.textureType != TextureImporterType.Sprite)
        {
            imp.textureType      = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled    = false;
            changed = true;
        }
        if (changed) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }

    static Sprite Load(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
}
