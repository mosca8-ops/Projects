namespace TXT.WEAVR.Utility
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;

    public class PropertyUtility
    {
        private static readonly Dictionary<string, PropertyInfo> _cachedProperties = new Dictionary<string, PropertyInfo>();

        public static PropertyInfo GetProperty(Type type, string propertyPath) {
            if (string.IsNullOrEmpty(propertyPath)) {
                return null;
            }

            // Split the entire path
            var splits = propertyPath.Split('.');
            if(splits.Length == 1) {
                // The path is one level only, so get the first encountered property
                return (type ?? typeof(GameObject)).GetProperty(propertyPath);
            }

            // The first string should represent the 
            type = type ?? typeof(GameObject);

            PropertyInfo foundProperty = null;

            for (int i = 0; i < splits.Length; i++) {
                foundProperty = type.GetProperty(splits[i]);
                if(foundProperty == null) {
                    return null;
                }
                type = foundProperty.GetType();
            }

            return foundProperty;
        }

        private static PropertyInfo GetProperty(UnityEngine.Object obj, string propertyPath) {
            return null;
        }

        private static PropertyInfo GetProperty(GameObject gameObject, string propertyPath) {
            return null;
        }
    }
}