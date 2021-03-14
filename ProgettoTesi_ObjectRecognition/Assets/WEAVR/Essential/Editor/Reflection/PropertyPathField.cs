namespace TXT.WEAVR.Editor
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using TXT.WEAVR.Core;
    using TXT.WEAVR.Core.DataTypes;
    using UnityEditor;
    using UnityEngine;

    public class PropertyPathField
    {
        #region Static Part
        /// <summary>
        /// Static cache to fasten the properties retrieval
        /// </summary>
        private static readonly Dictionary<Type, List<PropertyIdentifier>> _cachedComponents = new Dictionary<Type, List<PropertyIdentifier>>();

        private static readonly BindingFlags _bindingFlags = BindingFlags.Public | BindingFlags.Instance;

        private static readonly Dictionary<Type, IEnumerable<PropertyInfo>> s_cachedProperties = new Dictionary<Type, IEnumerable<PropertyInfo>>();

        private static readonly Dictionary<Type, IEnumerable<FieldInfo>> s_cachedFields = new Dictionary<Type, IEnumerable<FieldInfo>>();

        #endregion

        #region Property Related

        #region Properties
        private UnityEngine.Object _object;
        private Rect _lastRect;

        private List<PropertyIdentifier> _properties;
        [SerializeField]
        private int _propertyIndex;

        [SerializeField]
        private PropertyIdentifier _selectedProperty;
        public PropertyIdentifier SelectedProperty {
            get {
                if (_selectedProperty == null && 0 <= _propertyIndex && _propertyIndex < _properties.Count)
                {
                    _selectedProperty = _properties[_propertyIndex];
                }
                return _selectedProperty;
            }
        }

        [SerializeField]
        private string _selectedPropertyPath;
        public string SelectedPropertyPath {
            get {
                if (_selectedPropertyPath == null && _selectedProperty != null)
                {
                    _selectedPropertyPath = _selectedProperty.propertyPath;
                }
                return _selectedPropertyPath;
            }
        }

        public PropertyPathField()
        {
            _properties = new List<PropertyIdentifier>();
        }

        #endregion // Properties

        #region Methods

        public void SetPropertyPathByName(UnityEngine.Object newObject, string propertyName)
        {
            if(newObject == null) { return; }

            UpdateProperties(newObject, true);
            string key = $"[{newObject.GetType().AssemblyQualifiedName}].{propertyName}";

            for (int i = 0; i < _properties.Count; i++)
            {
                if(_properties[i].propertyPath == key)
                {
                    _selectedProperty = _properties[i];
                    _selectedPropertyPath = _selectedProperty.propertyPath;
                    return;
                }
            }
        }

        public void SetPropertyPath(UnityEngine.Object newObject, string propertyPath)
        {
            if (newObject == null) { return; }

            UpdateProperties(newObject, true);

            for (int i = 0; i < _properties.Count; i++)
            {
                if (_properties[i].propertyPath == propertyPath)
                {
                    _selectedProperty = _properties[i];
                    _selectedPropertyPath = _selectedProperty.propertyPath;
                    return;
                }
            }
        }

        private void UpdateProperties(UnityEngine.Object newObject, bool forced) {
            _object = newObject;

            GameObject gameObject = newObject is Component ? ((Component)newObject).gameObject :
                                                             newObject as GameObject;

            if (gameObject != null) {
                if (!forced) {
                    // Try and get whether to update properties or not
                    bool skipUpdate = true;
                    foreach(var component in gameObject.GetComponents(typeof(Component))) {
                        if(component == null) { continue; }
                        if (!_cachedComponents.ContainsKey(component.GetType())) {
                            skipUpdate = false;
                            break;
                        }
                    }
                    if (skipUpdate) { return; }
                }
                // Start scanning for properties and group them accordingly
                UpdateProperties(gameObject);
            }
        }

        private void UpdateProperties(GameObject gameObject) {
            _properties.Clear();
            foreach (var component in gameObject.GetComponents(typeof(Component))) {
                if (component == null) { continue; }
                Type type = component.GetType();
                List<PropertyIdentifier> properties = null;
                if (!_cachedComponents.TryGetValue(type, out properties)) {
                    properties = new List<PropertyIdentifier>();
                    if (Validate(type)) {
                        string typename = "[" + type.AssemblyQualifiedName + "]";
                        ParsePropertiesRecursive(properties, _bindingFlags, type, true, false, type.Name + '/', typename + '.', "");

                        // Escape properties rich text
                        foreach (var prop in properties) {
                            var splits = prop.propertyRichTextPath.Split(']');
                            if (splits.Length == 1) { continue; }
                            //var innerSplits = splits[0].Remove(0, 1).Replace(type.AssemblyQualifiedName, type.Name).Split('.');
                            prop.propertyRichTextPath = type.Name + splits[1];
                        }
                    }
                    _cachedComponents.Add(type, properties);
                }

                _properties.AddRange(properties);
            }

            // Assign indices
            ReassignIndices();
        }

        private void ReassignIndices()
        {
            bool hasProperty = !string.IsNullOrEmpty(_selectedPropertyPath); // whether the current selected object has the property or not
            if (hasProperty /*&& _selectedPropertyPath[0] != '[' && !_selectedPropertyPath.Contains(" ")*/)
            {
                _selectedPropertyPath = MakeCompatible(_selectedPropertyPath);
            }
            _propertyIndex = -1;
            for (int i = 0; i < _properties.Count; i++)
            {
                if (hasProperty && _properties[i].propertyPath == _selectedPropertyPath)
                {
                    _propertyIndex = i;
                    hasProperty = false;
                }
                _properties[i].index = i;
            }
            if (_propertyIndex < 0)
            {
                _selectedProperty = null;
                _selectedPropertyPath = null;
            }
        }

        private static bool ParsePropertiesRecursive(List<PropertyIdentifier> properties,
                                              BindingFlags bindingFlags,
                                              Type type,
                                              bool isEditable,
                                              bool showPrimitivesOnly,
                                              string currentMenuItemPath,
                                              string currentPropertyPath,
                                              string controlPath)
        {
            bool isValid = false;
            foreach (var field in GetFields(bindingFlags, type, false))
            {
                if (Validate(field)
                    && ValidatePath(controlPath, field.FieldType, field.Name)
                    && !TypesFilters.IsPropertyHidden(field))
                {
                    TryCreatePropertyIdentifier(properties,
                                                field.Name,
                                                bindingFlags,
                                                field.FieldType,
                                                field,
                                                isEditable && !field.IsInitOnly,
                                                showPrimitivesOnly,
                                                currentMenuItemPath,
                                                currentPropertyPath,
                                                controlPath);
                    isValid = true;
                }
            }

            foreach (var property in GetProperties(bindingFlags, type, false))
            {
                if (Validate(property)
                    && ValidatePath(controlPath, property.PropertyType, property.Name)
                    && !TypesFilters.IsPropertyHidden(property))
                {
                    TryCreatePropertyIdentifier(properties,
                                                property.Name,
                                                bindingFlags,
                                                property.PropertyType,
                                                property,
                                                //#if NET_4_6
                                                isEditable && property.SetMethod != null && property.SetMethod.IsPublic,
                                                //#else
                                                //                                                isEditable && property.GetSetMethod() != null && property.GetSetMethod().IsPublic,
                                                //#endif
                                                showPrimitivesOnly,
                                                currentMenuItemPath,
                                                currentPropertyPath,
                                                controlPath);
                    isValid = true;
                }
            }

            return isValid;
        }

        private static IEnumerable<PropertyInfo> GetProperties(BindingFlags bindingFlags, Type type, bool recurse)
        {
            if(s_cachedProperties.TryGetValue(type, out IEnumerable<PropertyInfo> list))
            {
                return list;
            }
            List<PropertyInfo> properties = new List<PropertyInfo>(type.GetProperties(bindingFlags));
            if (recurse)
            {
                type = type.BaseType;
                while(type != null && type != typeof(object))
                {
                    foreach (var property in type.GetProperties(bindingFlags))
                    {
                        if(!properties.Any(p => p.Name == property.Name))
                        {
                            properties.Add(property);
                        }
                    }
                    type = type.BaseType;
                }
            }
            s_cachedProperties[type] = properties;
            return properties;
        }

        private static IEnumerable<FieldInfo> GetFields(BindingFlags bindingFlags, Type type, bool recurse)
        {
            if (s_cachedFields.TryGetValue(type, out IEnumerable<FieldInfo> list))
            {
                return list;
            }
            List<FieldInfo> fields = new List<FieldInfo>(type.GetFields(bindingFlags));
            if (recurse)
            {
                type = type.BaseType;
                while (type != null && type != typeof(object))
                {
                    foreach (var field in type.GetFields(bindingFlags))
                    {
                        if (!fields.Any(f => f.Name == field.Name))
                        {
                            fields.Add(field);
                        }
                    }
                    type = type.BaseType;
                }
            }
            s_cachedFields[type] = fields;
            return fields;
        }

        private static void TryCreatePropertyIdentifier(List<PropertyIdentifier> properties,
                                                 string name,
                                                 BindingFlags bindingFlags,
                                                 Type type,
                                                 MemberInfo memberInfo,
                                                 bool isEditable,
                                                 bool showPrimitivesOnly,
                                                 string currentMenuItemPath,
                                                 string currentPropertyPath,
                                                 string controlPath)
        {
            // Handle BaseClasses

            var propertyId = new PropertyIdentifier()
            {
                propertyName = name,
                type = type,
                memberInfo = memberInfo,
                isEditable = isEditable
            };

            string propertyType = TypesFilters.GetTypeAlias(type);
            controlPath += String.Concat(type.Name, ".", name);
            propertyId.propertyPath = currentPropertyPath + name;
            showPrimitivesOnly |= memberInfo.GetCustomAttribute<ShowPrimitivesOnlyAttribute>() != null;
            if (!showPrimitivesOnly || type.IsPrimitive)
            {
                propertyId.propertyRichTextPath = String.Concat(currentPropertyPath, "<b>", name, "</b>");
                properties.Add(propertyId);
            }
            if (ShouldInspect(type, memberInfo) && ParsePropertiesRecursive(properties,
                                                                   bindingFlags,
                                                                   type,
                                                                   isEditable || !type.IsValueType,
                                                                   showPrimitivesOnly,
                                                                   String.Concat(currentMenuItemPath, name, "/"),
                                                                   propertyId.propertyPath + '.',
                                                                   controlPath))
            {
                propertyId.menuItem = String.Concat(currentMenuItemPath, name, "/", propertyType, " ", name);
            }
            else
            {
                propertyId.menuItem = String.Concat(currentMenuItemPath, propertyType, " ", name);
            }
        }

        private static bool ValidatePath(string controlPath, Type type, string name)
        {
            return !controlPath.Contains(string.Concat(type.Name, ".", name));
        }

        private static bool Validate(Type type)
        {
            return !(TypesFilters.IsPropertyPathHidden(type) || type.IsArray || type.IsGenericType || type == typeof(Type) || type.GetCustomAttribute<DoNotExposeAttribute>() != null);
        }

        private static bool Validate(PropertyInfo propInfo)
        {
            if (TypesFilters.IsPropertyPathHidden(propInfo.PropertyType)
                || propInfo.PropertyType.IsArray
                || propInfo.PropertyType.IsGenericType
                || propInfo.PropertyType == typeof(Type)
                || propInfo.PropertyType.GetCustomAttribute<DoNotExposeAttribute>() != null
                || TypesFilters.IsPropertyPathHidden(propInfo.DeclaringType)
                //|| propInfo.DeclaringType.IsArray
                //|| propInfo.DeclaringType.IsGenericType
                || propInfo.DeclaringType == typeof(Type)
                || propInfo.DeclaringType.GetCustomAttribute<DoNotExposeAttribute>() != null)
            {
                return false;
            }

            return propInfo.GetCustomAttribute<DoNotExposeAttribute>() == null;
        }

        private static bool Validate(FieldInfo fieldInfo)
        {
            if (TypesFilters.IsPropertyPathHidden(fieldInfo.FieldType)
                || fieldInfo.FieldType.IsArray
                || fieldInfo.FieldType.IsGenericType
                || fieldInfo.FieldType == typeof(Type)
                || fieldInfo.FieldType.GetCustomAttribute<DoNotExposeAttribute>() != null
                || TypesFilters.IsPropertyPathHidden(fieldInfo.DeclaringType)
                || fieldInfo.DeclaringType.IsArray
                || fieldInfo.DeclaringType.IsGenericType
                || fieldInfo.DeclaringType == typeof(Type)
                || fieldInfo.DeclaringType.GetCustomAttribute<DoNotExposeAttribute>() != null)
            {
                return false;
            }

            return fieldInfo.GetCustomAttribute<DoNotExposeAttribute>() == null;
        }

        private static bool ShouldInspect(Type type, MemberInfo memberInfo)
        {
            return TypesFilters.IsInspectable(type) && !type.IsEnum && !type.IsPrimitive
                && memberInfo.GetCustomAttribute<HideInternalsAttribute>() == null
                && type.GetCustomAttribute<HideInternalsAttribute>() == null;
        }

        /// <summary>
        /// Helper method to make old versions of property paths compatible with new ones
        /// </summary>
        /// <param name="legacyPropertyPath">The potentially old version of property path format</param>
        /// <returns>The modernized version of <paramref name="legacyPropertyPath"/></returns>
        private string MakeCompatible(string legacyPropertyPath)
        {
            if (legacyPropertyPath[0] == '[')
            {
                var typename = legacyPropertyPath.Substring(1, legacyPropertyPath.IndexOf(']') - 1);
                Type type = Type.GetType(typename);
                return type != null ? legacyPropertyPath.Replace(typename, type.AssemblyQualifiedName) : legacyPropertyPath;
            }

            int dotIndex = legacyPropertyPath.IndexOf('.');
            var componentTypename = legacyPropertyPath.Substring(0, dotIndex);
            Type componentType = null;
            if (_object != null && (_object is GameObject || _object is Component))
            {
                var component = _object is GameObject ?
                                (_object as GameObject).GetComponent(componentTypename) :
                                (_object as Component).GetComponent(componentTypename);

                if (component != null) { componentType = component.GetType(); }
            }
            if (componentType == null)
            {
                componentType = Type.GetType(componentTypename);
            }
            return String.Concat("[", componentType == null ? componentTypename : componentType.AssemblyQualifiedName, "]",
                                legacyPropertyPath.Substring(dotIndex, legacyPropertyPath.Length - dotIndex));
        }

        #endregion  // Methods

        #endregion  // Properties Related

        #region Drawing Related

        private void ChangeSelectedProperty(object index)
        {
            int newIndex = (int)index;
            if (_propertyIndex != newIndex)
            {
                _propertyIndex = newIndex;
                if (0 <= _propertyIndex && _propertyIndex < _properties.Count)
                {
                    _selectedProperty = _properties[_propertyIndex];
                    _selectedPropertyPath = _selectedProperty.propertyPath;
                }
            }
        }

        /// <summary>
        /// Draw the field responsible for selecting the property path
        /// </summary>
        /// <param name="rect">The rect where to draw it</param>
        /// <param name="label">The label of this field</param>
        /// <param name="obj">The object to show the properties for</param>
        /// <param name="propertyPath">The current property path</param>
        /// <param name="forEdit">Whether to show properties for edit or for read only</param>
        /// <param name="showFullPath">Whether to show the full path or only the property name</param>
        /// <param name="filterType">If set, show only properties of that type</param>
        public string DrawPropertyPathField(Rect rect, string label, UnityEngine.Object obj, string propertyPath, bool forEdit, bool showFullPath, Type filterType)
        {
            if (obj == null)
            {
                return null;
            }
            var labelRect = rect;
            labelRect.width = EditorGUIUtility.labelWidth;
            EditorGUI.LabelField(labelRect, label);
            rect.width -= labelRect.width;
            rect.x += labelRect.width;
            return DrawPropertyPathField(rect, obj, propertyPath, forEdit, showFullPath, filterType);
        }

        /// <summary>
        /// Draw the field responsible for selecting the property path
        /// </summary>
        /// <param name="rect">The rect where to draw it</param>
        /// <param name="label">The label of this field</param>
        /// <param name="obj">The object to show the properties for</param>
        /// <param name="propertyPath">The current property path</param>
        /// <param name="forEdit">Whether to show properties for edit or for read only</param>
        /// <param name="showFullPath">Whether to show the full path or only the property name</param>
        public string DrawPropertyPathField(Rect rect, string label, UnityEngine.Object obj, string propertyPath, bool forEdit, bool showFullPath)
        {
            if (obj == null)
            {
                return null;
            }
            var labelRect = rect;
            labelRect.width = EditorGUIUtility.labelWidth;
            EditorGUI.LabelField(labelRect, label);
            rect.width -= labelRect.width;
            rect.x += labelRect.width;
            return DrawPropertyPathField(rect, obj, propertyPath, forEdit, showFullPath);
        }

        /// <summary>
        /// Draw the field responsible for selecting the property path
        /// </summary>
        /// <param name="rect">The rect where to draw it</param>
        /// <param name="obj">The object to show the properties for</param>
        /// <param name="propertyPath">The current property path</param>
        /// <param name="forEdit">Whether to show properties for edit or for read only</param>
        /// <param name="showFullPath">Whether to show the full path or only the property name</param>
        /// <param name="filterType">If set, show only properties of that type</param>
        public string DrawPropertyPathField(Rect rect, UnityEngine.Object obj, string propertyPath, bool forEdit, bool showFullPath, Type filterType)
        {
            if (obj == null)
            {
                return null;
            }
            EditorStyles.popup.richText = true;
            if (obj != _object || (filterType != null && SelectedProperty != null && !filterType.IsAssignableFrom(SelectedProperty.type)))
            {
                _selectedPropertyPath = propertyPath;
                UpdateProperties(obj, true);
            }
            if (GUI.Button(rect, SelectedProperty != null ?
                                 (showFullPath ? _selectedProperty.propertyRichTextPath : _selectedProperty.propertyName) :
                                 _selectedPropertyPath,
                                 EditorStyles.popup))
            {
                UpdateProperties(obj, false);
                GenericMenu menu = CreatePropertiesMenu(_selectedPropertyPath, forEdit, filterType);
                menu.DropDown(rect);
            }
            return _selectedPropertyPath;
        }

        /// <summary>
        /// Draw the field responsible for selecting the property path
        /// </summary>
        /// <param name="rect">The rect where to draw it</param>
        /// <param name="obj">The object to show the properties for</param>
        /// <param name="propertyPath">The current property path</param>
        /// <param name="forEdit">Whether to show properties for edit or for read only</param>
        /// <param name="showFullPath">Whether to show the full path or only the property name</param>
        public string DrawPropertyPathField(Rect rect, UnityEngine.Object obj, string propertyPath, bool forEdit, bool showFullPath)
        {
            if (obj == null)
            {
                return null;
            }
            EditorStyles.popup.richText = true;
            if (obj != _object)
            {
                _selectedPropertyPath = propertyPath;
                UpdateProperties(obj, true);
            }
            if (GUI.Button(rect, SelectedProperty != null ?
                                 (showFullPath ? _selectedProperty.propertyRichTextPath : _selectedProperty.propertyName) :
                                 _selectedPropertyPath,
                                 EditorStyles.popup))
            {
                UpdateProperties(obj, false);
                GenericMenu menu = CreatePropertiesMenu(_selectedPropertyPath, forEdit, null);
                menu.DropDown(rect);
            }
            return _selectedPropertyPath;
        }

        /// <summary>
        /// Draw the field responsible for selecting the property path in layout mode
        /// </summary>
        /// <param name="label">The label of this field</param>
        /// <param name="obj">The object to show the properties for</param>
        /// <param name="propertyPath">The current property path</param>
        /// <param name="forEdit">Whether to show properties for edit or for read only</param>
        /// <param name="showFullPath">Whether to show the full path or only the property name</param>
        public string DrawPropertyPathField(string label, UnityEngine.Object obj, string propertyPath, bool forEdit, bool showFullPath)
        {
            if (obj == null)
            {
                return null;
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth));
            string propPath = DrawPropertyPathField(obj, propertyPath, forEdit, showFullPath, null);
            EditorGUILayout.EndHorizontal();
            return propPath;
        }

        /// <summary>
        /// Draw the field responsible for selecting the property path in layout mode
        /// </summary>
        /// <param name="obj">The object to show the properties for</param>
        /// <param name="propertyPath">The current property path</param>
        /// <param name="forEdit">Whether to show properties for edit or for read only</param>
        /// <param name="showFullPath">Whether to show the full path or only the property name</param>
        /// <param name="filterType">If set, show only properties of that type</param>
        public string DrawPropertyPathField(UnityEngine.Object obj, string propertyPath, bool forEdit, bool showFullPath, Type filterType = null)
        {
            if (obj == null)
            {
                return null;
            }
            EditorStyles.popup.richText = true;
            if (obj != _object || (filterType != null && SelectedProperty != null && !filterType.IsAssignableFrom(SelectedProperty.type)))
            {
                _selectedPropertyPath = propertyPath;
                UpdateProperties(obj, true);
            }
            if (GUILayout.Button(SelectedProperty != null ?
                                 (showFullPath ? _selectedProperty.propertyRichTextPath : _selectedProperty.propertyName) :
                                 _selectedPropertyPath,
                                 EditorStyles.popup,
                                 GUILayout.ExpandWidth(true)))
            {
                UpdateProperties(obj, false);
                GenericMenu menu = CreatePropertiesMenu(_selectedPropertyPath, forEdit, filterType);
                menu.DropDown(_lastRect);
            }
            else if (Event.current.type == EventType.Repaint)
            {
                _lastRect = GUILayoutUtility.GetLastRect();
            }
            return _selectedPropertyPath;
        }

        private GenericMenu CreatePropertiesMenu(string currentValue, bool forEdit, Type filterType)
        {
            GenericMenu menu = new GenericMenu();
            var properties = forEdit && filterType != null ?
                            _properties.Where(p => p.isEditable && filterType.IsAssignableFrom(p.type)) :
                            forEdit ? _properties.Where(p => p.isEditable) :
                            filterType != null ? _properties.Where(p => filterType.IsAssignableFrom(p.type)) : _properties;

            foreach (var property in properties)
            {
                menu.AddItem(new GUIContent(property.menuItem),
                             currentValue == property.propertyPath,
                             ChangeSelectedProperty,
                             property.index);
            }

            return menu;
        }

        #endregion

        public class PropertyIdentifier
        {
            internal string menuItem;
            public int index;
            public bool isEditable;
            public Type type;
            public MemberInfo memberInfo;
            public string propertyName;
            public string propertyPath;
            internal string propertyRichTextPath;
        }

    }

    //#if !NET_4_6 && !UNITY_WSA_10_0
    //    public static class TypeExtentions
    //    {
    //        public static T GetCustomAttribute<T>(this Type type) where T : Attribute {
    //            var attribs = type.GetCustomAttributes(typeof(T), true);
    //            return attribs != null && attribs.Length > 0 ? (T)attribs[0] : null;
    //        }

    //        public static T GetCustomAttribute<T>(this MemberInfo memberInfo) where T : Attribute {
    //            var attribs = memberInfo.GetCustomAttributes(typeof(T), true);
    //            return attribs != null && attribs.Length > 0 ? (T)attribs[0] : null;
    //        }
    //    }
    //#endif
}