using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [Serializable]
    public class BaseSerializableState
    {
        #region [ STATIC FIELDS ]
        public static List<ComponentMembersData> componentMembers = new List<ComponentMembersData>();

        protected static BindingFlags s_bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        #endregion

        #region [ SERIALIZED FIELDS ]
        public int componentID;
        public List<string> fieldValues = new List<string>();
        public List<string> propertyValues = new List<string>();
        #endregion

        #region [ RUNTIME ]
        protected ComponentMembersData m_membersData;
        #endregion

        public virtual bool Snapshot(Component _component) => false;
        public virtual bool Restore(Component _component) => false;

        public Type GetComponentType()
        {
            if (m_membersData == null)
                m_membersData = GetComponentMembersData(componentID);

            if (m_membersData != null)
                return Type.GetType(m_membersData.componentAssemblyName);

            return null;
        }

        protected bool IsSnapshotCallbackReceiver(Component _component, out ISnapshotCallbackReceiver _receiver)
        {
            if (_component is ISnapshotCallbackReceiver receiver)
            {
                _receiver = receiver;
                return true;
            }

            _receiver = null;
            return false;
        }
        protected bool IsRestoreCallbackReceiver(Component _component, out IRestoreCallbackReceiver _receiver)
        {
            if (_component is IRestoreCallbackReceiver receiver)
            {
                _receiver = receiver;
                return true;
            }
            _receiver = null;
            return false;
        }

        protected ComponentMembersData GetComponentMembersData(int _componentID)
        {
            return componentMembers.FirstOrDefault(c => c.componentID == _componentID);
        }
        protected ComponentMembersData GetComponentMembersData(string _componentAssemblyName)
        {
            return componentMembers.FirstOrDefault(c => c.componentAssemblyName == _componentAssemblyName);
        }
        protected void SetComponentMembersData(int _componentID, string _componentAssemblyName)
        {
            m_membersData = new ComponentMembersData(_componentID, _componentAssemblyName);
            componentMembers.Add(m_membersData);
        }
    }

    [Serializable]
    public class ComponentMembersData
    {
        public int componentID;
        public string componentAssemblyName;
        public List<string> fieldNames = new List<string>();
        public List<string> propertyNames = new List<string>();

        [NonSerialized]
        public List<Func<object, object>> fieldGetters = new List<Func<object, object>>();
        [NonSerialized]
        public List<SetterData> fieldSetters = new List<SetterData>();

        [NonSerialized]
        public List<Func<object, object>> propertyGetters = new List<Func<object, object>>();
        [NonSerialized]
        public List<SetterData> propertySetters = new List<SetterData>();

        public ComponentMembersData(int _componentID, string _componentAssemblyName)
        {
            componentID = _componentID;
            componentAssemblyName = _componentAssemblyName;
        }
    }

    public struct SetterData
    {
        public string memberName;
        public Type memberType;
        public Action<object, object> setter;

        public SetterData(string _name, Type _memberType, Action<object, object> _setter)
        {
            memberName = _name;
            memberType = _memberType;
            setter = _setter;
        }
    }
}
