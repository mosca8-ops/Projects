using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    [CustomPropertyDrawer(typeof(ButtonAttribute))]
    public class ButtonAttributeDrawer : ComposablePropertyDrawer
    {
        private bool m_initialized;
        private float m_positionX;
        private Action m_method;
        private Func<bool> m_isValid;
        private GUIContent m_content = new GUIContent();

        private float Height => ((ButtonAttribute)attribute).Height;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var buttonAttribute = (ButtonAttribute)attribute;
            var buttonRect = position;
            if (!m_initialized)
            {
                Initialize(property);
                m_initialized = true;
            }
            float initialHeight = position.height;
            buttonRect.height = Height;

            if (buttonAttribute.Inline)
            {
                buttonRect.width = buttonAttribute.Width ?? 80;
                position.width -= buttonRect.width - 2;
                buttonRect.x += position.width + 2;
                position.center = new Vector2(position.center.x, position.y + Mathf.Max(buttonRect.height, position.height) * 0.5f);
            }
            else
            {
                buttonRect.width = buttonAttribute.Width ?? buttonRect.width;
                position.y += buttonRect.height;
                position.height = initialHeight - position.height;
            }

            m_content.text = buttonAttribute.Label;

            var color = GUI.color;
            var enabled = GUI.enabled;

            if(m_method == null) 
            { 
                GUI.color = Color.red;
                m_content.tooltip = "Button not working";
            }

            GUI.enabled = m_isValid?.Invoke() == true;

            if (GUI.Button(buttonRect, m_content))
            {
                property.serializedObject.ApplyModifiedProperties();
                m_method?.Invoke();
                property.serializedObject.Update();
            }

            GUI.enabled = enabled;
            GUI.color = color;

            base.OnGUI(position, property, label);
        }

        private void Initialize(SerializedProperty property)
        {
            m_isValid = null;
            if(property.depth == 0)
            {
                if(TryGetMethod(property.serializedObject.targetObject, property.serializedObject.targetObject.GetType()))
                {
                    if(m_isValid == null)
                    {
                        m_isValid = () => true;
                    }
                    return;
                }
            }
            var parent = property.GetParent();
            while (parent != null && m_method == null)
            {
                var fieldInfo = parent.GetFieldInfo();
                if (fieldInfo != null && TryGetMethod(parent.TryGetValue(), fieldInfo.FieldType))
                {
                    if(m_isValid == null)
                    {
                        m_isValid = () => true;
                    }
                    return;
                }
            }
        }

        private bool TryGetMethod(object target, Type type)
        {
            var button = attribute as ButtonAttribute;
            var objectType = typeof(object);
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            while (m_method == null && type != objectType)
            {
                if(m_isValid == null && !string.IsNullOrEmpty(button.ValidationMethodName))
                {
                    var validMethodInfo = type.GetMethod(button.ValidationMethodName, bindingFlags);
                    if (validMethodInfo != null)
                    {
                        m_isValid = Delegate.CreateDelegate(typeof(Func<bool>), target, validMethodInfo) as Func<bool>;
                    }
                }
                var methodInfo = type.GetMethod(button.MethodName, bindingFlags);
                if (methodInfo != null)
                {
                    m_method = Delegate.CreateDelegate(typeof(Action), target, methodInfo) as Action;
                }
                type = type.BaseType;
            }

            return m_method != null;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return ((ButtonAttribute)attribute).Inline ? Mathf.Max(base.GetPropertyHeight(property, label), Height) 
                                                       : base.GetPropertyHeight(property, label) + Height;
        }
    }
}
