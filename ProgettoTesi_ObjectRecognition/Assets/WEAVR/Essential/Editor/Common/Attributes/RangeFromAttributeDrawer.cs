using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [CustomPropertyDrawer(typeof(RangeFromAttribute))]
    public class RangeFromAttributeDrawer : ComposablePropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            RangeFromAttribute attr = attribute as RangeFromAttribute;
            if (property.propertyType == SerializedPropertyType.Integer || property.propertyType == SerializedPropertyType.Float)
            {
                float min = GetMinLimit(property, attr) + attr.MinOffset;
                float max = GetMaxLimit(property, attr) + attr.MaxOffset;
                if (property.propertyType == SerializedPropertyType.Integer)
                {
                    if (min == max)
                    {
                        bool wasEnabled = GUI.enabled;
                        GUI.enabled = false;
                        property.intValue = EditorGUI.IntField(position, label, (int)min);
                        GUI.enabled = wasEnabled;
                    }
                    else
                    {
                        EditorGUI.IntSlider(position, property, (int)min, (int)max, label);
                    }
                }
                else if (property.propertyType == SerializedPropertyType.Float)
                {
                    if (min == max)
                    {
                        bool wasEnabled = GUI.enabled;
                        GUI.enabled = false;
                        property.floatValue = EditorGUI.FloatField(position, label, min);
                        GUI.enabled = wasEnabled;
                    }
                    else
                    {
                        EditorGUI.Slider(position, property, min, max, label);
                    }
                }
            }
            else
            {
                base.OnGUI(position, property, label);
            }
        }

        private float GetMinLimit(SerializedProperty property, RangeFromAttribute attr)
        {
            if (attr.Min.HasValue)
            {
                return attr.Min.Value;
            }
            var rangeProperty = property.serializedObject.FindProperty(attr.MinField);
            if (rangeProperty != null)
            {
                if (rangeProperty.isArray)
                {
                    return rangeProperty.arraySize;
                }
                switch (rangeProperty.propertyType)
                {
                    case SerializedPropertyType.ArraySize:
                    case SerializedPropertyType.Enum:
                    case SerializedPropertyType.FixedBufferSize:
                    case SerializedPropertyType.Integer:
                    case SerializedPropertyType.LayerMask:
                        return rangeProperty.intValue;
                    case SerializedPropertyType.Float:
                        return rangeProperty.floatValue;
                }
            }
            return float.MinValue;
        }

        private float GetMaxLimit(SerializedProperty property, RangeFromAttribute attr)
        {
            if (attr.Max.HasValue)
            {
                return attr.Max.Value;
            }
            var rangeProperty = property.serializedObject.FindProperty(attr.MaxField);
            if (rangeProperty != null)
            {
                if (rangeProperty.isArray)
                {
                    return rangeProperty.arraySize;
                }
                switch (rangeProperty.propertyType)
                {
                    case SerializedPropertyType.ArraySize:
                    case SerializedPropertyType.Enum:
                    case SerializedPropertyType.FixedBufferSize:
                    case SerializedPropertyType.Integer:
                    case SerializedPropertyType.LayerMask:
                        return rangeProperty.intValue;
                    case SerializedPropertyType.Float:
                        return rangeProperty.floatValue;
                }
            }
            return float.MaxValue;
        }
    }
}