using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using DrscfZ.UI;

/// <summary>
/// 为 SurvivalSettingsPanel 添加 SurvivalSettingsUI 组件并绑定所有 Inspector 字段。
/// 同时绑定 SurvivalIdleUI._settingsPanel 引用。
/// </summary>
public class Temp_FixSettingsPanel
{
    public static void Execute()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== Fix SurvivalSettingsPanel ===");

        Transform FindT(string path)
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
                if (match) return t;
            }
            return null;
        }

        var panelT = FindT("Canvas/SurvivalSettingsPanel");
        if (panelT == null) { Debug.LogError("[FixSettingsPanel] SurvivalSettingsPanel 未找到"); return; }

        var panelGo = panelT.gameObject;
        var settingsUI = panelGo.GetComponent<SurvivalSettingsUI>();
        if (settingsUI == null)
        {
            settingsUI = panelGo.AddComponent<SurvivalSettingsUI>();
            sb.AppendLine("  ✅ SurvivalSettingsUI 组件已添加");
        }
        else
        {
            sb.AppendLine("  ℹ SurvivalSettingsUI 组件已存在，仅更新字段");
        }

        var so = new SerializedObject(settingsUI);
        so.Update();

        void Bind(string fieldName, string path, System.Func<Transform, Object> getter)
        {
            var prop = so.FindProperty(fieldName);
            if (prop == null) { sb.AppendLine($"  ⚠ 字段 {fieldName} 未找到"); return; }
            var t = FindT(path);
            if (t == null) { sb.AppendLine($"  ⚠ 路径 {path} 未找到"); return; }
            var obj = getter(t);
            if (obj == null) { sb.AppendLine($"  ⚠ {path} 无所需组件"); return; }
            prop.objectReferenceValue = obj;
            sb.AppendLine($"  ✅ {fieldName} → {path}");
        }

        // _panel
        Bind("_panel",         "Canvas/SurvivalSettingsPanel",                          t => t.gameObject);
        // _closeBtn
        Bind("_closeBtn",      "Canvas/SurvivalSettingsPanel/CloseBtn",                 t => t.GetComponent<Button>());
        // BGM
        Bind("_bgmSlider",     "Canvas/SurvivalSettingsPanel/BGMRow/BGMSlider",         t => t.GetComponent<Slider>());
        Bind("_bgmValueText",  "Canvas/SurvivalSettingsPanel/BGMRow/BGMValueText",      t => t.GetComponent<TMP_Text>());
        Bind("_bgmToggle",     "Canvas/SurvivalSettingsPanel/BGMRow/BGMToggleBtn",      t => t.GetComponent<Button>());
        Bind("_bgmToggleText", "Canvas/SurvivalSettingsPanel/BGMRow/BGMToggleBtn/BGMToggleText", t => t.GetComponent<TMP_Text>());
        // SFX
        Bind("_sfxSlider",     "Canvas/SurvivalSettingsPanel/SFXRow/SFXSlider",         t => t.GetComponent<Slider>());
        Bind("_sfxValueText",  "Canvas/SurvivalSettingsPanel/SFXRow/SFXValueText",      t => t.GetComponent<TMP_Text>());
        Bind("_sfxToggle",     "Canvas/SurvivalSettingsPanel/SFXRow/SFXToggleBtn",      t => t.GetComponent<Button>());
        Bind("_sfxToggleText", "Canvas/SurvivalSettingsPanel/SFXRow/SFXToggleBtn/SFXToggleText", t => t.GetComponent<TMP_Text>());
        // Version / Video Toggles
        Bind("_versionText",      "Canvas/SurvivalSettingsPanel/VersionText",                t => t.GetComponent<TMP_Text>());
        Bind("_giftVideoToggle",  "Canvas/SurvivalSettingsPanel/GiftVideoRow/Toggle",         t => t.GetComponent<Toggle>());
        Bind("_vipVideoToggle",   "Canvas/SurvivalSettingsPanel/VIPVideoRow/Toggle",           t => t.GetComponent<Toggle>());

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(panelGo);
        sb.AppendLine("  ✅ 所有字段绑定完成");

        // ── 绑定 SurvivalIdleUI._settingsPanel ───────────────────────────
        var idleUI = Object.FindObjectOfType<SurvivalIdleUI>(true);
        if (idleUI != null)
        {
            var soIdle = new SerializedObject(idleUI);
            soIdle.Update();
            var pSettings = soIdle.FindProperty("_settingsPanel");
            if (pSettings != null)
            {
                pSettings.objectReferenceValue = settingsUI;
                soIdle.ApplyModifiedProperties();
                EditorUtility.SetDirty(idleUI);
                sb.AppendLine("  ✅ SurvivalIdleUI._settingsPanel 已绑定");
            }
            else sb.AppendLine("  ⚠ SurvivalIdleUI._settingsPanel 字段未找到");
        }
        else sb.AppendLine("  ⚠ SurvivalIdleUI 未找到");

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        sb.AppendLine("✅ Scene saved");
        Debug.Log("[FixSettingsPanel]\n" + sb.ToString());
    }
}
