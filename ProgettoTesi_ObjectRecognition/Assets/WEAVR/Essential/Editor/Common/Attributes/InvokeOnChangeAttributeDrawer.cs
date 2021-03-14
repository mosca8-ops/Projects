using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    [CustomPropertyDrawer(typeof(InvokeOnChangeAttribute))]
    public class InvokeOnChangeAttributeDrawer : ComposablePropertyDrawer
    {
        private bool m_initialized;
        private object m_lastValue;

        private MethodInfo m_method;
        private object[] m_dummyParameters;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if (!m_initialized) {
                m_lastValue = GetValue(property, fieldInfo);
                m_dummyParameters = new object[0];
                m_initialized = true;
            }
            EditorGUI.BeginChangeCheck();
            base.OnGUI(position, property, label);
            if (EditorGUI.EndChangeCheck()) {
                var newValue = GetValue(property, fieldInfo);
                if (m_lastValue != newValue) {
                    m_lastValue = newValue;
                    property.serializedObject.ApplyModifiedProperties();
                    var parent = GetPropertyParent(property);
                    GetMethodInfo(parent, (attribute as InvokeOnChangeAttribute).MethodName)?
                                 .Invoke(parent, m_dummyParameters);
                }
            }
        }
         
        private object GetPropertyParent(SerializedProperty property)
        {
            if(property.depth == 0) { return property.serializedObject.targetObject; }
            var splits = property.propertyPath.Split('.');
            int index = 1;
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            object value = property.serializedObject.targetObject;
            FieldInfo fieldInfo = value.GetType().GetField(splits[0], bindingFlags);
            while(value != null && index < splits.Length)
            {
                value = fieldInfo?.GetValue(value);
                fieldInfo = value?.GetType().GetField(splits[index++], bindingFlags);
            }
            return value;
        }

        private MethodInfo GetMethodInfo(object parent, string methodName)
        {
            if(m_method == null)
            {
                m_method = parent?.GetType().GetMethod(methodName, BindingFlags.Instance 
                                                                 | BindingFlags.Public 
                                                                 | BindingFlags.NonPublic);
            }
            return m_method;
        }

        public object GetValue(SerializedProperty property, FieldInfo fieldInfo = null) {
            switch (property.propertyType) {
                case SerializedPropertyType.AnimationCurve:
                    return property.animationCurveValue;
                case SerializedPropertyType.Boolean:
                    return property.boolValue;
                case SerializedPropertyType.Bounds:
                    return property.boundsValue;
                case SerializedPropertyType.BoundsInt:
                    return property.boundsIntValue;
                case SerializedPropertyType.Color:
                    return property.colorValue;
                case SerializedPropertyType.Enum:
                    return property.enumValueIndex;
                case SerializedPropertyType.Float:
                    return property.floatValue;
                case SerializedPropertyType.Integer:
                    return property.intValue;
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue;
                case SerializedPropertyType.Quaternion:
                    return property.quaternionValue;
                case SerializedPropertyType.Rect:
                    return property.rectValue;
                case SerializedPropertyType.RectInt:
                    return property.rectIntValue;
                case SerializedPropertyType.String:
                    return property.stringValue;
                case SerializedPropertyType.Vector2:
                    return property.vector2Value;
                case SerializedPropertyType.Vector2Int:
                    return property.vector2IntValue;
                case SerializedPropertyType.Vector3:
                    return property.vector3Value;
                case SerializedPropertyType.Vector3Int:
                    return property.vector3IntValue;
                case SerializedPropertyType.Vector4:
                    return property.vector4Value;
            }
            return fieldInfo?.GetValue(property.serializedObject.targetObject);
        }
    }
}
