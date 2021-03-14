using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(GenericComparisonCondition))]
    class GenericComparisonConditionEditor : ConditionEditor
    {
        private GenericComparisonCondition m_condition;
        private Type m_propertyType;
        private bool m_isComparable;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_condition = target as GenericComparisonCondition;
        }

        protected override void DrawProperties(Rect rect, SerializedProperty property)
        {
            if (!m_condition.TargetA)
            {
                var propRect = rect;
                DrawProperty(property, ref propRect);
                propRect.height = rect.height - propRect.height - EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.HelpBox(propRect, "No gameobject set for property target, please select one", MessageType.Warning);
                return;
            }

            if(m_propertyType != m_condition.PropertyType)
            {
                m_propertyType = m_condition.PropertyType;
                m_isComparable = m_propertyType != typeof(bool) && typeof(IComparable).IsAssignableFrom(m_propertyType);
            }

            if (m_isComparable)
            {
                base.DrawProperties(rect, property);
                return;
            }
            
            do
            {
                if (property.name == "m_operator")
                {
                    property.FindPropertyRelative("m_operator").enumValueIndex = 0; // Set to equals
                    bool wasEnabled = GUI.enabled;
                    GUI.enabled = false;
                    DrawProperty(property, ref rect);
                    GUI.enabled = wasEnabled;
                }
                else
                {
                    DrawProperty(property, ref rect);
                }
            }
            while (property.NextVisible(false));
        }
    }
}