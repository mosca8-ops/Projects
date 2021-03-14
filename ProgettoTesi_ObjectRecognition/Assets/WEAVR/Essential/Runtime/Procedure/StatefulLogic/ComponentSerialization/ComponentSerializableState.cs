using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using TXT.WEAVR.Core;
using TXT.WEAVR.Common;
using TXT.WEAVR.Debugging;

namespace TXT.WEAVR.Procedure
{
    [Serializable]
    public class ComponentSerializableState : BaseSerializableState
    {
        private static readonly List<string> s_propertiesToIngore = new List<string>()
        {
            "useGUILayout", "runInEditMode", "tag", "hideFlags"
        };

        public ComponentSerializableState() { }

        public ComponentSerializableState(BaseSerializableState _componentState)
        {
            componentID = _componentState.componentID;
            fieldValues = _componentState.fieldValues;
            propertyValues = _componentState.propertyValues;
            m_membersData = GetComponentMembersData(componentID);
        }

        #region [ SNAPSHOT ]
        public override bool Snapshot(Component _component)
        {
            if (_component == null)
                return false;

            var componentType = _component.GetType();
            var componentAssemblyName = componentType.AssemblyQualifiedName;

            m_membersData = GetComponentMembersData(componentAssemblyName);
            if (m_membersData == null)
                SetComponentMembersData(componentMembers.Count, componentType.AssemblyQualifiedName);

            if (m_membersData.fieldGetters.Count == 0 && m_membersData.propertyGetters.Count == 0)
            {
                foreach (var fieldInfo in RetrieveFields(componentType))
                {
                    m_membersData.fieldNames.Add(fieldInfo.Name);
                    m_membersData.fieldGetters.Add(fieldInfo.FastGetter());
                }

                foreach (var propertyInfo in RetrieveProperties(componentType))
                {
                    m_membersData.propertyNames.Add(propertyInfo.Name);
                    m_membersData.propertyGetters.Add(propertyInfo.FastGetter());
                }
            }

            componentID = m_membersData.componentID;

            ISnapshotCallbackReceiver callbackReceiver = null;
            var isCallbackReceiver = IsSnapshotCallbackReceiver(_component, out callbackReceiver);

            if (isCallbackReceiver)
                callbackReceiver.OnBeforeSnapshot();

            SerializeFields(_component);
            SerializeProperties(_component);

            if (isCallbackReceiver)
                callbackReceiver.OnAfterSnapshot();

            return true;
        }

        private void SerializeFields(Component _component)
        {
            if (_component == null)
                return;

            foreach (var getter in m_membersData.fieldGetters)
                fieldValues.Add(ValueSerialization.Serialize(getter(_component)));
        }

        private void SerializeProperties(Component _component)
        {
            if (_component == null)
                return;

            foreach (var getter in m_membersData.propertyGetters)
                propertyValues.Add(ValueSerialization.Serialize(getter(_component)));
        }
        #endregion

        #region [ RESTORE ]
        public override bool Restore(Component _component)
        {
            if (_component == null)
                return false;

            if (m_membersData == null)
                m_membersData = GetComponentMembersData(componentID);
            if (m_membersData == null)
                return false;

            if (m_membersData.fieldSetters.Count == 0 && m_membersData.propertySetters.Count == 0)
            {
                var componentType = GetComponentType();

                foreach (var fieldInfo in RetrieveFields(componentType))
                    m_membersData.fieldSetters.Add(new SetterData(fieldInfo.Name, fieldInfo.FieldType, fieldInfo.FastSetter()));

                foreach (var propertyInfo in RetrieveProperties(componentType))
                    m_membersData.propertySetters.Add(new SetterData(propertyInfo.Name, propertyInfo.PropertyType, propertyInfo.FastSetter()));
            }

            IRestoreCallbackReceiver callbackReceiver = null;
            var isCallbackReceiver = IsRestoreCallbackReceiver(_component, out callbackReceiver);

            if (isCallbackReceiver)
                callbackReceiver.OnBeforeRestore();

            DeserializeFields(_component);
            DeserializeProperties(_component);

            if (isCallbackReceiver)
                callbackReceiver.OnAfterRestore();

            return true;
        }

