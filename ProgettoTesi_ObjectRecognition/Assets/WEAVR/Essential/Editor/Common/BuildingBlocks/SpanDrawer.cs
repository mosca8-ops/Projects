    using TXT.WEAVR.Editor;
    using UnityEditor;
    using UnityEngine;

namespace TXT.WEAVR.Common
{
    [CustomPropertyDrawer(typeof(Span))]
    public class SpanDrawer : ComposablePropertyDrawer
    {
        private const float defaultWidth = 60;
        private const float miniLabelWidth = 30;

        private class Styles : BaseStyles
        {
            public GUIStyle minLabel;
            public GUIStyle maxLabel;

            protected override void InitializeStyles(bool isProSkin)
            {
                minLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                minLabel.alignment = TextAnchor.MiddleLeft;

                maxLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                maxLabel.alignment = TextAnchor.MiddleRight;
            }
        }

        private Styles m_styles = new Styles();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var minProperty = property.FindPropertyRelative("min");
            var maxProperty = property.FindPropertyRelative("max");
            var tempRect = position;
            var miniLabelRect = position;

            m_styles?.Refresh();

            tempRect.width = EditorGUIUtility.labelWidth - EditorGUI.indentLevel * EditorGUIUtility.singleLineHeight;
            EditorGUI.LabelField(tempRect, label);

            tempRect.x += tempRect.width;
            tempRect.width = position.width - tempRect.width;

            float rectWidth = Mathf.Min(defaultWidth, tempRect.width * 0.5f);
            float labelWidth = 40;// Mathf.Max((position.width - EditorGUIUtility.labelWidth - rectWidth * 2) * 0.5f, miniLabelWidth);
            tempRect.width = rectWidth;

            miniLabelRect.width = labelWidth;
            miniLabelRect.x = tempRect.x + tempRect.width;

            var lastLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.BeginChangeCheck();

            minProperty.floatValue = EditorGUI.DelayedFloatField(tempRect, minProperty.floatValue);
            EditorGUI.LabelField(miniLabelRect, "min", m_styles.minLabel);
            tempRect.x = position.width - tempRect.width + position.x;

            miniLabelRect.x = tempRect.x - labelWidth;
            EditorGUI.LabelField(miniLabelRect, "max", m_styles.maxLabel);
            maxProperty.floatValue = EditorGUI.DelayedFloatField(tempRect, maxProperty.floatValue);

            if (EditorGUI.EndChangeCheck())
            {
                //foreach(var t in property.serializedObject.targetObjects)
                //{
                //    var span = (Span)fieldInfo.GetValue(t);
                //    span.Refresh();
                //}
            }

            EditorGUIUtility.labelWidth = lastLabelWidth;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}