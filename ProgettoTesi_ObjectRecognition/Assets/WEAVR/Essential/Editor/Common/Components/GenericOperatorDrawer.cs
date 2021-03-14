using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace TXT.WEAVR.Core
{
    [CustomPropertyDrawer(typeof(GenericOperator))]
    public class GenericOperatorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property = property.FindPropertyRelative("m_operator");
            EditorGUI.PropertyField(position, property, label);
        }
    }
}
