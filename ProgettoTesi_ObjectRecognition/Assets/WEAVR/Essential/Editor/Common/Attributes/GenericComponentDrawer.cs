using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;


namespace TXT.WEAVR.Common
{
    [CustomPropertyDrawer(typeof(GenericComponentAttribute))]
    public class GenericComponentDrawer : ComposablePropertyDrawer
    {
        private bool m_initialized;
        private bool m_fallback;

        private Type m_fieldType;

        private GenericMenu m_menuToShow;
        private Action m_preDrawAction;

        private Dictionary<string, (Object oldValue, Object newValue)> m_values = new Dictionary<string, (Object oldValue, Object newValue)>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference || m_fallback)
            {
                base.OnGUI(position, property, label);
                return;
            }

            if(Event.current.type == EventType.Layout)
            {
                return;
            }

            if (!m_values.TryGetValue(property.propertyPath, out (Object old, Object @new) value))
            {
                m_initialized = true;
                m_fieldType = fieldInfo.FieldType;
                if(m_fieldType.IsArray)
                {
                    m_fieldType = m_fieldType.GetElementType();
                }
                else if (m_fieldType.IsGenericType)
                {
                    m_fieldType = m_fieldType.GetGenericArguments()[0];
                }

                m_fallback =  !typeof(Component).IsAssignableFrom(m_fieldType);
                if (m_fallback)
                {
                    base.OnGUI(position, property, label);
                    return;
                }

                value.old = property.objectReferenceValue;
                value.@new = property.objectReferenceValue;
            }

            if(m_menuToShow != null)
            {
                if (label != null && label != GUIContent.none)
                {
                    position.x += EditorGUIUtility.labelWidth;
                    position.width -= EditorGUIUtility.labelWidth;
                }
                m_menuToShow.DropDown(position);
                m_menuToShow = null;
            }

            if (m_preDrawAction != null)
            {
                m_preDrawAction();
                m_preDrawAction = null;
            }

            if (value.@new != value.old && Event.current.type == EventType.Repaint)
            {
                if ((value.@new is Component || value.@new is GameObject))
                {
                    var go = value.@new is Component c ? c.gameObject : value.@new is GameObject g ? g : null;
                    var components = go.GetComponents(m_fieldType);
                    m_menuToShow = new GenericMenu();
                    var propertyPath = property.propertyPath;
                    var serObj = property.serializedObject;
                    foreach (var component in components)
                    {
                        var componentToSet = component;
                        m_menuToShow.AddItem(new GUIContent(component.GetType().Name), false, () => m_preDrawAction = () =>
                        {
                            serObj.FindProperty(propertyPath).objectReferenceValue = componentToSet;
                            m_values[propertyPath] = (componentToSet, componentToSet);
                        });
                    }
                }
                else if (!value.@new)
                {
                    property.objectReferenceValue = null;
                }
                value.@new = value.old;
            }

            value.old = property.objectReferenceValue;
            EditorGUI.BeginChangeCheck();
            base.OnGUI(position, property, label);
            if (EditorGUI.EndChangeCheck() && m_menuToShow == null)
            {
                value.@new = property.objectReferenceValue;
                m_values[property.propertyPath] = value;
            }
            property.objectReferenceValue = value.old;

        }
    }
}
