//#define USE_OVERRIDES

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

using TypesPair = System.Collections.Generic.KeyValuePair<System.Type, string>;

namespace TXT.WEAVR.Core
{
    /// <summary>
    /// Represents a proxy to a field or a property of an object given the path to that member, separated by '.' .
    /// It is designed to be fast in getting and setting the value of the property/field.
    /// </summary>
    [Serializable]
    public class Property
    {
        public delegate void OnValueChange(Property property, object oldValue, object newValue);

        #region [  Static Part  ]

        private static readonly Dictionary<string, Property> s_cachedProperties = new Dictionary<string, Property>();
        private static readonly Dictionary<MemberInfo, AccessorsContainer> s_cachedReflectedAccessors = new Dictionary<MemberInfo, AccessorsContainer>();
        private static readonly Dictionary<TypesPair, AccessorsContainer> s_cachedTypedAccessors = new Dictionary<TypesPair, AccessorsContainer>();
        private static readonly List<IPropertyCache> s_externalCaches = new List<IPropertyCache>();
        private static readonly Dictionary<string, IPropertyCache> s_modulesCaches = new Dictionary<string, IPropertyCache>();

        private static Dictionary<Type, Func<object, object>> s_cachedConverters = new Dictionary<Type, Func<object, object>>();

        private static Func<object, object> s_unityObjectConverter = o => o;

        //{
        //    { typeof(bool), o => PropertyConvert.ToBoolean(o) },
        //    { typeof(int), o => PropertyConvert.ToInt(o) },
        //    { typeof(float), o => PropertyConvert.ToFloat(o) },
        //    { typeof(double), o => PropertyConvert.ToDouble(o) },
        //    { typeof(string), o => o != null ? o.ToString() : null },
        //    { typeof(Vector2), o => PropertyConvert.ToVector2(o) },
        //    { typeof(Vector3), o => PropertyConvert.ToVector3(o) },
        //    { typeof(Vector4), o => PropertyConvert.ToVector4(o) },
        //    { typeof(Color), o => PropertyConvert.ToColor(o) },
        //    { typeof(Enum), o => PropertyConvert.ToEnum(o) },
        //};

        private static readonly char[] _SEPARATORS = { '.' };

        /// <summary>
        /// Adds a converter to a list of cached converters
        /// </summary>
        /// <typeparam name="T">The type to convert to</typeparam>
        /// <param name="converter">The converter callback</param>
        public static void AddConverter<T>(Func<object, object> converter)
        {
            s_cachedConverters[typeof(T)] = converter;
        }

        public static void RegisterTypedAccessors<T, V>(string memberName, Func<T, V> getter, Action<T, V> setter) where T : class
        {
            s_cachedTypedAccessors[new TypesPair(typeof(T), memberName)] = new AccessorsContainer(typeof(V), o => getter(o as T), (o, v) => setter(o as T, (V)v));
        }

        public static void RegisterTypedAccessors<T, V>(string memberName, Func<T, V> getter, Func<T, V, T> setter) where T : struct
        {
            s_cachedTypedAccessors[new TypesPair(typeof(T), memberName)] = new AccessorsContainer(typeof(V), o => getter((T)o), (o, v) => setter((T)o, (V)v));
        }

        /// <summary>
        /// Set the default unity object converter
        /// </summary>
        /// <param name="converter">The converter callback</param>
        public static void SetUnityObjectConverter(Func<object, object> converter)
        {
            s_unityObjectConverter = converter;
        }

        /// <summary>
        /// Tries to get a property proxy from specified module. If thus is not possible, a newly created property will be returned.
        /// </summary>
        /// <param name="objToHandle">The object where to search for property</param>
        /// <param name="propertyPath">The property path to search for</param>
        /// <param name="moduleId">The module where to get the property from</param>
        /// <returns>The property proxy</returns>
        public static Property Get(object objToHandle, string propertyPath, string moduleId)
        {
            if (string.IsNullOrEmpty(propertyPath))
            {
                Debug.LogErrorFormat("Property: Property path for [{0}] is empty", objToHandle);
                return null;
            }
            Property property = null;
            IPropertyCache cache = null;
            if (s_modulesCaches.TryGetValue(moduleId, out cache) && cache.TryGetProperty(objToHandle, propertyPath, out property))
            {
                return property;
            }
            return Create(objToHandle, propertyPath);
        }

