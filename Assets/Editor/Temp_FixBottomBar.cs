using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel 14 修复：BottomBar 按钮重构为 HTML14 布局
///
/// BottomBar: pivot=(0.5,0) bottom-stretch, height=280
/// 子按钮坐标系（center anchor）: anchoredPosition相对于BottomBar中心(0,140)
///   canvas_y → local_y = canvas_y + 960 → anchoredPosition.y = local_y - 140
///   Row1 canvas=-776 → ap.y=44
///   Row2a canvas=-850 → ap.y=-30
///   Row2b canvas=-926 → ap.y=-106
///
/// HTML14 Row1: ▶开始(200) ⏸暂停(200) ⏹结束(200) 🔄重置(160)
/// HTML14 Row2a: T1礼物(220) T3礼物(220) T5礼物(220) 冻结(200)
/// HTML14 Row2b: 怪物(200) —flex-wrap换行
/// </summary>
public class Temp_FixBottomBar
{
    public static void Execute()
    {
        RectTransform FindRT(string path)
        {
            var parts = path.Split('/');
            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t.name != parts[parts.Length - 1]) continue;
                bool match = true;
                Transform cur = t;
                for (int i = parts.Length - 1; i >= 0; i--)
                {
                    if (cur == null || cur.name != parts[i]) { match = false; break; }
                    cur = cur.parent;
                }
                if (match) return t as RectTransform;
            }
            return null;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== BottomBar 重构 ===");

        var bottomBar = FindRT("Canvas/BottomBar");
        if (bottomBar == null) { Debug.LogError("[FixBottomBar] BottomBar not found"); return; }

        // ── 辅助：创建按钮 ──────────────────────────────────────────────────────
        Button CreateBtn(string name, string label, float ax, float ay, float w, float h, Color bgColor, Color textColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(bottomBar, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(ax, ay);
            rt.sizeDelta = new Vector2(w, h);

            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var btn = go.AddComponent<Button>();

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRT = textGo.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = textRT.offsetMax = Vector2.zero;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 24;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        // ── 辅助：重新定位现有按钮 ─────────────────────────────────────────────
        void MoveBtn(RectTransform rt, float ax, float ay, float w, float h, Color bgColor, string label)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(ax, ay);
            rt.sizeDelta = new Vector2(w, h);
            var img = rt.GetComponent<Image>();
            if (img) img.color = bgColor;
            var tmp = rt.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) { tmp.text = label; tmp.fontSize = 24; }
            else { var t2 = rt.GetComponentInChildren<Text>(); if (t2) t2.text = label; }
            EditorUtility.SetDirty(rt);
        }

        // ── 1. 隐藏已有的 BtnConnect 和 BtnSimulate（不删除，代码仍引用）─────
        var btnConnect = FindRT("Canvas/BottomBar/BtnConnect");
        if (btnConnect != null)
        {
            btnConnect.anchoredPosition = new Vector2(0f, -200f); // 移到panel外
            btnConnect.sizeDelta = new Vector2(1f, 1f);           // 极小
            var img = btnConnect.GetComponent<Image>();
            if (img) img.color = new Color(0,0,0,0);             // 透明
            EditorUtility.SetDirty(btnConnect);
            sb.AppendLine("  BtnConnect → 已隐藏（保留引用）");
        }
        var btnSim = FindRT("Canvas/BottomBar/BtnSimulate");
        if (btnSim != null)
        {
            btnSim.anchoredPosition = new Vector2(0f, -210f);
            btnSim.sizeDelta = new Vector2(1f, 1f);
            var img = btnSim.GetComponent<Image>();
            if (img) img.color = new Color(0,0,0,0);
            EditorUtility.SetDirty(btnSim);
            sb.AppendLine("  BtnSimulate → 已隐藏（保留引用）");
        }

        // ── 2. 删除 BtnPlayerData（代码中无引用）──────────────────────────────
        var btnPD = FindRT("Canvas/BottomBar/BtnPlayerData");
        if (btnPD != null)
        {
            Object.DestroyImmediate(btnPD.gameObject);
            sb.AppendLine("  BtnPlayerData → 已删除");
        }

        // ── 3. 重定位现有 BtnStart / BtnReset ────────────────────────────────
        // Row1 y = 44; HTML14: 开始x=-410(200), 暂停x=-194(200), 结束x=22(200), 重置x=218(160)
        var btnStart = FindRT("Canvas/BottomBar/BtnStart");
        if (btnStart != null)
        {
            MoveBtn(btnStart, -410f, 44f, 200f, 60f,
                new Color(0.09f, 0.40f, 0.20f, 1f), "▶ 开始游戏");
            sb.AppendLine("  BtnStart → pos(-410,44) 200×60 「▶ 开始游戏」");
        }
        var btnReset = FindRT("Canvas/BottomBar/BtnReset");
        if (btnReset != null)
        {
            MoveBtn(btnReset, 218f, 44f, 160f, 60f,
                new Color(0.12f, 0.23f, 0.37f, 1f), "🔄 重置");
            sb.AppendLine("  BtnReset → pos(218,44) 160×60 「🔄 重置」");
        }

