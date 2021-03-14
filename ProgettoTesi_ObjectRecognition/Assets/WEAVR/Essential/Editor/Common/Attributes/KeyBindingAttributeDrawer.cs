using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [CustomPropertyDrawer(typeof(KeyBindingAttribute))]
    public class KeyBindingAttributeDrawer : PropertyDrawer
    {
        private static GUIContent s_KeyGUILabel = new GUIContent("Key: ");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var field = property.serializedObject.FindProperty(((KeyBindingAttribute)attribute).BindingFieldName);
            if (field == null)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            position.width -= 120;
            EditorGUI.PropertyField(position, property, label);

            position.x += position.width;
            position.width = 110;
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 40;
            EditorGUI.PropertyField(position, field.FindPropertyRelative("m_code"), s_KeyGUILabel);
            EditorGUIUtility.labelWidth = labelWidth;
        }
    }
}