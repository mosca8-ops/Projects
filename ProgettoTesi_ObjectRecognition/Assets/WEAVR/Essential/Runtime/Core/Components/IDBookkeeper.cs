namespace TXT.WEAVR.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.EditorBridge;
    using UnityEngine;
    using UnityEngine.Assertions;
    using UnityEngine.SceneManagement;

    using Object = UnityEngine.Object;

    [Serializable] public class DictionaryOfStringAndUniqueId : SerializableDictionary<string, UniqueID> { }

    /// <summary>
    /// Keeps track of the unique ids found in hierarchy objects
    /// </summary>
    [Serializable]
    [Stateless]
    [ExecuteInEditMode]
    [AddComponentMenu("")]
    public class IDBookkeeper : MonoBehaviour, IWeavrSingleton, IExposedPropertyTable
    {
        #region [  STATIC PART  ]
        public static Action<UniqueID, string> SetIDValue;

        [NonSerialized]
        private static DictionaryOfIntAndString s_globalUids = new DictionaryOfIntAndString();
        [NonSerialized]
        private static DictionaryOfStringAndUniqueId s_globalIndexedObjects = new DictionaryOfStringAndUniqueId();

        //private static IDBookkeeper s_instance;
        ///// <summary>
        ///// Gets the current IDBookkeper
        ///// </summary>
        //public static IDBookkeeper Current
        //{
        //    get
        //    {
        //        if (s_instance == null)
        //        {
        //            s_instance = FindObjectOfType<IDBookkeeper>();
        //            if (s_instance == null)
        //            {
        //                s_instance = new GameObject("IDBookkeeper").AddComponent<IDBookkeeper>();
        //            }
        //            //s_instance = CreateInstance<IDBookkeeper>();
        //            //_instance.OnEnable();
        //        }
        //        return s_instance;
        //    }
        //}

        public static IDBookkeeper GetSingleton(Scene scene)
        {
            return Weavr.GetInScene<IDBookkeeper>(scene);
        }

        public static IDBookkeeper GetSingleton()
        {
            return Weavr.GetInCurrentScene<IDBookkeeper>();
        }

        #endregion

        [SerializeField]
        private bool m_autoUpdate = true;
        [NonSerialized]
        private DictionaryOfIntAndString m_uids = new DictionaryOfIntAndString();
        [SerializeField]
        private DictionaryOfStringAndUniqueId m_indexedObjects = new DictionaryOfStringAndUniqueId();

        [NonSerialized]
        private bool m_initialized;

        private Dictionary<PropertyName, Object> m_cachedObjects = new Dictionary<PropertyName, Object>();
        private Dictionary<PropertyName, IReferenceTable> m_tables = new Dictionary<PropertyName, IReferenceTable>();
        private IReferenceTable m_lastTable;

        public IReadOnlyDictionary<string, UniqueID> RegistredIds => m_indexedObjects; 

        public bool AutoUpdate => m_autoUpdate;

        private void OnEnable()
        {
            var existingInstance = GetSingleton(gameObject.scene);
            if (existingInstance != null && existingInstance != this)
            {
                if (Application.isEditor)
                {
                    DestroyImmediate(gameObject);
                }
                else
                {
                    Destroy(gameObject);
                }
                return;
            }

            List<string> keysToRemove = new List<string>();
            foreach(var pair in s_globalIndexedObjects)
            {
                if(!pair.Value || !pair.Value.gameObject)
                {
                    keysToRemove.Add(pair.Key);
                }
            }
            foreach(var key in keysToRemove)
            {
                s_globalIndexedObjects.Remove(key);
            }

            foreach (var pair in m_indexedObjects)
            {
                if (pair.Value?.gameObject)
                {
                    m_uids[pair.Value.gameObject.GetInstanceID()] = pair.Key;
                    s_globalUids[pair.Value.gameObject.GetInstanceID()] = pair.Key;
                    s_globalIndexedObjects[pair.Key] = pair.Value;
                }
            }
            m_initialized = true;
        }

        public void RegisterAllUids()
        {
            Clear();
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                foreach (var uid in root.GetComponentsInChildren<UniqueID>(true))
                {
                    Register(uid);
                }
            }
        }

        /// <summary>
        /// Gets the amount of objects registered
        /// </summary>
        public int RegisteredObjects => m_indexedObjects != null ? m_indexedObjects.Count : 0;

        /// <summary>
        /// Gets the amount of registered objects in ALL SCENES
        /// </summary>
        public static int AllRegisteredObjects => s_globalIndexedObjects != null ? s_globalIndexedObjects.Count : 0;

        /// <summary>
        /// Gets the object with the specified unique id. Returns null if no object is found with specified id
        /// </summary>
        /// <param name="uniqueID">The unique id as string to find the object</param>
        /// <returns>The object found by the ID, or null if not found</returns>
        public GameObject this[string uniqueID]
        {
            get
            {
                if (m_initialized)
                {
                    return !string.IsNullOrEmpty(uniqueID) && m_indexedObjects.TryGetValue(uniqueID, out UniqueID id) ? id?.gameObject : null;
                }
                else
                {
                    return FallbackRetrieveId(uniqueID);
                }
            }
        }

        private GameObject FallbackRetrieveId(string uniqueID)
        {
            var currentScene = gameObject.scene;
            if (currentScene.isLoaded)
            {
                foreach (var root in currentScene.GetRootGameObjects())
                {
                    foreach (var uid in root.GetComponentsInChildren<UniqueID>(true))
                    {
                        if (uid.ID == uniqueID)
                        {
                            return uid.gameObject;
                        }
                    }
                }
            }
            return null;
        }


        /// <summary>
        /// Gets the object with the specified unique id in all scenes. Returns null if no object is found with specified id
        /// </summary>
        /// <param name="uniqueID">The unique id as string to find the object</param>
        /// <returns>The object found by the ID, or null if not found</returns>
        public static GameObject Get(string uniqueID)
        {
            return !string.IsNullOrEmpty(uniqueID) && s_globalIndexedObjects.TryGetValue(uniqueID, out UniqueID id) ? id?.gameObject : null;
        }
        
        /// <summary>
        /// Gets the unique id of the specified instance ID.
        /// </summary>
        /// <remarks>The instance ID should belong to a gameobject and not a component</remarks>
        /// <param name="instanceID">The instance ID of the gameobject</param>
        /// <returns>The unique id or null for the object</returns>
        public string this[int instanceID]
        {
            get
            {
                return m_uids.TryGetValue(instanceID, out string id) ? id : null;
            }
        }

        /// <summary>
        /// Gets the unique id of the specified instance ID.
        /// </summary>
        /// <remarks>The instance ID should belong to a gameobject and not a component</remarks>
        /// <param name="instanceID">The instance ID of the gameobject</param>
        /// <returns>The unique id or null for the object</returns>
        public static string GetUniqueID(int instanceID)
        {
            return s_globalUids.TryGetValue(instanceID, out string id) ? id : null;
        }

        /// <summary>
        /// Registers the specified unique id
        /// </summary>
        /// <param name="id">The id to be registered</param>
        public string Register(UniqueID id)
        {
            if (id && SetIDValue != null && id.gameObject.scene == gameObject.scene)
            {
                UniqueID otherId = null;
                if (string.IsNullOrEmpty(id.ID))
                {
                    //id.ID = GenerateNewID();
                    SetIDValue(id, GenerateFromScene(id) ?? GenerateNewID());
                    //Debug.Log($"[IDBookkeeper]: Object [{id.name}] has no ID: Generated new [{id.ID}]");
                    SetID(id);
                    //m_indexedObjects[id.ID] = id;
                }
                else if (m_indexedObjects.TryGetValue(id.ID, out otherId) && otherId != id)
                {
                    //Debug.Log($"[IDBookkeeper]: Found ID conflict for [{id.ID}]: {id.name} vs {otherId.name}");
                    if (otherId.Timestamp <= id.Timestamp)
                    {
                        //Debug.Log($"[IDBookkeeper]: Found same id with later timestamp: [{id.name} - {id.Timestamp}] vs [{otherId.name} - {otherId.Timestamp}]");
                        //id.ID = GenerateNewID();
                        SetIDValue(id, GenerateFromScene(id) ?? GenerateNewID());
                        SetID(id);
                        //m_indexedObjects[id.ID] = id;
                    }
                    else
                    {
                        //otherId.ID = GenerateNewID();
                        SetIDValue(otherId, GenerateFromScene(id) ?? GenerateNewID());
                        SetID(id);
                        SetID(otherId);
                        //m_indexedObjects[id.ID] = id;
                        //m_indexedObjects[otherId.ID] = otherId;
                        //m_uids[otherId.gameObject.GetInstanceID()] = otherId.ID;
                    }
                }
                else if (!otherId)
                {
                    //id.UpdateTimestamp();
                    SetID(id);
                    //m_indexedObjects[id.ID] = id;
                }

                //m_uids[id.gameObject.GetInstanceID()] = id.ID;

                return id.ID;
            }
            return null;
        }

        private string GenerateFromScene(UniqueID id)
        {
            var part1 = id.gameObject.GetHierarchyPath().GetHashCode();
            var meshFilter = id.GetComponent<MeshFilter>();
            var part2 = (meshFilter && meshFilter.sharedMesh ? meshFilter.sharedMesh.name : "none").GetHashCode();
            string finalPart = $"{id.transform.GetSiblingIndex()}-{id.gameObject.scene.path.GetHashCode()}-{part2}-{part1}";
            return s_globalIndexedObjects.ContainsKey(finalPart) ? null : finalPart;
        }

        private void SetID(UniqueID id)
        {
            m_indexedObjects[id.ID] = id;
            s_globalIndexedObjects[id.ID] = id;
            m_uids[id.gameObject.GetInstanceID()] = id.ID;
            s_globalUids[id.gameObject.GetInstanceID()] = id.ID;
        }

        private static string GenerateNewID()
        {
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Searches and registers all unique ids in the hierarchy of the current scene
        /// </summary>
        public static void IndexCurrentScene()
        {
            IndexScene(SceneManager.GetActiveScene());
        }

        /// <summary>
        /// Clears all currently registered unique ids
        /// </summary>
        public void Clear()
        {
            //if (Application.isEditor)
            //{
            if (m_uids == null)
            {
                m_uids = new DictionaryOfIntAndString();
            }
            else
            {
                foreach(var key in m_uids.Keys)
                {
                    s_globalUids.Remove(key);
                }
                m_uids.Clear();
            }
            if (m_indexedObjects == null)
            {
                m_indexedObjects = new DictionaryOfStringAndUniqueId();
            }
            else
            {
                foreach(var key in m_indexedObjects.Keys)
                {
                    s_globalIndexedObjects.Remove(key);
                }
                m_indexedObjects.Clear();
            }
            //}
        }

        /// <summary>
        /// Searches and registers all unique ids in the hierarchy of the specified scene
        /// </summary>
        /// <param name="scene">The scene to be processed</param>
        public static void IndexScene(Scene scene)
        {
            if (scene.IsValid() && scene.isLoaded)
            {
                var idBookkeeper = GetSingleton(scene);
                idBookkeeper.Clear();
                //Debug.Log($"[IDBookkeeper]: Indexing scene [{scene.name}] ...");
                idBookkeeper.IndexSceneInternal(scene);
                //Debug.Log($"[IDBookkeeper]: Indexed scene [{scene.name}] success with {m_indexedObjects.Count} ids");
            }
        }

        private void IndexSceneInternal(Scene scene)
        {
            var gameObjects = scene.GetRootGameObjects();
            foreach (var gameObject in gameObjects)
            {
                foreach (var uniqueId in gameObject.GetComponentsInChildren<UniqueID>(true))
                {
                    Register(uniqueId);
                }
            }
        }

        public static string Register(GameObject gameObject)
        {
            Assert.IsNotNull(gameObject, "gameObject should not be null");
            var uniqueId = gameObject.GetComponent<UniqueID>();
            var idBookkeeper = GetSingleton(gameObject.scene);
            return idBookkeeper.Register(uniqueId ? uniqueId : gameObject.AddComponent<UniqueID>());
        }

        public static string Register(Component component)
        {
            Assert.IsNotNull(component, "component should not be null");
            var uniqueId = component.GetComponent<UniqueID>();
            var idBookkeeper = GetSingleton(component.gameObject.scene);
            return idBookkeeper.Register(uniqueId ? uniqueId : component.gameObject.AddComponent<UniqueID>());
        }

        /// <summary>
        /// Removes the id from the register
        /// </summary>
        /// <param name="id">The unique id as string to be removed from the register</param>
        /// <returns>True whether the removal took place, false otherwise or if not present in the register</returns>
        public bool RemoveID(string id)
        {
            if (m_indexedObjects.ContainsKey(id))
            {
                var gameObject = m_indexedObjects[id];
                return m_indexedObjects.Remove(id) && m_uids.Remove(gameObject.GetInstanceID()) 
                    && s_globalIndexedObjects.Remove(id) && s_globalUids.Remove(gameObject.GetInstanceID());
            }
            return false;
        }

        private void OnDestroy()
        {
            if (m_uids != null)
            {
                foreach (var key in m_uids.Keys)
                {
                    s_globalUids.Remove(key);
                }
            }
            if (m_indexedObjects != null)
            {
                foreach (var pair in m_indexedObjects)
                {
                    if (s_globalIndexedObjects.TryGetValue(pair.Key, out UniqueID existing) && existing == pair.Value)
                    {
                        s_globalIndexedObjects.Remove(pair.Key);
                    }
                }
            }

            Weavr.UnregisterSingleton(this);

            //if (s_instance == this)
            //{
            //    s_instance = null;
            //}
        }

        public void RegisterTable(IReferenceTable table)
        {
            foreach(var pair in table.IDs)
            {
                m_cachedObjects[pair.Key] = pair.Value.Item;
                m_tables[pair.Key] = table;
            }
            m_lastTable = table;
        }

        public void UnregisterTable(IReferenceTable table)
        {
            foreach(var pair in table.IDs)
            {
                m_cachedObjects.Remove(pair.Key);
                m_tables.Remove(pair.Key);
            }
            if(m_lastTable == table)
            {
                m_lastTable = null;
                if(m_tables.Count > 0)
                {
                    m_lastTable = m_tables.Values.GetEnumerator().Current;
                }
            }
        }

        public void SetReferenceValue(PropertyName id, Object value)
        {
            if (value is GameObject || value is Component || value == null) {
                if (!m_tables.TryGetValue(id, out IReferenceTable table) && m_lastTable != null)
                {
                    table = m_lastTable;
                    m_tables[id] = table;
                }
                if (table != null)
                {
                    m_cachedObjects[id] = value;
                    if(value == null)
                    {
                        table.IDs[id] = new SceneItem();
                        return;
                    }
                    if(!table.IDs.TryGetValue(id, out SceneItem sceneItem) || sceneItem.Item != value)
                    {
                        sceneItem.uniqueId = value is GameObject go ? Register(go) : Register(value as Component);
                        sceneItem.Update(value);
                    }
                    table.IDs[id] = sceneItem;
                }
            }
        }

        public Object GetReferenceValue(PropertyName id, out bool isValid)
        {
            isValid = false;
            if(m_cachedObjects.TryGetValue(id, out Object obj)/* && obj*/)
            {
                isValid = true;
                return obj;
            }
            if(!m_tables.TryGetValue(id, out IReferenceTable table) && m_lastTable != null && m_lastTable.IDs.ContainsKey(id))
            {
                table = m_lastTable;
                m_tables[id] = table;
            }
            if (table != null && table.IDs.TryGetValue(id, out SceneItem sceneItem) && sceneItem.IsValid)
            {
                var resultGO = Get(sceneItem.uniqueId);
                if (resultGO)
                {
                    var result = sceneItem.GetObject(resultGO);
                    m_cachedObjects[id] = result;
                    isValid = true;
                    return result;
                }
            }
            return null;
        }

        public void ClearReferenceValue(PropertyName id)
        {
            if (m_cachedObjects.ContainsKey(id))
            {
                m_cachedObjects.Remove(id);
            }
            if(m_tables.TryGetValue(id, out IReferenceTable table))
            {
                table?.IDs.Remove(id);
                m_tables.Remove(id);
            }
        }
    }
}