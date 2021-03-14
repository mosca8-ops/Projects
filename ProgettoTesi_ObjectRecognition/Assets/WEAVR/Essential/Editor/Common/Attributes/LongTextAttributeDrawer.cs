using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [CustomPropertyDrawer(typeof(LongTextAttribute))]
    public class LongTextAttributeDrawer : ComposablePropertyDrawer
    {
        private const float k_indentPerLevel = 15;

        private class Styles : BaseStyles
        {
            public GUIStyle text;

            protected override void InitializeStyles(bool isProSkin)
            {
                text = new GUIStyle(EditorStyles.textField);
                text.wordWrap = true;
            }
        }

        private static readonly Styles s_style = new Styles();
        private static readonly GUIContent s_content = new GUIContent();

        private float? m_width;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                base.OnGUI(position, property, label);
                return;
            }
            s_style.Refresh();
            if (Event.current.type == EventType.Repaint)
            {
                if (label != GUIContent.none)
                {
                    m_width = position.width - EditorGUIUtility.labelWidth - 2;
                }
                else
                {
                    m_width = position.width;
                }
            }
            EditorGUI.BeginProperty(position, label, property);
            //property.stringValue = EditorGUI.TextField(position, label, property.stringValue, s_style.text);
            position = EditorGUI.PrefixLabel(position, label);
            property.stringValue = EditorGUI.TextArea(position, property.stringValue, s_style.text);
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                return base.GetPropertyHeight(property, label);
            }
            s_style.Refresh();
            s_content.text = property.stringValue;
            return m_width.HasValue ? s_style.text.CalcHeight(s_content, m_width.Value) : base.GetPropertyHeight(property, label);
        }
    }
}
