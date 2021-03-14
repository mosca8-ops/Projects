using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TXT.WEAVR.Core.DataTypes;
using UnityEngine;

namespace TXT.WEAVR.Core
{
    [Stateless]
    [DoNotExpose]
    [AddComponentMenu("WEAVR/States Logic/State Manager")]
    public class StateManager : MonoBehaviour, IWeavrSingleton
    {
        #region [  STATIC PART  ]

        private static Dictionary<object, bool> s_statefulTypes = new Dictionary<object, bool>();
        private static Dictionary<Type, Type> s_componentStateTypes = new Dictionary<Type, Type>();

        public static AbstractComponentState GetStateContainer(Component component)
        {
            Type type = component.GetType();
            if (SpecialStateContainers.SpecialComponentStates.TryGetValue(type, out Func<Component, AbstractComponentState> constructor))
            {
                return constructor(component);
            }
            else if (s_componentStateTypes.TryGetValue(type, out Type componentStateType))
            {
                return Activator.CreateInstance(componentStateType) as AbstractComponentState;
            }
            var currentSingleton = component.TryGetSingleton<StateManager>();
            if(currentSingleton && currentSingleton.useReflectedStates)
            {
                return new ReflectionComponentState();
            }
            return new EmptyComponentState();
        }

        public static bool IsStateful(UnityEngine.Object obj)
        {
            return obj && IsStateful(obj.GetType());
        }

        public static bool IsStateful(Type type)
        {
            if(s_statefulTypes.TryGetValue(type, out bool stateful))
            {
                return stateful;
            }
            stateful = type.GetCustomAttribute(typeof(StatelessAttribute)) == null;
            s_statefulTypes.Add(type, stateful);
            return stateful;
        }

        public static bool IsStateful(FieldInfo fieldInfo)
        {
            if (s_statefulTypes.TryGetValue(fieldInfo, out bool stateful))
            {
                return stateful;
            }
            stateful = fieldInfo.GetCustomAttribute(typeof(StatelessAttribute)) == null;
            s_statefulTypes.Add(fieldInfo, stateful);
            return stateful;
        }

        public static bool IsStateful(PropertyInfo propertyInfo)
        {
            bool stateful = false;
            if (s_statefulTypes.TryGetValue(propertyInfo, out stateful))
            {
                return stateful;
            }
            stateful = propertyInfo.GetCustomAttribute(typeof(StatelessAttribute)) == null;
            s_statefulTypes.Add(propertyInfo, stateful);
            return stateful;
        }

        private static StateManager s_instance;

