using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(ConditionValidFor), true)]
    class ConditionValidForEditor : ConditionEditor
    {
        public override bool ShouldDrawNotToggle => false;

        protected GUIContent m_progressiveContent = new GUIContent();
        protected ConditionValidFor m_condition;
        private bool? m_progressiveIsEditable;
        protected float m_childHeight;

        protected bool CanBeProgressive
        {
            get
            {
                if (!m_progressiveIsEditable.HasValue)
                {
                    var property = serializedObject.FindProperty("m_progressive");
                    m_progressiveIsEditable = !m_hiddenProperties.Contains(property.propertyPath);
                    m_progressiveContent = new GUIContent(property.displayName, string.IsNullOrEmpty(property.tooltip) ?
                                                        "If true than this condition will slowly reset its progress to 0 when child is false" 
                                                        : property.tooltip);
                }
                return m_progressiveIsEditable.Value;
            }
        }

        //protected override float HeaderHeight => 0;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (target)
            {
                m_condition = target as ConditionValidFor;
            }
        }

        protected override void DrawProperties(Rect rect, SerializedProperty property)
        {
            EditorGUIUtility.labelWidth = 100;
            rect.height = EditorGUIUtility.singleLineHeight;
            if (CanBeProgressive)
            {
                float progressiveWidth = s_styles.textToggle.CalcSize(m_progressiveContent).x;
                var controlsRect = new Rect(rect.x, rect.y, rect.width - progressiveWidth - s_styles.textToggle.margin.horizontal, rect.height);
                EditorGUI.PropertyField(controlsRect, property);
                property.Next(false);
                controlsRect.x += controlsRect.width + s_styles.textToggle.margin.left;
                controlsRect.width = progressiveWidth;
                property.boolValue = GUI.Toggle(controlsRect, property.boolValue, m_progressiveContent, s_styles.textToggle);
            }
            else
            {
                EditorGUI.PropertyField(rect, property);
            }

            var editor = Get(m_condition.Child) as ConditionEditor;
            rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing + (editor?.BoxStyle ?? s_styles.negativeBox).border.top;
            rect.height = m_childHeight;
            editor.DrawFull(rect);
        }

        protected override float GetHeightInternal()
        {
            s_styles.Refresh();
            m_childHeight = m_condition.Child ? Get(m_condition.Child).GetHeight() : 0;
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + m_childHeight + HeaderHeight;
        }
    }
}