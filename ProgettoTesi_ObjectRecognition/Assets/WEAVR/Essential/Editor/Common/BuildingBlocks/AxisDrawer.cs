using System;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [CustomPropertyDrawer(typeof(Axis), true)]
    public class AxisDrawer : ComposablePropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float labelWidth = EditorGUIUtility.labelWidth;
            var labelRect = new Rect(position.x, position.y, labelWidth, position.height);
            var axisRect = new Rect(position.x + labelWidth + 2, position.y, Mathf.Min(50, (position.width - labelWidth - 2) / 3), position.height);
            GUI.Label(labelRect, label);
            
            EditorGUIUtility.labelWidth = 30;
            // X
            if (GUI.Toggle(axisRect, (property.intValue & 1) != 0, "X")) { property.intValue |= 1; }
            else { property.intValue &= ~1; }
            axisRect.x += axisRect.width;
            // Y
            if (GUI.Toggle(axisRect, (property.intValue & 2) != 0, "Y")) { property.intValue |= 2; }
            else { property.intValue &= ~2; }
            axisRect.x += axisRect.width;
            // Z
            if (GUI.Toggle(axisRect, (property.intValue & 4) != 0, "Z")) { property.intValue |= 4; }
            else { property.intValue &= ~4; }

            EditorGUIUtility.labelWidth = labelWidth;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}