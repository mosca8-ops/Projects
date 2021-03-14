using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using UnityEngine.UIElements;

using UnityEditor.UIElements;
using UnityEditor.Experimental;
using System;
using System.Linq;

namespace TXT.WEAVR.Common
{
    [CustomPropertyDrawer(typeof(PopupIntAttribute))]
    public class PopupIntAttributeDrawer : Editor.ComposablePropertyDrawer
    {
        private int[] m_values;
        private GUIContent[] m_stringValues;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if(property.propertyType == SerializedPropertyType.Integer)
            {
                if(m_values == null)
                {
                    List<int> values = new List<int>();
                    var popupAttr = attribute as PopupIntAttribute;
                    for (int i = popupAttr.Min; i <= popupAttr.Max; i += popupAttr.Step)
                    {
                        values.Add(i);
                    }
                    m_values = values.ToArray();
                    m_stringValues = values.Select(i => new GUIContent(popupAttr.Prelabel + i.ToString())).ToArray();
                }
                property.intValue = EditorGUI.IntPopup(position, label, property.intValue, m_stringValues, m_values);
                return;
            }
            base.OnGUI(position, property, label);
        }

        //public override VisualElement CreatePropertyGUI(SerializedProperty property)
        //{
        //    if (property.propertyType == SerializedPropertyType.Integer)
        //    {
        //        property.intValue = Mathf.Abs(property.intValue);
        //    }
        //    else if (property.propertyType == SerializedPropertyType.Float)
        //    {
        //        property.floatValue = Mathf.Abs(property.floatValue);
        //    }
        //    return base.CreatePropertyGUI(property);
        //}
    }
}
