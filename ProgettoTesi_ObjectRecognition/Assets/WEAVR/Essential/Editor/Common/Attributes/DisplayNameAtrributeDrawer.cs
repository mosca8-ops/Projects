using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [CustomPropertyDrawer(typeof(DisplayNameAttribute))]
    public class DisplayNameAtrributeDrawer : Editor.ComposablePropertyDrawer
    {

        private bool m_initialized;
        private Func<string> m_nameGetter;
        private string m_nameToDisplay;
        private bool m_isValid;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if (!m_initialized)
            {
                m_initialized = true;
                var attr = attribute as DisplayNameAttribute;
                m_nameToDisplay = string.IsNullOrEmpty(attr.DisplayName) ? null : attr.DisplayName;
                if (!string.IsNullOrEmpty(attr.MethodToGetName))
                {
                    m_nameGetter = Delegate.CreateDelegate(typeof(Func<string>), property.serializedObject.targetObject, attr.MethodToGetName) as Func<string>;
                }
                m_isValid = m_nameToDisplay != null || m_nameGetter != null;
            }
            if (label != null && m_isValid)
            {
                label.text = m_nameGetter?.Invoke() ?? m_nameToDisplay;
            }
            base.OnGUI(position, property, label);
        }
    }
}