        public static StateManager Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = Weavr.GetInCurrentScene<StateManager>();
                    if (s_instance == null)
                    {
                        // If no object is active, then create a new one
                        GameObject go = new GameObject("StateManager");
                        s_instance = go.AddComponent<StateManager>();
                        s_instance.transform.SetParent(ObjectRetriever.WEAVR.transform, false);
                    }
                    s_instance.Initialize();
                }
                return s_instance;
            }
        }

        #endregion

        #region [  HIDDEN FIELDS  ]

        private bool m_initialized;

        private StatesStack m_defaultStack;
        private Dictionary<int, IStatesStack> m_stacks;

        public HashSet<GameObject> StatefulSceneObjects { get; private set; } = new HashSet<GameObject>();

        #endregion

        #region [  INSPECTOR FIELDS  ]
        [Tooltip("Find all component states, or use only known ones. Enabling it will take longer to load the scene")]
        public bool findAllStateTypes = false;
        [Tooltip("In case the state container is not defined, if enabled an automatic reflection state container will be used instead, " +
            "however in this case the correct behaviour is not guaranteed")]
        public bool useReflectedStates = false;
        [Tooltip("Whether to log or not events such as gameobject being snapshot or not")]
        public bool logEvents = false;
        #endregion

        public int GroupIndex => m_defaultStack?.GroupIndex ?? 0;

        private void Awake()
        {
            Initialize();
        }

        public void Register(StatefulGameObject statefulGO)
        {
            StatefulSceneObjects.Add(statefulGO.gameObject);
            if (statefulGO.includeChildren)
            {
                RegisterGameObjectHierarchy(statefulGO.transform);
            }
        }

        private void RegisterGameObjectHierarchy(Transform t)
        {
            for (int i = 0; i < t.childCount; i++)
            {
                var child = t.GetChild(i);
                var stateful = child.GetComponent<StatefulGameObject>();
                if (!stateful)
                {
                    if (!child.GetComponent<StatelessGameObject>())
                    {
                        StatefulSceneObjects.Add(child.gameObject);
                    }
                }
                else if (stateful.enabled)
                {
                    StatefulSceneObjects.Add(child.gameObject);
                }
                else if(!stateful.includeChildren)
                {
                    continue;
                }
                RegisterGameObjectHierarchy(child);
            }
        }

        public void Unregister(StatefulGameObject statefulGO)
        {
            StatefulSceneObjects.Remove(statefulGO.gameObject);
            if (statefulGO.includeChildren)
            {
                UnregisterGameObjectHierarchy(statefulGO.transform);
            }
        }

        private void UnregisterGameObjectHierarchy(Transform t)
        {
            for (int i = 0; i < t.childCount; i++)
            {
                var child = t.GetChild(i);
                var stateful = child.GetComponent<StatefulGameObject>();
                if (!stateful)
                {
                    StatefulSceneObjects.Remove(child.gameObject);
                }
                else if (stateful.enabled)
                {
                    StatefulSceneObjects.Remove(child.gameObject);
                }
                else if (!stateful.includeChildren)
                {
                    continue;
                }
                UnregisterGameObjectHierarchy(child);
            }
        }

        private void Initialize()
        {
            if (m_initialized) { return; }

            m_defaultStack = new StatesStack();
            m_stacks = new Dictionary<int, IStatesStack>();

            if (findAllStateTypes && s_componentStateTypes.Count == 0)
            {
                Type componentType = null;
                foreach (var pair in TypeRetriever.GetAttributes<SpecialStateContainerAttribute>())
                {
                    componentType = pair.Value.ComponentType;
                    if (componentType != null && componentType.IsSubclassOf(typeof(AbstractComponentState)))
                    {
                        s_componentStateTypes[componentType] = pair.Key;
                    }
                }
            }

            m_initialized = true;
        }

        public IStatesStack GetStack(UnityEngine.Object client)
        {
            return client ? GetStack(client.GetInstanceID()) : null;
        }

        public IStatesStack GetStack(int id)
        {
            if(!m_stacks.TryGetValue(id, out IStatesStack stack))
            {
                stack = gameObject.activeInHierarchy ? new StatesStack() as IStatesStack : new VoidStack();
                m_stacks[id] = stack;
            }
            return stack;
        }

        public void RemoveStack(IStatesStack stack)
        {
            List<int> indicesToRemove = new List<int>();
            foreach(var pair in m_stacks)
            {
                if(pair.Value == stack)
                {
                    indicesToRemove.Add(pair.Key);
                }
            }

            stack.Clear();
            foreach(var index in indicesToRemove)
            {
                m_stacks.Remove(index);
            }
        }

        public void RemoveStack(int id)
        {
            if(m_stacks.TryGetValue(id, out IStatesStack stack))
            {
                RemoveStack(stack);
            }
        }

        public IStatesStack CreateInitialSnapshot(IEnumerable<GameObject> gameObjects, bool includeStatefulSceneGameObjects)
        {
            var stack = new StatesStack();
            stack.Register(gameObjects);
            if (includeStatefulSceneGameObjects)
            {
                stack.Register(StatefulSceneObjects);
            }
            stack.Snapshot(true);
            return stack;
        }

        #region [  DEFAULT STACK PART  ]

        public void MoveNext()
        {
            m_defaultStack?.MoveNext();
        }

        public void MovePrevious()
        {
            m_defaultStack?.MovePrevious();
        }

        public void Register(GameObject gameobject)
        {
            m_defaultStack?.Register(gameobject);
        }

        public void Unregister(GameObject gameobject)
        {
            m_defaultStack?.Unregister(gameobject);
        }

        public void Snapshot(bool overwriteExisting)
        {
            m_defaultStack?.Snapshot(overwriteExisting);
        }

        public void Snapshot(int groupId, bool overwriteExisting)
        {
            m_defaultStack?.Snapshot(groupId, overwriteExisting);
        }

        public void Snapshot(GameObject gameobject, bool overwriteExisting)
        {
            m_defaultStack?.Snapshot(gameobject, overwriteExisting);
        }

        public void Snapshot(int groupId, GameObject gameobject, bool overwriteExisting)
        {
            m_defaultStack?.Snapshot(groupId, gameobject, overwriteExisting);
        }

        public void Restore()
        {
            m_defaultStack?.Restore();
        }

        public void Restore(int groupId)
        {
            m_defaultStack?.Restore(groupId);
        }

        public void RestoreAndMoveTo(int groupIndex)
        {
            m_defaultStack?.RestoreAndMoveTo(groupIndex);
        }

        #endregion

        #region [  STATES STACK CLASS  ]

        public interface IStatesStack
        {
            int GroupIndex { get; }

            void Register(IEnumerable<GameObject> gameobjects);
            void Restore(int groupId);
            void Snapshot(bool overwriteExisting);
            void Snapshot(GameObject gameobject, bool overwriteExisting);
            void Snapshot(int groupId, bool overwriteExisting);
            void Snapshot(int groupId, GameObject gameobject, bool overwriteExisting);
            void Clear();
        }

        public class VoidStack : IStatesStack
        {
            public int GroupIndex => 0;

            public void Clear()
            {
                
            }

            public void Register(IEnumerable<GameObject> gameobjects)
            {
                
            }

            public void Restore(int groupId)
            {
                
            }

            public void Snapshot(bool overwriteExisting)
            {
                
            }

            public void Snapshot(GameObject gameobject, bool overwriteExisting)
            {
                
            }

            public void Snapshot(int groupId, bool overwriteExisting)
            {
                
            }

            public void Snapshot(int groupId, GameObject gameobject, bool overwriteExisting)
            {
                
            }
        }

        public class StatesStack : IStatesStack
        {
            private int m_groupId;
            private List<Dictionary<GameObject, GameObjectState>> m_gameObjectsStates;
            private List<GameObject> m_persistentGameObjects;

            public int GroupIndex => m_groupId;

            internal StatesStack()
            {
                m_gameObjectsStates = new List<Dictionary<GameObject, GameObjectState>>();
                m_persistentGameObjects = new List<GameObject>();
            }

            public void MoveNext()
            {
                while (m_gameObjectsStates.Count <= m_groupId)
                {
                    m_gameObjectsStates.Add(new Dictionary<GameObject, GameObjectState>());
                }
                m_groupId++;
            }

            public void MovePrevious()
            {
                m_groupId = m_groupId <= 0 ? 0 : m_groupId - 1;
            }

            public void Register(GameObject gameobject)
            {
                if (!m_persistentGameObjects.Contains(gameobject))
                {
                    if (Instance.logEvents)
                    {
                        WeavrDebug.Log(nameof(StateManager), $"Registering {gameobject.name}");
                    }
                    m_persistentGameObjects.Add(gameobject);
                }
            }

            public void Register(IEnumerable<GameObject> gameobjects)
            {
                foreach (var go in gameobjects)
                {
                    if (!m_persistentGameObjects.Contains(go))
                    {
                        if (Instance.logEvents)
                        {
                            WeavrDebug.Log(nameof(StateManager), $"Registering {go.name}");
                        }
                        m_persistentGameObjects.Add(go);
                    }
                }
            }

            public void Unregister(GameObject gameobject)
            {
                m_persistentGameObjects.Remove(gameobject);
            }

            public void Snapshot(bool overwriteExisting)
            {
                for (int i = 0; i < m_persistentGameObjects.Count; i++)
                {
                    if (m_persistentGameObjects[i] != null)
                    {
                        Snapshot(m_persistentGameObjects[i], overwriteExisting);
                    }
                    else
                    {
                        m_persistentGameObjects.RemoveAt(i--);
                    }
                }
            }

            public void Snapshot(int groupId, bool overwriteExisting)
            {
                for (int i = 0; i < m_persistentGameObjects.Count; i++)
                {
                    if (m_persistentGameObjects[i] != null)
                    {
                        Snapshot(groupId, m_persistentGameObjects[i], overwriteExisting);
                    }
                    else
                    {
                        m_persistentGameObjects.RemoveAt(i--);
                    }
                }
            }

            public void Snapshot(GameObject gameobject, bool overwriteExisting)
            {
                Snapshot(m_groupId, gameobject, overwriteExisting);
            }

            public void Snapshot(int groupId, GameObject gameobject, bool overwriteExisting)
            {
                while (m_gameObjectsStates.Count <= groupId)
                {
                    m_gameObjectsStates.Add(new Dictionary<GameObject, GameObjectState>());
                }
                if (m_gameObjectsStates[groupId].TryGetValue(gameobject, out GameObjectState state))
                {
                    if (overwriteExisting)
                    {
                        state.Snapshot();
                    }
                }
                else
                {
                    state = new GameObjectState(gameobject);
                    state.Snapshot();
                    m_gameObjectsStates[groupId][gameobject] = state;
                }
            }

            public void Restore()
            {
                Restore(m_groupId);
            }

            public void Restore(int groupId)
            {
                if (m_gameObjectsStates.Count <= groupId) { return; }

                foreach (var pair in m_gameObjectsStates[groupId])
                {
                    pair.Value.Restore();
                }
            }

            public void RestoreAndMoveTo(int groupIndex)
            {
                m_groupId = Mathf.Min(groupIndex, m_gameObjectsStates.Count - 1);
                Restore(m_groupId);
            }

            public void Clear()
            {
                m_groupId = 0;
                m_gameObjectsStates.Clear();
                m_persistentGameObjects.Clear();
            }
        }

        #endregion

    }
}