        /// <summary>
        /// Tries to get a property proxy. If thus is not possible, a newly created property will be returned.
        /// </summary>
        /// <param name="objToHandle">The object where to search for property</param>
        /// <param name="propertyPath">The property path to search for</param>
        /// <returns>The property proxy</returns>
        public static Property Get(object objToHandle, string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath))
            {
                Debug.LogErrorFormat("Property: Property path for [{0}] is empty", objToHandle);
                return null;
            }
            Property property = null;
            foreach (var cache in s_externalCaches)
            {
                if (cache.TryGetProperty(objToHandle, propertyPath, out property))
                {
                    return property;
                }
            }
            return Create(objToHandle, propertyPath);
        }

        /// <summary>
        /// Creates a property proxy.
        /// </summary>
        /// <param name="objToHandle">The object where to search for property</param>
        /// <param name="propertyPath">The property path to search for</param>
        /// <returns>The property proxy</returns>
        public static Property Create(object objToHandle, string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath))
            {
                Debug.LogErrorFormat("Property: Property path for [{0}] is empty", objToHandle);
                return null;
            }
            Property property = null;
            if (!s_cachedProperties.TryGetValue(propertyPath, out property))
            {
                property = BuildProperty(objToHandle, propertyPath);
                if (property == null || !property.ValidateObject(objToHandle))
                {
                    Debug.LogErrorFormat("Property: The property path '{0}' is not compatible with the object '{1}'", propertyPath, objToHandle);
                }
                else
                {
                    property.m_rootObject = objToHandle;
                    s_cachedProperties.Add(propertyPath, property);
                }
            }
            else
            {
                property = property.ShallowCopy(objToHandle);
                if (!property.ValidateObject(objToHandle))
                {
                    Debug.LogErrorFormat("Property: The property path '{0}' is not compatible with the object '{1}'", propertyPath, objToHandle);
                }
            }
            //wrapper.CacheEnabled = true;
            return property;
        }

        /// <summary>
        /// Creates a property proxy.
        /// </summary>
        /// <param name="ownerType">The object type where to search for property</param>
        /// <param name="propertyPath">The property path to search for</param>
        /// <returns>The property proxy</returns>
        public static Property Create(Type ownerType, string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath))
            {
                Debug.LogErrorFormat("Property: Property path for [TYPE: {0}] is empty", ownerType);
                return null;
            }

            if (!s_cachedProperties.TryGetValue(propertyPath, out Property property))
            {
                property = BuildProperty(ownerType, propertyPath);
                if (property == null || !property.m_finalProperty.IsValid)
                {
                    Debug.LogErrorFormat("Property: The property path '{0}' is not compatible with the type '{1}'", propertyPath, ownerType);
                }
                else
                {
                    property.m_rootObject = null;
                    s_cachedProperties.Add(propertyPath, property);
                }
            }
            else
            {
                property = property.ShallowCopy(null);
                if (!property.m_finalProperty.IsValid)
                {
                    Debug.LogErrorFormat("Property: The property path '{0}' is not compatible with the type '{1}'", propertyPath, ownerType);
                }
            }
            //wrapper.CacheEnabled = true;
            return property;
        }

        /// <summary>
        /// Registers a cache for properties proxies
        /// </summary>
        /// <param name="cache">The cache to register</param>
        public static void RegisterPropertyCache(IPropertyCache cache)
        {
            s_externalCaches.Add(cache);
            s_modulesCaches.Add(cache.ModuleId, cache);
        }

        /// <summary>
        /// Removes a cache from property proxies caches
        /// </summary>
        /// <param name="cache">The cache to remove</param>
        public static void UnregisterPropertyCache(IPropertyCache cache)
        {
            s_externalCaches.Remove(cache);
            s_modulesCaches.Remove(cache.ModuleId);
        }

        private static Property BuildProperty(object obj, string propertiesPath)
        {
            var accessorsChain = new List<AccessorsContainer>();
            string firstSplit = null;
            string propertiesSplit = null;
            bool potentialGenericObject = true;
            if (propertiesPath[0] == '[')
            {
                var typeSplits = propertiesPath.Split(']');
                firstSplit = typeSplits[0].Remove(0, 1);
                propertiesSplit = typeSplits[1];
                potentialGenericObject = false;
            }
            else
            {
                int dotIndex = propertiesPath.IndexOf('.');
                if (dotIndex < 0)
                {
                    // split not found
                    propertiesSplit = propertiesPath;
                }
                else
                {
                    firstSplit = propertiesPath.Substring(0, dotIndex);
                    propertiesSplit = propertiesPath.Substring(dotIndex, propertiesPath.Length - dotIndex);
                }
            }

            Component component = null;

            if (!string.IsNullOrEmpty(firstSplit))
            {
                component = GetComponent(obj, firstSplit);
                if (component == null)
                {
                    if (!potentialGenericObject)
                    {
                        return null;
                    }
                    propertiesSplit = firstSplit + propertiesSplit;
                }
            }
            else
            {
                firstSplit = null;
            }

            if (BuildAccessorsList(component ?? obj, propertiesSplit, accessorsChain))
            {
                return new Property(propertiesPath)
                {
                    m_componentTypename = firstSplit,
                    m_component = component,
                    m_rootObject = obj,
                    m_rootType = obj.GetType(),
                    m_accessorsChain = accessorsChain,
                    m_cacheEnabled = false,
                    m_finalProperty = accessorsChain[accessorsChain.Count - 1],
                };
            }
            return null;
        }

        private static Property BuildProperty(Type ownerType, string propertiesPath)
        {
            var accessorsChain = new List<AccessorsContainer>();
            string propertiesSplit = null;
            if (propertiesPath[0] == '[')
            {
                var typeSplits = propertiesPath.Split(']');
                ownerType = Type.GetType(typeSplits[0].Substring(1)) ?? ownerType;
                propertiesSplit = typeSplits[1];
            }
            else
            {
                int dotIndex = propertiesPath.IndexOf('.');
                if (dotIndex < 0)
                {
                    // split not found
                    propertiesSplit = propertiesPath;
                }
                else
                {
                    propertiesSplit = propertiesPath.Substring(dotIndex, propertiesPath.Length - dotIndex);
                }
            }

            if (BuildAccessorsList(ownerType, propertiesSplit, accessorsChain))
            {
                return new Property(propertiesPath)
                {
                    m_componentTypename = ownerType.IsSubclassOf(typeof(Component)) ? ownerType.FullName : null,
                    m_component = null,
                    m_rootObject = null,
                    m_rootType = ownerType,
                    m_accessorsChain = accessorsChain,
                    m_cacheEnabled = false,
                    m_finalProperty = accessorsChain[accessorsChain.Count - 1],
                };
            }
            return null;
        }

        private static Component GetComponent(object obj, string firstSplit)
        {
            if (obj == null || !(obj is GameObject || obj is Component)) { return null; }
            Component component = obj is Component ? (obj as Component).GetComponent(firstSplit)
                                                         : (obj as GameObject).GetComponent(firstSplit);
            if (component == null)
            {
                Type componentType = Type.GetType(firstSplit);
                if (componentType != null)
                {
                    component = obj is Component ? (obj as Component).GetComponent(componentType)
                                                 : (obj as GameObject).GetComponent(componentType);
                }
                if (component == null)
                {
                    var componentSplits = firstSplit.Split(_SEPARATORS, StringSplitOptions.RemoveEmptyEntries);
                    firstSplit = componentSplits[componentSplits.Length - 1];
                    component = obj is Component ? (obj as Component).GetComponent(firstSplit)
                                         : (obj as GameObject).GetComponent(firstSplit);
                }
            }
            return component;
        }

        private static bool BuildAccessorsList(object obj, string propertyPath, List<AccessorsContainer> containers)
        {
            object currentObject = obj;
            foreach (var propertyName in propertyPath.Split(_SEPARATORS, StringSplitOptions.RemoveEmptyEntries))
            {
                if (currentObject == null) { return false; }
                if (!s_cachedTypedAccessors.TryGetValue(new TypesPair(currentObject.GetType(), propertyName), out AccessorsContainer currentContainer))
                {
                    var memberInfo = DelegateFactory.GetMemberInfo(currentObject.GetType(), propertyName);
                    if (!s_cachedReflectedAccessors.TryGetValue(memberInfo, out currentContainer))
                    {
                        currentContainer = new AccessorsContainer(memberInfo);
                        if (s_cachedConverters.TryGetValue(currentContainer.Type, out Func<object, object> predefinedConverter))
                        {
                            currentContainer.Convert = predefinedConverter;
                        }
                        else if (currentContainer.Type == typeof(UnityEngine.Object)
                             || currentContainer.Type.IsSubclassOf(typeof(UnityEngine.Object)))
                        {
                            // Unity object converter is treated in a different way
                            currentContainer.Convert = s_unityObjectConverter;
                        }
                        s_cachedReflectedAccessors.Add(memberInfo, currentContainer);
                    }

#if USE_OVERRIDES
                    var overrideAttribute = memberInfo.GetCustomAttribute<OverrideAccessorsAttribute>();
                    if (overrideAttribute != null)
                    {
                        currentContainer = currentContainer.Clone();
                        currentContainer.Get = overrideAttribute.GetGetter(obj, memberInfo, currentContainer.Get);
                        currentContainer.Set = overrideAttribute.GetSetter(obj, memberInfo, currentContainer.Set);
                        currentContainer.Convert = overrideAttribute.GetConverter(obj, memberInfo, currentContainer.Convert);
                    }
#endif

                }
                containers.Add(currentContainer);
                currentObject = currentContainer.Get(currentObject);
            }
            return true;
        }

        private static bool BuildAccessorsList(Type type, string propertyPath, List<AccessorsContainer> containers)
        {
            foreach (var propertyName in propertyPath.Split(_SEPARATORS, StringSplitOptions.RemoveEmptyEntries))
            {
                if (type == null) { return false; }

                var memberInfo = DelegateFactory.GetMemberInfo(type, propertyName);
                if (!s_cachedTypedAccessors.TryGetValue(new TypesPair(type, propertyName), out AccessorsContainer currentContainer))
                {
                    if (!s_cachedReflectedAccessors.TryGetValue(memberInfo, out currentContainer))
                    {
                        currentContainer = new AccessorsContainer(memberInfo);
                        Func<object, object> predefinedConverter = null;
                        if (s_cachedConverters.TryGetValue(currentContainer.Type, out predefinedConverter))
                        {
                            currentContainer.Convert = predefinedConverter;
                        }
                        else if (currentContainer.Type == typeof(UnityEngine.Object)
                             || currentContainer.Type.IsSubclassOf(typeof(UnityEngine.Object)))
                        {
                            // Unity object converter is treated in a different way
                            currentContainer.Convert = s_unityObjectConverter;
                        }
                        s_cachedReflectedAccessors.Add(memberInfo, currentContainer);
                    }
                }

#if USE_OVERRIDES
                var overrideAttribute = memberInfo.GetCustomAttribute<OverrideAccessorsAttribute>();
                if (overrideAttribute != null)
                {
                    currentContainer = currentContainer.Clone();
                    currentContainer.Get = overrideAttribute.GetGetter(type, memberInfo, currentContainer.Get);
                    currentContainer.Set = overrideAttribute.GetSetter(type, memberInfo, currentContainer.Set);
                    currentContainer.Convert = overrideAttribute.GetConverter(type, memberInfo, currentContainer.Convert);
                }
#endif

                containers.Add(currentContainer);
                type = memberInfo is PropertyInfo ? (memberInfo as PropertyInfo).PropertyType : (memberInfo as FieldInfo).FieldType;
            }
            return true;
        }

        public static bool IsEnum(Type type)
        {
            return type.IsEnum;
        }

        #endregion

        private readonly string m_propertyPath;
        private List<AccessorsContainer> m_accessorsChain;
        private AccessorsContainer m_finalProperty;
        private object m_rootObject;
        private Type m_rootType;
        private string m_componentTypename;
        private object m_component;
        private object m_cachedLeafObject;
        private bool m_cacheEnabled;

        // Property Changed value
        public event OnValueChange ValueChanged;
        private object m_lastValue;

        /// <summary>
        /// Gets or sets the value of the property.
        /// </summary>
        /// <remarks>When cache is enabled, the entire property chain will not be evaluated except for the last property</remarks>
        public object Value {
            get {
                return m_cacheEnabled ? m_finalProperty.GetValue(LeafObject) : m_rootObject != null ? GetValueInternal(m_rootObject) : null;
            }
            set {
                // Enums are special types
                if (IsEnum(m_finalProperty.Type))
                {
                    if (value != null)
                    {
                        SetValueInternal(Enum.Parse(m_finalProperty.Type, value.ToString()));
                    }
                    else
                    {
                        SetValueInternal(Enum.GetValues(m_finalProperty.Type).GetValue(0));
                    }
                }
                else if (value == null || value.GetType() == m_finalProperty.Type)
                {
                    SetValueInternal(value);
                    if (ValueChanged != null && (m_lastValue != value))
                    {
                        ValueChanged(this, m_lastValue, value);
                        m_lastValue = value;
                    }
                }
                else
                {
                    // Convert the value first
                    object convertedValue = m_finalProperty.Convert(value);
                    SetValueInternal(convertedValue);
                    if (ValueChanged != null && m_lastValue != value)
                    {
                        ValueChanged(this, m_lastValue, convertedValue);
                        m_lastValue = convertedValue;
                    }
                }
            }
        }

        public object ParentValue => m_rootObject != null ? GetParentValueInternal(m_rootObject) : null;

        /// <summary>
        /// Gets the member info of the final property (if saved) in the chain
        /// </summary>
        public MemberInfo MemberInfo => m_finalProperty?.MemberInfo;

        /// <summary>
        /// Gets the type of the final property
        /// </summary>
        public Type Type {
            get {
                return m_finalProperty.Type;
            }
        }

        public Type OwnerType => m_rootType;

        /// <summary>
        /// Gets or sets the object from which to get/set the property
        /// </summary>
        public object Owner {
            get {
                return m_rootObject;
            }
            set {
                if (m_rootObject != value)
                {
                    m_component = null;
                    m_cachedLeafObject = null;
                    m_rootObject = value;
                    m_rootType = value?.GetType();
                }
            }
        }

        /// <summary>
        /// Gets the component of the object
        /// </summary>
        private object Component {
            get {
                if (m_component == null && m_componentTypename != null && m_rootObject != null)
                {
                    m_component = GetComponent(m_rootObject, m_componentTypename);
                }
                return m_component;
            }
        }

        /// <summary>
        /// Gets or set whether this wrapper should use caching or not
        /// </summary>
        /// <remarks>Caching will make this wrapper faster, 
        /// but will not check the full chain each time. 
        /// Each modification on the property out of this context 
        /// won't be updated if caching is enabled.</remarks>
        public bool CacheEnabled {
            get {
                return m_cacheEnabled;
            }
            set {
                m_cacheEnabled = value;
            }
        }

        /// <summary>
        /// Gets the deepest object in the property chain
        /// </summary>
        private object LeafObject {
            get {
                if (m_cachedLeafObject == null || !m_cacheEnabled)
                {
                    m_cachedLeafObject = GetLeafObject(m_rootObject);
                }
                return m_cachedLeafObject;
            }
        }

        /// <summary>
        /// Constructor. Wraps an object property by specifying its path
        /// </summary>
        /// <param name="propertyPath">The path to the property. 
        /// If the property is in a <see cref="Component"/> then 
        /// it should start with the type of the component</param>
        private Property(string propertyPath)
        {
            m_propertyPath = propertyPath;
            if (string.IsNullOrEmpty(propertyPath))
            {
                throw new ArgumentNullException("PropertyPath");
            }
        }

        public object GetValue(object target)
        {
            object currentObject = m_componentTypename != null ? GetComponent(target, m_componentTypename) : target;
            int propertyIndex = 0;
            while (currentObject != null && propertyIndex < m_accessorsChain.Count)
            {
                currentObject = m_accessorsChain[propertyIndex++].Get(currentObject);
            }
            return currentObject;
        }

        public void SetValue(object target, object value)
        {
            SetValueRecursive(m_componentTypename != null ? GetComponent(target, m_componentTypename) : target, 0, value);
        }

        private bool ValidateObject(object obj)
        {
            return GetLeafObject(obj) != null && m_finalProperty.IsValid;
        }

        private object GetLeafObject(object obj)
        {
            if (obj == null)
            {
                return null;
            }
            object currentObject = Component ?? obj;
            int propertyIndex = 0;
            int propertiesCount = m_accessorsChain.Count - 1;
            while (currentObject != null && propertyIndex < propertiesCount)
            {
                currentObject = m_accessorsChain[propertyIndex++].Get(currentObject);
            }
            return currentObject;
        }

        private object GetValueInternal(object obj)
        {
            object currentObject = Component ?? obj;
            int propertyIndex = 0;
            while (currentObject != null && propertyIndex < m_accessorsChain.Count)
            {
                currentObject = m_accessorsChain[propertyIndex++].Get(currentObject);
            }
            return currentObject;
        }

        private object GetParentValueInternal(object obj)
        {
            object currentObject = Component ?? obj;
            int propertyIndex = 0;
            while (currentObject != null && propertyIndex < m_accessorsChain.Count - 1)
            {
                currentObject = m_accessorsChain[propertyIndex++].Get(currentObject);
            }
            return currentObject;
        }

        private void SetValueInternal(object value)
        {
            SetValueRecursive(Component ?? m_rootObject, 0, value);
        }

        private object SetValueRecursive(object parent, int propertyIndex, object value)
        {
            if (parent == null)
            {
                return null;
            }
            object objectValue = null;
            if (propertyIndex < m_accessorsChain.Count - 1)
            {
                objectValue = SetValueRecursive(m_accessorsChain[propertyIndex].GetValue(parent), propertyIndex + 1, value);
            }
            else
            {
                objectValue = value;
            }

            return m_accessorsChain[propertyIndex].SetAndGet(parent, objectValue);
        }

        /// <summary>
        /// Copies these property values to another property
        /// </summary>
        /// <remarks>Property chain is copied by reference, so consider the side effects</remarks>
        /// <param name="obj">The new owner of the copy property</param>
        /// <returns>The copy of the property</returns>
        public Property ShallowCopy(object obj)
        {
            Property wrapper = new Property(m_propertyPath);
            wrapper.m_accessorsChain = m_accessorsChain;
            wrapper.m_finalProperty = m_finalProperty;
            wrapper.m_componentTypename = m_componentTypename;
            wrapper.m_rootObject = obj;
            wrapper.m_rootType = m_rootType;
            return wrapper;
        }

        /// <summary>
        /// Class which holds the accessors
        /// </summary>
        private class AccessorsContainer
        {
            public Func<object, object> Convert;
            public Func<object, object> Get;
            public Action<object, object> Set;
            public Func<object, object, object> SetAndGet;
            public MemberInfo MemberInfo { get; private set; }

            public bool IsValid { get; private set; }

            public Type Type { get; private set; }

            public AccessorsContainer(Type type)
            {
                Type = type;
            }

            public AccessorsContainer(Type type, Func<object, object> getter, Action<object, object> setter)
            {
                Type = type;
                Get = getter;
                Set = setter;

                SetAndGet = SetValue;

                IsValid = true;
                Convert = Converter;
            }

            public AccessorsContainer(Type type, Func<object, object> getter, Func<object, object, object> setter)
            {
                Type = type;
                Get = getter;
                SetAndGet = setter;

                IsValid = true;
                Convert = Converter;
            }

            public AccessorsContainer(MemberInfo memberInfo)
            {
                MemberInfo = memberInfo;
                if (memberInfo is FieldInfo fieldInfo)
                {
                    Type = fieldInfo.FieldType;
                    Get = DelegateFactory.FastGetter(fieldInfo);
                    Set = DelegateFactory.FastSetter(fieldInfo);
                    IsValid = true;
                }
                else if (memberInfo is PropertyInfo propertyInfo)
                {
                    Type = propertyInfo.PropertyType;
                    Get = DelegateFactory.FastGetter(propertyInfo);
                    Set = DelegateFactory.FastSetter(propertyInfo);
                    IsValid = true;
                }

                SetAndGet = SetValue;
                Convert = Converter;
            }

            public object Converter(object obj)
            {
                // Default converter
                return obj;
            }

            public object GetValue(object owner)
            {
                return Get(owner);
            }

            public object SetValue(object owner, object value)
            {
                Set(owner, value);
                return owner;
            }

            public AccessorsContainer Clone()
            {
                return new AccessorsContainer(Type)
                {
                    Get = Get,
                    Set = Set,
                    SetAndGet = SetAndGet,
                    Convert = Convert,
                    IsValid = IsValid
                };
            }
        }
    }
}