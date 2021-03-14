using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;

namespace TXT.WEAVR.Core {

    public static class PropertyUtility {

        public static object GetPropertyPathValue(object value, string path) {
            Type currentType = value.GetType();

            if ((value is GameObject || value is Component) && path.StartsWith("[")) {
                var splits = path.Split(']');
                var componentTypename = splits[0].Remove(0, 1);
                Type componentType = Type.GetType(componentTypename);

                value = value is GameObject ? (value as GameObject).GetComponent(componentType) 
                                            : (value as Component).GetComponent(componentType);
                path = splits[1].Substring(1);
            }

            foreach (string propertyName in path.Split('.')) {
                PropertyInfo property = currentType.GetProperty(propertyName);
                value = property.GetValue(value, null);
                if(value == null) { break; }
                currentType = property.PropertyType;
            }
            return value;
        }
    }
}