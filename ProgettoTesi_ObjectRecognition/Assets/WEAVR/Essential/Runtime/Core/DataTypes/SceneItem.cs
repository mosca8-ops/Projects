using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;

using Object = UnityEngine.Object;

namespace TXT.WEAVR
{
    [Serializable]
    public struct SceneItem
    {
        public string uniqueId;
        public string componentType;

        [NonSerialized]
        private bool m_initialized;
        [NonSerialized]
        private Object m_item;
        [NonSerialized]
        private GameObject m_go;
        [NonSerialized]
        private Type m_type;

        private Type ComponentType
        {
            get
            {
                if(m_type == null && !string.IsNullOrEmpty(componentType))
                {
                    m_type = Type.GetType(componentType);
                }
                return m_type;
            }
        }

        public bool IsComponent => !string.IsNullOrEmpty(componentType);

        public bool IsValid => !string.IsNullOrEmpty(uniqueId);

        public Object Item
        {
            get
            {
                if (!m_initialized)
                {
                    m_initialized = true;
                    if (!string.IsNullOrEmpty(uniqueId))
                    {
                        m_item = GetObject(IDBookkeeper.Get(uniqueId));
                    }
                }
                return m_item;
            }
        }

        public void Update(Object value)
        {
            if(value is Component c)
            {
                componentType = c.GetType().AssemblyQualifiedName;
                m_type = c.GetType();
                m_go = c.gameObject;
            }
            else if(value is GameObject)
            {
                componentType = string.Empty;
                m_type = typeof(GameObject);
                m_go = value as GameObject;
            }
            m_item = value;
            m_initialized = true;
        }

        public Object GetObject(GameObject go)
        {
            if(m_go == go)
            {
                return m_item;
            }
            if (go && !string.IsNullOrEmpty(componentType))
            {
                var component = go.GetComponent(ComponentType);
                m_go = component.gameObject;
                return component;
            }
            m_go = go;
            return go;
        }
    }
}