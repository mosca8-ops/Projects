using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEngine;


namespace TXT.WEAVR.Common
{

    [CustomPropertyDrawer(typeof(AnyTypeOfAttribute))]
    public class AnyTypeOfAttributeDrawer : PropertyDrawer
    {
        private List<Type> m_types;
        private GUIContent[] m_names;
        private int m_selectedIndex = -1;

        private Func<Type, object> m_creationCallback;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if(m_types == null)
            {
                ComputeTypes(property);
            }
            if (m_names == null || m_names.Length == 0)
            {
                base.OnGUI(position, property, label);
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                var rect = position;
                rect.height = EditorGUIUtility.singleLineHeight;
                m_selectedIndex = EditorGUI.Popup(rect, label, m_selectedIndex, m_names);
                if (EditorGUI.EndChangeCheck() && m_selectedIndex >= 0)
                {
                    SetNewInstance(property, m_creationCallback?.Invoke(m_types[m_selectedIndex]));
                }

                if (m_selectedIndex >= 0)
                {
                    EditorGUI.LabelField(rect, "Nothing Selected", EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    position.height -= rect.height;
                    position.y += rect.height;
                    label.text += " Data";
                    base.OnGUI(position, property, label);
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label) + EditorGUI.GetPropertyHeight(property, property.isExpanded);
        }

        private void ComputeTypes(SerializedProperty property)
        {
            m_creationCallback = DefaultCreationFunction;
            var attr = attribute as AnyTypeOfAttribute;
            if (!string.IsNullOrEmpty(attr.InstantiationMethod))
            {
                var creationMethod = fieldInfo.DeclaringType.GetMethod(attr.InstantiationMethod, new Type[] { typeof(Type) });
                if(creationMethod != null 
                    && creationMethod.GetParameters().Length == 1 
                    && creationMethod.GetParameters()[0].ParameterType == typeof(Type) 
                    && creationMethod.ReturnType != typeof(void))
                {
                    m_creationCallback = t => creationMethod.Invoke(property.serializedObject.targetObject, new[] { t });
                }
                else
                {
                    Debug.LogError($"AnyTypeOfAttribute: Creation Method '{attr.InstantiationMethod}' is not compatible");
                }
            }

            m_types = new List<Type>(EditorTools.GetAllSubclassesOf(attr.BaseType));
            if(m_types.Count > 0)
            {
                m_names = m_types.Select(t => new GUIContent(t.Name)).ToArray();
                var obj = fieldInfo.GetValue(property.serializedObject.targetObject);
                if(obj != null)
                {
                    m_selectedIndex = m_types.IndexOf(obj.GetType());
                }
            }
            else
            {
                Debug.LogError($"AnyTypeOfAttribute: Unable to find neither subclasses nor implementations of '{attr.BaseType.FullName}'");
            }
        }

        private object DefaultCreationFunction(Type type)
        {
            try
            {
                return Activator.CreateInstance(type);
            }
            catch (Exception)
            {
                Debug.LogError($"AnyTypeOfAttribute: Type '{type.FullName}' does not have a default constructor");
            }
            return null;
        }

        private void SetNewInstance(SerializedProperty property, object instance)
        {
            if(instance == null)
            {
                return;
            }

            fieldInfo.SetValue(property.serializedObject.targetObject, instance);
            property.serializedObject.Update();
        }
    }
}
