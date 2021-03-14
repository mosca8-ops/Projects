using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.Editor
{
    [CustomPropertyDrawer(typeof(ShowIfAttribute), true)]
    public class ShowIfAttributeDrawer : ComposablePropertyDrawer
    {
        bool m_initialized;

        private Func<bool> m_validation;
        private bool m_invert;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if (!m_initialized)
            {
                m_initialized = true;
                Initialize(property);
            }
            if (Show()) {
                base.OnGUI(position, property, label);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            if (!m_initialized)
            {
                m_initialized = true;
                Initialize(property);
            }
            return Show() ? base.GetPropertyHeight(property, label) : -EditorGUIUtility.standardVerticalSpacing;
        }

        private bool Show()
        {
            return m_validation == null || (m_invert ^ m_validation());
        }

        private void Initialize(SerializedProperty property)
        {
            if (attribute is ShowIfAttribute attr)
            {
                m_validation = Delegate.CreateDelegate(typeof(Func<bool>), property.serializedObject.targetObject, attr.MethodPath) as Func<bool>;
                m_invert = attr.Invert;
            }
        }
    }
}
