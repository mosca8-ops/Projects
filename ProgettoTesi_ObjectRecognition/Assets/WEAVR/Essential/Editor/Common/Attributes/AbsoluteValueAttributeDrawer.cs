using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental;
using System;

namespace TXT.WEAVR.Common
{
    [CustomPropertyDrawer(typeof(AbsoluteValueAttribute))]
    public class AbsoluteValueAttributeDrawer : Editor.ComposablePropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if(property.propertyType == SerializedPropertyType.Integer)
            {
                property.intValue = Mathf.Abs(property.intValue);
            }
            else if(property.propertyType == SerializedPropertyType.Float)
            {
                property.floatValue = Mathf.Abs(property.floatValue);
            }
            base.OnGUI(position, property, label);
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.Integer)
            {
                property.intValue = Mathf.Abs(property.intValue);
            }
            else if (property.propertyType == SerializedPropertyType.Float)
            {
                property.floatValue = Mathf.Abs(property.floatValue);
            }
            return base.CreatePropertyGUI(property);
        }
    }
}
