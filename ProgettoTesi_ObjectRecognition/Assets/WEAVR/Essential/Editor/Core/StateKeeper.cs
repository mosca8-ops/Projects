using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Core
{
    public class StateKeeper : ScriptableObject
    {
        private Dictionary<Component, ComponentState> m_allStates;

        private void OnEnable()
        {
            if(m_allStates == null)
            {
                m_allStates = new Dictionary<Component, ComponentState>();
            }
        }

        private void OnDisable()
        {
            RestoreAll();
        }

        public void SaveState(object caller, GameObject gameObject)
        {
            foreach(var component in gameObject.GetComponents<Component>())
            {
                SaveState(caller, component);
            }
        }

        public void RestoreState(object caller, GameObject gameObject)
        {
            foreach (var component in gameObject.GetComponents<Component>())
            {
                RestoreState(caller, component);
            }
        }

        public void RestoreDefaultState(GameObject gameObject)
        {
            foreach (var component in gameObject.GetComponents<Component>())
            {
                RestoreDefaultState(component);
            }
        }

        public void SaveState(object caller, Component component)
        {
            if(!m_allStates.TryGetValue(component, out ComponentState state))
            {
                state = new ComponentState(component);
                m_allStates[component] = state;
            }
            state.SaveState(caller);
        }

        public void RestoreState(object caller, Component component)
        {
            if (!m_allStates.TryGetValue(component, out ComponentState state))
            {
                state = new ComponentState(component);
                m_allStates[component] = state;
            }
            state.RestoreState(caller);
        }

        public void RestoreDefaultState(Component component)
        {
            if (!m_allStates.TryGetValue(component, out ComponentState state))
            {
                state = new ComponentState(component);
                m_allStates[component] = state;
            }
            state.ForceRestore();
            state.Clear();
        }

        public void RestoreAll()
        {
            foreach(var value in m_allStates.Values)
            {
                value.ForceRestore();
                value.Clear();
            }
            m_allStates.Clear();
        }

        private class ComponentState
        {
            private Component m_component;
            private SerializedObject m_rootObj;
            private List<KeyValuePair<object, SerializedObject>> m_serObjList;

            public ComponentState(Component component)
            {
                m_component = component;
                m_rootObj = new SerializedObject(component);
                m_serObjList = new List<KeyValuePair<object, SerializedObject>>();
            }

            public void SaveState(object client, bool update = false)
            {
                for (int i = 0; i < m_serObjList.Count; i++)
                {
                    if(m_serObjList[i].Key == client)
                    {
                        if (update)
                        {
                            m_serObjList[i].Value.Update();
                        }
                        return;
                    }
                }
                var serObj = new SerializedObject(m_component);
                serObj.Update();
                m_serObjList.Add(new KeyValuePair<object, SerializedObject>(client, serObj));
            }

            public void RestoreState(object caller, bool dispose = false)
            {
                var components = m_component.gameObject.GetComponents(m_component.GetType());
                if(m_serObjList.Count == 0)
                {
                    Restore(m_rootObj, components);
                    return;
                }

                for (int i = m_serObjList.Count - 1; i >= 0; i--)
                {
                    if(m_serObjList[i].Key == caller)
                    {
                        Restore(m_serObjList[i].Value, components);
                        if (dispose)
                        {
                            m_serObjList[i].Value.Dispose();
                            m_serObjList.RemoveAt(i);
                        }
                        return;
                    }
                }
            }

            private void Restore(SerializedObject serObj, IEnumerable<Component> components)
            {
                foreach(var component in components)
                {
                    if(serObj.targetObject == component)
                    {
                        var curSerObj = new SerializedObject(component);
                        var iterator = serObj.GetIterator();
                        iterator.Next(true);
                        while(iterator.Next(iterator.propertyType == SerializedPropertyType.Generic))
                        {
                            curSerObj.CopyFromSerializedProperty(iterator);
                        }
                    }
                }
            }

            public void Clear()
            {
                m_serObjList.Clear();
            }

            public void ForceRestore()
            {
                Restore(m_rootObj, m_component.gameObject.GetComponents(m_component.GetType()));
            }
        }
    }
}