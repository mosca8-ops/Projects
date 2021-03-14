using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TXT.WEAVR.Core;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEngine;


namespace TXT.WEAVR.Common
{
    [CustomPropertyDrawer(typeof(Optional), true)]
    public class OptionalsEditor : ComposablePropertyDrawer
    {
        private SerializedProperty enableProperty;
        private SerializedProperty valueProperty;

        private AnimatedValueDrawer m_animatedValueDrawer;
        private bool m_isAnimated;
        private bool m_initialized;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!m_initialized || enableProperty.serializedObject != property.serializedObject)
            {
                m_initialized = true;
                enableProperty = property.FindPropertyRelative(nameof(Optional.enabled));
                //valueProperty = property.FindPropertyRelative(nameof(Optional.enabled));
                valueProperty = enableProperty.Copy();
                valueProperty.NextVisible(false);

                var propertyType = valueProperty.GetPropertyType();
                m_isAnimated = propertyType?.IsSubclassOf(typeof(AnimatedValue)) ?? false;
                if (m_isAnimated)
                {
                    m_animatedValueDrawer = AnimatedValueDrawer.GetDrawer(propertyType);
                    //if(m_animatedValueDrawer != null)
                    //{
                    //    m_animatedValueDrawer.FetchAttributes(fieldInfo.GetCustomAttributes()
                    //                                    .Select(a => a as WeavrAttribute)
                    //                                    .Where(a => a != null));
                    //}
                }
            }

            EditorGUI.BeginProperty(position, label, property);
            float width = EditorGUIUtility.singleLineHeight;
            //enableProperty.boolValue = EditorGUI.ToggleLeft(position, GUIContent.none, m_target.enabled);
            enableProperty.boolValue = GUI.Toggle(new Rect(position.x + EditorGUI.indentLevel * width,
                                                           position.y,
                                                           width,
                                                           width),
                                                           enableProperty.boolValue, GUIContent.none);

            if (m_isAnimated && !enableProperty.boolValue)
            {
                valueProperty.FindPropertyRelative("m_animate").boolValue = false;
            }

            bool guiEnabled = GUI.enabled;
            GUI.enabled = enableProperty.boolValue;
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth -= width;
            position.x += width + EditorGUI.indentLevel * width;
            position.width -= width;
            int indent = EditorGUI.indentLevel;
            position.x -= EditorGUI.indentLevel * 15;
            //position.width -= EditorGUI.indentLevel * 15;
            DrawValue(position, valueProperty, label);
            EditorGUI.indentLevel = indent;
            GUI.enabled = guiEnabled;
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.EndProperty();
        }

        protected virtual void DrawValue(Rect position, SerializedProperty valueProperty, GUIContent label)
        {
            if (m_animatedValueDrawer != null)
            {
                m_animatedValueDrawer.OnGUI(position, valueProperty, label);
            }
            else
            {
                EditorGUI.PropertyField(position, valueProperty, label, valueProperty.isExpanded);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var valueProperty = property.FindPropertyRelative("value");
            return GetValueHeight(valueProperty, label);
        }

        protected virtual float GetValueHeight(SerializedProperty valueProperty, GUIContent label)
        {
            return m_animatedValueDrawer != null ? m_animatedValueDrawer.GetPropertyHeight(valueProperty, label)
                                                 : EditorGUI.GetPropertyHeight(valueProperty, label, valueProperty.isExpanded);
        }
    }

    [CustomPropertyDrawer(typeof(OptionalQuaternion), true)]
    public class OptionalQuaternionEditor : OptionalsEditor
    {
        private bool? m_showAsEuler;

        protected override void DrawValue(Rect position, SerializedProperty valueProperty, GUIContent label)
        {
            if(m_showAsEuler == true)
            {
                EditorGUI.BeginChangeCheck();
                var newEuler = EditorGUI.Vector3Field(position, label, valueProperty.quaternionValue.eulerAngles);
                if (EditorGUI.EndChangeCheck())
                {
                    valueProperty.quaternionValue = Quaternion.Euler(newEuler);
                }
                return;
            }
            base.DrawValue(position, valueProperty, label);
        }

        protected override float GetValueHeight(SerializedProperty valueProperty, GUIContent label)
        {
            if (!m_showAsEuler.HasValue)
            {
                m_showAsEuler = valueProperty.GetAttributesInParents<ShowAsEulerAttribute>() != null;
            }
            return m_showAsEuler == true ? EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing : 
                base.GetValueHeight(valueProperty, label);
        }
    }
}