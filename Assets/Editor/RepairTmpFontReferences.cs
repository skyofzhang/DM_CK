using System;
using System.Collections;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DrscfZ.Editor
{
    public static class RepairTmpFontReferences
    {
        private const string MainScenePath = "Assets/Scenes/MainScene.unity";
        private const string PrimaryFontPath = "Assets/Resources/Fonts/AlibabaPuHuiTi-3-85-Bold SDF.asset";
        private const string PrimaryMaterialPath = "Assets/Resources/Fonts/AlibabaPuHuiTi-3-85-Bold SDF Material.mat";
        private const string LegacyFontPath = "Assets/Resources/Fonts/ChineseFont SDF.asset";
        private const string LegacyFontName = "ChineseFont SDF";
        private const string AutoRepairSessionKey = "DrscfZ.RepairTmpFontReferences.AutoRepairRan.v4";
        private static readonly bool LogEachRebind = false;

        [InitializeOnLoadMethod]
        private static void ScheduleAutoRepair()
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            EditorApplication.delayCall += AutoRepairLoadedScenesOnce;
        }

        private static void AutoRepairLoadedScenesOnce()
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (SessionState.GetBool(AutoRepairSessionKey, false)) return;
            SessionState.SetBool(AutoRepairSessionKey, true);

            var font = LoadReplacementFont(out var material);
            if (font == null) return;

            int cleared = ClearTmpFallbackMaterialCache();
            int repaired = RepairLoadedScenes(font, material);
            int refreshed = RefreshLoadedTmp();

            if (repaired > 0)
                Debug.LogWarning("[RepairTmpFontReferences] Auto repair changed loaded TMP references but did not auto-save scenes. Use Tools/DrscfZ/TMP/Repair stale TMP font references to save intentionally.");

            Debug.Log($"[RepairTmpFontReferences] Auto repair complete. repaired={repaired}, refreshed={refreshed}, clearedFallbackMaterials={cleared}");
        }

        [MenuItem("Tools/DrscfZ/TMP/Repair stale TMP font references")]
        public static void RepairOpenScene()
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[RepairTmpFontReferences] Skip repair while Unity is in play mode.");
                return;
            }

            if (SceneManager.sceneCount == 0)
            {
                Debug.LogError("[RepairTmpFontReferences] No loaded scene to repair.");
                return;
            }

            var font = LoadReplacementFont(out var material);
            if (font == null) return;

            int cleared = ClearTmpFallbackMaterialCache();
            int repaired = RepairLoadedScenes(font, material);
            int refreshed = RefreshLoadedTmp();

            if (repaired > 0)
                SaveLoadedScenesIfEditing();

            Debug.Log($"[RepairTmpFontReferences] Loaded scenes repaired={repaired}, refreshed={refreshed}, clearedFallbackMaterials={cleared}");
        }

        public static void RepairMainSceneBatch()
        {
            var font = LoadReplacementFont(out var material);
            if (font == null)
            {
                EditorApplication.Exit(1);
                return;
            }

            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            int cleared = ClearTmpFallbackMaterialCache();
            int repaired = RepairScene(scene, font, material);
            int refreshed = RefreshLoadedTmp();

            if (repaired > 0)
                EditorSceneManager.MarkSceneDirty(scene);

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log($"[RepairTmpFontReferences] Batch repaired={repaired}, refreshed={refreshed}, clearedFallbackMaterials={cleared}");
        }

        private static TMP_FontAsset LoadReplacementFont(out Material material)
        {
            material = null;
            var primary = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PrimaryFontPath);
            if (!IsFontAtlasUsable(primary))
            {
                Debug.LogError($"[RepairTmpFontReferences] Primary TMP font asset atlas is not usable: {PrimaryFontPath}");
                return null;
            }

            material = AssetDatabase.LoadAssetAtPath<Material>(PrimaryMaterialPath);
            if (material == null)
                material = GetFontMaterial(primary);

            if (material == null)
            {
                Debug.LogError($"[RepairTmpFontReferences] Replacement TMP material is missing: {PrimaryMaterialPath}");
                return null;
            }

            EnsureFontMaterial(primary, material);
            return primary;
        }

        private static int RepairLoadedScenes(TMP_FontAsset replacementFont, Material replacementMaterial)
        {
            int repaired = 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded) continue;

                repaired += RepairScene(scene, replacementFont, replacementMaterial);
            }

            return repaired;
        }

        private static int RepairScene(Scene scene, TMP_FontAsset replacementFont, Material replacementMaterial)
        {
            int repaired = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                var texts = root.GetComponentsInChildren<TMP_Text>(true);
                foreach (var text in texts)
                {
                    if (text == null) continue;
                    if (!ShouldRebind(text)) continue;

                    RepairText(text, replacementFont, replacementMaterial);
                    repaired++;
                }
            }

            return repaired;
        }

        private static bool ShouldRebind(TMP_Text text)
        {
            if (text == null) return false;
            if (NeedsRepair(text)) return true;
            if (ReferencesAssetPath(text.font, LegacyFontPath)) return true;

            try
            {
                if (ReferencesLegacyMaterial(text.fontSharedMaterial)) return true;

                var materials = text.fontSharedMaterials;
                if (materials != null)
                {
                    foreach (var material in materials)
                    {
                        if (ReferencesLegacyMaterial(material)) return true;
                    }
                }
            }
            catch (MissingReferenceException)
            {
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RepairTmpFontReferences] Material reference check skipped for {GetPath(text.transform)}: {ex.Message}", text);
            }

            return false;
        }

        private static bool NeedsRepair(TMP_Text text)
        {
            if (text == null) return false;
            if (!IsFontAtlasUsable(text.font)) return true;

            try
            {
                var material = text.fontSharedMaterial;
                if (material == null) return true;

                var mainTexture = material.GetTexture(ShaderUtilities.ID_MainTex);
                if (mainTexture == null) return true;

                _ = mainTexture.name;
            }
            catch (MissingReferenceException)
            {
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RepairTmpFontReferences] Material check skipped for {GetPath(text.transform)}: {ex.Message}", text);
            }

            return false;
        }

        private static bool IsFontAtlasUsable(TMP_FontAsset font)
        {
            if (font == null) return false;

            try
            {
                var atlases = font.atlasTextures;
                if (atlases == null || atlases.Length == 0) return false;

                var hasUsableAtlas = false;
                for (int i = 0; i < atlases.Length; i++)
                {
                    var atlas = atlases[i];
                    if (atlas == null) continue;

                    _ = atlas.name;
                    hasUsableAtlas = true;
                }

                return hasUsableAtlas;
            }
            catch (MissingReferenceException)
            {
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RepairTmpFontReferences] Font check failed for {font.name}: {ex.Message}", font);
                return false;
            }
        }

        private static Material GetFontMaterial(TMP_FontAsset font)
        {
            if (font == null) return null;

            try
            {
                return font.material;
            }
            catch (MissingReferenceException)
            {
                return null;
            }
        }

        private static void EnsureFontMaterial(TMP_FontAsset font, Material material)
        {
            if (font == null || material == null) return;

            var current = GetFontMaterial(font);
            if (current == material) return;

            var so = new SerializedObject(font);
            var materialProperty = so.FindProperty("material");
            if (materialProperty == null) return;

            materialProperty.objectReferenceValue = material;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
        }

        private static bool ReferencesAssetPath(UnityEngine.Object asset, string assetPath)
        {
            if (asset == null) return false;

            try
            {
                return string.Equals(AssetDatabase.GetAssetPath(asset), assetPath, StringComparison.OrdinalIgnoreCase);
            }
            catch (MissingReferenceException)
            {
                return true;
            }
        }

        private static bool ReferencesLegacyMaterial(Material material)
        {
            if (material == null) return false;

            try
            {
                if (ReferencesAssetPath(material, LegacyFontPath)) return true;
                return material.name.StartsWith(LegacyFontName, StringComparison.Ordinal);
            }
            catch (MissingReferenceException)
            {
                return true;
            }
        }

        private static void RepairText(TMP_Text text, TMP_FontAsset font, Material material)
        {
            var so = new SerializedObject(text);

            var fontAsset = so.FindProperty("m_fontAsset");
            if (fontAsset != null)
                fontAsset.objectReferenceValue = font;

            var sharedMaterial = so.FindProperty("m_sharedMaterial");
            if (sharedMaterial != null)
                sharedMaterial.objectReferenceValue = material;

            var sharedMaterials = so.FindProperty("m_fontSharedMaterials");
            if (sharedMaterials != null)
                sharedMaterials.ClearArray();

            var fontMaterial = so.FindProperty("m_fontMaterial");
            if (fontMaterial != null)
                fontMaterial.objectReferenceValue = null;

            var fontMaterials = so.FindProperty("m_fontMaterials");
            if (fontMaterials != null)
                fontMaterials.ClearArray();

            so.ApplyModifiedPropertiesWithoutUndo();
            text.font = font;
            text.fontSharedMaterial = material;
            text.SetAllDirty();
            EditorUtility.SetDirty(text);

            if (LogEachRebind)
                Debug.Log($"[RepairTmpFontReferences] Rebound TMP font on {GetPath(text.transform)} -> {font.name}", text);
        }

        private static void SaveLoadedScenesIfEditing()
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(scene.path)) continue;

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            AssetDatabase.SaveAssets();
        }

        private static int ClearTmpFallbackMaterialCache()
        {
            try
            {
                int destroyed = 0;
                var managerType = typeof(TMP_MaterialManager);
                var fallbackMaterialsField = managerType.GetField("m_fallbackMaterials", BindingFlags.NonPublic | BindingFlags.Static);
                var lookupField = managerType.GetField("m_fallbackMaterialLookup", BindingFlags.NonPublic | BindingFlags.Static);
                var cleanupField = managerType.GetField("m_fallbackCleanupList", BindingFlags.NonPublic | BindingFlags.Static);
                var dirtyField = managerType.GetField("isFallbackListDirty", BindingFlags.NonPublic | BindingFlags.Static);

                if (fallbackMaterialsField?.GetValue(null) is IDictionary fallbackMaterials)
                {
                    foreach (var value in fallbackMaterials.Values)
                        destroyed += DestroyFallbackMaterial(value);

                    fallbackMaterials.Clear();
                }

                if (lookupField?.GetValue(null) is IDictionary lookup)
                    lookup.Clear();

                if (cleanupField?.GetValue(null) is IList cleanup)
                    cleanup.Clear();

                dirtyField?.SetValue(null, false);
                return destroyed;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RepairTmpFontReferences] Could not clear TMP fallback material cache: {ex.Message}");
                return 0;
            }
        }

        private static int DestroyFallbackMaterial(object fallback)
        {
            if (fallback == null) return 0;

            var materialField = fallback.GetType().GetField("fallbackMaterial", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (materialField?.GetValue(fallback) is Material material && material != null)
            {
                UnityEngine.Object.DestroyImmediate(material);
                return 1;
            }

            return 0;
        }

        private static int RefreshLoadedTmp()
        {
            int refreshed = 0;
            foreach (var text in Resources.FindObjectsOfTypeAll<TMP_Text>())
            {
                if (text == null || text.gameObject == null) continue;
                if (!text.gameObject.scene.IsValid()) continue;

                text.SetAllDirty();
                refreshed++;
            }

            return refreshed;
        }

        private static string GetPath(Transform transform)
        {
            if (transform == null) return "<null>";

            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }
    }
}
