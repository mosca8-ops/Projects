using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Core.DataTypes
{

    public class GameObjectState
    {
        private Dictionary<Component, AbstractComponentState> m_componentStates;
        private GameObject m_gameObject;

        public GameObject GameObject => m_gameObject;

        private bool m_isStateless;

        public GameObjectState(GameObject gameobject)
        {
            m_gameObject = gameobject;
            m_isStateless = m_gameObject.GetComponent<StatelessGameObject>();
        }

        public bool Snapshot()
        {
            if (!m_gameObject) { return false; }
            if (m_isStateless) { return true; }
            m_componentStates = new Dictionary<Component, AbstractComponentState>();
            foreach (var component in m_gameObject.GetComponents<Component>())
            {
                if (StateManager.IsStateful(component))
                {
                    var componentState = StateManager.GetStateContainer(component);
                    if (componentState != null)
                    {
                        componentState.SetComponent(component);
                        if (componentState.Snapshot())
                        {
                            m_componentStates[component] = componentState;
                        }
                    }
                }
            }
            return m_componentStates.Count > 0;
        }

        public bool SnapshotForced()
        {
            if (m_gameObject == null) { return false; }
            if (m_isStateless) { return true; }
            m_componentStates = new Dictionary<Component, AbstractComponentState>();
            foreach (var component in m_gameObject.GetComponents<Component>())
            {
                if (StateManager.IsStateful(component))
                {
                    var componentState = StateManager.GetStateContainer(component) ?? new ReflectionComponentState();
                    if (componentState != null)
                    {
                        componentState.SetComponent(component);
                        if (componentState.Snapshot())
                        {
                            m_componentStates[component] = componentState;
                        }
                    }
                }
            }
            return m_componentStates.Count > 0;
        }

        public bool Restore()
        {
            if(m_gameObject == null) { return false; }
            if (m_componentStates == null) { return m_isStateless; }
            bool outcome = false;
            AbstractComponentState componentState = null;
            List<Component> componentsToReAdd = new List<Component>(m_componentStates.Keys);
            foreach(var component in m_gameObject.GetComponents<Component>())
            {
                if(component && m_componentStates.TryGetValue(component, out componentState))
                {
                    // The component is still present even after the snapshot
                    outcome |= componentState.Restore();
                    //m_componentStates.Remove(component);
                    componentsToReAdd.Remove(component);
                }
                else if(StateManager.IsStateful(component))
                {
                    // The component was created after the snapshot
                    //Object.Destroy(component);
                }
            }

            // Check the components which were destroyed after snapshot
            foreach(var key in componentsToReAdd)
            {
                var component = m_gameObject.AddComponent(key.GetType());
                outcome |= m_componentStates[key].Restore(component);
            }
            return outcome;
        }
    }
}
