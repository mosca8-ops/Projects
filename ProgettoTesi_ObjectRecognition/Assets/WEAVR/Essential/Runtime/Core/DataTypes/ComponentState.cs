using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TXT.WEAVR.Core.DataTypes
{

    public abstract class AbstractComponentState
    {
        public abstract void SetComponent(Component component);
        public abstract bool Snapshot();
        public abstract bool Restore();
        public abstract bool Restore(Component other);
    }

    public class EmptyComponentState : AbstractComponentState
    {
        public override bool Restore()
        {
            return true;
        }

        public override bool Restore(Component other)
        {
            return true;
        }

        public override void SetComponent(Component component)
        {
            
        }

        public override bool Snapshot()
        {
            return true;
        }
    }

    public class ReflectionComponentState : AbstractComponentState
    {
        private static Dictionary<Type, List<Func<object, object>>> s_getters = new Dictionary<Type, List<Func<object, object>>>();
        private static Dictionary<Type, List<Action<object, object>>> s_setters = new Dictionary<Type, List<Action<object, object>>>();

        private static BindingFlags s_bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private List<Func<object, object>> m_getters;
        private List<Action<object, object>> m_setters;

        private List<object> m_values;
        private Component m_component;

        private void RetrieveFields(Component component)
        {
            if (!s_getters.TryGetValue(component.GetType(), out List<Func<object, object>> getters))
            {
                List<Action<object, object>> setters = new List<Action<object, object>>();
                getters = new List<Func<object, object>>();
                foreach (var fieldInfo in component.GetType().GetFields(s_bindingFlags))
                {
                    if (!fieldInfo.IsInitOnly 
                        && StateManager.IsStateful(fieldInfo) 
                        && fieldInfo.GetCustomAttribute<ObsoleteAttribute>() == null
                        && !fieldInfo.FieldType.IsArray
                        && (fieldInfo.FieldType.Namespace == null || !fieldInfo.FieldType.Namespace.StartsWith("Unity"))
                        && !typeof(IEnumerable).IsAssignableFrom(fieldInfo.FieldType))
                    {
                        getters.Add(fieldInfo.FastGetter());
                        setters.Add(fieldInfo.FastSetter());
                    }
                }
                s_getters[component.GetType()] = getters;
                s_setters[component.GetType()] = setters;
            }
        }

        private void RetrieveProperties(Component component)
        {
            Type componentType = component.GetType();
            if (!s_getters.TryGetValue(componentType, out List<Func<object, object>> getters))
            {
                List<Action<object, object>> setters = new List<Action<object, object>>();
                getters = new List<Func<object, object>>();
                foreach (var propertyInfo in componentType.GetProperties(s_bindingFlags))
                {
                    if (propertyInfo.CanWrite
                        && propertyInfo.CanRead
                        && StateManager.IsStateful(propertyInfo)
                        && propertyInfo.GetCustomAttribute<ObsoleteAttribute>() == null
                        && !propertyInfo.PropertyType.IsArray
                        && (propertyInfo.PropertyType.Namespace == null || !propertyInfo.PropertyType.Namespace.StartsWith("Unity"))
                        && !typeof(IEnumerable).IsAssignableFrom(propertyInfo.PropertyType))
                    {
                        getters.Add(propertyInfo.FastGetter());
                        setters.Add(propertyInfo.FastSetter());
                    }
                }
                s_getters[componentType] = getters;
                s_setters[componentType] = setters;
            }
        }

        public override bool Restore()
        {
            return Restore(m_component);
        }

        public override bool Restore(Component other)
        {
            if (m_values == null || m_values.Count == 0 || !other || other.GetType() != m_component.GetType()) { return false; }

            for (int i = 0; i < m_setters.Count; i++)
            {
                m_setters[i](other, m_values[i]);
            }
            return true;
        }

        public override bool Snapshot()
        {
            if (m_values == null)
            {
                m_values = new List<object>();
            }
            else
            {
                m_values.Clear();
            }
            for (int i = 0; i < m_getters.Count; i++)
            {
                m_values.Add(m_getters[i](m_component));
            }
            return true;
        }

        public override void SetComponent(Component component)
        {
            if (m_component != null) { return; }

            m_component = component;

            var componentType = component.GetType();

            if (!s_setters.TryGetValue(componentType, out m_setters) || !s_getters.TryGetValue(componentType, out m_getters))
            {
                RetrieveProperties(component);
                RetrieveFields(component);

                m_setters = s_setters[componentType];
                m_getters = s_getters[componentType];
            }

        }
    }


    public abstract class SpecialComponentState<T> : AbstractComponentState where T : Component
    {
        protected T m_component;

        public override sealed void SetComponent(Component component)
        {
            m_component = component as T;
        }

        public override bool Restore()
        {
            return Restore(m_component);
        }

        public override sealed bool Restore(Component other)
        {
            return other is T ? Restore(m_component) : false;
        }

        protected abstract bool Restore(T component);
    }

    public abstract class GenericComponentState<T> : AbstractComponentState where T : Component
    {
        private static List<AbstractComponentDelegate<T>> s_delegates;

        private List<object> m_values;
        protected T m_component;

        public GenericComponentState(T component)
        {
            m_component = component;
            if(s_delegates == null)
            {
                s_delegates = GetDelegates();
            }
        }

        public override bool Restore()
        {
            return Restore(m_component);
        }

        public override bool Snapshot()
        {
            if(m_values == null)
            {
                m_values = new List<object>();
            }
            else
            {
                m_values.Clear();
            }
            for (int i = 0; i < s_delegates.Count; i++)
            {
                m_values[i] = s_delegates[i].GetValue(m_component);
            }
            return true;
        }

        public abstract List<AbstractComponentDelegate<T>> GetDelegates();

        public override bool Restore(Component other)
        {
            if(other is T)
            {
                return Restore((T)other);
            }
            return false;
        }

        protected bool Restore(T component)
        {
            if (m_values == null || m_values.Count == 0) { return false; }

            for (int i = 0; i < s_delegates.Count; i++)
            {
                s_delegates[i].SetValue(component, m_values[i]);
            }
            return true;
        }
    }

    public abstract class AbstractComponentDelegate<T> where T : Component
    {
        public abstract void SetValue(T component, object value);
        public abstract object GetValue(T component);
    }

    public class ComponentDelegate<T, V> : AbstractComponentDelegate<T> where T : Component
    {
        private readonly Action<T, V> m_setter;
        private readonly Func<T, V> m_getter;

        public ComponentDelegate(Action<T, V> setter, Func<T, V> getter)
        {
            m_getter = getter;
            m_setter = setter;
        }

        public override object GetValue(T component)
        {
            return m_getter(component);
        }

        public override void SetValue(T component, object value)
        {
            if (value is V v)
            {
                m_setter(component, v);
            }
        }
    }
}
