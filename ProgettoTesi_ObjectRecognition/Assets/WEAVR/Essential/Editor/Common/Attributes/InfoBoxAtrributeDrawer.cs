using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [CustomPropertyDrawer(typeof(InfoBoxAttribute))]
    public class InfoBoxAttributeDrawer : DecoratorDrawer
    {
        private GUIContent m_content;
        private GUIContent GetContent()
        {
            if(m_content == null)
            {
                var attr = attribute as InfoBoxAttribute;
                Texture2D icon = null;
                switch (attr.IconType)
                {
                    case InfoBoxAttribute.InfoIconType.Error:
                        icon = WeavrStyles.Icons["error_icon"];
                        break;
                    case InfoBoxAttribute.InfoIconType.Warning:
                        icon = WeavrStyles.Icons["warning_icon"];
                        break;
                    case InfoBoxAttribute.InfoIconType.Information:
                        icon = WeavrStyles.Icons["info_icon"];
                        break;
                }
                m_content = new GUIContent("  " + attr.InfoText, icon);
            }
            return m_content;
        }

        private GUIStyle m_style;
        public GUIStyle GetStyle()
        {
            if(m_style == null)
            {
                m_style = new GUIStyle(EditorStyles.helpBox)
                {
                    richText = true,
                    fontSize = 11,
                    alignment = TextAnchor.MiddleLeft, 
                    padding = new RectOffset(8, 8, 4, 4),
                };
            }
            return m_style;
        }

        private Vector2 m_iconSize = new Vector2(20, 20);

        public override void OnGUI(Rect position)
        {
            if (Event.current.type == EventType.Repaint)
            {
                position.height -= 2;
                var attr = attribute as InfoBoxAttribute;
                using (var iconSize = new EditorGUIUtility.IconSizeScope(m_iconSize))
                {
                    GetStyle().Draw(position, GetContent(), false, false, false, false);
                }
            }
        }

        public override float GetHeight()
        {
            using (var iconSize = new EditorGUIUtility.IconSizeScope(m_iconSize))
            {
                return GetStyle().CalcHeight(GetContent(), EditorGUIUtility.currentViewWidth) + 2;
            }
        }
    }
}
