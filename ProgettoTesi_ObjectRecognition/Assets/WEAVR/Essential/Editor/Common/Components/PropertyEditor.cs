using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Editor;
using TXT.WEAVR.Core;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace TXT.WEAVR
{
    [CustomPropertyDrawer(typeof(Property))]
    public class PropertyEditor : ComposablePropertyDrawer
    {
        private PropertyPathField m_propertyPathField;
        private Property m_property;
        private string m_targetPath;
        private string m_targetPathNiceName;
        private bool m_initialized;
        private bool m_isSetter;

        private Property m_typeProperty;
        private Func<Type> m_getFilterType;

        private Type m_lastFilterType;

        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!m_initialized)
            {
                m_initialized = true;
                Initialize(property);
            }

            //EditorGUI.BeginChangeCheck();
            EditorGUI.BeginProperty(position, label, property);

            var path = property.FindPropertyRelative("m_path");

            var currentFilterType = m_typeProperty?.PropertyType ?? m_getFilterType?.Invoke();
            if(m_lastFilterType != currentFilterType)
            {
                path.stringValue = m_property.Path = string.Empty; 
            }
            m_lastFilterType = currentFilterType;

            if (string.IsNullOrEmpty(m_targetPath))
            {
                path.stringValue = m_property.Path = m_propertyPathField.DrawPropertyPathField(position, label.text, property.serializedObject.targetObject, path.stringValue, m_isSetter, true, currentFilterType);
                property.FindPropertyRelative("m_propertyName").stringValue = m_propertyPathField.SelectedProperty?.propertyName;
                m_property.MemberInfo = m_propertyPathField.SelectedProperty?.memberInfo;
                m_property.Target = property.serializedObject.targetObject;
                property.FindPropertyRelative("m_targetTypename").stringValue = m_property.TargetTypename = property.serializedObject.targetObject.GetType().AssemblyQualifiedName;
                property.FindPropertyRelative("m_propertyTypename").stringValue = m_property.PropertyTypename = m_propertyPathField.SelectedProperty?.type.AssemblyQualifiedName;
            }
            else
            {
                var objProperty = property.serializedObject.FindProperty(m_targetPath);
                if (objProperty != null)
                {
                    if (objProperty.objectReferenceValue)
                    {
                        path.stringValue = m_property.Path = m_propertyPathField.DrawPropertyPathField(position, label.text, objProperty.objectReferenceValue, path.stringValue, m_isSetter, true, currentFilterType);
                        property.FindPropertyRelative("m_propertyName").stringValue = m_propertyPathField.SelectedProperty?.propertyName;
                        m_property.MemberInfo = m_propertyPathField.SelectedProperty?.memberInfo;
                        m_property.Target = objProperty.objectReferenceValue;
                        property.FindPropertyRelative("m_targetTypename").stringValue = m_property.TargetTypename = objProperty.objectReferenceValue.GetType().AssemblyQualifiedName;
                        property.FindPropertyRelative("m_propertyTypename").stringValue = m_property.PropertyTypename = m_propertyPathField.SelectedProperty?.type.AssemblyQualifiedName;
                    }
                    else
                    {
                        var style = EditorStyles.centeredGreyMiniLabel;
                        bool wasRichText = style.richText;
                        style.richText = true;
                        EditorGUI.LabelField(position, label.text, $"No object at <color=#ffa500ff>{m_targetPathNiceName}</color>", style);
                        style.richText = wasRichText;
                    }
                }
                else
                {
                    var style = EditorStyles.centeredGreyMiniLabel;
                    bool wasRichText = style.richText;
                    style.richText = true;
                    EditorGUI.LabelField(position, label.text, $"Unable to find <color=#ffa500ff>{m_targetPath}</color>", style);
                    style.richText = wasRichText;
                }
            }

            EditorGUI.EndProperty();
            //if (EditorGUI.EndChangeCheck())
            //{
            //    m_property?.MakeDirty();
            //}
        }

        private void Initialize(SerializedProperty property)
        {
            m_propertyPathField = new PropertyPathField();
            var propertyTarget = fieldInfo.GetAttribute<PropertyDataFromAttribute>();
            if(propertyTarget != null)
            {
                m_isSetter = propertyTarget.IsSetter;
                var innerProperty = property.serializedObject.FindProperty(propertyTarget.TargetFieldName);
                if (innerProperty != null)
                {
                    m_targetPath = innerProperty.propertyPath;
                    m_targetPathNiceName = innerProperty.displayName;
                }
                string typePath = propertyTarget.TypeFieldName;
                if (!string.IsNullOrEmpty(typePath))
                {
                    try
                    {
                        m_typeProperty = property.serializedObject.targetObject.GetType().FieldPathGet(typePath)?
                                        .Invoke(property.serializedObject.targetObject) as Property;
                    }
                    catch(Exception e)
                    {
                        Debug.Log($"Property error: {e.Message}");
                    }
                }
                else if (!string.IsNullOrEmpty(propertyTarget.TypeFilterGetMethod))
                {
                    try
                    {
                        m_getFilterType = Delegate.CreateDelegate(typeof(Func<Type>), property.serializedObject.targetObject, propertyTarget.TypeFilterGetMethod) as Func<Type>;
                    }
                    catch(Exception e)
                    {
                        Debug.Log($"Property Type Filter Method error: {e.Message}");
                    }
                }
            }
            m_property = fieldInfo.GetValue(property.serializedObject.targetObject) as Property;
            m_lastFilterType = m_typeProperty?.PropertyType ?? m_getFilterType?.Invoke();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }


        private struct ChangeCheck
        {
            private string m_value;
            public bool HasChanged { get; private set; }
            public string Value
            {
                get => m_value;
                set
                {
                    if(m_value != value)
                    {
                        m_value = value;
                        HasChanged = true;
                    }
                }
            }

            public void Reset()
            {
                HasChanged = false;
            }
        }
    }
}
