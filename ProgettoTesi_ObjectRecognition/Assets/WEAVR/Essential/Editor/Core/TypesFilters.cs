namespace TXT.WEAVR.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;

    public class TypesFilters
    {
        public enum FilterType
        {
            PropertyHiddenTypes,
            PropertyNotInspectableTypes
        }

        private static readonly HashSet<Type> _hiddenTypes = new HashSet<Type>();
        private static readonly HashSet<Type> _notInspectableTypes = new HashSet<Type>();
        private static readonly HashSet<string> _hiddenProperties = new HashSet<string>();

        private static readonly Dictionary<Type, string> _aliases = new Dictionary<Type, string>();

        /// <summary>
        /// Add the types to the speficied filter type
        /// </summary>
        /// <param name="filter">The filter where to add the types</param>
        /// <param name="types">The types to add</param>
        public static void Add(FilterType filter, params Type[] types) {
            switch (filter) {
                case FilterType.PropertyHiddenTypes:
                    foreach (var type in types) {
                        _hiddenTypes.Add(type);
                    }
                    break;
                case FilterType.PropertyNotInspectableTypes:
                    foreach (var type in types) {
                        _notInspectableTypes.Add(type);
                    }
                    break;
            }
        }

        /// <summary>
        /// Registers a property to hide when searching for properties
        /// </summary>
        /// <param name="type">The type where the property was declared</param>
        /// <param name="propertyName">The name of the property</param>
        public static void HideProperty(Type type, string propertyName) {
            _hiddenProperties.Add(type.Name + "." + propertyName);
        }

        /// <summary>
        /// Gets whether the type is hidden or not when searching for properties
        /// </summary>
        /// <param name="type">The type to test</param>
        /// <returns>True if the type will be skipped when searching for properties, false otherwise</returns>
        public static bool IsPropertyPathHidden(Type type) {
            return _hiddenTypes.Contains(type) || (type.DeclaringType != null && _hiddenTypes.Contains(type.DeclaringType));
        }

        /// <summary>
        /// Gets whether the type should be further inspected when searching for properties
        /// </summary>
        /// <param name="type">The type to test</param>
        /// <returns>True whether the type should be further inspected</returns>
        public static bool IsInspectable(Type type) {
            return !_notInspectableTypes.Contains(type);
        }

        /// <summary>
        /// Gets whether the property is visible when searching for properties or not
        /// </summary>
        /// <param name="property">The <see cref="PropertyInfo"/> to get the name from</param>
        /// <returns>True if the property is hidden</returns>
        public static bool IsPropertyHidden(PropertyInfo property) {
            return _hiddenProperties.Contains(property.DeclaringType.Name + "." + property.Name);
        }

        /// <summary>
        /// Gets whether the property is visible when searching for properties or not
        /// </summary>
        /// <param name="field">The field to test</param>
        /// <returns>True if the property is hidden</returns>
        public static bool IsPropertyHidden(FieldInfo field) {
            return _hiddenProperties.Contains(field.DeclaringType.Name + "." + field.Name);
        }

        /// <summary>
        /// Gets whether the property is visible when searching for properties or not
        /// </summary>
        /// <param name="type">The declaring type of the property</param>
        /// <param name="propertyName">The property name</param>
        /// <returns>True if the property is hidden</returns>
        public static bool IsPropertyHidden(Type type, string propertyName) {
            return _hiddenProperties.Contains(type.Name + "." + propertyName);
        }

        /// <summary>
        /// Gets whether the property is visible when searching for properties or not
        /// </summary>
        /// <param name="propertyName">The property name of the property</param>
        /// <returns>True if the property is hidden</returns>
        public static bool IsPropertyHidden(string propertyName) {
            return _hiddenProperties.Contains(propertyName);
        }

        /// <summary>
        /// Registers an alias for a type
        /// </summary>
        /// <param name="type">The type to register the alias for</param>
        /// <param name="alias">The alias of the type</param>
        public static void RegisterTypeAlias(Type type, string alias) {
            _aliases.Add(type, alias);
        }

        /// <summary>
        /// Gets the type alias, if any, otherwise returns the type name
        /// </summary>
        /// <param name="type">The type to get the alias for</param>
        /// <returns>The type alias or, if none, the name of the type</returns>
        public static string GetTypeAlias(Type type) {
            string alias = null;
            if(_aliases.TryGetValue(type, out alias)) {
                return alias;
            }
            return type.Name;
        }
    }
}
