using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DrscfZ.UI;

namespace DrscfZ.EditorTools
{
    public static class UIPrefabBatchExporter
    {
        private const string OutputDir = "Assets/Prefabs/UI/Panels";

        private static readonly HashSet<string> SkipScripts = new HashSet<string>
        {
            "DamageNumber",
            "FeatureLockOverlay",
            "FailureToastLocale",
            "GameSpeedController",
            "GiftEffectSystem",
            "PlayerNameTag",
            "RuntimeUIFactory",
            "UIPrefabLoader",
            "UIPrefabOpenButton",
            "WorkerBubble",
            "WorldSpaceLabel"
        };

        private static readonly string[] RootPropertyNames =
        {
            "_panel",
            "_panelRoot",
            "_root",
            "_gameUIPanel",
            "_bottomBar",
            "_overlay"
        };

        [MenuItem("Tools/DrscfZ/UI Prefabs/Export Selected UI Roots")]
        public static void ExportSelectedRoots()
        {
            var selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                EditorUtility.DisplayDialog("UI Prefabs", "Select one or more scene UI roots first.", "OK");
                return;
            }

            ExportRoots(new List<GameObject>(selected), false);
        }

        [MenuItem("Tools/DrscfZ/UI Prefabs/Export Scene UI Candidates")]
        public static void ExportSceneCandidates()
        {
            var roots = CollectSceneCandidates();
            if (roots.Count == 0)
            {
                EditorUtility.DisplayDialog("UI Prefabs", "No scene UI candidates were found.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "UI Prefabs",
                    "Export " + roots.Count + " UI roots to " + OutputDir + " and connect scene objects to those prefabs?",
                    "Export",
                    "Cancel"))
            {
                return;
            }

            ExportRoots(roots, true);
        }

        [MenuItem("Tools/DrscfZ/UI Prefabs/Ping Output Folder")]
        public static void PingOutputFolder()
        {
            EnsureOutputDir();
            var folder = AssetDatabase.LoadAssetAtPath<Object>(OutputDir);
            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
        }

