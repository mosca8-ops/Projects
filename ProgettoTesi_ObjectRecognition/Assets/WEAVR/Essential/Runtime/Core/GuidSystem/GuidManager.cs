using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR
{
    public class GuidManager
    {

        #region [  STATIC PART  ]

        private static GuidManager s_instance;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static GuidManager GetInstance()
        {
            if (s_instance == null)
            {
                s_instance = new GuidManager();
            }
            return s_instance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static GuidManager GetUpdatedInstance()
        {
            if (s_instance == null)
            {
                s_instance = new GuidManager();
            }
            if (s_instance.RequiresUpdate)
            {
                s_instance.Update();
            }
            return s_instance;
        }

        
        public static IReadOnlyDictionary<Guid, GameObject> GameObjects => GetUpdatedInstance().GetGameObjects();

        public static IReadOnlyDictionary<Guid, object> Objects => GetUpdatedInstance().m_allGuids;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Register(Guid guid, GameObject gameObject) => GetInstance().RegisterInternal(guid, gameObject);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Register(Guid guid, object genericObject) => GetInstance().RegisterInternal(guid, genericObject);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Register(IGuidProvider guidProvider) => GetInstance().RegisterInternal(guidProvider);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool RegisterWeak(IWeakGuid weakGuid) => GetInstance().RegisterWeakInternal(weakGuid);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameObject GetGameObject(Guid guid) => GetInstance().GetGameObjectInternal(guid);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void UpdateFromScenes() => GetUpdatedInstance().UpdateFromScenesInternal();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object GetObject(Guid guid) => GetUpdatedInstance().GetObjectInternal(guid);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object GetWeakGuid(Guid guid) => GetInstance().GetWeakGuidInternal(guid);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static object GetWeakOrNormalGuid(Guid guid) => GetInstance().GetWeakGuidInternal(guid) ?? GetInstance().GetObjectInternal(guid);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClearAll() => GetInstance().ClearAllInternal();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Unregister(Guid guid) => s_instance?.UnregisterInternal(guid);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scene GetSceneOfGuid(Guid guid) => GetInstance().GetSceneOfGuidInternal(guid);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsGuid(Guid guid) => s_instance?.ContainsGuidInternal(guid) ?? false;

        #endregion

        private Dictionary<Guid, object> m_allGuids;
        private Dictionary<Guid, GameObject> m_gameObjects;

        /// <summary>
        /// Weak guids are guid owners which are not sure whether to be registered or not to the manager
        /// </summary>
        private List<IWeakGuid> m_weakGuids;
        private bool m_shouldUpdateGameObjects;

        public bool RequiresUpdate => m_weakGuids.Count > 0;

        private GuidManager()
        {
            // Hide the default constructor
            m_allGuids = new Dictionary<Guid, object>();
            m_gameObjects = new Dictionary<Guid, GameObject>();
            m_weakGuids = new List<IWeakGuid>();
        }

        private Dictionary<Guid, GameObject> GetGameObjects()
        {
            if (m_shouldUpdateGameObjects)
            {
                m_shouldUpdateGameObjects = false;
                m_gameObjects.Clear();

                foreach (var obj in m_allGuids)
                {
                    if (obj.Value is Component c && c)
                    {
                        m_gameObjects[obj.Key] = c.gameObject;
                    }
                }
            }
            return m_gameObjects;
        }

        private void Update()
        {
            UpdateWeakGuids();
        }

        private void UpdateFromScenesInternal()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                foreach(var root in SceneManager.GetSceneAt(i).GetRootGameObjects())
                {
                    foreach(var guid in root.GetComponentsInChildren<GuidComponent>(true))
                    {
                        Register(guid.Guid, guid);
                    }
                }
            }
        }

        private bool RegisterInternal(Guid guid, GameObject gameObject)
        {
            if (guid == Guid.Empty)
            {
                WeavrDebug.LogError(this, "Cannot register empty guid");
            }
            else if (!gameObject)
            {
                WeavrDebug.LogError(this, "Cannot register Null GameObject");
            }
            else if (m_gameObjects.TryGetValue(guid, out GameObject existing) && existing != gameObject)
            {
                WeavrDebug.LogError(this, $"There is already a GameObject '{existing.name}' registered with the Guid '{guid}'");
            }
            else if (!existing)
            {
                RemoveWeakGuid(guid);
                m_gameObjects.Add(guid, gameObject);
                m_allGuids.Add(guid, gameObject);
                return true;
            }
            return false;
        }

        private void RemoveWeakGuid(Guid guid)
        {
            for (int i = 0; i < m_weakGuids.Count; i++)
            {
                if (m_weakGuids[i] == null || m_weakGuids[i].Guid == guid)
                {
                    m_weakGuids.RemoveAt(i);
                    return;
                }
            }
        }

        private bool RegisterInternal(Guid guid, object genericObject)
        {
            if (guid == Guid.Empty)
            {
                WeavrDebug.LogError(this, "Cannot register empty guid");
            }
            else if (genericObject == null)
            {
                WeavrDebug.LogError(this, "Cannot register Null Object");
            }
            else if (m_allGuids.TryGetValue(guid, out object existing) && !Equals(existing, genericObject))
            {
                WeavrDebug.LogError(this, $"There is already an Object '{existing}' registered with the Guid '{guid}'");
            }
            else if (existing == null)
            {
                RemoveWeakGuid(guid);
                m_allGuids.Add(guid, genericObject);
                m_shouldUpdateGameObjects |= genericObject is Component;
                return true;
            }
            return false;
        }

        private bool RegisterInternal(IGuidProvider guidProvider)
        {
            var guid = guidProvider.Guid;
            if(guidProvider is Component c)
            {
                if(!m_gameObjects.TryGetValue(guid, out GameObject go))
                {
                    m_gameObjects.Add(guid, c.gameObject);
                    m_allGuids.Add(guid, c.gameObject);
                    return true;
                }
                
                if(go && go != c.gameObject)
                {
                    if (Application.isPlaying)
                    {
                        WeavrDebug.LogError(this, $"Guid collision detected between {go} and {c.gameObject}");
                    }
                    else
                    {
                        WeavrDebug.LogWarning(this, $"Guid collision detected between {go} and {c.gameObject}");
                    }
                    return false;
                }

                return true;
            }
            
            if(!m_allGuids.TryGetValue(guid, out object existing))
            {
                m_allGuids.Add(guid, guidProvider);
            }
            else if(existing != null && !Equals(existing, guidProvider))
            {
                if (Application.isPlaying)
                {
                    WeavrDebug.LogError(this, $"Guid collision detected between {existing} and {guidProvider}");
                }
                else
                {
                    WeavrDebug.LogWarning(this, $"Guid collision detected between {existing} and {guidProvider}");
                }
                return false;
            }
            return true;
        }

        private bool RegisterWeakInternal(IWeakGuid weakGuid)
        {
            if(weakGuid == null || m_weakGuids.Contains(weakGuid))
            {
                return false;
            }
            m_weakGuids.Add(weakGuid);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsGuidInternal(Guid guid) => m_allGuids.ContainsKey(guid);

        public GameObject GetGameObjectInternal(Guid guid)
        {
            if (m_gameObjects.TryGetValue(guid, out GameObject go))
            {
                return go;
            }
            UpdateWeakGuids();
            if (GetObjectInternal(guid) is Component component && component)
            {
                m_gameObjects[guid] = component.gameObject;
                RemoveWeakGuid(guid);
                return component.gameObject;
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object GetObjectInternal(Guid guid) => m_allGuids.TryGetValue(guid, out object obj) ? obj : default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IWeakGuid GetWeakGuidInternal(Guid guid) => m_weakGuids.Find(w => w.Guid == guid);


        private void UpdateWeakGuids()
        {
            if(m_weakGuids.Count == 0)
            {
                return;
            }

            for (int i = 0; i < m_weakGuids.Count; i++)
            {
                if(m_weakGuids[i] == null)
                {
                    m_weakGuids.RemoveAt(i--);
                }
                else
                {
                    m_weakGuids[i].UpdateState();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearAllInternal()
        {
            m_allGuids.Clear();
            m_gameObjects.Clear();
            m_weakGuids.Clear();
        }

        public void UnregisterInternal(Guid guid)
        {
            m_allGuids.Remove(guid);
            m_gameObjects.Remove(guid);
            RemoveWeakGuid(guid);
        }

        public Scene GetSceneOfGuidInternal(Guid guid)
        {
            if (m_gameObjects.TryGetValue(guid, out GameObject go))
            {
                return go.scene;
            }
            return new Scene();
        }
    }
}