        private void DeserializeFields(Component _component)
        {
            object deserieliedField = null;
            for (int i = 0; i < m_membersData.fieldNames.Count && i < fieldValues.Count; i++)
            {
                foreach (var fieldSetter in m_membersData.fieldSetters)
                {
                    if (fieldSetter.memberName == m_membersData.fieldNames[i])
                    {
                        deserieliedField = ValueSerialization.Deserialize(fieldValues[i], fieldSetter.memberType);
                        if (deserieliedField != null)
                            fieldSetter.setter(_component, deserieliedField);
                        break;
                    }
                }
            }
        }

        private void DeserializeProperties(Component _component)
        {
            object deserieliedProperty = null;
            for (int i = 0; i < m_membersData.propertyNames.Count && i < propertyValues.Count; i++)
            {
                foreach (var propertySetter in m_membersData.propertySetters)
                {
                    if (propertySetter.memberName == m_membersData.propertyNames[i])
                    {
                        deserieliedProperty = ValueSerialization.Deserialize(propertyValues[i], propertySetter.memberType);
                        if (deserieliedProperty != null)
                            propertySetter.setter(_component, deserieliedProperty);
                        break;
                    }
                }
            }
        }
        #endregion

        private List<FieldInfo> RetrieveFields(Type _componentType)
        {
            if (_componentType == null)
                return null;

            var fields = new List<FieldInfo>();
            Type fieldType;
            foreach (var fieldInfo in _componentType.GetFields(s_bindingFlags))
            {
                fieldType = fieldInfo.FieldType;
                if (!fieldInfo.IsInitOnly
                    && !fieldType.IsSubclassOf(typeof(UnityEngine.Object))
                    && StateManager.IsStateful(fieldInfo)
                    && fieldInfo.GetCustomAttribute<ObsoleteAttribute>() == null
                    && fieldInfo.GetCustomAttribute<IgnoreStateSerializationAttribute>() == null
                    && !fieldInfo.IsUnityEvent()
                    && !typeof(MulticastDelegate).IsAssignableFrom(fieldType)
                    && !fieldType.IsArray
                    && (fieldType == typeof(string)
                        || !typeof(IEnumerable).IsAssignableFrom(fieldType)))
                {
                    fields.Add(fieldInfo);
                }
            }
            return fields;
        }

        private List<PropertyInfo> RetrieveProperties(Type _componentType)
        {
            if (_componentType == null)
                return null;

            var properties = new List<PropertyInfo>();
            Type propertyType;
            foreach (var propertyInfo in _componentType.GetProperties(s_bindingFlags))
            {
                propertyType = propertyInfo.PropertyType;
                if (!IsPropertyToIgnore(propertyInfo)
                    && propertyInfo.CanWrite
                    && propertyInfo.CanRead
                    && !propertyType.IsSubclassOf(typeof(UnityEngine.Object))
                    && StateManager.IsStateful(propertyInfo)
                    && propertyInfo.GetCustomAttribute<ObsoleteAttribute>() == null
                    && propertyInfo.GetCustomAttribute<IgnoreStateSerializationAttribute>() == null
                    && !propertyInfo.IsUnityEvent()
                    && !typeof(MulticastDelegate).IsAssignableFrom(propertyType)
                    && !propertyType.IsArray
                    && (propertyType == typeof(string)
                        || !typeof(IEnumerable).IsAssignableFrom(propertyType)))
                {
                    properties.Add(propertyInfo);
                }
            }
            return properties;
        }

        private bool IsPropertyToIgnore(PropertyInfo _propertyInfo)
        {
            if (_propertyInfo.Name == "name" && _propertyInfo.DeclaringType == typeof(UnityEngine.Object))
                return true;

            return s_propertiesToIngore.Contains(_propertyInfo.Name);
        }
    }
}