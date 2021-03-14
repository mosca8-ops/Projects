namespace TXT.WEAVR.Core
{
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using TXT.WEAVR.Utility;
    using UnityEngine;

    public class ObjectRetriever
    {
        public static System.Func<string, System.Type, Object> GetAssetGloballyFunctor;

        private static ObjectRetriever s_instance;
        /// <summary>
        /// Gets the <see cref="ObjectRetriever"/> having the currently set <see cref="XmlProcedure"/>
        /// </summary>
        public static ObjectRetriever Current {
            get {
                if(s_instance == null) {
                    s_instance = new ObjectRetriever();
                }
                return s_instance;
            }
        }

        private static GameObject s_weavr;
        /// <summary>
        /// Gets the WEAVR gameobject
        /// </summary>
        public static GameObject WEAVR {
            get {
                if(s_weavr == null) {
                    s_weavr = Weavr.TryGetWEAVRInCurrentScene(out Transform wt) ? wt.gameObject : null;
                    if(s_weavr == null) {
                        s_weavr = new GameObject("WEAVR");
                    }
                }
                return s_weavr;
            }
        }

        private Dictionary<string, Object> m_globalObjectsCache;
        private Dictionary<string, Object> m_globalObjects;
        private List<GameObject> m_gameObjects;
        
        public bool LoadCompleted { get; private set; }

        public IEnumerable<GameObject> ProcedureGameObjects => m_gameObjects;

        private ObjectRetriever() {
            m_globalObjectsCache = new Dictionary<string, Object>();
            m_globalObjects = new Dictionary<string, Object>();
            m_gameObjects = new List<GameObject>();
            LoadCompleted = false;
        }

        public void Clear() {
            if(m_globalObjectsCache != null) {
                m_globalObjectsCache.Clear();
            }
            if(m_globalObjects != null) {
                m_globalObjects.Clear();
            }
        }

        ///// <summary>
        ///// Sets the current <see cref="XmlProcedure"/> in use.
        ///// </summary>
        ///// <param name="procedure">The <see cref="XmlProcedure"/> to get the objects for</param>
        //public void SetCurrentProcedure(XmlProcedure procedure) {
        //    LoadCompleted = false;

        //    m_currentProcedure = procedure;
        //    m_globalObjectsCache = new Dictionary<string, Object>();
        //    m_globalObjects = new Dictionary<string, Object>();
        //    m_gameObjects = new List<GameObject>();
        //    // Retrieve all data
        //    foreach(var obj in procedure.DataObjects) {
        //        if(obj is SceneObject) {
        //            var sceneObject = (SceneObject)obj;
        //            var gameObject = GetGameObject(sceneObject.UniqueId, sceneObject.HierarchyPath);
        //            if (gameObject != null && !string.IsNullOrEmpty(sceneObject.ComponentType)) {
        //                System.Type componentType = System.Type.GetType(sceneObject.ComponentType);
        //                if(componentType == null) {
        //                    Debug.LogErrorFormat("Unable to find component type '{0}'", sceneObject.ComponentType);
        //                    continue;
        //                }
        //                m_globalObjectsCache[obj.Id] = gameObject.GetComponent(componentType);
        //            }
        //            else {
        //                m_globalObjectsCache[obj.Id] = gameObject;
        //            }
        //            if(gameObject != null)
        //            {
        //                m_gameObjects.Add(gameObject);
        //            }
        //        }
        //        else if(obj is AssetObject) {
        //            var assetObject = (AssetObject)obj;
        //            m_globalObjectsCache[obj.Id] = GetAsset(assetObject.RelativePath, assetObject.Type);
        //        }
        //    }

        //    LoadCompleted = true;
        //}


        //public IEnumerable<GameObject> GetAllProcedureGameObjects(XmlProcedure procedure)
        //{
        //    return m_gameObjects;
        //}

        /// <summary>
        /// Gets the object with the specified id
        /// </summary>
        /// <param name="id">The id of the object</param>
        /// <returns>The found object or null if not found</returns>
        public Object Get(string id) {
            Object obj = null;
            if(m_globalObjectsCache.TryGetValue(id, out obj) || m_globalObjects.TryGetValue(id, out obj)) {
                return obj;
            }
            Debug.LogErrorFormat("Unable to find object with id: {0}", id);
            return null;
        }

        /// <summary>
        /// Tries to get the object with the specified id
        /// </summary>
        /// <param name="id">The id of the object</param>
        /// <param name="obj">The found object or null</param>
        /// <returns>Whether the object was found or not</returns>
        public bool TryGetObject(string id, out Object obj)
        {
            return m_globalObjectsCache.TryGetValue(id, out obj) || m_globalObjects.TryGetValue(id, out obj);
        }

        /// <summary>
        /// Gets the <typeparamref name="T"/> with the specified id
        /// </summary>
        /// <param name="id">The id of the <typeparamref name="T"/></param>
        /// <typeparam name="T">The type of the object</typeparam>
        /// <returns>The found object or null if not found</returns>
        public T Get<T>(string id) where T : Object {
            return Get(id) as T;
        }

        /// <summary>
        /// Gets the <see cref="Component"/> from the <see cref="GameObject"/> represented by the <paramref name="objectId"/> 
        /// and listed in current <see cref="XmlProcedure"/> in use.
        /// </summary>
        /// <typeparam name="T">The <see cref="Component"/> type to search for</typeparam>
        /// <param name="id">The <see cref="String"/> id of the object</param>
        /// <returns>The found <see cref="Component"/> or null if not found</returns>
        public T GetComponent<T>(string id) where T: Component {
            Object obj = null;
            if(m_globalObjectsCache.TryGetValue(id, out obj) && obj is T) {
                return (T)obj;
            }
            else if(obj is GameObject) {
                return ((GameObject)obj).GetComponent<T>();
            }
            Debug.LogErrorFormat("Unable to find component with id: {0}", id);
            return null;
        }

        /// <summary>
        /// Gets the <see cref="Component"/> from the <see cref="GameObject"/> represented by the <paramref name="objectId"/> 
        /// and listed in current <see cref="XmlProcedure"/> in use.
        /// </summary>
        /// <param name="id">The <see cref="String"/> id of the object</param>
        /// <returns>The found <see cref="Component"/> or null if not found</returns>
        public Component GetComponent(string id) {
            Object obj = null;
            if (m_globalObjectsCache.TryGetValue(id, out obj) && obj is Component) {
                return (Component)obj;
            }
            else if (obj is GameObject) {
                return ((GameObject)obj).GetComponent<Component>();
            }
            Debug.LogErrorFormat("Unable to find component with id: {0}", id);
            return null;
        }

        /// <summary>
        /// Gets the <see cref="GameObject"/> represented by the <paramref name="id"/> 
        /// and listed in current <see cref="XmlProcedure"/> in use.
        /// </summary>
        /// <param name="id">The <see cref="String"/> id of the object</param>
        /// <returns>The found <see cref="GameObject"/> or null if not found</returns>
        public GameObject GetGameObject(string id) {
            Object obj = null;
            if(m_globalObjectsCache.TryGetValue(id, out obj) && (obj is GameObject || obj is Component)) {
                return obj is GameObject ? (GameObject)obj : ((Component)obj).gameObject;
            }
            else if(m_globalObjects.TryGetValue(id, out obj) && (obj is GameObject || obj is Component)) {
                return obj is GameObject ? (GameObject)obj : ((Component)obj).gameObject;
            }
            Debug.LogErrorFormat("Unable to find game object with id: {0}", id);
            return null;
        }

        /// <summary>
        /// Gets the <see cref="Object"/> Asset represented by the <paramref name="id"/> 
        /// and listed in current <see cref="XmlProcedure"/> in use.
        /// </summary>
        /// <param name="id">The <see cref="String"/> id of the object</param>
        /// <returns>The found <see cref="Object"/> Asset or null if not found</returns>
        public Object GetAsset(string id) {
            return Get(id);
        }

        public static Component GetComponent(string uniqueId, string objectScenePath, System.Type type) {
            GameObject gameObject = GetGameObject(uniqueId, objectScenePath);
            return gameObject != null ? gameObject.GetComponent(type) : null;
        }

        public static Component GetComponent(string uniqueId, string objectScenePath, string componentType) {
            GameObject gameObject = GetGameObject(uniqueId, objectScenePath);
            return gameObject != null ? gameObject.GetComponent(componentType) : null;
        }

        public static T GetComponent<T>(string uniqueId, string objectScenePath) where T: Component {
            GameObject gameObject = GetGameObject(uniqueId, objectScenePath);
            return gameObject != null ? gameObject.GetComponent<T>() : null;
        }

        /// <summary>
        /// Gets the <see cref="GameObject"/> identified by the <paramref name="uniqueId"/> and/or by <paramref name="objectScenePath"/> 
        /// </summary>
        /// <param name="uniqueId">The <see cref="System.String"/> unique id of the <see cref="GameObject"/>s</param>
        /// <param name="objectScenePath">The <see cref="System.String"/> hierarchy path of the object</param>
        /// <returns>The found <see cref="GameObject"/> or null if not found</returns>
        public static GameObject GetGameObject(string uniqueId, string objectScenePath) {
            if (Current.m_globalObjectsCache.TryGetValue(uniqueId, out Object obj) && obj is GameObject go) {
                return go;
            }
            GameObject gameObject = string.IsNullOrEmpty(uniqueId) ? null : IDBookkeeper.Get(uniqueId);
            if(gameObject == null) {
                gameObject = string.IsNullOrEmpty(objectScenePath) ? null : SceneTools.GetGameObjectAtScenePath(objectScenePath);
                if(gameObject == null) {
                    gameObject = GameObject.Find(objectScenePath);
                    if(gameObject == null) {
                        Debug.LogErrorFormat("Unable to find gameobject either by unique id [{0}] nor by hierarchy path [{1}]",
                                     uniqueId, objectScenePath);
                        return null;
                    }
                }
            }
            if (gameObject != null) {
                if (!string.IsNullOrEmpty(uniqueId))
                {
                    Current.m_globalObjectsCache[uniqueId] = gameObject;
                }
                return gameObject;
            }
            return null;
        }

        /// <summary>
        /// Gets the <see cref="Object"/> Asset represented by the <paramref name="objectId"/> 
        /// and listed in <paramref name="xmlProcedure"/>
        /// </summary>
        /// <param name="xmlProcedure">The <see cref="XmlProcedure"/> with listed <see cref="Object"/> Assets</param>
        /// <param name="objectId">The <see cref="String"/> id of the object</param>
        /// <returns>The found <see cref="Object"/> Asset or null if not found</returns>
        public static Object GetAsset(string resourcesPath, string fullTypename) {
            
            if (!string.IsNullOrEmpty(resourcesPath) && resourcesPath.StartsWith("Assets/Resources/"))
            {
                resourcesPath = resourcesPath.Replace("Assets/Resources/", "");
            }

            
            Object asset = null;
            if (Current.m_globalObjectsCache.TryGetValue(resourcesPath, out asset)) {
                return asset;
            }
            if (string.IsNullOrEmpty(fullTypename)) {
                Debug.LogErrorFormat("The Asset Object at '{0}' does not have a defined type", resourcesPath);
                return null;
            }

            System.Type type = System.Type.GetType(fullTypename);

            // For now load it from resources
            if (!string.IsNullOrEmpty(resourcesPath)) {
                string pathWithoutExtension = Path.ChangeExtension(resourcesPath, "");
                asset = Resources.Load(pathWithoutExtension.Substring(0, pathWithoutExtension.Length - 1), type);
                if(asset != null) {
                    Current.m_globalObjectsCache[resourcesPath] = asset;
                    return asset;
                }
                else
                {
                    //Debug.Log($"Searching the asset globally: [{resourcesPath}]");
                    asset = GetAssetGloballyFunctor?.Invoke(resourcesPath, type);
                    if(asset != null)
                    {
                        Current.m_globalObjectsCache[resourcesPath] = asset;
                        return asset;
                    }
                }
                // Load from asset bundle
                // TODO: Load Asset from Asset Bundle
            }

            Debug.LogErrorFormat("Unable to find asset object by resource relative path [{0}]", resourcesPath);
            return null;
        }
    }
}