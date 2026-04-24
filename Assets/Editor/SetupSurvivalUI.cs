using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrscfZ.UI;
using DrscfZ.Survival;

namespace DrscfZ.Editor
{
    /// <summary>
    /// 生存UI场景结构自动化建立工具
    ///
    /// 菜单：Tools → DrscfZ → Setup Survival UI
    ///
    /// 功能：
    ///   1. 在 Canvas 下创建 LiveRankingPanel（右侧实时榜单，5行预创建）
    ///   2. 将 SurvivalCameraController 挂载到 Main Camera
    ///   3. 将 SurvivalLiveRankingUI 挂载到 Canvas（if not exist）
    ///   4. 保存场景
    ///
    /// 注意：
    ///   - 不使用 DisplayDialog（会阻塞 Coplay 进程）
    ///   - 若对象已存在则跳过，不重复创建
    ///   - 所有新建对象默认 inactive（Rule #2）
    /// </summary>
    public static class SetupSurvivalUI
    {
        [MenuItem("Tools/DrscfZ/Setup Survival UI (LiveRanking + Camera)")]
        public static void Execute()
        {
            int created = 0;

            // ── 1. 找到 Canvas ──────────────────────────────────────────────────
            var canvasGo = GameObject.Find("Canvas");
            if (canvasGo == null)
            {
                Debug.LogError("[SetupSurvivalUI] 未找到 Canvas，请确认场景中存在 Canvas 对象");
                return;
            }

            // ── 2. 在 Canvas/GameUIPanel 下创建 LiveRankingPanel ────────────────
            var gameUIPanel = canvasGo.transform.Find("GameUIPanel");
            Transform rankingParent = gameUIPanel != null ? gameUIPanel : canvasGo.transform;

            if (rankingParent.Find("LiveRankingPanel") == null)
            {
                var liveRankPanel = CreateLiveRankingPanel(rankingParent);
                created++;
                Debug.Log("[SetupSurvivalUI] 创建 LiveRankingPanel ✅");
            }
            else
            {
                Debug.Log("[SetupSurvivalUI] LiveRankingPanel 已存在，跳过");
            }

            // ── 3. SurvivalLiveRankingUI 挂载到 Canvas ──────────────────────────
            if (canvasGo.GetComponent<SurvivalLiveRankingUI>() == null)
            {
                var liveRankingUI = canvasGo.AddComponent<SurvivalLiveRankingUI>();
                // 自动绑定 _panel 引用
                var livePanel = rankingParent.Find("LiveRankingPanel");
                if (livePanel != null)
                {
                    // 通过 SerializedObject 设置 [SerializeField] 字段
                    var so = new SerializedObject(liveRankingUI);
                    so.FindProperty("_panel").objectReferenceValue = livePanel.gameObject;

                    // 绑定 5 行
                    var rowsArr = so.FindProperty("_rankRows");
                    var rowContainer = livePanel.Find("RowContainer");
                    if (rowContainer != null && rowContainer.childCount >= 5)
                    {
                        for (int i = 0; i < 5; i++)
                            rowsArr.GetArrayElementAtIndex(i).objectReferenceValue = rowContainer.GetChild(i).gameObject;
                    }

                    // 绑定标题
                    var titleTmp = livePanel.Find("Title")?.GetComponent<TMP_Text>();
                    if (titleTmp != null)
                        so.FindProperty("_titleText").objectReferenceValue = titleTmp;

                    // 绑定 RankingSystem
                    var rankSys = Object.FindObjectOfType<DrscfZ.Systems.RankingSystem>();
                    if (rankSys != null)
                        so.FindProperty("_rankingSystem").objectReferenceValue = rankSys;

                    so.ApplyModifiedProperties();
                }

                EditorUtility.SetDirty(canvasGo);
                created++;
                Debug.Log("[SetupSurvivalUI] SurvivalLiveRankingUI 挂载到 Canvas ✅");
            }
            else
            {
                Debug.Log("[SetupSurvivalUI] SurvivalLiveRankingUI 已存在，跳过");
            }

            // ── 4. SurvivalCameraController 挂载到 Main Camera ──────────────────
            var mainCam = Camera.main;
            if (mainCam == null)
            {
                Debug.LogWarning("[SetupSurvivalUI] 未找到 Main Camera（Tag=MainCamera），SurvivalCameraController 未挂载");
            }
            else if (mainCam.GetComponent<SurvivalCameraController>() == null)
            {
                mainCam.gameObject.AddComponent<SurvivalCameraController>();
                EditorUtility.SetDirty(mainCam.gameObject);
                created++;
                Debug.Log("[SetupSurvivalUI] SurvivalCameraController 挂载到 Main Camera ✅");
            }
            else
            {
                Debug.Log("[SetupSurvivalUI] SurvivalCameraController 已存在，跳过");
            }

            // ── 5. 保存场景（Rule #8）────────────────────────────────────────────
            if (created > 0)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[SetupSurvivalUI] ✅ 完成！共创建/挂载 {created} 个对象，场景已保存");
            }
            else
            {
                Debug.Log("[SetupSurvivalUI] ✅ 所有对象已存在，无需创建");
            }
        }

