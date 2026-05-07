using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DrscfZ.UI
{
    /// <summary>
    /// Loads UI panel prefabs on demand and keeps scene-level UI wiring small.
    /// Existing hand-bound scene UI can keep working while panels are moved one by one.
    /// </summary>
    public class UIPrefabLoader : MonoBehaviour
    {
        [Serializable]
        public class PanelEntry
        {
            public string id;
            public GameObject prefab;
            public Transform parent;
            public bool preload;
            public bool keepAlive = true;
            public bool hideAfterPreload = true;
        }

        public static UIPrefabLoader Instance { get; private set; }

        [SerializeField] private Transform _defaultParent;
        [SerializeField] private List<PanelEntry> _panels = new List<PanelEntry>();

        private readonly Dictionary<string, PanelEntry> _entries = new Dictionary<string, PanelEntry>();
        private readonly Dictionary<string, GameObject> _instances = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, bool> _openStates = new Dictionary<string, bool>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            RebuildLookup();
        }

        private void Start()
        {
            for (int i = 0; i < _panels.Count; i++)
            {
                var entry = _panels[i];
                if (entry == null || !entry.preload || string.IsNullOrEmpty(entry.id))
                    continue;

                var instance = EnsureInstance(entry.id);
                if (instance != null && entry.hideAfterPreload)
                    instance.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static GameObject OpenPanel(string id)
        {
            return Instance != null ? Instance.Show(id) : null;
        }

        public static void ClosePanel(string id)
        {
            if (Instance != null)
                Instance.Hide(id);
        }

        public static GameObject TogglePanel(string id)
        {
            return Instance != null ? Instance.Toggle(id) : null;
        }

        public GameObject Show(string id)
        {
            var instance = EnsureInstance(id);
            if (instance == null)
                return null;

            instance.SetActive(true);
            InvokePanelMethod(instance, "ShowPanel", "OpenPanel", "Show", "Open");
            _openStates[id] = true;
            return instance;
        }

        public void Hide(string id)
        {
            GameObject instance;
            if (!_instances.TryGetValue(id, out instance) || instance == null)
                return;

            bool invoked = InvokePanelMethod(instance, "HidePanel", "ClosePanel", "Hide", "Close");
            _openStates[id] = false;
            var entry = GetEntry(id);
            if (entry != null && !entry.keepAlive)
            {
                _instances.Remove(id);
                _openStates.Remove(id);
                Destroy(instance);
                return;
            }

            if (!invoked)
                instance.SetActive(false);
        }

        public GameObject Toggle(string id)
        {
            bool isOpen;
            if (_openStates.TryGetValue(id, out isOpen) && isOpen)
            {
                Hide(id);
                GameObject existing;
                _instances.TryGetValue(id, out existing);
                return existing;
            }

            return Show(id);
        }

        public T GetPanel<T>(string id) where T : Component
        {
            GameObject instance;
            if (!_instances.TryGetValue(id, out instance) || instance == null)
                return null;

            return instance.GetComponentInChildren<T>(true);
        }

        public T EnsurePanel<T>(string id) where T : Component
        {
            var instance = EnsureInstance(id);
            return instance != null ? instance.GetComponentInChildren<T>(true) : null;
        }

        public void Register(string id, GameObject prefab, Transform parent = null, bool keepAlive = true)
        {
            if (string.IsNullOrEmpty(id) || prefab == null)
                return;

            var entry = GetEntry(id);
            if (entry == null)
            {
                entry = new PanelEntry();
                entry.id = id;
                _panels.Add(entry);
            }

            entry.prefab = prefab;
            entry.parent = parent;
            entry.keepAlive = keepAlive;
            _entries[id] = entry;
        }

        private GameObject EnsureInstance(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            GameObject existing;
            if (_instances.TryGetValue(id, out existing) && existing != null)
                return existing;

            var entry = GetEntry(id);
            GameObject prefab = entry != null ? entry.prefab : null;
            if (prefab == null)
                prefab = Resources.Load<GameObject>("UI/" + id);

            if (prefab == null)
            {
                Debug.LogWarning("[UIPrefabLoader] Missing UI prefab: " + id);
                return null;
            }

            Transform parent = ResolveParent(entry);
            var instance = Instantiate(prefab, parent, false);
            instance.name = prefab.name;
            _instances[id] = instance;
            _openStates[id] = instance.activeSelf;
            return instance;
        }

        private Transform ResolveParent(PanelEntry entry)
        {
            if (entry != null && entry.parent != null)
                return entry.parent;

            if (_defaultParent != null)
                return _defaultParent;

            return RuntimeUIFactory.GetCanvasTransform();
        }

        private PanelEntry GetEntry(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            PanelEntry entry;
            if (_entries.TryGetValue(id, out entry))
                return entry;

            RebuildLookup();
            _entries.TryGetValue(id, out entry);
            return entry;
        }

        private void RebuildLookup()
        {
            _entries.Clear();
            for (int i = 0; i < _panels.Count; i++)
            {
                var entry = _panels[i];
                if (entry == null || string.IsNullOrEmpty(entry.id))
                    continue;

                _entries[entry.id] = entry;
            }
        }

        private static bool InvokePanelMethod(GameObject root, params string[] methodNames)
        {
            var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int nameIndex = 0; nameIndex < methodNames.Length; nameIndex++)
            {
                string methodName = methodNames[nameIndex];
                for (int i = 0; i < behaviours.Length; i++)
                {
                    var behaviour = behaviours[i];
                    if (behaviour == null)
                        continue;

                    var type = behaviour.GetType();
                    if (type.Namespace != "DrscfZ.UI")
                        continue;

                    var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    for (int methodIndex = 0; methodIndex < methods.Length; methodIndex++)
                    {
                        var method = methods[methodIndex];
                        if (method.Name != methodName)
                            continue;

                        object[] args;
                        if (!TryBuildDefaultArgs(method, out args))
                            continue;

                        method.Invoke(behaviour, args);
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryBuildDefaultArgs(MethodInfo method, out object[] args)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 0)
            {
                args = null;
                return true;
            }

            args = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (!parameter.IsOptional)
                    return false;

                args[i] = parameter.DefaultValue;
            }

            return true;
        }
    }
}
