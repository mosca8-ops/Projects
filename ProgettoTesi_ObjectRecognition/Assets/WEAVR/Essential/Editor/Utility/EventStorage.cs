using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TXT.WEAVR.Editor
{

    public class EventStorage
    {
        #region [  STATIC PART  ]
        private static EventStorage s_global = new EventStorage();

        public static EventStorage Global => s_global;

        #endregion

        private Dictionary<object, GenericEventContainer> m_containers;
        private Dictionary<object, SpecialEventContainer> m_specialContainers;

        public EventStorage()
        {
            m_containers = new Dictionary<object, GenericEventContainer>();
            m_specialContainers = new Dictionary<object, SpecialEventContainer>();
        }

        public bool Contains(object obj)
        {
            return m_specialContainers.ContainsKey(obj) || m_containers.ContainsKey(obj);
        }

        public void StoreEvents(object obj)
        {
            if(obj == null) { return; }
            GenericEventContainer container;
            if (m_containers.TryGetValue(obj, out container) && !container.Store(obj))
            {
                m_containers.Remove(obj);
            }
            else
            {
                container = new GenericEventContainer();
                if (container.Store(obj))
                {
                    m_containers[obj] = container;
                }
            }
        }

        public bool RestoreEvents(object obj)
        {
            if(obj == null) { return false; }
            GenericEventContainer container;
            if(m_containers.TryGetValue(obj, out container))
            {
                return container.Restore(obj);
            }
            return false;
        }

        public void StoreEvents(object obj, params EventCell[] events)
        {
            if (obj == null) { return; }
            SpecialEventContainer container;
            if (m_specialContainers.TryGetValue(obj, out container) && !container.Store(obj, events))
            {
                m_containers.Remove(obj);
            }
            else
            {
                container = new SpecialEventContainer();
                if (container.Store(obj, events))
                {
                    m_specialContainers[obj] = container;
                }
            }
        }

        public bool RestoreEvents(object obj, params string[] events)
        {
            if (obj == null) { return false; }
            SpecialEventContainer container;
            if (m_specialContainers.TryGetValue(obj, out container))
            {
                return container.Restore(obj, events);
            }
            return false;
        }

        #region [  EVENT CONTAINER  ]

        private class GenericEventContainer
        {
            private Type m_type;
            private Dictionary<EventInfo, List<Delegate>> m_events;

            public GenericEventContainer()
            {
                m_events = new Dictionary<EventInfo, List<Delegate>>();
            }

            public bool Store(object owner)
            {
                if(owner == null) { return false; }
                m_type = owner.GetType();
                m_events.Clear();
                bool outcome = false;
                var fields = m_type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                foreach(var e in m_type.GetEvents(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    FieldInfo eventField = fields.FirstOrDefault(f => f.Name == e.Name);
                    if (eventField != null)
                    {
                        List<Delegate> delegates = new List<Delegate>(((Delegate)eventField.GetValue(owner)).GetInvocationList());
                        outcome |= delegates.Count > 0;
                        m_events[e] = delegates;
                    }
                }

                return outcome;
            }

            public bool Restore(object owner)
            {
                if(m_type == null || owner == null || !owner.GetType().IsSubclassOf(m_type) || owner.GetType() != m_type)
                {
                    return false;
                }

                List<Delegate> delegates = null;
                foreach (var e in owner.GetType().GetEvents(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if(m_events.TryGetValue(e, out delegates))
                    {
                        foreach(var del in delegates)
                        {
                            e.RemoveEventHandler(owner, del);
                            e.AddEventHandler(owner, del);
                        }
                    }
                }
                return true;
            }
        }

        public struct EventCell
        {
            public readonly string eventName;
            public readonly Delegate eventDelegate;

            public EventCell(string name, Delegate theEvent)
            {
                eventName = name;
                eventDelegate = theEvent;
            }
        }

        private class SpecialEventContainer
        {
            private Type m_type;
            private Dictionary<string, List<Delegate>> m_events;

            public SpecialEventContainer()
            {
                m_events = new Dictionary<string, List<Delegate>>();
            }

            public bool Store(object owner, params EventCell[] eventsToStore)
            {
                if (owner == null || eventsToStore == null || eventsToStore.Length == 0) { return false; }
                m_type = owner.GetType();
                m_events.Clear();
                bool outcome = false;
                foreach(var cell in eventsToStore)
                {
                    if(cell.eventDelegate == null) { continue; }
                    var invList = cell.eventDelegate.GetInvocationList();
                    if(invList.Length > 0)
                    {
                        outcome = true;
                        m_events[cell.eventName] = new List<Delegate>(invList);
                    }
                }

                return outcome;
            }

            public bool Restore(object owner, params string[] eventsToRestore)
            {
                if (m_type == null || owner == null || m_events.Count == 0 || !(owner.GetType().IsSubclassOf(m_type) || owner.GetType() == m_type))
                {
                    return false;
                }

                List<Delegate> delegates = null;
                foreach(var e in eventsToRestore) {
                    var eventInfo = m_type.GetEvent(e, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (m_events.TryGetValue(e, out delegates))
                    {
                        foreach (var del in delegates)
                        {
                            if (del.Target != null)
                            {
                                eventInfo.RemoveEventHandler(owner, del);
                                eventInfo.AddEventHandler(owner, del);
                            }
                        }
                    }
                }
                return true;
            }
        }

        #endregion
    }
}