        // ==================== 创建 LiveRankingPanel 结构 ====================

        private static GameObject CreateLiveRankingPanel(Transform parent)
        {
            // LiveRankingPanel 根节点（初始 inactive，Rule #2）
            var panel = CreateUIObject("LiveRankingPanel", parent);
            panel.SetActive(false);

            // 布局：右侧竖向条，宽180×300
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 0.5f);
            panelRect.anchorMax = new Vector2(1f, 0.5f);
            panelRect.pivot     = new Vector2(1f, 0.5f);
            panelRect.anchoredPosition = new Vector2(-10f, 0f);
            panelRect.sizeDelta = new Vector2(200f, 280f);

            // 背景
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.65f);

            // 标题
            var titleGo = CreateUIObject("Title", panel.transform);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot     = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, 0f);
            titleRect.sizeDelta = new Vector2(0f, 32f);

            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text      = "守护者榜";
            titleTmp.fontSize  = 18f;
            titleTmp.color     = new Color(1f, 0.85f, 0.3f);
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.fontStyle = FontStyles.Bold;
            ApplyChineseFont(titleTmp);

            // RowContainer（Vertical Layout）
            var containerGo = CreateUIObject("RowContainer", panel.transform);
            var containerRect = containerGo.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0f, 0f);
            containerRect.anchorMax = new Vector2(1f, 1f);
            containerRect.offsetMin = new Vector2(4f, 4f);
            containerRect.offsetMax = new Vector2(-4f, -36f);  // 留出标题高度

            var layout = containerGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing           = 4f;
            layout.childAlignment    = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // 创建 5 行
            for (int i = 0; i < 5; i++)
                CreateRankRow(containerGo.transform, i);

            return panel;
        }

        private static void CreateRankRow(Transform parent, int index)
        {
            var row = CreateUIObject($"RankRow_{index}", parent);
            var rowRect = row.GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0f, 40f);

            // 行背景（半透明深色，交替色）
            var rowBg = row.AddComponent<Image>();
            rowBg.color = index % 2 == 0
                ? new Color(1f, 1f, 1f, 0.05f)
                : new Color(1f, 1f, 1f, 0.10f);

            var hlayout = row.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleLeft;
            hlayout.spacing = 4f;
            hlayout.padding = new RectOffset(4, 4, 2, 2);
            hlayout.childControlWidth = false;
            hlayout.childControlHeight = true;
            hlayout.childForceExpandWidth = false;
            hlayout.childForceExpandHeight = true;

            // 名次
            var rankGo = CreateUIObject("RankText", row.transform);
            var rankRect = rankGo.GetComponent<RectTransform>();
            rankRect.sizeDelta = new Vector2(28f, 0f);
            var rankTmp = rankGo.AddComponent<TextMeshProUGUI>();
            rankTmp.text     = $"#{index + 1}";
            rankTmp.fontSize = 14f;
            rankTmp.color    = GetRankColor(index);
            rankTmp.alignment = TextAlignmentOptions.Center;
            rankTmp.fontStyle = FontStyles.Bold;
            ApplyChineseFont(rankTmp);

            // 昵称
            var nameGo = CreateUIObject("NameText", row.transform);
            var nameRect = nameGo.GetComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(110f, 0f);
            var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            nameTmp.text     = "---";
            nameTmp.fontSize = 14f;
            nameTmp.color    = Color.white;
            nameTmp.alignment = TextAlignmentOptions.Left;
            nameTmp.overflowMode  = TextOverflowModes.Ellipsis;
            ApplyChineseFont(nameTmp);

            // 分数
            var scoreGo = CreateUIObject("ScoreText", row.transform);
            var scoreRect = scoreGo.GetComponent<RectTransform>();
            scoreRect.sizeDelta = new Vector2(46f, 0f);
            var scoreTmp = scoreGo.AddComponent<TextMeshProUGUI>();
            scoreTmp.text     = "0";
            scoreTmp.fontSize = 13f;
            scoreTmp.color    = new Color(1f, 0.85f, 0.3f);
            scoreTmp.alignment = TextAlignmentOptions.Right;
            ApplyChineseFont(scoreTmp);
        }

        // ==================== 工具方法 ====================

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Color GetRankColor(int index)
        {
            return index switch
            {
                0 => new Color(1f, 0.84f, 0f),   // 金色 #1
                1 => new Color(0.75f, 0.75f, 0.75f), // 银色 #2
                2 => new Color(0.8f, 0.5f, 0.2f), // 铜色 #3
                _ => new Color(0.65f, 0.65f, 0.65f)  // 普通灰
            };
        }

        private static void ApplyChineseFont(TextMeshProUGUI tmp)
        {
            var font = Resources.Load<TMP_FontAsset>("Fonts/AlibabaPuHuiTi-3-85-Bold SDF") ?? Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
            if (font != null) tmp.font = font;
        }
    }
}