        // ── 4. 新增 Row1 按钮 ─────────────────────────────────────────────────
        if (FindRT("Canvas/BottomBar/BtnPause") == null)
        {
            CreateBtn("BtnPause", "⏸ 暂停游戏", -194f, 44f, 200f, 60f,
                new Color(0.44f, 0.25f, 0.07f, 1f), new Color(0.99f, 0.83f, 0.27f));
            sb.AppendLine("  BtnPause 新建: pos(-194,44) 200×60");
        }
        if (FindRT("Canvas/BottomBar/BtnEnd") == null)
        {
            CreateBtn("BtnEnd", "⏹ 结束游戏", 22f, 44f, 200f, 60f,
                new Color(0.50f, 0.11f, 0.11f, 1f), new Color(0.99f, 0.64f, 0.64f));
            sb.AppendLine("  BtnEnd 新建: pos(22,44) 200×60");
        }

        // ── 5. 新增 Row2a 礼物/冻结按钮 ─────────────────────────────────────
        // Row2a y = -30; T1x=-400(220), T3x=-164(220), T5x=72(220), 冻结x=298(200)
        if (FindRT("Canvas/BottomBar/BtnGiftT1") == null)
        {
            CreateBtn("BtnGiftT1", "T1 礼物", -400f, -30f, 220f, 60f,
                new Color(0.12f, 0.23f, 0.37f, 1f), new Color(0.58f, 0.77f, 0.99f));
            sb.AppendLine("  BtnGiftT1 新建: pos(-400,-30) 220×60");
        }
        if (FindRT("Canvas/BottomBar/BtnGiftT3") == null)
        {
            CreateBtn("BtnGiftT3", "T3 礼物", -164f, -30f, 220f, 60f,
                new Color(0.12f, 0.23f, 0.37f, 1f), new Color(0.99f, 0.83f, 0.27f));
            sb.AppendLine("  BtnGiftT3 新建: pos(-164,-30) 220×60");
        }
        if (FindRT("Canvas/BottomBar/BtnGiftT5") == null)
        {
            CreateBtn("BtnGiftT5", "T5 礼物", 72f, -30f, 220f, 60f,
                new Color(0.27f, 0.10f, 0.01f, 1f), new Color(0.96f, 0.62f, 0.07f));
            sb.AppendLine("  BtnGiftT5 新建: pos(72,-30) 220×60");
        }
        if (FindRT("Canvas/BottomBar/BtnFreeze") == null)
        {
            CreateBtn("BtnFreeze", "❄ 冻结", 298f, -30f, 200f, 60f,
                new Color(0.12f, 0.23f, 0.37f, 1f), new Color(0.49f, 0.78f, 0.89f));
            sb.AppendLine("  BtnFreeze 新建: pos(298,-30) 200×60");
        }

        // ── 6. 新增 Row2b 怪物按钮（flex-wrap换行，左对齐）─────────────────
        // Row2b y = -106; x=-410(200)
        if (FindRT("Canvas/BottomBar/BtnMonster") == null)
        {
            CreateBtn("BtnMonster", "💥 怪物", -410f, -106f, 200f, 60f,
                new Color(0.27f, 0.04f, 0.04f, 1f), new Color(0.99f, 0.64f, 0.64f));
            sb.AppendLine("  BtnMonster 新建: pos(-410,-106) 200×60");
        }

        // ── 7. 将新按钮绑定到 GameControlUI Inspector 引用 ─────────────────
        var gcuiGo = bottomBar.GetComponent<DrscfZ.UI.GameControlUI>();
        if (gcuiGo != null)
        {
            var so = new SerializedObject(gcuiGo);
            so.Update();

            void AssignBtn(string fieldName, string path)
            {
                var rt2 = FindRT(path);
                if (rt2 == null) return;
                var btn = rt2.GetComponent<Button>();
                if (btn == null) return;
                var prop = so.FindProperty(fieldName);
                if (prop != null) prop.objectReferenceValue = btn;
            }

            AssignBtn("pauseButton",   "Canvas/BottomBar/BtnPause");
            AssignBtn("endButton",     "Canvas/BottomBar/BtnEnd");
            AssignBtn("giftT1Button",  "Canvas/BottomBar/BtnGiftT1");
            AssignBtn("giftT3Button",  "Canvas/BottomBar/BtnGiftT3");
            AssignBtn("giftT5Button",  "Canvas/BottomBar/BtnGiftT5");
            AssignBtn("freezeButton",  "Canvas/BottomBar/BtnFreeze");
            AssignBtn("monsterButton", "Canvas/BottomBar/BtnMonster");

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(gcuiGo);
            sb.AppendLine("✅ GameControlUI Inspector 引用已绑定");
        }
        else sb.AppendLine("⚠ GameControlUI 组件未找到，跳过引用绑定");

        EditorUtility.SetDirty(bottomBar);
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        sb.AppendLine("✅ Scene saved");
        Debug.Log("[FixBottomBar]\n" + sb.ToString());
    }
}
