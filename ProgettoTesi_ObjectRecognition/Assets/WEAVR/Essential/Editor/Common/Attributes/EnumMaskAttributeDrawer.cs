using System;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [CustomPropertyDrawer(typeof(EnumMaskAttribute))]
    public class EnumMaskAttributeDrawer : PropertyDrawer
    {
        private bool m_initialized;
        private bool m_zeroBased;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Enum)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            if (!m_initialized)
            {
                m_initialized = true;
                foreach (int value in Enum.GetValues(fieldInfo.FieldType))
                {
                    if (value == 0)
                    {
                        m_zeroBased = true;
                        break;
                    }
                }
            }

            if (m_zeroBased)
            {
                property.intValue = Mathf.Max(0, EditorGUI.MaskField(position, label, property.intValue << 1, property.enumNames) >> 1);
            }
            else
            {
                property.intValue = EditorGUI.MaskField(position, label, property.intValue, property.enumNames);
            }
        }
    }
}