namespace TXT.WEAVR.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;

    [Serializable]
    [Obsolete("This class is obsolete, use Property instead, it is faster (>20x) and safer")]
    public class PropertyWrapper
    {
        private static readonly Dictionary<string, PropertyWrapper> _cachedProperties = new Dictionary<string, PropertyWrapper>();

        private static readonly char[] _SEPARATORS = { '.' };

        /// <summary>
        /// Creates a property wrapper.
        /// </summary>
        /// <param name="objToHandle">The object where to search for property</param>
        /// <param name="propertyPath">The property path search for</param>
        /// <returns>The property wrapper</returns>
        public static PropertyWrapper Create(UnityEngine.Object objToHandle, string propertyPath) {
            if (string.IsNullOrEmpty(propertyPath)) {
                Debug.LogErrorFormat("PropertyWrapper: Property path for [{0}] is empty", objToHandle.name);
                return null;
            }
            PropertyWrapper wrapper = null;
            if (!_cachedProperties.TryGetValue(propertyPath, out wrapper)) {
                wrapper = new PropertyWrapper(propertyPath);
                if (!wrapper.ValidateObject(objToHandle)) {
                    Debug.LogError("The property path is not compatible with the specified object");
                }
                else {
                    wrapper._rootObject = objToHandle;
                    _cachedProperties.Add(propertyPath, wrapper);
                }
            }
            else {
                wrapper = wrapper.ShallowCopy(objToHandle);
                if (!wrapper.ValidateObject(objToHandle)) {
                    Debug.LogError("The property path is not compatible with the specified object");
                }
            }
            //wrapper.CacheEnabled = true;
            return wrapper;
        }


        [SerializeField]
        private readonly string _propertyPath;
        private List<Property> _propertiesChain;
        private Property _finalProperty;
        private UnityEngine.Object _rootObject;
        private string _componentType;
        private object _component;
        private object _cachedValue;
        private object _cachedLeafObject;
        private bool _cacheEnabled;

        /// <summary>
        /// Gets or sets the value of the wrapped property.
        /// </summary>
        /// <remarks>This Property is quite heavy so consider caching it whenever possible</remarks>
        public object Value {
            get {
                return LeafObject != null ? _finalProperty.GetValue(_cachedLeafObject) : null;
            }
            set {
                //if (_cachedValue != value && LeafObject != null) {
                //    _finalProperty.SetValue(_cachedLeafObject, value);
                //}
                SetValueBottomUp(value);
                _cachedValue = value;
            }
        }

        /// <summary>
        /// Gets the type of the final property
        /// </summary>
        public Type Type {
            get {
                return LeafObject != null ? _finalProperty.Type : null;
            }
        }

        /// <summary>
        /// Gets or sets the object from which to get/set the property
        /// </summary>
        public UnityEngine.Object Owner {
            get {
                return _rootObject;
            }
            set {
                if (_rootObject != value) {
                    _component = null;
                    _cachedLeafObject = null;
                    _rootObject = value;
                }
            }
        }

        /// <summary>
        /// Gets the component of the object
        /// </summary>
        private object Component {
            get {
                if (_component == null && _componentType != null && _rootObject != null) {
                    _component = _rootObject is Component ? (_rootObject as Component).gameObject.GetComponent(_componentType) :
                                 _rootObject is GameObject ? (_rootObject as GameObject).GetComponent(_componentType) : null;
                }
                return _component;
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
                return _cacheEnabled;
            }
            set {
                _cacheEnabled = value;
            }
        }

        /// <summary>
        /// Gets the deepest object in the property chain
        /// </summary>
        private object LeafObject {
            get {
                if (_cachedLeafObject == null || !_cacheEnabled) {
                    _cachedLeafObject = GetLeafObject(_rootObject);
                }
                return _cachedLeafObject;
            }
        }

        /// <summary>
        /// Constructor. Wraps an object property by specifying its path
        /// </summary>
        /// <param name="propertyPath">The path to the property. 
        /// If the property is in a <see cref="Component"/> then 
        /// it should start with the type of the component</param>
        private PropertyWrapper(string propertyPath) {
            _propertyPath = propertyPath;
            if (string.IsNullOrEmpty(propertyPath)) {
                throw new ArgumentNullException("PropertyPath");
            }
        }

        private bool BuildPropertiesChain(UnityEngine.Object obj) {
            _propertiesChain = new List<Property>();
            string firstSplit = null;
            string propertiesSplit = null;
            if (_propertyPath[0] == '[') {
                var typeSplits = _propertyPath.Split(']');
                firstSplit = typeSplits[0].Remove(0, 1);
                propertiesSplit = typeSplits[1];
            }
            else {
                int dotIndex = _propertyPath.IndexOf('.');
                firstSplit = _propertyPath.Substring(0, dotIndex);
                propertiesSplit = _propertyPath.Substring(dotIndex, _propertyPath.Length - dotIndex);
            }
            // Split the
            // If the path is only one component, then get the immediate property
            if (string.IsNullOrEmpty(propertiesSplit)) {
                var property = new Property(obj.GetType(), _propertyPath);
                if (!property.IsValid) {
                    return false;
                }
                _propertiesChain.Add(property);
                _finalProperty = _propertiesChain[_propertiesChain.Count - 1];
                return true;
            }

            var splits = propertiesSplit.Split(_SEPARATORS, StringSplitOptions.RemoveEmptyEntries);

            // Start searching from gameobject type
            GameObject gameObject = obj is Component ? (obj as Component).gameObject : obj as GameObject;
            if (gameObject == null) {
                _propertiesChain = null;
                return false;
            }

            // Check if first property is component or not
            _component = gameObject.GetComponent(firstSplit);
            _componentType = _component != null ? firstSplit : null;

            // Search for properties
            Type type = (_component ?? gameObject).GetType();
            for (int i = 0; i < splits.Length; i++) {
                var property = new Property(type, splits[i]);
                if (!property.IsValid) {
                    return false;
                }
                _propertiesChain.Add(property);
                type = property.Type;
            }

            _finalProperty = _propertiesChain[_propertiesChain.Count - 1];
            return true;
        }

        private bool ValidateObject(UnityEngine.Object obj) {
            return GetLeafObject(obj) != null && _finalProperty.IsValid;
        }

        private object GetLeafObject(UnityEngine.Object obj) {
            if (obj == null) {
                return null;
            }
            else if (_propertiesChain == null && !BuildPropertiesChain(obj)) {
                return null;
            }
            object currentObject = Component ?? obj;
            int propertyIndex = 0;
            int propertiesCount = _propertiesChain.Count - 1;
            while (currentObject != null && propertyIndex < _propertiesChain.Count - 1) {
                currentObject = _propertiesChain[propertyIndex++].GetValue(currentObject);
            }
            return currentObject;
        }

        private void SetValueBottomUp(object value) {
            SetValueBottomUpRecursive(Component ?? _rootObject, 0, value);
        }

        private object SetValueBottomUpRecursive(object parent, int propertyIndex, object value) {
            if (parent == null) {
                return null;
            }
            object objectValue = null;
            if (propertyIndex < _propertiesChain.Count - 1) {
                objectValue = SetValueBottomUpRecursive(_propertiesChain[propertyIndex].GetValue(parent), propertyIndex + 1, value);
            }
            else {
                objectValue = value;
            }
            _propertiesChain[propertyIndex].SetValue(parent, objectValue);
            return parent;
            //_finalProperty.SetValue(_cachedLeafObject, value);
            //object currentValue = value;
            //while (currentValue != null && propertyIndex > 0) {
            //    currentValue = _propertiesChain[propertyIndex--].GetValue(currentValue);
            //}
        }

        public PropertyWrapper ShallowCopy(UnityEngine.Object obj) {
            PropertyWrapper wrapper = new PropertyWrapper(_propertyPath);
            wrapper._propertiesChain = _propertiesChain;
            wrapper._finalProperty = _finalProperty;
            wrapper._componentType = _componentType;
            wrapper._rootObject = obj;
            return wrapper;
        }

        /// <summary>
        /// Class which combines <see cref="FieldInfo"/> and <see cref="PropertyInfo"/> to provide the same functionality.
        /// </summary>
        private class Property
        {
            private bool _isProperty;
            private FieldInfo _field;
            private PropertyInfo _property;

            public bool IsValid { get; private set; }

            public Type Type {
                get {
                    return _isProperty ? _property.PropertyType : _field.FieldType;
                }
            }

            public Property(Type type, string name) {
                _property = type.GetProperty(name);
                _isProperty = _property != null;
                _field = _isProperty ? null : type.GetField(name);
                IsValid = _isProperty || _field != null;
            }

            public object GetValue(object owner) {
                return _isProperty ? _property.GetValue(owner, null) : _field.GetValue(owner);
            }

            public void SetValue(object owner, object value) {
                if (_isProperty) { _property.SetValue(owner, value, null); }
                else { _field.SetValue(owner, value); }
            }
        }
    }
}