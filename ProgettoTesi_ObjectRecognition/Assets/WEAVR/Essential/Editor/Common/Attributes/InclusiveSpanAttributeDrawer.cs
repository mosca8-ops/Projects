namespace TXT.WEAVR.Common
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(InclusiveSpanAttribute))]
    public class InclusiveSpanAttributeDrawer : PropertyDrawer
    {
        private const float defaultWidth = 60;
        private const float miniLabelWidth = 60;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            var minProperty = property.FindPropertyRelative("min");
            var maxProperty = property.FindPropertyRelative("max");
            var otherSpanProperty = property.serializedObject.FindProperty(((InclusiveSpanAttribute)attribute).ControllingSpan);
            if(minProperty == null || maxProperty == null || otherSpanProperty == null) {
                base.OnGUI(position, property, label);
                return;
            }

            var otherMinProperty = otherSpanProperty.FindPropertyRelative("min");
            var otherMaxProperty = otherSpanProperty.FindPropertyRelative("max");
            if (otherMinProperty == null || otherMaxProperty == null) {
                base.OnGUI(position, property, label);
                return;
            }

            var tempRect = position;
            var sliderRect = position;

            tempRect.width = EditorGUIUtility.labelWidth;
            EditorGUI.LabelField(tempRect, label);

            tempRect.x += tempRect.width;
            tempRect.width = position.width - tempRect.width;


            float rectWidth = Mathf.Min(defaultWidth, tempRect.width * 0.5f);
            tempRect.width = rectWidth;

            sliderRect.width = position.width - EditorGUIUtility.labelWidth - rectWidth * 2 - 4;
            sliderRect.x = tempRect.x + tempRect.width + 2;

            minProperty.floatValue = EditorGUI.FloatField(tempRect, minProperty.floatValue);
            minProperty.floatValue = Mathf.Max(otherMinProperty.floatValue, minProperty.floatValue);

            float minValue = minProperty.floatValue;
            float maxValue = maxProperty.floatValue;
            EditorGUI.MinMaxSlider(sliderRect, ref minValue, ref maxValue, otherMinProperty.floatValue, otherMaxProperty.floatValue);
            minProperty.floatValue = minValue;
            maxProperty.floatValue = maxValue;

            tempRect.x = position.width - tempRect.width + position.x;
            maxProperty.floatValue = EditorGUI.FloatField(tempRect, maxProperty.floatValue);
            maxProperty.floatValue = Mathf.Min(otherMaxProperty.floatValue, maxProperty.floatValue);
        }
    }
}