        [MenuItem("Tools/DrscfZ/UI Prefabs/Add Loader To Canvas")]
        public static void AddLoaderToCanvas()
        {
            var existing = Object.FindObjectOfType<UIPrefabLoader>(true);
            if (existing != null)
            {
                Selection.activeObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                return;
            }

            var canvas = Object.FindObjectOfType<Canvas>(true);
            if (canvas == null)
            {
                var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Undo.RegisterCreatedObjectUndo(canvasGo, "Create UI Canvas");
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            var loaderGo = new GameObject("UIPrefabLoader");
            Undo.RegisterCreatedObjectUndo(loaderGo, "Create UI Prefab Loader");
            loaderGo.transform.SetParent(canvas.transform, false);
            var loader = loaderGo.AddComponent<UIPrefabLoader>();

            var serialized = new SerializedObject(loader);
            var parentProp = serialized.FindProperty("_defaultParent");
            if (parentProp != null)
                parentProp.objectReferenceValue = canvas.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeObject = loaderGo;
            EditorGUIUtility.PingObject(loaderGo);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private static List<GameObject> CollectSceneCandidates()
        {
            var result = new List<GameObject>();
            var seen = new HashSet<GameObject>();
            var behaviours = Object.FindObjectsOfType<MonoBehaviour>(true);

            for (int i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null)
                    continue;

                var script = MonoScript.FromMonoBehaviour(behaviour);
                if (script == null)
                    continue;

                string scriptPath = AssetDatabase.GetAssetPath(script).Replace("\\", "/");
                if (!scriptPath.StartsWith("Assets/Scripts/UI/"))
                    continue;

                string scriptName = script.name;
                if (SkipScripts.Contains(scriptName))
                    continue;

                var root = ResolveRoot(behaviour);
                if (root == null || !root.scene.IsValid())
                    continue;

                if (seen.Add(root))
                    result.Add(root);
            }

            result.Sort((a, b) => GetDepth(b.transform).CompareTo(GetDepth(a.transform)));
            return result;
        }

        private static GameObject ResolveRoot(MonoBehaviour behaviour)
        {
            var go = behaviour.gameObject;
            if (go.GetComponent<Canvas>() == null)
                return go;

            var serialized = new SerializedObject(behaviour);
            for (int i = 0; i < RootPropertyNames.Length; i++)
            {
                var prop = serialized.FindProperty(RootPropertyNames[i]);
                var root = ReadObjectReference(prop);
                if (root != null && root.scene.IsValid())
                    return root;
            }

            return null;
        }

        private static GameObject ReadObjectReference(SerializedProperty prop)
        {
            if (prop == null || prop.propertyType != SerializedPropertyType.ObjectReference)
                return null;

            var value = prop.objectReferenceValue;
            var go = value as GameObject;
            if (go != null)
                return go;

            var component = value as Component;
            return component != null ? component.gameObject : null;
        }

        private static void ExportRoots(List<GameObject> roots, bool useScriptPrefix)
        {
            EnsureOutputDir();

            int exported = 0;
            int skipped = 0;
            var usedPaths = new HashSet<string>();

            for (int i = 0; i < roots.Count; i++)
            {
                var root = roots[i];
                if (root == null || !root.scene.IsValid())
                {
                    skipped++;
                    continue;
                }

                if (PrefabUtility.IsAnyPrefabInstanceRoot(root))
                {
                    skipped++;
                    Debug.Log("[UIPrefabBatchExporter] Already a prefab instance: " + GetHierarchyPath(root.transform));
                    continue;
                }

                string fileName = BuildPrefabFileName(root, useScriptPrefix);
                string assetPath = MakeUniquePath(OutputDir + "/" + fileName, usedPaths);

                bool success;
                PrefabUtility.SaveAsPrefabAssetAndConnect(root, assetPath, InteractionMode.UserAction, out success);
                if (success)
                {
                    exported++;
                    Debug.Log("[UIPrefabBatchExporter] Exported " + GetHierarchyPath(root.transform) + " -> " + assetPath);
                }
                else
                {
                    skipped++;
                    Debug.LogWarning("[UIPrefabBatchExporter] Failed to export: " + GetHierarchyPath(root.transform));
                }
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog(
                "UI Prefabs",
                "Exported: " + exported + "\nSkipped: " + skipped + "\nFolder: " + OutputDir,
                "OK");
        }

        private static string BuildPrefabFileName(GameObject root, bool useScriptPrefix)
        {
            string baseName = Sanitize(root.name);
            if (useScriptPrefix)
            {
                var scriptName = GetPrimaryUIScriptName(root);
                if (!string.IsNullOrEmpty(scriptName) && scriptName != baseName)
                    baseName = Sanitize(scriptName + "_" + root.name);
            }

            return baseName + ".prefab";
        }

        private static string MakeUniquePath(string assetPath, HashSet<string> usedPaths)
        {
            if (!usedPaths.Contains(assetPath))
            {
                usedPaths.Add(assetPath);
                return assetPath;
            }

            string directory = Path.GetDirectoryName(assetPath).Replace("\\", "/");
            string name = Path.GetFileNameWithoutExtension(assetPath);
            string extension = Path.GetExtension(assetPath);
            int index = 2;
            string candidate;
            do
            {
                candidate = directory + "/" + name + "_" + index + extension;
                index++;
            }
            while (usedPaths.Contains(candidate));

            usedPaths.Add(candidate);
            return candidate;
        }

        private static string GetPrimaryUIScriptName(GameObject root)
        {
            var behaviours = root.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null)
                    continue;

                var script = MonoScript.FromMonoBehaviour(behaviour);
                if (script == null)
                    continue;

                string path = AssetDatabase.GetAssetPath(script).Replace("\\", "/");
                if (path.StartsWith("Assets/Scripts/UI/") && !SkipScripts.Contains(script.name))
                    return script.name;
            }

            return null;
        }

        private static void EnsureOutputDir()
        {
            if (AssetDatabase.IsValidFolder(OutputDir))
                return;

            string current = "Assets";
            string[] parts = OutputDir.Substring("Assets/".Length).Split('/');
            for (int i = 0; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static int GetDepth(Transform transform)
        {
            int depth = 0;
            while (transform.parent != null)
            {
                depth++;
                transform = transform.parent;
            }

            return depth;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            var names = new List<string>();
            while (transform != null)
            {
                names.Add(transform.name);
                transform = transform.parent;
            }

            names.Reverse();
            return string.Join("/", names.ToArray());
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "UIPanel";

            char[] invalid = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalid.Length; i++)
                value = value.Replace(invalid[i], '_');

            return value.Replace(' ', '_');
        }
    }
}
