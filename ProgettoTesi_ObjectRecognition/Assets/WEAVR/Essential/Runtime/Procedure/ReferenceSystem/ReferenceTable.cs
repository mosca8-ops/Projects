using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TXT.WEAVR.Procedure
{
    [Serializable]
    public class ReferenceTable : ProcedureObject, IReferenceTable
    {
        public static Action<ScriptableObject, ScriptableObject> s_PersistItem;
        public static Func<ProcedureObject, string, ReferenceTarget> s_CreateRefTarget;
        public static Action s_RefreshSceneView;

        [Serializable]
        private class SerializedDictionaryOfPropertyAndSceneItem : SerializableDictionary<PropertyName, SceneItem> { }

        //[SerializeField]
        [NonSerialized]
        private GameObject m_sceneWeavr;
        [SerializeField]
        private List<ReferenceItem> m_references;
        [SerializeField]
        private SceneData m_sceneData;

        [SerializeField]
        [HideInInspector]
        private List<string> m_prevReferences;
        private List<ReferenceItem> m_shadowReferences;

        [SerializeField]
        private SerializedDictionaryOfPropertyAndSceneItem m_exposedProperties;
        
        private Dictionary<Object, ReferenceItem> m_referencesTable;
        private HashSet<GameObject> m_gameObjects;

        [NonSerialized]
        private IDBookkeeper m_bookkeeper;

        public bool IsReady { get; private set; }

        public IReadOnlyList<ReferenceItem> References => m_references;

        public SceneData SceneData
        {
            get => m_sceneData;
            set
            {
                if(m_sceneData == null || !m_sceneData.IsSame(value))
                {
                    //BeginChange();
                    m_sceneData = value;
                    if (m_bookkeeper)
                    {
                        m_bookkeeper.UnregisterTable(this);
                    }
                    if (!value.IsEmpty)
                    {
                        m_bookkeeper = IDBookkeeper.GetSingleton(value.ResolveScene());
                        if (m_bookkeeper)
                        {
                            m_bookkeeper.RegisterTable(this);
                        }
                    }
                    PropertyChanged(nameof(SceneData));
                }
            }
        }

        public GameObject SceneWeavr
        {
            get { return m_sceneWeavr; }
            set
            {
                if(m_sceneWeavr != value && Application.isEditor)
                {
                    m_sceneWeavr = value;
                    if (m_sceneWeavr)
                    {
                        Resolve();
                    }
                }
            }
        }

        public Dictionary<PropertyName, SceneItem> IDs => m_exposedProperties;

        public IExposedPropertyTable Resolver
        {
            get
            {
                if (!m_bookkeeper)
                {
                    UpdateBookkeeper(true);
                }
                return m_bookkeeper;
            }
        }

        public static ReferenceTable Create()
        {
            var table = CreateInstance<ReferenceTable>();

            return table;
        }

        protected override void OnEnable()
        {
#if UNITY_2019_3
            if(m_referencesTable != null && m_referencesTable?.Count > 0)
            {
                // Here we have a save operation
                Procedure.ShouldReset();
                return;
            }
#endif
            base.OnEnable();
            if (m_references == null)
            {
                m_references = new List<ReferenceItem>();
            }
            if (m_exposedProperties == null)
            {
                m_exposedProperties = new SerializedDictionaryOfPropertyAndSceneItem();
            }
            if (m_sceneData == null)
            {
                AdaptToCurrentScene();
            }
            if(m_shadowReferences == null)
            {
                m_shadowReferences = new List<ReferenceItem>();
            }
            if(m_prevReferences == null)
            {
                m_prevReferences = new List<string>();
            }
            UpdateBookkeeper(true);
            if (Application.isEditor)
            {
                RemoveInvalidReferences();
                if (m_referencesTable == null)
                {
                    m_referencesTable = new Dictionary<Object, ReferenceItem>();
                }
                else
                {
                    m_referencesTable.Clear();
                }

                RefreshGameObjects();
            }

            SceneManager.sceneLoaded -= SceneManager_SceneLoaded;
            SceneManager.sceneLoaded += SceneManager_SceneLoaded;
            SceneManager.sceneUnloaded -= SceneManager_SceneUnloaded;
            SceneManager.sceneUnloaded += SceneManager_SceneUnloaded;

            IsReady = true;
        }

        private void RefreshGameObjects(bool forceClear = false)
        {
            m_gameObjects.Clear();
            if (forceClear)
            {
                m_referencesTable.Clear();
            }
            var scene = SceneData.ResolveScene();
            bool injectionIsValid = Application.isEditor && Procedure && scene.IsValid() && scene.isLoaded;
            foreach (var item in m_references)
            {
                if(injectionIsValid && !item.reference)
                {
                    item.Resolve();
                    item.InjectAll();
                }
                if (item.reference)
                {
                    m_referencesTable[item.reference] = item;
                    RegisterGameObject(item);
                }
            }
            s_RefreshSceneView?.Invoke();
        }

        private void UpdateBookkeeper(bool shouldRegister)
        {
            var scene = SceneData.ResolveScene();
            var oldBookkeeper = m_bookkeeper;
            if (scene.isLoaded && scene.IsValid())
            {
                m_bookkeeper = IDBookkeeper.GetSingleton(scene);
            }
            else
            {
                m_bookkeeper = IDBookkeeper.GetSingleton();
            }
            if (oldBookkeeper != m_bookkeeper && oldBookkeeper)
            {
                oldBookkeeper.UnregisterTable(this);
            }
            if(m_bookkeeper && shouldRegister)
            {
                m_bookkeeper.RegisterTable(this);
            }
        }

        private void SceneManager_SceneUnloaded(Scene scene)
        {
            if(scene.path == m_sceneData.Path && m_bookkeeper)
            {
                m_bookkeeper.UnregisterTable(this);
                m_bookkeeper = null;
            }
        }

        private void SceneManager_SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if(scene.path == m_sceneData.Path)
            {
                m_bookkeeper = IDBookkeeper.GetSingleton(scene);
                m_bookkeeper.RegisterTable(this);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (m_prevReferences == null || m_prevReferences.Count == 0)
            {
                BackupReferences();
            }
            if (m_bookkeeper)
            {
                m_bookkeeper.UnregisterTable(this);
                m_bookkeeper = null;
            }
        }

        public void BackupReferences()
        {
            if (Application.isEditor && m_references.Count(r => r) > 0)
            {
                m_prevReferences = m_references.Select(r => r.Serialize()).ToList();
            }
        }

        public void RefreshData()
        {
            if (!m_bookkeeper)
            {
                UpdateBookkeeper(true);
            }
        }

        private void RegisterGameObject(ReferenceItem item)
        {
            if (item.reference is Component c && c)
            {
                m_gameObjects.Add(c.gameObject);
            }
            else if(item.reference is GameObject go)
            {
                m_gameObjects.Add(go);
            }
        }

        public IEnumerable<GameObject> GetGameObjects()
        {
            return m_gameObjects;
        }

        public void AdaptToCurrentScene()
        {
            AdaptToScene(SceneManager.GetActiveScene());
        }

        public void AdaptToScene(Scene? scene = null)
        {
            m_sceneData = new SceneData(scene ?? SceneManager.GetActiveScene());
        }

        private void CleanUpReferences()
        {
            for (int i = 0; i < m_references.Count; i++)
            {
                if (!m_references[i].reference)
                {
                    m_references.RemoveAt(i--);
                }
            }
        }

        private void RemoveInvalidReferences()
        {
            int currentCount = m_references.Count;
            for (int i = 0; i < m_references.Count; i++)
            {
                if (!m_references[i] || !m_references[i].IsValid)
                {
                    WeavrDebug.Log(this, $"Removing reference {i} because it is invalid");
                    m_references.RemoveAt(i--);
                }
            }
            if (Application.isEditor && m_referencesTable == null)
            {
                m_referencesTable = new Dictionary<Object, ReferenceItem>();
            }
            if (m_gameObjects == null)
            {
                m_gameObjects = new HashSet<GameObject>();
            }
            else
            {
                m_gameObjects.Clear();
            }

            if (Application.isEditor && currentCount > 2 && m_references.Count < currentCount * 4 / 5)
            {
                WeavrDebug.Log(this, "Potential references loss detected!!");
            }
        }

        public void AutoResolve()
        {
            if (SceneWeavr) { return; }
            Resolve();
            m_sceneWeavr = WeavrManager.Main?.gameObject;
        }

        private void RetrieveRuntimeGameObjects()
        {
            if (!Application.isEditor)
            {
                if (m_gameObjects == null)
                {
                    m_gameObjects = new HashSet<GameObject>();
                }
                else
                {
                    m_gameObjects.Clear();
                }
                foreach (var item in m_references)
                {
                    RegisterGameObject(item);
                }
            }
        }

        public void Resolve()
        {
            RemoveInvalidReferences();
            if (Application.isEditor)
            {
                HashSet<IRequiresValidation> toValidate = new HashSet<IRequiresValidation>();
                foreach (var item in m_references)
                {
                    if (item.Resolve())
                    {
                        item.InjectAll();
                        if (item.reference)
                        {
                            m_referencesTable[item.reference] = item;
                            RegisterGameObject(item);
                            foreach(var target in item.Targets)
                            {
                                if(target is IRequiresValidation validator)
                                {
                                    toValidate.Add(validator);
                                }
                            }
                        }
                    }
                }

                //if (!Application.isPlaying)
                //{
                //    foreach (var validator in toValidate)
                //    {
                //        validator.OnValidate();
                //    }
                //}
            }
            else
            {
                foreach (var item in m_references)
                {
                    if (item.Resolve())
                    {
                        item.InjectAll();
                    }
                }
                RetrieveRuntimeGameObjects();
            }
        }

        public bool TryResolve(out List<ReferenceItem> referencesWithIssues)
        {
            referencesWithIssues = new List<ReferenceItem>();
            if (Application.isEditor)
            {
                HashSet<IRequiresValidation> toValidate = new HashSet<IRequiresValidation>();
                foreach (var item in m_references)
                {
                    if (item.Resolve())
                    {
                        item.InjectAll();
                        m_referencesTable[item.reference] = item;
                        RegisterGameObject(item);
                        foreach (var target in item.Targets)
                        {
                            if (target is IRequiresValidation validator)
                            {
                                toValidate.Add(validator);
                            }
                        }
                    }
                    else
                    {
                        referencesWithIssues.Add(item);
                    }
                }

                //if (!Application.isPlaying)
                //{
                //    foreach (var validator in toValidate)
                //    {
                //        validator.OnValidate();
                //    }
                //}
            }
            else
            {
                foreach (var item in m_references)
                {
                    if (item.Resolve())
                    {
                        item.InjectAll();
                    }
                    else
                    {
                        referencesWithIssues.Add(item);
                    }
                }
            }
            return referencesWithIssues.Count == 0;
        }

        public bool Merge(ReferenceTable other)
        {
            foreach(var item in other.m_references)
            {
                item.Resolve();
                if(m_referencesTable.TryGetValue(item.reference, out ReferenceItem thisItem))
                {
                    thisItem.Merge(item);
                }
                else
                {
                    var copy = Instantiate(item);
                    m_references.Add(copy);
                    m_referencesTable.Add(item.reference, copy);
                    RegisterGameObject(item);
                }
            }
            return true;
        }

        public ReferenceItem Merge(ReferenceItem item)
        {
            if (!Application.isEditor) { return null; }
            var mergedReference = m_references.FirstOrDefault(r => r != item && r.Merge(item));
            if (mergedReference){
                m_references.Remove(item);
                RemoveReference(item, false);
            }
            RefreshGameObjects(true);

            return mergedReference ? mergedReference : item;
        }

        public void RemoveReferenceItem(ReferenceItem item)
        {
            if (Application.isEditor)
            {
                m_references.Remove(item);
                RemoveReference(item, true);
                RefreshGameObjects();
            }
        }

        public bool ContainsTarget(ProcedureObject target)
        {
            foreach (var item in m_references)
            {
                if (item.ContainsTarget(target))
                {
                    return true;
                }
            }
            return false;
        }

        public void Register(ProcedureObject target, Object value, string path)
        {
            if (!Application.isEditor 
                || !value 
                || string.IsNullOrEmpty(path) 
                || s_CreateRefTarget == null 
                || (value is GameObject go && !go.scene.isLoaded) 
                || (value is Component c && !c.gameObject.scene.isLoaded)) 
            { return; }

            var item = GetItem(value);
            var refTarget = s_CreateRefTarget(target, path);
            if (item.AddTarget(refTarget))
            {
                s_PersistItem?.Invoke(item, this);
                s_PersistItem?.Invoke(refTarget, item);
            }
            else
            {
                DestroyImmediate(refTarget, true);
            }
            s_RefreshSceneView?.Invoke();
        }

        public void Unregister(ProcedureObject target, Object value, string path)
        {
            if (!Application.isEditor || !value || string.IsNullOrEmpty(path)) { return; }
            if(m_referencesTable.TryGetValue(value, out ReferenceItem item))
            {
                item.RemoveTarget(target, path);
                if(item.Targets.Count == 0)
                {
                    m_references.Remove(item);
                    m_referencesTable.Remove(value);
                    RemoveReference(item, true);

                    RefreshGameObjects();
                }
            }
        }

        public void RemoveTargetCompletely(ProcedureObject target)
        {
            if (!Application.isEditor) { return; }
            bool refreshGameObjects = false;
            for (int i = 0; i < m_references.Count; i++)
            {
                var item = m_references[i];
                if (item)
                {
                    item.RemoveTarget(target);
                    if (item.Targets.Count == 0)
                    {
                        if (item.reference && m_referencesTable.ContainsKey(item.reference))
                        {
                            m_referencesTable.Remove(item.reference);
                        }
                        m_references.RemoveAt(i--);
                        RemoveReference(item, true);
                        refreshGameObjects = true;
                    }
                }
            }
            if (refreshGameObjects)
            {
                RefreshGameObjects();
            }
        }

        public void Clear(bool destroyReferences)
        {
            if (!Application.isEditor) { return; }

            if (destroyReferences)
            {
                if(m_references.Count > 0 && m_references.All(r => r.Targets.All(t => t.Target)))
                {
                    m_prevReferences = m_references.Select(r => r.Serialize()).ToList();
                }
                for (int i = 0; i < m_references.Count; i++)
                {
                    RemoveReference(m_references[i], false);
                }
            }
            else
            {
                m_shadowReferences = new List<ReferenceItem>(m_references);
            }
            m_references.Clear();
            m_referencesTable.Clear();
            m_gameObjects.Clear();
        }

        public void ForceRestoreFromJSON()
        {
            m_references = m_prevReferences.Select(s => ReferenceItem.Deserialize(s, Procedure)).Where(r => r).ToList();

            if (s_PersistItem != null)
            {
                foreach (var item in m_references)
                {
                    if(item && item.Resolve())
                    {
                        item.InjectAll();
                    }
                    s_PersistItem(item, this);
                    foreach (var refTarget in item.Targets)
                    {
                        s_PersistItem(refTarget, item);
                    }
                }
            }

            if (m_references != null)
            {
                if (m_referencesTable == null)
                {
                    m_referencesTable = new Dictionary<Object, ReferenceItem>();
                }
                else
                {
                    m_referencesTable.Clear();
                }

                RefreshGameObjects();
            }
        }

        public bool TryRestorePreviousReferences()
        {
            if(m_shadowReferences != null && m_shadowReferences.Count(r => r) > 0)
            {
                WeavrDebug.Log(this, "Restoring references from shadow copy");
                m_references = m_shadowReferences;
                m_shadowReferences = null;
                RemoveInvalidReferences();
            }

            if((m_references.Count == 0 || !m_references.Any(r => r && r.Targets.All(t => t && t.Target))) 
                && m_prevReferences != null && m_prevReferences.Count > 0)
            {
                WeavrDebug.Log(this, "Last Resort: Restoring references from serialized copy");
                RestoreFromJson();
            }

            if (m_references != null)
            {
                if (m_referencesTable == null)
                {
                    m_referencesTable = new Dictionary<Object, ReferenceItem>();
                }
                else
                {
                    m_referencesTable.Clear();
                }

                RefreshGameObjects();
            }
            return m_references.Count > 0;
        }

        private void RestoreFromJson()
        {
            var references = m_prevReferences.Select(s => ReferenceItem.Deserialize(s, Procedure)).Where(r => r).ToList();
            HashSet<ReferenceItem> mergedReferences = new HashSet<ReferenceItem>();
            HashSet<ReferenceItem> validReferences = new HashSet<ReferenceItem>();
            foreach (var item in m_references)
            {
                if (!validReferences.Contains(item))
                {
                    foreach (var jsonItem in references)
                    {
                        if (!mergedReferences.Contains(item) && item.Merge(jsonItem))
                        {
                            validReferences.Add(item);
                            mergedReferences.Add(jsonItem);
                        }
                    }
                }
            }

            m_references = validReferences.ToList();
            m_references.AddRange(references.Where(r => !mergedReferences.Contains(r)));

            if (s_PersistItem != null)
            {
                foreach (var item in m_references)
                {
                    if (item && item.Resolve())
                    {
                        item.InjectAll();
                    }
                    s_PersistItem(item, this);
                    foreach (var refTarget in item.Targets)
                    {
                        s_PersistItem(refTarget, item);
                    }
                }
            }
        }

        private void RemoveReference(ReferenceItem item, bool destroyTargets)
        {
            if (destroyTargets)
            {
                for (int i = 0; i < item.Targets.Count; i++)
                {
                    if (item.Targets[i])
                    {
                        DestroyImmediate(item.Targets[i], true);
                    }
                }
            }
            DestroyImmediate(item, true);
        }

        public bool HasGameObject(GameObject gameObject)
        {
            return m_gameObjects != null && m_gameObjects.Contains(gameObject);
        }

        public ReferenceItem GetItem(Object reference)
        {
            if (!Application.isEditor) { return null; }
            if(!m_referencesTable.TryGetValue(reference, out ReferenceItem item) || !item)
            {
                item = ReferenceItem.Create(reference);
                //s_PersistItem?.Invoke(item, this);
                m_references.Add(item);
                m_referencesTable[reference] = item;
                RegisterGameObject(item);
            }
            return item;
        }
    }
}
