using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TXT.WEAVR.Common;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Localization
{
    [CustomPropertyDrawer(typeof(LocalizedString), useForChildren: true)]
    public class LocalizedStringDrawer : LocalizedItemDrawer
    {
        private bool m_toBeInitialized = true;
        private bool m_hasLongText = false;
        private bool m_isDelayed = false;

        private Dictionary<string, LongTextDrawer> m_drawers;

        public LocalizedStringDrawer()
        {
            m_drawers = new Dictionary<string, LongTextDrawer>();
        }

        public LocalizedStringDrawer(bool hasLongText)
        {
            m_drawers = new Dictionary<string, LongTextDrawer>();
            m_hasLongText = hasLongText;
            m_toBeInitialized = false;
        }

        protected override void TargetPropertyField(Rect position, SerializedProperty key, SerializedProperty value, GUIContent label, bool isExpanded)
        {
            if (m_toBeInitialized && value != null)
            {
                m_toBeInitialized = false;
                m_hasLongText = value.propertyType == SerializedPropertyType.String && value.GetAttributeInParents<LongTextAttribute>() != null;
                m_isDelayed = value.propertyType == SerializedPropertyType.String && value.GetAttributeInParents<DelayedTextAttribute>() != null;
            }
            if (m_hasLongText && value != null)
            {
                Get(value).OnGUI(position, value, label, m_isDelayed);
                return;
            }

            base.TargetPropertyField(position, key, value, label, isExpanded);
        }

        protected override float GetTargetPropertyHeight(SerializedProperty value)
        {
            if (m_toBeInitialized && value != null)
            {
                m_toBeInitialized = false;
                m_hasLongText = value.propertyType == SerializedPropertyType.String && value.GetAttributeInParents<LongTextAttribute>() != null;
                m_isDelayed = value.propertyType == SerializedPropertyType.String && value.GetAttributeInParents<DelayedTextAttribute>() != null;
            }
            if (m_hasLongText && value != null)
            {
                return Get(value).GetPropertyHeight(value);
            }
            
            return base.GetTargetPropertyHeight(value);
        }

        private LongTextDrawer Get(SerializedProperty property)
        {
            if(!m_drawers.TryGetValue(property.propertyPath, out LongTextDrawer drawer))
            {
                drawer = new LongTextDrawer();
                m_drawers.Add(property.propertyPath, drawer);
            }
            return drawer;
        }

        private class LongTextDrawer
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

            private string m_text;
            private GUIContent m_localLabel = new GUIContent();

            public void OnGUI(Rect position, SerializedProperty property, GUIContent label, bool delayed = false)
            {
                s_style.Refresh();
                if (position.width > 1)
                {
                    if (label != GUIContent.none)
                    {
                        m_width = position.width - EditorGUIUtility.labelWidth + 2;
                        //m_width -= EditorGUI.indentLevel * 15;
                    }
                    else
                    {
                        m_width = position.width;
                    }
                }
                EditorGUI.BeginProperty(position, label, property);
                //property.stringValue = EditorGUI.TextField(position, label, property.stringValue, s_style.text);
                position = EditorGUI.PrefixLabel(position, label);
                if (delayed)
                {
                    //if(Event.current.type == EventType.ValidateCommand)
                    //{
                    //    m_text = null;
                    //}
                    //m_text = EditorGUI.DelayedTextField(position, m_text ?? property.stringValue, s_style.text);
                    //if(Event.current.shift && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
                    //{
                    //    property.stringValue = m_text;
                    //    m_text = null;
                    //}
                    property.stringValue = EditorGUI.DelayedTextField(position, property.stringValue, s_style.text);
                }
                else
                {
                    property.stringValue = EditorGUI.TextArea(position, property.stringValue, s_style.text);
                }
                EditorGUI.EndProperty();
            }

            public float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                s_style.Refresh();
                s_content.text = property.stringValue;
                return s_style.text.CalcHeight(s_content, m_width ?? EditorGUIUtility.currentViewWidth - (label != GUIContent.none ? EditorGUIUtility.labelWidth : 0));
            }

            public float GetPropertyHeight(SerializedProperty property, string label)
            {
                m_localLabel.text = label;
                return GetPropertyHeight(property, m_localLabel);
            }

            public float GetPropertyHeight(SerializedProperty property)
            {
                m_localLabel.text = property.displayName;
                return GetPropertyHeight(property, m_localLabel);
            }
        }
    }
}
