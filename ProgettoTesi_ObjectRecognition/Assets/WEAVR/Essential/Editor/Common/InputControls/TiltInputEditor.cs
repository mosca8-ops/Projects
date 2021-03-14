using UnityEngine;
using UnityEditor;

namespace TXT.WEAVR.InputControls
{
    [CustomPropertyDrawer(typeof(TiltInput.AxisMapping))]
    public class TiltInputAxisStylePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float x = position.x;
            float y = position.y;
            float inspectorWidth = position.width;

            // Don't make child fields be indented
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var props = new[] { "type", "axisName" };
            var widths = new[] { .4f, .6f };
            if (property.FindPropertyRelative("type").enumValueIndex > 0)
            {
                // hide name if not a named axis
                props = new[] { "type" };
                widths = new[] { 1f };
            }
            const float lineHeight = 18;
            for (int n = 0; n < props.Length; ++n)
            {
                float w = widths[n] * inspectorWidth;

                // Calculate rects
                Rect rect = new Rect(x, y, w, lineHeight);
                x += w;

                EditorGUI.PropertyField(rect, property.FindPropertyRelative(props[n]), GUIContent.none);
            }

            // Set indent back to what it was
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}