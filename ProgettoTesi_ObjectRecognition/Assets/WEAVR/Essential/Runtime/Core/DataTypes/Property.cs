using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR
{
    [Serializable]
    public class Property
    {
        [SerializeField]
        private string m_path;

        [SerializeField]
        private string m_propertyName;

        [SerializeField]
        private string m_targetTypename;
        private Type m_targetType;

        [SerializeField]
        private string m_propertyTypename;
        private Type m_propertyType;

        [NonSerialized]
        private UnityEngine.Object m_target;

        [NonSerialized]
        private Core.Property m_property;
        
        public string Path
        {
            get => m_path;
            set
            {
                if(m_path != value)
                {
                    m_path = value;
                    MakeDirty();
                }
            }
        }

        public string PropertyName
        {
            get => m_propertyName;
        }

        public UnityEngine.Object Target
        {
            get => m_target;
            set
            {
                if(m_target != value)
                {
                    m_target = value;
                    if (m_target != null && m_property != null && m_property.OwnerType == m_target.GetType())
                    {
                        m_property.Owner = m_target;
                    }
                    else
                    {
                        MakeDirty();
                    }
                }
            }
        }

        public string TargetTypename
        {
            get => m_targetTypename;
            set
            {
                if(m_targetTypename != value)
                {
                    m_targetTypename = value;
                    if (!string.IsNullOrEmpty(m_targetTypename))
                    {
                        m_targetType = Type.GetType(m_targetTypename);
                    }
                }
            }
        }

        public Type TargetType
        {
            get
            {
                if(m_targetType == null && !string.IsNullOrEmpty(m_targetTypename))
                {
                    m_targetType = Type.GetType(m_targetTypename);
                }
                return m_targetType ?? Target?.GetType();
            }
        }

        public string PropertyTypename
        {
            get => m_propertyTypename;
            set
            {
                if (m_propertyTypename != value)
                {
                    m_propertyTypename = value;
                    if (!string.IsNullOrEmpty(m_propertyTypename))
                    {
                        m_propertyType = Type.GetType(m_propertyTypename);
                    }
                }
            }
        }

        public Type PropertyType
        {
            get
            {
                if (m_propertyType == null && !string.IsNullOrEmpty(m_propertyTypename))
                {
                    m_propertyType = Type.GetType(m_propertyTypename);
                }
                return m_propertyType;
            }
        }

        public MemberInfo MemberInfo
        {
            get; set;
        }

        protected Core.Property ReflectedProperty
        {
            get
            {
                if(m_property == null && !string.IsNullOrEmpty(m_path))
                {
                    if (m_target != null)
                    {
                        m_property = Core.Property.Create(m_target, m_path);
                    }
                    else if(TargetType != null)
                    {
                        m_property = Core.Property.Create(m_targetType, m_path);
                    }
                }
                return m_property;
            }
        }

        public object Value
        {
            get => ReflectedProperty?.Value;
            set
            {
                if(ReflectedProperty != null)
                {
                    ReflectedProperty.Value = value;
                }
            }
        }

        public void TrySetValue(object value)
        {
            if (ReflectedProperty?.MemberInfo?.GetCustomAttributes(typeof(ForcedSetterAttribute), false).FirstOrDefault() is ForcedSetterAttribute setterAttribute)
            {
                var parentValue = ReflectedProperty.ParentValue;
                if(parentValue != default)
                {
                    var ownerType = parentValue.GetType();
                    while (ownerType != null)
                    {
                        var methodInfo = ownerType.GetMethod(setterAttribute.SetterMember, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                        if (methodInfo != null)
                        {
                            methodInfo.Invoke(parentValue, new object[] { value });
                            return;
                        }
                        else
                        {
                            var setter = ownerType.PropertySetNoThrow(setterAttribute.SetterMember) ?? ownerType.FieldSetNoThrow(setterAttribute.SetterMember);
                            if(setter != null)
                            {
                                setter(parentValue, value);
                                return;
                            }
                        }
                        ownerType = ownerType.BaseType;
                    }
                }
            }

            Value = value;
        }

        public object Get(object target)
        {
            return ReflectedProperty?.GetValue(target);
        }

        public void Set(object target, object value)
        {
            ReflectedProperty?.SetValue(target, value);
        }

        public void MakeDirty()
        {
            m_propertyType = null;
            m_targetType = null;
            m_property = null;
        }
    }
}