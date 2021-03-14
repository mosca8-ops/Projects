namespace TXT.WEAVR.Editor
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using TXT.WEAVR.Common;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    [CustomPropertyDrawer(typeof(MeasureLabelAttribute))]
    public class MeasureLabelAttributeDrawer : ComposablePropertyDrawer
    {
        private GUIContent m_guiContent;
        private GUIStyle m_guiStyle;

        private GUIStyle GetStyle()
        {
            if(m_guiStyle == null)
            {
                m_guiStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    alignment = TextAnchor.MiddleRight,
                    padding = new RectOffset(8, 8, 0, 0),
                };
            }
            return m_guiStyle;
        }

        private GUIContent GetContent()
        {
            if(m_guiContent == null)
            {
                m_guiContent = new GUIContent((attribute as MeasureLabelAttribute).Measure);
            }
            return m_guiContent;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            base.OnGUI(position, property, label);
            if(Event.current.type == EventType.Repaint && (BaseDrawer == this || BaseDrawer == null))
            {
                if( property.propertyType == SerializedPropertyType.Float
                 || property.propertyType == SerializedPropertyType.Integer
                 || property.propertyType == SerializedPropertyType.String)
                {
                    GetStyle().Draw(position, GetContent(), false, false, false, false);
                }
            }
        }
    }
}
