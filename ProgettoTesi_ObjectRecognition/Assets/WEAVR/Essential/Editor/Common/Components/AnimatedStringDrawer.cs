using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR
{
    [CustomPropertyDrawer(typeof(AnimatedString), true)]
    public class AnimatedStringDrawer : AnimatedValueDrawer
    {
        private class TextStyles : BaseStyles
        {
            public GUIStyle text;

            protected override void InitializeStyles(bool isProSkin)
            {
                text = new GUIStyle(EditorStyles.textField);
                text.wordWrap = true;
            }
        }

        private static readonly TextStyles s_style = new TextStyles();
        private static readonly GUIContent s_content = new GUIContent();

        private float? m_width;
        
        protected override void DrawTarget(Rect position, SerializedProperty property, GUIContent label)
        {
            if (label != GUIContent.none)
            {
                m_width = position.width - EditorGUIUtility.labelWidth - 2;
            }
            else
            {
                m_width = position.width;
            }
            EditorGUI.BeginProperty(position, label, property);
            //property.stringValue = EditorGUI.TextField(position, label, property.stringValue, s_style.text);
            position = EditorGUI.PrefixLabel(position, label);
            property.stringValue = EditorGUI.TextArea(position, property.stringValue, s_style.text);
            EditorGUI.EndProperty();
        }

        protected override float GetTargetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            s_style.Refresh();
            s_content.text = property.stringValue;
            return s_style.text.CalcHeight(s_content, m_width ?? EditorGUIUtility.currentViewWidth - (label != GUIContent.none ? EditorGUIUtility.labelWidth : 0));
        }
    }
}
