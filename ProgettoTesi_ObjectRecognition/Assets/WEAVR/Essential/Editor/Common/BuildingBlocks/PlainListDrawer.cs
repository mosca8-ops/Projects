using System;
using TXT.WEAVR.Editor;
    using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [CustomPropertyDrawer(typeof(PlainList), true)]
    public class PlainListDrawer : ComposablePropertyDrawer
    {
        private class Styles : BaseStyles
        {
            public GUIStyle box;
            public GUIStyle headerLabel;
            public GUIStyle addButton;
            public GUIStyle removeButton;

            protected override void InitializeStyles(bool isProSkin)
            {
                box = WeavrStyles.ControlsSkin.FindStyle("plainList_Box") ?? new GUIStyle("Box");
                headerLabel = WeavrStyles.ControlsSkin.FindStyle("plainList_Header") ?? new GUIStyle("Foldout")
                {
                    fontSize = 11,
                    //fontStyle = FontStyle.Bold,
                };
                addButton = WeavrStyles.ControlsSkin.FindStyle("plainList_Add") ?? new GUIStyle(EditorStyles.miniButton)
                {
                    fontSize = 14,
                };
                removeButton = WeavrStyles.ControlsSkin.FindStyle("plainList_Remove") ?? new GUIStyle(EditorStyles.miniButton)
                {
                    fontSize = 14,
                };
            }
        }

        private static Styles s_styles = new Styles();

        private GUIContent m_label;
        private ReorderableList m_list;
        private SerializedObject m_serializedObject;
        private SerializedProperty m_property;
        private SerializedProperty m_valuesProperty;
        private SerializedProperty m_isCircularProperty;
        private string m_propertyPath;

        private GUIContent m_tempContent = new GUIContent();
        private GUIContent m_isCircularContent = new GUIContent("Is Circular", "If true, this list will start from beginning if the index exceeds its size or from the end if the index goes below 0");
        private Action m_prerenderAction;

        private ReorderableList GetList(SerializedProperty property)
        {
            if(m_list == null)
            {
                m_list = new ReorderableList(property.serializedObject, property, true, true, false, false)
                {
                    showDefaultBackground = false,
                    drawHeaderCallback = List_DrawHeader,
                    footerHeight = 0,
                    headerHeight = 20,
                    drawElementCallback = List_DrawElement,
                    elementHeightCallback = List_GetElementHeight,
                    drawElementBackgroundCallback = List_DrawElementBackground,
                    drawNoneElementCallback = List_DrawNone,
                };
            }
            return m_list;
        }

        
        private void List_DrawElementBackground(Rect rect, int index, bool isActive, bool isFocused)
        {
            
        }

        private void List_DrawNone(Rect rect)
        {
            if (m_property.isExpanded)
            {
                GUI.Label(rect, "No elements");
            }
        }

        private float List_GetElementHeight(int index)
        {
            if(m_valuesProperty == null || !m_property.isExpanded) { return 0; }
            var elementHeight = EditorGUI.GetPropertyHeight(m_valuesProperty.GetArrayElementAtIndex(index), true);
            return elementHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        private void List_DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (!m_property.isExpanded) { return; }
            rect.width -= 20;
            rect.height -= EditorGUIUtility.standardVerticalSpacing;
            m_tempContent.text = $"{index}";
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 30;
            var newRect = EditorGUI.PrefixLabel(rect, m_tempContent);
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.PropertyField(newRect, m_valuesProperty.GetArrayElementAtIndex(index), GUIContent.none, true);
            if(GUI.Button(new Rect(rect.xMax + 2, rect.y, 20, EditorGUIUtility.singleLineHeight), "-", s_styles.removeButton))
            {
                m_prerenderAction = () => m_serializedObject.FindProperty(m_propertyPath).DeleteArrayElementAtIndex(index);
            }
        }

        private void List_DrawHeader(Rect rect)
        {
            s_styles.Refresh();
            if(Event.current.type == EventType.Repaint)
            {
                s_styles.box.Draw(new Rect(rect.x - 6, rect.y, rect.width + 12, rect.height), false, false, false, false);
            }
            m_property.isExpanded = EditorGUI.Foldout(new Rect(rect.x + 10, rect.y, rect.width - 140, rect.height), m_property.isExpanded, m_label, true, s_styles.headerLabel);
            if(GUI.Button(new Rect(rect.xMax - 30, rect.y, 30, EditorGUIUtility.singleLineHeight), "+", s_styles.addButton))
            {
                m_prerenderAction = () => m_serializedObject.FindProperty(m_propertyPath).InsertArrayElementAtIndex(0);
            }
            if (m_isCircularProperty != null)
            {
                m_isCircularProperty.boolValue = EditorGUI.ToggleLeft(new Rect(rect.xMax - 130, rect.y, 100, EditorGUIUtility.singleLineHeight),
                                                            m_isCircularContent, m_isCircularProperty.boolValue);
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            s_styles.Refresh();
            if (m_label == null)
            {
                m_label = new GUIContent(property.displayName);
            }

            if(m_prerenderAction != null)
            {
                m_prerenderAction();
                m_prerenderAction = null;
            }

            if(m_property == null)
            {
                m_property = property;
                m_valuesProperty = property.FindPropertyRelative("m_values");
                m_isCircularProperty = property.FindPropertyRelative("m_circular");
                m_propertyPath = m_valuesProperty.propertyPath;
                m_serializedObject = property.serializedObject;
            }

            if (Event.current.type == EventType.Repaint)
            {
                s_styles.box.Draw(position, false, false, false, false);
            }
            
            if (m_property.isExpanded)
            {
                GetList(property.FindPropertyRelative("m_values")).DoList(position);
            }
            else
            {
                position.x += 5;
                position.width -= 10;
                List_DrawHeader(position);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            m_property = property;
            m_valuesProperty = property.FindPropertyRelative("m_values");
            m_isCircularProperty = property.FindPropertyRelative("m_circular");
            m_propertyPath = m_valuesProperty?.propertyPath;
            m_serializedObject = property.serializedObject;

            return m_property?.isExpanded == true ? GetList(m_valuesProperty).GetHeight() : 20f;
        }
    }
}