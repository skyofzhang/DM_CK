using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DrscfZ.EditorTools
{
    /// <summary>
    /// audit-r4 场景综合修复（只做一次，幂等）：
    ///   1. OrangeOverlay / OrangeIcon SetActive(false)（audit-r3 残留 P2）
    ///   2. SurvivalSettlementPanel._top3Slots[2] 未绑定 → 自动补 Slot3（复制 Slot2 结构）
    ///   3. 挂载 SeasonTopBarUI / SeasonSettlementUI（audit-r4 新建 UI）
    /// </summary>
    public static class AuditR4SceneFixes
    {
        private const string AlibabaFont = "Fonts/AlibabaPuHuiTi-3-85-Bold SDF";
        private const string FallbackFont = "Fonts/ChineseFont SDF";

        [MenuItem("Tools/DrscfZ/Audit-r4 Scene Fixes")]
        public static void Execute()
        {
            int disabled = 0;
            int slotBound = 0;
            int uiCreated = 0;

            // 1. 失活 OrangeOverlay / OrangeIcon
            var allObjects = GameObject.FindObjectsOfType<Transform>(true);
            foreach (var t in allObjects)
            {
                if (t == null) continue;
                string n = t.name;
                if (n == "OrangeOverlay" || n == "OrangeIcon" || n == "OrangeSpeedHUD")
                {
                    if (t.gameObject.activeSelf)
                    {
                        t.gameObject.SetActive(false);
                        EditorUtility.SetDirty(t.gameObject);
                        disabled++;
                        Debug.Log($"[AuditR4SceneFixes] Disabled legacy: {GetPath(t)}");
                    }
                }
            }

            // 2. 补齐 SurvivalSettlementPanel._top3Slots[2]
            var settlementUi = Object.FindObjectOfType<DrscfZ.UI.SurvivalSettlementUI>(true);
            if (settlementUi != null)
            {
                var so = new SerializedObject(settlementUi);
                var slotsProp = so.FindProperty("_top3Slots");
                if (slotsProp != null && slotsProp.isArray && slotsProp.arraySize >= 3)
                {
                    var slot0 = slotsProp.GetArrayElementAtIndex(0).objectReferenceValue as GameObject;
                    var slot1 = slotsProp.GetArrayElementAtIndex(1).objectReferenceValue as GameObject;
                    var slot2 = slotsProp.GetArrayElementAtIndex(2).objectReferenceValue as GameObject;
                    if (slot2 == null && slot1 != null)
                    {
                        // 复制 slot1 的结构作为 slot2
                        var newSlot = Object.Instantiate(slot1, slot1.transform.parent);
                        newSlot.name = "Top3Slot_3";
                        var rt = newSlot.GetComponent<RectTransform>();
                        if (rt != null)
                        {
                            var anchorPos = rt.anchoredPosition;
                            // slot1 的 anchoredPosition 下移同样步长 = slot1.y - slot0.y
                            if (slot0 != null)
                            {
                                var rt0 = slot0.GetComponent<RectTransform>();
                                var rt1 = slot1.GetComponent<RectTransform>();
                                if (rt0 != null && rt1 != null)
                                {
                                    float step = rt1.anchoredPosition.y - rt0.anchoredPosition.y;
                                    rt.anchoredPosition = new Vector2(rt1.anchoredPosition.x, rt1.anchoredPosition.y + step);
                                }
                            }
                        }
                        slotsProp.GetArrayElementAtIndex(2).objectReferenceValue = newSlot;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(settlementUi);
                        slotBound = 1;
                        Debug.Log($"[AuditR4SceneFixes] Created Top3Slot_3 at {newSlot.transform.parent.name}");
                    }
                }
            }

            // 3. 挂载 SeasonTopBarUI / SeasonSettlementUI
            //    目标父节点: Canvas/GameUIPanel（常驻挂点）
            var canvases = GameObject.FindObjectsOfType<Canvas>(true);
            Transform gameUIPanel = null;
            Transform canvasRoot = null;
            foreach (var c in canvases)
            {
                if (c == null || c.transform == null) continue;
                if (c.name == "Canvas" && c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    canvasRoot = c.transform;
                    var child = c.transform.Find("GameUIPanel");
                    if (child != null) gameUIPanel = child;
                }
            }

            if (gameUIPanel != null)
            {
                if (gameUIPanel.Find("SeasonTopBar") == null)
                {
                    var go = CreateSeasonTopBar(gameUIPanel);
                    uiCreated++;
                    Debug.Log($"[AuditR4SceneFixes] Created SeasonTopBar at {go.name}");
                }
                if (gameUIPanel.Find("SeasonSettlementPanel") == null)
                {
                    var go = CreateSeasonSettlement(gameUIPanel);
                    uiCreated++;
                    Debug.Log($"[AuditR4SceneFixes] Created SeasonSettlementPanel at {go.name}");
                }
            }
            else
            {
                Debug.LogWarning("[AuditR4SceneFixes] Canvas/GameUIPanel not found — skip SeasonUI creation");
            }

            // 保存场景
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isDirty)
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[AuditR4SceneFixes] Scene saved: {scene.path}");
            }

            Debug.Log($"[AuditR4SceneFixes] DONE — disabled={disabled} slotBound={slotBound} uiCreated={uiCreated}");
        }

        private static GameObject CreateSeasonTopBar(Transform parent)
        {
            var go = new GameObject("SeasonTopBar", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.0f, 1.0f);
            rt.anchorMax = new Vector2(0.5f, 1.0f);
            rt.pivot = new Vector2(0.0f, 1.0f);
            rt.anchoredPosition = new Vector2(20, -10);
            rt.sizeDelta = new Vector2(0, 60);

            // 背景（浅蓝半透明）
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.2f, 0.4f, 0.5f);

            var ui = go.AddComponent<DrscfZ.UI.SeasonTopBarUI>();

            // 3 行文本：Season / Theme / FortressDay
            var lblSeason      = CreateTmpLabel("LblSeason",      go.transform, new Vector2(0.0f, 0.66f), new Vector2(1.0f, 1.0f), "S1 · D1/7");
            var lblTheme       = CreateTmpLabel("LblTheme",       go.transform, new Vector2(0.0f, 0.33f), new Vector2(1.0f, 0.66f), "主题：经典冰原");
            var lblFortressDay = CreateTmpLabel("LblFortressDay", go.transform, new Vector2(0.0f, 0.0f),  new Vector2(1.0f, 0.33f), "堡垒日 D1（最高 D1）");

            // 绑定 SerializedField
            var so = new SerializedObject(ui);
            so.FindProperty("_panel").objectReferenceValue = go;
            so.FindProperty("_lblSeason").objectReferenceValue = lblSeason;
            so.FindProperty("_lblTheme").objectReferenceValue = lblTheme;
            so.FindProperty("_lblFortressDay").objectReferenceValue = lblFortressDay;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);
            EditorUtility.SetDirty(go);
            return go;
        }

        private static GameObject CreateSeasonSettlement(Transform parent)
        {
            var go = new GameObject("SeasonSettlementPanel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.2f, 0.15f);
            rt.anchorMax = new Vector2(0.8f, 0.85f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            // 背景
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.12f, 0.95f);

            var ui = go.AddComponent<DrscfZ.UI.SeasonSettlementUI>();

            // 子节点：标题 / 幸存 / TopList / CloseBtn
            var lblTitle = CreateTmpLabel("LblTitle",           go.transform, new Vector2(0.0f, 0.80f), new Vector2(1.0f, 1.0f),  "S1 赛季结束\n下赛季主题预告：血月", 30);
            var lblSurv  = CreateTmpLabel("LblSurvivingRooms",  go.transform, new Vector2(0.0f, 0.70f), new Vector2(1.0f, 0.80f), "全服幸存房间：- 间",       22);
            var lblTop   = CreateTmpLabel("LblTopList",         go.transform, new Vector2(0.0f, 0.15f), new Vector2(1.0f, 0.70f), "(等待数据)",             18);

            // 关闭按钮
            var btnGo = new GameObject("BtnClose", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(go.transform, false);
            var brt = btnGo.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.4f, 0.02f);
            brt.anchorMax = new Vector2(0.6f, 0.12f);
            brt.anchoredPosition = Vector2.zero;
            brt.sizeDelta = Vector2.zero;
            btnGo.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.3f, 0.95f);
            var btnLabel = CreateTmpLabel("Label", btnGo.transform, new Vector2(0.0f, 0.0f), new Vector2(1.0f, 1.0f), "关闭", 22);
            btnLabel.alignment = TextAlignmentOptions.Center;
            var btn = btnGo.GetComponent<Button>();

            // 绑定
            var so = new SerializedObject(ui);
            so.FindProperty("_panel").objectReferenceValue = go;
            so.FindProperty("_lblTitle").objectReferenceValue = lblTitle;
            so.FindProperty("_lblSurvivingRooms").objectReferenceValue = lblSurv;
            so.FindProperty("_lblTopList").objectReferenceValue = lblTop;
            so.FindProperty("_btnClose").objectReferenceValue = btn;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 默认隐藏
            go.SetActive(false);
            EditorUtility.SetDirty(ui);
            EditorUtility.SetDirty(go);
            return go;
        }

        private static TextMeshProUGUI CreateTmpLabel(string name, Transform parent, Vector2 amin, Vector2 amax, string text, int fontSize = 22)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = amin;
            rt.anchorMax = amax;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Left;
            var font = Resources.Load<TMP_FontAsset>(AlibabaFont) ?? Resources.Load<TMP_FontAsset>(FallbackFont);
            if (font != null) tmp.font = font;
            // 显式写 m_fontColor 避免 faceColor 白色陷阱
            var tmpSo = new SerializedObject(tmp);
            var cProp = tmpSo.FindProperty("m_fontColor");
            if (cProp != null) cProp.colorValue = Color.white;
            var c32Prop = tmpSo.FindProperty("m_fontColor32");
            if (c32Prop != null) { /* color32 written via color */ }
            tmpSo.ApplyModifiedPropertiesWithoutUndo();
            return tmp;
        }

        private static string GetPath(Transform t)
        {
            if (t == null) return "(null)";
            var sb = new System.Text.StringBuilder();
            sb.Append(t.name);
            while (t.parent != null)
            {
                t = t.parent;
                sb.Insert(0, t.name + "/");
            }
            return sb.ToString();
        }
    }
}
