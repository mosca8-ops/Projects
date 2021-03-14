using UnityEngine;
using UnityEditor;
using System;

namespace TXT.WEAVR.Common
{
    [CustomPropertyDrawer(typeof(ShowAsEulerAttribute))]
    public class ShowAsEulerAttributeDrawer : Editor.ComposablePropertyDrawer
    {
        private Type m_propertyType;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Quaternion)
            {
                EditorGUI.BeginChangeCheck();
                var newEuler = EditorGUI.Vector3Field(position, label, property.quaternionValue.eulerAngles);
                if (EditorGUI.EndChangeCheck())
                {
                    property.quaternionValue = Quaternion.Euler(newEuler);
                }
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
